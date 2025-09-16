using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class DoseViewerViewModel : ObservableObject
    {
        private readonly DoseViewerModel _model = new DoseViewerModel();

        // -----------------------------
        // File references
        // -----------------------------
        public string ReferenceFile
        {
            get => _model.ReferenceFilePath;
            set
            {
                if (_model.ReferenceFilePath != value)
                {
                    _model.ReferenceFilePath = value;
                    OnPropertyChanged();
                    IsCompareEnabled = !string.IsNullOrEmpty(value);
                }
            }
        }

        public string CompareFile
        {
            get => _model.CompareFilePath;
            set
            {
                if (_model.CompareFilePath != value)
                {
                    _model.CompareFilePath = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isCompareEnabled;
        public bool IsCompareEnabled
        {
            get => _isCompareEnabled;
            set => SetProperty(ref _isCompareEnabled, value);
        }

        // -----------------------------
        // Bottom controls
        // -----------------------------
        public ObservableCollection<string> BackgroundColorOptions { get; }
        public string SelectedBackgroundColor
        {
            get => _model.SelectedBackgroundColor;
            set
            {
                if (_model.SelectedBackgroundColor != value)
                {
                    _model.SelectedBackgroundColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> GradientModes { get; }
        public string SelectedGradientMode
        {
            get => _model.SelectedGradientMode;
            set
            {
                if (_model.SelectedGradientMode != value)
                {
                    _model.SelectedGradientMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ReferenceDose
        {
            get => _model.ReferenceDose;
            set
            {
                if (_model.ReferenceDose != value)
                {
                    _model.ReferenceDose = value;
                    OnPropertyChanged();
                }
            }
        }

        // -----------------------------
        // Display mode
        // -----------------------------
        public bool IsSingleDisplay
        {
            get => _model.IsSingleDisplay;
            set
            {
                if (_model.IsSingleDisplay != value)
                {
                    _model.IsSingleDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDualDisplay
        {
            get => _model.IsDualDisplay;
            set
            {
                if (_model.IsDualDisplay != value)
                {
                    _model.IsDualDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDoseDifference
        {
            get => _model.IsDoseDifference;
            set
            {
                if (_model.IsDoseDifference != value)
                {
                    _model.IsDoseDifference = value;
                    OnPropertyChanged();
                }
            }
        }

        // -----------------------------
        // Commands
        // -----------------------------
        public RelayCommand LoadReferenceCommand { get; }
        public RelayCommand LoadCompareCommand { get; }
        public RelayCommand Export3DCommand { get; }

        // -----------------------------
        // Services
        // -----------------------------
        private readonly Importer _importer = new SingleFileImporter(new[] { ".opg" });
        private readonly Exporter _exporter = new FolderExporter("3DModel");

        // -----------------------------
        // Constructor
        // -----------------------------
        public DoseViewerViewModel()
        {
            BackgroundColorOptions = new ObservableCollection<string>(_model.BackgroundColorOptions);
            GradientModes = new ObservableCollection<string>(_model.GradientModes);

            LoadReferenceCommand = new RelayCommand(async _ => await LoadReferenceFileAsync());
            LoadCompareCommand = new RelayCommand(async _ => await LoadCompareFileAsync(), _ => IsCompareEnabled);
            Export3DCommand = new RelayCommand(_ => Export3DModel());
        }

        // -----------------------------
        // Methods
        // -----------------------------
        private async Task LoadReferenceFileAsync()
        {
            string path = ImportDialogService.ShowDialog(_importer);
            if (string.IsNullOrEmpty(path)) return;

            var files = await _importer.ImportAsync(path);
            ReferenceFile = files.FirstOrDefault();

            if (!string.IsNullOrEmpty(ReferenceFile))
            {
                _model.ReferenceData = await LoadOpgDataAsync(ReferenceFile);
            }
        }

        private async Task LoadCompareFileAsync()
        {
            string path = ImportDialogService.ShowDialog(_importer);
            if (string.IsNullOrEmpty(path)) return;

            var files = await _importer.ImportAsync(path);
            CompareFile = files.FirstOrDefault();

            if (!string.IsNullOrEmpty(CompareFile))
            {
                _model.CompareData = await LoadOpgDataAsync(CompareFile);
            }
        }

        private Task<OpgFileData> LoadOpgDataAsync(string filePath)
        {
            // Simulate reading OPG file. Replace this with real parser.
            return Task.FromResult(new OpgFileData
            {
                ImageName = System.IO.Path.GetFileName(filePath),
                Energy = "6MV",
                DataUnit = "cGy",
                DataFactor = 1.0,
                NoOfColumns = 100,
                NoOfRows = 100,
                X = Enumerable.Range(0, 100).Select(i => (double)i).ToList(),
                Y = Enumerable.Range(0, 100).Select(i => (double)i).ToList(),
                PixelValues = Enumerable.Range(0, 100)
                    .Select(_ => Enumerable.Range(0, 100).Select(j => j * 0.1).ToList())
                    .ToList()
            });
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
                // TODO: Implement real 3D export logic using HelixToolkit or another 3D library
                MessageBox.Show($"3D Model would be exported to:\n{targetFolder}", "Export 3D", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export 3D model:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
