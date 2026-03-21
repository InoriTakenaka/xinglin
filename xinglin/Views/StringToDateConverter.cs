using System;
using System.Globalization;
using System.Windows.Data;

namespace xinglin.Views
{
    /// <summary>
    /// 字符串（yyyy-MM-dd）与 DateTime? 之间的双向转换器，供 DatePicker 绑定使用。
    /// </summary>
    public class StringToDateConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && DateTime.TryParse(str, out var date))
                return (DateTime?)date;
            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
                return date.ToString("yyyy-MM-dd");
            return string.Empty;
        }
    }
}
