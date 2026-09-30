# Endfield 角色页动画重建 Profile

> 类型：PROFILE；项目：终末地本地客户端与 `F:\UnityProject\EndGmae`；状态：已验证的本地批量导入结果，具体客户端版本和 Bundle 变化后必须重新核对。

## 1. 项目事实

### 1.1 工作区与工程

```text
客户端工作区：F:\Game\Hypergryph Launcher\work\endfield-asset-deep-20260914
动画清单：work\endfield-asset-deep-20260914\artifacts\decoded\all-animation-clips-evidence.json
ACL 输出：work\endfield-asset-deep-20260914\artifacts\decoded\all-animation-acl
Avatar TOS：work\endfield-asset-deep-20260914\artifacts\decoded\all-avatar-tos.json
Unity 工程：F:\UnityProject\EndGmae
工程资产：Assets\EndfieldReconstruction\Characters
```

原始解包目录不放进 `ProjectACG` 或 Unity 工程。工程中的 `Characters/Animation/Evidence` 是复制后的 JSON 证据，不是原始 Bundle 的替代品。

### 1.2 已确认资产范围 `[EVIDENCE]`

- 有效 `AnimationClip`：49 个；覆盖 17 个角色/通用骨架模板：Aglina、Ardelia、Boy、Chen、Endminf、Gentleman、Gentlemannpc、Girl、Kids、Lady、Lifeng、Pelica、Strong、Tangtang、Wulfa、Yvonne、Zhuangfy。
- ACL v10 Transform/Float/Root Motion 均已批量解码。
- 49/49 clip 的 Transform track path 已解析完整。
- Gentleman 的动作实际匹配共享 `SK_actor_gentlemannpc_01Avatar`；不能按名字选择独立 `SK_actor_gentleman_01Avatar`。
- 角色页 `.usm` 是预渲染视频证据，不计入可蒙皮骨骼动画。

统计报告：

```text
work\endfield-asset-deep-20260914\artifacts\decoded\all-animation-acl\_batch_summary.json
```

### 1.3 Unity 产物 `[EVIDENCE]`

当前工程已生成：

| 产物 | 数量 | 位置 |
|---|---:|---|
| Generic `.anim` | 49 | `Assets/EndfieldReconstruction/Characters/<Role>/Animation/Generic` |
| Humanoid `.anim` | 49 | `Assets/EndfieldReconstruction/Characters/<Role>/Animation/Humanoid` |
| AnimatorController | 34 | `Assets/EndfieldReconstruction/Characters/<Role>/Animator` |
| AvatarBinding manifest | 17 | `Assets/EndfieldReconstruction/Characters/<Role>/Avatar` |
| Clip Evidence | 49 | `Assets/EndfieldReconstruction/Characters/<Role>/Animation/Evidence/<Clip>` |

总报告：

```text
F:\UnityProject\EndGmae\Assets\EndfieldReconstruction\Characters\AnimationImportReport.txt
```

报告最终摘要为 `imported=49`、`failed=0`；批处理可能对已存在的 Chen 资源产生 skip，不能把 skip 当成失败。

## 2. 使用的工具与命令

### 2.1 ACL 导出器

已验证的导出工具：

```text
F:\Game\Hypergryph Launcher\work\chen-weapon0011-ab-extraction\bin\Debug\net9.0\AclClipExporter.exe
F:\UnityProject\work\20260919-chen-showcase-reconstruction\notes\EndfieldAclNativeExporter\build\Release\EndfieldAclNativeExporter.exe
```

由于导出器目标为 .NET 9，而机器可能只有更高/不同 runtime，执行前设置：

```powershell
$env:DOTNET_ROLL_FORWARD = 'Major'
```

批量脚本：

```text
F:\Game\Hypergryph Launcher\work\endfield-asset-deep-20260914\notes\batch_decode_animation_clips.py
```

执行：

```powershell
python 'F:\Game\Hypergryph Launcher\work\endfield-asset-deep-20260914\notes\batch_decode_animation_clips.py'
```

脚本会输出 `clip_metadata.json`、`binding_map.json`、`transform_track_map.json`、逐帧 JSON、`raw_acl`、`avatar_tos.json` 和 `animator_avatar_evidence.json`。

### 2.2 Unity 批量生成器

工程入口：

```text
Assets/EndfieldReconstruction/Editor/EndfieldAnimationBatchImporter.cs
```

菜单：

```text
Endfield/Animation/Generate All Character Generic + Humanoid Assets
Endfield/Animation/Repair Generic Translation Scale (cm to m)
Endfield/Animation/Create Missing Generic + Humanoid Controllers
```

BatchMode 示例：

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'F:\UnityProject\EndGmae' `
  -executeMethod EndfieldAnimationBatchImporter.BatchGenerate `
  -logFile 'F:\UnityProject\EndGmae\Logs\endfield-animation-batch.log'
```

不要在已有 Unity Editor 持有同一工程锁时启动 BatchMode；必要时使用隔离临时工程生成，再把已验证的 `Characters` 目录同步回目标工程。

## 3. 当前实现约定

### 3.1 Generic 单位修复 `[EVIDENCE]`

ACL Transform 平移在此批次按厘米解释，生成 Generic 曲线时乘 `0.01` 转 Unity 米。导入器提供幂等修复：只有检测到位置曲线绝对值明显大于米制阈值时才缩放，避免重复缩小。Chen 既有 PActor 变体使用独立 FBX 全局缩放策略，不能把 `PActorTranslationScale=100` 直接套到通用角色目录。

### 3.2 Humanoid 通道 `[EVIDENCE]`

Humanoid 资产写入 Root/Feet/Hands XForm 与 Unity `HumanTrait.MuscleName` 通道。独立 `.anim` 没有 FBX 的 `Animation Type` 下拉框；可重定向性由目标 FBX 的 Valid Humanoid Avatar、Animator.avatar 和控制器决定。

### 3.3 Avatar 限制 `[EVIDENCE]`

当前工程只有 Chen 对应的 Humanoid FBX 可直接解析真实 Avatar：

```text
Assets/EndfieldReconstruction/Chen/Model/P_actor_chen_01_Humanoid.fbx
```

Chen manifest 已标记为 `ResolvedByHumanoidFBX_SubAsset`/对应实际状态。其他角色的 `AvatarBinding.json` 为 `EvidenceOnly_ImportMatchingFBX`，只保存 Avatar 名称和 TOS 来源；导入对应角色 FBX 后再回填实际 Avatar 资产路径。

## 4. 问题、根因和解决方式

| 问题 | 根因 | 解决方式 | 当前状态 |
|---|---|---|---|
| Gentleman 70/133 路径 | 错选 `SK_actor_gentleman_01Avatar` | 以 hash 覆盖率选择 `SK_actor_gentlemannpc_01Avatar` | 已修复为 133/133 |
| 导出器无法运行 | .NET runtime 版本不匹配 | 设置 `DOTNET_ROLL_FORWARD=Major` | 已验证 |
| ACL 重跑文件占用 | 前一次进程未退出/同一输出文件被并发写入 | 停止残留 exporter，按 clip 顺序串行重跑 | 已解决 |
| Unity 批量导入极慢 | 每个 clip 内 `ImportAsset`/Refresh 触发全目录扫描 | 批量编辑、延后统一刷新、压缩常量轨道和关键帧 | 已验证临时项目 |
| 曲线资产过大 | 全骨架每帧写入大量常量曲线 | 证据逐帧保留，Unity 资产跳过无变化轨道、曲线最多约 120 key | 已采用，敏感动作需逐帧回退 |
| Generic 角色尺寸错误 | 厘米数据直接写入米制模型 | 位置曲线乘 `0.01`，并提供幂等修复菜单 | 已修复 |
| 误以为 `.anim` 自带 Avatar | Avatar 是模型导入产物，不是曲线文件内容 | 通过 FBX Import Settings 生成 Avatar，Animator 单独绑定 | 已在 README 说明 |
| 角色页主循环无法还原 | manifest 中没有对应骨骼 Clip，只有 `.usm` 预渲染视频 | 只交付有证据的 dialog/gesture ACL clips，标注缺口 | 未完成，禁止伪造 |

## 5. 验证、回退和未验证项

### 5.1 已完成验证 `[EVIDENCE]`

- ACL 批量摘要：49 个 clip，49/49 完整 Transform path。
- Unity 临时 2022.3 项目编译无新增 C# 错误。
- 目标工程产物计数：98 个 `.anim`、34 个 `.controller`、49 个 Evidence 目录、17 个 Avatar manifest。
- Chen、Gentleman 的 Avatar 映射已单独核对。
- 生成器重复执行默认不覆盖既有资产；显式 force 才允许重建。

### 5.2 回退步骤

1. 保留 `artifacts/decoded/all-animation-acl` 作为不变证据；
2. 从目标工程移除或恢复 `Assets/EndfieldReconstruction/Characters` 的生成目录；
3. 不删除已有 `Chen/Animation` 资源，除非明确要求重新生成；
4. 重新导入时先修复 Avatar/TOS 映射，再生成 Unity 曲线；
5. 若单位或采样策略错误，使用证据 JSON 重建，不在 `.anim` YAML 上手工批量替换。

### 5.3 尚未证明的内容 `[UNVERIFIED]`

- 49 个已解码 clip 是否覆盖游戏角色展示页所有主循环动作；当前证据主要是对话/手势动作。
- 非 Chen 角色对应 FBX 的真实 Humanoid Avatar 是否可在目标工程生成并通过 Unity 验证。
- 关键帧约 120 的压缩采样对高速手指、头发和接触动作是否达到逐帧视觉等价。
- 游戏运行时是否对这些动作叠加额外脚本、布料、表情或 Timeline 层。

这些项目必须继续作为候选验证项，不得在交付说明中写成“全部展示页动画已完整还原”。
