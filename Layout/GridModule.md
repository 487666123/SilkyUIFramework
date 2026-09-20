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
- `Auto` / `Pixels` / `Percent` / `Fraction` / `MinMax` 轨道尺寸
- 间距（`Gap`）
- 容器级默认子项对齐
- 子项自身对齐
- Fit 子项默认拉伸到 Grid 区域，非 Fit 子项使用声明尺寸

## 属性映射（SilkyUI -> CSS 概念）

| SilkyUI                       | CSS 概念                  | 说明                                  |
| ----------------------------- | ------------------------- | ------------------------------------- |
| `LayoutType.Grid`             | `display: grid`           | 启用 Grid 布局                        |
| `TemplateRows`                | `grid-template-rows`      | 显式行轨道                            |
| `TemplateColumns`             | `grid-template-columns`   | 显式列轨道                            |
| `AutoRows`                   | `grid-auto-rows`          | 隐式行轨道循环模板                    |
| `AutoColumns`                | `grid-auto-columns`       | 隐式列轨道循环模板                    |
| `GridDirection`               | `grid-auto-flow`          | 自动放置方向，仅支持 `Row` / `Column` |
| `Gap`                         | `gap`                     | 使用 `Size`，可分别控制列/行间距      |
| `RowSpan`                     | `grid-row` / `grid-row-*` | 子项所在行与跨行数量                  |
| `ColumnSpan`                  | `grid-column`             | 子项所在列与跨列数量                  |
| `GridItemsHorizontalAlignment`| `justify-items`           | 容器对子项的默认水平对齐              |
| `GridItemsVerticalAlignment`  | `align-items`             | 容器对子项的默认垂直对齐              |
| `GridContentHorizontalAlignment` | `justify-content`      | 列轨道集合的整体水平分布              |
| `GridContentVerticalAlignment` | `align-content`        | 行轨道集合的整体垂直分布              |
| `GridHorizontalAlignment`     | `justify-self`            | 子项自身水平对齐                      |
| `GridVerticalAlignment`       | `align-self`              | 子项自身垂直对齐                      |
| `GridTrack.Fr(n)`             | `nfr`                     | 按剩余空间比例分配                    |
| `GridTrack.Auto`              | `auto`                    | 根据内容外部尺寸撑开                  |
| `GridTrack.MinMax(min, max)` | `minmax(min, max)`        | 限制轨道最小和最大尺寸                |

当前未实现：

- 命名线（named lines）
- 命名区域（grid-template-areas）
- `dense` 自动放置
- `fit-content()`
- `repeat(auto-fill)` / `repeat(auto-fit)`
- `subgrid`
- baseline 对齐

## 容器属性（UIElementGroup）

| 属性                           | 类型                       | 默认值    | 说明                   |
| ------------------------------ | -------------------------- | --------- | ---------------------- |
| `LayoutType`                   | `LayoutType`               | `Flexbox` | 设置为 `Grid` 启用布局 |
| `TemplateRows`                 | `IReadOnlyList<GridTrack>` | 空        | 显式行模板             |
| `TemplateColumns`              | `IReadOnlyList<GridTrack>` | 空        | 显式列模板             |
| `AutoRows`                     | `IReadOnlyList<GridTrack>` | 空        | 隐式行循环模板         |
| `AutoColumns`                  | `IReadOnlyList<GridTrack>` | 空        | 隐式列循环模板         |
| `GridDirection`                | `GridDirection`           | `Row`     | 自动放置方向           |
| `Gap`                          | `Size`                     | `0`       | 列间距与行间距         |
| `GridItemsHorizontalAlignment` | `GridItemAlignment`        | `Stretch` | 子项默认水平对齐       |
| `GridItemsVerticalAlignment`   | `GridItemAlignment`        | `Stretch` | 子项默认垂直对齐       |
| `GridContentHorizontalAlignment` | `GridContentAlignment`  | `Start`   | 列轨道整体水平分布     |
| `GridContentVerticalAlignment` | `GridContentAlignment`   | `Start`   | 行轨道整体垂直分布     |

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

group.SetAutoColumns([
    GridTrack.Pixels(120f),
    GridTrack.Fr(1f),
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

当前实现使用 0-based 索引。第一个行/列轨道索引是 `0`。`GridSpan` 会原样保存 `Start` 和 `Size`；初始化 `GridArea` 时，负数起点按 `0` 处理，小于 `1` 的跨度按 `1` 处理。放置边界判断、冲突检测及最终占用区域统一使用归一化后的起点和跨度。

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

### `GridTrack.MinMax(min, max)`

使用 `GridTrackSize` 同时定义轨道的最小和最大尺寸。最小值支持 `Pixels`、`Percent` 和 `Auto`，最大值还支持 `Fr`。

```csharp
GridTrack.MinMax(
    GridTrackSize.Pixels(120f),
    GridTrackSize.Fr(1f)
)
```

该轨道至少为 `120px`，有剩余空间时按 `1fr` 扩展。`MinMax` 可同时用于 `TemplateRows` / `TemplateColumns` 和 `AutoRows` / `AutoColumns`。如果最小值使用 `Fr`，或尺寸值为 NaN/无穷数，创建轨道时会抛出配置异常。

单轨道子项所在轨道的最小端点为 `Auto` 时，子项明确设置的 `MinWidth` / `MinHeight`（换算成外部尺寸）会建立轨道下限。若该下限超过轨道声明的最大值，有效最大值随下限提高。例如 `MinMax(Auto, 100px)` 中的子项设置 `MinWidth = 200px`，列宽至少为 `200px`。普通内容测量值仍受有效上限限制。跨轨道子项的明确最小尺寸按下述跨轨道分配规则建立 Auto 下限，必要时提高有效上限。

### `GridTrack.Auto`

自动尺寸轨道，根据子项当前 `OuterBounds` 尺寸撑开。

```csharp
GridTrack.Auto
```

单轨道子项会直接撑开所在 `Auto` 轨道。跨轨道子项先扣除已有轨道尺寸与内部 Gap，再分配不足的尺寸，而不是把各轨道补到相同宽度。例如两列已有 `100 / 0px`，跨列项需要 `200px`，两列各增加 `50px`，得到 `150 / 50px`。

不跨 `Fr` 的子项按跨度从小到大分组；同组独立计算增量，再逐轨道取最大值，避免子项遍历顺序影响结果。可增长轨道达到上限后退出分配，其余轨道继续接收剩余增量。跨 `Fr` 的子项最后统一处理，只向其中含 Auto 下限的 flexible 轨道分配，并按 fr 比例分配增量；fr 总和不足 1 时，对应比例之外的部分均分。纯 `Fr` 的零下限以及 Fit 容器中 `Fr` / 百分比轨道的处理本次未改变。

这套分配仍使用框架已有的普通测量值和明确 min/max 约束，并未新增完整 CSS 的 min-content / max-content 测量模式。

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

当现有显式轨道不够时，会创建隐式轨道；如果配置了 `AutoRows` / `AutoColumns`，则按对应数组循环取用，否则使用 `GridTrack.Auto`。完全自动子项开始放置前，换行边界会计入前面已放置子项扩出的隐式轨道，以及待放置子项所需的跨度；Row 方向计算列边界，Column 方向计算行边界。

内部实现使用已放置矩形列表判断冲突，不使用完整二维 cell 占用表。行列起点都明确的子项直接加入列表，不检查彼此冲突；只明确一轴和完全自动的子项会查找无冲突位置。

## 对齐规则

### 轨道集合整体对齐

Grid 可以控制整组列轨道或行轨道在容器中的分布：

```csharp
grid.GridContentHorizontalAlignment = GridContentAlignment.Center;
grid.GridContentVerticalAlignment = GridContentAlignment.SpaceEvenly;
```

支持 `Start`、`Center`、`End`、`SpaceBetween` 和 `SpaceEvenly`。水平属性固定控制列轨道，垂直属性固定控制行轨道；`GridDirection` 不改变这两个属性的含义。`SpaceBetween` / `SpaceEvenly` 会用计算后的间距替换原始 `Gap`。Fit 轴没有剩余空间，按 `Start` 和原始 `Gap` 处理。

### 子项区域内对齐

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
3. `FitWidth = true` 或按内容计算高度的 `FitHeight = true` 在该轴按 auto 参与 `Stretch`，填满 Grid 区域并受 min/max 约束。非 Fit 轴使用声明的 `Width` / `Height`（百分比按 Grid 区域解析），不会因为 `Stretch` 而覆盖声明尺寸；`Stretch` 下未填满的显式尺寸子项靠区域起点放置。
4. `Start` / `Center` / `End` 会调用 `UpdateWidth` / `UpdateHeight` 更新可用尺寸；Fit 子项仍以内容测量尺寸为基础应用新约束，非 Fit 子项解析声明尺寸，再按对应方式定位。比例高度继续遵循原有比例计算规则。

## 尺寸与拉伸规则

Grid 仍然运行在 SilkyUI 现有布局管线中：

```text
Measure -> ResizeChildrenWidth -> RecalculateHeight -> ResizeChildrenHeight -> UpdateChildrenLayoutPosition
```

`Measure` 内部会先执行 `MeasureChildren`，再执行 `LayoutModule.Measure`。`RecalculateHeight` 内部会先执行子项高度重算，再执行 `LayoutModule.RecalculateHeight`。

行为要点：

- Grid 先放置子项所在区域，再让子项按父容器可用空间进行一次普通测量。
- Grid 根据子项本轮普通测量后的 `OuterBounds` 解析 `Auto` 轨道。
- `Pixels` / `Percent` / `Auto` 先确定基础尺寸和内容约束。
- `MinMax` 为轨道建立最小和最大尺寸边界。
- `Fraction` 根据剩余空间分配，并遵守轨道最小最大边界。
- 子项对齐默认是 Stretch，但只有对应轴为 Fit 才拉伸；明确设置尺寸的非 Fit 子项保留声明尺寸。
- 子项非 `Stretch` 对齐时，会调用 `UpdateWidth` / `UpdateHeight`。内容自适应子项会用刷新后的约束钳制已有尺寸，非 Fit 子项会更新到可用尺寸；宽度受限后的文本换行和高度回算由后续布局阶段完成。
- 子项尺寸写入时会经过 `MinWidth` / `MaxWidth` / `MinHeight` / `MaxHeight` 约束。
- 无流内子项时仍计算显式行列模板；例如两列各 `100px`、列间距 `10px` 的空 Grid，自适应内容宽度为 `210px`。没有子项也没有显式模板时，不凭空创建隐式轨道；移除最后一个子项后会重新计算。
- 此处的 Fit 子项语义仅调整 Grid 分配行为；容器的 Fit 尺寸测量、Flexbox 的 Grow/Shrink 和宽高比规则保持原样。

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
    FitWidth = true,
    FitHeight = true,
    ColumnSpan = GridSpan.At(0),
    RowSpan = GridSpan.At(0),
};

var content = new UIView
{
    FitWidth = true,
    FitHeight = true,
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
        FitWidth = true,
        Height = new Dimension(64f),
    });
}
```

所有子项默认 `RowSpan = GridSpan.Auto`、`ColumnSpan = GridSpan.Auto`，所以会自动按行放置。超过显式行数量后会创建隐式行；本例未配置 `AutoRows`，因此使用隐式 `Auto` 行。

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
    FitWidth = true,
    FitHeight = true,
    RowSpan = GridSpan.At(0),
    ColumnSpan = GridSpan.At(0, 3),
});

grid.AddChild(new UIView { FitWidth = true, FitHeight = true, RowSpan = GridSpan.At(1), ColumnSpan = GridSpan.At(0) });
grid.AddChild(new UIView { FitWidth = true, FitHeight = true, RowSpan = GridSpan.At(1), ColumnSpan = GridSpan.At(1) });
grid.AddChild(new UIView { FitWidth = true, FitHeight = true, RowSpan = GridSpan.At(1), ColumnSpan = GridSpan.At(2) });
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
    GridVerticalAlignment = GridItemAlignment.Stretch, // FitHeight=false，仍使用声明的 40px 高度。
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

Grid 默认对 Fit 子项执行 Stretch。若要使用明确尺寸，设 `FitWidth = false` / `FitHeight = false` 并声明 `Width` / `Height`。若要保持内容测量尺寸，可以调整对齐方式：

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
