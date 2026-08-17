---
name: ta-development-rules-tool
description: TA 通用开发规则中的 Unity 工具开发模块。以通用 CORE 与项目 Profile 分层，覆盖 Editor UI、资产批处理、预览、导入辅助、文档和验证。
---

# TA 通用开发规则｜工具开发模块（CORE + PROFILE）

## 概览

> 文档模式：ta-development-rules/tool/v1
> 语言与编码：中文，UTF-8
> 模块入口：TA 规则总入口见 [../README_Tech_TADevelopmentRules.md](../README_Tech_TADevelopmentRules.md)；当前工程约定见 [Profiles/ProjectACG/README_Tech_ProjectACGTAToolsProfile.md](Profiles/ProjectACG/README_Tech_ProjectACGTAToolsProfile.md)。

本模块适用于 Unity Editor 工具、资产扫描和批处理、资源生成/迁移、预览、检查器扩展与受控的导入辅助。它不默认适用于运行时代码、Player 构建链、Addressables/YooAsset、资源导入全局 Hook 或渲染管线改造；这些工作只有在用户明确要求且 Profile/消费者已确认时才可进入范围。

工具实现先按职责和风险分类，再决定文件拆分。简单不是“没有 UI”，复杂不是“代码行数多”；分类取决于是否写资产、是否批处理、是否有取消/回滚需求、是否影响导入/构建、是否跨程序集或有运行时消费者。

## 工作流程

1. 执行 TOOL-DOC-03，读取目标工具、相邻工具、README、asmdef、菜单、资源与消费者。
2. 执行 TOOL-CLS-01，确定工具类别、读写范围、破坏性等级、运行时/构建影响与最小验证入口。
3. 执行 TOOL-CMP-01，冻结输入、输出、已有资产行为、可撤销边界和用户确认点。
4. 按 TOOL-ARC、TOOL-UI、TOOL-OPS、TOOL-ORG 实现最小闭环。
5. 按 TOOL-VAL 完成编译、Editor 行为、资产结果和边界用例验证。
6. 按 TOOL-EVO 将已验证问题归档为候选，避免在单个工具中累积隐式例外。

## 生成规则

### TOOL-GEN｜工具产物与最小结构

#### TOOL-GEN-01｜按复杂度生成，不套用固定模板

生成新工具前必须先完成 TOOL-DOC-03、TOOL-CLS-01 与当前 Profile 的目录/程序集检查；先选最小实现模式，再创建代码、配置、README 和 Unity 资产。工具不因为是“通用模板”就默认生成多文件、Runtime 组件、全局 Hook、配置资产、第三方依赖或构建逻辑。

| 工具类型 | 最小生成产物 | 仅在必要时追加 | 默认不生成 |
| --- | --- | --- | --- |
| 简单查询 / 定位 | 一个 Editor C# 文件、唯一入口、输入校验与可定位结果。 | 紧邻 README、局部测试。 | Runtime、配置资产、批量框架、导入/构建 Hook。 |
| 单资产编辑 | 最小 Editor 实现、明确写入范围、Undo 或回滚说明。 | 自定义 Inspector、局部配置。 | 全工程扫描、隐式覆盖、全局状态。 |
| 配置型窗口 | Window 入口、UI、校验、执行闭环；复杂度足够时按职责拆分。 | 配置资产、预览、导出报告、可安全取消。 | 空的 Actions/Validation/Assets/Logging 文件。 |
| 批量生成 / 转换 / 迁移 | 输入收集、计划预览、确认、逐项结果、汇总与回滚说明。 | 进度、耗时、可安全取消、批量编辑保护。 | 未确认覆盖策略的自动写入、循环内频繁刷新。 |
| 预览 / Inspector / Scene GUI | 目标 Editor 扩展、临时状态与释放策略。 | 临时资源池、Scene Handle、可显式保存动作。 | sharedMaterial 写入、全局 Shader 状态、持久副作用。 |
| 导入 / 构建 / Runtime 协作 | 显式需求证明、最小接口、禁用/回滚入口与高影响验证。 | 限定范围的 Hook、独立 Runtime asmdef。 | 无条件自动触发、全项目扫描、Editor API 泄漏到 Player。 |
| Shader / 材质工具 | 工具入口与目标 Shader 公开接口的最小 Adapter。 | 变体/材质报告、RendererFeature 配置检查。 | 私有光照实现、未验证 Keyword、全局参数替代局部接口。 |

生成完成后必须执行 TOOL-ORG-02 和 TOOL-VAL。新工具或明显改造工具在目标工具目录新增或更新 Art README 与 Tech README；仅限局部 bugfix 时更新已有说明，不机械创建同义文档。

## 通用规则（CORE）

### TOOL-DOC｜文档协议与上下文

#### TOOL-DOC-01｜层级与稳定 ID

- MUST：未满足不可交付，除非记录用户批准的例外、影响和回退。
- SHOULD：默认行为；不采用时写明替代方案、风险与验证。
- CORE 不得包含固定资产路径、菜单名称、Unity 补丁版本、特定类别名、团队工具名或第三方插件。
- PROFILE 可定义目录、菜单、UI 术语、版本、已有工具和程序集约定，但不得静默弱化资产安全、依赖边界和验证要求。
- 一条规范性要求只能在一个稳定 ID 下定义；其他位置引用 ID，不复制成平行规则。

#### TOOL-DOC-02｜任务阅读索引

| 任务 | 必读规则 | 额外资源 | 最小验证 |
| --- | --- | --- | --- |
| 简单查询/辅助窗口 | TOOL-CLS-01、TOOL-UI-01、TOOL-VAL-01 | 目标附近窗口和菜单 | Unity 编译、打开窗口、基本输入校验。 |
| 资产扫描/报告 | TOOL-ARC-01、TOOL-OPS-01、TOOL-VAL-02 | 工具模式、资源消费者 | 结果可定位、抽样复核、空结果与异常输入。 |
| 批量写入/生成/迁移 | TOOL-CMP-01、TOOL-OPS-02、TOOL-VAL-02 | 集成检查 | 预览、确认、资产写入、取消/失败和回滚边界。 |
| 预览/Scene GUI/Inspector | TOOL-UI-02、TOOL-ARC-02、TOOL-VAL-02 | 目标编辑器扩展、生命周期 | Inspector/Scene 切换、资源重载、关闭和异常恢复。 |
| 导入/构建/运行时协作 | TOOL-ARC-03、TOOL-OPS-03、TOOL-VAL-03 | 集成检查、调用链 | Editor/Player 边界、导入或构建、禁用/回滚。 |
| Shader/材质工具 | TOOL-ARC-01、TOOL-OPS-02、TOOL-VAL-02 | Shader 模块及对应 Profile | 材质序列化、Shader 编译、目标绘制。 |

#### TOOL-DOC-03｜实现前上下文加载与澄清闸门

新增、重写、优化或审查工具前，必须读取当前 Profile、目标工具及其 README、直接使用的 asmdef、菜单入口、配置资产、资源读写范围和相邻同类实现。资产或渲染相关工具还必须读取实际消费者：Prefab、Scene、材质、导入器、脚本、RendererFeature 或构建流程。

必须明确工具类型、输入/输出资产、是否写入、是否可撤销、是否批量、是否允许取消、是否影响 Player/导入/构建、目标用户、目标平台和性能预算。仅在缺失信息会改变资源范围、破坏性等级、程序集边界、构建行为或交互契约时向用户追问。

### TOOL-CLS｜分类与实现选择

#### TOOL-CLS-01｜按职责与风险选择最小模式

- 只读查询 / 定位：不改资产；提供明确筛选、可定位结果和空结果说明。
- 单资产编辑：只修改用户明确选中的资产；优先使用 SerializedObject、Undo 与最小写入。
- 批量转换 / 生成：必须将输入收集、预览、校验、用户确认、执行、汇总分层；不可把扫描结果与写入混成一次不可追溯操作。
- 预览 / Inspector / Scene GUI：只维护 Editor 生命周期内的临时状态；不得把预览副作用写回正式资源，除非用户明确执行保存。
- 导入 / 构建 / 运行时协作：属于高影响模式。默认不用全局 Postprocessor、构建回调、静态初始化或运行时依赖解决局部工具需求。

简单工具可使用单一 C# 文件；只有同时存在复杂 UI、执行流程、校验、资产 IO、日志/报告或跨程序集依赖时才按职责拆分。不得为了模板机械创建空文件，也不得把复杂流程全部塞进一个不可测试、不可定位的窗口类。

### TOOL-CMP｜行为与资产兼容性

#### TOOL-CMP-01｜既有工具和资产默认行为保真

优化、重构或修复既有工具默认属于行为保真修改。冻结菜单入口、配置字段和序列化键、默认值、输入范围、输出路径/命名、覆盖策略、旧资产兼容、Undo、日志、构建/导入影响和代表性结果。

新增可见参数默认不能改变旧配置和既有资产输出；改变默认值、覆盖策略、路径、命名、写入时机、菜单、配置序列化、运行时绑定或导入/构建行为，属于独立兼容性改动。必须单独说明迁移、回退和受影响资产，不能作为格式整理混入。

### TOOL-ARC｜职责、依赖与生命周期

#### TOOL-ARC-01｜输入、校验、执行、输出分层

- 输入层只收集用户选择、配置和上下文，不在 OnGUI / Inspector 绘制阶段执行写资产或长耗时工作。
- 校验层负责路径、类型、依赖、冲突、覆盖风险、执行前置条件和可处理数量。
- 执行层只消费已校验的计划；对批处理记录每一项的成功、失败、跳过和错误原因。
- 输出层负责写入、刷新、选择/定位结果、汇总与可复制日志。不得把失败静默吞掉或只以 Console 堆栈代替用户可用的结果。

资源路径以 AssetDatabase 和对象引用为事实来源。不要要求用户输入裸字符串路径；只有外部文件工具才使用文件选择器，并把外部路径与 Assets 内路径明确区分。

#### TOOL-ARC-02｜Editor 与运行时边界

Editor 工具代码必须位于 Editor 目录或 Editor-only 程序集中。除非需求明确，工具不引入运行时组件、Player 依赖、全局场景对象、构建配置或资源包规则。确需 Runtime 协作时，Runtime API 必须独立、最小、可禁用，Editor 与 Runtime 的 asmdef 依赖方向必须单向明确。

预览、临时材质、临时 RenderTexture、事件订阅、SceneView/Inspector Hook 和静态缓存必须定义创建、重用、释放与 Domain Reload 行为；关闭窗口、切换选择、资源删除和异常退出不能残留写入或全局状态。

#### TOOL-ARC-03｜导入、构建与全局 Hook

IPreprocessBuildWithReport、IPostprocessBuildWithReport、AssetPostprocessor、InitializeOnLoad、全局菜单、EditorApplication 更新回调和全局渲染回调均属于项目级公开接口。局部工具不得默认依赖它们。

确需使用时，先证明显式菜单/手动刷新/局部入口无法满足需求；定义触发条件、幂等性、禁用开关、作用资产范围、顺序、性能预算、失败策略和回滚；验证对无关资源、增量导入、CI/BatchMode、Player 构建和 Domain Reload 的影响。

### TOOL-UI｜可操作性与编辑器体验

#### TOOL-UI-01｜UI 状态与交互契约

UI 只呈现状态和接收输入，不承担隐式执行。主操作区必须能识别输入、当前配置、校验状态、风险、执行入口、结果和错误；长页面按功能折叠或分区，短而线性的工具不强制套用 Foldout。

资源入口优先使用 ObjectField、文件夹选择、资产列表和可点击定位。对无效、缺失、类型错误、范围外或冲突输入要在执行前给出可行动提示。显示文本遵从项目的本地化机制和 Profile 术语，不把临时排查文本作为永久 UI。

#### TOOL-UI-02｜预览与选择状态

预览工具应区分临时预览、应用到资源和保存到资源。切换选择、窗口关闭、异常、撤销/重做、Domain Reload 与 Play Mode 切换后，临时状态必须可重建或安全释放。不得借助 sharedMaterial、全局 Shader 参数或持久资产写入来实现本应局部的编辑器预览。

### TOOL-OPS｜资产操作、性能与日志

#### TOOL-OPS-01｜执行前校验与显式计划

每次执行前先构建可观察计划：目标数、成功候选、跳过项、冲突、覆盖/删除风险和预计输出。对只读扫描可直接执行；对写入、覆盖、替换、删除、迁移或不可安全中断的批处理，必须先预览、再由用户确认、后执行。

当用户要求白名单、禁用或跳过某类处理时，在执行路径上硬短路；不得只隐藏 UI 或输出警告后继续处理。

#### TOOL-OPS-02｜批量写入、Undo、取消与性能

可撤销的对象编辑接入 Undo；不可撤销或不适合逐项 Undo 的批量写入，必须在确认区与 README 说明回滚方式。使用 AssetDatabase.StartAssetEditing 时必须由 try/finally 成对保护；只在确定能降低批量导入成本的写入流程中使用，循环内避免频繁 SaveAssets、Refresh、重导入或全工程搜索。

长耗时处理应提供进度、耗时和最终汇总；仅在流程能够保持资源一致性时提供取消。取消或异常后，输出已完成/未处理/失败的边界，不能将结果伪报为全部成功。

#### TOOL-OPS-03｜日志、诊断与可回放性

日志至少区分 Info、Warning、Error，包含工具身份、目标路径或可定位对象、结果和异常摘要。批量操作结束时汇总总数、成功、失败、跳过和总耗时。复杂流程记录输入快照、关键配置、覆盖决策和输出位置，使问题可复现；不要仅依赖 Unity Console 的瞬时堆栈。

### TOOL-ORG｜代码、注释与文档

#### TOOL-ORG-01｜模块与依赖方向

复杂工具推荐按入口/状态、UI、校验、Actions、Assets/IO、Logging 拆分。入口只持有生命周期与状态协调；UI 不直接写资产；校验不执行副作用；Assets/IO 不反向依赖窗口绘制。公共函数库只能容纳跨工具稳定复用、无工具私有状态的能力。

所有工具脚本使用命名空间。常量、路径键、菜单键和序列化键集中定义；避免散落魔法字符串。文件超过可维护阈值或职责混杂时拆分，但不以固定文件清单为准入条件。

#### TOOL-ORG-02｜中文技术注释、UTF-8 与 README

新增或明显改造的 Editor 工具、配置和技术文档使用 UTF-8。中文注释只说明非显而易见的 Unity 生命周期、资源流程、渲染/Shader 逻辑、关键输入输出、特殊约束、兼容性或回滚边界；不逐行翻译表面代码。

新增工具或明显改造现有工具时，必须在工具目录维护可区分的 Art README 与 Tech README。Art README 说明入口、输入、步骤、输出、常见问题与注意事项；Tech README 说明代码入口、模块、核心流程、依赖/边界、风险/回滚和验证。小型 bugfix 不强制重复新建 README，但已有 README 必须同步更新受影响约定。

### TOOL-VAL｜验证与交付

#### TOOL-VAL-01｜静态与编译检查

检查 UTF-8、乱码、命名空间、程序集引用、菜单唯一性、序列化字段、资源路径、API 可用性、事件订阅/释放、异常处理和文档链接。Unity Editor 必须重新编译受影响程序集；dotnet build、文本搜索或 diff 检查只能作为补充，不能替代 Unity Editor 编译。

#### TOOL-VAL-02｜Editor、资产与边界行为

在 Unity 内实际打开入口并执行代表性流程，覆盖无输入、错误类型、路径不存在、重复项、只读/只写、取消、异常、覆盖、空结果、重开窗口、Domain Reload 和资源重导入。写资产工具还要抽样检查输出资源、引用、Meta/GUID、Undo/回滚边界和结果计数。

Shader/材质工具必须额外执行 Shader 模块的编译、材质/场景绘制和变体检查；不得以工具窗口无报错替代渲染验证。

#### TOOL-VAL-03｜高影响链路

涉及导入 Hook、构建回调、运行时 API、SceneView/全局回调、临时资源或跨程序集程序集时，验证启用与禁用、无关资产、增量流程、BatchMode/CI、Player 编译、异常恢复和回滚。未能完成 Unity/构建/目标资源验证时，交付中必须明确说明实际完成的静态检查和剩余风险。

### TOOL-EVO｜规则演进

#### TOOL-EVO-01｜证据驱动更新闭环

每次已验证的工具错误、性能问题、资产损坏、路径/GUID 丢失、Undo 失效、菜单冲突、程序集问题或生命周期泄漏，都记录现象、根因、修正、适用范围、验证与回退。单一工具问题更新其 Tech README；跨工具稳定模式更新 CORE；路径、版本、程序集或团队约定更新 Profile。不得根据一次未复现的猜测把限制写成全局规则。

## 项目定制规则（PROFILE）

CORE 不包含固定目录、菜单、Unity 补丁版本、历史工具位置或第三方程序集。执行具体项目任务时，必须加载目标工程的工具 Profile；Profile 只能补充项目事实和收紧约束，不能绕过 TOOL-CMP、TOOL-ARC、TOOL-OPS 或 TOOL-VAL。

当前 ProjectACG 的工具 Profile 位于 [Profiles/ProjectACG/README_Tech_ProjectACGTAToolsProfile.md](Profiles/ProjectACG/README_Tech_ProjectACGTAToolsProfile.md)，其中定义：

- 新工具目录、类别、菜单和窗口命名；
- 历史 Render、LYJ_Tool 与 Plugins/TA_Tools 的维护边界；
- 原生 EditorWindow、中文专业术语和资源选择约定；
- 批处理日志、构建/导入隔离、README 与角色 Shader 调试链路。

## 资源加载与规则维护

- 新增简单窗口或查询工具：读取 TOOL-CLS-01、TOOL-UI-01、当前 Profile、目标附近同类窗口和 [references/tool-development-patterns.md](references/tool-development-patterns.md)。
- 新增扫描、批量写入、生成或迁移工具：额外读取 TOOL-ARC-01、TOOL-OPS-01、TOOL-OPS-02 和 [references/tool-integration-checklist.md](references/tool-integration-checklist.md)。
- 对 Humanoid 动画进行截图识别、分类、双语改名或资源组迁移：额外读取 [Unity 动画资源编目、识别与安全迁移参考](references/animation-resource-catalog-and-safe-migration.md)；ProjectACG 任务再读取 [动画资源编目与双语命名 Profile](Profiles/ProjectACG/animation-resource-catalog-and-bilingual-naming.md)。
- Mini 工程、外包/内部工程生成或资源裁剪：额外读取 TOOL-ARC-01、TOOL-OPS-01、TOOL-OPS-02、TOOL-VAL-02、TOOL-VAL-03 和 [Unity Mini 工程导出与交付参考](references/outsource-mini-project-builder-delivery.md)；ProjectACG 任务再读取 [外包 Mini 工程生成工具 Profile](Profiles/ProjectACG/outsource-mini-project-builder.md)。
- 将单项烘焙器扩展为单个/批量工作区，或涉及 GPU 阶段进度、安全取消、多文件提交：额外读取 [references/batch-baker-workspace-and-cancellation.md](references/batch-baker-workspace-and-cancellation.md)，并验证计划预检、覆盖确认、取消检查点、临时文件和原始输出内容。
- 将单一批处理窗口扩展为模型、材质、贴图等可切换模块：读取 TOOL-ARC-01、TOOL-UI-01、TOOL-OPS-01、TOOL-OPS-02、TOOL-VAL-02 和 [references/modular-batch-resource-tool-design.md](references/modular-batch-resource-tool-design.md)。
- 直接编辑 FBX 源资源：额外读取 TOOL-CMP-01、TOOL-ARC-02、TOOL-ARC-03、TOOL-OPS-01、TOOL-VAL-02、TOOL-VAL-03 和 [references/fbx-source-asset-editor-development.md](references/fbx-source-asset-editor-development.md)；ProjectACG 任务再读取 [Profiles/ProjectACG/fbx-source-asset-editor.md](Profiles/ProjectACG/fbx-source-asset-editor.md)。
- 预览、Inspector、Scene GUI 或临时渲染工具：额外读取 TOOL-ARC-02、TOOL-UI-02，并检查现有 Editor 生命周期与资源释放实现；涉及 `AnimationMode`、Selection 目标锁定、取消预览或源模型姿态恢复时，读取 [Unity 动画预览工具生命周期与姿态恢复参考](references/unity-animation-preview-lifecycle-and-pose-restoration.md)，ProjectACG 任务再读取 [AnimationClipPreviewer Profile](Profiles/ProjectACG/animation-clip-previewer.md)。
- 动画批量截图或短 Clip 代表帧任务：额外读取 TOOL-CMP-01、TOOL-ARC-01、TOOL-OPS-01、TOOL-OPS-02、TOOL-VAL-02，以及 [动画批量截图与短 Clip 采样参考](references/animation-batch-screenshot-and-clip-sampling.md)；ProjectACG 任务再读取 [AnimationBatchScreenshotTool Profile](Profiles/ProjectACG/animation-batch-screenshot-tool.md)。
- Prefab 重新生成后的配置迁移、组件/引用恢复、层级对象子树、路径映射或第三方组件缓存重建：读取 TOOL-CMP-01、TOOL-ARC-01、TOOL-ARC-02、TOOL-UI-01、TOOL-UI-02、TOOL-OPS-01、TOOL-OPS-02、TOOL-VAL-02 和 [Prefab 模块快照、映射与事务恢复参考](references/prefab-module-snapshot-and-restore.md)；ProjectACG `CharacterPrefabBuilder` 任务再读取 [Character Prefab 模块快照与恢复 Profile](Profiles/ProjectACG/character-prefab-module-snapshot-and-restore.md)。
- 导入、构建、运行时协作或全局 Hook：额外读取 TOOL-ARC-03、TOOL-VAL-03；先确认显式局部入口是否足够。
- Shader、材质、变体或 RendererFeature 工具：同时读取 Shader 开发模块及其引用资料，二者的验证均不可省略。

资源加载以目标功能和风险为边界。Profile、目标资产或 Unity API 与外部模板冲突时，以当前工程可验证事实为准，并把差异记录到 Profile 或候选中。
