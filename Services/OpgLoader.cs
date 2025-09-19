using Filmauswertung_ModernUI.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Filmauswertung_ModernUI.Core.Interfaces;
using System.IO;

namespace Filmauswertung_ModernUI.Services
{
    internal class OpgLoader : IOpgLoader
    {
        public async Task<OpgFileData> LoadOpgAsync(string path)
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(path)) return null;

                var lines = File.ReadAllLines(path);
                if (lines.Length == 0) return null;

                var fileContent = string.Join("\n", lines);
                var result = new OpgFileData
                {
                    ImageName = Path.GetFileName(path),
                    Energy = ExtractMetadata(fileContent, "Energy"),
                    DataUnit = ExtractMetadata(fileContent, "Data Unit")
                };

                double.TryParse(ExtractMetadata(fileContent, "Data Factor")?.Replace(",", "."),
                                NumberStyles.Any, CultureInfo.InvariantCulture, out double factor);
                result.DataFactor = factor > 0 ? factor : 1.0;

                int.TryParse(ExtractMetadata(fileContent, "No. of Columns"), out int cols);
                int.TryParse(ExtractMetadata(fileContent, "No. of Rows"), out int rows);
                result.NoOfColumns = cols;
                result.NoOfRows = rows;

                result.FFF = Regex.IsMatch(fileContent, "<FFF>true</FFF>", RegexOptions.IgnoreCase);

                // X coordinates
                int xHeaderIndex = Array.FindIndex(lines, l => l.Trim().StartsWith("X[mm]"));
                if (xHeaderIndex < 0) return null;

                result.X = Regex.Matches(lines[xHeaderIndex], @"-?\d+\.?\d*")
                                .Cast<Match>()
                                .Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture))
                                .ToList();

                // Y coordinates + pixel matrix
                var yList = new List<double>();
                var pixelMatrix = new List<List<double>>();
                for (int i = xHeaderIndex + 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                    var tokens = Regex.Matches(lines[i], @"-?\d+\.?\d*")
                                      .Cast<Match>()
                                      .Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture))
                                      .ToList();
                    if (tokens.Count != result.X.Count + 1) continue;

                    yList.Add(tokens[0]);
                    pixelMatrix.Add(tokens.Skip(1).ToList());
                }

                result.Y = yList;
                result.PixelValues = pixelMatrix;
                ValidateAndFixDimensions(result);

                return result;
            });
        }

        private static string ExtractMetadata(string fileContent, string tag)
        {
            var match = Regex.Match(fileContent, $"<{tag}>(.*?)</{tag}>", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static void ValidateAndFixDimensions(OpgFileData data)
        {
            if (data.PixelValues == null) data.PixelValues = new List<List<double>>();
            int rows = data.PixelValues.Count;
            int cols = rows > 0 ? data.PixelValues.Min(r => r.Count) : 0;
            data.NoOfRows = rows;
            data.NoOfColumns = cols;
        }
    }
}
