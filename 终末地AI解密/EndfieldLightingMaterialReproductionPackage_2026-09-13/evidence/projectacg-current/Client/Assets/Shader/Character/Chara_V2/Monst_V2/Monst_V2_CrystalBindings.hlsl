#ifndef MONST_V2_CRYSTAL_BINDINGS_INCLUDED
#define MONST_V2_CRYSTAL_BINDINGS_INCLUDED

#include "../Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl"

float _CrystalUseThicknessTint;
float _CrystalUseMaskMap;
float4 _CrystalThicknessColor;

float _CrystalUseSSS;
float4 _CrystalSSSColor;
float _CrystalSSSDistortion;
float _CrystalSSSPower;

float _CrystalUseParallax;
float4 _CrystalParallaxMap_ST;
float _CrystalParallaxAmplitude;
float4 _CrystalParallaxColor;

float _CrystalUseGlitter;
float4 _CrystalGlitterNoiseMap_ST;
float _CrystalGlitterOffset;
float4 _CrystalGlitterColor;

float _CrystalUseMatCap;
float4 _CrystalMatCapColor;

float _CrystalUseTangentSpaceMap;
float4 _CrystalTangentSpaceMap_ST;
float4 _CrystalTangentSpaceColor;

float _CrystalUseInnerGlow;
float4 _CrystalInnerGlowColor;
float3 _CrystalInnerGlowCenter;
float _CrystalInnerGlowSize;

TEXTURE2D(_CrystalParallaxMap); SAMPLER(sampler_CrystalParallaxMap);
TEXTURE2D(_CrystalGlitterNoiseMap); SAMPLER(sampler_CrystalGlitterNoiseMap);
TEXTURE2D(_CrystalMatCapMap); SAMPLER(sampler_CrystalMatCapMap);
TEXTURE2D(_CrystalTangentSpaceMap); SAMPLER(sampler_CrystalTangentSpaceMap);

#endif
