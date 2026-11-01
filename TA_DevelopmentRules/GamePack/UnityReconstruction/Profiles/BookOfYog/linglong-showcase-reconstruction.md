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
