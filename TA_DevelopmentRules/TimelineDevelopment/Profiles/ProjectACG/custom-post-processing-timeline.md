---
name: projectacg-custom-post-processing-timeline-profile
description: ProjectACG 当前 Custom Post Processing Timeline 与 URP RendererFeature 的实现事实、问题记录、验证边界和维护规则。
---

# ProjectACG Custom Post Processing Timeline Profile

> 类型：ProjectACG `PROFILE`。本文记录当前工程的具体版本、路径、字段和验证结果，不是跨项目 CORE。
>
> 记录日期：2026-09-19
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
| `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingRendererFeature.cs` | URP Feature、Pass、RTHandle、Mask、普通材质重绘和 Composite |
| `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingMask.shader` | 排除 Mask 和普通材质替换 Mask |
| `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingComposite.shader` | 原画面、效果画面、排除 Mask、替换 RT 的最终合成 |
| `Assets/Shader/RenderFeature/CustomPostProcessing/CustomPostProcessingColorExample.shader` | 示例颜色调整后处理 Shader |

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

