# Grid 布局模块

本文档基于当前实现编写，和源码保持一致，主要参考以下文件：

- `Layout/GridModule.cs`
- `Layout/Grid/GridContext.cs`
- `Layout/Grid/GridItem.cs`
- `Layout/Grid/GridArea.cs`
- `Layout/Grid/GridTrackOutput.cs`
- `Layout/Grid/FlowRect.cs`
- `Layout/Grid/GridPlacement.cs`
- `Layout/Grid/GridTrackSizing.cs`
- `Layout/Grid/GridLayoutHelper.cs`
- `Layout/GridTrack.cs`
- `Layout/GridSpan.cs`
- `Elements/UIElementGroup.Flexbox.cs`
- `Elements/UIView.FlexItem.cs`

注意：SilkyUI 的 Grid 是“工程实现版”，不是完整 CSS Grid 规范的 1:1 复刻。

## 概述

Grid 布局通过 `UIElementGroup.LayoutType = LayoutType.Grid` 启用。

该模块负责：

- 显式行列模板
- 隐式行列扩展
- 自动放置子项
- 指定行列位置
- 跨行跨列
- `Auto` / `Pixels` / `Percent` / `Fraction` 轨道尺寸
- 间距（`Gap`）
- 容器级默认子项对齐
- 子项自身对齐
- 子项默认拉伸到 Grid 区域

## 属性映射（SilkyUI -> CSS 概念）

| SilkyUI                       | CSS 概念                  | 说明                                  |
| ----------------------------- | ------------------------- | ------------------------------------- |
| `LayoutType.Grid`             | `display: grid`           | 启用 Grid 布局                        |
| `TemplateRows`                | `grid-template-rows`      | 显式行轨道                            |
| `TemplateColumns`             | `grid-template-columns`   | 显式列轨道                            |
| `GridDirection`               | `grid-auto-flow`          | 自动放置方向，仅支持 `Row` / `Column` |
| `Gap`                         | `gap`                     | 使用 `Size`，可分别控制列/行间距      |
| `RowSpan`                     | `grid-row` / `grid-row-*` | 子项所在行与跨行数量                  |
| `ColumnSpan`                  | `grid-column`             | 子项所在列与跨列数量                  |
| `GridItemsHorizontalAlignment`| `justify-items`           | 容器对子项的默认水平对齐              |
| `GridItemsVerticalAlignment`  | `align-items`             | 容器对子项的默认垂直对齐              |
| `GridHorizontalAlignment`     | `justify-self`            | 子项自身水平对齐                      |
| `GridVerticalAlignment`       | `align-self`              | 子项自身垂直对齐                      |
| `GridTrack.Fr(n)`             | `nfr`                     | 按剩余空间比例分配                    |
| `GridTrack.Auto`              | `auto`                    | 根据内容外部尺寸撑开                  |

当前未实现：

- 命名线（named lines）
- 命名区域（grid-template-areas）
- `dense` 自动放置
- `minmax()`
- `fit-content()`
- `repeat(auto-fill)` / `repeat(auto-fit)`
- `subgrid`
- `justify-content` / `align-content`
- baseline 对齐

## 容器属性（UIElementGroup）

| 属性                           | 类型                       | 默认值    | 说明                   |
| ------------------------------ | -------------------------- | --------- | ---------------------- |
| `LayoutType`                   | `LayoutType`               | `Flexbox` | 设置为 `Grid` 启用布局 |
| `TemplateRows`                 | `IReadOnlyList<GridTrack>` | 空        | 显式行模板             |
| `TemplateColumns`              | `IReadOnlyList<GridTrack>` | 空        | 显式列模板             |
| `GridDirection`                | `GridDirection`           | `Row`     | 自动放置方向           |
| `Gap`                          | `Size`                     | `0`       | 列间距与行间距         |
| `GridItemsHorizontalAlignment` | `GridItemAlignment`        | `Stretch` | 子项默认水平对齐       |
| `GridItemsVerticalAlignment`   | `GridItemAlignment`        | `Stretch` | 子项默认垂直对齐       |

设置模板时使用：

```csharp
group.SetTemplateColumns([
    GridTrack.Pixels(160f),
    GridTrack.Fr(1f),
    GridTrack.Fr(2f),
]);

group.SetTemplateRows([
    GridTrack.Pixels(40f),
    GridTrack.Auto,
]);
```

也可以使用 `Repeat`：

```csharp
group.SetTemplateColumns(GridTrack.Repeat(3, TemplateType.Fraction, 1f));
```

## 子项属性（UIView）

| 属性                      | 类型                | 默认值                      | 说明                         |
| ------------------------- | ------------------- | --------------------------- | ---------------------------- |
| `RowSpan`                 | `GridSpan`          | `GridSpan.Auto`             | 行位置；默认参与自动放置     |
| `ColumnSpan`              | `GridSpan`          | `GridSpan.Auto`             | 列位置；默认参与自动放置     |
| `GridHorizontalAlignment` | `GridItemAlignment` | `GridItemAlignment.Inherit` | 子项自身水平对齐             |
| `GridVerticalAlignment`   | `GridItemAlignment` | `GridItemAlignment.Inherit` | 子项自身垂直对齐             |

`GridSpan` 的语义：

```csharp
GridSpan.Auto          // 自动寻找位置，跨度为 1
GridSpan.At(0)         // 从第 0 条轨道开始，占 1 格
GridSpan.At(1, 2)      // 从第 1 条轨道开始，占 2 格
new GridSpan(null, 3)  // 自动寻找位置，占 3 格
```

当前实现使用 0-based 索引。第一个行/列轨道索引是 `0`。`GridSpan` 会原样保存 `Start` 和 `Size`；进入 `GridArea` 后，负数起点按 `0` 处理，小于 `1` 的跨度按 `1` 处理。

## 轨道类型

### `GridTrack.Pixels(value)`

固定像素轨道。

```csharp
GridTrack.Pixels(120f)
```

### `GridTrack.Percent(value)`

百分比轨道。`value` 原样保存，不限制到 `0..1`。

```csharp
GridTrack.Percent(0.5f)
```

列百分比基于容器 `InnerBounds.Width`，行百分比基于容器 `InnerBounds.Height`。非 Fit 轴上按 `availableSize * value` 计算，并把负结果钳制到 `0`。

如果对应轴是 `FitWidth` 或 `FitHeight`，百分比轨道先按 `0` 处理，再允许被覆盖它的子项内容撑开。

### `GridTrack.Fr(value)`

弹性比例轨道，分配剩余空间。

```csharp
GridTrack.Fr(1f)
GridTrack.Fr(2f)
```

在固定宽度容器中，`1fr 2fr` 会把剩余空间按 1:2 分配。

如果对应轴是 `FitWidth` 或 `FitHeight`，`fr` 不分配剩余空间，只保留被内容撑开的尺寸。

### `GridTrack.Auto`

自动尺寸轨道，根据子项当前 `OuterBounds` 尺寸撑开。

```csharp
GridTrack.Auto
```

单轨道子项会直接撑开所在 `Auto` 轨道。跨轨道子项会把不足尺寸平均分摊到覆盖的可内容撑开的轨道上。

## 自动放置规则

子项放置优先级：

1. `RowSpan.Start` 和 `ColumnSpan.Start` 都有值：直接放到指定区域。
2. 只有 `RowSpan.Start` 有值：固定行，自动寻找可用列。
3. 只有 `ColumnSpan.Start` 有值：固定列，自动寻找可用行。
4. 都没有值：从自动游标开始寻找第一个可用区域。

当前实现使用 sparse 自动放置：自动游标只向前推进，不会回填前面因为跨行/跨列产生的空洞。

`GridDirection.Row`：

- 优先向右查找。
- 当前行放不下时进入下一行。

`GridDirection.Column`：

- 优先向下查找。
- 当前列放不下时进入下一列。

当现有显式轨道不够时，会创建隐式 `Auto` 轨道。

内部实现使用已放置矩形列表判断冲突，不使用完整二维 cell 占用表。行列起点都明确的子项直接加入列表，不检查彼此冲突；只明确一轴和完全自动的子项会查找无冲突位置。

## 对齐规则

Grid 子项在各自 Grid 区域内支持单轴对齐：

- `Inherit`
- `Start`
- `Center`
- `End`
- `Stretch`

容器属性：

```csharp
grid.GridItemsHorizontalAlignment = GridItemAlignment.Stretch;
grid.GridItemsVerticalAlignment = GridItemAlignment.Center;
```

子项属性：

```csharp
item.GridHorizontalAlignment = GridItemAlignment.Center;
item.GridVerticalAlignment = GridItemAlignment.End;
```

解析规则：

1. 子项为 `Inherit` 时，使用父容器对应轴的默认对齐。
2. 如果父容器对应轴也是 `Inherit`，最终退化为 `Stretch`。
3. `Stretch` 会把子项外部尺寸设置为 Grid 区域尺寸。
4. `Start` / `Center` / `End` 会调用 `UpdateWidth` / `UpdateHeight` 更新可用尺寸；`FitWidth` / `FitHeight` 子项保留自身测量尺寸，非 Fit 子项会按可用尺寸更新。

## 尺寸与拉伸规则

Grid 仍然运行在 SilkyUI 现有布局管线中：

```text
Measure -> ResizeChildrenWidth -> RecalculateHeight -> ResizeChildrenHeight -> UpdateChildrenLayoutPosition
```

`Measure` 内部会先执行 `MeasureChildren`，再执行 `LayoutModule.Measure`。`RecalculateHeight` 内部会先执行子项高度重算，再执行 `LayoutModule.RecalculateHeight`。

行为要点：

- Grid 先放置子项所在区域，再让子项按父容器可用空间进行一次普通测量。
- Grid 根据子项本轮普通测量后的 `OuterBounds` 解析 `Auto` 轨道。
- `Pixels` / `Percent` / `Auto` 先确定基础尺寸。
- `Fraction` 根据剩余空间分配。
- 子项默认拉伸到它所在的 Grid 区域。
- 子项非 `Stretch` 对齐时，会调用 `UpdateWidth` / `UpdateHeight`。Fit 子项只更新约束，非 Fit 子项会更新到可用尺寸。
- 子项尺寸写入时会经过 `MinWidth` / `MaxWidth` / `MinHeight` / `MaxHeight` 约束。

## 使用示例

### 示例 1：两列布局

```csharp
var root = new UIElementGroup
{
    LayoutType = LayoutType.Grid,
    Width = new Dimension(600f),
    Height = new Dimension(300f),
    FitWidth = false,
    FitHeight = false,
    Gap = new Size(8f),
};

root.SetTemplateColumns([
    GridTrack.Pixels(160f),
    GridTrack.Fr(1f),
]);

root.SetTemplateRows([
    GridTrack.Fr(1f),
]);

var sidebar = new UIView
{
    ColumnSpan = GridSpan.At(0),
    RowSpan = GridSpan.At(0),
};

var content = new UIView
{
    ColumnSpan = GridSpan.At(1),
    RowSpan = GridSpan.At(0),
};

root.AddChild(sidebar);
root.AddChild(content);
```

### 示例 2：自动卡片网格

```csharp
var grid = new UIElementGroup
{
    LayoutType = LayoutType.Grid,
    Width = new Dimension(480f),
    FitWidth = false,
    FitHeight = true,
    Gap = new Size(8f),
};

grid.SetTemplateColumns(GridTrack.Repeat(3, TemplateType.Fraction, 1f));
grid.SetTemplateRows([GridTrack.Auto]);

for (var i = 0; i < 10; i++)
{
    grid.AddChild(new UIView
    {
        Height = new Dimension(64f),
    });
}
```

所有子项默认 `RowSpan = GridSpan.Auto`、`ColumnSpan = GridSpan.Auto`，所以会自动按行放置。超过显式行数量后会创建隐式 `Auto` 行。

### 示例 3：跨列标题

```csharp
var grid = new UIElementGroup
{
    LayoutType = LayoutType.Grid,
    Width = new Dimension(640f),
    FitWidth = false,
    FitHeight = true,
    Gap = new Size(6f),
};

grid.SetTemplateColumns([
    GridTrack.Fr(1f),
    GridTrack.Fr(1f),
    GridTrack.Fr(1f),
]);

grid.SetTemplateRows([
    GridTrack.Pixels(48f),
    GridTrack.Auto,
]);

grid.AddChild(new UIView
{
    RowSpan = GridSpan.At(0),
    ColumnSpan = GridSpan.At(0, 3),
});

grid.AddChild(new UIView { RowSpan = GridSpan.At(1), ColumnSpan = GridSpan.At(0) });
grid.AddChild(new UIView { RowSpan = GridSpan.At(1), ColumnSpan = GridSpan.At(1) });
grid.AddChild(new UIView { RowSpan = GridSpan.At(1), ColumnSpan = GridSpan.At(2) });
```

### 示例 4：子项对齐

```csharp
var grid = new UIElementGroup
{
    LayoutType = LayoutType.Grid,
    Width = new Dimension(360f),
    Height = new Dimension(160f),
    FitWidth = false,
    FitHeight = false,
    GridItemsHorizontalAlignment = GridItemAlignment.Center,
    GridItemsVerticalAlignment = GridItemAlignment.Center,
};

grid.SetTemplateColumns(GridTrack.Repeat(3, TemplateType.Fraction, 1f));
grid.SetTemplateRows([GridTrack.Fr(1f)]);

grid.AddChild(new UIView
{
    Width = new Dimension(40f),
    Height = new Dimension(40f),
});

grid.AddChild(new UIView
{
    Width = new Dimension(40f),
    Height = new Dimension(40f),
    GridHorizontalAlignment = GridItemAlignment.End,
    GridVerticalAlignment = GridItemAlignment.Stretch,
});
```

## 常见问题

### 1. 子项全部重叠在一起

检查是否显式把多个子项设置到了相同的 `RowSpan` / `ColumnSpan`。

默认情况下，子项使用 `GridSpan.Auto`，会自动放置，不会全部挤在 `(0, 0)`。

### 2. `fr` 看起来没有生效

检查对应轴是否是固定尺寸：

- 列方向需要 `FitWidth = false` 且容器有明确宽度。
- 行方向需要 `FitHeight = false` 且容器有明确高度。

如果容器对应轴是 Fit，`fr` 没有可分配的剩余空间，只保留被内容撑开的尺寸。

### 3. 百分比轨道尺寸不符合预期

百分比依赖容器对应轴的 `InnerBounds`：

- 列百分比依赖容器宽度。
- 行百分比依赖容器高度。

如果容器对应轴是 Fit，百分比轨道先为 `0`，之后可被内容撑开。

### 4. 子项尺寸被拉伸

Grid 默认把子项拉伸到 Grid 区域。可以通过两种方式调整：

- 设置 `GridItemsHorizontalAlignment` / `GridItemsVerticalAlignment` 改变容器默认对齐。
- 设置子项 `GridHorizontalAlignment` / `GridVerticalAlignment` 覆盖单个子项。

也可以通过子项的 `MinWidth` / `MaxWidth` / `MinHeight` / `MaxHeight` 限制最终尺寸。

## 快速排查清单

1. 父容器 `LayoutType` 是否是 `Grid`。
2. 是否设置了合理的 `TemplateColumns` / `TemplateRows`。
3. `FitWidth` / `FitHeight` 是否与 `Percent` / `Fr` 的预期一致。
4. 子项 `RowSpan` / `ColumnSpan` 是否重叠。
5. `Gap.Width` / `Gap.Height` 是否符合列间距/行间距预期。
6. 子项对齐是否为 `Stretch`，导致尺寸被填满。
7. 子项是否被最小/最大尺寸约束卡住。
