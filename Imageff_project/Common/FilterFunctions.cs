using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;

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
            _dstPixels = (byte[])_srcPixels.Clone();
        }

        #region Frames

        #region Standard Frames

        #region Standard Left Side
        // Frame for left side
        public void Frames_StandardLeftSide(double BlueColorValue, double GreenColorValue, double RedColorValue, double FrameWidth)
        {
            Frames_StandardLeftCheckFrameWidth(ref FrameWidth);

            for (int CurrentByte = 0, CurrentColumn = 1; CurrentByte < _dstPixels.Length; CurrentByte += 4, CurrentColumn++)
            {
                Frames_StandardLeftSideNewPixel(ref CurrentByte, ref CurrentColumn, BlueColorValue, GreenColorValue, RedColorValue, FrameWidth);
            }
        }

        // Calculate where is the pixel and if it is in the frame- it change it or set new currenybyte and currentcolumn
        public void Frames_StandardLeftSideNewPixel(ref int CurrentByte, ref int CurrentColumn, double BlueColorValue, double GreenColorValue, double RedColorValue, double FrameWidth)
        {
            if (CurrentColumn <= FrameWidth)
            {
                Fremes_StandardSetPixelValues(CurrentByte, BlueColorValue, GreenColorValue, RedColorValue);
            }
            else
            {
                CurrentColumn = 0;
                CurrentByte += 4 * (_width - (int)FrameWidth - 1); //go to the next row of pixels, minus 1 because we always increment current byte by 4(1 pixel)
            }
        }

        // Check if the Width of frame is more than the width of the image
        public void Frames_StandardLeftCheckFrameWidth(ref double FrameWidth)
        {
            if (FrameWidth > _width)
                FrameWidth = _width;
        }
        #endregion

        #region Standard Top Side
        // Frame for top side
        public void Frames_StandardTopSide(double BlueColorValue, double GreenColorValue, double RedColorValue, double FrameWidth)
        {
            Frames_StandardTopCheckFrameWidth(ref FrameWidth);

            for (int CurrentByte = 0; CurrentByte < 4 * _width * FrameWidth; CurrentByte += 4)
            {
                Fremes_StandardSetPixelValues(CurrentByte, BlueColorValue, GreenColorValue, RedColorValue);
            }
        }

        // Check if the Width of frame is more than the width of the image
        public void Frames_StandardTopCheckFrameWidth(ref double FrameWidth)
        {
            if (FrameWidth > _height)
                FrameWidth = _height;
        }
        #endregion

        #region Standard Right Side
        // Frame for Right side
        public void Frames_StandardRightSide(double BlueColorValue, double GreenColorValue, double RedColorValue, double FrameWidth)
        {
            int CurrentColumn = Frames_StandardRightSideGetFirstColumn(FrameWidth);

            for (int CurrentByte = 4 * CurrentColumn; CurrentByte < _dstPixels.Length;)
            {
                Frames_StandardRightSideNewPixel(ref CurrentByte, ref CurrentColumn, BlueColorValue, GreenColorValue, RedColorValue, FrameWidth);
            }
        }

        // Calculate where is the pixel and if it is in the frame- it change it or set new currenybyte and currentcolumn
        public void Frames_StandardRightSideNewPixel(ref int CurrentByte, ref int CurrentColumn, double BlueColorValue, double GreenColorValue, double RedColorValue, double FrameWidth)
        {
            if (CurrentColumn == _width)
            {
                CurrentColumn = Frames_StandardRightSideGetFirstColumn(FrameWidth);
                CurrentByte += 4 * (_width - (int)FrameWidth);
            }
            else
            {
                Fremes_StandardSetPixelValues(CurrentByte, BlueColorValue, GreenColorValue, RedColorValue);
                CurrentColumn++;
                CurrentByte += 4;
            }            
        }

        //Calculate the first index of right border
        public int Frames_StandardRightSideGetFirstColumn(double FrameWidth)
        {
            if (FrameWidth > _height)
            {
                return 0;
            }
            else
            {
                return _width - (int)FrameWidth;
            }
        }
        #endregion

        #region Standard Bottom Side
        // Frame for Bottom side
        public void Frames_StandardBottomSide(double BlueColorValue, double GreenColorValue, double RedColorValue, double FrameWidth)
        {
            int CurrentByte = Frames_StandardBottomSideGetFirstIndex(FrameWidth);

            for (; CurrentByte < _dstPixels.Length; CurrentByte += 4)
            {
                Fremes_StandardSetPixelValues(CurrentByte, BlueColorValue, GreenColorValue, RedColorValue);
            }
        }

        //Calculate the first index of bottom border
        public int Frames_StandardBottomSideGetFirstIndex(double FrameWidth)
        {
            if (FrameWidth > _height)
            {
                return 0;
            }
            else
            {
                return 4 * _width * (_height - (int)FrameWidth);
            }
        }
        #endregion

        // Set B G R value of the pixel
        public void Fremes_StandardSetPixelValues(int index, double BlueColorValue, double GreenColorValue, double RedColorValue)
        {   
            _dstPixels[index] = (byte)BlueColorValue;
            _dstPixels[index + 1] = (byte)GreenColorValue;
            _dstPixels[index + 2] = (byte)RedColorValue;
        }
        #endregion

        #region Darkness Frames
        #region Darkness Left Side
        // Frame for left side
        public void Frames_DarknessLeftSide()
        {
            int FrameWidth = Frames_DarknessGetLeftRightFrameWidth();

            for (int CurrentByte = 0, CurrentColumn = 0; CurrentByte < _dstPixels.Length;)
            {
                Frames_DarknessLeftSideNewPixel(ref CurrentByte, ref CurrentColumn, FrameWidth);
            }
        }

        // Calculate where is the pixel and if it is in the frame- it change it or set new currenybyte and currentcolumn
        public void Frames_DarknessLeftSideNewPixel(ref int CurrentByte, ref int CurrentColumn, int FrameWidth)
        {
            if (CurrentColumn == FrameWidth)
            {
                CurrentColumn = 0;
                CurrentByte += 4 * (_width - FrameWidth);               
            }
            else
            {
                Fremes_DarknessSetPixelData(CurrentByte);
                CurrentColumn++;
                CurrentByte += 4;
            }
        }
        #endregion

        #region Darkness Top Side
        // Frame for top side
        public void Frames_DarknessTopSide()
        {
            int FrameWidth = Frames_DarknessGetLeftRightFrameWidth();

            for (int CurrentByte = 0; CurrentByte < 4 * _width * FrameWidth; CurrentByte += 4)
            {
                Fremes_DarknessSetPixelData(CurrentByte);
            }
        }
        #endregion

        #region Darkness Right Side
        // Frame for Right side
        public void Frames_DarknessRightSide()
        {
            int FrameWidth = Frames_DarknessGetLeftRightFrameWidth();
            int CurrentColumn = _width - FrameWidth;

            for (int CurrentByte = 4 * CurrentColumn; CurrentByte < _dstPixels.Length;)
            {
                Frames_DarknessRightSideNewPixel(ref CurrentByte, ref CurrentColumn, FrameWidth);
            }
        }

        // Calculate where is the pixel and if it is in the frame- it change it or set new currenybyte and currentcolumn
        public void Frames_DarknessRightSideNewPixel(ref int CurrentByte, ref int CurrentColumn, int FrameWidth)
        {
            if (CurrentColumn == _width)
            {
                CurrentColumn = _width - FrameWidth;
                CurrentByte += 4 * (_width - FrameWidth);
                
            }
            else
            {
                Fremes_DarknessSetPixelData(CurrentByte);
                CurrentColumn++;
                CurrentByte += 4;
            }
        }
        #endregion

        #region Darkness Bottom Side
        // Frame for Bottom side
        public void Frames_DarknessBottomSide()
        {
            int FrameWidth = Frames_DarknessGetLeftRightFrameWidth();
            int CurrentByte = Frames_DarknessBottomSideGetFirstIndex(FrameWidth);

            for (; CurrentByte < _dstPixels.Length; CurrentByte += 4)
            {
                Fremes_DarknessSetPixelData(CurrentByte);
            }
        }

        //Calculate the first index of bottom border
        public int Frames_DarknessBottomSideGetFirstIndex(int FrameWidth)
        {
            return 4 * _width * (_height - FrameWidth);
        }
        #endregion

        // Calculate the width of left and right frame
        public int Frames_DarknessGetLeftRightFrameWidth()
        {
            return ((_width + _height) / 2 * 5) / 100;
        }

        // Set B G R value of the pixel
        public void Fremes_DarknessSetPixelData(int index)
        {
            _dstPixels[index] = (byte)(_srcPixels[index] * 0.3);
            _dstPixels[index + 1] = (byte)(_srcPixels[index + 1] * 0.3);
            _dstPixels[index + 2] = (byte)(_srcPixels[index + 2] * 0.3);
        }
        #endregion

        #region Smooth Darnkess
        // Main function
        public void Frames_SmoothDarkness()
        {
            int FrameWidth = Frames_SmoothDarknessGetFrameWidth();
            
            Frames_SmoothDarknessLEFTRIGHT(FrameWidth);
            Frames_SmoothDarknessTOPBOTTOM(FrameWidth);                       
        }

        // Smooth Darkness for Left and Right side of the image
        public void Frames_SmoothDarknessLEFTRIGHT(int FrameWidth)
        {
            double darkness = 0.1;            

            for (int CurrentByte = 0, CurrentColumn = 1; darkness < 1.0; CurrentByte += 4, CurrentColumn++)
            {
                Frames_SmoothDarknessLEFTRIGHTSetCurrentColumn(CurrentByte, darkness); // Left side
                Frames_SmoothDarknessLEFTRIGHTSetCurrentColumn(4 * (_width - CurrentColumn), darkness); // Right side
                darkness += 1.0 / FrameWidth;
            }
        }

        // Set all pixels of current column (darkness)
        public void Frames_SmoothDarknessLEFTRIGHTSetCurrentColumn(int StartIndex, double darkness)
        {
            for (int index = StartIndex; index < _dstPixels.Length; index += 4 * _width)
            {
                Frames_SmoothDarknessSetPixelValues(index, darkness);
            }
        }

        // Smooth Darkness for Top and Bottom side of the image
        public void Frames_SmoothDarknessTOPBOTTOM(int FrameWidth)
        {
            double darkness = 0.1;

            for (int CurrentByte = 0, CurrentRow = 1; darkness < 1.0; CurrentByte += 4 * _width, CurrentRow++)
            {
                Frames_SmoothDarknessLEFTRIGHTSetCurrentRow(CurrentByte, darkness); // Top side
                Frames_SmoothDarknessLEFTRIGHTSetCurrentRow(4 * _width * (_height - CurrentRow), darkness); // Bottom side
                darkness += 1.0 / FrameWidth;
            }
        }

        // Set all pixels of current row (darkness)
        public void Frames_SmoothDarknessLEFTRIGHTSetCurrentRow(int StartIndex, double darkness)
        {
            for (int index = StartIndex; index < 4 * _width  + StartIndex; index += 4)
            {
                Frames_SmoothDarknessSetPixelValues(index, darkness);
            }
        }

        // Calculate the width of left and right frame
        public int Frames_SmoothDarknessGetFrameWidth()
        {
            return ((_width + _height) / 2 * 20) / 100;
        }

        // Set B G R value of the pixel
        public void Frames_SmoothDarknessSetPixelValues(int CurrentByte, double darkness)
        {
            if (Frames_CheckForBrighterPixel(CurrentByte, darkness)) 
            {
                _dstPixels[CurrentByte] = (byte)(_srcPixels[CurrentByte] * darkness);
                _dstPixels[CurrentByte + 1] = (byte)(_srcPixels[CurrentByte + 1] * darkness);
                _dstPixels[CurrentByte + 2] = (byte)(_srcPixels[CurrentByte + 2] * darkness);
            }
        }

        // Check if the pixel is brighter
        public bool Frames_CheckForBrighterPixel(int CurrentByte, double darkness)
        {
            return _dstPixels[CurrentByte] > srcPixels[CurrentByte] * darkness || _dstPixels[CurrentByte + 1] > srcPixels[CurrentByte + 1] * darkness || _dstPixels[CurrentByte + 2] > srcPixels[CurrentByte + 2] * darkness;
        }
        #endregion

        #region Darkness Angle
        // Main function
        public void Frames_StandartAngle(double Color)
        {
            int FrameWidth = Frames_GetFrameWidth(5);
            Frames_DarknessAngleSetUpLeftAngle(Color, 4 * FrameWidth + 4 * _width * (FrameWidth * 2), FrameWidth);
            Frames_DarknessAngleSetUpRightAngle(Color, 4 * (_width - 3 * FrameWidth) + 4 * _width * (FrameWidth * 2 - 1), FrameWidth);
            Frames_DarknessAngleSetDownLeftAngle(Color, 4 * _width * (_height - (FrameWidth * 2)) + 4 * FrameWidth, FrameWidth);
            Frames_DarknessAngleSetDownRightAngle(Color, 4 * (_width - 3 * FrameWidth) + 4 * _width * (_height - (FrameWidth * 2 - 1)), FrameWidth);
        }

        // Set the pixels of Up Left angle of the image
        public void Frames_DarknessAngleSetUpLeftAngle(double Color, int Center, int FrameWidth)
        {
            int StartIndex, EndIndex;
            double X, Y, angle = 0;

            for (double degrees = 90.0; degrees < 180.0; degrees += 45.0 / (double)FrameWidth)
            {
                angle = Math.PI * degrees / 180.0;
                X = Math.Round((double)FrameWidth * Math.Cos(angle));
                Y = Math.Round((double)FrameWidth * Math.Sin(angle));
                StartIndex = Center - 4 * _width * (int)Y + 4 * (int)(FrameWidth + X) - 4 * (FrameWidth + (int)X);
                EndIndex = Center - 4 * _width * (int)Y + 4 * (int)(FrameWidth + X);
                Frames_DarknessAngleSetRow(StartIndex, EndIndex, Color);
            }
        }

        // Set the pixels of Up Right angle of the image
        public void Frames_DarknessAngleSetUpRightAngle(double Color, int Center, int FrameWidth)
        {
            int StartIndex, EndIndex;
            double X, Y, angle = 0;

            for (double degrees = 0.0; degrees < 90.0; degrees += 45.0 / (double)FrameWidth)
            {
                angle = Math.PI * degrees / 180.0;
                X = Math.Round((double)FrameWidth * Math.Cos(angle));
                Y = Math.Round((double)FrameWidth * Math.Sin(angle));
                StartIndex = Center - 4 * _width * (int)Y + 4 * (int)(FrameWidth + X);
                EndIndex =   Center - 4 * _width * (int)Y + 4 * (int)(FrameWidth + X) + 4 * (FrameWidth - (int)X);
                Frames_DarknessAngleSetRow(StartIndex, EndIndex, Color);
            }
        }

        // Set the pixels of Down Left angle of the image
        public void Frames_DarknessAngleSetDownLeftAngle(double Color, int Center, int FrameWidth)
        {
            int StartIndex, EndIndex;
            double X, Y, angle = 0;

            for (double degrees = 180.0; degrees < 270.0; degrees += 45.0 / (double)FrameWidth)
            {
                angle = Math.PI * degrees / 180.0;
                X = Math.Round((double)FrameWidth * Math.Cos(angle));
                Y = Math.Round((double)FrameWidth * Math.Sin(angle));
                StartIndex = Center - 4 * _width * (int)Y + 4 * (int)(FrameWidth + X) - 4 * (FrameWidth + (int)X);
                EndIndex = Center - 4 * _width * (int)Y + 4 * (int)(FrameWidth + X);
                Frames_DarknessAngleSetRow(StartIndex, EndIndex, Color);
            }
        }

        // Set the pixels of Down Right angle of the image
        public void Frames_DarknessAngleSetDownRightAngle(double Color, int Center, int FrameWidth)
        {
            int StartIndex, EndIndex;
            double X, Y, angle = 0;

            for (double degrees = 270.0; degrees < 360.0; degrees += 45.0 / (double)FrameWidth)
            {
                angle = Math.PI * degrees / 180.0;
                X = Math.Round((double)FrameWidth * Math.Cos(angle));
                Y = Math.Round((double)FrameWidth * Math.Sin(angle));
                StartIndex = Center - 4 * _width * (int)Y + 4 * (int)(FrameWidth + X);
                EndIndex = Center - 4 * _width * (int)Y + 4 * (int)(FrameWidth + X) + 4 * (FrameWidth - (int)X);
                Frames_DarknessAngleSetRow(StartIndex, EndIndex, Color);
            }
        }

        // Set the pixel values of all pixels between start and end index
        public void Frames_DarknessAngleSetRow(int StartIndex, int EndIndex, double Color)
        {
            for (int index = StartIndex; index < EndIndex && index < _dstPixels.Length; index += 4)
            {
                _dstPixels[index] = (byte)Color;
                _dstPixels[index + 1] = (byte)Color;
                _dstPixels[index + 2] = (byte)Color;
            }     
        }

        // Calculate the width of the frame
        public int Frames_GetFrameWidth(int percent)
        {
            return ((_width + _height) / 2 * percent) / 100;
        }
        #endregion

        #region Angle
        // Main function
        public void Frames_Angle(double Color)
        {
            int FrameWidth = Frames_GetFrameWidth(20);
            Frames_DarknessAngleSetUpLeftAngle(Color, 4 * _width * FrameWidth, FrameWidth);
            Frames_DarknessAngleSetUpRightAngle(Color, 4 * _width * FrameWidth + 4 * (_width - FrameWidth * 2), FrameWidth);
            Frames_DarknessAngleSetDownLeftAngle(Color, 4 * _width * (_height - FrameWidth) , FrameWidth);
            Frames_DarknessAngleSetDownRightAngle(Color, 4 * _width * (_height - FrameWidth) + 4 * (_width - FrameWidth * 2), FrameWidth);
        }

        #endregion
        #endregion

        #region Sharpen1

        // Main function of sharpen filter
        public void Sharpen1()
        {
            _dstPixels = (byte[])_srcPixels.Clone();
            Random random = new Random();
            int SquareWidth = Sharpen1_GetSquareWidth();

            for (int CurrentByte = 3, AlphaCoeff = 0, CurrentColumn = 0; CurrentByte < _dstPixels.Length; CurrentByte += 4, CurrentColumn++)
            {
                if (CurrentColumn == _width)
                {
                    CurrentColumn = 0;
                    CurrentByte += 4 * _width * (SquareWidth - 1);
                }

                if (CurrentColumn % SquareWidth == 0)
                {
                    AlphaCoeff = random.Next(0, 256);
                }

                for (int k = 0, index = CurrentByte; k < SquareWidth && index < _dstPixels.Length; k++, index += 4 * _width)
                {
                    _dstPixels[index] = (byte)AlphaCoeff;
                }
            }
        }

        public int Sharpen1_GetSquareWidth()
        {
            int val = (((_width + _height) / 2) * 1) / 100;
            
            return Math.Max(1, val);
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
                HFlip_SetNewValues(ref CurrentColumn, ref CurrentRow, ref CurrentByte);
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

            int index = 4 * (_width * (_height - 1 - CurrentRow) + CurrentColumn);
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
                Rotate_GetNewColumnRow(ref CurrentColumn, ref CurrentRow);
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
            RedColorValue /= 10;// Divide by 10 because the value must be between 0.2 and 5. 
            byte[] BlueGamma = Gamma_GetArray(BlueColorValue);  // Get new color list for BlueGamma. 
            byte[] GreenGamma = Gamma_GetArray(GreenColorValue);// Get new color list for GreenGamma  
            byte[] RedGamma = Gamma_GetArray(RedColorValue);    // Get new color list for RedGamma            

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
                gammaArray[i] = (byte)Math.Min(255, (int)((255.0 * Math.Pow(i / 255.0, 1.0 / color)) + 0.5));
            }

            return gammaArray;
        }
        #endregion

        #region Colorize

        // Main function
        public void Colorize(string colorToLeave)
        {
            _dstPixels = (byte[])_srcPixels.Clone();

            for (int CurrentByte = 0, average = 0; CurrentByte < _dstPixels.Length; CurrentByte += 4)
            {
                if (colorToLeave == "blue")
                {
                    if (_dstPixels[CurrentByte] < _dstPixels[CurrentByte + 1] || _dstPixels[CurrentByte] < _dstPixels[CurrentByte + 2]
                        && _dstPixels[CurrentByte] < (_dstPixels[CurrentByte + 1] + _dstPixels[CurrentByte + 2])
                        && (_dstPixels[CurrentByte] - _dstPixels[CurrentByte + 2]) > 150 && (_dstPixels[CurrentByte] - _dstPixels[CurrentByte + 1]) > 150)
                    {
                        makePixelGrayscale(CurrentByte, average);
                    }
                }
                else if (colorToLeave == "red")
                {
                    if (_dstPixels[CurrentByte + 2] < _dstPixels[CurrentByte + 1] || _dstPixels[CurrentByte + 2] < _dstPixels[CurrentByte]
                        && _dstPixels[CurrentByte + 2] < (_dstPixels[CurrentByte] + _dstPixels[CurrentByte + 1])
                        && (_dstPixels[CurrentByte + 2] - _dstPixels[CurrentByte + 1]) > 150 && (_dstPixels[CurrentByte + 2] - _dstPixels[CurrentByte]) > 150)
                    {
                        makePixelGrayscale(CurrentByte, average);
                    }
                }
                else if (colorToLeave == "green")
                {
                    if (_dstPixels[CurrentByte + 1] < _dstPixels[CurrentByte] || _dstPixels[CurrentByte + 1] < _dstPixels[CurrentByte + 2]
                        && _dstPixels[CurrentByte + 1] < (_dstPixels[CurrentByte] + _dstPixels[CurrentByte + 2])
                        && (_dstPixels[CurrentByte + 1] - _dstPixels[CurrentByte]) > 150 && (_dstPixels[CurrentByte + 1] - _dstPixels[CurrentByte + 2]) > 150)
                    {
                        makePixelGrayscale(CurrentByte, average);
                    }
                }
            }
        }

        private void makePixelGrayscale(int CurrentByte, int average)
        {
            average = (_dstPixels[CurrentByte] + _dstPixels[CurrentByte + 1] + _dstPixels[CurrentByte + 2]) / 3;
            _dstPixels[CurrentByte] = (byte)average;
            _dstPixels[CurrentByte + 1] = (byte)average;
            _dstPixels[CurrentByte + 2] = (byte)average;
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

        #region Histogram
        public void MakeHistogramEqualization()
        {
            int[] frequency = new int[256];

            for (int CurrentByte = 0; CurrentByte < _dstPixels.Length; CurrentByte += 4)
            {
                int i = _dstPixels[CurrentByte];
                frequency[i] += 1;
            }
            int[] cumulative = new int[256];
            cumulative[0] = frequency[0];
            for (int i = 1; i < 256; i++)
            {
                cumulative[i] = cumulative[i - 1] + frequency[i];
            }

            float[] cdf = new float[256];
            for (int i = 0; i < 256; i++)
            {
                cdf[i] = (float)cumulative[i] / (_width * _height);
                cdf[i] = cdf[i] * 255;
            }
            for (int CurrentByte = 0; CurrentByte < _dstPixels.Length; CurrentByte += 4)
            {
                int temp = (int)_dstPixels[CurrentByte];
                _dstPixels[CurrentByte] = _dstPixels[CurrentByte + 1] = _dstPixels[CurrentByte + 2] = (byte)(cdf[temp]);
            }

        }
        #endregion
    }
}