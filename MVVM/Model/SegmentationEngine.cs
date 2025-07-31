using Filmauswertung_ModernUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.MVVM.Model
{
    public class SegmentResult
    {
        public Point OriginMarker { get; set; }
        public Rect BoundingBox { get; set; }
        public byte[] CroppedPixelData { get; set; }
        public int CropWidth { get; set; }
        public int CropHeight { get; set; }
        public string SuggestedFileName { get; set; }
    }

    public class SegmentationEngine
    {
        private readonly ImageSegmentationService _service = new ImageSegmentationService();

        public SegmentResult SegmentRegion(BitmapSource imageSource, Point marker)
        {
            if (imageSource == null) return null;

            // Ensure pixel format is Bgra32 for easy pixel manipulation
            BitmapSource bmp = imageSource.Format == PixelFormats.Bgra32
                ? imageSource
                : new FormatConvertedBitmap(imageSource, PixelFormats.Bgra32, null, 0);

            int width = bmp.PixelWidth;
            int height = bmp.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            bmp.CopyPixels(pixels, stride, 0);

            int centerX = (int)Math.Round(marker.X);
            int centerY = (int)Math.Round(marker.Y);

            var region = _service.Get10x10RegionPixels(pixels, width, height, stride, centerX, centerY);
            if (region.Count == 0)
                return null;

            byte median = _service.CalculateMedian(region);
            var mask = _service.RegionGrow(pixels, width, height, stride, centerX, centerY, median);
            var box = _service.GetBoundingBox(mask, width, height);

            if (box.IsEmpty)
                return null;

            // Extract pixel data of the bounding box (cropped region)
            int cropX = (int)box.X;
            int cropY = (int)box.Y;
            int cropWidth = (int)box.Width;
            int cropHeight = (int)box.Height;
            int cropStride = cropWidth * 4;
            byte[] croppedPixels = new byte[cropHeight * cropStride];

            for (int row = 0; row < cropHeight; row++)
            {
                int sourceIndex = (cropY + row) * stride + cropX * 4;
                int destIndex = row * cropStride;
                Array.Copy(pixels, sourceIndex, croppedPixels, destIndex, cropStride);
            }

            return new SegmentResult
            {
                OriginMarker = marker,
                BoundingBox = box,
                CroppedPixelData = croppedPixels,
                CropWidth = cropWidth,
                CropHeight = cropHeight,
                SuggestedFileName = $"ROI_{cropX}_{cropY}_{cropWidth}x{cropHeight}.tif"
            };
        }
        public Rect InflateAndClampRect(Rect rect, double marginFactor, int imageWidth, int imageHeight)
        {
            double marginWidth = rect.Width * marginFactor;
            double marginHeight = rect.Height * marginFactor;

            double x = rect.X - marginWidth / 2;
            double y = rect.Y - marginHeight / 2;
            double width = rect.Width + marginWidth;
            double height = rect.Height + marginHeight;

            // Clamp to image bounds
            x = Math.Max(0, x);
            y = Math.Max(0, y);
            width = Math.Min(width, imageWidth - x);
            height = Math.Min(height, imageHeight - y);

            return new Rect(x, y, width, height);
        }
    }
}
