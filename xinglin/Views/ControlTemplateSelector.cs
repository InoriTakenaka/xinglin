using System.Windows;
using System.Windows.Controls;
using xinglin.Models.CoreEntities;

namespace xinglin.Views
{
    public class ControlTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LabelTemplate { get; set; }
        public DataTemplate TextBoxTemplate { get; set; }
        public DataTemplate ComboBoxTemplate { get; set; }
        public DataTemplate CheckBoxTemplate { get; set; }
        public DataTemplate RadioButtonTemplate { get; set; }
        public DataTemplate DateTimePickerTemplate { get; set; }
        public DataTemplate TableTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate LineTemplate { get; set; }
        public DataTemplate RectangleTemplate { get; set; }
        public DataTemplate DefaultControlTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ControlElement control)
            {
                switch (control.Type)
                {
                    case ControlType.Label:
                        return LabelTemplate ?? DefaultControlTemplate;
                    case ControlType.TextBox:
                        return TextBoxTemplate ?? DefaultControlTemplate;
                    case ControlType.ComboBox:
                        return ComboBoxTemplate ?? DefaultControlTemplate;
                    case ControlType.CheckBox:
                        return CheckBoxTemplate ?? DefaultControlTemplate;
                    case ControlType.RadioButton:
                        return RadioButtonTemplate ?? DefaultControlTemplate;
                    case ControlType.DateTimePicker:
                        return DateTimePickerTemplate ?? DefaultControlTemplate;
                    case ControlType.Table:
                        return TableTemplate ?? DefaultControlTemplate;
                    case ControlType.Image:
                        return ImageTemplate ?? DefaultControlTemplate;
                    case ControlType.Line:
                        return LineTemplate ?? DefaultControlTemplate;
                    case ControlType.Rectangle:
                        return RectangleTemplate ?? DefaultControlTemplate;
                    default:
                        return DefaultControlTemplate;
                }
            }
            return DefaultControlTemplate;
        }
    }
}