---
name: projectacg-character-render-timeline-profile
description: ProjectACG 当前工程的 CharacterRender Timeline 定制规则。记录具体 Controller、Track、Shader、PerObjectShadow 接口、作用域和验证边界，不可直接迁移到其他项目。
---

# ProjectACG CharacterRender Timeline Profile

## 1. 适用范围与迁移边界

本 Profile 仅适用于 `D:\work2025U3D\Valkyria\ProjectACG\Client` 当前实现。通用规则以 [../../README_Tech_TimelineDevelopmentRules.md](../../README_Tech_TimelineDevelopmentRules.md) 为准，通用模式以 [../../references/timeline-development-patterns.md](../../references/timeline-development-patterns.md) 为参考；本文件只补充 ProjectACG 的类型、路径、Shader 家族、PerObjectShadow 状态、优先级和已知限制。

迁移到其他项目时必须删除或重建本 Profile。不得把 `CharacterRenderController`、`CharacterRenderTimelineClip`、`Chara V2`、`BaseLit`、`PerObjectShadowFeature`、模式枚举、Shader 全局键或当前目录结构当成跨项目统一规范。其他项目只复用 CORE 中的作用域分析、基线恢复、协调器、Editor 事件链和验证方法，再按自己的渲染架构建立 Profile。

## 2. 当前 Source of Truth

| 职责 | 当前文件 |
| --- | --- |
| 场景角色渲染基线 | `Assets/GameScripts/AOT/GameArt/CharacterRender/CharacterRenderController.cs` |
| Timeline Clip 数据 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderTimelineClip.cs` |
| Track、Mixer、MPB 与共享请求协调 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderTimelineTrack.cs` |
| Clip Inspector、Orbit/Elevation 与 Scene Handle | `Assets/GameScripts/Editor/TimelineTrack/CharacterRenderTimelineClipEditor.cs` |
| Chara V2 局部覆盖入口 | `Assets/Shader/Character/Chara_V2/Common/Chara_Globals_V2.hlsl` |
| 角色 POS 采样 | `Assets/Shader/Character/Chara_V2/Common/Func_Chara_PerObjectShadow_V2.hlsl`、`Assets/Shader/Character/Common/Func_Chara_PerObjectShadow.hlsl` |
| POS RenderFeature 与最终方向/强度解析 | `Assets/Shader/RenderFeature/PerObjectShadowV2/Script/PerObjectShadowFeature.cs` |

代码、序列化资产或 Renderer 配置与本 Profile 不一致时，以当前代码和资产为准，并将差异作为 Profile 修订候选；不要反向修改 CORE 迁就单项目实现。

## 3. PRJ-TML-01｜Controller 能力同步边界

CharacterRender Timeline 当前需要覆盖最新版 `CharacterRenderController` 的镜头级艺术功能：

- 阴影采样模式：`Off / UnityOnly / POSOnly / Both`；
- Clip 选择 `POSOnly/Both` 时的 POS RenderFeature 启用请求；
- 高质量 POS 阴影方向跟随 Clip 主光；
- Chara V2、BaseLit 与材质高光阴影作用强度；
- 环境光对场景 SH 的覆盖；
- `FollowCamera XZ`。

这里的“同步”是艺术功能语义同步，不是把 Controller 字段逐个复制到 Clip。每个模块仍须按 [TML-DOC-02](../../README_Tech_TimelineDevelopmentRules.md#tml-doc-02镜像已有-controller-时维护能力与作用域映射) 建立 Controller 基线、Clip 输入、Mixer 规则、最终消费者和恢复合同；新增 Controller 功能时先更新能力表，再决定 Timeline 是否需要暴露。

## 4. PRJ-TML-02｜状态作用域与写入边界

| 状态 | ProjectACG 作用域 | 写入方式 | 多 Clip/Mixer 决策 |
| --- | --- | --- | --- |
| 最终采样 `Off/UnityOnly/POSOnly/Both` | 绑定角色 | Renderer/材质槽 `MaterialPropertyBlock` | 同一 Mixer 内最高权重模式，模块权重负责恢复场景基线。 |
| Chara V2 POS 接收强度、高光阴影作用强度 | 绑定角色 | Timeline 专属 MPB 值与权重 | 同一 Mixer 内连续混合，可按角色隔离。 |
| BaseLit 场景接收强度 | 场景/相机共享 | Timeline 专属共享请求 | 所有有效 Mixer 中选择最高权重请求，使用稳定 tie-breaker。 |
| POS RenderFeature 启用 | 相机共享 | Timeline 专属启用权重 | 所有有效请求取最大权重。 |
| POS 投影方向 | 相机共享 | Timeline 专属协调器 | 最高权重请求；`PerObjectShadowVolume` 显式方向优先。 |

BaseLit 接收材质不属于绑定角色根节点，不能通过角色 MPB 实现完全隔离。多角色可拥有独立的 Chara V2/高光接收强度，但 POS 投影方向和 BaseLit 场景接收强度仍是共享状态；Inspector HelpBox、功能 README 和交付说明都必须暴露这一限制。

## 5. PRJ-TML-03｜材质与 Controller 基线

- Shader 以材质和 `CharacterRenderController` 的当前实际值为基线，Timeline 使用独立 `value + weight` 叠加；权重为零时必须精确回到基线，不能落到未初始化的 `0`。
- 不把材质参数快照或机械复制成 Clip 字段。只有艺术上需要显式覆盖的输入进入 Clip；材质级高光强度继续作为最终乘数，使不同材质保持自身差异。
- Clip 阴影强度使用中性默认值 `1`，Inspector 范围为 `0..1`，Mixer 消费时使用 `Mathf.Clamp01`，防止旧序列化资源、脚本写入或 YAML 越界。
- MPB 先保留其它系统已有内容，只更新 Timeline 专属属性；清理时只把 Timeline 权重归零，不清空整块 MPB，不实例化或修改 `sharedMaterial`。

## 6. PRJ-TML-04｜POS 可见性、优先级与协调器

场景 `CharacterRenderController` 为 `Off/UnityOnly` 时，会使相关 POS 接收强度处于零基线。只让 `PerObjectShadowFeature` 入队仍然看不到最终阴影，因此 Clip 选择 `POSOnly/Both` 时必须同时完成：

1. 登记 POS RenderFeature 启用请求；
2. 使用 Clip 自身非零阴影强度并按 Clip 权重渐入；
3. Chara V2 接收强度写入绑定角色 MPB；
4. BaseLit 接收强度登记为共享请求；
5. Feature 在原有启用判断中组合 Controller 与 Timeline 请求，不覆盖 Controller 的全局键。

协调器以 Mixer Owner 保存启用、方向和 BaseLit 请求。启用取最大权重；方向与 BaseLit 强度分别选择最高权重请求，同权重使用稳定登记顺序。方向优先级为 `PerObjectShadowVolume 显式方向 > Timeline 方向 > CharacterRenderController`；零方向表示跟随场景主平行光，非零方向写出前归一化。

Timeline 必须使用专属全局键。Clip 结束、绑定丢失、PlayableGraph 停止/销毁时移除 Owner 请求；静态集合在 `SubsystemRegistration` 清空，避免禁用 Domain Reload 后残留。

## 7. PRJ-TML-05｜CharacterRender Clip Editor

- Orbit/Elevation 圆盘和数值框分别执行变化检测，再统一写回同一方向 `SerializedProperty`；每个 `BeginChangeCheck` 必须与同层级 `EndChangeCheck` 成对。
- 左键按下获取 `GUIUtility.hotControl`，拖拽期间即使指针离开圆盘仍继续更新，抬起释放；右键重置消费事件并触发 `GUI.changed`。
- Scene Handle 以 Timeline 根 Track 的绑定对象为锚点，子 Layer Clip 必须能回溯根绑定；编辑后记录 Undo、应用序列化属性、标脏并刷新预览。
- 圆盘“视觉能动但值不保存”时，先检查变化检测是否错误嵌套、是否只结束数值框的内层检查，再排查几何命中区域。

## 8. ProjectACG 验证清单

- [ ] `AOT.csproj`、Runtime/主程序集与 `Assembly-CSharp-Editor.csproj` 编译通过，新脚本已进入正确程序集。
- [ ] Unity Timeline 中新旧 CharacterRender Clip 均可创建、编辑和播放，无 Missing Script 或黄色脚本告警。
- [ ] 材质/Controller 非零基线下，Clip 权重从 `0 -> 1 -> 0` 后精确恢复原值，没有默认归零或材质差异丢失。
- [ ] Controller 为 `Off/UnityOnly` 时，Clip 选择 `POSOnly/Both` 会同时启用 POS 并提供非零接收强度，在 Chara V2 与 BaseLit 上均实际可见。
- [ ] Chara V2、高光、BaseLit、环境 SH、主光方向、`FollowCamera XZ` 与最新 Controller 的艺术语义一致。
- [ ] 两个角色和多个重叠 Clip 下，局部强度保持隔离，共享方向/BaseLit 按最高权重与稳定 tie-breaker 仲裁。
- [ ] Orbit/Elevation 圆盘左键拖动、右键重置、数值输入、Scene Handle 和 Undo 都写回同一 Clip 字段。
- [ ] Seek、Pause、Stop、Graph 销毁、Domain Reload 关闭和重新播放不会留下 Timeline MPB 权重或静态共享请求。
- [ ] 使用 Frame Debugger/Profiler 确认 POS Pass、接收材质、方向、强度和多相机行为；C# 构建不能代替 Unity 播放与视觉验证。

## 9. 当前已知限制

- 角色局部 Chara V2/高光接收强度可以隔离；POS 投影方向、BaseLit 场景接收强度和相机 RenderFeature 仍属于共享状态。
- 材质/Shader 当前值作为基线意味着美术修改材质后 Timeline 会跟随新值，这是预期行为；需要固定镜头值时必须在 Clip 中显式覆盖，不能偷偷缓存旧材质快照。
- 未完成 Unity 播放、Frame Debugger、多角色和多相机验证时，只能声明“静态编译/文档合同已通过”，不能声明视觉功能已完整交付。
