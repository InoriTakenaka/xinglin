using System;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using xinglin.Models.CoreEntities;
using xinglin.Services.Data;

namespace xinglin.Services.Pdf
{
    /// <summary>
    /// PDF 生成与打印服务实现，实现 IPdfPrintService 接口。
    /// </summary>
    public class PdfPrintService : IPdfPrintService
    {
        private readonly ILoggerService _logger;

        public PdfPrintService(ILoggerService logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task<string> GeneratePdfAsync(ReportInstance report, TemplateData template)
        {
            // 需求 7.4：非空校验
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            // 需求 7.3：在后台线程执行，不阻塞 UI
            return Task.Run(() =>
            {
                _logger.Information($"开始生成 PDF，ReportId={report.ReportId}");

                var layout = template.Layout;

                // 需求 2.x：获取页面尺寸（点）
                var (pageWidth, pageHeight) = PdfCoordinateConverter.GetPageSize(layout);

                // 需求 5.2：创建 PdfDocument 和 PdfPage，页面尺寸与模板一致
                var document = new PdfDocument();
                var page = document.AddPage();
                page.Width = XUnit.FromPoint(pageWidth);
                page.Height = XUnit.FromPoint(pageHeight);

                // 获取页边距偏移（点）
                var (originX, originY) = PdfCoordinateConverter.GetContentOrigin(layout);

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    // 需求 3.7：先渲染 FixedElements，再渲染 EditableElements
                    foreach (var el in layout.FixedElements)
                    {
                        RenderElement(gfx, el, report.Data, originX, originY);
                    }

                    foreach (var el in layout.EditableElements)
                    {
                        RenderElement(gfx, el, report.Data, originX, originY);
                    }
                }

                // 需求 5.1：文件名格式 Report_{ReportId}_{yyyyMMddHHmmss}.pdf，保存至临时目录
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var fileName = $"Report_{report.ReportId}_{timestamp}.pdf";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);

                document.Save(filePath);

                _logger.Information($"PDF 生成成功：{filePath}");

                // 需求 5.4：返回完整文件路径
                return filePath;
            });
        }

        /// <inheritdoc/>
        public Task PrintAsync(string pdfFilePath)
        {
            try
            {
                // 需求 6.3：检测系统打印机
                bool hasPrinters = PrinterSettings.InstalledPrinters.Count > 0;

                if (!hasPrinters)
                {
                    // 需求 6.3：无打印机时打开 PDF 所在目录
                    var folder = Path.GetDirectoryName(pdfFilePath) ?? Path.GetTempPath();
                    _logger.Information($"未检测到打印机，打开目录：{folder}");
                    Process.Start("explorer.exe", folder);
                    return Task.CompletedTask;
                }

                // 需求 6.1、6.2：有打印机时在 UI 线程显示 PrintDialog
                bool? dialogResult = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var printDialog = new PrintDialog();
                    dialogResult = printDialog.ShowDialog();
                });

                if (dialogResult == true)
                {
                    // 需求 6.2：用户确认后通过 Process.Start verb="print" 发送打印任务
                    _logger.Information($"发送打印任务：{pdfFilePath}");
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = pdfFilePath,
                        Verb = "print",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    _logger.Information("用户取消了打印操作。");
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // 需求 6.4：记录错误日志并抛出异常
                _logger.Error($"打印过程中发生错误：{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 渲染单个控件元素到 PDF 页面。
        /// </summary>
        private void RenderElement(XGraphics gfx, ControlElement el, ReportData data, double originX, double originY)
        {
            // 所有控件坐标 = PixelsToPoints(el.X) + originX，PixelsToPoints(el.Y) + originY
            double x = PdfCoordinateConverter.PixelsToPoints(el.X) + originX;
            double y = PdfCoordinateConverter.PixelsToPoints(el.Y) + originY;
            double w = PdfCoordinateConverter.MmToPoints(el.Width);
            double h = PdfCoordinateConverter.MmToPoints(el.Height);

            switch (el.Type)
            {
                case ControlType.Label:
                    // 需求 3.1：绘制 element.Text
                    RenderText(gfx, el, el.Text ?? "", x, y, w, h);
                    break;

                case ControlType.TextBox:
                    // 需求 3.2：绘制 data.Fields[BindingPath] ?? ""
                    {
                        string value = "";
                        if (!string.IsNullOrEmpty(el.BindingPath) && data.Fields.TryGetValue(el.BindingPath, out var fieldVal))
                            value = fieldVal ?? "";
                        RenderText(gfx, el, value, x, y, w, h);
                    }
                    break;

                case ControlType.CheckBox:
                    // 需求 3.3：绘制 8pt x 8pt 矩形边框，值为 "true" 时绘制 × 符号
                    {
                        const double boxSize = 8.0;
                        var pen = XPens.Black;
                        gfx.DrawRectangle(pen, x, y, boxSize, boxSize);

                        string checkVal = "";
                        if (!string.IsNullOrEmpty(el.BindingPath) && data.Fields.TryGetValue(el.BindingPath, out var cv))
                            checkVal = cv ?? "";

                        if (string.Equals(checkVal, "true", StringComparison.OrdinalIgnoreCase))
                        {
                            XFont checkFont = BuildFont(el, boxSize - 1);
                            var fmt = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
                            gfx.DrawString("×", checkFont, XBrushes.Black, new XRect(x, y, boxSize, boxSize), fmt);
                        }
                    }
                    break;

                case ControlType.Table:
                    // 需求 3.4：绘制表头行 + 数据行 + 单元格边框，超出高度截断
                    if (el is TableElement tableEl)
                        RenderTable(gfx, tableEl, data, x, y, w, h);
                    break;

                case ControlType.Line:
                    // 需求 3.5：水平或垂直线
                    {
                        double x2, y2;
                        if (el.Width >= el.Height)
                        {
                            // 水平线
                            x2 = x + w;
                            y2 = y;
                        }
                        else
                        {
                            // 垂直线
                            x2 = x;
                            y2 = y + h;
                        }
                        gfx.DrawLine(XPens.Black, x, y, x2, y2);
                    }
                    break;

                case ControlType.Rectangle:
                    // 需求 3.6：绘制矩形边框
                    gfx.DrawRectangle(XPens.Black, x, y, w, h);
                    break;

                default:
                    // 其他控件类型暂不渲染
                    break;
            }
        }

        /// <summary>
        /// 构建 XFont，字体不存在时回退到 Microsoft YaHei（需求 3.8）。
        /// </summary>
        private XFont BuildFont(ControlElement el, double? overrideSize = null)
        {
            double fontSize = overrideSize ?? el.FontSize;
            XFontStyleEx style = XFontStyleEx.Regular;
            if (el.IsBold) style |= XFontStyleEx.Bold;
            if (el.IsItalic) style |= XFontStyleEx.Italic;

            try
            {
                return new XFont(el.FontFamily, fontSize, style);
            }
            catch (Exception)
            {
                return new XFont("Microsoft YaHei", fontSize, style);
            }
        }

        /// <summary>
        /// 绘制文本字符串（Label / TextBox 共用）。
        /// </summary>
        private void RenderText(XGraphics gfx, ControlElement el, string text, double x, double y, double w, double h)
        {
            XFont font = BuildFont(el);
            var fmt = new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Near };
            gfx.DrawString(text, font, XBrushes.Black, new XRect(x, y, w > 0 ? w : 200, h > 0 ? h : 20), fmt);
        }

        /// <summary>
        /// 渲染表格控件（需求 3.4）。
        /// </summary>
        private void RenderTable(XGraphics gfx, TableElement table, ReportData data, double x, double y, double w, double h)
        {
            if (table.Columns == null || table.Columns.Count == 0)
                return;

            XFont headerFont = BuildFont(table);
            XFont cellFont = BuildFont(table);
            double rowHeight = PdfCoordinateConverter.MmToPoints(6); // 6mm 行高
            double currentY = y;
            double maxY = y + h;

            // 计算各列宽度（毫米转点）
            var colWidths = new double[table.Columns.Count];
            for (int i = 0; i < table.Columns.Count; i++)
                colWidths[i] = PdfCoordinateConverter.MmToPoints(table.Columns[i].Width > 0 ? table.Columns[i].Width : 20);

            var fmt = new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center };

            // 绘制表头行
            if (currentY + rowHeight > maxY)
                return;

            double colX = x;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var col = table.Columns[i];
                double cw = colWidths[i];
                gfx.DrawRectangle(XPens.Black, colX, currentY, cw, rowHeight);
                gfx.DrawString(col.Name ?? "", headerFont, XBrushes.Black,
                    new XRect(colX + 1, currentY, cw - 2, rowHeight), fmt);
                colX += cw;
            }
            currentY += rowHeight;

            // 获取行数据
            List<Dictionary<string, string>>? rows = null;
            if (!string.IsNullOrEmpty(table.BindingPath) && data.Tables.TryGetValue(table.BindingPath, out var tableRows))
                rows = tableRows;

            if (rows == null || rows.Count == 0)
                return;

            // 绘制数据行，超出高度截断
            foreach (var row in rows)
            {
                if (currentY + rowHeight > maxY)
                    break;

                colX = x;
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var col = table.Columns[i];
                    double cw = colWidths[i];
                    string cellText = "";
                    if (!string.IsNullOrEmpty(col.BindingPath) && row.TryGetValue(col.BindingPath, out var cv))
                        cellText = cv ?? "";

                    gfx.DrawRectangle(XPens.Black, colX, currentY, cw, rowHeight);
                    gfx.DrawString(cellText, cellFont, XBrushes.Black,
                        new XRect(colX + 1, currentY, cw - 2, rowHeight), fmt);
                    colX += cw;
                }
                currentY += rowHeight;
            }
        }
    }
}
