<!-- BEGIN automation:ta date:2026-11-15 layer:PROFILE -->
# ProjectACG Shader 与环境渲染 Profile｜2026-11-15

> 类型：PROFILE；项目：ProjectACG；只记录指定会话当天出现的实现、分析、报告和验证边界，不代表本次重新检查了工程源码。
> 关联参考：[Shader 云海与可选效果验证](../../../references/CodexDaily/Development_Reference_2026-11-15.md)；模块入口：[ShaderDevelopment](../../../README_Tech_URPShaderDevelopmentRules.md)；沿用 `CMP-01`、`CTL-02`、`VAL-03`。

## 1. 项目事实与现象

### FlowMap PBR 控件

- 会话报告将 Metallic、Smoothness、AO 基准滑条放入 PBR Mix Map 折叠区；关闭 Mix Map 时只禁用贴图和 UV 控件，基准滑条仍可调。空贴图使用白色默认值，使启用或关闭 Mix Map 时都由基准值控制。
- 会话报告 Unity D3D11 检查通过：1248 个编译组合、271 项像素检查和 3 项 Inspector 检查。真实角色材质与移动设备尚未复验。

### FlowMap 晶体内发光

- 用户要求把 `Monst_V2.shader` 的晶体内发光能力加入 FlowMap。会话描述的实现使用独立默认关闭开关、HDR 颜色、模型局部坐标中心/范围、UV0–UV3 选择，并把现有遮罩 G 通道用于区域限制；它独立于普通自发光开关。
- 会话报告代码已接入 Shader、`FlowMapShaderGUI` 与预设说明。默认关闭路径旨在不增加插值与片元开销。
- 当天可见的第一轮隔离检查报告三组变体编译、开关改变像素；但 HDR 阈值断言未通过，后续只看到调整检查方式的计划，没有当天成功重跑结果。因此该功能的最终验证状态未闭环，不能记作整体验收通过。

### BaseLitGalaxy 表面与内部发光

- 会话报告 `BaseLitGalaxy` 加入 BaseLit 风格 `_EMISSION`、`_EmissionMap`、HDR `_EmissionColor`。表面发光按贴图 RGB × HDR 颜色计算，使用基础表面 UV，Alpha 不参与，并进入 Meta Pass；表面发光与内部动态星空相加。
- 后续会话又将 `Monst_V2` 风格的晶体中心内发光接入同一材质：模型局部中心/半径、程序噪声、星空区域遮罩与内部显现控制；它与表面 PBR 自发光保持独立。
- 会话报告第一项功能通过 94 个 D3D11 变体、六个 Pass 导入、187 帧隔离渲染及新增像素检查；中心发光修改后又报告 94 个变体、六个 Pass、193 帧及 6 项新像素检查与 7 项既有回归通过。生产场景和移动设备未复验。

### 云海方案与已有环境渲染资产

- 用户目标是 Android/iOS 上从高处看到铺满山谷并延伸到地平线的背景型云海，期望性能良好；会话只做方案分析，没有修改工程文件。
- 会话查到已有 `CustomSkyCloudSurface` 云层 Shader、通用云朵网格预制体及 URP 高度雾。会话报告 Battle Low/High/Ultra Renderer 已挂自定义天空 Feature，Mid 未挂；高度雾只在部分档位接入。现有 README 对 Feature 未接入的描述与 Renderer 资产状态不一致。
- 会话还指出 `CustomSkybox.shader` 即使关闭天空层，也会先采样日/夜天空图再采云图；因此它不是纯云海需求下最轻的直接路径。
- 推荐方向是专用普通 URP 云面网格与轻量 Shader：低档单压缩云纹采样、缓慢 UV 滚动，高档可加细节；阴影、实时法线和多层透明应按画面需要谨慎启用，高度雾负责柔化地平线。该方案没有场景实现或目标设备测量。

## 2. 根因、修正与取舍

- FlowMap 的 PBR 滑条原先被放在独立区域，Mix Map 面板不能一处完成贴图与无贴图调参；修正为把基础值控件与 Mix Map 贴图控件放在同一区域，同时让基准值不依赖贴图开关。
- FlowMap 晶体效果复用现有遮罩 G 通道是工程适配选择，不等价于来源晶体 Shader 的独立晶体遮罩；必须核对目标材质的 G 通道内容。
- 云海优先网格云面是面向移动端可控采样和可活动镜头的方案建议，不代表当前现有天空 Shader 或 Renderer Feature 已满足场景目标。

## 3. 会话报告的验证证据

| 项目 | 会话报告 | 本 Profile 的证据界定 |
| --- | --- | --- |
| FlowMap PBR 滑条 | 1248 编译组合、271 像素检查、3 Inspector 检查通过 | 会话报告；生产角色/移动端未验。 |
| FlowMap 晶体内发光 | 首轮三组变体编译，开关改变像素；HDR 阈值断言失败 | 后续阈值重设与全套重跑没有在当天记录中得到成功结果。 |
| Galaxy 表面自发光 | 94 D3D11 变体、六 Pass、187 帧；像素与既有回归通过 | 会话报告；本次未读取报告产物。 |
| Galaxy 晶体中心发光 | 94 D3D11 变体、六 Pass、193 帧；6 项新检查和 7 项回归通过 | 会话报告；生产场景/移动端未验。 |
| 云海性能 | 推荐了网格云面和高度雾组合 | 仅方案分析；没有 Profiler、Overdraw 或实机数据。 |

## 4. 未验证项与候选

- FlowMap 晶体内发光需用正确的 HDR 断言重跑完整隔离验证，再检查目标材质 G 通道、角色观感、采样器与变体预算；当前以默认关闭作为回退。
- BaseLitGalaxy 两类发光需在生产场景、目标质量档和移动设备核对材质参数、Meta 烘焙与实际透明/后处理合成。
- 云海须先选定实际场景和相机，再对候选网格原型测 GPU 时间、Overdraw、覆盖率和温升；当前没有实现或性能结论。
- 规则候选：项目文档描述 RendererFeature 接入状态时应与每个目标质量档的活动 Renderer Data 对照。当前是一次项目文档漂移案例，留作 Profile 经验，不足以据此升级 CORE。

## 来源会话

- [01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe](../../../../../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md)
- [01a1d320-8c76-7d73-b99f-fd89b5b32b66](../../../../../VA_Worlk_memory/sessions/01a1d320-8c76-7d73-b99f-fd89b5b32b66.md)
- [01a1df44-47ae-7662-bfa1-571b90a718d9](../../../../../VA_Worlk_memory/sessions/01a1df44-47ae-7662-bfa1-571b90a718d9.md)

<!-- END automation:ta date:2026-11-15 layer:PROFILE -->
