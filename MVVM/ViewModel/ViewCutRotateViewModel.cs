using Filmauswertung_ModernUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class ViewCutRotateViewModel : ObservableObject
    {
        public RelayCommand SelectViewCommand { get; set; }
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
            ViewVM = new ViewCutRotateViewModels.ViewViewModel();
            CutVM = new ViewCutRotateViewModels.CutViewModel();
            RotateVM = new ViewCutRotateViewModels.RotateViewModel();
            SelectedContentView = CutVM;

            SelectViewCommand = new RelayCommand(_ => SelectedContentView = ViewVM);
            SelectCutCommand = new RelayCommand(_ => SelectedContentView = CutVM);
            SelectRotateCommand = new RelayCommand(_ => SelectedContentView = RotateVM);
        }
    }
}