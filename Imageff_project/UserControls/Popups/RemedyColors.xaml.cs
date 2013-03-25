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

            rootPage.ArchiveAddArray();
            rootPage.effectsApplied.Add("Color = " + BlueColorSlider.Value + "," + GreenColorSlider.Value + "," + RedColorSlider.Value + "," + BlueContrastSlider.Value + "," + GreenContrastSlider.Value + "," + RedContrastSlider.Value);
            rootPage.ResetColorMenuData();
            rootPage.ImageLoadingRing.IsActive = false;
        }

        public void OnColorResetClick(object sender, RoutedEventArgs e)
        {
            // This resets the interface and returns the last applied image.
            rootPage.ResetColorMenuData();
            rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            rootPage.image.Reset();
            rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
            rootPage.ColorsPopup.ColorApplyReset.Visibility = Visibility.Collapsed;
        }

        public void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            rootPage.BackPopupClicked(sender, e);
        }

    }
}
