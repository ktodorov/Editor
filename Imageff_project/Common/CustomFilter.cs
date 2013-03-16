using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace RemedyPic.Common
{
    class CustomFilter
    {
        private byte[] _dstPixels, _srcPixels;
        private int _width, _height, _left, _top, _right, _bottom, _offset, _scale;
        private int[,] _coeff;

        // Constructor, sets the parameters
        public CustomFilter(byte[] Pixels, int width, int height, int offset, int scale, int[,] coeff)
        {
            _dstPixels = (byte[])Pixels.Clone();
            _srcPixels = (byte[])Pixels.Clone();
            _width = width;
            _height = height;
            _offset = offset;
            _scale = scale;
            _coeff = coeff;
        }
        
        // Main function- Get the borders of the image(depends on the matrix`s values), set the new values of the pixles and fill the black pixels
        // return the new array of the image
        public byte[] Filter()
        {
            
            CustomFilter_GetBorders();
            CustomFilter_NewPixelsValue(8 * (_width + 1));// Set the current byte to second row, second column pixel
            CustomFilter_FillTheBlankPixels();
            return (byte[])_dstPixels.Clone();
        }

        // Recalculate the new values of all pixels of the image
        private void CustomFilter_NewPixelsValue(int current_byte, int current_width = 3, int current_height = 3)
        {
            while (current_byte < _srcPixels.Length && current_height <= _height - 2)
            {
                if (current_width <= _width - 2)
                {
                    CustomFilter_CalcPixelValues(current_byte);
                    current_byte += 4;
                    current_width++;
                }
                else
                {
                    current_width = 3;  // avoid first two columns
                    current_height++;
                    current_byte += 16; // avoid last two colums and first two colums
                }
            }
        }

        // Calculate the pixel values
        private void CustomFilter_CalcPixelValues(int current_byte)
        {
            int[] BGRValues = {0,0,0};
            CustomFilter_GetNewValues(ref BGRValues, current_byte - 8 * (_width + 1));
            CustomFilter_CheckNewValues(ref BGRValues);
            CustomFilter_SetNewValues(current_byte, BGRValues);
        }

        // Set the new values of pixel colors
        private void CustomFilter_SetNewValues(int current_byte, int[] BGRValues)
        {
            _dstPixels[current_byte++] = (byte)BGRValues[0];
            _dstPixels[current_byte++] = (byte)BGRValues[1];
            _dstPixels[current_byte++] = (byte)BGRValues[2];
        }

        // Calculate the B G R values
        private void CustomFilter_GetNewValues(ref int[] BGRValues, int current_byte)
        {
            CustomFilter_MultiplyByMatrixValues(ref BGRValues, current_byte);
            CustomFilter_DiviteByScaleAddOffset(ref BGRValues, current_byte);            
        }

        // Calculate the B G R values. Depends on the matrix of the filter
        private void CustomFilter_MultiplyByMatrixValues(ref int[] BGRValues, int current_byte)
        {
            for (int i = 2 - _left; i < 3 + _right; i++)
                for (int j = 2 - _top; j < 3 + _bottom; j++)
                    if (_coeff[i, j] != 0)
                    {
                        BGRValues[0] += _srcPixels[current_byte + i * 4 + j * _width * 4] * _coeff[i, j];
                        BGRValues[1] += _srcPixels[current_byte + i * 4 + j * _width * 4 + 1] * _coeff[i, j];
                        BGRValues[2] += _srcPixels[current_byte + i * 4 + j * _width * 4 + 2] * _coeff[i, j];
                    }        
        }

        // Divide the values by scale and add offset
        private void CustomFilter_DiviteByScaleAddOffset(ref int[] BGRValues, int current_byte)
        {
            for (int i = 0; i < 3; i++)
            {
                BGRValues[i] /= _scale;
                BGRValues[i] += _offset;
            }
        }

        // Check if the values of B G R color are between [0;255]
        private void CustomFilter_CheckNewValues(ref int[] BGRValues)
        {
            CustomFilter_CheckNewVal(ref BGRValues[0]); // Blue value
            CustomFilter_CheckNewVal(ref BGRValues[1]); // Green value
            CustomFilter_CheckNewVal(ref BGRValues[2]); // Red value
        }

        // Check if the given values is between 0 and 255 and set it if it isn`t
        private void CustomFilter_CheckNewVal(ref int val)
        {
            if (val > 255)
                val = 255;
            else if (val < 0)
                val = 0;
        }

        // Get the borders of the matrix
        private void CustomFilter_GetBorders()
        {
            _left = CustomFilter_GetLeftBorder();
            _top = CustomFilter_GetTopBorder();
            _right = CustomFilter_GetRightBorder();
            _bottom = CustomFilter_GetBottomBorder();
        }

        // Left border
        private int CustomFilter_GetLeftBorder()
        {
            if (CustomFilter_CoefInFirstColumn())
                return 2;
            else if (CustomFilter_CoefInSecondColumn())
                return 1;
            else
                return 0;
        }
        // Check of there is coeff in first column
        private bool CustomFilter_CoefInFirstColumn()
        {
            return _coeff[0, 0] != 0 || _coeff[0, 1] != 0 || _coeff[0, 2] != 0 || _coeff[0, 3] != 0 || _coeff[0, 4] != 0;
        }
        // Check of there is coeff in second column
        private bool CustomFilter_CoefInSecondColumn()
        {
            return _coeff[1, 0] != 0 || _coeff[1, 1] != 0 || _coeff[1, 2] != 0 || _coeff[1, 3] != 0 || _coeff[1, 4] != 0;
        }

        // Top border
        private int CustomFilter_GetTopBorder()
        {
            if (CustomFilter_CoefInFirstRow())
                return 2;
            else if (CustomFilter_CoefInSecondRow())
                return 1;
            else
                return 0;
        }
        // Check of there is coeff in first row
        private bool CustomFilter_CoefInFirstRow()
        {
            return _coeff[0, 0] != 0 || _coeff[1, 0] != 0 || _coeff[2, 0] != 0 || _coeff[3, 0] != 0 || _coeff[4, 0] != 0;
        }
        // Check of there is coeff in second row
        private bool CustomFilter_CoefInSecondRow()
        {
            return _coeff[0, 1] != 0 || _coeff[1, 1] != 0 || _coeff[2, 1] != 0 || _coeff[3, 1] != 0 || _coeff[4, 1] != 0;
        }

        // Right Border
        private int CustomFilter_GetRightBorder()
        {
            if (CustomFilter_CoefInFifthColumn())
                return 2;
            else if (CustomFilter_CoefInFourthColumn())
                return 1;
            else
                return 0;
        }
        // Check of there is coeff in Fifth Column
        private bool CustomFilter_CoefInFifthColumn()
        {
            return _coeff[4, 0] != 0 || _coeff[4, 1] != 0 || _coeff[4, 2] != 0 || _coeff[4, 3] != 0 || _coeff[4, 4] != 0;       
        }
        // Check of there is coeff in fourth Column
        private bool CustomFilter_CoefInFourthColumn()
        {
            return _coeff[3, 0] != 0 || _coeff[3, 1] != 0 || _coeff[3, 2] != 0 || _coeff[3, 3] != 0 || _coeff[3, 4] != 0;
        }

        // Bottom border
        private int CustomFilter_GetBottomBorder()
        {
            if (CustomFilter_CoefInFifthRow())
                return 2;
            else if (CustomFilter_CoefInForthRow())
                return 1;
            else
                return 0;
        }
        // Check of there is coeff in Fifth row
        private bool CustomFilter_CoefInFifthRow()
        {
            return _coeff[0, 4] != 0 || _coeff[1, 4] != 0 || _coeff[2, 4] != 0 || _coeff[3, 4] != 0 || _coeff[4, 4] != 0;
        }
        // Check of there is coeff in Fourth row
        private bool CustomFilter_CoefInForthRow()
        {
            return _coeff[0, 3] != 0 || _coeff[1, 3] != 0 || _coeff[2, 3] != 0 || _coeff[3, 3] != 0 || _coeff[4, 3] != 0;
        }

        // Fill the black pixels (2x2 wide border)
        private void CustomFilter_FillTheBlankPixels()
        {
            CustomFilter_FillLeftColumn(4);                         // Second Column
            CustomFilter_FillLeftColumn(0);                         // First Column
            CustomFilter_FillTopRow(4 * (_width));                  // Second Row
            CustomFilter_FillTopRow(0);                             // First Row
            CustomFilter_FillRightColumn(4 * (_width - 2));         // Last - 1 Column
            CustomFilter_FillRightColumn(4 * (_width - 1));         // Last Column
            CustomFilter_FillBottomRow(4 * _width * (_height - 2)); // Last - 1 Row
            CustomFilter_FillBottomRow(4 * _width * (_height - 1)); // Last Row
        }

        // Fill the pixels of one row - left side 
        private void CustomFilter_FillLeftColumn(int StartIndex)
        {
            for (int CurrentByte = StartIndex; CurrentByte < _dstPixels.Length; CurrentByte += 4 * _width)
            {
                _dstPixels[CurrentByte] = _dstPixels[CurrentByte + 4];
                _dstPixels[CurrentByte + 1] = _dstPixels[CurrentByte + 5];
                _dstPixels[CurrentByte + 2] = _dstPixels[CurrentByte + 6];
                _dstPixels[CurrentByte + 3] = _dstPixels[CurrentByte + 7];
            }
        }

        // Fill the pixels of one row - top side 
        private void CustomFilter_FillTopRow(int StartIndex)
        {
            for (int CurrentByte = StartIndex; CurrentByte < StartIndex + 4 * _width; CurrentByte += 4)
            {
                _dstPixels[CurrentByte] = _dstPixels[CurrentByte + 4 * _width];
                _dstPixels[CurrentByte + 1] = _dstPixels[CurrentByte + 1 + 4 * _width];
                _dstPixels[CurrentByte + 2] = _dstPixels[CurrentByte + 2 + 4 * _width];
                _dstPixels[CurrentByte + 3] = _dstPixels[CurrentByte + 3 + 4 * _width];
            }
        }

        // Fill the pixels of one row - right side 
        private void CustomFilter_FillRightColumn(int StartIndex)
        {
            for (int CurrentByte = StartIndex; CurrentByte < _dstPixels.Length; CurrentByte += 4 * _width)
            {
                _dstPixels[CurrentByte] = _dstPixels[CurrentByte - 4];
                _dstPixels[CurrentByte + 1] = _dstPixels[CurrentByte - 3];
                _dstPixels[CurrentByte + 2] = _dstPixels[CurrentByte - 2];
                _dstPixels[CurrentByte + 3] = _dstPixels[CurrentByte - 1];
            }
        }

        // Fill the pixels of one row - bottom side 
        private void CustomFilter_FillBottomRow(int StartIndex)
        {
            for (int CurrentByte = StartIndex; CurrentByte < StartIndex + 4 * _width; CurrentByte += 4)
            {
                _dstPixels[CurrentByte] = _dstPixels[CurrentByte - 4 * _width];
                _dstPixels[CurrentByte + 1] = _dstPixels[CurrentByte + 1 - 4 * _width];
                _dstPixels[CurrentByte + 2] = _dstPixels[CurrentByte + 2 - 4 * _width];
                _dstPixels[CurrentByte + 3] = _dstPixels[CurrentByte + 3 - 4 * _width];
            }
        }
    }
}