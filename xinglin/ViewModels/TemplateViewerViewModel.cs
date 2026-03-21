using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using xinglin.Models.CoreEntities;
using xinglin.Services.Data;

namespace xinglin.ViewModels
{
    public partial class TemplateViewerViewModel : ObservableObject
    {
        private readonly ITemplateService _templateService;

        [ObservableProperty]
        private TemplateData _currentTemplate;

        [ObservableProperty]
        private bool _isLoading;

        public TemplateViewerViewModel(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        [RelayCommand]
        public async Task LoadTemplateAsync(string templateId)
        {
            IsLoading = true;
            try
            {
                CurrentTemplate = await _templateService.GetTemplateByIdAsync(templateId);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task LoadTemplateFromFileAsync(string filePath)
        {
            IsLoading = true;
            try
            {
                CurrentTemplate = await _templateService.LoadTemplateFromFileAsync(filePath);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
