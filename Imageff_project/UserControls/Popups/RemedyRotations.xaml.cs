using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

namespace RemedyPic.UserControls.Popups
{
    public sealed partial class RemedyRotations : UserControl
    {
        public string appliedRotations;

        MainPage rootPage = MainPage.Current;

        public RemedyRotations()
        {
            this.InitializeComponent();
        }


        /// <summary>
        /// This function rotates the passed WriteableBitmap clockwise or counter-clockwise
        /// </summary>
        /// <param name="baseWriteBitmap"> WriteableBitmap to be rotated </param>
        /// <param name="width"> Width of the WriteableBitmap </param>
        /// <param name="height"> Height of the WriteableBitmap </param>
        /// <param name="position"> If 'right', it rotates clockwise, if 'left' - counterclockwise </param>
        public async Task<WriteableBitmap> RotateImage(WriteableBitmap baseWriteBitmap, uint width, uint height, string position)
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

            BitmapRotation rotateTo = BitmapRotation.None;

            if (position == "right")
            {
                rotateTo = BitmapRotation.Clockwise90Degrees;
            }
            else if (position == "left")
            {
                rotateTo = BitmapRotation.Clockwise270Degrees;
            }

            var transform = new BitmapTransform
            {
                ScaledWidth = width,
                ScaledHeight = height,
                Rotation = rotateTo
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
            encoder2.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, height, width, 96, 96, sourceDecodedPixels);
            await encoder2.FlushAsync();
            inMemoryRandomStream2.Seek(0);

            // Finally the resized WritableBitmap
            var bitmap = new WriteableBitmap((int)width, (int)height);
            await bitmap.SetSourceAsync(inMemoryRandomStream2);
            return bitmap;
        }


        // Reset the data of Rotate menu
        public void ResetRotateMenuData()
        {
            appliedRotations = null;
        }

        /// <summary>
        /// Applies the rotation or fliping and saves it to the export array.
        /// </summary>
        /// <param name="rotation"> String that shows what operation has been completed </param>
        public void ApplyRotate(string rotation)
        {
            switch (rotation)
            {
                case "hflip":
                    rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    rootPage.imageOriginal.HFlip();
                    rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    break;
                case "vflip":
                    rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    rootPage.imageOriginal.VFlip();
                    rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    break;
                default:
                    break;
            }
            rootPage.image.srcPixels = (byte[])rootPage.image.dstPixels.Clone();
            rootPage.imageOriginal.srcPixels = (byte[])rootPage.imageOriginal.dstPixels.Clone();
            rootPage.Panel.ArchiveAddArray();
            rootPage.OptionsPopup.effectsApplied.Add("Flip = " + rotation);
            ResetRotateMenuData();
        }


        public void OnRotateResetClick(object sender, RoutedEventArgs e)
        {
            // Called when the reset button is pressed.
            rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            rootPage.image.Reset();
            rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            ResetRotateMenuData();
            RotateApplyReset.Visibility = Visibility.Collapsed;
        }


        // These events are called when a Flip or Rotate button is clicked.
        public void OnHFlipClick(object sender, RoutedEventArgs e)
        {
            appliedRotations = "hflip";
            rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            rootPage.image.HFlip();
            rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
        }

        public void OnVFlipClick(object sender, RoutedEventArgs e)
        {
            appliedRotations = "vflip";
            rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            rootPage.image.VFlip();
            rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
        }

        public void OnRotateClick(object sender, RoutedEventArgs e)
        {
            rootPage.ImageLoadingRing.IsActive = true;
            rootPage.Menu.SelectRotations.IsChecked = false;

            if (e.OriginalSource.Equals(RotateLeft))
            {
                RotateBitmap("RotateLeft");
            }
            else if (e.OriginalSource.Equals(RotateRight))
            {
                RotateBitmap("RotateRight");
            }

            rootPage.ImageLoadingRing.IsActive = false;
        }

        public async void RotateBitmap(string givenElementString)
        {
            if (givenElementString == "RotateLeft")
            {
                rootPage.bitmapImage = await RotateImage(rootPage.bitmapImage, (uint)rootPage.bitmapImage.PixelWidth, (uint)rootPage.bitmapImage.PixelHeight, "left");
            }
            else if (givenElementString == "RotateRight")
            {
                rootPage.bitmapImage = await RotateImage(rootPage.bitmapImage, (uint)rootPage.bitmapImage.PixelWidth, (uint)rootPage.bitmapImage.PixelHeight, "right");
            }

            rootPage.imageDisplayed.displayImage.Source = rootPage.bitmapImage;

            rootPage.bitmapStream = rootPage.bitmapImage.PixelBuffer.AsStream();
            rootPage.imageOriginal.srcPixels = new byte[(uint)rootPage.bitmapStream.Length];
            await rootPage.bitmapStream.ReadAsync(rootPage.imageOriginal.srcPixels, 0, rootPage.imageOriginal.srcPixels.Length);
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);

            rootPage.setExampleBitmaps();
            rootPage.FiltersPopup.setFilterBitmaps();

            rootPage.imageDisplayed.sourceImagePixelHeight = (uint)rootPage.bitmapImage.PixelHeight;
            rootPage.imageDisplayed.sourceImagePixelWidth = (uint)rootPage.bitmapImage.PixelWidth;
        }

        
        public void OnRotateApplyClick(object sender, RoutedEventArgs e)
        {
            // Event for apply button on  Rotate popup. Sets the image with the applied flip

            rootPage.ImageLoadingRing.IsActive = true;
            // SelectRotations.IsChecked = false;
            ApplyRotate(appliedRotations);

            rootPage.FiltersPopup.setFilterBitmaps();
            rootPage.ImageLoadingRing.IsActive = false;
            RotateApplyReset.Visibility = Visibility.Collapsed;
            rootPage.Saved = false;
        }

        public void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            // Called when the back button is pressed.
            rootPage.BackPopupClicked(sender, e);
        }

    }
}
