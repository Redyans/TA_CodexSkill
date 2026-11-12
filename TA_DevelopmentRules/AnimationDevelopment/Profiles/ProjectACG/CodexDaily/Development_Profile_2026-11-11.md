<!-- BEGIN automation:ta date:2026-11-11 layer:PROFILE -->
# ProjectACG Animator 动画清单报告经验｜2026-11-11

> 类型：PROFILE；范围：北京时间 2026-11-11 00:00（含）至 2026-11-12 00:00（不含）。来源工作目录为 `ProjectACGMain/ProjectACG`；本次只读取指定 sessions，不读取 Excel、项目动画或源码。
> 可迁移盘点方式见 [动画资源盘点参考](../../../../ToolDevelopment/references/CodexDaily/Development_Reference_2026-11-11.md)。动画模块规则见 [AnimationDevelopment 入口](../../../README_Tech_TAAnimationDevelopmentRules.md)的 `ANM-CORE-07`；本例不修改 CORE。

## 1. `_hall.controller` 动画盘点

**项目事实/现象**：用户请求盘点 `Assets/AssetRaw/character/hero` 下 `_hall.controller` 的动画数量、压缩标记、是否“全帧”、Scale K 帧和资源大小，并要求 Excel 各项可独立排序。来源：[会话 01a1c8a9-dcd5-7b80-a906-86aafbe8762f](../../../../../VA_Worlk_memory/sessions/01a1c8a9-dcd5-7b80-a906-86aafbe8762f.md)，消息 2–19。

**项目范围与报告数字（会话报告）**：初版扫描找到 19 个 `_hall.controller`，其中含角色和道具；有效引用 205 个动画，源 `.anim` 文件合计约 7.50 GiB。报告称压缩标记全部关闭；107 个按工具判定符合全帧；205 个均有 Scale K 帧，其中 52 个 Scale 值恒定、153 个有变化。早期命名资源的 GUID 缺失单独列出。后续将结果收窄为 9 个六位数字角色本体，生成了 MB 保留两位小数的修订版；会话未保留这 9 个角色各自的行值，故本 Profile 不补写角色明细或最大动画大小。

**判定边界**：会话将“全帧”描述为检查曲线是否逐帧有关键帧，但没有给出各曲线采样端点、帧率差异和容差规则，107 项只是当时报告分类，不能泛化为动画质量或压缩结论。“Scale 有 K 帧”与“Scale 实际变化”是分开的；52 个恒定值示例说明二者不能合并成一个布尔项。

## 2. 文件大小口径与 Excel 修改

**项目事实/现象**：`.anim` 资源 Inspector 的数据大小与磁盘文件大小相差明显。来源：[会话 01a1ca14-5334-7541-816c-469b0474e54a](../../../../../VA_Worlk_memory/sessions/01a1ca14-5334-7541-816c-469b0474e54a.md)，消息 2–5。

**报告结论（会话答复）**：磁盘 `.anim` 文件大小用于源文件体积；Inspector 的动画数据大小更接近导入后数据量参考。运行时准确占用需用 Unity Profiler/Memory Profiler 检查已加载 `AnimationClip`；包体影响需读取实际 AssetBundle/Addressables 或 Player 构建报告，因为导入压缩、打包压缩和依赖都会改变结果。此项没有在本次重新实测。

**交付修改（会话报告）**：初版 Excel 有状态机汇总、动画明细、缺失引用三页，并把数值型大小/数量列设为可筛选排序；随后按用户要求仅保留 9 个六位 ID 角色，并把 MB 显示改为两位小数。原文件被占用时另存为修订版。会话报告称修订表可按数值排序；表格文件及内容本次未读取，因此不能独立核验公式、数据行或最终排序类型。

## 3. 未验证项与候选

- 缺少 9 个角色的最终明细，无法记录单角色动画数、合计大小或最大动画大小，也无法验证用户最初要求的列值。
- 未做实际构建包体对照、Profiler 内存采样或目标设备运行测试；任何这些结论都不能从 7.50 GiB 源文件合计推算。
- “全帧”算法定义、压缩字段读取方式与采样误差范围缺少会话细节，须在项目工具/脚本重新检查后再作为可复用判据。

<!-- END automation:ta date:2026-11-11 layer:PROFILE -->
