using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.MVVM.View;
using Filmauswertung_ModernUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels
{
    internal class CutViewModel : ObservableObject
    {
        // Commands for UI buttons
        public RelayCommand UploadTifCommand { get; }
        public RelayCommand SaveSegmentsCommand { get; }
        public RelayCommand RemoveLastMarkerCommand { get; }
        public RelayCommand ClearMarkersCommand { get; }

        private readonly Core.Interfaces.IImageService _imageService = new ImageService();
        private readonly SegmentationEngine _segmentationEngine = new SegmentationEngine();

        private bool _isBatchCutMode = true;
        public bool IsBatchCutMode
        {
            get => _isBatchCutMode;
            set
            {
                if (_isBatchCutMode != value)
                {
                    _isBatchCutMode = value;
                    OnPropertyChanged(nameof(IsBatchCutMode));
                }
            }
        }
        private byte _contrastValue;
        public byte ContrastValue
        {
            get => _contrastValue;
            set
            {
                if (_contrastValue != value)
                {
                    _contrastValue = value;
                    OnPropertyChanged(nameof(ContrastValue));
                    OnPropertyChanged(nameof(ContrastSliderLabel));
                }
            }
        }

        public string ContrastSliderLabel => _contrastValue.ToString();
        private byte _toleranceValue;
        public byte ToleranceValue
        {
            get => _toleranceValue;
            set
            {
                if (_toleranceValue != value)
                {
                    _toleranceValue = value;
                    OnPropertyChanged(nameof(ToleranceValue));
                    OnPropertyChanged(nameof(ToleranceSliderLabel));
                }
            }
        }

        public string ToleranceSliderLabel => _toleranceValue.ToString();


        private int _marginValue;
        public int MarginValue
        {
            get => _marginValue;
            set
            {
                if (_marginValue != value)
                {
                    _marginValue = value;
                    OnPropertyChanged(nameof(MarginValue));
                    OnPropertyChanged(nameof(MarginSliderLabel));
                }
            }
        }

        public string MarginSliderLabel => SliderMarginHelper.GetLabel(_marginValue);


        private string _loadedImageBaseName;
        private int _roiSaveCount;
        private string _lastSavedFolder;

        private readonly CutModel _cutModel = new CutModel();

        public ReadOnlyObservableCollection<Segment> Segments { get; }
        public ReadOnlyObservableCollection<Marker> Markers { get; }


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

        private Point? _lastMarkerPoint;
        public Point? LastMarkerPoint
        {
            get => _lastMarkerPoint;
            set
            {
                if (_lastMarkerPoint != value)
                {
                    _lastMarkerPoint = value;
                    OnPropertyChanged(nameof(LastMarkerPoint));
                }
            }
        }

        public CutViewModel()
        {
            Segments = new ReadOnlyObservableCollection<Segment>(_cutModel.Segments);
            Markers = new ReadOnlyObservableCollection<Marker>(_cutModel.Markers);
            UploadTifCommand = new RelayCommand(_ => UploadTif());
            SaveSegmentsCommand = new RelayCommand(SaveSegments);
            RemoveLastMarkerCommand = new RelayCommand(_ => RemoveLastMarker());
            ClearMarkersCommand = new RelayCommand(_ =>
            {
                LastMarkerPoint = null;
                _cutModel.Segments.Clear();
                _cutModel.Markers.Clear();

            });
        }

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

                    // Reset state
                    LastMarkerPoint = null;
                    _cutModel.Segments.Clear();
                    _cutModel.Markers.Clear();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Cut] Image load error: {ex.Message}");
                }
            }
        }

        private void SaveSegments(object obj)
        {
            if (DisplayedImage == null)
                return;

            string baseName = string.IsNullOrEmpty(_loadedImageBaseName) ? "Image" : _loadedImageBaseName;

            // --- Single Segment (ROI) ---
            if (Segments.Count == 1)
            {
                var rect = Segments[0].BoundingBox;
                string suffix = $"ROI_{_roiSaveCount}";
                var exporter = new SingleFileExporter(".tif", suffix);
                string exportPath = exporter.GetExportPath(baseName);

                if (!string.IsNullOrEmpty(exportPath))
                {
                    try
                    {
                        _imageService.SaveRoi(DisplayedImage, rect, exportPath);
                        _roiSaveCount++;
                        _lastSavedFolder = Path.GetDirectoryName(exportPath);

                        Clipboard.SetText(_lastSavedFolder);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var toast = new ToastWindow
                            {
                                ToastMessage = "Save path copied to clipboard!",
                            };
                            toast.ShowToast();
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to save ROI: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            // --- Multiple Segments ---
            else if (Segments.Count > 1)
            {
                var exporter = new FolderExporter("segments");
                string exportFolder = exporter.GetExportPath(baseName);

                if (string.IsNullOrEmpty(exportFolder))
                    return;

                for (int i = 0; i < Segments.Count; i++)
                {
                    var rect = Segments[i].BoundingBox;
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

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var toast = new ToastWindow
                        {
                            ToastMessage = "Save path copied to clipboard!"
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

        private void RemoveLastMarker()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_cutModel.Markers.Count > 0)
                {
                    _cutModel.Markers.RemoveAt(_cutModel.Markers.Count - 1);
                }

                if (_cutModel.Segments.Count > 0)
                {
                    _cutModel.Segments.RemoveAt(_cutModel.Segments.Count - 1);
                }

                if (_cutModel.Markers.Count > 0)
                    LastMarkerPoint = _cutModel.Markers.Last().Position;
                else
                    LastMarkerPoint = null;
            });
        }


        public void AddMarker(Point uiPoint)
        {
            if (DisplayedImage == null) return;

            const double epsilon = 0.1;
            if (_cutModel.Markers.Any(m => Math.Abs(m.Position.X - uiPoint.X) < epsilon && Math.Abs(m.Position.Y - uiPoint.Y) < epsilon))
                return;

            LastMarkerPoint = uiPoint;

            var result = _segmentationEngine.SegmentRegion(DisplayedImage, uiPoint, _toleranceValue);

            if (result != null && !result.BoundingBox.IsEmpty)
            {
                var originalBox = result.BoundingBox;
                var marginFactor = SliderMarginHelper.GetMarginFactor(_marginValue);

                var inflatedBox = _segmentationEngine.InflateAndClampRect(
                    originalBox, marginFactor, DisplayedImage.PixelWidth, DisplayedImage.PixelHeight);

                if (!_cutModel.Segments.Any(s => s.BoundingBox == inflatedBox))
                {
                    _cutModel.Segments.Add(new Segment(inflatedBox));
                }

                _cutModel.Markers.Add(new Marker(uiPoint));
            }
        }


        public void AddSegmentBox(Rect rect)
        {
            if (_cutModel.Segments.All(s => s.BoundingBox != rect))
            {
                _cutModel.Segments.Add(new Segment(rect));
            }
        }
    }
}
