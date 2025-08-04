using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.MVVM.View;
using Filmauswertung_ModernUI.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;           // For JSON serialization
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;


namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class CalibrationViewModel : ObservableObject
    {
        public ObservableCollection<CalibrationImageEntry> CalibrationFileNamesView { get; } = new ObservableCollection<CalibrationImageEntry>();

        private CalibrationImageEntry _selectedCalibrationFile;
        public CalibrationImageEntry SelectedCalibrationFile
        {
            get { return _selectedCalibrationFile; }
            set
            {
                if (SetProperty(ref _selectedCalibrationFile, value))
                {
                    DisplayedCalibrationImage = CalibrationModel.LoadImage(value != null ? value.FullPath : null);
                }
            }
        }

        private BitmapImage _displayedCalibrationImage;
        public BitmapImage DisplayedCalibrationImage
        {
            get { return _displayedCalibrationImage; }
            set { SetProperty(ref _displayedCalibrationImage, value); }
        }

        private string _calibrationInputText;
        public string CalibrationInputText
        {
            get { return _calibrationInputText; }
            set { SetProperty(ref _calibrationInputText, value); }
        }

        private string _selectedUnit;
        public string SelectedUnit
        {
            get { return _selectedUnit; }
            set { SetProperty(ref _selectedUnit, value); }
        }

        public ObservableCollection<string> Units { get; } = new ObservableCollection<string> { "Gy", "cGy", "mGy" };

        public RelayCommand ImportCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand RemoveCommand { get; set; }
        public RelayCommand ProcessCommand { get; set; }

        public CalibrationViewModel()
        {
            SelectedUnit = "Gy";  // default unit selected
            ImportCommand = new RelayCommand(param => Import());
            SaveCommand = new RelayCommand(param => Save(), param => DisplayedCalibrationImage != null);
            RemoveCommand = new RelayCommand(param => RemoveSelected(), param => SelectedCalibrationFile != null);
            ProcessCommand = new RelayCommand(param => Process(), param => DisplayedCalibrationImage != null);
        }

        private async void Import()
        {
            var importer = new MultipleFilesImporter(new[] { ".tif" });

            var selected = ImportDialogService.ShowDialog(importer);
            if (string.IsNullOrWhiteSpace(selected))
                return;

            if (!importer.CanImport(selected))
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
            string dateString = DateTime.Now.ToString("yyyy-MM-dd");
            var exporter = new SingleFileExporter(".json", $"calibration_{dateString}");

            string exportPath = exporter.GetExportPath($"calibration_{dateString}");
            if (string.IsNullOrWhiteSpace(exportPath))
                return;

            try
            {
                var calibrationInfo = new
                {
                    FileName = SelectedCalibrationFile?.FileName,
                    CalibrationValue = CalibrationInputText,
                    Unit = SelectedUnit
                };

                string json = System.Text.Json.JsonSerializer.Serialize(calibrationInfo, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(exportPath, json);
                System.Windows.MessageBox.Show("Calibration data exported successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                .Select(s => s.Trim())
                .ToList();

            if (entries.Count != CalibrationFileNamesView.Count)
            {
                ShowToast($"Number of values ({entries.Count}) must match number of images ({CalibrationFileNamesView.Count})");
                return;
            }

            var normalizedEntries = entries.Select(s => s.Replace(',', '.')).ToList();

            var values = new List<double>();
            foreach (var entry in normalizedEntries)
            {
                if (!double.TryParse(entry, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    ShowToast($"Invalid number: '{entry}' (use '.' or ',' as decimal)");
                    return;
                }
                values.Add(val);
            }

            if (!values.Any(v => Math.Abs(v) < 1e-9))
            {
                ShowToast("One calibration value must be zero (e.g., 0, 0.0)");
                return;
            }

            ShowToast($"Calibration processed for {values.Count} files");
        }

        private void ShowToast(string message, int duration = 3)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new ToastWindow
                {
                    ToastMessage = message,
                    ToastDuration = TimeSpan.FromSeconds(duration)
                };
                toast.ShowToast();
            });
        }

    }
}
