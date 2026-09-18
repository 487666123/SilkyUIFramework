# AspectRatio

`UIView.AspectRatio` 表示宽度与高度的比值，例如 `16f / 9f`。默认值 `0` 关闭比例；负数、NaN 和无穷大会抛出 `ArgumentOutOfRangeException`。

## 使用

```csharp
var view = new UIView
{
    Width = new Dimension(320f),
    FitHeight = true,
    AspectRatio = 16f / 9f
};
```

未受到高度上下限影响时，高度为 `180`。`FitHeight = false` 时忽略比例，继续使用声明高度和现有布局规则。将 `AspectRatio` 设回 `0` 会恢复原来的内容自适应高度行为。属性变化会标记布局为脏。

## 盒模型与约束

- `BoxSizing.Border` 使用 `Bounds.Width / AspectRatio` 计算边框盒高度。
- `BoxSizing.Content` 使用 `InnerBounds.Width / AspectRatio` 计算内容盒高度，再叠加 Padding 和 Border。
- Margin 不参与比例计算，但仍计入父布局的占用尺寸。
- `MinHeight` 和 `MaxHeight` 优先于比例，按对应盒模型钳制高度；不会为了保持比例反向修改宽度。
- 比例计算更新实际盒模型尺寸，不改写声明属性 `Height`。

## 布局顺序

初次测量提供比例高度的估计值。宽度分配完成后，使用该轮最终宽度重新计算比例高度，让父容器的内容尺寸、Flex 行尺寸和 Grid 行轨道读取这一结果。后续垂直 Stretch 和纵向 FlexGrow/FlexShrink 不再修改比例元素的高度，也不会把纵向伸缩份额分给它。

比例元素作为容器时，其高度由宽度决定，不再由内容撑高。内部布局可使用这个高度解析百分比、Grid 的 fr 行和纵向对齐。内容超过比例高度时沿用现有的溢出、裁剪和滚动规则，比例功能不负责缩放文字或图片。

## 纵向 Flex 换列

宽度阶段结束后不再因高度变化重新分配宽度。`Column + FlexWrap` 按最终高度重新分列并计算对齐，但不会再按照新分列重新执行横向 Stretch，也不会反向修改自适应父宽度。新列与冻结宽度不匹配时可以留空或溢出，不进行宽高迭代求解。

## Grid 百分比高度约束

比例元素所在区域包含 Auto 端点的行轨道时，百分比 `MinHeight` / `MaxHeight` 使用测量阶段的容器可用高度作为基准；内容自适应高度的容器使用 `0`。不再用由该元素撑出的 Auto 行高度反过来重算约束，以免形成循环依赖。

不含 Auto 端点的区域按分配后的 Grid 区域高度更新比例元素的高度约束。高度上下限或比例使元素超过固定区域时，可以溢出，不反向撑开固定轨道。
