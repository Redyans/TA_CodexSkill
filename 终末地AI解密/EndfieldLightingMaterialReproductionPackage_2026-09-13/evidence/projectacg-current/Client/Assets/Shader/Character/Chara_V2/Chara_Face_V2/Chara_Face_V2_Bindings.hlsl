// Purpose: Unified material and texture declarations for the Chara_Face_V2 family.
#ifndef CHARA_FACE_V2_BINDINGS_INCLUDED
#define CHARA_FACE_V2_BINDINGS_INCLUDED

#include "../Common/Chara_SharedFeatureParams_V2.hlsl"

CBUFFER_START(UnityPerMaterial)
    // Purpose: Family-local UnityPerMaterial block for Face V2.
    float _CharacterRedTint;
    float4 _CharacterRedTintColor;
    // Screen-space dithering fade. Factor 0 keeps all pixels; factor 1 clips all pixels.
    float _CharacterDitheringFactor;
    float _CharacterDitheringPixelSize;

    // ========================================
    // Shared Legacy / Runtime
    // ========================================
    float _fresnel;
    float _FresnelPower;
    float4 _FresnelInnerColor;
    float4 _FresnelOuterColor;
    float _FresnelSmoothMin;
    float _FresnelSmoothMax;
    float _FresnelStrength;
    float _SDF_UVSwitch;
    float _SDFShadowCenter;
    float _SDFShadowSmooth;
    float _SceneShadowCenter;
    float _SceneShadowSmooth;
    float4 _ShadowColor;

    // ========================================
    // Surface
    // ========================================
    float4 _BaseColor;
    float4 _ChinColor;
    float4 _NoseSpecColor;
    float4 _EmojiColor1;
    float _BaseColorPower;
    float _EmojiStrength1;
    float _UseSkinLut;
    float _FaceBrightness;
    float _directLight;
    float _ibl;
    float _Alpha;
    float _FogIntensity;

    // ========================================
    // Direct / Indirect Lighting
    // ========================================
    float _AlbedoDarkStrength;
    float _AlbedoDarkSaturation;
    float _RampSaturationStrength;
    float _RampStrength;
    float _RampColorNoLStrength;
    float _UsePerObjectShadow;
    float _FringeShadowBrightness;
    float4 _FringeShadowColor;
    float4 _OtherLightColor;
    float _OtherLightOffset;
    float _OtherLightStrength;
    float _OtherLightStrength_Offset;
    float _EffectLightToggle;
    float _EffetLightStrength;
    float3 _EffetDirectionDir;
    float _IndirectLightIntensity;
    float4 _IndirectTintColor;

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

    float _CustomLightDirection;
    float _CustomRimDirectionSpace;
    float4 _FresnelRimDirection;
    float _FresnelRimLightAlign;
    float4 _FresnelRimColor;
    float _FresnelIntensity;

    // ========================================
    // Outline / Fringe / Planar Shadow
    // ========================================
    float _outline;
    CHARA_SHARED_OUTLINE_PARAMS_V2
    CHARA_SHARED_PLANAR_SHADOW_PARAMS_V2

    float4 _HeadCenter;
    float4 _HeadRight;
    float4 _HeadForward;
    float _DebugFaceDiretion;
    half _IndirectDifficusPlaneVolume;
    CHARA_SHARED_FINAL_COLOR_GRADIENT_PARAMS_V2
CBUFFER_END

TEXTURE2D(_BaseTex);SAMPLER(sampler_BaseTex);
TEXTURE2D(_MaskTex);SAMPLER(sampler_MaskTex);
TEXTURE2D(_SDFTex);SAMPLER(sampler_SDFTex);
TEXTURE2D(_LutColorTex);SAMPLER(sampler_LutColorTex);
TEXTURE2D(_LipSpecTex);SAMPLER(sampler_LipSpecTex);
TEXTURE2D(_RampTex);SAMPLER(sampler_RampTex);
TEXTURE2D(_CustomMaskTex);SAMPLER(sampler_CustomMaskTex);
TEXTURE2D(_OutlineMask);SAMPLER(sampler_OutlineMask);
TEXTURE2D(_DeathMask);SAMPLER(sampler_DeathMask);
TEXTURECUBE(_CustomEnvCube);SAMPLER(sampler_CustomEnvCube);

#endif
