<!-- BEGIN automation:ta date:2026-11-12 layer:REFERENCE -->
# FlowMap 与 Shader 精度优化参考｜2026-11-12

> 类型：REFERENCE；适用范围：Unity Shader 的 UV FlowMap、纹理分层与局部精度优化；使用前提：按目标 Shader、URP、图形 API、材质和设备重新验证。
> 关联既有规则：`CMP-01`、`CTL-01`、`CTL-02`、`VAL-03`；ProjectACG 事实见 [Shader 与 FlowMap Profile](../../Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-12.md)。

## 1. 适用场景

- FlowMap 纹理随时间流动，周期回绕时出现纹理淡出/显现；或添加第二层流动纹理、扭曲、局部速度、颜色区间和发光。
- 评估片元路径的采样结果或颜色中间量是否能由 `float` 改为 `half`。
- 需要比较教程示例、材质参数和项目 Shader 的实现思路，但不直接照搬其他引擎或版本的材质。

## 2. 前提与依赖

- FlowMap 和噪声是数据纹理时需确认颜色空间；来源会话中 ProjectACG 贴图要求关闭 sRGB。迁移时重新核对 Importer、通道定义和 Shader 解码。
- 先约定每个通道语义。常见做法是 RG 表示方向、B 表示局部速率、A 或独立贴图保存遮罩；这不是固定标准，必须与绘制工具和材质保持一致。
- 区分“纹理属性/材质槽”与“纹理 RGBA 分量”。两者是不同选择层级，UI 不能把它们混称为一个通道。
- 双相位交叉混合可以减轻采样相位回绕的跳变，但不能据此断言任意速度、强度和贴图下循环都不可见。
- `half4` 片元返回值不能证明中间计算也以半精度执行；后端映射、指令和性能依赖图形 API 与设备。

## 3. 实现或排查步骤

1. 固定材质、相机、动画时间和目标设备，先复现流速/强度变化时的“消失再出现”。分别观察 FlowMap 回绕混合、UV 拉伸、噪声相位和 Alpha/遮罩，确定现象来自哪条数据路径；若根因不明确，保留为未解问题。
2. 为周期流动先验证双相位采样与平滑相位交叉淡化；将局部流速、相位噪声和主流向拆成独立控制量，避免把减速、扭曲和遮罩混在一个标量里调试。
3. 对第二层先明确目标是“细节扰动”还是“完整图层”。扰动层可围绕中性值累加；完整图层可对两张 RGB 做直接 `lerp`，比例 0/0.5/1 分别检查端点和中点。Alpha 应按独立的覆盖率合同处理，不能意外随第二贴图改变身体边缘。
4. 颜色分区、发光、Fresnel 与透明度使用各自明确的输入遮罩。UV 扭曲若只针对内部纹理，应让身体覆盖率保持稳定；不同材质可通过独立参数调节流动尺度和扭曲幅度。
5. 精度优化先盘点值的消费者：坐标、UV、深度/阴影坐标、法线/方向和裁剪阈值保留原精度；只挑颜色累加等局部候选逐个 A/B。不要因为变量范围有限就推断结果必定不变。
6. 检查目标 API 的编译产物和 GPU 时间，确认 `half` 真的生成更低精度运算且带来收益。保存同输入逐像素差异；有任何差异时回退该候选，或明确将其作为 Look Change 重新验收。

## 4. 风险与不适用边界

- 双相位交叉淡化可能在高速度/高强度、方向突变或特定纹理下暴露周期脉动；本参考没有证明单一参数能消除所有显隐。
- `lerp` 两端都可能执行采样，不应假定它跳过未选中的纹理或降低采样成本；编译期变体也需计入变体预算。
- 半精度可能改变阈值、高光、阴影、插值和颜色舍入；`float` 改 `half` 既不保证逐像素相等，也不自动带来速度提升。
- Catlike Coding 的 Flow 教程针对旧版 Built-in，Shader Graph 仓库各有 Unity/URP 版本；移植前要重建管线、Pass、材质和纹理颜色空间。
- 教程中的数值只是该教程资源与算法下的起点，不是跨项目默认值。

## 5. 验证与回退

固定时间与视角对比周期边界、低/高速度、低/高强度、遮罩边缘和 UV 接缝；对第二层检查 Lerp 端点及 Alpha 稳定性。精度改动逐项执行目标设备图像差异和 GPU 测量，同时查看生成代码。回退时恢复该变量/采样链原精度或原混合模式，保留已验证的其他功能。

可供拆解的案例：[Catlike Coding Texture Distortion](https://catlikecoding.com/unity/tutorials/flow/texture-distortion/)、[Catlike Coding Directional Flow](https://catlikecoding.com/unity/tutorials/flow/directional-flow/)、[limkarlkenji FlowMap](https://github.com/limkarlkenji/FlowMap)、[UnityFlowMap2dSimple](https://github.com/haneda-atsushi/UnityFlowMap2dSimple)、[UE4_FlowMap2dSimple](https://github.com/haneda-atsushi/UE4_FlowMap2dSimple)、[FlowmappedBurnShader](https://github.com/UnrealPlayground/FlowmappedBurnShader)、[Flowmap Canvas](https://github.com/dolag233/FlowmapCanvas)。页面与版本信息仅为来源会话报告，未在本次重新访问或运行。

来源：[01a1cdb0-8509-78c2-8c72-d97abaa8006a](../../../../VA_Worlk_memory/sessions/01a1cdb0-8509-78c2-8c72-d97abaa8006a.md)、[01a1d01e-0add-7fb0-ac81-a7080e7ce2ae](../../../../VA_Worlk_memory/sessions/01a1d01e-0add-7fb0-ac81-a7080e7ce2ae.md)、[01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe](../../../../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md)。
<!-- END automation:ta date:2026-11-12 layer:REFERENCE -->
