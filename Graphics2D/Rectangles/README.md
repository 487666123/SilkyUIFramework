# 圆角矩形绘制的另一种 C# 组织方式

保留填充、描边、屏幕纹理采样、指定 UV 采样和阴影功能，直接复用现有的
[SDFRectangle.fx](../SDFRectangle.fx) 和 [SDFGraphicsVertexType.cs](../SDFGraphicsVertexType.cs)。
本目录没有独立 shader，也没有替换现有绘制入口。

## 两个类的分工

- [RectangleRenderer.cs](RectangleRenderer.cs)：实例持有顶点、索引缓存及 Effect 参数和 pass 引用，负责参数绑定与绘制提交。
- [RectangleGeometryBuilder.cs](RectangleGeometryBuilder.cs)：无状态的内部工具，向传入的 Span 写入顶点、索引和 UV。顶点数量与拓扑也在这里定义。

数据流是“绘制器传入自己的数组 → 构建器填写 → 绘制器提交”。索引只在实例创建时填写一次；
纹理绘制在共用几何构建后补充 UV，不重复实现位置、半径和 SDF 坐标的计算。

## 使用

在图形资源加载完成后创建一次，将实例保存在绘制组件或服务中，不要每次 Draw 都创建：

```csharp
using SilkyUIFramework.Graphics2D.Rectangles;

var renderer = new RectangleRenderer(
    Main.graphics.GraphicsDevice,
    ModAsset.SDFRectangle.Value,
    Main.spriteBatch.spriteEffectPass);
```

在原先允许立即提交图元的位置调用，例如：

```csharp
renderer.DrawFill(position, size, cornerRadii, fillColor, transform);
renderer.DrawBordered(position, size, cornerRadii, fillColor, borderWidth, borderColor, transform);
renderer.DrawScreenTexture(texture, position, size, cornerRadii, transform);
renderer.DrawTexture(texture, position, size, uvOrigin, uvSize, cornerRadii, tintColor, transform);
renderer.DrawShadow(position, size, cornerRadii, shadowColor, blurSize, transform);
```

上面的五行分别展示五种入口，并非要求每个矩形依次调用它们。
原来的 DrawWithoutBorder 对应 DrawFill，DrawWithBorder 对应 DrawBordered，其他三种入口沿用当前名字。

## 使用约定

- cornerRadii 的分量顺序为左上、右上、左下、右下；尺寸、半径、描边和阴影模糊宽度使用局部坐标单位。
- 纹理 UV 使用归一化坐标；uvOrigin 为左上角，uvSize 为区域尺寸。整张纹理对应 Vector2.Zero 和 Vector2.One。
- DrawScreenTexture 沿用以 BackBuffer 尺寸推导 UV 的规则，UV 不应用 transform。
- 沿用原抗锯齿和投影计算，transform 适用于正的等比缩放和平移；显式 UV 绘制和阴影保持零外扩。
- 颜色及纹理遵守现有 shader 的预乘 Alpha 约定；阴影范围、偏移和扩大的圆角由调用方准备。
- 设备、Effect 和恢复 pass 都是借用资源，必须来自同一设备；绘制器不负责释放它们。Effect 需包含当前源码中的 Textured pass。
- 应在图形线程串行调用；独立顶点缓存不代表 GraphicsDevice 或共享 Effect 可以并行绘制。
- 调用方负责批次提交顺序及混合、采样、裁剪、光栅化状态。每次绘制结束恢复指定 pass 和原纹理槽 0，不恢复完整设备状态或 Effect 参数。
- 图形资源重新加载后，使用新资源重新创建绘制器；绘制器缓存的 EffectParameter 和 EffectPass 引用不会自动跟随资源更换。

本次仅调整 C# 的组织方式，保留 shader 的描边混色、抗锯齿与阴影公式，不借此修改视觉算法。
新增的状态处理是通过 finally 恢复指定 pass 及纹理槽 0。按照工作区要求，仅做静态审阅，未运行测试或编译。
