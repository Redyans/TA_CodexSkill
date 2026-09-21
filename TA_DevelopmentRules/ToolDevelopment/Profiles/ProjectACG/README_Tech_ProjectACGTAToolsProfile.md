---
name: projectacg-ta-tools-profile
description: ProjectACG 当前工程的 TA_Tools 定制规则。该文件依赖当前目录、菜单、程序集和历史资产，不可直接迁移到其他项目。
---

# ProjectACG TA_Tools Profile

## 1. 适用范围与版本事实

本 Profile 仅适用于当前 ProjectACG 工作区。通用规则以 [../../README_Tech_TAToolDevelopmentRules.md](../../README_Tech_TAToolDevelopmentRules.md) 为准；本文件只补充项目目录、入口、UI、文档和构建边界。Unity 编辑器基线为 2022.3.62f3；涉及 Shader、材质或 URP 渲染时，还必须读取 Shader 模块中的 PRJ-01，而不是在此复制 URP 事实。

新项目迁移时必须删除或重建本 Profile。不得把本文件中的 Assets 路径、类别、菜单、窗口参考、历史目录或程序集名称提升为 CORE。

## 2. 新工具位置与历史目录

### PRJ-TOOL-01｜新工具目录

当前工程的新 TA 工具默认位于：

Assets/Editor/TA_Tools/<Category>/<ToolName>/

ToolName 使用 PascalCase；一个工具一个目录。若工具有 Editor 代码，置于工具目录内的 Editor 子目录或现有兼容的 Editor-only asmdef 下；运行时代码仅在用户明确需要时置于单独 Runtime 目录。

新工具类别只使用 Character、Scene、Animation、UI、TA、Effect、Common。菜单路径同步使用 TA_Tools/<Category>/<ToolDisplayName>。

### PRJ-TOOL-02｜历史目录兼容

当前工程已存在 Render、LYJ_Tool 等历史目录。`Assets/Plugins/TA_Tools` 下的历史内容（`Scene/MeshSurfaceToolkit` 的 4 个旧版 Runtime 组件与 URP Shader）已按 PRJ-TOOL-22 下线，整个 `Assets/Plugins/TA_Tools` 目录树已移出工程；历史目录只维护既有功能、路径和程序集，不做批量迁移，也不作为新工具默认位置。

新需求无法归入 PRJ-TOOL-01 的类别时，先确认产品/维护归属；不得仅因已有历史目录就把新的工具继续堆入其中。对历史工具的小修复保持其原目录、菜单和文档位置，避免无关资产移动导致 GUID、入口或引用变化。

## 3. 菜单、窗口与 UI

### PRJ-TOOL-03｜命名与入口

- 新窗口菜单为 TA_Tools/<Category>/<ToolDisplayName>。
- ToolDisplayName、菜单末级名、窗口标题保持一致；MenuItem 必须在全工程唯一，提交前搜索冲突。
- 窗口类默认命名为 <ToolName>Window；复杂工具的常量集中声明 ToolDisplayName、MenuPath 与 WindowTitle。
- 若工具还提供 Assets 右键、Project Settings 或 Inspector 入口，必须在 Tech README 中一并写明，不能只记录主菜单。

### PRJ-TOOL-04｜编辑器界面

新窗口默认使用原生 EditorWindow，优先匹配 CharacterFbxPrefabBuilderWindow 的 OnGUI、单层 ScrollView、Toolbar、Header / HelpBox 分区风格。Odin 或其他窗口框架不是默认依赖；出现字体重影、双重滚动或绘制异常时，优先回到原生 EditorWindow。

TA 内部工具默认采用中文专业术语；用户可见文本仍须使用项目已采用的本地化/文本机制，不能把临时中文字符串散落在业务代码中。资源选择优先 ObjectField、文件夹选择器、资产列表和可点击定位；路径通过 AssetDatabase.GetAssetPath 获取，并校验位于 Assets/、存在且类型正确。

状态、校验和风险使用清晰提示区；结果文本紧凑、可复制、能定位源资产，并区分 Info、Warning 与 Error。

## 4. 执行、日志与安全

### PRJ-TOOL-05｜执行流程

- 关键配置支持恢复默认；执行前必须校验参数。
- 批量/长耗时工具显示进度、耗时和总计；可安全中断时提供取消，不可安全中断时在执行前明确说明。
- 日志格式为 [ToolName][Info|Warning|Error] 消息。
- 图集、场景转换、批量导出、替换、扫描等大工具结束时汇总总数、成功、失败、跳过、总耗时。
- 用户要求白名单、跳过或禁用功能时，必须在执行路径硬短路，不只隐藏 UI。

### PRJ-TOOL-06｜资产、构建与运行时边界

不修改 Library、Temp、Logs 或自动生成 csproj。移动 Unity 资产时保留 .meta，优先使用 Unity 安全 API；批量写入使用 StartAssetEditing / StopAssetEditing 时以 try/finally 成对保护。

新增或明显改造的 TA_Tools 默认独立：不影响 Player、运行时代码、资源构建或打包。除非用户明确要求，不新增 IPreprocessBuildWithReport、IPostprocessBuildWithReport、AssetPostprocessor、Addressables/YooAsset/URP 构建配置或全局导入行为；确需修改时，最终交付必须单独说明原因、影响资产和回滚方式。

删除、覆盖、替换等破坏性操作必须二次确认。能 Undo 的操作使用 Undo；不能撤销的批处理在 UI 和 README 中说明版本控制/备份等回滚路径。

## 5. 代码和 README 结构

### PRJ-TOOL-07｜按复杂度拆分

简单工具不强制多文件。复杂工具同时含较多 UI、执行、校验、资产 IO 或日志时，推荐以下职责边界：

- Window.cs：入口、字段、生命周期。
- Window.UI.cs：界面绘制。
- Window.Actions.cs：执行流程。
- Window.Validation.cs：参数校验。
- Window.Assets.cs：资源读写和路径处理。
- Window.Logging.cs：日志和汇总。

避免 UI、执行、校验、资产 IO 和日志全部堆到同一文件。单文件接近或超过约 1200 行，或职责已明显混杂时拆分；不以创建空文件满足形式要求。

### PRJ-TOOL-08｜工具 README

新增工具或明显改造工具时，在工具目录维护英文文件名的两份文档：

- README_Art_<ToolName>.md：入口、输入、操作步骤、输出、常见问题、注意事项和是否可撤销。
- README_Tech_<ToolName>.md：技术目的、代码入口、模块、核心流程、依赖与边界、风险与回滚、验证方式。

已有工具存在可明确区分 Art / Tech 的业务化英文文件名时可沿用；小型 bugfix 不要求新建同义 README，但必须更新已受影响的内容。当前历史工具文档不齐全，不因本 Profile 进行全量补文档或重命名。

## 6. Shader 与渲染工具专项

### PRJ-TOOL-09｜统一调试与材质接口

角色 Shader 体系已有统一调试链路时，工具与 Shader 优先复用它，避免每个 Shader 各自增加临时 debug 分支。当前角色体系的参考入口是 Assets/Shader/Character/Common/CharacterShaderDebug.hlsl 与 ACG_TryApplyCharacterShaderLightingDebug；仅在目标 Shader 已经接入该体系时使用，不能对无关 Shader 强行添加。

材质、Keyword、Drawer、Timeline、MaterialPropertyBlock、全局 Shader 状态和渲染顺序均是公开接口。工具批量改写前必须确认 Shader Properties、脚本/动画写入与实际消费者；局部角色效果不能用 Shader.SetGlobal 影响全场景。

### PRJ-TOOL-10｜Painter Color Profile 烘焙与输出域

当前 `PainterColorProfileBaker` 位于历史 `Assets/Editor/TA_Tools/Render/PainterColorProfileBaker/`，菜单为 `TA_Tools/Render/Substance Painter 调色预览 LUT`。它只烘焙逐像素 `RGB → RGB` 的 Tonemapping 和调色链，不承诺 Bloom、Fog、轮廓、深度、屏幕 Mask 或角色/场景同时存在的分支效果。

Identity EXR、White Point、sRGB 显示编码、Volume Active/Override、ObjectField/直接路径、批量 Volume Profile 和黑图诊断的完整项目契约见 [Painter Color Profile Baker Profile](painter-color-profile-baker.md)。普通 Painter SDR 流程保持“输出 sRGB 显示编码”开启；White Point 必须在 Unity 工具和 Painter 完全一致，且不得直接照搬 Window 默认值。批量工作区的计划、进度、取消和结果计数还应读取 [Editor 批量烘焙工作区与安全取消参考](../../references/batch-baker-workspace-and-cancellation.md)。

### PRJ-TOOL-11｜Unity-SP 材质预览桥接保持临时会话边界

当前 `PainterMaterialParameterBridge` 位于历史 `Assets/Editor/TA_Tools/Render/PainterMaterialParameterBridge/`，菜单为 `TA_Tools/Render/Unity-SP 材质参数同步`。它默认提供美术简易模式，可一键扫描/修复独立 Painter 材质实例，并支持当前/全部材质显式目标同步、当前材质显式目标主灯预览、80 项白名单参数、Unity ↔ Painter 临时贴图预览、SP → Unity Dirty 增量实时刷新、Unity → SP 有效主光预览，以及独立 `6419` 双向相机同步。当前 Painter Bridge 源码版本为 `2.10.1`，运行时仍以握手版本为准。

该工具不能把预览缓存写回正式 Painter 图层、Unity 源贴图或正式 Material。整组通道缺失时必须保持原 Unity 贴图槽；Color 参数以 Linear 作为协议边界；临时 Renderer 材质槽和 Painter Shader 参数必须在退出、断线和生命周期变化时恢复。不同材质必须绑定各自独立 Shader Instance；本机映射按 Painter `inventory-fingerprint` 与 Unity Material GUID 合并缓存，并在每次发送前重验证。相机只同步位置、朝向、投影模式、垂直 FOV/正交高度，不读取 Painter 视口、不绘制目标框，也不做画幅补偿。通用实现见 [Unity 与 DCC 本地材质预览链路参考](../../references/local-dcc-preview-link.md)，当前路径、版本、端口、映射、通道、颜色、实时参数、灯光、相机和插件安装见 [Painter 材质预览桥接 Profile](painter-material-preview-bridge.md)。

### PRJ-TOOL-12｜历史 Odin 烘焙工具使用一级工作区收敛界面

当前历史 `Render` 目录中的以下 Odin 窗口已采用一级工作区：

- `PainterColorProfileBaker`：`单个烘焙 / 批量烘焙`；
- `UnitySpecCubeAtlasBaker`：`单个烘焙 / 批量烘焙`；
- `PainterMaterialParameterBridge`：默认 `美术简易模式`，并保留 `单材质预览 / 批量同步 / 场景同步 / 高级 / JSON`。

一级工作区只控制可见输入和主操作；公共材质/Identity、烘焙参数、输出目录和状态不得因切换模式被清空。列表操作紧邻批量输入，主要按钮位于公共参数与输出之后，长说明、详细状态、插件管理和诊断默认折叠。该做法是历史 Odin 工具的兼容约定，不改变 PRJ-TOOL-04 对新窗口默认原生 `EditorWindow` 的要求。

两个批量烘焙器都应先构建计划，再统一确认覆盖并执行。Color Profile 单项输出 EXR；Cubemap Atlas 每项输出 EXR + JSON，使用临时文件完成整套结果后再提交。取消只在安全检查点生效；同步 GPU Readback、编码和资源刷新期间不会立即中断。完整实现与验证矩阵见 [Editor 批量烘焙工作区与安全取消参考](../../references/batch-baker-workspace-and-cancellation.md)。

### PRJ-TOOL-13｜AnimationClipPreviewer 分离目标、播放与恢复状态

当前 `AnimationClipPreviewer` 位于 `Assets/Editor/TA_Tools/Character/AnimationClipPreviewer/Editor/AnimationClipPreviewer.cs`，菜单为 `TA_Tools/Animation/动画预览`。它使用显式“读取当前选择”锁定一个或多个 Animator；Selection 改变不得自动刷新目标或退出预览。播放、暂停、停止到第 0 帧、取消预览和恢复 FBX A/T Pose 是五个不同动作，不能合并为含糊的“停止”。

该工具通过 `_ownsAnimationMode` 隔离其他动画工具，使用成对的 Editor Update 与 Animation Sampling，并在“取消预览”或窗口关闭时只退出自己拥有的 Animation Mode。“恢复 A/T Pose”属于带 Undo 的持久 Transform 写入，必须先退出临时预览，并从源模型 local Transform 按层级匹配，不能用 Bind Pose 推断冒充。完整实现、问题记录、维护清单和验证边界见 [AnimationClipPreviewer Profile](animation-clip-previewer.md)；跨项目方法见 [Unity 动画预览工具生命周期与姿态恢复参考](../../references/unity-animation-preview-lifecycle-and-pose-restoration.md)。

### PRJ-TOOL-14｜CharacterPrefabBuilder 使用模块快照保护重新生成外配置

当前 `CharacterPrefabBuilder` 位于 `Assets/Editor/TA_Tools/Character/CharacterPrefabBuilder/`，模块快照入口为 `TA_Tools/Character/Prefab模块快照与恢复`。基础 Prefab 的全量刷新可能删除人工组件和骨骼下新增对象，因此工具以独立快照保存非源 FBX 组件、对象引用和显式选择的 Hierarchy Object 原子子树，再通过稳定路径映射、Dry Run 和事务恢复写回目标 Prefab。

组件字段与对象引用必须分离迁移；完整 GameObject 子树不能拆成字段级恢复；Animator 即使命中源模型组件签名也要作为明确例外捕获；Magica Cloth 2 参数恢复后必须在目标拓扑上重建 initData，PreBuild 和拓扑不兼容在保存前阻断。当前两步 UI、双栏树、组件 Inspector、映射 Sub-Asset、恢复后实际层级、已解决问题、测试和限制见 [Character Prefab 模块快照与恢复 Profile](character-prefab-module-snapshot-and-restore.md)；跨项目方法见 [Prefab 模块快照、映射与事务恢复参考](../../references/prefab-module-snapshot-and-restore.md)。

### PRJ-TOOL-15｜OutsourceMiniProjectBuilder 按最小依赖闭包生成可验证 Mini 工程

当前 `OutsourceMiniProjectBuilder` 位于 `Assets/Editor/TA_Tools/Common/OutsourceMiniProjectBuilder/`，菜单为 `TA_Tools/Common/外包 Mini 工程生成`，基线 Unity 为 `2022.3.62f3`。它将输入分析、计划确认、临时生成、安全扫描和安装分开；场景、Prefab、RendererData、VolumeProfile 的剔除只作用于副本，更新已有输出先备份并支持失败恢复。

Runtime、Editor、RendererFeature、VolumeComponent、ShaderGUI/旧式 MaterialEditor 使用独立配置边界；ObjectField 自动通过 `AssetDatabase.GetAssetPath` 保存路径，列表不显示长绝对路径。内部明文模式和“强制生成”是独立开关，均不应掩盖必要输入或磁盘执行失败。完整字段语义、MMD DLL、Warning/Blocker 修正、默认 Profile 和验证边界见 [外包 Mini 工程生成工具 Profile](outsource-mini-project-builder.md)；可迁移流程见 [Unity Mini 工程导出与交付参考](../../references/outsource-mini-project-builder-delivery.md)。

### PRJ-TOOL-16｜AnimationBatchScreenshotTool 使用显式 Pose/短动画模式

当前 `AnimationBatchScreenshotTool` 位于 `Assets/Editor/TA_Tools/LYJ_Tool/Anima_type/AnimationScreenshotTool/AnimationScreenshotTool.cs`，菜单为 `TA_Tools/TA/Animation/动画批量截图`。它在兼容模式下保留全量 Clip 和手动采样帧；开启 `Pose / 短动画模式` 后，只处理静态 Pose 与不超过 `MaxShortClipFrames` 的短 Clip，静态 Pose 取第 0 帧，短 Clip 取中间帧。

该工具必须按 Clip 的 `frameRate` 换算时间，不得把全局 30 FPS 当成资源契约；FBX 资源必须用 `AssetDatabase.LoadAllAssetsAtPath()` 展开多个 Clip，并排除 `__preview__` 子资产。静态 Pose 通过 Editor 曲线稳定性识别，不能只按名称或时长猜测。实现、问题根因、验证证据和未验证项见 [AnimationBatchScreenshotTool Profile](animation-batch-screenshot-tool.md)；可迁移采样与排查方法见 [动画批量截图与短 Clip 采样参考](../../references/animation-batch-screenshot-and-clip-sampling.md)。

### PRJ-TOOL-17｜Booth 动画资源按 Humanoid、动作证据和资源组整理

当前 `Assets/TA_Test/Anima/Booth/anim` 已按 `Motion(动态动作)` 和 `Pose(静态姿势)` 分类，并使用英文原标识加中文语义的双语命名。分类、预览、迁移、音频配对、审计清单、当前数量和未完成验证见 [动画资源编目与双语命名 Profile](animation-resource-catalog-and-bilingual-naming.md)；跨项目的实现与验证方法见 [Unity 动画资源编目、识别与安全迁移参考](../../references/animation-resource-catalog-and-safe-migration.md)。

处理该资源库时先以 Unity `AnimationClip.humanMotion` 确认 Humanoid 状态，再用 Motion 的 `15%/50%/85% × 0°/45°/90°` 或 Pose 三视图完成语义复核。动画、预览图、已确认配对音频与各自 `.meta` 以资源组为单位迁移；不能只搬 `.anim`，也不能凭文件名或帧差删除疑似非 Humanoid 资源。

### PRJ-TOOL-20｜通用模型导入规则采用预设源与 ProjectSettings 快照分离

当前模型导入规则位于 `Assets/GameScripts/Editor/ModelImportManualSettings.cs`、`ModelImportRulePreset.cs` 和 `ModelAutoImportSettingsPostprocessor.cs`，预设资产放在 `Assets/Editor/TA_Tools/Common/ModelImportPresets/`，入口为 `Project Settings > ProjectACG > Model Import`。它独立于 `TA_Tools > TA > Model > FBX 导入设置批处理`，不引用或调用该窗口。

规则按“手动目录排除 → `whitelist` Label → 最深目录 → 最具体文件名”解析；文件名规则可整页覆盖 `Model / Rig / Animation / Materials`，未覆盖页继承目录规则。导入回调只读 `ProjectSettings/ModelImportManualSettings.asset` 的快照；已有模型必须通过显式“应用并重新导入”或单目录重导入更新。完整实现、已解决的命名空间/深拷贝/测试锁问题和验证边界见 [通用模型导入规则 Profile](model-import-rule-pipeline.md)；跨项目方法见 [Unity 模型导入规则与编译快照参考](../../references/unity-model-import-rules-and-snapshot.md)。

### PRJ-TOOL-18｜CharacterPrefabBuilder 按 FBX 槽数同步材质并保留旧索引

当前 `CharacterPrefabBuilder` 的基础生成链以 FBX 实例 Renderer 的 `sharedMaterials.Length` 作为最终材质槽数量事实源，不能用外置 Mesh 的 `subMeshCount` 或分析窗口预览值代替实际写入。更新已有 Prefab 时，重叠索引的非空旧材质继续保留，新增槽从 FBX 对应索引补入，减少槽只截掉尾部；工具不自动创建 `.mat`，源槽为空或材质缺失必须显式暴露。

主 Renderer、`RefreshFromTemplate`、`PreserveExistingAdditive` 和 LOD1 创建旁路必须使用同一材质绑定策略，避免主模型修复而 LOD 仍被旧数组覆盖。外置 Mesh、临时 `ModelImporter` 预设、坐标重建、典型 `emotion` 两 SubMesh/一材质槽问题、实现方式、测试矩阵和未验证边界见 [CharacterPrefabBuilder 生成与材质槽同步 Profile](character-prefab-builder-generation-and-material-sync.md)；ModelImporter 预设字段、原值快照/恢复、双工程同步边界和验证证据见 [CharacterPrefabBuilder ModelImporter 预设 Profile](character-prefab-model-importer-preset.md)。

### PRJ-TOOL-21｜SVC 变体集合合并工具以单目标文件收敛多端采集结果

当前 `ShaderVariantCollectionMergeWindow` 位于 `Assets/Editor/TA_Tools/TA/ShaderVariantCollect/ShaderVariantCollectionMergeWindow.cs`（单文件，未单独创建 asmdef，随默认 `Assembly-CSharp-Editor` 编译，不进入 Player），菜单为 `TA_Tools/TA/SVC 变体集合合并`（priority 212），并额外提供 Project 右键 `TA_Tools/合并选中的 ShaderVariantCollection 到目标...`（priority 2201，带 `MenuItem` 校验函数，只在选中项含 SVC 或含 SVC 的文件夹时可用）。同目录还维护 `README_Art_ShaderVariantCollectionMerge.md`（美术使用流程）与 `README_Tech_ShaderVariantCollectionMerge.md`（技术说明），当前版本 `v1.1.0`。

工具支持三种合并范围（全部输入变体 / 指定 Shader 的全部变体 / 指定 Shader 的单条变体）和两种写入模式（覆盖目标 / 与目标去重并集）。之所以按“多输入 → 单目标”设计，是因为裁剪侧的 `ShaderStrippingProfile.ShaderVariantCollectionAllowListPath` 目前只接受单个 SVC 路径，多平台、多质量档的采集结果需要先合并成一份目标 SVC 再交给裁剪器，而不是为了多端收集去改裁剪器。输入 SVC 在任何模式下都不会被写入；目标允许同时作为输入，工具先读完所有输入再写目标。覆盖模式执行前二次确认，并在确认文案中给出目标原有的 Shader 数与变体数。

目标 SVC 旁的 YooAsset `ShaderVariantCollectionManifest` 同名 `.json` 是可选软依赖，通过反射调用，类型或方法缺失时只写 SVC。`Undo.RegisterCompleteObjectUndo` 对非 `ScriptableObject` 可能抛异常，工具降级为警告并继续；因此该工具的回滚路径以版本管理为主，不能对用户宣称“可撤销”。通用实现、去重键、变体选择器 UI 模式和无 Unity 宿主的验证方式见 [SVC 多来源合并工具参考](../../references/shader-variant-collection-merge-tool.md)；去重用的关键字签名与 PassType 边界以 Shader 模块为准，不在本 Profile 重定义。

版本与验证边界：`v1.1.0` 已将单条变体选择从原生 `EditorGUILayout.Popup` 改为「关键字搜索 + 排序 + 可点击列表 + 关键字快捷过滤」。已在源工程 `ProjectACG` 的 `feature/TA_Variant-` 分支完成 Roslyn 单文件编译（退出码 0）与菜单/命名静态检查，并已整体迁入 `ProjectACGMain3`（分支 `feature/TA_UIMap_WB_UICanvas`），两侧 `ShaderVariantCollectionMergeWindow.cs` 的 SHA256 一致。Unity 编辑器内的实际点击验证与覆盖/并集对拍仍未完成，交付时必须明示。

### PRJ-TOOL-22｜地表与网格工具集统一位于 `Assets/Editor/TA_Tools/Scene` 并保持纯 Editor

当前 `Terrain`（网格地形绘制、Terrain Mesh 转换器、地形 TextureArray 烘焙器、导出地形 RGBA 权重图、权重图程序化散布）、`MeshDecalTool`、`ReflectionProbePostBake`、`TerrainBlendBake`、`Foliage Renormalizer` 五组工具整体位于 `Assets/Editor/TA_Tools/Scene/` 下，属于 Editor-only 程序集，不进入 Player；使用时不要求在地形或模型上挂任何工具脚本。

地形/网格绘制数据从「对象上的组件」改为 Editor-only 数据资产 `Assets/Tools/MeshSurfaceToolkit/MeshSurfaceDatabase.asset`；工具根目录由 `MeshSurfaceToolkitPaths` 按脚本自身位置反查，只有 `FallbackRoot` 需要随工程改写。旧版 `Assets/Plugins/TA_Tools/Scene/MeshSurfaceToolkit` 的 Runtime 组件与新编辑器数据类同名同命名空间，已连同整个 `Assets/Plugins/TA_Tools` 目录树移出工程（工程无版本控制，先备份再移动）；受影响的场景与 Prefab 会显示 `Missing (Mono Script)`，需在 Unity 内手动清理。

`MeshDecalTool/Runtime/MeshDecal.cs` 与 `Foliage Renormalizer` 的 `MonoBehaviour` 目前只存在于编辑器；若存在 Player 运行时用途，必须把 Runtime 脚本移出 `Editor` 目录。完整目录、菜单、数据资产、体积裁剪与验证状态见 [网格地形与地表工具集 Profile](mesh-surface-terrain-toolset.md)；迁移方法见 [Unity Editor 工具集跨工程迁移与旧件下线参考](../../references/unity-editor-tool-cross-project-migration.md)。

### PRJ-TOOL-23｜顶点色绘制窗口提供通道显示与另存/覆盖保存方式

`CharacterVertexColorPainterWindow`（菜单 `TA_Tools/Character/Vertex Color Painter`，目录 `Assets/Editor/TA_Tools/Character/VertexColorPainter/`）提供两个通道控件：显示通道 `{"RGB","R","G","B","A"}`（默认 `RGB`，通过预览材质 `_VertexColorPainterPreviewMode` 单独查看通道）与绘制通道 `{"RGB","R","G","B","A"}`（默认 `A`）。保存方式为 `{"另存为新 Mesh","覆盖源 Mesh"}`：另存使用 `_VertexColor` 后缀并与源 Mesh 同目录，覆盖以 `MeshSurfaceEditorUtilities.IsEditableMeshAsset` 为门槛并在写入前校验顶点数与记录 Undo。

预览走专用隐藏材质与 Shader，不写正式材质。笔刷缩略图必须用 `ScaleMode.StretchToFill` 与固定尺寸（`BrushPreviewSize`）绘制；同窗口按比例缩放的预览仍保留 `ScaleMode.ScaleToFit`，两者不可统一。通道语义属于项目 Shader 约定，工具不自行推断。通用实现约定见 [Editor 笔刷与顶点色绘制工具 UI 参考](../../references/editor-brush-and-vertex-color-ui-patterns.md)。

### PRJ-TOOL-24｜Timeline 基础资产生成器分离制作预览与正式运行

当前 `UltimateSkillCameraTimelineGenerator` 位于 `Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator/`，菜单为 `TA_Tools/Animation/技能大招资产生成器`。它使用历史 Odin 窗口，顶部三个横排按钮切换“大招技能 / 剧情过场 / 抽卡角色表演”，以直接资源列表支持多个动作、多个镜头、道具、音效和特效；大招主操作区按“只生成｜对应组装”两列提供模块入口，并保留完整生成/组装入口。

三种制作类型都支持 Virtual Camera、直接驱动现有渲染 Camera、Prefab 自带实体 Camera。镜头只产出 AnimationClip 与纯层级 Prefab，不生成 Animator Controller 或镜头子 Timeline；主 Prefab 内一个无 Controller Animator 驱动共享镜头层级。所有镜头按列表顺序放在一条 `CameraAnimation`，每个 Clip 同时保存 Transform 与 `Camera.fieldOfView`，不再生成每镜头 FOV Track。Prop、FX 与 Cinemachine Shot 的 ExposedReference 必须绑定主 Prefab 内持久对象，并在 Prefab 保存后执行二次绑定与回读自检。

剧情/抽卡当前只保证 Unity Editor 制作预览：虚拟模式用 `EditorCameraPreview` 自动绑定有效 Camera/Brain，直接模式用 `DirectCameraSync` 且 `editorPreviewOnly=true`，实体模式直接启用 Prefab Camera；正式 Player 加载、绑定和播放由程序侧另行接入，不修改现有 Presentation 业务模块。大招既有运行时 FOV 同步保留。固定命名、目录、Track/层级、`Battle_CameraPos` FOV `50`、已解决问题和未验证边界见 [Timeline 基础资产生成器 Profile](ultimate-skill-timeline-generator.md)；跨项目方法见 [Timeline 资产生成、Prefab 绑定与增量组装参考](../../../TimelineDevelopment/references/timeline-asset-generation-and-prefab-binding.md) 与 [Timeline 镜头制作、Editor 预览与运行时边界参考](../../../TimelineDevelopment/references/timeline-camera-authoring-editor-preview-and-runtime-boundary.md)。

## 7. 项目交付检查

- 新工具路径、菜单和类名符合 PRJ-TOOL-01 至 PRJ-TOOL-03，或在 Tech README 记录历史例外。
- Editor 程序集不进入 Player；没有无理由新增构建、导入或运行时依赖。
- UI、执行、日志、资源引用和破坏性操作符合 PRJ-TOOL-04 至 PRJ-TOOL-06。
- Painter Color Profile 烘焙任务已读取 PRJ-TOOL-10 及其专项 Profile，并完成 Identity、灰阶/RGB、White Point、sRGB 和 Active/Override 验证。
- Unity-SP 参数/贴图/灯光预览任务已读取 PRJ-TOOL-11、PRJ-LINK-11 及其专项 Profile，并验证简易状态流、显式目标、独立实例、工程映射重验证、白名单、Linear Color、缺失通道、增量/全量回退、Commit/Discard、预览恢复和 Painter 运行时插件版本。
- 单个/批量烘焙工作区已按 PRJ-TOOL-12 验证模式切换、计划预检、覆盖、取消、失败汇总、临时文件和真实输出内容。
- AnimationClipPreviewer 任务已按 PRJ-TOOL-13 验证目标锁定、Selection 改变、播放/暂停/停止/取消、Animation Mode 冲突、窗口关闭和 FBX 姿态恢复。
- CharacterPrefabBuilder 快照/恢复任务已按 PRJ-TOOL-14 验证稳定路径、组件与引用、源 FBX 归属、Hierarchy Object 原子边界、Animator 例外、Magica 重建、Preview 不写盘、恢复后实际层级和旧快照重新捕获要求。
- OutsourceMiniProjectBuilder 任务已按 PRJ-TOOL-15 检查最小依赖闭包、脚本/RendererFeature/VolumeComponent 分界、内部/强制模式边界、临时副本清理、更新备份、报告和目标工程 Batch Validation 入口；Unity EditMode 与目标工程实测未完成时必须在交付中明示。
- AnimationBatchScreenshotTool 任务已按 PRJ-TOOL-16 检查兼容模式回退、Pose/短动画筛选、多个 FBX Clip 展开、按 Clip 帧率采样、覆盖输出和 Unity Editor 实际出图边界。
- Booth 动画资源分类、改名或迁移任务已按 PRJ-TOOL-17 检查 Humanoid 导入结果、预览图完整性、资源组成员、GUID、内部名称、路径冲突和代表性重新导入。
- 通用模型导入规则任务已按 PRJ-TOOL-20 检查预设/快照分离、目录/文件名优先级、白名单/手动排除、四页 `ModelImporter` 映射、显式重导入、Meta/GUID、与旧 FBX 批处理隔离及 Unity 实际导入边界。
- CharacterPrefabBuilder 的 Mesh/材质同步任务已按 PRJ-TOOL-18 检查 FBX 源数组、旧索引保留、新槽补入、尾部截断、空槽、Refresh/Additive、LOD1 旁路、外置 Mesh、导入设置恢复和实际 Prefab 保存/重开结果。
- SVC 变体集合合并任务已按 PRJ-TOOL-21 检查输入/目标资产路径与类型校验、三种合并范围、覆盖/并集模式、目标同时作为输入、同名 JSON 清单软依赖、Undo 降级边界、跨工程副本迁移一致性和 Unity 内实际点击验证是否完成。
- 地表/网格工具集任务已按 PRJ-TOOL-22 检查目录与菜单、数据资产路径、纯 Editor 边界、旧版 `Plugins/TA_Tools` 下线影响面、`Missing (Mono Script)` 清理和 README/Profile 同步；Unity 内实际打开与绘制验证未完成时必须明示。
- 顶点色绘制任务已按 PRJ-TOOL-23 检查显示通道与绘制通道的独立性、单通道预览、另存与覆盖两类写入结果、不可写 Mesh 的门槛、Undo 与预览生命周期。
- Timeline 基础资产生成器任务已按 PRJ-TOOL-24 检查三种制作类型、三种镜头模式、多个动作/镜头顺序、单轨 Transform/FOV、模块生成/组装作用域、原路径更新、Prefab 持久对象二次绑定、Prop/FX/Shot 引用、`Battle_CameraPos`、Editor-only Preview Track、Player 边界、旧产物清理及 Unity 内实际重开/播放验证边界。
- README 已新增/更新；最终回复包含入口、验证和限制。
