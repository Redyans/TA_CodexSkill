<!-- BEGIN automation:ta date:2026-11-16 layer:PROFILE -->
# ProjectACG Shader 与 UI Dual Kawase Profile｜2026-11-16

> 类型：PROFILE；项目：ProjectACG；只记录指定会话中的实现报告、问题和验证边界，本次未读取项目源码或测试产物。
> 关联参考：[Shader 坐标空间、内部径向效果与 UI 模糊链](../../../references/CodexDaily/Development_Reference_2026-11-16.md)；模块入口：[ShaderDevelopment](../../../README_Tech_URPShaderDevelopmentRules.md)；沿用 `DOC-03`、`CMP-01`、`VAL-03`。

## 1. 项目事实与现象

### 1.1 独立 UI 模糊 Shader 需求

- 会话 `01a1c5c9-7942-7061-9a27-699e9e25fa9e` 中，用户要求 UI 双重模糊 Shader、接入 GammaUI、适配图集、可调模糊程度并注意性能。
- 会话中途把范围收窄为“单独 Shader，不要实现组件”，随后提出“`Kawase` 换一下，实现这个魔术”。在该会话的当日消息窗口内没有进一步定义“魔术”的算法，也没有实现结果或验证记录；不能把它等同于另一会话中完成的 Dual Kawase 方案。

### 1.2 UI 图标 Dual Kawase

- 另一会话 `01a1e275-b375-7483-8842-f85daa399b89` 报告完成 `UIDualBlurRendererFeature`、处理 Shader、`GammaUIDualBlurIcon.shader`、默认材质、`UIDualBlurIcon.cs` 和 `URP-UI-Renderer.asset` 接入。
- 会话报告该 Feature 位于 GammaUI 合成之后；有活动图标时执行 Source Draw、Dual Kawase 下采样/上采样和 Composite。`UIDualBlurIcon` 通过 `IMaterialModifier` 按需替换为模糊材质；关闭或强度为零时返回原 GammaUI 材质并跳过模糊链。
- 组件上的 `BlurStrength`、`BlurTint`、图集 UV Rect 可由 AnimationClip 驱动；`OnDidApplyAnimationProperties()` 负责刷新 UI 材质。`BlurStrength` 表示该图标的模糊贡献，实际扩散半径仍由共享 Feature 的 `spread`/`iterations` 控制。
- 会话把 `UI Dual Blur Source (Icon Draw)` 与 `UI Dual Blur Filter + Composite` 解释为不同处理阶段。`1727×971` 是用于最终合成的全分辨率相机颜色副本；滤波目标报告为 `432×243`、`216×121` 等低分辨率 RT。优化时删除了额外中间 Blur Source RT，并将默认 `iterations` 从 3 降至 2，减少一张 RT 和一次全屏采样。
- 会话估算 `1727×971` RT 约占 RGBA8 `6.4 MB` 或 RGBA16F `12.8 MB`；这是按尺寸/格式推算的容量，不是设备实测带宽或帧时间。GammaUI 合成链本身也有全分辨率副本。

### 1.3 FlowMap 晶体内发光

- 会话报告将 Monst_V2 的晶体内发光接入 FlowMap，默认关闭，中心/半径使用 Object Space；使用三层 Legacy Sine UV 噪声，可选 UV0–UV3，以遮罩 G 控制区域，并继续受顶点 RGB 与最终透明度影响；它与普通 HDR 发光开关独立。
- 会话报告 Shader/ShaderGUI/预设说明已同步。隔离 Unity 2022.3.62f3、URP 14.0.12、D3D11 检查为 1251 个编译组合、279 项功能/像素检查、3 项 Inspector 检查、0 FAIL。因既有 BaseLit 参考图为空，直接像素对照跳过；真实角色场景与 Android 未验。

### 1.4 BaseLitGalaxy 内部球形发光

- 初始实现以表面点到 Object Space 中心的距离求衰减。用户在球体/圆柱体上观察不到中心向外扩散的球形光斑；会话给出的根因是球面各点到球心距离近似相同，因此该算法只形成近似均匀的表面亮度。
- 后续改为沿观察方向进入模型并计算视线到内部 Object Space 球心的最近距离，再按半径与四次曲线衰减；用户要求去掉斑驳噪声，最终报告保留平滑球形衰减，不保留 ObjectSpace/ScreenUV 噪声开关。
- 会话报告 94 个 D3D11 编译变体通过。用户要求自行验收目标模型画面；会话没有提供该视觉验收结果。该方法被明确描述为表面投影近似，不是真实体积透射。

### 1.5 主线探索据点 Lambert 与外描边 Shader

- 会话 `01a1e293-676a-7c00-8a16-868eb54acef8` 先讨论主线探索据点的选中轮廓；用户随后把实现范围明确为专用 Shader，不改选中脚本或 RenderFeature。Shader 报告路径为 `Assets/Shader/Scene/New/MainStoryExploration/MSEBuildingLambertOutline.shader`，材质为 `Assets/AssetRaw/Scene/map_MainStoryExploration/mat/mat_MSE_buildings.mat`。
- 用户要求普通 Lambert、自定义环境光颜色/强度、`lerp` 混合、独立描边 Pass、描边开关且只绘制外轮廓。会话最终报告称保留 Lambert 与自定义环境光颜色/强度；描边以 `LightMode = "Outline"` Pass 实现，`_OutlineEnabled` 控制开关，沿用 Chara_V2 八面体顶点法线并只绘外轮廓。最终摘要没有列出 `lerp` 的精确输入、公式或参数标识。
- 首轮实现使用 `_LightDirectionOS`，用户随后要求世界空间光照方向；最终改为 `_LightDirectionWS`，Forward 法线用 `TransformObjectToWorldNormal` 转到世界空间后计算 Lambert，并同步材质字段。模型旋转时受光面随之变化。
- 会话报告现有 Render Objects Feature 已使用 `passTag: Outline`；只需模型 Layer 在其 LayerMask 中。移除主光、附加光、阴影接收与 `ShadowCaster`；`DepthOnly` 只用于深度。会话未报告修改脚本或 RenderFeature，也没有生产场景画面验收。

### 1.5 MSEBuildingLambertOutline

- 会话报告新 Shader 采用 `_LightDirectionWS`，将法线通过 `TransformObjectToWorldNormal` 转到世界空间后计算 Lambert；模型旋转会改变受光面，光照方向固定在世界中。
- 最终报告称已移除主光、附加光、阴影接收和 `ShadowCaster`，保留 Lambert、环境光参数与描边；隔离 Unity 2022.3.62f3 导入未报告 Shader 编译错误、警告。目标场景的光照观感没有实际验收报告。

## 2. 根因、修正与取舍

- 内部光斑的根因是表面距离场无法表达“从模型内部看见的球形光源”；修正为视线到内部球心的最近距离投影。适用前提是接受表面近似，不代表体积渲染。
- Lambert 光源从 Object Space 改为 World Space 后，方向参数与法线变换同步改名/改写；不能只改一个坐标输入。
- UI Blur 的 `1727×971` RT 属于避免相机颜色读写冲突的全分辨率合成副本；实际模糊目标更小。减少中间 RT 与滤波级数降低开销，但没有证据证明目标设备带宽或帧时间达标。
- 图标强度、模糊半径与整条 Blur 链开关是不同语义；强度为零应显示清晰图标，而非消失。逐图标可动画参数和共享 Feature 半径也不是同一控制层。

## 3. 会话报告的验证证据

| 项目 | 会话报告 | 本 Profile 的证据界定 |
| --- | --- | --- |
| UI Dual Kawase | C# 构建 0 警告/错误；两个 Shader D3D11 编译 `ok=1`；ProjectACG harness、UI governance 通过。 | 会话报告；GameView、复杂 Canvas/Mask 组合和移动设备 GPU 未验。 |
| UI 模糊性能 | 低分辨率 RT 尺寸、全分辨率副本尺寸及内存估算已记录。 | 尺寸与内存为会话中的计算/报告；没有 GPU 时间、带宽或发热测量。 |
| FlowMap 内发光 | 1251 编译组合、279 功能/像素检查、3 Inspector 检查、0 FAIL。 | 会话报告；BaseLit 空基线导致一项直接对照跳过，生产角色/Android 未验。 |
| BaseLitGalaxy | 94 个 D3D11 变体通过。 | 会话报告；用户目标模型视觉验收未回报。 |
| MSEBuildingLambertOutline | Unity 2022.3.62f3、D3D11 隔离导入无 Shader 编译错误；静态检查、材质引用检查和 `git diff --check` 报告通过。 | 会话报告；生产场景明暗、描边宽度与颜色未验。 |
| Shader-only “魔术”模糊 | 只有需求方向调整。 | 没有算法定义、代码实现或验证结果。 |

## 4. 未验证项与候选

- UI Blur 需在目标 GameView 验证清晰/模糊贡献、材质回退、图集边界、Mask、动画驱动和排序；使用 Frame Debugger/Profiler 及目标 Android 设备测量 GPU 时间与内存带宽。
- `BlurStrength = 0`、`Intensity = 0`、无活动图标时，应分别确认图标仍可见、Feature 是否跳过昂贵链路，以及每帧条件判断成本；会话报告未给出实机数据。
- BaseLitGalaxy 的球形投影需由用户在目标模型观察不同相机角度、模型形状和球心位置；当前只有编译报告。
- MSEBuildingLambertOutline 需在实际场景验证世界光方向旋转行为、环境光、深度和描边；隔离导入不能代替视觉验收。
- 未形成可升级 CORE 的新规则候选。上述实现依赖 ProjectACG 的 GammaUI、Renderer、材质和目标模型，继续留在 Profile，并引用 `VAL-03`。

## 来源会话

- [01a1c5c9-7942-7061-9a27-699e9e25fa9e](../../../../../VA_Worlk_memory/sessions/01a1c5c9-7942-7061-9a27-699e9e25fa9e.md)
- [01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe](../../../../../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md)
- [01a1d320-8c76-7d73-b99f-fd89b5b32b66](../../../../../VA_Worlk_memory/sessions/01a1d320-8c76-7d73-b99f-fd89b5b32b66.md)
- [01a1e293-676a-7c00-8a16-868eb54acef8](../../../../../VA_Worlk_memory/sessions/01a1e293-676a-7c00-8a16-868eb54acef8.md)
- [01a1e275-b375-7483-8842-f85daa399b89](../../../../../VA_Worlk_memory/sessions/01a1e275-b375-7483-8842-f85daa399b89.md)

<!-- END automation:ta date:2026-11-16 layer:PROFILE -->
