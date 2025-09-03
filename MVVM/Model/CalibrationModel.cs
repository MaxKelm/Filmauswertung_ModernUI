using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Filmauswertung_ModernUI.MVVM.Model
{
    internal class CalibrationImageEntry
    {
        public string FileName { get; set; }   // e.g., "img001"
        public string FullPath { get; set; }   // Full path for loading/saving

        public string FileNameWithoutExtension
        {
            get => Path.GetFileNameWithoutExtension(FullPath);
        }
    }
    public class OpticalDensityResult
    {
        public List<double> OdValues { get; set; }
        public double BackgroundTransmittance { get; set; }
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
            // Dummy implementation – add actual saving logic here
        }

        public static OpticalDensityResult CalculateOpticalDensities(List<string> imagePaths, string brightestPath)
        {
            if (imagePaths == null || imagePaths.Count < 2)
                throw new ArgumentException("At least one background and one measurement image are required.");

            var backgroundPath = brightestPath;
            var measurementPaths = imagePaths;

            var backgroundRedValues = GetRedChannelValues(backgroundPath);
            if (!backgroundRedValues.Any())
                throw new InvalidOperationException("Background image has no red channel data.");

            double backgroundMedian = GetMedian(backgroundRedValues);

            var odValues = new List<double>();
            foreach (var path in measurementPaths)
            {
                var redValues = GetRedChannelValues(path);
                if (!redValues.Any())
                {
                    odValues.Add(0); // or handle as error
                    continue;
                }

                double medianRed = GetAverageRedAroundCOM(path);
                double transmittance = medianRed / (backgroundMedian / 255);
                transmittance = Math.Max(0.01, Math.Min(transmittance, 1.0));
                double od = -Math.Log10(transmittance);
                odValues.Add(od);
            }

            return new OpticalDensityResult
            {
                OdValues = odValues,
                BackgroundTransmittance = backgroundMedian/255
            };
        }




        private static double ComputeODFromTiff(string path)
        {
            // Dummy optical density calculation
            // In practice, you would load the image and compute OD from pixel data
            Random rnd = new Random();
            return Math.Round(rnd.NextDouble() * 2, 2); // OD between 0.0 and 2.0
        }

        public static BitmapImage GenerateOdDosePlot(List<double> doseValues, List<double> odValues, double[] coefficients)
        {
            if (doseValues == null || odValues == null || doseValues.Count != odValues.Count || doseValues.Count == 0)
                throw new ArgumentException("Dose and OD values must be non-null and of equal non-zero length.");

            var plotModel = new PlotModel { Title = "Optical Density vs Dose" };
            plotModel.Background = OxyColors.White;

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Dose [Gy]",
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                MajorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(200, 200, 200),
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                MinorGridlineThickness = 0.5,
                MajorGridlineThickness = 1
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Optical Density",
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                MajorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(200, 200, 200),
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                MinorGridlineThickness = 0.5,
                MajorGridlineThickness = 1
            });

            // Original scatter series (points)
            var series = new LineSeries
            {
                Title = "Data",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.DarkBlue,
                LineStyle = LineStyle.None
            };
            for (int i = 0; i < doseValues.Count; i++)
            {
                series.Points.Add(new DataPoint(doseValues[i], odValues[i]));
            }
            plotModel.Series.Add(series);

            // Polynomial fit curve (3rd degree)
            var fitSeries = new LineSeries
            {
                Title = "3rd Degree Fit",
                Color = OxyColors.Red,
                StrokeThickness = 2
            };

            // Generate smooth x values for plotting the curve
            double minX = doseValues.Min();
            double maxX = doseValues.Max();
            int steps = 200; // smoothness
            double step = (maxX - minX) / steps;

            for (int i = 0; i <= steps; i++)
            {
                double x = minX + i * step;
                double y = coefficients[0] +
                           coefficients[1] * x +
                           coefficients[2] * Math.Pow(x, 2) +
                           coefficients[3] * Math.Pow(x, 3);
                fitSeries.Points.Add(new DataPoint(x, y));
            }
            plotModel.Series.Add(fitSeries);


            // Export plot to image
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");
            PngExporter.Export(plotModel, tempFilePath, 1600, 900, 96);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(tempFilePath);
            bitmap.EndInit();
            bitmap.Freeze();

            try { File.Delete(tempFilePath); } catch { }

            return bitmap;
        }




        public class BrightnessAnalysisResult
        {
            public string BrightestImagePath { get; set; }
            public List<string> Warnings { get; set; } = new List<string>();
        }

        public static BrightnessAnalysisResult AnalyzeImageBrightness(IEnumerable<string> imagePaths)
        {
            double maxAverageBrightness = double.MinValue;
            string brightestPath = null;
            var warnings = new List<string>();

            foreach (var path in imagePaths)
            {
                var bitmap = new BitmapImage(new Uri(path));
                var wb = new WriteableBitmap(bitmap);

                int whitePixelCount = 0;
                double totalBrightness = 0;
                int pixelCount = wb.PixelWidth * wb.PixelHeight;
                int[] pixels = new int[pixelCount];
                wb.CopyPixels(pixels, wb.PixelWidth * 4, 0);

                for (int i = 0; i < pixels.Length; i++)
                {
                    byte r = (byte)((pixels[i] >> 16) & 0xFF);
                    byte g = (byte)((pixels[i] >> 8) & 0xFF);
                    byte b = (byte)(pixels[i] & 0xFF);
                    double brightness = (0.2126 * r + 0.7152 * g + 0.0722 * b);
                    totalBrightness += brightness;

                    if (brightness > 250) whitePixelCount++;
                }

                double avgBrightness = totalBrightness / pixelCount;
                if (avgBrightness > maxAverageBrightness)
                {
                    maxAverageBrightness = avgBrightness;
                    brightestPath = path;
                }

                if (whitePixelCount > pixelCount * 0.01)  // more than 1% very white
                {
                    warnings.Add($"Warning: '{Path.GetFileName(path)}' contains many very bright pixels.\n No white background from the scanner should be visible.\n Calibration might be invalid.");
                }
            }

            return new BrightnessAnalysisResult
            {
                BrightestImagePath = brightestPath,
                Warnings = warnings
            };
        }

        private static List<byte> GetRedChannelValues(string imagePath)
        {
        var redValues = new List<byte>();

            try
            {
                var bitmap = new BitmapImage(new Uri(imagePath));
                var writable = new WriteableBitmap(bitmap);

                int width = writable.PixelWidth;
                int height = writable.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];

                writable.CopyPixels(pixels, stride, 0);

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte red = pixels[i + 2]; // R at offset +2 (BGRA order)
                    redValues.Add(red);
                }
            }
            catch
            {
                // Handle errors or log them
            }

            return redValues;
        }

        private static double GetMedian(List<byte> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int count = sorted.Count;
            if (count == 0) return 0;

            return count % 2 == 0
                ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0
                : sorted[count / 2];
        }
        private static double[,] GetNormalizedRedChannelMatrix(string path)
        {
            var bmp = new System.Drawing.Bitmap(path);
            int width = bmp.Width;
            int height = bmp.Height;
            double[,] normalized = new double[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = bmp.GetPixel(x, y);
                    // Normalize red channel (0-255) to 0-1
                    normalized[x, y] = pixel.R / 255.0;
                }
            }
            return normalized;
        }
        private static double GetAverageRedAroundCOM(string path)
        {
            var bmp = new System.Drawing.Bitmap(path);
            int width = bmp.Width;
            int height = bmp.Height;

            var normalized = GetNormalizedRedChannelMatrix(path);

            // Calculate center of mass (COM) of inverted intensities
            double sumWeights = 0;
            double sumXWeighted = 0;
            double sumYWeighted = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double weight = 1 - normalized[x, y]; // inverted intensity
                    sumWeights += weight;
                    sumXWeighted += x * weight;
                    sumYWeighted += y * weight;
                }
            }

            int comX = width / 2;
            int comY = height / 2;
            if (sumWeights > 0)
            {
                comX = (int)(sumXWeighted / sumWeights);
                comY = (int)(sumYWeighted / sumWeights);
            }
            // Calculate radius in pixels for 5 mm circle
            float horizontalDpi = bmp.HorizontalResolution;
            float verticalDpi = bmp.VerticalResolution;

            float mmPerPixelX = 25.4f / horizontalDpi;
            float mmPerPixelY = 25.4f / verticalDpi;

            double radiusMm = 5.0;
            int radiusX = (int)(radiusMm / mmPerPixelX);
            int radiusY = (int)(radiusMm / mmPerPixelY);
            int radius = Math.Min(radiusX, radiusY);

            List<double> circleValues = new List<double>();

            for (int y = Math.Max(comY - radius, 0); y < Math.Min(comY + radius, height); y++)
            {
                for (int x = Math.Max(comX - radius, 0); x < Math.Min(comX + radius, width); x++)
                {
                    int dx = x - comX;
                    int dy = y - comY;

                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        circleValues.Add(normalized[x, y]);
                    }
                }
            }

            if (!circleValues.Any())
            {
                return 0;
            }

            double averageValue = circleValues.Average();
            return averageValue;
        }

        public static double[] FitPolynomial3rdDegree(List<double> xValues, List<double> yValues)
        {
            if (xValues == null || yValues == null || xValues.Count != yValues.Count || xValues.Count < 4)
                throw new ArgumentException("At least 4 points are required for a 3rd-degree polynomial fit.");

            int n = xValues.Count;

            // Build Vandermonde matrix for 3rd-degree polynomial (1, x, x^2, x^3)
            var matrix = DenseMatrix.Create(n, 4, (i, j) => Math.Pow(xValues[i], j));
            var yVector = DenseVector.OfEnumerable(yValues);

            // Solve least squares system
            var coefficients = matrix.QR().Solve(yVector);

            // coefficients[0] = a0, coefficients[1] = a1, ..., coefficients[3] = a3
            return coefficients.ToArray();
        }

    }
}
