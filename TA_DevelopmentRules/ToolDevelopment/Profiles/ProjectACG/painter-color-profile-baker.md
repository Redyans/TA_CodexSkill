---
name: projectacg-painter-color-profile-baker
description: ProjectACG Painter Color Profile Baker 的工具边界、输入输出域、White Point、sRGB、路径选择、黑图排查和当前验证状态。
---

# ProjectACG Painter Color Profile Baker Profile

## 1. 适用范围与结论

本 Profile 记录 ProjectACG 将 Unity URP 的逐像素颜色变换烘焙为 Substance 3D Painter `Color Profile` EXR 的项目事实。通用工具约束以 [ProjectACG TA_Tools Profile](README_Tech_ProjectACGTAToolsProfile.md) 和 [TA 工具开发 CORE](../../README_Tech_TAToolDevelopmentRules.md) 为准；单个/批量工作区、计划预检、进度与安全取消见 [Editor 批量烘焙工作区与安全取消参考](../../references/batch-baker-workspace-and-cancellation.md)；材质 Shader 对齐方法以 [Unity 与 Substance Painter Shader 对齐实战参考](../../../ShaderDevelopment/references/unity-substance-painter-parity.md) 为准。

直接结论：该工具适合烘焙可表达为逐像素 `RGB → RGB` 的 Tone Mapping 和调色链，不适合 Bloom、Fog、轮廓、阴影、深度或角色/场景同时存在的屏幕分支。标准 Painter SDR 使用必须开启“输出 sRGB 显示编码”，并让 Unity 工具与 Painter 使用完全相同的 White Point。

## 2. 当前版本、入口与代码地图

| 项目 | 当前值 | Source of Truth |
| --- | --- | --- |
| Unity | `2022.3.62f3` | `ProjectSettings/ProjectVersion.txt` |
| URP | 内嵌 `14.0.12` | `Packages/manifest.json`、`Packages/packages-lock.json` |
| 菜单 | `TA_Tools/Render/Substance Painter 调色预览 LUT` | `PainterColorProfileBakerWindow.cs` |
| 工具目录 | `Assets/Editor/TA_Tools/Render/PainterColorProfileBaker/` | 当前资产目录 |
| Editor asmdef | `ProjectACG.TATools.Render.PainterColorProfileBaker.Editor` | 同目录 asmdef |

| 职责 | 文件 |
| --- | --- |
| Window、单个/批量工作区、计划预检、路径与结果汇总 | `PainterColorProfileBakerWindow.cs` |
| Volume 解析、URP LUT、阶段进度、Readback、结果校验与 EXR 输出 | `PainterColorProfileBakeUtility.cs` |
| Identity 反推、颜色链与显示编码 | `PainterColorProfileExport.compute` |
| 美术操作说明 | `README_Art_PainterColorProfileBaker.md` |
| 技术实现与升级检查 | `README_Tech_PainterColorProfileBaker.md` |

该工具位于历史 `Render` 类别并使用 `OdinEditorWindow`，属于现有工具的兼容事实，不作为新工具目录或 UI 框架的通用模板。工具独立在 Editor-only asmdef 中，不进入 Player。

## 3. 可烘焙和不可烘焙边界

### 3.1 可烘焙：同一像素的颜色映射

- 项目 Custom Tonemapping 与 URP Tonemapping；
- Post Exposure；
- Color Adjustments；
- White Balance；
- Channel Mixer；
- Color Curves；
- Lift / Gamma / Gain；
- Shadows / Midtones / Highlights；
- Split Toning；
- Color Lookup。

工具复用当前内嵌 URP 的 `LutBuilderLdr/Hdr`、`PostProcessPass.ApplyColorGrading`、Common HLSL，以及项目 Hable/LogC 3D LUT 逻辑。升级 URP 后必须重新对比工具 Tech README 列出的包内 Source of Truth。

### 3.2 不可烘焙：需要空间、邻域、时间或物体分类

- Bloom、Vignette、Depth of Field、Film Grain；
- Fog、Outline、屏幕 Mask；
- 主光阴影、Per Object Shadow、AO 等几何/屏幕结果；
- 依赖深度、法线、屏幕坐标、相邻像素或历史帧的效果；
- 角色与场景同时存在的屏幕分支曝光。

一张 RGB LUT 只能选择角色或场景其中一个曝光分支，不能在 Painter 中同时复现基于屏幕分类的两套曝光。

## 4. 标准烘焙流程

### 4.1 单个烘焙

1. 确认场景实际使用的 `Volume Profile` 资产，不从截图或同名副本猜测。
2. 检查目标组件 `active`，以及每个要烘焙参数左侧的 Override。
3. 将 Adobe 官方 `color_profile_linear.exr` 导入 Unity并放入 `Painter Identity LUT`。
4. 显式填写 White Point；不要依赖 Window 序列化默认值。
5. 普通 Painter SDR 预览保持“输出 sRGB 显示编码”开启。
6. 通过 Project 文件夹 ObjectField 或直接路径设置输出目录。
7. 点击烘焙，在保存窗口确认最终文件名。
8. Painter 导入生成 EXR，Resource Usage 选择 `colorlut`。
9. `Display Settings` 开启 `Activate Color Profile` 并选择该 EXR。
10. Painter White Point 填写与 Unity 工具完全相同的值。
11. 关闭 Painter 自己额外的 Color Correction 和非中性 Tone Mapping，避免重复处理。

### 4.2 批量烘焙

1. 将顶部模式切换为“批量烘焙”。
2. 直接拖入多个 `Volume Profile`，或在 Project 中多选后点击“添加 Project 当前选中的 Profile”。
3. 所有项共用 `Painter Identity LUT`、White Point、曝光分支、sRGB 编码和输出目录；这些配置在计划构建完成后冻结到每个请求中。
4. 检查“批量预检”。空项、重复 Profile、Identity 格式错误、输出目录无效，以及清洗文件名后的输出冲突都会阻断执行。
5. 点击“批量烘焙 Painter Color Profile EXR”。每项自动输出 `<Profile名>_PainterColorProfile.exr`；批量过程不逐项打开保存窗口。
6. 若已有同名 EXR，工具在开始前统一显示覆盖数量并只确认一次。
7. 进度条显示当前序号、Profile 和真实烘焙阶段。用户取消时停止当前项及后续项，此前完成的 EXR 保留。
8. 单项异常记录为失败并继续后续项；结束后汇总总数、成功、失败与未处理数量。

当前 Utility 的安全取消检查点位于 Volume 解析、内部 LUT 构建前、Painter LUT 转换前、Readback/校验前和 EXR 写入前。GPU LUT 构建、同步 Readback、EXR 编码和 `AssetDatabase.Refresh` 属于主线程阻塞阶段；取消请求要等该阶段返回后才能生效。若用户在“完成”阶段提出取消，当前已写入项计为成功，只停止后续项。

## 5. Identity LUT 契约

使用 Adobe 官方：

```text
color_profile_linear.exr
```

当前项目资产位于：

```text
Assets/Shader/color_profile_linear.exr
```

导入后的实际 GPU 资源要求：

```text
尺寸：2048 x 128
格式：R16G16B16A16_SFloat / RGBA Half
sRGB GraphicsFormat：否
压缩：否
Mip：关闭
Max Size：至少 2048
```

工具以 `Texture.graphicsFormat` 为准，检查非 sRGB、非压缩和 IEEE 754 浮点格式。Unity 2022 对 EXR 可能隐藏普通纹理的 sRGB/Compression 选项，`.meta` 中的 `sRGBTexture` 或 `textureCompression` 也不一定代表 EXR 最终 GPU 格式；不能只看 `.meta` 字段判定。PNG 等整数 LUT 不能替代该浮点 EXR。

## 6. White Point 输入域

Painter 在 Color Profile 前近似执行：

```text
normalized = clamp(sceneLinearHDR / WhitePoint, 0, 1)
```

烘焙器根据 Identity LUT 反推：

```text
sceneLinearHDR = identity.rgb * WhitePoint
```

因此：

- Unity 工具与 Painter White Point 必须完全相同；
- White Point 以上的 HDR 输入会在进入 LUT 前截断；
- 数值过小会切掉高光，过大会把有限 LUT 采样精度浪费在没有实际输入的范围；
- White Point 不是曝光、亮度补偿或“越大越 HDR”的参数。

选择方法：先用最小 `BlinnPhong_ColorParity` 或正式 Shader 输出线性灰阶/HDR 色块：

```text
0 / 0.18 / 0.5 / 1 / 2 / 4 / 8 / 16
```

选能覆盖有效 Tone Mapping 输入范围的最小值，并检查高光曲线是否在目标值前进入稳定压缩区。

当前 Art README 记录过 `LookDevVolume-HDRIOnly` 的开发期建议值 `1`；Window 代码默认值仍为 `16`。这两个值不能被当成自动正确配置：每次烘焙都应显式设置并在 Profile 或 Shader 输出变化后重新验证。`1` 只保留为此前测试配置的候选，最小 Blinn-Phong Harness 的高光输出可能超过 `1`。

## 7. “输出 sRGB 显示编码”的规则

当前 Compute Shader 仅在该开关开启时执行：

```hlsl
GetLinearToSRGB(saturate(outputColor))
```

### 标准情况：必须开启

普通 SDR 显示器、Painter 默认显示目标和本文标准 `colorlut` 流程都保持开启。关闭后，Linear 数值会被直接当成显示颜色，常见结果是：

- 画面更暗；
- 红色更纯；
- 饱和度看起来更高；
- Unity 与 Painter 的中灰和肤色明显分离。

### 只有以下情况才关闭

- 下游自定义管线明确会再执行一次 Linear → 显示编码；
- 输出用于离线 HDR/Linear 合成而不是 Painter 默认 SDR 显示；
- 输出只用于数值分析，比较的是线性值而不是最终画面。

不得因为 Unity 工程使用 Linear Color Space、Identity/输出是 EXR、Shader 使用 HDR Color，或 White Point 大于 `1` 而关闭。也不得在工具和 Painter 两端都做一遍额外编码/Color Correction，避免重复 Gamma。

## 8. Volume Profile 解析与 `LookDevVolume-HDRIOnly`

工具为每个支持的 VolumeComponent 创建默认实例，只在源 Profile 中对应组件 `active = true` 时调用 Override；`Override` 只把 `overrideState = true` 的参数以权重 `1` 写入。因此“Inspector 里看到了数值”不等于会进入 LUT。

排查“不生效”时依次确认：

1. 组件本身是否 Active；
2. 参数左侧 Override 是否勾选；
3. 修改是否保存到当前选择的 Profile 资产；
4. 工具选中的 Profile 是否就是场景实际使用的资产；
5. 当前 URP Asset 的 Color Grading Mode 和 LUT Size 是否有效。

当前目标资产：

```text
Assets/TA_Test/LYJ/Enverment/Env/Volumes/LookDevVolume-HDRIOnly.asset
```

2026-07-30 的静态资产检查显示，该文件所引用的项目 Custom Tonemapping 和 Color Adjustments 组件当前均为 `active: 0`；这可能是开发调试中的临时状态。此观察不作为永久配置，但意味着当前直接选择该资产烘焙时，不能根据旧截图或 Art README 假定这些效果一定生效。每次正式烘焙前必须在 Unity Inspector 复核并保存目标资产。

## 9. 输出路径 UI 契约

工具同时支持：

- `输出文件夹`：Project 内文件夹 `ObjectField`；
- `输出路径`：直接输入或粘贴 `Assets/...`、工程相对路径、绝对路径；
- 保存窗口：选择最终文件名。

行为规则：

- ObjectField 选择项目文件夹后，会同步路径文本；
- 输入能映射回 `Assets/...` 的路径时，会同步 ObjectField；
- 外部绝对目录没有 Unity 资源对象，ObjectField 为空是正常行为；
- 路径必须已存在，工具不隐式创建任意外部目录；
- 相对路径以 Unity 工程根目录解析；
- 单个模式的最终保存窗口会更新下一次默认输出位置；
- 批量模式直接使用已解析目录和清洗后的自动文件名，不弹逐项保存窗口；
- 同一批次的完整输出路径按不区分大小写去重，避免不同 Profile 名在清洗后覆盖同一 EXR。

不能要求美术只手输裸路径，也不能把外部绝对路径的空 ObjectField 误判为路径丢失。

## 10. 已出现问题与排查表

| 现象 | 首要检查 | 处理 |
| --- | --- | --- |
| 生成黑图 | Unity Console 的 LUT Builder/Compute 错误、Identity 格式、有效 URP Asset、Volume 解析、GPU Readback | 当前工具检测 NaN/Infinity 和 RGB 全黑；异常时停止写 EXR，不输出伪成功文件 |
| 烘焙后 Painter 饱和度明显更高 | “输出 sRGB 显示编码”是否关闭，Painter 是否重复 Tone Mapping | 标准 SDR 开启 sRGB 输出，只保留一次显示编码 |
| 画面比 Unity 暗、红色更纯 | Linear LUT 值被直接当显示值 | 开启 sRGB 输出；用中灰和 RGB 色块验证 |
| 高光被截断或大面积同色 | White Point 太低 | 提高到最小覆盖值，并在 Unity/Painter 同步 |
| White Point 提高后渐变精度变差 | 取值远大于实际输入域 | 降到仍能覆盖有效输入的最小值 |
| 设置了参数但 LUT 无变化 | 组件 inactive、Override 未勾选或选错 Profile | 按第 8 节逐项确认，不能只看 Inspector 数值 |
| Bloom/Fog/轮廓没烘进去 | 效果依赖空间、深度、邻域或物体分类 | 接受工具边界，在 Painter 单独预览或回 Unity 验收 |
| 角色/场景曝光只能对一个 | 单张 RGB LUT 无屏幕分类信息 | 在工具中选目标分支，分别生成不同用途的 LUT |
| ObjectField 为空但绝对路径有效 | 外部目录无法映射 Unity Asset | 以“实际输出目录”状态为准，不需要伪造 Asset 对象 |
| 输出看似重复调色 | Painter 额外 Color Correction/Tone Mapping 仍开启 | 关闭重复处理，只保留烘焙 Color Profile |
| 批量按钮不可点击 | 预检发现空项、重复源、Identity 格式、输出目录或输出名冲突 | 查看“批量预检”的第一个阻断原因；按钮禁用与执行使用同一个计划构建函数 |
| 点击取消后没有立即停止 | 当前正在执行 GPU LUT、Readback、EXR 编码或 Unity 刷新 | 等待当前阻塞调用返回；取消在下一个安全检查点生效 |
| 某一项失败但批次继续 | 失败被记录为逐项错误，而不是全局依赖失效 | 结束后查看批量状态；有效项继续生成，不能把批次误报为全部成功 |

早期“黑图”没有足够证据归因于某一个唯一根因。文档只保留可执行排查链，不把未复现猜测写成已确认结论。

## 11. 黑图的最小诊断闭环

按以下顺序停止扩大搜索：

1. 使用无调色 Profile、White Point `1`、sRGB 输出开启；
2. 烘焙官方 Identity，检查工具是否成功写出非全黑 EXR；
3. Painter 加载后，Color Profile 开/关应保持中性结果；
4. 用 RGB 色块确认 LUT 排列、上下方向和 Gamma；
5. 再逐个开启 Tonemapping、Post Exposure、Color Adjustments；
6. 最后切换正式 `LookDevVolume-HDRIOnly`、HDRI 和角色材质。

若第 2 步失败，检查 Console、Identity GPU 格式、Compute Shader 和有效 URP Asset；不要先调 White Point、曝光或 Painter Shader 材质参数。

## 12. 验证矩阵

| 阶段 | 输入 | 通过条件 |
| --- | --- | --- |
| Identity | 无调色 Profile、White Point `1` | Color Profile 开/关中性，无黑图 |
| 灰阶 | `0/0.18/0.5/1/2/4/8/16` | 曝光、Tone Mapping、截断和 White Point 边界可解释 |
| RGB | 纯 R/G/B、灰和互补色 | 无通道错位、上下翻转或重复 Gamma |
| Volume | 每次只启用一个组件/Override | LUT 变化与组件一致，未勾选参数不进入结果 |
| 路径 | Project ObjectField、`Assets/...`、相对和绝对路径 | 同步、解析、保存和 Reveal 行为正确 |
| 批量计划 | 空项、重复 Profile、同名/非法字符名、已有输出 | 预检、按钮状态、输出去重和覆盖数量一致 |
| 批量取消 | 首项前、Readback 后、写入前、完成阶段 | 已完成 EXR 保留；当前/后续项计数与落盘文件一致；进度条最终清理 |
| 批量失败 | 中间项无效或烘焙异常 | 失败项可定位，后续有效项继续，汇总不伪报全成功 |
| 最终 | 正式 Profile、同 Shader、HDRI、相机和曝光 | Painter 与 Unity 剩余差异可归因到 Shader/IBL/空间效果边界 |

## 13. 当前验证状态与剩余风险

历史实现阶段已完成独立 Editor 程序集构建、Unity Bee 导入、Compute Shader 导入、UTF-8/尾随空白和 asmdef JSON 静态检查。当前代码还具备：Identity 实际 GraphicsFormat 校验、有效 URP Asset 校验、有限浮点校验、全黑输出保护、顶部单个/批量工作区、共享计划预检、自动输出命名、覆盖确认、阶段进度、安全取消和逐项失败汇总。全黑保护只拒绝 RGB 全为精确 `0` 的结果，不会判断“数值非零但视觉几乎全黑”或曲线错误，因此仍需灰阶/RGB 视觉验证。

2026-08-05 定向构建 `ProjectACG.TATools.Render.PainterColorProfileBaker.Editor.csproj --no-restore -p:BuildProjectReferences=false` 为 `0 Warning / 0 Error`，目标路径 `git diff --check` 通过。该证据确认 C# 与 Odin 属性可编译，不证明批量 GPU 输出和目标 Painter 结果正确。

以下仍不能由静态检查替代：

- 当前 Unity 实例实际点击 Bake；
- 使用至少两个 Profile 执行完整批次，并在首项前、Readback 后和写入前验证取消计数与文件边界；
- 验证中间项失败后后续项继续，以及同名/非法字符 Profile 的输出冲突预检；
- 生成 EXR 在当前 Painter 版本中以 `colorlut` 导入；
- Identity、灰阶、RGB、HDR 色块与正式角色的 A/B；
- `LookDevVolume-HDRIOnly` 当前 Active/Override 状态确认；
- White Point 最终值与 `0.7` Environment 校准共同作用下的视觉验收。

工具正确生成 EXR不等于材质、环境和后处理整体已经对齐。最终验收必须同时执行 Shader Profile 的 Direct/Indirect Debug 流程。
