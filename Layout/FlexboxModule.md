# Flexbox 布局模块

Flexbox（弹性盒子布局）是一种一维布局模型，用于在容器内对子元素进行排列、对齐和分配空间。SilkyUI 实现了 CSS Flexbox 规范的核心功能，便于创建响应式、灵活的 UI 布局。

## 属性对照表

本模块基于 CSS Flexbox 规范设计，下表列出了 SilkyUI 属性名与 CSS 原属性名的对应关系：

| SilkyUI 属性名          | CSS 属性名        | 说明                       |
| ----------------------- | ----------------- | -------------------------- |
| `FlexDirection`         | `flex-direction`  | 主轴方向                   |
| `FlexWrap`              | `flex-wrap`       | 子元素换行                 |
| `MainAlignment`         | `justify-content` | 主轴对齐方式               |
| `CrossAlignment`        | `align-items`     | 交叉轴单行对齐方式         |
| `CrossContentAlignment` | `align-content`   | 交叉轴多行对齐方式         |
| `FlexGrow`              | `flex-grow`       | 弹性增长系数               |
| `FlexShrink`            | `flex-shrink`     | 弹性收缩系数               |
| `FlexBasis`             | `flex-basis`      | 弹性基准尺寸（未实现）     |
| `FlexAlignSelf`         | `align-self`      | 项目单独对齐方式（未实现） |

### `Flexbox` 容器属性

`UIElementGroup` 默认已启用 Flexbox 布局（`LayoutType` 默认值为 `LayoutType.Flexbox`）。

| 属性                    | 类型                    | 默认值                          | CSS 对应          | 作用               |
| ----------------------- | ----------------------- | ------------------------------- | ----------------- | ------------------ |
| `FlexDirection`         | `FlexDirection`         | `FlexDirection.Row`             | `flex-direction`  | 主轴方向           |
| `FlexWrap`              | `bool`                  | `false`                         | `flex-wrap`       | 子元素换行         |
| `MainAlignment`         | `MainAlignment`         | `MainAlignment.Start`           | `justify-content` | 主轴对齐方式       |
| `CrossAlignment`        | `CrossAlignment`        | `CrossAlignment.Start`          | `align-items`     | 交叉轴单行对齐方式 |
| `CrossContentAlignment` | `CrossContentAlignment` | `CrossContentAlignment.Stretch` | `align-content`   | 交叉轴多行对齐方式 |
| `Gap`                   | `Size`                  | `0`                             | `gap`             | 行和列之间的间距   |
| `FitWidth`              | `bool`                  | `true`                          | 无 CSS 对应       | 宽度是否适应内容（`true`）或固定（`false`） |
| `FitHeight`             | `bool`                  | `true`                          | 无 CSS 对应       | 高度是否适应内容（`true`）或固定（`false`） |

**注意**：
- `FlexWrap` 为 `true` 时对应 CSS `flex-wrap: wrap`，为 `false` 时对应 `flex-wrap: nowrap`（默认）
- CSS 中的 `flex-wrap: wrap-reverse` 当前未实现
- 当 `FlexWrap` 为 `true` 时，容器需要有固定宽度（`FitWidth = false`）或高度（`FitHeight = false`）才能换行
- `Gap` 属性的 `Size` 类型支持从 `float` 隐式转换，例如 `Gap = 10` 等同于 `Gap = new Size(10)`

### `Flexbox Item` 属性（子元素）

以下属性适用于 Flexbox 容器内的子元素（`UIView`）。

#### 空间分配算法

当子元素的总尺寸与父容器可用空间不匹配时，`FlexGrow` 和 `FlexShrink` 决定如何调整子元素大小：

- **`FlexGrow`**：当子元素总尺寸 **小于** 父容器可用空间时，按各子元素的 `FlexGrow` 比例分配剩余空间。
  公式：`剩余空间 = 父容器尺寸 - 子元素总尺寸`，每个子元素增加 `剩余空间 * (子元素FlexGrow / 总FlexGrow)`。

- **`FlexShrink`**：当子元素总尺寸 **大于** 父容器可用空间时，按各子元素的 `FlexShrink` 比例收缩超出空间。
  公式：`超出空间 = 子元素总尺寸 - 父容器尺寸`，每个子元素减少 `超出空间 * (子元素FlexShrink / 总FlexShrink)`。

算法会尊重每个子元素的 `MinOuterWidth`/`MinOuterHeight` 和 `MaxOuterWidth`/`MaxOuterHeight` 约束，从最易调整的子元素开始分配。

| 属性            | 类型             | 默认值 | CSS 对应      | 状态     | 作用                                         |
| --------------- | ---------------- | ------ | ------------- | -------- | -------------------------------------------- |
| `FlexGrow`      | `float`          | `0`    | `flex-grow`   | ✅ 已实现 | 子元素总尺寸小于父元素时，按比例分配剩余空间 |
| `FlexShrink`    | `float`          | `0`    | `flex-shrink` | ✅ 已实现 | 子元素总尺寸大于父元素时，按比例收缩超出空间 |
| `FlexBasis`     | `Dimension`      | `auto` | `flex-basis`  | ❌ 未实现 | 定义项目在分配多余空间之前的初始大小         |
| `FlexAlignSelf` | `CrossAlignment` | `auto` | `align-self`  | ❌ 未实现 | 允许单个项目有与其他项目不一样的对齐方式     |

**注意**：
- `FlexGrow` 默认值 `0` 与 CSS `flex-grow` 默认值相同
- `FlexShrink` 默认值 `0` 与 CSS `flex-shrink` 默认值 `1` 不同（CSS 默认允许收缩）
- `FlexBasis` 和 `FlexAlignSelf` 属性当前未在 API 中实现（CSS 对应功能不可用）

## 属性效果

### `ENUM: FlexDirection` 主轴方向

| 枚举值   | CSS 值   | 描述                               |
| -------- | -------- | ---------------------------------- |
| `Row`    | `row`    | 主轴为水平方向，子元素从左到右排列 |
| `Column` | `column` | 主轴为垂直方向，子元素从上到下排列 |

**注意**：CSS 中的 `row-reverse` 和 `column-reverse` 方向当前未实现。

### `ENUM: MainAlignment` 主轴对齐方式

| 枚举值         | CSS 值          | 描述                                           |
| -------------- | --------------- | ---------------------------------------------- |
| `Start`        | `flex-start`    | 子元素向主轴起点对齐（左对齐或上对齐）         |
| `End`          | `flex-end`      | 子元素向主轴终点对齐（右对齐或下对齐）         |
| `Center`       | `center`        | 子元素居中对齐                                 |
| `SpaceEvenly`  | `space-evenly`  | 所有子元素均匀分布，首尾与容器边缘也有相同间距 |
| `SpaceBetween` | `space-between` | 首尾子元素贴边，其余子元素均匀分布             |

**注意**：CSS 中的 `space-around` 对齐方式当前未实现。

### `ENUM: CrossContentAlignment` 交叉轴对齐方式

| 枚举值         | CSS 值          | 描述                                       |
| -------------- | --------------- | ------------------------------------------ |
| `Start`        | `flex-start`    | 多行向交叉轴起点对齐（上对齐或左对齐）     |
| `Center`       | `center`        | 多行向交叉轴居中对齐                       |
| `End`          | `flex-end`      | 多行向交叉轴终点对齐（下对齐或右对齐）     |
| `SpaceBetween` | `space-between` | 首尾行贴边，其余行均匀分布                 |
| `SpaceEvenly`  | `space-evenly`  | 所有行均匀分布，首尾与容器边缘也有相同间距 |
| `Stretch`      | `stretch`       | 拉伸各行以填满交叉轴空间（默认值）         |

### `ENUM: CrossAlignment` 交叉轴对齐方式

| 枚举值    | CSS 值       | 描述                                                 |
| --------- | ------------ | ---------------------------------------------------- |
| `Start`   | `flex-start` | 子元素向交叉轴起点对齐（上对齐或左对齐）             |
| `Center`  | `center`     | 子元素向交叉轴居中对齐                               |
| `End`     | `flex-end`   | 子元素向交叉轴终点对齐（下对齐或右对齐）             |
| `Stretch` | `stretch`    | 拉伸子元素以填满交叉轴空间（需子元素未设置固定尺寸） |

**注意**：CSS 中的 `baseline` 对齐方式当前未实现。

## 使用示例

### C# 代码示例

```csharp
// 创建 Flexbox 容器
var container = new UIElementGroup
{
    FlexDirection = FlexDirection.Row,
    FlexWrap = true,
    MainAlignment = MainAlignment.SpaceBetween,
    CrossAlignment = CrossAlignment.Center,
    CrossContentAlignment = CrossContentAlignment.Stretch,
    Gap = 10 // 设置行列间距，Size 支持从 float 隐式转换
};

// 添加子元素
var item1 = new UIView { Width = new Dimension(50f), Height = new Dimension(50f), FlexGrow = 1 };
var item2 = new UIView { Width = new Dimension(80f), Height = new Dimension(80f), FlexShrink = 2 };
var item3 = new UIView { Width = new Dimension(60f), Height = new Dimension(60f) };

container.AddChild(item1);
container.AddChild(item2);
container.AddChild(item3);
```

### XML 布局示例

```xml
<ElementGroup FlexDirection="Row"
              FlexWrap="true"
              MainAlignment="SpaceBetween"
              CrossAlignment="Center"
              CrossContentAlignment="Stretch"
              Gap="10"
              FitWidth="false">
    <View Width="50px" Height="50px" FlexGrow="1" />
    <View Width="80px" Height="80px" FlexShrink="2" />
    <View Width="60px" Height="60px" />
</ElementGroup>
```

### 注意事项

- 当 `FlexWrap` 为 `true` 时，容器需要有固定尺寸才能换行：
  - `FlexDirection="Row"`：需要固定宽度（`FitWidth = false`）
  - `FlexDirection="Column"`：需要固定高度（`FitHeight = false`）
- `FlexGrow` 和 `FlexShrink` 仅在子元素总尺寸与容器可用空间不匹配时生效。
- 默认的 `CrossContentAlignment` 为 `Stretch`，会拉伸多行以填满交叉轴空间。
- 使用 `FlexGrow: 1` 可以让元素填充剩余空间，适合创建自适应布局。

### 常见布局模式

#### 1. 水平导航栏
```xml
<ElementGroup FlexDirection="Row"
              MainAlignment="SpaceBetween"
              CrossAlignment="Center"
              Height="40px" Border="1" BorderColor="#ccc">
    <TextView Text="Logo" Width="100px" />
    <ElementGroup FlexDirection="Row" Gap="20">
        <TextView Text="首页" />
        <TextView Text="关于" />
        <TextView Text="设置" />
    </ElementGroup>
    <TextView Text="用户" Width="80px" />
</ElementGroup>
```

#### 2. 垂直菜单（侧边栏）
```xml
<ElementGroup FlexDirection="Column"
              MainAlignment="Start"
              CrossAlignment="Stretch"
              Width="200px" Gap="5" Padding="10">
    <TextView Text="菜单项1" Padding="10" BackgroundColor="#f0f0f0" />
    <TextView Text="菜单项2" Padding="10" BackgroundColor="#f0f0f0" />
    <TextView Text="菜单项3" Padding="10" BackgroundColor="#f0f0f0" />
    <View FlexGrow="1" /> <!-- 弹性空白占位 -->
    <TextView Text="底部项" Padding="10" BackgroundColor="#e0e0e0" />
</ElementGroup>
```

#### 3. 自适应网格（卡片布局）
```xml
<ElementGroup FlexDirection="Row"
              FlexWrap="true"
              MainAlignment="SpaceEvenly"
              CrossContentAlignment="Start"
              Gap="10" FitWidth="false">
    <!-- 每个卡片固定宽度，自动换行 -->
    <View Width="150px" Height="200px" BackgroundColor="#f8f8f8" Border="1" />
    <View Width="150px" Height="200px" BackgroundColor="#f8f8f8" Border="1" />
    <View Width="150px" Height="200px" BackgroundColor="#f8f8f8" Border="1" />
    <View Width="150px" Height="200px" BackgroundColor="#f8f8f8" Border="1" />
</ElementGroup>
```
