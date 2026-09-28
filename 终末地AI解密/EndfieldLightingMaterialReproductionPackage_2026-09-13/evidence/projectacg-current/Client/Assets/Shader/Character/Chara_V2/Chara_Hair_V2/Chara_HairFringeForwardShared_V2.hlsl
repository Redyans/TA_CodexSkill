// Shared forward lit implementation for Chara_Hair_V2 and Chara_Fringe_V2.
#ifndef CHARA_HAIR_FRINGE_FORWARD_ALPHA
#define CHARA_HAIR_FRINGE_FORWARD_ALPHA (_Alpha)
#endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Common/Chara_AdditionalLights_V2.hlsl"
            #include "Common/ToonBRDF_V2.hlsl"
            #include "Common/Chara_ShaderDebug_V2.hlsl"
            #include "Common/Chara_GlobalVirtualLight_V2.hlsl"

            // Hair-specific anisotropic and dual-lobe specular helpers stay local to the
            // hair/fringe forward implementation so this family remains self-contained.
            half CharaHairSpecularLobe(half3 normalWS, half3 bitangentWS, half3 halfDir, half normalShift, half gloss)
            {
                half3 shiftedNormal = SafeNormalize(normalWS * normalShift + bitangentWS);
                half dotTH = dot(shiftedNormal, halfDir);
                half sinTH = sqrt(saturate(1.0 - dotTH * dotTH));
                half dirAtten = smoothstep(-1.0, 0.0, dotTH);
                return saturate(dirAtten * pow(sinTH, gloss));
            }

            half3 CharaHairDoubleSpecular(
                half3 normalWS,
                half3 bitangentWS,
                half3 halfDir,
                half specMask,
                half shadow,
                half3 mainColor,
                half mainStrength,
                half mainGloss,
                half mainShift,
                half3 subColor,
                half subStrength,
                half subGloss,
                half subShift)
            {
                half mainSpec = CharaHairSpecularLobe(normalWS, bitangentWS, halfDir, mainShift, mainGloss);
                half subSpec = CharaHairSpecularLobe(normalWS, bitangentWS, halfDir, subShift, subGloss);
                half3 specular = mainSpec * mainColor * mainStrength + subSpec * subColor * subStrength;
                specular *= specMask * shadow;
                return specular;
            }

            float3 ClampViewHeight(float3 worldViewDir)
            {
                float viewY=clamp(worldViewDir.y,0.2,1);
                float len=sqrt(max(1-viewY*viewY,0));
                float2 viewXZ=normalize(worldViewDir.xz)*len;
                return float3(viewXZ.x,viewY,viewXZ.y);
            }

            // Fixed-view anisotropic highlight: use viewDir instead of halfDir.
            void CharaHairFringeAnisoRampSpec(
                float3 bitangentWS,
                float3 sphereNormalWS,
                
                float3 viewDir,
                float3 upWS,
                float shift,
                float shiftNoiseScale,
                float anisoOffset,
                float anisoLength,
                float anisoCut,
                TEXTURE2D_PARAM(specRampTex, samplerSpecRampTex),
                out float3 anisoColor,
                out float anisoArea)
            {
                // float3 bitangent = TransformWorldToTangentDir(bitangentWS);
                bitangentWS.xyz += sphereNormalWS.xyz * (shift * shiftNoiseScale);
                float3 bitangentShift = SafeNormalize(bitangentWS);
                float3 viewDirOffset = SafeNormalize(viewDir + upWS * anisoOffset);
                // viewDirOffset = ClampViewHeight(viewDirOffset);
                float bDotVOffset = dot(bitangentShift, viewDirOffset);
                float rampU = smoothstep(0, 1, sqrt(max(0, bDotVOffset * 1)));
                // return rampU;
                anisoArea = smoothstep(anisoLength, 0, abs(bDotVOffset));
                
                // anisoArea = smoothstep(0, 0.1, anisoArea);
                
                                
                // anisoColorArea = pow(1-BdotVOffset * BdotVOffset, 0.5) + _AnisoColorOffset;
                                
                // anisoArea = smoothstep(_AnisoLength, 0, abs(BdotVOffset));
                // float anisoAreaCut = pow(sqrt(saturate(1.0 - BdotVOffsetCut * BdotVOffsetCut)), 400 * _AnisoLength);
                // anisoAreaCut = smoothstep(0, 0.1, anisoAreaCut);
                // anisoArea = saturate(anisoArea - anisoAreaCut);
                
                float3 bitangentCut = SafeNormalize(bitangentWS + sphereNormalWS * shift * shiftNoiseScale * 5 * anisoCut);
                bitangentCut = SafeNormalize(bitangentWS + sphereNormalWS * anisoCut);
                float bDotVOffsetCut = dot(bitangentCut, viewDirOffset);
                float anisoCutMask = smoothstep(0.3, 0, abs(bDotVOffsetCut));
                // anisoCutMask = smoothstep(anisoLength, 0, abs(bDotVOffsetCut));
                // anisoArea = saturate(anisoArea * anisoCutMask);
                anisoCutMask = smoothstep(0.02, 0, anisoCutMask);
                // anisoArea = saturate(anisoArea - anisoCutMask);
                anisoArea = saturate(anisoArea * anisoCutMask);
                anisoColor = SampleRamp(TEXTURE2D_ARGS(specRampTex, samplerSpecRampTex), anisoArea, 1).rgb;// Sampling by area works better here.
                // anisoArea = rampU;
                
            }

            float4 ResolveHairSpaceDebugColor(
                float3 viewDir,
                float3 rawSphereNormalWS,
                float3 sphereNormalWS,
                float3 tangentWS,
                float3 bitangentWS,
                float3 headUpWS,
                float sphereMask,
                float alpha)
            {
                int debugMode = (int)round(_CharacterShaderDebugMode);
                float3 direction = 0.0.xxx;

                if (debugMode == 23)
                {
                    direction = viewDir;
                }
                else if (debugMode == 24)
                {
                    direction = sphereNormalWS;
                }
                else if (debugMode == 25)
                {
                    direction = tangentWS;
                }
                else if (debugMode == 26)
                {
                    direction = bitangentWS;
                }
                else if (debugMode == 27)
                {
                    direction = headUpWS;
                }
                else if (debugMode == 28)
                {
                    direction = rawSphereNormalWS;
                }
                else if (debugMode == 29)
                {
                    return float4(saturate(sphereMask).xxx, alpha);
                }

                if (debugMode < 23 || debugMode > 28)
                {
                    return float4(0.0, 0.0, 0.0, -1.0);
                }

                return float4(saturate(SafeNormalize(direction) * 0.5 + 0.5), alpha);
            }

            // Computes the secondary highlight area without sampling the ramp.
            void CharaHairFringeAnisoRampSpec(
                float3 bitangentWS,
                float3 sphereNormalWS,
                
                float3 viewDir,
                float3 upWS,
                float shift,
                float shiftNoiseScale,
                float anisoOffset,
                float anisoLength,
                float anisoCut,
                out float anisoArea)
            {
                // float3 bitangent = TransformWorldToTangentDir(bitangentWS);
                bitangentWS.xyz += sphereNormalWS.xyz * (shift * shiftNoiseScale);
                float3 bitangentShift = SafeNormalize(bitangentWS);
                float3 viewDirOffset = SafeNormalize(viewDir + upWS * anisoOffset);
                // viewDirOffset = ClampViewHeight(viewDirOffset);
                float bDotVOffset = dot(bitangentShift, viewDirOffset);

                anisoArea = smoothstep(anisoLength, 0, abs(bDotVOffset));
                // anisoArea = pow(sqrt(saturate(1.0 - bDotVOffset * bDotVOffset)), 200 * max(0.001,(1 - anisoLength)));
                // anisoArea = smoothstep(0, 0.1, anisoArea);
                
                float3 bitangentCut = SafeNormalize(bitangentWS + sphereNormalWS * anisoCut);
                float bDotVOffsetCut = dot(bitangentCut, viewDirOffset);
                float anisoCutMask = smoothstep(0.3, 0, abs(bDotVOffsetCut));
                anisoCutMask = smoothstep(0.02, 0.01, anisoCutMask);
                anisoArea = saturate(anisoArea);
                
            }

            // Test branch: anisotropic highlight driven by half vector.
            void CharaHairFringeAnisoRampSpec(
                float3 bitangentWS,
                float3 sphereNormalWS,
                float3 lightDir,
                float3 viewDir,
                float3 upWS,
                float shift,
                float shiftNoiseScale,
                float anisoOffset,
                float anisoLength,
                float anisoCut,
                TEXTURE2D_PARAM(specRampTex, samplerSpecRampTex),
                out float3 anisoColor,
                out float anisoArea)
            {
                float3 bitangentShift = SafeNormalize(bitangentWS + sphereNormalWS * (shift * shiftNoiseScale));
                float3 viewDirOffset = SafeNormalize(viewDir + upWS * anisoOffset);
                viewDirOffset = normalize(lightDir + viewDirOffset);
                float bDotVOffset = dot(bitangentShift, viewDirOffset);

                anisoArea = smoothstep(anisoLength, 0, abs(bDotVOffset));
                anisoColor = SampleRamp(TEXTURE2D_ARGS(specRampTex, samplerSpecRampTex), anisoArea, 1).rgb;
                anisoArea = smoothstep(0, 0.1, anisoArea);
                

                float3 bitangentCut = SafeNormalize(bitangentWS + sphereNormalWS * anisoCut);
                float bDotVOffsetCut = dot(bitangentCut, viewDirOffset);
                float anisoCutMask = smoothstep(0.3, 0, abs(bDotVOffsetCut));
                anisoCutMask = smoothstep(0.02, 0.01, anisoCutMask);
                anisoArea = saturate(anisoArea * anisoCutMask);
                
            }

            float3 GetFixedViewDir(float3 worldViewDir)
            {
                float viewY=clamp(worldViewDir.y,0,0.7);
                float len=sqrt(max(1-viewY*viewY,0));
                // GLTF -forward
                float2 viewXZ=TransformObjectToWorld(half3(0,1,0)).xz*len;
                return float3(viewXZ.x,viewY,viewXZ.y);
            }

            //#region 顶点结构传入法线和切线
            struct appdata
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;   
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
                float3 tangentWS : TEXCOORD1;
                float3 bitangentWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                float3 sphereNormalWS:TEXCOORD5;
                float3 smoothNormalVertexColor : TEXCOORD6;
                float2 gradientUV : TEXCOORD7;
                float4 positionHCS : SV_POSITION;
            };
            //#endregion
   
            #include "Chara_Hair_V2/Chara_Hair_V2_Bindings.hlsl"
            #include "Common/Chara_FinalColorGradient_V2.hlsl"
            #include "Common/Chara_CommonHelpers_V2.hlsl"
            #include "Common/Chara_Globals_V2.hlsl"
            #include "Common/Chara_RimShared_V2.hlsl"
            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
            #include "Common/Func_Chara_PerObjectShadow_V2.hlsl"
            #include "Common/Chara_Dithering_V2.hlsl"
            
            
            v2f vert (appdata v)
            {
                v2f o;
                o.positionHCS = TransformObjectToHClip(v.vertex.xyz);
                o.uv.xy = v.uv0;
                o.uv.zw = v.uv1;
                o.gradientUV = v.uv2;
                o.smoothNormalVertexColor = v.color.rgb;
                o.tangentWS = TransformObjectToWorldDir(v.tangent.xyz);//变换方向用Dir，变换顶点用World
                o.normalWS = TransformObjectToWorldNormal(v.normal);//在顶点着色器将法线从模型空间转到世界空间
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangent.w * GetOddNegativeScale();//副切线方向通过法线和切线叉积得到
                o.positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.sphereNormalWS = TransformObjectToWorld(v.vertex.xyz) - _HeadCenter.xyz;
                
                return o;
            }

            //#region Fragment Shader
            half4 frag (v2f i) : SV_Target
            {
                ApplyCharacterDitheringBayer4x4(
                    i.positionHCS,
                    _CharacterDitheringFactor,
                    _CharacterDitheringPixelSize);
                // return float4(_HeadCenter.xyz, 1);
                /////////////////////////////////////    贴图采样     ////////////////////////////////
                ///
                // sample the texture
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, i.uv.xy);
                
                float4 hairLineMask = SampleTextureWithCustomST(TEXTURE2D_ARGS(_HairLineTex, sampler_HairLineTex), i.uv.xy, _HairLine_ST);
                
                float3 baseColor = baseTex.rgb * _BaseColor.rgb;
                baseColor = pow(max(baseColor, 0.0), max(_BaseColorPower, 0.001));
                float hairLine = hairLineMask.r;
                baseColor = lerp(baseColor, _HairLineColor.rgb * baseColor, hairLine);

                // 描边+渐变mask
                float4 hairMask2 = SAMPLE_TEXTURE2D(_OutlineMask, sampler_OutlineMask, i.uv.xy);
                
                half hairRamp = hairMask2.g;
                half hairShadow = hairMask2.b;
                baseColor = baseColor* ( 1 + lerp(_RampDarkColor.rgb, _RampLightColor.rgb, hairRamp)) * 0.5;//混合ramp和basetex
                // return float4(baseColor, 1);
                // 参照终末地的球形高光遮罩+高光范围+AO+高光强度
                float4 hairMask = SAMPLE_TEXTURE2D(_HairMaskTex, sampler_HairMaskTex, i.uv.xy);
                half sphereMask = hairMask.r;//球形高光遮罩
                // return sphereMask;
                half anisoHLMask = hairMask.g;//高光范围
                half ao = hairMask.b;//AO
                
                ao = saturate(ao * _AOOffset);
                ao = saturate((ao - 0.5) * _AOContrast + 0.5);
                ao = pow(ao, _AOPower);

                // 也有用hairMask的ga当作反射率和粗糙度的
                half anisoHLIntensity = hairMask.a;//高光强度
                anisoHLIntensity *= lerp(_AnisoContrast.r, _AnisoContrast.g, anisoHLMask);//控制不同高光范围高光强度

                half anisoArea2Shadow = lerp(_anisoArea2Shadow,1,anisoHLMask);
                
                // return float4(diffuse, 1);
                // return float4(roughnessSquare.xxx, 1);

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
                float4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv.xy);
                float3 normalTSRaw = UnpackNormalFromTex(normalTex);
                float3 normalTS = UnpackNormalFromTex(normalTex, _NormalScale);
                float3 normalWS = TransformTangentToWorld(normalTS, float3x3(i.tangentWS, i.bitangentWS, i.normalWS));                
                normalWS = SafeNormalize(lerp( i.normalWS ,normalWS, _NeedNormalMap));
                // return float4(normalWS,1);
                

                // 几何数据
                float3 sphereNormalWS = SafeNormalize(i.sphereNormalWS);
                float3 diffDormalWS = lerp(normalWS, sphereNormalWS, _SphereNormalLerp);
                sphereNormalWS = lerp(normalWS, sphereNormalWS, sphereMask);

                // float3 lightDir = SafeNormalize(mainLight.direction);
                float3 viewDir = SafeNormalize(GetCameraPositionWS() - i.positionWS);
                float3 cameraForward = UNITY_MATRIX_V[2].xyz;
                cameraForward = normalize(cameraForward);
                viewDir = lerp(viewDir, cameraForward, _ForwardDirStrength);
                viewDir = normalize(viewDir);

                float3 halfDir = SafeNormalize(mainLightDir + viewDir);
                float NdotL = dot(diffDormalWS, mainLightDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float NdotV = saturate(dot(normalWS, viewDir)); 
                float HdotV = saturate(dot(halfDir, viewDir));
                float halfLambert = NdotL * 0.5 + 0.5;
                float2 screenUV = i.positionHCS.xy / _ScreenParams.xy;
                /////////////////////////////////////    PBR部分     ////////////////////////////////
                ///
                
                ////////////////////////////////////////    计算二阶明调暗调的albedo     ////////////////////////////////
                ///

                float3 albedoDarkColor = baseColor*_AlbedoDarkStrength ;
                float albedoDarkStrength = MyCalculateLightStrength(albedoDarkColor);
                albedoDarkColor = lerp(albedoDarkStrength.xxx, albedoDarkColor, _AlbedoDarkSaturation);

                // PBR的部分，暂时保留，但不参与计算
                float metallic = 0;
                float smoothness = 0;
                float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
                float roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);
                float roughnessSquare = max(roughness * roughness, HALF_MIN);
                float oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
                float3 diffuse = baseColor * oneMinusReflectivity;

                if (IsCharaShaderMaterialDebugEnabled())
                {
                    // 头发没有金属度/粗糙度贴图通道，原始与调整后均固定输出 0；原始 AO 直接读取 HairMask.B。
                    float3 debugRawMra = float3(0.0, 1.0, hairMask.b);
                    return GetCharaShaderDebugColor(
                        baseColor,
                        normalWS,
                        normalTSRaw,
                        normalTS,
                        0.0,
                        0.0,
                        debugRawMra,
                        i.smoothNormalVertexColor,
                        CHARA_HAIR_FRINGE_FORWARD_ALPHA);
                }

                /////////////////////////////////////    直接漫反射Diffuse     ////////////////////////////////
                ///
                // diffuse就是basecolor乘上能量分布，亮部basecolor不变，暗部可以手动控制
                float energyDistribution_metallic = 0.96 - 0.96 * metallic;
                float3 albedoLight = baseColor * energyDistribution_metallic;
                float3 albedoDark = albedoDarkColor * energyDistribution_metallic;

                // 场景阴影
                // 阴影数据
                float hairLambert = saturate(hairShadow * _HairShadowOffset + halfLambert);
                float lambertShadow = sigmoid(hairLambert, _LambertShadowCenter, _LambertShadowSmooth);
                float shadowAttenuation = 1;
                shadowAttenuation = ApplyPerObjectShadow(mainLight.shadowAttenuation, i.positionHCS, _UsePerObjectShadow);
                float shadowScene = 1;
                if (_UsePerObjectShadow > 0.5)
                {
                    shadowScene = lerp(1, shadowAttenuation, _ShadowStrength);
                    shadowScene = saturate(SigmoidSharp(shadowScene, _SceneShadowCenter, _SceneShadowSmooth));
                }
                
                shadowScene = saturate(shadowScene);
                // sceneShadow = sigmoid(sceneShadow, _SceneShadowCenter, _SceneShadowSmooth);
                // sceneShadow = lerp(_GlobalShadowBrightness, 1, sceneShadow);

                // 三阶暗调/暗中暗/暗部衰减及强度计算
                // 重新计算下兰伯特亮暗下diffuse的强度，暗部额外乘上了0.65的衰减
      
                float3 albedoDarkMore = albedoDark * 0.65 ; 
                float diffuseLightStrength = MyCalculateLightStrength(albedoLight);
                float diffuseDarkMoreStrength = MyCalculateLightStrength(albedoDarkMore);

                 /////////////////////////////////////    背光补偿     ////////////////////////////////
                ///
                // 计算LdotV视觉背光区域
                float2 cameraForward_xz = normalize(cameraForward.xz);
                float backLight = saturate(-dot(cameraForward_xz, mainLightDir_xz.xz));
                
                float backLight_y = saturate(-abs(cameraForward.y) + 0.75); 
                // backLight_y = MySmoothstep(backLight_y);
                backLight_y = smoothstep(0,1,backLight_y);
                // 背光区域根据视角进行上下的衰减
                backLight = backLight * backLight_y;
                
                
                // 背光补偿重新获得lambert阴影
                float fixNoL = 0.5 - 0.5 * NdotL * NdotL;// 在明暗交界线附近提亮最多，其余部分相应减少
                float NoL_ramp = fixNoL * backLight * _BackLightStrength + NdotL; 
                NoL_ramp = clamp(NoL_ramp, -1, 1);
                NoL_ramp = NoL_ramp * 0.5 + 0.5;
                // NoL_rampFinal = step(0.5 - hairShadow * _ShadowOffset, NoL_rampFinal);
                float NoL_ramp_Offset = smoothstep(0.5 + hairShadow * _HairShadowOffset - 0.02, 0.5 + hairShadow * _HairShadowOffset + 0.02, NoL_ramp);
                float NoL_rampFinal = lerp(NoL_ramp, NoL_ramp_Offset, _HairShadowLerp);
                // return NoL_rampFinal;
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
                float NoF = dot(normalWS, cameraForward);
                NoF = NoF * 0.5 + 0.5;
                float rampColor_NoF = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NoF, 0.5)).w; 
                rampColor_NoF *= _NoFStrength; 
                rampColor_NoF = pow(abs(rampColor_NoF), _NoFPow);

                /////////////////////////////////////    全光照下diffuse混合Start     ////////////////////////////////
                ///
                float aoShadow = ao * shadowScene;//ao在阴影里更暗
                float aoArea = ao;
      
                float sceneShadowArea = shadowScene;
                float totalShadowArea = min(aoArea, sceneShadowArea);
               
                // 这里用于混合前调+中间调
                totalShadowArea = min(totalShadowArea,remapNoL);
                
                // 这里用于混合中间调和后调
                // ao阴影区域乘上NdotF控制
                float aoShadowAreaNoF = aoShadow * rampColor_NoF * shadowScene;

                // diffuse对ao暗部进行了更暗的处理
                // 这么处理颜色会离灰度更远，更鲜明
                albedoDarkMore = lerp(diffuseDarkMoreStrength.xxx, albedoDarkMore, 1.2); 
                float3 mainDiffuseColor_Dark_lerp = lerp(albedoDarkMore, albedoDark , saturate(aoShadowAreaNoF + remapNoL));
                
           
                float3 diffuseBRDF = lerp(mainDiffuseColor_Dark_lerp, albedoLight, totalShadowArea);
                     
                // 色彩倾向越大，颜色影响越大
                float rampSaturationStrength = lerp(1, rampColorNoLStrength, _RampSaturationStrength);
                // rampSaturationStrength = rampColorNoLStrength;
                float3 rampColorNoLEffect = lerp(float3(1.0, 1.0, 1.0), rampColor_NoL.rgb, rampSaturationStrength);
                rampColorNoLEffect = lerp(float3(1.0, 1.0, 1.0), rampColorNoLEffect, _RampStrength);
                float3 diffuseBRDFRamp = diffuseBRDF * rampColorNoLEffect;
               
                float diffuseBRDFStrength = MyCalculateLightStrength(diffuseBRDF);
                float diffuseBRDFRampStrength = MyCalculateLightStrength(diffuseBRDFRamp);
                // 这里在做ramp亮度守恒补偿
                float rampColor_control = diffuseBRDFStrength / max(0.01, diffuseBRDFRampStrength);
                rampColor_control = clamp(rampColor_control, 0, 1.5);
                float3 finalDiffuseBRDF = diffuseBRDFRamp * rampColor_control;

                /////////////////////////////////////    顶光Start     ////////////////////////////////
                ///
                // 顶光
                // 由于模型旋转了90度，所以本地空间的up方向临时从(float3(0,1,0)改成了(float3(0,0,1)
                float3 topLightDir = float3(0,1,0);
                float topLightNoL = dot(topLightDir, normalWS);
                topLightNoL = saturate(topLightNoL + _OtherLightOffset);
                topLightNoL = topLightNoL * _OtherLightStrength + _OtherLightStrength_Offset; 
                
                float3 topLightDark = _OtherLightColor.rgb; 
                float3 otherLightColor = lerp(topLightDark, float3(1,1,1), totalShadowArea); 
                float3 finalTopLight = otherLightColor * topLightNoL;
                float3 topLightDay1 = saturate(finalTopLight);
                
                /////////////////////////////////////    直接漫反射Start     ////////////////////////////////
                ///  
                // 修正：加上阴影颜色
                float3 mainlightNew = lerp(_ShadowColor.rgb , mainLightColor, totalShadowArea) * mainLightIntensity;
                // 最主要的影响：无光照时只考虑otherLightResult_day0，有光照时直射光+otherLightResult_day1
                float3 finalMainlightDifficus = mainlightNew + topLightDay1; 
                

                float3 diffuseResult = finalMainlightDifficus * finalDiffuseBRDF;
                // return float4(diffuseResult,1);

                /////////////////////////////////// 各向异性高光       ////////////////////////////////////
                ///
                float3 fallbackHairLocalUpWS = SafeNormalize(float3(0,1,0));
                float useSyncedHeadUp = step(0.000001, dot(_HeadUp.xyz, _HeadUp.xyz));
                float3 hairLocalUpWS = SafeNormalize(lerp(fallbackHairLocalUpWS, _HeadUp.xyz, useSyncedHeadUp));
                hairLocalUpWS = _HeadUp.xyz;
                float3 radialTangentWS = SafeNormalize(cross(sphereNormalWS, hairLocalUpWS));
                
                float3 tangent = normalize(radialTangentWS);
                tangent = lerp(normalize(i.tangentWS), radialTangentWS, sphereMask);
                float3 bitangent = normalize(cross(sphereNormalWS, tangent));

                float4 spaceDebugColor = ResolveHairSpaceDebugColor(
                    viewDir,
                    SafeNormalize(i.sphereNormalWS),
                    sphereNormalWS,
                    tangent,
                    bitangent,
                    hairLocalUpWS,
                    sphereMask,
                    CHARA_HAIR_FRINGE_FORWARD_ALPHA);
                if (spaceDebugColor.a >= 0.0)
                {
                    return spaceDebugColor;
                }

                float3 anisoColor = 0;
                float anisoArea = 1;
                float3 doubleSpecularColor = 0;
                #ifdef _ANISOTROPIC_ON
                #ifdef _DOUBLE_SPECULAR_ON
                
                    doubleSpecularColor = CharaHairDoubleSpecular(
                        sphereNormalWS,
                        bitangent,
                        halfDir,
                        1,
                        totalShadowArea,
                        _HSpecularColor1.rgb,
                        _SpecStrength1,
                        _SpecGloss1,
                        _NormalShift1,
                        _HSpecularColor2.rgb,
                        _SpecStrength2,
                        _SpecGloss2,
                        _NormalShift2) * mainLightColor;
                    doubleSpecularColor *= smoothstep(0.8,0,dot(hairLocalUpWS,viewDir)) * smoothstep(0.8,0,dot(-hairLocalUpWS,viewDir));
                #endif
                    float shift = SampleTextureWithCustomST(TEXTURE2D_ARGS(_HairLineTex, sampler_HairLineTex), i.uv.xy, _Shift_ST).g - 0.5;
                    
                    float shiftNoise = lerp(_ShiftNoise2, _ShiftNoise, sphereMask);
                    float anisoOffset = lerp(_AnisoOffset2, _AnisoOffset, sphereMask);
                    float anisoCut = lerp(1, _AnisoCut, sphereMask);
                    float HNdotV = saturate(dot(sphereNormalWS, viewDir));
                    // 主渐变高光
                    CharaHairFringeAnisoRampSpec(
                        bitangent,
                        sphereNormalWS,
                        // lightDir,
                        viewDir,
                        hairLocalUpWS,
                        shift,
                        shiftNoise,
                        anisoOffset,
                        _AnisoLength,
                        anisoCut,
                        TEXTURE2D_ARGS(_SpecRampTex, sampler_SpecRampTex),
                        anisoColor,
                        anisoArea);
                        // return anisoArea;

                    // 参考zmd垫在底下那层
                    float anisoArea2 = 1;
                    CharaHairFringeAnisoRampSpec(
                        bitangent,
                        sphereNormalWS,
                        viewDir,
                        hairLocalUpWS,
                        shift,
                        0,
                        anisoOffset,
                        _AnisoLength * _AnisoAnotherLength,
                        _AnisoCut,
                        anisoArea2);   
                    // anisoArea2 = 0;
                    // anisoArea2 *= _AnisoAnotherStrength * _AnisoAnotherColor.r;
                    // return anisoArea + anisoArea2 * 0.1;
                    float anisoSecondArea = 0;
                    float3 anisoSecondColor = 0;
                    //发尾渐变高光
                    float3 sphereNormalWS2 = normalize(i.positionWS - (_HeadCenter.xyz + hairLocalUpWS * _AnisoSecondPosition));
                    sphereNormalWS = lerp(normalWS, sphereNormalWS2, sphereMask);
                    float3 radialTangentWS2 = cross(sphereNormalWS2, hairLocalUpWS);
                    float3 tangent2 = normalize(radialTangentWS2);
                    tangent2 = lerp(normalize(i.tangentWS), radialTangentWS2, sphereMask);
                    float3 bitangent2 = normalize(cross(sphereNormalWS2, tangent2));
                    CharaHairFringeAnisoRampSpec(
                        bitangent2,
                        sphereNormalWS2,
                        // lightDir,
                        viewDir,
                        hairLocalUpWS,
                        shift,
                        shiftNoise,
                        _AnisoSecondOffset,
                        _AnisoSecondLength,
                        _AnisoCut,
                        TEXTURE2D_ARGS(_SpecRampTex, sampler_SpecRampTex),
                        anisoSecondColor,
                        anisoSecondArea);
                    anisoSecondArea *= sphereMask;

                    // 参考zmd垫在底下那层，因为一些原因暂时弃用
                    float anisoSecondArea2 = 1;
                    CharaHairFringeAnisoRampSpec(
                        bitangent,
                        sphereNormalWS,
                        viewDir,
                        hairLocalUpWS,
                        shift,
                        0,
                        anisoOffset,
                        _AnisoLength * _AnisoAnotherLength,
                        _AnisoCut,
                        anisoSecondArea2);
                    anisoSecondArea2 *= sphereMask;
                    anisoSecondArea2 *= _AnisoAnotherStrength * _AnisoAnotherColor.r;
                    // anisoArea2 = 0;
                    // anisoSecondArea2 = 0;
                    
                    
                    // anisoArea *= shadow;
                    anisoColor =  (anisoColor * saturate(anisoArea) * anisoHLIntensity + _AnisoAnotherColor.rgb * anisoArea2 * anisoArea2Shadow * _AnisoAnotherStrength) * _AnisoStrength;
                    anisoColor += (saturate(anisoSecondArea) * anisoSecondColor + _AnisoAnotherColor.rgb * anisoSecondArea2 * anisoArea2Shadow) * _AnisoSecondStrength;
                    anisoColor *= pow(HNdotV, _AnisoPower)  ;//改成sceneShadow在非兰伯特也有高光
                    // return anisoHLIntensity;
                    // anisoColor *= pow(NdotH, 5);
                    anisoColor *= smoothstep(0.8,0,dot(hairLocalUpWS,viewDir)) * smoothstep(0.8,0,dot(-hairLocalUpWS,viewDir));
                    
                    // return lambertLightArea;
                
                #endif
                
                float3 finalMainlightSpecular = aoShadow * mainLightColor * mainLightIntensity;
            
                // selfAOShadowEffect影响高光程度
                float shadowArea = totalShadowArea;
                float specularShadowStrength = CharaResolveSpecularShadowStrength(_SpecularShadowStrength);
                float selfAOShadowEffect = lerp(1 - specularShadowStrength, 1, shadowArea);

                float3 specularLight = finalMainlightSpecular  * selfAOShadowEffect;
                
                               
                float3 F0 = F0_CONST * (1 - metallic) + baseColor * metallic;
                // float3 directDiffuse = diffuse * lambertRamp;
                float3 specularBRDF = anisoColor + doubleSpecularColor;
                
                float3 directLight = (diffuseResult + specularBRDF * specularLight);

                /////////////////////////////////////    间接漫反射Start     ////////////////////////////////
                ///
                // 间接漫反射体感和平面效果切换
                float3 indirectDiffuseplane = max(half3(0,0,0), half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w));
                float3 indirectDiffusevolume = SampleSH(normalWS);
                float3 indirectDiffuse = lerp(lerp(indirectDiffuseplane, indirectDiffusevolume, _IndirectDifficusPlaneVolume), 1.0.xxx, saturate(_GlobalCharacterRenderOverrideSceneAmbient)) * baseColor;

                // 材质级和场景级环境光参数分层相乘，便于每个场景统一调整角色间接漫反射。
                half3 indirectTint = _IndirectTintColor.rgb * _GlobalIndirectTintColor;
                indirectDiffuse *= _IndirectLightIntensity * _GlobalIndirecIntensity;
                indirectDiffuse *= indirectTint;
                
                //间接光照
                // float3 indirectDiffuse = IndirectDiffuse(normalWS, F0, roughness, NdotV, metallic, baseColor, occlusion);

                float3 indirectSpecular = 0;
                float3 indirectLight = (indirectDiffuse);

                // Hair / Fringe 的额外点光使用角色 V2 基础直接高光简化项，不复用头发各向异性、双层高光或 Rim 染色。
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
                    1.0.xxx,
                    addLightDiffuse,
                    addLightSpecular);
                float3 addLightResult = addLightDiffuse + addLightSpecular;
                float3 resultColor = directLight + indirectLight + addLightResult;
                resultColor += CharaGetGlobalVirtualBackLightResult(albedoLight, normalWS, mainLightDir);

                half4 lightingDebugColor;
                if (TryApplyCharaShaderLightingDebug(
                    lightingDebugColor,
                    diffuseResult + addLightDiffuse + CharaGetGlobalVirtualBackLightResult(albedoLight, normalWS, mainLightDir),
                    specularBRDF * specularLight + addLightSpecular,
                    indirectDiffuse,
                    indirectSpecular,
                    ao.xxx,
                    shadowAttenuation,
                    shadowScene,
                    CHARA_HAIR_FRINGE_FORWARD_ALPHA))
                {
                    return lightingDebugColor;
                }

                // return float4(indirectLight,1);
                
                // finalColor += topLightDay1;
                // return float4(topLightColor, 1);

                float3 fresnelColor = 1;
                #ifdef _FRESNEL_ON
                    fresnelColor = CalculateFresnel(
                        NdotV,
                        _FresnelPower,
                        _FresnelSmoothMin,
                        _FresnelSmoothMax,
                        _FresnelInnerColor.rgb,
                        _FresnelOuterColor.rgb);
                //return float4(fresnelColor,1);
                #endif
                resultColor *= fresnelColor;
                // return float4(fresnelColor,1);
                // return NdotV;

                #ifdef _HSV_ON
                    float luminance = dot(resultColor.rgb, float3(0.2126, 0.7152, 0.0722));
                    resultColor.rgb = lerp(float3(luminance, luminance, luminance), resultColor.rgb, _Saturation);
                #endif
                
                float3 rimColor = 0;
                float3 emissionColor = 0;
                                
                
                #ifdef _RIM_ON
                    float2 rimScreenUV = i.positionHCS.xy / _ScaledScreenParams.xy;
                    float3 rimNormalWS = lerp(normalize(i.normalWS), normalWS, _RimNormalBlend);
                    rimColor = CalculateRim(
                        rimScreenUV,
                        i.positionWS,
                        rimNormalWS,
                        viewDir,
                        mainLightDir,
                        shadowScene,
                        ao,
                        NdotV,
                        1.0,
                        albedoLight);
                // return float4(rimColor,1);
                #endif
                resultColor += rimColor;
                
                #ifdef _HSV_ON
                    resultColor *= _Brightness;
                #endif
                
                #if defined(_DEATH_ON)
                    float deathMask = SAMPLE_TEXTURE2D(_DeathMask, sampler_DeathMask, TransformDeathMaskUV(screenUV * _DeathMaskTiling.xy, _DeathMaskUVOffset, _DeathMaskUVRotation)).r;
            float3 deathMaskedColor = resultColor * lerp(_DeathInnerTint.rgb, _DeathOuterTint.rgb, deathMask);
                    resultColor = lerp(resultColor, deathMaskedColor, saturate(_DeathMaskBlendStrength));
                    resultColor = ApplyCharaFresnel(resultColor, normalWS, viewDir, _DeathFresnelScale, _DeathFresnelPower, _DeathFresnelColor);
                #endif

                resultColor = ApplyCharaRedTint(resultColor);
                resultColor = ApplyCharaFinalColorGradient(resultColor, i.gradientUV);
                // return float4(directLight,1);
                half finalAlpha = CHARA_HAIR_FRINGE_FORWARD_ALPHA;
                #if defined(CHARA_HAIR_FRINGE_OUTER_PASS)
                    half horizontalFade = 1.0h - saturate(dot(viewDir, SafeNormalize(_HeadForward.xyz)));
                    half verticalFade = abs(dot(viewDir, SafeNormalize(_HeadUp.xyz)));
                    half verticalAdjustedAlpha = saturate(finalAlpha * lerp(1.0h, 1.5h, verticalFade));
                    half sideAlpha = lerp(finalAlpha, 1.0h, saturate(_FringeOuterHorizontalAtten));
                    finalAlpha = lerp(verticalAdjustedAlpha, sideAlpha, horizontalFade);
                #endif
                finalAlpha = saturate(finalAlpha);

                // resultColor = doubleSpecularColor;
                return ApplyCharaShaderFinalDebug(
                    resultColor,
                    CharaFinalColorGradientDebugColor(i.gradientUV),
                    finalAlpha);
            }
            //#endregion
