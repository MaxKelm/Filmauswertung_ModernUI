using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.MVVM.Model
{
    public class Calibration
    {
        public string FileName { get; set; }
        public string CalibrationValues { get; set; }
        public string Unit { get; set; }
        public double BackgroundMedian { get; set; }
        public double[] PolynomialFitCoefficients { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public double[] ParsedCalibrationValues =>
            CalibrationValues?
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(v => double.Parse(v.Trim(), System.Globalization.CultureInfo.InvariantCulture))
            .ToArray() ?? Array.Empty<double>();
    }

    internal static class CalculationModel
    {
        /// <summary>
        /// Extracts the red channel, normalizes by 255, computes transmittance, optical density, and dose.
        /// </summary>
        public static double[] ExtractDoseFromImage(BitmapImage image, Calibration calibration)
        {
            if (image == null || calibration == null) return Array.Empty<double>();

            // Convert to Bgra32 for easy channel extraction
            BitmapSource source = image;
            if (image.Format != PixelFormats.Bgra32)
                source = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);
            Console.WriteLine($"Background Median {calibration.BackgroundMedian}");
            Console.WriteLine($"Coeffs {calibration.PolynomialFitCoefficients[0]}, {calibration.PolynomialFitCoefficients[1]}, {calibration.PolynomialFitCoefficients[2]}, {calibration.PolynomialFitCoefficients[3]}");

            double[] doseArray = new double[width * height];

            for (int i = 0; i < width * height; i++)
            {
                int idx = i * 4;
                double redValue = pixels[idx + 2]; // R in BGRA
                double normalized = redValue / 255.0;
                double transmittance = normalized / calibration.BackgroundMedian;
                transmittance = Math.Max(transmittance, 1e-6); // avoid log(0)
                double od = -Math.Log10(transmittance);

                double dose = 0;
                double[] coeffs = calibration.PolynomialFitCoefficients;
                if (coeffs != null && coeffs.Length >= 4)
                {
                    dose = coeffs[0] + coeffs[1] * od + coeffs[2] * Math.Pow(od, 2) + coeffs[3] * Math.Pow(od, 3);
                }

                doseArray[i] = dose;

                // Debug output for first few pixels
                if (i < 10)
                    Console.WriteLine($"Pixel {i}: Red={redValue:F2}, Norm={normalized:F3}, Trans={transmittance:F3}, OD={od:F3}, Dose={dose:F3}");
            }

            Console.WriteLine($"Extracted {doseArray.Length} dose values from image ({width}x{height})");
            return doseArray;
        }

        public static BitmapImage ConvertDoseArrayToBitmap(double[] doseArray, int width, int height)
        {
            if (doseArray == null || doseArray.Length != width * height)
                return null;

            double min = doseArray.Min();
            double max = doseArray.Max();

            Console.WriteLine($"Dose array min={min:F3}, max={max:F3}");

            var pixels = new byte[width * height * 4]; // BGRA32
            for (int i = 0; i < doseArray.Length; i++)
            {
                double t = (max > min) ? (doseArray[i] - min) / (max - min) : 0;
                Color c = ViridisColormap(t);

                int idx = i * 4;
                pixels[idx + 0] = c.B;
                pixels[idx + 1] = c.G;
                pixels[idx + 2] = c.R;
                pixels[idx + 3] = 255;
            }

            var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var stream = new System.IO.MemoryStream())
            {
                encoder.Save(stream);
                stream.Position = 0;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = stream;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
        }

        public static (double[] histogramBins, int[] histogramCounts, double[] topPeaks) CalculateDoseStatistics(double[] doseArray, int numBins = 256)
        {
            if (doseArray == null || doseArray.Length == 0)
                return (Array.Empty<double>(), Array.Empty<int>(), Array.Empty<double>());

            double minDose = doseArray.Where(d => d > 1e-6).DefaultIfEmpty(0).Min();
            double maxDose = doseArray.Max();

            Console.WriteLine($"Calculating histogram: minDose={minDose:F3}, maxDose={maxDose:F3}");

            double[] bins = new double[numBins];
            int[] counts = new int[numBins];
            double binWidth = (maxDose - minDose) / numBins;

            foreach (var dose in doseArray)
            {
                if (dose <= 1e-6) continue;
                int binIndex = (int)((dose - minDose) / binWidth);
                if (binIndex >= numBins) binIndex = numBins - 1;
                counts[binIndex]++;
            }

            for (int i = 0; i < numBins; i++)
                bins[i] = minDose + (i + 0.5) * binWidth;

            // Find top 3 peaks excluding background
            List<(int index, int count)> binCounts = counts.Select((c, i) => (i, c)).ToList();
            var topBins = binCounts
                .Where(b => b.count > 0)
                .OrderByDescending(b => b.count)
                .Take(10)
                .Where(b => bins[b.index] > 1e-3)
                .Take(3)
                .Select(b => bins[b.index])
                .ToArray();

            Console.WriteLine("Histogram top peaks:");
            for (int i = 0; i < topBins.Length; i++)
                Console.WriteLine($"Peak {i + 1}: {topBins[i]:F3}");

            return (bins, counts, topBins);
        }

        private static Color ViridisColormap(double t)
        {
            t = Clamp(t, 0, 1);
            double r = Clamp(4.0 * t - 1.5, 0, 1);
            double g = Clamp(-4.0 * Math.Abs(t - 0.5) + 1, 0, 1);
            double b = Clamp(1.5 - 4.0 * t, 0, 1);

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
