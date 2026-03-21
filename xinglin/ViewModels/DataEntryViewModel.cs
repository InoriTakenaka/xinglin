using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using xinglin.Models.CoreEntities;
using xinglin.Services.Data;
using xinglin.Services.Pdf;
using xinglin.Views;

namespace xinglin.ViewModels
{
    public partial class DataEntryViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private readonly ITemplateService _templateService;

        [ObservableProperty]
        private TemplateData _currentTemplate;

        [ObservableProperty]
        private ReportData _entryData;

        [ObservableProperty]
        private ReportInstance _generatedReport;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isValid;

        [ObservableProperty]
        private List<string> _validationErrors;

        [ObservableProperty]
        private Dictionary<string, List<string>> _propertyErrors;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private string _successMessage;

        [ObservableProperty]
        private bool _isPrintPreviewOpen;

        public DataEntryViewModel(IDataService dataService, ITemplateService templateService)
        {
            _dataService = dataService;
            _templateService = templateService;
            EntryData = new ReportData();
            ValidationErrors = new List<string>();
            PropertyErrors = new Dictionary<string, List<string>>();
        }

        [RelayCommand]
        public void SetTemplate(TemplateData template)
        {
            CurrentTemplate = template;
        }

        [RelayCommand]
        public void UpdateData(object data)
        {
            if (data is ReportData reportData)
                EntryData = reportData;
            ValidateData();
        }

        [RelayCommand]
        public void ValidateData()
        {
            ValidationResult result;
            if (CurrentTemplate != null)
            {
                result = _dataService.ValidateDataWithTemplate(EntryData ?? new ReportData(), CurrentTemplate);
            }
            else
            {
                result = _dataService.ValidateData(EntryData);
            }
            IsValid = result.IsValid;
            ValidationErrors = result.Errors;
            PropertyErrors = result.PropertyErrors;
            ErrorMessage = result.Errors.Count > 0 ? string.Join("\n", result.Errors) : string.Empty;
            SuccessMessage = string.Empty;
        }

        [RelayCommand]
        public async Task GenerateReportAsync()
        {
            if (CurrentTemplate == null)
            {
                ErrorMessage = "请先选择模板！";
                return;
            }

            // 收集录入数据
            var reportData = CollectReportData();
            EntryData = reportData;
            ValidateData();

            if (CurrentTemplate != null && IsValid)
            {
                IsLoading = true;
                try
                {
                    GeneratedReport = _dataService.GenerateReport(CurrentTemplate, reportData);
                    await _templateService.SaveReportInstanceAsync(GeneratedReport);
                    // 显示成功消息
                    SuccessMessage = "报告生成成功！";
                    ErrorMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    IsValid = false;
                    ErrorMessage = $"生成报告时出错: {ex.Message}";
                    SuccessMessage = string.Empty;
                }
                finally
                {
                    IsLoading = false;
                }
            }
            else
            {
                ErrorMessage = "数据验证失败，请检查输入！";
                SuccessMessage = string.Empty;
            }
        }

        [RelayCommand]
        public async Task SaveDataAsync(string key)
        {
            if (EntryData != null)
            {
                IsLoading = true;
                try
                {
                    await _dataService.SaveDataAsync(EntryData, key);
                    SuccessMessage = "数据保存成功！";
                    ErrorMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"保存数据时出错: {ex.Message}";
                    SuccessMessage = string.Empty;
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        [RelayCommand]
        public async Task LoadDataAsync(string key)
        {
            IsLoading = true;
            try
            {
                EntryData = await _dataService.LoadDataAsync(key) as ReportData ?? new ReportData();
                ValidateData();
                if (EntryData != null)
                {
                    SuccessMessage = "数据加载成功！";
                    ErrorMessage = string.Empty;
                }
                else
                {
                    ErrorMessage = "未找到指定数据！";
                    SuccessMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载数据时出错: {ex.Message}";
                SuccessMessage = string.Empty;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }

        [RelayCommand(CanExecute = nameof(CanPrint))]
        public async Task PrintAsync()
        {
            if (CurrentTemplate == null || GeneratedReport == null)
            {
                ErrorMessage = "请先生成报告后再打印！";
                return;
            }

            IsPrintPreviewOpen = true;
            PrintCommand.NotifyCanExecuteChanged();
            try
            {
                var printService = App.ServiceProvider.GetRequiredService<IPdfPrintService>();
                var window = new PrintPreviewWindow(GeneratedReport, CurrentTemplate, printService);
                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
            finally
            {
                IsPrintPreviewOpen = false;
                PrintCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanPrint() => !IsPrintPreviewOpen && GeneratedReport != null;

        /// <summary>
        /// 遍历当前模板的所有可编辑元素，按 BindingPath 收集录入值到 ReportData。
        /// 普通控件值存入 Fields，TableElement 行数据存入 Tables。
        /// </summary>
        public ReportData CollectReportData()
        {
            var reportData = new ReportData();
            if (CurrentTemplate?.Layout?.EditableElements == null)
                return reportData;

            foreach (var element in CurrentTemplate.Layout.EditableElements)
            {
                var key = !string.IsNullOrEmpty(element.BindingPath)
                    ? element.BindingPath
                    : element.ElementId;

                if (element is TableElement tableElement)
                {
                    // 收集表格行数据，以列 BindingPath 为 key（而非列名）
                    var rows = tableElement.Rows.Select(row =>
                    {
                        var rowDict = new Dictionary<string, string>();
                        foreach (var col in tableElement.Columns)
                        {
                            var colKey = !string.IsNullOrEmpty(col.BindingPath) ? col.BindingPath : col.Name;
                            row.CellValues.TryGetValue(col.Name, out var cellValue);
                            rowDict[colKey] = cellValue ?? string.Empty;
                        }
                        return rowDict;
                    }).ToList();
                    reportData.Tables[key] = rows;
                }
                else
                {
                    // 收集普通字段值
                    var value = element.Type switch
                    {
                        ControlType.CheckBox       => element.IsChecked.ToString(),
                        ControlType.ComboBox       => element.SelectedValue ?? string.Empty,
                        ControlType.DateTimePicker => element.Text ?? string.Empty,
                        _                          => element.Text ?? string.Empty
                    };
                    reportData.Fields[key] = value;
                }
            }

            return reportData;
        }
    }
}
