// Purpose: Chara_V2 公共透视畸变修正模块，可由不同角色 Shader 的顶点阶段 include 后调用。
#ifndef CHARA_FOV_ADJUSTMENT_V2_INCLUDED
#define CHARA_FOV_ADJUSTMENT_V2_INCLUDED

// 使用要求：调用方需要先 include URP Core.hlsl，以提供空间变换矩阵和 Transform* 系列函数。
// 本文件不声明材质属性、CBUFFER 或 Keyword，调用方显式传入角色中心与修正强度。
//
// 真实机制：
// ASP 的 “Set FOV Adjust On All” 只负责把 _FOVShiftX 批量写入材质；
// Shader 顶点阶段以角色中心为基准，压缩每个顶点在 View Space Z 方向上的深度差，
// 从而减弱近大远小造成的角色透视畸变。它不会修改相机 FOV，也不是沿屏幕 X 方向平移。
//
// 参数约定：
// 1. fovAdjustment 与 ASP 面板一致，限制在 0~1。
// 2. 0 保持原始顶点位置；1 把相对角色中心的 View Z 深度差压缩为原来的一半。
// 3. characterCenterWS 必须是整个角色统一的世界空间中心；多 Renderer 使用各自原点会产生接缝。
// 4. 该变形只调整顶点位置，不修改法线和切线，保持 ASP 原始光照行为。
// 5. 同一角色的 Forward、Outline、Depth、Shadow 等 Pass 必须使用相同修正，避免轮廓、深度和阴影错位。
// 6. 正交相机没有近大远小问题，应保持 fovAdjustment = 0，避免无意义地改变顶点深度。

float GetCharacterFOVAdjustmentDepthScale(float fovAdjustment)
{
    return rcp(1.0 + saturate(fovAdjustment));
}

// 核心 View Space 入口，适合调用方已经持有 positionVS / characterCenterVS 的情况。
float3 GetCharacterFOVAdjustedPositionVSFromVS(
    float3 positionVS,
    float3 characterCenterVS,
    float fovAdjustment)
{
    float depthScale = GetCharacterFOVAdjustmentDepthScale(fovAdjustment);
    positionVS.z = (positionVS.z - characterCenterVS.z) * depthScale + characterCenterVS.z;
    return positionVS;
}

// Object Space -> View Space。只进行一次 ModelView 变换，供后续 WS/OS/HClip 入口复用。
float3 GetCharacterFOVAdjustedPositionVS(
    float3 positionOS,
    float3 characterCenterWS,
    float fovAdjustment)
{
    float3 positionVS = mul(UNITY_MATRIX_MV, float4(positionOS, 1.0)).xyz;
    float3 characterCenterVS = TransformWorldToView(characterCenterWS);
    return GetCharacterFOVAdjustedPositionVSFromVS(
        positionVS,
        characterCenterVS,
        fovAdjustment);
}

// 推荐的最终裁剪空间入口。
// 顶点函数如果只需要输出 SV_POSITION，直接调用它可以避免像 ASP 原版那样先求逆矩阵回到 OS，
// 再执行一次 Object -> HClip 变换，是当前模块成本最低、最不容易接错的路径。
float4 GetCharacterFOVAdjustedPositionHClip(
    float3 positionOS,
    float3 characterCenterWS,
    float fovAdjustment)
{
    float3 adjustedPositionVS = GetCharacterFOVAdjustedPositionVS(
        positionOS,
        characterCenterWS,
        fovAdjustment);
    return TransformWViewToHClip(adjustedPositionVS);
}

// World Space 入口，适合顶点函数后续还要基于修正后位置计算世界空间数据。
float3 GetCharacterFOVAdjustedPositionWS(
    float3 positionWS,
    float3 characterCenterWS,
    float fovAdjustment)
{
    float3 positionVS = TransformWorldToView(positionWS);
    float3 characterCenterVS = TransformWorldToView(characterCenterWS);
    float3 adjustedPositionVS = GetCharacterFOVAdjustedPositionVSFromVS(
        positionVS,
        characterCenterVS,
        fovAdjustment);
    return TransformViewToWorld(adjustedPositionVS);
}

// Object Space 兼容入口，对应 ASP 的 GetFOVAdjustedPositionOS。
// 只有现有顶点流程必须继续把 positionOS 传给 GetVertexPositionInputs 时才使用；
// 新接入优先使用上面的 HClip 或 VS 入口，避免额外的空间往返。
float3 GetCharacterFOVAdjustedPositionOS(
    float3 positionOS,
    float3 characterCenterWS,
    float fovAdjustment)
{
    float3 adjustedPositionVS = GetCharacterFOVAdjustedPositionVS(
        positionOS,
        characterCenterWS,
        fovAdjustment);
    float3 adjustedPositionWS = TransformViewToWorld(adjustedPositionVS);
    return TransformWorldToObject(adjustedPositionWS);
}

// 无角色中心控制脚本时的便捷入口：使用当前 Renderer 的 Object Origin 作为中心。
// 只适合单 Renderer 或所有子网格共享同一 Object Origin 的模型；多 Renderer 角色应传统一中心。
float3 GetCharacterFOVAdjustmentObjectOriginWS()
{
    return TransformObjectToWorld(float3(0.0, 0.0, 0.0));
}

float4 GetCharacterFOVAdjustedPositionHClipFromObjectOrigin(
    float3 positionOS,
    float fovAdjustment)
{
    return GetCharacterFOVAdjustedPositionHClip(
        positionOS,
        GetCharacterFOVAdjustmentObjectOriginWS(),
        fovAdjustment);
}

#endif
