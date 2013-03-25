using RemedyPic.RemedyClasses;
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

namespace RemedyPic.UserControls
{
    public sealed partial class DisplayImage : UserControl
    {
        // Those are variables used with the manipulations of the Image
        public TransformGroup _transformGroup;
        public MatrixTransform _previousTransform;
        public CompositeTransform _compositeTransform;
        public bool forceManipulationsToEnd;

        // SelectedRegion variable, used by the crop function.
        public SelectedRegion selectedRegion;

        // The original Width and Height of the image in pixels.
        public uint sourceImagePixelWidth;
        public uint sourceImagePixelHeight;

        // The size of the corners of the crop rectangle.
        double cornerSize;
        double CornerSize
        {
            get
            {
                if (cornerSize <= 0)
                {
                    cornerSize = (double)Application.Current.Resources["Size"];
                }

                return cornerSize;
            }
        }

        // The dictionary holds the history of all previous pointer locations. It is used by the crop function.
        Dictionary<uint, Point?> pointerPositionHistory = new Dictionary<uint, Point?>();


        public DisplayImage()
        {
            this.InitializeComponent();
            AnimateOutPicture.Begin();
            forceManipulationsToEnd = false;
            InitManipulationTransforms();
            selectRegion.ManipulationMode = ManipulationModes.Scale |
                ManipulationModes.TranslateX | ManipulationModes.TranslateY;

            selectedRegion = new SelectedRegion { MinSelectRegionSize = 2 * CornerSize };
            this.DataContext = selectedRegion;
        }

        public void OnImagePointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // Event for the mouse wheel. 
            // It zooms in or out the image
            var delta = e.GetCurrentPoint(displayImage).Properties.MouseWheelDelta;

            if (delta > 0)
            {
                if (scale.ScaleX < 2.5)
                {
                    scale.CenterX = e.GetCurrentPoint(displayImage).Position.X;
                    scale.CenterY = e.GetCurrentPoint(displayImage).Position.Y;
                }
                scale.ScaleX = scale.ScaleX + 0.1;
                scale.ScaleY = scale.ScaleY + 0.1;
                //if (ZoomOut.IsEnabled == false)
                //    ZoomOut.IsEnabled = true;
            }
            else
            {
                if (scale.ScaleX > 0.9 && scale.ScaleY > 0.9)
                {
                    scale.ScaleX = scale.ScaleX - 0.1;
                    scale.ScaleY = scale.ScaleY - 0.1;
                }
                //if (ZoomOut.IsEnabled == true && scale.ScaleX <= 0.9)
                //{
                //    ZoomOut.IsEnabled = false;
                //}
            }
        }
        #region Manipulation Events

        public void ImagePointerReleased(object sender, PointerRoutedEventArgs e)
        {
            forceManipulationsToEnd = true;
        }

        public void InitManipulationTransforms()
        {
            _transformGroup = new TransformGroup();
            _compositeTransform = new CompositeTransform();
            _previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };

            _transformGroup.Children.Add(_previousTransform);
            _transformGroup.Children.Add(_compositeTransform);

            displayImage.RenderTransform = _transformGroup;
        }

        void ManipulateMe_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            forceManipulationsToEnd = false;
            e.Handled = true;
        }

        void ManipulateMe_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        void ManipulateMe_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.Handled = true;
        }


        void ManipulateMe_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (forceManipulationsToEnd)
            {
                e.Complete();
                return;
            }

            _previousTransform.Matrix = _transformGroup.Value;

            Point center = _previousTransform.TransformPoint(new Point(e.Position.X, e.Position.Y));
            _compositeTransform.CenterX = center.X;
            _compositeTransform.CenterY = center.Y;

            _compositeTransform.Rotation = e.Delta.Rotation;
            _compositeTransform.ScaleX = _compositeTransform.ScaleY = e.Delta.Scale;
            _compositeTransform.TranslateX = e.Delta.Translation.X / scale.ScaleX;
            _compositeTransform.TranslateY = e.Delta.Translation.Y / scale.ScaleY;

            e.Handled = true;
        }

        void ManipulateMe_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
        }
        #endregion




    }
}
