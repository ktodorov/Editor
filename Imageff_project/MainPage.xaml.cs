using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
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

		// bitmapImage is the image that is edited in RemedyPic.
		private WriteableBitmap bitmapImage;

		// bitmapStream is used to save the pixel stream to bitmapImage.
		Stream bitmapStream;

		static readonly long cycleDuration = TimeSpan.FromSeconds(3).Ticks;
		// This is true if the user load a picture.
		bool pictureIsLoaded = false;

		FilterFunctions image = new FilterFunctions();
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
				Windows.Storage.Pickers.FileOpenPicker openPicker = new Windows.Storage.Pickers.FileOpenPicker();
				openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
				openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;

				// Filter to include a sample subset of file types.
				openPicker.FileTypeFilter.Clear();
				openPicker.FileTypeFilter.Add(".bmp");
				openPicker.FileTypeFilter.Add(".png");
				openPicker.FileTypeFilter.Add(".jpeg");
				openPicker.FileTypeFilter.Add(".jpg");

				// Open the file picker.
				Windows.Storage.StorageFile file = await openPicker.PickSingleFileAsync();

				// file is null if user cancels the file picker.
				if (file != null)
				{
					// Open a stream for the selected file.
					Windows.Storage.Streams.IRandomAccessStream fileStream =
						await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

					RandomAccessStreamReference streamRef = RandomAccessStreamReference.CreateFromFile(file);

					using (IRandomAccessStreamWithContentType tempStream =
						   await streamRef.OpenReadAsync())
					{
						BitmapDecoder decoder = await BitmapDecoder.CreateAsync(tempStream);
						BitmapFrame frame = await decoder.GetFrameAsync(0);

						// I know the parameterless version of GetPixelDataAsync works for this image
						PixelDataProvider pixelProvider = await frame.GetPixelDataAsync();
						image.srcPixels = pixelProvider.DetachPixelData();

						bitmapImage = new WriteableBitmap((int)frame.PixelWidth, (int)frame.PixelHeight);
					}
					// Apply the fileStream that the user have selected to the WriteableBitmap file.
					bitmapImage.SetSource(fileStream);
					// Apply pixels to bitmap.
					displayImage.Source = bitmapImage;
					// Sets the file name to the screen.
					setFileProperties(file);
					// If the interface was changed from previous image, it should be resetted.
					resetInterface();
					// Show the interface after the picture is loaded.
					contentGrid.Visibility = Visibility.Visible;
					pictureIsLoaded = true;
					// Set the border of the image panel.
					border.BorderThickness = new Thickness(1, 1, 1, 1);
					border.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Black);
					// Set the left margin of the file name text block.
					SetMargin();
					// TO DO: Resize function.
				}
			}
			else
			{
				// If the window can't be unsnapped, show alert.
				MessageDialog messageDialog = new MessageDialog("Can't save in snapped state. Please unsnap the app and try again", "Close");
				await messageDialog.ShowAsync();
			}
		}
		#endregion

		private void SetMargin()
		{
			// Set the left margin of the file name text block to half of the loaded image width.
			Thickness margin = fileName.Margin;
			margin.Left = (displayImage.ActualWidth / 2) - 20 - fileName.Text.Length;
			fileName.Margin = margin;
		}

		#region Invert Filter
		private void OnInvertClick(object sender, RoutedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				prepareImage();
				int[,] coeff = new int[5, 5];
				int offset = 0, scale = 0;
				Invert_SetValues(ref coeff, ref offset, ref scale);

				CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
				image.dstPixels = custom_image.Filter();
				setStream();
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
				prepareImage();
				image.BlackAndWhite();
				setStream();
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
				prepareImage();
				int[,] coeff = new int[5, 5];
				int offset = 0, scale = 0;
				Emboss_SetValues(ref coeff, ref offset, ref scale);

				CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
				image.dstPixels = custom_image.Filter();
				setStream();
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
				prepareImage();
				int[,] coeff = new int[5, 5];
				int offset = 0, scale = 0;
				Sharpen_SetValues(ref coeff, ref offset, ref scale);

				CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
				image.dstPixels = custom_image.Filter();

				setStream();

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
				prepareImage();
				int[,] coeff = new int[5, 5];
				int offset = 0, scale = 0;
				Blur_SetValues(ref coeff, ref offset, ref scale);

				CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
				image.dstPixels = custom_image.Filter();

				setStream();

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
				prepareImage();
				int[,] coeff = new int[5, 5];
				int offset = 0, scale = 0;
				Custom_SetValues(ref coeff, ref offset, ref scale);

				CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
				image.dstPixels = custom_image.Filter();

				setStream();
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
														   (uint)bitmapImage.PixelWidth, (uint)bitmapImage.PixelHeight, 96.0, 96.0, image.dstPixels);
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
				prepareImage();
				if (brightSlider.Value < 0)
				{
					image.Darken(brightSlider.Value);
				}
				else if (brightSlider.Value >= 0)
				{
					image.Lighten(brightSlider.Value);
				}
				setStream();
			}
		}
		#endregion

		void setFileProperties(Windows.Storage.StorageFile file)
		{
			// This sets the file name to the text box
			fileName.Text = file.DisplayName;
		}

		void setStream()
		{
			// This sets the pixels to the bitmap
			bitmapStream.Seek(0, SeekOrigin.Begin);
			bitmapStream.Write(image.dstPixels, 0, image.dstPixels.Length);
			bitmapImage.Invalidate();
		}

		void prepareImage()
		{
			// This calculates the width and height of the bitmap image
			// and sets the Stream and the pixels byte array
			image.width = (int)bitmapImage.PixelWidth;
			image.height = (int)bitmapImage.PixelHeight;
			bitmapStream = bitmapImage.PixelBuffer.AsStream();
			image.dstPixels = new byte[4 * bitmapImage.PixelWidth * bitmapImage.PixelHeight];
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
				prepareImage();
				image.Reset();
				setStream();
				resetInterface();
			}
		}

		private void OnApplyClick(object sender, RoutedEventArgs e)
		{
			// This applies the current dstPixels array to the source pixel array
			// so the user can apply new functions over the image.
			if (pictureIsLoaded)
			{
				image.srcPixels = (byte[])image.dstPixels.Clone();
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
		}

		#region Color Change RGB
		private void OnRColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				prepareImage();
				image.ColorChange(RedColorSlider.Value, FilterFunctions.ColorType.Red);
				setStream();
			}
		}

		private void OnGColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				prepareImage();
				image.ColorChange(GreenColorSlider.Value, FilterFunctions.ColorType.Green);
				setStream();
			}
		}

		private void OnBColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				prepareImage();
				image.ColorChange(BlueColorSlider.Value, FilterFunctions.ColorType.Blue);
				setStream();
			}
		}
		#endregion

		#region Resizing image
		// TO DO: Resize function...
		private void resize()
		{
		}
		#endregion

	}
	#endregion
}
#endregion