using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Storage.Provider;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices.WindowsRuntime;

namespace RemedyPic.UserControls
{
    public sealed partial class MainOptionsPanel : UserControl
    {
        MainPage pageRoot = MainPage.Current;

        public MainOptionsPanel()
        {
            this.InitializeComponent();
        }

        #region Get Photo
        // This occures when GetPhoto button is clicked
        public void GetPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (pageRoot.Saved)
            {
                GetPhoto();
            }
            else
            {
                pageRoot.PopupCalledBy = "Browse";
                pageRoot.DarkenBorder.Visibility = Visibility.Visible;
                pageRoot.notSaved.IsOpen = true;
            }
        }

        public async void GetPhoto(StorageFile fileToUse = null)
        {
            // File picker APIs don't work if the app is in a snapped state.
            // If the app is snapped, try to unsnap it first. Only show the picker if it unsnaps.
            if (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped ||
                 Windows.UI.ViewManagement.ApplicationView.TryUnsnap() == true)
            {
                if (fileToUse == null)
                {
                    // First, create a new file picker.
                    FileOpenPicker filePicker = new FileOpenPicker();
                    // Make the file picker view mode to Thumbnails.
                    filePicker.ViewMode = PickerViewMode.Thumbnail;
                    // Add several file extensions to be available for opening.
                    filePicker.FileTypeFilter.Add(".jpg");
                    filePicker.FileTypeFilter.Add(".png");
                    filePicker.FileTypeFilter.Add(".bmp");
                    filePicker.FileTypeFilter.Add(".jpeg");

                    // Get the selected file and save it to the StorageFile variable.
                    pageRoot.file = await filePicker.PickSingleFileAsync();
                    // We create the new WriteableBitmap.
                }
                else
                {
                    pageRoot.file = fileToUse;
                }
                if (pageRoot.file != null)
                // File is null if user cancels the file picker.
                {
                    pageRoot.bitmapImage = new WriteableBitmap(1, 1);
                    pageRoot.ImageLoadingRing.IsActive = true;
                    pageRoot.Saved = true;
                    pageRoot.imageDisplayed.AnimateOutPicture.Begin();

                    // We create a temporary stream for the opened file.
                    // Then we decode the stream to a BitmapDecoder
                    // so we can set the image width and height to the variables.
                    // Then we set the Stream to the WriteableBitmap
                    // and set the pictureIsLoaded variable to true.
                    Windows.Storage.Streams.IRandomAccessStream fileStream =
                            await pageRoot.file.OpenAsync(Windows.Storage.FileAccessMode.Read);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);

                    pageRoot.imageDisplayed.sourceImagePixelHeight = decoder.PixelHeight;
                    pageRoot.imageDisplayed.sourceImagePixelWidth = decoder.PixelWidth;


                    pageRoot.bitmapImage.SetSource(fileStream);
                    RandomAccessStreamReference streamRef = RandomAccessStreamReference.CreateFromFile(pageRoot.file);

                    // If the interface was changed from previous image, it should be resetted.
                    ResetZoomPos();

                    pageRoot.pictureIsLoaded = true;
                    pageRoot.doAllCalculations();
                    pageRoot.ImageLoadingRing.IsActive = false;

                }
            }
            else
            {
                // If the window can't be unsnapped, show alert.
                await new MessageDialog("Can't open in snapped state. Please unsnap the app and try again", "Close").ShowAsync();
            }
        }

        public void OnCameraButtonClick(object sender, RoutedEventArgs e)
        {
            if (pageRoot.Saved)
            {
                getCameraPhoto();
            }
            else
            {
                pageRoot.PopupCalledBy = "Camera";
                pageRoot.DarkenBorder.Visibility = Visibility.Visible;
                pageRoot.notSaved.IsOpen = true;
            }
        }

        public async void getCameraPhoto()
        {
            CameraCaptureUI camera = new CameraCaptureUI();
            camera.PhotoSettings.CroppedAspectRatio = new Size(16, 10);
            StorageFile photoFile = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (photoFile != null)
                GetPhoto(photoFile);
        }

        #endregion


        #region Zoom
        public void ZoomInClicked(object sender, RoutedEventArgs e)
        {
            pageRoot.imageDisplayed.scale.ScaleX = pageRoot.imageDisplayed.scale.ScaleX + 0.1;
            pageRoot.imageDisplayed.scale.ScaleY = pageRoot.imageDisplayed.scale.ScaleY + 0.1;
            ZoomOut.IsEnabled = true;
        }

        public void ZoomOutClicked(object sender, RoutedEventArgs e)
        {
            if (pageRoot.imageDisplayed.scale.ScaleX > 0.9 && pageRoot.imageDisplayed.scale.ScaleY > 0.9)
            {
                pageRoot.imageDisplayed.scale.ScaleX = pageRoot.imageDisplayed.scale.ScaleX - 0.1;
                pageRoot.imageDisplayed.scale.ScaleY = pageRoot.imageDisplayed.scale.ScaleY - 0.1;
            }
            if (pageRoot.imageDisplayed.scale.ScaleX <= 0.9 && pageRoot.imageDisplayed.scale.ScaleY <= 0.9)
            {
                ZoomOut.IsEnabled = false;
            }
        }

        public void OnResetZoomClick(object sender, RoutedEventArgs e)
        {
            ResetZoomPos();
        }

        public void ResetZoomPos()
        {
            pageRoot.imageDisplayed.displayImage.Margin = new Thickness(0, 0, 0, 0);
            pageRoot.imageDisplayed.displayImage.RenderTransform = null;
            pageRoot.imageDisplayed.InitManipulationTransforms();
            pageRoot.imageDisplayed.scale.ScaleX = 1;
            pageRoot.imageDisplayed.scale.ScaleY = 1;
        }
        public void MoveChecked(object sender, RoutedEventArgs e)
        {
            pageRoot.imageDisplayed.displayImage.ManipulationMode = ManipulationModes.All;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Panning-checked.png");
            PanIcon.Source = temp;
        }

        public void MoveUnchecked(object sender, RoutedEventArgs e)
        {
            pageRoot.imageDisplayed.displayImage.ManipulationMode = ManipulationModes.None;
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Panning.png");
            PanIcon.Source = temp;
        }
        #endregion



        public void saveImageButton_Click(object sender, RoutedEventArgs e)
        {
            // When the user clicks Apply, the image is cropped.
            pageRoot.UpdatePreviewImage();
        }

        #region Undo and Redo

        // Undo button click
        public void OnUndoClick(object sender, RoutedEventArgs e)
        {
            pageRoot.ImageLoadingRing.IsActive = true;

            if (pageRoot.archive_current_index > 0) // Check if there is no more for undo
            {
                pageRoot.archive_current_index--;
				if (pageRoot.OptionsPopup.effectsApplied.Count > 0 && (Regex.IsMatch(pageRoot.OptionsPopup.effectsApplied[pageRoot.archive_current_index], "Crop") || Regex.IsMatch(pageRoot.OptionsPopup.effectsApplied[pageRoot.archive_current_index], "Resize")))
                {
					string[] sizes = pageRoot.OptionsPopup.effectsApplied[pageRoot.archive_current_index].Split(' ');
                    pageRoot.imageOriginal.width = Convert.ToInt32(sizes[1]);
                    pageRoot.imageOriginal.height = Convert.ToInt32(sizes[2]);
                    pageRoot.imageOriginal.srcPixels = (byte[])pageRoot.archive_data[pageRoot.archive_current_index].Clone();
                    pageRoot.imageOriginal.dstPixels = (byte[])pageRoot.archive_data[pageRoot.archive_current_index].Clone();
                    pageRoot.bitmapImage = new WriteableBitmap(pageRoot.imageOriginal.width, pageRoot.imageOriginal.height);
                    pageRoot.bitmapStream = pageRoot.bitmapImage.PixelBuffer.AsStream();
                }
                ArchiveSetNewImage();
                pageRoot.setExampleBitmaps();
            }
            pageRoot.ImageLoadingRing.IsActive = false;
        }

        //Redo button click
        public void OnRedoClick(object sender, RoutedEventArgs e)
        {
            pageRoot.ImageLoadingRing.IsActive = true;

            if (pageRoot.archive_current_index < pageRoot.archive_data.Count - 1) // Check if there is array for redo
            {
                pageRoot.archive_current_index++;
				if (Regex.IsMatch(pageRoot.OptionsPopup.effectsApplied[pageRoot.archive_current_index - 1], "Crop") || Regex.IsMatch(pageRoot.OptionsPopup.effectsApplied[pageRoot.archive_current_index - 1], "Resize"))
                {
					string[] sizes = pageRoot.OptionsPopup.effectsApplied[pageRoot.archive_current_index - 1].Split(' ');
                    pageRoot.imageOriginal.width = Convert.ToInt32(sizes[3]);
                    pageRoot.imageOriginal.height = Convert.ToInt32(sizes[4]);
                    pageRoot.imageOriginal.srcPixels = (byte[])pageRoot.archive_data[pageRoot.archive_current_index].Clone();
                    pageRoot.imageOriginal.dstPixels = (byte[])pageRoot.archive_data[pageRoot.archive_current_index].Clone();
                    pageRoot.bitmapImage = new WriteableBitmap(pageRoot.imageOriginal.width, pageRoot.imageOriginal.height);
                    pageRoot.bitmapStream = pageRoot.bitmapImage.PixelBuffer.AsStream();
                }
                ArchiveSetNewImage();
            }
            pageRoot.ImageLoadingRing.IsActive = false;
        }

        public void ArchiveSetNewImage()
        {
            pageRoot.prepareImage(pageRoot.bitmapStream, pageRoot.bitmapImage, pageRoot.imageOriginal);
            pageRoot.imageOriginal.srcPixels = (byte[])pageRoot.archive_data[pageRoot.archive_current_index].Clone();
            pageRoot.imageOriginal.dstPixels = (byte[])pageRoot.archive_data[pageRoot.archive_current_index].Clone();
            pageRoot.setStream(pageRoot.bitmapStream, pageRoot.bitmapImage, pageRoot.imageOriginal);
            pageRoot.setExampleBitmaps();
            pageRoot.FiltersPopup.setFilterBitmaps();
            pageRoot.imageDisplayed.displayImage.Source = pageRoot.bitmapImage;
        }

        // Add pixel array to the archive and increment current index of the archive
        public void ArchiveAddArray()
        {
            if (pageRoot.archive_current_index != -1 && pageRoot.archive_current_index != pageRoot.archive_data.Count - 1)
            {
                pageRoot.archive_data.RemoveRange(pageRoot.archive_current_index + 1, pageRoot.archive_data.Count - 1 - pageRoot.archive_current_index);
				pageRoot.OptionsPopup.effectsApplied.RemoveRange(pageRoot.archive_current_index, pageRoot.OptionsPopup.effectsApplied.Count - pageRoot.archive_current_index); // Here we don`t save the start image, so we have -1 index of pageRoot.archive_current_index
            }
            pageRoot.archive_data.Add((byte[])pageRoot.imageOriginal.srcPixels.Clone());
            pageRoot.archive_current_index++;
        }
        #endregion

        public void OnCancelSaveClicked(object sender, RoutedEventArgs e)
        {
            pageRoot.notSaved.IsOpen = false;
            pageRoot.DarkenBorder.Visibility = Visibility.Collapsed;
        }

        public async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            OnCancelSaveClicked(sender, e);
            if (e.OriginalSource.Equals(pageRoot.YesSave))
            {
                await pageRoot.SaveFile(true);
            }
            if (pageRoot.PopupCalledBy == "Browse")
            {
                GetPhoto();
            }
            else if (pageRoot.PopupCalledBy == "Camera")
            {
                getCameraPhoto();
            }
        }


        public void OnCameraPointerOver(object sender, PointerRoutedEventArgs e)
        {
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Camera-hover.png");
            CameraIcon.Source = temp;
        }

        public void OnCameraPointerExited(object sender, PointerRoutedEventArgs e)
        {
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Camera.png");
            CameraIcon.Source = temp;
        }

        public void OnBrowsePointerOver(object sender, PointerRoutedEventArgs e)
        {
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Browse-hover.png");
            BrowseIcon.Source = temp;
        }

        public void OnBrowsePointerExited(object sender, PointerRoutedEventArgs e)
        {
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Browse.png");
            BrowseIcon.Source = temp;
        }

        public void OnSavePointerOver(object sender, PointerRoutedEventArgs e)
        {
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Save-hover.png");
            SaveIcon.Source = temp;
        }

        public void OnSavePointerExited(object sender, PointerRoutedEventArgs e)
        {
            BitmapImage temp = new BitmapImage();
            temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Save.png");
            SaveIcon.Source = temp;
        }

        public void OnPanPointerOver(object sender, PointerRoutedEventArgs e)
        {
            if (ImageMoving.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Panning-hover.png");
                PanIcon.Source = temp;
            }
        }

        public void OnPanPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (ImageMoving.IsChecked == false)
            {
                BitmapImage temp = new BitmapImage();
                temp.UriSource = new Uri(this.BaseUri, "/Assets/Buttons/Panning.png");
                PanIcon.Source = temp;
            }
        }

        public async void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            // File picker APIs don't work if the app is in a snapped state.
            // If the app is snapped, try to unsnap it first. Only show the picker if it unsnaps.
            if (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped ||
                 Windows.UI.ViewManagement.ApplicationView.TryUnsnap() == true)
            {
                // We call the SaveFile function with true so we can use the file picker.
                await pageRoot.SaveFile(true);
            }
            else
            {
                MessageDialog messageDialog = new MessageDialog("Can't save in snapped state. Please unsnap the app and try again", "Close");
                await messageDialog.ShowAsync();
            }
        }



    }
}
