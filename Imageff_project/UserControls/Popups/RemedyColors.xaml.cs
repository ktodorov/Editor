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
    public sealed partial class RemedyColors : UserControl
    {
        public string appliedColors = null;

        MainPage rootPage = MainPage.Current;

        public RemedyColors()
        {
            this.InitializeComponent();
        }

        public void OnColorChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
                rootPage.image.ColorChange(BlueColorSlider.Value, GreenColorSlider.Value, RedColorSlider.Value, BlueContrastSlider.Value, GreenContrastSlider.Value, RedContrastSlider.Value);
                rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            }
        }

        public void OnColorApplyClick(object sender, RoutedEventArgs e)
        {
            ApplyColor();
            rootPage.FiltersPopup.setFilterBitmaps();
            rootPage.Saved = false;
        }

        public void ApplyColor()
        {
            rootPage.ImageLoadingRing.IsActive = true;
            ColorApplyReset.Visibility = Visibility.Collapsed;
            rootPage.Menu.SelectColors.IsChecked = false;
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            rootPage.imageOriginal.ColorChange(BlueColorSlider.Value, GreenColorSlider.Value, RedColorSlider.Value, BlueContrastSlider.Value, GreenContrastSlider.Value, RedContrastSlider.Value);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);

            rootPage.image.srcPixels = (byte[])rootPage.image.dstPixels.Clone();
            rootPage.imageOriginal.srcPixels = (byte[])rootPage.imageOriginal.dstPixels.Clone();

            rootPage.Panel.ArchiveAddArray();
            rootPage.OptionsPopup.effectsApplied.Add("Color = " + BlueColorSlider.Value + "," + GreenColorSlider.Value + "," + RedColorSlider.Value + "," + BlueContrastSlider.Value + "," + GreenContrastSlider.Value + "," + RedContrastSlider.Value);
            ResetColorMenuData();
            rootPage.ImageLoadingRing.IsActive = false;
        }

        public void OnColorResetClick(object sender, RoutedEventArgs e)
        {
            // This resets the interface and returns the last applied image.
            ResetColorMenuData();
            rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            rootPage.image.Reset();
            rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            ColorApplyReset.Visibility = Visibility.Collapsed;
        }

        public void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            rootPage.BackPopupClicked(sender, e);
        }

        // Reset the data of Color menu
        public void ResetColorMenuData()
        {
            appliedColors = null;

            BlueColorSlider.Value = 0;
            GreenColorSlider.Value = 0;
            RedColorSlider.Value = 0;

            BlueContrastSlider.Value = 0;
            GreenContrastSlider.Value = 0;
            RedContrastSlider.Value = 0;
        }

        public void importColor(string[] temp)
        {
            BlueColorSlider.Value = Convert.ToDouble(temp[0]);
            GreenColorSlider.Value = Convert.ToDouble(temp[1]);
            RedColorSlider.Value = Convert.ToDouble(temp[2]);
            ApplyColor();
        }

        public void importContrast(string[] temp)
        {
            BlueContrastSlider.Value = Convert.ToDouble(temp[0]);
            GreenContrastSlider.Value = Convert.ToDouble(temp[1]);
            RedContrastSlider.Value = Convert.ToDouble(temp[2]);
            ApplyColor();
        }

    }
}
