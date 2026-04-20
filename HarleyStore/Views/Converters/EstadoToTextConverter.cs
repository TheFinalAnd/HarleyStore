using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HarleyStore.Views.Converters
{
    public class EstadoToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "Desconocido";
            if (!long.TryParse(value.ToString(), out var id)) return "Desconocido";

            return id switch
            {
                4 => "Pendiente",
                5 => "Aceptada",
                6 => "Rechazada",
                _ => "Desconocido"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
