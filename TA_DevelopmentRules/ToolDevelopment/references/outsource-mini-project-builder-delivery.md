# Unity Mini 工程导出与交付参考

> 类型：REFERENCE；适用范围：Unity Editor 资源裁剪、Mini 工程生成、外包/内部工程交付；使用前提：必须以目标项目的 Unity 版本、程序集、资产消费者和实际导出代码重新验证。

## 1. 适用场景

这份参考用于把主工程中的场景、Prefab、Shader、渲染脚本、材质 Inspector 和必要编辑器工具裁剪成可独立打开的 Mini 工程。它适合“按任务交付最小依赖闭包”的导出器，不替代 Addressables、Player 构建系统、Package 发布系统或全工程备份。

导出器同时面对四类风险：依赖不完整导致目标工程出现 `Missing Script` 或编译错误；依赖范围过宽泄露无关程序集和内部源码；在源场景/RendererData/VolumeProfile 上直接清理造成主工程损坏；强制生成掩盖可判定问题。

使用本参考前，先读取工具模块的 `TOOL-DOC-03`、`TOOL-ARC-01`、`TOOL-OPS-01`、`TOOL-OPS-02`、`TOOL-VAL-02` 和 `TOOL-VAL-03`，再读取项目 Profile。

## 2. 前提与依赖

### 2.1 先冻结交付契约

导出前明确输入场景、Prefab、额外资产、额外 Shader、脚本和 Profile；目标 Unity 版本、平台与 Graphics API；输出目录和任务 ID；外包或内部模式；明文 Shader 是否允许；阻断/警告/强制放行边界；回包目录和覆盖策略。将这些值写入可审计的 manifest 或报告，不要让窗口临时 toggle 静默改写既有 Profile。

### 2.2 依赖闭包要有事实来源

资源路径和类型以 `AssetDatabase`、对象引用、序列化内容和实际消费者为准。程序集的“可见引用”不能直接当资源依赖闭包，它会把无关 Package、Editor/Roslyn、SDK 或整个业务程序集错误扩张进输出。Shader、RendererFeature、VolumeComponent、场景对象和材质 Inspector 是不同方向的依赖，不能假设 Unity 会自动反向推断它们。

## 3. 实现或排查步骤

### 3.1 六阶段流程

将流程拆成 `Analyze -> Plan -> Confirm -> Stage -> Scan -> Install`：

1. **Analyze**：读取输入、解析类型和引用，生成问题列表；不改源资产。
2. **Plan**：展开场景/Prefab/额外资产依赖、脚本、Package、Shader、DLL 和剔除动作，去重并标注来源。
3. **Confirm**：展示目标数、风险、删除/覆盖范围和不可回滚边界，用户确认后才写入。
4. **Stage**：在临时目录生成完整输出；场景、RendererData、VolumeProfile 等只清理副本。
5. **Scan**：检查路径越界、符号链接、危险扩展名、保护源码、禁止程序集、Missing Script 和 manifest 一致性。
6. **Install**：扫描通过后，以备份和可恢复事务安装到最终目录，并写报告。

若分析需要临时场景快照，先完成对象剔除，再对快照判定依赖，否则被剔除对象可能制造假阻断。

### 3.2 Runtime、Editor、RendererFeature 和 VolumeComponent 边界

| 类型 | 配置入口 | 复制/剔除语义 | 常见错误 |
| --- | --- | --- | --- |
| `MonoBehaviour` / Runtime | 运行时脚本目录或单个源码 | 进入 Player 程序集闭包；必要 `.asmdef/.asmref` 一并复制 | 选择整个业务根目录，或漏交付被引用类型 |
| Editor 脚本 | 含 `Editor` 路径的目录或单文件 | 只复制到目标工程 Editor 侧，不进入 Player 闭包 | 把 Editor 程序集当运行时，漏掉 `.uxml/.uss/.asset/.png` |
| `ScriptableRendererFeature` | RendererFeature 精确列表 | 依据 RendererData 副本的真实 Feature 引用；剔除只改副本 | 试图从 Shader 反推 Feature，或把 Volume 放入该列表 |
| `VolumeComponent` | VolumeComponent 精确列表 | 依据 VolumeProfile 副本的真实组件引用；剔除只改副本 | 只删脚本不删 Profile 子资产，留下悬空引用 |
| Shader / Include | 场景依赖、额外 Shader、手动 Shader 列表 | 按模式明文复制或进入保护链路；`Packages/...` Include 保留原引用 | 把整个 Shader 包或 URP 源码递归塞进 `Assets` |

Shader 只描述绘制接口和 Include 依赖，不会反向声明哪个 RendererFeature 驱动它。依赖屏幕纹理、Stencil、RenderPass、Volume 或全局参数的效果，必须显式交付实际运行时代码和配置资产。

### 3.3 ObjectField 自动转路径

编辑器配置优先保存对象引用；序列化时通过 `AssetDatabase.GetAssetPath(objectReference)` 写回项目相对路径。列表显示对象名或短相对路径，不显示难以阅读的绝对路径；定位按钮再调用 `EditorGUIUtility.PingObject` 或选择器。

写回路径时检查空引用、`Assets/`/`Packages/` 范围、类型、文件/目录语义、`..`、符号链接/目录联接、重复路径和父子目录重叠。不要要求用户手工输入裸字符串路径；外部输出目录使用文件夹选择器并与工程内路径分开。

### 3.4 最小依赖闭包和目录范围

额外资产、Prefab、材质和贴图可用 `AssetDatabase.GetDependencies(..., true)` 展开，但结果仍要按类型、安全策略和白名单过滤。目录选择只表示递归候选范围，不代表绕过脚本、Package、DLL 或源码扫描。

建议“精确文件优先、最小目录其次”：先复制任务直接需要的资产和脚本，再补 `.meta`、asmdef、材质 Inspector 资源和托管 DLL，最后才允许经过审计的最小目录。禁止整个 `Assets`、宽泛业务根、符号链接或目录联接扩大范围。官方 Registry/Built-in Package 通常记录精确版本到目标 `manifest.json`，由目标工程安装；Embedded、Local、Git Package 要明确交付方式。

### 3.5 ShaderGUI、旧式 MaterialEditor 和 DLL

解析 Shader 的 `CustomEditor` 后区分现代 `ShaderGUI`、`MaterialPropertyDrawer`、旧式 `UnityEditor.MaterialEditor` 派生 Inspector 和预编译 DLL。只允许实际 DLL 或最小源码目录，并保留 `.meta`，不能因为一个 Inspector 复制整个插件目录。

类型完全不存在时可以作为“目标工程回退默认 Inspector”的警告；类型已找到但实现/DLL 未交付，说明配置与消费者不一致，应阻断或要求用户明确选择。

### 3.6 Warning、Blocker 和 Force

- **Blocker**：输入缺失、输出不可写、最终必然编译失败、Missing Script、目标资产引用缺类、路径越界、危险文件或安全扫描命中；默认停止。
- **Warning**：目标仍可打开且有明确回退，例如 CustomEditor 类型完全不存在；必须写报告并可定位。
- **Info**：行为说明、自动迁移提示、去重项和非错误建议。

“强制生成”只应对可判定的分析、环境、路径长度和安全扫描阻断继续执行，并将原问题写入 `DependencyReport.json`、`SecurityReport.json` 和强制模式警告文件。必要输入缺失、输出目录无法创建、磁盘拒绝写入、快照/manifest 无法建立等客观执行失败仍然失败。

### 3.7 阻断项定位与修复 UI

每项阻断应有代码、等级、对象/资产、原因、影响、推荐操作、定位入口和自动修复能力标记。“定位”和“修复”放同一行；修复只对工具能证明修改范围的项目出现。自动修复需二次确认，完成后重新分析，不能只刷新计数。

清理源 Prefab 的 Missing Script、扩大白名单、删除源码或修改主工程 RendererData 不应静默执行；优先提供定位、复制路径和手工步骤。

### 3.8 副本清理、更新和失败恢复

场景、Prefab、RendererData、VolumeProfile 的剔除必须在临时副本进行，源资产不写回。删除真实引用时同时处理序列化引用和子资产；同一类型仍被其他最终复制资产引用时不能静默删除。

更新已有输出时，先临时生成并扫描，再验证旧目录 manifest 与任务 ID，备份旧目录后替换。任一步失败都保留或恢复旧输出。该流程不负责合并外包方未回传的修改，必须写明备份位置。

### 3.9 报告和可回放性

至少记录任务 ID、输入快照、Profile 摘要、目标平台、源到目标映射、复制/剔除清单、依赖来源、问题分类、强制放行项、备份目录、最终路径和验证入口。日志区分 Info、Warning、Error，并汇总成功、跳过和失败数量。

## 4. 风险与不适用边界

- 内部明文模式只改变交付保护和使用限制，不应默认关闭路径、依赖、Missing Script、程序集和文件系统检查。
- Shader 加密、源码裁剪和程序集裁剪不是同一件事；关闭其中一项不能推导其他项也关闭。
- 删除场景对象不会自动删除 Profile 中显式交付的脚本；必须重新计算实际消费者。
- 静态文本扫描不能证明目标工程可编译；条件编译、生成代码、第三方程序集和 Package 版本仍需目标工程验证。
- 强制产物应标记为内部诊断或临时交付，不能替代发布质量门槛。

## 5. 验证与回退

| 层级 | 验证重点 |
| --- | --- |
| 静态 | UTF-8、路径归一化、序列化键、程序集边界、链接和报告结构 |
| Unity Editor 编译 | Editor 工具、测试程序集、Shader/材质 Inspector 依赖 |
| Editor 行为 | 空输入、错误类型、重复项、ObjectField 路径写回、定位/修复、分析/计划/取消 |
| 资产结果 | 场景/Prefab/RendererData/VolumeProfile 副本、Meta/GUID、Shader/材质引用、无 Missing Script |
| 目标 Mini 工程 | 删除 `Library` 后用基线 Unity 打开，执行 Batch Validation，检查场景、材质、Shader 和脚本编译 |
| 更新/失败 | 覆盖、备份、磁盘失败、扫描失败、安装失败和旧输出恢复 |

验证失败时回退到临时目录和旧输出备份，不删除源资产。若 Unity 实例、Build Support 或第三方 DLL 不可用，应记录已完成的静态/编译验证和未完成的目标工程验证。
