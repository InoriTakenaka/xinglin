using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using xinglin.Models.CoreEntities;

namespace xinglin.Services.Data
{
    public class TemplateService : ITemplateService
    {
        private readonly string _templatesDirectory;
        private readonly string _reportsDirectory;
        private TemplateData? _currentTemplate;
        private readonly Dictionary<string, TemplateData> _templateCache;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILoggerService _loggerService;

        /// <summary>
        /// 统一序列化配置：枚举使用字符串名称，支持 TableElement 多态反序列化。
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter(),
                new ControlElementConverter()
            }
        };

        public TemplateService(IFileStorageService fileStorageService, ILoggerService loggerService)
        {
            // 使用AppDomain.CurrentDomain.BaseDirectory来构建目录路径
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // 构建模板目录和报告目录路径
            _templatesDirectory = Path.Combine(baseDirectory, "Templates");
            _reportsDirectory = Path.Combine(baseDirectory, "Reports");

            _fileStorageService = fileStorageService;
            _loggerService = loggerService;
            _templateCache = new Dictionary<string, TemplateData>();
            _currentTemplate = null;
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            _fileStorageService.EnsureDirectoryExists(_templatesDirectory);
            _fileStorageService.EnsureDirectoryExists(_reportsDirectory);
        }

        public async Task<List<TemplateData>> GetAllTemplatesAsync()
        {
            var templates = new List<TemplateData>();
            var templateFiles = _fileStorageService.GetFiles(_templatesDirectory, "*.json", true);

            foreach (var file in templateFiles)
            {
                try
                {
                    var template = await LoadTemplateFromFileAsync(file);
                    templates.Add(template);
                }
                catch (Exception ex)
                {
                    _loggerService.Error($"Error loading template {file}", ex);
                }
            }

            return templates;
        }

        public async Task<TemplateData> GetTemplateByIdAsync(string templateId)
        {
            if (_templateCache.TryGetValue(templateId, out var cachedTemplate))
            {
                return cachedTemplate;
            }

            // 首先在根目录查找
            var filePath = Path.Combine(_templatesDirectory, $"{templateId}.json");
            if (_fileStorageService.FileExists(filePath))
            {
                var template = await LoadTemplateFromFileAsync(filePath);
                if (template != null)
                {
                    _templateCache[templateId] = template;
                    return template;
                }
            }

            // 如果根目录找不到，在所有子目录中查找
            var allTemplateFiles = _fileStorageService.GetFiles(_templatesDirectory, "*.json", true);
            foreach (var file in allTemplateFiles)
            {
                try
                {
                    var template = await LoadTemplateFromFileAsync(file);
                    if (template != null && template.TemplateId == templateId)
                    {
                        _templateCache[templateId] = template;
                        return template;
                    }
                }
                catch (Exception ex)
                {
                    _loggerService.Error($"Error loading template {file}", ex);
                }
            }

            return CreateNewTemplate();
        }

        public async Task SaveTemplateAsync(TemplateData template)
        {
            template.ModifiedDate = DateTime.Now;
            var filePath = Path.Combine(_templatesDirectory, $"{template.TemplateId}.json");
            var json = JsonSerializer.Serialize(template, _jsonOptions);
            await _fileStorageService.WriteAllTextAsync(filePath, json);
            _templateCache[template.TemplateId] = template;
        }

        public async Task DeleteTemplateAsync(string templateId)
        {
            var filePath = Path.Combine(_templatesDirectory, $"{templateId}.json");
            if (_fileStorageService.FileExists(filePath))
            {
                _fileStorageService.DeleteFile(filePath);
                _templateCache.Remove(templateId);
            }
        }

        public async Task<TemplateData> LoadDefaultTemplateAsync()
        {
            var defaultTemplate = CreateNewTemplate();
            defaultTemplate.Name = "默认模板";
            defaultTemplate.Description = "默认的空白模板";
            return defaultTemplate;
        }

        public async Task ImportTemplateAsync(string sourceFilePath)
        {
            if (_fileStorageService.FileExists(sourceFilePath))
            {
                var templateJson = await _fileStorageService.ReadAllTextAsync(sourceFilePath);
                var template = JsonSerializer.Deserialize<TemplateData>(templateJson, _jsonOptions);
                if (template != null)
                {
                    await SaveTemplateAsync(template);
                }
            }
        }

        public async Task<TemplateData> LoadTemplateFromFileAsync(string filePath)
        {
            if (_fileStorageService.FileExists(filePath))
            {
                var json = await _fileStorageService.ReadAllTextAsync(filePath);
                var template = JsonSerializer.Deserialize<TemplateData>(json, _jsonOptions);
                return template ?? CreateNewTemplate();
            }
            return CreateNewTemplate();
        }

        public TemplateData LoadTemplateFromFileSync(string filePath)
        {
            if (_fileStorageService.FileExists(filePath))
            {
                var json = _fileStorageService.ReadAllText(filePath);
                var template = JsonSerializer.Deserialize<TemplateData>(json, _jsonOptions);
                return template ?? CreateNewTemplate();
            }
            return CreateNewTemplate();
        }

        public TemplateData CreateNewTemplate()
        {
            return new TemplateData();
        }

        public async Task SaveReportInstanceAsync(ReportInstance reportInstance)
        {
            reportInstance.ModifiedDate = DateTime.Now;
            var filePath = Path.Combine(_reportsDirectory, $"{reportInstance.ReportId}.json");
            var json = JsonSerializer.Serialize(reportInstance, _jsonOptions);
            await _fileStorageService.WriteAllTextAsync(filePath, json);
        }

        public async Task<ReportInstance> LoadReportInstanceAsync(string reportId)
        {
            var filePath = Path.Combine(_reportsDirectory, $"{reportId}.json");
            if (_fileStorageService.FileExists(filePath))
            {
                var json = await _fileStorageService.ReadAllTextAsync(filePath);
                var reportInstance = JsonSerializer.Deserialize<ReportInstance>(json, _jsonOptions);
                return reportInstance ?? new ReportInstance();
            }
            return new ReportInstance();
        }

        public async Task<List<ReportInstance>> GetReportInstancesAsync()
        {
            var reports = new List<ReportInstance>();
            var reportFiles = _fileStorageService.GetFiles(_reportsDirectory, "*.json");

            foreach (var file in reportFiles)
            {
                try
                {
                    var json = await _fileStorageService.ReadAllTextAsync(file);
                    var report = JsonSerializer.Deserialize<ReportInstance>(json, _jsonOptions);
                    if (report != null)
                    {
                        reports.Add(report);
                    }
                }
                catch (Exception ex)
                {
                    _loggerService.Error($"Error loading report {file}", ex);
                }
            }

            return reports;
        }

        public async Task DeleteReportInstanceAsync(string reportId)
        {
            var filePath = Path.Combine(_reportsDirectory, $"{reportId}.json");
            if (_fileStorageService.FileExists(filePath))
            {
                _fileStorageService.DeleteFile(filePath);
            }
        }

        public List<TemplateData> GetTemplates()
        {
            var templates = new List<TemplateData>();
            var templateFiles = _fileStorageService.GetFiles(_templatesDirectory, "*.json", true);

            foreach (var file in templateFiles)
            {
                try
                {
                    var template = LoadTemplateFromFileSync(file);
                    if (template != null)
                    {
                        templates.Add(template);
                    }
                }
                catch (Exception ex)
                {
                    _loggerService.Error($"Error loading template {file}", ex);
                }
            }

            return templates;
        }

        public async Task<List<TemplateData>> GetTemplatesTreeAsync()
        {
            return await GetAllTemplatesAsync();
        }

        public List<Models.TemplateTreeItem> GetTemplateDirectoryTree()
        {
            var rootItem = new Models.TemplateTreeItem { Name = "模板根目录", Id = "root" };

            // 递归构建目录树
            BuildTemplateDirectoryTree(_templatesDirectory, rootItem);

            return new List<Models.TemplateTreeItem> { rootItem };
        }

        private void BuildTemplateDirectoryTree(string directoryPath, Models.TemplateTreeItem parentItem)
        {
            try
            {
                // 获取目录下的所有子目录
                var subDirectories = _fileStorageService.GetDirectories(directoryPath);
                foreach (var subDir in subDirectories)
                {
                    var dirName = Path.GetFileName(subDir);
                    var dirItem = new Models.TemplateTreeItem
                    {
                        Name = dirName,
                        Id = subDir
                    };
                    parentItem.Children.Add(dirItem);

                    // 递归处理子目录
                    BuildTemplateDirectoryTree(subDir, dirItem);
                }

                // 获取目录下的所有模板文件
                var templateFiles = _fileStorageService.GetFiles(directoryPath, "*.json");
                foreach (var file in templateFiles)
                {
                    try
                    {
                        var template = LoadTemplateFromFileSync(file);
                        if (template != null)
                        {
                            var fileName = _fileStorageService.GetFileNameWithoutExtension(file);
                            var fileItem = new Models.TemplateTreeItem
                            {
                                Name = string.IsNullOrEmpty(template.Name) ? fileName : template.Name,
                                Id = template.TemplateId
                            };
                            parentItem.Children.Add(fileItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggerService.Error($"Error loading template file {file}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggerService.Error("Error building template directory tree", ex);
            }
        }

        public void OpenTemplate(string templatePath)
        {
            var template = LoadTemplateFromFileSync(templatePath);
            if (template != null)
            {
                _currentTemplate = template;
            }
        }

        public TemplateData GetCurrentTemplateData()
        {
            return _currentTemplate ?? CreateNewTemplate();
        }

        public async Task SaveTemplateDataAsync(TemplateData templateData)
        {
            _currentTemplate = templateData;
            await SaveTemplateAsync(templateData);
        }

        public void SaveTemplateData(TemplateData templateData)
        {
            _currentTemplate = templateData;
            // 使用同步方式保存模板
            templateData.ModifiedDate = DateTime.Now;
            var filePath = Path.Combine(_templatesDirectory, $"{templateData.TemplateId}.json");
            var json = JsonSerializer.Serialize(templateData, _jsonOptions);
            _fileStorageService.WriteAllText(filePath, json);
            _templateCache[templateData.TemplateId] = templateData;
        }
    }
}
