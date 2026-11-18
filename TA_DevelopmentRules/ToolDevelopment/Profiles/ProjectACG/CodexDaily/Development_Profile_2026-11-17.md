<!-- BEGIN automation:ta date:2026-11-17 layer:PROFILE -->
# ProjectACG 编辑器工具、Radial Blur 与 Git 状态 Profile｜2026-11-17

> 类型：PROFILE；项目：ProjectACG/ProjectACGMain2 相关 checkout；记录指定会话中的工具现象、配置判断和 Git 状态。本次日报没有读取项目源码或重新操作 Unity/Git。
> 关联参考：[Unity 编辑器阻塞、Renderer 配置与 Git 分叉排查](../../../references/CodexDaily/Development_Reference_2026-11-17.md)；模块入口：[ToolDevelopment](../../../README_Tech_TAToolDevelopmentRules.md)。

## 1. 项目事实与现象

- `AnimationOptimizationAutomation.Update` 因自动处理/应用独立 `.anim` 队列同步运行而占用 Unity 主线程；会话报告队列 1,034 项、13 项待处理、59 个自动写回。建议关闭 `AnimationImportCompressionSettings.json` 中自动处理，而不是只关自动应用。
- `CharacterDynamicCameraDebugWindow.MouseUp` 的忙碌提示对应镜头调试点更新、标脏和 `AssetDatabase.SaveAssetIfDirty` 保存；短暂等待可完成，长时间无变化需检查序列化/导入，强制关闭可能丢编辑器改动。
- Radial Blur 会话只确认旧的全屏 `RadialBlurRendererFeature`/材质不是 `URP-UI-Renderer` 的正式序列化配置；项目已有战斗 Renderer 的 `NBPostProcess` 径向模糊链。用户随后指定 UI 相机，但会话在完成 UI Renderer 配置前中断，未形成实现结果。
- Git 会话分析了本地 `a98788b5` merge、上游 `bf52650b57` 错误 merge 和后续 `3b3c3af095` 恢复提交。会话曾按“merge 改过且恢复提交未触及”的文件集合制作本地回退预览，数次因 HEAD 前进而重新计算，均未提交/推送。

## 2. 根因、修正与取舍

- 编辑器提示的根因是同步自动处理或资源保存占用主线程，不是编译错误；自动队列需要跨帧拆分才能获得真正可取消的行为。
- Radial Blur 不能仅凭 Inspector 截图配置；必须先决定是战斗相机还是 UI 相机，并避免与正式后处理/双重模糊链重复执行。
- merge 差异中的大量文件主要来自远端父分支整体变化；实际错误删除由上游 merge 造成，后续恢复提交已经处理大部分。整体 `git revert -m` 会再次影响已恢复内容或个人提交，因此改用文件集合预览。
- 后续个人开发应放在个人分支并推送，由负责人/PR 合并；`pull.ff=only` 只阻止隐式 merge，不改变公共分支的整合要求。

## 3. 会话报告的验证证据

| 检查 | 会话报告 | 边界 |
| --- | --- | --- |
| 自动队列 | 配置 JSON 解析/差异检查、静态代码检查通过。 | Unity 重启后的内存队列和实际动画播放未复验。 |
| CDC 忙碌提示 | 代码位置和保存调用已静态定位。 | 未在当前工程重新执行保存/导入。 |
| Radial Blur | 已核对正式 NBPostProcess 与旧 Feature 的归属差异。 | UI 相机目标未完成配置，未做画面/性能验证。 |
| Git | 读取父提交、远端更新和选择性文件集合，预览未提交/未推送。 | 工作区与远端会继续变化，旧预览文件集合不能直接复用。 |

## 4. 未验证项与回退

- Unity 工具应在重启后确认自动处理真的停止；已有自动写回动画按备份逐项检查，不能批量恢复。
- UI 相机 Radial Blur 仍需确认正确 RendererData、材质、按需开关和 RT 成本；当前没有可交付实现。
- Git 预览若不需要，用当前 HEAD 恢复对应路径；若要修复具体误删，应按文件和后续提交依赖定向处理，不能整体回退 merge。

## 来源会话

- [01a1e772-1ba0-7302-9e0c-8f66b80dd746](../../../../../VA_Worlk_memory/sessions/01a1e772-1ba0-7302-9e0c-8f66b80dd746.md)
- [01a1e84e-1e83-77a2-8e2a-a44ceb80ab00](../../../../../VA_Worlk_memory/sessions/01a1e84e-1e83-77a2-8e2a-a44ceb80ab00.md)
- [01a1e275-b375-7483-8842-f85daa399b89](../../../../../VA_Worlk_memory/sessions/01a1e275-b375-7483-8842-f85daa399b89.md)
- [01a1e7f7-1201-7c93-adf0-54f7b5b21f12](../../../../../VA_Worlk_memory/sessions/01a1e7f7-1201-7c93-adf0-54f7b5b21f12.md)

<!-- END automation:ta date:2026-11-17 layer:PROFILE -->
