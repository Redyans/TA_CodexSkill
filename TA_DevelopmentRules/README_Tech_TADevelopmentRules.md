---
name: ta-development-rules
description: 面向 Unity 技术美术的可迁移开发规则入口。按 Shader、工具与 Timeline 开发分模块维护，并将跨项目 CORE 与项目 PROFILE 明确隔离。
---

# TA 通用开发规则（Shader + 工具 + Timeline）

## 0. 写作前置规则（所有 AI 必须先执行）

> 在本目录新增或修改模块、规则、技巧、经验、技术说明前，必须先完成“内容分层”和“格式归一”。未确定落位层级、事实来源与验证边界时，不得直接写入 CORE，也不得把一次项目排查记录包装成通用规则。

### 0.1 先分层，后写作

| 层级 | 只允许写入 | 禁止写入 | 默认位置 |
| --- | --- | --- | --- |
| `CORE`（通用型） | 跨项目稳定成立、能由工程原理或充分证据支持的职责边界、兼容性、安全和验证要求。 | 项目名、固定路径、Unity/包补丁版本、具体类型/字段/枚举、菜单、资产、GUID、Shader Pass/Keyword、当前缺口或团队历史约定。 | 对应模块入口的“通用规则（CORE）”。 |
| `REFERENCE`（通用技巧/经验） | 可迁移的实现模式、排查方法、决策表、检查清单、示例和报告模板；使用前仍需按目标项目验证。 | 与 CORE 平行的强制规则、未经说明的项目事实、只描述一次故障过程的流水账。 | 对应模块的 `references/`。 |
| `PROFILE`（项目定制） | 项目版本、目录、类型、字段、枚举、菜单、资产、渲染时序、团队约定、当前限制、验证状态和回退边界。 | 冒充跨项目结论，或以项目习惯弱化 CORE 的安全、兼容性和验证要求。 | 对应模块的 `Profiles/<Project>/`；短小 Profile 可保留在模块入口，但不得与 CORE 混写。 |
| 功能 README | 单个 Shader、工具、Timeline 功能或资产的使用方式、参数、维护记录和局部约束。 | 应由整个模块共享的规则，或为了集中管理而复制的实现文档。 | 继续紧邻实际源码或资产。 |

落位判断按以下顺序执行：

1. 内容只要依赖项目名、固定路径、版本、类型名、字段、枚举、菜单、资产、GUID、Pass、Keyword、Renderer 配置或当前验证状态，默认归入 `PROFILE`。
2. 去除上述项目事实后仍能跨项目成立，并且证据足以支撑规范性要求，才允许提炼为 `CORE`。
3. 内容主要回答“如何实现、如何排查、如何选择”，且存在多种合理方案时，归入 `REFERENCE`，并引用相关 CORE ID，不另造平行强制规则。
4. 只有一次故障、尚未复现或验证不足的经验先作为候选保留在对应项目 Profile 的验证/候选区；不确定时宁可归入 `PROFILE` 或候选，也不得提前升级为 `CORE`。

### 0.2 新模块统一参考 ShaderDevelopment 结构

新模块默认参考 [ShaderDevelopment/README_Tech_URPShaderDevelopmentRules.md](ShaderDevelopment/README_Tech_URPShaderDevelopmentRules.md) 的层级与语气，不复制 Shader 领域内容。模块入口按以下顺序组织：

1. YAML front matter：`name`、`description`。
2. 一级标题：`TA 通用开发规则｜<领域> 开发模块（实际包含的层级）`。
3. `概览`：文档模式、语言/编码、总入口、适用范围、排除范围和分层说明。
4. `工作流程`：按“加载上下文 → 分类 → 冻结契约 → 最小实现 → 分层验证 → 交付演进”编排。
5. `通用规则（CORE）`：按职责域分组，使用稳定 ID。
6. `项目定制规则（PROFILE）`：较长时拆到 `Profiles/<Project>/`，模块入口只保留边界和链接；不得在 CORE 段落中穿插项目事实。
7. `资源加载与规则维护`：列出按任务最小加载的 reference、Profile、源码/资产和验证入口。

通用技巧、经验、实现模式和检查清单统一放入 `references/`；项目 Profile 统一放入 `Profiles/<Project>/`。已有模块尚未拆目录时可以渐进迁移，但新增内容必须直接落到正确层级，不继续扩大混写。

### 0.3 规则、技巧与经验使用同一写作格式

规范性规则统一使用以下骨架；没有内容的字段可省略，但顺序不变：

```markdown
#### <稳定 ID>｜<动作性标题>

<先写结论及适用范围，不写调查过程。>

- **必须（MUST）**：<不可缺失的行为或边界。>
- **应当（SHOULD）**：<默认做法；偏离时说明理由。>
- **禁止（MUST NOT）**：<会破坏兼容性、安全或验证的做法。>
- **验证**：<可执行的检查及通过条件。>
- **例外与回退**：<允许例外的条件、影响和回退方式。>
```

规则 ID 沿用模块既有前缀与编号，不为“补充说明”创建同义新规则，也不重排已有 ID。`PROFILE` 规则须使用能够识别项目层级的前缀或位于明确命名的项目 Profile 中。

通用技巧、经验和技术参考统一使用以下骨架，不使用 MUST/SHOULD 创造新的强制层：

```markdown
# <主题>参考

> 类型：REFERENCE；适用范围：<范围>；使用前提：必须以目标项目代码、资产和版本重新验证。

## 1. 适用场景
## 2. 前提与依赖
## 3. 实现或排查步骤
## 4. 风险与不适用边界
## 5. 验证与回退
```

项目经验若需要保留现象与根因，统一写入对应 `PROFILE`，按“项目事实/现象 → 根因 → 项目约束或修正 → 验证证据 → 未验证项/回退”表达；不能只保留聊天记录、时间线或没有结论的排查日志。

### 0.4 统一文风与写入检查

- 结论先行，使用工程规范语气；同一概念、标题层级、表格列名和 MUST/SHOULD 术语保持一致。
- 中文内容、UTF-8、英文文件名；代码标识符、路径、菜单、Pass、Keyword 和命令使用反引号。
- 事实与建议分开写；项目事实注明来源和当前验证边界，未验证内容不得写成已确认结论。
- 一个要求只保留一个规范来源：已有规则能表达时修订原条目，其他文档只引用稳定 ID 或相对链接。
- 新增或移动 Unity 文档资产时同步维护 `.meta` 并保持 GUID 唯一；所有相对链接必须可解析。
- 完成后至少检查：分层是否正确、结构是否匹配、ID 是否唯一、链接是否有效、UTF-8/乱码、尾随空白、项目事实是否误入 CORE、REFERENCE 是否偷渡强制规则。

## 1. 目的与使用边界

本目录是 TA 开发规范的单一入口，覆盖三类工作：

- Shader 开发：场景、角色、URP PBR、Toon、Custom Lighting、RendererFeature、材质 Inspector 与变体。
- 工具开发：Unity Editor 窗口、资产扫描/批处理、预览、导入辅助、资源生成与维护工具。
- Timeline 开发：Track、Clip、Mixer、Layer、绑定、材质/渲染状态、Inspector 与 Scene Handle。

规则以可迁移性为优先目标。跨项目稳定的工程约束写入模块 CORE；Unity 版本、URP、目录、菜单、ShaderGUI、已有资产、第三方库和团队约定写入对应 PROJECT PROFILE。迁移到其他工程时，保留 CORE 与参考文档，删除或重建 Profile；禁止把当前工程路径、枚举、Pass、包版本或历史工具目录当成通用事实。

本目录只集中维护规范性文档、参考清单和报告模板。每个具体 Shader 或工具的 Art / Tech README 继续紧邻其资产或源码，作为该功能的用户说明和维护记录；不得为集中目录而批量移动既有资产文档。

## 2. 模块入口

| 模块 | 入口 | 适用内容 |
| --- | --- | --- |
| Shader 开发 | [ShaderDevelopment/README_Tech_URPShaderDevelopmentRules.md](ShaderDevelopment/README_Tech_URPShaderDevelopmentRules.md) | Shader 分类、URP 复用、Pass、变体、ShaderGUI、角色/场景 Profile、行为保真重构。 |
| Unity 与 Painter Shader 对齐参考 | [ShaderDevelopment/references/unity-substance-painter-parity.md](ShaderDevelopment/references/unity-substance-painter-parity.md) | 从材质通道、直接光、IBL、颜色空间、Debug 和最小 Harness 分层定位 Unity/Painter 差异。 |
| 当前工程 Painter Shader Profile | [ShaderDevelopment/Profiles/ProjectACG/README_Tech_ProjectACGSubstancePainterShaderProfile.md](ShaderDevelopment/Profiles/ProjectACG/README_Tech_ProjectACGSubstancePainterShaderProfile.md) | ProjectACG `Chara_Cloth_V2` 的 MRA、Ramp、相机偏置高光、Unity 预卷积 Cubemap Atlas、间接高光与当前验证边界。 |
| Unity 与 DCC 本地预览链路参考 | [ToolDevelopment/references/local-dcc-preview-link.md](ToolDevelopment/references/local-dcc-preview-link.md) | 美术简易状态流、一键准备、显式目标、多材质独立实例、工程映射缓存、临时会话、颜色/Dirty/GPU 与运行时版本排查。 |
| 当前工程 Painter 材质预览桥接 | [ToolDevelopment/Profiles/ProjectACG/painter-material-preview-bridge.md](ToolDevelopment/Profiles/ProjectACG/painter-material-preview-bridge.md) | ProjectACG Bridge `2.10.1` 的美术简易模式、工程指纹映射、Unity-SP 参数/贴图/灯光/相机、版本端口与验证边界。 |
| RendererFeature 状态与时序 | [ShaderDevelopment/references/renderer-feature-stencil-and-timing.md](ShaderDevelopment/references/renderer-feature-stencil-and-timing.md) | 相机共享 Stencil、临时对象 ID、同帧/历史缓存、Pass 时序、清理策略与运动镜头验证。 |
| 工具开发 | [ToolDevelopment/README_Tech_TAToolDevelopmentRules.md](ToolDevelopment/README_Tech_TAToolDevelopmentRules.md) | 与 Shader 模块统一采用“概览 → 工作流程 → 生成规则 → CORE → PROFILE → 资源加载与维护”结构，覆盖 Editor 工具边界、UI、批处理、资产写入、日志、文档与验证。 |
| Timeline 开发 | [TimelineDevelopment/README_Tech_TimelineDevelopmentRules.md](TimelineDevelopment/README_Tech_TimelineDevelopmentRules.md) | Track、Clip、Mixer、Layer、绑定、材质参数混合、相机级渲染状态、Inspector、Scene Handle 与 Timeline 验证。 |
| 当前工程 CharacterRender Timeline Profile | [TimelineDevelopment/Profiles/ProjectACG/README_Tech_ProjectACGCharacterRenderTimelineProfile.md](TimelineDevelopment/Profiles/ProjectACG/README_Tech_ProjectACGCharacterRenderTimelineProfile.md) | ProjectACG 的 CharacterRenderController、CharacterRender Timeline、PerObjectShadow、Shader 接口和当前验证边界。 |
| 当前工程 Timeline Volume Profile | [TimelineDevelopment/Profiles/ProjectACG/README_Tech_ProjectACGTimelineVolumeProfile.md](TimelineDevelopment/Profiles/ProjectACG/README_Tech_ProjectACGTimelineVolumeProfile.md) | ProjectACG 的 URP/Timeline 版本、Global Volume Track、Clip 本地 Profile、生成式单效果、菜单和当前限制。 |
| 工具模式 | [ToolDevelopment/references/tool-development-patterns.md](ToolDevelopment/references/tool-development-patterns.md) | 依据工具类型选择单文件、分层窗口、扫描器、批处理、预览或受控 Hook 的最小实现。 |
| Unity 动画预览生命周期与姿态恢复 | [ToolDevelopment/references/unity-animation-preview-lifecycle-and-pose-restoration.md](ToolDevelopment/references/unity-animation-preview-lifecycle-and-pose-restoration.md) | Selection 目标快照、AnimationMode 所有权、播放/暂停/停止/取消语义、源模型姿态复制、问题排查与验证矩阵。 |
| 当前工程 AnimationClipPreviewer | [ToolDevelopment/Profiles/ProjectACG/animation-clip-previewer.md](ToolDevelopment/Profiles/ProjectACG/animation-clip-previewer.md) | ProjectACG 动画预览工具的路径、菜单、锁定目标、Clip 搜索、取消预览、FBX 姿态恢复和当前验证边界。 |
| 动画批量截图与短 Clip 采样 | [ToolDevelopment/references/animation-batch-screenshot-and-clip-sampling.md](ToolDevelopment/references/animation-batch-screenshot-and-clip-sampling.md) | 多 Clip 资源展开、静态 Pose 识别、短动画代表帧、按 Clip 帧率采样、输出恢复与验证矩阵。 |
| 当前工程 AnimationBatchScreenshotTool | [ToolDevelopment/Profiles/ProjectACG/animation-batch-screenshot-tool.md](ToolDevelopment/Profiles/ProjectACG/animation-batch-screenshot-tool.md) | ProjectACG 动画批量截图工具的路径、菜单、Pose/短动画开关、实现方式、问题根因和当前验证边界。 |
| 当前工程 Character Prefab 模块快照与恢复 | [ToolDevelopment/Profiles/ProjectACG/character-prefab-module-snapshot-and-restore.md](ToolDevelopment/Profiles/ProjectACG/character-prefab-module-snapshot-and-restore.md) | ProjectACG CharacterPrefabBuilder 的快照 Schema、FBX 来源识别、Hierarchy Object、Animator、Magica Cloth 2、双栏恢复 UI、已解决问题与当前验证边界。 |
| 批量烘焙工作区与安全取消 | [ToolDevelopment/references/batch-baker-workspace-and-cancellation.md](ToolDevelopment/references/batch-baker-workspace-and-cancellation.md) | 单个/批量模式 UI、执行计划、自动命名、覆盖确认、真实阶段进度、安全取消、临时文件提交与验证矩阵。 |
| 模块化批量资源工具设计 | [ToolDevelopment/references/modular-batch-resource-tool-design.md](ToolDevelopment/references/modular-batch-resource-tool-design.md) | 模型/GameObject、材质和贴图模块切换、Selection/目录范围、材质变体安全转换、紧凑 UI 与验证矩阵。 |
| Prefab 模块快照、映射与事务恢复 | [ToolDevelopment/references/prefab-module-snapshot-and-restore.md](ToolDevelopment/references/prefab-module-snapshot-and-restore.md) | 重新生成前快照、稳定路径、组件和层级对象原子模块、引用重绑、Analyze/Preview/Apply/Commit、第三方缓存重建、双栏映射 UI 与验证矩阵。 |
| 工具集成检查 | [ToolDevelopment/references/tool-integration-checklist.md](ToolDevelopment/references/tool-integration-checklist.md) | 新增/修改工具、资源写入、导入/构建 Hook、菜单和 asmdef 的检查清单。 |
| FBX 源数据编辑器开发参考 | [ToolDevelopment/references/fbx-source-asset-editor-development.md](ToolDevelopment/references/fbx-source-asset-editor-development.md) | FBX SDK 原始资源写回、单位/Pivot/UV/顶点色/材质槽/骨架实现、回读校验、程序集边界和问题规避。 |
| 当前工程 FBX 源资源编辑器 | [ToolDevelopment/Profiles/ProjectACG/fbx-source-asset-editor.md](ToolDevelopment/Profiles/ProjectACG/fbx-source-asset-editor.md) | ProjectACG FBXEditor 的路径、菜单、程序集、实现文件、已验证功能、已知限制和维护检查。 |
| 当前工程工具 Profile | [ToolDevelopment/Profiles/ProjectACG/README_Tech_ProjectACGTAToolsProfile.md](ToolDevelopment/Profiles/ProjectACG/README_Tech_ProjectACGTAToolsProfile.md) | ProjectACG 的目录、菜单、UI、README、历史目录和构建链边界。 |
| Mini 工程导出与交付参考 | [ToolDevelopment/references/outsource-mini-project-builder-delivery.md](ToolDevelopment/references/outsource-mini-project-builder-delivery.md) | Mini 工程最小依赖闭包、脚本/渲染配置边界、内部/强制模式、阻断修复、临时副本和验证流程。 |
| 当前工程 Mini 工程生成 Profile | [ToolDevelopment/Profiles/ProjectACG/outsource-mini-project-builder.md](ToolDevelopment/Profiles/ProjectACG/outsource-mini-project-builder.md) | ProjectACG 工具路径、三套 Profile、字段语义、MMD MaterialEditor、Warning 修正和当前验证状态。 |
| 当前工程 Painter 调色 LUT 工具 | [ToolDevelopment/Profiles/ProjectACG/painter-color-profile-baker.md](ToolDevelopment/Profiles/ProjectACG/painter-color-profile-baker.md) | `PainterColorProfileBaker` 的可烘焙范围、Identity、White Point、sRGB、路径和黑图排查。 |

各模块自己的参考文档、报告模板和当前工程 Profile 均位于对应模块目录内，不在本入口重复维护。

## 3. 规则分层与优先级

1. 系统、开发者、仓库级 AGENTS.md 和用户当前任务优先。
2. 模块 CORE 定义不可由项目差异改变的工程边界：最小范围、行为保真、资产安全、可验证性、UTF-8、依赖显式化。
3. 模块 PROFILE 记录项目事实，并且只能收紧 CORE；不能以“项目习惯”为由省略验证、静默破坏序列化、绕过资产安全或直接改管线源码。
4. 具体功能 README 记录单资产约束、参数、已验证行为和回滚方式；它不能反向改写 CORE 或 Profile。

规则条目使用稳定 ID。需要引用既有要求时链接或写明 ID，不复制同一要求形成多个版本。发生冲突时，以更高优先级的当前事实为准，并把差异记录为规则候选。

## 4. 资源加载方式

开始实现前先归类，再按最小充分原则加载资料：

1. 判断任务属于 Shader、工具、Timeline，或多个模块共同的资源/渲染链路。
2. 先读对应模块入口的 CORE，再读当前项目 Profile；只有项目事实会改变目录、API、资源接口、兼容性或验证方式时才继续读取 Profile。
3. 新增复杂功能、跨文件实现、资产写入、导入/构建 Hook、Keyword/变体、RendererFeature 或运行时接口时，读取模块指定的 reference / checklist。
4. 再读取目标附近的源码、README、asmdef、材质/Prefab/Scene/脚本消费者和直接依赖。已有实现足以回答的问题不得重复追问。
5. 不得把外部 AGENTS、Skill、模板或博客整段复制为规则。先按 CORE、Profile、冲突项分类；只吸收已验证且适用的要求。

跨模块任务必须同时加载相关模块。例如，Timeline 驱动角色 Shader 或 RendererFeature 时，Timeline 模块负责 Clip/Mixer、混合、恢复和 Editor 生命周期；Shader 模块负责参数、Pass、渲染时序和视觉验证；项目 Profile 负责具体类型、路径与优先级事实。

## 5. 共同交付门槛

- 只改完成当前效果或流程所需的范围；不覆盖、回退或重置用户已有改动。
- 新增或明显改造的模块使用 UTF-8，并为非显而易见的渲染、资源、编辑器流程和关键边界提供中文技术注释。
- 用户可见文本遵从目标项目的本地化机制；内部 TA 工具的默认显示语言、术语和资源入口由 Profile 定义。
- 完成静态检查、Unity 导入/编译和风险相称的功能验证；文本或 dotnet 检查不能代替 Unity 行为验证。
- 最终交付写清修改文件、入口、验证证据、未验证项和剩余风险。

## 6. 规则演进

已验证的编译错误、序列化失配、资源写入事故、视觉回归、变体异常、性能瓶颈、Unity API 差异或工具流程问题，均可形成规则候选。候选至少记录：现象、根因、修正、适用范围、兼容性影响、验证证据和回退方式。

候选必须按范围落位：单一资产进入其 README；同类功能进入模块规则；版本/目录/插件/资产依赖进入 Profile；只有跨项目稳定且证据充分的内容进入 CORE。自动化只能收集候选，不能静默改写已审定的规则；更新时修订原条目，不追加同义例外。

## 7. 当前目录约束

- 规则文件、reference 和模板均使用英文文件名、中文内容、UTF-8 编码。
- 新增 Unity 资产文档时必须提供对应 .meta；移动文档资产必须同时移动 .meta，保持 GUID 不变。
- 本目录不承载 Shader/HLSL、Editor C#、材质或 Prefab 的实现代码，也不替代各工具/Shader 紧邻的 Art / Tech README。
