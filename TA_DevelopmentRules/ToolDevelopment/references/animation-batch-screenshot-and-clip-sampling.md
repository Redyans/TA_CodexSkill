---
name: animation-batch-screenshot-and-clip-sampling
description: Unity Editor 动画批量截图、静态 Pose 识别、短 Clip 代表帧采样与输出验证参考。
---

# 动画批量截图与短 Clip 采样参考

> 类型：REFERENCE；适用范围：Unity Editor 中将 `AnimationClip` 批量采样并渲染为动作识别截图的工具；使用前必须按目标项目代码、Unity 版本、Prefab、相机和资源导入设置重新验证。

## 1. 适用场景

- 需要为动画文件夹批量生成 PNG 缩略图，帮助美术或程序快速识别动作内容。
- 文件夹中同时存在定帧 Pose、只有几帧的短动画和普通长动画。
- 一个 FBX 内可能包含多个 `AnimationClip`，不能只按文件路径视为一个 Clip。
- 工具已有“手动指定采样帧”的行为，需要增加智能模式但不能破坏旧输出。

## 2. 前提与依赖

- 工具运行在 Unity Editor 程序集，不进入 Player。
- 输入至少包括目标模型 Prefab、截图 Camera、动画目录、输出分辨率和角度列表。
- 必须先明确输出策略：输出目录、文件名、覆盖确认、是否过滤长动画、是否允许取消。
- 代表帧只是缩略图策略，不等价于动作完整预览；需要完整动作判断时仍应使用 Animation Window、预览工具或视频导出。

## 3. 实现或排查步骤

### 3.1 先冻结旧行为，再增加模式

推荐将功能分为两个显式模式：

1. 兼容模式：处理原有 Clip 集合，继续使用用户填写的采样帧。
2. Pose / 短动画模式：只处理静态 Pose 和不超过阈值的短 Clip，并自动选择代表帧。

新模式默认关闭。关闭时不要偷偷改变输出范围、手动帧含义或文件名，便于旧批处理结果回归比较。

### 3.2 按 Clip 收集资源，不要只按路径收集

`AssetDatabase.FindAssets("t:AnimationClip")` 得到的是资源 GUID/路径集合，不保证一个路径只有一个 Clip。对每个唯一资产路径使用 `AssetDatabase.LoadAllAssetsAtPath()`，再筛选 `AnimationClip` 子资产；同时排除导入器生成的预览 Clip，并保留 Clip 与资产路径的对应关系。

这样可以覆盖独立 `.anim` 主资产、FBX 中的多个动画子资产，以及同名 Clip 位于不同资源路径的情况。输出文件名仍需处理非法字符和同名覆盖；生成前应提示覆盖，执行结束汇总成功、跳过和失败数量。

### 3.3 用 Clip 自己的帧率计算帧数和时间

不要把全局 `30 FPS` 当作所有 Clip 的采样率。推荐计算：

```text
frameRate = clip.frameRate > 0 ? clip.frameRate : fallbackFrameRate
lastFrameIndex = max(0, round(clip.length * frameRate))
frameCount = lastFrameIndex + 1
sampleTime = clamp(sampleFrame / frameRate, 0, clip.length)
```

`frameCount` 的定义必须在 UI 文案中说明是否包含第 0 帧。对 0 时长 Pose，至少保留 1 帧；对 30/60 FPS 混合资源，必须用各自 `frameRate` 换算。

### 3.4 静态 Pose 与短动画分类

短动画分类使用显式可调阈值，例如最多 10 帧。仅按时长无法覆盖“保持 5 秒但姿态不变”的定帧 Pose，因此可在 Editor 中检查：

- `AnimationUtility.GetCurveBindings()` 得到的数值曲线是否在容差内保持不变；
- `AnimationUtility.GetObjectReferenceCurveBindings()` 得到的对象引用是否始终相同。

存在至少一个动画值且所有值稳定时，可标记为静态 Pose。没有曲线的 Clip 不应仅凭名称认定为 Pose；它是否纳入短 Clip 由帧数阈值决定。

### 3.5 代表帧选择

- 静态 Pose：采样第 0 帧，结果稳定且成本最低。
- 非静态短 Clip：优先采样中间帧；对只有 2 帧的 Clip 应避免再次落到第 0 帧。
- 长 Clip：继续使用用户配置的采样帧，或另行设计动作峰值/多帧 contact sheet，不要把中间帧启发式扩展成所有动画的唯一语义。

代表帧算法应集中在独立方法中，先得到 `sampleFrame`，再由统一截图流程执行，避免 UI、过滤和渲染代码各自换算时间。

### 3.6 截图执行与资源恢复

每个截图任务至少保护临时 Prefab 实例、`AnimationMode` 进入/退出边界、Camera 状态、原有 `targetTexture`、临时 `RenderTexture`、`Texture2D` 与 `RenderTexture.active`。写文件前完成覆盖确认，循环中显示当前 Clip/角度和进度，异常时仍通过 `finally` 清理临时对象与相机状态。输出写入动画资源同目录时，`AssetDatabase.Refresh()` 应集中在批处理结束阶段，避免循环内反复刷新。

## 4. 风险与不适用边界

- “中间帧”只能作为动作缩略图启发式；起手、收手或循环动作的辨识度可能不在中间帧。
- 静态曲线检测无法识别运行时脚本、约束、IK、布料或其他非 Clip 驱动的变化。
- 目标 Prefab 骨架、Avatar、Animator 类型或 Clip 导入设置不兼容时，`AnimationMode.SampleAnimationClip()` 可能采样不完整；静态编译不能证明姿态正确。
- 多个 Clip 可能产生同名 PNG；必须在输出命名中加入稳定区分，或在执行前报告冲突。
- 不要把批量截图工具当作完整动画预览器；播放、暂停、取消和 Animation Mode 所有权需要单独设计。

## 5. 验证与回退

| 用例 | 预期 |
| --- | --- |
| 兼容模式 + 长 Clip | 仍按手动采样帧输出 |
| Pose + 0 时长 Clip | 被识别并采样第 0 帧 |
| 保持姿态但时长较长的 Clip | 曲线稳定时被识别为 Pose |
| 2～阈值帧的短 Clip | 被识别并采样中间/后一个有效帧 |
| 超过阈值且有明显运动的 Clip | Pose / 短动画模式下跳过 |
| 30 FPS 与 60 FPS 混合 | 按各自帧率采样，不出现时间偏移 |
| 一个 FBX 多个 Clip | 每个 Clip 都能独立输出且无预览子资产噪声 |
| 中途异常或重复执行 | 临时对象、Camera 状态和输出覆盖行为可控 |

至少完成 Editor 程序集编译、窗口输入校验和代表性资源出图。无法启动 Unity 或无法读取真实资源时，只能报告静态/API 验证通过，不能宣称截图姿态已经验收。回退时关闭智能模式即可保留旧的全量手动采样路径。
