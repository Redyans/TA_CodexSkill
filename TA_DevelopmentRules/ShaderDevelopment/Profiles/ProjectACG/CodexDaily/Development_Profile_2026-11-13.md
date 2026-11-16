<!-- BEGIN automation:ta date:2026-11-13 layer:PROFILE -->
# ProjectACG FlowMap 与星空 Shader Profile｜2026-11-13

> 类型：PROFILE；项目：ProjectACG；记录指定会话当日报告的实现状态与验证范围，不作为其他项目的默认实现。
> 关联参考：[Shader 屏幕坐标、循环混合与纹理分层](../../../references/CodexDaily/Development_Reference_2026-11-13.md)；模块入口：[ShaderDevelopment](../../../README_Tech_URPShaderDevelopmentRules.md)；既有规则沿用 `CMP-01`、`ARC-02`、`VAL-03`。

## 1. 项目事实与现象

### FlowMap 动态材质

用户先报告元素身体轮廓稳定，但内部流纹周期性变淡再出现。会话将其归因于循环贴图错位交接时细节被混合变柔，且大位移/狭窄颜色阈值会加重可见显隐；不等同于身体 Alpha 回跳。

当日会话报告已增加或整理以下功能：

- 可选三相平滑混合，关闭时保留旧双相；两层动态贴图都受控。
- 主贴图、平铺贴图、FlowMap、循环噪声、扭曲与遮罩分别选择 `UV0`～`UV3`，各自平铺/偏移独立。
- Fresnel 边缘可受现有流纹扰动厚薄/断续；复用流纹而不增加采样，身体透明度和网格轮廓保持独立。
- 原平铺输入保留语义并新增模型 UV 固定的基础主色，按用户选择相乘；扭曲可以按 FlowMap 方向移动。
- 会话核对到现有局部速度读取 `FlowMap.B`，按黑/白倍率插值后缩放循环内的流动位移；影响两层流纹、跟随流动的法线及跟随 FlowMap 的扭曲，不影响独立 XY 滚动扭曲。`FlowMap.RG` 仍表示方向；独立遮罩 `_MaskMap.B` 是另一项颜色分区输入。
- 主贴图 Alpha 半透明、顶点颜色 RGB/Alpha、可选 BaseLit PBR、Mix Map、自发光、法线及 MatCap Add/Mul。
- Mix Map 通道约定为 `R=Smoothness`、`G=Metallic`、`B=AO`。Metallic/Smoothness 提供 `0～1` 滑条；法线具有强度和 UV 选择，并可跟随流动。
- 为控制采样器，共用部分贴图的过滤/Wrap 设置；这会限制每张纹理独立设定 sampler 的自由度。

### 星空 Shader

另一 Shader 会话报告 `BaseLitGalaxy.shader` 新增 `ScreenUV` 坐标模式和屏幕视距缩放补偿。还包含 ViewDirection/ScreenUV 选择、贴图保持比例、视角闪点、Fresnel 闪点遮罩及 MatCap `Add/Mul`。会话内报告 84 项编译检查、15 项 Screen UV 检查及后续回归通过；具体检查报告来自该会话，本文未重新读取或复现。

## 2. 根因与实现取舍

- FlowMap 周期的双相等权交接仍可能平均掉已错位的纹理细节；三相模式针对交接观感，不能保证极高位移下完全保留锐利纹样。
- 屏幕坐标能保持大致屏幕尺寸，但会呈现投影/滑动效果。模型 UV、Screen UV、ViewDirection 与视距补偿语义不同，不能混为一种“远近不变”。
- 主色与动态纹理分层，让模型 UV 上的固定颜色区块不随流动漂移；透明度各来源必须避免重复衰减。
- `FlowMap.B` 和独立遮罩的 B 通道用途不同。该会话中的 FlowMap 局部速度读取 B；`_MaskMap.B` 属于颜色分区语义。局部倍率通过缩放循环内位移实现，未改成 `time × per-pixel-speed`。

## 3. 会话报告的验证证据

以下数字均为会话助手报告，未在本次自动化中独立验证：

| 检查 | 会话报告 |
| --- | --- |
| 三相模式 | 1024 个编译组合、68 项像素检查、Inspector 与六套材质实渲；关闭时测试画面与旧模式逐像素一致。 |
| 独立 UV | 六类贴图 `UV0`～`UV3` 混用检查；120 项像素检查及 Inspector；全部 UV0 时逐像素兼容。 |
| 流动 Fresnel | 138 项像素检查和六套材质实渲；真实角色与移动端未验。 |
| PBR 扩展 | 1024 个旧流动组合、152 个 PBR 组合、214 项功能/像素检查及 Inspector；BaseLit 直接像素对照因隔离场景缺少专用绑定而未成立。 |
| 法线/MatCap | 1248 个编译组合、268 项功能/像素检查及 Inspector；采样器超限修复后回归通过，49 个现有材质/贴图/展示 Prefab 文件据会话核对未改。 |
| 星空 Shader | 会话报告 84 项编译、15 项 Screen UV、19 项闪点效果检查及独立 Unity 渲染回归。 |

## 4. 未实现方案、未验证项与回退

- FlowMap 局部流速当日确认使用 `FlowMap.B`，按黑/白倍率插值缩放循环内位移；保留统一循环周期。独立速度纹理 `R`、速度曲线和专用绘制模式是建议，截止当天没有在 FlowMap Shader 中实现。
- FlowMap 的 `Screen UV` / 视距补偿是方案讨论，未修改该 Shader。建议先区分用户需要屏幕像素宽度还是希望远处减少过密平铺，再决定实现。
- FlowMap 三相/坐标/PBR/法线/MatCap 等新路径需要真实角色和移动端复验；自发光、透明深度相关 Pass（会话明确未实现 `ShadowCaster`、`DepthNormals`、`Meta`）不应据此宣称已覆盖所有渲染消费者。
- 星空 Shader 的实际生产场景、移动端、TAA 与不同屏幕比例/相机仍需项目内复验。当前材料只保留会话报告，不推断已完成这些验收。
- 新开关可关闭以回到既有双相/Unlit 路径；变体或采样器问题优先单独关闭新采样功能，并以同场景、同输入 A/B。

## 来源会话

- [01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe](../../../../../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md)
- [01a1d320-8c76-7d73-b99f-fd89b5b32b66](../../../../../VA_Worlk_memory/sessions/01a1d320-8c76-7d73-b99f-fd89b5b32b66.md)

未发现足以加入正式 CORE 的新结论；可迁移方法留在配套 REFERENCE。
<!-- END automation:ta date:2026-11-13 layer:PROFILE -->
