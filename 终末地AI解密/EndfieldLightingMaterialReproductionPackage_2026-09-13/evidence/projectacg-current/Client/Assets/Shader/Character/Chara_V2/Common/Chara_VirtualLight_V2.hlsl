// Purpose: Shared virtual-light helpers for Chara_V2.
#ifndef CHARA_VIRTUAL_LIGHT_V2_INCLUDED
#define CHARA_VIRTUAL_LIGHT_V2_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Chara_CommonHelpers_V2.hlsl"

// 当前相机前向，保持与角色 Toon 链路一致的取法。
float3 CharaGetCameraForwardWS()
{
    return normalize(UNITY_MATRIX_V[2].xyz);
}

// 让视线方向向相机前向插值，保留旧链路的视角修正行为。
float3 CharaApplyCameraForwardToViewDir(float3 viewDirWS, float3 cameraForwardWS, float forwardDirStrength)
{
    return normalize(lerp(viewDirWS, cameraForwardWS, forwardDirStrength));
}

// 只在逆光区域增强虚拟光，避免俯视/仰视时补光过亮。
float CharaGetBackLightMask(float3 cameraForwardWS, float3 mainLightDirWS, float strength)
{
    float2 cameraForwardXZ = normalize(cameraForwardWS.xz + float2(1e-5, 0.0));
    float2 mainLightDirXZ = normalize(mainLightDirWS.xz + float2(1e-5, 0.0));

    float backLight = saturate(-dot(cameraForwardXZ, mainLightDirXZ));
    float backLightY = saturate(-abs(cameraForwardWS.y) + 0.75);
    backLightY = smoothstep(0.0, 1.0, backLightY);

    return saturate(backLight * backLightY * strength);
}

// 虚拟光方向支持主光方向与相机方向之间的插值。
float3 CharaGetVirtualLightDirectionWS(
    float3 mainLightDirWS,
    float3 cameraForwardWS,
    float useCameraDir,
    float cameraYBlend)
{
    float virtualLightY = lerp(mainLightDirWS.y, cameraForwardWS.y, cameraYBlend);
    float3 cameraVirtualLightDir = normalize(float3(cameraForwardWS.x, virtualLightY, cameraForwardWS.z));
    return normalize(lerp(mainLightDirWS, cameraVirtualLightDir, useCameraDir));
}

// 虚拟光总开关，可选只在逆光区域生效。
float CharaGetVirtualLightMask(
    float3 mainLightDirWS,
    float3 cameraForwardWS,
    float onlyBackLight,
    float backLightStrength,
    float enableVirtualLight)
{
    float backLightMask = CharaGetBackLightMask(cameraForwardWS, mainLightDirWS, backLightStrength);
    return lerp(1.0, backLightMask, onlyBackLight) * enableVirtualLight;
}

// 法线朝向相机前向的 0~1 结果。
float CharaGetNoF01(float3 normalWS, float3 cameraForwardWS)
{
    return dot(normalWS, cameraForwardWS) * 0.5 + 0.5;
}

// 构造一个偏向相机正前方的方向，保留旧风格化高光辅助逻辑。
float3 CharaGetForwardLightDirWS(float3 cameraForwardWS, float mainLightDirY, float dayStrength)
{
    float forwardLightDirY = lerp(0.5, mainLightDirY, dayStrength);
    return normalize(float3(cameraForwardWS.x, forwardLightDirY, cameraForwardWS.z));
}

// 把虚拟光方向当作主光方向来用。
float3 CharaApplyVirtualLightAsMainLightDir(
    float3 mainLightDirWS,
    float3 cameraForwardWS,
    float useCameraDir,
    float cameraYBlend,
    float onlyBackLight,
    float backLightStrength,
    float enableVirtualLight)
{
    float3 virtualLightDirWS = CharaGetVirtualLightDirectionWS(
        mainLightDirWS,
        cameraForwardWS,
        useCameraDir,
        cameraYBlend);

    float virtualLightMask = CharaGetVirtualLightMask(
        mainLightDirWS,
        cameraForwardWS,
        onlyBackLight,
        backLightStrength,
        enableVirtualLight);

    return normalize(lerp(mainLightDirWS, virtualLightDirWS, virtualLightMask));
}

// 简化的 Lambert 辅助函数，方便验证虚拟光效果。
float3 CharaGetLambertDiffuse(
    float3 albedo,
    float3 normalWS,
    float3 lightDirWS,
    float3 lightColor,
    float minLambert)
{
    float lambert = max(minLambert, saturate(dot(normalWS, lightDirWS)));
    return albedo * lightColor * lambert;
}

// 直接返回一盏虚拟光的补光结果。
float3 CharaGetVirtualLightResult(
    float3 albedo,
    float3 normalWS,
    float3 virtualLightDirWS,
    float virtualLightMask,
    float3 virtualLightColor,
    float virtualLightIntensity,
    float virtualLightOffset)
{
    float virtualNoL = saturate(dot(normalWS, virtualLightDirWS) + virtualLightOffset);
    return albedo * virtualLightColor * (virtualNoL * virtualLightIntensity * virtualLightMask);
}

// 把虚拟光作为额外补光叠加，不改原主光方向。
float3 CharaApplyVirtualLightAsAdditive(
    float3 albedo,
    float3 normalWS,
    float3 mainLightDirWS,
    float3 cameraForwardWS,
    float useCameraDir,
    float cameraYBlend,
    float onlyBackLight,
    float backLightStrength,
    float enableVirtualLight,
    float3 virtualLightColor,
    float virtualLightIntensity,
    float virtualLightOffset)
{
    float3 virtualLightDirWS = CharaGetVirtualLightDirectionWS(
        mainLightDirWS,
        cameraForwardWS,
        useCameraDir,
        cameraYBlend);

    float virtualLightMask = CharaGetVirtualLightMask(
        mainLightDirWS,
        cameraForwardWS,
        onlyBackLight,
        backLightStrength,
        enableVirtualLight);

    return CharaGetVirtualLightResult(
        albedo,
        normalWS,
        virtualLightDirWS,
        virtualLightMask,
        virtualLightColor,
        virtualLightIntensity,
        virtualLightOffset);
}

// 顶光/OtherLight 统一复用 Chara_V2 的公共 helper。
float CharaGetOtherLightNoL(float3 normalWS, float otherLightOffset, float otherLightStrength, float otherLightStrengthOffset)
{
    return CharaEvaluateTopLightNoL(
        normalWS,
        otherLightOffset,
        otherLightStrength,
        otherLightStrengthOffset);
}

float3 CharaGetOtherLightColor(float3 otherLightColorDark, float minShadowEffect)
{
    return CharaEvaluateTopLightColor(otherLightColorDark, minShadowEffect);
}

float3 CharaGetOtherLightResult(
    float3 normalWS,
    float3 otherLightColorDark,
    float minShadowEffect,
    float otherLightOffset,
    float otherLightStrength,
    float otherLightStrengthOffset)
{
    return CharaEvaluateTopLightResult(
        normalWS,
        otherLightColorDark,
        minShadowEffect,
        otherLightOffset,
        otherLightStrength,
        otherLightStrengthOffset);
}

#endif
