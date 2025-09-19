using Filmauswertung_ModernUI.Core;
using Filmauswertung_ModernUI.Core.Interfaces;
using Filmauswertung_ModernUI.MVVM.Model;
using Filmauswertung_ModernUI.Services;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Filmauswertung_ModernUI.MVVM.ViewModel
{
    internal class DoseViewerViewModel : ObservableObject
    {
        private readonly IToastService _toastService = new ToastService();
        private readonly DoseViewerModel _model = new DoseViewerModel();

        // -----------------------------
        // 3D Model Binding
        // -----------------------------
        private Model3D _referenceModel;
        public Model3D ReferenceModel
        {
            get => _referenceModel;
            set => SetProperty(ref _referenceModel, value);
        }

        public Action ZoomToFitAction { get; set; }


        // -----------------------------
        // 3D Model property for binding
        // -----------------------------
        private GeometryModel3D _surfaceModelContent;
        public GeometryModel3D SurfaceModelContent
        {
            get => _surfaceModelContent;
            set => SetProperty(ref _surfaceModelContent, value);
        }

        private GeometryModel3D _compareModelContent;
        public GeometryModel3D CompareModelContent
        {
            get => _compareModelContent;
            set => SetProperty(ref _compareModelContent, value);
        }
        private Model3DGroup _displayModelGroup;
        public Model3DGroup DisplayModelGroup
        {
            get => _displayModelGroup;
            set => SetProperty(ref _displayModelGroup, value);
        }


        public enum CompareMethod
        {
            SingleDisplay,
            DualDisplay,
            DoseDifference
        }
        private CompareMethod _selectedCompareMethod = CompareMethod.SingleDisplay;
        public CompareMethod SelectedCompareMethod
        {
            get => _selectedCompareMethod;
            set
            {
                if (_selectedCompareMethod != value)
                {
                    _selectedCompareMethod = value;
                    OnPropertyChanged();
                    RefreshDisplay();
                }
            }
        }

        public bool IsSingleDisplay
        {
            get => SelectedCompareMethod == CompareMethod.SingleDisplay;
            set { if (value) SelectedCompareMethod = CompareMethod.SingleDisplay; }
        }

        public bool IsDualDisplay
        {
            get => SelectedCompareMethod == CompareMethod.DualDisplay;
            set { if (value) SelectedCompareMethod = CompareMethod.DualDisplay; }
        }

        public bool IsDoseDifference
        {
            get => SelectedCompareMethod == CompareMethod.DoseDifference;
            set { if (value) SelectedCompareMethod = CompareMethod.DoseDifference; }
        }

        public HelixViewport3D Viewport { get; set; }


        // -----------------------------
        // File references
        // -----------------------------
        public string ReferenceFile
        {
            get => _model.ReferenceFilePath;
            set
            {
                if (_model.ReferenceFilePath != value)
                {
                    _model.ReferenceFilePath = value;
                    OnPropertyChanged();
                    IsCompareEnabled = !string.IsNullOrEmpty(value);
                }
            }
        }

        public string CompareFile
        {
            get => _model.CompareFilePath;
            set
            {
                if (_model.CompareFilePath != value)
                {
                    _model.CompareFilePath = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isCompareEnabled;
        public bool IsCompareEnabled
        {
            get => _isCompareEnabled;
            set => SetProperty(ref _isCompareEnabled, value);
        }

        private Brush _backgroundBrush = Brushes.Transparent;
        public Brush BackgroundBrush
        {
            get => _backgroundBrush;
            set => SetProperty(ref _backgroundBrush, value);
        }


        // -----------------------------
        // Bottom controls
        // -----------------------------
        public ObservableCollection<string> BackgroundColorOptions { get; }
        public string SelectedBackgroundColor
        {
            get => _model.SelectedBackgroundColor;
            set
            {
                if (_model.SelectedBackgroundColor != value)
                {
                    _model.SelectedBackgroundColor = value;
                    OnPropertyChanged();

                    // Update BackgroundBrush
                    if (!string.IsNullOrEmpty(value))
                    {
                        try
                        {
                            var converter = new BrushConverter();
                            BackgroundBrush = (Brush)converter.ConvertFromString(value);
                        }
                        catch
                        {
                            BackgroundBrush = Brushes.Transparent; // fallback
                        }
                    }
                }
            }
        }


        public ObservableCollection<string> GradientModes { get; }
        public string SelectedGradientMode
        {
            get => _model.SelectedGradientMode;
            set
            {
                if (_model.SelectedGradientMode != value)
                {
                    _model.SelectedGradientMode = value;
                    OnPropertyChanged();
                    RefreshDisplay(); // 🔥 trigger update
                }
            }
        }

        public double ReferenceDose
        {
            get => _model.ReferenceDose;
            set
            {
                if (_model.ReferenceDose != value)
                {
                    _model.ReferenceDose = value;
                    OnPropertyChanged();
                    RefreshDisplay(); // 🔥 trigger update
                }
            }
        }


        // -----------------------------
        // Commands
        // -----------------------------
        public RelayCommand LoadReferenceCommand { get; }
        public RelayCommand LoadCompareCommand { get; }
        public RelayCommand Export3DCommand { get; }
        public RelayCommand TakeScreenshotCommand { get; }


        // -----------------------------
        // Services
        // -----------------------------
        private readonly Importer _importer = new SingleFileImporter(new[] { ".opg" });
        private readonly Exporter _exporter = new FolderExporter("3DModel");

        // -----------------------------
        // Constructor
        // -----------------------------
        public DoseViewerViewModel()
        {
            BackgroundColorOptions = new ObservableCollection<string>(_model.BackgroundColorOptions);
            GradientModes = new ObservableCollection<string>(_model.GradientModes);

            LoadReferenceCommand = new RelayCommand(async _ => await LoadReferenceFileAsync());
            LoadCompareCommand = new RelayCommand(async _ => await LoadCompareFileAsync(), _ => IsCompareEnabled);
            Export3DCommand = new RelayCommand(_ => Export3DModel());
            TakeScreenshotCommand = new RelayCommand(_ => TakeScreenshot(), _ => DisplayModelGroup != null);
        }

        // -----------------------------
        // Methods
        // -----------------------------
        private async Task LoadReferenceFileAsync()
        {
            string path = ImportDialogService.ShowDialog(_importer);
            if (string.IsNullOrEmpty(path)) return;

            var files = await _importer.ImportAsync(path);
            ReferenceFile = files.FirstOrDefault();
            if (!string.IsNullOrEmpty(ReferenceFile))
            {
                _model.ReferenceData = await LoadOpgDataAsync(ReferenceFile);
                RefreshDisplay();
            }
        }

        private async Task LoadCompareFileAsync()
        {
            string path = ImportDialogService.ShowDialog(_importer);
            if (string.IsNullOrEmpty(path)) return;

            var files = await _importer.ImportAsync(path);
            CompareFile = files.FirstOrDefault();

            if (!string.IsNullOrEmpty(CompareFile))
            {
                _model.CompareData = await LoadOpgDataAsync(CompareFile);
                RefreshDisplay();
            }
        }


        private GeometryModel3D BuildSurfaceModel(OpgFileData data)
        {
            Debug.WriteLine($"[Surface] Building model with Rows={data.NoOfRows}, Cols={data.NoOfColumns}");

            if (data.NoOfRows <= 1 || data.NoOfColumns <= 1)
            {
                Debug.WriteLine("[Surface] Not enough data to build surface mesh.");
                return null;
            }

            try
            {
                double scaleZ = ComputeZScale(data);
                Debug.WriteLine($"[Surface] Z scaling factor={scaleZ}");

                double xCenter = (data.X.First() + data.X.Last()) / 2.0;
                double yCenter = (data.Y.First() + data.Y.Last()) / 2.0;
                var allValues = data.PixelValues.SelectMany(r => r);
                double zMin = allValues.Min();
                double zMax = allValues.Max();
                double zCenter = (zMin + zMax) / 2.0;

                Debug.WriteLine($"[Surface] Centers: X={xCenter}, Y={yCenter}, Z={zCenter} (range {zMin}–{zMax})");

                var mesh = BuildMesh(data, scaleZ, xCenter, yCenter, zCenter);
                var heatmap = CreateHeatmapBitmap(data);

                var brush = new ImageBrush(heatmap)
                {
                    Stretch = Stretch.Fill,
                    ViewportUnits = BrushMappingMode.RelativeToBoundingBox
                };

                var material = MaterialHelper.CreateMaterial(brush);

                Debug.WriteLine("[Surface] Surface model successfully built.");
                return new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Surface] Failed to build model: {ex}");
                return null;
            }
        }

        public static double ComputeZScale(OpgFileData data)
        {
            double xRange = data.X.Last() - data.X.First();
            double yRange = data.Y.Last() - data.Y.First();
            double zRange = data.PixelValues.SelectMany(r => r).Max() - data.PixelValues.SelectMany(r => r).Min();
            return zRange > 0 ? Math.Min(xRange, yRange) / zRange * 0.5 : 1.0;
        }
        private MeshGeometry3D BuildMesh(OpgFileData data, double scaleZ, double xCenter, double yCenter, double zCenter)
        {
            var mb = new MeshBuilder(false, true);
            int rows = data.NoOfRows;
            int cols = data.NoOfColumns;

            for (int y = 0; y < rows - 1; y++)
            {
                for (int x = 0; x < cols - 1; x++)
                {
                    double x0 = data.X[x] - xCenter;
                    double x1 = data.X[x + 1] - xCenter;
                    double y0 = data.Y[y] - yCenter;
                    double y1 = data.Y[y + 1] - yCenter;

                    double z00 = data.PixelValues[y][x] * scaleZ - zCenter * scaleZ;
                    double z01 = data.PixelValues[y + 1][x] * scaleZ - zCenter * scaleZ;
                    double z10 = data.PixelValues[y][x + 1] * scaleZ - zCenter * scaleZ;
                    double z11 = data.PixelValues[y + 1][x + 1] * scaleZ - zCenter * scaleZ;

                    var p00 = new Point3D(x0, y0, z00);
                    var p01 = new Point3D(x0, y1, z01);
                    var p10 = new Point3D(x1, y0, z10);
                    var p11 = new Point3D(x1, y1, z11);

                    // Correct UV mapping (flip V coordinate)
                    double u0 = (double)x / (cols - 1);
                    double u1 = (double)(x + 1) / (cols - 1);
                    double v0 = 1.0 - (double)y / (rows - 1);
                    double v1 = 1.0 - (double)(y + 1) / (rows - 1);

                    mb.AddQuad(
                        p00, p10, p11, p01,
                        new Point(u0, v0),
                        new Point(u1, v0),
                        new Point(u1, v1),
                        new Point(u0, v1)
                    );
                }
            }

            return mb.ToMesh();
        }


        private WriteableBitmap CreateHeatmapBitmap(OpgFileData data)
        {
            // Prefer actual matrix dimensions over declared metadata
            int height = data.PixelValues?.Count ?? 0;
            int width = height > 0 ? data.PixelValues.Min(r => r.Count) : 0;

            Debug.WriteLine($"[Heatmap] Effective Width={width}, Height={height}");
            Debug.WriteLine($"[Heatmap] Declared Width={data.NoOfColumns}, Height={data.NoOfRows}");

            if (width <= 0 || height <= 0)
                throw new InvalidOperationException("Cannot create bitmap: no valid pixel matrix.");

            // Compute Z range
            var allValues = data.PixelValues.SelectMany(row => row);
            double minZ = allValues.Min();
            double maxZ = allValues.Max();
            if (Math.Abs(maxZ - minZ) < 1e-9)
                maxZ = minZ + 1e-6; // avoid division by zero

            Debug.WriteLine($"[Heatmap] Z range: min={minZ}, max={maxZ}");

            // Create bitmap
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            string mode = SelectedGradientMode ?? "Default";
            double refDose = ReferenceDose;

            for (int y = 0; y < height; y++)
            {
                int flippedY = height - 1 - y;
                for (int x = 0; x < width; x++)
                {
                    double value = data.PixelValues[y][x];
                    Color c = GetColorFromGradient(value, minZ, maxZ, mode, refDose);

                    int offset = flippedY * stride + x * 4;
                    pixels[offset + 0] = c.B;
                    pixels[offset + 1] = c.G;
                    pixels[offset + 2] = c.R;
                    pixels[offset + 3] = c.A;
                }
            }

            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return wb;
        }



        // Ensure consistency between NoOfColumns/NoOfRows and PixelValues
        private static void ValidateAndFixDimensions(OpgFileData data)
        {
            if (data.PixelValues == null)
                data.PixelValues = new List<List<double>>();

            // Determine actual row count
            int actualRows = data.PixelValues.Count;

            // Determine actual column count
            int actualCols = 0;
            if (actualRows > 0)
            {
                int minCols = data.PixelValues.Min(r => r.Count);
                int maxCols = data.PixelValues.Max(r => r.Count);

                // If inconsistent row lengths → normalize to smallest
                if (minCols != maxCols)
                {
                    for (int i = 0; i < data.PixelValues.Count; i++)
                    {
                        if (data.PixelValues[i].Count > minCols)
                            data.PixelValues[i] = data.PixelValues[i].Take(minCols).ToList();
                    }
                    actualCols = minCols;
                }
                else
                {
                    actualCols = maxCols;
                }
            }

            // Always overwrite metadata with real values
            data.NoOfRows = actualRows;
            data.NoOfColumns = actualCols;
        }


        private static Color InterpolateColor(double t, Tuple<double, Color>[] stops)
        {
            for (int i = 0; i < stops.Length - 1; i++)
            {
                double startT = stops[i].Item1;
                Color startColor = stops[i].Item2;
                double endT = stops[i + 1].Item1;
                Color endColor = stops[i + 1].Item2;

                if (t >= startT && t <= endT)
                {
                    double localT = (t - startT) / (endT - startT);
                    byte r = (byte)(startColor.R + (endColor.R - startColor.R) * localT);
                    byte g = (byte)(startColor.G + (endColor.G - startColor.G) * localT);
                    byte b = (byte)(startColor.B + (endColor.B - startColor.B) * localT);
                    return Color.FromArgb(255, r, g, b);
                }
            }

            return stops[stops.Length - 1].Item2;
        }

        public static Color GetColorFromGradient(double value, double minZ, double maxZ, string mode, double refDose = 1.0)
        {
            if (refDose <= 0)
                refDose = 1000;

            double t = 0;
            if (maxZ > minZ)
            {
                t = (value - minZ) / (maxZ - minZ);
                if (t < 0) t = 0;
                if (t > 1) t = 1;
            }

            switch (mode)
            {
                case "Default":
                    return InterpolateColor(t, new[]
                    {
                Tuple.Create(0.0, Colors.Green),
                Tuple.Create(0.25, Colors.Blue),
                Tuple.Create(0.5, Colors.Yellow),
                Tuple.Create(0.75, Colors.Orange),
                Tuple.Create(1.0, Colors.Red)
            });

                case "Bipolar":
                    double maxAbs = Math.Max(Math.Abs(minZ), Math.Abs(maxZ));
                    double absValue = Math.Abs(value);
                    double normalizedT = maxAbs > 0 ? absValue / maxAbs : 0;
                    if (normalizedT < 0) normalizedT = 0;
                    if (normalizedT > 1) normalizedT = 1;

                    return InterpolateColor(normalizedT, new[]
                    {
                Tuple.Create(0.0, Colors.Green),
                Tuple.Create(0.33, Colors.Blue),
                Tuple.Create(0.66, Colors.Yellow),
                Tuple.Create(1.0, Colors.Red)
            });

                case "Stepped":
                    double dosePercent = (value / (refDose * 1000)) * 100.0;

                    if (dosePercent < 10) return Colors.Transparent;
                    else if (dosePercent < 30) return Color.FromRgb(164, 164, 0);
                    else if (dosePercent < 50) return Colors.Brown;
                    else if (dosePercent < 70) return Colors.DarkBlue;
                    else if (dosePercent < 80) return Colors.Orange;
                    else if (dosePercent < 90) return Colors.Blue;
                    else if (dosePercent < 95) return Colors.Cyan;
                    else if (dosePercent < 100) return Colors.Green;
                    else if (dosePercent < 105) return Colors.LightGreen;
                    else if (dosePercent < 110) return Colors.Red;
                    else return Colors.Magenta;

                default:
                    return Colors.Magenta;
            }
        }

        private async Task<OpgFileData> LoadOpgDataAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        Debug.WriteLine($"[OPG Loader] File does not exist: {filePath}");
                        return null;
                    }

                    string[] lines = File.ReadAllLines(filePath);
                    Debug.WriteLine($"[OPG Loader] Lines read: {lines.Length}");

                    if (lines.Length == 0)
                    {
                        Debug.WriteLine("[OPG Loader] File is empty!");
                        return null;
                    }

                    var fileContent = string.Join("\n", lines);
                    Debug.WriteLine($"[OPG Loader] File length (chars): {fileContent.Length}");

                    var result = new OpgFileData();

                    // Metadata
                    result.ImageName = Path.GetFileName(filePath);
                    result.Energy = ExtractMetadata(fileContent, "Energy");
                    result.DataUnit = ExtractMetadata(fileContent, "Data Unit");
                    Debug.WriteLine($"[OPG Loader] Metadata: Energy={result.Energy}, Unit={result.DataUnit}");

                    var rawFactor = ExtractMetadata(fileContent, "Data Factor")?.Replace(",", ".");
                    if (double.TryParse(rawFactor, NumberStyles.Any, CultureInfo.InvariantCulture, out double dataFactor))
                        result.DataFactor = dataFactor;

                    if (int.TryParse(ExtractMetadata(fileContent, "No. of Columns"), out int cols))
                        result.NoOfColumns = cols;

                    if (int.TryParse(ExtractMetadata(fileContent, "No. of Rows"), out int rows))
                        result.NoOfRows = rows;

                    result.FFF = Regex.IsMatch(fileContent, "<FFF>true</FFF>", RegexOptions.IgnoreCase);
                    Debug.WriteLine($"[OPG Loader] Declared cols={result.NoOfColumns}, rows={result.NoOfRows}");

                    // X coordinates
                    int xHeaderIndex = Array.FindIndex(lines, line => line.Trim().StartsWith("X[mm]"));
                    if (xHeaderIndex < 0)
                    {
                        Debug.WriteLine("[OPG Loader] ERROR: X[mm] header not found.");
                        return null;
                    }

                    var xLine = lines[xHeaderIndex];
                    var fullX = Regex.Matches(xLine, @"-?\d+\.?\d*")
                                .Cast<Match>()
                                .Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture))
                                .ToList();
                    Debug.WriteLine($"[OPG Loader] Parsed X values: {fullX.Count}");

                    // Y + matrix
                    var fullY = new List<double>();
                    var fullPixelMatrix = new List<List<double>>();

                    for (int i = xHeaderIndex + 1; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i]) || !Regex.IsMatch(lines[i], @"-?\d+\.?\d*"))
                            continue;

                        var tokens = Regex.Matches(lines[i], @"-?\d+\.?\d*")
                                      .Cast<Match>()
                                      .Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture))
                                      .ToList();

                        if (tokens.Count != fullX.Count + 1)
                        {
                            Debug.WriteLine($"[OPG Loader] Skipping row {i} (expected {fullX.Count + 1} tokens, got {tokens.Count})");
                            continue;
                        }

                        fullY.Add(tokens[0]);
                        fullPixelMatrix.Add(tokens.Skip(1).ToList());
                    }

                    Debug.WriteLine($"[OPG Loader] Parsed Y={fullY.Count}, MatrixRows={fullPixelMatrix.Count}");

                    // Validation
                    if (fullX.Count == 0 || fullY.Count == 0 || fullPixelMatrix.Count == 0)
                    {
                        Debug.WriteLine("[OPG Loader] ERROR: No valid dose data found.");
                        return null;
                    }

                    // Assign to result
                    result.X = fullX;
                    result.Y = fullY;
                    result.PixelValues = fullPixelMatrix;

                    ValidateAndFixDimensions(result);
                    Debug.WriteLine($"[OPG Loader] Final dims: {result.NoOfColumns} cols, {result.NoOfRows} rows");

                    return result;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OPG Loader] EXCEPTION: {ex}");
                    return null;
                }
            });
        }

        private void RefreshDisplay()
        {
            var group = new Model3DGroup();

            if (IsSingleDisplay)
            {
                if (_model.ReferenceData != null)
                {
                    SurfaceModelContent = BuildSurfaceModel(_model.ReferenceData);
                    if (SurfaceModelContent != null)
                        group.Children.Add(SurfaceModelContent);
                }
            }
            else if (IsDualDisplay)
            {
                if (_model.ReferenceData != null && _model.CompareData != null)
                {
                    var dualModelGroup = BuildDualDisplayModel(_model.ReferenceData, _model.CompareData);
                    group.Children.Add(dualModelGroup);
                }
            }
            else if (IsDoseDifference)
            {
                if (_model.ReferenceData != null && _model.CompareData != null)
                {
                    var diffModel = BuildDoseDifferenceModel(_model.ReferenceData, _model.CompareData);
                    group.Children.Add(diffModel);
                }
            }

            DisplayModelGroup = group;
            ZoomToFitAction?.Invoke();
        }



        private Model3DGroup BuildDualDisplayModel(OpgFileData reference, OpgFileData compare)
        {
            var group = new Model3DGroup();

            // Reference model: orange
            var refMesh = BuildMesh(reference, ComputeZScale(reference),
                                    (reference.X.First() + reference.X.Last()) / 2,
                                    (reference.Y.First() + reference.Y.Last()) / 2,
                                    (reference.PixelValues.SelectMany(r => r).Max() + reference.PixelValues.SelectMany(r => r).Min()) / 2);
            var refMaterial = MaterialHelper.CreateMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 165, 0)));
            group.Children.Add(new GeometryModel3D { Geometry = refMesh, Material = refMaterial, BackMaterial = refMaterial });

            // Compare model: blue
            var cmpMesh = BuildMesh(compare, ComputeZScale(compare),
                                    (compare.X.First() + compare.X.Last()) / 2,
                                    (compare.Y.First() + compare.Y.Last()) / 2,
                                    (compare.PixelValues.SelectMany(r => r).Max() + compare.PixelValues.SelectMany(r => r).Min()) / 2);
            var cmpMaterial = MaterialHelper.CreateMaterial(new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)));
            group.Children.Add(new GeometryModel3D { Geometry = cmpMesh, Material = cmpMaterial, BackMaterial = cmpMaterial });

            return group;
        }


        private GeometryModel3D BuildDoseDifferenceModel(OpgFileData reference, OpgFileData compare)
        {
            // Build matrix with reference - compare values
            int rows = Math.Min(reference.NoOfRows, compare.NoOfRows);
            int cols = Math.Min(reference.NoOfColumns, compare.NoOfColumns);

            var diffData = new OpgFileData
            {
                X = reference.X.Take(cols).ToList(),
                Y = reference.Y.Take(rows).ToList(),
                NoOfRows = rows,
                NoOfColumns = cols,
                PixelValues = new List<List<double>>()
            };

            for (int y = 0; y < rows; y++)
            {
                var row = new List<double>();
                for (int x = 0; x < cols; x++)
                {
                    row.Add(reference.PixelValues[y][x] - compare.PixelValues[y][x]);
                }
                diffData.PixelValues.Add(row);
            }

            return BuildSurfaceModel(diffData); // Apply gradient coloring
        }
        private void TakeScreenshot()
        {
            if (Viewport == null)
            {
                MessageBox.Show("Viewport not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int width = (int)Viewport.ActualWidth;
                int height = (int)Viewport.ActualHeight;

                if (width == 0 || height == 0)
                {
                    MessageBox.Show("Viewport has invalid size.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(Viewport);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string filePath = Path.Combine(folder, $"DoseViewer_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                // Copy parent folder path to clipboard
                string parentFolder = Path.GetDirectoryName(filePath);
                Clipboard.SetText(parentFolder);

                ShowToast($"Screenshot saved:\n{filePath}\n\nParent folder copied to clipboard.", 2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to take screenshot:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void ShowToast(string message, int duration = 3) => _toastService.ShowToast(message, duration);

        /// <summary>
        /// Extracts metadata fields from .opg file content.
        /// </summary>
        private static string ExtractMetadata(string fileContent, string tag)
        {
            var match = Regex.Match(fileContent, $"<{tag}>(.*?)</{tag}>", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        // -----------------------------
        // Export
        // -----------------------------
        private void Export3DModel()
        {
            if (_model.ReferenceData == null)
            {
                MessageBox.Show("Load a reference file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string targetFolder = _exporter.GetExportPath(DateTime.Now.ToString("yyyyMMdd"));
            if (string.IsNullOrEmpty(targetFolder)) return;

            try
            {
                // TODO: save mesh to STL/OBJ using HelixToolkit exporter
                MessageBox.Show($"3D Model would be exported to:\n{targetFolder}", "Export 3D", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export 3D model:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
