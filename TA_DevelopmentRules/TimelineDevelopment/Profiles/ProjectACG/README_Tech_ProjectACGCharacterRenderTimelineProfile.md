---
name: projectacg-character-render-timeline-profile
description: ProjectACG 当前工程的 CharacterRender Timeline 定制规则。记录具体 Controller、Track、Shader、PerObjectShadow 接口、作用域和验证边界，不可直接迁移到其他项目。
---

# ProjectACG CharacterRender Timeline Profile

## 1. 适用范围与迁移边界

本 Profile 仅适用于 ProjectACG 的 `Client` 工程，以当前 checkout 的代码与资产为准，不固定本机目录名。通用规则以 [../../README_Tech_TimelineDevelopmentRules.md](../../README_Tech_TimelineDevelopmentRules.md) 为准，通用模式以 [../../references/timeline-development-patterns.md](../../references/timeline-development-patterns.md) 为参考；本文件只补充 ProjectACG 的类型、路径、Shader 家族、PerObjectShadow 状态、优先级和已知限制。

迁移到其他项目时必须删除或重建本 Profile。不得把 `CharacterRenderController`、`CharacterRenderTimelineClip`、`Chara V2`、`BaseLit`、`PerObjectShadowFeature`、模式枚举、Shader 全局键或当前目录结构当成跨项目统一规范。其他项目只复用 CORE 中的作用域分析、基线恢复、协调器、Editor 事件链和验证方法，再按自己的渲染架构建立 Profile。

## 2. 当前 Source of Truth

| 职责 | 当前文件 |
| --- | --- |
| 场景角色渲染基线 | `Assets/GameScripts/AOT/GameArt/CharacterRender/CharacterRenderController.cs` |
| Timeline Clip 数据 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderTimelineClip.cs` |
| Render Settings 根轨与材质/渲染分层 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderSettingTrack.cs` |
| Track、Mixer、MPB 与共享请求协调 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderTimelineTrack.cs` |
| 绑定与渲染目标解析 | `Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderTimelineTargets.cs`，以及当前 `TimelineBindingResolver` 实现 |
| Clip Inspector、Orbit/Elevation 与 Scene Handle | `Assets/GameScripts/Editor/TimelineTrack/CharacterRenderTimelineClipEditor.cs` |
| 同类实时预览实现 | `Assets/GameScripts/Editor/TimelineTrack/CharacterMaterialTimelineClipEditor.cs`、`MountFollowClipInspector.cs`、`BoundMountFollowClipInspector.cs` |
| Chara V2 局部覆盖入口 | `Assets/Shader/Character/Chara_V2/Common/Chara_Globals_V2.hlsl` |
| 角色 POS 采样 | `Assets/Shader/Character/Chara_V2/Common/Func_Chara_PerObjectShadow_V2.hlsl`、`Assets/Shader/Character/Common/Func_Chara_PerObjectShadow.hlsl` |
| POS RenderFeature 与最终方向/强度解析 | `Assets/Shader/RenderFeature/PerObjectShadowV2/Script/PerObjectShadowFeature.cs` |

代码、序列化资产或 Renderer 配置与本 Profile 不一致时，以当前代码和资产为准，并将差异作为 Profile 修订候选；不要反向修改 CORE 迁就单项目实现。

## 3. PRJ-TML-01｜Controller 能力同步边界

CharacterRender Timeline 当前需要覆盖最新版 `CharacterRenderController` 的镜头级艺术功能：

- 阴影采样模式：`Off / UnityOnly / POSOnly / Both`；
- Clip 选择 `POSOnly/Both` 时的 POS RenderFeature 启用请求；
- 高质量 POS 阴影方向跟随 Clip 主光；
- Chara V2、BaseLit 与材质高光阴影作用强度；
- 环境光对场景 SH 的覆盖；
- `FollowCamera XZ`。

这里的“同步”是艺术功能语义同步，不是把 Controller 字段逐个复制到 Clip。每个模块仍须按 [TML-DOC-02](../../README_Tech_TimelineDevelopmentRules.md#tml-doc-02镜像已有-controller-时维护能力与作用域映射) 建立 Controller 基线、Clip 输入、Mixer 规则、最终消费者和恢复合同；新增 Controller 功能时先更新能力表，再决定 Timeline 是否需要暴露。

## 4. PRJ-TML-02｜状态作用域与写入边界

| 状态 | ProjectACG 作用域 | 写入方式 | 多 Clip/Mixer 决策 |
| --- | --- | --- | --- |
| 最终采样 `Off/UnityOnly/POSOnly/Both` | 绑定角色 | Renderer/材质槽 `MaterialPropertyBlock` | 同一 Mixer 内最高权重模式，模块权重负责恢复场景基线。 |
| Chara V2 POS 接收强度、高光阴影作用强度 | 绑定角色 | Timeline 专属 MPB 值与权重 | 同一 Mixer 内连续混合，可按角色隔离。 |
| BaseLit 场景接收强度 | 场景/相机共享 | Timeline 专属共享请求 | 所有有效 Mixer 中选择最高权重请求，使用稳定 tie-breaker。 |
| POS RenderFeature 启用 | 相机共享 | Timeline 专属启用权重 | 所有有效请求取最大权重。 |
| POS 投影方向 | 相机共享 | Timeline 专属协调器 | 最高权重请求；`PerObjectShadowVolume` 显式方向优先。 |

BaseLit 接收材质不属于绑定角色根节点，不能通过角色 MPB 实现完全隔离。多角色可拥有独立的 Chara V2/高光接收强度，但 POS 投影方向和 BaseLit 场景接收强度仍是共享状态；Inspector HelpBox、功能 README 和交付说明都必须暴露这一限制。

## 5. PRJ-TML-03｜材质与 Controller 基线

- Shader 以材质和 `CharacterRenderController` 的当前实际值为基线，Timeline 使用独立 `value + weight` 叠加；权重为零时必须精确回到基线，不能落到未初始化的 `0`。
- 不把材质参数快照或机械复制成 Clip 字段。只有艺术上需要显式覆盖的输入进入 Clip；材质级高光强度继续作为最终乘数，使不同材质保持自身差异。
- Clip 阴影强度使用中性默认值 `1`，Inspector 范围为 `0..1`，Mixer 消费时使用 `Mathf.Clamp01`，防止旧序列化资源、脚本写入或 YAML 越界。
- MPB 先保留其它系统已有内容，只更新 Timeline 专属属性；清理时只把 Timeline 权重归零，不清空整块 MPB，不实例化或修改 `sharedMaterial`。

## 6. PRJ-TML-04｜POS 可见性、优先级与协调器

场景 `CharacterRenderController` 为 `Off/UnityOnly` 时，会使相关 POS 接收强度处于零基线。只让 `PerObjectShadowFeature` 入队仍然看不到最终阴影，因此 Clip 选择 `POSOnly/Both` 时必须同时完成：

1. 登记 POS RenderFeature 启用请求；
2. 使用 Clip 自身非零阴影强度并按 Clip 权重渐入；
3. Chara V2 接收强度写入绑定角色 MPB；
4. BaseLit 接收强度登记为共享请求；
5. Feature 在原有启用判断中组合 Controller 与 Timeline 请求，不覆盖 Controller 的全局键。

协调器以 Mixer Owner 保存启用、方向和 BaseLit 请求。启用取最大权重；方向与 BaseLit 强度分别选择最高权重请求，同权重使用稳定登记顺序。方向优先级为 `PerObjectShadowVolume 显式方向 > Timeline 方向 > CharacterRenderController`；零方向表示跟随场景主平行光，非零方向写出前归一化。

Timeline 必须使用专属全局键。Clip 结束、绑定丢失、PlayableGraph 停止/销毁时移除 Owner 请求；静态集合在 `SubsystemRegistration` 清空，避免禁用 Domain Reload 后残留。

## 7. PRJ-TML-05｜CharacterRender Clip Editor

- Orbit/Elevation 圆盘和数值框分别执行变化检测，再统一写回同一方向 `SerializedProperty`；每个 `BeginChangeCheck` 必须与同层级 `EndChangeCheck` 成对。
- 左键按下获取 `GUIUtility.hotControl`，拖拽期间即使指针离开圆盘仍继续更新，抬起释放；右键重置消费事件并触发 `GUI.changed`。
- Scene Handle 以 Timeline 根 Track 的绑定对象为锚点，子 Layer Clip 必须能回溯根绑定；编辑后记录 Undo、应用序列化属性、标脏并刷新预览。
- 圆盘“视觉能动但值不保存”时，先检查变化检测是否错误嵌套、是否只结束数值框的内层检查，再排查几何命中区域。
- 参数刷新遵循 `TML-EDT-03`：该 Clip 的渲染参数由 Mixer 实时读取，Editor 通过 `IInspectorChangeHandler` 接管宿主默认刷新，用序列化写回结果记录变化并请求 `SceneNeedsUpdate`；Scene Handle 成功写回后显式复用该入口。具体证据、补丁和未验项见 [第 10 节](#10-render-settings-调参闪烁证据修复与工具)，迁移方法见 [预览刷新参考](../../references/timeline-preview-refresh-and-live-value-sync.md)。

## 8. ProjectACG 验证清单

- [ ] `AOT.csproj`、Runtime/主程序集与 `Assembly-CSharp-Editor.csproj` 编译通过，新脚本已进入正确程序集。
- [ ] Unity Timeline 中新旧 CharacterRender Clip 均可创建、编辑和播放，无 Missing Script 或黄色脚本告警。
- [ ] 材质/Controller 非零基线下，Clip 权重从 `0 -> 1 -> 0` 后精确恢复原值，没有默认归零或材质差异丢失。
- [ ] Controller 为 `Off/UnityOnly` 时，Clip 选择 `POSOnly/Both` 会同时启用 POS 并提供非零接收强度，在 Chara V2 与 BaseLit 上均实际可见。
- [ ] Chara V2、高光、BaseLit、环境 SH、主光方向、`FollowCamera XZ` 与最新 Controller 的艺术语义一致。
- [ ] 两个角色和多个重叠 Clip 下，局部强度保持隔离，共享方向/BaseLit 按最高权重与稳定 tie-breaker 仲裁。
- [ ] Orbit/Elevation 圆盘左键拖动、右键重置、数值输入、Scene Handle 和 Undo 都写回同一 Clip 字段。
- [ ] 含相机/动画轨时连续拖动 Render Settings 的颜色、方向、强度和开关，画面与效果不因普通调参反复闪回；暂停时无需移动播放头即可生效。
- [ ] 折叠/展开分组和显示 Handle 不触发内容图重建；Inspector、Scene Handle、Undo/Redo、独立 Inspector 的当前帧更新分别验证。
- [ ] 真正影响建图、实例或绑定的变更仍正确生效，旧目标/旧覆盖完成清理；CharacterRenderTimelineTrack 与 Render Settings 的渲染层分别验证。
- [ ] Seek、Pause、Stop、Graph 销毁、Domain Reload 关闭和重新播放不会留下 Timeline MPB 权重或静态共享请求。
- [ ] 使用 Frame Debugger/Profiler 确认 POS Pass、接收材质、方向、强度和多相机行为；C# 构建不能代替 Unity 播放与视觉验证。

## 9. 当前已知限制

- 角色局部 Chara V2/高光接收强度可以隔离；POS 投影方向、BaseLit 场景接收强度和相机 RenderFeature 仍属于共享状态。
- 材质/Shader 当前值作为基线意味着美术修改材质后 Timeline 会跟随新值，这是预期行为；需要固定镜头值时必须在 Clip 中显式覆盖，不能偷偷缓存旧材质快照。
- 未完成 Unity 播放、Frame Debugger、多角色和多相机验证时，只能声明“静态编译/文档合同已通过”，不能声明视觉功能已完整交付。

## 10. Render Settings 调参闪烁：证据、修复与工具

### 10.1 问题、环境与结论强度

现象是修改 Render Settings Track 上 CharacterRender Clip 的参数时，Timeline 预览画面或效果一会儿出现、一会儿消失。修复针对普通参数编辑引发的不必要整图重建：让现有图读取 Clip 新值并重新求值，保持原混合和清理合同。

| 项目 | 查证事实 |
| --- | --- |
| 记录日期 | 2026-10-20。 |
| 案例工作区 | `D:\work2025U3D\Valkyria\ProjectACGMain2\ProjectACG\Client`；命令在此 checkout 的 `Client` 根执行。 |
| Unity | `2022.3.62f3`，来自 `ProjectSettings/ProjectVersion.txt`。 |
| Timeline / URP | `1.7.7` / `14.0.12`，来自 `Packages/manifest.json`。 |
| 实际补丁 | `CharacterRenderTimelineClipEditor.cs` 的接口、变化标记、刷新回调及 Scene Handle 入口。 |
| 证据范围 | 已核对项目和包源码，已完成该 Editor 文件的匹配程序集编译；尚未在 Unity 中完成本场景的复现及修复后连续视觉回归。 |

下面的根因链是源码证据支持的修复依据。它没有排除相机切换、其他写入者或资源生命周期导致的额外闪烁，最终是否消除用户所见现象仍需 10.7 的 Unity 操作确认。

### 10.2 源码证据与诊断链

表中项目路径相对 `Client`；包路径相对 `Library/PackageCache/com.unity.timeline@1.7.7/`。行号用于本次定位，后续以类型和方法名为准。

| 入口/证据 | 本次观察 | 对修复的影响 |
| --- | --- | --- |
| `Editor/inspectors/ClipInspector/ClipInspector.cs`，约 639–674 行 | 宿主包住自定义 Inspector 检查 GUI 改动；实现接口则回调，否则默认 `Refresh(ContentsModified)`。 | 自定义 Editor 没有显式重建调用，也不能排除重建；要接管宿主分支。 |
| `Editor/inspectors/IInspectorChangeHandler.cs` | 接口注释说明默认 Inspector 变化会强制重建 PlayableGraph。 | 使用包提供的扩展点，而非修改包缓存。 |
| `Editor/TimelineEditor.cs`，`Refresh`，约 90 行 | `ContentsModified` 设置重建标志；`SceneNeedsUpdate` 请求 `Evaluate`，结构分支优先。 | 普通实时参数用后者；混合两个 reason 仍会重建。 |
| `Editor/Window/TimelineWindow.cs`，`RebuildGraphIfNecessary`，约 450 行 | 重建前可先 `previewMode = false`，再建图、恢复时间与求值。 | 整张 Timeline 的相机/动画/效果预览都可能受重建影响。 |
| `Editor/State/WindowState.cs`，`previewMode` setter，约 265–282 行 | 退出预览执行 Stop、`OnStopPreview`、`AnimationMode.StopAnimationMode`。 | 已记录临时状态有恢复再应用的窗口，支持闪回候选链。 |
| `CharacterRenderTimelineClip.cs`，`CreatePlayable`，约 103 行 | `playable.GetBehaviour().clip = this`。 | Behaviour 持有资产本身，不是仅在建图时复制所有参数。 |
| `CharacterRenderTimelineTrack.cs`，`CharacterRenderTimelineMixer.ProcessFrame` | 每次求值重置聚合状态，遍历有效输入，调用 `_blendState.Add(behaviour.clip, weight)` 后应用覆盖。 | 当前渲染参数已有 live 读取，不必再加冗余字段同步。 |
| 同一 Mixer 的 `OnGraphStop` / `OnPlayableDestroy` | 两者调用 `ClearOverrides()`。 | 清理是必要合同；处理不必要重建，保留 Stop/Destroy 恢复。 |
| `CharacterRenderSettingTrack.cs`，`GetLayerContent` / `CreateRenderMixer` | 同一根轨可分材质层与渲染层，渲染层创建 CharacterRender Mixer。 | 用户所见 Render Settings 的渲染 Clip 复用此 Inspector；原 CharacterRender Track 同样适用，分别待验。 |
| 相邻 Editor | 材质 Clip 已用接口与 `_changed`；Mount Follow 在建图输入变化与普通值变化间区分 reason。 | 优先采用仓库已有模式，避免另造预览管理器。 |

据此形成的候选链为：普通字段或 GUI 改动 → 宿主默认 `ContentsModified` → 请求整图重建 → 退出预览及 Mixer 清理 → 相机/动画/渲染覆盖重新应用。需要在 Unity 中确认该链与闪烁时刻一致。

### 10.3 最小实现与各编辑入口

改动位于 `Assets/GameScripts/Editor/TimelineTrack/CharacterRenderTimelineClipEditor.cs`，现有 `using UnityEditor.Timeline;` 提供接口及刷新 API。

1. 类声明由仅继承 `Editor` 改为 `Editor, IInspectorChangeHandler`。
2. 增加 `private bool _changed;`；Inspector 绘制结束时用 `_changed |= serializedObject.ApplyModifiedProperties();` 累积真实数据变化。
3. 在类中增加以下完整回调。它过滤仅折叠/显示状态的 GUI 改动，并替代宿主默认重建：

```csharp
public void OnPlayableAssetChangedInInspector()
{
    if (!_changed)
    {
        return;
    }

    _changed = false;
    // The mixer reads the clip live. Rebuilding the graph resets camera/FX preview state.
    TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
}
```

4. 在现有 `DrawSceneDirectionHandle` 检测到旋转变化的分支中复用刷新入口。下面是该分支内的代码；局部变量和参数沿用原方法，不是独立脚本：

```csharp
Undo.RecordObject(target, undoName);
directionProperty.vector3Value = -(newRotation * Vector3.forward);
_changed |= serializedObject.ApplyModifiedProperties();
// Scene handles are outside Timeline's inspector change callback.
OnPlayableAssetChangedInInspector();
EditorUtility.SetDirty(target);
directionRotation = newRotation;
```

这里保留原 Undo、方向符号、脏标记和手柄旋转缓存；新增的是变化累积与显式求值。Scene Handle 不经过宿主的 Inspector 绘制回调，只改 Inspector 路径会留下该入口的刷新缺口。

此次修复没有新增 Undo 事件订阅，也没有为独立/锁定 Inspector 增加专门的 Director 路由。它们是需分别验证的入口；确有刷新缺口时，再按 [通用参考的入口策略](../../references/timeline-preview-refresh-and-live-value-sync.md#35-覆盖-scene-handle独立-inspector-与-undoredo) 补齐，不能在文档中当成已经实现。

### 10.4 可复用的职责划分与最小改动原则

| 组件 | 本实现职责 | 后续参考 |
| --- | --- | --- |
| Track / Layer | 限制 Clip 类型、解析根绑定、创建对应 Mixer；Render Settings 每层按材质或渲染分类 | 分层不等于跨层自动混合；混入不兼容 Clip 要验证诊断行为。 |
| Clip | 序列化艺术输入，`CreatePlayable` 交出 Clip 引用 | 不为修 Editor 问题改字段名、默认值或旧资产脚本引用。 |
| Behaviour | 保存当前 Clip 引用 | 有 live 引用时不重复维护一份全部参数的快照。 |
| Mixer | 按权重聚合，解析目标，写出并清理本系统覆盖 | 恢复、作用域与共享仲裁分别遵循 `PRJ-TML-02` 至 `PRJ-TML-04`。 |
| MPB / 协调器 / Shader / RenderFeature | 消费局部值或共享请求，组合基线与覆盖 | Editor 不直接抢写共享材质或全局渲染键。 |
| Editor / Handle | 序列化编辑、Undo、变化分类、请求当前帧更新 | 保存、求值、重绘各司其职，不把普通拖参升级为整图结构变化。 |

本次问题已有运行时实时取值基础，因此修复集中在 Editor。将 Shader、Controller、材质或 `ClearOverrides()` 一并改动，会扩大验证范围，也难以判断到底是哪条链解决了问题。

以后给 Clip 加颜色、强度、方向、模式或开关时，先确认 `_blendState.Add` 及最终消费者已经读取新字段；只加 Inspector 不算接通功能。若新增 Prefab、一次性绑定或节点创建输入，重新分类刷新，不直接沿用本 Editor 的“全部轻量求值”假设。

### 10.5 问题、技巧与避免的误判

| 常见问题/误判 | 经验与处理方式 |
| --- | --- |
| 搜不到自定义 Editor 的重建调用，就认为不可能重建 | 查宿主 `ClipInspector` 默认分支和其他 Editor/工具入口。 |
| 折叠分组也可能触发宿主 GUI 变化 | 记录 `ApplyModifiedProperties` 的结果；`_changed |=` 在刷新消费前保留待处理变化。 |
| 仅调用 `SceneView.RepaintAll` | 只重绘，不保证 Mixer 重新消费 Clip；暂停时请求求值。 |
| 改成轻量刷新后字段不生效 | 检查字段是否只在 `CreatePlayable` 复制或解析，以及缓存失效是否完整。 |
| 关闭相机轨后不闪，就当成最终修复 | 这是诊断线索，恢复相机轨后继续验证刷新链。 |
| 为防消失而取消 `ClearOverrides` | 会破坏 Stop/销毁恢复；保留幂等清理，减少不必要触发。 |
| 用一次截图或编译成功证明不闪 | 连续操作录屏/观察才能验证时间上的稳定；编译只证明语法与 API。 |
| 单文件编译时引用旧 Editor 程序集 | 同名旧类型可能干扰结果；此案例排除 `Assembly-CSharp-Editor*`，其他项目仍需按依赖调整。 |
| 用默认编码重写中文文档或源码 | 使用 `apply_patch` 或 .NET 显式 UTF-8；不要通过不确定编码的 PowerShell 管道重写。 |
| 补丁恢复后记不清当前代码状态 | 先搜索类接口、`_changed`、回调与 Handle 调用，再看 Git diff；不要整文件覆盖用户并行修改。 |

### 10.6 工具与可复用命令

**源码定位和差异检查**。下列 PowerShell 命令在 `Client` 根执行，包缓存需已由该项目解析；版本升级后先调整包目录。

```powershell
rg -n 'IInspectorChangeHandler|OnPlayableAssetChangedInInspector|ApplyModifiedProperties|RefreshReason' Assets/GameScripts/Editor/TimelineTrack
rg -n 'GetBehaviour\(\).clip|_blendState.Add|OnGraphStop|OnPlayableDestroy|ClearOverrides' Assets/GameScripts/AOT/GameArt/TimelineTrack -g 'CharacterRender*.cs'
rg -n 'IInspectorChangeHandler|ContentsModified|SceneNeedsUpdate|rebuildGraph|RebuildGraphIfNecessary|StopAnimationMode' Library/PackageCache/com.unity.timeline@1.7.7/Editor
git diff -- Assets/GameScripts/Editor/TimelineTrack/CharacterRenderTimelineClipEditor.cs
git diff --check -- Assets/GameScripts/Editor/TimelineTrack/CharacterRenderTimelineClipEditor.cs
```

`rg` 用于找调用者、同类实现和最终消费者；`git diff --check` 仅检查差异空白等问题。若补丁已提交或相对 HEAD 无差异，diff 为空并不能证明回调存在，仍要核对源码。

**匹配 Unity 的单文件 Roslyn 检查**。以下脚本是本案例使用的局部 API/类型编译方式；需要 `dotnet`、对应 Unity 安装、已生成且匹配当前源码的 `Library/ScriptAssemblies`。路径按机器调整，产物仅落入系统临时目录，不写 Unity 生成目录。

```powershell
$ErrorActionPreference = 'Stop'
$unityData = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data'
$checkDirectory = Join-Path ([IO.Path]::GetTempPath()) ('character-render-preview-check-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($checkDirectory)
$referenceFiles = @(
    Get-ChildItem -LiteralPath (Join-Path $unityData 'NetStandard\ref\2.1.0') -Filter '*.dll'
    Get-ChildItem -LiteralPath (Join-Path $unityData 'NetStandard\compat\2.1.0\shims\netfx') -Filter '*.dll'
    Get-ChildItem -LiteralPath (Join-Path $unityData 'Managed') -Filter '*.dll'
    Get-ChildItem -LiteralPath (Join-Path $unityData 'Managed\UnityEngine') -Filter '*.dll'
    Get-ChildItem -LiteralPath 'Library\ScriptAssemblies' -Filter '*.dll' |
        Where-Object { $_.Name -notlike 'Assembly-CSharp-Editor*' }
)
$sourcePath = (Resolve-Path -LiteralPath 'Assets\GameScripts\Editor\TimelineTrack\CharacterRenderTimelineClipEditor.cs').Path
$responseLines = @(
    '/nologo', '/nostdlib+', '/target:library', '/langversion:9.0',
    '/define:UNITY_EDITOR;UNITY_2022_3_OR_NEWER',
    ('/out:"' + (Join-Path $checkDirectory 'CharacterRenderPreviewCheck.dll') + '"')
)
$responseLines += $referenceFiles | ForEach-Object { '/reference:"' + $_.FullName + '"' }
$responseLines += '"' + $sourcePath + '"'
$responsePath = Join-Path $checkDirectory 'compile.rsp'
[IO.File]::WriteAllLines($responsePath, [string[]]$responseLines, [Text.UTF8Encoding]::new($false))
& dotnet (Join-Path $unityData 'DotNetSdkRoslyn\csc.dll') ('@' + $responsePath)
if ($LASTEXITCODE -ne 0) { throw "Editor compile failed: $LASTEXITCODE" }
Write-Output (Join-Path $checkDirectory 'CharacterRenderPreviewCheck.dll')
```

响应文件避免命令行长度和路径空格问题；显式使用 Unity 自带编译器及该项目程序集，排除旧同名 Editor 类型。此方法不替代 Unity 全量重编译、程序集依赖重建、导入过程或视觉检查；目标脚本若依赖同程序集的其他源码，需调整输入而非盲目排除引用。

**Unity 行为与渲染工具**。用暂停改值、连续拖参、Undo/Redo 和生命周期操作验证刷新；用录屏比较时间上的闪回。用 Profiler 看是否仍反复重建/产生开销，用 Frame Debugger 查实际 Pass、材质与渲染结果；两者不能互相替代。用户已打开项目时使用现有 Editor，避免对同一工程再起 batch 实例或关闭用户会话。

### 10.7 验证记录、验收与回退

| 项目 | 当前状态 | 边界 |
| --- | --- | --- |
| 修复源码与宿主包调用链 | 已查证 | 证据支持以上路径，未完成运行时序观测。 |
| Unity 2022.3.62f3 自带 Roslyn + Unity/项目程序集单文件编译 | 已通过 | 只覆盖 `CharacterRenderTimelineClipEditor.cs` 的局部编译，不代表全工程构建。 |
| 本次 Editor 差异空白检查 | 已通过 | `git diff --check`，不代表行为正确。 |
| Unity 原场景复现与修复后无闪烁 | 待验证 | 不得写成视觉验收完成。 |
| Inspector/Scene Handle 暂停更新、纯 GUI、Undo/Redo、独立 Inspector | 待验证 | 本补丁未补 Undo 专用订阅和独立 Inspector 路由。 |
| 两类轨道、交叠、多角色/多相机、Clip 出界/Stop/Destroy | 待验证 | 沿用既有运行时逻辑，但仍需回归作用域与恢复。 |

建议在同一资产、同一相机与同一时间区间比较修复前后：先连续拖动强度/颜色/方向，再暂停改值和 Scene Handle，随后撤销重做、Seek、Stop、重播并离开 Clip。成功标准是参数立即生效、无编辑触发闪回，且停止后回到正确基线；最后再扩展到交叠和多实例。

若轻量刷新导致某个新字段不更新，先检查实时消费与缓存。必须重建的字段可恢复为对应结构刷新；不要把所有字段一并退回重建，也不要删除清理函数。完整回退时只逆向撤销本次 Editor 的接口、变化标记及刷新入口改动，保留原有其他编辑功能，并重新说明旧版预览重建限制。

迁移到其他功能时保留 [通用参考](../../references/timeline-preview-refresh-and-live-value-sync.md) 的调用链分析、字段分类、入口覆盖、最小修复与验证方法；本节版本、路径、类型和当前测试状态留在本 Profile，不能复制成新项目的已验证事实。
