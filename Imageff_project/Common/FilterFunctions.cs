using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace RemedyPic.Common
{
	class FilterFunctions
	{
		static byte[] invertedAlreadyArray, blackWhiteAlreadyArray;

		public static void BlackAndWhite(ref byte[] dstPixels, ref byte[] srcPixels, ref int height, ref int width, bool alreadyDone = false)
		{
			int currentByte = 0;
			if (alreadyDone)
			{
				dstPixels = blackWhiteAlreadyArray;
			}
			else
			{
				while (currentByte < (4 * height * width))
				{
					blackWhiteAlreadyArray = srcPixels;
					int baw = (srcPixels[currentByte] + srcPixels[currentByte + 1] + srcPixels[currentByte + 2]) / 3;
					Color tempColor = Color.FromArgb(srcPixels[currentByte + 3], (byte)baw, (byte)baw, (byte)baw);
					dstPixels[currentByte++] = tempColor.B;
					dstPixels[currentByte++] = tempColor.G;
					dstPixels[currentByte++] = tempColor.R;
					dstPixels[currentByte++] = tempColor.A;

				}
			}
		}

		public static void Filter(ref byte[] dstPixels, ref byte[] srcPixels, ref int height, ref int width, bool invertedAlready = false)
		{
			int currentByte = 0;
			if (invertedAlready)
			{
				dstPixels = invertedAlreadyArray;
				invertedAlreadyArray = new byte[0];
			}
			else
			{
				invertedAlreadyArray = srcPixels;
				while (currentByte < (4 * height * width))
				{
					dstPixels[currentByte++] = (byte)(-srcPixels[currentByte - 1] + 255);
					dstPixels[currentByte++] = (byte)(-srcPixels[currentByte - 1] + 255);
					dstPixels[currentByte++] = (byte)(-srcPixels[currentByte - 1] + 255);
					dstPixels[currentByte++] = srcPixels[currentByte - 4];
				}
			}
		}

		public static void Darken(ref byte[] dstPixels, ref byte[] srcPixels, ref int height, ref int width, double value)
		{
			double darkness = -value;
			darkness = (1 / darkness);
			if (darkness != 1)
				darkness += .1;
			int currentByte = 0;
			while (currentByte < (4 * height * width))
			{
				dstPixels[currentByte] = (byte)(srcPixels[currentByte++] * darkness);
				dstPixels[currentByte] = (byte)(srcPixels[currentByte++] * darkness);
				dstPixels[currentByte] = (byte)(srcPixels[currentByte++] * darkness);
				dstPixels[currentByte] = srcPixels[currentByte++];
			}
		}

		#region Lighten function
		public static void Lighten(ref byte[] dstPixels, ref byte[] srcPixels, ref int height, ref int width, double value)
		{
			// This function lighten the Writeablebitmap picture
			// by taking the array and multiplying every pixel with the (value of the slider * 0,05) + 1
			double brightness = (value * 0.05) + 1;
			int currentByte = 0;
			while (currentByte < (4 * height * width))
			{
				if ((srcPixels[currentByte] * brightness) > 255)
					dstPixels[currentByte++] = 255;
				else
					dstPixels[currentByte] = (byte)(srcPixels[currentByte++] * brightness);
				if ((srcPixels[currentByte] * brightness) > 255)
					dstPixels[currentByte++] = 255;
				else
					dstPixels[currentByte] = (byte)(srcPixels[currentByte++] * brightness);
				if ((srcPixels[currentByte] * brightness) > 255)
					dstPixels[currentByte++] = 255;
				else
					dstPixels[currentByte] = (byte)(srcPixels[currentByte++] * brightness);
				dstPixels[currentByte] = srcPixels[currentByte++];
			}
		}
		#endregion

	}
}
