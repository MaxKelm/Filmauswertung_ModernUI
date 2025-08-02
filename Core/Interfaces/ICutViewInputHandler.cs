using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Filmauswertung_ModernUI.Core.Interfaces
{
    public interface ICutViewInputHandler
    {
        void OnMouseLeftButtonDown(Point uiPoint);
        void OnMouseMove(Point uiPoint);
        void OnMouseLeftButtonUp(Point uiPoint);
    }
}
