using System.Threading.Tasks;
using xinglin.Models.CoreEntities;

namespace xinglin.Services.Pdf
{
    public interface IPdfPrintService
    {
        /// <summary>生成 PDF，返回文件完整路径</summary>
        Task<string> GeneratePdfAsync(ReportInstance report, TemplateData template);

        /// <summary>将指定 PDF 文件发送至打印机；无打印机时打开所在目录</summary>
        Task PrintAsync(string pdfFilePath);
    }
}
