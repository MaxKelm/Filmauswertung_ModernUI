using Filmauswertung_ModernUI.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filmauswertung_ModernUI.Core.Interfaces
{
    internal interface IOpgLoader
    {
        Task<OpgFileData> LoadOpgAsync(string path);
    }
}
