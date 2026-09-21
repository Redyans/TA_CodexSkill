---
name: timeline-relative-pose-authoring
description: Timeline 跟随类功能中相对位姿、偏移倍率、Scene Handle、动态捕获与编辑器烘焙的可迁移实现参考。
---

# Timeline 相对位姿与偏移编辑参考

> 类型：`REFERENCE`；适用范围：Timeline Clip 让模型、道具、特效或其他 `Transform` 相对挂点保持位置/旋转，并同时支持运行时捕获、编辑器烘焙和 Scene 视图调整。使用前必须按目标 Unity/Timeline 版本重新验证 Playable 求值顺序、`AnimationMode`、`RefreshReason` 和 Transform 缩放语义。
>
> 关联规则：`TML-CMP-01`、`TML-ARC-03`、`TML-LIF-01`、`TML-LIF-02`、`TML-EDT-01`、`TML-EDT-03`、`TML-VAL-01`。模块入口见 [Timeline 开发规则](../README_Tech_TimelineDevelopmentRules.md)；挂点解析见 [Timeline 目标解析与嵌套挂点跟随参考](timeline-object-attachment-resolution.md)；动画后校正和生命周期见 [Timeline 动画后跟随与 Playable 生命周期参考](timeline-post-animation-follow-and-playable-lifecycle.md)；无闪预览见 [Timeline 片段参数预览与运行时数值同步参考](timeline-preview-refresh-and-live-value-sync.md)。

## 1. 适用场景

- 模型或道具需要跟随骨骼、挂点、插槽、空物体或另一个物体的 `Transform`。
- 美术希望把模型摆到当前视觉位置后保持相对关系，而不是手算世界坐标。
- 偏移量级较大但制作时需要小范围精调，例如原始值为 `1-100`，实际倍率为 `0.01-1`。
- 需要同时支持 Inspector 数值、Scene Move/Rotate Tool 和运行时捕获。
- 需要把一次捕获结果固定成可见、可审查、可继续微调的字段。

本参考不适用于固定序列化引用、复杂多对象约束混合，或多个写入者竞争同一个 `Transform` 却没有明确仲裁规则的场景。

## 2. 先定义相对位姿

### 2.1 使用挂点局部空间

不要把两个世界坐标直接相减作为完整偏移。挂点会旋转，目标应保存相对于挂点坐标轴的位移和相对旋转：

~~~text
relativePosition = inverse(mount.rotation) * (target.position - mount.position)
relativeRotation = inverse(mount.rotation) * target.rotation
~~~

应用必须是逆运算：

~~~text
target.position = mount.position + mount.rotation * relativePosition
target.rotation = mount.rotation * relativeRotation
~~~

捕获与应用不互为逆运算时，会出现固定偏差或偏移方向错误。

### 2.2 区分动态捕获与编辑器烘焙

| 模式 | 触发时机 | 数据位置 | 适合场景 | 风险 |
| --- | --- | --- | --- | --- |
| 动态捕获 | Clip 首次有效时 | `Behaviour` 运行态 | 上游动画或脚本决定进入时位置 | 从不同时间 Seek 可能得到不同基准 |
| 编辑器烘焙 | 用户点击获取按钮 | 写入 Clip 偏移字段 | 结果稳定、可见、可审查、可微调 | 必须先解析唯一挂点 |

推荐同时提供两者：复选框表达运行时动态保持，按钮表达当前位姿转成可编辑数据。按钮完成后关闭动态模式，避免两个偏移来源互相覆盖。

### 2.3 保存原始目标位姿

目标已经被跟随逻辑写过后，再读取它会得到被系统修改过的结果，容易形成漂移。推荐：

1. Clip 首次生效时保存目标原始世界位置和旋转。
2. 先按当前挂点算一次偏移，保证首帧立即显示。
3. 第一次取得最终挂点姿态后，用保存的目标位姿重算。
4. 后续只复用相对偏移，不再从已跟随目标采样。

## 3. 偏移倍率

### 3.1 分离原始值与有效值

建议序列化原始值，运行时统一计算有效值：

~~~text
effectivePositionOffset = positionOffset * (offsetScaleEnabled ? clamp(offsetScale, 0.01, 1) : 1)
effectiveRotationOffset = rotationOffset * (offsetScaleEnabled ? clamp(offsetScale, 0.01, 1) : 1)
~~~

- 倍率开关默认关闭，保证旧资产行为不变。
- 倍率建议限制为 `0.01-1`，并在运行时再次 Clamp。
- Scene Handle 显示、运行时应用、烘焙和反算必须使用同一个有效倍率。
- Handle 写回时必须反算：`rawOffset = effectiveOffset / effectiveScale`，否则会重复缩放。
- 旋转写回前把欧拉角归一化到 `-180..180`，避免 `359` 与 `-1` 之间跳变。

烘焙流程是：先计算模型相对挂点的真实局部位姿，再除以有效倍率写入原始位置和旋转字段。这样点击按钮前后，目标实际世界位姿不应跳变。

## 4. Scene Handle

### 4.1 Handle 是同一字段的第二个编辑入口

Inspector 数值框和 Scene Handle 必须编辑同一组序列化字段，不要维护隐藏副本。推荐流程：

1. 确认当前选中的 Timeline Clip。
2. 用运行时同一解析器解析唯一挂点。
3. 按跟随轴取有效偏移。
4. 计算世界 Pivot。
5. Move 或 Rotate Tool 修改世界结果。
6. 用挂点逆旋转转回局部偏移，再除以倍率写回原始字段。
7. 调用 `Undo.RecordObject`、标记脏、请求 `SceneNeedsUpdate`、重绘 `SceneView`。

位置 Pivot 和反算：

~~~text
pivotWorld = mount.position + mount.rotation * effectivePositionOffset
effectiveOffset = inverse(mount.rotation) * (nextPivotWorld - mount.position)
~~~

旋转初值和反算：

~~~text
currentRotation = mount.rotation * Quaternion.Euler(effectiveRotationOffset)
effectiveRotation = inverse(mount.rotation) * nextRotation
~~~

不要直接用世界欧拉角减挂点欧拉角；复合旋转下欧拉角不是可交换的线性空间。

### 4.2 轴掩码和动态模式

- 未勾选轴保持目标跟随前的基准值，不要强行清零。
- Handle 只写入已勾选轴，其他轴保留原始字段。
- 动态捕获会忽略手填偏移，因此应隐藏或禁用 Scene Handle，并给出原因提示。
- 关闭动态模式后立即恢复 Handle。
- “所有轴未勾选”是兼容语义时，要明确它映射为 `All` 还是 `None`。

## 5. 职责分层

| 层 | 保存/处理 | 不应承担 |
| --- | --- | --- |
| Track | 绑定约束、具体 Clip 类型、Mixer 创建 | 每个 Clip 的偏移字段 |
| Clip/基类 | 序列化字段、默认值、有效倍率、运行时同步 | 搜索场景、直接绘制 Handle |
| Behaviour | 缓存目标/挂点、Transform 应用、恢复、生命周期 | Editor GUI、Undo |
| 解析器 | 名称/路径、绑定优先、歧义、嵌套 Director 链 | 写 Transform、显示 UI |
| Inspector | `SerializedProperty`、按钮、错误提示、刷新 | 复制运行时解析算法 |
| Scene Handle | 可视化编辑和反算 | 写另一套隐藏字段 |
| 后置驱动/协调器 | 动画后校正或多写入者仲裁 | 每帧扫描层级 |

两条轨道若只在挂点来源上不同，应共享基类、Behaviour、Inspector 绘制和解析器，只通过绑定策略区分自动解析轨和显式绑定轨。

## 6. 刷新与生命周期

偏移、轴开关、倍率、挂点文本通常只影响数值，适合 `Behaviour` 每帧同步并请求 `SceneNeedsUpdate`；目标引用、`ExposedReference`、Prefab 实例化和轨道绑定属于结构变化，才使用 `ContentsModified`。

~~~csharp
clip.SyncLiveValues(behaviour);
TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);

// 结构字段变化时：
TimelineEditor.Refresh(RefreshReason.ContentsModified);
~~~

预览期间写过 Transform 时，必须覆盖：

- Clip 出界：停用临时实例、恢复场景目标、释放后置驱动。
- Graph 暂停但 Clip 仍有效：保留目标和跟随状态。
- Playable 销毁或图重建：幂等恢复并释放所有临时对象。

只依赖 `OnBehaviourPause` 会让暂停预览时目标消失；只依赖 `OnPlayableDestroy` 会让 Clip 出界后继续写目标。Pause 判定必须对照目标 Timeline 版本的 `FrameData.effectivePlayState` 和内置 Playable 实现。

## 7. 常见问题与定位

| 现象 | 优先检查 | 典型修正 |
| --- | --- | --- |
| 偏差随动作速度变化 | 读取挂点早于动画写回 | `ProcessFrame` 即时写入 + 动画后校正，或改用图内依赖 |
| 静止时固定偏差 | 空间公式、初始 Pivot、旋转表示 | 对比挂点和目标根 Transform，不先看网格中心 |
| 只在某些轴偏 | 轴掩码和未勾选轴基准 | 全轴复测，再检查未勾选轴是否保留原值 |
| 修改倍率位置异常 | Handle 和运行时使用了不同倍率 | 统一有效倍率，写回原始值时除以倍率 |
| 获取偏移后模型跳变 | 写入有效值或动态模式仍覆盖 | 按倍率反算，并关闭动态保持 |
| 旋转出现 `360/0` 跳变 | 欧拉角未归一化 | 写回前归一化到 `-180..180` |
| Handle 能拖但运行时不变 | 动态模式忽略手填值或写错字段 | 共用字段；动态模式隐藏/禁用 Handle |
| 暂停后目标消失 | Pause 无条件清理 | 区分图暂停和 Clip 出界 |
| 反复调参越来越偏 | 未在销毁时恢复，或重复采样已跟随目标 | 幂等恢复；只捕获一次原始位姿 |
| 两个同名模型跟错 | 自动解析范围过宽 | 显式绑定、相对路径、歧义报错 |
| Editor 构建失败但本功能无关 | 工作区基线缺失类型或生成代码 | 记录错误文件/类型，分离基线阻断和本次改动 |

排查顺序：目标/挂点唯一性 → 空间公式 → 倍率 → 轴掩码 → 求值时序 → 生命周期。

## 8. 最小验证矩阵

| 维度 | 用例 | 通过条件 |
| --- | --- | --- |
| 公式 | 挂点平移/旋转，目标有非零位置/旋转 | 应用结果保持捕获的局部相对位姿 |
| 倍率 | 关闭、`1`、`0.1`、`0.01`、非法值 | 关闭等效旧行为；实际偏移正确；非法值 Clamp |
| 烘焙 | 摆好场景目标后点击获取 | 字段可见，动态关闭，模型实际姿态不跳变 |
| Scene Handle | Move/Rotate、Undo、改倍率后再拖 | 只改目标轴；撤销有效；运行时和 Scene 一致 |
| 动态捕获 | 前一帧进入、Seek、倒回重播 | 捕获时机正确；不会采样已跟随目标 |
| 快速动画 | 挂点高速平移/旋转 | 帧末关系正确，无明显一帧滞后 |
| 暂停/出界 | Clip 内暂停、越过 Clip、重建 Graph | 暂停不消失；出界清理；重建不漂移 |
| 两类目标 | 场景目标和 Prefab 实例 | 场景目标恢复；Prefab 正确停用/销毁；都按当前实例位姿捕获 |
| 编译 | Runtime、主程序集、Editor 程序集 | 记录错误来源，不把基线错误误报为功能通过 |

静态编译不能证明求值顺序；至少要在 Unity Timeline 窗口验收快速动作、暂停、拖帧、越界和重播。

## 9. 迁移与回退边界

- Timeline 版本改变 `RefreshReason`、预览模式或 Pause 语义时，先重跑刷新和生命周期验证。
- 没有可靠动画后置阶段时，升级为 `PlayableGraph` 依赖、Animation Job 或约束完成回调。
- 多 Clip、Layer、Director 写同一 `Transform` 时，改用 Mixer/协调器聚合，或禁止冲突，不能依赖 `LateUpdate` 顺序。
- 非均匀父级缩放需要单独测试，不要假定 `lossyScale` 写入 `localScale` 一定等价。
- 没有 Scene Handle 的项目可以只保留 Inspector 数值和烘焙按钮，不要复制运行时算法。
- 无法保证预览无闪时可回退到 `ContentsModified`，但交付说明必须写明会重建预览。

## 10. 交付检查清单

- [ ] 已写清目标、挂点、空间、旋转表示和轴掩码契约。
- [ ] 捕获与应用公式互为逆运算，未混入未参与应用的缩放。
- [ ] 已区分动态捕获和编辑器烘焙，两个来源不会隐式覆盖。
- [ ] 原始/有效偏移分层保存，倍率关闭时旧资产行为不变。
- [ ] Scene Handle 和 Inspector 编辑同一字段，支持 Undo、脏标记、刷新。
- [ ] 动态模式下 Handle 的禁用原因清楚。
- [ ] 运行时和编辑器共用挂点解析合同。
- [ ] 覆盖 `ProcessFrame`、动画后校正、Pause、Clip 出界、Destroy。
- [ ] 数值字段用 live sync + `SceneNeedsUpdate`，结构字段才重建图。
- [ ] 已验证公式、倍率、烘焙、Handle、快速动画、暂停、出界、重建和编译。
- [ ] 未验证项、项目事实和回退边界分开记录。
