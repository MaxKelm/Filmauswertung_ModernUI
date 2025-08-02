using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
        #region Fields
        private Ellipse _previousMarker = null;

        private bool _isDrawingRoi = false;
        private Point _roiStartPoint;
        private readonly Rectangle _currentRoiRect;

        #endregion

        #region Constructor
        public CutView()
        {
            InitializeComponent();
            _currentRoiRect = new Rectangle();

            Loaded += CutView_Loaded;

            TifImage.MouseLeftButtonDown += OnImageMouseLeftButtonDown;
            TifImage.MouseRightButtonDown += OnImageMouseRightButtonDown;
            TifImage.MouseMove += OnImageMouseMove;
            TifImage.MouseLeftButtonUp += OnImageMouseLeftButtonUp;
        }
        #endregion

        #region Initialization

        private void CutView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CutViewModel vm)
            {
                vm.MarkersChanged += Markers_CollectionChanged;
                vm.SegmentsChanged += Segments_CollectionChanged;
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        }
        #endregion

        #region ViewModel Event Handlers

        private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!(DataContext is CutViewModel vm)) return;

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
            {
                var newMarker = e.NewItems[0] as Marker;
                AddMarkerEllipse(newMarker.Position);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                RemoveLastMarkerEllipse();
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearAllMarkers();
            }
        }

        private void Segments_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!(DataContext is CutViewModel vm)) return;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Segment seg in e.NewItems)
                    AddSegmentRectangle(seg.BoundingBox);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                // Remove rectangles corresponding to removed segments if necessary
                RefreshSegmentRectangles();
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearAllSegments();
            }
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CutViewModel.LastMarkerPoint))
            {
                // Optionally update UI or selection state based on LastMarkerPoint
            }
        }

        #endregion

        #region Marker Management

        private void AddMarkerEllipse(Point position)
        {
            var ellipse = new Ellipse
            {
                Fill = Brushes.Red,
                Width = 10,
                Height = 10,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(ellipse, position.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, position.Y - ellipse.Height / 2);

            MarkerCanvas.Children.Add(ellipse);
            _previousMarker = ellipse;
        }

        private void RemoveLastMarkerEllipse()
        {
            if (_previousMarker != null)
            {
                MarkerCanvas.Children.Remove(_previousMarker);
                _previousMarker = null;
            }
        }

        private void ClearAllMarkers()
        {
            MarkerCanvas.Children.Clear();
            _previousMarker = null;
        }

        #endregion

        #region Segment Management

        private void AddSegmentRectangle(Rect rect)
        {
            var rectangle = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                Width = rect.Width,
                Height = rect.Height
            };

            Canvas.SetLeft(rectangle, rect.Left);
            Canvas.SetTop(rectangle, rect.Top);

            RoiCanvas.Children.Add(rectangle);
        }

        private void RefreshSegmentRectangles()
        {
            RoiCanvas.Children.Clear();
            if (DataContext is CutViewModel vm)
            {
                foreach (var seg in vm.Segments)
                {
                    AddSegmentRectangle(seg.BoundingBox);
                }
            }
        }

        private void ClearAllSegments()
        {
            RoiCanvas.Children.Clear();
        }

        #endregion

        #region ROI Drawing

        private void OnImageMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(DataContext is CutViewModel vm))
                return;

            var clickPoint = e.GetPosition(TifImage);

            if (vm.IsBatchCutMode)
            {
                // In batch mode, add marker automatically instead of manual drawing
                vm.AddMarker(clickPoint, TifImage);
                return; // Skip manual ROI drawing
            }

            // Manual ROI drawing mode
            _roiStartPoint = clickPoint;
            _isDrawingRoi = true;

            _currentRoiRect.Width = 0;
            _currentRoiRect.Height = 0;
            _currentRoiRect.Stroke = Brushes.Green;
            _currentRoiRect.StrokeThickness = 2;

            if (!RoiCanvas.Children.Contains(_currentRoiRect))
                RoiCanvas.Children.Add(_currentRoiRect);
        }


        private void OnImageMouseMove(object sender, MouseEventArgs e)
        {
            if (IsBatchCutModeEnabled() || !_isDrawingRoi) return;

            Point currentPoint = e.GetPosition(TifImage);

            double x = Math.Min(currentPoint.X, _roiStartPoint.X);
            double y = Math.Min(currentPoint.Y, _roiStartPoint.Y);
            double width = Math.Abs(currentPoint.X - _roiStartPoint.X);
            double height = Math.Abs(currentPoint.Y - _roiStartPoint.Y);

            _currentRoiRect.Width = width;
            _currentRoiRect.Height = height;

            Canvas.SetLeft(_currentRoiRect, x);
            Canvas.SetTop(_currentRoiRect, y);
        }

        private void OnImageMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsBatchCutModeEnabled() || !_isDrawingRoi) return;

            _isDrawingRoi = false;

            if (DataContext is CutViewModel vm)
            {
                var rect = new Rect(Canvas.GetLeft(_currentRoiRect), Canvas.GetTop(_currentRoiRect), _currentRoiRect.Width, _currentRoiRect.Height);
                vm.AddSegmentBox(rect);
            }

            RoiCanvas.Children.Remove(_currentRoiRect);
            _currentRoiRect.Width = 0;
            _currentRoiRect.Height = 0;
        }


        private void OnImageMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is CutViewModel vm)
            {
                vm.RemoveLastMarkerCommand.Execute(null);
            }
        }

        #endregion
        #region Helper Methods
        private bool IsBatchCutModeEnabled()
        {
            if (DataContext is CutViewModel vm)
                return vm.IsBatchCutMode;
            return false;
        }
        #endregion
    }
}
