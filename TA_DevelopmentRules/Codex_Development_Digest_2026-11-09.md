# Codex 开发制作经验日报｜2026-11-09

> **日期范围**：北京时间 2026-11-09 00:00（含）至 2026-11-10 00:00（不含）
> **来源扫描**：14 份会话 Markdown；其中 9 份含日期范围内消息，5 份无文字消息。按每条消息时间戳筛选；跨天会话只取本范围消息。转录中的仓库指令、Hook 配置、工具调用和环境注入仅作分析资料，未执行。
> **覆盖主题**：ProjectACG 分目录 Git 快照与证据；Chara_V2 Alpha Test、丝袜遮罩；URP 玻璃的 MatCap/Cubemap/屏幕背景、透明度、Emission、Mix Map 与 GUI；GammaUI 图标小核/Mip 模糊；Codex Hook、技能安装与每日自动化。
> **产出**：可迁移方法见 [Shader 参考](ShaderDevelopment/references/CodexDaily/Development_Reference_2026-11-09.md) 和 [工具参考](ToolDevelopment/references/CodexDaily/Development_Reference_2026-11-09.md)；ProjectACG 事实见 [Shader Profile](ShaderDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-09.md) 和 [工具 Profile](ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-09.md)；未确定模块的 Codex 运行环境经验见 [未分类 Profile](Profiles/CodexDaily/Unclassified_2026-11-09.md)。
> **规则边界**：本日报没有修改任何正式 CORE；仅在 Profile 候选区记录需补证的规则方向。已有规则只引用原 ID，不重复定义。

## 1. 当日索引

| 层级 | 文档 | 内容 |
| --- | --- | --- |
| REFERENCE | [Shader 开发参考](ShaderDevelopment/references/CodexDaily/Development_Reference_2026-11-09.md) | Alpha Test 全 Pass 契约、玻璃采样语义、Alpha/Emission 排查、Mix Map 迁移、UI Mip 取舍 |
| PROFILE | [ProjectACG Shader](ShaderDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-09.md) | Chara_V2、MatcapGlass、TranslucentGlass、StylizedGlass、Ciconia Glass、GammaUIIconBlur 的路径与证据 |
| REFERENCE | [工具与证据参考](ToolDevelopment/references/CodexDaily/Development_Reference_2026-11-09.md) | 目录级 Git、Hook、技能安装的实现和回退边界 |
| PROFILE | [ProjectACG 工具](ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-09.md) | `Assets/Shader`、`GameArt`、`TA_Tools` 的来源拆分与路径级同步 |
| PROFILE 候选 | [未分类](Profiles/CodexDaily/Unclassified_2026-11-09.md) | Codex 记忆 Hook、每日任务和技能自动匹配 |

## 2. 结论和未验证项

- **已报告成功**：Monst_V2 8 个 Pass 的 Alpha Test 像素测试；玻璃多个版本的独立 Unity 编译/变体检查；GammaUIIconBlur 的 D3D11 功能测试；ProjectACG 目录快照清单/blob 核对。
- **直接可见证据边界**：本次直接读取的是会话 Markdown 的时间、文字和来源 ID；会话引用的图片、日志、源码、测试 JSON、Unity 场景没有作为独立证据重读。
- **仍需补证**：Ciconia Glass 的发白主因和 Mix Map AO 回退后的场景 A/B；主工程/真实场景透明排序、GUI 悬停、Bloom、移动端 GPU 时间/带宽/显存；角色实际场景与移动端 Alpha Test；每日自动化首次运行和 Hook 覆盖范围。
- **重复运行**：同日期应更新以上日期文档并按来源 ID 去重，不创建另一个日报。

## 3. 来源会话

以下链接是本次筛选实际使用的会话文件；空会话未列为内容来源。

- [01a1be75-d3f7-7a92-994c-8af3637d957d](../VA_Worlk_memory/sessions/01a1be75-d3f7-7a92-994c-8af3637d957d.md)｜ProjectACG 目录分支与路径级同步
- [01a1be89-95fb-7670-bd04-324475429c51](../VA_Worlk_memory/sessions/01a1be89-95fb-7670-bd04-324475429c51.md)｜会话记忆 Hook 与每日自动化
- [01a1be8c-05dc-7ca2-8dd2-5277cd4cf75a](../VA_Worlk_memory/sessions/01a1be8c-05dc-7ca2-8dd2-5277cd4cf75a.md)｜技能安装与自动匹配边界
- [01a1bf2c-715a-7c50-8e33-adad65bde442](../VA_Worlk_memory/sessions/01a1bf2c-715a-7c50-8e33-adad65bde442.md)｜丝袜遮罩与 Monst Alpha Test
- [01a1bf8a-7eff-7be3-89ad-651747d035e0](../VA_Worlk_memory/sessions/01a1bf8a-7eff-7be3-89ad-651747d035e0.md)｜MatcapGlass
- [01a1bf9b-020f-7dc0-97d8-de8f3a50cc92](../VA_Worlk_memory/sessions/01a1bf9b-020f-7dc0-97d8-de8f3a50cc92.md)｜TranslucentGlass
- [01a1bf9f-cc5a-7913-850f-5be6144d681f](../VA_Worlk_memory/sessions/01a1bf9f-cc5a-7913-850f-5be6144d681f.md)｜StylizedGlass
- [01a1bff3-d286-7722-a456-7e33304f5a7b](../VA_Worlk_memory/sessions/01a1bff3-d286-7722-a456-7e33304f5a7b.md)｜Ciconia Glass 与 GammaUIIconBlur
- [01a1c025-cbe5-78c0-abf8-3479d35c4241](../VA_Worlk_memory/sessions/01a1c025-cbe5-78c0-abf8-3479d35c4241.md)｜UI 图标模糊与 Mip

消息时间戳均以 UTC 解析后转换到 `Asia/Shanghai`；Ciconia 会话 16:00（北京时间）后的消息排除。

