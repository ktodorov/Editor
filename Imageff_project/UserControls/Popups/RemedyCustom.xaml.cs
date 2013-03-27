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
using RemedyPic.RemedyClasses;

namespace RemedyPic.UserControls.Popups
{
	public sealed partial class RemedyCustom : UserControl
	{
		MainPage pageRoot = MainPage.Current;

		public RemedyCustom()
		{
			this.InitializeComponent();
		}

		public void OnCoeffChanged(object sender, TextChangedEventArgs text)
		{
			CustomApplyReset.Visibility = Visibility.Visible;
			TextBox val = (TextBox)text.OriginalSource;
			if (val != null)
			{
				CustomFilter_CheckValue(ref val);
			}
			CustomFilter_CalculateScaleOffset();
		}

		// SCALE AND OFFSET
		public void OnScaleChanged(object sender, TextChangedEventArgs text)
		{
			CustomApplyReset.Visibility = Visibility.Visible;
			CustomFilter_CheckScale();
		}

		public void OnOffsetChanged(object sender, TextChangedEventArgs text)
		{
			CustomApplyReset.Visibility = Visibility.Visible;
			CustomFilter_CheckValue(ref Offset);
		}

		// Check if the value of text box is number and set scale and offset
		public void CustomFilter_CheckValue(ref TextBox coeff)
		{
			try
			{
				Convert.ToInt32(coeff.Text);
			}
			catch (FormatException e)
			{
				if (!(coeff.Text.Length == 1 && coeff.Text[0] == '-'))
					coeff.Text = "";
			}
		}

		// Check if the scale is > 0
		public void CustomFilter_CheckScale()
		{
			try
			{
				int val = Convert.ToInt32(Scale.Text);

				if (val == 0)
					Scale.Text = "";
			}
			catch (FormatException e)
			{
				Scale.Text = "";
			}
		}

		//Calculate the Scale and offset
		public void CustomFilter_CalculateScaleOffset()
		{
			int[,] coeff = new int[5, 5];
			int offset = 0, scale = 1;
			CustomFilter_SetValues(ref coeff, ref offset, ref scale);
			int sum = 0;

			foreach (int val in coeff)
			{
				sum += val;
			}

			if (sum != 0)
				scale = Math.Abs(sum);
			else
				scale = 1;

			if (sum / scale == 1)
				offset = 0;
			else if (sum == 0)
				offset = 128;
			else if (sum / scale == -1)
				offset = 255;

			Scale.Text = scale.ToString();
			Offset.Text = offset.ToString();
		}

		// Review button click
		public void OnCustomReviewClick(object sender, RoutedEventArgs e)
		{
			CustomReview();
		}

		public void CustomReview()
		{
			pageRoot.ImageLoadingRing.IsActive = true;

			if (pageRoot.pictureIsLoaded)
			{
				pageRoot.prepareImage(pageRoot.bitmapStream, pageRoot.bitmapImage, pageRoot.imageOriginal);
				int[,] coeff = new int[5, 5];
				int offset = 0, scale = 0;
				CustomFilter_SetValues(ref coeff, ref offset, ref scale);

				CustomFilter custom_image = new CustomFilter(pageRoot.imageOriginal.srcPixels, pageRoot.imageOriginal.width, pageRoot.imageOriginal.height, offset, scale, coeff);
				pageRoot.imageOriginal.dstPixels = custom_image.Filter();

				pageRoot.setStream(pageRoot.bitmapStream, pageRoot.bitmapImage, pageRoot.imageOriginal);
			}

			pageRoot.ImageLoadingRing.IsActive = false;
		}

		// Apply button click
		public void OnCustomApplyClick(object sender, RoutedEventArgs e)
		{
			CustomApply();
			CustomApplyReset.Visibility = Visibility.Collapsed;
			pageRoot.setExampleBitmaps();
			pageRoot.FiltersPopup.setFilterBitmaps();
			pageRoot.Saved = false;
		}

		public void CustomApply()
		{
			pageRoot.ImageLoadingRing.IsActive = true;

			if (pageRoot.pictureIsLoaded)
			{
				pageRoot.prepareImage(pageRoot.bitmapStream, pageRoot.bitmapImage, pageRoot.imageOriginal);
				int[,] coeff = new int[5, 5];
				int offset = 0, scale = 0;
				CustomFilter_SetValues(ref coeff, ref offset, ref scale);

				CustomFilter custom_image = new CustomFilter(pageRoot.imageOriginal.srcPixels, pageRoot.imageOriginal.width, pageRoot.imageOriginal.height, offset, scale, coeff);
				pageRoot.imageOriginal.dstPixels = custom_image.Filter();

				pageRoot.setStream(pageRoot.bitmapStream, pageRoot.bitmapImage, pageRoot.imageOriginal);
			}

			pageRoot.imageOriginal.srcPixels = (byte[])pageRoot.imageOriginal.dstPixels.Clone();
			pageRoot.Panel.ArchiveAddArray();
			pageRoot.OptionsPopup.effectsApplied.Add("Custom = " + "TODO");
			CustomFilter_ResetValues();
			pageRoot.ImageLoadingRing.IsActive = false;
		}

		// Reset button click
		public void OnCustomResetClick(object sender, RoutedEventArgs e)
		{
			CustomFilter_ResetValues();

			if (pageRoot.pictureIsLoaded)
			{
				pageRoot.prepareImage(pageRoot.bitmapStream, pageRoot.bitmapImage, pageRoot.imageOriginal);
				pageRoot.imageOriginal.dstPixels = (byte[])pageRoot.imageOriginal.srcPixels.Clone();
				pageRoot.setStream(pageRoot.bitmapStream, pageRoot.bitmapImage, pageRoot.imageOriginal);
			}
			CustomApplyReset.Visibility = Visibility.Collapsed;
		}

		// Reset All values of custom filter
		public void CustomFilter_ResetValues()
		{
			coeff00.Text = coeff10.Text = coeff20.Text = coeff30.Text = coeff40.Text = "";
			coeff01.Text = coeff11.Text = coeff21.Text = coeff31.Text = coeff41.Text = "";
			coeff02.Text = coeff12.Text = coeff32.Text = coeff42.Text = "";
			coeff03.Text = coeff13.Text = coeff23.Text = coeff33.Text = coeff43.Text = "";
			coeff04.Text = coeff14.Text = coeff24.Text = coeff34.Text = coeff44.Text = "";
			coeff22.Text = "1";

			Scale.Text = "1";
			Offset.Text = "0";
		}

		// Set the matrix for custom filter
		public void CustomFilter_SetValues(ref int[,] coeff, ref int offset, ref int scale)
		{
			CustomFilter_SetValue(ref coeff[0, 0], coeff00);
			CustomFilter_SetValue(ref coeff[1, 0], coeff10);
			CustomFilter_SetValue(ref coeff[2, 0], coeff20);
			CustomFilter_SetValue(ref coeff[3, 0], coeff30);
			CustomFilter_SetValue(ref coeff[4, 0], coeff40);

			CustomFilter_SetValue(ref coeff[0, 1], coeff01);
			CustomFilter_SetValue(ref coeff[1, 1], coeff11);
			CustomFilter_SetValue(ref coeff[2, 1], coeff21);
			CustomFilter_SetValue(ref coeff[3, 1], coeff31);
			CustomFilter_SetValue(ref coeff[4, 1], coeff41);

			CustomFilter_SetValue(ref coeff[0, 2], coeff02);
			CustomFilter_SetValue(ref coeff[1, 2], coeff12);
			CustomFilter_SetValue(ref coeff[2, 2], coeff22);
			CustomFilter_SetValue(ref coeff[3, 2], coeff32);
			CustomFilter_SetValue(ref coeff[4, 2], coeff42);

			CustomFilter_SetValue(ref coeff[0, 3], coeff03);
			CustomFilter_SetValue(ref coeff[1, 3], coeff13);
			CustomFilter_SetValue(ref coeff[2, 3], coeff23);
			CustomFilter_SetValue(ref coeff[3, 3], coeff33);
			CustomFilter_SetValue(ref coeff[4, 3], coeff43);

			CustomFilter_SetValue(ref coeff[0, 4], coeff04);
			CustomFilter_SetValue(ref coeff[1, 4], coeff14);
			CustomFilter_SetValue(ref coeff[2, 4], coeff24);
			CustomFilter_SetValue(ref coeff[3, 4], coeff34);
			CustomFilter_SetValue(ref coeff[4, 4], coeff44);

			CustomFilter_SetScale(ref scale);
			CustomFilter_SetValue(ref offset, Offset);
		}

		// Set one coeff of matrix
		public void CustomFilter_SetValue(ref int coeff, TextBox val)
		{
			int new_val = 0;
			try
			{
				new_val = Convert.ToInt32(val.Text);
			}
			catch (FormatException e)
			{
				if (!(val.Text.Length == 1 && val.Text[0] == '-'))
					val.Text = "";
			}

			if (new_val != 0)
				coeff = new_val;
		}

		// Set the scale value of custom filter
		public void CustomFilter_SetScale(ref int scale)
		{
			int new_val = 0;
			try
			{
				new_val = Convert.ToInt32(Scale.Text);

				if (new_val <= 0)
				{
					Scale.Text = "";
					scale = 1;
				}
			}
			catch (FormatException e)
			{
				Scale.Text = "";
				scale = 1;
			}

			if (new_val > 0)
				scale = new_val;
		}

		public void BackPopupClicked(object sender, RoutedEventArgs e)
		{
			pageRoot.BackPopupClicked(sender, e);
		}

	}
}
