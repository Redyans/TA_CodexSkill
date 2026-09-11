# ProjectACG 外包 Mini 工程生成工具 Profile

> 层级：PROFILE；适用工程：`D:/work2025U3D/Valkyria/ProjectACGMain2/ProjectACG/Client`；基线 Unity：`2022.3.62f3`。本文件记录当前实现、配置语义和验证边界，不得提升为跨项目 CORE。

## 1. 项目事实与入口

工具目录为 `Assets/Editor/TA_Tools/Common/OutsourceMiniProjectBuilder/`，主要程序集为 `ProjectACG.OutsourceMiniProject.Editor`，测试程序集为 `ProjectACG.OutsourceMiniProject.Editor.Tests`。用户入口是 `TA_Tools/Common/外包 Mini 工程生成`。

当前目录包含：

- `Editor/OutsourceMiniProjectWindow.cs`：窗口状态、内部/外包模式、分析、生成和强制生成入口；
- `Editor/OutsourceExportProfile.cs` 与 `OutsourceExportProfileEditor.cs`：导出配置和 ObjectField Inspector；
- `Editor/OutsourceSceneAnalyzer.cs`：场景、脚本、Package、DLL、ShaderGUI 和依赖问题分析；
- `Editor/OutsourceSceneSnapshot.cs`、`OutsourceRendererDataSanitizer.cs`：临时副本和 RendererData/VolumeProfile 剔除；
- `Editor/OutsourceMiniProjectBuilder.cs`、`OutsourceMiniProjectBatch.cs`：临时生成、安装、更新、备份和 BatchMode；
- `Editor/OutsourceMiniProjectSecurityScanner.cs`、`ShaderSourceBundler.cs`：安全扫描和 Shader/Include 收集；
- `ShaderImporter/ExternalSdk/OutsourceBatchValidation.cs`：目标 Mini 工程 Batch Validation；
- `Tests/Editor/`：分析器、导出器、安全、环境契约和 Shader 保护链路测试。

工具 README 是实现细节的第一事实源：`Assets/Editor/TA_Tools/Common/OutsourceMiniProjectBuilder/README.md`。跨项目方法见 [Unity Mini 工程导出与交付参考](../../references/outsource-mini-project-builder-delivery.md)。

## 2. 当前配置模型

### 2.1 三套导出 Profile

当前配置资产为：

- `configuration/OutsourceExportProfile_Character.asset`：角色/完整视觉场景，保留角色渲染相关 Runtime 目录；
- `configuration/OutsourceExportProfile_Scene.asset`：场景视觉预览，默认不保留 Runtime 前缀，并剔除 `chara_renderSetting`；
- `configuration/OutsourceExportProfile_TA.asset`：TA 内部场景，保留更多渲染、后处理、Timeline 和角色调试 Runtime 目录，并配置内部原样依赖。

三套 Profile 当前默认目标平台为 `Android`，默认开启自定义 Shader 保护，目标平台可在窗口中覆盖并随预设保存。`_failOnVisualRuntimeScripts` 当前为 `true`，表示未配置的视觉 Runtime 脚本默认阻断，除非用户使用明确的剔除或强制模式。

### 2.2 字段语义

| 序列化字段 | 当前类型 | 用途与边界 |
| --- | --- | --- |
| `_allowedRuntimeScriptPrefixes` | `List<string>` | 允许交付的 Runtime 脚本目录/前缀；只复制真实需要的 `.cs/.asmdef/.asmref`，不等于复制整个程序集 |
| `_editorScriptSources` | `List<Object>` | 随 Mini 工程交付的 Editor 源码目录或单文件；只允许 `Editor` 路径段或明确 Editor 程序集 |
| `_excludedEditorScriptSources` | `List<Object>` | 从已选 Editor 父目录中剔除子目录/文件；排除项不在父范围内应阻断，避免配置看似生效但未实际过滤 |
| `_internalRawDependencySources` | `List<Object>` | Profile 配置的通用原样资源/目录，可包含 TA_Tools、Sirenix、DLL、UXML、图标等；内部/外部调用路径一致复制，不由历史模式开关绕过 |
| `_excludedSceneObjectPaths` | `List<string>` | 以完整 Hierarchy 路径从临时场景副本剔除对象及子节点；源场景不修改 |
| `_excludedSceneMonoBehaviourScripts` | `List<MonoScript>` | 从临时场景副本剔除指定 `MonoBehaviour` 组件；优先级高于 Runtime 保留列表 |
| `_excludedRendererFeatureScripts` | `List<MonoScript>` | 只接受 `ScriptableRendererFeature`，从 RendererData 副本剔除真实 Feature 引用 |
| `_excludedVolumeComponentScripts` | `List<MonoScript>` | 只接受 `VolumeComponent`，从 VolumeProfile 副本剔除真实组件和子资产 |
| `_allowedPackageRoots` | `List<string>` | 当前项目需要明确放行的非默认 Package 根；官方 Registry/Built-in Package 以目标 manifest 精确版本解析 |
| `_allowedShaderGuiRoots` | `List<string>` | ShaderGUI、MaterialPropertyDrawer 或旧式 MaterialEditor 的精确源码/DLL 根 |
| `_additionalAssets` | `List<Object>` | 场景未直接引用但任务明确需要交付的资产；仍走依赖、安全和脚本配置检查 |
| `_additionalProtectedShaders` | `List<Shader>` | 长期维护的额外自定义 Shader 保护列表，与窗口临时额外 Shader 合并去重 |
| `_editableRoots` | `List<string>` | 输出 manifest 的可编辑根；内部模式会扩展到 `Assets/`、`Packages/` 和 `ProjectSettings/` |

Inspector 对 Runtime、Editor、Package、ShaderGUI、额外资产和内部原样依赖使用 `ObjectField`。对象引用转路径由 `AssetDatabase.GetAssetPath` 完成；列表不显示冗长绝对路径，定位使用 Ping/选择器。

### 2.3 RendererFeature 与 VolumeComponent 的配置结论

当前实现已将渲染剔除拆为两个类型化列表。配置 RendererFeature 时拖入继承 `ScriptableRendererFeature` 的 `MonoScript`；配置 Volume 时拖入继承 `VolumeComponent` 的 `MonoScript`。Inspector 类型校验和分析器都会阻断类型放错。

旧 Profile 若仍把 VolumeComponent 放在历史渲染列表中，当前实现保持兼容但发出迁移警告。`ExcludedRenderScripts` 只是代码兼容视图，实际副本清理必须以两个类型化列表和 RendererData/VolumeProfile 的真实引用为准。

## 3. 已验证的实现方式与问题修正

### 3.1 内部模式与强制生成分开

历史窗口中的“内部模式/外部模式”字段只作为兼容数据读取，当前实现不再改变资源收集、Package/Sirenix 复制、脚本闭包校验、Shader 处理、manifest、可编辑目录或安全扫描。Shader 明文/加密只由 Profile 的 `ProtectCustomShaders` 决定；通用原样依赖在两种调用路径都复制。旧 manifest 中的 `internalMode` 固定写为 `false`，避免下游继续按旧语义分叉。

“强制生成（忽略全部可判定阻断）”仍是独立确认开关，只用于内部排查或临时产物。它会记录并忽略分析、环境、路径长度和安全扫描阻断，也会把步骤失败写入报告，但必要输入缺失、输出目录不可创建、磁盘拒绝写入、场景快照或 manifest 无法建立仍然失败；强制模式不能改变内部/外部统一导出契约。

### 3.2 脚本依赖按消费者而非程序集猜测

`AssetDatabase.GetDependencies` 能看到 Prefab 上挂载的 `MonoScript`，但 Shader 不会声明由哪个 RendererFeature 驱动。因此运行时脚本、Editor 脚本、RendererFeature 和 VolumeComponent 必须在各自入口显式配置。

选中脚本后，分析器使用 `CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)` 确认其 Player/Editor 边界，并检查脚本引用的项目类型是否也交付。属于已有 `AOT` 的脚本只额外携带原 asmdef 并在导出副本清理未交付 references；属于 `Assembly-CSharp` 的脚本仍由目标工程的 `Assembly-CSharp` 编译，主工程不新增拆分 asmdef。

已剔除的场景对象/组件先在临时快照删除，再判定 Prefab、RendererData、VolumeProfile、DLL 和脚本依赖。最终仍会复制的资产若引用未配置脚本，使用 `SELECTED_ASSET_SCRIPT_NOT_CONFIGURED` 或 `PRESERVED_ASSET_SCRIPT_NOT_CONFIGURED` 阻断；存在 Missing Script 的 Prefab 使用 `PREFAB_MISSING_SCRIPT` 阻断。

### 3.3 渲染剔除只改副本

RendererData 和 VolumeProfile 通过副本清理真实引用和子资产，源资产不修改。同一脚本如果还被 Prefab、场景外配置或其他原样复制资产引用，仍然阻断，避免目标工程出现 Missing Script。场景对象剔除按完整 Hierarchy 路径执行，路径不存在或同名歧义会阻止导出。

### 3.4 MMD4Mecanim 旧式 MaterialEditor 收敛

MMD4Mecanim 的材质 Inspector 位于 `MMD4MecanimEditor.dll`，继承旧式 `UnityEditor.MaterialEditor`，不是现代 `ShaderGUI`。当前 Profile 精确允许：

```text
Assets/Editor/TA_Tools/TA/MMD/MMD4Mecanim/Editor/MMD4MecanimEditor.dll
Assets/Editor/TA_Tools/TA/MMD/MMD4Mecanim/Scripts/MMD4Mecanim.dll
```

不因材质 Inspector 导出整个 `PMX2FBX`、原生 DLL、`MMD4MecanimImporter.cs` 或启动 Hook。对应测试 `ShaderGuiSourceCollector_CollectsLegacyMmdMaterialEditorDllsOnly` 已验证只收集两个托管 DLL。

### 3.5 缺失 CustomEditor 的等级修正

`SHADER_GUI_TYPE_MISSING` 当前为 Warning：当 Shader 声明的 CustomEditor 类型在主工程完全不存在时，继续导出并提示目标工程可能回退 Unity 默认材质 Inspector。若类型已找到但源码/DLL 未配置，仍视为配置不一致并阻断。测试 `ShaderGuiSourceCollector_MissingCustomEditorIsWarningOnly` 已覆盖 Warning 且无 Blocker 的行为。

### 3.6 阻断项修复交互

窗口问题列表按“全部/阻断/警告/提示”筛选。每个阻断项在定位入口下方同列提供“修复”按钮，节省长列表空间；点击后展开原因、影响、推荐步骤、Profile/资产定位和可复制方案。只有能控制修改范围的项目才提供自动修复；源 Prefab Missing Script 清理等不可逆操作必须二次确认，修复后重新分析。

## 4. 默认配置摘要

当前三个 Profile 都允许 `Packages/com.unity.render-pipelines.core@14.0.12`、`universal-config@14.0.10`、`universal@14.0.12`，角色和 TA Profile 另允许 `Assets/Packages/MagicaCloth2`。ShaderGUI 默认包含 `Assets/Plugins/CustomShaderGUI/` 以及上文两个 MMD DLL。

角色 Profile 的 Runtime 前缀包括 `Assets/Shader/RenderFeature/PerObjectShadowV2`、`Assets/GameScripts/AOT/GameArt/PerObjectShadow` 和 `CharacterRender`。TA Profile 另包含 `CharacterFaceDirection`、`CharacterMainlight`、`TimelineTrack`、`Assets/Renders/Graphics/Features`、`Assets/Shader/PostProcess` 和 `CharacterPlanarShadowController.cs`；其内部原样列表当前配置 TA 工具目录源和一个额外内部依赖源。场景 Profile 当前剔除 `chara_renderSetting`。

这些是当前配置事实，不代表所有任务都应照抄。新任务应从实际场景消费者和分析报告重新建立最小列表。

## 4.1 本轮排除契约（2026-09）

以下内容属于当前 ProjectACG Mini 工程的明确剔除要求。Profile 中的 Package 排除、Runtime 源码排除和 RendererFeature 排除是三类不同动作，不能只在 UI 中隐藏名称；分析、临时快照、复制、manifest 和安全扫描必须使用同一份归一化结果。

### Package 根目录：不复制、不写入目标 manifest

| 排除根 | 当前语义 |
| --- | --- |
| `Packages/com.xuanxuan.nb.fx` | 项目内本地 Package，当前任务不交付 |
| `Packages/appsflyer-unity-plugin` | 当前任务不交付；不得因历史平台兼容逻辑自动带入 |
| `Packages/com.emilia.kit` | 项目内本地 Package，当前任务不交付 |
| `Packages/UniTask` | 项目内本地 Package，当前任务不交付 |

如果保留源码实际引用了上述 Package，不能靠“剔除名单”掩盖编译依赖：应先剔除消费者、改用目标已有依赖或把问题保留为阻断。官方 Registry/Built-in Package 仍按实际程序集依赖写入精确版本，不能把“排除本地 Package”误解为“删除所有 Package”。

### Runtime 源码：从输出源码闭包中剔除

当前明确不交付的脚本为：

- `Assets/GameScripts/AOT/Common/CameraHotkeySwitcher.cs`
- `Assets/GameScripts/HotFix/GameLogic/Battle/CharacterMonsterEffect/CharacterDeath.cs`
- `Assets/GameScripts/HotFix/GameLogic/Battle/CharacterMonsterEffect/CharacterRedTint.cs`
- `Assets/GameScripts/HotFix/GameLogic/Battle/DamagePart.cs`
- `Assets/GameScripts/HotFix/GameLogic/Battle/FPawn.cs`
- `Assets/GameScripts/HotFix/GameLogic/Battle/FWeaponSocket.cs`
- `Assets/GameScripts/HotFix/GameLogic/Battle/PlayerState/PlayerCameraPostureConfig.cs`
- `Assets/GameScripts/HotFix/GameLogic/Battle/PlayerWeaponAimRigController.cs`
- `Assets/GameScripts/HotFix/GameLogic/Battle/Presentation/UltimateTimelineCameraTransformSync.cs`
- `Assets/GameScripts/HotFix/GameLogic/Battle/Weapon/WeaponManager.cs`
- `Assets/GameScripts/HotFix/GameLogic/Common/HallCharacterController.cs`
- `Assets/GameScripts/HotFix/GameLogic/Common/RoomCharacterController.cs`

源码文件不从主工程删除；只在临时场景快照和输出工程中省略。若剔除后仍有 Prefab、Scene、RendererData、VolumeProfile 或原样依赖资产引用这些类型，分析必须报告缺类阻断，而不是生成带 `Missing Script` 的工程。

### RendererFeature：从 RendererData 副本中剔除

`Assets/Shader/RenderFeature/UICorrectGamma/GammaUICompositeFeature.cs` 映射到 `ScriptableRendererFeature` 排除列表。清理动作只修改 RendererData 临时副本中的真实 Feature 引用和必要子资产；源 RendererData 不写回。Shader 名称或 Shader 源码不能用来反向推断 Feature 是否可删除。

## 4.2 必须随工程交付的依赖

- `Assets/Plugins/Sirenix` 是 Assets 插件而非 UPM Package。按插件根目录原样复制全部子目录、文件和目录/文件 `.meta`；内部/外部调用路径一致，不能只复制某个 Odin DLL。
- `Assets/Packages/MagicaCloth2` 是本次配置要求整根复制的项目内 Assets 目录，不应错误写成 UPM `file:` manifest 包。当前交付必须保留该目录全部子目录、asmdef、脚本、资源和 `.meta`；其 `Unity.Burst`、`Unity.Collections`、`Unity.Mathematics` 由目标 manifest 按实际版本解析。只有后续完成真实消费者审计和目标工程验证后，才允许把整根复制收紧为更小闭包。
- `Assets/Editor/Utils/DefaultMaterialShaderPolicy.cs` 是 Editor 源码闭包中的必交付文件。`ProjectACG.Editor.DefaultMaterialShaderPolicy` 被其他工具脚本引用时，缺少定义文件会在目标工程产生 `CS0246`；将其纳入最小 Editor 源码目录或通用原样依赖，不能仅复制引用方。
- 原样依赖交付必须保留文件、文件 `.meta`、目录 `.meta` 和全部子目录；复制后在 manifest/report 中记录来源、目标路径和哈希。

## 4.3 受保护 Shader、材质手工复制与 Package 导入

当前 `.pcgshader` 保护链保留源 Shader GUID，但 ScriptedImporter 生成的 Shader Local File ID 与明文 `.shader` 不同。Unity `2022.3.62f3` 曾观察到 Local File ID `-7482078289662181024`，该值只用于本版本诊断和测试；生产代码应通过 `AssetDatabase.TryGetGUIDAndLocalFileIdentifier` 读取实际值。

`ProtectedMaterialReferencePostprocessor` 的兼容边界为：

1. 监听 `.mat` 和 `.pcgshader` 的导入；无论先导入材质还是先导入受保护 Shader，都在延迟回调中重试。
2. 只改材质 YAML 的 `m_Shader.fileID`，不改材质 GUID、Shader GUID、属性、Keyword、RenderQueue 或其他序列化字段。
3. 手工复制 `.mat`、导入 `.unitypackage`、从 Package 导入材质以及 FBX `Extract Materials` 均复用这条 SDK 兼容链；不要求美术在 Inspector 中重新指定 Shader。
4. 同 GUID 的明文 `.shader` 与 `.pcgshader` 不能同时存在；追加独立 Shader 包时要删除/备份同 GUID 明文源，避免 Unity 选择错误资产。

独立重新导出加密 Shader 时，只需交付 `.pcgshader`、匹配 `.meta`、最小 `ProtectedShaderImporter`/Crypto DLL、实际使用的 ShaderGUI 和显式运行时消费者；不把密钥源码、无关 URP 源码或整个业务目录带出。

## 5. 验证状态与维护边界

已完成或观察到的验证：

- `ProjectACG.OutsourceMiniProject.Editor.csproj`：0 warning / 0 error；
- `ProjectACG.OutsourceMiniProject.Editor.Tests.csproj`：0 warning / 0 error；
- Unity 已重新编译相关 `Library/ScriptAssemblies`；
- 新增的 MMD 旧式 `MaterialEditor` 收敛测试和缺失 CustomEditor Warning 测试已写入测试程序集。
- 静态扫描确认当前三套 Profile 已配置 `Assets/Plugins/Sirenix`、`Assets/Packages/MagicaCloth2`、四个排除 Package 根和上述脚本/`GammaUICompositeFeature.cs` 排除项；`DefaultMaterialShaderPolicy.cs` 在分析器必交付 Editor 源码清单中。

当前未完成：Unity EditMode 测试尚未在本轮实际运行，因为已有 Unity 实例打开，无法再启动第二个实例；目标 Mini 工程的真实首次导入、Package Resolve、`OUTSOURCE_MINI_PROJECT_VALIDATION_OK`、材质手工拖入/`.unitypackage` 导入、Prefab/Scene 重开、材质绘制和 Android/Windows 双平台验证仍需在可用环境执行。静态编译通过不能替代这些验证。

维护时先更新工具 README 和本 Profile 的“验证状态”，再考虑把已在多个项目复现的模式提炼为 `references/` 或 CORE。不要把本 Profile 的路径、版本、GUID、字段、默认列表和当前缺口复制进通用规则。
