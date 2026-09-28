// Purpose: Chara_V2 公共五材质区域解析与参数选择模块。
// 调用方负责采样 LightMap、声明材质属性和 CBUFFER；本文件不声明纹理、材质参数或 Keyword。
#ifndef CHARA_MATERIAL_REGION5_V2_INCLUDED
#define CHARA_MATERIAL_REGION5_V2_INCLUDED

// LightMap A 的离散区域编码沿用参考工程的现有资产约定：
//
//   [0.0, 0.2) -> 区域 1（默认区域）
//   [0.2, 0.4) -> 区域 4
//   [0.4, 0.6) -> 区域 3
//   [0.6, 0.8) -> 区域 5
//   [0.8, 1.0] -> 区域 2
//
// 区域编号顺序看似不连续，但这是为了兼容参考工程已有 LightMap A 的编码，
// 后续接入不同 Chara_V2 Shader 时必须复用该函数，禁止各 Shader 自行解释通道。
// LightMap A 是离散 ID，不是连续遮罩：禁止在区域内部绘制渐变。若同一张 LightMap
// 的其他通道必须使用 Bilinear/Mipmap，A 通道需要做边界扩张并检查压缩后的中间值串区。
uint CharaDecodeMaterialRegion5(float lightMapAlpha)
{
    float materialMask = saturate(lightMapAlpha);

    if (materialMask >= 0.8)
        return 2u;
    if (materialMask >= 0.6)
        return 5u;
    if (materialMask >= 0.4)
        return 3u;
    if (materialMask >= 0.2)
        return 4u;

    return 1u;
}

// enabledRegions2345 的分量约定：x=区域2、y=区域3、z=区域4、w=区域5。
// 未启用的区域统一回退到区域 1，保持旧材质和未配置材质的稳定结果。
uint CharaResolveEnabledMaterialRegion5(
    uint materialRegion,
    float4 enabledRegions2345)
{
    if (materialRegion == 2u && enabledRegions2345.x < 0.5)
        return 1u;
    if (materialRegion == 3u && enabledRegions2345.y < 0.5)
        return 1u;
    if (materialRegion == 4u && enabledRegions2345.z < 0.5)
        return 1u;
    if (materialRegion == 5u && enabledRegions2345.w < 0.5)
        return 1u;

    return materialRegion >= 1u && materialRegion <= 5u
        ? materialRegion
        : 1u;
}

// 推荐的一步式入口。调用方不需要单独保存原始区域编号时直接使用此函数。
uint CharaGetMaterialRegion5(
    float lightMapAlpha,
    float4 enabledRegions2345)
{
    return CharaResolveEnabledMaterialRegion5(
        CharaDecodeMaterialRegion5(lightMapAlpha),
        enabledRegions2345);
}

// 与参考工程五区域功能对齐的公共参数集合。
// Shader 需要整套区域参数时可一次选择，编译器会在内联后移除未使用字段；
// 只需要一种颜色或标量的 Pass（例如 Outline）应使用下方轻量选择函数。
struct CharaMaterialRegionDataV2
{
    float3 baseColorMultiplier;
    float3 outlineColor;
    float3 warmShadowColor;
    float3 coolShadowColor;
    float shadowTransitionRange;
    float shadowTransitionSoftness;
    float emissionScale;
    float specularShininess;
    float specularIntensity;
};

CharaMaterialRegionDataV2 CharaSelectMaterialRegion5Data(
    uint materialRegion,
    CharaMaterialRegionDataV2 region1,
    CharaMaterialRegionDataV2 region2,
    CharaMaterialRegionDataV2 region3,
    CharaMaterialRegionDataV2 region4,
    CharaMaterialRegionDataV2 region5)
{
    if (materialRegion == 2u)
        return region2;
    if (materialRegion == 3u)
        return region3;
    if (materialRegion == 4u)
        return region4;
    if (materialRegion == 5u)
        return region5;

    return region1;
}

// 以下选择函数不绑定具体材质模型。BaseColor、描边色、冷暖阴影色、
// 阴影过渡、高光参数和 Emission 等都通过同一 regionId 选择各自的五组值。
// 非法 regionId 统一回退到 value1。
float CharaSelectMaterialRegion5Float(
    uint materialRegion,
    float value1,
    float value2,
    float value3,
    float value4,
    float value5)
{
    if (materialRegion == 2u)
        return value2;
    if (materialRegion == 3u)
        return value3;
    if (materialRegion == 4u)
        return value4;
    if (materialRegion == 5u)
        return value5;

    return value1;
}

float2 CharaSelectMaterialRegion5Float2(
    uint materialRegion,
    float2 value1,
    float2 value2,
    float2 value3,
    float2 value4,
    float2 value5)
{
    if (materialRegion == 2u)
        return value2;
    if (materialRegion == 3u)
        return value3;
    if (materialRegion == 4u)
        return value4;
    if (materialRegion == 5u)
        return value5;

    return value1;
}

float3 CharaSelectMaterialRegion5Float3(
    uint materialRegion,
    float3 value1,
    float3 value2,
    float3 value3,
    float3 value4,
    float3 value5)
{
    if (materialRegion == 2u)
        return value2;
    if (materialRegion == 3u)
        return value3;
    if (materialRegion == 4u)
        return value4;
    if (materialRegion == 5u)
        return value5;

    return value1;
}

float4 CharaSelectMaterialRegion5Float4(
    uint materialRegion,
    float4 value1,
    float4 value2,
    float4 value3,
    float4 value4,
    float4 value5)
{
    if (materialRegion == 2u)
        return value2;
    if (materialRegion == 3u)
        return value3;
    if (materialRegion == 4u)
        return value4;
    if (materialRegion == 5u)
        return value5;

    return value1;
}

#endif // CHARA_MATERIAL_REGION5_V2_INCLUDED
