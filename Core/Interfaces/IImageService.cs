using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Filmauswertung_ModernUI.Core.Interfaces
{
    public interface IImageService
    {
        BitmapImage LoadImage(string path);
    }

}
