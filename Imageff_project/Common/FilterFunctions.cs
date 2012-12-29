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

        #region EmbossFilter
        public void EmbossFilter()
        {
            _dstPixels = (byte[])_srcPixels.Clone();
            int current_byte = 0, current_width = 1, current_height = 1;

            while (current_byte < _srcPixels.Length && current_height != _height)
            {                
                if (current_width != _width)
                {   
                    _dstPixels[current_byte] = EmbossFilter_CalculatePixel(current_byte++);
                    _dstPixels[current_byte] = EmbossFilter_CalculatePixel(current_byte++);
                    _dstPixels[current_byte] = EmbossFilter_CalculatePixel(current_byte++);                   
 
                    current_byte++;
                    current_width++;
                }
                else
                {
                    current_width = 1;
                    current_height++;
                    current_byte += 4;
                }
            }
        }

        public byte EmbossFilter_CalculatePixel(int current_byte)
        {
            int val = (_srcPixels[current_byte] - _srcPixels[current_byte + (4 * _width) + 4] + 128);
            
            if (val > 255)
                val = 255;
            else if (val < 0)
                val = 0;

            return (byte) val;
        }
        #endregion
        
        #region BlackAndWhite
        public void BlackAndWhite(bool alreadyDone = false)
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
        #endregion

        #region Filter
        public void Filter(bool invertedAlready = false)
		{
			int currentByte = 240000;
			if (invertedAlready)
			{
				dstPixels = invertedAlreadyArray;
				invertedAlreadyArray = new byte[0];
			}
			else
			{
				invertedAlreadyArray = srcPixels;
                _dstPixels = (byte[])_srcPixels.Clone();
				while (currentByte < (4 * height * width))
				{
					dstPixels[currentByte++] = (byte)(-srcPixels[currentByte - 1] + 255);
					dstPixels[currentByte++] = (byte)(-srcPixels[currentByte - 1] + 255);
					dstPixels[currentByte++] = (byte)(-srcPixels[currentByte - 1] + 255);
                    currentByte++;
				}
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
