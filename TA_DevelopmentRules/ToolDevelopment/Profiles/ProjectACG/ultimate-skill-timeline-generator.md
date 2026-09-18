---
name: projectacg-ultimate-skill-timeline-generator-profile
description: ProjectACG 大招 Timeline 基础资产生成器的 Odin UI、模块资产、主 Timeline/Prefab 组装、Cinemachine/FOV、持久绑定、已解决问题与验证边界 Profile。
---

# ProjectACG 大招 Timeline 基础资产生成器 Profile

> 类型：`PROFILE`；适用范围：当前 ProjectACG `Client` 工作区的 `UltimateSkillCameraTimelineGenerator`。通用工具规则见 [TA 工具开发模块](../../README_Tech_TAToolDevelopmentRules.md)，通用 Timeline 规则见 [Timeline 开发模块](../../../TimelineDevelopment/README_Tech_TimelineDevelopmentRules.md)，可迁移实现方法见 [Timeline 资产生成、Prefab 绑定与增量组装参考](../../../TimelineDevelopment/references/timeline-asset-generation-and-prefab-binding.md)。本文件记录项目路径、类型、固定命名、Prefab 层级、已复现问题和当前验证边界，不将项目事实提升为 CORE。
>
> 整理时间：`2026-09-18 23:23`（Asia/Shanghai）。

该工具面向美术，目标不是生成最终大招，而是按固定结构生成基础资产和主 Timeline/Prefab，使美术可以继续制作特效、镜头与细节。工具支持多个动作和多个镜头，列表顺序就是各自 Timeline Clip 的排列顺序。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 工具目录 | `Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator/`。 |
| 主类 | `UltimateSkillCameraTimelineGeneratorWindow`，拆为主流程、Odin UI、Validation 三个 partial 文件。 |
| 菜单 | `TA_Tools/Animation/技能大招资产生成器`。 |
| 命名空间 | `GameLogic.Editor`。 |
| UI 框架 | `OdinEditorWindow`；这是历史工具的兼容选择，不改变 PRJ-TOOL-04 对新工具默认原生 `EditorWindow` 的约定。 |
| 依赖 | Unity Timeline、Cinemachine、URP Volume、自定义 `fx` Track/Clip 与 `GameLogic.Battle` 类型。 |
| 功能文档 | 工具目录内 `README_Art_UltimateSkillCameraTimelineGenerator.md` 与 `README_Tech_UltimateSkillCameraTimelineGenerator.md`。 |
| 参考主资源 | `Assets/AssetRaw/character/hero/100601/timeline/New/tPre_hero_100601_bigskill2.prefab` 及对应主 Timeline。 |
| 编译入口 | `Assembly-CSharp-Editor.csproj`，只作为 Unity 编译的补充证据。 |

窗口状态序列化在 `_settings`。主 Timeline 和主 Prefab 共用一个 `DefaultAsset outputFolder`；若未选择，工具尝试使用角色目录下 `timeline/New`，再回退到 `timeline` 或规则拼接路径。

## 2. 当前行为合同

### 2.1 界面输入

界面只暴露美术真正需要的字段：

| 模块 | UI 类型 | 输入语义 |
| --- | --- | --- |
| 角色/技能 | `string heroId`、`string skillName` | 决定输出名和默认目录。 |
| 生成位置 | `DefaultAsset` ObjectField | 主 Timeline 与主 Prefab 的共同目录。 |
| 动作 | `List<AnimationClip>` | 直接引用已有动画，按列表顺序连续组装。 |
| 镜头 | `List<UnityEngine.Object>` | 拖入镜头 FBX，生成镜头动画与镜头 Prefab。 |
| 道具 | `List<GameObject>` | 直接引用已有 Prefab，不复制。 |
| 音效 | `List<AudioClip>` | 直接引用源音频，不复制、不生成音效 Prefab。 |
| 特效 | `List<string>` | 每行是特效名，用于生成 FX Prefab、子 Timeline 和后处理。 |

列表元素不再包装成“源动画片段”等自定义输入对象。`[InlineProperty] + [HideLabel]` 用于移除 Odin 默认 `Settings` 表头造成的空白列；长说明和中间产物不显示在主界面。

操作区当前为：

| 左列 | 右列 |
| --- | --- |
| `只生成道具` | `道具组装` |
| `只生成音效` | `音效组装` |
| `只生成镜头` | `镜头组装` |
| `只生成特效` | `特效组装` |

另保留 `生成并组装全部` 和 `只组装主 Timeline`。`检查输入` 已移除；每次执行都会按当前作用域自动校验，失败原因用中文写入 Console。

### 2.2 作用域

`GenerationScope` 当前包含：

```text
All
Camera / CameraAssembly
Sound / SoundAssembly
Fx / FxAssembly
Prop / PropAssembly
Assembly
```

- `Camera`、`Fx` 只生成模块资源，不改主 Timeline。
- Prop 和 Sound 直接引用已有资源，没有独立产物；点击“只生成”只提示应使用对应组装按钮。
- 四个 `*Assembly` 只更新对应模块。
- `Assembly` 使用已有模块资源完整重组。
- `All` 先生成镜头/特效资源，再完整组装。

### 2.3 输出命名与目录

工具不强制源资源名称；源 FBX 名中能解析 `cam_##` 时使用该编号，否则按列表位置使用 `01`、`02`。输出名仍按项目固定规则生成，重复输出路径会在写入前阻断。

| 资产 | 格式 | 目录 |
| --- | --- | --- |
| 主 Timeline | `tl_hero_<heroId>_<skill>.playable` | 生成位置。 |
| 主 Prefab | `tPre_hero_<heroId>_<prefabSkill>.prefab` | 生成位置；`bigskill02` 沿用为 `bigskill2`。 |
| 镜头动画 | `ani_c_hero_cam_<NN>_<heroId>_<skill>.anim` | `Assets/AssetRaw/character/hero/<heroId>/anim_clip/action/`。 |
| 镜头 Prefab | `tPre_cam_<NN>_<heroId>_<skill>.prefab` | 生成位置的 `cam/`。 |
| FX Timeline | `tl_fx_<heroId>_<effect>.playable` | 生成位置的 `fx/`。 |
| FX Prefab | `tPre_fx_<heroId>_<effect>.prefab` | 生成位置的 `fx/`。 |
| 后处理 | `pp_fx_<effect>.asset` | 生成位置的 `pp/`。 |

不生成 `prop/` 或 `sound/` 目录。镜头不生成 Animator Controller，也不生成 `tl_cam_*` 子 Timeline；生成镜头时会清理同名历史 `.controller` 和子 Timeline 产物。

## 3. 主 Timeline 与主 Prefab 结构

### 3.1 主 Timeline

工具维护的根轨顺序是：

1. `Ultimate_Character`：一个 `AnimationTrack`，多个动作按列表顺序连续排列。
2. `Camera Group`：每个镜头一条 `Camera Animation NN`，有 FOV 曲线时增加 `Camera FOV NN`，最后是一条 `Ultimate_Camera` CinemachineTrack。
3. `Prop Track`：一条 `MountFollowTrack`，多个道具各占一个 Clip。
4. `Sound Track`：一条 `AudioTrack`，直接引用 AudioClip。
5. `FX Track`：一条 `ControlTrack`，控制主 Prefab 内的特效实例。

角色动作总长度是所有动作 Clip 长度之和；无动作时回退到镜头动画总长度。镜头 Transform、FOV 和对应 Cinemachine Shot 共用同一 `{ start, duration }`。道具、音效和特效 Clip 从 `0` 铺到主 Timeline 总长度。

多动作与多镜头是两套独立顺序，不要求动作和镜头一一配对。

### 3.2 主 Prefab

工具保证以下固定容器：

```text
tPre_hero_<heroId>_<skill>
├─ Cam_Virtual
│  ├─ Battle_CameraPos
│  ├─ Virtual_Camera_01
│  └─ Virtual_Camera_02 ...
├─ Cam_Ani
│  ├─ tPre_cam_01_<heroId>_<skill>
│  └─ tPre_cam_02_<heroId>_<skill> ...
├─ Fx
│  └─ tPre_fx_<heroId>_<effect> ...
└─ Prop
   └─ <现成道具 Prefab 实例> ...
```

主 Prefab 根节点带 `PlayableDirector`。工具更新已有主 Prefab时使用 `PrefabUtility.LoadPrefabContents`，不采用 delete-then-create；`Cam_Ani`、`Fx`、`Prop` 与工具生成的 `Virtual_Camera_##` 属于工具维护区域。

`Battle_CameraPos`：

- 位于 `Cam_Virtual`，本地位置/旋转为原点 `000`；
- 默认 FOV 为 `50`，Priority 为 `9`；
- `Follow` / `LookAt` 留空；
- 工具不自动在 `Ultimate_Camera` 上生成它的 Shot；
- 美术手动添加的非工具 Cinemachine Shot 会在完整重建前捕获，并按引用名/目标节点尝试恢复。

`Virtual_Camera_##`：

- Priority 为 `10`；
- Body 为 `CinemachineHardLockToTarget`；
- Aim 为 `CinemachineSameAsFollowTarget`；
- Follow 指向对应镜头实例的 `Camera_Root/Camera`；
- LookAt 优先指向 `Camera_Root/Camera.Target`，缺失时回退到 Camera 节点；
- 自身带空 Animator，供 `Camera FOV NN` 绑定。

## 4. 模块生成与组装实现

### 4.1 镜头

镜头 FBX 读取第一个可用 AnimationClip；FBX 含多个片段时记录 Warning。动画复制到角色 `anim_clip/action` 后，工具把源 FOV 曲线从：

```text
path: Camera_Root/Camera
type: Camera
property: field of view
```

重定向为：

```text
path: ""
type: CinemachineVirtualCamera
property: m_Lens.FieldOfView
```

镜头模块 Prefab 只保留 FBX 机位层级，不带 `Camera`、URP Camera Data、Animator、Animator Controller、PlayableDirector、`cm` 子节点或 `UltimateTimelineCameraTransformSync`。

组装主 Prefab 时才在 `Cam_Ani` 下的镜头实例补一个没有 Controller 的 Animator，并把 `Camera Animation NN` 绑定到该 Animator。FOV Track 则绑定到 `Virtual_Camera_##` 自身的 Animator。这样 Transform 动画与 Lens 动画目标清晰，不由状态机与 Timeline 争抢曲线。

### 4.2 道具

道具直接实例化到主 Prefab 的 `Prop` 容器。所有道具共用一条 `Prop Track`；每个 `MountFollowClip.sceneTarget.exposedName` 使用稳定引用名，`PlayableDirector` 的引用值指向对应的 Prefab 内实例，而不是 Project 中的道具 Prefab。

`道具组装` 只删除和重建 `Prop Track` 上的 Clip及 `Prop` 子节点。当前自定义 Track/Clip 的默认挂点与跟随行为以 [ProjectACG Mount Follow Track Profile](../../../TimelineDevelopment/Profiles/ProjectACG/mount-follow-track.md) 为准。

### 4.3 音效

音效不生成 Prefab 或复制资源。`音效组装` 只替换 `Sound Track` 上的 AudioClip，并把每个 Clip 长度设置为主 Timeline 总长度；主 Prefab 只需要已有 `PlayableDirector`，无需增加音效节点。

### 4.4 特效

每个特效名生成：

- 一个 VolumeProfile；
- 一个带 `Light`、`PostProcess`、`FXPrefab`、`FXScript`、`BJ` 默认 TrackGroup 的 FX Timeline；
- 一个挂接该子 Timeline 的 FX Prefab。

组装时 FX Prefab 实例放在主 Prefab 的 `Fx` 容器。主 Timeline 的 `ControlPlayableAsset.prefabGameObject` 保持 `null`，`sourceGameObject` 指向主 Prefab 内 FX 实例；因此 Timeline Inspector 的 `Source Game Object/Parent Object` 不是 Project 中的 FX Prefab 资源。

### 4.5 持久绑定

绑定键使用：

```text
<heroId>_<skill>_camera_<NN>
<heroId>_<skill>_prop_<NN>
<heroId>_<skill>_fx_<NN>
```

实现分两遍：

1. 在 PrefabContents 场景中建立层级、组件和初次绑定并保存；
2. 重新导入、再次加载已保存主 Prefab，从持久化子对象重写绑定并回读验证。

`SetDirectorPlayableAsset` 同步 `m_PlayableAsset`；`SetDirectorBinding` 除调用 `SetGenericBinding` 外还同步 `m_SceneBindings`；ExposedReference 使用 `SetReferenceValue`。这些序列化字段依赖 Unity `2022.3.62f3`，升级 Unity 后要重新验证。

读取 Clip 引用名时通过 `SerializedObject` 访问 `exposedName`，不使用 `PropertyName.ToString()`；后者会混入内部 ID，导致查找生成 Clip 失败。

## 5. 已解决问题与经验

| 现象 | 根因 | 当前解决方式 |
| --- | --- | --- |
| 生成镜头时报“没有 Animator” | 代码访问不存在的组件；镜头模块 Prefab 又按需求不能自带 Animator。 | 镜头模块 Prefab保持纯层级；主 Prefab 实例化后判空补 Animator，再做 Track Binding。 |
| `Virtual_Camera_01` 自检绑定无效 | Shot 的引用没有写入最终 Prefab 持久对象，或引用名匹配失败。 | Timeline 先保存；Prefab 两遍绑定；重新加载后以 `GetReferenceValue` 验证目标组件。 |
| Prop Clip 的场景目标为 `None` | 绑定到源 Prefab或保存前临时实例。 | 用主 Prefab `Prop` 容器下的实例重写 `sceneTarget` 引用。 |
| FX Track 的 `Source Game Object` 丢失 | Control Clip 仍按 Prefab 实例化语义工作，或没有回填 Parent Object。 | `prefabGameObject = null`，`sourceGameObject` 绑定主 Prefab `Fx` 子实例。 |
| 只播放相机位置旋转，FOV 不播放 | 镜头 Prefab 移除了 Camera，但动画曲线仍指向 Camera FOV。 | 把曲线转换为 `CinemachineVirtualCamera.m_Lens.FieldOfView`，生成独立 FOV Track。 |
| 镜头产物冗余且互相抢曲线 | 同时生成 Animator Controller、镜头子 Timeline、Camera 和同步脚本。 | 只保留 `.anim` + 纯层级镜头 Prefab；动画全部由主 Timeline 驱动。 |
| `Battle_CameraPos` 被工具自动放进 Timeline | 把兜底相机存在与自动 Shot 组装混成一件事。 | 工具只保证 Prefab 节点存在；Shot 由美术手动组装。 |
| Odin 界面出现空 `Settings` 区和窄列 | `_settings` 作为嵌套对象绘制，列表又包装复杂输入类型。 | `[InlineProperty] + [HideLabel]` 平铺；五类输入直接使用资源/字符串列表。 |
| 美术因命名不合规无法生成 | 把源资产命名规范设成强制阻断。 | 不检查源命名；输出仍按规则生成；只对无效资源和重复输出阻断。 |
| 只更新一个模块却重建全部结构 | Generation Scope 只有“全部/只生成”，没有独立 Assembly。 | 新增四个模块 Assembly Scope，并为 Timeline/Prefab 实现局部替换。 |
| 单模块组装被其它模块数量不一致拦截 | `ValidateGeneratedAssets` 对所有模块统一自检。 | 自检按 `validateCameras/Sounds/Props/Fx` 作用域分支执行。 |
| 已有 Timeline/Prefab 再生成产生新文件或丢 GUID | 创建逻辑没有优先加载原路径。 | `LoadAssetAtPath` / `LoadPrefabContents` 原位更新，路径不变时保留 `.meta`。 |

这次迭代最重要的经验是：`TimelineAsset` 上“有 Clip”与主 Prefab 的 Director“引用已落盘”是两层状态。只看 Timeline Inspector、内存对象或第一次 `SaveAsPrefabAsset` 都不足以证明绑定有效。

## 6. UI 与工作流经验

- 面向美术的列表直接使用 ObjectField；不显示动画复制、Controller、子 Timeline 等内部中间输入。
- 主 Timeline 与主 Prefab 生成位置合并成一个 ObjectField，子目录由工具推导。
- 按“只生成｜对应组装”两列排列四个模块，语义与执行 Scope 一一对应。
- 多动作、多镜头依赖拖拽排序，不额外增加序号输入；空行跳过，不中断生成。
- 高级设置只保留“生成主 Prefab、覆盖已有资源、默认镜头 FOV”，默认折叠。
- 错误提示直接写中文原因；异常日志仍保留堆栈，但结果摘要不能只让美术读堆栈。
- 已有资源默认在原路径更新；关闭覆盖时先展示目标摘要并确认。

## 7. 当前风险与验证边界

### 7.1 已有验证证据

- 多轮执行 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:q "-clp:ErrorsOnly;Summary"`，最近结果为 `0 errors / 470 warnings`；Warning 是工程现有总量，未作为本工具零 Warning 结论。
- `git diff --check` 对工具目录通过，仅有仓库行尾转换提示。
- 静态检查确认 UI 已包含四组“只生成/组装”按钮，`检查输入` 已移除。
- 静态检查确认镜头不再生成 Controller/子 Timeline，`Battle_CameraPos` 默认 FOV 常量为 `50`。
- 生成器内置自检覆盖镜头/FOV、主轨结构、Clip 长度、Prefab 层级、Director Binding 和 ExposedReference。

### 7.2 未完成或需要继续复验

- 当前最终版本仍需要在 Unity Editor 内分别点击 `All`、四个单模块生成、四个单模块组装和 `Assembly`。
- 需要保存、关闭并重新打开主 Prefab，确认 Shot、Prop、FX 与 FOV Track 绑定仍存在。
- 需要用实际 3ds Max 导出的镜头 FBX 对拍 Transform、FOV、切镜时刻和画幅表现。
- 需要验证空列表、重复镜头编号、已有旧 Controller/子 Timeline、只读资产和异常中断。
- 完整组装当前会重建工具根轨；已明确保留非工具 Cinemachine Shot，但任意人工根轨、Marker 和其它手工内容是否保留需要单独确认，不能用“模块组装安全”替代该验证。
- 单模块组装在主 Timeline/Prefab 不存在或结构不完整时会回退到完整创建，写入范围会扩大。
- 对 `m_PlayableAsset`、`m_SceneBindings` 的直接序列化访问依赖当前 Unity 版本，升级 Unity/Timeline 后需重做落盘验证。

## 8. 维护检查单

- [ ] 修改前已读取工具 Art/Tech README、主 Timeline/Prefab 和本 Profile。
- [ ] 新字段、按钮和 `GenerationScope` 在 UI、校验、执行、覆盖摘要和自检中同步。
- [ ] 多动作和多镜头仍按列表顺序组装，空项计数不会造成编号错位。
- [ ] 源命名不作为强制门槛；输出冲突、资源类型和目录有效性仍在写入前检查。
- [ ] 镜头输出仍只有动画与纯层级 Prefab；没有恢复 Controller、子 Timeline、Camera 或同步脚本。
- [ ] FOV 曲线仍转换到 Cinemachine Lens，FOV Track 与 Shot 时间一致并绑定正确 Animator。
- [ ] `Ultimate_Camera`、`Prop Track`、`Sound Track`、`FX Track` 名称和顺序未被无意改变。
- [ ] `Battle_CameraPos` 保持原点、FOV `50`，不自动生成 Shot。
- [ ] Prop/FX/Shot 的 ExposedReference 指向主 Prefab 内实例，不指向 Project 资源。
- [ ] Prefab 保存后执行持久对象二次绑定和回读；不以第一次内存绑定作为成功证据。
- [ ] 单模块组装只改目标模块；回退完整创建时有明确日志和风险说明。
- [ ] 旧资源在原路径更新，`.meta`/GUID 未因 delete-then-create 改变。
- [ ] 完成 Editor 编译、实际按钮、资源重开、Timeline 播放、Seek/Stop 与代表性 FOV 视觉验证，并分别记录未完成项。
