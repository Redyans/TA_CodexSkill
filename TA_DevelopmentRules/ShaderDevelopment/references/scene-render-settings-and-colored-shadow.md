# 场景 RenderSettings 总控与实时彩色阴影参考

> **类型**：REFERENCE；**适用范围**：Unity/URP 场景光照、实时阴影着色和场景级渲染状态；**使用前提**：必须以目标项目的 URP 版本、Shader 输入、Renderer 时序和场景生命周期重新验证。

本文总结一类常见场景需求：实时阴影需要可调色，材质的打包 `MixMap` 参数需要从自由浮点改成可控 Slider，同时希望把天空盒、环境光、主灯和全局 Shader 参数逐步收敛到一个场景 RenderSettings 总控。文中给出的是可迁移的实现和排查方法，不替代项目 Profile，也不要求修改 URP Package。

## 1. 适用场景与停止条件

适用于：

- 只改变实时主方向光阴影区域的色相/明度，而不改变已经烘焙到 Lightmap 的阴影颜色；
- 需要同时兼容 Unity 主光阴影、屏幕空间自阴影或其他已经合并到 `shadowAttenuation` 的阴影来源；
- 需要把分散的 `RenderSettings`、全局 Shader 参数和场景灯光控制逐步迁移为可恢复、可排序的场景配置；
- 需要在不增加 Shader Keyword、额外 Pass、全屏 RT 或阴影贴图采样的前提下完成 MVP。

以下情况应停止套用本文方案并重新设计：

- 需求是修改 Lightmap/Shadowmask 中已经烘焙的阴影色；
- 阴影来自屏幕空间后处理、透明投影、贴花或自定义 RT，无法在主灯 `Light` 结构中得到统一衰减值；
- 多相机需要互不影响，但实现只能写全局 Shader 参数；
- 需要对附加灯、角色专用光照或每个物体使用不同阴影色；
- 目标平台不支持当前 Shader 的精度、变体或全局参数链路。

## 2. 前提与依赖

在写代码前建立以下事实表：

| 项目 | 需要确认 |
| --- | --- |
| 阴影标量 | 主灯 `shadowAttenuation` 在哪里由 Unity 阴影、`_ReceiveShadows`、POS/屏幕空间阴影合并。 |
| GI 时序 | `MixRealtimeAndBakedGI` 是否会读取/修改主灯的阴影信息；烘焙 GI 与直接光是否分开。 |
| 颜色域 | 项目是否为 Linear；全局颜色是线性值还是显示/Gamma 值。 |
| 全局状态 | 谁写入 `Shader.SetGlobalColor`、`RenderSettings.sun`、天空盒、环境光和 `DynamicGI.UpdateEnvironment()`。 |
| 生命周期 | 场景加载、Additive Scene、Active Scene、禁用/销毁、Domain Reload、多相机和 Timeline 的恢复语义。 |
| 材质接口 | 属性名、默认值、序列化旧值、ShaderGUI/Drawer、动画曲线和脚本写入。 |

## 3. 实现方式

### 3.1 实时阴影颜色：染色衰减，不改阴影生产

将阴影衰减 `a`（`0` 表示全阴影，`1` 表示无阴影）和场景阴影色 `T` 分开。推荐的直接光颜色因子为：

```hlsl
half3 shadowTintFactor = lerp(T, 1.0h.xxx, saturate(a));
mainLight.color *= shadowTintFactor;
```

语义是：

- `T = black`：完整阴影仍为黑色，结果与 URP 原有标量阴影一致；
- `T = white`：阴影不额外染色，适合 A/B 对照；
- `T` 为蓝/紫/暖色：只有衰减后的阴影区域向该颜色过渡，受光区域保持主灯原色。

不要把 `T` 直接乘到整个场景颜色或 `bakedGI`；那会把受光区域、环境光和 Lightmap 一起染色。

### 3.2 插入点：完成实时/烘焙合并后再染色

推荐时序：

```text
GetMainLight(...)
  → 应用 _ReceiveShadows
  → 应用 POS/屏幕空间阴影
  → MixRealtimeAndBakedGI(...)
  → mainLight.color *= shadowTintFactor
  → mainLight.shadowAttenuation = 1
  → 进入 BRDF/直接光计算
```

把染色放在 `MixRealtimeAndBakedGI` 之后，可以保留烘焙 GI 的原始标量语义，同时只影响主灯直接光。将 `shadowAttenuation` 设为 `1` 是为了防止后续通用 BRDF 再次把标量阴影乘一次；如果项目的光照入口不会重复消费该字段，则必须以源码确认后再决定是否清零。

### 3.3 公共 HLSL 与参数所有权

跨多个场景 Shader 复用时，把计算函数和全局变量放在公共 HLSL：

```hlsl
half4 _GlobalSceneRealtimeShadowColor;

inline half3 ResolveSceneRealtimeShadow(half attenuation)
{
    return lerp(_GlobalSceneRealtimeShadowColor.rgb, 1.0h.xxx,
                saturate(attenuation));
}
```

脚本只负责将配置写入全局参数，不在每个材质上复制颜色。全局参数必须有唯一场景级 owner；多个组件同时写同一个 ID 会产生“最后一次写入”竞态，不能把执行顺序当成稳定优先级。

### 3.4 Light 组件 MVP

最小可用控制器可以挂在场景主灯上：

1. `[RequireComponent(typeof(Light))]`，颜色字段遵循项目颜色空间，并确认写入 HLSL 的值域为线性 RGB；
2. 在 `OnEnable`、`OnDisable`、`OnDestroy`、`OnValidate` 和动画属性变化时更新；
3. 优先选择 `RenderSettings.sun` 对应的控制器，找不到时按明确的 fallback 规则选择一个；
4. 没有有效控制器时写回黑色，使默认行为可回退；
5. 不使用每帧 `Update()`，避免无变化时重复设置全局参数。

该 MVP 适合单场景、单主灯和同一相机族。它不是多场景全局状态管理器。

### 3.5 长期方案：Scene RenderSettings 总控

当后续需要控制天空盒、环境光、主灯、雾、默认 Volume 或其他场景级渲染状态时，建议拆成三层：

```text
SceneRenderSettingsProfile   纯数据资产：天空盒、环境光、主灯引用、阴影色、雾/Volume 默认值
SceneRenderSettingsController 场景对象：引用 Profile、生命周期、Apply/Restore、编辑器入口
SceneRenderSettingsRuntime    运行时所有权：Active Scene、Priority、Additive 切换、相机/Timeline 请求、恢复
```

推荐职责边界：

- **Profile** 只保存可序列化配置和版本，不持有运行时缓存；
- **Controller** 负责场景对象引用、显式应用和离开场景后的恢复；
- **Runtime** 负责唯一 owner、优先级、场景切换和请求合并；
- `DynamicGI.UpdateEnvironment()` 只在天空盒或环境光确实变化时调用，不要每帧调用；
- Additive 场景使用 Active Scene + Priority 决定 owner，不能以 `OnEnable` 顺序决定；
- Battle 环境切换器应请求 Profile/Variant，Timeline Volume 继续拥有自己的 Volume 混合规则。

不应把以下内容整体复制进总控：Volume 内每个后处理参数、角色专用光照、RendererFeature/URP Asset 共享配置。它们有独立的消费者、相机范围和生命周期，应通过引用或请求接口接入。

### 3.6 烘焙阴影颜色的正确控制点

实时阴影色不会回写到 Lightmap。烘焙区域的色相/明度主要由以下输入共同决定：

- Environment Lighting（Skybox、Ambient Color/Intensity）；
- 烘焙主灯颜色/强度、`Indirect Multiplier` 和 Baked 补光灯；
- 材质 Albedo、Emission 与 Meta Pass 输出；
- Baked AO（主要改变明暗，不直接决定色相）；
- Mixed Lighting 模式、Shadowmask 和 Lightmap 编码。

需要调整烘焙阴影时，应修改这些烘焙输入后重新 Bake 并检查 Lightmap/Shadowmask；不建议直接后处理 Lightmap 贴图，因为 Directional Lightmap、压缩和 Mixed Lighting 数据之间存在耦合。

### 3.7 Float → Range Slider 的兼容迁移

把已有浮点属性改成 `Range(min,max)` 时，先扫描所有材质、Prefab、Animation、Timeline 和脚本写入，统计旧值范围，再选择能覆盖存量值的 Slider 范围。不要为了“好看”直接夹断旧值；若必须改变范围，应另起迁移任务并提供资产回退。

属性名、默认值和序列化类型保持不变时，已有材质无需批量重写。注意“Brightness”实际是乘法缩放还是加法偏移，名称不能替代数学语义；对比度负值、零输入和幂运算要单独做数值边界测试。

### 3.8 移除 MixMap 开关并固定读取纹理

当产品决定默认读取 MixMap 时：

1. 删除 Shader `Properties` 中的开关和对应 GUI 文案；
2. 删除 HLSL 中只服务该开关的分支函数/Keyword；
3. 无绑定纹理的旧材质读取默认白图，并确认数学结果等价于旧关闭分支；
4. 对旧开关为关闭但实际绑定纹理的材质单独列出影响清单；
5. 不批量改写材质，除非需求明确要求资产迁移；
6. 重新扫描残留属性、Keyword、ShaderGUI、动画和脚本引用。

## 4. 风险与不适用边界

- 全局 Shader 参数是进程级状态，不能天然按相机隔离；预览相机、角色展示相机和多相机需要隔离 Scope 或独立材质参数。
- 只在主灯直接光上染色不会改变附加灯、角色专用光、透明投影、后处理或 Lightmap；这是边界而不是遗漏。
- Linear 工程中的颜色字段应按线性值处理，不能把 Inspector 显示的 Gamma 颜色直接当作 HLSL 输入。
- 全局 owner、天空盒切换、Timeline Volume、Additive Scene 若没有明确优先级和恢复协议，会出现“场景顺序相关”或离场污染。
- 负对比度、负幂、零输入和 `half` 精度可能导致饱和、NaN 或平台差异；兼容迁移阶段不要擅自改变旧数学。
- `lerp` 不会自动减少两端计算；本方案的性能优势来自不增加采样、Pass 和变体，而不是“无分支”口号。

## 5. 验证与回退

### 5.1 静态验证

- 检查 Shader `Properties`、CBUFFER、HLSL 声明/Include、ShaderGUI、材质、Prefab、Animation、Timeline 和脚本引用一致；
- 确认只保留一个全局参数 owner，禁用/销毁时能恢复默认值或切换到明确 fallback；
- 重新扫描旧 MixMap 开关、Keyword、函数名和序列化属性；
- 检查 Markdown UTF-8、相对链接、`.meta` GUID 和目标文件尾随空白。

### 5.2 Unity/运行时验证

使用固定场景、相机、时间点、质量档和曝光做 A/B：

1. 黑色阴影色与未改实现一致；白色阴影色可作为无染色基线；蓝/暖色只出现在阴影区域；
2. 覆盖 UnityOnly、POSOnly、Both、Off（若项目存在）以及 `_ReceiveShadows`；
3. 检查 GI、Lightmap、Shadowmask、Fog、附加灯、高频法线和高光没有意外染色；
4. Frame Debugger 确认阴影生产、`MixRealtimeAndBakedGI`、直接光消费和后续 Pass 顺序；
5. 覆盖直接进场景、Additive 切换、Timeline/环境变体、预览相机、多相机和禁用/销毁控制器；
6. 在目标 API/设备对比 Shader 编译、ALU、变体、GPU 时间和无效重复写入。

### 5.3 回退策略

- 将全局阴影色写回黑色即可恢复原有标量阴影外观；
- 若全局状态污染，先停用 Controller/Runtime，再恢复进入场景前保存的 `RenderSettings` 和 Volume 状态；
- Slider 迁移保留原属性名和材质序列化值，可通过恢复 Shader 声明回退；
- 移除 MixMap 开关属于接口删除，回退需要恢复属性、分支和旧材质行为，不能只恢复 Inspector 文案。
