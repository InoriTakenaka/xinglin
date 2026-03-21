using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace xinglin.Models.CoreEntities
{
    public enum ControlType
    {
        Label,
        TextBox,
        CheckBox,
        RadioButton,
        ComboBox,
        DateTimePicker,
        Table,
        Image,
        Line,
        Rectangle
    }

    public partial class ControlElement : ObservableObject
    {
        [ObservableProperty]
        private string _elementId;

        [ObservableProperty]
        private ControlType _type;

        [ObservableProperty]
        private string _displayName;

        [ObservableProperty]
        private string _text;

        [ObservableProperty]
        private string _selectedValue;

        [ObservableProperty]
        private double _x;

        [ObservableProperty]
        private double _y;

        [ObservableProperty]
        private double _width;

        [ObservableProperty]
        private double _height;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private string _bindingPath;

        [ObservableProperty]
        private string _fontFamily = "Microsoft YaHei";

        [ObservableProperty]
        private double _fontSize = 12;

        [ObservableProperty]
        private bool _isBold;

        [ObservableProperty]
        private bool _isItalic;

        [ObservableProperty]
        private bool _isUnderline;

        [ObservableProperty]
        private bool _isChecked;

        public ControlElement()
        {
            ElementId = Guid.NewGuid().ToString();
        }
    }
}
