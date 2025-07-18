struct HitboxVSInput
{
    float4 vPosition : POSITION;
    float4 vDiffuse : COLOR0;
    float2 vTexCoor : TEXCOORD0;
};
struct HitboxVSOutput
{
    float4 position : POSITION;
    float4 color : COLOR0;
    float2 vTexCoor : TEXCOORD0;
};

uniform float4x4 mvpMatrix : register(c0);
uniform float2 viewPort : register(c4);


HitboxVSOutput HitboxVS(HitboxVSInput input)
{
    HitboxVSOutput output;
    
    output.position = mul(input.vPosition, mvpMatrix);
    // D3D9 Half-pixel offset correction
    output.position.xy -= float2(0.5, 0.5) * output.position.w / viewPort;
    //float2 ndc = output.position.xy / output.position.w;
    output.color = input.vDiffuse;
    output.vTexCoor = input.vTexCoor;
    
    return output;
}

struct HitboxPSInput
{
    float4 color : COLOR0;
    float2 vTexCoor : TEXCOORD0;
};

uniform float borderThickness : register(c5);

float4 HitboxPS(HitboxPSInput input) : COLOR
{
    float4 color = input.color;
    float2 uv = input.vTexCoor;
    
    if (borderThickness >= 0)
    {
        float2 dx = ddx(uv);
        float2 dy = ddy(uv);
        float2 uvSizeInPixels;
        uvSizeInPixels.x = 1.0 / length(dx);
        uvSizeInPixels.y = 1.0 / length(dy);
        float2 borderUV = borderThickness / uvSizeInPixels;
        
        bool isBorder =
            (uv.x < borderUV.x) || (uv.x > 1.0 - borderUV.x) ||
            (uv.y < borderUV.y) || (uv.y > 1.0 - borderUV.y);
        
        color.a = isBorder ? 1.0 : color.a;
    }
    
    return color;
}

technique Hitbox
{
    pass P0
    {
        VertexShader = compile vs_3_0 HitboxVS();
        PixelShader  = compile ps_3_0 HitboxPS();
    }
}


struct LineVSInput
{
    float4 vPosition : POSITION;
    float4 vDiffuse : COLOR0;
};
struct LineVSOutput
{
    float4 position : POSITION;
    float4 color : COLOR0;
};



LineVSOutput LineVS(LineVSInput input)
{
    LineVSOutput output;
    
    output.position = mul(input.vPosition, mvpMatrix);
    output.color = input.vDiffuse;
    
    return output;
}

struct LinePSInput
{
    float4 color : COLOR0;
};

float4 LinePS(LinePSInput input) : COLOR
{
    return input.color;
}

technique Line
{
    pass P1
    {
        VertexShader = compile vs_3_0 LineVS();
        PixelShader  = compile ps_3_0 LinePS();
    }
}