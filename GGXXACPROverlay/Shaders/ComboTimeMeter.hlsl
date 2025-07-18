#define BORDER_SIZE_PX 2
#define HSD_THRESHOLD_1 (18.0 / 86.0)
#define HSD_THRESHOLD_2 (30.0 / 86.0)
#define HSD_THRESHOLD_3 (42.0 / 86.0)
#define HSD_THRESHOLD_4 (60.0 / 86.0)
#define COMBO_TIME_MAX 860.0
#define HSD_COLOR_100 float4(0.0, 0.0, 1.0, 1.0)
#define HSD_COLOR_95 float4(0.0, 1.0, 0.0, 1.0)
#define HSD_COLOR_90 float4(1.0, 1.0, 0.0, 1.0)
#define HSD_COLOR_80 float4(1.0, 0.5, 0.0, 1.0)
#define HSD_COLOR_70 float4(1.0, 0.0, 0.0, 1.0)
#define EMPTY_COLOR float4(0.0, 0.0, 0.0, 0.2)
#define BORDER_COLOR float4(0.0, 0.0, 0.0, 1.0)


struct MeterVSData
{
    float4 vPosition : POSITION;
    float4 vColor : COLOR0;
    float2 vTexCoor : TEXCOORD0;
};

uniform float2 viewPort : register(c0);

// Simple pass-through
MeterVSData MeterVS(MeterVSData input)
{
    MeterVSData output;
    
    output.vPosition = input.vPosition;
    // D3D9 Half-pixel offset correction
    output.vPosition.xy -= float2(0.5, 0.5) * output.vPosition.w / viewPort;
    output.vColor = input.vColor;
    output.vTexCoor = input.vTexCoor;
    
    return output;
}

struct ComboTimePSInput
{
    float2 vTexCoor : TEXCOORD0;
};

// x = value | y = max value
uniform float2 meterParams : register(c1);

float4 ComboTimePS(ComboTimePSInput input) : COLOR
{
    float2 uv = input.vTexCoor;
    uv.y = 1.0 - uv.y;
    float comboTimeUV = meterParams.x / COMBO_TIME_MAX;
    
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    float2 uvSizeInPixels;
    uvSizeInPixels.x = 1.0 / length(dx);
    uvSizeInPixels.y = 1.0 / length(dy);
    float2 borderUV = BORDER_SIZE_PX / uvSizeInPixels;
    
    float4 color;
    
    if (uv.y < HSD_THRESHOLD_1)
        color = HSD_COLOR_100;
    else if (uv.y < HSD_THRESHOLD_2)
        color = HSD_COLOR_95;
    else if (uv.y < HSD_THRESHOLD_3)
        color = HSD_COLOR_90;
    else if (uv.y < HSD_THRESHOLD_4)
        color = HSD_COLOR_80;
    else
        color = HSD_COLOR_70;
    
    bool isBorder =
        (uv.x < borderUV.x) || (uv.x > 1.0 - borderUV.x) ||
        (uv.y < borderUV.y) || (uv.y > 1.0 - borderUV.y);
    
    if (uv.y < comboTimeUV)
        color = EMPTY_COLOR;
    if (isBorder) color = BORDER_COLOR;
    
    return color;
}

technique ComboTime
{
    pass P1
    {
        VertexShader = compile vs_3_0 MeterVS();
        PixelShader  = compile ps_3_0 ComboTimePS();
    }
}


struct MeterPSInput
{
    float4 vColor : COLOR0;
    float2 vTexCoor : TEXCOORD0;
};

float4 MeterPS(MeterPSInput input) : COLOR
{
    float2 uv = input.vTexCoor;
    float meterValueUV = meterParams.x / meterParams.y;
    
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    float2 uvSizeInPixels;
    uvSizeInPixels.x = 1.0 / length(dx);
    uvSizeInPixels.y = 1.0 / length(dy);
    float2 borderUV = BORDER_SIZE_PX / uvSizeInPixels;
    
    float4 color = input.vColor;
    
    bool isBorder =
        (uv.x < borderUV.x) || (uv.x > 1.0 - borderUV.x) ||
        (uv.y < borderUV.y) || (uv.y > 1.0 - borderUV.y);
    
    if (uv.y > meterValueUV)
        color = EMPTY_COLOR;
    if (isBorder)
        color = BORDER_COLOR;
    
    return color;
}

technique Meter
{
    pass P1
    {
        VertexShader = compile vs_3_0 MeterVS();
        PixelShader = compile ps_3_0 MeterPS();
    }
}
