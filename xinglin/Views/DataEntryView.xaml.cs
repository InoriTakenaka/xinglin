using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using xinglin.ViewModels;
using xinglin.Models.CoreEntities;

namespace xinglin.Views
{
    public partial class DataEntryView : UserControl
    {
        private DataEntryViewModel _viewModel;
        private StackPanel? _dataEntryPanel;
        private static readonly StringToDateConverter _dateConverter = new StringToDateConverter();

        public DataEntryView(DataEntryViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _viewModel = viewModel;
            
            // 查找数据录入面板
            _dataEntryPanel = FindName("DataEntryPanel") as StackPanel;
            
            // 订阅模板变化事件
            if (viewModel != null)
            {
                viewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(viewModel.CurrentTemplate))
                    {
                        InitializeFromTemplate();
                    }
                };
            }
        }

        private void InitializeFromTemplate()
        {
            if (_viewModel == null || _viewModel.CurrentTemplate == null || _dataEntryPanel == null)
                return;

            int editableCount = _viewModel.CurrentTemplate.Layout?.EditableElements?.Count ?? 0;
            int fixedCount = _viewModel.CurrentTemplate.Layout?.FixedElements?.Count ?? 0;

            System.Diagnostics.Debug.WriteLine($"开始初始化数据录入视图\n模板ID: {_viewModel.CurrentTemplate.TemplateId}\n可编辑元素数量: {editableCount}\n固定元素数量: {fixedCount}");

            // 确保布局存在
            if (_viewModel.CurrentTemplate.Layout == null)
            {
                _viewModel.CurrentTemplate.Layout = new xinglin.Models.CoreEntities.LayoutMetadata();
                System.Diagnostics.Debug.WriteLine("创建了新的布局元数据");
            }

            // 清空现有控件
            _dataEntryPanel.Children.Clear();

            // 添加标题
            var titleBlock = new TextBlock
            {
                Text = "数据录入",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(10)
            };
            _dataEntryPanel.Children.Add(titleBlock);

            // 遍历可编辑元素并创建对应控件
            if (_viewModel.CurrentTemplate.Layout != null && _viewModel.CurrentTemplate.Layout.EditableElements != null)
            {
                foreach (var element in _viewModel.CurrentTemplate.Layout.EditableElements)
                {
                    // Table 类型单独处理（占整行，不用 label+input 的 Grid 布局）
                    if (element.Type == ControlType.Table)
                    {
                        var tableElement = element as TableElement;
                        if (tableElement != null)
                        {
                            _dataEntryPanel.Children.Add(CreateTableControl(tableElement));
                        }
                        continue;
                    }

                    var grid = new Grid { Margin = new Thickness(10) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // 添加标签
                    var label = new TextBlock
                    {
                        Text = $"{element.DisplayName}:",
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(label, 0);
                    grid.Children.Add(label);

                    // 根据控件类型创建对应输入控件
                    FrameworkElement? inputControl = null;
                    switch (element.Type)
                    {
                        case ControlType.TextBox:
                            var textBox = new TextBox();
                            textBox.SetBinding(TextBox.TextProperty, new Binding("Text") { Source = element, Mode = BindingMode.TwoWay });
                            inputControl = textBox;
                            break;

                        case ControlType.CheckBox:
                            var checkBox = new CheckBox { Content = element.DisplayName };
                            checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding("IsChecked") { Source = element, Mode = BindingMode.TwoWay });
                            inputControl = checkBox;
                            break;

                        case ControlType.ComboBox:
                            var comboBox = new ComboBox();
                            comboBox.SetBinding(ComboBox.TextProperty, new Binding("SelectedValue") { Source = element, Mode = BindingMode.TwoWay });
                            // 从强类型 Config.ComboBoxOptions 加载选项
                            var config = _viewModel.CurrentTemplate.Config;
                            if (config != null && !string.IsNullOrEmpty(element.BindingPath)
                                && config.ComboBoxOptions.TryGetValue(element.BindingPath, out var options))
                            {
                                foreach (var option in options)
                                    comboBox.Items.Add(option);
                            }
                            inputControl = comboBox;
                            break;

                        case ControlType.DateTimePicker:
                            var datePicker = new DatePicker();
                            datePicker.SetBinding(DatePicker.SelectedDateProperty,
                                new Binding("Text")
                                {
                                    Source = element,
                                    Mode = BindingMode.TwoWay,
                                    Converter = _dateConverter
                                });
                            inputControl = datePicker;
                            break;

                        default:
                            var defaultTextBox = new TextBox();
                            defaultTextBox.SetBinding(TextBox.TextProperty, new Binding("Text") { Source = element, Mode = BindingMode.TwoWay });
                            inputControl = defaultTextBox;
                            break;
                    }

                    if (inputControl != null)
                    {
                        Grid.SetColumn(inputControl, 1);
                        grid.Children.Add(inputControl);
                        _dataEntryPanel.Children.Add(grid);
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"数据录入视图初始化完成\n创建了 {_dataEntryPanel.Children.Count - 1} 个输入控件");
        }

        /// <summary>
        /// 为 TableElement 创建 DataGrid 控件，预填空行并绑定行数据。
        /// </summary>
        private FrameworkElement CreateTableControl(TableElement tableElement)
        {
            // 若 Rows 为空，按 RowCount 预填空行
            if (tableElement.Rows.Count == 0)
            {
                for (int i = 0; i < tableElement.RowCount; i++)
                {
                    var row = new TableRow { RowIndex = i };
                    foreach (var col in tableElement.Columns)
                        row.CellValues[col.Name] = col.DefaultValue ?? string.Empty;
                    tableElement.Rows.Add(row);
                }
            }

            var panel = new StackPanel { Margin = new Thickness(10) };

            var header = new TextBlock
            {
                Text = $"{tableElement.DisplayName}:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            panel.Children.Add(header);

            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = tableElement.AllowAddRows,
                ItemsSource = tableElement.Rows,
                MaxHeight = 300
            };

            foreach (var col in tableElement.Columns)
            {
                var column = new DataGridTextColumn
                {
                    Header = col.Name,
                    Width = new DataGridLength(col.Width > 0 ? col.Width : 80),
                    IsReadOnly = !col.IsEditable,
                    Binding = new Binding($"CellValues[{col.Name}]")
                };
                dataGrid.Columns.Add(column);
            }

            panel.Children.Add(dataGrid);
            return panel;
        }
    }
}
