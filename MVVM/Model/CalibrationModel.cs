using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.MVVM.Model
{
    internal class CalibrationImageEntry
    {
        public string FileName { get; set; }   // e.g., "img001"
        public string FullPath { get; set; }   // Full path for loading/saving
        public string FileNameWithoutExtension
        {
            get
            {
                return System.IO.Path.GetFileNameWithoutExtension(FullPath);
            }
        }
    }

    internal static class CalibrationModel
    {
        public static IEnumerable<CalibrationImageEntry> CreateEntriesFromPaths(IEnumerable<string> paths)
        {
            return paths?
                .Where(File.Exists)
                .Select(path => new CalibrationImageEntry
                {
                    FileName = Path.GetFileNameWithoutExtension(path),
                    FullPath = path
                })
                ?? Enumerable.Empty<CalibrationImageEntry>();
        }

        public static BitmapImage LoadImage(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
                return null;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(fullPath);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public static void SaveImage(BitmapImage image, string filePath)
        {
            Debug.WriteLine($"Saving image to {filePath}");
        }
    }
}
