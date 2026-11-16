<!-- BEGIN automation:ta date:2026-11-13 layer:INDEX -->
# Codex 开发制作经验日报｜2026-11-13

> 类型：当日总览与索引；可迁移方法见 REFERENCE，项目状态见 PROFILE；本索引不定义强制规则。
> **日期范围**：Asia/Shanghai，2026-11-13 00:00（含）至 2026-11-14 00:00（不含）；等价 UTC 区间为 2026-11-12 16:00（含）至 2026-11-13 16:00（不含）。
> **来源筛选**：指定 `sessions/` 共 53 份 Markdown；4 份含区间消息，共 229 条。排除本自动化会话 1 份、9 条消息后，3 个技术会话、220 条带时间戳消息进入主题核对；仓库指令注入和重复上下文不作为开发经验。
> **覆盖主题**：ProjectACG 的 FlowMap/星空 Shader、Mesh FlowMap 与通用贴图绘制工具、UV 边界、GPU 绘制性能、工作区恢复与绘制窗口整理。
> **产出**：本索引、ShaderDevelopment 与 ToolDevelopment 各一份 REFERENCE 和一份 ProjectACG PROFILE，共 5 份 Markdown 及 5 个文档 `.meta`；复用现有 `CodexDaily` 目录，不修改 CORE 或其他既有文档。
> **未验证项**：生产场景内最终交互与材质观感需本地确认；真实角色、移动端及目标设备性能未覆盖。FlowMap 的屏幕坐标/视距补偿及独立速度贴图方案仍是讨论建议，不是当日已实现功能。

## 1. 当日文档索引

| 层级 | 文档 | 内容与定位 |
| --- | --- | --- |
| REFERENCE | [Shader 屏幕坐标、循环混合与纹理分层](ShaderDevelopment/references/CodexDaily/Development_Reference_2026-11-13.md) | 屏幕坐标与距离补偿的取舍、FlowMap 相位混合、静态底色与动态层组合，以及验证要点。 |
| PROFILE | [ProjectACG FlowMap 与星空 Shader](ShaderDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-13.md) | 当日会话报告的功能、通道/采样边界、独立渲染检查和未实现方案。 |
| REFERENCE | [Unity Mesh 贴图绘制工具参考](ToolDevelopment/references/CodexDaily/Development_Reference_2026-11-13.md) | UV 周期语义、通道与笔刷解耦、局部 GPU 绘制、异步备份和紧凑 UI 的可迁移做法。 |
| PROFILE | [ProjectACG Mesh 贴图绘制工具](ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-13.md) | FlowMap Painter 扩展、报告性能数据、空绑定恢复故障及当前交互验证边界。 |

## 2. 来源会话

- [01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe](../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md)：FlowMap Shader 功能与局部速度、屏幕坐标方案讨论。
- [01a1d02f-f5cf-7c83-8274-b18802855696](../VA_Worlk_memory/sessions/01a1d02f-f5cf-7c83-8274-b18802855696.md)：Mesh 贴图绘制工具功能、UV、性能、恢复与 UI。
- [01a1d320-8c76-7d73-b99f-fd89b5b32b66](../VA_Worlk_memory/sessions/01a1d320-8c76-7d73-b99f-fd89b5b32b66.md)：星空 Shader 的贴图坐标、视距补偿、闪点遮罩与 MatCap。

来源消息中的性能、编译和渲染结果均为会话内报告，本次没有读取项目源码或重跑 Unity；详细证据类型与未验证边界见各 Profile。
<!-- END automation:ta date:2026-11-13 layer:INDEX -->
