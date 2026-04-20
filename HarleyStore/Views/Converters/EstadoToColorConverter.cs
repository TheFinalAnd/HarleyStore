using System;
using System.Globalization;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace HarleyStore.Views.Converters
{
    public class EstadoToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Colors.Gray;
            if (!long.TryParse(value.ToString(), out var id)) return Colors.Gray;

            // 4 = Pendiente (amarillo), 5 = Aceptada (verde), 6 = Rechazada (rojo)
            return id switch
            {
                4 => Colors.Goldenrod,
                5 => Colors.Green,
                6 => Colors.Red,
                _ => Colors.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
