---
name: ta-unity-lod-group-scene-instance-stripping
description: Unity Editor 中按 LODGroup Renderer 引用安全精简场景实例的实现参考，覆盖 Prefab 边界、LOD1 保留、LOD0 删除保护、Undo 与验证。
---

# Unity LODGroup 场景实例精简参考

> 类型：REFERENCE；适用于需要在已加载场景中批量移除 LODGroup、固定显示某个 LOD，或删除低级模型对象的 Unity Editor 工具。项目路径、菜单和当前验证状态见 [ProjectACG LOD 批量工具 Profile](../Profiles/ProjectACG/lod-group-batch-tool.md)。

## 适用场景

这类工具会修改场景对象，且可选删除层级对象，属于带破坏性的场景批处理。开始实现前先冻结三件事：操作对象是场景实例还是 Prefab 源资产；移除组件后要保留哪些 Renderer 状态；删除模型 GameObject 时如何保护共享与骨骼依赖。

若需求只针对场景中的 Prefab 实例，应只记录实例上的场景修改，不调用 `PrefabUtility.ApplyPrefabInstance`、`SaveAsPrefabAsset` 或其他写回源 Prefab 的路径。保存场景可以保存实例上的组件移除、Renderer 状态和已删除子对象覆盖；Prefab 源文件仍保持原样。重新实例化或还原实例覆盖时，源 Prefab 中的原始层级仍可恢复。

如果目标是直接改 Prefab 源资源，应设计单独的 Prefab 编辑事务、输出备份、目标清单和回滚流程，不要把场景实例工具静默扩展为资产修改器。

## 操作契约

| 操作 | 组件与 Renderer 行为 | GameObject 行为 | 前置条件 |
| --- | --- | --- | --- |
| 仅移除 `LODGroup` | 移除选定范围内的 `LODGroup`，不主动改 Renderer 的 `enabled`。 | 保留全部子对象。 | 目标是已加载普通场景中的对象。 |
| 固定显示 LOD1 | 开启 LOD1 Renderer，关闭该组其它 LOD Renderer，并清除保留 Renderer 的 `forceRenderingOff`。 | 激活 LOD1 Renderer 到组根之间被关闭的祖先；随后移除 `LODGroup`。 | LOD 索引 1 至少有一个有效 Renderer。 |
| 同时删除 LOD0 模型 | 与“固定显示 LOD1”相同。 | 从 LOD0 Renderer 引用得到候选对象，过滤依赖后删除最上层候选。 | 被删除对象不能承载其它 LOD、嵌套组、骨骼或探针锚点依赖。 |

模式必须明确表达是否保留低级模型。不要把“关闭 Renderer”和“删除 GameObject”合并成一个含糊选项：前者可逆且保留层级，后者会移除场景实例上的对象。

## 推荐实现流程

### 1. 收紧场景对象范围

只接受非持久化、属于有效且已加载 Scene、不是 Preview Scene 对象、也不在 Prefab Stage 中的 GameObject。Prefab 资源本身、Prefab Mode 内容和预览对象都应在计划阶段拒绝。执行前还应拒绝 Play Mode 与 Play Mode 切换窗口。

普通场景中实例化的 Prefab 仍是允许目标。`Undo`、Scene Dirty 标记和 `PrefabUtility.RecordPrefabInstancePropertyModifications` 用于记录当前场景实例的变化；不要调用 Apply 将变化传播到源 Prefab。

### 2. 从 LOD 数据建立计划

读取 `LODGroup.GetLODs()`，以每个 `LOD.renderers` 的对象引用作为模型归属依据，不按 `GameObject.name` 猜测 LOD0/LOD1。名称可以用于日志，但不能成为破坏性操作的唯一判定条件。

计划至少保存以下信息：

- 将移除的 `LODGroup`。
- 全部相关 Renderer 和要保留的 Renderer 集合。
- 可删除的 LOD0 GameObject 集合。
- 无法安全处理的组及原因。
- 被依赖保护而保留的 LOD0 数量。

固定 LOD1 时，先检查 `lods.Length > 1` 且 LOD1 至少有一个有效 Renderer。没有有效 LOD1 的组应跳过，并明确报告原因；不要因为 LOD0 可用就默认为退化到 LOD0。

若 Renderer 位于 `LODGroup` 层级之外，或同一个 Renderer 被多个 `LODGroup` 共用，则该组的显隐归属不明确，应跳过而不是改变共享对象。

### 3. 识别并保护删除依赖

LOD0 的删除候选来自 LOD0 Renderer 所在 GameObject。删除前建立保护集合，至少包含：

- 当前组根、嵌套 `LODGroup` 及其祖先。
- 非独占 LOD0 Renderer 的对象及祖先。
- 这些 Renderer 的 `probeAnchor` 及祖先。
- `SkinnedMeshRenderer.rootBone`、全部 `bones` 及祖先。

只删除不在保护集合中的候选；候选有父子关系时只销毁最上层对象，避免重复删除。一个候选同时承载 LOD1 或骨骼的情况应保留该对象，并把保护数量写入结果。

这套保护规则是保守策略。若项目还存在自定义脚本引用、约束、粒子绑定或外部资产引用，需要将其纳入保护分析，或提供精确对象预览让用户确认；不能假设 Renderer 引用列出了全部依赖。

### 4. 确认后用单个 Undo 事务执行

先构建计划，再显示目标组数、删除数量、跳过数量和保护数量。用户取消时不得产生 Scene Dirty、Undo 记录或场景写入。破坏范围较大时，应进一步显示可展开的逐对象清单。

执行时创建独立 Undo 组，记录 Renderer 与需要激活的 GameObject；然后移除 `LODGroup` 并销毁对象。完成后折叠为一个撤销动作并标记 Scene Dirty。发生异常时回滚本次 Undo 组并给出失败原因，不保留半批结果。

应用前应再次验证目标仍属于可处理的场景范围。若 UI 预览与执行之间可能有异步间隔，还应重建或核对 Renderer/删除对象计划，避免对已变化的层级执行陈旧计划。

## 常见问题与处理

| 现象 | 常见根因 | 处理方式 |
| --- | --- | --- |
| 名称为 `LOD0` 的对象没有 Renderer，真正模型节点名称各异 | 资产层级命名不统一，命名节点与 LOD 引用不是一一对应 | 从 `LODGroup` 的 LOD Renderer 引用反查对象。 |
| 删除 LOD0 后 LOD1 不显示 | LOD1 的父节点原本关闭，或 Renderer 仍受 `forceRenderingOff` 控制 | 开启 LOD1 Renderer 的祖先并清除保留 Renderer 的强制关闭状态。 |
| 删除模型后骨骼、嵌套 LOD 或探针引用断开 | 候选 GameObject 同时承载受保护依赖 | 删除前保护依赖对象及祖先；在结果中报告未删对象。 |
| 多个组切换显隐后互相影响 | 不同 LODGroup 共用 Renderer | 检出共享引用并跳过冲突组。 |
| 保存场景后 Prefab 源也被改了 | 对实例执行了 Apply/源资产保存，或工具误处理了 Prefab Stage | 限定普通场景实例，移除 Apply 路径，并分别检查场景文件和 Prefab 源文件。 |
| 批处理执行到一半失败 | 对象已销毁但没有事务边界，或对象在计划后发生变化 | 使用独立 Undo 组与异常回滚，并在破坏前重验证计划。 |

## 验证与回退

验证必须同时证明结果正确、源资产未变和操作可恢复：

- 普通场景 GameObject：只移除组件时子层级与 Renderer 状态不变。
- Prefab 场景实例：处理并保存场景、关闭重开后，场景实例保留预期修改；源 Prefab 的组件和层级仍不变。
- 嵌套 Prefab 与导入模型实例：确认只形成场景实例修改，没有写回源 Prefab 或导入资产。
- 固定 LOD1：缺少有效 LOD1、LOD1 父节点关闭、Renderer 强制关闭、跨组共享 Renderer、层级外 Renderer 等边界。
- 删除 LOD0：嵌套 `LODGroup`、非独占 Renderer、`probeAnchor`、Skinned 骨骼，以及父子候选去重。
- 撤销/重做：一次 Undo 恢复整批组件、Renderer、激活状态和 GameObject；Redo 能重现结果。
- 失败回滚：在受控测试中注入执行异常，确认整批恢复且场景状态一致。

`Ctrl+Z` 是未保存场景的快速回退方式；场景保存后仍可通过 Undo 或版本控制恢复场景文件。Prefab 源文件不应依赖回退才能保持不变，因为该工具本来就不应写它。

代码编译、Editor 自动化断言和人工场景视觉检查是不同证据。自动化通过不代表目标场景的最终外观已经验收。
