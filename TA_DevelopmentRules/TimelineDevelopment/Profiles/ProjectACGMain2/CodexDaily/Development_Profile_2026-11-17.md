<!-- BEGIN automation:ta date:2026-11-17 layer:PROFILE -->
# ProjectACGMain2 CDC 与 Global Volume Timeline Profile｜2026-11-17

> 类型：PROFILE；项目：ProjectACGMain2；记录指定会话报告中的 CDC 烘焙校验、Global Volume 生命周期和验证边界。本次日报没有读取项目源码或执行 Unity。
> 关联参考：[Timeline 生成、挂点绑定与烘焙生命周期](../../../references/CodexDaily/Development_Reference_2026-11-17.md)；模块入口：[TimelineDevelopment](../../../README_Tech_TimelineDevelopmentRules.md)。

## 1. 项目事实与现象

- CDC 合并 Timeline 烘焙在 `CharacterDynamicCameraDebugTimelineBaker.ValidatePose` 报告 `Timeline hierarchy changed during baking`。会话定位到 Baker 创建后源角色按需出现的 `Character Dissolve Particles` 动态子树。
- 第一版修正尝试固定创建时的 Transform 配对和相对路径，验证已绑定节点删除/重挂父级，同时忽略采样期间新增节点；编辑器程序集编译报告为 0 错误，但 Unity EditMode 因项目锁未执行，harness 另有无关 `.meta`/本地化失败。
- 用户随后明确“不管角色溶解”。后续进度称已在录制和验证两侧显式排除 `Character Dissolve Particles` 子树，并调整回归测试覆盖新增、重建和销毁；该阶段没有最终编译/测试结果。
- `Global Volume Track` 左侧必须绑定实际挂有 `Volume` 组件的 GameObject；Clip 播放结束后默认只恢复参数，Volume 组件仍可继续生效。新增“Clip 结束后关闭 Volume”轨道开关，默认关闭；开启时按所有启用层最后 Clip 结束时间关闭绑定组件，回到 Clip 区间重新启用。

## 2. 根因、修正与取舍

- CDC 的固定绑定数组把不属于录制目标的动态溶解粒子误当作必须存在的层级，导致新增、重建或销毁时验证失败。最终方向是从 Recorder 和验证范围同时排除该子树，仍保留对真实烘焙对象的结构检查。
- Global Volume 轨道绑定空对象时不会找到 `Volume`，Clip 参数不会生效；关闭开关关闭的是整个 Volume 组件，会连带停止同组件承载的其他后处理，建议使用专用技能 Volume。
- 首轮隔离测试报告 4 项通过、2 项失败，失败涉及最后一个隔离持有者释放后未恢复其他 Volume，以及编辑器直接 `Evaluate` 后 `Stop` 不一定销毁播放图。修正已写入工作区，但自动审批阻止了复跑。

## 3. 会话报告的验证证据

| 检查 | 会话报告 | 边界 |
| --- | --- | --- |
| CDC 编译 | 编辑器程序集增量编译 0 错误。 | Unity EditMode 未运行；用户后续要求排除溶解后的最终版本未完成复测。 |
| Global Volume | 运行时代码和测试代码编译通过；单轨、多层、结束边界首轮通过。 | 场景隔离与预览停止失败后的修正未复跑。 |
| Git 对比 | 远端最新 `master` 中已有开关初版；预览恢复、场景隔离恢复和测试调整仍是本地补充。 | 本 Profile 不代表这些本地修改已提交或已推送。 |

## 4. 未验证项与回退

- 需要在实际 CDC 资源中重跑生成，确认排除溶解子树后 Timeline 能正常生成，同时已绑定相机、角色和灯光仍被验证。
- 需要重新运行隔离测试和真实 Timeline 播放，检查 Volume 关闭、重新启用、场景隔离释放和编辑器预览退出；当前自动审批阻止的测试环境未形成新结果。
- 若预览或场景状态残留，恢复测试前的 Volume 启用状态和 Timeline 预览快照；不要把关闭整个 Volume 当作只恢复 Clip 参数。

## 来源会话

- [01a1e864-8076-77c2-afee-2dfbff033201](../../../../../VA_Worlk_memory/sessions/01a1e864-8076-77c2-afee-2dfbff033201.md)
- [01a1e9c1-34a4-7250-a700-d73984e74c70](../../../../../VA_Worlk_memory/sessions/01a1e9c1-34a4-7250-a700-d73984e74c70.md)

<!-- END automation:ta date:2026-11-17 layer:PROFILE -->
