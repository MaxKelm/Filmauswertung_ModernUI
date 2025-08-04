using Filmauswertung_ModernUI.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels
{
    internal class GlueViewModel : ObservableObject
    {
        private ObservableCollection<string> _imageFileNames = new ObservableCollection<string>();
        public ObservableCollection<string> ImageFileNames
        {
            get => _imageFileNames;
            set
            {
                _imageFileNames = value;
                OnPropertyChanged(nameof(ImageFileNames));
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

        private string _selectedImage;
        public string SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                OnPropertyChanged(nameof(SelectedImage));
                LoadImage(value);
                RaiseCommandStates();
            }
        }

        private ImageSource _displayedImage;
        public ImageSource DisplayedImage
        {
            get => _displayedImage;
            set
            {
                _displayedImage = value;
                OnPropertyChanged(nameof(DisplayedImage));
            }
        }

        private int _columns = 1;
        public int Columns
        {
            get => _columns;
            set
            {
                _columns = value;
                OnPropertyChanged(nameof(Columns));
            }
        }

        private int _rows = 1;
        public int Rows
        {
            get => _rows;
            set
            {
                _rows = value;
                OnPropertyChanged(nameof(Rows));
            }
        }

        public int ToleranceMax => 10; // Used by sliders

        public ICommand ImportCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand RemoveCommand { get; }

        public GlueViewModel()
        {
            ImportCommand = new RelayCommand(_ => ImportImages());
            SaveCommand = new RelayCommand(_ => SaveImage());
            MoveUpCommand = new RelayCommand(_ => MoveUp(), _ => SelectedImage != null);
            MoveDownCommand = new RelayCommand(_ => MoveDown(), _ => SelectedImage != null);
            RemoveCommand = new RelayCommand(_ => Remove(), _ => SelectedImage != null);

            ImageFileNames = new ObservableCollection<string>();
            ImageFileNamesView = CollectionViewSource.GetDefaultView(ImageFileNames);
        }

        private void ImportImages()
        {
            ImageFileNames.Clear();
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");
            ImageFileNames.Add("Zebra.tif");
            ImageFileNames.Add("Apple.tif");
            ImageFileNames.Add("Monkey.tif");

            SelectedImage = ImageFileNames.FirstOrDefault();
        }

        private void SaveImage()
        {
            Debug.WriteLine("SaveCommand executed.");
        }

        private void LoadImage(string fileName)
        {
            Debug.WriteLine($"Dummy Load Image for {fileName}");
            // Placeholder image logic
        }

        private void MoveUp()
        {
            if (SelectedImage == null) return;

            int index = ImageFileNames.IndexOf(SelectedImage);
            if (index > 0)
            {
                ImageFileNames.Move(index, index - 1);
            }
        }

        private void MoveDown()
        {
            if (SelectedImage == null) return;

            int index = ImageFileNames.IndexOf(SelectedImage);
            if (index < ImageFileNames.Count - 1)
            {
                ImageFileNames.Move(index, index + 1);
            }
        }

        private void Remove()
        {
            if (SelectedImage == null) return;

            int index = ImageFileNames.IndexOf(SelectedImage);
            ImageFileNames.Remove(SelectedImage);

            if (ImageFileNames.Count > 0)
                SelectedImage = ImageFileNames[Math.Min(index, ImageFileNames.Count - 1)];
            else
                SelectedImage = null;
        }

        private void RaiseCommandStates()
        {
            (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
