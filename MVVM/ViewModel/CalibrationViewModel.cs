using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.Core.Interfaces;
using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class CalibrationViewModel : ObservableObject
    {
        public ObservableCollection<CalibrationImageEntry> CalibrationFileNamesView { get; }
            = new ObservableCollection<CalibrationImageEntry>();

        private readonly IToastService _toastService = new ToastService();
        private CalibrationImageEntry _selectedCalibrationFile;

        private List<double> _lastDoseValues;
        private List<double> _lastOdValues;
        private double _lastBackgroundMedian;
        private double[] _lastPolynomialCoefficients;
        private int _successfulSaveCount = 0;

        private PlotModel _calibrationPlotModel;
        public PlotModel CalibrationPlotModel
        {
            get => _calibrationPlotModel;
            set => SetProperty(ref _calibrationPlotModel, value);
        }

        public CalibrationImageEntry SelectedCalibrationFile
        {
            get => _selectedCalibrationFile;
            set => SetProperty(ref _selectedCalibrationFile, value);
        }

        private string _calibrationInputText;
        public string CalibrationInputText
        {
            get => _calibrationInputText;
            set => SetProperty(ref _calibrationInputText, value);
        }

        private string _selectedUnit;
        public string SelectedUnit
        {
            get => _selectedUnit;
            set => SetProperty(ref _selectedUnit, value);
        }

        public ObservableCollection<string> Units { get; } = new ObservableCollection<string> { "Gy", "cGy", "mGy" };

        public RelayCommand ImportCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand RemoveCommand { get; }
        public RelayCommand ProcessCommand { get; }

        public CalibrationViewModel()
        {
            SelectedUnit = "Gy";

            ImportCommand = new RelayCommand(_ => Import());
            SaveCommand = new RelayCommand(_ => Save(), _ => _lastPolynomialCoefficients != null);
            RemoveCommand = new RelayCommand(_ => RemoveSelected(), _ => SelectedCalibrationFile != null);
            ProcessCommand = new RelayCommand(_ => Process(), _ => SelectedCalibrationFile != null);
        }

        private async void Import()
        {
            var importer = new MultipleFilesImporter(new[] { ".tif" });
            var selected = ImportDialogService.ShowDialog(importer);

            if (string.IsNullOrWhiteSpace(selected) || !importer.CanImport(selected))
            {
                MessageBox.Show("Selected files are not valid TIF files.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var paths = await importer.ImportAsync(selected);
            var entries = CalibrationModel.CreateEntriesFromPaths(paths);

            foreach (var entry in entries)
            {
                if (!CalibrationFileNamesView.Any(e => e.FullPath == entry.FullPath))
                    CalibrationFileNamesView.Add(entry);
            }

            SelectedCalibrationFile = CalibrationFileNamesView.LastOrDefault();
        }

        private void Save()
        {
            if (_lastDoseValues == null || _lastOdValues == null || _lastPolynomialCoefficients == null)
            {
                ShowToast("Please run calibration first (Process) before saving.", 3);
                return;
            }

            _successfulSaveCount++;
            string dateString = DateTime.Now.ToString("yyyy-MM-dd");
            string fileName = $"calibration_{dateString}";

            var exporter = new SingleFileExporter(".json", suggestedSuffix: _successfulSaveCount.ToString());
            string exportPath = exporter.GetExportPath(fileName);
            if (string.IsNullOrWhiteSpace(exportPath))
                return;

            try
            {
                var calibrationInfo = new
                {
                    FileName = fileName,
                    CalibrationValues = CalibrationInputText,
                    Unit = SelectedUnit,
                    BackgroundMedian = _lastBackgroundMedian,
                    PolynomialFitCoefficients = _lastPolynomialCoefficients
                };

                string json = JsonSerializer.Serialize(calibrationInfo, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(exportPath, json);

                string folderPath = Path.GetDirectoryName(exportPath);
                if (!string.IsNullOrEmpty(folderPath))
                    Clipboard.SetText(folderPath);

                ShowToast($"Calibration data exported successfully as {Path.GetFileName(exportPath)}. Folder path copied to clipboard.", 3);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveSelected()
        {
            if (SelectedCalibrationFile != null)
            {
                CalibrationFileNamesView.Remove(SelectedCalibrationFile);
                SelectedCalibrationFile = CalibrationFileNamesView.LastOrDefault();
            }
        }

        private void Process()
        {
            if (string.IsNullOrWhiteSpace(CalibrationInputText))
            {
                ShowToast("Please enter calibration values separated by ';'");
                return;
            }

            var entries = CalibrationInputText
    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(s => s.Trim().Replace(',', '.'))
    .ToList();


            if (entries.Count != CalibrationFileNamesView.Count)
            {
                ShowToast($"Number of values ({entries.Count}) must match number of images ({CalibrationFileNamesView.Count})");
                return;
            }

            var doseValues = new List<double>();
            foreach (var entry in entries)
            {
                if (!double.TryParse(entry, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    ShowToast($"Invalid number: '{entry}' (use '.' or ',' as decimal)");
                    return;
                }

                // Convert to Gy (C# 7.3 compatible)
                switch (SelectedUnit)
                {
                    case "Gy":
                        // no conversion needed
                        break;
                    case "cGy":
                        val = val / 100.0;
                        break;
                    case "mGy":
                        val = val / 1000.0;
                        break;
                    default:
                        // fallback, keep val as is
                        break;
                }
                doseValues.Add(val);
            }

            if (!doseValues.Any(v => Math.Abs(v) < 1e-9))
            {
                ShowToast("One calibration value must be zero (e.g., 0, 0.0)");
                return;
            }

            var imagePaths = CalibrationFileNamesView.Select(e => e.FullPath).ToList();
            var brightnessAnalysis = CalibrationModel.AnalyzeImageBrightness(imagePaths);
            var brightestImagePath = brightnessAnalysis.BrightestImagePath;

            foreach (var warning in brightnessAnalysis.Warnings)
                ShowToast(warning, 5);

            var odResult = CalibrationModel.CalculateOpticalDensities(imagePaths, brightestImagePath);
            var odValues = odResult.OdValues;
            var backgroundMedian = odResult.BackgroundTransmittance;

            var sortedDoseValues = doseValues.OrderBy(x => x).ToList();
            var sortedOdValues = odValues.OrderBy(x => x).ToList();

            var coefficients = CalibrationModel.FitPolynomial3rdDegree(sortedOdValues, sortedDoseValues);
            ShowToast($"Polynomial Fit: y = {coefficients[3]:F4} od^3 + {coefficients[2]:F4} od^2 + {coefficients[1]:F4} od + {coefficients[0]:F4}", 2);

            UpdateCalibrationPlot(sortedOdValues, sortedDoseValues, coefficients);

            // Store last results
            _lastDoseValues = doseValues;
            _lastOdValues = odValues;
            _lastBackgroundMedian = backgroundMedian;
            _lastPolynomialCoefficients = coefficients;
        }
        private void UpdateCalibrationPlot(List<double> odValues, List<double> doseValues, double[] coefficients)
        {
            if (odValues == null || doseValues == null || coefficients == null)
                return;

            var model = new PlotModel
            {
                Title = "Calibration Curve",
                TextColor = OxyColors.LightGray,
                TitleColor = OxyColors.LightGray,
                PlotAreaBorderColor = OxyColors.LightGray
            };

            // Scatter points for measured OD vs Dose
            var scatter = new ScatterSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColors.CornflowerBlue,
                MarkerSize = 4,
                Title = "Measured Data"
            };

            for (int i = 0; i < odValues.Count; i++)
                scatter.Points.Add(new ScatterPoint(odValues[i], doseValues[i]));

            model.Series.Add(scatter);

            // Fit cubic polynomial (smooth curve)
            var line = new LineSeries
            {
                Color = OxyColors.Red,
                StrokeThickness = 2,
                Title = "3rd Degree Fit"
            };

            double xMin = odValues.Min();
            double xMax = odValues.Max();
            int steps = 200;
            double step = (xMax - xMin) / steps;

            for (int i = 0; i <= steps; i++)
            {
                double x = xMin + i * step;
                double y = coefficients[0] + coefficients[1] * x + coefficients[2] * x * x + coefficients[3] * x * x * x;
                line.Points.Add(new DataPoint(x, y));
            }

            model.Series.Add(line);

            // Axes with light gray font and gridlines
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Optical Density",
                TitleColor = OxyColors.LightGray,
                TextColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Dose [Gy]",
                TitleColor = OxyColors.LightGray,
                TextColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray
            });

            CalibrationPlotModel = model;
        }


        private void ShowToast(string message, int duration = 3)
        {
            _toastService.ShowToast(message, duration);
        }
    }
}
