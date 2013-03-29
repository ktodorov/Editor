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
using RemedyPic.RemedyClasses;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.ApplicationSettings;
using Windows.Media.Capture;
using Windows.System.UserProfile;
using System.Diagnostics;
using System.Text.RegularExpressions;
using RemedyPic.UserControls;
using RemedyPic.UserControls.Popups;

#region Namespace RemedyPic
namespace RemedyPic
{
    #region MainPage class
    public sealed partial class MainPage
    {


        #region Variables

        // Those are all the global variables, that are used in MainPage.xaml.cs file.

		public static MainPage Current;

        // Undo Redo archive
        public List<byte[]> archive_data = new List<byte[]>();
        public int archive_current_index = -1;     // -1 because we don`t have saved pixel array

        // mruToken is used for LoadState and SaveState functions.
        public string mruToken = null;

        // Those store the corner positions of 
        // the canvas, used for croping.
        public double canvasStartX = 0.00;
        public double canvasStartY = 0.00;
        public double canvasEndX = 0.00;
        public double canvasEndY = 0.00;

        // This variable holds the current file that we are using.
        public StorageFile file;

        // String variables that hold the current applied changes to the image.

        // We create two WriteableBitmap variables.
        // One for the original image and one for the small bitmaps.
        // They are used to display the image on the screen.
        public WriteableBitmap bitmapImage, exampleBitmap;

        // The streams are used to save the pixels as a Stream to the WriteableBitmap objects.
        public Stream bitmapStream, exampleStream;

        // This is set true when the user opens a picture.
        public bool pictureIsLoaded = false;

        // We create three RemedyImages.
        // One for the original displayed image, one for the small images and
        // one to hold the original loaded image so we can get back to it at any time
        public RemedyImage image = new RemedyImage();
        public RemedyImage imageOriginal = new RemedyImage();
        public RemedyImage uneditedImage = new RemedyImage();

        // We create two streams for two of the WriteableBitmap objects.
        public Stream uneditedStream;
        public WriteableBitmap uneditedBitmap;

        // The dictionary holds the history of all previous pointer locations. It is used by the crop function.
        Dictionary<uint, Point?> pointerPositionHistory = new Dictionary<uint, Point?>();

        // This bool variable checks if the user 
        // has made any changes and if he saved them.
        public bool Saved = true;
        public string PopupCalledBy = "";

		public MenuPopup Menu;
        public MainOptionsPanel Panel;
        public DisplayImage imageDisplayed;

        public RemedyColors ColorsPopup;
        public RemedyFilters FiltersPopup;
        public RemedyExposure ExposurePopup;
        public RemedyRotations RotatePopup;
        public RemedyColorize ColorizePopup;
        public RemedyFrames FramesPopup;
		public RemedyCustom CustomPopup;
		public RemedyHistogram HistogramPopup;
		public RemedyOptions OptionsPopup;

        #endregion

        public MainPage()
        {
            // This function is called when the page is loaded in the beginning.
            // We first initialize the interface, then drop the picture border out
            // so we can animate it later. 
            // Then the charms are registered for using later. 
            // After this, the events are generated for the image.
            // They are used later for the image panning and crop function.
            // Finally, we set the selected crop region width and height.
            this.InitializeComponent();
			
            Current = this;

            RegisterInterfaceElements();

            RegisterCharms();
        }

        private void RegisterInterfaceElements()
        {
            Menu = new MenuPopup();
            Panel = new MainOptionsPanel();

            double displayImageWidth = (Window.Current.Bounds.Width * (569.00 / 683.00)) * 0.97;
            double displayImageHeight = (Window.Current.Bounds.Height - 70);

            imageDisplayed = new DisplayImage(displayImageWidth, displayImageHeight);

            MainMenuPanel.Children.Add(Menu);
            PanelStack.Children.Add(Panel);
            ImageStack.Children.Add(imageDisplayed);

            ColorsPopup = new RemedyColors();
            FiltersPopup = new RemedyFilters();
            ExposurePopup = new RemedyExposure();
            RotatePopup = new RemedyRotations();
            ColorizePopup = new RemedyColorize();
            FramesPopup = new RemedyFrames();
			CustomPopup = new RemedyCustom();
			HistogramPopup = new RemedyHistogram();
			OptionsPopup = new RemedyOptions();

            setPopupsHeight();

            SmallPopups.Children.Add(ExposurePopup);
            SmallPopups.Children.Add(ColorsPopup);
            contentGrid.Children.Add(FiltersPopup);
            SmallPopups.Children.Add(RotatePopup);
            SmallPopups.Children.Add(ColorizePopup);
            SmallPopups.Children.Add(FramesPopup);
			SmallPopups.Children.Add(CustomPopup);
			SmallPopups.Children.Add(HistogramPopup);
			SmallPopups.Children.Add(OptionsPopup);
        }


        #region Charms
        public void RegisterCharms()
        {
            // If the user chooses the charm, we register the share charm 
            // and the settings charm so we can get all the available apps for sharing.
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>(this.ShareImageHandler);
            SettingsPane.GetForCurrentView().CommandsRequested += OnSettingsPaneCommandRequested;
        }

		public void ShowPopup(string Popup)
		{
			ColorsPopup.Popup.IsOpen = true;
		}

        public void OnSettingsPaneCommandRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // We add the Feedback settings to the Settings charm.
            args.Request.ApplicationCommands.Add(new SettingsCommand("commandID",
                                                                     "Feedback", FeedbackPopup));
        }

        public void FeedbackPopup(IUICommand command)
        {
            // This event occures when the user clicks on the Feedback in the settings charm.
            Feedback.IsOpen = true;
        }

        public async void ShareImageHandler(DataTransferManager sender,
            DataRequestedEventArgs e)
        {
            // This handles the Share charm.

            if (!pictureIsLoaded)
            {
                // First we check if the user has loaded an image. If not, warn him.
                e.Request.FailWithDisplayText("Load an image and try sharing again! :)");
            }
            else
            {
                // If the user has loaded the image, we set the title, the description
                // and set RemedyPic as application name.
                DataRequest request = e.Request;
                request.Data.Properties.Title = "RemedyPic";
                request.Data.Properties.Description = "Share your image.";
                request.Data.Properties.ApplicationName = "RemedyPic";

                // Because we are making async calls in the DataRequested event handler,
                // we need to get the deferral first.
                DataRequestDeferral deferral = request.GetDeferral();

                // Make sure we always call Complete on the deferral.
                try
                {
                    // We save the current edited image to a temporary file in the application local folder on the current machine.
                    // This way, we can add more share handlers and use the Mail Share option.
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
            // This loads information if the app had quit unexpected.
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
                            imageDisplayed.displayImage.Source = bitmapImage;

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
            // This saves information if the app quits unexpected.
            if (!String.IsNullOrEmpty(mruToken))
            {
                pageState["mruToken"] = mruToken;
            }
        }
        #endregion

        #region Functions, called when opening an image.


        public async void doAllCalculations()
        {
            // We make all the required calculations in order for
            // the app elements to appear and work normal.
            uneditedBitmap = bitmapImage;

            // Resize the original image for faster work.
            // Note that we only set the resize to the small images.
            // The original big image is left in original resolution.
            // After this we get the image pixels as streams and then
            // write the streams to the RemedyImage arrays.
            exampleBitmap = await OptionsPopup.ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));
            exampleStream = exampleBitmap.PixelBuffer.AsStream();
            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            uneditedStream = uneditedBitmap.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            image.srcPixels = new byte[(uint)exampleStream.Length];
            uneditedImage.srcPixels = new byte[(uint)uneditedStream.Length];
            await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
            await uneditedStream.ReadAsync(uneditedImage.srcPixels, 0, uneditedImage.srcPixels.Length);

            // Reset all sliders
            ResetAllSliders();

            // Reset archive and archive index and add new image
            archive_data.Clear();
            archive_current_index = -1;
            Panel.ArchiveAddArray();

            // Clear array with effects
            OptionsPopup.effectsApplied.Clear();

            // Set the small WriteableBitmap objects to the three XAML Image objects.
            setElements(ColorsPopup.ColorsExamplePicture, exampleBitmap);
            setElements(RotatePopup.RotationsExamplePicture, exampleBitmap);
            setElements(ExposurePopup.ExposureExamplePicture, exampleBitmap);

            // Make the images ready for work.
            prepareImage(exampleStream, exampleBitmap, image);
            setStream(exampleStream, exampleBitmap, image);
            prepareImage(uneditedStream, uneditedBitmap, uneditedImage);
            setStream(uneditedStream, uneditedBitmap, uneditedImage);
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            setStream(bitmapStream, bitmapImage, imageOriginal);

            Panel.ZoomStack.Visibility = Visibility.Visible;

            // set the small WriteableBitmap objects to the filter buttons.
            FiltersPopup.setFilterBitmaps();

            // Display the file name.
            setFileProperties(file);

            // Set the WriteableBitmap as source to the XAML Image object. This makes the picture appear on the screen.
            imageDisplayed.displayImage.Source = bitmapImage;
            imageDisplayed.AnimateInPicture.Begin();

            // We check the CheckBox that is required for the image to move by default.
            Panel.ImageMoving.IsChecked = true;

            // We set the imagePanel maximum height so the image not to go out of the screen
            imageDisplayed.displayImage.MaxWidth = imageDisplayed.imageBorder.ActualWidth * 0.90;
            imageDisplayed.displayImage.MaxHeight = imageDisplayed.imageBorder.ActualHeight * 0.90;



			OptionsPopup.widthHeightRatio = (double)bitmapImage.PixelWidth / (double)bitmapImage.PixelHeight;
			OptionsPopup.newWidth.Text = bitmapImage.PixelWidth.ToString();
			OptionsPopup.newHeight.Text = bitmapImage.PixelHeight.ToString();

            // Show the interface.
            showInterface();
        }

        public void ResetAllSliders()
        {
            ResetFilterMenuData();
            ColorsPopup.ResetColorMenuData();
            ExposurePopup.ResetExposureMenuData();
            RotatePopup.ResetRotateMenuData();
            ColorizePopup.ResetColorizeMenuData();
            FramesPopup.ResetFrameMenuData();
        }

        // Reset data of Filter menu
        public void ResetFilterMenuData()
        {
            FiltersPopup.appliedFilters = null;
            FiltersPopup.deselectFilters();
        }


        public void setPopupsHeight()
        {
            // We set the popups height to match the current machine's resolution
            contentGrid.Height = Window.Current.Bounds.Height;
            SmallPopups.Height = Window.Current.Bounds.Height;
            ColorsPopup.Colors.Height = Window.Current.Bounds.Height;
            FiltersPopup.Filters.Height = Window.Current.Bounds.Height;
            ExposurePopup.Exposure.Height = Window.Current.Bounds.Height;
            RotatePopup.Rotations.Height = Window.Current.Bounds.Height;
            OptionsPopup.ImageOptions.Height = Window.Current.Bounds.Height;
            ColorizePopup.Colorize.Height = Window.Current.Bounds.Height;
            FramesPopup.Frames.Height = Window.Current.Bounds.Height;
            HistogramPopup.Histogram.Height = Window.Current.Bounds.Height;
            FeedbackGrid.Height = Window.Current.Bounds.Height;
            CustomPopup.CustomFilter.Height = Window.Current.Bounds.Height;
            notSaved.Width = Window.Current.Bounds.Width;
            notSavedGrid.Width = Window.Current.Bounds.Width;
        }

        public void setElements(Windows.UI.Xaml.Controls.Image imageElement, WriteableBitmap source)
        {
            // We set the XAML Image object a bitmap as a source 
            // and then set the width and height to be proportional to the actual bitmap
            imageElement.Source = source;
            imageElement.Width = bitmapImage.PixelWidth / 4;
            imageElement.Height = bitmapImage.PixelHeight / 4;
        }

        public void showInterface()
        {
            // Called when the image is loaded.
            // It shows the interface.
            Panel.Zoom.Visibility = Visibility.Visible;
            Menu.menuPopup.IsOpen = true;
            Panel.UndoRedo.Visibility = Visibility.Visible;
        }
        #endregion

       

        #region Save

        public async Task<bool> SaveFile(bool picker)
        {
            // Only execute if there is a picture that is loaded
            if (pictureIsLoaded)
            {
                file = null;
                ImageLoadingRing.IsActive = true;
                // If the picker variable is true, we call a FilePicker.
                // If it's not, we save a temporary file without notifying the user to the local directory of the app.
                if (picker == true)
                {
                    FileSavePicker savePicker = new FileSavePicker();
                    Saved = true;
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
            ImageLoadingRing.IsActive = false;
            return true;
        }
        #endregion

        public void pauseTimer(int miliseconds)
        {
            // This pauses the calling function for N miliseconds.
            Stopwatch sw = new Stopwatch(); // sw cotructor
            sw.Start(); // starts the stopwatch
            for (int i = 0; ; i++)
            {
                if (i % 100000 == 0) // if in 100000th iteration (could be any other large number
                // depending on how often you want the time to be checked) 
                {
                    sw.Stop(); // stop the time measurement
                    if (sw.ElapsedMilliseconds > miliseconds) // check if desired period of time has elapsed
                    {
                        break; // if more than the given milliseconds have passed, stop looping and return
                        // to the existing code
                    }
                    else
                    {
                        sw.Start(); // if less than the given milliseconds have elapsed, continue looping
                        // and resume time measurement
                    }
                }
            }
        }

        void setFileProperties(Windows.Storage.StorageFile file)
        {
            // This sets the file name to the text box
            fileName.Text = file.Name;
            if (fileName.Text.Length > 20)
                fileName.FontSize = 55;
            if (fileName.Text.Length > 50)
                fileName.FontSize = 35;
            if (fileName.Text.Length < 15)
                fileName.FontSize = 85;
        }

        public void setStream(Stream givenStream, WriteableBitmap givenBitmap, RemedyImage givenImage)
        {
            // This sets the pixels to the bitmap
            // and makes the ApplyReset stackPanel of the current popup to appear.
            givenStream.Seek(0, SeekOrigin.Begin);
            givenStream.Write(givenImage.dstPixels, 0, givenImage.dstPixels.Length);
            givenBitmap.Invalidate();
            if (givenImage == image)
            {
                if (FiltersPopup.Popup.IsOpen)
                    FiltersPopup.FilterApplyReset.Visibility = Visibility.Visible;
                else if (ColorsPopup.Popup.IsOpen)
                    ColorsPopup.ColorApplyReset.Visibility = Visibility.Visible;
                else if (RotatePopup.Popup.IsOpen)
                    RotatePopup.RotateApplyReset.Visibility = Visibility.Visible;
                else if (ExposurePopup.Popup.IsOpen)
                    ExposurePopup.ExposureApplyReset.Visibility = Visibility.Visible;
            }
        }

        public void prepareImage(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            // This calculates the width and height of the bitmap image
            // and sets the Stream and the pixels byte array
            givenImage.width = (int)bitmap.PixelWidth;
            givenImage.height = (int)bitmap.PixelHeight;
            stream = bitmap.PixelBuffer.AsStream();
            givenImage.dstPixels = new byte[4 * bitmap.PixelWidth * bitmap.PixelHeight];
            givenImage.Reset();
        }

        #region Back buttons

        public void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            // If the back popup button is clicked, close all popups.
            Menu.SelectColors.IsChecked = false;
            Menu.SelectFilters.IsChecked = false;
            Menu.SelectRotations.IsChecked = false;
            Menu.SelectOptions.IsChecked = false;
            Menu.SelectColorize.IsChecked = false;
            Menu.SelectFrames.IsChecked = false;
            Menu.SelectHistogram.IsChecked = false;
            Menu.SelectExposure.IsChecked = false;
            Menu.SelectCustom.IsChecked = false;
        }

        public void BackFeedbackClicked(object sender, RoutedEventArgs e)
        {
            // If the back feedback button is clicked, close the feedback and show the settings charm.
            Feedback.IsOpen = false;
            SettingsPane.Show();
        }
        #endregion

 
        public void ImagePointerReleased(object sender, PointerRoutedEventArgs e)
        {
            imageDisplayed.forceManipulationsToEnd = true;
        }

        public void GridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // A simple "hot key".
            // When the user double-clicks on the interface, 
            // the currently opened popup closes.
            Menu.deselectPopups();
        }

        public async Task deleteUsedFile()
        {
            // Deletes the temporary created file.
            if (imageOriginal.dstPixels != null)
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("temp.jpg");
                await file.DeleteAsync();
            }
        }


        #region Crop region

        #region Select Region methods

        public void Corner_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // If a pointer presses in the corner, it means that the user starts to move the corner.
            // 1. Capture the pointer, so that the UIElement can get the Pointer events (PointerMoved,
            //    PointerReleased...) even the pointer is outside of the UIElement.
            // 2. Record the start position of the move.
            (sender as UIElement).CapturePointer(e.Pointer);

            Windows.UI.Input.PointerPoint pt = e.GetCurrentPoint(this);

            // Record the start point of the pointer.
            pointerPositionHistory[pt.PointerId] = pt.Position;

            e.Handled = true;
        }

        void Corner_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // If a pointer which is captured by the corner moves，the select region will be updated.
            Windows.UI.Input.PointerPoint pt = e.GetCurrentPoint(this);
            uint ptrId = pt.PointerId;
            calculateCanvasCorners();

            if (pointerPositionHistory.ContainsKey(ptrId) && pointerPositionHistory[ptrId].HasValue)
            {

                Point currentPosition = pt.Position;
                Point previousPosition = pointerPositionHistory[ptrId].Value;

                double xUpdate = 0.0;
                double yUpdate = 0.0;

                // Those scary if's check the new position so the user 
                // can't expand the crop region if the pointer is out of the image.             
                if ((currentPosition.X > canvasStartX && currentPosition.X < canvasEndX)
                    || (currentPosition.X > previousPosition.X && currentPosition.X > canvasEndX)
                    || (currentPosition.X < previousPosition.X && currentPosition.X < canvasStartX))
                {
                    xUpdate = currentPosition.X - previousPosition.X;
                }
                else
                {
                    xUpdate = 0.0;
                }
                if ((currentPosition.Y > canvasStartY && currentPosition.Y < canvasEndY)
                    || (currentPosition.Y > previousPosition.Y && currentPosition.Y > canvasEndY)
                    || (currentPosition.Y < previousPosition.Y && currentPosition.Y < canvasStartY))
                {
                    yUpdate = currentPosition.Y - previousPosition.Y;
                }
                else
                {
                    yUpdate = 0.0;
                }

                imageDisplayed.selectedRegion.UpdateCorner((sender as ContentControl).Tag as string, xUpdate, yUpdate);

                pointerPositionHistory[ptrId] = currentPosition;

            }
            e.Handled = true;
        }

        public void Corner_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // The pressed pointer is released, which means that the move is ended.
            // 1. Release the Pointer.
            // 2. Clear the position history of the Pointer.
            uint ptrId = e.GetCurrentPoint(this).PointerId;
            if (this.pointerPositionHistory.ContainsKey(ptrId))
            {
                this.pointerPositionHistory.Remove(ptrId);
            }

            (sender as UIElement).ReleasePointerCapture(e.Pointer);

            e.Handled = true;


        }

        void selectRegion_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            imageDisplayed.selectedRegion.UpdateSelectedRect(e.Delta.Scale, e.Delta.Translation.X, e.Delta.Translation.Y);
            e.Handled = true;
        }

        #endregion



        public async void UpdatePreviewImage()
        {
            // Updates the current image with the new cropped one.
            ImageLoadingRing.IsActive = true;
            await SaveFile(false);

            double sourceImageWidthScale = imageDisplayed.imageCanvas.Width / imageDisplayed.sourceImagePixelWidth;
            double sourceImageHeightScale = imageDisplayed.imageCanvas.Height / imageDisplayed.sourceImagePixelHeight;

            Size previewImageSize = new Size(
                imageDisplayed.selectedRegion.SelectedRect.Width / sourceImageWidthScale,
                imageDisplayed.selectedRegion.SelectedRect.Height / sourceImageHeightScale);

            int OrignalWidth = imageOriginal.width;
            int OriginalHeight = imageOriginal.height;
            
            if (previewImageSize.Width <=imageDisplayed.imageCanvas.Width &&
                previewImageSize.Height <=imageDisplayed.imageCanvas.Height)
            {
                imageDisplayed.displayImage.Stretch = Windows.UI.Xaml.Media.Stretch.None;
            }
            else
            {
                imageDisplayed.displayImage.Stretch = Windows.UI.Xaml.Media.Stretch.Uniform;
            }

            bitmapImage = await CropBitmap.GetCroppedBitmapAsync(
                   file,
                   new Point(imageDisplayed.selectedRegion.SelectedRect.X / sourceImageWidthScale, imageDisplayed.selectedRegion.SelectedRect.Y / sourceImageHeightScale),
                   previewImageSize,
                   1);

            // After the cropping is done, we set the new bitmapImage objects again.
            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
            imageOriginal.Reset();

            setExampleBitmaps();
            FiltersPopup.setFilterBitmaps();

            Menu.SelectCrop.IsChecked = false;

            imageDisplayed.sourceImagePixelHeight = (uint)bitmapImage.PixelHeight;
            imageDisplayed.sourceImagePixelWidth = (uint)bitmapImage.PixelWidth;

			Panel.ArchiveAddArray();
			OptionsPopup.effectsApplied.Add("Crop " + OrignalWidth + " " + OriginalHeight + " " + (int)previewImageSize.Width + " " + (int)previewImageSize.Height);
            ImageLoadingRing.IsActive = false;
            imageDisplayed.displayImage.Source = bitmapImage;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Called when the current page is displayed.
            base.OnNavigatedTo(e);

           imageDisplayed.selectedRegion.PropertyChanged += selectedRegion_PropertyChanged;

            // Handle the pointer events of the corners. 
            AddCornerEvents(imageDisplayed.topLeftCorner);
            AddCornerEvents(imageDisplayed.topRightCorner);
            AddCornerEvents(imageDisplayed.bottomLeftCorner);
            AddCornerEvents(imageDisplayed.bottomRightCorner);

            // Handle the manipulation events of the selectRegion
            imageDisplayed.selectRegion.ManipulationDelta += selectRegion_ManipulationDelta;

            imageDisplayed.displayImage.SizeChanged += sourceImage_SizeChanged;

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Called when the current page is removed.
            base.OnNavigatedFrom(e);

            imageDisplayed.selectedRegion.PropertyChanged -= selectedRegion_PropertyChanged;

            // Handle the pointer events of the corners. 
            RemoveCornerEvents(imageDisplayed.topLeftCorner);
            RemoveCornerEvents(imageDisplayed.topRightCorner);
            RemoveCornerEvents(imageDisplayed.bottomLeftCorner);
            RemoveCornerEvents(imageDisplayed.bottomRightCorner);

            // Handle the manipulation events of the selectRegion
            imageDisplayed.selectRegion.ManipulationDelta -= selectRegion_ManipulationDelta;

            imageDisplayed.displayImage.SizeChanged -= sourceImage_SizeChanged;

        }

        public void AddCornerEvents(Control corner)
        {
            corner.PointerPressed += Corner_PointerPressed;
            corner.PointerMoved += Corner_PointerMoved;
            corner.PointerReleased += Corner_PointerReleased;
        }

        public void RemoveCornerEvents(Control corner)
        {
            corner.PointerPressed -= Corner_PointerPressed;
            corner.PointerMoved -= Corner_PointerMoved;
            corner.PointerReleased -= Corner_PointerReleased;
        }

        void sourceImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Called when the original image size is changed.
            // It calculates the new width and height.

            if (e.NewSize.IsEmpty || double.IsNaN(e.NewSize.Height) || e.NewSize.Height <= 0)
            {
                imageDisplayed.imageCanvas.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                imageDisplayed.selectedRegion.OuterRect = Rect.Empty;
                imageDisplayed.selectedRegion.ResetCorner(0, 0, 0, 0);
            }
            else
            {
                imageDisplayed.imageCanvas.Height = e.NewSize.Height;
                imageDisplayed.imageCanvas.Width = e.NewSize.Width;
                imageDisplayed.selectedRegion.OuterRect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);

                if (e.PreviousSize.IsEmpty || double.IsNaN(e.PreviousSize.Height) || e.PreviousSize.Height <= 0)
                {
                    imageDisplayed.selectedRegion.ResetCorner(0, 0, e.NewSize.Width, e.NewSize.Height);
                }
                else
                {
                    double scale = e.NewSize.Height / e.PreviousSize.Height;
                    imageDisplayed.selectedRegion.ResizeSelectedRect(scale);
                }

            }
        }

        public void calculateCanvasCorners()
        {
            canvasStartX = 114.00 / 683.00;
            canvasStartX = Window.Current.Bounds.Width * canvasStartX;
            canvasStartX = (Window.Current.Bounds.Width -imageDisplayed.displayImage.ActualWidth) - canvasStartX;
            canvasStartX = canvasStartX / 2;

            canvasEndX = canvasStartX +imageDisplayed.displayImage.ActualWidth;

            double temp = Window.Current.Bounds.Height - 140;
            canvasStartY = 49.00 / 631.00;
            canvasStartY = temp * canvasStartY;
            canvasStartY = (Window.Current.Bounds.Height -imageDisplayed.displayImage.ActualHeight) - canvasStartY;
            canvasStartY = canvasStartY / 2;

            canvasEndY = canvasStartY +imageDisplayed.displayImage.ActualHeight;
        }

        void selectedRegion_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Called when the user has dragged the crop corner.
            double widthScale = imageDisplayed.imageCanvas.Width / imageDisplayed.sourceImagePixelWidth;
            double heightScale = imageDisplayed.imageCanvas.Height / imageDisplayed.sourceImagePixelHeight;

            if (imageDisplayed.selectedRegion.SelectedRect.Width !=imageDisplayed.displayImage.ActualWidth ||
                imageDisplayed.selectedRegion.SelectedRect.Height !=imageDisplayed.displayImage.ActualHeight)
                Panel.CropApply.Visibility = Visibility.Visible;
            else
                Panel.CropApply.Visibility = Visibility.Collapsed;
            Panel.selectInfoInBitmapText.Text = string.Format("Resolution: {0}x{1}",
                Math.Floor(imageDisplayed.selectedRegion.SelectedRect.Width / widthScale),
                Math.Floor(imageDisplayed.selectedRegion.SelectedRect.Height / heightScale));
        }

        public void saveImageButton_Click(object sender, RoutedEventArgs e)
        {
            // When the user clicks Apply, the image is cropped.
            UpdatePreviewImage();
        }

        #endregion

       
        public void OnCancelSaveClicked(object sender, RoutedEventArgs e)
        {
            notSaved.IsOpen = false;
            DarkenBorder.Visibility = Visibility.Collapsed;
        }

        public async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            OnCancelSaveClicked(sender, e);
            if (e.OriginalSource.Equals(YesSave))
            {
                await SaveFile(true);
            }
            if (PopupCalledBy == "Browse")
            {
                Panel.GetPhoto();
            }
            else if (PopupCalledBy == "Camera")
            {
                Panel.getCameraPhoto();
            }
        }

        public async void setExampleBitmaps()
        {
			exampleBitmap = await OptionsPopup.ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));

            exampleStream = exampleBitmap.PixelBuffer.AsStream();
            image.srcPixels = new byte[(uint)exampleStream.Length];
            await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);

            prepareImage(exampleStream, exampleBitmap, image);
            setStream(exampleStream, exampleBitmap, image);
            setElements(ColorsPopup.ColorsExamplePicture, exampleBitmap);
            setElements(RotatePopup.RotationsExamplePicture, exampleBitmap);
            setElements(ExposurePopup.ExposureExamplePicture, exampleBitmap);
        }

    }
    #endregion
}
#endregion
