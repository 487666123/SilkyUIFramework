# UI 控件

SilkyUI 框架提供了一系列预定义的 UI 控件，用于快速构建用户界面。\
本页面详细介绍了每个控件的属性、方法和使用方式。

> **布局系统说明**：有关 Flexbox 布局的详细文档，请参阅 [FlexboxModule.md](./Layout/FlexboxModule.md)。

## 控件索引

| 控件                                        | XML 元素名        | 描述                                                     |
| ------------------------------------------- | ----------------- | -------------------------------------------------------- |
| **基础控件**                                |
| [`UIView`](#uiview)                         | `View`            | 所有 UI 元素的基础类，提供布局、样式和事件处理           |
| [`UIElementGroup`](#uielementgroup)         | `ElementGroup`    | 容器控件，用于组织和管理子元素                           |
| [`BaseBody`](#basebody)                     | `Body`            | UI 主体，继承自 `UIElementGroup`，提供完整的 UI 窗口功能 |
| **文本控件**                                |
| [`UITextView`](#uitextview)                 | `TextView`        | 文本显示控件，支持富文本、自动换行和样式设置             |
| [`SUIEditText`](#suiedittext)               | `EditText`        | 文本输入框，支持占位符和光标显示                         |
| **交互控件**                                |
| [`SUISlider`](#suislider)                   | `Slider`          | 滑块控件，用于选择数值范围                               |
| [`SUIToggleSwitch`](#suitoggleswitch)       | `ToggleSwitch`    | 切换开关控件，用于布尔值选择                             |
| [`SUIScrollbar`](#suiscrollbar)             | `Scrollbar`       | 滚动条控件，支持水平和垂直方向                           |
| **容器控件**                                |
| [`SUIScrollView`](#suiscrollview)           | `ScrollView`      | 滚动视图容器，包含滚动条和遮罩                           |
| [`SUIScrollContainer`](#suiscrollcontainer) | `ScrollContainer` | 滚动容器，用于承载可滚动内容                             |
| [`SUIScrollMask`](#suiscrollmask)           | `ScrollMask`      | 滚动遮罩，限制内容显示区域                               |
| [`SUIDraggableView`](#suidraggableview)     | `DraggableView`   | 可拖拽视图，支持拖拽移动                                 |
| **显示控件**                                |
| [`SUIImage`](#suiimage)                     | `Image`           | 图片显示控件，支持纹理和大小适配                         |
| [`SUIGif`](#suigif)                         | `Gif`             | GIF 动画显示控件                                         |
| [`SUIItemSlot`](#suiitemslot)               | `ItemSlot`        | 物品槽控件，用于显示和交互 Terraria 物品                 |
| **辅助控件**                                |
| [`SUIDividingLine`](#suidividingline)       | `DividingLine`    | 分割线控件，用于视觉分隔                                 |
| [`SUIBlankSpace`](#suiblankspace)           | `BlankSpace`      | 空白空间控件，用于占位和间距                             |
| [`SUICross`](#suicross)                     | `Cross`           | 十字关闭按钮控件                                         |

## 基础控件

### UIView

所有 UI 元素的基类，提供布局、样式、事件处理等基础功能。

**XML 元素名**: `View`

#### 关键属性

| 属性                      | 类型          | 描述                                                |
| ------------------------- | ------------- | --------------------------------------------------- |
| `Width`                   | `Dimension`   | 宽度尺寸                                            |
| `Height`                  | `Dimension`   | 高度尺寸                                            |
| `Padding`                 | `Margin`      | 内边距                                              |
| `Margin`                  | `Margin`      | 外边距                                              |
| `BackgroundColor`         | `Color`       | 背景颜色                                            |
| `Border`                  | `float`       | 边框宽度                                            |
| `BorderColor`             | `Color`       | 边框颜色                                            |
| `BorderRadius`            | `Vector4`     | 边框圆角（左上、右上、右下、左下）                  |
| `Positioning`             | `Positioning` | 定位方式（`Relative`、`Absolute`、`Sticky`、`Fixed`、`Static`） |
| `IgnoreMouseInteraction`  | `bool`        | 忽略鼠标交互（不影响子元素）                        |
| `DisableMouseInteraction` | `bool`        | 禁用鼠标交互（影响子元素）                          |
| `Invalid`                 | `bool`        | 是否无效（不参与布局）                              |
| `ZIndex`                  | `int`         | Z 轴顺序                                            |

#### 布局相关属性

有关 Flexbox 布局的详细说明，请参阅 [FlexboxModule.md](../Layout/FlexboxModule.md)。

#### 方法

| 方法                                 | 描述                                        |
| ------------------------------------ | ------------------------------------------- |
| `SetSize(float width, float height)` | 设置固定尺寸                                |
| `SetPadding(float padding)`          | 设置统一内边距                              |
| `SetMargin(float margin)`            | 设置统一外边距                              |
| `MarkLayoutDirty()`                  | 标记布局为脏，触发重新计算                  |
| `AddChild(UIView child)`             | 添加子元素（仅当父元素为 `UIElementGroup`） |
| `RemoveChild(UIView child)`          | 移除子元素                                  |

#### 事件

| 事件         | 描述               |
| ------------ | ------------------ |
| `MouseEnter` | 鼠标进入元素时触发 |
| `MouseLeave` | 鼠标离开元素时触发 |
| `MouseDown`  | 鼠标按下时触发     |
| `MouseUp`    | 鼠标释放时触发     |
| `MouseClick` | 鼠标点击时触发     |

### UIElementGroup

容器控件，用于组织和管理子元素，支持 Flexbox 和 Grid 布局。

**XML 元素名**: `ElementGroup`

#### 特有属性

| 属性                      | 类型                    | 描述                                                           |
| ------------------------- | ----------------------- | -------------------------------------------------------------- |
| `OverflowHidden`          | `bool`                  | 是否隐藏溢出内容                                               |
| `IndependentRenderTarget` | `bool`                  | 是否使用独立的渲染目标（当 `OverflowHidden` 为 `true` 时有效） |
| `Children`                | `IReadOnlyList<UIView>` | 子元素列表（只读）                                             |
| `ChildrenCache`           | `IReadOnlyList<UIView>` | 实际用于更新和绘制的子元素列表（只读）                         |

#### 方法

| 方法                            | 描述           |
| ------------------------------- | -------------- |
| `AddChild(UIView child)`        | 添加子元素     |
| `RemoveChild(UIView child)`     | 移除子元素     |
| `RemoveAllChildren()`           | 清空所有子元素 |
| `IndexOf(UIView view)`          | 获取子元素索引 |

### BaseBody

UI 主体，继承自 `UIElementGroup`，提供完整的 UI 窗口功能，通常用作顶级容器。

**XML 元素名**: `Body`

#### 特有属性

| 属性              | 类型   | 描述             |
| ----------------- | ------ | ---------------- |
| `Enabled`         | `bool` | 是否启用         |
| `IsInteractable`  | `bool` | 是否可交互       |
| `AvailableItem`   | `bool` | 是否允许物品交互 |
| `AvailableScroll` | `bool` | 是否允许滚动交互 |

#### 构造函数行为

`BaseBody` 构造函数默认设置：
- 尺寸：480×270 像素（16:9 比例）
- 间距：10 像素
- 定位方式：`Fixed`
- 边框：2 像素黑色
- 背景颜色：白色半透明（0.25 透明度）
- 布局类型：`Flexbox`
- 主轴方向：`Column`（垂直）
- 主轴对齐：`Start`
- 换行：`false`
- 绘制边框：`true`
（有关 Flexbox 布局的详细说明，请参阅 [FlexboxModule.md](../Layout/FlexboxModule.md)）

## 文本控件

### UITextView

用于显示文本的视图，支持富文本片段、自动换行、文本缩放、颜色等。

### 属性

| 属性                | 类型                | 描述                                                              |
| ------------------- | ------------------- | ----------------------------------------------------------------- |
| `Font`              | `DynamicSpriteFont` | 文本字体，默认为 `FontAssets.MouseText.Value`                     |
| `Text`              | `string`            | 文本内容，设置时会触发 `ContentChanging` 和 `ContentChanged` 事件 |
| `WordWrap`          | `bool`              | 是否自动换行                                                      |
| `MaxLines`          | `int`               | 最大行数，-1 表示无限制                                           |
| `TextScale`         | `float`             | 文本缩放比例，默认为 1f                                           |
| `TextColor`         | `Color`             | 文本颜色，默认为 `Color.White`                                    |
| `TextBorder`        | `float`             | 文本边框宽度，默认为 2f                                           |
| `TextBorderColor`   | `Color`             | 文本边框颜色，默认为 `Color.Black`                                |
| `TextOffset`        | `Vector2`           | 文本偏移量（像素）                                                |
| `TextPercentOffset` | `Vector2`           | 文本百分比偏移量（相对于视图内边界大小）                          |
| `TextPercentOrigin` | `Vector2`           | 文本百分比原点（相对于文本大小）                                  |
| `TextAlign`         | `Vector2`           | 文本对齐方式（0-1 范围，0 为左/上对齐，1 为右/下对齐）            |
| `IgnoreTextColor`   | `bool`              | 是否忽略文本片段的颜色，使用 `TextColor` 代替                     |
| `MaximumCharacters` | `int`               | 最大字符数，仅在输入时生效                                        |
| `TextSize`          | `Vector2`           | 只读属性，文本的测量大小（不受缩放影响）                          |
| `IsDeathText`       | `bool`              | 是否使用死亡文本字体                                              |
| `IsMouseText`       | `bool`              | 是否使用鼠标文本字体                                              |

### 方法

| 方法                                 | 描述                                      |
| ------------------------------------ | ----------------------------------------- |
| `UseDeathText()`                     | 将字体设置为 `FontAssets.DeathText.Value` |
| `UseMouseText()`                     | 将字体设置为 `FontAssets.MouseText.Value` |
| `Measure(float width, float height)` | 重写自 `UIView`，测量视图大小             |
| `RecalculateHeight()`                | 重新计算视图高度（当宽度固定时）          |

### 事件

| 事件              | 描述                                   |
| ----------------- | -------------------------------------- |
| `ContentChanging` | 当文本内容即将更改时触发，可以修改新值 |
| `ContentChanged`  | 当文本内容更改后触发                   |

### 示例

```csharp
var textView = new UITextView();
textView.Text = "Hello, World!";
textView.TextColor = Color.Yellow;
textView.TextScale = 1.5f;
textView.WordWrap = true;
```

### SUIEditText

文本输入框控件，继承自 `UITextView`，支持占位符、光标显示和文本输入。

**XML 元素名**: `EditText`

#### 特有属性

| 属性                     | 类型     | 描述                           |
| ------------------------ | -------- | ------------------------------ |
| `Placeholder`            | `string` | 占位符文本，当输入框为空时显示 |
| `PlaceholderColor`       | `Color`  | 占位符文本颜色                 |
| `PlaceholderBorderColor` | `Color`  | 占位符文本边框颜色             |
| `CursorColor`            | `Color`  | 光标颜色                       |
| `CursorFlashColor`       | `Color`  | 光标闪烁颜色                   |

#### 示例
```csharp
var editText = new SUIEditText();
editText.Placeholder = "请输入文本...";
editText.PlaceholderColor = Color.Gray;
editText.Text = "初始文本";
```

## 交互控件

### SUISlider

滑块控件，用于在指定范围内选择数值。

**XML 元素名**: `Slider`

#### 关键属性

| 属性           | 类型    | 描述                                  |
| -------------- | ------- | ------------------------------------- |
| `Value`        | `float` | 当前值（0-1 范围）                    |
| `Step`         | `float` | 步进值，0 表示连续                    |
| `MinValue`     | `float` | 最小值（映射到 0）                    |
| `MaxValue`     | `float` | 最大值（映射到 1）                    |
| `CurrentValue` | `float` | 当前值（根据 MinValue/MaxValue 计算） |

#### 事件
- `ValueChanged`: 值改变时触发
- `Drag`: 拖拽时触发

#### 示例
```csharp
var slider = new SUISlider();
slider.Value = 0.5f;
slider.Step = 0.1f;
slider.ValueChanged += (sender, value) => Console.WriteLine($"值改变: {value}");
```

### SUIToggleSwitch

切换开关控件，用于布尔值选择。

**XML 元素名**: `ToggleSwitch`

#### 关键属性

| 属性     | 类型   | 描述                              |
| -------- | ------ | --------------------------------- |
| `Status` | `bool` | 开关状态（true 为开，false 为关） |

#### 事件
- `OnStatusChanges`: 状态改变时触发

#### 示例
```csharp
var toggle = new SUIToggleSwitch();
toggle.Status = true;
toggle.OnStatusChanges += (status) => Console.WriteLine($"开关状态: {status}");
```

### SUIScrollbar

滚动条控件，支持水平和垂直方向。

#### 关键属性

| 属性                    | 类型      | 描述         |
| ----------------------- | --------- | ------------ |
| `CurrentScrollPosition` | `Vector2` | 当前滚动位置 |
| `GetScrollRange()`      | `Vector2` | 获取可滚动范围（方法） |

#### 事件
- `OnCurrentScrollPositionChanged`: 滚动位置改变时触发

## 容器控件

### SUIScrollView

滚动视图容器，包含滚动条和遮罩，用于显示可滚动内容。

**XML 元素名**: `ScrollView`

#### 关键属性

| 属性        | 类型                 | 描述                                   |
| ----------- | -------------------- | -------------------------------------- |
| `Direction` | `Direction`          | 滚动方向（`Horizontal` 或 `Vertical`） |
| `ScrollBar` | `SUIScrollbar`       | 滚动条实例                             |
| `Mask`      | `SUIScrollMask`      | 遮罩层实例                             |
| `Container` | `SUIScrollContainer` | 内容容器实例                           |

### SUIScrollContainer

滚动容器，用于承载可滚动内容。

**XML 元素名**: `ScrollContainer`

### SUIScrollMask

滚动遮罩，限制内容显示区域。

**XML 元素名**: `ScrollMask`

### SUIDraggableView

可拖拽视图，支持拖拽移动。

**XML 元素名**: `DraggableView`

## 显示控件

### SUIImage

图片显示控件，支持纹理和大小适配。

**XML 元素名**: `Image`

#### 关键属性

| 属性                | 类型               | 描述           |
| ------------------- | ------------------ | -------------- |
| `Texture2D`         | `Asset<Texture2D>` | 纹理资源       |
| `ImageOriginalSize` | `Vector2`          | 图片原始大小   |
| `ImageOffset`       | `Vector2`          | 图片偏移量     |
| `ImagePercent`      | `Vector2`          | 图片百分比位置 |
| `ImageScale`        | `Vector2`          | 图片缩放比例   |

#### 事件
- `TextureChanged`: 纹理改变时触发

### SUIGif

GIF 动画显示控件。

**XML 元素名**: `Gif`

#### 关键属性

| 属性                | 类型          | 描述         |
| ------------------- | ------------- | ------------ |
| `GifRenderer`       | `GifRenderer` | GIF 渲染器   |
| `ImageOriginalSize` | `Vector2`     | 图片原始大小 |

#### 事件
- `GifRendererChanged`: GIF 渲染器改变时触发

### SUIItemSlot

物品槽控件，用于显示和交互 Terraria 物品。

**XML 元素名**: `ItemSlot`

#### 关键属性

| 属性         | 类型   | 描述               |
| ------------ | ------ | ------------------ |
| `Item`       | `Item` | 物品实例           |
| `ItemInside` | `Item` | 内部物品（受保护） |

#### 事件
- `ItemChanged`: 物品改变时触发

## 辅助控件

### SUIDividingLine

分割线控件，用于视觉分隔。

**XML 元素名**: `DividingLine`

### SUIBlankSpace

空白空间控件，用于占位和间距。

**XML 元素名**: `BlankSpace`

### SUICross

十字关闭按钮控件。

**XML 元素名**: `Cross`

## 使用建议

1. **布局优先**: 优先使用 Flexbox 布局，避免硬编码位置
2. **响应式设计**: 使用百分比尺寸和弹性布局适应不同屏幕大小
3. **事件处理**: 利用控件提供的事件进行交互逻辑处理
4. **XML 布局**: 尽可能使用 XML 布局文件，提高代码可维护性

## 扩展自定义控件

可以通过继承 `UIView` 或现有控件类来创建自定义控件：

```csharp
[XmlElementMapping("CustomControl")]
public class CustomControl : UIView
{
    // 自定义属性
    public string CustomProperty { get; set; }
    
    // 自定义绘制
    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
        // 自定义绘制逻辑
    }
}
```

注册 XML 映射后，即可在 XML 布局中使用 `<CustomControl>` 元素。
