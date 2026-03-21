# xinglin模板编辑器优化 - 实施计划

## [x] 任务1：修复MVVM原则违反
- **优先级**：P0
- **依赖**：无
- **描述**：
  - 重构TemplateEditorViewModel，移除直接创建View的代码
  - 使用事件或消息机制在ViewModel和View之间通信
  - 确保ViewModel不再直接依赖View
- **验收标准**：AC-1
- **测试要求**：
  - `human-judgment` TR-1.1：检查ViewModel是否不再直接创建View实例
  - `human-judgment` TR-1.2：检查ViewModel和View之间是否通过事件或消息机制通信
- **备注**：需要在TemplateEditorView中添加事件处理，响应ViewModel的事件

## [x] 任务2：抽象文件存储操作
- **优先级**：P0
- **依赖**：无
- **描述**：
  - 创建IFileStorageService接口，定义文件存储相关的方法
  - 实现LocalFileStorageService，使用本地文件系统存储
  - 修改TemplateService，使用IFileStorageService接口
- **验收标准**：AC-2
- **测试要求**：
  - `programmatic` TR-2.1：验证TemplateService不再直接依赖文件系统
  - `programmatic` TR-2.2：验证IFileStorageService接口和实现是否正确
- **备注**：需要在依赖注入配置中注册IFileStorageService

## [x] 任务3：完善错误处理机制
- **优先级**：P0
- **依赖**：任务2（抽象文件存储操作）
- **描述**：
  - 添加异常处理，替代简单的Console.WriteLine
  - 添加日志记录功能
  - 确保错误信息被正确记录和处理
- **验收标准**：AC-3
- **测试要求**：
  - `programmatic` TR-3.1：验证异常是否被正确捕获和处理
  - `programmatic` TR-3.2：验证错误信息是否被正确记录
- **备注**：可以使用.NET内置的日志记录功能，暂时不引入第三方日志库

## [x] 任务4：优化代码规范
- **优先级**：P1
- **依赖**：无
- **描述**：
  - 创建EditorConfig配置文件，定义代码风格规则
  - 在.csproj文件中配置静态代码分析规则
  - 运行代码格式化工具，确保代码风格一致
- **验收标准**：AC-4
- **测试要求**：
  - `programmatic` TR-4.1：验证EditorConfig文件是否存在且配置正确
  - `programmatic` TR-4.2：验证静态代码分析规则是否配置正确
- **备注**：使用dotnet format工具进行代码格式化

## [x] 任务5：改进界面布局
- **优先级**：P1
- **依赖**：无
- **描述**：
  - 移除视图中硬编码的尺寸和布局
  - 实现响应式布局，适应不同屏幕尺寸
  - 优化控件的排列和间距
- **验收标准**：AC-5
- **测试要求**：
  - `human-judgment` TR-5.1：检查视图是否不再使用硬编码的尺寸
  - `human-judgment` TR-5.2：检查界面是否适应不同屏幕尺寸
- **备注**：重点关注TemplateEditorView和DataEntryView的布局

## [x] 任务6：统一样式管理
- **优先级**：P1
- **依赖**：任务5（改进界面布局）
- **描述**：
  - 创建独立的ResourceDictionary文件，如Styles.xaml、Brushes.xaml等
  - 将分散在各个视图中的样式移动到统一的ResourceDictionary中
  - 在App.xaml中合并ResourceDictionary
- **验收标准**：AC-6
- **测试要求**：
  - `human-judgment` TR-6.1：检查是否创建了独立的ResourceDictionary文件
  - `human-judgment` TR-6.2：检查样式是否集中管理
- **备注**：可以创建主题系统，支持主题切换

## [x] 任务7：增加测试覆盖率
- **优先级**：P1
- **依赖**：任务2（抽象文件存储操作）
- **描述**：
  - 为ViewModel添加单元测试
  - 为服务层添加更全面的单元测试
  - 确保测试覆盖率达到合理水平
- **验收标准**：AC-7
- **测试要求**：
  - `programmatic` TR-7.1：验证ViewModel的单元测试是否存在且通过
  - `programmatic` TR-7.2：验证服务层的单元测试是否存在且通过
- **备注**：使用MSTest框架进行单元测试，使用模拟对象测试ViewModel和服务