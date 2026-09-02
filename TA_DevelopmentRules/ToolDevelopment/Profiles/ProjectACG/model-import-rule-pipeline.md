---
name: projectacg-model-import-rule-pipeline
description: ProjectACG 当前通用模型导入规则实现、入口、资产位置、优先级、验证证据与限制。
---

# ProjectACG 通用模型导入规则 Profile

> 类型：PROFILE；适用范围：`D:\work2025U3D\Valkyria\ProjectACG\Client` 当前通用模型导入规则；不可直接迁移到其他项目，迁移前必须重新确认 Unity 版本、`TEngine.Editor` 基类、`ModelImporter` API、目录、菜单和现有导入处理器。

通用方法见 [Unity 模型导入规则与编译快照参考](../../references/unity-model-import-rules-and-snapshot.md)，模块规范见 [工具开发模块](../../README_Tech_TAToolDevelopmentRules.md)。本 Profile 明确记录本项目实际实现，不定义与 `TA_Tools > TA > Model > FBX 导入设置批处理` 的共享契约；两套功能保持独立。

## 1. 项目事实与入口

- Unity 基线：`2022.3.62f3`。
- Project Settings 入口：`Project Settings > ProjectACG > Model Import`。
- 配置方式对照：沿用 `Project Settings > ProjectACG > Texture Import` 的“预设资产编辑源 + `ProjectSettings` 编译快照 + 导入回调只读快照”分层；对应贴图实现为 `Assets/GameScripts/Editor/TextureImportManualSettings.cs`、`TextureImportRulePreset.cs`，预设目录为 `Assets/Editor/TA_Tools/Common/TextureImportPresets/`。
- 快捷菜单：`Tools > TA > Model Import Rules > Open Project Settings`、`Tools > TA > Model Import Rules > Apply Presets to Snapshot`。
- 预设目录：`Assets/Editor/TA_Tools/Common/ModelImportPresets/`。
- 默认预设：`Assets/Editor/TA_Tools/Common/ModelImportPresets/BaseModelImportPreset.asset`。
- 编译快照：`ProjectSettings/ModelImportManualSettings.asset`。
- 自动导入入口：`Assets/GameScripts/Editor/ModelAutoImportSettingsPostprocessor.cs` 的 `OnPreprocessModel()`。
- 规则与 UI：`Assets/GameScripts/Editor/ModelImportManualSettings.cs`；预设类型与 Inspector：`Assets/GameScripts/Editor/ModelImportRulePreset.cs`。
- EditMode 测试：`Assets/GameScripts/Editor/ModelImportManualSettingsTests.cs`。

当前默认预设是空规则，ProjectSettings 快照中的 `ManualImportRoots` 和 `DirectoryRules` 均为空；因此仅安装这套系统不会自动改变已有模型。

## 2. 当前实现方式

### 2.1 人工预设与快照分离

`ModelImportRulePreset` 是人工编辑源；`ModelImportManualSettings` 使用 `EditorScriptableSingleton` 保存当前工程的预设引用、快照哈希、手动导入目录和规范化目录规则。点击“应用预设到快照”后：

1. 确保默认预设引用存在。
2. 规范化路径和数值。
3. 按预设顺序合并规则；相同 Root 的后一个预设覆盖前一个，并记录 Warning。
4. 深拷贝目录规则和文件名规则。
5. 计算哈希并写入 `ProjectSettings/ModelImportManualSettings.asset`。

导入阶段只读取快照，不加载预设资产，也不执行 `SaveAndReimport()`，从而避免导入回调递归和预设写入竞争。

### 2.2 目录/文件名解析

当前解析顺序为：

1. `ManualImportRoots` 中的目录及其子目录直接跳过自动规则。
2. 资产带有大小写不敏感的 `whitelist` Label 时跳过。
3. 在启用的目录规则中选择匹配路径最长的 Root。
4. 在该目录规则的文件名规则中选择有效字符最多的匹配项；同长度保留数组中靠前者。
5. 文件名规则勾选的 `OverrideModel`、`OverrideRig`、`OverrideAnimation`、`OverrideMaterials` 页面整页替换目录页；未勾选页面继承目录页。

路径统一为正斜杠，目录匹配包含边界；文件名匹配不区分大小写，支持 `Exact`、`StartsWith`、`Contains`、`EndsWith`、`Glob`，可选择忽略扩展名。

### 2.3 导入设置映射

- `Model` 页映射 `globalScale`、单位/轴转换、BlendShape、Camera/Light、Hierarchy、Mesh Compression、Read/Write、法线/切线、UV、索引格式和网格校验等 `ModelImporter` 属性。
- `Rig` 页映射 `animationType`、`avatarSetup`、`sourceAvatar`、Skin Weights、骨骼优化和 `Optimize Game Objects`。
- `Animation` 页映射动画/约束导入、Bake、压缩、误差、自定义属性和常量缩放曲线。
- `Materials` 页映射材质导入模式、位置、命名和搜索策略；当导入模式为 `None` 时不继续写其他材质字段。

每个目录规则可只启用部分页面；默认值不会在未勾选页面覆盖 Unity 原设置。

### 2.4 重新导入边界

- “应用预设到快照”不触发重导入。
- “应用并重新导入”只针对快照中有效目录去重后执行。
- 规则内“重新导入此目录下的模型”使用 `AssetDatabase.FindAssets("t:Model", new[] { root })` 收集指定目录下的模型，按路径排序，并在 `StartAssetEditing` / `StopAssetEditing` 的 `try/finally` 中执行。
- 没有有效目录、目录不存在或目录下没有模型时只记录明确的跳过日志。

## 3. 已解决问题与经验

### 3.1 `EditorScriptableSingleton<>` 命名空间缺失

**现象**：Unity 工程编译报 `ModelImportManualSettings.cs(174,53): CS0246`，找不到 `EditorScriptableSingleton<>`；独立 harness 若引用不完整也可能复现。

**根因**：该基类属于项目的 `TEngine.Editor` 命名空间，新增文件只引用了 `UnityEditor`，未引用项目程序集命名空间。

**修正**：在源文件中加入 `using TEngine.Editor;`，并以项目实际 `TEngine.Editor.dll` 编译；不要在测试 harness 中伪造一个同名基类掩盖真实依赖。

**证据**：独立最小编译 harness 引用 Unity 2022.3.62f3 API、`TEngine.Editor` 和 NUnit 后通过，结果为 `0 warning / 0 error`；Unity 随后完成程序集重载。

### 3.2 `JsonUtility` 深拷贝不会自动保证 Unity 对象引用

**现象**：规则复制可以得到字段值，但 `Rig.SourceAvatar` 这类 `UnityEngine.Object` 引用存在丢失风险。

**根因**：`JsonUtility` 适合复制可序列化数据图，不应被当作 Unity 对象引用迁移协议。

**修正**：目录规则深拷贝后，显式把源/目标 `SourceAvatar` 引用恢复到编译快照；文件名规则的 Rig 页面同样处理。

**边界**：新增其他 Unity 对象引用时必须同步扩展复制和验证逻辑，不能仅因为 JSON 测试通过就认为引用稳定。

### 3.3 测试退出码不是 Unity 测试通过证据

**现象**：定向 Unity 命令返回退出码 `0`，但 `Temp/ModelImportTestResults.xml` 未生成。

**根因**：项目当时已被另一个 Unity 实例打开，日志显示 `Multiple Unity instances cannot open the same project`，进程在测试启动前退出。

**修正**：验证时同时检查测试 XML、日志中的测试摘要和项目锁；项目被占用时只报告“未执行”，不能把退出码当作通过。

### 3.4 全工程构建锁冲突不应归因于本功能

**现象**：并行的全工程 `dotnet build` 出现 `CS2012` 文件占用，涉及其他工具/包项目。

**处理**：使用只包含四个新增 Editor 脚本的隔离编译 harness 验证本功能；不结束所有 `dotnet` 进程，不覆盖用户构建，也不把无关中间文件锁误报成模型规则编译错误。

## 4. 风险、限制与回退

1. `AssetPostprocessor` 会影响所有匹配扩展名的模型导入；若项目新增其他模型处理器，必须重新确认字段覆盖顺序和禁用入口。
2. 当前默认预设为空，实际生产规则需要由使用者在预设资产中配置并点击“应用预设到快照”；只修改预设而未编译快照不会改变导入行为。
3. 已有模型不会因为规则配置自动改变，除非执行“应用并重新导入”或规则内的目录重导入。
4. `ModelImporter` API 和枚举依赖 Unity 版本；升级 Unity 后必须重新编译并抽样验证 Model/Rig/Animation/Materials 四页。
5. 批量重导入不是逐项 Undo 操作，回退依赖版本控制或导入前备份；不要把重新导入当成错误设置的恢复手段。
6. 当前 Unity EditMode 测试因项目锁未实际执行，仍需在单独占用项目的 Unity Test Runner 中运行 `GameLogic.EditorTools.ModelImportManualSettingsTests`。

## 5. 验证记录与维护清单

已完成：

- `Assembly-CSharp-Editor.csproj` 已包含四个新增脚本。
- 独立最小编译 harness：Unity 2022.3.62f3 API + `TEngine.Editor` + NUnit，`0 warning / 0 error`。
- 默认预设哈希与 `ProjectSettings/ModelImportManualSettings.asset` 的快照哈希一致。
- 新增脚本、预设目录、预设资产和快照的 `.meta` 配对完整；新增 GUID 在 `Assets` 内唯一。
- 模型规则源码未引用 `FbxImportSettingsBatchWindow`、`FBX 导入设置批处理` 或其他旧批处理窗口。
- 新增文件 whitespace 检查通过；工作区已有 Atlas、贴图、材质、场景和用户设置改动未被回退。

待在 Unity 独占项目实例中完成：

- 打开 `Project Settings > ProjectACG > Model Import`，确认预设 Inspector、目录规则和文件名规则 UI。
- 使用临时代表性模型验证新导入自动命中、白名单/手动目录跳过、四页设置实际落盘和单目录重导入。
- 运行 `ModelImportManualSettingsTests` 并确认 XML 测试结果。
- 检查与项目其他模型导入处理器的字段覆盖顺序，以及 BatchMode/CI 增量导入行为。

维护时优先保留：预设/快照分离、快照只读导入、路径边界、规则优先级、显式重导入和缺失快照保护；任何新增全局 Hook、默认规则、自动重导入或跨工具耦合都必须单独记录影响和回退。
