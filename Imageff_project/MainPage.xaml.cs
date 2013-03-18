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
using Windows.Media.Capture;
using Windows.System.UserProfile;

#region Namespace RemedyPic
namespace RemedyPic
{
	#region MainPage class
	public sealed partial class MainPage : RemedyPic.Common.LayoutAwarePage
	{

		#region Variables
		// Those are all the global variables, that are used in MainPage.xaml.cs file.

		private Configuration configFile = new Configuration();
		private List<string> effectsApplied = new List<string>();

        // Undo Redo archive
        private List<byte[]> archive_data = new List<byte[]>();
        private int archive_current_index = -1;     // -1 because we don`t have saved pixel array

		private double widthHeightRatio = 0;
		private bool keepProportions = true;

		// mruToken is used for LoadState and SaveState functions.
		private string mruToken = null;

		// This variable holds the current file that we are using.
		StorageFile file;

		// String variables that hold the current applied changes to the image.
		private string appliedFilters = null, appliedColors = null,
						   appliedRotations = null, appliedFrame = null, appliedFrameColor = null;

		// We create two WriteableBitmap variables.
		// One for the original image and one for the small bitmaps.
		// They are used to display the image on the screen.
		private WriteableBitmap bitmapImage, exampleBitmap;

		// The streams are used to save the pixels as a Stream to the WriteableBitmap objects.
		Stream bitmapStream, exampleStream;

		// This is set true when the user opens a picture.
		bool pictureIsLoaded = false;

		// Colorize selected colors
		private bool redForColorize, greenForColorize, blueForColorize, yellowForColorize,
						 orangeForColorize, purpleForColorize, cyanForColorize, limeForColorize = false;

		// We create three RemedyImages.
		// One for the original displayed image, one for the small images and
		// one to hold the original loaded image so we can get back to it at any time
		RemedyImage image = new RemedyImage();
		RemedyImage imageOriginal = new RemedyImage();
		RemedyImage uneditedImage = new RemedyImage();

		// We create two streams for two of the WriteableBitmap objects.
		private Stream uneditedStream;
		private WriteableBitmap uneditedBitmap;

		// Those are variables used with the manipulations of the Image
		private TransformGroup _transformGroup;
		private MatrixTransform _previousTransform;
		private CompositeTransform _compositeTransform;
		private bool forceManipulationsToEnd;

		// SelectedRegion variable, used by the crop function.
		SelectedRegion selectedRegion;

		// The original Width and Height of the image in pixels.
		uint sourceImagePixelWidth;
		uint sourceImagePixelHeight;

		// The size of the corners of the crop rectangle.
		double cornerSize;
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

		// The dictionary holds the history of all previous pointer locations. It is used by the crop function.
		Dictionary<uint, Point?> pointerPositionHistory = new Dictionary<uint, Point?>();

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
			setPopupsHeight();
		}

		#region Charms
		private void RegisterCharms()
		{
			// If the user chooses the charm, we register the share charm 
			// and the settings charm so we can get all the available apps for sharing.
			DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
			dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
				DataRequestedEventArgs>(this.ShareImageHandler);
			SettingsPane.GetForCurrentView().CommandsRequested += OnSettingsPaneCommandRequested;
		}

		private void OnSettingsPaneCommandRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
		{
			// We add the Feedback settings to the Settings charm.
			args.Request.ApplicationCommands.Add(new SettingsCommand("commandID",
																	 "Feedback", FeedbackPopup));
		}

		private void FeedbackPopup(IUICommand command)
		{
			// This event occures when the user clicks on the Feedback in the settings charm.
			Feedback.IsOpen = true;
		}

		private async void ShareImageHandler(DataTransferManager sender,
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
			// This saves information if the app quits unexpected.
			if (!String.IsNullOrEmpty(mruToken))
			{
				pageState["mruToken"] = mruToken;
			}
		}
		#endregion

		#region Functions, called when opening an image.
		#region Get Photo
		// This occures when GetPhoto button is clicked
		private void GetPhotoButton_Click(object sender, RoutedEventArgs e)
		{
			GetPhoto();
		}

		private async void GetPhoto(StorageFile fileToUse = null)
		{
			// File picker APIs don't work if the app is in a snapped state.
			// If the app is snapped, try to unsnap it first. Only show the picker if it unsnaps.
			if (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped ||
				 Windows.UI.ViewManagement.ApplicationView.TryUnsnap() == true)
			{
				if (fileToUse == null)
				{
					// First, create a new file picker.
					FileOpenPicker filePicker = new FileOpenPicker();
					// Make the file picker view mode to Thumbnails.
					filePicker.ViewMode = PickerViewMode.Thumbnail;
					// Add several file extensions to be available for opening.
					filePicker.FileTypeFilter.Add(".jpg");
					filePicker.FileTypeFilter.Add(".png");
					filePicker.FileTypeFilter.Add(".bmp");
					filePicker.FileTypeFilter.Add(".jpeg");

					// Get the selected file and save it to the StorageFile variable.
					file = await filePicker.PickSingleFileAsync();
					// We create the new WriteableBitmap.
				}
				else
				{
					file = fileToUse;
				}
				bitmapImage = new WriteableBitmap(1, 1);
				if (file != null)
				// File is null if user cancels the file picker.
				{
					ImageLoadingRing.IsActive = true;
					AnimateOutPicture.Begin();

					// We create a temporary stream for the opened file.
					// Then we decode the stream to a BitmapDecoder
					// so we can set the image width and height to the variables.
					// Then we set the Stream to the WriteableBitmap
					// and set the pictureIsLoaded variable to true.
					Windows.Storage.Streams.IRandomAccessStream fileStream =
							await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

					BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);

					sourceImagePixelHeight = decoder.PixelHeight;
					sourceImagePixelWidth = decoder.PixelWidth;


					bitmapImage.SetSource(fileStream);
					RandomAccessStreamReference streamRef = RandomAccessStreamReference.CreateFromFile(file);

					// If the interface was changed from previous image, it should be resetted.
					ResetZoomPos();

					pictureIsLoaded = true;
					doAllCalculations();
					ImageLoadingRing.IsActive = false;
				}
			}
			else
			{
				// If the window can't be unsnapped, show alert.
				MessageDialog messageDialog = new MessageDialog("Can't open in snapped state. Please unsnap the app and try again", "Close");
				await messageDialog.ShowAsync();
			}
		}

		private async void OnCameraButtonClick(object sender, RoutedEventArgs e)
		{
				CameraCaptureUI camera = new CameraCaptureUI();
				camera.PhotoSettings.CroppedAspectRatio = new Size(16, 10);
				StorageFile photoFile = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);
				if (photoFile != null)
					GetPhoto(photoFile);
		}

		#endregion

		private async void doAllCalculations()
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
			setElements(ColorsExamplePicture, exampleBitmap);
			setElements(RotationsExamplePicture, exampleBitmap);
			setElements(ExposureExamplePicture, exampleBitmap);

			// Make the images ready for work.
			prepareImage(exampleStream, exampleBitmap, image);
			setStream(exampleStream, exampleBitmap, image);
			prepareImage(uneditedStream, uneditedBitmap, uneditedImage);
			setStream(uneditedStream, uneditedBitmap, uneditedImage);
			prepareImage(bitmapStream, bitmapImage, imageOriginal);
			setStream(bitmapStream, bitmapImage, imageOriginal);

			ZoomStack.Visibility = Visibility.Visible;

			// set the small WriteableBitmap objects to the filter buttons.
			setFilterBitmaps();

			// Display the file name.
			setFileProperties(file);

			// Set the WriteableBitmap as source to the XAML Image object. This makes the picture appear on the screen.
			displayImage.Source = bitmapImage;
			AnimateInPicture.Begin();

			// We check the CheckBox that is required for the image to move by default.
			AvailableForMoving.IsChecked = true;

			// We set the imagePanel maximum height so the image not to go out of the screen
			displayImage.MaxWidth = imageBorder.ActualWidth * 0.80;
			displayImage.MaxHeight = imageBorder.ActualHeight * 0.80;

			widthHeightRatio = (double)bitmapImage.PixelWidth / (double)bitmapImage.PixelHeight;
			newWidth.Text = bitmapImage.PixelWidth.ToString();
			newHeight.Text = bitmapImage.PixelHeight.ToString();

			// Show the interface.
			showInterface();


		}

        private async void setExampleImage()
        {
            exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));
            exampleStream = exampleBitmap.PixelBuffer.AsStream();
            image.srcPixels = new byte[(uint)exampleStream.Length];
            await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);
            setElements(ColorsExamplePicture, exampleBitmap);
            setElements(RotationsExamplePicture, exampleBitmap);
            setElements(ExposureExamplePicture, exampleBitmap);
            prepareImage(exampleStream, exampleBitmap, image);
            setStream(exampleStream, exampleBitmap, image);
        }

		private void ResetAllSliders()
		{
			ResetFilterMenuData();
			ResetColorMenuData();
			ResetExposureMenuData();
			ResetRotateMenuData();
			ResetColorizeMenuData();
			ResetFrameMenuData();
		}

		// Reset data of Filter menu
		private void ResetFilterMenuData()
		{
			appliedFilters = null;
			deselectFilters();
		}

		// Reset the data of Color menu
		private void ResetColorMenuData()
		{
			appliedColors = null;

			BlueColorSlider.Value = 0;
			GreenColorSlider.Value = 0;
			RedColorSlider.Value = 0;

			BlueContrastSlider.Value = 0;
			GreenContrastSlider.Value = 0;
			RedContrastSlider.Value = 0;
		}

		// Reset the slider values of Exposure Menu
		private void ResetExposureMenuData()
		{
			brightSlider.Value = 0;

			BlueGammaSlider.Value = 10;
			GreenGammaSlider.Value = 10;
			RedGammaSlider.Value = 10;
		}

		// Reset the data of Rotate menu
		private void ResetRotateMenuData()
		{
			appliedRotations = null;
		}

		// Reset the data of Colorize menu
		private void ResetColorizeMenuData()
		{
			redForColorize = greenForColorize = blueForColorize = yellowForColorize =
							 orangeForColorize = purpleForColorize = cyanForColorize =
							 limeForColorize = false;
			deselectColorizeGridItems();
		}

		// Reset the data of Frame menu
		private void ResetFrameMenuData()
		{
			appliedFrameColor = "black";
			BlackFrameColor.IsSelected = true;
			FrameWidthPercent.Value = 1;
			appliedFrame = null;
		}
		private void setPopupsHeight()
		{
			// We set the popups height to match the current machine's resolution
			Filters.Height = Window.Current.Bounds.Height;
			Colors.Height = Window.Current.Bounds.Height;
			Rotations.Height = Window.Current.Bounds.Height;
			ImageOptions.Height = Window.Current.Bounds.Height;
			Colorize.Height = Window.Current.Bounds.Height;
			Frames.Height = Window.Current.Bounds.Height;
			Histogram.Height = Window.Current.Bounds.Height;
			FeedbackGrid.Height = Window.Current.Bounds.Height;
			Exposure.Height = Window.Current.Bounds.Height;
		}

		private void setElements(Windows.UI.Xaml.Controls.Image imageElement, WriteableBitmap source)
		{
			// We set the XAML Image object a bitmap as a source 
			// and then set the width and height to be proportional to the actual bitmap
			imageElement.Source = source;
			imageElement.Width = bitmapImage.PixelWidth / 4;
			imageElement.Height = bitmapImage.PixelHeight / 4;
		}

		private void showInterface()
		{
			// Called when the image is loaded.
			// It shows the interface.
			Zoom.Visibility = Visibility.Visible;
			Menu.Visibility = Visibility.Visible;
			UndoRedoPanel.Visibility = Visibility.Visible;
		}
		#endregion

		#region Filter functions

		#region Invert Filter
		// Invert filter function
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
			}
		}

		// Set the matrix for invert filter
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
			}
		}
		#endregion

		#region Emboss Filter
		// Emboss filter function
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
			}
		}

		// Set the matrix for emboss filter
		private void Emboss_SetValues(ref int[,] coeff, ref int offset, ref int scale)
		{
			coeff[2, 2] = 1;
			coeff[3, 3] = -1;
			offset = 128;
			scale = 1;
		}
		#endregion

		#region Emboss 2 Filter
		// Emboss 2 filter function
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
			}
		}

		// Set the matrix for emboss2 filter
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
		// Sharpen filter function
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
			}
		}

		// Set the matrix for sharpen filter
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
		// Blur filter function
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
			}
		}

		// Set the matrix for blur filter
		private void Blur_SetValues(ref int[,] coeff, ref int offset, ref int scale)
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
		}
		#endregion

		#region Blur2 Filter
		// Blur2 filter function
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
			}
		}

		// Set the matrix for blur2 filter
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
		// EdgeDetect filter function
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
			}
		}

		// Set the matrix for edgeDetect filter
		private void EdgeDetect_SetValues(ref int[,] coeff, ref int offset, ref int scale)
		{
			coeff[2, 2] = -4;
			coeff[1, 2] = coeff[2, 1] = coeff[2, 3] = coeff[3, 2] = 1;
			offset = 0;
			scale = 1;
		}
		#endregion

		#region EdgeEnhance Filter
		// EdgeEnhance filter function
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
			}
		}

		// Set the matrix for edgeEnhance filter
		private void EdgeEnhance_SetValues(ref int[,] coeff, ref int offset, ref int scale)
		{
			coeff[2, 2] = 1;
			coeff[3, 0] = -1;
			offset = 0;
			scale = 1;
		}

		#endregion

		#endregion

		#region Save
		private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
		{
			// File picker APIs don't work if the app is in a snapped state.
			// If the app is snapped, try to unsnap it first. Only show the picker if it unsnaps.
			if (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped ||
				 Windows.UI.ViewManagement.ApplicationView.TryUnsnap() == true)
			{
				// We call the SaveFile function with true so we can use the file picker.
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
				// If the picker variable is true, we call a FilePicker.
				// If it's not, we save a temporary file without notifying the user to the local directory of the app.
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
						// If the array is null, this means no changes were made, so we can use the currently opened file.
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

		void setFileProperties(Windows.Storage.StorageFile file)
		{
			// This sets the file name to the text box
			fileName.Text = file.Name;
		}

		void setStream(Stream givenStream, WriteableBitmap givenBitmap, RemedyImage givenImage)
		{
			// This sets the pixels to the bitmap
			// and makes the ApplyReset stackPanel of the current popup to appear.
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
				else if (PopupExposure.IsOpen)
					ExposureApplyReset.Visibility = Visibility.Visible;
			}
		}

		void prepareImage(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
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
        private void OnUndoClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;

            if (archive_current_index > 0) // Check if there is no more for undo
            {                       
                archive_current_index--;
                ArchiveSetNewImage();               
            }
            ImageLoadingRing.IsActive = false;
        }

        //Redo button click
        private void OnRedoClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;

            if (archive_current_index < archive_data.Count - 1) // Check if there is array for redo
            {
                archive_current_index++;
                ArchiveSetNewImage();                
            }
            ImageLoadingRing.IsActive = false;
        }

        private void ArchiveSetNewImage()
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.srcPixels = (byte[])archive_data[archive_current_index].Clone();
            imageOriginal.dstPixels = (byte[])archive_data[archive_current_index].Clone();
            setStream(bitmapStream, bitmapImage, imageOriginal);
            setExampleImage();
            setFilterBitmaps();
        }

        // Add pixel array to the archive and increment current index of the archive
        private void ArchiveAddArray()
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

        #region Color Change

        private void OnColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				prepareImage(exampleStream, exampleBitmap, image);
				image.ColorChange(BlueColorSlider.Value, GreenColorSlider.Value, RedColorSlider.Value, BlueContrastSlider.Value, GreenContrastSlider.Value, RedContrastSlider.Value);
				setStream(exampleStream, exampleBitmap, image);
			}
		}

		#endregion

		#region Resizing an image

		// This function resize the passed WriteableBitmap object with the passed width and height.
		// The passed width and height must be proportional of the original width and height( /2, /3, /4 ..).
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

		#region Filters
		// Change the image with black and white filter applied
		private void doBlackWhite(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			givenImage.BlackAndWhite(givenImage.dstPixels, givenImage.srcPixels);
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with invert filter applied
		private void doInvert(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			int[,] coeff = new int[5, 5];
			int offset = 0, scale = 0;
			Invert_SetValues(ref coeff, ref offset, ref scale);
			CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
			givenImage.dstPixels = custom_image.Filter();
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with emboss filter applied
		private void doEmboss(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			int[,] coeff = new int[5, 5];
			int offset = 0, scale = 0;
			Emboss_SetValues(ref coeff, ref offset, ref scale);
			CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
			givenImage.dstPixels = custom_image.Filter();
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with emboss2 filter applied
		private void doEmboss2(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			int[,] coeff = new int[5, 5];
			int offset = 0, scale = 0;
			Emboss2_SetValues(ref coeff, ref offset, ref scale);
			CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
			givenImage.dstPixels = custom_image.Filter();
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with sharpen filter applied
		private void doSharpen(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			int[,] coeff = new int[5, 5];
			int offset = 0, scale = 0;
			Sharpen_SetValues(ref coeff, ref offset, ref scale);
			CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
			givenImage.dstPixels = custom_image.Filter();
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with noise filter applied
		private void doNoise(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);

			givenImage.Noise(givenImage.Noise_GetSquareWidth(20));
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with hardnoise filter applied
		private void doHardNoise(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);

			givenImage.Noise(1);
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with blur filter applied
		private void doBlur(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			int[,] coeff = new int[5, 5];
			int offset = 0, scale = 0;
			Blur_SetValues(ref coeff, ref offset, ref scale);
			CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
			givenImage.dstPixels = custom_image.Filter();
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with blur2 filter applied
		private void doBlur2(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			int[,] coeff = new int[5, 5];
			int offset = 0, scale = 0;
			Blur2_SetValues(ref coeff, ref offset, ref scale);
			CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
			givenImage.dstPixels = custom_image.Filter();
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with edgeDetect filter applied
		private void doEdgeDetect(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			int[,] coeff = new int[5, 5];
			int offset = 0, scale = 0;
			EdgeDetect_SetValues(ref coeff, ref offset, ref scale);
			CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
			givenImage.dstPixels = custom_image.Filter();
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with edgeEnhance filter applied
		private void doEdgeEnhance(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			int[,] coeff = new int[5, 5];
			int offset = 0, scale = 0;
			EdgeEnhance_SetValues(ref coeff, ref offset, ref scale);
			CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
			givenImage.dstPixels = custom_image.Filter();
			setStream(stream, bitmap, givenImage);

		}

		// Change the image with retro filter applied
		private void doRetro(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			givenImage.ColorChange(0, 0, 0, 50, 50, -50);
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with darken filter applied
		private void doDarken(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			givenImage.ColorChange(0, 0, 0, 50, 50, 0);
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with brighten filter applied
		private void doBrighten(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			givenImage.ColorChange(70, 70, 70, 0, 0, 0);
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with shadow filter applied
		private void doShadow(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			givenImage.ColorChange(-80, -80, -80, 0, 0, 0);
			setStream(stream, bitmap, givenImage);
		}

		// Change the image with crystal filter applied
		private void doCrystal(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			prepareImage(stream, bitmap, givenImage);
			givenImage.ColorChange(0, 0, 0, 50, 35, 35);
			setStream(stream, bitmap, givenImage);
		}
		#endregion

		#region Apply Buttons
		// Event for apply button on Filters popup. Sets the image with the applied filter
		private void OnFilterApplyClick(object sender, RoutedEventArgs e)
		{			
			ApplyFilter(appliedFilters);            
			FilterApplyReset.Visibility = Visibility.Collapsed;
			SelectFilters.IsChecked = false;
			setFilterBitmaps();
		}

		public void ApplyFilter(string filter)
		{
			ImageLoadingRing.IsActive = true;
			switch (filter)
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
				case "noise":
					doNoise(bitmapStream, bitmapImage, imageOriginal);
					doNoise(exampleStream, exampleBitmap, image);
					break;
				case "hardNoise":
					doHardNoise(bitmapStream, bitmapImage, imageOriginal);
					doHardNoise(exampleStream, exampleBitmap, image);
					break;
				case "EdgeDetect":
					doEdgeDetect(bitmapStream, bitmapImage, imageOriginal);
					doEdgeDetect(exampleStream, exampleBitmap, image);
					break;
				case "EdgeEnhance":
					doEdgeEnhance(bitmapStream, bitmapImage, imageOriginal);
					doEdgeEnhance(exampleStream, exampleBitmap, image);
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
            ArchiveAddArray();
            effectsApplied.Add("Filter = " + appliedFilters);
			ResetFilterMenuData();
			ImageLoadingRing.IsActive = false;
		}
        
		// Event for apply button on Colors popup. Sets the image with the applied colors
		private void OnColorApplyClick(object sender, RoutedEventArgs e)
		{
            ApplyColor();
			setFilterBitmaps();
		}

		private void ApplyColor()
		{
			ImageLoadingRing.IsActive = true;
			ColorApplyReset.Visibility = Visibility.Collapsed;
			SelectColors.IsChecked = false;
			prepareImage(bitmapStream, bitmapImage, imageOriginal);
			imageOriginal.ColorChange(BlueColorSlider.Value, GreenColorSlider.Value, RedColorSlider.Value, BlueContrastSlider.Value, GreenContrastSlider.Value, RedContrastSlider.Value);
			setStream(bitmapStream, bitmapImage, imageOriginal);

			image.srcPixels = (byte[])image.dstPixels.Clone();
			imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();

            ArchiveAddArray();
            effectsApplied.Add("Color = " + BlueColorSlider.Value + "," + GreenColorSlider.Value + "," + RedColorSlider.Value + "," + BlueContrastSlider.Value + "," + GreenContrastSlider.Value + "," + RedContrastSlider.Value);
			ResetColorMenuData();
			ImageLoadingRing.IsActive = false;
		}

		// Event for apply button on  Rotate popup. Sets the image with the applied flip
		private void OnRotateApplyClick(object sender, RoutedEventArgs e)
		{
			ImageLoadingRing.IsActive = true;
			SelectRotations.IsChecked = false;			
			ApplyRotate(appliedRotations);           
			setFilterBitmaps();
			ImageLoadingRing.IsActive = false;
			RotateApplyReset.Visibility = Visibility.Collapsed;
		}

		private void ApplyRotate(string rotation)
		{
			switch (rotation)
			{
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
				default:
					break;
			}
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone(); 
            ArchiveAddArray();
            effectsApplied.Add("Flip = " + appliedRotations);
			ResetRotateMenuData();
		}

		// Event for apply button on Colorize popup. Sets the image with the applied color
		private void OnColorizeApplyClick(object sender, RoutedEventArgs e)
		{
			doColorize(exampleStream, exampleBitmap, image);
			ApplyColorize();
			setFilterBitmaps();
			ColorizeApplyReset.Visibility = Visibility.Collapsed;
			SelectColorize.IsChecked = false;
		}

		private void ApplyColorize()
		{
			ImageLoadingRing.IsActive = true;			
			image.srcPixels = (byte[])image.dstPixels.Clone();
			imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();            
            ArchiveAddArray();
            Colorize_SetColorizeEffect();
			ImageLoadingRing.IsActive = false;
		}

		private void Colorize_SetColorizeEffect()
		{
			string colorizeColors = "";
			Colorize_GetColorizeColors(ref colorizeColors);
			effectsApplied.Add("Colorize = " + colorizeColors);

		}

		private void Colorize_GetColorizeColors(ref string colorizeColors)
		{
			if (blueForColorize)
			{
				Colorize_CheckForFirsColor(ref colorizeColors);
				colorizeColors += "blue";
			}
			if (redForColorize)
			{
				Colorize_CheckForFirsColor(ref colorizeColors);
				colorizeColors += "red";
			}
			if (greenForColorize)
			{
				Colorize_CheckForFirsColor(ref colorizeColors);
				colorizeColors += "green";
			}
			if (yellowForColorize)
			{
				Colorize_CheckForFirsColor(ref colorizeColors);
				colorizeColors += "yellow";
			}
			if (orangeForColorize)
			{
				Colorize_CheckForFirsColor(ref colorizeColors);
				colorizeColors += "orange";
			}
			if (purpleForColorize)
			{
				Colorize_CheckForFirsColor(ref colorizeColors);
				colorizeColors += "purple";
			}
			if (cyanForColorize)
			{
				Colorize_CheckForFirsColor(ref colorizeColors);
				colorizeColors += "cyan";
			}
			if (limeForColorize)
			{
				Colorize_CheckForFirsColor(ref colorizeColors);
				colorizeColors += "lime";
			}
		}

		private void Colorize_CheckForFirsColor(ref string colorizeColors)
		{
			if (!colorizeColors.Equals(""))
			{
				colorizeColors += ",";
			}

		}
		// Event for apply button on Exposure popup. Sets the image with the applied exposure
		private void OnExposureApplyClick(object sender, RoutedEventArgs e)
		{
			ApplyExposure(appliedColors);
			setFilterBitmaps();
		}

		private void ApplyExposure(string effect)
		{
			ImageLoadingRing.IsActive = true;
			ExposureApplyReset.Visibility = Visibility.Collapsed;
			SelectExposure.IsChecked = false;			

			switch (effect)
			{
				case "gammadarken":
					doGammaDarken();
					break;
				case "gammalighten":
					doGammaLighten();
					break;
				default:
					break;
			}

			image.srcPixels = (byte[])image.dstPixels.Clone();
			imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Exposure = " + brightSlider.Value + "," + BlueGammaSlider.Value + "," + GreenGammaSlider.Value + "," + RedGammaSlider.Value);
			ResetExposureMenuData();
			ImageLoadingRing.IsActive = false;
		}

		private void doGammaDarken()
		{
			prepareImage(bitmapStream, bitmapImage, imageOriginal);
			imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
			imageOriginal.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
			imageOriginal.Darken(brightSlider.Value);
			setStream(bitmapStream, bitmapImage, imageOriginal);
		}

		private void doGammaLighten()
		{
			prepareImage(bitmapStream, bitmapImage, imageOriginal);
			imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
			imageOriginal.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
			imageOriginal.Lighten(brightSlider.Value);
			setStream(bitmapStream, bitmapImage, imageOriginal);
		}

		#endregion

		#region Reset Buttons
		// All those events reset the interface and return the last applied image.
		private void OnFilterResetClick(object sender, RoutedEventArgs e)
		{
			// This resets the interface and returns the last applied image.
			if (pictureIsLoaded)
			{
				prepareImage(exampleStream, exampleBitmap, image);
				image.Reset();
				setStream(exampleStream, exampleBitmap, image);
			}
			FilterApplyReset.Visibility = Visibility.Collapsed;
			ResetFilterMenuData();
			deselectFilters();
		}

		private void OnColorResetClick(object sender, RoutedEventArgs e)
		{
			// This resets the interface and returns the last applied image.
			ResetColorMenuData();
			prepareImage(exampleStream, exampleBitmap, image);
			image.Reset();
			setStream(exampleStream, exampleBitmap, image);
			ColorApplyReset.Visibility = Visibility.Collapsed;
		}

		private void OnRotateResetClick(object sender, RoutedEventArgs e)
		{
			prepareImage(exampleStream, exampleBitmap, image);
			image.Reset();
			setStream(exampleStream, exampleBitmap, image);
			ResetRotateMenuData();
			RotateApplyReset.Visibility = Visibility.Collapsed;
		}

		private void OnColorizeResetClick(object sender, RoutedEventArgs e)
		{
			deselectColorizeGridItems();
			prepareImage(bitmapStream, bitmapImage, imageOriginal);
			imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
			setStream(bitmapStream, bitmapImage, imageOriginal);
			redForColorize = greenForColorize = blueForColorize = yellowForColorize =
				 orangeForColorize = purpleForColorize = cyanForColorize =
				 limeForColorize = false;
		}

		private void OnExposureResetClick(object sender, RoutedEventArgs e)
		{
			ResetExposureMenuData();
			ExposureApplyReset.Visibility = Visibility.Collapsed;
		}
		#endregion

		#region Checked Menu Buttons
		// The events are called when a Menu button is checked or unchecked.

		private void FiltersChecked(object sender, RoutedEventArgs e)
		{
			SelectColors.IsChecked = false;
			SelectRotations.IsChecked = false;
			SelectOptions.IsChecked = false;
			SelectColorize.IsChecked = false;
			SelectFrames.IsChecked = false;
			SelectHistogram.IsChecked = false;
			SelectExposure.IsChecked = false;
			SelectCrop.IsChecked = false;
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
			SelectExposure.IsChecked = false;
			SelectCrop.IsChecked = false;
			PopupColors.IsOpen = true;
		}

		private void ColorsUnchecked(object sender, RoutedEventArgs e)
		{
			PopupColors.IsOpen = false;
		}

		private void ExposureChecked(object sender, RoutedEventArgs e)
		{
			SelectFilters.IsChecked = false;
			SelectRotations.IsChecked = false;
			SelectOptions.IsChecked = false;
			SelectColorize.IsChecked = false;
			SelectFrames.IsChecked = false;
			SelectHistogram.IsChecked = false;
			SelectColors.IsChecked = false;
			SelectCrop.IsChecked = false;
			PopupExposure.IsOpen = true;
		}

		private void ExposureUnchecked(object sender, RoutedEventArgs e)
		{
			PopupExposure.IsOpen = false;
		}

		private void RotationsChecked(object sender, RoutedEventArgs e)
		{
			SelectFilters.IsChecked = false;
			SelectColors.IsChecked = false;
			SelectOptions.IsChecked = false;
			SelectColorize.IsChecked = false;
			SelectFrames.IsChecked = false;
			SelectHistogram.IsChecked = false;
			SelectCrop.IsChecked = false;
			SelectExposure.IsChecked = false;
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
			SelectCrop.IsChecked = false;
			SelectExposure.IsChecked = false;
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
			SelectExposure.IsChecked = false;
			SelectCrop.IsChecked = false;
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
			SelectExposure.IsChecked = false;
			SelectCrop.IsChecked = false;
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
			SelectExposure.IsChecked = false;
			SelectCrop.IsChecked = false;
			PopupHistogram.IsOpen = true;
		}

		private void HistogramUnchecked(object sender, RoutedEventArgs e)
		{
			PopupHistogram.IsOpen = false;
		}

		#endregion

		#region Frames
		// The events are called when a frame button is clicked.
		// Set standard frame to the image
		private void OnStandardClick(object sender, RoutedEventArgs e)
		{
			FramesApplyReset.Visibility = Visibility.Visible;

			if (pictureIsLoaded)
			{
				appliedFrame = "standard";
				prepareImage(bitmapStream, bitmapImage, imageOriginal);
				imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
				imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				setStream(bitmapStream, bitmapImage, imageOriginal);
			}
		}

		// Set standard frame (only UP or DOWN) to the image
		private void OnStandardUpDownClick(object sender, RoutedEventArgs e)
		{
			FramesApplyReset.Visibility = Visibility.Visible;

			if (pictureIsLoaded)
			{
				appliedFrame = "standard up down";
				prepareImage(bitmapStream, bitmapImage, imageOriginal);
				imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
				imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				setStream(bitmapStream, bitmapImage, imageOriginal);
			}
		}

		// Set standard frame (only LEFT or RIGHT) to the image
		private void OnStandardLeftRightClick(object sender, RoutedEventArgs e)
		{
			FramesApplyReset.Visibility = Visibility.Visible;

			if (pictureIsLoaded)
			{
				appliedFrame = "standard left right";
				prepareImage(bitmapStream, bitmapImage, imageOriginal);
				imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
				imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				setStream(bitmapStream, bitmapImage, imageOriginal);
			}
		}

		// Set darkness frame to the image
		private void OnDarknessClick(object sender, RoutedEventArgs e)
		{
			FramesApplyReset.Visibility = Visibility.Visible;

			if (pictureIsLoaded)
			{
				appliedFrame = "darkness";
				prepareImage(bitmapStream, bitmapImage, imageOriginal);
				imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
				imageOriginal.Frames_DarknessLeftSide((int)FrameWidthPercent.Value);
				imageOriginal.Frames_DarknessTopSide((int)FrameWidthPercent.Value);
				imageOriginal.Frames_DarknessRightSide((int)FrameWidthPercent.Value);
				imageOriginal.Frames_DarknessBottomSide((int)FrameWidthPercent.Value);
				setStream(bitmapStream, bitmapImage, imageOriginal);
			}
		}

		// Set darkness frame (only left or right) to the image
		private void OnDarknessLeftRightClick(object sender, RoutedEventArgs e)
		{
			FramesApplyReset.Visibility = Visibility.Visible;

			if (pictureIsLoaded)
			{
				appliedFrame = "darkness left right";
				prepareImage(bitmapStream, bitmapImage, imageOriginal);
				imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
				imageOriginal.Frames_DarknessLeftSide((int)FrameWidthPercent.Value);
				imageOriginal.Frames_DarknessRightSide((int)FrameWidthPercent.Value);
				setStream(bitmapStream, bitmapImage, imageOriginal);
			}
		}

		// Set darkness frame (only up or down) to the image
		private void OnDarknessUpDownSidesClick(object sender, RoutedEventArgs e)
		{
			FramesApplyReset.Visibility = Visibility.Visible;

			if (pictureIsLoaded)
			{
				appliedFrame = "darkness up down";
				prepareImage(bitmapStream, bitmapImage, imageOriginal);
				imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
				imageOriginal.Frames_DarknessTopSide((int)FrameWidthPercent.Value);
				imageOriginal.Frames_DarknessBottomSide((int)FrameWidthPercent.Value);
				setStream(bitmapStream, bitmapImage, imageOriginal);
			}
		}

		// Set smooth darkness frame to the image
		private void OnSmoothDarknessClick(object sender, RoutedEventArgs e)
		{
			FramesApplyReset.Visibility = Visibility.Visible;

			if (pictureIsLoaded)
			{
				appliedFrame = "smooth darkness";
				prepareImage(bitmapStream, bitmapImage, imageOriginal);
				imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
				imageOriginal.Frames_SmoothDarkness((int)FrameWidthPercent.Value);
				setStream(bitmapStream, bitmapImage, imageOriginal);
			}
		}

		// Set standard frame with smooth angles to the image
		private void OnStandardAngleClick(object sender, RoutedEventArgs e)
		{
			FramesApplyReset.Visibility = Visibility.Visible;

			if (pictureIsLoaded)
			{
				appliedFrame = "standard angle";
				prepareImage(bitmapStream, bitmapImage, imageOriginal);
				imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
				imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				imageOriginal.Frames_StandartAngle(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				setStream(bitmapStream, bitmapImage, imageOriginal);
			}
		}

		// Set smooth angles frame to the image
		private void OnAngleClick(object sender, RoutedEventArgs e)
		{
			FramesApplyReset.Visibility = Visibility.Visible;

			if (pictureIsLoaded)
			{
				appliedFrame = "angle";
				prepareImage(bitmapStream, bitmapImage, imageOriginal);
				imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
				imageOriginal.Frames_Angle(Frame_GetFrameColor(), (int)FrameWidthPercent.Value);
				setStream(bitmapStream, bitmapImage, imageOriginal);
			}
		}

		// Apply the frame on the image
		private void OnApplyFramesClick(object sender, RoutedEventArgs e)
		{
			imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Frame = " + FrameWidthPercent.Value + "," + appliedFrameColor + "," + appliedFrame);
            setExampleImage();
			setFilterBitmaps();
			FramesApplyReset.Visibility = Visibility.Collapsed;
			ResetFrameMenuData();
		}

		// Reset the image (return the pixels before applying the frame)
		private void OnResetFramesClick(object sender, RoutedEventArgs e)
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
		private void BlackFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "black";
		}

		// If gray color is selected
		private void GrayFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "gray";
		}

		// If white color is selected
		private void WhiteFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "white";
		}

		// If blue color is selected
		private void BlueFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "blue";
		}

		// If lime color is selected
		private void LimeFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "lime";
		}

		// If yellow color is selected
		private void YellowFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "yellow";
		}

		// If cyan color is selected
		private void CyanFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "cyan";
		}

		// If magenta color is selected
		private void MagentaFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "magenta";
		}

		// If silver color is selected
		private void SilverFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "silver";
		}

		// If maroon color is selected
		private void MaroonFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "maroon";
		}

		// If olive color is selected
		private void OliveFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "olive";
		}

		// If green color is selected
		private void GreenFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "green";
		}

		// If purple color is selected
		private void PurpleFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "purple";
		}

		// If teal color is selected
		private void TealFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "teal";
		}

		// If navy color is selected
		private void NavyFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "navy";
		}

		// If red color is selected
		private void RedFrameTapped(object sender, TappedRoutedEventArgs e)
		{
			appliedFrameColor = "red";
		}

		// Get the B G R value of selected color
		private byte[] Frame_GetFrameColor()
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

		#region Rotate
		// The events are called when a Rotate button is clicked.

		private void OnHFlipClick(object sender, RoutedEventArgs e)
		{
			appliedRotations = "hflip";
			prepareImage(exampleStream, exampleBitmap, image);
			image.HFlip();
			setStream(exampleStream, exampleBitmap, image);
		}

		private void OnVFlipClick(object sender, RoutedEventArgs e)
		{
			appliedRotations = "vflip";
			prepareImage(exampleStream, exampleBitmap, image);
			image.VFlip();
			setStream(exampleStream, exampleBitmap, image);
		}

		#endregion

		#region Exposure
		// The event is called when the Gama slider or Brighr slider is changed.
		private void OnExposureChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				prepareImage(exampleStream, exampleBitmap, image);
				image.dstPixels = (byte[])image.srcPixels.Clone();
				image.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
				// We check if the changed value 
				// is higher than 0 - we call the brightness function
				// is lower than 0  - we call the darkness function
				// And finally we save the new byte array to the image.
				if (brightSlider.Value < 0)
				{
					appliedColors = "gammadarken";
					image.Darken(brightSlider.Value);
				}
				else if (brightSlider.Value >= 0)
				{
					appliedColors = "gammalighten";
					image.Lighten(brightSlider.Value);
				}
				setStream(exampleStream, exampleBitmap, image);
			}
		}

		#endregion

		#region Back buttons

		private void BackPopupClicked(object sender, RoutedEventArgs e)
		{
			// If the back popup button is clicked, close all popups.
			SelectColors.IsChecked = false;
			SelectFilters.IsChecked = false;
			SelectRotations.IsChecked = false;
			SelectOptions.IsChecked = false;
			SelectColorize.IsChecked = false;
			SelectFrames.IsChecked = false;
			SelectHistogram.IsChecked = false;
			SelectExposure.IsChecked = false;
		}

		private void BackFeedbackClicked(object sender, RoutedEventArgs e)
		{
			// If the back feedback button is clicked, close the feedback and show the settings charm.
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
			if (scale.ScaleX > 0.9 && scale.ScaleY > 0.9)
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
			// This creates temporary Streams and WriteableBitmap objects for every filter available.
			// We set the bitmaps as source to the XAML Image objects.
			// After this, we apply different filter for each of the WriteableBitmap objects.

			RemedyImage filterimage = new RemedyImage();
			uint newWidth = (uint)bitmapImage.PixelWidth;
			uint newHeight = (uint)bitmapImage.PixelHeight;

			while (newWidth > 150 && newHeight > 150)
			{
				newWidth = newWidth / 2;
				newHeight = newHeight / 2;
			}

			Stream
			blackWhiteStream = null,
			emboss2Stream = null,
			embossStream = null,
			invertStream = null,
			blurStream = null,
			blur2Stream = null,
			sharpenStream = null,
			noiseStream = null;

			WriteableBitmap
			blackWhiteBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			embossBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			emboss2Bitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			invertBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			blurBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			blur2Bitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			sharpenBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			noiseBitmap = await ResizeImage(bitmapImage, newWidth, newHeight);

			blackWhiteFilter.Source = blackWhiteBitmap;
			embossFilter.Source = embossBitmap;
			emboss2Filter.Source = emboss2Bitmap;
			invertFilter.Source = invertBitmap;
			blurFilter.Source = blurBitmap;
			blur2Filter.Source = blur2Bitmap;
			sharpenFilter.Source = sharpenBitmap;
			noiseFilter.Source = noiseBitmap;

			blackWhiteStream = blackWhiteBitmap.PixelBuffer.AsStream();
			embossStream = embossBitmap.PixelBuffer.AsStream();
			emboss2Stream = emboss2Bitmap.PixelBuffer.AsStream();
			invertStream = invertBitmap.PixelBuffer.AsStream();
			blurStream = blurBitmap.PixelBuffer.AsStream();
			blur2Stream = blur2Bitmap.PixelBuffer.AsStream();
			sharpenStream = sharpenBitmap.PixelBuffer.AsStream();
			noiseStream = noiseBitmap.PixelBuffer.AsStream();

			initializeBitmap(blackWhiteStream, blackWhiteBitmap, filterimage);
			initializeBitmap(embossStream, embossBitmap, filterimage);
			initializeBitmap(emboss2Stream, emboss2Bitmap, filterimage);
			initializeBitmap(invertStream, invertBitmap, filterimage);
			initializeBitmap(blurStream, blurBitmap, filterimage);
			initializeBitmap(blur2Stream, blur2Bitmap, filterimage);
			initializeBitmap(sharpenStream, sharpenBitmap, filterimage);
			initializeBitmap(noiseStream, noiseBitmap, filterimage);

			prepareImage(blackWhiteStream, blackWhiteBitmap, filterimage);
			setStream(blackWhiteStream, blackWhiteBitmap, filterimage);

			doFilter(blackWhiteStream, blackWhiteBitmap, filterimage, "blackwhite");
			doFilter(embossStream, embossBitmap, filterimage, "emboss");
			doFilter(emboss2Stream, emboss2Bitmap, filterimage, "emboss2");
			doFilter(invertStream, invertBitmap, filterimage, "invert");
			doFilter(blurStream, blurBitmap, filterimage, "blur");
			doFilter(blur2Stream, blur2Bitmap, filterimage, "blur2");
			doFilter(sharpenStream, sharpenBitmap, filterimage, "sharpen");
			doFilter(noiseStream, noiseBitmap, filterimage, "noise");

			Stream
			hardNoiseStream = null,
			retroStream = null,
			darkenStream = null,
			edgeDetectStream = null,
			edgeEnhanceStream = null,
			brightenStream = null,
			shadowStream = null,
			crystalStream = null;

			WriteableBitmap
			hardNoiseBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			edgeDetectBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			edgeEnhanceBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			retroBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			darkenBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			brightenBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			shadowBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
			crystalBitmap = await ResizeImage(bitmapImage, newWidth, newHeight);

			hardNoiseFilter.Source = hardNoiseBitmap;
			edgeDetectFilter.Source = edgeDetectBitmap;
			edgeEnhanceFilter.Source = edgeEnhanceBitmap;
			retroFilter.Source = retroBitmap;
			darkenFilter.Source = darkenBitmap;
			brightenFilter.Source = brightenBitmap;
			shadowFilter.Source = shadowBitmap;
			crystalFilter.Source = crystalBitmap;

			hardNoiseStream = hardNoiseBitmap.PixelBuffer.AsStream();
			edgeDetectStream = edgeDetectBitmap.PixelBuffer.AsStream();
			edgeEnhanceStream = edgeEnhanceBitmap.PixelBuffer.AsStream();
			retroStream = retroBitmap.PixelBuffer.AsStream();
			darkenStream = darkenBitmap.PixelBuffer.AsStream();
			brightenStream = brightenBitmap.PixelBuffer.AsStream();
			shadowStream = shadowBitmap.PixelBuffer.AsStream();
			crystalStream = crystalBitmap.PixelBuffer.AsStream();

			initializeBitmap(hardNoiseStream, hardNoiseBitmap, filterimage);
			initializeBitmap(edgeDetectStream, edgeDetectBitmap, filterimage);
			initializeBitmap(edgeEnhanceStream, edgeEnhanceBitmap, filterimage);
			initializeBitmap(retroStream, retroBitmap, filterimage);
			initializeBitmap(darkenStream, darkenBitmap, filterimage);
			initializeBitmap(brightenStream, brightenBitmap, filterimage);
			initializeBitmap(shadowStream, shadowBitmap, filterimage);
			initializeBitmap(crystalStream, crystalBitmap, filterimage);

			prepareImage(hardNoiseStream, hardNoiseBitmap, filterimage);
			setStream(hardNoiseStream, hardNoiseBitmap, filterimage);

			doFilter(hardNoiseStream, hardNoiseBitmap, filterimage, "hardNoise");
			doFilter(edgeDetectStream, edgeDetectBitmap, filterimage, "EdgeDetect");
			doFilter(edgeEnhanceStream, edgeEnhanceBitmap, filterimage, "EdgeEnhance");
			doFilter(retroStream, retroBitmap, filterimage, "retro");
			doFilter(darkenStream, darkenBitmap, filterimage, "darken");
			doFilter(brightenStream, brightenBitmap, filterimage, "brighten");
			doFilter(shadowStream, shadowBitmap, filterimage, "shadow");
			doFilter(crystalStream, crystalBitmap, filterimage, "crystal");
		}

		private async void initializeBitmap(Stream givenStream, WriteableBitmap givenBitmap, RemedyImage givenImage)
		{
			// This makes the required operations for initializing the WriteableBitmap.
			givenStream = givenBitmap.PixelBuffer.AsStream();
			givenImage.srcPixels = new byte[(uint)givenStream.Length];
			await givenStream.ReadAsync(givenImage.srcPixels, 0, givenImage.srcPixels.Length);
		}

		private void doFilter(Stream givenStream, WriteableBitmap givenBitmap, RemedyImage givenImage, string filter)
		{
			// Filter the passed image with the passed filter as a string.
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
				case "noise":
					doNoise(givenStream, givenBitmap, givenImage);
					break;
				case "hardNoise":
					doHardNoise(givenStream, givenBitmap, givenImage);
					break;
				case "EdgeDetect":
					doEdgeDetect(givenStream, givenBitmap, givenImage);
					break;
				case "EdgeEnhance":
					doEdgeEnhance(givenStream, givenBitmap, givenImage);
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

		private void noiseChecked(object sender, RoutedEventArgs e)
		{
			appliedFilters = "noise";
			deselectFilters();
			FilterApplyReset.Visibility = Visibility.Visible;
		}

		private void hardNoiseChecked(object sender, RoutedEventArgs e)
		{
			appliedFilters = "hardNoise";
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
			var filterSender = sender as ToggleButton;
			filterSender.IsChecked = false;
			FilterApplyReset.Visibility = Visibility.Collapsed;
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
			if (without != "noise")
				noiseCheck.IsChecked = false;
			if (without != "hardNoise")
				hardNoiseCheck.IsChecked = false;
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
			// A simple "hot key".
			// When the user double-clicks on the interface, 
			// the currently opened popup closes.
			deselectPopups();
		}

		private void deselectPopups()
		{
			// Close all popups.
			SelectColors.IsChecked = false;
			SelectFilters.IsChecked = false;
			SelectRotations.IsChecked = false;
			SelectOptions.IsChecked = false;
			SelectColorize.IsChecked = false;
			SelectFrames.IsChecked = false;
			SelectHistogram.IsChecked = false;
			SelectExposure.IsChecked = false;
		}

		private void OnImagePointerWheelChanged(object sender, PointerRoutedEventArgs e)
		{
			// Event for the mouse wheel. 
			// It zooms in or out the image.
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
				if (scale.ScaleX > 0.9 && scale.ScaleY > 0.9)
				{
					scale.ScaleX = scale.ScaleX - 0.1;
					scale.ScaleY = scale.ScaleY - 0.1;
				}
				if (ZoomOut.Visibility == Visibility.Visible && scale.ScaleX <= 0.9)
				{
					ZoomOut.Visibility = Visibility.Collapsed;
				}
			}
		}

		#region Image Options
		private async void SetLockPic_Clicked(object sender, RoutedEventArgs e)
		{
			// This sets the current image as a wallpaper on the lock screen of the current user and inform him that everything was okay.
			await SaveFile(false);
			await LockScreen.SetImageFileAsync(file);
			MessageDialog messageDialog = new MessageDialog("Picture set! :)", "All done");
			await messageDialog.ShowAsync();
			await deleteUsedFile();
		}

		private async void SetAccountPic_Clicked(object sender, RoutedEventArgs e)
		{
			// This sets the current image as an avatar of the current user and inform him that everything was okay.
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
			// Restore the original image.
			RestoreOriginalBitmap();
		}

		private async void RestoreOriginalBitmap()
		{
			// Reset the current image.
			imageOriginal.srcPixels = (byte[])uneditedImage.srcPixels.Clone();
			imageOriginal.dstPixels = (byte[])uneditedImage.dstPixels.Clone();
			bitmapStream = uneditedStream;
			bitmapImage = uneditedBitmap;
			prepareImage(bitmapStream, bitmapImage, imageOriginal);
			setStream(bitmapStream, bitmapImage, imageOriginal);
			displayImage.Source = bitmapImage;

			exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));
			exampleStream = exampleBitmap.PixelBuffer.AsStream();
			image.srcPixels = new byte[(uint)exampleStream.Length];
			await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);
			prepareImage(exampleStream, exampleBitmap, image);
			setStream(exampleStream, exampleBitmap, image);
			setElements(ColorsExamplePicture, exampleBitmap);
			setElements(RotationsExamplePicture, exampleBitmap);
			setElements(ExposureExamplePicture, exampleBitmap);

			setFilterBitmaps();
			this.selectedRegion.ResetCorner(0, 0, displayImage.ActualWidth, displayImage.ActualHeight);
		}

		#endregion

		private async Task deleteUsedFile()
		{
			// Deletes the temporary created file.
			if (imageOriginal.dstPixels != null)
			{
				file = await ApplicationData.Current.LocalFolder.GetFileAsync("temp.jpg");
				await file.DeleteAsync();
			}
		}

		private void HistogramClicked(object sender, RoutedEventArgs e)
		{
			// Equalize the histogram of the current image.
			SelectHistogram.IsChecked = false;
			prepareImage(bitmapStream, bitmapImage, imageOriginal);
			imageOriginal.MakeHistogramEqualization();
			setStream(bitmapStream, bitmapImage, imageOriginal);
			prepareImage(exampleStream, exampleBitmap, image);
			image.MakeHistogramEqualization();
			setStream(exampleStream, exampleBitmap, image);
			image.srcPixels = (byte[])image.dstPixels.Clone();
			imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            effectsApplied.Add("Histogram = true");
            ArchiveAddArray();
			setFilterBitmaps();
		}

		#region Crop region

		#region Select Region methods

		private void Corner_PointerPressed(object sender, PointerRoutedEventArgs e)
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

		private void Corner_PointerReleased(object sender, PointerRoutedEventArgs e)
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

			CropApply.Visibility = Visibility.Visible;
			e.Handled = true;


		}

		void selectRegion_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			this.selectedRegion.UpdateSelectedRect(e.Delta.Scale, e.Delta.Translation.X, e.Delta.Translation.Y);
			e.Handled = true;
		}

		void selectRegion_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			CropApply.Visibility = Visibility.Visible;
		}

		#endregion

		private void CropChecked(object sender, RoutedEventArgs e)
		{
			// Called when the Crop button is checked.
			deselectPopups();
			Crop.Visibility = Visibility.Visible;
			imageCanvas.Visibility = Visibility.Visible;
			displayGrid.Margin = new Thickness(15);
			ResetZoomPos();
		}

		private void CropUnchecked(object sender, RoutedEventArgs e)
		{
			// Called when the Crop button is unchecked.
			Crop.Visibility = Visibility.Collapsed;
			imageCanvas.Visibility = Visibility.Collapsed;
			this.selectedRegion.ResetCorner(0, 0, displayImage.ActualWidth, displayImage.ActualHeight);
			displayGrid.Margin = new Thickness(0);
		}

		async void UpdatePreviewImage()
		{
			// Updates the current image with the new cropped one.
			ImageLoadingRing.IsActive = true;
			await SaveFile(false);

			double sourceImageWidthScale = imageCanvas.Width / this.sourceImagePixelWidth;
			double sourceImageHeightScale = imageCanvas.Height / this.sourceImagePixelHeight;

			Size previewImageSize = new Size(
				this.selectedRegion.SelectedRect.Width / sourceImageWidthScale,
				this.selectedRegion.SelectedRect.Height / sourceImageHeightScale);

			if (previewImageSize.Width <= imageCanvas.Width &&
				previewImageSize.Height <= imageCanvas.Height)
			{
				displayImage.Stretch = Windows.UI.Xaml.Media.Stretch.None;
			}
			else
			{
				displayImage.Stretch = Windows.UI.Xaml.Media.Stretch.Uniform;
			}

			bitmapImage = await CropBitmap.GetCroppedBitmapAsync(
				   file,
				   new Point(this.selectedRegion.SelectedRect.X / sourceImageWidthScale, this.selectedRegion.SelectedRect.Y / sourceImageHeightScale),
				   previewImageSize,
				   1);

			// After the cropping is done, we set the new bitmapImage objects again.
			bitmapStream = bitmapImage.PixelBuffer.AsStream();
			imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
			await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
			imageOriginal.Reset();

			exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));

			exampleStream = exampleBitmap.PixelBuffer.AsStream();
			image.srcPixels = new byte[(uint)exampleStream.Length];
			await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);

			prepareImage(exampleStream, exampleBitmap, image);
			setStream(exampleStream, exampleBitmap, image);
			setElements(ColorsExamplePicture, exampleBitmap);
			setElements(RotationsExamplePicture, exampleBitmap);
			setElements(ExposureExamplePicture, exampleBitmap);
			setFilterBitmaps();

			SelectCrop.IsChecked = false;

			sourceImagePixelHeight = (uint)bitmapImage.PixelHeight;
			sourceImagePixelWidth = (uint)bitmapImage.PixelWidth;

			ImageLoadingRing.IsActive = false;
			displayImage.Source = bitmapImage;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			// Called when the current page is displayed.
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
			// Called when the current page is removed.
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
			// Called when the original image size is changed.
			// It calculates the new width and height.

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
			// Called when the user has dragged the crop corner.
			double widthScale = imageCanvas.Width / sourceImagePixelWidth;
			double heightScale = imageCanvas.Height / sourceImagePixelHeight;

			this.selectInfoInBitmapText.Text = string.Format("Resolution: {0}x{1}",
				Math.Floor(this.selectedRegion.SelectedRect.Width / widthScale),
				Math.Floor(this.selectedRegion.SelectedRect.Height / heightScale));
		}

		private void saveImageButton_Click(object sender, RoutedEventArgs e)
		{
			// When the user clicks Apply, the image is cropped.
			UpdatePreviewImage();
		}

		#endregion

		private void deselectMenu()
		{
			// Deselect all Menu Toggle buttons.
			SelectColors.IsChecked = false;
			SelectRotations.IsChecked = false;
			SelectOptions.IsChecked = false;
			SelectColorize.IsChecked = false;
			SelectFrames.IsChecked = false;
			SelectHistogram.IsChecked = false;
			SelectFilters.IsChecked = false;
			SelectCrop.IsChecked = false;
		}

		#region Resizing the image

		private void OnNewWidthTextChanged(object sender, TextChangedEventArgs e)
		{
			int temp;

			if (keepProportions && newWidth.Text != "" && int.TryParse(newWidth.Text, out temp))
			{
				newHeight.Text = (Math.Round(temp / widthHeightRatio)).ToString();
			}
			keepProportions = !keepProportions;
			if (newWidth.Text != "")
			{
				ApplyResize.Visibility = Visibility.Visible;
			}
			else
			{
				ApplyResize.Visibility = Visibility.Collapsed;
			}
		}

		private void OnNewHeightTextChanged(object sender, TextChangedEventArgs e)
		{
			int temp;
			if (keepProportions && newHeight.Text != "" && int.TryParse(newHeight.Text, out temp))
			{
				newWidth.Text = (Math.Round(temp * widthHeightRatio)).ToString();
			}
			keepProportions = !keepProportions;
			if (newHeight.Text != "")
			{
				ApplyResize.Visibility = Visibility.Visible;
			}
			else
			{
				ApplyResize.Visibility = Visibility.Collapsed;
			}
		}

		private void OnKeepPropsUnchecked(object sender, RoutedEventArgs e)
		{
			keepProportions = false;
		}

		private void OnKeepPropsChecked(object sender, RoutedEventArgs e)
		{
			keepProportions = true;
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
			// Resize the current image.
			int resizeWidth = Convert.ToInt32(newWidth.Text);
			int resizeHeight = Convert.ToInt32(newHeight.Text);
			ApplyResize.Visibility = Visibility.Collapsed;

			bitmapImage = await ResizeImage(bitmapImage, (uint)(resizeWidth), (uint)(resizeHeight));
			bitmapStream = bitmapImage.PixelBuffer.AsStream();
			imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
			image.srcPixels = new byte[(uint)exampleStream.Length];
			await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
			prepareImage(bitmapStream, bitmapImage, imageOriginal);
			setStream(bitmapStream, bitmapImage, imageOriginal);
			displayImage.Source = bitmapImage;
			setFilterBitmaps();
		}

		#endregion

		#region Colorize

		#region Colorize events
		// Events for checking and unchecking the colorize rectangles.
		private void blueColorize_Checked(object sender, RoutedEventArgs e)
		{
			blueForColorize = true;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			blueRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
		}
		private void blueColorize_Unchecked(object sender, RoutedEventArgs e)
		{
			blueForColorize = false;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			blueRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255));
		}

		private void redColorize_Checked(object sender, RoutedEventArgs e)
		{
			redForColorize = true;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			redRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
		}
		private void redColorize_Unchecked(object sender, RoutedEventArgs e)
		{
			redForColorize = false;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			redRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
		}

		private void yellowColorize_Checked(object sender, RoutedEventArgs e)
		{
			yellowForColorize = true;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			yellowRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
		}
		private void yellowColorize_Unchecked(object sender, RoutedEventArgs e)
		{
			yellowForColorize = false;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			yellowRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0));
		}

		private void orangeColorize_Checked(object sender, RoutedEventArgs e)
		{
			orangeForColorize = true;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			orangeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 116, 0));
		}
		private void orangeColorize_Unchecked(object sender, RoutedEventArgs e)
		{
			orangeForColorize = false;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			orangeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 116, 0));
		}

		private void greenColorize_Checked(object sender, RoutedEventArgs e)
		{
			greenForColorize = true;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			greenRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 120, 0));
		}
		private void greenColorize_Unchecked(object sender, RoutedEventArgs e)
		{
			greenForColorize = false;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			greenRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 90, 0));
		}

		private void cyanColorize_Checked(object sender, RoutedEventArgs e)
		{
			cyanForColorize = true;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			cyanRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255));
		}
		private void cyanColorize_Unchecked(object sender, RoutedEventArgs e)
		{
			cyanForColorize = false;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			cyanRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255));
		}

		private void purpleColorize_Checked(object sender, RoutedEventArgs e)
		{
			purpleForColorize = true;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			purpleRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 255));
		}
		private void purpleColorize_Unchecked(object sender, RoutedEventArgs e)
		{
			purpleForColorize = false;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			purpleRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 255));
		}

		private void limeColorize_Checked(object sender, RoutedEventArgs e)
		{
			limeForColorize = true;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			limeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 25, 255, 25));
		}
		private void limeColorize_Unchecked(object sender, RoutedEventArgs e)
		{
			limeForColorize = false;
			doColorize(bitmapStream, bitmapImage, imageOriginal);
			limeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 25, 255, 25));
		}

		#endregion

		private void deselectColorizeGridItems()
		{
			// Deselect all colorize rectangles and return their original color.
			redForColorize = greenForColorize = blueForColorize = yellowForColorize =
				 orangeForColorize = purpleForColorize = cyanForColorize =
				 limeForColorize = false;
			blueColorize.IsChecked = false;
			redColorize.IsChecked = false;
			greenColorize.IsChecked = false;
			yellowColorize.IsChecked = false;
			orangeColorize.IsChecked = false;
			purpleColorize.IsChecked = false;
			cyanColorize.IsChecked = false;
			limeColorize.IsChecked = false;
			blueRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255));
			redRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
			greenRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 90, 0));
			yellowRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0));
			orangeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 116, 0));
			purpleRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 255));
			cyanRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255));
			limeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 25, 255, 25));
			ColorizeApplyReset.Visibility = Visibility.Collapsed;
		}

		private void doColorize(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
		{
			// Call the colorize function
			prepareImage(stream, bitmap, givenImage);
			givenImage.Colorize(blueForColorize, redForColorize, greenForColorize, yellowForColorize,
										orangeForColorize, purpleForColorize, cyanForColorize, limeForColorize);
			setStream(stream, bitmap, givenImage);
			ColorizeApplyReset.Visibility = Visibility.Visible;
		}

		#endregion

		private void FramesBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			// This event completes when the mouse pointer enter the frame border.
			var borderSender = sender as Border;
			borderSender.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
		}

		private void FramesBorder_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			// This event completes when the mouse pointer exit the frame border.
			var borderSender = sender as Border;
			borderSender.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 25, 112, 0));
		}

		#region Export/Import
		private void OnExportButtonClick(object sender, RoutedEventArgs e)
		{
            if (archive_current_index != archive_data.Count - 1)
            {
                archive_data.RemoveRange(archive_current_index + 1, archive_data.Count - 1 - archive_current_index);
                effectsApplied.RemoveRange(archive_current_index, effectsApplied.Count - archive_current_index); // Here we don`t save the start image, so we have -1 index of archive_current_index
            }
            configFile.Export(effectsApplied);
		}

		private void onImportButtonClick(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < configFile.effects.Count; i += 2)
			{
				checkEffect(i);
			}
			setFilterBitmaps();
		}

		private void checkEffect(int i)
		{
			string[] temp = new string[10];
			switch (configFile.effects[i])
			{
				case "filter":
					ApplyFilter(configFile.effects[i + 1]);
					break;

				case "color":
					temp = configFile.effects[i + 1].Split(',');
					BlueColorSlider.Value = Convert.ToDouble(temp[0]);
					GreenColorSlider.Value = Convert.ToDouble(temp[1]);
					RedColorSlider.Value = Convert.ToDouble(temp[2]);
					ApplyColor();
					break;

				case "contrast":
					temp = configFile.effects[i + 1].Split(',');
					BlueContrastSlider.Value = Convert.ToDouble(temp[0]);
					GreenContrastSlider.Value = Convert.ToDouble(temp[1]);
					RedContrastSlider.Value = Convert.ToDouble(temp[2]);
					ApplyColor();
					break;

				case "exposure":
					temp = configFile.effects[i + 1].Split(',');
					brightSlider.Value = Convert.ToDouble(temp[0]);
					BlueGammaSlider.Value = Convert.ToDouble(temp[1]);
					GreenGammaSlider.Value = Convert.ToDouble(temp[2]);
					RedGammaSlider.Value = Convert.ToDouble(temp[3]);
					if (brightSlider.Value < 0)
						ApplyExposure("gammadarken");
					else
						ApplyExposure("gammalighten");
					break;

				case "flip":
					temp = configFile.effects[i + 1].Split(',');
					ApplyRotate(temp[0]);
					break;

				case "colorize":
					temp = configFile.effects[i + 1].Split(',');
					for (int k = 0; k < temp.Length; k++)
					{
						checkColorizeColor(temp[k]);
					}
					doColorize(bitmapStream, bitmapImage, imageOriginal);
					ApplyColorize();
					break;

				default: break;
			}
		}

		private void checkColorizeColor(string color)
		{
			switch (color)
			{
				case "red":
					redForColorize = true;
					redRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
					break;
				case "blue":
					blueForColorize = true;
					blueRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
					break;
				case "green":
					greenForColorize = true;
					greenRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 120, 0));
					break;
				case "lime":
					limeForColorize = true;
					limeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 25, 255, 25));
					break;
				case "yellow":
					yellowForColorize = true;
					yellowRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
					break;
				case "cyan":
					cyanForColorize = true;
					cyanRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255));
					break;
				case "orange":
					orangeForColorize = true;
					orangeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 116, 0));
					break;
				case "purple":
					purpleForColorize = true;
					purpleRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 255));
					break;
				default:
					break;
			}
		}

		private void onImportFileSelectButtonClick(object sender, RoutedEventArgs e)
		{
			configFile.Import(importFileName);
			importFilePanel.Visibility = Visibility.Visible;
		}
		#endregion

	}
	#endregion
}
#endregion
