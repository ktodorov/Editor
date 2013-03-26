﻿using System;
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

        // Those are used for the import/export functions.
        public Configuration configFile = new Configuration();
        public List<string> effectsApplied = new List<string>();

		public static MainPage Current;

        // Undo Redo archive
        public List<byte[]> archive_data = new List<byte[]>();
        public int archive_current_index = -1;     // -1 because we don`t have saved pixel array

        public double widthHeightRatio = 0;
        public bool keepProportions = true;
        public bool calledByOther = false;

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
        public string appliedFrame = null, appliedFrameColor = null;

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

            double displayImageWidth = (Window.Current.Bounds.Width * (569.00 / 683.00)) * 0.90;
            double displayImageHeight = Window.Current.Bounds.Height * 0.90;

            imageDisplayed = new DisplayImage(displayImageWidth, displayImageHeight);

            MainMenuPanel.Children.Add(Menu);
            PanelStack.Children.Add(Panel);
            ImageStack.Children.Add(imageDisplayed);

            ColorsPopup = new RemedyColors();
            FiltersPopup = new RemedyFilters();
            ExposurePopup = new RemedyExposure();
            RotatePopup = new RemedyRotations();
            ColorizePopup = new RemedyColorize();

            setPopupsHeight();

            SmallPopups.Children.Add(ExposurePopup);
            SmallPopups.Children.Add(ColorsPopup);
            contentGrid.Children.Add(FiltersPopup);
            SmallPopups.Children.Add(RotatePopup);
            SmallPopups.Children.Add(ColorizePopup);
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

            // Reset all sliders
            ResetAllSliders();

            // Reset archive and archive index and add new image
            archive_data.Clear();
            archive_current_index = -1;
            ArchiveAddArray();

            // Clear array with effects
            effectsApplied.Clear();

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



            widthHeightRatio = (double)bitmapImage.PixelWidth / (double)bitmapImage.PixelHeight;
            newWidth.Text = bitmapImage.PixelWidth.ToString();
            newHeight.Text = bitmapImage.PixelHeight.ToString();

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
            ResetFrameMenuData();
        }

        // Reset data of Filter menu
        public void ResetFilterMenuData()
        {
            FiltersPopup.appliedFilters = null;
            FiltersPopup.deselectFilters();
        }

        // Reset the data of Frame menu
        public void ResetFrameMenuData()
        {
            appliedFrameColor = "black";
            BlackFrameColor.IsSelected = true;
            FrameWidthPercent.Value = 1;
            appliedFrame = null;
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
            ImageOptions.Height = Window.Current.Bounds.Height;
            ColorizePopup.Colorize.Height = Window.Current.Bounds.Height;
            Frames.Height = Window.Current.Bounds.Height;
            Histogram.Height = Window.Current.Bounds.Height;
            FeedbackGrid.Height = Window.Current.Bounds.Height;
            CustomFilter.Height = Window.Current.Bounds.Height;
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

        #region Undo and Redo

        // Undo button click
        public void OnUndoClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;

            if (archive_current_index > 0) // Check if there is no more for undo
            {
                archive_current_index--;
                if (effectsApplied.Count > 0 && (Regex.IsMatch(effectsApplied[archive_current_index], "Crop") || Regex.IsMatch(effectsApplied[archive_current_index], "Resize")))
                {                    
                    string[] sizes = effectsApplied[archive_current_index].Split(' ');
                    imageOriginal.width = Convert.ToInt32(sizes[1]);
                    imageOriginal.height = Convert.ToInt32(sizes[2]);
                    imageOriginal.srcPixels = (byte[])archive_data[archive_current_index].Clone();
                    imageOriginal.dstPixels = (byte[])archive_data[archive_current_index].Clone();
                    bitmapImage = new WriteableBitmap(imageOriginal.width, imageOriginal.height);
                    bitmapStream = bitmapImage.PixelBuffer.AsStream();
                }
                ArchiveSetNewImage();
                setExampleBitmaps();
            }
            ImageLoadingRing.IsActive = false;
        }

        //Redo button click
        public void OnRedoClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;

            if (archive_current_index < archive_data.Count - 1) // Check if there is array for redo
            {
                archive_current_index++;
                if (Regex.IsMatch(effectsApplied[archive_current_index - 1], "Crop") || Regex.IsMatch(effectsApplied[archive_current_index - 1], "Resize"))
                {
                    string[] sizes = effectsApplied[archive_current_index - 1].Split(' ');
                    imageOriginal.width = Convert.ToInt32(sizes[3]);
                    imageOriginal.height = Convert.ToInt32(sizes[4]);
                    imageOriginal.srcPixels = (byte[])archive_data[archive_current_index].Clone();
                    imageOriginal.dstPixels = (byte[])archive_data[archive_current_index].Clone();
                    bitmapImage = new WriteableBitmap(imageOriginal.width, imageOriginal.height);
                    bitmapStream = bitmapImage.PixelBuffer.AsStream();
                }                
                ArchiveSetNewImage();
            }
            ImageLoadingRing.IsActive = false;
        }

        public void ArchiveSetNewImage()
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.srcPixels = (byte[])archive_data[archive_current_index].Clone();
            imageOriginal.dstPixels = (byte[])archive_data[archive_current_index].Clone();
            setStream(bitmapStream, bitmapImage, imageOriginal);
            setExampleBitmaps();
            FiltersPopup.setFilterBitmaps();
            imageDisplayed.displayImage.Source = bitmapImage;
        }

        // Add pixel array to the archive and increment current index of the archive
        public void ArchiveAddArray()
        {
            if (archive_current_index != -1 && archive_current_index != archive_data.Count - 1)
            {
                archive_data.RemoveRange(archive_current_index + 1, archive_data.Count - 1 - archive_current_index);
                effectsApplied.RemoveRange(archive_current_index, effectsApplied.Count - archive_current_index); // Here we don`t save the start image, so we have -1 index of archive_current_index
            }
            archive_data.Add((byte[])imageOriginal.srcPixels.Clone());
            archive_current_index++;
        }
        #endregion


        #region Resizing an image

        // This function resize the passed WriteableBitmap object with the passed width and height.
        // The passed width and height must be proportional of the original width and height( /2, /3, /4 ..).
        public async Task<WriteableBitmap> ResizeImage(WriteableBitmap baseWriteBitmap, uint width, uint height)
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
                ScaledHeight = height,
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

            // We encode the image buffer again:

            // Encoding data
            var inMemoryRandomStream2 = new InMemoryRandomAccessStream();
            var encoder2 = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, inMemoryRandomStream2);
            encoder2.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, width, height, 96, 96, sourceDecodedPixels);
            await encoder2.FlushAsync();
            inMemoryRandomStream2.Seek(0);

            // Finally the resized WritableBitmap
            var bitmap = new WriteableBitmap((int)width, (int)height);
            await bitmap.SetSourceAsync(inMemoryRandomStream2);
            return bitmap;
        }
        #endregion





        #region Frames
        // The events are called when a frame button is clicked.


        // Set standard frame to the image
        public void OnStandardClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "standard";
                ApplyStandardFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandardFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set standard frame (only UP or DOWN) to the image
        public void OnStandardUpDownClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "standard up down";
                ApplyStandartUpDownFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandartUpDownFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set standard frame (only LEFT or RIGHT) to the image
        public void OnStandardLeftRightClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "standard left right";
                ApplyStandardLeftRightFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandardLeftRightFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set darkness frame to the image
        public void OnDarknessClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "darkness";
                ApplyDarknessFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyDarknessFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_DarknessLeftSide(thick);
            imageOriginal.Frames_DarknessTopSide(thick);
            imageOriginal.Frames_DarknessRightSide(thick);
            imageOriginal.Frames_DarknessBottomSide(thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set darkness frame (only left or right) to the image
        public void OnDarknessLeftRightClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "darkness left right";
                ApplyDarknessLeftRightFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyDarknessLeftRightFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_DarknessLeftSide(thick);
            imageOriginal.Frames_DarknessRightSide(thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set darkness frame (only up or down) to the image
        public void OnDarknessUpDownSidesClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "darkness up down";
                ApplyDarknessUpDownFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyDarknessUpDownFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_DarknessTopSide(thick);
            imageOriginal.Frames_DarknessBottomSide(thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set smooth darkness frame to the image
        public void OnSmoothDarknessClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "smooth darkness";
                ApplySmoothDarknessFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplySmoothDarknessFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_SmoothDarkness(thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set standard frame with smooth angles to the image
        public void OnStandardAngleClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "standard angle";
                ApplyStandardAngleFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandardAngleFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandartAngle(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set smooth angles frame to the image
        public void OnAngleClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "angle";
                ApplyAngleFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyAngleFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_Angle(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Apply the frame on the image
        public void OnApplyFramesClick(object sender, RoutedEventArgs e)
        {
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Frame = " + FrameWidthPercent.Value + "," + appliedFrameColor + "," + appliedFrame);
            setExampleBitmaps();
            FiltersPopup.setFilterBitmaps();
            FramesApplyReset.Visibility = Visibility.Collapsed;
            ResetFrameMenuData();
            Saved = false;
        }

        // Reset the image (return the pixels before applying the frame)
        public void OnResetFramesClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                ResetFrameMenuData();
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.Reset();
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }

            FramesApplyReset.Visibility = Visibility.Collapsed;
        }

        // If black color is selected
        public void BlackFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "black";
        }

        // If gray color is selected
        public void GrayFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "gray";
        }

        // If white color is selected
        public void WhiteFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "white";
        }

        // If blue color is selected
        public void BlueFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "blue";
        }

        // If lime color is selected
        public void LimeFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "lime";
        }

        // If yellow color is selected
        public void YellowFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "yellow";
        }

        // If cyan color is selected
        public void CyanFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "cyan";
        }

        // If magenta color is selected
        public void MagentaFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "magenta";
        }

        // If silver color is selected
        public void SilverFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "silver";
        }

        // If maroon color is selected
        public void MaroonFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "maroon";
        }

        // If olive color is selected
        public void OliveFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "olive";
        }

        // If green color is selected
        public void GreenFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "green";
        }

        // If purple color is selected
        public void PurpleFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "purple";
        }

        // If teal color is selected
        public void TealFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "teal";
        }

        // If navy color is selected
        public void NavyFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "navy";
        }

        // If red color is selected
        public void RedFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "red";
        }

        // Get the B G R value of selected color
        public byte[] Frame_GetFrameColor()
        {
            byte[] Color = { 0, 0, 0 };

            switch (appliedFrameColor)
            {
                case "black":
                    {
                        Color[0] = 0;
                        Color[1] = 0;
                        Color[2] = 0;
                        break;
                    }
                case "gray":
                    {
                        Color[0] = 128;
                        Color[1] = 128;
                        Color[2] = 128;
                        break;
                    }
                case "white":
                    {
                        Color[0] = 255;
                        Color[1] = 255;
                        Color[2] = 255;
                        break;
                    }
                case "lime":
                    {
                        Color[0] = 0;
                        Color[1] = 255;
                        Color[2] = 0;
                        break;
                    }
                case "yellow":
                    {
                        Color[0] = 0;
                        Color[1] = 255;
                        Color[2] = 255;
                        break;
                    }
                case "blue":
                    {
                        Color[0] = 255;
                        Color[1] = 0;
                        Color[2] = 0;
                        break;
                    }
                case "red":
                    {
                        Color[0] = 0;
                        Color[1] = 0;
                        Color[2] = 255;
                        break;
                    }
                case "cyan":
                    {
                        Color[0] = 255;
                        Color[1] = 255;
                        Color[2] = 0;
                        break;
                    }
                case "magenta":
                    {
                        Color[0] = 255;
                        Color[1] = 0;
                        Color[2] = 255;
                        break;
                    }
                case "silver":
                    {
                        Color[0] = 192;
                        Color[1] = 192;
                        Color[2] = 192;
                        break;
                    }
                case "maroon":
                    {
                        Color[0] = 0;
                        Color[1] = 0;
                        Color[2] = 128;
                        break;
                    }
                case "olive":
                    {
                        Color[0] = 0;
                        Color[1] = 128;
                        Color[2] = 128;
                        break;
                    }
                case "green":
                    {
                        Color[0] = 0;
                        Color[1] = 128;
                        Color[2] = 0;
                        break;
                    }
                case "purple":
                    {
                        Color[0] = 128;
                        Color[1] = 0;
                        Color[2] = 128;
                        break;
                    }
                case "teal":
                    {
                        Color[0] = 128;
                        Color[1] = 128;
                        Color[2] = 0;
                        break;
                    }
                case "navy":
                    {
                        Color[0] = 128;
                        Color[1] = 0;
                        Color[2] = 0;
                        break;
                    }
            }
            return Color;
        }
        #endregion

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

        #region Image Options
        public async void SetLockPic_Clicked(object sender, RoutedEventArgs e)
        {
            // This sets the current image as a wallpaper on the lock screen of the current user and inform him that everything was okay.
            bool savedFile = false;
            savedFile = await SaveFile(false);
            while (!savedFile)
            {

            }
            await LockScreen.SetImageFileAsync(file);
            MessageDialog messageDialog = new MessageDialog("Picture set! :)", "All done");
            await messageDialog.ShowAsync();
            await deleteUsedFile();
        }

        public async void SetAccountPic_Clicked(object sender, RoutedEventArgs e)
        {
            // This sets the current image as an avatar of the current user and inform him that everything was okay.
            bool savedFile = false;
            savedFile = await SaveFile(false);
            while (!savedFile)
            {

            }
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

        public void ReturnOriginal_Clicked(object sender, RoutedEventArgs e)
        {
            // Restore the original image.
            RestoreOriginalBitmap();
        }

        public void RestoreOriginalBitmap()
        {
            // Reset the current image.
            imageOriginal.srcPixels = (byte[])uneditedImage.srcPixels.Clone();
            imageOriginal.dstPixels = (byte[])uneditedImage.dstPixels.Clone();
            bitmapStream = uneditedStream;
            bitmapImage = uneditedBitmap;
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            setStream(bitmapStream, bitmapImage, imageOriginal);
            imageDisplayed.displayImage.Source = bitmapImage;

            setExampleBitmaps();

            FiltersPopup.setFilterBitmaps();
            imageDisplayed.selectedRegion.ResetCorner(0, 0, imageDisplayed.displayImage.ActualWidth, imageDisplayed.displayImage.ActualHeight);
        }

        #endregion

        public async Task deleteUsedFile()
        {
            // Deletes the temporary created file.
            if (imageOriginal.dstPixels != null)
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("temp.jpg");
                await file.DeleteAsync();
            }
        }

        public void HistogramClicked(object sender, RoutedEventArgs e)
        {
            // Equalize the histogram of the current image.
            Menu.SelectHistogram.IsChecked = false;
            equalizeHistogram();
            FiltersPopup.setFilterBitmaps();
        }

        public void equalizeHistogram()
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.MakeHistogramEqualization();
            setStream(bitmapStream, bitmapImage, imageOriginal);
            prepareImage(exampleStream, exampleBitmap, image);
            image.MakeHistogramEqualization();
            setStream(exampleStream, exampleBitmap, image);
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Histogram = true");
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

            ArchiveAddArray();
            effectsApplied.Add("Crop " + OrignalWidth + " " + OriginalHeight + " " + (int)previewImageSize.Width + " " + (int)previewImageSize.Height);
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

        #region Resizing the image

        public void OnNewWidthTextChanged(object sender, TextChangedEventArgs e)
        {
            int temp;

            if (keepProportions && newWidth.Text != "" && int.TryParse(newWidth.Text, out temp) && !calledByOther)
            {
                newHeight.Text = (Math.Round(temp / widthHeightRatio)).ToString();
            }
            calledByOther = !calledByOther;
            if (newWidth.Text != "")
            {
                ApplyResize.Visibility = Visibility.Visible;
            }
            else
            {
                ApplyResize.Visibility = Visibility.Collapsed;
            }
        }

        public void OnNewHeightTextChanged(object sender, TextChangedEventArgs e)
        {
            int temp;
            if (keepProportions && newHeight.Text != "" && int.TryParse(newHeight.Text, out temp) && !calledByOther)
            {
                newWidth.Text = (Math.Round(temp * widthHeightRatio)).ToString();
            }
            calledByOther = !calledByOther;
            if (newHeight.Text != "")
            {
                ApplyResize.Visibility = Visibility.Visible;
            }
            else
            {
                ApplyResize.Visibility = Visibility.Collapsed;
            }
        }

        public void OnKeepPropsUnchecked(object sender, RoutedEventArgs e)
        {
            keepProportions = false;
        }

        public void OnKeepPropsChecked(object sender, RoutedEventArgs e)
        {
            keepProportions = true;
        }

        public void Resize_Checked(object sender, RoutedEventArgs e)
        {
            ResizePanel.Visibility = Visibility.Visible;
        }

        public void Resize_Unchecked(object sender, RoutedEventArgs e)
        {
            ResizePanel.Visibility = Visibility.Collapsed;
        }

        public async void ApplyResize_Clicked(object sender, RoutedEventArgs e)
        {
            // Resize the current image.
            int a = 0;
            MessageDialog messageDialog = new MessageDialog("Please, input only numbers.", "Error");
            if (!int.TryParse(newWidth.Text, out a) || !int.TryParse(newHeight.Text, out a))
            {
                await messageDialog.ShowAsync();
                return;
            }
            int resizeWidth = Convert.ToInt32(newWidth.Text);
            int resizeHeight = Convert.ToInt32(newHeight.Text);
            int OrignalWidth = imageOriginal.width;
            int OriginalHeight = imageOriginal.height;

            ApplyResize.Visibility = Visibility.Collapsed;

            bitmapImage = await ResizeImage(bitmapImage, (uint)(resizeWidth), (uint)(resizeHeight));
            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            image.srcPixels = new byte[(uint)exampleStream.Length];
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            setStream(bitmapStream, bitmapImage, imageOriginal);
            imageDisplayed.displayImage.Source = bitmapImage;
            ArchiveAddArray();
            effectsApplied.Add("Resize " + OrignalWidth + " " + OriginalHeight + " " + (int)resizeWidth + " " + (int)resizeHeight);
            imageDisplayed.displayImage.Source = bitmapImage;
            FiltersPopup.setFilterBitmaps();
        }

        #endregion

        public void FramesBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // This event completes when the mouse pointer enter the frame border.
            var borderSender = sender as Border;
            borderSender.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        }

        public void FramesBorder_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // This event completes when the mouse pointer exit the frame border.
            var borderSender = sender as Border;
            borderSender.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 25, 112, 0));
        }


        #region Custom Filter

        public void OnCoeffChanged(object sender, TextChangedEventArgs text)
        {
            CustomApplyReset.Visibility = Visibility.Visible;
            TextBox val = (TextBox)text.OriginalSource;
            if (val != null)
            {
                CustomFilter_CheckValue(ref val);
            }
            CustomFilter_CalculateScaleOffset();
        }

        // SCALE AND OFFSET
        public void OnScaleChanged(object sender, TextChangedEventArgs text)
        {
            CustomApplyReset.Visibility = Visibility.Visible;
            CustomFilter_CheckScale();
        }

        public void OnOffsetChanged(object sender, TextChangedEventArgs text)
        {
            CustomApplyReset.Visibility = Visibility.Visible;
            CustomFilter_CheckValue(ref Offset);
        }

        // Check if the value of text box is number and set scale and offset
        public void CustomFilter_CheckValue(ref TextBox coeff)
        {
            try
            {
                Convert.ToInt32(coeff.Text);
            }
            catch (FormatException e)
            {
                if (!(coeff.Text.Length == 1 && coeff.Text[0] == '-'))
                    coeff.Text = "";
            }
        }

        // Check if the scale is > 0
        public void CustomFilter_CheckScale()
        {
            try
            {
                int val = Convert.ToInt32(Scale.Text);

                if (val == 0)
                    Scale.Text = "";
            }
            catch (FormatException e)
            {
                Scale.Text = "";
            }
        }

        //Calculate the Scale and offset
        public void CustomFilter_CalculateScaleOffset()
        {
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 1;
            CustomFilter_SetValues(ref coeff, ref offset, ref scale);
            int sum = 0;

            foreach (int val in coeff)
            {
                sum += val;
            }

            if (sum != 0)
                scale = Math.Abs(sum);
            else
                scale = 1;

            if (sum / scale == 1)
                offset = 0;
            else if (sum == 0)
                offset = 128;
            else if (sum / scale == -1)
                offset = 255;

            Scale.Text = scale.ToString();
            Offset.Text = offset.ToString();
        }

        // Review button click
        public void OnCustomReviewClick(object sender, RoutedEventArgs e)
        {
            CustomReview();
        }

        public void CustomReview()
        {
            ImageLoadingRing.IsActive = true;

            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                CustomFilter_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(imageOriginal.srcPixels, imageOriginal.width, imageOriginal.height, offset, scale, coeff);
                imageOriginal.dstPixels = custom_image.Filter();

                setStream(bitmapStream, bitmapImage, imageOriginal);
            }

            ImageLoadingRing.IsActive = false;
        }

        // Apply button click
        public void OnCustomApplyClick(object sender, RoutedEventArgs e)
        {
            CustomApply();
            CustomApplyReset.Visibility = Visibility.Collapsed;
            setExampleBitmaps();
            FiltersPopup.setFilterBitmaps();
            Saved = false;
        }

        public void CustomApply()
        {
            ImageLoadingRing.IsActive = true;

            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                CustomFilter_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(imageOriginal.srcPixels, imageOriginal.width, imageOriginal.height, offset, scale, coeff);
                imageOriginal.dstPixels = custom_image.Filter();

                setStream(bitmapStream, bitmapImage, imageOriginal);
            }

            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Custom = " + "TODO");
            CustomFilter_ResetValues();
            ImageLoadingRing.IsActive = false;
        }

        // Reset button click
        public void OnCustomResetClick(object sender, RoutedEventArgs e)
        {
            CustomFilter_ResetValues();

            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
            CustomApplyReset.Visibility = Visibility.Collapsed;
        }

        // Reset All values of custom filter
        public void CustomFilter_ResetValues()
        {
            coeff00.Text = coeff10.Text = coeff20.Text = coeff30.Text = coeff40.Text = "";
            coeff01.Text = coeff11.Text = coeff21.Text = coeff31.Text = coeff41.Text = "";
            coeff02.Text = coeff12.Text = coeff32.Text = coeff42.Text = "";
            coeff03.Text = coeff13.Text = coeff23.Text = coeff33.Text = coeff43.Text = "";
            coeff04.Text = coeff14.Text = coeff24.Text = coeff34.Text = coeff44.Text = "";
            coeff22.Text = "1";

            Scale.Text = "1";
            Offset.Text = "0";
        }

        // Set the matrix for custom filter
        public void CustomFilter_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            CustomFilter_SetValue(ref coeff[0, 0], coeff00);
            CustomFilter_SetValue(ref coeff[1, 0], coeff10);
            CustomFilter_SetValue(ref coeff[2, 0], coeff20);
            CustomFilter_SetValue(ref coeff[3, 0], coeff30);
            CustomFilter_SetValue(ref coeff[4, 0], coeff40);

            CustomFilter_SetValue(ref coeff[0, 1], coeff01);
            CustomFilter_SetValue(ref coeff[1, 1], coeff11);
            CustomFilter_SetValue(ref coeff[2, 1], coeff21);
            CustomFilter_SetValue(ref coeff[3, 1], coeff31);
            CustomFilter_SetValue(ref coeff[4, 1], coeff41);

            CustomFilter_SetValue(ref coeff[0, 2], coeff02);
            CustomFilter_SetValue(ref coeff[1, 2], coeff12);
            CustomFilter_SetValue(ref coeff[2, 2], coeff22);
            CustomFilter_SetValue(ref coeff[3, 2], coeff32);
            CustomFilter_SetValue(ref coeff[4, 2], coeff42);

            CustomFilter_SetValue(ref coeff[0, 3], coeff03);
            CustomFilter_SetValue(ref coeff[1, 3], coeff13);
            CustomFilter_SetValue(ref coeff[2, 3], coeff23);
            CustomFilter_SetValue(ref coeff[3, 3], coeff33);
            CustomFilter_SetValue(ref coeff[4, 3], coeff43);

            CustomFilter_SetValue(ref coeff[0, 4], coeff04);
            CustomFilter_SetValue(ref coeff[1, 4], coeff14);
            CustomFilter_SetValue(ref coeff[2, 4], coeff24);
            CustomFilter_SetValue(ref coeff[3, 4], coeff34);
            CustomFilter_SetValue(ref coeff[4, 4], coeff44);

            CustomFilter_SetScale(ref scale);
            CustomFilter_SetValue(ref offset, Offset);
        }

        // Set one coeff of matrix
        public void CustomFilter_SetValue(ref int coeff, TextBox val)
        {
            int new_val = 0;
            try
            {
                new_val = Convert.ToInt32(val.Text);
            }
            catch (FormatException e)
            {
                if (!(val.Text.Length == 1 && val.Text[0] == '-'))
                    val.Text = "";
            }

            if (new_val != 0)
                coeff = new_val;
        }

        // Set the scale value of custom filter
        public void CustomFilter_SetScale(ref int scale)
        {
            int new_val = 0;
            try
            {
                new_val = Convert.ToInt32(Scale.Text);

                if (new_val <= 0)
                {
                    Scale.Text = "";
                    scale = 1;
                }
            }
            catch (FormatException e)
            {
                Scale.Text = "";
                scale = 1;
            }

            if (new_val > 0)
                scale = new_val;
        }

        #endregion


        #region Export/Import
        public void OnExportButtonClick(object sender, RoutedEventArgs e)
        {
            if (archive_current_index != archive_data.Count - 1)
            {
                archive_data.RemoveRange(archive_current_index + 1, archive_data.Count - 1 - archive_current_index);
                effectsApplied.RemoveRange(archive_current_index, effectsApplied.Count - archive_current_index); // Here we don`t save the start image, so we have -1 index of archive_current_index
            }
            configFile.Export(effectsApplied);
        }

        public void onImportButtonClick(object sender, RoutedEventArgs e)
        {
            if (archive_current_index != archive_data.Count - 1)
            {
                archive_data.RemoveRange(archive_current_index + 1, archive_data.Count - 1 - archive_current_index);
                effectsApplied.RemoveRange(archive_current_index, effectsApplied.Count - archive_current_index); // Here we don`t save the start image, so we have -1 index of archive_current_index
            }

            ImageLoadingRing.IsActive = true;
            DarkenBorder.Visibility = Visibility.Visible;

            for (int i = 0; i < configFile.effects.Count; i += 2)
            {
                checkEffect(i);
            }
            FiltersPopup.setFilterBitmaps();
            ImageLoadingRing.IsActive = false;
            DarkenBorder.Visibility = Visibility.Collapsed;
        }

        public void checkEffect(int i)
        {
            string[] temp = new string[10];
            switch (configFile.effects[i])
            {
                case "Filter":
                    FiltersPopup.ApplyFilter(configFile.effects[i + 1]);
                    break;

                case "Color":
                    temp = configFile.effects[i + 1].Split(',');
                    importColor(temp);
                    break;

                case "Contrast":
                    temp = configFile.effects[i + 1].Split(',');
                    importContrast(temp);
                    break;

                case "Exposure":
                    temp = configFile.effects[i + 1].Split(',');
                    importExposure(temp);
                    break;

                case "Flip":
                    temp = configFile.effects[i + 1].Split(',');
                    RotatePopup.ApplyRotate(temp[0]);
                    break;

                case "Colorize":
                    temp = configFile.effects[i + 1].Split(',');
                    importColorize(temp);
                    break;

                case "Frame":
                    temp = configFile.effects[i + 1].Split(',');
                    checkAndApplyFrames(temp);
                    imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
                    break;

                case "Rotate":
                    temp = configFile.effects[i + 1].Split(',');
                    RotatePopup.RotateBitmap(temp[0]);
                    break;

                case "Histogram":
                    if (configFile.effects[i + 1] == "true")
                    {
                        equalizeHistogram();
                    }
                    break;
                default: break;
            }
        }

        #region Import Functions
        public void importColor(string[] temp)
        {
            ColorsPopup.importColor(temp);
        }

        public void importContrast(string[] temp)
        {
            ColorsPopup.importContrast(temp);
        }

        public void importExposure(string[] temp)
        {
            ExposurePopup.Import(temp);
        }

        public void importColorize(string[] temp)
        {
            ColorizePopup.Import(temp);
        }
        #endregion


        public void checkAndApplyFrames(string[] frameStats)
        {
            int thickPercent = Convert.ToInt32(frameStats[0]);
            appliedFrameColor = frameStats[1];
            string frameType = frameStats[2];

            switch (frameType)
            {
                case "standard":
                    ApplyStandardFrame(thickPercent);
                    break;
                case "standard up down":
                    ApplyStandartUpDownFrame(thickPercent);
                    break;
                case "standard left right":
                    ApplyStandardLeftRightFrame(thickPercent);
                    break;
                case "darkness":
                    ApplyDarknessFrame(thickPercent);
                    break;
                case "darkness left right":
                    ApplyDarknessLeftRightFrame(thickPercent);
                    break;
                case "darkness up down":
                    ApplyDarknessUpDownFrame(thickPercent);
                    break;
                case "smooth darkness":
                    ApplySmoothDarknessFrame(thickPercent);
                    break;
                case "standard angle":
                    ApplyStandardAngleFrame(thickPercent);
                    break;
                case "angle":
                    ApplyAngleFrame(thickPercent);
                    break;
                default:
                    break;
            }
        }

        public async void onImportFileSelectButtonClick(object sender, RoutedEventArgs e)
        {
            bool imported = await configFile.Import(importFileName);
            if (imported)
                importFilePanel.Visibility = Visibility.Visible;
            else if (configFile == null)
                importFilePanel.Visibility = Visibility.Collapsed;
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
            exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));

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
