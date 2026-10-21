---
name: projectacg-ultimate-timeline-regeneration-profile
description: 大招 Timeline 生成器的重复生成保护边界、末尾 Battle_CameraPos 方案、持久引用、当前源码与历史记录差异、排查工具及验证证据。
---

# ProjectACG 大招 Timeline 重复生成与收尾镜头案例

> 类型：`PROFILE`；核对日期：`2026-10-20`，Asia/Shanghai。本文记录项目约定、源码事实和验证边界；可迁移的方法见 [重复生成与收尾镜头参考](../../../TimelineDevelopment/references/timeline-regeneration-preservation-and-tail-camera.md)。整体工具背景见 [生成器总 Profile](ultimate-skill-timeline-generator.md)。
>
> **当前状态：本次核对的工作区没有自动生成末尾 0.3 秒 `Battle_CameraPos` Shot 的实现。** 历史会话记录曾描述完成该功能和静态编译，但不能作为当前分支具备功能的证据。本次任务只整理文档，没有恢复功能代码，也没有重新生成生产资源。

## 1. 项目事实与需求边界

### 1.1 需求及预期行为

用户要求在“大招技能 / 驱动虚拟相机镜头”模式中，生成 Timeline 时，在镜头虚拟相机轨道上增加 `Battle_CameraPos`，占动画结尾的 `0.3s`；随后关注重新生成会不会丢失别人手工新增的轨道和配置。

这里包含两个独立合同：

1. 自动收尾：时间窗口、镜头混合、Prefab 相机和引用表相互一致。
2. 可继续编辑：再次生成只更新工具接管的数据，说明手工片段、节点覆盖、绑定和播放范围的保留边界。

不能用“主 Timeline 没有整个删除”直接回答“所有手工配置都不丢”。

### 1.2 本次核对基线

| 项目 | 本次读取结果 |
| --- | --- |
| 工作区 | `D:/work2025U3D/Valkyria/ProjectACGMain3/ProjectACG/Client`。 |
| 分支 / HEAD | `feature/CharaOptimization` / `30125ad3f0`。 |
| 工具目录差异 | `git status --short -- Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator` 无输出。仅代表该目录，不代表全仓干净。 |
| Unity | `ProjectSettings/ProjectVersion.txt`：`2022.3.62f3`。 |
| 包版本 | `Packages/manifest.json`：Cinemachine `2.10.5`、Timeline `1.7.7`。 |
| 主源码 SHA-256 | `9AEA007EDACB42F64B42014E875543C78BC8F4F4EC2F0C1DE36E9A7A01ECF501`。 |
| 本次动作 | 只读核对源码、参考资产和已有规则，编写规则参考与案例。 |

主源码是工具目录中的 `UltimateSkillCameraTimelineGeneratorWindow.cs`。下文以方法名定位，行号仅作为此次快照的辅助，后续版本可能变化。

### 1.3 当前源码与历史记录的差异

| 功能点 | 当前源码事实 | 历史记录 / 后续设计 |
| --- | --- | --- |
| 末尾 Shot | `CreateOrUpdateMainTimeline` 与 `ReassembleCameras` 只铺普通镜头及捕获到的手工 Shot。 | 历史记录描述过 `AddBattleCameraShot`，当前文件没有该方法和调用。 |
| 时间常量 | 当前没有 `BattleCameraShotDuration = 0.3d`。 | 用户需求仍是末尾 0.3 秒，不能从参考资产复制其它长度。 |
| Battle 节点 | `EnsureBattleCameraPosition` 创建/配置相机节点。 | 节点存在不代表 Timeline 上存在相应 Shot。 |
| Battle 引用键 | `BuildBattleCameraReferenceName` 仍存在，参与旧工具片段识别。 | 仅有命名方法不代表创建、绑定、自检闭环完整。 |
| 清理风险 | `IsGeneratedCameraReference` 排除 `_camera_` 前缀和精确 `_battle_camera` 键。 | 旧自动 Battle Shot 可被排除后不再恢复；升级/回退时都要覆盖。 |
| Battle 自检 | 当前验证节点、组件、管线和 `UltimateTimelineBindings`。 | 没有末尾 Shot 数量与 0.3 秒区间的对应校验。 |

本次不能判断差异是分支切换、未合入还是再次回退造成，不能写成已经查明“误回滚”。历史实现方案保留在第 4 节，作为后续开发参考。

## 2. 代码导航与资产数据流

### 2.1 阅读顺序

共同目录：`Client/Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator/`。

| 文件 / 入口 | 职责与本案例关注点 |
| --- | --- |
| `UltimateSkillCameraTimelineGeneratorWindow.cs` | 输入收集、Scope 分发、时间计算、Timeline/Prefab 更新、绑定与批处理入口。 |
| `UltimateSkillCameraTimelineGeneratorWindow.UI.cs` | Odin 窗口、制作模式、镜头模式、生成与组装操作。 |
| `UltimateSkillCameraTimelineGeneratorWindow.Validation.cs` | 输入检查和按作用域的生成后验证；查看是否覆盖新增内容。 |
| `UltimateSkillCameraTimelineGeneratorWindow.Presentation.cs` | 剧情/抽卡路径；本次大招 Battle 收尾需求不自动扩展到这些模式。 |
| `UltimateSkillCameraTimelineGeneratorWindow.Presets.cs` | 本机预设、资源引用和设置恢复。 |
| `UltimateTimelineBindingsEditor.cs` | 大招场景预览绑定与嵌套 FX 保存。 |
| `Tests/UltimateFxPrefabSaveTests.cs` | 已有 FX Prefab 保存测试；不是收尾镜头和人工轨保留测试的替代。 |
| 相邻 Art/Tech README | 功能操作和维护说明的源码旁入口；内容与当前代码不一致时逐项核对。 |

方法定位：`ResolveDuration` → `CreateOrUpdateMainTimeline` / `ReassembleCameras` → `CreateOrUpdateMainPrefab` / `UpdateMainPrefabCameras` → `BindSavedMainPrefabReferences` → `VerifySavedMainPrefabReferences` → `ValidateGeneratedAssets`。

### 2.2 生成、组装、预览的职责

| Scope / 操作 | 当前含义 | 保留边界 |
| --- | --- | --- |
| `All` | 生成模块并完整组装。 | 所有工具内容可能更新。 |
| `Camera` / `Fx` | 生成对应模块资产。 | 不等于把结果组装到主 Timeline。 |
| `Sound` / `Prop` | 准备和检查现有资源。 | 道具带有效动画而缺 Animator 时可能补写源 Prefab，不能理解为绝对只读。 |
| `Assembly` | 按当前输入完整组装主资源。 | 不重新生成模块不代表不重建主轨道或节点。 |
| `CameraAssembly` | 更新镜头轨道；正常虚拟相机分支局部更新主 Prefab。 | 缺 Timeline/Camera Group 时回退完整组装；缺主 Prefab 时完整创建。非虚拟模式也可能走完整 Prefab 更新。 |
| `FxAssembly` / `SoundAssembly` / `PropAssembly` | 按模块重新组装。 | 需逐个检查缺结构的回退路径；道具组装还可能扩展主时长。 |
| 一键预览绑定 | 场景实例绑定角色 Animator 和相机 Brain。 | 不把场景角色或相机固化进 Prefab 资产。 |

### 2.3 当前命名和时间来源

当前 `MainTimelineName` 为 `tl_<heroId>_<EffectiveSkillName>`，`MainPrefabName` 为 `tPre_<heroId>_<EffectiveSkillName>`，保留技能名的前导零。旧资产的 `tl_hero_*` 名称是历史资源实例，不是当前主资源命名模板。

完整组装设置 `frameRate = 60`、`durationMode = FixedLength`、`fixedDuration = context.Duration`。`ResolveDuration` 先取角色动作长度之和；没有有效动作长度时取镜头长度之和；再与最长一组道具动画长度取大值，最小 `0.01s`。

因此“动画最后 0.3 秒”仍需明确是角色动画末尾还是主 Timeline 末尾。历史方案按主 Timeline 总长度实现；道具动画更长时，两者会不同。`ResolveMainTimelineDuration` 优先读取现有固定长度，其次取角色轨结束时间，最后回退上下文长度，可供局部组装使用。

## 3. 重复生成时到底保留什么

### 3.1 当前代码的保护矩阵

| 内容 | 当前行为 | 实际风险 |
| --- | --- | --- |
| 主 Timeline 资产 | 原路径加载并更新，已有资产复用。 | GUID 保留不等于内部参数全部保留。 |
| 非工具根轨道 / Group | 原对象保留；自定义轨道彼此顺序保留。 | 相对于集中排列的工具轨位置可变化；绑定、播放范围另查。 |
| `Camera Group` 内非工具子轨 | 第一条匹配 Group 内的其它子轨保留。 | 同名重复 Group 可被删除，连带其子内容。 |
| `Ultimate_Character`、`CameraAnimation` | 按类型和精确名称匹配，清空 Clip 再铺设。 | 在这些轨道内手调的 Clip 不属于完整保留范围。 |
| `Ultimate_Camera` | 清空后重建自动 Shot 和有限手工快照。 | 不能保证 blend、ease、clipIn、timeScale 等完整恢复。 |
| `Prop Track` / `Prop Track NN - …` | 由工具重建跟随 Clip。 | 工具轨内手改参数可能覆盖。 |
| `Prop Animation Track NN - …` | 按道具动画列表重建或清理。 | 输入清空、重排会改变轨道和绑定。 |
| `Sound Track`、主 `FX Track` | 工具维护的 Clip 重铺。 | 手工改动的播放参数会被重建逻辑覆盖。 |
| 旧 `Camera Animation `、`Camera FOV ` 前缀轨 | 在既定组内按类型/前缀清理。 | 手工内容若沿用这些名字，也可能被识别为旧产物。 |
| `DirectCameraSync` | 按当前模式创建或移除。 | 模式切换属于结构变化。 |
| 主 Timeline 固定长度 | 完整组装重新计算。 | 手工 Clip 可以存活但落在播放终点之外。 |
| FX 子 Timeline | 保留已有轨道，只补缺失默认结构。 | 固定长度等工具字段仍可能更新，不等于整文件不变。 |
| FX 源 Prefab、已有 VolumeProfile | 保留源节点/参数并补必要绑定。 | 与主 Prefab 中 FX 实例 override 是两类数据。 |
| 主 Prefab 四个容器 | 完整组装清空 `camAni_group`、`fx_group`、`prop_group`、`scene_root` 后重建。 | 这些实例上的独有修改可能消失；外部轨道绑定旧对象可失效。 |
| 生成虚拟相机 | 工具维护 `Virtual_Camera_##` 和 Battle 节点配置。 | 手调生成相机的组件、位置或管线可能被重置。 |

所有权目前由名称、类型和前缀约定识别，不是持久 owner 元数据。`GetOrCreateGeneratedRootTrack<T>` / `GetOrCreateGeneratedChildTrack<T>` 会删除重复匹配项并调用 `ClearTrackClips`，名称与类型碰撞也是风险。

### 3.2 用户所举自定义轨道

历史会话所举内容包括：

- `Timeline Battle Control Track` 及其 `(1)` 副本。
- `Timeline Object Visibility Track` 及其 `(1)` 副本。
- 包含 `Ultimate_Layer_Culling` Clip 的相机层级剔除轨道。

在这些轨道是独立的非工具轨、没有落入被删除的同名工具 Group 等条件下，当前清理规则不会主动清空它们，原 Clip、时间和参数保留。这个结论来自源码所有权审查，没有通过本次真实资源重新生成验证。

仍需确认它们的目标是否在重建容器内，以及 Clip 是否超过新固定长度。表达给作者时可说：“这些独立自定义轨会保留；工具轨内手调片段、绑定到重建节点的对象和播放总长度需另外确认。”

### 3.3 手工 Cinemachine Shot 的有限快照

`CaptureManualCameraShots` 只保存：

```text
ReferenceName / DisplayName / TargetName / Start / Duration
```

重建后调用 `AddCinemachineShot` 恢复，再由 `BindMainPrefabReferences` 按目标名称查找相机。没有复制所有 TimelineClip 字段，未找到目标或遇到同名对象时也不能认为完整恢复。

当前 `_battle_camera` 稳定键会被识别为工具内容并跳过快照。其它引用键的手工 `Battle_CameraPos` Shot 则可能被捕获，但依然只有上述五个字段。历史“自动补末尾 Shot”方案进一步接管 Battle 手工镜头，属于另一版本合同，不能混写成当前行为。

## 4. 末尾 0.3 秒镜头的实现参考

> 本节是历史方案和后续实现检查点，**不是当前源码中已经存在的功能**。若恢复功能，应重新完成生成、绑定、迁移、验证和 Unity 复验。

### 4.1 参考资产应读行为，不照抄数字

用户最初提及 `100104`、`100601`。本次在 `Assets/AssetRaw/character` 按角色编号匹配 `.playable` 文件未找到 `100104`；不能写成已经核对该角色的效果。

当前可确认的样本为：

```text
Assets/AssetRaw/character/hero/100601/timeline/tl_hero_100601_bigskill02_1.playable
```

该资产文本中普通镜头为 `0–4s`，`Battle_CameraPos` 起点 `3.5s`、长度约 `0.883333s`，两侧混合字段为 `0.5s`，主 Timeline 固定长度 `4s`。它体现的是“最后镜头与 Battle 镜头交叠”的做法；片段本身延伸到固定播放范围之外。用户要求的 0.3 秒窗口要独立计算。

历史记录还提到 `100101`、`100401` 的对照数据，但此次没有重新确认它们的相同 Battle 片段，因此不将其历史秒数列为本次证据。

### 4.2 时间区间与混合

以下是历史方案的核心片段，执行上下文为生成器的镜头组装逻辑；`timelineDuration` 已由工程规则归一化：

```csharp
double duration = Math.Min(0.3d, Math.Max(timelineDuration, 0.01d));
double start = Math.Max(timelineDuration - duration, 0d);
```

对应方法设计为 `AddBattleCameraShot(settings, track, lastCameraShot, timelineDuration)`；普通 `AddCinemachineShot` 返回 `TimelineClip`，以便设置上一片段与 Battle 片段的混合。

历史方案的混合判断如下，变量均为已创建的 TimelineClip：

```csharp
double overlap = lastCameraShot == null ? 0d :
    Math.Min(lastCameraShot.end, battleShot.end) - battleShot.start;
if (lastCameraShot != null &&
    lastCameraShot.start <= battleShot.start && overlap > 0d)
{
    lastCameraShot.blendOutDuration = overlap;
    battleShot.blendInDuration = overlap;
}
```

这个判断只覆盖最后镜头不晚于 Battle 起点、并确有交叠的情况。最后镜头很短、多镜头跨尾窗或镜头提前结束时，不能承诺平滑混合；按 [通用参考](../../../TimelineDevelopment/references/timeline-regeneration-preservation-and-tail-camera.md) 的边界矩阵处理。

| 输入 | 预期区间 | 额外检查 |
| --- | --- | --- |
| 主时长 `4s` | `[3.7, 4.0]`。 | 上一镜头是否覆盖尾窗。 |
| 主时长 `0.2s` | `[0, 0.2]`。 | 不出现负起点或超长片段。 |
| 无 FBX 镜头，但有合法时长 | 仍可按合同添加 Battle Shot。 | 相机节点、Brain 和播放控制权是否齐备。 |
| 角色 `4s`、道具 `5s` | 若按主时长则为 `[4.7, 5.0]`。 | 是否符合用户“动画末尾”的意思。 |
| 局部镜头组装 | 根据现有 Timeline 长度计算。 | 避免仅凭当前输入改变既有总时长语义。 |

### 4.3 接入位置和幂等迁移

历史方案覆盖 `CreateOrUpdateMainTimeline` 和 `ReassembleCameras` 两个入口，且只在大招 `settings.UsesVirtualCamera` 分支执行。完整组装使用 `context.Duration`；局部组装使用 `ResolveMainTimelineDuration`。

引用键为 `<heroId>_<skillName>_battle_camera`，目标是主 Prefab 的 `camVirtual_group/Battle_CameraPos` 上的 `CinemachineVirtualCamera`。重复生成后预期恰好一个工具 Battle Shot。

历史方案会排除旧工具键，以及显示名或解析目标名为 `Battle_CameraPos` 的手工片段，然后按新规则生成。其代价是这类手工片段的时间和混合参数被接管；不能把它描述成无损恢复。后续实现若存在同名但用途不同的片段，应增加更明确的迁移条件。

### 4.4 相机节点与引用闭环

当前 Battle 节点配置可复用：本地原点、旋转 identity、缩放 one，`CinemachineVirtualCamera` + 无 Controller 的 `Animator`，FOV `50`、Priority `9`，Follow/LookAt 为空，标准 `cm/CinemachinePipeline` 内没有 Body/Aim/Noise 组件。

普通 `Virtual_Camera_##` Priority 为 `10`，采用 Hard Lock To Target / Same As Follow Target；两者用途不同。不要把动画跟随管线复制到 Battle 节点，也不能把“Prefab 原点”理解成运行时已自动对齐战斗镜头位置。

历史新增 Shot 还需要在 `BindMainPrefabReferences` 中明确绑定，并让生成后校验覆盖唯一数量、时间区间和正确相机对象。部分 FX/Prop 组装面对旧 Timeline 时，可在引用完整性检查中按 Shot 是否存在进行判断，避免未升级相机的旧资源阻断无关模块操作；Camera/完整组装则检查新的必需 Shot。

## 5. 已有实现方式与可复用经验

### 5.1 两遍保存解决 Prefab 引用持久化

当前工具先保存主 Timeline 子资产，再构建并保存主 Prefab；随后重新 `LoadPrefabContents`，从已落盘层级解析目标，写 Generic Binding 与 ExposedReference，再保存并回读。

`ResolveExposedName` 经 `SerializedObject` 读取纯 exposedName，不能把 `PropertyName.ToString()` 的显示字符串当原始引用键。`VerifySavedMainPrefabReferences` 同时检查 `GetReferenceValue` 与序列化引用表；失败时尝试对持久 Prefab 对象修复，仍缺失则报错。

第二遍保存避免无必要地重新赋值 PlayableAsset 或 `RebuildGraph`。保存、导入、卸载后重新解析对象，避免沿用已经失效的 PrefabContents 引用。具体步骤沿用 [持久绑定参考](../../../TimelineDevelopment/references/timeline-asset-generation-and-prefab-binding.md)，不另建一套规则。

### 5.2 镜头动画只维护一份时间数据

共享 `CameraAnimation` 连续铺多个动画 Clip；对应 Cinemachine Shot 使用同一 `{Start, Duration}`。镜头层级为 `Camera_Root/Camera` 及目标节点，Transform 与 `Camera.fieldOfView` 曲线保存在同一个 AnimationClip，减少单独 FOV 轨的维护。

当前镜头 Prefab 已自带实体 Camera 和 URP Additional Camera Data；主组装按模式决定是否渲染，并补/复用共享 Animator。这一点与旧总 Profile 的“纯 Transform 镜头 Prefab、不带 Camera”描述不同，以本次代码和源码旁 README 为准。

### 5.3 FX、道具和预设的资产边界

- FX Clip 控制主 Prefab 内实例，`prefabGameObject` 留空，`sourceGameObject` 通过稳定引用绑定，避免误生成两套实例。
- 配置道具动画时，`EnsurePropAnimator` 检查全层级，仅缺失时补无 Controller 的 Animator；没有动画不因该需求改写源 Prefab。
- 已有 VolumeProfile 复用，保留美术参数；FX 子 Timeline 补缺失结构，不清空所有内容。
- 本机预设位于 `Library/TA_Tools/UltimateSkillCameraTimelineGenerator/Presets/`，`EditorPrefs` 记忆选择；资源引用使用 GUID/localFileId。Library 会被清理，不是共享交付物或唯一备份。
- 嵌套 FX 保存与主资源组装是不同操作，前者按 owner/target 边界保存源资源；详见 [嵌套 Prefab 参考](../../../TimelineDevelopment/references/timeline-nested-prefab-save-and-generated-binding.md)。

### 5.4 可迁移到其它工具的工作方法

先列输入、输出、所有权和 Scope，再决定改哪几个方法。用同一命名/路径解析器贯通生成、查找、绑定和自检；把对已有资源的“补缺失”与“重建”明确分开；仅在必要组件缺失且输入确实需要它时自动补齐。

作者可见的工具摘要说明实际生成了什么、更新了什么、哪些手工区域会重建。实现细节留给技术 README，产品界面只呈现有助于作者决定和排错的内容。

## 6. 问题、根因、处理与证据

| 问题 / 现象 | 根因或待确认项 | 处理方式 | 本次证据边界 |
| --- | --- | --- | --- |
| 历史记录说末尾镜头已完成，当前无实现 | 工作区版本与记录不同，具体原因未定。 | 固定分支、HEAD、文件哈希和方法调用；把方案与现状分开。 | 本次源码可确认缺少方法和调用。 |
| 旧 Battle Shot 可能不再恢复 | 当前清理将旧稳定键当工具内容，创建路径却不补回。 | 恢复功能时同时修创建、迁移、绑定、自检；先做隔离资产用例。 | 静态调用链风险，未执行生产资源重组。 |
| 手工轨保留但效果丢了 | 绑定指向重建节点，或时间超出 fixedDuration。 | 比较绑定和播放范围，不只比较轨道列表。 | 当前写入边界可确认，实际特定资源待验证。 |
| 手工 Shot 混合变化 | 快照只含五个字段。 | 保留对象或补完整、经验证的快照。 | 当前字段和恢复调用可确认。 |
| Shot/Prop/FX 为 None | 临时对象映射、引用键读取或落盘时序。 | 两遍保存、纯键、持久对象、回读。 | 当前已有处理代码，不代表任意资产均已实测。 |
| 全量编译前先用 no-restore 失败 | 缺 `project.assets.json`。 | 改正常 restore/build，输出用临时目录。 | 历史执行记录，不是本次重新编译。 |
| Roslyn 响应文件报缺源文件 | 缓存 `.rsp` 对应旧工作树。 | 使用当前项目文件，核对生成时间和源码列表。 | 历史记录；不据此判断当前源码有错。 |
| 仓库 harness 失败 | 文档/配置/skill/UI 治理等其它检查项。 | 分别列出工具验证与仓库问题，避免伪称全绿。 | 历史报告不替代当前报告。 |

## 7. 验证工具、命令和结果边界

### 7.1 可复用的只读核对命令

执行目录：`Client`，PowerShell。各命令可独立运行。

```powershell
git branch --show-current
git rev-parse --short HEAD
git status --short -- Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator
git diff --check -- Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator
git log -5 --oneline -- Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator
rg -n 'AddBattleCameraShot|BattleCameraShotDuration|CaptureManualCameraShots|IsGeneratedCameraReference|ClearChildren' Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator --glob '*.cs'
Get-Content -LiteralPath ProjectSettings/ProjectVersion.txt -Encoding utf8
```

`rg` 无匹配会返回非零码，只说明该搜索未找到匹配，不直接等于构建或工具失败。中文文件读写使用显式 UTF-8；不要把 Unicode 文本经默认编码的原生 stdin 管道再落盘。

### 7.2 编译与 Unity 验证入口

历史使用过的完整 Editor 构建形式如下；首次不要直接跳过 restore：

```powershell
dotnet build Assembly-CSharp-Editor.csproj -v:q `
  -p:OutputPath="$env:TEMP\acg_camera_return_check\bin\" `
  -p:IntermediateOutputPath="$env:TEMP\acg_camera_return_check\obj\" `
  -clp:ErrorsOnly
```

Unity 批处理自清理 SmokeTest 入口是 `GameLogic.Editor.UltimateSkillCameraTimelineGeneratorBatch.SmokeTest`。以下命令在 `Client` 执行，前提是目标项目未被另一个 Unity 实例占用；本次仅记录命令，没有运行：

```powershell
$taskUnity = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe'
& $taskUnity -batchmode -quit -projectPath (Get-Location).Path `
  -executeMethod GameLogic.Editor.UltimateSkillCameraTimelineGeneratorBatch.SmokeTest `
  -logFile "$env:TEMP\ultimate-timeline-smoke.log"
```

该 SmokeTest 使用独立临时资产目录并清理，主要覆盖默认基础资产和生成后校验。新增尾镜和人工内容保留需要额外的明确断言；测试入口存在不能写成这些用例已通过。`GenerateDefault` 会生成默认正式路径资源，不用它代替隔离验证。

仓库门禁从 `Client` 调用：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File ..\Tools\Codex\check-projectacg-harness.ps1 -Scope Changed
```

### 7.3 已验证与未验证

| 证据层级 | 状态 | 能支持的结论 |
| --- | --- | --- |
| 本次源码核对 | 已完成。 | 当前没有 0.3 秒自动 Battle Shot；保护矩阵和有限快照来自当前代码。 |
| 本次参考资产 | 已读取 `100601` 样本时间与混合字段。 | 确认样本采用交叠结构，不能替代视觉验证。 |
| 历史 Editor 编译 | 会话记录为 `0 errors / 783 warnings`，记录日期 `2026-10-14`。 | 只作为历史说明；本次未重跑，且当前代码与历史方案不一致。 |
| 历史 diff/harness | 记录称目标 diff 检查通过，harness 有无关失败项。 | 不是本次工作区整体通过的证明。 |
| 本次文档检查 | 交付时对新增/修改文件检查分层、链接、UTF-8、meta/GUID、尾随空白和写入回读。 | 证明文档完整性，不证明 Unity 功能。 |
| Unity 生成、保存重开、反复生成 | 本次未执行。 | 不保证具体生产资源无损。 |
| Timeline 播放、混合和 FOV | 本次未执行。 | 不保证任意输入下镜头连续和控制权正确。 |
| Player / AssetBundle | 本次未执行。 | 不扩展成正式运行接入或构建验证结论。 |

### 7.4 恢复或扩展功能时的用例

1. 在隔离资源中首次生成、连续生成两次，确认只有一个自动 Battle Shot。
2. 检查 `4s`、小于 `0.3s`、无镜头、角色/道具/镜头长度不一致。
3. 执行完整组装与镜头组装，验证尾窗依据及缺结构回退。
4. 加入独立控制轨、显隐轨、剔除轨、Marker，比较原对象、Clip 参数与相对顺序。
5. 在工具镜头轨放入手工 Shot，单独核对 ease、blend 曲线、clipIn、timeScale、引用键。
6. 分别放入旧 `_battle_camera` 键和手工随机键的 Battle Shot，验证迁移和重复生成。
7. 让手工轨引用工具容器内对象，检查重建后是否仍能解析并播放。
8. 在主固定长度之后放手工片段，确认保存与播放截断的差异。
9. 保存重开 Prefab 后核对引用表和正确目标；Play/Seek/Stop/重复播放检查相机、FOV 和恢复。
10. 失败后按本次资源及 `.meta` 的精确变更集回退，保留原工作区其它未提交内容。

## 8. 尚未实现的改进方向与规则维护

| 改进方向 | 价值 | 当前状态 |
| --- | --- | --- |
| 持久 owner ID 与稳定条目身份 | 减少名字碰撞、重排误认。 | 当前仍主要按名称/类型/前缀。 |
| 保留原手工 Clip 或完整状态快照 | 保住混合曲线和时间编辑。 | 当前只保存五个字段。 |
| 自定义绑定重定向 | 解决外部手工轨引用被重建对象。 | 没有通用完整保证。 |
| 写入预览、备份、失败事务恢复 | 更清楚地展示影响并降低多资产半成功风险。 | 不把现有 finally 清理视为事务。 |
| 尾镜及保留边界集成测试 | 防止创建/清理/绑定/校验版本不一致。 | 本次没有新增或执行这些测试。 |

本案例的可复用经验进入 [通用参考](../../../TimelineDevelopment/references/timeline-regeneration-preservation-and-tail-camera.md)；项目方法名、路径、相机数值和验证状态留在本 Profile；工具按钮使用方式仍留在源码旁 Art README。旧 [生成器总 Profile](ultimate-skill-timeline-generator.md) 保留历史上下文，但命名、镜头 Prefab、保留边界和验证状态发生冲突时，以本次有源码证据的对应条目为准，后续代码变化后再次核对。
