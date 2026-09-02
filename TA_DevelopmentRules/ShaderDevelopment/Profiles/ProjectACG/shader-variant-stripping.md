# ProjectACG Shader 变体裁剪与全量剔除 Profile

> **Profile ID**：`projectacg-shader-variant-stripping-v1`
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACGMain3\ProjectACG\Client`
> **事实快照**：2026-09-02
> **用途**：记录 ProjectACG 当前 TEngine Shader 变体裁剪、材质证据采集和“按 Shader 全量剔除”功能的实现事实、问题和验证边界。本文是项目 Profile，不得上升为跨项目 CORE。通用方法见 [`../../references/shader-variant-stripping-and-build-safety.md`](../../references/shader-variant-stripping-and-build-safety.md)；`BattleSceneBaseLit` 的专用 SVC 与 Pass/关键字矩阵见 [`battle-scene-baselit-variant-stripping.md`](battle-scene-baselit-variant-stripping.md)。

## 1. 当前实现事实

| 项目 | 当前事实 |
| --- | --- |
| Unity | `2022.3.62f3` |
| URP | 嵌入式 `com.unity.render-pipelines.universal@14.0.12` |
| 裁剪入口 | `Assets/TEngine/Editor/ShaderStripping/TEngineShaderVariantStripper.cs` |
| Profile | `Assets/TEngine/Editor/ShaderStripping/ShaderStrippingProfile.asset` |
| 构建验证工具 | `Assets/TEngine/Editor/ShaderStripping/ShaderVariantBuildTool.cs` |
| 审计工具 | `Assets/TEngine/Editor/ShaderStripping/ShaderVariantAuditTool.cs` |
| 采集工具 | `Assets/TEngine/Editor/ShaderStripping/ShaderVariantUsageCollector.cs` |
| 当前收集模式 | `DefaultPackagePlusScenes`，Package 为 `DefaultPackage` |
| 当前局部组合裁剪 | `EnableAssetsLocalComboStrip: 1` |
| 当前回退策略 | `FallbackPolicy: KeepAll` |
| SVC 白名单 | `EnableShaderVariantCollectionAllowList: 1` |
| SVC 路径 | `Assets/TEngine/Editor/ShaderStripping/NewShaderVariants.shadervariants` |
| SVC 治理 Shader | `Valkyria/Scene/BaseLit` |
| 全量剔除列表 | `StripAllVariantsShaderNames`，当前已包含 Lit 等 6 个 Shader |
| 后处理额外关键字 | `UberPostExtraStripKeywordNames`，仅作用于 `Hidden/Universal Render Pipeline/UberPost` |

## 2. 当前裁剪层次

1. `Meta`、`ScenePickingPass`、`SceneSelectionPass` 等 Editor-only Pass 在回调中直接清空。
2. 对 URP 白名单 Shader 或 `Assets/Shader/` Shader 应用额外关键字黑名单。
3. 从静态材质 `Material.enabledKeywords` 采集本地组合，并对目标 Shader 做组合白名单裁剪。
4. 对配置在 `ShaderVariantCollectionAllowListShaderNames` 中的 Shader，按 Pass 类型和完整关键字集合匹配 SVC，只保留集合内变体。
5. 当 `StripAllVariantsShaderNames` 包含完整 `Shader.name` 时，对该 Shader 的每个回调 Pass 执行 `data.Clear()`，原因记为 `ShaderConfiguredFullStrip`。

全量剔除分支位于材质证据计算之前，因此命中后不会继续扫描材质、计算签名或执行 Terrain Shader 证据映射。

SVC 白名单与局部材质组合裁剪互斥，避免“人工审核保留的变体”又被第二套材质规则删掉；全量剔除优先级最高。

## 3. 使用方式与回退

### 3.1 启用某个 Shader 全量剔除

编辑 Profile：

```yaml
StripAllVariantsShaderNames:
- Universal Render Pipeline/Lit
```

必须使用精确的 `Shader.name`。只写 `Lit`、文件名 `Lit.shader` 或路径片段不会命中。

当前 Profile 已启用以下全量剔除：

```text
Universal Render Pipeline/Lit
Hidden/TerrainEngine/Details/UniversalPipeline/Vertexlit
Hidden/TerrainEngine/Details/UniversalPipeline/WavingDoublePass
Hidden/TerrainEngine/Details/UniversalPipeline/BillboardWavingDoublePass
Shader Graphs/PhysicalMaterial3DsMax
Shader Graphs/PhysicalMaterial3DsMaxTransparent
```

其中 Lit 的静态材质引用尚未全部解释，生产包启用前必须完成运行时验证。

### 3.2 使用 ShaderVariantCollection 白名单

当前 SVC 配置为：

```yaml
EnableShaderVariantCollectionAllowList: 1
ShaderVariantCollectionAllowListPath: Assets/TEngine/Editor/ShaderStripping/NewShaderVariants.shadervariants
ShaderVariantCollectionAllowListShaderNames:
- Valkyria/Scene/BaseLit
```

集合当前包含 20 条 BaseLit 变体记录。实现按 `passType` 和排序后的完整关键字集合匹配；集合路径、条目为空或指定 Shader 缺失时会输出错误并关闭白名单，避免“坏集合导致全量误删”。

### 3.3 回退

1. 从列表删除目标 Shader 名称，恢复为空列表；若问题来自 SVC，则关闭 `EnableShaderVariantCollectionAllowList`。
2. 清理 Shader/AssetBundle/YooAsset 构建缓存。
3. 重新导出变体审计报告并重新构建目标 Package。
4. 在代表性场景和动态加载路径确认材质恢复。

当前 Profile 已把 `Universal Render Pipeline/Lit` 写入列表，但静态扫描仍发现 `Assets` 下有 180 个材质、其中 `Assets/AssetRaw` 下有 8 个材质引用它；这些引用尚未解释前，不应视为生产安全：

```text
Assets/AssetRaw/character/monster/test_levelprefab/defense_material.mat
Assets/AssetRaw/character/monster/test_levelprefab/base_material02.mat
Assets/AssetRaw/character/monster/test_levelprefab/base_material.mat
Assets/AssetRaw/character/weapon/M249_MG/mat/Weampn_M249.mat
Assets/AssetRaw/Scene/map_ACG_A199C19L19_build/mat/mat_terrain_A199C19L19_build.mat
Assets/AssetRaw/Effects/Models/Materials/No Name.mat
Assets/AssetRaw/UI/Settlement/balck.mat
Assets/AssetRaw/Scene/map_ACG_AIchat_Bedroom/mat/mat_bedroom_dengdai_bake.mat
```

这些引用是否进入目标包、是否会被运行时加载，仍需以实际 YooAsset BuildMap 和 Player/真机验证为准。

## 4. 历史演进证据

以下提交可用于追溯当前实现为何形成，阅读时以仓库实际提交内容为准：

| 提交 | 经验/问题 |
| --- | --- |
| `4841a95a5f` | 场景路径错误会导致大厅白屏和变体采集异常；Profile 场景路径必须逐项确认资产真实位置。 |
| `0303076b6a` | 增加 `Assets/Shader` 额外关键字剥离；全局关键字规则必须限制 Shader 路径/家族范围。 |
| `76f3270344` | Fog、Lightmap、ShadowMask、SH 等管线关键字误参与本地组合签名会把 Forward 剥离到 0；签名必须投影到真正 local 维度。 |
| `6f4dfe3cad` | 当前 Pass 维度投影和材质组合签名逻辑形成；材质证据与编译器变体必须使用同一规范化口径。 |
| `73a2a2f140` | 后处理 UberPost 额外关键字应单独按 Shader 限定，不能把固定后处理策略扩散到其它 URP Shader。 |
| `938fd26bf5` | `BattleSceneBaseLit` 使用 SVC 白名单；保留源码能力、构建层精确剥离，避免直接删除 pragma 造成不可逆兼容变化。 |

这些提交记录是 ProjectACG 的演进背景，不应被直接当作其他工程的默认实现。

## 5. 本次已确认的问题

### 5.1 配置总开关边界不完整

当前 `Enabled` 主要控制 Profile 材质组合采集和 Assets 组合裁剪；Editor-only Pass 与固定关键字剔除仍可能执行。因此 `Enabled=false` 不能被解释为“所有裁剪关闭”。后续应拆分独立策略开关或引入 Disabled/AuditOnly/Conservative/Aggressive 模式。

### 5.2 材质证据不是完整运行时白名单

当前证据主要来自静态材质 `enabledKeywords`。运行时动态 `EnableKeyword`、动态创建材质、Timeline/RendererFeature 写入、ShaderVariantCollection 和平台关键字未被完整覆盖。默认 `KeepAll` 是当前较安全的回退。

### 5.3 Collector 路径与实际 BuildMap 仍有差距

当前采集器自行扫描 Package/Scene/目录路径。已观察到 Profile 中有历史场景路径与实际文件不一致，DefaultPackage 中还存在文件型 Collector；仅按有效目录扫描会漏掉文件型 Collector、嵌入式材质和依赖闭包。应改为读取实际 BuildMap，并保留额外根目录作为显式补充。

### 5.4 可能出现零变体但没有硬保护

非 Editor Pass 被错误剔除后，Unity 可能仍允许构建完成，但运行时可能出现粉材质或特定功能缺失。除显式全量剔除外，应增加“可运行 Pass 不得全为 0”的构建错误/保守回退。

### 5.5 初始化与处理器顺序

`_inited` 在采集完成前设置，采集异常可能留下半初始化状态。当前 TEngine、NB 和调试裁剪器存在 `callbackOrder == 0` 的情况，执行顺序不应视为稳定业务契约。应在成功初始化后再置位，并明确各裁剪器的职责和顺序。

## 6. ProjectACG 推荐优化顺序

### P0

1. 修复 `Enabled` 的总开关语义，或拆分为 Editor Pass、关键字、局部组合和全量 Shader 四个开关。
2. 区分 `shader_feature_local` 与 `multi_compile_local`，禁止把全局可覆盖关键字混入材质签名。
3. 使用 YooAsset 实际 BuildMap，补齐文件型 Collector、嵌入式材质和依赖闭包。
4. 对非 Editor Pass 增加零变体保护；只有显式列入 `StripAllVariantsShaderNames` 才允许归零。
5. 构建开始前刷新 Profile、材质证据和签名缓存，修复半初始化风险。
6. 为 TEngine/NB/调试裁剪器分配可解释的 `callbackOrder`，并记录实际启用策略。

### P1

1. 建立运行时动态关键字白名单，覆盖脚本、Timeline、RendererFeature、质量和平台路径。
2. 缓存按 Package、BuildTarget、Profile 版本和输入指纹隔离。
3. 规则优先使用 Shader 路径/GUID，名称作为可读别名。
4. 将 ExtraStrip 关键字改为 Shader/Pass/平台级配置。
5. 将全量剔除与“只保留空 local 组合”分成独立策略。

### P2

1. 构建前生成材质证据缓存，`OnProcessShader` 只读缓存。
2. 缓存 Variant 关键字和签名，减少 AssetDatabase、排序和临时分配。
3. 生成 JSON/CSV 裁剪报告，包含 Shader、Pass、Before、After、Removed、原因和示例材质。
4. CI 检查零变体 Shader、Shader bundle 体积、构建警告和关键包体回归。
5. 默认关闭 `ShaderVariantLogTracker` 文件输出，仅诊断构建开启。

## 7. 验证记录与边界

### 已完成

- `dotnet build TEngine.Editor.csproj -nologo --no-restore -p:BuildProjectReferences=false`：0 错误，项目原有警告。
- `dotnet build Assembly-CSharp-Editor.csproj -nologo --no-restore -p:BuildProjectReferences=false`：0 错误。
- 新增 Profile 字段、裁剪入口和序列化 YAML 可静态解析。
- 文档 UTF-8 round-trip、相对链接检查和 `git diff --check` 通过。

### 未完成

- 未在 Unity Editor 中实际执行 YooAsset 构建。
- 未确认 `unityshaders.bundle` 的体积变化和每个 Pass 的最终变体计数。
- 未完成 Frame Debugger、Player、动态 AssetBundle 加载和真机材质回归。
- 未验证 Shader fallback、运行时动态关键字和其他 `IPreprocessShaders` 的最终调用顺序。

因此当前结论是：**全量剔除功能的实现路径已具备，Lit 是否可安全归零仍未通过生产包验证；在 8 个 Lit 材质引用未解释前，不建议启用 Lit 全量剔除。**
