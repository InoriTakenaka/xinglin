using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using xinglin.Models.CoreEntities;

namespace xinglin.Services.Data
{
    public class DataService : IDataService
    {
        private readonly string _dataDirectory;

        public DataService()
        {
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(_dataDirectory);
        }

        public async Task SaveDataAsync(object data, string key)
        {
            var filePath = Path.Combine(_dataDirectory, $"{key}.json");
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<object> LoadDataAsync(string key)
        {
            var filePath = Path.Combine(_dataDirectory, $"{key}.json");
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var data = JsonSerializer.Deserialize<object>(json);
                return data ?? new { };
            }
            return new { };
        }

        public ValidationResult ValidateData(object data)
        {
            var validator = new DataValidator();

            // 添加基本验证规则
            validator.AddRule("Data", DataValidator.IsRequired, "数据不能为空");

            // 可以根据具体数据类型添加更多验证规则

            return validator.Validate(data);
        }

        public ValidationResult ValidateDataWithTemplate(ReportData data, TemplateData template)
        {
            var result = new ValidationResult { IsValid = true };

            if (template?.Layout == null)
                return result;

            foreach (var element in template.Layout.EditableElements)
            {
                var key = !string.IsNullOrEmpty(element.BindingPath) ? element.BindingPath : element.ElementId;
                var rule = template.Config?.ValidationRules?.GetValueOrDefault(key);
                if (rule == null) continue;

                // 必填验证
                if (rule.IsRequired)
                {
                    bool hasValue;
                    if (element is TableElement)
                        hasValue = data.Tables.ContainsKey(key) && data.Tables[key].Count > 0;
                    else
                        hasValue = data.Fields.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v);

                    if (!hasValue)
                    {
                        var msg = rule.ErrorMessage ?? $"{element.DisplayName} 不能为空";
                        result.IsValid = false;
                        result.Errors.Add(msg);
                        if (!result.PropertyErrors.ContainsKey(key))
                            result.PropertyErrors[key] = new System.Collections.Generic.List<string>();
                        result.PropertyErrors[key].Add(msg);
                        continue;
                    }
                }

                // 仅对普通字段做格式验证
                if (element is not TableElement && data.Fields.TryGetValue(key, out var fieldValue))
                {
                    // 最大长度验证
                    if (rule.MaxLength.HasValue && fieldValue.Length > rule.MaxLength.Value)
                    {
                        var msg = rule.ErrorMessage ?? $"{element.DisplayName} 长度不能超过 {rule.MaxLength.Value} 个字符";
                        result.IsValid = false;
                        result.Errors.Add(msg);
                        if (!result.PropertyErrors.ContainsKey(key))
                            result.PropertyErrors[key] = new System.Collections.Generic.List<string>();
                        result.PropertyErrors[key].Add(msg);
                    }

                    // 正则验证
                    if (!string.IsNullOrEmpty(rule.RegexPattern)
                        && !string.IsNullOrWhiteSpace(fieldValue)
                        && !System.Text.RegularExpressions.Regex.IsMatch(fieldValue, rule.RegexPattern))
                    {
                        var msg = rule.ErrorMessage ?? $"{element.DisplayName} 格式不正确";
                        result.IsValid = false;
                        result.Errors.Add(msg);
                        if (!result.PropertyErrors.ContainsKey(key))
                            result.PropertyErrors[key] = new System.Collections.Generic.List<string>();
                        result.PropertyErrors[key].Add(msg);
                    }
                }
            }

            return result;
        }

        public bool ValidateDataSimple(object data)
        {
            // 简单的数据验证逻辑
            if (data == null)
            {
                return false;
            }
            return true;
        }

        public ReportInstance GenerateReport(TemplateData template, ReportData reportData)
        {
            var reportInstance = new ReportInstance
            {
                TemplateId = template.TemplateId,
                TemplateVersion = template.Version,
                Data = reportData
            };
            return reportInstance;
        }

        public void ImportData(string filePath)
        {
            // 简单的数据导入逻辑
            if (File.Exists(filePath))
            {
                // 实现数据导入
            }
        }

        public void ExportData(object data, string filePath)
        {
            // 简单的数据导出逻辑
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
