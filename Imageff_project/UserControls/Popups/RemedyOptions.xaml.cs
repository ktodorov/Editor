using RemedyPic.RemedyClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.System.UserProfile;
using System.Text.RegularExpressions;

namespace RemedyPic.UserControls.Popups
{
	public sealed partial class RemedyOptions : UserControl
	{
		MainPage rootPage = MainPage.Current;

		// Those are used for the import/export functions.
		public Configuration configFile = new Configuration();
		public List<string> effectsApplied = new List<string>();

		public double widthHeightRatio = 0;
		public bool keepProportions = true;
		public bool calledByOther = false;

        UIElement temp;


		public RemedyOptions()
		{
            // Main function.
            this.InitializeComponent();
            temp = ResizePanel;
            tempPanel.Children.Remove(ResizePanel);
		}

		#region Export/Import
		public void OnExportButtonClick(object sender, RoutedEventArgs e)
		{
            // Called when export button is pressed.
			if (rootPage.archive_current_index != rootPage.archive_data.Count - 1)
			{
				rootPage.archive_data.RemoveRange(rootPage.archive_current_index + 1, rootPage.archive_data.Count - 1 - rootPage.archive_current_index);
				effectsApplied.RemoveRange(rootPage.archive_current_index, effectsApplied.Count - rootPage.archive_current_index); // Here we don`t save the start image, so we have -1 index of archive_current_index
			}
			configFile.Export(effectsApplied);
		}

		public void onImportButtonClick(object sender, RoutedEventArgs e)
		{
            // Called when Import button is pressed.
			if (rootPage.archive_current_index != rootPage.archive_data.Count - 1)
			{
				rootPage.archive_data.RemoveRange(rootPage.archive_current_index + 1, rootPage.archive_data.Count - 1 - rootPage.archive_current_index);
				effectsApplied.RemoveRange(rootPage.archive_current_index, effectsApplied.Count - rootPage.archive_current_index); // Here we don`t save the start image, so we have -1 index of archive_current_index
			}

			rootPage.ImageLoadingRing.IsActive = true;
			rootPage.DarkenBorder.Visibility = Visibility.Visible;

			for (int i = 0; i < configFile.effects.Count; i += 2)
			{
				checkEffect(i);
			}
			rootPage.FiltersPopup.setFilterBitmaps();
			rootPage.ImageLoadingRing.IsActive = false;
			rootPage.DarkenBorder.Visibility = Visibility.Collapsed;
		}

        /// <summary>
        /// This checks the loaded config file on the given 'i' position
        /// and imports the function which is there.
        /// </summary>
        /// <param name="i"> The position where the config file must be checked </param>
		public void checkEffect(int i)
		{
			string[] temp = new string[10];
			switch (configFile.effects[i])
			{
				case "Filter":
					rootPage.FiltersPopup.ApplyFilter(configFile.effects[i + 1]);
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
					rootPage.RotatePopup.ApplyRotate(temp[0]);
					break;

				case "Colorize":
					temp = configFile.effects[i + 1].Split(',');
					importColorize(temp);
					break;

				case "Frame":
					temp = configFile.effects[i + 1].Split(',');
					rootPage.FramesPopup.checkAndApplyFrames(temp);
					rootPage.imageOriginal.srcPixels = (byte[])rootPage.imageOriginal.dstPixels.Clone();
					break;

				case "Rotate":
					temp = configFile.effects[i + 1].Split(',');
					rootPage.RotatePopup.RotateBitmap(temp[0]);
					break;

				case "Histogram":
					if (configFile.effects[i + 1] == "true")
					{
						rootPage.HistogramPopup.equalizeHistogram();
					}
					break;
				default: break;
			}
		}

		#region Import Functions
        // Functions called for importing effects.
		public void importColor(string[] temp)
		{
			rootPage.ColorsPopup.importColor(temp);
		}

		public void importContrast(string[] temp)
		{
			rootPage.ColorsPopup.importContrast(temp);
		}

		public void importExposure(string[] temp)
		{
			rootPage.ExposurePopup.Import(temp);
		}

		public void importColorize(string[] temp)
		{
			rootPage.ColorizePopup.Import(temp);
		}
		#endregion

		public async void onImportFileSelectButtonClick(object sender, RoutedEventArgs e)
		{
            // Called when the button for selecting file to import is pressed.
			bool imported = await configFile.Import(importFileName);
			if (imported)
				importFilePanel.Visibility = Visibility.Visible;
			else if (configFile == null)
				importFilePanel.Visibility = Visibility.Collapsed;
		}
		#endregion

		#region Resizing the image

		public void OnNewWidthTextChanged(object sender, TextChangedEventArgs e)
		{
            // Called when the value of the text box with the new width is changed.
            // If we must keep proportions, calculations are made and the value in the height box is changed proportionally.
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
            // Called when the value of the text box with the new height is changed.
            // If we must keep proportions, calculations are made and the value in the width box is changed proportionally.
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
            // Show the resize panel when the resize button is pressed.
            tempPanel.Children.Add(temp);
			temp.Visibility = Visibility.Visible;
		}

		public void Resize_Unchecked(object sender, RoutedEventArgs e)
		{
            // Hide the resize panel when the resize button is pressed again.
			temp.Visibility = Visibility.Collapsed;
            tempPanel.Children.Remove(temp);
            
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
            int OrignalWidth = rootPage.imageOriginal.width;
            int OriginalHeight = rootPage.imageOriginal.height;
            CheckForCropBeforeResize(ref OrignalWidth, ref OriginalHeight);

			ApplyResize.Visibility = Visibility.Collapsed;

			rootPage.bitmapImage = await ResizeImage(rootPage.bitmapImage, (uint)(resizeWidth), (uint)(resizeHeight));

			rootPage.bitmapStream = rootPage.bitmapImage.PixelBuffer.AsStream();
			rootPage.imageOriginal.srcPixels = new byte[(uint)rootPage.bitmapStream.Length];
			rootPage.image.srcPixels = new byte[(uint)rootPage.exampleStream.Length];
			await rootPage.bitmapStream.ReadAsync(rootPage.imageOriginal.srcPixels, 0, rootPage.imageOriginal.srcPixels.Length);
			rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
			rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
			rootPage.imageDisplayed.displayImage.Source = rootPage.bitmapImage;
			rootPage.Panel.ArchiveAddArray();
			effectsApplied.Add("Resize " + OrignalWidth + " " + OriginalHeight + " " + (int)resizeWidth + " " + (int)resizeHeight);
			rootPage.imageDisplayed.displayImage.Source = rootPage.bitmapImage;
			rootPage.FiltersPopup.setFilterBitmaps();
		}

        /// <summary>
        /// Check if there is crop before resize (one step before resize) 
        /// </summary>
        /// <param name="OrignalWidth"> The original width of the bitmap </param>
        /// <param name="OriginalHeight"> The original height of the bitmap </param>
        private void CheckForCropBeforeResize(ref int OriginalWidth, ref int OriginalHeight)
        {
            if (rootPage.archive_current_index - 1 >= 0 && Regex.IsMatch(rootPage.OptionsPopup.effectsApplied[rootPage.archive_current_index - 1], "Crop")) 
            {
                string[] sizes = rootPage.OptionsPopup.effectsApplied[rootPage.archive_current_index - 1].Split(' ');
                OriginalWidth = Convert.ToInt32(sizes[3]);
                OriginalHeight = Convert.ToInt32(sizes[4]);
            }
        }
		#endregion


		#region Resizing an image

		
        /// <summary>
        /// This function resize the passed WriteableBitmap object with the passed width and height.
        /// </summary>
        /// <param name="baseWriteBitmap"> The bitmap to be resized </param>
        /// <param name="width"> New width to be resized to </param>
        /// <param name="height"> New height to be resized to </param>
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


		#region Image Options
		public async void SetLockPic_Clicked(object sender, RoutedEventArgs e)
		{
			// This sets the current image as a wallpaper on the lock screen of the current user and inform him that everything was okay.
			bool savedFile = false;
			savedFile = await rootPage.SaveFile(false);
			while (!savedFile)
			{

			}
			await LockScreen.SetImageFileAsync(rootPage.file);
			MessageDialog messageDialog = new MessageDialog("Picture set! :)", "All done");
			await messageDialog.ShowAsync();
			await rootPage.deleteUsedFile();
		}

		public void ReturnOriginal_Clicked(object sender, RoutedEventArgs e)
		{
			// Restore the original image.
			RestoreOriginalBitmap();
		}

        /// <summary>
        /// This restores the original loaded in the beginning bitmap.
        /// </summary>
		public void RestoreOriginalBitmap()
		{
			rootPage.imageOriginal.srcPixels = (byte[])rootPage.uneditedImage.srcPixels.Clone();
			rootPage.imageOriginal.dstPixels = (byte[])rootPage.uneditedImage.dstPixels.Clone();
			rootPage.bitmapStream = rootPage.uneditedStream;
			rootPage.bitmapImage = rootPage.uneditedBitmap;
			rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
			rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
			rootPage.imageDisplayed.displayImage.Source = rootPage.bitmapImage;

			rootPage.setExampleBitmaps();

			rootPage.FiltersPopup.setFilterBitmaps();
			rootPage.imageDisplayed.selectedRegion.ResetCorner(0, 0, rootPage.imageDisplayed.displayImage.ActualWidth, rootPage.imageDisplayed.displayImage.ActualHeight);
		}

		#endregion

		public void BackPopupClicked(object sender, RoutedEventArgs e)
		{
			rootPage.BackPopupClicked(sender, e);
		}
	}
}
