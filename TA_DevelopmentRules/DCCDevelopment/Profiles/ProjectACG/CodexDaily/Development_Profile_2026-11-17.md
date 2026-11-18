<!-- BEGIN automation:ta date:2026-11-17 layer:PROFILE -->
# ProjectACG 3ds Max 动画传递工具 Profile｜2026-11-17

> 类型：PROFILE；项目：ProjectACG 相关 DCC 工具；只记录指定会话报告的文件、脚本、运行时观察和验证状态。本次日报没有读取项目源码或 Max 场景。
> 关联参考：[3ds Max 矩阵动画批处理参考](../../../references/CodexDaily/Development_Reference_2026-11-17.md)；模块入口：[DCCDevelopment](../../../README_Tech_DCCDevelopmentRules.md)。

## 1. 项目事实与现象

- 批处理脚本为 `G:/Connect/ConnectDocs/Doc/WorkDoc/Repor/Work_word/Valkyria/DCC_AI/暴力帧拷贝粘贴_批处理.ms`，需求包括 A/B 同名配对、Selection Set 骨骼白名单、姿态/动作传递、配置保存、结构诊断和覆盖 B。
- 旧实现曾把 ticks 当帧设置，日志出现 `19840f` 或错误的 `0–24000` 范围；又曾把 A 的绝对姿势直接写入 B，导致基准姿势破坏。会话后来改为按官方 `TimeValue.frame` 读取帧，并将帧范围和写键时间分开。
- 当前脚本在会话报告中改为完整 `node.transform` 矩阵传递，增加逐帧 `S` / 源关键帧 `K` 模式；父级局部模式根据 B 父级还原世界矩阵，保留缩放；仍保留文件配对、Selection Set 缓存、层级匹配、覆盖保存和详细日志。
- A/B 结构检查会记录缺失、重名、父级、对象类型和 Position/Rotation/Scale Controller 差异。新增按层级路径忽略名称的开关默认关闭；歧义映射跳过并写入日志。

## 2. 原工具对照与根因

- 原始 `a暴力帧拷贝粘贴脚本_V1.191.mse` 是 32235 bytes、熵约 7.994 的加密/编译 MaxScript，静态字符串不能恢复源码。
- 通过已打开的 3ds Max 2024 与 `ScriptSender.exe -file`，会话报告确认原工具窗口标题为 `CopyPasteKey V1.19`，可观察到 `Pose`、`Motion`、`Acc`、起止帧、`XYZ`、`TCB`、`Offset`、`U`、`Copy`、`Paste` 等控件；运行时发现宿主的 `copyPasteKeys` 和 Biped 接口，但没有确认原工具的完整调用链。
- 因此当前批处理与原工具是功能目标相近的重写，不是逐行复刻；`Acc`、`XYZ`、`TCB`、`Offset`、`U` 的精确语义未确认。
- 同日分析的 `cgjoy-cptools.ms` 使用节点名和 `transform` 矩阵序列化到 `.bps`，K 模式写入 `frame:` 与每个采样帧，再按名称写回关键帧。会话报告称父级局部矩阵小测误差约 `7.6e-06`，位置/旋转控制器各生成 2 个键，采样时间为 `0、5、10`；真实 A/B 批量场景尚未执行。

## 3. 会话报告的验证证据

| 检查 | 会话报告 | 边界 |
| --- | --- | --- |
| Autodesk API | 核对了 `TimeValue.frame`、`deleteKeys ... #allKeys`、`getSubAnim`、`coordsys parent/world` 和节点变换文档。 | 未在生产 A/B 场景复跑。 |
| MaxScript 矩阵小测 | `cgjoy-cptools.ms` 改造后的脚本可在 Max 2024 加载；局部矩阵误差约 `7.6e-06`，关键帧采样与写入通过。 | 仅是临时对象/小型测试，不代表真实 Rig。 |
| 原 `.mse` 加载 | 通过已运行 Max 的 ScriptSender 观察到 UI 加载。 | 未完成无鼠标 Copy/Paste 按钮行为探针，未恢复源码。 |
| 静态检查 | 批处理脚本括号/字符串结构检查通过。 | 静态通过不等于 B 文件重载或播放通过。 |

## 4. 未验证项与回退

- 当前未完成真实 A/B 批处理后的保存、重载、播放闭环；没有证据证明所有目标骨骼在实际 Rig 上使用同一活动 Controller/Animation Layer。
- 原始 `.mse` 的内部算法仍不能确认；内存扫描到的字符串不能作为完整源码恢复证据。
- 运行测试应使用 `G:\新建文件夹GG\源文件` 与 `G:\新建文件夹GG\同骨骼测试` 的干净副本，先检查日志中的矩阵误差、首末帧和逐通道变化，再决定是否覆盖 B。
- 发生姿势错位、无键或不播放时，恢复未被旧脚本写入的 B 副本；不要对已经多次覆盖的 B 继续叠加测试。

## 来源会话

- [01a1e26f-2645-7ce2-8a4d-1764c762504d](../../../../../VA_Worlk_memory/sessions/01a1e26f-2645-7ce2-8a4d-1764c762504d.md)

<!-- END automation:ta date:2026-11-17 layer:PROFILE -->
