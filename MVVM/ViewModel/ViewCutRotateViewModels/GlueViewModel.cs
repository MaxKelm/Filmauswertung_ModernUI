using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels
{
    internal class GlueViewModel : ObservableObject
    {
        // 1) Now holds ImageItem, not string
        private ObservableCollection<ImageItem> _imageFileNames = new ObservableCollection<ImageItem>();
        public ObservableCollection<ImageItem> ImageFileNames
        {
            get => _imageFileNames;
            set
            {
                _imageFileNames = value;
                OnPropertyChanged(nameof(ImageFileNames));
                OnPropertyChanged(nameof(ColumnMax));
                OnPropertyChanged(nameof(RowMax));
                ImageFileNamesView = CollectionViewSource.GetDefaultView(_imageFileNames);
            }
        }

        private ICollectionView _imageFileNamesView;
        public ICollectionView ImageFileNamesView
        {
            get => _imageFileNamesView;
            private set
            {
                _imageFileNamesView = value;
                OnPropertyChanged(nameof(ImageFileNamesView));
            }
        }

        private ImageItem _selectedImage;
        public ImageItem SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                OnPropertyChanged(nameof(SelectedImage));
                LoadImage(value?.FullPath);
                RaiseCommandStates();
                OnPropertyChanged(nameof(ColumnMax));
                OnPropertyChanged(nameof(RowMax));

            }
        }

        private DrawingImage _displayedImage;
        public DrawingImage DisplayedImage
        {
            get => _displayedImage;
            set => SetProperty(ref _displayedImage, value);
        }

        private int _columns = 1;
        public int Columns
        {
            get => _columns;
            set
            {
                if (_columns != value)
                {
                    _columns = value;
                    OnPropertyChanged(nameof(Columns));
                    OnPropertyChanged(nameof(RowMax));

                    // Clamp Rows to new RowMax if necessary
                    if (Rows > RowMax)
                    {
                        Rows = RowMax;
                    }
                    else
                    {
                        // Update image grid when Columns changes but Rows is fine
                        UpdateDisplayedImageGrid();
                    }
                }
            }
        }

        private int _rows = 1;
        public int Rows
        {
            get => _rows;
            set
            {
                if (SetProperty(ref _rows, value))
                {
                    UpdateDisplayedImageGrid();
                }
            }
        }
        public int ColumnMax => ImageFileNames.Count;

        public int RowMax
        {
            get
            {
                if (Columns <= 0) return 1;
                return (int)Math.Ceiling((double)ImageFileNames.Count / Columns);
            }
        }

        public int ToleranceMax => 10; // Used by sliders

        // Commands
        public RelayCommand ImportCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand MoveUpCommand { get; }
        public RelayCommand MoveDownCommand { get; }
        public RelayCommand RemoveCommand { get; }

        // Services
        private readonly ImageService _imageService
            = new ImageService();
        private readonly Importer _importer
            = new MultipleFilesImporter(new[] { ".tif" });

        public GlueViewModel()
        {
            ImportCommand = new RelayCommand(_ => ImportImages());
            SaveCommand = new RelayCommand(_ => SaveImage());
            MoveUpCommand = new RelayCommand(_ => MoveUp(), _ => SelectedImage != null);
            MoveDownCommand = new RelayCommand(_ => MoveDown(), _ => SelectedImage != null);
            RemoveCommand = new RelayCommand(_ => Remove(), _ => SelectedImage != null);

            // initialize collection & view
            ImageFileNames = new ObservableCollection<ImageItem>();
            ImageFileNamesView = CollectionViewSource.GetDefaultView(ImageFileNames);
        }

        private async void ImportImages()
        {
            var selectedPath = ImportDialogService.ShowDialog(_importer);
            if (string.IsNullOrWhiteSpace(selectedPath)) return;

            var paths = await _importer.ImportAsync(selectedPath);

            ImageFileNames.Clear();
            foreach (var path in paths)
            {
                ImageFileNames.Add(new ImageItem
                {
                    FullPath = path,
                    FileNameWithoutExtension = Path.GetFileNameWithoutExtension(path)
                });
            }

            SelectedImage = ImageFileNames.FirstOrDefault();
        }

        private void SaveImage()
        {
            Debug.WriteLine("SaveCommand executed.");
            // TODO: call _imageService.SaveImage or SaveRoi here
        }

        private void LoadImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                UpdateDisplayedImageGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load image: {ex.Message}");
            }
        }
        private void UpdateDisplayedImageGrid()
        {
            try
            {
                DisplayedImage = _imageService.CreateImageGrid(
                    ImageFileNames.Select(img => img.FullPath),
                    Columns
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update displayed image grid: {ex.Message}");
            }
        }
        private void MoveUp()
        {
            if (SelectedImage == null) return;
            var idx = ImageFileNames.IndexOf(SelectedImage);
            if (idx > 0) ImageFileNames.Move(idx, idx - 1);
        }

        private void MoveDown()
        {
            if (SelectedImage == null) return;
            var idx = ImageFileNames.IndexOf(SelectedImage);
            if (idx < ImageFileNames.Count - 1) ImageFileNames.Move(idx, idx + 1);
        }

        private void Remove()
        {
            if (SelectedImage == null) return;
            var idx = ImageFileNames.IndexOf(SelectedImage);
            ImageFileNames.Remove(SelectedImage);
            SelectedImage = ImageFileNames.Count > 0
                ? ImageFileNames[Math.Min(idx, ImageFileNames.Count - 1)]
                : null;
        }

        private void RaiseCommandStates()
        {
            MoveUpCommand.RaiseCanExecuteChanged();
            MoveDownCommand.RaiseCanExecuteChanged();
            RemoveCommand.RaiseCanExecuteChanged();
        }
    }
}
