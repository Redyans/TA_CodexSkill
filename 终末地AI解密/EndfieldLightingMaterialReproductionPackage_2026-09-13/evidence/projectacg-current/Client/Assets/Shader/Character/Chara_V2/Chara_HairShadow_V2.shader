Shader "Valkyria/Chara/Chara_HairShadow_V2"
{
    Properties
    {
        [HideInInspector] _CharacterRedTint ("_CharacterRedTint", Range(0, 1)) = 0
        [HideInInspector] _CharacterRedTintColor ("_CharacterRedTintColor", Color) = (1, 0, 0, 1)

        [Foldout(1, 1, 0, 0)] _baseSetting("基础设置_Foldout", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTestMode", Float) = 4.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("CullMode", Float) = 0.0

        [Foldout(1, 1, 0, 1)] _shadow("Shadow_Foldout", Float) = 1
        _BaseMap("基础贴图 (R 透明度)", 2D) = "white" {}
        // _Alpha("透明度", Range(0, 1)) = 1
        _TransparentColor("透射颜色", Color) = (1, 1, 1, 1)

        [HideInInspector] _FaceStencilReadMask("Face Stencil Read Mask", Float) = 2
        [HideInInspector] [Enum(UnityEngine.Rendering.Universal.Internal.StencilUsage)] _ELStencil("模板 ID", Float) = 16
        [HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)] _ELStencilComp("模板比较", Float) = 7
        [HideInInspector] [Enum(UnityEngine.Rendering.StencilOp)] _ELStencilOp("模板操作", Float) = 2

    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Stencil
            {
                Ref [_FaceStencilReadMask]
                ReadMask [_FaceStencilReadMask]
                WriteMask 0
                Comp Equal
                Pass Keep
            }

            ZWrite Off
            ZTest [_ZTestMode]
            Cull [_Cull]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 baseUV : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float _CharacterRedTint;
                float4 _CharacterRedTintColor;
                float4 _TransparentColor;
                float4 _BaseMap_ST;
                float _Alpha;
                float _ZTestMode;
                float _Cull;
            CBUFFER_END

            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.baseUV = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half baseMask = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV).r;
                half3 finalColor = _TransparentColor.rgb;
                half finalAlpha = baseMask * _TransparentColor.a;

                clip(finalAlpha - 0.001h);
                finalColor = ApplyCharaRedTint(finalColor);
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    CustomEditor "Scarecrow.SimpleShaderGUI"
}
