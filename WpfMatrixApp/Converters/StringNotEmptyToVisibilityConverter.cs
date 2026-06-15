using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfMatrixApp.Converters
{
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return !string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
