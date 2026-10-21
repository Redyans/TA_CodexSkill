---
name: ta-shader-sky-mesh-render-feature-reference
description: 天空与云层 Mesh Shader、URP Renderer Feature、ShaderGUI 单贴图位和生成式渐变贴图的可迁移实现与排查参考。
---

# 天空 Mesh Shader 与 Renderer Feature 参考

> 类型：REFERENCE；适用范围：Unity URP 的天空、云层和专用 Mesh 绘制链；使用前提：必须按目标工程的 Unity/URP 版本、材质接口、Renderer 配置和设备重新验证。本参考整理实现模式，不证明特定移植与来源版本完全等价。

## 1. 适用场景

- 将 Unity Environment Skybox 改为场景 Mesh 材质，以获得独立几何、排序、Alpha、层级或云层组合控制。
- 用 ScriptableRendererFeature 绘制带专用 LightMode 的 Shader Pass。
- 合并天空平面、天空球和云层 Shader 的能力，同时保留独立材质路径。
- 在一个材质贴图属性上同时支持拖入美术贴图和编辑/烘焙 RGB/A 渐变。
- 将第三方 URP Shader 移植到另一 URP 版本，并审查遗漏的特性、资源或渲染时序。

## 2. 前提与依赖

开始前，收集源工程和目标工程的 Unity/URP 版本、颜色空间、目标图形 API、支持设备、Shader、HLSL Include、Renderer Feature/Pass、材质、Prefab/Scene、Renderer Data、Camera Stack 及运行时材质写入者。逐项建立功能矩阵，标记源实现、目标映射、实际消费者和验证结果。

| 工具 | 用途 | 证据边界 |
| --- | --- | --- |
| rg、版本控制 diff | 搜索属性、Pass、Keyword、Include、材质引用和源目标差异。 | 静态文本不证明编译、运行时启用或画面正确。 |
| Unity Editor / Console | 导入、Shader/C# 编译、查看实际材质 Inspector 和资源引用。 | 导入成功不证明 Renderer Feature 已加入正确 Renderer。 |
| Frame Debugger | 查看 Draw、Pass、材质、渲染目标及前后时序。 | 应选中专用 Draw；最终合成 Draw 未必能说明前置采样是否正确。 |
| RenderDoc / GPU Capture | 检查图形 API 上的 Draw、绑定和像素输入。 | 适用于 Frame Debugger 无法解释的设备差异。 |
| 目标设备构建 | 检查平台编译、Keyword、精度、性能、透明排序及 GPU 行为。 | Editor/PC 预览不能代替设备验证。 |

参考图、聊天描述和属性名称只能作为线索。源代码、材质序列化、消费者配置和实测结果相互印证后，才能记录为已迁移功能。

## 3. 实现或排查步骤

### 3.1 建立功能与依赖矩阵

对每个原功能记录输入属性和通道、坐标空间/UV、默认值、Blend/深度/剔除状态、Keyword、Pass、调用者、平台分支和验收方式。

| 功能域 | 应核对的行为 |
| --- | --- |
| 天空 | Environment Skybox 或 Mesh UV/方向映射、昼夜插值、贴图平移/缩放、天空旋转、HSV/曝光/颜色、Alpha。 |
| 星光 | 主贴图、遮罩、闪烁时间、区域抑制、强度及与昼夜/天空层开关的依赖。 |
| 云层 | RGBA 与打包通道语义、遮罩、密度、自发光、透明度、Alpha/Add/Multiply 合成。 |
| 云层运动 | 基础 UV、Flow Map、双相位采样、速度、细节/法线是否跟随流动。 |
| 云层照明 | 法线空间、切线基、主光、间接光、曝光及关闭法线图时的几何法线路径。 |
| 雾与裁剪 | 地平线雾或场景雾、雾坐标/衰减、Alpha Clip 输入及其是否影响相关 Pass。 |
| 绘制链 | Pass Tag、Queue、Blend、Cull、ZWrite/ZTest、Renderer Event、Layer、相机类型与目标附件。 |

先解决资源和职责对应，再写目标实现。算法相似、属性同名或 Inspector 可见都不足以证明行为等价。

### 3.2 选择组织方式与绘制入口

简单、独立的 Unlit 效果可放在单个 .shader；需要多个 Pass 或多个 Shader 稳定复用时，再提取职责明确的 Include。复杂天空/云层应将材质采样与合成和绘制时机分开：Shader 定义公开 Pass Tag 和渲染状态，Renderer Feature 按 Layer、Queue、Camera 过滤并提交 Draw。

迁移或改名时维护映射表：旧 Shader 名到新 Shader 名、旧 C# 类型/namespace 到新类型、旧 Pass LightMode 到新 Tag、旧 Feature 文件到新路径、材质/Renderer Data 到新 GUID。资产改名或目录搬迁时检查引用和 .meta；不要仅凭文件名替换推断 YAML 引用会自动改变。

运行时 Include 应使用相对于当前 Shader/Include 文件的路径，例如：

```hlsl
#include "ShaderLibrary/CustomSceneColorAdjust.hlsl"
```

改动目录后从实际 Shader 所在位置解析每一条相对 Include，并在 Unity 导入后检查编译日志。不要把某台机器的盘符或工程根目录写进 Shader Include。

### 3.3 用一个贴图属性兼容贴图和渐变制作

如果美术输入和程序生成结果在渲染阶段都是同一种纹理数据，可让它们共用一个 ShaderLab 2D 属性：普通图片直接拖入；生成渐变则由现有 MaterialPropertyDrawer 在同一属性下增加编辑控件、预览和烘焙命令。材质序列化只保留一个纹理引用，不需要“原图槽 + 渐变图槽 + 模式开关”。

示例语法必须与目标项目的 Drawer 构造函数一致。项目允许选项参数时，可采用如下形式；未实现该参数的项目不得直接照搬：

```shaderlab
[GradientSplit(PreserveSource)][NoScaleoffset] _DaySky ("白天天空贴图", 2D) = "white" {}
```

把 Inspector 的视觉高度和 Shader 纹理槽数量分开看：GradientSplit 会在一个 texture field 下面绘制 RGB/A 渐变、宽度、读取/烘焙/另存和采样位置等控件。这些额外控件不是额外纹理槽。若属性被普通 GUI 和 Drawer 重复绘制，检查 CustomEditor 是否重复调用 ShaderProperty、是否又手动画同一属性、Drawer 高度和属性隐藏规则。

烘焙既有普通贴图时，应由目标 GUI 明确定义源资产策略。保留源图策略需要跟踪用户拖入的源纹理，辨别预览纹理与正式生成纹理；首次烘焙写到材质旁的新资产，回写材质引用，之后只更新有明确生成标记/元数据的输出。用测试材质和副本验证首次生成、二次更新、另存、读取及撤销，不对正式美术源图试错。

把任意二维纹理自动解释为渐变会有信息损失：普通天空图无法无损恢复成一维 RGB/A 曲线。读取像素得到的曲线只能视为近似编辑起点；若贴图含高频细节、压缩、Mip 或通道专用编码，不能把近似曲线重新烘焙当作原图副本。

### 3.4 让普通天空图与生成渐变共用采样接口

单槽方案需要让 Shader 识别输入形态，或使用显式且可维护的材质语义。按宽高阈值猜测窄高图是渐变实现简单，但可能误识别横幅、LUT 或图集。项目可控时优先用清晰元数据/导入设置，或明确的类型参数；若必须按尺寸推断，应把阈值写在一个采样函数内，并验证普通贴图边界。

采样前明确每类纹理是否应用 _ST、流动偏移和 Wrap Mode。生成 ramp 通常把一维渐变放在纹理 X 轴、用天空高度映射到 ramp X；此模式可能有意忽略普通贴图的二维缩放和流动。NoScaleoffset 会隐藏 tiling/offset 控件；如果制作流程要求调节普通天空图 tiling，需将 Inspector、HLSL _ST 和两类纹理采样规则一起设计。

### 3.5 接通 URP Renderer Feature

Renderer Feature 的绘制链应能回答：在哪些相机 Enqueue、选择哪个 LayerMask 和 Queue、按哪个 ShaderTagId 找 Pass、目标 Color/Depth 是什么、何时绘制、何时释放资源。示意：

```csharp
var drawingSettings = CreateDrawingSettings(shaderTags, ref renderingData, sortingCriteria);
context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
```

调用只有在 Pass Tag、Layer、Camera、Render Queue、排序和目标附件都匹配时才有效。将 Feature 加入实际运行的 URP Renderer Data；在 Prefab/Scene 设置正确 Layer；逐一验证 Base/Overlay、SceneView、Reflection/Preview 相机策略和 RenderPassEvent。若 Feature 消费全局纹理、Stencil 或临时 RT，还要明确生产者、消费者、生命周期和清理规则，按 ARC-03、ARC-04 及 Renderer Feature 时序参考执行。

### 3.6 做受控迁移而非按文件复制

每个源功能在目标端标记为“保留、映射、替换、有意删除、未迁移、未验证”。检查源 Shader 的 Properties、所有 Pass/pragma/变体、HLSL 函数、资源通道、Renderer Feature/Pass、材质值、宏、URP API 和平台分支。发现目标没有对应代码时先确认它是否由其他模块提供，不能只补 Shader 片段就宣称完成。

先建立最小基线材质，逐组打开功能并对比源/目标：先基础天空，再昼夜、云层、Flow、Detail、Lighting、Fog、透明；一次只改变一组输入。固定相机、网格/UV、纹理、颜色空间、灯光、时间和曝光，记录差异与已知不等价项。

## 4. 风险与不适用边界

- Unity Environment Skybox 的天空绘制、相机旋转、雾、深度和管线时序语义，不会因改成球体 Mesh 自动保留。
- CustomRenderPass 读取 Renderer Feature 当前 Color/Depth Target 受 URP 版本 API、RenderGraph、相机 Stack 和 Pass Event 影响；URP 14 写法不能假定适用于更新版本。
- Transparent Queue 与 SortingCriteria.CommonOpaque、ZWrite Off、Blend 状态的组合应按需求审查，不能把示例排序用于所有透明云材质。
- SceneView、Overlay、反射和相机预览可能需要不同绘制策略；跳过某类相机可能造成 Editor/GameView 差异或相机叠加重复绘制。
- GradientSplit 参数、源贴图保护、预览资产路径、Importer 元数据和平台压缩规则均由具体 Drawer 实现决定；PreserveSource 不是 Unity 内建语法。
- Inspector 可见不等于 Shader 分支被编译/启用。序列化 Float、Keyword 和 shader_feature_local 必须保持一致。
- 改名会影响脚本按 Shader 名查找、材质引用、RendererData、AssetBundle 收集和 GUID。需检查项目内所有消费者。
- 功能合并会放大一个材质的参数复杂度。可以合并实现但保留职责清晰的独立 Shader/材质入口；不要为“只有一个 Shader”丢失独立使用路径。

## 5. 验证与回退

### 5.1 静态检查

- 检查 Shader 名、Property 名/类型/默认值、CustomEditor、Pass Name、LightMode、Queue/Blend/Cull/Depth、Keyword pragma 与 HLSL 声明/读取是否一致。
- 检查 Include 相对路径、Feature 类型名/namespace、Renderer Data 引用、Layer、材质 GUID 和 .meta 完整性。
- 确认单槽设计每种天空状态只有一个 Shader 2D 属性；额外 RGB/A 控件来自一个 Drawer，不是重复 Properties 或另一段 GUI 绘制。
- 比较源/目标功能矩阵，单列未迁移和无证据功能；不以 diff 行数、属性数或编译通过替代行为核对。

### 5.2 编辑器与渲染验证

1. 等待 Unity 导入完成，检查 Console/Editor.log 中本次 Shader、Drawer、Renderer Feature 的错误。
2. 用临时材质分别测试：直接拖普通 2D 图；编辑 RGB 和 A；预览；首次烘焙确认源图未变且材质切到新图；再次烘焙确认更新生成图；另存和读取按约定工作。
3. 普通贴图检查 tiling、offset、Wrap、颜色和 UV；渐变纹理检查采样轴、颜色域、Alpha、压缩、Mip 和最小尺寸；确认材质关键字在编辑器重开后仍正确。
4. 在实际 Renderer Data 启用 Feature，以目标 Layer、目标相机和目标 Pass 验证。用 Frame Debugger 查找专用 LightMode Draw，核对顺序、附件、材质、变体和混合结果。
5. 做天空/云层代表性场景 A/B，并在目标设备/构建复验透明、相机 Stack、运行时 Keyword 和性能。记录未覆盖设备与功能。

### 5.3 回退

保留原 Shader/材质/Renderer 配置快照和源贴图；按资产 GUID/引用关系执行反向映射。回退后确认旧材质仍解析原 Shader、Renderer Data 未遗留错误 Feature 引用、AssetBundle/构建收集恢复。烘焙资产是生成结果，确认没有场景/材质继续引用后，才由资产所有者决定清理。
