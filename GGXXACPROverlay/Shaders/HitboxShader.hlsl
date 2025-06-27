struct HitboxVSInput
{
    float4 vPosition : POSITION;
    float4 vDiffuse : COLOR0;
    float2 boxDim : TEXCOORD0;
    float2 vTexCoor : TEXCOORD1;
};
struct HitboxVSOutput
{
    float4 position : POSITION;
    float4 color : COLOR0;
    float2 boxDim : TEXCOORD0;
    float2 vTexCoor : TEXCOORD1;
};

uniform float4x4 mvpMatrix : register(c0);
uniform float2 viewPort : register(c4);


HitboxVSOutput HitboxVS(HitboxVSInput input)
{
    HitboxVSOutput output;
    
    output.position = mul(input.vPosition, mvpMatrix);
    //float2 ndc = output.position.xy / output.position.w;
    output.color = input.vDiffuse;
    output.vTexCoor = input.vTexCoor;
    output.boxDim = input.boxDim;
    //output.screenPos = (ndc * 0.5f + 0.5f) * viewPort;
    
    return output;
}

struct HitboxPSInput
{
    float4 color : COLOR0;
    float2 boxDim : TEXCOORD0;
    float2 vTexCoor : TEXCOORD1;
};

uniform float borderThickness : register(c5);

float4 HitboxPS(HitboxPSInput input) : COLOR
{
    float4 color = input.color;
    float2 uv = input.vTexCoor;
    
    if (borderThickness >= 0)
    {
        // subtracting small amount to account for float inaccuracy
        float2 uvEdgeThickness = (borderThickness - 0.001f) / input.boxDim;
    
        bool isEdge =
        uv.x < uvEdgeThickness.x ||
        uv.x > (1.0 - uvEdgeThickness.x) ||
        uv.y < uvEdgeThickness.y ||
        uv.y > (1.0 - uvEdgeThickness.y);
    
        color.a = isEdge ? 1.0 : color.a;
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