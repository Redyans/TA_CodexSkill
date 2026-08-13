---
name: projectacg-substance-painter-shader-profile
description: ProjectACG 当前 Chara_Cloth_V2 Unity 与 Substance Painter 预览 Shader 的数据契约、Unity 预卷积 Cubemap Atlas、间接高光、Debug 和验证边界，不可直接迁移到其他项目。
---

# ProjectACG Unity 与 Substance Painter Shader 对齐 Profile

## 1. 适用范围与迁移边界

本 Profile 只适用于当前 ProjectACG 工作区中的 `Chara_Cloth_V2` Unity/Painter 对齐开发。跨项目方法以 [Unity 与 Substance Painter Shader 对齐实战参考](../../references/unity-substance-painter-parity.md) 为准；本文件记录当前项目路径、通道、公式、Painter 版本兼容、Atlas 布局、当前实现与验证边界。

本文件不是美术操作手册。Painter 安装、Texture Set 通道、导出模板和 Unity 导入步骤仍以源码旁的 `Assets/Shader/Character/Chara_V2/SP/Chara_Cloth_V2_SP_Readme.md` 为准。

以下内容不得直接迁移到其他项目：`_MRATex` 通道、`User1`、Unity 相机偏置半角向量、Atlas 最大 LOD/Face 方向/基轴修正、当前 Debug 数值、资产路径和 Painter 11.0.2 编译结论。

## 2. 当前版本与代码地图

| 项目 | 当前值 | Source of Truth |
| --- | --- | --- |
| 工作区 | `D:\work2025U3D\Valkyria\ProjectACGMain\ProjectACG\Client` | 当前实际检出的项目根目录 |
| Unity | `2022.3.62f3` | `ProjectSettings/ProjectVersion.txt` |
| URP | 内嵌 `14.0.12` | `Packages/manifest.json`、`Packages/packages-lock.json` |
| Painter | `11.0.2` 开发期实测 | Painter 实际导入/编译；升级后重测自动参数与环境 API |

| 职责 | 当前文件 |
| --- | --- |
| Unity 运行时目标 Shader | `Assets/Shader/Character/Chara_V2/Chara_Cloth_V2.shader` |
| Unity 材质 Binding | `Assets/Shader/Character/Chara_V2/Chara_Cloth_V2/Chara_Cloth_V2_Bindings.hlsl` |
| Painter 预览 Shader | `Assets/Shader/Character/Chara_V2/SP/Chara_Cloth_V2_SP.glsl` |
| 美术使用、通道与导出说明 | `Assets/Shader/Character/Chara_V2/SP/Chara_Cloth_V2_SP_Readme.md` |
| Unity-SP 参数与贴图桥接 | `Assets/Editor/TA_Tools/Render/PainterMaterialParameterBridge/` |
| Shader Adapter | `chara-cloth-v2@2`；参数 Profile Schema 仍为 v1 |
| Painter Bridge 源码版本 | `2.6.0`；当前运行版本以连接握手为准 |
| Unity 预卷积 Atlas 工具 | `Assets/Editor/TA_Tools/Render/UnitySpecCubeAtlasBaker/` |
| Atlas 工具功能 README | `Assets/Editor/TA_Tools/Render/UnitySpecCubeAtlasBaker/README_UnitySpecCubeAtlasBaker.md` |
| Unity Debug UI 顺序 | `Assets/Editor/TA_Tools/Render/CharacterShaderDebug/CharacterShaderRenderingDebugger.cs` |
| Unity Debug 常量与输出 | `Assets/Shader/Character/Chara_V2/Common/Chara_ShaderDebug_V2.hlsl` |
| 最小 Unity 颜色/光照 Harness | `Assets/TA_Test/map_Character_Display/SubstancePainter/BlinnPhong_ColorParity_Unity.shader` |
| 最小 Painter 颜色/光照 Harness | `Assets/TA_Test/map_Character_Display/SubstancePainter/BlinnPhong_ColorParity_SP.glsl` |

代码、材质或 Painter Shader 与本 Profile 不一致时，以当前源码和实际导入结果为准，并修订本文；不能用本文覆盖当前代码事实。

`README_UnitySpecCubeAtlasBaker.md` 的“对齐边界”目前仍保留“翻查询方向 / Cubemap Y 对齐”的旧表述，与当前 `Chara_Cloth_V2_SP.glsl` 的 Face-local 存储 V 转换不一致。在功能 README 同步前，方向契约以当前 Shader 源码和 `Chara_Cloth_V2_SP_Readme.md` 第 16 节为准；不得按旧 README 取反世界方向 Y。

## 3. 当前材质数据契约

### PRJ-SP-01｜制作阶段使用独立通道

正式制作通道为：

```text
Base Color
Normal
Metallic
Roughness
Ambient Occlusion
Emissive
Opacity
User1 / Reflectivity
```

当前 `_MRATex` 最终契约：

```text
_MRATex.R = Metallic
_MRATex.G = Smoothness = 1 - Roughness
_MRATex.B = Ambient Occlusion
_MRATex.A = Reflectivity = User1
```

正式 Painter 工程不添加 `User0`。当前 `Chara_Cloth_V2_SP.glsl` 仍保留旧 `User0.RGB` 兼容读取；只要 `User0` 存在，它就会覆盖原生 Metallic、Roughness 和 AO，造成美术修改原生通道却看不到变化。该兼容路径不是正式制作入口。

`User0.A` 不承担 Reflectivity。Sparse Alpha 在当前采样路径中还用于覆盖/有效性，Reflectivity 使用独立灰度 `User1`。

### PRJ-SP-02｜只在导出时打包

项目 Export Preset 应满足：

| 输出 | Painter 来源 | 操作 |
| --- | --- | --- |
| `_BaseTex.rgb` | Base Color | 直接输出，Unity 作为 sRGB |
| `_BaseTex.a` | Opacity | 直接输出 |
| `_NormalMap.rgb` | Normal OpenGL | 直接输出，Unity 设为 Normal Map |
| `_MRATex.r` | Metallic | 直接输出 |
| `_MRATex.g` | Roughness | 反相为 `1 - Roughness` |
| `_MRATex.b` | Ambient Occlusion | 直接输出 |
| `_MRATex.a` | User1 | 直接输出 Reflectivity |
| `_EmissionTex.rgb` | Emissive | 直接输出，Unity 作为 sRGB 颜色 |

Unity 中 `_MRATex` 必须关闭 sRGB 并保留 Alpha。Painter Shader Settings 的强度、对比度、颜色、灯光和环境参数只影响预览，不会自动写入导出贴图；Unity 材质必须继续保存同样的参数。

### PRJ-SP-03｜正式预览开关

制作阶段确认：

```text
Painter：是否需要材质贴图 = 开启
Unity：_IsNeedOrmTex = 1
```

关闭后使用的是参数常量，容易误判为 Painter 通道无响应。Normal、Alpha Test、Ramp、Unity 相机偏置高光等开关也必须按当前比较目的显式冻结，不能只比较截图里的数值。

## 4. Ramp 问题与当前修正

### 4.1 空 Ramp 让漫反射看似不响应灯光

已出现现象：未指定有效 Ramp 时，Painter 空纹理 Alpha 返回 `1`，`remapNoL` 被推向全亮，旋转主光几乎看不到变化。

当前修正：`Chara_Cloth_V2_SP.glsl` 提供“使用渐变贴图”开关；无有效 Ramp 时关闭，回退到未被空白 Alpha 锁死的明暗路径。正式对齐 Unity Ramp 时再开启。

### 4.2 同一 Ramp 在 Painter 偏淡

根因：Unity 将 `_RampTex` 作为 sRGB 颜色贴图，GPU 采样后自动执行 sRGB → Linear；Painter 自定义纹理采样未自动提供相同结果。

当前修正：`sampleRamp()` 只对 Ramp RGB 执行精确 sRGB → Linear；Alpha 继续作为线性 Toon 重映射曲线，不做 Gamma。

### 4.3 Ramp A 混合为 1 时出现小黑点

根因：Painter 自定义纹理可能 Repeat；Ramp 首尾颜色跨度大时，Bilinear 在边界跨端采样。

当前修正：使用 `textureSize()` 取得宽度，将 U Clamp 到首尾半 Texel 中心，再采样 `Y=0.5`。不要通过降低 Ramp A 混合或涂改 Ramp 边界颜色掩盖采样问题。

## 5. 主光、背景旋转与直接高光

### PRJ-SP-04｜背景旋转是一比一数值契约

当前 Painter 自动参数：

```glsl
//: param auto environment_rotation
uniform float environment_rotation_cloth_v2;
```

Painter 11.0.2 中仅导入 `lib-env.glsl` 未保证裸变量存在，曾报：

```text
error C1503: undefined variable "environment_rotation"
```

显式声明自动参数后解决。当前换算为 `environment_rotation_cloth_v2 * 360`，并约定：

```text
0°   = +Z
90°  = +X
180° = -Z
```

“主光跟随背景旋转”开启时，主光水平角与背景数值一比一；关闭时才使用手动方位角。该规则不保证 HDRI 太阳的真实方位与背景零点相同。例如 `193°` 时主光水平投影与正面 `+Z` 的点积约为 `-0.974`；实际 `N·L` 还受仰角影响，但灯仍位于背面，正面无光属于方向结果。

### PRJ-SP-05｜严格高光对比需开启 Unity 相机偏置

Unity `Chara_Cloth_V2` 当前直接高光不是标准 `normalize(L + V)`，而是包含 Camera Forward 的偏置半角向量。Painter 提供：

```text
直接高光 > 使用 Unity 原始相机偏置高光
```

当前默认关闭时使用标准半角向量，便于观察高光随主光旋转；要与 Unity 严格比较 Direct Specular 时必须开启。若该开关状态不同，即使 Base Color、Roughness、F0 和光参数完全相同，高光仍不会对齐。

对比顺序：世界法线 → 主光方向 → View Direction → Half Direction → Roughness → F0 → Direct Specular。不能从“参数面板数值相同”直接得出高光公式相同。

### PRJ-SP-06｜两端同步切换几何法线 Specular AA

Unity 与 Painter 现在都有同名的“启用高光抗锯齿（Specular AA）”开关，默认关闭。开启后两端都使用 `screenSpaceVariance = 0.125`、`threshold = 0.20` 的屏幕空间几何法线方差过滤，并将过滤后的最终 Roughness 同时用于 Roughness Debug、Direct Specular 和 Indirect Specular。

当前对齐的是不依赖法线贴图 Alpha 的简单版，不读取项目预烘法线方差数据。由于屏幕空间导数受视口分辨率、FOV、相机距离和模型画面占比影响，开启后仍必须尽量统一这些条件；只比较 Roughness → LOD 时应先让两端同时关闭。

### PRJ-SP-10｜新增高光功能按同一数据流对齐

当前 Unity `Chara_Cloth_V2` 新增的高光 Ramp、三层 GGX 各向异性高光和 MatCap 已在 Painter Shader 中建立对应实现。对齐对象不是同名参数本身，而是“输入纹理与颜色域 → 坐标基底 → View/Light 向量 → 遮罩 → BRDF/查找纹理 → 合成位置”的完整数据流。

| 功能 | Unity/Painter 当前共同契约 | 关键边界 |
| --- | --- | --- |
| 高光 Ramp | `_NeedSpecularRamp`、视角权重、强度、各向异性融合和各向异性参数共同决定 U；V 固定为 `0.5` | Ramp 是 sRGB 颜色纹理；Painter 只对 RGB 做一次 sRGB → Linear |
| 三层 GGX 各向异性 | 三层颜色 Alpha/锐度/宽度/偏移累加；总强度、粗糙度缩放、各向异性和方向旋转共享 | 使用原始 Eye Vector；不能复用普通 Cloth 高光的相机偏置 View 路径 |
| 法线驱动切线扰动 | `normalTS.x/y * _GGXAnisoNormalStrength` 分别扰动切线/副切线，再旋转方向 | Painter 必须从切线空间 X/Y/Z 单位轴重建完整 TBN |
| 噪声与断裂 | `_AnisoNoiseMap.R` 同时提供偏移噪声和 Breakup；Tiling/Offset 与 Unity `_ST` 一致 | 噪声是 Linear 数据，不做 Gamma |
| MatCap | 模型法线/贴图法线融合后转 View Space，再执行旋转、Flip Y、颜色、强度和底色影响 | MatCap RGB 是 sRGB；Painter 的 View-to-World 法线矩阵必须转置后用于 World-to-View |

高光 Ramp 不是只影响普通 Direct Specular：它先修改共享 `F0`，因此同一 `F0` 还会进入后续 IBL BRDF。若 Painter 只在 Direct 分支乘 Ramp，会出现 Direct Debug 对齐、Indirect Specular 仍不一致。

### PRJ-SP-11｜`_BaseTex.a` 是共享高光强度遮罩

当前 `_BaseTex.a` 对应 Painter `Opacity`。启用 `_UseAnisoStrengthMask` 后，`_AnisoStrengthMaskStrength` 与 `_AnisoStrengthMaskContrast` 生成 `sharedSpecularMask`，共同调制：

- 高光 Ramp 混合强度；
- 三层 GGX 各向异性高光；
- MatCap 高光。

该 Alpha 仍参与透明度/Alpha Test 和 `_DiffuseBlendEffect`。制作与导出时不能把它误当成只服务透明裁切的无关通道，也不能另造 Painter `User0.A` 代替。

## 6. 间接高光与 Unity 预卷积 Cubemap Atlas

### PRJ-SP-07｜严格路径使用 Unity 预卷积 Atlas

当前正式对齐路径是把 Unity `_EnvMap` 已预卷积的 LOD `0～6` 烘焙到 2D Atlas，再由 Painter Shader 直接采样。工具入口：

```text
TA_Tools > Render > Substance Painter Unity 环境 Atlas
```

源码位于 `Assets/Editor/TA_Tools/Render/UnitySpecCubeAtlasBaker/`。输出目录既可用 Project 文件夹 ObjectField 选择，也可直接填写路径；最终会生成 `*_UnitySpecCubeAtlas.exr` 和同名 JSON 元数据。

Atlas 数据契约：

- 每个 Mip 一行，Mip `0～6` 从下到上按 Unity 渲染目标/回读后的 2D 像素坐标保存；
- 每行面顺序固定为 `+X,-X,+Y,-Y,+Z,-Z`；
- `Face Padding` 默认 `2`，可设 `1～8`；Painter 必须填同一值；
- 中间 RT 使用 `R16G16B16A16_SFloat`、`sRGB=false`，导出 `RGBAHalf` EXR；
- JSON 记录尺寸、`baseFaceSize`、`maxMip`、`padding`、`faceOrder`、`colorEncoding`、Unity 版本和 `activeBuildTarget`；
- 若基础 Face 为 `1024`、Padding 为 `2`、最大 LOD 为 `6`，计算得到的 Atlas 尺寸为 `6168x2060`；这是布局示例，不代表仓库内已有对应输出资产。

工具采样的是当前 `activeBuildTarget` 下 Unity 实际导入的 Cubemap，并通过 `DecodeHDREnvironment` 写出 Linear HDR。切换到 Android 后，必须等待源 Cubemap 按 Android Override 重新导入，再使用材质 `_EnvMap` 的同一资产重新烘焙；不应把 PC 平台烘焙的 Atlas 当成 Android 实际采样结果。源 Cubemap 的平台格式、HDR 支持、压缩质量、Max Size 或 Mip 改变后也必须重烘。`RGBA Half` 可用作高保真/根因诊断基线，但不能因为它亮度更接近就默认为手机量产格式；移动端内存、带宽和目标 GPU 支持仍需单独评估。

该 Atlas 以 Painter `Texture` 资源导入，不是 Painter `Environment`，也不是交给宿主自动解析的标准单行 `6 Frames` 布局。`SP：使用 Unity 预卷积环境 Atlas` 开启后使用 Atlas；关闭时才通过 `envSampleLOD()` 使用 Painter 原生 Environment 做 A/B fallback。

两条采样路径之后都继续执行 Unity Cloth 的 `NoV + Roughness` IBL BRDF 拟合、F0/Reflectivity、多重散射/能量补偿、AO/Shadow 门控、`_EnvColor` 和 `_EnvLightStrength`。

### PRJ-SP-08｜旧 Log2 与 URP 感知曲线可同步切换

Unity 和 Painter 当前都提供“环境Mip映射模式（旧Log2 / URP感知）”材质参数，默认值为 `0`，保留项目原有 Log2 外观。两端必须选择同一模式。

旧 Log2 模式（`0`）：

```text
lod = max(0, log2(max(0.01, saturate(roughness))) * 1.2 + 5.0)
```

该曲线在 Roughness `0～1` 内访问 LOD `0～5`。URP 感知模式（`1`）：

```text
perceptualRoughness = saturate(roughness)
perceptualRoughness *= 1.7 - 0.7 * perceptualRoughness
lod = perceptualRoughness * 6
```

该曲线访问 LOD `0～6`。这里的 `0.7` 是公式 `1.7 - 0.7 * roughness` 的系数，不是“Painter Roughness 固定乘 `0.7`”校准。两种模式只切换 Roughness 到 LOD 曲线，Unity 共用 `sampler_EnvMap` 和 `DecodeHDREnvironment`，避免把 HDR 编码差异误判为 Mip 曲线差异。

Atlas 工具仍统一导出 LOD `0～6`：旧 Log2 模式不使用 LOD `6`，URP 模式会使用。切换时不需修改工具、JSON 或重新烘焙 Atlas。Painter 原生 Environment 只能把选中曲线归一化后近似映射到其 Mip 范围；严格对齐仍需开启 Unity Atlas。

当前 SP 方向链为：

```text
reflect(-viewDir, normalWS) -> Z 反转 -> _EnvRotation -> Atlas
```

`Direction -> Face UV` 使用 `+X,-X,+Y,-Y,+Z,-Z`；第三张保持 `+Y`，第四张保持 `-Y`。`SP：Unity/Painter Atlas Face V 对齐` 默认开启，只在选中 Face 内做存储 V 翻转；不取反世界方向 Y，也不翻转整张 Atlas。

每层 Mip 使用四个 `texelFetch` Tap 手动 Bilinear；Tap 越界时转回方向，再映射到相邻 Face。相邻两层 Mip 手动 Trilinear，所以不依赖 2D Atlas 自身的跨 Tile 过滤。

当前实现仍有两个必须保留的风险：

1. `fetchUnitySpecCubeAtlasTap()` 当前只读取 Face 内 Texel，越界时重投影到邻面，没有直接使用 Unity 已烘焙的 Padding；三面角点仍只能近似原生 seamless Cubemap filtering。
2. 当前 Tap 读取是 `texelFetch(...).rgb`，假设 Painter 传给 Shader 的就是 Linear HDR RGB。若 Painter 实际将 EXR 以 RGBM/UNorm 存储，这条路径尚未解码 Alpha 乘数；必须在每个 Tap 解码后再 Bilinear/Trilinear，不得在 RGBM 编码域直接混合。

### PRJ-SP-09｜Preview Link 只传输已对齐数据，不替代 Shader 对齐

当前 Unity-SP 桥接使用 `chara-cloth-v2@2`，可以双向同步 80 项白名单参数和临时贴图，并把 Unity 有效主光单向发送给 Painter。完整项目契约见 [Painter 材质预览桥接 Profile](../../../ToolDevelopment/Profiles/ProjectACG/painter-material-preview-bridge.md)。

Unity → Painter 使用 Unity 材质上的最终打包贴图，并额外临时传递 `_SpecularRampTex`、`_AnisoNoiseMap`、`_MatCapTex` 和 `_AnisoNoiseMap_ST`；Painter → Unity 才从 Painter 独立制作通道临时合并 `_BaseTex/_NormalMap/_MRATex/_EmissionTex`。Ramp、Noise、MatCap 和 Unity Atlas 都是预览辅助输入，不是 Painter 制作通道，不能在 SP → Unity 时伪装成绘制结果回传。该过程只用于效果 A/B，不改变 PRJ-SP-01/02 的分通道制作与正式导出契约。

灯光命令只覆盖 Painter 预览 Shader 可表达的主光方向、线性颜色和强度；相机、背景、Environment、Debug、RendererFeature、Timeline MPB 和其他当前未同步状态仍需手动冻结。参数或贴图传输成功不等于 Direct/Indirect/Final 已对齐，最终判断继续按本 Profile 的 Debug 与最小 Harness 顺序执行。

## 7. Debug 对齐契约

Debug UI 的 Source of Truth 是 `CharacterShaderRenderingDebugger.cs` 中的 `DebugModeOrder`，底层值以 `Chara_ShaderDebug_V2.hlsl` 为准。当前 Painter 名称、顺序和数值为：

```text
Off
Base Color
normal（原始）
Metallic (原始)
Smoothness (原始)
Roughness (原始)
AO (原始)
normal（调整后）
Metallic (调整后)
Roughness (调整后)
AO (调整后)
Direct Diffuse
Direct Specular
Indirect Diffuse
Indirect Specular
Final Color Gradient
Final Grayscale
normal（世界空间）
Smooth Normal（顶点色）
```

Painter 不具备以下 Unity 专有输入，当前用洋红色显式表示不支持：

- `Final Color Gradient`；
- `Smooth Normal（顶点色）`。

不能使用这两个模式判断 Unity/Painter 已对齐，也不能用一个伪造的灰度/法线结果替代“不支持”。

`Direct Specular` 必须输出普通 Cloth Specular、三层 GGX 各向异性高光和 MatCap 的合计值；否则新增分支可能在 Final 中明显不同，却被旧 Debug 错误地显示为“直接高光已经一致”。`Indirect Specular` 则继续包含高光 Ramp 修改后的共享 `F0`，但不重复叠加 GGX 各向异性或 MatCap 直接光结果。

## 8. 最小 Harness 的使用结论

当前项目已提供成对的 `BlinnPhong_ColorParity` Shader。它们使用同一 Base Color、可选 Normal、手动光方向和 Blinn-Phong 公式，不接 Ramp、IBL、阴影、Fog、Specular AA 或角色全局参数。

排查顺序：

1. 两端开启 `Base Color Only`，关闭全部 Tone Mapping / Color Profile；
2. 若颜色不一致，先查贴图 sRGB、HDR 颜色值和显示编码；
3. 关闭 `Base Color Only`，用相同灯光检查 Direct Diffuse；
4. 再检查 Direct Specular；
5. 最小 Harness 一致后才回到 `Chara_Cloth_V2`，逐项恢复 Ramp、IBL 和后处理。

曾出现 Painter 最终颜色更饱和，不能仅归因于“HDR 颜色”。若加载 LUT 后才出现，优先检查 Color Profile 是否输出了 sRGB 显示编码，以及 Painter 是否又叠加自己的 Tone Mapping / Color Correction。

## 9. 编译与实现问题记录

| 错误/现象 | 根因 | 当前处理 |
| --- | --- | --- |
| `C1503 undefined variable "environment_rotation"` | 主 Shader 未显式声明 Painter 自动参数 | 增加 `param auto environment_rotation` uniform |
| `C1038 redeclaration`，变量 `painterRoughness` | 同一 `shade()` 作用域重复声明 | Environment 局部变量改为 `environmentPerceptualRoughness` |
| 原生 Roughness 修改无反应 | Texture Set 中存在旧 `User0`，覆盖原生通道 | 正式工程移除 `User0` |
| Roughness Debug 有变化，Indirect Specular 一直像镜面 | 问题位于 Environment 采样/LOD，不在材质输入 | 原生路径用 `envSampleLOD()` 验证；严格路径切 Unity Atlas，不改材质 Roughness |
| 多个连续 Roughness 档位效果完全一致 | 层级过早取整或只取单层 Mip | Atlas 保留小数 LOD，手动混合相邻两层 Mip |
| Atlas 第三/第四面上下对调 | 用世界方向 Y 翻转补偿 2D 纹理 V 原点 | 第三面保持 `+Y`、第四面保持 `-Y`；只翻 Face-local 存储 V |
| 高 Roughness 出现六面接缝 | 2D Atlas 边界过滤不知 Cubemap 邻接 | 四 Tap 跨面重投影 + 手动 Bilinear/Trilinear；仍保留“未直读 Unity Padding”的角点风险 |
| Painter 环境反射特征看起来更大 | 相机 FOV/轴、Aspect、距离或模型屏幕占比不一致 | 优先导入同一相机并叠图，不改 Atlas UV 或 `_EnvRotation` |
| 切 Android 后 Cubemap 变亮 | 平台 Override 改变了 HDR 格式/压缩/Mip 和实际 GPU 输入 | 重新导入并在 Android target 重烘 Atlas；`RGBA Half` 仅作高保真基线，量产格式另做机型验证 |
| Atlas 采样过白/偏色 | Painter 可能以 RGBM/UNorm 保存 EXR，当前 `texelFetch(...).rgb` 未解码 Alpha | 先确认 Painter 实际 GPU 编码；如为 RGBM，每个 Tap 解码后再过滤 |
| 两端参数相同但高光不一致 | Unity 使用相机偏置半角向量，Painter 开关未开启 | 严格对比时开启 Unity 原始相机偏置高光 |
| 普通高光一致，丝织高光方向或宽度不同 | Painter TBN、原始 Eye Vector、方向旋转或法线切线扰动未按 Unity 重建 | 分别 Debug T/B/N、raw V、H 和三层 Lobe；不要只对照面板参数 |
| Direct Specular 一致但 IBL 颜色仍不同 | 高光 Ramp 只乘了 Direct 结果，没有在共享 F0 阶段修改 | 在 F0 阶段应用 Ramp，让后续 IBL BRDF 读取同一 F0 |
| MatCap 上下/旋转不一致 | 把 Painter View-to-World 矩阵直接当 World-to-View，或纹理 Y 约定不同 | 转置法线矩阵，并单独验证 Rotation 与 Flip Y |
| Base Alpha 改变后多个高光分支不同步 | Unity 使用共享 Alpha 遮罩，Painter 只调制某一个分支 | 用同一 `sharedSpecularMask` 调制 Ramp、各向异性和 MatCap |
| Ramp 偏淡 | Painter Ramp RGB 未按 Unity sRGB 采样语义解码 | Ramp RGB sRGB → Linear，Alpha 不变 |
| Ramp 黑点 | Repeat/Bilinear 跨边界 | U Clamp 到半 Texel 中心 |

新增 Painter 局部变量前必须搜索整个 `shade()` 作用域；新增自动参数时必须在 Painter 11.0.2 实际导入编译，不能用静态文本检查替代。

## 10. 当前预览边界

当前 Painter Shader 不完整模拟：

- Unity Main Light ShadowMap 与 Per Object Shadow；
- Forward/Forward+ 额外灯和特效平行光；
- Fresnel Rim、Depth Rim、Outline、Planar Shadow；
- RendererFeature、Character Render Controller 和 Timeline 全局控制；
- Death、最终高度渐变、顶点色 Smooth Normal；
- Unity Volume / Tone Mapping / Color Profile，除非另行加载烘焙 LUT。

当前 Unity `Chara_Cloth_V2` Forward Fragment 的 Alpha Test 是否完整执行仍有实现边界；Painter Alpha Test 只作为遮罩预览，不能替代 Unity 最终裁切验收。

## 11. 验证状态与收口清单

已由源码静态确认：通道契约、Ramp 修正、自动参数声明、Debug 映射、相机偏置高光开关、Unity/SP 同名几何法线 Specular AA 开关、旧 Log2 / URP 感知 Mip 映射开关、Atlas 工具入口/输出格式/元数据、LOD `0～6` 布局、`+X,-X,+Y,-Y,+Z,-Z` 面顺序、Face-local V 对齐、四 Tap 手动 Bilinear、相邻 Mip 手动 Trilinear 与 Unity Atlas/Painter Environment A/B 分支；以及高光 Ramp、Base Alpha 共享遮罩、三层 GGX 各向异性、法线切线扰动、噪声/Breakup、MatCap 和 Direct Specular Debug 合成。

桥接静态验证已确认：C# 与 JavaScript Descriptor 均为 80 个且 ID 一致，新增 37 个字段在 Unity/Painter 两端存在；`chara-cloth-v2@2`、Bridge `2.6.0` 与参数 Profile Schema v1 的职责分离明确；`node --check bridge.js`、`plugin.json` 解析、112 个 GLSL 参数元数据 JSON 块、括号/大括号平衡和 `git diff --check` 均通过。此前参数桥接独立 C# 构建为 0 Warning/0 Error；本次文档收口重跑完整 `Assembly-CSharp-Editor.csproj --no-restore` 时，因 `Temp/bin/Debug` 缺少 206 个 Unity 生成 DLL 而在源码编译前失败，不能替代 Unity Editor 编译。

仍需在实际环境完成：

- Painter 11.0.2 最终 Shader 重新导入无错误；
- 在 Painter 中重新安装 Bridge `2.6.0`、完整重启并重新选择 `Chara_Cloth_V2_SP`；仅修改工程源码不代表运行时已加载新版；
- 使用同模型与灯光逐项 A/B 高光 Ramp、三层 GGX 各向异性、Alpha 遮罩、Noise/Breakup 和 MatCap；重点验证切线方向、MatCap Flip Y、Direct Specular Debug 与 IBL F0 影响；
- 当前仓库未检出已输出的 `*_UnitySpecCubeAtlas.json`；需用实际目标 Cubemap 烘焙一次并复核 JSON 与 Painter 参数；
- 同模型、姿态、导入相机、HDRI、曝光下的 Direct/Indirect Debug 截图 A/B；
- 两端同时选择旧 Log2，再同时选择 URP 感知模式；分别按 Roughness `0～1` 每 `0.1` 一档，跨多个代表性 HDRI 验证高光宽度、相邻 Mip 过渡和高 Roughness 接缝；
- 高亮贴边/三面角点场景中，对比当前邻面重投影与直接读取 Unity 预烘焙 Padding；
- 用超过 `1.0` 的灰阶/彩色高光确认 Painter 导入 EXR 的实际 GPU 编码；当前未实现 RGBM Tap 解码；
- Android 目标下用最终候选 Cubemap 平台格式重烘，与 `RGBA Half` 诊断基线做亮度、Mip 和性能对比；
- Unity 与 Painter 同时加载/关闭 Color Profile 的成对颜色验证。

Unity 场景画面仍是最终验收标准。若 Painter 只能近似某个运行时专有输入，应记录边界，不继续修改 Base Color、Roughness 或导出贴图来抵消。
