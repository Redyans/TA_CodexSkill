# Timeline 片段参数预览刷新与运行时取值参考

> 类型：REFERENCE。提供可迁移的实现模式和排查方法，不另立强制规则。刷新分支、宿主回调与预览退出链依赖 Unity/Timeline 版本，使用前核对目标包源码；本次已查证版本及修复证据见 [ProjectACG 案例](../Profiles/ProjectACG/README_Tech_ProjectACGCharacterRenderTimelineProfile.md#10-render-settings-调参闪烁证据修复与工具)。
>
> 关联规则：`TML-CMP-01`、`TML-EDT-01`、`TML-EDT-03`、`TML-LIF-02`、`TML-VAL-01`。入口见 [Timeline 开发规则](../README_Tech_TimelineDevelopmentRules.md)；跟随对象的暂停、隐藏和恢复另见 [Playable 生命周期参考](timeline-post-animation-follow-and-playable-lifecycle.md)。

## 1. 适用场景

自定义 Track/Clip 的光照、阴影、颜色、方向、偏移、倍率、开关或目标解析参数，需要在编辑期修改后立即更新当前帧。典型问题包括连续拖动 Inspector 时画面闪回、效果时有时无，或改成轻量刷新后参数直到拖动播放头才生效。

先区分触发条件：

| 现象 | 优先检查 | 说明 |
| --- | --- | --- |
| 每改一次参数闪一下，停止编辑就稳定 | Inspector 刷新、图重建、预览退出及重新应用 | 本文重点；还需源码或运行证据确认。 |
| 暂停后参数保存了，画面没更新 | 求值请求、建图时的旧值副本、缓存失效 | 仅刷新窗口通常不够。 |
| 不操作参数时仍持续闪烁 | 多写入者、相机切换、权重边界、Shader/资源及逐帧生命周期 | 不能直接套用 Inspector 修复。 |
| 只有 Scene Handle 不生效 | 编辑入口是否经过宿主回调 | Handle 通常需要显式请求求值。 |
| 只有 Undo/Redo 不更新 | 撤销后是否重新求值、是否存在旧缓存 | 撤销成功与预览更新是两项检查。 |

本文处理编辑期刷新，不替代混合规则、相机仲裁、运行时生命周期或 Shader 排查。

## 2. 前提与依赖

### 2.1 先画出三条链

1. **数据链**：Inspector/Handle → `SerializedProperty` → Clip 资产 → Behaviour/Mixer → MPB、Controller 或渲染协调器 → Shader/RenderFeature。
2. **刷新链**：宿主 `ClipInspector` → 自定义 Editor 回调或默认分支 → `TimelineEditor.Refresh` → 当前帧求值或重建图。
3. **生命周期链**：退出预览/停止图 → 恢复与清理 → 建图/重新求值 → 再次应用覆盖。

只看自定义 Editor 是否主动调用 `ContentsModified`，会漏掉宿主 Inspector 的默认行为；只看字段是否保存，会漏掉运行时仍使用旧副本的问题。

### 2.2 刷新 reason 的职责和优先级

下面是本次查证的 Timeline 包实现模式，升级后以目标版本为准：

| Reason | `TimelineEditor.Refresh` 分支 | 适合用途 |
| --- | --- | --- |
| `ContentsAddedOrRemoved` | `state.Refresh()` | 轨道/片段增删等内容结构变化。 |
| `ContentsModified` | `state.rebuildGraph = true` | 现有图不能消费的建图输入变化。 |
| `SceneNeedsUpdate` | `state.Evaluate()` | 在当前图和当前时间重新计算可实时读取的参数。 |

这些分支按表中顺序使用 `if/else if` 选择，再请求窗口重绘。`ContentsModified | SceneNeedsUpdate` 仍优先设置重建标志，不能把它当成轻量求值。实际刷新完成时机还取决于窗口状态与后续编辑器更新，不能假定 API 返回时所有画面已经更新。

`SceneNeedsUpdate` 不主动设置重建标志，但也不会取消其他路径已经请求的重建。修改后仍闪时，继续查找全部重建来源。

### 2.3 宿主 Inspector 可以隐式请求整图重建

本次版本的 `ClipInspector` 在外层使用 `BeginChangeCheck/EndChangeCheck` 包住自定义 Editor：检测到 GUI 变化后标记 Clip，再尝试将 Editor 转为 `IInspectorChangeHandler`。

- 实现接口：调用 `OnPlayableAssetChangedInInspector()`，由该 Editor 决定刷新方式。
- 未实现接口：默认 `Refresh(ContentsModified)`。

所以自定义 Editor 即使只调用 `ApplyModifiedProperties()`，也可能被宿主默认分支带入整图重建。折叠栏、Handle 显示按钮等纯 GUI 状态也可能触发外层变化检测；可以用序列化写回结果进一步判断是否确实改了资产数据。

### 2.4 重建为何可能表现为闪回

本次版本的 `RebuildGraphIfNecessary()` 会保存时间与播放状态；在未忽略预览的分支先退出预览，再重建 PlayableGraph，恢复时间并重新求值。退出预览会进入 `OnStopPreview()` 与 `AnimationMode.StopAnimationMode()`，撤销动画模式记录的临时属性；自定义 Mixer 的 Stop/Destroy 也可能清理覆盖。

因此一轮编辑可能出现“恢复场景基线 → 重新建图 → 再应用相机、动画或渲染覆盖”。连续调参会反复触发该链，形成画面回跳的候选原因。是否能看到闪烁，仍取决于渲染时序、相机、后处理及资源生命周期，需要实际验证。

静音相机轨后不再闪、只有 Game 视图受影响等现象可缩小范围，不能单独证明唯一根因。也不能据此删除正确的 Stop/Destroy 清理。

## 3. 实现或排查步骤

### 3.1 从最小复现和源码证据收敛

1. 保留一条出问题的自定义 Clip，记录 Director、绑定、当前时间、有效权重、相机/动画轨、编辑字段及播放或暂停状态。
2. 分别记录静止、连续拖参、暂停改值、拖动播放头的表现。可用录屏比较修改前后同一场景，避免凭一次重绘下结论。
3. 搜索自定义 Inspector、宿主 `ClipInspector`、`IInspectorChangeHandler`、`TimelineEditor.Refresh`、`rebuildGraph` 和 `RebuildGraphIfNecessary`。同时对照项目中已有同类 Editor。
4. 沿 Clip 的 `CreatePlayable` 追到 `ProcessFrame`，确认本次字段从何处取值；再读 Stop/Destroy、覆盖清理和最终渲染消费者。
5. 必要时用断点或短期诊断计数验证“拖参 → 重建 → 清理 → 再应用”。每帧大量日志会扰动时序，不宜作为常驻工具。
6. 静音相机轨、动画轨或效果轨做单变量比较，完成后恢复原设置。若证据不支持刷新问题，转查持续闪烁路径。

### 3.2 按消费合同分类字段

| 变化实际影响 | 例子 | 刷新策略 |
| --- | --- | --- |
| 已有图每次求值都能读取的新值 | Clip 引用上的颜色、方向、权重、渲染模式或开关 | `SceneNeedsUpdate`。枚举/开关不天然属于结构变化。 |
| 建图时复制的字段 | `CreatePlayable` 中仅复制一次的强度或偏移 | 当前实现需要重建；若频繁编辑，可先改成 live 读取，再用轻量求值。 |
| 建图时解析的绑定或创建的实例 | 一次性 `ExposedReference.Resolve`、依赖 Prefab 创建的对象 | 更新相应解析/实例生命周期，必要时 `ContentsModified`。 |
| 具备实时重解析合同的引用、名称或路径 | 每次变更会失效目标缓存并重新解析 | 可在现有图中更新；检查丢失/歧义、旧目标清理和新基准。 |
| 轨道/片段增删、Playable 类型或输出连接变化 | 创建轨道、更换图节点类型 | 使用对应结构刷新路径。 |
| 纯 Editor 显示状态 | 折叠栏、显示方向手柄 | 只更新必要的 GUI/SceneView，不请求内容重建。 |

判断问题是“当前图能否正确处理变化”，不是“字段是不是对象引用”。文本可能是实例创建条件，数值也可能影响节点数量；都需要读实现。

### 3.3 选择一种实时取值路径

**直接读取 Clip**：`CreatePlayable` 把 `this` 交给 Behaviour，Mixer 在求值时通过 `behaviour.clip` 读取输入。若项目已经这样实现，Editor 改刷新策略即可，不再加一套逐字段复制。

**显式同步输入**：若 Behaviour 的计算依赖自身字段，保存 Clip 引用，在 `PrepareFrame`/`ProcessFrame` 的适当入口调用 `SyncLiveValues`。同步覆盖全部实时字段，并保留运行时输入的所有权与优先级。

两种方式都不等于“每次绘制 Inspector 就改场景”。副作用仍由正常求值和 Mixer 聚合统一写出。多个 Clip 先混合再应用，不在 Clip Editor 直接抢写材质或全局状态。

### 3.4 用真实数据变化接管宿主刷新

以下为已有自定义 Editor 的嵌入片段，前提是所属类继承 `Editor`、实现 `IInspectorChangeHandler`，且现有 `OnInspectorGUI()` 已执行 `serializedObject.Update()` 和字段绘制。Editor API 来自 `UnityEditor` 与 `UnityEditor.Timeline`；这不是可独立运行的完整脚本。

```csharp
private bool _changed;

// 放在现有 OnInspectorGUI() 的序列化写回位置。
// 累积待处理变化，直到刷新入口消费；不要在重复 GUI 绘制中覆盖为 false。
_changed |= serializedObject.ApplyModifiedProperties();

public void OnPlayableAssetChangedInInspector()
{
    if (!_changed)
        return;

    _changed = false;
    TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
}
```

此简化方案只适用于该 Editor 的全部数据字段都能被现有图实时消费的情况。它用 `ApplyModifiedProperties()` 的结果过滤纯 GUI 改动，也接管了宿主的默认重建分支。

若有建图输入，绘制对应 `SerializedProperty` 时单独检测并累积 `_graphInputsChanged`，在统一回调中决定 reason；不要把整个面板的 `GUI.changed` 都当成建图输入变化。派发方式可替换为：

```csharp
public void OnPlayableAssetChangedInInspector()
{
    bool rebuild = _graphInputsChanged;
    _graphInputsChanged = false;
    if (!_changed)
        return;

    _changed = false;
    TimelineEditor.Refresh(rebuild
        ? RefreshReason.ContentsModified
        : RefreshReason.SceneNeedsUpdate);
}
```

`_graphInputsChanged` 是该 Editor 维护的字段分类标记，需要在真实的数据编辑入口设置；代码片段没有替代该记录工作。无变化时也清掉分类标记，避免下一次普通编辑误走重建。

### 3.5 覆盖 Scene Handle、独立 Inspector 与 Undo/Redo

| 编辑入口 | 数据处理 | 刷新处理 |
| --- | --- | --- |
| Timeline 宿主内的 Clip Inspector | SerializedObject 写回并累积变化 | 宿主调用接口，按类别派发。 |
| Scene Handle | 必要的 Undo、写同一 SerializedProperty、Apply、维护脏状态 | 不假设进入宿主回调；显式调用同一刷新入口，再按需重绘 SceneView。 |
| 独立/锁定 Inspector、菜单、批量编辑工具 | 明确当前编辑资产与 Director 上下文 | 确认是否由宿主托管；否则显式请求适当 Director 求值，避免只刷新当前无关 Timeline。 |
| Undo/Redo | 数据由 Undo 系统恢复，必要时失效缓存 | 核对宿主是否已求值；确有缺口再按需接入 `Undo.undoRedoPerformed`。 |

Scene Handle 可在成功写回后调用 `OnPlayableAssetChangedInInspector()`，避免复制两套 reason 选择逻辑。若接入 `Undo.undoRedoPerformed` 或 `SceneView.duringSceneGui`，在 `OnEnable/OnDisable` 成对注册/注销，过滤无关资产和 Director，避免重复订阅、全局重建或递归刷新。

Undo 后是否刷新不能由 `ApplyModifiedProperties()` 的本帧返回值推断：撤销不是本次 Inspector 手工写入。需要单独验证；没有缺口时不必额外订阅。

### 3.6 把保存、求值与重绘分开排查

| 动作 | 解决的问题 | 不能替代 |
| --- | --- | --- |
| `ApplyModifiedProperties()` | 将序列化编辑写回对象；按 API 合同处理 Undo/脏状态 | PlayableGraph 求值。 |
| `Undo.RecordObject()` | 记录相应直接对象修改的撤销基线 | 写回字段与重新求值。SerializedProperty 已处理的路径不机械重复增加 Undo。 |
| `EditorUtility.SetDirty()` | 必要路径上标记资产已修改 | 保存到磁盘、Undo 或画面更新。 |
| `SceneNeedsUpdate` / 合适的 Director 求值 | 让当前图消费新输入 | 新增节点、重建缓存合同或持久保存。 |
| `SceneView.RepaintAll()` / 窗口重绘 | 绘制更新后的视图、辅助线或 Handle | Mixer 的 `ProcessFrame`。 |
| 用户保存/显式保存资产 | 持久化到磁盘 | 当前帧计算与视觉验收。 |

暂停时尤其容易暴露职责混淆：参数已经保存、SceneView 也重绘了，Mixer 仍可能从未消费新值。

### 3.7 保留清理合同与缓存失效

- 图重建会放大资源清理与恢复问题，但去掉重建不代表清理可以删除。Clip 出界、绑定丢失、Stop、图销毁仍需恢复；恢复方法保持幂等。
- `OnBehaviourPause` 不能独立代表 Clip 结束。结合目标版本的 `FrameData`、有效权重、时间区间和图状态验证暂停与出界行为，具体方式参考 `TML-LIF-02`。
- 写过位姿、MPB 或共享请求后，恢复的是本系统拥有的覆盖；不清空其他系统的 MPB，也不把“关闭效果”硬设为材质基线。
- 实时修改解析参数时，失效旧目标、材质槽或路径缓存；编辑器校验可提供强制刷新，避免显示限时缓存的旧结果。缓存策略见 [对象解析参考](timeline-object-attachment-resolution.md)。

### 3.8 源资产编辑还需要独立的刷新触发

Clip Inspector 的变更回调不会自动覆盖源材质 Inspector 的修改。暂停时即使求值代码会重新复制源材质，也可能因没有新的求值而看不到变化。可监听当前预览真正引用的持久源资产，合并事件后请求当前时间求值，并过滤临时副本避免循环；具体链路、所有权和验证边界见 [材质属性控制器参考](timeline-material-property-controller.md)。

## 4. 风险与不适用边界

| 边界或误区 | 处理方式 |
| --- | --- |
| 只把 `ContentsModified` 改名，新值却仍只在建图时复制 | 补 live 读取或保留必要重建；检查当前帧实际输入。 |
| 实现了接口但回调从不派发刷新 | 宿主默认重建被接管，暂停预览可能不再更新；覆盖所有数据编辑入口。 |
| 把全部对象引用都强制重建，或全部引用都改为 live | 根据节点、解析、实例和清理合同逐项决定，不能一刀切。 |
| 认为轻量刷新能消除所有闪烁 | 继续排查其他重建请求、相机切换、多写入者、Shader 和生命周期。 |
| 图本来应该重建却长期禁止重建 | 可能留下旧实例、旧绑定或旧节点；结构变化仍需正确刷新。 |
| 用删除 Stop/Destroy 清理、禁用相机轨来“修好”闪烁 | 只能掩盖问题，并可能留下覆盖或破坏镜头；回到触发链处理。 |
| 为修 Editor 问题顺带改公共字段名、默认值或 Shader | 增加资产迁移和渲染回归范围；最小修复优先保持序列化合同。 |
| 修改 `Library/PackageCache` 作为项目最终修复 | 包缓存可能重建或升级覆盖；把版本源码用作证据，项目行为由自有 Editor/受控包维护。 |
| 用编辑期刷新结果推断运行时性能 | 编辑器宿主链和运行时不同，分别测试；也不能由单文件编译推断视觉通过。 |

## 5. 验证与回退

### 5.1 验证矩阵

| 验证 | 操作 | 通过条件 |
| --- | --- | --- |
| 编译与兼容 | 用项目匹配的 Unity/Timeline 程序集编译 | API 可用，无新增编译错误；旧 Clip 字段和脚本引用完整。 |
| 连续调参 | 含相机/动画轨，拖动颜色、强度、方向、开关 | 当前帧连续更新，没有编辑触发的整幅画面回跳或效果闪失。 |
| 暂停实时性 | 播放头停在有效 Clip 内改参数 | 不移动播放头也能看到变化。 |
| 编辑入口 | 数值框、圆盘、重置、Scene Handle、独立工具 | 写回同一字段；预览、持久化和 Undo 行为符合各入口合同。 |
| 纯 GUI | 展开/收起分组、切换 Handle 显示 | 不因显示状态反复重建内容图。 |
| Undo/Redo | 分别撤销 Inspector 与 Handle 修改 | 字段与当前帧效果一致；不累积事件回调。 |
| 结构变化 | 改实际需要重建的目标、实例输入或图结构 | 新图/目标生效，旧实例正确释放，不残留覆盖。 |
| 混合与恢复 | Clip 交叠、出界、权重归零、Seek/Pause/Stop/销毁 | 不改变原混合语义，恢复正确，无漂移与共享请求残留。 |
| 渲染与多实例 | 多角色、多 Director、多相机，必要时 Frame Debugger | 作用域与仲裁正确，实际 Pass/材质消费了最终值。 |
| 版本变化 | 升级 Unity/Timeline | 重新核对宿主分支、reason 优先级、预览退出及接口可用性。 |

### 5.2 工具选择与证据等级

`rg` 用于定位完整链路和同类实现；包源码与断点用于确认分支；Git diff 用于控制最小写入与检查空白；匹配 Unity 程序集的 Roslyn 用于局部 API 编译；Unity 编辑器、录屏、Profiler 与 Frame Debugger 用于行为、性能和最终渲染检查。具体可复用命令见 [ProjectACG 工具记录](../Profiles/ProjectACG/README_Tech_ProjectACGCharacterRenderTimelineProfile.md#106-工具与可复用命令)。

交付时分别记录“源码已查证”“编译已通过”“Unity 已复现/回归”及未完成项。截图只能证明一个时刻；无闪烁需要连续操作观察。文档链接、UTF-8 和空白检查只证明文档质量。

### 5.3 回退与后续扩展

回退时只撤销本次 Editor 接口/刷新派发改动，保留原资产和正确清理；接受原有重建限制并明确记录。若发现某字段无法 live 更新，先恢复该字段必要的重建，再单独补同步与缓存合同。

以后增加字段时按顺序检查：序列化兼容 → 所有权/作用域 → 建图或实时消费 → 混合规则 → Inspector/Handle/Undo 入口 → 缓存失效 → 出界恢复 → 暂停与连续调参验证。新增字段未走完这条链，即使 Inspector 已显示，也不能当成完整功能。
