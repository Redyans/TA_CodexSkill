---
name: projectacg-animation-resource-catalog-profile
description: ProjectACG Booth 动画资源的 Humanoid 筛选、预览图识别、分类迁移与中英文命名 Profile。依赖当前资源路径、Unity 版本和本轮审计输出，不可直接迁移到其他项目。
---

# ProjectACG 动画资源编目与双语命名 Profile

> 类型：PROFILE；适用范围：当前 ProjectACG 中 `Assets/TA_Test/Anima/Booth/anim` 的已整理动画库；通用方法、风险与验证见 [Unity 动画资源编目、识别与安全迁移参考](../../references/animation-resource-catalog-and-safe-migration.md)。

## 1. 项目事实与本轮范围

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 已整理根目录 | `Assets/TA_Test/Anima/Booth/anim`。 |
| 一级分类 | `Motion(动态动作)` 与 `Pose(静态姿势)`。 |
| 筛选目标 | 以 Unity `AnimationClip.humanMotion` 确认的 Humanoid 动画为迁移对象。 |
| 动态动作预览 | 每个 Motion 使用 `15%`、`50%`、`85%` 以及 `0°`、`45°`、`90°` 截图作为语义复核证据。 |
| 静态姿势预览 | Pose 使用正面、45 度和侧面三视图作为重心与姿态复核证据。 |
| 双语命名 | 文件夹、动画、截图和已匹配音频使用 `EnglishName(中文语义)`；英文原标识保留为主检索键。 |

本 Profile 记录的是当前已完成迁移的资产事实和验证边界，不定义跨项目强制规则。将同一方法用于 `Package`、`vrcmods` 或其他资源根目录时，必须重新扫描 Humanoid 状态、预览完整性、音频匹配和引用消费者。

## 2. 当前分类与命名约定

### 2.1 目录语义

当前 Motion 二级目录包括：

- `Action_Combat(战斗动作)`、`AFK_Idle(挂机待机)`、`CharacterVariant(角色专用变体)`；
- `Daily_FullBodyFace(日常全身与面部)`、`Greeting(问候)`、`Emote_Gesture(表情与手势)`、`Interaction(交互动作)`、`Locomotion(移动动作)`；
- `Dance_Duet(双人舞)`、`Dance_GameEmote(游戏表情舞)`、`Dance_Solo(单人舞)`、`Dance_TikTok(短视频舞蹈)`；
- `NeedsPreview(待预览确认)`，用于证据不足或语义仍不确定的动作。

当前 Pose 一级姿态目录包括：

- `Airborne_Acrobatic(腾空与杂技)`；
- `Floor_Lying(地面与躺姿)`；
- `Kneeling_Crouching(跪姿与蹲姿)`；
- `Seated(坐姿)`；
- `Standing(站姿)`。

二级或三级目录可补充 `ActionKeyPose`、`DanceKeyPose`、`HandGesture`、`Pair_Role`、`Group_Role`、`Prop`、`Theme_Heart`、`Theme_Maid` 等使用条件或主题。该层级用于检索和人工浏览，不应被误读为 Animator 状态机、Avatar Mask 或运行时标签。

### 2.2 文件与配套资源命名

动画改名后，关联资源遵循同一基础名称：

```text
Animation: EnglishName(中文语义).anim
Pose image: EnglishName(中文语义)_view0.png
Motion image: EnglishName(中文语义)_t15_view45.png
Audio: EnglishName(中文语义).wav
```

时间、视角、声道/格式等技术后缀不翻译、不删除。动画文件可安全解析为 YAML 时已同步内部 `m_Name`；后续通过 Unity API 或外部导入器生成的 Clip 不应直接套用文本替换。

## 3. 本轮实现与审计产物

### 3.1 执行链路

本轮按“预览证据 -> 人工分类 -> 资源组迁移 -> 双语改名 -> 文件系统验证”执行。实现和审计脚本位于当前工程根目录或指定 Editor 目录：

| 路径 | 用途 |
| --- | --- |
| `CodexPoseReclassify.ps1` | Pose 三视图的重分类与迁移辅助。 |
| `CodexMotionAnalysis/Measure-MotionFrames.ps1` | Motion 多时刻截图的帧度量与异常辅助检查。 |
| `CodexMotionAnalysis/New-MotionContactSheets.ps1` | Motion 预览联系表生成。 |
| `CodexMotionAnalysis/Move-MotionResourceGroups.ps1` | 按动画资源组同步迁移动态动作、截图与音频。 |
| `CodexBilingualRename.ps1` | 文件夹和资源双语改名、`.meta`/内部名称与清单验证。 |
| `Assets/Editor/NonHumanoidAnimationScan.cs` | 在 Unity 导入结果中扫描 `AnimationClip.humanMotion`。 |

这些脚本是本次处理的实现依据，不是可直接复制到其他项目的通用工具。再次执行前先检查输入根目录、目标目录、命名映射、删除开关、重复执行行为及当前 Unity 导入状态。

### 3.2 已生成清单与验证结果

审计输出目录：`CodexBilingualRename/`。

| 文件 | 用途 |
| --- | --- |
| `bilingual-rename-manifest.csv` | 资源级旧名、新名、路径与改名映射。 |
| `bilingual-folder-manifest.csv` | 文件夹旧路径、新路径和原目录 GUID 映射。 |
| `bilingual-rename-verification.txt` | 改名后的计数、路径长度、错误数和最终状态。 |

`bilingual-rename-verification.txt` 当前记录：

```text
status=passed
folders=64
animations=3138
previewImages=24189
audio=228
maxTargetPathLength=233
errors=0
```

本轮文件系统复核确认上述三个资源计数仍与验证报告一致。动画、截图、音频和对应 `.meta` 已按清单重命名；保留资产的 `.meta` GUID 和可解析 `.anim` 的内部 `m_Name` 已在该验证流程中检查。

## 4. 发现的问题、修正与维护约束

### 4.1 不能用单帧或帧差替代动作识别

**现象**：仅看中间帧、正面视图或图像变化量时，容易把等待、呼吸、持物、舞蹈定格与真正静态姿势混淆。

**修正**：Motion 固定输出三时刻三视角的九图；Pose 固定输出三视图。帧差仅用于发现截图损坏、重复帧或异常候选，最终分类结合动作语义、名称和必要时的 Unity 实际播放。

**维护约束**：低置信度条目保留在 `NeedsPreview(待预览确认)`，不为了分类完整度强行归入舞蹈、表情或交互目录。

### 4.2 迁移必须按动画资源组完成

**现象**：先前资源与音频、截图混放时，只移动 `.anim` 会造成预览和声音失去可追溯关系。

**修正**：以动画为主键建立资源组，迁移 `.anim`、`.meta`、所有相关截图及 `.meta`，以及已确认配对音频及 `.meta`；把无匹配或多候选音频列为异常，不在脚本中猜测。

**维护约束**：后续新增或重分类一个动画时，同步检查基础名、时间/视角后缀、音频匹配和 `.meta`；不得单独拖拽移动动画文件后再人工补图或补音频。

### 4.3 双语可读性不能破坏检索和 Unity 身份

**现象**：仅换成中文会丢失来源、插件和英文搜索信息；仅改文件名会使预览图、音频和 Inspector 名称不一致。

**修正**：采用 `EnglishName(中文语义)`，保留英文原标识；同步动画、图片、音频、各自 `.meta` 的文件名，并对可解析 YAML 动画同步 `m_Name`。文件夹也按该形式命名。

**维护约束**：改名计划必须预检重名、大小写冲突和路径长度，且保留旧/新路径映射。移动 Unity 资产时保持原 `.meta`，禁止重新生成 GUID。

### 4.4 非 Humanoid 参数动画需要单独处置

**现象**：`AnimationClip` 可以拥有预览图和动作式名称，却只驱动自定义参数，并非 Humanoid 全身动作。

**修正**：使用 `Assets/Editor/NonHumanoidAnimationScan.cs` 从 Unity 导入结果读取 `humanMotion`，再决定隔离或删除。文件名中的 `humanoid` 不能作为筛选依据。

**当前状态**：当前目录仍可定位 8 个名称已标记为 `非Humanoid参数动画` 的 `.anim`：4 个 `AdvancedGestureVRoid_Animation_WeightControl_*` 与 4 个 `BekoShop_SigBreathMod_Animations_LocalHR*`。本轮文档整理没有重新运行 Unity 扫描，也没有删除它们；下次处置前应重新确认 `humanMotion` 和潜在消费者，再按用户确认的删除范围处理。

## 5. 当前验证边界与后续维护

### 已完成的验证

- 文件夹、动画、预览图与音频计数和双语重命名验证报告一致。
- 验证报告为 `status=passed`、`errors=0`，最长目标路径为 `233`。
- 文件夹 GUID 映射保存在 `bilingual-folder-manifest.csv`；保留动画的 `.meta` GUID 已由改名流程核对。
- Motion 使用的九图和 Pose 使用的三视图已经生成，可作为后续重分类的人工审阅证据。

### 未完成或需要重新验证的项目

- 尚未在本轮文档整理中启动 Unity，对全部改名资源执行完整重新导入、加载和实际播放抽样。
- 本轮未验证 Animator、Timeline、外部 DCC、构建配置或代码是否有裸路径依赖。
- 当前命令行构建因中间依赖清单缺失而失败，不能作为本轮 Unity 编译结论。
- 8 个已标记的非 Humanoid 参数动画仍需在再次扫描和用户确认后隔离或删除。

后续增量整理时，先读取本 Profile、通用参考和 `CodexBilingualRename/` 的现有清单；在现有路径映射基础上新增计划，不要再次用原始目录假设覆盖当前分类结果。
