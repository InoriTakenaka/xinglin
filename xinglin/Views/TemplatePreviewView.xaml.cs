using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using xinglin.Models.CoreEntities;

namespace xinglin.Views
{
    public partial class TemplatePreviewView : UserControl
    {
        public TemplatePreviewView()
        {
            InitializeComponent();
        }

        public void SetTemplate(TemplateData template)
        {
            if (template != null && template.Layout != null)
            {
                // 设置Canvas大小
                PreviewCanvas.Width = template.Layout.PaperWidth;
                PreviewCanvas.Height = template.Layout.PaperHeight;

                // 清空Canvas
                PreviewCanvas.Children.Clear();

                // 添加固定元素
                foreach (var element in template.Layout.FixedElements)
                {
                    AddElementToCanvas(element);
                }

                // 添加可编辑元素
                foreach (var element in template.Layout.EditableElements)
                {
                    AddElementToCanvas(element);
                }
            }
        }

        private void AddElementToCanvas(ControlElement element)
        {
            if (element == null)
                return;

            // 创建元素的视觉表示
            Border border = new Border
            {
                Width = element.Width,
                Height = element.Height,
                BorderBrush = System.Windows.Media.Brushes.Black,
                BorderThickness = new Thickness(1),
                Background = GetElementBackground(element)
            };

            // 设置Canvas位置
            Canvas.SetLeft(border, element.X);
            Canvas.SetTop(border, element.Y);

            // 根据元素类型创建不同的内容
            UIElement content = CreateElementContent(element);
            border.Child = content;

            // 添加到Canvas
            PreviewCanvas.Children.Add(border);
        }

        private System.Windows.Media.Brush GetElementBackground(ControlElement element)
        {
            switch (element.Type)
            {
                case ControlType.TextBox:
                    return System.Windows.Media.Brushes.White;
                case ControlType.Label:
                    return System.Windows.Media.Brushes.LightGray;
                case ControlType.CheckBox:
                    return System.Windows.Media.Brushes.LightBlue;
                case ControlType.ComboBox:
                    return System.Windows.Media.Brushes.LightGreen;
                case ControlType.Table:
                    return System.Windows.Media.Brushes.LightYellow;
                default:
                    return System.Windows.Media.Brushes.LightGray;
            }
        }

        private UIElement CreateElementContent(ControlElement element)
        {
            StackPanel panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };

            // 添加显示名称
            TextBlock nameText = new TextBlock
            {
                Text = element.DisplayName,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            panel.Children.Add(nameText);

            // 根据元素类型添加不同的内容
            switch (element.Type)
            {
                case ControlType.TextBox:
                    TextBox textBox = new TextBox
                    {
                        Text = "示例文本",
                        FontSize = 10,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    panel.Children.Add(textBox);
                    break;
                case ControlType.CheckBox:
                    CheckBox checkBox = new CheckBox
                    {
                        Content = "选项",
                        FontSize = 10,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    panel.Children.Add(checkBox);
                    break;
                case ControlType.ComboBox:
                    ComboBox comboBox = new ComboBox
                    {
                        FontSize = 10,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    comboBox.Items.Add("选项1");
                    comboBox.Items.Add("选项2");
                    comboBox.SelectedIndex = 0;
                    panel.Children.Add(comboBox);
                    break;
                case ControlType.Table:
                    TextBlock tableText = new TextBlock
                    {
                        Text = "表格元素",
                        FontSize = 8,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    panel.Children.Add(tableText);
                    break;
            }

            return panel;
        }

        public void SetReportInstance(ReportInstance reportInstance, TemplateData template)
        {
            if (reportInstance == null || template?.Layout == null)
                return;

            PreviewCanvas.Width = template.Layout.PaperWidth;
            PreviewCanvas.Height = template.Layout.PaperHeight;
            PreviewCanvas.Children.Clear();

            // 渲染固定元素（原样展示，不填充 ReportData）
            foreach (var element in template.Layout.FixedElements)
                AddElementToCanvas(element);

            // 渲染可编辑元素（按 BindingPath 填充录入值）
            foreach (var element in template.Layout.EditableElements)
            {
                var key = !string.IsNullOrEmpty(element.BindingPath) ? element.BindingPath : element.ElementId;

                if (element is TableElement tableElement)
                {
                    reportInstance.Data.Tables.TryGetValue(key, out var rows);
                    AddTableToCanvas(tableElement, rows);
                }
                else
                {
                    reportInstance.Data.Fields.TryGetValue(key, out var value);
                    AddFieldToCanvas(element, value ?? string.Empty);
                }
            }
        }

        // 保留旧签名以兼容现有调用
        public void SetReportInstance(ReportInstance reportInstance)
        {
            if (reportInstance != null)
            {
                PreviewCanvas.Width = 210;
                PreviewCanvas.Height = 297;
                PreviewCanvas.Children.Clear();

                var reportIdText = new TextBlock
                {
                    Text = $"报告ID: {reportInstance.ReportId}",
                    FontSize = 12,
                    Margin = new Thickness(10, 10, 0, 0)
                };
                PreviewCanvas.Children.Add(reportIdText);

                var dataText = new TextBlock
                {
                    Text = "报告数据已生成（请使用 SetReportInstance(instance, template) 以查看完整预览）",
                    FontSize = 10,
                    Margin = new Thickness(10, 35, 0, 0),
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Width = 190
                };
                PreviewCanvas.Children.Add(dataText);
            }
        }

        /// <summary>
        /// 将普通字段控件渲染到 Canvas，显示录入值。
        /// </summary>
        private void AddFieldToCanvas(ControlElement element, string value)
        {
            var border = new Border
            {
                Width = element.Width,
                Height = element.Height,
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            Canvas.SetLeft(border, element.X);
            Canvas.SetTop(border, element.Y);

            var textBlock = new TextBlock
            {
                Text = value,
                FontFamily = new System.Windows.Media.FontFamily(element.FontFamily),
                FontSize = element.FontSize,
                FontWeight = element.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = element.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = textBlock;
            PreviewCanvas.Children.Add(border);
        }

        /// <summary>
        /// 将 TableElement 渲染到 Canvas，显示行数据。
        /// </summary>
        private void AddTableToCanvas(TableElement tableElement,
            System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>? rows)
        {
            var border = new Border
            {
                Width = tableElement.Width,
                Height = tableElement.Height,
                BorderBrush = System.Windows.Media.Brushes.Black,
                BorderThickness = new Thickness(1)
            };
            Canvas.SetLeft(border, tableElement.X);
            Canvas.SetTop(border, tableElement.Y);

            var grid = new Grid();
            // 列定义
            foreach (var col in tableElement.Columns)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(col.Width > 0 ? col.Width : 60) });

            // 表头行
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < tableElement.Columns.Count; c++)
            {
                var headerCell = new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Background = System.Windows.Media.Brushes.LightGray
                };
                headerCell.Child = new TextBlock
                {
                    Text = tableElement.Columns[c].Name,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(2)
                };
                Grid.SetRow(headerCell, 0);
                Grid.SetColumn(headerCell, c);
                grid.Children.Add(headerCell);
            }

            // 数据行
            if (rows != null)
            {
                for (int r = 0; r < rows.Count; r++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    for (int c = 0; c < tableElement.Columns.Count; c++)
                    {
                        var col = tableElement.Columns[c];
                        var colKey = !string.IsNullOrEmpty(col.BindingPath) ? col.BindingPath : col.Name;
                        rows[r].TryGetValue(colKey, out var cellValue);

                        var cell = new Border
                        {
                            BorderBrush = System.Windows.Media.Brushes.Black,
                            BorderThickness = new Thickness(0, 0, 1, 1)
                        };
                        cell.Child = new TextBlock
                        {
                            Text = cellValue ?? string.Empty,
                            FontSize = 9,
                            Margin = new Thickness(2),
                            TextWrapping = System.Windows.TextWrapping.Wrap
                        };
                        Grid.SetRow(cell, r + 1);
                        Grid.SetColumn(cell, c);
                        grid.Children.Add(cell);
                    }
                }
            }

            border.Child = grid;
            PreviewCanvas.Children.Add(border);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // UserControl没有Close方法，由父容器负责处理关闭逻辑
        }
    }
}
