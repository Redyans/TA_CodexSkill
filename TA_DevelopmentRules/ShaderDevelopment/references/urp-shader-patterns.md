# URP Shader 模式参考

> **用途**：为 `ARC-*`、`WRK-02`、`WRK-05` 提供通用的 URP 实现选择。实际函数签名、Keyword 和 Pass Tag 必须以当前项目 Profile 指定的 URP 版本与现有实现为准。

## 1. 实现模式选择

| 类型 | 最小模式 | 关键边界 |
| --- | --- | --- |
| 普通 Unlit | 单一 Forward Unlit Pass、最小 `Core.hlsl` 依赖。 | 不默认添加粒子、阴影、GI、Fog、Meta 或多 Pass。 |
| 标准 URP PBR | Surface 输入 + InputData + 当前 URP 标准 PBR 入口。 | 主光、附加灯、GI、阴影优先复用 URP，不复制独立实现。 |
| SimpleLit | Specular/SpecGloss 与 Smoothness 语义。 | 不把 Metallic/Mask 工作流强套到 SimpleLit。 |
| Toon / Custom Lighting | 项目既有共享 Kernel/Adapter 的最小扩展点。 | 保留通用光源遍历、GI、Fog 和最终流程；不得每个 Shader 复制 Kernel。 |
| Fullscreen / Decal / 后处理 | 匹配现有 RendererFeature/Blit 和 RenderPassEvent。 | 资源由 RenderPass 生产，材质只消费公开接口。 |

## 2. 标准材质输入检查

1. Surface 层只采样并解释实际开放的 Base、Normal、Metallic 或 Specular、Smoothness、Occlusion、Emission、Alpha/Alpha Clip。
2. Input 层按实际能力组装世界坐标、法线、视线、阴影、Fog、GI、Lightmap 和 ShadowMask；未启用的能力不声明为支持。
3. PBR 使用当前 URP 的标准入口；标准入口无法表达需求时，先比较局部 Adapter、共享 Kernel、专用 Pass 与 RendererFeature 的边界。
4. ShadowCaster、DepthOnly、DepthNormals、Meta 只在需求/消费者存在时生成或维护，并复用同一顶点与 Alpha 契约。

## 3. 可选能力与变体

- Lightmap、ShadowMask、Forward+、Light Cookies、SSAO、Detail、LOD Cross Fade、DOTS、GPU Instancing、Specular Highlights 和 Environment Reflection 均是按需能力，不因模板注释存在而默认开启。
- Keyword 选择遵循 `CTL-02`：先确定局部/全局、顶点/片元、互斥关系、控制方和剥离路径，再决定 `shader_feature` 或 `multi_compile`。
- 每次生成/重写或改变变体来源时，使用 [`shader-variant-report.md`](shader-variant-report.md) 报告参考范围与实际验证边界。
