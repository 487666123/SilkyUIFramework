# Flexbox 布局模块

本文档基于当前实现编写，和源码保持一致，主要参考以下文件：

- `Layout/FlexboxModule.cs`
- `Layout/FlexboxModule.LayoutModule.cs`
- `Elements/UIElementGroup.Flexbox.cs`
- `Elements/UIElementGroup.Layout.cs`
- `Elements/UIView.FlexItem.cs`

注意：SilkyUI 的 Flexbox 是“工程实现版”，并非完整 CSS Flexbox 规范的 1:1 复刻。

## 概述

`UIElementGroup` 默认使用 Flexbox（`LayoutType = LayoutType.Flexbox`）。

该模块负责：

- 主轴方向（行/列）
- 是否换行
- 主轴对齐
- 交叉轴单行对齐
- 交叉轴多行对齐
- 子项增长/收缩（`FlexGrow` / `FlexShrink`）
- 间距（`Gap`）

## 属性映射（SilkyUI -> CSS 概念）

| SilkyUI                 | CSS 概念          | 说明                                 |
| ----------------------- | ----------------- | ------------------------------------ |
| `FlexDirection`         | `flex-direction`  | 仅支持 `Row` / `Column`              |
| `FlexWrap`              | `flex-wrap`       | `false`=nowrap，`true`=wrap          |
| `MainAlignment`         | `justify-content` | 主轴对齐                             |
| `CrossAlignment`        | `align-items`     | 交叉轴单行对齐                       |
| `CrossContentAlignment` | `align-content`   | 交叉轴多行对齐                       |
| `Gap`                   | `gap`             | 使用 `Size`，可分别控制横向/纵向间距 |
| `FlexGrow`              | `flex-grow`       | 子项增长系数                         |
| `FlexShrink`            | `flex-shrink`     | 子项收缩系数                         |

当前未实现：

- `row-reverse` / `column-reverse`
- `wrap-reverse`
- `space-around`
- `baseline`
- `flex-basis`
- `align-self`

## 容器属性（UIElementGroup）

| 属性                    | 类型                    | 默认值    | 说明               |
| ----------------------- | ----------------------- | --------- | ------------------ |
| `LayoutType`            | `LayoutType`            | `Flexbox` | 布局模块选择       |
| `FlexDirection`         | `FlexDirection`         | `Row`     | 主轴方向           |
| `FlexWrap`              | `bool`                  | `false`   | 是否换行           |
| `MainAlignment`         | `MainAlignment`         | `Start`   | 主轴对齐方式       |
| `CrossAlignment`        | `CrossAlignment`        | `Start`   | 交叉轴单行对齐方式 |
| `CrossContentAlignment` | `CrossContentAlignment` | `Stretch` | 交叉轴多行对齐方式 |
| `Gap`                   | `Size`                  | `0`       | 项/行之间的间距    |

### 换行触发条件（重点）

换行不是只看 `FlexWrap=true`，还依赖容器是否在对应轴“固定可用空间”：

- 当 `FlexDirection = Row`：
  - 只有 `FlexWrap=true` 且 `FitWidth=false` 时才会走换行逻辑。
- 当 `FlexDirection = Column`：
  - 只有 `FlexWrap=true` 且 `FitHeight=false` 时才会走换行逻辑。

否则会退化为单行（`SingleRow` 或 `SingleColumn`）。

## 子项属性（UIView）

| 属性         | 类型    | 默认值 | 说明                       |
| ------------ | ------- | ------ | -------------------------- |
| `FlexGrow`   | `float` | `0`    | 还有剩余空间时，按比例扩展 |
| `FlexShrink` | `float` | `0`    | 空间不足时，按比例收缩     |

这两个属性只有在父容器布局类型是 `Flexbox` 时才会触发布局脏标记。

## 尺寸分配规则

对每一行（或列）分别计算：

- 若主轴剩余空间 `remaining > 0`：
  - 对 `FlexGrow > 0` 的元素按比例分配可增长空间。
- 若主轴剩余空间 `remaining < 0`：
  - 对 `FlexShrink > 0` 的元素按比例分配收缩量。

分配过程中会受最小/最大外部尺寸约束：

- 宽度方向：`MinOuterWidth` / `MaxOuterWidth`
- 高度方向：`MinOuterHeight` / `MaxOuterHeight`

## 对齐规则

### 主轴对齐（MainAlignment）

支持：

- `Start`
- `End`
- `Center`
- `SpaceEvenly`
- `SpaceBetween`

### 交叉轴单行对齐（CrossAlignment）

支持：

- `Start`
- `Center`
- `End`
- `Stretch`

### 交叉轴多行对齐（CrossContentAlignment）

支持：

- `Start`
- `Center`
- `End`
- `SpaceEvenly`
- `SpaceBetween`
- `Stretch`

## 方向语义

### `FlexDirection.Row`

- 主轴：水平
- 交叉轴：垂直
- 同一行内项目间距使用 `Gap.Width`
- 行与行之间间距使用 `Gap.Height`

### `FlexDirection.Column`

- 主轴：垂直
- 交叉轴：水平
- 同一列内项目间距使用 `Gap.Height`
- 列与列之间间距使用 `Gap.Width`

## 使用示例

### 示例 1：水平分布

```xml
<ElementGroup FlexDirection="Row"
              MainAlignment="SpaceBetween"
              CrossAlignment="Center"
              Gap="10"
              FitWidth="false"
              Width="600px">
    <View Width="120px" Height="40px" />
    <View Width="120px" Height="40px" />
    <View Width="120px" Height="40px" />
</ElementGroup>
```

### 示例 2：自动换行卡片流

```xml
<ElementGroup FlexDirection="Row"
              FlexWrap="true"
              MainAlignment="Start"
              CrossContentAlignment="Start"
              Gap="8"
              FitWidth="false"
              Width="500px">
    <View Width="120px" Height="80px" />
    <View Width="120px" Height="80px" />
    <View Width="120px" Height="80px" />
    <View Width="120px" Height="80px" />
</ElementGroup>
```

### 示例 3：增长与收缩

```xml
<ElementGroup FlexDirection="Row"
              Gap="6"
              FitWidth="false"
              Width="420px">
    <View Width="120px" Height="40px" FlexGrow="1" />
    <View Width="180px" Height="40px" FlexShrink="1" />
    <View Width="180px" Height="40px" FlexShrink="2" />
</ElementGroup>
```

## 常见问题

### 1. 设置了 `FlexWrap=true` 但不换行

- `Row` 方向下要检查 `FitWidth` 是否为 `false`
- `Column` 方向下要检查 `FitHeight` 是否为 `false`

### 2. `FlexGrow` / `FlexShrink` 看起来没效果

仅当“子项总主轴尺寸”和“容器可用主轴空间”不匹配时才会生效。

### 3. 出现意外拉伸

检查：

- `CrossAlignment`
- `CrossContentAlignment`

尤其是是否设置为 `Stretch`。

## 快速排查清单

1. 父容器 `LayoutType` 是否是 `Flexbox`。
2. `FlexDirection` 是否符合预期。
3. `FitWidth` / `FitHeight` 是否与换行需求匹配。
4. 子项是否被最小/最大尺寸约束卡住。
5. 对齐属性是否显式设置（避免默认值影响判断）。
