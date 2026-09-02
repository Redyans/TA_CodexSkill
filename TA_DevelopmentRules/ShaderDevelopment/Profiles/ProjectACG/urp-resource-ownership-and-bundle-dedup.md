---
name: projectacg-urp-resource-ownership-and-bundle-dedup
description: ProjectACG 中 URP Pipeline/Renderer 与 YooAsset 资源收集链导致 UberPost 重复实例的根因、修复实现和验证边界。
---

# ProjectACG URP 资源所有权与 `UberPost` 去重 Profile

> **Profile ID**：`projectacg-urp-resource-ownership-and-bundle-dedup-v1`
>
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACGMain3\ProjectACG\Client`
>
> **事实快照**：`2026-09-02`
>
> **用途**：记录本工程中 `Hidden/Universal Render Pipeline/UberPost` 重复现象的资源链、修复边界、实现文件、验证结果和未闭环风险。本文是 ProjectACG `PROFILE`，不得上升为通用 CORE；跨项目方法见 [`../../references/urp-shader-resource-ownership-and-bundle-dedup.md`](../../references/urp-shader-resource-ownership-and-bundle-dedup.md)。

## 1. 工程基线与根因

| 项目 | 当前事实 |
| --- | --- |
| Unity | `2022.3.62f3` |
| URP | embedded `com.unity.render-pipelines.universal@14.0.12` |
| URP 源资源 | `Assets/AssetRaw/Settings/URP-*.asset` 及匹配 Renderer 资产 |
| Player 配置入口 | `ProjectSettings/GraphicsSettings.asset`、`ProjectSettings/QualitySettings.asset` |
| YooAsset 配置 | `Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset` |
| 运行时质量入口 | `QualityModule.ApplyForScene → URPQualitySwitcher.Switch → PerformanceSettingApplier.Apply` |

原问题不是 `UberPost` 源文件必然存在多份，而是存在两条资源持有链：

```text
ProjectSettings → Player URP Pipeline/Renderer → URP 后处理 Shader
YooAsset Settings collector + AutoCollectShaders
    → defaultpackage_unityshaders.bundle
运行时 URPQualitySwitcher → YooAsset LoadAsset<UniversalRenderPipelineAsset>
```

因此同一逻辑 URP 后处理依赖可能同时被 Player 和 YooAsset 实例化。多个 URP 资产引用同一个 `PostProcessData` GUID 并不是单独的重复根因。

## 2. 当前工程规则

### PACG-URP-OWN-01｜URP Pipeline 运行时唯一所有者

- **必须（MUST）**：运行时质量切换只使用 Player 持有的 URP Pipeline/Renderer 实例。
- **应当（SHOULD）**：通过 `Assets/Resources/QualitySettings.asset` 维护场景/质量到 Pipeline 的显式映射。
- **禁止（MUST NOT）**：在同一运行时链路中再调用 YooAsset 加载 `UniversalRenderPipelineAsset` 作为备用或 fallback。
- **验证**：搜索 QualityModule 及其消费者，不应再出现运行时 `LoadAsset<UniversalRenderPipelineAsset>`；注册表覆盖 Lobby/Battle 的 8 个组合。
- **回退**：若 Player 映射缺失，记录错误并拒绝切换；不要回退到 YooAsset 或当前管线猜测值。

### PACG-URP-OWN-02｜Settings 收集器精确排除 URP

- **必须（MUST）**：`Assets/AssetRaw/Settings` 保持原目录收集范围，但使用 `CollectNonUrpSettings` 排除 URP Pipeline/RendererData。
- **应当（SHOULD）**：保留 `CharaRenderSettings` 等普通运行时设置进入 YooAsset。
- **禁止（MUST NOT）**：为了排除 URP 把 collector 缩成 `Settings/CharaRenderSettings`，也不得全局关闭 `AutoCollectShaders`。
- **验证**：当前静态盘点为 19 个 URP 根资产被排除、7 个普通角色设置保留；过滤规则同时按 `URP-` 名称和 `RenderPipelineAsset`/`ScriptableRendererData` 类型判断。
- **回退**：恢复原 `CollectAll` 前，必须同时恢复运行时加载链；否则会重新形成双持有或切换失败。

### PACG-URP-OWN-03｜Player 映射是资源接口

- **必须（MUST）**：修改或新增质量档 URP 资产时，同步更新 `QualitySettings.asset` 映射。
- **应当（SHOULD）**：保持原资源路径和 `.meta` GUID，不做无必要迁移。
- **禁止（MUST NOT）**：把资源名拼接、裸地址或 YooAsset 地址作为运行时唯一真相源。
- **验证**：`QualitySettings.asset` 当前包含 Lobby/Battle × Low/Mid/High/Ultra 共 8 条唯一映射，所有 Pipeline GUID 均能在 `Assets/AssetRaw/Settings` 找到。
- **回退**：映射缺失时保留当前管线，不加载另一份同名 URP 资产，并输出可定位错误。

### PACG-URP-OWN-04｜源资产、Player 依赖与 Bundle 产物分层

- **必须（MUST）**：源 URP 资产继续留在 `Assets/AssetRaw/Settings`，通过 Resources 映射进入 Player；YooAsset 不再主动持有这批 Pipeline/Renderer 根资产。
- **应当（SHOULD）**：把普通设置、Shader 内容资源和 URP 管线配置分成可审计的收集边界。
- **禁止（MUST NOT）**：把旧 `Bundles/.../Simulate` 文件当作修复后证据，或只重建 Shader bundle 而复用旧 manifest。
- **验证**：变更后执行 `-ForceRebuildAssets -ForceRebuildBundles`，检查新 manifest、`defaultpackage_unityshaders.bundle` 和 Player 运行时对象来源。
- **回退**：发布前保留旧版本 Bundle 的可回滚版本；不要在未确认缓存引用时直接删除线上资源。

## 3. 实际实现文件与职责

| 文件 | 本次职责 |
| --- | --- |
| `Assets/GameScripts/HotFix/GameLogic/Module/QualityModule/Runtime/URPQualitySwitcher.cs` | 删除 YooAsset/AssetDatabase 运行时加载和当前管线 fallback，只读 `QualityAssetRegistry` |
| `Assets/GameScripts/HotFix/GameLogic/Module/QualityModule/Data/QualitySettingsAsset.cs` | 定义 Player 侧 `PipelineMapping` ScriptableObject |
| `Assets/GameScripts/HotFix/GameLogic/Module/QualityModule/Runtime/QualityAssetRegistry.cs` | 从 `Resources/QualitySettings` 缓存并返回 Pipeline 引用 |
| `Assets/Resources/QualitySettings.asset` / `.meta` | 保存 8 档映射；保留 Pipeline 源资产 GUID |
| `Assets/Editor/AssetBundleCollector/ProjectSceneCollectRules.cs` | 实现 `CollectNonUrpSettings`，不修改 YooAsset 包源码 |
| `Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset` | Settings collector 恢复到 `Assets/AssetRaw/Settings`，过滤规则改为 `CollectNonUrpSettings` |
| `Assets/AssetRaw/Settings/README.md` | 记录源资源、Player 所有权和全量重建要求 |
| `Assets/GameScripts/HotFix/GameLogic/Module/QualityModule/README.md` | 记录运行时映射入口、8 档覆盖和缺失映射行为 |

这次没有修改 Shader 源码、URP Package、`AutoCollectShaders` 全局开关、`Always Included Shaders`、材质/Prefab/Scene 或用户已有 Bundle 文件。

## 4. 已确认的静态证据

- `CollectNonUrpSettings` 恢复宽目录并按类型/名称排除 URP；19 个 `URP-*.asset` 根资源不进入 Settings collector，7 个 `CharaRenderSettings` 仍保留。
- `QualitySettings.asset` 有 8 条映射且键唯一，所有 GUID 均存在。
- `URPQualitySwitcher` 不再直接调用 YooAsset 或 AssetDatabase 加载 URP Pipeline。
- `GameLogic.csproj`：`0 errors`，144 个既有 warnings。
- `Assembly-CSharp-Editor.csproj`：`0 errors`，86 个既有 warnings。
- Unity asset/GUID 一致性检查通过；仓库 Harness 的失败项来自已有规则入口、文档链接和无关 UI 脏改，不是本 Profile 新增内容。

## 5. 必须补做的 Unity/Player 验证

以下内容不能用静态编译替代：

1. 用干净的 YooAsset 构建执行 `-ForceRebuildAssets -ForceRebuildBundles`。
2. 检查新 manifest 中不再把 URP Pipeline/Renderer 作为 Settings Bundle 主资源，确认 shader bundle 来源符合预期。
3. 构建 Development Player，进入 Lobby/Battle 并切换 Low/Mid/High/Ultra。
4. 用 Memory Profiler 搜索 `Hidden/Universal Render Pipeline/UberPost`，确认稳态实例数和来源；不要把编辑器缓存或旧 Bundle 计入结果。
5. 用 Frame Debugger/截图确认 Bloom、FinalPost、TAA、UI 和普通材质没有粉色、缺失或时序回归。
6. 验证旧 Bundle 句柄可释放、场景切换后没有旧 Pipeline/Renderer 残留。

在这些步骤完成前，结论只能写为“资源所有权和代码路径已修正，运行时去重待 Player 证据确认”。

## 6. 维护与回退清单

新增质量档或替换 URP 资产时：

- 更新 `Assets/Resources/QualitySettings.asset`；
- 检查 `ProjectSettings` 当前默认管线是否仍为目标 Player 资产；
- 确认 `Settings` collector 仍使用 `CollectNonUrpSettings`；
- 重建 AssetBundle/manifest，清理或失效旧缓存；
- 重新执行 8 档切换、后处理视觉和内存检查。

若出现切换失败，优先检查 Resources 映射和 Player 构建依赖；若出现 Shader 数量仍异常，优先检查是否运行了旧 Bundle、是否有第二个运行时加载器，以及 Profiler 统计的是变体还是 Shader 实例。不要先删除 `UberPost`、URP 源资产或全局关闭 Shader 收集。

