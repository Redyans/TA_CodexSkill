---
name: timeline-volume-post-processing
description: Unity Timeline 驱动 URP Volume 后处理的专项规则，覆盖多效果与单效果 Clip、本地 Profile 子资产、混合、Layer、场景隔离、Inspector、菜单生成、性能、生命周期和验证。
---

# Timeline × URP Volume 后处理开发规则与复盘

本文补充 [../README_Tech_TimelineDevelopmentRules.md](../README_Tech_TimelineDevelopmentRules.md) 的 Timeline CORE，分类为跨项目通用 REFERENCE。它总结 Timeline 后处理扩展中反复出现的设计、编辑器和生命周期问题，供后续新功能直接复用。具体工程的目录、包版本、自定义 VolumeComponent、菜单实现和生成物清单必须记录在项目 Profile 或功能 README；ProjectACG 当前实现见 [Timeline Volume Profile](../Profiles/ProjectACG/README_Tech_ProjectACGTimelineVolumeProfile.md)，不得把其中事实升级为跨项目规则。

| 层级 | 应记录的内容 | 不应记录的内容 |
| --- | --- | --- |
| CORE | 跨项目稳定的职责边界、资产安全、生命周期、恢复和验证门槛 | 项目路径、包版本、类名、菜单文案、效果清单 |
| REFERENCE（本文） | 可复用的 Volume 架构模式、混合合同、Inspector 原则、失败模式和验证方法 | 某个项目当前已经实现到哪一步、具体生成物数量或临时作者约束 |
| Project PROFILE | 当前项目的版本、代码地图、类型与菜单、效果覆盖、算法偏差、已知限制和验证状态 | 未经验证就宣称为所有项目必须采用的规则 |

其他项目复用本文时，应保留 CORE/REFERENCE 的行为合同和验证方式，同时新建自己的 Profile；不要复制 ProjectACG Profile 后只替换项目名。

## 1. 先确定产品合同

实现前必须先回答以下问题，不能从“Timeline 能播放一个 Profile”推断完整需求：

1. 艺术家需要整份后处理预设切换，还是需要在 Clip 上逐参数调整和打关键帧？
2. Timeline 后处理与场景 Volume 是共同参与 Volume Stack，还是播放期间完全隔离？
3. 同类型 Clip 交叠时如何混合，离散参数如何决胜？
4. Timeline 结束、Seek、Stop、绑定丢失或 Domain Reload 后恢复到什么状态？
5. Inspector 修改的是 Clip 自己的本地 Profile，还是外部共享的 VolumeProfile 资产？
6. 一个 Timeline 中需要很多效果 Track，还是一个 Global Volume Track 下同时提供多效果与单效果 Clip？

未明确这些合同前，不要先生成大量 Track/Clip 或复用原生 Inspector；否则最容易得到“能创建但不可调”“能显示但运行时不恢复”或“场景和 Timeline 意外叠加”的半成品。

## 2. 推荐的终局结构

### 2.1 一条 Global Volume Track，效果作为 Clip

推荐使用一个绑定到 `GameObject`/`Volume` 的 Global Volume Track，在同一轨道中添加多效果 Clip 或具体单效果 Clip：

```text
Global Volume Track
├─ Multi-effect Profile Clip
├─ Single-effect Clip（效果 A）
├─ Single-effect Clip（效果 B）
└─ Single-effect Clip（效果 C）
```

这样 Timeline 窗口不会因为每个后处理类型都生成独立 Track 而失控。Track 只负责绑定、隔离开关和 Mixer；具体效果类型、参数与混合由 Clip 负责。

只有当不同效果必须拥有不同绑定、不同生命周期或明确独立的 Layer/Mixer 时，才拆分 Track。不要仅因为效果类型不同就机械生成一条 Track。

### 2.2 多效果 Clip 与单效果 Clip 是两种能力

| 类型 | 数据 | 用途 | 能否逐参数做 Timeline 动画 |
| --- | --- | --- | --- |
| 多效果 Clip | Clip 本地 `VolumeProfile` 子资产；外部 Profile 只作为导入源 | 在一个 Clip 中集中配置多个效果，并按 Clip 权重混合 | 可直接编辑 Override，但 Profile 内嵌参数通常不作为 Clip 曲线字段暴露 |
| 单效果 Clip | `overrideXxx + value` | 在 Timeline 面板调某一类效果的具体参数，并支持交叠混合 | 可以，前提是字段真实序列化在 Clip 上 |

默认推荐多效果 Clip，适合多个效果拥有相同起止时间的镜头配置；需要独立时序、Animation Curve 或按参数精确交叠时，再使用单效果 Clip。不得用“Profile 可以播放”替代“单效果参数可以编辑/动画”。

外部 Profile 的推荐语义是“导入并深拷贝到 Clip”，而不是长期共享引用。导入后修改 Clip 本地参数不得污染源 Profile；复制 Timeline Clip 后也必须拆分本地 Profile，避免两个 Clip 后续互相修改。

### 2.3 同类型单效果 Clip 优先于多效果 Clip

当多效果 Clip 与单效果 Clip 同时提供同一种 `VolumeComponent` 时，推荐由单效果 Clip 接管该类型，多效果 Clip 不再重复写同类型组件；不同组件仍可组合：

```text
Multi Effect Clip：Bloom + Tonemapping
Vignette 单效果 Clip：Vignette
结果：Bloom + Tonemapping + Vignette
```

```text
Multi Effect Clip：Bloom
Bloom 单效果 Clip：Bloom
结果：Bloom 单效果 Clip 接管 Bloom，避免同一类型被两套来源重复混合
```

优先级必须写入 Mixer，而不是依赖输入遍历顺序。若项目采用“接管”语义，它不是把单效果继续叠加在多效果结果上；同类型需要平滑地从多效果 Clip 过渡到单效果 Clip 时，应先扩展明确的组合混合合同，不要依赖淡入阶段的偶然结果。

## 3. 运行时状态模型

Mixer 应明确执行以下顺序：

```text
Resolve Binding
→ Collect Current Inputs
→ Ensure Required Components
→ Restore Baseline
→ Apply Multi Effect Profiles
→ Apply Direct Component Clips
→ Release / Restore
```

### 3.1 Baseline 必须先快照、每帧先恢复

首次遇到某种 VolumeComponent 时，保存绑定 Volume 中该组件的基线快照。每帧应用 Timeline 输入前先恢复基线，再根据当前有效输入计算结果。这样可以避免：

- 上一帧 Clip 的值泄漏到下一帧；
- Clip 空隙继续保持旧值；
- Seek 到任意时间得到依赖播放历史的结果；
- 重叠 Clip 权重下降后无法回到原始状态。

恢复不仅要还原参数值，也要还原 `active` 与每个 `VolumeParameter.overrideState`。

### 3.2 缺失组件可以运行时临时补齐

绑定的 Volume Profile 不要求提前包含所有效果。某个 Vignette/Bloom Clip 生效时，如果目标 Profile 中没有该组件，可以通过当前 SRP 版本的公开 Profile API 临时添加组件；Timeline 结束后只删除自己添加的组件。

必须满足：

- 不永久修改共享 Profile 资产；
- 区分“原本存在”和“Timeline 添加”；
- 保存新组件的默认基线；
- Release 时恢复原组件并销毁临时对象；
- Edit Mode 使用 `DestroyImmediate`，Play Mode 使用 `Destroy`。

优先操作 `Volume.profile` 的独立运行时实例，而不是直接改 `sharedProfile` 资产。具体 Unity/URP 版本的 `profile` 实例化语义必须从本地包源码确认。

### 3.3 所有退出路径都要释放

至少覆盖：

- `playerData`/绑定为空；
- 绑定的 Volume 或 Profile 发生变化；
- PlayableGraph 销毁；
- Timeline Stop、重新播放和重新绑定；
- Inspector 预览与 Edit Mode Evaluate。

清理只处理当前 Mixer 拥有的状态，不得删除用户原有组件或启用状态。

## 4. 混合规则

### 4.1 连续参数

Float、Int、Color、Vector 等连续参数按有效 Clip 权重混合，并保留基线权重：

```text
baseWeight = max(0, 1 - totalClipWeight)
result = (baseline * baseWeight + Σ(clipValue * clipWeight))
         / (baseWeight + totalClipWeight)
```

当总 Clip 权重达到 1 时，不再混入基线；淡入、淡出期间则自然与基线过渡。Int 最终按明确规则取整。

只统计已勾选对应 Override 的 Clip。Clip 没有覆盖某个参数时，该参数继续使用基线，不要用 Clip 字段的默认零值覆盖。

### 4.2 离散参数

Bool、Enum、Texture、Curve、模式和其他不可线性插值的数据，选择有效权重最高的 Clip；同权重使用稳定的输入顺序或显式 Order 决胜。不得对枚举做数学平均，也不得依赖 Dictionary 遍历顺序。

### 4.3 多个多效果 Clip

多个多效果 Clip 交叠时，按 Timeline 输入权重逐组件混合。只有本地 Profile 中存在、处于 Active 且参数 Override 已开启的值才应参与。Profile 缺少某组件时，不应把该组件的默认值加入权重。

通用混合合同要求按“参数是否 Override”统计权重，而不是只按“Profile 是否含有该组件”统计。否则一个 Clip 虽然包含 Bloom 组件但没有覆盖 Threshold，也可能错误稀释另一个 Clip 的 Threshold 权重。采用组件级累计权重不符合本文的通用合同；项目若暂未实现参数级统计，必须在自己的 Profile 中记录算法偏差、作者约束、影响范围和交叠测试，不能把限制反向写成通用规则。

### 4.4 Global Volume Layer

需要高低优先级覆盖时，可以让根 `Global Volume Track` 实现 `ILayerable`，子 Track 作为 Volume Layer，但绑定、场景隔离和最终写入仍由根 Track 统一拥有：

- 没有子 Layer 时保留普通单 Track Graph，避免无意义的 Layer Mixer；
- 根 Track 是最低层，子 Layer 按 Timeline 中的稳定顺序依次应用，后置 Layer 优先级更高；
- 每层开始时以当前下层结果作为 Baseline；
- 本层只提交自己真正 Override 的参数，未覆盖参数继续保留下层结果；
- 同一层内先执行多效果 Clip，再由同类型单效果 Clip 按显式规则接管；
- 场景 Volume 隔离开关只显示和保存在根 Track，子 Layer 不重复申请或显示一份容易误导的开关；
- Layer Mixer 销毁或换绑时统一恢复根绑定 Volume，子 Mixer 不能各自 Restore 同一 Profile。

不要把 Layer 当作另一套 Volume Stack。它是 Timeline 内部的确定性覆盖顺序，最终仍只写入一个绑定 Volume Profile。

## 5. 场景 Volume 共存与完全接管

必须把两种模式作为显式产品开关，而不是隐式推断。

### 5.1 正常叠加模式

隔离关闭时，场景 Volume 与 Timeline 绑定 Volume 都参与 URP Volume Stack：

- 场景 Bloom + Timeline Vignette：两个效果共同生效；
- 场景 Bloom + Timeline Bloom：根据 Volume Priority、Weight 和 Override 状态混合；
- 要让 Timeline 同类型参数在满权重时覆盖场景结果，应明确设置 Timeline Volume 更高 Priority 与 `Weight = 1`。

“同类型 Clip 内部覆盖基线”不等于“自动覆盖其他场景 Volume”。最终结果仍受 URP Stack 的 Layer、Priority、Weight、Global/Local 范围和摄像机 Volume Layer Mask 影响。

### 5.2 场景隔离模式

隔离开启时，Timeline 播放期间临时禁用除绑定 Volume 外的其他场景 Volume，使视觉完全由 Timeline Volume 决定。实现必须：

- 保存每个被禁用 Volume 原来的 `enabled` 状态；
- 只排除当前 Timeline 绑定的 Volume；
- 多个 Timeline/Mixer 同时请求隔离时使用引用计数或 Owner 集合；
- 最后一个 Owner 释放后才恢复；
- 新出现或重新启用的场景 Volume 需要明确刷新策略；
- Stop、解绑、图销毁和异常路径都能恢复。

隔离开启后，如果 Timeline Profile 只有压暗而没有 Bloom，场景 Bloom 应消失；这不是丢效果，而是“完全接管”合同的结果。

隔离扫描不能放在每帧热路径。推荐只在第一个 Owner Acquire、Owner 变化或显式刷新时扫描场景 Volume，并保存原 `enabled`。这意味着播放中动态创建的新 Volume 是否立即被隔离必须作为单独合同：可以监听场景/对象变化或提供低频刷新，但不能无条件每帧 `FindObjectsOfType`。多个 Timeline 绑定 Volume 同时成为 Owner 时，它们都应保持启用；引用计数归零后再恢复其他 Volume。

## 6. Inspector 规则

### 6.1 单效果 Clip：直接编辑 Clip SerializedProperty

单效果 Clip 保存的是：

```csharp
public bool overrideIntensity;
public float intensity;
```

而 URP 原生 VolumeComponent 保存的是嵌套结构：

```text
intensity.overrideState
intensity.value
```

两者不是同一个 SerializedObject 数据模型。不要创建临时 VolumeComponent，再反射调用 `VolumeComponentEditor.Init` 或 `OnInternalInspectorGUI`，然后把值拷回 Clip。这种代理方案依赖 SRP 内部 API、父 Inspector、Active 状态和 GUI Scope，容易出现整片参数灰色、Override 不可点、Undo/多选失效和版本升级破坏。

正确做法：

1. Inspector 直接使用 Clip 的 `serializedObject`；
2. Override 与 value 分别查找 `SerializedProperty`；
3. Override 复选框不放在自己的 DisabledScope 中；
4. 只有右侧 value 在 Override 关闭时变灰；
5. 使用明确 Rect 的 `EditorGUI.PropertyField` 绘制 Override，避免 Timeline 嵌套布局导致点击区域失效；
6. `ApplyModifiedProperties` 负责 Undo 和脏标记；
7. 从真实 VolumeParameter 实例只读取范围、Tooltip、HDR/Alpha、AdditionalProperty 等元数据，不让它成为被编辑的数据源。

通用类型可以统一绘制 Slider、IntSlider、Color、Enum、Texture、Vector；Bloom、DOF、Tonemapping、Channel Mixer、Color Curves、Trackball 等有强语义结构的效果，应复制必要的布局逻辑或编写 Timeline 专用 Drawer，而不是把所有字段平铺成一大片。

### 6.2 多效果 Clip：嵌入 Clip 本地 Profile Inspector

多效果 Clip 可以在 `.playable` 内保存一个真实的 `VolumeProfile` 子资产，再通过公开 `Editor.CreateEditor(profile)` 嵌入原生 Profile Inspector。这样可以直接获得组件折叠、Active、Override、ALL/NONE、Add Override 和效果专用 UI，同时不会把单效果 Clip 强行伪装成 `VolumeComponent`。

推荐的资产语义：

- 新建多效果 Clip 时创建空的本地 Profile；
- “从 Profile 导入”是深拷贝组件和参数到本地 Profile，源 Profile 只读且不受后续修改影响；
- 复制 Clip 后，新旧 Clip 初始值相同，但必须各自拥有独立 Profile；
- 本地 Profile、VolumeComponent 都作为 `.playable` 子资产保存，提交 `.playable` 即可带走数据；
- Track 绑定 Volume 只是运行时写入目标，不作为多个 Clip 的可编辑数据容器；否则不同 Clip 无法保存不同参数；
- Profile 切换、Inspector 关闭和 Domain Reload 时销毁嵌套 Editor。

只有产品明确要求共享资产联动时，才允许 Clip 长期引用外部 Profile，并必须在 Inspector 中提示“修改会影响所有引用者”。不要让“导入预设”和“编辑共享源资产”使用同一个含糊入口。

### 6.3 Clip 本地 Profile 的创建、复制与保存

Clip 本地 Profile 属于结构化子资产，处理不当会造成 `.playable` 无限保存、反复导入或积累几百个孤立 Profile。必须遵守：

1. 创建 Profile、导入 Profile、拆分重复 Clip 引用属于结构变化；使用 `Undo.RegisterCreatedObjectUndo`、`AssetDatabase.AddObjectToAsset` 和稳定子资产命名。
2. 复制 Timeline Clip 时 Unity 会先复制对象引用。保存前按 `GUID + local fileID` 判断是否与另一 Clip 共享；确认共享后只克隆一次。
3. 导入期间对象包装器可能暂时没有持久 ID。此时没有证据说明它被共享，不能反复克隆；等待资产稳定后再判断。
4. 如果 Clip 引用丢失但 `.playable` 中仍有同命名或可恢复的 Profile，先重新认领已有子资产，再考虑新建，避免每次 Inspector 重绘都增加一份 Profile。
5. 结构保存必须退出当前 IMGUI 后通过 `EditorApplication.delayCall` 执行，并按资产路径合并重复请求；保存完成、引用稳定后才能清理孤立 Profile。
6. 普通参数编辑只标记 Profile 和组件 Dirty，交给 Unity 正常保存。严禁在 `VolumeProfileEditor.OnInspectorGUI` 的 `GUI.changed` 路径中每帧调用 `SaveAssetIfDirty`、重新导入 `.playable` 或 `TimelineEditor.Refresh`。
7. 不要在每次 Inspector `OnEnable` 或重绘时执行全量孤立 Profile 清理。清理只跟随确定的结构变更或显式维护命令。

原生 `VolumeProfileEditor` 可能在 Repaint 阶段持续报告 GUI 变化；如果每次变化都同步保存并刷新 Timeline，会形成“保存 → 导入 → Inspector 重建 → 再保存”的无限循环，表现为鼠标持续转圈、Clip 一直读取、`.playable` 体积不断增长。这是编辑器生命周期问题，不是 Volume 参数数量本身导致的运行时性能问题。

### 6.4 Timeline 外层禁用状态

Timeline 可能在嵌套 PlayableAsset Inspector 外层设置 `GUI.enabled = false`。只有在同时确认以下条件后，Timeline 专用 Inspector 才可恢复 GUI：

- `.playable` 资产可写；
- 父 Track 未锁定；
- VCS 状态允许编辑；
- 目标不是只读包资源。

不能无条件强制 `GUI.enabled = true`，否则会绕过 Track 锁定和版本控制。

## 7. 代码生成与程序集

### 7.1 生成器是唯一真相源

项目后处理类型较多时，可以生成单效果 Clip 和混合代码，但生成器必须是唯一真相源：

- 标准 URP VolumeComponent 的生成物放入运行时 AOT 可引用的目录；
- 自定义 VolumeComponent 若位于独立运行时程序集，生成物放在能合法引用其程序集的位置；
- 禁止形成 AOT → HotFix/Editor 的反向依赖；
- 生成类需要稳定类型名、字段名、DisplayName 和 `ClipCaps.Blending`；
- UnityEngine.Object 类型按离散值处理；
- 生成文件标注“重新生成，不手改”；
- Generator 改动后重新生成全部目标并检查 diff，不能只修一个输出文件。

生成前读取当前 Unity、Timeline、Core RP、URP 包版本的本地 API。不要把其他工程或其他 URP 版本的 `VolumeParameter.Interp`、Editor 内部方法和 asmdef 引用关系直接复制过来。

生成式混合代码不应在运行时使用反射。生成阶段可以反射 `VolumeComponent` 的公开字段并写出强类型代码；运行时只读序列化字段、Clip 权重和缓存状态。Playable 泛型约束、Profile 组件创建和 VolumeComponent 混合必须使用目标包公开 API，具体方法签名写入当前项目 Profile，并以本地包源码为准。

### 7.2 用菜单分组收口大量 Clip 类型

统一 Track 支持许多派生 Clip 后，Timeline 默认可能把所有类型平铺在 Add 菜单中。推荐保留两级信息架构：

```text
Add Multi-effect Clip

Add Single-effect Clip
├─ Effect A
├─ Effect B
├─ Effect C
└─ …
```

跨项目只统一“多效果顶级入口、单效果二级入口”的信息架构，不统一具体文案、语言和效果排序。可用 `DisplayName`、公开菜单扩展或项目已有注册表实现，但必须先从目标 Timeline 包源码确认路径分隔、排序、Clip 默认名和可见性规则。

每个项目必须在自己的 Profile 中记录已核对的包版本、源码入口、最终菜单文案和实现方式；升级版本后重新验证。不要修改 `PackageCache`，也不要依赖项目程序集无法访问的 internal 菜单特性。

菜单收口不得为了排序或文案重命名 Clip 类型、脚本、GUID 或序列化字段。若菜单元数据由生成器产出，模板和所有已有生成物必须同步更新，确保下次重新生成不会覆盖分组。

## 8. 常见失败与根因

| 现象 | 根因 | 正确处理 |
| --- | --- | --- |
| 只有 Profile 字段，不能在 Timeline 调单个参数 | Profile 引用不是 Clip 参数模型 | 增加真实序列化的单效果 Clip |
| Timeline 窗口出现大量后处理 Track | 按效果生成 Track，而不是在统一 Track 中放效果 Clip | 一条 Global Volume Track + 多种 Clip |
| 参数全部灰色 | 把原生 VolumeComponentEditor 嵌入代理对象，或 GUI Scope/Active 状态不匹配 | 单效果 Clip 使用直接 SerializedProperty UI |
| ALL 可点，单项 Override 点不了 | 布局式 Toggle 在嵌套 Inspector 中点击区域不可靠 | 为 Override 分配明确 Rect，直接 PropertyField |
| 添加多效果 Clip 后鼠标持续转圈、Inspector 一直刷新 | 在原生 Profile Inspector 的重绘路径同步保存、导入并刷新 Timeline | 参数编辑只标 Dirty；结构保存延迟并按资产路径合并 |
| `.playable` 从几百 KB 增长到数 MB，并出现大量重复 Profile | 每次重绘都创建 Profile，或引用未稳定前重复判断共享/清理 | 先恢复已有子资产；持久 ID 不可用时不克隆；结构稳定后再清理孤立项 |
| 复制 Clip 后修改一个，另一个也变化 | Unity 复制 Clip 时暂时共享同一 Profile 引用 | 检测另一个持久 Clip 的相同 local fileID，延迟深拷贝为独立子资产 |
| 从外部 Profile 导入后源文件也被修改 | Clip 长期编辑共享 Profile，而不是导入副本 | 将源 Profile 深拷贝到 `.playable` 本地 Profile，Inspector 只编辑本地对象 |
| 一个 Track 的 Add 菜单平铺十几个效果 | 所有派生 Clip 都使用顶级菜单入口 | 保留多效果顶级入口和单效果二级入口；同步生成器模板 |
| 单效果 Clip 淡入时同类型多效果结果突然变化 | “单效果接管同类型”被误认为在多效果结果上继续混合 | 避免同类型同时 author，或先实现明确的组合混合合同 |
| Edit Mode 报 `Destroy may not be called from edit mode` | 生命周期未区分播放和编辑模式 | Play Mode `Destroy`，Edit Mode `DestroyImmediate` |
| Timeline 压暗后场景 Bloom 仍存在 | 隔离关闭，两个 Volume 正常共同参与 Stack | 明确这是叠加模式；需要完全接管则开启隔离 |
| Timeline Bloom 没完全覆盖场景 Bloom | Timeline Volume Priority/Weight 或 Override 不满足 | 检查 Layer、Priority、Weight、Global/Local、相机 Mask |
| 绑定 Profile 没有 Vignette 就不生效 | 运行时未补齐所需组件，或 Intensity/Override 为零 | 临时添加缺失组件并验证有效参数 |
| 停止后场景 Volume/参数没恢复 | Release 路径不完整或没有保存基线/原 enabled | 所有退出路径统一 Restore + Release |
| 外部 `dotnet build` 大量 CS0006 | Unity 生成程序集或 Temp/bin 不完整 | 以 Unity Console/Editor.log 的真实编译为准，dotnet 仅作补充 |
| 为证明功能直接改用户 `.playable` | 把验证数据写入用户资产 | 使用临时测试 Timeline，或只读检查指定资产 |

## 9. 性能与大规模使用

### 9.1 Runtime CPU

- 根 Mixer 和每个 Layer Mixer 每帧都要遍历该 Track 的 Playable 输入并读取权重，因此 CPU 基础成本随 Clip 输入总数和 Layer 数增长，即使大部分 Clip 当前权重为零也需要一次判断。
- 进入有效区间后，多效果 Clip 的成本近似为“有效多效果 Clip 数 × 涉及组件数”；生成式单效果 Clip 常见实现会对每个参数遍历同类型有效输入，成本近似为“参数数 × 同类型交叠 Clip 数”。
- Dictionary、List、HashSet 和临时组件应在 Mixer 生命周期内复用；热路径禁止反射、LINQ、`FindObjectsOfType`、AssetDatabase 和逐帧 `Instantiate`。列表容量稳定后应做到近似零 GC。
- 每种首次需要的组件只创建一次 Baseline；Layer 模式可为该组件额外维护并复用 Layer Baseline/Target。不要按 Clip 创建运行时组件副本。
- 绑定 Profile 缺失的组件只在首次需要时临时添加，停止或换绑时移除。结构变化不能每帧执行。
- 若状态字典保留“本图历史上出现过的所有组件”，每帧恢复成本会随累计类型增长。长 Timeline 含大量分散效果时应 Profile 验证该成本，必要时按安全生命周期清理长期不再使用的状态。

### 9.2 GPU

Timeline Clip 本身不会为每个 Clip 重复执行一套 URP 后处理。Mixer 最终只写绑定 Volume Profile，GPU 成本主要由最终 Volume Stack 中实际启用的 Bloom、DOF、Motion Blur、Color Grading 等效果决定。

因此“一个多效果 Clip”和“多个单效果 Clip”在最终激活效果完全相同时，GPU 大头通常相近；差异主要在 Timeline CPU 遍历、Inspector 复杂度和资产体积。GPU 结论仍必须用目标平台的 URP Profiler、Frame Debugger 和 GPU Capture 验证，不能只按 Clip 数推断。

### 9.3 Editor 与资产体积

- 每个多效果 Clip 拥有一个本地 Profile 及若干 VolumeComponent 子资产，`.playable` 大小会随 Clip 数和组件数线性增长。
- 复制很多完整多效果 Clip 会重复保存全部参数。多个效果起止时间一致时应合并在一个 Clip；只有时序或混合需求不同才拆分。
- 单效果 Clip 没有 Profile 子资产，但仍会序列化该生成类型的字段；大量 Clip 依然会增加 Timeline 导入、Inspector 和序列化成本。
- 嵌入原生 Profile Inspector 可以复用 UI，但不要在 OnGUI 重绘中扫描、保存和重导入整个 `.playable`。结构操作与普通参数编辑必须分流。
- 外部 Profile 导入、孤立子资产扫描和持久 ID 检查属于 Editor 结构维护，不得进入 Runtime，也不应在每次 Repaint 无条件执行。

### 9.4 Authoring 选择

| 场景 | 推荐 |
| --- | --- |
| 多个效果完全同起止时间 | 一个多效果 Clip |
| 单个效果需要独立淡入、淡出或关键帧 | 对应的单效果 Clip |
| 同类型需要多个 Clip 交叠 | 单效果 Clip，并验证连续/离散参数混合 |
| 只是复用已有预设 | 导入外部 Profile 到多效果 Clip 的本地副本 |
| 需要高低优先级覆盖 | 少量显式 Layer；后置 Layer 只提交自身 Override |

大量使用前至少建立三档样本，例如 20、100、500 个 Clip，分别记录 Timeline 窗口响应、`.playable` 体积、Domain Reload/导入时间、Play Mode CPU/GC 和目标平台 GPU。达到预算后停止继续拆 Clip，而不是等整条演出制作完成后再优化。

## 10. 新功能实施顺序

1. 冻结行为合同：叠加/隔离、多效果/单效果、参数动画、优先级、混合、恢复。
2. 确认绑定对象和 Volume Stack 前置条件。
3. 先完成一个代表性连续参数效果，例如 Vignette Intensity。
4. 建立 Baseline、每帧恢复和生命周期清理。
5. 验证两个同类型 Clip 交叠与淡入淡出。
6. 增加离散参数规则并验证同权重稳定性。
7. 实现多效果 Clip，并定义与单效果 Clip 的同类型优先级。
8. 建立 Clip 本地 Profile 的创建、导入、复制拆分、恢复、延迟保存和孤立清理链路。
9. 实现场景隔离引用计数与恢复。
10. 完成单效果直接 Inspector；不要先走代理原生 UI。
11. 对 Clip 本地 Profile 嵌入真实 Profile Inspector，并验证不会修改外部导入源。
12. 最后再实现生成器、菜单分组、批量生成和效果专用布局。
13. 建立小/中/大 Clip 数量样本，验证编辑器响应、资产体积和 Runtime CPU/GC。
14. 在真实 Timeline 资产上做只读确认，在临时资产上执行写入验证。

## 11. 验证清单

### 11.1 运行时与混合

- [ ] 绑定 Volume 的 Profile 没有目标组件时，Clip 仍能生效。
- [ ] Timeline 添加的组件在 Stop/销毁后被移除，原组件不被删除。
- [ ] 单 Clip 满权重、Ease In/Out、两个同类型 Clip 交叠结果正确。
- [ ] 连续、离散、Texture/Curve 参数分别符合混合合同。
- [ ] 多效果 Clip 与单效果 Clip 同类型时优先级确定，交叠切换无未定义跳变。
- [ ] 根 Track 与子 Layer 顺序稳定；后置 Layer 只提交自己勾选的 Override。
- [ ] Seek、Pause、Stop、重新播放不依赖历史帧。

### 11.2 场景 Volume

- [ ] 隔离关闭：场景不同类型效果与 Timeline 效果共同生效。
- [ ] 隔离关闭：同类型效果受 Priority、Weight 和 Override 控制。
- [ ] 隔离开启：其他场景 Volume 暂时禁用，Timeline 结束精确恢复。
- [ ] 两个 Timeline/Director 同时隔离时，先结束者不会提前恢复场景 Volume。
- [ ] 摄像机 Volume Layer Mask、Global/Local 和 Collider 范围已验证。

### 11.3 Inspector 与资产

- [ ] 每个 Override 可以单独点击，ALL/NONE、Undo/Redo 可用。
- [ ] Override 关闭只禁用 value，不禁用复选框本身。
- [ ] 多效果 Clip 显示真实组件结构、Add Override、ALL/NONE 并可编辑。
- [ ] 外部 Profile 导入后源资产不变化；Clip 本地 Profile 随 `.playable` 保存。
- [ ] 复制 Clip 后两个本地 Profile 已拆分，修改任一 Clip 不影响另一个。
- [ ] 反复选择、重绘、Undo/Redo、保存和 Domain Reload 不会持续刷新，也不会增加重复 Profile 子资产。
- [ ] Track 锁定、只读文件和 VCS 未 Checkout 时不可编辑。
- [ ] Inspector 切换、Profile 切换和 Domain Reload 无临时 Editor 泄漏。

### 11.4 Unity 验证边界

- [ ] Unity 导入和 Editor 编译无新增错误。
- [ ] Timeline 窗口中创建、选择、拖拽、混合和播放均验证。
- [ ] Add 菜单保留多效果顶级入口和单效果二级入口；Clip 标题未被菜单路径污染。
- [ ] 参数修改真实写入 Clip 或 Profile 的预期资产，并可 Undo。
- [ ] 多档 Clip 数量下记录 `.playable` 体积、导入/打开耗时、Runtime CPU/GC 和 GPU。
- [ ] Scene/Game 视图完成视觉验证；必要时使用 Frame Debugger/Profiler。
- [ ] 未覆盖或回退用户已有 `.playable`、Prefab、Scene 和 Profile 脏改。

## 12. 停止条件

当以下证据齐全时停止扩大实现：

- 多效果与单效果 Clip 的职责已经清楚；
- 场景叠加/隔离、同类型优先级和混合结果已验证；
- Inspector 中 Override 和本地 Profile 内容都能实际编辑并写入正确 `.playable`，且不污染外部 Profile；
- Stop/Seek/销毁能恢复；
- 生成器覆盖当前目标组件且程序集依赖合法；
- Unity 编译、真实 Timeline 播放和视觉结果均有证据。

不要继续为了“更像原版”反射更多 SRP 内部 API。若少数 Trackball/Curve UI 尚未达到原生视觉，应把它作为独立的 Timeline Drawer 完善任务，而不是破坏已经稳定的序列化与运行时路径。
