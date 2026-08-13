---
name: unity-animation-preview-lifecycle-and-pose-restoration
description: Unity Editor 动画预览工具的目标锁定、AnimationMode 所有权、播放状态、取消预览、源模型姿态恢复、问题排查与验证参考。
---

# Unity 动画预览工具生命周期与姿态恢复参考

> 类型：REFERENCE；适用范围：使用 `EditorWindow`、`AnimationMode` 和 `AnimationClip` 对场景中 `Animator` 临时采样的 Unity Editor 工具；使用前提：必须按目标 Unity 版本、模型导入设置、骨架结构和现有动画工具重新验证。
>
> 关联规则：`TOOL-CMP-01`、`TOOL-ARC-02`、`TOOL-ARC-03`、`TOOL-UI-02`、`TOOL-ORG-01`、`TOOL-VAL-01`、`TOOL-VAL-02`。当前模块入口见 [工具开发规则](../README_Tech_TAToolDevelopmentRules.md)。

## 1. 适用场景

本参考适用于以下 Editor 工作流：

- 从当前 `Selection` 读取一个或多个带 `Animator` 的角色，在 Scene 中循环预览 `AnimationClip`。
- 预览开始后仍需选择灯光、骨骼、材质、Renderer 或其他对象，而不能因为 Selection 改变就中断角色动画。
- 需要明确区分“暂停”“停止到动画起点”“取消临时预览”和“把源模型姿态写入当前对象”。
- 动画数量较多，需要搜索、滚动、时长展示和可观察的当前片段状态。
- 需要从 FBX 等源模型复制本地 Transform，恢复 A-Pose/T-Pose，并支持 Undo。

本参考不适用于 Player 运行时动画系统、Timeline 正式播放、Animator Controller 状态机设计、Humanoid Retargeting 质量调试或修改模型导入器的骨架配置。运行时需求应使用 `Animator`、Playable 或 Timeline 的运行时 API，不能引用 `UnityEditor.AnimationMode`。

## 2. 前提与依赖

### 2.1 把四类状态拆开

动画预览窗口至少包含四组不同状态，不能都绑定到 Unity 当前 Selection：

| 状态 | 示例 | 生命周期 | 是否写入正式对象 |
| --- | --- | --- | --- |
| 目标快照 | 已锁定的 `Animator` 列表、Clip 列表、当前 Clip | 显式读取目标后保持，直到用户换目标、对象失效或窗口关闭 | 否 |
| 播放状态 | `_isPlaying`、归一化时间、速度、上次更新时间 | 播放/暂停/停止之间变化 | 否 |
| 预览会话 | 是否进入 `AnimationMode`、当前工具是否拥有该会话 | 首次采样时创建，取消预览或窗口关闭时释放 | 否，退出时应恢复开始预览前的临时状态 |
| 姿态写入 | 从源模型复制的 local Position/Rotation/Scale | 用户显式执行，并进入 Undo/Dirty 边界 | 是 |

核心原则是：**Selection 是输入来源，不是预览目标的持续所有权。** 如果 `OnSelectionChange()` 无条件重新收集目标，就会把“用户查看其他对象”误判为“用户要求切换角色”，进而退出 `AnimationMode`、停止动画并恢复原姿态。

### 2.2 先冻结按钮语义

以下动作必须在 UI、代码和帮助文本中保持不同语义：

| 动作 | 播放标记 | 当前时间 | `AnimationMode` | 角色结果 |
| --- | --- | --- | --- | --- |
| 播放 | `true` | 从当前时间继续 | 保持或创建 | 持续采样 |
| 暂停 | `false` | 保留 | 保持 | 停在当前采样姿态 |
| 停止 | `false` | 回到 `0` | 保持 | 停在动画第一帧 |
| 取消预览 | `false` | 清零 | 仅当本工具拥有时退出 | 恢复开始预览前的临时姿态 |
| 恢复 A/T Pose | `false` | 不再属于预览会话 | 先退出本工具会话；存在外部会话时阻止 | 将源模型姿态写入对象，并支持 Undo |

“停止”和“取消预览”不能共用一个含糊入口。美术用户说“取消动画、变回 T-Pose”时，通常需要的是退出临时预览，而不是采样动画第 0 帧。实际恢复结果是开始预览前的姿态；只有开始前本身为 T-Pose 时才保证回到 T-Pose。

### 2.3 明确状态转换

推荐状态转换如下：

| 输入 | 前置状态 | Transition | Effect |
| --- | --- | --- | --- |
| 打开窗口 | 无 | 读取一次当前 Selection，注册 Editor Update | 建立初始目标快照，不自动播放 |
| 读取当前选择 | 任意 | 退出本工具预览 → 重建目标和 Clip 列表 | 旧角色恢复，锁定新角色 |
| 选择 Clip | 有目标、有 Clip | 时间归零 → 确保预览会话 → 采样 | 从头播放或按产品契约仅采样 |
| 暂停 | 播放中 | 只关闭播放标记 | 保留当前姿态和会话 |
| 停止 | 会话中 | 关闭播放 → 时间归零 → 重采样 | 保留会话，停在第一帧 |
| 取消预览 | 本工具拥有会话 | 关闭播放 → `StopAnimationMode()` | 恢复预览前姿态 |
| Selection 改变 | 已锁定目标 | 默认不转换 | 当前角色继续播放 |
| 窗口关闭/禁用 | 任意 | 注销 Update → 退出本工具预览 | 不残留回调和预览状态 |
| 其他工具占用 AnimationMode | 本工具未拥有会话 | 标记冲突并阻止采样 | 不退出、不接管外部会话 |
| 恢复源模型姿态 | 无外部预览会话 | 注册 Undo → 匹配层级 → 写 Transform | 显示写入数与未匹配结果 |

若产品确实需要“跟随 Selection”，应提供显式 Toggle，并定义播放中切换、无 Animator 选择、多选和切换失败的行为；不要把隐式 `OnSelectionChange()` 当作唯一交互。

## 3. 功能实现方式

### 3.1 使用显式目标快照

推荐提供“读取当前选择”或 ObjectField 作为明确入口：

1. 先退出本工具拥有的旧预览会话。
2. 从 `Selection.gameObjects` 收集目标并过滤类型。
3. 去重后保存强类型对象引用，而不是每帧重新读取 Selection。
4. 从已锁定目标重建 Clip 列表、提示和按钮状态。
5. 目标列表为空时进入可理解的空状态，不自动接管随后每次 Selection 变化。

多目标预览必须定义 Clip 来源。常见方案包括只使用第一个有效 Controller 的 Clip 集合、求交集、求并集或为每个目标维护独立 Clip；不能在实现中隐式混用。对象被删除或引用失效时，采样循环至少跳过空引用；更严格的工具应清理失效目标，并在全部失效时停止播放和退出会话。

### 3.2 用 Editor Update 驱动播放

`EditorWindow` 动画不应依赖窗口焦点或鼠标事件。使用 `EditorApplication.update` 时需满足：

- `OnEnable` 注册，`OnDisable` 精确注销。
- 用 `EditorApplication.timeSinceStartup` 计算 `deltaTime`，不要把 `OnGUI` 调用频率当作时间源。
- 开始播放、恢复播放或修改速度时重置 `_lastUpdateTime`，避免暂停后第一帧时间跳跃。
- 使用 `normalizedTime += deltaTime * speed / clip.length`；零长度或近零长度 Clip 必须停止或跳过。
- 循环播放用减去 `Mathf.Floor(normalizedTime)` 的方式保留超出部分，避免大帧间隔导致累计漂移。
- 每次有效采样后刷新 Scene；窗口内时间和状态变化时刷新窗口。

### 3.3 显式声明 `AnimationMode` 所有权

`AnimationMode` 是 Editor 全局共享状态，不能看到 `AnimationMode.InAnimationMode()` 就假定归当前窗口所有。推荐维护 `_ownsAnimationMode`：

1. 若 `_ownsAnimationMode` 且仍处于 Animation Mode，可继续采样。
2. 若 `_ownsAnimationMode` 但全局模式已退出，先清除陈旧所有权。
3. 若全局已在 Animation Mode、当前工具却不拥有，显示冲突并阻止播放。
4. 只有全局空闲时才调用 `StartAnimationMode()`，并以调用后的实际状态确认是否取得所有权。
5. 退出时只有 `_ownsAnimationMode && AnimationMode.InAnimationMode()` 才调用 `StopAnimationMode()`。

这套边界可以防止一个预览窗口的“取消”按钮结束 Animation Window、Timeline 或另一个 TA 工具的预览会话。

### 3.4 采样必须成对结束

采样的最小安全结构是：

```csharp
AnimationMode.BeginSampling();
try
{
    foreach (Animator animator in targets)
    {
        if (animator != null)
        {
            AnimationMode.SampleAnimationClip(animator.gameObject, clip, sampleTime);
        }
    }
}
finally
{
    AnimationMode.EndSampling();
}
```

采样异常不能跳过 `EndSampling()`。进入采样前检查 Clip、目标和会话所有权；采样后调用 `SceneView.RepaintAll()`。不要通过直接修改 Animator Controller、shared Asset 或骨骼 Transform 来伪装临时预览。

### 3.5 大型 Clip 列表使用有界搜索

当 Controller 中有大量动作时，长 `EditorGUILayout.Popup` 会降低检索效率。优先选择：

- 固定高度或随窗口扩展的 `ScrollView`。
- 按名称不区分大小写筛选。
- 显示当前筛选数/总数、Clip 时长和当前选择。
- 单击行后复用统一的“切换 Clip → 时间归零 → 采样/播放”入口。
- 搜索变化时重置滚动位置，清除搜索时释放输入焦点。

是否排除纯 BlendShape 表情 Clip 属于工具契约。可以用 `AnimationUtility.GetCurveBindings()` 区分骨骼/普通曲线和 `SkinnedMeshRenderer` 的 `blendShape.*` 曲线，但必须按实际用途决定，不能把“表情 Clip 不显示”升级为所有工具的通用规则。

### 3.6 把临时取消与持久姿态恢复分开

`StopAnimationMode()` 只负责撤销当前 Animation Mode 产生的临时采样。它不等于“从 FBX 重新读取标准姿态”。当用户明确需要源模型 A-Pose/T-Pose 时，可以采用以下受控流程：

1. 退出本工具拥有的预览会话。
2. 若仍有其他工具占用 Animation Mode，阻止持久写入。
3. 从 `SkinnedMeshRenderer.sharedMesh`、`Animator.avatar` 或显式模型引用找到候选源模型。
4. 只接受可确认的模型导入资产；加载源模型层级。
5. 捕获每个源节点的 `localPosition`、`localRotation` 或等价旋转表示、`localScale`。
6. 按层级路径或“父节点上下文 + 子节点名”匹配目标，不用全局同名搜索。
7. 在写入前调用 `Undo.RegisterFullObjectHierarchyUndo()`。
8. 写入匹配节点，标记 Dirty，并报告匹配模型数、Transform 写入数与未匹配项。

直接复制源模型 local Transform 与从 `Mesh.bindposes` 反推姿态不是同一语义。Bind Pose 矩阵服务于蒙皮空间变换，不保证等于导入模型层级中用户期望的 A/T Pose。若需求明确要求“按 FBX 骨骼 Transform 恢复”，优先读取源层级，不生成临时 AnimationClip，也不使用未经验证的矩阵反推。

## 4. 常见问题、根因与解决方式

| 现象 | 常见根因 | 解决方式 |
| --- | --- | --- |
| 点击其他对象后角色立刻停止并恢复 T-Pose | `OnSelectionChange()` 无条件刷新目标，刷新入口先退出 Animation Mode | 改为显式目标快照；Selection 改变默认不影响已锁定目标 |
| 暂停后无法回到预览前姿态 | “暂停/停止”只改变播放标记或采样第 0 帧 | 增加“取消预览”，统一调用拥有者安全的退出入口 |
| 取消按钮把其他动画工具也关掉 | 未记录 `AnimationMode` 所有权 | 维护 `_ownsAnimationMode`，不停止外部会话 |
| 暂停后继续播放突然跳帧 | `_lastUpdateTime` 仍是暂停前时间 | 恢复播放和修改速度时重置时间基线 |
| 异常后 Editor 预览状态卡住 | `BeginSampling()` 与 `EndSampling()` 未用 `try/finally` 成对 | 以 `finally` 结束采样；窗口禁用时统一退出 |
| 动作很多，选择需要滚动很久 | 使用无搜索的长 Popup | 改用有界搜索列表或可搜索 Dropdown |
| “恢复 T-Pose”得到奇怪姿态 | 把 Bind Pose、动画第一帧或 Humanoid Avatar 误当源模型 Transform | 先确认语义；需要 FBX 原始层级时直接复制源 local Transform |
| 多个模型只恢复了一部分 | 源/目标根节点错误、同名骨骼、Optimize Game Objects、不同层级或多个模型来源 | 按候选层级匹配分数选择根，报告写入数；不静默宣称全部成功 |
| 命令行编译通过但交互仍错误 | 生命周期和 Scene 恢复属于 Editor 行为，静态编译不覆盖 | 在 Unity 中执行 Selection、取消、关闭窗口和冲突矩阵 |

## 5. 风险与不适用边界

- `AnimationMode` 的具体行为依赖 Unity Editor 版本；升级后要重新验证进入、采样、退出和与 Animation Window/Timeline 的冲突。
- `Optimize Game Objects`、Humanoid Avatar、导入剥离骨骼、嵌套 Prefab 和模型实例重排可能让源/目标层级无法一一对应。
- 按子节点名匹配要求同一父节点下名称稳定；存在重名、重定向骨骼或 DCC 重命名时应升级为路径映射或显式映射表。
- 直接写 Transform 会修改 Scene/Prefab 实例，应有 Undo、Dirty 和 Prefab 覆盖边界；不要把它当作无副作用预览。
- 多角色统一使用一个 Clip 只在绑定路径兼容时可靠。不同骨架或不同 Controller 应分组处理或明确拒绝。
- Domain Reload、进入 Play Mode、目标对象销毁和窗口被强制关闭都必须作为恢复场景验证，不能只测试正常点击按钮。

## 6. 验证与回退

### 6.1 最小验证矩阵

| 场景 | 预期 |
| --- | --- |
| 无 Selection 打开窗口 | 显示可行动空状态，无异常、无 Animation Mode 残留 |
| 选择一个/多个 Animator 并读取 | 目标数和 Clip 数正确，不自动播放 |
| 播放后选择灯光、骨骼、材质或其他对象 | 已锁定角色继续播放，进度继续增长 |
| 暂停 | 保持当前姿态；再次播放从当前时间继续且不跳帧 |
| 停止 | 停在动画第一帧，仍处于本工具预览会话 |
| 取消预览 | 退出本工具 Animation Mode，恢复预览前姿态 |
| 选择新角色并再次读取 | 旧角色恢复，新角色成为唯一目标 |
| 其他工具已占用 Animation Mode | 当前工具阻止采样并提示，不关闭外部会话 |
| 关闭窗口/Domain Reload/进入 Play Mode | Update 注销，无残留临时姿态或全局模式 |
| Clip 为 null、零长度、纯 BlendShape | 按工具契约跳过或提示，不产生除零和无穷时间 |
| 源姿态恢复成功/部分匹配/零匹配 | 分别显示正确计数和 Warning；Undo/Redo 可用 |
| 多目标、不同骨架、目标被删除 | 不抛异常；不兼容目标有明确跳过或停止策略 |

### 6.2 分层验证

1. **静态检查**：事件注册/注销、`BeginSampling`/`EndSampling`、所有权判断、空引用、零长度、UI 文案和退出入口。
2. **编译检查**：以目标 Unity Editor 的实际程序集编译为主；生成 `.csproj` 或独立 Roslyn 仅作补充。
3. **Editor 行为**：实际打开窗口执行上表，观察 Scene、Hierarchy Selection、Animation Mode 状态和窗口状态。
4. **姿态结果**：对目标 Prefab/Scene 实例比较源模型骨骼 local Transform，检查写入数、Undo、Prefab Override 和保存/重开。

### 6.3 回退策略

- 目标锁定出现问题时，优先保留显式“读取当前选择”，临时禁用自动跟随，而不是恢复无条件 `OnSelectionChange()`。
- Animation Mode 所有权不可靠时，先禁用播放/取消入口并显示冲突，不要强制 `StopAnimationMode()`。
- 源姿态匹配证据不足时，保留临时预览取消，只禁用持久姿态恢复；不要回退到 Bind Pose 推断并伪称 FBX 原始姿态。
- Unity 人工验证未完成时，交付中明确记录静态/编译证据与剩余场景，不把“可编译”表述为“生命周期已验证”。
