using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;

namespace Filmauswertung_ModernUI.Converters
{
    public class OffsetConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double original && double.TryParse(parameter?.ToString(), out double offset))
                    return original + offset;

                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
                throw new NotImplementedException();
    }
}
