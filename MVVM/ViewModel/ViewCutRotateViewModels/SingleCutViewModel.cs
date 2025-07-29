using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using Filmauswertung_ModernUI.Core;

namespace Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels
{
    internal class SingleCutViewModel : ObservableObject
    {
        // DisplayedImage property for binding the image source
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

        // Dummy commands
        public RelayCommand UploadTifCommand { get; set; }
        public RelayCommand DefineRoiCommand { get; set; }
        public RelayCommand SaveRoiCommand { get; set; }

        public SingleCutViewModel()
        {
            // Initialize commands with dummy actions
            UploadTifCommand = new RelayCommand(_ => UploadTif());
            DefineRoiCommand = new RelayCommand(_ => DefineRoi());
            SaveRoiCommand = new RelayCommand(_ => SaveRoi());
        }

        private readonly Core.Interfaces.IImageService _imageService = new Services.ImageService();
        private void UploadTif()
        {
            var tifImporter = new Services.SingleFileImporter(new[] { ".tif" });
            string selectedPath = Services.ImportDialogService.ShowDialog(tifImporter);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                try
                {
                    DisplayedImage = _imageService.LoadImage(selectedPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Image load error: {ex.Message}");
                }
            }
        }


        private void DefineRoi()
        {
            Debug.WriteLine("Define ROI command executed.");
            // Implement ROI logic here
        }

        private void SaveRoi()
        {
            Debug.WriteLine("Save ROI command executed.");
            // Implement save logic here
        }
    }
}
