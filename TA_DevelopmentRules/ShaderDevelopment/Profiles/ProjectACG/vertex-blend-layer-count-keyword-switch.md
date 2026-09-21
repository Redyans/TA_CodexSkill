# ProjectACG 顶点色混合层数开关（VertexBlend4 `_BlendLayerCount`）Profile

> **Profile ID**：`projectacg-vertex-blend-layer-count-keyword-switch-v1`
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACGMain3\ProjectACG\Client`
> **事实快照**：`2026-09-14`
> **用途**：记录 ProjectACG `Valkyria/Scene/VertexBlend4` 顶点色混合层数下拉 `_BlendLayerCount` 的关键字、Pass 与材质事实，以及「下拉为 Layer1 却渲染 2 层」的根因、修正和验证边界。本文是项目 Profile，不得上升为跨项目 CORE。跨项目的 KeywordEnum 开关写法、变体计数与 ShaderGUI 关键字同步方法见 [`../../references/keyword-enum-feature-switch-and-gui-sync.md`](../../references/keyword-enum-feature-switch-and-gui-sync.md)。
> 迁移到其他工程时删除本文件，只保留该参考与相关 CORE。
>
> 关联规则：`CTL-02`、`ORG-02`、`ORG-03`、`VAL-01`、`VAL-03`、`PRJ-02`。

## 1. 项目事实

| 项 | 内容 |
| --- | --- |
| Shader | `Client/Assets/Shader/Scene/New/PackedMaskPBR/BattleSceneVertexBlend4.shader`（`Valkyria/Scene/VertexBlend4`） |
| Include | `Common/VertexBlend4Bindings.hlsl`（关键字宏与 `UnityPerMaterial`）、`Common/VertexBlend4Surface.hlsl`（采样与权重） |
| 属性 | `[KeywordEnum(Layer1, Layer2, Layer3, Layer4)] _BlendLayerCount("Blend Layer Count", Float) = 0`，默认 `Layer1` = 1 层 |
| 关键字 | `_BLENDLAYERCOUNT_LAYER1` ~ `_BLENDLAYERCOUNT_LAYER4`，`shader_feature_local_fragment`，5 个 Pass（ForwardLit / ShadowCaster / DepthOnly / DepthNormals / Meta）各自声明 |
| 宏 | `VERTEXBLEND4_LAYER_COUNT`（1/2/3/4）驱动 `[unroll]` 循环；未用层的 Color/Normal/Mask 采样、权重和 `lerp` 在编译期消失；1 层只读顶点色 R 通道 |
| ShaderGUI | `Assets/Plugins/CustomShaderGUI/Editor/SimpleShaderGUI.cs`（`Scarecrow.SimpleShaderGUI`），属性经 `MaterialEditor.ShaderProperty` 绘制 |
| 功能说明 | 同目录 `README_Tech_PackedMaskPBR.md`（参数、`_LayerSliceMap`、逐层 `_LayerNColor`、维护记录） |

存量材质：`Assets/Shader/Scene/New/PackedMaskPBR/Valkyria_Scene_PackedMaskPBRLit.mat`、`Assets/TA_Test/LYJ/Scene/ManholeCover10_2K/New Material.mat` 等由其他 Shader 迁移而来，`m_ValidKeywords` 为空且没有 `_BlendLayerCount` 条目，默认档完全依赖 shader 默认值和关键字列表首项。

## 2. 现象与根因

- 现象：下拉为 `Layer1`（或材质根本没有该属性）时，画面实际渲染 2 层。
- 根因：5 个 Pass 的 pragma 只写了 `_BLENDLAYERCOUNT_LAYER2` ~ `_BLENDLAYERCOUNT_LAYER4`，既没有枚举 0 项 `_BLENDLAYERCOUNT_LAYER1`，也没有 `_`。材质不携带列表内任何关键字时 Unity 用列表第一项渲染，默认档因此落到 `LAYER2`。

## 3. 项目约束与修正

- 5 个 Pass 统一为 `#pragma shader_feature_local_fragment _BLENDLAYERCOUNT_LAYER1 _BLENDLAYERCOUNT_LAYER2 _BLENDLAYERCOUNT_LAYER3 _BLENDLAYERCOUNT_LAYER4`：正好 4 个状态、`Layer1` 位于首项、不加 `_`（否则会多编一个内容相同的 1 层变体）。
- `SimpleShaderGUI` 在 `OnGUI` 早期按 `_BlendLayerCount` 回写关键字（`SyncKeywordEnumKeywords`）：只动 shader 声明的关键字，幂等并 `EditorUtility.SetDirty`，打开材质即自愈粘贴、工具批量写值留下的脏关键字。
- 同轮修复：`SyncKeywordEnumKeywords` 内调用子类实例 `FindProperty` 重载导致 `CS0120`（整个 ShaderGUI 编译失败），改为静态 `ShaderGUI.FindProperty`。
- 已知未修：同 Shader 的 `_MixArray`（Properties 声明）与 `_MaskArray`（HLSL 声明）不一致，Inspector 的 MixArray 槽位不生效；改名属于材质数据迁移，需单独评估。

## 4. 验证证据

- 离线编译：`SimpleShaderGUI.cs` 以 Unity 2022.3.62f3 程序集加 Roslyn 编译 `exit=0`，只剩原有 `CS0108` 告警；用同一条命令编译改动前版本作对照，确认 `CS0120` 由本次改动引入并已消除。
- 静态：5 个 Pass 的 pragma 列表一致且正好等于 4 个枚举状态；存量材质 `m_ValidKeywords` 为空 → 首项 `Layer1` = 1 层。

## 5. 未验证项与回退

- 未完成：Unity 内重导入与实编译、逐档位（1/2/3/4 层）画面与性能对比、Android 变体收集或 SVC 与 AssetBundle 动态加载档位抽查。
- 回退：保留「首项 = 期望默认档」的写法，并重新打开受影响材质让 ShaderGUI 回写关键字；只改单个 Pass 或只改材质属性值都会重新引入不一致。
