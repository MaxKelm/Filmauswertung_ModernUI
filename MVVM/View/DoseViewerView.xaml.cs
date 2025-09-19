using Filmauswertung_ModernUI.MVVM.ViewModel;
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

namespace Filmauswertung_ModernUI.MVVM.View
{
    /// <summary>
    /// Interaktionslogik für _3DDoseViewerView.xaml
    /// </summary>
    public partial class DoseViewerView : UserControl
    {
        public DoseViewerView()
        {
            InitializeComponent();
            var vm = (DoseViewerViewModel)DataContext;

            // Assign the zoom action
            vm.ZoomToFitAction = () => Dispatcher.Invoke(() => PlotViewport.ZoomExtents());
            vm.Viewport = PlotViewport;

        }
    }
}
