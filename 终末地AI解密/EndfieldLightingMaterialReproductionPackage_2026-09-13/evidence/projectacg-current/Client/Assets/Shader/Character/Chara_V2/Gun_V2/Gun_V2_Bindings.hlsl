// Purpose: Unified material and texture declarations for Gun_V2.
#ifndef GUN_V2_BINDINGS_INCLUDED
#define GUN_V2_BINDINGS_INCLUDED

#include "../Common/Chara_SharedFeatureParams_V2.hlsl"

CBUFFER_START(UnityPerMaterial)
#if defined(CHARA_CLOTH_V2_VARIANT_SILK_STOCKINGS)
    float _ZwriteOp;
    float _Cull;
    float _SrcBlend;
    float _DstBlend;

    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _NormalMap_ST;
    float4 _LightDarkColor;
    float4 _OtherLightColor;
    float4 _EnvColor;
    float4 _RimLightColor;
    float4 _EmissionColor;
    float4 _SpecularRefineColor;

    float _NeedMRATex;
    float _NeedSelfLight;
    float _SelfLightIntensity;
    float _NormalScale;
    float _NeedNormalMap;
    float _ForwardDirStrength;
    float _BaseColorPower;
    float _AOPower;
    float _MetallicOffset;
    float _MetallicContrast;
    float _AOOffset;
    float _AOContrast;
    float _RoughnessNonMetal;
    float _RoughnessNonMetalContrast;
    float _RoughnessMetal;
    float _RoughnessMetalContrast;
    float _SpecularAA;
    float _UsePerObjectShadow;
    float4 _AOShadowColor;
    float4 _ShadowColor;
    float _SceneShadowCenter;
    float _SceneShadowSmooth;
    float _ShadowOffset;
    float _ShadowStrength;
    float _BackLightStrength;
    float _MainLightWrap;
    float _MainLightSoftness;
    float _OtherLightOffset;
    float _OtherLightStrength;
    float _OtherLightStrength_Offset;
    float _OtherLightResultStrength_day1;
    float _OtherLightResultStrength_day0;
    float _SpecularShadowStrength;
    float4 _StockingsSpecColor;
    float _StockingsSpecStrength;
    float _RefineF0U_lerp;
    float _EnvRotation;
    float _EnvLightStrength;
    float4 _EnvMap_HDR;
    float _RimLightArea;
    float _RimLightStrength;
    float _RimNormalBlend;
    CHARA_SHARED_STANDARD_RIM_PARAMS_V2
    float _RimLightNoLxzStrength;
    float _EmissionColorStrength;
    float _CustomAlpha;
    float _Alpha;
    float _FogIntensity;

    float _outline;
    CHARA_SHARED_OUTLINE_PARAMS_V2
    CHARA_SHARED_PLANAR_SHADOW_PARAMS_V2

    float _SpecularRefineColorStrength;
    float _Clip;

    float _CharacterRedTint;
    float4 _CharacterRedTintColor;

    float _IndirectLightIntensity;
    float4 _IndirectTintColor;

    half _IndirectDifficusPlaneVolume;

    half _IsNeedOrmTex;

    half _EffectLightToggle;
    half _EffetLightStrength;
    half3 _EffetDirectionDir;

    // Stockings-only controls used by the LYJ silk branch.
    float _StockingsBlend;
    float _BaseThickness;
    float _ThicknessRemapMin;
    float _ThicknessRemapMax;
    float _ThicknessPower;
    float _StockingsPow;
    float4 _StockingsColorInside;
    float4 _StockingsColorOutside;
    float _EdgeDarken;
    float _SheerStrength;
    float4 _FakeSkinTint;
    float _FakeSkinStrength;
    float _ThinSpecBoost;

    float4 _StockingsControlMap_ST;
    float4 _FiberSpecColor;
    float4 _FiberSpecColor2;

    float _UseAnisoSpecular;
    float _AnisoSpecIntensity;
    float _UseAnisoStrengthMask;
    float _AnisoStrengthMaskStrength;
    float _AnisoStrengthMaskContrast;
    float _NormalAniso;
    float _FiberRotation;
    float _FiberSpecStrength;
    float _FiberSpecPower;
    float _FiberWidth;
    float _FiberSpecOffset;
    float _FiberSpecStrength2;
    float _FiberSpecPower2;
    float _FiberWidth2;
    float _FiberSpecOffset2;
    float _UseAnisoNoise;
    float _AnisoNoiseStrength;
    float _UseAnisoBreakup;
    float _AnisoBreakupStrength;
#else
    float _ZwriteOp;
    float _Cull;
    float _SrcBlend;
    float _DstBlend;

    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _NormalMap_ST;
    float4 _LightDarkColor;
    float4 _OtherLightColor;
    float4 _EnvColor;
    float4 _EmissionColor;
    float4 _SpecularRefineColor;

    float _NeedMRATex;
    float _NeedSelfLight;
    float _SelfLightIntensity;
    float _NormalScale;
    float _NeedNormalMap;
    float _ForwardDirStrength;
    float _BaseColorPower;
    float _AlbedoDarkStrength;
    float _AlbedoDarkSaturation;
    float _AOPower;
    float _MetallicOffset;
    float _MetallicContrast;
    float _AOOffset;
    float _AOContrast;
    float _RoughnessNonMetal;
    float _RoughnessNonMetalContrast;
    float _RoughnessMetal;
    float _RoughnessMetalContrast;
    float _SpecularAA;
    float _UsePerObjectShadow;
    float4 _AOShadowColor;
    float4 _ShadowColor;
    float _SceneShadowCenter;
    float _SceneShadowSmooth;
    float _ShadowStrength;
    float _BackLightStrength;
    float _RampSmooth;
    float _RampThreshold;
    float _RampColorNoLStrength;
    float _NoFStrength;
    float _NoFPow;
    float _OtherLightOffset;
    float _OtherLightStrength;
    float _OtherLightStrength_Offset;
    float _OtherLightResultStrength_day1;
    float _OtherLightResultStrength_day0;
    float _SpecularShadowStrength;
    float4 _SpecularColor;
    float _SpecularStrength;
    float _DiffuseBlendEffect;
    float _RefineF0U_lerp;
    float _refineF0TexLerp;
    float _EnvRotation;
    float _EnvLightStrength;
    float4 _EnvMap_HDR;
    float _RimNormalBlend;
    float _EmissionColorStrength;
    float _CustomAlpha;
    float _Alpha;
    float _FogIntensity;

    float _outline;
    CHARA_SHARED_OUTLINE_PARAMS_V2
    CHARA_SHARED_PLANAR_SHADOW_PARAMS_V2

    float _SpecularRefineColorStrength;
    float _Clip;

    float _CharacterRedTint;
    float4 _CharacterRedTintColor;

    float _IndirectLightIntensity;
    float4 _IndirectTintColor;
    float _IndirDiffIntensity;
    float4 _CustomEnvColor;
    float _CustomEnvDiffLerp;
    float _DirectDiffIntensity;
    float _FresnelPower;
    float4 _FresnelInnerColor;
    float4 _FresnelOuterColor;
    float _FresnelSmoothMin;
    float _FresnelSmoothMax;
    CHARA_SHARED_STANDARD_RIM_PARAMS_V2
    float _LambertShadowCenter;
    float _LambertShadowSmooth;
    float _GlobalShadowBrightness;

    half _IndirectDifficusPlaneVolume;

    half _IsNeedOrmTex;

    half _EffectLightToggle;
    half _EffetLightStrength;
    half3 _EffetDirectionDir;

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
#endif
    // Weapon dissolve (shared by Gun_PBR and Gun_V2).
    float _dissolve;
    float _DISSOLVE;
    float4 _DissolveNoiseMap_ST;
    half _DissolveThreshold;
    half _DissolveLineWidth;
    half _DissolveNoiseStrength;
    float4 _DissolveAxisOS;
    float _DissolveAxisMin;
    float _DissolveAxisMax;
    float _DissolveMode;
    float _DissolveCenter01;
    float _DISSOLVE_OFFSET;
    half4 _DissolveOffsetDirection;
    half _DissolveOffsetThreshold;
    half _DissolveOffsetScale;
    half4 _DissolveColor;
    half4 _DissolveDarkColor;

    CHARA_SHARED_FINAL_COLOR_GRADIENT_PARAMS_V2
CBUFFER_END

#if defined(CHARA_CLOTH_V2_VARIANT_SILK_STOCKINGS)
TEXTURE2D(_BaseTex);SAMPLER(sampler_BaseTex);
TEXTURE2D(_MRATex);SAMPLER(sampler_MRATex);
TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
TEXTURE2D(_RampTex);SAMPLER(sampler_RampTex);
TEXTURE2D(_SpecularRefineF0Tex);SAMPLER(sampler_SpecularRefineF0Tex);
TEXTURE2D(_StockingsControlMap);SAMPLER(sampler_StockingsControlMap);
TEXTURECUBE(_EnvMap);SAMPLER(sampler_EnvMap);
TEXTURE2D(_EmissionTex);SAMPLER(sampler_EmissionTex);
#else
TEXTURE2D(_BaseTex);SAMPLER(sampler_BaseTex);
TEXTURE2D(_MRATex);SAMPLER(sampler_MRATex);
TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
TEXTURE2D(_RampTex);SAMPLER(sampler_RampTex);
TEXTURE2D(_SpecularRefineF0Tex);SAMPLER(sampler_SpecularRefineF0Tex);
TEXTURECUBE(_EnvMap);SAMPLER(sampler_EnvMap);
TEXTURE2D(_EmissionTex);SAMPLER(sampler_EmissionTex);
TEXTURE2D(_DeathMask);SAMPLER(sampler_DeathMask);
#endif

TEXTURE2D(_DissolveNoiseMap);SAMPLER(sampler_DissolveNoiseMap);

#endif
