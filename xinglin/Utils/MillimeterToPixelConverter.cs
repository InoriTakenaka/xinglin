using System;
using System.Globalization;
using System.Windows.Data;

namespace xinglin.Utils
{
    public class MillimeterToPixelConverter : IValueConverter
    {
        // 标准转换比例：1英寸 = 25.4毫米 = 96 DIP
        private const double MM_TO_PIXEL = 96.0 / 25.4;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double millimeters)
            {
                return millimeters * MM_TO_PIXEL;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double pixels)
            {
                return pixels / MM_TO_PIXEL;
            }
            return 0;
        }
    }
}