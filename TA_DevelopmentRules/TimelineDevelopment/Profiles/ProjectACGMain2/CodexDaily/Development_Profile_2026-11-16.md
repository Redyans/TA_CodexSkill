<!-- BEGIN automation:ta date:2026-11-16 layer:PROFILE -->
# ProjectACGMain2 Timeline 预览角色结构报错 Profile｜2026-11-16

> 类型：PROFILE；项目：ProjectACGMain2；仅记录指定会话的错误、运行时条件和排查结论。本次没有读取该工程源码或资产，也没有修改/复测 Prefab。
> 关联参考：[Timeline 镜头制作、Editor 预览与运行时边界](../../../references/timeline-camera-authoring-editor-preview-and-runtime-boundary.md)；模块入口：[TimelineDevelopment](../../../README_Tech_TimelineDevelopmentRules.md)；同类 ProjectACG 生成事实见 [入场 Timeline Profile](../../ProjectACG/CodexDaily/Development_Profile_2026-11-16.md)。

## 1. 项目事实与现象

- 用户日志来自 `ProjectACGMain2/Client`，入场演出准备失败，资源路径为 `Assets/AssetRaw/character/hero/100401/timeline/admission_timeline/tPre_100401_admission_playing.prefab`，presentation 为 `100401002`、character 为 `100401`。
- 异常为 `Preview character contains Timeline/Camera/FX/Audio objects; move presentation objects outside the preview model.`，抛出位置报告为 `SceneTimelinePreviewCharacterScope.Acquire`，调用链进入 `PresentationPlaybackModule.SceneTimelineRuntimeScope.PrepareUltimateTimelineBindings()`。
- 会话报告的运行时校验先从 `Ultimate_Character` Track 取得预览 Animator，再检查该 Animator 和所有后代（含 inactive）是否包含 `PlayableDirector`、`Camera`、`ParticleSystem` 或 `AudioSource`。发现任意一种便拒绝准备 Timeline。
- 资源根下的 `fx_group`、`camAni_group`、`sound_group` 与 `chara_group` 同级，按会话中的校验条件允许存在；受限制的是绑定 Animator 自身及其模型子树。
- 会话定位到 `chara_group` 下嵌有 `cPre_hero_100401_shoot_timeline` 角色 Prefab，并提示继续检查它的内部层级。会话没有确认具体哪一个后代组件命中了检查，也没有给出已修复资源的证据。

## 2. 根因、修正与取舍

- 已确认的失败原因是运行时隔离边界被触发：系统需要把制作预览角色隐藏/隔离，并把 Timeline 绑定迁移到外部角色；若 Animator 子树内同时包含演出对象，隔离模型会连带影响这些对象，因此 `Prepare` 中止。
- 这不是该会话证据支持的网络或角色加载故障。失败发生在 Timeline 准备阶段，入场 Timeline 没有开始播放；角色详情界面仍显示不能证明演出已开始。
- 排查应从 `Ultimate_Character` 绑定的 Animator 开始递归检查 inactive 后代。将相机、FX、音频和其他 Timeline Director 移到模型子树外的专属同级容器，再通过挂点跟随或 Exposed Reference 重新绑定。
- 当前 Profile 只能确认校验规则和排查范围；实际 offending node 未被隔离，不能把嵌套 Prefab 本身直接定为唯一根因。

## 3. 会话报告的证据

| 项目 | 会话报告 | 边界 |
| --- | --- | --- |
| 校验条件 | Runtime 代码检查 Animator 及所有后代，包含 inactive 对象。 | 来自会话中的源码检查报告；本次没有读取源文件。 |
| 层级区分 | 根下同级 `fx_group` 等可用；角色 Animator 子树中的演出对象触发拒绝。 | 需要目标 Prefab 实际层级确认。 |
| 具体触发物 | 没有确认具体对象和组件。 | 缺失。 |
| 修复与回归 | 没有修复或重新播放结果。 | 未验证。 |

## 4. 未验证项与回退

- 在 ProjectACGMain2 中打开上述实际 Prefab，检查 `chara_group` 内嵌角色 Prefab 的 Animator 后代，记录命中的对象路径、组件类型及 active 状态。
- 将演出对象移出 Animator 子树并重新绑定后，重新准备/播放 `100401002`；同时检查隐藏角色、绑定迁移、退出恢复及同级 FX/Camera/Audio 是否正常。
- 如无法确认节点所属或引用关系，先复制 Prefab 做受控验证；保留原 Prefab 与 Timeline 绑定作为回退，不直接清空整棵模型层级。
- 本案例是 ProjectACGMain2 的单次运行时校验记录，保留为 Profile；没有升级 CORE 的证据。

## 来源会话

- [01a1e31e-dbe5-7ab0-933b-16ed61f05c3b](../../../../../VA_Worlk_memory/sessions/01a1e31e-dbe5-7ab0-933b-16ed61f05c3b.md)

<!-- END automation:ta date:2026-11-16 layer:PROFILE -->
