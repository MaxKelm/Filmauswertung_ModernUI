using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;  // Make sure this is included at the top of your file

namespace Filmauswertung_ModernUI.Services
{
    public class ImageSegmentationService
    {
        public List<byte> Get10x10RegionPixels(byte[] pixels, int width, int height, int stride, int centerX, int centerY)
        {
            List<byte> intensities = new List<byte>();
            int halfSize = 5;

            for (int y = centerY - halfSize; y < centerY + halfSize; y++)
            {
                if (y < 0 || y >= height) continue;
                for (int x = centerX - halfSize; x < centerX + halfSize; x++)
                {
                    if (x < 0 || x >= width) continue;

                    int idx = y * stride + x * 4;
                    byte b = pixels[idx];
                    byte g = pixels[idx + 1];
                    byte r = pixels[idx + 2];

                    byte gray = (byte)((0.299 * r) + (0.587 * g) + (0.114 * b));
                    intensities.Add(gray);
                }
            }

            return intensities;
        }

        public byte CalculateMedian(List<byte> values)
        {
            if (values == null || values.Count == 0) return 0;
            values.Sort();
            int mid = values.Count / 2;
            return values.Count % 2 == 0 ? (byte)((values[mid - 1] + values[mid]) / 2) : values[mid];
        }

        public bool[,] RegionGrow(byte[] pixels, int width, int height, int stride,
                          int centerX, int centerY, byte median, byte tolerance = 15)
        {
            if (centerX < 0 || centerX >= width || centerY < 0 || centerY >= height)
            {
                Debug.WriteLine($"Seed point ({centerX},{centerY}) is out of image bounds.");
                return new bool[width, height]; // early exit for invalid seed point
            }

            bool[,] visited = new bool[width, height];
            bool[,] mask = new bool[width, height];

            int min = Math.Max(0, median - tolerance);
            int max = Math.Min(255, median + tolerance);

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(centerX, centerY));
            visited[centerX, centerY] = true;

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                int x = (int)p.X;
                int y = (int)p.Y;

                int idx = y * stride + x * 4;

                if (idx + 2 >= pixels.Length)
                {
                    continue;
                }

                byte b = pixels[idx];
                byte g = pixels[idx + 1];
                byte r = pixels[idx + 2];
                byte gray = (byte)((0.299 * r) + (0.587 * g) + (0.114 * b));

                if (gray >= min && gray <= max)
                {
                    mask[x, y] = true;

                    foreach (var offset in new[] { new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1) })
                    {
                        int nx = x + (int)offset.X;
                        int ny = y + (int)offset.Y;

                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        {
                            continue;
                        }

                        if (!visited[nx, ny])
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue(new Point(nx, ny));
                        }
                    }
                }
            }

            return mask;
        }


        public Rect GetBoundingBox(bool[,] mask, int width, int height)
        {
            int minX = width, maxX = 0, minY = height, maxY = 0;
            bool found = false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (mask[x, y])
                    {
                        found = true;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            return found ? new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1) : Rect.Empty;
        }
    }
}
