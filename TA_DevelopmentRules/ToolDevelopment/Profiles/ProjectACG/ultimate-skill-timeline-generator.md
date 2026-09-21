---
name: projectacg-ultimate-skill-timeline-generator-profile
description: ProjectACG Timeline 基础资产生成器的产品合同、Odin UI、镜头/FOV、模块组装、Prefab 持久绑定、故障归因、验证矩阵与维护规则 Profile。
---

# ProjectACG Timeline 基础资产生成器 Profile

> 类型：`PROFILE`。适用范围：ProjectACG `Client` 工作区中的 `UltimateSkillCameraTimelineGenerator`，包括大招技能、剧情过场和抽卡角色表演三种制作类型。通用工具规则见 [TA 工具开发模块](../../README_Tech_TAToolDevelopmentRules.md)，通用 Timeline 规则见 [Timeline 开发模块](../../../TimelineDevelopment/README_Tech_TimelineDevelopmentRules.md)。本文件只记录当前项目的类型、路径、命名、层级、运行时契约和已验证事实，不把 ProjectACG 的固定实现提升为 CORE。
>
> 最后整理：`2026-09-18`（Asia/Shanghai）。

## 1. 工具定位与当前入口

该工具面向美术，产出的是可继续编辑的基础 Timeline、Prefab 和模块资源，不负责自动完成最终演出。设计目标按优先级排列为：

1. 操作简单：输入只保留演出标识、生成位置和资源列表。
2. 结构稳定：同类 Timeline 使用相同轨道名、Prefab 层级和绑定方式。
3. 可重复生成：更新原资源并保留 GUID，尽量保留美术新增内容。
4. 可诊断：失败时中文说明具体资源、轨道或引用，不让美术只读异常堆栈。
5. 运行简单：多镜头共用一条动画轨和一个驱动实例，避免每镜头状态机、子 Timeline 与同步组件叠加。

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 工具目录 | `Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator/`。 |
| 编辑器主类 | `UltimateSkillCameraTimelineGeneratorWindow`，按主流程、Odin UI、Validation、Presentation 拆为 partial 文件。 |
| 菜单入口 | `TA_Tools/Animation/技能大招资产生成器`。历史菜单名暂未随能力扩展而改名。 |
| UI | `OdinEditorWindow`。`_settings` 使用 `[InlineProperty] + [HideLabel]`，避免空的 `Settings` 区和窄列。 |
| 运行时绑定 | `GameLogic.Battle.UltimateTimelineBindings`、`UltimateTimelineRuntimeContext`。 |
| 场景预览 | `UltimateTimelineBindingsEditor` 和 Editor-only `UltimateTimelineEditorFovPreview`。 |
| 主要依赖 | Unity Timeline、Cinemachine、URP Volume、自定义 Mount Follow Track/Clip。 |
| 工具内说明 | 同目录的 `README_Art_UltimateSkillCameraTimelineGenerator.md` 和 `README_Tech_UltimateSkillCameraTimelineGenerator.md`。 |

## 2. 需求演进后形成的最终合同

### 2.1 三种制作类型

窗口顶部使用三个横排按钮切换制作类型，不使用制作类型下拉框：

| 制作类型 | 当前产物与边界 |
| --- | --- |
| 大招技能 | 延续角色目录、`tl_hero_*`、`tPre_hero_*`、Cinemachine Shot、FX 子 Timeline 等完整工作流。 |
| 剧情过场 | 生成 `tl_story_<演出名>.playable` 与 `tPre_story_<演出名>.prefab`；支持多角色动作、多镜头、道具、音效和特效，但当前不实现 Storyline 节点剧情系统。 |
| 抽卡角色表演 | 生成 `TL_Gacha_Char_<角色编号>.playable` 与 `Stage_Gacha_Char_<角色编号>.prefab`，并配置 `PresentationStageBinder`、`PresentationSignalReceiver`。 |

每种制作类型都可以选择三种镜头驱动方式：

| 镜头模式 | 工作方式 | 适用情况 |
| --- | --- | --- |
| `VirtualCamera` | 动画驱动共享机位，Cinemachine Virtual Camera 跟随机位，Brain 输出到渲染 Camera。 | 大招默认；需要镜头混合、优先级和 Cinemachine 工作流。 |
| `DirectRenderCamera` | Timeline 先驱动 Prefab 内代理 Camera，再把 Transform/FOV 同步到现有渲染 Camera。 | 场景已有唯一渲染 Camera，不想创建虚拟相机输出。 |
| `PrefabEntityCamera` | Prefab 内实体 Camera 直接负责渲染，演出期间临时切换外部 Camera 状态。 | 独立演出 Prefab，希望最短输出链路。 |

三种模式共用镜头动画资源和 `CameraAnimation` 轨道。差异只在最终输出层，不应复制三套镜头导入逻辑。

### 2.2 面向美术的输入

大招模式只暴露一个生成目录 ObjectField 和五类列表：

| 模块 | 输入类型 | 规则 |
| --- | --- | --- |
| 动作 | `List<AnimationClip>` | 直接引用已有动画，按列表顺序连续组装，不复制。 |
| 镜头 | `List<UnityEngine.Object>` | 选择含动画的镜头 FBX，生成 `.anim` 和纯机位 Prefab。 |
| 道具 | `List<GameObject>` | 直接选择已完成的 Prefab，不复制资源。 |
| 音效 | `List<AudioClip>` | 直接进入主 Timeline 的 `AudioTrack`，不生成音效 Prefab。 |
| 特效 | `List<string>` | 按名称生成 FX Prefab、FX 子 Timeline 和后处理资源。 |

不要重新引入“源动画片段”等包装对象。列表元素就是 ObjectField 或字符串；列表顺序就是 Timeline Clip 顺序。

操作区保留：

```text
生成并组装全部

只生成道具 | 只生成音效 | 只生成镜头 | 只生成特效
道具组装   | 音效组装   | 镜头组装   | 特效组装

只组装主 Timeline
```

“只生成”不应修改主 Timeline；“模块组装”只更新目标模块。道具和音效没有独立产物，因此对应“只生成”只负责校验/提示，真正写入发生在组装阶段。独立的“检查输入”按钮已删除，每个执行入口自动按作用域校验。

## 3. 命名与目录

### 3.1 大招资源

工具不强制源资源名称。源 FBX 能解析 `cam_##` 时使用该编号，否则按有效列表项顺序生成 `01`、`02`。输出名称仍固定：

| 资产 | 名称 | 位置 |
| --- | --- | --- |
| 主 Timeline | `tl_hero_<heroId>_<skill>.playable` | 用户选择的生成目录。 |
| 主 Prefab | `tPre_hero_<heroId>_<prefabSkill>.prefab` | 同一生成目录；例如 `bigskill02` 沿用为 `bigskill2`。 |
| 镜头动画 | `ani_c_hero_cam_<NN>_<heroId>_<skill>.anim` | 对应角色的 `anim_clip/action/`。 |
| 镜头 Prefab | `tPre_cam_<NN>_<heroId>_<skill>.prefab` | 生成目录的 `cam/`。 |
| FX Timeline | `tl_fx_<heroId>_<effect>.playable` | 生成目录的 `fx/`。 |
| FX Prefab | `tPre_fx_<heroId>_<effect>.prefab` | 生成目录的 `fx/`。 |
| 后处理 | `pp_fx_<effect>.asset` | 生成目录的 `pp/`。 |

不生成 `prop/` 或 `sound/` 目录。镜头不生成 Animator Controller，不生成 `tl_cam_*` 子 Timeline；重复生成会清理这两类固定路径上的历史遗留产物。

### 3.2 命名校验策略

- 源 FBX、AnimationClip、Prefab 和 AudioClip 的文件名不作为阻断条件。
- `heroId`、技能名、演出名中的非法路径字符由工具归一化，不要求美术先手改名。
- 资源缺失、类型错误、输出目录不在 `Assets` 下、重复镜头编号导致同一路径冲突时必须停止。
- 空列表行跳过，不应导致编号错位；编号按有效项计算。

这一区分很重要：放宽源命名不等于放弃输出确定性。源资源可以任意命名，生成资源仍必须遵守项目契约。

## 4. 主 Timeline 和主 Prefab 结构

### 4.1 工具轨道

大招主 Timeline 的工具轨道顺序为：

1. `Ultimate_Character`：一个 `AnimationTrack`，多个角色动作连续排列。
2. `Camera Group`：一条 `CameraAnimation` 和一条 `Ultimate_Camera`。
3. `Prop Track`：一个 `MountFollowTrack`，所有道具各占一个 Clip。
4. `Sound Track`：一个 `AudioTrack`，直接引用源 AudioClip。
5. `FX Track`：一个 `ControlTrack`，控制主 Prefab 内的 FX 实例。

`CameraAnimation` 只有一条。多个镜头动画 Clip 按列表顺序首尾排列，Transform 与 FOV 曲线保存在同一个 Clip 内。不要恢复旧版的 `Camera Animation 01/02` 和 `Camera FOV 01/02` 轨道。

主长度优先取全部角色动作长度之和；没有动作时回退到镜头动画总长度，最小值为 `0.01s`。镜头动画 Clip 与对应 Cinemachine Shot 使用相同的 `start` 和 `duration`。Prop、Sound、FX 的默认 Clip 铺到主 Timeline 结束。

### 4.2 固定 Prefab 层级

三种制作类型统一使用以下语义层：

```text
<main prefab>
├─ chara_group
├─ camAni_group
├─ camVirtual_group
│  ├─ Battle_CameraPos
│  └─ Virtual_Camera_01 ...
├─ fx_group
├─ prop_group
└─ sound_group
```

大招模式中：

- `chara_group` 只提供固定层级，真实角色由运行时绑定，不复制进 Prefab。
- `camAni_group` 只放一个共享镜头驱动实例；多个镜头通过一条轨道上的多个 Clip 依次求值。
- `camVirtual_group` 放归位虚拟相机和工具生成的 Shot 虚拟相机。
- `fx_group` 放主 Prefab 内的特效 Prefab 实例。
- `prop_group` 放现成道具 Prefab 实例。
- `sound_group` 保留统一层级，但 Sound 直接在 Timeline 播放，不创建音效对象或 `AudioSource`。

旧层级 `Cam_Ani`、`Cam_Virtual`、`Fx`、`Prop` 已废弃，不要在新逻辑里重新生成。

## 5. 镜头资产、共享驱动与三种输出模式

### 5.1 独立镜头 Prefab 必须保持纯净

`cam/tPre_cam_<NN>_*.prefab` 只保存 FBX 的机位 Transform 层级，通常包含 `Camera_Root/Camera` 和 `Camera_Root/Camera.Target`。它不应包含：

- `Camera` 与 URP Additional Camera Data；
- `Animator` 和 Animator Controller；
- `PlayableDirector` 或镜头子 Timeline；
- `UltimateTimelineCameraTransformSync`；
- Cinemachine Virtual Camera 或 `cm` 管线节点。

独立镜头 Prefab 是可复用的机位数据，不是最终播放节点。把运行组件塞进每个镜头 Prefab 会造成重复组件、状态机与 Timeline 争抢曲线，以及后续维护困难。

### 5.2 为什么主 Prefab 内又会出现 Camera 和 Animator

组装主 Prefab 时，工具把第一项镜头 Prefab 的普通实例放进 `camAni_group`，作为所有镜头 Clip 共用的驱动层级，并只在这个主 Prefab 实例上补：

- 一个无 Controller 的 `Animator`，作为 `CameraAnimation` 的 Generic Binding；
- `Camera_Root/Camera` 上的一个 Camera，作为 `Camera.fieldOfView` 曲线的接收器。

这不违反“镜头 Prefab 不带 Camera”的规则。两者生命周期不同：`cam/` 下的模块 Prefab保持纯净，主 Prefab 内的共享实例负责 Timeline 求值。

虚拟相机和直接驱动模式下，这个 Camera 默认禁用，因此不会参与 Culling、渲染或产生额外画面；它只保存被动画求值后的 FOV。实体 Camera 模式下它才会启用并直接输出。单个禁用 Camera 与一个共享 Animator 的成本可控，远低于每个镜头各放一套 Camera、Animator、Controller 和子 Timeline。

### 5.3 三种输出模式的运行路径

```text
CameraAnimation Clip
    -> camAni_group/共享 Animator
    -> Camera_Root/Camera 的 Transform + FOV
       -> VirtualCamera：Virtual_Camera_## + CinemachineBrain
       -> DirectRenderCamera：复制到场景渲染 Camera
       -> PrefabEntityCamera：启用 Prefab 内实体 Camera
```

`PrefabEntityCamera` 链路最短，但不能脱离画面栈、后处理、相机切换和平台验证就简单判定为“绝对性能最优”。真正的选择依据是是否需要 Cinemachine 混合、是否必须复用现有渲染 Camera，以及演出期间由谁拥有最终输出。

## 6. FOV 的最终实现

3ds Max 导出的镜头动画同时包含位置、旋转和 FOV。生成阶段把曲线路径归一到共享层级的 `Camera_Root/Camera`，FOV 保持为 `Camera.fieldOfView`，并与 Transform 一起保存在对应的镜头动画 Clip 中。

旧版每镜头一条 `Camera FOV ##` 的问题是：

- 轨道数量随镜头数增长；
- Transform/FOV 的时序容易不一致；
- 镜头替换后容易残留旧 FOV 轨；
- 一次镜头修改需要同时维护两条轨。

当前方案把多个镜头动画放在同一条 `CameraAnimation` 上，Clip 自己携带 Transform 与 FOV。虚拟相机模式的正式运行时流程是：

1. Timeline 求值共享 Camera 的 Transform 与 `fieldOfView`。
2. `UltimateTimelineRuntimeContext` 在 Timeline 求值后读取共享 Camera FOV。
3. 把该值同步给工具生成的 `Virtual_Camera_##.m_Lens.FieldOfView`。
4. 当前 Cinemachine Shot 通过 Brain 输出最终画面。

Editor Timeline 预览没有正式战斗更新循环，因此 `UltimateTimelineEditorFovPreview` 只在 `AnimationMode` 期间执行同类同步。它必须满足：

- 仅编译进 Editor；
- 不挂到 Prefab；
- 不进入 Player；
- 不产生运行时轮询成本；
- 停止预览、切换 Timeline、进入 Play Mode 或程序集重载时恢复原 Lens；
- 用 `AnimationMode` 临时属性记录，避免把预览值保存成 Prefab/场景覆盖。

## 7. Cinemachine 结构与 `Battle_CameraPos`

### 7.1 `Virtual_Camera_##`

每个工具镜头对应一个 Cinemachine Shot 和一个 `Virtual_Camera_##`：

- Priority：`10`；
- Follow：共享机位的 `Camera_Root/Camera`；
- LookAt：优先使用 `Camera_Root/Camera.Target`；
- Body：`CinemachineHardLockToTarget`；
- Aim：`CinemachineSameAsFollowTarget`；
- Damping：保持为 `0`，让输出严格跟随 Max 动画。

### 7.2 `Battle_CameraPos`

`Battle_CameraPos` 是 Prefab 内的归位虚拟相机，不是 Timeline 镜头 Clip：

- 位于 `camVirtual_group`；
- Local Position/Rotation 为 `000`，Local Scale 为 `1`；
- 默认 FOV 为 `50`，Priority 为 `9`；
- 挂 `CinemachineVirtualCamera`；
- 挂无 Controller 的 `Animator`；
- 保留标准 `cm/CinemachinePipeline` 管线容器，但不挂 Body/Aim/Noise 组件，因此 Inspector 显示 Body=`Do nothing`、Aim=`Do nothing`、Noise=`None`；
- Follow/LookAt 默认留空；
- 不自动写入 `Ultimate_Camera`，由美术决定是否添加归位 Shot。

### 7.3 为什么 `Battle_CameraPos` 保留空的 `cm`

`Battle_CameraPos` 是原点归位相机，不跟随 `camAni_group` 的动画机位。截图中的 Cinemachine Inspector 配置 `Body=Do nothing`、`Aim=Do nothing`、`Noise=None`，对应没有 Body/Aim/Noise Pipeline Component，但保留一个空的 `cm/CinemachinePipeline` 容器，使它与 `Virtual_Camera_##` 使用相同的序列化生成结构。只有 `Virtual_Camera_##` 才需要在该容器中挂 Hard Lock / Same As Follow Target 管线组件。

早期把 `Battle_CameraPos` 错误地套用了动画虚拟相机管线，或在场景 Prefab Instance 上让 Inspector 临时补管线，出现了：

```text
It's not possible to add this component to a prefab instance
```

所以最终规则是：`Battle_CameraPos` 保留 `CinemachineVirtualCamera + Animator + 空的 cm/CinemachinePipeline`；`Virtual_Camera_##` 在同一结构中额外挂 Hard Lock / Same As Follow Target，不能混用两种管线配置。

如果旧的 `Battle_CameraPos` 本身是嵌套 Prefab Instance，生成器在修改工具独占节点前先解包，再添加/替换 Cinemachine 组件。否则即使代码调用 `AddComponent` 正确，也会因 Prefab Instance 限制失败。只应解包工具拥有的节点，不要无条件解包美术的任意嵌套 Prefab。

## 8. Prop、FX 与 Sound 组装

### 8.1 Prop

所有道具共用一条 `Prop Track`。每个 `MountFollowClip.sceneTarget.exposedName` 使用稳定引用名：

```text
<heroId>_<skill>_prop_<NN>
```

引用值必须指向 `prop_group` 下的 Prefab 实例，不能指向 Project 窗口里的源 Prefab。默认挂点 `weapon_r` 和跟随轴由 Track/Clip 默认值提供，偏移留给美术调整。

### 8.2 FX

每个特效生成：

- `pp_fx_<effect>.asset`；
- 含 `Light`、`PostProcess`、`FXPrefab`、`FXScript`、`BJ` 五个默认 Group 的 FX 子 Timeline；
- 挂该子 Timeline 的 FX Prefab。

主 Timeline 上的 `ControlPlayableAsset.prefabGameObject` 保持 `null`，`sourceGameObject` 指向主 Prefab 的 `fx_group` 下对应 FX 实例。也就是说 Inspector 的 `Parent Object/Source Game Object` 是 Hierarchy 内对象，不是 Project 资源。

已存在的 FX Prefab 和 FX 子 Timeline 采用增量补齐：缺什么补什么，不删除美术已经增加的 FX 节点、轨道和已调整的 Volume 参数。

### 8.3 Sound

Sound 直接使用主 Timeline 的 `AudioTrack` 和源 AudioClip：

- 不复制音频；
- 不生成 Sound Prefab；
- 不生成 `sound/` 目录；
- 主 Prefab 不挂 `AudioSource`。

不要因为 Timeline 有 AudioTrack 就在主 Prefab 上预放一个 AudioSource。当前工具明确清理历史 AudioSource，避免无主绑定、重复播放组件和无意义层级。

## 9. Timeline 引用为什么会丢，以及如何正确落盘

Timeline Asset 上“有 Clip”和 Prefab 内 PlayableDirector“引用已保存”是两层状态。Prop 场景目标、FX Parent Object 和 Cinemachine Shot 都依赖 Prefab 场景实例，不能只把 Project 资源写进 Clip。

最终采用两遍保存：

1. 用 `PrefabUtility.LoadPrefabContents` 创建/更新主 Prefab 层级并保存，让子对象取得稳定 fileID。
2. 重新加载已保存的 Prefab，在最终持久对象上写入 Generic Binding 和 ExposedReference，再保存并回读。

关键实现规则：

- `PlayableDirector.playableAsset` 与序列化字段 `m_PlayableAsset` 保持一致。
- Generic Binding 除 `SetGenericBinding` 外，要确认 `m_SceneBindings` 已落盘。
- Shot、Prop、FX 使用 `PlayableDirector.SetReferenceValue(exposedName, prefabChild)`。
- ExposedReference 名称通过 `SerializedObject` 读取 `exposedName`，不能用 `PropertyName.ToString()`；后者会混入内部 ID，导致按名字查找失败。
- 保存后用 `GetReferenceValue` 回读，必要时再检查 Prefab 文本中的 `m_ExposedReferences`，不能以第一次内存对象不为空作为成功证据。

典型丢失原因：

| 现象 | 根因 | 解决方式 |
| --- | --- | --- |
| `Virtual_Camera_01` 显示 None | Shot 指向保存前的临时对象或引用名匹配错误。 | 两遍保存，在最终 Prefab 子对象上重写并回读。 |
| Prop 场景目标丢失 | Clip 绑定了源 Prefab或临时实例。 | `sceneTarget` 绑定 `prop_group` 下的最终实例。 |
| FX Source Game Object 丢失 | `prefabGameObject` 仍有值，或没有回填 `sourceGameObject`。 | 清空 Project Prefab 引用，绑定 `fx_group` 下的最终实例。 |
| 第一次看似成功，重开 Prefab 后丢失 | 只改了内存引用表，没有确认序列化结果。 | 保存、重新加载、回读验证，失败则中文中断。 |

## 10. 场景预览绑定

主 Prefab 根节点挂 `UltimateTimelineBindings`。把 Prefab 拖进场景后，美术可以选择一个预览角色并点击“一键配置预览绑定”：

- `Ultimate_Character` 绑定所选场景角色自身或子层级中的唯一 Animator；
- `Ultimate_Camera` 绑定场景中唯一名为 `mainCamera_CJ_1` 的对象上的 `CinemachineBrain`；
- 角色选择用 `SessionState` 记忆当前 Unity 编辑会话，不写回 Prefab；
- 操作只修改场景 Prefab Instance 的 Director Binding，正式战斗仍由运行时上下文覆盖。

按钮必须拒绝 Project Prefab、Prefab Mode 对象、没有 Animator 或有多个候选 Animator 的角色、重名 `mainCamera_CJ_1`，以及缺少 Camera/CinemachineBrain 的场景相机。这样可以把“预览方便”与“运行时依赖”分离，不为打包加入编辑器查找逻辑。

## 11. 重复生成与美术内容保护边界

### 11.1 应当保留

- 主 Timeline/Prefab 在原路径更新，保留 `.meta` 和 GUID。
- 只删除工具通过固定名称/前缀拥有的轨道；美术新增的其它根轨道保留。
- 重建 Camera Group 时捕获并恢复不属于工具的手工 Cinemachine Shot。
- 特效子 Timeline 只补缺失结构，不清空美术轨道。
- 已存在的 FX Prefab 和 VolumeProfile 复用，不覆盖美术参数。

### 11.2 工具拥有并会重建

- `Ultimate_Character`、`Camera Group`、`CameraAnimation`、`Ultimate_Camera`、`Prop Track`、`Sound Track`、`FX Track`。
- 旧版固定前缀的 `Camera Animation ##`、`Camera FOV ##` 等生成轨道。
- 主 Prefab 的 `camAni_group`、`fx_group`、`prop_group` 中由工具按当前列表生成的实例。
- `camVirtual_group` 下工具命名的 `Virtual_Camera_##`。

因此“保留美术内容”不是无条件保存整个工具区域。美术自定义轨道应使用非工具名称；不能把唯一手工内容放进会被工具清空重建的节点。新增工具轨道或前缀时，必须同步更新所有权识别、清理、自检和文档，避免误删。

## 12. 已解决问题与工程经验

| 问题 | 根因 | 最终处理 |
| --- | --- | --- |
| 生成镜头时报缺少 Animator | 代码默认访问组件，但镜头模块 Prefab 按规则不能带 Animator。 | 模块 Prefab 保持纯净；只在主 Prefab 的共享实例上补无 Controller Animator。 |
| 镜头只播放位置旋转，FOV 不动 | 移除 Camera 后，FOV 曲线没有接收器。 | 主 Prefab 共享实例补禁用 Camera；运行时/Editor 预览把求值结果同步到输出相机。 |
| FOV 轨道太多且有残留 | 每镜头拆成独立 FOV Track，清理与时序都复杂。 | Transform/FOV 合并进同一 Clip，所有镜头共用一条 `CameraAnimation`。 |
| `Battle_CameraPos` 的 Body/Aim 配置错误或 Prefab Instance 报错 | 错把动画虚拟相机的 Hard Lock/Same As Follow Target 管线套给归位相机，旧节点还可能是嵌套实例。 | 生成时解包工具节点，保留标准 `cm/CinemachinePipeline` 容器，清除其中的 Body/Aim/Noise Pipeline Component，保持 Do nothing/Do nothing/None。 |
| Shot/Prop/FX 生成后显示 None | 绑定临时对象、引用名读取错误，或只改内存未落盘。 | 两遍 Prefab 保存、稳定 exposedName、最终对象回填、重新加载自检。 |
| 主 Prefab 出现 AudioSource | 把 AudioTrack 误解为必须预挂播放器。 | Sound 直接引用 AudioClip，生成器清理主 Prefab AudioSource。 |
| 镜头资源出现 Controller、子 Timeline 和同步脚本 | 每镜头生成完整播放单元导致资产冗余和职责重叠。 | 镜头模块只保留 `.anim` + 纯机位 Prefab；统一由主 Timeline 驱动。 |
| Odin 界面空白多、资源拖不进去 | 嵌套 Settings 和自定义列表项让 Odin 画出多层字段。 | Settings 平铺、列表直接使用 Unity Object 类型、按钮按生成/组装两行排列。 |
| 命名不合规就完全不能生成 | 把源文件规范误当成工具输入门槛。 | 不强制源名称，只保证输出名称和冲突检测。 |
| 单模块组装被其它模块错误拦截 | 自检没有按 Scope 分支。 | 校验、生成和生成后自检统一使用模块作用域。 |
| 重复生成丢 GUID 或美术轨道 | 删除后重建整个资源。 | 原路径更新，只重建工具拥有内容。 |

最关键的通用经验是：先明确资产所有权，再写重复生成。Timeline 轨道、Prefab 子节点和 ExposedReference 必须各自区分“工具拥有”“美术拥有”“运行时注入”；否则所谓自动生成很容易变成不可预测的破坏性重建。

## 13. 失败停止与中文诊断

生成分为输入预检、资源生成、组装、落盘自检。可以在写入前确定的问题必须先中止；生成后发现引用丢失也必须判定失败，不能打印成功后让美术在 Inspector 中自行发现 None。

错误摘要至少包含：

- 当前制作类型与作用域；
- 失败模块；
- 资源路径或 Clip/Track 名称；
- 期望类型/结构；
- 建议修复动作。

异常堆栈保留给程序定位，但第一条 Console 错误应是中文原因。需要注意：AssetDatabase 写入不是完整事务；“生成失败”不等于自动回滚所有已写资产。涉及多个文件的失败后，应重新打开目标资源核对，或在修正输入后重复生成恢复一致状态。

## 14. 验证矩阵

### 14.1 已执行的静态验证

当前代码曾执行：

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore `
  -p:UseSharedCompilation=false -p:WarningLevel=0 -v:minimal
```

结果为 `0 errors`。由于命令显式设置 `WarningLevel=0`，这只能证明该配置下编译无错误，不能作为“工程没有 Warning”的证据。另已执行 `git diff --check`，未发现空白错误。

### 14.2 Unity 内必须人工复验

| 场景 | 必查项 |
| --- | --- |
| 完整生成 | `All` 可生成/更新全部资产，目录与名称正确。 |
| 单模块 | 四个“只生成”和四个“组装”按钮只影响目标模块。 |
| 重复生成 | GUID 不变；非工具轨道、手工 Shot、FX 内容和 Volume 参数仍在。 |
| 保存重开 | 关闭并重新打开 Prefab 后，Shot、Prop、FX 引用仍有效。 |
| 多动作/多镜头 | Clip 严格按列表顺序连续，长度、切镜和 FOV 对齐。 |
| FOV | Timeline Play、Seek、Stop、重复播放时 FOV 正确，退出预览恢复 Lens。 |
| `Battle_CameraPos` | 原点、FOV 50、Animator 无 Controller、Body/Aim 为 Do nothing、Noise 为 None，并且存在空的标准 `cm/CinemachinePipeline`。 |
| 三种镜头模式 | Virtual、Direct、Prefab Entity 分别验证最终输出、禁用/恢复状态和后处理表现。 |
| 场景预览 | 一键绑定角色与 `mainCamera_CJ_1`，切换场景/角色后不保留错误引用。 |
| 异常输入 | 空列表、重复镜头编号、无动画 FBX、只读目录和中途异常有中文原因。 |
| 性能 | Profiler 检查没有多 Camera 重复渲染、无额外运行时 Editor 服务、无每帧对象查找/集合分配。 |

静态编译不能证明 Unity 序列化、Cinemachine 管线、Prefab fileID、Timeline Preview 或最终画面正确。涉及这类改动时，Unity 内复验是交付必要条件。

## 15. 维护检查单

- [ ] 修改前已读取工具 Art/Tech README、本 Profile、目标主 Timeline/Prefab。
- [ ] 新字段或按钮已同步 UI、Scope、校验、执行、覆盖提示和生成后自检。
- [ ] 三种制作类型仍用顶部横排按钮；每种类型仍能选择三种镜头驱动方式。
- [ ] 动作、镜头、道具、音效、特效仍使用简单列表，顺序等于 Clip 顺序。
- [ ] 独立镜头 Prefab 没有 Camera、Animator、Controller、子 Timeline 和同步脚本。
- [ ] 主 Prefab 共享镜头实例有一个 Animator 和 FOV 接收 Camera；Camera 启用状态符合驱动模式。
- [ ] 多镜头仍合并到一条 `CameraAnimation`；没有重新生成独立 FOV Track。
- [ ] `Battle_CameraPos` 保持原点、FOV 50、Do nothing/Do nothing/None、存在空的标准 `cm/CinemachinePipeline`，且不自动生成 Shot。
- [ ] 主 Prefab 层级仍为 `chara_group/camAni_group/camVirtual_group/fx_group/prop_group/sound_group`。
- [ ] Prop/FX/Shot 的 ExposedReference 指向主 Prefab 内最终实例，不指向 Project 资源或临时对象。
- [ ] Sound 仍直接使用 AudioTrack；未恢复 Sound Prefab、sound 目录或主 Prefab AudioSource。
- [ ] 重复生成只清理工具拥有的轨道/节点；非工具根轨和 FX 美术内容保留。
- [ ] 保存后重新加载并验证绑定，未把内存状态当作落盘成功。
- [ ] 预览 FOV 服务仍是 Editor-only，并覆盖退出预览、Play Mode、切换 Timeline 和程序集重载恢复。
- [ ] 完成 Editor 编译、Unity 按钮、资源重开、Timeline Play/Seek/Stop 和代表性镜头视觉验证。

## 16. 关联参考

- [Timeline 资产生成、Prefab 绑定与增量组装](../../../TimelineDevelopment/references/timeline-asset-generation-and-prefab-binding.md)
- [Timeline 场景对象附着与引用解析](../../../TimelineDevelopment/references/timeline-object-attachment-resolution.md)
- [Timeline 预览刷新与实时值同步](../../../TimelineDevelopment/references/timeline-preview-refresh-and-live-value-sync.md)
- [Unity 动画预览生命周期与姿态恢复](../../references/unity-animation-preview-lifecycle-and-pose-restoration.md)

这些参考文档记录可迁移的方法；本 Profile 中的 ProjectACG 类型名、轨道名、路径、FOV、层级和验证状态仍以当前工程代码为准。
