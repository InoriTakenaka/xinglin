using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace xinglin.Views
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue;
            
            // 处理对象类型（如SelectedControl）
            if (value == null)
            {
                boolValue = false;
            }
            // 处理bool类型
            else if (value is bool)
            {
                boolValue = (bool)value;
            }
            // 其他类型视为true
            else
            {
                boolValue = true;
            }

            // 检查是否需要反转值
            if (parameter != null && parameter.ToString() == "Reverse")
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;

                // 检查是否需要反转值
                if (parameter != null && parameter.ToString() == "Reverse")
                {
                    result = !result;
                }

                return result;
            }
            return false;
        }
    }
}
