using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Scream.Converters
{
    public class DicJson : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Utilities.javaScriptSerializer.Deserialize<dynamic>(value.ToString());
        }
    }
}
