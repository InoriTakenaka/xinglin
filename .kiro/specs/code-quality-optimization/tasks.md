# Implementation Plan

- [x] 1. 编写 Bug Condition 探索测试（修复前运行）
  - **Property 1: Bug Condition** - 拖拽坐标单位不一致与重复缩放 Bug
  - **CRITICAL**: 此测试 MUST FAIL on unfixed code — 失败即证明 bug 存在
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: 此测试编码了期望行为，修复后通过即验证 bug 已修复
  - **GOAL**: 暴露反例，证明 bug 存在
  - **Scoped PBT Approach**: 针对确定性 bug，将属性范围限定到具体失败场景以确保可复现
  - 测试 1：Drop 边界计算错误（Bug 1.6）
    - 创建 Width=100mm 的 ControlElement，Drop 到 Canvas X=700px（Canvas 宽 794px）
    - 断言：`resultX + (100 * 96.0/25.4) <= 794`（未修复时失败，因为 `794 - 100 = 694`，`694 + 378 > 794`）
    - 属性测试：对任意 Width ∈ [10mm, 300mm] 和任意 dropX ∈ [-100, 1000]，验证边界约束
  - 测试 2：MouseMove 重复缩放（Bug 1.7）
    - 设置 ZoomLevel=200，模拟鼠标从 (100,100) 移动到 (200,200)（Canvas 本地坐标）
    - 断言：控件位置变化量 == (100,100)（未修复时变化量为 (50,50)）
    - 属性测试：对任意 ZoomLevel ∈ [25, 200]（排除 100），验证控件移动量等于鼠标移动量
  - 测试文件：`xinglin/Tests/BugConditionTests.cs`（新建）
  - 在未修复代码上运行，**EXPECTED OUTCOME**: 测试 FAILS（证明 bug 存在）
  - 记录反例（如：`Drop(Width=100mm, X=700) → resultX=694, 694+378=1072 > 794`）
  - 任务完成标准：测试已编写、已运行、失败已记录
  - _Requirements: 1.6, 1.7_

- [ ]* 2. 编写 Preservation 属性测试（修复前运行）
  - **Property 2: Preservation** - 非 Bug 路径行为保持
  - **IMPORTANT**: 遵循 observation-first 方法论
  - 在未修复代码上观察非 bug 条件下的行为：
    - 观察：ZoomLevel=100 时，鼠标移动 (50,50)，控件移动 (50,50)（`/1.0` 无影响）
    - 观察：DragOver 虚线框位置计算（已正确使用 `control.Width * 96.0/25.4`）
    - 观察：Drop 到画布中央（X=300, Width=50mm），控件放置在 X=300（未超出边界，边界 bug 不触发）
  - 编写属性测试：
    - 属性 2a：ZoomLevel=100 时，任意鼠标 delta，控件移动量 == 鼠标移动量（修复前后相同）
    - 属性 2b：Drop 到画布中央（不触发边界限制），控件 X 坐标 == dropX（修复前后相同）
    - 属性 2c：DragOver 虚线框宽度 == `control.Width * 96.0/25.4`（不受 Drop 修复影响）
  - 测试文件：`xinglin/Tests/PreservationTests.cs`（新建）
  - 在未修复代码上运行，**EXPECTED OUTCOME**: 测试 PASSES（确认基线行为）
  - 任务完成标准：测试已编写、已运行、在未修复代码上通过
  - _Requirements: 3.1, 3.2, 3.3, 3.6_

- [x] 3. 修复所有 Bug

  - [x] 3.1 修复调试弹窗残留（Bug 1.1）
    - 删除 `TemplateEditorView.xaml.cs::InitializeFromTemplate()` 中所有 `MessageBox.Show(...)` 调用（共 4 处）
    - 删除 `DataEntryView.xaml.cs::InitializeFromTemplate()` 中所有 `MessageBox.Show(...)` 调用（共 3 处）
    - 保留逻辑代码，将调试信息改为 `Debug.WriteLine(...)` 或直接删除
    - _Bug_Condition: isBugConditionA(event) — InitializeFromTemplate() 被调用且方法体内存在 MessageBox.Show()_
    - _Expected_Behavior: 模板加载/切换时静默完成，不弹出任何对话框_
    - _Preservation: 模板加载逻辑（ControlElement 渲染、属性面板更新）保持不变_
    - _Requirements: 1.1, 2.1_

  - [x] 3.2 修复 XAML 字符串字面量绑定（Bug 1.2、1.4）
    - 修复 `TemplateEditorView.xaml`：将 `Text="选中控件: {Binding SelectedControl.DisplayName}"` 拆分为 `<Run>` 组合
    - 修复 `DataEntryView.xaml`：将 `Text="错误数: {Binding ValidationErrors.Count}"` 改为 `StringFormat` 绑定
    - _Bug_Condition: isBugConditionB(xamlElement) — TextBlock.Text 属性值包含字面字符串 "{Binding ...}"_
    - _Expected_Behavior: 属性面板正确显示控件 DisplayName；数据录入视图正确显示错误数量_
    - _Preservation: 其他 TextBlock 绑定和 UI 布局不受影响_
    - _Requirements: 1.2, 1.4, 2.2, 2.4_

  - [x] 3.3 修复属性面板可见性（Bug 1.3）
    - 确认 `TemplateEditorView.xaml` 中属性面板 `Visibility` 绑定到 `SelectedControl`，Converter 为 `BooleanToVisibilityConverter`
    - 验证 ViewModel 初始化时 `SelectedControl = null`，确保初始状态属性面板隐藏
    - 如果 Converter 对对象类型判断有问题，改用 `NullToVisibilityConverter` 或添加 `ObjectToVisibilityConverter`
    - _Bug_Condition: isBugConditionC(state) — SelectedControl == null 且属性面板 Visibility == Visible_
    - _Expected_Behavior: 未选中控件时属性面板隐藏，选中后显示_
    - _Preservation: 选中控件后属性面板正常显示，双向绑定正常工作_
    - _Requirements: 1.3, 2.3_

  - [x] 3.4 修复 BooleanToVisibilityConverter Reverse（Bug 1.5）
    - 确认 `DataEntryView.xaml` 中 `BooleanToVisibilityConverter` 资源键与 `App.xaml`/`Converters.xaml` 中注册的一致
    - 验证 `IsValid=false` 时"无效"TextBlock 的 Visibility 绑定使用了 `ConverterParameter=Reverse`
    - 确认 `BooleanToVisibilityConverter.cs` 已实现 Reverse 逻辑（false → Visible）
    - _Bug_Condition: isBugConditionD(state) — IsValid=false 且"无效"TextBlock Visibility == Collapsed_
    - _Expected_Behavior: 数据验证失败时显示红色"无效"状态文本_
    - _Preservation: 数据验证通过时"无效"文本隐藏，"有效"状态正常显示_
    - _Requirements: 1.5, 2.5_

  - [x] 3.5 修复拖入画布坐标单位不一致（Bug 1.6）— 核心修复
    - 在 `TemplateEditorView.xaml.cs::EditorCanvas_Drop()` 中添加毫米到像素转换
    - 将 `control.Width` 和 `control.Height`（毫米）乘以 `96.0/25.4` 转换为像素后再做边界限制
    - 修复前：`if (left + control.Width > EditorCanvas.Width)` （毫米 vs 像素，错误）
    - 修复后：`double controlWidthPx = control.Width * 96.0 / 25.4; if (left + controlWidthPx > EditorCanvas.Width)`
    - 保持 `AddControlAtPosition` 传入的 `left/top` 为像素值（`control.X/Y` 存储像素坐标）
    - _Bug_Condition: isBugConditionE_Drop — `left + control.Width > EditorCanvas.Width`（毫米 vs 像素比较）_
    - _Expected_Behavior: 0 ≤ resultX ≤ (canvasWidthPx - controlWidthPx)，controlWidthPx = control.Width * MM_TO_PIXEL_
    - _Preservation: DragOver 虚线框位置计算不受影响；Drop 到画布中央时控件位置不变_
    - _Requirements: 1.6, 2.6, 3.1_

  - [x] 3.6 修复画布内拖动重复缩放（Bug 1.7）— 核心修复
    - 在 `TemplateEditorView.xaml.cs::ControlElement_MouseDown()` 中移除 `_dragStartPoint` 的 `/scale` 操作
    - 在 `TemplateEditorView.xaml.cs::ControlElement_MouseMove()` 中移除 `currentPoint` 的 `/scale` 操作
    - 修复 `ControlElement_MouseDown` 中 `_dragRectangle` 尺寸：将 `control.Width/Height`（毫米）转换为像素
    - 修复前：`currentPoint.X /= scale;`（重复缩放，错误）
    - 修复后：直接使用 `e.GetPosition(EditorCanvas)` 返回值（已是 Canvas 本地坐标）
    - _Bug_Condition: isBugConditionF — scale != 1.0 时 GetPosition 返回值再次除以 scale_
    - _Expected_Behavior: newX = mouseCanvasX - dragOffsetX（不含额外 scale 除法）_
    - _Preservation: ZoomLevel=100 时拖动行为不变（`/1.0` 无影响）；缩放操作本身不受影响_
    - _Requirements: 1.7, 2.7, 3.1, 3.6_

  - [x] 3.7 修复模板树无法滚动（Bug 1.8）
    - 修改 `MainWindow.xaml`：将包裹 `TreeView` 的 `StackPanel` 替换为 `DockPanel`
    - 为 `TreeView` 设置 `ScrollViewer.VerticalScrollBarVisibility="Auto"`
    - _Bug_Condition: StackPanel 包裹 TreeView 导致无高度约束_
    - _Expected_Behavior: 模板数量多时模板树可滚动_
    - _Preservation: 模板树选择功能、模板加载流程不受影响_
    - _Requirements: 1.8, 2.8, 3.7_

  - [x] 3.8 修复预览画布尺寸错误（Bug 1.9）
    - 修改 `TemplatePreviewView.xaml`：将 `Canvas Width="210" Height="297"` 改为正确像素值
    - A4 竖向：`Width="794" Height="1123"`；或通过 `MillimeterToPixelConverter` 绑定到模板纸张尺寸
    - _Bug_Condition: Canvas 使用毫米值（210×297）作为像素尺寸_
    - _Expected_Behavior: 预览画布尺寸为 794×1123px（A4 竖向），正常显示报告内容_
    - _Preservation: 预览视图中控件渲染逻辑不受影响_
    - _Requirements: 1.9, 2.9_

  - [x] 3.9 修复数据录入按钮命令缺失（Bug 1.10）
    - 修改 `DataEntryView.xaml`：为"加载数据"按钮添加 `Command="{Binding LoadDataCommand}"`
    - 修改 `DataEntryView.xaml`：为"保存数据"按钮添加 `Command="{Binding SaveDataCommand}"`
    - 确认 `DataEntryViewModel` 中 `LoadDataCommand` 和 `SaveDataCommand` 已通过 `[RelayCommand]` 生成
    - _Bug_Condition: 按钮未绑定 Command，点击无响应_
    - _Expected_Behavior: 点击"加载数据"执行 LoadDataCommand，点击"保存数据"执行 SaveDataCommand_
    - _Preservation: 其他按钮命令绑定不受影响；数据验证和报告生成流程不受影响_
    - _Requirements: 1.10, 2.10, 3.8_

  - [x] 3.10 修复 UI 样式简陋（Bug 1.11）
    - 修改 `Resources/Styles.xaml`：为 Button 添加悬停（`IsMouseOver`）和按下（`IsPressed`）触发器
    - 在触发器中使用 `Brushes.xaml` 中已定义的 `PrimaryBrush (#0078D7)` 和 `SecondaryBrush`
    - 为工具栏 `ToolBar` 设置 `Background="{StaticResource PrimaryBrush}"`
    - _Bug_Condition: Button 样式无悬停/按下效果，PrimaryBrush 未使用_
    - _Expected_Behavior: 按钮有悬停变色和按下效果，工具栏显示主色调_
    - _Preservation: 现有按钮功能和命令绑定不受样式修改影响_
    - _Requirements: 1.11, 2.11_

  - [ ]* 3.11 验证 Bug Condition 探索测试现在通过
    - **Property 1: Expected Behavior** - 拖拽坐标单位不一致与重复缩放 Bug
    - **IMPORTANT**: 重新运行任务 1 中编写的 SAME 测试，不要编写新测试
    - 任务 1 中的测试编码了期望行为，通过即确认 bug 已修复
    - 运行 `xinglin/Tests/BugConditionTests.cs` 中的所有测试
    - **EXPECTED OUTCOME**: 测试 PASSES（确认 bug 已修复）
    - _Requirements: 2.6, 2.7_

  - [ ]* 3.12 验证 Preservation 测试仍然通过
    - **Property 2: Preservation** - 非 Bug 路径行为保持
    - **IMPORTANT**: 重新运行任务 2 中编写的 SAME 测试，不要编写新测试
    - 运行 `xinglin/Tests/PreservationTests.cs` 中的所有测试
    - **EXPECTED OUTCOME**: 测试 PASSES（确认无回归）
    - 确认所有保持性测试通过（无回归）
    - _Requirements: 3.1, 3.2, 3.3, 3.6_

- [ ]* 4. Checkpoint — 确保所有测试通过
  - 运行全部测试（BugConditionTests + PreservationTests + 已有的 TemplateEditorViewModelTests）
  - 确认所有测试通过，如有疑问请询问用户
  - 手动验证关键流程：模板加载无弹窗、拖拽控件位置正确、缩放后拖动精确、预览尺寸正常、按钮响应正常
