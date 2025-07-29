using Filmauswertung_ModernUI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.Services
{
    public class ImageService : IImageService
    {
        public BitmapImage LoadImage(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Image file not found", path);

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze(); // Safe for cross-thread access

            return image;
        }
    }
}
