using System.Windows;

namespace Filmauswertung_ModernUI.Core
{
    internal class Extensions
    {
        public static readonly DependencyProperty Icon = DependencyProperty.RegisterAttached(
            "Icon",
            typeof(string),
            typeof(Extensions),
            new PropertyMetadata(default(string)));

        public static void SetIcon(UIElement element, string value)
        {
            element.SetValue(Icon, value);
        }

        public static string GetIcon(UIElement element)
        {
            return (string)element.GetValue(Icon);
        }

        public static readonly DependencyProperty DisplayValueProperty = DependencyProperty.RegisterAttached(
            "DisplayValue",
            typeof(string),
            typeof(Extensions),
            new PropertyMetadata(default(string)));

        public static void SetDisplayValue(UIElement element, string value) => element.SetValue(DisplayValueProperty, value);
        public static string GetDisplayValue(UIElement element) => (string)element.GetValue(DisplayValueProperty);
    }
}
