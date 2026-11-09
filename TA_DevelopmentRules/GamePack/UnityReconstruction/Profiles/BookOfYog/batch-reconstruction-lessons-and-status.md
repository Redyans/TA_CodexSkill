# 《依盖之书》批量角色制作经验与状态

> 类型：PROFILE；整理日期：2026-11-06。依据既有制作记录、当前源码/材质、抓帧导出和验证产物整理；本次为文档与证据核对，没有重新运行 Unity 编译、渲染或播放。时间字段保留证据原有 UTC，不把文件修改时间当作测试完成时间。

## 1. 范围与阅读入口

目标工程为 `F:\UnityProject\HYYYYY`，Unity `2022.3.62f3`、URP `14.0.12`、D3D11、Linear。复刻根为 `Assets/YogCharacterReconstruction`；以下工程路径除明确说明外均相对复刻根。输入来自 `Cap/依盖之书`，`1` 号玲珑作为公共展示实现基线，其他角色复用已经取证的环境、Shader、光照与后处理。

用户当前要求是按顺序一个角色一个角色检查，光照正确、整体观感接近即可，不要求逐像素对齐；损坏的 `19`–`22` 跳过。范围包含背景、地面和人物，不重建原游戏 UI，角色动作保留为后续可替换资产。

| 资料 | 内容与用途 |
| --- | --- |
| [批量制作参考](../../references/batch-character-reconstruction-playbook.md) | 可迁移到其他项目的流程、技巧、问题定位和验收方式 |
| [玲珑及早期批次 Profile](linglong-showcase-reconstruction.md) | 原始模型/动画、Shader、光照、环境、URP、AO/TAA/后处理及早期数值证据 |
| [Unity 资源与渲染工作流](../../references/unity-asset-and-render-reconstruction-workflow.md) | 从客户端依赖闭包到 Unity 可编辑场景 |
| [故障排查参考](../../references/renderdoc-validation-and-troubleshooting.md) | 具体症状与分层排查顺序 |

本页补充后续经验和有效边界；早期文档中“最新”是当时阶段，不覆盖本文列出的后续改动。

## 2. 已建立的制作基础与可复用范围

| 内容 | 工程实际实现/资料 | 如何复用 |
| --- | --- | --- |
| 资源解包 | UnityPy 索引、原生 AssetBundle 读取、`Evidence/Assets` 与 `Evidence/BatchCaptures` 清单 | 按新角色源 prefab 和依赖追加；不按旧事件号套资产 |
| 原始动画 | 压缩运行时曲线解码、Avatar/TOS 路径还原、身体/武器分离 | `Characters/<Character>/Animations` 存角色可替换动作；玲珑仍有两条历史动作在 `SourceAssets/Animations`，见基线 Profile |
| Shader | DXBC/常量/资源取证、`Shaders/BatchVariants` 与材质绑定 | 相同程序复用实现，差异集中到每 Case 的材质和必要变体 |
| 背景与地面 | `Environments/CharacterShowcase`，环境、水面、反射和阴影实现 | 执行程序相同可复用，逐角色参数/动态输入仍需检查 |
| 后处理 | AO、TAA、Bloom、LogC LUT、暗角与 dither | 原输入单 Pass 证据留在基线；新场景 history、分辨率和输入另验 |
| 本地 URP | `Packages~/com.unity.render-pipelines.universal`；工程 `Packages/com.unity.render-pipelines.universal` 是指向它的 Junction | 本次核对目标与包版本存在；修改前读取 `Evidence/LocalURP.md`，不把路径存在写成主编辑器已刷新 |
| 自动化 | `Tools~/UnityWorker`、`StartUnityWorker.ps1`、`InvokeWorker.ps1` | 使用隐藏 Worker 和真实 D3D11；同一请求通道串行 |
| 批量场景 | `Scenes/Batch/<编号>_frame<帧号>.unity` | 一个 RDC 对应一个 Case；`32` 目录有 `frame126142` 与 `frame9114` 两个独立 Case |
| 预览与证据 | `Validation/Batch`、`Validation/Batch/References`、`Evidence/BatchCaptures/<编号>` | 正式预览、参考、实验图和历史报告分别识别 |

`batch-environment-post-reuse.json` 的既有审计显示 32 份可回放抓帧执行相同的 8 组主环境 VS/PS 及 TAA/LUT/Uber 程序；`batch-lighting-audit.json` 记录相同主光方向、点光位置与强度。这支持复用公共实现，不能证明每帧纹理、sampler、几何、历史或最终输出相同。

## 3. 真实问题、处理方式和可借鉴结论

### 3.1 已有修复中最值得复用的经验

以下是基线 Profile 中已有证据的提炼，完整事件、指标和实现入口仍由原条目维护。

| 现象 | 已定位原因/处理 | 可借鉴结论 |
| --- | --- | --- |
| 导出 `.anim` 后 1 秒、零曲线 | 原 clip 内容在 runtime StreamedClip；恢复 key、切线及路径后持久化 | 编辑器曲线为空不代表原动作为空，导出完成需要真实采样验证 |
| 描边缺失 | 原 `ModifyMeshRuntime` 写入额外顶点通道 | 对比源 Mesh 与实际 IA，资源包并非全部运行输入 |
| 全批次相机改完黑屏 | 数组形状的 JSON 不能正确反序列化为当前 `Vector3` 字段；改为 `{x,y,z}` | 接口读回验证比“JSON 能解析”更有意义 |
| 7 号链条顶点色 NaN | 零步长 `R8G8B8A8_UNORM` 流被按 float 解读 | 同时尊重输入格式、buffer、stride 与 offset，不能仅看字节宽度 |
| 26 号帽饰异常 | 源 Mesh 的范围异常；按指定 draw 的 GPU 输入重建并挂接 | 原资产与 GPU 证据交叉定位，替代网格仍需说明静态/动态边界 |
| 10 号法杖位置朝向错误 | 由手骨逆世界矩阵与抓帧对象矩阵求附件局部变换 | 先查挂点变换，连续扫身体相位不能代替矩阵核对 |
| 32/frame9114 剑刃偏暗 | 导入器统一 Clamp 与原多槽 Wrap sampler 不符 | 先查采样；强制全亮阴影未解决主差异，不应继续调公共光照 |
| 同相位扫描前后分数不同 | 上一步姿态/历史影响连续采样，独立重开否定更优相位 | 最终候选独立重开复拍，保留被否定的实验作为限制证据 |
| 本地 URP 加载 CS0246 | 本工程 Assets 内 `file:` 入口被 Unity 拒绝；采用标准 Packages 入口与本地源码 | 包物理位置、UPM 来源和实际加载是三个检查点 |
| 19 号 RDC 修头后仍不能回放 | `OpenFile` 成功但 `OpenCapture` 读取流末尾失败 | 容器打开不等于完整回放；保留原件并按用户要求跳过 |

### 3.2 25 号：先纠正槽位判断，再记录近似边界

对象为 `SasayaTNL`，场景 `Scenes/Batch/25_frame134425.unity`。原问题为画面左腿浅色横纹，与深色网袜参考不同。实际绑定已由 `Tools~/Batch/Case25Diagnostic/leg-material-bindings.tsv` 确认：

| Renderer / 槽位 | 材质与 Shader | 原纹理资产 |
| --- | --- | --- |
| `Sasaya_05_Leg` 槽 0 | `Event1502.mat` / `Variant_421623954dd6` | `2219400_BC1_SRGB.asset`，`T_SasayaTNL01` |
| `Sasaya_05_Leg` 槽 1 | `Event1362.mat` / `Body` | `2219385_BC1_SRGB.asset`，`T_SasayaTNL02` |
| `Sasaya_05_SW` | E2463/E2466 / `Variant_b80da329f26c` | 两材质保留 `_YogSockDomainOutputFix=1`，不是本轮原算法完整性证明 |

材质和纹理均位于 `Characters/SasayaTNL/Cases/25_frame134425`。不能从 E1362/E1502 的先后猜测 Renderer 槽位。历史诊断中，增加袜层 Alpha、强制深度通过没有补齐左腿；Repeat 采样曾减少横纹，但没有恢复正确网袜；换图或 UV 平移又影响另一腿。上述实验不支持“已定位并恢复原网袜算法”。

当前保留 `Variant_421623954dd6.shader/.hlsl` 的 `_YogFishnetLegFix`，默认 0，仅 E1502 材质开启。`YogBatchSceneValidator.ApplyCase25FishnetLeg` 是设置入口。实现从 `v3.w/v4.w/v5.w` 取得位置，减去 `(1000,1000,1000)`，以 `dot(pos.xz,(0.8,0.6)) ± pos.y`、频率 58 的正弦构造斜交网纹，最后乘暗已完成照明的 RGB。

这属于固定展示的程序网纹近似：未恢复原贴图采样或原算法，纹理没有绑定到蒙皮表面，角色移动时存在滑动风险；仍有横带、网纹形状和左右腿亮度差异。世界原点、频率、颜色乘数都是本 Case 参数，不是通用技巧默认值。回退时将当前材质 `_YogFishnetLegFix` 设为 0，并保留诊断图供原算法修复后比较。

本次核对发现正式 PNG 的文件时间早于当前 HLSL 文件时间，二者没有源码哈希绑定；因此只记录已有预览，不能证明最后保存的源码已独立复拍。

### 3.3 28 号：几何存在，但透明问题仍未闭环

对象为 `Kash`，场景 `Scenes/Batch/28_frame130682.unity`。`SM_Body04` 与 `SM_Body04SW` 使用同形腿部网格及 `Variant_8532699b73c8`。历史隔离/纯色标记显示腿部几何存在，但原着色下画面右侧腿很透明或几乎不可见。此前将它解释为“深色重叠”不足以关闭问题，本页按未解决登记。

证据在 `Tools~/Batch/Case28Diagnostic` 的 `marked-*`/`without-*` 和 `Tools~/Batch/Case28Phase/phase_00..10.png`。0–5 秒、每 0.5 秒独立打开场景的历史扫描没有消除问题。相位不是已证实的根因。

本次只读核对 `Evidence/BatchCaptures/28/依盖之书_2026.10.31_20.26_frame130682-tessellation.json`：E2691 后 Domain 顶点 stride 为 128，保存的 16 个样本在签名对应 `TEXCOORD0.zw` 处均为 `(1,1)`。目标通用 `YogDomain` 仍插值输入后调用 `YogVertex`；PS 将 `v1.z` 乘入 alpha，并以相关值执行 `discard`。这是可复现的数据流疑点，尚未用修正后的目标 GPU 输出证明它就是全部根因。

下一次修复应对比同一组源 DS 输出与目标插值，分别观察 `tex0.zw`、alpha、clip 及叠层状态，再最小修正并独立复拍。不要把纯色能看见腿直接解释为“原材质已修复”，也不要将诊断常数写入所有细分角色。

### 3.4 30 号：局部调暗改善观感，不等于恢复叠层

对象为 `RuoYGF`，场景 `Scenes/Batch/30_frame129132.unity`。`SM_Body01` 对应 E1645 / `Variant_4a5f25b2adbd`；`SM_Body01SW` 对应 E2632 / `Variant_864bc0361def`。历史隔离图保存在 `Tools~/Batch/Case30Diagnostic`。关闭基础层与关闭袜层的结果不同，说明需要继续核对两层输入与组合；这些图来自同一诊断方法中的顺序操作，最终根因判断还需独立重开确认。

当前仅 E1645 材质启用 `_YogCase30DarkTights=1`，Shader 默认 0；实际逻辑是 `if (_YogCase30DarkTights > 0.5) o0.xyz *= 0.16;`。它使腿部变深，但不恢复原 UV、采样或叠层算法，也会缩放最终高光/照明。服装金色条纹密度、剑刃明度仍有差异。回退为关闭该材质开关，再用原始输入定位问题。

已有 `RunCase30` 重拍记录及正式 PNG，但没有覆盖该次 Shader 修改的最新全量编译报告。本次仅核对实现与产物，不增加新的视觉或播放通过结论。

## 4. 后续批次状态与验收边界

| Case | 既有处理/检查 | 本次整理后的结论 |
| --- | --- | --- |
| 2–18 | 早期已有逐角色处理及阶段图，详见基线 Profile | 本次没有重新逐组验收；静态附件、特殊透明/粒子等限制保留 |
| 19–22 | 原 RDC 不可回放 | 按用户要求跳过；不是已还原角色 |
| 23_frame65326 | 上一阶段记录皮肤采样调整，已有正式预览 | 本次未重新验证其算法或画面 |
| 24_frame126050 | 上一阶段记录蓝色火焰近似，已有正式预览 | 保留近似属性；没有完整粒子生命周期证明 |
| 25_frame134425 | 程序网纹和调暗后已有预览 | 固定展示近似，未闭合原网袜与最终源码复拍 |
| 26_frame133463 | 历史复拍后主观受光接近；帽饰/气球已有专项处理 | 气球 Domain、飘带和画笔细节仍有限制 |
| 27_frame131865 | 历史查看预览，金属臂、火翼和刀的受光接近 | 静态附件的动态跟随未证明 |
| 28_frame130682 | 标记与相位诊断，腿几何存在 | 腿部透明问题未解决，不能登记完整通过 |
| 29_frame130604 | 历史查看预览，受光接近 | 本次没有重拍/播放 |
| 30_frame129132 | 基础层乘暗后已有复拍 | 近似改善；条纹、剑及叠层原算法仍未完成 |
| 31_frame128043 | 历史查看预览 | 仅记录当时主观光照判断，没有本次新验证 |
| 32_frame126142 | 历史查看预览 | 与同目录另一帧分开管理，未补动态验证 |
| 32_frame9114 | 剑 sampler 已有证据修正，历史查看预览 | 朝向/裙摆及部分静态附件边界保留 |
| 33_frame9805 | 历史查看预览 | 皮肤/白袜区域、武器姿态仍有差异 |
| 34_frame10152、35_frame12994 | 历史查看预览 | 仅记录当时光照观感判断，不等于新编译或动态验收 |

`*-preview.json` 当前仍使用 `nonblank_preview_rendered_effect_alignment_pending`。它表达成功生成非空图像，不表达视觉验收。后续宜分别记录渲染完成、光照判断、近似项、动态验证和未解决问题；这里是维护建议，尚未修改该报告格式。

现存 `Validation/shader-compilation.json` 的 `utc` 为 `2026-11-03T11:30:43.1171733Z`，548 个 Pass/变体、0 错误。25/30 的相关 HLSL 在此后保存，因此这份报告不能证明后续版本全部编译通过。`BatchSceneComparison32.json` 是更早的 32 组阶段报告，整图均值 0.02832748、范围 0.01811083–0.05257281；它同样不能作为后续近似补丁的当前指标。

`YogBatchSceneValidator.cs` 中仍有 `ScanCase28Phase`、`DiagnoseCase28LegLayers`、`DiagnoseCase30BodyLayers` 临时诊断方法。本次没有清理源码。后续如清理，按方法边界删除，保留 `RunCase26/27/30/31` 等正式入口，不用硬编码行号范围切除邻接方法。

## 5. 工具与复现入口

基线历史工具版本为 UnityPy 1.25.3、RenderDoc 1.46、3Dmigoto cmd_Decompiler 1.3.16、Il2CppDumper 6.7.46。版本是历史取证记录，本次没有重新安装或逐工具查询版本。当前核对确认 `C:\Program Files\RenderDoc\qrenderdoc.exe` 存在；现有 RenderDoc 脚本用该宿主的 `--python` 执行，不要求系统 Python 能直接导入 `renderdoc`。

| 任务 | 当前入口 | 输出/注意点 |
| --- | --- | --- |
| 批次和依赖规划 | `Tools~/Batch/inventory_captures.py`、`build_case_plan.py`、`extract_source_bundles.py` | 每次确认输入/输出和选择过滤，避免重建覆盖既有成果 |
| 动作与原生导入 | `decode_display_animations.py`、`prepare_native_import.py`；`Editor/YogBatchCharacterImporter.cs` | 角色动作、源对象与导入清单 |
| 共享关系核对 | `audit_environment_post_reuse.py`、`audit_showcase_lighting.py`、`audit_showcase_cameras.py` | 程序、光照及主相机审计 |
| Shader/材质生成 | `generate_forward_variants.py`、`apply_verified_forward_variants.py`；`Editor/YogBatchCapturedMaterials.cs` | 手写变体补丁重建前需检查是否已回写生成链 |
| 缺附件/细分调查 | `audit_unbound_showcase_draws.py`、`export_accessory_draws.py`、`export_tessellation_evidence.py` | 候选、实际 IA/Shader/资源、HS/DS 证据 |
| UI 前参考 | `export_no_ui_references.py`、`convert_no_ui_references.py` | 原始线性输出与显示 PNG 分开保存 |
| 批次比较 | `compare_batch_scenes.py --prefix BatchSceneComparison<新数字>` | 前缀语法只接受此名称加数字；输出前检查文件不存在，脚本本身会覆盖同名报告 |
| 隐藏 Unity 验证 | `Tools~/InvokeWorker.ps1`、`Editor/YogBatchSceneValidator.cs`、`YogShaderValidation.cs` | 请求唯一 ID、最终 JSON、目标场景/图像；不是鼠标自动化 |

上表无目录前缀的 Python 脚本均位于 `Tools~/Batch`。完整功能说明保持在工程源码/README 附近，本规则库只留经验与调用索引。

以下是当前已存在的验证方法，**本次没有执行**。前提是正确版本的专用 Worker 已运行，且 `Tools~/Control/request.json` 没有未完成任务；修改代码后先刷新，再串行调用。独立场景重拍并不自动包含 PlayMode 验证。

```powershell
Set-Location -LiteralPath 'F:\UnityProject\HYYYYY'
& 'Assets/YogCharacterReconstruction/Tools~/InvokeWorker.ps1' -Method 'refresh'
& 'Assets/YogCharacterReconstruction/Tools~/InvokeWorker.ps1' -Method 'YogReconstruction.Editor.YogShaderValidation.Validate'
& 'Assets/YogCharacterReconstruction/Tools~/InvokeWorker.ps1' -Method 'YogReconstruction.Editor.YogBatchSceneValidator.RunCase25'
& 'Assets/YogCharacterReconstruction/Tools~/InvokeWorker.ps1' -Method 'YogReconstruction.Editor.YogBatchSceneValidator.RunCase30'
```

`InvokeWorker.ps1` 自身等待响应并拒绝重用请求 ID；已有请求时不要删除锁文件抢占。超时先核对对应进程、日志和任务状态。`StartUnityWorker.ps1` 使用 `-batchmode -force-d3d11` 和隐藏窗口；它不是幂等启动器，调用前先确认没有仍在运行的专用 Worker。

`export_tessellation_evidence.py` 支持 `YOG_BATCH_CASES` 过滤，默认跳过已有导出，`YOG_BATCH_FORCE=1` 才强制重导出。需要新取证时隐藏启动 `qrenderdoc.exe --python`，保留当前环境变量原值并在完成后恢复；脚本的进程退出与结果文件成功状态分别检查，不在文档整理时重放全部 RDC。

## 6. Evidence → Finding → Path

本次实际做了静态读取、JSON 解析、已有 DS 样本解码、文件存在性与 SHA-256 核对；没有从文档摘要推造新的编译/渲染报告。

| Evidence | source_ref 与本次观察 | Finding / 状态 |
| --- | --- | --- |
| E-BOY-BATCH-01 | `Tools~/Batch/Case25Diagnostic/leg-material-bindings.tsv`，并核对对应材质与 HLSL | F-BOY-BATCH-01 / validated：E1502 实际为槽 0，当前程序网纹是局部近似；不表示原网袜已恢复 |
| E-BOY-BATCH-02 | `Evidence/BatchCaptures/28/*-tessellation.json` 的 E2691、`Variant_8532699b73c8.hlsl` 与既有诊断图 | F-BOY-BATCH-02 / candidate：DS 插值到 alpha 的数据流值得优先验证；尚未完成修复对照 |
| E-BOY-BATCH-03 | `Variant_4a5f25b2adbd.hlsl`、`Characters/RuoYGF/Cases/30_frame129132/Materials/Captured/Event1645.mat` | F-BOY-BATCH-03 / validated：当前近似缩放最终 RGB；原叠层根因仍未知 |
| E-BOY-BATCH-04 | `Validation/shader-compilation.json` 原 UTC 与源码文件时间；`*-preview.json` 状态字段 | F-BOY-BATCH-04 / validated：现有编译报告早于相关修改，非空预览不表达视觉通过 |
| E-BOY-BATCH-05 | `batch-environment-post-reuse.json`、`batch-lighting-audit.json` 的既有审计 | F-BOY-BATCH-05 / validated（限原报告范围）：公共程序/已取常量支持复用，角色输出仍需独立检查 |

表中未带目录的变体 HLSL 位于 `Shaders/BatchVariants`，两个公共审计文件位于 `Evidence/BatchCaptures`。`validated` 仅修饰该行明确的观察结论，不修饰整角色还原完成度。

**P-BOY-BATCH-01（排错路径）**：场景问题 → 实际槽位清单（E-BOY-BATCH-01/F-BOY-BATCH-01）→ 原 GPU 阶段与 sampler（E-BOY-BATCH-02/F-BOY-BATCH-02）→ 限定材质修正并记录近似（E-BOY-BATCH-01、E-BOY-BATCH-03）→ 独立重开渲染及目标宿主编译（E-BOY-BATCH-04 说明当前缺口）→ 动态验证。此路径末两步对最新补丁仍有未完成项。

**P-BOY-BATCH-02（推广路径）**：共享实现与常量审计（E-BOY-BATCH-05）→ 每 Case 的 Mesh/动画/材质绑定 → 单角色光照与完整性检查 → 记录近似和遗留问题 → 下一角色。检查顺序不意味着此前角色的未知动态行为已经解决。

以下是本次读取的关键文件快照，SHA-256 用于识别来源版本，不是 GPU 通过证明；重写产物后需重新关联证据。

| 工程内文件 | SHA-256 |
| --- | --- |
| `Tools~/Batch/Case25Diagnostic/leg-material-bindings.tsv` | `0994d2eeb555d2cd53480d464e7bb7e6171cca2e5b04675d8eab32e18286f237` |
| `Shaders/BatchVariants/Variant_421623954dd6.hlsl` | `f6cf84d1366e778aa33b8161847273f6b9ee742a7c584260d4b82be186b77c22` |
| `Shaders/BatchVariants/Variant_4a5f25b2adbd.hlsl` | `c0a01e21099b036d87fe2971fc423fb0a07f350149f5b0bb9cbf18391426acfa` |
| `Validation/Batch/25_frame134425.png` | `40e7e7012af8371fe616fcda0c0622701bb916083b56a35ded3dd22667c8d7b6` |
| `Validation/Batch/28_frame130682.png` | `fc9919d92262cd1170e10bcfecdbb9f933b8a8af08a4ad02f0176cfa3830b945` |
| `Validation/Batch/30_frame129132.png` | `05070be6ee754842b20464cfb2b7bd21a5c1611332bf4d608ca157d968d60b23` |
| `Validation/shader-compilation.json` | `acc6f5f280b7559f4cec9a109d3a2a6228448772a0e636edd39e8c5c25c5cf7c` |
| `Validation/Batch/BatchSceneComparison32.json` | `ad4bf1a1dc654858ef8fedabb7d7c572d4e33451679c189eda230ad4c935f505` |

可用以下只读命令复核 E-BOY-BATCH-02 的已保存样本；它解析已有证据，不重新回放 RDC。偏移 16 来自该导出的阶段签名：首个 float4 为位置，随后为 `TEXCOORD0`。当前结果为 16 个样本、`[(1.0, 1.0)]`，不能外推到未导出的全部顶点或其他事件。

```powershell
Set-Location -LiteralPath 'F:\UnityProject\HYYYYY'
@'
from pathlib import Path
import json
import struct

folder = Path("Assets/YogCharacterReconstruction/Evidence/BatchCaptures/28")
files = list(folder.glob("*-tessellation.json"))
assert len(files) == 1, "Capture identity is ambiguous"
report = json.loads(files[0].read_text(encoding="utf-8-sig"))
draw = next(item for item in report["draws"] if item["eventId"] == 2691)
data = bytes.fromhex(draw["postDomain"]["sampleHex"])
stride = draw["postDomain"]["stride"]
assert stride == 128 and len(data) % stride == 0
tex0 = [struct.unpack_from("<4f", data, offset + 16)
        for offset in range(0, len(data), stride)]
print(len(tex0), sorted({value[2:] for value in tex0}))
'@ | python -
```

## 7. 后续维护与回退

继续还原时优先完成 28 号可见透明问题；25/30 号保留近似标记，只有原始数据流修复并独立验证后才替换。对修改过的源码先跑目标编译与当前角色复拍，动态诉求再检查动作和附件跟随；无需为了记录经验重跑无关角色。

重建场景可能覆盖逐场景屏幕阴影绑定，重建 Shader 可能覆盖手写修正。先核对 `build_screen_shadow_plan.py`、`InstallBatchCapturedScreenShadows` 与生成器的实际行为，保存本任务资产/配置及 GUID；避免把编辑模式捕获阴影当成 Play 中已恢复的实时阴影。

本次新增内容属于 REFERENCE 与 PROFILE，未把单角色近似或未经闭环的 Domain 假设升级为 CORE。文档更新不修改正式场景、Shader、材质和工具，也不把未完成清理或重测写成已完成。

本次文档检查覆盖 7 份新增/修改的 Markdown：159 个本地链接及其锚点解析通过，8 项证据 SHA-256 相符，UTF-8、代码围栏、尾随空白与两个新增 `.meta` 的 GUID 唯一性检查通过。上文只读 DS 样本命令实际运行得到 `16 [(1.0, 1.0)]`；Worker 方法只核对源码入口存在，未执行。上述检查证明文档与所引用文件相符，不增加 Unity 编译、画面或动态验证结论。
