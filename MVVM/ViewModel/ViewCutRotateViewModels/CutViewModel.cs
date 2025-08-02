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
        #region Commands
        public RelayCommand UploadTifCommand { get; }
        public RelayCommand SaveSegmentsCommand { get; }
        public RelayCommand RemoveLastMarkerCommand { get; }
        public RelayCommand ClearMarkersCommand { get; }
        #endregion

        #region Services and Models
        private readonly Core.Interfaces.IImageService _imageService = new ImageService();
        private readonly SegmentationEngine _segmentationEngine = new SegmentationEngine();
        private readonly CutModel _cutModel = new CutModel();
        #endregion

        #region State Fields
        private bool _isBatchCutMode = true;
        private byte _contrastValue=0;
        private byte _toleranceValue=10;
        private int _marginValue=1;
        private string _loadedImageBaseName;
        private int _roiSaveCount;
        private string _lastSavedFolder;
        private BitmapImage _displayedImage;
        private Point? _lastMarkerPoint;
        #endregion

        #region Properties

        public bool IsBatchCutMode
        {
            get => _isBatchCutMode;
            set => SetProperty(ref _isBatchCutMode, value);
        }
        #region Sliders
        public byte ContrastValue
        {
            get => _contrastValue;
            set
            {
                if (SetProperty(ref _contrastValue, value))
                    OnPropertyChanged(nameof(ContrastSliderLabel));
            }
        }

        public string ContrastSliderLabel => _contrastValue.ToString();

        public byte ToleranceValue
        {
            get => _toleranceValue;
            set
            {
                if (SetProperty(ref _toleranceValue, value))
                    OnPropertyChanged(nameof(ToleranceSliderLabel));
            }
        }

        public string ToleranceSliderLabel => _toleranceValue.ToString();

        public int MarginValue
        {
            get => _marginValue;
            set
            {
                if (SetProperty(ref _marginValue, value))
                    OnPropertyChanged(nameof(MarginSliderLabel));
            }
        }

        public string MarginSliderLabel => SliderMarginHelper.GetLabel(_marginValue);
        #endregion
        public ReadOnlyObservableCollection<Segment> Segments { get; }
        public ReadOnlyObservableCollection<Marker> Markers { get; }
        public event NotifyCollectionChangedEventHandler SegmentsChanged;
        public event NotifyCollectionChangedEventHandler MarkersChanged;

        public BitmapImage DisplayedImage
        {
            get => _displayedImage;
            set => SetProperty(ref _displayedImage, value);
        }

        public Point? LastMarkerPoint
        {
            get => _lastMarkerPoint;
            set => SetProperty(ref _lastMarkerPoint, value);
        }

        #endregion

        #region Constructor
        public CutViewModel()
        {
            Segments = new ReadOnlyObservableCollection<Segment>(_cutModel.Segments);
            Markers = new ReadOnlyObservableCollection<Marker>(_cutModel.Markers);
            _cutModel.Segments.CollectionChanged += (s, e) => SegmentsChanged?.Invoke(s, e);
            _cutModel.Markers.CollectionChanged += (s, e) => MarkersChanged?.Invoke(s, e);

            UploadTifCommand = new RelayCommand(_ => UploadTif());
            SaveSegmentsCommand = new RelayCommand(SaveSegments);
            RemoveLastMarkerCommand = new RelayCommand(_ => RemoveLastMarker());
            ClearMarkersCommand = new RelayCommand(_ => ClearMarkers());
        }
        #endregion

        #region Command Methods

        private void UploadTif()
        {
            var tifImporter = new SingleFileImporter(new[] { ".tif" });
            string selectedPath = ImportDialogService.ShowDialog(tifImporter);

            if (string.IsNullOrEmpty(selectedPath))
                return;

            try
            {
                DisplayedImage = _imageService.LoadImage(selectedPath);
                _loadedImageBaseName = Path.GetFileNameWithoutExtension(selectedPath);
                ResetState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Cut] Image load error: {ex.Message}");
            }
        }

        private void SaveSegments(object obj)
        {
            if (DisplayedImage == null)
                return;

            string baseName = string.IsNullOrEmpty(_loadedImageBaseName) ? "Image" : _loadedImageBaseName;

            if (Segments.Count == 1)
            {
                SaveSingleSegment(baseName);
            }
            else if (Segments.Count > 1)
            {
                SaveMultipleSegments(baseName);
            }
        }

        private void RemoveLastMarker()
        {
            if (_cutModel.Markers.Count > 0)
            {
                _cutModel.Markers.RemoveAt(_cutModel.Markers.Count - 1);
            }

            if (_cutModel.Segments.Count > 0)
            {
                _cutModel.Segments.RemoveAt(_cutModel.Segments.Count - 1);
            }

            LastMarkerPoint = _cutModel.Markers.Count > 0 ? _cutModel.Markers.Last().Position : (Point?)null;
        }

        private void ClearMarkers()
        {
            LastMarkerPoint = null;
            _cutModel.Segments.Clear();
            _cutModel.Markers.Clear();
        }

        #endregion

        #region Helpers

        private void ResetState()
        {
            LastMarkerPoint = null;
            _cutModel.Segments.Clear();
            _cutModel.Markers.Clear();
            _roiSaveCount = 0;
            _lastSavedFolder = null;
        }

        private void SaveSingleSegment(string baseName)
        {
            var rect = Segments[0].BoundingBox;
            string suffix = $"ROI_{_roiSaveCount}";
            var exporter = new SingleFileExporter(".tif", suffix);
            string exportPath = exporter.GetExportPath(baseName);

            if (string.IsNullOrEmpty(exportPath))
                return;

            try
            {
                _imageService.SaveRoi(DisplayedImage, rect, exportPath);
                _roiSaveCount++;
                _lastSavedFolder = Path.GetDirectoryName(exportPath);
                CopyPathToClipboardWithToast(_lastSavedFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save ROI: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveMultipleSegments(string baseName)
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

            CopyPathToClipboardWithToast(exportFolder);
        }

        private void CopyPathToClipboardWithToast(string path)
        {
            try
            {
                Clipboard.SetText(path);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var toast = new ToastWindow { ToastMessage = "Save path copied to clipboard!" };
                    toast.ShowToast();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to copy to clipboard: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        public void AddMarker(Point uiPoint, System.Windows.Controls.Image imageControl)
        {
            if (DisplayedImage == null)
            {
                Debug.WriteLine("[AddMarker] DisplayedImage is null, aborting.");
                return;
            }

            if (imageControl == null)
            {
                Debug.WriteLine("[AddMarker] imageControl is null, aborting.");
                return;
            }

            Debug.WriteLine($"[AddMarker] UI Point: {uiPoint}");

            // Map uiPoint from control coordinates to image pixel coordinates
            var pixelPoint = ConvertToImagePixelCoordinates(uiPoint, imageControl, DisplayedImage);

            Debug.WriteLine($"[AddMarker] Converted to pixel coordinates: {pixelPoint}");

            const double epsilon = 0.1;
            bool duplicateMarker = _cutModel.Markers.Any(m =>
                Math.Abs(m.Position.X - pixelPoint.X) < epsilon &&
                Math.Abs(m.Position.Y - pixelPoint.Y) < epsilon);

            if (duplicateMarker)
            {
                Debug.WriteLine("[AddMarker] Marker too close to existing one, skipping.");
                return;
            }

            LastMarkerPoint = pixelPoint;
            Debug.WriteLine($"[AddMarker] LastMarkerPoint set to: {LastMarkerPoint}");

            var result = _segmentationEngine.SegmentRegion(DisplayedImage, pixelPoint, _toleranceValue);

            if (result == null)
            {
                Debug.WriteLine("[AddMarker] Segmentation result is null, aborting.");
                return;
            }

            if (result.BoundingBox.IsEmpty)
            {
                Debug.WriteLine("[AddMarker] Result bounding box is empty, aborting.");
                return;
            }

            Debug.WriteLine($"[AddMarker] Segmentation bounding box: {result.BoundingBox}");

            var marginFactor = SliderMarginHelper.GetMarginFactor(_marginValue);
            Debug.WriteLine($"[AddMarker] Margin factor: {marginFactor}");

            var inflatedBox = _segmentationEngine.InflateAndClampRect(
                result.BoundingBox, marginFactor, DisplayedImage.PixelWidth, DisplayedImage.PixelHeight);

            Debug.WriteLine($"[AddMarker] Inflated bounding box: {inflatedBox}");

            // Reverse transform inflatedBox from pixel coordinates to UI coordinates
            var scaleX = DisplayedImage.PixelWidth / imageControl.ActualWidth;
            var scaleY = DisplayedImage.PixelHeight / imageControl.ActualHeight;

            var uiRect = new Rect(
                inflatedBox.X / scaleX,
                inflatedBox.Y / scaleY,
                inflatedBox.Width / scaleX,
                inflatedBox.Height / scaleY);

            bool segmentExists = _cutModel.Segments.Any(s => s.BoundingBox == uiRect);

            if (!segmentExists)
            {
                _cutModel.Segments.Add(new Segment(uiRect));
                Debug.WriteLine("[AddMarker] Added new segment (UI coordinates).");
            }
            var uiMarkerPoint = new Point(pixelPoint.X / scaleX, pixelPoint.Y / scaleY);

            _cutModel.Markers.Add(new Marker(uiMarkerPoint));
            Debug.WriteLine("[AddMarker] Added new marker.");
        }

        // Helper method to map UI point to image pixel coordinates
        private Point ConvertToImagePixelCoordinates(Point uiPoint, System.Windows.Controls.Image imageControl, BitmapImage bitmapImage)
        {
            var controlWidth = imageControl.ActualWidth;
            var controlHeight = imageControl.ActualHeight;

            Debug.WriteLine($"[ConvertToImagePixelCoordinates] Control size: {controlWidth} x {controlHeight}");

            var imagePixelWidth = bitmapImage.PixelWidth;
            var imagePixelHeight = bitmapImage.PixelHeight;

            Debug.WriteLine($"[ConvertToImagePixelCoordinates] Image pixel size: {imagePixelWidth} x {imagePixelHeight}");

            double scaleX = imagePixelWidth / controlWidth;
            double scaleY = imagePixelHeight / controlHeight;

            Debug.WriteLine($"[ConvertToImagePixelCoordinates] Scale factors - X: {scaleX}, Y: {scaleY}");

            double pixelX = uiPoint.X * scaleX;
            double pixelY = uiPoint.Y * scaleY;

            // Clamp coordinates to image bounds
            pixelX = Math.Max(0, Math.Min(pixelX, imagePixelWidth - 1));
            pixelY = Math.Max(0, Math.Min(pixelY, imagePixelHeight - 1));

            Debug.WriteLine($"[ConvertToImagePixelCoordinates] Mapped and clamped pixel point: ({pixelX}, {pixelY})");

            return new Point(pixelX, pixelY);
        }



        public void AddSegmentBox(Rect rect)
        {
            if (_cutModel.Segments.All(s => s.BoundingBox != rect))
                _cutModel.Segments.Add(new Segment(rect));
        }

        #endregion
    }
}
