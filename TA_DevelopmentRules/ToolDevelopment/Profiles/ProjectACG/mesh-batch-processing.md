---
name: projectacg-mesh-batch-processing
description: ProjectACG Mesh 批处理开发案例，记录原实现结构、Unity API、导入策略、问题修正、测试证据及当前源码与验证缺口。
---

# ProjectACG Mesh 属性批处理开发案例

> 类型：PROFILE；整理日期：2026-10-20；开发/实测记录：2026-10-13；Unity 基线：2022.3.62f3。该文档是开发经验和证据案例，不替代源码旁的功能 README，不表示当前工作区已包含原交付工具。

**当前状态**：2026-10-20 在工作区 `feature/CharaOptimization`、HEAD `30125ad3f0` 检查时，原交付的三个 MeshBatch 脚本未在 `Assets` 中找到；原读写窗口仍有 YAML 文本替换实现。系统临时目录保存着处理器、测试副本和部分日志，不能据此推断工具仍可从当前 Unity 菜单打开，也不能推断缺失的原因。本轮只整理文档，没有恢复源码或处理游戏资源。

通用方法见 [Unity Mesh 属性批处理参考](../../references/unity-mesh-batch-processing.md)；规范来源见 [工具开发模块](../../README_Tech_TAToolDevelopmentRules.md)；当前项目目录/菜单/UI 约定见 [ProjectACG 工具 Profile](README_Tech_ProjectACGTAToolsProfile.md)。

## 1. 需求判断与已有能力

原需求是：检查是否已有批量设置 Mesh 读写、删除顶点属性的工具；例如删除 UV3；支持选中文件夹或选中 Mesh，已有则补全。截图里的 Position、Normal、Tangent、Color、UV 等属于顶点属性，而不是一组都能通过导入器布尔字段删除的设置。

| 已有实现/链路 | 当时发现的能力 | 对新需求的启发 |
| --- | --- | --- |
| `Assets/Editor/TA_Tools/Common/BatchReadWriteSwitcher/BatchReadWriteSwitcherWindow.cs` | 纹理、模型和原生 Mesh 读写，目录递归；独立 Mesh 通过替换文本中的 `m_IsReadable` 写入 | 保留旧入口，复用新执行服务；避免继续扩展字符串写资产 |
| `Assets/Editor/TA_Tools/LYJ_Tool/Model_type/FBXImportSettingsBatchTool/Editor/FbxImportSettingsBatchWindow.cs` | Model/Rig/Animation/Materials 页面级设置，Read/Write 属于 Model 页 | 页面批处理不能直接替代单属性的最小修改；默认页状态也要复核 |
| `Assets/Editor/UwaProjScan/OpenSrcFixer/BasicAssetFixers/MeshRuleFixers.cs` | 检查项驱动的读写、法线、切线修复 | 有局部能力，不等于统一通道面板 |
| `Assets/GameScripts/Editor/ModelImportManualSettings.cs` | 项目级模型导入规则与编译快照 | 手动读写设置可能被导入规则重新覆盖 |
| `Assets/GameScripts/Editor/ModelAutoImportSettingsPostprocessor.cs` | `OnPreprocessModel` 调用项目规则 | 需要确认同字段写入顺序，提供解除显式覆盖的方式 |
| Outline Normal、FBXMeshExtractor、FBXEditor 等专项工具 | 特定数据生成、提取或源编辑 | 不能凭名称相近就当作通用属性清理工具 |

现有读写入口记录为 `TA_Tools/Common/资源读写批量开关` 和 `Assets/TA_Tools/批量切换贴图与Mesh读写`。这是项目菜单事实，迁移到其他工程时重新定位。

## 2. 实现来源与证据可信度

### 2.1 原交付组成与本轮核对

| 原交付内容 | 职责 | 2026-10-20 核对状态 |
| --- | --- | --- |
| `Assets/GameScripts/Editor/MeshBatchProcessor.cs` | 选集解析、批处理、策略、原生资产操作及导入后处理器 | 原路径缺失；临时 Unity 工程保留完整副本 |
| `Assets/GameScripts/Editor/MeshBatchProcessingWindow.cs` | Odin 窗口、预览、执行、恢复入口 | 原路径缺失；窗口行为依据本次开发交付记录，未找到可回读副本 |
| `Assets/GameScripts/Editor/MeshBatchProcessorTests.cs` | 四个 NUnit 回归 | 原路径缺失；临时 Unity 工程保留测试副本 |
| 三个 `.cs.meta` | Unity 脚本资产身份 | 原交付记录有生成；本轮未从原路径复核其内容 |
| 旧 `BatchReadWriteSwitcherWindow.cs` 的桥接修改 | 增加面板入口、模型读写委托、原生 Mesh 改对象序列化 | 原交付记录有改造；当前文件仍有文本替换，不能当作已保留 |

处理器和测试属于 `GameLogic.EditorTools`。原新增菜单记录为 `TA_Tools/TA/Model/Mesh 属性批处理`、`Assets/TA_Tools/Mesh 属性批处理`；旧窗口曾加入“Mesh 属性 / 通道清理”入口。以上是交付记录，不是当前安装清单。

### 2.2 可回读的本机证据

下列路径中的 `%TEMP%` 指当前用户的系统临时目录，可能被系统清理；路径和哈希用于辨认当时副本，不是跨机器依赖。

| 证据 | 路径 | 结论与边界 |
| --- | --- | --- |
| 处理器副本 | `%TEMP%/ProjectACG-MeshBatch-UnitySmoke/Assets/Editor/MeshBatchProcessor.cs` | 本轮已回读，支持第 3～7 节的实现描述 |
| NUnit 副本 | 同目录 `MeshBatchProcessorTests.cs` | 本轮已回读，证明用例存在，不能证明执行通过 |
| 首次失败日志 | `%TEMP%/ProjectACG-MeshBatch-UnitySmoke/Editor.log` | 第 763 行有 `MESH_BATCH_SMOKE_FAIL`，模型清理/读写断言失败 |
| 小样本成功日志 | 同目录 `Editor-second.log` | 第 328 行有 `MESH_BATCH_SMOKE_PASS`，只覆盖第 9 节的小样本范围 |
| 后续环境失败 | 同目录 `Editor-tests-sandbox.log` | Licensing IPC 拒绝连接并超时，退出码 199；不是四项回归通过或失败的证据 |
| 外部编译工程 | `%TEMP%/ProjectACG-MeshBatch-Compile/MeshBatchCompile.csproj` | 包含 Unity/Odin/NUnit 引用和原四个源码路径；这些路径现已部分失效 |

2026-10-20 计算的 SHA-256：

```text
MeshBatchProcessor.cs
E2D762AC6D05A59A9BCF0A428059C9A22EDB5112988807EBB72CA9C3519E21D6

MeshBatchProcessorTests.cs
640A54E7A332EA13150F1AA6C27C361EBBBB698337025B29E78EB19E06624DCA

Editor-second.log
2888DE93F5801C7CA829777F2F23DEB4C8194789C399116D4A4D1868F0EFDAB5
```

**证据配对限制**：当前临时 `MeshBatchSmoke.cs` 已追加四个测试调用和 `MESH_BATCH_SMOKE_PASS: 4 tests`，但已完成日志中只有更早的 `MESH_BATCH_SMOKE_PASS`。不能将修改后的测试脚本与旧日志配对，声称四个测试已运行。处理器副本哈希同样只标识本轮回读版本，不构成与旧通过日志完全同版本的证明。

## 3. 选集解析与操作粒度

### 3.1 数据结构与合并方式

`MeshBatchTarget` 包含 `Path`、`IsModel`、`AllMeshes`，以及使用 ordinal 比较的 `MeshNames` 集合和原生 `MeshAssets` 集合。外层以资产路径字典去重，输出按路径排序。

`ResolveTargets` 按输入处理：

| 输入 | 收集方式 | 作用范围 |
| --- | --- | --- |
| 指定目录或 Project 中选中的目录 | `AssetDatabase.FindAssets("t:Model")` 和 `"t:Mesh"`，限定目录 | 递归找到的路径中全部 Mesh；不额外遍历目录内每个 Prefab 的依赖 |
| 模型主 GameObject | 识别 `ModelImporter`，加载路径下 Mesh | 整模型 |
| 模型中的 Mesh 子资产 | 记录名称 | 该名称对应 Mesh 的通道；读写仍是整模型 |
| 原生 `.asset` / `.mesh` 中选中 Mesh | 记录对象 | 对应持久化对象 |
| Prefab 或 Hierarchy GameObject/Component | 遍历 inactive 在内的 `MeshFilter`、`SkinnedMeshRenderer`，取 `sharedMesh` | 它们引用的持久化 Mesh 资产 |
| Packages、空路径或非持久化对象 | 排除 | 不写入 |

同一路径的“整模型”选择覆盖子 Mesh 集合。`LoadAllAssetsAtPath` 保证模型主对象之外的 Mesh 可以被找到。仅处理 `Assets/` 下的模型和允许扩展名的原生 Mesh 容器。

### 3.2 名称身份的约束

模型重导入后对象会重建，策略选择使用 Mesh 名称持久化。`TargetNamesAreUnique` 检查当前资产内目标名称恰好出现一次；不存在或重名时中止该项通道设置，建议整模型处理。

导入后处理器再次统计实际渲染组件引用的 Mesh 名称；旧规则中的名称丢失或重复时警告并跳过该条。全模型掩码仍正常应用。此方案不支持可靠地区分同名 Mesh，也不会自动把旧名称迁移到新名称。

### 3.3 共享资产影响

Hierarchy/Prefab 入口处理 `sharedMesh` 指向的资产，没有为每个场景对象复制 Mesh。改动影响全部引用者。选一个子 Mesh 修改 Read/Write 时，设置的是整个 `ModelImporter`。这些都需要在预览和工具内确认中明确。

## 4. 操作与导入持久化策略

### 4.1 读写与删除的独立语义

| 操作 | 模型 | 原生 Mesh |
| --- | --- | --- |
| `Keep` | 不改本次读写意图，已有覆盖仍有效 | 保持原读写标志 |
| `Enable` / `Disable` | 写显式策略并设置导入器 | 设置资产序列化读写属性 |
| `UseProjectRules` | 把自身读写覆盖改为 `-1`，让项目导入链决定 | 没有对应模型规则，保持现状 |
| 通道复选 | 追加选中通道的删除策略 | 直接删除选中属性 |
| 清除模型通道规则 | 清除该模型所有通道策略并重导入，保留读写覆盖 | 不作为原生 Mesh 的恢复入口 |

清除通道按钮即使从子 Mesh 选择进入，也会清除整个源模型的通道策略；不是仅恢复所选名称。若源模型或其他处理器不能生成某个通道，清除本工具策略也不保证该通道出现。

### 4.2 持久化载体与合并

`MeshBatchImportPolicy` 的字段为：

```text
ReadWrite: -1（本工具不覆盖）/ 0（关闭）/ 1（开启）
AllMeshChannels: 全模型删除通道位掩码
NamedMeshes: [{ Name, Channels }, ...]
```

规则写入 `ModelImporter.userData` 的独立行：`ProjectACG.MeshBatch.v1:` 加单行 JSON。`WritePolicy` 只移除并替换该前缀行，保留其他行；策略全部为空时不保留自身标记。`ReadPolicy` 使用 `JsonUtility`，无规则时返回默认对象，非法 JSON 的已处理异常产生警告。

这里采用共享字段内的命名空间隔离，而不是整体覆盖 `userData`。当前实现没有完整的 schema 验证或 v1→后续版本迁移器；损坏字段、未知数值和异常 JSON 的更多情况仍需补测。

`MergeChannels` 使用按位 OR：整模型与按名称掩码都是累加删除。取消勾选只是不新增删除，不能恢复已有规则；精细编辑/单通道撤销没有包含在原实现内。

### 4.3 导入执行顺序

```text
用户执行
→ 备份旧 userData / isReadable
→ 合并自身策略，设置本次读写意图
→ SaveAndReimport
    → 项目 ModelAutoImportSettingsPostprocessor 读取编译快照
    → MeshBatchImportPostprocessor 处理显式读写覆盖
    → Unity 生成模型 Mesh
    → OnPostprocessModel 清理指定属性
→ 重新 LoadAllAssetsAtPath 校验
→ 记录结果；异常时尝试恢复原设置并再导入
```

`MeshBatchImportPostprocessor.GetPostprocessOrder()` 返回 `1000`，用于在已知默认顺序的项目快照处理之后执行自身覆盖。它只能表达与已知处理器的相对关系，不保证将来新增更晚处理器仍不会改写。

`OnPostprocessModel` 用 `HashSet<Mesh>` 去除多个 Renderer 对同一网格的重复引用，然后将全模型掩码与唯一名称规则取并集。无策略模型快速返回。回调不调用 `SaveAndReimport`，避免递归。

`VerifyModel` 对 Enable/Disable 检查导入器状态，对选中通道检查实际 Mesh 属性存在性；Keep/UseProjectRules 不强制某个最终读写布尔值。参数与存储一致时也先校验产物，不直接假定输出已经正确。

## 5. 通道操作与 UV 编号

### 5.1 编号与保护范围

| Inspector / SetUVs channel | 通道序号 | C# 常见属性 | 顶点属性 |
| --- | --- | --- | --- |
| `UV0` / `0` | 第 1 通道 | `mesh.uv` | `TexCoord0` |
| `UV1` / `1` | 第 2 通道 | `mesh.uv2` | `TexCoord1` |
| `UV2` / `2` | 第 3 通道 | `mesh.uv3` | `TexCoord2` |
| `UV3` / `3` | 第 4 通道 | `mesh.uv4` | `TexCoord3` |
| `UV4`～`UV7` | 第 5～8 通道 | `mesh.uv5`～`mesh.uv8` | `TexCoord4`～`TexCoord7` |

“删除 UV3”如果指脚本属性 `uv3`，实际是 Inspector 的 UV2；如果指 Inspector UV3，实际是第四通道。原窗口记录明确标注这一差别。

`MeshVertexChannels` 使用 Flags：Normal/Tangent/Color 占前三个位，UV0～UV7 占随后八个位。Position 不列入可删除项；骨权重、BlendShape、顶点数量和拓扑不提供删除开关。

### 5.2 本次采用的 API

以下是处理器副本中的核心调用说明，属于项目实现摘录，不是带有保存、Undo 或输入校验的独立可运行脚本：

```csharp
// 对选中的属性分别调用；执行前后用 HasVertexAttribute 检查。
mesh.normals = Array.Empty<Vector3>();
mesh.tangents = Array.Empty<Vector4>();
mesh.colors = Array.Empty<Color>();

// channel 使用 Inspector 的零起点编号；空列表表达删除。
mesh.SetUVs(channel, emptyVector4List);
```

`HasAnyChannel` 使用 `Mesh.HasVertexAttribute` 和 `VertexAttribute`，UV 通过 `TexCoord0 + channel` 映射。删除不等于把原数组填零。

没有先读取并重写全部 UV、颜色或构造新 Mesh；因此避免主动把 UV 高维数据降为 Vector2、把未选 Color32 全部转为 float Color。保留骨权重、BlendShape 和精度是设计目标；相关复杂数据回归没有完成，不能写成保证。

## 6. 原生 Mesh 的读写、备份和保存

### 6.1 替代文本替换

原旧实现按字符串替换 `m_IsReadable: 0/1`，依赖 YAML 形式。原交付改为 `SerializedObject(mesh).FindProperty("m_IsReadable")`，通过 `ApplyModifiedPropertiesWithoutUndo` 应用，由外层统一登记 Undo。

这个方式减少了整文件文本操作，但内部字段本身仍是版本耦合点。找不到属性，或应用后 `mesh.isReadable` 不符合预期时抛出异常。迁移 Unity 版本后需要重新验证，不能把字段名作为通用 API 契约。

不使用 `UploadMeshData(true)` 模拟可逆关闭：该 API 会释放 CPU 数据，不等于只改可读标志。已经释放数据的 Mesh 能否恢复不是本工具已经证明的能力。

### 6.2 单 Mesh 编辑路径

`ApplyNativeMeshes` 的逻辑：

1. 记录原读写状态，根据 Keep/Enable/Disable 计算目标状态；无需改动时跳过。
2. `Object.Instantiate(mesh)` 创建临时备份，并将备份名称还原为原名称，避免恢复时带入 Clone 后缀。
3. `Undo.RegisterCompleteObjectUndo` 登记目标对象。
4. 临时设置 readable 为 true，调用 `StripChannels`，检查所选属性不再存在。
5. 设置目标读写状态，`EditorUtility.SetDirty` 后 `AssetDatabase.SaveAssetIfDirty(mesh)`。
6. 异常时 `EditorUtility.CopySerialized(backup, mesh)` 恢复到原对象并尝试保存；finally 销毁临时备份。

保持原对象可以避免“失败恢复时换了 Mesh 对象”引起引用变化。局部保存避免无条件 `SaveAssets` 顺带提交其他 dirty 资产。以上是代码路径，原生 Mesh 的保存重载、读写往返和 Undo 仍需真实 Unity 完整复验。

## 7. 窗口、批处理与错误处理经验

本节窗口细节来自原交付记录，未在本轮找到窗口副本回读；处理器行为可由临时源码核对。

| 设计 | 实现记录/目的 | 边界 |
| --- | --- | --- |
| Odin 窗口与执行服务分开 | 窗口展示、服务处理资产 | 是当时实现，不是当前新工具框架规范；当前 Profile 默认原生 EditorWindow |
| 当前选择/指定文件夹两种输入 | 降低手输路径需求 | 文件夹递归处理 Mesh 资产，不等于依赖闭包处理 |
| 只读预览 | 总量、路径、范围、读写、已有规则；前 20 资源及每项部分 Mesh 属性摘要 | 限量展示不能省略总范围说明 |
| 预览失效机制 | Refresh/MarkStale，选择变化刷新；事件成对解绑 | 不在每次绘制中写资产或重导入 |
| 执行时重新解析 | 避免使用过期选择缓存 | 原实现没有完整输入指纹/执行计划持久化 |
| 工具内执行确认 | 明确共享资产、模型级读写和将删通道 | 不等于开发代理每一步都向用户重复索取权限 |
| 每资产异常隔离 | 失败项记错后继续后续项 | 不是整批原子事务 |
| 可取消进度 | 每个资产开始前检查，finally 清理进度条 | 已完成项保留，不自动全量撤回 |
| `MeshBatchResult` | ChangedModels、ChangedMeshAssets、Skipped、Errors、Cancelled、Messages | 模型与 Mesh 计数单位不同，且无总耗时字段，不能宣称实现了统一单位完整审计报告 |

`ApplyModel` 保存原 `userData`/读写值，导入或校验失败后尝试恢复；恢复失败另报错误。`ClearModelChannelRules` 则没有等量的旧值备份和导入后通道校验；`SetModelReadable` 的桥接路径也没有 `ApplyModel` 同级事务包装。复用时不要把主路径保障自动归给所有入口。

## 8. 问题、根因与解决方式

### 8.1 关闭读写导致导入清理被跳过

- **现象**：临时 OBJ 同时关闭读写并删除 Normal/UV0 后，通道仍存在，首轮 smoke 报错。
- **根因**：最初用 `mesh.isReadable` 判断是否跳过 `OnPostprocessModel` 清理，把运行时 CPU 访问标志当成导入期可编辑数据的前置条件。
- **修正**：移除该跳过条件，在导入后处理阶段直接对有规则的 Mesh 执行属性清理。
- **证据**：首轮 `Editor.log` 第 763 行失败；修正后 `Editor-second.log` 第 328 行通过。
- **边界/回退**：只证明本版本、小三角形模型的导入阶段行为；不能推广到任意时刻的不可读网格。关闭本工具策略并从源导入可停止清理，源数据本身仍需存在。

### 8.2 导入产物修改缺少持久化

- **问题**：单纯修改 FBX/OBJ 内 Mesh，无法作为重导入后持续有效的保存策略。
- **实现**：将显式策略写入单模型元数据，预处理应用读写，后处理删除通道；清理策略可单独解除。
- **证据**：处理器副本有策略编码与回调；小样本完成一次策略重导入。反复导入、规则清除恢复和项目规则接管的完整回归仍待执行。
- **风险**：同字段覆盖、后续处理器顺序、重名与源改名、元数据损坏均需独立覆盖。

### 8.3 原生 Mesh 使用文本替换的局限

- **问题**：旧窗口假设资源可按 YAML token 修改，二进制与复杂容器没有通用保证。
- **实现**：原交付将读取属性集中为序列化对象操作，外围负责备份、Undo 和局部保存。
- **证据**：临时处理器副本包含实现；本轮当前旧窗口仍可搜索到 `text.Replace`，说明该改造不能当作当前状态。
- **风险**：内部字段和原生数据恢复均需按版本实测，尤其是曾经主动释放 CPU 数据的对象。

### 8.4 外部编译的环境错误与代码错误区分

- **现象**：最初 netstandard2.1 临时工程 restore 遇到 NuGet SSL 问题；引用不全时出现 Unity 模块缺失。
- **处理**：用已安装 .NET 10 做临时类型检查，引用真实 Unity 2022.3 程序集、Odin、NUnit，并补齐 JSONSerializeModule、IMGUIModule。
- **证据**：原交付记录为最终 0 errors / 0 warnings；临时 csproj 仍可回读，但本轮未找到独立最终编译日志，也未复跑失效路径。
- **边界**：只作为历史外部编译记录，不能证明 Unity 项目全量编译或窗口正常。不要据此更改主工程目标框架，也不要把 NuGet 连通性当作源码缺陷。

### 8.5 新增回归被环境启动阻断

- **现象**：后续临时 Unity 进程连接 `LicenseClient-admin` 被拒绝，等待约 60 秒后 exit 199。
- **判断**：属于该次宿主启动失败，未执行出可用于判断四个回归的结果。
- **处理边界**：用户明确要求“先交付，剩余验证我在 Unity 中做”。本轮没有再启动 Unity，没有把新增测试写成通过。
- **经验**：交付状态分别写“代码存在”“已编译记录”“已执行通过”“未执行”；复用旧 PASS 前核对对应测试版本。

## 9. 验证结果与人工复验清单

### 9.1 实际完成与未完成

| 验证项 | 实际状态 | 可支持的结论 |
| --- | --- | --- |
| 外部 C# 编译：处理器、窗口、测试、旧窗口 | 原交付记录为 0 errors / 0 warnings；本轮未重跑 | 历史类型检查，不能替代 Unity 全工程编译 |
| 真实 Unity 临时 OBJ：Normal + UV0，读写关闭后策略重导入 | `Editor-second.log` 有 PASS | 该样本 Normal/UV0 消失，Position 存在、顶点数为 3，导入器读写为 false |
| 临时内存 Mesh：UV0 + UV3，只删除 UV3 | 同次 smoke 通过 | `TexCoord3` 消失、`TexCoord0` 保留；没有证明原生资产保存与重载 |
| 四个 NUnit 回归 | 已有代码与编译记录，未完成运行 | 不能声称模型反复导入/清除恢复、原生读写往返、蒙皮或 BlendShape 保留已通过 |
| Unity 工程完整编译、菜单、Odin 交互、Hierarchy/Prefab 操作 | 未完成完整验收；当前源码缺失 | 需定位或恢复代码后在 Unity 内验收 |
| 生产 FBX、实际 Shader、目标设备、CPU/GPU 内存收益 | 未验证 | 不承诺生产兼容与具体优化收益 |
| 原交付定向文本/空白检查 | 交付记录通过 | 全仓已有资源差异，不能宣称全仓 diff 检查通过 |
| 2026-10-20 本轮工作 | 源码副本、日志和当前状态只读复核，文档整理 | 没有运行批处理，没有修改游戏资源 |

成功 smoke 检查了 Position 与顶点数，但没有对导入模型索引做逐值比对；因此不能把它概括成“所有拓扑已验证”。

本轮按工程要求运行了 `Tools/Codex/check-projectacg-harness.ps1 -Scope Repo`，退出码为 1：现有配置工作区文件、技能路径与文档链接检查，以及 UI 监听/本地化门禁未通过。这是源工程的现有检查结果，不是 Mesh 功能测试结论；本轮没有修改这些文件。文档另行检查 UTF-8、相对链接、标题锚点、规则引用、索引和 `.meta` GUID。

### 9.2 四个测试的意图

| 用例名称 | 设计覆盖 | 尚需补强的地方 |
| --- | --- | --- |
| `ModelStripPersistsAcrossReimportAndCanRestoreFromSource` | 模型/子 Mesh 混选去重、再次导入、RW false、其他 userData 保留、清除规则、解除读写覆盖 | 无项目规则时解除覆盖不证明接管后的最终值；更多真实 FBX 与处理器冲突 |
| `NativeAsset_ReadWriteRoundTripAndSelectiveStripPreserveSkinAndBlendShapes` | 原生资产关 RW→Keep 清 UV3→重载→开 RW，UV0、颜色 UNorm8、骨权重、绑定姿态、BlendShape、三角形 | 现有断言多为存在、长度和部分值；还需逐值检查权重、BlendShape 多帧增量、UV 维度与复杂 SubMesh |
| `StripChannels_RemovesOnlySelectedAttributesAndKeepsTopology` | 内存 Mesh 删除 Normal/Color/UV3，保留 Position/Tangent/UV0，索引和顶点数 | 多格式、多 UV 维度、边界与索引格式 |
| `PolicyMarker_PreservesOtherUserDataAndCanBeRemoved` | 策略编码/解码、保留别人工具行、空策略移除标记 | CRLF、尾部空行、非法/旧版策略、重复 marker、schema 异常 |

### 9.3 在 Unity 中复验的顺序

1. 先定位或恢复确切的源码版本，确认三脚本及 `.meta`、旧入口桥接和程序集依赖；不要将本 Profile 当作当前代码自动存在的依据。
2. 在独立临时工程或非生产资产副本上，先完成 Unity 内编译与四个测试。记录具体版本、样本、日志与断言结果。
3. 用带蒙皮和多帧 BlendShape 的代表性模型，逐值检查权重、绑定姿态、形变帧、UV 格式与维度，再播放动画观察结果。
4. 测目录、重叠选择、单子 Mesh、重名、改名、共享引用、Packages、非持久化 Mesh、空输入与取消。
5. 测 Undo、保存重载、重复执行、重复导入、删除规则、解除读写覆盖、导入失败和回滚失败；检查其他工具 userData。
6. 对实际材质/Shader 验证光照、法线贴图、Lightmap、描边与自定义 UV/颜色语义，再检查运行时 CPU 访问链路。
7. 有优化结论需求时，再测目标设备构建的内存、包体和加载数据；仅凭属性消失不报百分比收益。

以上是复验计划，不是本轮已执行结果。

## 10. 工具链、迁移与维护决策

| 工具/API | 本次用途 | 复用边界 |
| --- | --- | --- |
| `rg`、`rg --files`、Git 定向状态与历史 | 查旧能力、回调、菜单和当前源码状态 | 不输出庞大全仓差异掩盖目标事实；不覆盖用户已有改动 |
| `AssetDatabase` | 查找目录、加载子资产、重导入、局部保存 | 避免在绘制或导入回调中全工程扫描/递归导入 |
| `SerializedObject`、`Undo`、`CopySerialized` | 原生 Mesh 标志、撤销和备份恢复 | 内部字段和恢复效果按 Unity 版本验证 |
| `HasVertexAttribute`、空数组与空 UV 列表 | 选择性删除和结果检测 | 不代替未选数据逐值、格式与消费者验证 |
| `ModelImporter.userData`、`JsonUtility`、`AssetPostprocessor` | 单资产策略和导入重放 | 自有 marker 隔离、版本迁移、顺序和限定范围 |
| Odin | 原窗口 UI 依赖 | 本案例事实；不构成新工具统一依赖要求 |
| .NET SDK + Unity DLL | 外部类型检查 | 不是 Player/.NET 配置方案，也不是 Unity 宿主验收 |
| Unity 临时工程、NUnit、Editor 日志 | 最小导入实证与回归 | 测试脚本、日志和代码版本配对；环境失败单列 |
| PowerShell 显式 UTF-8、文件哈希 | 中文文档写入与证据辨认 | 避免 native 管道隐式编码；哈希不等于功能正确 |

后续实现若要沿用此方案，优先确认当前工程规范与历史交付之间的差异：新工具默认目录、原生窗口偏好、README 要求和日志格式已在现行 Profile 中定义。历史的 `Assets/GameScripts/Editor`、Odin、静态服务和日志形式记录为案例事实，不自动成为新工具模板。

可继续改进但尚未实现/验证的内容包括：清除规则入口的事务与读回校验、桥接读写入口的同级回滚、策略 schema/迁移、逐子 Mesh/通道撤销、同名网格身份方案、统一单位和耗时报告、完整输入指纹、更多格式与变形数据逐值测试。应按实际需求选择，不为总结文档顺手重构工具。

## 11. 沉淀位置与回退边界

本轮将跨项目可借鉴的选择模型、来源分派、持久化策略、最小修改和验证方法放入 [REFERENCE](../../references/unity-mesh-batch-processing.md)，把路径、字段、菜单、版本、源码状态与实证留在本 Profile。没有新增同义 CORE 规则，也没有把未执行用例升级为规范事实。

恢复原工具属于后续独立开发任务：先确认可信源码与资产身份，再按当前 Profile 适配目录、窗口和文档。文档自身可通过版本或文件备份回退；模型规则回退、原生 Mesh 数据回退与整批撤销属于不同机制，不能互相替代。
