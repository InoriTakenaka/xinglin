# xinglin模板编辑器优化 - 产品需求文档

## 概述

* **摘要**：对现有xinglin模板编辑器项目进行架构优化和代码质量提升，解决当前存在的MVVM原则违反、服务层耦合、错误处理简单等问题，提高项目的可维护性、可测试性和可扩展性。

* **目的**：通过优化现有代码架构和实现，解决技术债务，为后续功能开发打下坚实基础。

* **目标用户**：开发团队和最终用户。

## 目标

* 修复违反MVVM原则的代码，实现View和ViewModel的严格分离

* 抽象文件存储操作，提高服务层的可测试性和可扩展性

* 完善错误处理机制，添加日志记录功能

* 优化代码规范，提高代码质量

* 改进界面布局和样式管理，提升用户体验

* 增加测试覆盖率，确保功能正确性

## 非目标（范围外）

* 不添加新的业务功能

* 不修改现有核心业务逻辑

* 不进行大规模重构，仅针对已识别的问题进行优化

* 不改变项目的整体架构和技术栈

## 背景与上下文

项目是一个基于WPF MVVM架构的模板编辑器，使用.NET 8.0、CommunityToolkit.Mvvm和Microsoft.Extensions.DependencyInjection。当前项目存在一些架构问题和代码质量问题，需要进行优化以提高可维护性和可扩展性。

## 功能需求

* **FR-1**：修复ViewModel中直接创建View的代码，实现View和ViewModel的严格分离

* **FR-2**：抽象文件存储操作，为文件存储添加接口

* **FR-3**：完善错误处理机制，添加日志记录功能

* **FR-4**：优化代码规范，添加EditorConfig配置和静态代码分析规则

* **FR-5**：改进界面布局，移除硬编码尺寸，实现响应式布局

* **FR-6**：统一样式管理，创建独立的ResourceDictionary文件

* **FR-7**：增加测试覆盖率，为ViewModel和服务层添加单元测试

## 非功能需求

* **NFR-1**：代码质量：遵循C#编码规范，使用EditorConfig和静态代码分析确保代码一致性

* **NFR-2**：可测试性：服务层使用接口，便于单元测试和模拟对象

* **NFR-3**：可扩展性：抽象文件存储操作，支持不同的存储策略

* **NFR-4**：错误处理：完善的错误处理机制，包含日志记录

* **NFR-5**：用户体验：改进界面布局和样式，提升用户体验

## 约束

* **技术**：保持现有的技术栈，不引入新的依赖

* **业务**：不改变现有业务逻辑，仅优化代码实现

* **依赖**：使用现有的第三方库，如CommunityToolkit.Mvvm和Microsoft.Extensions.DependencyInjection

## 假设

* 项目将继续使用WPF MVVM架构

* 项目将继续使用.NET 8.0

* 项目将继续使用现有的第三方库

## 验收标准

### AC-1：修复MVVM原则违反

* **Given**：项目中存在ViewModel直接创建View的代码

* **When**：重构TemplateEditorViewModel，移除直接创建View的代码，使用事件或消息机制

* **Then**：ViewModel不再直接依赖View，符合MVVM原则

* **Verification**：`human-judgment`

### AC-2：抽象文件存储操作

* **Given**：TemplateService与文件系统耦合较紧

* **When**：创建IFileStorageService接口，实现文件存储的抽象

* **Then**：TemplateService不再直接依赖文件系统，可测试性和可扩展性提高

* **Verification**：`programmatic`

### AC-3：完善错误处理机制

* **Given**：当前错误处理仅使用Console.WriteLine

* **When**：添加异常处理和日志记录功能

* **Then**：错误信息被正确记录，便于排查问题

* **Verification**：`programmatic`

### AC-4：优化代码规范

* **Given**：项目缺少代码规范配置

* **When**：创建EditorConfig配置文件，配置静态代码分析规则

* **Then**：代码风格一致，符合C#编码规范

* **Verification**：`programmatic`

### AC-5：改进界面布局

* **Given**：部分视图使用了硬编码的尺寸和布局

* **When**：移除硬编码尺寸，实现响应式布局

* **Then**：界面布局更加灵活，适应不同屏幕尺寸

* **Verification**：`human-judgment`

### AC-6：统一样式管理

* **Given**：样式定义分散在各个视图中

* **When**：创建独立的ResourceDictionary文件，管理样式和资源

* **Then**：样式管理更加集中，便于维护

* **Verification**：`human-judgment`

### AC-7：增加测试覆盖率

* **Given**：当前测试覆盖率低

* **When**：为ViewModel和服务层添加单元测试

* **Then**：测试覆盖率提高，功能正确性得到保障

* **Verification**：`programmatic`

## 未解决的问题

* [ ] 是否需要引入第三方日志库（如Serilog）可以

* [ ] 是否需要使用更成熟的DI容器（如Autofac）不需要

* [ ] 是否需要添加更多高级UI功能（如拖拽调整大小、实时预览等）加入到计划内

