# Unity Timeline 镜头制作、Editor 预览与运行时边界参考

> 类型：`REFERENCE`；适用范围：把 3ds Max、Maya 等 DCC 导出的相机动画组装到 Unity Timeline，并同时支持 Cinemachine 虚拟相机、现有渲染 Camera 和 Prefab 自带实体 Camera 的制作工具；使用前提：必须按目标项目的 Unity、Timeline、Cinemachine/相机栈版本重新验证。
>
> 关联规则：`TML-CMP-01`、`TML-ARC-01`、`TML-LIF-01`、`TML-LIF-02`、`TML-EDT-01`、`TML-VAL-01`。资源生成、Prefab 持久绑定和增量组装见 [Timeline 资产生成、Prefab 绑定与增量组装参考](timeline-asset-generation-and-prefab-binding.md)；ProjectACG 当前实现见 [Timeline 基础资产生成器 Profile](../../ToolDevelopment/Profiles/ProjectACG/ultimate-skill-timeline-generator.md)。

这类工具最容易混淆三件事：DCC 相机动画如何进入 Timeline、编辑器里如何让美术直接看到结果、正式运行时由谁加载和绑定。三者可以复用同一份镜头动画，但不应默认共用同一套生命周期或修改同一批业务代码。

## 1. 适用场景

- DCC 导出的相机同时包含位置、旋转和 FOV K 帧。
- 一个 Timeline 需要按列表顺序连续播放多个镜头，而不是为每个镜头创建一套播放单元。
- 同一生成器需要支持虚拟相机、直接驱动现有渲染 Camera、Prefab 自带实体 Camera。
- 美术把生成的主 Prefab 拖进场景后，希望直接 Play、Seek、暂停和调试。
- 剧情、抽卡或展示 Timeline 当前只需要 Editor 制作预览，正式运行时加载与绑定由业务程序后续接入。
- 已有大招、战斗或演出运行链路必须保留，不能为了新增 Editor 预览而顺手改写。

本参考不规定项目固定轨道名、Prefab 路径、运行模块、相机对象名或 Cinemachine 版本；这些事实应进入项目 Profile。

## 2. 前提与依赖

### 2.1 实现前冻结四类边界

| 边界 | 必须先回答的问题 | 常见后果 |
| --- | --- | --- |
| 动画数据 | Transform 与 FOV 最终写到哪个层级、组件和属性？ | 位置旋转能播，FOV 静默失效。 |
| 编辑器预览 | 谁寻找场景 Camera、谁临时绑定、何时恢复？ | 每次手拖绑定，或停止预览后相机状态残留。 |
| 正式运行 | 谁实例化 Prefab、绑定角色/相机、启动 Director、处理跳过和销毁？ | Editor 能播却误判为运行时已接通。 |
| 工具所有权 | 哪些轨道、Clip、Prefab 节点由工具重建，哪些由美术维护？ | 重复生成误删手工轨道，或旧辅助轨不断堆积。 |

如果需求只授权“生成工具和 Timeline Track”，正式运行模块不在修改范围内。此时 Player 中预览辅助逻辑必须为空操作，文档也必须把运行时接入列为未完成项。

### 2.2 三种镜头输出模式

| 模式 | 数据路径 | 优点 | 代价与边界 |
| --- | --- | --- | --- |
| 虚拟相机 | 动画驱动共享相机层级，Virtual Camera 跟随，Brain 输出。 | 支持 Shot、混合、优先级和 Cinemachine 工作流。 | 需要 Brain、Shot 引用和 FOV 同步；预览绑定更复杂。 |
| 直接驱动现有渲染 Camera | 动画先写共享源 Camera，再把 Transform/FOV 复制给当前渲染 Camera。 | 复用场景相机和已有后处理栈。 | 必须保存并恢复目标 Camera 与 Brain 状态；正式运行需要明确所有权。 |
| Prefab 自带实体 Camera | 动画直接驱动 Prefab 内 Camera，由它输出画面。 | 链路最短，资产自包含。 | 需要处理外部 Camera 启停、AudioListener、后处理和多相机冲突。 |

“Prefab 自带实体 Camera 链路最短”不等于在所有项目中绝对性能最好。真正成本取决于是否发生重复渲染、是否复制相机状态、Cinemachine 更新、后处理栈和相机切换方式，应以 Profiler 与 Frame Debugger 为准。

## 3. 标准数据流

### 3.1 DCC 相机动画只转换一次

推荐数据流：

```text
DCC Camera FBX
    -> 提取 AnimationClip
    -> 归一化到稳定层级路径
    -> 保留 Position + Rotation + Camera.fieldOfView
    -> 多个 Clip 按输入列表顺序放入一条 CameraAnimation
    -> 根据输出模式接到 Virtual / Direct / Entity Camera
```

不要为三种输出模式分别导入三份相机动画。输出差异应位于最后一层适配器，DCC 曲线、Clip 顺序、时长和共享动画轨应保持一致。

### 3.2 多镜头使用单轨多 Clip

- 一条 `CameraAnimation` 承载全部镜头 AnimationClip。
- Clip 顺序严格等于工具列表顺序，前一个 `end` 等于后一个 `start`。
- 每个 Clip 自己同时包含 Transform 和 FOV 曲线。
- Cinemachine Shot 与对应动画 Clip 复用同一份 `{ start, duration }`。
- 不再按镜头生成 `Camera Animation 01/02` 或 `Camera FOV 01/02`。
- 重复生成时清理工具历史前缀轨，避免旧 FOV 轨残留。

单轨方案减少轨道数量、绑定数量和美术维护点，也能避免 Transform 与 FOV 分轨后出现长度或起点漂移。

### 3.3 FOV 保留在原 AnimationClip

DCC 导出的 FOV 首选继续绑定共享源层级上的 `Camera.fieldOfView`，与位置、旋转一起求值。最终输出不直接消费该 Camera 时，再由一个输出适配层同步 FOV：

- 虚拟相机：把共享源 Camera 的 FOV 同步到当前工具虚拟相机 Lens。
- 直接驱动：把共享源 Camera 的 Transform 与 FOV 同步到场景渲染 Camera。
- 实体 Camera：源 Camera 自身就是最终消费者，不需要额外 FOV Track。

只有目标组件无法通过共享源值适配、且项目已验证独立曲线绑定稳定时，才考虑辅助轨；不要把“每镜头一条 FOV Track”作为默认设计。

### 3.4 模块 Prefab 与主 Prefab 实例分职责

镜头模块 Prefab 应只保存可复用机位层级，不默认携带：

- Animator Controller；
- PlayableDirector 或镜头子 Timeline；
- 输出 Camera、Cinemachine Virtual Camera；
- 每镜头同步脚本。

主 Prefab 内只保留一个共享驱动实例，并按需要补：

- 一个无 Controller 的 Animator，供 `CameraAnimation` Generic Binding；
- 一个 Camera，接收 FOV 曲线；虚拟/直接模式默认禁用渲染，实体模式才启用；
- 输出模式所需的虚拟相机或同步 Track。

禁用的共享 Camera 只作为动画属性接收器，不参与渲染。相比每镜头各放 Camera、Animator、Controller 和子 Timeline，这种结构更简单，也更容易统一恢复状态。

## 4. Editor 预览实现

### 4.1 把预览适配做成明确 Track

需要随 Timeline Play/Seek 求值的相机同步，适合用一条覆盖主时长的辅助 Track，而不是依赖窗口常驻 Update：

| 模式 | 推荐 Editor 预览结构 |
| --- | --- |
| 虚拟相机 | `CameraAnimation` + Editor-only 预览 Track + Cinemachine Shot Track。 |
| 直接驱动 | `CameraAnimation` + Camera 同步 Track，Clip 标记为 Editor-only。 |
| 实体 Camera | 只需 `CameraAnimation`，Prefab 内 Camera 在该模式启用。 |

Track、Clip、Behaviour 的职责应保持清晰：

- Track 声明接受的 Clip 和 Binding 类型。
- Clip 保存稳定路径、目标名称、是否仅 Editor 预览等序列化配置。
- Behaviour 管理一次 PlayableGraph 会话的缓存、绑定、同步和恢复。
- 复杂运行状态放在可释放的会话对象中，不把大量临时字段堆到 Clip 资产。

### 4.2 Player 空操作边界必须显式

仅用于美术预览的逻辑应使用以下一种或两种方式限制：

1. 行为代码放在 `#if UNITY_EDITOR` 中，Player 保留可反序列化的 Track/Clip 类型但不执行预览逻辑。
2. 复用运行时 Track 时增加序列化标记，例如 `editorPreviewOnly`；Player 遇到该标记立即返回，而其它正式运行资产仍可使用同一 Track。

不要根据轨道名称猜测是否只用于预览。轨道可以被改名，名称也可能被美术复制；行为边界必须由类型或序列化字段表达。

### 4.3 场景渲染 Camera 的解析顺序

自动找 Camera 时使用稳定且可诊断的优先级，并排除源 Camera、SceneView、Preview、Reflection、RenderTexture Camera、禁用或非激活对象。推荐顺序：

1. 当前有效的 `Camera.main`；
2. 项目约定名称的有效 Camera；
3. 已挂且启用 CinemachineBrain 的 Camera；
4. 有效 Camera 中 Depth 最高者；
5. 显式 Track Binding 作为项目合同要求的兜底或优先项。

具体顺序可按项目调整，但必须固定、可记录诊断，并避免“找到第一个 Camera”这种依赖遍历顺序的实现。

### 4.4 Director 与镜头层级可能是兄弟节点

查找共享源 Camera 时不能只从 `PlayableDirector.transform` 向下搜索。常见主 Prefab 结构是 Director 子节点与相机组为兄弟：

```text
MainPrefab
├─ Director
└─ camAni_group/Camera_Root/Camera
```

解析时先查 Director 自身，再查 Director 父节点；仍找不到时可以按明确名称递归兜底，并把实际搜索范围写入诊断。

### 4.5 虚拟相机预览可临时补 Brain

如果当前有效渲染 Camera 没有 CinemachineBrain，Editor 预览可以临时添加：

- 使用 `HideFlags.HideAndDontSave`，不保存到场景或 Prefab；
- 记录原 Brain 启用状态；
- 在图停止、Playable 销毁、切换 Director、进入 Play Mode 或程序集重载时移除；
- 清理函数必须幂等。

不要把临时 Brain 变成正式场景依赖，也不要因为预览方便而修改业务场景 Prefab。

### 4.6 临时状态必须完整恢复

至少保存并恢复：

- 目标 Camera 的位置、旋转、FOV、enabled；
- CinemachineBrain 的 enabled；
- 临时 Generic Binding；
- 虚拟相机原 Lens FOV；
- Prefab 实体 Camera 的原启用状态；
- 注册的渲染回调或 Editor 事件。

恢复入口至少覆盖 `OnGraphStop`、`OnPlayableDestroy` 和显式 `Dispose`。`OnBehaviourPause` 不一定代表 Clip 真正结束，不能不看 `FrameData` 就销毁状态。

## 5. 正式运行边界

### 5.1 Editor 能预览不等于 Player 已接通

Editor 预览通常只解决：

- 场景 Camera 自动发现；
- 临时 Brain；
- Timeline 轨道绑定；
- Transform/FOV 同步；
- 停止预览后的状态恢复。

正式运行还需要业务程序决定：

- 何时加载和实例化 Timeline Prefab；
- 角色、相机、道具和特效绑定来源；
- 播放、跳过、取消、异常与场景切换生命周期；
- 相机控制权、输入、UI、AudioListener 和后处理切换；
- 资源卸载与对象池策略。

如果这些不在当前任务范围内，应在 Player 中保持预览辅助为空操作，并把“正式运行接入由业务侧实现”写进 Profile 和交付说明。

### 5.2 不要为新演出类型污染既有运行链路

已有大招、战斗或角色展示运行链路如果已经处理 FOV、Camera、角色绑定和恢复，应继续由原系统负责。新增剧情/抽卡 Editor 预览时：

- 不修改无关 Presentation、状态机或网络模块；
- 不把 Editor 自动查找复制进 Player；
- 不改变旧 Timeline 的默认序列化值；
- 复用 Track 时让旧资产保持原行为，新资产通过显式标记启用 Editor-only 边界。

## 6. 绑定、落盘与重复生成

### 6.1 Generic Binding 与 ExposedReference 分开使用

| 引用关系 | 推荐方式 |
| --- | --- |
| AnimationTrack → Animator、同步 Track → Camera、CinemachineTrack → Brain | `PlayableDirector.SetGenericBinding`。 |
| Cinemachine Shot → Virtual Camera、Control Clip → Prefab 内 Prop/FX | `ExposedReference` + `SetReferenceValue`。 |

Clip 指向的目标必须是最终主 Prefab 内的持久实例，不能是 Project 资源，也不能是第一次 `LoadPrefabContents` 得到、保存后已失效的临时对象。复杂引用按“保存层级 → 重新加载 → 重绑 → 回读自检”处理。

### 6.2 工具只删除自己拥有的轨道

工具轨道所有权使用“固定类型 + 固定名称”共同判断：

- 重复生成时删除并重建工具拥有的目标轨道；
- 清理已知历史前缀，例如旧版每镜头 Animation/FOV 轨；
- 保留美术新增的其它轨道及其相对顺序；
- 单模块组装只处理目标模块，不因其它列表为空而失败；
- 新增工具轨道时同步更新创建、清理、自检和文档。

只按名称删除容易误删美术同名轨；只按类型删除会误删同类人工轨。两者结合并配合稳定前缀迁移，边界更可控。

## 7. 性能实现

- 缓存源 Camera、目标 Camera、Brain、虚拟相机列表和反射 FieldInfo。
- 场景 Camera 不要每帧全量扫描；使用缓存失效检查与小频率重试。
- 渲染 Camera 同步优先在目标 Camera 即将渲染的回调中执行，避免 Timeline 输出顺序造成一帧延迟；Editor 非渲染预览可在求值帧补一次同步。
- 不在 `ProcessFrame` 每帧创建 List、字符串或反射查询结果。
- 只有一个共享 Animator 和源 Camera，不为每个镜头实例化重复播放组件。
- 临时 Editor 组件使用 `HideAndDontSave`，结束后立即释放。
- Player 空操作路径应尽早返回，不注册事件、不扫描场景、不产生持续轮询。

性能结论必须通过 Profiler、Frame Debugger 和代表性多镜头资产验证，不能只根据组件数量推断。

## 8. 常见问题与解决方式

| 现象 | 根因 | 解决方向 |
| --- | --- | --- |
| 位置旋转有效，FOV 不动 | FOV 曲线指向已移除的 Camera，或输出端没有同步 Lens。 | 保留共享源 Camera 作为曲线接收器，再由输出适配层同步 FOV。 |
| Timeline 出现大量 FOV 轨道 | 每个镜头拆了独立辅助轨。 | Transform/FOV 合并回同一 AnimationClip，镜头共用一条 CameraAnimation。 |
| Editor 播放没有镜头变化 | CinemachineTrack 没绑定 Brain，或直接驱动 Track 没找到有效场景 Camera。 | 增加 Editor 预览 Track、稳定 Camera 解析和中文诊断。 |
| 停止预览后相机位置/FOV 被改掉 | 没保存原状态，或只在单一回调恢复。 | 会话开始快照，GraphStop/Destroy/Dispose 幂等恢复。 |
| Director 下找不到源 Camera | Director 与相机组是兄弟节点。 | 同时搜索 Director 自身与父节点。 |
| 无 Brain 时虚拟相机不输出 | 预览场景没有 CinemachineBrain。 | Editor 临时添加 HideAndDontSave Brain，结束后移除。 |
| Player 中剧情/抽卡开始抢相机 | Editor 预览逻辑没有 Player 边界。 | `#if UNITY_EDITOR` 或 `editorPreviewOnly`，Player 立即返回。 |
| 新功能影响既有大招 | 复用 Track 时改了默认值或修改了原运行模块。 | 新资产显式标记，旧资产保持默认；运行模块修改严格按授权范围。 |
| Camera Track 绑定每次都要手拖 | 生成 Prefab 未写 Generic Binding，或预览没有自动绑定。 | Prefab 持久绑定；场景临时目标由 Editor 预览会话设置并恢复。 |
| 相机同步有一帧延迟 | 自定义 Track 与 AnimationTrack 的求值顺序未形成合同。 | 在渲染前同步，或建立明确 PlayableGraph 依赖并验证 Seek/首帧。 |

## 9. 风险与不适用边界

| 边界 | 说明 |
| --- | --- |
| Cinemachine 版本 | Brain、Virtual Camera、Lens 与 Pipeline API 会变化；反射字段和组件类型必须重验。 |
| 物理相机 | `fieldOfView` 不一定是最终视觉真相；Physical Camera、焦距、Sensor Size、Gate Fit 需要单独合同。 |
| 多 Camera 场景 | 自动选择只能作为明确规则，不应替代业务侧相机所有权。 |
| 多 Director | 两个 Timeline 同时驱动同一 Camera 时需要全局仲裁，单会话恢复不足以保证正确。 |
| Timeline 求值顺序 | 面板轨道上下顺序不是跨输出的执行保证；必须验证首帧、Seek、Pause 和渲染回调。 |
| Editor Play Mode | `UNITY_EDITOR` 在 Editor Play Mode 仍成立；“Editor-only”若需要在 Play Mode 生效，应在文档明确。 |
| Prefab 实体 Camera | 可能与场景 Camera、AudioListener、URP Camera Stack 和后处理重复，必须实测。 |
| 静态编译 | 编译通过不能证明 Prefab fileID、Timeline binding、Cinemachine 输出或视觉结果正确。 |

## 10. 验证与回退

### 10.1 最小验证矩阵

| 场景 | 通过条件 |
| --- | --- |
| 单镜头 | Position、Rotation、FOV 与 DCC 代表帧一致。 |
| 多镜头 | 所有 Clip 在一条 CameraAnimation 上按列表顺序连续；Shot 起点和长度一致。 |
| 虚拟相机 | 有 Brain 和无 Brain 的 Editor 场景都能预览；临时 Brain 不落盘。 |
| 直接驱动 | 优先使用当前有效渲染 Camera；Transform/FOV 同步；停止后完整恢复。 |
| 实体 Camera | 只启用预期 Camera；外部 Camera、AudioListener 和后处理没有冲突。 |
| Play/Seek/Pause/Stop | 首帧、跳帧、末帧、重复播放无一帧延迟和状态残留。 |
| 保存重开 | Generic Binding、Shot ExposedReference 和 Prefab 子对象引用仍有效。 |
| 重复生成 | 不残留旧 FOV/Animation 辅助轨，不删除美术轨道。 |
| Player 边界 | Editor-only 资产不扫描场景、不注册预览回调、不控制正式 Camera。 |
| 既有链路 | 原大招/战斗 Timeline 的绑定、FOV、停止恢复和默认模式无回归。 |

### 10.2 回退策略

- Editor 自动绑定不稳定时，回退到显式场景绑定按钮，不把名称猜测带进 Player。
- 临时 Brain 与目标 Camera 冲突时，回退到要求场景预放 Brain，并在工具中给出明确提示。
- FOV 输出无法稳定同步时，保留源 AnimationClip，不破坏曲线；先回退到实体 Camera 对拍定位。
- 新 Track 影响旧资产时，保留类型与默认值，使用新序列化标记隔离新行为。
- 单模块组装失败时停止，不扩大为完整重建；已有主 Timeline/Prefab 通过版本管理恢复。

### 10.3 交付记录

交付说明至少分别记录：

- DCC 曲线提取与路径归一化；
- 单轨多 Clip 与 FOV 所在位置；
- 三种镜头模式的 Editor 预览结果；
- Player 是否实际接入；
- 既有运行链路是否修改；
- 编译、Prefab 重开、Timeline Play/Seek/Stop、Cinemachine 和视觉验证；
- 未完成的业务运行接入与剩余相机所有权风险。
