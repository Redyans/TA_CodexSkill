# 《龙之剑觉醒》六角色展示还原 Profile

> 类型：PROFILE；证据快照：2026-11-01；语言与编码：中文，UTF-8。

## 概览与证据边界

本页记录《龙之剑觉醒》六组本地客户端、截图与 RenderDoc 抓帧在 Unity 2022.3.62f3 / URP 14.0.12 中的还原结果、根因、工具和维护边界。适用规范为 [客户端到 Unity 复刻模块](../../README_Tech_UnityCaptureReconstructionRules.md) 的 `UCR-EVD-01/02`、`UCR-AST-01/02`、`UCR-ISO-01`、`UCR-VAL-01/02/03`。本页数值与坐标约定属于当前项目，不能直接推广到其他 UE 游戏。

六个可实时播放的角色展示场景已经形成，覆盖模型、身体/面部/武器动作、角色 Shader、原生光照与后处理链，并保留源码、工具和验证数据。不重建原游戏 UI。原生连续动态历史和全部动画时刻的同帧等价未得到证明，最终状态是“实现已交付并有分层证据”，不是“每一帧逐像素相同”。

配套方法按需阅读：[UE 解包工具链](../../references/unreal-extraction-toolchain.md)、[UE 动画与次级运动](../../references/unreal-animation-and-secondary-motion.md)、[UE 渲染链移植](../../references/unreal-render-pipeline-porting.md)、[隔离 Unity Worker 与交付验证](../../references/isolated-unity-worker-and-delivery-validation.md)。单功能说明仍以项目资产旁的 README 和 `Documentation/` 为准。

### 路径别名

本页代码路径默认相对 `ROOT`；带 `PROJECT`、`GAME` 或 `CAPTURE` 的路径使用对应根。跨磁盘证据用“根别名 + 相对路径”定位，不伪造规则库到工程的相对链接。

| 别名 | 本次本地路径或版本事实 |
| --- | --- |
| `PROJECT` | `F:\UnityProject\HYYYYY` |
| `ROOT` | `PROJECT/Assets/DragonSwordReconstruction` |
| `GAME` | `G:\SteamLibrary\steamapps\common\DragonSword  Awakening` |
| `CAPTURE` | `PROJECT/Cap/龙之剑觉醒`，子目录 `1` 至 `6` |
| 源解析 | UE 5.3 分支，CUE4Parse `EGame.GAME_DragonSwordAwakening` |
| 源 GPU | D3D12、SM 6.2；本批抓帧内部渲染尺寸 `2128 × 1184`，最终显示窗口可缩放 |
| 目标宿主 | Unity `2022.3.62f3`、URP `14.0.12`、Windows D3D11 |
| 交付清单 | `Validation/final-delivery-manifest.json`，UTC `2026-11-01T01:15:09.483197+00:00` |
| 资源快照 | `Validation/final-resource-audit.json`，UTC `2026-11-01T01:14:45.228322+00:00` |

## DSA-01｜保存任务目录、角色归属和既有工程边界

所有本任务资源、配置、源码、证据与工具集中在 `ROOT`。`Characters/`、`Shared/`、`Scenes/`、`Rendering/` 等是 Unity 资产；`Source~`、`Evidence~`、`Tools~`、`Packages~` 是 Unity 忽略的工作资料目录。界面展示动作已按角色目录归属保存，独立武器骨架另行标注。

| 抓帧目录 | 角色目录 `Characters/…` | 正式场景 `Scenes/…` | 身体/面部 clip | 角色本地武器 clip |
| --- | --- | --- | ---: | ---: |
| `1` | `01_Onette` | `Character01_Onette.unity` | 32 | 3 |
| `2` | `02_Lute` | `Character02_Lute.unity` | 67 | 0 |
| `3` | `03_Tarte` | `Character03_Tarte.unity` | 25 | 0 |
| `4` | `04_Onette_Unique` | `Character04_Onette_Unique.unity` | 32 | 5 |
| `5` | `05_Kalien_Unique` | `Character05_Kalien_Unique.unity` | 28 | 0 |
| `6` | `06_Johnny` | `Character06_Johnny.unity` | 61 | 0 |

当前资源集合含 25 个角色模型 FBX、1 个共享武器模型、245 条身体/面部动作、8 条角色本地武器动作和 2 条共享武器菜单动作。总计 255 个 `.anim` 与 255 个动画 FBX，510 文件、18,802,072,485 字节；源动画依赖去重后为 363 个包。展示时间线有 53 个本地动作、44 个唯一 Montage、48 个唯一 Notify。计数不是“一份 Montage 等于一条 clip”，详见各角色 `Animations/source-animation-manifest.json`。

项目采用 `Rendering/DragonSwordPipeline.asset` 和自有 RendererFeature。本任务没有修改主工程 URP 包源码，也没有把依盖之书的本地 URP 补丁归为本任务产物。主工程原有本地包安排继续保留；Worker 使用物理包副本，1108 个管线文件的主工程与 Worker SHA 相同。播放时临时切换专用管线和 Unlimited skin weights，结束后恢复。

既有工程基线核对 833 项，其中 831 项哈希不变；2 项依盖之书文件变化有另一个用户任务的修改证据。`allBaselineHashesUnchanged=false`，没有缺失或无法归因的变化。维护时保留该区别，不能改写成“833 项全部不变”。对应证据为 `Validation/final-delivery-manifest.json` 的 `baselineProtection` 与 `Validation/main-project-integration-audit.json`。

## DSA-02｜区分实际生产工具与候选工具

以下是已核实的实际工具用途。工具存在、下载成功或能浏览资产，不等于该工具生成了最终资源。

| 工具 | 已核实版本/用途 | 项目证据或位置 |
| --- | --- | --- |
| 定制 CUE4Parse CLI | Assembly `1.2.2.0`，本地修改，`net10.0`；正式索引、属性、模型、纹理、TRS 和引用导出 | `Tools~/Unreal/CUE4ParseSource/CUE4Parse-master/CUE4Parse.Example/Program.cs`；其 `bin/Release/net10.0/CUE4Parse.Example.exe` |
| .NET SDK | `10.0.200` | 外部安装位置 `C:\Program Files\dotnet\sdk\10.0.200` |
| Blender | `4.5.5 LTS`，构建 `836beaaf597a`；后台 GLB/FBX 转换 | `Tools~/Blender/blender-4.5.5-windows-x64/blender.exe` |
| Python / NumPy | Blender 内置 Python `3.11.11`、NumPy `1.26.4` | `Tools~/Blender/blender-4.5.5-windows-x64/4.5/python/bin/python.exe` |
| ACL / RTM | ACL `2.1.0`，revision `414689d5cff4286a7898487a46dc5e48005d38da`；RTM `2.2.0`，revision `d7982f2b2524feeb322f424b29cf43df30b5d5b7` | `Tools~/Unreal/CUE4ParseSource/CUE4Parse-master/CUE4Parse-Natives/ACL/external/acl` 及其 RTM 依赖 |
| CMake / MSVC | Native ACL 构建使用 `Visual Studio 17 2022` 生成器；未据此推定编译器补丁版本 | `Source~/Unreal/acl-build.log` 与 `CUE4Parse-Natives/builddir/CMakeCache.txt` |
| Capstone | `5.0.9`；Shipping 原生反射字段与构造默认值取证 | `Tools~/Unreal/python-libs/capstone-5.0.9.dist-info/METADATA` |
| RenderDoc | `1.46`；六组实际回放成功 | `Evidence~/RenderDoc/Character01/status.json` 至 `Character06/status.json` |
| DXC library | RenderDoc 的 `dxcompiler.dll`；DXIL 反汇编为带类型 LLVM | 外部 `C:\Program Files\RenderDoc\plugins\d3d12\dxcompiler.dll`；`Tools~/Lighting/disassemble_dxil.py` |
| D3DCompile / FXC | `d3dcompiler_47.dll` 编译 `ps_5_0`；FXC 来自 Windows SDK `10.0.26100.0` | `Tools~/Shader/compile_batch_materials_offline.py`、`Tools~/Lighting/build_fsr3_compute.py` |
| FidelityFX SDK | tag `v1.1.4`，用于匹配 FSR 3.1 头文件/常量缓冲 | `Tools~/Lighting/download_fsr3_source.py`；`Evidence~/RenderDoc/Character01/Temporal/amd-source-provenance.json` |
| Oodle | CLI 初始化使用的 DLL 已保存；独立版本未核实 | `Tools~/Unreal/oodle-data-shared.dll`；用文件哈希识别，不能猜版本 |
| FModel / UModel | 已下载或解压，未找到最终生产导出的完整日志；定位为浏览/兼容性核验候选 | `Tools~/Unreal` 中对应压缩包/程序；不作为正式导出来源 |

资源审计中 RenderDoc 的空版本字段是该审计范围的缺口；实际回放 `status.json` 的 `1.46` 是有效版本来源。部分早期工具下载未保存完整来源元数据，当前文件 SHA 只能证明当前字节，不能补证下载来源。以后补充来源时记录 URL、tag/revision、许可证和下载件 SHA，不把上游 Assembly 版本当作本地补丁唯一身份。

本地解包密钥输入不进入文档、日志或可分享清单。`Source~/Unreal/aes.local` 和 `key-candidates.local` 仅是被排除的本地秘密文件；本页不读取或复制其内容。

## DSA-03｜用原生字段结构修正解包完整性

50 个容器已挂载，0 个未加载，328,188 个文件进入索引。包数量成功不能证明每个导出对象完整；本项目最初的 Studio 属性在错误继承链/属性布局下被截断。修正以 Shipping 原生反射字段为依据，核对名称、索引、类型、继承和对象结束位置，而不是仅寻找“解析不报错”的借用 usmap。

| 现象与根因 | 本项目修正与约束 | 验证证据与限制 |
| --- | --- | --- |
| Studio 字段缺失，借用 schema 与原生类布局不同 | 合并目标引擎/游戏扩展，并从 PE 反射恢复原生布局；静态默认值与当前抓帧 CB 分开使用 | `CaptureData/character-studio-source-contract.json`、`Validation/character-studio-postprocess-schema-audit.json`：69 exports、0 errors；447 个后处理字段名称/索引/类型匹配 |
| Montage 未序列化的 Blend 值曾可能被当作 0 | 查询原生构造默认值；2 个 gacha Montage 的缺省值为 `0.25 s`、Linear | `CaptureData/native-montage-defaults.json`；不能把未序列化普遍解释为该数值 |
| 动作只按角色文件夹查找会遗漏共享武器、Notify 和依赖 | 用 AssetRegistry 与对象引用形成闭包，再按角色用途归属 | `Characters/<角色>/Animations/source-animation-manifest.json`、`Validation/display-animation-coverage.json` |

当前 CLI 将 `ROOT` 与 `GAME/DS/Content/Paks` 写在 `Program.cs` 中，不是通用可移植命令。大部分模式使用 `mode filterRegex mapping.usmap [merge]`；`references` 的第 4 参数是引用查询表达式，不能机械套用 `merge`。可用模式包括 `index`、`loose`、`raw`、`registry`、`references`、`properties`、`mappings`、`mapaudit`、`export`、`animjson`、`weights`、`texhash`、`vectorfield`、`refskeleton`。

每次普通调用都会更新共享索引/容器状态，部分模式还更新同名属性或汇总文件。因此同一输出根只允许一个写入进程；并发分析读取已冻结的产物。`DS_READ_SCRIPT_DATA=1` 读取脚本/稀疏数据；`DS_COMPONENT_NATIVE`、`DS_NATIVE_BODY_MAPPINGS`、`DS_NATIVE_NIAGARA_MAPPINGS`、`DS_NATIVE_WEAPON_MAPPINGS`、`DS_NATIVE_ANIM_MAPPINGS`、`DS_NATIVE_WIND_MAPPINGS` 决定相应 native mappings，重跑前按项目脚本恢复其依赖，不能仅复制最后一条命令。

## DSA-04｜保留绑定、顶点字节与原生动画曲线

模型与动画最终用可编辑 FBX/`.anim` 交付；正式文件安装后保留其身份。部分精确通道由派生运行时 Mesh 纠正，不能用重新导出的 FBX 无条件覆盖现有文件。

| 现象 → 根因 | 修正/维护约束 | 当前验证 |
| --- | --- | --- |
| 反射绑定与蒙皮不一致 → Blender EditBone 无法完整表达带反射的 bind，普通导入流程可能重烘焙 skin | 保留 PoseBone scale 与实际 cluster bind，避免再次 skin rebake；直接从 `FReferenceSkeleton` 导出 `p/q/s` | `Validation/exactbind-current-file-identity.json`；对应安装计划 `CaptureData/exactbind-model-install-plan.json` |
| 权重或顶点序变化 → 重复 bone influence 被字典覆盖，glTF 自定义属性集合遍历顺序不稳定 | 同骨权重求和；固定自定义法线/切线属性导出次序；保留 8 weights | `Validation/source-to-gltf-combined-weight-audit.json`：25/25，最大误差 `6.7684e-8` |
| 顶点遮罩变亮 → Blender 颜色空间转换把 `9/255` 改为约 `53/255` | 运行时派生 Mesh 使用源 UNORM8 字节；保留已安装原 FBX | `Validation/vertex-color-unity-integrity.json`、`vertex-color-source-chain.json`；`Documentation/2026-11-01_原顶点色遮罩链修复.md` |
| TBN/法线微差 → GPU SNORM8 解码 `/127` 与通用 `/127.5` 不同，镜像符号可能丢失 | 保留原 SNORM8 规则、`tangent.w` 与 renderer mirror sign | `Documentation/2026-11-01_TBN与SNORM8取证.md` |
| 面部跳变 → 合并原生/重采样的近等时间后 float 时间坍缩，切线错误 | 按 RichCurve 的 Constant/Linear/Cubic、原值与切线构建，保留负值和超过 100 的值 | 64 个 clip 修正、446 个其他动画文件不变；身体曲线/事件/设置/GUID 保留 |
| 末帧或速率不对 → 帧数与终点约定被当作普通均匀采样 | `animjson` 以 Skeleton ref `q/p/s` 初始化再取每帧，包括终点；本分支有效 keyrate 为 `FramesPerSecond*(NumFrames-1)/NumFrames` | 此公式仅针对已验证解码器/源格式，不推广为所有 UE 动画规则 |

面部最终验证覆盖 174 个 clip、370 条曲线、286,289 个 Unity 采样，最大误差 `1.7855913e-5` 个百分比点，见 `Validation/face-curves-all-existing-validation.json`。修复过程见 `Validation/face-curve-native-repair.json`、`face-curve-animation-file-change-audit.json`。菜单还原使用原 Montage section、next section、notify、socket 与独立武器 skeleton，53 项时间线通过 `Validation/display-timeline-validation.json`。

关键生成入口包括 `Tools~/Unreal/glb_to_fbx.py`、`animation_trs_to_glb.py`、`animated_glb_to_fbx.py`、`build_animation_metadata.py`、`export_weapon_notify_timelines.py`、`build_weapon_animation.py`。输入/安装计划分别保存在 `Evidence~/Unreal/eightweights-model-jobs.json` 与 `CaptureData/exactbind-model-install-plan.json`；重新导出时这些计划和绑定/primitive/index/hash 审计一起更新。

## DSA-05｜分别验证组件、世界、镜像和物理求值

原生姿态矩阵看似一致，仍可能包含四元数、世界基或物理中心错误。本项目采用下列坐标契约：

| 数据域 | UE → Unity 本项目约定 |
| --- | --- |
| 模型局部位置 | `C(x,y,z)=(-x,z,y)`，`det(C)=+1`，长度厘米乘 `0.01` |
| 模型局部四元数 | `qC=(-qx,qz,qy,qw)` |
| 全局位置 | `P(x,y,z)=(x,z,y)`，`det(P)=-1`，长度厘米乘 `0.01` |
| 全局四元数 | `qP=(-qx,-qz,-qy,qw)` |
| Actor 几何变换 | `AUnity = P * AUE * inverse(C)`；本批抓帧 Actor 为纯 yaw |
| World 物理骨基 | 原生 `Actor × BoneComponent` 先组成 World 基；不能把局部 `C` 直接用于 World 数据 |

| 现象 → 根因 | 修正/项目约束 | 验证证据与范围 |
| --- | --- | --- |
| 骑乘专属物理也同时开启 → PropertyAccess 未导出时被替换为静态 Alpha 1 | 保留 434 stages；默认启用数为 `44/42/34/29/46/21`，合计 216；Default/RideNone 为 1，Ride1/2/3 为 0，JohnnyPush 为 Blueprint local 0 | `Validation/lobby-physics-input-audit.json`、`physics-alpha-runtime-validation.json`、`physics-property-access-current-audit.json`；31 Lobby 输入，枚举 `[1,2,3,0]` 的默认 0 对应 slot 1；5253 动态检查最大误差 0 |
| Collider 偏移放大 → FBX armature 100 又乘到已换算为米的 offset | 仅消除明确的导入比例，保留原生有符号骨 scale | `CaptureData/physics-collider-native-fixtures.json`、`Validation/physics-collider-coordinate-validation.json`；43 组合 × 2 mirror × 2 scale = 172，中心最大误差 `8.9257833e-7 m` |
| 镜像后发片中心倒向 → `inverse(space.rotation)*bone.rotation` 混入反射共轭 | 读取 localRotation 链乘积并按 parent-local 回写；单独验证纯 q | `Evidence~/Unreal/physics-dynamicpose1-before-mirror-fix.json`；hairBack offset/重力 cosine 从正确 `+0.867441` 变为错误 `-0.867441` 的故障已修正 |
| 静态 reference 通过仍不能证明动画纯 q | 增加动画 t=1 的原生 fixture 与双 parity 运行时比较 | `CaptureData/physics-dynamics-animated-native-fixtures.json`、`Validation/physics-animated-dynamics-pose-validation.json`；原始 ref→GLB 704 bones，动画五角色 704 bones × 2 parity = 1408 samples，q error 0 |
| World 节点位移偏移 → 局部与全局换基混用 | 15 个正式 World 节点按全局基处理；13 个 offset 受影响，另 2 个 breast X=0 为特殊情形 | `Evidence~/Unreal/physics-world-native-coordinate-fixture.json`、`Validation/physics-world-native-coordinate-audit.json`；point/center 最大 `0.005778454/0.005808976 cm`，axes `4.3388804e-6`，q/write 0 |
| 各向异性惯量响应错误 → row-matrix 到四元数的共轭方向反转 | 原 UE `M D M^T` 对应 `inverse(q) * (D * (q * torque))` | `Validation/physics-inertia-runtime-validation.json`：720 cases，最大 `2.1155582e-8` |
| 混合约束次序与重力来源不明 | 恢复 Plane→Sphere 的 3 个活动节点；重力来自 BaseEngine `DefaultGravityZ=-980`，41 个游戏/Windows/Saved INI 未覆盖 | `Validation/physics-gravity-config-audit.json`；默认值来自配置证据，未以画面调参替代 |

原生 AD77 在 360 steps 的 source replay 中 q 误差 0°、offset 误差 `3.021854e-6 cm`，最终角度 `29.794214°`。Kawaii 88 cases 的 pose/collider/write q 误差 0、位置 `1.015468e-6 m`，parity 误差 `5.198349e-7 m`。这类 fixture 验证算法和换基，不等于相同原生初态下完整运行轨迹等价；详细推导位于 `Documentation/2026-11-01_reverse-secondary-motion-report.md` 和 `Validation/physics-kawaii-rotation-validation.json`。

Johnny 固定 t=1、关闭/全开/仅 AnimDynamics 的 360 steps 隔离观察中，179/359 帧自然下垂，未发现持续反向翻转。没有速度阈值证明已达静态平衡，也没有同初态原生转身轨迹，因此不能将此观察写成完整动态等价。

## DSA-06｜按实际 GPU 链区分实现和误差

角色渲染使用原 VS/PS、CB、input layout、sampler、纹理视图、深度/混合状态与 draw 次序，覆盖 predepth、MRT、auxEqual、shadow、lighting、reflection、translucency 和 post。原生原材质参数与 Studio 默认值用于解释配置，实际抓帧常量决定当前执行分支。

| 项目 | 已验证结果 | 证据/限制 |
| --- | --- | --- |
| 渲染模块 | 120 项 GPU 模块比较；跨 12 个正式/固定验证场景的 36 行源 draw 覆盖通过 | `Validation/rendering-module-evidence-summary.json`、`all-scene-source-draw-coverage.json`；模块结果不代替整图 |
| Onette 纹理 | 187 个纹理 mip 字节精确匹配 | `Evidence~/Unreal/Onette_RDC_texture_hash_matches.json`；其他角色绑定未全部宣称逐 hash 匹配 |
| Temporal | 六组捕获的 11 个 temporal compute Shader 字节相同；原路径为 FSR 3 native AA | `Validation/temporal-production-evidence.json` |
| 独立 FSR3 | D3D11 RGB MAE `1.108734e-5`、RMSE `7.180623e-5`；2,519,552 像素中 1 个大于 `0.01`，最大 `0.020996` | 原 D3D12 SM6.2 native-half 与 D3D11 min16float 存在精度差；不是逐 bit 相同 |
| 六角色固定姿态 | 84 阶段；显示 RGB MAE 依角色顺序为 `1.3343/1.1743/0.9014/1.2719/1.1932/1.3178`，单位 `/255` | `Validation/fixed-scene-final-stage-summary.json`；Unity 关闭 temporal AA，而原参考含 FSR history，只作诊断对比 |
| Niagara | 恢复原 VM、生命周期、vector field、源参数；生产使用显式记录的新 seed | 捕获前随机状态/历史缺失；不是原随机轨迹回放。见 `Documentation/2026-11-01_Niagara原始生命周期还原.md` |

Shader 转译入口为 `Tools~/Shader/translate_dxil.py`；DXIL 与 temporal 源处理在 `Tools~/Lighting`，抓帧导出在 `Tools~/RenderDoc`。后续扩角色时先核对实际 draw/dispatch 差异，再决定复用哪个 Kernel/Feature，不能仅复制 Onette 材质和数值。

## DSA-07｜按证据层级解读最终验证

| 验证层 | 结果 | 边界/证据 |
| --- | --- | --- |
| 离线编译 | 120 sources，0 errors、0 warnings；551 个只读输入未变化 | `Validation/full-project-csharp-compatibility.json`；只证明本次离线编译 |
| 实际 Unity 健康 | 74 个任务脚本（49 Editor、25 Runtime）、461 Shader、25 compute、1011 material；shader errors 0，issues/editorErrors 为空，六场景无 MissingScript，sourceCurrent=true | `Validation/final-project-health.json`；实际导入、GPU、播放由匹配版本隔离 Worker 完成，未操纵用户交互编辑器刷新 |
| 实际 PlayMode | 六场景各 120 frames，合计 720；errors 为空；53 个展示动作时间线通过 | `Validation/playmode-validation.json`、`display-timeline-validation.json` |
| GUID/引用 | 当时记录 4144 个引用 GUID、1666 个必需路径，均解析成功；GUID 总量随文档等资产新增变化 | `Validation/asset-guid-dependency-audit.json`；读取报告时点，不固化为永恒总量 |
| 图像人工/独立 QA | 27 图：18 张 PlayMode + 9 张 Johnny 隔离图；visible integrity pass，blocking findings 为空 | `Validation/playmode-visual-qa.json`；`nativeDynamicParityAccepted=false`、`allFramesFullyInspectable=false` |
| Worker 结束 | 工作 Worker 均已停止；后续空场景与 Onette 120 帧专项退出未再现 JobTempAlloc 警告 | `Validation/worker-shutdown-diagnostics.json`；长驻 Worker 历史仍有 2 条 JobTempAlloc 剩余警告，来源未定位 |

02/03/06 的最后一张图在切到原生 summon 动作后部分或全部离开固定展示相机；可见完整性依据其首/中间图检查，不能声称每张图都可完整检查。QA 不承担原生同帧相机或每一时刻的视觉等价验收。

退出遥测还记录 Unity UTP `type=MemoryLeaks`、`phase=Immediate` 的内存标签汇总 `32,652,686 B`，与 JobTempAlloc 不是同一项。后续未复现不能消除历史记录，也不能写成“所有历史日志零 warning”。Extra Emotion/Vehicle、HeadGaze/HandLock 等展示 Montage 之外的玩法辅助保留源证据，未全部重建为游戏逻辑。

## Evidence → Finding → Path

以下证据均以 `ROOT` 定位。每条 Finding 只覆盖其证据层，不把采样或单模块结果提升为全动画/整图完全一致。

| Evidence | 主要 source_ref | Finding | 实现或维护 Path |
| --- | --- | --- | --- |
| `E-DSA-01` | `Validation/final-resource-audit.json`、各角色 source-animation-manifest | `F-DSA-01`：源包闭包与角色归属可追溯，255 clip/255 动画 FBX 已交付 | `P-DSA-01`：index/registry/references → raw/properties/TRS → 角色目录 → coverage |
| `E-DSA-02` | `Validation/character-studio-postprocess-schema-audit.json` | `F-DSA-02`：原生字段布局恢复了截断属性，默认值仍需与运行时 CB 区分 | `P-DSA-02`：PE reflection → mappings → properties → source contract |
| `E-DSA-03` | `Validation/source-to-gltf-combined-weight-audit.json`、`vertex-color-unity-integrity.json` | `F-DSA-03`：绑定/权重/顶点字节需要跨 DCC 验证 | `P-DSA-03`：源原始通道 → 固定索引转换 → 安装计划 → 运行时派生 Mesh |
| `E-DSA-04` | `Validation/face-curves-all-existing-validation.json`、`display-timeline-validation.json` | `F-DSA-04`：原生 RichCurve 与 Montage/Notify 时序可验证 | `P-DSA-04`：源曲线/默认值 → 本地 clip/武器 skeleton → Timeline 播放 |
| `E-DSA-05` | `Validation/physics-animated-dynamics-pose-validation.json`、`physics-world-native-coordinate-audit.json` | `F-DSA-05`：局部/World/镜像纯 q 的错误已由 fixture 发现和修正 | `P-DSA-05`：原始 TRS/原生代码 → 静态/动画 fixture → runtime → 隔离观察 |
| `E-DSA-06` | `Validation/rendering-module-evidence-summary.json`、`temporal-production-evidence.json` | `F-DSA-06`：主要渲染链恢复且精度范围量化 | `P-DSA-06`：RDC 实际链 → HLSL/Feature → 同输入 GPU → 固定姿态诊断 |
| `E-DSA-07` | `Validation/final-project-health.json`、`playmode-visual-qa.json` | `F-DSA-07`：实际宿主可加载播放，可见样本通过，完整原生轨迹仍未验证 | `P-DSA-07`：匹配 Worker → 六场景/53动作 → 图像 QA → 最终清单 |

下表为本页引用的重要报告字节快照；重跑报告会改变 SHA，不能继续沿用旧值。大型资源审计对 64 个已改 clip 重算 SHA，对 446 个文件只在 size/mtime 一致后沿用旧 SHA，并未重新哈希全部大文件。

| ROOT 相对文件 | SHA-256 |
| --- | --- |
| `Validation/final-delivery-manifest.json` | `70b1c5fb76a3655554ae26bcdf95279a907ed40f509a10ab8a5533eacf126951` |
| `Validation/final-resource-audit.json` | `6ca470fdd3b3e04960d81d801ec7c7ce69db350f296fb915eface00678c45359` |
| `Validation/playmode-visual-qa.json` | `74f380c5c55753ca1f2b0dec673169116b0aaaad025a5ebf7a6f93da4cfd354d` |
| `Validation/final-project-health.json` | `2032a6ad926f029e3a898bc4d1178c63e7cda3dbcecebb5029ba3097bdef2241` |
| `Validation/full-project-csharp-compatibility.json` | `56155a78ac840c2c0d819a6a0c5d76b0a93a3cad59a743318ecdbf2c828ee8c1` |
| `Validation/asset-guid-dependency-audit.json` | `a88846872e4fb3697f1c29b6a907af4b64319663f123819be6f93afdeeec41d1` |
| `Validation/physics-animated-dynamics-pose-validation.json` | `c75a44fb4c0d65303b3abd092132681d85dc93e4496b7fd2ac2a24d9d82345f0` |
| `Validation/face-curves-all-existing-validation.json` | `5e31584c4324456c7a4f211068f7bcf0965745d1073cb9f9bed50aebd7da3e70` |
| `Evidence~/RenderDoc/Character01/status.json` | `2b21a4a3306860b4bd5f0bbc064b8a017c18259955823b839958d0e4182a6db8` |

## 复现入口、维护与回退

先只读检查当前清单与身份；下列命令不会启动 Unity、触碰密钥或写项目：

```powershell
$dsaRoot = 'F:\UnityProject\HYYYYY\Assets\DragonSwordReconstruction'
$dsaDelivery = Get-Content -LiteralPath (Join-Path $dsaRoot 'Validation/final-delivery-manifest.json') -Raw | ConvertFrom-Json
$dsaDelivery.resourceTotals
$dsaDelivery.runtimeHealth
Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $dsaRoot 'Validation/final-delivery-manifest.json')
Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $dsaRoot 'Validation/final-resource-audit.json')
```

如需重新生成证据，先保存原报告和本地补丁身份。本页仅保留命令协议说明，不提供带占位源对象的可执行解包段。真实参数解析入口为 `Tools~/Unreal/CUE4ParseSource/CUE4Parse-master/CUE4Parse.Example/Program.cs`；任务输入分别由 `Evidence~/Unreal/eightweights-model-jobs.json`、`CaptureData/exactbind-model-install-plan.json` 和各角色 `Animations/source-animation-manifest.json` 定位，转换脚本见 DSA-04。

已只读核实 `Program.cs` 的映射解析为 `Path.Combine(Root,"Tools~","Unreal",mappingArgument)`。默认 `UE53-reference.usmap` 实际位于 `F:\UnityProject\HYYYYY\Assets\DragonSwordReconstruction\Tools~\Unreal\UE53-reference.usmap`，不是从命令行当前目录寻找。`Root`、源 Paks 和输出目录同样由源码中的绝对根解析。执行前仍需核对当前源码、本地补丁和环境开关；普通调用会写 `Source~/Unreal/file-index.txt` 与 `container-status.json`，其他模式还会写对应属性/依赖报告，因此它们不是只读查看命令。

GUID 审计脚本实际位于 `Tools~/Unreal/audit_unity_asset_references.py`。它只读取资产，但会覆盖 `Validation/asset-guid-dependency-audit.json`，因此不能将执行描述为整个文件系统零写入。新一轮审计需先保留旧证据快照；其他验证入口按对应功能 README 与 Worker 参考执行，不盲目重导正式 FBX。

维护优先级为源配置/原始通道 → 导出物身份和索引 → 动画/物理 fixture → 同输入 GPU → 正式场景/PlayMode → 可见 QA。修改映射、绑定、颜色、primitive 或 Shader 生成器后，重跑受影响层以及下游层；只修改文档则验证链接、编码和元数据，不启动资源转换。

回退以保留的源证据、安装计划、原正式资产及 `.meta`/GUID 为单位，不以临时预览 FBX 覆盖已有资产。变更全局 Graphics/Quality/Packages 前保存原配置；本 Profile 不授权修改其他还原目录。新抓帧、不同游戏版本或新增动作需要建立新的输入身份和报告前缀，不能直接延用本页的数值结论。
