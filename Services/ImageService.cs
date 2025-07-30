using Filmauswertung_ModernUI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;

namespace Filmauswertung_ModernUI.Services
{
    public class ImageService : IImageService
    {
        public BitmapImage LoadImage(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Image file not found", path);

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze(); // Safe for cross-thread access

            return image;
        }

        public void SaveRoi(BitmapSource image, Rect roi, string savePath)
        {
            if (image == null || roi.IsEmpty)
                throw new ArgumentException("Invalid image or ROI.");

            double scale = image.PixelWidth / image.Width;

            var cropRect = new Int32Rect(
                (int)(roi.X * scale),
                (int)(roi.Y * scale),
                (int)(roi.Width * scale),
                (int)(roi.Height * scale));

            var croppedBitmap = new CroppedBitmap(image, cropRect);
            var encoder = new TiffBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
    }
}
