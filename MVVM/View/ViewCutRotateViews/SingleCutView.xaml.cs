using Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Filmauswertung_ModernUI.MVVM.View.ViewCutRotateViews
{
    public partial class SingleCutView : UserControl
    {
        private Point _startPoint;
        private Rectangle _roiRect;
        private bool _isDrawing;

        public SingleCutView()
        {
            InitializeComponent();
            _roiRect = RoiRectangle;

            MainImage.MouseLeftButtonDown += OnMouseLeftButtonDown;
            MainImage.MouseMove += OnMouseMove;
            MainImage.MouseLeftButtonUp += OnMouseLeftButtonUp;

            this.DataContextChanged += OnDataContextChanged;
        }


        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is SingleCutViewModel vm && vm.IsDrawingRoi)
            {
                _startPoint = e.GetPosition(MainImage);
                Canvas.SetLeft(_roiRect, _startPoint.X);
                Canvas.SetTop(_roiRect, _startPoint.Y);
                _roiRect.Width = 0;
                _roiRect.Height = 0;
                _roiRect.Visibility = Visibility.Visible;
                _isDrawing = true;

                Mouse.OverrideCursor = Cursors.Cross;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                Point pos = e.GetPosition(MainImage);
                double x = Math.Min(pos.X, _startPoint.X);
                double y = Math.Min(pos.Y, _startPoint.Y);
                double w = Math.Abs(pos.X - _startPoint.X);
                double h = Math.Abs(pos.Y - _startPoint.Y);

                Canvas.SetLeft(_roiRect, x);
                Canvas.SetTop(_roiRect, y);
                _roiRect.Width = w;
                _roiRect.Height = h;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing && DataContext is SingleCutViewModel vm)
            {
                _isDrawing = false;
                Mouse.OverrideCursor = null;

                Point endPoint = e.GetPosition(MainImage);
                vm.SetRoi(_startPoint, endPoint);

                // Only show if rectangle is valid
                if (vm.RoiRect.Width > 0 && vm.RoiRect.Height > 0)
                {
                    _roiRect.Visibility = Visibility.Visible;
                }
                else
                {
                    _roiRect.Visibility = Visibility.Collapsed;
                }
            }
        }


        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SingleCutViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is SingleCutViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SingleCutViewModel.RoiRect))
            {
                if (DataContext is SingleCutViewModel vm)
                {
                    if (vm.RoiRect.IsEmpty)
                    {
                        _roiRect.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

    }
}
