using Filmauswertung_ModernUI.Core;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class ViewCutRotateViewModel : ObservableObject
    {
        public RelayCommand SelectCutCommand { get; set; }
        public RelayCommand SelectRotateCommand { get; set; }

        private object _selectedContentView;
        public object SelectedContentView
        {
            get { return _selectedContentView; }
            set
            {
                _selectedContentView = value;
                OnPropertyChanged(nameof(SelectedContentView));
            }
        }

        public object ViewVM { get; set; }
        public object CutVM { get; set; }
        public object RotateVM { get; set; }

        public ViewCutRotateViewModel()
        {
            CutVM = new ViewCutRotateViewModels.CutViewModel();
            RotateVM = new ViewCutRotateViewModels.RotateViewModel();
            SelectedContentView = CutVM;

            SelectCutCommand = new RelayCommand(_ => SelectedContentView = CutVM);
            SelectRotateCommand = new RelayCommand(_ => SelectedContentView = RotateVM);
        }
    }
}