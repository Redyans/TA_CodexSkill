# ProjectACG Shader 项目经验｜2026-11-09

> 类型：PROFILE；统计范围：北京时间 2026-11-09 00:00（含）至 2026-11-10 00:00（不含）。仅复述指定 sessions 中的当日事实，不代表当前工程状态。
>
> 本次直接可见的是消息文本、用户纠正和转录内代码。编译、像素测试、截图观感、文件哈希等均为“会话报告”；原始测试日志、图片像素和项目源码未在本次读取。没有复核的事项明确保留为未验证。
>
> 通用方法见 [当日 Shader 参考](../../../references/CodexDaily/Development_Reference_2026-11-09.md)；规范引用 [Shader 入口](../../../README_Tech_URPShaderDevelopmentRules.md) 的 `CMP-01`、`ORG-02`、`ORG-03`、`VAL-01`～`VAL-03`、`PRJ-02`。返回 [总览](../../../../Codex_Development_Digest_2026-11-09.md)。

## 1. 工程身份和工作区边界

当日玻璃与 UI 会话报告在 `D:\work2025U3D\Valkyria\ProjectACGMain\ProjectACG\Client` 工作；角色丝袜/Monst 检查与修正在 `D:\work2025U3D\Valkyria\ProjectACGMain2\ProjectACG\Client`。两者不能互作当前文件或日志证据。后文资源路径相对各自 `Client`。

会话报告验证基线为 Unity `2022.3.62f3`；TranslucentGlass 明确报告 URP `14.0.12`，MatcapGlass 报告 URP `14`。没有逐项明示的包补丁版本不补全。统一 Inspector 为 `Scarecrow.SimpleShaderGUI`；这是项目约束，不是跨项目通用要求。

Chara_V2 的目录快照导入记于 [工具 Profile](../../../../ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-09.md)，不将 Main 的路径同步结果外推到 Main2。

## 2. 丝袜各向异性遮罩与角色 Alpha Test

来源：[01a1bf2c-715a-7c50-8e33-adad65bde442](../../../../../VA_Worlk_memory/sessions/01a1bf2c-715a-7c50-8e33-adad65bde442.md)，消息 2～20，北京时间 15:19～16:51。

### 项目事实/现象

- `Chara_SilkStockings_V2` 的 `_StockingsControlMap`：R 控制厚薄，G 控制 A/B 两层各向异性高光强度，A 控制边缘光遮罩。
- 「启用各向异性强度遮罩」默认关闭；启用后 G 白保留、黑屏蔽、灰减弱。遮罩强度 1 按图控制，0 忽略图，对比度调节灰度响应。
- `Chara_Cloth_V2` 和 `Monst_V2` 虽有 Alpha Test 控件，检查时均不能按预期裁剪本体。

### 根因

Cloth 声明 `_ALPHATEST_ON` 和 `_Clip`，主渲染却未执行基于贴图 Alpha 的 `clip()`；其基础 A 还参与「BaseTex.a 区域启用漫反射混合」。Monst 主渲染只输出 `基础贴图.a × _Alpha`；深度/阴影通用逻辑使用 `_BaseMap / _Cutoff`，与项目的 `_BaseTex / _Clip` 不匹配；描边和运动矢量也未接通裁剪。

### 项目约束或修正

当日用户只要求补齐 Monst。修正集中于 `Assets/Shader/Character/Chara_V2/Monst_V2/Monst_V2.shader`，8 个 Pass 统一采用 `基础贴图 A × Alpha < Clip` 的裁剪契约。Alpha Test 关闭分支曾出现 DepthNormals 引用被编译器移除的采样器，随后报告修正；具体代码修正缺失。

材质操作：基础图保留 Alpha，勾选 Alpha Test，`Alpha=1`，Clip 可从 `0.5` 起调。Cloth 没有当日修复完成证据，不能因 Monst 修复而标为已支持。

### 验证证据

会话报告 Unity 2022.3.62f3／D3D11 下 8 个 Pass 共 66 项像素测试通过，覆盖开关、三个 Clip 阈值、Alpha 倍率、UV 偏移/缩放、阴影、深度和描边。测试的逐项输出没有随 Markdown 保存。丝袜为只读源码检查报告，没有材质实测。

### 未验证项/回退

实际怪物场景、手机图形后端和性能未复验；工作区资源门禁因既有动画 `.meta` GUID 改动失败，不能写成工程全量检查通过。保留修改前单文件和材质可作回退方案；会话没有记录执行过回退。

## 3. MatcapGlass：文章效果、Cubemap 与平面适配

来源：[01a1bf8a-7eff-7be3-89ad-651747d035e0](../../../../../VA_Worlk_memory/sessions/01a1bf8a-7eff-7be3-89ad-651747d035e0.md)，消息 2～22，北京时间 17:01～18:01。

### 项目事实/现象

用户要求按文章《如何在游戏中表现玻璃的质感？》实现效果，放入项目路径并沿用 ShaderGUI。转录仅包含文章的会话提炼，文章原文和附图未在本次读取。主要思路为两层 MatCap、厚度遮罩、Fresnel、法线、内外壁、液体遮罩流动与磨砂近似，不额外抓屏。

### 根因

常规 MatCap 根据法线取样，平面法线一致时易显示为单色，不能自然铺满纹理；新增环境贴图方案还需要区分反射、模拟折射的采样方向和旋转归属。

### 项目约束或修正

- 产物：`Assets/Shader/Scene/New/MatcapGlass/MatcapGlass.shader`，同目录 `README.md`。
- 最初生成透明、内壁、磨砂、琥珀液体、冰裂五类材质；透明示例为 `Materials/M_Glass_Clear.mat`。
- 反射和模拟折射各自可选 MatCap/Cubemap，可混用；Cubemap 分别提供 XYZ 旋转，MatCap 分别提供平面旋转。原材质默认继续使用 MatCap。
- 新增 `PlanarUV` 模式及 `Materials/M_Glass_Plane.mat`，支持缩放、偏移、法线扰动。
- 基础色图 RGB 染色、A 控制整体透明度，独立 Tiling/Offset；「基础颜色覆盖」可增强透明中心图案。
- 会话报告处理正交相机、负缩放法线和彩色高光透明度。具体公式与变体逐项列表缺失。
- 用户明确不允许操作鼠标；后续改由文件、编译桥和独立后台 Unity 验证。

### 验证证据

会话报告主工程最初导入诊断为 0；后因主工程脚本重编译阻塞预览，移除临时验证脚本并转独立工程。后台完成 32 个变体编译/预热、ShaderGUI 绑定和透视/正交预览；四种 MatCap/Cubemap 组合、独立旋转和折射率变化通过检查。新增模式后原五材质预览报告逐像素一致，平面 RGB/A/ST 也报告生效。

### 未验证项/回退

主场景透明排序、Android 真机与实际开销未验证；本次不能依据图片路径确认画面。回退可恢复旧材质默认 MatCap 路径；新增采样模式并非完整物理折射。

## 4. TranslucentGlass：保留旧公式的 URP 移植

来源：[01a1bf9b-020f-7dc0-97d8-de8f3a50cc92](../../../../../VA_Worlk_memory/sessions/01a1bf9b-020f-7dc0-97d8-de8f3a50cc92.md)，消息 2～13，北京时间 17:19～17:43。

### 项目事实/现象

用户提供 `Qilin/Translucent` Surface Shader 代码，含 `Shadersvg`、`uv_MainTexsvg`、`Emissionsvg` 等拼写问题，要求 URP HLSL 及项目 GUI。用户明确不操作鼠标。转录直接保留了原代码，但没有最终完整 HLSL。

### 根因

原逻辑的 `_Refract` 只参与透明度混合，没有屏幕背景折射；直接按变量名称解释会夸大功能。原 `_Solidity=1` 产生不透明结果；原纹理未提供，无法按原素材做视觉等价验收。

### 项目约束或修正

新增独立 `Valkyria/Scene/TranslucentGlass`，避免覆盖已有未提交 MatcapGlass。资源位于 `Assets/Shader/Scene/New/TranslucentGlass/`，含 `M_TranslucentGlass.mat`、`README.md` 及两张示例 Cubemap。

保留 Cubemap 颜色混合与 Fresnel 透明度公式，修正拼写/空间/通道问题，接入 URP 光照、阴影接收、雾、实例化及 SRP Batcher 声明。固定透明混合、不写深度、不依赖 Opaque Texture，适应会话报告中该项目关闭 Opaque Texture 的设置；示例使用生成的棚拍 Cubemap。

### 验证证据

会话报告独立 Unity 2022.3.62f3／URP 14.0.12 完成 130 个变体零错误、SRP Batcher、透视/正交渲染与资源配对检查。完全透明与背景一致，白色遮罩与实心度 1 的不透明结果一致；测试副本与工程文件报告逐一核对。后台无法调用 Inspector 绘制接口，改用其他检查方式，不能当作 GUI 交互通过。

### 未验证项/回退

目标模型透明排序、移动端与原素材效果未验证。回退使用新增资产前的状态或移除独立材质引用；没有实施回退报告。`_Refract` 仍不是屏幕折射，不能改写为完整折射功能。

## 5. StylizedGlass：视角条带和材质球制作

来源：[01a1bf9f-cc5a-7913-850f-5be6144d681f](../../../../../VA_Worlk_memory/sessions/01a1bf9f-cc5a-7913-850f-5be6144d681f.md)，消息 2～13，北京时间 17:24～17:53。

### 项目事实/现象

用户参考图要求随视角变化的条形高光。会话提炼为透明底色、Fresnel 亮边和条带假反射，并要求进一步调试材质球。

### 根因

初次渲染暴露球体 UV 扭曲墨镜条带、杯壁条带过密；直条投影与曲面反射条不能使用同一组默认参数。

### 项目约束或修正

新增 `Assets/Shader/Scene/New/StylizedGlass/`，沿用项目 GUI，支持宽窄双条、硬边抗锯齿、视角移动和法线反射投影。提供平板、杯子、墨镜预设；四套材质球为冰蓝透明、浅色水晶、琥珀玻璃、烟灰镜面。

冰蓝/琥珀保留曲面弯曲反射条；水晶/烟灰调整为平行斜条。材质目录为 `Assets/AssetRaw/Materials/StylizedGlass`，场景为 `Assets/TA_Test/LYJ/Scene/StylizedGlass/StylizedGlass_MaterialBalls.unity`，说明为 Shader 目录下 `README_Art_StylizedGlass.md`。

### 验证证据

会话报告 Unity 编译无错、项目 GUI 绑定正确、转视角渲染和资源检查通过，可见度为零无残留亮色。最终用独立预览场景隔离其他物体；用户回复已刷新后生成最终截图，并报告清理临时验证脚本。转录只有图片路径，本次没有查看图像。

### 未验证项/回退

移动端及实际角色透明交叠未验证；准确材质数值、具体投影公式缺失。回退需保存材质修改前参数和预览场景，不能用截图路径代替这些资产。

## 6. Ciconia Glass：渲染语义、GUI、Alpha 与材质迁移

来源：[01a1bff3-d286-7722-a456-7e33304f5a7b](../../../../../VA_Worlk_memory/sessions/01a1bff3-d286-7722-a456-7e33304f5a7b.md)，本日只采用消息 2～96，北京时间 18:56～23:54。消息 97 起属于次日，不用于本节。

### 项目事实/现象

初始范围为 `Assets/Ciconia Studio/Shaders/Glass Shaders/URP` 的 `URP - CS_SimpleGlass.shader`、`URP - CS_Glass.shader`、`URP - CS_Glass(IOR).shader`。用户先询问实现及 Albedo 发白，再调整 GUI、贴花、自发光、Alpha 和 Mix Map，随后将范围收窄到普通 Glass。

**基线身份有明确用户纠正**：消息 84 指定 `URP - CS_Glass.shader` 为旧版，`URP - CS_Glass_Change.shader` 为修改版；Codex 承认先前把主文件误当修改版。文档遵循此纠正。文件被复制或恢复的具体时间、方式缺失；前段“已修改主文件”的报告不用于证明当天结束时主文件状态。

### 根因

| 现象 | 会话中报告的原因与证据边界 |
| --- | --- |
| Albedo 发白 | Albedo 只影响表面光照，背景和额外 Cubemap 反射作为 Emission 另加。现场参数报告为 Refraction=0、反射=1、Fresnel=1、Opacity=0.223、Metallic=0.85。未做完整逐项关闭对照，最初发白的主因未确认。 |
| “Refraction=0”仍扭曲 | 普通版偏移由 `(_Refraction-1)` 控制，1 才是无偏移。IOR 参数也是屏幕偏移近似，不追踪厚度及前后表面；IOR 偏移报告使用几何法线。 |
| Albedo A=0 仍有玻璃 | 初始普通 Glass 的 A 只参与背景混合，最终 Alpha 由 Fade 决定；IOR 将背景先混入 RGB，最终 Alpha=1。 |
| 反射模糊与背景不一致 | `_BlurReflection` 控制 Cubemap Mip，不做背景图滤波。 |
| GUI 说明只显示属性 ID | 公共 GUI 只以属性名作 Tooltip，纹理 Drawer 还丢弃提示。详细工具修正见 [工具 Profile](../../../../ToolDevelopment/Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-09.md)。 |
| Mix Map 后外观不同 | 旧材质槽不会因新 `_MixMap` 属性自动迁移；旧 AO 等效为 1，新 B 通道读暗值；Importer Swizzle 还改变了实际采样通道。最后只完成分析，保持旧效果的修复未报告完成。 |

三个版本初始折射均由 `SHADERGRAPH_SAMPLE_SCENE_COLOR` 最终读取 `_CameraOpaqueTexture`，需要 URP/相机提供 Opaque Texture，默认不含后绘制的透明物体。初始 Forward 均写死 `ZWrite On`；面板存在 `_ZWrite` 不代表能控制 Forward。

### 项目约束或修正

**GUI 与状态。** 最终采用 `Scarecrow.SimpleShaderGUI`、中文悬停提示；移除专用 GUI 和“参数用途说明”折叠栏。按 `PackedMaskPBR` 参考整理「基础设置」与「其他设置」，基础组包含深度写入/测试、剔除、混合因子，其他组为高光/环境反射/接收阴影；渲染状态报告接入实际 Pass。UV 标签保留 `Global Tiling X/Y`、`Global Offset X/Y` 英文。贴花及其专用发光、法线、遮罩逻辑被删除，属于功能变更。

**独立 Emission。** 三版本先加入默认关闭的贴图 RGB × HDR 颜色 × 强度；IOR 在背景混合之后加发光，避免被背景透出压掉。当天末修改版删除 `Emission Intensity`，改为贴图 × HDR 颜色。`Self Illumination / Intensity` 对应的 `_Intensity` 中文为「背景亮度倍率」，仍控制背景，不是新增 Emission。

**最终 Alpha。** Glass/IOR 后来将「基础贴图 A 控制透明度」接入最终 Alpha，支持反转；会话报告 A=0 跳过深度/阴影写入。普通版关闭角度淡出时为 `最终 Alpha = 基础贴图 A 遮罩 × (1 - Fade)`；关闭遮罩时按 1，开启反转时按 `1-A`。表面颜色 A 未参与此公式。背景透出、背景亮度、光滑度遮罩只影响 RGB 背景合成。推荐 Blend 为 `SrcAlpha / OneMinusSrcAlpha`。中间 Alpha 的深度策略仍要另验。

**Mix Map 的最终需求。** 用户否决沿用 BaseLit 的亮度、对比度、AO 强度控制；最后只保留金属度与光滑度倍率：

| 通道/参数 | 当日修改版的报告语义 |
| --- | --- |
| R | 光滑度，即 1−粗糙度；乘光滑度滑条。 |
| G | 金属度；乘金属度滑条。 |
| B | AO，直接读取。 |
| A | 不使用。 |
| 无图 | 金属度和光滑度由滑条控制，AO=1。 |
| `_UseSmoothness` | 迁移为读原始 Mix Map R 控制背景透出遮罩，不是最终 Alpha。 |
| 导入 | 数值遮罩关闭 sRGB。滑条=1 保留通道原值。 |

早期“完全照搬 BaseLit 亮度/对比度”的提案已被用户撤回，不能作为最终实现记录。原 `_MetallicGlossMap` 使用 R 金属度、A 光滑度，A 还可能参与背景遮罩；原 AO 为独立图。

**RainGlass 迁移。** `Window_Lp_RainGlass_MetallicSmoothness` 的 Swizzle 报告已是 `A,R,B,A`、sRGB 关闭，因此新 R=源 A，新 G=源 R，新 B=源 B。四个 RainGlass 材质报告已接入 Mix Map；`RainGlass` 金属度改为 1，其他三个保留 0.85，光滑度均为 1。最后分析报告源 B 约为 `6～54/255`，旧 RainGlass 无 AO 图、等效 AO=1；两者明显不同。建议新 B=1 或显式无 AO 回退，但当日没有完成该修复的证据。早段称 B 为 AO 的说法只表示接入位置，不证明它原本包含有效 AO。

**当天最后修改版。** `URP - CS_Glass_Change.shader` 增加默认关闭的 MatCap 高光，支持贴图、HDR 颜色、旋转、法线贴图影响，随透明度/Fade 淡出，不写入烘焙光照；建议使用黑底亮斑图。额外反射强度改为 0～1 滑条，默认 1。

### 验证证据

以下均为会话报告，属于不同阶段，不能相加为“最终版本完整通过”的总数：

| 阶段 | 报告结果 | 仍有的边界 |
| --- | --- | --- |
| 中文 GUI/Tooltip | C# 编译、静态覆盖通过；53+46+27=126 条说明转悬停；当时渲染代码未变 | 悬停布局与真实 Inspector 未复验。 |
| 面板状态与贴花移除 | Glass、IOR Unity 重导入无诊断；三者静态检查通过 | SimpleGlass 当时编译桥未响应；没有把等待当成功。 |
| 独立 Emission | 39 个编译组合无诊断；黑白遮罩、HDR×强度、归零、UV、关闭状态像素对照通过 | 场景 Bloom 未复验；不是整套贴花删除后的视觉回归。 |
| Alpha 修正 | 26 个编译组合、144 项像素测试通过，覆盖 A 梯度、反转、Fade、Emission | 主场景未复验。 |
| Mix Map | 独立工程编译、通道、无图参数、透明度和发光回归通过 | 没有给出项目场景 A/B 通过；随后用户仍报告差异。 |
| 最后 MatCap 修改版 | 独立 Unity 编译及 22 项渲染检查通过 | 原始结果文件未随本次来源提供；实际场景未复验。 |

### 未验证项/回退

优先补证 B 通道 AO 回退后的旧/新场景 A/B、准确材质/文件基线、GUI 悬停、多层透明、深度遮挡、Bloom 和移动端性能。后续恢复旧效果应同步恢复 Shader、材质槽、倍率、ST 与纹理 Importer；仅恢复 Shader 不足。具体可用备份和回退执行结果未确认。本日之后的会话结果不纳入。

## 7. GammaUIIconBlur：多点模糊到 Mip 组合

来源：[01a1c025-cbe5-78c0-abf8-3479d35c4241](../../../../../VA_Worlk_memory/sessions/01a1c025-cbe5-78c0-abf8-3479d35c4241.md)，消息 2～16，北京时间 19:51～20:17。

### 项目事实/现象

用户要求 UI 图标清晰到模糊并注意性能。项目使用 Gamma UI 链路；现有图标报告通常关闭 Mip Map。

### 根因

固定多点大半径易重影；图集整体 Clamp 不能限制单 Sprite，低层 Mip 已混入邻图颜色时 Clamp 也无法修复。第一次边界处理的梯度采样在当前 D3D11 未得到预期 Mip，根因未进一步说明，不能写成已确认的驱动缺陷。

### 项目约束或修正

- Shader：`Assets/Shader/UI/GammaUI/GammaUIIconBlur.shader`，名称 `Valkyria/UI/GammaIconBlur`。
- 示例材质：`Assets/AssetRaw/Materials/GammaUIIconBlur.mat`；说明：`Assets/Shader/UI/GammaUI/README_IconBlur.md`。
- 基础路径：Blur Strength 0～1，默认 5 点、高质量 9 点，清晰时单次采样；Alpha 加权减少黑边，支持 Mask、RectMask2D、SceneView。
- 不抓屏、不新增模糊 RT；图集手工设置 Sprite UV 范围。
- Mip 路径：开启 Generate Mip Maps、Trilinear、Use Mip Blur，最大 Mip 偏移建议先用 3；Local Radius=0 为纯 Mip，一次采样指令；0.5～1 为 Mip+5/9 点。
- 先尝试由原始 UV 梯度控制边界 Mip，后报告切换显式层级采样以修正 D3D11 结果。最终公式和各 Pass 实现未在正文给出。

### 验证证据

基础版报告 30 项独立 GPU 检查通过、16 个 D3D11 变体无错误/警告；中途初始化警告已报告清除。Mip 版最终报告 61 项 D3D11 GPU 检查通过，包含低层图集串色边界验证。两组是不同版本，不能累加或外推手机性能。

### 未验证项/回退

实际 UI 页面、手机 GPU 时间/带宽/显存、实际 Draw 和覆盖开销未验证。完整 Mip 链的约 33% 内存增量是估算条件，不是此工程实测。仓库文档链接和本地化门禁有既有失败；不等于当前 Shader 编译失败，也不等于全量工程通过。可关闭 Mip 回退基础多点路径，并恢复 Importer 原设置；执行回退未报告。

## 8. 规则候选与补证清单

此处不定义新的 CORE ID，也不修改正式规则。

| 候选经验 | 既有规则表达 | 现有证据与适用边界 | 下一步补证 |
| --- | --- | --- | --- |
| 检查所有 Pass 的 Alpha/纹理契约 | `ORG-03`、`VAL-01` | Monst 的参数失配和 66 项测试为会话报告 | 业务怪物、目标设备、真实 Pass 覆盖。 |
| 将 Importer/材质迁移纳入视觉基线 | `CMP-01`、`VAL-02` | RainGlass 映射与暗 AO 原因分析，有用户差异反馈 | B=1 后旧/新 A/B；确认基线文件身份。 |
| 拓展模式默认关闭并对照旧画面 | `CMP-01` | MatcapGlass 报告默认路径逐像素不变 | 真实场景、各平台和材质引用复验。 |
| 把采样次数与实测性能分开 | `CTL-01`、`VAL-02`、`VAL-03` | UI Mip D3D11 功能验证，无手机计时 | 目标设备 Draw、带宽、纹理内存与 GPU 时间。 |
| GUI 可见性不代替渲染状态生效 | `ORG-02`、`ARC-03` | 初始 ZWrite 被写死、Tooltip 丢失 | Inspector 真实交互与旧材质兼容。 |

