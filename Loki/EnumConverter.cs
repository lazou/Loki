using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace Loki
{
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (targetType.IsEnum)
            {
                // int -> Enum
                return Enum.ToObject(targetType, value);
            }

            if (value.GetType().IsEnum)
            {
                // Enum -> int
                return System.Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Same conversion for both directions
            return Convert(value, targetType, parameter, culture);
        }
    }
}
