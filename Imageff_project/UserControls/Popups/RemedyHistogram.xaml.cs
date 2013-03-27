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
	public sealed partial class RemedyHistogram : UserControl
	{
		MainPage rootPage = MainPage.Current;

		public RemedyHistogram()
		{
			this.InitializeComponent();
		}


		public void HistogramClicked(object sender, RoutedEventArgs e)
		{
			// Equalize the histogram of the current image.
			rootPage.Menu.SelectHistogram.IsChecked = false;
			equalizeHistogram();
			rootPage.FiltersPopup.setFilterBitmaps();
		}

		public void equalizeHistogram()
		{
			rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
			rootPage.imageOriginal.MakeHistogramEqualization();
			rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
			rootPage.prepareImage(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
			rootPage.image.MakeHistogramEqualization();
			rootPage.setStream(rootPage.exampleStream, rootPage.exampleBitmap, rootPage.image);
			rootPage.image.srcPixels = (byte[])rootPage.image.dstPixels.Clone();
			rootPage.imageOriginal.srcPixels = (byte[])rootPage.imageOriginal.dstPixels.Clone();
			rootPage.Panel.ArchiveAddArray();
			rootPage.OptionsPopup.effectsApplied.Add("Histogram = true");
		}

		public void BackPopupClicked(object sender, RoutedEventArgs e)
		{
			rootPage.BackPopupClicked(sender, e);
		}

	}
}
