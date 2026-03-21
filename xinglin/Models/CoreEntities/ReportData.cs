using System.Collections.Generic;

namespace xinglin.Models.CoreEntities
{
    /// <summary>
    /// 强类型报告数据，替代弱类型 object，按 BindingPath 组织录入值。
    /// </summary>
    public class ReportData
    {
        /// <summary>
        /// 普通字段数据：key = BindingPath（或 ElementId），value = 用户录入的字符串值。
        /// 例如：{ "PatientName": "张三", "PatientAge": "35", "ExamDate": "2026-01-15" }
        /// </summary>
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 表格数据：key = TableElement.BindingPath（或 ElementId），value = 行数据列表。
        /// 每行是一个以列 BindingPath 为 key 的字典。
        /// 例如：{ "ResultTable": [ { "ItemName": "血糖", "Result": "5.6", "Unit": "mmol/L" } ] }
        /// </summary>
        public Dictionary<string, List<Dictionary<string, string>>> Tables { get; set; }
            = new Dictionary<string, List<Dictionary<string, string>>>();
    }
}
