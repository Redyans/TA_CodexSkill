// Purpose: Apply the Chara_V2 final visible-color gradient.
#ifndef CHARA_FINAL_COLOR_GRADIENT_V2_INCLUDED
#define CHARA_FINAL_COLOR_GRADIENT_V2_INCLUDED

#include "Chara_Globals_V2.hlsl"

float CharaFinalColorGradientFactor(float3 positionWS)
{
    float parameterOverride = saturate(_GlobalCharacterRenderFinalColorGradientParametersOverride);
    float gradientMinY = lerp(_GradientMinY, _GlobalCharacterRenderFinalColorGradientMinY, parameterOverride);
    float gradientMaxY = lerp(_GradientMaxY, _GlobalCharacterRenderFinalColorGradientMaxY, parameterOverride);
    float positionWSY = TransformObjectToWorld(float3(0,0,0)).y;
    float relativeHeightY = positionWS.y - positionWSY;
    float heightRange = max(abs(gradientMaxY - gradientMinY), 1e-5);
    // return positionWSY;
    return saturate((gradientMaxY - relativeHeightY) / heightRange);
}

// Character meshes bake their vertical gradient into UV3's V component.
// GradientMinY/GradientMaxY remap the baked range so material, global, and
// Timeline overrides retain their original controls.
float CharaFinalColorGradientFactor(float2 uv3)
{
    float parameterOverride = saturate(_GlobalCharacterRenderFinalColorGradientParametersOverride);
    float gradientMinY = lerp(_GradientMinY, _GlobalCharacterRenderFinalColorGradientMinY, parameterOverride);
    float gradientMaxY = lerp(_GradientMaxY, _GlobalCharacterRenderFinalColorGradientMaxY, parameterOverride);
    float heightRange = max(abs(gradientMaxY - gradientMinY), 1e-5);
    return saturate((gradientMaxY - uv3.y) / heightRange);
}

float3 CharaFinalColorGradientDebugColor(float3 positionWS)
{
    float gradientFactor = CharaFinalColorGradientFactor(positionWS);
    float parameterOverride = saturate(_GlobalCharacterRenderFinalColorGradientParametersOverride);
    float3 gradientTint = lerp(
        _GradientColor.rgb,
        _GlobalCharacterRenderFinalColorGradientColor.rgb,
        parameterOverride);
    float3 gradientColor = lerp(1.0.xxx, gradientTint, gradientFactor);
    return gradientFactor;
    // return lerp(1.0.xxx, gradientColor, saturate(_GlobalCharacterRenderFinalColorGradientToggle));
}

float3 CharaFinalColorGradientDebugColor(float2 uv3)
{
    float gradientFactor = CharaFinalColorGradientFactor(uv3);
    float parameterOverride = saturate(_GlobalCharacterRenderFinalColorGradientParametersOverride);
    float3 gradientTint = lerp(
        _GradientColor.rgb,
        _GlobalCharacterRenderFinalColorGradientColor.rgb,
        parameterOverride);
    float3 gradientColor = lerp(1.0.xxx, gradientTint, gradientFactor);
    return gradientFactor;
    // return lerp(1.0.xxx, gradientColor, saturate(_GlobalCharacterRenderFinalColorGradientToggle));
}

float3 ApplyCharaFinalColorGradient(float3 finalColor, float3 positionWS)
{
    float gradientFactor = CharaFinalColorGradientFactor(positionWS);
    float parameterOverride = saturate(_GlobalCharacterRenderFinalColorGradientParametersOverride);
    float3 gradientTint = lerp(
        _GradientColor.rgb,
        _GlobalCharacterRenderFinalColorGradientColor.rgb,
        parameterOverride);
    float3 gradientColor = lerp(finalColor, finalColor * gradientTint, gradientFactor);
    return lerp(finalColor, gradientColor, saturate(_GlobalCharacterRenderFinalColorGradientToggle));
}

float3 ApplyCharaFinalColorGradient(float3 finalColor, float2 uv3)
{
    float gradientFactor = CharaFinalColorGradientFactor(uv3);
    float parameterOverride = saturate(_GlobalCharacterRenderFinalColorGradientParametersOverride);
    float3 gradientTint = lerp(
        _GradientColor.rgb,
        _GlobalCharacterRenderFinalColorGradientColor.rgb,
        parameterOverride);
    float3 gradientColor = lerp(finalColor, finalColor * gradientTint, gradientFactor);
    return lerp(finalColor, gradientColor, saturate(_GlobalCharacterRenderFinalColorGradientToggle));
}

#endif
