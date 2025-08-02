using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;

namespace Filmauswertung_ModernUI.Core.Interfaces
{
    public interface IImageService
    {
        BitmapImage LoadImage(string path);
        void SaveRoi(BitmapSource image, Rect roi, string savePath);
    }
    public interface IImageProcessingService
    {
        BitmapImage AdjustContrast(BitmapImage sourceImage, int contrastLevel);
    }
}
