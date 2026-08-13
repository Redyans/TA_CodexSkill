---
name: projectacg-animation-clip-previewer-profile
description: ProjectACG AnimationClipPreviewer 的目标锁定、动画采样、取消预览、FBX 姿态复制、已解决问题和验证边界 Profile。
---

# ProjectACG AnimationClipPreviewer Profile

> 类型：PROFILE；适用范围：当前 ProjectACG 的 `AnimationClipPreviewer` 编辑器工具；通用生命周期、实现模式和验证矩阵见 [Unity 动画预览工具生命周期与姿态恢复参考](../../references/unity-animation-preview-lifecycle-and-pose-restoration.md)。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 源码文件 | `Assets/Editor/TA_Tools/Character/AnimationClipPreviewer/Editor/AnimationClipPreviewer.cs`。 |
| 菜单入口 | `TA_Tools/Animation/动画预览`。 |
| 窗口类型 | `SK.Framework.AnimationClipPreviewer`。 |
| 程序集边界 | 当前目录无独立 asmdef，由 `Assembly-CSharp-Editor` 覆盖；代码位于 Editor 目录，不进入 Player。 |
| 最小窗口 | `700 × 320`，左侧控制、右侧 Clip 搜索列表。 |
| 目标来源 | 窗口打开时读取一次；之后由“读取当前选择”显式更新。 |
| Clip 来源 | 已锁定 Animator 中第一个有效 `runtimeAnimatorController` 的 Clip 集合。 |

该工具属于现有 Character 工具的小步维护，不移动目录、菜单、命名空间或程序集。仓库中存在其他动画预览实现，定位问题时必须先用窗口可见文案 `读取当前选择`、`回到起点`、`重采样`、`恢复 A/T Pose`、`取消预览` 匹配当前文件，不能因为类名相似就修改平行工具。

## 2. 当前功能与实现方式

### 2.1 目标锁定与 Selection 行为

当前目标契约如下：

- `OnEnable()` 调用一次 `RefreshSelectionContext()`，并注册 `EditorApplication.update`。
- 顶部“读取当前选择”按钮是之后唯一的主动换目标入口。
- 当前实现**有意不提供** `OnSelectionChange()` 自动刷新。预览时选择灯光、骨骼、材质或其他对象不会改变 `_animators`，也不会退出 Animation Mode。
- `RefreshSelectionContext()` 会先 `ExitAnimationPreview()`，再清空并从 `Selection.gameObjects` 收集直接挂有 `Animator` 的对象；重复对象会去重。
- 没有 Animator 或 Controller 时清空 Clip 并停止播放，不进入异常状态。

不要把状态文案改回“选中 Animator 数量”。Selection 已经可以和预览目标不同，当前 UI 使用“已锁定 Animator 数量”表达真实所有权。

### 2.2 Clip 收集、搜索与选择

Clip 列表通过以下流程建立：

1. 从首个有效 Controller 读取 `animationClips`。
2. `Distinct()` 去重。
3. `IsPreviewableAnimationClip()` 排除只有 `SkinnedMeshRenderer` `blendShape.*` 曲线、且没有对象引用曲线的纯表情 Clip。
4. 按 `clip.name` 使用 `StringComparer.Ordinal` 排序。
5. 右侧以不区分大小写的名称搜索、滚动列表、时长和当前高亮展示。

点击 Clip 行调用统一的 `PlayClipAtIndex()`：索引校验 → 当前时间归零 → 确保 Animation Mode → 采样第 0 帧 → 对有效长度 Clip 开始播放。搜索只改变可见项，不改变 `_clips` 的稳定索引。

### 2.3 播放更新与采样

`OnEditorUpdate()` 只在 `_isPlaying` 时工作：

- 用 `EditorApplication.timeSinceStartup` 计算 `deltaTime`。
- 归一化时间增量为 `deltaTime * _playbackSpeed / clip.length`。
- 超过 1 时减去 `Mathf.Floor()`，保持循环余量。
- 调用 `SampleCurrentClip()` 后刷新窗口。

`SampleCurrentClip()` 先取得当前 Clip 并调用 `EnsureAnimationMode()`。采样通过 `AnimationMode.BeginSampling()` / `EndSampling()` 的 `try/finally` 成对保护；对每个非空 Animator 调用 `AnimationMode.SampleAnimationClip()`，最后刷新全部 SceneView。

播放速度变化和重新开始播放都会更新 `_lastUpdateTime`，避免暂停或调速后时间突跳。

### 2.4 Animation Mode 所有权

当前工具以 `_ownsAnimationMode` 记录会话所有权：

- 已拥有且全局模式仍有效时继续使用。
- 所有权标记陈旧时先清除。
- 全局已在 Animation Mode 但本工具不拥有时，设置 `_animationModeConflict`、停止播放并显示警告。
- 全局空闲时才 `StartAnimationMode()`。
- `ExitAnimationPreview()` 只有在本工具拥有且全局仍处于 Animation Mode 时才 `StopAnimationMode()`。

任何新入口都必须复用 `EnsureAnimationMode()` / `ExitAnimationPreview()`，不能直接散落 `StartAnimationMode()` 或无所有权地调用 `StopAnimationMode()`。

### 2.5 按钮语义

| 按钮 | 当前行为 |
| --- | --- |
| 播放/暂停 | 切换 `_isPlaying`；暂停保留当前采样姿态和 Animation Mode。 |
| 停止 | `_isPlaying = false`，时间归零并重新采样；角色停在动画第一帧。 |
| 取消预览 | 调用 `ExitAnimationPreview()`；退出本工具 Animation Mode，恢复开始预览前的临时姿态。 |
| 回到起点 | 与“停止”的时间归零采样语义一致。 |
| 重采样 | 在当前归一化时间重新执行采样。 |
| 恢复 A/T Pose | 退出临时预览后，从候选模型资源复制骨骼 local Transform，并登记 Undo。 |

“取消预览”是为保留旧 Selection 自动退出体验而增加的显式入口。以后不能把它合并进“停止”，否则会再次丢失“停在第一帧”和“恢复预览前姿态”的区分。

## 3. FBX A/T Pose 恢复链路

### 3.1 源模型发现

`ResolveModelPoseSourceRoot()` 从当前 Animator 的以下引用收集候选资产路径：

- 子层级 `SkinnedMeshRenderer.sharedMesh`。
- `Animator.avatar`。

路径按不区分大小写去重。`LoadModelPoseSourceRoot()` 只接受 `AssetImporter.GetAtPath()` 为 `ModelImporter` 的资产，再用 `AssetDatabase.LoadAssetAtPath<GameObject>()` 加载模型。源根优先模型根上的 Animator，其次模型根名称匹配，最后使用首个子 Animator 或模型根。

### 3.2 层级匹配和复制

`TransformPoseData.Capture()` 递归记录：

- 节点名。
- `localPosition`。
- `localEulerAngles`，写入时转换为 `Quaternion.Euler()`。
- `localScale`。

每个候选源模型都通过 `FindBestPoseTargetRoot()` 在当前 Animator 全部子 Transform 中计算递归子节点名称匹配数，选择最高分源根和目标根。写入时使用 `ApplyChildren()`，因此保留目标匹配根自身 Transform，只覆盖匹配的后代节点。

写入前调用：

```csharp
Undo.RegisterFullObjectHierarchyUndo(animator.gameObject, "恢复 FBX 原始姿态");
```

写入后 `EditorUtility.SetDirty(animator.gameObject)`，并按恢复 Animator 数与 Transform 数显示 Info/Warning。零匹配不会谎报成功。

### 3.3 当前边界

- 该实现按“父节点上下文中的直接子节点名”递归匹配，不是全局同名查找。
- 它依赖导入模型保留可访问层级；`Optimize Game Objects`、DCC 改名、骨架重排或不同模型来源可能导致部分/零匹配。
- 当前目标只读取选中 GameObject 自身的 `GetComponent<Animator>()`，不会自动向父/子级搜索 Animator。
- 当前使用 Euler 值捕获和回写；如果后续发现旋转表示差异，应基于真实模型验证后再考虑直接保存 Quaternion，不能无证据改写。
- 该操作会持久修改 Scene/Prefab 实例 Transform，与“取消预览”的临时恢复不同；维护 UI 和文档时必须继续明确 Undo/Prefab Override 边界。

## 4. 已确认问题、根因和解决方式

### 4.1 Selection 改变导致动画中断和 T-Pose

**现象**：角色播放后，鼠标离开窗口并在 Hierarchy/Project 选择其他对象，动画立即停止，角色恢复 T-Pose。

**根因**：旧 `OnSelectionChange()` 无条件调用 `RefreshSelectionContext()`；该方法第一步调用 `ExitAnimationPreview()`，从而退出当前 Animation Mode。

**修正**：删除自动 Selection 刷新，把目标改为显式快照。窗口打开读取一次，之后只在点击“读取当前选择”时切换目标。

**维护规则**：不得重新添加无条件 `OnSelectionChange()`。如将来需要跟随选择，必须做成显式选项，并在播放中默认保持当前锁定目标。

### 4.2 锁定目标后缺少恢复原姿态入口

**现象**：修正自动中断后，用户可以选择其他对象而保持播放，但需要一个按钮恢复修改前“离开工具后回到 T-Pose”的结果。

**根因**：现有“暂停”只关闭播放标记；“停止”会采样第 0 帧；两者都不退出 Animation Mode。

**修正**：增加“取消预览”按钮。仅在本工具拥有且全局处于 Animation Mode 时可用，点击后统一调用 `ExitAnimationPreview()`。

**维护规则**：取消预览只撤销临时采样；不能调用 `RestoreModelRestPose()`，也不能在未拥有会话时停止其他工具的 Animation Mode。

### 4.3 动画很多时选择困难

**现象**：长列表依赖普通 Popup 时需要大量向下滚动，难以按名称定位。

**修正结果**：当前界面使用右侧有界滚动列表与搜索框，显示可见数/总数、名称和时长；单击行从头采样并播放。

**维护规则**：搜索改变时只重置列表滚动，不重建目标或退出预览；过滤结果不能改变底层 Clip 索引。

### 4.4 恢复姿态不能用 Bind Pose 推断替代

**问题**：曾考虑用 `Mesh.bindposes`、骨骼索引或临时 AnimationClip 重建姿态，但它们不等价于用户要求的“直接读取 FBX 每个骨骼 Transform”。

**最终方式**：复用现有 Pose Copy 工具的语义，直接捕获源模型层级 local Transform，按名称层级匹配目标，并用 Undo 写入。

**维护规则**：需求仍是 FBX 原始 Transform 时，不回退到 Bind Pose 矩阵推断；源模型无法匹配时应警告并停止，而不是生成看似合理但来源不同的姿态。

### 4.5 Unity 生成 csproj 不能代表最终 Editor 行为

**现象**：`Assembly-CSharp-Editor.csproj` 的独立 `dotnet build` 可能因 `Temp/bin/Debug/*.dll` 缺失产生大量 `CS0006`，这些错误不等于目标文件语法失败。

**解决方式**：当前文件曾用 Unity 2022.3.62f3 自带 Roslyn、Unity Managed DLL 和 .NET Reference Assemblies 做单文件补充编译，结果 `CSC_EXIT=0`。但独立 Roslyn 只能证明语法/API 引用，不替代 Unity Editor 内 Animation Mode、Selection 和姿态恢复验证。

## 5. 维护与扩展规则

1. 修改前先用可见文案匹配当前工具，避免误改其他动画预览窗口。
2. 保持 `OnEnable` 注册、`OnDisable` 注销 `EditorApplication.update`，并在禁用时退出本工具预览。
3. Selection 只通过窗口打开和“读取当前选择”进入目标快照；不要在 Update/OnGUI 中重复读取。
4. 新的播放、滑条、Clip 选择或快捷键入口统一调用 `EnsureAnimationMode()` 和 `SampleCurrentClip()`。
5. 新的退出、换目标、关闭或异常路径统一调用 `ExitAnimationPreview()`。
6. 只有本工具拥有会话时才停止 Animation Mode；外部冲突只提示并阻止。
7. 保持“暂停 / 停止 / 取消预览 / 恢复 A/T Pose”四种语义分离。
8. 多目标 Clip 来源当前是首个有效 Controller；若改为交集、并集或独立 Clip，必须单独确认兼容性和 UI。
9. 纯 BlendShape Clip 当前被过滤；若表情预览纳入需求，作为独立契约修改并验证骨骼/表情混合 Clip。
10. 源姿态恢复继续使用 Undo、匹配计数和零匹配 Warning；不得静默写入或宣称全部成功。
11. 不手改或提交 Unity 生成 `.csproj`；最终编译证据以 Unity Editor 为主。

## 6. 验证证据、未验证项与回退

### 已确认

- 当前源码中目标刷新入口只有 `OnEnable()` 和“读取当前选择”；不存在 `OnSelectionChange()` 自动刷新。
- `EditorApplication.update` 的注册/注销成对；窗口禁用会退出预览。
- `BeginSampling()` / `EndSampling()` 使用 `try/finally`。
- `ExitAnimationPreview()` 有 `_ownsAnimationMode` 所有权保护。
- “取消预览”复用 `ExitAnimationPreview()`，不调用持久姿态恢复。
- 当前文件曾完成 Unity 2022.3.62f3 Roslyn 单文件编译，`CSC_EXIT=0`；`git diff --check` 仅报告换行提示。

### 仍需 Unity 人工验证

1. 播放后分别选择其他角色、灯光、骨骼、材质和 Project 资产，确认原角色持续播放。
2. 验证暂停后继续不跳帧；停止停在第 0 帧；取消预览恢复开始前姿态。
3. 选择新角色并点击“读取当前选择”，确认旧角色恢复、新角色锁定。
4. 与 Animation Window、Timeline 或其他 Animation Mode 工具并行时，确认只提示冲突、不结束外部会话。
5. 关闭窗口、Domain Reload、进入 Play Mode 和删除目标对象时无残留回调/姿态。
6. 在真实角色执行“恢复 A/T Pose”，检查源根选择、Transform 写入数、部分/零匹配、Undo/Redo 和 Prefab Override。

### 回退边界

- 目标锁定异常时保留显式“读取当前选择”，不要直接恢复旧的无条件 Selection 自动刷新。
- “取消预览”异常时可临时禁用该按钮，但必须保留窗口关闭时的安全退出；不得改成无所有权 `StopAnimationMode()`。
- FBX 姿态匹配不可靠时只禁用“恢复 A/T Pose”，保留临时动画预览和取消能力。
- Unity 人工矩阵未完成前，文档状态保持“静态/编译验证，行为待验收”，不得表述为全部完成。

## 7. 项目维护检查单

- [ ] 路径、菜单、命名空间和 `Assembly-CSharp-Editor` 边界未被无关改动。
- [ ] Selection 改变不刷新目标；显式读取可以安全切换目标。
- [ ] 播放、暂停、停止、取消和姿态恢复语义没有合并。
- [ ] Animation Mode 所有权、采样配对、Update 订阅/释放完整。
- [ ] Clip 搜索、稳定索引、零长度和纯 BlendShape 过滤符合当前契约。
- [ ] 源模型候选、层级匹配、Undo、Dirty、计数和 Warning 完整。
- [ ] Unity Editor 完成代表性行为验证；命令行编译未被当作视觉/生命周期验收。
