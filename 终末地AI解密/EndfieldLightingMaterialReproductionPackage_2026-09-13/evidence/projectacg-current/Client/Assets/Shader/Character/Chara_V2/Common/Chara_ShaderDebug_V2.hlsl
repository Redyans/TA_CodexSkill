// Purpose: Debug output selection helpers for Chara_V2 material and lighting inspection.
#ifndef CHARA_SHADER_DEBUG_INCLUDED
#define CHARA_SHADER_DEBUG_INCLUDED

float _CharacterShaderDebugMode;

#define CHARA_SHADER_DEBUG_OFF 0
#define CHARA_SHADER_DEBUG_BASE_COLOR 1
#define CHARA_SHADER_DEBUG_NORMAL_WS 2
#define CHARA_SHADER_DEBUG_METALLIC 3
#define CHARA_SHADER_DEBUG_ROUGHNESS 4
#define CHARA_SHADER_DEBUG_OCCLUSION 5
#define CHARA_SHADER_DEBUG_DIRECT_DIFFUSE 6
#define CHARA_SHADER_DEBUG_DIRECT_SPECULAR 7
#define CHARA_SHADER_DEBUG_INDIRECT_DIFFUSE 8
#define CHARA_SHADER_DEBUG_INDIRECT_SPECULAR 9
#define CHARA_SHADER_DEBUG_FINAL_GRAYSCALE 10
#define CHARA_SHADER_DEBUG_SMOOTH_NORMAL_VERTEX_COLOR 11
#define CHARA_SHADER_DEBUG_RAW_METALLIC 12
#define CHARA_SHADER_DEBUG_RAW_ROUGHNESS 13
#define CHARA_SHADER_DEBUG_RAW_OCCLUSION 14
#define CHARA_SHADER_DEBUG_NORMAL_TS_ADJUSTED 15
#define CHARA_SHADER_DEBUG_NORMAL_TS_RAW 16
#define CHARA_SHADER_DEBUG_RAW_SMOOTHNESS 17
#define CHARA_SHADER_DEBUG_SHADOW_ATTENUATION 21
#define CHARA_SHADER_DEBUG_SCENE_SHADOW 22
#define CHARA_SHADER_DEBUG_FINAL_COLOR_GRADIENT 18

bool IsCharaShaderDebugEnabled()
{
    return _CharacterShaderDebugMode > 0.5;
}

bool IsCharaShaderMaterialDebugEnabled()
{
    int debugMode = (int)round(_CharacterShaderDebugMode);
    return (debugMode > CHARA_SHADER_DEBUG_OFF && debugMode < CHARA_SHADER_DEBUG_OCCLUSION)
        || debugMode == CHARA_SHADER_DEBUG_SMOOTH_NORMAL_VERTEX_COLOR
        || (debugMode >= CHARA_SHADER_DEBUG_RAW_METALLIC && debugMode <= CHARA_SHADER_DEBUG_RAW_OCCLUSION)
        || debugMode == CHARA_SHADER_DEBUG_NORMAL_TS_ADJUSTED
        || debugMode == CHARA_SHADER_DEBUG_NORMAL_TS_RAW
        || debugMode == CHARA_SHADER_DEBUG_RAW_SMOOTHNESS;
}

half4 GetCharaShaderDebugColor(
    float3 albedo,
    float3 normalWS,
    float3 normalTSRaw,
    float3 normalTSAdjusted,
    float metallic,
    float roughness,
    float3 rawMra,
    float3 smoothNormalVertexColor,
    float alpha)
{
    int debugMode = (int)round(_CharacterShaderDebugMode);

    if (debugMode == CHARA_SHADER_DEBUG_BASE_COLOR)
    {
        return half4(saturate(albedo), alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_NORMAL_WS)
    {
        float3 debugNormal = SafeNormalize(normalWS) * 0.5 + 0.5;
        return half4(saturate(debugNormal), alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_NORMAL_TS_RAW)
    {
        float3 debugNormal = SafeNormalize(normalTSRaw) * 0.5 + 0.5;
        return half4(saturate(debugNormal), alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_NORMAL_TS_ADJUSTED)
    {
        float3 debugNormal = SafeNormalize(normalTSAdjusted) * 0.5 + 0.5;
        return half4(saturate(debugNormal), alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_METALLIC)
    {
        return half4(saturate(metallic).xxx, alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_ROUGHNESS)
    {
        return half4(saturate(roughness).xxx, alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_SMOOTH_NORMAL_VERTEX_COLOR)
    {
        return half4(saturate(smoothNormalVertexColor), alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_RAW_METALLIC)
    {
        return half4(saturate(rawMra.r).xxx, alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_RAW_SMOOTHNESS)
    {
        return half4(saturate(rawMra.g).xxx, alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_RAW_ROUGHNESS)
    {
        return half4(saturate(1.0 - rawMra.g).xxx, alpha);
    }

    if (debugMode == CHARA_SHADER_DEBUG_RAW_OCCLUSION)
    {
        return half4(saturate(rawMra.b).xxx, alpha);
    }

    return half4(0, 0, 0, alpha);
}

half4 GetCharaShaderDebugColor(
    float3 albedo,
    float3 normalWS,
    float3 normalTSRaw,
    float3 normalTSAdjusted,
    float metallic,
    float roughness,
    float3 smoothNormalVertexColor,
    float alpha)
{
    return GetCharaShaderDebugColor(
        albedo,
        normalWS,
        normalTSRaw,
        normalTSAdjusted,
        metallic,
        roughness,
        float3(0.0, 1.0, 1.0),
        smoothNormalVertexColor,
        alpha);
}

half4 GetCharaShaderDebugColor(
    float3 albedo,
    float3 normalWS,
    float3 normalTS,
    float metallic,
    float roughness,
    float3 rawMra,
    float3 smoothNormalVertexColor,
    float alpha)
{
    return GetCharaShaderDebugColor(
        albedo,
        normalWS,
        normalTS,
        normalTS,
        metallic,
        roughness,
        rawMra,
        smoothNormalVertexColor,
        alpha);
}

half4 GetCharaShaderDebugColor(
    float3 albedo,
    float3 normalWS,
    float3 normalTS,
    float metallic,
    float roughness,
    float3 smoothNormalVertexColor,
    float alpha)
{
    return GetCharaShaderDebugColor(
        albedo,
        normalWS,
        normalTS,
        normalTS,
        metallic,
        roughness,
        float3(0.0, 1.0, 1.0),
        smoothNormalVertexColor,
        alpha);
}

half4 ApplyCharaShaderFinalDebug(float3 finalColor, float alpha)
{
    int debugMode = (int)round(_CharacterShaderDebugMode);

    if (debugMode == CHARA_SHADER_DEBUG_FINAL_GRAYSCALE)
    {
        float luminance = dot(finalColor, float3(0.2126, 0.7152, 0.0722));
        return half4(luminance.xxx, alpha);
    }

    return half4(finalColor, alpha);
}

half4 ApplyCharaShaderFinalDebug(float3 finalColor, float3 finalColorGradient, float alpha)
{
    int debugMode = (int)round(_CharacterShaderDebugMode);

    if (debugMode == CHARA_SHADER_DEBUG_FINAL_COLOR_GRADIENT)
    {
        return half4(finalColorGradient, alpha);
    }

    return ApplyCharaShaderFinalDebug(finalColor, alpha);
}

bool TryApplyCharaShaderLightingDebug(
    out half4 debugColor,
    float3 directDiffuse,
    float3 directSpecular,
    float3 indirectDiffuse,
    float3 indirectSpecular,
    float3 occlusion,
    float shadowAttenuation,
    float sceneShadow,
    float alpha)
{
    int debugMode = (int)round(_CharacterShaderDebugMode);

    if (debugMode == CHARA_SHADER_DEBUG_DIRECT_DIFFUSE)
    {
        debugColor = half4(directDiffuse, alpha);
        return true;
    }

    if (debugMode == CHARA_SHADER_DEBUG_DIRECT_SPECULAR)
    {
        debugColor = half4(directSpecular, alpha);
        return true;
    }

    if (debugMode == CHARA_SHADER_DEBUG_INDIRECT_DIFFUSE)
    {
        debugColor = half4(indirectDiffuse, alpha);
        return true;
    }

    if (debugMode == CHARA_SHADER_DEBUG_INDIRECT_SPECULAR)
    {
        debugColor = half4(indirectSpecular, alpha);
        return true;
    }

    if (debugMode == CHARA_SHADER_DEBUG_OCCLUSION)
    {
        debugColor = half4(occlusion, alpha);
        return true;
    }

    if (debugMode == CHARA_SHADER_DEBUG_SHADOW_ATTENUATION)
    {
        debugColor = half4(saturate(shadowAttenuation).xxx, alpha);
        return true;
    }

    if (debugMode == CHARA_SHADER_DEBUG_SCENE_SHADOW)
    {
        debugColor = half4(saturate(sceneShadow).xxx, alpha);
        return true;
    }

    debugColor = half4(0, 0, 0, alpha);
    return false;
}

#endif
