Shader "Valkyria/Chara/Chara_Face_V2"
{
    Properties
    {

        // RedTint (driven by script via MaterialPropertyBlock)
        [HideInInspector] _CharacterRedTint ("_CharacterRedTint", Range(0, 1)) = 0
        [HideInInspector] _CharacterRedTintColor ("_CharacterRedTintColor", Color) = (1, 0, 0, 1)
        [HideInInspector] _Alpha("Alpha", Range(0, 1)) = 1
        [Foldout(1, 1, 0, 0)] _baseSetting("渲染设置_Foldout", Float) = 1
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
        _OtherLightColor("OtherLightColor", Color) = (1, 1, 1, 1)
        _OtherLightOffset("OtherLightOffset", Range(-1, 1)) = 0
        _OtherLightStrength("OtherLightStrength", Range(0, 2)) = 0
        _OtherLightStrength_Offset("OtherLightStrength_Offset", Range(-1, 1)) = 0

        [Foldout(1, 1, 0, 1)] _surface("表面_Foldout", float) = 1
        [Foldout(2, 2, 0, 1)] _pbrBasic("PBR 基础_Foldout", float) = 1
        [NoScaleoffset][Tex(_BaseColor)]_BaseTex("基础贴图", 2D) = "white" {}
        [HideInInspector]_BaseColor("基础颜色", Color) = (1, 1, 1, 1)
        _BaseColorPower("基础颜色Power", Range(0, 4)) = 1
        [Tex][NoScaleoffset]_MaskTex("FaceMask:次表面范围(R)下颌范围(G)边缘光范围(A)", 2D) = "black" {}
        [Tex][NoScaleoffset]_SDFTex("SDF 贴图", 2D) = "white" {}
        
        [NoScaleoffset][Tex(_UseSkinLut)] _LutColorTex("肤色LUT", 2D) = "white" {}
        [HideInInspector][Toggle_Switch] _UseSkinLut("Use Skin LUT", Float) = 0
        

        [Foldout(1, 1, 0, 1)] _directLight("直接光照_Foldout", float) = 1
        _AlbedoDarkStrength("暗部颜色强度", Range(0, 1)) = 0.5
        _AlbedoDarkSaturation("暗部颜色饱和度", Range(0, 2)) = 1
        [GradientSplit]_RampTex("渐变贴图", 2D) = "white" {}
        _RampSaturationStrength("渐变色饱和度影响强度", Range(0, 1)) = 1
        _RampStrength("渐变色强度", Range(0, 1)) = 1
        _RampColorNoLStrength("渐变A通道混合程度", Range(0, 1)) = 1
        _ShadowColor("AO阴影颜色", Color) = (1, 1, 1, 1)
        [Foldout(2, 2, 1, 0)] _fresnel("次表面散射_Foldout", Float) = 0
        _FresnelPower("次表面散射Power", Range(0, 10)) = 1
        _FresnelInnerColor("次表面散射内侧颜色", Color) = (1, 1, 1, 1)
        _FresnelOuterColor("次表面散射外侧颜色", Color) = (1, 1, 1, 1)
        _FresnelSmoothMin("次表面散射平滑最小值", Range(0, 1)) = 0
        _FresnelSmoothMax("次表面散射平滑最大值", Range(0, 2)) = 1
        _FresnelStrength("次表面散射强度", Range(0, 1)) = 1
        
        [Foldout(1, 1, 0, 1)] _other("其他_Foldout", float) = 1
        
        [Foldout(2, 2, 0, 1)] _emoji("表情_Foldout", float) = 1
        _EmojiColor1("尴尬表情颜色", Color) = (0.5, 0.5, 0.5, 1)
        _EmojiStrength1("尴尬表情强度", Range(0, 1)) = 0
        [HideInInspector]_ChinColor("下巴颜色", Color) = (1, 1, 1, 1)
        [HideInInspector]_FaceBrightness("面部亮度", Range(0, 5)) = 1

        [Foldout(1, 1, 0, 1)] _ibl("间接漫反射_Foldout", Float) = 1
        [Tip(Warning)] _IndirectDiffuseTip("参数统一：间接漫反射强度 = 0.2。", Float) = 0
        _IndirectLightIntensity("间接漫反射强度", Range(0, 2)) = 0.2
        _IndirectTintColor("间接漫反射颜色", Color) =(0.8, 0.85, 1.0, 0)
        _IndirectDifficusPlaneVolume("间接漫反射0平1体感", Range(0, 1)) = 0.5

        [Foldout(1, 1, 0, 1)] _shadowControl("阴影控制_Foldout", float) = 1
        [Toggle_Switch] _SDF_UVSwitch("SDF使用1U", float) = 0
        [Enum_Switch(AfterSkinning, BeforeSkinning, CustomFaceDiretion)] _DebugFaceDiretion("面部SDF DEBUG", Float) = 0
        [Switch(CustomFaceDiretion)]_HeadForward("头部前向向量", Vector) = (0.0, 1.0, 0.0)
        [Switch(CustomFaceDiretion)]_HeadRight("头部右向向量", Vector) = (0.0, 0.0, -1.0)
        
        _SDFShadowCenter("SDF 阴影中心", Range(-1, 1)) = 0.1
        _SDFShadowSmooth("SDF 阴影平滑", Range(0, 1)) = 0.5


    
        [Foldout(1, 1, 1, 0)] _outline("描边_Foldout", float) = 0
        [Toggle_Switch] _UseOutlineMask("使用描边遮罩（不开启，描边统一写入顶点色）", Float) = 0
        [Switch(_UseOutlineMask)][NoScaleOffset][Tex] _OutlineMask("描边遮罩", 2D) = "white" {}
        [Toggle_Switch] _SmoothNormal("平滑法线", Float) = 1.0
        [Toggle_Switch] _OutlineMultiplyBaseColor("描边乘自身颜色", Float) = 1.0
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineWidth("描边宽度", Range(0, 1)) = 0.5
        _OutlineClipSpaceZOffset("描边 Z 偏移", Range(-10, 10)) = 0.0
        
        [Foldout(1, 1, 0, 1)] _ShadowControl("阴影_Foldout", Float) = 1
        [Foldout(2, 2)] _CSMPerShadow("CSMPerObject阴影_Foldout", Float) = 1
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

        [HideInInspector] [Enum(UnityEngine.Rendering.Universal.Internal.StencilUsage)] _FriStencil("模板 ID", Float) = 15
        [HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)] _FriStencilComp("模板比较", Float) = 3
        [HideInInspector] [Enum(UnityEngine.Rendering.StencilOp)] _FriStencilOp("模板操作", Float) = 2

        [HideInInspector] _FaceStencilWriteRef("Face Stencil Write Ref", Float) = 3
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

        [Foldout(1, 1, 1, 0)] _rim("边缘光_Foldout", Float) = 0
        [Toggle_Switch] _CustomLightDirection("自定义边缘光方向", float) = 0
        [Switch(_CustomLightDirection)] [Enum_Switch(View, World)] _CustomRimDirectionSpace("自定义边缘光方向空间", float) = 1
        [Vector(3, _CustomLightDirection)] _FresnelRimDirection("自定义边缘光方向", Vector) = (0, 0, 0, 0)
        _FresnelRimLightAlign("边缘光对光方向", Range(-1, 1)) = 1
        [HDR] _FresnelRimColor("边缘光颜色", Color) = (1, 1, 1, 1)
        _FresnelIntensity("边缘光强度", Range(0, 10)) = 3

        [Foldout(1, 1, 0, 1)] _finalColorGradient("最终颜色渐变_Foldout", Float) = 1
        _GradientColor("渐变颜色", Color) = (1, 1, 1, 1)
        _GradientMinY("渐变最小 Y", Float) = 0.0
        _GradientMaxY("渐变最大 Y", Float) = 1.0
        
        
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

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

            Stencil
            {
                Ref [_FaceStencilWriteRef]
                WriteMask [_FaceStencilWriteRef]
                Comp Always
                Pass Replace
            }
            ZWrite [_ZwriteOp]
            Cull[_Cull]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            // Temporary: compile character custom features into the base variant.
            #pragma shader_feature_local _ _CUSTOMENVIRONMENTLIGHT_ON
            #pragma shader_feature_local _ _CUSTOMENVCUBEMAP_ON
            #pragma shader_feature_local _ _FRESNEL_ON
            // #pragma shader_feature_local _ _DEPTHRIM_ON
            #pragma shader_feature_local _ _CUSTOMMAINLIGHT_ON
            #pragma shader_feature_local _ _OUTLINE_ON
            #pragma shader_feature_local _ _RIM_ON
            #pragma shader_feature_local _ _DEATH_ON
            // #define _CUSTOMENVIRONMENTLIGHT_ON 1
            // #define _CUSTOMENVCUBEMAP_ON 1
            // #define _DEPTHRIM_ON 1
            // #define _CUSTOMMAINLIGHT_ON 1
            // #define _OUTLINE_ON 1
            // #define _DEATH_ON 1
            // #pragma shader_feature _ADDITIONAL_LIGHTS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN  //主光
            #pragma multi_compile _ _ADDITIONAL_LIGHTS //额外光
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING         //反射球混合
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION   
            #pragma multi_compile_fragment _ _SHADOWS_SOFT            //软阴影
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION  // SSAO

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Common/Chara_AdditionalLights_V2.hlsl"
            #include "Common/ToonBRDF_V2.hlsl"
            #include "Common/Chara_ShaderDebug_V2.hlsl"

            //#region 顶点结构传入法线和切线
            struct appdata
            {
                float4 vertex : POSITION;
                //float4 tangent : TANGENT;   
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };
            //#endregion
            
            //#region 顶点输出结构要输出计算好的TBN
            struct v2f
            {
                float4 uv : TEXCOORD0;
                //float3 tangentWS : TEXCOORD1;
                //float3 bitangentWS : TEXCOORD2;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 sphereNormalWS : TEXCOORD3;
                float4 positionCS : SV_POSITION;
                float4 faceSDF : TEXCOORD4;
                float3 smoothNormalVertexColor : TEXCOORD5;
                float2 gradientUV : TEXCOORD6;
            };
            //#endregion
            
            #include "Chara_Face_V2/Chara_Face_V2_Bindings.hlsl"
            #include "Common/Chara_FinalColorGradient_V2.hlsl"
            #include "Common/Chara_Dithering_V2.hlsl"
            #include "Common/Chara_Globals_V2.hlsl"
            #include "Common/Chara_CommonHelpers_V2.hlsl"
            #include "Common/Chara_GlobalVirtualLight_V2.hlsl"
            #include "Common/Chara_CommonHelpers_V2.hlsl"
            #include "Common/Func_Chara_PerObjectShadow_V2.hlsl"

            
            

            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
            //#region 纹理采样时分离采样器和贴图（“双线性过滤+重复平铺”等）
            
            //#endregion
            

            // 面部 SDF、直接光和鼻梁高光都依赖统一后的主光方向，先在这里把场景补光和特效平行光揉进来。
            float3 ResolveFaceMainLightDirection(float3 mainLightDirection)
            {
                float3 resolvedDirection = CharaResolveMainLightDirectionWithOverride(
                    mainLightDirection,
                    _CustomMainLightPerObjectShadowDirectionEnabled,
                    _CustomMainLightPerObjectShadowDirection,
                    saturate(_GlobalCharacterRenderLightToggle),
                    _GlobalCharacterRenderLightDirection,
                    saturate(_EffectLightToggle),
                    _EffetDirectionDir);
                return CharaApplyGlobalVirtualLightAsMainLightDir(resolvedDirection);
            }

            float3 FaceSkinLinearToSrgb(float3 color)
            {
                float3 safeColor = max(color, 0.0);
                float3 linearSegment = safeColor * 12.9200001;
                float3 curveSegment = 1.05499995 * pow(max(safeColor, 1.0e-6), 1.0 / 2.4) - 0.0549999997;
                return lerp(curveSegment, linearSegment, step(safeColor, 0.00313080009));
            }

            float3 FaceSampleSkinLutColor(float3 albedoLinear)
            {
                float3 albedoSrgb = FaceSkinLinearToSrgb(albedoLinear.brg);
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

            // Skin V2 diffuse core. Face supplies its SDF/chin light area while
            // scene shadow is combined after ramp remapping, matching Skin V2.
            float3 EvaluateFaceSkinDiffuse(
                float noLArea,
                float sceneShadow,
                float3 albedoLight,
                float3 albedoDark,
                float3 albedoDarkMore,
                float albedoDarkStrength,
                float3 normalizedMainLightColor,
                float mainLightIntensity,
                out float3 mainLightColorFinal)
            {
                float4 rampColorNoL = SampleRamp(
                    TEXTURE2D_ARGS(_RampTex, sampler_RampTex),
                    saturate(noLArea),
                    0.125);

                float remapNoL = lerp(noLArea, rampColorNoL.a, _RampColorNoLStrength);
                float rampColorMax = max(max(rampColorNoL.r, rampColorNoL.g), rampColorNoL.b);
                float rampColorMin = min(min(rampColorNoL.r, rampColorNoL.g), rampColorNoL.b);
                float rampColorRange = rampColorMax - rampColorMin;
                float aoForLighting = 1.0;
                float aoShadow = aoForLighting * sceneShadow;
                float minShadowEffect = min(min(aoForLighting, sceneShadow), remapNoL);
                float totalShadowArea = minShadowEffect;

                float otherLightNoL = saturate(_OtherLightOffset) * _OtherLightStrength + _OtherLightStrength_Offset;
                float3 otherLightColor = lerp(_OtherLightColor.rgb, 1.0.xxx, minShadowEffect);
                float3 mainLightTint = lerp(_ShadowColor.rgb, normalizedMainLightColor, totalShadowArea) * mainLightIntensity;
                mainLightColorFinal = mainLightTint + otherLightColor * otherLightNoL;

                albedoDarkMore = lerp(albedoDarkStrength.xxx, albedoDarkMore, 1.2);
                float3 mainDiffuseColorDark = lerp(albedoDarkMore, albedoDark, saturate(aoShadow + remapNoL));
                float3 diffuseBRDF = lerp(mainDiffuseColorDark, albedoLight, totalShadowArea);
                float rampSaturationStrength = lerp(1.0, rampColorRange, _RampSaturationStrength);
                float3 rampColorNoLEffect = lerp(1.0.xxx, rampColorNoL.rgb, rampSaturationStrength);
                rampColorNoLEffect = lerp(1.0.xxx, rampColorNoLEffect, _RampStrength);
                float3 diffuseBRDFRamp = diffuseBRDF * rampColorNoLEffect;
                float rampColorControl = clamp(
                    MyCalculateLightStrength(diffuseBRDF) / max(0.01, MyCalculateLightStrength(diffuseBRDFRamp)),
                    0.0,
                    1.5);
                return diffuseBRDFRamp * rampColorControl;
            }

            // 面部背光补偿保持 SDF 平面分区，避免使用法线 Lambert 产生体积感。
            float EvaluateFaceSdfAttenuation(
                float2 baseSdfUV,
                float3 lightDirectionWS,
                float3 headForwardWS,
                float3 headRightWS)
            {
                float lDotR = dot(lightDirectionWS, headRightWS);
                float lDotF = dot(lightDirectionWS, -headForwardWS);
                float normalizedAngle = atan2(lDotR, lDotF) / PI;
                float angleThreshold = normalizedAngle > 0.0 ? 1.0 - normalizedAngle : normalizedAngle + 1.0;

                float flipLR = lDotR > 0.0 ? 1.0 : 0.0;
                float2 sdfUV = baseSdfUV;
                sdfUV.x = lerp(1.0 - sdfUV.x, sdfUV.x, flipLR);
                float sdf = SAMPLE_TEXTURE2D(_SDFTex, sampler_SDFTex, sdfUV).r;
                return sigmoid(sdf - angleThreshold, _SDFShadowCenter, _SDFShadowSmooth);
            }
            
            //#region 在顶点着色器中计算T\B\N
            v2f vert (appdata v)
            {
                v2f o = (v2f)0;
                o.positionCS = TransformObjectToHClip(v.vertex.xyz);
                o.uv.xy = v.uv0;
                o.uv.zw = v.uv1;
                o.gradientUV = v.uv2;
                o.smoothNormalVertexColor = v.color.rgb;
                //o.tangentWS = TransformObjectToWorldDir(v.tangent.xyz);//变换方向用Dir，变换顶点用World
                o.normalWS = TransformObjectToWorldNormal(v.normal);//在顶点着色器将法线从模型空间转到世界空间
                //o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangent.w * GetOddNegativeScale();//副切线方向通过法线和切线叉积得到
                o.positionWS = TransformObjectToWorld(v.vertex.xyz);
                // o.sphereNormalWS = TransformObjectToWorld(v.vertex.xyz) - _HeadCenter.xyz;
                Light mainLight = GetMainLight();

                mainLight.direction = ResolveFaceMainLightDirection(mainLight.direction);
                float3 localForward = float3(0,1,0);
                float3 localRight = float3(0,0,-1);
                    
                if (_DebugFaceDiretion == 1)
                {
                    localForward = float3(0,0,1);
                    localRight = float3(1,0,0);
                }
                else if (_DebugFaceDiretion == 2)
                {
                    localForward = _HeadForward.xyz;
                    localRight = _HeadRight.xyz;
                }
                
                float3 headForward = SafeNormalize(TransformObjectToWorldDir(localForward));
                float3 headRight = SafeNormalize(TransformObjectToWorldDir(localRight));
                float3 headUp = SafeNormalize(TransformObjectToWorldDir(float3(-1,0,0)));

                float3 lightDirForSdf = SafeNormalize(mainLight.direction);

                float lDotR = dot(lightDirForSdf, headRight);
                float lDotF = dot(lightDirForSdf, -headForward);

                float normalizedAngle = atan2(lDotR, lDotF) / PI;
                float angleThreshold = normalizedAngle > 0.0 ? 1.0 - normalizedAngle : normalizedAngle + 1.0;

                o.faceSDF.x = lDotR;
                o.faceSDF.y = angleThreshold;
                return o;
            }
            //#endregion

            //#region Fragment Shader
            half4 frag (v2f i) : SV_Target
            {
                ApplyCharacterDitheringBayer4x4(
                    i.positionCS,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
                // return float4(_FresnelPower.xxx,1);
                // float3 faceUpDir = SafeNormalize(TransformObjectToWorldDir(float3(0, 0, 1)));
                // float3 faceFrontDir = SafeNormalize(TransformObjectToWorldDir(float3(0, -1, 0)));
                // float3 faceRightDir = SafeNormalize(TransformObjectToWorldDir(float3(1, 0, 0)));
                
                float flipSDFThreshold = i.faceSDF.x;
                float angleThreshold = i.faceSDF.y;
                // return angleThreshold;
                
                float3 localForward = float3(0,1,0);
                float3 localRight = float3(0,0,-1);
                    
                if (_DebugFaceDiretion == 1)
                {
                    localForward = float3(0,0,1);
                    localRight = float3(1,0,0);
                }
                else if (_DebugFaceDiretion == 2)
                {
                    localForward = _HeadForward.xyz;
                    localRight = _HeadRight.xyz;
                }

                
                float3 headForward = SafeNormalize(TransformObjectToWorldDir(localForward));
                float3 headRight = SafeNormalize(TransformObjectToWorldDir(localRight));

                float4 baseTex = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, i.uv.xy);
                float noseDark = baseTex.a;
                float3 baseColor = baseTex.rgb * _BaseColor.rgb;
                baseColor = pow(max(baseColor, 0.0), max(_BaseColorPower, 0.001));

                float4 maskTex = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv.zw);
                float sssArea = saturate(maskTex.r);
                float chinAreaMask = saturate(maskTex.g);
                float rimArea = saturate(maskTex.a);
                float3 chinAreaTint = lerp(1.0.xxx, _ChinColor.rgb, chinAreaMask);
                baseColor *= chinAreaTint;

                float3 normalWS = SafeNormalize(i.normalWS);
                if (IsCharaShaderMaterialDebugEnabled())
                {
                    return GetCharaShaderDebugColor(baseColor, normalWS, float3(0.0, 0.0, 1.0), float3(0.0, 0.0, 1.0), 0.0, 1.0, maskTex.rgb, i.smoothNormalVertexColor, _Alpha);
                }

                float4 shadowCoords = TransformWorldToShadowCoord(i.positionWS);
                #if _MAIN_LIGHT_SHADOWS_SCREEN || _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_CASCADE
                    Light mainLight = GetMainLight(shadowCoords);
                #else
                    Light mainLight = GetMainLight();
                #endif

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

                float3 lightDir = ResolveFaceMainLightDirection(mainLight.direction);
                float3 viewDir = SafeNormalize(GetCameraPositionWS() - i.positionWS);
                float halfLambert = saturate(dot(normalWS, lightDir) * 0.5 + 0.5);
                float forwardViewFactor = dot(headForward, viewDir);

                // float fresnelMask = saturate(baseTex.a);
                float3 sssColorEffect = 1.0.xxx;
#ifdef _FRESNEL_ON
                float fresnelNoV = saturate(dot(normalWS, viewDir));
                float fresnelArea = saturate(1.0 - fresnelNoV * _FresnelSmoothMax + _FresnelSmoothMin);
                fresnelArea = pow(fresnelArea, max(_FresnelPower, 0.001));
                fresnelArea *= lerp(smoothstep(0.8, 1,dot(headForward, viewDir)) * sssArea, 1, chinAreaMask);
                // fresnelArea = smoothstep(_FresnelSmoothMin, _FresnelSmoothMax, fresnelArea);
                // fresnelArea *= lerp(1.0, 1, saturate(_FresnelMaskStrength));
                sssColorEffect = lerp(_FresnelInnerColor.rgb, _FresnelOuterColor.rgb, fresnelArea) ;
#endif
                
                float3 faceAlbedo = baseColor * lerp(1, sssColorEffect, _FresnelStrength) * lerp(0.5,1,noseDark);
                
                // return float4(fresnelColorEffect,1);

                float2 sdfUV = i.uv.zw;
                if (_SDF_UVSwitch > 0.5)
                {
                    sdfUV = i.uv.xy;
                }
                float2 baseSdfUV = sdfUV;
                float flipLR = flipSDFThreshold > 0.0 ? 1.0 : 0.0;
                sdfUV.x = lerp(1.0 - sdfUV.x, sdfUV.x, flipLR);
                float4 sdfTex = SAMPLE_TEXTURE2D(_SDFTex, sampler_SDFTex, sdfUV);
                float sdfMask = sdfTex.r;
                float emojiMask1 = sdfTex.g;
                faceAlbedo = lerp(faceAlbedo, _EmojiColor1, emojiMask1 * _EmojiStrength1);
                float sdfShadow = sigmoid(sdfMask - angleThreshold, _SDFShadowCenter, _SDFShadowSmooth);
                float lambertShadow = saturate(halfLambert);
                // Keep one direct-light path: SDF controls the face side and
                // Lambert controls chin, while SSS is removed in the chin area.
                float shadowAttenuation = ApplyPerObjectShadow(mainLight.shadowAttenuation, i.positionCS, _UsePerObjectShadow);
                float sceneShadow = 1.0;
                if (_UsePerObjectShadow > 0.5)
                {
                    sceneShadow = SigmoidSharp(shadowAttenuation, _SceneShadowCenter, _SceneShadowSmooth);
                }
                sceneShadow = saturate(sceneShadow);

                float backLight = saturate(-dot(viewDir, lightDir.xz));
                // sceneShadow = lerp(1,sceneShadow);
                sceneShadow = lerp(1, sceneShadow, chinAreaMask);
                // return flipSDFThreshold;
                float shadow = lerp(sdfShadow, lambertShadow, chinAreaMask);
                
                float3 albedoLight = faceAlbedo * 0.96;
                float3 lutAlbedo = (_UseSkinLut > 0.5) ? FaceSampleSkinLutColor(faceAlbedo) : faceAlbedo;
                
                
                float3 albedoDark = lutAlbedo * 0.96 * _AlbedoDarkStrength;
                float albedoDarkStrength = MyCalculateLightStrength(albedoDark);
                albedoDark = lerp(albedoDarkStrength.xxx, albedoDark, _AlbedoDarkSaturation);
                float3 mainLightColorFinal;
                float3 diffuseBRDF = EvaluateFaceSkinDiffuse(
                    shadow,
                    sceneShadow,
                    albedoLight,
                    albedoDark,
                    albedoDark * 0.65,
                    albedoDarkStrength,
                    mainLightColor,
                    mainLightIntensity,
                    mainLightColorFinal);
                float3 directLight = mainLightColorFinal * diffuseBRDF;
                float3 virtualLightDir = CharaGetVirtualLightDirectionWS(
                    lightDir,
                    CharaGetGlobalVirtualLightMaskCameraDirection(),
                    1.0,
                    0.0);
                float virtualBackLightMask = CharaGetVirtualLightMask(
                    lightDir,
                    CharaGetGlobalVirtualLightMaskCameraDirection(),
                    1.0,
                    1.0,
                    CharaGetGlobalVirtualLightBackLightMode());
                float virtualSdfAttenuation = EvaluateFaceSdfAttenuation(
                    baseSdfUV,
                    virtualLightDir,
                    headForward,
                    headRight);
                float3 virtualBackLight = albedoLight
                    * _GlobalCharacterRenderVirtualLightColor.rgb
                    * (virtualSdfAttenuation
                        * _GlobalCharacterRenderVirtualLightIntensity
                        * virtualBackLightMask
                        * (1.0 - chinAreaMask));
                directLight += virtualBackLight;

                /////////////////////////////////////    间接漫反射Start     ////////////////////////////////
                ///
                //间接漫反射体感和平面效果切换
                float3 indirectDiffuseplane = max(half3(0, 0, 0), half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w));
                float3 indirectDiffusevolume = SampleSH(normalWS);
                float3 indirectDiffuse = lerp(lerp(indirectDiffuseplane, indirectDiffusevolume, _IndirectDifficusPlaneVolume), 1.0.xxx, saturate(_GlobalCharacterRenderOverrideSceneAmbient)) * baseColor;
                indirectDiffuse *= _IndirectLightIntensity * _GlobalIndirecIntensity * (_IndirectTintColor.rgb * _GlobalIndirectTintColor);
                float3 indirectLight = indirectDiffuse;
                /////////////////////////////////////    间接漫反射End    ////////////////////////////////
                

                // Face follows Skin V2 additional-light accumulation; the 0.5 response
                // keeps the face's established weaker point-light contribution.
                float3 addLightDiffuse = 0.0.xxx;
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
                            // Face lighting is authored as a side-based SDF rather than
                            // a volumetric Lambert response. Reuse the same SDF split as
                            // the main light and virtual back light so an additional light
                            // does not introduce a harsh NdotL terminator on the cheeks.
                            float additionalLightSdfAttenuation = EvaluateFaceSdfAttenuation(
                                baseSdfUV,
                                SafeNormalize(light.direction),
                                headForward,
                                headRight);
                            addLightDiffuse += albedoLight
                                * light.color * light.distanceAttenuation * light.shadowAttenuation
                                * additionalLightSdfAttenuation;
                        }
                    }
                #endif
                
                float rimFlip = flipLR;
                float rimAngleThreshold = angleThreshold;
                if (_CustomLightDirection > 0.5)
                {
                    float3 customRimDirection = _FresnelRimDirection.xyz;
                    customRimDirection = dot(customRimDirection, customRimDirection) > 1.0e-6
                        ? normalize(customRimDirection)
                        : lightDir;
                    if (_CustomRimDirectionSpace < 0.5)
                    {
                        customRimDirection = TransformViewToWorldDir(customRimDirection, true);
                    }

                    customRimDirection = SafeNormalize(customRimDirection);
                    // 自定义方向必须同时驱动左右选择和宽度阈值；否则 View 方向与主光方向
                    // 不一致时，角色轻微摆动就可能让整段面部边缘光跨阈值消失。
                    float customSide = dot(customRimDirection, headRight);
                    float customForward = dot(customRimDirection, -headForward);
                    float customNormalizedAngle = atan2(customSide, customForward) / PI;
                    rimAngleThreshold = customNormalizedAngle > 0.0
                        ? 1.0 - customNormalizedAngle
                        : customNormalizedAngle + 1.0;

                    float customFlip = step(0.0, customSide);
                    float alignWeight = saturate(_FresnelRimLightAlign * 0.5 + 0.5);
                    rimFlip = lerp(1.0 - customFlip, customFlip, alignWeight);
                }

                float3 rimColor = 0;
                #if defined(_RIM_ON)
                    float rimLightArea = i.uv.x > 0.5 ? 1.0 : 0.0;
                    rimLightArea = lerp(rimLightArea, 1 - rimLightArea, rimFlip) * rimArea;
                    float3 rimTint = _FresnelRimColor.rgb * _FresnelRimColor.a * _FresnelIntensity;
                    rimColor = saturate((rimLightArea - rimAngleThreshold) * smoothstep(0, 0.2, rimAngleThreshold) * smoothstep(0.85, 1, dot(headForward, viewDir))) * rimTint;
                    rimColor *= saturate(_GlobalCharacterRenderRimLightToggle)
                        * max(_GlobalCharacterRenderRimIntensity, 0.0);
                #endif

                // return smoothstep(0.85, 1,saturate(dot(headForward, viewDir)));
                float3 finalColor = directLight + indirectLight + addLightDiffuse * 0.5 + rimColor;
                finalColor *= _FaceBrightness;
                half4 lightingDebugColor;
                if (TryApplyCharaShaderLightingDebug(
                    lightingDebugColor,
                    directLight + addLightDiffuse * 0.5,
                    float3(0.0, 0.0, 0.0),
                    indirectDiffuse,
                    float3(0.0, 0.0, 0.0),
                    float3(1.0, 1.0, 1.0),
                    shadowAttenuation,
                    sceneShadow,
                    _Alpha))
                {
                    return lightingDebugColor;
                }

 
                
                //角色死亡效果
                #if defined(_DEATH_ON)
                    float2 screenUV = i.positionCS.xy / _ScreenParams.xy;
                    float deathMask = SAMPLE_TEXTURE2D(_DeathMask, sampler_DeathMask, TransformDeathMaskUV(screenUV * _DeathMaskTiling.xy, _DeathMaskUVOffset, _DeathMaskUVRotation)).r;
                    float3 deathMaskedColor = finalColor * lerp(_DeathInnerTint.rgb, _DeathOuterTint.rgb, deathMask);
                    finalColor = lerp(finalColor, deathMaskedColor, saturate(_DeathMaskBlendStrength));
                    finalColor = ApplyCharaFresnel(finalColor, normalWS, viewDir, _DeathFresnelScale, _DeathFresnelPower, _DeathFresnelColor);
                #endif

                finalColor = ApplyCharaRedTint(finalColor);
                finalColor = ApplyCharaFinalColorGradient(finalColor, i.gradientUV);
                return ApplyCharaShaderFinalDebug(
                    finalColor,
                    CharaFinalColorGradientDebugColor(i.gradientUV),
                    _Alpha);
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

            #include "Chara_Face_V2/Chara_Face_V2_Bindings.hlsl"
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

            #include "Chara_Face_V2/Chara_Face_V2_Bindings.hlsl"
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
            #pragma shader_feature _PLANARSHADOW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Chara_Face_V2/Chara_Face_V2_Bindings.hlsl"
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
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex CharaShadowPassVertex
            #pragma fragment CharaShadowDitheringFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #define Attributes CharaShadowAttributes
            #define Varyings CharaShadowVaryings
            #define ShadowPassVertex CharaShadowPassVertex
            #define ShadowPassFragment CharaShadowBaseFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            #undef ShadowPassVertex
            #undef ShadowPassFragment
            #undef Varyings
            #undef Attributes
            #include "Common/Chara_Dithering_V2.hlsl"

            float _CharacterDitheringFactor;
            float _CharacterDitheringPixelSize;

            half4 CharaShadowDitheringFragment(CharaShadowVaryings input) : SV_TARGET
            {
                ApplyCharacterDitheringBayer4x4(
                    input.positionCS,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
                return CharaShadowBaseFragment(input);
            }
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
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex CharaDepthNormalsPassVertex
            #pragma fragment CharaDepthNormalsDitheringFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #define Attributes CharaDepthNormalsAttributes
            #define Varyings CharaDepthNormalsVaryings
            #define DepthNormalsVertex CharaDepthNormalsPassVertex
            #define DepthNormalsFragment CharaDepthNormalsBaseFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            #undef DepthNormalsVertex
            #undef DepthNormalsFragment
            #undef Varyings
            #undef Attributes
            #include "Common/Chara_Dithering_V2.hlsl"

            float _CharacterDitheringFactor;
            float _CharacterDitheringPixelSize;

            void CharaDepthNormalsDitheringFragment(
                CharaDepthNormalsVaryings input,
                out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
#endif
            )
            {
                ApplyCharacterDitheringBayer4x4(
                    input.positionCS,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
                CharaDepthNormalsBaseFragment(
                    input,
                    outNormalWS
#ifdef _WRITE_RENDERING_LAYERS
                    , outRenderingLayers
#endif
                );
            }
            ENDHLSL
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
