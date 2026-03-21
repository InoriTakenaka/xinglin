# Bugfix Requirements Document

## Introduction

本文档描述对 xinglin WPF 医疗报告模板编辑器项目进行的综合代码质量改进。
审查发现多类问题：残留的调试弹窗严重干扰用户操作、XAML 数据绑定语法错误导致 UI 显示异常、坐标单位不一致引发拖放定位错误、以及整体 UI 样式简陋缺乏现代感。
这些问题覆盖逻辑缺陷、UI 渲染错误和用户体验问题三个层面。

---

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN 用户切换或加载模板时 THEN 系统弹出多个 MessageBox 调试对话框（"初始化信息"、"初始化完成"等），阻断用户操作流程

1.2 WHEN 属性面板显示选中控件名称时 THEN 系统显示字面文本 `选中控件: {Binding SelectedControl.DisplayName}` 而非实际控件名称（XAML 字符串字面量中嵌入 Binding 语法无效）

1.3 WHEN 用户未选中任何控件时 THEN 系统仍然显示右侧属性面板（BooleanToVisibilityConverter 绑定到对象类型 SelectedControl，无法正确判断非空）

1.4 WHEN 数据录入视图显示验证错误数量时 THEN 系统显示字面文本 `错误数: {Binding ValidationErrors.Count}` 而非实际数字

1.5 WHEN 数据验证失败时 THEN 系统不显示"无效"状态文本（BooleanToVisibilityConverter 未实现 ConverterParameter=Reverse 逻辑）

1.6 WHEN 用户从工具箱拖放控件到编辑画布时 THEN 系统使用毫米单位的控件尺寸与像素单位的画布尺寸比较，导致边界限制计算错误，控件可能被放置到画布外

1.7 WHEN 用户拖动已有控件在画布内移动时 THEN 系统对鼠标坐标重复除以缩放比例（e.GetPosition 已返回 Canvas 本地坐标），导致高缩放比例下控件位置偏移

1.8 WHEN 用户查看左侧模板树时 THEN 系统使用 StackPanel 包裹 TreeView，TreeView 无高度限制无法滚动，模板数量多时超出窗口范围

1.9 WHEN 用户点击预览功能时 THEN 系统将预览画布尺寸设为 210×297 像素（毫米值），导致预览区域极小（约 5.5cm），无法正常预览报告

1.10 WHEN 用户点击数据录入视图中的"加载数据"或"保存数据"按钮时 THEN 系统无任何响应（按钮未绑定 Command）

1.11 WHEN 应用程序整体 UI 呈现时 THEN 系统显示样式简陋的界面：按钮无悬停效果、工具箱控件无图标区分、颜色主题未应用到主要控件、整体视觉层次感弱

### Expected Behavior (Correct)

2.1 WHEN 用户切换或加载模板时 THEN 系统 SHALL 静默完成初始化，不弹出任何调试对话框，仅在状态栏或日志中记录信息

2.2 WHEN 属性面板显示选中控件名称时 THEN 系统 SHALL 正确显示当前选中控件的 DisplayName 属性值

2.3 WHEN 用户未选中任何控件时 THEN 系统 SHALL 隐藏右侧属性面板；当选中控件时 SHALL 显示属性面板

2.4 WHEN 数据录入视图显示验证错误数量时 THEN 系统 SHALL 正确显示实际错误数量数字

2.5 WHEN 数据验证失败时 THEN 系统 SHALL 显示红色"无效"状态文本

2.6 WHEN 用户从工具箱拖放控件到编辑画布时 THEN 系统 SHALL 将控件尺寸转换为像素单位后再进行边界限制计算，确保控件始终放置在画布范围内

2.7 WHEN 用户拖动已有控件在画布内移动时 THEN 系统 SHALL 直接使用 Canvas 本地坐标计算位置，不重复应用缩放系数，确保控件跟随鼠标精确移动

2.8 WHEN 用户查看左侧模板树时 THEN 系统 SHALL 使模板树可滚动，在模板数量多时不超出窗口范围

2.9 WHEN 用户点击预览功能时 THEN 系统 SHALL 将预览画布尺寸转换为正确的像素值（A4: 794×1123px），使预览区域正常显示报告内容

2.10 WHEN 用户点击数据录入视图中的"加载数据"或"保存数据"按钮时 THEN 系统 SHALL 执行对应的 LoadDataCommand 和 SaveDataCommand

2.11 WHEN 应用程序整体 UI 呈现时 THEN 系统 SHALL 显示具有现代感的界面：按钮有悬停和按下效果、主色调 (#0078D7) 应用到工具栏和标题区域、工具箱控件有清晰的视觉分组、整体视觉层次感清晰

### Unchanged Behavior (Regression Prevention)

3.1 WHEN 用户从工具箱拖放控件到画布时 THEN 系统 SHALL CONTINUE TO 在拖拽过程中显示虚线预览框，并在放开鼠标后将控件添加到对应位置

3.2 WHEN 用户选中画布上的控件时 THEN 系统 SHALL CONTINUE TO 在属性面板中显示该控件的通用属性（名称、坐标、尺寸、绑定路径）和字体属性

3.3 WHEN 用户修改属性面板中的控件属性时 THEN 系统 SHALL CONTINUE TO 通过双向绑定实时更新画布上的控件显示

3.4 WHEN 用户点击"新建"、"保存"、"加载默认模板"、"预览"等工具栏按钮时 THEN 系统 SHALL CONTINUE TO 执行对应的 ViewModel 命令

3.5 WHEN 用户在表格控件上右键选择"配置表格"时 THEN 系统 SHALL CONTINUE TO 打开 TableConfigWindow 进行列和行的配置

3.6 WHEN 用户调整缩放滑块或点击缩放按钮时 THEN 系统 SHALL CONTINUE TO 正确缩放编辑画布

3.7 WHEN 用户在模板树中选择模板时 THEN 系统 SHALL CONTINUE TO 加载对应模板到编辑器和数据录入视图

3.8 WHEN 数据验证通过时 THEN 系统 SHALL CONTINUE TO 允许用户点击"生成报告"按钮并生成报告实例
