<!-- BEGIN automation:ta date:2026-11-12 layer:INDEX -->
# Codex 开发制作经验日报｜2026-11-12

> 类型：当日总览与索引；经验正文分别放入 REFERENCE 与 PROFILE，本索引不定义强制规则。
> **日期范围**：Asia/Shanghai，2026-11-12 00:00（含）至 2026-11-13 00:00（不含）；等价 UTC 区间为 2026-11-11 16:00（含）至 2026-11-12 16:00（不含）。
> **来源扫描**：指定目录共 50 份 Markdown；11 份含区间消息，共解析 324 条。排除 1 个旧日报自动化会话（10 条）、8 条仓库规则注入和 3 条上下文摘要，10 个技术会话共 303 条进入主题分析；进度与重复迭代按主题合并。
> **覆盖主题**：FlowMap Shader 与网格绘制工具、Shader 半精度边界、动画曲线压缩、Timeline 材质替换与动态绑定、Timeline 失效绑定清理、FBX Prefab 改名/删除同步、模型名隐藏 Unicode 字符及道具节点引用。
> **产出**：本总览、4 份 REFERENCE、4 份 ProjectACG PROFILE，共 9 份 Markdown；新增 9 个文档 `.meta` 和 3 个目录 `.meta`。未修改 CORE。
> **未验证项**：FlowMap 高速/高强度循环显隐问题仍在排查；网格绘制工具读取已有材质属性并载入贴图的需求尚未实现；角色实机/移动端、Timeline 最终播放、若干 Unity EditMode 回归及绘制工具主工程交互仍未验证。

## 1. 当日文档索引

| 层级 | 文档 | 内容与定位 |
| --- | --- | --- |
| REFERENCE | [FlowMap 与 Shader 精度优化](ShaderDevelopment/references/CodexDaily/Development_Reference_2026-11-12.md) | FlowMap 采样/循环/贴图分层，以及半精度候选的风险和 A/B 验证方式。 |
| PROFILE | [ProjectACG Shader 与 FlowMap](ShaderDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-12.md) | `Chara_Cloth_V2` 实际精度调整、`Effect_FlowMap` 最终功能与验证状态。 |
| REFERENCE | [Editor 资产生成与绘制工具](ToolDevelopment/references/CodexDaily/Development_Reference_2026-11-12.md) | Prefab 生成器状态核对、孤儿绑定清理、Scene 绘制性能诊断与隔离验证。 |
| PROFILE | [ProjectACG 工具问题与 FlowMap Painter](ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-12.md) | FBX/Prefab、悬浮道具引用、Timeline 保存工具及 FlowMap Painter 的具体实现状态。 |
| REFERENCE | [动画曲线压缩与保真](AnimationDevelopment/references/CodexDaily/Development_Reference_2026-11-12.md) | 恒值曲线简化、Scale 删除边界、误差验证和资源大小口径。 |
| PROFILE | [ProjectACG 动画压缩工具](AnimationDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-12.md) | 压缩工具选项、武器节点保护、Undo、中文界面和隔离测试状态。 |
| REFERENCE | [Timeline 材质替换与动态绑定](TimelineDevelopment/references/CodexDaily/Development_Reference_2026-11-12.md) | 角色绑定、按 Renderer 聚合写回、材质引用恢复与实例化边界。 |
| PROFILE | [ProjectACG Timeline 变更](TimelineDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-12.md) | 材质替换 Track、Transition Asset 绑定修复、隐藏武器开关和未解运行问题。 |

沿用 Shader `CMP-01`、`VAL-03`，工具 `TOOL-CMP-01`、`TOOL-OPS-02`、`TOOL-VAL-02`，动画 `ANM-CORE-06`、`ANM-CORE-07`，Timeline `TML-DOC-01`、`TML-ARC-03`、`TML-VAL-01`；本日报不重复定义这些规则。

## 2. 来源、日期筛选与主题

来源文件未提供可确认的会话标题，以下用会话 ID 标识。消息按各自时间戳转换到北京时间筛选，跨日内容仅取区间内消息；自动化提示、仓库规则注入和上下文摘要仅用于过滤，不作为知识来源。

| 来源会话 ID | 纳入主题 |
| --- | --- |
| [01a1cab4-df21-72c3-ba9b-5c0157ab2b5a](../VA_Worlk_memory/sessions/01a1cab4-df21-72c3-ba9b-5c0157ab2b5a.md) | FBX Mesh 改名/删除后的 Prefab 节点同步及旧 Mesh 引用处理。 |
| [01a1cdb0-8509-78c2-8c72-d97abaa8006a](../VA_Worlk_memory/sessions/01a1cdb0-8509-78c2-8c72-d97abaa8006a.md) | `Chara_V2` 精度分析、`Chara_Cloth_V2` 半精度改动和剩余降精度候选。 |
| [01a1ce6c-9a06-75f3-a87c-2e48e35153ea](../VA_Worlk_memory/sessions/01a1ce6c-9a06-75f3-a87c-2e48e35153ea.md) | FBX 内名称的零宽字符、`ModelNodeReferences` 自动生成和引用诊断。 |
| [01a1cec6-dec3-7260-9d98-b069c5285e29](../VA_Worlk_memory/sessions/01a1cec6-dec3-7260-9d98-b069c5285e29.md) | Timeline 隐藏战斗武器字段、模型初始化异常及最终开关约束。 |
| [01a1cf40-ee85-79b3-af3a-15bf06a6a14d](../VA_Worlk_memory/sessions/01a1cf40-ee85-79b3-af3a-15bf06a6a14d.md) | 角色材质替换 Track/Clip、多 Mesh 多槽、动态绑定与运行失败定位。 |
| [01a1cf8d-7fc4-74f1-b72e-667b9a007a2c](../VA_Worlk_memory/sessions/01a1cf8d-7fc4-74f1-b72e-667b9a007a2c.md) | 动画压缩行为、恒值曲线关键帧精简、Undo 和本地化。 |
| [01a1cfd8-0052-70e3-b54e-213cc45ec6fd](../VA_Worlk_memory/sessions/01a1cfd8-0052-70e3-b54e-213cc45ec6fd.md) | 保存 Timeline Prefab 时移除失效 Track 绑定记录。 |
| [01a1d01e-0add-7fb0-ac81-a7080e7ce2ae](../VA_Worlk_memory/sessions/01a1d01e-0add-7fb0-ac81-a7080e7ce2ae.md) | Unity/UE4 FlowMap 案例与现有绘制工具参考链接。 |
| [01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe](../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md) | `Effect_FlowMap` 功能扩展、材质示例、双层 Lerp 和循环问题。 |
| [01a1d02f-f5cf-7c83-8274-b18802855696](../VA_Worlk_memory/sessions/01a1d02f-f5cf-7c83-8274-b18802855696.md) | Mesh FlowMap Painter、笔刷库、性能测试及尚未完成的已有材质贴图载入。 |

另有会话 [01a1cd80-4220-7221-a6b1-2be1742f3e43](../VA_Worlk_memory/sessions/01a1cd80-4220-7221-a6b1-2be1742f3e43.md) 是前一日日报自动化，整份排除以避免递归总结。

## 3. 结果阅读边界

- 会话报告的编译、隔离测试、GPU 检查和材质预览均按原报告记录；本次没有访问项目源码、运行 Unity 或重做渲染测试。
- `Chara_Cloth_V2` 半精度修改没有目标设备逐像素对比或 iOS 性能数据；半精度不等于画面无差异或性能提升。
- FlowMap Shader 的 512 编译组合、43 项 GPU 检查和六组示例材质结果来自隔离检查；真实角色 UV、动画与手机效果未验证。
- FlowMap Painter 有独立 GPU/编译检查和一次绘制耗时测量，但主工程 UI 交互未验证；同步 PNG 备份的高分辨率停顿仍存在，且现有材质贴图属性载入功能尚未实现。
- Timeline Track 的普通绑定回归曾通过；修复包装后的 Transition Asset 搜索后未再运行 Unity EditMode 或实际大招播放，也没有 Profiler 基准。
- FBX 与 Timeline 保存工具有程序集编译和部分静态/测试报告；相关 Unity EditMode 测试未执行。强制删除仍被引用的 Mesh 会让其他资源产生 Missing 引用。
- 动画优化工具隔离 EditMode 测试报告 38/38 通过；实际角色动作、视觉差异和构建/运行内存变化未在当日验证。

<!-- END automation:ta date:2026-11-12 layer:INDEX -->
