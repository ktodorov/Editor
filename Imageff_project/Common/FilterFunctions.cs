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
        // Main function which changes RGB colors
        public void ColorChange(double RedColorValue, double GreenColorValue, double BlueColorValue, double RedContrastValue, double GreenContrastValue, double BlueContrastValue)
        {
            _dstPixels = (byte[])_srcPixels.Clone();
            for (int CurrentByte = 0; CurrentByte < 4 * height * width; CurrentByte += 4)
            {
                ColorChange_GetNewColors(CurrentByte, RedColorValue, GreenColorValue, BlueColorValue);
                ColorChange_GetNewContrasts(CurrentByte, RedContrastValue, GreenContrastValue, BlueContrastValue);
            }
        }

        #region Colors
        // Gets new values for R G B color of selected pixel of image (depends of the value of R G B color sliders)
        public void ColorChange_GetNewColors(int CurrentByte, double RedValue, double GreenValue, double BlueValue)
        {
            ColorChange_GetNewColor(CurrentByte, (int)BlueValue);
            ColorChange_GetNewColor(CurrentByte + 1, (int)GreenValue);
            ColorChange_GetNewColor(CurrentByte + 2, (int)RedValue);
        }

        // Get new value for one color of selected pixel of image
        public void ColorChange_GetNewColor(int CurrentByte, int value)
        {
            int temp = _dstPixels[CurrentByte] + value;
            ColorChange_CheckColorValue(ref temp);
            _dstPixels[CurrentByte] = (byte)temp; 
        }

        // Sets the value of the color in the bounds [20-200]
        public void ColorChange_CheckColorValue(ref int val)
        {
            if (val > 200)
                val = 200;
            else if (val < 20)
                val = 20;
        }
        #endregion 

        #region Contrasts
        // Gets new values for R G B color of selected pixel of image (depends of the value of R G B contrast sliders)
        public void ColorChange_GetNewContrasts(int CurrentByte, double RedContrastValue, double GreenContrastValue, double BlueContrastValue)
        {
            Contrast_GetContrastValue(ref BlueContrastValue);
            Contrast_GetContrastValue(ref GreenContrastValue);
            Contrast_GetContrastValue(ref RedContrastValue);

            ColorChange_GetNewContrast(CurrentByte, BlueContrastValue);
            ColorChange_GetNewContrast(CurrentByte + 1, GreenContrastValue);
            ColorChange_GetNewContrast(CurrentByte + 2, RedContrastValue);

        }

        // Calculate contrast value of slider to value between 0 and 4
        public void Contrast_GetContrastValue(ref double contrast)
        {
            contrast = (100.0 + contrast) / 100.0;
            contrast *= contrast;
        }

        // Get new value for one color of selected pixel of image
        public void ColorChange_GetNewContrast(int currentByte, double contrast)
        {
            double temp = Contrast_GetNewValue(dstPixels[currentByte], contrast);
            ColorChange_CheckContrastValue(ref temp);
            dstPixels[currentByte] = (byte)temp;
            
        }

        // Calculate the new value of the color
        public double Contrast_GetNewValue(double temp, double contrast)
        {
            temp /= 255.0;
            temp -= 0.5;
            temp *= contrast;
            temp += 0.5;
            return temp *= 255;
        }

        // Sets the value of the color in the bounds [0-255]
        public void ColorChange_CheckContrastValue(ref double val)
        {
            if (val > 255)
                val = 255;
            else if (val < 0)
                val = 0;
        }
        #endregion
        #endregion

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