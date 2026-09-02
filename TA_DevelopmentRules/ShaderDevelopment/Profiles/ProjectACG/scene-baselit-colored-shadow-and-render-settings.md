# ProjectACG BaseLit 彩色实时阴影与场景 RenderSettings Profile

> **Profile ID**：`projectacg-baselit-colored-shadow-and-render-settings-v1`
>
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACG\Client`
>
> **事实快照**：`2026-09-02`
>
> **用途**：记录本工程 `Valkyria/Scene/BaseLit` 的彩色实时阴影、Packed Mask PBR Slider、MixMap 默认读取和场景 RenderSettings 总控分析。本文是 ProjectACG `PROFILE`，不得上升为跨项目 CORE。通用实现模式见 [`../../references/scene-render-settings-and-colored-shadow.md`](../../references/scene-render-settings-and-colored-shadow.md)。

## 1. 工程基线与实现路径

| 项目 | 当前事实 |
| --- | --- |
| 工作区 | `D:\work2025U3D\Valkyria\ProjectACG\Client` |
| Unity | `2022.3.62f3` |
| URP | embedded `com.unity.render-pipelines.universal@14.0.12` |
| 目标 Shader | `Assets/Shader/Scene/New/PackedMaskPBR/BattleSceneBaseLit.shader` |
| Shader 名 | `Valkyria/Scene/BaseLit` |
| 材质数量 | 静态扫描到 `217` 个 `Valkyria/Scene/BaseLit` 材质引用 |
| 公共输入 HLSL | `Assets/Shader/Scene/New/PackedMaskPBR/PackedMaskPBRInput.hlsl` |
| 公共光照 HLSL | `Assets/Shader/Scene/New/PackedMaskPBR/PackedMaskPBRLighting.hlsl` |
| 彩色阴影 HLSL | `Assets/Shader/Scene/New/SceneRealtimeShadowColor.hlsl` |
| 灯光控制脚本 | `Assets/GameScripts/AOT/GameArt/SceneRealtimeShadowColor/SceneRealtimeShadowColorController.cs` |
| 脚本菜单 | `GameArt/Scene Realtime Shadow Color Controller` |
| 脚本 GUID | `6fd53220db48462bb27026fda8f85a7b`（从原目录移动后保留） |

当前代码方案不修改 URP Package，不增加额外 Pass、RT、纹理或 Shader 变体。Unity 实际编译和画面验证需在目标工程中执行，本 Profile 不把静态检查当作运行时证明。

## 2. 项目事实/现象

### 2.1 实时阴影颜色

`SceneRealtimeShadowColor.hlsl` 声明全局参数：

```hlsl
half4 _GlobalSceneRealtimeShadowColor;
```

`PackedMaskPBRLighting.hlsl` 的当前顺序为：

```text
GetMainLight → _ReceiveShadows → ApplyPerObjectShadowRaw
→ MixRealtimeAndBakedGI → 彩色阴影乘入 mainLight.color
→ mainLight.shadowAttenuation = 1 → BRDF
```

染色函数使用：

```hlsl
lerp(_GlobalSceneRealtimeShadowColor.rgb, 1.0h.xxx,
     saturate(mainLight.shadowAttenuation))
```

因此黑色默认值保持 URP 原始阴影外观；颜色只作用于主灯直接光，不直接修改 `bakedGI`、Lightmap 或附加灯。

### 2.2 控制器行为

`SceneRealtimeShadowColorController` 当前为 `[ExecuteAlways]`、`[DisallowMultipleComponent]`、`[RequireComponent(typeof(Light))]`，事件驱动写入全局颜色，没有 `Update()`。启用控制器优先匹配 `RenderSettings.sun` 对应的 Light；没有匹配时使用活动列表中最后的 fallback。禁用或销毁后自动切换其他控制器，没有有效控制器则恢复黑色。

这是单场景主灯的 MVP，不是多相机隔离方案。全局 Shader 参数仍是进程级状态，预览/展示相机若需要不同阴影色必须另行隔离。

**使用方式**：选中场景主方向光，在 Inspector 的组件菜单中选择 `GameArt/Scene Realtime Shadow Color Controller`；确认该灯被设置为 `RenderSettings.sun`，然后在 `Shadow Color` 中调节颜色。黑色为原始 URP 阴影，白色为无额外染色基线，蓝/紫/暖色用于场景风格化。脚本为 `[ExecuteAlways]`，编辑器和运行时都会应用；离开场景或禁用组件后会按 fallback 规则切换或恢复黑色。

### 2.3 Packed Mask PBR Slider

`BattleSceneBaseLit.shader` 当前属性为：

```shader
_MixMap("Mix Map", 2D) = "white" {}
_OcclusionStrength("B AO Strength", Range(0, 2)) = 1
_MetallicBrightness("Metallic Brightness", Range(-2, 2)) = 0
_MetallicContrast("Metallic Contrast", Range(0, 3)) = 1
_RoughnessBrightness("Roughness Brightness", Range(0, 4)) = 0
_RoughnessContrast("Roughness Contrast", Range(-2, 4)) = 1
```

扫描到的存量材质值与新范围如下：

| 参数 | 存量实际范围 | 当前 Slider 范围 |
| --- | ---: | ---: |
| `_MetallicBrightness` | `-1.24～1` | `-2～2` |
| `_MetallicContrast` | `0～2` | `0～3` |
| `_RoughnessBrightness` | `0～3` | `0～4` |
| `_RoughnessContrast` | `-2～3` | `-2～4` |

范围覆盖现有序列化值，未批量改写 `217` 个材质。需注意工程中“Brightness”当前是乘法缩放而非真正加法偏移；负值经过 `max(..., 0)` 后通常等价于零。`_RoughnessContrast` 已存在负值，若实现内部使用幂运算，零输入/负幂可能产生异常或饱和，不能在本次兼容改动中擅自夹成正数。

### 2.4 MixMap 开关移除

旧 `_UseMixTextures` 已从 Shader 属性、GUI 文案和 HLSL 分支中移除；现在始终读取 `_MixMap`，没有绑定纹理的材质读取默认白图。

静态扫描事实：

- `211` 个材质含显式 `_UseMixTextures` 序列化值；
- 其中 `29` 个值为 `0`，`182` 个值为 `1`；
- `3` 个值为 `0` 的材质实际仍绑定了 MixMap，移除开关后会开始读取该纹理；
- 其余旧关闭材质没有 MixMap，读取默认白图，数学结果与原关闭分支一致；
- `217` 个材质未被批量重写。

该变化是有意的行为兼容例外：旧关闭但绑定纹理的 3 个材质会发生视觉变化，需在美术验收时单独确认。

## 3. 根因与实现约束

### PACG-SCN-01｜阴影色必须在 GI 合并后进入主灯直接光

如果在 `MixRealtimeAndBakedGI` 之前修改 `shadowAttenuation` 或 `bakedGI`，会把实时阴影色带入烘焙 GI，造成 Lightmap/环境光被重复染色。当前约束是保留 Unity/POS 合并后的标量阴影，再把 `lerp(tint, white, attenuation)` 乘入 `mainLight.color`，并将 `shadowAttenuation` 置 `1` 防止后续重复衰减。

### PACG-SCN-02｜保持 BaseLit 与 POS 枚举语义

BaseLit 的 `_UsePerObjectShadow` 编码为 `UnityOnly=0`、`Both=1`、`POSOnly=2`、`Off=3`；角色 Shader 的同名枚举不同，不能复制。彩色阴影必须覆盖已有 Unity 主光与 POS 合并结果，但不能改变 `_ReceiveShadows` 和 POS 生产/消费时序。

### PACG-SCN-03｜全局状态所有权必须收敛

当前工程已有多个渲染状态写入者，不能让新的总控与旧组件同时写同一字段。目标长期结构为 `SceneRenderSettingsController`、`SceneRenderSettingsProfile`、`SceneRenderSettingsRuntime`：Profile 负责数据，Controller 负责场景引用和 Apply/Restore，Runtime 负责 Active Scene + Priority、Additive 切换和唯一 owner。旧 `SceneRealtimeShadowColorController` 在总控落地后应改为请求/适配层或移除其全局写入，不能与总控并存争抢。

## 4. 当前工程其他渲染状态写入者

以下是本次分析确认的真实路径；它们不是新总控的已实现部分：

| 系统 | 当前路径/事实 | 边界 |
| --- | --- | --- |
| 场景天空盒与环境光 | `Assets/Shader/Scene/StylizedSky/ProjectACGStylizedSkyController.cs` 可写 `RenderSettings.skybox`、`sun`、三向环境色和强度，并在禁用时恢复。 | 仍是独立控制器，未接入统一 Profile。 |
| 战斗/演出环境切换 | `Assets/GameScripts/HotFix/GameLogic/Battle/Presentation/BattleScenePresentationEnvironmentSwitcher.cs` 及其边界适配器。 | 应最终改为向总控请求 Profile/Variant，避免直接改全局状态。 |
| Volume 雾 | `Assets/Shader/PostProcess/MergedHeightFog/Scripts/HeightFogVolumeComponent.cs` 和 `Assets/Shader/PostProcess/TriLayerOasisHeightFog/Scripts/TriLayerOasisHeightFogVolumeComponent.cs`；部分配置可读取 `RenderSettings.sun` 方向。 | Volume 参数和混合仍由 Volume 系统拥有。 |
| Timeline Volume | `Assets/GameScripts/AOT/GameArt/TimelineTrack/TimelineVolumeRuntime.cs`。 | 负责 Timeline 驱动的 Volume 状态保存、混合和恢复，不应与场景总控争抢 Volume 所有权。 |
| 阴影距离 | `Assets/Renders/Graphics/Features/ShadowDistanceVolumeFeature.cs`。 | RendererFeature/相机级资源，不应复制为材质总控字段。 |
| 角色主灯 | `Assets/GameScripts/AOT/GameArt/CharacterMainlight/CharacterMainLightController.cs` 及其调用方。 | 角色展示/局部目标的光照职责，不应直接并入场景主灯总控。 |
| 预览天空盒 | `SettlementRolePreviewPresenter`、`StarGateTowerSettlementPage` 等会临时写 `RenderSettings.skybox` 并调用 `DynamicGI.UpdateEnvironment()`，离开时恢复。 | 需要通过隔离 Scope 或总控请求管理，避免污染游戏场景。 |

## 5. 总控设计建议（尚未实现）

建议 Profile 至少覆盖：`RenderSettings.skybox`、Ambient/Reflection 设置、场景主灯引用与颜色/强度、`_GlobalSceneRealtimeShadowColor`、内置 Fog 和默认 VolumeProfile 引用。不要把 Volume 内所有具体后处理参数、角色特有参数、URP Asset/RendererData/RendererFeature 共享配置复制进 Profile；这些对象应通过独立系统和明确请求接入。

Additive 场景不可按启用顺序选 owner，应使用 Active Scene 与 Priority。Timeline 继续拥有 Volume 混合和时间采样；环境切换只提交 Profile/Variant 请求。天空盒/环境光变化时才调用 `DynamicGI.UpdateEnvironment()`。

该设计目前仅为架构建议；工程中尚未创建 `SceneRenderSettingsController`、`SceneRenderSettingsProfile` 或 `SceneRenderSettingsRuntime`，也没有完成旧写入者迁移。

## 6. 验证证据与当前状态

### 已完成的静态证据

- 目标 Shader 属性、HLSL Include 和控制器路径已读取并核对；
- `217` 个 BaseLit 材质、旧 MixMap 开关值和 Slider 存量范围已扫描；
- 已确认彩色阴影 HLSL 只增加 `half3 lerp` 与一次主灯颜色乘法，不增加采样/Pass/Keyword；
- 已确认 Meta Pass/Lightmap 不消费 `_GlobalSceneRealtimeShadowColor`；
- 已确认场景中全局 RenderSettings、Volume、RendererFeature、角色主灯和预览 Scope 的真实写入路径。

### 未验证项

- 本快照没有 Unity Editor Shader 编译、运行时画面、Frame Debugger、RenderDoc、设备 GPU 性能或最终构建变体证明；
- 需要在目标场景覆盖 UnityOnly/Both/POSOnly/Off、`_ReceiveShadows`、Lightmap/Shadowmask、Additive/Timeline、多相机和控制器启停；
- 需要对旧开关为 `0` 但绑定 MixMap 的 3 个材质做视觉验收；
- 需要实测负 `_RoughnessContrast` 在目标 GPU/输入为零时的行为；
- Scene RenderSettings 总控仍未实现，所有优先级、恢复和跨相机隔离结论属于设计约束，不是已合入功能。

## 7. 回退边界

- 将 `_GlobalSceneRealtimeShadowColor` 恢复为黑色可回到原始标量阴影外观；
- 若全局状态污染，应停用控制器并恢复进入场景前保存的 `RenderSettings`/Volume 状态；
- Slider 属性名和序列化值未批量迁移，恢复原 `Range` 声明或旧浮点 Drawer 即可回退编辑器表现；
- `_UseMixTextures` 已删除，回退必须同时恢复属性、GUI、HLSL 分支和旧材质行为，不能只新增一个 Inspector 开关；
- 总控建议在实现前先以单场景 Profile + 明确 Apply/Restore 做 Harness，验证通过后再迁移旧环境切换器和预览 Scope。
