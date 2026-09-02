---
name: projectacg-character-prefab-builder-generation-and-material-sync-profile
description: ProjectACG CharacterPrefabBuilder 的 FBX 生成、外置 Mesh、Prefab 更新、材质槽同步、LOD、导入预设、坐标重建和验证经验 Profile。
---

# ProjectACG CharacterPrefabBuilder 生成与材质槽同步 Profile

> 类型：`PROFILE`；适用范围：当前 ProjectACG `Client` 工作区的 `CharacterPrefabBuilder`。通用工具规则见 [TA 工具开发模块](../../README_Tech_TAToolDevelopmentRules.md)；通用批量生成、资产写入和验证方法见 [工具实现模式参考](../../references/tool-development-patterns.md)。本文件记录项目事实、已复现问题和已验证/待验证边界，不把项目路径和版本提升为 CORE。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 工具目录 | `Assets/Editor/TA_Tools/Character/CharacterPrefabBuilder/`。 |
| 生成窗口 | `CharacterFbxPrefabBuilderWindow.cs`。 |
| Pipeline 配置 | `PrefabBuildPipeline_hero.asset`、`PrefabBuildPipeline_monster.asset` 等。 |
| 生成程序集 | `TA_Tools.CharacterPrefabBuilder.Editor`，Editor-only。 |
| 测试程序集 | `TA_Tools.CharacterPrefabBuilder.Editor.Tests`，Editor-only。 |
| 典型输入 | `Assets/AssetRaw/character/<type>/<id>/model/*.fbx`。 |
| 典型输出 | 同角色目录下的 `mesh/*.mesh` 和 `prefab/*.prefab`。 |
| 相关功能文档 | 工具目录内的 `README_Art_CharacterPrefabBuilder.md`、`README_Tech_CharacterPrefabBuilder.md`、`CharacterPrefabBuilder_ConfigGuide.md`。 |

基础 pre 的 Assets 右键入口是 `TA_Tools/Character/重新生成角色 Prefab`；已有 Prefab 的增量入口是 `TA_Tools/Character/更新已有角色 Prefab`。两者都从当前选中的 FBX 解析 Pipeline，实际执行前会重新分析步骤、源文件、输出路径和警告。

## 2. 生成链路与职责边界

### 2.1 主链路

生成一个 Model FBX 步骤时，职责顺序固定为：

1. 从 `Selection.activeObject` 获取持久化 FBX，并通过 `AssetDatabase.GetAssetPath` 校验路径、类型、角色类型、编号和品质。
2. `Analyze()` 实例化 FBX，收集 `SkinnedMeshRenderer` / `MeshRenderer`，解析源 Mesh 名称、目标 Mesh 名称、材质槽数量和命名警告。
3. `ResolveBuildSteps()` 解析每个步骤的源 FBX、品质覆盖、输出目录、Prefab 名称、LOD 依赖和既有 Prefab 状态。
4. Model FBX 步骤先临时应用 `ModelImporter` 预设（如启用），再实例化源模型。
5. `BuildPrefabFromModelRefresh()` 或 `BuildPrefabFromModelAdditive()` 创建/更新目标层级；`UpdateGeneratedRenderers()` 生成外置 Mesh 并绑定 Renderer。
6. 可选执行 LOD、Prefab 模板、组件模板、角色层级策略、阴影和坐标轴重建。
7. `PrefabUtility.SaveAsPrefabAsset` 写回原输出路径，最后 `AssetDatabase.SaveAssets/Refresh` 并恢复临时导入设置。

窗口绘制只负责输入、状态和计划展示，不在 `OnGUI` 的 Layout/Repaint 阶段隐式写资产。生成异常必须显示失败步骤和异常摘要；导入设置恢复放在 `finally`，不能因为中途报错而把临时值永久写入 FBX `.meta`。

### 2.2 两种既有 Prefab 模式

- `RefreshFromTemplate`：重新实例化源 FBX 并按模板全量刷新，适合基础 `pre`。保存到同一路径时不删除 Prefab `.meta`，静态条件下可保持 GUID。
- `PreserveExistingAdditive`：加载已有 Prefab，按同名层级补充缺失节点并更新 Renderer，适合需要保留手工配置的 `cPre`。它不是通用三方合并器，不能替代模块快照。

Prefab 模块快照、组件/引用恢复和 Hierarchy Object 原子子树属于独立职责，详见 [Character Prefab 模块快照与恢复 Profile](character-prefab-module-snapshot-and-restore.md)。不要把“生成器更新模型”与“迁移用户配置”混成一个不可回放步骤。

## 3. Mesh、SubMesh 与材质槽契约

### 3.1 两个数量不是同一个数据

Unity 的 `Mesh.subMeshCount` 描述几何索引分段；`Renderer.sharedMaterials.Length` 描述 Renderer 材质槽数组。二者通常应能对应，但不能把其中一个当成另一个的替代品：

- `CreateOrUpdateMesh()` 复制源 Mesh 时会保留源 Mesh 的 SubMesh 结构。
- `ResolveMaterialSlotCount()` 只用于分析窗口的预览提示，不能代替实际写入 `sharedMaterials`。
- Prefab 序列化的 `m_Materials` 才是最终 Renderer 材质槽结果；只看到外置 `.mesh` 有多个 SubMesh，并不代表 Prefab 已经有相同数量的材质槽。

### 3.2 槽位数量权威与保留规则

当前项目要求：**最终材质槽数量以源 FBX 实例 Renderer 的 `sharedMaterials.Length` 为准；已有索引上的非空 Prefab 材质不得被替换。** `CharacterPrefabMaterialBindingPolicy.Resolve()` 的行为是：

| 情况 | 结果 |
| --- | --- |
| 旧数组不存在或没有任何有效材质 | 使用源 FBX 完整材质数组。 |
| 旧数组与源数组长度相同 | 保留旧数组中已有的非空材质；旧空槽由源对应索引补入。 |
| 源数组变长 | 旧数组重叠索引保留；新增尾部槽从源 FBX 对应索引补入。 |
| 源数组变短 | 保留重叠索引的旧材质；只截掉超出源数组长度的尾部槽。 |
| 源数组某个新增索引本身为空 | 保留空槽并记录/暴露源 FBX 数据问题，不凭空创建 `.mat`。 |

因此，旧材质槽 `Element 0` 不会因为 `1 → 2` 而被替换；`Element 1` 才从 FBX 补入。对于 `2 → 1`，只移除原 `Element 1`，不会重排或覆盖 `Element 0`。

### 3.3 不生成材质球

CharacterPrefabBuilder 只生成/更新外置 `.mesh` 和 Prefab，不向角色 `mat` 目录自动创建或重命名 `.mat`。如果 FBX 的槽位名称或材质资源缺失，工具只能保持槽位数量、保留可保留的旧引用并报告缺失；不能用复制 `Element 0`、按名称猜材质或新建临时材质掩盖源数据错误。

## 4. 已复现材质槽问题与根因

### 4.1 现象

对 `Assets/AssetRaw/character/hero/100601/model/mesh_100601_QHigh.fbx` 重新生成时，外置 Mesh `mesh_hero_100601_emotion_QHigh_Ed.mesh` 有两个 SubMesh，但 `pre_hero_100601_QHigh.prefab` 中对应 `SkinnedMeshRenderer` 只有一个 `m_Materials` 元素。

### 4.2 根因

旧实现的更新顺序是：

1. 全量刷新前从旧 Prefab 捕获 `Renderer.sharedMaterials`。
2. `UpdateGeneratedRenderers()` 设置新外置 Mesh。
3. `ApplyRendererMaterials()` 只要找到旧路径材质绑定，就直接把旧数组重新赋给 Renderer。

这样几何 Mesh 已经变成两个 SubMesh，但旧的一个元素材质数组仍覆盖新 Renderer。`ResolveMaterialSlotCount()` 虽然计算了 `max(materialCount, subMeshCount)`，但只显示预览，没有参与资产写入。

### 4.3 当前修正

- 把材质选择逻辑提取为无窗口状态的 `CharacterPrefabMaterialBindingPolicy`，便于单元测试和 LOD 复用。
- `ApplyRendererMaterials()` 传入“旧 Prefab 数组 + FBX 源数组”，按索引执行上面的保留/补槽/截尾规则。
- 发生槽位变化时输出 `MatSync` 日志，包含路径和 `旧槽数 -> FBX 槽数`；槽数一致且可保留时输出 `MatKeep`。
- LOD1 创建路径同样使用该策略，避免主 Renderer 已修复而 LOD1 仍被旧数组覆盖。

## 5. LOD、外置 Mesh 与坐标重建的相互影响

### 5.1 LOD

LOD0 和 LOD1 可能来自不同 FBX。每个源 FBX 都必须分别实例化、生成外置 Mesh 并读取自己的材质数组；不能把 LOD0 的材质槽数量或材质数组当成 LOD1 的输入。LOD1 Renderer 创建后再映射到目标骨架，材质同步仍按目标路径和 LOD1 源数组执行。

### 5.2 外置 Mesh

外置 Mesh 是目标 Prefab 的几何资源事实来源；源 FBX 子资源不应被直接写回。更新 Mesh 时使用 `EditorUtility.CopySerialized` 保留 Mesh 数据对象路径，避免无故替换 `.meta`/GUID。Mesh 命名由源 Renderer/Mesh 名称和角色品质规则解析，命名不匹配应告警或阻断，而不是静默生成不可追踪名称。

### 5.3 坐标轴重建

启用坐标轴重建时，必须从未回绑外置 Mesh 的 FBX 实例读取源绑定数据；先完成几何、骨骼、BindPose、Renderer 绑定和 Bounds 重建，再执行平滑法线等依赖目标 Mesh 的后处理。重建生成的 Rebase Profile 属于项目资产事实，旧动画转换不应在同一次 Prefab 生成中隐式改写。

## 6. ModelImporter 临时预设

ModelImporter 预设仅在 Model FBX 步骤、开启外置 Mesh 生成且确有覆盖项时生效。实现应遵循：

1. 首次接触每个源 FBX 时捕获原始导入设置。
2. 只临时修改明确勾选的字段，调用 `SaveAndReimport()` 后生成 Mesh/Prefab。
3. 主 FBX 和可选 LOD1 使用同一组步骤预设，但每个 importer 独立记录原值。
4. 成功、失败、取消和异常路径均在 `finally` 恢复原始设置；恢复失败要显式报错。
5. 不把临时 Read/Write、BlendShape Normals 或 Tangent 设置永久固化为项目全局规则。

## 7. 排查方法与常见错误

| 现象 | 优先检查 | 不要采用 |
| --- | --- | --- |
| Mesh 有多个 SubMesh，Prefab 只有一个材质槽 | 源 FBX 实例 Renderer 的 `sharedMaterials.Length`、目标 Prefab `m_Materials`、`ApplyRendererMaterials()` 是否覆盖旧数组。 | 只看 `.mesh` 的 `m_SubMeshes` 或只看窗口预览数字。 |
| 槽数增加后旧材质变了 | 检查旧/源数组按索引合并逻辑和是否存在名称排序/重排。 | 直接使用 FBX 完整数组覆盖旧数组。 |
| 新槽为空或显示粉色 | 检查 FBX 导入材质搜索、对应 `.mat` 是否存在、源数组对应索引是否为空。 | 复制前一个槽材质或自动新建 `.mat`。 |
| LOD0 正常、LOD1 槽位错误 | 检查 `CharacterPrefabLodBuildService.Apply()` 的目标路径和 LOD1 源 Renderer 材质数组。 | 复用 LOD0 数组或按 Renderer 名称猜测。 |
| 重新生成后手工组件消失 | 确认使用了 `RefreshFromTemplate`，并检查是否先捕获/恢复模块快照。 | 修改生成器让它隐式保留所有未知对象。 |
| 重新导入后生成结果变化 | 检查是否临时改过 ModelImporter、是否在异常路径恢复，以及源 FBX 是否被其他导入规则命中。 | 直接修改 `.meta` 试图固定结果。 |
| Prefab GUID 变化 | 检查输出路径、`.meta` 是否被删除/替换、是否发生 delete-then-create。 | 仅凭同名文件判断 GUID 保持。 |

## 8. 验证矩阵与当前边界

### 8.1 必做静态/编译检查

- 对受影响 Editor 程序集执行 Unity Editor 重编译；`dotnet build` 只能作为补充。
- 检查材质同步代码不创建新 `.mat`，不写入无关目录，不改变已有菜单和序列化字段。
- 检查 `Refresh` 与 `Additive` 两条路径、LOD1 旁路和空材质数组均有明确行为。
- `git diff --check` 或等价文本检查通过；文档使用 UTF-8，无乱码和尾随空格。

### 8.2 代表性行为测试

至少覆盖以下数组组合：

1. 旧 `1` 槽、源 `2` 槽：索引 `0` 保留旧材质，索引 `1` 使用源材质。
2. 旧 `2` 槽、源 `1` 槽：索引 `0` 保留，尾部索引 `1` 移除。
3. 旧 `2` 槽、源 `2` 槽：两个旧材质均保留。
4. 旧槽中有空引用：非空旧材质保留，空索引由源数组补入。
5. LOD1 源数组与旧目标数组长度不一致：应用相同按索引策略。
6. 源数组为空或存在空元素：不凭空创建材质，结果和告警可解释。

### 8.3 当前验证证据与未验证项

本次材质槽修正已完成：

- `TA_Tools.CharacterPrefabBuilder.Editor.Tests.csproj` restore-enabled 编译：`0 warnings / 0 errors`。
- `--no-restore` 增量编译：`0 warnings / 0 errors`。
- `git diff --check`：通过（仅可能有仓库行尾转换提示）。
- 测试覆盖主策略的增槽、减槽、等槽，以及 LOD1 增槽保留旧索引。

以下仍需在目标 Unity 编辑器内执行，不能用 CLI 编译替代：

- 实际打开 `mesh_100601_QHigh.fbx` 并运行“重新生成”；
- 读取 FBX 实例每个 Renderer 的 `sharedMaterials`，确认 `emotion` 槽位顺序和材质资源；
- 检查生成 Prefab、LOD1、外置 Mesh 的序列化结果和 Prefab 重开后的引用；
- 用 Unity Test Runner 执行 EditMode 测试；
- 若涉及模型导入预设，再验证异常恢复、源 `.meta` 未残留临时设置和增量重导入结果。

## 9. 维护检查单

- [ ] 源 FBX、目标 Mesh、目标 Prefab 和 Pipeline 配置已按实际 AssetDatabase 路径确认。
- [ ] `Mesh.subMeshCount` 与 `Renderer.sharedMaterials.Length` 分别读取，没有用预览值冒充写入结果。
- [ ] 槽位数量以 FBX 源 Renderer 数组为准；重叠索引旧材质不被替换，新增槽按索引补入，减少槽只截尾。
- [ ] 主 Renderer 和 LOD1 Renderer 使用同一策略；没有隐藏的直接 `sharedMaterials = existingMaterials` 旁路。
- [ ] 工具不创建/重命名 `.mat`，源材质缺失作为可定位告警或阻断。
- [ ] Refresh/Additive、已有 Prefab GUID、模块快照和模板职责边界没有混淆。
- [ ] ModelImporter 临时设置在成功、失败和异常路径恢复；没有污染源 `.meta`。
- [ ] 受影响程序集、Unity Test Runner、实际 Prefab 保存/重开和代表性视觉结果分别记录。
- [ ] 代码旁 Art/Tech README 与本 Profile 的项目事实同步；跨项目方法只写入 `references/` 或 CORE。
