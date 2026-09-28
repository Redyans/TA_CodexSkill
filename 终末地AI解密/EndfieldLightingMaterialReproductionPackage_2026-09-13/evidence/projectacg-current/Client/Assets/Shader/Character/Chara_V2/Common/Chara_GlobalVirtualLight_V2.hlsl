// Purpose: CharacterRenderController-driven virtual-light helpers for Chara_V2.
#ifndef CHARA_GLOBAL_VIRTUAL_LIGHT_V2_INCLUDED
#define CHARA_GLOBAL_VIRTUAL_LIGHT_V2_INCLUDED

#include "Chara_Globals_V2.hlsl"
#include "Chara_VirtualLight_V2.hlsl"

float3 CharaGetGlobalVirtualLightCameraDirection()
{
    float directionLengthSq = dot(
        _GlobalCharacterRenderVirtualLightCameraDirection,
        _GlobalCharacterRenderVirtualLightCameraDirection);
    return directionLengthSq > 0.0001
        ? normalize(_GlobalCharacterRenderVirtualLightCameraDirection)
        : CharaGetCameraForwardWS();
}

float3 CharaGetGlobalVirtualLightMaskCameraDirection()
{
    float directionLengthSq = dot(
        _GlobalCharacterRenderVirtualLightMaskCameraDirection,
        _GlobalCharacterRenderVirtualLightMaskCameraDirection);
    return directionLengthSq > 0.0001
        ? normalize(_GlobalCharacterRenderVirtualLightMaskCameraDirection)
        : CharaGetCameraForwardWS();
}

float CharaGetGlobalVirtualLightBackLightMode()
{
    return step(0.5, _GlobalCharacterRenderBackLightToggle);
}

float3 CharaApplyGlobalVirtualLightAsMainLightDir(float3 mainLightDirWS)
{
    // 主光覆盖方向已经由 CharacterRenderController 按相机逐帧解析并写入全局主光方向。
    return mainLightDirWS;
}

float3 CharaGetGlobalVirtualBackLightResult(
    float3 albedo,
    float3 normalWS,
    float3 mainLightDirWS)
{
    float3 virtualLightDirWS = CharaGetVirtualLightDirectionWS(
        mainLightDirWS,
        CharaGetGlobalVirtualLightMaskCameraDirection(),
        1.0,
        0.0);
    float virtualLightMask = CharaGetVirtualLightMask(
        mainLightDirWS,
        CharaGetGlobalVirtualLightMaskCameraDirection(),
        1.0,
        1.0,
        CharaGetGlobalVirtualLightBackLightMode());

    return CharaGetVirtualLightResult(
        albedo,
        normalWS,
        virtualLightDirWS,
        virtualLightMask,
        _GlobalCharacterRenderVirtualLightColor.rgb,
        _GlobalCharacterRenderVirtualLightIntensity,
        _GlobalCharacterRenderVirtualLightOffset);
}

#endif
