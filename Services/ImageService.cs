using Filmauswertung_ModernUI.Core.Interfaces;
using Filmauswertung_ModernUI.MVVM.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            image.Freeze();

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

        public void SaveImage(BitmapSource image, string savePath)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            var encoder = new TiffBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        public void SaveSegmentCroppedImage(SegmentResult segment, string savePath)
        {
            if (segment == null || segment.CroppedPixelData == null)
                throw new ArgumentNullException(nameof(segment), "Segment or cropped pixels are null.");

            int width = segment.CropWidth;
            int height = segment.CropHeight;
            int stride = width * 4;

            var bitmap = BitmapSource.Create(
                width,
                height,
                96, 96,
                PixelFormats.Bgra32,
                null,
                segment.CroppedPixelData,
                stride);

            var encoder = new TiffBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        public BitmapImage ConvertToBitmapImage(BitmapSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
        }

        /// <summary>
        /// Composes a grid of images from file paths.
        /// </summary>
        /// <param name="imagePaths">List of full image paths.</param>
        /// <param name="columns">Number of columns in the grid.</param>
        /// <param name="cellWidth">Width of each image cell.</param>
        /// <param name="cellHeight">Height of each image cell.</param>
        /// <returns>A DrawingImage composed as a grid.</returns>
        public DrawingImage CreateImageGrid(IEnumerable<string> imagePaths, int columns, int cellWidth = 200, int cellHeight = 200)
        {
            if (imagePaths == null)
                throw new ArgumentNullException(nameof(imagePaths));
            if (columns <= 0)
                throw new ArgumentException("Columns must be greater than zero.");

            var group = new DrawingGroup();
            var paths = new List<string>(imagePaths);

            for (int i = 0; i < paths.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;

                var imageSource = LoadImage(paths[i]);
                if (imageSource != null)
                {
                    var rect = new Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                    var drawing = new ImageDrawing(imageSource, rect);
                    group.Children.Add(drawing);
                }
            }

            return new DrawingImage(group);
        }
    }
}
