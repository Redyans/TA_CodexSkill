// Purpose: Unified material and texture declarations shared by Brow, Eye, and EyeBlend variants.
#ifndef CHARA_EYE_V2_BINDINGS_INCLUDED
#define CHARA_EYE_V2_BINDINGS_INCLUDED

#include "../Common/Chara_SharedFeatureParams_V2.hlsl"

CBUFFER_START(UnityPerMaterial)
#if defined(CHARA_EYE_V2_VARIANT_BROW)
    float _CharacterRedTint;
    float4 _CharacterRedTintColor;
    float _Alpha;
    float _EmissionIntensity;
    float _BackLightAttenuation;
    float4 _HeadCenter;
    float4 _HeadForward;
    float4 _HeadRight;
    float4 _HeadUp;
    float _DeathProgress;
    float4 _DeathMaskTiling;
    float4 _DeathMaskUVOffset;
    float _DeathMaskUVRotation;
    float4 _DeathInnerTint;
    float4 _DeathOuterTint;
    float _DeathMaskBlendStrength;
    float _DeathFresnelScale;
    float _DeathFresnelPower;
    float4 _DeathFresnelColor;
#elif defined(CHARA_EYE_V2_VARIANT_EYEBLEND)
    float _Alpha;
    float _MatCapIntensity;
    float _MatCapFlipY;
    float _MatCapSphereLerp;
    float _ZwriteOp;
    float _ZTestMode;
    float _Cull;
    float _SrcBlend;
    float _DstBlend;
    half _CharacterRedTint;
    half4 _CharacterRedTintColor;
    float _BackLightAttenuation;
    float4 _HeadCenter;
    float4 _HeadForward;
    float4 _HeadRight;
    float4 _HeadUp;
#else

    half4 _EyeShadowColor;
    float _CharacterRedTint;
    float4 _CharacterRedTintColor;
    float _Alpha;
    float4 _IrisMap_ST;
    float _ParallaxScale;
    float _ParallaxFadeStart;
    float _ParallaxFadeEnd;
    float4 _RefractionCenterOffset;
    float _IOR;
    float _OffsetScale;
    float _PhysicalFadeStart;
    float _PhysicalFadeEnd;
    float _AngleThreshold;
    float _EmissionIntensity;
    float _BackLightAttenuation;
    float _ForwardViewCenter;
    float _ForwardViewSmooth;
    float4 _HeadCenter;
    float4 _HeadForward;
    float4 _HeadRight;
    float4 _HeadUp;
    float _EffectLightToggle;
    float3 _EffetDirectionDir;
    float _EffetLightStrength;
    float _ZwriteOp;
    float _ZTestMode;
    float _SrcBlend;
    float _DstBlend;
    float _DeathProgress;
    float4 _DeathMaskTiling;
    float4 _DeathMaskUVOffset;
    float _DeathMaskUVRotation;
    float4 _DeathInnerTint;
    float4 _DeathOuterTint;
    float _DeathMaskBlendStrength;
    float _DeathFresnelScale;
    float _DeathFresnelPower;
    float4 _DeathFresnelColor;
#endif
    // Screen-space dithering fade. Factor 0 keeps all pixels; factor 1 clips all pixels.
    float _CharacterDitheringFactor;
    float _CharacterDitheringPixelSize;
    CHARA_SHARED_FINAL_COLOR_GRADIENT_PARAMS_V2
CBUFFER_END

#if defined(CHARA_EYE_V2_VARIANT_BROW)
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
TEXTURE2D(_DeathMask);SAMPLER(sampler_DeathMask);
#elif defined(CHARA_EYE_V2_VARIANT_EYEBLEND)
TEXTURE2D(_MatCap);
SAMPLER(sampler_MatCap);
#else
TEXTURE2D(_EyeShadowMask); SAMPLER(sampler_EyeShadowMask);
TEXTURE2D(_IrisMap);SAMPLER(sampler_IrisMap);
TEXTURE2D(_DeathMask);SAMPLER(sampler_DeathMask);
#endif

#endif
