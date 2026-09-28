Shader "Valkyria/Chara/Chara_Hair_V2"
{
    Properties
    {
        // RedTint (driven by script via MaterialPropertyBlock)
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
        [Tex(_NormalScale)][NoScaleoffset]_NormalMap("法线贴图", 2D) = "bump" {}
        [HideInInspector]_NormalScale("法线强度", Range(0, 4)) = 1
        [Tex][NoScaleoffset]_HairMaskTex("Aniso:球形法线mask(R)高光范围(G)AO(B)高光强度(A)", 2D) = "white" {}
        [Tex][NoScaleoffset]_HairLineTex("LineNoise:发丝(R)各向异性噪声(G)", 2D) = "white" {}
        [Tex][NoScaleoffset]_OutlineMask("LineMask:描边(R)渐变(G)发丝阴影(B)", 2D) = "white" {}
        
        
        [Foldout(2,2)]_MRAChange("渐变AO_Foldout", Float) = 1
        _AOOffset("AO 强度", Range(0, 2)) = 1
        _AOContrast("AO 对比度", Range(0, 4)) = 1
        _AOPower("AO Power", Range(0, 4)) = 1

        _HairLine_ST("发丝Tiling&Offset", Vector) = (1, 1, 0, 0)
        _HairLineColor("发丝颜色", Color) = (1, 1, 1, 1)
        
        [HDR]_RampLightColor("渐变亮部颜色", Color) = (1, 1, 1, 1)
        [HDR]_RampDarkColor("渐变暗部颜色", Color) = (1, 1, 1, 1)
        
        [Foldout(1, 1, 0, 1)] _directLight("直接漫反射_Foldout", Float) = 1
        _AlbedoDarkStrength("暗部颜色强度", Range(0, 1)) = 0.8
        _AlbedoDarkSaturation("暗部饱和度", Range(0, 2)) = 1
        _BackLightStrength("背光补偿强度", Range(0, 1)) = 0
        [GradientSplit][NoScaleoffset]_RampTex("渐变贴图", 2D) = "white" {}
        _RampSaturationStrength("渐变色饱和度影响强度", Range(0, 1)) = 0
        _RampStrength("渐变色强度", Range(0, 1)) = 1
        _RampColorNoLStrength("A通道混合程度", Range(0, 1)) = 1
        
        _ShadowColor("AO+阴影颜色", Color) = (0.5, 0.5, 0.5, 1)
        [Foldout(3, 3, 0, 1)] _shadowControl("阴影过渡控制_Foldout", float) = 1
        _SphereNormalLerp("球形法线混合强度", Range(0, 1)) = 0
        _HairShadowLerp("发丝阴影混合强度", Range(0, 1)) = 0
        _HairShadowOffset("发丝阴影偏移", Range(-1, 1)) = 0.5

        [Foldout(1, 1, 1, 0)] _anisotropic("各向异性高光_Foldout", float) = 1
        
        _ForwardDirStrength("视线朝向权重", Range(0, 1)) = 0
        [HideInInspector]_NoFStrength("NoF 强度", Range(0, 2)) = 1
        [HideInInspector]_NoFPow("NoF 幂次", Range(0.1, 8)) = 1
        
        [Foldout(2, 2, 0, 1)] _anisoPosition("高光形状位置_Foldout", float) = 0
        _AnisoLength("各向异性长度", Range(0, 1)) = 0.5
        _AnisoOffset("球形高光偏移", Range(-1, 1)) = 0
        _AnisoOffset2("Aniso.R切线高光偏移", Range(-1, 1)) = 0
        _AnisoCut("各向异性裁剪", Range(-1, 1)) = 1
        _Shift_ST("各向异性噪声Tiling&Offset", Vector) = (1, 1, 0, 0)
        _ShiftNoise("球形高光噪声强度", Range(-1, 1)) = 0
        _ShiftNoise2("Aniso.R切线高光噪声强度", Range(-1, 1)) = 0
        [Foldout(2, 2, 0, 1)] _anisoStrength("高光颜色强度_Foldout", float) = 0
        [Gradient] _SpecRampTex("高光渐变贴图", 2D) = "white" {}
        
        _AnisoPower("各向异性幂值", Range(0.001, 5)) = 2
        _SpecularShadowStrength("阴影作用强度", Range(0, 1)) = 1
        [Range]_AnisoContrast("各项异性Mask强度范围", float) = (0,1,0,1)
        _anisoArea2Shadow("各项异性Mask暗部强度", Range(0, 1)) = 0

        _AnisoStrength("各向异性强度", Range(0, 10)) = 1
        _AnisoAnotherColor("各向异性副高光颜色", Color) = (0.5, 0.5, 0.5, 1)
        _AnisoAnotherLength("各向异性副高光长度", Range(1, 3)) = 1
        _AnisoAnotherStrength("各向异性副高光强度", Range(0, 2)) = 0
        
        
        [Foldout(2, 2, 1, 1)] _DOUBLE_SPECULAR("旧主副高光（与各向异性位置有偏差）_Foldout", float) = 0
        // [Toggle_Switch] _DOUBLE_SPECULAR("主副高光", Float) = 0
        _HSpecularColor1("主高光颜色", Color) = (1, 1, 1, 1)
        _SpecStrength1("主高光强度", Range(0, 1)) = 1
        _SpecGloss1("主高光幂值", Range(0, 600)) = 90
        _NormalShift1("主高光偏移", Range(-10, 10)) = -0.6
        _HSpecularColor2("副高光颜色", Color) = (0.5, 0.5, 0.5, 1)
        _SpecStrength2("副高光强度", Range(0, 1)) = 1
        _SpecGloss2("副高光幂值", Range(0, 600)) = 20
        _NormalShift2("副高光偏移", Range(-10, 10)) = -0.2

        [Foldout(2, 2, 0, 1)] _anisoSecond("发尾高光_Foldout", float) = 0
        _AnisoSecondLength("发尾高光长度", Range(0, 1)) = 1
        _AnisoSecondPosition("发尾高光位置", Range(-1, 1)) = 0
        _AnisoSecondOffset("发尾高光偏移", Range(-10, 10)) = 0
        _AnisoSecondStrength("发尾高光强度", Range(0, 10)) = 0
        
        [Foldout(1, 1, 0, 1)] _IndirectLight("间接光照_Foldout", Float) = 1
        [Foldout(2, 2, 0, 1)] _ibl("间接漫反射_Foldout", Float) = 1
        [Tip(Warning)] _IndirectDiffuseTip("参数统一：间接漫反射强度 = 0.2。", Float) = 0
        _IndirectLightIntensity("间接漫反射强度", Range(0, 2)) = 0.2
        _IndirectTintColor("间接漫反射颜色", Color) =(0.8, 0.85, 1.0, 0)
        _IndirectDifficusPlaneVolume("间接漫反射0平1体感", Range(0, 1)) = 0

        [Foldout(1, 1, 1, 0)] _fresnel("菲涅尔_Foldout", float) = 0
        _FresnelPower("菲涅尔强度", Range(0, 10)) = 1
        _FresnelInnerColor("菲涅尔内侧颜色", Color) = (1, 1, 1, 1)
        _FresnelOuterColor("菲涅尔外侧颜色", Color) = (1, 1, 1, 1)
        _FresnelSmoothMin("菲涅尔平滑最小值", Range(0, 1)) = 0
        _FresnelSmoothMax("菲涅尔平滑最大值", Range(0, 2)) = 1

        [Foldout(1, 1, 1, 0)] _Rim("边缘光_Foldout", float) = 0
        [Enum_Switch(FresnelRim, DepthRim, Add)] _RimType("边缘光类型", float) = 0
        [HideInInspector] _CustomLightDirection("自定义边缘光方向", float) = 0
        [HideInInspector] _CustomRimDirectionSpace("自定义边缘光方向空间", float) = 1
        [HideInInspector] _FresnelRimDirection("自定义边缘光方向", Vector) = (0, 0, 0, 0)
        [HideInInspector] _FresnelRimLightAlign("边缘光对光方向", Range(-1, 1)) = 1
        _FresnelRimColor("边缘光颜色", Color) = (1, 1, 1, 1)
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

        [Foldout(1, 1, 1, 0)] _outline("描边_Foldout", float) = 0
        [Toggle_Switch] _UseOutlineMask("使用描边遮罩", Float) = 0
        [Toggle_Switch] _SmoothNormal("平滑法线", Float) = 1.0
        [Toggle_Switch] _OutlineMultiplyBaseColor("描边乘自身颜色", Float) = 1.0
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineWidth("描边宽度", Range(0, 1)) = 0.5
        _OutlineClipSpaceZOffset("描边 Z 偏移", Range(-10, 10)) = 0.0
        
        [Foldout(1, 1, 0, 1)] _ShadowControl("阴影_Foldout", Float) = 1
        [Foldout(2,2)] _fringe("刘海阴影透射_Foldout", float) = 1
        _OuterAlpha("眉眼透明度", Range(0, 1)) = 0.5
        _FringeOuterVerticalAtten("外层上下衰减", Range(0, 1)) = 0
        _FringeOuterHorizontalAtten("外层左右衰减", Range(0, 1)) = 0

        [Foldout(2,2)]_CSMPerShadow("CSMPerObejct阴影_Foldout", Float) = 1
        [Enum_Switch(Off, UnityOnly, POSOnly, Both)] _UsePerObjectShadow("Per Object Shadow Mode", Float) = 0
        [Switch(UnityOnly, Both, POSOnly)]_SceneShadowCenter("场景阴影中心", Range(0, 1)) = 0.5
        [Switch(UnityOnly, Both, POSOnly)]_SceneShadowSmooth("场景阴影平滑", Range(0, 1)) = 0.1
        [Switch(UnityOnly, Both, POSOnly)]_ShadowStrength("场景阴影强度", Range(0, 2)) = 1
        [Foldout(2, 2, 1, 0)] _planarShadow("平面阴影_Foldout", float) = 0
        _PlanarShadowFalloff("平面阴影衰减", Float) = 1.0
        _PlanarShadowRange("平面阴影范围", Float) = 0.0
        _PlanarShadowLightDir("光照方向 xyz + 平面高度 w", Vector) = (0, 1, 0, 0)
        _PlanarShadowGlobalCenter("平面阴影全局中心", Vector) = (0, 0, 0, 0)
        _PlanarShadowColor("平面阴影颜色", Color) = (0, 0, 0, 1)
        

        [HideInInspector] [Enum(UnityEngine.Rendering.Universal.Internal.StencilUsage)] _Stencil("模板 ID", Float) = 16
        [HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("模板比较", Float) = 5
        [HideInInspector] [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp("模板操作", Float) = 0
        [HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)] _OuterStencilComp("外层模板比较", Float) = 3
        
        [HideInInspector] [Enum(UnityEngine.Rendering.Universal.Internal.StencilUsage)] _FriStencil("模板 ID", Float) = 15
        [HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)] _FriStencilComp("模板比较", Float) = 6
        [HideInInspector] [Enum(UnityEngine.Rendering.StencilOp)] _FriStencilOp("模板操作", Float) = 2
        
        

        [HideInInspector] _HeadUp("头部上方向向量", Vector) = (0.0, 0.0, 0.0)
        [HideInInspector] _HeadCenter("头部中心位置", Vector) = (0.0, 0.0, 0.0)
        [HideInInspector] _HeadRight("头部右向向量", Vector) = (0.0, 0.0, 0.0)
        [HideInInspector] _HeadForward("头部前向向量", Vector) = (1.0, 1.0, 1.0)

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
        Tags { "RenderType" = "Opaque" }
        ZWrite On
        ZTest LEqual
        Cull [_Cull]

       
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
        
        //Base Pass
        Pass
        {
            // Tags{"LightMode" = "UniversalForward"}
            Tags{"LightMode" = "Fringe"}
            Stencil
            {
                Ref[_Stencil]
                Comp[_StencilComp]
                Pass[_StencilOp]
            }
            ZWrite [_ZwriteOp]
            Cull[_Cull]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            // Temporary: compile character custom features into the base variant.
            #pragma shader_feature _FRESNEL_ON
            #pragma shader_feature _RIM_ON
            #pragma shader_feature _ANISOTROPIC_ON
            #pragma shader_feature _DOUBLE_SPECULAR_ON
            #pragma shader_feature_local _ _DEATH_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN  //主光
            #pragma multi_compile _ _ADDITIONAL_LIGHTS //额外光
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING         //反射球混合
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION   
            #pragma multi_compile_fragment _ _SHADOWS_SOFT            //软阴影
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION  // SSAO

            #define CHARA_RIM_MASK_USE_ROOT_POSITION
            #define CHARA_RIM_USE_GLOBAL_CONTROLS
            #include "Chara_Hair_V2/Chara_HairFringeForwardShared_V2.hlsl"
            #undef CHARA_RIM_USE_GLOBAL_CONTROLS
            #undef CHARA_RIM_MASK_USE_ROOT_POSITION
            ENDHLSL
        }
        // Fringe Outer
        Pass
        {
            Name "FringeOuter"
            Tags
            {
                "LightMode" = "FringeOuter"
            }

            Stencil
            {
                Ref[_Stencil]
                Comp[_OuterStencilComp]
                Pass[_StencilOp]
            }

            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha
            // ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Temporary: compile character custom features into the base variant.
            #pragma shader_feature _FRESNEL_ON
            #pragma shader_feature _RIM_ON
            #pragma shader_feature _ANISOTROPIC_ON
            #pragma shader_feature _DOUBLE_SPECULAR_ON
            #pragma shader_feature_local _DEATH_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN  //主光
            #pragma multi_compile _ _ADDITIONAL_LIGHTS //额外光
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING         //反射球混合
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION   
            #pragma multi_compile_fragment _ _SHADOWS_SOFT            //软阴影
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION  // SSAO

            #define CHARA_HAIR_FRINGE_OUTER_PASS 1
            #define CHARA_HAIR_FRINGE_FORWARD_ALPHA (_Alpha * _OuterAlpha)
            #define CHARA_RIM_MASK_USE_ROOT_POSITION
            #define CHARA_RIM_USE_GLOBAL_CONTROLS
            #include "Chara_Hair_V2/Chara_HairFringeForwardShared_V2.hlsl"
            #undef CHARA_RIM_USE_GLOBAL_CONTROLS
            #undef CHARA_RIM_MASK_USE_ROOT_POSITION
            #undef CHARA_HAIR_FRINGE_FORWARD_ALPHA
            #undef CHARA_HAIR_FRINGE_OUTER_PASS
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

            #include "Chara_Hair_V2/Chara_Hair_V2_Bindings.hlsl"
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

            #include "Chara_Hair_V2/Chara_Hair_V2_Bindings.hlsl"
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
            #pragma shader_feature _PLANARSHADOW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Chara_Hair_V2/Chara_Hair_V2_Bindings.hlsl"
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
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

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

    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
