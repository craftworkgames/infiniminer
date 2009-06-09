struct VertexToPixel
{
    float4 Position : POSITION;
    float4 Color : COLOR0;   
    float LightingFactor: TEXCOORD0; 
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

//------- XNA-to-HLSL variables --------
float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float4	 xColor;

//------- Technique: Block --------

VertexToPixel BlockVS( float4 inPos : POSITION, float2 inTexCoords: TEXCOORD0, float inShade: TEXCOORD1)
{	
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);	
	Output.Color = xColor;
	Output.LightingFactor = inShade;
	
	return Output;    
}

PixelToFrame BlockPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	Output.Color = PSIn.Color;
	Output.Color.rgb *= saturate(PSIn.LightingFactor);

	return Output;
}

technique Projectile
{
	pass Pass0
	{   
		VertexShader = compile vs_2_0 BlockVS();
		PixelShader  = compile ps_2_0 BlockPS();
	}
}
