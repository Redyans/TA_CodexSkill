---
name: projectacg-character-render-profile-import
description: ProjectACG CharacterRenderLightProfile 导入 CharacterRenderTimelineClip 的实现事实、字段映射和验证边界。
---

# ProjectACG CharacterRender Profile 导入 Timeline Clip

> 分类：PROFILE；适用项目：`ProjectACG`；实现参考版本：历史提交 `6a271f0a86`（2026-10-10）。当前工作区若未包含该提交，不得把本文当作当前分支已经落地的代码清单。

## 1. 适用范围与当前边界

本文记录 ProjectACG 中“角色 Render Setting 配置导入 CharacterRender Timeline Clip，并允许导入后继续调整”的具体实现事实。通用设计和验证方法见 [Timeline 预设参数导入与 Clip 二次调整参考](../../references/timeline-profile-to-clip-import-and-safe-override.md)。

本 Profile 只适用于当前 ProjectACG 的 CharacterRenderController、CharacterRenderLightProfile、CharacterRenderTimelineClip 和 CharacterRenderTimelineTrack。迁移到其他 Unity 项目时，必须重新确认类型、路径、Shader 属性、RendererFeature 和共享状态优先级。

## 2. Source of Truth 与代码入口

| 职责 | ProjectACG 文件 |
| --- | --- |
| 角色渲染 Profile | `Client/Assets/GameScripts/AOT/GameArt/CharacterRender/CharacterRenderLightProfile.cs` |
| Timeline Clip 序列化数据 | `Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderTimelineClip.cs` |
| Track、Mixer、MPB 与共享请求 | `Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderTimelineTrack.cs` |
| Clip Inspector、Scene Handle | `Client/Assets/GameScripts/Editor/TimelineTrack/CharacterRenderTimelineClipEditor.cs` |
| Profile 导入 Utility | `Client/Assets/GameScripts/Editor/TimelineTrack/CharacterRenderTimelineProfileUtility.cs` |
| 导入/绑定 EditMode 测试 | `Client/Assets/GameScripts/Tests/EditMode/GameLogic/CharacterRenderTimelineProfileTests.cs`、`TimelineCharacterAutoBindingTests.cs` |
| Profile 使用方 | `Client/Assets/GameScripts/AOT/GameArt/CharacterRender/CharacterRenderController.cs` |

Profile 的公开属性经过迁移兼容、归一化或 Clamp 后才是导入源。不要直接读取私有序列化字段，也不要把 Controller 的本地缓存字段当成 Profile 的唯一来源。

## 3. 当前实现方式

### 3.1 Inspector 入口

`CharacterRenderTimelineClipEditor.OnInspectorGUI()` 的入口顺序为：

1. `serializedObject.Update()`；
2. 配置文件 `ObjectField` 和“导入并覆盖当前 Clip 参数”按钮；
3. Clip 的说明、分组和方向编辑器；
4. `serializedObject.ApplyModifiedProperties()`。

导入后清空临时 Profile 选择，调用 `UpdateIfRequiredOrScript()` 和 `SceneView.RepaintAll()`。入口置顶是有意设计，表示“先建立镜头基线，再做局部调整”。

### 3.2 Utility 的安全边界

`CharacterRenderTimelineProfileUtility` 是 Editor-only 的内部工具类，执行以下操作：

- 空 Clip 或空 Profile 直接返回失败；
- `Undo.RecordObject(clip, ...)` 只记录当前 Clip；
- 逐字段从 Profile 只读属性复制到 Clip；
- `EditorUtility.SetDirty(clip)` 标记 Timeline 子资产；
- 不保存 Profile 引用，不写回 Profile，不修改材质资源。

导入后 Clip 与 Profile 独立。再次修改 Profile 不会自动更新已导入 Clip；需要用户再次点击导入，或者后续另做同步工具。

注意：这里的“不写回 Profile”是指 Utility 不主动把 Clip 参数写回源资产。当前 Profile 的部分公开 getter 可能包含旧数据迁移、归一化或 `Ensure*` 延迟初始化；读取这些 getter 可能在内存中补齐 Profile 私有字段。若要保证源资产绝对不产生 lazy migration 副作用，应先制作纯读取快照，或将迁移步骤显式化并单独验证 Profile 的脏状态。

### 3.3 字段映射

| 参数域 | Profile 来源 | Clip 目标 | 导入策略 |
| --- | --- | --- | --- |
| 主光 | `MainLightOverrideMode`、`GetWorldDirection()`、`CustomLightColor`、`CustomLightStrength` | `mainLightMode`、`mainLightDirection`、`mainLightColor`、`mainLightStrength` | 当前实现把主光作为显式 Timeline 覆盖；`Off` 的处理要按产品语义确认。 |
| 主光跟随相机 | `VirtualLightCameraDirectionOffsetEuler` | `followCameraOffsetEuler` | 复用 Profile 的相机偏移数据。 |
| 背光 | `EnableBackLight` | `enableBackLight` | 与主光覆盖一起导入。 |
| 虚拟补光 | `VirtualLightColor`、`VirtualLightIntensity`、`VirtualLightOffset`、`GetWorldDirection()` | `virtualLightColor`、`virtualLightIntensity`、`virtualLightOffset`、`virtualLightCameraDirection` | 方向值写入 Clip；FollowCamera 时额外设置跟随开关和偏移。 |
| 虚拟光遮罩 | 当前 Profile 没有独立遮罩跟随开关 | `virtualLightMaskFollowCamera`、`virtualLightMaskCameraDirection` | 历史实现将 `virtualLightMaskFollowCamera` 设为 `true`，这是项目决策，不应直接当作通用默认。 |
| 角色点光 | `EnableCharacterAdditionalLights`、`EnableCharacterAdditionalLightDiffuse`、`EnableCharacterAdditionalLightSpecular` | 对应三个 Clip 开关 | 最高权重 Clip 决定离散开关。 |
| 平面阴影 | `EnablePlanarShadow`、跟随主光开关、方向、强度、衰减、范围、高度、中心、颜色 | `overridePlanarShadow` 及对应字段 | 跟随主光映射为 `planarShadowDirectionMode = 2`；固定方向和材质方向必须区分。 |
| 描边 | `EnableOutline` | `overrideOutline`、`enableOutline` | 导入时打开覆盖。 |
| Rim | Rim 开关、方向空间、方向、点灯/直接光遮罩、范围、强度 | `overrideRimLight` 及 Rim 字段 | 方向按向量归一化，开关由最高权重 Clip 决定。 |
| 最终颜色渐变 | 渐变开关、颜色、最小/最大 Y | `overrideFinalColorGradient` 及对应字段 | 导入完整渐变参数。 |
| 场景阴影 | `SceneShadowMode` | `overrideSceneShadowMode`、`sceneShadowMode` | Clip 只覆盖角色最终采样的阴影模式，不能把相机级 POS 方向误认为角色局部值。 |
| 高质量阴影方向 | 跟随主光开关、`GetHighQualityShadowDirection()` | `overrideHighQualityShadowDirection`、跟随标记、方向 | 主光不是 `Off` 且 Profile 开启跟随时使用 Clip 主光方向。 |
| 阴影强度 | `SelfShadowStrength`、`EnvironmentShadowStrength`、`SpecularShadowStrength` | `overrideHighQualityShadowStrength` 及三个强度字段 | 自阴影/环境阴影按 Profile 的 `0..2` 语义导入；最终消费仍需 Clamp。 |
| 环境光 | `EnableGlobalIndirectLight`、`OverrideSceneAmbient`、`GlobalIndirectIntensity`、`GlobalIndirectTintColor` | `overrideIndirectLight` 及对应字段 | 环境光覆盖要与场景 SH 和共享状态优先级一起验证。 |

### 3.4 运行时新增字段

历史实现为虚拟补光增加了：

- `virtualLightFollowCamera`，默认 `false`；
- `virtualLightCameraOffsetEuler`；
- `virtualLightMaskFollowCamera`，默认 `false`；

Mixer 只有在对应 FollowCamera 开关为 `true` 且 `Camera.main` 存在时才从相机方向解析；否则继续使用旧的 `virtualLightCameraDirection` 和 `virtualLightMaskCameraDirection`。这保证旧 Timeline YAML 缺少新字段时使用默认值，不改变旧方向。

历史实现同时把 POS 阴影强度字段的 Inspector 范围扩展到 `0..2`，Mixer 使用 `Mathf.Clamp(..., 0f, 2f)`。这种范围扩展必须和 Profile 的真实范围、Shader 消费和旧资产验证一起落地，不能只改 Inspector Slider。

## 4. 兼容性结论与注意事项

### 已有 Timeline 的预期兼容性

- 没有点击导入按钮时，Profile 不会被读取，Clip 不会被覆盖。
- 旧 Timeline 没有 `virtualLightFollowCamera` 等新字段时，Unity 使用 `false` 默认值。
- FollowCamera 为 `false` 时，运行时保留原有序列化方向字段。
- 新增字段是附加数据，不改变旧 Clip 的类型名、绑定类型、`ClipCaps` 或 `CreatePlayable`。
- 导入操作支持 Undo，源 Profile 不被修改。

### 必须人工确认的行为

- 当前实现对导入的所有主要参数打开 `override`。这意味着导入的 Clip 会主动覆盖场景 Controller 的同类参数，不是“只填充数值但继续沿用场景开关”。
- 主光模式为 `Off` 时，历史实现仍把 `overrideMainLight` 设为 `true`，用于阻止场景主光泄漏。若项目需求是保留场景主光，需要调整映射和测试。
- Profile 没有虚拟光遮罩跟随相机的独立开关，历史实现无条件把 Clip 的遮罩跟随开关设为 `true`。这应在后续版本中确认或改成显式 Profile 字段。
- BaseLit 场景接收强度、POS 投影方向和相机 RenderFeature 属于共享状态，不能宣称完全按角色隔离。
- 当前检出分支 `feature/CharaOptimization` 的工作树不包含 `CharacterRenderTimelineProfileUtility.cs`；文中实现对应历史提交 `6a271f0a86`。使用前先确认目标分支是否已同步该提交。

## 5. ProjectACG 验证清单

### 静态与 EditMode

- [ ] AOT、Runtime 和 Editor 程序集编译通过。
- [ ] `CharacterRenderTimelineProfileUtility.cs` 和 `.meta` 存在且进入 Editor 程序集。
- [ ] Profile 主光 `Off / Custom / FollowCamera`、背光和方向映射测试通过。
- [ ] 自阴影/环境阴影 `0..2` 的序列化、导入和 Mixer 混合测试通过。
- [ ] 旧 JSON/YAML 缺 follow 字段时，旧方向保持不变。
- [ ] Profile 与 Clip 双向独立测试通过。

### Unity 编辑器与播放

- [ ] 打开旧 Timeline，不导入 Profile，记录并确认画面和 Clip 参数不变。
- [ ] 导入 Profile，确认 Inspector 顶部入口、字段覆盖、Undo 和源 Profile 不变。
- [ ] 导入后调整 Clip，确认只改变当前镜头。
- [ ] 验证主光 `Off`、`Custom`、`FollowCamera` 和无 `Camera.main` 回退。
- [ ] 验证重叠 Clip、不同权重、多个角色、多个相机、Seek、Pause、Stop 和 Graph 销毁。
- [ ] 使用 Frame Debugger/Profiler 检查 Chara V2、BaseLit、POS Pass、Rim 和环境光最终消费链。

## 6. 开发制作经验与工具

### 6.1 迭代路线与可迁移经验

以下时间线按历史提交和功能演进整理，具体文件以目标分支实际检出内容为准：

| 阶段 | 主要工作 | 可迁移经验 |
| --- | --- | --- |
| 7/21 | 建立 CharacterRender Timeline Track 初版 | 先打通 Track、Clip、Mixer 和最小可播放链路，再逐步扩展参数；不要一开始把所有 Profile 字段塞进 Clip。 |
| 7/22-7/28 | 补齐 Clip 参数、拆分 PlayableAsset/Timeline Editor、加入 SceneView 方向手柄和加权混合 | 编辑器数据、运行时行为和 Scene Handle 分层；连续值与离散开关分别定义混合合同。 |
| 9/26 | 参数完整化并接入自动角色绑定 | 先确认绑定目标和最终消费者，再增加参数；绑定失败要有明确诊断和安全回退。 |
| 9/27 | 抽出 `TimelineBindingResolver`，统一绑定入口并缓存绑定/Renderer 查找 | 解析规则集中管理；缓存生命周期与绑定失效清理要同时设计，避免每帧查找和跨角色串值。 |
| 10/10 | 增加 Profile→Clip 导入和 EditMode 回归测试 | Profile 只提供艺术基线，Clip 保存镜头快照；导入必须是显式动作，并用测试锁定字段映射和旧资产默认值。 |
| 10/13 | 解耦 `CharacterLightData` 与 Controller 的阴影/灯光状态 | 把数据快照、运行时控制和共享状态仲裁分开，避免一个 Controller 同时承担资产、播放和全局状态。 |
| 10/14 | 扩展 RenderTarget 选择范围和 Track 自动绑定 | 自动化只负责减少重复操作，不能绕过绑定校验、作用域限制和人工确认。 |

经验可以概括为：先定义数据所有权，再确定序列化快照；先保证旧 Timeline 的默认路径，再增加显式开关；先验证最终 Shader/RenderFeature 消费，再判断 Inspector 参数是否真正生效。

### 6.2 工具分工

| 工具或 API | 用途 | 使用边界 |
| --- | --- | --- |
| Unity Timeline Editor | 创建 Track/Clip、调整时长和权重、Seek/Pause/Stop 验证 | 只能证明编辑器和播放表象；最终效果仍需追到 Mixer 与渲染 Pass。 |
| `SerializedObject` / `SerializedProperty` | Inspector 序列化读写、刷新和撤销链 | 先 `Update`，改完 `ApplyModifiedProperties`；不要在普通绘制路径隐式覆盖手调值。 |
| `EditorGUI.ObjectField` | 顶部选择 Profile 资产 | 限制类型；临时选择在导入后清空，不把它保存成运行时依赖。 |
| `Undo.RecordObject` / `EditorUtility.SetDirty` | 记录 Clip 修改并保证 Timeline 子资源保存 | 只记录被修改的 Clip；不要为了导入把 Profile 或共享材质标脏。 |
| `SceneView.RepaintAll` | 导入后立即刷新 SceneView 预览 | 仅用于编辑器刷新，不能代替运行时状态同步或恢复逻辑。 |
| PlayableAsset / PlayableBehaviour / Track / Mixer | 保存 Clip 数据、构建 Playable、按权重混合和写入目标 | Clip 不直接改材质；共享相机/场景状态经协调器仲裁。 |
| `MaterialPropertyBlock` | 写入角色绑定 Renderer 的局部 Shader 覆盖 | 必须按 Timeline owner 清理，不能全局清空其他系统的 MPB。 |
| EditMode 测试 | 验证 Profile→Clip 映射、默认值、空引用和新旧序列化兼容 | 不能替代 Unity 场景中的可见性、绑定和最终渲染验证。 |
| Frame Debugger / Profiler | 检查最终 Pass、Mask/强度消费、GC 和每帧查找 | 重点检查 Chara V2、BaseLit、POS、Rim、环境光和绑定缓存链路。 |
| Git 历史、`rg`、`git diff --check` | 定位实现提交、确认字段覆盖和发现格式问题 | 历史提交只能作为实现证据，不能代替当前分支编译和 Unity 验证。 |

### 6.3 问题定位顺序

遇到“参数面板有值但画面不对”时，按以下顺序排查：

1. Profile getter 是否返回了迁移/归一化后的预期值；
2. 导入映射是否写到了正确的 Clip 字段，且 `override` 已按语义设置；
3. Clip 是否被正确绑定、Mixer 是否按权重读取并在每帧重置聚合状态；
4. 目标 Renderer、Controller 或共享协调器是否收到值，是否被更高优先级覆盖；
5. Shader Pass、RenderFeature、Mask 和材质开关是否真正消费该值；
6. Clip 结束、权重归零、Seek、Stop 或 Graph 销毁后，Timeline owner 是否完成恢复和清理。

## 7. 后续维护建议

1. 为 Profile 与 Clip 维护一张机器可检查的字段映射表，新增 Profile 字段时让测试或审查清单提示 Utility 是否漏映射。
2. 给导入按钮增加“全量导入/按分组导入”选择，避免完整覆盖与局部覆盖语义混在一个按钮中。
3. 为 Profile 增加版本号或导入快照标识，便于识别 Clip 是否基于旧版本 Profile。
4. 为虚拟光遮罩跟随相机补充 Profile 开关，消除当前无条件开启的隐式决策。
5. 将旧 Timeline A/B、导入后二次调整、Undo 和共享状态验证纳入 Unity 验收，而不是只依赖静态 C# 或 EditMode 测试。

