using System;
using System.Globalization;
using System.Windows.Data;

namespace Scream.Converters
{
    public class IntString : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int number;
            bool success = Int32.TryParse(value.ToString(), out number);
            if (success)
            {
                return number;
            }
            return 0;
        }
    }
}
