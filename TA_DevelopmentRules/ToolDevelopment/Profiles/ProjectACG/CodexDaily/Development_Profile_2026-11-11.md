<!-- BEGIN automation:ta date:2026-11-11 layer:PROFILE -->
# ProjectACG 工具开发经验｜2026-11-11

> 类型：PROFILE；范围：北京时间 2026-11-11 00:00（含）至 2026-11-12 00:00（不含）。来源会话工作目录为 `ProjectACGMain/ProjectACG`；只采用指定 sessions 内当日消息，本次不读取项目源码或测试产物。
> 可迁移方法见 [工具开发参考](../../../references/CodexDaily/Development_Reference_2026-11-11.md)。规范引用 [工具模块](../../../README_Tech_TAToolDevelopmentRules.md)中的 `TOOL-CMP-01`、`TOOL-OPS-01`～`TOOL-OPS-03`、`TOOL-VAL-01`～`TOOL-VAL-03`；不修改 CORE。

## 1. 特效 Prefab 分析器：等级与面数语义

**项目事实/现象**：用户要求等级节点使用 `lv1`、`lv2`、`lv3`，并把整个特效 Prefab 面数改为检查单个特效粒子所用 Mesh 是否超限。来源：[会话 01a1c9ef-f703-7a90-9960-74a4e8062b98](../../../../../VA_Worlk_memory/sessions/01a1c9ef-f703-7a90-9960-74a4e8062b98.md)，消息 2–11。

**根因或设计约束**：会话称原有结构校验、组合规则、参考标准和性能统计把 `L0/L1/L2` 写死；总 Prefab 面数也不符合“单个粒子 Mesh”阈值语义。

**项目修正（会话报告）**：资源节点和界面严格识别 `lv1/lv2/lv3`，不再静默将 `L0/L1/L2`、`LOD*` 视为等价；内部 0/1/2 数值和 `lod:0/1/2` 配置键保留，以维持已有标准资产。面数只扫描 `ParticleSystemRenderer` 候选 Mesh，单个 Mesh 分别计算三角面，报告其中最大值并与可配置阈值比较；默认值为 500。普通 `MeshRenderer`、`SkinnedMeshRenderer`、粒子数量、Billboard 和拖尾不参与此指标。

**验证证据**：消息报告修改涉及 `PrefabPerformanceData.cs`、`PrefabPerformanceAnalyzer.cs`、`PrefabAnalyzerTool.cs` 和 `PrefabPerformanceStandards.asset`；三脚本中旧等级文本已清除，harness 检查、资源一致性和针对性静态断言通过。

**未验证项/回退**：未在 Unity 打开工具或导入代表性 Prefab 验证界面、编译和三角面统计。重新编译后需用同时含 `scale/lv1/lv2/lv3`、普通网格、多个粒子 Mesh、Billboard/拖尾的 Prefab 验证；此项是会话建议，不是已经完成的测试。

## 2. FBX Prefab 改名/删除后的源层级同步

**项目事实/现象**：角色 FBX 生成 Prefab 后，FBX Mesh 改名会新增 Mesh 资源而不替换旧节点；删除源 Mesh 后 Prefab 中旧 GameObject 和 Mesh 仍保留。来源：[会话 01a1cab4-df21-72c3-ba9b-5c0157ab2b5a](../../../../../VA_Worlk_memory/sessions/01a1cab4-df21-72c3-ba9b-5c0157ab2b5a.md)，消息 2–8、10–17。

**根因**：会话定位到两条更新路径。常用 `PreserveExistingAdditive` / `MergeSourceHierarchyAdditive` 只按已有相对路径补充节点，路径改变时旧节点没有来源归属可用于清理；刷新路径 `CollectRefreshExtraChildren` 又把所有当前源不存在的节点当作手工内容恢复。两种策略都缺少上一轮生成来源记录。

**项目约束或修正（会话报告）**：在 `PrefabImporter.userData` 保存 Base64 JSON 状态，包含源 FBX GUID、上一轮源层级路径和当前 Prefab 实际引用的生成 Mesh 路径。增量及刷新前清理可证明是旧源且当前已不存在的节点；保留真实手工节点、组件及引用。无状态旧 Prefab 使用兼容回退，仅清理能识别为工具 Mesh 输出目录中失效 Renderer 的节点。生成后仅当旧 `.mesh` 已无 Prefab/Scene/当前 Prefab 引用时才删除；手工 `MeshCollider` 等引用受保护。

**验证证据（会话报告）**：修改 `CharacterFbxPrefabBuilderWindow.cs` 与 `CharacterPrefabDeletionRebuildTests.cs`；测试工程 `dotnet build ... --no-restore` 报告 0 warnings、0 errors，`check-projectacg-harness.ps1 -Scope Changed` 和目标文件 `git diff --check` 通过，并新增改名/删除回归测试。

**未验证项/回退**：Unity EditMode 测试未执行，日志报告 `HandleProjectAlreadyOpenInAnotherInstance`。尚需在可运行 Unity 测试的环境检查 Preserve/Refresh 两种模式下改名后旧节点消失、新节点出现、删除后旧节点消失、手工节点及引用保留、旧 Mesh 只在无其他引用时删除。来源会话还把 `SaveAndReimport()` 时序和多步骤生成状态列为待核查边界。无来源标记的旧 Prefab 无法从纯骨骼节点可靠推断来源；兼容 fallback 不应扩大为全层级删除。

## 3. CORE 候选

- **候选**：生成器应持久化可验证的源所有权，差异更新只清理由生成器拥有且确认失效的节点，删除资源前检查完整消费者集合。证据来自一次 ProjectACG 修复；Unity EditMode 未跑，适用边界和不同 Prefab 合并策略尚未验证。留在本 Profile 候选区，不升级正式 CORE；相关通用实现步骤见 FBX Prefab REFERENCE。

<!-- END automation:ta date:2026-11-11 layer:PROFILE -->
