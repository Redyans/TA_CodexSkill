<!-- BEGIN automation:ta date:2026-11-11 layer:REFERENCE -->
# Unity 资产盘点与 Prefab 同步参考｜2026-11-11

> 类型：REFERENCE；适用范围：Unity `AnimationClip` / `.anim` 资产盘点、Animator 引用统计、FBX 生成 Prefab 更新；使用前提：必须核对目标 Unity 版本、资产序列化方式、Prefab 生成策略和实际构建/运行时配置。
> 关联既有规则：工具模块 `TOOL-CMP-01`、`TOOL-OPS-01`～`TOOL-OPS-03`、`TOOL-VAL-01`、`TOOL-VAL-02`。当日项目 Profile 见 [ProjectACG 工具案例](../../Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-11.md)。

## 1. 适用场景

当问题是“动画资源多大、有没有压缩、曲线采样密度如何、各角色哪些动画最大”时，先区分统计对象和使用阶段。源文件磁盘大小、导入后的 `AnimationClip` 数据、构建包体和运行时加载内存是不同指标，不能把一个 Inspector 数字当成其余三项。

## 2. 前提与依赖

- 从 Animator/Controller 的实际引用关系收集 clip，并区分状态机、角色本体、道具、重复引用、缺失 GUID 和孤儿资源。
- 明确大小是十进制 MB 还是 MiB、按磁盘字节还是导入对象统计；报告保留原始字节数用于排序，只把格式化列作为显示。
- 压缩状态、采样率、逐帧关键帧和 Scale 变化是不同字段。恒定 Scale 也可能存在关键帧，故应同时记录“有无曲线/关键帧”和“值是否变化”。
- 明确“全帧”的判定定义和采样端点；不同曲线、帧率和压缩导入不能只凭资源大小推断。

## 3. 实现或排查步骤

1. 建立 Controller → State/Motion → Clip 引用表，以稳定资源标识去重；缺失引用单独列出，不计为有效文件。
2. 对每个 `.anim` 保存磁盘字节数，并读取导入设置中的压缩标记、帧率及各类曲线关键帧时间和值。将缺失数据标为未知，不以默认值填补。
3. 对 Scale 同时判断是否有 K 帧、值是否恒定以及是否变化；对逐帧状态逐曲线应用已声明的帧时间容差规则。
4. 汇总页与明细页分开；保留 Controller/角色、有效 clip 数、总源文件大小、最大单个 clip、压缩、全帧、Scale 等可筛选列。缺失引用单独成页。
5. 数值排序列保留数值类型，不把 `30.75 MB` 作为文本存储；显示可采用 MB 两位小数，排序不会错位。若为业务筛选保留多个输出版本，明确标注范围与单位。
6. 需要判断“加载后占用”时，在目标 Unity/Player 中用 Profiler 或 Memory Profiler 观察实际加载的 `AnimationClip`。需要构建体积时检查 AssetBundle/Addressables 或 Player 构建报告及依赖；对比前冻结打包压缩设置和依赖闭包。

## 4. 风险与不适用边界

- `.anim` 文本源文件的磁盘体积不等于导入对象内存、压缩包体或运行时总动画开销。
- Inspector 的导入数据估算可作为参考，不能替代运行时 Profiler；Animator 状态机、混合计算、Avatar 等额外开销也不包含在单个 Clip 估算里。
- Build Report 的单文件/Bundle 大小受构建压缩和共享依赖影响；不能简单累加源文件大小得到包体增量。
- Excel 排序格式正确只证明表格列类型，不能证明统计口径正确；仍需抽样比对 Controller 引用、缺失资产和曲线内容。

## 5. 验证与回退

用一个已知 Controller 手工核对其引用、丢失 GUID、曲线和源文件字节数；抽查恒定与变化 Scale、稀疏与逐帧曲线。确认 MB 列按数值排序且整行同步移动。运行时内存或包体结论应回到目标构建/运行环境实测；若无法完成，报告中仅保留源文件大小和已检查的导入字段。

来源：[会话 01a1c8a9-dcd5-7b80-a906-86aafbe8762f](../../../../VA_Worlk_memory/sessions/01a1c8a9-dcd5-7b80-a906-86aafbe8762f.md)，消息 2–19；[会话 01a1ca14-5334-7541-816c-469b0474e54a](../../../../VA_Worlk_memory/sessions/01a1ca14-5334-7541-816c-469b0474e54a.md)，消息 2–5。

## 6. FBX Prefab 改名与删除后的同步

### 6.1 适用场景

FBX 中的 Mesh 节点改名或删除后，重新生成 Prefab 却同时留下新旧 GameObject/Mesh；或“保留已有”模式保护手工配置时，也把已失效的源节点保留下来。

### 6.2 前提与依赖

- 明确哪些节点/组件由源 FBX 生成器拥有，哪些是人工添加、Prefab override 或第三方组件。
- 增量补充和全量刷新是不同策略；“保留已有”通常是局部覆盖规则，不保证新源层级完整同步。
- 持久化来源状态应包含源 FBX GUID、上一轮源层级稳定路径和工具生成的 Mesh 路径。仅用叶节点名称无法区分重名层级。
- 旧 Prefab 可能没有历史标记；兼容回退必须比有来源记录时更保守。

### 6.3 实现或排查步骤

1. 比较所有更新入口：新增/增量、保留模式、刷新模式和失败/取消路径，找出合并与清理语义差异。
2. 生成新层级前读取上次持久化的源状态，将旧源路径与本次 FBX 路径对照，识别改名/删除候选。
3. 只清理可证明属于旧源且不承载手工内容的节点；含手工子树、组件或引用时保留或输出冲突，不做盲删。
4. 无状态旧 Prefab 使用窄范围兼容策略，例如只识别工具专属输出目录中的失效 Renderer Mesh；不能把所有源中不存在的节点都判为生成物。
5. 对每个 Renderer 记录实际 Mesh，并从当前 Prefab 最终引用生成状态，避免多步骤/多 Prefab 共享集合串入无关 Mesh。
6. 删除旧 `.mesh` 前扫描其他 Prefab、Scene 和当前 Prefab 引用；兼顾 `MeshFilter`、`SkinnedMeshRenderer`、`MeshCollider`。存在保留引用时不删除。
7. 为改名、删除、手工挂点、MeshCollider 引用保护和旧 Prefab 无状态场景添加回归测试；编译通过后仍在 Unity EditMode 检查真实 Prefab 结果。

### 6.4 风险与不适用边界

- 只按新旧路径差集删除会误删挂点或手工子树；只做 additive merge 会留下失效源节点。
- 旧 Prefab 无来源标记时，纯 Transform 节点与手工节点可能无法可靠区分；应记录缺口，不能宣称完整自动清理。
- 依赖扫描遗漏当前 Prefab、`MeshCollider` 或 Scene，可能误删仍在使用的 Mesh。
- 标记保存/重导入时序、多步骤资源归属和 Prefab override 合并需由目标 Unity 测试覆盖；源码编译不能证明宿主行为。

### 6.5 验证与回退

测试 Mesh 改名、删除、重复生成和手工引用四组场景；确认新节点与 Mesh 更新、失效源节点按策略清理、人工内容保持、仍被引用的旧 Mesh 不删除。失败时恢复源 Prefab 与 Mesh 资产备份，并检查来源标记和路径映射。Unity EditMode 与代表性 Prefab 检查通过前，不把修复称为宿主完整验证。

来源：[会话 01a1cab4-df21-72c3-ba9b-5c0157ab2b5a](../../../../VA_Worlk_memory/sessions/01a1cab4-df21-72c3-ba9b-5c0157ab2b5a.md)，消息 2–8、10–17；消息 9 为上下文摘要，未作为事实来源。
<!-- END automation:ta date:2026-11-11 layer:REFERENCE -->
