# ProjectACG URP 材质导入、默认 Shader 与 SH 求值 Profile

> **Profile ID**：`projectacg-urp-material-import-and-sh-evaluation-v1`  
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACGMain3\ProjectACG\Client`  
> **事实快照**：2026-09-02  
> **用途**：记录本工程 3ds Max Physical Material 导入、编辑器新材质默认 Shader、URP SH 求值和相关变体治理的实际实现、风险与验证边界。本文只属于 ProjectACG `PROFILE`；可迁移方法见 [`../../references/urp-material-import-and-sh-evaluation.md`](../../references/urp-material-import-and-sh-evaluation.md) 和 [`../../references/shader-variant-stripping-and-build-safety.md`](../../references/shader-variant-stripping-and-build-safety.md)。

## 1. 工程基线与实现文件

| 职责 | 当前实现 |
| --- | --- |
| Unity / URP | Unity `2022.3.62f3`；嵌入式 `com.unity.render-pipelines.universal@14.0.12` |
| 3ds Max 材质导入 | `Packages/com.unity.render-pipelines.universal@14.0.12/Editor/AssetPostProcessors/PhysicalMaterial3DsMaxPreprocessor.cs` |
| 新材质默认策略 | `Assets/Editor/Utils/DefaultMaterialShaderPolicy.cs` |
| 材质批量生成器 | `Assets/Editor/TA_Tools/LYJ_Tool/Asset_type/MaterialGenerator/MaterialGenerator.cs` |
| 默认模板材质 | `Assets/Editor/Utils/New Material.mat` |
| 变体剔除器 | `Assets/TEngine/Editor/ShaderStripping/TEngineShaderVariantStripper.cs` |
| 变体剔除配置 | `Assets/TEngine/Editor/ShaderStripping/ShaderStrippingProfile.asset` |

## 2. 3ds Max Physical Material 导入实现

### 2.1 已落地行为

- `PhysicalMaterial3DsMaxPreprocessor.GetVersion()` 已从 `2` 提升到 `3`，用于触发存量模型重新执行材质处理。
- `Physical Material` 与 `Simplified Physical Material` 都通过 `ShaderUtils.GetShaderGUID(ShaderPathID.Unlit)` 解析并绑定 `Universal Render Pipeline/Unlit`，不再绑定旧 Physical Material Shader Graph。
- 换 Shader 后清空旧 `material.shaderKeywords`，避免携带 `_EMISSION` 等不属于 Unlit 的历史关键字。
- 透明材质统一设置 `_Surface`、`_Blend`、`_AlphaClip`，再调用 URP `BaseShaderGUI.SetupMaterialBlendMode`，让 Blend、Queue、RenderType 和相关关键字由目标 Shader 规则统一配置。
- `base_color_map` / `basecolor` 映射到 `_BaseMap` / `_BaseColor`，并保留纹理 Offset/Scale。
- Physical Material 的 `transparency` 按“不透明度”语义写入 `_BaseColor.a`：`alpha *= clamp01(1 - transparency)`。
- 导入时会清除模型材质动画曲线，避免旧 Shader 属性曲线继续写入无效属性。

### 2.2 有意保留的兼容边界

- URP Unlit 只有一个主要 Base Map。颜色贴图与独立透明贴图同时存在时，当前实现优先颜色贴图；不会自动完成透明贴图反相和两图合成。
- 旧 Physical Material 的金属、粗糙度、法线、AO、发光等输入不再映射到 Unlit。若资产需要这些外观，应该新建专用适配 Shader，而不是向 Unlit 偷塞无效属性。
- 导入器只有在 `GraphicsSettings.currentRenderPipeline` 是 `UniversalRenderPipelineAsset` 时才执行；非 URP 工程不会被强制改写。
- 仅提升导入器版本不会替代 Unity 中的实际 Reimport。存量 FBX 必须重新导入并检查生成材质、透明和纹理变换。

## 3. 新材质默认 Shader 实现

### 3.1 当前策略

`DefaultMaterialShaderPolicy` 是 Editor-only `[InitializeOnLoad]` 类：

1. 通过 `Shader.Find("Universal Render Pipeline/Unlit")` 缓存目标 Shader。
2. 监听 `EditorApplication.update`，检查当前 `GraphicsSettings.currentRenderPipeline`。
3. URP 14 的 `RenderPipelineAsset.defaultShader` 为只读，策略通过反射写入 URP Asset 私有 `m_DefaultShader` 字段。
4. 管线切换后重新应用；字段不存在或 Shader 未导入时只输出一次警告，并保留 Unity fallback，不阻塞编辑器。

项目材质生成器在未指定 Shader 时也显式调用该策略；`New Material.mat` 的 Shader GUID 已切换为 URP Unlit。该策略不批量迁移已有材质，也不修改 URP Package 源文件。

### 3.2 维护边界

- 反射依赖私有字段名，升级 Unity/URP 后必须重新检查；反射失败不能默认为“默认值已生效”。
- 该文件位于 `Assets/Editor`，不得被运行时代码引用或进入 Player。
- Unity 内置新建材质、项目工具、FBX 导入器和模板材质是四个独立入口；新增入口时必须加入默认 Shader 覆盖清单。
- 默认改为 Unlit 只影响新建/导入路径；工程中存量 Lit 材质仍需单独扫描和迁移决策。

## 4. URP SH 求值与质量档配置

### 4.1 源码事实

当前 URP 源码的 `ShEvalMode` 为：

```text
Auto      = 0
PerVertex = 1
Mixed     = 2
PerPixel  = 3
```

`ForwardLights` 通过 `PlatformAutoDetect.ShAutoDetect` 根据平台处理 `Auto`：XR Mobile、移动 Shader API 和 Switch 会返回 `PerVertex`，其他平台通常返回 `PerPixel`。随后设置 `EVALUATE_SH_MIXED` / `EVALUATE_SH_VERTEX` 全局关键字。

因此“所有平台逐像素 SH”必须选择 `PerPixel`，不能保留 `Auto`。

### 4.2 本工程实际资产

以下 8 个运行时质量 URP Asset 当前均为：

```yaml
m_ShEvalMode: 1
m_MixedLightingSupported: 1
```

`m_ShEvalMode: 1` 对应 `PerVertex`，与“所有平台逐像素 SH”的目标相反；当前只是已确认的工程现状，不是推荐配置。

| 场景 | Low | Mid | High | Ultra |
| --- | --- | --- | --- | --- |
| Battle | `URP-Battle-Low.asset` | `URP-Battle-Mid.asset` | `URP-Battle-High.asset` | `URP-Battle-Ultra.asset` |
| Lobby | `URP-Lobby-Low.asset` | `URP-Lobby-Mid.asset` | `URP-Lobby-HighFidelity.asset` | `URP-Lobby-Ultra.asset` |

运行时 `URPQualitySwitcher` 会按场景和设备质量切换这些资产；只改 `ProjectSettings/GraphicsSettings.asset` 的默认管线不能覆盖质量级别的 `QualitySettings.renderPipeline`。

### 4.3 操作结论

在每个上述 URP Asset Inspector 中设置：

```text
Lighting
  -> Additional Properties / Additional Settings
    -> SH Evaluation Mode = Per Pixel
```

为满足本需求，序列化结果应改为：

```yaml
m_ShEvalMode: 3
```

`PerPixel` 仍然使用 Light Probe 的 SH 数据，只是把 SH 求值放到片元阶段；它不等价于关闭 Renderer 的 Light Probes。若要某个 Renderer 完全不接受探针，需单独设置该 Renderer 的 `Light Probes = Off`，并做视觉回归。

## 5. Mixed Lighting 与变体关系

- `LIGHTMAP_SHADOW_MIXING` 不是顶点 SH 关键字，而是混合光照/光照贴图阴影路径。
- `m_MixedLightingSupported: 1` 当前保留 Mixed Lighting 变体。只有确认没有 Mixed Lights、Shadowmask、Distance Shadowmask 和 Subtractive Lighting 消费者后，才可在所有运行时 URP Asset 中改为 `0`。
- 变体理论组合为：3 种 SH 求值模式（无关键字、`EVALUATE_SH_MIXED`、`EVALUATE_SH_VERTEX`）× 2 种 `LIGHTMAP_SHADOW_MIXING` 状态，即每个 Pass 最多 6 种该维度组合。最终构建数量还受其他 Keyword、Pass、平台和剔除规则影响。
- `TEngineShaderVariantStripper` 已将 `EVALUATE_SH_VERTEX`、`EVALUATE_SH_MIXED` 和 `LIGHTMAP_SHADOW_MIXING` 放入 `LocalComboSignatureIgnoredKeywords`，避免把管线关键字误当成材质局部组合。
- 若 `ShaderStrippingProfile.asset` 的 `StripAllVariantsShaderNames` 命中某 Shader，该 Shader 每个 Pass 直接 `data.Clear()`，上述三个关键字是否保留不再有意义。

## 6. 关联的全量剔除配置

当前 `StripAllVariantsShaderNames` 实际配置为：

```text
Universal Render Pipeline/Lit
Hidden/TerrainEngine/Details/UniversalPipeline/Vertexlit
Hidden/TerrainEngine/Details/UniversalPipeline/WavingDoublePass
Hidden/TerrainEngine/Details/UniversalPipeline/BillboardWavingDoublePass
Shader Graphs/PhysicalMaterial3DsMax
Shader Graphs/PhysicalMaterial3DsMaxTransparent
```

命中规则使用精确的 `Shader.name`，对每个回调 Pass 清空变体。当前静态扫描发现：

- `Assets` 下约 `180` 个 `.mat` 仍引用 `Universal Render Pipeline/Lit`；
- 其中 `Assets/AssetRaw` 下有 `8` 个材质引用 Lit；
- 因此 Lit 全量剔除存在粉材质/`Missing Shader` 风险，不能仅凭“工程大部分材质不用 Lit”视为生产安全；
- `PhysicalMaterial3DsMax` 旧 ShaderGraph 的存量材质也要在 FBX/材质重导入后复查，不能只依赖剔除列表掩盖未迁移资产。

完整的裁剪层次、证据采集和回退方式见 [`shader-variant-stripping.md`](shader-variant-stripping.md)。

## 7. 验证记录与未验证项

### 已完成的静态检查

- URP 14.0.12 源码确认 `ShEvalMode` 枚举、`Auto` 平台分支和 Inspector 字段。
- 8 个运行时质量 URP Asset 的 `m_ShEvalMode` / `m_MixedLightingSupported` 已盘点；当前为 `1 / 1`，即 `PerVertex` + Mixed Lighting 开启。
- Lit 引用扫描为 `Assets` 下 180 个 `.mat`，`Assets/AssetRaw` 下 8 个。
- 导入器目标 Shader、版本号、材质属性映射和默认 Shader 策略代码已核对。
- 变体剔除器的精确名称匹配、逐 Pass 清空、关键字忽略集合和配置列表已核对。

### 尚未完成

- 未在 Unity Editor 中批量 Reimport 所有 FBX，也未逐个确认颜色/透明贴图视觉结果。
- 尚未将 8 个运行时 URP Asset 从 `m_ShEvalMode: 1` 实际改为 `m_ShEvalMode: 3`（`Per Pixel`），也未用移动/XR/Switch 目标运行验证。
- 未证明工程完全不使用 Mixed Lighting/Shadowmask，因此未关闭 `m_MixedLightingSupported`。
- 未完成干净 Player/AssetBundle 构建、最终每 Pass 变体计数、Frame Debugger、真机粉材质和包体回归。

## 8. 回退与维护清单

1. 导入外观异常：恢复目标材质 Shader 映射或回退导入器版本，清理生成材质后重新 Reimport；不要删除 URP Package 源文件。
2. 新材质默认异常：检查当前活动 URP Asset、私有字段名和 `Shader.Find` 结果；反射失败时暂时改用项目工具显式传入 Shader。
3. 移动端出现顶点 SH：逐个检查实际加载的 URP Asset 是否为 `m_ShEvalMode: 3`，并确认没有运行旧 Player/Bundle。
4. 粉材质或 Pass 缺失：先从 `StripAllVariantsShaderNames` 删除目标 Shader，清理 Shader/AssetBundle 缓存后用 `KeepAll` 重建，再定位静态或动态引用。
5. 修改 URP Asset、导入器或剔除 Profile 后，必须重新执行 Unity 导入、目标平台构建和代表性场景/质量档验证；静态检查不能替代宿主行为。
