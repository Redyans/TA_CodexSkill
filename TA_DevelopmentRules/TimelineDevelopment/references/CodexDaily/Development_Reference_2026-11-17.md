<!-- BEGIN automation:ta date:2026-11-17 layer:REFERENCE -->
# Timeline 生成、挂点绑定与烘焙生命周期参考｜2026-11-17

> 类型：REFERENCE；适用范围：Timeline/Prefab 生成清理、运行时对象挂点、Volume 生命周期和动态层级烘焙验证；使用前提：以目标 Unity 版本、Track 实现、绑定对象和实际播放链重新验证。
> 关联既有规则：`TML-CMP-01`、`TML-VAL-01`、`TML-BND-01`、`TML-OPS-01`；模块入口：[TimelineDevelopment](../../README_Tech_TimelineDevelopmentRules.md)。项目事实见 [ProjectACG Profile](../../Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-17.md) 与 [ProjectACGMain2 Profile](../../Profiles/ProjectACGMain2/CodexDaily/Development_Profile_2026-11-17.md)。

## 1. 适用场景

- 从模板复制并生成入场 Timeline/Prefab，需要只清理目标副本中的指定轨道。
- Prop 通过 `mountId` 跟随运行时角色骨骼，但 Timeline 内同时有预览角色和外部角色。
- Global Volume Track 需要明确绑定对象，并在 Clip 结束后决定恢复参数还是关闭整个 Volume。
- CDC/Timeline 烘焙采样期间，源层级会按需新增、销毁或重建特效节点。

## 2. 前提与依赖

- 先冻结生成入口、目标目录、源资产保护范围、GUID/引用合同和覆盖策略；复制后的清理不得反向修改源抽卡资源。
- 区分显式 Track Binding、`ExposedReference` 和自动挂点扫描；记录运行时绑定根及候选数量。
- Volume 生命周期要定义 Clip 权重归零、Timeline 停止、播放图销毁、编辑器预览退出和场景隔离释放的语义。
- 烘焙验证应保存 GameObjectRecorder 创建时的固定绑定集合；动态效果若不属于烘焙目标，必须从录制和验证范围显式排除。

## 3. 实现或排查步骤

1. 生成工具在目标副本落盘后执行结构化清理：按目标目录过滤 `.playable`，递归删除指定组内轨道，保留组本身、其他组和源资源；对清理结果增加残留断言。
2. 运行时挂点优先使用稳定角色根的显式 Binding；若仍使用自动解析，记录 `Mount not found`、`Ambiguous mount` 和命中的候选根，不要只改 `mountId`。
3. 对 Volume Track，把“恢复 Clip 覆盖参数”和“关闭绑定 Volume 组件”分成两个开关/状态；多层轨道以所有启用层的最后 Clip 结束时间为准，回到 Clip 区间时重新启用。
4. 若关闭整个 Volume，确认该组件是否还承载其他效果；技能 Volume 应尽量独立。场景隔离的持有者计数和释放必须成对，最后一个持有者释放时恢复原状态。
5. Baker 创建时记录 Transform 配对、相对路径和录制组件；采样时只验证固定绑定对象的删除/重挂父级。对不属于烘焙目标的动态粒子等子树，在 Recorder 和验证两侧都排除。
6. 验证顺序为：程序集编译 → 定向 EditMode/隔离测试 → 真实生成/播放 → 保存重开 → 源资源与目标结构对比。项目被其他 Unity 实例占用时，编译通过不能替代行为测试。

## 4. 风险与不适用边界

- 只删除空组或只处理手动入口会遗漏复制入口；清空所有根轨道又会破坏用户要求保留的动画/特效。
- 自动挂点扫描在同名预览角色与外部角色并存时会安全失败；解析成功但 Prop 不动时，还需排查 Animator/AnimationTrack/LateUpdate 覆盖顺序。
- “Clip 播放完”不等于 Volume 被关闭；Hold、停帧、播放图未销毁和对象被隐藏会改变生命周期。
- 将采样期间动态新增节点当作固定绑定会产生“层级变化”误报；反过来忽略已绑定节点被删除会掩盖真正的烘焙损坏。
- 编译、静态残留检查和首轮测试通过不能证明真实 Timeline、预览恢复和多场景隔离已闭环。

## 5. 验证与回退

- 生成后检查目标 Timeline 指定组为空、其他组保留、Prefab 容器符合约定、源资源哈希/内容未变；失败时恢复生成前备份。
- 挂点测试需在一次实际播放中记录解析状态和候选根，并逐帧确认 Prop 位置；明确区分未找到、歧义和后续覆盖。
- Volume 测试覆盖单轨、多层、最后一个 Clip、回到播放区间、场景隔离释放和编辑器预览退出；失败时恢复原 Volume 启用状态。
- Baker 测试覆盖“创建后新增/重建/销毁非目标子树”和“删除已绑定节点”两类，不能只测静态层级。

## 来源与证据边界

- [入场 Timeline 生成清理会话](../../../../VA_Worlk_memory/sessions/01a1e7bf-c3c1-71c1-b9a2-8c42ced0ee84.md)
- [100601 Prop 挂点排查会话](../../../../VA_Worlk_memory/sessions/01a1e7da-77bf-7e52-9c6c-eb36d5efe6ea.md)
- [CDC 烘焙层级会话](../../../../VA_Worlk_memory/sessions/01a1e864-8076-77c2-afee-2dfbff033201.md)
- [Global Volume Track 会话](../../../../VA_Worlk_memory/sessions/01a1e9c1-34a4-7250-a700-d73984e74c70.md)

本参考只提炼会话中报告的实现和排查方法；本次日报没有重新运行 Unity 或读取生产场景。
<!-- END automation:ta date:2026-11-17 layer:REFERENCE -->
