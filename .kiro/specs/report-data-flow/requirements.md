# 需求文档：报告数据流转（report-data-flow）

## 简介

本文档基于设计文档，梳理报告录入（DataEntry）和模板编辑（TemplateEditor）两个系统之间的控件元素结构与报告 JSON 的完整流转需求。目标是确保模板编辑、数据录入、报告生成、报告预览四个阶段的数据可以正常流转，消除当前存在的类型不一致、弱类型数据结构和数据收集缺失等问题。

## 词汇表

- **ControlElement**：模板中的基础控件元素，包含 ElementId、Type、BindingPath 等属性
- **TableElement**：继承自 ControlElement 的表格控件，包含 Columns 和 Rows
- **TableColumn**：表格列定义，包含 Name、BindingPath、ControlType、IsEditable 等属性
- **TableRow**：表格行数据，包含 CellValues 字典（key 为列名）
- **ControlType**：控件类型枚举（Label=0, TextBox=1, CheckBox=2, RadioButton=3, ComboBox=4, DateTimePicker=5, Table=6, Image=7, Line=8, Rectangle=9）
- **BindingPath**：控件的数据绑定路径，用于在 ReportData 中唯一标识字段
- **TemplateData**：模板数据模型，包含 Layout（FixedElements + EditableElements）和 Config
- **TemplateConfig**：模板配置，包含 ComboBoxOptions 和 ValidationRules
- **ReportData**：强类型报告数据，包含 Fields（普通字段）和 Tables（表格数据）
- **ReportInstance**：报告实例，包含 ReportId、TemplateId、TemplateVersion、Data（ReportData）
- **TemplateService**：模板服务，负责模板和报告实例的序列化/反序列化
- **DataService**：数据服务，负责报告生成和数据验证
- **DataEntryViewModel**：数据录入视图模型，负责收集录入数据
- **DataEntryView**：数据录入视图，负责按控件类型创建输入控件
- **TemplatePreviewView**：报告预览视图，负责按 BindingPath 填充并渲染报告
- **ControlElementConverter**：支持 TableElement 多态反序列化的 JsonConverter
- **ValidationResult**：验证结果，包含字段级别的错误信息列表

---

## 需求

### 需求 1：统一控件类型枚举序列化

**用户故事：** 作为开发者，我希望 ControlType 枚举在 JSON 中以字符串名称表示，以便消除 JSON 文件与代码枚举值之间的不一致问题。

#### 验收标准

1. THE TemplateService SHALL 在所有 JsonSerializer 调用中使用包含 JsonStringEnumConverter 的统一序列化配置
2. WHEN ControlElement 被序列化时，THE TemplateService SHALL 将 Type 字段输出为字符串枚举名称（如 `"Type": "Label"`），而非数字
3. WHEN 包含字符串 Type 的 JSON 被反序列化时，THE TemplateService SHALL 正确还原对应的 ControlType 枚举值
4. THE TemplateService SHALL 同时支持反序列化历史数字格式的 Type 字段（向后兼容）
5. WHEN 现有 baseTemplate JSON 文件中存在 `"Type": 16` 时，THE TemplateService SHALL 将其识别为 ControlType.Line（值为 8）

---

### 需求 2：TableElement 多态反序列化

**用户故事：** 作为开发者，我希望从 JSON 反序列化 LayoutMetadata.EditableElements 时，Type 为 Table 的元素能被正确还原为 TableElement 实例，以便保留表格列定义和行数据。

#### 验收标准

1. THE ControlElementConverter SHALL 在反序列化 ControlElement 时，检查 Type 字段是否为 `"Table"` 或 `"6"`
2. WHEN Type 为 Table 时，THE ControlElementConverter SHALL 将 JSON 反序列化为 TableElement 实例（而非基类 ControlElement）
3. WHEN Type 不为 Table 时，THE ControlElementConverter SHALL 将 JSON 反序列化为 ControlElement 实例
4. WHEN TableElement 被序列化时，THE ControlElementConverter SHALL 保留 Columns 和 Rows 等 TableElement 特有字段
5. THE TemplateService SHALL 在序列化配置中注册 ControlElementConverter

---

### 需求 3：强类型报告数据模型

**用户故事：** 作为开发者，我希望 ReportInstance.Data 使用强类型 ReportData 替代 object，以便在录入、生成和预览阶段能够按 BindingPath 可靠地读写字段值。

#### 验收标准

1. THE ReportData SHALL 包含 `Fields` 属性（`Dictionary<string, string>`），用于存储普通字段的 BindingPath 到值的映射
2. THE ReportData SHALL 包含 `Tables` 属性（`Dictionary<string, List<Dictionary<string, string>>>`），用于存储表格字段的 BindingPath 到行数据列表的映射
3. THE ReportInstance SHALL 将 `Data` 属性类型从 `object` 改为 `ReportData`
4. WHEN ReportData 被序列化后再反序列化时，THE TemplateService SHALL 还原等价的 ReportData 对象（Fields 和 Tables 内容不变）
5. IF ReportData.Fields 中不存在某个 BindingPath 的键时，THEN THE TemplatePreviewView SHALL 以空字符串渲染对应控件

---

### 需求 4：强类型模板配置模型

**用户故事：** 作为开发者，我希望 TemplateData.Config 使用强类型 TemplateConfig 替代 object，以便 DataEntryView 能够直接读取 ComboBox 选项和验证规则，无需多层类型转换。

#### 验收标准

1. THE TemplateConfig SHALL 包含 `ComboBoxOptions` 属性（`Dictionary<string, List<string>>`），key 为 BindingPath，value 为选项列表
2. THE TemplateConfig SHALL 包含 `ValidationRules` 属性（`Dictionary<string, FieldValidationRule>`），key 为 BindingPath，value 为验证规则
3. THE FieldValidationRule SHALL 包含 `IsRequired`、`MaxLength`、`RegexPattern`、`ErrorMessage` 字段
4. THE TemplateData SHALL 将 `Config` 属性类型从 `object` 改为 `TemplateConfig`
5. WHEN DataEntryView 初始化 ComboBox 控件时，THE DataEntryView SHALL 从 `CurrentTemplate.Config.ComboBoxOptions[element.BindingPath]` 加载选项列表

---

### 需求 5：数据录入界面控件初始化

**用户故事：** 作为数据录入员，我希望录入界面能够根据模板中每个控件的类型自动创建对应的输入控件，以便我能够正确填写各类型的数据。

#### 验收标准

1. WHEN DataEntryView 加载模板时，THE DataEntryView SHALL 遍历 `Layout.EditableElements` 并按 `element.Type` 创建对应输入控件
2. WHEN element.Type 为 TextBox 时，THE DataEntryView SHALL 创建双向绑定到 `element.Text` 的 TextBox 控件
3. WHEN element.Type 为 CheckBox 时，THE DataEntryView SHALL 创建双向绑定到 `element.IsChecked` 的 CheckBox 控件
4. WHEN element.Type 为 ComboBox 时，THE DataEntryView SHALL 创建 ComboBox 控件并从 `Config.ComboBoxOptions[element.BindingPath]` 加载选项
5. WHEN element.Type 为 DateTimePicker 时，THE DataEntryView SHALL 创建 DatePicker 控件并双向绑定到 `element.Text`（格式：yyyy-MM-dd）
6. WHEN element.Type 为 Table 时，THE DataEntryView SHALL 创建 DataGrid 控件，列定义来自 `TableElement.Columns`，数据源绑定到 `TableElement.Rows`
7. WHEN TableElement.Rows 为空时，THE DataEntryView SHALL 按 `TableElement.RowCount` 预填对应数量的空行，每行的 CellValues 以列名为 key、DefaultValue 为初始值

---

### 需求 6：录入数据收集

**用户故事：** 作为系统，我希望在用户点击"生成报告"时能够完整收集所有录入控件的值，并按 BindingPath 组织到 ReportData 中，以便生成包含完整数据的报告实例。

#### 验收标准

1. THE DataEntryViewModel SHALL 提供 `CollectReportData()` 方法，遍历 `CurrentTemplate.Layout.EditableElements` 并返回 ReportData
2. WHEN element.BindingPath 不为空时，THE DataEntryViewModel SHALL 以 `element.BindingPath` 作为 ReportData 中的 key
3. WHEN element.BindingPath 为空时，THE DataEntryViewModel SHALL 以 `element.ElementId` 作为 ReportData 中的 key
4. WHEN element 为 TableElement 时，THE DataEntryViewModel SHALL 将 `tableElement.Rows` 中每行的 CellValues 转换为以列 BindingPath 为 key 的字典，存入 `ReportData.Tables[key]`
5. WHEN element 为普通控件时，THE DataEntryViewModel SHALL 按控件类型读取对应属性值（CheckBox 读 IsChecked、ComboBox 读 SelectedValue、其他读 Text），存入 `ReportData.Fields[key]`
6. WHEN 用户点击"生成报告"时，THE DataEntryViewModel SHALL 调用 `CollectReportData()` 替代传入空对象 `new {}`

---

### 需求 7：报告生成与持久化

**用户故事：** 作为数据录入员，我希望点击"生成报告"后系统能够将录入数据保存为报告文件，以便后续查阅和预览。

#### 验收标准

1. WHEN 用户点击"生成报告"时，THE DataEntryViewModel SHALL 先调用 `CollectReportData()` 收集数据，再调用数据验证，验证通过后调用 `DataService.GenerateReport(template, reportData)`
2. THE DataService SHALL 接受 `(TemplateData template, ReportData reportData)` 参数并返回 ReportInstance
3. THE DataService.GenerateReport SHALL 创建 ReportInstance，将 `Data` 字段设置为传入的 reportData
4. WHEN 报告生成成功时，THE DataEntryViewModel SHALL 调用 `TemplateService.SaveReportInstanceAsync(reportInstance)` 将报告持久化到磁盘
5. IF 数据验证失败时，THEN THE DataEntryViewModel SHALL 设置 ErrorMessage 并阻止报告生成

---

### 需求 8：报告预览渲染

**用户故事：** 作为医生，我希望在报告预览界面能够看到完整填充了录入数据的报告，以便核对报告内容是否正确。

#### 验收标准

1. THE TemplatePreviewView SHALL 接受 `(ReportInstance reportInstance, TemplateData template)` 参数进行初始化
2. WHEN 渲染 FixedElements 时，THE TemplatePreviewView SHALL 按元素原始属性渲染，不填充 ReportData 中的值
3. WHEN 渲染 EditableElements 中的普通字段时，THE TemplatePreviewView SHALL 以 `element.BindingPath`（或 ElementId）为 key 从 `reportInstance.Data.Fields` 读取值并渲染为 TextBlock
4. WHEN 渲染 EditableElements 中的 TableElement 时，THE TemplatePreviewView SHALL 以 `element.BindingPath`（或 ElementId）为 key 从 `reportInstance.Data.Tables` 读取行数据并渲染为表格
5. IF `reportInstance.Data.Fields` 中不存在对应 key 时，THEN THE TemplatePreviewView SHALL 渲染空字符串
6. IF `reportInstance.Data.Tables` 中不存在对应 key 时，THEN THE TemplatePreviewView SHALL 渲染空表格

---

### 需求 9：数据验证

**用户故事：** 作为数据录入员，我希望系统能够在生成报告前验证必填字段和格式规则，以便及时发现并纠正录入错误。

#### 验收标准

1. THE DataService.ValidateDataWithTemplate SHALL 接受 `(ReportData data, TemplateData template)` 参数并返回 ValidationResult
2. WHEN 字段的 ValidationRule.IsRequired 为 true 且 `ReportData.Fields[key]` 为空或纯空白时，THE DataService SHALL 在 ValidationResult 中添加该字段的错误信息
3. WHEN TableElement 的 ValidationRule.IsRequired 为 true 且 `ReportData.Tables[key]` 不存在或行数为 0 时，THE DataService SHALL 在 ValidationResult 中添加该字段的错误信息
4. WHEN 字段值不匹配 ValidationRule.RegexPattern 时，THE DataService SHALL 在 ValidationResult 中添加该字段的错误信息
5. WHEN 字段值长度超过 ValidationRule.MaxLength 时，THE DataService SHALL 在 ValidationResult 中添加该字段的错误信息
6. IF ValidationRule 为 null 时，THEN THE DataService SHALL 跳过该字段的验证

---

## 正确性属性

*属性是在系统所有有效执行中都应成立的特征或行为——本质上是关于系统应做什么的形式化陈述。属性是人类可读规范与机器可验证正确性保证之间的桥梁。*

### 属性 1：ControlType 枚举序列化 round-trip

*对任意* ControlElement 对象，将其序列化为 JSON 后再反序列化，得到的 ControlElement 的 Type 字段应与原始值相等，且序列化后的 JSON 中 Type 字段应为字符串形式（如 `"Label"`）而非数字。

**验证：需求 1.2、1.3**

### 属性 2：TableElement 多态反序列化正确性

*对任意* 包含 `"Type": "Table"` 的 JSON 片段，反序列化后得到的对象应为 TableElement 实例，且其 Columns 和 Rows 字段应被正确还原。

**验证：需求 2.1、2.2、2.4**

### 属性 3：ReportData 序列化 round-trip

*对任意* ReportData 对象（包含任意 Fields 和 Tables 内容），序列化为 JSON 后再反序列化，应得到与原始对象等价的 ReportData（Fields 和 Tables 的所有键值对均相同）。

**验证：需求 3.4**

### 属性 4：CollectReportData 覆盖所有 EditableElements

*对任意* 包含 n 个 EditableElements 的 TemplateData，调用 CollectReportData() 后，ReportData.Fields 中的 key 集合与 ReportData.Tables 中的 key 集合的并集，应等于所有 EditableElements 的 BindingPath（或 ElementId）集合，无遗漏。

**验证：需求 6.1、6.2、6.3**

### 属性 5：TableElement 行数据以列 BindingPath 为 key 收集

*对任意* TableElement，其 Rows 中每行的 CellValues 以列名为 key；CollectReportData() 收集后，ReportData.Tables 中对应条目的每行字典应以列的 BindingPath 为 key，而非列名。

**验证：需求 6.4**

### 属性 6：Table 控件空行预填数量等于 RowCount

*对任意* Rows 为空的 TableElement，DataEntryView 初始化后，TableElement.Rows.Count 应等于 TableElement.RowCount。

**验证：需求 5.7**

### 属性 7：必填字段为空时验证失败

*对任意* 标记了 IsRequired=true 的字段，若 ReportData.Fields 中对应值为空字符串或纯空白字符串，ValidateDataWithTemplate 返回的 ValidationResult 应包含该字段的错误条目。

**验证：需求 9.2**

### 属性 8：必填表格为空时验证失败

*对任意* 标记了 IsRequired=true 的 TableElement，若 ReportData.Tables 中对应 key 不存在或行数为 0，ValidateDataWithTemplate 返回的 ValidationResult 应包含该字段的错误条目。

**验证：需求 9.3**

### 属性 9：预览渲染值与 ReportData 一致

*对任意* ReportInstance 和对应的 TemplateData，TemplatePreviewView 渲染后，每个 EditableElement 对应位置显示的值应等于 ReportData.Fields[element.BindingPath] 或 ReportData.Tables[element.BindingPath] 中存储的值。

**验证：需求 8.3、8.4**
