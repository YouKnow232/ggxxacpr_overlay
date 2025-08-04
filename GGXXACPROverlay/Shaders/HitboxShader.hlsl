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

sampler2D maskSampler : register(s0);

float4 MaskingPS() : Color
{
    return float4(1, 1, 1, 1);
}

HitboxVSOutput CombinedHitboxVS(HitboxVSInput input)
{
    HitboxVSOutput output;
    
    output.color = input.vDiffuse;
    output.position = input.vPosition;
    output.vTexCoor = input.vTexCoor;
    
    return output;
}

float4 CombinedHitboxPS(HitboxPSInput input) : Color
{
    float center = tex2D(maskSampler, input.vTexCoor).r;
    float2 uv = input.vTexCoor;
    float4 output = input.color;
    
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    float2 uvSizeInPixels;
    uvSizeInPixels.x = 1.0 / length(dx);
    uvSizeInPixels.y = 1.0 / length(dy);
    float2 borderUV = borderThickness / uvSizeInPixels;
    
    // Sampling
    float up    = tex2D(maskSampler, uv + float2(0, -borderUV.y)).r;
    float down  = tex2D(maskSampler, uv + float2(0,  borderUV.y)).r;
    float left  = tex2D(maskSampler, uv + float2(-borderUV.x, 0)).r;
    float right = tex2D(maskSampler, uv + float2( borderUV.x, 0)).r;

    float edge = center * (1.0 - up * down * left * right);
    
    if (edge > 0.0) output.a = 1.0;
    
    return center > 0.0 ? output : float4(0,0,0,0);
}

technique CombinedHitboxMask
{
    pass P0
    {
        VertexShader = compile vs_3_0 HitboxVS();
        PixelShader  = compile ps_3_0 MaskingPS();
    }
}
technique CombinedHitbox
{
    pass P0
    {
        VertexShader = compile vs_3_0 CombinedHitboxVS();
        PixelShader  = compile ps_3_0 CombinedHitboxPS();
    }
}