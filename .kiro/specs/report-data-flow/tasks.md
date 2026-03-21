# 实现计划：报告数据流转（report-data-flow）

## 概述

按照设计文档的 P0/P1/P2 优先级，逐步补齐 DataEntry 和 TemplateEditor 两个系统之间的数据流转代码，确保模板编辑 → 数据录入 → 报告生成 → 报告预览四个阶段的数据可以正常流转。

## 任务

### P0：阻塞数据流转（必须首先完成）

- [x] 1. 新增强类型报告数据模型 ReportData
  - 新建 `xinglin/Models/CoreEntities/ReportData.cs`
  - 包含 `Fields: Dictionary<string, string>` 和 `Tables: Dictionary<string, List<Dictionary<string, string>>>`
  - _需求：3.1、3.2_

- [x] 2. 修改 ReportInstance.Data 为强类型 ReportData
  - 修改 `xinglin/Models/CoreEntities/ReportInstance.cs`，将 `Data` 属性类型从 `object` 改为 `ReportData`
  - _需求：3.3_

- [x] 3. 实现 DataEntryViewModel.CollectReportData() 并接入报告生成流程
  - [x] 3.1 在 `xinglin/ViewModels/DataEntryViewModel.cs` 中新增 `CollectReportData()` 方法
    - 遍历 `CurrentTemplate.Layout.EditableElements`
    - 以 `element.BindingPath`（或 `element.ElementId`）为 key
    - TableElement 的行数据存入 `ReportData.Tables`，普通控件值存入 `ReportData.Fields`
    - _需求：6.1、6.2、6.3、6.4、6.5_
  - [ ]* 3.2 为 CollectReportData 编写属性测试：覆盖所有 EditableElements（属性 4）
    - **属性 4：CollectReportData 覆盖所有 EditableElements**
    - **验证：需求 6.1、6.2、6.3**
  - [ ]* 3.3 为 CollectReportData 编写属性测试：Table 行数据以列 BindingPath 为 key（属性 5）
    - **属性 5：TableElement 行数据以列 BindingPath 为 key 收集**
    - **验证：需求 6.4**
  - [x] 3.4 修改 `GenerateReportAsync`，调用 `CollectReportData()` 替代 `new {}`
    - _需求：6.6、7.1_

- [x] 4. P0 检查点 - 确保数据流转基础链路可运行
  - 确保所有测试通过，如有疑问请向用户确认。

---

### P1：保证数据正确性

- [x] 5. 实现 ControlElementConverter（TableElement 多态反序列化）
  - 新建 `xinglin/Services/Data/ControlElementConverter.cs`
  - 读取 Type 字段，若为 `"Table"` 或 `"6"` 则反序列化为 `TableElement`，否则为 `ControlElement`
  - 序列化时若为 `TableElement` 则保留 Columns 和 Rows 字段
  - _需求：2.1、2.2、2.3、2.4_

- [x] 6. 统一 JsonSerializer 序列化配置（JsonStringEnumConverter + ControlElementConverter）
  - [x] 6.1 修改 `xinglin/Services/Data/TemplateService.cs`
    - 新增静态 `_jsonOptions`，包含 `JsonStringEnumConverter` 和 `ControlElementConverter`
    - 所有 `JsonSerializer` 调用统一使用 `_jsonOptions`
    - _需求：1.1、2.5_
  - [ ]* 6.2 为 ControlType 枚举序列化编写属性测试（属性 1）
    - **属性 1：ControlType 枚举序列化 round-trip**
    - **验证：需求 1.2、1.3**
  - [ ]* 6.3 为 TableElement 多态反序列化编写属性测试（属性 2）
    - **属性 2：TableElement 多态反序列化正确性**
    - **验证：需求 2.1、2.2、2.4**
  - [ ]* 6.4 为 ReportData 序列化编写属性测试（属性 3）
    - **属性 3：ReportData 序列化 round-trip**
    - **验证：需求 3.4**

- [x] 7. 修复 baseTemplate JSON 文件中的 Type 枚举值
  - 将 `xinglin/Assets/baseTemplate/*.json` 中所有数字 Type 值改为字符串形式
  - 重点修复 `"Type": 16` → `"Type": "Line"`
  - _需求：1.5_

- [x] 8. P1 检查点 - 确保序列化/反序列化链路正确
  - 确保所有测试通过，如有疑问请向用户确认。

---

### P2：完善功能

- [x] 9. 新增强类型模板配置模型 TemplateConfig
  - 新建 `xinglin/Models/CoreEntities/TemplateConfig.cs`
  - 包含 `ComboBoxOptions: Dictionary<string, List<string>>` 和 `ValidationRules: Dictionary<string, FieldValidationRule>`
  - `FieldValidationRule` 包含 `IsRequired`、`MaxLength`、`RegexPattern`、`ErrorMessage`
  - _需求：4.1、4.2、4.3_

- [x] 10. 修改 TemplateData.Config 为强类型 TemplateConfig
  - 修改 `xinglin/Models/CoreEntities/TemplateData.cs`，将 `Config` 属性类型从 `object` 改为 `TemplateConfig`
  - _需求：4.4_

- [x] 11. 补齐 DataEntryView 控件初始化逻辑
  - [x] 11.1 在 `xinglin/Views/DataEntryView.xaml.cs` 的 `InitializeFromTemplate()` 中补齐 Table 类型的 DataGrid 创建逻辑
    - 按 `TableElement.Columns` 定义列，绑定 `CellValues[col.Name]`
    - 若 `TableElement.Rows` 为空，按 `RowCount` 预填空行（DefaultValue 为初始值）
    - _需求：5.6、5.7_
  - [ ]* 11.2 为 Table 空行预填编写属性测试（属性 6）
    - **属性 6：Table 控件空行预填数量等于 RowCount**
    - **验证：需求 5.7**
  - [x] 11.3 补齐 DateTimePicker 控件绑定逻辑
    - 新建 `xinglin/Views/StringToDateConverter.cs`（字符串↔日期转换器）
    - DatePicker 双向绑定到 `element.Text`（格式：yyyy-MM-dd）
    - _需求：5.5_
  - [x] 11.4 补齐 ComboBox 控件从 `Config.ComboBoxOptions[BindingPath]` 加载选项
    - _需求：5.4、4.5_

- [x] 12. 补齐 TemplatePreviewView 按 BindingPath 填充值
  - 修改 `xinglin/Views/TemplatePreviewView.xaml.cs`
  - `SetReportInstance(ReportInstance, TemplateData)` 方法：
    - FixedElements 按原始属性渲染，不填充 ReportData
    - EditableElements 普通字段从 `reportInstance.Data.Fields[key]` 读取值渲染为 TextBlock
    - EditableElements TableElement 从 `reportInstance.Data.Tables[key]` 读取行数据渲染为表格
    - key 不存在时渲染空字符串/空表格
  - _需求：8.1、8.2、8.3、8.4、8.5、8.6_

- [x] 13. 补齐数据验证逻辑使用强类型 ReportData
  - [x] 13.1 修改 `xinglin/Services/Data/IDataService.cs` 接口签名
    - `GenerateReport(TemplateData, ReportData)` 和 `ValidateDataWithTemplate(ReportData, TemplateData)`
    - _需求：7.2_
  - [x] 13.2 修改 `xinglin/Services/Data/DataService.cs`
    - `GenerateReport` 参数改为 `(TemplateData template, ReportData reportData)`，`Data` 字段设置为传入的 reportData
    - `ValidateDataWithTemplate` 参数改为 `(ReportData data, TemplateData template)`，实现必填、正则、长度验证
    - _需求：7.2、7.3、9.1、9.2、9.3、9.4、9.5、9.6_
  - [ ]* 13.3 为必填字段验证编写属性测试（属性 7）
    - **属性 7：必填字段为空时验证失败**
    - **验证：需求 9.2**
  - [ ]* 13.4 为必填表格验证编写属性测试（属性 8）
    - **属性 8：必填表格为空时验证失败**
    - **验证：需求 9.3**

- [x] 14. 最终检查点 - 确保所有测试通过
  - 确保所有测试通过，如有疑问请向用户确认。

## 备注

- 标有 `*` 的子任务为可选属性测试，可跳过以加快 MVP 进度
- P0 任务完成后即可验证基础数据流转链路（录入 → 生成报告）
- P1 任务完成后可验证序列化正确性（枚举字符串化、TableElement 多态）
- P2 任务完成后数据流转全链路（含预览、验证、ComboBox 配置）完整可用
- 属性测试建议使用 FsCheck（C# 属性测试库）
