# Timeline 通用开发模式与检查清单（REFERENCE）

> 分类：跨项目通用 REFERENCE，不是项目 Profile。

本文件补充 [../README_Tech_TimelineDevelopmentRules.md](../README_Tech_TimelineDevelopmentRules.md) 的 CORE，不定义新的强制规则 ID。它只保留可迁移的决策模式、伪代码和检查方法；项目名称、固定路径、具体 Track/Controller、模式枚举、Shader 属性和 RendererFeature 实现必须放入对应 `Profiles/<Project>/`。实现前仍须读取当前项目 Profile、目标工程的 Track、RendererFeature、Shader、Timeline 资产与程序集。

## 1. Track 结构决策

| 需求 | 推荐结构 | 不推荐 |
| --- | --- | --- |
| 只有一种 Clip、一个参数域 | 单一 `TrackAsset` + 单一 Mixer。 | 为未来假设预先创建多层空结构。 |
| 材质参数和角色渲染参数需要并排编辑 | 一个根 `ILayerable` Track；Material Layer 与 Render Layer 使用各自 Mixer。 | 将两种 Clip 放进同一 Mixer 后按输入顺序分别写。 |
| 同类型 Clip 在同一时间交叠 | 同 Layer 内按参数类型混合。 | Clip 间完全不混合，或跨 Layer 盲目求平均。 |
| Layer 可能写同一个属性 | 将写入集中到单一 Mixer/协调器，或编辑器报错阻止该组合。 | 依赖 Layer 评估顺序，产生非确定性结果。 |
| 只有某些字段需要动画 | 优先生成/复用针对字段的 Track，或暴露真正可序列化的 Clip 字段。 | 仅把 Profile 引用放到 Timeline，期待单个 Profile 内字段可直接打关键帧。 |

## 2. Mixer 状态机

建议把 Mixer 的职责显式拆为 `Input -> BlendState -> Effect -> Restore`：

1. **Input**：读取有效 Playable 输入与权重，跳过无效或零权重输入。
2. **BlendState**：只保存当前帧可回放的聚合值、最佳离散输入和来源权重。
3. **Effect**：将聚合值一次性写到 Renderer、材质槽、协调器或其它正确的所有者。
4. **Restore**：无输入、绑定为空、图停止或销毁时只清除该 Mixer 所拥有的覆盖，不能清空其它 Mixer 的请求。

推荐的混合伪代码：

```csharp
foreach (var input in activeInputs)
{
    if (input.clip.overridesFloat)
        floatSum += input.clip.value * input.weight;

    if (input.clip.overridesMode && input.weight > bestModeWeight)
        bestMode = input.clip.mode;
}

float resolvedFloat = totalWeight > epsilon ? floatSum / totalWeight : baseline;
```

方向应使用加权向量后归一化；离散模式不做数学平均。若两个离散 Clip 完全同权重，必须以 Track/Clip 的稳定序号或首次登记顺序决胜，不能依赖 Dictionary 遍历或浮点噪声。

## 3. 材质参数与保持值

一个材质属性的真正键至少是：`Renderer instance + material slot + Shader property ID`。只以属性 ID 缓存会让不同 Renderer 或多个材质槽互相污染。

对“后一个 Clip 没有该属性”的合同有三种合法选择：

- **保持值**：适合镜头分段中希望参数持续到后续显式覆盖的情形；需要缓存源值、最后值和恢复时机。
- **恢复基线**：适合局部临时表现；Clip 结束时恢复 Material/Controller 的源状态。
- **显式默认**：仅在产品明确要求每个 Clip 都可独立播放时使用。

三者都不能靠 `MaterialPropertyBlock` 缺字段的偶然行为实现。Timeline 不应实例化材质，也不应在播放过程中修改 `sharedMaterials`。

## 4. 相机级渲染状态模式

屏幕空间阴影、相机深度资源、全局方向光替代、全局雾或 RendererFeature 参数通常是相机级状态。它们无法通过 `MaterialPropertyBlock` 被隔离为每角色不同的资源。

推荐流程：

1. Clip 只声明“是否覆盖”和数据；Mixer 先在自身 Layer 内选择有效请求。
2. Timeline 专属协调器按 Mixer/Director 存储请求，不直接覆盖默认系统正在写入的全局键。
3. 统一写 Timeline 专属的全局值或服务状态；RenderFeature 在原有 Volume/默认 Controller 解析链中读取它。
4. 优先级写在同一个解析函数中。例如：`显式 Volume > Timeline 请求 > 默认场景 Controller > Feature 默认值`。
5. 无有效请求时将 Timeline 专属权重清零；默认系统不需要知道 Timeline 曾经存在。

### 4.1 功能入队不等于最终效果可见

RendererFeature、Pass 或计算任务已经启用，只能证明生产链可能执行，不能证明最终材质或合成阶段会显示效果。交付前至少逐层确认：

1. 启用请求已经进入正确相机和正确帧；
2. 生产阶段确实生成有效纹理、缓冲或全局状态；
3. 消费材质/Shader 的接收强度、权重、Mask 和 Keyword 不是关闭状态；
4. 最终 Pass 实际采样该结果，且没有被更高优先级的 Volume、Controller 或 Profile 覆盖；
5. 默认系统关闭、默认接收强度为零以及多个请求并存时，仍按声明的合同恢复或仲裁。

若同一 Clip 语义同时要求“启用生产者”和“让消费者可见”，两者应由同一个显式模块共同登记，不能要求用户再发现一个隐蔽前置开关。具体生产者、消费者、属性名和优先级放入当前项目 PROFILE。

## 5. Inspector 与 Scene Handle 模式

- 自定义 Inspector 将字段按艺术语义分组；开关与值紧邻，避免存在“可编辑但未覆盖”的隐蔽字段。
- 方向字段可同时提供 Vector3、Orbit/Elevation 和 Scene Handle，但三者必须写同一序列化字段。
- Orbit/Elevation 圆盘的 `BeginChangeCheck`/`EndChangeCheck` 必须成对闭合；圆盘拖拽和下方数值框分别检测变化，再统一写回方向字段。若只结束数值框的变化检查，会出现圆盘手柄看似可拖但值不保存的假交互。
- 自绘圆盘在左键按下时获取 `GUIUtility.hotControl`，拖拽期间即使指针离开圆盘也继续更新，抬起时释放；右键重置必须消费事件并触发 `GUI.changed`。
- Scene Handle 使用当前被 Timeline 根 Track 绑定的对象作为锚点；子 Layer 中的 Clip 也必须能回溯到根绑定。
- 编辑 Handle 后应记录 Undo、`ApplyModifiedProperties`、`SetDirty`，必要时调用 Director Evaluate 和 SceneView Repaint；关闭 Inspector 时注销回调并关闭临时 Handle。
- HelpBox 必须说明相机级限制、优先级和不能做到的角色隔离，不把限制藏在运行时日志中。

## 6. Clip 资产识别、正常外观与迁移案例

### 6.1 症状与根因

Timeline 中 Clip 的黄色感叹号和黄色标题通常不是配色问题。Timeline 的默认 `ClipEditor` 会在 `PlayableAsset` 无法解析对应 `MonoScript` 时填充错误文本，绘制器再据此显示告警。不要仅在自定义 Editor 中清空错误文本；那会让资产仍不可追踪，却失去唯一的可见诊断。

一个常见来源是把 `TrackAsset` 与继承 `PlayableAsset` 的 Clip 定义放在同一 `.cs`。即使运行时类型可实例化，Unity 仍可能不能把该 Clip 作为独立 ScriptableObject 正确映射到脚本资产，导致 Timeline 告警。

### 6.2 正确修复顺序

1. 将 Clip 移到独立 `.cs`，保持类型名、序列化字段、默认值、`ClipCaps` 和 `CreatePlayable` 契约不变。
2. 为新脚本生成并确认 `.meta`；迁移已有 `.playable` 时，仅将目标 Clip 子资产的 `m_Script` 更新到新 GUID，并保留其它 Clip、轨道和用户脏改。
3. 使用 `ClipEditor` 只定义正常状态的标题、Tooltip 和高亮色；从 `base.GetClipOptions` 保留未来真实错误。
4. 对新 Clip 用 `OnCreate` 设定友好的显示名；对已有 Clip 单独迁移 `m_DisplayName`，不要误以为 `OnCreate` 会回填旧资产。
5. 等 Unity 导入完成后，在 Timeline 窗口确认无黄色感叹号、Inspector 可编辑、播放行为不变；若仍告警，先检查 Console 与子资产 `m_Script` GUID。
## 7. 交付前检查清单

- [ ] 每种 Track/Clip/Behaviour 均有单一序列化职责，旧字段默认行为未改变。
- [ ] 每个可独立显示的 `PlayableAsset` Clip 均有独立脚本与有效 `MonoScript`；旧 `.playable` 的脚本引用和显示名已迁移并在 Unity 中确认。
- [ ] 自定义 `ClipEditor` 未隐藏 `errorText`；正常视觉样式与真实诊断状态均已验证。
- [ ] Layer 内混合、Layer 间独立性和冲突写入策略已写明并在 Unity 中验证。
- [ ] 根 Track 与子 Layer 的绑定、Clip 创建、Inspector、Scene Handle 和 Undo 可用。
- [ ] Renderer/材质槽级写入不实例化材质、不修改共享资源，且无多 Renderer 串值。
- [ ] 相机级状态有 Timeline 专属协调器；多个 Timeline、Clip 结束和图停止不会留下脏全局状态。
- [ ] 相机级功能同时验证生产者已执行、消费者接收强度/权重有效；不能只以 RenderFeature 或 Pass 已入队作为效果可见证据。
- [ ] Volume/场景 Controller/Feature 默认值的优先级已通过实际播放确认。
- [ ] 自绘旋钮/方向输入的左键拖拽、重置、数值输入与 Undo 均真实写回同一 Clip 字段。
- [ ] 已完成 C# 编译、Unity 导入/播放、重叠 Clip、Seek/Stop、多角色/相机与必要的 Frame Debugger/Profiler 验证。
- [ ] 最终记录明确区分“已编译”与“已在 Unity 播放/视觉验证”，并列出未验证边界。
