Shader "Valkyria/Chara/Chara_Brow_V2"
{
    Properties
    {
        // RedTint (driven by script via MaterialPropertyBlock)
        [HideInInspector] _CharacterRedTint ("_CharacterRedTint", Range(0, 1)) = 0
        [HideInInspector] _CharacterRedTintColor ("_CharacterRedTintColor", Color) = (1, 0, 0, 1)
        [HideInInspector] _Alpha("Alpha", Range(0, 1)) = 1
        [HideInInspector] _CharacterDitheringFactor("抖动裁剪进度", Range(0, 1)) = 0
        [HideInInspector] _CharacterDitheringPixelSize("抖动像素尺寸", Range(1, 10)) = 1
        [Foldout(1, 1, 0, 0)] _baseSetting("渲染设置_Foldout", Float) = 1
        [Toggle] _ZwriteOp("ZWrite", Float) = 1.0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTestMode", Float) = 4.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("CullMode", Float) = 2.0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0.0
        [Foldout(1, 1, 0, 1)] _brow("眉毛_Foldout", Float) = 1
        _BaseMap ("基础贴图", 2D) = "white" {}

        _EmissionIntensity("自发光强度", Float) = 1.0
        _BackLightAttenuation("逆光衰减系数", Range(0, 1)) = 0.5
        [HideInInspector] _HeadUp("头部上方向向量", Vector) = (-1.0, 0.0, 0.0, 0.0)
        [HideInInspector] _HeadCenter("头部中心位置", Vector) = (0.0, 0.0, 0.0, 0.0)
        [HideInInspector] _HeadRight("头部右向向量", Vector) = (0.0, 0.0, -1.0, 0.0)
        [HideInInspector] _HeadForward("头部前向向量", Vector) = (0.0, 1.0, 0.0, 0.0)

        // [Header(EyelashStencil)]
		[HideInInspector] [Enum(UnityEngine.Rendering.Universal.Internal.StencilUsage)]_ELStencil("模板 ID", Float) = 16
		[HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)]_ELStencilComp("模板比较", Float) = 7 //GEqual
		[HideInInspector] [Enum(UnityEngine.Rendering.StencilOp)]_ELStencilOp("模板操作", Float) = 2

        [HideInInspector] [Toggle(_DEATH_ON)] _DEATH_ON ("_DEATH_ON", Float) = 0
        [HideInInspector] _DeathProgress ("_DeathProgress", Range(0, 1)) = 0
        [HideInInspector] _DeathMask ("_DeathMask", 2D) = "white" {}
        [HideInInspector] _DeathMaskTiling ("死亡遮罩平铺", Vector) = (1, 1, 0, 0)
        [HideInInspector] _DeathMaskUVOffset ("_DeathMaskUVOffset", Vector) = (0, 0, 0, 0)
        [HideInInspector] _DeathMaskUVRotation ("_DeathMaskUVRotation", Float) = 0
        [HideInInspector] _DeathInnerTint ("_DeathInnerTint", Color) = (1, 1, 1, 1)
        [HideInInspector] _DeathOuterTint ("_DeathOuterTint", Color) = (1, 1, 1, 1)
        [HideInInspector] _DeathMaskBlendStrength ("_DeathMaskBlendStrength", Range(0, 1)) = 1
        [HideInInspector] _DeathFresnelScale ("_DeathFresnelScale", Range(0, 10)) = 0
        [HideInInspector] _DeathFresnelPower ("_DeathFresnelPower", Range(0.1, 10)) = 1
        [HideInInspector] _DeathFresnelColor ("_DeathFresnelColor", Color) = (1, 1, 1, 1)

    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

//        Pass
//        {
//            Name "TransparentDepthPrepass"
//            Tags { "LightMode" = "TransparentDepthPrepass" }
//
//            Cull [_Cull]
//            ZWrite On
//            ZTest LEqual
//            ColorMask 0
//            Blend One Zero
//
//            HLSLPROGRAM
//            #pragma target 3.5
//            #pragma vertex CharaTransparentDepthPrepassVertex
//            #pragma fragment CharaTransparentDepthPrepassFragment
//            #pragma multi_compile_instancing
//
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            float _CharacterDitheringFactor;
//            float _CharacterDitheringPixelSize;
//            #define CHARA_TRANSPARENT_DEPTH_APPLY_DITHERING(positionCS) \
//                ApplyCharacterDitheringBayer4x4(positionCS, _CharacterDitheringFactor, _CharacterDitheringPixelSize)
//            #include "Common/Chara_Dithering_V2.hlsl"
//            #include "Common/CharaTransparentDepthPrepass_V2.hlsl"
//            #undef CHARA_TRANSPARENT_DEPTH_APPLY_DITHERING
//            ENDHLSL
//        }

        Pass
        {
            Tags
			{
				"LightMode" = "Eye"
			}

			Stencil
			{
				Ref[_ELStencil]
				Comp GEqual
				Pass[_ELStencilOp]
			}

			ZWrite [_ZwriteOp]
			ZTest [_ZTestMode]
			Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]
            
            HLSLPROGRAM
            #pragma target 3.5
            // Temporary: compile character custom features into the base variant.
            #pragma shader_feature_local _DEATH_ON
            // #define _DEATH_ON 1
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS       : SV_POSITION;
                float2 uv               : TEXCOORD0;
                float fogCoord          : TEXCOORD1;
                float3 positionWS          : TEXCOORD2;
            };

            #define CHARA_EYE_V2_VARIANT_BROW 1
            #include "Chara_Eye_V2/Chara_Eye_V2_Bindings.hlsl"

            #include "Common/Chara_Globals_V2.hlsl"
            #include "Common/Chara_CommonHelpers_V2.hlsl"
            #include "Common/Chara_Dithering_V2.hlsl"
            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionWS = positionWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                ApplyCharacterDitheringBayer4x4(
                    i.positionCS,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
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
                half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                half3 finalColor = baseColor * _EmissionIntensity * resolvedMainLightIntensity * backLightAttenuation;
                finalColor = ApplyCharaRedTint(finalColor);
                float2 screenUV = i.positionCS.xy / _ScreenParams.xy;
                #if defined(_DEATH_ON)
                    float deathMask = SAMPLE_TEXTURE2D(_DeathMask, sampler_DeathMask, TransformDeathMaskUV(screenUV * _DeathMaskTiling.xy, _DeathMaskUVOffset, _DeathMaskUVRotation)).r;
                    float3 deathMaskedColor = finalColor * lerp(_DeathInnerTint.rgb, _DeathOuterTint.rgb, deathMask);
                    finalColor = lerp(finalColor, deathMaskedColor, saturate(_DeathMaskBlendStrength));
                    // 眉毛保留死亡溶解与边缘表现，不使用死亡菲涅尔。
                #endif
                return half4(finalColor, _Alpha);
            }

            ENDHLSL
        }
        
//		Pass
//		{
//			Name "ShadowCaster"
//			Tags{"LightMode" = "ShadowCaster"}
//
//			ZWrite On
//			ZTest LEqual
//			ColorMask 0
//			Cull[_Cull]
//
//			HLSLPROGRAM
//			#pragma exclude_renderers gles gles3 glcore
//			#pragma target 4.5
//
//			// -------------------------------------
//			// Material Keywords
//			#pragma shader_feature_local_fragment _ALPHATEST_ON
//			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//
//			//--------------------------------------
//			// GPU Instancing
//			#pragma multi_compile_instancing
//			#pragma multi_compile _ DOTS_INSTANCING_ON
//
//			// -------------------------------------
//			// Universal Pipeline keywords
//
//			// This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
//			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
//
//			#pragma vertex ShadowPassVertex
//			#pragma fragment ShadowPassFragment
//
//			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
//			ENDHLSL
//		}
//
//		Pass
//		{
//			Name "DepthOnly"
//			Tags{"LightMode" = "DepthOnly"}
//
//			ZWrite On
//			ColorMask 0
//			Cull[_Cull]
//
//			HLSLPROGRAM
//			#pragma exclude_renderers gles gles3 glcore
//			#pragma target 4.5
//
//			#pragma vertex DepthOnlyVertex
//			#pragma fragment DepthOnlyFragment
//
//			// -------------------------------------
//			// Material Keywords
//			#pragma shader_feature_local_fragment _ALPHATEST_ON
//			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//
//			//--------------------------------------
//			// GPU Instancing
//			#pragma multi_compile_instancing
//			#pragma multi_compile _ DOTS_INSTANCING_ON
//
//			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
//			ENDHLSL
//		}
//
//		// This pass is used when drawing to a _CameraNormalsTexture texture
//		Pass
//		{
//			Name "DepthNormals"
//			Tags{"LightMode" = "DepthNormals"}
//
//			ZWrite On
//			Cull[_Cull]
//
//			HLSLPROGRAM
//			#pragma exclude_renderers gles gles3 glcore
//			#pragma target 4.5
//
//			#pragma vertex DepthNormalsVertex
//			#pragma fragment DepthNormalsFragment
//
//			// -------------------------------------
//			// Material Keywords
//			#pragma shader_feature_local _NORMALMAP
//			#pragma shader_feature_local _PARALLAXMAP
//			#pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
//			#pragma shader_feature_local_fragment _ALPHATEST_ON
//			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//
//			//--------------------------------------
//			// GPU Instancing
//			#pragma multi_compile_instancing
//			#pragma multi_compile _ DOTS_INSTANCING_ON
//
//			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
//			ENDHLSL
//		}
        
    }
     CustomEditor "Scarecrow.SimpleShaderGUI"
}
