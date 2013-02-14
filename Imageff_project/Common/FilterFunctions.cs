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

        #region Sharpen1

        // Main function of sharpen filter
        public void Sharpen1()
        {
            _dstPixels = (byte[])_srcPixels.Clone();
            Random random = new Random();

            for (int CurrentByte = 3, AlphaCoeff = 0, CurrentColumn = 0; CurrentByte < _dstPixels.Length; CurrentByte += 4, CurrentColumn++)
            {
                if (CurrentColumn == _width)
                {
                    CurrentColumn = 0;
                    CurrentByte += 4 * _width * 9;
                }

                if (CurrentColumn % 10 == 0)
                {
                    AlphaCoeff = random.Next(0, 256);
                }

                for (int k = 0, index = CurrentByte; k < 10 && index < _dstPixels.Length; k++, index += 4 * _width)
                {
                    _dstPixels[index] = (byte) AlphaCoeff;
                }
            }
        }
        #endregion


        #region Flip

        #region H Flip
        // Main Flip function
        public void HFlip()
        {
            _dstPixels = (byte[])_srcPixels.Clone();            

            for (int CurrentByte = 0, CurrentColumn = 0, CurrentRow = 0; CurrentByte < 4 * _height * _width; CurrentColumn++)
            {
                HFlip_SetNewValues(ref CurrentColumn,ref CurrentRow,ref CurrentByte);
            }

            //_srcPixels = (byte[])_dstPixels.Clone();
        }

        // Set the new values for the pixel
        public void HFlip_SetNewValues(ref int CurrentColumn, ref int CurrentRow, ref int CurrentByte)
        {
            HFlip_GetNewColumnRowByte(ref CurrentColumn, ref CurrentRow, ref CurrentByte);

            if (CurrentRow != _height)
            {                
                int index = 4 * ((_width - 1 - CurrentColumn) + _width * CurrentRow);
                Flip_SwapPixelData(ref CurrentByte, index);
            }
        }

        // Calculate the new Row, Byte, Column of pixel
        public void HFlip_GetNewColumnRowByte(ref int CurrentColumn, ref int CurrentRow, ref int CurrentByte)
        {
            int ColumnsInLeftSide = HFlip_GetColumnNumber();

            if (CurrentColumn == ColumnsInLeftSide)
            {
                CurrentColumn = 0;
                CurrentByte += 4 * (ColumnsInLeftSide + (_width % ColumnsInLeftSide));
                CurrentRow++;
            }
        }

        // Calculate the column number of left side of the image
        public int HFlip_GetColumnNumber()
        {
            return _width / 2;
        }
        #endregion

        #region V Flip
        // Main Flip function
        public void VFlip()
        {
            _dstPixels = (byte[])_srcPixels.Clone();

            for (int CurrentByte = 0, CurrentColumn = 0, CurrentRow = 0; CurrentByte < 2 * _height * _width; CurrentColumn++)
            {
                VFlip_SetNewValues(ref CurrentColumn, ref CurrentRow, ref CurrentByte);
            }

            //_srcPixels = (byte[])_dstPixels.Clone();
        }

        // Set the new values for the pixel
        public void VFlip_SetNewValues(ref int CurrentColumn, ref int CurrentRow, ref int CurrentByte)
        {
            VFlip_GetNewColumnRow(ref CurrentColumn, ref CurrentRow);
          
            int index = 4 * (_width * (_height - 1 - CurrentRow) +  CurrentColumn);
            Flip_SwapPixelData(ref CurrentByte, index);
        }

        // Calculate the new Row and Column of pixel
        public void VFlip_GetNewColumnRow(ref int CurrentColumn, ref int CurrentRow)
        {
            if (CurrentColumn == _width)
            {
                CurrentColumn = 0;
                CurrentRow++;
            }
        }
        #endregion

        // Swap pixel data of B G R A
        public void Flip_SwapPixelData(ref int CurrentByte, int index)
        {
            Flip_SwapValues(CurrentByte++, index);
            Flip_SwapValues(CurrentByte++, index + 1);
            Flip_SwapValues(CurrentByte++, index + 2);
            Flip_SwapValues(CurrentByte++, index + 3);
            dstPixels[CurrentByte++] = 50;
        }

        // Swap one of BGRA data of the pixel
        public void Flip_SwapValues(int CurrentByte, int index)
        {
            _dstPixels[CurrentByte] = _srcPixels[index];
            _dstPixels[index] = _srcPixels[CurrentByte];
        } 

        #endregion

        #region Rotate
        // Main function of rotation
        public void Rotate()
        {
            _dstPixels = (byte[])_srcPixels.Clone();

            for (int CurrentByte = 0, CurrentColumn = 0, CurrentRow = 0; CurrentByte < 4 * _height * _width; CurrentColumn++)
            {
                Rotate_GetNewColumnRow(ref CurrentColumn,ref CurrentRow);
                Rotate_SetNewValues(ref CurrentByte, (_width * (_height - (CurrentColumn + 1)) + CurrentRow) * 4);                           
            }

            Rotate_swapWH();
           _srcPixels = (byte[])_dstPixels.Clone();
        }

        // Sets the new values of BGRA
        public void Rotate_SetNewValues(ref int CurrentByte, int index)
        {
            _dstPixels[CurrentByte++] = _srcPixels[index];
            _dstPixels[CurrentByte++] = _srcPixels[index + 1];
            _dstPixels[CurrentByte++] = _srcPixels[index + 2];
            _dstPixels[CurrentByte++] = _srcPixels[index + 3];     
        }

        // Check if the currentcolumn is at the last cell of row
        public void Rotate_GetNewColumnRow(ref int CurrentColumn, ref int CurrentRow)
        {
            if (CurrentColumn == _height)
            {
                CurrentRow++;
                CurrentColumn = 0;
            }
        }

        // Swap width and height
        public void Rotate_swapWH()
        {
            int temp = _width;
            _width = _height;
            _height = _width;
        }
        #endregion

        #region Color
        // Main function which changes BGR colors
        public void ColorChange(double RedColorValue, double GreenColorValue, double BlueColorValue, double RedContrastValue, double GreenContrastValue, double BlueContrastValue)
        {
            _dstPixels = (byte[])_srcPixels.Clone();
            for (int CurrentByte = 0; CurrentByte < 4 * _height * _width; CurrentByte += 4)
            {
                ColorChange_GetNewColors(CurrentByte, RedColorValue, GreenColorValue, BlueColorValue);
                ColorChange_GetNewContrasts(CurrentByte, RedContrastValue, GreenContrastValue, BlueContrastValue);
            }
        }

        #region Colors
        // Gets new values for B G R color of selected pixel of image (depends of the value of R G B color sliders)
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
        // Gets new values for B G R color of selected pixel of image (depends of the value of R G B contrast sliders)
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

        #region Gamma
        // Main function which changes BGR colors
        public void GammaChange(double BlueColorValue, double GreenColorValue, double RedColorValue)
        {
            _dstPixels = (byte[])_srcPixels.Clone();
            byte[] BlueGamma = Gamma_GetArray(BlueColorValue / 10);  // Divide by 10 because the value must be between 0.2 and 5. Get new color list for BlueGamma. 
            byte[] GreenGamma = Gamma_GetArray(GreenColorValue / 10);// Get new color list for GreenGamma  
            byte[] RedGamma = Gamma_GetArray(RedColorValue / 10);    // Get new color list for RedGamma            

            for (int CurrentByte = 0; CurrentByte < 4 * _height * _width; CurrentByte += 4)
            {
                Gamma_SetNewBGRValues(CurrentByte, BlueGamma, GreenGamma, RedGamma); 
            }            
        }

        public void Gamma_SetNewBGRValues(int CurrentByte, byte[] BlueGamma, byte[] GreenGamma, byte[] RedGamma)
        {
            _dstPixels[CurrentByte] = BlueGamma[_dstPixels[CurrentByte]];
            _dstPixels[CurrentByte + 1] = GreenGamma[_dstPixels[CurrentByte + 1]];
            _dstPixels[CurrentByte + 2] = RedGamma[_dstPixels[CurrentByte + 2]];
        }

        // Sets the new array of colors
        private byte[] Gamma_GetArray(double color)
        {
            byte[] gammaArray = new byte[256];

            for (int i = 0; i < 256; i++)
            {
                gammaArray[i] = (byte)Math.Min(255,(int)((255.0 * Math.Pow(i / 255.0, 1.0 / color)) + 0.5));
            }

            return gammaArray;
        }
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