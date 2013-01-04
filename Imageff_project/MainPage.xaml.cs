using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using RemedyPic.Common;
using Windows.ApplicationModel.DataTransfer;


#region Namespace RemedyPic
namespace RemedyPic
{
	#region MainPage class
	public sealed partial class MainPage : RemedyPic.Common.LayoutAwarePage
	{
		private string mruToken = null;
		private WriteableBitmap tempBitmap;
		byte[] srcPixels, dstPixels;
		int width, height;
		Stream temp;
		bool invertedAlready = false, blackWhiteAlready = false;
		static readonly long cycleDuration = TimeSpan.FromSeconds(3).Ticks;
		bool pictureIsLoaded = false;
		Windows.Storage.StorageFile Globalfile;


		public MainPage()
		{
			this.InitializeComponent();
		}

		#region This is the LoadState.
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
							tempBitmap.SetSource(fileStream);
							displayImage.Source = tempBitmap;

							// Set the data context for the page.
							this.DataContext = file;
						}
					}
				}
			}
		}
		#endregion

		#region This is the SaveState.
		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			if (!String.IsNullOrEmpty(mruToken))
			{
				pageState["mruToken"] = mruToken;
			}
		}
		#endregion

		#region GetPhotoButton_Click function
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
				Globalfile = file;

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
						srcPixels = pixelProvider.DetachPixelData();
						dstPixels = srcPixels;

						tempBitmap = new WriteableBitmap((int)frame.PixelWidth, (int)frame.PixelHeight);
					}
					tempBitmap.SetSource(fileStream);
					// apply pixels to bitmap
					displayImage.Source = tempBitmap;
					setFileProperties(file);
					brightSlider.Value = 0;
					contentGrid.Opacity = 100;
					pictureIsLoaded = true;
					border.BorderThickness = new Thickness(1,1,1,1);
					border.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Black);
				}
			}
		}
		#endregion

		#region setFileProperties function
		void setFileProperties(Windows.Storage.StorageFile file)
		{
			// This sets the file name, path and date created
			// to the text boxes
			fileName.Text = file.DisplayName;
		}
		#endregion

		#region OnInvertClick event
		private void OnInvertClick(object sender, RoutedEventArgs e)
		{
			// This occures when InvertFilterButton is clicked
			if (pictureIsLoaded)
			{
				prepareImage();
				FilterFunctions.Filter(ref dstPixels, ref srcPixels, ref height, ref width, invertedAlready);
				invertedAlready = !invertedAlready;
				setStream();
			}
		}
		#endregion

		#region OnBlackWhiteClick event
		private void OnBlackWhiteClick(object sender, RoutedEventArgs e)
		{
			// This occures when OnBlackWhiteButton is clicked
			if (pictureIsLoaded)
			{
				prepareImage();
				FilterFunctions.BlackAndWhite(ref dstPixels, ref srcPixels, ref height, ref width, blackWhiteAlready);
				blackWhiteAlready = !blackWhiteAlready;
				setStream();
			}
		}
		#endregion

		#region setStream function
		void setStream()
		{
			// This sets the pixels to the bitmap
			temp.Seek(0, SeekOrigin.Begin);
			temp.Write(dstPixels, 0, dstPixels.Length);
			tempBitmap.Invalidate();
		}
		#endregion

		#region prepareImage function
		void prepareImage()
		{
			// This calculates the width and height of the bitmap image
			// and sets the Stream and the pixels byte array
			width = tempBitmap.PixelWidth;
			height = tempBitmap.PixelHeight;
			temp = tempBitmap.PixelBuffer.AsStream();
			dstPixels = new byte[4 * tempBitmap.PixelWidth * tempBitmap.PixelHeight];
		}
		#endregion

		#region OnSaveButtonClick event
		// Occures when Save Button is clicked
		private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
		{
			// The save button must activate only if there is a picture loaded.
			// File picker APIs don't work if the app is in a snapped state.
			// If the app is snapped, try to unsnap it first. Only show the picker if it unsnaps.
			if (pictureIsLoaded && (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped ||
				 Windows.UI.ViewManagement.ApplicationView.TryUnsnap() == true))
			{
				FileSavePicker savePicker = new FileSavePicker();
				savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

				// Dropdown of file types the user can save the file as
				savePicker.FileTypeChoices.Add("JPEG", new List<string>() { ".jpg" });
				savePicker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
				savePicker.FileTypeChoices.Add("Bitmap", new List<string>() { ".bmp" });
				
				savePicker.SuggestedFileName = fileName.Text;

				// Default file name if the user does not type one in or select a file to replace
				StorageFile file = await savePicker.PickSaveFileAsync();

				if (file != null)
				{
					//Windows.Storage.Streams.IRandomAccessStream fileStream =
					IRandomAccessStream writeStream = await file.OpenAsync(FileAccessMode.ReadWrite);
					BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, writeStream);
					encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)tempBitmap.PixelWidth, (uint)tempBitmap.PixelHeight, tempBitmap.PixelWidth, tempBitmap.PixelHeight, dstPixels);
					await encoder.FlushAsync();
				}
			}
		}
		#endregion

		private void OnSliderChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			if (pictureIsLoaded)
			{
				prepareImage();
				if (brightSlider.Value < 0)
				{
					FilterFunctions.Darken(ref dstPixels, ref srcPixels, ref height, ref width, brightSlider.Value);
				}
				else if (brightSlider.Value >= 0)
				{
					FilterFunctions.Lighten(ref dstPixels, ref srcPixels, ref height, ref width, brightSlider.Value);
				}
				setStream();
			}
		}

	}
	#endregion
}
#endregion