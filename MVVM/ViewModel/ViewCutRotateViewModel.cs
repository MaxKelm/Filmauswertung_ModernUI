using Filmauswertung_ModernUI.Core;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class ViewCutRotateViewModel : ObservableObject
    {
        public RelayCommand SelectCutCommand { get; set; }
        public RelayCommand SelectRotateCommand { get; set; }
        public RelayCommand SelectGlueCommand { get; set; }

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

        public object GlueVM { get; set; }
        public object CutVM { get; set; }
        public object RotateVM { get; set; }

        public ViewCutRotateViewModel()
        {
            CutVM = new ViewCutRotateViewModels.CutViewModel();
            GlueVM = new ViewCutRotateViewModels.GlueViewModel();
            RotateVM = new ViewCutRotateViewModels.RotateViewModel();
            SelectedContentView = CutVM;

            SelectCutCommand = new RelayCommand(_ => SelectedContentView = CutVM);
            SelectRotateCommand = new RelayCommand(_ => SelectedContentView = RotateVM);
            SelectGlueCommand = new RelayCommand(_ => SelectedContentView = GlueVM);
        }
    }
}