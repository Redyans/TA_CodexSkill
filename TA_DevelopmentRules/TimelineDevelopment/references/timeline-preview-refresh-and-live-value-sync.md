# Timeline 片段参数预览刷新与运行时数值同步参考

> 类型：REFERENCE；适用范围：自定义 Clip 的可编辑参数需要在编辑器预览中"改完立刻生效"，并且调参时不能让整幅预览画面闪动；使用前提：必须以目标 Unity/Timeline 版本的 `TimelineEditor.Refresh`、`WindowState` 与 `ClipInspector` 实现重新验证，本文档的行为描述基于 Timeline 1.7.x。
>
> 关联规则：`TML-CMP-01`、`TML-EDT-01`、`TML-EDT-03`、`TML-LIF-02`、`TML-VAL-01`。当前模块入口见 [Timeline 开发规则](../README_Tech_TimelineDevelopmentRules.md)；涉及动画后跟随、暂停不隐藏与 Clip 边界清理时，同时读取 [Timeline 动画后跟随与 Playable 生命周期参考](timeline-post-animation-follow-and-playable-lifecycle.md)。

## 1. 适用场景

- 自定义 Clip 带有用户会在 Inspector 里反复拖动或输入的参数：偏移、轴开关、倍率、名称/路径文本。
- 现象符合以下任一组合：
  - 拖动 Clip 数值时，Game 视图（透过后置相机看的那一路）画面闪动、位移；
  - Scene 视图基本正常，只有 Game 视图动；
  - 把关掉相机轨（如 Cinemachine 虚拟相机轨）或动画轨后现象消失；
  - 画面不是持续抖，而是在"改一次参数"时闪一下。
- 需要一个"改完立刻生效"但"不重建 playable 图"的刷新策略。

本参考不适用于：改变 playable 图结构的操作（新增/删除轨道片段、换绑定对象、换 Clip 类型、换 Prefab 实例），这些本来就必须重建图；也不适用于"静止状态下持续抖动"的问题，那属于逐帧写值或 Repaint 抖动。

## 2. 前提与依赖

### 2.1 `RefreshReason` 的三条分支

`TimelineEditor.Refresh(RefreshReason)` 是异步的（下一次 GUI 循环生效），三条分支行为完全不同：

| Reason | 内部动作 | 代价 |
| --- | --- | --- |
| `ContentsAddedOrRemoved` | `state.Refresh()` | 最重 |
| `ContentsModified` | `state.rebuildGraph = true` | 重建整张图，会退出预览 |
| `SceneNeedsUpdate` | `state.Evaluate()` | 只按当前时间重算一次 |

多个 reason 可以用 `|` 组合，判断顺序是先 `ContentsAddedOrRemoved`、再 `ContentsModified`、最后 `SceneNeedsUpdate`。

### 2.2 重建图为什么会闪

`state.rebuildGraph` 在下一次重绘时由 `RebuildGraphIfNecessary()` 消费，顺序是：

1. 记录当前时间与播放标记；
2. `state.previewMode = false`；
3. `RebuildPlayableGraph()`（`director.RebuildGraph()`）；
4. 恢复时间，必要时重播；
5. `EvaluateImmediate()` / `Evaluate()`。

其中第 2 步是关键：`previewMode` 的 setter 在退出时依次执行 `Stop()`、`OnStopPreview()`、`AnimationMode.StopAnimationMode(previewDriver)`。`AnimationMode` 会把它记录过的属性**还原到进入动画模式之前的值**——相机轨、动画轨驱动的对象都会先回到编辑态，再被重建后的图重新写入。对着数值连续拖动时，这个"停一次 → 重建 → 重播"的循环就是画面闪动、位移的来源。

因此：**闪动不是自定义轨在写值的错，而是刷新 reason 触发了预览退出。**把相机轨静音后现象消失，正是因为它不再被 `AnimationMode` 还原和重写。

### 2.3 Inspector 侧入口

自定义片段 Inspector 实现 `IInspectorChangeHandler`（`ClipInspector` 在字段写回后会调用 `OnPlayableAssetChangedInInspector()`），刷新请求应在这里按字段类别派发。该回调不区分"改了什么字段"，类别必须由面板自己记录。

## 3. 实现或排查步骤

### 3.1 先确认闪动来自图重建

| 观察 | 结论 | 下一步 |
| --- | --- | --- |
| 静音/关闭相机轨、动画轨后不再闪 | 闪动来自重建时的预览退出与恢复 | 按 3.2 分类字段 |
| 只有 Game 视图动，Scene 视图不动 | 预览相机被还原过 | 同上 |
| 不编辑参数、静止时也在闪 | 与本文档无关 | 检查逐帧写值、`Repaint`、Scene 脏标记 |
| 换绑定对象/预制体才闪 | 属于结构性变更，允许闪一次 | 无需修改 |

### 3.2 把 Clip 字段按"是否影响图结构"分类

| 字段类别 | 典型字段 | 编辑器刷新 | 运行时取值 |
| --- | --- | --- | --- |
| 影响图结构 | 目标物体引用、`ExposedReference`、绑定对象、Clip 类型、需要重新实例化的 Prefab | `ContentsModified`（重建图，允许闪一次） | 建图时解析一次 |
| 只影响数值 | 偏移、轴开关、倍率、名称/路径文本、开关位 | `SceneNeedsUpdate`（只 Evaluate） | 每帧从 Clip 资产读取 |

判断标准：这个字段变化后，playable 图的**节点结构或解析结果**是否需要重做。只需要换一个数值的，一律归第二类。

### 3.3 仅数值字段改为"运行时每帧从片段读"

- `CreatePlayable` 时把片段资产自身交给 Behaviour（例如 `behaviour.clipAsset = this`）；
- Behaviour 在 `ProcessFrame` 开头把数值字段同步到自己的字段上；
- 结构性字段（目标引用、`ExposedReference.Resolve`）仍在 `CreatePlayable` 时解析一次。

推荐给这个同步动作一个显式入口（例如 `SyncLiveValues(behaviour)`），并在片段基类里统一实现，避免每个具体片段重复；同步内容要覆盖全部"仅数值"字段，漏写会导致某个字段改完不生效。

这个模式同时解决了另一个诉求：挂点名称/路径这类文本变化只需要就地重新解析一次目标，不必重建图。

### 3.4 Inspector 侧两级派发

```csharp
public void OnPlayableAssetChangedInInspector()
{
    if (!hasChanges) return;
    hasChanges = false;

    // 换目标物体：需要重新实例化 / 重新解析 ExposedReference，只能重建图
    if (targetChanged)
    {
        targetChanged = false;
        TimelineEditor.Refresh(RefreshReason.ContentsModified);
        return;
    }

    // 纯数值改动：只让预览按新数值重算一次
    TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
}
```

- 自绘面板需要在绘制目标物体字段时记录"本帧该字段是否变化"（例如让 `DrawTargetFields(...)` 返回 `bool`，变化时累积到 `targetChanged`）；
- 依赖 `serializedObject.ApplyModifiedProperties()` 作为"本次确实有改动"的门槛，避免无变化的 Repaint 也触发刷新；
- 两个标记都要在派发后清零，防止下一次无关改动被误判为结构性变更。

### 3.5 不要为了"完全不闪"而省掉刷新

Timeline 暂停在某一帧时不会自己重跑 `PlayableBehaviour.ProcessFrame`。如果纯数值改动时"什么都不刷"，物体就不会动，用户会认为参数失效。

`SceneNeedsUpdate` 走的是 `state.Evaluate()`：只按当前时间重算一次，不退出预览、不重建图，既能让新数值生效，也不会闪。

### 3.6 预览期间写过的状态要还原，但不能把所有 `OnBehaviourPause` 都当成 Clip 结束

跟随、采样、临时覆盖这类 Behaviour 会修改场景对象；在编辑器里每次重建图都会先停一次预览，如果没有在**图销毁**路径上还原，就会留下"已经被改过"的状态，下一次跟随把它当成初始基准（未勾选的轴、倍率都吃这个基准），表现为反复调参后**越调越偏**。

但 `OnBehaviourPause` 也会在整张图暂停、而 Clip 仍处于有效区间时触发。此时无条件隐藏实例或恢复场景目标，会表现为“编辑器一暂停，道具就消失”。

- `OnBehaviourPause` 先按目标 Timeline 版本的 `FrameData.effectivePlayState` 等上下文区分“图暂停”和“Clip 出界”；图暂停时保留当前目标，Clip 出界时才执行停用 / 还原；
- `OnPlayableDestroy` 始终走最终清理，销毁临时实例并兜底还原场景目标；
- Pause 与 Destroy 共用的还原 / 释放方法必须幂等，重复调用不产生新副作用；
- 一次性资源仍要区分“Clip 出界时停用”和“图销毁时销毁”两种语义。

### 3.7 解析结果有缓存时，编辑器校验要强制刷新

如果对象解析链带限时缓存（见 [Timeline 对象解析与挂点跟随参考](timeline-object-attachment-resolution.md)），编辑器校验必须带"忽略缓存"参数，否则会显示上一次的过期结论，让用户误以为改动没生效。

## 4. 风险与不适用边界

| 边界 | 说明 |
| --- | --- |
| 版本依赖 | `RefreshReason` 的分支、`previewMode` setter、`RebuildGraphIfNecessary()` 都属于 Timeline 编辑器内部实现，升版本后必须重新核对；本文结论基于 Timeline 1.7.x。 |
| Play Mode 差异 | 进入 Play Mode 后走的是运行时求值路径，不能拿编辑器里的刷新行为推断运行时性能。 |
| 字段所有权 | 数值改为每帧读片段后，运行时代码对同名字段的写入会被持续覆盖；需要运行时回写就必须另定所有权与优先级。 |
| 不能过度收敛 | 结构性变更该重建就重建。把 Prefab 实例、`ExposedReference` 也塞进 live 读取是错的。 |
| 与录制/混合的关系 | 本参考只解决"编辑期刷新"，不改变混合与恢复语义；混合规则仍以模块 CORE 为准。 |

## 5. 验证与回退

| 层级 | 检查 | 通过条件 |
| --- | --- | --- |
| 编译 | 目标程序集编译通过 | 无新增错误 |
| 预览实时性 | 在含相机轨/动画轨的 Timeline 上连续拖动偏移、轴开关、倍率 | Game 视图无闪动、无位移，目标连续跟随 |
| 暂停生效 | Timeline 暂停在某一帧时修改数值 | 目标立刻更新，无需移动播放头 |
| 结构性变更 | 更换目标物体 / 重新指定 Prefab | 允许闪一次，行为正确 |
| 恢复语义 | 片段结束、图重建、Director 释放 | 场景目标还原到跟随前位姿；反复编辑后不漂移 |
| 缓存一致 | 修改挂点文本后立即看面板校验 | 显示的是本次结果，不是上次缓存 |
| 回退 | 目标版本不支持该刷新语义 | 回退到 `ContentsModified`，并在交付说明中写明"编辑期会有一次预览重建" |
