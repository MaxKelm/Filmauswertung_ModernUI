using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filmauswertung_ModernUI.Core.Interfaces
{
    public interface IToastService
    {
        void ShowToast(string message, int durationInSeconds = 3);
    }
}
