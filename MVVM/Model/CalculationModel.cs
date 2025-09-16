using Filmauswertung_ModernUI.Core.Interfaces;
using Filmauswertung_ModernUI.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            if (image == null || calibration == null)
                return Array.Empty<double>();

            BitmapSource source = image;
            if (image.Format != PixelFormats.Bgra32)
                source = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            double[] doseArray = new double[width * height];
            double[] coeffs = calibration.PolynomialFitCoefficients;

            bool isCubicFit = coeffs != null && coeffs.Length == 4;
            bool isInverseLinearFit = coeffs != null && coeffs.Length == 3;

            for (int i = 0; i < width * height; i++)
            {
                int idx = i * 4;
                double redValue = pixels[idx + 2]; // R in BGRA
                double normalized = redValue / 255.0;
                double transmittance = normalized / calibration.BackgroundMedian;
                transmittance = Math.Max(transmittance, 1e-6); // avoid log(0)
                double od = -Math.Log10(transmittance);

                double dose = 0;

                if (isCubicFit)
                {
                    dose = coeffs[0] + coeffs[1] * od + coeffs[2] * Math.Pow(od, 2) + coeffs[3] * Math.Pow(od, 3);
                }
                else if (isInverseLinearFit)
                {
                    double a = coeffs[0];
                    double b = coeffs[1];
                    double c = coeffs[2];
                    double odAdjusted = Math.Abs(od - a) < 1e-12 ? od + 1e-12 : od; // avoid division by zero
                    dose = b / (odAdjusted - a) + c;
                }

                doseArray[i] = Math.Max(dose, 0);

            }

            return doseArray;
        }



        public static BitmapImage ConvertDoseArrayToBitmap(double[] doseArray, int width, int height)
        {
            if (doseArray == null || doseArray.Length != width * height)
                return null;

            double min = doseArray.Min();
            double max = doseArray.Max();

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

        public static (double[] histogramBins, int[] histogramCounts, double[] topPeaks, double medianDose) CalculateDoseStatistics(double[] doseArray, int numBins = 256)
        {
            if (doseArray == null || doseArray.Length == 0)
                return (Array.Empty<double>(), Array.Empty<int>(), Array.Empty<double>(), 0);

            // Filter out zero/background doses
            var validDoses = doseArray.Where(d => d > 1e-6).ToArray();
            if (!validDoses.Any()) return (Array.Empty<double>(), Array.Empty<int>(), Array.Empty<double>(), 0);

            double minDose = validDoses.Min();
            double maxDose = validDoses.Max();
            double medianDose = validDoses.Length > 0 ? validDoses.OrderBy(d => d).ElementAt(validDoses.Length / 2) : 0;

            double[] bins = new double[numBins];
            int[] counts = new int[numBins];
            double binWidth = (maxDose - minDose) / numBins;

            foreach (var dose in validDoses)
            {
                int binIndex = (int)((dose - minDose) / binWidth);
                if (binIndex >= numBins) binIndex = numBins - 1;
                counts[binIndex]++;
            }

            for (int i = 0; i < numBins; i++)
                bins[i] = minDose + (i + 0.5) * binWidth;

            // Find top 3 peaks by **highest dose values** (not counts)
            var topPeaksByValue = validDoses
                .OrderByDescending(d => d)
                .Take(3)
                .ToArray();

            return (bins, counts, topPeaksByValue, medianDose);
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

        /// <summary>
        /// Generates OPG content for multiple dose images using a template.
        /// </summary>
        /// <param name="fileList">List of files to process</param>
        /// <param name="imageService">Image service to load images</param>
        /// <param name="calibration">Current calibration</param>
        /// <param name="smoothnessValue">Smoothing level (1-5)</param>
        /// <param name="templatePath">Full path to the Muster.opg template</param>
        /// <returns>Final OPG content as string</returns>
        public static Dictionary<string, string> GenerateOpgFiles(
            IEnumerable<FileEntry> fileList,
            IImageService imageService,
            Calibration calibration,
            double smoothnessValue,
            string templatePath,
            string selectedLeftItem,
            string selectedRightItem,
            bool srsResampling)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("OPG template not found.", templatePath);

            var results = new Dictionary<string, string>();

            foreach (var file in fileList)
            {
                if (!File.Exists(file.FullPath))
                    continue;

                // Load template fresh for each file
                string templateContent = File.ReadAllText(templatePath);
                var sb = new System.Text.StringBuilder(templateContent);

                // Update Energy, Device Type, and Radiation Type
                sb = new System.Text.StringBuilder(UpdateOpgHeader(sb.ToString(), selectedLeftItem, selectedRightItem));

                // Determine if FFF mode is active
                bool isFFF = false;
                if (!string.IsNullOrEmpty(selectedLeftItem))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(selectedLeftItem, @"(\d+)([XFE])");
                    if (match.Success)
                    {
                        string typeChar = match.Groups[2].Value.ToUpper();
                        isFFF = typeChar == "F";
                    }
                }

                // Insert <FFF>true</FFF> line if needed
                if (isFFF)
                {
                    string pattern = @"(<DefectsInterpolated>.*?</DefectsInterpolated>)";
                    var match = Regex.Match(sb.ToString(), pattern, RegexOptions.Singleline);
                    if (match.Success)
                    {
                        sb.Replace(match.Value, match.Value + "\r\n<FFF>true</FFF>");
                    }
                }

                // Process image
                var rawImage = imageService.LoadImage(file.FullPath);

                // Extract dose values
                var doses = ExtractDoseFromImage(rawImage, calibration);

                // Apply smoothing
                int smoothLevel = (int)Clamp(smoothnessValue, 1, 5);
                if (smoothLevel > 1)
                    doses = SmoothDoseArray(doses, rawImage.PixelWidth, rawImage.PixelHeight, smoothLevel);

                // Use actual image DPI to calculate physical spacing
                int width = rawImage.PixelWidth;
                int height = rawImage.PixelHeight;
                double dpiX = rawImage.DpiX;
                double dpiY = rawImage.DpiY;
                // fallback in case DPI is missing or invalid
                if (dpiX < 1)
                {
                    dpiX = 96;
                    Debug.WriteLine("Fallback used for X dpi");
                }
                if (dpiY < 1)
                {
                    dpiY = 96;
                    Debug.WriteLine("Fallback used for Y dpi");
                }

                double spacingXmm = 25.4 / dpiX; // mm per pixel
                double spacingYmm = 25.4 / dpiY; // mm per pixel

                if (srsResampling)
                {
                    double targetSpacing = 0.4; // mm per pixel for SRS
                    doses = ResampleDoseArray(doses, width, height, spacingXmm, spacingYmm, targetSpacing);
                    width = (int)Math.Round(width * spacingXmm / targetSpacing);
                    height = (int)Math.Round(height * spacingYmm / targetSpacing);
                    spacingXmm = targetSpacing;
                    spacingYmm = targetSpacing;
                }


                // Rewrite X[mm] block
                // Calculate X positions
                var xPositions = Enumerable.Range(0, width).Select(i => i * spacingXmm).ToList();
                double medianX = CalculateMedian(xPositions);
                var shiftedXmmValues = ShiftAndRoundXmmValues(xPositions, medianX);

                sb = new System.Text.StringBuilder(ReplaceXValuesInOpg(sb.ToString(), shiftedXmmValues));

                // Rewrite Y[mm] block with shifted values and doses
                sb = new System.Text.StringBuilder(
                        ReplaceYValuesInOpg(sb.ToString(), doses, width, height, spacingYmm));

                // Update header with correct grid dimensions
                sb = new System.Text.StringBuilder(UpdateRowsAndColumns(sb.ToString(), width, height));


                // Update File Name + Image Name based on current file
                string baseName = Path.GetFileNameWithoutExtension(file.FileName);
                string finalContent = UpdateOpgFileAndImageName(sb.ToString(), baseName);

                // Save in dictionary: key = suggested filename, value = OPG content
                results[$"{baseName}.opg"] = finalContent;
            }

            return results;
        }


        public static string UpdateOpgFileAndImageName(string opgContent, string baseName)
        {
            // File Name with .opg extension
            string fileNameLinePattern = @"File Name:\s*.*";
            string fileNameLineReplacement = $"File Name:          {baseName}.opg\r";
            opgContent = Regex.Replace(opgContent, fileNameLinePattern, fileNameLineReplacement);

            // Image Name without extension
            string imageNameLinePattern = @"Image Name:\s*.*";
            string imageNameLineReplacement = $"Image Name:         {baseName}\r";
            opgContent = Regex.Replace(opgContent, imageNameLinePattern, imageNameLineReplacement);

            return opgContent;
        }


        public static string Match(this string input, string pattern)
        {
            var m = Regex.Match(input, pattern, RegexOptions.Singleline);
            return m.Success ? m.Value : null;
        }


        /// <summary>
        /// Updates Energy and Device Type lines in the template content.
        /// </summary>
        private static string UpdateOpgHeader(string opgContent, string selectedLeftItem, string selectedRightItem)
        {

            if (string.IsNullOrEmpty(selectedLeftItem))
            {
                System.Diagnostics.Debug.WriteLine("No left item selected. Returning original OPG content.");
                return opgContent;
            }

            // Extract nominal value and type
            var match = Regex.Match(selectedLeftItem, @"(\d+)([XFE])");
            if (!match.Success)
            {
                System.Diagnostics.Debug.WriteLine("Failed to parse selectedLeftItem. Returning original OPG content.");
                return opgContent;
            }

            int nominal = int.Parse(match.Groups[1].Value);
            string typeChar = match.Groups[2].Value;
            string energyStr;
            string radType;

            switch (typeChar.ToUpper())
            {
                case "X":
                case "F":  // FFF photons
                    energyStr = $"{nominal}".ToString(System.Globalization.CultureInfo.InvariantCulture) + ".00 MV";
                    radType = "Photons";
                    break;
                case "E":
                    energyStr = $"{nominal}".ToString(System.Globalization.CultureInfo.InvariantCulture) + ".00 MeV";
                    radType = "Electrons";
                    break;
                default:
                    energyStr = $"{nominal}".ToString(System.Globalization.CultureInfo.InvariantCulture);
                    radType = "Photons";
                    break;
            }


            string energyLinePattern = @"^Energy:.*(\r?\n)?";
            string energyLineReplacement = $"Energy:             {energyStr}\r\n";

            opgContent = Regex.Replace(opgContent, energyLinePattern, energyLineReplacement, RegexOptions.Multiline);

            string radTypePattern = @"^Radiation Type:.*(\r?\n)?";
            string radTypeReplacement = $"Radiation Type:     {radType}\r\n";

            opgContent = Regex.Replace(opgContent, radTypePattern, radTypeReplacement, RegexOptions.Multiline);

            if (!string.IsNullOrEmpty(selectedRightItem))
            {
                string deviceLinePattern = @"^Device Type:.*(\r?\n)?";
                string deviceLineReplacement = $"Device Type:         {selectedRightItem}\r\n";
                opgContent = Regex.Replace(opgContent, deviceLinePattern, deviceLineReplacement, RegexOptions.Multiline);
            }
            return opgContent;
        }


        /// <summary>
        /// Smooths a dose array with a box filter.
        /// </summary>
        public static double[] SmoothDoseArray(double[] doseArray, int width, int height, int smoothLevel)
        {
            if (smoothLevel <= 1)
                return doseArray; // level 1 = no smoothing

            // Map smoothLevel (2..5) to radius and sigma values
            int radius;
            double sigmaSpatial;
            double sigmaRange;

            switch (smoothLevel)
            {
                case 2: // minimal smoothing
                    radius = 1;
                    sigmaSpatial = 1.0;
                    sigmaRange = 0.05; // sensitive to dose differences
                    break;
                case 3:
                    radius = 2;
                    sigmaSpatial = 2.0;
                    sigmaRange = 0.1;
                    break;
                case 4:
                    radius = 3;
                    sigmaSpatial = 3.0;
                    sigmaRange = 0.15;
                    break;
                case 5: // strongest smoothing
                    radius = 4;
                    sigmaSpatial = 4.0;
                    sigmaRange = 0.2;
                    break;
                default:
                    return doseArray;
            }

            // Precompute Gaussian spatial kernel
            double[,] spatialKernel = GenerateGaussianKernel(radius, sigmaSpatial);

            double[] smoothed = new double[doseArray.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    double centerValue = doseArray[index];

                    double weightedSum = 0;
                    double weightTotal = 0;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        int ny = y + ky;
                        if (ny < 0 || ny >= height) continue;

                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int nx = x + kx;
                            if (nx < 0 || nx >= width) continue;

                            double neighborValue = doseArray[ny * width + nx];

                            // Spatial weight from Gaussian kernel
                            double spatialWeight = spatialKernel[ky + radius, kx + radius];

                            // Range weight based on intensity difference
                            double rangeWeight = Math.Exp(-Math.Pow(neighborValue - centerValue, 2) / (2 * sigmaRange * sigmaRange));

                            // Combined bilateral weight
                            double weight = spatialWeight * rangeWeight;

                            weightedSum += neighborValue * weight;
                            weightTotal += weight;
                        }
                    }

                    smoothed[index] = weightedSum / weightTotal;
                }
            }

            return smoothed;
        }

        private static double[,] GenerateGaussianKernel(int radius, double sigma)
        {
            int size = 2 * radius + 1;
            double[,] kernel = new double[size, size];
            double sum = 0;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    double value = Math.Exp(-(x * x + y * y) / (2 * sigma * sigma));
                    kernel[y + radius, x + radius] = value;
                    sum += value;
                }
            }

            // Normalize kernel so that all weights sum to 1
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[y, x] /= sum;
                }
            }

            return kernel;
        }



        /// <summary>
        /// Generates the ASCII dose block for a single image.
        /// </summary>
        public static string GenerateDoseBlock(double[] doses, int width, int height)
        {
            var sb = new System.Text.StringBuilder();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double dose = doses[y * width + x];
                    sb.Append(dose.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture));
                    sb.Append("\t");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static double CalculateMedian(List<double> values)
        {
            var sortedValues = values.OrderBy(x => x).ToList();
            int count = sortedValues.Count;

            if (count % 2 == 0)
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2;
            else
                return sortedValues[count / 2];
        }

        private static List<double> ShiftAndRoundXmmValues(List<double> values, double medianXmm)
        {
            return values.Select(x => Math.Round(x - medianXmm, 1)).ToList();
        }

        private static string ReplaceXValuesInOpg(string opgContent, List<double> shiftedXmmValues)
        {
            string pattern = @"X\[mm\](.*?)Y\[mm\]";
            var match = Regex.Match(opgContent, pattern, RegexOptions.Singleline);
            if (!match.Success) return opgContent;

            string xBlock = match.Groups[1].Value;
            var sb = new System.Text.StringBuilder();

            foreach (var x in shiftedXmmValues)
            {
                string val = x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                int len = val.Length;
                int spacesAfterTab = 2 + (5 - len);
                if (spacesAfterTab < 0) spacesAfterTab = 0;

                sb.Append(' ')
                  .Append('\t')
                  .Append(new string(' ', spacesAfterTab))
                  .Append(val);
            }

            sb.Append("\r\n");
            return opgContent.Replace(xBlock, sb.ToString());
        }
        private static string ReplaceYValuesInOpg(string opgContent, double[] doses, int width, int height, double spacingYmm)
        {
            // Calculate Y positions using the provided spacing
            var yPositions = Enumerable.Range(0, height).Select(i => i * spacingYmm).ToList();
            double medianY = CalculateMedian(yPositions);
            var shiftedYmmValues = yPositions.Select(y => Math.Round(y - medianY, 1)).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine();

            for (int row = 0; row < height; row++)
            {
                string yValStr = shiftedYmmValues[row].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

                // Y value with indentation
                sb.Append("    ").Append(yValStr).Append(" \t");

                for (int col = 0; col < width; col++)
                {
                    double dose = doses[row * width + col] * 1000; // scaled
                    string doseStr = Math.Round(dose, 4).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

                    if (col == 0)
                        sb.Append(doseStr);
                    else
                        sb.Append("\t").Append(doseStr);
                }

                sb.Append("\r\n");
            }

            // Replace block in OPG content
            string pattern = @"(?<=Y\[mm\])(.*?)(?=</asciibody>)";
            var match = Regex.Match(opgContent, pattern, RegexOptions.Singleline);
            if (match.Success)
                opgContent = opgContent.Replace(match.Groups[1].Value, sb.ToString());

            return opgContent;
        }
        private static double[] ResampleDoseArray(double[] doses, int originalWidth, int originalHeight, double originalSpacingX, double originalSpacingY, double targetSpacing)
        {
            int newWidth = (int)Math.Round(originalWidth * originalSpacingX / targetSpacing);
            int newHeight = (int)Math.Round(originalHeight * originalSpacingY / targetSpacing);

            double[] resampled = new double[newWidth * newHeight];

            for (int y = 0; y < newHeight; y++)
            {
                double srcY = y * targetSpacing / originalSpacingY;
                int y0 = (int)Math.Floor(srcY);
                int y1 = Math.Min(y0 + 1, originalHeight - 1);
                double fy = srcY - y0;

                for (int x = 0; x < newWidth; x++)
                {
                    double srcX = x * targetSpacing / originalSpacingX;
                    int x0 = (int)Math.Floor(srcX);
                    int x1 = Math.Min(x0 + 1, originalWidth - 1);
                    double fx = srcX - x0;

                    // Bilinear interpolation
                    double dose = (1 - fx) * (1 - fy) * doses[y0 * originalWidth + x0]
                                + fx * (1 - fy) * doses[y0 * originalWidth + x1]
                                + (1 - fx) * fy * doses[y1 * originalWidth + x0]
                                + fx * fy * doses[y1 * originalWidth + x1];

                    resampled[y * newWidth + x] = dose;
                }
            }

            return resampled;
        }

        private static string UpdateRowsAndColumns(string opgContent, int width, int height)
        {
            // Replace "No. of Columns" line
            opgContent = Regex.Replace(
                opgContent,
                @"No\. of Columns:\s*\d+",
                $"No. of Columns:     {width}");

            // Replace "No. of Rows" line
            opgContent = Regex.Replace(
                opgContent,
                @"No\. of Rows:\s*\d+",
                $"No. of Rows:        {height}");

            return opgContent;
        }

    }
}
