using Filmauswertung_ModernUI.Core.Interfaces;
using Filmauswertung_ModernUI.MVVM.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace Filmauswertung_ModernUI.Services
{
    public class ToastService : IToastService
    {
        public void ShowToast(string message, int durationInSeconds = 3)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new ToastWindow
                {
                    ToastMessage = message,
                    ToastDuration = TimeSpan.FromSeconds(durationInSeconds)
                };
                toast.ShowToast();
            });
        }
    }
}
