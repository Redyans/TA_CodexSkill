<!-- BEGIN automation:ta date:2026-11-16 layer:PROFILE -->
# ProjectACG 入场 Timeline 与 Prefab 清理 Profile｜2026-11-16

> 类型：PROFILE；项目：ProjectACG；只记录指定会话报告的行为、最终清理范围与验证状态，本次未读取项目源码或执行 Unity。
> 关联参考：[Timeline 资产生成、Prefab 绑定与增量组装](../../../references/timeline-asset-generation-and-prefab-binding.md)、[Timeline 重复生成与手工内容保护](../../../references/timeline-regeneration-preservation-and-tail-camera.md)；模块入口：[TimelineDevelopment](../../../README_Tech_TimelineDevelopmentRules.md)；沿用 `TML-CMP-01`、`TML-VAL-01`、`TOOL-OPS-01`、`TOOL-VAL-02`。
> 相关复制与 GUID 处理见 [ToolDevelopment Profile](../../../../ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-16.md)。

## 1. 项目事实与现象

- 生成器位于 `Client/Assets/Editor/TA_Tools/Animation/UltimateSkillCameraTimelineGenerator/`。用户要求复制/生成入场资源时清理遗留 Timeline 与主 Prefab 的 `camAni_group` 内容。
- 需求先被理解为清空全部 Timeline 根轨道，用户随后明确收窄为只清理 `Camera Group`。截图继续显示该组内部仍有旧子轨道/Clip，且 `camAni_group` 中仍残留旧镜头实例。
- 最终会话报告的复制路径行为是：在入场资源复制到目标后，删除目标 Timeline 的整个 `Camera Group`（包括子轨道与 Clip），清空目标 Prefab 的 `camAni_group` 所有子实例；源抽卡资源不修改，已存在的目标仍原位覆盖并保留 GUID。
- 手动入场组装路径也增加残留校验：清理后若仍检测到 `Camera Group` 或 `camAni_group` 子对象则报错。普通大招生成的手工轨道保护逻辑继续保留。
- 二次生成测试曾安排验证：删除旧 Camera Group、保留其他根轨道，并清除旧镜头子对象。最终清理范围是整个 `Camera Group` 组及其内容，不是删除所有 Timeline 根轨道。

## 2. 根因、修正与取舍

- 只在手动组装入口清理不足以覆盖“从模板复制资源”路径；复制出来的 Timeline 会重新带入源资源中的旧 Camera Group。修正落在复制后的目标资源清理阶段，并保留对手动生成路径的检查。
- 仅删除或重建空的根分组会留下子轨道和 Clip；最终要求以整组为单位删除，并逐项清空 Prefab 容器，再用残留断言阻止静默保存。
- 用户修正后的所有权边界是：生成器可重建 `Camera Group` 和 `camAni_group` 内容，其他 Timeline 根轨道需保留。此为当前 ProjectACG 入场工具的 Profile 事实。
- 资源覆盖与结构清理是两个不同合同：保持目标 GUID 不等于保留旧 Timeline 内容。复制流程既要保持资源身份，也要按入场规则重建指定结构。

## 3. 会话报告的验证证据

| 检查 | 会话报告 | 边界 |
| --- | --- | --- |
| C# 编译 | `Assembly-CSharp-Editor.csproj` 与 `GameLogic.EditMode.Tests.csproj` 均报告 0 错误。 | 只证明静态编译。 |
| Diff | `git diff --check` 报告通过。 | 不证明 Prefab/Timeline 实际序列化或播放正确。 |
| 生成测试 | 会话新增重复生成/复制清理测试，覆盖目标无旧 Camera Group 和 `camAni_group` 子节点。 | Unity 内 EditMode 未执行；后来 Changed harness 仍被工作区已有资源 `.meta`/GUID 漂移阻断。 |
| 源资源保护 | 会话报告清理只作用于目标副本，源抽卡资源不修改。 | 本次没有直接比较源/目标资产哈希。 |

## 4. 未验证项与回退

- 需在 Unity 中对真实入场资源运行复制和生成，保存后重开 Timeline/Prefab，确认 Camera Group 整组及 Clip 已删除、其他根轨道仍在、`camAni_group` 为空、源抽卡资源未变，并检查播放结果。
- 若清理失败，工具报告应中止保存并定位残留对象；恢复时使用生成前资源备份，不能用清空整个 Timeline 作为通用回退。
- Timeline 资源身份、结构和持久绑定的检查流程引用上述既有 REFERENCE；本案例没有足够证据扩展正式 CORE。

## 来源会话

- [01a1e316-6129-7101-9c79-603ad628bd1e](../../../../../VA_Worlk_memory/sessions/01a1e316-6129-7101-9c79-603ad628bd1e.md)
- [01a1e275-b375-7483-8842-f85daa399b89](../../../../../VA_Worlk_memory/sessions/01a1e275-b375-7483-8842-f85daa399b89.md)

<!-- END automation:ta date:2026-11-16 layer:PROFILE -->
