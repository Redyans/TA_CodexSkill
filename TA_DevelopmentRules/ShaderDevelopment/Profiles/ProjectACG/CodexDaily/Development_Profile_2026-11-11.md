<!-- BEGIN automation:ta date:2026-11-11 layer:PROFILE -->
# ProjectACG UI 模糊开发经验｜2026-11-11

> 类型：PROFILE；范围：北京时间 2026-11-11 00:00（含）至 2026-11-12 00:00（不含）。来源会话工作目录是 `ProjectACGMain/ProjectACG`；只采用指定 sessions 内当日消息，不读取项目源码、截图或测试产物。
> 可迁移方法见 [UI 模糊参考](../../../references/CodexDaily/Development_Reference_2026-11-11.md)。正式要求引用 [Shader 模块入口](../../../README_Tech_URPShaderDevelopmentRules.md)的 `ARC-03`、`ARC-04`、`VAL-02`，不修改 CORE。

## 1. Gamma UI 合成与模糊 Shader

**项目事实/现象**：用户用 `UI Texture Blur` 测试 Image 模糊，并要求与项目正确 UI 一致。会话称模糊 Shader 带 `GammaUI` Pass、预乘 Alpha 和防重复绘制 Guard，但此前只测单 Pass。来源：会话 [01a1c5c9-7942-7061-9a27-699e9e25fa9e](../../../../../VA_Worlk_memory/sessions/01a1c5c9-7942-7061-9a27-699e9e25fa9e.md)，消息 13–40。

**根因与修正（会话报告）**：HDR 场景 RT 半透明时比 `GammaDefault` 偏亮；跨 Shader 材质复制丢失模糊纹理绑定，影响 SoftMask；截图还显示 `_BlurTex` 报错和图标灰块。会话称已对齐颜色/透明度处理，改为仅复制目标 Shader 匹配属性，并修正 Dual Blur 随金字塔层级累积过大的采样距离。另一个 `cameraColorTarget` 时机错误归属于项目原有 `RadialBlurRendererFeature`，被报告为 URP 相机纹理读取阶段和同 Pass RT 释放顺序问题，不等同于新模糊 Shader 的独立错误。

**验证证据（会话报告）**：独立 Unity/D3D11 检查报告 37 项 Gamma 合成检查、112 项原有回归检查，最终又报告合计 182 项与 AOT 编译通过。分项包含关系未说明；按原报告保留，不推导总数。覆盖颜色、顶点/材质颜色、多档透明、HDR RT、Stencil、RectMask2D、普通 Mask、SoftMask 两种组件顺序及相机 Pass。会话同时说明原有模糊回归结果另有覆盖。

**项目使用条件/未验证项**：记录中的建议配置为 Image 使用 `GammaUIDefault`、组件 `Source Texture`/`Source Camera` 留空、`Kawase` 半径 4–8、4 次迭代、Full 分辨率；场景 RT 则需指定来源 RT/相机并保证源相机先渲染。图标放在 UI Layer，Canvas 绑定 `UICamera` Overlay；若父级用 `Mask`，其 Image 也使用 `GammaUIDefault.mat`。当前页面最终显示、真实项目的排序/遮挡及手机性能都未复验；以上只记录会话说明，不保证当前工程状态。

## 2. 候选与验证边界

- **候选**：跨 Shader 复制模糊材质时保留纹理绑定、在正式相机合成链路验证遮罩和 HDR、对已有 RendererFeature 错误按调用栈归因。这些方法可引用既有 `ARC-03`、`ARC-04`、`VAL-02`，当日证据不足以新增 CORE。
- **待复验**：在 Unity 重新编译后观察目标 UI，复测真实 Canvas 排序、Mask/SoftMask 组合及目标移动 GPU；会话中的独立 fixture 不等于生产场景验证。
- **未读取**：实际 Shader/组件源文件、用户图片和验证日志。本 Profile 不是对它们当前内容的独立确认。

<!-- END automation:ta date:2026-11-11 layer:PROFILE -->
