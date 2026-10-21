---
name: timeline-profile-to-clip-import-and-safe-override
description: Unity Timeline 从角色渲染 Profile 导入参数到 Clip，并支持导入后局部二次调整的通用实现与验证参考。
---

# Timeline 预设参数导入与 Clip 二次调整参考

> 分类：REFERENCE；适用范围：Unity Timeline、角色渲染、材质或场景参数的预设导入工作流。本文记录可迁移的实现模式，不把 ProjectACG 的类名、路径、枚举和 Shader 参数升级为 CORE 规则。

## 结论先行

“读取预设配置文件后继续调整”最稳妥的语义是一次性复制，而不是让 Timeline Clip 长期引用 Profile：

```text
Profile（艺术基线）
        │ 用户在 Inspector 主动点击导入
        ▼
Clip（本镜头的可序列化副本） ── 美术二次调整 ──► Mixer / Runtime
        ▲
        └── Undo 可回退；源 Profile 不被修改
```

这种方式适合“先统一角色渲染参数，再针对镜头做少量偏移”的制作流程。未选择 Profile、未点击导入按钮时，Clip 不会自动读取源配置，也不会改变已有 Timeline 的参数。导入完成后，Clip 和 Profile 分离，调整 Clip 不会反向修改 Profile。

这里的“源 Profile 不被修改”指导入 Utility 不把 Clip 的编辑结果写回 Profile。若 Profile 的公开 getter 内部包含旧数据迁移、归一化或 `Ensure*` 延迟初始化，读取 getter 本身可能在内存中补齐 Profile 私有字段；需要绝对只读时，应先建立纯读取快照，或让导入路径避开会写回的 lazy migration，并单独验证 Profile 是否被标记为脏。

这不是实时绑定方案。Profile 后续发生变化时，已经导入的 Clip 不会自动同步；需要再次点击导入，或者另行设计明确的重同步工具。

## 1. 适用场景

- 角色或 Renderer 已有一套可复用的灯光、阴影、描边、Rim、渐变和环境光 Profile。
- Timeline 需要在某个镜头中复用这套基线，并允许针对镜头、动作或构图做二次调整。
- 需要保留 Timeline 片段的可编辑性，不能把所有参数继续隐藏在外部资源引用中。
- 需要支持 Undo、源配置保护和旧 Timeline 资产兼容。

不适合直接套用在以下场景：

- 需要所有镜头永久跟随同一个 Profile 的实时联动。此时应使用显式引用加版本/刷新机制，并定义冲突优先级。
- 参数的真正所有者是共享材质、相机 RenderFeature 或场景 Volume，且无法通过 Clip 的权重和协调器隔离。此时必须先设计共享状态仲裁。
- Profile 与 Clip 的序列化字段完全不一致，且没有明确的映射、默认值和版本策略。

## 2. 前提与依赖

### 2.1 先冻结数据契约

实现前先列出每个参数的以下信息：

| 信息 | 必须回答的问题 |
| --- | --- |
| 源 | Profile 的哪个公开属性是真实来源？是否经过迁移、归一化或 Clamp？ |
| 目标 | Clip 哪个序列化字段接收值？该字段是否已有旧资产？ |
| 覆盖开关 | 复制后是否应打开 `override` 标志？关闭时由谁提供基线？ |
| 作用域 | 绑定角色、单个 Renderer、材质槽、相机还是场景共享状态？ |
| 混合 | 连续值求加权平均，还是由最高权重 Clip 决定？ |
| 恢复 | Clip 结束、权重归零或 Graph 停止后恢复谁的值？ |
| 兼容 | 旧 YAML 缺少字段时使用什么默认值？新增字段是否改变旧语义？ |

不要只根据 Inspector 中“看得到的字段”判断功能已经完成。必须追到 Mixer、MaterialPropertyBlock、Controller、Shader 或 RenderFeature 的最终消费者。

### 2.2 推荐的职责边界

| 组件 | 职责 | 不应承担的职责 |
| --- | --- | --- |
| Profile / ScriptableObject | 保存可复用的艺术基线和资产级参数 | 在 Timeline 播放期间被隐式修改 |
| Clip / PlayableAsset | 保存本镜头的序列化输入和显式覆盖开关 | 运行时直接修改材质或全局状态 |
| Editor 导入 Utility | 在用户点击后执行字段映射、Undo 和脏标记 | 参与 Player 运行时逻辑 |
| Mixer | 按权重聚合 Clip，写入正确的 Renderer/协调器 | 重新读取 Profile，或猜测缺失参数的语义 |
| 协调器 / Controller | 处理相机、场景和跨角色共享状态的优先级 | 被单个角色 Mixer 直接清空或长期占用 |

### 2.3 资源和版本边界

- Editor-only 导入代码放在 Editor 程序集或 `#if UNITY_EDITOR` 块中，不能把 `UnityEditor` 带入运行时程序集。
- 可独立显示的 `PlayableAsset` Clip 继续放在独立 `.cs` 中，保持类型名、字段名、`ClipCaps`、`CreatePlayable` 和旧资产的 `m_Script` 引用稳定。
- 新增序列化字段优先使用向后兼容的默认值；改变既有字段含义、范围或绑定类型时，应视为迁移任务，而不是普通补字段。
- 任何 `.md` 文档资产若处于 Unity 管理目录，需同步维护 `.meta`，避免 GUID 和索引失配。

## 3. 实现步骤

### 3.1 设计“复制后独立”的数据流

导入按钮只做一次明确动作，数据流应满足：

1. 用户在 Clip Inspector 选择一个 Profile。
2. 用户点击“导入并覆盖当前 Clip 参数”。
3. Utility 读取 Profile 的只读属性，按映射表写入 Clip。
4. 记录 Clip 的 Undo，并标记 Clip 脏。
5. 清空临时 Profile 选择，刷新 Inspector/SceneView。
6. 用户继续直接编辑 Clip 字段，Mixer 只消费 Clip。

不要在 `OnInspectorGUI` 每帧把 Profile 值写入 Clip，也不要把 Profile 引用保存到 Clip 作为隐式运行时依赖。否则用户刚做的二次调整会在下一次 Inspector 重绘时被覆盖。

### 3.2 Inspector 入口应放在最上方

配置导入属于“初始化当前 Clip”的操作，入口应放在 `serializedObject.Update()` 之后、普通说明和参数折叠组之前。这样用户打开 Clip 就能先完成基线导入，也能清楚看到这是一次覆盖操作。

推荐结构：

```csharp
public override void OnInspectorGUI()
{
    serializedObject.Update();
    DrawProfileImport();
    DrawHelpBoxAndSections();
    serializedObject.ApplyModifiedProperties();
}
```

`DrawProfileImport` 的关键行为：

- `ObjectField` 类型限制为目标 Profile 类型；
- Profile 为空时禁用导入按钮；
- 按钮文案明确写出“导入并覆盖当前 Clip 参数”；
- 导入后清空临时选择，避免重复点击时误认为仍在保持绑定；
- 调用 `serializedObject.UpdateIfRequiredOrScript()` 和 `SceneView.RepaintAll()`，让参数面板和场景预览立即反映结果；
- HelpBox 明确提示“参数复制到 Clip，之后调整 Clip 不会修改源配置”。

### 3.3 使用显式字段映射，不做无约束整对象复制

Profile 和 Clip 通常不是同一个类型，不能假设 `EditorJsonUtility.FromJsonOverwrite` 或反射复制能正确处理：

- 私有字段与公开字段的命名差异；
- 归一化方向、Clamp 范围和迁移兼容属性；
- Profile 中不存在的 Timeline 专用开关；
- Timeline 的 `override`、权重和作用域字段；
- 一个 Profile 字段需要映射到多个 Clip 字段的情况。

推荐在独立的 Editor Utility 中逐字段写入，并把每一组映射按艺术语义组织：

```csharp
#if UNITY_EDITOR
internal static class TimelineProfileImportUtility
{
    public static bool ImportProfile(MyTimelineClip clip, MyRenderProfile profile)
    {
        if (clip == null || profile == null)
        {
            return false;
        }

        Undo.RecordObject(clip, "从 Render Profile 导入 Timeline Clip 参数");
        clip.overrideMainLight = true;
        clip.mainLightMode = profile.MainLightMode;
        clip.mainLightDirection = profile.GetMainLightDirection();
        clip.mainLightColor = profile.MainLightColor;
        clip.mainLightStrength = profile.MainLightStrength;
        // 继续写入虚拟光、阴影、描边、环境光等分组。
        EditorUtility.SetDirty(clip);
        return true;
    }
}
#endif
```

真实代码中不要保留上例的注释式占位字段；每一个实际支持的参数都应在映射表和测试中出现。

### 3.4 处理 Profile 属性和 Clip 字段的不对称

常见的不对称需要在映射表中写明决策：

| 情况 | 处理建议 |
| --- | --- |
| Profile 的方向经过归一化 | 导入归一化结果，不把未归一化编辑缓存写入 Clip；零方向要有明确回退语义。 |
| Profile 的强度范围比 Clip 旧范围更大 | 同步更新 Inspector `Range` 和 Mixer Clamp，兼容旧 YAML 的旧值。 |
| Profile 只有“跟随主光”开关，Clip 还有方向模式枚举 | 将开关映射为明确的 Clip 枚举值，并记录 `Off` 时的行为。 |
| Profile 没有 Timeline 专用开关 | 不要凭感觉生成开关；使用确定的产品决策或新增 Profile 字段。 |
| Clip 有 `override` 标志而 Profile 没有 | 导入是完整快照时显式打开对应 `override`；部分导入时只打开被选中的分组。 |
| 一个值属于相机/场景共享状态 | Clip 只登记请求，最终由协调器按优先级仲裁，不要把它伪装成角色局部状态。 |

### 3.5 明确“导入覆盖范围”和“二次调整”语义

一次完整导入通常会覆盖：主光、虚拟补光、额外点光、平面阴影、场景阴影、高质量阴影强度、描边、Rim、最终颜色渐变和环境光，并打开对应的 `override` 开关。

这会带来一个必须主动确认的产品语义：如果 Profile 的主光模式是 `Off`，是否应该让 Timeline 显式关闭场景主光，还是保留场景 Controller 的主光？两种语义都合理，但必须二选一并在 UI 提示、测试和回退说明中保持一致：

- **Clip 完整拥有光照状态**：`overrideMainLight = true`，`mainLightMode = Off`，防止场景主光泄漏进镜头。
- **Clip 只覆盖开启的光照**：`overrideMainLight = mainLightMode != Off`，`Off` 时让场景基线继续生效。

导入后用户的二次调整只修改当前 Clip。若想只导入部分模块，应在按钮旁提供分组选择，或者提供“复制到 Clip 后保留未选分组”的明确策略，不能靠用户猜测当前按钮是否会覆盖全部字段。

### 3.6 新增运行时能力时采用“默认关闭 + 显式分支”

例如给旧 Clip 增加“虚拟光跟随相机”能力时：

```csharp
public bool virtualLightFollowCamera;
public Vector3 virtualLightCameraOffsetEuler;
public bool virtualLightMaskFollowCamera;
```

运行时只在开关为 `true` 且相机存在时走新路径，否则保留旧的序列化方向：

```csharp
private static Vector3 ResolveVirtualLightDirection(MyTimelineClip clip)
{
    return clip.virtualLightFollowCamera && Camera.main != null
        ? Camera.main.transform.rotation
            * Quaternion.Euler(clip.virtualLightCameraOffsetEuler)
            * Vector3.back
        : clip.virtualLightCameraDirection;
}
```

这保证旧 `.playable` 没有新字段时使用 `false` 默认值，旧方向仍被消费。没有 `Camera.main` 时也必须有确定回退，不能产生零方向或异常。

### 3.7 Mixer 必须保持原有混合合同

- 每帧先 `Reset` 聚合状态，再遍历有效输入，避免 Clip 结束后沿用上一帧的值。
- 连续标量、颜色和普通向量按权重求和并归一化；方向先加权再归一化，零向量有明确回退。
- Bool、枚举、模式和互斥开关由最高权重有效 Clip 决定；同权重使用稳定顺序。
- 权重为零时恢复材质或 Controller 当前基线；不要用未初始化的零覆盖基线。
- 角色局部状态可写绑定 Renderer 的 MPB；相机/场景共享状态必须走 Timeline 专属协调器。
- 清理时只清除当前 Mixer 所拥有的 Timeline 覆盖，不清空其他系统的 MPB 或全局状态。

## 4. 常见问题、规则与解决方式

| 问题或误区 | 根因 | 解决方式 |
| --- | --- | --- |
| Profile 一改，所有已做镜头自动变化 | 把 Profile 作为 Clip 的实时引用，或 Inspector 每帧同步 | 默认采用一次性复制；需要同步时另做版本化重同步按钮。 |
| 导入后调整 Clip，源配置也被改了 | 直接写 Profile，或保存了可写引用 | Utility 只读 Profile 属性，只写 Clip；配合 `Undo.RecordObject(clip)`。 |
| 导入按钮每次刷新都覆盖手调值 | 导入逻辑放在 `OnInspectorGUI` 的普通绘制路径 | 只有按钮点击才调用导入；临时选择不等于运行时绑定。 |
| 导入后 Inspector 看不到新值 | 忘记更新序列化对象或场景预览 | 导入后调用 `UpdateIfRequiredOrScript`，必要时 `SceneView.RepaintAll`。 |
| `MainLight Off` 仍看到场景主光 | `overrideMainLight` 没有打开，场景 Controller 继续写值 | 选择“Clip 完整拥有光照状态”语义时，显式打开 override；否则在文档中说明场景基线会保留。 |
| 只有 RenderFeature 入队但画面没有阴影 | 生产者启用，消费者接收强度、Mask 或材质开关仍是零 | 同时验证生产者、接收参数、最终 Shader Pass 和更高优先级覆盖。 |
| 角色之间互相串阴影或环境强度 | 把相机/场景共享状态当作角色局部 MPB | 使用共享请求协调器；角色局部值和共享值分开写入。 |
| 旧 Timeline 播放方向改变 | 新字段默认值不为旧语义，或旧分支被无条件替换 | 新 bool 默认 `false`；运行时 `true` 才走新路径；保留旧方向字段。 |
| 没有主相机时虚拟光失效或出现异常 | follow 分支没有空相机回退 | `Camera.main == null` 时返回原 Clip 方向，并在 Unity 中验证。 |
| Profile 新增字段后导入漏值 | Utility 没有映射表或覆盖测试 | Profile/Clip 字段表、导入测试和代码审查清单同步更新。 |
| Profile 没有遮罩跟随开关但 Clip 被强制打开 | 项目实现把“跟随相机”当成隐式默认 | 先确认艺术需求；通用实现应新增源字段或默认关闭，避免导入产生隐藏行为。 |
| 把所有 Profile 字段机械复制到 Clip | 两者作用域和生命周期不同 | 只复制 Timeline 真正需要覆盖的参数，材质资源和共享状态仍由原所有者管理。 |
| 用 `RemoveAllListeners` 或全局清理解决状态恢复 | 清理边界不清，会误伤其他 owner | 按 owner 精确清理；Timeline 状态使用权重归零和幂等释放。 |

## 5. 验证与回退

### 5.1 静态检查

- 编译 AOT/Runtime 和 Editor 程序集，确认新 Clip、Utility、`.meta` 进入正确程序集。
- 搜索新增字段是否同时出现在 Clip、Editor、Mixer、最终消费者和导入映射中。
- 确认运行时代码没有 `UnityEditor`，没有 `dynamic`，新增日志使用项目约定的日志入口。
- 执行 `git diff --check`，检查文档和代码没有意外乱码、尾随空格或大范围无关重排。
- 执行项目 harness 和 Unity 资产一致性检查；检查 `.meta` GUID 稳定且未产生无关生成物。

### 5.2 EditMode / 单元测试矩阵

至少覆盖：

| 测试 | 目标 |
| --- | --- |
| Profile `Off / Custom / FollowCamera` | 主光模式、方向、背光和 follow 偏移映射正确。 |
| 平面阴影和高质量阴影跟随主光 | Profile 开关到 Clip 模式/标记的转换正确。 |
| 自阴影、环境阴影 `0..2` | 强度范围、序列化和 Mixer Clamp 一致。 |
| Profile 与 Clip 独立 | 导入后修改 Clip，Profile 字段不改变；修改 Profile 也不自动改变 Clip。 |
| 旧 JSON / 旧 YAML 缺少 follow 字段 | 新字段使用默认 `false`，旧方向仍保持。 |
| 空 Clip、空 Profile | Utility 返回失败，不写入、不产生异常。 |

### 5.3 Unity 行为验证

静态测试不能替代 Unity 播放验证，至少执行以下 A/B：

1. 打开一个旧 Timeline，不选择 Profile，记录主光、虚拟光、阴影、Rim 和环境光效果。
2. 重新打开同一 Timeline，确认未点击导入前效果和参数不变。
3. 选择 Profile 并点击导入，确认 Clip 字段被复制，源 Profile 未变。
4. 修改 Clip 的强度、方向、颜色和开关，确认只改变当前 Clip，并可通过 Undo 恢复。
5. 测试 Profile 主光为 `Off`、`Custom`、`FollowCamera`，以及无 `Camera.main` 时的回退。
6. 测试多个重叠 Clip、不同权重、Seek、Pause、Stop、Graph 销毁和重新播放。
7. 测试多个角色和多个相机，确认局部 MPB 与相机/场景共享状态没有串值。
8. 用 Frame Debugger/Profiler 确认最终 Pass 消费到正确的接收强度、方向、Mask 和材质状态。

### 5.4 回退策略

- 单次误导入：使用 Clip 的 Undo；源 Profile 不需要回退。
- 导入映射错误：修正 Utility 后重新导入，或从版本控制恢复 Clip 资产；不要手改共享材质来“抵消”错误。
- 新字段导致旧资产行为变化：先关闭新开关或恢复旧方向分支，再检查序列化默认值和字段含义是否被改写。
- 共享状态残留：停止 Director、销毁 Graph 或取消绑定时清除 Timeline owner 请求，确保恢复逻辑幂等。
- 迁移到其他项目：只带走复制、Undo、显式映射、兼容分支和验证方法；删除 ProjectACG 的类名、路径、Shader 和枚举事实后重新建立 Profile。

## 6. 可直接复用的设计清单

### 开发前

- [ ] 已确认 Profile、Clip、Mixer 和最终消费者。
- [ ] 已区分角色局部、材质资产、相机共享和场景共享状态。
- [ ] 已建立字段映射表、范围、默认值、覆盖开关和恢复语义。
- [ ] 已决定 `Off` 是显式关闭还是保留场景基线。

### 编辑器

- [ ] 导入入口在 `serializedObject.Update()` 后置顶。
- [ ] ObjectField 只接受正确的 Profile 类型。
- [ ] 导入按钮为空时禁用，文案明确“覆盖当前 Clip”。
- [ ] 导入使用 Undo、SetDirty、序列化刷新和预览刷新。
- [ ] 导入后没有保存 Profile 引用，也没有每帧自动同步。

### 运行时

- [ ] 新字段默认值保持旧资产行为。
- [ ] 新功能只有显式开关开启时才进入新分支。
- [ ] 相机缺失、绑定丢失、权重归零和 Graph 停止都有回退。
- [ ] 连续参数、离散参数、方向和共享状态分别采用正确混合策略。
- [ ] 清理只影响当前 Timeline owner。

### 交付

- [ ] 代码编译、EditMode 测试和资产检查通过。
- [ ] 旧 Timeline 未导入前后效果一致。
- [ ] 导入、二次调整、Undo 和 Profile 独立性已在 Unity 中验证。
- [ ] 重叠 Clip、多角色、多相机、Seek/Stop 和最终渲染 Pass 已验证。
- [ ] 交付说明区分“静态验证通过”和“Unity 视觉验证通过”。

