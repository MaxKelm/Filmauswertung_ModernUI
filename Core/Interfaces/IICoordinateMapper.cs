using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.Core.Interfaces
{
    public interface ICoordinateMapper
    {
        Point VisualToPixel(Point visualPoint, FrameworkElement visualElement, BitmapSource bitmap);
        Rect VisualToPixel(Rect visualRect, FrameworkElement visualElement, BitmapSource bitmap);
        Rect PixelToVisual(Rect pixelRect, FrameworkElement visualElement, BitmapSource bitmap);
    }

}
