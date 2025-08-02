using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.MVVM.View;
using Filmauswertung_ModernUI.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels
{
    internal class RotateViewModel : ObservableObject
    {
        private BitmapImage _displayedImage;
        private string _originalImagePath;
        private readonly ImageService _imageService = new ImageService();

        public BitmapImage DisplayedImage
        {
            get => _displayedImage;
            set
            {
                _displayedImage = value;
                OnPropertyChanged(nameof(DisplayedImage));
            }
        }

        // Commands
        public RelayCommand ImportCommand { get; set; }
        public RelayCommand RotateRightCommand { get; }
        public RelayCommand RotateLeftCommand { get; }
        public RelayCommand MirrorCommand { get; }
        public RelayCommand SaveCommand { get; }

        public RotateViewModel()
        {
            ImportCommand = new RelayCommand(_ => ImportTif());
            RotateRightCommand = new RelayCommand(_ => Rotate(90), _ => DisplayedImage != null);
            RotateLeftCommand = new RelayCommand(_ => Rotate(-90), _ => DisplayedImage != null);
            MirrorCommand = new RelayCommand(_ => Mirror(), _ => DisplayedImage != null);
            SaveCommand = new RelayCommand(_ => SaveTif(), _ => DisplayedImage != null);
        }

        private void ImportTif()
        {
            var importer = new SingleFileImporter(new[] { ".tif", ".tiff" });
            string path = ImportDialogService.ShowDialog(importer);

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                _originalImagePath = path;
                DisplayedImage = _imageService.LoadImage(path);
            }
            catch (Exception ex)
            {
                // Handle exceptions, e.g. show message
            }
        }

        private void Rotate(int degrees)
        {
            if (DisplayedImage == null) return;

            TransformedBitmap tb = new TransformedBitmap(DisplayedImage, new System.Windows.Media.RotateTransform(degrees));
            BitmapImage rotatedImage = _imageService.ConvertToBitmapImage(tb);
            DisplayedImage = rotatedImage;
        }

        private void Mirror()
        {
            if (DisplayedImage == null) return;

            TransformedBitmap tb = new TransformedBitmap(DisplayedImage,
                new System.Windows.Media.ScaleTransform(-1, 1, 0.5, 0.5)); // horizontal flip
            BitmapImage mirroredImage = _imageService.ConvertToBitmapImage(tb);
            DisplayedImage = mirroredImage;
        }

        private void SaveTif()
        {
            if (DisplayedImage == null) return;

            // Use original filename (without extension) as base name for saving
            string baseName = "image";
            if (!string.IsNullOrEmpty(_originalImagePath))
            {
                baseName = Path.GetFileNameWithoutExtension(_originalImagePath);
            }

            var exporter = new SingleFileExporter(".tif", "rotated");
            string savePath = exporter.GetExportPath(baseName);

            if (string.IsNullOrEmpty(savePath)) return;

            try
            {
                _imageService.SaveImage(DisplayedImage, savePath);

                // Copy the folder path (directory) to clipboard instead of full file path
                string folderPath = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    CopyPathToClipboardWithToast(folderPath);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g. show message)
            }
        }

        private void CopyPathToClipboardWithToast(string path)
        {
            try
            {
                Clipboard.SetText(path);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var toast = new ToastWindow { ToastMessage = "Save folder path copied to clipboard!" };
                    toast.ShowToast();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to copy to clipboard: {ex.Message}");
            }
        }
    }
}
