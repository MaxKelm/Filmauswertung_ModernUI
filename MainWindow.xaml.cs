using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.MVVM.ViewModel;

namespace Filmauswertung_ModernUI
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IWindowService
    {
        public MainWindow()
        {
            InitializeComponent();

            // Inject this window into the ViewModel
            if (DataContext is MainViewModel vm)
                vm.WindowService = this;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove(); // Built-in method to allow window dragging
        }

        public void Close()
        {
            Application.Current.Shutdown();
        }
        public void Minimize()
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
