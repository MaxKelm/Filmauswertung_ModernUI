using Filmauswertung_ModernUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class MainViewModel : ObservableObject
    {

        public RelayCommand MoveWindowCommand { get; set; }
        public RelayCommand ShutdownWindowCommand { get; set; }
        public RelayCommand MaximizeWindowCommand { get; set; }
        public RelayCommand MinimizeWindowCommand { get; set; }

        public RelayCommand ViewCutRotateCommand { get; set; }
        public RelayCommand ViewDoseViewerCommand { get; set; }
        public RelayCommand ViewCalibrationCommand { get; set; }
        public RelayCommand ViewCalculationCommand { get; set; }



        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }

        public ViewCutRotateViewModel ViewCutRotateVM { get; set; }
        public DoseViewerViewModel DoseViewerVM { get; set; }
        public CalculateConvertViewModel CalculateConvertVM { get; set; }
        public CalibrationViewModel CalibrationVM { get; set; }

        public MainViewModel()
        {
            ViewCutRotateVM = new ViewCutRotateViewModel();
            DoseViewerVM = new DoseViewerViewModel();
            CalibrationVM = new CalibrationViewModel();
            CalculateConvertVM = new CalculateConvertViewModel();

            CurrentView = CalculateConvertVM;

            /* View Commands*/
            ViewCutRotateCommand = new RelayCommand(o =>
            {
                CurrentView = ViewCutRotateVM;
            });
            ViewDoseViewerCommand = new RelayCommand(o =>
            {
                CurrentView = DoseViewerVM;
            });
            ViewCalibrationCommand = new RelayCommand(o =>
            {
                CurrentView = CalibrationVM;
            });
            ViewCalculationCommand = new RelayCommand(o =>
            {
                CurrentView = CalculateConvertVM;
            });

            /* Control UI Elements*/
            MoveWindowCommand = new RelayCommand(o =>
            {
                var window = Application.Current.MainWindow;
                if (window == null) return;

                if (window.WindowState == WindowState.Maximized)
                {
                    window.WindowState = WindowState.Normal;
                    window.DragMove();
                }
                else
                {
                    window.DragMove();
                }
            });

            ShutdownWindowCommand = new RelayCommand(o => Application.Current.Shutdown());
            MaximizeWindowCommand = new RelayCommand(o =>             {
                if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                else
                    Application.Current.MainWindow.WindowState = WindowState.Maximized;
            });
            MinimizeWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.WindowState = WindowState.Minimized; });

        }
    }
}
