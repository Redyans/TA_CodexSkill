// Purpose: Unified material and texture declarations for the standalone Chara_Skin_V2 family.
#ifndef CHARA_SKIN_V2_BINDINGS_INCLUDED
#define CHARA_SKIN_V2_BINDINGS_INCLUDED

#include "../Common/Chara_SharedFeatureParams_V2.hlsl"

CBUFFER_START(UnityPerMaterial)
    float _CharacterRedTint;
    float4 _CharacterRedTintColor;
    float _Alpha;
    // Screen-space dithering fade. Factor 0 keeps all pixels; factor 1 clips all pixels.
    float _CharacterDitheringFactor;
    float _CharacterDitheringPixelSize;
    float4 _BaseMap_ST;
    float4 _NormalMap_ST;
    half4 _BaseColor;
    float _BaseColorPower;
    float _NeedNormalMap;
    float _NormalScale;
    float _MetallicOffset;
    float _ReflectivityStrength;
    float _AOOffset;
    float _AOContrast;
    float _AOPower;
    float _Roughness;
    float _RoughnessOffset;
    float _RoughnessContrast;
    float _AlbedoDarkStrength;
    float _AlbedoDarkSaturation;
    float _ChinDarkStrength;
    float _ChinDarkSaturation;
    float _FresnelStrength;
    float4 _ShadowColor;
    float4 _SpecularColor;
    float _SpecularStrength;
    float _SpecularShadowStrength;
    float _SelfAoShadowStrength;
    float _ForwardDirStrength;
    float _UseSkinLut;
    float _SkinLutSampleMode;
    float _RampSaturationStrength;
    float _RampStrength;
    float _RampColorNoLStrength;
    float _EffectLightToggle;
    float3 _EffetDirectionDir;
    float _EffetLightStrength;
    float _VirtualLightMode;
    float4 _VirtualLightColor;
    float _VirtualLightIntensity;
    float _VirtualLightOffset;
    float4 _OtherLightColor;
    float _OtherLightOffset;
    float _OtherLightStrength;
    float _OtherLightStrength_Offset;
    float4 _EnvColor;
    float _EnvRotation;
    float _EnvLightStrength;
    float _IndirectLightIntensity;
    float4 _IndirectTintColor;
    half _IndirectDifficusPlaneVolume;
    float _UsePerObjectShadow;
    float _SceneShadowCenter;
    float _SceneShadowSmooth;
    float _ShadowStrength;
    CHARA_SHARED_PLANAR_SHADOW_PARAMS_V2
    float _FresnelPower;
    float4 _FresnelInnerColor;
    float4 _FresnelOuterColor;
    float _FresnelSmoothMin;
    float _FresnelSmoothMax;
    float _FresnelMaskStrength;
    CHARA_SHARED_OUTLINE_PARAMS_V2
    float _FogIntensity;

    CHARA_SHARED_STANDARD_RIM_PARAMS_V2
    float4 _RimMaskRootPosition;
    float _RimNormalBlend;

    // Death Effect
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

    CHARA_SHARED_FINAL_COLOR_GRADIENT_PARAMS_V2
CBUFFER_END

TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
TEXTURE2D(_MixMap);SAMPLER(sampler_MixMap);
TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
TEXTURE2D(_RampMap);SAMPLER(sampler_RampMap);
TEXTURE2D(_LutColorTex);SAMPLER(sampler_LutColorTex);
TEXTURECUBE(_EnvMap);SAMPLER(sampler_EnvMap);
TEXTURE2D(_FresnelMask);SAMPLER(sampler_FresnelMask);
TEXTURE2D(_OutlineMask);SAMPLER(sampler_OutlineMask);
TEXTURE2D(_DeathMask);SAMPLER(sampler_DeathMask);

#endif
