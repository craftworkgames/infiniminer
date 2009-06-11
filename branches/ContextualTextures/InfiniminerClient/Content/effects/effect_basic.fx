struct VertexToPixel
{
    float4 Position   	: POSITION;    
    float4 Color		: COLOR0;
    float2 SpecialInfo  : TEXCOORD0;  // .x == lighting factor, .y == depth
    float2 TextureCoords: TEXCOORD1;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

//------- XNA-to-HLSL variables --------
float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float3	 xLODColor;
float	 xTime;

//------- Texture Samplers --------
Texture xTexture;
sampler TextureSampler = sampler_state
{
	texture = <xTexture>;
	magfilter = POINT;
	minfilter = ANISOTROPIC;
	mipfilter = NONE;
	AddressU = WRAP;
	AddressV = WRAP;
};

//------- Technique: Block --------

#define FADE_DISTANCE 64

VertexToPixel BlockVS( float4 inPos : POSITION, float2 inTexCoords: TEXCOORD0, float inShade: TEXCOORD1)
{	
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);	
	Output.TextureCoords = inTexCoords;
	Output.SpecialInfo.x = inShade;
	Output.SpecialInfo.y = clamp(Output.Position.z,0,FADE_DISTANCE) / FADE_DISTANCE;
	
	return Output;    
}

PixelToFrame BlockPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	float4 texColor = tex2D(TextureSampler, PSIn.TextureCoords);
	Output.Color.rgb = lerp(texColor.rgb, xLODColor, PSIn.SpecialInfo.y);
	Output.Color.rgb *= PSIn.SpecialInfo.x;
	Output.Color.a = texColor.a;
	
	return Output;
}

technique Block
{
	pass Pass0
	{   
		VertexShader = compile vs_2_0 BlockVS();
		PixelShader  = compile ps_2_0 BlockPS();
	}
}

VertexToPixel LavaBlockVS( float4 inPos : POSITION, float2 inTexCoords: TEXCOORD0, float inShade: TEXCOORD1)
{	
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);	
	Output.TextureCoords = inTexCoords;
	Output.TextureCoords.y -= xTime / 5;
	Output.SpecialInfo.y = clamp(Output.Position.z,0,FADE_DISTANCE) / FADE_DISTANCE;
	
	return Output;    
}

PixelToFrame LavaBlockPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	float3 texColor = tex2D(TextureSampler, PSIn.TextureCoords);
	Output.Color.rgb = lerp(texColor, xLODColor, PSIn.SpecialInfo.y) * 1.2;
	
	return Output;
}

technique LavaBlock
{
	pass Pass0
	{   
		VertexShader = compile vs_2_0 LavaBlockVS();
		PixelShader  = compile ps_2_0 LavaBlockPS();
	}
}
