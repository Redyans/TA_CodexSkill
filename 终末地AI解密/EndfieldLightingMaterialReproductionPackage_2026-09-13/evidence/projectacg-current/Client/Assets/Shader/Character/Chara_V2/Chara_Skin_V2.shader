Shader "Valkyria/Chara/Chara_Skin_V2"
{
    //// ramp混合保留少前那套，ao阴影颜色剔除lambort暗部
    Properties
    {
        // 受击泛红由 CharacterRedTintController 通过 MaterialPropertyBlock 驱动。
        [HideInInspector] _CharacterRedTint ("_CharacterRedTint", Range(0, 1)) = 0
        [HideInInspector] _CharacterRedTintColor ("_CharacterRedTintColor", Color) = (1, 0, 0, 1)

        [Foldout(1, 1, 0, 0)] _baseSetting("渲染设置_Foldout", Float) = 1
        [Toggle_Switch] _AlphaTest("Alpha Test", Float) = 0
        [Switch(_AlphaTest)]_Clip("Clip", Range(0, 1)) = 0.5
        _Alpha("Alpha", Range(0, 1)) = 1
        [HideInInspector] _CharacterDitheringFactor("抖动裁剪进度", Range(0, 1)) = 0
        [HideInInspector] _CharacterDitheringPixelSize("抖动像素尺寸", Range(1, 10)) = 1
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

        [HideInInspector] _VirtualLightMode("主光调整模式", Float) = 0
        [HideInInspector] _VirtualLightColor("逆光补光颜色", Color) = (1, 1, 1, 1)
        [HideInInspector] _VirtualLightIntensity("逆光补光强度", Range(0, 2)) = 0.2
        [HideInInspector] _VirtualLightOffset("逆光补光兰伯特偏移", Range(-1, 1)) = 0

        [Foldout(2, 2, 0, 1)]_TopLight("顶光_Foldout", Float) = 0
        [Tip(Warning)] _TopLightTip("参数统一：顶光兰伯特偏移 = 0；顶光强度 = 0.5；顶光强度整体偏移 = 0。", Float) = 0
        _OtherLightColor("顶光颜色", Color) = (1, 1, 1, 1)
        _OtherLightOffset("顶光兰伯特偏移", Range(-1, 1)) = 0
        _OtherLightStrength("顶光强度", Range(0, 2)) = 0
        _OtherLightStrength_Offset("顶光强度整体偏移", Range(-1, 1)) = 0

        [Foldout(1, 1, 0, 1)] _surface("表面_Foldout", Float) = 1
        [NoScaleoffset][Tex(_BaseColor)]_BaseMap("基础贴图", 2D) = "white" {}
        [HideInInspector]_BaseColor("基础颜色", Color) = (1,1,1,1)
        _BaseColorPower("基础颜色Power", Range(0, 4)) = 1
        [Toggle_Switch] _NeedNormalMap("启用法线", Float) = 1
        [Tex(_NormalScale, _NeedNormalMap)][NoScaleoffset]_NormalMap("法线贴图", 2D) = "bump" {}
        [HideInInspector]_NormalScale("法线强度", Range(0,4)) = 1
        [NoScaleoffset][Tex]_MixMap("Mask：金属(R) 光泽(G) AO(B) 脖子阴影(A)", 2D) = "white" {}
        [NoScaleoffset][Tex(_UseSkinLut)] _LutColorTex("肤色LUT", 2D) = "white" {}
        [HideInInspector][Toggle_Switch] _UseSkinLut("Use Skin LUT", Float) = 0
        [Foldout(2,2)]_MRAChange("金属粗糙AO_Foldout", Float) = 1
        
        [HideInInspector] _MetallicOffset("兼容旧材质金属度", Range(0,1)) = 1.0
        _ReflectivityStrength("反射率强度", Range(0.01, 2)) = 1
        _AOOffset("AO 强度", Range(0, 1)) = 1
        _AOContrast("AO 对比度", Range(0, 4)) = 1
        _AOPower("AO Power", Range(0, 4)) = 1
        _RoughnessOffset("粗糙度强度", Range(0, 1)) = 1
        _RoughnessContrast("粗糙度对比度", Range(0, 4)) = 1

        [Foldout(1, 1, 0, 1)] _directLight("直接漫反射_Foldout", Float) = 1
        _AlbedoDarkStrength("暗部颜色强度", Range(0, 1)) = 0.5
        _AlbedoDarkSaturation("暗部饱和度", Range(0, 2)) = 1
        _ChinDarkStrength("脖子阴影强度", Range(0, 1)) = 1
        _ChinDarkSaturation("脖子阴影饱和度", Range(0, 2)) = 1
        _ShadowColor("AO阴影颜色", Color) = (1, 1, 1, 1)
        [GradientSplit][NoScaleoffset]_RampMap("渐变贴图", 2D) = "white" {}
        _RampSaturationStrength("渐变色饱和度影响强度", Range(0, 1)) = 1
        _RampStrength("渐变色强度", Range(0, 1)) = 1
        _RampColorNoLStrength("渐变A通道混合程度", Range(0, 1)) = 1

        [Foldout(1, 1, 0, 1)] _directSpecular("直接高光_Foldout", Float) = 1
        _SpecularColor("高光颜色", Color) = (1, 1, 1, 1)
        _SpecularStrength("高光强度", Range(0, 4)) = 1
        _SpecularShadowStrength("阴影作用强度", Range(0, 1)) = 1
        _SelfAoShadowStrength("高光AO保留", Range(0, 1)) = 1
        _ForwardDirStrength("视线朝向权重", Range(0, 1)) = 0

        [Foldout(1, 1, 0, 1)] _IndirectLight("间接光照_Foldout", Float) = 1
        [Foldout(2, 2, 0, 1)] _ibl("间接反射高光_Foldout", Float) = 1
        [Tex(_EnvColor)][NoScaleoffset]_EnvMap("环境贴图", Cube) = "black" {}
        _EnvRotation("环境旋转", Range(-180, 180)) = 0
        [HideInInspector]_EnvColor("环境颜色", Color) = (1, 1, 1, 1)
        _EnvLightStrength("环境光强度", Range(0, 2)) = 1
        [Foldout(2, 2, 0, 1)] _ibl("间接漫反射_Foldout", Float) = 1
        [Tip(Warning)] _IndirectDiffuseTip("参数统一：间接漫反射强度 = 0.2。", Float) = 0
        _IndirectLightIntensity("间接漫反射强度", Range(0, 2)) = 0.2
        _IndirectTintColor("间接漫反射颜色", Color) = (0.8, 0.85, 1.0, 0)
        _IndirectDifficusPlaneVolume("间接漫反射0平1体感", Range(0, 1)) = 0.5

        
        
        [Foldout(1, 1, 1, 0)] _fresnel("菲涅尔_Foldout", Float) = 0
        _FresnelPower("菲涅尔Power", Range(0, 10)) = 1
        _FresnelInnerColor("菲涅尔内侧颜色", Color) = (1, 1, 1, 1)
        _FresnelOuterColor("菲涅尔外侧颜色", Color) = (1, 1, 1, 1)
        _FresnelSmoothMin("菲涅尔平滑最小值", Range(0, 1)) = 0
        _FresnelSmoothMax("菲涅尔平滑最大值", Range(0, 2)) = 1
        _FresnelStrength("菲涅尔强度", Range(0, 1)) = 1
        _FresnelMaskStrength("菲涅尔遮罩强度", Range(0, 1)) = 1

        [Foldout(1, 1, 1, 0)] _rim("边缘光_Foldout", Float) = 0
        [Enum_Switch(FresnelRim, DepthRim, Add)] _RimType("边缘光类型", float) = 0
        [HideInInspector] _CustomLightDirection("自定义边缘光方向", float) = 0
        [HideInInspector] _CustomRimDirectionSpace("自定义边缘光方向空间", float) = 1
        [HideInInspector] _FresnelRimDirection("自定义边缘光方向", Vector) = (0, 0, 0, 0)
        [HideInInspector] _FresnelRimLightAlign("边缘光对光方向", Range(-1, 1)) = 1
        [HDR] _FresnelRimColor("边缘光颜色", Color) = (1, 1, 1, 1)
        _FresnelRimPower("边缘光范围", Range(0, 2)) = 0.5
        _FresnelRimSmooth("边缘光平滑", Range(0.001, 1)) = 0.001
        [Switch(FresnelRim,Add)]_FresnelIntensity("菲涅尔边缘光强度", Range(0, 10)) = 3
        [Switch(DepthRim,Add)]_DepthRimWidthX("深度边缘光宽度 X", Range(0, 1)) = 0.5
        [Switch(DepthRim,Add)] _DepthRimWidthY("深度边缘光宽度 Y", Range(0, 1)) = 0.5
        [Switch(DepthRim,Add)] _DepthRimIntensity("深度边缘光强度", Range(0, 10)) = 3

        [Foldout(2, 2, 0, 1)] _RimOtherSetting("边缘光其他设置_Foldout", Float) = 1
        _RimLightDiffuseColorEffect("边缘光受底色影响", Range(0, 1)) = 0
        _RimShadowStrength("边缘光阴影作用强度", Range(0, 1)) = 0
        _RimNormalBlend("边缘光模型法线/法线贴图融合", Range(0, 1)) = 1
        [HideInInspector] _RimMaskRootPosition("边缘光遮罩中心世界坐标", Vector) = (0, 0, 0, 0)
        [HideInInspector] _UseRimFakePointMask("启用模拟点灯遮罩", float) = 0
        [HideInInspector] _RimFakePointMaskPosition("模拟点灯位置偏移（随自定义方向空间）", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RimFakePointMaskRange("模拟点灯范围", Range(0.01, 10)) = 1
        [HideInInspector] _RimFakePointMaskPower("模拟点灯遮罩软硬", Range(0.25, 8)) = 2
        [HideInInspector] _UseRimFakeDirectMask("启用模拟直接光遮罩", float) = 0
        [HideInInspector] _RimFakeDirectMaskDirection("模拟直接光切面方向", Vector) = (1, 0, 0, 0)
        [HideInInspector] _RimFakeDirectMaskPosition("模拟直接光切面位置偏移（随自定义方向空间）", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RimFakeDirectMaskRange("模拟直接光范围", Range(0.01, 10)) = 1

        

        [Foldout(1, 1, 1, 0)] _outline("描边_Foldout", Float) = 0
        [Toggle_Switch] _SmoothNormal("平滑法线", Float) = 1.0
        [Toggle_Switch] _OutlineMultiplyBaseColor("描边乘自身颜色", Float) = 1.0
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineWidth("描边宽度", Range(0, 1)) = 0.5
        _OutlineClipSpaceZOffset("描边 Z 偏移", Range(-15, 15)) = 0.0
        [Toggle_Switch] _UseOutlineMask("使用描边遮罩", Float) = 0
        [Switch(_UseOutlineMask)][NoScaleoffset][Tex] _OutlineMask("描边遮罩", 2D) = "white" {}
        [HideInInspector] _FogIntensity("雾效强度", Range(0, 1)) = 1
        
        [Foldout(1, 1, 0, 1)] _ShadowControl("阴影_Foldout", Float) = 1
        [Foldout(2, 2)] _CSMPerShadow("CSMPerObject阴影_Foldout", Float) = 1
        [Enum_Switch(Off, UnityOnly, POSOnly, Both)] _UsePerObjectShadow("Per Object Shadow Mode", Float) = 0
        [Switch(UnityOnly, Both, POSOnly)]_SceneShadowCenter("场景阴影中心", Range(0, 1)) = 0.5
        [Switch(UnityOnly, Both, POSOnly)]_SceneShadowSmooth("场景阴影平滑", Range(0, 1)) = 0.1
        [Switch(UnityOnly, Both, POSOnly)]_ShadowStrength("场景阴影强度", Range(0, 1)) = 1
        [Foldout(2, 2, 1, 0)] _planarShadow("平面阴影_Foldout", Float) = 0
        _PlanarShadowFalloff("平面阴影衰减", Float) = 1.0
        _PlanarShadowRange("平面阴影范围", Float) = 0.0
        _PlanarShadowLightDir("光照方向 xyz + 平面高度 w", Vector) = (0, 1, 0, 0)
        _PlanarShadowGlobalCenter("平面阴影全局中心", Vector) = (0, 0, 0, 0)
        _PlanarShadowColor("平面阴影颜色", Color) = (0, 0, 0, 1)

        [Foldout(1, 1, 0, 1)] _finalColorGradient("最终颜色渐变_Foldout", Float) = 1
        _GradientColor("渐变颜色", Color) = (1, 1, 1, 1)
        _GradientMinY("渐变最小 Y", Float) = 0.0
        _GradientMaxY("渐变最大 Y", Float) = 1.0
        
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

        [Foldout_Out]
        _FoldoutOut("Foldout Out_Foldout", Float) = 1
    }

    SubShader
    {
        Tags {  "Queue"="Geometry" "RenderType"="Opaque" }

        HLSLINCLUDE
        // 核心库
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Common/Chara_AdditionalLights_V2.hlsl"
        #include "Common/ToonBRDF_V2.hlsl"
        #include "Common/Func_Chara_PerObjectShadow_V2.hlsl"
        #include "Common/Chara_CommonHelpers_V2.hlsl"
        #include "Common/Chara_GlobalVirtualLight_V2.hlsl"

        #include "Chara_Skin_V2/Chara_Skin_V2_Bindings.hlsl"
        #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
        #include "Common/Chara_FinalColorGradient_V2.hlsl"
        #include "Common/Chara_ShaderDebug_V2.hlsl"
        #include "Common/Chara_Dithering_V2.hlsl"

        #define CHARA_RIM_MASK_USE_ROOT_POSITION
        #define CHARA_RIM_USE_GLOBAL_CONTROLS
        #include "Common/Chara_RimShared_V2.hlsl"
        #include "Common/Chara_Globals_V2.hlsl"

        float3 ToonSkinLinearToSrgb(float3 color)
        {
            float3 safeColor = max(color, 0.0);
            float3 linearSegment = safeColor * 12.9200001;
            float3 curveSegment = 1.05499995 * pow(max(safeColor, 1.0e-6), 1.0 / 2.4) - 0.0549999997;
            return lerp(curveSegment, linearSegment, step(safeColor, 0.00313080009));
        }

        float3 ToonSkinSampleSkinLutColor(float3 albedoLinear)
        {
            float3 albedoSrgb = ToonSkinLinearToSrgb(albedoLinear.brg);
            float2 lutUV = albedoSrgb.xz * float2(31.0, 0.96875);
            float lutFloorX = floor(lutUV.x);
            float2 lutUVYZ = albedoSrgb.yz * float2(0.0302734375, 0.96875) + float2(0.00048828125, 0.015625);
            float lutUVX = lutFloorX * 0.03125 + lutUVYZ.x;
            float2 lutUVFinal = float2(lutUVX, lutUVYZ.y);

            float lutLerp = albedoSrgb.x * 31.0 - lutFloorX;
            float3 lut0 = SAMPLE_TEXTURE2D(_LutColorTex, sampler_LutColorTex, lutUVFinal).rgb;
            float3 lut1 = SAMPLE_TEXTURE2D(_LutColorTex, sampler_LutColorTex, lutUVFinal + float2(0.03125, 0.015625)).rgb;
            return lerp(lut0, lut1, lutLerp);
        }

        struct a2v
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 texcoord   : TEXCOORD0;
            float2 texcoord2  : TEXCOORD2;
            float4 tangentOS  : TANGENT;
            float4 color      : COLOR;
        };

        struct v2f
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float2 gradientUV : TEXCOORD1;
            float3 normalWS   : TEXCOORD3;
            float3 tangentWS  : TEXCOORD4;
            float3 bitangentWS: TEXCOORD5;
            float3 positionWS : TEXCOORD6;
            // 手动声明 shadowCoord，避开宏解析失败的问题
            float4 shadowCoord : TEXCOORD7; 
            float3 smoothNormalVertexColor : TEXCOORD8;
        };

        // ShadowCaster / DepthOnly / DepthNormals helpers are kept local so
        // Chara_Skin_V2 remains self-contained while the three passes still
        // share one implementation inside this shader file.
        float3 _LightDirection;
        float3 _LightPosition;

        struct SkinShadowAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct SkinShadowVaryings
        {
            float4 positionCS : SV_POSITION;
        };

        float4 GetSkinShadowPositionHClip(SkinShadowAttributes input)
        {
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

        #if _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 lightDirectionWS = normalize(_LightPosition - positionWS);
        #else
            float3 lightDirectionWS = _LightDirection;
        #endif

            float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

        #if UNITY_REVERSED_Z
            positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
        #else
            positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
        #endif

            return positionCS;
        }

        SkinShadowVaryings ShadowPassVertex(SkinShadowAttributes input)
        {
            SkinShadowVaryings output = (SkinShadowVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            output.positionCS = GetSkinShadowPositionHClip(input);
            return output;
        }

        half4 ShadowPassFragment(SkinShadowVaryings input) : SV_TARGET
        {
            ApplyCharacterDitheringBayer4x4(
                input.positionCS,
                _CharacterDitheringFactor,
                _CharacterDitheringPixelSize);
            return 0;
        }

        struct SkinDepthOnlyAttributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct SkinDepthOnlyVaryings
        {
            float4 positionCS : SV_POSITION;
        };

        SkinDepthOnlyVaryings DepthOnlyVertex(SkinDepthOnlyAttributes input)
        {
            SkinDepthOnlyVaryings output = (SkinDepthOnlyVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            return output;
        }

        half DepthOnlyFragment(SkinDepthOnlyVaryings input) : SV_TARGET
        {
            ApplyCharacterDitheringBayer4x4(
                input.positionCS,
                _CharacterDitheringFactor,
                _CharacterDitheringPixelSize);
            return input.positionCS.z;
        }

        struct SkinDepthNormalsAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct SkinDepthNormalsVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float4 tangentWS : TEXCOORD2;
        };

        SkinDepthNormalsVaryings DepthNormalsVertex(SkinDepthNormalsAttributes input)
        {
            SkinDepthNormalsVaryings output = (SkinDepthNormalsVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);

            VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.texcoord;
            output.normalWS = normalInputs.normalWS;

            float tangentSign = input.tangentOS.w * GetOddNegativeScale();
            output.tangentWS = float4(normalInputs.tangentWS.xyz, tangentSign);
            return output;
        }

        void DepthNormalsFragment(
            SkinDepthNormalsVaryings input,
            out half4 outNormalWS : SV_Target0
        #ifdef _WRITE_RENDERING_LAYERS
            , out float4 outRenderingLayers : SV_Target1
        #endif
        )
        {
            float2 normalUV = input.uv.xy * _NormalMap_ST.xy + _NormalMap_ST.zw;
            float4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV);
            float3 normalTS = UnpackNormalFromTex(normalTex, _NormalScale);
            float3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
            float3 mappedNormalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS)));
            float3 normalWS = normalize(lerp(input.normalWS, mappedNormalWS, saturate(_NeedNormalMap)));

        #if defined(_GBUFFER_NORMALS_OCT)
            float2 octEncoded = PackNormalOctQuadEncode(normalWS);
            float2 remappedOct = saturate(octEncoded * 0.5 + 0.5);
            half3 packedNormalWS = PackFloat2To888(remappedOct);
            outNormalWS = half4(packedNormalWS, 0.0h);
        #else
            outNormalWS = half4(normalWS, 0.0h);
        #endif

        #ifdef _WRITE_RENDERING_LAYERS
            uint renderingLayers = GetMeshRenderingLayer();
            outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
        #endif
        }

        ENDHLSL
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
//            #define CHARA_TRANSPARENT_DEPTH_APPLY_DITHERING(positionCS) \
//                ApplyCharacterDitheringBayer4x4(positionCS, _CharacterDitheringFactor, _CharacterDitheringPixelSize)
//            #include "Common/Chara_Dithering_V2.hlsl"
//            #include "Common/CharaTransparentDepthPrepass_V2.hlsl"
//            #undef CHARA_TRANSPARENT_DEPTH_APPLY_DITHERING
//            ENDHLSL
//        }
        // ------------------------------------------------------------------
        // Pass 1: Forward Lit (渲染物体并接收阴影)
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite [_ZwriteOp]
            ZTest [_ZTestMode]
            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VERT
            #pragma fragment FRAG_ToonSkinV2
            #pragma shader_feature_local _FRESNEL_ON
            #pragma shader_feature_local _RIM_ON
            #pragma multi_compile_local _ _DEATH_ON

            // 必须定义的阴影关键字，否则 GetMainLight 不会有阴影数据
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            v2f VERT(a2v i)
            {
                v2f o;
                // 基础坐标转换
                VertexPositionInputs posInputs = GetVertexPositionInputs(i.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(i.normalOS, i.tangentOS);

                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.uv = i.texcoord;
                o.gradientUV = i.texcoord2;
                o.smoothNormalVertexColor = i.color.rgb;
                
                o.normalWS = normInputs.normalWS;
                o.tangentWS = normInputs.tangentWS;
                o.bitangentWS = normInputs.bitangentWS;

                // 【关键】手动计算并传递阴影坐标，不依赖宏
                o.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);
                
                return o;
            }

            float4 FRAG_ToonSkinV2(v2f i) : SV_TARGET
            {
                ApplyCharacterDitheringBayer4x4(
                    i.positionCS,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
                float2 baseUV = i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                float2 normalUV = i.uv * _NormalMap_ST.xy + _NormalMap_ST.zw;
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
                float4 mixMap = SAMPLE_TEXTURE2D(_MixMap, sampler_MixMap, i.uv);
                float4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV);

                float3 geometricNormalWS = normalize(i.normalWS);
                float3x3 tbn = float3x3(normalize(i.tangentWS), normalize(i.bitangentWS), geometricNormalWS);
                float3 normalTSRaw = UnpackNormalFromTex(normalTex);
                float3 normalTS = UnpackNormalFromTex(normalTex, _NormalScale);
                float3 normalWS = normalize(lerp(geometricNormalWS, normalize(mul(normalTS, tbn)), _NeedNormalMap));

                float3 baseColor = baseTex.rgb * _BaseColor.rgb;
                baseColor = pow(max(baseColor, 0.0), max(_BaseColorPower, 0.001));
                float fresnelMask = saturate(baseTex.a);
                float ao = saturate(mixMap.b * _AOOffset);
                ao = saturate((ao - 0.5) * _AOContrast + 0.5);
                ao = pow(ao, _AOPower);
                float roughness = 1 - mixMap.g;
                roughness = saturate(roughness * _RoughnessOffset);
                roughness = saturate((roughness - 0.5) * _RoughnessContrast + 0.5);
                roughness = max(roughness, HALF_MIN_SQRT);
                float chinDark = lerp(1.0, saturate(mixMap.a), saturate(_ChinDarkStrength));
                float debugRoughness = roughness;

                if (IsCharaShaderMaterialDebugEnabled())
                {
                    return GetCharaShaderDebugColor(baseColor, normalWS, normalTSRaw, normalTS, 0.0, debugRoughness, mixMap.rgb, i.smoothNormalVertexColor, 1.0);
                }

                Light mainLight = GetMainLight(i.shadowCoord);
                float effectLightToggle = saturate(_EffectLightToggle);
                float customRenderLightToggle = saturate(_GlobalCharacterRenderLightToggle);
                float3 mainLightColor = CharaResolveMainLightColor(
                    mainLight.color,
                    customRenderLightToggle,
                    _GlobalCharacterRenderLightColor.rgb,
                    _GlobalCharacterRenderLightStrength,
                    effectLightToggle,
                    _EffetLightStrength);
                float mainLightIntensity = CharaGetLightIntensity(mainLightColor);
                mainLightColor /= mainLightIntensity;

                float3 mainLightDir = mainLight.direction;
                float3 customRenderLightDirection = _GlobalCharacterRenderLightDirection;
                customRenderLightDirection = dot(customRenderLightDirection, customRenderLightDirection) < 1.0e-4 ? mainLightDir : normalize(customRenderLightDirection);
                float3 effectLightDirection = _EffetDirectionDir;
                effectLightDirection = dot(effectLightDirection, effectLightDirection) < 1.0e-4 ? mainLightDir : normalize(effectLightDirection);
                mainLightDir = normalize(lerp(mainLightDir, customRenderLightDirection, customRenderLightToggle));
                mainLightDir = normalize(lerp(mainLightDir, effectLightDirection, effectLightToggle));
                mainLightDir = CharaApplyGlobalVirtualLightAsMainLightDir(mainLightDir);

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                float3 cameraForward = normalize(UNITY_MATRIX_V[2].xyz);
                viewDir = normalize(lerp(viewDir, cameraForward, _ForwardDirStrength));

                float3 sssColorEffect = 1.0.xxx;
#ifdef _FRESNEL_ON
                float fresnelNoV = saturate(dot(normalWS, viewDir));
                float fresnelArea = saturate(1.0 - fresnelNoV * _FresnelSmoothMax + _FresnelSmoothMin);
                
                fresnelArea = pow(fresnelArea, max(_FresnelPower, 0.001));
                
                // fresnelArea = smoothstep(_FresnelSmoothMin, _FresnelSmoothMax, fresnelArea);
                fresnelArea *= lerp(1.0, fresnelMask, saturate(_FresnelMaskStrength));
                // return float4(_FresnelPower.xxx,1);
                sssColorEffect = lerp(_FresnelInnerColor.rgb, _FresnelOuterColor.rgb, fresnelArea) ;
                // return float4(fresnelArea.xxx,1);
#endif
                
                float3 albedoSssRefine = baseColor * lerp(1.0.xxx, sssColorEffect, _FresnelStrength);
                // return float4(albedoSssRefine,1);
                float3 albedoLight = albedoSssRefine * 0.96;
                float3 F0 = (0.04 * _ReflectivityStrength).xxx;
                float roughness2 = max(0.0078125, roughness * roughness);
                float3 albedoLutRefine = albedoSssRefine;
                if (_UseSkinLut > 0.5)
                {
                    albedoLutRefine = ToonSkinSampleSkinLutColor(albedoSssRefine);
                }

                float3 albedoDark = albedoLutRefine * 0.96 * _AlbedoDarkStrength;
                float albedoDarkStrength = MyCalculateLightStrength(albedoDark);
                albedoDark = lerp(albedoDarkStrength.xxx, albedoDark, _AlbedoDarkSaturation);
                float3 albedoDarkMore = albedoDark * 0.65;

                float shadowAttenuation = ApplyPerObjectShadow(mainLight.shadowAttenuation, i.positionCS, _UsePerObjectShadow);
                float sceneShadow = 1.0;
                if (_UsePerObjectShadow > 0.5)
                {
                    sceneShadow = SigmoidSharp(shadowAttenuation, _SceneShadowCenter, _SceneShadowSmooth);
                }
                sceneShadow = saturate(sceneShadow);

                float NoL = dot(normalWS, mainLightDir);
                float NoL_rampFinal = min(clamp(NoL, -1.0, 1.0) * 0.5 + 0.5, chinDark);
                float4 rampColor_NoL = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(NoL_rampFinal, 0.5));
                float remapNoL = lerp(NoL_rampFinal, rampColor_NoL.a, _RampColorNoLStrength);
                float rampColorNoLStrength = max(max(rampColor_NoL.r, rampColor_NoL.g), rampColor_NoL.b) - min(min(rampColor_NoL.r, rampColor_NoL.g), rampColor_NoL.b);

                float aoForLighting = lerp(1.0, ao, chinDark);
                float aoShadow = aoForLighting * sceneShadow;
                float minShadowEffect = min(min(aoForLighting, sceneShadow), remapNoL);
                float totalShadowArea = minShadowEffect;
                float otherLightNoL = saturate(_OtherLightOffset) * _OtherLightStrength + _OtherLightStrength_Offset;
                float3 otherLightColor = lerp(_OtherLightColor.rgb, 1.0.xxx, minShadowEffect);
                float3 mainLightTint = lerp(_ShadowColor.rgb, mainLightColor, totalShadowArea) * mainLightIntensity;
                float3 mainLightColorFinal = mainLightTint + otherLightColor * otherLightNoL;

                albedoDarkMore = lerp(albedoDarkStrength.xxx, albedoDarkMore, 1.2);
                float3 mainDiffuseColorDark = lerp(albedoDarkMore, albedoDark, saturate(aoShadow + remapNoL));
                float neckShadowMask = saturate(1.0 - chinDark);
                float neckShadowLuminance = dot(mainDiffuseColorDark, float3(0.2126, 0.7152, 0.0722));
                mainDiffuseColorDark = lerp(mainDiffuseColorDark, lerp(neckShadowLuminance.xxx, mainDiffuseColorDark, max(0.0, _ChinDarkSaturation)), neckShadowMask);
                float3 diffuseBRDF = lerp(mainDiffuseColorDark, albedoLight, totalShadowArea);
                float rampSaturationStrength = lerp(1.0, rampColorNoLStrength, _RampSaturationStrength);
                float3 rampColorNoLEffect = lerp(1.0.xxx, rampColor_NoL.rgb, rampSaturationStrength);
                rampColorNoLEffect = lerp(1.0.xxx, rampColorNoLEffect, _RampStrength);
                float3 diffuseBRDFRamp = diffuseBRDF * rampColorNoLEffect;
                float rampColorControl = clamp(MyCalculateLightStrength(diffuseBRDF) / max(0.01, MyCalculateLightStrength(diffuseBRDFRamp)), 0.0, 1.5);
                float3 diffuseResult = mainLightColorFinal * diffuseBRDFRamp * rampColorControl;
                diffuseResult += CharaGetGlobalVirtualBackLightResult(albedoLight, normalWS, mainLightDir);

                float3 forwardLightDir = normalize(float3(cameraForward.x, mainLightDir.y, cameraForward.z));
                float3 halfDir = normalize(viewDir * 3.0 + mainLightDir + 2.0 * forwardLightDir);
                float NoV = max(saturate(dot(normalWS, viewDir)), 1.0e-4);
                float NoH = saturate(dot(normalWS, halfDir));
                float a2 = roughness2 * roughness2;
                float specularD = (NoH * a2 - NoH) * NoH + 1.0;
                specularD = a2 / max(specularD * specularD, 1.0e-4);
                float specularV = 0.5 / max(NoV * 2.0 + roughness2 + 1.0e-4, 1.0e-4);
                float specularDV = clamp(specularD * specularV - 6.10351562e-05, 0.0, 20.0);
                float selfAoShadowStrength = min(_SelfAoShadowStrength, CharaResolveSpecularShadowStrength(_SpecularShadowStrength));
                float3 directSpec = mainLightColorFinal * lerp(selfAoShadowStrength, 1.0, totalShadowArea) * totalShadowArea * (specularDV * F0);
                directSpec *= _SpecularColor.rgb * _SpecularStrength;

                float3 indirectDiffusePlane = max(half3(0, 0, 0), half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w));
                float3 indirectDiffuseVolume = SampleSH(normalWS);
                float3 indirectDiffuse = lerp(lerp(indirectDiffusePlane, indirectDiffuseVolume, _IndirectDifficusPlaneVolume), 1.0.xxx, saturate(_GlobalCharacterRenderOverrideSceneAmbient)) * baseColor;
                indirectDiffuse *= _IndirectLightIntensity * _GlobalIndirecIntensity * (_IndirectTintColor.rgb * _GlobalIndirectTintColor);
                float3 finalColor = diffuseResult + directSpec + indirectDiffuse;

                float3 addLightDiffuse = 0.0;
#if defined(_ADDITIONAL_LIGHTS)
                half additionalLightsWeight = saturate(_CharacterRenderTimelineAdditionalLightsWeight);
                half additionalLightsEnabled = lerp(_GlobalCharacterAdditionalLightsEnabled, _CharacterRenderTimelineAdditionalLightsEnabled, additionalLightsWeight);
                half additionalLightDiffuseEnabled = lerp(_GlobalCharacterAdditionalLightsDiffuseEnabled, _CharacterRenderTimelineAdditionalLightsDiffuseEnabled, additionalLightsWeight);
                UNITY_BRANCH
                if (additionalLightsEnabled >= 0.5h && additionalLightDiffuseEnabled >= 0.5h)
                {
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, i.positionWS);
                        float additionalLightNdotL = saturate(dot(normalWS, SafeNormalize(light.direction)));
                        addLightDiffuse += albedoLight * light.color * light.distanceAttenuation * light.shadowAttenuation * additionalLightNdotL;
                    }
                }
#endif

                float2 rimScreenUV = i.positionCS.xy / _ScaledScreenParams.xy;
                float3 rimNormalWS = lerp(geometricNormalWS, normalWS, _RimNormalBlend);
                float3 rimLightResult = CalculateRim(rimScreenUV, i.positionWS, rimNormalWS, viewDir, mainLightDir, sceneShadow, aoForLighting, NoV, 1.0, albedoLight);
                finalColor += max(rimLightResult, 0.0) + addLightDiffuse * 0.5;

                #if defined(_DEATH_ON)
                    float deathMask = SAMPLE_TEXTURE2D(_DeathMask, sampler_DeathMask, TransformDeathMaskUV(rimScreenUV * _DeathMaskTiling.xy, _DeathMaskUVOffset, _DeathMaskUVRotation)).r;
                    float3 deathMaskedColor = finalColor * lerp(_DeathInnerTint.rgb, _DeathOuterTint.rgb, deathMask);
                    finalColor = lerp(finalColor, deathMaskedColor, saturate(_DeathMaskBlendStrength));
                    finalColor = ApplyCharaFresnel(finalColor, normalWS, viewDir, _DeathFresnelScale, _DeathFresnelPower, _DeathFresnelColor);
                #endif

                finalColor = ApplyCharaRedTint(finalColor);

                half4 lightingDebugColor;
                if (TryApplyCharaShaderLightingDebug(lightingDebugColor, diffuseResult + addLightDiffuse, directSpec, indirectDiffuse, 0.0.xxx, aoForLighting.xxx, shadowAttenuation, sceneShadow, 1.0))
                {
                    return lightingDebugColor;
                }

                finalColor = ApplyCharaFinalColorGradient(finalColor, i.gradientUV);
                return ApplyCharaShaderFinalDebug(finalColor, CharaFinalColorGradientDebugColor(i.gradientUV), saturate(_Alpha));
            }

            
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Pass 2: Outline
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="Outline" }

            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OutlinePassVertex
            #pragma fragment OutlinePassFragment
            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile_instancing
            #ifndef SHADER_API_GLES3
            #pragma instancing_options renderinglayer
            #endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #define CHARA_OUTLINE_APPLY_DITHERING(positionCS) \
                ApplyCharacterDitheringBayer4x4(positionCS, _CharacterDitheringFactor, _CharacterDitheringPixelSize)
            #include "Common/Chara_Dithering_V2.hlsl"
            #define CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS 1
            #define CHARA_OUTLINE_MASK_UV(inputUv) (inputUv)
            #define CHARA_OUTLINE_SAMPLE_BASE_COLOR(inputUv) pow(max(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, inputUv).rgb * _BaseColor.rgb, 0.0), max(_BaseColorPower, 0.001))
            #define _BaseTex _BaseMap
            #define sampler_BaseTex sampler_BaseMap
            #include "Common/CharaOutlinePass_V2.hlsl"
            #undef sampler_BaseTex
            #undef _BaseTex
            #undef CHARA_OUTLINE_SAMPLE_BASE_COLOR
            #undef CHARA_OUTLINE_MASK_UV
            #undef CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS
            #undef CHARA_OUTLINE_APPLY_DITHERING
            ENDHLSL
        }

        Pass
        {
            Name "CharacterExposureOutlineMask"
            Tags { "LightMode" = "CharacterExposureOutlineMask" }

            Cull Front
            Blend One Zero
            ZWrite Off
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OutlinePassVertex
            #pragma fragment OutlineExposureMaskPassFragment
            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile_instancing
            #ifndef SHADER_API_GLES3
            #pragma instancing_options renderinglayer
            #endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #define CHARA_OUTLINE_APPLY_DITHERING(positionCS) \
                ApplyCharacterDitheringBayer4x4(positionCS, _CharacterDitheringFactor, _CharacterDitheringPixelSize)
            #include "Common/Chara_Dithering_V2.hlsl"
            #define CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS 1
            #define CHARA_OUTLINE_MASK_UV(inputUv) (inputUv)
            #define CHARA_OUTLINE_SAMPLE_BASE_COLOR(inputUv) pow(max(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, inputUv).rgb * _BaseColor.rgb, 0.0), max(_BaseColorPower, 0.001))
            #define _BaseTex _BaseMap
            #define sampler_BaseTex sampler_BaseMap
            #include "Common/CharaOutlinePass_V2.hlsl"
            #undef sampler_BaseTex
            #undef _BaseTex
            #undef CHARA_OUTLINE_SAMPLE_BASE_COLOR
            #undef CHARA_OUTLINE_MASK_UV
            #undef CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS
            #undef CHARA_OUTLINE_APPLY_DITHERING
            ENDHLSL
        }

        Pass
        {
            Name "PlanarShadow"
            Tags { "LightMode" = "PlanarShadow" }

            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 0
                Comp Equal
                Pass IncrWrap
                Fail Keep
                ZFail Keep
            }

            ZWrite Off
            Offset -1, 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PlanarShadowPassVertex
            #pragma fragment PlanarShadowPassFragment
            #pragma shader_feature _PLANARSHADOW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #define CHARA_PLANAR_SHADOW_APPLY_DITHERING(positionCS) \
                ApplyCharacterDitheringBayer4x4(positionCS, _CharacterDitheringFactor, _CharacterDitheringPixelSize)
            #include "Common/Chara_Dithering_V2.hlsl"
            #include "Common/CharaPlanarShadowPass_V2.hlsl"
            #undef CHARA_PLANAR_SHADOW_APPLY_DITHERING
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Pass 3: Shadow Caster
        // Pass
        // {
        //     Name "ShadowCaster"
        //     Tags { "LightMode"="ShadowCaster" }

        //     ZWrite On
        //     ZTest LEqual
        //     ColorMask 0

        //     HLSLPROGRAM
        //     #pragma target 3.5
        //     #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        //     #pragma vertex ShadowPassVertex
        //     #pragma fragment ShadowPassFragment

        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        //     ENDHLSL
        // }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            ENDHLSL
        }

        // Pass
        // {
        //     Name "DepthNormals"
        //     Tags { "LightMode"="DepthNormals" }

        //     ZWrite On
        //     Cull Back

        //     HLSLPROGRAM
        //     #pragma target 3.5
        //     #pragma vertex DepthNormalsVertex
        //     #pragma fragment DepthNormalsFragment

        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        //     ENDHLSL
        // }
    }

    CustomEditor "Scarecrow.SimpleShaderGUI"
}
