using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using RemedyPic.Common;


#region Namespace RemedyPic
namespace RemedyPic
{
	#region MainPage class
	public sealed partial class MainPage : RemedyPic.Common.LayoutAwarePage
	{

		#region Variables

		// mruToken is used for LoadState and SaveState functions.
		private string mruToken = null;
		private string appliedFilters = null, appliedColors = null;
		// bitmapImage is the image that is edited in RemedyPic.
		private WriteableBitmap bitmapImage, exampleBitmap;

		// bitmapStream is used to save the pixel stream to bitmapImage.
		Stream bitmapStream, exampleStream;
		static readonly long cycleDuration = TimeSpan.FromSeconds(3).Ticks;

		// This is true if the user load a picture.
		bool pictureIsLoaded = false;

		FilterFunctions image = new FilterFunctions();
		FilterFunctions imageOriginal = new FilterFunctions();
		WriteableBitmap bitmap = new WriteableBitmap(500, 250);
		#endregion

		public MainPage()
		{
			this.InitializeComponent();
		}

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
				filePicker.FileTypeFilter.Add(".jpg");
				filePicker.FileTypeFilter.Add(".png");
				filePicker.FileTypeFilter.Add(".bmp");
				filePicker.FileTypeFilter.Add(".jpeg");
				var result = await filePicker.PickSingleFileAsync();
				bitmapImage = new WriteableBitmap(1, 1);

				if (result != null)
				// Result is null if user cancels the file picker.
				{
					Windows.Storage.Streams.IRandomAccessStream fileStream =
							await result.OpenAsync(Windows.Storage.FileAccessMode.Read);
					bitmapImage.SetSource(fileStream);
					RandomAccessStreamReference streamRef = RandomAccessStreamReference.CreateFromFile(result);
					// TO DO: Find place for File name...
					// setFileProperties(result);

					// If the interface was changed from previous image, it should be resetted.
					resetInterface();
					// Show the interface after the picture is loaded.
					contentGrid.Visibility = Visibility.Visible;
					pictureIsLoaded = true;
					doAllCalculations();
					// Set the border of the image panel.
					//border.BorderThickness = new Thickness(1, 1, 1, 1);
					//border.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Black);

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
			exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 2), (uint)(bitmapImage.PixelHeight / 2));
			displayImage.Source = bitmapImage;
			Stream stream = exampleBitmap.PixelBuffer.AsStream();
			bitmapStream = bitmapImage.PixelBuffer.AsStream();
			imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
			image.srcPixels = new byte[(uint)stream.Length];
			await stream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);
			await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
			setElements(FiltersExamplePicture, exampleBitmap);
			setElements(ColorsExamplePicture, exampleBitmap);
			prepareImage(exampleStream, exampleBitmap, image);
			setStream(exampleStream, exampleBitmap);
		}


		private void setElements(Windows.UI.Xaml.Controls.Image imageElement, WriteableBitmap source)
		{
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
				setStream(exampleStream, exampleBitmap);
				resetInterface();
				changeButton(ref invertButton);
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
				setStream(exampleStream, exampleBitmap);
				resetInterface();
				changeButton(ref BlackAndWhiteButton);
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
				setStream(exampleStream, exampleBitmap);
				resetInterface();
				changeButton(ref embossButton);
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

				setStream(exampleStream, exampleBitmap);

				resetInterface();
				changeButton(ref SharpenButton);
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

				setStream(exampleStream, exampleBitmap);

				resetInterface();
				changeButton(ref blurButton);
			}
		}

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

				setStream(exampleStream, exampleBitmap);
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

				// Only execute if there is a picture that is loaded
				if (pictureIsLoaded)
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
					StorageFile file = await savePicker.PickSaveFileAsync();
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

						IRandomAccessStream writeStream = await file.OpenAsync(FileAccessMode.ReadWrite);
						BitmapEncoder encoder = await BitmapEncoder.CreateAsync(fileType, writeStream);
						encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
														   (uint)bitmapImage.PixelWidth, (uint)bitmapImage.PixelHeight, 96.0, 96.0, imageOriginal.dstPixels);
						// Flush all the data to the encoder(file)
						await encoder.FlushAsync();
					}
				}
			}
			else
			{
				MessageDialog messageDialog = new MessageDialog("Can't save in snapped state. Please unsnap the app and try again", "Close");
				await messageDialog.ShowAsync();
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
				setStream(exampleStream, exampleBitmap);
			}
		}
		#endregion

		void setFileProperties(Windows.Storage.StorageFile file)
		{
			// This sets the file name to the text box
			fileName.Text = file.DisplayName;
		}

		void setStream(Stream givenStream, WriteableBitmap givenBitmap)
		{
			// This sets the pixels to the bitmap
			givenStream.Seek(0, SeekOrigin.Begin);
			if (givenBitmap == bitmapImage)
				givenStream.Write(imageOriginal.dstPixels, 0, imageOriginal.dstPixels.Length);
			else
				givenStream.Write(image.dstPixels, 0, image.dstPixels.Length);
			givenBitmap.Invalidate();
			if (Filters.Visibility == Visibility.Visible)
				FilterApplyReset.Visibility = Visibility.Visible;
			else if (Colors.Visibility == Visibility.Visible)
				ColorApplyReset.Visibility = Visibility.Visible;
		}

		void prepareImage(Stream stream, WriteableBitmap bitmap, FilterFunctions image)
		{
			// This calculates the width and height of the bitmap image
			// and sets the Stream and the pixels byte array
			image.width = (int)bitmap.PixelWidth;
			image.height = (int)bitmap.PixelHeight;
			exampleStream = exampleBitmap.PixelBuffer.AsStream();
			bitmapStream = bitmapImage.PixelBuffer.AsStream();
			imageOriginal.dstPixels = new byte[4 * bitmap.PixelWidth * bitmap.PixelHeight];
			image.dstPixels = new byte[4 * bitmap.PixelWidth * bitmap.PixelHeight];
			image.Reset();
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
				setStream(exampleStream, exampleBitmap);
				resetInterface();
			}
			appliedFilters = null;
			appliedColors = null;
		}

		private void OnApplyClick(object sender, RoutedEventArgs e)
		{
			// This applies the current dstPixels array to the source pixel array
			// so the user can apply new functions over the image.
			if (pictureIsLoaded)
			{
				image.srcPixels = (byte[])image.dstPixels.Clone();
				setStream(bitmapStream, bitmapImage);
				resetInterface();
			}
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
			resetButton(ref BlackAndWhiteButton);
			resetButton(ref embossButton);
			resetButton(ref invertButton);
			resetButton(ref blurButton);
			resetButton(ref SharpenButton);
			brightSlider.Value = 0;
			RedColorSlider.Value = 0;
			GreenColorSlider.Value = 0;
			BlueColorSlider.Value = 0;
			RedContrastSlider.Value = 0;
			GreenContrastSlider.Value = 0;
			BlueContrastSlider.Value = 0;
		}

		#region Color Change RGB
		private void OnRColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				appliedColors = "redcolor";
				prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
				setStream(exampleStream, exampleBitmap);
			}
		}

		private void OnGColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				appliedColors = "greencolor";
				prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
				setStream(exampleStream, exampleBitmap);
			}
		}

		private void OnBColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				appliedColors = "bluecolor";
				prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
				setStream(exampleStream, exampleBitmap);
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
				setStream(exampleStream, exampleBitmap);
			}
		}

		private void OnGContrastChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				appliedColors = "greencontrast";
				prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
				setStream(exampleStream, exampleBitmap);
			}
		}

		private void OnBContrastChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				appliedColors = "bluecontrast";
				prepareImage(exampleStream, exampleBitmap, image);
                image.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
				setStream(exampleStream, exampleBitmap);
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
		private void doBlackWhite(Stream stream, WriteableBitmap bitmap, FilterFunctions image)
		{
			prepareImage(stream, bitmap, image);
			imageOriginal.BlackAndWhite(image.dstPixels, image.srcPixels);
			setStream(stream, bitmap);
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
			setStream(stream, bitmap);
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
			setStream(stream, bitmap);
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
			setStream(stream, bitmap);
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
			setStream(stream, bitmap);

			resetInterface();
		}
		#endregion

		#region Apply Buttons
		private void OnFilterApplyClick(object sender, RoutedEventArgs e)
		{
			FilterApplyReset.Visibility = Visibility.Collapsed;
			SelectFilters.IsChecked = false;
			switch (appliedFilters)
			{
				case "blackwhite":
					doBlackWhite(bitmapStream, bitmapImage, imageOriginal);
					Filters.Visibility = Visibility.Collapsed;
					break;
				case "invert":
					doInvert(bitmapStream, bitmapImage, imageOriginal);
					Filters.Visibility = Visibility.Collapsed;
					break;
				case "emboss":
					doEmboss(bitmapStream, bitmapImage, imageOriginal);
					Filters.Visibility = Visibility.Collapsed;
					break;
				case "blur":
					doBlur(bitmapStream, bitmapImage, imageOriginal);
					Filters.Visibility = Visibility.Collapsed;
					break;
				case "sharpen":
					doSharpen(bitmapStream, bitmapImage, imageOriginal);
					Filters.Visibility = Visibility.Collapsed;
					break;
				default:
					break;
			}
		}

		private void OnColorApplyClick(object sender, RoutedEventArgs e)
		{
			ColorApplyReset.Visibility = Visibility.Collapsed;
			SelectColors.IsChecked = false;
			switch (appliedColors)
			{
				case "darken":
					prepareImage(bitmapStream, bitmapImage, imageOriginal);
					imageOriginal.Darken(brightSlider.Value);
					setStream(bitmapStream, bitmapImage);
					break;
				case "lighten":
					prepareImage(bitmapStream, bitmapImage, imageOriginal);
					imageOriginal.Lighten(brightSlider.Value);
					setStream(bitmapStream, bitmapImage);
					break;
				case "redcolor":
					prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
					setStream(bitmapStream, bitmapImage);
					break;
				case "greencolor":
					prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
					setStream(bitmapStream, bitmapImage);
					break;
				case "bluecolor":
					prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
					setStream(bitmapStream, bitmapImage);
					break;
				case "redcontrast":
					prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
					setStream(bitmapStream, bitmapImage);
					break;
				case "greencontrast":
					prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
					setStream(bitmapStream, bitmapImage);
					break;
				case "bluecontrast":
					prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.ColorChange(RedColorSlider.Value, GreenColorSlider.Value, BlueColorSlider.Value, RedContrastSlider.Value, GreenContrastSlider.Value, BlueContrastSlider.Value);
					setStream(bitmapStream, bitmapImage);
					break;
				default:
					break;
			}
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
				setStream(exampleStream, exampleBitmap);
				resetInterface();
			}
			FilterApplyReset.Visibility = Visibility.Collapsed;
			appliedFilters = null;
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
				setStream(exampleStream, exampleBitmap);
				resetInterface();
			}
			ColorApplyReset.Visibility = Visibility.Collapsed;
			appliedColors = null;
		}
		#endregion

		#region Checked Buttons
		private void FiltersChecked(object sender, RoutedEventArgs e)
		{
			Filters.Visibility = Visibility.Visible;
			Colors.Visibility = Visibility.Collapsed;
			SelectColors.IsChecked = false;
		}

		private void FiltersUnchecked(object sender, RoutedEventArgs e)
		{
			Filters.Visibility = Visibility.Collapsed;
		}

		private void ColorsChecked(object sender, RoutedEventArgs e)
		{
			Colors.Visibility = Visibility.Visible;
			Filters.Visibility = Visibility.Collapsed;
			SelectFilters.IsChecked = false;
		}

		private void ColorsUnchecked(object sender, RoutedEventArgs e)
		{
			Colors.Visibility = Visibility.Collapsed;
		}
		#endregion

        private void OnRotateClick(object sender, RoutedEventArgs e)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.Rotate();
            setStream(bitmapStream, bitmapImage);

            resetInterface();
        }


	}
	#endregion
}
#endregion