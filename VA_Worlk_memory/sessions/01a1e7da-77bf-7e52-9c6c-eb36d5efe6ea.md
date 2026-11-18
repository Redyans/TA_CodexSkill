# Codex 会话记忆

- 会话 ID：`01a1e7da-77bf-7e52-9c6c-eb36d5efe6ea`
- 会话开始：2026-11-17T04:53:37.200Z
- 最近保存：2026-11-17T13:25:04+08:00（北京时间）
- 工作目录：`D:\work2025U3D\Valkyria\ProjectACGMain\ProjectACG`
- 保存事件：Stop
- 文字消息数：16

> 本文件自动更新，记录用户提问、Codex 进度说明和回答。

## 1. 用户

时间：2026-11-17T04:53:39.767Z

# AGENTS.md instructions for D:\work2025U3D\Valkyria\ProjectACGMain\ProjectACG

<INSTRUCTIONS>

# Repository Guidelines

本文件只保留仓库地图、任务路由、硬约束和最常用命令；详细流程与验证要求统一放在 `docs/ai-coding/`，领域细则以 `docs/ai-coding/rules/` 为准。

## 仓库地图

- `Client/`：Unity 客户端工程（Unity `2022.3.62f3`）
  - 代码：`Client/Assets/GameScripts/`（`AOT/` + `HotFix/`）
  - 热更入口：`Client/Assets/GameScripts/HotFix/GameLogic/GameApp.cs`
  - 运行时配置：`Client/Assets/AssetRaw/Configs/`
  - 不提交的 Unity 生成目录：`Client/Library/`、`Client/Temp/`、`Client/Obj/`、`Client/Build/`、`Client/Logs/`
- `aigame_rpc/`：proto / gRPC 协议与代码生成脚本
- `aigame_config/`：Luban / 配置源
- `BuildCLI/`：Android 打包脚本与 Jenkins 流水线
- `Tools/`：开发工具与辅助脚本

## 规则入口与任务路由

根文件只保留主题入口；细分 checklist、排障记录和补充说明由主题 README 内部链接，不在这里重复列出。

| 任务 | 入口 |
| --- | --- |
| 通用流程 / 验证 / harness | `docs/ai-coding/README.md`、`docs/ai-coding/workflow.md`、`docs/ai-coding/verification-matrix.md`、`docs/ai-coding/project-harness.md` |
| 领域规则 | `docs/ai-coding/rules/README.md`，并读取目标目录最近的 `AGENTS.override.md` |
| UI / HotFix / 网络 / 配置 | `docs/ai-coding/rules/ui-development.md`、`docs/ai-coding/rules/ui-visibility-channel.md`、`docs/ai-coding/ui/ui-lifecycle-listener-localization-governance.md`、`hotfix-development.md`、`network-integration.md`、`config-module.md` |
| 多语言 / 本地化文本 | skill `.agents/skills/projectacg-localize-table/SKILL.md`（首选入口）、`docs/ai-coding/rules/config-localize-tool.md`（命令、域策略与风险细则） |
| AVPro 视频播放 / 视频 UI | `docs/ai-coding/rules/avpro-video.md`；必须使用 `MediaPlayer -> DisplayUGUI`，并检查 Gamma UI 材质、Canvas 遮挡和平台纹理格式 |
| Battle / Monster AI / 红点 | `docs/battle/README.md`、`docs/ai-coding/monster-ai/README.md`、`docs/ai-coding/red-dot/README.md` |
| OpenSpec | `docs/ai-coding/openspec-workflow.md`、`openspec/AGENTS.md` |
| AI 自进化 / 文档漂移 | `docs/ai-coding/ai-self-evolution.md`、`docs/ai-coding/docs-context-loop.md` |

出现以下情况时，先读取 `openspec/AGENTS.md` 和 `openspec/config.yaml`，并结合相关 `openspec/changes/**` / `openspec/specs/**` 文档，不要直接实现：

- 用户明确要求创建 / 应用 / 归档 OpenSpec 工件
- 涉及 breaking change、跨模块公共契约、架构边界、协议 schema 或配置格式变更
- 需要形成可长期追踪的 proposal / design / spec delta；普通计划、局部修复和只读方案讨论不因出现 `plan` / `proposal` / `spec` / `change` 单词而自动进入 OpenSpec

## 问题探索与多 agent

- 默认单 agent。只有用户明确要求，或适用的 `AGENTS.md` / skill 明确授权 delegation / parallel agent work，且问题簇可独立归因、并行收益明显高于同步成本时，才使用子 agent。
- 先聚类现象并判断共同根因；同源问题单线收敛。子 agent 优先承担只读查证、日志 / 测试定位和独立复核，默认不递归委派。
- 主 agent 始终负责目标边界、归因、写入集成、最终验证和交付；reviewer 按风险触发，不作为固定仪式。

## 仓库级硬约束

- 禁止使用 `dynamic`
- 设计优先组合，避免无必要的深继承和巨型单例
- 默认写实例方法；只有纯工具、无状态、无生命周期依赖、无上下文依赖的方法，才写静态方法。
- 新增或修改的运行时代码日志优先使用 `TEngine.Log`，避免直接使用 `UnityEngine.Debug`
- 新增或修改的 HotFix 手写运行时代码禁止引用 `UnityEditor`，命名空间优先保持 `GameLogic`；历史例外不要在普通任务里顺手扩散
- 所有网络协议的唯一真相源是 `aigame_rpc/**/*.proto`；禁止手改 `Client/Assets/GameScripts/HotFix/GameProto/NetProto/`，协议变更必须先改 `aigame_rpc/` 并通过生成链同步
- `[Window(..., location: "...")]` 的 `location` 视为稳定 ID，不要随意改名
- 事件、按钮监听、网络 Handler 必须成对注册 / 注销，并在销毁或关闭时清理
- 生产 UI 默认禁止使用 `RemoveAllListeners()`；固定回调精确 `RemoveListener`，lambda / 捕获变量使用 `UIListenerBindings` 管理当前 owner 的监听；框架自有清理和 vendor 独占事件按所有权单独评审
- 生产 UI 禁止 `async void`；同步事件入口转调返回 `UniTask` 的实现并显式 `.Forget()`，异步实现必须处理取消、异常和销毁边界
- 保持既有分层：`UI -> Controller -> NetworkModule -> WebSocket/HTTP -> Server`
- 服务地址、鉴权和环境配置统一从 `GameModule.Config.Config.Server` 读取，避免硬编码
- `TargetId`、本地化 Key、PlayerPrefs / EditorPrefs Key、跨模块事件 Key、配置查询 Key 等稳定字符串标识必须集中到对应系统的 `*Ids` / `*Keys` / 常量目录文件中维护，不要在业务调用点裸写字符串 key
- 用户可见文本必须走本地化 Key；代码、配置、预制体、样例数据和引导流程中不要硬编码中文/英文展示文案，也不要用代码内置中文 fallback 代替本地化表。新手引导相关本地化 Key 统一维护在 `aigame_config/GameConfig/Localize/UI/#Guide_Localize.xlsx`，并通过本地化合并 / 导入流程同步到 `aigame_config/GameConfig/Localize/UILocalization.xlsx` 和 `Client/Assets/AssetRaw/Configs/Localization.csv`
- 处理任何新手引导、Guide Flow、NewbieGuide、引导目标或战斗引导任务前，必须先读 `docs/ai-coding/newbie-guide/newbie-guide-task-playbook.md`；需要历史取舍或相似问题证据时继续读同目录的 `newbie-guide-evidence-ledger.md`。新表驱动 `NewbieGuideModule` 的固定 UI 目标以 `NewbieGuideUiTarget` 为真相源，旧/通用 `GuideModule` 才使用 `Client/Assets/AssetRaw/Guide/GuideTargetCatalog.json`，动态目标走明确 resolver/provider，业务 UI 不要手写逐按钮注册
- 新的视频 UI 播放链路统一使用 AVPro `MediaPlayer -> DisplayUGUI`；禁止新增 Unity `VideoPlayer`、`VideoClip`、`RawImage` 或通过 `RenderTexture` 复制视频帧的旧链路。详细绑定、Gamma UI、SoftMask、平台输出和验证要求见 `docs/ai-coding/rules/avpro-video.md`
- 使用 `UICamera` 的普通单纹理 AVPro `DisplayUGUI` 必须显式绑定 `Client/Assets/AssetRaw/Materials/AVProVideoGammaUI.mat`（Shader：`Valkyria/UI/AVProVideoGammaUI`）；普通 UI 继续使用 `GammaUIDefault.mat`。OES/YCbCr/多纹理/立体/透明输出必须使用兼容 Gamma UI Pass 的 AVPro Shader，动态创建的显示层同样适用；不得把“已解码/已播放”当作“用户可见”的证据
- 不提交密钥、`*.keystore`、本地环境配置；本地差异使用 `local.properties` 或环境变量

## 修改边界

- 可以直接修改：`Client/Assets/GameScripts/**`、`aigame_rpc/`、`aigame_config/`、`BuildCLI/`、`Tools/`、`docs/`
- 需要额外谨慎：`Client/Assets/AssetRaw/Configs/`、UI `location`、资源路径、`.meta`、构建脚本中的环境分支
- 不要主动修改：Unity 生成目录、协议生成目录、提交凭据或其他机器本地配置
- `aigame_rpc/`、`aigame_config/` 可能是独立 Git 工作区；涉及它们时需分别检查 `git -C aigame_rpc status --short` / `git -C aigame_config status --short`，不要只看主仓状态

## 常用命令

- 项目初始化：`setup_project.bat`（Windows）或 `./setup_project.sh`（Mac/Linux）
- 项目 harness 检查：`powershell -ExecutionPolicy Bypass -File Tools/Codex/check-projectacg-harness.ps1 -Scope Repo`
- 静态上下文 readiness：`powershell -ExecutionPolicy Bypass -File Tools/Codex/check-project-context-readiness.ps1 -Scope Repo`
- 模型能力 readiness：`powershell -ExecutionPolicy Bypass -File Tools/Codex/check-model-readiness.ps1 -Scope Repo -Model gpt-5.6-sol -Effort high`；配置或模型变化后加 `-RunModelEval` 刷新真实 smoke 报告
- Android 打包：`BuildCLI\\build_android.bat <branch> [dev|prod|uat]`
- 协议生成：在 `aigame_rpc/` 下运行 `./build.sh game_logic_service`
- Swagger 生成：在 `aigame_rpc/` 下运行 `./build_swagger.sh game_logic_service`
- 静态资源服务：在 `Tools/FileServer/` 下运行 `npm install && npm run dev`
- Source Generator：`dotnet build Tools/GameEventSourceGenerator/SourceGenerator.sln`
- UI 生命周期、监听与本地化门禁：`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Codex/check-ui-governance.ps1 -Scope Repo`
- Git hooks 说明：`Tools/GitHooks/README.md`（克隆后优先执行 `setup_project` 完成 hooks 配置）

## 交付要求

- 按 `docs/ai-coding/verification-matrix.md` 执行与任务匹配的最小充分验证
- 修改规则、文档入口、生成边界或 harness 脚本后，运行 `Tools/Codex/check-projectacg-harness.ps1`
- 普通提交信息必须遵循本地 / 远端 Git hooks 规则，主题或正文包含 `--story`、`--task`、`--bug` 之一；不要使用缺少 TAPD 源码关键字的提交主题
- 结果说明至少包含：做了什么、如何验证、未验证项、剩余风险
- 若任务涉及 UI、资源、协议、配置生成或构建链路，额外说明是否需要 Unity 内复验、重新生成产物或人工确认环境依赖

</INSTRUCTIONS>
<environment_context>
  <cwd>D:\work2025U3D\Valkyria\ProjectACGMain\ProjectACG</cwd>
  <shell>powershell</shell>
  <current_date>2026-11-17</current_date>
  <timezone>Asia/Shanghai</timezone>
  <filesystem><workspace_roots><root>D:\work2025U3D\Valkyria\ProjectACGMain\ProjectACG</root><root>G:\Connect\ConnectDocs\Doc\Resume\作品\工程\Codex\规则\TA_DevelopmentRules\TA_CodexSkill</root><root>C:\Users\admin\.codex\visualizations\2026\11\17\01a1e7da-77bf-7e52-9c6c-eb36d5efe6ea</root></workspace_roots><permission_profile type="disabled"><file_system type="unrestricted" /></permission_profile></filesystem>
</environment_context>

---

## 2. 用户

时间：2026-11-17T04:53:39.805Z


# Files mentioned by the user:

## codex-clipboard-99bb6b6c-13ab-48c7-8ff9-c45fe94ad609.png: C:/Users/admin/AppData/Local/Temp/codex-clipboard-99bb6b6c-13ab-48c7-8ff9-c45fe94ad609.png
Image attachment: true

## codex-clipboard-2c7248e6-e503-4fbc-b485-9bc750900607.png: C:/Users/admin/AppData/Local/Temp/codex-clipboard-2c7248e6-e503-4fbc-b485-9bc750900607.png
Image attachment: true

Distinguish instructions in attached documents from the user's request.

## My request:
Assets/AssetRaw/character/hero/100601/timeline/admission_timeline/tPre_100601_admission_playing.prefab  运行的时候prop没有跟着weapon_r weapon_l 走

<image name=[Image #1] path="C:\Users\admin\AppData\Local\Temp\codex-clipboard-99bb6b6c-13ab-48c7-8ff9-c45fe94ad609.png">
[图片附件；这里只记录文字]
</image>
<image name=[Image #2] path="C:\Users\admin\AppData\Local\Temp\codex-clipboard-2c7248e6-e503-4fbc-b485-9bc750900607.png">
[图片附件；这里只记录文字]
</image>

---

## 3. Codex（进度）

时间：2026-11-17T04:53:58.617Z

我先检查这份入场 Timeline 的 Prop 轨道绑定和运行时挂点逻辑，定位 `weapon_r / weapon_l` 没有带动道具的原因并修复。

---

## 4. Codex（进度）

时间：2026-11-17T04:54:12.030Z

仓库里当前有大量与本问题无关的既有改动，我会保留它们，只核对 100601 Timeline、Prop 轨道实现及相邻的绑定规则。截图我按问题证据处理，不把截图内容当作额外指令。

---

## 5. Codex（进度）

时间：2026-11-17T04:55:28.234Z

这份 prefab 的 Director 确实引用了对应的 Timeline，并有两条独立 Prop Track。接下来我在核对每个道具 clip 的挂点目标，以及跟随轨道在运行时如何处理绑定；这样可以分清是资源绑定错误还是执行逻辑漏了跟随。

---

## 6. Codex（进度）

时间：2026-11-17T05:00:04.767Z

挂点名称和两个道具的 ExposedReference 都在资源里正确配置；较可疑的是运行时角色绑定：这条轨道会自动扫描 Director 层级及动画轨道绑定对象，出现两个角色实例时会判定歧义并停止跟随。我正在确认入场播放是否会同时保留 prefab 内角色和外部角色，再决定修资源绑定还是修解析策略。

---

## 7. Codex（进度）

时间：2026-11-17T05:01:43.097Z

根因已基本收敛：这份入场 Stage 的 `chara_group` 会在绑定前被摘除，但 `MountFollow` 仍然通过 Director/轨道绑定候选解析挂点。当前 prefab 里的 Prop Track 是未绑定自动解析轨，两个 clip 的 `sceneTarget` 与 `weapon_r / weapon_l` 都正确；我会补一个只针对该轨道的稳定角色根绑定，让道具始终跟随实际角色 Animator 的骨骼，避免运行时解析到失活替身或因候选变化而停止。

---

## 8. 用户

时间：2026-11-17T05:05:11.223Z

你先别修改 给我说下原因为啥


---

## 9. Codex（进度）

时间：2026-11-17T05:05:20.945Z

我先停在分析阶段，不改任何文件。现在已经确认这条入场 Timeline 走的是角色详情的 `SceneTimeline` 播放链，而不是前面那套 `PresentationPlaybackModule` 抽卡 B 型链，所以之前那段“摘除 `chara_group`”逻辑并不会覆盖这次运行。

---

## 10. Codex

时间：2026-11-17T05:06:07.637Z

## 当前进度

- 用户反馈：运行时 `Assets/AssetRaw/character/hero/100601/timeline/admission_timeline/tPre_100601_admission_playing.prefab` 中的两个 Prop 没有跟随 `weapon_r / weapon_l`。
- 用户随后明确要求：先不要修改，只解释原因。
- 已暂停所有写入；本轮没有修改文件。
- 已读取仓库规则、ProjectACG 开发规范、HotFix 规范及相关 Timeline/挂点代码。
- 工作区已有大量与本问题无关的未提交改动，必须保留，不能回滚。

## 已确认事实

### Timeline 资源配置正确

`tl_100601_admission_playing.playable`：

- `Prop Track 01 - pre_hero_100601_prop_pen`
  - `sceneTarget: 100601_show_draw_playing_prop_01`
  - `mountId: weapon_r`
- `Prop Track 02 - pre_hero_100601_prop_screen`
  - `sceneTarget: 100601_show_draw_playing_prop_02`
  - `mountId: weapon_l`
- 三个位置、旋转、缩放轴均开启。
- Prefab Director 的 `m_ExposedReferences` 中存在：
  - `100601_show_draw_playing_prop_01`
  - `100601_show_draw_playing_prop_02`

Prefab 中两个 prop 都在 `prop_group` 下，Timeline 的 `PlayableDirector` 和 `UltimateTimelineBindings` 存在。

### 运行时链路不是抽卡 Presentation B 型链

100601 角色详情入场由：

- `CharacterDetailEntranceTimelineFlow.cs`
- 创建 `SceneTimelinePlaybackRequest`
- 进入 `PresentationPlaybackModule.SceneTimeline.cs`

而不是普通 `PresentationPlaybackModule.PlayAsync(PresentationPlayRequest)` 的外部角色入场路径。

### 当前 SceneTimeline 运行流程

`PresentationPlaybackModule.SceneTimeline.cs`：

1. 实例化 Timeline prefab。
2. 找到 Director。
3. `PrepareUltimateTimelineBindings()`：
   - 获取 `UltimateTimelineBindings`
   - `_ultimateCharacterAnimator = _request.ExternalCharacterAnimator`
   - 创建 `SceneTimelinePreviewCharacterScope`
4. `SceneTimelinePreviewCharacterScope.Acquire(...)`：
   - 查找 prefab 内 `chara_group`
   - 找到其 `Ultimate_Character` 绑定的预览 Animator
   - 将明确的角色绑定迁移到外部角色
   - 隐藏预览角色
   - 将预览角色移到 inactive quarantine
   - 对 `MountFollowClipBase.sceneTarget` 做 ExposedReference 映射
5. `BindUltimateTimelineTracks()`：
   - 绑定 `Ultimate_Character`
   - 绑定 `Ultimate_Weapon`
   - 绑定 Camera 等约定轨道
   - **没有给普通 `MountFollowTrack` 设置轨道 Binding**
6. Director 重建 Graph / Evaluate / Play。

### MountFollow 的自动解析逻辑

`MountFollowTrack` 是无 Binding 的自动解析轨道，`MountFollowBehaviour` 运行时调用：

```csharp
MountResolver.TryResolve(owner, null, mountId, ...)
```

自动解析会扫描：

- 当前 Director 层级
- Timeline 各输出轨道绑定对象
- 嵌套 Director 链

如果找到两个不同的同名挂点，则返回 `Ambiguous`，不跟随。

### 关键风险点

`SceneTimelinePreviewCharacterScope` 中存在专门处理预览角色的逻辑：

- 预览角色被移出运行时层级并隐藏。
- `TimelineBindingResolver.Invalidate(director)` 被调用。
- `MountFollowClipBase.sceneTarget` 的 ExposedReference 会映射到运行时对象。
- 但 `MountFollowTrack` 本身仍依赖自动挂点解析，未显式绑定外部角色根。

因此最可能的原因是：

1. 这份资源使用的是无 Binding 的 `MountFollowTrack`；
2. 角色详情链路中预览角色被隔离、外部角色由 `Ultimate_Character` 重绑；
3. `MountResolver` 的候选根不稳定或仍包含失活/预览层级；
4. `weapon_r / weapon_l` 自动解析失败或判定歧义，`MountFollowBehaviour` 将 `m_Mount` 保持为 null，Prop 就不会跟随。

需要注意：这不是目前最普通的“Timeline clip 的 `mountId` 写错”问题，资源字段已经核对正确。

## 相关文件

- [tl_100601_admission_playing.playable](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/AssetRaw/character/hero/100601/timeline/admission_timeline/tl_100601_admission_playing.playable)
- [tPre_100601_admission_playing.prefab](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/AssetRaw/character/hero/100601/timeline/admission_timeline/tPre_100601_admission_playing.prefab)
- [MountFollowBehaviour.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowBehaviour.cs)
- [MountResolver.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/MountResolver.cs)
- [TimelineBindingResolver.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/TimelineBindingResolver.cs)
- [SceneTimelinePreviewCharacterScope.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/HotFix/GameLogic/Module/PresentationModule/SceneTimelinePreviewCharacterScope.cs)
- [PresentationPlaybackModule.SceneTimeline.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/HotFix/GameLogic/Module/PresentationModule/PresentationPlaybackModule.SceneTimeline.cs)
- [CharacterDetailEntranceTimelineFlow.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/HotFix/GameLogic/UI/Character/V3/Character/CharacterDetailEntranceTimelineFlow.cs)

## 尚未完成

- 还没有运行 Unity/PlayMode，也没有实际日志确认是：
  - `Mount not found`
  - `Ambiguous mount`
  - `sceneTarget` 解析为空
  - 或 LateUpdate / Animator 执行顺序导致的跟随覆盖。
- 还没有修改代码或 prefab。
- 下一步应先查看运行时日志中 `[MountFollow]` 和 `SceneTimelinePreviewCharacterScope` 相关输出，特别是：
  - `Ambiguous mount 'weapon_r'`
  - `Mount not found`
  - `Prop animation probe mount candidates`
- 若日志确认自动解析问题，优先考虑将这两条轨道改成 `BoundMountFollowTrack` 并在 SceneTimeline 运行时绑定到外部角色根，或为当前角色详情链增加显式 MountFollow 绑定；不要直接改 `mountId`。
- 用户当前只要求解释原因，继续工作前应先给出原因说明，等待用户确认是否进入修复。

---

## 11. Codex（进度）

时间：2026-11-17T05:06:16.673Z

先不修改。我会核对挂点解析代码和已有运行日志，确认 Prop 没跟随是在哪一步断掉的。


---

## 12. Codex（进度）

时间：2026-11-17T05:06:24.903Z

按项目的 Timeline/挂点规范，我只做只读排查：确认资源字段、播放链路、`MountFollow` 解析候选和已有日志，不改 prefab 或代码。

---

## 13. Codex

时间：2026-11-17T05:19:00.064Z

## 当前进度

- 用户反馈：运行时 `tPre_100601_admission_playing.prefab` 中两个 Prop 没有跟随 `weapon_r / weapon_l`。
- 用户明确要求：**先不要修改，只解释原因**。
- 当前没有修改任何文件。
- 已读取项目规则、Timeline 挂点代码、资源和历史 OpenSpec 证据。
- 工作区已有大量无关未提交修改，不能回滚。

## 已确认事实

资源配置本身正确：

- `tl_100601_admission_playing.playable`
  - Prop 01：`sceneTarget=100601_show_draw_playing_prop_01`，`mountId=weapon_r`
  - Prop 02：`sceneTarget=100601_show_draw_playing_prop_02`，`mountId=weapon_l`
- Prefab 的 `PlayableDirector.m_ExposedReferences` 中存在两个 Prop 引用。
- 两条轨道实际类型是普通 `MountFollowTrack`，不是 `BoundMountFollowTrack`。
- Prefab 中 Prop 位于 `prop_group`，角色预览模型位于 `chara_group`。

运行链路：

1. 角色详情通过 `CharacterDetailEntranceTimelineFlow` 创建 `SceneTimelinePlaybackRequest`。
2. `PresentationPlaybackModule.SceneTimeline` 将 Timeline 的 `Ultimate_Character` 绑定切换到外部角色 Animator。
3. `SceneTimelinePreviewCharacterScope` 会隐藏并隔离 Timeline 内的预览角色。
4. 普通 `MountFollowTrack` 没有轨道 Binding，运行时依赖 `MountResolver.TryResolve(...)` 自动扫描：
   - Director 层级；
   - Timeline 其它轨道的绑定对象；
   - 嵌套 Timeline 的父级绑定。
5. 如果自动解析失败或命中多个同名挂点，`MountFollowBehaviour` 不会设置 `m_Mount`，随后直接跳过跟随。

关键代码：

- [MountFollowBehaviour.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowBehaviour.cs:128)
  - `MountFollowTrack` 自动解析挂点；
  - 解析失败时打印 `Mount not found` 或 `Ambiguous mount`；
  - `m_Mount == null` 时不执行位姿跟随。
- [MountResolver.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/MountResolver.cs:129)
  - 自动扫描 Director 层级和其它 Timeline 轨道绑定。
- [SceneTimelinePreviewCharacterScope.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/HotFix/GameLogic/Module/PresentationModule/SceneTimelinePreviewCharacterScope.cs:130)
  - 隔离 Timeline 内制作角色；
  - 将 `Ultimate_Character` 绑定迁移到外部角色。
- [PresentationPlaybackModule.SceneTimeline.cs](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/HotFix/GameLogic/Module/PresentationModule/PresentationPlaybackModule.SceneTimeline.cs:929)
  - 准备外部角色绑定；
  - 随后调用 `BindUltimateTimelineTracks()`。

## 当前最可能原因

不是 `mountId` 填错，而是运行时自动挂点解析链路没有稳定锁定外部角色：

- 两条 Prop 轨道使用的是**无 Binding 的自动解析轨道**；
- 角色详情播放时，Timeline 自带的预览角色会被隐藏/隔离，真实角色 Animator 在 Timeline 外部；
- 自动解析依赖 `Ultimate_Character` 的 Generic Binding 是否已经正确迁移，以及该 Animator 的 Transform 是否覆盖完整角色骨架；
- 若预览角色仍被扫描到，会出现两个 `weapon_r` / `weapon_l`，解析结果为 `Ambiguous`，系统按设计直接不跟随；
- 若外部 Animator 根节点不包含这两个节点，则结果为 `Mount not found`，同样不跟随。

历史 OpenSpec 已记录过同类 100601 问题：

- 初次日志中 `weapon_r / weapon_l` 因 `chara_group` 预览替身与运行时角色同时存在而报 `Ambiguous mount`；
- 摘除预览角色后，日志显示挂点解析成功，命中运行时角色的 `weapon_r / weapon_l`。
- 见 `openspec/changes/adapt-gacha-dual-stage-profiles/tasks.md` 第 153、157 行附近。

## 尚未确认

当前没有 Unity PlayMode 日志，因此还不能最终区分：

1. `[MountFollow] Mount not found`;
2. `[MountFollow] Ambiguous mount`;
3. 外部 Animator 根节点不覆盖 `weapon_r / weapon_l`;
4. 挂点解析成功，但后续 Animator/其它轨道覆盖了 Prop 位姿。

`Client/Logs` 当前没有可用的 Player 运行日志，只有资源导入日志。

## 下一步

如果继续排查，应只运行一次 100601 角色详情入场，并查看：

- `[MountFollow]`
- `SceneTimelinePreviewCharacterScope`
- `Prop animation probe mount candidates`
- `UltimateTimeline.prepare`

其中：

- `Ambiguous mount`：确认预览角色未被完全隔离或仍存在重复角色根；
- `Mount not found`：确认 `Ultimate_Character` 绑定的 Animator 根和 `weapon_r / weapon_l` 的实际层级；
- `mount resolved` 但画面仍不跟：再检查 Animator/AnimationTrack 的写入时序和 LateUpdate 跟随校正。

可修复方向尚未执行：优先考虑将这两条轨道改为 `BoundMountFollowTrack` 并显式绑定外部角色根，或在当前角色详情链路补充显式 MountFollow 绑定；不要先改 `mountId`。

---

## 14. Codex（进度）

时间：2026-11-17T05:20:42.723Z

我先保持只读，核对截图、挂点配置和运行时解析代码，确认原因能定位到哪一步。

---

## 15. Codex（进度）

时间：2026-11-17T05:20:55.417Z

截图只说明 Timeline 上有两条 Prop 轨道和运行画面中的道具位置，不能单独证明挂点绑定成功。我继续把这两条轨道的类型、`mountId`、预览角色隔离和运行时绑定顺序对起来。

---

## 16. Codex

时间：2026-11-17T05:25:02.655Z

我没有修改任何文件。原因基本不是 `mountId` 写错，而是这两条轨道使用的是普通 `MountFollowTrack`：

- `mountId` 确实是 `weapon_r` / `weapon_l`：[playable:179](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/AssetRaw/character/hero/100601/timeline/admission_timeline/tl_100601_admission_playing.playable:179)、[playable:303](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/AssetRaw/character/hero/100601/timeline/admission_timeline/tl_100601_admission_playing.playable:303)。
- 普通 `MountFollowTrack` 没有明确的角色 Binding，只会自动扫描 Timeline 层级和其他轨道绑定对象。
- 这个 prefab 同时存在 `chara_group` 里的预览角色和运行时传入的真实角色。两套角色都有同名的 `weapon_r` / `weapon_l`。
- 自动解析命中多个同名挂点时会判定为 `Ambiguous`，代码设计上会直接放弃跟随，而不是随机选择一个挂点：[MountFollowBehaviour.cs:316](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowBehaviour.cs:316)。
- 另外，如果运行时外部角色没有被正确绑定，或者其骨骼层级中没有这两个节点，也会报 `Mount not found`，同样不会跟随：[MountFollowBehaviour.cs:323](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/MountFollowBehaviour.cs:323)。

这个 100601 资源历史上已经出现过同样现象：`chara_group` 预览替身导致 `weapon_r / weapon_l` 报 `Ambiguous mount`；移除预览角色后才解析到运行时角色的挂点。[tasks.md:153](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/openspec/changes/adapt-gacha-dual-stage-profiles/tasks.md:153)、[tasks.md:157](D:/work2025U3D/Valkyria/ProjectACGMain/ProjectACG/openspec/changes/adapt-gacha-dual-stage-profiles/tasks.md:157)。

所以当前最可能的根因是：**运行时仍然扫描到了 prefab 内的 `chara_group` 预览角色，导致挂点解析歧义，Prop 的跟随逻辑被安全跳过。** 需要看运行日志中的 `[MountFollow] Ambiguous mount` 或 `Mount not found` 才能把这两个分支最终区分开。

---
