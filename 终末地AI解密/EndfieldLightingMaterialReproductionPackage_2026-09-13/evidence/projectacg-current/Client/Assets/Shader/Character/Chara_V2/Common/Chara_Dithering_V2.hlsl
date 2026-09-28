// Purpose: Chara_V2 公共屏幕空间抖动裁剪模块，可由任意 Shader Pass include 后调用。
#ifndef CHARA_DITHERING_V2_INCLUDED
#define CHARA_DITHERING_V2_INCLUDED

// 公共约定：
// 1. pixelPosition 传入片元阶段 SV_POSITION.xy，单位为屏幕像素，不要再次除以 w。
// 2. ditheringFactor = 0 时完全显示，= 1 时完全裁剪。
// 3. ditherPixelSize 控制微观图案的像素尺寸，内部至少按 1 像素处理。
// 4. 每种图案都是独立函数；后续 Shader 只调用选中的函数，不需要运行时模式分支。

// 把任意 0~1 图案值压入开区间，确保 Factor=0 时全部保留、Factor=1 时全部裁剪。
float EncodeCharacterDitheringThreshold(float threshold01)
{
    return lerp(1.0 / 17.0, 16.0 / 17.0, saturate(threshold01));
}

float EvaluateCharacterDitheringThreshold(float ditheringFactor, float threshold)
{
    return (1.0 - saturate(ditheringFactor)) - threshold;
}

void ApplyCharacterDitheringThreshold(float ditheringFactor, float threshold)
{
    clip(EvaluateCharacterDitheringThreshold(ditheringFactor, threshold));
}

float GetSafeCharacterDitheringSize(float ditherPixelSize)
{
    return clamp(ditherPixelSize, 1.0, 10.0);
}

// 返回当前微观格内的局部坐标，范围为 -1~1，格子中心为 0。
float2 GetCharacterDitheringCellPosition(float2 pixelPosition, float ditherPixelSize)
{
    float safePixelSize = GetSafeCharacterDitheringSize(ditherPixelSize);
    return frac(pixelPosition / safePixelSize) * 2.0 - 1.0;
}

// 一维 Hash，用于生成无纹理随机阈值。
float Hash12CharacterDithering(float2 value, float seed)
{
    float3 hashValue = frac(float3(value.xyx) * 0.1031 + seed * 0.013);
    hashValue += dot(hashValue, hashValue.yzx + 33.33);
    return frac((hashValue.x + hashValue.y) * hashValue.z);
}

// 二维 Hash，仅供 Voronoi 单元生成随机特征点。
float2 Hash22CharacterDithering(float2 value, float seed)
{
    float3 hashValue = frac(float3(value.xyx) * float3(0.1031, 0.1030, 0.0973) + seed * 0.013);
    hashValue += dot(hashValue, hashValue.yzx + 33.33);
    return frac((hashValue.xx + hashValue.yz) * hashValue.zy);
}

// ============================== 阈值图案 ==============================

// ASP 原版 4x4 Bayer：规则、稳定、无纹理采样，适合作为默认实现。
float GetCharacterDitheringBayer4x4Threshold(float2 pixelPosition, float ditherPixelSize)
{
    float2 ditherUV = pixelPosition / GetSafeCharacterDitheringSize(ditherPixelSize);
    uint2 ditherCell = (uint2)floor(ditherUV) & 3u;
    uint ditherIndex = ditherCell.x * 4u + ditherCell.y;

    const float ditherThresholds[16] =
    {
         1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
         4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };

    return ditherThresholds[ditherIndex];
}

// 棋盘格：只有两组阈值，变化节奏清晰，但中间覆盖率档位较少。
float GetCharacterDitheringCheckerThreshold(float2 pixelPosition, float ditherPixelSize)
{
    float safePixelSize = GetSafeCharacterDitheringSize(ditherPixelSize);
    float2 cellIndex = floor(pixelPosition / safePixelSize);
    float checker = frac((cellIndex.x + cellIndex.y) * 0.5) * 2.0;
    return EncodeCharacterDitheringThreshold(lerp(0.25, 0.75, checker));
}

// 方形收缩：Factor 增大时，每个方块从四周向中心收缩。
float GetCharacterDitheringSquareThreshold(float2 pixelPosition, float ditherPixelSize)
{
    float2 cellPosition = abs(GetCharacterDitheringCellPosition(pixelPosition, ditherPixelSize));
    return EncodeCharacterDitheringThreshold(max(cellPosition.x, cellPosition.y));
}

// 圆点网屏：Factor 增大时，圆点从外向内缩小，适合漫画或印刷网点风格。
float GetCharacterDitheringDotThreshold(float2 pixelPosition, float ditherPixelSize)
{
    float2 cellPosition = GetCharacterDitheringCellPosition(pixelPosition, ditherPixelSize);
    float normalizedDistance = length(cellPosition) * 0.70710678;
    return EncodeCharacterDitheringThreshold(normalizedDistance);
}

// 菱形收缩：使用曼哈顿距离，比圆点更便宜，形状也更锐利。
float GetCharacterDitheringDiamondThreshold(float2 pixelPosition, float ditherPixelSize)
{
    float2 cellPosition = abs(GetCharacterDitheringCellPosition(pixelPosition, ditherPixelSize));
    float normalizedDistance = (cellPosition.x + cellPosition.y) * 0.5;
    return EncodeCharacterDitheringThreshold(normalizedDistance);
}

// 十字收缩：越靠近格子水平轴或垂直轴，阈值越低，最后保留细十字。
float GetCharacterDitheringCrossThreshold(float2 pixelPosition, float ditherPixelSize)
{
    float2 cellPosition = abs(GetCharacterDitheringCellPosition(pixelPosition, ditherPixelSize));
    return EncodeCharacterDitheringThreshold(min(cellPosition.x, cellPosition.y));
}

// 方向条纹：stripeDirection 控制屏幕方向，stripePhase 用于整体平移图案。
float GetCharacterDitheringStripeThreshold(
    float2 pixelPosition,
    float ditherPixelSize,
    float2 stripeDirection,
    float stripePhase)
{
    float directionLengthSq = dot(stripeDirection, stripeDirection);
    float2 safeDirection = directionLengthSq > 1.0e-6
        ? stripeDirection * rsqrt(directionLengthSq)
        : float2(1.0, 0.0);
    float stripePosition = dot(
        pixelPosition / GetSafeCharacterDitheringSize(ditherPixelSize),
        safeDirection) + stripePhase;
    return EncodeCharacterDitheringThreshold(frac(stripePosition));
}

// IGN：无纹理、指令较少、规则网格感弱，适合移动端通用随机抖动。
float GetCharacterDitheringIGNThreshold(float2 pixelPosition, float ditherPixelSize)
{
    float2 noiseCell = floor(pixelPosition / GetSafeCharacterDitheringSize(ditherPixelSize));
    float noise = frac(52.9829189 * frac(dot(noiseCell, float2(0.06711056, 0.00583715))));
    return EncodeCharacterDitheringThreshold(noise);
}

// Hash Noise：可通过 seed 生成不同随机排列；比 IGN 稍贵，但方向性更弱。
float GetCharacterDitheringHashThreshold(
    float2 pixelPosition,
    float ditherPixelSize,
    float seed)
{
    float2 noiseCell = floor(pixelPosition / GetSafeCharacterDitheringSize(ditherPixelSize));
    return EncodeCharacterDitheringThreshold(Hash12CharacterDithering(noiseCell, seed));
}

// 六边形收缩：使用交错六边形网格，适合科技、护盾或晶体风格。
float GetCharacterDitheringHexagonThreshold(float2 pixelPosition, float ditherPixelSize)
{
    float2 gridPosition = pixelPosition / GetSafeCharacterDitheringSize(ditherPixelSize);
    const float2 hexScale = float2(1.0, 1.73205081);
    float2 halfHexScale = hexScale * 0.5;

    float2 cellA = gridPosition - hexScale * floor(gridPosition / hexScale) - halfHexScale;
    float2 shiftedPosition = gridPosition - halfHexScale;
    float2 cellB = shiftedPosition - hexScale * floor(shiftedPosition / hexScale) - halfHexScale;
    float2 cellPosition = dot(cellA, cellA) < dot(cellB, cellB) ? cellA : cellB;

    float2 absCellPosition = abs(cellPosition);
    float hexagonDistance = max(
        absCellPosition.x * 0.86602540 + absCellPosition.y * 0.5,
        absCellPosition.y);
    return EncodeCharacterDitheringThreshold(saturate(hexagonDistance * 2.0));
}

// Voronoi：形成不规则细胞状颗粒。每像素计算 3x3 邻域，成本明显高于其他模式。
// 推荐只用于少量特殊效果，或后续改为离线生成纹理再采样。
float GetCharacterDitheringVoronoiThreshold(
    float2 pixelPosition,
    float ditherPixelSize,
    float seed)
{
    float2 gridPosition = pixelPosition / GetSafeCharacterDitheringSize(ditherPixelSize);
    float2 baseCell = floor(gridPosition);
    float2 localPosition = frac(gridPosition);
    float nearestDistanceSq = 8.0;

    [unroll]
    for (int y = -1; y <= 1; ++y)
    {
        [unroll]
        for (int x = -1; x <= 1; ++x)
        {
            float2 neighborCell = float2(x, y);
            float2 featurePoint = neighborCell + Hash22CharacterDithering(baseCell + neighborCell, seed);
            float2 toFeaturePoint = featurePoint - localPosition;
            nearestDistanceSq = min(nearestDistanceSq, dot(toFeaturePoint, toFeaturePoint));
        }
    }

    float normalizedDistance = saturate(sqrt(nearestDistanceSq) * 0.70710678);
    return EncodeCharacterDitheringThreshold(normalizedDistance);
}

// Blue Noise / 自定义纹理入口：本文件不声明纹理，调用方采样后把 R 通道传进来。
// 推荐纹理使用 Point + Repeat、关闭 Mipmap，并避免有损压缩破坏阈值分布。
float GetCharacterDitheringSampledThreshold(float sampledThreshold)
{
    return EncodeCharacterDitheringThreshold(sampledThreshold);
}

// ============================== 一行式裁剪入口 ==============================

void ApplyCharacterDitheringBayer4x4(float4 positionCS, float ditheringFactor, float ditherPixelSize)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringBayer4x4Threshold(positionCS.xy, ditherPixelSize));
}

void ApplyCharacterDitheringChecker(float4 positionCS, float ditheringFactor, float ditherPixelSize)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringCheckerThreshold(positionCS.xy, ditherPixelSize));
}

void ApplyCharacterDitheringSquare(float4 positionCS, float ditheringFactor, float ditherPixelSize)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringSquareThreshold(positionCS.xy, ditherPixelSize));
}

void ApplyCharacterDitheringDot(float4 positionCS, float ditheringFactor, float ditherPixelSize)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringDotThreshold(positionCS.xy, ditherPixelSize));
}

void ApplyCharacterDitheringDiamond(float4 positionCS, float ditheringFactor, float ditherPixelSize)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringDiamondThreshold(positionCS.xy, ditherPixelSize));
}

void ApplyCharacterDitheringCross(float4 positionCS, float ditheringFactor, float ditherPixelSize)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringCrossThreshold(positionCS.xy, ditherPixelSize));
}

void ApplyCharacterDitheringStripe(
    float4 positionCS,
    float ditheringFactor,
    float ditherPixelSize,
    float2 stripeDirection,
    float stripePhase)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringStripeThreshold(
            positionCS.xy,
            ditherPixelSize,
            stripeDirection,
            stripePhase));
}

void ApplyCharacterDitheringIGN(float4 positionCS, float ditheringFactor, float ditherPixelSize)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringIGNThreshold(positionCS.xy, ditherPixelSize));
}

void ApplyCharacterDitheringHash(
    float4 positionCS,
    float ditheringFactor,
    float ditherPixelSize,
    float seed)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringHashThreshold(positionCS.xy, ditherPixelSize, seed));
}

void ApplyCharacterDitheringHexagon(float4 positionCS, float ditheringFactor, float ditherPixelSize)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringHexagonThreshold(positionCS.xy, ditherPixelSize));
}

void ApplyCharacterDitheringVoronoi(
    float4 positionCS,
    float ditheringFactor,
    float ditherPixelSize,
    float seed)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringVoronoiThreshold(positionCS.xy, ditherPixelSize, seed));
}

void ApplyCharacterDitheringSampled(float ditheringFactor, float sampledThreshold)
{
    ApplyCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringSampledThreshold(sampledThreshold));
}

// 兼容原始公共接口：默认继续使用 ASP 4x4 Bayer，后续已有调用无需改名。
float EvaluateCharacterDithering(
    float2 pixelPosition,
    float ditheringFactor,
    float ditherPixelSize)
{
    return EvaluateCharacterDitheringThreshold(
        ditheringFactor,
        GetCharacterDitheringBayer4x4Threshold(pixelPosition, ditherPixelSize));
}

void ApplyCharacterDithering(
    float4 positionCS,
    float ditheringFactor,
    float ditherPixelSize)
{
    ApplyCharacterDitheringBayer4x4(positionCS, ditheringFactor, ditherPixelSize);
}

#endif
