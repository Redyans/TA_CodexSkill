# Shader 变体裁剪、全量剔除与构建安全参考

> **文档类型**：REFERENCE
> **适用范围**：Unity Editor `IPreprocessShaders`、URP Shader 变体、YooAsset/AssetBundle 构建链
> **使用前提**：必须以目标工程当前 Unity/URP 版本、Shader 源码、材质/Prefab/Scene 依赖和实际构建结果重新验证；本文不替代项目 Profile。
>
> **相关参考**：SVC 白名单的收集、`PassType` 兼容和 Inspector 维护细节见 [`shader-variant-allowlist-stripping.md`](shader-variant-allowlist-stripping.md)。

## 1. 结论先行

Shader 变体裁剪不是“删除一个 Shader 文件”，而是在 Unity 编译 Shader 变体时修改某个 Shader Pass 的 `IList<ShaderCompilerData>`。对每个回调批次执行 `data.Clear()`，可以让该批次最终保留 `0` 个变体；如果该 Shader 的所有 Pass 都命中，打包后的 Shader 内容就没有可编译变体。

当前实现适合拆成五层：

1. **Editor 专用 Pass 清理**：`Meta`、`ScenePickingPass`、`SceneSelectionPass` 等构建运行时不需要的 Pass 直接清空。
2. **关键字黑名单**：按关键字剔除 XR、Debug 或工程确认不用的变体。
3. **本地关键字组合裁剪**：从静态材质的 `enabledKeywords` 收集证据，只保留出现过的组合；这是白名单裁剪，不是完整的运行时安全证明。
4. **ShaderVariantCollection 白名单**：对显式配置的 Shader 按 Pass 类型和完整关键字集合精确匹配，只保留集合内变体。
5. **Shader 全量剔除**：Profile 显式列出 Shader 名称，命中后不再判断 Pass 或关键字，直接清空每个 Pass 的变体列表。

全量剔除应当被视为**强制归零开关**，只适用于已经证明不会进入目标 Player/AssetBundle 的 Shader。它不能解决运行时动态 `EnableKeyword`、动态创建材质、Fallback Shader 或错误 BuildMap 收集等问题；配置错误会直接导致粉材质、Shader 加载失败或特定 Pass 缺失。

## 2. Unity 构建执行链

```text
BuildPipeline / YooAsset ScriptableBuildPipeline
    -> Unity Shader 编译器枚举 Shader + Pass + 平台变体
    -> 每个 ShaderSnippetData 回调 IPreprocessShaders.OnProcessShader
    -> 裁剪器原地删除 IList<ShaderCompilerData>
    -> Unity 编译剩余列表
    -> 内置 Shader bundle / AssetBundle
```

### 2.1 回调粒度

- `OnProcessShader` 不是每个 Shader 只调用一次，而是按 Shader 的 Pass、阶段和构建上下文分批调用。
- `snippet.passName`、`snippet.passType`、`shader.name` 和 `AssetDatabase.GetAssetPath(shader)` 是判断作用域的主要输入。
- `data.Clear()` 只清空当前回调批次，因此“全量剔除”必须在每次回调都命中；不能只清空一次或只处理 Forward Pass。
- `Shader` 资源本身仍可能存在于工程或 bundle 元数据中；“没有变体”不等同于“引用该 Shader 一定安全”。运行时找不到可执行程序时可能报错，也可能根据 ShaderLab 的 fallback 行为表现为其他结果，必须实机确认。

### 2.2 裁剪顺序

推荐把强制规则放在普通关键字和组合裁剪之前：

```text
EnsureInited()
  -> 命中全量剔除列表？是：记录原因、data.Clear()、return
  -> Editor-only Pass？是：记录原因、data.Clear()、return
  -> 计算材质证据和局部关键字维度
  -> 命中 ShaderVariantCollection 白名单？否：删除该变体
  -> 关键字黑名单
  -> 本地组合白名单
```

这样做有三个好处：

- 不会为已经要归零的 Shader 扫描材质、计算签名或访问 Terrain 映射。
- 记录的原因明确为 `ShaderConfiguredFullStrip`，不与关键字误删混淆。
- 每个 Pass 都走同一条路径，避免只剔除主 Pass、遗漏 ShadowCaster 或 Meta。

## 3. 全量剔除的推荐实现模式

### 3.1 配置契约

使用精确的 `Shader.name`，不要使用文件名、显示名或模糊包含匹配：

```yaml
StripAllVariantsShaderNames:
- Universal Render Pipeline/Lit
```

推荐契约：

- 列表默认空，不默认剔除任何 Shader。
- 读取 Profile 时去除首尾空格，使用 `HashSet<string>(StringComparer.Ordinal)` 去重。
- `Profile.Enabled == false` 时不启用全量剔除列表。
- 只允许精确匹配；如果需要按路径或 GUID 识别，应另建字段，不能把名称匹配悄悄升级成模糊匹配。
- 配置变更后必须重新启动或显式刷新裁剪器缓存，避免静态缓存继续使用旧列表。

### 3.2 回调实现

核心逻辑应保持简单、可审计：

```csharp
if (_stripAllVariantsShaderNames.Contains(shader.name))
{
    RecordStripStats(shader.name, data.Count, 0);
    LogStripReason(shader, snippet, "ShaderConfiguredFullStrip");
    data.Clear();
    return;
}
```

实际项目可以保留逐变体日志，但必须注意：

- 日志只用于构建诊断，不要在每次普通构建默认写入大量文件。
- 统计应记录 Before、Removed、After，并按 Shader 名称聚合。
- 不能在 `data.Clear()` 后再尝试读取变体关键字来生成原因。
- 全量剔除分支应早于可能抛异常的材质收集、ShaderGraph 关键字投影和 Terrain Shader 映射。

### 3.3 ShaderVariantCollection 白名单

当材质组合不足以表达实际需求时，可以用 `ShaderVariantCollection` 作为某个 Shader 的人工审核白名单。典型实现要点：

- Profile 通过 `EnableShaderVariantCollectionAllowList` 显式开启，并指定项目相对路径。
- 可选填写 `ShaderVariantCollectionAllowListShaderNames`；为空时使用集合内所有 Shader，否则只治理列出的精确 `Shader.name`。
- 每个变体同时匹配 `passType` 和排序后的完整关键字集合；关键字顺序差异不应影响匹配。
- 集合配置错误、路径不合法、条目为空或指定 Shader 缺少条目时，应记录错误并关闭白名单，而不是把所有变体静默剔除。
- 被 SVC 白名单治理的 Shader 不应再叠加更宽泛的局部组合或 ExtraStrip 规则，否则人工收集的变体可能被第二套规则误删；仅保留明确的 AlwaysStrip 覆盖规则。
- 全量剔除列表优先级高于 SVC 白名单；同一 Shader 同时出现在两者时，结果仍是 0 个变体。

SVC 白名单适合“需要保留的少量已审核组合”，不适合代替全工程材质/运行时证据，也不适合未经构建和运行时验证就批量生成。

### 3.4 为什么不把 Lit 默认加入列表

URP Lit 通常是工程级通用 Shader。启用全量剔除前，至少要完成：

1. 扫描材质、Prefab、Scene、模型嵌入材质、Timeline/脚本动态材质和 AssetBundle BuildMap。
2. 确认所有使用者已经迁移到项目 Shader，或该内容不会进入目标 Player。
3. 构建目标平台的 Shader bundle，确认 Lit 的每个 Pass 变体数量确实为 `0`。
4. 用代表性场景和动态加载路径运行，确认没有粉材质、Shader 编译错误、Fallback 误用或材质回退。

只要仍有一个运行时材质引用 Lit，就不应把它当作“安全剔除”。

## 4. 证据采集：从“看起来没用”到“可以归零”

### 4.1 静态引用检查

最小检查顺序：

1. 读取 Shader 的真实 `shader.name` 和资产路径。
2. 通过 Shader `.meta` 的 GUID 搜索 `.mat`、`.prefab`、`.unity`、`.asset`。
3. 扫描材质的 `m_Shader` 引用，同时检查模型或 Prefab 中的嵌入式材质。
4. 检查 Scene 依赖、Prefab 依赖和 Addressables/YooAsset 收集路径。
5. 检查脚本中的 `Shader.Find`、`Resources.Load`、材质运行时创建和 `EnableKeyword`。

PowerShell 示例（路径和 GUID 必须替换为当前工程值）：

```powershell
$guid = '<Shader .meta 中的 GUID>'
rg -l -F "guid: $guid" Assets --glob '*.mat' --glob '*.prefab' --glob '*.unity' --glob '*.asset'
rg -n 'Shader\.Find|EnableKeyword|DisableKeyword|new Material|CreateAsset' Assets --glob '*.cs'
```

静态搜索只能证明“发现了引用”，不能证明“运行时绝不会走到该引用”。

### 4.2 采集范围必须接近真实构建

材质白名单或全量剔除的证据来源优先级：

1. 实际构建使用的 YooAsset BuildMap/Collector 结果。
2. 构建目标 Scene、Prefab、Addressable/资源清单的依赖闭包。
3. 明确维护的额外材质根目录。
4. 仅用于诊断的全工程 `FindAssets` 扫描。

只扫描某些文件夹会漏掉：

- 文件型 Collector（直接收集某个 `.prefab`、`.shader` 或 `.fbx`）。
- 模型、Prefab、Scene 内嵌的 Material。
- Collector 根目录以外的依赖材质。
- 构建脚本临时添加的资源和运行时动态下载内容。

因此，审计工具的“Shader 使用清单”与实际 YooAsset BuildMap 不一致时，不能据此安全归零。

### 4.3 运行时关键字证据

静态材质的 `Material.enabledKeywords` 只能覆盖已保存状态。必须额外检查：

- `shader_feature_local`：通常可以按材质组合裁剪，但动态启用的新组合需要白名单或保留策略。
- `multi_compile_local`：虽然是 local 作用域，仍然会生成组合；不能因为名字带 local 就默认安全。
- `multi_compile` 和管线关键字：可能由 URP、Renderer、质量设置、光照、阴影、Forward+、SSAO 或平台决定，不能当作材质局部组合直接投影。
- `ShaderVariantCollection`：可能额外请求静态扫描未发现的变体；启用白名单时，未收集项会被明确剔除，集合本身也可能过期。
- 运行时 `Material.EnableKeyword`、`DisableKeyword`、`Shader.EnableKeyword` 和动态材质创建。

## 5. 常见问题、根因和解决方式

### 5.1 “Enabled=false 了，为什么仍然在剔除？”

很多实现只用 `Enabled` 控制材质组合裁剪，却在函数前面无条件清空 Meta、Picking 或 Selection Pass，或者继续应用固定关键字黑名单。应将 `Enabled` 定义为真正的总开关，或明确拆成 `EditorPassStripEnabled`、`KeywordStripEnabled`、`LocalComboStripEnabled`、`FullShaderStripEnabled` 等独立模式，并在文档中说明每个开关的边界。

### 5.2 “局部关键字裁剪后变粉”

常见根因是把全局管线关键字混入材质组合签名，或只采集静态材质而运行时动态启用了新组合。解决方式：

- 根据 `LocalKeywordSpace` 区分可由材质控制的关键字和可覆盖的全局关键字。
- 先把材质关键字集合投影到当前 Pass 实际出现的 local 维度，再计算签名。
- 采集不到证据时默认 KeepAll，而不是默认只保留空组合。
- 为动态路径维护显式白名单或关闭组合裁剪。

### 5.3 “Package Shader 没被正确采集”

只用 `Assets/` 下的 `FindAssets` 无法代表 Package Shader 的材质使用情况；但把整个 `Packages/` 扫入又可能把示例、测试和编辑器材质错误纳入白名单。推荐使用实际 BuildMap，再对少数需要治理的 URP Shader 建立明确白名单和依赖映射。

### 5.4 “配置了目录，实际资源没被采集”

目录采集器通常只处理有效文件夹。若 YooAsset Collector 的 `CollectPath` 是一个文件，或材质嵌入模型/Prefab，目录扫描会静默漏采。解决方式是同时支持文件型 Collector、依赖闭包和嵌入式材质，并对无效路径输出构建前警告。

### 5.5 “某个 Pass 变成 0，构建能过但运行时异常”

Unity 可能允许单个 Pass 或 Shader 的变体列表为空，但运行时是否需要该 Pass 取决于 Renderer、材质队列、阴影、深度、烘焙和平台。对于非 Editor Pass，建议提供硬保护：除非 Shader 命中显式全量剔除列表，否则剔除后不能让所有可运行 Pass 都为 0；发现 0 时在构建日志中报错或恢复保守集合。

### 5.6 “改了 Profile，但构建还用旧配置”

静态 `_inited`、签名缓存和 Shader local keyword 缓存会跨多次构建保留状态。构建开始前应清理或刷新缓存；初始化应在采集成功后再设置 `_inited = true`，异常时不能留下半初始化字典。

### 5.7 “ShaderVariantCollection 白名单把可用变体删掉”

SVC 不是“材质使用报告”，而是按 Pass 和完整关键字集合执行的严格白名单。常见根因是只收集了 Forward、漏掉 ShadowCaster/DepthOnly，或序列化的 `passType` 与 Unity 回调类型不一致。解决方式：逐 Pass 采集、规范化关键字顺序、对旧集合的 `PassType.Normal` 兼容只做受限回退，并在构建后检查白名单命中/未命中数量。

### 5.8 “多个裁剪器互相影响”

Unity 项目里可能同时存在 URP、TEngine、插件和调试工具的 `IPreprocessShaders`。`callbackOrder` 相同并不构成稳定的业务顺序。应明确：

- 哪个裁剪器负责管线通用关键字。
- 哪个裁剪器负责项目 Shader/材质证据。
- 哪个裁剪器负责 NB 等独立 Shader 家族。
- 哪个裁剪器负责诊断日志而不改变列表。

为关键裁剪器分配不同的 `callbackOrder`，并验证“先清空后删除”不会被其他处理器误判为异常。

## 6. 优化路线

### P0：先保证正确性

1. **真正的总开关或模式**：至少区分 Disabled、AuditOnly、Conservative、Aggressive；默认 Conservative。
2. **local 关键字分类**：明确 `shader_feature_local` 与 `multi_compile_local`，禁止把全局可覆盖关键字当材质维度。
3. **真实 BuildMap 采集**：以实际构建输入为 source of truth，补齐文件型 Collector、嵌入式材质和依赖闭包。
4. **非 Editor Pass 非零保护**：未显式全量剔除时，发现所有运行 Pass 变体为 0 就报错或回退。
5. **构建前刷新**：刷新 Profile、材质证据、签名和 Shader 关键字缓存；异常不得半初始化。
6. **裁剪顺序固定**：给自定义裁剪器设置明确 `callbackOrder`，并在构建日志中输出实际启用的策略。

### P1：降低运行时风险和维护成本

1. 建立动态关键字白名单，覆盖脚本、Timeline、RendererFeature、质量设置和平台差异。
2. 缓存按 Package、BuildTarget、Profile 版本和 Git 变更指纹隔离，避免 Android/iOS/Windows 互相污染。
3. 排除规则优先使用 Shader 路径/GUID，名称仅作为可读别名。
4. 把全局 ExtraStrip 关键字改成按 Shader/Pass/平台配置，避免一个关键字误伤所有家族。
5. 把“全量剔除”与“只保留空 local 组合”分成两个不同选项，防止误把局部策略当成 Shader 归零。

### P2：性能、审计和 CI

1. 构建前生成材质证据缓存，`OnProcessShader` 只读 HashSet，避免重复扫描 AssetDatabase。
2. 缓存每个 Variant 的关键字文本和签名，减少 `GetShaderKeywords()`、字符串排序和临时分配。
3. 输出 JSON/CSV：Shader、路径、Pass、Before、After、Removed、原因、示例材质和构建目标。
4. CI 检查变体数量、`unityshaders.bundle` 体积、零变体 Shader、缺失 fallback 和构建警告。
5. `ShaderVariantLogTracker` 等文件日志默认关闭，只在诊断构建开启，并限制单 Shader/单 Pass 日志量。

## 7. 推荐操作清单

### 7.1 新增或修改裁剪规则前

- [ ] 读取 Unity/URP 版本、ShaderStripping Profile、YooAsset Package 和构建入口。
- [ ] 确认目标 Shader 的真实名称、路径、GUID、Pass 列表和 Fallback。
- [ ] 列出静态材质、Prefab、Scene、模型嵌入材质和动态加载/关键字路径。
- [ ] 明确规则属于 Editor Pass、关键字、局部组合还是 Shader 全量剔除。
- [ ] 记录默认行为、启用条件、回退策略和误配后果。

### 7.2 启用全量剔除前

- [ ] 全量引用扫描没有未解释的运行时材质。
- [ ] 实际 BuildMap/Collector 已覆盖，不只是目录 `FindAssets`。
- [ ] 已确认该 Shader 不会被 `Shader.Find`、动态材质或运行时脚本使用。
- [ ] 目标平台构建后每个 Pass 的变体数量为 `0`。
- [ ] 代表性场景、AssetBundle 加载、材质替换和设备运行通过。
- [ ] 已保留一键回退：从列表删除 Shader 名称并清理构建缓存后重新打包。

### 7.3 发现异常时

1. 先从全量剔除列表删除目标 Shader，清理 Shader/AssetBundle 构建缓存。
2. 用 AuditOnly/KeepAll 模式确认问题是否由裁剪引入。
3. 对比构建前后的 Shader bundle、变体报告和日志原因。
4. 再缩小到具体 Pass、关键字或动态材质路径，不要直接扩大黑名单。

## 8. 可沉淀的规则候选（待跨项目验证）

以下内容适合作为后续 CORE/PROFILE 规则候选，不应在缺少更多工程证据时直接升级为强制规范：

- 全量 Shader 剔除必须使用显式、精确、默认关闭的列表；模糊匹配不得作为默认行为。
- `IPreprocessShaders` 规则必须说明回调粒度、Pass 覆盖范围、数据证据和运行时回退。
- 材质局部组合裁剪必须区分 local 与可覆盖的全局关键字；动态关键字路径必须有白名单或保守回退。
- 采集器必须以实际构建输入为准，并覆盖文件型 Collector、依赖材质和嵌入式材质。
- 非显式全量剔除的 Shader 不得静默得到 0 个可运行 Pass；零变体必须报错、回退或由 Profile 明确批准。
- 任何裁剪优化都要同时报告“构建体积收益”和“运行时变体缺失风险”，不能只看编译数量。
