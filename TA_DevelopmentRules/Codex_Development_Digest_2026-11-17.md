<!-- BEGIN automation:ta date:2026-11-17 layer:INDEX -->
# Codex 开发制作经验日报｜2026-11-17

> 类型：当日总览与索引；可迁移方法见 REFERENCE，项目状态见 PROFILE；本索引不定义强制规则。
> **日期范围**：Asia/Shanghai，2026-11-17 00:00（含）至 2026-11-18 00:00（不含）；等价 UTC 区间为 2026-11-16 16:00（含）至 2026-11-17 16:00（不含）。
> **来源筛选**：指定 `sessions/` 共 91 份 Markdown；14 份含窗口消息，共 348 条带时间戳消息。排除本自动化会话 1 份、17 条消息，以及与开发制作无关的 UGit 支持会话 1 份、3 条消息后，12 个技术会话、328 条消息进入主题整理；按消息时间筛选，不使用文件修改时间或会话开始日期。
> **覆盖主题**：ProjectACG/ProjectACGMain2 的 Unity 动画导入压缩与自动队列阻塞、Shader 变体白名单裁剪与 Android 菲涅尔参数失效、入场 Timeline/挂点与 CDC 烘焙层级、Global Volume 生命周期、Unity Renderer/Radial Blur 与 Git 分叉排查；3ds Max 矩阵动画传递、控制器和保存重载验证。
> **产出**：本索引、5 份 REFERENCE 和 6 份项目 PROFILE，共 12 份 Markdown 和 12 个文档 `.meta`；复用已有 `CodexDaily` 目录，未修改 CORE 或其他既有文档。
> **未验证项**：Unity 实际 FBX/`.anim` 重新导入、动画播放与队列跨帧取消未闭环；Shader Android 构建和设备菲涅尔效果未验；Timeline 真实生成/播放、CDC 动态子树和 Global Volume 生命周期未在 Unity EditMode/生产场景复测；Radial Blur UI Renderer 尚未形成实现；3ds Max 真实 A/B 保存重载、活动控制器和播放仍未验证。

## 1. 当日文档索引

| 层级 | 文档 | 内容与定位 |
| --- | --- | --- |
| REFERENCE | [Unity 动画导入压缩与 Editor 自动处理](AnimationDevelopment/references/CodexDaily/Development_Reference_2026-11-17.md) | FBX Importer 参数、standalone `.anim` 减帧和可暂停自动队列的分层、备份与验证。 |
| PROFILE | [ProjectACG 动画导入压缩与自动队列](AnimationDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-17.md) | `ResampleCurves`、自动处理开关、队列规模和 Unity 重导入未验证边界。 |
| REFERENCE | [3ds Max 矩阵动画批处理与保存后验证](DCCDevelopment/references/CodexDaily/Development_Reference_2026-11-17.md) | A/B 配对、Selection Set、帧/Tick、世界/父级局部矩阵和保存重载检查。 |
| PROFILE | [ProjectACG 3ds Max 动画传递工具](DCCDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-17.md) | 当前 MaxScript 重写、原 `.mse` 运行时观察、矩阵小测和真实 Rig 未闭环项。 |
| REFERENCE | [Shader 变体白名单裁剪与移动端参数失效排查](ShaderDevelopment/references/CodexDaily/Development_Reference_2026-11-17.md) | 精确 Shader 剔除、SVC 顺序、资源版本核对和 Android 运行时参数排查。 |
| PROFILE | [ProjectACG Shader 变体裁剪与 Android 菲涅尔](ShaderDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-17.md) | BaseLit 阴影关键字裁剪、Android/Windows 资源时间差和构建设备未验证项。 |
| REFERENCE | [Timeline 生成、挂点绑定与烘焙生命周期](TimelineDevelopment/references/CodexDaily/Development_Reference_2026-11-17.md) | 生成清理、挂点解析、动态层级烘焙、Global Volume 生命周期和分层验证。 |
| PROFILE | [ProjectACG 入场 Timeline 与挂点](TimelineDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-17.md) | 入场生成清理、外部角色绑定、预览角色隔离和挂点歧义边界。 |
| PROFILE | [ProjectACGMain2 CDC 与 Global Volume Timeline](TimelineDevelopment/Profiles/ProjectACGMain2/CodexDaily/Development_Profile_2026-11-17.md) | CDC 烘焙层级变化、Global Volume 状态和项目锁导致的 Unity 验证缺口。 |
| REFERENCE | [Unity 编辑器阻塞、Renderer 配置与 Git 分叉排查](ToolDevelopment/references/CodexDaily/Development_Reference_2026-11-17.md) | 主线程队列、UI Renderer/Radial Blur 归属、Git merge 分叉和选择性回退检查。 |
| PROFILE | [ProjectACG 编辑器工具、Radial Blur 与 Git 状态](ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-17.md) | 动画自动队列、Radial Blur 未完成配置及本地 merge/回退分析状态。 |

## 2. 来源会话

- [01a1e26f-2645-7ce2-8a4d-1764c762504d](../VA_Worlk_memory/sessions/01a1e26f-2645-7ce2-8a4d-1764c762504d.md)：3ds Max 2024 批量骨骼动画传递、矩阵模式、原工具观察和保存重载问题。
- [01a1e275-b375-7483-8842-f85daa399b89](../VA_Worlk_memory/sessions/01a1e275-b375-7483-8842-f85daa399b89.md)：Unity Renderer/Radial Blur 归属判断与未完成的 UI 相机配置。
- [01a1e7bf-c3c1-71c1-b9a2-8c42ced0ee84](../VA_Worlk_memory/sessions/01a1e7bf-c3c1-71c1-b9a2-8c42ced0ee84.md)：入场 Timeline 生成、复制落盘、覆盖清理与校验顺序。
- [01a1e7da-77bf-7e52-9c6c-eb36d5efe6ea](../VA_Worlk_memory/sessions/01a1e7da-77bf-7e52-9c6c-eb36d5efe6ea.md)：100601 Prop 挂点跟随与预览角色绑定歧义排查。
- [01a1e7f7-1201-7c93-adf0-54f7b5b21f12](../VA_Worlk_memory/sessions/01a1e7f7-1201-7c93-adf0-54f7b5b21f12.md)：Git merge 分叉、恢复提交和选择性回退预览。
- [01a1e772-1ba0-7302-9e0c-8f66b80dd746](../VA_Worlk_memory/sessions/01a1e772-1ba0-7302-9e0c-8f66b80dd746.md)：Unity 动画自动减帧队列占用主线程及关闭自动处理的边界。
- [01a1e781-5896-78e1-855d-90617274c69f](../VA_Worlk_memory/sessions/01a1e781-5896-78e1-855d-90617274c69f.md)：FBX/standalone `.anim` 压缩设置、`ResampleCurves` 和预设快照。
- [01a1e7a5-c0bf-7c52-ae82-7689467fca0d](../VA_Worlk_memory/sessions/01a1e7a5-c0bf-7c52-ae82-7689467fca0d.md)：未纳入主题整理的 UGit 推送报错支持，不属于开发制作日报。
- [01a1e84e-1e83-77a2-8e2a-a44ceb80ab00](../VA_Worlk_memory/sessions/01a1e84e-1e83-77a2-8e2a-a44ceb80ab00.md)：`CharacterDynamicCameraDebugWindow` 忙碌提示与自动队列阻塞观察。
- [01a1e864-8076-77c2-afee-2dfbff033201](../VA_Worlk_memory/sessions/01a1e864-8076-77c2-afee-2dfbff033201.md)：CDC Timeline 烘焙时动态子树改变层级的排查与修正尝试。
- [01a1e8fa-53c6-7570-8796-c6ec61c5b961](../VA_Worlk_memory/sessions/01a1e8fa-53c6-7570-8796-c6ec61c5b961.md)：BaseLit/NBShader 变体统计、精确关键字强制剔除和 SVC 顺序。
- [01a1e9c1-34a4-7250-a700-d73984e74c70](../VA_Worlk_memory/sessions/01a1e9c1-34a4-7250-a700-d73984e74c70.md)：Global Volume Timeline 生命周期、临时状态和验证边界。
- [01a1ea3a-4be0-74a3-a981-0f1fe3627403](../VA_Worlk_memory/sessions/01a1ea3a-4be0-74a3-a981-0f1fe3627403.md)：Android 资源版本差异与 `CharacterDissolveEffect` 菲涅尔参数失效排查。

会话报告中的编译、静态检查、矩阵小测、资源清单和 Git 分析均按来源报告记录；本次没有读取项目源码、场景、SVC、Max 文件或测试日志，也没有重新运行 Unity、Android 构建或 3ds Max。UGit 支持会话在来源清单中用于说明排除原因；本自动化会话未列入技术来源，二者均不进入技术主题统计。
<!-- END automation:ta date:2026-11-17 layer:INDEX -->
