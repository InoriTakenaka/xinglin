using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using xinglin.Models.CoreEntities;
using xinglin.Services.Data;

namespace xinglin.ViewModels
{
    public partial class TemplateEditorViewModel : ObservableObject
    {
        private readonly ITemplateService _templateService;

        [ObservableProperty]
        private TemplateData _currentTemplate;

        [ObservableProperty]
        private ControlElement _selectedControl;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _paperSize = "A4";

        [ObservableProperty]
        private double _zoomLevel = 100.0;

        // 预览模板事件
        public event EventHandler<TemplateData> PreviewTemplateRequested;

        public TemplateEditorViewModel(ITemplateService templateService)
        {
            _templateService = templateService;
            // 初始化时自动创建新模板
            CurrentTemplate = _templateService.CreateNewTemplate();
            // 设置默认纸张大小
            SetDefaultPaperSize();
        }

        [RelayCommand]
        public async Task CreateNewTemplateAsync()
        {
            CurrentTemplate = _templateService.CreateNewTemplate();
            // 设置默认纸张大小
            SetDefaultPaperSize();
        }

        [RelayCommand]
        public async Task LoadTemplateAsync(string templateId)
        {
            IsLoading = true;
            try
            {
                CurrentTemplate = await _templateService.GetTemplateByIdAsync(templateId);
                // 设置纸张大小
                SetDefaultPaperSize();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task SaveTemplateAsync()
        {
            if (CurrentTemplate != null)
            {
                IsLoading = true;
                try
                {
                    await _templateService.SaveTemplateAsync(CurrentTemplate);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public void AddControlAtPosition(ControlElement control, double x, double y)
        {
            if (CurrentTemplate != null && CurrentTemplate.Layout != null)
            {
                control.X = x;
                control.Y = y;
                CurrentTemplate.Layout.EditableElements.Add(control);
                // 自动选择新添加的控件
                SelectControl(control);
            }
        }

        public void SelectControl(ControlElement control)
        {
            // 取消之前选中的控件
            if (SelectedControl != null)
            {
                SelectedControl.IsSelected = false;
            }
            // 选中新控件
            SelectedControl = control;
            if (control != null)
            {
                control.IsSelected = true;
            }
        }

        [RelayCommand]
        public void RemoveControl(ControlElement control)
        {
            if (CurrentTemplate != null && CurrentTemplate.Layout != null)
            {
                CurrentTemplate.Layout.EditableElements.Remove(control);
                if (SelectedControl == control)
                {
                    SelectedControl = null;
                }
            }
        }

        [RelayCommand]
        public async Task LoadDefaultTemplateAsync()
        {
            IsLoading = true;
            try
            {
                CurrentTemplate = await _templateService.LoadDefaultTemplateAsync();
                // 设置纸张大小
                SetDefaultPaperSize();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void PreviewTemplate()
        {
            if (CurrentTemplate != null)
            {
                // 触发预览模板事件
                PreviewTemplateRequested?.Invoke(this, CurrentTemplate);
            }
        }

        [RelayCommand]
        public void ChangePaperSize(string paperSize)
        {
            if (CurrentTemplate != null && CurrentTemplate.Layout != null && !string.IsNullOrEmpty(paperSize))
            {
                switch (paperSize)
                {
                    case "A4":
                        CurrentTemplate.Layout.PaperWidth = 210;
                        CurrentTemplate.Layout.PaperHeight = 297;
                        break;
                    case "A5":
                        CurrentTemplate.Layout.PaperWidth = 148;
                        CurrentTemplate.Layout.PaperHeight = 210;
                        break;
                    case "A3":
                        CurrentTemplate.Layout.PaperWidth = 297;
                        CurrentTemplate.Layout.PaperHeight = 420;
                        break;
                }
                PaperSize = paperSize;
            }
        }

        private void SetDefaultPaperSize()
        {
            // 默认设置为A4纸张大小
            if (CurrentTemplate != null && CurrentTemplate.Layout != null)
            {
                CurrentTemplate.Layout.PaperWidth = 210;
                CurrentTemplate.Layout.PaperHeight = 297;
            }
        }

        [RelayCommand]
        public void ZoomIn()
        {
            ZoomLevel = Math.Min(ZoomLevel + 10, 200);
        }

        [RelayCommand]
        public void ZoomOut()
        {
            ZoomLevel = Math.Max(ZoomLevel - 10, 25);
        }

        [RelayCommand]
        public void SetZoomLevel(double level)
        {
            ZoomLevel = Math.Max(25, Math.Min(level, 200));
        }

        [RelayCommand]
        public void ResetZoom()
        {
            ZoomLevel = 100;
        }
    }
}
