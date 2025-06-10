struct VertexShaderInput
{
    float4 vPosition : POSITION;
    float4 vDiffuse : COLOR0;
};
struct VertexShaderOutput
{
    float4 position : POSITION;
    float4 color : COLOR0;
};


VertexShaderOutput ColorVertexShader(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    output.position = input.vPosition;
    output.color = input.vDiffuse;
    
    return output;
}

struct PixelShaderInput
{
    float4 color : COLOR0;
};

float4 ColorPixelShader(PixelShaderInput input) : COLOR0
{
    return input.color;
}

float4 g_SolidColor : register(c0);
float4 SolidColorPixelShader() : COLOR0
{
    return g_SolidColor;
}
