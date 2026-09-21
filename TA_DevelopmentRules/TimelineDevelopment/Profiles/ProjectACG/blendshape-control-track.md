---
name: projectacg-blendshape-control-track
description: ProjectACG 当前工程 BlendShape Timeline Control Track 的实现、文件映射、动态绑定入口和验证边界。不可直接迁移到其他项目。
---

# ProjectACG BlendShape Timeline Control Track Profile

## 1. 适用范围与迁移边界

本 Profile 只记录当前 ProjectACG 工程的 BlendShape 轨道实现事实。跨项目方法以 [BlendShape Timeline 通用参考](../../references/timeline-blendshape-track-authoring-and-runtime-binding.md) 和 [Timeline CORE](../../README_Tech_TimelineDevelopmentRules.md) 为准；本文件中的路径、类名、枚举和验证状态不得直接当作通用规范。

## 2. 当前实现能力

当前轨道支持：

- 一个 Renderer 下添加多个 BlendShape；
- Renderer 分组折叠，BlendShape 搜索弹窗；
- 权重滑条和 `AnimationCurve`；
- Clip 淡入淡出、`ILayerable` 与 Add Layer；
- `Override`、`Additive`、`Multiply`、`Maximum`；
- 表情预设、捕获当前权重、当前帧归零、应用到当前帧；
- Renderer/BlendShape 缺失、重复目标校验；
- 停止、绑定切换、目标失效时恢复原始权重；
- 仅权重变化时调用 `SetBlendShapeWeight`；
- 编辑器手动绑定预览和运行时动态 Generic Binding；
- Director 层级相对路径和字符串 GameObject 路径；
- Renderer 字典缓存，避免每帧 `Transform.Find` / `GetComponentsInChildren`；
- 旧版单 BlendShape 数据迁移到新条目列表。

## 3. Source of Truth

| 职责 | 当前文件 |
| --- | --- |
| Track、BindingMode、Mixer、Layer Mixer、状态恢复 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/BlendShapeTimelineTrack.cs` |
| Clip、条目、混合模式、Preset Asset | `Assets/GameScripts/AOT/GameArt/TimelineTrack/BlendShapeTimelineClip.cs` |
| 运行时绑定 API | `Assets/GameScripts/AOT/GameArt/TimelineTrack/BlendShapeTimelineBinding.cs` |
| Clip Inspector：分组、搜索、校验、捕获与预设 | `Assets/GameScripts/Editor/TimelineTrack/BlendShapeTimelineClipEditor.cs` |
| Track Inspector / Binding Mode | `Assets/GameScripts/Editor/TimelineTrack/BlendShapeTimelineTrackEditor.cs` |
| Timeline Clip 外观 | `Assets/GameScripts/Editor/TimelineTrack/BlendShapeTimelineClipTimelineEditor.cs` |

当前绑定模式：

~~~csharp
public enum BlendShapeTimelineBindingMode
{
    DirectorHierarchy,
    RuntimeGenericBinding,
}
~~~

## 4. ProjectACG 运行时绑定约定

动态模型加载完成后，调用方使用：

~~~csharp
BlendShapeTimelineBinding.BindAll(director, loadedModelRoot);
director.time = 0d;
director.Evaluate();
director.Play();
~~~

模型位于 Director 层级下时，可使用：

~~~csharp
BlendShapeTimelineBinding.BindAllByPath(
    director,
    "Characters/Hero");
~~~

更换模型根时使用 `Rebind`；绑定根不变、只替换 Renderer 或 Mesh 时使用 `Refresh`；停止控制使用 `Unbind`。当前 `Rebind` 已经内部执行刷新，不需要随后重复调用 `Refresh`。绑定完成后会在必要时调用 Graph 输出重绑入口。

轨道在 Director 层级模式下优先使用显式 Generic Binding；没有显式绑定时，再按 `directorCharacterPath` 从 Director 根解析角色。运行时 Generic Binding 模式优先使用传入的绑定对象；配置了 `runtimeBindingGameObjectPath` 时在该根下解析目标 GameObject。

## 5. 已验证与未验证

### 已验证

- `dotnet build AOT.csproj --no-restore`：0 个错误；
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`：0 个错误；
- 目标脚本执行 `git diff --check` 无新增格式错误。

这些结果只证明离线编译和格式检查通过。

### 尚未验证

- Unity Inspector 的真实视觉布局；
- Play Mode 异步动态加载、绑定时序和模型换装；
- Timeline Preview 拖帧、暂停改值、交叠 Clip、Add Layer、Seek/Stop；
- 多角色、多 Renderer、多 Mesh 热替换压力；
- 与 Animator、其它表情系统同时写同一 BlendShape 的所有权仲裁；
- Player/移动端构建、Profiler 数值和实际 GC；
- Timeline 或 Unity 版本升级后的内部刷新语义。

## 6. 维护与回退

- 变更序列化字段前，先保留旧字段迁移逻辑并对已有 Timeline 资产做备份。
- 变更路径或绑定模式时，先在编辑器和运行时各跑一组“显式绑定、路径绑定、未绑定、模型晚加载”用例。
- 若动态解析不稳定，回退为显式 Generic Binding；若预览刷新兼容性不确定，回退到结构刷新并记录一次重建预览的影响。
