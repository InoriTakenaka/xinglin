using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using xinglin.Views;
using xinglin.ViewModels;
using xinglin.Models;
using xinglin.Services.Data;

namespace xinglin;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow:Window
{
    private TemplateEditorViewModel? _templateEditorViewModel;
    private DataEntryViewModel? _dataEntryViewModel;
    private ITemplateService? _templateService;

    public MainWindow()
    {
        InitializeComponent();
        // 确保依赖注入服务已初始化
        if(App.ServiceProvider == null)
        {
            MessageBox.Show("依赖注入服务未初始化","错误",MessageBoxButton.OK,MessageBoxImage.Error);
            return;
        }

        // 获取服务
        _templateService = App.ServiceProvider.GetService(typeof(ITemplateService)) as ITemplateService;

        // 通过依赖注入获取ViewModel并添加视图到选项卡
        InitializeTabs();

        // 初始化模板树
        InitializeTemplateTree();

        // 添加模板树点击事件
        TemplateTreeView.MouseDoubleClick += TemplateTreeView_MouseDoubleClick;
        // 添加模板树选择变化事件，支持单击加载模板
        TemplateTreeView.SelectedItemChanged += TemplateTreeView_SelectedItemChanged;
    }

    private void InitializeTabs()
    {
        try
        {
            // 初始化模板编辑器选项卡
            _templateEditorViewModel = App.ServiceProvider.GetService(typeof(TemplateEditorViewModel)) as TemplateEditorViewModel;
            if(_templateEditorViewModel != null)
            {
                var templateEditorView = new TemplateEditorView(_templateEditorViewModel);
                templateEditorView.Visibility = Visibility.Visible;
                TemplateEditorTab.Content = templateEditorView;
            }

            // 初始化数据录入选项卡
            _dataEntryViewModel = App.ServiceProvider.GetService(typeof(DataEntryViewModel)) as DataEntryViewModel;
            if(_dataEntryViewModel != null)
            {
                var dataEntryView = new DataEntryView(_dataEntryViewModel);
                dataEntryView.Visibility = Visibility.Visible;
                DataEntryTab.Content = dataEntryView;
            }
        }
        catch(Exception ex)
        {
            MessageBox.Show($"初始化选项卡时出错: {ex.Message}","错误",MessageBoxButton.OK,MessageBoxImage.Error);
        }
    }

    private void InitializeTemplateTree()
    {
        try
        {
            // 使用模板服务的GetTemplateDirectoryTree方法构建模板树
            try
            {
                if(_templateService != null)
                {
                    var templateTree = _templateService.GetTemplateDirectoryTree();
                    if(templateTree != null && templateTree.Count > 0)
                    {
                        // 设置模板树数据源
                        TemplateTreeView.ItemsSource = templateTree;
                    }
                    else
                    {
                        // 如果没有实际模板，添加示例模板数据
                        var root = new TemplateTreeItem { Name = "模板根目录",Id = "root" };
                        AddSampleTemplateData(root);
                        TemplateTreeView.ItemsSource = new[] { root };
                    }
                }
                else
                {
                    // 如果模板服务为空，添加示例模板数据
                    var root = new TemplateTreeItem { Name = "模板根目录",Id = "root" };
                    AddSampleTemplateData(root);
                    TemplateTreeView.ItemsSource = new[] { root };
                }
            }
            catch(Exception ex)
            {
                // 如果加载模板树失败，添加示例模板数据
                var root = new TemplateTreeItem { Name = "模板根目录",Id = "root" };
                AddSampleTemplateData(root);
                TemplateTreeView.ItemsSource = new[] { root };
                Console.WriteLine($"Error loading template directory tree: {ex.Message}");
            }
        }
        catch(Exception ex)
        {
            MessageBox.Show($"初始化模板树时出错: {ex.Message}","错误",MessageBoxButton.OK,MessageBoxImage.Error);
        }
    }

    private void AddSampleTemplateData(TemplateTreeItem root)
    {
        // 添加示例模板
        var template1 = new TemplateTreeItem { Name = "模板1",Id = "template1" };
        var template2 = new TemplateTreeItem { Name = "模板2",Id = "template2" };

        // 添加子项
        template1.Children.Add(new TemplateTreeItem { Name = "子模板1.1",Id = "template1.1" });
        template2.Children.Add(new TemplateTreeItem { Name = "子模板2.1",Id = "template2.1" });
        template2.Children.Add(new TemplateTreeItem { Name = "子模板2.2",Id = "template2.2" });

        // 添加到根目录
        root.Children.Add(template1);
        root.Children.Add(template2);
    }

    private void TemplateTreeView_MouseDoubleClick(object sender,MouseButtonEventArgs e)
    {
        var treeView = sender as TreeView;
        if(treeView != null)
        {
            var selectedItem = treeView.SelectedItem as TemplateTreeItem;

            if(selectedItem != null && selectedItem.Id != "root")
            {
                // 加载模板
                LoadTemplate(selectedItem.Id);
            }
        }
    }

    private void TemplateTreeView_SelectedItemChanged(object sender,RoutedPropertyChangedEventArgs<object> e)
    {
        var treeView = sender as TreeView;
        if(treeView != null)
        {
            var selectedItem = treeView.SelectedItem as TemplateTreeItem;

            if(selectedItem != null && selectedItem.Id != "root")
            {
                // 加载模板
                LoadTemplate(selectedItem.Id);
            }
        }
    }

    private async void LoadTemplate(string templateId)
    {
        try
        {
            // 从模板服务加载模板
            if(_templateService != null)
            {
                var template = await _templateService.GetTemplateByIdAsync(templateId);

                // 如果没有找到模板，创建一个新模板
                if(template == null)
                {
                    template = _templateService.CreateNewTemplate();
                    template.TemplateId = templateId;
                }

                // 更新模板编辑器
                if(_templateEditorViewModel != null)
                {
                    // 先清除之前的选中状态
                    _templateEditorViewModel.SelectControl(null);
                    // 设置新模板
                    _templateEditorViewModel.CurrentTemplate = template;
                }

                // 更新数据录入
                if(_dataEntryViewModel != null)
                {
                    _dataEntryViewModel.SetTemplate(template);
                }
            }
        }
        catch(Exception ex)
        {
            MessageBox.Show($"加载模板时出错: {ex.Message}","错误",MessageBoxButton.OK,MessageBoxImage.Error);
        }
    }

    private void NewTemplate_Click(object sender,RoutedEventArgs e)
    {
        // 切换到模板编辑器选项卡
        TabControl.SelectedIndex = 0;

        // 创建新模板
        if(_templateEditorViewModel != null)
        {
            _templateEditorViewModel.CreateNewTemplateCommand.Execute(null);
        }
    }

    private void OpenTemplate_Click(object sender,RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "模板文件 (*.json)|*.json|所有文件 (*.*)|*.*"
        };

        if(openFileDialog.ShowDialog() == true)
        {
            // 切换到模板编辑器选项卡
            TabControl.SelectedIndex = 0;
        }
    }

    private void Save_Click(object sender,RoutedEventArgs e)
    {
        // 保存当前模板
        if(_templateEditorViewModel != null)
        {
            _templateEditorViewModel.SaveTemplateCommand.Execute(null);
        }
    }

    private void Exit_Click(object sender,RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void TemplateEditor_Click(object sender,RoutedEventArgs e)
    {
        // 切换到模板编辑器选项卡
        TabControl.SelectedIndex = 0;
    }

    private void DataEntry_Click(object sender,RoutedEventArgs e)
    {
        // 切换到数据录入选项卡
        TabControl.SelectedIndex = 1;
    }

    private void TemplateViewer_Click(object sender,RoutedEventArgs e)
    {
        // 模板查看器已移除，切换到模板编辑器
        TabControl.SelectedIndex = 0;
    }

    private void ReportPreview_Click(object sender,RoutedEventArgs e)
    {
        var previewView = new Views.TemplatePreviewView();
        var previewWindow = new Window
        {
            Title = "报告预览",
            Width = 800,
            Height = 600,
            Content = previewView
        };
        previewWindow.ShowDialog();
    }

    private void About_Click(object sender,RoutedEventArgs e)
    {
        MessageBox.Show("模板编辑器和录入查看工具 v1.0\n基于WPF和.NET 8.0","关于");
    }
}
