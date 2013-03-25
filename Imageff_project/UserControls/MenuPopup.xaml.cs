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
using RemedyPic.UserControls;
using RemedyPic;

namespace RemedyPic.UserControls
{
    public sealed partial class MenuPopup : UserControl
    {
        public MenuPopup()
        {
            this.InitializeComponent();
        }

        #region Checked Menu Buttons
        // The events are called when a Menu button is checked or unchecked.

        public void FiltersChecked(object sender, RoutedEventArgs e)
        {
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            SelectExposure.IsChecked = false;
            SelectCrop.IsChecked = false;
            SelectCustom.IsChecked = false;
            //PopupFilters.IsOpen = true;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Effects-check.png");
            EffectsIcon.Source = temp;
        }

        public void FiltersUnchecked(object sender, RoutedEventArgs e)
        {
            //PopupFilters.IsOpen = false;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Effects.png");
            EffectsIcon.Source = temp;
        }

        public void ColorsChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            SelectExposure.IsChecked = false;
            SelectCrop.IsChecked = false;
            SelectCustom.IsChecked = false;

            //PopupColors.IsOpen = true;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Colors-checked.png");
            ColorsIcon.Source = temp;

        }

        public void ColorsUnchecked(object sender, RoutedEventArgs e)
        {
            //PopupColors.IsOpen = false;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Colors.png");
            ColorsIcon.Source = temp;
        }

        public void ExposureChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectCrop.IsChecked = false;
            SelectCustom.IsChecked = false;
            //PopupExposure.IsOpen = true;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Exposure-checked.png");
            ExposureIcon.Source = temp;
        }

        public void ExposureUnchecked(object sender, RoutedEventArgs e)
        {
            //PopupExposure.IsOpen = false;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Exposure.png");
            ExposureIcon.Source = temp;
        }

        public void RotationsChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            SelectCrop.IsChecked = false;
            SelectExposure.IsChecked = false;
            SelectCustom.IsChecked = false;
            //PopupRotations.IsOpen = true;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Rotate-checked.png");
            RotateIcon.Source = temp;
        }

        public void RotationsUnchecked(object sender, RoutedEventArgs e)
        {
            //PopupRotations.IsOpen = false;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Rotate.png");
            RotateIcon.Source = temp;
        }

        public void OptionsChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            SelectCrop.IsChecked = false;
            SelectExposure.IsChecked = false;
            SelectCustom.IsChecked = false;
            //PopupImageOptions.IsOpen = true;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Options-checked.png");
            OptionsIcon.Source = temp;
        }

        public void OptionsUnchecked(object sender, RoutedEventArgs e)
        {
            //PopupImageOptions.IsOpen = false;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Options.png");
            OptionsIcon.Source = temp;
        }

        public void ColorizeChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectHistogram.IsChecked = false;
            SelectExposure.IsChecked = false;
            SelectCrop.IsChecked = false;
            SelectCustom.IsChecked = false;
            //PopupColorize.IsOpen = true;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Colorize-checked.png");
            ColorizeIcon.Source = temp;
        }

        public void ColorizeUnchecked(object sender, RoutedEventArgs e)
        {
            //PopupColorize.IsOpen = false;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Colorize.png");
            ColorizeIcon.Source = temp;
        }

        public void FramesChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectHistogram.IsChecked = false;
            SelectExposure.IsChecked = false;
            SelectCrop.IsChecked = false;
            SelectCustom.IsChecked = false;
            //PopupFrames.IsOpen = true;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Frame-checked.png");
            FramesIcon.Source = temp;
        }

        public void FramesUnchecked(object sender, RoutedEventArgs e)
        {
            //PopupFrames.IsOpen = false;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Frame.png");
            FramesIcon.Source = temp;
        }

        public void HistogramChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectExposure.IsChecked = false;
            SelectCrop.IsChecked = false;
            SelectCustom.IsChecked = false;
            //PopupHistogram.IsOpen = true;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Histogram-checked.png");
            HistogramIcon.Source = temp;
        }

        public void HistogramUnchecked(object sender, RoutedEventArgs e)
        {
            //PopupHistogram.IsOpen = false;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Histogram.png");
            HistogramIcon.Source = temp;
        }


        public void CustomFilterChecked(object sender, RoutedEventArgs e)
        {
            SelectFilters.IsChecked = false;
            SelectColors.IsChecked = false;
            SelectRotations.IsChecked = false;
            SelectOptions.IsChecked = false;
            SelectColorize.IsChecked = false;
            SelectFrames.IsChecked = false;
            SelectExposure.IsChecked = false;
            SelectCrop.IsChecked = false;
            SelectHistogram.IsChecked = false;
            //PopupCustomFilter.IsOpen = true;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/CustomFilter-checked.png");
            CustomIcon.Source = temp;
        }

        public void CustomFilterUnchecked(object sender, RoutedEventArgs e)
        {
            //PopupCustomFilter.IsOpen = false;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/CustomFilter.png");
            CustomIcon.Source = temp;
        }

		public void deselectPopups()
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
            SelectCustom.IsChecked = false;
	}


        public void CropChecked(object sender, RoutedEventArgs e)
        {
            // Called when the Crop button is checked.
            deselectPopups();
            //Crop.Visibility = Visibility.Visible;
            //imageDisplayed.imageCanvas.Visibility = Visibility.Visible;
            //imageDisplayed.displayGrid.Margin = new Thickness(15);
            //ResetZoomPos();
			
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Crop-checked.png");
            CropIcon.Source = temp;
        }

        public void CropUnchecked(object sender, RoutedEventArgs e)
        {
            // Called when the Crop button is unchecked.
            //Crop.Visibility = Visibility.Collapsed;
            //imageDisplayed.imageCanvas.Visibility = Visibility.Collapsed;
            //imageDisplayed.selectedRegion.ResetCorner(0, 0, imageDisplayed.displayImage.ActualWidth, imageDisplayed.displayImage.ActualHeight);
            //imageDisplayed.displayGrid.Margin = new Thickness(0);
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Crop.png");
            CropIcon.Source = temp;
        }

        #endregion

        #region Button Icons Events
        public void OnEffectsPointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectFilters.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Effects-hover.png");
                EffectsIcon.Source = temp;
            }
        }

        public void OnEffectsPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectFilters.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Effects.png");
                EffectsIcon.Source = temp;
            }
        }

        public void OnColorsPointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectColors.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Colors-hover.png");
                ColorsIcon.Source = temp;
            }
        }

        public void OnColorsPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectColors.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Colors.png");
                ColorsIcon.Source = temp;
            }
        }

        public void OnExposurePointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectExposure.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Exposure-hover.png");
                ExposureIcon.Source = temp;
            }
        }

        public void OnExposurePointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectExposure.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Exposure.png");
                ExposureIcon.Source = temp;
            }
        }

        public void OnRotatePointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectRotations.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Rotate-hover.png");
                RotateIcon.Source = temp;
            }
        }

        public void OnRotatePointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectRotations.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Rotate.png");
                RotateIcon.Source = temp;
            }
        }

        public void OnColorizePointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectColorize.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Colorize-hover.png");
                ColorizeIcon.Source = temp;
            }
        }

        public void OnColorizePointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectColorize.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Colorize.png");
                ColorizeIcon.Source = temp;
            }
        }

        public void OnFramePointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectFrames.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Frame-hover.png");
                FramesIcon.Source = temp;
            }
        }

        public void OnFramePointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectFrames.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Frame.png");
                FramesIcon.Source = temp;
            }
        }

        public void OnHistogramPointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectHistogram.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Histogram-hover.png");
                HistogramIcon.Source = temp;
            }
        }

        public void OnHistogramPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectHistogram.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Histogram.png");
                HistogramIcon.Source = temp;
            }
        }

        public void OnCropPointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectCrop.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Crop-hover.png");
                CropIcon.Source = temp;
            }
        }

        public void OnCropPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectCrop.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Crop.png");
                CropIcon.Source = temp;
            }
        }

        public void OnOptionsPointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectOptions.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Options-hover.png");
                OptionsIcon.Source = temp;
            }
        }

        public void OnOptionsPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectOptions.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Options.png");
                OptionsIcon.Source = temp;
            }
        }


        public void OnCustomFilterPointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (SelectCustom.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/CustomFilter-hover.png");
                CustomIcon.Source = temp;
            }
        }

        public void OnCustomFilterPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (SelectCustom.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/CustomFilter.png");
                CustomIcon.Source = temp;
            }
        }

        #endregion




    }
}
