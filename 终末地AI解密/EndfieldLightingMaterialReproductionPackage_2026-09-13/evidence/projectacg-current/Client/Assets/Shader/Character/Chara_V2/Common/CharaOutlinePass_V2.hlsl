// Purpose: Shared outline pass implementation with smooth-normal, mask, and clip-space offset support.
#ifndef CHARA_OUTLINE_PASS_INCLUDED
#define CHARA_OUTLINE_PASS_INCLUDED

#include "Chara_Globals_V2.hlsl"

#ifndef CHARA_OUTLINE_RUNTIME_ENABLED
#define CHARA_OUTLINE_RUNTIME_ENABLED 1
#endif

float _ACG_CharacterShaderDebugMode;

#ifndef CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS
#define CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS 0
#endif

#ifndef CHARA_OUTLINE_MASK_UV
#define CHARA_OUTLINE_MASK_UV(inputUv) TRANSFORM_TEX(inputUv, _OutlineMask)
#endif

#ifndef CHARA_OUTLINE_SAMPLE_BASE_COLOR
#define CHARA_OUTLINE_SAMPLE_BASE_COLOR(inputUv) SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, inputUv).rgb
#endif

#ifndef CHARA_OUTLINE_APPLY_DITHERING
#define CHARA_OUTLINE_APPLY_DITHERING(positionCS)
#endif

#if !CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS
TEXTURE2D(_BaseTex);
SAMPLER(sampler_BaseTex);
TEXTURE2D(_OutlineMask);
SAMPLER(sampler_OutlineMask);
#endif

struct OutlineAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float fogCoord : TEXCOORD1;
};

float3 GetOutlineFallbackTangentOS(float3 normalOS)
{
    float3 fallbackAxis = abs(normalOS.y) < 0.999 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0);
    return SafeNormalize(cross(fallbackAxis, normalOS));
}

float3 GetOutlineTangentOS(float3 normalOS, float4 tangentOS)
{
    float3 tangent = tangentOS.xyz;
    float tangentLengthSq = dot(tangent, tangent);
    return tangentLengthSq > 1e-6 ? tangent * rsqrt(tangentLengthSq) : GetOutlineFallbackTangentOS(normalOS);
}

float3 DecodeOutlineSmoothNormalOS(float3 baseNormalOS, float4 tangentOS, float4 color)
{
    float2 oct = color.rg * 2.0 - 1.0;
    float3 smoothNormalTS = float3(oct, 1.0 - dot(float2(1.0, 1.0), abs(oct)));
    float fold = max(-smoothNormalTS.z, 0.0);
    smoothNormalTS.xy += float2(
        smoothNormalTS.x >= 0.0 ? -fold : fold,
        smoothNormalTS.y >= 0.0 ? -fold : fold);
    smoothNormalTS = SafeNormalize(smoothNormalTS);
    float3 tangent = GetOutlineTangentOS(baseNormalOS, tangentOS);
    float tangentSign = abs(tangentOS.w) > 1e-6 ? sign(tangentOS.w) : 1.0;
    float3 bitangent = SafeNormalize(cross(baseNormalOS, tangent) * tangentSign * GetOddNegativeScale());
    return SafeNormalize(
        tangent * smoothNormalTS.x +
        bitangent * smoothNormalTS.y +
        baseNormalOS * smoothNormalTS.z);
}

float GetOutlineCameraFOV()
{
    float fov = atan(1.0 / unity_CameraProjection._m11) * 114.59156;
    return max(fov, 1e-4);
}

float GetOutlineCameraFovAndDistanceFixMultiplier(float positionVSZ)
{
    float fixMultiplier;
    if (unity_OrthoParams.w == 0.0)
    {
        float cameraFOV = GetOutlineCameraFOV();
        fixMultiplier = min(60.0 / cameraFOV, abs(positionVSZ)) * cameraFOV;
    }
    else
    {
        fixMultiplier = min(2.0, abs(unity_OrthoParams.y)) * 50.0;
    }

    return fixMultiplier * 0.00005;
}

float4 ApplyOutlineViewSpaceZOffsetPerspective(float4 positionCS, float viewSpaceZOffset)
{
    float floatEpsilon = 5.960464478e-8;
    float modifiedPositionVSZ = -max(_ProjectionParams.y + floatEpsilon, abs(positionCS.w) - viewSpaceZOffset);
    float modifiedPositionCSZ = modifiedPositionVSZ * UNITY_MATRIX_P[2].z + UNITY_MATRIX_P[2].w;
    positionCS.z = modifiedPositionCSZ * positionCS.w / (-modifiedPositionVSZ);
    return positionCS;
}

float4 ApplyOutlineViewSpaceZOffsetOrtho(float4 positionCS, float viewSpaceZOffset)
{
    float zOffsetCS = viewSpaceZOffset / (_ProjectionParams.z - _ProjectionParams.y);
    zOffsetCS *= UNITY_NEAR_CLIP_VALUE > 0 ? 1.0 : -2.0;
    positionCS.z += zOffsetCS;
    return positionCS;
}

float4 ApplyOutlineViewSpaceZOffset(float4 positionCS, float viewSpaceZOffset)
{
    return unity_OrthoParams.w != 0.0
        ? ApplyOutlineViewSpaceZOffsetOrtho(positionCS, viewSpaceZOffset)
        : ApplyOutlineViewSpaceZOffsetPerspective(positionCS, viewSpaceZOffset);
}

OutlineVaryings OutlinePassVertex(OutlineAttributes input)
{
    OutlineVaryings output = (OutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);

    output.uv = input.uv;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.fogCoord = ComputeFogFactor(output.positionCS.z);

#ifdef _OUTLINE_ON
    if (CHARA_OUTLINE_RUNTIME_ENABLED && _GlobalCharacterRenderOutlineToggle >= 0.5)
    {
        float3 baseNormalOS = SafeNormalize(input.normalOS);
        float3 smoothNormalOS = DecodeOutlineSmoothNormalOS(baseNormalOS, input.tangentOS, input.color);
        float3 normalOS = lerp(baseNormalOS, smoothNormalOS, saturate(_SmoothNormal));

        float outlineMask = 1.0;
        #if !defined(CHARA_OUTLINE_IGNORE_MASK)
        if (_UseOutlineMask > 0.5)
        {
            float2 maskUV = CHARA_OUTLINE_MASK_UV(input.uv);
            outlineMask = SAMPLE_TEXTURE2D_LOD(_OutlineMask, sampler_OutlineMask, maskUV, 0).r;
        }
        #endif

        float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
        float3 positionVS = TransformWorldToView(positionWS);
        float3 scaleFactor = float3(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21), length(unity_ObjectToWorld._m02_m12_m22));
        positionWS += TransformObjectToWorldNormal(normalOS) * (max(_OutlineWidth, 0.0) * 0.01 * 1 * outlineMask * scaleFactor);

        float4 outlinePositionCS = TransformWorldToHClip(positionWS);
        float outlineZOffset = _OutlineClipSpaceZOffset * 0.0001;
        outlineZOffset *= max(1.0, GetOutlineCameraFovAndDistanceFixMultiplier(positionVS.z) / 0.0025);
        outlinePositionCS = ApplyOutlineViewSpaceZOffset(outlinePositionCS, -outlineZOffset);

        output.positionCS = outlinePositionCS;
        output.fogCoord = ComputeFogFactor(outlinePositionCS.z);
        // output.positionCS = TransformWorldToHClip(positionWS);
    }
#endif

    return output;
}

half4 OutlineExposureMaskPassFragment(OutlineVaryings input) : SV_Target
{
    CHARA_OUTLINE_APPLY_DITHERING(input.positionCS);
    clip(0.5 - _ACG_CharacterShaderDebugMode);

#ifdef _OUTLINE_ON
    if (CHARA_OUTLINE_RUNTIME_ENABLED && _GlobalCharacterRenderOutlineToggle >= 0.5)
        return half4(1.0h, 0.0h, 0.0h, 1.0h);
#endif

    // 描边关闭时不覆盖已写入的角色本体 Mask。
    clip(-1.0);
    return 0;
}

half4 OutlinePassFragment(OutlineVaryings input) : SV_Target
{
    CHARA_OUTLINE_APPLY_DITHERING(input.positionCS);
    // 材质通道调试期间丢弃描边像素，避免描边颜色和深度遮挡调试结果。
    clip(0.5 - _ACG_CharacterShaderDebugMode);

#ifdef _OUTLINE_ON
    if (CHARA_OUTLINE_RUNTIME_ENABLED && _GlobalCharacterRenderOutlineToggle >= 0.5)
    {
        half3 baseColor = CHARA_OUTLINE_SAMPLE_BASE_COLOR(input.uv);
        half3 outlineColor = lerp(_OutlineColor.rgb, baseColor * _OutlineColor.rgb, saturate(_OutlineMultiplyBaseColor));
        half3 foggedColor = MixFog(outlineColor, input.fogCoord);
        outlineColor = lerp(outlineColor, foggedColor, saturate(_FogIntensity));
        return half4(outlineColor, 1.0h);
    }
#endif

    return 0;
}

#endif
