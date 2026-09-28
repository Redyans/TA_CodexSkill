// Purpose: Shared per-object-shadow sampling, filtering, and mode-selection helpers.
#include "Chara_CommonHelpers_V2.hlsl"
#include "Chara_Globals_V2.hlsl"
TEXTURE2D_X(_ScreenSpaceShadowMap);
SAMPLER(sampler_ScreenSpaceShadowMap);

// Unified per-object shadow mode:
// 0 Off / 1 UnityOnly / 2 POSOnly / 3 Both

half SamplePerObjectShadow(float4 positionHCS)
{
    float2 screenUV = positionHCS.xy / _ScaledScreenParams.xy;
    half shadow = SAMPLE_TEXTURE2D_X(_ScreenSpaceShadowMap, sampler_ScreenSpaceShadowMap, screenUV).r;
    half receiverStrength = lerp(
        1.0h,
        max(0.0h, (half)_CharacterRenderTimelinePerObjectShadowStrength),
        saturate((half)_CharacterRenderTimelineHighQualityShadowStrengthWeight));
    // 场景级 Self/Environment 强度已经在 POS 合成 Pass 中应用；Timeline 只缩放绑定角色的最终接收结果。
    return lerp(1.0h, shadow, saturate(receiverStrength));
}

half FilterPerObjectShadow(half perObjectShadow, half sceneShadowCenter, half sceneShadowSmooth)
{
    return sigmoid(perObjectShadow, sceneShadowCenter, sceneShadowSmooth);
}

half SelectPerObjectShadowMode(half unityShadow, half bothShadow, half perObjectOnlyShadow, half usePerObjectShadow)
{
    half useUnityOnly = step(0.5h, usePerObjectShadow) - step(1.5h, usePerObjectShadow);
    half usePerObjectOnly = step(1.5h, usePerObjectShadow) - step(2.5h, usePerObjectShadow);
    half useBoth = step(2.5h, usePerObjectShadow);
    half shadow = lerp(1.0h, unityShadow, useUnityOnly);
    shadow = lerp(shadow, perObjectOnlyShadow, usePerObjectOnly);
    return lerp(shadow, bothShadow, useBoth);
}

half CombinePerObjectShadow(half unityShadow, half perObjectShadow, half usePerObjectShadow)
{
    return SelectPerObjectShadowMode(unityShadow, unityShadow * perObjectShadow, perObjectShadow, usePerObjectShadow);
}

half GetFilteredPerObjectShadow(float4 positionHCS, half sceneShadowCenter, half sceneShadowSmooth)
{
    return FilterPerObjectShadow(SamplePerObjectShadow(positionHCS), sceneShadowCenter, sceneShadowSmooth);
}

half ApplyPerObjectShadowRaw(half unityShadow, float4 positionHCS, half usePerObjectShadow)
{
    half resolvedMode = lerp(
        usePerObjectShadow,
        (half)_GlobalCharacterRenderSceneShadowMode,
        saturate((half)_GlobalCharacterRenderSceneShadowModeOverride));
    return CombinePerObjectShadow(unityShadow, SamplePerObjectShadow(positionHCS), resolvedMode);
}

half ApplyPerObjectShadow(half unityShadow, float4 positionHCS, half usePerObjectShadow)
{
    return ApplyPerObjectShadowRaw(unityShadow, positionHCS, usePerObjectShadow);
}
