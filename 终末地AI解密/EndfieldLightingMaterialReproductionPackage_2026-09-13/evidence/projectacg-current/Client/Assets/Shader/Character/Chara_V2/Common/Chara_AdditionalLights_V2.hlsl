#ifndef PROJECT_CHARA_ADDITIONAL_LIGHTS_V2_INCLUDED
#define PROJECT_CHARA_ADDITIONAL_LIGHTS_V2_INCLUDED

#include "Chara_Globals_V2.hlsl"

#if USE_FORWARD_PLUS
struct AdditionalLightsLoopInputData
{
    float3 positionWS;
    float2 normalizedScreenSpaceUV;
};
#endif

// 角色 V2 Shader 的统一额外灯直接光入口。
// 高光复用角色 V2 主高光的 D * V 形状，并省略 F0 精修、Ramp、各向异性和阴影链路。
// 当前消费灯光方向、颜色和距离/聚光衰减，不采样额外灯阴影。
void EvaluateCharacterAdditionalLight(
    Light additionalLight,
    float3 normalWS,
    float3 viewDirectionWS,
    float3 baseColor,
    float metallic,
    float perceptualRoughness,
    float3 specularF0,
    float3 specularWeight,
    out float3 diffuseLighting,
    out float3 specularLighting)
{
    float3 lightDirectionWS = SafeNormalize(additionalLight.direction);
    float NdotL = saturate(dot(normalWS, lightDirectionWS));
    float3 attenuatedLightColor = additionalLight.color * additionalLight.distanceAttenuation;
    float3 diffuseRadiance = attenuatedLightColor * NdotL;
    // Diffuse follows Lambert; the stylized Chara V2 highlight uses light attenuation only.

    // Metallic surfaces do not have a diffuse lobe. In particular, metallic=1
    // must remain diffuse-free even at smoothness=1 (perceptualRoughness=0).
    float oneMinusReflectivity = OneMinusReflectivityMetallic(saturate(metallic));
    diffuseLighting = baseColor * oneMinusReflectivity * diffuseRadiance;

    // 对齐 Cloth / Skin / Monster V2 的基础直接高光：视线方向权重更高，
    // 只保留高光形状所需的 D、V 项，由各材质继续提供自己的 F0、颜色和强度。
    float3 halfDirectionWS = SafeNormalize(viewDirectionWS * 3.0 + lightDirectionWS);
    float NdotV = max(saturate(dot(normalWS, viewDirectionWS)), 1.0e-4);
    float NdotH = saturate(dot(normalWS, halfDirectionWS));
    float roughness = saturate(perceptualRoughness);
    float roughnessSquare = max(roughness * roughness, 0.0078);
    float roughnessFourth = roughnessSquare * roughnessSquare;
    float distributionDenominator = (roughnessFourth - 1.0) * NdotH * NdotH + 1.0;
    distributionDenominator = max(distributionDenominator * distributionDenominator, 1.0e-8);
    float distributionTerm = roughnessFourth / distributionDenominator;
    float visibilityTerm = 0.5 / max(NdotV * 2.0 + roughnessSquare + 1.0e-4, 1.0e-4);
    float specularDV = clamp(distributionTerm * visibilityTerm - 6.10351562e-05, 0.0, 20.0);
    float3 specularBRDF = specularDV * specularF0;
    // A light behind the surface must not produce a front-facing highlight.
    // The half vector can still point into the visible hemisphere when the
    // light direction is back-facing, so explicitly gate the specular lobe by
    // the same NdotL used by diffuse lighting.
    specularLighting = specularBRDF * specularWeight * attenuatedLightColor * NdotL;
}

// 使用 URP 统一灯光循环，兼容当前 Forward 路径并为 Forward+ 保留正确的聚类灯遍历入口。
// positionHCS 必须传 Fragment 的 SV_POSITION，Forward+ 会用它构造屏幕空间聚类坐标。
void AccumulateCharacterAdditionalLighting(
    float3 positionWS,
    float4 positionHCS,
    float3 normalWS,
    float3 viewDirectionWS,
    float3 baseColor,
    float metallic,
    float perceptualRoughness,
    float3 specularF0,
    float3 specularWeight,
    out float3 additionalDiffuse,
    out float3 additionalSpecular)
{
    additionalDiffuse = 0.0;
    additionalSpecular = 0.0;

#if defined(_ADDITIONAL_LIGHTS)
    float diffuseEnabled = step(0.5, _GlobalCharacterAdditionalLightsDiffuseEnabled);
    float specularEnabled = step(0.5, _GlobalCharacterAdditionalLightsSpecularEnabled);
    UNITY_BRANCH
    if (_GlobalCharacterAdditionalLightsEnabled >= 0.5h && max(diffuseEnabled, specularEnabled) >= 0.5)
    {
        float3 normalizedNormalWS = SafeNormalize(normalWS);
        float3 normalizedViewDirectionWS = SafeNormalize(viewDirectionWS);

#if USE_FORWARD_PLUS
        AdditionalLightsLoopInputData inputData = (AdditionalLightsLoopInputData)0;
        inputData.positionWS = positionWS;
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionHCS);
#endif

        uint additionalLightsCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(additionalLightsCount)
            Light additionalLight = GetAdditionalLight(lightIndex, positionWS);
            float3 lightDiffuse;
            float3 lightSpecular;
            EvaluateCharacterAdditionalLight(
                additionalLight,
                normalizedNormalWS,
                normalizedViewDirectionWS,
                baseColor,
                metallic,
                perceptualRoughness,
                specularF0,
                specularWeight,
                lightDiffuse,
                lightSpecular);
            additionalDiffuse += lightDiffuse * diffuseEnabled;
            additionalSpecular += lightSpecular * specularEnabled;
        LIGHT_LOOP_END
    }
#endif
}

#endif
