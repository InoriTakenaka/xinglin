using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace xinglin.Views
{
    /// <summary>
    /// Converts null/non-null object values to Visibility.
    /// null → Collapsed, non-null → Visible
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
