using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using xinglin.Models.CoreEntities;
using xinglin.ViewModels;

namespace xinglin.Views
{
    public partial class TemplateEditorView : UserControl
    {
        private bool _isDragging;
        private Point _dragStartPoint;
        private Point _dragOffset;
        private ControlElement _draggedControl;
        private System.Windows.Shapes.Rectangle _dragRectangle;

        public TemplateEditorView(TemplateEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            // 订阅SelectedControl属性变化事件
            if (viewModel != null)
            {
                viewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(viewModel.SelectedControl))
                    {
                        GenerateSpecificProperties();
                    }
                    else if (e.PropertyName == nameof(viewModel.CurrentTemplate))
                    {
                        InitializeFromTemplate();
                    }
                };

                // 订阅预览模板事件
                viewModel.PreviewTemplateRequested += OnPreviewTemplateRequested;
            }
        }

        private void InitializeFromTemplate()
        {
            var viewModel = DataContext as TemplateEditorViewModel;
            if (viewModel == null || viewModel.CurrentTemplate == null)
                return;

            System.Diagnostics.Debug.WriteLine($"开始初始化模板编辑器视图\n模板ID: {viewModel.CurrentTemplate.TemplateId}");

            // 清除之前的选中状态
            viewModel.SelectControl(null);

            // 确保布局存在
            if (viewModel.CurrentTemplate.Layout == null)
            {
                viewModel.CurrentTemplate.Layout = new xinglin.Models.CoreEntities.LayoutMetadata();
                System.Diagnostics.Debug.WriteLine("创建了新的布局元数据");
            }
            else
            {
                int editableCount = viewModel.CurrentTemplate.Layout.EditableElements?.Count ?? 0;
                int fixedCount = viewModel.CurrentTemplate.Layout.FixedElements?.Count ?? 0;
                System.Diagnostics.Debug.WriteLine($"布局信息\n可编辑元素: {editableCount} 个\n固定元素: {fixedCount} 个");
            }

            System.Diagnostics.Debug.WriteLine("模板编辑器视图初始化完成");
        }

        private void EditorCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ControlElement)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private System.Windows.Shapes.Rectangle _dragOverRectangle;

        private void EditorCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ControlElement)))
            {
                e.Effects = DragDropEffects.Copy;
                
                // 显示拖拽虚线框
                var control = e.Data.GetData(typeof(ControlElement)) as ControlElement;
                if (control != null)
                {
                    // 获取鼠标在Canvas中的位置，并考虑缩放
                    Point currentPoint = e.GetPosition(EditorCanvas);
                    double scale = GetZoomScale();
                    currentPoint.X /= scale;
                    currentPoint.Y /= scale;

                    // 转换控件尺寸为像素单位
                    double pixelWidth = control.Width * 96.0 / 25.4; // 毫米转像素
                    double pixelHeight = control.Height * 96.0 / 25.4;

                    // 创建或更新虚线框
                    if (_dragOverRectangle == null)
                    {
                        _dragOverRectangle = new System.Windows.Shapes.Rectangle
                        {
                            Width = pixelWidth,
                            Height = pixelHeight,
                            Stroke = System.Windows.Media.Brushes.DarkGray,
                            StrokeDashArray = new System.Windows.Media.DoubleCollection(new double[] { 5, 3 }),
                            StrokeThickness = 1,
                            Fill = System.Windows.Media.Brushes.Transparent
                        };
                        EditorCanvas.Children.Add(_dragOverRectangle);
                    }
                    else
                    {
                        // 更新虚线框尺寸
                        _dragOverRectangle.Width = pixelWidth;
                        _dragOverRectangle.Height = pixelHeight;
                    }
                    
                    // 计算虚线框位置（左上角定位）
                    double left = currentPoint.X;
                    double top = currentPoint.Y;
                    
                    // 限制在画布内
                    if (left < 0) left = 0;
                    if (top < 0) top = 0;
                    if (left + pixelWidth > EditorCanvas.Width)
                        left = EditorCanvas.Width - pixelWidth;
                    if (top + pixelHeight > EditorCanvas.Height)
                        top = EditorCanvas.Height - pixelHeight;
                    
                    System.Windows.Controls.Canvas.SetLeft(_dragOverRectangle, left);
                    System.Windows.Controls.Canvas.SetTop(_dragOverRectangle, top);
                }
                
                e.Handled = true;
            }
        }

        private double GetZoomScale()
        {
            var viewModel = DataContext as TemplateEditorViewModel;
            return viewModel != null ? viewModel.ZoomLevel / 100.0 : 1.0;
        }

        private double ConvertPixelsToMillimeters(double pixels)
        {
            // 标准转换比例：1英寸 = 25.4毫米 = 96 DIP
            return pixels * 25.4 / 96.0;
        }

        private void EditorCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ControlElement)))
            {
                var control = e.Data.GetData(typeof(ControlElement)) as ControlElement;
                if (control != null)
                {
                    // 获取鼠标在Canvas中的位置，并考虑缩放
                    Point dropPoint = e.GetPosition(EditorCanvas);
                    double scale = GetZoomScale();
                    dropPoint.X /= scale;
                    dropPoint.Y /= scale;

                    // 计算控件位置（左上角定位）
                    double left = dropPoint.X;
                    double top = dropPoint.Y;

                    // 限制在画布内（control.Width/Height 是毫米，需转换为像素后再比较）
                    const double MM_TO_PIXEL = 96.0 / 25.4;
                    double controlWidthPx = control.Width * MM_TO_PIXEL;
                    double controlHeightPx = control.Height * MM_TO_PIXEL;

                    if (left < 0) left = 0;
                    if (top < 0) top = 0;
                    if (left + controlWidthPx > EditorCanvas.Width)
                        left = EditorCanvas.Width - controlWidthPx;
                    if (top + controlHeightPx > EditorCanvas.Height)
                        top = EditorCanvas.Height - controlHeightPx;

                    // 调用ViewModel的方法添加控件
                    var viewModel = DataContext as TemplateEditorViewModel;
                    if (viewModel != null && viewModel.CurrentTemplate != null && viewModel.CurrentTemplate.Layout != null)
                    {
                        // 确保控件有唯一的ElementId
                        if (string.IsNullOrEmpty(control.ElementId))
                        {
                            control.ElementId = System.Guid.NewGuid().ToString();
                        }
                        // 添加控件到画布
                        viewModel.AddControlAtPosition(control, left, top);
                    }
                }
                
                // 移除拖拽虚线框
                if (_dragOverRectangle != null)
                {
                    EditorCanvas.Children.Remove(_dragOverRectangle);
                    _dragOverRectangle = null;
                }
                
                e.Handled = true;
            }
        }

        private void EditorCanvas_DragLeave(object sender, DragEventArgs e)
        {
            // 移除拖拽虚线框
            if (_dragOverRectangle != null)
            {
                EditorCanvas.Children.Remove(_dragOverRectangle);
                _dragOverRectangle = null;
            }
            e.Handled = true;
        }

        private void ControlElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null && border.Tag is ControlElement control)
            {
                var viewModel = DataContext as TemplateEditorViewModel;
                if (viewModel != null)
                {
                    viewModel.SelectControl(control);
                }

                // 开始拖拽
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _isDragging = true;
                    _draggedControl = control;
                    // 获取鼠标在Canvas中的位置（GetPosition已返回Canvas本地坐标，无需再除以scale）
                    _dragStartPoint = e.GetPosition(EditorCanvas);
                    
                    // 记录鼠标相对于控件左上角的偏移
                    _dragOffset = new Point(
                        _dragStartPoint.X - control.X,
                        _dragStartPoint.Y - control.Y
                    );
                    
                    // 创建拖拽虚线框（control.Width/Height为毫米，转换为像素）
                    const double MM_TO_PIXEL = 96.0 / 25.4;
                    _dragRectangle = new System.Windows.Shapes.Rectangle
                    {
                        Width = control.Width * MM_TO_PIXEL,
                        Height = control.Height * MM_TO_PIXEL,
                        Stroke = System.Windows.Media.Brushes.DarkGray,
                        StrokeDashArray = new System.Windows.Media.DoubleCollection(new double[] { 5, 3 }),
                        StrokeThickness = 1,
                        Fill = System.Windows.Media.Brushes.Transparent
                    };
                    System.Windows.Controls.Canvas.SetLeft(_dragRectangle, control.X);
                    System.Windows.Controls.Canvas.SetTop(_dragRectangle, control.Y);
                    EditorCanvas.Children.Add(_dragRectangle);
                    
                    border.CaptureMouse();
                }
                e.Handled = true;
            }
        }

        private void ControlElement_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedControl != null && _dragRectangle != null)
            {
                // 获取鼠标在Canvas中的位置（GetPosition已返回Canvas本地坐标，无需再除以scale）
                Point currentPoint = e.GetPosition(EditorCanvas);
                
                // 计算新位置（考虑鼠标偏移）
                double newX = currentPoint.X - _dragOffset.X;
                double newY = currentPoint.Y - _dragOffset.Y;

                // 限制在画布内
                if (newX < 0) newX = 0;
                if (newY < 0) newY = 0;
                if (newX + _dragRectangle.Width > EditorCanvas.Width)
                    newX = EditorCanvas.Width - _dragRectangle.Width;
                if (newY + _dragRectangle.Height > EditorCanvas.Height)
                    newY = EditorCanvas.Height - _dragRectangle.Height;

                // 更新虚线框位置
                System.Windows.Controls.Canvas.SetLeft(_dragRectangle, newX);
                System.Windows.Controls.Canvas.SetTop(_dragRectangle, newY);
            }
        }

        private void ControlElement_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                // 更新控件的实际位置
                if (_draggedControl != null && _dragRectangle != null)
                {
                    double newX = System.Windows.Controls.Canvas.GetLeft(_dragRectangle);
                    double newY = System.Windows.Controls.Canvas.GetTop(_dragRectangle);
                    _draggedControl.X = newX;
                    _draggedControl.Y = newY;
                }
                
                // 移除虚线框
                if (_dragRectangle != null)
                {
                    EditorCanvas.Children.Remove(_dragRectangle);
                    _dragRectangle = null;
                }
                
                _isDragging = false;
                _draggedControl = null;
                _dragOffset = new Point();
                var border = sender as Border;
                if (border != null)
                {
                    border.ReleaseMouseCapture();
                }
            }
        }

        private void EditorCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 点击画布空白区域，清除选中状态
            var viewModel = DataContext as TemplateEditorViewModel;
            if (viewModel != null)
            {
                viewModel.SelectControl(null);
            }
        }

        private void ConfigureTable_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (menuItem.DataContext is ControlElement control && control is TableElement tableElement)
                {
                    TableConfigWindow configWindow = new TableConfigWindow(tableElement);
                    Window ownerWindow = Window.GetWindow(this);
                    if (ownerWindow != null)
                    {
                        configWindow.Owner = ownerWindow;
                    }
                    configWindow.ShowDialog();
                }
            }
        }

        private void GenerateSpecificProperties()
        {
            // 清空特定属性面板
            SpecificPropertiesPanel.Children.Clear();

            var viewModel = DataContext as TemplateEditorViewModel;
            if (viewModel == null || viewModel.SelectedControl == null)
            {
                return;
            }

            var control = viewModel.SelectedControl;

            // 根据控件类型生成特定属性
            switch (control.Type)
            {
                case ControlType.Table:
                    GenerateTableProperties(control as TableElement);
                    break;
                case ControlType.TextBox:
                    GenerateTextBoxProperties();
                    break;
                case ControlType.CheckBox:
                    GenerateCheckBoxProperties();
                    break;
                case ControlType.ComboBox:
                    GenerateComboBoxProperties();
                    break;
                // 可以添加其他控件类型的特定属性
                default:
                    // 对于其他控件类型，显示默认消息
                    var defaultText = new TextBlock { Text = "无特定属性", Margin = new Thickness(10) };
                    SpecificPropertiesPanel.Children.Add(defaultText);
                    break;
            }
        }

        private void GenerateTableProperties(TableElement table)
        {
            if (table == null)
                return;

            // 添加行数属性
            var rowCountGrid = new Grid { Margin = new Thickness(5) };
            rowCountGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            rowCountGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var rowCountLabel = new TextBlock { Text = "行数:", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(rowCountLabel, 0);

            var rowCountTextBox = new TextBox { Text = table.RowCount.ToString() };
            rowCountTextBox.TextChanged += (sender, e) =>
            {
                if (int.TryParse(rowCountTextBox.Text, out int rowCount))
                {
                    table.RowCount = rowCount;
                }
            };
            Grid.SetColumn(rowCountTextBox, 1);

            rowCountGrid.Children.Add(rowCountLabel);
            rowCountGrid.Children.Add(rowCountTextBox);
            SpecificPropertiesPanel.Children.Add(rowCountGrid);

            // 添加允许添加行属性
            var allowAddRowsGrid = new Grid { Margin = new Thickness(5) };
            allowAddRowsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            allowAddRowsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var allowAddRowsLabel = new TextBlock { Text = "允许添加行:", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(allowAddRowsLabel, 0);

            var allowAddRowsCheckBox = new CheckBox { IsChecked = table.AllowAddRows };
            allowAddRowsCheckBox.Checked += (sender, e) => table.AllowAddRows = true;
            allowAddRowsCheckBox.Unchecked += (sender, e) => table.AllowAddRows = false;
            Grid.SetColumn(allowAddRowsCheckBox, 1);

            allowAddRowsGrid.Children.Add(allowAddRowsLabel);
            allowAddRowsGrid.Children.Add(allowAddRowsCheckBox);
            SpecificPropertiesPanel.Children.Add(allowAddRowsGrid);
        }

        private void GenerateTextBoxProperties()
        {
            // 添加文本框特定属性
            var textBlock = new TextBlock { Text = "文本框特定属性", Margin = new Thickness(10) };
            SpecificPropertiesPanel.Children.Add(textBlock);
        }

        private void GenerateCheckBoxProperties()
        {
            // 添加复选框特定属性
            var textBlock = new TextBlock { Text = "复选框特定属性", Margin = new Thickness(10) };
            SpecificPropertiesPanel.Children.Add(textBlock);
        }

        private void GenerateComboBoxProperties()
        {
            // 添加下拉框特定属性
            var textBlock = new TextBlock { Text = "下拉框特定属性", Margin = new Thickness(10) };
            SpecificPropertiesPanel.Children.Add(textBlock);
        }

        private void PaperSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox != null && comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                var paperSizeTag = selectedItem.Tag as string;
                var viewModel = DataContext as ViewModels.TemplateEditorViewModel;
                if (viewModel != null && !string.IsNullOrEmpty(paperSizeTag))
                {
                    viewModel.ChangePaperSizeCommand.Execute(paperSizeTag);
                }
            }
        }

        private void OnPreviewTemplateRequested(object sender, Models.CoreEntities.TemplateData template)
        {
            if (template != null)
            {
                var previewView = new TemplatePreviewView();
                previewView.SetTemplate(template);
                var previewWindow = new System.Windows.Window
                {
                    Title = "模板预览",
                    Width = 800,
                    Height = 600,
                    Content = previewView
                };
                previewWindow.ShowDialog();
            }
        }

        private void TogglePropertiesPanel_Click(object sender, RoutedEventArgs e)
        {
            if (PropertiesContentPanel.Visibility == Visibility.Visible)
            {
                PropertiesContentPanel.Visibility = Visibility.Collapsed;
                TogglePropertiesButton.Content = "▶";
            }
            else
            {
                PropertiesContentPanel.Visibility = Visibility.Visible;
                TogglePropertiesButton.Content = "▼";
            }
        }
    }
}
