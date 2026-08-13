# Shader 项目集成检查清单

> **用途**：新增 Shader/Include、修改材质、RendererFeature、全局资源、Pass 或运行时接口时使用。静态检查不能替代 Unity 导入、Frame Debugger、目标设备和实际构建。

## 1. 修改前

- 确认 Unity/URP 版本、目标 Shader 类型、复杂度、目标平台和性能预算。
- 冻结受影响 `.mat`、Prefab、Scene、Animation、Timeline、脚本、`MaterialPropertyBlock`、AssetBundle、Renderer 和 Camera 配置。
- 列出属性、默认值、贴图通道、Keyword、Pass/`LightMode`、Queue、Blend、ZWrite、ZTest、Cull、Stencil、全局资源和 RendererFeature 消费者。

## 2. 实现中

- ShaderLab Tags、Pass Name、Include、CBUFFER、Texture/Sampler、Keyword 和 Render State 与实际声明一致。
- 屏幕资源记录生产者、RenderPassEvent、相机生命周期、MSAA/动态分辨率/Camera Stack 语义和消费者。
- 属性、Drawer、GUI、脚本和动画接口保持序列化兼容；变更时提供可审计、可回退的迁移方案。
- 不修改 URP Package；功能应落在 Shader、Adapter、RendererFeature 或项目共享模块的正确边界。

## 3. 交付前

- Unity 导入无本次引入的 Shader 错误；目标材质、Prefab、Scene 和资源包引用没有丢失。
- Frame Debugger 验证实际 Draw、Pass、Render Target、资源绑定和时序；角色还验证部位、姿态、动画和透明层级。
- 目标设备验证视觉、性能和运行时 Keyword/动态加载；涉及变体时附 [`shader-variant-report.md`](shader-variant-report.md)。
- 记录已完成验证、未验证项和剩余风险；`git diff --check` 仅作为文本检查。
