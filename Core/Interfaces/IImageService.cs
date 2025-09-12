using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Filmauswertung_ModernUI.MVVM.Model;

namespace Filmauswertung_ModernUI.Core.Interfaces
{
    /// <summary>
    /// Service for loading, saving, and converting images.
    /// </summary>
    public interface IImageService
    {
        /// <summary>Loads an image from the specified file path.</summary>
        BitmapImage LoadImage(string path);

        /// <summary>Saves a region of interest (ROI) of an image to disk.</summary>
        void SaveRoi(BitmapSource image, Rect roi, string savePath);

        /// <summary>Saves an entire BitmapSource image to disk.</summary>
        void SaveImage(BitmapSource image, string savePath);

        /// <summary>Saves a cropped segment from a SegmentResult to disk.</summary>
        void SaveSegmentCroppedImage(SegmentResult segment, string savePath);

        /// <summary>Converts a BitmapSource to a BitmapImage.</summary>
        BitmapImage ConvertToBitmapImage(BitmapSource source);

        /// <summary>Renders a DrawingImage to a BitmapSource.</summary>
        BitmapSource RenderDrawingImageToBitmapSource(DrawingImage drawingImage, double dpiX = 96, double dpiY = 96);

        /// <summary>Composes a grid of images from file paths into a DrawingImage.</summary>
        DrawingImage CreateImageGrid(IEnumerable<string> imagePaths, int columns);
    }

    /// <summary>
    /// Service for processing images, such as adjusting contrast.
    /// </summary>
    public interface IImageProcessingService
    {
        /// <summary>Adjusts the contrast of the given BitmapImage.</summary>
        BitmapImage AdjustContrast(BitmapImage sourceImage, int contrastLevel);
    }
}
