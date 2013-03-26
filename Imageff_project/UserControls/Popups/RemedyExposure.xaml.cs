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
using Windows.UI.Xaml.Navigation;

namespace RemedyPic.UserControls.Popups
{
    public sealed partial class RemedyExposure : UserControl
    {
        public string appliedExposure = null;
        MainPage rootPage = MainPage.Current;


        public RemedyExposure()
        {
            this.InitializeComponent();
        }

        // Event for apply button on Exposure popup. Sets the image with the applied exposure
        public void OnExposureApplyClick(object sender, RoutedEventArgs e)
        {
            ApplyExposure(appliedExposure);
            rootPage.FiltersPopup.setFilterBitmaps();
            rootPage.Saved = false;
        }


        public void ApplyExposure(string effect)
        {
            rootPage.ImageLoadingRing.IsActive = true;
            ExposureApplyReset.Visibility = Visibility.Collapsed;
            rootPage.Menu.SelectExposure.IsChecked = false;

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

            rootPage.image.srcPixels = (byte[])rootPage.image.dstPixels.Clone();
            rootPage.imageOriginal.srcPixels = (byte[])rootPage.imageOriginal.dstPixels.Clone();
            rootPage.ArchiveAddArray();
            rootPage.effectsApplied.Add("Exposure = " + brightSlider.Value + "," + BlueGammaSlider.Value + "," + GreenGammaSlider.Value + "," + RedGammaSlider.Value);
            ResetExposureMenuData();
            rootPage.ImageLoadingRing.IsActive = false;
        }


        public void OnExposureResetClick(object sender, RoutedEventArgs e)
        {
            ResetExposureMenuData();
            ExposureApplyReset.Visibility = Visibility.Collapsed;
        }



        // The event is called when the Gama slider or Brighr slider is changed.
        public void OnExposureChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                rootPage.image.dstPixels = (byte[])rootPage.image.srcPixels.Clone();
                rootPage.image.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
                // We check if the changed value 
                // is higher than 0 - we call the brightness function
                // is lower than 0  - we call the darkness function
                // And finally we save the new byte array to the image.
                if (brightSlider.Value < 0)
                {
                    appliedExposure = "gammadarken";
                    rootPage.image.Darken(brightSlider.Value);
                }
                else if (brightSlider.Value >= 0)
                {
                    appliedExposure = "gammalighten";
                    rootPage.image.Lighten(brightSlider.Value);
                }
                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }


        public void doGammaDarken()
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
            rootPage.imageOriginal.Darken(brightSlider.Value);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
        }

        public void doGammaLighten()
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
            rootPage.imageOriginal.Lighten(brightSlider.Value);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
        }


        // Reset the slider values of Exposure Menu
        public void ResetExposureMenuData()
        {
            brightSlider.Value = 0;

            BlueGammaSlider.Value = 10;
            GreenGammaSlider.Value = 10;
            RedGammaSlider.Value = 10;
        }

        public void Import(string[] temp)
        {
            brightSlider.Value = Convert.ToDouble(temp[0]);
            BlueGammaSlider.Value = Convert.ToDouble(temp[1]);
            GreenGammaSlider.Value = Convert.ToDouble(temp[2]);
            RedGammaSlider.Value = Convert.ToDouble(temp[3]);
            if (brightSlider.Value < 0)
                ApplyExposure("gammadarken");
            else
                ApplyExposure("gammalighten");
        }

        public void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            rootPage.BackPopupClicked(sender, e);
        }

    }
}
