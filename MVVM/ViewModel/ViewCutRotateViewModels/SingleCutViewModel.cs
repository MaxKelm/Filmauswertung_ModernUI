using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;

namespace Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels
{
    internal class SingleCutViewModel : ObservableObject
    {
        private BitmapImage _displayedImage;
        public BitmapImage DisplayedImage
        {
            get => _displayedImage;
            set
            {
                if (_displayedImage != value)
                {
                    _displayedImage = value;
                    OnPropertyChanged(nameof(DisplayedImage));
                }
            }
        }
        private string _loadedImageBaseName;
        private int _roiSaveCount = 0;

        public RelayCommand UploadTifCommand { get; set; }
        public RelayCommand SaveRoiCommand { get; set; }
        public RelayCommand DefineRoiCommand { get; set; }

        public SingleCutViewModel()
        {
            UploadTifCommand = new RelayCommand(_ => UploadTif());
            SaveRoiCommand = new RelayCommand(_ => SaveRoi());
            DefineRoiCommand = new RelayCommand(_ => StartRoiDrawing());
        }

        private readonly Core.Interfaces.IImageService _imageService = new Services.ImageService();

        private void UploadTif()
        {
            IsDrawingRoi = false;
            OnPropertyChanged(nameof(IsDrawingRoi));

            var tifImporter = new SingleFileImporter(new[] { ".tif" });
            string selectedPath = ImportDialogService.ShowDialog(tifImporter);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                try
                {
                    DisplayedImage = _imageService.LoadImage(selectedPath);
                    _loadedImageBaseName = Path.GetFileNameWithoutExtension(selectedPath);

                    RoiRect = Rect.Empty;
                    OnPropertyChanged(nameof(RoiRect));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Image load error: {ex.Message}");
                }
            }
        }

        public bool IsDrawingRoi { get; private set; }

        private void StartRoiDrawing()
        {
            IsDrawingRoi = true;
            OnPropertyChanged(nameof(IsDrawingRoi));
        }

        public Rect RoiRect { get; private set; }

        public void SetRoi(Point start, Point end)
        {
            RoiRect = new Rect(start, end);
            OnPropertyChanged(nameof(RoiRect));
        }

        private void SaveRoi()
        {
            Debug.WriteLine($"ROI saved: {RoiRect}");

            if (DisplayedImage == null || RoiRect.IsEmpty)
                return;
            string baseName = string.IsNullOrEmpty(_loadedImageBaseName) ? "ROI" : _loadedImageBaseName;
            string suffix = $"ROI_{_roiSaveCount}";
            var exporter = new SingleFileExporter(".tif", suffix);
            string exportPath = exporter.GetExportPath(baseName);

            if (!string.IsNullOrEmpty(exportPath))
            {
                try
                {
                    _imageService.SaveRoi(DisplayedImage, RoiRect, exportPath);
                    _roiSaveCount++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to save ROI: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}
