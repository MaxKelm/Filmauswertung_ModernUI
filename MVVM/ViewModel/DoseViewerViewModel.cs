using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.Core.Interfaces;
using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.MVVM.Services;
using Filmauswertung_ModernUI.Services;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class DoseViewerViewModel : ObservableObject
    {
        #region Services & Model

        private readonly IToastService _toastService = new ToastService();
        private readonly DoseViewerModel _model = new DoseViewerModel();
        private readonly IOpgLoader _loader = new OpgLoader();
        private readonly Importer _importer = new SingleFileImporter(new[] { ".opg" });
        private readonly Exporter _exporter = new FolderExporter("3DModel");
        private readonly DoseVisualizer _visualizer = new DoseVisualizer();

        #endregion

        #region 3D Model Binding

        private Model3D _referenceModel;
        public Model3D ReferenceModel
        {
            get => _referenceModel;
            set => SetProperty(ref _referenceModel, value);
        }

        private GeometryModel3D _surfaceModelContent;
        public GeometryModel3D SurfaceModelContent
        {
            get => _surfaceModelContent;
            set => SetProperty(ref _surfaceModelContent, value);
        }

        private GeometryModel3D _compareModelContent;
        public GeometryModel3D CompareModelContent
        {
            get => _compareModelContent;
            set => SetProperty(ref _compareModelContent, value);
        }

        private Model3DGroup _displayModelGroup;
        public Model3DGroup DisplayModelGroup
        {
            get => _displayModelGroup;
            set => SetProperty(ref _displayModelGroup, value);
        }

        public HelixViewport3D Viewport { get; set; }
        public Action ZoomToFitAction { get; set; }

        #endregion

        #region Comparison Modes

        public enum CompareMethod { SingleDisplay, DualDisplay, DoseDifference }
        private CompareMethod _selectedCompareMethod = CompareMethod.SingleDisplay;
        public CompareMethod SelectedCompareMethod
        {
            get => _selectedCompareMethod;
            set
            {
                if (_selectedCompareMethod == value) return;

                _selectedCompareMethod = value;
                OnPropertyChanged();

                // Always override the gradient mode for the selected compare method
                switch (_selectedCompareMethod)
                {
                    case CompareMethod.SingleDisplay:
                        SelectedGradientMode = "Default";
                        break;
                    case CompareMethod.DualDisplay:
                        SelectedGradientMode = "Uniform";
                        break;
                    case CompareMethod.DoseDifference:
                        SelectedGradientMode = "Bipolar";
                        break;
                    default:
                        SelectedGradientMode = "Default";
                        break;
                }

                // Refresh after gradient is set
                RefreshDisplay();
            }
        }
        public bool IsSingleDisplay
        {
            get => SelectedCompareMethod == CompareMethod.SingleDisplay;
            set { if (value) SelectedCompareMethod = CompareMethod.SingleDisplay; }
        }
        public bool IsDualDisplay
        {
            get => SelectedCompareMethod == CompareMethod.DualDisplay;
            set { if (value) SelectedCompareMethod = CompareMethod.DualDisplay; }
        }
        public bool IsDoseDifference
        {
            get => SelectedCompareMethod == CompareMethod.DoseDifference;
            set { if (value) SelectedCompareMethod = CompareMethod.DoseDifference; }
        }

        #endregion

        #region File Handling

        public string ReferenceFile
        {
            get => _model.ReferenceFilePath;
            set
            {
                if (_model.ReferenceFilePath == value) return;
                _model.ReferenceFilePath = value;
                OnPropertyChanged();
                IsCompareEnabled = !string.IsNullOrEmpty(value);
            }
        }

        public string CompareFile
        {
            get => _model.CompareFilePath;
            set
            {
                if (_model.CompareFilePath == value) return;
                _model.CompareFilePath = value;
                OnPropertyChanged();
            }
        }

        private bool _isCompareEnabled;
        public bool IsCompareEnabled
        {
            get => _isCompareEnabled;
            set => SetProperty(ref _isCompareEnabled, value);
        }

        #endregion

        #region UI Properties

        private Brush _backgroundBrush = Brushes.Transparent;
        public Brush BackgroundBrush
        {
            get => _backgroundBrush;
            set => SetProperty(ref _backgroundBrush, value);
        }

        public ObservableCollection<string> BackgroundColorOptions { get; }
        public string SelectedBackgroundColor
        {
            get => _model.SelectedBackgroundColor;
            set
            {
                if (_model.SelectedBackgroundColor == value) return;
                _model.SelectedBackgroundColor = value;
                OnPropertyChanged();
                BackgroundBrush = TryConvertToBrush(value) ?? Brushes.Transparent;
            }
        }

        public ObservableCollection<string> GradientModes { get; }
        public string SelectedGradientMode
        {
            get => _model.SelectedGradientMode;
            set
            {
                if (_model.SelectedGradientMode == value) return;
                _model.SelectedGradientMode = value;
                OnPropertyChanged();
                RefreshDisplay();
            }
        }

        public double ReferenceDose
        {
            get => _model.ReferenceDose;
            set
            {
                if (_model.ReferenceDose == value) return;
                _model.ReferenceDose = value;
                OnPropertyChanged();
                RefreshDisplay();
            }
        }

        #endregion

        #region Commands

        public RelayCommand LoadReferenceCommand { get; }
        public RelayCommand LoadCompareCommand { get; }
        public RelayCommand Export3DCommand { get; }
        public RelayCommand TakeScreenshotCommand { get; }

        #endregion

        #region Constructor

        public DoseViewerViewModel()
        {
            BackgroundColorOptions = new ObservableCollection<string>(_model.BackgroundColorOptions);
            GradientModes = new ObservableCollection<string>(_model.GradientModes);

            LoadReferenceCommand = new RelayCommand(async _ => await LoadReferenceFileAsync());
            LoadCompareCommand = new RelayCommand(async _ => await LoadCompareFileAsync(), _ => IsCompareEnabled);
            Export3DCommand = new RelayCommand(_ => Export3DModel());
            TakeScreenshotCommand = new RelayCommand(_ => TakeScreenshot(), _ => DisplayModelGroup != null);
        }

        #endregion

        #region File Loading

        private async Task LoadReferenceFileAsync() => await LoadFileAsync(_importer, true);
        private async Task LoadCompareFileAsync() => await LoadFileAsync(_importer, false);

        private async Task LoadFileAsync(Importer importer, bool isReference)
        {
            string path = ImportDialogService.ShowDialog(importer);
            if (string.IsNullOrEmpty(path)) return;

            var files = await importer.ImportAsync(path);
            string selectedFile = files.FirstOrDefault();
            if (string.IsNullOrEmpty(selectedFile)) return;

            if (isReference) ReferenceFile = selectedFile;
            else CompareFile = selectedFile;

            var data = await _loader.LoadOpgAsync(selectedFile);
            if (data == null)
            {
                ShowToast($"Failed to load {(isReference ? "reference" : "compare")} file.", 3);
                return;
            }

            if (isReference) _model.ReferenceData = data;
            else _model.CompareData = data;

            RefreshDisplay();
        }

        #endregion

        #region Display Refresh

        private void RefreshDisplay()
        {
            var group = new Model3DGroup();

            // Use the current SelectedGradientMode
            string mode = SelectedGradientMode;

            if (IsSingleDisplay && _model.ReferenceData != null)
            {
                // "Uniform" only for Single Display
                if (mode == "Uniform") mode = "Uniform";

                SurfaceModelContent = _visualizer.BuildSurfaceModel(_model.ReferenceData, mode, ReferenceDose);
                if (SurfaceModelContent != null) group.Children.Add(SurfaceModelContent);
            }
            else if (IsDualDisplay && _model.ReferenceData != null && _model.CompareData != null)
            {
                group.Children.Add(BuildDualDisplayModel(_model.ReferenceData, _model.CompareData, mode));
            }
            else if (IsDoseDifference && _model.ReferenceData != null && _model.CompareData != null)
            {
                // If the user hasn't selected a gradient yet, default to Bipolar
                if (string.IsNullOrEmpty(mode) || !_model.GradientModes.Contains(mode))
                    mode = "Bipolar";

                group.Children.Add(BuildDoseDifferenceModel(_model.ReferenceData, _model.CompareData, mode));
            }

            DisplayModelGroup = group;
            ZoomToFitAction?.Invoke();
        }



        private Model3DGroup BuildDualDisplayModel(OpgFileData reference, OpgFileData compare, string gradientMode)
        {
            var group = new Model3DGroup();
            if (reference == null || compare == null) return group;

            void AddColoredModel(OpgFileData data, Color color)
            {
                var surfaceModel = _visualizer.BuildSurfaceModel(data, gradientMode == "Uniform" ? null : gradientMode, ReferenceDose);

                if (gradientMode == "Uniform")
                {
                    var brush = new SolidColorBrush(Color.FromArgb(128, color.R, color.G, color.B));
                    var material = MaterialHelper.CreateMaterial(brush);
                    surfaceModel.Material = material;
                    surfaceModel.BackMaterial = material;
                }

                group.Children.Add(surfaceModel);
            }

            AddColoredModel(reference, Colors.Orange);
            AddColoredModel(compare, Colors.Blue);

            return group;
        }



        private GeometryModel3D BuildDoseDifferenceModel(OpgFileData reference, OpgFileData compare, string gradientMode)
        {
            if (reference == null || compare == null) return null;

            int rows = Math.Min(reference.NoOfRows, compare.NoOfRows);
            int cols = Math.Min(reference.NoOfColumns, compare.NoOfColumns);

            var diffData = new OpgFileData
            {
                X = reference.X.Take(cols).ToList(),
                Y = reference.Y.Take(rows).ToList(),
                NoOfRows = rows,
                NoOfColumns = cols,
                PixelValues = new List<List<double>>()
            };

            for (int y = 0; y < rows; y++)
            {
                var row = new List<double>();
                for (int x = 0; x < cols; x++)
                {
                    row.Add(reference.PixelValues[y][x] - compare.PixelValues[y][x]);
                }
                diffData.PixelValues.Add(row);
            }

            return _visualizer.BuildSurfaceModel(diffData, gradientMode, ReferenceDose);
        }


        #endregion

        #region Screenshot & Export

        private void TakeScreenshot()
        {
            if (Viewport == null)
            {
                MessageBox.Show("Viewport not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int width = (int)Viewport.ActualWidth;
                int height = (int)Viewport.ActualHeight;
                if (width == 0 || height == 0)
                {
                    MessageBox.Show("Viewport has invalid size.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(Viewport);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string filePath = Path.Combine(folder, $"DoseViewer_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                using (var fs = new FileStream(filePath, FileMode.Create))
                    encoder.Save(fs);

                Clipboard.SetText(Path.GetDirectoryName(filePath));
                ShowToast($"Screenshot saved:\n{filePath}\n\nParent folder copied to clipboard.", 2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to take screenshot:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export3DModel()
        {
            if (_model.ReferenceData == null)
            {
                MessageBox.Show("Load a reference file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string targetFolder = _exporter.GetExportPath(DateTime.Now.ToString("yyyyMMdd"));
            if (string.IsNullOrEmpty(targetFolder)) return;

            try
            {
                // TODO: save mesh to STL/OBJ using HelixToolkit exporter
                MessageBox.Show($"3D Model would be exported to:\n{targetFolder}", "Export 3D", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export 3D model:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper

        private void ShowToast(string message, int duration = 3) => _toastService.ShowToast(message, duration);

        private Brush TryConvertToBrush(string colorString)
        {
            try
            {
                return (Brush)new BrushConverter().ConvertFromString(colorString);
            }
            catch
            {
                return null;
            }
        }
        private void PreselectGradientMode()
        {
            if (!string.IsNullOrEmpty(SelectedGradientMode) && _model.GradientModes.Contains(SelectedGradientMode))
                return; // user already selected, don't override

            switch (_selectedCompareMethod)
            {
                case CompareMethod.SingleDisplay:
                    SelectedGradientMode = "Default";
                    break;
                case CompareMethod.DualDisplay:
                    SelectedGradientMode = "Uniform";
                    break;
                case CompareMethod.DoseDifference:
                    SelectedGradientMode = "Bipolar";
                    break;
            }
        }

        #endregion
    }
}
