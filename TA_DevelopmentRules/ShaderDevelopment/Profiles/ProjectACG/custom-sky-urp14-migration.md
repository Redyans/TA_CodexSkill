---
name: projectacg-custom-sky-urp14-migration
description: ProjectACG CustomSkyboxURP14 Shader、Renderer Feature、GradientSplit GUI 与迁移验证的项目事实和未闭环项。
---

# ProjectACG Custom Sky URP14 移植 Profile

> Profile ID：projectacg-custom-sky-urp14-migration-v1

> 适用工程：当前 ProjectACG Client 工作区

> 事实快照：2026-10-20

> 用途：记录 CustomSkyboxURP14 移入 ProjectACG 后的目录、材质 GUI、绘制入口、关键修正和验证边界。本文属于项目 PROFILE；跨项目实现方法见 [天空 Mesh Shader 与 Renderer Feature 参考](../../references/sky-mesh-render-feature-and-texture-gui.md)。

## 1. 项目事实与当前实现

### 1.1 资产和依赖

| 角色 | 当前 ProjectACG 路径/事实 |
| --- | --- |
| 场景 Shader 与 HLSL | `Assets/Shader/Scene/New/CustomSkybox/`，包含三个 Shader 和 `ShaderLibrary/`；当前没有 `Materials/`、`Textures/` 子目录。 |
| 现有场景材质 | `Assets/AssetRaw/scene/common/Public_Sky/mat/` 下有 `mat_Public_SkyBase_Dusk01.mat`（CustomSkyPlane）及 `mat_Public_Cloud01_Dusk.mat`、`mat_Public_SkyCloud_Dusk01_test.mat`、`mat_Public_SkyCloud_Dusk02_test.mat`（CustomSkyCloudSurface）；当前没有材质引用合并用的 CustomSkybox Shader。 |
| Renderer Feature | `Assets/Shader/RenderFeature/CustomSkybox/CustomSkyRendererFeature.cs`。 |
| Inspector GUI | `Assets/Plugins/CustomShaderGUI/`；Shader 指定 `Scarecrow.SimpleShaderGUI`，渐变使用现有 `GradientSplitDrawer`。 |
| 目标运行时 | Unity 2022.3.62f3，embedded URP 14.0.12；迁移到其他版本需重新核对 API。 |
| 原始来源 | F:\UnityProject\Meow\Assets\__Art\MeowSkyboxURP14\；含 Runtime Shader/Include、RendererFeature、Runtime asmdef、Editor GUI、材质和贴图。 |
| Shader 名 | Valkyria/Scene/CustomSkybox、Valkyria/Scene/CustomSkyPlane、Valkyria/Scene/CustomSkyCloudSurface。 |
| Feature namespace/type | CustomSkyboxURP14.CustomSkyRendererFeature。Feature .cs 的 .meta GUID 与原 MeowSkyRendererFeature 相同，因此 Renderer Data 脚本引用保持。 |

Feature 按 CustomSkyboxForward Tag 绘制，使用可配置 Layer Mask，默认事件为 BeforeRenderingOpaques，允许 SceneView 选项并默认跳过 Overlay 相机。Assets/AssetRaw/Settings/URP-Battle-Low-Renderer.asset、URP-Battle-High-Renderer.asset 和 URP-Battle-Ultra-Renderer.asset 均序列化引用同一 Feature GUID，m_Active 为 1、LayerMask 为全部 Layer。Low/Ultra 的 RenderPassEvent 序列值为 250，High 为 400；三份配置的绘制时序并不一致。AssetRaw 中存在配置不等于已证明运行时质量档正在使用它们，仍需核对 URP Asset/Quality 映射并用 Frame Debugger 检查 Draw。

原工程包含 MeowSkyboxURP14.Runtime.asmdef，引用 Unity.RenderPipelines.Core.Runtime 与 Unity.RenderPipelines.Universal.Runtime。当前迁入的 Feature 源码位于默认程序集，没有迁入该 asmdef。单个 Feature 可在预定义 Assembly-CSharp 中编译，不需要名为 MeowSkyboxURP14.Runtime 的额外 C# 文件；但省略 asmdef 会改变程序集身份。若其他 asmdef、脚本反射或构建工具按旧程序集名引用类型，必须保留/替换 asmdef 并验证引用。当前已发现的 Renderer Data 使用保留的 MonoScript GUID，而没有发现要求旧 Runtime assembly 名的调用证据。

### 1.2 Shader 职责

| Shader | 当前职责 |
| --- | --- |
| CustomSkyPlane.shader | 昼夜天空、HSV/曝光/颜色、流动、星光、雾和 Alpha Clip 的 Mesh Pass。 |
| CustomSkyCloudSurface.shader | 单独云层表面，包含 RGBA/打包通道、遮罩/自发光、Flow、Detail、法线与光照。 |
| CustomSkybox.shader | 将天空与云层功能放在同一 Mesh Shader；支持 Mesh UV 或方向映射、昼夜、云层通道、Flow、Detail、Lighting、Fog、Alpha Clip 与天空/云层独立开关。 |

CustomSkybox.shader 不再依赖 Unity Environment Skybox 槽；三个 Shader 的 Pass 由 Renderer Feature 通过 CustomSkyboxForward 选择。文件 Include 使用相对 Shader 目录的项目路径，不写外部盘符。

原始 MeowSkybox.shader 是 Environment Skybox Pass（LightMode=Skybox），原始 MeowSkyPlane 和 MeowSkyCloudSurface 则由 MeowSkyboxForward Renderer Feature 绘制。迁入后的 CustomSkybox 将合并天空/云功能改为 Mesh 的 CustomSkyboxForward Pass；场景必须将材质放在网格上，不能再只配置 RenderSettings.skybox。这是明确的绘制入口变化。

### 1.3 单贴图位与生成式渐变

白天和夜晚属性分别为 _CubeMap_D 与 _CubeMap_N。两个属性都使用：

```shaderlab
[GradientSplit(PreserveSource)][NoScaleoffset]
```

每个属性在材质数据中只有一个 2D 纹理槽。GradientSplitDrawer 在同一字段下绘制 RGB/A 曲线、宽度、读取、烘焙和另存；曲线控件并非额外 Shader 纹理槽。普通 2D 贴图可直接拖入。

PreserveSource 是对现有 Assets/Plugins/CustomShaderGUI/Editor/PropertyDraw/PropertyGUI_Gradient.cs 的扩展选项，不是 Unity 内建 Drawer。源图没有渐变元数据时，首次烘焙在材质相邻 Gradients/ 目录生成新 TGA 并将属性改指向生成结果；有生成元数据的贴图可继续更新。GUI 编辑时还会在 Assets/Generated/RampPreviewCache/ 管理预览图。读取普通图的像素只近似恢复 RGB/A 曲线，不能恢复原始图像细节。

Shader 当前根据 _CubeMap_*_TexelSize 判断宽度至少 64、高度至多 4 的纹理为横向 ramp，并按天空 UV 的 Y 采样；普通贴图继续按二维 UV 采样。两类采样路径不同，ramp 路径不应用二维纹理缩放与同一流动逻辑。白天/夜晚天空属性带 NoScaleoffset，因此不显示 tiling/offset；云层主贴图 _MainTex 没有 NoScaleoffset，现有 TexDrawer 会绘制 TextureScaleOffsetProperty，可调 tiling 和 offset。不要把天空贴图与云层主贴图的控件状态混为一谈。若未来要求调节天空 tiling，应同时重新设计 Drawer 与采样契约，不要只移除 Attribute 后假设 ramp 也适用 _ST。

## 2. 迁移中的关键问题与解决方式

### 2.1 多贴图槽造成 Inspector 重复选择

曾采用原始天空图、生成渐变图和开关分别暴露属性，材质面板会出现多个需要选择的贴图位置。由于 Shader 最终只需要一组采样结果，现改为每个昼夜时段一个贴图属性，由 GUI 在同一属性下提供曲线编辑和烘焙。

排查类似“插槽很多”时，区分四种情况：

1. ShaderLab Properties 中实际声明了多少 2D 属性；
2. SimpleShaderGUI 是否将同一个 MaterialProperty 绘制两次；
3. Drawer 是否在单个属性下绘制多行子控件；
4. 当前材质选用的是哪一个 Shader、是否选中了旧版材质或旧属性仍被折叠显示。

以材质 Inspector 的 Shader 名、Property 名和槽位为准，不能仅数面板中的控件行数。

### 2.2 烘焙不能破坏普通源贴图

普通天空贴图是可编辑源资产，渐变 ramp 是派生资产。PreserveSource 路径将源贴图缓存和当前预览/烘焙输出分开，并用渐变元数据判定是否属于可更新的 ramp。验证此逻辑时必须用临时材质和源图副本：首次烘焙、再次烘焙、另存、读取分别检查文件引用；不得用覆盖正式美术贴图的方式做实验。

### 2.3 源 Shader 对照与接口变化

原始目录已从 F:\UnityProject\Meow\Assets\__Art\MeowSkyboxURP14\ 读取。静态 Property 名称比对结果：CustomSkyPlane 保留了原 MeowSkyPlane 的全部属性名；CustomSkybox 保留原 MeowSkybox 的全部属性名，并增加了 Mesh UV、相机朝向、天空/云层开关、Blend/Cull、远深度和分组属性；CustomSkyCloudSurface 去掉 _CullFace 与 _BlendMode，改用 _Cull、_SrcBlend、_DstBlend 等 Unity 枚举属性，并增加显式 Flow/Detail/Normal Toggle 属性。名称保留不证明数值、Keyword、视觉和默认值等价。

原 MeowSkybox.shader 原本已经把天空和云层写在同一 Shader，但以 Environment Skybox Pass 绘制。当前 CustomSkybox 的主要变化是改为 RendererFeature Mesh Pass，增加 Mesh UV/方向映射和层控制，并用独立 _CloudBlendMode 在 Shader 内合成天空/云层，再用 _SrcBlend/_DstBlend 控制 Mesh 与场景混合。必须把“内部云层合成”和“外部 Mesh 混合”当作两个不同接口验证。

原 MeowSkyCloudSurface 的 MeowSkyBlendDrawer 会联动 Blend keyword、Src/Dst Blend Factor 和 Alpha Factor；MeowSkyCullDrawer 会把面枚举映射为 _Cull。目标 CloudSurface 删除这两个自定义 Drawer，改用 Unity Enum 与标准 Toggle，并以现有 Scarecrow.SimpleShaderGUI 绘制。这个改造满足不新增 CustomSkyCloudStateDrawers 的约束，但独立 CloudSurface 的混合预设/关键字联动语义不应被假定为逐项一致，应按三种混合模式和 Cull 状态做材质 A/B。

原工程带有 MeowSkyboxShaderGUI.cs 与 MeowSkyCloudStateDrawers.cs；目标 Shader 统一使用 Assets/Plugins/CustomShaderGUI/ 的 Scarecrow.SimpleShaderGUI 和现有 GradientSplitDrawer，不迁入这两个定制 GUI 文件。移植自定义 Drawer 时，不能只搬 Shader 属性标记；应核对它写入的隐藏属性、Blend/Queue、Keyword、材质保存和 Undo 行为，并确认目标 GUI 已提供等价能力。

## 3. 颜色和贴图语义核对表

合并 Shader 与源 Shader 核对时，至少比对：

| 项目 | 合并 Shader 当前接口 | 核对方式 |
| --- | --- | --- |
| 天空坐标 | _UseMeshUV 开启读 Mesh UV，关闭时用局部方向生成经纬 UV；_SkyRotation 对应两条路径都参与。 | 用已知 UV 网格与方向球分别测试旋转、接缝、极点和 UV 反转。 |
| 天空白天/夜晚 | _CubeMap_D、_CubeMap_N、_LerpVal、两组 HSV/Exposure/颜色。 | 固定输入逐步改变插值；确认两个资源都能被采样且 Alpha 语义未丢。 |
| Ramp 识别 | width >= 64 且 height <= 4，沿 uv.y 采样纹理 X 轴。 | 用阈值边界的 ramp 和普通宽横图验证误判风险。 |
| 云层贴图 | _Use_RGBAMainTexture 选择 RGBA 直读或项目打包通道解码。 | 检查真实贴图通道含义，不以 Shader 属性名推断。 |
| 云层合成 | _CloudBlendMode 为 Alpha、Add、Multiply；_UseSkyLayer/_UseCloudLayer 独立控制层。 | 测试单独天空、单独云和同时显示，检查透明背景与 Alpha Clip。 |
| Flow/Detail/Normal | _UseFlowMap、_FlowEffectNormal、_FlowEffectDetail 与相应变体。 | 分别启停流图及跟随开关，观察云、细节、法线和噪波是否独立正确。 |
| 云照明 | _UseCloudLighting、_UseNormalMap、主光/间接光参数。 | 测试无光照、无贴图法线、存在法线三种路径。 |
| 雾 | _FogMode 选择 Horizon 或 Scene；使用 _FogInt 等参数。 | 比较相机距离、视线方向、场景 Fog 设置及 URP Fog variant。 |

表中是当前代码映射，不代表原版参数或画面已完整等价。遇到不同资产布局、UV、颜色空间或云图通道定义时，以源工程和目标场景证据为准。

## 4. 当前验证状态与限制

| 项目 | 状态 |
| --- | --- |
| 修改文件静态导入 | Editor.log 中记录本组 Shader 与 GUI C# 的导入 artifact；当前日志检索未发现以 CustomSkyPlane、CustomSkybox 或 PropertyGUI_Gradient.cs 为对象的编译错误。 |
| Shader 属性槽位 | 源码中 CustomSkyPlane 和 CustomSkybox 各只有一个 _CubeMap_D 和一个 _CubeMap_N 2D 属性，未发现 _SkyGradient_* 备用属性。 |
| 文本检查 | 对本组目标文件执行 git diff --check 通过；工作区整体有其他既存尾随空格，不能据全仓失败误判为本组失败。 |
| Unity Inspector 截图 | 未完成：Windows UI 自动化通道未连通，未取得本次实际 Inspector 截图。 |
| 临时材质烘焙试验 | 未完成独立 UI/资产验证；需按 5.2 节重新执行首次生成与重复更新。 |
| Renderer Data 序列化配置 | 已静态确认 Low/High/Ultra 三份 AssetRaw Renderer Data 引用同一 GUID 且 Active；事件值不同。尚未证明运行时 Quality/URP Asset 映射实际选中它们。 |
| 源 Shader 静态对照 | 已比较三支 Shader 的 Property 名、Pass Tag/Queue/Blend 和 Feature Tag；尚未做全量 HLSL 分支逐语句审查或源/目标运行画面对比。 |
| Frame Debugger 场景验证 | 未完成；需先确认运行时选中的 Renderer Data，再检查 CustomSkyboxForward Draw、排序和附件。 |
| 目标设备和构建 | 未完成。 |

Editor artifact 导入和静态源码检查只是证据，不等于 GUI 实际显示、目标设备 Shader 编译或场景视觉验收。

## 5. ProjectACG 复核清单

1. 在 Unity 中检查材质 Shader 名确为 Valkyria/Scene/CustomSkybox 或 Valkyria/Scene/CustomSkyPlane；每个昼夜组应只出现一个天空 Texture 字段，RGB/A 编辑器属于该字段的 Drawer。
2. 用测试材质拖入普通天空图，修改 RGB/A 后预览与烘焙；确认原图不变、输出 ramp 位于材质 Gradients/ 下、材质引用已更新，再次烘焙能改动派生图。
3. 当前使用 NoScaleoffset，确认美术流程无需可见 tiling 控件；如需可控 tiling，将其作为材质接口需求，并分别定义普通图与 ramp 行为。
4. 核对实际 Quality/URP Asset 引用哪份 Renderer Data。Low/Ultra 当前序列化事件为 250，High 为 400，LayerMask 均为全部 Layer；根据目标场景明确时序/Layer 策略后再用 Frame Debugger 检查 SceneView、Game、Overlay 相机与 Queue。
5. Frame Debugger 确认 CustomSkyboxForward Pass 被绘制，读取深度/颜色附件正确，绘制顺序合理；再测试组合材质及独立 Plane/Cloud 材质。
6. 对 Property 名称已匹配的部分继续核对 Float 默认值、隐藏属性、Keyword、Blend 因子、颜色/Alpha 通道、UV、采样和所有 RenderState；重点回归独立 CloudSurface 被替换的 Blend/Cull Drawer 语义。
7. 在固定网格、材质、相机、时间、灯光和颜色空间下做原版/目标 A/B，逐项记录 API/版本差异、删改项、设备覆盖和所有未实现能力。

回退时恢复原 Shader 与材质引用、Renderer Data 配置及 .meta/GUID 映射；确认场景和材质不再依赖新资产后，才清理派生测试资源。
