---
name: projectacg-mount-follow-track
description: ProjectACG 当前工程的挂点跟随 Timeline 轨道 Profile。记录两条轨道的 Add 菜单落点、片段类型隔离、挂点解析范围、片段面板校验、动画后校正、暂停生命周期、性能现状和验证边界，不可直接迁移到其他项目。
---

# ProjectACG 挂点跟随 Timeline 轨道 Profile

## 1. 适用范围与迁移边界

本 Profile 仅适用于 `D:\work2025U3D\Valkyria\ProjectACG\Client` 当前实现；其中历史提交证据来自同项目的既有开发记录。跨项目强制规则以 [Timeline CORE](../../README_Tech_TimelineDevelopmentRules.md) 为准；可迁移的实现与排查方法见 [相对位姿、偏移倍率与 Scene Handle](../../references/timeline-relative-pose-authoring.md)、[片段参数预览刷新与运行时数值同步](../../references/timeline-preview-refresh-and-live-value-sync.md)、[动画后跟随与 Playable 生命周期](../../references/timeline-post-animation-follow-and-playable-lifecycle.md) 与 [目标解析及嵌套挂点跟随](../../references/timeline-object-attachment-resolution.md)；本文件只记录 ProjectACG 的路径、类型、菜单、解析范围、当前算法与验证状态。

迁移到其他项目时必须删除或重建本 Profile。不得把 `fx` 命名空间、`MountFollowTrack` / `BoundMountFollowTrack` 两条轨道的划分、ProjectACG 的挂点命名（`weapon_r`、`Bip001/...`）、下方 `.meta` GUID 或当前性能结论当成跨项目统一规范。

## 2. 当前版本事实

| 项目 | 当前值 | Source of Truth |
| --- | --- | --- |
| Unity | `2022.3.62f3` | `ProjectSettings/ProjectVersion.txt` |
| Timeline | `1.7.7` | `Packages/manifest.json`、`Library/PackageCache/com.unity.timeline@1.7.7` |
| 轨道命名空间 | `fx` | `Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowTrack.cs` |

本文档引用的 Timeline 编辑器行为（`RefreshReason` 分支、`previewMode` setter、`RebuildGraphIfNecessary()`、`GetTrackCategoryName()`、`TypeUtility.GetPlayableAssetsHandledByTrack()`）均出自 `Library/PackageCache/com.unity.timeline@1.7.7`。升级 Timeline 后必须先复核这些实现，再修改相关代码或结论。

## 3. 当前代码地图

| 职责 | 当前文件 |
| --- | --- |
| 不绑定轨道 `MountFollowTrack` | `Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowTrack.cs` |
| 绑定轨道 `BoundMountFollowTrack` | `Assets/GameScripts/AOT/GameArt/TimelineTrack/BoundMountFollowTrack.cs` |
| 片段公共抽象基类 `MountFollowClipBase` | `Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowClipBase.cs` |
| 两个具体片段 `MountFollowClip` / `BoundMountFollowClip` | `Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowClip.cs`、`BoundMountFollowClip.cs` |
| 跟随运行时行为、跟随轴、失败原因枚举 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowBehaviour.cs`、`MountFollowAxis.cs`、`MountResolveFailure.cs` |
| 动画采样后的最终位姿校正驱动 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowLateUpdateDriver.cs` |
| 无状态挂点解析工具（含嵌套 Director 链反查） | `Assets/GameScripts/AOT/GameArt/TimelineTrack/MountResolver.cs` |
| 两个片段面板与共用绘制 | `Assets/GameScripts/Editor/TimelineTrack/MountFollowClipInspector.cs`、`BoundMountFollowClipInspector.cs`、`MountFollowClipInspectorShared.cs` |
| 紧邻源码的功能说明 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/README_MountFollowTrack.md` |

功能使用说明、字段语义与美术操作步骤维护在紧邻源码的 `README_MountFollowTrack.md`，本 Profile 不复制其内容。

序列化资产迁移需要脚本 GUID 时的当前值：

| 脚本 | `.meta` GUID |
| --- | --- |
| `MountFollowTrack.cs` | `b7c1e5a93d8f4e2a9c6b0f4d7a2e5183` |
| `BoundMountFollowTrack.cs` | `b23d2f7f1c9a4de4b9e7456bf257a90d` |
| `MountFollowClipBase.cs` | `5c4d9a1e7b3f4a2d8e6f0b1c9d3a7e52` |
| `MountFollowClip.cs` | `2016674dd99cd4b429a36e6c47dd9c93` |
| `BoundMountFollowClip.cs` | `3ba6f2d18c9e4d47a5b8c1e0f2d5a374` |
| `MountResolver.cs` | `9a3c5e71b2d64f08ac1e7b5d0f436c28` |
| `MountFollowLateUpdateDriver.cs` | `0f4b8e7d2c6a41f5a93e7b18d0c462af` |

GUID 会随文件重建而变；发生重建时以当前 `.meta` 为准并修订本表。

## 4. PRJ-TML-MNT-01｜两条轨道的职责与 Add 菜单落点

两条轨道服务于"挂点来源是否需要显式绑定指定对象"这一条区分：

| 轨道 | `DisplayName` | 绑定 | 挂点来源 |
| --- | --- | --- | --- |
| `MountFollowTrack` | `Mount Follow Track` | 无 `TrackBindingType`（不绑定） | 自动解析：当前 Timeline 及其嵌套链上各 Director 自身层级 + 各轨道绑定对象 |
| `BoundMountFollowTrack` | `Bound Mount Follow Track` | `TrackBindingType(typeof(GameObject))` | 只在轨道绑定的对象自身及其子层级里解析 |

绑定轨的绑定对象没有类型限制：`TrackBindingType` 只约束到 `GameObject`，角色模型、道具、机关、特效载体都可以；
**绑定对象自身**与它子层级里的任意节点（骨骼、挂点空物体、道具子节点）都能作为挂点。`MountResolver.GetBindingRoot(object)` 同时接受 `GameObject` 与任意 `Component`，因此绑定到模型上的组件也等价于绑定该对象。

自身作挂点的写法：填 `MountResolver.SelfPath`（`"."`）、直接填绑定对象的名字、或写成"首段等于绑定对象名字的路径"。整块模型 / 道具跟随另一个对象时用它，不必预先在被跟随对象里补空挂点。不绑定轨上 `.` 会落到各候选根上，多候选时报歧义，属预期行为，提示引导改用绑定轨。

两条轨道都实现 `ILayerable`，`Add Layer` 生成的子 Layer 沿用父轨道绑定，没有独立 PlayableOutput。

Add 菜单落点：

- 两条轨道放在 Timeline 的 `fx` 子菜单下，由**命名空间 `fx`** 决定：Timeline 1.7.7 在 `TimelineHelpers.GetTrackCategoryName()` 中先读内部的 `MenuCategoryAttribute`（该属性是 internal，用户代码无法使用），取不到时回退为 `trackType.Namespace + "/"`；`DisplayName` 中含 `/` 时则改用该显示名本身作为菜单路径。
- 因此改变轨道所在命名空间会直接改变它在 Add 菜单里的分组；`DisplayName` 不得随意为两条轨道添加 `/` 前缀，除非确实要改菜单结构。

片段类型隔离：

- 两条轨道分别用 `[TrackClipType(typeof(MountFollowClip))]` 与 `[TrackClipType(typeof(BoundMountFollowClip))]` 声明**具体**片段类型；
- 两个片段共同继承抽象基类 `MountFollowClipBase`，**彼此不是父子关系**。Timeline 的 `TypeUtility.GetPlayableAssetsHandledByTrack()` 用 `TrackClipType` 声明类型的 `IsAssignableFrom` 过滤片段，一旦让两个片段形成继承关系，两条轨道的 Add 菜单会互相多出对方的片段；
- 基类声明为 `abstract`，`TypeUtility.IsConcreteAsset()` 会把它排除在片段菜单之外，不需要额外做菜单过滤；
- 轨道、片段各占一个同名 `.cs` 文件：Timeline 的 `ClipEditor` 依赖 `PlayableAsset` 能解析到有效 `MonoScript`，多类型混写会让片段显示 `Script: None (Mono Script)` 并带黄色感叹号。

## 5. PRJ-TML-MNT-02｜挂点解析范围与失败语义

统一入口为 `MountResolver.TryResolve(director, bindingRoot, mountText, out mount, out failure, out firstHitOwner, forceRefreshNestedLookup)`：

- `bindingRoot != null`（绑定轨）→ 只在该根及其子层级下解析，找不到就返回 `NotFound`，不去父 Timeline 兜底；
- `bindingRoot == null`（不绑定轨）→ 按 Director 链自动解析。

Director 链的构建顺序（`MountResolver.GetDirectorChain` / `BuildDirectorChain`）：

1. 层级规则 `GetComponentsInParent<PlayableDirector>(true)`；
2. Control Track 反查：扫描 `PlayableDirector`，检查其 Control 轨片段的 `sourceGameObject.Resolve(parent)`、`updateDirector`、`searchHierarchy` 是否指向本 Director。

链缓存为 1 秒、最多 32 条、最大深度 8，并带环检测；编辑器校验传 `forceRefreshNestedLookup: true` 跳过缓存。

失败语义：

| 结果 | 触发条件 | 运行时行为 |
| --- | --- | --- |
| `NotFound` | 挂点文本为空、绑定模型内不存在、自动解析无命中 | 不跟随，打一条带搜索链的 Warning，成功前逐帧重试但只提示一次 |
| `Ambiguous` | 自动解析命中两个不同挂点 | 不跟随，打 Error 并**列出全部候选**（`模型名（挂点相对路径）`）；同一 Transform 被多条路径重复命中不算歧义 |
| `NoBinding` | 绑定轨未绑对象 | 走 `RequireTrackBinding` 分支提示先绑对象 |

查找文本：带 `/` 时先按相对路径逐级匹配（失败**不**降级为名字查找），无分隔符时按名字深度优先匹配。

`NormalizePath` 归一规则：`/` 与 `\` 等价、合并连续分隔符、去掉首尾分隔符、去掉开头的 `./`；单独的 `.` 保留为 `SelfPath`。`FindByPath` 先按"从绑定根的子层级开始"走一遍，没命中且首段正好等于绑定根名字时，再按"从绑定根自身开始"走一遍（避免与"根下恰好有同名子节点"的正常情况冲突）。

## 6. PRJ-TML-MNT-03｜片段面板与编辑器校验

两个片段由 `MountFollowClipInspector` / `BoundMountFollowClipInspector` 提供中文分区块面板，文案与排版集中在 `MountFollowClipInspectorShared`。绑定轨的片段额外提供挂点下拉框：第 0 项显示 `.（绑定对象自身）`（写入值仍是 `.`），其后是扫描绑定对象子层级得到的候选路径；命中绑定对象自身时回显"自身"而不是空路径。

校验行为：

- 使用与运行时同一份 `MountResolver`，选片段即校验；
- 校验结果按「挂点文本 + 绑定模型 + `PlayableDirector`」缓存，不每次重绘重扫；
- 自动解析失败时附上 `MountResolver.DescribeDirectorChain(director)` 的搜索链，便于判断主 Timeline 有没有被认出来；
- 歧义时附上 `MountResolver.DescribeMountHits(...)` 的全部候选，并把相对路径写进红字，美术可直接抄进字段。

## 7. PRJ-TML-MNT-04｜刷新、动画后校正与生命周期

面板按字段类别派发刷新：

| 字段 | 派发 |
| --- | --- |
| 目标物体（预制体目标 / 场景目标） | `TimelineEditor.Refresh(RefreshReason.ContentsModified)`（需要重新实例化 Prefab、重新解析 `ExposedReference`） |
| 挂点文本、跟随轴、偏移、缩放倍率、进入 Clip 时保持当前相对位姿 | `TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate)` |

实时取值：`MountFollowClipBase.SyncLiveValues(behaviour)` 把上述数值字段同步给 `MountFollowBehaviour`，`CreatePlayable` 与 `ProcessFrame` 都会调用，所以改数值不必重建图；`mountId` 变化时 Behaviour 就地重新解析挂点并重置 `m_HasKeptOffset`。

### 7.1 两条 Track 共享同一套修复

`MountFollowTrack` 与 `BoundMountFollowTrack` 都由 `MountFollowClipBase.CreatePlayable()` 创建 `ScriptPlayable<MountFollowBehaviour>`。两者的跟随轴、偏移、目标生命周期、双阶段写入和暂停处理完全共享；唯一差异是 `RequireTrackBinding`：

- `MountFollowClip` 返回 `false`，走自动解析；
- `BoundMountFollowClip` 返回 `true`，只从轨道绑定对象及其子层级解析。

因此跟随精度或暂停生命周期的修复应落在 `MountFollowBehaviour` / `MountFollowLateUpdateDriver`，不能分别在两条 Track 上复制实现。

### 7.2 动态偏差的当前修复

原实现只在 `MountFollowBehaviour.ProcessFrame()` 读取挂点并写目标 Transform。Animation Track 与 Mount Follow Track 属于独立 Playable 输出，没有建立“动画写回后再读取挂点”的显式依赖，快速动作时可能读到动画写回前或上一帧的骨骼姿态，表现为随动作速度变化的动态偏差。

当前采用双阶段写入：

1. `ProcessFrame` 立即调用 `ApplyFollowTransform()`，保证首帧 `Evaluate()`、编辑器拖帧和暂停改参数能即时生效；
2. Clip 激活后创建隐藏的 `Mount Follow Late Update Driver`，组件带 `[ExecuteAlways]` 与 `[DefaultExecutionOrder(32000)]`；
3. 驱动在 `LateUpdate` 调用 `MountFollowBehaviour.ApplyAfterAnimation()`，用本帧最终骨骼姿态再次校正；
4. 驱动对象使用 `HideFlags.HideAndDontSave`，有 Director 时挂在 Director 节点下，只在 Clip 有效期保留。

不要用调整 Timeline 轨道上下顺序代替该时序。`32000` 是 ProjectACG 当前动画链路下的实现值；如果后续引入更晚执行的 Animation Rigging、IK 或自定义骨骼约束，必须重新验证并考虑升级为图内依赖。

### 7.3 相对位姿、倍率与 Scene Handle

`MountFollowClipBase` 的 `positionOffset` / `rotationOffset` 保存原始偏移；`offsetScaleEnabled` 开启后，`offsetScale`（`0.01-1`）在运行时统一乘到位置和旋转偏移上。关闭倍率开关时有效倍率为 `1`，旧 Timeline 资产保持旧行为。Scene Handle 与运行时使用同一个有效倍率，拖动后除以倍率写回原始字段，避免缩放两次。

Inspector 的“进入 Clip 时保持当前相对位姿”是动态模式：Behaviour 保存目标当前实例的世界位置 / 旋转到 `m_KeptSourceWorldPosition` 与 `m_KeptSourceWorldRotation`，再按挂点局部空间计算相对位姿；第一次 `ApplyAfterAnimation()` 取得最终挂点姿态后，用同一份原始世界位姿调用 `RefreshKeptOffset()`，避免把动画写回前的姿态固化成固定偏差。场景目标和 Prefab 实例都按当前实例 Transform 捕获，不读取 Prefab 资源 Transform。

Inspector 的“从模型当前位置获取偏移”是编辑器烘焙模式，仅对 Hierarchy 场景目标启用：用运行时同一个 `MountResolver` 解析唯一挂点，计算模型当前位姿相对挂点的局部位置 / 旋转，按有效倍率反算写入 `positionOffset` / `rotationOffset`，并关闭 `keepInitialOffset`。这样结果可见、可审查，且可继续使用 Scene Handle 微调，点击前后实际姿态不应跳变。

动态模式打开/关闭在预览期间会清理旧的捕获基准；重新打开时按模型此刻的位姿重新捕获，不复用旧基准。动态模式下 Scene Handle 隐藏，因为手填偏移会被动态基准忽略。

### 7.4 暂停、出界与销毁

`OnBehaviourPause` 同时可能表示“整张 Graph 暂停”和“Clip 离开有效区间”。原来不区分就隐藏 Prefab / 恢复场景目标，会导致编辑器暂停时道具消失。

当前实现与 Timeline 1.7.7 的 `Runtime/Playables/PrefabControlPlayable.cs` 对齐：

```csharp
if (info.effectivePlayState == PlayState.Playing) return;
```

生命周期结果：

| 场景 | 当前处理 |
| --- | --- |
| Graph 暂停但 Clip 仍有效 | 保留目标与 `MountFollowLateUpdateDriver`，暂停预览不隐藏道具 |
| Clip 离开有效区间 | 释放驱动；Prefab 目标停用；场景目标恢复原始局部位置、旋转和缩放 |
| `OnPlayableDestroy` / 图重建 | 释放驱动；Prefab 实例销毁；场景目标幂等恢复 |

`OnPlayableDestroy` 的恢复不能删除：编辑器图重建时只靠 Pause 路径会把“已经被跟随挪过”的位姿留在场景里，下一次求值再把它当初始基准，出现越调越偏。

## 8. PRJ-TML-MNT-05｜性能现状与优化候选

稳态（挂点已解析、目标已就绪）每帧执行两次 Transform 计算 / 写入：`ProcessFrame` 一次、`LateUpdate` 最终校正一次；另有一次数值字段拷贝。后置阶段只使用缓存的目标与挂点，不重新扫描场景，正常稳态无 GC 分配。每个激活的 Behaviour 会创建一个隐藏的驱动 GameObject，退出 Clip 或销毁图时释放。

已知相对重的路径：

- 挂点解析失败时逐帧重试：`DescribeMountHits` 会构造 `List<StringBuilder>` 文案，`CollectMountHits` 会遍历 Director 链的全部 `playableAsset.outputs`；
- 反查父 Director 需要全场景查找 `PlayableDirector`（受 1 秒缓存约束）；
- 预制体目标在片段开始时 `Instantiate`、结束时销毁，属于设计成本。
- 大量 Mount Follow Clip 同时激活时，隐藏驱动数量与 Behaviour 数量线性增长；出现规模压力后应基于 Profiler 证据改为 Director 级统一调度。

优化候选（尚未实施，需先有 Profiler 证据）：失败重试节流、按 Director 缓存父链映射表、缓存归一化后的挂点文本与候选根列表、目标 Transform 无变化时跳过写入、合并同一 Director 下的后置驱动。

当前没有 Profiler 采样数据，本节的量化描述来自代码结构分析，不是实测结论。

当前缩放公式把 `m_Mount.lossyScale` 参与计算后写入目标 `localScale`。目标自身若还有非 1 父级缩放，世界缩放可能再次叠加；遇到“位置旋转准确但缩放不准”时，应先在非 1 父级缩放用例中验证，不能归因于动画后跟随时序。

## 9. PRJ-TML-MNT-06｜验证状态与未验证项

既有挂点解析与编辑器刷新功能已完成的验证：

- 运行时与 Editor 脚本的离线编译校验（Unity 自带 Roslyn 4.3.x，方式见 [Unity Editor 脚本离线编译校验参考](../../../ToolDevelopment/references/unity-editor-script-offline-compile-verification.md)），0 error / 0 warning；
- 用反射复刻 Timeline 1.7.7 `TypeUtility.GetPlayableAssetsHandledByTrack` 的片段菜单判定，确认两条轨道各自只列出自己的片段；
- 用反射调用 `MountResolver.NormalizePath` 跑 10 组用例（`.`、`./A`、`./A/B`、`/A/B`、`A//B`、`A/B/`、`A\B`、`\A\B`、`R_Hand`、`Bip001/Bip001 Pelvis/R_Hand`），全部 PASS；
- Unity 内的人工验证：连续调数值不再闪动、暂停状态改数值即时生效、嵌套子 Timeline 能找到主 Timeline 挂点、多角色同框报歧义（由使用者在编辑器内确认）；
- 相关改动已提交，提交信息带 `--story=1009103@tapd-69352340`。

本次动画后跟随与暂停生命周期修复已完成的验证：

- `dotnet build AOT.csproj --no-restore -v:minimal` 通过，0 error；
- `git diff --check` 通过；
- `MountFollowLateUpdateDriver.cs` 与 `.meta` 配对，GUID `0f4b8e7d2c6a41f5a93e7b18d0c462af` 唯一；
- 对照 `Library/PackageCache/com.unity.timeline@1.7.7/Runtime/Playables/PrefabControlPlayable.cs`，确认内置 Prefab Playable 同样使用 `FrameData.effectivePlayState` 区分 Clip 出界与其他 Pause；
- 精度修复提交为 `7942102a43`，暂停不隐藏修复提交为 `d2253b4720`，两者均带 `--story=1009103@tapd-69352340`。

完整 ProjectACG Unity 资产门禁受当前工作区原有的 `100601` 删除资源仍被引用问题阻断；该阻断与 `MountFollowBehaviour` / `MountFollowLateUpdateDriver` 修改无关，不能记作本次功能通过或失败。

未验证项：

- 没有做 Profiler 采样，性能结论为结构分析；
- 未做 Unity 批处理（BatchMode）编译与自动化用例，校验时机是编辑器处于打开状态；
- 本次相对位姿、偏移倍率和“从模型当前位置获取偏移”已完成 AOT 与 GameLogic 离线编译（0 error）；Editor 全量工程仍被工作区相机 Timeline 工具的 5 个既有缺失类型错误阻断，错误不在本次 Mount Follow 文件；
- 新增按钮与动态/烘焙双模式尚未在 Unity Timeline 窗口完成完整人工视觉验收，尤其需要覆盖倍率 `0.01`、旋转归一化、Undo、Prefab 实例和两个 Track 入口；
- 本次新增的“快速动画帧末贴合、Clip 内暂停不消失、越过 Clip 后正确清理、倒回重播”尚未在 Unity Timeline 窗口做人工视觉与 Transform 数值验收；`MountFollowTrack` 与 `BoundMountFollowTrack` 两条入口都需要覆盖；
- "绑定对象自身"的运行时行为（填 `.`、填绑定对象名字、下拉框第 0 项、命中回显"自身"）无法离线验证，需要 `Transform` 实例，必须在 Unity 里复验；
- 未验证 Timeline 升级到 1.7.7 以上后的 `RefreshReason`、`previewMode`、菜单分组与片段菜单判定行为；
- 未验证运行时（Player）构建、移动端表现、大量实例并发时的驱动数量与双次 Transform 写入开销；
- 未验证非 1 父级缩放、多个 Clip / Layer 同时写同一目标、Animation Rigging 或更晚骨骼约束与 `DefaultExecutionOrder(32000)` 的相对顺序。

回退边界：解析链不稳定或实例无法区分时，回退到 `BoundMountFollowTrack` 显式绑定；刷新语义变化时回退到 `ContentsModified`，并在交付说明中写明"编辑期会有一次预览重建"；`LateUpdate` 无法晚于最终骨骼写入时，必须升级为图内依赖、Animation Job 或约束完成后的明确回调。
