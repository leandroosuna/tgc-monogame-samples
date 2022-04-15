#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

//Uniforms
float4x4 World;
float4x4 WorldView;
float4x4 WorldViewProjection;
float4x4 InverseTransposeWorld;
float4x4 View;
float4x4 InvView;
float4x4 InvProjection;
float FarPlaneDistance;
float3 CameraPosition;

float Time;
float3 Color;

float KD;
float KS;

static const int kernel_r = 6;
static const int kernel_size = 13;
static const float Kernel[kernel_size] =
{
    0.002216, 0.008764, 0.026995, 0.064759, 0.120985, 0.176033, 0.199471, 0.176033, 0.120985, 0.064759, 0.026995, 0.008764, 0.002216,
};
float2 ScreenSize;
float3 AmbientColor;
float3 KA;
float3 DLightDirection;
float3 DLightColor;
float3 SpecularColor;

float3 Light1Pos;

float3 LightsColor[2];

struct GBufferVSI
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};
struct GBufferVSO
{
    float4 Position : SV_POSITION;
    float4 Normal : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
    float4 PositionVS : TEXCOORD2;
};
struct GBufferPSO
{
    float4 Color : COLOR0;
    float4 Normal : COLOR1;
    float4 Bloom : COLOR2;
    float4 Depth : COLOR3;
};

struct PostVSI
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};
struct PostVSO
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

struct PostPSO
{
    float4 Color : COLOR0;
    float4 BlurH : COLOR1;
    float4 BlurV : COLOR2;
};

texture ModelTexture;
sampler2D textureSampler = sampler_state
{
    Texture = (ModelTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};


texture ColorMap;
sampler colorSampler = sampler_state
{
    Texture = (ColorMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture DepthMap;
sampler depthMapSampler = sampler_state
{
    Texture = (DepthMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture PosXMap;
sampler posXSampler = sampler_state
{
    Texture = (PosXMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture PosYMap;
sampler posYSampler = sampler_state
{
    Texture = (PosYMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture PosZMap;
sampler posZSampler = sampler_state
{
    Texture = (PosZMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture NormalMap;
sampler normalMapSampler = sampler_state
{
    Texture = (NormalMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;
};

texture BloomFilter;
sampler bloomSampler = sampler_state
{
    Texture = (BloomFilter);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;
};


texture BlurHTexture;
sampler2D blurHSampler = sampler_state
{
    Texture = (BlurHTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
texture BlurVTexture;
sampler2D blurVSampler = sampler_state
{
    Texture = (BlurVTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
texture SceneTexture;
sampler2D sceneSampler = sampler_state
{
    Texture = (SceneTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

technique Textured
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL GbufferVS();
        PixelShader = compile PS_SHADERMODEL TexturePS();
    }
};

technique FlatColor
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL GbufferVS();
        PixelShader = compile PS_SHADERMODEL FlatPS();
    }
};

technique LightSphere
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL GbufferVS();
        PixelShader = compile PS_SHADERMODEL LightSpherePS();
    }
};



float3 calculateOmniLights(float3 pos, float3 normal)
{
    float3 output = float3(0,0,0);
    
    float3 dir1 = Light1Pos - pos;
    float3 len1 = length(dir1);
    float3 ld1 = normalize(dir1);
    
    float ndl1 = saturate(dot(normal, ld1));
    
    float intensity = 1 - smoothstep(0, 30, len1);
    
    float3 l1 = intensity * float3(1, 0, 1) * ndl1;
    
    output = saturate(float3(0.15, 0.15, 0.15) + l1);
    
    return output;
}