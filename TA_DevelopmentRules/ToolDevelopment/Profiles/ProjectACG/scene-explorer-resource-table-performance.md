---
name: projectacg-scene-explorer-resource-table-performance
description: ProjectACG Scene Explorer 资源页性能优化的源码事实、原因、缓存与虚拟列表实现、分支错误、工具链、验证证据和未验证边界。
---

# ProjectACG Scene Explorer 资源表格性能 Profile

> 类型：PROFILE；适用范围：ProjectACG 当前 `ModelExplorerWindow` 的 Materials/Textures 资源页。问题处理日期：2026-10-10；文档整理与源码复核日期：2026-10-20。
>
> 可迁移方法见 [Unity Editor 资源表格性能与缓存设计参考](../../references/unity-editor-resource-table-performance.md)；项目约定见 [ProjectACG TA 工具入口](README_Tech_ProjectACGTAToolsProfile.md)。本文件记录项目事实和证据，不替代源码旁的功能使用说明，也不将当前实现提升为跨项目 CORE。
>
> 验证结论：三个相关 partial 文件的离线编译和 2000 组可见范围算法检查已经通过。独立 Unity GUI/Importer 测试没有执行完成；实际场景帧率、滚动交互、导入及 Undo/Redo 仍待宿主验证。没有实测加速百分比。

## 1. 项目事实与现象

### 1.1 输入现象与维护范围

用户截图显示 `Scene Explorer` 的 `Textures` 页打开后很卡。页面使用“场景引用 / 已加载场景”，表内存在大量纹理，而右上角计数显示“结果：0”。截图可确认显示不一致与用户报告，不能据此判断实际纹理总量、CPU 占用或每帧耗时。

| 项目项 | 源码事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3` |
| 菜单 | `TA_Tools/Scene/Scene Explorer` |
| 窗口类型 | `internal sealed partial class ModelExplorerWindow : EditorWindow` |
| 文件位置 | `Assets/Editor/TA_Tools/Scene/` 下三个 `ModelExplorerWindow*.cs` |
| 程序集 | 项目生成的 `Assembly-CSharp-Editor.csproj` 包含三个文件；代码在 Editor 目录 |
| 页签 | `Lights`、`Models`、`Materials`、`Textures` |
| 本次改动 | 资源页共享的绘制、筛选、扫描缓存，以及资源刷新生命周期 |
| 行高 | `RowHeight = 23f`，表头占一行且位于滚动内容内 |
| 平台预览标识 | `Android`、`iPhone`、`Standalone`；界面显示 Android / iOS / PC |
| 资源来源 | `Scene` 与 `Project`；场景范围为已加载、当前、选择对象子树 |
| 当前改动影响 | Materials 与 Textures 共用表格虚拟化；Models 表格不在本次优化范围 |

本次优化没有修改贴图格式、Max Size、场景资源或渲染设置。只有用户在工具中显式编辑属性时，既有写入链才修改 Importer 或材质。Lights 页仍委托 Unity 内置 Light Explorer。

### 1.2 源码职责导航

下列路径相对 `Client/`；文档库不拥有源码，不复制完整工具实现。

| 文件 | 关键函数 | 职责 |
| --- | --- | --- |
| `Assets/Editor/TA_Tools/Scene/ModelExplorerWindow.cs` | `OnEnable`、`OnDisable`、`OnUndoRedo`、`OnHierarchyChange`、`OnProjectChange`、`OnFocus`、`OnSelectionChange`、`ProcessRefresh` | 生命周期、刷新请求、页签和原模型页 |
| `Assets/Editor/TA_Tools/Scene/ModelExplorerWindow.Resources.cs` | `DrawResourceExplorer`、`CountFilteredResources`、`EnsureFilteredResources`、`GetTextureData`、`MatchesTexture`、`RefreshResources`、`AddSceneMaterial` | 来源/筛选 UI、集合、元数据、场景扫描与共享解析 |
| `Assets/Editor/TA_Tools/Scene/ModelExplorerWindow.ResourceTable.cs` | `DrawResourceTable`、`GetVisibleResourceRows`、`DrawTextureRow`、`EnsureTexturePlatformPreviews`、`ApplyResourceBatch`、`ApplyTextureImports` | 可见行绘制、平台预览、行编辑与批量写入 |

维护时保留当前历史目录和菜单，不为了套新工具模板移动资产。当前类仍没有 namespace，三个 partial 文件仍直接共享窗口状态；这是现状，不是新工具结构建议。

## 2. 原因与修正路径

### 2.1 重绘中遍历、匹配和绘制全表

修正前，资源表格位于 `EditorGUILayout.ScrollViewScope` 内，通过 `foreach` 遍历所有资源，对每个匹配项完整调用行绘制。屏幕外的行仍创建控件并读取数据。

`MatchesTexture` 先取得资产路径，再无条件查询 `TextureImporter`，即使属性筛选为 `None` 也会执行。每行平台预览再读取 Android、iPhone、Standalone 的平台设置与默认设置，遇到 `Automatic` 继续解析格式。随着资源数增加，单次 GUI 事件的工作量随全表增长。

该结论来自当次源码路径检查，未使用 Unity Profiler 定量确认各 API 的耗时占比。可以确认重复工作存在，不能声称某个函数独占了多少毫秒。

修正路径：全表绘制 → 可见范围绘制；重复筛选 → dirty 驱动缓存；逐次解析平台信息 → 资源首次显示时解析并复用。

### 2.2 “结果：0”及相邻入口的悬空 else

修正前，计数和全选附近存在外层 `if`、`foreach`、内层 `if` 没有花括号的嵌套结构。C# 将 `else` 绑定到最近未配对的内层 `if`，导致纹理页不按预期走外层 `else`。同类结构也出现在材质批处理的筛选/已选目标选择中。

修正路径：明确外层块边界，计数直接返回筛选缓存数量，全选与批量目标消费同一份筛选列表。此问题属于控制流正确性，不能仅靠编译发现。

### 2.3 共享材质被重复解析纹理属性

每个 Renderer 的材质槽原本都会枚举 `GetTexturePropertyNames()` 并读取纹理，即使前面已经遇到同一材质。新增 `_materialTextures` 后，每轮扫描按材质 Instance ID 解析一次贴图列表；后续引用复用解析结果，同时继续累加 `_materialUses` 与 `_textureUses`。

去重减少的是属性枚举和读取，不跳过引用计数。当前 `Uses` 仍是沿扫描路径累加的引用次数，不能解释为唯一 Renderer 数；同一纹理绑定多个材质属性、重复材质槽或选择子树重叠都可能重复累计。

## 3. 当前实现方式与项目约束

### 3.1 可见行绘制

`DrawResourceTable` 先从缓存取得筛选数量，用一个 `GUILayoutUtility.GetRect` 预留整个视口；当前实现对这个表格区域的 Layout 事件提前返回，其他事件使用显式 Rect 绘制。

内容高度按 `(count + 1) * RowHeight` 计算；`+1` 是内部表头。视口高度扣除 `GUI.skin.horizontalScrollbar.fixedHeight` 后，先 clamp `_resourceScroll.y`，再计算：

```text
first = clamp(floor((scrollY - RowHeight) / RowHeight), 0, count)
end   = clamp(ceil((scrollY + viewportHeight - RowHeight) / RowHeight), first, count)
rowY  = (index + 1) * RowHeight
```

只对 `[first, end)` 里的资源调用 `DrawMaterialRow` 或 `DrawTextureRow`。完整内容高度保留滚动范围；条纹按结果索引奇偶计算；列宽调整和横向滚动继续存在；`GUI.EndScrollView()` 在 `finally` 中执行。

滚动位置变化时调用 `GUI.FocusControl(null)`，用于降低虚拟化后焦点落到错误行的风险。该措施不等于控件 ID、拖拽、弹出菜单和键盘操作均已验证，仍需 Unity 实操。

### 3.2 筛选缓存与统一结果源

| 字段/函数 | 当前行为 |
| --- | --- |
| `_materials`、`_textures` | 扫描所得的原始资源列表 |
| `_filteredMaterials`、`_filteredTextures` | 匹配当前条件的结果列表 |
| `_materialFilterDirty`、`_textureFilterDirty` | 控制下一次访问是否重建 |
| `CountFilteredResources` | 调用 Ensure 后直接返回对应列表 Count |
| `DrawResourceActions` 的全选 | 遍历完整筛选列表，将 Instance ID 放入选择集合 |
| `ApplyResourceBatch` 的 filtered 分支 | 使用同一筛选结果；材质目标复制为新列表，贴图目标去重为路径集合 |
| `_selectedMaterials`、`_selectedTextures` | 与显示行范围分离；筛选隐藏的选择仍可能保留 |

搜索变化和属性筛选变化会同时标记两个筛选缓存失效，属于简化实现。行内材质渲染队列、Instancing、Double Sided GI 变化只标记材质筛选失效；材质批处理后同样标记。

`MatchesTexture` 在无搜索且不按 Importer 布尔值筛选时避免查询 Importer；`Type`、`FilterMode` 和 `WrapMode` 使用纹理对象属性。当前搜索分支调用 `GetTextureData`，因此搜索首次遍历时仍会读取匹配检查所需资源的路径与 Importer；尚未拆成路径缓存和 Importer 缓存两层。

### 3.3 纹理元数据与平台预览

`_textureData` 按纹理 Instance ID 缓存 `TextureResourceData`：`Path`、`Importer`、`PlatformFormats` 和 `PlatformMaxSizes`。

`GetTextureData` 缓存路径与 Importer；空路径返回空 Importer。`EnsureTexturePlatformPreviews` 仅在 `PlatformFormats` 尚未初始化时读取三个平台，默认平台设置读取一次。最终 Format 与 Max Size 生成 `GUIContent` 数组供后续绘制复用。

| 数据 | 来源与刷新 |
| --- | --- |
| Format / Max Size / tooltip | 缓存，下一次资源刷新后重建 |
| Mipmaps / Readable / sRGB | 从缓存的 Importer 对象读取当前值 |
| Filter / Wrap | 从纹理对象读取；无 Importer 时禁用编辑 |
| Name / Type / 实际宽高 | 行绘制仍读取对象并格式化部分字符串 |
| Uses / 首个使用者 | 来自扫描字典 |

因此当前方案不是“每行所有数据完全缓存”，也不是“每次重绘零分配”。它主要移除了重复的平台设置解析和屏幕外行绘制。

平台覆盖存在时使用覆盖值，否则使用默认值；有效 Format 为 `Automatic` 时调用 `GetAutomaticFormat(platform)`。无 Importer 时平台列显示 `N/A`，Importer 开关不可编辑。

### 3.4 扫描与缓存失效

`RefreshResources()` 清除刷新请求、标记两个筛选缓存失效，清空纹理元数据、共享材质解析、资源集合、Uses 和 owner 字典，再扫描并排序。

| 触发来源 | 当前处理 | 需要理解的边界 |
| --- | --- | --- |
| 首次打开/刷新按钮/页签切换 | 直接调用 `RefreshResources` | 仍是同步扫描 |
| 资源来源/场景范围/未激活开关变化 | 直接刷新 | 项目来源会扩大工作量 |
| `OnHierarchyChange` | 标记请求 | 后续 `ProcessRefresh` 消费 |
| `OnProjectChange` | 标记请求并 Repaint | 通知不是任意字段编辑的全覆盖保证 |
| `OnFocus` | 标记请求 | 外部 Inspector 修改后的兜底，但可能重复全扫 |
| `OnUndoRedo` | 标记资源与模型请求并 Repaint | Importer 撤销后的实际资源结果待验证 |
| `OnSelectionChange` | 仅选择范围扫描模式下标记 | 普通定位只需 Repaint |
| `ProcessRefresh` | 请求存在且当前是 Materials/Textures 才执行 | 其他页签保留请求，不提前丢弃 |
| `ApplyTextureImports` 的 finally | 标记请求 | 出错也刷新，但不代表自动回滚 |

`OnEnable`/`OnDisable` 成对订阅和退订 `EditorApplication.update`、`Undo.undoRedoPerformed`。本次新增的是窗口局部的事件处理，没有添加全局 `AssetPostprocessor`。

### 3.5 扫描范围和统计语义

场景扫描从 `GetExplorerRoots()` 开始，遍历 Renderer 的 `sharedMaterials`、Terrain 的 `materialTemplate` 以及 TerrainLayer 的 diffuse/normal/mask 贴图。这里的“场景引用”不是所有 Unity 资源依赖的完整闭包，例如未通过这些路径发现的光照贴图、UI、独立脚本引用等不能自动假定已覆盖。

`Project` 来源在场景扫描后，继续扫描 `Assets` 内的 Material 与 Texture；Texture 使用 `LoadAllAssetsAtPath` 兼顾子资产。即使只查看其中一个资源页，也会收集材质和纹理两类数据。

保留的边界：选择根对象未对父子重叠进行归一化；排序仍逐次读取对象名；选择清理只剔除已不再是 Material/Texture 的对象，不保证选中项仍属于当前范围。后续若改变这些行为，需当作独立语义变更处理。

### 3.6 编辑与导入链

纹理行编辑通过 `QueueTextureImport`，批量纹理编辑通过捕获路径 HashSet、属性和值，再注册 `EditorApplication.delayCall`。`ApplyTextureImports` 执行时重新获取 Importer，跳过未变项，登记 Undo，设置字段并逐项 `SaveAndReimport()`，最后合并 Undo 组并请求刷新。

材质批量应用先取得目标快照，对 RenderQueue、EnableInstancing、DoubleSidedGI 做差异检查与 Undo，修改后 `SetDirty` 并标记筛选失效。

这些主要是原有写入链保留与结果源修正，不能描述为本次新做的完整批处理框架。当前没有逐项失败报告、取消、自动回滚或延迟回调统一注销；大批量重导入仍可能阻塞 Editor。

## 4. 证据、结论与调用路径

### 4.1 源码快照

2026-10-20 复核时，当前三个源码文件与 2026-10-10 临时验证工程保存的文件 SHA256 相同。当前仓库 HEAD 为 `e40901fcb452b6d320f5fcf00eb454e012fd8073`；该 HEAD 只标识复核基线，不代表本次文档工作产生提交。

| 文件简称 | SHA256 |
| --- | --- |
| `ModelExplorerWindow.cs` | `7BD0F48EA1DAAF3A4A814E0774AC0F1BB413F77AF80B687908FC8119A1CBBD3B` |
| `ModelExplorerWindow.Resources.cs` | `AB764D6E5860DE1C60AC847107C198EA5839FF36D2676E7E08DF6D0A59D0DD1C` |
| `ModelExplorerWindow.ResourceTable.cs` | `B90193D02D87E57EB1C4FA0991E81F64D2BEDAF55CDF149A23DF7AAFD562AEF0` |

以下命令在 ProjectACG 的 `Client` 目录执行，用于重新定位和核对，不写资源：

```powershell
rg -n 'GetVisibleResourceRows|EnsureTexturePlatformPreviews|EnsureFilteredResources|GetTextureData|AddSceneMaterial' Assets/Editor/TA_Tools/Scene -g 'ModelExplorerWindow*.cs'
git status --short -- Assets/Editor/TA_Tools/Scene
Get-FileHash Assets/Editor/TA_Tools/Scene/ModelExplorerWindow*.cs -Algorithm SHA256
```

### 4.2 Evidence → Finding → Path

| 证据 | 来源 | 可支持的结论 | 不能支持的结论 |
| --- | --- | --- | --- |
| `E-SE-01` | 用户截图与 2026-10-10 反馈 | Textures 卡顿被报告，表格与“结果：0”不一致 | 帧耗时、总资源数 |
| `E-SE-02` | 当次修改前源码读取记录 | 全表绘制、重复 Importer/平台读取及悬空 else 存在 | 各热点实际耗时比例 |
| `E-SE-03` | 上述三个源码快照与函数 | 可见行、筛选缓存、平台缓存、刷新请求和共享解析已落地 | GUI 和重导入行为已实测 |
| `E-SE-04` | 2026-10-10 Roslyn 编译退出码 0 | 三文件子集与所用 Unity API 引用兼容 | 全工程 Unity 编译成功 |
| `E-SE-05` | 2026-10-10 算法输出；2026-10-20 对保留程序集重跑 | 2000 组索引检查通过，超大 count 的绘制范围有界 | 创建并浏览了一百万张真实纹理 |
| `E-SE-06` | 独立 Unity 日志与工具审批结果 | 宿主测试停在许可证连接，权限启动重试未完成 | 测试成功或功能失败 |

`F-SE-01`：`E-SE-02` 与 `E-SE-03` 支持“重复处理与全表绘制已被局部重构”；实际性能收益仍是待测结论。

`F-SE-02`：`E-SE-02` 与 `E-SE-03` 支持“计数、全选和批量目标的分支结构已修正，并统一结果来源”；实际窗口交互待 `E-SE-06` 缺口补齐。

`P-SE-01`（旧调用路径）：资源页绘制 → 全表匹配 → 全行控件 → 每行三个平台解析。依据 `E-SE-02`。

`P-SE-02`（当前调用路径）：资源变更标记 → `RefreshResources` → 筛选 dirty → `EnsureFilteredResources` → Count / 全选 / 批量目标 / 可见范围 → `GetTextureData` → 首次 `EnsureTexturePlatformPreviews`。依据 `E-SE-03`。

### 4.3 当次工具与命令边界

离线编译使用本机 `.NET SDK 10.0.200` 的 Roslyn `csc.dll`，参数包括 `-target:library`、`-nostdlib+`、`-langversion:9`；引用 Unity `2022.3.62f3` 的 `NetStandard/ref/2.1.0`、`NetStandard/compat/2.1.0/shims/netstandard` 和 `Managed/UnityEngine` 中的程序集，输出到临时目录。

不能把该检查描述为 Unity 自带编译器或全项目 build。首次引用枚举包含不存在的 `Managed/UnityEditor` 目录，PowerShell 给出路径错误，但所需 UnityEditor 模块在 `Managed/UnityEngine` 内可用；后续移除错误路径后，对工具加验证脚本的编译退出码仍为 0。迁移时先检查真实安装布局，脚本将路径错误视为独立失败，不能只看最后一条命令退出码。完整方法引用 [离线编译校验参考](../../references/unity-editor-script-offline-compile-verification.md)。

历史验证目录是：

```text
C:/Users/admin/AppData/Local/Temp/SceneExplorerVerification_71aa4a5f3c8b45ab990855d56cee3554/
    SceneExplorer.dll
    SceneExplorerWithChecks.dll
    RangeChecks/Program.cs
    RangeChecks/RangeChecks.csproj
    Project/Assets/Editor/SceneExplorerVerification.cs
    unity.log
```

该临时目录在 2026-10-20 仍可读取，但不属于稳定交付路径，后续可能被系统清理。本次文档保留命令、测试范围和输出，不把临时 DLL 当作工程依赖。

保留目录存在且相同 SDK 可用时，可重跑历史算法检查：

```powershell
$verifyRoot = Join-Path $env:TEMP 'SceneExplorerVerification_71aa4a5f3c8b45ab990855d56cee3554'
dotnet run --no-build --project (Join-Path $verifyRoot 'RangeChecks/RangeChecks.csproj') -- (Join-Path $verifyRoot 'SceneExplorer.dll')
```

2026-10-20 重跑输出：

```text
PASS 2000 viewport cases: empty, large lists, partial rows and end-of-list.
PASS 1,000,000 rows / 550px viewport: 23 rows drawn.
```

这里 `rows drawn` 是测试脚本对 `end - first` 的输出措辞，实际只调用范围函数，没有构造百万资源或绘制百万行。测试用随机种子 `1234` 检查索引有界、所有相交行被包含、行数受视口限制；零高度视口没有额外断言结果必须为空。

### 4.4 独立 Unity 测试为何不能记为通过

临时工程只复制工具脚本并创建合成资源，预备验证 3000 行列表、缓存复用、滚到底部、全选、筛选后 clamp、Android override、Automatic、共享材质 Uses、重导入、Readable 编辑、Undo 和后台页签请求。

启动使用 `-batchmode -nographics -projectPath ... -executeMethod SceneExplorerVerification.Run -logFile ...`。日志停留于：

```text
[Licensing::Module] Trying to connect to existing licensing client channel...
[Licensing::IpcConnector] Connection to channel LicenseClient-admin refused
[Licensing::Module] Successfully launched the LicensingClient
```

本次验证创建的 Unity 进程已停止。扩大权限的启动先遇到自动审批服务连接失败，重试又超时，目标测试未执行；这些记录只说明环境阻塞，不是判断操作不安全，也不是工具运行失败。

测试脚本虽然已编译，但 GUI 使用固定坐标点击；后续还需确认 batchmode/nographics 下窗口与 GUI 事件是否支持预期行为。正式验收可改为 Editor Test Runner 和交互窗口；不能仅因为未来出现结果文件就跳过对夹具本身的核验。

## 5. 未验证项、候选改进与回退

### 5.1 当前仍存在的边界

| 项目 | 当前状态 | 后续处理方向 |
| --- | --- | --- |
| 真实场景性能 | 未采集修改前后 Profiler 数据 | 固定场景、窗口大小和条件做 A/B |
| 全项目首次扫描 | 仍同步 FindAssets 与加载所有相关资产 | 限定目录、只取必要类型、分帧与取消另立任务 |
| 焦点刷新 | `OnFocus` 请求全量扫描并清缓存 | 区分集合失效与元数据失效 |
| 搜索冷缓存 | 搜索会通过 GetTextureData 同时读 Importer | 独立缓存搜索用路径，需测收益 |
| 空结果布局 | 内容高度为一行表头，但提示画在下一行 | 小视口、滚动和裁剪实测后再修正 |
| GUI 交互 | Layout 跳过、焦点、弹出菜单、列拖拽未实测 | 目标 Unity 实际操作 |
| 失效覆盖 | Project/Hierarchy/Undo/Focus 是基础兜底 | 外部程序化字段修改、平台变化按需补充 |
| 对象失效 | 缓存以会话 ID 为键 | 删除/移动/重导入/重载时确认无陈旧对象 |
| 范围外选择 | 有效但不在当前扫描范围的选择可保留 | 明确产品语义后决定是否清理 |
| 统计口径 | Uses 按引用路径累加 | 不作为唯一使用对象数展示或推断 |
| 延迟导入 | 匿名 delayCall 未统一注销，重导入逐项同步 | 窗口关闭、互斥、取消、部分失败另行设计 |
| 扫描异常 | 清空旧数据后同步建立新数据 | 必要时改成成功后替换快照 |

这些是从当前代码和测试缺口提取的候选，本文没有实现它们。未完成真实复现和验证前，不升级为 CORE 或宣称所有场景已解决。

### 5.2 下一轮宿主验收清单

1. 等工程完成脚本编译，从现有菜单打开窗口；记录场景、纹理数、材质数和窗口尺寸。
2. 记录首次切入、静止重绘、滚动、搜索、焦点切回的耗时与分配，核对热点调用次数。
3. 检查计数、全选、隐藏选择、应用到已选和应用到筛选结果，特别覆盖 Materials 与 Textures 两条分支。
4. 对测试贴图验证三平台 override/default/Automatic；使用资产副本验证 Mipmaps、Readable、sRGB、Filter、Wrap。
5. 验证保存/重导入、Undo/Redo、项目变化、资源移动/删除和重新打开；核对 Inspector 与表格一致。
6. 测试一行/零行、末行、缩小窗口、拖列、弹出菜单、键盘焦点、父子重叠选择及临时纹理。
7. 将实测结果与未通过项追加到本 Profile 的证据区域，不用算法输出代替实际交互结论。

### 5.3 回退与维护

源码回退只针对本次资源表格、缓存和生命周期变化；不回退无关用户修改。对编辑器内已经应用的材质或 Importer 修改，依据已验证的 Undo 或版本管理单独恢复，撤回窗口代码不能恢复资源设置。

后续引入分帧扫描、独立服务类或全局通知前，先确认当前热点和行为需求。可以保留本次“筛选快照 + 按需预览 + 可见行”的结构，再逐项补充；不要为迁移而复制项目路径、菜单、平台配置和历史临时目录到新工程。
