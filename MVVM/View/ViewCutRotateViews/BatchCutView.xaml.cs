using Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Specialized;


namespace Filmauswertung_ModernUI.MVVM.View.ViewCutRotateViews
{
    /// <summary>
    /// Interaktionslogik für BatchCutView.xaml
    /// </summary>
    public partial class BatchCutView : UserControl
    {
        private readonly List<Point> clickedPoints = new List<Point>();

        public BatchCutView()
        {
            InitializeComponent();
            this.Loaded += BatchCutView_Loaded;
        }

        private void TifImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is BatchCutViewModel vm && vm.IsMarkerModeEnabled)
            {
                Point clickPoint = e.GetPosition(TifImage);

                clickedPoints.Add(clickPoint);
                vm.AddMarker(clickPoint);

                DrawMarker(clickPoint);
            }
        }

        private void DrawMarker(Point point)
        {
            var marker = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Red,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(marker, point.X - 5);
            Canvas.SetTop(marker, point.Y - 5);

            ClickCanvas.Children.Add(marker);
        }

        private void RemoveLastClick_Click(object sender, RoutedEventArgs e)
        {
            if (clickedPoints.Count == 0) return;

            clickedPoints.RemoveAt(clickedPoints.Count - 1);
            ClickCanvas.Children.RemoveAt(ClickCanvas.Children.Count - 1);

            if (DataContext is BatchCutViewModel vm && vm.MarkerPoints.Count > 0)
                vm.RemoveLastMarker();
        }

        private void ClearClicksButton_Click(object sender, RoutedEventArgs e)
        {
            clickedPoints.Clear();
            ClickCanvas.Children.Clear();

            if (DataContext is BatchCutViewModel vm)
                vm.MarkerPoints.Clear();
        }
        private void BatchCutView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BatchCutViewModel vm)
            {
                vm.MarkerPoints.CollectionChanged += MarkerPoints_CollectionChanged;
            }
        }
        private void MarkerPoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var vm = DataContext as BatchCutViewModel;
            if (vm == null)
                return;

            // Full sync approach — clear and redraw all
            ClickCanvas.Children.Clear();
            clickedPoints.Clear();

            foreach (var point in vm.MarkerPoints)
            {
                clickedPoints.Add(point);
                DrawMarker(point);
            }
        }

    }
}
