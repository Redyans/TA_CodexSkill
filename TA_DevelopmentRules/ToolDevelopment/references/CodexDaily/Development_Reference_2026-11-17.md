<!-- BEGIN automation:ta date:2026-11-17 layer:REFERENCE -->
# Unity 编辑器阻塞、Renderer 配置与 Git 分叉排查参考｜2026-11-17

> 类型：REFERENCE；适用范围：编辑器工具主线程占用、RendererFeature 归属判断和 Git 分叉/误合并分析；使用前提：按目标工程、工具版本、远端权限和当前工作区状态重新验证。
> 关联既有规则：`TOOL-OPS-01`、`TOOL-VAL-02`、`TOOL-VAL-03`；模块入口：[ToolDevelopment](../../README_Tech_TAToolDevelopmentRules.md)。项目事实见 [ProjectACG Tool Profile](../../Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-17.md)。

## 1. 适用场景

- Unity 窗口显示某个 Editor callback 正在执行，主线程长时间无响应。
- 需要判断一个 Blur/Radial Blur RendererFeature 应挂到战斗 Renderer 还是 UI Renderer。
- Git 客户端生成了未预期的 merge，需要确认两个父提交、后续修复和当前未提交修改，再做本地预览。

## 2. 前提与依赖

- 编辑器阻塞排查要有 callback 名、日志、任务队列/状态文件和备份；不要只根据截图猜耗时步骤。
- Renderer 配置要核对相机、RendererData、已有后处理链、Feature 激活状态、材质绑定和全屏 RT 成本。
- Git 操作前记录当前分支、HEAD、远端引用和工作区状态；已有未提交修改时禁止直接 reset、切分支或整段回退。

## 3. 实现或排查步骤

1. 主线程问题先判断是编译错误、同步导入、资源保存还是自动队列；记录队列规模、当前任务和最近写回数量。只关闭“自动应用”通常不足以停止候选生成和重导入。
2. 需要长期保留自动处理时，把采样、候选、验证、写回和导入刷新拆成跨帧任务；取消检查放在可让出主线程的边界。当前同步调用无法被界面按钮中断，强制结束前先检查备份。
3. 配置 RendererFeature 前，先查项目是否已有正式后处理链。旧的全屏 Radial Blur Feature 若没有资源、按需开关或正确 Renderer 归属，直接挂到 UI Renderer 可能和 Gamma UI/Dual Blur 重复采样。
4. Git 分叉时使用个人分支和 PR；`git pull --ff-only` 或本地 `pull.ff=only` 只能阻止隐式 merge，不能让分叉的公共 `master` 只接收个人提交。需要时由负责人整合，而不是强推覆盖远端。
5. 分析 merge 先看两个父提交、共同祖先、`merge^1..merge` 与 `merge^2..merge` 的差异，再核对后续修复提交。想预览选择性回退时按文件集合生成工作区差异，不提交、不推送，并保留恢复命令。
6. 看到“几百个文件差异”时，不要直接等同于 merge 自己删除了几百个文件；需要区分远端父分支整体差异、merge 结果和后续恢复提交。

## 4. 风险与不适用边界

- Editor “正在忙”提示没有证明代码卡死；等待、同步导入、序列化保存和资源刷新有不同回退风险。
- 没有实际目标相机/Renderer/Feature 的配置证据时，不能保证 Radial Blur 能在 UI 相机生效，也不能把旧 Feature 当作正式后处理链。
- `pull.ff=only` 不会防止用户或 GUI 显式执行 merge；强制推送会覆盖他人历史，不适合作为“只保留我的提交”的捷径。
- 机械地把 merge 前文件恢复到工作区，可能撤掉后续正常提交；选择性预览必须再次检查当前 HEAD 和文件是否仍存在。

## 5. 验证与回退

- Editor：关闭自动入口后重启并检查队列，代表资源手动执行一次，再确认 UI/场景恢复；把“编译通过”和“实际播放”分开。
- Renderer：确认 Feature 的 RendererData、材质、相机筛选、RT 尺寸和帧调试阶段，随后用 GameView/Profiler/目标设备验证。
- Git：刷新远端引用后记录 HEAD/远端差异；预览只存在工作区，检查完用 `git restore --source=HEAD --worktree -- <paths>` 恢复，不直接提交预览。

## 来源与证据边界

- [动画自动队列阻塞会话](../../../../VA_Worlk_memory/sessions/01a1e772-1ba0-7302-9e0c-8f66b80dd746.md)
- [CharacterDynamicCameraDebugWindow 忙碌提示会话](../../../../VA_Worlk_memory/sessions/01a1e84e-1e83-77a2-8e2a-a44ceb80ab00.md)
- [Radial Blur 配置会话](../../../../VA_Worlk_memory/sessions/01a1e275-b375-7483-8842-f85daa399b89.md)
- [Git merge 分叉与选择性回退会话](../../../../VA_Worlk_memory/sessions/01a1e7f7-1201-7c93-adf0-54f7b5b21f12.md)

本参考只总结会话中已报告的排查方法；本次日报没有读取项目源码、远端仓库或重新运行 Unity/Git 操作。
<!-- END automation:ta date:2026-11-17 layer:REFERENCE -->
