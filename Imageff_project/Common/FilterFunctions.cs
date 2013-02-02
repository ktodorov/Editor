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

		#region getters and setters

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
		#endregion

		public enum ColorType
        {
            Red,
            Green,
            Blue,
            Purple
        };  

		public void Reset()
		{
			_dstPixels = (byte[]) _srcPixels.Clone();
		}

        #region Color Change
        public void ColorChange(double value, ColorType color)
        {
            _dstPixels = (byte[])_srcPixels.Clone();
            int temp;
            int currentByte = GetColor(color);
            for (; currentByte < 4 * height * width; currentByte += 4)
            {
                temp = dstPixels[currentByte] + (int)value;
                ColorChange_CheckValue(ref temp);
                dstPixels[currentByte] = (byte)temp;
            }
        }

        public void ColorChange_CheckValue(ref int val)
        {
            if (val > 200)
                val = 200;
            else if (val < 20)
                val = 20;
        }
        #endregion

        #region Contrast Change 
        public void Contrast(double contrast, ColorType color)
        {
            int currentByte = GetColor(color);
            _dstPixels = (byte[])_srcPixels.Clone(); 
            contrast = Contrast_GetContrastValue(contrast);
            Contrast_GetNewPixels(currentByte, contrast);           
        }

        public void Contrast_GetNewPixels(int currentByte, double contrast)
        {
            double temp;

            for (; currentByte < 4 * height * width; currentByte += 4)
            {
                temp = Contrast_GetNewColor(dstPixels[currentByte], contrast);
                Contrast_CheckValue(ref temp);
                dstPixels[currentByte] = (byte)temp;
            }
        }

        public double Contrast_GetNewColor(double temp, double contrast)
        {
            temp /= 255.0;
            temp -= 0.5;
            temp *= contrast;
            temp += 0.5;
            return temp *= 255;
        }

        public void Contrast_CheckValue(ref double val)
        {
            if (val > 255)
                val = 255;
            else if (val < 0)
                val = 0;
        }

        public double Contrast_GetContrastValue(double contrast)
        {
            contrast = (100.0 + contrast) / 100.0;
            return contrast *= contrast;
        }
        #endregion

        public int GetColor(ColorType color)
        {
            switch (color)
            {
                case ColorType.Red:
                    return 2;
                case ColorType.Green:
                    return 1;
                default:
                    return 0;
            }
        }       

        #region BlackAndWhite
        public void BlackAndWhite(byte[] dstPixels, byte[] srcPixels)
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