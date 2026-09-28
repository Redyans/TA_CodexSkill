Shader "Valkyria/Chara/Chara_Eye_V2"
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
        [Foldout(1, 1, 0, 1)] _lighting("灯光_Foldout", Float) = 1
        [Foldout(2,2)]_EffectLight("特效用平行光_Foldout", Float) = 0
        [Toggle] _EffectLightToggle("开启特效用平行光", Float) = 0
        [Vector(3)]_EffetDirectionDir("特效用平行光方向", Vector) = (0, 0, 1, 1)
        _EffetLightStrength("特效用平行光强度", Float) = 1

        [Foldout(1, 1, 0, 1)] _Eye("眼睛_Foldout", Float) = 1
        _IrisMap ("虹膜贴图", 2D) = "white" {}
        
        _EmissionIntensity("自发光强度", Float) = 1.0
        _BackLightAttenuation("逆光衰减系数", Range(0, 1)) = 0.5

        [Enum_Switch(Parallax, Physical)] _Refraction("折射模式", Float) = 0
        
        [Switch(Parallax)]_ParallaxScale("视差缩放",Range(-1,1)) = 0.01
        [Switch(Parallax)]_ParallaxFadeStart("视差衰减起点", Range(0, 1)) = 0.35
        [Switch(Parallax)]_ParallaxFadeEnd("视差衰减终点", Range(0, 1)) = 0.78
        _RefractionCenterOffset("折射中心偏移", Vector) = (0, 0, 0, 0)

        [Switch(Physical)]_IOR("折射率",Range(1,3)) = 1.5
        [Switch(Physical)]_OffsetScale("偏移缩放",Range(-1,1))=1
        [Switch(Physical)]_PhysicalFadeStart("折射衰减起点", Range(0, 1)) = 0.35
        [Switch(Physical)]_PhysicalFadeEnd("折射衰减终点", Range(0, 1)) = 0.78

        

        
        
        [HideInInspector] _HeadUp("头部上方向向量", Vector) = (-1.0, 0.0, 0.0, 0.0)
        [HideInInspector] _HeadCenter("头部中心位置", Vector) = (0.0, 0.0, 0.0, 0.0)
        [HideInInspector] _HeadRight("头部右向向量", Vector) = (0.0, 0.0, -1.0, 0.0)
        [HideInInspector] _HeadForward("头部前向向量", Vector) = (0.0, 1.0, 0.0, 0.0)

        // [Header(EyelashStencil)]
		[HideInInspector] [Enum(UnityEngine.Rendering.Universal.Internal.StencilUsage)]_ELStencil("模板 ID", Float) = 16
		[HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)]_ELStencilComp("模板比较", Float) = 5
		[HideInInspector] [Enum(UnityEngine.Rendering.StencilOp)]_ELStencilOp("模板操作", Float) = 2
        [HideInInspector] _AngleThreshold("角度阈值", Float)=0.0

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

        // Pass
        // {
        //     Name "TransparentDepthPrepass"
        //     Tags { "LightMode" = "TransparentDepthPrepass" }

        //     Cull [_Cull]
        //     ZWrite On
        //     ZTest LEqual
        //     ColorMask 0
        //     Blend One Zero

        //     HLSLPROGRAM
        //     #pragma target 3.5
        //     #pragma vertex CharaTransparentDepthPrepassVertex
        //     #pragma fragment CharaTransparentDepthPrepassFragment
        //     #pragma multi_compile_instancing

        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        //     #include "Common/CharaTransparentDepthPrepass_V2.hlsl"
        //     ENDHLSL
        // }
        
        Pass
        {
            Tags
			{
				"LightMode" = "Eye"
			}

			Stencil
			{
				Ref[_ELStencil]
				Comp[_ELStencilComp]
				Pass[_ELStencilOp]
			}

			ZWrite [_ZwriteOp]
			ZTest [_ZTestMode]
			Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]

            
            HLSLPROGRAM
            // Temporary: compile character custom features into the base variant.
            #pragma target 3.5
            #pragma shader_feature_local _DEATH_ON
            #pragma shader_feature _REFRACTION_NONE _REFRACTION_PARALLAX _REFRACTION_PHYSICAL
            // #define _DEATH_ON 1
            // #define _REFRACTION_PARALLAX 1
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 normal: NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                float4 vertColor : COLOR;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 normalWS:TEXCOORD1;
                float4 vertexColor:TEXCOORD2;
                float4 positionCS : SV_POSITION;
                float3 positionWS: TEXCOORD3;
                float3 tangentDir : TEXCOORD4;
                float3 bitangentDir : TEXCOORD5;
                float2 eyeUV : TEXCOORD6;
            };

            // params
            #include "Chara_Eye_V2/Chara_Eye_V2_Bindings.hlsl"

            // 角色补光走全局 Shader 参数，和 Chara_PBRNew 保持一致，避免材质实例各自分裂状态。
            #include "Common/Chara_Globals_V2.hlsl"
            #include "Common/Chara_CommonHelpers_V2.hlsl"
            #include "Common/Chara_Dithering_V2.hlsl"


            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.positionWS = positionWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                
                o.uv = TRANSFORM_TEX(v.uv, _IrisMap);
                o.eyeUV = v.uv;
                o.vertexColor = v.vertColor;
                o.normalWS = TransformObjectToWorldNormal(v.normal.xyz);
                o.tangentDir = normalize(mul(unity_ObjectToWorld,float4(v.tangent.xyz,0.0)).xyz);
                o.bitangentDir = normalize(cross(o.normalWS,o.tangentDir)*v.tangent.w);
                return o;
            }

            // 眼球折射统一按 UV 半径做内强外弱衰减，避免整块虹膜一起平移或折射。
            float ComputeEyeRefractionFade(float2 eyeUV, float fadeStartParam, float fadeEndParam)
            {
                float2 eyeCenter = float2(0.5f, 0.5f) + _RefractionCenterOffset.xy;
                float2 eyeCenterOffset = eyeUV - eyeCenter;
                float eyeRadius = saturate(length(eyeCenterOffset) * 2.0f);
                float fadeStart = min(fadeStartParam, fadeEndParam);
                float fadeEnd = max(max(fadeStartParam, fadeEndParam), fadeStart + 0.001f);
                float fadeT = saturate((eyeRadius - fadeStart) / (fadeEnd - fadeStart));
                float refractionFade = 1.0f - fadeT;
                return refractionFade * refractionFade;
            }
            
            half4 frag (Varyings i) : SV_Target
            {
                ApplyCharacterDitheringBayer4x4(
                    i.positionCS,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 normalWS = normalize(i.normalWS);
                float2 screenUV = i.positionCS.xy / _ScreenParams.xy;
                Light mainLight = GetMainLight();
                float customRenderLightToggle = saturate(_GlobalCharacterRenderLightToggle);
                float effectLightToggle = saturate(_EffectLightToggle);
                float3 resolvedMainLightColor = CharaResolveMainLightColor(
                    mainLight.color,
                    customRenderLightToggle,
                    _GlobalCharacterRenderLightColor.rgb,
                    _GlobalCharacterRenderLightStrength,
                    effectLightToggle,
                    _EffetLightStrength);
                float3 eyeLightColor = lerp(1.0.xxx, resolvedMainLightColor, 0.5);
                float3 lDir = CharaResolveMainLightDirection(
                    mainLight.direction,
                    customRenderLightToggle,
                    _GlobalCharacterRenderLightDirection,
                    effectLightToggle,
                    _EffetDirectionDir);
                float forwardViewFactor = dot( normalize(_HeadForward.xyz), lDir) * 0.5 + 0.5;
                // forwardViewFactor = sigmoid(forwardViewFactor, _ForwardViewCenter, _ForwardViewSmooth);
                forwardViewFactor = pow(forwardViewFactor,2);
                half backLightAttenuation = lerp(_BackLightAttenuation, 1.0h, forwardViewFactor);
                // return float4(backLightAttenuation.xxx,1);
                // return backLightAttenuation;
                float2 irisUV = i.uv;
                float3x3 TBN = float3x3(i.tangentDir,i.bitangentDir,i.normalWS); // Tangent transform matrix

#ifdef _REFRACTION_PARALLAX
                float3 viewDirTS = normalize(mul(TBN, viewDir));
                // 眼球视差按可调半径范围衰减，内圈保留更多位移，外圈逐渐压到 0，避免测试角边缘拉伸。
                float parallaxDepth = ComputeEyeRefractionFade(i.eyeUV, _ParallaxFadeStart, _ParallaxFadeEnd);
                float parallaxViewZ = max(viewDirTS.z + 1.0f, 1.0f);
                float2 offset = (viewDirTS.xy / parallaxViewZ) * (_ParallaxScale * parallaxDepth);
                irisUV -= offset;
#elif defined(_REFRACTION_PHYSICAL)
                // model of eye cornea and aqueous humor
                float height = saturate( 1.0 - 18.4 * 0.1 * 0.1 );
                
                // compute refraction vector
                float n = 1.0 / _IOR;
                float w = n * dot( normalWS, viewDir ); // eta * cos theta_i
                float k = sqrt( 1.0 + ( w - n ) * ( w + n ) ); // sqrt(1-eta^2(1-cos theta^2_i)) 
                float3 refractedDirWS = n * viewDir - ( w - k ) * normalWS;

                float cosAlpha = dot(float3(0.0,0.0,1.0), refractedDirWS);// use (0,0,1) as temp forward vector
                float dist = height / cosAlpha;
                float3 offsetWS = dist * refractedDirWS;
                float2 offsetTS = mul(TBN,offsetWS);
                float physicalDepth = ComputeEyeRefractionFade(i.eyeUV, _PhysicalFadeStart, _PhysicalFadeEnd);
                irisUV -= (_OffsetScale * physicalDepth) * offsetTS; // in unity y axis is inverted
#endif
                
                // sample the texture
                half4 baseColor = SAMPLE_TEXTURE2D(_IrisMap, sampler_IrisMap, irisUV);
                half3 diffuse = baseColor.rgb;
                half3 finalColor = diffuse * _EmissionIntensity * eyeLightColor * backLightAttenuation;
                finalColor = ApplyCharaRedTint(finalColor);
                #if defined(_DEATH_ON)
                    float deathMask = SAMPLE_TEXTURE2D(_DeathMask, sampler_DeathMask, TransformDeathMaskUV(screenUV * _DeathMaskTiling.xy, _DeathMaskUVOffset, _DeathMaskUVRotation)).r;
                    half3 deathMaskedColor = finalColor * lerp(_DeathInnerTint.rgb, _DeathOuterTint.rgb, deathMask);
                    finalColor = lerp(finalColor, deathMaskedColor, saturate(_DeathMaskBlendStrength));
                    finalColor = ApplyCharaFresnel(finalColor, normalWS, viewDir, _DeathFresnelScale, _DeathFresnelPower, _DeathFresnelColor);
                #endif
                return half4(finalColor, _Alpha);
            }
            ENDHLSL
        }

//       Pass
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
