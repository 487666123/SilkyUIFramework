// RectangleRenderer 的独立 Effect。原 SDFRectangle.fx 保持不变。
// 颜色和纹理使用预乘 Alpha；局部坐标与现有矩形顶点格式一致。
sampler uImage0 : register(s0);

cbuffer RectangleParameters : register(b0)
{
    float4x4 uTransformMatrix;
    float4 uBackgroundColor;
    float4 uBorderColor;
    float2 uSmoothstepRange;
    float uBorder;
    float uShadowBlurSize;

    float2 uInnerOrigin;
    float2 uInnerSize;
    // 顺序均为左上、右上、左下、右下；退化为直角时倒数与 mask 均为 0。
    float4 uInnerInvRadiusX;
    float4 uInnerInvRadiusY;
    float4 uInnerCornerMask;

    float2 uRectangleOrigin;
    float2 uRectangleSize;
    float4 uBorderColorLeft;
    float4 uBorderColorTop;
    float4 uBorderColorRight;
    float4 uBorderColorBottom;
    // 每组两个系数：相邻横向边宽、纵向边宽除以它们的向量长度。
    float4 uBorderTopCornerWeights;    // 左上、右上
    float4 uBorderBottomCornerWeights; // 左下、右下
    float2 uBorderOppositeSplits;      // 左右、上下的颜色分界坐标
    float4 uBorderSideMask;            // 左、上、右、下；零宽边为 0
    float uInnerEnabled;              // 彩色描边即使没有内部区域也需要绘制
};

struct VSInput
{
    float2 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
    float2 DistanceFromEdge : TEXCOORD1;
    float BorderRadius : TEXCOORD2;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TextureCoordinates : TEXCOORD0;
    float2 DistanceFromEdge : TEXCOORD1;
    float BorderRadius : TEXCOORD2;
    float2 InnerPosition : TEXCOORD3;
    float2 RectanglePosition : TEXCOORD4;
};

PSInput VS_Rectangle(VSInput input)
{
    PSInput output;
    output.Position = mul(float4(input.Position, 0, 1), uTransformMatrix);
    output.TextureCoordinates = input.TextureCoordinates;
    output.DistanceFromEdge = input.DistanceFromEdge;
    output.BorderRadius = input.BorderRadius;
    output.InnerPosition = input.Position - uInnerOrigin;
    output.RectanglePosition = input.Position - uRectangleOrigin;
    return output;
}

// q 已经由几何构建器折叠方向并加上半径偏移。
float RoundedRectangleDistance(float2 q, float radius)
{
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
}

float DistanceCoverage(float distance)
{
    return 1.0 - smoothstep(uSmoothstepRange.x, uSmoothstepRange.y, distance);
}

// 保留原统一宽度描边的混色公式。
float4 HasBorder(PSInput input) : SV_Target
{
    float distance = RoundedRectangleDistance(input.DistanceFromEdge, input.BorderRadius);
    float4 color = lerp(uBackgroundColor, uBorderColor,
        smoothstep(uSmoothstepRange.x, uSmoothstepRange.y, distance + uBorder));
    return color * DistanceCoverage(distance);
}

float4 NoBorder(PSInput input) : SV_Target
{
    float distance = RoundedRectangleDistance(input.DistanceFromEdge, input.BorderRadius);
    return uBackgroundColor * DistanceCoverage(distance);
}

float4 Textured(PSInput input) : SV_Target
{
    float distance = RoundedRectangleDistance(input.DistanceFromEdge, input.BorderRadius);
    return tex2D(uImage0, input.TextureCoordinates) * uBackgroundColor * DistanceCoverage(distance);
}

float4 Shadow(PSInput input) : SV_Target
{
    float distance = RoundedRectangleDistance(input.DistanceFromEdge, input.BorderRadius);
    float coverage = 1.0 - smoothstep(
        uSmoothstepRange.x - uShadowBlurSize, uSmoothstepRange.y, distance);
    return uBackgroundColor * coverage;
}

// 椭圆边界附近的局部距离近似。圆形时退化为精确圆距离。
// 没有导数，允许非角区域提前返回，避免在那里计算椭圆距离。
float InnerCornerDistance(float2 q, float2 inverseRadii, float boxDistance)
{
    [branch]
    if (q.x <= 0.0 || q.y <= 0.0)
        return boxDistance;

    float k0 = length(q);
    float k1 = length(q * inverseRadii);
    return k0 * (k0 - 1.0) / max(k1, 0.000000000001);
}

float InnerRectangleDistance(float2 p)
{
    float2 box = abs(p - uInnerSize * 0.5) - uInnerSize * 0.5;
    float distance = min(max(box.x, box.y), 0.0) + length(max(box, 0.0));
    float boxDistance = distance;

    float4 inwardX = float4(p.x, uInnerSize.x - p.x, p.x, uInnerSize.x - p.x);
    float4 inwardY = float4(p.y, p.y, uInnerSize.y - p.y, uInnerSize.y - p.y);
    // 半径倒数在 CPU 预计算；mask 为 0 的角退化为直角。
    float4 qx = max(uInnerCornerMask - inwardX * uInnerInvRadiusX, 0.0);
    float4 qy = max(uInnerCornerMask - inwardY * uInnerInvRadiusY, 0.0);

    distance = max(distance, InnerCornerDistance(float2(qx.x, qy.x),
        float2(uInnerInvRadiusX.x, uInnerInvRadiusY.x), boxDistance));
    distance = max(distance, InnerCornerDistance(float2(qx.y, qy.y),
        float2(uInnerInvRadiusX.y, uInnerInvRadiusY.y), boxDistance));
    distance = max(distance, InnerCornerDistance(float2(qx.z, qy.z),
        float2(uInnerInvRadiusX.z, uInnerInvRadiusY.z), boxDistance));
    distance = max(distance, InnerCornerDistance(float2(qx.w, qy.w),
        float2(uInnerInvRadiusX.w, uInnerInvRadiusY.w), boxDistance));
    return distance;
}

float InnerRectangleCoverage(float2 p, float outerCoverage)
{
    // 内外轮廓共用局部距离和同一抗锯齿区间。
    float innerCoverage = DistanceCoverage(InnerRectangleDistance(p));

    // 很窄的内部区域，其覆盖率随可用宽高趋于零。
    [branch]
    if (min(uInnerSize.x, uInnerSize.y) < uSmoothstepRange.y - uSmoothstepRange.x)
    {
        float2 intervalCoverage = saturate(
            smoothstep(uSmoothstepRange.x, uSmoothstepRange.y, p)
            - smoothstep(uSmoothstepRange.x, uSmoothstepRange.y, p - uInnerSize));
        innerCoverage = min(innerCoverage, min(intervalCoverage.x, intervalCoverage.y));
    }

    return min(innerCoverage, outerCoverage);
}

float4 HasPerSideBorder(PSInput input) : SV_Target
{
    float outerDistance = RoundedRectangleDistance(input.DistanceFromEdge, input.BorderRadius);
    float outerCoverage = DistanceCoverage(outerDistance);
    float innerCoverage = InnerRectangleCoverage(input.InnerPosition, outerCoverage);

    return uBackgroundColor * innerCoverage
         + uBorderColor * (outerCoverage - innerCoverage);
}

float4 PerSideBorderColor(float2 p)
{
    // 像素属于 distance / width 最小的有效边。
    // CPU 已归一化六条分界线；这里的比较值是到分界线的有符号局部距离。
    float4 d = float4(p.x, p.y, uRectangleSize.x - p.x, uRectangleSize.y - p.y);
    float lo = uSmoothstepRange.x;
    float hi = uSmoothstepRange.y;

    // 名称中的第一条边获胜时，值趋于 1；第二条边获胜时，值趋于 0。
    float leftTop = smoothstep(lo, hi,
        d.y * uBorderTopCornerWeights.x - d.x * uBorderTopCornerWeights.y);
    float leftRight = smoothstep(lo, hi, uBorderOppositeSplits.x - p.x);
    float leftBottom = smoothstep(lo, hi,
        d.w * uBorderBottomCornerWeights.x - d.x * uBorderBottomCornerWeights.y);
    float topRight = smoothstep(lo, hi,
        d.z * uBorderTopCornerWeights.w - d.y * uBorderTopCornerWeights.z);
    float topBottom = smoothstep(lo, hi, uBorderOppositeSplits.y - p.y);
    float rightBottom = smoothstep(lo, hi,
        d.w * uBorderBottomCornerWeights.z - d.z * uBorderBottomCornerWeights.w);

    // 排除零宽边。各边同时满足与其他有效边的比较，交界处形成局部抗锯齿过渡。
    // 对边也参与比较，因此内部区域消失、多个颜色在中心相接时仍有定义。
    float4 weights;
    weights.x = uBorderSideMask.x
        * lerp(1.0, leftTop, uBorderSideMask.y)
        * lerp(1.0, leftRight, uBorderSideMask.z)
        * lerp(1.0, leftBottom, uBorderSideMask.w);
    weights.y = uBorderSideMask.y
        * lerp(1.0, 1.0 - leftTop, uBorderSideMask.x)
        * lerp(1.0, topRight, uBorderSideMask.z)
        * lerp(1.0, topBottom, uBorderSideMask.w);
    weights.z = uBorderSideMask.z
        * lerp(1.0, 1.0 - leftRight, uBorderSideMask.x)
        * lerp(1.0, 1.0 - topRight, uBorderSideMask.y)
        * lerp(1.0, rightBottom, uBorderSideMask.w);
    weights.w = uBorderSideMask.w
        * lerp(1.0, 1.0 - leftBottom, uBorderSideMask.x)
        * lerp(1.0, 1.0 - topBottom, uBorderSideMask.y)
        * lerp(1.0, 1.0 - rightBottom, uBorderSideMask.z);

    // 归一化保证同色和多边交汇处不会因权重和变化产生额外透明度。
    float total = max(weights.x + weights.y + weights.z + weights.w, 0.00001);
    return (uBorderColorLeft * weights.x + uBorderColorTop * weights.y
        + uBorderColorRight * weights.z + uBorderColorBottom * weights.w) / total;
}

float4 HasPerSideBorderColors(PSInput input) : SV_Target
{
    float outerDistance = RoundedRectangleDistance(input.DistanceFromEdge, input.BorderRadius);
    float outerCoverage = DistanceCoverage(outerDistance);
    float innerCoverage = 0.0;

    [branch]
    if (uInnerEnabled > 0.5)
        innerCoverage = InnerRectangleCoverage(input.InnerPosition, outerCoverage);

    float4 borderColor = PerSideBorderColor(input.RectanglePosition);
    return uBackgroundColor * innerCoverage
         + borderColor * (outerCoverage - innerCoverage);
}

technique T1
{
    pass HasBorder
    {
        VertexShader = compile vs_3_0 VS_Rectangle();
        PixelShader = compile ps_3_0 HasBorder();
    }

    pass NoBorder
    {
        VertexShader = compile vs_3_0 VS_Rectangle();
        PixelShader = compile ps_3_0 NoBorder();
    }

    pass Shadow
    {
        VertexShader = compile vs_3_0 VS_Rectangle();
        PixelShader = compile ps_3_0 Shadow();
    }

    pass Textured
    {
        VertexShader = compile vs_3_0 VS_Rectangle();
        PixelShader = compile ps_3_0 Textured();
    }

    pass HasPerSideBorder
    {
        VertexShader = compile vs_3_0 VS_Rectangle();
        PixelShader = compile ps_3_0 HasPerSideBorder();
    }

    pass HasPerSideBorderColors
    {
        VertexShader = compile vs_3_0 VS_Rectangle();
        PixelShader = compile ps_3_0 HasPerSideBorderColors();
    }
}
