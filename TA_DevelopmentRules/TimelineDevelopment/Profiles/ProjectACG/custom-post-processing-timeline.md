---
name: projectacg-custom-post-processing-timeline-profile
description: ProjectACG 当前 Custom Post Processing Timeline 与 URP RendererFeature 的实现事实、问题记录、验证边界和维护规则。
---

# ProjectACG Custom Post Processing Timeline Profile

> 类型：ProjectACG `PROFILE`。本文记录当前工程的具体版本、路径、字段和验证结果，不是跨项目 CORE。
>
> 初始记录：2026-09-19；本次经验补充：2026-10-20。
>
> 阅读方式：通用架构见 [自定义后处理参考](../../references/timeline-custom-post-processing-renderfeature.md)；参数面板经验见 [按需 Inspector 参考](../../../ToolDevelopment/references/progressive-inspector-and-searchable-property-picker.md)；曲线、数值与开关实现见 [材质属性控制器参考](../../references/timeline-material-property-controller.md)。本文保留项目事实、问题归因和证据，不复制源码旁的功能使用手册。
>
> 验证边界：第 8 节为早期记录；第 10～14 节补充本轮面板与控制器开发。已完成相关 C# 子集编译，未执行 Unity Test Runner、Inspector 视觉验收和目标设备验收。
>
> 整体显隐补充快照：2026-10-20（第 9 节；源码核对与待验边界单独记录）
>
> 迁移提示：其他项目只能参考本文的实现思路；路径、类型名、Shader 属性、Renderer Data、Timeline 版本和 Pass 时序必须重新确认。

## 1. 当前工程事实

| 项目 | 当前值 |
| --- | --- |
| Unity | `2022.3.62f3` |
| URP | `14.0.12` |
| Timeline | `1.7.7` |
| Track 菜单名 | `Custom Post Processing Track` |
| Inspector/Tooltip | 中文 |
| Camera 绑定 | 不绑定具体 Camera，使用当前 Renderer 的 `cameraColorTargetHandle` |
| RenderFeature 目录 | `Assets/Shader/RenderFeature/CustomPostProcessing/` |
| Timeline 目录 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/` |
| Editor 目录 | `Assets/GameScripts/Editor/TimelineTrack/` |

## 2. 当前源码落点

| 文件 | 职责 |
| --- | --- |
| `Assets/GameScripts/AOT/GameArt/TimelineTrack/CustomPostProcessingTrack.cs` | `TrackAsset`、Mixer、运行时材质实例和效果请求发布 |
| `Assets/GameScripts/AOT/GameArt/TimelineTrack/CustomPostProcessingClip.cs` | Clip 数据、效果材质、属性曲线、排除对象、普通材质条目 |
| `Assets/GameScripts/AOT/GameArt/TimelineTrack/CustomPostProcessingRuntime.cs` | Director owner 注册、效果/事件/排除对象合并与缓存 |
| `Assets/GameScripts/Editor/TimelineTrack/CustomPostProcessingTimelineEditor.cs` | 中文 Inspector、Inspector 修改后的 Timeline 刷新分类 |
| `Assets/GameScripts/AOT/GameArt/TimelineTrack/CustomPostProcessingShaderToggle.cs` | Editor/Runtime 共用的开关元数据与已声明 Keyword 写入 |
| `Assets/GameScripts/Editor/TimelineTrack/CustomPostProcessingMaterialPreviewRefresh.cs` | 当前预览所用源材质修改后，请求停帧重新求值 |
| `Assets/GameScripts/Tests/EditMode/GameLogic/CustomPostProcessingTimelinePauseTests.cs` | 真实 Director 的暂停、Seek、owner 清理和 Toggle 回归用例；当前仅编译，未运行 |
| `Assets/GameScripts/AOT/GameArt/TimelineTrack/README_CustomPostProcessingTimeline.md` | 具体使用方式、参数含义、Shader/Mask 功能和历史验证；功能说明的唯一维护位置 |
| `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingRendererFeature.cs` | URP Feature、Pass、RTHandle、Mask、普通材质重绘和 Composite |
| `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingMask.shader` | 排除 Mask 和普通材质替换 Mask |
| `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingComposite.shader` | 原画面、效果画面、排除 Mask、替换 RT 的最终合成 |
| `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingColorExample.shader` | 示例颜色调整后处理 Shader |
| `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingDistortedOutline.shader` | 本轮能力、形状分支和 Toggle/Keyword 核对用的效果 Shader |
| `Assets/GameScripts/Editor/TimelineTrack/DistortedOutlineShaderGUI.cs` | 对照内置 Drawer、条件参数和材质编辑语义 |

实际项目中如果新增文件，应同时检查 `.meta`、程序集编译范围和 Renderer Data 是否引用了正确资源。

## 3. 当前实现合同

### 3.1 Timeline 数据和运行时效果

当前 Clip 支持：

- 多个后处理效果；
- `RenderPassEvent`；
- Float、Vector、Color 属性曲线；
- 权重混合；
- 排除对象列表；
- 普通材质替换列表；
- `All` / `ActiveScene` 场景范围；
- 可选的普通材质模型名筛选。

每个有效效果在 Mixer 中转换为运行时请求。请求按稳定的 Track 输入顺序排序，RendererFeature 按 RenderPassEvent 分发到对应 Pass。

### 3.2 排除对象字段

排除对象不是直接的 `GameObject` 字段，而是：

```csharp
public ExposedReference<GameObject> target;
```

运行时由当前 PlayableGraph 的 resolver 解析。这样 Timeline 资产可以从 Hierarchy 选择对象，同时保持 Director 级绑定关系。

### 3.3 普通材质字段

普通材质条目要求：

```text
modelName + material
```

`replacementMaterialModelName` 为空时，当前实现执行列表中全部有效条目；填写时只执行匹配模型名的条目。模型匹配自身和父层级，并忽略 `(Clone)` 后缀。

这类替换只写入临时 RT，不修改模型 `sharedMaterials`，因此 Clip 结束时不需要逐 Renderer 恢复材质。

## 4. URP 接入事实

### 4.1 当前相机目标

Feature 在 `SetupRenderPasses` 中绑定：

```csharp
renderer.cameraColorTargetHandle
```

不要在 Timeline Clip、Mixer 或 Feature 字段中保存用户指定 Camera。Scene、Game 和其他相机都必须使用它们自己的当前颜色目标。

### 4.2 Pass 输入和临时 RT

Pass 请求：

```csharp
ConfigureInput(
    ScriptableRenderPassInput.Color |
    ScriptableRenderPassInput.Depth);
```

临时资源使用 `RTHandle`。效果/原画面 RT 使用相机描述符；Mask 使用 `R8_UNorm`、非 sRGB、点过滤；相机允许动态分辨率时设置 `useDynamicScale`。

排除 Mask Shader 读取 `_CameraDepthTexture`，不直接绑定 MSAA 相机深度附件。这个约束来自 Scene/Game、MSAA 和单采样 Mask 之间的兼容性问题。

## 5. 已解决的问题和根因

| 现象 | 根因 | 当前处理 |
| --- | --- | --- |
| 编辑器调颜色时 Game 画面跳动/闪烁 | 数值修改调用 `ContentsModified`，Timeline 重建 PlayableGraph | 数值字段使用 `SceneNeedsUpdate`；结构字段才重建图 |
| Scene 有变化，Game 没变化 | 使用了错误/过期的相机颜色目标，或当前 Renderer Data 未添加 Feature | 在 `SetupRenderPasses` 绑定当前 `cameraColorTargetHandle`，检查 Renderer Data |
| `Destroy may not be called from edit mode` | 编辑模式销毁运行时材质使用了 `Destroy` | 编辑模式使用 `DestroyImmediate`，播放模式使用 `Destroy` |
| 排除对象无法从 Hierarchy 选择 | Timeline 资产不能稳定保存直接 Scene `GameObject` | 改为 `ExposedReference<GameObject>` 并由 Director resolver 解析 |
| 排除对象仍受后处理 | Timeline 中有重叠 Clip；另一个 Clip 继续处理对象 | 当前帧合并所有有效 Clip 的排除对象 |
| 排除 Mask 在部分相机/MSAA 下失效 | Mask 直接使用相机深度附件 | 改采样 `_CameraDepthTexture`，Mask 单独使用 R8 RT |
| 普通材质没有效果 | 强制要求重复模型名、根节点匹配过窄、条目被筛选或未找到 Renderer | 空筛选时执行全部条目；匹配父层级和 `(Clone)`；输出明确警告 |
| 新增 Renderer 没被替换 | 每帧扫描方式不稳定且成本高 | 共享 Renderer 缓存、Hierarchy 变化刷新、未命中时兜底刷新 |

## 6. 当前性能实现

当前重点优化已经落在 `CustomPostProcessingRendererFeature.cs` 和 `CustomPostProcessingRuntime.cs`：

1. 同一个 RenderPass 的排除 Mask 只绘制一次。
2. Runtime Registry 缓存效果、RenderPassEvent 和排除对象，避免每个相机创建 HashSet。
3. 场景 Renderer 缓存由所有 Pass 和相机共享。
4. 场景加载/卸载、Hierarchy 变化时刷新 Renderer 缓存。
5. 动态对象未命中时允许低频强制刷新，避免只靠固定时间间隔导致漏对象。
6. `GetComponentsInChildren`、`GetSharedMaterials`、匹配结果使用复用 List。
7. 没有 Mask/普通材质替换时跳过 `_originalHandle` 的无必要全屏拷贝。

这些优化减少了 CPU 扫描、临时分配和 DrawCall 重复，但没有在未验证的情况下承诺固定毫秒收益。最终收益必须用目标设备、目标分辨率和目标场景的 Unity Profiler 确认。

## 7. 维护规则

### 7.1 新增效果 Shader

必须检查：

- Shader 已导入且材质引用有效；
- 存在 Pass 0；
- C# 属性名和 Shader 属性名一致；
- Float/Range、Vector、Color 类型一致；
- 权重语义明确，是由 Shader 处理还是由 Composite 处理；
- 空材质、缺失属性、缺失 Pass 都有可见诊断，不静默假装成功。

### 7.2 修改排除对象

必须检查：

- Inspector 仍显示 Hierarchy 对象选择；
- 当前 Director 能解析 `ExposedReference`；
- 对象及子节点包含 Renderer；
- 当前相机 Culling Mask 包含对象 Layer；
- 重叠 Clip、多个 RenderPassEvent 和多相机行为符合预期；
- SceneScope 合并没有扩大到不应排除的场景。

### 7.3 修改普通材质

必须检查：

- 模型名是否为用户可见且稳定的命名合同；
- 子 Renderer、父节点和 `(Clone)` 规则是否仍有效；
- 空列表、空材质、无 Pass 0 的条目有明确处理；
- 多 SubMesh、多 Renderer 和动态实例仍然可见；
- 替换 RT 和替换 Mask 都会清空并重新生成。

### 7.4 修改 RenderPassEvent 或 RendererFeature

必须检查：

- Pass 是否添加到了当前 Renderer Data；
- `AddRenderPasses` 和 `SetupRenderPasses` 的顺序假设是否仍适用于当前 URP；
- Scene/Game/多相机是否使用各自的 RTHandle；
- PassEvent 是否处于效果所需的渲染阶段；
- 是否与 Volume、其他 Feature 或默认后处理发生覆盖；
- Preview Camera 是否被安全跳过或单独处理。

## 8. ProjectACG 验证记录

本节保留初始开发记录，不表示 2026-10-20 文档整理时重新完成了全项目构建。面板简化、Float 乘数与开关同步的验证范围见第 13 节。

### 已完成

- `AOT.csproj` 编译通过，最终检查为 0 警告、0 错误。
- `Assembly-CSharp-Editor.csproj` 编译通过，最终检查为 0 警告、0 错误。
- `git diff --check` 通过。
- 已检查运行时材质的编辑模式销毁分支。
- 已检查当前相机 `cameraColorTargetHandle`、RTHandle 和动态分辨率路径。
- 已检查排除对象 SceneScope 合并和普通材质 Renderer 缓存路径。

### 尚未由命令行证明

- Unity Editor 中实际拖动 Timeline 参数的画面稳定性；
- Scene/Game/多相机下的最终画面一致性；
- Frame Debugger 中 Pass 0、Mask 和 Composite 的实际执行顺序；
- 目标设备上的 GPU 时间和 GC Alloc 数值；
- 动态分辨率、MSAA 和真实场景对象数量下的性能曲线。

### 推荐回归顺序

1. 在当前 Renderer Data 添加并确认 Feature。
2. 创建示例颜色材质，确认 Pass 0 和属性类型。
3. 创建一个 Clip，拖动颜色参数，确认 Game 不闪烁且立即更新。
4. 添加 Hierarchy 排除对象，确认对象仍保持原色。
5. 添加普通材质条目，分别测试根节点、子 Renderer、多 SubMesh 和 `(Clone)`。
6. 叠加两个 Clip，测试排除对象是否仍然生效。
7. 切换 Scene/Game、暂停、Seek、Stop、重新播放和销毁 Director。
8. 用 Profiler 对比开启前后的 CPU、GPU、GC Alloc 和全屏 Pass 数量。

## 9. 整体显隐参数与 Timeline 控制（2026-10-20）

### 9.1 问题、来源与本次归档范围

`CustomPostProcessingEffectOutline` 需要一个能够让描边主体、外缘与细节发光整体出现/消失的制作参数。既有隐藏权重用于 Timeline 播放，不适合再由美术曲线争用；颜色、宽度、扰动等参数也不能等价表达整体覆盖淡入淡出。

该功能已经存在于源码提交 `39f3481b97636ba1ff2d8dd63dffb70ac7108dbb`（2026-10-19，主题含“后处理描边shader添加显示控制参数”）。2026-10-20 的工作是核对源码并整理经验，没有在本次文档归档中再次修改 Shader。该提交对目标 Shader 的差异为 5 行新增、2 行删除；其 C# 消费链与预览机制作为既有依赖核对。

| 快照项 | 值 |
| --- | --- |
| 核对工作区 | `D:/work2025U3D/Valkyria/ProjectACGMain2/ProjectACG/Client` |
| Unity / URP / Timeline | `2022.3.62f3` / 嵌入式 `14.0.12` / `1.7.7`；来源为版本与包文件 |
| Shader 文件 | `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingEffectOutline.shader` |
| Shader 名 | `Valkyria/Custom Post Processing/Effect Character Outline` |
| Shader SHA256 | `9F3215021E0B593629FB43775A68D0E75F74AF19217D56ADA795FD389184929E` |
| 功能旁说明 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/README_CustomPostProcessingTimeline.md` |
| 可迁移推导 | [后处理整体显隐与 Timeline 权重参考](../../../ShaderDevelopment/references/post-effect-visibility-and-timeline-weight.md) |

以下行号用于该次源码定位，后续改动后以类型、函数和属性名为准。第 8 节保留的历史 C# 编译记录不构成本节 Shader 的 Unity 编译、视觉或设备性能验证。

### 9.2 参数归属与最小实现

| 属性 | 当前语义与归属 | 已确认边界 |
| --- | --- | --- |
| `_EffectVisibility` | 材质视觉总控，显示名“整体效果显现”，`Range(0,1)`，默认 1 | 可由材质或 Float 属性控制器驱动；控制覆盖淡入淡出 |
| `_CustomPostProcessingWeight` | 隐藏的 Timeline 权重，默认 1；由 RendererFeature 写入 `effect.Weight` | 用户曲线的同名值会被最后覆写，不作为制作总控 |
| `_OutlineColor.a` / `_RimColor.a` | 主体/外缘各自的基础覆盖率 | 保留局部配方，再统一乘整体权重 |
| `_DetailIntensity` / `_DetailColor` | 细节发光强度和 HDR 颜色 | 发光加入主体/外缘颜色，随统一覆盖率一起退出 |

Shader 的 `Properties` 第 5 行加入：

```shaderlab
_EffectVisibility("整体效果显现", Range(0, 1)) = 1
```

在原有 `UnityPerMaterial` CBUFFER 中声明 `half _EffectVisibility;`。`Frag`（约第 190 行）先采样输入画面，再组合两个权重：

```hlsl
half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
half effectWeight = saturate(_EffectVisibility) * saturate(_CustomPostProcessingWeight);
if (_CustomPostProcessingObjectMaskTexelSize.x <= 0.0 || effectWeight <= 0.0h)
    return scene;
```

原有形状、扰动和保护计算后，最终覆盖使用：

```hlsl
half weight = effectWeight * (1.0h - protection);
half coreAlpha = core * saturate(_OutlineColor.a) * weight;
half rimAlpha = rim * saturate(_RimColor.a) * weight;
half3 color = scene.rgb * (1.0h - coreAlpha - rimAlpha)
    + coreColor * coreAlpha + rimColor * rimAlpha;
return half4(color, scene.a);
```

这些片段摘自现有 Shader，依赖其现有变量和 URP include，不是可独立导入的完整 Shader。`coreColor`、`rimColor` 已包含同一份细节发光；总控作用在覆盖率上，因此效果减弱时背景同时恢复，输入 alpha 保留，HDR RGB 没有被总控额外钳制。

当前 `Blend Off`，合成在 Fragment 内完成。只乘输出 RGB 会留下背景扣减，只乘输出 alpha 不会自动淡出 RGB。默认 1 的意图是保留旧强度；旧材质重导入和实际 Bundle 的默认值行为尚未在 Unity 中复验。

此次实现没有增加 Keyword、Pass、RT 或提高 Shader target。原四个 Pass 为 `SpiritCharacterOutline`、`GenerateOutlineMask`、`MaskKawaseDownsample`、`MaskKawaseUpsample`，既有 `target 3.0` 保持。显隐为 0 只让本效果 Fragment 返回其输入，不承诺关闭替换材质、整个 Composite 或整个后处理链。

### 9.3 Timeline 自动接入与真实混合公式

`CustomPostProcessingTimelineEditor.cs` 的 `GetPropertyNames`（约第 1402 行）枚举 Float、Range、Vector、Color 属性，并过滤 `IsClipControlledShaderProperty`。新增属性为 Range 且不在过滤表内，因此沿现有选择器自动进入候选，不需要为此属性新增 Inspector 分支。

当前过滤的形状属性只有 `_OutlineInputMode`、`_FilledMaskExpansionPixels`、`_FilledMaskExpansionMode`、`_FilledMaskUVScalePivot`。选择器没有按 `HideInInspector` 排除属性，所以隐藏的 `_CustomPostProcessingWeight` 仍可能出现在候选中；其不能作为可靠美术总控的原因是 Feature 的最终覆写，不能写成“隐藏所以无法选中”。

Float 控制链如下：

```text
源材质参数 M
  → GetRuntimeMaterial：每次 ProcessFrame 先 CopyPropertiesFromMaterial
  → controller.Evaluate(t)：Q(t) = curve(t) × floatValue
  → ApplyProperties：V = Lerp(M, Q(t), Clamp01(weight))
  → RendererFeature：_CustomPostProcessingWeight = effect.Weight
  → Frag：G = Saturate(V) × Saturate(effect.Weight)
  → 角色局部覆盖：G × (1 - protection)
```

`CustomPostProcessingClip.cs` 的 `Evaluate`（约第 151 行）先把归一化时间钳制到 0–1；曲线为空时返回的曲线值为 0；Float 类型再乘 `floatValue`。`CustomPostProcessingTrack.cs` 的 `ApplyProperties`（约第 307–360 行）检查属性名、Clip 所有权和类型，随后从源材质值向曲线目标插值。`RendererFeature.cs` 约第 570–572 行最后写自动播放权重。

令 `w=Clamp01(effect.Weight)`，在正常输入权重范围内：

```text
Q(t) = curve(t) × floatValue
V = lerp(M, Q(t), w)
G = saturate(V) × w
```

| 源显隐 M | 曲线目标 Q | Clip 权重 w | 实例显隐 V | Shader 总权重 G |
| --- | --- | --- | --- | --- |
| 1 | 0 | 1 | 0 | 0 |
| 1 | 0 | 0.5 | 0.5 | 0.25 |
| 1 | 1 | 0.5 | 1 | 0.5 |
| 0 | 1 | 0.5 | 0.5 | 0.25 |

因此，Clip 未到满权重时，属性曲线 0 不保证完全隐藏；Clip 自带淡入淡出与显隐曲线也可能组合成重复缓动。无该属性控制器时，以复制后的源材质值参与合成。满权重时，曲线会覆盖源材质同名字段，编辑源值不一定产生可见变化。此处记录现有混合合同，没有为本参数改写整个 Mixer。

### 9.4 材质实例与编辑器预览

`GetRuntimeMaterial` 按效果持有独立材质副本，复用时先复制源材质，再应用曲线。实例使用 `HideAndDontSave`，播放模式 `Destroy`，编辑模式 `DestroyImmediate`，图销毁时清理；源材质资产本身不因曲线求值被修改。

停帧改材质还依赖既有 `CustomPostProcessingMaterialPreviewRefresh.cs`：监听相关源材质变化和 Undo，合并请求，以 `SceneNeedsUpdate` 触发当前时间求值，而不是每次 `ContentsModified` 重建图。完整操作与维护说明以功能旁 README 为准，本归档只记录影响总控生效的依赖。

排查优先级为“源材质 → 实例复制 → 属性曲线 → Feature 自动权重 → Shader 合成”。另需注意 Shader 的 `_Time` 动画不会因 Timeline 暂停自动冻结；静态 A/B 要控制时间和纹理输入。暂停会触发 `OnGraphStop`，既有逻辑不能因此直接清空预览；owner 与实例释放在 `OnPlayableDestroy` 等实际销毁边界处理。

### 9.5 成本、问题归因与可借鉴实现

| 问题或误判 | 已核对原因 | 实现或处理方式 |
| --- | --- | --- |
| 已有 Timeline 权重，是否无需新参数 | 该字段由 Feature 自动写入，控制权不属于材质制作 | 使用独立视觉参数，在 Fragment 乘法组合 |
| 淡出变黑或剩余发光 | RGB、覆盖率和独立发光的职责可能混淆 | 本实现统一乘主体/外缘覆盖率，细节发光包含在对应颜色内 |
| 曲线为 0 仍有残留 | 源值到曲线目标的插值，再乘播放权重 | 用第 9.3 节公式核对，不把现有行为直接判为 Shader 失效 |
| 停帧改源材质无变化 | 未求值、实例未同步，或同名曲线覆盖 | 检查既有刷新链和控制器优先级 |
| 0 时仍有渲染成本 | Mask/Blur 等已在执行 Shader 之前发生 | 早退只跳过 Fragment 后续工作；调度优化另立证据 |

当前 Feature 顺序为 `RenderObjectMask → RenderOutlineMask → RenderBlurredMask → RenderReplacementMaterials → 效果 Pass 0 Blit → 可选 Composite/拷回`。本总控没有让前面的 Mask 和 Blur 自动消失，也没有取消全屏复制或 RT 成本。普通材质替换和排除 Composite 另有职责，不能只依据 `_EffectVisibility=0` 就跳过整个 effect。

邻近实现可用于比较，不能直接替代本合同：`CustomPostProcessingColorExample.shader` 有强度与播放权重组合；`CustomPostProcessingDistortedOutline.shader` 有更复杂的形状/溶解控制；`CharacterDistortedOutlineComposite.shader` 的 `_OutlineIntensity` 用于发光加法，其语义与覆盖式显隐不同。本次参数只涉及 `EffectOutline`，未统一其他 Shader。

### 9.6 证据、工具与复查命令

| Evidence：源码锚点 | Finding：核对结论 | Path：对实现/排查的影响 |
| --- | --- | --- |
| 目标 Shader 的 Properties、CBUFFER、`Frag` | 新属性独立且参与统一覆盖，0 时返回输入 | 从声明追到消费，避免只增加面板参数 |
| Clip 的 `Evaluate`、Track 的 `ApplyProperties` | Float 曲线乘幅度，并按源值插值 | 曲线验证同时记录源值、幅度与 Clip 权重 |
| Editor 的 `GetPropertyNames` 与 Runtime 的属性过滤函数 | Range 可自动接入，隐藏标记不是所有权过滤 | 新参数沿现有路径接入，内部字段仍由 Feature 主导 |
| Track 的 `GetRuntimeMaterial`、材质预览刷新脚本 | 先复制再覆盖，停帧需重新求值 | 区分面板资产值与实际绘制实例值 |
| Feature 的权重写入与 Mask/Blit 调用顺序 | 自动权重最后写入，前置工作仍执行 | 不承诺零耗时或关闭整个渲染链 |

以下命令在第 9.1 节的 Client 工作区执行，都是只读检查：

```powershell
git show --stat 39f3481b97 -- Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingEffectOutline.shader
git show 39f3481b97 -- Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingEffectOutline.shader
rg -n '_EffectVisibility|_CustomPostProcessingWeight|coreAlpha|rimAlpha' Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingEffectOutline.shader
rg -n -g 'CustomPostProcessing*.cs' 'Evaluate|ApplyProperties|CopyPropertiesFromMaterial|GetPropertyNames|IsClipControlledShaderProperty' Assets/GameScripts
rg -n 'SetFloat|RenderObjectMask|RenderOutlineMask|RenderBlurredMask|RenderReplacementMaterials|BlitCameraTexture' Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingRendererFeature.cs
Get-FileHash -LiteralPath 'Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingEffectOutline.shader' -Algorithm SHA256
git diff --check -- Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingEffectOutline.shader
```

`rg -g` 用于 Windows 文件过滤；不要将 `目录/CustomPostProcessing*.cs` 当成必定存在的单个路径。中文文本用显式 UTF-8 读取；维护现有文档保留 BOM/换行，新增文档同步 `.meta`，发布后回读校验。

### 9.7 验证状态与回退边界

**本次已核对**：实际 Shader、Mixer、Clip、Editor 属性枚举、Runtime 过滤、RendererFeature 与预览链源码；提交差异、目标 Shader SHA256 和版本来源。目标 Shader 当前无未提交差异，不能把空 `git diff` 当成此次功能不存在。

**尚未完成 Unity 实测**：Shader 在各目标图形 API 的导入/编译；显隐 0、0.25、0.5、1 的画面对照；主体、外缘、细节光、角色保护和输入 alpha；曲线与 Clip 淡入淡出的组合；停帧修改、Undo、Seek、Pause、Stop 和图销毁；Scene/Game/多相机、分辨率与屏幕边缘；旧材质重导入、实际 Bundle 与目标设备性能。源码公式确认不替代这些验证。

推荐先在完整 Clip 权重、关闭同名控制器的条件下验证材质 0/1 端点，再开启曲线并测试半权重，随后覆盖保护、预览和多相机，最后用 Frame Debugger/Profiler 验证时序与成本。沿用 [通用参考的验证矩阵](../../../ShaderDevelopment/references/post-effect-visibility-and-timeline-weight.md#51-最小充分验证矩阵)。

视觉回退候选是将 `_EffectVisibility` 恢复为 1，并停用该属性控制器；前提是自动播放权重与其他参数也回到比较基线。若需代码回退，按原提交的局部 Shader 差异审查处理，不重置整个工作区，不修改既有 `.meta`，也不顺手回滚其他材质、轨道或 Renderer 配置。

## 10. 本轮需求、决策与源码基线

本轮围绕同一个可用性问题收敛：材质控制器越来越多，但用户只想快速找到当前要改的参数，并立即看到效果。改动属于局部 Inspector、控制器求值和编辑期刷新协作，没有为面板折叠引入新的运行时配置系统。

| 需求 | 采用的实现 | 关键边界 |
| --- | --- | --- |
| 属性选择支持搜索、展开后可滚动 | 自绘 Popup，搜索描述和内部名，限高列表 | 不改序列化绑定标识，失效项保留诊断 |
| Float 同时支持曲线和数值 | `curve(t) × floatValue`，乘数默认 1 | 两者相乘，再与源值按 Clip 权重混合 |
| 后处理效果区容易识别 | 整块效果面板使用主题适配的蓝色底 | 不对全部 GUI 文字和控件统一染色 |
| 开关属性显示勾选框且真正生效 | 识别内置 Toggle 家族，运行时同步 Keyword | ToggleUI 只写数值；ToggleOff 反向；仅写临时副本 |
| 提示太多、占空间 | 长说明默认收起，字段短说明放 Tooltip | 错误与风险诊断独立保留 |
| 参数按需出现 | 材质能力 + 当前模式 + 折叠摘要 + 完整入口 | 不删除隐藏配置，多选时显示完整设置 |

文档整理时源码 HEAD 为 `4c979a94cbedcdebce9a55c1d20159b9d687148e`。相关历史提交为 `12efbc1324`（2026-10-19，界面优化）和 `60398d3283`（2026-10-19，界面简化）。这些标识用于定位开发记录，后续维护仍以当前代码为准。相关源码在核对时没有未提交差异；工作区其他资产有用户改动，本轮文档任务未修改它们。

代码复用来源包括 `CharacterMaterialTimelineClipEditor.cs` 的材质属性搜索、`BlendShapeTimelineClipEditor.cs` 的搜索交互，以及 `CharacterAttachPoint/AttachPointControlEditor.cs` 的滚动与选中态实现。复用的是局部模式，不是将它们的绑定、序列化和生命周期整套搬入。

## 11. 实现方式与可迁移技巧

### 11.1 面板按能力、模式与配置状态显示

`CustomPostProcessingClipEditor` 使用局部 `MaterialCapabilities` 位标记，扫描 Clip 中有效效果材质并取并集。判断依据是公开属性及 Pass，不是 Shader 显示名。

| 能力或状态 | 当前识别/显示方式 | 维护注意 |
| --- | --- | --- |
| 形状输入 | 声明 `_OutlineInputMode` | 形状参数为 Clip 内共用，不能让用户误以为每个效果独立 |
| 像素外扩 | 声明对应外扩像素属性 | 实心模式下显示；数值保留而非切换时清空 |
| UV 缩放 | 同时声明外扩模式与 UV 缩放中心属性 | 自动中心开时隐藏手动中心；未知合同用完整入口 |
| 中间描边/外扩 RT | `GenerateOutlineMask` 与当前形状分支共同判断 | 实心零外扩或 UV 直接路径可不显示该 RT 比例；混合效果按并集保留 |
| 模糊 | 同时有 `MaskKawaseDownsample`、`MaskKawaseUpsample` | 级数为 0 时收起间距；是否实际执行还依赖相机 RendererFeature 开关 |
| 对象作用目标 | 已知 Mask 能力或已有目标配置 | 摘要区分绑定/自动解析、指定名称、额外对象 |
| 未知材质与旧配置 | 保留摘要和 `显示全部设置` | 推断不完整时不宣称“此 Shader 一定不支持” |

`OnInspectorGUI` 在绘制形状后重新计算能力，再绘制画质区，避免同一事件内使用旧模式判断。画质、作用目标和高级项默认收起，摘要包含当前参数或“已调整”标记。空的可选列表优先显示添加入口，有效条目显示数量，普通材质条目错误时打开修正内容。

无有效材质时，默认先显示材质字段与诊断，已有控制器只保留数量提示。效果名通过标题与“改名”入口编辑。控制器折叠标题显示属性和值，新增时自动展开；与 Shader 类型一致时省去重复的值类型控件。

控制器在面板宽度不小于 `420` 时并排显示曲线和 Float，小于该宽度时分行；`labelWidth` 在 `finally` 中恢复。效果组在 `VerticalScope(EditorStyles.helpBox)` 的 Repaint 阶段绘制实色底，边缘缩进 1，按明暗主题选择蓝色，不创建背景纹理。

帮助开关为 Editor 内状态，效果级“说明”还会展开对应作用目标；普通说明默认收起。失效材质、属性和类型等诊断在控制器/效果收起路径仍执行。这里的“默认隐藏提示”只针对说明，不是屏蔽全部错误。

### 11.2 搜索 Popup 与安全写回

`MaterialPropertySearchPopup` 宽 `460`，行高 `24`，窗口高度按 `3～10` 个可见条目限制。列表强制竖向滚动，行文字包含 Shader 描述和内部属性名，查询 Trim 后忽略大小写，支持清除、焦点恢复、无结果提示、选中高亮与 Escape 关闭。

可选集合只包括 Float/Range、Vector、Color，并过滤 `CustomPostProcessingRuntime.IsClipControlledShaderProperty` 管理的系统参数。切换属性后通过 Shader 类型同步控制器类型。旧绑定失效时不自动改成第一项。

弹窗保存控制器的 `propertyPath`，在 `ApplyPropertySelection` 中重新 `Update/FindProperty`，不跨 GUI 事件持有数组迭代器。写回后使用 `_changed |= ApplyModifiedProperties()`，再请求 `SceneNeedsUpdate` 并重绘。

当前只校验 Editor、目标、材质及路径是否存在，未实现稳定元素身份。弹窗未关闭时若能重排/删除列表或切换材质，仍有路径指向其他元素或资源已变化的风险。这是待验证/加强项，不应描述为完全解决异步编辑一致性。

### 11.3 Float 乘数与混合顺序

`CustomPostProcessingPropertyController.floatValue` 默认 `1f`。`Evaluate` 对归一化时间 Clamp 后求曲线，Float 返回曲线乘 Float；Vector/Color 返回曲线，再在 Mixer 中乘各自的向量/颜色。曲线为 null 时当前返回 0。

`CustomPostProcessingTrackMixer.ApplyProperties` 按源材质当前值和 `Clamp01(weight)` 做 Lerp。Float 示例：曲线 0.4、乘数 3、源值 0、权重 0.5，最终为 0.6。Curve 没有因为 Shader 的 Range 标签而改成受限倍率滑条。

添加控制器时显式初始化属性名、曲线、类型、Float/Vector/Color；特别补上 `floatValue = 1`，避免 `arraySize++` 继承上一条为 0 的开关数值。中性默认值旨在保留旧曲线语义，但既有 Timeline 资产缺失字段的 Unity 反序列化结果尚未验收。

### 11.4 开关控件与运行时 Keyword

`CustomPostProcessingShaderToggle` 为 Editor/Runtime 共用的只读元数据结构。`TryGet` 仅处理 Float/Range，精确识别 `Toggle`、`ToggleUI`、`ToggleOff`，显式参数优先，无参数时分别使用大写属性名加 `_ON` / `_OFF`。

| 属性示例 | Drawer/Keyword | 当前行为 |
| --- | --- | --- |
| `_DissolveEnabled` | `Toggle(_DISSOLVE)` | 混合后 Float 大于 0.5 时启用 `_DISSOLVE` |
| `_DistortEnabled` | `ToggleOff(_DISTORT_OFF)` | 混合后 Float 大于 0.5 时关闭 `_DISTORT_OFF` |
| `_OutlineScaleWithCamera` | `ToggleUI` | 连续 Float 按曲线写入，不改 Keyword |

Keyword 通过 `shader.keywordSpace.FindKeyword` 解析，仅对有效声明调用 `Material.SetKeyword`。最终 Float 等于 0.5 时按关闭处理；ToggleOff 再反转 Keyword 状态。`floatValue` 的勾选框只在用户明确点击后写 0/1，重绘时不把历史小数归一化。多选保守退回数值编辑。

控制器按 Shader 和属性名缓存元数据，换 Shader/属性后重新解析；同一 Shader 对象重导入的缓存失效尚未专门处理。自定义 `Toggle_Switch` 等 Drawer、枚举互斥关键字组不在当前支持范围。

### 11.5 源材质、临时副本与暂停预览

当前更新链路是：源材质编辑 → Editor 请求当前时间求值 → `GetRuntimeMaterial` 同步源副本 → `ApplyProperties` 覆盖数值与 Keyword → Runtime Registry 发布当前 owner 帧 → RendererFeature 消费。

源材质与 Shader 未变时复用同一副本，每次求值先 `CopyPropertiesFromMaterial(source)`；源或 Shader 改变时销毁并重建。副本使用 `HideAndDontSave`，编辑态 `DestroyImmediate`，播放态 `Destroy`。控制器删掉后，后续求值回到源状态，不沿用上一帧的动画 Keyword。

`OnGraphStop` 会在暂停和编辑期求值时调用，因此当前 Mixer 不在这里清空。`ProcessFrame` 替换当前 owner 的帧，Seek 出界提交空效果，`OnPlayableDestroy` 注销本 owner 并释放副本。直接 `PlayableGraph.Stop()` 且保留图会保留画面；完全结束按当前合同停止 Director 或销毁图。多个 Director 不共享清理所有权。

`CustomPostProcessingMaterialPreviewRefresh` 监听 `ObjectChangeEvents.changesPublished`，仅处理活动预览图及当前/主 Timeline 引用的持久材质，忽略副本与无关资产，每批事件请求一次 `SceneNeedsUpdate`。脚本重载前注销事件。当前功能没有新增 Player 轮询、Pass 或 RT。

曲线控制优先于源材质：受控属性在满权重下由控制器完全决定，暂停时改源材质该属性看不到变化可能是预期结果。未受控属性应跟随源材质。Shader `_Time` 仍继续变化，暂停 Timeline 不等于冻结所有贴图动画。

## 12. 本轮问题、根因、修正与验证边界

以下“修正”表示代码中已落实的处理；功能是否通过宿主验收按第 13 节判断。

| 现象/风险 | 根因 | 代码处理 | 证据或剩余边界 |
| --- | --- | --- | --- |
| 下拉选项太多 | 普通 Popup 无过滤且一次展开过长 | 搜索、限高、滚动、描述与内部名并列 | 源码已核对，实际长列表与键盘交互待 Unity 验收 |
| 只能画曲线，不能直接调整幅度 | Float 缺独立乘数与对应求值 | 增加中性乘数，曲线后相乘 | 求值与完整相关链路已编译，旧资产反序列化待验 |
| 勾选溶解仍可能不生效 | 只改 Float，没有同步 `_DISSOLVE` | 基于 Drawer 元数据写入已声明 Keyword | 用例覆盖正反开关与混合，但仅编译未执行 |
| 开关在淡入中切换时机不一致 | 用目标值而非混合后值决定 Keyword | 依据最终混合 Float 过阈值判断 | `ToggleKeyword_UsesValueAfterClipBlend` 待运行 |
| 打开面板就把旧小数改成 0/1 | 自绘 Toggle 每次回写 | ChangeCheck，仅显式点击写入 | Undo/Dirty 与历史值需交互验收 |
| 弹窗选值后刷新标记丢失 | 后续 Apply 返回 false 覆盖前次变更 | `_changed` 累积后消费 | 仍有路径身份与材质变更一致性边界 |
| 暂停时后处理消失 | 把 GraphStop 当最终清理 | 暂停保留，出界空帧，销毁释放 | 真实 Director 用例已写，尚未跑 Test Runner |
| 暂停改材质要重播才更新 | 同步逻辑存在，但没有触发 ProcessFrame | 相关源材质变化触发当前时间求值 | 手动 Evaluate 用例不等价于事件监听全链路测试 |
| 求值触发无限刷新 | 监听到了自己写入的副本 | 过滤持久资产、当前引用并合并事件 | Undo/Redo、重载和资源切换待宿主验收 |
| 参数拖动带动相机回跳 | 轻量修改使用整图重建 | 当前 Inspector 数值读取链路使用 SceneNeedsUpdate | 其他轨道若缓存建图数据，仍需各自判断 |
| 面板简化后找不到已配置值 | 只隐藏，没有摘要与回退 | 已调整/保留摘要、完整模式、多选完整视图 | 未知 Shader 与混合能力组合仍需人工验收 |
| 新增开关沿用上一项的 0 | SerializedProperty 新元素可能复制旧元素 | 显式初始化全部控制器默认值 | 新增/删除/重排和对象引用双阶段删除待交互验收 |

## 13. 工具、证据与验证方法

### 13.1 工具选择

| 工具/API | 在本轮解决的问题 | 不能由它单独证明的内容 |
| --- | --- | --- |
| `rg`、`rg --files` | 定位同类搜索器、字段生产者/消费者、Keyword 与回调 | 执行顺序和真实画面 |
| Shader 属性 API、`ShaderUtil` | 类型、描述、Drawer 与能力枚举 | 实际 Pass 分支是否执行 |
| `PopupWindowContent`、SerializedObject/Property | 搜索与受控序列化写回 | 异步元素身份、多选和 Undo 全部正确 |
| `LocalKeyword`、`keywordSpace`、`SetKeyword` | 限定已声明关键字的材质写入 | Player 变体保留与平台 Shader 编译 |
| Timeline Refresh、ObjectChangeEvents | 当前时间求值、相关源材质刷新 | 真实拖参体验与事件边界 |
| Unity 自带 Roslyn + `.rsp` | 使用真实 Unity 引用做 C# 子集编译 | 完整 Unity 导入、程序集边界、测试执行和渲染 |
| Unity Test Runner | 执行 Director 生命周期与控制器断言 | 本轮尚未运行，不能称测试通过 |
| Frame Debugger、Profiler | 后续捕获 Pass、临时纹理和 CPU/GPU/GC | 本轮未采集当前面板优化的实测数据 |
| Git 状态/历史、`git diff --check` | 范围、提交基线、空白问题 | 功能正确性 |
| 显式 UTF-8 读写、哈希与相对链接检查 | 文档中文、写入一致性与索引完整 | Unity 内功能验收 |

复用命令示例：在 Client 根目录执行 `rg -n 'floatValue|ApplyToggleKeyword|ApplyProperties|OnPlayableDestroy' Assets/GameScripts/AOT/GameArt/TimelineTrack`，可以从控制器追到写入与释放。搜索应限制到实际模块，避免扫描 Library 和无关生成目录。

### 13.2 已完成与未完成

开发阶段已完成 Unity `2022.3.62f3` 管理程序集引用下的 Roslyn 子集编译，覆盖 Clip、Inspector、Toggle helper、Track/Mixer、Runtime 与暂停/Toggle 测试源码；另做 Runtime 不定义 `UNITY_EDITOR` 的编译检查。完成了相关文本、刷新链路、只写临时材质和差异空白核对。

编译辅助使用 Unity 的 `Data/DotNetSdkRoslyn/csc.dll`、NetStandard 参考程序集、netfx shims、Unity 管理程序集和当前工程 `Library/ScriptAssemblies`，通过响应文件传递引用与源码。参与编译的源码与旧 AOT 引用可能产生同名类型警告，本轮窄检查定向抑制了 `CS0436`；这也是不能把结果等同于完整 Unity 程序集构建的原因。迁移时按 [离线编译参考](../../../ToolDevelopment/references/unity-editor-script-offline-compile-verification.md) 重建引用闭包，不复制机器临时路径或一律排除所有 AOT 依赖。

测试源码的重要设计：`PauseKeepsLastFrame_ResumeReusesMaterial_StopReleasesIt` 暂停后刻意不 Evaluate，避免重新发布帧掩盖暂停丢失问题；源材质同步用例手动 Evaluate，只覆盖同步结果；它不证明 `ObjectChangeEvents` 自动触发路径已经通过。另有多 Director 销毁隔离、正反 Toggle、最终混合阈值和 ToggleUI 保持连续值的用例。

尚未完成：Unity Test Runner 执行、真实 Inspector 布局/搜索/拖值、Undo/Redo、多选、未知 Shader、旧 Timeline 资产、脚本/Shader 重载与目标设备验证。开发时工程已打开，未关闭用户 Unity 进程或启动同工程第二实例；锁文件存在不是测试失败，也不能当作测试已通过。

源码 README 中关于 D3D11 WARP、关键字组合编译、Mask/UV/Kawase 的数字属于其他渲染工作阶段的历史证据。它们不证明本轮搜索、面板折叠、自动停帧刷新或 Toggle 回归测试通过。早期仓库级 readiness/harness 存在既有规则、链接和本地化问题，也不应写成此功能的新增失败或全绿门禁。

### 13.3 后续最小验收清单

| 场景 | 操作 | 通过条件 |
| --- | --- | --- |
| 搜索与布局 | 搜描述/内部名、清除、滚动、Escape、窄面板与明暗主题 | 可定位、无误写、无背景/控件遮挡 |
| 依赖显示 | 空心、实心零/正像素外扩、UV 自动/手动、模糊 0/正级数 | 相关参数出现，共用设置摘要准确，数据不被清空 |
| 回退与多选 | 未知 Shader、不同材质混合选择、完整模式 | 可访问全部保留参数，无隐式统一写入 |
| Float 与旧资产 | 原 Timeline 资产 + 新建控制器，恒 1 和变化曲线 | 乘法/混合公式正确，旧曲线不被意外缩成 0 |
| 开关 | 正向、反向、ToggleUI、0.5 阈值、部分 Clip 权重 | 数值与 Keyword 一致，源资产不变 |
| 暂停生命周期 | Play/Pause/Resume、Seek 出入 Clip、Stop、销毁图、两个 Director | 暂停保留、出界清空、只释放对应 owner 与副本 |
| 自动材质刷新 | 暂停改未受控属性/贴图/开关，Undo/Redo，再改受控属性 | 未受控项即时跟随；受控项仍遵守 Clip 优先，无刷新循环 |
| 编辑事件边界 | 弹窗未关时改列表/材质，重载 Shader/脚本 | 无误改；若不能保证则关闭弹窗或增加身份校验 |
| 实际渲染 | 捕获当前相机、质量档和目标构建 | 正确 Pass/变体生效，无未经测量的性能承诺 |

## 14. 维护、迁移与回退边界

继续维护时按现有稳定规则加载：`TOOL-CMP-01` 管资产兼容，`TOOL-UI-02` 管预览临时态，`TML-BLD-01` 管混合，`TML-LIF-02` 管暂停/出界/销毁，`TML-EDT-03` 管刷新，`TML-VAL-01` 管宿主验证。具体字段与 Pass 留在本 Profile，不升级为通用 CORE。

可复用的是“依赖表先于条件隐藏”“曲线形状乘幅度”“按最终混合值同步离散状态”“源基线先于动画覆盖”“暂停保留与销毁释放分开”等实现思路。迁移时重建 Shader 能力合同、Drawer/Keyword 约定、混合阈值、停止语义、变体保留和程序集依赖；不要整体复制当前项目的属性清单。

当前风险还包括：路径型折叠状态随重排变化，Shader 同实例重导入的元数据缓存失效不完整，Inspector 内多次能力扫描未做性能测量，以及较大的 Editor 类后续扩展可能需要拆分。这些是验证或设计候选，不是已复现的性能事故。

回退优先使用完整视图检查保留数据，再按显示层、Keyword Adapter、刷新监听、求值语义分别定位。功能回滚按单独提交/补丁处理；乘数字段已经保存到资产后，不能只恢复旧运行时而忽略数值语义。文档回退不触碰材质、Timeline 资产、Renderer Data 或 Unity 生成目录。
