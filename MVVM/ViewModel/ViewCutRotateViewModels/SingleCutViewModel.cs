using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Filmauswertung_ModernUI.Core;

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

            var tifImporter = new Services.SingleFileImporter(new[] { ".tif" });
            string selectedPath = Services.ImportDialogService.ShowDialog(tifImporter);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                try
                {
                    DisplayedImage = _imageService.LoadImage(selectedPath);

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

        private Point _roiStartPoint;
        public Rect RoiRect { get; private set; }

        public void SetRoi(Point start, Point end)
        {
            RoiRect = new Rect(start, end);
            OnPropertyChanged(nameof(RoiRect));
        }

        private void SaveRoi()
        {
            IsDrawingRoi = false;
            OnPropertyChanged(nameof(IsDrawingRoi));
            Debug.WriteLine($"ROI saved: {RoiRect}");
        }
    }
}
