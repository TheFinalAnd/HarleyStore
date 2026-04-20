using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HarleyStore.Views.Converters
{
    public class MilesToUnitsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float price)
            {
                return price * 1000;
            }
            if (value is decimal priceDecimal)
            {
                return priceDecimal * 1000;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0f;
            if (float.TryParse(value.ToString(), out var units))
            {
                return units / 1000f;
            }
            return value;
        }
    }
}
