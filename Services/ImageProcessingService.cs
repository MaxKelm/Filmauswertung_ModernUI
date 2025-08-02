using Filmauswertung_ModernUI.Core.Interfaces;
using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class ImageProcessingService : IImageProcessingService
{
    public BitmapImage AdjustContrast(BitmapImage sourceImage, int contrastLevel)
    {
        if (sourceImage == null)
        {
            throw new ArgumentNullException(nameof(sourceImage));
        }

        try
        {
            double factor = (contrastLevel - 1) / 3.0;  // factor from 0 to 1

            BitmapSource enhanced = EnhanceContrast(sourceImage, factor);

            BitmapImage result = BitmapSourceToBitmapImage(enhanced);
            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private BitmapSource EnhanceContrast(BitmapSource source, double factor)
    {
        if (source.Format != PixelFormats.Bgra32)
        {
            source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        }

        int width = source.PixelWidth;
        int height = source.PixelHeight;
        int stride = width * 4;
        byte[] pixelData = new byte[height * stride];
        source.CopyPixels(pixelData, stride, 0);

        // Create histograms for R, G, B
        int[][] histograms = new int[3][];
        for (int i = 0; i < 3; i++)
            histograms[i] = new int[256];

        for (int i = 0; i < pixelData.Length; i += 4)
        {
            histograms[0][pixelData[i + 0]]++; // B
            histograms[1][pixelData[i + 1]]++; // G
            histograms[2][pixelData[i + 2]]++; // R
        }

        // Calculate cumulative distribution function (CDF)
        int[][] cdf = new int[3][];
        for (int c = 0; c < 3; c++)
        {
            cdf[c] = new int[256];
            cdf[c][0] = histograms[c][0];
            for (int i = 1; i < 256; i++)
            {
                cdf[c][i] = cdf[c][i - 1] + histograms[c][i];
            }
        }

        // Normalize CDF to [0, 255]
        byte[][] lut = new byte[3][];
        for (int c = 0; c < 3; c++)
        {
            lut[c] = new byte[256];
            int cdfMin = Array.Find(cdf[c], val => val != 0);
            double totalPixels = width * height;
            for (int i = 0; i < 256; i++)
            {
                double value = (cdf[c][i] - cdfMin) / (totalPixels - cdfMin);
                value = value * 255;

                // Blend with original value using 'factor'
                lut[c][i] = (byte)(factor * value + (1 - factor) * i);
            }
        }

        // Apply LUT
        for (int i = 0; i < pixelData.Length; i += 4)
        {
            pixelData[i + 0] = lut[0][pixelData[i + 0]]; // B
            pixelData[i + 1] = lut[1][pixelData[i + 1]]; // G
            pixelData[i + 2] = lut[2][pixelData[i + 2]]; // R
        }

        return BitmapSource.Create(width, height, source.DpiX, source.DpiY,
                                    PixelFormats.Bgra32, null, pixelData, stride);
    }


    private BitmapImage BitmapSourceToBitmapImage(BitmapSource bitmapSource)
    {
        try
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
