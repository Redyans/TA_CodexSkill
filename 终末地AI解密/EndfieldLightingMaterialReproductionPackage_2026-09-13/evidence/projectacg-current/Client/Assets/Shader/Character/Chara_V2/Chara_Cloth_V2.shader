Shader "Valkyria/Chara/Chara_Cloth_V2"
{
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

        [Foldout(2,2)]_TopLight("顶光_Foldout", Float) = 0
        [Tip(Warning)] _TopLightTip("参数统一：顶光兰伯特偏移 = 0；顶光强度 = 0.5；顶光强度整体偏移 = 0。", Float) = 0
        _OtherLightColor("顶光颜色", Color) = (1, 1, 1, 1)
        _OtherLightOffset("顶光兰伯特偏移", Range(-1, 1)) = 0
        _OtherLightStrength("顶光强度", Range(0, 2)) = 0.5
        _OtherLightStrength_Offset("顶光强度整体偏移", Range(-1, 1)) = 0

       
        [Foldout(1, 1, 0, 1)] _surface("表面_Foldout", Float) = 1
        [NoScaleoffset][Tex(_BaseColor)]_BaseTex("基础贴图", 2D) = "white" {}
        [HideInInspector]_BaseColor("基础颜色", Color) = (1, 1, 1, 1)
        _BaseColorPower("基础颜色Power", Range(0, 4)) = 1
        [Toggle_Switch] _NeedNormalMap("启用法线", Float) = 1
        [Tex(_NormalScale, _NeedNormalMap)][NoScaleoffset]_NormalMap("法线贴图", 2D) = "bump" {}
        [HideInInspector]_NormalScale("法线强度", Range(0, 4)) = 1
        [Tex(_IsNeedOrmTex)] [NoScaleoffset]_MRATex("金属(R)光泽(G)AO(B)Reflec(A)", 2D) = "white" {}
        [HideInInspector][Toggle_Switch] _IsNeedOrmTex ("是否需要材质贴图", Float) = 1
        [Foldout(2,2)]_MRAChange("金属粗糙AO_Foldout", Float) = 1
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
        [GradientSplit][NoScaleoffset]_RampTex("渐变贴图", 2D) = "white" {}
        _RampColorNoLStrength("渐变A通道混合程度", Range(0, 1)) = 1
        _ShadowColor("AO+阴影颜色", Color) = (0.5, 0.5, 0.5, 1)


        [Foldout(1, 1, 0, 1)] _specularLight("直接高光_Foldout", Float) = 1
        _SpecularColor("高光颜色", Color) = (1, 1, 1, 1)
        _SpecularStrength("高光强度", Range(0, 4)) = 1
        _ForwardDirStrength("视线朝向权重", Range(0, 1)) = 0
        [HideInInspector]_NoFStrength("NoF 强度", Range(0, 2)) = 1
        [HideInInspector]_NoFPow("NoF 幂次", Range(0.1, 8)) = 1
        _SpecularShadowStrength("阴影作用强度", Range(0, 1)) = 1

        _DiffuseBlendEffect("BaseTex.a区域启用漫反射混合", Range(0, 1)) = 0
        [Toggle_Switch] _NeedRefineF0("启用高光预积分贴图", Float) = 0
        [NoScaleoffset][Gradient(_NeedRefineF0)] _SpecularRefineF0Tex("F0高光预积分贴图", 2D) = "white" {}
        [Switch(_NeedRefineF0)]_RefineF0U_lerp("F0高光视角权重", Range(0, 1)) = 0.5
        [Switch(_NeedRefineF0)] _refineF0TexLerp("F0渐变色强度", Range(0, 1)) = 1
        [Toggle_Switch] _NeedSpecularRamp("启用高光Ramp重映射", Float) = 0
        [NoScaleoffset][Gradient(_NeedSpecularRamp)] _SpecularRampTex("高光Ramp重映射贴图", 2D) = "white" {}
        [Switch(_NeedSpecularRamp)]_SpecularRampUBlend("高光Ramp视角权重", Range(0, 1)) = 0.5
        [Switch(_NeedSpecularRamp)]_SpecularRampStrength("高光Ramp混合强度", Range(0, 5)) = 1
        [Switch(_NeedSpecularRamp)]_SpecularRampAnisoBlend("高光Ramp各向异性融合", Range(0, 1)) = 1
        [Switch(_NeedSpecularRamp)]_SpecularRampAnisotropy("高光Ramp各向异性", Range(-0.99, 0.99)) = 0.5

        

        [Foldout(2,2,1,0)] _UseGGXAnisoSpecular("各向异性高光_Foldout", Float) = 0
        [Toggle_Switch] _UseAnisoStrengthMask("启用BaseTex.a控制各向异性", Float) = 0
        [Switch(_UseAnisoStrengthMask)]_AnisoStrengthMaskStrength("BaseTex.a高光强度遮罩强度", Range(0, 1)) = 1
        [Switch(_UseAnisoStrengthMask)]_AnisoStrengthMaskContrast("BaseTex.a高光强度遮罩对比度", Range(0.25, 4)) = 1
        _GGXAnisoSpecIntensity("各向异性高光总强度", Range(0, 4)) = 1
        _GGXAnisoRoughnessScale("各向异性粗糙度强度", Range(0.1, 2)) = 1
        _GGXAnisotropy("各向异性", Range(-0.99, 0.99)) = 0.5
        _GGXAnisoRotation("各向异性方向旋转", Range(-180, 180)) = 0
        _GGXAnisoNormalStrength("各向异性法线切线扰动强度", Range(0, 3)) = 1
        [Toggle_Switch] _UseAnisoNoise("启用各向异性噪声", Float) = 0
        [Tex(_UseAnisoNoise)]_AnisoNoiseMap("各向异性噪声图(R)", 2D) = "gray" {}
        [Switch(_UseAnisoNoise)]_AnisoNoiseStrength("各向异性噪声强度", Range(0, 1)) = 0.15
        [Toggle_Switch] _UseAnisoBreakup("启用高光断裂", Float) = 0
        [Switch(_UseAnisoBreakup)]_AnisoBreakupStrength("高光断裂强度", Range(0, 1)) = 0.2

        [Foldout(3,3,1,0)] _AnisoSpecularLayer1("各向异性高光1_Foldout", Float) = 1
        _GGXAnisoSpecColor("各向异性高光1颜色(A强度)", Color) = (1, 1, 1, 1)
        _GGXAnisoSharpness("各向异性高光1锐度", Range(0.25, 4)) = 1
        _GGXAnisoWidth("各向异性高光1宽度", Range(0.1, 2)) = 1
        _GGXAnisoOffset("各向异性高光1偏移", Range(-1, 1)) = 0

        [Foldout(3,3,1,0)] _AnisoSpecularLayer2("各向异性高光2_Foldout", Float) = 0
        _GGXAnisoSpecColor2("各向异性高光2颜色(A强度)", Color) = (0, 0, 0, 1)
        _GGXAnisoSharpness2("各向异性高光2锐度", Range(0.25, 4)) = 1
        _GGXAnisoWidth2("各向异性高光2宽度", Range(0.1, 2)) = 1
        _GGXAnisoOffset2("各向异性高光2偏移", Range(-1, 1)) = 0

        [Foldout(3,3,1,0)] _AnisoSpecularLayer3("各向异性高光3_Foldout", Float) = 0
        _GGXAnisoSpecColor3("各向异性高光3颜色(A强度)", Color) = (0, 0, 0, 1)
        _GGXAnisoSharpness3("各向异性高光3锐度", Range(0.25, 4)) = 1
        _GGXAnisoWidth3("各向异性高光3宽度", Range(0.1, 2)) = 1
        _GGXAnisoOffset3("各向异性高光3偏移", Range(-1, 1)) = 0

        [Foldout(2,2,1,0)] _UseMatCap("MatCap高光_Foldout", Float) = 0
        [NoScaleOffset]_MatCapTex("MatCap贴图", 2D) = "black" {}
        _MatCapColor("MatCap颜色", Color) = (1, 1, 1, 1)
        _MatCapIntensity("MatCap强度", Range(0, 4)) = 1
        _MatCapRotation("MatCap旋转", Range(-180, 180)) = 0
        [Toggle]_MatCapFlipY("MatCap翻转Y", Float) = 0
        _MatCapNormalBlend("MatCap模型法线/法线贴图融合", Range(0, 1)) = 1
        _MatCapBaseColorEffect("MatCap受底色影响", Range(0, 1)) = 0
        

        [Foldout(1, 1, 0, 1)] _IndirectLight("间接光照_Foldout", Float) = 1
        [Foldout(2, 2, 0, 1)] _ibl("间接反射高光_Foldout", Float) = 1
        [Tex(_EnvColor)][NoScaleoffset]_EnvMap("环境贴图", Cube) = "black" {}
        _EnvRotation("环境旋转", Range(-180, 180)) = 0
        [HideInInspector]_EnvColor("环境颜色", Color) = (1, 1, 1, 1)
        _EnvLightStrength("环境光强度", Range(0, 2)) = 0

        [Foldout(2, 2, 0, 1)] _ibl("间接漫反射_Foldout", Float) = 1
        [Tip(Warning)] _IndirectDiffuseTip("参数统一：间接漫反射强度 = 0.2。", Float) = 0
        _IndirectLightIntensity("间接漫反射强度", Range(0, 2)) = 0.2
        _IndirectTintColor("间接漫反射颜色", Color) =(1, 1, 1.0, 0)
        _IndirectDifficusPlaneVolume("间接漫反射0平1体感", Range(0, 1)) = 0

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

        [Foldout(1, 1, 0, 1)] _emission("自发光_Foldout", Float) = 1
         [NoScaleoffset][Tex(_EmissionColor)]_EmissionTex("自发光贴图", 2D) = "black" {}
        [HideInInspector] _EmissionColor("自发光颜色", Color) = (0, 0, 0, 1)
        _EmissionColorStrength("自发光强度", Range(0, 4)) = 1
        
        [Foldout(1, 1, 1, 0)] _outline("描边_Foldout", float) = 0
        [Toggle_Switch] _UseOutlineMask("使用描边遮罩", Float) = 0
        [Switch(_UseOutlineMask)][Tex][NoScaleoffset] _OutlineMask("描边遮罩", 2D) = "black" {}
        [Toggle_Switch] _SmoothNormal("平滑法线", Float) = 1.0
        [Toggle_Switch] _OutlineMultiplyBaseColor("描边乘自身颜色", Float) = 1.0
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineWidth("描边宽度", Range(0, 1)) = 0.5
        _OutlineClipSpaceZOffset("描边 Z 偏移", Range(-10, 10)) = 0.0
        
        [Foldout(1, 1, 0, 1)] _ShadowControl("阴影_Foldout", Float) = 1
        [Foldout(2,2)]_CSMPerShadow("CSMPerObejct阴影_Foldout", Float) = 1
        [Enum_Switch(Off, UnityOnly, POSOnly, Both)] _UsePerObjectShadow("Per Object Shadow Mode", Float) = 0
        [Switch(UnityOnly, Both, POSOnly)]_SceneShadowCenter("场景阴影中心", Range(0, 1)) = 0.5
        [Switch(UnityOnly, Both, POSOnly)]_SceneShadowSmooth("场景阴影平滑", Range(0, 1)) = 0.1
        [Switch(UnityOnly, Both, POSOnly)]_ShadowStrength("场景阴影强度", Range(0, 1)) = 1
        [Foldout(2, 2, 1, 0)] _planarShadow("平面阴影_Foldout", float) = 0
        _PlanarShadowFalloff("平面阴影衰减", Float) = 1.0
        _PlanarShadowRange("平面阴影范围", Float) = 0.0
        _PlanarShadowLightDir("光照方向 xyz + 平面高度 w", Vector) = (0, 1, 0, 0)
        _PlanarShadowGlobalCenter("平面阴影全局中心", Vector) = (0, 0, 0, 0)
        _PlanarShadowColor("平面阴影颜色", Color) = (0, 0, 0, 1)
        
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

        [Foldout(1, 1, 0, 1)] _finalColorGradient("最终颜色渐变_Foldout", Float) = 1
        _GradientColor("渐变颜色", Color) = (1, 1, 1, 1)
        _GradientMinY("渐变最小 Y", Float) = 0.0
        _GradientMaxY("渐变最大 Y", Float) = 1.0

        [Foldout_Out]
        _FoldoutOut("Foldout Out_Foldout", Float) = 1
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
//            #include "Common/CharaTransparentDepthPrepass_V2.hlsl"
//            ENDHLSL
//        }
        Pass
        {
            Tags{"LightMode" = "UniversalForward"}
            ZWrite [_ZwriteOp]
            ZTest [_ZTestMode]
            Cull[_Cull]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature _ _NEEDREFINEF0_ON
            #pragma shader_feature _ _NEEDSPECULARRAMP_ON
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature_local _RIM_ON
            #pragma multi_compile_local _ _DEATH_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN  //主光
            #pragma multi_compile _ _ADDITIONAL_LIGHTS //额外光
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING         //反射球混合
            #pragma multi_compile_fragment _ _SHADOWS_SOFT            //软阴影
 

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Common/Chara_AdditionalLights_V2.hlsl"
            #include "Common/ToonBRDF_V2.hlsl"
            #include "Common/Func_Chara_PerObjectShadow_V2.hlsl"
            #include "Common/Chara_CommonHelpers_V2.hlsl"
            #include "Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl"
            #include "Common/Chara_FinalColorGradient_V2.hlsl"
            #include "Common/Chara_ShaderDebug_V2.hlsl"
            #include "Common/Chara_Dithering_V2.hlsl"
            #include "Common/Chara_Globals_V2.hlsl"
            #include "Common/Chara_GlobalVirtualLight_V2.hlsl"
            #define CHARA_RIM_MASK_USE_ROOT_POSITION
            #define CHARA_RIM_USE_GLOBAL_CONTROLS
            #include "Common/Chara_RimShared_V2.hlsl"
            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"

            float2 TransformAnisoNoiseUV(float2 uv)
            {
                return uv * _AnisoNoiseMap_ST.xy + _AnisoNoiseMap_ST.zw;
            }

            float SampleAnisoNoise(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_AnisoNoiseMap, sampler_AnisoNoiseMap, TransformAnisoNoiseUV(uv)).r;
            }

            float ApplyAnisoMaskContrast(float value, float contrast)
            {
                return pow(saturate(value), max(contrast, 1.0e-4));
            }

            float ResolveSpecularStrengthMask(float baseAlpha)
            {
                if (_UseAnisoStrengthMask < 0.5 || _AnisoStrengthMaskStrength <= 1.0e-4)
                {
                    return 1.0;
                }

                float mask = ApplyAnisoMaskContrast(baseAlpha, _AnisoStrengthMaskContrast);
                return lerp(1.0, mask, _AnisoStrengthMaskStrength);
            }

            float3 GetGGXAnisoDirectionWS(float3 tangentWS, float3 bitangentWS)
            {
                float angle = radians(_GGXAnisoRotation);
                float sineValue;
                float cosineValue;
                sincos(angle, sineValue, cosineValue);
                return normalize(cosineValue * tangentWS + sineValue * bitangentWS);
            }

            float SampleAnisoNoiseOffset(float2 uv)
            {
                if (_UseAnisoNoise < 0.5 || _AnisoNoiseStrength <= 1.0e-4)
                {
                    return 0.0;
                }

                float noise = SampleAnisoNoise(uv);
                return (noise * 2.0 - 1.0) * _AnisoNoiseStrength;
            }

            float EvaluateAnisoBreakup(float2 uv)
            {
                if (_UseAnisoBreakup < 0.5 || _AnisoBreakupStrength <= 1.0e-4)
                {
                    return 1.0;
                }

                float noise = SampleAnisoNoise(uv);
                return max(0.0, lerp(1.0 - _AnisoBreakupStrength, 1.0 + _AnisoBreakupStrength, noise));
            }

            float D_GGX_Anisotropic(
                float NoH,
                float3 halfDirWS,
                float3 tangentWS,
                float3 bitangentWS,
                float at,
                float ab)
            {
                float ToH = dot(tangentWS, halfDirWS);
                float BoH = dot(bitangentWS, halfDirWS);
                float a2 = at * ab;
                float3 v = float3(ab * ToH, at * BoH, a2 * NoH);
                float v2 = max(dot(v, v), 1.0e-8);
                float w2 = a2 / v2;
                return a2 * w2 * w2 * (1.0 / PI);
            }


            struct GGXAnisoContext
            {
                float NoH;
                float NoV;
                float NoL;
                float VoH;
                float XoV;
                float XoL;
                float XoH;
                float YoV;
                float YoL;
                float YoH;
                float roughness;
                float anisotropy;
                float noiseOffset;
            };

            float3 EvaluateGGXAnisoLayer(
                GGXAnisoContext context,
                float3 baseSpecularColor,
                float4 layerColor,
                float sharpness,
                float width,
                float offset)
            {
                float layerStrength = max(layerColor.a, 0.0);
                if (layerStrength <= 1.0e-4)
                {
                    return 0.0.xxx;
                }

                float anisoRoughness = max(
                    context.roughness * _GGXAnisoRoughnessScale * width,
                    0.001);
                float baseXoH = context.XoH;
                float baseYoH = context.YoH;
                float noisyXoH = context.XoH;
                float noisyYoH = context.YoH;
                if (context.anisotropy >= 0.0)
                {
                    baseYoH = clamp(baseYoH + offset, -1.0, 1.0);
                    noisyYoH = clamp(baseYoH + context.noiseOffset, -1.0, 1.0);
                }
                else
                {
                    baseXoH = clamp(baseXoH + offset, -1.0, 1.0);
                    noisyXoH = clamp(baseXoH + context.noiseOffset, -1.0, 1.0);
                }

                float aspect = sqrt(max(1.0 - 0.9 * context.anisotropy, 0.001));
                float ax = max(anisoRoughness / aspect, 0.001);
                float ay = max(anisoRoughness * aspect, 0.001);
                float baseDistribution = D_GGXaniso(ax, ay, context.NoH, baseXoH, baseYoH);
                float distribution = baseDistribution;
                if (_UseAnisoNoise >= 0.5 && _AnisoNoiseStrength > 1.0e-4)
                {
                    distribution = min(
                        D_GGXaniso(ax, ay, context.NoH, noisyXoH, noisyYoH),
                        baseDistribution);
                }

                float compressedDistribution = distribution / (1.0 + distribution);
                compressedDistribution = pow(
                    saturate(compressedDistribution),
                    max(sharpness, 0.001));
                distribution = compressedDistribution / max(1.0 - compressedDistribution, 1.0e-4);
                float visibility = Vis_SmithJointAniso(
                    ax,
                    ay,
                    context.NoV,
                    context.NoL,
                    context.XoV,
                    context.XoL,
                    context.YoV,
                    context.YoL);
                float3 fresnel = F_Schlick_UE4(baseSpecularColor * layerColor.rgb, context.VoH);
                return distribution * visibility * fresnel * layerStrength;
            }

            float3 EvaluateGGXAnisoSpecular(
                float3 normalTS,
                float3 normalWS,
                float3 tangentWS,
                float3 bitangentWS,
                float3 viewDirWS,
                float3 lightDirWS,
                float3 lightColor,
                float roughness,
                float3 baseSpecularColor,
                float shadowFactor,
                float noiseOffset,
                float breakupMask,
                float strengthMask)
            {
                if (_UseGGXAnisoSpecular < 0.5 || _GGXAnisoSpecIntensity <= 1.0e-4)
                {
                    return 0.0.xxx;
                }

                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);
                float3 tangentTS = normalize(float3(1.0, 0.0, normalTS.x * _GGXAnisoNormalStrength));
                float3 bitangentTS = normalize(float3(0.0, 1.0, normalTS.y * _GGXAnisoNormalStrength));
                float3 tangentAnisoWS = normalize(mul(tangentTS, tangentToWorld));
                float3 bitangentAnisoWS = normalize(mul(bitangentTS, tangentToWorld));

                float angle = radians(_GGXAnisoRotation);
                float sineValue;
                float cosineValue;
                sincos(angle, sineValue, cosineValue);
                float3 rotatedTangentWS = normalize(cosineValue * tangentAnisoWS + sineValue * bitangentAnisoWS);
                float3 rotatedBitangentWS = normalize(-sineValue * tangentAnisoWS + cosineValue * bitangentAnisoWS);

                float3 halfDirWS = normalize(lightDirWS + viewDirWS);
                GGXAnisoContext context;
                context.NoH = saturate(dot(normalWS, halfDirWS));
                context.NoV = saturate(abs(dot(normalWS, viewDirWS)) + 1.0e-5);
                context.NoL = saturate(dot(normalWS, lightDirWS));
                context.VoH = saturate(dot(viewDirWS, halfDirWS));
                context.XoV = dot(rotatedTangentWS, viewDirWS);
                context.XoL = dot(rotatedTangentWS, lightDirWS);
                context.XoH = dot(rotatedTangentWS, halfDirWS);
                context.YoV = dot(rotatedBitangentWS, viewDirWS);
                context.YoL = dot(rotatedBitangentWS, lightDirWS);
                context.YoH = dot(rotatedBitangentWS, halfDirWS);
                context.roughness = roughness;
                context.anisotropy = clamp(_GGXAnisotropy, -0.99, 0.99);
                context.noiseOffset = noiseOffset;

                float3 layeredSpecular = EvaluateGGXAnisoLayer(
                    context,
                    baseSpecularColor,
                    _GGXAnisoSpecColor,
                    _GGXAnisoSharpness,
                    _GGXAnisoWidth,
                    _GGXAnisoOffset);
                layeredSpecular += EvaluateGGXAnisoLayer(
                    context,
                    baseSpecularColor,
                    _GGXAnisoSpecColor2,
                    _GGXAnisoSharpness2,
                    _GGXAnisoWidth2,
                    _GGXAnisoOffset2);
                layeredSpecular += EvaluateGGXAnisoLayer(
                    context,
                    baseSpecularColor,
                    _GGXAnisoSpecColor3,
                    _GGXAnisoSharpness3,
                    _GGXAnisoWidth3,
                    _GGXAnisoOffset3);

                float3 radiance = context.NoL * lightColor * PI;
                return layeredSpecular * radiance *
                    (_GGXAnisoSpecIntensity * shadowFactor * saturate(breakupMask) * strengthMask);
            }

            float3 EvaluateClothMatCap(
                float3 geometricNormalWS,
                float3 shadedNormalWS,
                float3 baseColor,
                float strengthMask)
            {
                if (_UseMatCap < 0.5 || _MatCapIntensity <= 1.0e-4)
                {
                    return 0.0.xxx;
                }

                float3 matCapNormalWS = normalize(lerp(
                    normalize(geometricNormalWS),
                    normalize(shadedNormalWS),
                    saturate(_MatCapNormalBlend)));
                float3 matCapNormalVS = normalize(mul((float3x3)UNITY_MATRIX_V, matCapNormalWS));

                float angle = radians(_MatCapRotation);
                float sineValue;
                float cosineValue;
                sincos(angle, sineValue, cosineValue);
                float2 rotatedNormal = float2(
                    cosineValue * matCapNormalVS.x - sineValue * matCapNormalVS.y,
                    sineValue * matCapNormalVS.x + cosineValue * matCapNormalVS.y);
                float2 matCapUV = rotatedNormal * 0.5 + 0.5;
                if (_MatCapFlipY > 0.5)
                {
                    matCapUV.y = 1.0 - matCapUV.y;
                }

                float3 matCapSample = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, matCapUV).rgb;
                float3 baseColorTint = lerp(1.0.xxx, baseColor, saturate(_MatCapBaseColorEffect));
                return matCapSample * _MatCapColor.rgb * baseColorTint * (_MatCapIntensity * strengthMask);
            }

            //#region 顶点结构传入法线和切线
            struct appdata
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;   
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };
            //#endregion
            
            //#region 顶点输出结构要输出计算好的TBN
            struct v2f
            {
                float4 uv : TEXCOORD0;
                float3 tangentWS : TEXCOORD1;
                float3 bitangentWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                float3 smoothNormalVertexColor : TEXCOORD5;
                float2 gradientUV : TEXCOORD6;
                float4 positionHCS : SV_POSITION;
            };
            
            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
            //#region 纹理采样时分离采样器和贴图（“双线性过滤+重复平铺”等）
            //#endregion
           
            
            //#region 在顶点着色器中计算T\B\N
            v2f vert (appdata v)
            {
                v2f o;
                float3 positionOS = v.vertex.xyz;
                o.positionHCS = TransformObjectToHClip(positionOS);
                o.uv.xy = v.uv;
                o.uv.zw = v.uv1;
                o.gradientUV = v.uv2;
                o.smoothNormalVertexColor = v.color.rgb;
                o.tangentWS = TransformObjectToWorldDir(v.tangent.xyz);//变换方向用Dir，变换顶点用World
                o.normalWS = TransformObjectToWorldNormal(v.normal);//在顶点着色器将法线从模型空间转到世界空间
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangent.w * GetOddNegativeScale();//副切线方向通过法线和切线叉积得到
                o.positionWS = TransformObjectToWorld(positionOS);
                return o;
            }
            //#endregion
            //#region Fragment Shader
            half4 frag (v2f i) : SV_Target
            {
                ApplyCharacterDitheringBayer4x4(
                    i.positionHCS,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
                half3 resultColor = float3(0, 0, 0);
                
                /////////////////////////////////////    贴图采样Start     ////////////////////////////////
                float2 baseUV = i.uv.xy * _BaseTex_ST.xy + _BaseTex_ST.zw;
                float2 normalUV = i.uv.xy * _NormalMap_ST.xy + _NormalMap_ST.zw;
                float4 mainTex = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, baseUV);
                // BaseTex.a 被布料光照占用时不再兼作透明度；未占用时保留原有区域透明。
                float baseAlphaUsedBySpecular = step(0.5, _UseAnisoStrengthMask)
                    * step(1.0e-4, _AnisoStrengthMaskStrength);
                float baseAlphaUsedByLighting = max(
                    baseAlphaUsedBySpecular,
                    step(1.0e-4, _DiffuseBlendEffect));
                half finalAlpha = lerp(mainTex.a, 1.0, baseAlphaUsedByLighting) * _Alpha;
                // return mainTex.a;
                float4 mraTex = SAMPLE_TEXTURE2D(_MRATex, sampler_MRATex, i.uv.xy);
                float4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV);
                // mraTex = lerp(float4(_MetallicOffset,_RoughnessMetal+_RoughnessNonMetal,_AOOffset,1), mraTex, _IsNeedOrmTex);
                /////////////////////////////////////    贴图采样End     ////////////////////////////////
     
                /////////////////////////////////////    光照计算     ////////////////////////////////
                ///
                float4 shadowCoords = TransformWorldToShadowCoord(i.positionWS);
                #if _MAIN_LIGHT_SHADOWS_SCREEN || _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_CASCADE
                    Light mainLight = GetMainLight(shadowCoords);
                #else
                    Light mainLight = GetMainLight();
                #endif

                // 计算主光强度的特殊方式：分离亮度和色调，便于计算高光阴影toon和颜色混合
                // 把光的rgb转换成光强
                float globalCharaRenderLightToggle = saturate(_GlobalCharacterRenderLightToggle);
                float effectLightToggle = saturate(_EffectLightToggle);
                float3 mainLightColor = CharaResolveMainLightColor(
                    mainLight.color,
                    globalCharaRenderLightToggle,
                    _GlobalCharacterRenderLightColor.rgb,
                    _GlobalCharacterRenderLightStrength,
                    effectLightToggle,
                    _EffetLightStrength);
                // 角色补光通过场景级全局参数统一驱动，不在每个材质实例上重复存一份方向/强度状态。
                float mainLightIntensity = CharaGetLightIntensity(mainLightColor);
                mainLightColor = mainLightColor / mainLightIntensity;
    
            
                // 计算一个暗部亮度更接近sRGB，_MainLightColor_dark只用到了一个通道
                float3 mainLightDir = CharaResolveMainLightDirection(
                    mainLight.direction,
                    globalCharaRenderLightToggle,
                    _GlobalCharacterRenderLightDirection,
                    effectLightToggle,
                    _EffetDirectionDir);
                mainLightDir = CharaApplyGlobalVirtualLightAsMainLightDir(mainLightDir);
            
                float3 mainLightDir_xz = normalize(float3(mainLightDir.x, 6.10351562e-05, mainLightDir.z));
          

                /////////////////////////////////////    数据准备     ////////////////////////////////
                ///
                float3 normalTSRaw = UnpackNormalFromTex(normalTex);
                float3 normalTS = UnpackNormalFromTex(normalTex, _NormalScale);
                float3x3 tbn = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                float3 tangentWS = normalize(i.tangentWS);
                float3 bitangentWS = normalize(i.bitangentWS);
                // 自己的模型就注释掉这个
                // 没有归一化
                float3 normalWS = lerp( i.normalWS ,normalize(mul(normalTS, tbn)), _NeedNormalMap);
                
                
                // float facing = isFrontFace ? 1.0 : -1.0;
                // normalWS = normalWS * facing;
                
                float3 rawViewDir = normalize(_WorldSpaceCameraPos.xyz-i.positionWS.xyz);
                float3 viewDir = rawViewDir;

                // 用视觉空间变换矩阵的最后一行获取前向，即统一的相机朝向
                // 一二三行分别是右上前
                float3 cameraForward = UNITY_MATRIX_V[2].xyz;
                cameraForward = normalize(cameraForward);
                viewDir = lerp(viewDir, cameraForward, _ForwardDirStrength);
                viewDir = normalize(viewDir);

                /////////////////////////////////////    PBR部分     ////////////////////////////////
                ///
                
                ////////////////////////////////////////    计算二阶明调暗调的albedo     ////////////////////////////////
                ///
                float3 baseColor = mainTex.xyz * _BaseColor.xyz;
                baseColor = pow(abs(baseColor), max(_BaseColorPower, 0.001));

                // 对basecolor兰伯特暗部进行明度和饱和度调整
                float3 albedoDarkColor = baseColor * _AlbedoDarkStrength;
                float albedoDarkStrength = MyCalculateLightStrength(albedoDarkColor);
                albedoDarkColor = lerp(albedoDarkStrength.xxx, albedoDarkColor, _AlbedoDarkSaturation);
       

                // PBR计算F0，能量补偿等等
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
                
                // 调试开关只使用几何法线的屏幕空间方差，不读取法线贴图 Alpha
                // 或项目预烘方差数据，便于在 Painter 中以同一公式复现。
                if (_SpecularAA > 0.5)
                {
                    roughness = FilterCharacterRoughness(roughness, i.normalWS);
                }

                // Debug 显示真正进入 Direct / Indirect Specular 的最终 Roughness。
                float debugRoughness = roughness;

                if (IsCharaShaderMaterialDebugEnabled())
                {
                    return GetCharaShaderDebugColor(baseColor, normalWS, normalTSRaw, normalTS, metallic, debugRoughness, mraTex.rgb, i.smoothNormalVertexColor, finalAlpha);
                }

                // finalColor = reflectivity;
                // return;
                /////////////////////////////////////    直接漫反射Diffuse     ////////////////////////////////
                ///
                // diffuse就是basecolor乘上能量分布，亮部basecolor不变，暗部可以手动控制
                float energyDistribution_metallic = 0.96 - 0.96 * metallic;
                float3 albedoLight = baseColor * energyDistribution_metallic;
                float3 albedoDark = albedoDarkColor * energyDistribution_metallic;


                // 场景阴影
                float shadowAttenuation = 1;
                shadowAttenuation = ApplyPerObjectShadow(mainLight.shadowAttenuation, i.positionHCS, _UsePerObjectShadow);
                float sceneShadow = 1.0;
                // PBRNew 这条枚举顺序是 Off / UnityOnly / POSOnly / Both。
                // 当材质面板选择 Off 时，整条场景阴影链直接旁路。
                if (_UsePerObjectShadow > 0.5)
                {
                    sceneShadow = lerp(1, shadowAttenuation, _ShadowStrength);
                    sceneShadow = saturate(SigmoidSharp(sceneShadow, _SceneShadowCenter, _SceneShadowSmooth));
                }
               
      

                // 三阶暗调/暗中暗/暗部衰减及强度计算
                // 重新计算下兰伯特亮暗下diffuse的强度，暗部额外乘上了0.65的衰减
                float NdotL = dot(normalWS, mainLightDir);
        
                // float3 mainDiffuseColor_moreDark = mainDiffuseColor_Dark ;

                
                float3 albedoDarkMore = albedoDark * 0.65 ; 
                float diffuseLightStrength = MyCalculateLightStrength(albedoLight);
                float diffuseDarkMoreStrength = MyCalculateLightStrength(albedoDarkMore);


                /////////////////////////////////////    背光补偿     ////////////////////////////////
                ///
                // 计算LdotV视觉背光区域
                float2 cameraForward_xz = normalize(cameraForward.xz);
                float backLight = saturate(-dot(cameraForward_xz, mainLightDir_xz.xz));
                // 测试视角固定时的背光区域
                // backLight = saturate(-dot(float2(0,1), mainLightDir_xz.xz));
                
                float backLight_y = saturate(-abs(cameraForward.y) + 0.75); 
                // backLight_y = MySmoothstep(backLight_y);
                backLight_y = smoothstep(0,1,backLight_y);
                // 背光区域根据视角进行上下的衰减
                backLight = backLight * backLight_y;
                
                
                // 背光补偿重新获得lambert阴影
                float fixNoL = 0.5 - 0.5 * NdotL * NdotL;// 在明暗交界线附近提亮最多，其余部分相应减少
                float NoL_rampFinal = fixNoL * backLight * _BackLightStrength + NdotL; 
                NoL_rampFinal = clamp(NoL_rampFinal, -1, 1);
                NoL_rampFinal = NoL_rampFinal * 0.5 + 0.5;
              
             
                /////////////////////////////////////    采样渐变Start     ////////////////////////////////
                ///
                // 用背光补偿后的半兰伯特去采样ramp，ramp重映射强度好像有问题，先标记下
                float4 rampColor_NoL = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NoL_rampFinal, 0.5));
                // 修正了下ramp混合程度
                float remapNoL = lerp(NoL_rampFinal, rampColor_NoL.w, _RampColorNoLStrength);

                // 计算ramp的色彩倾向，即饱和度，饱和度越大则ramp染色程度提高
                float rampColor_max = max(max(rampColor_NoL.x, rampColor_NoL.y), rampColor_NoL.z);
                float rampColor_min = min(min(rampColor_NoL.x, rampColor_NoL.y), rampColor_NoL.z);
                float rampColorNoLStrength = rampColor_max - rampColor_min;


                // F是视线的前向，V完全拉平后的NdotV，用NdotF去采样ramp获得a通道
                float NdotF = dot(normalWS, cameraForward);
                NdotF = NdotF * 0.5 + 0.5;
                float rampColor_NoF = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NdotF, 0.5)).w; 
                rampColor_NoF *= _NoFStrength; 
                rampColor_NoF = pow(abs(rampColor_NoF), _NoFPow);
                /////////////////////////////////////    采样渐变End     ////////////////////////////////


                /////////////////////////////////////    全光照下diffuse混合Start     ////////////////////////////////
                ///
                float aoShadow = ao * sceneShadow;//ao在阴影里更暗
                float aoArea = ao;
                
                float totalShadowArea = min(aoArea, sceneShadow);
               
                // 这里用于混合前调+中间调
                totalShadowArea = min(totalShadowArea,remapNoL);
                
                   
                
                // 这里用于混合中间调和后调
                // ao阴影区域乘上NdotF控制
                float aoShadowAreaNoF = aoShadow * rampColor_NoF * sceneShadow;

                // diffuse对ao暗部进行了更暗的处理
                // 这么处理颜色会离灰度更远，更鲜明
                albedoDarkMore = lerp(diffuseDarkMoreStrength.xxx, albedoDarkMore,1.2); 
                
                // 这两项都涉及到了NdotL
                float3 mainDiffuseColor_Dark_lerp = lerp(albedoDarkMore, albedoDark , saturate(aoShadowAreaNoF + remapNoL));
         
                float3 diffuseBRDF = lerp(mainDiffuseColor_Dark_lerp, albedoLight, totalShadowArea);
               
                // 色彩倾向越大，颜色影响越大
                float3 rampColorNoLEffect = lerp(float3(1.0, 1.0, 1.0), rampColor_NoL.rgb, rampColorNoLStrength);

                float3 diffuseBRDFRamp = diffuseBRDF * rampColorNoLEffect;
                
                float diffuseBRDFStrength = MyCalculateLightStrength(diffuseBRDF);
                float diffuseBRDFRampStrength = MyCalculateLightStrength(diffuseBRDFRamp);
                // 这里在做ramp亮度守恒补偿
                float rampColor_control = diffuseBRDFStrength / max(0.01, diffuseBRDFRampStrength);
                rampColor_control = clamp(rampColor_control, 0, 1.5);
                /////////////////////////////////////    全光照下diffuse混合End     ////////////////////////////////


                float3 finalDiffuseBRDF = diffuseBRDFRamp * rampColor_control;
                /////////////////////////////////////    顶光Start     ////////////////////////////////
                ///
                // otherlight颜色混合是控制暗部，亮部仍然保持白色，防止喧宾夺主，不控制整体
                // otherlight不参与ramp混合
                float topLightNoL = CharaEvaluateTopLightNoL(
                    normalWS,
                    _OtherLightOffset,
                    _OtherLightStrength,
                    _OtherLightStrength_Offset);

                float3 topLightDark = _OtherLightColor.rgb; 
                float3 otherLightColor = CharaEvaluateTopLightColor(topLightDark, totalShadowArea);
                float3 finalTopLight = otherLightColor * topLightNoL;
       
                // 用0和1不同的强度值控制补光的强度
                float3 topLightDay1 = finalTopLight ;
                /////////////////////////////////////    顶光End     ////////////////////////////////



                /////////////////////////////////////    直接漫反射Start     ////////////////////////////////
                ///

                // 修正：加上阴影颜色
                float3 mainlightNew = lerp(_ShadowColor.rgb, mainLightColor, totalShadowArea) * mainLightIntensity;


                // 最主要的影响：无光照时只考虑otherLightResult_day0，有光照时直射光+otherLightResult_day1
                float3 finalMainlightDifficus = mainlightNew + topLightDay1; 
                float3 diffuseResult = finalMainlightDifficus * finalDiffuseBRDF;
                /////////////////////////////////////    直接漫反射End     ////////////////////////////////

                 
        
                /////////////////////////////////////    直接高光Start     ////////////////////////////////
                ///
                // 太阳高度角最小0.5
                float cameraLightDir_y = mainLightDir.y;
                float3 cameraLightDir = normalize(float3(cameraForward.x, cameraLightDir_y, cameraForward.z));
                float NdotV = saturate(dot(normalWS, viewDir));
                float3 lightDirFixed = mainLightDir + 2 * cameraLightDir;
                float3 halfDirFixed = viewDir * 3 + lightDirFixed;
                halfDirFixed = normalize(halfDirFixed);
                float NdotH = dot(normalWS, halfDirFixed);

                // F0计算高光
                float3 baseF0 = 0.04 * reflectivity.xxx + metallic * (baseColor - reflectivity.xxx * 0.04);
                float3 F0 = baseF0;
                float roughness2 = max(roughness * roughness, 0.0078);
                float a2 = roughness2 * roughness2;
                // 标准GGX的D项计算：DistributionGGX
                float specular_D = (a2 - 1)*NdotH*NdotH + 1;
                specular_D *= specular_D;
                specular_D = a2 / specular_D;

                float specular_V = NdotV * 2 + roughness2 + 9.99999975e-05;
                specular_V = 0.5/specular_V;

                // BaseTex.a 统一调制高光 Ramp、GGX 各向异性高光和 MatCap。
                float sharedSpecularMask = ResolveSpecularStrengthMask(mainTex.a);

                // 高光重映射后的颜色直接乘高光
                #ifdef _NEEDREFINEF0_ON
                float refineF0_U = lerp(specular_D * roughness2, NdotV * NdotV, _RefineF0U_lerp);
                float refineF0_V = roughness * (1 - ao);
                float4 refineF0Tex = SAMPLE_TEXTURE2D(_SpecularRefineF0Tex, sampler_SpecularRefineF0Tex, float2(refineF0_U,refineF0_V));
                F0 *= lerp(1 , refineF0Tex.xyz, _refineF0TexLerp);
                #endif

                // 独立的高光 Ramp 仅使用 U 轴重映射，V 固定在贴图中线，不受粗糙度或 AO 驱动。
                #ifdef _NEEDSPECULARRAMP_ON
                float specularRampU = lerp(specular_D * roughness2, NdotV * NdotV, _SpecularRampUBlend);
                if (_SpecularRampAnisoBlend > 1.0e-4)
                {
                    float3 rampFiberDirectionWS = GetGGXAnisoDirectionWS(tangentWS, bitangentWS);
                    rampFiberDirectionWS = normalize(
                        rampFiberDirectionWS - normalWS * dot(normalWS, rampFiberDirectionWS));
                    float3 rampBitangentWS = normalize(cross(normalWS, rampFiberDirectionWS));
                    float rampAnisotropy = clamp(_SpecularRampAnisotropy, -0.99, 0.99);
                    float at = max(roughness * (1.0 + rampAnisotropy), 0.001);
                    float ab = max(roughness * (1.0 - rampAnisotropy), 0.001);
                    float specularRampAnisoU = D_GGX_Anisotropic(
                        saturate(dot(normalWS, halfDirFixed)),
                        halfDirFixed,
                        rampFiberDirectionWS,
                        rampBitangentWS,
                        at,
                        ab) * roughness;
                    specularRampU = lerp(
                        specularRampU,
                        specularRampAnisoU,
                        _SpecularRampAnisoBlend);
                }
                float3 specularRampColor = SAMPLE_TEXTURE2D(
                    _SpecularRampTex,
                    sampler_SpecularRampTex,
                    float2(specularRampU, 0.5)).rgb;
                F0 *= lerp(1.0.xxx, specularRampColor, _SpecularRampStrength * sharedSpecularMask);
                #endif

                float specular_DV = specular_D * specular_V - 6.10351562e-05;
                specular_DV = clamp(specular_DV, 0, 20);
                float3 specularBRDF = specular_DV * F0;

                float3 finalMainlightSpecular = aoShadow * mainLightColor * mainLightIntensity;
            
                // selfAOShadowEffect影响高光程度
                float shadowArea = totalShadowArea;
                float specularShadowStrength = CharaResolveSpecularShadowStrength(_SpecularShadowStrength);
                float selfAOShadowEffect = lerp(1 - specularShadowStrength, 1, shadowArea);

                float3 specularLight = finalMainlightSpecular  * selfAOShadowEffect * (shadowArea*0.5+0.5);
         
                float3 specularResult = specularLight * specularBRDF;
   
                specularResult *= _SpecularColor.rgb * _SpecularStrength;

                // GGX 各向异性高光使用独立噪声图 R 通道提供偏移噪声和高光断裂。
                float anisoNoiseOffset = 0.0;
                float anisoBreakupMask = 1.0;
                if (_UseGGXAnisoSpecular >= 0.5 && _UseAnisoNoise >= 0.5 && _AnisoNoiseStrength > 1.0e-4)
                {
                    anisoNoiseOffset = SampleAnisoNoiseOffset(i.uv.xy);
                }
                if (_UseGGXAnisoSpecular >= 0.5 && _UseAnisoBreakup >= 0.5 && _AnisoBreakupStrength > 1.0e-4)
                {
                    anisoBreakupMask = EvaluateAnisoBreakup(i.uv.xy);
                }

                float3 ggxAnisoSpecular = EvaluateGGXAnisoSpecular(
                    normalTS,
                    normalWS,
                    normalize(i.tangentWS),
                    normalize(i.bitangentWS),
                    rawViewDir,
                    mainLightDir,
                    mainLightColor * mainLightIntensity,
                    roughness,
                    baseF0,
                    selfAOShadowEffect,
                    anisoNoiseOffset,
                    anisoBreakupMask,
                    sharedSpecularMask);

                float3 matCapResult = EvaluateClothMatCap(
                    i.normalWS,
                    normalWS,
                    baseColor,
                    sharedSpecularMask);
  
                float diffuseSpecularBlend = lerp(1, mainTex.w, _DiffuseBlendEffect);
                float3 mainLightResult = diffuseResult * diffuseSpecularBlend + specularResult + ggxAnisoSpecular + matCapResult;
                 /////////////////////////////////////    直接高光End     ////////////////////////////////

                 /////////////////////////////////////    间接漫反射Start     ////////////////////////////////
                 ///
                 //间接漫反射体感和平面效果切换
                float3 indirectDiffuseplane = max(half3(0,0,0), half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w)) ;
                float3 indirectDiffusevolume = SampleSH(normalWS) ;
                float3 indirectDiffuse = lerp(lerp(indirectDiffuseplane, indirectDiffusevolume, _IndirectDifficusPlaneVolume), 1.0.xxx, saturate(_GlobalCharacterRenderOverrideSceneAmbient)) * baseColor;

                 // 材质级和场景级环境光参数分层相乘，便于每个场景统一调整角色间接漫反射。
                half3 indirectTint = _IndirectTintColor.rgb * _GlobalIndirectTintColor;
                indirectDiffuse *= _IndirectLightIntensity * _GlobalIndirecIntensity;
                indirectDiffuse *= indirectTint;
                /////////////////////////////////////    间接漫反射End    ////////////////////////////////
                /// 
                 /////////////////////////////////////    间接镜面反射IBLStart     ////////////////////////////////
                ///
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

                float IBLspecular_brdf = IBLspecular_brdf1/IBLspecular_brdf2; 
                
                float scale_fit_part1 = dot(float2(-1.28514, 1.0), float2(NdotV, 0.990440011));
                float scale_fit_part2 = dot(float2(1.0, -0.75591), float2(1.29678, NdotV));
                float env_scale = dot(float2(scale_fit_part1, scale_fit_part2), float2(1, roughness2));
                float bias_fit_x = dot(float3(2.92338, 59.4188, 1.0), float3(NdotV, NoV3, 1.0));
                float bias_fit_y = dot(float3(1.0, -27.0302, 222.592), float3(20.3225, NdotV, NoV3));
                float bias_fit_z = dot(float3(626.130, 316.627, 1.0), float3(NdotV, NoV3, 121.563004));
                float bias_denominator = dot(float3(bias_fit_x, bias_fit_y, bias_fit_z), float3(1,roughness2,roughness6));
                float env_bias = env_scale / max(bias_denominator,1.0e-4);

                float3 IBLspecular_brdf_final = IBLspecular_brdf * F0 + env_bias;
                float IBLspecular_brdf_final_noF0 = IBLspecular_brdf  + env_bias;
                
                float directionalAlbedo = IBLspecular_brdf_final_noF0;
                float energyLossFactor = (1.0 - directionalAlbedo) / directionalAlbedo;
                float3 ms_compensation = F0 * energyLossFactor;
                float3 IBLBRDF = IBLspecular_brdf_final * (1.0 + ms_compensation);
                
                float3 reflectDir = reflect(-viewDir, normalWS);
                reflectDir.x = -reflectDir.x;\
                reflectDir.z = -reflectDir.z;
                
                float angle = _EnvRotation * 0.0174532925; 
                float s, c;
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
                /////////////////////////////////////    间接镜面反射IBLEnd     ////////////////////////////////
                


                /////////////////////////////////////    边缘光Start     ////////////////////////////////
                ///
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
                /////////////////////////////////////    边缘光End     ////////////////////////////////
                
                /////////////////////////////////////    自发光Start     ////////////////////////////////
                ///
                float3 emissionColor = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, i.uv.xy).xyz;
                emissionColor = _EmissionColor.rgb * _EmissionColorStrength * emissionColor * lerp(1.0, mainTex.w, 0.8);

                /////////////////////////////////////    计算最终颜色     ////////////////////////////////
                ///
                /// 
                // 额外点光沿用自身基础直接高光的简化形状，不进入精修、Ramp、各向异性、IBL、Rim、自发光与阴影链路。
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
                resultColor = mainLightResult + indirLightSpecular + indirectDiffuse + max(rimLight_finalResult,0) + emissionColor + addLightResult;
                resultColor += CharaGetGlobalVirtualBackLightResult(albedoLight, normalWS, mainLightDir);

                half4 lightingDebugColor;
                if (TryApplyCharaShaderLightingDebug(
                    lightingDebugColor,
                    diffuseResult * diffuseSpecularBlend + addLightDiffuse + CharaGetGlobalVirtualBackLightResult(albedoLight, normalWS, mainLightDir),
                    specularResult + ggxAnisoSpecular + matCapResult + addLightSpecular,
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
                resultColor = ApplyCharaRedTint(resultColor);
                resultColor = ApplyCharaFinalColorGradient(resultColor, i.gradientUV);
                // resultColor = ggxAnisoSpecular;
                return ApplyCharaShaderFinalDebug(
                    resultColor,
                    CharaFinalColorGradientDebugColor(i.gradientUV),
                    finalAlpha);
            }
           
            ENDHLSL
        }
         // Outline
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

            // Temporary: compile character custom features into the base variant.
            #pragma shader_feature_local _OUTLINE_ON
            // #define _OUTLINE_ON 1

            #pragma multi_compile_instancing
            #ifndef SHADER_API_GLES3
            #pragma instancing_options renderinglayer
            #endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #include "Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl"
            #include "Common/Chara_FinalColorGradient_V2.hlsl"
            #include "Common/Chara_Globals_V2.hlsl"
            #define CHARA_OUTLINE_APPLY_DITHERING(positionCS) \
                ApplyCharacterDitheringBayer4x4(positionCS, _CharacterDitheringFactor, _CharacterDitheringPixelSize)
            #include "Common/Chara_Dithering_V2.hlsl"


            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
            #define CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS 1
            #include "Common/CharaOutlinePass_V2.hlsl"
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

            #include "Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl"
            #include "Common/Chara_FinalColorGradient_V2.hlsl"
            #include "Common/Chara_Globals_V2.hlsl"
            #define CHARA_OUTLINE_APPLY_DITHERING(positionCS) \
                ApplyCharacterDitheringBayer4x4(positionCS, _CharacterDitheringFactor, _CharacterDitheringPixelSize)
            #include "Common/Chara_Dithering_V2.hlsl"
            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
            #define CHARA_OUTLINE_EXTERNAL_TEXTURE_DECLS 1
            #include "Common/CharaOutlinePass_V2.hlsl"
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
            // Temporary: compile character custom features into the base variant.
            #pragma shader_feature _PLANARSHADOW_ON
            // #define _PLANARSHADOW_ON 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl"
            #include "Common/Chara_FinalColorGradient_V2.hlsl"
            #include "Common/Chara_Globals_V2.hlsl"


            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
            #define CHARA_PLANAR_SHADOW_APPLY_DITHERING(positionCS) \
                ApplyCharacterDitheringBayer4x4(positionCS, _CharacterDitheringFactor, _CharacterDitheringPixelSize)
            #include "Common/Chara_Dithering_V2.hlsl"
            #include "Common/CharaPlanarShadowPass_V2.hlsl"
            #undef CHARA_PLANAR_SHADOW_APPLY_DITHERING
            ENDHLSL
        }
        // Pass
        // {
        //     Name "ShadowCaster"
        //     Tags{"LightMode" = "ShadowCaster"}

        //     ZWrite On
        //     ZTest LEqual
        //     ColorMask 0
        //     Cull[_Cull]

        //     HLSLPROGRAM
        //     #pragma target 3.5

        //     // -------------------------------------
        //     // Material Keywords
        //     #pragma shader_feature_local_fragment _ALPHATEST_ON

        //     //--------------------------------------
        //     // GPU Instancing
        //     #pragma multi_compile_instancing
        //     #pragma multi_compile _ DOTS_INSTANCING_ON

        //     // -------------------------------------
        //     // Universal Pipeline keywords

        //     // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
        //     #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

        //     #pragma vertex ShadowPassVertex
        //     #pragma fragment ShadowPassFragment

        //     #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        //     #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
        //     ENDHLSL
        // }
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex CharaDepthOnlyPassVertex
            #pragma fragment CharaDepthOnlyDitheringFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #define Attributes CharaDepthOnlyAttributes
            #define Varyings CharaDepthOnlyVaryings
            #define DepthOnlyVertex CharaDepthOnlyPassVertex
            #define DepthOnlyFragment CharaDepthOnlyBaseFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            #undef DepthOnlyVertex
            #undef DepthOnlyFragment
            #undef Varyings
            #undef Attributes
            #include "Common/Chara_Dithering_V2.hlsl"

            float _CharacterDitheringFactor;
            float _CharacterDitheringPixelSize;

            half CharaDepthOnlyDitheringFragment(CharaDepthOnlyVaryings input) : SV_TARGET
            {
                ApplyCharacterDitheringBayer4x4(
                    input.positionCS,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
                return CharaDepthOnlyBaseFragment(input);
            }
            ENDHLSL
        }
        // This pass is used when drawing to a _CameraNormalsTexture texture
        // Pass
        // {
        //     Name "DepthNormals"
        //     Tags{"LightMode" = "DepthNormals"}

        //     ZWrite On
        //     Cull[_Cull]

        //     HLSLPROGRAM
        //     #pragma target 3.5

        //     #pragma vertex DepthNormalsVertex
        //     #pragma fragment DepthNormalsFragment

        //     // -------------------------------------
        //     // Material Keywords
        //     #pragma shader_feature_local _NORMALMAP
        //     #pragma shader_feature_local _PARALLAXMAP
        //     #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
        //     #pragma shader_feature_local_fragment _ALPHATEST_ON

        //     //--------------------------------------
        //     // GPU Instancing
        //     #pragma multi_compile_instancing
        //     #pragma multi_compile _ DOTS_INSTANCING_ON

        //     #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        //     #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
        //     ENDHLSL
        // }
    }
 
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
	CustomEditor "Scarecrow.SimpleShaderGUI"
}
