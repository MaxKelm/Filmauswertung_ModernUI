using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Filmauswertung_ModernUI.Core.Interfaces;

namespace Filmauswertung_ModernUI.Services
{
    public class CoordinateMapper : ICoordinateMapper
    {
        public Point VisualToPixel(Point visual, FrameworkElement visualElement, BitmapSource bitmap)
        {
            if (bitmap == null || visualElement == null) return visual;

            double scaleX = bitmap.PixelWidth / visualElement.ActualWidth;
            double scaleY = bitmap.PixelHeight / visualElement.ActualHeight;

            return new Point(visual.X * scaleX, visual.Y * scaleY);
        }

        public Rect VisualToPixel(Rect visualRect, FrameworkElement visualElement, BitmapSource bitmap)
        {
            if (bitmap == null || visualElement == null) return visualRect;

            double scaleX = bitmap.PixelWidth / visualElement.ActualWidth;
            double scaleY = bitmap.PixelHeight / visualElement.ActualHeight;

            return new Rect(
                visualRect.X * scaleX,
                visualRect.Y * scaleY,
                visualRect.Width * scaleX,
                visualRect.Height * scaleY);
        }

        public Rect PixelToVisual(Rect pixelRect, FrameworkElement visualElement, BitmapSource bitmap)
        {
            if (bitmap == null || visualElement == null) return pixelRect;

            double scaleX = visualElement.ActualWidth / bitmap.PixelWidth;
            double scaleY = visualElement.ActualHeight / bitmap.PixelHeight;

            return new Rect(
                pixelRect.X * scaleX,
                pixelRect.Y * scaleY,
                pixelRect.Width * scaleX,
                pixelRect.Height * scaleY);
        }
    }

}
