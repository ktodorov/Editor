using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using RemedyPic.Common;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.ApplicationSettings;
using Windows.System.UserProfile;

#region Namespace RemedyPic
namespace RemedyPic
{
    #region MainPage class
    public sealed partial class MainPage : RemedyPic.Common.LayoutAwarePage
    {

        #region Variables

        // mruToken is used for LoadState and SaveState functions.
        private string mruToken = null;
        StorageFile file;
        private string appliedFilters = null, appliedColors = null,
                           appliedRotations = null, appliedColorize = null;
        // bitmapImage is the image that is edited in RemedyPic.
        private WriteableBitmap bitmapImage, exampleBitmap;

        // bitmapStream is used to save the pixel stream to bitmapImage.
        Stream bitmapStream, exampleStream;
        static readonly long cycleDuration = TimeSpan.FromSeconds(3).Ticks;

        // This is true if the user load a picture.
        bool pictureIsLoaded = false;

        FilterFunctions image = new FilterFunctions();
        FilterFunctions imageOriginal = new FilterFunctions();
        FilterFunctions uneditedImage = new FilterFunctions();
        Stream uneditedStream;
        private WriteableBitmap uneditedBitmap;

        // Those are variables used with the manipulations of the Image
        private TransformGroup _transformGroup;
        private MatrixTransform _previousTransform;
        private CompositeTransform _compositeTransform;
        private bool forceManipulationsToEnd;

        SelectedRegion selectedRegion;

        // The size of the corners.
        double cornerSize;
        uint sourceImagePixelWidth;
        uint sourceImagePixelHeight;
        double CornerSize
        {
            get
            {
                if (cornerSize <= 0)
                {
                    cornerSize = (double)Application.Current.Resources["Size"];
                }

                return cornerSize;
            }
        }

        // The previous points of all the pointers.
        Dictionary<uint, Point?> pointerPositionHistory = new Dictionary<uint, Point?>();



        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            AnimateOutPicture.Begin();
            RegisterCharms();
            forceManipulationsToEnd = false;
            displayImage.ManipulationStarting += new ManipulationStartingEventHandler(ManipulateMe_ManipulationStarting);
            displayImage.ManipulationStarted += new ManipulationStartedEventHandler(ManipulateMe_ManipulationStarted);
            displayImage.ManipulationDelta += new ManipulationDeltaEventHandler(ManipulateMe_ManipulationDelta);
            displayImage.ManipulationCompleted += new ManipulationCompletedEventHandler(ManipulateMe_ManipulationCompleted);
            displayImage.ManipulationInertiaStarting += new ManipulationInertiaStartingEventHandler(ManipulateMe_ManipulationInertiaStarting);
            InitManipulationTransforms();
            selectRegion.ManipulationMode = ManipulationModes.Scale |
                ManipulationModes.TranslateX | ManipulationModes.TranslateY;

            selectedRegion = new SelectedRegion { MinSelectRegionSize = 2 * CornerSize };
            this.DataContext = selectedRegion;
        }



        #region Charms
        private void RegisterCharms()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>(this.ShareImageHandler);
            SettingsPane.GetForCurrentView().CommandsRequested += OnSettingsPaneCommandRequested;
        }

        private void OnSettingsPaneCommandRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            args.Request.ApplicationCommands.Add(new SettingsCommand("commandID",
                                                                     "Feedback", FeedbackPopup));
        }

        private void FeedbackPopup(IUICommand command)
        {
            Feedback.IsOpen = true;
        }

        private async void ShareImageHandler(DataTransferManager sender,
            DataRequestedEventArgs e)
        {
            if (!pictureIsLoaded)
            {
                e.Request.FailWithDisplayText("Load an image and try sharing again! :)");
            }
            else
            {
                DataRequest request = e.Request;
                request.Data.Properties.Title = "RemedyPic";
                request.Data.Properties.Description = "Share your image.";
                request.Data.Properties.ApplicationName = "RemedyPic";

                // Because we are making async calls in the DataRequested event handler,
                //  we need to get the deferral first.
                DataRequestDeferral deferral = request.GetDeferral();

                // Make sure we always call Complete on the deferral.
                try
                {
                    using (IRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        Stream pixelStream = bitmapImage.PixelBuffer.AsStream();
                        byte[] pixels = new byte[pixelStream.Length];
                        await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)bitmapImage.PixelWidth, (uint)bitmapImage.PixelHeight, 96.0, 96.0, pixels);

                        List<IStorageItem> imageItems = new List<IStorageItem>();
                        await SaveFile(false);
                        imageItems.Add(file);
                        request.Data.SetStorageItems(imageItems);
                        RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(file);
                        request.Data.Properties.Thumbnail = imageStreamRef;
                        request.Data.SetBitmap(imageStreamRef);

                        await encoder.FlushAsync();
                    }
                }
                finally
                {
                    deferral.Complete();
                }
            }
        }
        #endregion

        #region LoadState
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (pageState != null && pageState.ContainsKey("mruToken"))
            {
                object value = null;
                if (pageState.TryGetValue("mruToken", out value))
                {
                    if (value != null)
                    {
                        mruToken = value.ToString();

                        // Open the file via the token that you stored when adding this file into the MRU list.
                        Windows.Storage.StorageFile file =
                            await Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(mruToken);

                        if (file != null)
                        {
                            // Open a stream for the selected file.
                            Windows.Storage.Streams.IRandomAccessStream fileStream =
                                await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                            bitmapImage.SetSource(fileStream);
                            displayImage.Source = bitmapImage;

                            // Set the data context for the page.
                            this.DataContext = file;
                        }
                    }
                }
            }
        }
        #endregion

        #region Save State
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            if (!String.IsNullOrEmpty(mruToken))
            {
                pageState["mruToken"] = mruToken;
            }
        }
        #endregion

        #region Get Photo
        // This occures when GetPhoto button is clicked
        private async void GetPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            // File picker APIs don't work if the app is in a snapped state.
            // If the app is snapped, try to unsnap it first. Only show the picker if it unsnaps.
            if (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped ||
                 Windows.UI.ViewManagement.ApplicationView.TryUnsnap() == true)
            {
                FileOpenPicker filePicker = new FileOpenPicker();
                filePicker.ViewMode = PickerViewMode.Thumbnail;
                filePicker.FileTypeFilter.Add(".jpg");
                filePicker.FileTypeFilter.Add(".png");
                filePicker.FileTypeFilter.Add(".bmp");
                filePicker.FileTypeFilter.Add(".jpeg");
                file = await filePicker.PickSingleFileAsync();
                bitmapImage = new WriteableBitmap(1, 1);

                if (file != null)
                // File is null if user cancels the file picker.
                {
                    AnimateOutPicture.Begin();
                    Windows.Storage.Streams.IRandomAccessStream fileStream =
                            await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);

                    sourceImagePixelHeight = decoder.PixelHeight;
                    sourceImagePixelWidth = decoder.PixelWidth;

                    bitmapImage.SetSource(fileStream);
                    RandomAccessStreamReference streamRef = RandomAccessStreamReference.CreateFromFile(file);

                    // If the interface was changed from previous image, it should be resetted.
                    resetInterface();
                    ResetZoomPos();
                    // Show the interface after the picture is loaded.
                    //contentGrid.Visibility = Visibility.Visible;
                    pictureIsLoaded = true;
                    doAllCalculations();
                }
            }
            else
            {
                // If the window can't be unsnapped, show alert.
                MessageDialog messageDialog = new MessageDialog("Can't open in snapped state. Please unsnap the app and try again", "Close");
                await messageDialog.ShowAsync();
            }
        }
        #endregion

        private async void doAllCalculations()
        {
            // We make all the required calculations in order for
            // the app elements to appear and work normal.
            uneditedBitmap = bitmapImage;

            exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));
            exampleStream = exampleBitmap.PixelBuffer.AsStream();
            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            uneditedStream = uneditedBitmap.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            image.srcPixels = new byte[(uint)exampleStream.Length];
            uneditedImage.srcPixels = new byte[(uint)uneditedStream.Length];
            await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
            await uneditedStream.ReadAsync(uneditedImage.srcPixels, 0, uneditedImage.srcPixels.Length);

            setElements(ColorsExamplePicture, exampleBitmap);
            setElements(RotationsExamplePicture, exampleBitmap);
            prepareImage(exampleStream, exampleBitmap, image);
            setStream(exampleStream, exampleBitmap, image);
            prepareImage(uneditedStream, uneditedBitmap, uneditedImage);
            setStream(uneditedStream, uneditedBitmap, uneditedImage);

            AnimateInPicture.Begin();
            ZoomStack.Visibility = Visibility.Visible;
            setFilterBitmaps();
            displayImage.MaxWidth = imagePanel.ActualWidth;
            displayImage.MaxHeight = imagePanel.ActualHeight;
            setFileProperties(file);
            setPopupsHeight();
            displayImage.Source = bitmapImage;
            AvailableZoom.IsChecked = true;

            NewResolution.Text = string.Format("New Resolution: {0}x{1} (current)", bitmapImage.PixelWidth, bitmapImage.PixelHeight);
        }

        private void setPopupsHeight()
        {
            // We set the popup height to match the current machine resolution
            Filters.Height = PopupFilters.ActualHeight + 5;
            Colors.Height = PopupColors.ActualHeight + 5;
            Rotations.Height = PopupRotations.ActualHeight + 5;
            Zoom.Height = PopupZoom.ActualHeight + 5;
            ImageOptions.Height = PopupImageOptions.ActualHeight + 5;
            Colorize.Height = PopupColorize.ActualHeight + 5;
            Frames.Height = PopupFrames.ActualHeight + 5;
            Histogram.Height = PopupHistogram.ActualHeight + 5;

            // We set the imagePanel maximum height so the image not to go out of the screen
            imagePanel.MaxWidth = imageBorder.ActualWidth - 10;
        }

        private void setElements(Windows.UI.Xaml.Controls.Image imageElement, WriteableBitmap source)
        {
            // We set the XAML Image object a bitmap as source 
            // and then set the width and height to be proportional to the actual bitmap
            imageElement.Source = source;
            imageElement.Width = bitmapImage.PixelWidth / 4;
            imageElement.Height = bitmapImage.PixelHeight / 4;
        }

        #region Invert Filter
        private void OnInvertClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "invert";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Invert_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();
                setStream(exampleStream, exampleBitmap, image);
                resetInterface();
                //changeButton(ref invertButton);
            }
        }

        private void Invert_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = -1;
            offset = 255;
            scale = 1;
        }
        #endregion

        #region B&W Filter
        private void OnBlackWhiteClick(object sender, RoutedEventArgs e)
        {
            // This occures when OnBlackWhiteButton is clicked
            if (pictureIsLoaded)
            {
                // First we prepare the image for filtrating, then we call the filter.
                // After that we save the new data to the current image,
                // reset all other highlighted buttons and make the B&W button selected
                appliedFilters = "blackwhite";
                prepareImage(exampleStream, exampleBitmap, image);
                image.BlackAndWhite(image.dstPixels, image.srcPixels);
                setStream(exampleStream, exampleBitmap, image);
                resetInterface();
                //changeButton(ref BlackAndWhiteButton);
            }
        }
        #endregion

        #region Emboss Filter
        private void OnEmbossClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "emboss";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Emboss_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();
                setStream(exampleStream, exampleBitmap, image);
                resetInterface();
                //changeButton(ref embossButton);
            }
        }

        private void Emboss_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[3, 3] = -1;
            offset = 128;
            scale = 1;
        }
        #endregion

        #region Emboss 2 Filter
        private void OnEmboss2Click(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "emboss2";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Emboss2_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();
                setStream(exampleStream, exampleBitmap, image);
                resetInterface();
                //changeButton(ref emboss2Button);
            }
        }

        private void Emboss2_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[2, 1] = -1;
            coeff[1, 2] = -1;
            coeff[1, 1] = -2;
            coeff[2, 3] = 1;
            coeff[3, 2] = 1;
            coeff[4, 3] = 2;
            offset = 0;
            scale = 1;
        }
        #endregion

        #region Sharpen Filter
        private void OnSharpenClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "sharpen";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Sharpen_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);

                resetInterface();
                //changeButton(ref SharpenButton);
            }
        }

        private void Sharpen_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 5;
            coeff[2, 3] = -1;
            coeff[2, 1] = -1;
            coeff[1, 2] = -1;
            coeff[3, 2] = -1;
            offset = 0;
            scale = 1;
        }
        #endregion

        #region Blur Filter
        private void OnBlurClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "blur";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Blur_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);

                resetInterface();
                //changeButton(ref blurButton);
            }
        }

        /*		private void Blur_SetValues(ref int[,] coeff, ref int offset, ref int scale)
                {
                    coeff[2, 2] = 3;
                    coeff[0, 0] = coeff[1, 0] = coeff[2, 0] = coeff[3, 0] = coeff[4, 0] = 1;
                    coeff[0, 1] = coeff[0, 2] = coeff[0, 3] = coeff[4, 1] = coeff[4, 2] = coeff[4, 3] = 1;
                    coeff[0, 4] = coeff[1, 4] = coeff[2, 4] = coeff[3, 4] = coeff[4, 4] = 1;
                    coeff[1, 1] = coeff[2, 1] = coeff[3, 1] = 2;
                    coeff[1, 2] = coeff[3, 2] = 2;
                    coeff[1, 3] = coeff[2, 3] = coeff[3, 3] = 2;
                    offset = 0;
                    scale = 35;
                }*/
        private void Blur_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[1, 1] = coeff[2, 1] = coeff[3, 1] = 3;
            coeff[1, 2] = coeff[3, 2] = 3;
            coeff[1, 3] = coeff[2, 3] = coeff[3, 3] = 3;
            offset = 0;
            scale = 25;
        }
        #endregion

        #region Blur2 Filter
        private void OnBlur2Click(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "blur2";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Blur2_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);

                resetInterface();
                //changeButton(ref blur2Button);
            }
        }

        private void Blur2_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[1, 1] = coeff[2, 1] = coeff[3, 1] = 1;
            coeff[1, 2] = coeff[3, 2] = 1;
            coeff[1, 3] = coeff[2, 3] = coeff[3, 3] = 1;
            offset = 0;
            scale = 9;
        }

        #endregion

        #region EdgeDetect Filter
        private void OnEdgeDetectClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "EdgeDetect";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                EdgeDetect_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);

                resetInterface();
                //changeButton(ref EdgeDetectButton);
            }
        }

        private void EdgeDetect_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = -4;
            coeff[1, 2] = coeff[2, 1] = coeff[2, 3] = coeff[3, 2] = 1;
            offset = 0;
            scale = 1;
        }

        #endregion

        #region EdgeEnhance Filter
        private void OnEdgeEnhanceClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "EdgeEnhance";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                EdgeEnhance_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);

                resetInterface();
                //changeButton(ref EdgeEnhanceButton);
            }
        }

        private void EdgeEnhance_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[1, 2] = -1;
            offset = 0;
            scale = 1;
        }

        #endregion

        #region Custom Filter
        private void OnCustomClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Custom_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);
            }
        }

        private void Custom_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = -1; //Set All values from fields...
            offset = 255;
            scale = 1;
        }
        #endregion

        #region Save
        private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            // File picker APIs don't work if the app is in a snapped state.
            // If the app is snapped, try to unsnap it first. Only show the picker if it unsnaps.
            if (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped ||
                 Windows.UI.ViewManagement.ApplicationView.TryUnsnap() == true)
            {
                await SaveFile(true);
            }
            else
            {
                MessageDialog messageDialog = new MessageDialog("Can't save in snapped state. Please unsnap the app and try again", "Close");
                await messageDialog.ShowAsync();
            }
        }

        private async Task SaveFile(bool picker)
        {
            // Only execute if there is a picture that is loaded
            if (pictureIsLoaded)
            {
                if (picker == true)
                {
                    FileSavePicker savePicker = new FileSavePicker();

                    // Set My Documents folder as suggested location if no past selected folder is available
                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                    // Dropdown of file types the user can save the file as
                    savePicker.FileTypeChoices.Add("JPEG", new List<string>() { ".jpg" });
                    savePicker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
                    savePicker.FileTypeChoices.Add("Bitmap", new List<string>() { ".bmp" });

                    savePicker.SuggestedFileName = fileName.Text;

                    // Default file name if the user does not type one in or select a file to replace
                    file = await savePicker.PickSaveFileAsync();
                }
                else
                {
                    if (imageOriginal.dstPixels == null)
                    {
                        return;
                    }
                    file = await ApplicationData.Current.LocalFolder.CreateFileAsync("temp.jpg", CreationCollisionOption.ReplaceExisting);
                }
                System.Guid fileType = BitmapEncoder.JpegEncoderId;

                // File is null if the user press Cancel without choosing file
                if (file != null)
                {
                    // Check the file type that the user had selected and set the BitmapEncoder to that type
                    switch (file.FileType)
                    {
                        case ".jpeg":
                        case ".jpg":
                            fileType = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".png":
                            fileType = BitmapEncoder.PngEncoderId;
                            break;
                        case ".bmp":
                            fileType = BitmapEncoder.BmpEncoderId;
                            break;
                        default:
                            break;
                    }

                    using (IRandomAccessStream writeStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(fileType, writeStream);
                        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                                                           (uint)bitmapImage.PixelWidth, (uint)bitmapImage.PixelHeight, 96.0, 96.0, imageOriginal.dstPixels);
                        // Flush all the data to the encoder(file)
                        await encoder.FlushAsync();
                    }
                }
            }
        }
        #endregion

        #region Brightness Scroll
        private void OnValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // This occures when the brightness scroll value is changed.
            if (pictureIsLoaded)
            {
                // We prepare the image for editing
                // Then we check if the changed value 
                // is higher than 0 - we call the brightness function
                // is lower than 0  - we call the darkness function
                // And finally we save the new byte array to the image.
                prepareImage(exampleStream, exampleBitmap, image);
                if (brightSlider.Value < 0)
                {
                    appliedColors = "darken";
                    image.Darken(brightSlider.Value);
                }
                else if (brightSlider.Value >= 0)
                {
                    appliedColors = "lighten";
                    image.Lighten(brightSlider.Value);
                }
                setStream(exampleStream, exampleBitmap, image);
            }
        }
        #endregion

        void setFileProperties(Windows.Storage.StorageFile file)
        {
            // This sets the file name to the text box
            fileName.Text = file.Name;
        }

        void setStream(Stream givenStream, WriteableBitmap givenBitmap, FilterFunctions givenImage)
        {
            // This sets the pixels to the bitmap
            // and hides the visible Apply buttons.
            givenStream.Seek(0, SeekOrigin.Begin);
            givenStream.Write(givenImage.dstPixels, 0, givenImage.dstPixels.Length);
            givenBitmap.Invalidate();
            if (givenImage == image)
            {
                if (PopupFilters.IsOpen)
                    FilterApplyReset.Visibility = Visibility.Visible;
                else if (PopupColors.IsOpen)
                    ColorApplyReset.Visibility = Visibility.Visible;
                else if (PopupRotations.IsOpen)
                    RotateApplyReset.Visibility = Visibility.Visible;
            }
        }

        void prepareImage(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            // This calculates the width and height of the bitmap image
            // and sets the Stream and the pixels byte array
            givenImage.width = (int)bitmap.PixelWidth;
            givenImage.height = (int)bitmap.PixelHeight;
            stream = bitmap.PixelBuffer.AsStream();
            givenImage.dstPixels = new byte[4 * bitmap.PixelWidth * bitmap.PixelHeight];
            givenImage.Reset();
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            // This resets the interface and returns the last applied image.
            if (pictureIsLoaded)
            {
                brightSlider.Value = 0;
                RedColorSlider.Value = 0;
                GreenColorSlider.Value = 0;
                BlueColorSlider.Value = 0;
                prepareImage(exampleStream, exampleBitmap, image);
                image.Reset();
                setStream(exampleStream, exampleBitmap, image);
                resetInterface();
            }
            appliedFilters = null;
            appliedColors = null;
        }


        private void resetButton(ref Windows.UI.Xaml.Controls.Button but)
        {
            // This resets the passed button with normal border.
            but.BorderThickness = new Thickness(1, 1, 1, 1);
            but.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.White);
        }

        private void changeButton(ref Windows.UI.Xaml.Controls.Button but)
        {
            // This make the passed button "selected" - it makes its border bigger and green.
            but.BorderThickness = new Thickness(3, 3, 3, 3);
            but.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
        }


        private void resetInterface()
        {
            // This calls the reset function for every button
            // and sets the values of all sliders to 0.
            brightSlider.Value = 0;
            RedColorSlider.Value = 0;
            GreenColorSlider.Value = 0;
            BlueColorSlider.Value = 0;
            RedContrastSlider.Value = 0;
            GreenContrastSlider.Value = 0;
            BlueContrastSlider.Value = 0;
            RedGammaSlider.Value = 10;
            GreenGammaSlider.Value = 10;
            BlueGammaSlider.Value = 10;
            deselectColorizeListItems();
            deselectFilters();
            //deselectMenu();
        }

        #region Color Change RGB
        private void OnRColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedColors = "redcolor";
                prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                setStream(exampleStream, exampleBitmap, image);
            }
        }

        private void OnGColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedColors = "greencolor";
                prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                setStream(exampleStream, exampleBitmap, image);
            }
        }

        private void OnBColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedColors = "bluecolor";
                prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                setStream(exampleStream, exampleBitmap, image);
            }
        }
        #endregion

        #region Contrast Change
        private void OnRContrastChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedColors = "redcontrast";
                prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                setStream(exampleStream, exampleBitmap, image);
            }
        }

        private void OnGContrastChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedColors = "greencontrast";
                prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                setStream(exampleStream, exampleBitmap, image);
            }
        }

        private void OnBContrastChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedColors = "bluecontrast";
                prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                setStream(exampleStream, exampleBitmap, image);
            }
        }
        #endregion

        #region Resizing an image
        private async Task<WriteableBitmap> ResizeImage(WriteableBitmap baseWriteBitmap, uint width, uint height)
        {
            // Get the pixel buffer of the writable bitmap in bytes
            Stream stream = baseWriteBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[(uint)stream.Length];
            await stream.ReadAsync(pixels, 0, pixels.Length);
            //Encoding the data of the PixelBuffer we have from the writable bitmap
            var inMemoryRandomStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, inMemoryRandomStream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)baseWriteBitmap.PixelWidth, (uint)baseWriteBitmap.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync();
            // At this point we have an encoded image in inMemoryRandomStream
            // We apply the transform and decode
            var transform = new BitmapTransform
            {
                ScaledWidth = width,
                ScaledHeight = height
            };
            inMemoryRandomStream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(inMemoryRandomStream);
            var pixelData = await decoder.GetPixelDataAsync(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Straight,
                            transform,
                            ExifOrientationMode.IgnoreExifOrientation,
                            ColorManagementMode.DoNotColorManage);
            // An array containing the decoded image data
            var sourceDecodedPixels = pixelData.DetachPixelData();
            // Approach 1 : Encoding the image buffer again:
            // Encoding data
            var inMemoryRandomStream2 = new InMemoryRandomAccessStream();
            var encoder2 = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, inMemoryRandomStream2);
            encoder2.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, width, height, 96, 96, sourceDecodedPixels);
            await encoder2.FlushAsync();
            inMemoryRandomStream2.Seek(0);
            // finally the resized writablebitmap
            var bitmap = new WriteableBitmap((int)width, (int)height);
            await bitmap.SetSourceAsync(inMemoryRandomStream2);
            return bitmap;
        }
        #endregion

        #region Filters
        private void doBlackWhite(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.BlackAndWhite(givenImage.dstPixels, givenImage.srcPixels);
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doInvert(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Invert_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doEmboss(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Emboss_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doEmboss2(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Emboss2_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doSharpen(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Sharpen_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doSharpen1(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);

            givenImage.Sharpen1();
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doColorize(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.Colorize(appliedColorize);
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doBlur(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Blur_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);

            resetInterface();
        }

        private void doBlur2(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Blur2_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);

            resetInterface();
        }

        private void doEdgeDetect(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            EdgeDetect_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);

            resetInterface();
        }

        private void doEdgeEnhance(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            EdgeEnhance_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);

            resetInterface();
        }

        private void doRetro(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(0, 0, 0, 50, 50, -50);
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doDarken(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(0, 0, 0, 50, 50, 0);
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doBrighten(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(70, 70, 70, 0, 0, 0);
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doShadow(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(-80, -80, -80, 0, 0, 0);
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        private void doCrystal(Stream stream, WriteableBitmap bitmap, FilterFunctions givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(0, 0, 0, 50, 35, 35);
            setStream(stream, bitmap, givenImage);
            resetInterface();
        }

        #endregion

        #region Apply Buttons
        private void OnFilterApplyClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;
            FilterApplyReset.Visibility = Visibility.Collapsed;
            SelectFilters.IsChecked = false;
            switch (appliedFilters)
            {
                case "blackwhite":
                    doBlackWhite(bitmapStream, bitmapImage, imageOriginal);
                    doBlackWhite(exampleStream, exampleBitmap, image);
                    break;
                case "invert":
                    doInvert(bitmapStream, bitmapImage, imageOriginal);
                    doInvert(exampleStream, exampleBitmap, image);
                    break;
                case "emboss":
                    doEmboss(bitmapStream, bitmapImage, imageOriginal);
                    doEmboss(exampleStream, exampleBitmap, image);
                    break;
                case "emboss2":
                    doEmboss2(bitmapStream, bitmapImage, imageOriginal);
                    doEmboss2(exampleStream, exampleBitmap, image);
                    break;
                case "blur":
                    doBlur(bitmapStream, bitmapImage, imageOriginal);
                    doBlur(exampleStream, exampleBitmap, image);
                    break;
                case "blur2":
                    doBlur2(bitmapStream, bitmapImage, imageOriginal);
                    doBlur2(exampleStream, exampleBitmap, image);
                    break;
                case "sharpen":
                    doSharpen(bitmapStream, bitmapImage, imageOriginal);
                    doSharpen(exampleStream, exampleBitmap, image);
                    break;
                case "sharpen1":
                    doSharpen1(bitmapStream, bitmapImage, imageOriginal);
                    doSharpen1(exampleStream, exampleBitmap, image);
                    break;
                case "EdgeDetect":
                    doEdgeDetect(bitmapStream, bitmapImage, imageOriginal);
                    doEdgeDetect(exampleStream, exampleBitmap, image);
                    break;
                case "EdgeEnhance":
                    doEdgeEnhance(bitmapStream, bitmapImage, imageOriginal);
                    doEdgeEnhance(exampleStream, exampleBitmap, image);
                    break;
                case "colorize":
                    doColorize(bitmapStream, bitmapImage, imageOriginal);
                    doColorize(exampleStream, exampleBitmap, image);
                    break;
                case "retro":
                    doRetro(bitmapStream, bitmapImage, imageOriginal);
                    doRetro(exampleStream, exampleBitmap, image);
                    break;
                case "darken":
                    doDarken(bitmapStream, bitmapImage, imageOriginal);
                    doDarken(exampleStream, exampleBitmap, image);
                    break;
                case "brighten":
                    doBrighten(bitmapStream, bitmapImage, imageOriginal);
                    doBrighten(exampleStream, exampleBitmap, image);
                    break;
                case "shadow":
                    doShadow(bitmapStream, bitmapImage, imageOriginal);
                    doShadow(exampleStream, exampleBitmap, image);
                    break;
                case "crystal":
                    doCrystal(bitmapStream, bitmapImage, imageOriginal);
                    doCrystal(exampleStream, exampleBitmap, image);
                    break;
                default:
                    break;
            }
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            setFilterBitmaps();
            appliedFilters = null;
            deselectFilters();
            ImageLoadingRing.IsActive = false;
        }

        private void OnColorApplyClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;
            ColorApplyReset.Visibility = Visibility.Collapsed;
            SelectColors.IsChecked = false;
            switch (appliedColors)
            {
                case "darken":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.Darken(brightSlider.Value);
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "lighten":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.Lighten(brightSlider.Value);
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "redcolor":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "greencolor":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "bluecolor":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "redcontrast":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "greencontrast":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "bluecontrast":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "gamma":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                default:
                    break;
            }
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            setFilterBitmaps();
            appliedColors = null;
            ImageLoadingRing.IsActive = false;
        }

        private void OnRotateApplyClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;
            SelectRotations.IsChecked = false;
            switch (appliedRotations)
            {
                case "rotate":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.Rotate();
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "hflip":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.HFlip();
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "vflip":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.VFlip();
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
            }
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            setFilterBitmaps();
            appliedRotations = null;
            ImageLoadingRing.IsActive = false;
        }

        private void OnColorizeApplyClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            setFilterBitmaps();
            appliedColorize = null;
            deselectColorizeListItems();
            SelectColorize.IsChecked = false;
            ImageLoadingRing.IsActive = false;
        }

        #endregion

        #region Reset Buttons
        private void OnFilterResetClick(object sender, RoutedEventArgs e)
        {
            // This resets the interface and returns the last applied image.
            if (pictureIsLoaded)
            {
                prepareImage(exampleStream, exampleBitmap, image);
                image.Reset();
                setStream(exampleStream, exampleBitmap, image);
                resetInterface();
            }
            FilterApplyReset.Visibility = Visibility.Collapsed;
            appliedFilters = null;
            deselectFilters();
        }

        private void OnColorResetClick(object sender, RoutedEventArgs e)
        {
            // This resets the interface and returns the last applied image.
            if (pictureIsLoaded)
            {
                brightSlider.Value = 0;
                RedColorSlider.Value = 0;
                GreenColorSlider.Value = 0;
                BlueColorSlider.Value = 0;
                prepareImage(exampleStream, exampleBitmap, image);
                image.Reset();
                setStream(exampleStream, exampleBitmap, image);
                resetInterface();
            }
            ColorApplyReset.Visibility = Visibility.Collapsed;
            appliedColors = null;
        }

        private void OnRotateResetClick(object sender, RoutedEventArgs e)
        {
            prepareImage(exampleStream, exampleBitmap, image);
            image.Reset();
            setStream(exampleStream, exampleBitmap, image);
            ColorApplyReset.Visibility = Visibility.Collapsed;
            appliedRotations = null;
            RotateApplyReset.Visibility = Visibility.Collapsed;
        }


        private void OnColorizeResetClick(object sender, RoutedEventArgs e)
        {
            appliedColorize = null;
            RestoreOriginalBitmap();
            deselectColorizeListItems();
        }
        #endregion

        #region Checked Buttons
        private void FiltersChecked(object sender, RoutedEventArgs e)
        {
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            PopupFilters.IsOpen = true;

        }

        private void FiltersUnchecked(object sender, RoutedEventArgs e)
        {
            PopupFilters.IsOpen = false;

        }

        private void ColorsChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            PopupColors.IsOpen = true;
        }

        private void ColorsUnchecked(object sender, RoutedEventArgs e)
        {
            PopupColors.IsOpen = false;
        }

        private void RotationsChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            PopupRotations.IsOpen = true;
        }

        private void RotationsUnchecked(object sender, RoutedEventArgs e)
        {
            PopupRotations.IsOpen = false;
        }

        private void OptionsChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            PopupImageOptions.IsOpen = true;
        }

        private void OptionsUnchecked(object sender, RoutedEventArgs e)
        {
            PopupImageOptions.IsOpen = false;
        }

        private void ColorizeChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            PopupColorize.IsOpen = true;
        }

        private void ColorizeUnchecked(object sender, RoutedEventArgs e)
        {
            PopupColorize.IsOpen = false;
        }


        private void FramesChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectHistogram.IsChecked = false;
            PopupFrames.IsOpen = true;
        }

        private void FramesUnchecked(object sender, RoutedEventArgs e)
        {
            PopupFrames.IsOpen = false;
        }

        private void HistogramChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            PopupHistogram.IsOpen = true;
        }

        private void HistogramUnchecked(object sender, RoutedEventArgs e)
        {
            PopupHistogram.IsOpen = false;
        }

        #endregion

        #region Frames

        private void OnFourSidesClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                imageOriginal.Frames_StandardLeftSide(FrameBColor.Value, FrameGColor.Value, FrameRColor.Value, FrameWidth.Value);
                imageOriginal.Frames_StandardTopSide(FrameBColor.Value, FrameGColor.Value, FrameRColor.Value, FrameWidth.Value);
                imageOriginal.Frames_StandardRightSide(FrameBColor.Value, FrameGColor.Value, FrameRColor.Value, FrameWidth.Value);
                imageOriginal.Frames_StandardBottomSide(FrameBColor.Value, FrameGColor.Value, FrameRColor.Value, FrameWidth.Value);
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
        }

        private void OnLeftSideClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                imageOriginal.Frames_StandardLeftSide(FrameBColor.Value, FrameGColor.Value, FrameRColor.Value, FrameWidth.Value);
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
        }

        private void OnTopSideClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                imageOriginal.Frames_StandardTopSide(FrameBColor.Value, FrameGColor.Value, FrameRColor.Value, FrameWidth.Value);
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
        }

        private void OnRightSideClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                imageOriginal.Frames_StandardRightSide(FrameBColor.Value, FrameGColor.Value, FrameRColor.Value, FrameWidth.Value);
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
        }

        private void OnBottomSideClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                imageOriginal.Frames_StandardBottomSide(FrameBColor.Value, FrameGColor.Value, FrameRColor.Value, FrameWidth.Value);
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
        }

        private void OnDarknessFourSidesClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                imageOriginal.Frames_DarknessLeftSide();
                imageOriginal.Frames_DarknessTopSide();
                imageOriginal.Frames_DarknessRightSide();
                imageOriginal.Frames_DarknessBottomSide();
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
        }

        private void OnDarknessLeftRightSidesClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                imageOriginal.Frames_DarknessLeftSide();
                imageOriginal.Frames_DarknessRightSide();
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
        }

        private void OnDarknessTopBottomSidesClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                imageOriginal.Frames_DarknessTopSide();
                imageOriginal.Frames_DarknessBottomSide();
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
        }

        private void OnDarknessAngleClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                imageOriginal.Frames_SmoothDarkness();
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
        }
        #endregion

        private void OnRotateClick(object sender, RoutedEventArgs e)
        {
            appliedRotations = "rotate";
            prepareImage(exampleStream, exampleBitmap, image);
            image.Rotate();
            setStream(exampleStream, exampleBitmap, image);
            resetInterface();
        }

        private void OnHFlipClick(object sender, RoutedEventArgs e)
        {
            appliedRotations = "hflip";
            prepareImage(exampleStream, exampleBitmap, image);
            image.HFlip();
            setStream(exampleStream, exampleBitmap, image);
            resetInterface();
        }

        private void OnVFlipClick(object sender, RoutedEventArgs e)
        {
            appliedRotations = "vflip";
            prepareImage(exampleStream, exampleBitmap, image);
            image.VFlip();
            setStream(exampleStream, exampleBitmap, image);
        }

        private void OnGamaChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedRotations = "gamma";
                prepareImage(exampleStream, exampleBitmap, image);
                image.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
                setStream(exampleStream, exampleBitmap, image);
            }
        }

        #region Back buttons
        private void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            SelectColors.IsChecked = false;
            SelectFilters.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
        }

        private void BackFeedbackClicked(object sender, RoutedEventArgs e)
        {
            Feedback.IsOpen = false;
            SettingsPane.Show();
        }
        #endregion

        #region Zoom
        private void ZoomInClicked(object sender, RoutedEventArgs e)
        {
            scale.ScaleX = scale.ScaleX + 0.1;
            scale.ScaleY = scale.ScaleY + 0.1;
            ZoomOut.Visibility = Visibility.Visible;
        }

        private void ZoomOutClicked(object sender, RoutedEventArgs e)
        {
            if (scale.ScaleX > 1 && scale.ScaleY > 1)
            {
                scale.ScaleX = scale.ScaleX - 0.1;
                scale.ScaleY = scale.ScaleY - 0.1;
            }
            else
            {
                ZoomOut.Visibility = Visibility.Collapsed;
            }
        }

        private void OnResetZoomClick(object sender, RoutedEventArgs e)
        {
            ResetZoomPos();
        }

        private void ResetZoomPos()
        {
            displayImage.Margin = new Thickness(0, 0, 0, 0);
            ZoomOut.Visibility = Visibility.Collapsed;
            displayImage.RenderTransform = null;
            InitManipulationTransforms();
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }
        private void MoveChecked(object sender, RoutedEventArgs e)
        {
            displayImage.ManipulationMode = ManipulationModes.All;
        }

        private void MoveUnchecked(object sender, RoutedEventArgs e)
        {
            displayImage.ManipulationMode = ManipulationModes.None;
        }
        #endregion

        #region Manipulation Events

        private void ImagePointerReleased(object sender, PointerRoutedEventArgs e)
        {
            forceManipulationsToEnd = true;
        }

        private void InitManipulationTransforms()
        {
            _transformGroup = new TransformGroup();
            _compositeTransform = new CompositeTransform();
            _previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };

            _transformGroup.Children.Add(_previousTransform);
            _transformGroup.Children.Add(_compositeTransform);

            displayImage.RenderTransform = _transformGroup;
        }

        void ManipulateMe_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            forceManipulationsToEnd = false;
            e.Handled = true;
        }

        void ManipulateMe_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        void ManipulateMe_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.Handled = true;
        }


        void ManipulateMe_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (forceManipulationsToEnd)
            {
                e.Complete();
                return;
            }

            _previousTransform.Matrix = _transformGroup.Value;

            Point center = _previousTransform.TransformPoint(new Point(e.Position.X, e.Position.Y));
            _compositeTransform.CenterX = center.X;
            _compositeTransform.CenterY = center.Y;

            _compositeTransform.Rotation = e.Delta.Rotation;
            _compositeTransform.ScaleX = _compositeTransform.ScaleY = e.Delta.Scale;
            _compositeTransform.TranslateX = e.Delta.Translation.X;
            _compositeTransform.TranslateY = e.Delta.Translation.Y;

            e.Handled = true;
        }

        void ManipulateMe_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
        }
        #endregion

        #region Small Bitmaps for Filters
        private async void setFilterBitmaps()
        {
            Stream
            blackWhiteStream = null,
            emboss2Stream = null,
            embossStream = null,
            invertStream = null,
            blurStream = null,
            blur2Stream = null,
            sharpenStream = null,
            sharpenStream1 = null,
            retroStream = null,
            darkenStream = null,
            edgeDetectStream = null,
            edgeEnhanceStream = null,
            brightenStream = null,
            shadowStream = null,
            crystalStream = null;

            FilterFunctions filterimage = new FilterFunctions();

            WriteableBitmap
            blackWhiteBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            embossBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            emboss2Bitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            invertBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            blurBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            blur2Bitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            sharpenBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            sharpenBitmap1 = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            edgeDetectBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            edgeEnhanceBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            retroBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            darkenBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            brightenBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            shadowBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5)),
            crystalBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));

            blackWhiteFilter.Source = blackWhiteBitmap;
            embossFilter.Source = embossBitmap;
            emboss2Filter.Source = emboss2Bitmap;
            invertFilter.Source = invertBitmap;
            blurFilter.Source = blurBitmap;
            blur2Filter.Source = blur2Bitmap;
            sharpenFilter.Source = sharpenBitmap;
            sharpenFilter1.Source = sharpenBitmap1;
            edgeDetectFilter.Source = edgeDetectBitmap;
            edgeEnhanceFilter.Source = edgeEnhanceBitmap;
            retroFilter.Source = retroBitmap;
            darkenFilter.Source = darkenBitmap;
            brightenFilter.Source = brightenBitmap;
            shadowFilter.Source = shadowBitmap;
            crystalFilter.Source = crystalBitmap;

            blackWhiteStream = blackWhiteBitmap.PixelBuffer.AsStream();
            embossStream = embossBitmap.PixelBuffer.AsStream();
            emboss2Stream = emboss2Bitmap.PixelBuffer.AsStream();
            invertStream = invertBitmap.PixelBuffer.AsStream();
            blurStream = blurBitmap.PixelBuffer.AsStream();
            blur2Stream = blur2Bitmap.PixelBuffer.AsStream();
            sharpenStream = sharpenBitmap.PixelBuffer.AsStream();
            sharpenStream1 = sharpenBitmap1.PixelBuffer.AsStream();
            edgeDetectStream = edgeDetectBitmap.PixelBuffer.AsStream();
            edgeEnhanceStream = edgeEnhanceBitmap.PixelBuffer.AsStream();
            retroStream = retroBitmap.PixelBuffer.AsStream();
            darkenStream = darkenBitmap.PixelBuffer.AsStream();
            brightenStream = brightenBitmap.PixelBuffer.AsStream();
            shadowStream = shadowBitmap.PixelBuffer.AsStream();
            crystalStream = crystalBitmap.PixelBuffer.AsStream();

            initializeBitmap(blackWhiteStream, blackWhiteBitmap, filterimage);
            initializeBitmap(embossStream, embossBitmap, filterimage);
            initializeBitmap(emboss2Stream, emboss2Bitmap, filterimage);
            initializeBitmap(invertStream, invertBitmap, filterimage);
            initializeBitmap(blurStream, blurBitmap, filterimage);
            initializeBitmap(blur2Stream, blur2Bitmap, filterimage);
            initializeBitmap(sharpenStream, sharpenBitmap, filterimage);
            initializeBitmap(sharpenStream1, sharpenBitmap1, filterimage);
            initializeBitmap(edgeDetectStream, edgeDetectBitmap, filterimage);
            initializeBitmap(edgeEnhanceStream, edgeEnhanceBitmap, filterimage);
            initializeBitmap(retroStream, retroBitmap, filterimage);
            initializeBitmap(darkenStream, darkenBitmap, filterimage);
            initializeBitmap(brightenStream, brightenBitmap, filterimage);
            initializeBitmap(shadowStream, shadowBitmap, filterimage);
            initializeBitmap(crystalStream, crystalBitmap, filterimage);

            prepareImage(blackWhiteStream, blackWhiteBitmap, filterimage);
            setStream(blackWhiteStream, blackWhiteBitmap, filterimage);

            doFilter(blackWhiteStream, blackWhiteBitmap, filterimage, "blackwhite");
            doFilter(embossStream, embossBitmap, filterimage, "emboss");
            doFilter(emboss2Stream, emboss2Bitmap, filterimage, "emboss2");
            doFilter(invertStream, invertBitmap, filterimage, "invert");
            doFilter(blurStream, blurBitmap, filterimage, "blur");
            doFilter(blur2Stream, blur2Bitmap, filterimage, "blur2");
            doFilter(sharpenStream, sharpenBitmap, filterimage, "sharpen");
            doFilter(sharpenStream1, sharpenBitmap1, filterimage, "sharpen1");
            doFilter(edgeDetectStream, edgeDetectBitmap, filterimage, "EdgeDetect");
            doFilter(edgeEnhanceStream, edgeEnhanceBitmap, filterimage, "EdgeEnhance");
            doFilter(retroStream, retroBitmap, filterimage, "retro");
            doFilter(darkenStream, darkenBitmap, filterimage, "darken");
            doFilter(brightenStream, brightenBitmap, filterimage, "brighten");
            doFilter(shadowStream, shadowBitmap, filterimage, "shadow");
            doFilter(crystalStream, crystalBitmap, filterimage, "crystal");
        }

        private async void initializeBitmap(Stream givenStream, WriteableBitmap givenBitmap, FilterFunctions givenImage)
        {
            givenStream = givenBitmap.PixelBuffer.AsStream();
            givenImage.srcPixels = new byte[(uint)givenStream.Length];
            await givenStream.ReadAsync(givenImage.srcPixels, 0, givenImage.srcPixels.Length);
        }

        private void doFilter(Stream givenStream, WriteableBitmap givenBitmap, FilterFunctions givenImage, string filter)
        {
            switch (filter)
            {
                case "blackwhite":
                    doBlackWhite(givenStream, givenBitmap, givenImage);
                    break;
                case "invert":
                    doInvert(givenStream, givenBitmap, givenImage);
                    break;
                case "emboss":
                    doEmboss(givenStream, givenBitmap, givenImage);
                    break;
                case "emboss2":
                    doEmboss2(givenStream, givenBitmap, givenImage);
                    break;
                case "blur":
                    doBlur(givenStream, givenBitmap, givenImage);
                    break;
                case "blur2":
                    doBlur2(givenStream, givenBitmap, givenImage);
                    break;
                case "sharpen":
                    doSharpen(givenStream, givenBitmap, givenImage);
                    break;
                case "sharpen1":
                    doSharpen1(givenStream, givenBitmap, givenImage);
                    break;
                case "EdgeDetect":
                    doEdgeDetect(givenStream, givenBitmap, givenImage);
                    break;
                case "EdgeEnhance":
                    doEdgeEnhance(givenStream, givenBitmap, givenImage);
                    break;
                case "colorize":
                    doColorize(givenStream, givenBitmap, givenImage);
                    break;
                case "retro":
                    doRetro(givenStream, givenBitmap, givenImage);
                    break;
                case "darken":
                    doDarken(givenStream, givenBitmap, givenImage);
                    break;
                case "brighten":
                    doBrighten(givenStream, givenBitmap, givenImage);
                    break;
                case "shadow":
                    doShadow(givenStream, givenBitmap, givenImage);
                    break;
                case "crystal":
                    doCrystal(givenStream, givenBitmap, givenImage);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Filters Check Buttons
        private void blackWhiteChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "blackwhite";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void invertChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "invert";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void sharpenChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "sharpen";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void colorizeChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "colorize";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void retroChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "retro";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void darkenChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "darken";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void sharpenChecked1(object sender, RoutedEventArgs e)
        {
            appliedFilters = "sharpen1";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void embossChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "emboss";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void emboss2Checked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "emboss2";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void blurChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "blur";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void blur2Checked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "blur2";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void edgeDetectChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "EdgeDetect";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void edgeEnhanceChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "EdgeEnhance";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void brightenChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "brighten";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void shadowChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "shadow";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void crystalChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "crystal";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        private void filterUnchecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = null;
            deselectFilters();
        }

        private void deselectFilters()
        {
            String without = appliedFilters;
            if (without != "blackwhite")
                blackWhiteCheck.IsChecked = false;
            if (without != "invert")
                invertCheck.IsChecked = false;
            if (without != "sharpen")
                sharpenCheck.IsChecked = false;
            if (without != "sharpen1")
                sharpenCheck1.IsChecked = false;
            if (without != "emboss2")
                emboss2Check.IsChecked = false;
            if (without != "emboss")
                embossCheck.IsChecked = false;
            if (without != "EdgeDetect")
                edgeDetectCheck.IsChecked = false;
            if (without != "EdgeEnhance")
                edgeEnhanceCheck.IsChecked = false;
            if (without != "blur2")
                blur2Check.IsChecked = false;
            if (without != "blur")
                blurCheck.IsChecked = false;
            if (without != "retro")
                retroCheck.IsChecked = false;
            if (without != "darken")
                darkenCheck.IsChecked = false;
            if (without != "brighten")
                brightenCheck.IsChecked = false;
            if (without != "shadow")
                shadowCheck.IsChecked = false;
            if (without != "crystal")
                crystalCheck.IsChecked = false;
            if (appliedFilters == null)
                FilterApplyReset.Visibility = Visibility.Collapsed;
        }
        #endregion

        private void GridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            SelectColors.IsChecked = false;
            SelectFilters.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
        }

        private void OnImagePointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(displayImage).Properties.MouseWheelDelta;
            if (delta > 0)
            {
                scale.ScaleX = scale.ScaleX + 0.1;
                scale.ScaleY = scale.ScaleY + 0.1;
                if (ZoomOut.Visibility != Visibility.Visible)
                    ZoomOut.Visibility = Visibility.Visible;
            }
            else
            {
                if (scale.ScaleX > 1 && scale.ScaleY > 1)
                {
                    scale.ScaleX = scale.ScaleX - 0.1;
                    scale.ScaleY = scale.ScaleY - 0.1;
                }
                if (ZoomOut.Visibility == Visibility.Visible && scale.ScaleX <= 1)
                {
                    ZoomOut.Visibility = Visibility.Collapsed;
                }
            }
        }

        #region Image Options
        private async void SetLockPic_Clicked(object sender, RoutedEventArgs e)
        {
            await SaveFile(false);
            await LockScreen.SetImageFileAsync(file);
            MessageDialog messageDialog = new MessageDialog("Picture set! :)", "All done");
            await messageDialog.ShowAsync();
            await deleteUsedFile();
        }

        private async void SetAccountPic_Clicked(object sender, RoutedEventArgs e)
        {
            await SaveFile(false);
            SetAccountPictureResult result = await UserInformation.SetAccountPicturesAsync(null, file, null);

            if (result == SetAccountPictureResult.Success)
            {
                MessageDialog messageDialog = new MessageDialog("Picture set! :)", "All done");
                await messageDialog.ShowAsync();
                await deleteUsedFile();
            }
            else
            {
                MessageDialog messageDialog = new MessageDialog("Something failed :(", "Close");
                await messageDialog.ShowAsync();
            }
        }

        private void ReturnOriginal_Clicked(object sender, RoutedEventArgs e)
        {
            RestoreOriginalBitmap();
        }

        private void RestoreOriginalBitmap()
        {
            imageOriginal.srcPixels = (byte[])uneditedImage.srcPixels.Clone();
            imageOriginal.dstPixels = (byte[])uneditedImage.dstPixels.Clone();
            bitmapStream = uneditedStream;
            bitmapImage = uneditedBitmap;
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            setStream(bitmapStream, bitmapImage, imageOriginal);
            displayImage.Source = bitmapImage;
            setFilterBitmaps();
            resetInterface();
            this.selectedRegion.ResetCorner(0, 0, displayImage.ActualWidth, displayImage.ActualHeight);
            this.selectedRegion.OuterRect = Rect.Empty;
        }

        #endregion

        private async Task deleteUsedFile()
        {
            if (imageOriginal.dstPixels != null)
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("temp.jpg");
                await file.DeleteAsync();
            }
        }

        #region Colorize
        private void blueSquareTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedColorize = "blue";
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            doColorize(exampleStream, exampleBitmap, image);
        }

        private void redSquareTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedColorize = "red";
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            doColorize(exampleStream, exampleBitmap, image);
        }

        private void greenSquareTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedColorize = "green";
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            doColorize(exampleStream, exampleBitmap, image);
        }

        private void deselectColorizeListItems()
        {
            blueColorize.IsSelected = false;
            redColorize.IsSelected = false;
            greenColorize.IsSelected = false;
        }
        #endregion

        private void HistogramClicked(object sender, RoutedEventArgs e)
        {
            SelectHistogram.IsChecked = false;
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.MakeHistogramEqualization();
            setStream(bitmapStream, bitmapImage, imageOriginal);
            prepareImage(exampleStream, exampleBitmap, image);
            image.MakeHistogramEqualization();
            setStream(exampleStream, exampleBitmap, image);
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            setFilterBitmaps();
        }

        #region Crop region
        #region Select Region methods

        /// <summary>
        /// If a pointer presses in the corner, it means that the user starts to move the corner.
        /// 1. Capture the pointer, so that the UIElement can get the Pointer events (PointerMoved,
        ///    PointerReleased...) even the pointer is outside of the UIElement.
        /// 2. Record the start position of the move.
        /// </summary>
        private void Corner_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).CapturePointer(e.Pointer);

            Windows.UI.Input.PointerPoint pt = e.GetCurrentPoint(this);

            // Record the start point of the pointer.
            pointerPositionHistory[pt.PointerId] = pt.Position;

            e.Handled = true;
        }

        /// <summary>
        /// If a pointer which is captured by the corner moves，the select region will be updated.
        /// </summary>
        void Corner_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint pt = e.GetCurrentPoint(this);
            uint ptrId = pt.PointerId;

            if (pointerPositionHistory.ContainsKey(ptrId) && pointerPositionHistory[ptrId].HasValue)
            {
                Point currentPosition = pt.Position;
                Point previousPosition = pointerPositionHistory[ptrId].Value;

                double xUpdate = currentPosition.X - previousPosition.X;
                double yUpdate = currentPosition.Y - previousPosition.Y;

                this.selectedRegion.UpdateCorner((sender as ContentControl).Tag as string, xUpdate, yUpdate);

                pointerPositionHistory[ptrId] = currentPosition;
            }

            e.Handled = true;
        }

        /// <summary>
        /// The pressed pointer is released, which means that the move is ended.
        /// 1. Release the Pointer.
        /// 2. Clear the position history of the Pointer.
        /// </summary>
        private void Corner_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            uint ptrId = e.GetCurrentPoint(this).PointerId;
            if (this.pointerPositionHistory.ContainsKey(ptrId))
            {
                this.pointerPositionHistory.Remove(ptrId);
            }

            (sender as UIElement).ReleasePointerCapture(e.Pointer);

            CropApply.Visibility = Visibility.Visible;
            //UpdatePreviewImage();
            e.Handled = true;


        }

        void selectRegion_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            this.selectedRegion.UpdateSelectedRect(e.Delta.Scale, e.Delta.Translation.X, e.Delta.Translation.Y);
            e.Handled = true;
        }

        void selectRegion_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            //UpdatePreviewImage();
            CropApply.Visibility = Visibility.Visible;
        }

        #endregion

        private void CropChecked(object sender, RoutedEventArgs e)
        {
            CropPanel.Visibility = Visibility.Visible;
            imageCanvas.Visibility = Visibility.Visible;
            displayGrid.Margin = new Thickness(15);
            ResetZoomPos();
        }

        private void CropUnchecked(object sender, RoutedEventArgs e)
        {
            CropPanel.Visibility = Visibility.Collapsed;
            imageCanvas.Visibility = Visibility.Collapsed;
            this.selectedRegion.ResetCorner(0, 0, displayImage.ActualWidth, displayImage.ActualHeight);
            this.selectedRegion.OuterRect = Rect.Empty;
            displayGrid.Margin = new Thickness(0);
        }

        async void UpdatePreviewImage()
        {
            ImageLoadingRing.IsActive = true;
            await SaveFile(false);

            double sourceImageWidthScale = imageCanvas.Width / this.sourceImagePixelWidth;
            double sourceImageHeightScale = imageCanvas.Height / this.sourceImagePixelHeight;

            Size previewImageSize = new Size(
                this.selectedRegion.SelectedRect.Width / sourceImageWidthScale,
                this.selectedRegion.SelectedRect.Height / sourceImageHeightScale);

            double previewImageScale = 1;

            if (previewImageSize.Width <= imageCanvas.Width &&
                previewImageSize.Height <= imageCanvas.Height)
            {
                displayImage.Stretch = Windows.UI.Xaml.Media.Stretch.None;
            }
            else
            {
                displayImage.Stretch = Windows.UI.Xaml.Media.Stretch.Uniform;

                previewImageScale = Math.Min(imageCanvas.Width / previewImageSize.Width,
                    imageCanvas.Height / previewImageSize.Height);
            }

            bitmapImage = await CropBitmap.GetCroppedBitmapAsync(
                   file,
                   new Point(this.selectedRegion.SelectedRect.X / sourceImageWidthScale, this.selectedRegion.SelectedRect.Y / sourceImageHeightScale),
                   previewImageSize,
                   previewImageScale);

            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);

            exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));
            prepareImage(exampleStream, exampleBitmap, image);
            setStream(exampleStream, exampleBitmap, image);
            setElements(ColorsExamplePicture, exampleBitmap);
            setElements(RotationsExamplePicture, exampleBitmap);

            setFilterBitmaps();

            SelectCrop.IsChecked = false;

            sourceImagePixelHeight = (uint)bitmapImage.PixelHeight;
            sourceImagePixelWidth = (uint)bitmapImage.PixelWidth;

            ImageLoadingRing.IsActive = false;
            displayImage.Source = bitmapImage;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            selectedRegion.PropertyChanged += selectedRegion_PropertyChanged;

            // Handle the pointer events of the corners. 
            AddCornerEvents(topLeftCorner);
            AddCornerEvents(topRightCorner);
            AddCornerEvents(bottomLeftCorner);
            AddCornerEvents(bottomRightCorner);

            // Handle the manipulation events of the selectRegion
            selectRegion.ManipulationDelta += selectRegion_ManipulationDelta;
            selectRegion.ManipulationCompleted += selectRegion_ManipulationCompleted;

            this.displayImage.SizeChanged += sourceImage_SizeChanged;

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            selectedRegion.PropertyChanged -= selectedRegion_PropertyChanged;

            // Handle the pointer events of the corners. 
            RemoveCornerEvents(topLeftCorner);
            RemoveCornerEvents(topRightCorner);
            RemoveCornerEvents(bottomLeftCorner);
            RemoveCornerEvents(bottomRightCorner);

            // Handle the manipulation events of the selectRegion
            selectRegion.ManipulationDelta -= selectRegion_ManipulationDelta;
            selectRegion.ManipulationCompleted -= selectRegion_ManipulationCompleted;

            this.displayImage.SizeChanged -= sourceImage_SizeChanged;

        }

        private void AddCornerEvents(Control corner)
        {
            corner.PointerPressed += Corner_PointerPressed;
            corner.PointerMoved += Corner_PointerMoved;
            corner.PointerReleased += Corner_PointerReleased;
        }

        private void RemoveCornerEvents(Control corner)
        {
            corner.PointerPressed -= Corner_PointerPressed;
            corner.PointerMoved -= Corner_PointerMoved;
            corner.PointerReleased -= Corner_PointerReleased;
        }

        void sourceImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            if (e.NewSize.IsEmpty || double.IsNaN(e.NewSize.Height) || e.NewSize.Height <= 0)
            {
                this.imageCanvas.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                this.selectedRegion.OuterRect = Rect.Empty;
                this.selectedRegion.ResetCorner(0, 0, 0, 0);
            }
            else
            {
                this.imageCanvas.Height = e.NewSize.Height;
                this.imageCanvas.Width = e.NewSize.Width;
                this.selectedRegion.OuterRect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);

                if (e.PreviousSize.IsEmpty || double.IsNaN(e.PreviousSize.Height) || e.PreviousSize.Height <= 0)
                {
                    this.selectedRegion.ResetCorner(0, 0, e.NewSize.Width, e.NewSize.Height);
                }
                else
                {
                    double scale = e.NewSize.Height / e.PreviousSize.Height;
                    this.selectedRegion.ResizeSelectedRect(scale);
                }

            }
        }

        void selectedRegion_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            double widthScale = imageCanvas.Width / sourceImagePixelWidth;
            double heightScale = imageCanvas.Height / sourceImagePixelHeight;

            this.selectInfoInBitmapText.Text = string.Format("Resolution: {0}x{1}",
                Math.Floor(this.selectedRegion.SelectedRect.Width / widthScale),
                Math.Floor(this.selectedRegion.SelectedRect.Height / heightScale));
        }

        private void saveImageButton_Click(object sender, RoutedEventArgs e)
        {
            UpdatePreviewImage();
        }
        #endregion

        private void deselectMenu()
        {
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            SelectFilters.IsChecked = false;
            SelectCrop.IsChecked = false;
        }

        private void ResizeSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                int value = (int)ResizeSlider.Value;
                switch (value)
                {
                    case 1:
                        NewResolution.Text = string.Format("New Resolution: {0}x{1}", bitmapImage.PixelWidth / 4, bitmapImage.PixelHeight / 4);
                        break;
                    case 2:
                        NewResolution.Text = string.Format("New Resolution: {0}x{1}", bitmapImage.PixelWidth / 3, bitmapImage.PixelHeight / 3);
                        break;
                    case 3:
                        NewResolution.Text = string.Format("New Resolution: {0}x{1}", bitmapImage.PixelWidth / 2, bitmapImage.PixelHeight / 2);
                        break;
                    case 4:
                        NewResolution.Text = string.Format("New Resolution: {0}x{1}", bitmapImage.PixelWidth, bitmapImage.PixelHeight);
                        break;
                    default:
                        break;
                }
            }
        }

        private void Resize_Checked(object sender, RoutedEventArgs e)
        {
            ResizePanel.Visibility = Visibility.Visible;
        }

        private void Resize_Unchecked(object sender, RoutedEventArgs e)
        {
            ResizePanel.Visibility = Visibility.Collapsed;
        }

        private async void ApplyResize_Clicked(object sender, RoutedEventArgs e)
        {
            int resizeValue = (int)ResizeSlider.Value;
            switch (resizeValue)
            {
                case 1:
                    resizeValue = 4;
                    break;
                case 2:
                    resizeValue = 3;
                    break;
                case 3:
                    resizeValue = 2;
                    break;
                default:
                    break;
            }

            bitmapImage = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / resizeValue), (uint)(bitmapImage.PixelHeight / resizeValue));
            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            image.srcPixels = new byte[(uint)exampleStream.Length];
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            setStream(bitmapStream, bitmapImage, imageOriginal);
            displayImage.Source = bitmapImage;
            doAllCalculations();
        }


    }
    #endregion
}
#endregion