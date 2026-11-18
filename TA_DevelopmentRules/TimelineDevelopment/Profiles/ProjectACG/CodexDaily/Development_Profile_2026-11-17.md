<!-- BEGIN automation:ta date:2026-11-17 layer:PROFILE -->
# ProjectACG 入场 Timeline 与挂点 Profile｜2026-11-17

> 类型：PROFILE；项目：ProjectACG；记录指定会话中的生成工具、Timeline 资源和运行时挂点事实。本次日报没有读取项目源码或执行 Unity。
> 关联参考：[Timeline 生成、挂点绑定与烘焙生命周期](../../../references/CodexDaily/Development_Reference_2026-11-17.md)；模块入口：[TimelineDevelopment](../../../README_Tech_TimelineDevelopmentRules.md)。

## 1. 项目事实与现象

- 入场复制入口为 `AdmissionTimelineCopyService`。用户最终要求只改“复制抽卡资源并生成入场文件夹”流程：复制完成后，递归删除目标 `fx/` 下特效子 Timeline 的 `Light`、`PostProcess` 分组内轨道；保留分组、其他轨道和源抽卡资源。
- 会话报告称该逻辑已落在复制落盘后、校验前，Editor 与 EditMode 测试程序集编译通过，`git diff --check` 通过；Unity EditMode 未实际运行，harness 被工作区无关 `.meta` GUID 和本地化 Key 问题阻断。
- `100601` 入场资源 `tPre_100601_admission_playing.prefab` 的两条 Prop Track 使用普通 `MountFollowTrack`，`mountId` 分别为 `weapon_r`、`weapon_l`，资源中的 `sceneTarget` 和 ExposedReference 存在。
- 角色详情播放链会把 `Ultimate_Character` 绑定迁移到外部 Animator，并隔离预览角色；普通 `MountFollowTrack` 仍依赖 `MountResolver` 自动扫描 Director/轨道候选。历史证据显示预览替身与外部角色同时存在时可能产生 `Ambiguous mount`，进而跳过跟随。

## 2. 根因、修正与取舍

- 入场清理的范围必须落在复制副本，不能删除全部根轨道，也不能把手动生成和抽卡复制路径混为一谈；用户后续明确收窄后，手动流程的改动被收回。
- Prop 不跟随目前不能归因于 `mountId` 填错。最可能是自动解析仍看到预览角色导致歧义，或外部 Animator 根不包含挂点；也存在解析成功后被 Animator/其他轨道覆盖的可能。
- 会话中没有修改 Prop 资源或运行时代码。建议方向是显式绑定外部角色根或使用 `BoundMountFollowTrack`，但这仍是未执行方案。

## 3. 会话报告的验证证据

| 检查 | 会话报告 | 边界 |
| --- | --- | --- |
| 生成清理 | Editor/测试程序集编译 0 错误，静态残留检查通过。 | EditMode 未执行，未在 Unity 中重开/播放目标资源。 |
| 资源配置 | `mountId`、`sceneTarget`、ExposedReference 和轨道类型已静态核对。 | 没有一次实际运行日志证明是 `Ambiguous` 还是 `Mount not found`。 |
| 历史证据 | OpenSpec 历史记录过同类预览替身歧义。 | 历史记录不能替代本次 100601 播放日志。 |

## 4. 未验证项与回退

- 生成流程需在 Unity 中执行一次，确认目标 `fx/` 子 Timeline 的两个组轨道清空、其他轨道和源资源未变。
- 100601 需抓取 `[MountFollow]`、`SceneTimelinePreviewCharacterScope`、`Prop animation probe mount candidates` 和 `UltimateTimeline.prepare` 日志，才能区分歧义、缺失或时序覆盖。
- 若清理或绑定修复失败，恢复生成前资源/Prefab；不要先修改 `mountId` 或扩大清理到全部 Timeline。

## 来源会话

- [01a1e7bf-c3c1-71c1-b9a2-8c42ced0ee84](../../../../../VA_Worlk_memory/sessions/01a1e7bf-c3c1-71c1-b9a2-8c42ced0ee84.md)
- [01a1e7da-77bf-7e52-9c6c-eb36d5efe6ea](../../../../../VA_Worlk_memory/sessions/01a1e7da-77bf-7e52-9c6c-eb36d5efe6ea.md)

<!-- END automation:ta date:2026-11-17 layer:PROFILE -->
