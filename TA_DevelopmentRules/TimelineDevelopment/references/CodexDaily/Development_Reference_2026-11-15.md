<!-- BEGIN automation:ta date:2026-11-15 layer:REFERENCE -->
# Timeline 材质参数曲线与 SubMesh 叠加绘制参考｜2026-11-15

> 类型：REFERENCE；适用范围：Timeline 驱动 Renderer 材质槽、局部 Shader 参数与追加绘制；使用前提：按目标 Unity/URP 版本、Timeline 生命周期、Shader 属性和角色绑定重新验证。
> 关联既有规则：`TML-DOC-01`、`TML-ARC-02`、`TML-LIF-02`、`TML-VAL-01`；ProjectACG 实现状态见 [Timeline 材质 Clip Profile](../../Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-15.md)。

## 1. 适用场景

- 先替换角色某个 Mesh 材质槽，再对新材质的 Float、Vector 或 Color 属性播放曲线。
- 只想在 Timeline 某个 Renderer/材质槽覆盖参数，不希望改共享材质资源。
- 需要保留原材质并在指定 SubMesh 上额外绘制材质，例如临时发光、叠色或表现层。
- Vector 曲线需要单独控制部分分量，或材质 Inspector 数值与运行时画面看起来不一致。

## 2. 前提与依赖

- 先明确最终作用域是材质资产、Renderer、材质槽还是 Overlay Renderer；每个目标应由一个明确 Mixer 聚合和清理。
- 检查实际 Shader 是否声明并使用该属性、运行时属性类型是否匹配、材质是否启用相关 Feature。ShaderGUI 的范围/标签辅助字段可能不是渲染属性。
- 读取曲线求值语义。若结果为“基础值 × 曲线”，常量 `1` 是中性曲线，不会让基础值随时间变化；Vector 整体共用曲线会同时缩放全部分量。
- 确认现有 Timeline Binding、角色动态重绑、材质槽/SubMesh 对应关系、重叠 Clip 优先级和 Clip 退出/Stop/解绑恢复职责。

## 3. 实现或排查步骤

### 3.1 选择局部参数写入方式

1. 对单 Renderer/材质槽的短时动态参数，评估槽位级 `MaterialPropertyBlock`（MPB），避免 `.material` 实例化或直接写共享材质资产。
2. 应用材质替换后再解析并验证其 Shader 属性，再将参数曲线写入对应槽位；Overlay 模式应写 Overlay Renderer，而不是底层原 Renderer。
3. 每个 Mixer 缓存 MPB、Renderer 与属性 ID，合并目标槽更新；没有参数曲线时跳过参数扫描。不要用缓存实现描述替代 Profiler 结论。
4. 曲线结束、换材质、材质不含属性、目标材质槽为空、外部重绑、Director Stop 或角色 Unbind 时，按即将生效材质的基值恢复此前覆盖，并保留同槽其他系统的 MPB 属性。
5. 材质 Inspector 不显示 MPB 覆盖值是预期的；用实际 Renderer/槽位读取或画面断言确认运行值。

### 3.2 按分量动画 Vector

1. 为兼容已有 Clip，保留原整体曲线模式作为默认；新增独立 X/Y/Z/W 模式时，开启瞬间把旧曲线复制到各分量，避免时间连续性突然改变。
2. 若各分量结果仍乘以基础向量分量，目标只动画 X 时让 X 使用变化曲线，其余分量保持中性曲线 `1`；基础 X 为零时乘法曲线不会产生非零结果。
3. 若需求是直接赋值而非乘数，需单独定义迁移和混合语义，不能将二者都称作“曲线控制”。

### 3.3 选择替换或追加绘制

1. **替换**：改目标 Renderer 的现有材质槽，Clip 退出时恢复原引用。
2. **追加**：不要假定把材质放到 `sharedMaterials` 尾部就能绘制任意槽。Unity 额外槽常会落到最后一个 SubMesh；需要精确绘制任意选中 SubMesh 时，使用独立 Renderer/绘制路径，只提交目标 SubMesh，并让 Timeline 自己拥有该临时对象的生命周期。
3. 确认追加材质 Blend、ZWrite、Queue、Stencil 和专用 Pass；追加绘制表示多一次表面绘制，不等于与底层自动透明混合。
4. 若需覆盖原材质的后续专用 Pass，应在 Frame Debugger 确认队列与 Pass 时序；提高到 Transparent 队列可能覆盖较早的 Opaque/角色 Pass，但会改变与其他透明物体的排序关系。

### 3.4 追查“参数没变”或“目标消失”

1. 先区分材质资产 Inspector 没变、MPB 运行值没变和最终像素没变三种现象。
2. 检查 Clip 当前曲线值、基础值、目标 Renderer/材质槽、当前替换材质和 `HasProperty`/属性类型分支。
3. 将范围元数据属性与真正被 Shader 消费的属性分开；检查属性是否只控制 GUI 滑条范围，而不参与 HLSL 或 ShaderLab 状态。
4. 目标渲染消失时，在 Clip 起点、稳定中段和结束点分别检查源 Renderer/父级 active、Overlay Renderer 状态和 Frame Debugger Draw。截图只能形成假设，不能确认是哪条状态链关闭。

## 4. 风险与不适用边界

- MPB 不修改共享材质，但在启用 SRP Batcher 的管线中，受控 Renderer 会失去该批处理路径；适合少量局部目标，不适合未经测量地扩展到大量持续动画 Renderer。
- 每个被追加的 SubMesh 多一次绘制，透明队列会影响透明排序、深度和与角色专用 Pass 的相互作用；需要目标场景验证。
- 独立 Vector 曲线额外增加每分量求值；若项目实现使用乘数曲线，所有基础分量都应非零或明确其零值语义。
- 属性存在不代表 Shader 实际消费它；反过来，MPB 生效也不会改写材质资产 Inspector。
- 编译通过和隔离测试不能代替生产 Timeline 的动态绑定、实际动画求值和 GPU 性能测量。

## 5. 验证与回退

- 测试替换后属性写入、共享材质不变、多个参数/槽聚合、外部换材质、材质缺属性、空槽、Clip 结束、Stop、解绑及重新绑定。
- Vector 独立模式覆盖旧模式默认兼容、启用时复制旧曲线、X/Y/Z/W 单独求值、中性分量、基础值为零及寻址/反向播放。
- 追加模式覆盖单/多 SubMesh、被选中槽、SkinnedMesh/BlendShape、Renderer 开关、特殊 Pass、透明排序和临时 Renderer 清理。
- 对 MPB 与材质实例方案在目标设备比较 CPU/GPU 帧时间和 SRP Batcher；没有测量时只报告机制和受影响范围，不给绝对帧耗时结论。
- 异常时禁用参数/追加模式并恢复保存的材质数组与 MPB 基值；不要依赖退出 Play Mode 才会发生的隐式清理。

## 来源与证据边界

- [ProjectACG Timeline 会话](../../../../VA_Worlk_memory/sessions/01a1cf40-ee85-79b3-af3a-15bf06a6a14d.md)

以上方法从 ProjectACG 一次实现中归纳，迁移前需重新验证目标 Unity/URP 和角色 Pass。MPB 的目标设备成本没有 Profiler 数据；最新 Vector 曲线的 EditMode 与 Timeline Inspector 手工交互尚未完成。
<!-- END automation:ta date:2026-11-15 layer:REFERENCE -->
