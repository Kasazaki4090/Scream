using System;
using System.Globalization;
using System.Windows.Data;

namespace Scream.Converters
{
    public class IntBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((int)value != 0)
            {
                switch (parameter)
                {
                    case "reverse":
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                switch (parameter)
                {
                    case "reverse":
                        return false;
                    default:
                        return true;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
