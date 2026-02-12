# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在此代码库中工作时提供指导。

## 项目概述

SilkyUIFramework 是一个用于 Terraria tModLoader 的 UI 框架，它扩展了原版的 Terraria.UI 系统。它提供了声明式的 XML 布局、CSS Flexbox 布局系统、基于依赖注入的自动 UI 管理以及丰富的 UI 组件集。

## 分支结构

- **main**: 发布在 Steam 创意工坊的版本
- **preview**: 最新的开发版本，定期合并到 main 分支

## 构建命令

这是一个面向 .NET 8.0 的 tModLoader mod 项目。使用标准的 tModLoader mod 构建流程：

```bash
# 构建 mod
dotnet build

# 需要 SilkyUIAnalyzer 项目引用以实现 XML 布局功能
# 分析器项目必须被克隆并在 .csproj 中引用
```

### 依赖项

SilkyUIFramework.csproj 中的关键依赖项：
- `Microsoft.Extensions.DependencyInjection` (v9.0.9) - DI 容器
- `Solaestas.tModLoader.ModBuilder` (v1.6.1) - tModLoader 构建工具
- `SixLabors.ImageSharp` (v3.1.11) - 图像处理
- `SilkyUIAnalyzer` (本地项目) - XML 布局分析器

## 架构概述

### 依赖注入系统
- `ServiceProviderBuilder.cs` 构建 DI 容器
- `ServiceAttribute.cs` 标记类用于 DI 注册
- `SilkyUISystem.cs` 初始化 DI 并管理生命周期

### UI 注册系统
- `RegisterUIAttribute.cs` 为特定游戏层注册 UI
- `RegisterGlobalUIAttribute.cs` 注册全局 UI
- `SilkyUIRegistrar.cs` 收集和组织已注册的 UI

### XML 布局系统
- 在 XML 文件中声明式定义 UI (`UserInterfaces/*.xml`)
- 通过 `XmlElementMappingAttribute` 映射到 C# 类
- 支持类型解析的属性赋值（实现 `IParsable<TSelf>`）
- XML 属性支持基本类型（bool、int、float、double、string）、特殊类型（Color、Vector2/3/4）、实现 `IParsable<T>` 的类型（如 Dimension、Anchor）以及枚举类型（直接使用成员名称）

### 布局系统
- `FlexboxModule.cs` 实现 CSS Flexbox 规范
- `GridModule.cs` 提供网格布局（开发中）
- 属性：`FlexDirection`、`FlexWrap`、`MainAlignment`、`CrossAlignment` 等
- 通过脏标记系统自动更新布局

### UI 管理
- `SilkyUIManager.cs` - 协调更新和渲染的主 UI 管理器
- `SilkyUIRenderSystem.cs` - 处理渲染和层管理
- `BaseBody.cs` - 根 UI 容器（继承自 `UIElementGroup`）
- `UIElementGroup.cs` - 子元素容器
- `UIView.cs` - 所有 UI 元素的基类

## 创建 UI

1. **创建 UI 类**：继承 `BaseBody`
2. **添加注册特性**：使用 `[RegisterUI]` 或 `[RegisterGlobalUI]`
3. **创建 XML 布局**：添加包含 UI 定义的 `.xml` 文件
4. **映射 XML 元素**：在 C# 类上使用 `[XmlElementMapping]`

XML 模板示例：
```xml
<?xml version="1.0" encoding="utf-8" ?>
<Body Class="YourNamespace.YourUIClass">
    <ElementGroup Width="100px 0%" Height="100%">
        <TextView Text="Hello SilkyUI"/>
    </ElementGroup>
</Body>
```

## 关键目录

- `/Elements/` - UI 元素实现（SUIImage、SUIEditText 等）
- `/Layout/` - 布局系统（FlexboxModule、GridModule）
- `/Attributes/` - 用于注册和映射的自定义特性
- `/Components/` - 渲染组件（NinePatch、SnippetModule）
- `/UserInterfaces/` - XML 布局文件
- `/Extensions/` - 扩展方法
- `/Animation/` - 动画系统
- `/Graphics2D/` - 图形实用工具
- `/Helper/` - 辅助类
- `/Hooks/` - 游戏钩子

## 与 tModLoader 的集成

- `SilkyUIFramework.cs` - 主 mod 类
- `SilkyUISystem.cs` - 用于生命周期管理的 `ModSystem`
- `SilkyUIPlayer.cs` - 用于世界进入事件的 `ModPlayer`
- 通过 Terraria 的 `ModifyInterfaceLayers` 系统集成

## 特殊功能

- **Flexbox 布局**：实现 CSS Flexbox，支持自动尺寸调整和定位
- **脏标记**：属性变化自动触发布局更新
- **渲染目标池**：`RenderTargetPool.cs` 用于性能优化
- **九宫格渲染**：`NinePatch.cs` 用于可拉伸的 UI 元素
- **富文本**：`SnippetModule.cs` 用于使用 Terraria 的 TextSnippet 系统渲染文本

## 文档

- `README.md` - 项目概述和设置说明
- `Elements.md` - UI 元素文档
- `FlexboxModule.md` - Flexbox 布局文档
- `MigrationGuide.md` - 从原版 Terraria.UI 迁移的指南