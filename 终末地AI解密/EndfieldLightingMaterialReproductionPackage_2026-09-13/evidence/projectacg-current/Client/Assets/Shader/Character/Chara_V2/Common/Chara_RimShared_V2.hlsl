// Purpose: Shared rim-light evaluation for the standard Chara_V2 rim parameter family.
#ifndef CHARA_RIM_SHARED_INCLUDED
#define CHARA_RIM_SHARED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Chara_Globals_V2.hlsl"

#if defined(CHARA_RIM_USE_GLOBAL_CONTROLS)
#define CHARA_RIM_CUSTOM_DIRECTION_ENABLED _GlobalCharacterRenderRimCustomDirectionEnabled
#define CHARA_RIM_DIRECTION_SPACE _GlobalCharacterRenderRimDirectionSpace
#define CHARA_RIM_DIRECTION _GlobalCharacterRenderRimDirection
#define CHARA_RIM_LIGHT_ALIGN 1.0
#define CHARA_RIM_FAKE_POINT_MASK_ENABLED _GlobalCharacterRenderRimFakePointMaskEnabled
#define CHARA_RIM_FAKE_POINT_MASK_POSITION _GlobalCharacterRenderRimFakePointMaskPosition
#define CHARA_RIM_FAKE_POINT_MASK_RANGE _GlobalCharacterRenderRimFakePointMaskRange
#define CHARA_RIM_FAKE_POINT_MASK_POWER _GlobalCharacterRenderRimFakePointMaskPower
#define CHARA_RIM_FAKE_DIRECT_MASK_ENABLED _GlobalCharacterRenderRimFakeDirectMaskEnabled
#define CHARA_RIM_FAKE_DIRECT_MASK_DIRECTION _GlobalCharacterRenderRimFakeDirectMaskDirection
#define CHARA_RIM_FAKE_DIRECT_MASK_POSITION _GlobalCharacterRenderRimFakeDirectMaskPosition
#define CHARA_RIM_FAKE_DIRECT_MASK_RANGE _GlobalCharacterRenderRimFakeDirectMaskRange
#define CHARA_RIM_INTENSITY _GlobalCharacterRenderRimIntensity
#else
#define CHARA_RIM_CUSTOM_DIRECTION_ENABLED _CustomLightDirection
#define CHARA_RIM_DIRECTION_SPACE _CustomRimDirectionSpace
#define CHARA_RIM_DIRECTION _FresnelRimDirection.xyz
#define CHARA_RIM_LIGHT_ALIGN _FresnelRimLightAlign
#define CHARA_RIM_FAKE_POINT_MASK_ENABLED _UseRimFakePointMask
#define CHARA_RIM_FAKE_POINT_MASK_POSITION _RimFakePointMaskPosition.xyz
#define CHARA_RIM_FAKE_POINT_MASK_RANGE _RimFakePointMaskRange
#define CHARA_RIM_FAKE_POINT_MASK_POWER _RimFakePointMaskPower
#define CHARA_RIM_FAKE_DIRECT_MASK_ENABLED _UseRimFakeDirectMask
#define CHARA_RIM_FAKE_DIRECT_MASK_DIRECTION _RimFakeDirectMaskDirection.xyz
#define CHARA_RIM_FAKE_DIRECT_MASK_POSITION _RimFakeDirectMaskPosition.xyz
#define CHARA_RIM_FAKE_DIRECT_MASK_RANGE _RimFakeDirectMaskRange
#define CHARA_RIM_INTENSITY 1.0
#endif

// 标准角色边缘光公共入口。
// 目标是让其他 Shader 只 include 这一个文件，并直接传入逐像素数据，不再额外组 struct。
// 当前文件只负责“标准 Rim 参数族”的通用计算，不处理材质属性声明和面板逻辑。

// Fresnel / Depth Rim 共用的前后区间翻转函数。
// `control` 负责把边缘光偏向受光侧或背光侧，`direction` 用来区分前后两套遮罩。
float FlipZone(float range, float control, float direction)
{
    float remap = control * 0.5 * direction + 0.5;
    float frontAlign = lerp(1.0, range, remap);
    float frontInverseAlign = lerp(1.0, 1.0 - range, 1.0 - remap);
    return frontAlign * frontInverseAlign;
}

// 统一把美术输入的方向解析到世界空间。
// 当前约定：`directionSpace < 0.5` 视为 View 空间，其余按 World 空间直接使用。
float3 ResolveCustomDirection(float3 direction, float directionSpace)
{
    float3 safeDirection = dot(direction, direction) > 1.0e-6 ? normalize(direction) : float3(0.0, 0.0, 1.0);

    if (directionSpace < 0.5)
    {
        return TransformViewToWorldDir(safeDirection, true);
    }

    return safeDirection;
}

// 标准 Rim 颜色解析。
// 这里沿用当前角色 Shader 的约定：`rgb` 为颜色，`a` 直接作为整体亮度乘子。
float3 ResolveTint(float4 rimColor)
{
    return rimColor.rgb * rimColor.a;
}

// 场景阴影参与度控制。
// `rimShadowStrength = 0` 时忽略场景阴影，`1` 时完全按场景阴影衰减。
float ResolveShadowFactor(float shadowScene, float rimShadowStrength)
{
    return lerp(1.0, saturate(shadowScene), saturate(rimShadowStrength));
}

float ResolveDepthLightResponse(float3 normalVS, float3 lightDirectionWS)
{
    float baseAxis = saturate(normalVS.x * 0.5 + 0.5);

    if (CHARA_RIM_CUSTOM_DIRECTION_ENABLED <= 0.5)
    {
        return baseAxis;
    }

    float3 lightDirectionVS = TransformWorldToViewDir(lightDirectionWS);

    if (CHARA_RIM_DIRECTION_SPACE > 0.5)
    {
        const float worldSpaceFlipWidth = 0.15;
        float horizontalFlip = smoothstep(-worldSpaceFlipWidth, worldSpaceFlipWidth, lightDirectionVS.x);
        return lerp(1.0 - baseAxis, baseAxis, horizontalFlip);
    }

    float2 lightScreenDir = lightDirectionVS.xy;
    float lightScreenDirLength = length(lightScreenDir);

    if (lightScreenDirLength <= 1.0e-6)
    {
        return baseAxis;
    }

    float2 lightScreenAxis = lightScreenDir / lightScreenDirLength;
    float lightAxis = saturate(dot(normalVS.xy, lightScreenAxis) * 0.5 + 0.5);
    float lightAxisInfluence = saturate(lightScreenDirLength);
    return lerp(baseAxis, lightAxis, lightAxisInfluence);
}

// 对阈值型 Rim 遮罩做解析型抗锯齿：
// - 仍然沿用原有的最小/最大阈值语义；
// - 只在屏幕空间导数较大时自动扩边，压住远景和运动中的锯齿闪烁；
// - 不新增材质参数，也不改变已有美术调参含义。
float AnalyticRimAA(float value, float minEdge, float maxEdge)
{
    float aaWidth = max(fwidth(value), 1.0e-4);
    return smoothstep(minEdge - aaWidth, maxEdge + aaWidth, value);
}

// 把世界空间方向投影到屏幕平面，用于固定屏幕方向偏移版 Depth Rim 选择前后两侧各自的偏移方向。
// Fresnel Rim 核心遮罩。
// 这里专门使用传入的 `normalWS` / `NdotV` 来决定边缘形状，便于不同 Shader 自己选择结构法线还是像素法线。
float FresnelMask(
    float3 normalWS,
    float NdotL,
    float NdotV,
    float2 rimSettings,
    float rimLightAlign)
{
    float halfLambert = saturate(NdotL * 0.5 + 0.5);

    float rimFrontRaw = saturate((1.0 - NdotV) * FlipZone(halfLambert, rimLightAlign, 1.0));
    float frontThreshold = 1.0 - rimSettings.x;
    // 保持现有效果语义：当前标准角色 Rim 共享链路仍然只输出 back rim。
    // 这里不顺手恢复 front rim，避免改变已有材质观感。
    float frontRim = 0.0;

    float rimBackRaw = saturate((1.0 - NdotV) * FlipZone(halfLambert, rimLightAlign, -1.0));
    float backThreshold = 1.0 - rimSettings.x;
    float backRim = AnalyticRimAA(
        rimBackRaw,
        backThreshold,
        backThreshold + rimSettings.y);

    return frontRim + backRim;
}

// Depth Rim 核心遮罩。
// 通过屏幕空间深度差估算轮廓区域；`rimSettings` 只用于控制深度轮廓软硬。
// 当前这套共享路径里，Depth Rim 不再额外依赖旧版 front/back 开关。
// `_DepthRimWidthX / _DepthRimWidthY` 的正负直接决定固定屏幕方向偏移的采样方向，
// 不再额外把反方向轮廓并回结果里，避免调某一侧时另一侧也跟着出残留。
float DepthMask(
    float3 normalWS,
    float3 lightDirectionWS,
    float2 screenUV,
    float2 depthRimWidth,
    float2 rimSettings,
    float rimLightAlign)
{
    float2 scaledRimSettings = rimSettings * 0.05;
    float3 normalVS = normalize(TransformWorldToViewDir(normalWS));
    float2 offsetUV = screenUV + normalVS.xy * depthRimWidth * 0.01;
    float screenDepth = SampleSceneDepth(screenUV);
    float offsetDepth = SampleSceneDepth(offsetUV);
    float eye01ScreenDepth = LinearEyeDepth(screenDepth, _ZBufferParams);
    float eye01OffsetDepth = LinearEyeDepth(offsetDepth, _ZBufferParams);
    float deltaDepth = eye01OffsetDepth - eye01ScreenDepth;
    float rimArea = AnalyticRimAA(
        deltaDepth,
        scaledRimSettings.x - scaledRimSettings.y,
        scaledRimSettings.x + scaledRimSettings.y);
    float depthLightAxis = ResolveDepthLightResponse(normalVS, lightDirectionWS);
    float depthMask = rimArea * smoothstep(0.1, 0.8, FlipZone(depthLightAxis, rimLightAlign, -1.0));
    return depthMask;
}

// Rim 类型混合。
// 当前保持和 `Chara_PBRNew` 一致：Fresnel / Depth / Add 三种模式，Add 直接相加兼容旧行为。
float3 ResolveModeAddCompatible(float3 fresnelColor, float3 depthColor, float rimType)
{
    if (rimType < 0.5)
    {
        return fresnelColor;
    }

    if (rimType < 1.5)
    {
        return depthColor;
    }

    return fresnelColor + depthColor;
}

float3 ResolveModeLegacyCompatible(float3 fresnelColor, float3 depthColor, float rimType)
{
    if (rimType < 0.5)
    {
        return fresnelColor;
    }

    if (rimType < 1.5)
    {
        return depthColor;
    }

    if (rimType < 2.5)
    {
        return fresnelColor + depthColor;
    }

    return fresnelColor * depthColor;
}

float3 ResolveRimMaskCenterPositionWS()
{
#if defined(CHARA_RIM_MASK_USE_ROOT_POSITION)
    // SetHairFaceCenterSync 按 Prefab 实例写入可见 Renderer 中心，避免切面绕脚底 Root 旋转。
    // 属性名保留 RootPosition 以兼容现有材质和 MPB 协议。
    return _RimMaskRootPosition.w > 0.5
        ? _RimMaskRootPosition.xyz
        : TransformObjectToWorld(float3(0.0, 0.0, 0.0));
#else
    return TransformObjectToWorld(float3(0.0, 0.0, 0.0));
#endif
}

// 将像素相对角色 Root 的向量解析到遮罩参数所在空间。
// View 模式直接使用当前渲染 Camera 的 View Matrix，Camera Roll、相机栈和自定义 View Matrix
// 都会与角色实际渲染保持一致；World 模式保留世界空间向量。
float3 ResolveRimMaskRelativePosition(
    float3 positionWS,
    float3 rootPositionWS,
    float directionSpace)
{
    float3 relativePositionWS = positionWS - rootPositionWS;
    return directionSpace < 0.5
        ? TransformWorldToViewDir(relativePositionWS)
        : relativePositionWS;
}

float3 ResolveRimMaskDirection(float3 direction)
{
    return dot(direction, direction) > 1.0e-6
        ? normalize(direction)
        : float3(0.0, 0.0, 1.0);
}

// 假点灯遮罩。World 模式在世界空间计算，View 模式随相机坐标轴变化。
float FakePointMask(
    float3 positionWS,
    float3 pointOffset,
    float pointRange,
    float pointPower,
    float directionSpace)
{
    float3 relativePosition = ResolveRimMaskRelativePosition(
        positionWS,
        ResolveRimMaskCenterPositionWS(),
        directionSpace);
    float fakePointMaskDistance = distance(relativePosition, pointOffset);
    float fakePointMaskAtten = saturate(1.0 - fakePointMaskDistance / max(pointRange, 1.0e-4));
    // Power controls the radial falloff: 2 preserves the historical quadratic
    // attenuation, lower values make the edge softer/wider, higher values make
    // it harder/more concentrated near the mask center.
    return pow(fakePointMaskAtten, max(pointPower, 0.25));
}

// 假直接光切面遮罩。
// 方向决定保留哪一侧，位置决定切面经过哪里，`transitionRange` 控制切面过渡宽度。
float FakeDirectMask(
    float3 positionWS,
    float3 direction,
    float3 positionOffset,
    float transitionRange,
    float directionSpace)
{
    float3 fakeDirectMaskDirection = ResolveRimMaskDirection(direction);

    if (dot(fakeDirectMaskDirection, fakeDirectMaskDirection) <= 1.0e-6)
    {
        return 1.0;
    }

    float3 relativePosition = ResolveRimMaskRelativePosition(
        positionWS,
        ResolveRimMaskCenterPositionWS(),
        directionSpace);
    float fakeDirectSignedDistance = dot(
        relativePosition - positionOffset,
        fakeDirectMaskDirection);
    float fakeDirectTransitionRange = max(transitionRange, 1.0e-4);
    return smoothstep(-fakeDirectTransitionRange, fakeDirectTransitionRange, fakeDirectSignedDistance);
}

// 标准属性版入口：直接使用当前角色 Shader 体系已有的 Rim 参数名。
// Chara_V2 当前统一只保留这一套 Cloth/Skin 风格的 Rim 逻辑。
// 对外只要求传入逐像素上下文，内部统一完成：
// 1. 自定义方向解析
// 2. Fresnel / Depth Rim 计算
// 3. 与底色、AO、场景阴影融合
// 4. 假点灯与假直接光遮罩收口
// 5. 预留外部 float mask 接口，方便其他 Shader 直接把贴图通道或自定义遮罩乘在最终 Rim 结果后面
float3 CalculateRim(
    float2 screenUV,
    float3 positionWS,
    float3 normalWS,
    float3 viewDir,
    float3 mainLightDir,
    float shadowScene,
    float ao,
    float NdotV,
    float rimExternalMask,
    float3 albedoLight)
{
#if defined(_RIM_ON)
    float3 customLightDir = mainLightDir;
    if (CHARA_RIM_CUSTOM_DIRECTION_ENABLED > 0.5)
    {
        customLightDir = ResolveCustomDirection(CHARA_RIM_DIRECTION, CHARA_RIM_DIRECTION_SPACE);
    }

    float rimShadowFactor = ResolveShadowFactor(shadowScene, _RimShadowStrength);
    float3 rimTint = ResolveTint(_FresnelRimColor);
    float2 rimSettings = float2(_FresnelRimPower, _FresnelRimSmooth);
    float2 depthRimWidth = float2(_DepthRimWidthX, _DepthRimWidthY);

    float depthMask = DepthMask(
        normalWS,
        customLightDir,
        screenUV,
        depthRimWidth,
        rimSettings,
        CHARA_RIM_LIGHT_ALIGN);
    float3 depthColor = rimTint * depthMask * _DepthRimIntensity ;
    
    float fresnelNdotL = dot(customLightDir, normalWS);
    float fresnelShapeNdotV = saturate(dot(viewDir, normalWS));
    float fresnelMask = FresnelMask(
        normalWS,
        fresnelNdotL,
        fresnelShapeNdotV,
        rimSettings,
        CHARA_RIM_LIGHT_ALIGN);
    float3 fresnelColor = rimTint * fresnelMask * _FresnelIntensity  ;

    float3 rimColor = ResolveModeAddCompatible(fresnelColor, depthColor, _RimType);
    float rimShadowMask = min(ao, rimShadowFactor);
    float3 rimLightBrdf = ((albedoLight - 0.25) * _RimLightDiffuseColorEffect + 0.25);
    float rimMaskDirectionSpace = 1.0;
#if defined(CHARA_RIM_MASK_USE_ROOT_POSITION)
    if (CHARA_RIM_CUSTOM_DIRECTION_ENABLED > 0.5)
    {
        rimMaskDirectionSpace = CHARA_RIM_DIRECTION_SPACE;
    }
#endif

    float rimFakePointMask = 1.0;
    if (CHARA_RIM_FAKE_POINT_MASK_ENABLED > 0.5)
    {
        rimFakePointMask = FakePointMask(
            positionWS,
            CHARA_RIM_FAKE_POINT_MASK_POSITION,
            CHARA_RIM_FAKE_POINT_MASK_RANGE,
            CHARA_RIM_FAKE_POINT_MASK_POWER,
            rimMaskDirectionSpace);
    }

    float rimFakeDirectMask = 1.0;
    if (CHARA_RIM_FAKE_DIRECT_MASK_ENABLED > 0.5)
    {
        rimFakeDirectMask = FakeDirectMask(
            positionWS,
            CHARA_RIM_FAKE_DIRECT_MASK_DIRECTION,
            CHARA_RIM_FAKE_DIRECT_MASK_POSITION,
            CHARA_RIM_FAKE_DIRECT_MASK_RANGE,
            rimMaskDirectionSpace);
    }

    return rimColor * rimLightBrdf * rimShadowMask * rimFakePointMask * rimFakeDirectMask
        * rimExternalMask * saturate(_GlobalCharacterRenderRimLightToggle) * max(CHARA_RIM_INTENSITY, 0.0);
#else
    return float3(0.0, 0.0, 0.0);
#endif
}

#endif
