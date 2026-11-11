# ProjectACG 工具与目录同步经验｜2026-11-09

> 类型：PROFILE；统计范围：北京时间 2026-11-09 00:00（含）至 2026-11-10 00:00（不含）。项目事实仅来自会话 `01a1be75-d3f7-7a92-994c-8af3637d957d`；不能替代当前仓库状态。通用流程见 [工具参考](../../../references/CodexDaily/Development_Reference_2026-11-09.md) 和 [工具模块入口](../../../README_Tech_TAToolDevelopmentRules.md)。返回 [总览](../../../../Codex_Development_Digest_2026-11-09.md)。

## 1. 目录最新状态与来源分拆

### 项目事实/现象

当日对 ProjectACG 的三个目录做了分支/远端引用对比：

| 目录 | 会话报告的最新提交 | 会话报告的来源 |
| --- | --- | --- |
| `Client/Assets/Shader` 整体 | `3142d159a5`，2026-11-07 10:40 | `master` 及多个远端跟踪分支 |
| `Client/Assets/Shader/Character/Chara_V2` | `5ebdbabef7`，2026-11-06 18:06 | `origin/feature/m_battle_develop`、`origin/feature/m_character_monster` |
| `Client/Assets/GameScripts/AOT/GameArt` | `d8aa32608d`，2026-11-06 17:21 | `master` 及多个远端跟踪分支 |
| `Client/Assets/Editor/TA_Tools` | `6954bbc1a1`，2026-11-07 17:55 | `origin/feature/m_character_monster` |

### 根因

目录最后变更与分支整体尖端不是同一概念。`Chara_V2` 的最新改动不在 `master`，而 `Assets/Shader` 整体在 `master` 有更晚的其他渲染提交；因此不能用一个 feature 分支整体覆盖 `master`。

### 项目约束或修正

用户指定只将 `TA_Tools` 路径同步到 `6954bbc1a1`，后续又确认只同步 `Chara_V2` 子目录。会话报告采用 `git restore --source=<commit> --worktree -- <path>`，保留目录外改动和未暂存状态。`TA_Tools` 目录报告涉及 115 个文件；`Chara_V2` 目录报告最终为 108 个文件，新增怪物效果 Shader/Pass 及布料绑定修改。

### 验证证据

会话报告称：远端引用已刷新；目录清单与来源提交一致；`Chara_V2` 新增文件 blob 校验通过；`TA_Tools` 新增 5 个文件逐一核对；`Chara_V2` 的 `git diff --check` 无错误。改动留在工作区，未暂存、未提交。

### 未验证项/回退

Unity Editor 编译、Shader 实际材质效果和 TA_Tools 工具测试均未运行。`TA_Tools` 来源部分 `.meta` 有尾随空格，另有目录外既有改动。若回退，恢复同步前的同一路径快照并重新检查文件清单与 blob；没有会话证据表明已执行回退。

## 2. 规则候选

不新增 CORE。可作为 ProjectACG 维护候选：目录级同步前必须区分目录提交与分支尖端，刷新 refs，限定 `-- <path>`，验证新增/删除文件及 blob，并把 Unity/运行时验证缺口单独记录。该候选依赖 Git 仓库和当前目录，不应提升为跨项目规则。
