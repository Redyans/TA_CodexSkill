#ifndef MONST_V2_CRYSTAL_FEATURE_INCLUDED
#define MONST_V2_CRYSTAL_FEATURE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

void CrystalRotateHueDegrees(float3 inputColor, float offsetDegrees, out float3 outputColor)
{
    float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(inputColor.bg, k.wz), float4(inputColor.gb, k.xy), step(inputColor.b, inputColor.g));
    float4 q = lerp(float4(p.xyw, inputColor.r), float4(inputColor.r, p.yzx), step(p.x, inputColor.r));
    float d = q.x - min(q.w, q.y);
    float e = 1e-10;
    float v = (d == 0.0) ? q.x : (q.x + e);
    float3 hsv = float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), v);

    float hue = hsv.x + offsetDegrees / 360.0;
    hsv.x = (hue < 0.0) ? hue + 1.0 : (hue > 1.0) ? hue - 1.0 : hue;

    float4 k2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p2 = abs(frac(hsv.xxx + k2.xyz) * 6.0 - k2.www);
    outputColor = hsv.z * lerp(k2.xxx, saturate(p2 - k2.xxx), hsv.y);
}

float CrystalSampleLegacySineValueNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);
    uv = abs(frac(uv) - 0.5);

    float2 c0 = i + float2(0.0, 0.0);
    float2 c1 = i + float2(1.0, 0.0);
    float2 c2 = i + float2(0.0, 1.0);
    float2 c3 = i + float2(1.0, 1.0);

    float r0;
    float r1;
    float r2;
    float r3;
    Hash_LegacySine_2_1_float(c0, r0);
    Hash_LegacySine_2_1_float(c1, r1);
    Hash_LegacySine_2_1_float(c2, r2);
    Hash_LegacySine_2_1_float(c3, r3);

    float bottom = lerp(r0, r1, f.x);
    float top = lerp(r2, r3, f.x);
    return lerp(bottom, top, f.y);
}

void CrystalGenerateLegacySineSimpleNoise(float2 uv, float scale, out float noiseValue)
{
    noiseValue = 0.0;

    float freq = pow(2.0, 0.0);
    float amp = pow(0.5, 3.0);
    noiseValue += CrystalSampleLegacySineValueNoise(uv * (scale / freq)) * amp;

    freq = pow(2.0, 1.0);
    amp = pow(0.5, 2.0);
    noiseValue += CrystalSampleLegacySineValueNoise(uv * (scale / freq)) * amp;

    freq = pow(2.0, 2.0);
    amp = pow(0.5, 1.0);
    noiseValue += CrystalSampleLegacySineValueNoise(uv * (scale / freq)) * amp;
}

struct PerPixelHeightDisplacementParam
{
    float2 uv;
};

float CrystalSampleParallaxHeight(
    float2 texOffsetCurrent,
    float lod,
    PerPixelHeightDisplacementParam sampleData,
    TEXTURE2D_PARAM(heightTexture, heightSampler))
{
    return SAMPLE_TEXTURE2D_LOD(heightTexture, heightSampler, sampleData.uv + texOffsetCurrent, lod)[0];
}

#define ComputePerPixelHeightDisplacement CrystalSampleParallaxHeight
#define POM_NAME_ID CrystalParallaxOffset
#define POM_USER_DATA_PARAMETERS , TEXTURE2D_PARAM(heightTexture, samplerState)
#define POM_USER_DATA_ARGUMENTS , TEXTURE2D_ARGS(heightTexture, samplerState)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl"
#undef ComputePerPixelHeightDisplacement
#undef POM_NAME_ID
#undef POM_USER_DATA_PARAMETERS
#undef POM_USER_DATA_ARGUMENTS

float4 CrystalEvaluateParallaxLayer(float2 meshUv, float3 tangentViewDirection)
{
#ifdef _CRYSTAL_ON
    if (_CrystalUseParallax <= 0.5)
    {
        return 0.0.xxxx;
    }

    float4x4 parallaxWorldToObject = GetWorldToObjectMatrix();
    float3 parallaxObjectScale = 1.0.xxx;
    parallaxObjectScale.x = length(float3(parallaxWorldToObject._m00, parallaxWorldToObject._m01, parallaxWorldToObject._m02));
    parallaxObjectScale.z = length(float3(parallaxWorldToObject._m20, parallaxWorldToObject._m21, parallaxWorldToObject._m22));

    float3 parallaxViewDirection = tangentViewDirection * parallaxObjectScale.xzy;
    float parallaxMaxHeight = _CrystalParallaxAmplitude * 0.01;
    float2 parallaxUvTiling = _CrystalParallaxMap_ST.xy;
    parallaxMaxHeight *= 2.0 / max(abs(parallaxUvTiling.x) + abs(parallaxUvTiling.y), 0.0001);

    float2 parallaxUvSpaceScale = parallaxMaxHeight * parallaxUvTiling;
    float3 parallaxViewDirectionUv = normalize(float3(parallaxViewDirection.xy * parallaxUvSpaceScale, parallaxViewDirection.z));

    PerPixelHeightDisplacementParam parallaxData;
    float2 parallaxSampleUv = TRANSFORM_TEX(meshUv, _CrystalParallaxMap);
    parallaxData.uv = parallaxSampleUv;

    float parallaxOutHeight = 0.0;
    const int parallaxStepCount = 5;
    float2 parallaxDisplacedUv = parallaxSampleUv;

    if (parallaxStepCount > 0)
    {
        parallaxDisplacedUv += ParallaxOcclusionMappingCrystalParallaxOffset(
            0.0,
            0.0,
            parallaxStepCount,
            parallaxViewDirectionUv,
            parallaxData,
            parallaxOutHeight,
            TEXTURE2D_ARGS(_CrystalParallaxMap, sampler_CrystalParallaxMap));
    }

    float4 parallaxMapSample = SAMPLE_TEXTURE2D(_CrystalParallaxMap, sampler_CrystalParallaxMap, parallaxDisplacedUv);
    return parallaxMapSample * _CrystalParallaxColor;
#else
    return 0.0.xxxx;
#endif
}

float4 CrystalEvaluateGlitterLayer(float2 meshUv, float3 worldViewDirection)
{
#ifdef _CRYSTAL_ON
    if (_CrystalUseGlitter <= 0.5)
    {
        return 0.0.xxxx;
    }

    float2 glitterNoiseUv = TRANSFORM_TEX(meshUv, _CrystalGlitterNoiseMap);
    float3 glitterNoiseSample = SAMPLE_TEXTURE2D(_CrystalGlitterNoiseMap, sampler_CrystalGlitterNoiseMap, glitterNoiseUv).rgb;

    float3 rotatedGlitterNoise;
    CrystalRotateHueDegrees(
        glitterNoiseSample,
        10.0 * _TimeParameters.x,
        rotatedGlitterNoise);

    float3 glitterDirection = normalize(rotatedGlitterNoise - _CrystalGlitterOffset.xxx);
    float glitterMask = saturate(dot(glitterDirection, 1.0.xxx - normalize(worldViewDirection)));
    return glitterMask.xxxx * _CrystalGlitterColor;
#else
    return 0.0.xxxx;
#endif
}

float3 CrystalEvaluateTangentSpaceContribution(float3 tangentViewDirection)
{
#ifdef _CRYSTAL_ON
    if (_CrystalUseTangentSpaceMap <= 0.5)
    {
        return 0.0.xxx;
    }

    float2 tangentSpaceUv = tangentViewDirection.xy;
    float2 tangentSpaceSampleUv = tangentSpaceUv * _CrystalTangentSpaceMap_ST.xy + _CrystalTangentSpaceMap_ST.zw;
    float3 tangentSpaceMapSample = SAMPLE_TEXTURE2D(_CrystalTangentSpaceMap, sampler_CrystalTangentSpaceMap, tangentSpaceSampleUv).rgb;
    return _CrystalTangentSpaceColor.rgb * tangentSpaceMapSample;
#else
    return 0.0.xxx;
#endif
}

float4 CrystalEvaluateInnerGlowLayer(float2 meshUv, float3 objectPosition)
{
#ifdef _CRYSTAL_ON
    if (_CrystalUseInnerGlow <= 0.5)
    {
        return 0.0.xxxx;
    }

    float innerGlowDistance = distance(objectPosition, _CrystalInnerGlowCenter) / max(_CrystalInnerGlowSize, 0.0001);
    float innerGlowMask = saturate(1.0 - innerGlowDistance);
    float innerGlowCoreMask = innerGlowMask * innerGlowMask;
    innerGlowCoreMask *= innerGlowCoreMask;

    float innerGlowNoise;
    CrystalGenerateLegacySineSimpleNoise(meshUv, 1.0, innerGlowNoise);
    return _CrystalInnerGlowColor * (innerGlowCoreMask * innerGlowNoise).xxxx;
#else
    return 0.0.xxxx;
#endif
}

float3 CrystalEvaluateMatCapContribution(float3 normalWS)
{
#ifdef _CRYSTAL_ON
    if (_CrystalUseMatCap <= 0.5)
    {
        return 0.0.xxx;
    }

    float3 normalVS = normalize(TransformWorldToViewDir(normalWS));
    float2 matCapUv = normalVS.xy * 0.5 + 0.5;
    float3 matCapSample = SAMPLE_TEXTURE2D(_CrystalMatCapMap, sampler_CrystalMatCapMap, matCapUv).rgb;
    return matCapSample * _CrystalMatCapColor.rgb;
#else
    return 0.0.xxx;
#endif
}

float CrystalEvaluateRangeMask(float4 baseMapSample)
{
    // 基础贴图 A 通道当前就是晶体厚度来源，遮罩开启时复用该通道作为晶体范围。
    return lerp(1.0, saturate(baseMapSample.a), step(0.5, _CrystalUseMaskMap));
}

void CrystalApplySurface(
    float2 meshUv,
    float4 baseMapSample,
    float3 baseAlbedo,
    float3 positionOS,
    float3 viewDirWS,
    float3 tangentViewDirection,
    float3 surfaceNormalWS,
    out float3 finalAlbedo,
    out float3 extraEmission)
{
    finalAlbedo = baseAlbedo;
    extraEmission = 0.0.xxx;

#ifdef _CRYSTAL_ON
    // 晶体直接复用 cloth v2 主材质的基础贴图、法线、MRA 和自发光，
    // 这里只叠加 CrystalCode 的附加模块效果。
    float crystalMask = CrystalEvaluateRangeMask(baseMapSample);
    if (_CrystalUseThicknessTint > 0.5)
    {
        finalAlbedo += baseMapSample.a * _CrystalThicknessColor.rgb * baseMapSample.rgb * crystalMask;
    }

    finalAlbedo += (CrystalEvaluateTangentSpaceContribution(tangentViewDirection)
        + CrystalEvaluateMatCapContribution(surfaceNormalWS)) * crystalMask;

    extraEmission += (CrystalEvaluateGlitterLayer(meshUv, viewDirWS).rgb
        + CrystalEvaluateParallaxLayer(meshUv, tangentViewDirection).rgb
        + CrystalEvaluateInnerGlowLayer(meshUv, positionOS).rgb) * crystalMask;
#endif
}

float3 CrystalEvaluateSSSContribution(Light light, float3 normalWS, float3 viewDirWS, float thickness)
{
    float3 distortedHalfVector = normalize(light.direction + normalWS + _CrystalSSSDistortion.xxx);
    float viewDotHalf = pow(saturate(dot(viewDirWS, -distortedHalfVector)), max(_CrystalSSSPower, 0.0001));
    return light.color * _CrystalSSSColor.rgb * (viewDotHalf * thickness);
}

float3 CrystalEvaluateSSS(
    float3 positionWS,
    float4 shadowCoord,
    float3 normalWS,
    float3 viewDirWS,
    float thickness)
{
#ifdef _CRYSTAL_ON
    if (_CrystalUseSSS <= 0.5)
    {
        return 0.0.xxx;
    }

    float3 sssLayer = 0.0.xxx;

    #if _MAIN_LIGHT_SHADOWS_SCREEN || _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_CASCADE
        Light mainLight = GetMainLight(shadowCoord);
    #else
        Light mainLight = GetMainLight();
    #endif

    sssLayer += CrystalEvaluateSSSContribution(mainLight, normalWS, viewDirWS, thickness);

    #if defined(_ADDITIONAL_LIGHTS)
    uint additionalLightsCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
    {
        Light additionalLight = GetAdditionalLight(lightIndex, positionWS);
        sssLayer += CrystalEvaluateSSSContribution(additionalLight, normalWS, viewDirWS, thickness);
    }
    #endif

    return sssLayer;
#else
    return 0.0.xxx;
#endif
}

#endif
