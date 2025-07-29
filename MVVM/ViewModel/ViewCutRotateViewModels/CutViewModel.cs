using Filmauswertung_ModernUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels
{
    internal class CutViewModel : ObservableObject
    {
        public RelayCommand SelectSingleCutCommand { get; set; }
        public RelayCommand SelectBatchCutCommand { get; set; }
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
        public object SingleCutVM { get; set; }
        public object BatchCutVM { get; set; }
        public CutViewModel()
        {
            SingleCutVM = new SingleCutViewModel();
            BatchCutVM = new BatchCutViewModel();
            SelectedContentView = BatchCutVM;
            SelectSingleCutCommand = new RelayCommand(_ => SelectedContentView = SingleCutVM);
            SelectBatchCutCommand = new RelayCommand(_ => SelectedContentView = BatchCutVM);
        }
    }
}
