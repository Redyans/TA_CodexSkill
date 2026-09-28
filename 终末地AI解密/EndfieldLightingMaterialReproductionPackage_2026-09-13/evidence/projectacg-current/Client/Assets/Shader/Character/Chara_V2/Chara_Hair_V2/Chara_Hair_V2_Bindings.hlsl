// Purpose: Unified material and texture declarations shared by Chara_Hair_V2 and Chara_Fringe_V2.
#ifndef CHARA_HAIR_V2_BINDINGS_INCLUDED
#define CHARA_HAIR_V2_BINDINGS_INCLUDED

#include "../Common/Chara_SharedFeatureParams_V2.hlsl"

CBUFFER_START(UnityPerMaterial)
    // Purpose: Family-local UnityPerMaterial block for Hair/Fringe V2.
    float _CharacterRedTint;
    float4 _CharacterRedTintColor;
    // Screen-space dithering fade. Factor 0 keeps all pixels; factor 1 clips all pixels.
    float _CharacterDitheringFactor;
    float _CharacterDitheringPixelSize;

    // ========================================
    // Shared Legacy / Runtime
    // ========================================
    float4 _ShadowColor;
    float _Saturation;
    float _Brightness;
    float4 _HairLine_ST;
    float4 _Shift_ST;
    float _SpecularShadowStrength;
    float _NeedNormalMap;
    float _AOPower;
    float _AOOffset;
    float _AOContrast;
    float _ShadowOffset;
    float _ShadowStrength;
    float _RampSaturationStrength;

    // ========================================
    // Surface
    // ========================================
    float4 _BaseColor;
    float _BaseColorPower;
    float _NormalScale;
    float _directLight;
    float _IndirectLight;
    float _ibl;
    float _ForwardDirStrength;
    float _NoFStrength;
    float _NoFPow;
    float _Alpha;
    float4 _RampLightColor;
    float4 _RampDarkColor;
    float4 _HairLineColor;
    float _Clip;
    float _FogIntensity;

    // ========================================
    // Direct / Indirect Lighting
    // ========================================
    float _HairShadowLerp;
    float _RampStrength;
    float _AlbedoDarkStrength;
    float _AlbedoDarkSaturation;
    float _BackLightStrength;
    float _HairShadowOffset;
    float _LambertShadowCenter;
    float _LambertShadowSmooth;
    float _SceneShadowCenter;
    float _SceneShadowSmooth;
    float _UsePerObjectShadow;
    float _GlobalShadowBrightness;
    float4 _OtherLightColor;
    float _OtherLightOffset;
    float _OtherLightStrength;
    float _OtherLightStrength_Offset;
    float _RampColorNoLStrength;
    float _EffectLightToggle;
    float _EffetLightStrength;
    float3 _EffetDirectionDir;
    float _IndirectLightIntensity;
    float4 _IndirectTintColor;

    // ========================================
    // Spec / Aniso / Fresnel
    // ========================================
    float _ShiftNoise;
    float _ShiftNoise2;
    float _AnisoOffset;
    float _AnisoOffset2;
    float4 _HSpecularColor1;
    float _SpecStrength1;
    float _SpecGloss1;
    float _NormalShift1;
    float4 _HSpecularColor2;
    float _SpecStrength2;
    float _SpecGloss2;
    float _NormalShift2;
    float _AnisoLength;
    float _AnisoCut;
    float4 _AnisoContrast;
    float _AnisoStrength;
    float _AnisoColorOffset;
    float _AnisoPower;
    float4 _AnisoAnotherColor;
    float _AnisoSecondLength;
    float _AnisoAnotherLength;
    float _AnisoSecondOffset;
    float _AnisoSecondPosition;
    float _AnisoSecondStrength;
    float _SphereNormalLerp;
    float _FresnelPower;
    float4 _FresnelInnerColor;
    float4 _FresnelOuterColor;
    float _FresnelSmoothMin;
    float _FresnelSmoothMax;
    CHARA_SHARED_STANDARD_RIM_PARAMS_V2
    float4 _RimMaskRootPosition;
    float _RimNormalBlend;

    // ========================================
    // Death Effect
    // ========================================
    float _DeathProgress;
    float _DeathFresnelScale;
    float _DeathFresnelPower;
    float4 _DeathFresnelColor;
    float4 _DeathMaskTiling;
    float4 _DeathMaskUVOffset;
    float _DeathMaskUVRotation;
    float4 _DeathInnerTint;
    float4 _DeathOuterTint;
    float _DeathMaskBlendStrength;

    // ========================================
    // Outline / Fringe / Planar Shadow
    // ========================================
    float _outline;
    CHARA_SHARED_OUTLINE_PARAMS_V2
    float _OuterAlpha;
    float _FringeOuterVerticalAtten;
    float _FringeOuterHorizontalAtten;
    float _ScreenOffsetScaleX;
    float _ScreenOffsetScaleY;
    CHARA_SHARED_PLANAR_SHADOW_PARAMS_V2

    float4 _HeadUp;
    float4 _HeadCenter;
    float4 _HeadRight;
    float4 _HeadForward;
    half _IndirectDifficusPlaneVolume;
    float _AnisoAnotherStrength;
    half _anisoArea2Shadow ;
    CHARA_SHARED_FINAL_COLOR_GRADIENT_PARAMS_V2
CBUFFER_END

TEXTURE2D(_BaseTex);SAMPLER(sampler_BaseTex);
TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
TEXTURE2D(_RampTex);SAMPLER(sampler_RampTex);
TEXTURE2D(_SpecRampTex);SAMPLER(sampler_SpecRampTex);
TEXTURE2D(_HairMaskTex);SAMPLER(sampler_HairMaskTex);
TEXTURE2D(_HairLineTex);SAMPLER(sampler_HairLineTex);
TEXTURE2D(_OutlineMask);SAMPLER(sampler_OutlineMask);
TEXTURE2D(_DeathMask);SAMPLER(sampler_DeathMask);

#endif
