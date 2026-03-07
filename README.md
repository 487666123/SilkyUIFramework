<div align="center">
    <img src="icon_workshop.png" width="200"/>
    <h3>SilkyUI Framework</h3>
</div>

### 背景
**SilkyUI** 最初只是 **更好的体验** 和 **积分商店** 两个模组内部，为扩展 **Terraria.UI** 而写的一些改动。由于 UI 代码直接维护在模组里，导致两个模组之间的同步非常麻烦。随着更新需求逐渐增多，仅仅扩展 **Terraria.UI** 已经无法满足需要，因此把 UI 独立出来，形成可供多个模组复用的库，并命名为 **SilkyUI**。

### Terraria.UI 的局限
在使用 **Terraria.UI** 时会遇到很多问题：
- 没有统一的 UI 管理，需要手动维护
- 缺乏专业布局功能
- 布局只能通过硬编码位置完成
- 不支持自适应大小
- 布局更新必须手动调用 **Recalculate()**
- 只能使用 C# 代码创建 UI，不支持声明式布局

这些问题不仅增加冗余代码，还让开发和维护体验都很差。

### SilkyUI 的改进
**SilkyUI** 针对上述痛点提供了解决方案：
- 继承 **BaseBody** 并添加 **RegisterUI** 特性即可快速创建 UI
- 内建层级关系，自动避免鼠标穿透，并支持类似 **Windows 窗口** 的层级更新
- **UIView / UIElementGroup** 采用脏标记机制，属性变化会自动触发布局更新，无需显式调用 **Recalculate()**，还能局部更新
- 已实现 [Flexbox 布局标准](https://www.w3.org/TR/css-flexbox-1/)，计划支持 Grid
- 支持 **XML 布局**，可复用样式并生成元素映射

### 说明

- main 分支是已发布在 Steam 创意工坊中的版本
- preview 分支是最新版，会定期并入 main 分支，并在 Steam 创意工坊中更新
- preview 不一定有破坏性更新，如果出现会在通知

> 交流群：[971038831](https://qm.qq.com/q/mRF9AJDHWM)

### 相关文档

[SilkyUI 迁移指南](MigrationGuide.md)

[Flexbox 文档](FlexboxModule.md)

[CSS Flexible Box Layout Module Level 1](https://www.w3.org/TR/css-flexbox-1/)

### TODO

1. [ ] Grid 布局设计与实现

### XML 初始模板：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<!-- Class 填写对应类名 -->
<Body Class="SilkyUIFramework.UserInterfaces.MouseMenuUI">
    <!-- ElementGroup 是基本元素 -->
    <!-- Width 和 Height 的类型实现了 IParsable<TSelf>，演示的写法是支持的方式 -->
    <ElementGroup Width="100px 0%" Height="100%">
        <!-- 这是内置的文本元素，目前仅有简单支持，使用泰拉的 TextSnippet -->
        <TextView Text="Hello SilkyUI"/>
    </ElementGroup>
</Body>
```

### XML 属性转 C#对象 属性和字段赋值，支持的属性和字段类型:

1. 实现 IParsable<TSelf> 的任意类，会调用 Parse 方法进行解析。
2. float double char int string
3. Vector2 Vector3 Vector4 Color

### 如何使用？

**SilkyUI** 是以前置模组的方式在 **tModLoader** 中使用，所以需要将其设置为模组引用。

**XML** 布局功能使用了**增量生成器**，所以你还需要一同将生成器项目引入你的模组项目（无需模组引用）。

### 设为前置模组

在你项目中的 build.txt 文件内添加模组引用

```txt
modReferences = SilkyUIFramework
```

### 克隆必要项目

在你的 ModSources 文件夹下运行以下两个 git clone 命令

```sh
git clone https://github.com/487666123/SilkyUIFramework.git
git clone https://github.com/487666123/SilkyUIAnalyzer.git
```

### 引入这两个项目

1. 将两个项目添加入你的解决方案中（选中 .csproj 文件）\
![](./readme_add.png)

1. 这两个项目会出现在你的解决方案中\
![](./readme_sln.png)

1. 在你的项目的 .csporj 文件中添加这两个项目的引用\
```xml
<ItemGroup>
    <ProjectReference Include="..\SilkyUIAnalyzer\SilkyUIAnalyzer.csproj">
        <OutputItemType>Analyzer</OutputItemType>
        <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    </ProjectReference>
    <ProjectReference Include="..\SilkyUIFramework\SilkyUIFramework.csproj"></ProjectReference>
</ItemGroup>
```
