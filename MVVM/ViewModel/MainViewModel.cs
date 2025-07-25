using Filmauswertung_ModernUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class MainViewModel : ObservableObject
    {
        public  RelayCommand  HomeViewCommand { get; set; }
        public RelayCommand DiscoveryViewCommand { get; set; }
        public HomeViewModel HomeVM { get; set; }
        public DiscoveryViewModel DiscoveryVM { get; set; }
        public IWindowService WindowService { get; set; }
        public ICommand CloseCommand { get; }
        public ICommand MinimizeCommand { get; }

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

        public MainViewModel()
        {
            HomeVM = new HomeViewModel();
            DiscoveryVM = new DiscoveryViewModel();
            CurrentView = HomeVM;
            HomeViewCommand = new RelayCommand(o =>
            {
                CurrentView = HomeVM;
            });
            DiscoveryViewCommand = new RelayCommand(o =>
            {
                CurrentView = DiscoveryVM;
            });
            CloseCommand = new RelayCommand(_ => WindowService?.Close());
            MinimizeCommand = new RelayCommand(_ => WindowService?.Minimize());
        }
    }
}
