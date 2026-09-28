// Purpose: Shared math, light-resolution, color-space, and normal-unpack
// helpers reused by multiple Chara_V2 shaders.
#ifndef CHARA_COMMON_HELPERS_V2_INCLUDED
#define CHARA_COMMON_HELPERS_V2_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Chara_Globals_V2.hlsl"

float2 TransformDeathMaskUV(float2 uv, float4 uvOffset, float rotationRadians)
{
    float s = sin(rotationRadians);
    float c = cos(rotationRadians);
    float2 centered = uv - 0.5;
    // uvOffset.xy 为静态偏移，uvOffset.zw 为 UV 单位/秒的流动速度。
    // Unity 内置 _Time.y 即为秒。
    float2 flowOffset = uvOffset.zw * _Time.y;
    return float2(centered.x * c - centered.y * s, centered.x * s + centered.y * c) +
        0.5 + uvOffset.xy + flowOffset;
}

#ifndef CHARA_SIGMOID_HELPERS_INCLUDED
#define CHARA_SIGMOID_HELPERS_INCLUDED
float Sigmoid(float blend, float offset, float x)
{
    float e = 2.71828;
    return 1.0 / (1.0 + pow(e, -max(0.0, blend) * (x - offset)));
}

float SigmoidSharp(float x, float center, float smoothness)
{
    float sharpness = rcp(max(1.0e-4, smoothness));
    return Sigmoid(sharpness, center, x);
}

float sigmoid(float x, float offset, float smooth)
{
    return 1.0 / (1.0 + pow(100000.0, (-3.0 * smooth * (x - offset))));
}

half sigmoidSharp(half x, half center, half sharpness)
{
    return rcp(pow(100000.0h, (x - center) * (-3.0h * sharpness)) + 1.0h);
}
#endif

#ifndef CHARA_UNPACK_NORMAL_HELPERS_INCLUDED
#define CHARA_UNPACK_NORMAL_HELPERS_INCLUDED
// 角色法线图预烘把平均 z 分量编码进 alpha 的低半区：
// - alpha >= 0.5 视为旧资源，沿用原先的 xy 重建 z 路径；
// - alpha < 0.5 视为 baked 资源，可在 mip 过滤后恢复 avgNormalLength，
//   再把 TextureNormalVariance 并入 roughness AA。
#define CHARA_BAKED_NORMAL_AVERAGE_Z_ALPHA_SCALE 0.49

bool HasCharacterBakedAverageNormalZ(float packedAlpha)
{
    return packedAlpha <= CHARA_BAKED_NORMAL_AVERAGE_Z_ALPHA_SCALE + (0.5 / 255.0);
}

float GetCharacterAverageNormalLength(float4 normalTexVar)
{
    if (!HasCharacterBakedAverageNormalZ(normalTexVar.a))
    {
        return 1.0;
    }

    float2 averageNormalXY = normalTexVar.xy * 2.0 - 1.0;
    float averageNormalZ = saturate(normalTexVar.a / CHARA_BAKED_NORMAL_AVERAGE_Z_ALPHA_SCALE);
    return saturate(length(float3(averageNormalXY, averageNormalZ)));
}

float3 UnpackNormalFromTex(float4 normalTexVar, float scale = 1.0)
{
    float3 normal;
    normal.xy = normalTexVar.xy * 2.0 - 1.0;
    normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));
    normal.xy *= scale;
    return normal;
}
#endif

#ifndef CHARA_LIGHT_STRENGTH_HELPERS_INCLUDED
#define CHARA_LIGHT_STRENGTH_HELPERS_INCLUDED
float MyCalculateLightStrength(float3 color)
{
    float3 lightCoefficient = float3(0.212672904, 0.715152204, 0.0721750036);
    return dot(color, lightCoefficient);
}

float CharaGetLightIntensity(float3 lightColor)
{
    return max(0.001, dot(lightColor, float3(0.299, 0.587, 0.114)));
}
#endif

#ifndef CHARA_LIGHTING_HELPERS_INCLUDED
#define CHARA_LIGHTING_HELPERS_INCLUDED
float3 CharaResolveMainLightColor(
    float3 mainLightColor,
    float globalToggle,
    float3 globalColor,
    float globalStrength,
    float effectToggle,
    float3 effectLightColor)
{
    // Follow-scene mode must preserve the scene main-light color (including its
    // intensity).  The custom and effect modes continue to use their own colors.
    float3 sceneLightColor = mainLightColor;
    float3 customLightColor = globalColor * max(0.0, globalStrength);
    float3 resolvedColor = lerp(sceneLightColor, customLightColor, globalToggle);
    return lerp(resolvedColor, effectLightColor, effectToggle);
}

half CharaResolveLightingScale(
    half globalToggle,
    half globalStrength,
    half effectToggle,
    half effectStrength)
{
    half lightingScale = 1.0h;
    lightingScale = lerp(lightingScale, lightingScale * max(0.0h, globalStrength), globalToggle);
    return lerp(lightingScale, max(0.0h, effectStrength), effectToggle);
}

float3 CharaSafeNormalizeDirection(float3 direction)
{
    float lengthSq = dot(direction, direction);
    if (lengthSq < 1.0e-8)
    {
        return 0.0.xxx;
    }

    return direction * rsqrt(lengthSq);
}

// FollowCamera changes only the main-light azimuth. Preserve the scene main
// light elevation while rebuilding a normalized direction from the camera XZ.
float3 CharaResolveFollowCameraXZDirection(float3 sceneDirection, float3 cameraDirection)
{
    float3 normalizedSceneDirection = CharaSafeNormalizeDirection(sceneDirection);
    float2 cameraDirectionXZ = cameraDirection.xz;
    float cameraDirectionXZLengthSq = dot(cameraDirectionXZ, cameraDirectionXZ);
    if (cameraDirectionXZLengthSq < 1.0e-8)
    {
        cameraDirectionXZ = normalizedSceneDirection.xz;
        cameraDirectionXZLengthSq = dot(cameraDirectionXZ, cameraDirectionXZ);
    }

    if (cameraDirectionXZLengthSq < 1.0e-8)
    {
        return normalizedSceneDirection;
    }

    cameraDirectionXZ *= rsqrt(cameraDirectionXZLengthSq);
    float sceneDirectionY = clamp(normalizedSceneDirection.y, -1.0, 1.0);
    float horizontalMagnitude = sqrt(max(0.0, 1.0 - sceneDirectionY * sceneDirectionY));
    return float3(
        cameraDirectionXZ.x * horizontalMagnitude,
        sceneDirectionY,
        cameraDirectionXZ.y * horizontalMagnitude);
}

float3 CharaResolveGlobalLightDirection(
    float3 mainLightDirection,
    float globalToggle,
    float3 globalDirection)
{
    float3 resolvedDirection = mainLightDirection;
    float3 resolvedGlobalDirection = globalDirection;
    if (dot(resolvedGlobalDirection, resolvedGlobalDirection) < 1.0e-4)
    {
        resolvedGlobalDirection = resolvedDirection;
    }
    else
    {
        resolvedGlobalDirection = normalize(resolvedGlobalDirection);
    }

    if (_GlobalCharacterRenderLightFollowCameraXZ >= 0.5)
    {
        resolvedGlobalDirection = CharaResolveFollowCameraXZDirection(
            resolvedDirection,
            resolvedGlobalDirection);
    }

    return normalize(lerp(resolvedDirection, resolvedGlobalDirection, globalToggle));
}

float3 CharaResolveMainLightDirection(
    float3 mainLightDirection,
    float globalToggle,
    float3 globalDirection,
    float effectToggle,
    float3 effectDirection)
{
    float3 resolvedDirection = CharaResolveGlobalLightDirection(mainLightDirection, globalToggle, globalDirection);
    return normalize(lerp(resolvedDirection, effectDirection, effectToggle));
}

float3 CharaResolveMainLightDirectionWithEffectFallback(
    float3 mainLightDirection,
    float globalToggle,
    float3 globalDirection,
    float effectToggle,
    float3 effectDirection)
{
    float3 resolvedDirection = CharaResolveGlobalLightDirection(mainLightDirection, globalToggle, globalDirection);
    float3 resolvedEffectDirection = effectDirection;
    if (dot(resolvedEffectDirection, resolvedEffectDirection) < 1.0e-4)
    {
        resolvedEffectDirection = resolvedDirection;
    }
    else
    {
        resolvedEffectDirection = normalize(resolvedEffectDirection);
    }

    return CharaSafeNormalizeDirection(lerp(resolvedDirection, resolvedEffectDirection, effectToggle));
}

float3 CharaResolveMainLightDirectionWithOverride(
    float3 mainLightDirection,
    float overrideEnabled,
    float3 overrideDirection,
    float globalToggle,
    float3 globalDirection,
    float effectToggle,
    float3 effectDirection)
{
    float3 resolvedDirection = CharaSafeNormalizeDirection(mainLightDirection);
    float useOverride = step(0.5, overrideEnabled);
    resolvedDirection = CharaSafeNormalizeDirection(lerp(resolvedDirection, overrideDirection, useOverride));

    float3 resolvedGlobalDirection = globalDirection;
    if (dot(resolvedGlobalDirection, resolvedGlobalDirection) < 1.0e-4)
    {
        resolvedGlobalDirection = resolvedDirection;
    }
    else
    {
        resolvedGlobalDirection = normalize(resolvedGlobalDirection);
    }

    if (_GlobalCharacterRenderLightFollowCameraXZ >= 0.5)
    {
        resolvedGlobalDirection = CharaResolveFollowCameraXZDirection(
            mainLightDirection,
            resolvedGlobalDirection);
    }
    resolvedDirection = CharaSafeNormalizeDirection(lerp(resolvedDirection, resolvedGlobalDirection, globalToggle));

    float3 resolvedEffectDirection = effectDirection;
    if (dot(resolvedEffectDirection, resolvedEffectDirection) < 1.0e-4)
    {
        resolvedEffectDirection = resolvedDirection;
    }
    else
    {
        resolvedEffectDirection = normalize(resolvedEffectDirection);
    }

    return CharaSafeNormalizeDirection(lerp(resolvedDirection, resolvedEffectDirection, effectToggle));
}
#endif

#ifndef CHARA_SPECULAR_AA_HELPERS_INCLUDED
#define CHARA_SPECULAR_AA_HELPERS_INCLUDED
// 只对由粗糙度驱动的镜面项做几何法线预过滤。
// 这样可以压住高光闪烁，但不去改 diffuse / rim / 阴影等风格化逻辑。
float FilterCharacterPerceptualSmoothness(float perceptualSmoothness, float3 geometricNormalWS)
{
    const float screenSpaceVariance = 0.125;
    const float threshold = 0.20;
    float3 safeGeometricNormalWS = SafeNormalize(geometricNormalWS);
    return ProjectedSpaceGeometricNormalFiltering(
        saturate(perceptualSmoothness),
        safeGeometricNormalWS,
        screenSpaceVariance,
        threshold);
}

float FilterCharacterRoughness(float roughness, float3 geometricNormalWS)
{
    return 1.0 - FilterCharacterPerceptualSmoothness(1.0 - saturate(roughness), geometricNormalWS);
}

float FilterCharacterRoughness(
    float roughness,
    float3 geometricNormalWS,
    float4 normalTexVar,
    float normalMapWeight)
{
    const float screenSpaceVariance = 0.125;
    const float threshold = 0.20;

    float3 safeGeometricNormalWS = SafeNormalize(geometricNormalWS);
    float variance = GeometricNormalVariance(safeGeometricNormalWS, screenSpaceVariance);

    float textureVarianceWeight = saturate(normalMapWeight);
    if (textureVarianceWeight > 1.0e-4)
    {
        float avgNormalLength = GetCharacterAverageNormalLength(normalTexVar);
        if (avgNormalLength < 0.9999)
        {
            variance += TextureNormalVariance(avgNormalLength) * textureVarianceWeight;
        }
    }

    float filteredPerceptualSmoothness = ProjectedSpaceNormalFiltering(
        saturate(1.0 - roughness),
        variance,
        threshold);
    return 1.0 - filteredPerceptualSmoothness;
}
#endif

#ifndef CHARA_SMOOTHSTEP_HELPERS_INCLUDED
#define CHARA_SMOOTHSTEP_HELPERS_INCLUDED
float MySmoothstep(float x)
{
    float t = x * -2.0 + 3.0;
    float x2 = x * x;
    return t * x2;
}
#endif

#ifndef CHARA_COLORSPACE_HELPERS_INCLUDED
#define CHARA_COLORSPACE_HELPERS_INCLUDED
float3 linear2sRGB(float3 color)
{
    float3 linearSegment = color * 12.9200001;
    float3 exponent = log2(abs(color));
    exponent *= 0.416666657;
    float3 curveSegment = exp2(exponent);
    curveSegment = curveSegment * 1.05499995 - 0.0549999997;

    bool3 isLinear = color <= 0.00313080009;

    float3 result;
    result.x = isLinear.x ? linearSegment.x : curveSegment.x;
    result.y = isLinear.y ? linearSegment.y : curveSegment.y;
    result.z = isLinear.z ? linearSegment.z : curveSegment.z;
    return result;
}
#endif

#ifndef CHARA_TEXTURE_SAMPLE_HELPERS_INCLUDED
#define CHARA_TEXTURE_SAMPLE_HELPERS_INCLUDED
float4 SampleTextureWithCustomST(TEXTURE2D_PARAM(tex, samplertex), float2 uv, float4 textureSt)
{
    float2 sampleUV = uv * textureSt.xy + textureSt.zw;
    return SAMPLE_TEXTURE2D(tex, samplertex, sampleUV);
}

float4 SampleRamp(TEXTURE2D_PARAM(tex, samplertex), float rampU, float rampV)
{
    return SAMPLE_TEXTURE2D(tex, samplertex, float2(rampU, rampV));
}
#endif

#ifndef CHARA_COLOR_ADJUST_HELPERS_INCLUDED
#define CHARA_COLOR_ADJUST_HELPERS_INCLUDED
float3 RGBToHSV(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1e-10;
    float h = abs(q.z + (q.w - q.y) / (6.0 * d + e));
    float s = d / (q.x + e);
    float v = q.x;
    return float3(h, s, v);
}

float3 HSVToRGB(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

float3 ApplyCharaHSVAdjustment(float3 color, float hueOffset, float saturationScale, float brightnessScale)
{
    float3 hsv = RGBToHSV(max(color, 0));
    hsv.x = frac(hsv.x + hueOffset);
    hsv.y = saturate(hsv.y * saturationScale);
    hsv.z = max(hsv.z * brightnessScale, 0);
    return HSVToRGB(hsv);
}
#endif

#ifndef CHARA_SURFACE_RESPONSE_HELPERS_INCLUDED
#define CHARA_SURFACE_RESPONSE_HELPERS_INCLUDED
float3 CalculateFresnel(
    float NdotV,
    float fresnelPower,
    float fresnelSmoothMin,
    float fresnelSmoothMax,
    float3 fresnelInnerColor,
    float3 fresnelOuterColor)
{
    float fresnel = pow(1 - NdotV, fresnelPower);
    fresnel = smoothstep(fresnelSmoothMin, fresnelSmoothMax, fresnel);
    return lerp(fresnelInnerColor, fresnelOuterColor, fresnel);
}

half3 CalculateTopLight(
    float3 normalWS,
    float3 albedo,
    float sceneShadow,
    float3 ao,
    float topLightSoft,
    float topLightOffset,
    float3 topLightColor,
    float topLightIntensity)
{
    half topNdotL = dot(normalWS, TransformObjectToWorldDir(float3(0, 0, 1), true));
    half topLambert = saturate(topNdotL * topLightSoft + topLightOffset);
    return albedo * topLambert * topLightColor * topLightIntensity * sceneShadow * ao;
}

half3 BlendSoftLight(half3 base, half3 blend)
{
    return (1.0 - 2.0 * blend) * base * base + 2.0 * blend * base;
}
#endif

#ifndef CHARA_TOP_LIGHT_HELPERS_INCLUDED
#define CHARA_TOP_LIGHT_HELPERS_INCLUDED
// Chara_V2 统一顶光/OtherLight 入口：
// - 固定使用世界空间朝上方向 (0, 1, 0)；
// - 保持原有参数语义不变，只消除各 shader 的重复实现；
// - 结果默认不做额外 saturate，调用方若历史上需要截断，仍由调用方自己处理。
float CharaEvaluateTopLightNoL(
    float3 normalWS,
    float otherLightOffset,
    float otherLightStrength,
    float otherLightStrengthOffset)
{
    const float3 topLightDirWS = float3(0.0, 1.0, 0.0);
    float topLightNoL = dot(topLightDirWS, normalWS);
    topLightNoL = saturate(topLightNoL + otherLightOffset);
    return topLightNoL * otherLightStrength + otherLightStrengthOffset;
}

float3 CharaEvaluateTopLightColor(float3 topLightDarkColor, float shadowBlend)
{
    return lerp(topLightDarkColor, 1.0.xxx, shadowBlend);
}

float3 CharaEvaluateTopLightResult(
    float3 normalWS,
    float3 topLightDarkColor,
    float shadowBlend,
    float otherLightOffset,
    float otherLightStrength,
    float otherLightStrengthOffset)
{
    float topLightNoL = CharaEvaluateTopLightNoL(
        normalWS,
        otherLightOffset,
        otherLightStrength,
        otherLightStrengthOffset);
    float3 topLightColor = CharaEvaluateTopLightColor(topLightDarkColor, shadowBlend);
    return topLightColor * topLightNoL;
}
#endif

#endif
