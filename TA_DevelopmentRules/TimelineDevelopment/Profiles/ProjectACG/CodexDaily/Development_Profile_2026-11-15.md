<!-- BEGIN automation:ta date:2026-11-15 layer:PROFILE -->
# ProjectACG Timeline 材质 Clip Profile｜2026-11-15

> 类型：PROFILE；项目：ProjectACG；记录来源会话当天的实现与用户播放问题，不代表本次读取工程源码或复测。
> 关联参考：[Timeline 材质参数曲线与叠加绘制](../../../references/CodexDaily/Development_Reference_2026-11-15.md)；模块入口：[TimelineDevelopment](../../../README_Tech_TimelineDevelopmentRules.md)；沿用 `TML-DOC-01`、`TML-ARC-02`、`TML-LIF-02`、`TML-VAL-01`。

## 1. 项目事实与现象

### 材质参数曲线与 MPB

- `CharacterMaterialSwapClip` 会在替换材质后控制指定 Mesh/槽位的 Float、Vector、Color 属性曲线，复用 `CharacterMaterialTimelineMixer` 与槽位级 MPB；共享材质资产不被克隆或写入。
- Inspector 支持按 Mesh/材质槽分组、多参数曲线和从替换材质 Shader 读取可动画属性；后续选择控件改为可搜索列表，并缓存 Shader 属性和 Mesh 列表。
- 会话报告隔离 Unity 2022.3.62f3 工程的 EditMode 测试 14/14 通过，AOT 与 Editor 编译通过。MPB 活动时，项目 Battle/Lobby High 的 URP 资产启用 SRP Batcher，受控 Renderer 会退出该路径；没有 Profiler 数据。

### 追加绘制 Overlay

- Clip 新增“替换槽位 / 追加绘制”模式。追加模式由 Timeline Track 自行管理临时 Renderer，不复用 `MaterialOverlayEffect`；保留原 Renderer/材质并在指定槽位对应的 SubMesh 上绘制 Overlay，参数曲线也作用于 Overlay Renderer。
- 为覆盖角色原有较晚绘制的 Fringe、Eye、Outline 等 Opaque 专用 Pass，会话将 Overlay 材质运行时副本 Queue 至少设为 Transparent（3000），并排在源材质队列之后。会话报告 AOT/测试程序集编译通过、隔离 EditMode 19/19 通过；实际场景透明排序和视觉效果未复验。
- 用户报告 Hair 在 Clip 进入时消失、稍后才出现。会话代码核对显示 Overlay Renderer 同步源 Renderer 的 `enabled`；因此“Clip 起点源 Hair Renderer 被关闭”是优先排查假设，不是已确认根因。应检查目标 SkinnedMeshRenderer 和 Overlay 对象在起点/中段的状态。

### Vector 分量曲线与 Dissolve 属性

- Vector 参数增加独立 X/Y/Z/W 曲线开关，默认关闭以保持旧 Clip 的整体单曲线兼容。首次切换到独立模式会将旧曲线复制到四个分量；每条分量曲线乘基础向量对应分量。报告称额外三次曲线采样且不逐帧创建堆对象。
- 当天 Unity Editor 自动编译相关程序集并报告无 C# 错误、`git diff --check` 通过；该向量功能新增 EditMode 用例尚未实际运行，Inspector 四曲线布局也未手工验收。
- 截图中的 `DissolveXRangeVec` 被识别为 ShaderGUI 的 X 范围辅助属性；它设置 `_Dissolve.x` 滑条范围，不是驱动画面的 Dissolve 值。曲线选择的真正属性需为 Shader 实际消费的 `_Dissolve`，且目标 Shader 必须支持并启用该功能。
- 对照的同名 `BaseLit.mat` 使用 `BattleSceneBaseLit.shader`，来源会话检查该 Shader 未声明 `_Dissolve`。此外，默认水平曲线值为 `1`，在“基础向量 × 曲线”语义下不会改变基础值；截图属性也不改变材质资产 Inspector 中的 MPB 值。
- 当前磁盘 `.playable` 参数与截图显示不一致，来源会话提出可能是 Inspector 改动未保存或截图来自另一个副本；未确认是哪一种。

## 2. 根因、修正与项目约束

- 追加材质槽并不能保证任意原 SubMesh 被重绘；精确按槽追加采用 Track 自有 Overlay Renderer，并保留源 Renderer。
- Hair 特殊 Pass 会在 Opaque 主绘制后出现。Overlay 提升至 Transparent 队列是项目当前用于后覆盖的实现取舍；它也会改变与其他透明物体的排序，需在目标演出验证。
- Overlay 同步源 Renderer `enabled` 使源对象关闭时 Overlay 也关闭。该行为可能解释用户截图中的 Hair 隐现，但两帧实际 Enabled 状态未取得证据，根因仍待确认。
- `DissolveXRangeVec` 是控制 UI 范围的元数据，不是实际材质效果；`_Dissolve` 也只有目标 Shader 声明并消费时才有视觉意义。

## 3. 会话报告的验证证据

| 范围 | 会话报告 | 未覆盖 |
| --- | --- | --- |
| 材质参数曲线 | 隔离 EditMode 14/14；AOT/Editor 编译通过 | 目标 Inspector 手工验收、目标设备 Profiler。 |
| 追加 Overlay | AOT/测试程序集编译通过；隔离 EditMode 19/19 | 生产 Hair/眼部场景画面、Frame Debugger 队列顺序和透明交互。 |
| Vector 分量曲线 | AOT/Editor/EditMode 程序集自动编译无错；新增测试已写入 | EditMode 未运行，Inspector 未手工检查。 |
| Hair 起点消失 | 源码同步逻辑提供候选解释 | 起点/后续帧源 Renderer 与父级 active 状态未检查。 |
| Dissolve | 属性用途、Shader 声明与曲线语义在会话中静态检查 | 当前运行中材质引用、已保存 Timeline 资产与实际播放未闭环。 |

## 4. 未验证项与候选

- 在生产 Timeline 的 Clip 起点、稳定中段和退出时，记录源 `SkinnedMeshRenderer.enabled`、父级激活状态、Overlay Renderer 状态及 Frame Debugger Draw 顺序；确认 Hair 问题前不调整同步策略。
- 运行新增 Vector 分量 EditMode 测试，并手工确认 Inspector 序列化/旧 Clip 兼容；对多个 Clip/Layer 同写同一槽的仲裁另行验证。
- 对 Overlay Transparent Queue 检查透明 VFX、角色专用 Pass、深度与遮挡；若影响排序，不能只根据 19/19 单测保留该视觉方案。
- MPB 与材质实例方案需在目标设备测量；来源会话没有帧时间数据。
- 候选：Timeline 材质参数排查可把“UI 辅助属性、Shader 实际消费属性、材质槽目标和曲线中性值”作为属性不变化问题的检查项。当前来自一个工程案例，留在此 Profile，不升级 CORE。

## 来源会话

- [01a1cf40-ee85-79b3-af3a-15bf06a6a14d](../../../../../VA_Worlk_memory/sessions/01a1cf40-ee85-79b3-af3a-15bf06a6a14d.md)

<!-- END automation:ta date:2026-11-15 layer:PROFILE -->
