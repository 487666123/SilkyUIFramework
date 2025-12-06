## Flexible Box 基本属性

### `Flexbox` 需要将 LayoutType 设置为 Flexbox

| 属性                    | 类型           | 作用               |
| ----------------------- | -------------- | ------------------ |
| `FlexDirection`         | `row` `column` | 主轴方向           |
| `FlexWrap`              | `boolean`      | 子元素换行         |
| `MainAlignment`         | `enum`         | 主轴对齐方式       |
| `CrossAlignment`        | `enum`         | 交叉轴单行对齐方式 |
| `CrossContentAlignment` | `enum`         | 交叉轴多行对其方式 |

### `Flexbox Item` 在父元素为 Flexbox 时才有效

基本算法

- `FlexGrow` 压缩子元素: (child - parent) / totalGrow * grow
- `FlexShrink` 拉伸子元素: (parent - child) / totalShrink * shrink

如果遇到 Min Max 约束, 会按照最易满足的子元素开始拉长或者收短

| 属性            | 作用                       |
| --------------- | -------------------------- |
| `FlexGrow`      | 子元素总尺寸大于父元素生效 |
| `FlexShrink`    | 子元素总尺寸小于父元素生效 |
| `FlexBasis`     | 定义项目初始大小 `don't`   |
| `FlexAlignSelf` | 定义项目单独对齐 `don't`   |

## 属性效果

### `ENUM: MainAlignment` 主轴对其方式

| 名称            | 效果                                               |
| --------------- | -------------------------------------------------- |
| `start`         | 头部对齐                                           |
| `center`        | 居中对其                                           |
| `end`           | 尾部对齐                                           |
| `space-between` | 效果: [item] [1] [item] [1] [item]                 |
| `space-evenly`  | 效果: [1] [item] [1] [item] [1] [item] [1]         |
| `space-around`  | 效果: [1] [item] [2] [item] [2] [item] [1] `don't` |

### `ENUM: CrossContentAlignment` 交叉轴对其方式

| 名称            | 效果                                       |
| --------------- | ------------------------------------------ |
| `start`         | 头部对齐                                   |
| `center`        | 居中对其                                   |
| `end`           | 尾部对齐                                   |
| `space-between` | 效果: [item] [1] [item] [1] [item]         |
| `space-evenly`  | 效果: [1] [item] [1] [item] [1] [item] [1] |
| `stretch`       | 拉伸子元素                                 |

### `ENUM: CrossAlignment` 交叉轴对其方式

| 名称      | 效果       |
| --------- | ---------- |
| `start`   | 头部对齐   |
| `center`  | 居中对其   |
| `end`     | 尾部对齐   |
| `stretch` | 拉伸子元素 |

