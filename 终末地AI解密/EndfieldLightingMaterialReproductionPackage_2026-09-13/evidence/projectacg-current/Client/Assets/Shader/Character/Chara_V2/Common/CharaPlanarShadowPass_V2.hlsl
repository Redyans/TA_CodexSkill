// Purpose: Shared planar shadow pass implementation for character shaders.
#ifndef CHARA_PLANAR_SHADOW_PASS_INCLUDED
#define CHARA_PLANAR_SHADOW_PASS_INCLUDED

#ifndef CHARA_PLANAR_SHADOW_APPLY_DITHERING
#define CHARA_PLANAR_SHADOW_APPLY_DITHERING(positionCS)
#endif

#include "Chara_Globals_V2.hlsl"

struct PlanarShadowAttributes
{
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PlanarShadowVaryings
{
    float4 positionCS : SV_POSITION;
    float falloff : TEXCOORD0;
};

// 平面阴影方向模式：
// 0 = 使用材质 _PlanarShadowLightDir
// 1 = 使用 Controller 提供的平面阴影自定义方向
// 2 = 完整跟随主光覆盖方向
float3 ResolveCharacterPlanarShadowDirection(float3 materialDirection)
{
    float3 resolvedDirection = materialDirection;
    if (_GlobalCharacterRenderPlanarShadowDirectionMode >= 1.5)
    {
        float3 planarBaseDirection = dot(
            _GlobalCharacterRenderPlanarShadowDirection,
            _GlobalCharacterRenderPlanarShadowDirection) >= 0.0001
            ? _GlobalCharacterRenderPlanarShadowDirection
            : materialDirection;
        float followDirectionLengthSq = dot(
            _GlobalCharacterRenderLightDirection,
            _GlobalCharacterRenderLightDirection);
        resolvedDirection = followDirectionLengthSq >= 0.0001
            ? _GlobalCharacterRenderLightDirection
            : planarBaseDirection;
    }
    else if (_GlobalCharacterRenderPlanarShadowDirectionMode >= 0.5)
    {
        resolvedDirection = _GlobalCharacterRenderPlanarShadowDirection;
    }

    if (dot(resolvedDirection, resolvedDirection) < 0.0001)
    {
        resolvedDirection = materialDirection;
    }

    if (dot(resolvedDirection, resolvedDirection) < 0.0001)
    {
        resolvedDirection = float3(0.0, 1.0, 0.0);
    }

    return normalize(resolvedDirection);
}

PlanarShadowVaryings PlanarShadowPassVertex(PlanarShadowAttributes input)
{
    PlanarShadowVaryings output = (PlanarShadowVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);

    output.positionCS = TransformObjectToHClip(input.positionOS);

#ifdef _PLANARSHADOW_ON
    if (_GlobalCharacterRenderPlanarShadowToggle >= 0.5)
    {
        float3 worldPos = TransformObjectToWorld(input.positionOS);
        float3 lightDir = ResolveCharacterPlanarShadowDirection(_PlanarShadowLightDir.xyz);
        float parameterOverride = saturate(_GlobalCharacterRenderPlanarShadowParametersOverride);
        float planarShadowFalloff = lerp(
            _PlanarShadowFalloff,
            max(0.0, _GlobalCharacterRenderPlanarShadowFalloff),
            parameterOverride);
        float planarShadowRange = lerp(
            _PlanarShadowRange,
            _GlobalCharacterRenderPlanarShadowRange,
            parameterOverride);
        float planarShadowPlaneHeight = lerp(
            _PlanarShadowLightDir.w,
            _GlobalCharacterRenderPlanarShadowPlaneHeight,
            parameterOverride);
        float3 planarShadowGlobalCenter = lerp(
            _PlanarShadowGlobalCenter.xyz,
            _GlobalCharacterRenderPlanarShadowGlobalCenter,
            parameterOverride);
        float3 shadowPos;

        shadowPos.y = min(worldPos.y, planarShadowPlaneHeight);

        float denom = max(0.001, lightDir.y);
        shadowPos.xz = worldPos.xz - lightDir.xz * max(0.0, worldPos.y - planarShadowPlaneHeight) / denom;

        output.falloff = 1.0 - saturate(
            distance(shadowPos, planarShadowGlobalCenter) * planarShadowFalloff + planarShadowRange);
        output.positionCS = TransformWorldToHClip(shadowPos);
    }
#endif

    return output;
}

half4 PlanarShadowPassFragment(PlanarShadowVaryings input) : SV_Target
{
#ifdef _PLANARSHADOW_ON
    if (_GlobalCharacterRenderPlanarShadowToggle < 0.5)
    {
        return 0;
    }

    CHARA_PLANAR_SHADOW_APPLY_DITHERING(input.positionCS);

    half parameterOverride = saturate(_GlobalCharacterRenderPlanarShadowParametersOverride);
    half4 planarShadowColor = lerp(
        _PlanarShadowColor,
        _GlobalCharacterRenderPlanarShadowColor,
        parameterOverride);
    return half4(
        planarShadowColor.rgb,
        saturate(planarShadowColor.a * input.falloff * _GlobalCharacterRenderPlanarShadowStrength));
#else
    return 0;
#endif
}

#endif
