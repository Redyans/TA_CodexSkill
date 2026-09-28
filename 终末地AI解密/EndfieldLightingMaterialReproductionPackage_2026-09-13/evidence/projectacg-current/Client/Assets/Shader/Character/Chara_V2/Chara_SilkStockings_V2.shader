Shader "Valkyria/Chara/Chara_SilkStockings_V2"
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
        _OtherLightStrength("顶光强度", Range(0, 2)) = 0
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

        [Foldout(1, 1, 0, 1)] _directLight("直接漫反射_Foldout", Float) = 1
        // _BackLightStrength("背光补偿强度", Range(0, 1)) = 0
        [GradientSplit][NoScaleoffset]_RampTex("渐变贴图", 2D) = "white" {}
        _ShadowColor("AO+阴影颜色", Color) = (0.5, 0.5, 0.5, 1)
        _MainLightWrap("主光包裹", Range(0, 1)) = 0.2
        _MainLightSoftness("主光柔和度", Range(0.01, 1)) = 0.18

        [Foldout(1, 1, 0, 1)] _stockings("丝袜光照_Foldout", Float) = 1
        _StockingsControlMap("丝袜控制图(R厚薄/G强度/B噪声/A边缘光遮罩)", 2D) = "white" {}
        _StockingsBlend("丝袜效果强度", Range(0, 1)) = 1
        _BaseThickness("基础厚薄", Range(0, 1)) = 0.45
        _ThicknessRemapMin("厚薄最小值", Range(0, 1)) = 0
        _ThicknessRemapMax("厚薄最大值", Range(0, 1)) = 1
        _ThicknessPower("厚薄对比度", Range(0.25, 4)) = 1
        _StockingsPow("视角染色强度", Range(0.5, 6)) = 1.8
        _StockingsColorInside("正视染色", Color) = (1, 1, 1, 1)
        _StockingsColorOutside("边缘染色", Color) = (0.72, 0.68, 0.8, 1)
        _EdgeDarken("轮廓压暗", Range(0, 1)) = 0.25
        _SheerStrength("假透肉强度", Range(0, 1)) = 0.5
        _FakeSkinTint("肤色抬升颜色", Color) = (1.12, 0.9, 0.88, 1)
        _FakeSkinStrength("肤色抬升混合", Range(0, 1)) = 0.35
        _ThinSpecBoost("薄区丝感增强", Range(0, 2)) = 0.4
        [Foldout(2,2)] _stockingsAniso("丝袜各项异性高光_Foldout", Float) = 1
        [Toggle] _UseAnisoSpecular("启用各向异性高光", Float) = 1
        _AnisoSpecIntensity("各向异性高光强度", Range(0, 4)) = 1

        _NormalAniso("法线Anisotropy", Range(0, 3)) = 0
        _FiberRotation("丝向旋转", Range(-180, 180)) = 0
        _FiberSpecColor("各向异性颜色A", Color) = (1, 1, 1, 1)
        _FiberSpecStrength("各向异性强度A", Range(0, 4)) = 1.25
        _FiberSpecPower("各向异性锐度A", Range(1, 64)) = 24
        _FiberWidth("各向异性宽度A", Range(0, 1)) = 0.35
        _FiberSpecOffset("各向异性偏移A", Range(-1, 1)) = 0
        _FiberSpecColor2("各向异性颜色B", Color) = (0.85, 0.9, 1, 1)
        _FiberSpecStrength2("各向异性强度B", Range(0, 4)) = 0.75
        _FiberSpecPower2("各向异性锐度B", Range(1, 64)) = 10
        _FiberWidth2("各向异性宽度B", Range(0, 1)) = 0.65
        _FiberSpecOffset2("各向异性偏移B", Range(-1, 1)) = -0.35
        [Toggle] _UseAnisoStrengthMask("启用各向异性强度遮罩", Float) = 0
        _AnisoStrengthMaskStrength("各向异性强度遮罩强度", Range(0, 1)) = 1
        _AnisoStrengthMaskContrast("各向异性强度遮罩对比度", Range(0.25, 4)) = 1
        [Toggle] _UseAnisoNoise("启用各向异性噪声", Float) = 0
        _AnisoNoiseStrength("各向异性噪声强度", Range(0, 1)) = 0.15
        [Toggle] _UseAnisoBreakup("启用高光断裂", Float) = 0
        _AnisoBreakupStrength("高光断裂强度", Range(0, 1)) = 0.2

        [Foldout(2,2)] _specular("丝袜直接高光_Foldout", Float) = 1
        _StockingsSpecColor("丝袜基础高光颜色", Color) = (1, 1, 1, 1)
        _StockingsSpecStrength("丝袜基础高光强度", Range(0, 15)) = 1
        _ForwardDirStrength("视线朝向权重", Range(0, 1)) = 0
        _SpecularShadowStrength("阴影作用强度", Range(0, 1)) = 1
        [Toggle_Switch] _NeedRefineF0("启用高光预积分贴图", Float) = 0
        [Tex(_RefineF0U_lerp,_NeedRefineF0)][NoScaleoffset] _SpecularRefineF0Tex("F0 精修贴图", 2D) = "white" {}
        [HideInInspector]_RefineF0U_lerp("F0 精修 U 插值", Range(0, 1)) = 0.5

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

        [Foldout(1, 1, 0, 1)] _ShadowControl("阴影_Foldout", Float) = 1
        [Foldout(2,2)]_CSMPerShadow("CSMPerObejct阴影_Foldout", Float) = 1
        [Enum_Switch(Off, UnityOnly, POSOnly, Both)] _UsePerObjectShadow("Per Object Shadow Mode", Float) = 0
        [Switch(UnityOnly, Both, POSOnly)]_SceneShadowCenter("场景阴影中心", Range(0, 1)) = 0.5
        [Switch(UnityOnly, Both, POSOnly)]_SceneShadowSmooth("场景阴影平滑", Range(0, 1)) = 0.1
        [Switch(UnityOnly, Both, POSOnly)]_ShadowStrength("场景阴影强度", Range(0, 2)) = 1

        [Foldout(1, 1, 1, 0)] _outline("描边_Foldout", Float) = 0
        [Toggle_Switch] _UseOutlineMask("使用描边遮罩", Float) = 0
        [Toggle_Switch] _SmoothNormal("平滑法线", Float) = 1.0
        [Toggle_Switch] _OutlineMultiplyBaseColor("描边乘自身颜色", Float) = 1.0
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineWidth("描边宽度", Range(0, 1)) = 0.5
        _OutlineClipSpaceZOffset("描边 Z 偏移", Range(-10, 10)) = 0.0
        [HideInInspector][NoScaleoffset] _OutlineMask("描边遮罩", 2D) = "white" {}
        [HideInInspector] _FogIntensity("雾效强度", Range(0, 1)) = 1

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
        _FoldoutOut("结束_Foldout", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        Cull Off
 
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
            // Temporary: compile character custom features into the base variant.
      
            #pragma shader_feature _ _NEEDREFINEF0_ON
            #pragma shader_feature_local _RIM_ON
            #pragma multi_compile_local _ _DEATH_ON
      
            
            #pragma shader_feature _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN  // 主光
            #pragma multi_compile _ _ADDITIONAL_LIGHTS // 额外光
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING         // 反射球混合
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION   
            #pragma multi_compile_fragment _ _SHADOWS_SOFT            // 软阴影
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION  // SSAO

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Common/Chara_AdditionalLights_V2.hlsl"
            //#include "Common/ToonBRDF.hlsl"
            #include "Common/Chara_ShaderDebug_V2.hlsl"
            #include "Common/Func_Chara_PerObjectShadow_V2.hlsl"
            //#include "Common/Func_Chara_Dissolve.hlsl"
            #include "Common/Chara_CommonHelpers_V2.hlsl"
            #define CHARA_CLOTH_V2_VARIANT_SILK_STOCKINGS 1
            #include "Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl"
            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
            #include "Common/Chara_FinalColorGradient_V2.hlsl"
            #include "Common/Chara_Dithering_V2.hlsl"
            #include "Common/Chara_Globals_V2.hlsl"
            #define CHARA_RIM_MASK_USE_ROOT_POSITION
            #define CHARA_RIM_USE_GLOBAL_CONTROLS
            #include "Common/Chara_RimShared_V2.hlsl"

            //#region 纹理采样（采样器与贴图分离，便于双线性过滤和重复平铺）
            //#endregion

            float2 TransformStockingsControlUV_LYJ(float2 uv)
            {
                return uv * _StockingsControlMap_ST.xy + _StockingsControlMap_ST.zw;
            }

            float4 SilkSampleStockingsControl_LYJ(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_StockingsControlMap, sampler_StockingsControlMap, TransformStockingsControlUV_LYJ(uv));
            }

            float SilkEvaluateThicknessMask_LYJ(float controlThickness)
            {
                // 控制图仍是默认白图时，回退到基础厚薄，避免无图状态下所有区域都被判成最厚。
                if (controlThickness >= 0.999)
                {
                    controlThickness = saturate(_BaseThickness);
                }
                float range = max(_ThicknessRemapMax - _ThicknessRemapMin, 1.0e-4);
                float remappedThickness = saturate((controlThickness - _ThicknessRemapMin) / range);
                return pow(saturate(remappedThickness), max(_ThicknessPower, 1.0e-4));
            }

            float SilkEvaluateViewFactor_LYJ(float3 normalWS, float3 viewDirWS)
            {
                float NdotV = saturate(dot(normalWS, viewDirWS));
                return pow(NdotV, max(_StockingsPow, 0.05));
            }

            float3 SilkApplyStockingsTint_LYJ(float3 albedo, float viewFactor, float thicknessMask)
            {
                float thinArea = 1.0 - thicknessMask;
                float frontThinMask = saturate(thinArea * viewFactor);
                float edgeDarkFactor = lerp(1.0 - _EdgeDarken, 1.0, viewFactor);
                float sheerLift = 1.0 + _SheerStrength * thinArea * viewFactor;
                float3 stockingsTint = lerp(_StockingsColorOutside.rgb, _StockingsColorInside.rgb, viewFactor);
                float3 surfaceColor = albedo * stockingsTint * edgeDarkFactor * sheerLift;
                float skinBlend = saturate(frontThinMask * _FakeSkinStrength);
                surfaceColor = lerp(surfaceColor, surfaceColor * _FakeSkinTint.rgb, skinBlend);
                return surfaceColor;
            }

            float SilkEvaluateSilkBoost_LYJ(float thicknessMask, float viewFactor)
            {
                float thinArea = 1.0 - thicknessMask;
                return 1.0 + thinArea * _ThinSpecBoost + thinArea * viewFactor * _SheerStrength;
            }

            float SilkApplyMaskContrast_LYJ(float value, float contrast)
            {
                return pow(saturate(value), max(contrast, 1.0e-4));
            }

            void SilkApplyNormalAnisoToBasis_LYJ(
                float3 normalTS,
                float3 normalWS,
                inout float3 tangentWS,
                inout float3 bitangentWS)
            {
                if (_NormalAniso <= 1.0e-4)
                {
                    return;
                }

                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);
                float3 tangentTS = normalize(float3(1.0, 0.0, normalTS.x * _NormalAniso));
                float3 bitangentTS = normalize(float3(0.0, 1.0, normalTS.y * _NormalAniso));
                tangentWS = normalize(mul(tangentTS, tangentToWorld));
                bitangentWS = normalize(mul(bitangentTS, tangentToWorld));
            }

            float SilkEvaluateAnisoStrengthMask_LYJ(float controlStrength)
            {
                if (_UseAnisoStrengthMask < 0.5 || _AnisoStrengthMaskStrength <= 1.0e-4)
                {
                    return 1.0;
                }

                float mask = controlStrength;
                mask = SilkApplyMaskContrast_LYJ(mask, _AnisoStrengthMaskContrast);
                return lerp(1.0, mask, _AnisoStrengthMaskStrength);
            }

            float3 SilkGetFiberDirectionWS_LYJ(float3 tangentWS, float3 bitangentWS)
            {
                float angle = radians(_FiberRotation);
                float sineValue;
                float cosineValue;
                sincos(angle, sineValue, cosineValue);
                return normalize(cosineValue * tangentWS + sineValue * bitangentWS);
            }

            float SilkEvaluateAnisoNoiseOffset_LYJ(float controlNoise)
            {
                if (_UseAnisoNoise < 0.5 || _AnisoNoiseStrength <= 1.0e-4)
                {
                    return 0.0;
                }

                return (controlNoise * 2.0 - 1.0) * _AnisoNoiseStrength;
            }

            float SilkEvaluateAnisoBreakup_LYJ(float controlNoise)
            {
                if (_UseAnisoBreakup < 0.5 || _AnisoBreakupStrength <= 1.0e-4)
                {
                    return 1.0;
                }

                return max(0.0, lerp(1.0 - _AnisoBreakupStrength, 1.0 + _AnisoBreakupStrength, controlNoise));
            }

            float SilkEvaluateAnisoBand_LYJ(float3 axisWS, float3 halfDirWS, float width)
            {
                float axisDotH = dot(axisWS, halfDirWS);
                float lobe = sqrt(saturate(1.0 - axisDotH * axisDotH));
                float bandMin = 1.0 - saturate(width);
                return saturate((lobe - bandMin) / max(width, 1.0e-4));
            }

            float SmoothBandSilk_LYJ(float value, float center, float softness)
            {
                float halfWidth = max(softness, 1.0e-4) * 0.5;
                return smoothstep(center - halfWidth, center + halfWidth, value);
            }

            float3 FresnelSchlickSilk_LYJ(float cosTheta, float3 fresnel0)
            {
                float oneMinusCos = 1.0 - saturate(cosTheta);
                float oneMinusCos2 = oneMinusCos * oneMinusCos;
                float oneMinusCos5 = oneMinusCos2 * oneMinusCos2 * oneMinusCos;
                return fresnel0 + (1.0 - fresnel0) * oneMinusCos5;
            }

            float3 FresnelSchlickRoughnessSilk_LYJ(float cosTheta, float3 fresnel0, float roughness)
            {
                float3 grazing = max((1.0 - roughness).xxx, fresnel0);
                float oneMinusCos = 1.0 - saturate(cosTheta);
                float oneMinusCos2 = oneMinusCos * oneMinusCos;
                float oneMinusCos5 = oneMinusCos2 * oneMinusCos2 * oneMinusCos;
                return fresnel0 + (grazing - fresnel0) * oneMinusCos5;
            }

            float DistributionGGXSilk_LYJ(float NdotH, float roughness)
            {
                float a = max(roughness * roughness, 1.0e-4);
                float a2 = a * a;
                float denom = max(NdotH * NdotH * (a2 - 1.0) + 1.0, 1.0e-4);
                return a2 / (PI * denom * denom);
            }

            float GeometrySchlickGGXSilk_LYJ(float NdotX, float roughness)
            {
                float r = roughness + 1.0;
                float k = (r * r) * 0.125;
                return NdotX / lerp(k, 1.0, NdotX);
            }

            float GeometrySmithSilk_LYJ(float NdotL, float NdotV, float roughness)
            {
                float ggxL = GeometrySchlickGGXSilk_LYJ(saturate(NdotL), roughness);
                float ggxV = GeometrySchlickGGXSilk_LYJ(saturate(NdotV), roughness);
                return ggxL * ggxV;
            }

            float SilkEvaluateAnisoLobe_LYJ(
                float3 normalWS,
                float3 fiberDirectionWS,
                float3 viewDirWS,
                float3 lightDirWS,
                float width,
                float power,
                float offset)
            {
                float3 anisAxisWS = normalize(fiberDirectionWS + normalWS * offset);
                float3 halfDirWS = normalize(viewDirWS + lightDirWS);
                float band = SilkEvaluateAnisoBand_LYJ(anisAxisWS, halfDirWS, width);
                float highlight = pow(band, max(power, 1.0));
                float viewAtten = sqrt(saturate(1.0 - abs(dot(anisAxisWS, viewDirWS))));
                float lightAtten = saturate(dot(normalWS, lightDirWS));
                return highlight * viewAtten * lightAtten;
            }

            float3 SilkEvaluateAnisoSpecular_LYJ(
                float3 normalWS,
                float3 fiberDirectionWS,
                float3 viewDirWS,
                float3 lightDirWS,
                float3 lightColor,
                float shadowFactor,
                float silkBoost,
                float noiseOffset,
                float breakupMask,
                float anisoStrengthMask)
            {
                if (_UseAnisoSpecular < 0.5 || _AnisoSpecIntensity <= 1.0e-4)
                {
                    return 0.0.xxx;
                }

                float lobeA = SilkEvaluateAnisoLobe_LYJ(
                    normalWS,
                    fiberDirectionWS,
                    viewDirWS,
                    lightDirWS,
                    _FiberWidth,
                    _FiberSpecPower,
                    _FiberSpecOffset + noiseOffset);

                float lobeB = SilkEvaluateAnisoLobe_LYJ(
                    normalWS,
                    fiberDirectionWS,
                    viewDirWS,
                    lightDirWS,
                    _FiberWidth2,
                    _FiberSpecPower2,
                    _FiberSpecOffset2 + noiseOffset);

                float3 anisoColor =
                    _FiberSpecColor.rgb * (_FiberSpecStrength * lobeA) +
                    _FiberSpecColor2.rgb * (_FiberSpecStrength2 * lobeB);

                return anisoColor * lightColor * (_AnisoSpecIntensity * silkBoost * shadowFactor * breakupMask * anisoStrengthMask);
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
            
            //#region 顶点输出结构要输出计算好的 TBN
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
            //#endregion
            

               
            //#region 在顶点着色器中计算 T/B/N
            v2f vert (appdata v)
            {
                v2f o;
                float3 positionOS = v.vertex.xyz;
                o.positionHCS = TransformObjectToHClip(positionOS);
                o.uv.xy = v.uv;
                o.uv.zw = v.uv1;
                o.gradientUV = v.uv2;
                o.smoothNormalVertexColor = v.color.rgb;
                o.tangentWS = TransformObjectToWorldDir(v.tangent.xyz);// 变换方向用 Dir，变换顶点用 World
                o.normalWS = TransformObjectToWorldNormal(v.normal);// 在顶点着色器中将法线从模型空间转到世界空间
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangent.w * GetOddNegativeScale();// 副切线方向通过法线和切线叉积得到
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
                float3 resultColor = float3(0, 0, 0);
                

                /////////////////////////////////////    璐村浘閲囨牱Start     ////////////////////////////////
                float2 baseUV = i.uv.xy * _BaseTex_ST.xy + _BaseTex_ST.zw;
                float2 normalUV = i.uv.xy * _NormalMap_ST.xy + _NormalMap_ST.zw;
                float4 mainTex = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, baseUV);
                float4 rawMraTex = SAMPLE_TEXTURE2D(_MRATex, sampler_MRATex, i.uv.xy);
                float4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV);
                float4 mraTex = rawMraTex;

                /////////////////////////////////////    贴图采样 End     ////////////////////////////////
     
                /////////////////////////////////////    光照计算     ////////////////////////////////
                ///
                float4 shadowCoords = TransformWorldToShadowCoord(i.positionWS);
                #if _MAIN_LIGHT_SHADOWS_SCREEN || _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_CASCADE
                    Light mainLight = GetMainLight(shadowCoords);
                #else
                    Light mainLight = GetMainLight();
                #endif
      
               
                // 计算主光强度时拆分亮度和色调，便于后续高光、阴影 toon 和颜色混合。
                // 这里先把光的 rgb 转换成光强。
                float globalCharaRenderLightToggle = saturate(_GlobalCharacterRenderLightToggle);
                float effectLightToggle = saturate(_EffectLightToggle);
                float3 mainLightColor = CharaResolveMainLightColor(
                    mainLight.color,
                    globalCharaRenderLightToggle,
                    _GlobalCharacterRenderLightColor.rgb,
                    _GlobalCharacterRenderLightStrength,
                    effectLightToggle,
                    _EffetLightStrength);
                // 角色补光通过场景级全局参数统一驱动，不在每个材质实例上重复存一份方向和强度状态。
                float mainLightIntensity = CharaGetLightIntensity(mainLightColor);
                mainLightColor = mainLightColor / mainLightIntensity;
    
            
                // 主光方向也统一走角色全局灯光与特效灯光方向解析。
                float3 mainLightDir = CharaResolveMainLightDirection(
                    mainLight.direction,
                    globalCharaRenderLightToggle,
                    _GlobalCharacterRenderLightDirection,
                    effectLightToggle,
                    _EffetDirectionDir);
            
                float3 mainLightDir_xz = normalize(float3(mainLightDir.x, 6.10351562e-05, mainLightDir.z));
                
                
                /////////////////////////////////////    鏁版嵁鍑嗗     ////////////////////////////////
                ///
                float3 normalTSRaw = UnpackNormalFromTex(normalTex);
                float3 normalTS = UnpackNormalFromTex(normalTex, _NormalScale);
                float3x3 tbn = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                float3 tangentWS = normalize(i.tangentWS);
                float3 bitangentWS = normalize(i.bitangentWS);
                // 关闭法线贴图时沿用模型法线。
                // 开启法线贴图时使用切线空间法线转换后的世界法线。
                float3 normalWS = lerp( i.normalWS ,normalize(mul(normalTS, tbn)), _NeedNormalMap);
                SilkApplyNormalAnisoToBasis_LYJ(normalTS, normalWS, tangentWS, bitangentWS);
                
                
                // float facing = isFrontFace ? 1.0 : -1.0;
                // normalWS = normalWS * facing;
                
                float3 rawViewDir = normalize(_WorldSpaceCameraPos.xyz-i.positionWS.xyz);
                float3 viewDir = rawViewDir;

                // 用视图空间矩阵最后一行获取前向，即统一的相机朝向。
                // 第一、二、三行分别对应右、上、前。
                float3 cameraForward = UNITY_MATRIX_V[2].xyz;
                cameraForward = normalize(cameraForward);
                viewDir = lerp(viewDir, cameraForward, _ForwardDirStrength);
                viewDir = normalize(viewDir);

 
                /////////////////////////////////////    PBR 部分     ////////////////////////////////
                ///
                
                ////////////////////////////////////////    计算二阶明调暗调的 albedo     ////////////////////////////////
                ///
                float3 baseColor = mainTex.xyz * _BaseColor.xyz;
                baseColor = pow(max(baseColor, 0.0), max(_BaseColorPower, 0.001));
                // 丝袜层只调制漫反射输入色，不改 LYJ 直漫/间漫反的原有框架。
                float4 stockingsControl = SilkSampleStockingsControl_LYJ(i.uv.xy);
                float stockingsThickness = SilkEvaluateThicknessMask_LYJ(stockingsControl.r);
                float stockingsViewFactor = SilkEvaluateViewFactor_LYJ(normalWS, rawViewDir);
                float3 stockingsBaseColor = SilkApplyStockingsTint_LYJ(baseColor, stockingsViewFactor, stockingsThickness);
                stockingsBaseColor = lerp(baseColor, stockingsBaseColor, saturate(_StockingsBlend));
                float silkBoost = SilkEvaluateSilkBoost_LYJ(stockingsThickness, stockingsViewFactor);
                float anisoStrengthMask = SilkEvaluateAnisoStrengthMask_LYJ(stockingsControl.g);
                float anisoNoiseOffset = SilkEvaluateAnisoNoiseOffset_LYJ(stockingsControl.b);
                float anisoBreakupMask = SilkEvaluateAnisoBreakup_LYJ(stockingsControl.b);
                // A 通道只遮罩边缘光最终项，不影响基础或各向异性高光。
                float rimLightMask = saturate(stockingsControl.a);
                float3 fiberDirectionWS = SilkGetFiberDirectionWS_LYJ(tangentWS, bitangentWS);
                
                // 对 baseColor 兰伯特暗部做明度和饱和度调整。
                float3 albedoDarkColor = stockingsBaseColor * 0.8;
                float albedoDarkStrength = MyCalculateLightStrength(albedoDarkColor);
                albedoDarkColor = lerp(albedoDarkStrength.xxx, albedoDarkColor, 1.0);
          

                // PBR 计算 F0、能量补偿等参数。
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
                float debugRoughness = saturate(roughness);
                roughness = FilterCharacterRoughness(roughness, i.normalWS, normalTex, _NeedNormalMap);

                /////////////////////////////////////    直接漫反射 Diffuse     ////////////////////////////////
                ///
                // diffuse 就是 baseColor 乘上能量分布，亮部 baseColor 不变，暗部可以手动控制。
                float energyDistribution_metallic = 0.96 - 0.96 * metallic;
                float3 albedoLight = stockingsBaseColor * energyDistribution_metallic;
                float3 albedoDark = albedoDarkColor * energyDistribution_metallic;

                // 鍦烘櫙闃村奖
                float shadowAttenuation = 1;
                shadowAttenuation = ApplyPerObjectShadow(mainLight.shadowAttenuation, i.positionHCS, _UsePerObjectShadow);
                float shadowScene = 1;
                if (_UsePerObjectShadow > 0.5)
                {
                    shadowScene = lerp(1, shadowAttenuation, _ShadowStrength);
                    shadowScene = saturate(SigmoidSharp(shadowScene, _SceneShadowCenter, _SceneShadowSmooth));
                }
                
                shadowScene = saturate(shadowScene);
               

                // 三阶暗调/暗中暗：计算暗部衰减和强度。
                // 重新计算兰伯特亮暗下的 diffuse 强度，暗部额外乘上 0.65 的衰减。
                float NoL = dot(normalWS, mainLightDir);
      
                float3 albedoDarkMore = albedoDark * 0.65 ; 
                float diffuseLightStrength = MyCalculateLightStrength(albedoLight);
                float diffuseDarkMoreStrength = MyCalculateLightStrength(albedoDarkMore);


                /////////////////////////////////////    背光补偿     ////////////////////////////////
                ///
                // 计算 LdotV 视觉背光区域。
                float2 cameraForward_xz = normalize(cameraForward.xz);
                float backLight = saturate(-dot(cameraForward_xz, mainLightDir_xz.xz));
                
                float backLight_y = saturate(-abs(cameraForward.y) + 0.75); 
                // backLight_y = MySmoothstep(backLight_y);
                backLight_y = smoothstep(0,1,backLight_y);
                // 背光区域根据视角做上下衰减。
                backLight = backLight * backLight_y;
                
                
                // 背光补偿重新构造 lambert 阴影。
                float fixNoL = 0.5 - 0.5 * NoL * NoL;// 在明暗交界线附近提亮最多，其余部分相应减弱
                float NoL_rampFinal = fixNoL * backLight * _BackLightStrength + NoL; 
                NoL_rampFinal = clamp(NoL_rampFinal, -1, 1);
                NoL_rampFinal = NoL_rampFinal * 0.5 + 0.5;
              
             
                /////////////////////////////////////    采样渐变 Start     ////////////////////////////////
                ///
                float stockingsWrappedNdotL = saturate((NoL + _MainLightWrap) / (1.0 + _MainLightWrap));
                float stockingsLambertShadow = SmoothBandSilk_LYJ(stockingsWrappedNdotL, 0.5, _MainLightSoftness);
                float stockingsSceneShadow = SmoothBandSilk_LYJ(shadowScene, _SceneShadowCenter, _SceneShadowSmooth);
                float stockingsShadowArea = saturate(min(stockingsLambertShadow, stockingsSceneShadow));
                float4 stockingsRamp = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(stockingsWrappedNdotL, 0.5));
                float3 stockingsShadowTint = lerp(_ShadowColor.rgb, 1.0.xxx, stockingsShadowArea);
                albedoDarkMore = lerp(diffuseDarkMoreStrength.xxx, albedoDarkMore, 1.2);
                float3 stockingsDiffuseBase = lerp(albedoDarkMore, albedoLight, saturate(stockingsWrappedNdotL * ao));
                float3 stockingsDiffuseRamp = stockingsDiffuseBase * stockingsRamp.rgb;
                float stockingsDiffuseBaseStrength = MyCalculateLightStrength(stockingsDiffuseBase);
                float stockingsDiffuseRampStrength = MyCalculateLightStrength(stockingsDiffuseRamp);
                float stockingsRampCompensation = stockingsDiffuseBaseStrength / max(0.01, stockingsDiffuseRampStrength);
                stockingsRampCompensation = clamp(stockingsRampCompensation, 0, 1.5);
                float3 finalDiffuseBRDF = stockingsDiffuseRamp * stockingsRampCompensation * stockingsShadowTint;
                float totalShadowArea = saturate(stockingsShadowArea * ao);
                   
                /////////////////////////////////////    全光照下 diffuse 混合 End     ////////////////////////////////

      
                /////////////////////////////////////    顶光 Start     ////////////////////////////////
                ///
                // otherLight 颜色混合只控制暗部，亮部仍保持白色，避免喧宾夺主，不直接控制整体亮度。
                // otherLight 不参与 ramp 混合。
                float topLightNoL = CharaEvaluateTopLightNoL(
                    normalWS,
                    _OtherLightOffset,
                    _OtherLightStrength,
                    _OtherLightStrength_Offset);

                float3 topLightDark = _OtherLightColor.rgb; 
                float3 otherLightColor = CharaEvaluateTopLightColor(topLightDark, totalShadowArea);
                float3 finalTopLight = otherLightColor * topLightNoL;
                float3 topLightDay1 = finalTopLight;
                /////////////////////////////////////    顶光 End     ////////////////////////////////


                /////////////////////////////////////    直接漫反射 Start     ////////////////////////////////
                ///  
                // 修正：加入阴影颜色。
                float3 mainlightNew = lerp(_ShadowColor.rgb , mainLightColor, totalShadowArea) * mainLightIntensity;
                // 主要影响：无光照时只考虑 otherLightResult_day0；有光照时为直射光加 otherLightResult_day1。
                float3 finalMainlightDifficus = mainlightNew + topLightDay1; 
                 

                float3 diffuseResult = finalMainlightDifficus * finalDiffuseBRDF;

                      
                /////////////////////////////////////    直接漫反射 End     ////////////////////////////////
                /// 
                
      
                /////////////////////////////////////    直接高光 Start     ////////////////////////////////
                float NoV = saturate(dot(normalWS, viewDir));
                float perceptualRoughness = saturate(roughness);
                float roughnessLinear = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);

                if (IsCharaShaderMaterialDebugEnabled())
                {
                    return GetCharaShaderDebugColor(baseColor, normalWS, normalTSRaw, normalTS, metallic, debugRoughness, rawMraTex.rgb, i.smoothNormalVertexColor, 1.0);
                }

                float3 fresnel0 = lerp(0.04.xxx * _StockingsSpecColor.rgb, stockingsBaseColor, metallic);
                float NdotV = NoV;
                float roughness2 = max(roughness * roughness, 0.0078);
                // 间接高光切到 Chara_Cloth_V2 同款 IBL 时，单独复用布料链路的 F0/RefineF0 输入，
                // 保留丝袜现有直射高光和各向异性响应，不把整套主高光一起改掉。
                float3 clothIblF0 = 0.04 * reflectivity.xxx + metallic * (stockingsBaseColor - reflectivity.xxx * 0.04);
                float3 halfDirWS = normalize(viewDir + mainLightDir);
                float NdotH = saturate(dot(normalWS, halfDirWS));
                float HdotV = saturate(dot(halfDirWS, viewDir));
                float stockingsD = DistributionGGXSilk_LYJ(NdotH, roughnessLinear);
                float stockingsG = GeometrySmithSilk_LYJ(saturate(NoL), NoV, roughnessLinear);
                float3 stockingsF = FresnelSchlickSilk_LYJ(HdotV, fresnel0);
                float3 baseSpecular = (stockingsD * stockingsG * stockingsF) / max(4.0 * saturate(NoL) * NoV, 1.0e-4);
                float shadowArea = totalShadowArea;
                float specularShadowStrength = CharaResolveSpecularShadowStrength(_SpecularShadowStrength);
                float selfAOShadowEffect = lerp(1 - specularShadowStrength, 1, shadowArea);
                baseSpecular *= mainLightColor * mainLightIntensity * _StockingsSpecStrength * selfAOShadowEffect;
              
                 // 各项异性高光表现
                float3 anisoSpecular = SilkEvaluateAnisoSpecular_LYJ(
                    normalWS,
                    fiberDirectionWS,
                    rawViewDir,
                    mainLightDir,
                    mainLightColor * mainLightIntensity,
                    selfAOShadowEffect,
                    silkBoost,
                    anisoNoiseOffset,
                    anisoBreakupMask,
                    anisoStrengthMask);

                float cameraLightDir_y = mainLightDir.y;
                float3 cameraLightDir = normalize(float3(cameraForward.x, cameraLightDir_y, cameraForward.z));
                float3 lightDirFixed = mainLightDir + 2 * cameraLightDir;
                float3 halfDirFixed = normalize(viewDir * 3 + lightDirFixed);
                float NdotH_ibl = dot(normalWS, halfDirFixed);
                float a2 = roughness2 * roughness2;
                float specularDForIbl = (a2 - 1) * NdotH_ibl * NdotH_ibl + 1;
                specularDForIbl *= specularDForIbl;
                specularDForIbl = a2 / specularDForIbl;

                #ifdef _NEEDREFINEF0_ON
                float refineF0_U = lerp(specularDForIbl * roughness2, NdotV * NdotV, _RefineF0U_lerp);
                float refineF0_V = roughness * (1 - ao);
                float4 refineF0Tex = SAMPLE_TEXTURE2D(_SpecularRefineF0Tex, sampler_BaseTex, float2(refineF0_U, refineF0_V));
                clothIblF0 *= refineF0Tex.xyz;
                #endif
                
                /////////////////////////////////////    直接高光 End     ////////////////////////////////

                /////////////////////////////////////    间接漫反射 Start     ////////////////////////////////
                ///
                // 间接漫反射在体感和偏平面效果之间切换。
                float3 indirectDiffuseplane = max(half3(0,0,0), half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w));
                float3 indirectDiffusevolume = SampleSH(normalWS);
                float3 indirectDiffuse = lerp(lerp(indirectDiffuseplane, indirectDiffusevolume, _IndirectDifficusPlaneVolume), 1.0.xxx, saturate(_GlobalCharacterRenderOverrideSceneAmbient)) * stockingsBaseColor;

                // 材质级和场景级环境光参数分层相乘，便于每个场景统一调整角色间接漫反射。
                half3 indirectTint = _IndirectTintColor.rgb * _GlobalIndirectTintColor;
                indirectDiffuse *= _IndirectLightIntensity * _GlobalIndirecIntensity;
                indirectDiffuse *= indirectTint;
                /////////////////////////////////////    间接漫反射 End    ////////////////////////////////

                /////////////////////////////////////    间接镜面反射 IBL Start     ////////////////////////////////
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

                float IBLspecular_brdf = IBLspecular_brdf1 / IBLspecular_brdf2;

                float scale_fit_part1 = dot(float2(-1.28514, 1.0), float2(NdotV, 0.990440011));
                float scale_fit_part2 = dot(float2(1.0, -0.75591), float2(1.29678, NdotV));
                float env_scale = dot(float2(scale_fit_part1, scale_fit_part2), float2(1, roughness2));
                float bias_fit_x = dot(float3(2.92338, 59.4188, 1.0), float3(NdotV, NoV3, 1.0));
                float bias_fit_y = dot(float3(1.0, -27.0302, 222.592), float3(20.3225, NdotV, NoV3));
                float bias_fit_z = dot(float3(626.130, 316.627, 1.0), float3(NdotV, NoV3, 121.563004));
                float bias_denominator = dot(float3(bias_fit_x, bias_fit_y, bias_fit_z), float3(1, roughness2, roughness6));
                float env_bias = env_scale / max(bias_denominator, 1.0e-4);

                float3 IBLspecular_brdf_final = IBLspecular_brdf * clothIblF0 + env_bias;
                float IBLspecular_brdf_final_noF0 = IBLspecular_brdf + env_bias;

                float directionalAlbedo = IBLspecular_brdf_final_noF0;
                float energyLossFactor = (1.0 - directionalAlbedo) / directionalAlbedo;
                float3 ms_compensation = clothIblF0 * energyLossFactor;
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

                float envMap_level = log2(max(0.01, roughness));
                envMap_level = max(0, envMap_level * 1.2 + 5.0);
                float3 environmentColor = SAMPLE_TEXTURECUBE_LOD(_EnvMap, sampler_LinearRepeat, rotatedDir, envMap_level).rgb;
                environmentColor *= _EnvColor.rgb;

                float3 stockingsIndirectSpecular = environmentColor * IBLBRDF * _EnvLightStrength;
                /////////////////////////////////////    间接镜面反射 IBL End     ////////////////////////////////
                /////////////////////////////////////    计算最终颜色    ////////////////////////////////
                ///
                float3 rimLightResult = 0;
                #ifdef _RIM_ON
                    float2 rimScreenUV = i.positionHCS.xy / _ScaledScreenParams.xy;
                    float3 rimNormalWS = lerp(normalize(i.normalWS), normalWS, _RimNormalBlend);
                    rimLightResult = CalculateRim(
                        rimScreenUV,
                        i.positionWS,
                        rimNormalWS,
                        viewDir,
                        mainLightDir,
                        shadowScene,
                        ao,
                        NoV,
                        1.0,
                        albedoLight);
                #endif
                rimLightResult *= rimLightMask;

                float3 mainLightResult = diffuseResult + baseSpecular + anisoSpecular;
                // 额外点光使用自身 F0 和强度驱动角色 V2 基础直接高光简化项，不参与各向异性与阴影计算。
                float3 addLightDiffuse;
                float3 addLightSpecular;
                AccumulateCharacterAdditionalLighting(
                    i.positionWS,
                    i.positionHCS,
                    normalWS,
                    viewDir,
                    stockingsBaseColor,
                    metallic,
                    roughness,
                    fresnel0,
                    _StockingsSpecStrength.xxx,
                    addLightDiffuse,
                    addLightSpecular);
                float3 addLightResult = addLightDiffuse + addLightSpecular;
                resultColor = mainLightResult + indirectDiffuse + stockingsIndirectSpecular + rimLightResult + addLightResult;

                half4 lightingDebugColor;
                if (TryApplyCharaShaderLightingDebug(
                    lightingDebugColor,
                    diffuseResult + addLightDiffuse,
                    baseSpecular + anisoSpecular + addLightSpecular,
                    indirectDiffuse,
                    stockingsIndirectSpecular,
                    ao.xxx,
                    shadowAttenuation,
                    shadowScene,
                    1.0))
                {
                    return lightingDebugColor;
                }

                #if defined(_DEATH_ON)
                    float2 screenUV = i.positionHCS.xy / _ScaledScreenParams.xy;
                    float deathMask = SAMPLE_TEXTURE2D(_DeathMask, sampler_DeathMask, TransformDeathMaskUV(screenUV * _DeathMaskTiling.xy, _DeathMaskUVOffset, _DeathMaskUVRotation)).r;
                    float3 deathMaskedColor = resultColor * lerp(_DeathInnerTint.rgb, _DeathOuterTint.rgb, deathMask);
                    resultColor = lerp(resultColor, deathMaskedColor, saturate(_DeathMaskBlendStrength));
                    resultColor = ApplyCharaFresnel(resultColor, normalWS, viewDir, _DeathFresnelScale, _DeathFresnelPower, _DeathFresnelColor);
                #endif

                resultColor = ApplyCharaRedTint(resultColor);
                resultColor = ApplyCharaFinalColorGradient(resultColor, i.gradientUV);

                /////////////////////////////////////    自发光 Start     ////////////////////////////////
                ///
                return ApplyCharaShaderFinalDebug(
                    resultColor,
                    CharaFinalColorGradientDebugColor(i.gradientUV),
                    saturate(_Alpha));
            }
            //#endregion
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

            #define CHARA_CLOTH_V2_VARIANT_SILK_STOCKINGS 1
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

            #define CHARA_CLOTH_V2_VARIANT_SILK_STOCKINGS 1
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

            #define CHARA_CLOTH_V2_VARIANT_SILK_STOCKINGS 1
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
    }
 
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
	CustomEditor "Scarecrow.SimpleShaderGUI"
}
