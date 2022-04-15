#include "DeferredRendering.fxh"

// Gbuffer Pass

GBufferVSO GbufferVS(in GBufferVSI input)
{
    GBufferVSO output = (GBufferVSO) 0;
    
    float4 pos = mul(input.Position, WorldViewProjection);
    output.Position = pos;
    //output.PositionVS = pos;
    output.Normal = mul(input.Normal, InverseTransposeWorld);
    output.TexCoord = input.TexCoord;
    // output.Depth = mul(input.Position, WorldView).z / -FarPlaneDistance;
    output.PositionVS = mul(input.Position, World);
    return output;
}

GBufferPSO TexturePS(GBufferVSO input)
{
    GBufferPSO output = (GBufferPSO) 0;
    
    float3 color = tex2D(textureSampler, input.TexCoord).rgb;
    
    output.Color = float4(color, 1);
    
    output.Normal = float4((input.Normal.xyz + 1) * 0.5,1);

    output.Bloom = float4(0, 0, 0, 1);
    
    //output.Depth.rgb = ((input.PositionVS.xyz / 65535) * 2.0) - 1.0;
    //output.Depth.a = 1.0;
    
    return output;
}

GBufferPSO FlatPS(GBufferVSO input)
{
    GBufferPSO output = (GBufferPSO) 0;
    
    output.Color = float4(Color, 1);
    
    output.Normal = float4((input.Normal.xyz + 1) * 0.5, 1);

    output.Bloom = float4(0, 0, 0, 1);
    
    //output.Depth.r = input.Depth;
    
    return output;
}

GBufferPSO LightSpherePS(GBufferVSO input)
{
    GBufferPSO output = (GBufferPSO) 0;
    
    output.Color = float4(Color, 1);
    
    
    output.Normal = float4((input.Normal.xyz + 1) * 0.5, 0);
    
    output.Bloom = float4(Color, 1);
    
    //output.Depth.rgb = ((input.PositionVS.xyz / 65535) * 2.0) - 1.0;
    //output.Depth.a = 1.0;
    
    return output;
}


//Post processing
GBufferVSO PosEncodeVS(PostVSI input)
{
    GBufferVSO output = (GBufferVSO) 0;
    output.Position = mul(input.Position, WorldViewProjection);
    output.PositionVS = mul(input.Position, World);
    return output;
}
PostPSO PosEncodePS(GBufferVSO input)
{
    PostPSO output = (PostPSO) 0;
    //[-32767,32768] -> [-1,1] -> [0,2] -> [0-1]
    output.Color.r = ((input.PositionVS.x / 32767) + 1.0) * 0.5;
    output.BlurH.r = ((input.PositionVS.y / 32767) + 1.0) * 0.5;
    output.BlurV.r = ((input.PositionVS.z / 32767) + 1.0) * 0.5;
    
    return output;
    
}
float4 PosReconstructPS(PostVSO input) : COLOR
{
    float4 output = float4(0,0,0,0);
    
    float3 color = tex2D(colorSampler, input.TexCoord).rgb;
    float4 ns = tex2D(normalMapSampler, input.TexCoord);
    float3 normal = ns.xyz;
    float applyLight = ns.a;
    
    float3 pos = float3(tex2D(posXSampler, input.TexCoord).r,
                        tex2D(posYSampler, input.TexCoord).r,
                        tex2D(posZSampler, input.TexCoord).r);
    //[0-1] -> [0,2] -> [-1,1] -> [-32767,32768]
    pos = (pos* 2.0 - 1.0) * 32767;
    
    
    output.a = 1;
    if(applyLight == 1.0)
        output.rgb = color * calculateOmniLights(pos, normal);
    else
        output.rgb = color;
    return output;
}
PostVSO PostProcessVS(PostVSI input)
{
    PostVSO output = (PostVSO) 0;
    output.Position = float4(input.Position.xyz, 1);
    output.TexCoord = input.TexCoord;
    return output;
}
PostPSO PostProcessPS(PostVSO input)
{
    PostPSO output = (PostPSO) 0;
    
    float4 normalMap = tex2D(normalMapSampler, input.TexCoord);
    float4 colorMap = tex2D(colorSampler, input.TexCoord);
    //float4 positionMap = tex2D(positionSampler, input.TexCoord);
    float4 bloomMap = tex2D(bloomSampler, input.TexCoord);
       
    
    float3 normal = (normalMap.xyz * 2) - 1;
    float applyLighting = bloomMap.w;
    
    float3 color = colorMap.rgb;
    //float KD = colorMap.a;
    
    float depth = tex2D(depthMapSampler, input.TexCoord).r;
    float3 position;
    float4 pos;
    pos.x = input.TexCoord.x * 2.0 - 1.0;
    pos.y = -(input.TexCoord.y * 2.0 - 1.0);
    pos.z = depth;
    pos.w = 1.0;
    
    //pos = mul(pos, InvViewProjection);
    pos /= pos.w;
    
    position = pos.xyz;
    
    float KD = 0.8;
    float KS = 0.8;
    float specularPower = 10;
    
    if (applyLighting == 0.0)
    {
        output.Color = float4(color, 1);
    }
    else
    {
        // Base vectors
        float3 viewDirection = normalize(CameraPosition - position);
        float3 halfVector = normalize(DLightDirection + viewDirection);

	
	    // Calculate the diffuse light
        float NdotL = saturate(dot(normal, DLightDirection));
        float3 diffuseLight = KD * DLightColor * NdotL;

	    // Calculate the specular light
        float NdotH = dot(normal.xyz, halfVector);
        float3 specularLight = sign(NdotL) * KS * SpecularColor * pow(saturate(NdotH), specularPower);
    
        // Final calculation
        output.Color = float4(saturate(AmbientColor * KA + diffuseLight) * color + specularLight, 1);
        
        output.BlurH = float4(1, 0, 0, 1);
        output.BlurV = float4(0, 0, 1, 1);
    }
    
    
    // Calculate horizontal and vertical blur for the bloom filter
    float4 hColor = float4(0, 0, 0, 1);
    float4 vColor = float4(0, 0, 0, 1);
        
        
    for (int i = 0; i < kernel_size; i++)
    {
        float2 scaledTextureCoordinatesH = input.TexCoord + float2((float) (i - kernel_r) / ScreenSize.x, 0);
        float2 scaledTextureCoordinatesV = input.TexCoord + float2(0, (float) (i - kernel_r) / ScreenSize.y);
        hColor += tex2D(bloomSampler, scaledTextureCoordinatesH) * Kernel[i];
        vColor += tex2D(bloomSampler, scaledTextureCoordinatesV) * Kernel[i];
    }
    
    output.BlurH = hColor;
    output.BlurV = vColor;


    return output;
}


float4 IntPS(PostVSO input) : COLOR
{
    float3 color = tex2D(colorSampler, input.TexCoord).rgb;
    float3 posSamp = tex2D(depthMapSampler, input.TexCoord).rgb;
    
    float3 worldPos = ((posSamp + 1) / 2) * 65536;
    
    float dist = distance(worldPos, float3(0.0, 0.0, 0.0));
    
    float res = step(20, dist);
    
    /*    
    float depth = tex2D(depthMapSampler, input.TexCoord).r;
    
    float z = 2.0 * depth - 1.0;
    
    float4 clipSpacePos = float4(input.TexCoord * 2.0 - 1.0, z, 1.0);
    float4 viewSpacePos = mul(InvProjection, clipSpacePos);
    //perspective div
    viewSpacePos /= viewSpacePos.w;
    
    float4 worldSpacePos = mul(InvView, viewSpacePos);
    
    
    */
    float3 light = float3(res, 0, 0);
    float3 blend = color *0.5 + light * 0.5;
    
    return float4(blend,1.0);
    
}


technique PostProcess
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL PostProcessVS();
        PixelShader = compile PS_SHADERMODEL PostProcessPS();
    }
};
technique Intermediate
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL PostProcessVS();
        PixelShader = compile PS_SHADERMODEL IntPS();
    }
};

technique PosEncode
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL PosEncodeVS();
        PixelShader = compile PS_SHADERMODEL PosEncodePS();
    }
};
technique PosReconstruct
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL PostProcessVS();
        PixelShader = compile PS_SHADERMODEL PosReconstructPS();
    }
};


// Final Integration
float4 FinalIntegratePS(PostVSO input) : COLOR
{
    float4 blurHColor = float4(tex2D(blurHSampler, input.TexCoord).rgb, 1);
    float4 blurVColor = float4(tex2D(blurVSampler, input.TexCoord).rgb, 1);
    
    float4 sceneColor = float4(tex2D(sceneSampler, input.TexCoord).rgb, 1);
     
    return sceneColor * 0.8 + blurHColor * 1.0 + blurVColor * 1.0;
    
    //return sceneColor;
}

technique FinalIntegrate
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL PostProcessVS();
        PixelShader = compile PS_SHADERMODEL FinalIntegratePS();
    }
};
