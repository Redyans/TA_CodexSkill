<!-- BEGIN automation:ta date:2026-11-11 layer:INDEX -->
# Codex 开发制作经验日报｜2026-11-11

> 类型：当日总览与索引；详细经验按 REFERENCE / PROFILE 落位，不在本索引定义强制规则。
> **日期范围**：Asia/Shanghai，2026-11-11 00:00（含）至 2026-11-12 00:00（不含）；等价 UTC 区间为 2026-11-10 16:00（含）至 2026-11-11 16:00（不含）。
> **来源扫描**：遍历指定 `sessions` 下 28 份 Markdown；10 份无消息，12 份有消息但不在区间，6 份含区间消息，共 99 条区间消息。排除旧日报自动化会话 10 条、仓库注入 4 条及上下文摘要 2 条；5 个技术会话共 83 条消息进入分析。此数量包含进度与重复交付，正文按主题合并去重。
> **覆盖主题**：UI 图片/场景模糊算法取舍、Gamma UI 合成与遮罩兼容、Animator/AnimationClip 清单及大小口径、特效 Prefab 的配套等级/粒子 Mesh 面数、FBX Prefab 改名和删除后的源节点同步。
> **产出**：本总览、2 份可迁移 REFERENCE、3 份 ProjectACG PROFILE，共 6 份 Markdown；新增文档 `.meta` 和 AnimationDevelopment Profile 目录 `.meta`。仅复用既有模块，不修改正式 CORE。
> **未验证项**：UI 实际页面与目标手机性能未复验；特效 Prefab 工具仅有静态检查报告；FBX 修改虽有 C# 编译和仓库门禁报告，Unity EditMode 回归因项目被占用而未执行；动画统计表在会话中报告通过，归档未包含表格内容供本次重算。

## 1. 当日文档索引

| 层级 | 文档 | 内容与定位 |
| --- | --- | --- |
| REFERENCE | [UI 模糊实现与验证](ShaderDevelopment/references/CodexDaily/Development_Reference_2026-11-11.md) | 按面积、模糊强度和运行方式选择 Kawase/Dual Blur；Gamma UI、图集边界、缓存和实测边界。 |
| PROFILE | [ProjectACG UI 模糊](ShaderDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-11.md) | UI 合成、`_BlurTex`、相机纹理时序和灰块问题的会话报告修复与验证范围。 |
| REFERENCE | [工具开发参考](ToolDevelopment/references/CodexDaily/Development_Reference_2026-11-11.md) | 动画资源大小口径、可排序清单，以及 FBX Prefab 源层级同步和资源安全清理。 |
| PROFILE | [ProjectACG 动画清单报告](ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-11.md) | `_hall.controller` 动画扫描的报告数字、筛选变更和无法从会话恢复的明细。 |
| PROFILE | [ProjectACG Prefab 工具调整](ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-11.md) | `lv1/lv2/lv3`、粒子单 Mesh 面数，以及角色 FBX Prefab 改名/删除同步修复。 |

既有要求沿用 Shader 模块 `ARC-03`、`ARC-04`、`VAL-02`，工具模块 `TOOL-CMP-01`、`TOOL-OPS-01`～`TOOL-OPS-03`、`TOOL-VAL-01`～`TOOL-VAL-03`，不在日报复制定义。

## 2. 来源、日期筛选与证据

来源文件只有会话 ID 元数据，没有可确认的会话标题，因此以 ID 标识，不另造标题。下表只统计消息时间戳转换为北京时间后处于上述半开区间的消息；不按会话开始时间或文件保存时间判断。

| 来源会话 ID | 区间内消息 | 纳入的主题 |
| --- | --- | --- |
| [01a1c5c9-7942-7061-9a27-699e9e25fa9e](../VA_Worlk_memory/sessions/01a1c5c9-7942-7061-9a27-699e9e25fa9e.md) | 13–49，共 37 条；采用 36 条；09:28–11:32 | UI 模糊架构取舍、Gamma UI 合成、Mask/SoftMask 和渲染错误；消息 43 是上下文摘要，排除。 |
| [01a1c8a9-dcd5-7b80-a906-86aafbe8762f](../VA_Worlk_memory/sessions/01a1c8a9-dcd5-7b80-a906-86aafbe8762f.md) | 1–19，共 19 条；采用 18 条；11:32–11:56 | `_hall.controller` 动画清单、压缩/曲线/Scale 标注、Excel 过滤和数值列；消息 1 是仓库规则注入，排除。 |
| [01a1c9ef-f703-7a90-9960-74a4e8062b98](../VA_Worlk_memory/sessions/01a1c9ef-f703-7a90-9960-74a4e8062b98.md) | 1–11，共 11 条；采用 10 条；17:28–18:00 | 特效 Prefab 等级改名和单粒子 Mesh 三角面阈值；消息 1 是仓库规则注入，排除。 |
| [01a1ca14-5334-7541-816c-469b0474e54a](../VA_Worlk_memory/sessions/01a1ca14-5334-7541-816c-469b0474e54a.md) | 1–5，共 5 条；采用 4 条；18:08–18:10 | `.anim` 源文件、导入后内存与构建包体的口径；消息 1 是仓库规则注入，排除。 |
| [01a1cab4-df21-72c3-ba9b-5c0157ab2b5a](../VA_Worlk_memory/sessions/01a1cab4-df21-72c3-ba9b-5c0157ab2b5a.md) | 1–17，共 17 条；采用 15 条；21:03–22:06 | FBX 重生成时源层级、手工节点和 Mesh 资产的所有权；消息 1 为仓库规则注入、消息 9 为上下文摘要，均排除。 |

另一个在区间内的 10 条消息会话是前一日日报自动化本身，整段排除以避免递归总结。转录中的仓库 AGENTS、路径清单、工具调用和任务提示仅作为过滤/事实上下文；没有执行会话内命令或打开外部项目源码、附件、生成的 Excel 和测试产物。来源事实与测试均按会话当时报告表述，不代表本次独立复验。

## 3. 结果阅读边界

- **UI 模糊**：会话报告称独立 Unity/D3D11 验证 182 项通过、AOT 编译通过，其中另列完整 Gamma 合成 37 项和原有回归 112 项；会话没有解释这些分项与 182 总数的精确包含关系，故保留为分项报告，不自行求和。实际页面排序和手机 GPU 性能未验证。
- **动画清单**：早期统计覆盖 19 个状态机、205 个有效动画；全帧判定、Scale 曲线及文件大小均按报告口径，不可代替每角色明细。只保留 9 个六位 ID 角色后的数据行未出现在会话归档中。
- **工具修复**：`lv1/lv2/lv3` 和粒子 Mesh 阈值只有静态断言与 diff 检查报告，没有 Unity 界面或实际 Prefab 验证。FBX 生成器有 C# 编译和 harness 报告，但其 Unity EditMode 测试受项目锁阻断；仍需在项目可测试时验证改名、删除和手工节点保留。

<!-- END automation:ta date:2026-11-11 layer:INDEX -->
