using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
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

        public static double[] FitPolynomial3rdDegree(List<double> odValues, List<double> doseValues)
        {
            if (odValues == null || doseValues == null || odValues.Count != doseValues.Count || odValues.Count < 4)
                throw new ArgumentException("At least 4 points are required for a 3rd-degree polynomial fit.");

            int n = odValues.Count;

            Debug.WriteLine("=== FitPolynomial3rdDegree Debugging ===");
            Debug.WriteLine($"Number of points: {n}");

            // Print input pairs (OD, Dose)
            for (int i = 0; i < n; i++)
            {
                Debug.WriteLine($"Point {i}: OD = {odValues[i]}, Dose = {doseValues[i]}");
            }

            // Build Vandermonde matrix for 3rd-degree polynomial (1, x, x^2, x^3)
            var matrix = DenseMatrix.Create(n, 4, (i, j) => Math.Pow(odValues[i], j));
            var yVector = DenseVector.OfEnumerable(doseValues);

            Debug.WriteLine("Vandermonde Matrix:");
            for (int i = 0; i < n; i++)
            {
                Debug.WriteLine($"Row {i}: {matrix[i, 0]}, {matrix[i, 1]}, {matrix[i, 2]}, {matrix[i, 3]}");
            }

            Debug.WriteLine("Y Vector (Doses):");
            for (int i = 0; i < n; i++)
            {
                Debug.WriteLine($"y[{i}] = {yVector[i]}");
            }

            // Solve least squares system (minimize error between predicted dose and actual dose)
            var coefficients = matrix.QR().Solve(yVector);

            Debug.WriteLine("Fitted Coefficients:");
            Debug.WriteLine($"a0 = {coefficients[0]}");
            Debug.WriteLine($"a1 = {coefficients[1]}");
            Debug.WriteLine($"a2 = {coefficients[2]}");
            Debug.WriteLine($"a3 = {coefficients[3]}");

            // Check how well the fit reproduces the input
            for (int i = 0; i < n; i++)
            {
                double predicted = coefficients[0]
                                 + coefficients[1] * odValues[i]
                                 + coefficients[2] * Math.Pow(odValues[i], 2)
                                 + coefficients[3] * Math.Pow(odValues[i], 3);
                Debug.WriteLine($"Check Point {i}: OD = {odValues[i]}, Actual Dose = {doseValues[i]}, Predicted Dose = {predicted}");
            }

            Debug.WriteLine("=== End FitPolynomial3rdDegree Debugging ===");

            // coefficients[0] = a0, coefficients[1] = a1, coefficients[2] = a2, coefficients[3] = a3
            // Dose ≈ a0 + a1*OD + a2*OD^2 + a3*OD^3
            return coefficients.ToArray();
        }

    }
}
