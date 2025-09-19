using Filmauswertung_ModernUI.MVVM.Model;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Filmauswertung_ModernUI.MVVM.Services
{
    public class DoseVisualizer
    {
        #region Surface & Mesh Generation

        public GeometryModel3D BuildSurfaceModel(OpgFileData data, string gradientMode, double referenceDose)
        {
            if (data?.PixelValues == null || data.NoOfRows <= 1 || data.NoOfColumns <= 1)
                return null;

            try
            {
                double scaleZ = OpgFileData.ComputeZScale(data);
                var center = GetCenter(data);
                var zRange = GetZRange(data);

                var mesh = BuildMesh(data, scaleZ, center.x, center.y, zRange.zCenter);

                var brush = new ImageBrush(CreateHeatmapBitmap(data, gradientMode, referenceDose))
                {
                    Stretch = Stretch.Fill,
                    ViewportUnits = BrushMappingMode.RelativeToBoundingBox
                };

                var material = MaterialHelper.CreateMaterial(brush);
                return new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                };
            }
            catch
            {
                return null;
            }
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

                    double u0 = (double)x / (cols - 1);
                    double u1 = (double)(x + 1) / (cols - 1);
                    double v0 = 1.0 - (double)y / (rows - 1);
                    double v1 = 1.0 - (double)(y + 1) / (rows - 1);

                    mb.AddQuad(p00, p10, p11, p01, new System.Windows.Point(u0, v0), new System.Windows.Point(u1, v0), new System.Windows.Point(u1, v1), new System.Windows.Point(u0, v1));
                }
            }

            return mb.ToMesh();
        }

        private (double x, double y) GetCenter(OpgFileData data)
        {
            return ((data.X.First() + data.X.Last()) / 2, (data.Y.First() + data.Y.Last()) / 2);
        }

        private (double zMin, double zMax, double zCenter) GetZRange(OpgFileData data)
        {
            var allValues = data.PixelValues.SelectMany(r => r);
            double min = allValues.Min();
            double max = allValues.Max();
            return (min, max, (min + max) / 2);
        }

        #endregion

        #region Heatmap

        public WriteableBitmap CreateHeatmapBitmap(OpgFileData data, string gradientMode, double referenceDose)
        {
            int height = data.PixelValues.Count;
            int width = height > 0 ? data.PixelValues.Min(r => r.Count) : 0;
            if (width <= 0 || height <= 0) throw new InvalidOperationException("Invalid pixel matrix");

            var allValues = data.PixelValues.SelectMany(r => r);
            double minZ = allValues.Min();
            double maxZ = allValues.Max();
            if (Math.Abs(maxZ - minZ) < 1e-9) maxZ = minZ + 1e-6;

            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            string mode = gradientMode ?? "Default";

            for (int y = 0; y < height; y++)
            {
                int flippedY = height - 1 - y;
                for (int x = 0; x < width; x++)
                {
                    double value = data.PixelValues[y][x];
                    Color c = DoseColorHelper.GetColorFromGradient(value, minZ, maxZ, mode, referenceDose);

                    int offset = flippedY * stride + x * 4;
                    pixels[offset + 0] = c.B;
                    pixels[offset + 1] = c.G;
                    pixels[offset + 2] = c.R;
                    pixels[offset + 3] = c.A;
                }
            }

            wb.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, stride, 0);
            return wb;
        }

        #endregion
    }

    public static class DoseColorHelper
    {
        public static Color GetColorFromGradient(double value, double minZ, double maxZ, string mode, double refDose = 1.0)
        {
            if (refDose <= 0) refDose = 1000;

            // Clamp manually
            double t;
            if (maxZ > minZ)
            {
                t = (value - minZ) / (maxZ - minZ);
                if (t < 0) t = 0;
                if (t > 1) t = 1;
            }
            else
            {
                t = 0;
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
                    double normalizedT = maxAbs > 0 ? Math.Abs(value) / maxAbs : 0;
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
                    return GetSteppedColor(value, refDose);

                case "Uniform":
                    // Always half-transparent orange
                    return Color.FromArgb(128, 255, 165, 0);

                default:
                    return Colors.Magenta;
            }
        }

        private static Color InterpolateColor(double t, Tuple<double, Color>[] stops)
        {
            for (int i = 0; i < stops.Length - 1; i++)
            {
                double startT = stops[i].Item1;
                double endT = stops[i + 1].Item1;
                if (t >= startT && t <= endT)
                {
                    double localT = (t - startT) / (endT - startT);
                    var startColor = stops[i].Item2;
                    var endColor = stops[i + 1].Item2;
                    byte r = (byte)(startColor.R + (endColor.R - startColor.R) * localT);
                    byte g = (byte)(startColor.G + (endColor.G - startColor.G) * localT);
                    byte b = (byte)(startColor.B + (endColor.B - startColor.B) * localT);
                    return Color.FromArgb(255, r, g, b);
                }
            }

            // Replace ^1 with stops.Length - 1
            return stops[stops.Length - 1].Item2;
        }


        private static Color GetSteppedColor(double value, double refDose)
        {
            double dosePercent = (value / (refDose * 1000)) * 100.0;

            if (dosePercent < 10) return Colors.Transparent;
            if (dosePercent < 30) return Color.FromRgb(164, 164, 0);
            if (dosePercent < 50) return Colors.Brown;
            if (dosePercent < 70) return Colors.DarkBlue;
            if (dosePercent < 80) return Colors.Orange;
            if (dosePercent < 90) return Colors.Blue;
            if (dosePercent < 95) return Colors.Cyan;
            if (dosePercent < 100) return Colors.Green;
            if (dosePercent < 105) return Colors.LightGreen;
            if (dosePercent < 110) return Colors.Red;

            return Colors.Magenta;
        }
    }
}
