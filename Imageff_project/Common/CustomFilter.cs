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
        byte[] _dstPixels, _srcPixels;
        private int _width, _height, _left, _top, _right, _bottom, _offset, _scale;
        int[,] _coeff;

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
        
        public byte[] Filter()
        {
            CustomFilter_GetBorders();
            CustomFilter_NewPixelsValue((_width * 4 + 4) * _top, 1 + _left, 1 + _top);
            CustomFilter_FillTheBlankPixels();
            return (byte[])_dstPixels.Clone();
        }

        public void CustomFilter_FillTheBlankPixels()
        {
            CustomFilter_FillLeftColumn(4); // Second Column
            CustomFilter_FillLeftColumn(0); // First Column
            CustomFilter_FillTopRow(4 * (_width)); // Second Row
            CustomFilter_FillTopRow(0); // First Row
            CustomFilter_FillRightColumn(4 * (_width - 2)); // Last - 1 Column
            CustomFilter_FillRightColumn(4 * (_width - 1)); // Last Column
            CustomFilter_FillBottomRow(4 * _width * (_height - 2)); // Last - 1 Row
            CustomFilter_FillBottomRow(4 * _width * (_height - 1)); // Last Row
        }

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

        private void CustomFilter_NewPixelsValue(int current_byte, int current_width, int current_height)
        {
            while (current_byte < _srcPixels.Length && current_height <= _height - _bottom)
            {
                if (current_width <= _width - _right)
                {
                    CustomFilter_NewBGR(ref current_byte);
                    current_width++;
                }
                else
                {
                    current_width = 1 + _left;
                    current_height++;
                    current_byte += 4 * _left + 4 * _right;
                }
            }
        }

        private void CustomFilter_NewBGR(ref int current_byte)
        {
            CustomFilter_CalcPixelValue(current_byte++);
            CustomFilter_CalcPixelValue(current_byte++);
            CustomFilter_CalcPixelValue(current_byte++);
            current_byte++; //For 4th value (A)
        }

        private void CustomFilter_CalcPixelValue(int current_byte)
        {
            int val = CustomFilter_GetNewVal(current_byte - (2 * 4 * _width + 2 * 4));      
            CustomFilter_CheckVal(ref val);
            _dstPixels[current_byte] = (byte)val;
        }

        private int CustomFilter_GetNewVal(int current_byte)
        {
            int val = 0;

            for (int i = 2 - _left; i < 3 + _right; i++)
                for (int j = 2 - _top; j < 3 + _bottom; j++)
                    if (_coeff[i,j] != 0)
                        val += _srcPixels[Math.Abs(current_byte + i * 4 + j * _width * 4 )] * _coeff[i, j];// Abs, because it can be negative...

            val /= _scale;
            val += _offset;

            return val;
        }

        private void CustomFilter_CheckVal(ref int val)
        {
            if (val > 255)
                val = 255;
            else if (val < 0)
                val = 0;
        }

        private void CustomFilter_GetBorders()
        {
            _left = CustomFilter_GetLeftBorder();
            _top = CustomFilter_GetTopBorder();
            _right = CustomFilter_GetRightBorder();
            _bottom = CustomFilter_GetBottomBorder();
        }

        private int CustomFilter_GetLeftBorder()
        {
            if (CustomFilter_CoefInFirstColumn())
                return 2;
            else if (CustomFilter_CoefInSecondColumn())
                return 1;
            else
                return 0;
        }
        private bool CustomFilter_CoefInFirstColumn()
        {
            return _coeff[0, 0] != 0 || _coeff[0, 1] != 0 || _coeff[0, 2] != 0 || _coeff[0, 3] != 0 || _coeff[0, 4] != 0;
        }
        private bool CustomFilter_CoefInSecondColumn()
        {
            return _coeff[1, 0] != 0 || _coeff[1, 1] != 0 || _coeff[1, 2] != 0 || _coeff[1, 3] != 0 || _coeff[1, 4] != 0;
        }

        private int CustomFilter_GetTopBorder()
        {
            if (CustomFilter_CoefInFirstRow())
                return 2;
            else if (CustomFilter_CoefInSecondRow())
                return 1;
            else
                return 0;
        }
        private bool CustomFilter_CoefInFirstRow()
        {
            return _coeff[0, 0] != 0 || _coeff[1, 0] != 0 || _coeff[2, 0] != 0 || _coeff[3, 0] != 0 || _coeff[4, 0] != 0;
        }
        private bool CustomFilter_CoefInSecondRow()
        {
            return _coeff[0, 1] != 0 || _coeff[1, 1] != 0 || _coeff[2, 1] != 0 || _coeff[3, 1] != 0 || _coeff[4, 1] != 0;
        }

        private int CustomFilter_GetRightBorder()
        {
            if (CustomFilter_CoefInFifthColumn())
                return 2;
            else if (CustomFilter_CoefInFourthColumn())
                return 1;
            else
                return 0;
        }
        private bool CustomFilter_CoefInFifthColumn()
        {
            return _coeff[4, 0] != 0 || _coeff[4, 1] != 0 || _coeff[4, 2] != 0 || _coeff[4, 3] != 0 || _coeff[4, 4] != 0;       
        }
        private bool CustomFilter_CoefInFourthColumn()
        {
            return _coeff[3, 0] != 0 || _coeff[3, 1] != 0 || _coeff[3, 2] != 0 || _coeff[3, 3] != 0 || _coeff[3, 4] != 0;
        }

        private int CustomFilter_GetBottomBorder()
        {
            if (CustomFilter_CoefInFifthRow())
                return 2;
            else if (CustomFilter_CoefInForthRow())
                return 1;
            else
                return 0;
        }
        private bool CustomFilter_CoefInFifthRow()
        {
            return _coeff[0, 4] != 0 || _coeff[1, 4] != 0 || _coeff[2, 4] != 0 || _coeff[3, 4] != 0 || _coeff[4, 4] != 0;
        }
        private bool CustomFilter_CoefInForthRow()
        {
            return _coeff[0, 3] != 0 || _coeff[1, 3] != 0 || _coeff[2, 3] != 0 || _coeff[3, 3] != 0 || _coeff[4, 3] != 0;
        }
    }
}