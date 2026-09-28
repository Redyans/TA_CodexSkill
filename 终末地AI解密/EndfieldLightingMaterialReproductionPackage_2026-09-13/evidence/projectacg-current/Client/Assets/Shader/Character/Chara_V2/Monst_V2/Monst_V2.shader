Shader "Valkyria/Chara/Monster_V2"
{
    Properties
    {
        [Foldout(1, 1, 0, 0)] _baseSetting("渲染设置_Foldout", Float) = 1
        [Toggle_Switch] _AlphaTest("Alpha Test", Float) = 0
        [Switch(_AlphaTest)] _Clip("Clip", Range(0, 1)) = 0.5
        _Alpha("Alpha", Range(0, 1)) = 1
        [Toggle] _ZwriteOp("ZWrite", Float) = 1.0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTestMode", Float) = 4.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("CullMode", Float) = 2.0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0.0

        [Foldout(1, 1, 0, 1)] _lighting("灯光_Foldout", Float) = 1
        [Foldout(2, 2)] _EffectLight("特效用平行光_Foldout", Float) = 0
        [Toggle] _EffectLightToggle("开启特效用平行光", Float) = 0
        [Vector(3)] _EffetDirectionDir("特效用平行光方向", Vector) = (0, 0, 1, 1)
        _EffetLightStrength("特效用平行光强度", Float) = 1

        [Foldout(2, 2)] _TopLight("顶光_Foldout", Float) = 0
        [Tip(Warning)] _TopLightTip("参数统一：顶光兰伯特偏移 = 0；顶光强度 = 0.5；顶光强度整体偏移 = 0。", Float) = 0
        _OtherLightColor("顶光颜色", Color) = (1, 1, 1, 1)
        _OtherLightOffset("顶光兰伯特偏移", Range(-1, 1)) = 0
        _OtherLightStrength("顶光强度", Range(0, 2)) = 0
        _OtherLightStrength_Offset("顶光强度整体偏移", Range(-1, 1)) = 0

        [Foldout(1, 1, 0, 1)] _surface("表面_Foldout", Float) = 1
        [NoScaleoffset] [Tex(_BaseColor)] _BaseTex("基础贴图", 2D) = "white" {}
        [HideInInspector] _BaseColor("基础颜色", Color) = (1, 1, 1, 1)
        _BaseColorPower("基础颜色Power", Range(0, 4)) = 1
        [Toggle_Switch] _NeedNormalMap("启用法线", Float) = 1
        [Tex(_NormalScale, _NeedNormalMap)] [NoScaleoffset] _NormalMap("法线贴图", 2D) = "bump" {}
        [HideInInspector] _NormalScale("法线强度", Range(0, 4)) = 1
        [Tex(_IsNeedOrmTex)] [NoScaleoffset] _MRATex("金属(R)光泽(G)AO(B)Reflec(A)", 2D) = "white" {}
        [HideInInspector] [Toggle_Switch] _IsNeedOrmTex("是否需要材质贴图", Float) = 1
        [Foldout(2, 2)] _MRAChange("金属粗糙AO_Foldout", Float) = 1
        _MetallicOffset("金属度强度", Range(0, 2)) = 1
        [Switch(_IsNeedOrmTex)] _MetallicContrast("金属度对比度", Range(0, 4)) = 1
        _AOOffset("AO 强度", Range(0, 2)) = 1
        [Switch(_IsNeedOrmTex)] _AOContrast("AO 对比度", Range(0, 4)) = 1
        [Switch(_IsNeedOrmTex)] _AOPower("AO Power", Range(0, 4)) = 1
        _RoughnessNonMetal("非金属粗糙度强度", Range(0, 2)) = 1
        [Switch(_IsNeedOrmTex)] _RoughnessNonMetalContrast("非金属粗糙度对比度", Range(0, 4)) = 1
        [Switch(_IsNeedOrmTex)] _RoughnessMetal("金属粗糙度强度", Range(0, 2)) = 1
        [Switch(_IsNeedOrmTex)] _RoughnessMetalContrast("金属粗糙度对比度", Range(0, 4)) = 1
        [Toggle_Switch] _SpecularAA("启用高光抗锯齿（Specular AA）", Float) = 0

        [Foldout(1, 1, 0, 1)] _directLight("直接漫反射_Foldout", Float) = 1
        _AlbedoDarkStrength("暗部颜色强度", Range(0, 1)) = 0
        _AlbedoDarkSaturation("暗部饱和度", Range(0, 2)) = 1
        _BackLightStrength("背光补偿强度", Range(0, 1)) = 0
        [GradientSplit] [NoScaleoffset] _RampTex("渐变贴图", 2D) = "white" {}
        _RampColorNoLStrength("渐变A通道混合程度", Range(0, 1)) = 0
        _ShadowColor("AO+阴影颜色", Color) = (0.5, 0.5, 0.5, 1)

        [Foldout(1, 1, 0, 1)] _specularLight("直接高光_Foldout", Float) = 1
        _SpecularColor("高光颜色", Color) = (1, 1, 1, 1)
        _SpecularStrength("高光强度", Range(0, 4)) = 1
        _ForwardDirStrength("视线朝向权重", Range(0, 1)) = 0
        [HideInInspector] _NoFStrength("NoF 强度", Range(0, 2)) = 1
        [HideInInspector] _NoFPow("NoF 幂次", Range(0.1, 8)) = 1
        _SpecularShadowStrength("阴影作用强度", Range(0, 1)) = 1
        _DiffuseBlendEffect("BaseTex.a区域启用漫反射混合", Range(0, 1)) = 0
        [Toggle_Switch] _NeedRefineF0("启用高光预积分贴图", Float) = 0
        [NoScaleoffset][Gradient(_NeedRefineF0)] _SpecularRefineF0Tex("F0高光预积分贴图", 2D) = "white" {}
        [Switch(_NeedRefineF0)] _RefineF0U_lerp("F0高光视角权重", Range(0, 1)) = 0.5
        [Switch(_NeedRefineF0)] _refineF0TexLerp("F0渐变色强度", Range(0, 1)) = 1

        [Foldout(1, 1, 0, 1)] _IndirectLight("间接光照_Foldout", Float) = 1
        [Foldout(2, 2, 0, 1)] _iblSpec("间接反射高光_Foldout", Float) = 1
        [Tex(_EnvColor)] [NoScaleoffset] _EnvMap("环境贴图", Cube) = "black" {}
        _EnvRotation("环境旋转", Range(-180, 180)) = 0
        [HideInInspector]_EnvColor("环境颜色", Color) = (1, 1, 1, 1)
        _EnvLightStrength("环境光强度", Range(0, 2)) = 0
        [Foldout(2, 2, 0, 1)] _iblDiffuse("间接漫反射_Foldout", Float) = 1
        [Tip(Warning)] _IndirectDiffuseTip("参数统一：间接漫反射强度 = 0.2。", Float) = 0
        _IndirectLightIntensity("间接漫反射强度", Range(0, 2)) = 0.2
        _IndirectTintColor("间接漫反射颜色", Color) = (1, 1, 1.0, 0)
        _IndirectDifficusPlaneVolume("间接漫反射0平1体感", Range(0, 1)) = 0

        [Foldout(1, 1, 1, 0)] _rim("边缘光_Foldout", Float) = 0
        [Enum_Switch(FresnelRim, DepthRim, Add)] _RimType("边缘光类型", Float) = 0
        [Toggle_Switch] _CustomLightDirection("自定义边缘光方向", Float) = 0
        [Switch(_CustomLightDirection)] [Enum_Switch(View, World)] _CustomRimDirectionSpace("自定义边缘光方向空间", Float) = 1
        [Vector(3, _CustomLightDirection)] _FresnelRimDirection("自定义边缘光方向", Vector) = (0, 0, 0, 0)
        _FresnelRimLightAlign("边缘光对光方向", Range(-1, 1)) = 1
        [HDR] _FresnelRimColor("边缘光颜色", Color) = (1, 1, 1, 1)
        _FresnelRimPower("边缘光范围", Range(0, 2)) = 0.5
        _FresnelRimSmooth("边缘光平滑", Range(0.001, 1)) = 0.001
        [Switch(FresnelRim, Add)] _FresnelIntensity("菲涅尔边缘光强度", Range(0, 10)) = 3
        [Switch(DepthRim, Add)] _DepthRimWidthX("深度边缘光宽度 X", Range(0, 1)) = 0.5
        [Switch(DepthRim, Add)] _DepthRimWidthY("深度边缘光宽度 Y", Range(0, 1)) = 0.5
        [Switch(DepthRim, Add)] _DepthRimIntensity("深度边缘光强度", Range(0, 10)) = 3

        [Foldout(2, 2, 0, 1)] _RimOtherSetting("边缘光其他设置_Foldout", Float) = 1
        _RimLightDiffuseColorEffect("边缘光受底色影响", Range(0, 1)) = 0
        _RimShadowStrength("边缘光阴影作用强度", Range(0, 1)) = 0
        _RimNormalBlend("边缘光模型法线/法线贴图融合", Range(0, 1)) = 1
        [Toggle_Switch] _UseRimFakePointMask("启用模拟点灯遮罩", Float) = 0
        [PosRange(_RimFakePointMaskRange, _UseRimFakePointMask)] _RimFakePointMaskPosition("模拟点灯位置 世界偏移", Vector) = (0, 0, 0, 0)
        [Switch(_UseRimFakePointMask)] _RimFakePointMaskRange("模拟点灯范围", Range(0.01, 10)) = 1
        [Switch(_UseRimFakePointMask)] _RimFakePointMaskPower("模拟点灯遮罩软硬", Range(0.25, 8)) = 2
        [Toggle_Switch] _UseRimFakeDirectMask("启用模拟直接光遮罩", Float) = 0
        [Vector(3, _UseRimFakeDirectMask)] _RimFakeDirectMaskDirection("模拟直接光切面方向", Vector) = (1, 0, 0, 0)
        [PosRange(_RimFakeDirectMaskRange, _UseRimFakeDirectMask)] _RimFakeDirectMaskPosition("模拟直接光切面位置", Vector) = (0, 0, 0, 0)
        [Switch(_UseRimFakeDirectMask)] _RimFakeDirectMaskRange("模拟直接光范围", Range(0.01, 10)) = 1

        [Foldout(1, 1, 0, 1)] _emission("自发光_Foldout", Float) = 1
        [NoScaleoffset] [Tex(_EmissionColor)] _EmissionTex("自发光贴图", 2D) = "black" {}
        [HideInInspector] _EmissionColor("自发光颜色", Color) = (0, 0, 0, 1)
        _EmissionColorStrength("自发光强度", Range(0, 4)) = 1

        [Foldout(1, 1, 1, 0)] _crystal("晶体_Foldout", Float) = 0

        [Foldout(2, 2, 0, 1)] _crystalSurface("晶体表面_Foldout", Float) = 1
        [Toggle_Switch] _CrystalUseMaskMap("启用晶体遮罩图", Float) = 0
        [Toggle_Switch] _CrystalUseThicknessTint("启用厚度染色", Float) = 1
        [HDR] _CrystalThicknessColor("厚度染色颜色", Color) = (0, 0, 0, 0)
        [Toggle_Switch] _CrystalUseParallax("启用晶体视差", Float) = 1
        [Tex(_CrystalParallaxColor)] _CrystalParallaxMap("晶体视差贴图", 2D) = "black" {}
        _CrystalParallaxAmplitude("晶体视差幅度", Float) = 20
        [HideInInspector][HDR] _CrystalParallaxColor("晶体视差颜色", Color) = (0, 0, 0, 0)
        [Toggle_Switch] _CrystalUseTangentSpaceMap("启用晶体流光贴图", Float) = 1
        [Tex(_CrystalTangentSpaceColor)] _CrystalTangentSpaceMap("晶体流光贴图", 2D) = "white" {}
        [HideInInspector][HDR] _CrystalTangentSpaceColor("晶体流光颜色", Color) = (0, 0, 0, 0)
        [Toggle_Switch] _CrystalUseGlitter("启用晶体闪粉", Float) = 1
        [Tex(_CrystalGlitterColor)] _CrystalGlitterNoiseMap("晶体闪粉噪声贴图", 2D) = "white" {}
        [HideInInspector][HDR] _CrystalGlitterColor("晶体闪粉颜色", Color) = (2, 2, 2, 0)
        _CrystalGlitterOffset("晶体闪粉偏移", Range(0, 1)) = 1

        [Foldout(2, 2, 0, 1)] _crystalLighting("晶体光照_Foldout", Float) = 1
        [Toggle_Switch] _CrystalUseSSS("启用晶体SSS", Float) = 0
        [HDR] _CrystalSSSColor("晶体SSS颜色", Color) = (1, 1, 1, 1)
        _CrystalSSSDistortion("晶体SSS扭曲", Range(0, 2)) = 0.5
        _CrystalSSSPower("晶体SSS幂次", Range(0.1, 16)) = 4
        [Toggle_Switch] _CrystalUseInnerGlow("启用晶体内发光", Float) = 1
        [HDR] _CrystalInnerGlowColor("晶体内发光颜色", Color) = (0.9653798, 1, 0.004717, 0)
        _CrystalInnerGlowCenter("晶体内发光中心", Vector) = (0, 0, 0, 0)
        _CrystalInnerGlowSize("晶体内发光范围", Range(0.01, 4)) = 1
        
        [Foldout(2, 2, 0, 1)] _crystalReflection("晶体反射_Foldout", Float) = 1
        [Toggle_Switch] _CrystalUseMatCap("启用晶体MatCap", Float) = 0
        [NoScaleOffset] [Tex(_CrystalMatCapColor)] _CrystalMatCapMap("晶体MatCap贴图", 2D) = "black" {}
        [HideInInspector][HDR] _CrystalMatCapColor("晶体MatCap颜色", Color) = (1, 1, 1, 1)

        [Foldout(1, 1, 1, 0)] _outline("描边_Foldout", Float) = 0
        [Toggle_Switch] _SmoothNormal("平滑法线", Float) = 1.0
        [Toggle_Switch] _OutlineMultiplyBaseColor("描边乘自身颜色", Float) = 1.0
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineWidth("描边宽度", Range(0, 1)) = 0.5
        _OutlineClipSpaceZOffset("描边 Z 偏移", Range(-10, 10)) = 0.0

        [Foldout(1, 1, 0, 1)] _ShadowControl("阴影_Foldout", Float) = 1
        [Foldout(2, 2)] _CSMPerShadow("CSMPerObejct阴影_Foldout", Float) = 1
        [Enum_Switch(Off, UnityOnly, POSOnly, Both)] _UsePerObjectShadow("Per Object Shadow Mode", Float) = 0
        [Switch(UnityOnly, Both, POSOnly)] _SceneShadowCenter("场景阴影中心", Range(0, 1)) = 0.5
        [Switch(UnityOnly, Both, POSOnly)] _SceneShadowSmooth("场景阴影平滑", Range(0, 1)) = 0.1
        [Switch(UnityOnly, Both, POSOnly)] _ShadowStrength("场景阴影强度", Range(0, 1)) = 1

        [Foldout(2, 2, 1, 0)] _planarShadow("平面阴影_Foldout", Float) = 0
        _PlanarShadowFalloff("平面阴影衰减", Float) = 1.0
        _PlanarShadowRange("平面阴影范围", Float) = 0.0
        _PlanarShadowLightDir("光照方向 xyz + 平面高度 w", Vector) = (0, 1, 0, 0)
        _PlanarShadowGlobalCenter("平面阴影全局中心", Vector) = (0, 0, 0, 0)
        _PlanarShadowColor("平面阴影颜色", Color) = (0, 0, 0, 1)

        [HideInInspector] [Toggle(_DEATH_ON)] _DEATH_ON("_DEATH_ON", Float) = 0
        [HideInInspector] _DeathProgress("_DeathProgress", Range(0, 1)) = 0
        [HideInInspector] _DeathMask("_DeathMask", 2D) = "white" {}
        [HideInInspector] _DeathMaskTiling("死亡遮罩平铺", Vector) = (1, 1, 0, 0)
        [HideInInspector] _DeathMaskUVOffset("_DeathMaskUVOffset", Vector) = (0, 0, 0, 0)
        [HideInInspector] _DeathMaskUVRotation("_DeathMaskUVRotation", Float) = 0
        [HideInInspector] _DeathInnerTint("_DeathInnerTint", Color) = (1, 1, 1, 1)
        [HideInInspector] _DeathOuterTint("_DeathOuterTint", Color) = (1, 1, 1, 1)
        [HideInInspector] _DeathMaskBlendStrength("_DeathMaskBlendStrength", Range(0, 1)) = 1
        [HideInInspector] _DeathFresnelScale("_DeathFresnelScale", Range(0, 10)) = 0
        [HideInInspector] _DeathFresnelPower("_DeathFresnelPower", Range(0.1, 10)) = 1
        [HideInInspector] _DeathFresnelColor("_DeathFresnelColor", Color) = (1, 1, 1, 1)

        [Foldout(1, 1, 0, 1)] _finalColorGradient("最终颜色渐变_Foldout", Float) = 1
        _GradientColor("渐变颜色", Color) = (1, 1, 1, 1)
        _GradientMinY("渐变最小 Y", Float) = 0.0
        _GradientMaxY("渐变最大 Y", Float) = 1.0

        [Foldout_Out] _FoldoutOut("Foldout Out_Foldout", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            // Mark Monster V2 pixels so screen-space decals can avoid drawing over them.
            // Only the bits represented by 20 are changed; unrelated stencil bits are preserved.
            Stencil
            {
                Ref 20
                ReadMask 20
                WriteMask 20
                Comp Always
                Pass Replace
                Fail Keep
                ZFail Keep
            }

            ZWrite [_ZwriteOp]
            ZTest [_ZTestMode]
            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature _ _NEEDREFINEF0_ON
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature_local _CRYSTAL_ON
            #pragma shader_feature_local _RIM_ON
            #pragma shader_feature_local _DEATH_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #define CHARA_GLOBALS_USE_MONSTER 1
            #include "../Common/Chara_Globals_V2.hlsl"
            #include "../Common/Chara_AdditionalLights_V2.hlsl"
            #include "../Common/ToonBRDF_V2.hlsl"
            #include "../Common/Func_Chara_PerObjectShadow_V2.hlsl"
            #include "../Common/Chara_CommonHelpers_V2.hlsl"
            #include "Monst_V2_CrystalBindings.hlsl"
            #include "../Common/Chara_FinalColorGradient_V2.hlsl"
            #include "../Common/Chara_ShaderDebug_V2.hlsl"
            #include "../Common/Chara_GlobalVirtualLight_V2.hlsl"
            #include "../Common/Chara_RimShared_V2.hlsl"
            #include "../Common/Func_Chara_GlobalEffect_V2.hlsl"
            #include "Monst_V2_CrystalFeature.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float3 tangentWS : TEXCOORD1;
                float3 bitangentWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                float3 smoothNormalVertexColor : TEXCOORD5;
                float4 positionHCS : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 positionOS = v.vertex.xyz;
                o.positionHCS = TransformObjectToHClip(positionOS);
                o.uv.xy = v.uv;
                o.uv.zw = v.uv1;
                o.smoothNormalVertexColor = v.color.rgb;
                o.tangentWS = TransformObjectToWorldDir(v.tangent.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangent.w * GetOddNegativeScale();
                o.positionWS = TransformObjectToWorld(positionOS);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half3 resultColor = 0.0.xxx;

                float2 baseUV = i.uv.xy * _BaseTex_ST.xy + _BaseTex_ST.zw;
                float2 normalUV = i.uv.xy * _NormalMap_ST.xy + _NormalMap_ST.zw;
                float4 mainTex = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, baseUV);
                float4 mraTex = SAMPLE_TEXTURE2D(_MRATex, sampler_MRATex, i.uv.xy);
                float4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV);

                float4 shadowCoords = TransformWorldToShadowCoord(i.positionWS);
                #if _MAIN_LIGHT_SHADOWS_SCREEN || _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_CASCADE
                    Light mainLight = GetMainLight(shadowCoords);
                #else
                    Light mainLight = GetMainLight();
                #endif

                float globalCharaRenderLightToggle = saturate(_GlobalCharacterRenderLightToggle);
                float effectLightToggle = saturate(_EffectLightToggle);
                float3 mainLightColor = CharaResolveMainLightColor(
                    mainLight.color,
                    globalCharaRenderLightToggle,
                    _GlobalCharacterRenderLightColor.rgb,
                    _GlobalCharacterRenderLightStrength,
                    effectLightToggle,
                    _EffetLightStrength);
                float mainLightIntensity = CharaGetLightIntensity(mainLightColor);
                mainLightColor = mainLightColor / mainLightIntensity;

                float3 mainLightDir = CharaResolveMainLightDirection(
                    mainLight.direction,
                    globalCharaRenderLightToggle,
                    _GlobalCharacterRenderLightDirection,
                    effectLightToggle,
                    _EffetDirectionDir);
                mainLightDir = CharaApplyGlobalVirtualLightAsMainLightDir(mainLightDir);
                float3 mainLightDir_xz = normalize(float3(mainLightDir.x, 6.10351562e-05, mainLightDir.z));

                float3 normalTSRaw = UnpackNormalFromTex(normalTex);
                float3 normalTS = UnpackNormalFromTex(normalTex, _NormalScale);
                float3x3 tbn = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                float3 normalWS = lerp(i.normalWS, normalize(mul(normalTS, tbn)), _NeedNormalMap);
                
                float3 rawViewDir = normalize(_WorldSpaceCameraPos.xyz - i.positionWS.xyz);
                float3 viewDir = rawViewDir;
                float3 cameraForward = normalize(UNITY_MATRIX_V[2].xyz);
                viewDir = lerp(viewDir, cameraForward, _ForwardDirStrength);
                viewDir = normalize(viewDir);
                float3 tangentViewDirection = mul(float3x3(i.tangentWS, i.bitangentWS, i.normalWS), viewDir);

                float3 baseColor = mainTex.xyz * _BaseColor.xyz;
                baseColor = pow(abs(baseColor), max(_BaseColorPower, 0.001));

                float roughness = 1 - mraTex.g;
                float metallic = mraTex.r;
                float reflectivity = mraTex.a;
                float ao = mraTex.b;

                metallic = saturate(metallic * _MetallicOffset);
                // metallic = step(0.00001,metallic);
                metallic = saturate((metallic - 0.5) * _MetallicContrast + 0.5) * step(0.001,metallic);
                metallic = lerp(saturate(_MetallicOffset), metallic, _IsNeedOrmTex);
                ao = saturate(ao * _AOOffset);
                ao = saturate((ao - 0.5) * _AOContrast + 0.5);
                ao = pow(ao, _AOPower);
                ao = lerp(saturate(_AOOffset), ao, _IsNeedOrmTex);
                float roughnessNonMetal = saturate(roughness * _RoughnessNonMetal);
                roughnessNonMetal = saturate((roughnessNonMetal - 0.5) * _RoughnessNonMetalContrast + 0.5);
                float roughnessMetal = saturate(roughness * _RoughnessMetal);
                roughnessMetal = saturate((roughnessMetal - 0.5) * _RoughnessMetalContrast + 0.5);
                roughness = lerp(roughnessNonMetal, roughnessMetal, metallic);
                roughness = lerp(saturate(_RoughnessNonMetal), roughness, _IsNeedOrmTex);
                if (_SpecularAA > 0.5)
                {
                    roughness = FilterCharacterRoughness(roughness, i.normalWS);
                }

                float3 crystalEmission = 0.0.xxx;
                float3 crystalBaseColor;
                float crystalMask = CrystalEvaluateRangeMask(mainTex);
                CrystalApplySurface(
                    i.uv.xy,
                    mainTex,
                    baseColor,
                    TransformWorldToObject(i.positionWS),
                    viewDir,
                    tangentViewDirection,
                    normalWS,
                    crystalBaseColor,
                    crystalEmission);

                baseColor = crystalBaseColor;

                if (IsCharaShaderMaterialDebugEnabled())
                {
                    return GetCharaShaderDebugColor(
                        baseColor,
                        normalWS,
                        normalTSRaw,
                        normalTS,
                        metallic,
                        roughness,
                        mraTex.rgb,
                        i.smoothNormalVertexColor,
                        mainTex.a * _Alpha);
                }

                float3 albedoDarkColor = baseColor * _AlbedoDarkStrength;
                float albedoDarkStrength = MyCalculateLightStrength(albedoDarkColor);
                albedoDarkColor = lerp(albedoDarkStrength.xxx, albedoDarkColor, _AlbedoDarkSaturation);

                float energyDistribution_metallic = 0.96 - 0.96 * metallic;
                float3 albedoLight = baseColor * energyDistribution_metallic;
                float3 albedoDark = albedoDarkColor * energyDistribution_metallic;

                float shadowAttenuation = 1;
                shadowAttenuation = ApplyPerObjectShadow(mainLight.shadowAttenuation, i.positionHCS, _UsePerObjectShadow);
                float sceneShadow = 1.0;
                if (_UsePerObjectShadow > 0.5)
                {
                    sceneShadow = lerp(1, shadowAttenuation, _ShadowStrength);
                    sceneShadow = saturate(SigmoidSharp(sceneShadow, _SceneShadowCenter, _SceneShadowSmooth));
                }

                float NdotL = dot(normalWS, mainLightDir);
                float3 albedoDarkMore = albedoDark * 0.65;
                float diffuseBRDFStrength = MyCalculateLightStrength(albedoLight);
                float diffuseDarkMoreStrength = MyCalculateLightStrength(albedoDarkMore);

                float2 cameraForward_xz = normalize(cameraForward.xz);
                float backLight = saturate(-dot(cameraForward_xz, mainLightDir_xz.xz));
                float backLight_y = saturate(-abs(cameraForward.y) + 0.75);
                backLight_y = smoothstep(0, 1, backLight_y);
                backLight = backLight * backLight_y;

                float fixNoL = 0.5 - 0.5 * NdotL * NdotL;
                float NoL_rampFinal = fixNoL * backLight * _BackLightStrength + NdotL;
                NoL_rampFinal = clamp(NoL_rampFinal, -1, 1);
                NoL_rampFinal = NoL_rampFinal * 0.5 + 0.5;

                float4 rampColor_NoL = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NoL_rampFinal, 0.5));
                float remapNoL = lerp(NoL_rampFinal, rampColor_NoL.w, _RampColorNoLStrength);

                float rampColor_max = max(max(rampColor_NoL.x, rampColor_NoL.y), rampColor_NoL.z);
                float rampColor_min = min(min(rampColor_NoL.x, rampColor_NoL.y), rampColor_NoL.z);
                float rampColorNoLStrength = rampColor_max - rampColor_min;

                float NdotF = dot(normalWS, cameraForward);
                NdotF = NdotF * 0.5 + 0.5;
                float rampColor_NoF = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NdotF, 0.5)).w;
                rampColor_NoF *= _NoFStrength;
                rampColor_NoF = pow(abs(rampColor_NoF), _NoFPow);

                float aoShadow = ao * sceneShadow;
                float aoArea = ao;
                float totalShadowArea = min(aoArea, sceneShadow);
                totalShadowArea = min(totalShadowArea, remapNoL);

                float aoShadowAreaNoF = aoShadow * rampColor_NoF * sceneShadow;
                albedoDarkMore = lerp(diffuseDarkMoreStrength.xxx, albedoDarkMore, 1.2);

                float3 mainDiffuseColor_Dark_lerp = lerp(albedoDarkMore, albedoDark, saturate(aoShadowAreaNoF + remapNoL));
                float3 diffuseBRDF = lerp(mainDiffuseColor_Dark_lerp, albedoLight, totalShadowArea);
                float3 rampColorNoLEffect = lerp(1.0.xxx, rampColor_NoL.rgb, rampColorNoLStrength);
                float3 diffuseBRDFRamp = diffuseBRDF * rampColorNoLEffect;

                diffuseBRDFStrength = MyCalculateLightStrength(diffuseBRDF);
                float diffuseBRDFRampStrength = MyCalculateLightStrength(diffuseBRDFRamp);
                float rampColor_control = diffuseBRDFStrength / max(0.01, diffuseBRDFRampStrength);
                rampColor_control = clamp(rampColor_control, 0, 1.5);

                float3 finalDiffuseBRDF = diffuseBRDFRamp * rampColor_control;

                float topLightNoL = CharaEvaluateTopLightNoL(
                    normalWS,
                    _OtherLightOffset,
                    _OtherLightStrength,
                    _OtherLightStrength_Offset);

                float3 topLightDark = _OtherLightColor.rgb;
                float3 otherLightColor = CharaEvaluateTopLightColor(topLightDark, totalShadowArea);
                float3 finalTopLight = otherLightColor * topLightNoL;
                float3 topLightDay1 = finalTopLight;

                float3 mainlightNew = lerp(_ShadowColor.rgb, mainLightColor, totalShadowArea) * mainLightIntensity;
                float3 finalMainlightDifficus = mainlightNew + topLightDay1;
                float3 diffuseResult = finalMainlightDifficus * finalDiffuseBRDF;

                float cameraLightDir_y = mainLightDir.y;
                float3 cameraLightDir = normalize(float3(cameraForward.x, cameraLightDir_y, cameraForward.z));
                float NdotV = saturate(dot(normalWS, viewDir));
                float3 lightDirFixed = mainLightDir + 2 * cameraLightDir;
                float3 halfDirFixed = normalize(viewDir * 3 + lightDirFixed);
                float NdotH = dot(normalWS, halfDirFixed);

                float3 F0 = 0.04 * reflectivity.xxx + metallic * (baseColor - reflectivity.xxx * 0.04);
                float roughness2 = max(roughness * roughness, 0.0078);
                float a2 = roughness2 * roughness2;
                float specular_D = (a2 - 1) * NdotH * NdotH + 1;
                specular_D *= specular_D;
                specular_D = a2 / specular_D;

                float specular_V = NdotV * 2 + roughness2 + 9.99999975e-05;
                specular_V = 0.5 / specular_V;

                #ifdef _NEEDREFINEF0_ON
                    float refineF0_U = lerp(specular_D * roughness2, NdotV * NdotV, _RefineF0U_lerp);
                    float refineF0_V = roughness * (1 - ao);
                    float4 refineF0Tex = SAMPLE_TEXTURE2D(_SpecularRefineF0Tex, sampler_BaseTex, float2(refineF0_U, refineF0_V));
                    F0 *= lerp(1.0.xxx, refineF0Tex.xyz, _refineF0TexLerp);
                #endif

                float specular_DV = specular_D * specular_V - 6.10351562e-05;
                specular_DV = clamp(specular_DV, 0, 20);
                float3 specularBRDF = specular_DV * F0;

                float3 finalMainlightSpecular = aoShadow * mainLightColor * mainLightIntensity;
                float shadowArea = totalShadowArea;
                float specularShadowStrength = CharaResolveSpecularShadowStrength(_SpecularShadowStrength);
                float selfAOShadowEffect = lerp(1 - specularShadowStrength, 1, shadowArea);
                float3 specularLight = finalMainlightSpecular * selfAOShadowEffect * (shadowArea * 0.5 + 0.5);
                float3 specularResult = specularLight * specularBRDF;
                specularResult *= _SpecularColor.rgb * _SpecularStrength;

                float diffuseSpecularBlend = lerp(1, mainTex.w, _DiffuseBlendEffect);
                float3 mainLightResult = diffuseResult * diffuseSpecularBlend + specularResult;

                float3 indirectDiffuseplane = max(0.0.xxx, half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w));
                float3 indirectDiffusevolume = SampleSH(normalWS);
                float3 indirectDiffuse = lerp(lerp(indirectDiffuseplane, indirectDiffusevolume, _IndirectDifficusPlaneVolume), 1.0.xxx, saturate(_GlobalCharacterRenderOverrideSceneAmbient)) * baseColor;
                half3 indirectTint = _IndirectTintColor.rgb * _GlobalIndirectTintColor;
                indirectDiffuse *= _IndirectLightIntensity * _GlobalIndirecIntensity;
                indirectDiffuse *= indirectTint;

                float roughness4 = roughness2 * roughness2;
                float roughness6 = roughness4 * roughness2;
                float NoV2 = NdotV * NdotV;
                float NoV3 = NoV2 * NdotV;
                float fit_A = 3.32707 * NdotV + 0.0365463;
                float fit_B = -9.04755 * NdotV + 9.0632;
                float IBLspecular_brdf1 = fit_A + fit_B * roughness2;

                float fitX = 3.59685 * NoV2 - 1.36772 * NoV3 + 1.0;
                float fitY = 9.22949 * NoV3 - 16.3174 * NoV2 + 9.04401;
                float fitZ = -20.2123 * NoV3 + 19.7886 * NoV2 + 5.56589;
                float3 nvFactors = float3(fitX, fitY, fitZ);
                float IBLspecular_brdf2 = dot(nvFactors, float3(1, roughness2, roughness6));

                float IBLspecular_brdf = IBLspecular_brdf1 / IBLspecular_brdf2;
                float scale_fit_part1 = dot(float2(-1.28514, 1.0), float2(NdotV, 0.990440011));
                float scale_fit_part2 = dot(float2(1.0, -0.75591), float2(1.29678, NdotV));
                float env_scale = dot(float2(scale_fit_part1, scale_fit_part2), float2(1, roughness2));
                float bias_fit_x = dot(float3(2.92338, 59.4188, 1.0), float3(NdotV, NoV3, 1.0));
                float bias_fit_y = dot(float3(1.0, -27.0302, 222.592), float3(20.3225, NdotV, NoV3));
                float bias_fit_z = dot(float3(626.130, 316.627, 1.0), float3(NdotV, NoV3, 121.563004));
                float bias_denominator = dot(float3(bias_fit_x, bias_fit_y, bias_fit_z), float3(1, roughness2, roughness6));
                float env_bias = env_scale / max(bias_denominator, 1.0e-4);

                float3 IBLspecular_brdf_final = IBLspecular_brdf * F0 + env_bias;
                float IBLspecular_brdf_final_noF0 = IBLspecular_brdf + env_bias;
                float directionalAlbedo = IBLspecular_brdf_final_noF0;
                float energyLossFactor = (1.0 - directionalAlbedo) / directionalAlbedo;
                float3 ms_compensation = F0 * energyLossFactor;
                float3 IBLBRDF = IBLspecular_brdf_final * (1.0 + ms_compensation);

                float3 reflectDir = reflect(-viewDir, normalWS);
                reflectDir.x = -reflectDir.x;
                reflectDir.z = -reflectDir.z;

                float angle = _EnvRotation * 0.0174532925;
                float s;
                float c;
                sincos(angle, s, c);

                float3 rotatedDir;
                rotatedDir.x = reflectDir.x * c - reflectDir.z * s;
                rotatedDir.y = reflectDir.y;
                rotatedDir.z = reflectDir.x * s + reflectDir.z * c;

                float envMap_level = PerceptualRoughnessToMipmapLevel(saturate(roughness));

                float4 encodedEnvironment = SAMPLE_TEXTURECUBE_LOD(
                    _EnvMap,
                    sampler_EnvMap,
                    rotatedDir,
                    envMap_level);
                float3 environmentColor = DecodeHDREnvironment(encodedEnvironment, _EnvMap_HDR);
                environmentColor *= _EnvColor.rgb;
                float3 indirLightSpecular = environmentColor * IBLBRDF * _EnvLightStrength * selfAOShadowEffect;

                float2 screenUV = i.positionHCS.xy / _ScreenParams.xy;
                float2 rimScreenUV = i.positionHCS.xy / _ScaledScreenParams.xy;
                float3 rimNormalWS = lerp(normalize(i.normalWS), normalWS, _RimNormalBlend);
                float3 rimLight_finalResult = CalculateRim(
                    rimScreenUV,
                    i.positionWS,
                    rimNormalWS,
                    viewDir,
                    mainLightDir,
                    sceneShadow,
                    ao,
                    NdotV,
                    1.0,
                    albedoLight);

                half finalAlpha = mainTex.w * _Alpha;
                float3 emissionColor = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, i.uv.xy).xyz;
                emissionColor = _EmissionColor.rgb * _EmissionColorStrength * emissionColor * lerp(1.0, mainTex.w, 0.8);
                emissionColor += crystalEmission;

                float3 crystalSSS = CrystalEvaluateSSS(
                    i.positionWS,
                    shadowCoords,
                    normalize(normalWS),
                    normalize(viewDir),
                    mainTex.a * crystalMask);

                float3 addLightDiffuse;
                float3 addLightSpecular;
                AccumulateCharacterAdditionalLighting(
                    i.positionWS,
                    i.positionHCS,
                    normalWS,
                    viewDir,
                    baseColor,
                    metallic,
                    roughness,
                    F0,
                    _SpecularColor.rgb * _SpecularStrength,
                    addLightDiffuse,
                    addLightSpecular);
                float3 addLightResult = addLightDiffuse + addLightSpecular;

                resultColor = mainLightResult
                    + indirLightSpecular
                    + indirectDiffuse
                    + max(rimLight_finalResult, 0)
                    + emissionColor
                    + crystalSSS
                    + addLightResult;

                half4 lightingDebugColor;
                if (TryApplyCharaShaderLightingDebug(
                    lightingDebugColor,
                    diffuseResult * diffuseSpecularBlend + addLightDiffuse,
                    specularResult + addLightSpecular,
                    indirectDiffuse,
                    indirLightSpecular,
                    ao.xxx,
                    shadowAttenuation,
                    sceneShadow,
                    finalAlpha))
                {
                    return lightingDebugColor;
                }

                #if defined(_DEATH_ON)
                    float deathMask = SAMPLE_TEXTURE2D(_DeathMask, sampler_DeathMask, TransformDeathMaskUV(screenUV * _DeathMaskTiling.xy, _DeathMaskUVOffset, _DeathMaskUVRotation)).r;
                    float3 deathMaskedColor = resultColor * lerp(_DeathInnerTint.rgb, _DeathOuterTint.rgb, deathMask);
                    resultColor = lerp(resultColor, deathMaskedColor, saturate(_DeathMaskBlendStrength));
                    resultColor = ApplyCharaFresnel(resultColor, normalWS, viewDir, _DeathFresnelScale, _DeathFresnelPower, _DeathFresnelColor);
                #endif

                resultColor = ApplyCharaFinalColorGradient(resultColor, i.positionWS);
                return ApplyCharaShaderFinalDebug(
                    resultColor,
                    CharaFinalColorGradientDebugColor(i.positionWS),
                    finalAlpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "Outline"
            }

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

            #define CHARA_GLOBALS_USE_MONSTER 1
            #include "../Common/Chara_Globals_V2.hlsl"
            #include "../Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl"
            #include "../Common/Chara_FinalColorGradient_V2.hlsl"
            #include "../Common/Func_Chara_GlobalEffect_V2.hlsl"
            #define CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS 1
            #define CHARA_OUTLINE_IGNORE_MASK 1
            #include "../Common/CharaOutlinePass_V2.hlsl"
            #undef CHARA_OUTLINE_IGNORE_MASK
            #undef CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS
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

            #define CHARA_GLOBALS_USE_MONSTER 1
            #include "../Common/Chara_Globals_V2.hlsl"
            #include "../Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl"
            #include "../Common/Chara_FinalColorGradient_V2.hlsl"
            #include "../Common/Func_Chara_GlobalEffect_V2.hlsl"
            #define CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS 1
            #define CHARA_OUTLINE_IGNORE_MASK 1
            #include "../Common/CharaOutlinePass_V2.hlsl"
            #undef CHARA_OUTLINE_IGNORE_MASK
            #undef CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS
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
            #pragma vertex PlanarShadowPassVertex
            #pragma fragment PlanarShadowPassFragment
            #pragma shader_feature _PLANARSHADOW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "../Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl"
            #include "../Common/Chara_FinalColorGradient_V2.hlsl"
            #include "../Common/Chara_Globals_V2.hlsl"
            #include "../Common/Func_Chara_GlobalEffect_V2.hlsl"
            #include "../Common/CharaPlanarShadowPass_V2.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
