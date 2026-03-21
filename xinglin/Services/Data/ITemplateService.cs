using System.Collections.Generic;
using System.Threading.Tasks;
using xinglin.Models;
using xinglin.Models.CoreEntities;

namespace xinglin.Services.Data
{
    public interface ITemplateService
    {
        Task<List<TemplateData>> GetAllTemplatesAsync();
        Task<TemplateData> GetTemplateByIdAsync(string templateId);
        Task SaveTemplateAsync(TemplateData template);
        Task DeleteTemplateAsync(string templateId);
        Task<TemplateData> LoadDefaultTemplateAsync();
        Task ImportTemplateAsync(string sourceFilePath);
        Task<TemplateData> LoadTemplateFromFileAsync(string filePath);
        TemplateData CreateNewTemplate();
        Task SaveReportInstanceAsync(ReportInstance reportInstance);
        Task<ReportInstance> LoadReportInstanceAsync(string reportId);
        Task<List<ReportInstance>> GetReportInstancesAsync();
        Task DeleteReportInstanceAsync(string reportId);
        List<TemplateData> GetTemplates();
        Task<List<TemplateData>> GetTemplatesTreeAsync();
        List<Models.TemplateTreeItem> GetTemplateDirectoryTree();
        void OpenTemplate(string templatePath);
        TemplateData GetCurrentTemplateData();
        void SaveTemplateData(TemplateData templateData);
    }
}
