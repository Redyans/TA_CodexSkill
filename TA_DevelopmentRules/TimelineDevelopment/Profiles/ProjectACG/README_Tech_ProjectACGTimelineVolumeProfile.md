---
name: projectacg-timeline-volume-profile
description: ProjectACG 当前工程的 Timeline URP Volume 定制 Profile。记录版本、代码地图、多效果与单效果 Clip、本地 Profile、Layer、场景隔离、菜单、性能和验证边界，不可直接迁移到其他项目。
---

# ProjectACG Timeline Volume Profile

## 1. 适用范围与迁移边界

本 Profile 仅适用于 `D:\work2025U3D\Valkyria\ProjectACG\Client` 当前实现。跨项目强制规则以 [Timeline CORE](../../README_Tech_TimelineDevelopmentRules.md) 为准，通用后处理模式以 [Timeline × URP Volume 后处理规则](../../references/timeline-volume-post-processing.md) 为准；本文件只记录 ProjectACG 的版本、路径、类型、菜单、当前算法、已知限制和验证状态。

迁移到其他项目时必须删除或重建本 Profile。不得把 `PostProcessTimelineTrack`、`PostProcessTimelineClip`、当前 AOT/Editor 目录、17 个生成效果、Timeline 1.7.7 菜单内部实现或当前组件级 Profile 混合算法当成跨项目统一规范。其他项目可以统一采用 CORE 中的职责、命名风格、资产安全、恢复、混合分类和验证门槛，但必须重新建立自己的版本与代码地图。

## 2. 当前版本事实

| 项目 | 当前值 | Source of Truth |
| --- | --- | --- |
| Unity | `2022.3.62f3` | `ProjectSettings/ProjectVersion.txt` |
| URP | `14.0.12` | `Packages/manifest.json`、`Packages/packages-lock.json` |
| Core RP | `14.0.12` 文件包 | `Packages/packages-lock.json` |
| Timeline | `1.7.7` | `Packages/manifest.json`、`Packages/packages-lock.json` |

版本升级后先重新检查本地包 API、Timeline 菜单构建和 `Volume.profile` 行为，再修改生成器或运行时。Profile 中的版本事实过期时修订本文件，不反向修改通用 CORE。

## 3. 当前代码地图

| 职责 | 当前文件 |
| --- | --- |
| Global Volume Track、绑定、Layer 入口和隔离开关 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/PostProcessTimelineTrack.cs` |
| 多效果 Clip 与本地 Profile 兼容字段 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/PostProcessTimelineClip.cs` |
| Clip 基类、单层/多层 Mixer、Baseline、隔离和临时组件 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/TimelineVolumeRuntime.cs` |
| 17 个标准 URP 单效果 Clip | `Assets/GameScripts/AOT/GameArt/TimelineTrack/VolumeGenerated/` |
| Track/Clip 创建时初始化 | `Assets/GameScripts/Editor/TimelineTrack/PostProcessTimelineTrackEditor.cs` |
| 多效果和单效果 Inspector | `Assets/GameScripts/Editor/TimelineTrack/TimelineVolumeClipAssetEditor.cs` |
| Clip 本地 Profile 创建、导入、拆分、恢复、保存和清理 | `Assets/GameScripts/Editor/TimelineTrack/TimelineClipVolumeProfileUtility.cs` |
| 单效果 Clip 生成器 | `Assets/GameScripts/Editor/TimelineTrack/VolumeTrackGenerator/TimelineVolumeTrackGenerator.cs` |

代码、序列化资产或包版本与本 Profile 不一致时，以当前代码、资产和本地 PackageCache 为准，并将差异作为 Profile 修订候选。

## 4. PRJ-TML-VOL-01｜Track、Clip 与菜单结构

当前统一入口为 `Global Volume Track`：

- 类型：`PostProcessTimelineTrack : TrackAsset, ILayerable`；
- 绑定：`GameObject`，运行时从绑定对象解析 `Volume`；
- 支持的 Clip 基类：`TimelineVolumeClipAsset`；
- 根 Track 保存 `隔离场景 Volume`；
- 没有子 Layer 时保持单 Track Graph；添加子 Layer 后才创建 `GlobalVolumeLayerMixer`。

Timeline 右键 Add 菜单采用以下项目约定：

```text
Add Post Process (Multi Effect)

Add Single Effect
├─ Bloom
├─ Channel Mixer
├─ Chromatic Aberration
├─ Color Adjustments
├─ Color Curves
├─ Color Lookup
├─ Depth Of Field
├─ Film Grain
├─ Lens Distortion
├─ Lift Gamma Gain
├─ Motion Blur
├─ Panini Projection
├─ Shadows Midtones Highlights
├─ Split Toning
├─ Tonemapping
├─ Vignette
└─ White Balance
```

当前 Timeline 1.7.7 已从本地源码确认：`TimelineContextMenu` 使用 `TypeUtility.GetDisplayName` 构造 `Add {0}`，`ActionManager.BuildMenu` 将字符串交给 `GenericMenu`，所以 `[DisplayName("Single Effect/Bloom")]` 可形成子菜单；`TrackAsset.CreateClipOfType` 使用类型名创建 PlayableAsset 和 Clip 默认名，因此菜单中的 `/` 不会写入新 Clip 标题。

这里只改 `DisplayName`，不改类型名、脚本、GUID 或序列化字段。生成器模板和全部生成物必须同步；升级 Timeline 后重新核对上述本地源码，禁止修改 `PackageCache`。

## 5. PRJ-TML-VOL-02｜多效果 Clip 本地 Profile

`Post Process (Multi Effect)` 当前不长期编辑外部 Profile，而是让每个 Clip 在 `.playable` 内拥有独立 `VolumeProfile` 子资产：

1. 新建 Clip 延迟创建空本地 Profile；
2. `从 Profile 导入`会深拷贝可加载的 VolumeComponent 和参数，源 Profile 后续不受 Clip 修改影响；
3. 本地 Profile 与其组件都作为 `.playable` 子资产保存，提交 `.playable` 即可带走配置；
4. 复制 Clip 时 Unity 会先复制引用，Utility 通过持久 `GUID + local fileID` 判断共享并拆分副本；
5. 导入期间没有持久 ID 时不推断共享，避免反复克隆；
6. 引用损坏时优先恢复已有同资产 Profile，再创建新对象；
7. 当前数据版本为 `profileDataVersion = 1`，旧 `legacyProfile/privateProfile` 字段只保留迁移兼容。

普通参数编辑只标记 Profile 和组件 Dirty。严禁在原生 `VolumeProfileEditor` 的每次 GUI 变化中同步调用 `SaveAssetIfDirty` 和 `TimelineEditor.Refresh`；此前会形成“保存、重导入、Inspector 重建、再保存”的循环，表现为鼠标持续转圈和 `.playable` 重复增长。

创建、导入、拆分和孤立清理属于结构变更，当前使用 `EditorApplication.delayCall` 延迟保存并按资产路径合并请求。孤立清理只跟随结构变更或显式维护，不在每次 Inspector `OnEnable`/Repaint 执行。

## 6. PRJ-TML-VOL-03｜Runtime 混合与恢复

当前 Runtime 最终写入根 Track 绑定 Volume 的 `volume.profile` 实例：

- 每个 Mixer 首次绑定时记录 `Volume` 与 Profile；换绑或 Profile 变化先释放旧状态；
- 每种首次需要的 VolumeComponent 保存 Baseline；每帧先恢复 Baseline，再应用当前输入；
- 绑定 Profile 缺少目标组件时通过当前 Core RP 公开 `VolumeProfile.Add(Type, bool)` 临时添加；
- Stop、换绑或 Graph 销毁时恢复原组件，并删除 Timeline 自己添加的组件；
- Edit Mode 临时对象使用 `DestroyImmediate`，Play Mode 使用 `Destroy`；
- `WeightEpsilon` 当前为 `0.0001f`。

同一 Layer 内当前优先级为：

```text
多效果 Clip Profile
→ 同类型 Single Effect Clip 接管该 VolumeComponent
```

不同组件可以组合，例如多效果 Clip 提供 Bloom/Tonemapping，单效果 Clip 提供 Vignette。若多效果和单效果同时提供 Bloom，只执行单效果 Bloom 的生成式 Blend，不是在多效果 Bloom 结果上继续叠加。作者不要把两者的同类型淡入当成平滑组合；需要该能力时先扩展明确合同。

生成式单效果 Clip 对 Float、Int、Color、Vector 使用带 Baseline 权重的连续混合；Bool、Enum、Texture、Curve 等离散参数选最高权重输入，同权重使用稳定输入顺序。只有 `overrideXxx` 开启的参数参与。

### 当前多效果交叠限制

多个多效果 Clip 交叠时，当前实现先按组件存在性统计总权重，再调用 `VolumeComponent.Override`。这不是严格的逐参数权重统计：一个 Profile 含有 Bloom 组件但未 Override 某参数时，仍可能影响该组件的累计权重。当前作者约束是同类型多效果 Clip 尽量保持一致的 Override 集合；若镜头依赖不同参数集合交叠，必须先补逐参数测试或改进算法。

## 7. PRJ-TML-VOL-04｜Global Volume Layer

当前根 Track 是最低层，Timeline 编译的后置子 Layer 优先级更高：

- Layer Mixer 每帧先收集根和子 Layer 的需要类型；
- 全局 Baseline 只恢复一次；
- 每层开始时用当前下层结果建立可复用 Layer Baseline/Target；
- 本层只把 `overrideState = true` 的参数提交回最终目标；
- 未覆盖参数保留下层结果；
- 子 Mixer 不独立 Restore/写入同一个 Volume Profile；
- 场景隔离只由根 Track申请和释放，子 Layer 不显示独立开关。

Layer 是 Timeline 内部的覆盖顺序，不是第二套 URP Volume Stack。新增 Layer 只用于明确的高低优先级，不要按效果类型机械拆层。

## 8. PRJ-TML-VOL-05｜场景 Volume 共存与隔离

`隔离场景 Volume` 默认关闭：

- 场景 Bloom 与 Timeline Vignette 可以同时生效；
- 场景和 Timeline 都覆盖 Bloom 时，最终仍受 URP Volume Layer、Priority、Weight、Global/Local 和相机 Mask 影响；
- Timeline 内部同类型接管不等于自动压过其它场景 Volume。

开关开启后，`TimelineVolumeIsolation` 使用 Owner 引用计数临时禁用除当前 Timeline Owner Volume 外的其它场景 Volume，最后一个 Owner 释放后恢复原 `enabled`。多个 Timeline Volume 同时成为 Owner 时都会保持启用。

当前扫描只在 Acquire/Release 导致的 `Refresh` 中调用 `FindObjectsOfType<Volume>(true)`，不做逐帧扫描。因此隔离期间新动态创建的 Volume 不保证立即被禁用；如果项目出现该需求，应增加事件驱动或低频显式刷新，不能把全场景扫描直接放进每帧热路径。

## 9. PRJ-TML-VOL-06｜Inspector

### 多效果 Clip

- 编辑对象是真实的 Clip 本地 VolumeProfile，使用公开 `Editor.CreateEditor(profile)` 嵌入原生 Profile Inspector；
- 支持组件折叠、Active、Override、ALL/NONE、Add Override 和 URP 专用 UI；
- 导入源来自 HDRP 或缺失插件时，Inspector 会提示无法加载的组件并跳过；
- Track 锁定、`.playable` 只读或资源无效时保持禁用；
- Inspector 销毁时释放嵌套 Editor。

### 单效果 Clip

- 直接编辑 Clip 的 `SerializedProperty`，不创建代理 VolumeComponent 作为数据源；
- Override 复选框保持可点，只禁用右侧 value；
- `ALL/NONE` 只切换当前可见 Override；
- 当前为 Bloom、Depth Of Field、Film Grain、Motion Blur、Channel Mixer、Color Curves、Lift Gamma Gain、Shadows Midtones Highlights、Tonemapping 等效果提供专用布局，其余使用通用绘制；
- 元数据 VolumeComponent 只在 Editor `OnEnable` 创建一次，用于 Range、Tooltip、HDR 和 AdditionalProperty，不在 Repaint 反复创建。

## 10. PRJ-TML-VOL-07｜性能与使用规模

- 每个 Layer Mixer 每帧遍历该层全部 Playable 输入读取权重；Clip 总数和 Layer 数是 Runtime CPU 基础成本。
- 多效果 Clip 还会遍历本地 Profile 组件；生成式单效果 Clip 当前常按“参数数 × 同类型有效输入数”执行强类型循环。
- Dictionary、List、HashSet 和临时 VolumeComponent 在 Mixer 生命周期内复用，运行时不使用 AssetDatabase 或反射；容量稳定后应接近零 GC。
- 状态字典会保留本 Graph 已遇到的组件到释放阶段；长 Timeline 使用很多分散类型时，每帧恢复成本可能随累计类型增加。
- 每个多效果 Clip 都保存一个 Profile 和若干组件子资产，`.playable` 体积随 Clip/组件数增长；多个效果同起止时间时优先合并为一个多效果 Clip。
- Timeline Clip 不会为每个 Clip 重复执行一套 URP 后处理。GPU 大头仍由最终 Stack 实际启用的 Bloom、DOF、Motion Blur 等效果决定。

大量投入生产前建立 20/100/500 Clip 三档样本，记录 Timeline 窗口响应、`.playable` 大小、导入/打开时间、Runtime CPU/GC 和目标平台 GPU。不要只用 C# 编译或单个镜头推断大规模性能。

## 11. 当前验证状态与剩余风险

截至 2026-07-28 的静态验证：

- `dotnet build AOT.csproj -nologo --no-restore -p:BuildProjectReferences=false`：`0 errors`，工程已有 13 个警告；
- `dotnet build Assembly-CSharp-Editor.csproj -nologo --no-restore -p:BuildProjectReferences=false`：`0 errors`，工程其它路径已有 46 个警告；
- 17 个标准生成 Clip 均使用 `Single Effect/...` DisplayName；
- 菜单路径与 Clip 默认名已从 Timeline 1.7.7 本地包源码静态确认；
- 相关 C# 差异通过 `git diff --check`。

尚未完成的 Unity 行为验证：

- Timeline 窗口实际右键菜单顺序、创建和 Clip 标题；
- 多效果/单效果同类型交叠的视觉连续性；
- 根 Track、多个子 Layer、Seek/Stop/重新播放；
- 场景 Volume 隔离、多个 Director 和动态新建 Volume；
- 大规模 Clip 的 Timeline 窗口、CPU/GC、资产体积与目标平台 GPU。

未完成上述 Unity 验证前，只能声明“静态编译和代码合同通过”，不能声明播放、视觉和性能已经完整验证。

## 12. ProjectACG 使用检查

- [ ] 绑定对象包含 `Volume`，相机 Volume Layer Mask 能看到该层，Global/Local、Priority 和 Weight 符合镜头合同。
- [ ] 同起止时间的多个效果使用 `Post Process (Multi Effect)`；独立时序或曲线使用 `Single Effect/<Effect>`。
- [ ] 从外部 Profile 导入后源文件未变化，复制 Clip 后两个本地 Profile 已拆分。
- [ ] 反复选择、保存、Undo/Redo 和 Domain Reload 不会持续转圈或增加重复 Profile 子资产。
- [ ] 绑定 Profile 缺少目标效果时，单效果 Clip 仍可临时补齐并在 Stop 后清理。
- [ ] 同 Layer、跨 Layer、多效果/单效果同类型优先级符合本 Profile。
- [ ] 隔离关闭时场景 Volume 正常叠加；隔离开启时其它 Volume 恢复原 `enabled`。
- [ ] Unity Console、Timeline 播放、Scene/Game 视图、Frame Debugger/Profiler 和目标平台性能均已验证。
