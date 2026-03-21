using System;

namespace xinglin.Models.CoreEntities
{
    public class ReportInstance
    {
        public string ReportId { get; set; } = Guid.NewGuid().ToString();
        public string TemplateId { get; set; }
        public string TemplateVersion { get; set; }
        /// <summary>
        /// 强类型报告数据，按 BindingPath 存储录入值（Fields）和表格行数据（Tables）。
        /// </summary>
        public ReportData Data { get; set; } = new ReportData();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }
}
