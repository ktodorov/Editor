using RemedyPic.RemedyClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace RemedyPic.UserControls.Popups
{
    public sealed partial class RemedyColorize : UserControl
    {

        // Colorize selected colors
        public bool redForColorize, greenForColorize, blueForColorize, yellowForColorize,
                         orangeForColorize, purpleForColorize, cyanForColorize, limeForColorize = false;

        MainPage rootPage = MainPage.Current;

        public RemedyColorize()
        {
            this.InitializeComponent();
        }


        // Reset the data of Colorize menu
        public void ResetColorizeMenuData()
        {
            redForColorize = greenForColorize = blueForColorize = yellowForColorize =
                             orangeForColorize = purpleForColorize = cyanForColorize =
                             limeForColorize = false;
            deselectColorizeGridItems();
        }

        // Event for apply button on Colorize popup. Sets the image with the applied color
        public void OnColorizeApplyClick(object sender, RoutedEventArgs e)
        {
            doColorize(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            ApplyColorize();
            rootPage.FiltersPopup.setFilterBitmaps();
            ColorizeApplyReset.Visibility = Visibility.Collapsed;
            rootPage.Menu.SelectColorize.IsChecked = false;
            rootPage.Saved = false;
        }

        public void ApplyColorize()
        {
            rootPage.ImageLoadingRing.IsActive = true;
            rootPage.image.srcPixels = (byte[])rootPage.image.dstPixels.Clone();
            rootPage.imageOriginal.srcPixels = (byte[])rootPage.imageOriginal.dstPixels.Clone();
            rootPage.ArchiveAddArray();
            Colorize_SetColorizeEffect();
            rootPage.ImageLoadingRing.IsActive = false;
        }

        public void Colorize_SetColorizeEffect()
        {
            string colorizeColors = "";
            Colorize_GetColorizeColors(ref colorizeColors);
            rootPage.effectsApplied.Add("Colorize = " + colorizeColors);

        }

        public void Colorize_GetColorizeColors(ref string colorizeColors)
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

        public void Colorize_CheckForFirsColor(ref string colorizeColors)
        {
            if (!colorizeColors.Equals(""))
            {
                colorizeColors += ",";
            }

        }

        public void OnColorizeResetClick(object sender, RoutedEventArgs e)
        {
            deselectColorizeGridItems();
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            redForColorize = greenForColorize = blueForColorize = yellowForColorize =
                 orangeForColorize = purpleForColorize = cyanForColorize =
                 limeForColorize = false;
        }

        #region Colorize events
        // Events for checking and unchecking the colorize rectangles.
        public void blueColorize_Checked(object sender, RoutedEventArgs e)
        {
            blueForColorize = true;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            blueRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
        }
        public void blueColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            blueForColorize = false;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            blueRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255));
        }

        public void redColorize_Checked(object sender, RoutedEventArgs e)
        {
            redForColorize = true;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            redRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        }
        public void redColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            redForColorize = false;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            redRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        }

        public void yellowColorize_Checked(object sender, RoutedEventArgs e)
        {
            yellowForColorize = true;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            yellowRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
        }
        public void yellowColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            yellowForColorize = false;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            yellowRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0));
        }

        public void orangeColorize_Checked(object sender, RoutedEventArgs e)
        {
            orangeForColorize = true;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            orangeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 116, 0));
        }
        public void orangeColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            orangeForColorize = false;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            orangeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 116, 0));
        }

        public void greenColorize_Checked(object sender, RoutedEventArgs e)
        {
            greenForColorize = true;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            greenRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 120, 0));
        }
        public void greenColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            greenForColorize = false;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            greenRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 90, 0));
        }

        public void cyanColorize_Checked(object sender, RoutedEventArgs e)
        {
            cyanForColorize = true;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            cyanRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255));
        }
        public void cyanColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            cyanForColorize = false;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            cyanRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255));
        }

        public void purpleColorize_Checked(object sender, RoutedEventArgs e)
        {
            purpleForColorize = true;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            purpleRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 255));
        }
        public void purpleColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            purpleForColorize = false;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            purpleRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 255));
        }

        public void limeColorize_Checked(object sender, RoutedEventArgs e)
        {
            limeForColorize = true;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            limeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 25, 255, 25));
        }
        public void limeColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            limeForColorize = false;
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            limeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 25, 255, 25));
        }

        #endregion

        public void deselectColorizeGridItems()
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

        public void doColorize(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            // Call the colorize function
            rootPage.prepareImage(stream, bitmap, givenImage);
            givenImage.Colorize(blueForColorize, redForColorize, greenForColorize, yellowForColorize,
                                        orangeForColorize, purpleForColorize, cyanForColorize, limeForColorize);
            rootPage.setStream(stream, bitmap, givenImage);
            ColorizeApplyReset.Visibility = Visibility.Visible;
        }


        public void Import(string[] temp)
        {
            for (int k = 0; k < temp.Length; k++)
            {
                checkColorizeColor(temp[k]);
            }
            doColorize(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            ApplyColorize();
        }


        public void checkColorizeColor(string color)
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

        public void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            rootPage.BackPopupClicked(sender, e);
        }

    }
}
