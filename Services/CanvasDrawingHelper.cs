using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Filmauswertung_ModernUI.Services
{
    public static class CanvasDrawingHelper
    {
        public static Ellipse DrawMarker(Canvas canvas, Point point)
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
            canvas.Children.Add(marker);
            return marker;
        }

        public static Rectangle DrawBoundingBox(Canvas canvas, Rect box)
        {
            var rectangle = new Rectangle
            {
                Width = box.Width,
                Height = box.Height,
                Stroke = Brushes.Green,
                StrokeThickness = 4,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(60, 0, 128, 0)),
                IsHitTestVisible = false,
                Tag = "SegBox"
            };

            Canvas.SetLeft(rectangle, box.X);
            Canvas.SetTop(rectangle, box.Y);
            canvas.Children.Add(rectangle);
            return rectangle;
        }
    }
}
