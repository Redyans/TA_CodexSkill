# ProjectACG BaseLit 高光抗锯齿 Profile

> **Profile ID**：`projectacg-base-lit-specular-aa-v1`
>
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACGMain3\ProjectACG\Client`
>
> **事实快照**：`2026-09-02`
>
> **通用参考**：[Specular AA 与法线贴图方差过滤参考](../../references/specular-aa-and-normal-map-filtering.md)

本文只记录 ProjectACG 当前 `BattleSceneBaseLit`/`PackedMaskPBR` 的实现事实、用户决策、验证证据和未闭环边界。迁移到其他工程时必须重新确认路径、URP API、材质通道、投影模式和验证状态。

## 1. 目的、边界与 Source of Truth

本次需求是让场景 BaseLit 在低采样几何和运动镜头下稳定镜面高光，且“默认开启、不要 Inspector 开关”。本 Profile 不把当前实现包装成“所有法线贴图方差都已过滤”。

| 职责 | 当前 Source of Truth |
| --- | --- |
| Shader 属性与 Pass | `Assets/Shader/Scene/New/PackedMaskPBR/BattleSceneBaseLit.shader` |
| Forward 顶点/片元与投影模式 | `Assets/Shader/Scene/New/PackedMaskPBR/PackedMaskPBRForwardPass.hlsl` |
| PBR BRDF 与 Specular AA helper | `Assets/Shader/Scene/New/PackedMaskPBR/PackedMaskPBRLighting.hlsl` |
| 材质贴图采样与 `InputData` | `Assets/Shader/Scene/New/PackedMaskPBR/PackedMaskPBRInput.hlsl` |
| 几何/Mask 到 PBR 数值的转换 | `Assets/Shader/Scene/New/Scene_CommonFunc.hlsl` |
| URP 过滤函数 | `Packages/com.unity.render-pipelines.core@14.0.12/ShaderLibrary/CommonMaterial.hlsl` |
| 已有角色 AA 参考 | `Assets/Shader/Character/Chara_V2/Common/Chara_CommonHelpers_V2.hlsl` |
| Unity/URP 版本 | `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json`、`Packages/packages-lock.json` |

## 2. 项目事实与实现方式

### 2.1 BaseLit 的材质输入数据流

`BattleSceneBaseLit.shader` 暴露 `_BumpMap` 和隐藏的 `_BumpScale`。`PackedMaskPBRInput.hlsl` 中的 `SamplePackedMaskPBRNormalTS` 使用 `UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, ...), _BumpScale)`，并按 `_BumpScale` 将 `normalTS.z` 向 `1` 插值。

Forward 路径随后执行：

```text
SamplePackedMaskPBRNormalTS
  → TransformTangentToWorld
  → NormalizeNormalPerPixel
  → InputData.normalWS
  → InitializeBRDFData(..., smoothness, ...)
  → GlobalIllumination + LightingPhysicallyBased
```

Mask 中的粗糙度通道先在 `SceneBuildPBRMaskSourceFromSmoothnessMetallicAO` 中反相为 roughness source，再由 `SceneResolvePBRValuesFromMaskSource` 生成感知 `smoothness`。当前过滤 helper 接收的是该最终 `smoothness`，不是原始 Mask 值。

### 2.2 Specular AA 的落点

`PackedMaskPBRForwardPass.hlsl` 的 `PackedMaskPBRLitPassFragment` 在 `InputData` 初始化之后、`PackedMaskPBRFragmentPBRWithPerObjectShadow` 调用之前无条件执行：

```hlsl
half3 geometricNormalWS = useProjectedDecal
    ? projectedData.sceneNormalWS
    : input.normalWS;
smoothness = PackedMaskPBRFilterSpecularAASmoothness(smoothness, geometricNormalWS);
```

`PackedMaskPBRLighting.hlsl` 的 helper 复用 URP `ProjectedSpaceGeometricNormalFiltering`，当前常量为：

```text
screenSpaceVariance = 0.125
threshold           = 0.20
```

URP 函数使用 `ddx/ddy` 统计屏幕空间几何法线变化，再将方差转换为过滤后的感知光滑度。`PackedMaskPBRFragmentPBRInternal` 用过滤后的值构造 `BRDFData`；同一 `BRDFData` 同时供 `GlobalIllumination`、主光和附加灯的 `LightingPhysicallyBased` 使用，因此 Direct/Indirect Specular 共享过滤结果。

### 2.3 普通模型与屏幕空间投影模式

| 模式 | 方差估计使用的法线 | 说明 |
| --- | --- | --- |
| 普通模型 | `input.normalWS` | 顶点阶段由 `GetVertexNormalInputs` 得到并插值；这是几何法线输入，最终着色法线仍会叠加 `_BumpMap`。 |
| `_UseScreenSpaceDecalProjection` 开启 | `projectedData.sceneNormalWS` | 由重建的 `projectedData.positionWS` 的 `ddx/ddy` 叉积得到，并按视线方向校正朝向；与投影后的光照位置保持一致。 |

投影模式仍用 `projectedData.tangentWS/bitangentWS/sceneNormalWS` 将 `normalTS` 转换为最终 `inputData.normalWS`。Specular AA 只读取 `sceneNormalWS` 做几何方差，不替换这套着色法线。

### 2.4 Pass、阴影和渲染器边界

当前 BaseLit 的 Forward、ShadowCaster、DepthOnly、DepthNormals、Meta Pass 均使用既有 `#pragma target 3.5`；这属于项目兼容性基线，不因新增 `ddx/ddy` 自动降级。Specular AA 只接入 Forward 的 PBR 光照路径，其他 Pass 是否需要改动必须以实际消费者为准，不能机械复制 Forward 片元计算。

BaseLit 的屏幕空间投影和阴影组合仍受 `_UsePerObjectShadow` 与 `_ReceiveShadows` 约束：`_UsePerObjectShadow` 编码为 `UnityOnly=0`、`Both=1`、`POSOnly=2`、`Off=3`。高光验证不能绕过这些模式；应确认过滤后的 `smoothness` 不改变 POS、主光/附加灯阴影、Fog、Depth/Stencil 和多相机时序。

## 3. 用户决策与材质接口约束

### PACG-BASELIT-SAA-01｜强制开启，不新增开关

当前决策是所有 BaseLit Forward 像素都执行 Specular AA：

- `BattleSceneBaseLit.shader` 不新增 `_SpecularAA` 属性；
- `PackedMaskPBRInput.hlsl` 的 `UnityPerMaterial` 不新增 `_SpecularAA` 字段；
- 不新增 `shader_feature`、`multi_compile` 或全局 Keyword；
- 不让材质、动画、Timeline 或脚本写入一个实际不生效的伪开关。

这样可以避免材质状态分裂、变体膨胀和“面板关闭但代码仍过滤”的歧义；代价是所有 BaseLit 材质固定承担 `ddx/ddy` 和粗糙度过滤成本。若未来低端设备测量证明需要质量档位，应另立兼容性变更，定义质量控制方、变体/分支策略和构建保留路径。

### PACG-BASELIT-SAA-02｜只改 Specular 粗糙度，不改整体法线

过滤后的 `smoothness` 在 BRDF 构造前写回，`inputData.normalWS`、漫反射、AO、阴影、Fog、投影和法线贴图采样保持原路径。不要通过平滑或替换 `inputData.normalWS` 来“抗锯齿”，否则会同时改变漫反射、阴影边界、法线细节和既有材质风格。

## 4. 有法线贴图的模型是否生效

结论分两层：

1. **法线贴图对 BaseLit 光照生效。** `_BumpMap` 经 `SamplePackedMaskPBRNormalTS` 采样后进入 `InputData.normalWS`，会影响 `LightingPhysicallyBased` 和 `GlobalIllumination` 的着色法线。
2. **当前 Specular AA 没有完整计算法线贴图自身的高频方差。** 普通模型的过滤输入是 `input.normalWS`（几何法线），投影模式是 `projectedData.sceneNormalWS`；两者都不是叠加 `_BumpMap` 后的法线分布统计。

因此当前版本可以改善曲面/低模几何造成的高光闪烁，但不能保证压住高频法线贴图细节造成的闪烁。出现“模型有法线贴图，AA 仍不稳定”并不代表 `_BumpMap` 没生效，而是纹理方差尚未接入。

### 4.1 要实现法线贴图完整覆盖的候选方案

按风险从低到高：

| 方案 | 需要补齐的契约 | 当前状态 |
| --- | --- | --- |
| 预烘平均法线长度/向量 | 导出工具、纹理通道、Importer 压缩/Mip、采样与旧资产回退。用 `TextureNormalVariance(avgNormalLength)` 合并几何方差。 | 未接入 BaseLit。 |
| 独立方差纹理 | 新增资源绑定、UV/采样成本、AssetBundle/材质迁移和缺省纹理。 | 未评估。 |
| 运行时导数估算 | 额外法线采样或梯度计算；需验证精度、分支发散、移动 GPU 和平台 API。 | 未评估。 |

ProjectACG 的 `Chara_CommonHelpers_V2.hlsl` 已存在带 `normalTexVar` 与 `normalMapWeight` 的 `FilterCharacterRoughness` 重载，可作为项目内方差数据设计的参考；它依赖角色侧自己的平均法线编码和资产契约，不能直接当作 BaseLit 已支持的证明，也不能未经适配复用其 Alpha 语义。

在补齐纹理方差前，文档、Shader Debug 和验收报告应明确写“几何法线 Specular AA”，不要写“法线贴图 Specular AA 已完成”。

## 5. 已确认的问题、根因与解决方式

### 5.1 初始开关设计与最终强制开启的差异

角色 Shader 家族中仍能看到 `_SpecularAA` 的历史开关和条件调用；这不能复制到 BaseLit。BaseLit 本次按用户要求改为无条件调用，避免出现旧材质默认值为 `0` 导致功能未启用，或 Inspector 状态与实际编译路径不一致。

### 5.2 只过滤几何法线的边界

URP 的 `ProjectedSpaceGeometricNormalFiltering` 只消费几何法线导数。它不是法线贴图方差估计器；没有 `avgNormalLength`、预烘方差 Alpha 或独立纹理时，纹理高频部分无法被同一公式覆盖。排查时先区分几何闪烁与纹理闪烁，再决定是否扩展资产管线。

### 5.3 参数和坐标的敏感性

`screenSpaceVariance = 0.125`、`threshold = 0.20` 沿用项目已有 Chara V2 几何法线路径，属于当前实现基线，不是跨项目或所有材质的最佳值。过滤效果会随分辨率、FOV、相机距离、模型屏幕占比和法线插值变化；不能用单张静态截图证明全场景稳定。

### 5.4 投影模式法线不能继续沿用网格法线

屏幕空间投影会重建位置和切线基。如果仍用原网格法线做 AA 方差，投影后的高光变化与实际着色表面不一致。当前实现使用 `sceneNormalWS`，并对叉积结果按视线方向翻转，保持投影表面的朝向一致。

### 5.5 直接光/间接光必须共享过滤结果

过滤调用位于 `PackedMaskPBRFragmentPBRWithPerObjectShadow` 之前，最终 `BRDFData` 由过滤后的 `smoothness` 构造。这样 `GlobalIllumination`、主光和附加灯共享同一粗糙度；不能只在某个 `LightingPhysicallyBased` 分支临时修改。

## 6. ProjectACG 验证证据与未验证项

### 6.1 已完成的静态核对

- 已确认 `ProjectedSpaceGeometricNormalFiltering` 在当前嵌入式 URP `14.0.12` 的 `CommonMaterial.hlsl` 中存在，参数顺序与调用一致。
- 已确认 BaseLit Forward 无条件调用 `PackedMaskPBRFilterSpecularAASmoothness`，且调用发生在 BRDF 前。
- 已确认普通模型/投影模式分别使用 `input.normalWS`/`projectedData.sceneNormalWS`。
- 已确认 `_BumpMap → normalTS → InputData.normalWS → BRDF` 数据流仍保留。
- 已确认 BaseLit Shader 属性和 `UnityPerMaterial` CBUFFER 没有新增 `_SpecularAA` 接口。
- 目标 Shader/HLSL 的静态 diff/空白检查已通过；代码修改已存在于 Git 提交 `967354e174`（仅用于定位历史，不替代运行时证据）。

### 6.2 当前未完成或不能宣称已通过的验证

- 本次强制开启版本的可靠 Unity Shader 编译结果；此前 batchmode 受到已有编辑器实例/License IPC 状态影响，旧 `compile-result.json` 不能作为当前版本证据。
- Unity Scene/Game View 高光闪烁前后 A/B，尤其是运动镜头、不同分辨率、FOV、相机距离和投影模式。
- 高频法线贴图模型的纹理方差覆盖；当前没有 BaseLit 的预烘平均法线长度/方差通道。
- Android/移动 GPU 的额外导数、ALU、带宽和帧时长成本，以及构建变体报告。
- ShadowCaster、DepthOnly、DepthNormals、Meta 等非 Forward Pass 的实际编译/绘制路径回归；它们通常不消费 Specular，但仍需按当前 Pass 矩阵确认。

未完成项不能在交付或后续规则中写成“Unity 已验证”“法线贴图已支持”或“移动端无性能影响”。

## 7. 验证与回退流程

### 7.1 推荐 A/B 矩阵

固定同一场景、灯光、曝光、材质和相机，至少覆盖：

| 组别 | 条件 |
| --- | --- |
| 几何 | 低模硬边、平滑曲面、近景、远景、快速旋转/平移。 |
| 材质 | 金属/非金属、低/高 smoothness、无 `_BumpMap`、低频法线贴图、高频法线贴图。 |
| 模式 | 普通模型、`_UseScreenSpaceDecalProjection` 投影模式。 |
| 光照 | 主光、附加灯、环境反射/IBL、阴影开关和 `_UsePerObjectShadow` 四种模式。 |
| 输出 | Game/SceneView（如使用）、不同分辨率、代表性 Android 图形 API。 |

分别记录原始/过滤后 `smoothness`、几何方差、（未来的）纹理方差、Direct Specular、Indirect Specular 和最终颜色。当前 BaseLit 没有材质 Off/On 开关；A/B 的 Off 侧应使用临时旁路或诊断分支，不得把旁路重新提交为 Inspector 接口。只看最终截图无法定位是 BRDF、环境 Mip、阴影还是后处理差异。

### 7.2 回退顺序

1. 首先只回退 `screenSpaceVariance`/`threshold` 到已知基线，确认是否为参数过强。
2. 再临时旁路 `PackedMaskPBRFilterSpecularAASmoothness`，保持 `_BumpMap`、材质属性、Pass 和 Renderer 配置不变，用于确认根因是否在 Specular AA。
3. 若需长期关闭某类设备，另建质量策略和构建验证，不恢复一个没有明确所有权的材质开关。
4. 若未来接入法线贴图方差，保留无方差旧材质的几何法线回退，不删除或重烘全部现有法线贴图作为第一选择。

## 8. 维护规则

- 修改 BaseLit 的高光、法线、投影或 BRDF 前，先同步检查本 Profile、通用参考、当前 URP `CommonMaterial.hlsl` 和所有 `PackedMaskPBR` Pass。
- 新增方差通道、材质属性、Keyword、Importer 或导出工具时，必须把资产迁移、变体、序列化和回退写成独立变更；不能在本 Profile 中默认为已完成。
- 任何视觉或性能结论都要注明是静态、Unity 编辑器、Frame Debugger、设备还是构建验证；没有实际证据时保留为“未验证”。
- 若源码、URP 版本或用户决策改变，先更新 Source of Truth、实现事实和验证边界，再考虑修订通用参考；不要把项目特例上提为 CORE。
