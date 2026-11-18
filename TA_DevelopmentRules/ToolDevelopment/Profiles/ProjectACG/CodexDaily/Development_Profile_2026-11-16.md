<!-- BEGIN automation:ta date:2026-11-16 layer:PROFILE -->
# ProjectACG 入场资源复制工具 Profile｜2026-11-16

> 类型：PROFILE；项目：ProjectACG；记录指定会话报告的实现、测试和边界，本次未读取项目源码或执行 Unity。
> 关联参考：[Timeline 资产生成、Prefab 绑定与增量组装](../../../../TimelineDevelopment/references/timeline-asset-generation-and-prefab-binding.md)；模块入口：[ToolDevelopment](../../../README_Tech_TAToolDevelopmentRules.md)；沿用 `TOOL-CMP-01`、`TOOL-OPS-01`、`TOOL-VAL-02`。

## 1. 项目事实与现象

- 入场资源复制入口位于 `UltimateSkillCameraTimelineGenerator/AdmissionTimelineCopyService.cs`。原实现遇到目标路径已存在时直接失败，无法重复复制更新已有入场资源。
- 用户要求目标存在时原位覆盖并保持 GUID。会话报告改为保留目标 `.meta`，将源资源 YAML 内容写回目标路径，并用源 GUID 到目标 GUID 的映射重绑内部资源引用；源资源保持不变。
- 新资源仍使用 `AssetDatabase.CopyAsset`。覆盖已有目标前备份目标文件；失败时恢复备份并清理本次新建资源。成功状态文案改为“已生成或覆盖”。
- 当日同一生成工具又处理了复制后的 Timeline/Prefab 清理范围，详见 [Timeline Profile](../../../../TimelineDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-16.md)。

## 2. 根因、修正与取舍

- 资源文件路径相同不代表资源身份相同；直接删除后重建 `.meta` 会改变 GUID 并断开引用。该实现以目标 `.meta` 为身份锚点，只更新资源正文并显式重映射内部引用。
- 复制调用必须区分“目标不存在”和“目标已存在”：前者创建，后者原位更新。备份与失败恢复缩小中途写入失败的损坏范围，但不能替代 Unity 内实际重开资源和引用验收。
- 该工具路径、Timeline/Prefab 类型、AssetDatabase 行为与序列化细节均为 ProjectACG 事实，不作为跨项目规则；复用时先查目标 Unity 版本与资产格式。

## 3. 会话报告的验证证据

| 检查 | 会话报告 | 边界 |
| --- | --- | --- |
| 编辑器程序集 | `Assembly-CSharp-Editor.csproj` 编译 0 错误。 | 静态编译，不是 Unity 内运行。 |
| EditMode 测试程序集 | `GameLogic.EditMode.Tests.csproj` 编译 0 错误；新增重复复制、内容更新和 GUID 不变测试。 | Unity 内实际 EditMode 测试没有运行，因为工程已被编辑器占用。 |
| ProjectACG harness | 会话早期报告通过；稍后 Changed harness 被工作区既有无关资源/`.meta`/GUID 漂移阻断。 | 最终报告不能等同于 Changed harness 全部通过；阻断项被报告为本任务之外。 |
| Diff | `git diff --check` 报告通过。 | 不能证明生成结果和资源引用正确。 |

## 4. 未验证项与回退

- 仍需在 Unity 中对已有入场资源再次执行复制，确认目标 `.meta` GUID 保持、资源内容更新、内部引用正确，并实际打开/播放目标 Timeline。
- Unity 内重复复制测试尚未运行；如果失败，先恢复覆盖前备份，再检查目标与源 GUID 映射，不要删除目标 `.meta` 重新生成。
- 相关既有生成检查方法见 [Timeline 资产生成参考](../../../../TimelineDevelopment/references/timeline-asset-generation-and-prefab-binding.md)；本 Profile 不新增 CORE 或新的强制规则。

## 来源会话

- [01a1e316-6129-7101-9c79-603ad628bd1e](../../../../../VA_Worlk_memory/sessions/01a1e316-6129-7101-9c79-603ad628bd1e.md)
- [01a1e275-b375-7483-8842-f85daa399b89](../../../../../VA_Worlk_memory/sessions/01a1e275-b375-7483-8842-f85daa399b89.md)

<!-- END automation:ta date:2026-11-16 layer:PROFILE -->
