using System.Collections.Generic;

namespace xinglin.Models.CoreEntities
{
    /// <summary>
    /// 强类型模板配置，替代弱类型 object Config。
    /// </summary>
    public class TemplateConfig
    {
        /// <summary>
        /// ComboBox 选项配置：key = BindingPath，value = 选项列表。
        /// 例如：{ "PatientGender": ["男", "女", "未知"] }
        /// </summary>
        public Dictionary<string, List<string>> ComboBoxOptions { get; set; }
            = new Dictionary<string, List<string>>();

        /// <summary>
        /// 字段验证规则：key = BindingPath，value = 验证规则。
        /// </summary>
        public Dictionary<string, FieldValidationRule> ValidationRules { get; set; }
            = new Dictionary<string, FieldValidationRule>();
    }

    /// <summary>
    /// 字段级别的验证规则。
    /// </summary>
    public class FieldValidationRule
    {
        /// <summary>是否必填。</summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>最大长度限制（null 表示不限制）。</summary>
        public int? MaxLength { get; set; }

        /// <summary>正则表达式验证模式（null 表示不验证）。</summary>
        public string? RegexPattern { get; set; }

        /// <summary>验证失败时显示的错误消息。</summary>
        public string? ErrorMessage { get; set; }
    }
}
