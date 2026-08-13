---
name: ta-development-rules-timeline
description: Unity Timeline 开发的可迁移规则。以跨项目 CORE、通用 REFERENCE 与项目 PROFILE 分层，覆盖 Track、Clip、Mixer、Layer、绑定、渲染状态、Inspector、Scene Handle 与验证边界。
---

# TA 通用开发规则｜Timeline 开发模块（CORE + REFERENCE + PROFILE）

## 概览

> 文档模式：`ta-development-rules/timeline/v1`
> 语言与编码：中文，UTF-8
> 模块入口：TA 规则总入口见 [../README_Tech_TADevelopmentRules.md](../README_Tech_TADevelopmentRules.md)；通用实现模式见 [references/timeline-development-patterns.md](references/timeline-development-patterns.md)；URP Volume 通用规则见 [references/timeline-volume-post-processing.md](references/timeline-volume-post-processing.md)；ProjectACG 定制事实分别见 [CharacterRender Timeline Profile](Profiles/ProjectACG/README_Tech_ProjectACGCharacterRenderTimelineProfile.md) 与 [Timeline Volume Profile](Profiles/ProjectACG/README_Tech_ProjectACGTimelineVolumeProfile.md)。

本模块适用于 Unity Timeline 的 `TrackAsset`、`PlayableAsset`/Clip、`PlayableBehaviour`、Mixer、`ILayerable`、绑定、Inspector、Scene Handle，以及由 Timeline 驱动的材质与渲染状态。

它不把 Timeline 当作“任意脚本每帧写值”的入口。先确定状态的真正所有者和作用域：角色/Renderer、材质槽、相机、场景、Volume 或渲染管线。只有 Timeline 负责的状态才由 Mixer 聚合；已有系统的默认值、资源生命周期与优先级仍由原有所有者维护。

## 规则分层与加载顺序

| 层级 | 内容 | 跨项目使用方式 |
| --- | --- | --- |
| `CORE` | 本文件中带稳定 ID 的强制工程边界、兼容性、混合、生命周期和验证规则。 | 默认保留；除非规则本身被修订，不因项目名称、路径或渲染实现变化而复制新版本。 |
| `REFERENCE` | 可复用的实现模式、伪代码、决策表和检查清单，不新增与 CORE 并行的强制规则。 | 可以迁移；使用前必须用目标项目代码和版本事实重新验证。 |
| `PROFILE` | Unity/URP/Timeline 版本、工作区路径、具体 Track/Clip/Controller、Shader 属性、RendererFeature、菜单实现、枚举、优先级、当前缺口与验证状态。 | 只适用于对应项目；迁移到新项目时删除或重建，禁止直接当成统一风格。 |

加载顺序固定为 `CORE -> 当前项目 PROFILE -> 按需 REFERENCE -> 目标代码与资产`。PROFILE 只能补充或收紧 CORE，不能降低资产安全、恢复语义或验证门槛。把一次实现经验升级为规则时，先判断它是否依赖项目名、固定路径、类型名、枚举、Shader 参数、渲染管线版本或现有优先级；任一项为真时，默认进入 PROFILE，而不是 CORE。

## 工作流程

1. **TML-DOC-01**：读取目标 Track、Clip、Mixer、绑定类型、相邻 Track、Inspector、Shader/RenderFeature 消费者、asmdef/csproj 与已有 Timeline 资产。
2. **TML-CMP-01**：冻结已序列化类型、字段名、默认值、绑定类型、Clip Caps、已有资产行为与停止/恢复语义。
3. **TML-ARC-01**：明确 Track、Clip、Behaviour、Mixer、渲染协调器和 Editor 的职责，不把多种 Unity 可序列化对象混在一个职责不清的脚本中。
4. **TML-BLD-01**：为每一类参数写明混合规则、跨层冲突规则、缺失属性的保持/恢复规则与同权重决策。
5. **TML-RND-01**：先确认渲染状态属于 Renderer、材质、相机还是场景；相机级状态必须使用显式协调与优先级，而非角色局部写入。
6. **TML-EDT-01**：实现 Inspector 和 Scene Handle 时维护 Editor 生命周期、Undo、脏标记、绑定定位与预览刷新。
7. **TML-VAL-01**：完成编译、资产创建、Timeline 播放、交叠、Layer、Seek/Stop、绑定和渲染视觉验证；静态构建不能代替 Unity 行为验证。

## 通用规则（CORE）

### TML-DOC｜上下文与行为契约

#### TML-DOC-01｜实现前读取完整调用链

新增或修改 Timeline 功能前，必须读取：

- 目标 Track、Clip、Behaviour、Mixer 和同类实现；
- Timeline 的实际绑定对象及其父/子 Layer 关系；
- Inspector/自定义 Track Editor、Scene Handle 和资产创建入口；
- 参数最终消费者：Renderer、MaterialPropertyBlock、材质、全局 Shader 状态、Volume、RendererFeature、Runtime Controller 或 Shader；
- 相关 asmdef/csproj 与可执行验证入口。

必须先回答：值由谁保存、由谁混合、由谁最终消费、Clip 结束后谁恢复默认、多个 Director 或 Layer 同时写入时谁优先。不能仅根据 Inspector 中“能看到字段”就认定运行时效果已打通。

#### TML-DOC-02｜镜像已有 Controller 时维护能力与作用域映射

- Timeline 需要跟进既有 Controller/Profile 的新功能时，先建立“艺术功能 -> Controller 源字段 -> Clip 输入 -> Mixer 规则 -> 最终消费者 -> 恢复基线”的能力表；不能只按 Inspector 字段数量判断功能已经同步。
- 新功能必须先分类为角色/Renderer 局部状态、材质资产、场景共享状态或相机/RenderFeature 状态，再选择 MPB、资源写入、场景控制器或协调器。一个功能跨越多个作用域时，应拆成多个写入边界，由一个明确的解析合同组合结果。
- 能力映射的方法属于 CORE；具体 Controller 名、模式枚举、Shader 属性、RendererFeature 和项目路径只写入当前项目 PROFILE。

### TML-CMP｜序列化与资产兼容

#### TML-CMP-01｜Track、Clip 与 Behaviour 分职责定义

- `TrackAsset` 负责可放置 Clip 类型、绑定约束和 Mixer 创建；不要让它同时承载 Clip 的序列化数据定义。
- `PlayableAsset`/Clip 只保存可序列化的输入数据与 `CreatePlayable`；每一种独立 Clip 类型单独定义，避免一个 `.cs` 同时混合不相关的 Track 与 Clip 职责。
- 需要作为 Timeline Clip 子资产正常创建、显示或挂接 `ClipEditor` 的 `PlayableAsset`，必须在独立 `.cs` 文件中定义为唯一的 Unity 可序列化主类型。不要把该 Clip 与 `TrackAsset` 定义在同一源文件；否则 Unity 可能无法为 Clip 解析有效 `MonoScript`，Timeline 会把它显示为黄色脚本告警。
- `PlayableBehaviour` 只承接 Clip 数据；Mixer 才负责读取输入权重、聚合和写出副作用。
- 新字段默认值必须保持旧资产行为。改变字段含义、默认值、绑定类型、Clip 类型或恢复语义时，属于独立迁移任务，必须提供迁移和回退说明。
- 资源引用、枚举值和字段名均是资产契约；重命名或删除前先确认序列化迁移方案。

#### TML-CMP-02｜独立 Clip 脚本与资产迁移

- 将既有 Clip 从 Track 脚本拆到独立 `.cs` 时，视为序列化迁移：保持类型名、字段名、默认值、`ClipCaps` 与 `CreatePlayable` 行为不变，并验证旧 `.playable` 子资产指向新脚本的 `m_Script` GUID。
- 优先使用可审计的 Editor 迁移工具；必须直接修复 YAML 时，仅修改目标子资产的脚本引用和必要显示元数据，不重排、不格式化或覆盖同一 Timeline 资产中的无关脏改。
- 迁移后必须在 Unity 中确认旧 Clip 数据未丢失、Inspector 正常、Timeline 不再出现 Missing Script/黄色脚本告警，并保留可回退的资产变更记录。

### TML-ARC｜层、绑定与职责边界

#### TML-ARC-01｜Layer 只隔离自身状态，不隐式跨层混合

`ILayerable` 用于把独立的参数域放到同一根轨道下管理，不等同于“所有 Layer 自动互相混合”。每个 Layer 必须明确自己的 Mixer、写入目标和参数域：

- 同一 Layer 内、同一参数域的交叠 Clip 按该 Layer 的 Mixer 规则混合。
- 不同 Layer 的参数域独立时，不做跨层混合；例如材质参数 Layer 与角色渲染覆盖 Layer 分别写各自状态。
- 两个 Layer 可能写同一 Renderer/材质属性或同一相机级状态时，必须定义单一仲裁点或显式禁止组合，不能依赖 Playable 评估顺序。
- 根 Track 的绑定只作为统一入口；必须在 Unity 中验证子 Layer 是否继承该绑定，不能只依赖 API 推断。

#### TML-ARC-02｜按真实作用域选择写入边界

| 状态作用域 | 推荐边界 | 禁止的替代 |
| --- | --- | --- |
| 单角色 / 单 Renderer / 单材质槽 | Mixer 聚合后写 `MaterialPropertyBlock`，键包含 Renderer、材质槽与属性 ID。 | 直接改 `sharedMaterial` 或用全局 Shader 值模拟局部状态。 |
| 材质资源资产 | 显式用户操作、Undo、脏标记和资源影响说明。 | Timeline 播放时隐式修改共享材质。 |
| 相机级 RenderFeature / 屏幕空间资源 | Timeline 专属协调器 + 全局优先级 + 生命周期清理。 | 每个角色或 Mixer 直接抢写同一全局状态。 |
| 场景 / Volume | 使用既有 Volume/场景控制器的优先级合同。 | 绕过 Volume 显式覆盖或把场景状态伪装成角色局部参数。 |

### TML-BLD｜混合与恢复

#### TML-BLD-01｜参数类型决定混合规则

- 连续标量、颜色与普通向量：按有效权重求和/归一化平均，并定义无有效 Clip 时的回退值。
- 方向：先按权重累加，再归一化；零向量必须有明确语义，不能因归一化产生无效方向。
- Bool、枚举、模式、开关和互斥状态：由权重最高的有效 Clip 决定；同权重使用稳定的次序，避免每帧抖动。
- 同一参数在后续 Clip 缺失时，必须明确是保持上一有效值、恢复源材质/Controller 基线，还是使用 Clip 默认值。不能让“属性不存在”偶然变成资源值闪回。
- Renderer 局部覆盖使用 Timeline 专属 `value + weight`；`weight = 0` 必须精确回到材质/Controller 当前基线。不要为了避免默认零而把材质值复制进 Clip；只有显式覆盖的字段才使用 Clip 默认值，材质级参数仍应保留为最终计算因子。
- Inspector 范围与运行时范围必须一致。`Range`/Slider 只约束编辑入口，Mixer 或最终消费边界仍需 Clamp，防止旧序列化资源、脚本写入或 YAML 值越界。
- Mixer 每帧先重置聚合状态，再遍历有效输入；不要让上一帧的聚合值在 Clip 已结束后泄漏。

#### TML-BLD-02｜相机级离散状态使用协调器

相机级状态不能由单个 Mixer 清空或长期占用。协调器至少要：

1. 以 Mixer/Director 请求为单位登记或移除有效请求；
2. 在所有有效请求中按权重和稳定 tie-breaker 选出唯一胜者；
3. 在无请求、绑定丢失、图停止或销毁时清空 Timeline 专属覆盖；
4. 保留已有系统的优先级，例如 Volume 显式覆盖通常高于 Timeline，Timeline 高于默认 Controller；
5. 使用专属全局键，不覆盖默认 Controller 的全局键，避免相机回调在同帧反写。

### TML-EDT｜Inspector、Scene Handle 与预览

#### TML-EDT-01｜Editor 临时状态必须可释放

- Inspector 通过 `SerializedObject`/`SerializedProperty` 写 Clip；Scene Handle 修改需记录 Undo、应用序列化改动、标记脏对象并刷新 SceneView。
- Scene Handle 的显示开关、旋转缓存和临时预览状态仅存在于 Editor 生命周期；`OnDisable` 必须注销事件、清理临时状态并重绘。
- 从当前 Timeline 查找绑定时，递归检查根 Track 与子 Track；绑定变化、Director 缺失或目标 Clip 不在当前 Timeline 中都应安全退出。
- 方向 Anchor、旋钮和 Handle 是同一个序列化字段的不同编辑入口，修改其中任一入口后必须同步预览状态。
- IMGUI 旋钮必须闭合完整事件链：每个 `BeginChangeCheck` 对应同层级的 `EndChangeCheck`；`MouseDown` 获取 `hotControl`，`MouseDrag` 更新并消费事件，`MouseUp` 释放；旋钮变化与数值框变化分别收口后统一写回同一个 `SerializedProperty`。不得嵌套开启变化检查却只结束内层，否则控件视觉会动但数据不会写回。

#### TML-EDT-02｜修复资产告警，不隐藏告警 UI

- `ClipEditor.GetClipOptions` 可设置正常状态的高亮色、Tooltip 与新建 Clip 的默认显示名，但必须从 `base.GetClipOptions` 开始，不能清空或伪造 `errorText` 来隐藏黄色感叹号。
- 黄色图标或黄色标题首先按真实资产错误处理：检查 `PlayableAsset` 的 `MonoScript`、导入错误、`m_Script` GUID 与脚本类型；只在错误消除后再定义正常视觉样式。
- `OnCreate` 只影响新建 Clip。已有 Timeline 资产的名称或脚本迁移必须单独处理，并在播放前逐项确认。

### TML-VAL｜验证门槛

#### TML-VAL-01｜Timeline 改动的最小验证矩阵

| 风险 | 必须验证 |
| --- | --- |
| C# 与程序集边界 | Runtime、主程序集与 Editor 程序集编译；检查 asmdef/csproj 是否包含新脚本。 |
| 序列化与资产创建 | Track/Clip 能在 Timeline 中创建；旧资产无字段丢失、无 Missing Script/黄色脚本告警。 |
| Layer 与混合 | 同 Layer 交叠、不同 Layer 并排、同权重、无有效 Clip、属性缺失后的恢复。 |
| 绑定与生命周期 | 根绑定、子 Layer 绑定继承、Director Seek、Pause、Stop、图销毁与重新播放。 |
| 材质与渲染 | 目标材质槽、多个 Renderer、多个角色、Frame Debugger/Profiler、场景/相机切换。 |
| 相机级状态 | Volume 优先级、多个 Timeline/Director 仲裁、Clip 结束恢复、同相机多角色限制。 |
| Editor 交互 | Inspector 字段、正常 Clip 标题/Tooltip/高亮、Scene Handle 打开/关闭、Undo、切换选择、Domain Reload。 |

## 性能与交付

- 缓存目标 Renderer、材质槽和 `MaterialPropertyBlock`；不在每帧反复做全层级搜索、反射或资源扫描。
- 全局状态只在最终仲裁后写出；不要在每个 Clip 或每个 Renderer 循环中重复写相同的全局参数。
- Timeline 渲染功能的最终说明必须包含：修改入口、混合与优先级合同、验证证据、未验证的 Unity 行为，以及多角色/多相机/共享材质等剩余风险。
- Project 路径、具体 Track 名、枚举数值、Shader 参数名和 RendererFeature 实现细节只进入项目 Profile 或功能 README，不写入本模块 CORE。
