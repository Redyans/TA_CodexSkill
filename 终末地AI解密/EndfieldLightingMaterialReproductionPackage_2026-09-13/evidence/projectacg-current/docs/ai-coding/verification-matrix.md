# Verification Matrix

本文件定义 ProjectACG 中不同类型 AI coding 任务的最低验证要求。

使用方式：

1. 先按 [workflow.md](./workflow.md) 对任务分类。
2. 再对照本矩阵执行最低验证。
3. 如果环境限制导致无法完成验证，必须在结果说明中明确写出缺口。

## 1. 通用基线

所有任务至少检查以下内容：

| 检查项 | 要求 |
| --- | --- |
| 规则命中 | 说明读取了哪些规则或基于哪些现有实现对齐 |
| 影响范围 | 说明影响的模块、目录、调用链或资源 |
| 非目标回归 | 确认没有顺手改动无关逻辑 |
| 高风险目录 | 确认未误改生成目录、关键资源 location、`.meta`、协议产物 |
| 验证结论 | 明确写出已验证、未验证和剩余风险 |
| 项目 harness | 涉及规则入口、生成边界、跨仓目录或 harness 脚本时，运行 `Tools/Codex/check-projectacg-harness.ps1` |

## 2. 按任务类型验证

### 2.1 小型修复

| 必查项 | 说明 |
| --- | --- |
| 触发条件是否成立 | 复核 bug 场景、空值路径、边界判断或回归点 |
| 修复点是否最小 | 避免将局部修复扩大成无关重构 |
| 相邻调用链 | 至少检查直接调用方和被调用方是否受影响 |

### 2.2 UI 任务

| 必查项 | 说明 |
| --- | --- |
| `Window` 与 location | 不随意改 `location` 字符串，避免资源引用失效 |
| 监听所有权 | 固定回调是否精确 `RemoveListener`；lambda / 捕获变量是否由 `UIListenerBindings` 管理；生产 UI 不得用 `RemoveAllListeners()` 清理共享事件 |
| 动态重绑 | Rebind / Despawn / `OnDestroy` 是否只清理当前 owner；同一实例连续 Rebind 20 次后单次点击仍只触发一次，且不影响其他 owner |
| 事件解绑 | `AddListener` / `GameEvent.AddEventListener` 是否在销毁时成对移除 |
| 生命周期 | 打开、刷新、关闭、销毁路径是否成对完整 |
| 异步点击 | 生产 UI 不得使用 `async void`；同步入口转调 `UniTask` 并显式 `.Forget()`，处理异常、取消和 Token 所有权 |
| 本地化 | 玩家文案是否使用集中 Key；源分表 / 总表 / `Localization.csv` 是否一致，并在中英文检查裸 Key、占位符、富文本和布局 |
| UI 静态门禁 | 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Codex/check-ui-governance.ps1 -Scope Repo` |
| 资源依赖 | 若涉及 prefab 或绑定字段，说明是否需要 Unity 内复验 |

### 2.3 HotFix 任务

| 必查项 | 说明 |
| --- | --- |
| 命名空间 | 是否保持 `GameLogic` 命名空间约定 |
| `UnityEditor` | HotFix 目录内不得引用 `UnityEditor` |
| `dynamic` | 确认未引入 `dynamic` |
| 生命周期清理 | 事件、回调、异步任务是否在模块关闭或销毁时清理 |
| 异步安全 | `.Forget()`、异常处理、取消令牌是否合理 |

### 2.3.1 AVPro 视频 UI 任务

| 必查项 | 说明 |
| --- | --- |
| 播放链路 | 新视频必须使用 `MediaPlayer -> DisplayUGUI`；不得新增 `VideoPlayer`、`VideoClip`、`RawImage` 或视频帧复制 `RenderTexture` |
| 绑定时序 | `DisplayUGUI.Player` 必须在 `OpenMedia` 前绑定；不能只在 `FirstFrameReady` 中首次绑定 |
| Gamma UI | `UICamera` 视频显示面显式使用 `AVProVideoGammaUI.mat`（普通 UI 使用 `GammaUIDefault.mat`）或明确兼容的 AVPro Gamma Shader；Frame Debugger 确认 Draw 位于 Gamma UI Pass |
| 特殊纹理 | OES / YCbCr / 多纹理 / 立体 / 透明输出不得盲目套普通 Gamma 材质，先确认 AVPro 专用 Shader 或格式转换 |
| 遮挡与生命周期 | 检查 Canvas、CanvasGroup、sibling、Mask/SoftMask、fallback；关闭时解绑 Player、移除事件、关闭播放器并释放 lease |
| 资源路径 | 按项目解析器、YooAsset RawFile 或 StreamingAssets 约定提供可访问媒体；路径失败、OpenMedia 失败和首帧超时必须有日志和 fallback |
| 验证层级 | 至少完成 AOT/GameLogic 编译、`check-unity-assets.ps1 -Scope Changed` 和 `git diff --check`；最终在 Unity/目标设备验收首帧、切换、过渡、关闭重开及平台纹理 |

### 2.4 网络任务

| 必查项 | 说明 |
| --- | --- |
| 分层 | UI 不直接发包，链路应保持 `UI -> Controller -> Module -> Network` |
| 协议来源 | `MsgID`、请求结构、响应处理是否来自既有协议定义 |
| Handler 生命周期 | `RegisterMessageHandler` / `UnregisterMessageHandler` 是否成对 |
| 配置读取 | 服务地址、鉴权、环境配置是否来自 `GameModule.Config.Config.Server` |
| 错误路径 | 超时、失败、重试或兜底提示是否有明确处理 |

### 2.5 配置与 Luban 任务

| 必查项 | 说明 |
| --- | --- |
| 配置来源 | 确认修改的是运行时配置还是生成源配置 |
| 生成链路 | 若改动 `aigame_config` 或表结构，说明是否需要重新生成 C# / bytes |
| 产物目录 | 不混淆源表、生成代码和运行时配置目录 |
| 向后兼容 | 确认旧字段、默认值、读取路径不会直接崩溃 |

### 2.6 协议生成任务

| 必查项 | 说明 |
| --- | --- |
| 生成目录 | 不手工修改 `Client/Assets/GameScripts/HotFix/GameProto/NetProto/`；RPC CI 生成提交可以只包含该生成目录 |
| 生成来源 | 修改应发生在 `aigame_rpc/` 或 RPC CI 生成链路，交付时说明 source-of-truth |
| 再生成说明 | 结果里写清楚是否已重新生成，若未生成需写明原因 |
| 调用对齐 | 新旧消息 ID、字段名、调用链是否一致 |

### 2.7 模块任务

| 必查项 | 说明 |
| --- | --- |
| 模块边界 | 是否仍通过 `GameModule.*` / `ModuleSystem.GetModule<T>()` 访问 |
| 生命周期 | `OnInit()`、`Update()`、`Shutdown()` 是否保持合理职责 |
| 事件通知 | 状态变化是否通过事件而不是耦合到具体实现 |
| 清理逻辑 | 关闭时是否释放监听、回调、任务和临时状态 |

### 2.8 战斗任务

| 必查项 | 说明 |
| --- | --- |
| 流程阶段 | 是否符合 `BattleFSM` 主流程阶段 |
| 模块职责 | `BattleModule` 不直接承担网络职责 |
| 时序影响 | 改动是否影响 Preload、LoadScene、Play、Settle、Exit 的衔接 |
| 性能敏感路径 | 高频逻辑是否引入额外分配或无必要日志 |
| TaskLevel 对象池残留 | 若改动 `FPoolUtils`、PoolManager、LevelTask 预加载或战斗进出清理，运行 `Tools/Codex/scan-tasklevel-prefab-residue.ps1 -Root . -FailOnClone -DeepMetrics` |

### 2.9 Monster AI 状态机任务

| 必查项 | 说明 |
| --- | --- |
| 任务归类 | 先说明本次属于 Profile、DecisionScript、Command、State、Execution、View 还是 validation |
| 正式源头 | 当前配置真相是 `MonsterBehaviorConfigProfile` / `MonsterActionParamProfile` ScriptableObject；运行真相是状态机代码 |
| 绑定一致性 | 若涉及接入，检查 `monsterId -> DecisionScriptId + ActionParamProfile` 与默认/测试 Profile override |
| 单链门禁 | Monster AI 作用域不得出现 BehaviorTree、ExternalBehavior、旧 runtime/bridge 或运行模式 fallback |
| 运行时口径 | 不把旧表 `BehaviorId / PhaseBehaviorId`、历史 SpecDriven 文档或 BehaviorDesigner 插件存在误当成当前生产主链 |
| 验证层级 | 明确本次只做到文档复核、CLI 校验、`dotnet build`，还是 Unity acceptance；影响行为逻辑时说明是否需要人工复验 |

### 2.10 编辑器与工具任务

| 必查项 | 说明 |
| --- | --- |
| 运行时隔离 | 编辑器代码不泄漏到运行时目录 |
| 工具入口 | 命令、菜单、脚本入口是否清晰 |
| 失败处理 | 异常、提示和回滚路径是否可理解 |
| 兼容性 | 说明是否依赖 Windows、Unity 版本或外部环境 |
| 离线关卡保存 | 若改动关卡编辑器保存链路，确认 `TaskLevel_*.prefab` 不会保存 `_DefaultPool` / `_DefaultBattle` / `SpawnPool` / `(Clone)` 残留 |

### 2.11 BuildCLI / 构建任务

| 必查项 | 说明 |
| --- | --- |
| 环境差异 | 明确 dev / prod / uat 环境影响 |
| 密钥与配置 | 不把密钥、keystore、本地配置写入仓库 |
| 脚本副作用 | 说明会修改哪些目录或产物 |
| 验证路径 | 说明是否完成脚本级验证、是否需要 CI 或 Jenkins 复验 |
| TaskLevel 构建门禁 | Android 构建链路改动需确认 `TASKLEVEL_PREFAB_RESIDUE_SCAN` 默认开启；不要把关闭扫描作为常规配置提交 |

## 3. 高风险变更附加检查

以下变更在完成最低验证之外，还应追加人工确认或 proposal/spec：

| 变更类型 | 附加要求 |
| --- | --- |
| 跨模块公共接口调整 | 先统一调用方清单，再实施 |
| 协议字段或 `MsgID` 变化 | 明确客户端、服务端、生成链路的同步要求 |
| 配置格式变化 | 明确兼容策略、默认值和回滚方式 |
| Monster AI 结构或绑定链路变化 | 明确 Profile / DecisionScript / Command / State / Execution 边界、Prefab 组件和 Unity smoke |
| UI location / 资源路径变化 | 明确资源引用影响和历史 `.meta` 风险 |
| 构建链路变化 | 明确环境依赖、CI 影响和回退方式 |

## 4. 结果说明模板

每次交付至少补充以下验证结论：

- 已做验证：列出已完成的检查项和手段。
- 未做验证：列出无法完成的验证及原因。
- 剩余风险：列出最可能出问题的地方和建议复验方式。
