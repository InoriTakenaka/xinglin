# 需求文档

## 简介

本功能为 WPF 病理检验报告系统（xinglin）实现 PDF 打印引擎。系统已具备报告数据录入（DataEntryView）和模板预览（TemplatePreviewView）能力，本功能在此基础上新增：将 `ReportInstance` 与 `TemplateData` 渲染为 PDF 文件，并支持通过物理打印机打印输出。

核心流程：打印预览 → 用户确认 → 生成 PDF → 调用打印机（或仅保存 PDF）。

---

## 词汇表

- **PdfPrintEngine**：本功能新增的 PDF 生成与打印服务，位于 `xinglin/Services/Pdf/`
- **PrintPreviewWindow**：打印预览窗口，展示待打印报告的像素级预览
- **ReportInstance**：报告实例，包含 `ReportData`（字段值 + 表格数据）及模板引用
- **TemplateData**：模板数据，包含 `LayoutMetadata`（纸张规格、边距、控件列表）
- **LayoutMetadata**：布局元数据，定义纸张类型（A4/A5）、方向（横/竖）、边距及控件集合
- **ControlElement**：控件元素基类，坐标 X/Y 为像素（96 dpi），Width/Height 为毫米
- **TableElement**：继承自 ControlElement 的表格控件，含列定义和行数据
- **BindingPath**：控件与 ReportData 字段的绑定键名
- **PdfDocument**：由 PdfPrintEngine 生成的 PDF 文件对象
- **PrintDialog**：WPF 内置打印对话框，用于选择打印机和打印参数

---

## 需求

### 需求 1：PDF 库选型

**用户故事：** 作为开发者，我希望选用合适的 PDF 生成库，以便在 .NET 8 WPF 环境下稳定生成符合医疗报告格式要求的 PDF 文件。

#### 验收标准

1. THE PdfPrintEngine SHALL 使用 **PdfSharp（6.x，MIT 许可）** 作为 PDF 生成库。
2. THE PdfPrintEngine SHALL 不依赖任何需要商业授权的第三方组件。
3. THE PdfPrintEngine SHALL 通过 NuGet 包引用 `PdfSharp` 并在 `.csproj` 中声明版本约束。

> **选型说明：** 选择 PdfSharp 而非 QuestPDF 的理由：
> - PdfSharp 提供底层绘图 API（XGraphics），与现有 Canvas 绝对坐标布局模型天然匹配，无需引入流式布局抽象层。
> - 现有 `TemplatePreviewView` 已使用像素坐标直接定位控件，PdfSharp 的 `XGraphics.DrawString` / `DrawRectangle` 可直接映射，迁移成本最低。
> - QuestPDF 的流式布局（Column/Row）与模板的绝对坐标体系存在阻抗失配，需要额外的坐标转换层。
> - PdfSharp 6.x 已支持 .NET 8，MIT 许可无商业风险。

---

### 需求 2：坐标与单位转换

**用户故事：** 作为开发者，我希望系统正确处理模板坐标单位，以便 PDF 中的控件位置与屏幕预览完全一致。

#### 验收标准

1. THE PdfPrintEngine SHALL 将 `ControlElement.X` 和 `ControlElement.Y`（像素，96 dpi）转换为 PDF 点坐标，转换公式为：`points = pixels × 72 / 96`。
2. THE PdfPrintEngine SHALL 将 `LayoutMetadata.PaperWidth` 和 `PaperHeight`（毫米）转换为 PDF 点坐标，转换公式为：`points = mm × 72 / 25.4`。
3. THE PdfPrintEngine SHALL 将 `ControlElement.Width` 和 `Height`（毫米）转换为 PDF 点坐标，使用相同的毫米转点公式。
4. WHEN `LayoutMetadata.IsLandscape` 为 `true`，THE PdfPrintEngine SHALL 交换纸张宽高以生成横向页面。
5. THE PdfPrintEngine SHALL 支持 A4（210mm × 297mm）和 A5（148mm × 210mm）两种纸张规格。

---

### 需求 3：报告内容渲染

**用户故事：** 作为检验员，我希望生成的 PDF 与屏幕预览视觉一致，以便打印出的报告准确反映录入数据。

#### 验收标准

1. WHEN 渲染 `ControlType.Label` 元素，THE PdfPrintEngine SHALL 在 PDF 对应坐标处绘制 `ControlElement.Text` 文本，字体、字号、粗体、斜体属性与 `ControlElement` 定义一致。
2. WHEN 渲染 `ControlType.TextBox` 元素，THE PdfPrintEngine SHALL 在 PDF 对应坐标处绘制 `ReportData.Fields[BindingPath]` 的值；若该键不存在，THE PdfPrintEngine SHALL 绘制空字符串。
3. WHEN 渲染 `ControlType.CheckBox` 元素，THE PdfPrintEngine SHALL 绘制复选框符号，并根据 `ReportData.Fields[BindingPath]` 的值（"true"/"false"）决定是否填充选中状态。
4. WHEN 渲染 `ControlType.Table` 元素（`TableElement`），THE PdfPrintEngine SHALL 按列定义绘制表头行，并按 `ReportData.Tables[BindingPath]` 的行数据依次绘制数据行，单元格边框使用细实线。
5. WHEN 渲染 `ControlType.Line` 元素，THE PdfPrintEngine SHALL 在对应坐标绘制水平或垂直线段。
6. WHEN 渲染 `ControlType.Rectangle` 元素，THE PdfPrintEngine SHALL 在对应坐标绘制矩形边框。
7. THE PdfPrintEngine SHALL 先渲染 `LayoutMetadata.FixedElements`，再渲染 `LayoutMetadata.EditableElements`，以保证固定元素在底层。
8. THE PdfPrintEngine SHALL 使用系统已安装字体，不嵌入特定字体文件；WHEN 指定字体不存在，THE PdfPrintEngine SHALL 回退到 `Microsoft YaHei`。

---

### 需求 4：打印预览

**用户故事：** 作为检验员，我希望在打印前看到报告预览，以便确认内容无误后再提交打印。

#### 验收标准

1. WHEN 用户在 `DataEntryViewModel` 触发打印命令，THE PrintPreviewWindow SHALL 打开并展示当前 `ReportInstance` 与 `TemplateData` 的渲染预览。
2. THE PrintPreviewWindow SHALL 复用 `TemplatePreviewView.SetReportInstance(reportInstance, template)` 方法渲染预览内容，保证预览与 PDF 输出视觉一致。
3. THE PrintPreviewWindow SHALL 提供"打印"按钮和"取消"按钮。
4. WHEN 用户点击"取消"，THE PrintPreviewWindow SHALL 关闭窗口且不执行任何打印或 PDF 生成操作。
5. THE PrintPreviewWindow SHALL 在预览区域按实际纸张比例显示报告，缩放比例自适应窗口大小。

---

### 需求 5：PDF 文件生成

**用户故事：** 作为检验员，我希望系统将报告保存为 PDF 文件，以便归档和后续查阅。

#### 验收标准

1. WHEN 用户在 PrintPreviewWindow 点击"打印"，THE PdfPrintEngine SHALL 在系统临时目录（`Path.GetTempPath()`）生成 PDF 文件，文件名格式为 `Report_{ReportId}_{yyyyMMddHHmmss}.pdf`。
2. THE PdfPrintEngine SHALL 生成单页 PDF，页面尺寸与 `LayoutMetadata` 定义的纸张规格一致。
3. IF PDF 文件生成失败，THEN THE PdfPrintEngine SHALL 向调用方抛出包含失败原因的异常，THE PrintPreviewWindow SHALL 显示错误提示对话框。
4. WHEN PDF 文件生成成功，THE PdfPrintEngine SHALL 返回生成文件的完整路径。

---

### 需求 6：物理打印机打印

**用户故事：** 作为检验员，我希望系统将生成的 PDF 发送到物理打印机，以便直接输出纸质报告。

#### 验收标准

1. WHEN PDF 文件生成成功且系统存在可用打印机，THE PdfPrintEngine SHALL 使用 WPF `PrintDialog` 让用户选择打印机并确认打印参数。
2. WHEN 用户在 `PrintDialog` 确认打印，THE PdfPrintEngine SHALL 将 PDF 内容通过 WPF `DocumentPaginator` 发送至所选打印机。
3. WHEN 系统不存在任何已安装打印机，THE PdfPrintEngine SHALL 跳过打印步骤，仅保留 PDF 文件，并打开 PDF 文件所在目录（`Process.Start("explorer.exe", folderPath)`）。
4. IF 打印过程中发生错误，THEN THE PdfPrintEngine SHALL 记录错误日志，THE PrintPreviewWindow SHALL 显示错误提示，且已生成的 PDF 文件不被删除。

---

### 需求 7：打印服务接口

**用户故事：** 作为开发者，我希望打印引擎以可测试的接口形式提供，以便在不依赖 UI 的情况下进行单元测试。

#### 验收标准

1. THE PdfPrintEngine SHALL 实现 `IPdfPrintService` 接口，该接口定义以下方法：
   - `Task<string> GeneratePdfAsync(ReportInstance report, TemplateData template)` — 生成 PDF 并返回文件路径
   - `Task PrintAsync(string pdfFilePath)` — 打印指定 PDF 文件
2. THE PdfPrintEngine SHALL 通过依赖注入（`IServiceProvider`）注册为 `IPdfPrintService` 的实现。
3. WHEN `GeneratePdfAsync` 被调用，THE PdfPrintEngine SHALL 在后台线程执行 PDF 生成，不阻塞 UI 线程。
4. THE PdfPrintEngine SHALL 对 `GeneratePdfAsync` 的输入参数进行非空校验；IF `report` 或 `template` 为 `null`，THEN THE PdfPrintEngine SHALL 抛出 `ArgumentNullException`。

---

### 需求 8：DataEntryViewModel 集成

**用户故事：** 作为检验员，我希望在数据录入界面直接触发打印，以便完成录入后无需切换界面。

#### 验收标准

1. THE DataEntryViewModel SHALL 提供 `PrintCommand`（`IRelayCommand`），绑定到数据录入界面的打印按钮。
2. WHEN `PrintCommand` 被执行，THE DataEntryViewModel SHALL 构建当前 `ReportInstance`，并打开 `PrintPreviewWindow`。
3. WHILE 打印预览窗口处于打开状态，THE DataEntryViewModel SHALL 禁用 `PrintCommand` 以防止重复触发。
4. IF 当前 `ReportInstance` 的 `TemplateId` 对应的 `TemplateData` 无法加载，THEN THE DataEntryViewModel SHALL 显示错误提示并取消打印流程。
