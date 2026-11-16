<!-- BEGIN automation:ta date:2026-11-15 layer:INDEX -->
# Codex 开发制作经验日报｜2026-11-15

> 类型：当日总览与索引；可迁移方法见 REFERENCE，项目状态见 PROFILE；本索引不定义强制规则。
> **日期范围**：Asia/Shanghai，2026-11-15 00:00（含）至 2026-11-16 00:00（不含）；等价 UTC 区间为 2026-11-14 16:00（含）至 2026-11-15 16:00（不含）。
> **来源筛选**：指定 `sessions/` 共 57 份 Markdown；4 份含窗口消息，共 222 条带时间戳消息。排除本自动化会话 1 份、5 条消息后，3 个技术会话、217 条消息进入主题整理；按消息时间筛选，不使用文件修改时间或会话开始日期。
> **覆盖主题**：Timeline 材质替换/追加绘制、Shader 参数曲线与恢复；ProjectACG 移动端云海方案；FlowMap PBR 滑条与晶体内发光；BaseLitGalaxy 表面/中心内发光。
> **产出**：本索引、ShaderDevelopment 与 TimelineDevelopment 各一份 REFERENCE 和一份 ProjectACG PROFILE，共 5 份 Markdown 及 5 个文档 `.meta`；复用已有 `CodexDaily` 目录，未修改 CORE 或其他既有文档。
> **未验证项**：云海方案没有工程实现或实机性能数据；FlowMap 晶体发光验证在 HDR 阈值断言失败后未见当天重跑结果；Timeline 最新 Vector 分量曲线没有运行 EditMode，Hair 隐现原因仍是待查假设；ProjectACG 生产场景和移动设备均未完成全面复验。

## 1. 当日文档索引

| 层级 | 文档 | 内容与定位 |
| --- | --- | --- |
| REFERENCE | [Shader 云海与可选效果验证](ShaderDevelopment/references/CodexDaily/Development_Reference_2026-11-15.md) | 移动端云海方案取舍、现有 Renderer/资产核验、Shader 开关与隔离验证方式。 |
| PROFILE | [ProjectACG Shader 与环境渲染](ShaderDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-15.md) | FlowMap、BaseLitGalaxy 与云海相关项目事实、会话报告的验证和未闭环事项。 |
| REFERENCE | [Timeline 材质参数曲线与叠加绘制](TimelineDevelopment/references/CodexDaily/Development_Reference_2026-11-15.md) | MPB、Vector 分量曲线、SubMesh 叠画、恢复生命周期和性能验证边界。 |
| PROFILE | [ProjectACG Timeline 材质 Clip](TimelineDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-15.md) | 参数曲线、Overlay Renderer、透明队列、Dissolve 属性匹配、验证状态及 Hair 显隐排查。 |

## 2. 来源会话

- [01a1cf40-ee85-79b3-af3a-15bf06a6a14d](../VA_Worlk_memory/sessions/01a1cf40-ee85-79b3-af3a-15bf06a6a14d.md)：Timeline 材质参数曲线、追加绘制和实际播放排查。
- [01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe](../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md)：FlowMap PBR 控件与晶体内发光。
- [01a1d320-8c76-7d73-b99f-fd89b5b32b66](../VA_Worlk_memory/sessions/01a1d320-8c76-7d73-b99f-fd89b5b32b66.md)：BaseLitGalaxy 表面自发光与晶体中心内发光。
- [01a1df44-47ae-7662-bfa1-571b90a718d9](../VA_Worlk_memory/sessions/01a1df44-47ae-7662-bfa1-571b90a718d9.md)：ProjectACG 移动端云海实现方案分析。

编译、隔离渲染、像素与 EditMode 结果均按会话报告记录，本次没有读取项目源码或重新运行 Unity；各 Profile 区分已报告验证、未验证与候选原因。
<!-- END automation:ta date:2026-11-15 layer:INDEX -->
