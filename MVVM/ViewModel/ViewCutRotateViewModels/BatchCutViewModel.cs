using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.MVVM.View;
using Filmauswertung_ModernUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels
{
    internal class BatchCutViewModel : ObservableObject
    {
        private static readonly Dictionary<int, string> SliderALabels = new Dictionary<int, string>()
        {
            { 0, "Original" },
            { 1, "Low increase" },
            { 2, "Medium increase" },
            { 3, "Strong increase" },
            { 4, "Absurde increase" }
        };

        private int _sliderAValue;
        public int SliderAValue
        {
            get => _sliderAValue;
            set
            {
                if (_sliderAValue != value)
                {
                    _sliderAValue = value;
                    OnPropertyChanged(nameof(SliderAValue));
                    OnPropertyChanged(nameof(SliderADisplayLabel)); // Triggers UI update
                }
            }
        }

        private static readonly Dictionary<int, string> SliderBLabels = new Dictionary<int, string>()
        {
            { 0, "No Margin" },
            { 1, "Tight Margin" },
            { 2, "Moderate Margin" },
            { 3, "Wide Margin" },
        };

        private static readonly Dictionary<int, double> MarginFactors = new Dictionary<int, double>()
        {
            { 0, 0.0 },  // No Margin
            { 1, 0.10 }, // Tight Margin (10%)
            { 2, 0.30 }, // Moderate Margin (30%)
            { 3, 0.70 }   // Wide Margin (70%)
        };

        private int _sliderBValue;
        public int SliderBValue
        {
            get => _sliderBValue;
            set
            {
                if (_sliderBValue != value)
                {
                    _sliderBValue = value;
                    OnPropertyChanged(nameof(SliderBValue));
                    OnPropertyChanged(nameof(SliderBDisplayLabel)); // Triggers UI update
                }
            }
        }

        public string SliderADisplayLabel =>
            SliderALabels.TryGetValue(SliderAValue, out var label) ? label : "Unknown";

        public string SliderBDisplayLabel =>
            SliderBLabels.TryGetValue(SliderBValue, out var label) ? label : "Unknown";

        public ObservableCollection<Point> MarkerPoints { get; } = new ObservableCollection<Point>();
        private readonly SegmentationEngine _segmentationEngine = new SegmentationEngine();
        public ObservableCollection<Rect> SegmentBoxes { get; } = new ObservableCollection<Rect>();



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

        public RelayCommand UploadTifCommand { get; }
        public RelayCommand RemoveLastMarkerCommand { get; }
        public RelayCommand ClearMarkersCommand { get; }
        public RelayCommand SaveSegmentsCommand { get; }

        public BatchCutViewModel()
        {
            UploadTifCommand = new RelayCommand(_ => UploadTif());

            RemoveLastMarkerCommand = new RelayCommand(
                _ => RemoveLastMarker(),
                _ => MarkerPoints.Any()
            );

            ClearMarkersCommand = new RelayCommand(
                _ =>
                {
                    MarkerPoints.Clear();
                    SegmentBoxes.Clear();
                },
                _ => MarkerPoints.Any()
            );
            SaveSegmentsCommand = new RelayCommand(
                _ => SaveSegments(),
                _ => SegmentBoxes.Any());
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

                    // Reset everything
                    MarkerPoints.Clear();
                    SegmentBoxes.Clear();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BatchCut] Image load error: {ex.Message}");
                }
            }
        }


        public void AddMarker(Point uiPoint)
        {
            if (DisplayedImage == null)
                return;

            MarkerPoints.Add(uiPoint);

            var result = _segmentationEngine.SegmentRegion(DisplayedImage, uiPoint);

            if (result != null && !result.BoundingBox.IsEmpty)
            {
                var originalBox = result.BoundingBox;

                MarginFactors.TryGetValue(SliderBValue, out double marginFactor);

                var inflatedBox = _segmentationEngine.InflateAndClampRect(originalBox, marginFactor, DisplayedImage.PixelWidth, DisplayedImage.PixelHeight);

                SegmentBoxes.Add(inflatedBox);
            }

        }

        public void RemoveLastMarker()
        {
            if (MarkerPoints.Count > 0)
            {
                var removedMarker = MarkerPoints.Last();
                MarkerPoints.RemoveAt(MarkerPoints.Count - 1);

                if (SegmentBoxes.Count > 0)
                {
                    var removedBox = SegmentBoxes.Last();
                    SegmentBoxes.RemoveAt(SegmentBoxes.Count - 1);
                }
            }
        }

        private void SaveSegments()
        {
            if (DisplayedImage == null || !SegmentBoxes.Any())
                return;

            var exporter = new FolderExporter("segments");
            string baseName = string.IsNullOrEmpty(_loadedImageBaseName) ? "Image" : _loadedImageBaseName;
            string exportFolder = exporter.GetExportPath(baseName);
            if (string.IsNullOrEmpty(exportFolder))
                return;

            for (int i = 0; i < SegmentBoxes.Count; i++)
            {
                var rect = SegmentBoxes[i];
                string savePath = Path.Combine(exportFolder, $"{baseName}_segment_{i + 1}.tif");

                try
                {
                    _imageService.SaveRoi(DisplayedImage, rect, savePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving segment {i + 1}: {ex.Message}");
                }
            }

            try
            {
                Clipboard.SetText(exportFolder);

                // Show toast
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var toast = new ToastWindow
                    {
                        ToastMessage = "Save path saved to clipboard!"
                    };
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