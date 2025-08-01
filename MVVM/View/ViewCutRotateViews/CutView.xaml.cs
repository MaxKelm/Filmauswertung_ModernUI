using Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Filmauswertung_ModernUI.MVVM.View.ViewCutRotateViews
{
    public partial class CutView : UserControl
    {
        private Ellipse _previousMarker = null;

        // ROI drawing state for single mode
        private bool _isDrawingRoi = false;
        private Point _roiStartPoint;
        private Rectangle _currentRoiRect;

        public CutView()
        {
            InitializeComponent();
            Loaded += CutView_Loaded;

            // Mouse event handlers for ROI drawing & marker placement
            TifImage.MouseLeftButtonDown += TifImage_MouseLeftButtonDown;
            TifImage.MouseMove += TifImage_MouseMove;
            TifImage.MouseLeftButtonUp += TifImage_MouseLeftButtonUp;

            // Prepare the ROI rectangle, hidden by default
            _currentRoiRect = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(60, 0, 0, 255)),
                Visibility = Visibility.Collapsed,
                Tag = "SegBox"
            };
            ClickCanvas.Children.Add(_currentRoiRect);
        }

        private void CutView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CutViewModel vm)
            {
                ((INotifyCollectionChanged)vm.Segments).CollectionChanged += SegmentBoxes_CollectionChanged;
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        }


        private void SegmentBoxes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DataContext is CutViewModel vm)
            {
                // Clear old rectangles
                var toRemove = new List<UIElement>();
                foreach (UIElement child in ClickCanvas.Children)
                {
                    if (child is Rectangle rect && rect.Tag?.ToString() == "SegBox" && rect != _currentRoiRect)
                    {
                        toRemove.Add(child);
                    }
                }
                foreach (var el in toRemove)
                    ClickCanvas.Children.Remove(el);

                // Draw bounding boxes from Segments
                foreach (var segment in vm.Segments)
                {
                    DrawBoundingBox(segment.BoundingBox);
                }
            }
        }


        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CutViewModel.LastMarkerPoint))
            {
                // Remove previous marker if exists
                if (_previousMarker != null)
                {
                    Dispatcher.Invoke(() => ClickCanvas.Children.Remove(_previousMarker));
                    _previousMarker = null;
                }
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
            _previousMarker = marker;
        }

        private void DrawBoundingBox(Rect box)
        {
            var rectangle = new Rectangle
            {
                Width = box.Width,
                Height = box.Height,
                Stroke = Brushes.Green,
                StrokeThickness = 4,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(60, 0, 128, 0)),
                Tag = "SegBox",
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rectangle, box.X);
            Canvas.SetTop(rectangle, box.Y);

            ClickCanvas.Children.Add(rectangle);
        }

        private void TifImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is CutViewModel vm)
            {
                Point clickPoint = e.GetPosition(TifImage);

                if (vm.IsBatchCutMode)
                {
                    // Remove previous marker, add new marker
                    if (_previousMarker != null)
                        ClickCanvas.Children.Remove(_previousMarker);

                    vm.AddMarker(clickPoint);
                    DrawMarker(clickPoint);
                }
                else
                {
                    // Single mode: start ROI drawing
                    _isDrawingRoi = true;
                    _roiStartPoint = clickPoint;

                    _currentRoiRect.Width = 0;
                    _currentRoiRect.Height = 0;
                    Canvas.SetLeft(_currentRoiRect, _roiStartPoint.X);
                    Canvas.SetTop(_currentRoiRect, _roiStartPoint.Y);
                    _currentRoiRect.Visibility = Visibility.Visible;

                    Mouse.OverrideCursor = Cursors.Cross;

                    TifImage.CaptureMouse();
                }
            }
        }

        private void TifImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawingRoi && DataContext is CutViewModel)
            {
                Point currentPoint = e.GetPosition(TifImage);

                double x = Math.Min(currentPoint.X, _roiStartPoint.X);
                double y = Math.Min(currentPoint.Y, _roiStartPoint.Y);
                double width = Math.Abs(currentPoint.X - _roiStartPoint.X);
                double height = Math.Abs(currentPoint.Y - _roiStartPoint.Y);

                Canvas.SetLeft(_currentRoiRect, x);
                Canvas.SetTop(_currentRoiRect, y);
                _currentRoiRect.Width = width;
                _currentRoiRect.Height = height;
            }
        }

        private void TifImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingRoi && DataContext is CutViewModel vm && !vm.IsBatchCutMode)
            {
                _isDrawingRoi = false;
                Mouse.OverrideCursor = null;
                TifImage.ReleaseMouseCapture();

                Point endPoint = e.GetPosition(TifImage);

                double x = Math.Min(endPoint.X, _roiStartPoint.X);
                double y = Math.Min(endPoint.Y, _roiStartPoint.Y);
                double width = Math.Abs(endPoint.X - _roiStartPoint.X);
                double height = Math.Abs(endPoint.Y - _roiStartPoint.Y);

                if (width > 0 && height > 0)
                {
                    Rect newRect = new Rect(x, y, width, height);
                    vm.AddSegmentBox(newRect);
                }

                _currentRoiRect.Visibility = Visibility.Collapsed;
            }
        }
    }
}
