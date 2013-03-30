using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using RemedyPic.RemedyClasses;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace RemedyPic.UserControls.Popups
{
    public sealed partial class RemedyFilters : UserControl
    {
        MainPage rootPage;
        public string appliedFilters = null;


        public RemedyFilters()
        {
            this.InitializeComponent();
            rootPage = MainPage.Current;
        }

        #region Small Bitmaps for Filters
        public async void setFilterBitmaps()
        {
            // This creates temporary Streams and WriteableBitmap objects for every filter available.
            // We set the bitmaps as source to the XAML rootPage.image objects.
            // After this, we apply different filter for each of the WriteableBitmap objects.

            RemedyImage filterimage = new RemedyImage();
            uint newWidth = (uint)rootPage.bitmapImage.PixelWidth;
            uint newHeight = (uint)rootPage.bitmapImage.PixelHeight;

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
            blackWhiteBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			embossBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			emboss2Bitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			invertBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			blurBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			blur2Bitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			sharpenBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			noiseBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight);

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

            rootPage.prepareImage(blackWhiteStream, blackWhiteBitmap, filterimage);
            rootPage.setStream(blackWhiteStream, blackWhiteBitmap, filterimage);

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
			hardNoiseBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			edgeDetectBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			edgeEnhanceBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			retroBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			darkenBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			brightenBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			shadowBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight),
			crystalBitmap = await rootPage.OptionsPopup.ResizeImage(rootPage.bitmapImage, newWidth, newHeight);

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

            rootPage.prepareImage(hardNoiseStream, hardNoiseBitmap, filterimage);
            rootPage.setStream(hardNoiseStream, hardNoiseBitmap, filterimage);

            doFilter(hardNoiseStream, hardNoiseBitmap, filterimage, "hardNoise");
            doFilter(edgeDetectStream, edgeDetectBitmap, filterimage, "EdgeDetect");
            doFilter(edgeEnhanceStream, edgeEnhanceBitmap, filterimage, "EdgeEnhance");
            doFilter(retroStream, retroBitmap, filterimage, "retro");
            doFilter(darkenStream, darkenBitmap, filterimage, "darken");
            doFilter(brightenStream, brightenBitmap, filterimage, "brighten");
            doFilter(shadowStream, shadowBitmap, filterimage, "shadow");
            doFilter(crystalStream, crystalBitmap, filterimage, "crystal");
        }

        public async void initializeBitmap(Stream givenStream, WriteableBitmap givenBitmap, RemedyImage givenImage)
        {
            // This makes the required operations for initializing the WriteableBitmap.
            givenStream = givenBitmap.PixelBuffer.AsStream();
            givenImage.srcPixels = new byte[(uint)givenStream.Length];
            await givenStream.ReadAsync(givenImage.srcPixels, 0, givenImage.srcPixels.Length);
        }

        public void doFilter(Stream givenStream, WriteableBitmap givenBitmap, RemedyImage givenImage, string filter)
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


        #region Filters
        // Change the image with black and white filter applied
        private void doBlackWhite(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            givenImage.BlackAndWhite(givenImage.dstPixels, givenImage.srcPixels);
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with invert filter applied
        public void doInvert(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Invert_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with emboss filter applied
        public void doEmboss(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Emboss_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with emboss2 filter applied
        public void doEmboss2(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Emboss2_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with sharpen filter applied
        public void doSharpen(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Sharpen_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with noise filter applied
        public void doNoise(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);

            givenImage.Noise(givenImage.Noise_GetSquareWidth(20));
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with hardnoise filter applied
        public void doHardNoise(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);

            givenImage.Noise(1);
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with blur filter applied
        public void doBlur(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Blur_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with blur2 filter applied
        public void doBlur2(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Blur2_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with edgeDetect filter applied
        public void doEdgeDetect(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            EdgeDetect_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with edgeEnhance filter applied
        public void doEdgeEnhance(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            EdgeEnhance_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            rootPage.setStream(stream, bitmap, givenImage);

        }

        // Change the image with retro filter applied
        public void doRetro(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(0, 0, 0, 50, 50, -50);
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with darken filter applied
        public void doDarken(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(0, 0, 0, 50, 50, 0);
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with brighten filter applied
        public void doBrighten(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(70, 70, 70, 0, 0, 0);
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with shadow filter applied
        public void doShadow(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(-80, -80, -80, 0, 0, 0);
            rootPage.setStream(stream, bitmap, givenImage);
        }

        // Change the image with crystal filter applied
        public void doCrystal(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            rootPage.prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(0, 0, 0, 50, 35, 35);
            rootPage.setStream(stream, bitmap, givenImage);
        }
        #endregion

        #region Filters Check Buttons
        public void blackWhiteChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "blackwhite";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void invertChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "invert";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void sharpenChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "sharpen";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void colorizeChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "colorize";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void retroChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "retro";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void darkenChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "darken";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void noiseChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "noise";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void hardNoiseChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "hardNoise";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void embossChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "emboss";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void emboss2Checked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "emboss2";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void blurChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "blur";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void blur2Checked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "blur2";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void edgeDetectChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "EdgeDetect";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void edgeEnhanceChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "EdgeEnhance";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void brightenChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "brighten";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void shadowChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "shadow";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void crystalChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "crystal";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void filterUnchecked(object sender, RoutedEventArgs e)
        {
            var filterSender = sender as ToggleButton;
            filterSender.IsChecked = false;
            FilterApplyReset.Visibility = Visibility.Collapsed;
        }

        public void deselectFilters()
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

        #region Filter functions

        #region Invert Filter
        // Invert filter function
        public void OnInvertClick(object sender, RoutedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                appliedFilters = "invert";
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Invert_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter customImage = new CustomFilter(rootPage.image.srcPixels, rootPage.image.width, rootPage.image.height, offset, scale, coeff);
                rootPage.image.dstPixels = customImage.Filter();
                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }

        // Set the matrix for invert filter
        public void Invert_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = -1;
            offset = 255;
            scale = 1;
        }
        #endregion

        #region B&W Filter
        public void OnBlackWhiteClick(object sender, RoutedEventArgs e)
        {
            // This occures when OnBlackWhiteButton is clicked
            if (rootPage.pictureIsLoaded)
            {
                // First we prepare the rootPage.image for filtrating, then we call the filter.
                // After that we save the new data to the current rootPage.image,
                // reset all other highlighted buttons and make the B&W button selected
                appliedFilters = "blackwhite";
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                rootPage.image.BlackAndWhite(rootPage.image.dstPixels, rootPage.image.srcPixels);
                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }
        #endregion

        #region Emboss Filter
        // Emboss filter function
        public void OnEmbossClick(object sender, RoutedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                appliedFilters = "emboss";
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Emboss_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter customImage = new CustomFilter(rootPage.image.srcPixels, rootPage.image.width, rootPage.image.height, offset, scale, coeff);
                rootPage.image.dstPixels = customImage.Filter();
                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }

        // Set the matrix for emboss filter
        public void Emboss_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[3, 3] = -1;
            offset = 128;
            scale = 1;
        }
        #endregion

        #region Emboss 2 Filter
        // Emboss 2 filter function
        public void OnEmboss2Click(object sender, RoutedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                appliedFilters = "emboss2";
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Emboss2_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter customImage = new CustomFilter(rootPage.image.srcPixels, rootPage.image.width, rootPage.image.height, offset, scale, coeff);
                rootPage.image.dstPixels = customImage.Filter();
                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }

        // Set the matrix for emboss2 filter
        public void Emboss2_SetValues(ref int[,] coeff, ref int offset, ref int scale)
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
        public void OnSharpenClick(object sender, RoutedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                appliedFilters = "sharpen";
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Sharpen_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter customImage = new CustomFilter(rootPage.image.srcPixels, rootPage.image.width, rootPage.image.height, offset, scale, coeff);
                rootPage.image.dstPixels = customImage.Filter();

                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }

        // Set the matrix for sharpen filter
        public void Sharpen_SetValues(ref int[,] coeff, ref int offset, ref int scale)
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
        public void OnBlurClick(object sender, RoutedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                appliedFilters = "blur";
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Blur_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter customImage = new CustomFilter(rootPage.image.srcPixels, rootPage.image.width, rootPage.image.height, offset, scale, coeff);
                rootPage.image.dstPixels = customImage.Filter();

                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }

        // Set the matrix for blur filter
        public void Blur_SetValues(ref int[,] coeff, ref int offset, ref int scale)
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
        public void OnBlur2Click(object sender, RoutedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                appliedFilters = "blur2";
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Blur2_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter customImage = new CustomFilter(rootPage.image.srcPixels, rootPage.image.width, rootPage.image.height, offset, scale, coeff);
                rootPage.image.dstPixels = customImage.Filter();

                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }

        // Set the matrix for blur2 filter
        public void Blur2_SetValues(ref int[,] coeff, ref int offset, ref int scale)
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
        public void OnEdgeDetectClick(object sender, RoutedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                appliedFilters = "EdgeDetect";
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                EdgeDetect_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter customImage = new CustomFilter(rootPage.image.srcPixels, rootPage.image.width, rootPage.image.height, offset, scale, coeff);
                rootPage.image.dstPixels = customImage.Filter();

                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }

        // Set the matrix for edgeDetect filter
        public void EdgeDetect_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = -4;
            coeff[1, 2] = coeff[2, 1] = coeff[2, 3] = coeff[3, 2] = 1;
            offset = 0;
            scale = 1;
        }
        #endregion

        #region EdgeEnhance Filter
        // EdgeEnhance filter function
        public void OnEdgeEnhanceClick(object sender, RoutedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                appliedFilters = "EdgeEnhance";
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                EdgeEnhance_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter customImage = new CustomFilter(rootPage.image.srcPixels, rootPage.image.width, rootPage.image.height, offset, scale, coeff);
                rootPage.image.dstPixels = customImage.Filter();

                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }

        // Set the matrix for edgeEnhance filter
        public void EdgeEnhance_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[3, 0] = -1;
            offset = 0;
            scale = 1;
        }

        #endregion

        #endregion

        public void OnFilterApplyClick(object sender, RoutedEventArgs e)
        {
            ApplyFilter(appliedFilters);
            FilterApplyReset.Visibility = Visibility.Collapsed;
            rootPage.Menu.SelectFilters.IsChecked = false;
            setFilterBitmaps();
            rootPage.Saved = false;
        }

        public void ApplyFilter(string filter)
        {
            rootPage.ImageLoadingRing.IsActive = true;
            switch (filter)
            {
                case "blackwhite":
                    doBlackWhite(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doBlackWhite(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "invert":
                    doInvert(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doInvert(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "emboss":
                    doEmboss(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doEmboss(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "emboss2":
                    doEmboss2(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doEmboss2(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "blur":
                    doBlur(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doBlur(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "blur2":
                    doBlur2(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doBlur2(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "sharpen":
                    doSharpen(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doSharpen(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "noise":
                    doNoise(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doNoise(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "hardNoise":
                    doHardNoise(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doHardNoise(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "EdgeDetect":
                    doEdgeDetect(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doEdgeDetect(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "EdgeEnhance":
                    doEdgeEnhance(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doEdgeEnhance(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "retro":
                    doRetro(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doRetro(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "darken":
                    doDarken(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doDarken(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "brighten":
                    doBrighten(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doBrighten(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "shadow":
                    doShadow(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doShadow(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                case "crystal":
                    doCrystal(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
                    doCrystal(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                    break;
                default:
                    break;
            }
            rootPage.image.srcPixels = (byte[])rootPage.image.dstPixels.Clone();
            rootPage.imageOriginal.srcPixels = (byte[])rootPage.imageOriginal.dstPixels.Clone();
            rootPage.Panel.ArchiveAddArray();
            rootPage.OptionsPopup.effectsApplied.Add("Filter = " + filter);
            rootPage.ResetFilterMenuData();
            rootPage.ImageLoadingRing.IsActive = false;
        }

        public void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            rootPage.BackPopupClicked(sender, e);
        }

        public void OnFilterResetClick(object sender, RoutedEventArgs e)
        {
            // This resets the interface and returns the last applied image.
            if (rootPage.pictureIsLoaded)
            {
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                rootPage.image.Reset();
                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
            rootPage.FiltersPopup.FilterApplyReset.Visibility = Visibility.Collapsed;
            rootPage.ResetFilterMenuData();
            rootPage.FiltersPopup.deselectFilters();
        }


    }
}
