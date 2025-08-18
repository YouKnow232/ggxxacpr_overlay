struct VertexShaderData
{
    float4 vPosition : POSITION;
    float4 vDiffuse : COLOR0;
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

technique
{
    pass P0
    {
        VertexShader = compile vs_3_0 BasicVS();
        PixelShader  = compile vs_3_0 BasicPS();
    }
}