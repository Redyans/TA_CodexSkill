---
name: projectacg-submesh-splitter-cleanup-and-output
description: ProjectACG 子网格拆分工具的项目事实与开发复盘，记录清理开关、输出目录、场景结果、事务/Undo 修正、历史 33 项断言、复现材料和未验证边界。
---

# ProjectACG 子网格拆分工具开发 Profile

> 类型：PROFILE；事实核对日期：2026-10-20；历史隔离测试日志日期：2026-10-19；适用版本：本文记录的源码身份。
>
> 通用方法见[Unity 网格拆分、清理与撤销参考](../../references/unity-mesh-split-cleanup-and-undo.md)。本页保留项目约束、根因和证据；操作说明与完整局部实现文档继续以源码旁 Art/Tech README 为准，不在知识库复制。

## 1. 项目事实与来源

| 项目 | 当前事实 |
| --- | --- |
| Unity | `2022.3.62f3`，日志修订号 `96770f904ca7` |
| 客户端目录 | `D:/work2025U3D/Valkyria/ProjectACGMain2/ProjectACG/Client` |
| 代码 | `Assets/Editor/TA_Tools/LYJ_Tool/Model_type/SubmeshSplitter/SubmeshSplitter.cs` |
| 命名空间/窗口 | `TA_Tools.TA.SubmeshSplitter.SubmeshSplitterWindow` |
| 菜单 | `TA_Tools/TA/子网格拆分与合并` |
| 程序集 | 既有 Editor 工程程序集，没有为此次改动单独新增 asmdef |
| 功能说明 | 同目录 `README_Art_SubmeshSplitter.md`、`README_Tech_SubmeshSplitter.md` |
| 当前源码 SHA-256 | `D84899B5E7EC827C59ECD19DEFDE588D2AE40488C815C13E94EE64EC5989A1BF` |
| 本地历史定位 | `f44af80746`，2026-10-20 10:53:44 +0800，`--story=1008716@tapd-69352340 [TA支持] 场景性能优化处理工具上传` |
| 被测源码 | 临时隔离工程中的源码与当前源码 SHA-256 一致 |
| 历史结果 | `PASS: 33 assertions; static, skin, path, LOD, prefab, Undo/Redo, rollback.` |

本次知识整理核对了源码、源码旁 README、测试脚本、结果文件和日志；没有重新运行历史 33 项 Unity 自检。提交信息用于定位，不代替文件哈希或行为证据。当源码变更后，先重新核对版本，再复用本页结论。

源码和功能 README 的相对路径以客户端目录为基准。知识库与客户端位于不同磁盘，正文保留路径事实，不构造依赖本机目录布局的跨盘相对链接；知识库内部材料使用下方可解析的相对链接。

## 2. 需求到验收的项目决策

此次优化针对“拆分”的三个目标：可选清理源组件、保存独立 Mesh 到源目录或自定义目录、在场景中创建并定位结果。既有合并功能不因此扩展到静态与蒙皮混合合并。

| 需求 | 最终实现 | 保留的边界 |
| --- | --- | --- |
| 清理原 MeshRenderer | `_removeSourceRendererAfterSplit` 默认 `false`；成功后删源 Renderer，静态同时删 MeshFilter | 保留源 GameObject、Transform、其他脚本/Collider、已有子节点 |
| 自动保存目录 | 每个来源独立优先使用 Mesh 资源所在的 `Assets` 目录 | 无有效 Mesh 路径时继续查 Prefab，再查兜底 |
| 自定义路径 | `_useCustomSplitOutputFolder` 默认 `false`；`_splitOutputFolderAsset` 用 `DefaultAsset` 保存文件夹对象 | 仅支持工程 `Assets` 根或有效子目录；无效时拒绝执行 |
| 场景结果 | 创建子节点并绑定保存后的 Mesh；选中全部结果、Ping、可选聚焦 | 继承原显示状态，不强制启用原本关闭的来源 |
| 保持形变 | `_preserveBlendShapes` 默认 `true`；复用既有形变数据路径 | 数据与实际动画行为分别验收 |
| 聚焦结果 | `_frameSplitObjects` 默认 `true`；存在活动 SceneView 才 FrameSelected | 自动自检中关闭，不以其证明视觉效果 |
| 可撤销 | 一次拆分批次的场景变更合并为一个 Undo | 成功 Mesh 资产保留，保证 Redo 引用；资产人工清理 |
| 局部失败 | 每个 Renderer 独立回滚，已完成项保留，可继续后续项 | 取消结束批次，当前项回滚，已完成项保留 |

项目使用既有原生 `EditorWindow` 与中文轻量界面。此经验不要求将全部 TA 工具迁移到同一窗口框架，也没有新增 Runtime 组件、全局导入 Hook、构建回调或源 FBX 写回。

## 3. 实现导航与关键路径

### 3.1 以方法名定位代码职责

| 方法/类型 | 职责与维护关注点 |
| --- | --- |
| `DrawSplitSectionLocalized` | 拆分设置、目录选择、提示和执行入口 |
| `OnSelectionChange` | 刷新界面，执行时仍重新校验 Selection |
| `SplitSelectedObjects` | 收集、调度静态/蒙皮列表、批次 Undo、最终保存/刷新/定位及汇总 |
| `CollectSplitRenderers` | 场景输入、子节点范围、Renderer 身份去重、多 SubMesh 过滤 |
| `TrySplitRenderer` | 单 Renderer 完整性检查、资产写入、子节点、可选清理、事务补偿 |
| `CreateSplitObject` | 静态/蒙皮组件与状态复制；未注册 Undo 前失败时自清理 |
| `ReplaceSplitLodRenderers` | 在祖先 LODGroup 各 LOD 中替换原 Renderer 引用 |
| `ResolveSplitOutputFolder` / `TryGetSplitOutputFolder` | 自动目录优先级与显式目录校验 |
| `IsSplitAssetFolder` / `EnsureAssetFolderExists` | 有效文件夹判定与逐层创建 |
| `SanitizeAssetName` / `GetMaterialForSubmesh` | 文件名与材质槽策略 |
| `CopyCommonRendererSettings` / `CopySkinnedRendererSettings` | 显示、光照、排序与蒙皮设置复制 |
| `GetAllSubMeshAsIsolatedMeshes` / `MeshExtension.GetSubmesh` | 子网格提取与顶点映射 |
| `MeshSnapshot` | 读取源顶点通道、骨骼与 BlendShape Frame 数据 |
| `MeshIndexFormatUtility` | 按输出顶点数选择 UInt16/UInt32 |
| `MeshBuildData` | 既有合并输出路径；不要与拆分的通道契约混淆 |

方法名比固定行号更适合长期导航。源码目前是既有单文件结构；本次局部改造保留其组织，没有为文档整理额外重构。

### 3.2 收集、路径与持久化

`SplitSelectedObjects` 在编辑模式校验自定义目录，再读取 `Selection.gameObjects`。先通过 `HashSet<Renderer>` 收集为静态和蒙皮两个列表，之后才执行修改；同时选中父子不会重复拆分。Project 面板中的持久化 Prefab 资产被拒绝。只有 `subMeshCount > 1` 的 Mesh 进入候选。

自动路径为：

```text
自定义开启：有效的 _splitOutputFolderAsset
自定义关闭：源 Mesh 的 Assets 目录
          → 最近 Prefab 实例对应资源的 Assets 目录
          → _fallbackOutputFolder
```

当前兜底目录是 `Assets/TA_Test/LYJ_Tool/Model_type/SubmeshSplitter/Generated`。文件夹选择器通过 `FileUtil.GetProjectRelativePath` 转换；`IsSplitAssetFolder` 接受 `Assets` 根或 `Assets/` 内有效文件夹。包目录和工程外目录不是当前自定义资产输出范围。

资产名以 `<来源Renderer名>_Sub_<SubMesh索引>.asset` 为基础，清理非法文件名字符后用 `AssetDatabase.GenerateUniqueAssetPath` 取得路径，不覆盖旧文件。逐份 `CreateAsset` 后检查 `AssetDatabase.Contains(mesh)`，通过后才绑定到新场景对象。

本实现移除了拆分外层的 `StartAssetEditing/StopAssetEditing`，以满足同一执行过程中对持久化和引用的即时检查。它是当前正确性选择，不是跨工具禁止批编辑；没有记录此次改造的吞吐量基准，不能宣称提速。

### 3.3 单 Renderer 事务

1. 重新校验来源类型、Mesh 与目录，建立本项 Undo group。
2. 提取全部子 Mesh，要求数量等于来源 `subMeshCount`；不完整则拒绝本项。
3. 每个 SubMesh 的保存前检查取消；记录本项新资产路径，保存并校验，然后创建子节点和注册创建 Undo。
4. 全部结果就绪后，清理开启时先替换祖先 LOD 引用，再删除原 Renderer；静态来源随后删除原 MeshFilter。
5. 每次删除后检查原组件是否仍存在；Unity 若因依赖拒绝删除，则抛错进入本项补偿。
6. 标记结果所在场景 Dirty；将对象、目录和数量加入批次成功集合；最后设置 `completed = true`。
7. `finally` 中，未完成则 Flush 并 `RevertAllDownToGroup(rendererUndoGroup)`，删除本项新资产；所有未持久化 Mesh 都释放。

此处的完整性策略会拒绝包含空 SubMesh、无法得到等量输出的来源，不应表述为“任意合法 Mesh 均能拆分”。新建节点还未注册 Undo 就失败时，`CreateSplitObject` 自己销毁临时节点再抛出异常，避免事务没有记录到它。

已完成资产不随用户场景 Undo 删除。失败补偿只清理本项新资产；当前没有完整覆盖磁盘写满、DeleteAsset 自身失败、最终 SaveAssets/Refresh 异常等注入。

### 3.4 状态、LOD、Prefab 和骨骼数据

新节点位于源节点下，`SetParent(sourceRenderer.transform, false)` 保持局部单位变换；复制 layer、tag、activeSelf 和完整 Static Flags。复制通用 Renderer 的 enabled、阴影、Light/Reflection Probe、probeAnchor、遮挡、motion vectors、renderingLayerMask、priority、Lightmap index/ST 和 sorting 等设置。材质引用来自源 SubMesh 对应槽位。

静态结果为 `MeshFilter + MeshRenderer`。蒙皮结果为 `SkinnedMeshRenderer`，沿用 bones、rootBone、Mesh 内的 Bind Pose/权重、quality、updateWhenOffscreen、skinnedMotionVectors 等。开启 BlendShape 保留时按顶点映射复制 Shape 的全部 Frame/Delta，同步当前 Renderer 权重，并沿用来源 `localBounds`；关闭时使用拆分 Mesh bounds。

`ReplaceSplitLodRenderers` 只处理 `GetComponentsInParent<LODGroup>(true)` 返回的组，在所有 LOD 内把原引用替换为结果集合；保留其他 LOD 参数，并使用 `Undo.RecordObject`、`PrefabUtility.RecordPrefabInstancePropertyModifications`。它不搜索场景中的非祖先 LODGroup，更不扫描整个项目的任意脚本引用。

Prefab 实例的新增节点与移除组件形成 overrides；工具不自动 Unpack，也不 Apply 回源 Prefab。普通场景实例已自检；Model Prefab、嵌套、Variant、Prefab Mode 等复杂编辑情形没有在历史自检中完整覆盖。Animator/Timeline 对原 Renderer 或 BlendShape 路径的绑定需要另行重绑。

### 3.5 结果展示与一次 Undo 的最终顺序

`SplitSelectedObjects` 的最终顺序是：

```text
完成/取消逐项循环
→ AssetDatabase.SaveAssets
→ AssetDatabase.Refresh
→ Selection.objects = 全部成功新节点
→ Ping 第一个结果
→ 按开关和活动 SceneView 执行 FrameSelected
→ Repaint SceneView 和 Hierarchy
→ finally: FlushUndoRecordObjects
          CollapseUndoOperations(batchGroup)
          IncrementCurrentGroup
          ClearProgressBar、停止计时
```

汇总包含有效 Renderer 数、已生成 Mesh 数、失败数、耗时和输出目录。普通完成分支追加 warnings；取消分支当前不展示完整 warnings 明细，因此不能说所有退出状态都提供相同完整报告。

只标记场景 Dirty，不自动保存用户场景。原对象或父节点关闭、Renderer disabled 时继续保持原状态；创建、选中和聚焦不等于 GPU 已显示，真实材质、相机、Lightmap、LOD 和动画仍需人工复验。

### 3.6 与既有合并功能的关系

源码包含拆分、合并、逐 Renderer SubMesh 选择和 Proxy 输出。此次 33 项断言针对拆分增强，不是整套合并功能的完整回归。

`MeshSnapshot` 读取全部 UV 通道供拆分使用；既有 `MeshBuildData` 合并输出固定只保留 Unity UV0，即美术 UV1。不要把合并的 UV 裁剪策略误写为拆分策略。索引格式的既有分界为最终顶点数 `<= 65,535` 使用 `UInt16`，否则 `UInt32`；本次历史自检未覆盖极限顶点数。

当前 Mesh 路径使用经典 `boneWeights`；任意高级权重、自定义额外 Vertex Streams、MaterialPropertyBlock、全部 UV 维数与极端数据没有由这些断言证明。

## 4. 问题、根因与修正证据

| 项目现象/风险 | 根因 | 最终处理 | 证据与边界 |
| --- | --- | --- | --- |
| 源模型与拆分结果叠画 | 原 Renderer 留在原位，同时新增子 Renderer | 可选清理，全部结果就绪后再删 | 清理开关开/关断言通过；实际像素未测 |
| 清理后 LOD 失效 | LODGroup 仍引用已删除 Renderer | 删除前替换祖先组的全部命中位置 | 单项和混批 LOD/Undo 断言通过 |
| MeshFilter 删除被阻止 | `RequireComponent` 依赖导致 Unity 拒绝销毁，未必抛预期异常 | 检查组件存活并让当前项回滚 | 依赖组件故障注入通过 |
| 空 SubMesh 产生不完整结果 | 提取器没有得到与来源槽数相等的结果 | 完整性检查失败，保留来源 | `partial extraction preserves source` 通过 |
| 一次 Undo 没恢复来源 | 原先在主体循环结束时收束 Undo，随后 Save/Refresh/Selection 又产生后续记录 | 把最终 Flush/Collapse/Increment 放在刷新、选择和聚焦之后的 finally | 单项、LOD、Redo、混批一次 Undo 通过 |
| 测试启动早期 Undo 状态难归因 | `executeMethod` 时序与测试分组缺乏独立基线 | `delayCall` 后执行，先做最小 Undo 探针，再清空隔离工程历史 | `baseline Unity Undo works` 通过；并非把真实工具 bug 归因 batchmode |
| 内部 Undo 记录诊断调用不稳定 | `Undo.GetRecords` 有不同重载，参数签名不匹配 | 反射诊断按签名筛选；生产逻辑继续使用公开 API | 归档测试脚本仍有诊断反射，跨版本需维护 |
| 主工程 `dotnet build --no-restore` 无法验证 | `NETSDK1004`，缺少 `project.assets.json` | 转为独立参考程序集编译并补 Unity 内自检 | 不把依赖准备失败误报为代码错误 |
| 临时 SDK 工程 restore 失败 | `NETStandard.Library.Ref` 缺失及网络/源不可用，`NU1101/NU1801` | 使用实际 Unity Managed/NetStandard 引用与响应文件 | 独立编译通过；不能替代 Editor 行为 |
| 离线编译 `CS0433` | 聚合 UnityEditor DLL 与模块中的同名定义重复 | 按实际目录收敛引用，排除重复聚合引用 | 解决的是编译装置，不改生产业务代码 |
| 沙箱内 Unity 启动失败 | 许可证 IPC 通道访问失败，历史退出码 `199` | 对隔离 Temp 工程在允许的执行环境启动 | 成功日志完成 33 项；不是修改许可或绕过激活 |
| 仓库检查仍有失败 | 规则入口、skill 路径、文档链接和 UI 治理存在既有基线问题 | 区分局部工具通过与全仓结果，保留原报告 | 不能声称全仓 harness 通过 |
| 中文文档重编码风险 | PowerShell 与原生 stdin/stdout 编码不一致 | 显式 UTF-8 文件写入与读回，避免中文管道重写 | 属于交付工具链检查，不能替代功能测试 |

开发中使用的内部 API 诊断只是定位 Undo 边界的辅助，归档它是为了保留证据；迁移到其他 Unity 版本时可以去掉诊断块，但仍保留实际 Undo/Redo 状态断言。

## 5. 历史验证矩阵与证据

### 5.1 33 项断言覆盖

编号与归档日志中的 `CHECK PASS` 顺序对应。

| 编号 | 覆盖内容 | 能支持的结论 |
| --- | --- | --- |
| 1 | Unity 最小 Undo 探针 | 测试启动阶段可以完成基本 Undo |
| 2–8 | 静态清理、其他组件/已有子节点、父子去重、子 Renderer/材质、局部变换、Static Flags、源目录、LOD | 此最小静态样本的输出结构与声明状态正确 |
| 9–13 | 选择结果、一次 Undo 恢复组件/LOD、Redo 恢复拆分、Redo 后 Mesh 持久引用 | 单项场景撤销重做与资产保留策略成立 |
| 14–18 | 保留源组件、自定义目录子节点/输出、重复路径不覆盖、无效目录拒绝 | 两种输出模式和清理关闭路径成立 |
| 19–21 | 蒙皮清理、bones/rootBone、BlendShape 当前权重 | 简单单骨骼、一个 Shape 样本正确 |
| 22–24 | 普通 Prefab 实例清理、源 Prefab 不改、Undo | 普通 Prefab 场景实例边界成立 |
| 25–27 | 依赖阻止删除时恢复来源、清除当前项半成品、空 SubMesh 保留来源 | 已注入的失败路径能够补偿 |
| 28–33 | 混批保留前项成功、失败只回滚本项、继续蒙皮、LOD 引用、一次 Undo 及 LOD 恢复 | 局部失败与整批 Undo 在该混批样本中兼容 |

第 4 项原始名称为 `visible children and materials`，实际只检查结果 Renderer 数量、enabled 和材质引用，不是截图或 GPU 像素测试。第 21 项验证 Shape 存在和当前权重，不代表逐 Frame Delta 已做数值对照。

### 5.2 已归档的可复用材料

| 材料 | 内容及用途 |
| --- | --- |
| [SplitSmoke.cs.txt](evidence/submesh-splitter-20261019/SplitSmoke.cs.txt) | 原始通过版本的自检脚本；保存成文本以免被知识库工程编译；测试会清空其工程的场景/Undo 并重建测试目录，只在隔离工程运行 |
| [smoke-result.txt](evidence/submesh-splitter-20261019/smoke-result.txt) | 历史结果文件原文 |
| [smoke-assertions.log.txt](evidence/submesh-splitter-20261019/smoke-assertions.log.txt) | 历史日志的 33 个成功断言与最终标记，按原顺序提取；未复制机器/许可证信息 |
| [source-and-evidence-sha256.txt](evidence/submesh-splitter-20261019/source-and-evidence-sha256.txt) | 当前/被测源身份及归档证据 SHA-256，便于检查记录与文件对应 |
| [run-isolated-smoke.ps1.txt](evidence/submesh-splitter-20261019/run-isolated-smoke.ps1.txt) | 整理时新增的隔离工程启动辅助；拒绝复用已有目录，检查退出码、结果文件及 33 项标记；已做语法检查，尚未实际端到端重跑 |

原始完整隔离工程曾位于 `C:/Users/admin/AppData/Local/Temp/SubmeshSplitterSmoke-20261019`，包括 `Assets/Editor/SubmeshSplitter.cs`、`Assets/Editor/SplitSmoke.cs`、`compile.rsp`、`SplitCompile.dll`、`unity-smoke.log`、`harness.txt` 等。Temp 可能被清理，今后复现优先使用本页归档和仓库源码；不依赖原 Temp 长期存在。

整理时确认原生 Unity 日志包含不能按严格 UTF-8 解码的字节；归档仅提取未受影响的 ASCII 断言行，再用显式 UTF-8 写入。新增运行器对原生日志采用容错读取，只匹配 ASCII 成功标记；文档和结果文件仍独立检查编码。

没有复制编译 DLL、Unity 生成目录、包含机器信息的完整 Editor 日志或失败临时工程到知识库。源码的唯一维护位置仍在客户端仓库，知识库只保存其身份和测试装置。

### 5.3 重新运行隔离自检

前提：本机已安装并能正常启动对应 Unity、许可可用、参数指定的源码存在。运行器只创建新的临时工程，不关闭正式工程、不保存用户场景，也不删除已有目录。脚本源码使用 `.txt` 保存，执行时复制为临时 `.ps1`：

```powershell
$rulesRoot = 'G:\Connect\ConnectDocs\Doc\Resume\作品\工程\Codex\规则\TA_DevelopmentRules\TA_CodexSkill\TA_DevelopmentRules'
$evidenceDir = Join-Path $rulesRoot 'ToolDevelopment\Profiles\ProjectACG\evidence\submesh-splitter-20261019'
$runner = Join-Path ([System.IO.Path]::GetTempPath()) ('RunSubmeshSmoke-' + [Guid]::NewGuid().ToString('N') + '.ps1')
Copy-Item -LiteralPath (Join-Path $evidenceDir 'run-isolated-smoke.ps1.txt') -Destination $runner
& $runner `
    -UnityExe 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' `
    -SourceFile 'D:\work2025U3D\Valkyria\ProjectACGMain2\ProjectACG\Client\Assets\Editor\TA_Tools\LYJ_Tool\Model_type\SubmeshSplitter\SubmeshSplitter.cs' `
    -HarnessFile (Join-Path $evidenceDir 'SplitSmoke.cs.txt')
```

本机路径变化时修改这些参数；不要把脚本放入正式工程的 `Assets/Editor` 后直接运行。运行器复制归档测试脚本为隔离工程中的 `SplitSmoke.cs`，归档原文保持不变。工具字段/方法重命名后，维护测试反射入口再执行。

历史 Unity 启动命令的关键参数是 `-batchmode -nographics -projectPath <隔离工程> -executeMethod SplitSmoke.Run -logFile <日志>`。不添加提前退出的 `-quit`，由延迟测试结束后的 `EditorApplication.Exit` 退出。

运行完成后同时检查：进程 exit code 为 0、新结果含 `PASS: 33 assertions`、日志有 33 个 `CHECK PASS:` 且有 `SPLIT_SMOKE_PASS 33`。新增启动辅助自身仅语法检查通过，不能把本节“可复现步骤”写成整理当日已经重新执行的结果。

### 5.4 编译与仓库检查记录

历史独立编译使用实际 Unity 安装下 `Data/NetStandard/ref/2.1.0/netstandard.dll`、`Data/Managed/UnityEngine/` 的模块程序集与 C# 响应文件，排除了造成同名类型冲突的聚合引用。复用时优先按[离线编译参考](../../references/unity-editor-script-offline-compile-verification.md)核对实际布局；这里保留“引用集合造成假失败”的教训，不机械复制一组跨版本 DLL 路径。

历史 `check-projectacg-harness.ps1 -Scope Changed` 未全通过，报告有 4 类失败：规则入口、官方 skill 路径、Markdown 链接、UI 生命周期/本地化治理。开发前 readiness 中已有对应基线；资源/GUID 一致性检查通过。它们与工具 33 项行为断言是不同范围的证据，不能互相抵消或替代。

## 6. 未验证项、回退与后续借鉴

### 6.1 当前证据不能覆盖的范围

- 正式项目实际窗口点击、SceneView 聚焦视觉、目标相机 GPU 渲染、真实场景材质/Lightmap/LOD 距离与性能。
- 真实进度条点击取消；现有代码具备取消检查和当前项补偿，但历史 33 项没有操作取消 UI。
- 磁盘写满、只读目录、资产创建失败、最终 SaveAssets/Refresh 异常、回滚 DeleteAsset 自身失败。
- 场景保存后关闭重开、资源重导入、Domain Reload、连续复杂编辑历史。
- Model Prefab、Variant、嵌套、Prefab Mode 的完整矩阵；祖先范围外 LODGroup、任意脚本/动画/Timeline 引用迁移。
- 多骨骼复杂模型、所有 Shape Frame/Delta 的数值对照、极限形变 Bounds、UV 各维数、自定义流及高级骨骼权重。
- UInt16/UInt32 边界顶点、超大网格、多场景、多来源大批量吞吐和取消延迟。
- 整个合并路径的端到端回归，以及新增隔离启动辅助的实际端到端执行。

### 6.2 回退与资产清理

单来源失败由当前事务补偿；批次此前成功项保留。用户要撤销成功拆分时，一次 Undo 恢复来源组件、子节点和已记录的 LOD 引用。成功生成的 Mesh 继续保留，保证 Redo 能找到它；确实不再需要 Redo 时，再依据输出目录/清单人工清理生成资产。不要先删资产再宣称还能完整 Redo。

原 Mesh 和源 Prefab 没有在这次拆分流程中被覆盖。需要回退代码时使用版本控制确认该工具实际文件范围，避免把工作区其他资源改动一起撤销。

### 6.3 适合借鉴与待演进的部分

| 可直接借鉴的思路 | 使用前仍需验证 |
| --- | --- |
| 来源去重与执行分离 | 目标项目的收集范围和 Stage 语义 |
| 完整替代结果就绪后再清理来源 | 数据通道、外部引用、组件依赖 |
| 逐来源 Undo 与资产补偿清单 | 回滚自身失败、耦合产物是否需要整批原子性 |
| 最终刷新/定位后收束批次 Undo | 目标版本和异步后续编辑 |
| 成功资产保留以支持场景 Redo | 用户是否需要资产级事务和长期产物回收 |
| 最小 Undo 基线、隔离工程、源码哈希 | 正式项目编译、真实资产与渲染 |
| 历史问题按现象/根因/修正/证据归档 | 新版本源码是否仍符合记录 |

后续可按真实需求评估：结构化成功/失败/跳过清单、取消时完整 warnings、回滚删除失败报告、外部引用扫描、空 SubMesh 独立策略、幂等更新和大批量性能测量。这些是候选改进，不是本次已实现能力，不升级为 CORE。
