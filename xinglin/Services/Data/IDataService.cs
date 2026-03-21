using System.Threading.Tasks;
using xinglin.Models.CoreEntities;

namespace xinglin.Services.Data
{
    public interface IDataService
    {
        Task SaveDataAsync(object data, string key);
        Task<object> LoadDataAsync(string key);
        ValidationResult ValidateData(object data);
        ValidationResult ValidateDataWithTemplate(ReportData data, TemplateData template);
        bool ValidateDataSimple(object data);
        ReportInstance GenerateReport(TemplateData template, ReportData reportData);
        void ImportData(string filePath);
        void ExportData(object data, string filePath);
    }
}
