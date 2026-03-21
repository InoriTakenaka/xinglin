using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace xinglin.Utils
{
    public class BoolToFontStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isItalic && isItalic)
            {
                return FontStyles.Italic;
            }
            return FontStyles.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FontStyle fontStyle)
            {
                return fontStyle == FontStyles.Italic;
            }
            return false;
        }
    }
}