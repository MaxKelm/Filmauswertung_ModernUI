using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class CalculateConvertViewModel : ObservableObject
    {
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
        private bool _isCalibrationLoaded;
        public bool IsCalibrationLoaded
        {
            get => _isCalibrationLoaded;
            set => SetProperty(ref _isCalibrationLoaded, value);
        }
        private BitmapImage _displayedImage;
        public BitmapImage DisplayedImage
        {
            get => _displayedImage;
            set => SetProperty(ref _displayedImage, value);
        }

        private int _currentImageIndex = -1;

        public RelayCommand ImageButton1Command { get; set; } // Previous Dose
        public RelayCommand ImageButton2Command { get; set; } // Next Dose

        private string _dynamicLabelText;
        public string DynamicLabelText
        {
            get => _dynamicLabelText;
            set => SetProperty(ref _dynamicLabelText, value);
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
                    SmoothnessLabel = $"{value:F0}";
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

        // -----------------------------
        // Constructor
        // -----------------------------
        public CalculateConvertViewModel()
        {
            // Default dropdown selection
            SelectedLeftItem = LeftDropdownItems.First();
            SelectedRightItem = RightDropdownItems.First();

            // Commands
            RemoveFileCommand = new RelayCommand(_ => RemoveFile(), _ => SelectedFile != null);
            ImageButton1Command = new RelayCommand(_ => ShowPreviousDose(), _ => FileList.Any());
            ImageButton2Command = new RelayCommand(_ => ShowNextDose(), _ => FileList.Any());
            LoadCalibrationCommand = new RelayCommand(_ => LoadCalibration());
            LoadMeasurementsCommand = new RelayCommand(_ => LoadMeasurements());
            SaveOpgCommand = new RelayCommand(_ => SaveOpg());
        }

        // -----------------------------
        // Command Methods
        // -----------------------------
        private void RemoveFile()
        {
            if (SelectedFile != null)
            {
                int index = FileList.IndexOf(SelectedFile);
                FileList.Remove(SelectedFile);
                _currentImageIndex = Math.Min(index, FileList.Count - 1);
                UpdateDisplayedImage();
            }
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
            if (_currentImageIndex >= 0 && _currentImageIndex < FileList.Count)
            {
                var file = FileList[_currentImageIndex];
                if (File.Exists(file.FullPath))
                {
                    DisplayedImage = new BitmapImage(new Uri(file.FullPath));
                    DynamicLabelText = file.FileName;
                }
            }
            else
            {
                DisplayedImage = null;
                DynamicLabelText = "Displayed Dose";
            }
        }

        private void LoadCalibration()
        {
            var dlg = new OpenFileDialog { Filter = "Calibration files (*.cal)|*.cal|All files (*.*)|*.*", Multiselect = false };
            if (dlg.ShowDialog() == true)
            {
                // Example: add file
                FileList.Add(new FileEntry { FileName = Path.GetFileName(dlg.FileName), FullPath = dlg.FileName });
                _currentImageIndex = FileList.Count - 1;
                UpdateDisplayedImage();
            }
        }

        private void LoadMeasurements()
        {
            var dlg = new OpenFileDialog { Filter = "TIF files (*.tif)|*.tif|All files (*.*)|*.*", Multiselect = true };
            if (dlg.ShowDialog() == true)
            {
                foreach (var path in dlg.FileNames)
                    FileList.Add(new FileEntry { FileName = Path.GetFileName(path), FullPath = path });

                _currentImageIndex = FileList.Count - 1;
                UpdateDisplayedImage();
            }
        }

        private void SaveOpg()
        {
            // Implement saving logic here
            MessageBox.Show("Save .opg functionality not yet implemented.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // -----------------------------
    // Helper class for file entries
    // -----------------------------
    public class FileEntry
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
    }
}
