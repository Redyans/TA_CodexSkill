# Timeline 动画后跟随与 Playable 生命周期参考

> 类型：REFERENCE；适用范围：自定义 Timeline Clip 需要读取 Animation Track、Animator 或约束系统写出的 Transform，并在编辑器预览、运行时播放、暂停和越过片段边界时保持一致。
>
> 关联规则：`TML-LIF-01`、`TML-LIF-02`、`TML-EDT-03`、`TML-VAL-01`。模块入口见 [Timeline 开发规则](../README_Tech_TimelineDevelopmentRules.md)；对象如何解析见 [Timeline 目标解析与嵌套挂点跟随参考](timeline-object-attachment-resolution.md)；编辑期数值刷新见 [Timeline 片段参数预览刷新与运行时数值同步参考](timeline-preview-refresh-and-live-value-sync.md)。

## 1. 适用场景

- 武器、道具、特效或临时模型需要跟随动画骨骼 / 挂点。
- 跟随对象大体正确，但快速动作时出现一帧滞后、动态偏差或偶发错位。
- 勾选“保持相对偏移”后出现稳定的固定偏差，且偏差与 Clip 首帧有关。
- Timeline 在编辑器里暂停时道具消失，继续播放又出现。
- 同一套 Behaviour 同时被不绑定 Track 和绑定 Track 复用，需要保证修复不会只落在一个入口。

本参考不负责“挂点解析到了谁”。如果同名对象、嵌套 Timeline 或绑定范围本身有歧义，先按对象解析参考排除目标错误，再处理同帧求值顺序。

## 2. 根因：读写之间没有同帧依赖

Animation Track 与自定义 Transform Track 通常产生不同的 Playable 输出。两条轨道都在 Timeline 面板里，不代表自定义 Behaviour 的 `ProcessFrame` 一定发生在动画结果写回 Transform 之后。

典型错误链路：

1. Timeline 开始求值本帧；
2. 跟随 Behaviour 的 `ProcessFrame` 先读挂点；
3. 读到的是动画写回前或上一帧的骨骼姿态；
4. 跟随目标按旧姿态写入；
5. 动画随后更新挂点，目标与挂点在本帧结束时不一致。

这种偏差会随动作速度和旋转幅度变化。动作越快越明显，暂停比较单帧时又可能看似正常。它与“偏移参数填错”不同，也不能靠调整 Timeline 轨道上下顺序获得稳定保证。

## 3. 方案选择

| 方案 | 时序强度 | 适用条件 | 主要代价 |
| --- | --- | --- | --- |
| 在 PlayableGraph 内建立明确依赖 | 最强 | 能控制图结构，且跟随逻辑可进入图内 | 实现和兼容成本最高 |
| Animation Job / 约束系统内完成跟随 | 强 | 目标属于动画管线，逻辑适合 Job 或约束 | 需要改造现有数据和动画链路 |
| `ProcessFrame` 即时写入 + 动画后的后置校正 | 中等且实用 | 需要兼容编辑器 `Evaluate`、暂停拖帧和已有 Track | 每个激活 Behaviour 多一次 Transform 写入与生命周期管理 |
| 只在 `LateUpdate` 写入 | 不完整 | 仅运行时播放且不要求暂停 / 拖帧即时反馈 | 编辑期单次 `Evaluate`、首帧或暂停调参可能不更新 |

无法低风险改造 PlayableGraph 时，推荐第三种双阶段模式：`ProcessFrame` 保留即时响应，后置阶段负责最终精度。

## 4. 双阶段写入模式

### 4.1 `ProcessFrame` 负责即时结果

`ProcessFrame` 中完成：

1. 同步 Clip 的实时数值；
2. 创建或取得跟随目标；
3. 解析挂点；
4. 保存目标原始状态；
5. 立即应用一次跟随；
6. 确保后置驱动存在。

这次写入不能省。它保证 Timeline 首帧 `Evaluate()`、编辑器拖动播放头、暂停状态改参数时立刻看到结果，也给尚未进入 `LateUpdate` 的求值路径一个合理状态。

### 4.2 后置阶段使用最终骨骼姿态校正

兼容型实现可以创建隐藏的 `MonoBehaviour` 驱动：

```csharp
[ExecuteAlways]
[DefaultExecutionOrder(32000)]
public sealed class FollowLateUpdateDriver : MonoBehaviour
{
    private FollowBehaviour m_Behaviour;

    private void LateUpdate()
    {
        m_Behaviour?.ApplyAfterAnimation();
    }
}
```

- `[ExecuteAlways]` 让编辑器预览也能校正；
- 高执行顺序用于晚于普通动画后处理，但具体数值是项目事实，不是跨项目常量；
- 驱动对象应使用 `HideFlags.HideAndDontSave`，避免污染场景和资产；
- `ApplyAfterAnimation()` 只读取已缓存目标与挂点，不在 `LateUpdate` 重扫层级或重新解析对象；
- 驱动必须由 Behaviour 显式绑定和解绑，不能依赖 Unity 最终销毁顺便清理引用。

如果项目还有 Animation Rigging、自定义 IK、约束组件或其他高顺序 `LateUpdate` 写骨骼，必须核对谁最后写 Transform。无法证明驱动更晚时，应升级为图内依赖或动画系统提供的完成回调。

### 4.3 “保持相对偏移”要延迟校准一次

首个 `ProcessFrame` 可能读到旧挂点姿态。如果直接用它计算“目标相对挂点”的偏移，这个首帧误差会被永久保存，最终表现为稳定的固定偏差。

稳妥做法：

1. Clip 首次激活时保存目标的原始世界位置和世界旋转；
2. `ProcessFrame` 可以先按当前挂点计算一次，保证即时显示；
3. 第一次后置校正取得最终挂点姿态后，用同一份原始目标世界位姿重算相对偏移；
4. 后续帧复用该偏移，不重复采样“已经被跟随移动过”的目标。

世界位姿转相对偏移时要与实际应用公式互为逆运算。例如应用是“挂点位置 + 挂点旋转 × 局部偏移”，捕获就应使用 `Quaternion.Inverse(mount.rotation) * (targetWorldPosition - mount.position)`，不要混入未参与应用的缩放。

### 4.4 多 Clip / 多写入者边界

一个激活 Behaviour 一个驱动的方式只适合写入目标互不冲突的情况。两个 Clip、两个 Layer 或两个 Director 同时写同一 Transform 时，即使它们都有相同的执行顺序，最终胜者也可能不稳定。

出现共享目标时必须二选一：

- 在 Mixer / 协调器里先聚合成唯一结果，再由一个后置驱动写出；
- 明确禁止重叠，并在编辑器校验或运行时诊断中报冲突。

## 5. 正确理解 `OnBehaviourPause`

`OnBehaviourPause` 的名字容易让实现误以为“Clip 结束了”。实际它可能由整张图暂停、Director 停止或 Clip 离开有效区间触发，清理动作不能只看回调名。

Timeline 1.7.7 自带 `PrefabControlPlayable` 的处理方式可作为版本内证据：只有 `info.effectivePlayState == PlayState.Paused` 时才隐藏实例；如果回调发生时有效播放状态仍为 `Playing`，它会保留实例。

推荐生命周期表：

| 事件 | 判定 | 临时 Prefab | 场景目标 | 后置驱动 |
| --- | --- | --- | --- | --- |
| Clip 内播放 | 正常 `ProcessFrame` | 激活 | 跟随 | 保留 |
| 图暂停但 Clip 仍有效 | `OnBehaviourPause` 且 `effectivePlayState == Playing` | 保留 | 保留当前跟随结果 | 保留 |
| Clip 离开有效区间 | `OnBehaviourPause` 且 `effectivePlayState == Paused` | 停用 | 恢复原始局部位姿 / 缩放 | 释放 |
| Playable 销毁 / 图重建 | `OnPlayableDestroy` | 销毁 | 幂等恢复 | 释放 |

这张表需要按目标 Timeline 版本复核。不要把 1.7.7 的内部语义未经验证直接复制到其他版本。

## 6. 诊断矩阵

| 现象 | 优先怀疑 | 快速验证 |
| --- | --- | --- |
| 偏差随动作速度变化，静止时接近正确 | 动画采样与跟随读取顺序 | 同帧对比挂点 Transform 与目标根 Transform；增加动画后校正验证 |
| 每帧方向一致的固定偏差 | 手填偏移、保持相对偏移首帧采样、Pivot | 关闭偏移；检查首次最终姿态重算；比较 Transform 而不是网格中心 |
| 只在某些轴不一致 | 轴掩码与未勾选轴的基准 | 全轴开启后复测，再检查初始世界位姿 |
| 缩放随父级变化异常 | 把 `lossyScale` 写进 `localScale` 后与目标父级再次叠加 | 在非 1 父级缩放下单独验证世界缩放 |
| Timeline 暂停后道具消失 | `OnBehaviourPause` 无条件隐藏 / 恢复 | 记录 `effectivePlayState`，对照 Clip 是否仍在有效区间 |
| Clip 出界后仍残留或继续跟随 | 驱动未释放，或只处理 Destroy | 越过 Clip 边界并检查隐藏驱动和目标状态 |
| 编辑器改参数画面闪一下 | 数值变化触发 `ContentsModified` 重建图 | 改用 `SceneNeedsUpdate` 与 live value 同步 |
| Scene 里看似有偏差但 Transform 数值一致 | 模型 Pivot、网格中心或蒙皮视觉中心 | 比较挂点 Transform 与目标根 Transform 的位置和旋转 |

排查时先把问题分成“动态偏差”与“固定偏差”。前者优先查时序，后者优先查偏移、Pivot、轴掩码与缩放空间，能显著缩短定位时间。

## 7. 常见错误

- 通过拖动 Timeline 轨道上下顺序修复同帧依赖；这不是稳定执行合同。
- 删除 `ProcessFrame` 写入，只保留 `LateUpdate`；暂停拖帧和单次 `Evaluate` 会失去即时反馈。
- 每个 `LateUpdate` 都重新搜索挂点或扫描 Director；后置阶段应只做缓存 Transform 的计算与写入。
- 在 `OnBehaviourPause` 一进入就隐藏 Prefab 或恢复场景目标；编辑器暂停会直接丢道具。
- 只在 `OnPlayableDestroy` 清理；Clip 出界但图仍存活时会继续写入。
- 用第一次错误挂点姿态计算保持偏移，此后即使每帧后置校正也会保留固定误差。
- 只比较可见网格中心，不比较真实挂点与目标根 Transform。

## 8. 最小验证矩阵

| 维度 | 用例 | 通过条件 |
| --- | --- | --- |
| 快速动画 | 手部 / 武器点快速平移和旋转 | 目标根 Transform 在帧末与挂点按公式一致，无一帧滞后 |
| 首帧 | 从 Clip 前一帧进入，或直接 Seek 到 Clip 内 | 首帧不跳位，立即显示目标 |
| 保持偏移 | 开启保持相对偏移后从不同时间进入 Clip | 初始相对位姿一致，不因首帧采样顺序产生固定偏差 |
| 编辑器拖帧 | 暂停并来回拖动播放头 | 当前帧立即更新，无需继续播放 |
| 图暂停 | 播放头停在 Clip 有效区间内暂停 | 道具和场景目标不消失 |
| Clip 边界 | 播放越过 Clip 末尾，再倒回重播 | Prefab 正确停用 / 重建，场景目标正确恢复，驱动不残留 |
| 图重建 | 修改结构性字段或重建 Director 图 | 临时实例销毁，场景目标幂等恢复，重新求值不漂移 |
| 两类目标 | 分别使用 Prefab 目标与场景目标 | 生命周期符合各自语义 |
| 两类 Track | 自动解析 Track 与绑定 Track 使用同一共享 Behaviour | 精度、暂停和退出行为完全一致，仅解析范围不同 |
| 冲突 | 两个 Clip / Layer 指向同一目标 | 有明确聚合结果或明确报冲突，不依赖写入先后 |

静态编译只能证明 API 和程序集边界成立，不能证明求值顺序。至少要在 Unity Timeline 窗口完成一次快速动作、暂停、拖帧、越界和重播的视觉与 Transform 数值验收。

## 9. 回退与升级边界

- 高顺序 `LateUpdate` 仍早于项目中的骨骼约束：改为约束完成后的回调，或把逻辑移入 PlayableGraph / Animation Job。
- 编辑器不执行后置驱动：保留 `ProcessFrame` 即时写入，并检查 `[ExecuteAlways]`、对象隐藏标记和 Editor Update；不能以运行时正确代替编辑器预览验收。
- 多写入者无法建立稳定优先级：停止使用每 Behaviour 一个驱动，升级为 Mixer / Director 级协调器。
- Timeline 包升级：重新阅读目标版本同类内置 Playable 的 Pause 判定，并重跑完整生命周期矩阵。
