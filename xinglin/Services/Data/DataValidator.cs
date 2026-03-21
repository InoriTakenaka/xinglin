using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace xinglin.Services.Data
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, List<string>> PropertyErrors { get; set; } = new Dictionary<string, List<string>>();
    }

    public class ValidationRule
    {
        public string PropertyName { get; set; }
        public Func<object, bool> Validator { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsRequired { get; set; } = false;
    }

    public class DataValidator
    {
        private readonly List<ValidationRule> _rules = new List<ValidationRule>();

        public void AddRule(string propertyName, Func<object, bool> validator, string errorMessage, bool isRequired = false)
        {
            _rules.Add(new ValidationRule
            {
                PropertyName = propertyName,
                Validator = validator,
                ErrorMessage = errorMessage,
                IsRequired = isRequired
            });
        }

        public ValidationResult Validate(object data)
        {
            var result = new ValidationResult { IsValid = true };

            foreach (var rule in _rules)
            {
                try
                {
                    // 获取属性值
                    object? propertyValue = GetPropertyValue(data, rule.PropertyName);

                    // 验证
                    bool isValid = rule.Validator(propertyValue!);
                    if (!isValid)
                    {
                        result.IsValid = false;
                        result.Errors.Add(rule.ErrorMessage);

                        // 添加到属性级错误
                        if (!result.PropertyErrors.ContainsKey(rule.PropertyName))
                        {
                            result.PropertyErrors[rule.PropertyName] = new List<string>();
                        }
                        result.PropertyErrors[rule.PropertyName].Add(rule.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    string errorMessage = $"验证 {rule.PropertyName} 时出错: {ex.Message}";
                    result.Errors.Add(errorMessage);

                    // 添加到属性级错误
                    if (!result.PropertyErrors.ContainsKey(rule.PropertyName))
                    {
                        result.PropertyErrors[rule.PropertyName] = new List<string>();
                    }
                    result.PropertyErrors[rule.PropertyName].Add(errorMessage);
                }
            }

            return result;
        }

        private object? GetPropertyValue(object? data, string propertyName)
        {
            if (data == null)
                return null;

            // 支持嵌套属性，如 "User.Name"
            if (propertyName.Contains("."))
            {
                string[] parts = propertyName.Split('.');
                object? current = data;

                foreach (string part in parts)
                {
                    if (current == null)
                        return null;

                    PropertyInfo? prop = current.GetType().GetProperty(part);
                    if (prop == null)
                        return null;

                    current = prop.GetValue(current);
                }

                return current;
            }
            else
            {
                PropertyInfo? prop = data.GetType().GetProperty(propertyName);
                return prop?.GetValue(data);
            }
        }

        // 静态方法，用于常见验证
        public static bool IsRequired(object? value)
        {
            return value != null && !string.IsNullOrWhiteSpace(value.ToString());
        }

        public static bool IsNumber(object value)
        {
            return double.TryParse(value?.ToString(), out _);
        }

        public static bool IsEmail(object value)
        {
            var email = value?.ToString();
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsDate(object value)
        {
            return DateTime.TryParse(value?.ToString(), out _);
        }

        public static bool MinLength(object value, int minLength)
        {
            var str = value?.ToString();
            return !string.IsNullOrEmpty(str) && str.Length >= minLength;
        }

        public static bool MaxLength(object value, int maxLength)
        {
            var str = value?.ToString();
            return string.IsNullOrEmpty(str) || str.Length <= maxLength;
        }

        public static bool LengthBetween(object value, int minLength, int maxLength)
        {
            var str = value?.ToString();
            return !string.IsNullOrEmpty(str) && str.Length >= minLength && str.Length <= maxLength;
        }

        public static bool MinValue(object value, double minValue)
        {
            if (!double.TryParse(value?.ToString(), out double num))
                return false;
            return num >= minValue;
        }

        public static bool MaxValue(object value, double maxValue)
        {
            if (!double.TryParse(value?.ToString(), out double num))
                return false;
            return num <= maxValue;
        }

        public static bool Range(object value, double minValue, double maxValue)
        {
            if (!double.TryParse(value?.ToString(), out double num))
                return false;
            return num >= minValue && num <= maxValue;
        }

        public static bool MatchesPattern(object value, string pattern)
        {
            var str = value?.ToString();
            if (string.IsNullOrEmpty(str))
                return false;
            return System.Text.RegularExpressions.Regex.IsMatch(str, pattern);
        }

        public static bool IsPhone(object value)
        {
            var phone = value?.ToString();
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // 简单的电话号码验证
            string pattern = @"^\d{10,11}$";
            return System.Text.RegularExpressions.Regex.IsMatch(phone, pattern);
        }

        public static bool IsIdCard(object value)
        {
            var idCard = value?.ToString();
            if (string.IsNullOrWhiteSpace(idCard))
                return false;

            // 简单的身份证号验证
            string pattern = @"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$";
            return System.Text.RegularExpressions.Regex.IsMatch(idCard, pattern);
        }
    }
}
