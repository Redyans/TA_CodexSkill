---
name: projectacg-animation-batch-screenshot-tool-profile
description: ProjectACG 动画批量截图工具的路径、入口、Pose/短动画模式、采样实现和当前验证边界。
---

# ProjectACG AnimationBatchScreenshotTool Profile

> 类型：PROFILE；适用范围：当前 ProjectACG 的 `AnimationBatchScreenshotTool`；通用实现模式见 [动画批量截图与短 Clip 采样参考](../../references/animation-batch-screenshot-and-clip-sampling.md)。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 源码文件 | `Assets/Editor/TA_Tools/LYJ_Tool/Anima_type/AnimationScreenshotTool/AnimationScreenshotTool.cs`。 |
| 窗口类 | `AnimationBatchScreenshotTool : EditorWindow`。 |
| 菜单入口 | `TA_Tools/TA/Animation/动画批量截图`。 |
| 程序集边界 | 无独立 asmdef；由 `Assembly-CSharp-Editor` 编译，不进入 Player。 |
| 输入 | 目标模型 Prefab、截图 Camera、动画文件夹、采样设置、分辨率、透明背景、相机距离和角度列表。 |
| 输出 | PNG 写入对应动画资源所在目录，文件名包含 Clip 名和视角后缀。 |

该工具位于历史 `LYJ_Tool` 目录，属于既有工具的小步维护。不要因为新增模式就移动目录、改变菜单路径或批量创建资源迁移链。

## 2. 当前实现方式

### 2.1 兼容模式

`PoseAndShortClipMode` 默认关闭。关闭时收集动画目录下全部可读取的 `AnimationClip`，使用用户填写的 `CaptureFrame`，并按每个 Clip 的实际 `frameRate` 换算采样时间，维持原有角度循环、PNG 输出和覆盖确认。

### 2.2 Pose / 短动画模式

开启后显示 `MaxShortClipFrames`，默认值为 10。工具只保留所有动画曲线值在容差内保持不变的静态 Pose，以及 `FrameCount <= MaxShortClipFrames` 的短 Clip。静态 Pose 使用第 0 帧；非静态短 Clip 使用中间帧。关闭模式即可回到旧的全量手动采样路径。

### 2.3 Clip 收集与采样换算

旧实现先按资源路径去重，再使用 `AssetDatabase.LoadAssetAtPath<AnimationClip>()`，一个 FBX 含多个动画子资产时可能只处理其中一个。当前实现对每个唯一路径调用 `AssetDatabase.LoadAllAssetsAtPath()`，筛选 `AnimationClip` 并排除 `__preview__` 子资产。

`GetClipFrameRate()` 优先使用 `clip.frameRate`，无效时回退到 30 FPS。`GetClipFrameCount()` 将 `clip.length * frameRate` 转换为最后帧索引并保留第 0 帧；最终采样时间统一经过 `Mathf.Clamp()` 限制在 Clip 时长内。

## 3. 已确认问题、根因和解决方式

### 3.1 全局 30 FPS 会让短 Clip 采样错位

**根因**：采样时间原先固定使用 `CaptureFrame / 30`，没有消费 `AnimationClip.frameRate`。

**修正**：采样和帧数判断统一使用当前 Clip 的帧率；无效帧率才使用 30 FPS 回退。

### 3.2 只按资源路径会漏掉 FBX 内的 Clip

**根因**：资源路径和 `AnimationClip` 实例不是一一对应关系。

**修正**：`LoadAllAssetsAtPath()` 展开全部子资产，建立“Clip + 来源路径”任务项；排除导入器预览 Clip，避免噪声输出。

### 3.3 长时间保持姿态的 Pose 不能只按时长识别

**根因**：时长描述的是采样区间，不代表姿态是否变化。

**修正**：在智能模式中检查数值动画曲线和对象引用曲线是否稳定；稳定时归类为静态 Pose，并采样第 0 帧。

### 3.4 新功能不能改变原有批处理结果范围

**约束**：用户未开启新模式时，不能自动过滤长动画、替换手动采样帧或改写输出命名。

**修正**：新增模式使用独立开关，默认关闭；`恢复默认`同时关闭开关并恢复 10 帧阈值。

## 4. 当前验证状态

### 已完成

- `Assembly-CSharp-Editor.csproj` 使用 `--no-restore -p:BuildProjectReferences=false` 编译通过，0 个错误；输出包含项目原有警告。
- 静态检查确认智能模式开关、阈值校验、Clip 展开、代表帧解析、按 Clip 帧率采样和默认回退路径均存在。
- `git diff --check` 通过；仅有现有工作区的 LF/CRLF 提示。

### 尚未完成

- 尚未在 Unity Editor 窗口中用真实 Prefab、静态 Pose、2～10 帧 Clip、长 Clip 和多 Clip FBX 完成出图验收。
- 尚未验证所有项目 Avatar/Animator 类型都能被目标 Prefab 正确采样。
- 静态曲线检测不能覆盖脚本驱动 IK、约束、布料或运行时组件产生的姿态变化。

## 5. 维护检查单

- [ ] 保持菜单 `TA_Tools/TA/Animation/动画批量截图` 不变。
- [ ] 新增过滤或采样策略必须有独立开关，旧模式默认行为不变。
- [ ] 处理 FBX 时确认一个路径下的多个 `AnimationClip` 是否都已展开。
- [ ] 帧数、采样时间和进度文案使用同一套 Clip 帧率定义。
- [ ] 静态 Pose 判定必须说明容差和曲线覆盖范围，不能仅凭名称猜测。
- [ ] 输出覆盖、临时实例、Camera、RenderTexture 和 `AnimationMode` 生命周期在异常路径也能恢复。
- [ ] 静态编译通过后，仍需在 Unity Editor 内确认真实姿态和 PNG 内容。
