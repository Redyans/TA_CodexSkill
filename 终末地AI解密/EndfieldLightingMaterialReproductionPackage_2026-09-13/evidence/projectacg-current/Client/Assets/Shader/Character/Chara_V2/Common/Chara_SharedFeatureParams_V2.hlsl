// Purpose: Shared UnityPerMaterial declaration blocks for outline, planar-shadow,
// and rim parameter families reused across Chara_V2 shaders.
#ifndef CHARA_SHARED_FEATURE_PARAMS_V2_INCLUDED
#define CHARA_SHARED_FEATURE_PARAMS_V2_INCLUDED

#define CHARA_SHARED_OUTLINE_PARAMS_V2 \
    float _SmoothNormal; \
    float _OutlineMultiplyBaseColor; \
    float4 _OutlineColor; \
    float _OutlineWidth; \
    float _OutlineClipSpaceZOffset; \
    float _UseOutlineMask; \
    float4 _OutlineMask_ST;

#define CHARA_SHARED_PLANAR_SHADOW_PARAMS_V2 \
    float _PlanarShadowFalloff; \
    float _PlanarShadowRange; \
    float4 _PlanarShadowLightDir; \
    float4 _PlanarShadowGlobalCenter; \
    float4 _PlanarShadowColor;

#define CHARA_SHARED_FINAL_COLOR_GRADIENT_PARAMS_V2 \
    float4 _GradientColor; \
    float _GradientMinY; \
    float _GradientMaxY;

#define CHARA_SHARED_STANDARD_RIM_PARAMS_V2 \
    float _RimType; \
    float _CustomLightDirection; \
    float _CustomRimDirectionSpace; \
    float4 _FresnelRimDirection; \
    float _FresnelRimLightAlign; \
    float4 _FresnelRimColor; \
    float _FresnelRimPower; \
    float _FresnelRimSmooth; \
    float _FresnelIntensity; \
    float _DepthRimWidthX; \
    float _DepthRimWidthY; \
    float _DepthRimIntensity; \
    float _RimLightDiffuseColorEffect; \
    float _RimShadowStrength; \
    float _UseRimFakePointMask; \
    float4 _RimFakePointMaskPosition; \
    float _RimFakePointMaskRange; \
    float _RimFakePointMaskPower; \
    float _UseRimFakeDirectMask; \
    float4 _RimFakeDirectMaskDirection; \
    float4 _RimFakeDirectMaskPosition; \
    float _RimFakeDirectMaskRange;

#endif
