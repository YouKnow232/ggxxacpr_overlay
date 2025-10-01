struct VertexShaderData
{
    float4 vPosition : POSITION;
    float4 vDiffuse : COLOR0;
    float2 vTexCoor : TEXCOORD0;
};

uniform float4x4 mvpMatrix : register(c0);
uniform float2 viewPort : register(c4);

VertexShaderData BasicVS(VertexShaderData input)
{
    VertexShaderData output;
    
    output.vPosition = mul(input.vPosition, mvpMatrix);
    // D3D9 Half-pixel offset correction
    output.vPosition.xy -= float2(0.5, 0.5) * output.vPosition.w / viewPort;
    output.vDiffuse = input.vDiffuse;
    output.vTexCoor = input.vTexCoor;
    
    return output;
}

struct PixelShaderInput
{
    float4 color : COLOR0;
};

float4 BasicPS(PixelShaderInput input) : COLOR
{
    return input.color;
}


struct TextShaderInput
{
    float4 color : COLOR0;
    float2 texCoor : TEXCOORD0;
};

sampler2D mySampler : register(s0);

float4 TextPS(TextShaderInput input) : COLOR
{
    float4 sample = tex2D(mySampler, input.texCoor);
    sample.rgb = sample.rgb * (1 - input.color.a) + input.color.rgb * input.color.a;

    return sample;
}


technique basic
{
    pass P0
    {
        VertexShader = compile vs_3_0 BasicVS();
        PixelShader  = compile vs_3_0 BasicPS();
    }
}

technique text
{
    pass P0
    {
        VertexShader = compile vs_3_0 BasicVS();
        PixelShader  = compile ps_3_0 TextPS();
    }
}