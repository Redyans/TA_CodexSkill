<!-- BEGIN automation:ta date:2026-11-16 layer:INDEX -->
# Codex 开发制作经验日报｜2026-11-16

> 类型：当日总览与索引；可迁移方法见 REFERENCE，项目状态见 PROFILE；本索引不定义强制规则。
> **日期范围**：Asia/Shanghai，2026-11-16 00:00（含）至 2026-11-17 00:00（不含）；等价 UTC 区间为 2026-11-15 16:00（含）至 2026-11-16 16:00（不含）。
> **来源筛选**：指定 `sessions/` 共 70 份 Markdown；9 份含窗口消息，共 389 条带时间戳消息。排除本自动化会话 1 份、11 条消息后，8 个技术会话、378 条消息进入主题整理；按消息时间筛选，不使用文件修改时间或会话开始日期。
> **覆盖主题**：ProjectACG 的 UI Dual Kawase Blur、FlowMap 晶体内发光、BaseLitGalaxy 内部发光、世界空间 Lambert 描边 Shader、入场 Timeline/Prefab 资源覆盖与清理；ProjectACGMain2 的 Timeline 预览角色结构报错；3ds Max 批量动画传递与关键帧持久化问题。
> **产出**：本索引、ShaderDevelopment 的 REFERENCE 与 ProjectACG PROFILE、ToolDevelopment 的 ProjectACG PROFILE、TimelineDevelopment 的 ProjectACG 与 ProjectACGMain2 PROFILE、DCCDevelopment 的 REFERENCE、根目录 Unclassified PROFILE，共 8 份 Markdown 和 8 个文档 `.meta`；为 DCC `references/CodexDaily` 与 ProjectACGMain2 `CodexDaily` 配套目录 `.meta`，复用根 `Profiles/CodexDaily` 的既有 `.meta`，未修改 CORE 或其他既有文档。
> **未验证项**：Dual Kawase 缺实际 GameView/设备 GPU 验收；BaseLitGalaxy 的最终投影观感留给用户检查；入场 Unity EditMode 测试未运行；ProjectACGMain2 的具体触发子节点未隔离且修复后未复测；3ds Max 最新脚本保存重载后的活动控制器关键帧仍未得到可靠验证。

## 1. 当日文档索引

| 层级 | 文档 | 内容与定位 |
| --- | --- | --- |
| REFERENCE | [Shader 坐标空间、内部径向效果与 UI 模糊链](ShaderDevelopment/references/CodexDaily/Development_Reference_2026-11-16.md) | 光照坐标空间、内部球形发光投影近似、Dual Kawase 图标模糊的混合语义与性能验证。 |
| PROFILE | [ProjectACG Shader 与 UI 模糊](ShaderDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-16.md) | UI Blur 两段需求、FlowMap/Galaxy 发光、Lambert 描边的实现状态和未验项。 |
| PROFILE | [ProjectACG Timeline 资产复制与入场生成](ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-16.md) | 已有资源覆盖、GUID 保持、失败恢复与 Editor 测试边界。 |
| PROFILE | [ProjectACG 入场 Timeline 与 Prefab 清理](TimelineDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-16.md) | 只清理 `Camera Group` 及其内容、清空 `camAni_group` 的最终范围。 |
| PROFILE | [ProjectACGMain2 预览角色结构报错](TimelineDevelopment/Profiles/ProjectACGMain2/CodexDaily/Development_Profile_2026-11-16.md) | `SceneTimelinePreviewCharacterScope` 的拒绝条件、Prefab 排查位置和未定位的触发对象。 |
| REFERENCE | [3ds Max 批量动画传递与关键帧验证](DCCDevelopment/references/CodexDaily/Development_Reference_2026-11-16.md) | 文件配对、骨骼白名单、帧/Tick 单位和保存重载检查。 |
| PROFILE | [3ds Max 批处理脚本未分类案例](Profiles/CodexDaily/Unclassified_2026-11-16.md) | 加密 `.mse`、独立 `.ms`、测试日志、覆盖风险和未闭环的控制器问题。 |

## 2. 来源会话

- [01a1c5c9-7942-7061-9a27-699e9e25fa9e](../VA_Worlk_memory/sessions/01a1c5c9-7942-7061-9a27-699e9e25fa9e.md)：独立 UI 模糊 Shader 的需求调整；“Kawase 换一下”后的最终算法在该会话窗口内未确认。
- [01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe](../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md)：FlowMap 晶体内发光的完成报告与隔离验证数字。
- [01a1d320-8c76-7d73-b99f-fd89b5b32b66](../VA_Worlk_memory/sessions/01a1d320-8c76-7d73-b99f-fd89b5b32b66.md)：BaseLitGalaxy 内部球形发光的空间采样调整及编译报告。
- [01a1e26f-2645-7ce2-8a4d-1764c762504d](../VA_Worlk_memory/sessions/01a1e26f-2645-7ce2-8a4d-1764c762504d.md)：3ds Max 2024 批量骨骼动画传递、失败案例与最新保存重载检查。
- [01a1e275-b375-7483-8842-f85daa399b89](../VA_Worlk_memory/sessions/01a1e275-b375-7483-8842-f85daa399b89.md)：入场资源生成、UI Dual Kawase Blur 与 Lambert 描边 Shader。
- [01a1e293-676a-7c00-8a16-868eb54acef8](../VA_Worlk_memory/sessions/01a1e293-676a-7c00-8a16-868eb54acef8.md)：主线探索据点选中轮廓分析，随后实现专用 Lambert/Outline Shader 并切换世界空间光照方向。
- [01a1e316-6129-7101-9c79-603ad628bd1e](../VA_Worlk_memory/sessions/01a1e316-6129-7101-9c79-603ad628bd1e.md)：入场资源重复覆盖与复制后 Timeline/Prefab 清理范围。
- [01a1e31e-dbe5-7ab0-933b-16ed61f05c3b](../VA_Worlk_memory/sessions/01a1e31e-dbe5-7ab0-933b-16ed61f05c3b.md)：ProjectACGMain2 `100401` 预览角色结构隔离异常诊断。

会话报告的编译、harness、像素、关键帧和 Inspector 数字均标记为来源报告；本次没有读取项目源码、测试日志、场景或 Max 文件，也没有重新运行 Unity 或 3ds Max。项目事实与未验证项分别见配套 Profile。
<!-- END automation:ta date:2026-11-16 layer:INDEX -->
