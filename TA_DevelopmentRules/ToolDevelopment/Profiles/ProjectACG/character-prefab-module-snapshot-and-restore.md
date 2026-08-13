---
name: projectacg-character-prefab-module-snapshot-and-restore-profile
description: ProjectACG CharacterPrefabBuilder 的 Prefab 模块快照、层级对象、引用重绑、Magica Cloth 2、Animator、双栏恢复 UI、已解决问题和验证边界 Profile。
---

# ProjectACG Character Prefab 模块快照与恢复 Profile

> 类型：PROFILE；适用范围：当前 ProjectACGMain 工作区的 `CharacterPrefabBuilder`；通用实现模式、排查表和验证矩阵见 [Prefab 模块快照、映射与事务恢复参考](../../references/prefab-module-snapshot-and-restore.md)。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 工具目录 | `Assets/Editor/TA_Tools/Character/CharacterPrefabBuilder/`。 |
| 生成器窗口 | `CharacterFbxPrefabBuilderWindow.cs`。 |
| 快照窗口 | `CharacterPrefabModuleSnapshotWindow.cs`。 |
| 菜单入口 | `TA_Tools/Character/Prefab模块快照与恢复`。 |
| 快照数据 | `CharacterPrefabModuleSnapshot.cs`，当前 Schema `2`，最小支持 Schema `1`。 |
| 映射数据 | `CharacterPrefabModuleMappingProfile.cs`，默认作为快照 `.asset` 的 Sub-Asset。 |
| 捕获服务 | `CharacterPrefabModuleCaptureService.cs`。 |
| 恢复服务 | `CharacterPrefabModuleRestoreService.cs`。 |
| 差异服务 | `CharacterPrefabModuleDiffUtility.cs`。 |
| 稳定路径 | `CharacterPrefabModulePathUtility.cs`。 |
| 双栏树 | `CharacterPrefabModuleHierarchyCompareView.cs`。 |
| 组件详情 | `CharacterPrefabModuleComponentDetailWindow.cs`。 |
| Editor 程序集 | `TA_Tools.CharacterPrefabBuilder.Editor`。 |
| 测试程序集 | `TA_Tools.CharacterPrefabBuilder.Editor.Tests`。 |
| 第三方专项 | `MagicaCloth2`，使用 `MagicaClothV2.dll` 与 Editor API。 |

源码旁的完整功能文档仍以以下文件为准，本 Profile 不替代它们：

- `README_Art_CharacterPrefabBuilder.md`
- `README_Tech_CharacterPrefabBuilder.md`
- `CharacterPrefabBuilder_ConfigGuide.md`

## 2. 生成与快照的职责边界

当前生成器有两种已有 Prefab 策略：

- `RefreshFromTemplate`：按模型/模板全量刷新，适合基础 `pre`；可能删除人工组件和层级。
- `PreserveExistingAdditive`：保留已有、补充缺失，适合 `cPre`；不等于完整迁移任意配置。

模块快照是独立保护层：在重新生成前保存旧 Prefab 的人工模块，生成完成后恢复到新结果或另一个兼容 Prefab。它不改变生成器两种模式的语义，也不把所有人工内容自动加入 Pipeline 配置。

当前项目维护时应遵循：

1. 生成器长期拥有 FBX 层级、Mesh、Renderer 和模板步骤。
2. 快照拥有非源 FBX 组件配置、项目脚本、第三方模块和显式选择的新增 GameObject 子树。
3. 长期固定、所有角色通用的组件优先进入 Pipeline 组件模板；角色特有或插件生成配置使用快照。
4. `RefreshFromTemplate` 保存到原 Prefab 路径，不主动删除 `.meta`；路径不变时 GUID 保持，但正式恢复仍无 Unity Undo。

## 3. 当前数据模型与身份规则

### 3.1 节点与组件身份

节点路径相对 Prefab 根，每段格式为：

```text
Uri.EscapeDataString(name)[sameNameOrdinal]
```

根节点使用空字符串。组件模块 ID 由源 Prefab GUID、挂载路径、组件 `AssemblyQualifiedName` 和同类型序号组成；Hierarchy Object 模块 ID 由源 Prefab GUID、固定类别和子树根稳定路径组成。

外部引用使用 GUID + local fileID；Prefab 内引用保存稳定节点路径、对象类型、组件类型和同类型序号。禁止改回按叶节点名称或 Unity InstanceID 恢复。

### 3.2 快照和映射同文件

“新建并捕获快照”默认打开源 Prefab 所在目录。一个 `CharacterPrefabModuleSnapshot` 可以按不同目标 Prefab 内嵌多个 `CharacterPrefabModuleMappingProfile` Sub-Asset：

- 切换目标时按目标对象引用自动选择对应映射。
- 同一目标复用并更新同一个 Sub-Asset。
- 旧版独立映射资产仍可读取；保存时复制到当前快照文件，不回写旧资产。
- 映射只保存源路径 → 目标路径，不保存字段级恢复选择。

调整映射存储时不得改变现有快照 Schema，也不得让 Project 窗口无法展开查看 Sub-Asset。

## 4. 当前捕获边界

### 4.1 普通组件

当前捕获不使用组件类型白名单。除了不能作为独立组件恢复的 `Transform/RectTransform`，所有未被识别为源 FBX 固有内容的可挂载组件都会进入快照并默认勾选，包括：

- Unity 内置组件。
- Collider、Rigidbody、Joint、Cloth 等物理组件。
- 项目业务脚本和团队工具脚本。
- 第三方/插件组件。
- Magica Cloth 与 Magica Collider。

普通字段由 `EditorJsonUtility` 保存；对象引用由 `SerializedObject` 单独采集。`m_Script`、`m_GameObject`、Prefab Source 等引擎所有权字段不会作为普通对象引用迁移。

### 4.2 源 FBX 归属识别

当前使用两级识别：

1. 仍有 Model Prefab 来源时，读取 `GetCorrespondingObjectFromSource`、`IsPartOfModelPrefab` 和 Added Component Override。
2. 生成器已经展开模型层级时，在角色同级 `model` 目录选择品质匹配且层级重合最高的 FBX，建立“稳定路径 + 组件类型 + 同类型序号”签名。

源 FBX/Prefab 固有节点在层级对象候选树中灰显不可选，但扫描会继续进入其子节点，以发现骨骼下新增的挂点、Prefab 或普通子树。

### 4.3 Animator 是明确例外

`Animator` 即使命中源模型组件签名，也必须进入快照。原因是基础 Prefab 重新生成可能移除根 Animator；若继续按源组件排除，快照无法恢复以下配置：

- `runtimeAnimatorController`
- `applyRootMotion`
- `updateMode`
- `cullingMode`
- 其他 Unity 序列化字段和资源引用

该例外位于 `CharacterPrefabModuleCaptureService.IsOriginalSourceModelComponent()`，维护时不得在没有替代迁移链路的情况下删除。

**旧快照不会自动补 Animator。** 2026-08-08 检查 `Assets/AssetRaw/character/hero/1002/prefab/pre_hero_1002_QLow_ModuleSnapshot.asset` 时，`Hierarchy` 中能看到 `UnityEngine.Animator`，但 `Modules` 中没有 Animator 记录。使用该快照前必须点击“更新当前快照”重新捕获。

### 4.4 Hierarchy Object 原子模块

层级对象模块用于保存不属于源 FBX 的：

- 骨骼下新增的嵌套 Prefab。
- 空挂点。
- Added GameObject。
- 完整普通 GameObject 子树。

模块保存整棵子树的节点状态、SiblingIndex、非 Transform 组件、字段和引用。父节点选中后，子节点选择归一化关闭；嵌套 Prefab 以实例根作为最小候选单元。

目标同稳定路径已有模块根时，Preview 标记 `WillReplace`，正式恢复整体替换该子树；映射到的父骨骼只是挂载点，不会被替换。如果目标同路径是嵌套源模型的固有节点，分析必须阻断。

当前不把 Hierarchy Object 拆成字段级恢复开关。该边界用于保护 Prefab 连接、兄弟顺序、内部引用和组件依赖。

## 5. 当前恢复事务

### 5.1 Analyze 与 Preview

`CharacterPrefabModuleRestoreService.Analyze()` 负责：

- 展开模块依赖。
- 解析完全匹配、手动映射和计划创建路径。
- 检查资源引用、组件引用、子树重叠和目标所有权冲突。
- 生成 Blocking/Warning 和恢复模块计划。

`Preview()` 在未保存的目标 Prefab 内容副本上执行恢复，不写磁盘。预计差异由 `CharacterPrefabModuleDiffUtility` 比较恢复前快照和 Preview 结果。

路径缺失时，同名唯一候选只显示建议，不自动采用；同名歧义保持 Blocking。用户在双栏树中左选源节点、右选目标节点建立映射，再重新生成预览。

### 5.2 Apply 与 Commit 顺序

正式执行顺序固定为：

1. 创建或替换全部层级对象。
2. 创建全部组件。
3. 覆盖全部组件 JSON。
4. 统一恢复对象引用。
5. 验证并重建 Magica Cloth。
6. 所有步骤成功后保存目标 Prefab。

这个顺序支持层级对象内部、普通模块之间和 Magica 模块之间互相引用。不得恢复为“创建一个组件后立即写完它所有引用”的单遍流程。

正式保存前弹出确认，明确目标路径、模块数、整体替换子树数、无 Undo 和 Magica 重建。失败、取消、超时或关闭窗口时只 Unload 内存副本，不保存目标 Prefab。

### 5.3 恢复后实际结果

Commit 成功后重新从 `AssetDatabase` 加载目标 Prefab，并再次采集层级和实际差异。双栏树右侧切换为“恢复后 Prefab 层级 + 已恢复模块”。

修改目标、模块选择或映射时，必须清空 `_postRestoreTargetHierarchy` 并回到新的恢复前预览；不能继续显示旧恢复结果。

## 6. Magica Cloth 2 专项

### 6.1 可迁移内容与重建

普通 BoneCloth、BoneSpring 和 MeshCloth 迁移参数与对象引用，但不直接复用旧 `serializeData2.initData`。恢复后：

1. 清空旧 initData。
2. 使用目标 Prefab 的骨骼、Renderer 和 Collider 引用。
3. 调用 `ClothEditorManager.RegisterComponent(cloth, GizmoType.Active, true)`。
4. 轮询 `GetResultCode` 与 `initData.HasData()`。
5. 所有 Cloth 成功后才 Commit。

窗口超时上限当前为 `180` 秒。取消、异常和超时均不保存目标。

### 6.2 拓扑签名与阻断

- BoneCloth/BoneSpring：签名包含 ClothType、rootBone 相对子树和 childCount；BoneSpring 还包含 collisionBones 的局部结构。
- MeshCloth：签名包含 sourceRenderer 顺序、Renderer 类型、Mesh vertexCount 和 subMeshCount。
- `preBuildData.UsePreBuild()` 为 true 时分析阶段直接阻断，避免覆盖共享 PreBuild ScriptableObject。
- 差异报告忽略 initData，因为它是重建产物，不是可移植配置。

骨骼根可以通过手动映射移动到新的父层级，但内部拓扑不兼容时不得强行恢复或保存。

## 7. 当前 Editor UI 契约

### 7.1 两步工作区

顶部只显示：

1. `保存 / 查看快照`
2. `层级与恢复模块`

旧 `Mapping/Preview` 枚举值仅保留窗口序列化兼容，绘制时重定向到第二页。不得重新增加独立的 Dry Run、手动映射或问题列表页面，除非先证明融合页无法承载目标任务。

### 7.2 快照页

“层级对象选择与快照预览”使用一棵 TreeView：

- 默认折叠并开启“只显示可保存对象”。
- 源固有节点灰显不可选。
- 行内显示对象类型 Icon、组件数和快照组件 Icon。
- Prefab、Mesh、Collider、脚本对象和普通 GameObject 使用不同图标。
- 支持搜索、重新扫描、全选可保存、清空、展开/折叠全部。
- 支持“列表高度跟随窗口”和 `180–800` 手动高度。

### 7.3 层级与恢复页

绘制顺序固定为：

1. 恢复目标和高级设置。
2. 默认折叠的 `按类型批量选择 / 高级视图`。
3. 其下默认展开的 `层级映射 + 恢复组件`。
4. 重新生成预览和 `恢复并保存 Prefab`。

快照、目标和模块齐备时自动排队生成首次预览，层级区始终绘制；不得依赖用户先展开高级列表。自动分析只在 `Idle/Captured` 状态进入，失败状态不能每帧无限重试。

双栏树左侧显示快照组件，右侧显示恢复组件和模块开关：

- 蓝框：快照组件。
- 绿框：本次已选恢复模块。
- 灰框：模块关闭。
- 完全匹配且没有已选恢复模块的 GameObject 灰显。
- 挂有已选恢复组件的匹配 GameObject 保持正常亮度；“匹配”状态文字仍可灰色。
- 搜索和“只显示差异/模块”保留必要父链。
- 双栏树有独立的自动高度与 `180–800` 手动高度，不与快照页共享状态。

独立的颜色图例、统计卡片、全量差异列表、手动映射 Foldout、问题列表和页面大标题已经移除。底层 Dry Run 与实际差异数据仍保留，不等于删除安全分析。

### 7.4 组件 Icon 详情

点击组件 Icon 打开 `CharacterPrefabModuleComponentDetailWindow`：

- 使用 inactive、`HideAndDontSave` 临时 GameObject 创建真实组件。
- 从快照 JSON 恢复字段。
- 外部引用加载真实 Asset；Prefab 内引用创建临时占位对象/组件。
- 使用组件真实 Unity Editor 绘制只读 Inspector。
- 不读取当前 Prefab 上的组件值，也不写回快照。
- 类型缺失、AddComponent 失败或自定义 Inspector 不兼容时回退到原始 JSON。
- `ExitGUIException` 必须继续抛出，不能当普通 Inspector 错误吞掉。

Hierarchy Object 根 Icon 显示整棵子树摘要，不提供字段级拆分。

## 8. 已解决问题与维护结论

| 问题 | 根因 | 当前解决方式 |
| --- | --- | --- |
| 重新生成无法保证 Magica/业务脚本完整保留 | 生成器更新策略不等于配置迁移 | 独立快照 → 分析 → 预览 → 事务恢复 |
| 快照无法传递骨骼下新增 Prefab/空挂点 | 旧模型只有组件模块 | 新增 Hierarchy Object 原子子树模块 |
| 源 FBX 内容与用户新增内容混在候选树 | 只按层级扫描，没有来源归属 | Model Prefab 来源 + 展开模型签名；源节点灰显但继续向下扫描 |
| 快照资产无法拖到映射方案字段 | 快照与映射是两个类型 | 映射作为快照 Sub-Asset，同文件按目标保存和自动选择 |
| 多套页面重复显示层级和模块 | 快照、映射、恢复各自维护列表 | 快照页一棵树；恢复页一棵双栏树；高级批量列表默认折叠 |
| 点击 Icon 只能看 JSON，不像 Inspector | 没有快照对象实例可交给 Unity Editor | 从快照重建临时组件并绘制真实只读 Inspector |
| Animator 无法迁移 | 被源 FBX 组件签名排除 | Animator 来源例外 + Controller/运行参数恢复测试 |
| 层级树只有打开高级列表后才出现 | 无 Plan 时直接 return，分析触发与模块 UI 耦合 | Foldout 始终绘制；输入齐备后独立排队首次分析 |
| 有恢复组件的匹配节点也变灰 | 灰显只判断 `Exact` | 加入已选恢复模块判断，行动节点保持正常亮度 |
| 恢复后层级结果无法查看 | UI 只保留恢复前计划 | 保存后重新采集实际层级并复用右侧双栏 |
| 恢复后修改选择仍显示旧结果 | 恢复后缓存没有随输入失效 | 模块、映射、目标变化时清空实际结果缓存 |
| 双栏列表高度不足 | 使用固定窗口比例，没有用户控制 | 独立自动高度 + 手动高度滑条 |

## 9. 当前限制与不适用边界

- 旧快照不会自动补齐 Animator 或其他新增捕获类型；必须重新捕获。
- Magica Cloth 使用 PreBuild 时当前阻断，不自动迁移共享 PreBuild 数据。
- 当前不单独重放嵌套 Prefab 的 Removed GameObject / Removed Component Override。
- Hierarchy Object 目标同路径内容会整体替换，不做自动三方合并。
- 插件类型移除、程序集改名、字段改名或对象引用资源丢失时可能阻断。
- GUID + local fileID 仅对当前工程或保留 `.meta` 的资产迁移可靠。
- 组件临时 Inspector 是显示能力，不证明该组件一定可正式迁移；正式结果仍由 Analyze/Apply 验证。
- 命令行编译不能替代 Unity 内实际窗口、Prefab 连接、Magica 重建和保存后重开验证。

## 10. 测试与验证证据

### 10.1 当前测试覆盖

`CharacterPrefabModuleSnapshotTests.cs` 当前包含以下代表性用例：

- 重名和特殊字符稳定路径。
- 层级 Icon 分类、双栏行状态、搜索父链和差异过滤。
- 默认快照目录。
- 同一目标映射 Sub-Asset 复用、同快照多目标映射。
- 普通字段、Prefab 内引用、外部资源引用和 Preview 不写盘。
- 根 Animator、Controller、Root Motion、Update Mode、Culling Mode。
- 源模型 Animator 仍可迁移。
- 展开 FBX 后排除 Renderer/MeshFilter 等源组件、保留新增脚本。
- 缺失父节点、手动映射、重复同名新节点序号。
- 自动依赖关闭后的缺失组件阻断。
- 完整层级对象子树、内部引用、目标替换和父子选择归一化。
- 嵌套 Prefab 连接、来源 GUID 丢失阻断。
- Magica Mesh/BoneSpring 拓扑不一致阻断。

### 10.2 2026-08-08 当前验证

- `dotnet build TA_Tools.CharacterPrefabBuilder.Editor.csproj -nologo --no-restore -p:BuildProjectReferences=false`：`0 warnings / 0 errors`。
- `dotnet build TA_Tools.CharacterPrefabBuilder.Editor.Tests.csproj -nologo --no-restore -p:BuildProjectReferences=false`：`0 warnings / 0 errors`。
- `git diff --check -- Assets/Editor/TA_Tools/Character/CharacterPrefabBuilder`：通过，仅有仓库行尾转换提示。
- 测试代码已编译，但本轮未启动新的 Unity BatchMode 执行 EditMode Test Runner。
- 本轮未在 Unity 中完成全部窗口视觉、组件自定义 Inspector、真实 Magica 重建和保存后重开矩阵。

独立 dotnet 编译需要目标 Unity 已生成的程序集依赖。当前环境曾将 `MagicaClothV2.dll`、`MagicaClothV2.Editor.dll`、`OutlineNormalSmoother.dll`、`Unity.Burst.dll`、`Unity.Collections.dll` 和 `Unity.Mathematics.dll` 临时复制到 `Temp/bin/Debug` 后编译，并在完成后删除；该操作只是本地 CLI 补充验证，不是工具运行依赖，也不得提交这些临时 DLL。

## 11. 项目维护检查单

- [ ] 重新生成与更新已有 Prefab 的职责没有和快照迁移混为一谈。
- [ ] 快照 Schema、组件身份、稳定路径和旧资产兼容未被破坏。
- [ ] 普通字段与对象引用继续分离保存和恢复。
- [ ] 源 FBX 组件排除不会误伤 Added Component；Animator 例外仍有测试。
- [ ] Hierarchy Object 继续以完整子树为原子模块，源固有节点不可选。
- [ ] 映射只处理源/目标路径，不隐式修改组件字段。
- [ ] Preview 不写盘；Apply 按对象 → 组件 → JSON → 引用 → Magica → Commit 执行。
- [ ] Magica 拓扑、PreBuild、超时、取消和失败不保存边界完整。
- [ ] 两步 UI、默认折叠、树搜索/父链、组件 Icon、Inspector 预览和高度控制符合当前契约。
- [ ] 有已选恢复组件的匹配 GameObject 不灰显；关闭模块后灰显恢复。
- [ ] 恢复后结果来自重新加载的磁盘 Prefab，输入变化会使缓存失效。
- [ ] Editor/Tests 程序集编译通过，并在 Unity 内完成代表性 Prefab 和交互验证。
- [ ] 源码旁 Art/Tech README 与配置指南同步更新。
