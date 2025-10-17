using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.Core.Interfaces;
using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class CalculateConvertViewModel : ObservableObject
    {
        // -----------------------------
        // Dependencies
        // -----------------------------
        private readonly IToastService _toastService = new ToastService();
        private readonly IImageService _imageService = new ImageService();

        // -----------------------------
        // File List & Selection
        // -----------------------------
        public ObservableCollection<FileEntry> FileList { get; } = new ObservableCollection<FileEntry>();

        private FileEntry _selectedFile;
        public FileEntry SelectedFile
        {
            get => _selectedFile;
            set => SetProperty(ref _selectedFile, value);
        }

        public RelayCommand RemoveFileCommand { get; set; }
        public RelayCommand LoadCalibrationCommand { get; set; }
        public RelayCommand LoadMeasurementsCommand { get; set; }
        public RelayCommand SaveOpgCommand { get; set; }

        // -----------------------------
        // Image display & navigation
        // -----------------------------
        // Hovered pixel
        private string _currentPixelValue;
        public string CurrentPixelValue
        {
            get => _currentPixelValue;
            set => SetProperty(ref _currentPixelValue, value);
        }

        private bool _isPixelHovered;
        public bool IsPixelHovered
        {
            get => _isPixelHovered;
            set => SetProperty(ref _isPixelHovered, value);
        }
        public RelayCommand MouseOverImageCommand { get; set; }
        public RelayCommand MouseLeaveImageCommand { get; set; }


        private bool _isCalibrationLoaded;
        public bool IsCalibrationLoaded
        {
            get => _isCalibrationLoaded;
            set
            {
                if (SetProperty(ref _isCalibrationLoaded, value))
                    LoadMeasurementsCommand.RaiseCanExecuteChanged();
            }
        }

        private Calibration _currentCalibration;
        public Calibration CurrentCalibration
        {
            get => _currentCalibration;
            set => SetProperty(ref _currentCalibration, value);
        }

        private BitmapImage _displayedImage;
        public BitmapImage DisplayedImage
        {
            get => _displayedImage;
            set => SetProperty(ref _displayedImage, value);
        }

        public double[] CurrentDoseValues { get; private set; } = Array.Empty<double>();

        private int _currentImageIndex = -1;
        public RelayCommand LeftImageCommand { get; set; }
        public RelayCommand RightImageCommand { get; set; }

        private string _dynamicLabelText;
        public string DynamicLabelText
        {
            get => _dynamicLabelText;
            set => SetProperty(ref _dynamicLabelText, value);
        }

        private string _dynamicDoseText;
        public string DynamicDoseText
        {
            get => _dynamicDoseText;
            set => SetProperty(ref _dynamicDoseText, value);
        }

        // -----------------------------
        // Bottom Controls
        // -----------------------------
        private double _smoothnessValue = 1;
        public double SmoothnessValue
        {
            get => _smoothnessValue;
            set
            {
                if (SetProperty(ref _smoothnessValue, value))
                {
                    SmoothnessLabel = $"{value:F0}";

                    // Trigger image update immediately when slider changes
                    UpdateDisplayedImage();
                }
            }
        }


        private string _smoothnessLabel;
        public string SmoothnessLabel
        {
            get => _smoothnessLabel;
            set => SetProperty(ref _smoothnessLabel, value);
        }

        private bool _srsResampling;
        public bool SrsResampling
        {
            get => _srsResampling;
            set => SetProperty(ref _srsResampling, value);
        }

        // Dropdowns
        public ObservableCollection<string> LeftDropdownItems { get; } = new ObservableCollection<string>
        {
            "6X", "6F", "10X", "10F", "15X", "6E", "9E", "12E"
        };
        private string _selectedLeftItem;
        public string SelectedLeftItem
        {
            get => _selectedLeftItem;
            set => SetProperty(ref _selectedLeftItem, value);
        }

        public ObservableCollection<string> RightDropdownItems { get; } = new ObservableCollection<string>
        {
            "Film", "SRS", "Other"
        };

        private string _selectedRightItem;
        public string SelectedRightItem
        {
            get => _selectedRightItem;
            set => SetProperty(ref _selectedRightItem, value);
        }

        private bool _isOpgExport = true;
        public bool IsOpgExport
        {
            get => _isOpgExport;
            set => SetProperty(ref _isOpgExport, value);
        }

        // -----------------------------
        // Constructor
        // -----------------------------
        public CalculateConvertViewModel()
        {
            SelectedLeftItem = LeftDropdownItems.First();
            SelectedRightItem = RightDropdownItems.First();

            RemoveFileCommand = new RelayCommand(_ => RemoveFile(), _ => SelectedFile != null);
            LeftImageCommand = new RelayCommand(_ => ShowPreviousDose(), _ => FileList.Any());
            RightImageCommand = new RelayCommand(_ => ShowNextDose(), _ => FileList.Any());
            LoadCalibrationCommand = new RelayCommand(_ => LoadCalibration());
            LoadMeasurementsCommand = new RelayCommand(_ => LoadMeasurements(), _ => IsCalibrationLoaded);
            SaveOpgCommand = new RelayCommand(_ => SaveOpg());
            
            MouseOverImageCommand = new RelayCommand(param =>
            {
                if (param is Point point)
                    UpdatePixelHover(point);
            });

            MouseLeaveImageCommand = new RelayCommand(_ =>
            {
                IsPixelHovered = false;
            });

        }

        private void UpdatePixelHover(Point point)
        {
            if (DisplayedImage == null || CurrentDoseValues == null || CurrentDoseValues.Length == 0)
            {
                IsPixelHovered = false;
                return;
            }

            // Map mouse position to image pixel coordinates
            var img = DisplayedImage;
            double scaleX = img.PixelWidth / (double)img.Width;
            double scaleY = img.PixelHeight / (double)img.Height;

            int x = Math.Min((int)(point.X * scaleX), img.PixelWidth - 1);
            int y = Math.Min((int)(point.Y * scaleY), img.PixelHeight - 1);

            int index = y * img.PixelWidth + x;

            if (index >= 0 && index < CurrentDoseValues.Length)
            {
                CurrentPixelValue = CurrentDoseValues[index].ToString("F2");
                IsPixelHovered = true;
            }
            else
            {
                IsPixelHovered = false;
            }
        }

        // -----------------------------
        // Command Methods
        // -----------------------------
        private void RemoveFile()
        {
            if (SelectedFile == null) return;

            int index = FileList.IndexOf(SelectedFile);
            FileList.Remove(SelectedFile);
            _currentImageIndex = Math.Min(index, FileList.Count - 1);
            UpdateDisplayedImage();
        }

        private void ShowPreviousDose()
        {
            if (!FileList.Any()) return;
            _currentImageIndex = (_currentImageIndex <= 0) ? FileList.Count - 1 : _currentImageIndex - 1;
            UpdateDisplayedImage();
        }

        private void ShowNextDose()
        {
            if (!FileList.Any()) return;
            _currentImageIndex = (_currentImageIndex >= FileList.Count - 1) ? 0 : _currentImageIndex + 1;
            UpdateDisplayedImage();
        }

        private void UpdateDisplayedImage()
        {
            if (_currentImageIndex < 0 || _currentImageIndex >= FileList.Count)
            {
                DisplayedImage = null;
                DynamicLabelText = "Displayed Dose";
                return;
            }

            var file = FileList[_currentImageIndex];
            DynamicLabelText = file.FileName;

            if (!File.Exists(file.FullPath)) return;

            // Load image using ImageService
            var rawImage = _imageService.LoadImage(file.FullPath);

            if (CurrentCalibration != null)
            {
                // Extract dose values
                CurrentDoseValues = CalculationModel.ExtractDoseFromImage(rawImage, CurrentCalibration);

                // Apply smoothing based on SmoothnessValue (1–5)
                int smoothLevel = (int)Clamp(SmoothnessValue, 1, 5);

                if (smoothLevel > 1)
                {
                    int width = rawImage.PixelWidth;
                    int height = rawImage.PixelHeight;
                    double[] smoothed = new double[CurrentDoseValues.Length];

                    // Determine window size based on smooth level
                    int windowRadius = smoothLevel - 1; // 1=no smoothing, 2=>radius=1, ..., 5=>radius=4

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = y * width + x;
                            double sum = 0;
                            int count = 0;

                            // Average over neighborhood
                            for (int dy = -windowRadius; dy <= windowRadius; dy++)
                            {
                                int ny = y + dy;
                                if (ny < 0 || ny >= height) continue;

                                for (int dx = -windowRadius; dx <= windowRadius; dx++)
                                {
                                    int nx = x + dx;
                                    if (nx < 0 || nx >= width) continue;

                                    int nIndex = ny * width + nx;
                                    sum += CurrentDoseValues[nIndex];
                                    count++;
                                }
                            }

                            smoothed[index] = sum / count;
                        }
                    }

                    CurrentDoseValues = smoothed;
                }

                // Calculate statistics
                var stats = CalculationModel.CalculateDoseStatistics(CurrentDoseValues);
                var histogramBins = stats.histogramBins;
                var histogramCounts = stats.histogramCounts;
                var topPeaks = stats.topPeaks;
                var medianDose = stats.medianDose;

                // Build summary string
                string summary = "Top Dose Peaks:\n";
                for (int i = 0; i < topPeaks.Length; i++)
                    summary += $"Peak {i + 1}: {topPeaks[i]:F2} Gy\n";
                summary += $"Median Dose: {medianDose:F2} Gy\n";

                double meanDose = histogramBins.Zip(histogramCounts, (bin, count) => bin * count).Sum() / Math.Max(histogramCounts.Sum(), 1);
                summary += $"Dose Range: {histogramBins.First():F2} Gy - {histogramBins.Last():F2} Gy\n";
                summary += $"Mean Dose: {meanDose:F2} Gy";

                // Update dynamic label
                DynamicDoseText = summary;

                // Convert dose array to heatmap
                DisplayedImage = CalculationModel.ConvertDoseArrayToBitmap(CurrentDoseValues, rawImage.PixelWidth, rawImage.PixelHeight);
            }
            else
            {
                DisplayedImage = rawImage;
                DynamicLabelText = "Displayed Dose";
            }
        }


        private async void LoadCalibration()
        {
            var importer = new SingleFileImporter(new[] { ".json" });
            var selectedFile = ImportDialogService.ShowDialog(importer);
            if (string.IsNullOrEmpty(selectedFile) || !importer.CanImport(selectedFile)) return;

            var files = await importer.ImportAsync(selectedFile);
            var filePath = files.First();

            try
            {
                string jsonText = File.ReadAllText(filePath);
                var calibration = JsonSerializer.Deserialize<Calibration>(jsonText);

                if (calibration == null) return;

                if (!calibration.CalibrationDose.Any(v => v == 0))
                {
                    ShowToast("One calibration value must be zero (e.g., 0, 0.0)", 2);
                    return;
                }

                CurrentCalibration = calibration;
                IsCalibrationLoaded = true;
                ShowToast($"Calibration '{calibration.FileName}' loaded successfully.", 2);
                UpdateDisplayedImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load calibration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMeasurements()
        {
            var dlg = new OpenFileDialog { Filter = "TIF files (*.tif)|*.tif|All files (*.*)|*.*", Multiselect = true };
            if (dlg.ShowDialog() != true) return;

            foreach (var path in dlg.FileNames)
                FileList.Add(new FileEntry { FileName = Path.GetFileName(path), FullPath = path });

            _currentImageIndex = FileList.Count - 1;
            UpdateDisplayedImage();
        }


        private void SaveOpg()
        {
            if (IsOpgExport)
            {
                SaveOpgExport();
            }
            else
            {
                SaveArray();
            }
        }

        private void SaveArray()
        {
            if (!FileList.Any())
            {
                MessageBox.Show("No measurement images loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CurrentCalibration == null)
            {
                MessageBox.Show("No calibration loaded. Cannot calculate doses.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exporter = new FolderExporter("CalcDose");
            string targetFolder = exporter.GetExportPath($"{DateTime.Now:yyyyMMdd}");
            if (string.IsNullOrEmpty(targetFolder))
                return;

            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            ShowToast("Exporting dose arrays as JSON...", 1);

            try
            {
                foreach (var file in FileList)
                {
                    if (!File.Exists(file.FullPath))
                        continue;

                    var rawImage = _imageService.LoadImage(file.FullPath);
                    var doses = CalculationModel.ExtractDoseFromImage(rawImage, CurrentCalibration);

                    int smoothLevel = (int)Clamp(SmoothnessValue, 1, 5);
                    if (smoothLevel > 1)
                        doses = CalculationModel.SmoothDoseArray(doses, rawImage.PixelWidth, rawImage.PixelHeight, smoothLevel);

                    double spacingXmm = 25.4 / rawImage.DpiX;
                    double spacingYmm = 25.4 / rawImage.DpiY;

                    if (_srsResampling)
                    {
                        double targetSpacing = 0.4;
                        doses = CalculationModel.ResampleDoseArray(doses, rawImage.PixelWidth, rawImage.PixelHeight, spacingXmm, spacingYmm, targetSpacing);
                        spacingXmm = targetSpacing;
                        spacingYmm = targetSpacing;
                    }
                    string outPath = Path.Combine(targetFolder, Path.ChangeExtension(file.FileName, ".json"));

                    var doses2D = new double[rawImage.PixelHeight][];
                    for (int y = 0; y < rawImage.PixelHeight; y++)
                    {
                        doses2D[y] = new double[rawImage.PixelWidth];
                        for (int x = 0; x < rawImage.PixelWidth; x++)
                            doses2D[y][x] = doses[y * rawImage.PixelWidth + x];
                    }

                    // Build JSON string for Doses_Gy with each inner array on one line
                    var sbDoses = new System.Text.StringBuilder();
                    sbDoses.Append("[\n");
                    for (int y = 0; y < doses2D.Length; y++)
                    {
                        sbDoses.Append("  [");
                        sbDoses.Append(string.Join(", ", doses2D[y]));
                        sbDoses.Append("]");
                        if (y < doses2D.Length - 1)
                            sbDoses.Append(",\n");
                        else
                            sbDoses.Append("\n");
                    }
                    sbDoses.Append("]");

                    // Create export object without Doses_Gy
                    var exportObj = new
                    {
                        FileName = file.FileName,
                        Width = rawImage.PixelWidth,
                        Height = rawImage.PixelHeight,
                        Resolution_mm = new { X = spacingXmm, Y = spacingYmm },
                        Doses_Gy = "__DOSES_PLACEHOLDER__"
                    };

                    // Serialize outer object with indentation
                    string json = JsonSerializer.Serialize(exportObj, new JsonSerializerOptions { WriteIndented = true });

                    // Replace placeholder with custom Doses_Gy
                    json = json.Replace("\"__DOSES_PLACEHOLDER__\"", sbDoses.ToString());

                    File.WriteAllText(outPath, json);
                }

                Clipboard.SetText(targetFolder);
                ShowToast($"JSON export completed for {FileList.Count} files.\nFolder copied to clipboard.", 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export JSON files:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SaveOpgExport()
        {
            if (!FileList.Any())
            {
                MessageBox.Show("No measurement images loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CurrentCalibration == null)
            {
                MessageBox.Show("No calibration loaded. Cannot calculate doses.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Muster.opg");

            // Folder name pattern: yyyyMMdd_CalcDose
            string datedSuffix = "CalcDose";

            // Ask user for export folder
            var exporter = new FolderExporter(datedSuffix);
            string targetFolder = exporter.GetExportPath($"{DateTime.Now:yyyyMMdd}");
            if (string.IsNullOrEmpty(targetFolder))
                return;

            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            ShowToast(
                    $"Calculating...",
                    1);

            try
            {
                // Generate one OPG file per measurement
                var results = CalculationModel.GenerateOpgFiles(
                    FileList,
                    _imageService,
                    CurrentCalibration,
                    SmoothnessValue,
                    templatePath,
                    SelectedLeftItem,
                    SelectedRightItem,
                    SrsResampling);

                foreach (var kv in results)
                {
                    string outPath = Path.Combine(targetFolder, kv.Key);
                    File.WriteAllText(outPath, kv.Value);
                }

                // Copy folder path to clipboard
                Clipboard.SetText(targetFolder);

                ShowToast(
                    $"Successfully generated {results.Count} OPG files in:\n{targetFolder}\nParent folder copied to clipboard.",
                    1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save OPG files:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }


        private void ShowToast(string message, int duration = 3) => _toastService.ShowToast(message, duration);
    }

    public class FileEntry
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
    }
}
