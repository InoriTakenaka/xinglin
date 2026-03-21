# 实现计划：PDF 打印引擎（pdf-print-engine）

## 概述

按 P0 → P1 → P2 优先级递进实现。P0 完成后即可生成并打印 PDF；P1 补全预览窗口与 ViewModel 集成；P2 补全测试覆盖与边界处理。

语言：C# / .NET 8 WPF，PDF 库：PdfSharp 6.x。

---

## P0：核心 PDF 生成能力

> 目标：能够将 ReportInstance + TemplateData 渲染为合法 PDF 文件。

- [x] 1. 添加 NuGet 依赖
  - 在 `xinglin/xinglin.csproj` 中添加：
    - `<PackageReference Include="PdfSharp" Version="6.*" />` — PDF 生成库
    - `<PackageReference Include="System.Drawing.Common" Version="8.*" />` — 提供 `PrinterSettings.InstalledPrinters`（.NET 8 不内置）
    - `<PackageReference Include="FsCheck" Version="2.*" />` 和 `<PackageReference Include="FsCheck.MsTest" Version="2.*" />` — 属性测试
  - _需求：1.1、1.3_

- [x] 2. 实现 PdfCoordinateConverter 工具类
  - 新建 `xinglin/Services/Pdf/PdfCoordinateConverter.cs`
  - 实现 `PixelsToPoints(double pixels)`：`pixels × 72.0 / 96.0`
  - 实现 `MmToPoints(double mm)`：`mm × 72.0 / 25.4`
  - 实现 `GetPageSize(LayoutMetadata layout)`：根据 `IsLandscape` 交换宽高后返回 `(width, height)` 点值
  - 实现 `GetContentOrigin(LayoutMetadata layout)`：返回 `(MmToPoints(MarginLeft), MmToPoints(MarginTop))` 作为内容区起始点
  - _需求：2.1、2.2、2.3、2.4_

  - [ ]* 2.1 属性测试：像素转点（属性 1）
    - **属性 1：PixelsToPoints 对任意非负输入满足 result == pixels x 72.0 / 96.0，误差不超过 1e-9**
    - **验证：需求 2.1**
    - 注释：`// Feature: pdf-print-engine, Property 1: pixels-to-points conversion`

  - [ ]* 2.2 属性测试：毫米转点（属性 2）
    - **属性 2：MmToPoints 对任意非负输入满足 result == mm x 72.0 / 25.4，误差不超过 1e-9**
    - **验证：需求 2.2、2.3**
    - 注释：`// Feature: pdf-print-engine, Property 2: mm-to-points conversion`

  - [ ]* 2.3 属性测试：横向纸张宽高互换（属性 3）
    - **属性 3：GetPageSize 对任意 LayoutMetadata，横向时宽=MmToPoints(h)、高=MmToPoints(w)；竖向时宽=MmToPoints(w)、高=MmToPoints(h)**
    - **验证：需求 2.4、2.5**
    - 注释：`// Feature: pdf-print-engine, Property 3: landscape page size swap`

  - [ ]* 2.4 单元测试：具体纸张尺寸转换
    - A4 竖向（210mm x 297mm）转换结果正确
    - A5 横向（210mm x 148mm）转换结果正确

- [x] 3. 定义 IPdfPrintService 接口
  - 新建 `xinglin/Services/Pdf/IPdfPrintService.cs`
  - 声明 `Task<string> GeneratePdfAsync(ReportInstance report, TemplateData template)`
  - 声明 `Task PrintAsync(string pdfFilePath)`
  - _需求：7.1_

- [x] 4. 实现 PdfPrintService — PDF 生成核心
  - 新建 `xinglin/Services/Pdf/PdfPrintService.cs`，实现 `IPdfPrintService`
  - 构造函数注入 `ILoggerService logger`
  - `GeneratePdfAsync` 中：
    - 非空校验 `report` / `template`，为 null 时抛 `ArgumentNullException`（需求 7.4）
    - 调用 `PdfCoordinateConverter.GetPageSize` 创建 `PdfDocument` 和 `PdfPage`
    - 调用 `PdfCoordinateConverter.GetContentOrigin` 获取页边距偏移，传入渲染方法
    - 先渲染 `FixedElements`，再渲染 `EditableElements`（需求 3.7）
    - 文件名格式：`Report_{ReportId}_{yyyyMMddHHmmss}.pdf`，保存至 `Path.GetTempPath()`（需求 5.1）
    - 返回完整文件路径（需求 5.4）
  - 使用 `Task.Run` 在后台线程执行，不阻塞 UI（需求 7.3）
  - _需求：5.1、5.2、5.4、7.3、7.4_

- [x] 5. 实现各控件类型渲染方法
  - 在 `PdfPrintService` 中实现私有渲染分发方法 `RenderElement(XGraphics gfx, ControlElement el, ReportData data, double originX, double originY)`
  - 所有控件坐标 = `PixelsToPoints(el.X) + originX`，`PixelsToPoints(el.Y) + originY`（应用页边距偏移）
  - Label：`DrawString(element.Text, ...)`（需求 3.1）
  - TextBox：`DrawString(data.Fields[BindingPath] ?? "", ...)`（需求 3.2）
  - CheckBox：绘制 8pt x 8pt 矩形边框，值为 "true"（忽略大小写）时绘制 x 符号（需求 3.3）
  - Table（TableElement）：绘制表头行 + 数据行 + 单元格边框，超出高度截断（需求 3.4）
  - Line：`DrawLine` 水平或垂直（需求 3.5）
  - Rectangle：`DrawRectangle` 边框（需求 3.6）
  - 字体构建：`new XFont(element.FontFamily, element.FontSize, style)`；字体不存在时 catch 后回退 `Microsoft YaHei`（需求 3.8）
  - _需求：3.1、3.2、3.3、3.4、3.5、3.6、3.7、3.8_

  - [ ]* 5.1 属性测试：文本内容渲染到 PDF（属性 4）
    - **属性 4：含 Label/TextBox 的报告生成 PDF 后，PDF 内容流中可找到对应文本字符串**
    - **验证：需求 3.1、3.2**
    - 注释：`// Feature: pdf-print-engine, Property 4: text content rendered to PDF`

  - [ ]* 5.2 属性测试：表格行数据渲染到 PDF（属性 5）
    - **属性 5：含 Table 控件且有行数据的报告，PDF 中包含所有列名和单元格文本**
    - **验证：需求 3.4**
    - 注释：`// Feature: pdf-print-engine, Property 5: table rows rendered to PDF`

  - [ ]* 5.3 单元测试：CheckBox 渲染状态
    - "true" 时绘制选中符号；"false" 时绘制空矩形
    - 字体不存在时回退到 Microsoft YaHei 不抛异常

- [x] 6. P0 检查点
  - 确保所有测试通过，`PdfCoordinateConverter` 和 `PdfPrintService.GeneratePdfAsync` 可独立调用生成合法 PDF 文件。如有问题请告知。

  - [ ]* 6.1 属性测试：文件路径格式（属性 6）
    - **属性 6：GeneratePdfAsync 返回路径在 Path.GetTempPath() 下，文件名匹配正则 ^Report_[^_]+_\d{14}\.pdf$，且文件实际存在**
    - **验证：需求 5.1、5.4**
    - 注释：`// Feature: pdf-print-engine, Property 6: PDF file path format`

  - [ ]* 6.2 属性测试：PDF 页面尺寸与模板一致（属性 7）
    - **属性 7：生成 PDF 的页面尺寸（点）与 GetPageSize(layout) 返回值一致，误差不超过 0.5pt**
    - **验证：需求 5.2**
    - 注释：`// Feature: pdf-print-engine, Property 7: PDF page size matches template`

  - [ ]* 6.3 属性测试：null 参数抛出 ArgumentNullException（属性 8）
    - **属性 8：GeneratePdfAsync(null, template) 或 GeneratePdfAsync(report, null) 抛出 ArgumentNullException，且不生成文件**
    - **验证：需求 7.4**
    - 注释：`// Feature: pdf-print-engine, Property 8: null arguments throw ArgumentNullException`

---

## P1：打印预览窗口与 ViewModel 集成

> 目标：用户可在 DataEntryView 点击"打印"，弹出预览窗口后确认打印。

- [x] 7. 实现 PrintPreviewWindow
  - 新建 `xinglin/Views/PrintPreviewWindow.xaml`：
    - Grid 两行：上方 ScrollViewer > Viewbox > TemplatePreviewView，下方按钮栏（打印 + 取消）
    - 窗口尺寸 900x700，WindowStartupLocation="CenterOwner"
  - 新建 `xinglin/Views/PrintPreviewWindow.xaml.cs`：
    - 构造函数接收 `ReportInstance`、`TemplateData`、`IPdfPrintService`
    - 构造时调用 `PreviewView.SetReportInstance(report, template)`
    - `PrintButton_Click`：禁用按钮 → `GeneratePdfAsync` → `PrintAsync` → 关闭窗口；异常时 `MessageBox.Show`
    - `CancelButton_Click`：直接 `Close()`
  - _需求：4.1、4.2、4.3、4.4、4.5、5.3_

  - [ ]* 7.1 单元测试：预览窗口按钮行为
    - 取消按钮不触发打印
    - 打印失败时 PDF 文件不被删除

- [x] 8. 实现 PdfPrintService.PrintAsync — 物理打印
  - 在 `PdfPrintService` 中实现 `PrintAsync(string pdfFilePath)`
  - 检测系统打印机：`PrinterSettings.InstalledPrinters.Count > 0`
  - 有打印机：显示 `PrintDialog`，用户确认后通过 `DocumentPaginator` 发送打印任务（需求 6.1、6.2）
  - 无打印机：`Process.Start("explorer.exe", Path.GetDirectoryName(pdfFilePath))` 打开目录（需求 6.3）
  - 打印出错：`ILoggerService` 记录日志，抛出异常（需求 6.4）
  - _需求：6.1、6.2、6.3、6.4_

  - [ ]* 8.1 单元测试：无打印机时打开目录
    - Mock PrinterSettings，验证调用 explorer.exe 而非 PrintDialog

- [x] 9. 集成 DataEntryViewModel.PrintCommand
  - 在 `xinglin/ViewModels/DataEntryViewModel.cs` 中新增：
    - `[ObservableProperty] private bool _isPrintPreviewOpen`
    - `[RelayCommand(CanExecute = nameof(CanPrint))] public async Task PrintAsync()`
    - `private bool CanPrint() => !IsPrintPreviewOpen && GeneratedReport != null`
  - `PrintAsync` 实现：从 DI 获取 `IPdfPrintService`，构建 `PrintPreviewWindow`，`ShowDialog()`
  - 在 `xinglin/Views/DataEntryView.xaml` 中添加打印按钮：`<Button Content="打印" Command="{Binding PrintCommand}"/>`
  - _需求：8.1、8.2、8.3、8.4_

  - [ ]* 9.1 单元测试：PrintCommand 状态管理
    - PrintCommand 存在且类型为 IRelayCommand
    - 预览窗口打开时 CanExecute 返回 false
    - GeneratedReport 为 null 时 CanExecute 返回 false
    - TemplateData 加载失败时 ErrorMessage 被设置

- [x] 10. 注册依赖注入
  - 在 `xinglin/App.xaml.cs` 的 `ConfigureServices` 中添加：
    - `services.AddSingleton<IPdfPrintService, PdfPrintService>()`
  - _需求：7.2_

  - [ ]* 10.1 单元测试：DI 注册验证
    - IPdfPrintService 可从 IServiceProvider 解析，且实例类型为 PdfPrintService

- [x] 11. P1 检查点
  - 确保所有测试通过，完整流程可运行：DataEntryView 打印按钮 → 预览窗口 → 生成 PDF → 打印/打开目录。如有问题请告知。

---

## P2：边界处理与测试补全

> 目标：补全错误路径、边界条件处理，确保生产稳定性。

- [x] 12. 补全错误处理边界
  - `PdfPrintService.GeneratePdfAsync`：PDF 写入失败（磁盘满等）时抛出原始异常，不吞异常（需求 5.3）
  - `DataEntryViewModel.PrintAsync`：`CurrentTemplate == null || GeneratedReport == null` 时设置 `ErrorMessage` 并 return（需求 8.4）
  - 确认字体回退逻辑：catch Exception 后用 `new XFont("Microsoft YaHei", ...)` 重试（需求 3.8）
  - _需求：3.8、5.3、8.4_

  - [ ]* 12.1 单元测试：错误路径覆盖
    - PDF 写入失败时异常向上传播
    - 字体不存在时回退到 Microsoft YaHei 不抛异常
    - 打印失败时已生成 PDF 文件不被删除

- [x] 13. 最终检查点
  - 确保所有测试通过（单元测试 + 属性测试），代码无编译警告。如有问题请告知。

---

## 备注

- 标记 * 的子任务为可选测试任务，可跳过以加快 MVP 交付
- 每个属性测试须包含注释 // Feature: pdf-print-engine, Property {N}: {property_text}
- 属性测试使用 FsCheck + FsCheck.MsTest，每个属性最少运行 100 次迭代
- 所有任务均引用具体需求条款以保证可追溯性
