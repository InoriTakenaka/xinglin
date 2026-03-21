using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using xinglin.Models.CoreEntities;

namespace xinglin.ViewModels
{
    public partial class ToolboxViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<ControlType> _availableControls;

        public ToolboxViewModel()
        {
            AvailableControls = new List<ControlType>
            {
                ControlType.Label,
                ControlType.TextBox,
                ControlType.CheckBox,
                ControlType.RadioButton,
                ControlType.ComboBox,
                ControlType.DateTimePicker,
                ControlType.Table,
                ControlType.Image,
                ControlType.Line,
                ControlType.Rectangle
            };
        }

        public ControlElement CreateControl(ControlType type)
        {
            var control = new ControlElement
            {
                ElementId = System.Guid.NewGuid().ToString(),
                Type = type,
                DisplayName = GetControlDisplayName(type),
                Text = GetControlDefaultText(type),
                SelectedValue = GetControlDefaultValue(type),
                Width = 100,
                Height = 30
            };

            if (type == ControlType.Table)
            {
                return new TableElement
                {
                    ElementId = System.Guid.NewGuid().ToString(),
                    Type = type,
                    DisplayName = GetControlDisplayName(type),
                    Text = GetControlDefaultText(type),
                    Width = 200,
                    Height = 100,
                    Columns = new ObservableCollection<TableColumn>
                    {
                        new TableColumn { Name = "列1", Width = 100, Index = 0 },
                        new TableColumn { Name = "列2", Width = 100, Index = 1 }
                    },
                    RowCount = 2
                };
            }

            return control;
        }

        private string GetControlDefaultText(ControlType type)
        {
            return type switch
            {
                ControlType.Label => "标签文本",
                ControlType.TextBox => "文本内容",
                ControlType.CheckBox => "复选框选项",
                ControlType.RadioButton => "单选按钮选项",
                ControlType.ComboBox => "下拉框选项",
                ControlType.DateTimePicker => "2026-01-01",
                ControlType.Table => "表格",
                ControlType.Image => "图片",
                ControlType.Line => "线条",
                ControlType.Rectangle => "矩形",
                _ => type.ToString()
            };
        }

        private string GetControlDefaultValue(ControlType type)
        {
            return type switch
            {
                ControlType.ComboBox => "默认选项",
                _ => string.Empty
            };
        }

        private string GetControlDisplayName(ControlType type)
        {
            return type switch
            {
                ControlType.Label => "标签",
                ControlType.TextBox => "文本框",
                ControlType.CheckBox => "复选框",
                ControlType.RadioButton => "单选按钮",
                ControlType.ComboBox => "下拉框",
                ControlType.DateTimePicker => "日期时间选择器",
                ControlType.Table => "表格",
                ControlType.Image => "图片",
                ControlType.Line => "线条",
                ControlType.Rectangle => "矩形",
                _ => type.ToString()
            };
        }
    }
}
