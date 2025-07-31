using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels
{
    internal class BatchCutViewModel : ObservableObject
    {
        public ObservableCollection<Point> MarkerPoints { get; } = new ObservableCollection<Point>();

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

        private bool _isMarkerModeEnabled;
        public bool IsMarkerModeEnabled
        {
            get => _isMarkerModeEnabled;
            set
            {
                _isMarkerModeEnabled = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand UploadTifCommand { get; }
        public RelayCommand SetMarkerCommand { get; }
        public RelayCommand StartProcessCommand { get; }
        public RelayCommand RemoveLastMarkerCommand { get; }
        public RelayCommand ClearMarkersCommand { get; }

        public BatchCutViewModel()
        {
            UploadTifCommand = new RelayCommand(_ => UploadTif());
            SetMarkerCommand = new RelayCommand(_ => IsMarkerModeEnabled = true);
            StartProcessCommand = new RelayCommand(_ => StartProcessing());

            RemoveLastMarkerCommand = new RelayCommand(
                _ => RemoveLastMarker(),
                _ => MarkerPoints.Any()
            );

            ClearMarkersCommand = new RelayCommand(
    _ =>
    {
        Debug.WriteLine($"[BatchCut] ClearMarkers called. Removing {MarkerPoints.Count} markers.");
        MarkerPoints.Clear();
    },
    _ => MarkerPoints.Any()
);

        }

        private readonly Core.Interfaces.IImageService _imageService = new ImageService();

        private void UploadTif()
        {
            var tifImporter = new SingleFileImporter(new[] { ".tif" });
            string selectedPath = ImportDialogService.ShowDialog(tifImporter);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                try
                {
                    DisplayedImage = _imageService.LoadImage(selectedPath);
                    _loadedImageBaseName = Path.GetFileNameWithoutExtension(selectedPath);
                    MarkerPoints.Clear(); // Reset markers
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BatchCut] Image load error: {ex.Message}");
                }
            }
        }

        public void AddMarker(Point p)
        {
            if (IsMarkerModeEnabled)
            {
                MarkerPoints.Add(p);
            }
        }

        public void RemoveLastMarker()
        {
            Debug.WriteLine($"[BatchCut] RemoveLastMarker called. Current marker count: {MarkerPoints.Count}");

            if (MarkerPoints.Count > 0)
            {
                var removed = MarkerPoints.Last();
                MarkerPoints.RemoveAt(MarkerPoints.Count - 1);
                Debug.WriteLine($"[BatchCut] Removed marker at: X={removed.X:F2}, Y={removed.Y:F2}. Remaining markers: {MarkerPoints.Count}");
            }
            else
            {
                Debug.WriteLine("[BatchCut] No markers to remove.");
            }
        }


        private void StartProcessing()
        {
            Debug.WriteLine("Processing started...");
            // Implement batch cutting logic here
        }
    }
}