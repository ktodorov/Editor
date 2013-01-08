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
		static byte[] blackWhiteAlreadyArray;
		private int _width, _height;
		private byte[] _dstPixels, _srcPixels;
		public byte[] dstPixels
		{
			get
			{
				return _dstPixels;
			}
			set
			{
				_dstPixels = value;
			}
		}
		public byte[] srcPixels
		{
			get
			{
				return _srcPixels;
			}
			set
			{
				_srcPixels = value;
			}
		}
		public int height
		{
			get
			{
				return _height;
			}
			set
			{
				_height = value;
			}
		}
		public int width
		{
			get
			{
				return _width;
			}
			set
			{
				_width = value;
			}
		}

		public void Reset()
		{
			dstPixels = srcPixels;
		}

		#region BlackAndWhite
		public void BlackAndWhite()
		{
			int currentByte = 0;

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
		#endregion

		#region Darken
		public void Darken(double value)
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
		#endregion

		#region Lighten function
		public void Lighten(double value)
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