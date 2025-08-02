// ImageProcessingService.cs
using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Filmauswertung_ModernUI.Core.Interfaces;

namespace Filmauswertung_ModernUI.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        public BitmapImage AdjustContrast(BitmapImage sourceImage, int contrastLevel)
        {
            if (sourceImage == null)
                throw new ArgumentNullException(nameof(sourceImage));

            // contrastLevel from 1 (low) to 4 (high)
            // Map contrastLevel to contrast factor (example: 1->0.8, 2->1.0, 3->1.2, 4->1.4)
            double contrastFactor = 0.6 + 0.2 * contrastLevel;

            // Convert BitmapImage to WriteableBitmap for pixel manipulation
            var writableBitmap = new WriteableBitmap(sourceImage);

            int width = writableBitmap.PixelWidth;
            int height = writableBitmap.PixelHeight;
            int stride = width * (writableBitmap.Format.BitsPerPixel / 8);
            byte[] pixelData = new byte[height * stride];
            writableBitmap.CopyPixels(pixelData, stride, 0);

            // Adjust contrast per pixel
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                // Pixels in BGRA order
                byte b = pixelData[i];
                byte g = pixelData[i + 1];
                byte r = pixelData[i + 2];
                byte a = pixelData[i + 3];

                pixelData[i] = AdjustContrastValue(b, contrastFactor);
                pixelData[i + 1] = AdjustContrastValue(g, contrastFactor);
                pixelData[i + 2] = AdjustContrastValue(r, contrastFactor);
                pixelData[i + 3] = a; // Alpha remains the same
            }

            var contrastedBitmap = new WriteableBitmap(width, height, writableBitmap.DpiX, writableBitmap.DpiY, writableBitmap.Format, null);
            contrastedBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
            // Convert back to BitmapImage
            //return ConvertWriteableBitmapToBitmapImage(contrastedBitmap);
            return sourceImage;
        }

        private byte AdjustContrastValue(byte colorValue, double contrastFactor)
        {
            double color = colorValue / 255.0;
            color -= 0.5;
            color *= contrastFactor;
            color += 0.5;
            color = Clamp(color, 0, 1);
            return (byte)(color * 255);
        }

        private BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wBitmap)
        {
            // Create a new WriteableBitmap copy (detached from original stream)
            var copiedBitmap = new WriteableBitmap(wBitmap);

            // Freeze for thread safety
            copiedBitmap.Freeze();

            // Convert WriteableBitmap directly to BitmapImage is tricky, 
            // but often you can just return BitmapSource or WriteableBitmap instead of BitmapImage

            // If BitmapImage is required, fallback to encoding method but with async
            return ConvertWriteableBitmapToBitmapImageViaEncoder(copiedBitmap);
        }

        private BitmapImage ConvertWriteableBitmapToBitmapImageViaEncoder(WriteableBitmap wBitmap)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wBitmap));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }


        private double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}

