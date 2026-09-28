// Purpose: Core shared BRDF, indirect-light, and toon/PBR hybrid lighting helpers for Chara_V2.
// 文件名 + 后缀
#ifndef TOONBRDF_INCULDED
#define TOONBRDF_INCULDED

#define F0_CONST float3(0.04, 0.04, 0.04)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Chara_CommonHelpers_V2.hlsl"

#ifdef S_CHAR_TRANSPARENT
TEXTURE2D_X_FLOAT(_PerObjectTransparentDepthTexture);
SAMPLER(sampler_PerObjectTransparentDepthTexture);
#endif
// roughness 在每个公式里都要平方。
// 这里统一传入 roughnessSquare。
float DistributionGGX(float roughnessSquare, float NdotH)
{
    float b = NdotH * NdotH * (roughnessSquare - 1.0) + 1.0;
    float donum = PI * b * b;
    
    return roughnessSquare / donum;
}

//Gsub Schlick-GGX
/*
 * G: GeometrySmith
 * in:  NdotL, NdotV is max(dot, 0)
 *      kdirect = pow(roughness + 1, 2)/8;
 *      kIBL = pow(roughness, 2)/2
 */
float SchlickGGX(float roughness, float cosTheta)
{
    float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
    float num = cosTheta;
    float denom = cosTheta * (1.0 - k) + k;
    return num / (denom + 1e-5f);// 保证分母不为 0
}
//Gsub Schlick-GGX
float GeometrySmith(float roughness, float NdotV, float NdotL)
{
    return SchlickGGX(roughness, NdotL) * SchlickGGX(roughness, NdotV);
}

// 菲涅尔项是三维向量，原始版本。
// float3 FresnelTerm(float3 albedo, float metallic, float HdotV)
// {
//     
//     float3 F0 = F0_CONST * (1 - metallic) + albedo * metallic;// 金属高光颜色是 Albedo，非金属是 F0_CONST
//     float3 F = F0 + (1.0 - F0) * (1.0 - HdotV) * (1.0 - HdotV) * (1.0 - HdotV) * (1.0 - HdotV) * (1.0 - HdotV);
//     return F;
// }

float3 FresnelTerm(float3 F0, float HdotV)
{
    float3 F = F0 + (1 - F0) * exp2((-5.55473 * HdotV - 6.98316 ) * HdotV);
    return F;
}

// float3 FresnelSchlickRoughness(float3 albedo, float metallic, float NdotV, float roughness)// 这里沿用基于 NdotV 的版本
// {
//     float3 F0 = F0_CONST * (1 - metallic) + albedo * metallic;// 金属高光颜色是 Albedo，非金属是 F0_CONST
//     float3 F = F0 + (max(1.0f - roughness, F0) - F0) * (1.0 - NdotV) * (1.0 - NdotV) * (1.0 - NdotV) * (1.0 - NdotV) * (1.0 - NdotV);
//     return F;
// }

// UE4 优化版
float3 FresnelSchlickIndirect(float NdotV, float3 F0,float roughness)
{
    float3 F = exp2((-5.55473 * NdotV - 6.98316 ) * NdotV);
    return F0 + F * saturate(1 - roughness - F0);
}

half ModifyD_GGXaniso(half RoughnessT,half RoughnessB,half NoH,half ToH,half BoH)
{
    half aT = RoughnessT;
    half aB = RoughnessB;
    half d = ToH * ToH /(aT * aT) + BoH * BoH/(aB * aB) + NoH * NoH;
    return 1.0f/(aT* aB *d*d*PI+1e-3);
}
half Custom_V_SmithJointGGXAniso(half RoughnessT,half RoughnessB,half NoV,half NoL,half ToV,half ToL,half BoV,half BoL)
{
    half aT = RoughnessT;
    half aT2 = aT * aT;
    half aB = RoughnessB;
    half aB2 = aB * aB;

    half lambdaV = NoL *sqrt(aT2 * ToV * ToV + aB2 * BoV *BoV+NoV * NoV);
    half lambdaL = NoL *sqrt(aT2 * ToL * ToL + aB2 * BoL *BoL+NoL * NoL);
    return 0.5f/(lambdaV + lambdaL + 1e-3);
}
float3 Custom_F_Schlick (float3 SpecularColor, float VoH)
{
    float Fc = pow(1 - VoH, 5);
    return saturate(50.0 * SpecularColor.g) * Fc + (1-Fc) * SpecularColor;
}


// [Burley 2012, "Physically-Based Shading at Disney"]
float D_GGXaniso(float RoughnessT, float RoughnessB, float NoH, float TdotH, float BdotH)
{
    float a2 = RoughnessT * RoughnessB;
    float3 V = float3(RoughnessB * TdotH, RoughnessT * BdotH, a2 * NoH);
    float S = dot(V, V);

    return(1.0f / PI) * a2 * (a2 / S) * (a2 / S);
}

// [Heitz 2014, "Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs"]
float Vis_SmithJointAniso(float ax, float ay, float NoV, float NoL, float XoV, float XoL, float YoV, float YoL)
{
    float Vis_SmithV = NoL * length(float3(ax * XoV, ay * YoV, NoV));
    float Vis_SmithL = NoV * length(float3(ax * XoL, ay * YoL, NoL));
    return 0.5 * rcp(Vis_SmithV + Vis_SmithL);
}
// 这个函数里 x 对应 tangent，y 对应 bitangent，o 表示 dot 的结果。

// [Schlick 1994, "An Inexpensive BRDF Model for Physically-Based Rendering"]
float3 F_Schlick_UE4(float3 SpecularColor, float VoH)
{
    float Fc = (1 - VoH) * (1 - VoH) * (1 - VoH) * (1 - VoH) * (1 - VoH);					// 1 sub, 3 mul
    //return Fc + (1 - Fc) * SpecularColor;		// 1 add, 3 mad
	
    // Anything less than 2% is physically impossible and is instead considered to be shadowing
    return saturate(50.0 * SpecularColor.g) * Fc + (1 - Fc) * SpecularColor;
}


// 这里只返回高光项。
float3 SlikBRDF(float3 DiffuseColor, float3 SpecularColor, float Roughness, float Anisotropy,
float3 N, float3 T, float3 B, float3 V, float3 L, float3 LightColor, float Shadow)
{
    float Alpha = Roughness * Roughness;
    float a2 = Alpha * Alpha;
    // Anisotropic parameters: ax and ay are the Roughness along the tangent and bitangent
    // Kulla 2017, "Revisiting Physically Based Shading at Imageworks"
    float ax = max(Alpha * (1.0 + Anisotropy), 0.001f);
    float ay = max(Alpha * (1.0 - Anisotropy), 0.001f);
    float3 H = normalize(L + V);
    float NoH = saturate(dot(N, H));
    float NoV = saturate(abs(dot(N, V)) + 1e-5);
    float NoL = saturate(dot(N, L));
    float VoH = saturate(dot(V, H));

    float XoV = dot(T, V);
    float XoL = dot(T, L);
    float XoH = dot(T, H);
    float YoV = dot(B, V);
    float YoL = dot(B, L);
    float YoH = dot(B, H);

    float3 Radiance = NoL * LightColor * Shadow * PI;
	
    // 直接光漫反射
    // float3 DiffuseTerm = Diffuse_Lambert(DiffuseColor) * Radiance;

    // 直接光镜面反射
    float D = D_GGXaniso(ax, ay, NoH, XoH, YoH);
    float Vis = Vis_SmithJointAniso(ax, ay, NoV, NoL, XoV, XoL, YoV, YoL);
    float3 F = F_Schlick_UE4(SpecularColor, VoH);
    float3 SpecularTerm = ((D * Vis) * F) * Radiance;

    return SpecularTerm;
}

// float3 DirectLighting(float3 DiffuseColor, float3 SpecularColor, float Roughness, float3 WorldPos, float Anisotropy, 
//     float3 N, float3 T, float3 B, float3 V, float4 shadowCoord, float4 shadowMask)
// {
//     //涓诲厜婧?
//     half3 DirectLighting_MainLight = half3(0, 0, 0);
//     {
//         Light light = GetMainLight(shadowCoord, WorldPos, shadowMask);
//         half3 L = light.direction;
//         half3 LightColor = light.color;
//         half Shadow = light.shadowAttenuation;
//         DirectLighting_MainLight = SlikBRDF(DiffuseColor, SpecularColor, Roughness, Anisotropy, N, T, B, V, L, LightColor, Shadow);
//     }
//     // 附加光源
//     half3 DirectLighting_AddLight = half3(0, 0, 0);
//     #ifdef _ADDITIONAL_LIGHTS
//     uint pixelLightCount = GetAdditionalLightsCount();
//     for (uint lightIndex = 0; lightIndex < pixelLightCount; ++lightIndex)
//     {
//         Light light = GetAdditionalLight(lightIndex, WorldPos, shadowMask);
//         half3 L = light.direction;
//         half3 LightColor = light.color;
//         half Shadow = light.shadowAttenuation * light.distanceAttenuation;
//         DirectLighting_AddLight += SlikBRDF(DiffuseColor, SpecularColor, Roughness, Anisotropy, N, T, B, V, L, LightColor, Shadow);
//     }
//     #endif
//
//     return DirectLighting_MainLight + DirectLighting_AddLight;
// }

// float3 IndirectLighting(float3 DiffuseColor, float3 SpecularColor, float Roughness, float3 WorldPos, float Anisotropy, 
// float3 N, float3 T, float3 B, float3 V, float Occlusion, float EnvRotation)
// {
//     float NoV = saturate(abs(dot(N, V)) + 1e-5);
//     //SH
//     float3 DiffuseAO = AOMultiBounce(DiffuseColor, Occlusion);
//     float3 RadianceSH = SampleSH(N);
//     float3 IndirectDiffuse = RadianceSH * DiffuseColor * DiffuseAO;
// 	
//     // 根据各项异性强度扭曲法向，实现各项异性的环境高光
//     float3 anisotropicDirection = Anisotropy >= 0.0 ? B : T;
//     float3 anisotropicTangent = cross(anisotropicDirection, V);
//     float3 anisotropicNormal = cross(anisotropicTangent, anisotropicDirection);
//     float3 bentNormal = normalize(lerp(N, anisotropicNormal, abs(Anisotropy)));
//
//     //IBL
//     half3 R = reflect(-V, bentNormal);
//     R = RotateDirection(R, EnvRotation);
//     half3 SpeucularLD = GlossyEnvironmentReflection(R, WorldPos, Roughness, Occlusion);
//     half3 SpecularDFG = EnvBRDFApprox(SpecularColor, Roughness, NoV);
//     float SpecularOcclusion = GetSpecularOcclusion(NoV, Pow2(Roughness), Occlusion);
//     float3 SpecularAO = AOMultiBounce(SpecularColor, SpecularOcclusion);
//     float3 IndirectSpecular = SpeucularLD * SpecularDFG * SpecularAO;
//
//     return IndirectDiffuse + IndirectSpecular;
// }


float DG_SmithJointGGXAniso (float TH, float BH, float NH, float TV, float BV, float NV, float TL, float BL, float NL, float roughness)
{
    float a2 = roughness * roughness;
    float3 v = float3(roughness * TH, roughness * BH, a2 * NH);
    float s = dot(v, v);
    float lambdaV = NL * length(float3(roughness * TV, roughness * BV, NV));
    float lambdaL = NV * length(float3(roughness * TL, roughness * BL, NL));
    float2 D = float2(a2 * a2 * a2, s * s) * INV_PI;
    float2 G = float2(1, lambdaV + lambdaL) * 0.5;
    return D.x * G.x / max(D.y * G.y, 1e-7);
}

float3 EvaluateHairAdditionalLightBRDF(
    Light light,
    float3 normalWS,
    float3 viewDir,
    float3 albedo,
    float roughness,
    float roughnessSquare,
    float3 F0,
    float NdotV,
    float oneMinusReflectivity)
{
    float3 lightDirAdd = SafeNormalize(light.direction);
    float3 halfDirAdd = SafeNormalize(lightDirAdd + viewDir);
    float NdotLAdd = saturate(dot(normalWS, lightDirAdd));
    float NdotHAdd = saturate(dot(normalWS, halfDirAdd));
    float HdotVAdd = saturate(dot(halfDirAdd, viewDir));

    float GTermAdd = GeometrySmith(roughness, NdotV, NdotLAdd);
    float3 FTermAdd = FresnelTerm(F0, HdotVAdd);
    float DTermAdd = DistributionGGX(roughnessSquare, NdotHAdd);
    float denomAdd = max(4 * NdotLAdd * NdotV, 0.001f);

    float3 addDiffColor = albedo * oneMinusReflectivity;
    float3 addSpecColor = PI * DTermAdd * GTermAdd * FTermAdd / denomAdd;
    return (addDiffColor + addSpecColor) * light.color * light.distanceAttenuation * light.shadowAttenuation * NdotLAdd;
}

float3 IndirectDiffuse(float3 NormalWS, float3 F0, float roughness, float NdotV, float metallic, float3 albedo, float occlusion, float4 customEnvData)
{
    float3 SHNormal = NormalWS;
    float3 irradiance = SampleSH(SHNormal);
    float3 envColor = customEnvData.a > 0.5 ? customEnvData.rgb : irradiance;
    float3 indirF = FresnelSchlickIndirect(NdotV, F0, roughness);
    float3 indirKd = (1 - indirF) * (1 - metallic);
    float3 indirect = indirKd * envColor * albedo * occlusion;
    return indirect;
}

float3 IndirectDiffuse(float3 NormalWS, float3 F0, float roughness, float NdotV, float metallic, float3 albedo, float occlusion)
{
    float3 SHNormal = NormalWS;
    float3 irradiance = SampleSH(SHNormal);
    float3 envColor = irradiance;
    float3 indirF = FresnelSchlickIndirect(NdotV, F0, roughness);
    float3 indirKd = (1 - indirF) * (1 - metallic);
    float3 indirect = indirKd * envColor * albedo * occlusion;
    return indirect;
}

float3 IndirectSpecCube(float perceptualRoughness, float occlusion, float3 vDir, float3 NormalWS)
{
    float3 rDir = reflect(-vDir, NormalWS);
    return GlossyEnvironmentReflection(rDir, perceptualRoughness, occlusion);
    
}

// 间接高光改用曲线拟合，放弃 LUT 采样，思路类似 envBRDF。
float3 IndirectSpecFactor(float roughnessSquare, float smoothness,float metallic, float3 F0, float NdotV)
{
    float surfaceReduction = 1.0 / (roughnessSquare + 1.0);

    half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
    half reflectivity = half(1.0) - oneMinusReflectivity;
    // #if defined(SHADER_API_GLES)//Lighting.hlsl 261琛?
    // float reflectivity = BRDFSpec.x;
    // #else
    // float reflectivity = max(max(BRDFSpec.x,BRDFSpec.y),BRDFSpec.z);
    // #endif
    
    half grazingTerm = saturate(smoothness + reflectivity);
    float fresnelTerm = Pow4(1.0 - NdotV);

    return  surfaceReduction * lerp(F0, grazingTerm, fresnelTerm);
}

#endif // TOONBRDF_INCULDED

