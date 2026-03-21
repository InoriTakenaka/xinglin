using System;
using System.Globalization;
using System.Windows.Data;

namespace xinglin.Utils
{
    public class ZoomToBorderThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double zoomLevel)
            {
                // 基础边框厚度
                double baseThickness = 1.0;
                // 根据缩放级别调整边框厚度
                // 当缩小时，边框厚度应该减小，当放大时，边框厚度应该增加
                double scale = zoomLevel / 100.0;
                double adjustedThickness = baseThickness / scale;
                // 确保边框厚度不会太小
                return Math.Max(adjustedThickness, 0.5);
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}