# AlphaAce 工程恢复与大厅武器检视案例

> 类型：PROFILE；整理日期：2026-11-07。
> 目标：恢复现有工程的本地编辑与大厅独立武器检视，并总结可迁移的制作、资源恢复和排错方法。
> 结论边界：当前属于离线 Editor 展示恢复；没有同版本原游戏基准，不能声称完整客户端或逐像素还原。旧的少量样本验证未覆盖所有目录条目。

返回 [模块入口](../../README_Tech_UnityCaptureReconstructionRules.md)。方法分别见 [工程与依赖恢复](../../references/legacy-unity-project-and-dependency-recovery.md)、[展示与定制渲染](../../references/weapon-showcase-and-custom-rendering-recovery.md)、[工具与证据](../../references/unity-recovery-tooling-and-evidence.md)。

## 1. 项目事实、目标与来源

### 1.1 路径别名与版本

以下路径属于本案例，迁移到其他工程时重新定位；跨磁盘证据不伪装为规则库内部链接。

| 别名 | 当前路径 |
| --- | --- |
| `BASE` | `G:/valkyria/AlphaAce` |
| `PROJECT` | `BASE/unlimited/project` |
| `EVIDENCE` | `BASE/unlimited/showcase-recovery-20261105` |
| `AUDIT` | `BASE/unlimited/render-analysis-20261105` |
| `REPAIR` | `BASE/unlimited/render-repair-20261104-152514` |
| `MESH_FIX` | `BASE/unlimited/showcase-mesh-recovery-20261107` |
| 当前编辑器 | `C:/Program Files/Unity/Hub/Editor/2019.4.17f1/Editor/Unity.exe` |

`ProjectSettings/ProjectVersion.txt` 指定 `2019.4.17f1 (667c8606c536)`。本地 URP 包标签为 `7.2.1`，但源码含 Athena 定制；不能替换为官方同版本包后假定行为一致。manifest 用本地 `file:` 接入 URP/Core/ShaderGraph、TMP、Zeus、地形、HLOD 等包。

用户当前聊天工作目录与真正 Unity 工程不是同一路径；工程恢复以 `PROJECT` 为准。旧恢复文档中的 `C:/Users/xverse/...`、`E:/matr1x/lemon/...` 是历史环境，不能作为当前启动或备份已存在的证明。

### 1.2 输入目录的实际作用

| 位置 | 作用与处理 |
| --- | --- |
| `BASE/art_assetBackup` | 含 Animation、Hall_CDN、weapons_CDN 和场景等多类备份，不能整体当完整 Unity 工程 |
| `BASE/unlimited/project` | 实际工程、定制包、恢复脚本与场景 |
| `BASE/protocol/out/excel` | 二进制配置表和 Lua 表的现成导出物 |
| `BASE/unlimited/ds/ProjectSettings` | 历史层配置候选来源 |
| `BASE/platforms/AssetBundles` | 平台资源包与 manifest，读取结果需另验 |
| `BASE/HoudiniProjects`、`WwiseProject` 等 | DCC/音频作者内容；存在目录不等于作者链已验证 |
| `external`、`tools`、`tridentscript`、CI 目录等 | 外部依赖与构建工具候选，按当前功能收窄恢复范围 |

### 1.3 正确的展示基准

本任务已明确选择“大厅里的独立武器检视”。它由环境、检视 Prefab、相机、质量设置和运行状态共同组成，不是打开任意枪械测试场景。

| 来源 | 在恢复中的职责 |
| --- | --- |
| `Assets/Scene/Hall/lobby_Lens.unity` | 环境、灯组、场景渲染设置 |
| `Assets/Scene/Hall/Prefab/itemshow_prefab.prefab` | 独立检视的对象与灯光安排 |
| `Assets/Scene/HallRoot.prefab` | 相机来源 |
| `Assets/Scene/Hall/ToneMapping.prefab` | 定制调色来源 |
| `Assets/RenderLoop/Pipeline_Lobby_High.asset`、`Renderer_Lobby_High.asset` | 大厅管线与自定义效果 |
| `Assets/Recovery/Scenes/WeaponShowcase.unity` | 离线组装后的展示场景 |

`Assets/Scene/Test/StandardScene_gunsMaterial.unity` 含极端 Emission/Tint 测试参数；`Assets/StandardScene/Scene/StandardScene_gun.unity` 是材质/灯光测试入口。图标环境也不是交互检视基准。这些场景可做诊断，不能直接作为正式还原结果。

## 2. 编译与缺失能力：现象、根因和修正

### 2.1 定制引擎类型缺失

现象：官方编辑器缺少 `StreamedFileAsset`、`LandscapeCollider`、`VirtualTexture` 等类型。根因是原生/定制引擎能力缺口，不是普通 using 缺失。用户明确允许屏蔽这些能力。

当前 `Packages/com.vertexgames.customterrain` 内相关实现由 `ALPHAACE_NATIVE_TERRAIN` 隔离，保留兼容入口，流送查询返回不可用结果。效果是本地非地形展示可以继续；原地形流送、原生碰撞和虚拟纹理没有因此恢复。重新启用前需要匹配的原生引擎/插件及相应场景验证。

### 2.2 依赖契约与 API 兼容

以下来自 `PROJECT/LOCAL_DEBUG_RECOVERY.md` 的历史恢复记录，结合当前文件核对；早期编译证据所在旧环境目录未在本次重新跑完。

| 问题 | 已采用的修正 | 验证与限制 |
| --- | --- | --- |
| `GamePlay.dll` 对 System.Memory 强引用不符 | 采用 AssemblyVersion `4.0.1.1` 候选，保留 meta | NuGet 标签不是程序集身份；候选来源未证明同 SVN 修订 |
| NPOI 与压缩/加密依赖不匹配 | NPOI `2.5.4 net45`、Portable.BouncyCastle `1.8.6`、SharpZipLib `1.3.1.9` | 历史有 XLSX/ZIP 契约验证；保留来源和许可证 |
| Google 库要求新版 Json | 保留定制 Newtonsoft.Json 8 的 Unity 转换器，隔离要求 13 的上传功能 | 不关闭版本检查，也不把不可用上传返回成功 |
| 新旧 Unity/TMP/Editor API 不符 | 按当前程序集与序列化字段适配 | 不把升级整套包当默认解法 |
| HLOD 递归序列化 | 保留原对象图和程序集，记录诊断 | 历史记录涉及 104 个资产、23,724 处组件引用；不是本次重扫统计 |
| Wwise 作者工程缺失 | 未配置的自动导入可提示并返回，显式动作保留失败 | 没有证明原生音频完整可用 |

历史隔离编译记录为 firstpass `0 errors / 18 warnings`、主程序集 `0 / 393`、Editor `0 / 139`，不等于工程无警告，也不代表当前全部资源及 Player 已验证。隔离输出没有人工覆盖 Library。

### 2.3 默认关闭的能力宏

当前约束的详细解释由源工程 `LOCAL_DEBUG_RECOVERY.md` 维护。这里保留迁移判断，不复制操作手册。

| 能力宏 | 当前边界 |
| --- | --- |
| `ALPHAACE_REALTIME_VOICE` | RTC/YouMe 不可用 |
| `ALPHAACE_AMD_FSR` | 对应 FSR 功能不可用 |
| `ALPHAACE_RUNTIME_ASSET_UPDATE` | 热更、分包、CDN 与应用内更新停用；保留必要本地初始化 |
| `ALPHAACE_CUSTOM_RENDER_EXTENSIONS` | 原抓屏刀光、LOD、PVS、特殊剔除/统计等不完整 |
| `ALPHAACE_CUSTOM_TMP_EXTENSIONS` | 定制图文事件及更新优化停用，普通兼容入口保留 |
| `ALPHAACE_CUSTOM_WWISE_CALLBACKS` | 额外回调委托停用，不据此宣称全音频系统失效或恢复 |
| `ALPHAACE_UNITY_FATAL_LOG` | 不引用缺失的 Fatal 枚举，普通错误处理保留 |
| `ALPHAACE_GOOGLE_CLOUD_UPLOAD` | 远端上传不可用，不产生假成功或删除待上传数据 |
| `ALPHAACE_TEXTURE_DENSITY_EDITOR` | 缺失定制检测/CI 入口不可用 |
| `ALPHAACE_PREFAB_GENERATE_EDITOR` | 不匹配的生成器和 Animator 工具不可用 |
| `ALPHAACE_TRIDENT_CUSTOM` | 定制任务注册不可用 |
| `ALPHAACE_CUSTOM_CI_TOOLS` | 缺依赖的优化、打包、配置和检查入口在写资产前失败 |
| `ALPHAACE_MATCHED_ASSET_BUILD_PIPELINE` | 当前候选包不足以恢复原 AssetBundle 构建 |
| `ALPHAACE_CUSTOM_TEXTURE_EDITOR` | 定制图集/图标生成停用；临时 importer 改动归还 |
| `ALPHAACE_D11_SCENE_TOOLS` | 缺失场景工具组默认关闭 |
| `ALPHAACE_CUSTOM_TERRAIN_EXPORT` | 缺定制地形数据时在输出前失败 |

`Assets/Scripts/Common/AlphaAceCapabilities.cs` 向调用方提供能力状态。编译宏打开只说明实现参与编译，不保证资源、服务或插件就绪。

## 3. 资源与配置恢复

### 3.1 GUID 审计是分流，不是画面验收

`AUDIT/dependency-audit.json` 声明仅扫描序列化 YAML 与 importer GUID，包含非激活对象，没有 live Unity 验证。历史索引含 117,529 个 GUID、9 个入口。

| 入口简称 | 已解析资产 / 未解析 GUID | 判断边界 |
| --- | --- | --- |
| lobby_Lens | 217 / 50 | 包含非当前检视必需节点 |
| HallRoot | 27 / 16 | 需追相机实际路径 |
| itemshow prefab | 32 / 1 | 缺烟雾 MainTex，不能由此推断主体材质全坏 |
| Lobby pipeline | 31 / 2 | stencilDeferredPS/materialErrorPS，Forward 是否使用需区分 |
| StandardScene_gun | 370 / 8 | 测试场景，不是正式基准 |
| AK47_1P | 113 / 36 | 大部分为 Controller/Animation 依赖 |
| M4RAS_1P | 115 / 36 | 同样需区分检视 Idle 与完整动作 |
| AK47_Blending | 111 / 38 | 另有法线/遮罩槽，要检查动态皮肤生成 |
| IconEnv | 5 / 0 | 无缺 GUID 不代表适合作为大厅检视 |

备份中找到基础瞄准 Controller，但仍有 7 项依赖缺口，没有把一个残缺 Controller 当完整动画修复。GUID 能解析后还要检查 fileID、类型、子资产与运行时选择。

### 3.2 恢复生成配置与层编号

`EVIDENCE/restore_inputs.py` 从 `BASE/protocol/out/excel` 恢复：

- `bin_client` 的 179 个 `.bytes` 到 `Assets/ConfigAsset/XlsConfig`。
- `lua_table_out_client` 的 335 个 `.lua` 到 `Assets/LuaScripts/config/data`。
- 从历史 `TagManager.asset` 补当前空缺的 Layer 8–26。

已有同哈希文件跳过，不同内容拒绝覆盖；源与目标记录在 `restored-inputs.json`。Layer 17 原名是 `Layer_Projectile`，检视代码复用这个数字，不应按用途改名猜测。未找到的 `Layer_Capture` 不猜编号，不覆盖整份 TagManager。

GUID、Lua 路径、表 ID、BundleConfig 名称和运行生成输入各有独立依赖关系。只补 Prefab 无法让皮肤与展示参数自动齐全。

### 3.3 历史修复文件的解释

`REPAIR/configuration-changes.json` 记录 Graphics/Quality 配置恢复及 Battle Renderer 的缺失 `GrabTextureAfterTransparent`、`AMDFSR` 功能剔除；不是实现了这些效果。`validation.json` 的 13 个 Renderer 脚本引用有效、材质引用有效、Editor errors 0 属于静态阶段，GPU 当时仍标 Pending。

历史 `shape-flags-validation/summary.json` 有 exit 0、errors 空和 396 warnings。不能把它缩写成“项目零问题”。

`hlod-meta-backup-20261104-121001/recovery.json` 实际记录 Sprite GUID 恢复：从 atlas packed 索引及模板 meta 还原、border 归零。目录名含 HLOD 不代表它证明 HLOD 递归问题已解决。

## 4. 灯光、反射与后处理恢复

### 4.1 灯组和对象层

| 灯组 | 真实灯与强度 | 独立武器检视 |
| --- | --- | --- |
| PROPS | WeaponMainLight 4.5、WeaponFillLight 5 | 开 |
| TP | CharMainLight 3.6、CharFillLight 14 | 关 |
| FP | FPMainLight 5、FPFillLight 4 | 关 |
| BakeGI | 烘焙辅助 | 关 |

原 1P 武器通常处于 layer 11，检视时切整个层级到 layer 17。灯 mask `132096 = (1 << 10) | (1 << 17)`。不能同时开启三组灯，也不能把 TP 参数误写成 PROPS 参数。

### 4.2 原始质量契约

| 项目 | 当前来源与值 | 解释 |
| --- | --- | --- |
| 色彩空间/输出 | Gamma；验证目标 1280×720 LDR | 没有为了“更好看”自动换 Linear/HDR |
| HDR | pipeline SupportsHDR false；相机 allowHDR true | 相机允许不等于实际 HDR RT |
| FOV | 35 | 独立展示相机 |
| 原检视局部位姿 | position `(0, 1.327, 2.2)`，rotation `(0, 180, 0)` | 离线全窗口另有居中适配 |
| MSAA/renderScale | 2 / 1 | 不是已升级到 4× MSAA |
| explicit resolution | 720 | Native resolution 开关关闭该内部缩放 |
| 附加光模式 | 序列化 1，实际 PerPixel | 本地枚举为 Disabled=0、PerVertex=2、PerPixel=1 |
| 附加光数量上限 | 3 | 没有实施 8 灯增强 |
| shadowDistance | 源资产 0；ItemShow 临时 7 | 在展示管线实例上应用 |

“1 就是逐顶点”是对官方/其他版本枚举的错误套用；本案例按本地 `UniversalRenderPipelineAsset.cs` 纠正。是否真实执行某一 Shader 分支仍以运行状态和抓帧为准。

### 4.3 反射与发白问题

场景 ambient/SH/customReflection 来自大厅。恢复的 Probe 使用 `Panorama_noSun.HDR`，强度 0.5、size 10000、mask 为 `1 << 17`。

AK47 材质使用 `CustomCubeMapEnable=1`、强度约 0.509，并共享名为 `GunReflection_Deagle.tga` 的反射资源。名字不是错误绑定的充分证据。材质 cubemap、ReflectionProbe 与平面 ReflectionManager 不是同一种系统。

用户报告关 Bloom 后仍有大面积白色。已确认不能把所有白区归因 Bloom；需要分别对 base、emission、mask、normal、直接光、反射和 Tone 做同输入 A/B。此前没有完成每个白区的贡献分解，不记录一个未经证实的唯一根因。

### 4.4 定制后处理与初始化

`RenderBloomHall` 由自定义 RendererFeature 注入，限制 `Camera.main`，另受 `EnableBloomHall` 和 `performanceSettingEnable` 控制。当前源 Renderer 参数：

| 参数 | 值 |
| --- | --- |
| Filter | Dual |
| dualFilterCount | 4 |
| threshold / intensity / size | 0.9 / 0.8 / 2.5 |
| color | `(1, 0.9775735, 0.8773585)` |
| 注入位置 | BeforeRenderingPostProcessing |

相机标准 `renderPostProcessing=false` 不代表该 Bloom 不运行。PBR_Dynamic 的材质路径中已有 PostColor/ToneMapping，再处理 emission；不能未经依据再叠一套 ACES。

恢复宿主的 `RefreshToneMapping` 执行 Volume 更新 → `UpdateAceToneEffect`/`ApplyAceToneParams` → 启用 `_TONEMAP_CUSTOM` → 重置 Skin tint。首次 Update 延后初始化，处理场景组件 OnEnable 的顺序。历史实机记录 Tone curve `(13,10,10,0.5)`、channel `(1,1,1,1)`。

这些配置和实际帧证明当前链路可运行；没有原游戏 RenderDoc，不能据此宣布所有 Pass 原样执行或最终画面一致。

## 5. 展示、皮肤和目录的实现

### 5.1 当前源码职责

| 文件，均相对 PROJECT | 职责 |
| --- | --- |
| `Assets/Editor/AlphaAceRenderingRecovery.cs` | 基础渲染配置修复入口 |
| `Assets/Editor/AlphaAceShowcaseBuilder.cs` | 场景装配、配置目录、代表样本、诊断 |
| `Assets/Editor/AlphaAceShowcaseValidation.cs` | 历史实机、目录资产与生命周期检查 |
| `Assets/Editor/AlphaAceShowcaseBatchBridge.cs` | 显式启用的本地 batch 指令桥 |
| `Assets/Scripts/Recovery/AlphaAceWeaponShowcase.cs` | 选择、按需加载、相机、临时管线与交互 |
| `Assets/Scripts/Recovery/AlphaAceShowcaseWeaponUtility.cs` | Idle/姿态、可见网格与包围盒 |
| `Assets/Scripts/Recovery/AlphaAceOfflineSkin.cs` | 本地皮肤生成适配 |
| `Assets/Scripts/SkinSystem/SkinResourceEditorProvider.cs` | 本地配置与资源映射 |
| `Assets/Editor/AlphaAceShowcaseMeshValidation.cs` | 本轮新增的运行网格与切换专项验证 |

### 5.2 姿态与构图

读取 `tbl_weapon_inspector_view_config.lua`，对 AnimatorOverrideController 的 Idle 采样第 0 秒，再补偿 root bone，应用 angle/scale/offset。默认交互 yaw 为 340；AK47 scale 0.555，M4RAS scale 0.6。

可读网格使用骨骼矩阵、bindpose 与权重求世界顶点，排除退化三角形，避免 Idle 折叠的备用弹匣把中心拉偏。透视 fit 使用旋转后的八角点、FOV 和 aspect；当前是全窗口展示适配，不等于原检视 UI viewport。构图仍存在固定 yaw 等项目约束，不能直接称通用相机框架。

最初该算法直接读原 mesh 数据，只覆盖可读网格。2026-11-07 用户在运行换 Famas/闪光弹时提供的错误揭示了真实缺口，修正与新证据见第 8 节。

### 5.3 程序皮肤

复用原 SkinSystem 的本地 Editor provider，不启动登录/下载。传入 item、paintKit、seed、wear，设置 CRT 大小 `1024/256`，编辑态推进生成任务，记录 Generating/Succeed/失败和 60 秒超时。

切换前保存 sharedMaterials，释放 handle 后恢复材质和 DebugState。`Secai`/`SeCai` 历史差异采用精确匹配优先，大小写候选必须落到唯一目标路径；不同目标时报歧义。

已实机验证的程序皮肤输入为 `item=111030005, kit=60, seed=0, wear=0`。超时当前主要设置失败状态，不能等同原异步任务已取消；异常即时清理、过期回调 token 和单例尺寸恢复仍是进一步完善项。

本轮新增生成前本地输入检查，复用原 PaintKeyValues 与 Body/Scope 路径，检查材质、Shader 和待加载纹理；缺资源时状态为 Unavailable，归还本次 DebugState/材质，保留原 Prefab 材质。AUG_Goth 的枪身 `AUG_Goth_C.tga` 当前缺失，降级显示不等于这款皮肤已恢复。

### 5.4 目录覆盖与发布边界

当前从 336 个配置条目筛出 334 个可选条目，缺少：

| item ID | 资源名 |
| --- | --- |
| 144060001 | Grenade_AirOverlord_1P |
| 151020001 | C4_Lightning_1P |

334 是配置条目数，同一枪型可能有多种皮肤；不是 334 个独立枪型。界面分页 12 条、支持搜索、按需创建当前模型。三个内置样本之外的条目通过 `UNITY_EDITOR` 下的 AssetDatabase 加载，当前 Player 不支持该目录链路。

早期 `catalog-asset-validation.txt` 实际只额外加载 index 3 与 333 的 Prefab；其中“runtime error count”命名不准确，是资产检查，不是所有条目实际 PlayMode/GPU 验收。用户后续报错证明不能从这份报告外推全目录运行成功。

### 5.5 临时状态的所有权

展示使用 `Instantiate(lobbyPipeline)` 与 `HideAndDontSave`，保存 Graphics/Quality 引用、Bloom 开关，临时设 shadowDistance=7。释放时仅在当前引用仍是自己的实例时归还，并以 suspended 阻止 Update 再接管；Disable、退出和程序集重载均有清理入口。

这不是完整的全局状态隔离：管线克隆没有深克隆全部 Renderer/Material；Tone keyword/vector、Skin 单例纹理尺寸尚未全面快照恢复。相关状态在其他宿主复用前仍需逐项核对。

## 6. 背景与资源包尝试

最新恢复场景用 `Assets/UI/Images/Background/bg_itemshow.tga`，1024×1024、24 bit。这是现有 ItemShow 资源替代缺失主题，未确认是原游戏默认武器主题。

实际 Lua 路径为 `Assets/LuaScripts/logic/manager/inspectormanager.lua` 中的 `DefaultWeaponThemePath = UI/LargeImages/Weapon/Weapon_Theme_Default.png`，当前缺失；通用背景另有 `com_bg_v3.png`。旧恢复记录仍写通用背景，不能盖过当前源码和诊断。方形背景 cover 到 16:9 会裁剪，现有图顶部 logo 被裁，背景构图尚未完整还原。

平台包 `BASE/platforms/AssetBundles/Development/iOSpvrtc/ui_bg_itemshow_a.ab` 的 manifest 指向 `Assets/UI/Images/Background/bg_itemshow_A.png`。原生加载实际日志为：

```text
Unable to read header from archive file: ...
Failed to read data for the AssetBundle 'ui_bg_itemshow_a.ab'.
InvalidDataException: AssetBundle.LoadFromFile returned null.
```

首个失败在归档头，未到纹理解码；不能由目录名推出 PVRTC 不兼容。包装、偏移、加密、损坏和版本均为待查候选，没有成功导出。一次性提取脚本已删除，保留 `assetbundle-itemshow-a.txt` 与 `unity-extract-itemshow-a.log`。

UnityPy、AssetRipper、AssetStudio 是后续可选工具，本案例没有它们成功解出该背景的证据。

## 7. 验证证据、时点与可支持的结论

### 7.1 旧展示验证

| 证据 | 记录的行为 | 支持范围 |
| --- | --- | --- |
| `EVIDENCE/runtime-validation.txt` 与 `unity-validation-bg-itemshow.log` | Unity 2019、RTX 3060、D3D11；1280×720 LDR；3 个代表配置及返回/重选；运行错误 0 | 2026-11-07 17:22 左右对应样本 |
| `AK47-original.png`、`M4RAS-original.png` | 两个基础款恢复输出 | original 是基础款命名，不是原游戏截图 |
| `M4RAS-SeCai.png` | 程序皮肤生成 Succeed | 指定 kit/seed/wear |
| `M4RAS-return.png`、`M4RAS-SeCai-repeat.png` | 返回与重复选择输出 | 当前样本的重复性 |
| `showcase-diagnostics.txt` | 334 条目、新背景、姿态与输入 | 构建时诊断，不替代逐条运行 |
| `catalog-asset-validation.txt` | 静态目录数与两项 Prefab 加载 | 不覆盖非可读网格运行路径 |
| `lifecycle-validation.txt` | 禁用、启用、重开与释放跨 5 次 Editor update 成功 | 该轮管线引用恢复 |
| `before-render-settings.json` | 5 个渲染设置的旧哈希 | 比较基线，不代表所有全局状态 |

`final-verification.json` 是当天 02:09 的旧汇总，截图后来在 17:22 被覆盖；不能自动作为新背景版本的汇总证据。`verify_final.py` 硬读早期 `unity-render-v2.log`，不要盲目重跑将多个批次拼成一个成功报告。

当前文档附带 [证据快照](evidence-snapshot.json)，记录整理时可读文件的哈希、时间和重复图比较。它是文件审计，不是新一次原游戏对照或全部 Unity 回归。新增网格修复另有独立运行目录。

整理时五项旧基线中，三个管线/Renderer 资产哈希仍一致；GraphicsSettings 与 QualitySettings 已不同，文件修改时间早于本轮首次真实工程网格回归。未把差异直接归因某次测试，也未盲目覆盖当前设置；旧的“设置完全未变”结论不能沿用到现在。

### 7.2 Evidence → Finding → Path

| Evidence | Finding | Path |
| --- | --- | --- |
| ProjectVersion、manifest、本地 URP 源码 | 定制 URP，不能按官方版本标签推等价 | 保留本地包与版本；恢复准确管线引用 |
| 原生类型编译错误及 terrain 条件编译 | 缺定制引擎能力 | 用户允许的能力隔离；明确流送/碰撞未恢复 |
| 导出表、BundleConfig、Lua 调用 | 展示依赖生成配置和字符串映射 | 幂等复制脚本、冲突检查、表加载验证 |
| 场景灯组、Layer、运行管理器 | 独立检视与 TP/FP 互斥 | PROPS 灯组、layer17、原始模式 |
| 定制枚举源文件 | 数字1是 PerPixel | 更正旧解释；保留实际数量限制 |
| Bloom Feature 与 PBR_Dynamic 调色 | 标准后处理开关不能代表全链 | 恢复独立开关、Tone 初始化和材质输入 |
| 皮肤任务结果与重选图片 | 动态材质依赖完整生成链 | 本地 provider、输入固定、句柄/材质恢复 |
| 包头错误日志 | 尚未进入 PVRTC 解码 | 保留原包；先查归档头，不声称成功提取 |
| 用户运行截图与旧目录资产报告 | 静态存在性检查漏掉 PlayMode 读取权限 | 新增非可读网格与真实切换回归，见第8节 |

## 8. 本轮新增：运行换枪的非可读网格问题

### 8.1 事实与根因

用户截图指出 `Famas_1P_Replaced`、`Grenade_Flash_1P_Replaced` 的 `isReadable=false`，读取 vertices/triangles 报错，随后 `PrepareWeapon` 抛出无法计算包围盒。

旧算法直接从原 Mesh 读顶点、骨骼权重与索引。编辑态可以访问的模型，在 PlayMode 可能已丢弃 CPU 数据；“Prefab 能加载”与“运行时可读网格”不是同一检查。

### 8.2 实际修正与进一步发现

`AlphaAceShowcaseWeaponUtility.SkinnedWorldVertices` 增加可读性分支：可读源沿用原算法；不可读源用 `SkinnedMeshRenderer.BakeMesh` 创建临时可读网格，再做可见三角形过滤，finally 销毁临时网格。不批量修改原 mesh/importer 的 Read/Write。

独立 Unity 2019 测试进一步发现 BakeMesh 输出已带世界缩放，直接再用 TransformPoint 会重复缩放。实际修正以 lossyScale 的倒数补偿，再应用 localToWorld。单位、0.6、2 三种缩放 × 激活/非激活的六组合成样本已通过，最大误差小于 `1e-6`；早期失败结果保存在 `mesh-worker/result-before-scale-fix.txt`。

场景 Builder 的同名采样入口改为复用 utility，避免两套读取实现分叉。`AlphaAceWeaponShowcase.LoadCatalogWeapon` 的失败路径会释放皮肤、停用并销毁临时对象，避免异常后残留孤立枪械。

扩大到目录检查后还发现两类不同问题：部分原型缺检视配置；拳头 Prefab 没有独立网格。前者提供通用预览角度、单位缩放和自动居中，并在界面提示“使用通用检视姿态”；这不是原游戏姿态恢复。后者提示“此条目没有独立展示模型”，不创建假模型、不抛连带异常。静态 MeshRenderer 也纳入包围盒；不可读静态网格用本地 bounds 八角点保守估计。

配置查找先恢复原 Lua 的“item 派生 ID → 配置表 WeaponType”回退，只有仍缺配置才采用通用姿态。粒子材质检查区分停用 Renderer、主材质和启用的拖尾；Steampunk 的 Shader/材质文件正常，错误来自把未启用拖尾的空槽误当成缺失资源。加载改为在停用的临时父节点下实例化，完成准备再激活，减少原游戏 OnEnable 抢先执行的副作用。

### 8.3 验证与回退入口

新增 `AlphaAceShowcaseMeshValidation`，可在展示场景 PlayMode 用菜单 `Tools/AlphaAce/Weapon Showcase/Validate runtime mesh switching` 显式运行；后台入口为 `AlphaAceShowcaseMeshValidation.BatchValidate`。测试在独立 run 目录记录唯一 Prefab/pose 几何覆盖和实际换枪，并输出截图。

本轮真实项目的最终结果与范围见 [运行换枪回归记录](runtime-mesh-switching-regression.md)。合成测试不替代真实项目验证；全目录几何检查也不等于全皮肤逐条视觉验收。

最终 `run-20261107-205922`：219 个唯一几何组合通过，11 次选择、10 张截图，专项运行错误 0，Unity 退出码 0；截图中的 Famas 基础/Snake/Steampunk/Blending 和闪光弹均实际显示。28 个组合使用通用姿态，3 个组合无独立网格；AUG_Goth 只验证了缺贴图降级提示。返回 Famas 的 PNG 哈希不同，不声称本轮图像确定性通过。

修改前 4 个源文件保存在 `MESH_FIX/*.cs.before`。回退先比较当前文件与本轮改动，再定点恢复；不要覆盖之后其他功能的修改。临时验证请求仅在显式运行时使用，完成后清理，不留下未来启动时自动执行的请求。

## 9. 工具经验与后续工作

### 9.1 本次与候选工具的区分

| 工具 | 本案例状态 |
| --- | --- |
| rg / PowerShell / Python | 实际用于源码索引、日志、哈希、清单和文档检查 |
| Unity 2019.4.17f1 | 历史展示验证及本轮网格合成/真实工程回归宿主 |
| Unity Roslyn/程序集元数据 | 历史依赖/API/编译恢复手段；不能用旧路径证明当前工具存在 |
| AssetDatabase、AssetBundle API | 资产加载实际使用；指定背景包的原生读取失败 |
| computer-use | 本轮尝试连接，native pipe 不可用；没有伪造 GUI 操作结果 |
| RenderDoc / Frame Debugger | 推荐用于精确链路比较；本文未引用原游戏实际抓帧结果 |
| UnityPy / AssetRipper / AssetStudio | 后续提取候选；没有该背景成功导出证据 |

后台验证不使用 `-nographics`；异步 PlayMode 入口不追加 `-quit`，由验证器完成后退出。同工程不同时启动两个 Editor，按实际进程及 project path 确认。窗口不可控时，可在工程已关闭的条件下采用独立后台验证，不假定 GUI 已刷新。

### 9.2 未完成范围与下一步

| 未完成项 | 下一步证据 | 不能提前写的结论 |
| --- | --- | --- |
| 原游戏画面一致性 | 同版本、同镜头、同皮肤的参考/抓帧 | 完整还原、逐像素一致、还原率 |
| 默认武器主题背景 | 原主题资源或可证明的映射 | 当前替代图就是原默认背景 |
| 全主题/特效/动画 | 分类覆盖与运行截图、时间行为 | 334 条目全部视觉无误 |
| AUG_Goth 程序皮肤 | 补齐同版本枪身颜色图再验证生成 | Unavailable 降级显示等于原皮肤还原 |
| 缺失两项 Prefab | 同版本资源与依赖闭包 | 所有枪械/道具完整 |
| 地形与原生能力 | 配套定制引擎和运行插件 | 屏蔽类型等于修复地形 |
| 全局状态隔离 | Tone、Skin 单例、Renderer/Material 深层状态快照 | 临时管线已无任何宿主污染 |
| Player/移动端/完整游戏 | 加载器、资源构建、平台插件及业务验证 | Editor 展示可用等于可发布客户端 |
| 更高画质配置 | 与原始配置分离的增强方案和性能/视觉验证 | 原始配置被改好看即还原更准 |

后续复用建议按“先找真实工程 → 保留定制包 → 分类缺能力 → 建多种依赖图 → 恢复具体入口 → 分项渲染排查 → 实际 PlayMode/GPU 验证 → 固定证据批次”推进。项目参数留在本 Profile，可迁移方法回到三篇 reference。

## 10. 原工程文档与维护边界

源工程仍维护：

- `LOCAL_DEBUG_RECOVERY.md`：编译兼容、能力宏和历史依赖记录。
- `docs/2026-11-05_武器展示还原分析-report.md`：入口与资源分析。
- `docs/2026-11-07_武器展示恢复记录.md`：阶段恢复记录，背景等以最新源码核对。
- `docs/武器展示使用说明.md`：具体场景功能使用。

规则库不复制资源、实现脚本或整份功能 README。新增事实先更新本 Profile 的证据与状态，跨项目复现后再提炼到 reference；本轮没有新增或重排 CORE ID。

