﻿float4x4 uTransform;
float2 uSizeOver2;
float uBorder;
float uRound;
float4 uBorderColor;
float4 uBackgroundColor;
float2 uStart;
float2 uEnd;
float uLineWidth;
float2 uSmoothstepRange; // 过渡范围（原uTransition，更名以提高可读性）

struct VSInput
{
    float2 Pos : POSITION0;
    float2 Coord : TEXCOORD0;
};

struct PSInput
{
    float4 Pos : SV_POSITION;
    float2 Coord : TEXCOORD0;
};

PSInput VSFunction(VSInput input)
{
    PSInput output;
    output.Coord = input.Coord;
    output.Pos = mul(float4(input.Pos, 0, 1), uTransform);
    return output;
}

float4 Cross(float2 coords : TEXCOORD0) : COLOR0
{
    float2 p = abs(coords - uSizeOver2);
    float d = length(p - min(p.x + p.y, uSizeOver2) * 0.5) - uRound;
    return lerp(lerp(uBackgroundColor, uBorderColor, smoothstep(uSmoothstepRange.x, uSmoothstepRange.y, d + uBorder)), 0, smoothstep(uSmoothstepRange.x, uSmoothstepRange.y, d));
}

float4 HasBorderLine(float2 coords : TEXCOORD0) : COLOR0
{
    float2 ba = uEnd - uStart;
    float2 pa = coords - uStart;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0, 1);
    return lerp(lerp(uBackgroundColor, uBorderColor, smoothstep(-0.5, 0.5, length(pa - h * ba) - uLineWidth + uBorder)), 0, smoothstep(-0.5, 0.5, length(pa - h * ba) - uLineWidth));
}

float4 NoBorderLine(float2 coords : TEXCOORD0) : COLOR0
{
    float2 ba = uEnd - uStart;
    float2 pa = coords - uStart;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0, 1);
    return lerp(uBackgroundColor, 0, smoothstep(-0.5, 0.5, length(pa - h * ba) - uLineWidth));
}

float4 HasBorderRound(float2 coords : TEXCOORD0) : COLOR0
{
    float2 p = abs(coords - uSizeOver2);
    float d = distance(p, 0);
    return lerp(lerp(uBackgroundColor, uBorderColor, smoothstep(-1, 0.5, d - uSizeOver2.x + uBorder)), 0, smoothstep(-1, 0.5, d - uSizeOver2.x));
}

float4 NoBorderRound(float2 coords : TEXCOORD0) : COLOR0
{
    float2 p = abs(coords - uSizeOver2);
    return lerp(uBackgroundColor, 0, smoothstep(-1, 0.5, distance(p, 0) - uSizeOver2.x));
}

technique Technique1
{
    pass HasBorderCross
    {
        VertexShader = compile vs_3_0 VSFunction();
        PixelShader = compile ps_2_0 Cross();
    }

    pass HasBorderLine
    {
        VertexShader = compile vs_3_0 VSFunction();
        PixelShader = compile ps_2_0 HasBorderLine();
    }

    pass NoBorderLine
    {
        VertexShader = compile vs_3_0 VSFunction();
        PixelShader = compile ps_2_0 NoBorderLine();
    }

    pass HasBorderRound
    {
        VertexShader = compile vs_3_0 VSFunction();
        PixelShader = compile ps_3_0 HasBorderRound();
    }

    pass NoBorderRound
    {
        VertexShader = compile vs_3_0 VSFunction();
        PixelShader = compile ps_3_0 NoBorderRound();
    }
}