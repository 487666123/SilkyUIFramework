# 圆角矩形绘制与四边独立宽度、颜色

[RectangleRenderer.cs](RectangleRenderer.cs) 提供填充、统一宽度描边、四边独立宽度及颜色、纹理和阴影绘制。
四边可以共用一种颜色，也可以分别指定颜色。每次调用仍提交一次绘制，复用原有的 16 个顶点和 8 个三角形。

新增的 [RectangleEffect.fx](RectangleEffect.fx) 是本目录专用 shader。
它包含原有四种 pass，以及 HasPerSideBorder 和 HasPerSideBorderColors。原 [SDFRectangle.fx](../SDFRectangle.fx) 未修改。
原顶点格式 [SDFGraphicsVertexType.cs](../SDFGraphicsVertexType.cs) 继续复用。

## 加载一次，复用绘制器

更新 shader 资源后，在图形资源就绪的客户端图形线程创建实例。直接使用 ModAsset.RectangleEffect.Value：

~~~csharp
using Microsoft.Xna.Framework;
using SilkyUIFramework.Graphics2D.Rectangles;
using Terraria;

var renderer = new RectangleRenderer(
    Main.graphics.GraphicsDevice,
    ModAsset.RectangleEffect.Value,
    Main.spriteBatch.spriteEffectPass);
~~~

把 renderer 保存在字段或绘制服务中，不要每次绘制都创建实例。
资源重新加载后应重建绘制器，因为 EffectParameter 和 EffectPass 引用在构造时缓存。

## 绘制四边不同宽度和颜色

~~~csharp
renderer.DrawBordered(
    position: new Vector2(100f, 100f),
    size: new Vector2(240f, 120f),
    cornerRadii: new Vector4(12f, 24f, 8f, 20f),
    fillColor: new Color(40, 44, 52),
    borderWidths: new Vector4(4f, 10f, 18f, 2f),
    leftBorderColor: Color.OrangeRed,
    topBorderColor: Color.Gold,
    rightBorderColor: Color.CornflowerBlue,
    bottomBorderColor: Color.MediumSeaGreen,
    transform: Matrix.Identity);
~~~

宽度和颜色的顺序都是左、上、右、下。颜色按“到边的距离 / 边宽”最小的有效边归属，在交界处做局部抗锯齿混色。
等宽相邻边在圆角处按 45° 方向分界；不等宽时，分界线随边宽比例变化。
零宽边的颜色不参与混合；颜色透明并不取消该边的宽度。内部区域被边框占满时，四边颜色仍按同一规则分区。
多边相接时会归一化颜色权重，四种颜色相同时自动复用单色四边路径。

## 只指定四边不同宽度

在原先允许立即提交图元的位置调用：

~~~csharp
renderer.DrawBordered(
    position: new Vector2(100f, 100f),
    size: new Vector2(240f, 120f),
    cornerRadii: new Vector4(12f, 24f, 8f, 20f),
    fillColor: new Color(40, 44, 52),
    borderWidths: new Vector4(4f, 10f, 18f, 2f),
    borderColor: Color.CornflowerBlue,
    transform: Matrix.Identity);
~~~

- borderWidths：左、上、右、下，示例中分别为 4、10、18、2。
- cornerRadii：左上、右上、左下、右下，示例中分别为 12、24、8、20。
- 使用你的 UI 变换时，把 Matrix.Identity 换成实际的 UI 矩阵；位置、尺寸、半径和边宽都采用该矩阵变换前的局部坐标。
- 透明背景可以传 Color.Transparent；若需要半透明色，请传入符合预乘 Alpha 约定的值，例如 Color.CornflowerBlue * 0.5f。

一个边宽为零就表示该边不占宽度。四边全零退化为纯填充。
负边宽和负圆角在四边重载中按零处理；边宽吃完内矩形时不再绘制背景，整个外轮廓使用边框颜色或颜色分区。
内圆角在两个方向分别减去相邻边宽，形成椭圆；任一轴退化为零时按直角处理。
内圆角过大时统一缩小到内矩形可容纳的范围。

## 其他绘制接口

~~~csharp
// 原标量重载：保留既有描边公式。
renderer.DrawBordered(position, size, cornerRadii,
    fillColor, 4f, borderColor, transform);

// 四边重载：等宽也使用内外覆盖率之差，和不等宽情况保持同一种混色规则。
renderer.DrawBordered(position, size, cornerRadii,
    fillColor, new Vector4(4f), borderColor, transform);

renderer.DrawFill(position, size, cornerRadii, fillColor, transform);
renderer.DrawScreenTexture(texture, position, size, cornerRadii, transform);
renderer.DrawTexture(texture, position, size,
    uvOrigin, uvSize, cornerRadii, tintColor, transform);
renderer.DrawShadow(position, size, cornerRadii, shadowColor, blurSize, transform);
~~~

所有绘制入口统一使用 ModAsset.RectangleEffect.Value，不支持传入旧的 SDFRectangle Effect。
构造函数要求完整的六个 pass 及对应参数；缺失时立即报错，不提供可选能力或兼容分支。

四边重载统一计算独立内轮廓，等宽和不等宽使用相同的几何及覆盖率混色规则。
原 float 重载保留旧混色，所以细描边的外观可能与 Vector4 重载略有差异。需要连续调整四边时，始终使用 Vector4 重载。

## 实现与使用约定

- [RectangleGeometryBuilder.cs](RectangleGeometryBuilder.cs) 是无状态工具，填写调用方数组，并预计算内轮廓及六条颜色分界线的系数。
- 绘制器拥有缓存及参数、pass 引用；无需给每个矩形创建新的数组或绘制器。
- 内椭圆使用边界附近的局部距离近似，没有迭代求解；内圆角为圆形时退化为圆距离，与外轮廓共用抗锯齿区间。
- 非角区域提前跳过椭圆距离计算；很窄的内矩形额外限制覆盖率，避免内部区域消失时留下半透明细带。
- 颜色分界系数在 CPU 归一化，shader 不需要再除以边宽或计算分界线长度；多边交汇的混色是局部抗锯齿近似。
- 非圆椭圆的抗锯齿仍是近似，极扁圆角或亚像素尺寸应以实际画面为准；没有进行性能测量。
- 沿用原有外轮廓四象限和 padding 规则，并未扩展原有超大外圆角的支持范围。
- transform 适用于正的等比缩放和平移。显式 UV 绘制和阴影继续保持原来的零外扩行为。
- DrawScreenTexture 根据 BackBuffer 尺寸推导 UV，UV 不应用 transform。
- 设备、Effect 和恢复 pass 都是借用资源，必须来自同一设备，绘制器不负责释放它们。
- 在图形线程串行调用。调用方负责批次提交顺序、混合、采样、裁剪和光栅化状态；绘制器恢复指定 pass 与纹理槽 0。

本次仅做静态审查，没有运行测试、编译或实际图形验证；调用方接入和画面测试由使用者单独进行。
