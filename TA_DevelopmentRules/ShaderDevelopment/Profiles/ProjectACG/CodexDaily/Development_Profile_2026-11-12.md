<!-- BEGIN automation:ta date:2026-11-12 layer:PROFILE -->
# ProjectACG Shader 与 FlowMap Profile｜2026-11-12

> 类型：PROFILE；项目：ProjectACG；记录当日会话报告的实现、证据与未验证边界，不构成跨项目默认值。
> 关联参考：[FlowMap 与 Shader 精度优化](../../../references/CodexDaily/Development_Reference_2026-11-12.md)；既有规则沿用 `CMP-01`、`VAL-03`。

## 1. 项目事实与现象

### `Chara_V2` 浮点精度

- 只读检查覆盖 46 个 `.shader/.hlsl/.cginc`。分析将位置、世界/裁剪坐标、UV、屏幕坐标、深度/阴影坐标、法线方向、裁剪阈值、SDF/Rim 和 `UnityPerMaterial` 缓冲区列为保留 `float` 的高风险链路；建议候选集中在局部颜色累加或有限遮罩标量。
- 一份 2026-11-05 iOS/Xcode 捕获被报告为：衣服同名 Shader 汇总约 13.1%、头发约 10.4%；衣服某热点状态寄存器 228，Draw 5789 单项约 3.06%。捕获包与当日源码/变体未逐项对应，且这些数字不是毫秒或瓶颈证明。
- 按用户请求，`Chara_Cloth_V2.shader` Forward 路径中的显式纹理采样结果和局部颜色中间量改为 `half`，局部各向异性高光、MatCap 颜色 helper 也改为 `half`。坐标、方向/法线、阈值及材质 CBUFFER 仍为 `float`；附加光接口保留 `float3` 并在本 Shader 内转换为 `half3`。
- `_NeedNormalMap` 仍是材质缓冲区 `float`。采样发生在它参与混合之前，因此改其精度不会省掉法线采样；严格 0/1 的局部转换虽可精确表示端点，但运行时若写入过渡值仍会改变结果。
- 会话指出当前 `shadowCoords` 一处使用 `half4` 不合适，建议恢复 `float4`；未见后续完成该恢复的证据。`metallic`、`reflectivity`、`ao`、`totalShadowArea` 和 Ramp 颜色强度只是待测候选，roughness、BRDF/IBL 与阴影坐标仍建议保留原精度。

### `Effect_FlowMap.shader`

- Shader 名为 `Valkyria/Effect/FlowMap`。当日逐步增加 FlowMap 强度/速度、噪声错相、B 通道局部速度、第二层流动、独立遮罩/双色分区、HDR 发光、Fresnel、雾效及自身纹理扭曲；每个功能模块有独立开关，ShaderGUI 参考工程现有风格。
- 最终第二层从灰度扰动改为两张贴图完整 RGB 直接 `lerp`，面板比例 0/0.5/1 表示第一层/等比/第二层。双色分区开启时仍会重着色混合结果；透明度保持独立控制。扭曲 XY 速度决定扭曲噪声移动方向，具体部位局部流向仍由 FlowMap 决定。
- 六套岩浆/水元素材质与展示 Prefab 已更新。最后一轮隔离验证报告 512 个编译组合、43 项 GPU 像素检查和 Inspector 检查通过；真实角色 UV、动画和移动端未验证。
- 22:59 左右用户报告高速度/高强度时感觉 FlowMap 纹理消失再出现。区间结束前只有继续检查循环混合的进度消息，没有根因、修复或复验结论，需保持未解决状态。

## 2. 根因、修正与证据

- **精度收益尚不能确定**：half 修改完成，`git diff --check` 通过，Unity 自动导入日志未发现该 Shader 自身编译错误；没有逐像素画面对比、完整变体编译或 iOS GPU 测量，因此不能声称零画面变化或性能提升。
- **关闭 `_NeedNormalMap` 不等于跳过采样**：数据先采样后按开关混合；要减少采样需单独改造可裁剪路径并检查实际变体/指令，尚未实施。
- **双层颜色语义已明确**：完整纹理之间的混合用 RGB Lerp，保持透明度独立；前一版本的第二层灰度加法不是相同效果，材质参数也已随实现变更。

## 3. 项目约束与候选

- 不将旧捕获的寄存器/占比直接换算为性能收益；先核对设备、Shader 版本、Keyword、材质和捕获时间。
- “高速度/高强度导致周期纹理显隐”可作为 FlowMap 调试候选，不是已复现根因或正式规则；需对齐相位回绕、UV 扭曲、遮罩和 Alpha 后再修改。
- 尚无证据支持把 ProjectACG 当前贴图通道、开关默认值或材质参数作为其他项目的默认约定。

## 4. 验证状态与回退

- **已报告**：Shader 自动导入无该文件编译错误；FlowMap 隔离编译、像素和 Inspector 检查通过；六套展示材质通过固定时刻实渲对照。
- **未验证**：`Chara_Cloth_V2` 图像零差异、设备性能、角色场景、FlowMap 真实角色 UV/动画、手机表现，以及高速循环显隐修复。
- **回退**：精度差异不接受时恢复对应变量/颜色 helper 原精度；FlowMap 新层可由模块开关关闭。恢复 `shadowCoords` 精度是会话建议，尚无完成记录。

来源：[01a1cdb0-8509-78c2-8c72-d97abaa8006a](../../../../../VA_Worlk_memory/sessions/01a1cdb0-8509-78c2-8c72-d97abaa8006a.md)、[01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe](../../../../../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md)。
<!-- END automation:ta date:2026-11-12 layer:PROFILE -->
