---
name: projectacg-mulligan-batch-asset-renamer-profile
description: ProjectACG 历史 MulliganRenamer 批量资源重命名工具的实现、扩展和验证 Profile。依赖当前目录、程序集、菜单和序列化格式，不可直接迁移到其他项目。
---

# ProjectACG Mulligan 批量资源重命名 Profile

> 类型：PROFILE；适用范围：当前 ProjectACG 的 `MulliganRenamer` 编辑器工具；通用设计、风险和验证方法见 [批量资源重命名工具设计参考](../../references/batch-asset-renaming-tool-design.md)。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 源码目录 | `Assets/Editor/TA_Tools/Common/MulliganRenamer/`。 |
| 菜单入口 | `TA_Tools/Common/批量资源重命名`。 |
| 窗口类 | `RedBlueGames.MulliganRenamer.MulliganRenamerWindow`。 |
| Editor 程序集 | `RBG.Mulligan`。 |
| 操作模型接口 | `IRenameOperation`：`HasErrors()`、`Rename(string, int)`、`Clone()`。 |
| 操作序列与预设 | `RenameOperationSequence<IRenameOperation>`，当前格式写入版本标签、类型字符串和 `JsonUtility` JSON；兼容读取层仍支持旧的本地化菜单路径。 |
| 本地化目录 | `Resources/MulliganLanguages/`；语言 JSON 的翻译项位于 `elements[].key/value`。 |

该工具属于历史工具扩展，不移动目录、菜单或程序集，也不手改 Unity 生成的 `.csproj`。新能力应在既有插件边界内接入，避免另起一个会与旧预设、预览和 Undo 分离的平行窗口。

## 2. 当前结构与职责

| 文件或区域 | 当前职责 | 扩展时的要求 |
| --- | --- | --- |
| `GUI/MulliganRenamerWindow.cs` | 窗口状态、Operation 原型、收集规则面板、预览和执行协调。 | 新操作必须在原型列表注册；新增面板必须重新计算下游 Rect。 |
| `Operations/*.cs` | 命名操作模型、纯字符串计算、序列化字段、Clone/比较语义。 | 不在 Operation 中访问 Selection/AssetDatabase；新增字段维持旧预设安全默认。 |
| `OperationDrawers/*.cs` | Operation 参数 UI 与错误提示。 | Drawer 仅编辑模型并遵从本地化，不执行资产写入。 |
| `Renaming/RenameOperationSequence.cs` | 顺序组合、预览、预设读写、旧格式操作映射。 | 新类型必须进入映射；不得改变既有 key、类型名或枚举语义。 |
| `Renaming/BulkRenamer.cs` | 按对象分类执行、冲突策略、临时名占位、Undo 和进度。 | 预览、冲突与执行必须使用同一列表顺序和策略。 |
| `Renaming/BulkRenamePreview.cs` | 批次内/已有资产/GameObject 同级冲突与阻断状态。 | 任何新警告先确定它是提示还是阻断，并同步按钮状态。 |
| `GUI/MulliganRenamerPreviewPanel.cs` | 空状态、拖拽、预览滚动、列布局和操作结果显示。 | 所有控件坐标基于传入 Rect 的原点，不能假设区域从窗口顶部开始。 |
| `Localization/*.cs` 与语言 JSON | 显示文案、英文回退和语言更新。 | 新 UI 文案用 key；主要语言 key 必须同步。 |

当前 `MulliganRenamerWindow` 已同时承担窗口生命周期、Operation 注册、收集 UI、排序/筛选和执行协调。后续若继续加入复杂范围管理、报告导出或大规模异步扫描，应按 `PRJ-TOOL-07` 把 UI、收集/校验和 Actions 拆分，不能持续扩大同一个窗口类。

## 3. 已实现能力清单

### 3.1 资源收集、排序和执行规则

收集面板支持：

- `DefaultAsset` 文件夹选择，校验 `AssetDatabase.IsValidFolder`。
- 递归或仅当前目录收集。
- `Texture`、`Material`、`Prefab`、`AnimationClip`、`Mesh`、`Shader`、`AudioClip`、`ScriptableObject`、`Other` 类型 Mask。
- 当前列表按自然名称、资产路径、资产类型或当前顺序排序，并支持降序。
- 编号全局连续、按文件夹重置或按资源类型重置。
- 冲突策略：跳过冲突、发现阻断问题时停止全部、自动追加 `_1` 递增编号。

收集流程使用 GUID → 资产路径 → 主资产的路径，并排除文件夹和重复路径。非递归模式比较规范化后的父目录；不要把 `FindAssets` 返回结果直接当成可写对象列表。

### 3.2 命名 Operation

原有操作继续保留，并在原有序列中增加以下能力：

| 分类 | 当前操作 | 实现边界 |
| --- | --- | --- |
| 指定删除 | `RemoveStringOperation` | 支持位置、出现次数和大小写控制；空查找串是错误配置。 |
| 边界删除 | `RemoveBoundaryStringOperation` | 只移除实际匹配的前缀/后缀，区别于按字符数删除。 |
| 截取与插入 | `ExtractSubstringOperation`、`InsertStringAtPositionOperation` | 支持明确的索引/模式，越界不应导致未处理异常。 |
| 分隔符与风格 | `NormalizeSeparatorsOperation`、`NamingStyleOperation`、`CleanupSuffixOperation` | 用于统一分隔符、大小写命名和常见副本后缀。 |
| 前后缀 | `EnsureAffixesOperation` | 只有缺少时才添加，避免重复前后缀。 |
| 多规则替换 | `MultiReplaceOperation` | 使用多行 `旧=>新` 映射；可选大小写敏感和仅替换首处；非法行阻断执行。 |
| 括号处理 | `ProcessBracketsOperation` | 覆盖圆/方/花/尖括号和常见全角形式；可删内容或只删括号。 |
| 名称清洗 | `SanitizeAssetNameOperation` | 规范全角、空白和非法字符，合并替换符，并处理 Windows 保留名。 |
| 长度限制 | `LimitNameLengthOperation` | 保留开头、末尾或首尾；中间连接符长度超过上限时有边界处理。 |
| 片段重排 | `ReorderNameTokensOperation` | 按分隔符拆分，可用负索引；`-1` 表示末尾片段；非法索引阻断执行。 |
| 编号位置 | `EnumerateOperation.InsertionIndexFromEnd` | 指定位置插入编号可从名称末尾计算；旧预设默认 `false`。 |

新增 Operation 的模型文件为：

- `Operations/AdvancedStringOperations.cs`
- `Operations/PracticalStringOperations.cs`

对应 Drawer 为：

- `OperationDrawers/AdvancedStringOperationDrawers.cs`
- `OperationDrawers/PracticalStringOperationDrawers.cs`

两者都必须同步注册到 `MulliganRenamerWindow.CacheRenameOperationPrototypes()`，并同步添加到 `RenameOperationSequence` 的旧格式映射。仅添加 Drawer 或仅添加序列化映射都不构成完整接入。

### 3.3 预览、冲突和真正写入

当前执行链路的关键事实：

- `BulkRenamer.GetBulkRenamePreview()` 先缓存同目录已有 Asset，再按当前对象列表和编号分组生成逐项预览。
- `BulkRenamePreview` 检查批次内最终路径冲突、已有 Asset 冲突和 GameObject 同级名称冲突；`HasBlockingIssues` 统一表达阻断状态。
- `AppendNumber` 策略先移除本批次原路径，再按稳定顺序预留最终名称；候选依次尝试 `_1`、`_2`。
- `RenameObjects()` 在 `StopAll` 有阻断问题时直接返回；其他策略中跳过带警告或路径碰撞项。
- Asset 之间存在名称交换/环形依赖时，`ApplyNameDeltas()` 先将冲突目标写为实例 ID 临时名，再写最终名称。
- GameObject 使用 Unity Undo；Asset 和 Sprite 使用工具现有的专用撤销记录与写回逻辑；执行中显示进度并清理进度条。

任何新冲突规则必须同时检查：预览标记、`HasBlockingIssues`、窗口 Rename 按钮禁用条件、`RenameObjects()` 执行短路，以及最终汇总。只改其中一个位置会造成“UI 可点但执行跳过”或“预览显示成功但执行失败”。

## 4. 本次开发中已确认的问题与修正

### 4.1 空列表提示覆盖资源收集面板

**现象**：新增“资源收集与规则”面板后，空列表的“拖动对象来重命名”提示覆盖了筛选和按钮区域。

**根因**：`MulliganRenamerPreviewPanel.DrawPreviewPanelContentsEmpty()` 用 `previewPanelRect.height` 计算居中 Y，忽略了预览区被上方面板下推后的 `previewPanelRect.y`。

**修正**：位置改为：

```csharp
labelRect.y = previewPanelRect.y + previewPanelRect.height / 2.0f - labelRect.height;
```

**经验**：窗口内新增面板时，要检查所有空状态、提示、Footer 和滚动内容是否以传入 Rect 的绝对 `x/y` 为坐标原点。不能只检查主列表。

### 4.2 移除商店支持横幅不能只隐藏绘制

**现象**：窗口底部显示 Mulligan 商店支持/评价横幅，用户要求完全移除。

**修正**：删除 `NeedsReview`、会话感谢状态、横幅绘制方法和按钮回调；同时删除横幅高度与 padding 的 Footer 预留。Rename 按钮现在从 `previewPanelRect.yMax + 16` 定位，Footer 固定为 60px。

**经验**：移除 UI 必须包括状态、绘制、布局预留和副作用入口。只让条件恒为 `false` 会留下不可见空白和死状态。

### 4.3 Unity 自动生成 csproj 与命令行验证不同步

**现象**：新增脚本后，`RBG.Mulligan.csproj` 曾未立即列出新文件，普通 `dotnet build` 不能证明所有新源文件都参与编译。

**修正**：以 Unity Bee 产出 `RBG.Mulligan.dll` 为最终编译证据；需要命令行补验时，用临时 MSBuild targets 注入缺失文件，构建后立刻删除。等待 Unity 刷新 `.csproj` 后再执行普通 build；从未手改或提交该生成文件。

**经验**：`dotnet build` 是补充验证。新增 Unity 脚本的真实编译覆盖必须结合生成 csproj 状态、Unity Editor 日志顺序和最终程序集更新时间判断。

### 4.4 本地化键检查误数根对象字段

**现象**：语言 JSON 顶层只有 `name/key/version/elements` 等字段，直接统计顶层会得到错误的“4 个键”。

**修正**：解析 `elements[].key`，检查重复键和主要语言集合差异。

**当前结果**：`en.json` 与 `zh_CN.json` 各 302 个翻译键且集合一致；`es.json`、`pt.json` 各 174 键，未覆盖键由现有英文回退承担。

**经验**：本地化验证必须按真实数据结构统计；既要检查 JSON 可解析，也要检查 key 唯一性、主语言对齐和回退路径。

### 4.5 编译错误记录需要按时间顺序判断

**现象**：`Editor.log` 中保留了较早阶段的 Mulligan C# 错误，单纯搜索 `error CS` 容易把历史失败误判为当前状态。

**修正**：比较最后一条相关错误与最终 `CopyFiles Library/ScriptAssemblies/RBG.Mulligan.dll` 的日志行序；最终 DLL 复制发生在错误之后才表示当前程序集已成功编译。

**经验**：持续运行的 Unity 日志是时间序列，不是“当前状态快照”。验证结论必须绑定本轮文件修改时间和最后一次目标程序集编译。

## 5. 维护和扩展检查单

新增或修改一个命名能力前，依次确认：

1. 读取 `IRenameOperation`、相邻 Operation、Drawer、预设序列化和当前本地化结构。
2. 冻结输入、输出、顺序、错误、默认值、克隆、等价和旧预设兼容语义。
3. 仅在 Operation 中实现纯字符串转换；Drawer 不写 Asset；窗口不重复转换算法。
4. 注册 Window 原型和 `RenameOperationSequence` 映射。
5. 为新 UI 文案补齐 `en.json`、`zh_CN.json`；次要语言保持英文回退或按产品要求补齐。
6. 添加 `.meta` 并确认 GUID 唯一。
7. 对正常场景、非法配置和边界长度执行纯逻辑测试。
8. 检查批次内、已有 Asset、同级 GameObject、自动编号和名称交换。
9. 以 Unity Editor 编译为主、dotnet 为补充；不要提交 `RBG.Mulligan.csproj`。
10. Unity 内人工验证 Drawer 布局、预设保存/加载、真实资源改名、Undo/Redo、窄 Dock 和 Domain Reload。

## 6. 当前验证证据与未验证项

### 已完成

- `RBG.Mulligan.csproj` 已使用 `dotnet build --no-restore -p:BuildProjectReferences=false` 编译通过，`0 warning / 0 error`。
- Unity Bee 已生成并复制 `Library/ScriptAssemblies/RBG.Mulligan.dll`；最终复制记录在最后一条相关 C# 错误之后。
- 新增的实用 Operation 完成了 14 个正常行为场景和 3 个非法输入场景验证。
- `en.json`/`zh_CN.json` 的 302 键集合完全一致、无重复键；其他语言 JSON 均可解析。
- 所有 Mulligan C# 文件均有 `.meta`；新增 Operation/Drawer 文档资产的 GUID 已检查。
- `git diff --check` 通过；仅存在仓库既有的 LF 到 CRLF 提示。
- 空状态重叠和商店横幅移除后的 `RBG.Mulligan` 编译均为 `0 warning / 0 error`。

### 尚未完成的 Unity 人工验证

- 在真实窗口中逐项点击新的 Drawer、检查中文/英文及窄 Dock 下的布局。
- 保存并重新加载每种新 Operation 组成的预设，验证旧预设兼容。
- 在隔离资源目录执行真实 Asset、Prefab、Sprite 和 GameObject 改名，检查 GUID、引用、Undo/Redo 与重新导入。
- 用大型真实目录检查目录扫描、预览、自动编号和冲突处理的耗时与内存。

在这些行为验证完成前，不应把“命令行编译成功”表述为所有 Unity 资产流程已完全验证。
