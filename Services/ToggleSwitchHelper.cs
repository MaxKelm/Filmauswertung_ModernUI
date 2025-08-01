using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Filmauswertung_ModernUI.Services
{
    public static class ToggleSwitchHelper
    {
        public static readonly DependencyProperty IconUncheckedProperty =
            DependencyProperty.RegisterAttached("IconUnchecked", typeof(string), typeof(ToggleSwitchHelper), new PropertyMetadata(string.Empty));

        public static string GetIconUnchecked(DependencyObject obj) => (string)obj.GetValue(IconUncheckedProperty);
        public static void SetIconUnchecked(DependencyObject obj, string value) => obj.SetValue(IconUncheckedProperty, value);

        public static readonly DependencyProperty IconCheckedProperty =
            DependencyProperty.RegisterAttached("IconChecked", typeof(string), typeof(ToggleSwitchHelper), new PropertyMetadata(string.Empty));

        public static string GetIconChecked(DependencyObject obj) => (string)obj.GetValue(IconCheckedProperty);
        public static void SetIconChecked(DependencyObject obj, string value) => obj.SetValue(IconCheckedProperty, value);

        public static readonly DependencyProperty LabelUncheckedProperty =
            DependencyProperty.RegisterAttached("LabelUnchecked", typeof(string), typeof(ToggleSwitchHelper), new PropertyMetadata(string.Empty));

        public static string GetLabelUnchecked(DependencyObject obj) => (string)obj.GetValue(LabelUncheckedProperty);
        public static void SetLabelUnchecked(DependencyObject obj, string value) => obj.SetValue(LabelUncheckedProperty, value);

        public static readonly DependencyProperty LabelCheckedProperty =
            DependencyProperty.RegisterAttached("LabelChecked", typeof(string), typeof(ToggleSwitchHelper), new PropertyMetadata(string.Empty));

        public static string GetLabelChecked(DependencyObject obj) => (string)obj.GetValue(LabelCheckedProperty);
        public static void SetLabelChecked(DependencyObject obj, string value) => obj.SetValue(LabelCheckedProperty, value);
    }

}
