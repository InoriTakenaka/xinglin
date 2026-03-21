# Code Quality Optimization Bugfix Design

## Overview

本文档描述对 xinglin WPF 医疗报告模板编辑器的综合代码质量修复方案。
共涉及 11 个 bug，覆盖调试弹窗残留、XAML 绑定语法错误、控件拖拽坐标单位不一致、
预览尺寸错误、按钮命令缺失和 UI 样式简陋六类问题。

修复策略以最小化改动为原则：精确定位每个 bug 的根本原因，仅修改必要代码，
通过 Fix Checking 验证 bug 已修复，通过 Preservation Checking 确保现有功能不受影响。

**重点关注**：控件拖拽逻辑（Bug 1.6 和 1.7）涉及坐标系转换，是本次修复的核心难点。


## Glossary

- **Bug_Condition (C)**：触发 bug 的条件集合，本文档中每个 bug 有独立的 isBugCondition 函数
- **Property (P)**：bug 修复后应满足的正确行为断言
- **Preservation**：修复不应影响的现有正确行为
- **EditorCanvas**：`TemplateEditorView.xaml` 中的 WPF Canvas 控件，承载所有已放置的模板控件
- **ControlElement**：`Models/CoreEntities/ControlElement.cs` 中的控件模型，Width/Height/X/Y 均以**毫米**为单位存储
- **ZoomLevel**：`TemplateEditorViewModel.ZoomLevel`，范围 25~200，表示百分比缩放比例
- **scale**：`ZoomLevel / 100.0`，实际缩放系数
- **Canvas 本地坐标**：`e.GetPosition(EditorCanvas)` 返回的坐标，已是 Canvas 内部坐标系（不含 ScaleTransform 影响），单位为像素
- **MM_TO_PIXEL**：`96.0 / 25.4 ≈ 3.7795`，毫米到 WPF DIP 像素的转换系数
- **DragOver 虚线框**：从工具箱拖入时显示的 `_dragOverRectangle`，跟随鼠标移动
- **MouseMove 虚线框**：画布内拖动已有控件时显示的 `_dragRectangle`


## Bug Details

### Bug Group A：调试弹窗残留（Bug 1.1）

**Bug Condition A：**

```
FUNCTION isBugConditionA(event)
  INPUT: event = 模板加载/切换事件
  OUTPUT: boolean

  RETURN InitializeFromTemplate() 被调用
         AND 方法体内存在 MessageBox.Show() 调用
END FUNCTION
```

**受影响位置：**
- `TemplateEditorView.xaml.cs` → `InitializeFromTemplate()`：4 处 `MessageBox.Show`
- `DataEntryView.xaml.cs` → `InitializeFromTemplate()`：3 处 `MessageBox.Show`

**示例：**
- 用户在模板树单击任意模板 → 触发 `TemplateTreeView_SelectedItemChanged` → 调用 `LoadTemplate` → 设置 `CurrentTemplate` → 触发 `InitializeFromTemplate` → 弹出"初始化信息"对话框，阻断操作

---

### Bug Group B：XAML 字符串字面量绑定（Bug 1.2、1.4）

**Bug Condition B：**

```
FUNCTION isBugConditionB(xamlElement)
  INPUT: xamlElement = XAML TextBlock 元素
  OUTPUT: boolean

  RETURN xamlElement.Text 属性值包含字面字符串 "{Binding ...}"
         AND 该字符串未被解析为 WPF Binding 表达式
END FUNCTION
```

**受影响位置：**
- `TemplateEditorView.xaml` 第 ~430 行：`Text="选中控件: {Binding SelectedControl.DisplayName}"`
- `DataEntryView.xaml`：`Text="错误数: {Binding ValidationErrors.Count}"`

**示例：**
- 选中画布上的文本框控件 → 属性面板显示 `选中控件: {Binding SelectedControl.DisplayName}` 而非 `选中控件: 文本框1`

---

### Bug Group C：属性面板可见性（Bug 1.3）

**Bug Condition C：**

```
FUNCTION isBugConditionC(state)
  INPUT: state = 当前 SelectedControl 状态
  OUTPUT: boolean

  RETURN state.SelectedControl == null
         AND 属性面板 Border 的 Visibility == Visible
END FUNCTION
```

**受影响位置：**
- `TemplateEditorView.xaml`：`Visibility="{Binding SelectedControl, Converter={StaticResource BooleanToVisibilityConverter}}"`
- `BooleanToVisibilityConverter.cs`：当 value 为非 null 对象时返回 Visible，null 时返回 Collapsed（逻辑正确）
- **实际问题**：`SelectedControl` 是 `ControlElement` 对象类型，Converter 已正确处理 null/非null，但初始状态下属性面板仍可见（初始值为 null 时应隐藏）

---

### Bug Group D：BooleanToVisibilityConverter Reverse（Bug 1.5）

**Bug Condition D：**

```
FUNCTION isBugConditionD(state)
  INPUT: state = IsValid=false 时的 DataEntryView 状态
  OUTPUT: boolean

  RETURN state.IsValid == false
         AND "无效" TextBlock 的 Visibility == Collapsed
END FUNCTION
```

**分析**：`BooleanToVisibilityConverter` 已实现 `ConverterParameter=Reverse` 逻辑，代码正确。
问题在于 `DataEntryView.xaml` 中 `BooleanToVisibilityConverter` 未在 UserControl.Resources 中声明，
而是依赖 `App.xaml` 全局资源，需确认资源键名一致性。


### Bug Group E：拖拽坐标单位不一致（Bug 1.6）— 重点

**背景**：`ControlElement` 的 `Width`/`Height` 以**毫米**存储（如 A4 纸宽 210mm）。
`EditorCanvas` 的 `Width`/`Height` 通过 `MillimeterToPixelConverter` 转换为**像素**（如 A4 宽 794px）。
`e.GetPosition(EditorCanvas)` 返回**像素**坐标。

**Bug Condition E（工具箱拖入 Drop）：**

```
FUNCTION isBugConditionE_Drop(dropEvent, control)
  INPUT: dropEvent = DragEventArgs（Drop 事件）
         control   = ControlElement（来自工具箱）
  OUTPUT: boolean

  pixelX = dropEvent.GetPosition(EditorCanvas).X / scale  // 像素
  pixelY = dropEvent.GetPosition(EditorCanvas).Y / scale  // 像素

  // Bug：control.Width 是毫米，EditorCanvas.Width 是像素，单位不同
  RETURN (pixelX + control.Width > EditorCanvas.Width)    // 毫米 vs 像素，比较无意义
      OR (pixelY + control.Height > EditorCanvas.Height)
END FUNCTION
```

**具体错误**：`EditorCanvas_Drop` 中边界限制：
```csharp
// 错误代码（control.Width 是毫米，EditorCanvas.Width 是像素）
if (left + control.Width > EditorCanvas.Width)
    left = EditorCanvas.Width - control.Width;
```
A4 纸宽 794px，一个 50mm 宽的控件：`794 - 50 = 744px`，但实际控件渲染宽度为 `50 * 3.78 ≈ 189px`，
边界限制错误，控件可被放置到画布右侧 `744~794px` 区间，但控件实际宽度 189px 会超出画布。

**Bug Condition E（DragOver 虚线框）：**

```
FUNCTION isBugConditionE_DragOver(dragEvent, control)
  INPUT: dragEvent = DragEventArgs（DragOver 事件）
         control   = ControlElement
  OUTPUT: boolean

  // DragOver 中已正确转换：pixelWidth = control.Width * 96.0 / 25.4
  // 但 Drop 中未做此转换，两者行为不一致
  RETURN Drop 中边界计算使用毫米值 != DragOver 中使用像素值
END FUNCTION
```

**注意**：`EditorCanvas_DragOver` 中已正确将毫米转为像素（`control.Width * 96.0 / 25.4`），
但 `EditorCanvas_Drop` 中遗漏了此转换，导致 DragOver 虚线框位置正确而 Drop 后实际位置错误。

**示例：**
- 从工具箱拖入一个 Width=100mm、Height=20mm 的文本框到画布右侧
- DragOver 虚线框正确显示在画布内（因为已转换）
- Drop 后控件 X 坐标可能为 `794 - 100 = 694px`，但控件实际渲染宽度 `100 * 3.78 = 378px`，`694 + 378 = 1072px > 794px`，超出画布

---

### Bug Group F：画布内拖动坐标重复缩放（Bug 1.7）— 重点

**背景**：`EditorCanvas` 的父 `Border` 应用了 `ScaleTransform`（缩放比例 = `ZoomLevel/100`）。
`e.GetPosition(EditorCanvas)` 在 WPF 中返回**相对于目标元素的本地坐标**，
已自动处理了 `ScaleTransform`，返回的是 Canvas 内部坐标系中的像素值（不含缩放）。

**Bug Condition F：**

```
FUNCTION isBugConditionF(mouseMoveEvent, zoomLevel)
  INPUT: mouseMoveEvent = MouseEventArgs（MouseMove 事件）
         zoomLevel      = 当前缩放比例（如 200 表示 200%）
  OUTPUT: boolean

  scale = zoomLevel / 100.0
  rawPos = mouseMoveEvent.GetPosition(EditorCanvas)  // 已是 Canvas 本地坐标
  
  // Bug：再次除以 scale，导致坐标被缩小
  adjustedPos.X = rawPos.X / scale  // 错误：重复缩放
  adjustedPos.Y = rawPos.Y / scale

  RETURN scale != 1.0  // 只要缩放不是 100%，就会出现偏移
END FUNCTION
```

**具体错误**：`ControlElement_MouseMove` 和 `ControlElement_MouseDown` 中：
```csharp
// 错误代码
Point currentPoint = e.GetPosition(EditorCanvas);
double scale = GetZoomScale();
currentPoint.X /= scale;  // GetPosition 已返回 Canvas 本地坐标，不需要再除以 scale
currentPoint.Y /= scale;
```

**影响分析**：
- 缩放 100%（scale=1.0）：无影响，`/1.0` 不改变值
- 缩放 200%（scale=2.0）：鼠标坐标被除以 2，控件移动速度是鼠标的 1/2，产生明显滞后
- 缩放 50%（scale=0.5）：鼠标坐标被乘以 2，控件移动速度是鼠标的 2 倍，控件飞出

**同样问题存在于 `ControlElement_MouseDown`**（记录 `_dragStartPoint` 时也做了 `/scale`），
导致 `_dragOffset` 计算也是错误的。

**示例：**
- 缩放设为 200%，拖动画布上的控件向右移动 100px（视觉距离）
- 实际 Canvas 坐标变化应为 50px（因为 200% 缩放下 1 视觉像素 = 0.5 Canvas 像素）
- 但代码将 `GetPosition` 返回的 50px 再除以 2，得到 25px，控件只移动了 25px，严重滞后


### Bug Group G：其他 UI 问题（Bug 1.8、1.9、1.10、1.11）

**Bug 1.8 - 模板树无法滚动：**
`MainWindow.xaml` 中 `TreeView` 被 `StackPanel` 包裹，`StackPanel` 会无限扩展高度，
导致 `TreeView` 无高度约束，无法触发滚动条。

**Bug 1.9 - 预览画布尺寸错误：**
`TemplatePreviewView.xaml` 中 `Canvas Width="210" Height="297"` 是硬编码毫米值，
应为像素值：A4 竖向 = 794×1123px，A4 横向 = 1123×794px。

**Bug 1.10 - 数据录入按钮未绑定命令：**
`DataEntryView.xaml` 中：
```xml
<Button Content="加载数据"/>   <!-- 缺少 Command="{Binding LoadDataCommand}" -->
<Button Content="保存数据"/>   <!-- 缺少 Command="{Binding SaveDataCommand}" -->
```
`DataEntryViewModel` 中已有 `LoadDataCommand` 和 `SaveDataCommand`（通过 `[RelayCommand]` 生成）。

**Bug 1.11 - UI 样式简陋：**
`Resources/Styles.xaml` 中 Button 样式无悬停/按下效果，工具栏无主色调，
`Brushes.xaml` 中已定义 `PrimaryBrush (#0078D7)` 但未在 Styles 中使用。

---

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors（必须保持不变）：**
- 3.1 从工具箱拖入控件时，DragOver 过程中显示虚线预览框，Drop 后控件添加到对应位置
- 3.2 选中画布上的控件时，属性面板显示该控件的通用属性和字体属性，双向绑定实时更新
- 3.3 工具栏"新建"、"保存"、"加载默认模板"、"预览"按钮正常执行 ViewModel 命令
- 3.4 右键表格控件选择"配置表格"打开 TableConfigWindow
- 3.5 缩放滑块和缩放按钮正确缩放编辑画布
- 3.6 模板树选择模板时加载对应模板到编辑器和数据录入视图
- 3.7 数据验证通过时"生成报告"按钮可用并生成报告实例

**Scope（不受影响的输入）：**
- 鼠标点击画布上的控件（选中操作）
- 属性面板中的文本框输入（双向绑定更新）
- 缩放操作（ZoomIn/ZoomOut/Reset）
- 纸张大小切换（ChangePaperSize）
- 表格配置窗口操作


## Hypothesized Root Cause

### 1. 调试弹窗（Bug 1.1）
开发阶段为调试初始化流程而添加的 `MessageBox.Show` 调用未在发布前清理。
涉及 `TemplateEditorView.xaml.cs::InitializeFromTemplate()` 和 `DataEntryView.xaml.cs::InitializeFromTemplate()`。

### 2. XAML 字符串字面量绑定（Bug 1.2、1.4）
开发者将 `{Binding ...}` 语法写在了 `Text="..."` 属性的字符串值内，而非作为 XAML 标记扩展使用。
WPF 中 `Text="选中控件: {Binding X}"` 是字面字符串，正确写法需要拆分为多个 `Run` 或使用 `StringFormat`。

### 3. 属性面板可见性（Bug 1.3）
`BooleanToVisibilityConverter` 对非 null 对象返回 Visible，逻辑正确。
但需确认初始状态下 `SelectedControl` 是否为 null（ViewModel 初始化时未赋值，应为 null）。
实际问题可能是 Converter 在 XAML 设计器中的行为，或初始化顺序问题。

### 4. 拖拽坐标单位不一致（Bug 1.6）
**根本原因**：`ControlElement` 模型设计时以毫米为单位存储尺寸（符合打印需求），
但 `EditorCanvas_Drop` 中直接用毫米值与像素值比较，遗漏了单位转换。
`EditorCanvas_DragOver` 中已正确转换（`control.Width * 96.0 / 25.4`），
说明开发者知道需要转换，但 Drop 处理函数中遗漏了。

### 5. 画布内拖动重复缩放（Bug 1.7）
**根本原因**：开发者误解了 `e.GetPosition(target)` 的行为。
WPF 的 `GetPosition(UIElement)` 返回的是相对于目标元素**视觉坐标系**的坐标，
对于应用了 `ScaleTransform` 的 Canvas，`GetPosition(EditorCanvas)` 已经返回 Canvas 内部坐标（未缩放的逻辑坐标），
不需要再除以 scale。开发者可能认为需要手动"反缩放"，导致双重缩放。

### 6. 预览画布尺寸（Bug 1.9）
`TemplatePreviewView.xaml` 中直接使用了 A4 纸张的毫米尺寸（210×297）作为像素值，
未经过 `MillimeterToPixelConverter` 转换，导致预览区域极小。

### 7. 按钮命令缺失（Bug 1.10）
`DataEntryView.xaml` 中"加载数据"和"保存数据"按钮在开发时未完成命令绑定，
ViewModel 中命令已实现但 XAML 中未连接。

### 8. UI 样式（Bug 1.11）
`Brushes.xaml` 中定义了主色调 `PrimaryBrush`，但 `Styles.xaml` 中的 Button 样式
未使用这些颜色，也未添加 `ControlTemplate` 来实现悬停/按下效果。


## Correctness Properties

Property 1: Bug Condition - 拖入画布边界限制正确性

_For any_ 从工具箱拖入的 `ControlElement`（任意 Width/Height 毫米值）和任意 Drop 位置（像素坐标），
固定后的 `EditorCanvas_Drop` 函数 SHALL 确保控件放置后的像素坐标满足：
`0 ≤ X ≤ (canvasWidthPx - controlWidthPx)` 且 `0 ≤ Y ≤ (canvasHeightPx - controlHeightPx)`，
其中 `controlWidthPx = control.Width * MM_TO_PIXEL`，`canvasWidthPx = EditorCanvas.Width`。

**Validates: Requirements 2.6**

Property 2: Bug Condition - 画布内拖动坐标精确性

_For any_ 缩放比例（25%~200%）和任意鼠标移动 delta，
固定后的 `ControlElement_MouseMove` 函数 SHALL 使控件位置变化量等于鼠标在 Canvas 本地坐标系中的移动量，
即 `newX = mouseCanvasX - dragOffsetX`（不含额外的 scale 除法）。

**Validates: Requirements 2.7**

Property 3: Preservation - 拖拽功能整体保持

_For any_ 不涉及边界限制计算和坐标缩放的拖拽操作（如 DragOver 虚线框显示、Drop 后控件添加到 Layout），
固定后的代码 SHALL 产生与原始代码相同的行为，保持拖拽预览和控件添加功能完整。

**Validates: Requirements 3.1, 3.2**

Property 4: Preservation - 非拖拽功能保持

_For any_ 不涉及本次修复的操作（选中控件、属性编辑、缩放、保存、加载等），
固定后的代码 SHALL 产生与原始代码完全相同的行为。

**Validates: Requirements 3.3, 3.4, 3.5, 3.6, 3.7, 3.8**


## Fix Implementation

### Changes Required

#### Fix A：移除调试弹窗

**File**: `xinglin/Views/TemplateEditorView.xaml.cs`
**Function**: `InitializeFromTemplate()`
**Change**: 删除所有 `MessageBox.Show(...)` 调用，保留逻辑代码，改用 `Console.WriteLine` 或 `Debug.WriteLine` 记录调试信息

**File**: `xinglin/Views/DataEntryView.xaml.cs`
**Function**: `InitializeFromTemplate()`
**Change**: 同上，删除所有 `MessageBox.Show(...)` 调用

---

#### Fix B：修复 XAML 字符串字面量绑定

**File**: `xinglin/Views/TemplateEditorView.xaml`
**Change**: 将字面量文本拆分为静态文本 + 绑定文本：
```xml
<!-- 修复前 -->
<TextBlock Text="选中控件: {Binding SelectedControl.DisplayName}" Margin="10"/>

<!-- 修复后 -->
<TextBlock Margin="10">
    <Run Text="选中控件: "/>
    <Run Text="{Binding SelectedControl.DisplayName}"/>
</TextBlock>
```

**File**: `xinglin/Views/DataEntryView.xaml`
**Change**: 使用 `StringFormat` 绑定：
```xml
<!-- 修复前 -->
<TextBlock Text="错误数: {Binding ValidationErrors.Count}" Margin="5,0"/>

<!-- 修复后 -->
<TextBlock Text="{Binding ValidationErrors.Count, StringFormat='错误数: {0}'}" Margin="5,0"/>
```

---

#### Fix C：修复属性面板可见性

**File**: `xinglin/Views/TemplateEditorView.xaml`
**Analysis**: `BooleanToVisibilityConverter` 已正确处理 null（返回 Collapsed）和非 null 对象（返回 Visible）。
当前绑定 `Visibility="{Binding SelectedControl, Converter={StaticResource BooleanToVisibilityConverter}}"` 逻辑正确。
需要验证初始状态，如果仍有问题，考虑改用 `ObjectToVisibilityConverter` 或确保 ViewModel 初始化时 `SelectedControl = null`。

---

#### Fix D：修复 BooleanToVisibilityConverter Reverse

**File**: `xinglin/Views/DataEntryView.xaml`
**Analysis**: `BooleanToVisibilityConverter` 已实现 Reverse 逻辑。
需确认 `DataEntryView.xaml` 中引用的 `BooleanToVisibilityConverter` 资源键与 `App.xaml` 中注册的一致。
当前 `App.xaml` 通过 `Converters.xaml` 注册了 `BooleanToVisibilityConverter`，`DataEntryView.xaml` 直接使用 `{StaticResource BooleanToVisibilityConverter}`，应可正常工作。
如果"无效"文本不显示，需检查 `IsValid` 初始值和绑定路径。


---

#### Fix E：修复拖入画布坐标单位不一致（核心修复）

**File**: `xinglin/Views/TemplateEditorView.xaml.cs`
**Function**: `EditorCanvas_Drop()`

**Specific Changes**:
1. 在 Drop 处理中，将 `control.Width` 和 `control.Height`（毫米）转换为像素后再做边界限制
2. 保持 `AddControlAtPosition` 传入的坐标为像素值（ViewModel 中 `control.X/Y` 存储像素坐标）

```csharp
// 修复前（错误：毫米 vs 像素）
if (left + control.Width > EditorCanvas.Width)
    left = EditorCanvas.Width - control.Width;
if (top + control.Height > EditorCanvas.Height)
    top = EditorCanvas.Height - control.Height;

// 修复后（正确：统一使用像素）
const double MM_TO_PIXEL = 96.0 / 25.4;
double controlWidthPx = control.Width * MM_TO_PIXEL;
double controlHeightPx = control.Height * MM_TO_PIXEL;

if (left < 0) left = 0;
if (top < 0) top = 0;
if (left + controlWidthPx > EditorCanvas.Width)
    left = EditorCanvas.Width - controlWidthPx;
if (top + controlHeightPx > EditorCanvas.Height)
    top = EditorCanvas.Height - controlHeightPx;
```

**注意**：`AddControlAtPosition` 将 `left/top`（像素）赋值给 `control.X/Y`，
而 `ControlElement.X/Y` 在模型中是毫米单位，这里存在另一个潜在的单位混用问题。
需要确认 `control.X/Y` 的语义：如果是像素则 Canvas 绑定正确；如果是毫米则需要在 `AddControlAtPosition` 中转换回毫米。
根据 XAML 中 `Canvas.Left="{Binding X}"` 直接绑定（无 Converter），推断 `X/Y` 存储的是像素值，与 `Width/Height` 存储毫米不同。
**修复时需保持 X/Y 为像素，仅修复边界限制中的单位转换。**

---

#### Fix F：修复画布内拖动重复缩放（核心修复）

**File**: `xinglin/Views/TemplateEditorView.xaml.cs`
**Functions**: `ControlElement_MouseDown()`, `ControlElement_MouseMove()`

**Root Cause Confirmation**：
`e.GetPosition(EditorCanvas)` 在 WPF 中返回相对于 `EditorCanvas` 的**逻辑坐标**，
已经是 Canvas 内部坐标系（ScaleTransform 不影响 GetPosition 的返回值，因为 GetPosition 使用视觉树变换矩阵的逆变换）。
因此不需要再除以 scale。

```csharp
// 修复前（错误：重复缩放）
Point currentPoint = e.GetPosition(EditorCanvas);
double scale = GetZoomScale();
currentPoint.X /= scale;  // 错误
currentPoint.Y /= scale;  // 错误

// 修复后（正确：直接使用 Canvas 本地坐标）
Point currentPoint = e.GetPosition(EditorCanvas);
// 不需要 scale 转换，GetPosition 已返回 Canvas 本地坐标
```

**同样修复 `ControlElement_MouseDown` 中的 `_dragStartPoint` 计算：**
```csharp
// 修复前
_dragStartPoint = e.GetPosition(EditorCanvas);
double scale = GetZoomScale();
_dragStartPoint.X /= scale;  // 错误
_dragStartPoint.Y /= scale;  // 错误

// 修复后
_dragStartPoint = e.GetPosition(EditorCanvas);
// 直接使用，无需 scale 转换
```

**边界限制中的 `_dragRectangle.Width/Height`**：
`_dragRectangle` 在 `ControlElement_MouseDown` 中创建时使用 `control.Width/Height`（毫米），
这与 Canvas 像素坐标不一致，也需要修复：
```csharp
// 修复前
_dragRectangle = new Rectangle { Width = control.Width, Height = control.Height, ... };

// 修复后
const double MM_TO_PIXEL = 96.0 / 25.4;
_dragRectangle = new Rectangle {
    Width = control.Width * MM_TO_PIXEL,
    Height = control.Height * MM_TO_PIXEL, ...
};
```


---

#### Fix G：其他 UI 修复

**Fix 1.8 - 模板树滚动**
**File**: `xinglin/MainWindow.xaml`
**Change**: 将 `StackPanel` 替换为 `DockPanel`，并为 `TreeView` 设置 `VerticalAlignment="Stretch"` 和 `ScrollViewer.VerticalScrollBarVisibility="Auto"`，或直接用 `ScrollViewer` 包裹 `TreeView` 并设置 `MaxHeight`：
```xml
<!-- 修复后 -->
<DockPanel>
    <TextBlock DockPanel.Dock="Top" Text="模板树" FontWeight="Bold" Margin="10"/>
    <TreeView x:Name="TemplateTreeView" Margin="5" ScrollViewer.VerticalScrollBarVisibility="Auto"/>
</DockPanel>
```

**Fix 1.9 - 预览画布尺寸**
**File**: `xinglin/Views/TemplatePreviewView.xaml`
**Change**: 将硬编码毫米值改为正确的像素值（A4 竖向）：
```xml
<!-- 修复前 -->
<Canvas x:Name="PreviewCanvas" Background="White" Width="210" Height="297">

<!-- 修复后 -->
<Canvas x:Name="PreviewCanvas" Background="White" Width="794" Height="1123">
```
或通过 `MillimeterToPixelConverter` 绑定到模板的纸张尺寸（更灵活）。

**Fix 1.10 - 数据录入按钮命令绑定**
**File**: `xinglin/Views/DataEntryView.xaml`
**Change**:
```xml
<!-- 修复前 -->
<Button Content="加载数据"/>
<Button Content="保存数据"/>

<!-- 修复后 -->
<Button Content="加载数据" Command="{Binding LoadDataCommand}"/>
<Button Content="保存数据" Command="{Binding SaveDataCommand}"/>
```
注意：`LoadDataCommand` 和 `SaveDataCommand` 需要参数（key），需要确认是否使用默认 key 或通过 `CommandParameter` 传入。

**Fix 1.11 - UI 样式现代化**
**File**: `xinglin/Resources/Styles.xaml`
**Change**: 为 Button 添加 `ControlTemplate` 实现悬停/按下效果，使用 `PrimaryBrush` 作为工具栏背景：
```xml
<Style TargetType="Button">
    <Setter Property="Background" Value="{StaticResource SecondaryBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
            <Setter Property="Foreground" Value="White"/>
        </Trigger>
        <Trigger Property="IsPressed" Value="True">
            <Setter Property="Background" Value="#005A9E"/>
        </Trigger>
    </Style.Triggers>
</Style>
```


## Testing Strategy

### Validation Approach

测试分两阶段：
1. **探索阶段**：在未修复代码上运行测试，确认 bug 存在并理解根本原因
2. **验证阶段**：修复后运行 Fix Checking 和 Preservation Checking，确认修复正确且无回归

### Exploratory Bug Condition Checking

**Goal**: 在未修复代码上暴露 bug，确认根本原因分析正确。

**Test Plan**: 针对拖拽坐标 bug（1.6、1.7）编写单元测试，在未修复代码上运行，观察失败。

**Test Cases**:
1. **Drop 边界测试**：创建 Width=100mm 的控件，Drop 到 Canvas 右侧（X=700px，Canvas 宽 794px），
   验证 `left + controlWidthPx <= 794`（未修复时会失败，因为 `794 - 100 = 694`，但 `694 + 378 > 794`）
2. **MouseMove 缩放测试**：设置 ZoomLevel=200，模拟鼠标从 (100,100) 移动到 (200,200)，
   验证控件位置变化为 (100,100)（未修复时变化为 (50,50)）
3. **MouseMove 缩放测试 50%**：设置 ZoomLevel=50，模拟鼠标移动 100px，
   验证控件移动 100px（未修复时移动 200px）
4. **调试弹窗测试**：调用 `InitializeFromTemplate()`，验证不弹出 MessageBox（未修复时会弹出）

**Expected Counterexamples**:
- Drop 边界：控件 X 坐标 + 像素宽度 > Canvas 宽度（超出画布）
- MouseMove：控件移动距离 ≠ 鼠标移动距离（缩放比例不为 1 时）

---

### Fix Checking

**Goal**: 验证修复后所有 bug 条件下行为正确。

**Pseudocode（拖拽边界）：**
```
FOR ALL control WHERE control.Width IN [10mm, 50mm, 100mm, 200mm]
FOR ALL dropX WHERE dropX IN [0, 400, 700, 800, 1000]  // 包括超出范围的值
  result := EditorCanvas_Drop_fixed(control, dropX, canvasWidth=794)
  controlWidthPx := control.Width * 96.0 / 25.4
  ASSERT result.X >= 0
  ASSERT result.X + controlWidthPx <= 794
END FOR
```

**Pseudocode（画布内拖动）：**
```
FOR ALL zoomLevel IN [25, 50, 100, 150, 200]
FOR ALL mouseDelta IN [(10,0), (0,10), (50,50), (-30,20)]
  result := ControlElement_MouseMove_fixed(zoomLevel, mouseDelta)
  ASSERT result.controlDelta == mouseDelta  // 控件移动量等于鼠标移动量
END FOR
```

---

### Preservation Checking

**Goal**: 验证修复后非 bug 路径行为不变。

**Pseudocode：**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT originalFunction(input) = fixedFunction(input)
END FOR
```

**Testing Approach**: 属性测试适合验证拖拽坐标修复的保持性，因为需要覆盖大量缩放比例和坐标组合。

**Test Cases**:
1. **DragOver 虚线框保持**：验证 DragOver 中虚线框位置计算不受 Drop 修复影响
2. **ZoomLevel=100 时拖动保持**：缩放 100% 时 `/scale` 等于 `/1.0`，修复前后行为相同
3. **控件选中保持**：点击控件后 `SelectedControl` 正确更新，属性面板显示
4. **属性双向绑定保持**：修改属性面板文本框，控件 `DisplayName` 同步更新

---

### Unit Tests

**File**: `xinglin/Tests/TemplateEditorViewModelTests.cs`（已存在，可扩展）

- 测试 `AddControlAtPosition`：验证控件 X/Y 坐标正确设置
- 测试 `SelectControl`：验证 `SelectedControl` 更新和 `IsSelected` 标志
- 测试 `BooleanToVisibilityConverter`：null → Collapsed，非 null → Visible，Reverse 参数
- 测试 `MillimeterToPixelConverter`：210mm → 794px（±1px 误差），0mm → 0px

---

### Property-Based Tests

- **Property 1（拖入边界）**：生成随机 `control.Width`（1~300mm）和随机 `dropX`（-100~1000px），
  验证修复后 `resultX + controlWidthPx ∈ [0, canvasWidth]`
- **Property 2（画布内拖动）**：生成随机 `zoomLevel`（25~200）和随机鼠标 delta，
  验证 `controlDelta == mouseDelta`（不受 scale 影响）
- **Property 3（保持性）**：生成随机非 bug 输入（ZoomLevel=100 的拖动），
  验证修复前后结果相同

---

### Integration Tests

- 完整拖拽流程：从工具箱拖入控件 → DragOver 显示虚线框 → Drop 后控件在画布内 → 选中控件 → 属性面板显示
- 缩放后拖动：设置 ZoomLevel=150 → 拖动画布上的控件 → 验证控件跟随鼠标精确移动
- 模板加载流程：选择模板树节点 → 模板加载 → 无弹窗 → 编辑器和数据录入视图更新
- 数据录入流程：加载数据 → 修改字段 → 保存数据 → 验证按钮响应
