---
name: timeline-blendshape-track-authoring-and-runtime-binding
description: BlendShape Timeline 轨道的可迁移实现参考，覆盖多 Renderer/BlendShape 编辑、Generic Binding、动态模型、Mixer/Layer、编辑器预览、性能与验证回退。
---

# BlendShape Timeline 控制轨道：制作、绑定与运行时动态模型参考

> 类型：REFERENCE；适用范围：Unity Timeline 驱动角色 `SkinnedMeshRenderer` 的 BlendShape 权重。
> 使用前提：必须用目标项目的 Unity/Timeline 版本、序列化资产、绑定方式和实际模型层级重新验证。本参考不把任何项目路径、具体枚举值或当前验证状态当成跨项目事实。

本参考沉淀一类常见需求：在 Timeline 中为一个或多个 Renderer 选择多个 BlendShape，用曲线和滑条制作表情或变形；角色可能保存在 Timeline Prefab 中，也可能在运行时异步加载；同一轨道需要支持 Clip 淡入淡出、Add Layer 和多种混合；编辑器中要能手动绑定并预览，运行时又要能只保存轨道数据、在模型加载完成后重新绑定 Director。

## 1. 适用场景

适合以下组合：

- 一个角色根节点下有多个 `SkinnedMeshRenderer`，每个 Renderer 有多个 BlendShape。
- 美术需要通过 Renderer 下拉框、BlendShape 搜索下拉框、折叠分组、滑条和 `AnimationCurve` 制作动画。
- Timeline 资产需要放进 Prefab 或独立资源，不能序列化运行时实例的场景对象引用。
- 角色在 `PlayableDirector` 创建前后都可能变化：静态层级、Timeline Prefab 内角色、异步实例化、换装、换 Mesh。
- 同一参数可能被相邻 Clip、交叠 Clip 或 Add Layer 同时驱动。
- Clip 结束、Seek、Stop、Graph 重建或绑定失效后，角色要恢复 Timeline 接管前的权重。

不适合直接套用的情况：

- 目标不是 `SkinnedMeshRenderer`，而是材质属性、骨骼姿态或 GPU Morph，需要换成对应的状态所有者和写入边界。
- 多个系统同时写同一权重但没有所有权合同。此时先定义仲裁者，再决定是否由 Timeline 写入。
- 只想做一次性场景烘焙；实时 Mixer、绑定缓存和恢复逻辑可以裁剪，但仍应保留可回退资产。

## 2. 前提与依赖

### 2.1 先冻结序列化合同

在改 Inspector 之前，先确认旧资产的字段、默认值和绑定类型。推荐每一条 BlendShape 记录保持扁平、可序列化：

| 字段 | 作用 | 兼容策略 |
| --- | --- | --- |
| `rendererPath` | 从轨道绑定根到 Renderer 的相对路径，空值表示绑定根自身或单 Renderer 入口 | 路径是主定位键；层级变化需要显式迁移 |
| `blendShapeName` | BlendShape 名称 | 名称优先于索引，便于 Mesh 重新导入后保持语义 |
| `blendShapeIndex` | 名称找不到时的旧索引回退 | 只作回退，不能作为唯一长期合同 |
| `weight` | 归一化 Clip 时间上的 `0..100` 曲线 | Inspector 范围与运行时 Clamp 必须一致 |

不要为了在 Inspector 中按 Renderer 分组，就把序列化列表改成嵌套字典或运行时对象引用。推荐“运行时扁平数据 + 编辑器分组视图”：Inspector 按 `rendererPath` 临时分组，删除/增加操作仍只改原列表。这样旧 `.playable` 和 Timeline Prefab 的字段布局稳定，运行时遍历也简单。

### 2.2 明确绑定根和路径语义

所有路径都必须有唯一基准：

- 编辑器手动绑定时，基准是轨道的 Generic Binding 对象。
- Director 层级模式时，基准是 `PlayableDirector` 所在 GameObject。
- 运行时 Generic Binding 时，基准是运行时传入的模型根，或从 Director 根按配置路径找到的 GameObject。
- Renderer 路径只在绑定根内部解析，不应在全场景盲搜。
- Renderer 变更后清空旧 BlendShape 选择，避免相同索引被误认为同名形状。
- 名称匹配失败时可回退到保存索引，但必须在编辑器校验中提示 Mesh 已变化。

### 2.3 区分两种绑定模式

| 模式 | 资产保存什么 | 运行时如何得到角色 | 适用场景 |
| --- | --- | --- | --- |
| Director 层级模式 | 轨道数据 + Director 根下的相对角色路径；可选轨道手动绑定 | 先用轨道显式绑定，未绑定时从 Director 根按路径查找 | 角色保存在 Timeline Prefab 或 Director 自身层级 |
| Runtime Generic Binding | 轨道数据 + 可选的相对 GameObject 路径，不保存实例引用 | 模型加载完成后调用绑定 API 设置 Generic Binding，再重绑 Graph 输出 | 异步加载、换装、模型实例不稳定 |

两个模式应共用 Renderer 相对路径和 BlendShape 名称解析规则。不要让编辑器用一套路径语义、运行时再发明另一套查找逻辑。

## 3. 实现或排查步骤

### 3.1 先画出数据流和所有权

推荐把轨道拆成以下职责：

~~~mermaid
flowchart LR
  A[PlayableDirector / Track Binding] --> B[Binding Root + Relative Path]
  B --> C[Renderer Cache]
  C --> D[Clip Inputs]
  D --> E[Layer/Clip Aggregation]
  E --> F[Original State Store]
  F --> G[SetBlendShapeWeight]
  G --> H[Restore on exit / rebind / destroy]
~~~

- `TrackAsset`：声明 Clip 类型、绑定类型、Layer 和 Mixer 创建。
- `PlayableAsset/Clip`：只保存曲线、Renderer/BlendShape 定位和混合模式。
- `PlayableBehaviour`：传递 Clip 数据，不直接扫描场景或写 Renderer。
- `Mixer`：读取输入权重、解析目标、聚合所有有效 Clip，最后一次性写出。
- 状态存储：第一次控制每个目标时快照原始权重；目标不再被当前轨道控制或图销毁时恢复。
- Editor：提供下拉选择、搜索、折叠、校验、Undo、预览刷新和表情预设，不把折叠状态写入运行时资产。

### 3.2 绑定流程设计

#### Director 层级模式

建议解析优先级：

1. 轨道有显式 Generic Binding，且对象是 GameObject 或 Renderer：使用显式对象。
2. 没有显式绑定：从 Director 根按 `directorCharacterPath` 查找角色根。
3. 路径为空：使用 Director 根作为角色根，或根据项目约定报“未选择角色”。
4. 只在角色根下缓存 `SkinnedMeshRenderer` 和相对路径。

这满足“可以把 Timeline Prefab 内的角色拖到轨道上”，也保留“不绑定轨道、从 Director 自身层级按路径找角色”的用法。

#### Runtime Generic Binding

运行时模型加载完成后再绑定，推荐顺序：

~~~csharp
// modelRoot 是异步加载完成的实例根节点。
int count = BlendShapeTimelineBinding.BindAll(director, modelRoot);
if (count > 0)
{
    director.time = 0d;
    director.Evaluate();
    director.Play();
}
~~~

若模型位于 Director 层级下，改用相对路径入口（二选一，不要在同一次绑定中连续调用两种入口）：

~~~csharp
int count = BlendShapeTimelineBinding.BindAllByPath(
    director,
    "Characters/Hero");
~~~

绑定换装或替换 Mesh 时，使用显式的 `Rebind`/`Refresh` 流程，而不是等待下一帧碰运气：

~~~csharp
BlendShapeTimelineBinding.Rebind(director, newModelRoot);
// 绑定根不变、只替换了 Renderer 或 Mesh 时单独调用：
BlendShapeTimelineBinding.Refresh(director);
// 完全解除 Timeline 控制时调用：
BlendShapeTimelineBinding.Unbind(director);
~~~

上面三个调用代表不同场景：换模型根使用 `Rebind`；同一绑定根下替换 Renderer/Mesh 使用 `Refresh`；完全解除控制使用 `Unbind`。如果项目里的 `Rebind` 已经内部刷新 Graph，就不要紧接着再调用一次 `Refresh`。

如果 Director 已经创建了 PlayableGraph，设置 Generic Binding 后要调用目标版本提供的 Graph 输出重绑入口（常见为 `RebindPlayableGraphOutputs`），然后执行一次 `Evaluate`。否则轨道上的绑定对象可能已经更新，但 Mixer 仍持有旧输出。

### 3.3 Renderer 与 BlendShape 解析

推荐解析顺序：

1. 根据 `rendererPath` 从绑定根的缓存字典取 Renderer。
2. 先用 `sharedMesh.GetBlendShapeIndex(blendShapeName)` 按名称解析。
3. 名称不存在时才使用 `blendShapeIndex`，并检查索引范围。
4. Renderer、Mesh 或 BlendShape 缺失时跳过该条目并给出可定位的校验信息。
5. 同一个 Renderer + BlendShape 被同一轨道重复添加时，编辑器报重复，而不是依赖遍历顺序覆盖。

运行时不要每帧执行 `Transform.Find`、`GetComponentsInChildren` 或重复解析索引。绑定根、路径、Mesh 实例变化时才重建缓存。

### 3.4 Clip、Layer 与混合语义

Clip 只产出“目标值 + Clip 权重”，Mixer 统一聚合。建议明确以下四种模式的数学含义（权重范围为 `0..1`，BlendShape 值为 `0..100`）：

| 模式 | 语义 | 典型用途 |
| --- | --- | --- |
| Override | `Lerp(original, clipValue, clipWeight)` | 主表情、嘴型 |
| Additive | `original + clipValue * clipWeight` | 在基础表情上叠加眼睛或脸颊 |
| Multiply | 把 `0..100` 转为因子，按权重从 `1` 插值到该因子后相乘 | 衰减或压低已有表情 |
| Maximum | 当前结果与曲线结果取较大值 | 多来源表情取更强者 |

一个可审计的聚合顺序：

1. 清空当前帧的所有目标状态。
2. 跳过无效输入和零权重输入。
3. 对每个 Clip 计算归一化时间，读取曲线值。
4. 按 Renderer + BlendShape 键聚合。
5. 先应用 Override，再应用 Additive/Multiply/Maximum，最后 Clamp 到 `0..100`。
6. 由状态存储把结果写回 Renderer。

Add Layer 时，每个 Layer 先独立聚合，再按 Layer 权重合并；不能让子 Layer 直接写 Renderer，也不能依赖 Track 在 Timeline 面板中的上下顺序决定覆盖关系。多个 Layer 写同一目标时，必须在 Mixer 中定义优先级或显式禁止组合。

### 3.5 原始值快照与恢复

第一次控制某个目标时保存 `GetBlendShapeWeight` 的值，并记录最后一次写入值：

- 当前帧仍控制该目标：只在结果变化超过小阈值时调用 `SetBlendShapeWeight`。
- 当前目标不再出现在聚合结果：先恢复快照，再从状态字典移除。
- 绑定对象变化、Mesh 替换、`OnPlayableDestroy` 或 Director 解除绑定：恢复旧目标并清空缓存。
- 恢复函数必须幂等，重复调用不能把已恢复值再次当作新基线。

这条规则能解决“Clip 播完表情留在脸上”“Seek 后旧 Renderer 仍被写入”“换装后新模型继承旧缓存”等问题。若角色系统在 Timeline 同时写同一 BlendShape，必须把所有权改为显式协调器，否则恢复动作可能覆盖角色系统的新值。

### 3.6 编辑器制作体验

推荐 Inspector 结构：

- Renderer 折叠组：每组显示 Renderer 下拉框、删除 Renderer、该 Renderer 的所有 BlendShape。
- BlendShape 条目：搜索下拉框、曲线、当前帧滑条、删除按钮。
- 轨道级工具：捕获当前权重、当前帧归零、保存/应用表情预设。
- 校验区：Renderer 缺失、Mesh 缺失、BlendShape 缺失、重复目标、未绑定根。
- 预览区：手动 Generic Binding 和 Director 层级路径都能直接驱动当前场景对象。

下拉搜索应从当前 Mesh 的真实 BlendShape 名称生成，不能让用户手写名称作为主要入口。搜索弹窗至少要支持大小写不敏感包含匹配，并回显当前选择。折叠状态、搜索文本和临时候选列表保存在 Editor 内存，不写入 Clip 序列化字段。

捕获当前表情时，只把当前权重写到当前 Clip 的归一化时间点；不要把一个瞬时姿态覆盖整条曲线。表情预设只保存 Renderer 相对路径、BlendShape 名称和权重，不保存场景 GameObject 引用，这样可以跨实例复用。

### 3.7 编辑器刷新和运行时求值

把修改分为两类：

| 修改 | 处理 |
| --- | --- |
| 权重、曲线、混合模式等数值变化 | 同步 Behaviour 的实时字段，触发 Scene/Timeline 数值刷新，避免无意义重建 Graph |
| 增删 Clip、增删 Renderer/BlendShape、绑定对象或路径变化 | 允许结构刷新/重建，并重新解析缓存 |

编辑器预览和运行时必须共用绑定根、路径和名称解析语义。预览目标失效时要显示明确警告，不要静默把值写到上一次缓存对象。Timeline 暂停时修改滑条应立即可见；图重建、停止或退出预览时应执行恢复。

### 3.8 性能实现方式

低成本且稳定的优化顺序：

1. 绑定或路径变化时一次性扫描 Renderer，建立 `Dictionary<string, SkinnedMeshRenderer>`。
2. Mesh 变化时刷新 BlendShape 名称到索引的缓存。
3. 每帧只遍历有效 Playable 输入和已解析条目。
4. 只有结果变化超过 epsilon 才调用 `SetBlendShapeWeight`。
5. 失败日志限流；不要在每帧失败时构造完整候选描述。
6. 大量角色/Clip 时用 Profiler 证明瓶颈后，再考虑 Director 级调度或共享缓存。

优化不能改变绑定和恢复语义。缓存命中不能成为“路径改了但仍写旧 Renderer”的理由；缓存键至少包含绑定根、Renderer 路径、Mesh/Renderer 实例变化信息。

### 3.9 常见问题与解决方式

| 症状 | 根因 | 解决方式 |
| --- | --- | --- |
| Timeline 保存后找不到 SkinMeshRenderer | 资产保存了场景实例引用，或动态模型尚未加载 | 资产只保存路径/名称；编辑器用手动绑定预览，运行时模型完成后调用 Generic Binding |
| 轨道绑定已设置但仍找错角色 | Mixer 仍按 Director 全局搜索，未遵守显式绑定优先 | 有绑定时只在绑定根内解析；找不到就报错，不降级全场景搜索 |
| Renderer 下拉切换后 BlendShape 数值跳到另一个形状 | 复用了旧索引或旧名称 | 切换 Renderer 时清空名称和索引，重新从新 Mesh 生成候选 |
| BlendShape 改名后动画失效 | 只保存了索引，Mesh 顺序也变了 | 名称为主键，索引只回退；编辑器标红并要求重新选择 |
| 运行时绑定后 Timeline 不生效 | Graph 已在绑定前创建，输出仍指向旧对象 | 设置 Generic Binding 后重绑 Graph 输出，执行 `Evaluate` 再播放 |
| Clip 结束后权重残留 | 没有原始值快照，或只在 Pause 回调恢复 | 状态存储跟踪“当前受控集合”，目标离开集合和 Graph 销毁都恢复 |
| 多个 Clip 结果依赖轨道上下顺序 | 各 Clip 直接写 Renderer | 改为 Mixer 统一聚合，明确定义 Override/Additive 等数学语义 |
| 动态加载后路径偶尔找不到 | 绑定发生在实例化之前，或缓存未失效 | 等加载完成后绑定；换模时显式 Rebind/Refresh，清掉旧缓存 |
| 编辑器滑条能动但场景不更新 | 数值写入后未同步 Behaviour 或未触发合适刷新 | 数值变化走 SceneNeedsUpdate；结构变化才重建 Graph；预览目标失效时重解析 |
| 运行时每帧卡顿 | 每帧扫描层级、解析索引或打印失败日志 | 建缓存、按变化失效、只在结果变化时写值，并对日志限流 |
| 表情预设在另一个角色上错位 | 预设保存了实例引用或只存索引 | 只保存相对路径 + BlendShape 名称 + 权重；应用时重新解析 |

## 4. 风险与不适用边界

- 模型导入、换装或 DCC 改名会破坏路径和名称合同；应提供迁移/校验工具，而不是静默按错误索引写入。
- `SkinnedMeshRenderer.sharedMesh` 可能被替换。换 Mesh 后必须刷新名称/索引缓存，并决定旧目标是恢复后放弃，还是按新 Mesh 重新绑定。
- Additive、Multiply、Maximum 不是 Unity 内置统一标准；跨项目迁移时必须带公式和示例，不能只迁移枚举名称。
- BlendShape 权重通常是 `0..100`，但部分项目会使用非标准曲线范围。编辑器、Mixer 和资源导入必须统一约定。
- 状态恢复只对 Timeline 自己拥有的写入负责。若另一个系统在 Timeline 播放期间写入同一目标，应改为请求/仲裁模型。
- 预览能运行不代表 Player 构建能运行；编辑器 API、Timeline 版本内部实现和动态加载时序都要单独验证。
- 不要用全场景名称搜索替代显式绑定。重名角色、嵌套 Director 和多实例场景必须报歧义或要求路径。
- 性能结论只能来自目标项目的 Profiler/帧时间证据；“只扫描一次”是实现意图，不等于已证明无开销。

## 5. 验证与回退

### 5.1 最小验证矩阵

| 类别 | 用例 | 通过条件 |
| --- | --- | --- |
| 序列化 | 旧 Clip 打开、保存、重新导入 | 字段不丢、曲线不变、没有 Missing Script |
| 编辑器 | Renderer 下拉、BlendShape 搜索、折叠、多个条目 | 选择稳定、同 Renderer 可添加多个形状、Undo 可用 |
| 预览 | 手动绑定、Director 路径、拖帧、暂停改值 | 当前场景即时更新，失效目标有提示 |
| 混合 | 相邻 Clip、交叠 Clip、Add Layer、四种混合模式 | 结果符合公式且不依赖面板顺序 |
| 生命周期 | Clip 出界、Seek、Stop、Graph 重建、Director 销毁 | 原始权重恢复，无旧对象继续写入 |
| 动态绑定 | 模型晚于 Director 加载、换装、换 Mesh | 绑定后 Evaluate 生效，旧缓存清理 |
| 多实例 | 两个同名角色、多 Director | 只写指定根；无法区分时明确报错 |
| 性能 | 多 Renderer、多条目、长时间播放 | 无每帧层级扫描；写入次数和 GC 在目标预算内 |

### 5.2 编译与行为证据要分开

静态编译只能证明类型、引用和语法成立，不能证明 Timeline 预览、Layer、Seek/Stop、动态绑定和视觉结果。交付记录应分别写：

- 已通过：Runtime/Editor 编译、静态格式检查、序列化迁移检查。
- 已在 Unity 验证：Inspector、拖帧、播放、交叠、Layer、绑定、恢复、多实例。
- 尚未验证：Player 构建、移动端、极端并发、Mesh 热替换、与其它表情系统并写。

### 5.3 回退策略

- 解析歧义或模型命名不稳定：回退到显式 Generic Binding，要求调用方传入模型根。
- Timeline 版本不支持细粒度预览刷新：回退到结构刷新，接受一次预览重建，并记录闪动边界。
- 无法保证多个系统写入顺序：回退到唯一所有权或新增协调器，不继续叠加“最后写入者赢”。
- 运行时模型生命周期无法在轨道内可靠获知：把 Bind/Rebind/Unbind 暴露给加载器，由加载流程显式调用。

### 5.4 本次案例可迁移结论

本次实现中最值得复用的不是某个类名，而是以下决策：

1. 先冻结旧 Clip 的序列化字段，再用扁平列表承载多 Renderer/多 BlendShape，Inspector 只做分组视图。
2. 把“编辑器手动绑定”和“运行时动态 Generic Binding”设计成同一条绑定合同的两个入口。
3. 以 Renderer 相对路径 + BlendShape 名称为稳定定位，索引只做兼容回退。
4. Mixer 统一聚合并一次性写出，状态存储负责原始值快照、差异写入和生命周期恢复。
5. 动态加载场景必须有显式 BindAll/BindByPath/Rebind/Refresh/Unbind API，不能依赖 Timeline 资产持久化运行时对象。
6. 编辑器校验、预览解析和运行时解析尽量共用语义；失败原因要区分未绑定、找不到和目标失效。
7. 性能优化从绑定缓存和写入去重开始；只有有 Profiler 证据时才引入更复杂的全局调度。

具体工程文件、类型名、编译证据与未验证边界见 [ProjectACG BlendShape Track Profile](../Profiles/ProjectACG/blendshape-control-track.md)。
