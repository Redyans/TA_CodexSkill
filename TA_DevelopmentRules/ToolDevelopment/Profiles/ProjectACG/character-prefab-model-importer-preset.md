---
name: projectacg-character-prefab-model-importer-preset-profile
description: ProjectACG CharacterPrefabBuilder 的 ModelImporter 预设、外置 Mesh/Prefab 生成、双工程同步、验证证据和当前限制 Profile。
---

# ProjectACG CharacterPrefabBuilder ModelImporter 预设 Profile

> 类型：PROFILE；适用范围：ProjectACG `CharacterPrefabBuilder` 的角色 FBX 生成流程。通用实现方式和排查矩阵见 [Unity ModelImporter 预设与安全 Mesh/Prefab 生成参考](../../references/model-importer-preset-and-safe-mesh-generation.md)；Prefab 模块快照仍见 [Character Prefab 模块快照与恢复 Profile](character-prefab-module-snapshot-and-restore.md)。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 工具目录 | `Assets/Editor/TA_Tools/Character/CharacterPrefabBuilder/`。 |
| 角色生成窗口 | `CharacterFbxPrefabBuilderWindow.cs`。 |
| 配置资产类型 | `CharacterPrefabBuildPipelineConfig`。 |
| 配置资产菜单 | `TA_Tools/角色Prefab构建流程配置`。 |
| 生成窗口菜单 | `TA_Tools/Character/角色FBX生成Prefab`。 |
| Assets 菜单 | `Assets/Prefab生成/重新生成Prefab`、`Assets/Prefab生成/更新已有Prefab`。 |
| Editor 程序集 | `TA_Tools.CharacterPrefabBuilder.Editor`。 |
| 测试程序集 | `TA_Tools.CharacterPrefabBuilder.Editor.Tests`。 |
| 不在本 Profile 范围 | `Assets/Editor/TA_Tools/TA/MMD`；本次同步和规则总结均不处理该目录。 |

本 Profile 的路径和菜单只适用于 ProjectACG 及其同源 Client checkout。迁移到其他工程时，先重新确认 Unity 版本、程序集依赖、配置资产和实际菜单，不要把这些项目事实当成 CORE。

## 2. 本次功能契约

每个 `SourceType = ModelFbx` 的流程步骤新增一个折叠区：

> `Model 导入设置预设（生成 Mesh / Prefab 时临时应用）`

预设默认关闭；开启后仍需至少勾选一项覆盖字段，且 `GenerateMeshAndMaterialAssets` 必须开启。未勾选字段继续继承源 FBX 的原始导入值。

### 2.1 覆盖字段

当前配置共 26 个公开 `ModelImporter` 项目，分为：

| 分组 | 项目 |
| --- | --- |
| Scene | `globalScale`、`useFileScale`、`bakeAxisConversion`、`importBlendShapes`、`importBlendShapeDeformPercent`、`importVisibility`、`importCameras`、`importLights`、`preserveHierarchy`、`sortHierarchyByName` |
| Meshes | `meshCompression`、`isReadable`、`optimizeMeshPolygons`/`optimizeMeshVertices`、`addCollider` |
| Geometry | `keepQuads`、`weldVertices`、`indexFormat`、`importBlendShapeNormals`、`importNormals`、`normalCalculationMode`、`normalSmoothingSource`、`normalSmoothingAngle`、`importTangents`、`swapUVChannels`、`generateSecondaryUV`、`strictVertexDataChecks` |

Inspector 提供：

- `套用截图示例并勾选全部`：写入当前工具定义的示例值并勾选 26 项；
- `清空覆盖勾选`：保留预设值但关闭所有覆盖，恢复为“继承源 FBX”；
- 旧字段迁移：将隐藏兼容字段 `SetBlendShapeNormalsToNoneDuringMeshGeneration` 迁移为 `BlendShapeNormals = None` 的显式覆盖。

示例按钮当前值为：Scale `1`、Convert Units 开启、Bake Axis 关闭、BlendShapes/Deform/Visibility/Cameras/Lights 开启、Preserve Hierarchy 关闭、Sort Hierarchy 开启；Mesh Compression 为 `Off`、Read/Write 关闭、Optimize Mesh 为 `Everything`、Collider 关闭；Keep Quads 关闭、Weld 开启、Index Format 为 `Auto`、Blend Shape Normals 为 `None`、Normals 为 `Import`、Normals Mode 为 `AreaAndAngleWeighted`、Smoothness Source 为 `PreferSmoothingGroups`、Smoothing Angle 为 `60`、Tangents 为 `CalculateMikk`、Swap UV/Lightmap UV/Strict Checks 关闭。

`Legacy Blend Shape Normals` 没有写入：Unity `2022.3` 没有稳定公开属性，因此不依赖内部序列化字段。

## 3. 实现方式与关键代码路径

### 3.1 配置与策略

- `CharacterModelImporterPresetConfig` 保存 `Enabled`、每个 `OverrideX` 和对应值。
- `CharacterPrefabModelImportPolicy.ShouldApplyModelImporterPreset()` 将来源类型、外置 Mesh 开关、总开关和“至少一个覆盖”集中为生效闸门。
- `ResolveOptimizeMesh()` 将配置枚举转换为 Unity 的两个优化布尔值。
- 旧 `SetBlendShapeNormalsToNoneDuringMeshGeneration` 使用 `[HideInInspector]` 保留序列化兼容，不让历史资产直接丢字段。

### 3.2 原值快照和临时应用

`CharacterModelImporterSettingsSnapshot` 捕获完整公开设置。`CharacterFbxPrefabBuilderWindow` 的 `BuildSession.OriginalModelImporterSettings` 以规范化 FBX 路径为键，确保同一构建会话只捕获一次。

执行路径为：

1. `BuildStepRecursive()` 进入 `ModelFbx` 步骤；
2. `EnsureSourceModelImporterSettingsForStep()` 对主 FBX 和可选 LOD1 FBX 使用同一预设；
3. `EnsureModelImporterSettings()` 从原始快照计算目标值，仅在发生差异时 `SaveAndReimport()`；
4. 生成外置 Mesh/Material/Prefab；
5. `BuildFrom...` 的外层 `finally` 调用 `RestoreTemporarySourceModelImporterSettings()`；
6. 恢复失败逐项记录，全部尝试后汇总抛出，而不是只恢复第一个文件或静默吞错。

平滑法线或坐标轴重建需要顶点读取时，步骤会临时要求 `Read/Write = true`；恢复阶段仍按原快照回写。

### 3.3 为什么要求外置 Mesh

预设只在源 FBX 导入期间生效。若生成 Prefab 仍引用 FBX 内部 Mesh，结束时恢复 FBX 原导入设置会让内部 Mesh 重新生成，导致预设的法线、切线、焊接、优化或坐标结果不再可靠。因此当前规则把“启用 Model 预设”与“生成外置 Mesh”绑定，而不是允许只改 FBX 后继续引用内部子资产。

## 4. 两工程同步经验

本次明确的同步对象是以下两个 checkout 的 `CharacterPrefabBuilder`：

- `D:\work2025U3D\Valkyria\ProjectACG\Client`
- `D:\work2025U3D\Valkyria\ProjectACGMain\ProjectACG\Client`

同步时采用以下边界：

1. 先列出两个目录的文件清单和 Git 状态，识别用户已有脏改；
2. 只同步工具实际改动的配置、Editor、支持逻辑、README 和测试文件，不用 `git add .` 或整目录覆盖；
3. 比较时统一换行后对全部文件计算内容哈希，不能只比较 Git diff；
4. 同步后检查两边目录文件数量、规范化内容和 `git diff --check`；
5. 单独扫描 `using TEngine`、`TEngine.Log`，确保工具使用 Unity 日志；
6. 单独验证 `TA/MMD` 没有状态变化；
7. 后续某一工程新增其他功能时，不要把“目录曾经一致”当作永久事实，应重新做分支级比较。

这一流程解决了“两个工程分别改过，合并时无法判断哪边更新”的根因：把同步对象、排除范围、内容比较算法和验证证据固定下来，而不是凭文件时间或肉眼合并。

## 5. 规则、问题与解决方式

| 问题/风险 | 根因 | 当前解决方式 |
| --- | --- | --- |
| 只想改一个 Model 项却重置其他导入项 | 预设默认值被无条件写入 | 每项独立 `OverrideX`；未勾选项从原始快照取值 |
| 多步骤构建互相污染导入设置 | 下一步骤读取了上一步临时值 | 会话内按路径缓存第一次快照，每个步骤都从快照重算 |
| 生成后法线/几何效果消失 | Prefab 继续引用 FBX 内部 Mesh，恢复 FBX 后内部子资产重新导入 | 启用预设时要求外置 Mesh/Material 生成 |
| 平滑法线处理报 Mesh 不可读 | 源 FBX `Read/Write` 关闭 | 处理阶段临时强制开启，最终按快照恢复 |
| 异常后 FBX 保持临时设置 | 恢复逻辑只写在正常返回路径 | 外层 `finally` 恢复全部快照，单项失败最后汇总 |
| 两边工具功能漂移 | 修改只在某个工程完成 | 明确同步文件、排除 MMD、规范化哈希和双边编译检查 |
| 试图用内部字段支持 Legacy 选项 | Unity 版本没有稳定公开属性 | 不写内部序列化键，记录为明确不支持项 |
| 工具误进入 Player | Editor 脚本引用被普通程序集自动收集 | Editor 目录 + `includePlatforms: ["Editor"]`，无 Runtime 依赖 |
| 仅 CLI 编译通过但实际导入结果未知 | 没有执行 Unity 导入和真实资产生成 | 把 CLI 作为补充；增加 Unity UI、临时 FBX E2E、恢复和 Player 边界验证 |

## 6. 当前验证证据与未验证边界

### 6.1 已完成

- ModelImporter 预设对应的 7 个源码/文档/测试文件在本次同步时逐文件核对，统一换行后内容一致；该结论只代表当次同步，不代表后续其他功能修改后两个 checkout 永久一致。
- 两边目标目录 `git diff --check` 通过；仅有 Git 的 LF→CRLF 提示。
- `TA/MMD` 无状态变化。
- Editor 与 Editor Tests 程序集均为 Editor-only；无 `using TEngine` 或 `TEngine.Log`。
- `TA_Tools.CharacterPrefabBuilder.Editor.csproj` 和 `TA_Tools.CharacterPrefabBuilder.Editor.Tests.csproj` 编译为 `0 warning / 0 error`。
- ProjectACGMain 的 Unity `2022.3.62f3` Inspector 已确认预设折叠栏、总开关、示例/清空按钮、Scene/Meshes/Geometry 字段正常显示。
- 直接相关策略测试：`CharacterPrefabModelImportPolicyTests` `8/8`，`CharacterProbModelConventionTests` `12/12`。

### 6.2 未完成

- 未使用生产角色执行真实 Mesh/Prefab 生成，尚未证明最终外置 Mesh 的法线/几何结果与截图预设完全一致。
- 尚未在临时复制 FBX 上完成主模型 + LOD1、异常恢复、取消恢复和恢复后重新打开的完整矩阵。
- 工程 A 曾被无关的 `ModelImportManualSettings.cs` 编译错误阻止有效 Domain Reload；该错误不属于本工具，本 Profile 不给出修复结论。
- 整个 CharacterPrefabBuilder 测试程序集中的 ModuleSnapshot 既有失败仍需单独维护；不能把这次 20 项直接相关测试通过扩展为整个程序集全部通过。

## 7. 维护检查单

- [ ] 修改预设字段前确认序列化兼容和默认关闭行为。
- [ ] 新增 `ModelImporter` 项前确认目标 Unity 版本有稳定公开 API。
- [ ] 未勾选项继续使用源 FBX 快照，不使用配置默认值代替。
- [ ] 主 FBX、LOD1 和重复步骤不会共享临时值作为下一步原值。
- [ ] 预设生效时外置 Mesh 生成开关仍是硬条件。
- [ ] 所有 `SaveAndReimport()` 都有异常路径恢复和可定位日志。
- [ ] `Read/Write` 临时强制开启后最终恢复。
- [ ] 不新增全局导入 Hook，除非先证明局部入口不足并完成高影响验证。
- [ ] 两个 checkout 重新比较规范化内容、Git 状态和排除目录；不覆盖用户脏改。
- [ ] Editor asmdef、Player 编译、Unity Inspector 和临时 FBX E2E 均按变更风险重新验证。
- [ ] 源码旁 `README_Art`、`README_Tech` 和配置指南同步更新；规则文档只记录可迁移经验和已证实边界。
