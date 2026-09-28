Shader "Valkyria/Chara/Chara_EyeBlend_V2"
{
    Properties
    {
        // RedTint (driven by script via MaterialPropertyBlock)
        [HideInInspector] _CharacterRedTint ("_CharacterRedTint", Range(0, 1)) = 0
        [HideInInspector] _CharacterRedTintColor ("_CharacterRedTintColor", Color) = (1, 0, 0, 1)
  

        [Foldout(1, 1, 0, 0)] _baseSetting("渲染设置_Foldout", Float) = 1
        [Toggle] _ZwriteOp("ZWrite", Float) = 1.0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTestMode", Float) = 4.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("CullMode", Float) = 2.0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0.0
        _Alpha("Alpha", Range(0, 1)) = 1
        [HideInInspector] _CharacterDitheringFactor("抖动裁剪进度", Range(0, 1)) = 0
        [HideInInspector] _CharacterDitheringPixelSize("抖动像素尺寸", Range(1, 10)) = 1

        [Foldout(1, 1, 0, 1)] _EyeBlend("脸部半透_Foldout", Float) = 1
        [Toggle_Switch] _Emoji("Emoji表情", Float) = 0
        [NoScaleOffset] _MatCap("MatCap/Emoji", 2D) = "white" {}
        _MatCapIntensity("亮度", Range(0, 10)) = 1
        _BackLightAttenuation("逆光衰减系数", Range(0, 1)) = 0.5
        [Switch(FlatSphere)] _MatCapSphereLerp("MatCap Sphere Distortion", Range(0, 1)) = 1

        [HideInInspector] _HeadUp("头部上方向向量", Vector) = (-1.0, 0.0, 0.0, 0.0)
        [HideInInspector] _HeadCenter("头部中心位置", Vector) = (0.0, 0.0, 0.0, 0.0)
        [HideInInspector] _HeadRight("头部右向向量", Vector) = (0.0, 0.0, -1.0, 0.0)
        [HideInInspector] _HeadForward("头部前向向量", Vector) = (0.0, 1.0, 0.0, 0.0)

    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite [_ZwriteOp]
            ZTest [_ZTestMode]
            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma only_renderers gles3 vulkan d3d11 metal
            #pragma vertex vert
            #pragma fragment frag

             #pragma shader_feature_local _EMOJI_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 tangentDir : TEXCOORD1;
                float3 bitangentDir : TEXCOORD2;
                float2 eyeUV : TEXCOORD3;
            };

            #define CHARA_EYE_V2_VARIANT_EYEBLEND 1
            #include "Chara_Eye_V2/Chara_Eye_V2_Bindings.hlsl"

            #include "Common/Chara_Globals_V2.hlsl"
            #include "Common/Chara_CommonHelpers_V2.hlsl"
            #include "Common/Chara_Dithering_V2.hlsl"
            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.eyeUV = v.uv;
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.tangentDir = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)).xyz);
                o.bitangentDir = normalize(cross(o.normalWS, o.tangentDir) * v.tangent.w);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                ApplyCharacterDitheringBayer4x4(
                    i.vertex,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
                float3 normalWS = normalize(i.normalWS);
                float3x3 TBN = float3x3(normalize(i.tangentDir), normalize(i.bitangentDir), normalWS);
                // 眼球 MatCap 固定使用基于 UV 重建的球面法线，避免平面眼片退回到 mesh normal 映射。
                float2 sphereXY = (i.eyeUV - 0.5f) * 2.0f;
                float sphereZ = sqrt(saturate(1.0f - dot(sphereXY, sphereXY)));
                float3 sphereNormalTS = normalize(float3(sphereXY, sphereZ));
                float3 sphereNormalWS = normalize(TransformTangentToWorld(sphereNormalTS, TBN));
                float3 matCapNormalWS = normalize(lerp(normalWS, sphereNormalWS, _MatCapSphereLerp));

                half3 normalVS = normalize(TransformWorldToViewDir(normalize(sphereNormalWS)));
                half2 matCapUV = normalVS.xy * 0.5h + 0.5h;
                #ifdef _EMOJI_ON
                    matCapUV = i.eyeUV;
                #endif
                
                half4 matCapSample = SAMPLE_TEXTURE2D(_MatCap, sampler_MatCap, matCapUV);
                Light mainLight = GetMainLight();
                float customRenderLightToggle = saturate(_GlobalCharacterRenderLightToggle);
                float3 resolvedMainLightColor = CharaResolveMainLightColor(
                    mainLight.color,
                    customRenderLightToggle,
                    _GlobalCharacterRenderLightColor.rgb,
                    _GlobalCharacterRenderLightStrength,
                    0.0,
                    float3(0.0, 0.0, 0.0));
                float resolvedMainLightIntensity = CharaGetLightIntensity(resolvedMainLightColor);
                float3 lDir = CharaResolveMainLightDirection(
                    mainLight.direction,
                    customRenderLightToggle,
                    _GlobalCharacterRenderLightDirection,
                    0.0,
                    float3(0.0, 0.0, 0.0));
                float forwardViewFactor = dot( normalize(_HeadForward.xyz), lDir) * 0.5 + 0.5;
                forwardViewFactor = pow(forwardViewFactor,2);
                half backLightAttenuation = lerp(_BackLightAttenuation, 1.0h, forwardViewFactor);
                half3 rgb = matCapSample.rgb * _MatCapIntensity * resolvedMainLightIntensity * backLightAttenuation;
                half finalAlpha = saturate(matCapSample.a  * backLightAttenuation * _Alpha);

                rgb = ApplyCharaRedTint(rgb);
                return half4(rgb, finalAlpha);
            }
            ENDHLSL
        }
    }

    CustomEditor "Scarecrow.SimpleShaderGUI"
}
