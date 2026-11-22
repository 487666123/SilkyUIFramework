// 通用渐变的停止点纹理。第一行存颜色，第二行的 X 分量存停止点位置。
sampler uStops : register(s0);

// 将视口像素坐标映射到裁剪空间，也包含绘制时的平移。
float4x4 uProjection;

// 两行仿射变换系数，将网格的像素坐标换算成归一化的渐变坐标。
float3 uGradientX;

float3 uGradientY;

// 停止点数量和它的倒数，对应纹理宽度和单个纹素的宽度。
float uStopCount;

float uInverseStopCount;

// C# 每次绘制设置一次渐变类型。false 表示线性，true 表示径向。
bool uRadial;

// 区间外的着色方式。0 延伸端点，1 重复，2 镜像，3 透明。
float uTileMode;

// 停止点的 RGB 是否已乘以 Alpha，以及是否在线性 sRGB 中插值。
float uPremultiplied;

float uLinearRgb;

// 简单渐变直接从参数读取两端颜色。
float4 uFirstColor;

float4 uLastColor;

struct GradientVertex
{
    float4 Position : POSITION0;

    float4 Coverage : COLOR0;
};

struct GradientPixel
{
    float4 Position : SV_POSITION;

    float2 Coordinate : TEXCOORD0;

    float Coverage : TEXCOORD1;
};

GradientPixel VS_Gradient(GradientVertex input)
{
    GradientPixel output;
    output.Position = mul(float4(input.Position.xyz, 1), uProjection);
    float3 position = float3(input.Position.xy, 1);
    output.Coordinate = float2(dot(position, uGradientX), dot(position, uGradientY));
    // CPU 算出的覆盖率存放在顶点颜色的 Alpha 中。
    output.Coverage = input.Coverage.a;
    return output;
}

// 在指定行采样第 index 个纹素的中心。
float4 ReadStop(float index, float row)
{
    return tex2Dlod(uStops, float4((index + 0.5) * uInverseStopCount, row, 0, 0));
}

float4 InterpolateStops(float t)
{
    float low = 0;

    float high = uStopCount;
    // 查找第一项严格大于 t 的停止点。同位置的双停止点保留硬边。
    [loop]
    for (int i = 0; i < 13; i++)
    {
        if (low >= high)
            break;
        float mid = floor((low + high) * 0.5);
        if (t >= ReadStop(mid, 0.75).x)
            low = mid + 1;
        else
            high = mid;
    }

    float left = max(low - 1, 0);

    float right = min(low, uStopCount - 1);

    float from = ReadStop(left, 0.75).x;

    float to = ReadStop(right, 0.75).x;

    float fraction = 0;
    if (to > from)
        fraction = saturate((t - from) / (to - from));
    return lerp(ReadStop(left, 0.25), ReadStop(right, 0.25), fraction);
}

float3 ToSrgb(float3 linearColor)
{
    float3 low = 12.92 * linearColor;

    float3 high = 1.055 * pow(max(linearColor, 0), 1.0 / 2.4) - 0.055;
    return lerp(low, high, step(0.0031308, linearColor));
}

// 起点或圆心对应 t = 0，终点或一个渐变半径对应 t = 1。
float GradientPosition(float2 coordinate)
{
    // 径向取到圆心的距离，线性取沿渐变方向的投影。
    // 展开条件后选值，线性渐变也会计算距离。
    float t;
    [flatten]
    if (uRadial)
        t = length(coordinate);
    else
        t = coordinate.x;

    [flatten]
    if (uTileMode == 1)
        return frac(t);

    [flatten]
    if (uTileMode == 2)
        return 1 - abs(frac(t * 0.5) * 2 - 1);

    return t;
}

// 将插值颜色转成用于混合的预乘 sRGB，再用覆盖率缩放 RGB 和 Alpha。
float4 ApplyCoverage(float4 color, float coverage)
{
    if (uLinearRgb > 0.5)
    {
        // sRGB 转换作用于未预乘的颜色，转换后再乘回 Alpha。
        if (uPremultiplied > 0.5)
            color.rgb = color.a > 0 ? color.rgb / color.a : float3(0, 0, 0);
        color.rgb = ToSrgb(color.rgb) * color.a;
    }
    else if (uPremultiplied < 0.5)
    {
        color.rgb *= color.a;
    }
    return color * coverage;
}

// 两端颜色直接插值。纯色画笔的两端颜色相同。
float4 PS_SimpleGradient(GradientPixel input) : COLOR0
{
    float t = GradientPosition(input.Coordinate);

    // 区间外透明模式
    if (uTileMode == 3 && (t < 0 || t > 1))
        return float4(0, 0, 0, 0);

    float progress = saturate(t);
    float4 color = lerp(uFirstColor, uLastColor, progress);
    return ApplyCoverage(color, input.Coverage);
}

// 通用渐变先查找 t 两侧的停止点，再在相邻颜色之间插值。
float4 PS_Gradient(GradientPixel input) : COLOR0
{
    float t = GradientPosition(input.Coordinate);

    if (uTileMode == 3 && (t < 0 || t > 1))
        return float4(0, 0, 0, 0);

    // Clamp 保留原始 t，由查表延伸颜色，避免破坏端点硬边。
    return ApplyCoverage(InterpolateStops(t), input.Coverage);
}

technique PathGradientTechnique
{
    pass SimpleGradient
    {
        VertexShader = compile vs_3_0 VS_Gradient();
        PixelShader = compile ps_3_0 PS_SimpleGradient();
    }

    pass Gradient
    {
        VertexShader = compile vs_3_0 VS_Gradient();
        PixelShader = compile ps_3_0 PS_Gradient();
    }
}
