---
name: projectacg-lod-group-batch-tool
description: ProjectACG LOD 批量工具的场景实例处理范围、LOD1 固定显示和 LOD0 删除保护实现，以及当前验证边界。
---

# ProjectACG LOD 批量工具 Profile

> 类型：PROFILE；只记录当前 ProjectACG 工程事实。跨项目实现经验见 [Unity LODGroup 场景实例精简参考](../../references/lod-group-scene-instance-stripping.md)。

## 1. 工程入口与范围

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 工具目录 | `Assets/Editor/TA_Tools/Scene/LODGroupBatchTool/`。 |
| 窗口文件 | `LODGroupBatchToolWindow.cs`，类 `TATools.Scene.LODGroupBatchToolWindow`。 |
| 场景精简实现 | `LODGroupSceneStripper.cs`，通过独立 `Plan` 收集处理组、Renderer、删除候选和跳过原因。 |
| 菜单入口 | `TA_Tools/Scene/LOD 批量工具`。 |
| 处理输入 | 窗口中的场景根节点列表及其子层级 `LODGroup`；默认包含未激活对象。 |
| 修改范围 | 仅普通、已加载 Scene 中的对象实例；不处理 Prefab 资源、Prefab Stage、Preview Scene 对象或播放模式。 |

工具窗口还提供 LOD 切换阈值、指定 LOD Renderer 临时开关和按名称删除 LOD 子组等既有功能。本 Profile 记录新增的 LODGroup 移除与 LOD1 固定显示行为，不重新定义既有模块。

## 2. 新增操作

窗口“移除场景实例的 LODGroup”区有两个命令：

| 命令 | 当前行为 |
| --- | --- |
| `仅删除 LODGroup 组件` | 移除选定根节点范围内可处理的 `LODGroup` 组件，保留 GameObject 和 Renderer 当前状态。 |
| `删除 LODGroup，仅显示 LOD1` | 开启 LOD1 Renderer，清除这些 Renderer 的 `forceRenderingOff`，关闭同组其它 LOD Renderer，激活 LOD1 到组根间关闭的祖先，然后移除 `LODGroup`。 |
| `仅显示 LOD1 时，同时删除 LOD0 模型 GameObject` | 默认开启。以 LOD0 Renderer 的 GameObject 为候选，在依赖保护后删除顶层候选。关闭该选项时只切换 Renderer 状态，不删除模型 GameObject。 |

每次执行前弹出确认框，展示待移除组件数、LOD0 删除对象数、跳过组数和保护对象数。详细跳过原因记录到 Console。取消不执行；完成后整批可用一次 `Ctrl+Z` 撤销。

## 3. 场景实例与 Prefab 边界

目标首先由 `LODGroupSceneStripper.IsSceneObject()` 判定：非持久化对象、有效且已加载的场景对象、非 Preview Scene 对象，并且不在 Prefab Stage。执行阶段还会拒绝 Play Mode 或 Play Mode 切换状态。

普通场景中实例化的 Prefab 属于允许目标。移除组件和删除子 GameObject 只记录为场景实例变化，保留实例与 Prefab 的关联；工具没有调用 `ApplyPrefabInstance`、`SaveAsPrefabAsset` 或其它写回 Prefab 源文件的流程。保存场景只保存场景实例状态，不会把修改应用到 Prefab 资源。

这一边界是需求契约：如果未来要支持源 Prefab 修改，应作为单独模式实现，需有明确资产目标、写入预览、备份和独立验证；不得修改当前场景实例命令的语义。

## 4. 计划、保护与事务实现

### 4.1 计划构建

`BuildPlan()` 使用 `LODGroup.GetLODs()` 获取各级 Renderer，按组件引用识别模型，不要求 GameObject 命名符合 `LOD0`/`LOD1`。固定显示 LOD1 时，如果没有有效的索引 1 Renderer，则跳过该组。

固定 LOD1 的组还会在已加载场景中检查 Renderer 归属。若 Renderer 位于组层级之外，或被多个 LODGroup 共用，计划跳过该组，避免改变外部对象或其它组正在使用的 Renderer。

### 4.2 LOD0 删除保护

候选来自 LOD0 Renderer 所在 GameObject。执行删除前保护以下对象与祖先：

- 当前与嵌套 `LODGroup` 的层级。
- 除独占 LOD0 Renderer 外的其它 Renderer。
- 非独占 Renderer 的 `probeAnchor`。
- `SkinnedMeshRenderer.rootBone` 和 `bones`。

最后从未受保护候选中只保留最上层对象，避免父对象销毁后又重复销毁子对象。被保护的候选不会阻止安全部分继续处理，但会计入“保留 LOD0 对象数”并显示在确认信息里。

保护分析基于当前工具可见的 Renderer、嵌套 LODGroup、骨骼和探针锚点关系。自定义脚本字段、约束系统或其它非 Renderer 引用不在当前依赖扫描范围内。

### 4.3 Undo 与异常回滚

`Apply()` 创建独立 Undo 组；Renderer 状态和激活状态通过 Undo 记录，组件和 GameObject 使用 `Undo.DestroyObjectImmediate()` 删除。成功后折叠成一个 Undo 操作并刷新 SceneView；执行异常时调用 `Undo.RevertAllDownToGroup()` 回滚本批操作，窗口报告异常和“本批操作已撤销”。

场景会标记 Dirty，由用户决定保存场景。工具不自动保存 Prefab 或场景文件。

## 5. 已解决问题

| 问题 | 原因 | 当前处理 |
| --- | --- | --- |
| 对 Prefab 的修改边界不清晰 | 场景实例和 Prefab 源文件是不同持久化对象，直接 Apply 会扩大影响范围 | 只接受普通 Scene 实例，不提供源 Prefab 写入路径；场景中的 Prefab 实例修改保留为场景覆盖。 |
| 仅按名称寻找 `LOD0` 不可靠 | 不同模型的 Renderer 可能挂在不同命名层级 | 按 `LODGroup` 的 LOD Renderer 引用反查 GameObject。 |
| 删除低级模型可能破坏保留模型依赖 | GameObject 可能还承载 LOD1、骨骼、探针锚点或嵌套组 | 构建保护集合，冲突项保留并计数。 |
| 一个 Renderer 可能被多个组使用 | 显隐改动会影响共享该 Renderer 的其它组 | 固定 LOD1 模式检测跨组共享并跳过。 |
| 批量删除中途异常会留下部分结果 | 组件、激活状态和对象删除是多个场景写操作 | 使用单一 Undo 组，并在异常路径整组回滚。 |

## 6. 当前验证状态

### 已完成

- 使用 Unity `2022.3.62f3` 在独立临时 Unity 工程运行 Editor BatchMode smoke test，日志记录 `LOD_SCENE_SMOKE_PASS assertions=42`。
- 42 条断言覆盖普通与嵌套 Prefab 场景实例、导入模型实例、场景保存重开、Undo/Redo、共享 Renderer 和骨骼保护等行为。
- 工具代码编译检查通过。

日志路径：`%LOCALAPPDATA%\Temp\ProjectACG-LODSceneCheck\smoke.log`。

### 尚未完成

- 尚未在目标 ProjectACG 场景内逐个检查真实模型的 LOD1 视觉质量、材质和层级激活状态。
- 当前确认框提供数量汇总，逐组跳过原因在 Console；没有展示待删除 GameObject 的完整可点击清单。
- 依赖保护不扫描任意自定义脚本字段或项目特有约束引用。

## 7. 维护检查单

- [ ] 保持操作对象只限普通已加载场景实例；Prefab Stage、Prefab 资源和 Preview Scene 不得进入执行计划。
- [ ] 不添加 Apply/Prefab 保存逻辑到场景实例命令；场景保存后抽查源 Prefab 文件未变。
- [ ] 固定 LOD1 前仍检查有效 LOD1、外部 Renderer 引用及跨组共享 Renderer。
- [ ] 扩展删除规则时同步覆盖嵌套 LODGroup、Renderer 祖先、`probeAnchor`、Skinned 骨骼与非 Renderer 依赖。
- [ ] 破坏性执行仍需用户确认、单组 Undo、异常整批回滚和 Scene Dirty 标记。
- [ ] 在目标场景做人工视觉复核；smoke test 和代码编译不能替代场景验收。
