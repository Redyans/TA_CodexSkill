# 《依盖之书》玲珑展示场景复刻 Profile

## 概览与证据边界

本页只记录《依盖之书》`UIPre_LingLon` 与展示环境在 2026-10-31 抓帧/Unity 工程中的事实。通用边界见 [Unity 抓帧复刻模块](../../README_Tech_UnityCaptureReconstructionRules.md)，操作顺序见[工作流](../../references/unity-asset-and-render-reconstruction-workflow.md)。项目源码、完整取证和最新 JSON 均在 `F:\UnityProject\HYYYYY\Assets\YogCharacterReconstruction`（下文称“复刻根”）。本页的数值和路径不能直接套用于其他角色。

**当前范围**：人物、动作、光照、背景、地面、水面、特效、反射及后处理；不重建原游戏 UI。最早的人物技术报告和 README 写于背景/地面扩围之前，文中的“背景未恢复”属于当时阶段。当前环境实现与完整场景比较以复刻根的 `Shaders/EnvironmentReconstruction.md`、`Environments/CharacterShowcase` 和 `Validation/FullSceneComparison1.json` 为准。整图仍有误差，未宣称完全一致。

| 输入/宿主 | 当前项目事实 |
| --- | --- |
| 原游戏 | `D:\RJ\Steam\steamapps\common\book of yog 依盖之书`，Unity 2021.3.45f2，标准 UnityFS |
| 目标工程 | `F:\UnityProject\HYYYYY`，Unity 2022.3.62f3，URP 14.0.12，Windows Direct3D 11，Linear |
| 抓帧 | `F:\UnityProject\HYYYYY\Cap\依盖之书_2026.10.31_16.51_frame15710.rdc`；原始 SHA-256 `4F562848202FDF7889C7C3801BDFF36CABD8CA26565E4F091C28D53D89BEBBD0` |
| 美术参考 | `Cap/20261031165807_1.jpg`；与 frame15710 动作时刻不同，不能直接做同帧像素比较 |
| 同帧参考 | `Validation/Capture_Reference_NoUI_3840.png`，源自 E2365 UI 前输出，按原方向和 IEC linear→sRGB 派生 |
| 场景 | `Scenes/LingLongCharacter.unity`，展示环境在 `Environments/CharacterShowcase` |
| 工具 | UnityPy 1.25.3、RenderDoc 1.46、3Dmigoto cmd_Decompiler 1.3.16、Il2CppDumper 6.7.46、pefile/Capstone、Unicorn 2.1.4、Unity/D3D11 GPU readback |

## 项目规则与工程布局

### BOY-01｜集中依盖之书复刻资料并保留 GUID

本项目新增的共享 Shader、RendererFeature、取证与验证位于复刻根；角色独有资产放 `Characters/<Character>`，场景独有环境放 `Environments/<Scene>`。新角色复用共享实现时新增自己的依赖、材质和动作清单，不以玲珑的事件号/相位代替取证。

玲珑当前 31 条动作中，29 条在 `Characters/LingLong/Animations`；展示 `AnUI_LingLon_StandM01` 与武器 idle `An_LingLon_Wep01_Stand01` 仍在 `SourceAssets/Animations`。这是保留场景/控制器 GUID 的现状，不能写成全部已迁入角色目录。以后迁移这两条时，连同 `.meta` 原 GUID 移动并复核场景与 Controller 引用。

- **必须（MUST）**：搬动 `.anim`、Prefab、场景或管线资产时连同 `.meta` 移动，检查引用 GUID、场景缺失脚本和目标工程实际加载路径。
- **应当（SHOULD）**：保留 `Tools~` 中的只读 bundle 工作副本、提取脚本和旧验证报告；新的比较输出使用新前缀。
- **验证**：复刻根 `Validation/scene-integrity.json` 记录的当时场景检查为缺失脚本 0、依赖 168、Animator 2；这是 2026-10-31 11:38 的阶段检查，环境后续修改需要重新执行场景完整性回归。
- **例外与回退**：跨工程迁移时先备份原 Graphics/Quality/manifest/lock，重建本地包入口后验证 GUID；见 `Tools~/OriginalProjectSettings`。

### BOY-02｜以本帧实际 GPU 输入覆盖静态配置猜测

frame15710 中主相机实际 VP 使用 `cb4_v17..20`；同缓冲 `cb4_v5..8` 保留反射相机 oblique 数据。E908 实际画了 Face 阴影，即使原 Prefab 的静态 CastShadows 为 Off。反射与主相机的 shadowmap 共用资源号但帧内被重写，须按事件取内容。

- **必须（MUST）**：本场景的材质、阴影、AO、反射和后处理读取实际 draw/dispatch 的矩阵、常量、sampler、RT 内容与顺序。
- **验证**：对应原始信息在 `Evidence/RenderDoc`、`Shaders/ShadowReconstruction.md`、`Shaders/EnvironmentReconstruction.md`；同输入/实时回归见下表。
- **例外与回退**：抓帧之外的时刻只能作为推断或新取证对象，不直接写为本帧结论。

## 资源与动画经验

原 bundle 索引覆盖 17,191 个文件；玲珑 6 个目标部件及递归依赖为 56 包，约 127.23 MB。源记录和逐包 SHA-256 在 `Evidence/Assets/import_manifest.json`，只读副本在 `Tools~/Extraction/Bundles`。原生 mesh/Avatar 通过 Unity AssetBundle API 读取；104 张去重 PNG 只作预览，155 条纹理源记录保留颜色空间、mip、wrap/filter。12 个角色 draw 的原 UV0/已有 UV1 与捕获逐点相符；描边所用 TEXCOORD3 是 `ModifyMeshRuntime` 运行期通道，已从抓帧补入目标 Mesh。

原 `AnimationClip` 的普通 editor curves 为空，实际数据在 `m_MuscleClip.m_Clip.data.m_StreamedClip`。直接复制保存产生 1 秒、零曲线资产；最终用原三次多项式、key 时间/值/切线和 Avatar/TOS 路径生成可编辑 `.anim`，没有固定帧率重采样。UI 的 `An_Stand01`、`An_Stand02`、`An_StandUp` 三个展示槽都覆盖为同一条 `AnUI_LingLon_StandM01`（约 6 秒），不是三条动作。另有身体战斗及武器可替换动作，总计 31 条：1 展示、15 身体战斗、15 武器；角色目录和分类见 `Characters/LingLong/Animations/README.md`。

`Evidence/Assets/animation_import_validation.json` 记录 24,479 条可绑定曲线、435,214 keys、缺失路径 0。原身体战斗动作中无法映射到现有 Avatar 的作者 hash 未猜造骨名，原 key/切线保存在 `Tools~/Extraction/DecodedAnimations`。`Validation/animation-switch-validation.json` 的替换播放检查为 `success=true`。身体和武器是独立 Animator；frame15710 的拟合预览时间分别为 0.44166666 秒与 1.11666667 秒，固定 GPU 姿态只用于对照。

DynamicBone/CameraDriven 的实现来自原 `GameAssembly.dll` 与 metadata 的实际使用分支。相机扰动只由相机 Y 位移计算，原 x64 方法 512 组仿真与 Unity 32 组/128 链 golden 校验通过；详见 `Evidence/Native/README.md`。单帧不含捕获前的粒子速度、相机路径和执行先后历史，不能称所有动态帧已精确重放。

## 渲染、光照和后处理经验

| 子系统 | 本帧取证与实现 | 限制/验证入口 |
| --- | --- | --- |
| 人物 Shader | 102 个角色相关 draw、76 个唯一 VS/PS，当前人物 12 个子网格 draw；DXBC、常量、资源与状态在 `Evidence/RenderDoc`，生成器在 `Tools~/Shader`。 | 原 AG 法线由 shader 解码；`Validation/shader-compilation.json` 的 83 组合零错误是 10:14:58Z 初检，不能覆盖后续修改。 |
| 光照 | 主相机约 FOV 25°、俯角 2°；主光方向 `(0.588217139,0.322607517,-0.741569340)`，点光位置 `(1004.280029,1004.200012,998.919983)`；原 SH 与 `T_ReflectionProbe03` cube/mips 保留。 | 数值来自指定 draw 的常量，别的角色/时刻要重取。 |
| 自阴影 | 原 4096² D16 caster、严格 Greater 比较、16 次 linear comparison 组成 7×7 tent、`0.9*PCF+0.1`；`YogShadowFeature` 每帧重绘人物。 | `Shaders/ShadowReconstruction.md` 与 `Validation/shadow-live-gpu-validation.json`；旧阶段未画地面 caster 的描述不能扩展到后续环境。 |
| 环境与地面 | `Environments/CharacterShowcase` 包含 Ground、Sky、Water、草、花焰/粒子等；8 类环境 Shader、44 组材质记录（29 主视图、15 反射特效）。 | 实际 Pass 顺序见 `Shaders/EnvironmentReconstruction.md`；抓帧粒子网格不证明完整发射生命周期。 |
| 水面反射 | 1536² 实时平面反射，镜像 View、斜截投影、反向剔除、独立 shadow 配置；反射 HDR 到 RGBA8 sRGB 的复制在原 E731。 | 反射与主相机使用不同 shadow 快照；原 E2013 反射材质 Point sampler 不可随意换成 Bilinear。 |
| AO | 原 PPv2 MultiScaleVO，深度/16 层 atlas/遮蔽/上采样与乘色合成按 E1603–1791 恢复。 | `Validation/ao-gpu-validation.json` 的 18 阶段全部逐字节一致，限定为所给原输入。 |
| TAA | 原 E2179，读取当前/历史 HDR、D32S8 depth、RGHalf motion；外部 jitter 接入本地 URP，非抖动矩阵供 motion 使用。 | `Validation/temporal-gpu-validation.json`：同输入 8,294,400 像素逐 bit 一致。实时 history 另验证。 |
| Bloom/调色 | 6 级 Bloom down/up、33³ RGBAHalf LogC LUT、暗角与 dithering；不叠加 URP 默认 tonemapper。 | `Validation/PostGPU/Comparison.json` 是原输入的独立阶段比较，不代表实时整图完全一致。 |

主工程的本地 URP 源集中在复刻根 `Packages~/com.unity.render-pipelines.universal`，`Packages/com.unity.render-pipelines.universal` 为指向它的 NTFS Junction；manifest 保留 `14.0.12`，UPM 实际 `Embedded`。Unity 曾拒绝 `file:../Assets/...` 指向本工程 Assets 内包，致 `ScriptableRendererFeature` 等 CS0246；改为标准 Packages 入口后脚本加载成功。`Tools~/EnsureLocalURP.ps1` 重建入口，`Tools~/InstallProjectPipeline.ps1` 安装本地管线并保留原 Graphics/Quality 备份；补丁与根因在 `Evidence/LocalURP.md`。`Validation/main-project-health.json` 记录主工程 2026-10-31T15:27:18Z 脚本加载成功、URP 14.0.12 `Embedded`。

## 实测结果与未验证项

| 检查 | 结果与范围 | 项目内证据 |
| --- | --- | --- |
| Native 纹理 | 53/53 张全部 mip/face 字节一致，0 失败。早期 51 张与最终 53 张为统计集合变化。 | `Validation/CapturedTextureByteValidation.json` |
| 动画导入/切换 | 31 clip、24,479 曲线、435,214 keys、缺失路径 0；切换成功。 | `Evidence/Assets/animation_import_validation.json`、`Validation/animation-switch-validation.json` |
| TAA 同输入 | 3840×2160 全部 8,294,400 像素 RGBAHalf 输出 SHA-256 与原 E2179 相同。 | `Validation/temporal-gpu-validation.json` |
| AO 同输入 | 18 个阶段，包含 16 层数组，逐字节一致。 | `Validation/ao-gpu-validation.json` |
| 自阴影实时局部 | 不透明内区 308,230 像素，screen-shadow MAE 0.0021722；不能替换成独立滤波的近全同数字。 | `Validation/shadow-live-gpu-validation.json` |
| 完整场景同帧阶段 | 全帧 8,294,400 像素 sRGB MAE 0.712834/255；环境区约 0.603229/255，人物可靠不透明内区约 1.181061/255。 | `Validation/FullSceneComparison1.json`、`FullSceneComparison1_Regions.json` |

这些报告对应各自保存时点。完整场景指标没有做配准、曝光拟合或背景替换；它不是逐像素相同。后续修改若影响 Shader、场景、动态或管线，重新生成带新前缀的编译、实时缓冲、HDR、最终图和 PlayMode 报告，不把较早通过结果自动延伸到新版本。未由单帧证明的内容包括完整粒子发射生命周期、捕获前动态物理/相机历史、未执行 Shader 分支及其他镜头/动作时刻的逐 bit 一致性。

## Evidence → Finding → Path

以下 `source_ref` 均相对复刻根；原文件哈希及更细的复现步骤见 `Evidence/input-manifest.json` 和项目技术报告 `2026-10-31_逆向工程-依盖之书人物还原-report.md`。本页不复制大型 RDC 或客户端二进制。

| Evidence | source_ref / 观察 | Finding |
| --- | --- | --- |
| E-BOY-01 | `Evidence/Assets/import_manifest.json`：6 个目标包与 56 个递归依赖，逐源包哈希 | F-BOY-01：角色资产依赖闭包可追溯 |
| E-BOY-02 | `Evidence/Assets/animation_import_validation.json`、`Validation/animation-switch-validation.json`：31 clip 与实际骨架绑定/切换 | F-BOY-02：runtime stream 需显式解码，导入资产可播放 |
| E-BOY-03 | `Validation/CapturedTextureByteValidation.json`：53/53 native 纹理全部 mip/face 一致 | F-BOY-03：当前材质可使用原 GPU 字节输入 |
| E-BOY-04 | `Validation/temporal-gpu-validation.json`、`ao-gpu-validation.json`：原输入模块输出一致 | F-BOY-04：指定 TAA/AO Pass 在原输入下恢复 |
| E-BOY-05 | `Validation/shadow-live-gpu-validation.json`、`Validation/FullSceneComparison1.json`：实时局部/整图仍有误差 | F-BOY-05：模块一致不能推导整图一致 |
| E-BOY-06 | `Evidence/LocalURP.md`、`Validation/main-project-health.json`：包入口故障与主工程恢复 | F-BOY-06：当前本地 URP 加载成功，故障根因有证据 |

**P-BOY-01（数据路径）**：原 bundle/RDC → 依赖与 GPU 事件索引（E-BOY-01/03）→ Mesh/Avatar/动画持久化（E-BOY-02）→ Shader/阴影/AO/TAA/环境与本地 URP（E-BOY-04/06）→ 实时缓冲和同帧整图比较（E-BOY-05）。终点是“可复现、可播放且有阶段误差报告的 Unity 场景”，不是“所有帧完全一致”。

本次写作时主要证据文件的 SHA-256 如下；重跑导出会改变报告字节，需重新计算哈希。复现方法按下节入口和项目技术报告的依赖顺序执行，原 bundle/RDC 仍需本地原始输入。

| Evidence | 主证据 SHA-256 | 复现入口 |
| --- | --- | --- |
| E-BOY-01 `Evidence/Assets/import_manifest.json` | `5E6788C25F31A8C082E81D0F78AC9EF5C766A9E5F2CACA1039F1E87186D05A40` | `Tools~/Extraction/index_bundles.py` → `extract_linglong.py` |
| E-BOY-02 `Evidence/Assets/animation_import_validation.json` | `6AF58C27AB1B0607155A78967F0BCA865CC53E75506C766CD7962165634F4979` | `decode_animation_curves.py` → `YogDecodedAnimationImporter.ImportDecodedAnimations` |
| E-BOY-03 `Validation/CapturedTextureByteValidation.json` | `FD77CB82CBE4E8B49D2A1755F58D08AF1C7BBCFD171D66FF930AD5B5882101F4` | `YogCapturedTextureValidation.Validate` |
| E-BOY-04 `Validation/temporal-gpu-validation.json` | `972EA6107C63F9EA333A6D3E512EE1E83F47F4ED3FF4DFB1A9FA85248FB29129` | `YogTemporalShaderValidation.ValidateCapturedInputs` → `validate_temporal_gpu.py`；AO 另见同名报告 |
| E-BOY-05 `Validation/FullSceneComparison1.json` | `C610E97E217D0E1A2226351E379DC7E250AA70858F7475219D7D3567C9764EF6` | `compare_full_scene.py`，固定原参考与本次候选截图 |
| E-BOY-06 `Validation/main-project-health.json` | `1B1306E6AE54CF59D38B7CC618347944524AA9F5C7ED12C0A7ED9DED1ACFD226` | 主工程打开与包解析健康检查；`Evidence/LocalURP.md` 记录故障处理 |

## 批量角色抓帧复刻的当前状态（2026-11-01）

`Cap/依盖之书` 中除已完成的 `1` 外有 36 份 RDC。`2`–`18`、`23`–`35` 的 32 份可回放，已在复刻根 `Scenes/Batch` 生成场景；`19`–`22` 的四份 RDC 无法回放，不能用同目录 JPG 推定其 Shader 常量或动画姿态。原始输入、候选 UI prefab 和源哈希见 `Evidence/BatchCaptures/batch-plan.json`。32 组导入的 `Validation/Batch/CapturedMaterialSummary.json` 为成功 32、失败 0、抓帧材质 472、子网格绑定 489、未绑定子网格 0；32 组 `*-preview.json` 均为非空预览，仍标注效果对齐待完成。

早期批量材质生成器只覆盖普通 VS/PS，漏掉 41 个绑定 Hull/Domain 的角色 draw。`Evidence/BatchCaptures/*/*-tessellation.json` 显示它们共用 HS 哈希 `77c40f1bb288...`，三控制点三角 patch、`fractional_even`，边缘与内部因子均为 5；正面与其他通道使用两种 DS 哈希。代表事件 `30/frame129132/E1645` 输入 16,770 索引、后细分 905,580 顶点；固定拓扑已有实现，Domain 位移仍为近似。`33` 号角色另有 2 个特殊 VS draw，原顶点纹理采样和位移运算也已接入。`Validation/shader-compilation.json` 最新记录 Unity 2022.3.62f3 / D3D11 的 536 个组合、0 编译错误，不能据此声称画面一致。

主相机必须从角色正面 draw 的常量读取，不能拿同一 RDC 的首个阴影/反射 draw。`Tools~/Batch/audit_showcase_cameras.py` 在各组 `*-shared-materials.json` 的普通正面 draw 中选事件，再取 VS `cb1[4]` 的相机位置及投影矩阵推导 FOV；审计结果在 `Evidence/BatchCaptures/batch-camera-audit.json`。32 组 FOV 均约 25°，机位大多与玲珑模板一致，少数偏移约 0.1 米；`YogBatchSceneBuilder.ApplyCapturedCameras` 已写入各场景与 `*-scene.json`。本轮曾因 JSON 将 `Vector3` 写成数组，Unity `JsonUtility` 读为零向量，导致 32 个预览全黑；改为 `{x,y,z}` 后重写场景，32/32 组恢复非空，并加位置/FOV 合法性检查。场景背景、地面和后处理仍使用玲珑模板配置，逐组差异未核验。

32 份可回放 RDC 已从 Uber 后、UI 前的全分辨率 RT 导出线性 EXR，并按玲珑相同翻转和 IEC sRGB 流程生成显示 PNG；资料在 `Validation/Batch/References`。最新完整回归 `Validation/Batch/BatchSceneComparison21.json` 对 1024×576 预览做无配准、无曝光拟合的同帧显示 sRGB 比较：整图 MAE 范围 0.02310–0.06104，均值 0.03177，32 组预览均非空。目标 Unity 2022.3.62f3 / D3D11 的最新 `Validation/shader-compilation.json` 检查 542 个 Pass/变体、错误 0；`BatchSceneComparison2`–`20.json` 保留为阶段快照，其中 `12` 包含被独立复拍否定的 5 号 0.4 秒相位试验，不能用作当前结果。`Evidence/BatchCaptures/batch-environment-post-reuse.json` 证明 32 份抓帧均执行与玲珑相同字节码的 8 组主环境 VS/PS，以及 TAA、LUT、Uber 程序；常量、纹理、几何和最终像素仍需逐帧核验。主光方向、点光位置和强度在 32 份角色正面 draw 中相同，见 `batch-lighting-audit.json`。

本轮明确修复三组可见偏差：`5/frame50787` 九条尾部多余的 URP Lit 材质覆盖移除后整图 MAE 0.09897 → 0.06103，原 E1561 特效 draw 尚未复刻；`7/frame52335` 武器按 E1372 世界矩阵换算到 Prefab 本地空间后 0.07333 → 0.04760；`26/frame133463` 帽饰源 Mesh 实际顶点范围异常，改用 E1593 GPU 输入网格并接到 `Bone_Hat01` 后 0.12122 → 0.02448。修复入口为 `Editor/YogBatchAttachmentRepair.cs`、`YogBatchGpuMeshRepair.cs`、`YogBatchCapturedMaterials.cs`；逐角色报告、参考图和预览在复刻根 `Validation/Batch`。`31` 号额外 Lit 槽的移除试验使误差升高，已恢复原槽位，不能推广 `5` 号的特例。

`5/frame50787` 九个尾部 Flow draw 已按 E1541–E1581 的同一 VS/PS 编译并恢复第二材质槽，`Evidence/BatchCaptures/5/*-captured-materials.json` 报告 24 材质、9 Flow Pass、0 未绑定；Flow 影响约 3,116 像素，但整图 MAE 0.061026 → 0.061035，未改善主要差异。`26/frame133463` 已补 E1652/E1697 两个气球主体 GPU 网格和 E1895 透明层，数据在 `balloon-draw-inputs.json` 与 `*-gpu-mesh-event*-uv1.bin`；整图 MAE 0.024483 → 0.024018，Domain 位移仍是近似。

`7/frame52335` 缺失链条定位到 E2513/E2516，原 VS/PS 哈希分别为 `676b4b2e...`/`578ea2b2...`。原 GPU `COLOR` 输入来自独立的零步长 `R8G8B8A8_UNORM` 顶点流，字节为 `FFFFFFFF`；旧通用网格导出器把它按 float32 误读为 NaN。`chain-vertex-layout.json` 保留格式和资源，`generate_case7_chains.py` 写入带双 UV 的 IA 网格与 URP Shader，`YogBatchCapturedMaterials.BuildCase7Chains` 绑定抓帧常量、透明状态和源 PNG。目标编译通过，预览可见链条且整图 MAE 0.047596 → 0.046079。当前 Shader 直接使用该帧捕获的对象与 VP 常量，因此链条只对这一静态姿态定位，切换动作时不会动态跟随；源 PNG 尚未做原 GPU BC3/mip 字节回归，不能宣称该效果逐像素一致。

`16/frame61565` 的像素历史定位六个粒子 draw：E2244、E2261、E2399、E2409、E2414、E2472；它们共用 VS `af528120...`，像素程序分三种。`effects-draw-inputs.json`、`effect-vertex-layout.json` 和 `effect-mesh-summary.json` 分别保存常量/贴图/透明状态、实际 IA 格式和六份当帧网格。E2244/E2472 顶点步长 36，其他四条为 52，颜色均按 `R8G8B8A8_UNORM` 解码。`YogBatchCapturedMaterials.BuildCase16Effects` 将网格和三种 URP Shader 接到场景，整图 MAE 0.050050 → 0.04557；源 PNG 尚未做 GPU 字节回归。头后环形的主要像素贡献由 `halo-pixel-values.json` 指向武器 E1459，源 Mesh 与抓帧 IA 有相同 1,788 顶点/5,040 索引，索引不匹配数 0；刚体拟合 RMS 约 0.0000008 米，表明差异主要是坐标朝向。`BuildCase16CapturedWeaponPreview` 用 E1459 网格和对象矩阵建立静态对照对象，最终 MAE 0.044983。原可动画武器仍在 Prefab，当前场景禁用其渲染；静态对照不随动作更新。

`6/frame51525` 将棺体按 E1267 实际对象矩阵修正到角色挂点，并用共享链条 Shader 接入 E2156/E2159 的独立常量、GPU 网格和源纹理；整图 MAE 0.043465 → 0.034057。`27/frame131865` 按 E1351/E1370/E1449/E2240 的对象矩阵和 GPU 网格复原机械臂、刀和火翼等四个静态附件，MAE 0.043088 → 0.033450。两者的截图和逐组证据在 `Validation/Batch`、`Evidence/BatchCaptures/6`、`Evidence/BatchCaptures/27`；`27` 的当帧静态对象尚未证明随原动作骨骼更新。

`3/frame49277` 的像素历史定位剑光 E2267、剑身 E1351 和叠层 E1489；导出实际网格、常量和 GPU 纹理后，整图 MAE 0.04049 → 0.03413。`11/frame56826` 以 E1439/E1534 的 GPU 网格和对象矩阵修正双剑，MAE 0.04103 → 0.03731。两组当前显示当帧静态对照对象，原可动画网格保留但禁用渲染；动态挂点仍未验证。`13/frame57795` 的镰刀 E1834、`14/frame60290` 的锤子 E879 使用 VS `cb3_v0..3` 对象矩阵修正 Prefab 武器模块，分别使 MAE 0.03692 → 0.03607、0.04018 → 0.03652；入口为 `Editor/YogBatchAttachmentRepair.cs` 中的 `AlignIridaWeaponToCapture` 与 `AlignChloeWeaponToCapture`。这两组保留原武器渲染器及骨骼父级，但尚未验证动态播放时与原效果一致。

`32/frame9114` 的 Pixel History 将大剑轮廓定位到 E1983 `SM_ZXY_Wep01`、E2113 `SM_ZXY_Jian01`，不是已有蒙皮网格 `SM_ZXY_SW01`。原资源在 `pre_zxy_wep01.prefab` 与 `pre_zxy_jian01.prefab`，8 个 bundle 的依赖闭包和副本哈希记录在 `Evidence/BatchCaptures/32/frame9114-weapon-source.json`。两条 draw 的实际 IA 网格、常量、去重 GPU 贴图与原 VS/PS 字节码已接入 `Characters/ZXY01/Cases/32_frame9114`，新增前向 Shader 在 `Shaders/BatchVariants/Variant_ffab4e1fe653.shader`。E1983 剑刃已挂到角色右手 `Bone_Wep01`，不改变当帧世界矩阵；`Validation/Batch/32_frame9114_blade-playback.json` 的 0–1.5 秒展示动作采样记录手部位移 0.0120 米、剑刃位移 0.0168 米、局部位置漂移 0。E2113 后侧剑体仍为静态对照对象。当帧截图中左下剑刃轮廓出现，但新增部分明显偏暗，整图 MAE 0.03590 → 0.03624；这是几何与来源恢复，不是着色验收，跨帧原游戏运动仍未对照。

`5/frame50787` 的动作相位诊断显示连续采样不具备单次场景重开的再现性：粗扫 0.4 秒得到整图 MAE 0.058833，细扫同一时间得到 0.067486，重新打开场景并单次渲染得到 0.07082，差于原 0 秒的 0.061035。连续扫描混入先前采样的姿态/历史状态，不能据此确认原抓帧动作相位；已将场景恢复到 0 秒并重拍。`Validation/Batch/BatchSceneComparison19.json` 保留恢复后的 0 秒基准，试验图仅供排查，不代表有效候选。

`19/frame65745` 的 423,895,797 字节 RDC 存在 framecapture 段压缩长度字段与文件末尾不符。独立副本修补该字段后，RenderDoc `OpenFile` 成功，但 `OpenCapture` 仍因读取数据流末尾失败，不能提升为可回放证据；原件未修改。偏移、修补值和状态见 `Evidence/BatchCaptures/19/frame65745-repair-attempt.md`。`20`–`22` 的 RDC 仅 413,637、336,130、299,022 字节，均在 framecapture 段内截断；四组需要新抓帧。

当前未验证项：`5` 号尾部主要可见偏差、`26` 号气球 Domain 位移、`3`/`11`/`16`/`27` 号静态对象与特效及 `32` 号 E2113 后侧剑体在动态播放下的挂点、`13`/`14` 号可动画武器模块的动态对齐、`32` 号剑体当前偏暗的 Shader/光照差异、其他组原 GPU 纹理字节，以及附属件挂点和独立骨架动作相位、阴影与描边变体、逐角色 HDR 和环境/后处理常量。上述区域 MAE 是诊断，不等于逐像素一致。损坏 RDC 的四组只能标为候选，获得可回放抓帧后再提升验证级别。复刻根 `README.md` 给出场景和阶段入口；这些批量数字不可回填到玲珑 `1` 号场景的既有逐像素结论。

## 批量附件增量验证（2026-11-02）

本节仅记录《依盖之书》当前工程的具体状态，沿用 `UCR-EVD-02`、`UCR-AST-01`、`UCR-VAL-01` 的证据与验证边界。`Tools~/Batch/audit_unbound_showcase_draws.py` 从主颜色 RT、Mesh 名和源 bundle 贴图交集生成候选，不能直接把候选判定为缺失几何；已恢复绘制和 `Plane001` 环境绘制过滤后仍有 25 条叠加/特效候选。每个补入附件先核对 RDC 事件、实际 IA 通道、PS 贴图和源 prefab，再由 `export_accessory_draws.py` 导出网格/纹理及 SHA-256，`extract_accessory_bundles.py` 保存源 bundle 递归依赖和副本哈希。两脚本分别在 `Tools~/Batch` 与各组 `Evidence/BatchCaptures/<编号>` 留痕。

| 场景 | 补入的当帧主颜色 draw | 整图 MAE（补入前 → 最新） | 当前边界 |
| --- | --- | --- | --- |
| `9_frame55743` | E1111 `SM_Wep_BowSKbl01` | 0.03362 → 0.03356 | E1300 叠加层和原游戏动态姿态未验收。 |
| `10_frame56195` | E1083 `SM_Hat02`、E1799 `SM_Hat01` | 0.03041 → 0.02797 | 帽沿/花饰已可见；E1217/E1818 其他绘制及法杖动作姿态待核实。 |
| `32_frame9114` | E2129 `SM_ZXY_Hat01` | 0.03624 → 0.03605 | 发带可见；E1983 剑刃仍显著偏暗，E2305 叠加层未验收。 |
| `33_frame9805` | E1585 `SM_Hat01` | 0.02476 → 0.02458 | 花饰可见；E1739 叠加层及动作相位未验收。 |
| `34_frame10152` | E1491 `SM_Hat01` | 0.02374 → 0.02354 | 花饰可见；E1675 叠加层及动作相位未验收。 |

32 组完整回归为 `Validation/Batch/BatchSceneComparison26.json`：1024×576 显示 sRGB、无配准/曝光拟合，整图 MAE 范围 0.02310–0.06104、均值 0.03167；32/32 预览非空。Unity 2022.3.62f3 / D3D11 的 `Validation/shader-compilation.json` 为 548 个 Pass/变体、0 错误。上述数值只是同帧阶段指标，不能称作完整效果对齐。32 号剑刃分层重拍确认 E1983 贡献偏暗长刃；原生贴图槽 0/1/2/5/6/7 的预览在 `Validation/Batch/32_frame9114_blade_texture*.png`，对象矩阵行列式为正值，尚未定位着色差异。临时全亮阴影诊断图 `32_frame9114_sword_force_lit.png` 相对当前正式预览只改变 11,341 像素，整图 MAE 0.03605 → 0.03579，剑刃仍偏暗；实验用 Shader 开关已移除，正式场景未保留强制全亮设置。分层诊断的每张图必须独立重开场景；连续重拍曾使第二张图异常变黑，不能据此推断原 Shader 输出。

增量复现：隐藏 `qrenderdoc.exe --python` 运行 `export_case26_balloon_inputs.py`，设置 `YOG_EFFECT_CASE`、`YOG_EFFECT_EVENTS`、`YOG_EFFECT_LABEL=accessory` 与明确的 `YOG_EFFECT_CAPTURE`；用同一事件集合运行 `export_accessory_draws.py`。32 号 E2129 另运行 `generate_case32_hat_shader.py`。然后 Worker `refresh` → `YogShaderValidation.Validate` → `YogBatchCapturedMaterials.BuildCase9CapturedBow`、`BuildCase10CapturedHat`、`BuildCase32CapturedHat`、`BuildCase33CapturedHat`、`BuildCase34CapturedHat` → `YogBatchSceneValidator.Run` → `compare_batch_scenes.py --prefix BatchSceneComparison26`。源副本用 `extract_accessory_bundles.py` 复核；报告重跑时使用新前缀保留旧证据。`19`–`22` 仍需新的可回放 RDC，其他组的阴影、描边、材质叠加、动作相位与逐场景后处理常量继续按实际事件核验。

## 复现入口与维护

在目标工程根可执行以下已存在的入口；命令会更新项目内产物，先核对输入清单与当前编辑器状态。后台 Worker 使用隐藏窗口，不依赖鼠标；异步检查需等待最终 JSON。

```powershell
Set-Location -LiteralPath 'F:\UnityProject\HYYYYY'
& 'Assets/YogCharacterReconstruction/Tools~/EnsureLocalURP.ps1'
& 'Assets/YogCharacterReconstruction/Tools~/InvokeWorker.ps1' -Method 'YogReconstruction.Editor.YogCapturedTextureValidation.Validate'
& 'Assets/YogCharacterReconstruction/Tools~/InvokeWorker.ps1' -Method 'YogReconstruction.Editor.YogShaderValidation.Validate'
& 'Assets/YogCharacterReconstruction/Tools~/InvokeWorker.ps1' -Method 'YogReconstruction.Editor.YogTemporalShaderValidation.ValidateCapturedInputs'
python 'Assets/YogCharacterReconstruction/Tools~/Shader/validate_temporal_gpu.py'
& 'Assets/YogCharacterReconstruction/Tools~/InvokeWorker.ps1' -Method 'YogReconstruction.Editor.YogAOShaderValidation.ValidateCapturedInputs'
python 'Assets/YogCharacterReconstruction/Tools~/Shader/validate_ao_gpu.py'
python 'Assets/YogCharacterReconstruction/Tools~/Validation/compare_full_scene.py' --candidate 'CapturedPose.png' --prefix 'FullSceneComparison2'
```

重建资产的依赖顺序、菜单和完整命令见复刻根的技术报告“复现与回归”；单资产维护见各自 README。新角色应建立自己的 `Characters/<Character>`、取证 manifest、事件映射与验证前缀，先检查共享 Shader/RendererFeature 是否覆盖其实际变体，再决定是否扩充实现。
