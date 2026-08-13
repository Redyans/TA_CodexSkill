---
name: projectacg-painter-material-preview-bridge
description: ProjectACG Unity-SP 美术简易流程、材质参数、双向贴图、单向灯光与双向相机同步的项目 Profile，记录当前版本、路径、协议、映射、通道、颜色和验证边界。
---

# ProjectACG Painter 材质预览桥接 Profile

## 1. 适用范围与事实来源

本 Profile 只适用于当前 ProjectACG 的 `Chara_Cloth_V2` Unity/Substance Painter 对齐工具。跨项目实现与排查方法见 [Unity 与 DCC 本地材质预览链路参考](../../references/local-dcc-preview-link.md)；默认美术简易模式、单材质/批量/场景/高级工作区和批量执行方法可结合 [Editor 批量烘焙工作区与安全取消参考](../../references/batch-baker-workspace-and-cancellation.md) 的 UI 与计划分层；Shader、MRA、Ramp、Specular AA、Cubemap Atlas 和 Roughness/LOD 契约见 [ProjectACG Unity 与 Substance Painter Shader 对齐 Profile](../../../ShaderDevelopment/Profiles/ProjectACG/README_Tech_ProjectACGSubstancePainterShaderProfile.md)。

功能使用细节以源码旁 `Assets/Editor/TA_Tools/Render/PainterMaterialParameterBridge/README_PainterMaterialParameterBridge.md` 为准。本文件只保留当前项目的稳定接口、已解决问题、验证状态和维护边界，不能代替功能 README。

| 项目 | 当前值 |
| --- | --- |
| Unity 工具目录 | `Assets/Editor/TA_Tools/Render/PainterMaterialParameterBridge/` |
| Unity 菜单 | `TA_Tools/Render/Unity-SP 材质参数同步` |
| Unity 目标 Shader | `Valkyria/Chara/Chara_Cloth_V2` |
| Painter Shader | `Chara_Cloth_V2_SP` |
| Painter Shader 源码 | `Assets/Shader/Character/Chara_V2/SP/Chara_Cloth_V2_SP.glsl` |
| Painter 开发期版本 | `11.0.2` |
| Painter 插件源码版本 | `2.10.1`；运行时以连接握手为准 |
| Cloth Shader Adapter | `chara-cloth-v2@2` |
| 材质/贴图 Preview Link | `ProjectACG.PainterPreviewLink` v1，`ws://127.0.0.1:6418` |
| 双向相机 Link | Python Companion，`ws://127.0.0.1:6419` |
| 参数协议 | `ProjectACG.PainterMaterialProfile` v1，`colorSpace=linear` |
| 缓存根目录 | `Library/PainterPreviewCache/` |
| 美术映射缓存 | `Library/PainterPreviewCache/ArtistMappings.json`，`ProjectACG.PainterArtistMappings@1` |
| Painter 工程身份 | `inventory-fingerprint`；不包含 `.spp` 绝对路径 |

## 2. 工具边界与选择方式

### PRJ-LINK-01｜从模型解析材质，并用显式绑定支持批量同步

Unity 窗口默认打开“美术简易模式”。可指定场景模型实例，递归收集 Renderer 上去重后的兼容材质；也可以只指定一个当前 Material。日常流程为：

1. 选择模型或当前材质；
2. 点击 `一键准备 / 修复材质同步`；
3. 用 `当前材质 Unity → SP`、`当前材质 SP → Unity` 逐个检查，或用两个“全部材质”按钮批量预览；
4. 用 `Unity → SP 灯光预览` 把当前材质对应的有效主光方向、Linear 颜色和强度发到独立 Painter Shader Instance；
5. 点击 `确认参数并结束` 只保留共享参数，或点击 `放弃修改并恢复` 完整恢复。

准备阶段只接受 Texture Set 完整同名或唯一 `mat_` ↔ `tex_` 前缀别名，不做包含或相似度模糊匹配。请求会自动连接 Painter，不要求美术先操作“连接 Painter”或维护 Painter 当前下拉选择。

每个批量绑定必须同时保存：

```text
Unity Material GUID
Painter Texture Set 精确名称
Painter Shader Instance 数字 ID
Shader Adapter ID + Version
```

名称只用于生成上述安全候选，执行时不做模糊匹配。完整双向预览要求每个 Texture Set 使用独立 Shader Instance；否则 Shader Settings 与 Unity 外部预览贴图会相互覆盖。Painter 默认多个 Texture Set 共用同一个 ID 时，准备流程先显示一次工程修改确认，再调用 `painter.targets.ensureBatch` 创建或修复独立 Shader Instance、重新拉取 inventory 并预检。用户取消或运行时不支持修复时保持阻断，不按“最后同步者覆盖前者”的顺序继续执行。

批量预检会拒绝重复 Unity Material、重复 Texture Set、修复后仍共用的 Shader Instance、未保存 Material、重复输出目录和 Adapter 不兼容。简易模式的当前/全部双向请求都通过 `AttachBindingTarget(...)` 显式携带 `bindingId`、Texture Set、Shader Instance ID 和 Adapter ID/Version，不依赖 Painter 当前高亮项。

参数 JSON 同步只需要 Material。`SP → Unity 贴图+参数预览` 必须指定场景模型实例，因为工具需要把临时材质精确替换到对应 Renderer 材质槽并在退出时恢复。Prefab/FBX 资产本身不作为临时预览对象。

当前窗口是可停靠的 `OdinEditorWindow`。顶部一级工作区固定为“美术简易模式 / 单材质预览 / 批量同步 / 场景同步 / 高级 / JSON”，默认选中美术简易模式。模型、材质选择和材质/Painter/Unity 临时预览状态保持公共显示。简易模式只显示 Painter 状态、材质准备、`流畅/标准/最终检查` 质量预设、当前/全部/结束主按钮和 Unity 主灯预览；单材质工作区只放贴图预览、实时性能和退出保留；批量工作区只放映射和批量操作；场景工作区只放相机、主灯和 Atlas；连接测试、JSON、参数差异、详细状态和插件安装进入高级工作区。模式切换只控制显示，不清空旧序列化字段。

`SendRequestAsync` 会按需建立材质/贴图连接，因此大型“连接 Painter”按钮不是主流程前置条件；高级工作区保留“测试 Painter 连接”作为诊断入口。JSON/剪贴板仍可在 Preview Link 不可用时独立使用。正式写入按钮必须明确标注“正式 Unity 材质”和 `Undo` 边界，避免与临时预览混淆。

专业单材质工作区中的 `当前 Texture Set（跟随 Painter）` 是只读状态，不再维护独立手动选择。它读取 `alg.mapexport.documentStructure().materials[].selected`，跟随 Painter 纹理集列表当前高亮项，并在每次手动或实时导出前重新校验。没有明确选中 Texture Set 时专业单材质导出必须停止，不能回退到排序后的第一个材质；切换 Texture Set 后实时预览执行一次全量刷新，避免复用上一材质的通道缓存。美术简易模式不走该选择链路，而是始终使用绑定中显式目标。

### PRJ-LINK-11｜按 Painter 工程指纹持久化并重验证材质映射

Bridge `2.10.1` 的 `hello` 响应包含：

```text
projectIdentity
projectIdentityKind
projectSignature
capabilities.artistWorkflowProjectIdentity
```

当前 `projectIdentityKind` 为 `inventory-fingerprint`。指纹输入是排序后的 Texture Set 名称，以及兼容 Shader Instance 的 `id/label/shader/url`；它不传输 `.spp` 绝对路径。该身份只用于选择本机候选缓存，不等价于 Painter 的永久工程 GUID。

映射保存在 Unity 当前工程的 `Library/PainterPreviewCache/ArtistMappings.json`，Schema 为 `ProjectACG.PainterArtistMappings@1`，不生成 Unity 资产或 `.meta`。同一 Painter 工程内按 Unity Material GUID 合并保存：逐个准备第二个材质不会删除第一个材质的记录。JSON 写入使用同目录临时文件与 `File.Replace/File.Move`；损坏文件会改名为 `.corrupt-*` 后忽略，避免一个坏文件阻断工具启动。

每次准备或发送前仍必须全量复核工程指纹/签名、Texture Set、Shader Instance ID/Label、Shader/URL、Adapter 版本以及重复 Texture Set/ID。任一项变化即忽略过期记录并要求重新一键准备。映射缓存不得绕过运行时握手，也不会自动保存 Painter 工程。

### PRJ-LINK-02｜参数只同步 80 项 Adapter 白名单

Source of Truth 是 `PainterMaterialParameterMapping.cs` 与 Painter `bridge.js` 中类型一致的 Descriptor 表。当前 80 项覆盖透明裁切、顶光、基础色/法线/MRA、Specular AA、直接/间接光、Ramp、F0、环境 Mip 模式、高光 Ramp、三层 GGX 各向异性和 MatCap 等两端共享参数。

版本边界必须分开：参数 JSON 的结构没有变化，因此 `ProjectACG.PainterMaterialProfile` 仍为 v1；Cloth 参数集合和行为已变化，因此 Adapter 为 `chara-cloth-v2@2`；Painter 插件实现当前为 `2.10.1`。不能只比较 Schema v1 就接受旧 Adapter 或旧运行时。

`_NeedSpecularRamp` 需要同步 Unity Keyword `_NEEDSPECULARRAMP_ON`。`_UseGGXAnisoSpecular`、`_UseAnisoNoise`、`_UseAnisoBreakup`、`_UseMatCap` 等在 Unity 当前是运行时数值分支，使用普通 Bool Value，不应凭命名自动生成并切换不存在的 Keyword。

相机、背景、Painter 图层、绘制通道、`Debug Mode`、Unity Rim/Outline/Shadow/Blend/ZWrite/Cull、RendererFeature 和其他非共享状态不在参数白名单内。相机由独立 `6419` 链路负责，不混入材质 JSON；Unity Toggle 导入时同步其绑定 Keyword；未知参数只报告警告。

## 3. 双向贴图预览契约

### PRJ-LINK-03｜Unity → Painter 发送最终打包贴图

Unity 侧发送当前材质的 `_BaseTex`、`_NormalMap`、`_MRATex`、`_EmissionTex`，以及可选 Diffuse Ramp、F0 Refine、高光 Ramp、各向异性噪声、MatCap 和 Unity SpecCube Atlas。该方向只给 Painter Shader 的预览槽使用，不会拆回 Painter Layer Stack，也不会改变正式分通道制作规则。

同时发送 80 项白名单参数、Base/Normal ST、`_AnisoNoiseMap_ST` 和 Atlas 的 `maxMip/padding/flipY` 元数据。Base、Emission、高光 Ramp 和 MatCap 是 sRGB 颜色纹理；MRA、Normal、各向异性噪声和 Atlas 是 Linear 数据。

`_SpecularRampTex`、`_AnisoNoiseMap`、`_MatCapTex` 和 Atlas 属于 Shader 辅助预览输入，不是 Painter 绘制通道。SP → Unity 不导出它们，也不会用 Painter Base/MRA 结果覆盖这些 Unity 材质槽；退出预览时它们随 Painter Shader Instance 快照恢复。

### PRJ-LINK-04｜Painter → Unity 按原生通道临时合图

当前合图契约：

| Unity 临时贴图 | Painter 来源 | 关键规则 |
| --- | --- | --- |
| `_BaseTex` | Base Color RGB + Opacity A | Base/Emission RGB 为 Linear 颜色；只有 Opacity 时 RGB 用白色 |
| `_NormalMap` | Normal / Height | Linear；按当前 Shader 的 XY 语义转换并重建 Z |
| `_MRATex` | 原生 Metallic/Roughness/AO，`User1.R` | G 为 Smoothness；原生 Roughness 使用 `1 - Roughness`；AO 优先 `ao_mixed`，回退烘焙 Mesh AO |
| `_EmissionTex` | Emissive RGB | Linear 颜色 |

当前正式预览链路不声明、不导出、也不读取 `User0`。旧 Painter 工程中的 `User0` 数据不自动删除，但不参与预览；Metallic、Roughness 和 AO 始终来自 Painter 原生通道，Reflectivity 使用 `User1.R`。AO 优先导出当前可见的 `ao_mixed`，失败时回退已烘焙 `ambient_occlusion` Mesh Map。

Painter 响应携带每个文件的 `channelFormats`。Base Color 与 Emission 依据 `sRGB8` 等颜色格式按 sRGB 源纹理加载；Opacity、Normal、Metallic、Roughness、AO、Reflectivity 保持 Linear。不能只根据 PNG 扩展名猜测颜色空间。

只有某组至少存在一个来源通道时才生成并覆盖该临时贴图。整组不存在时保留 Unity 原槽，包括 `None`；不会再生成默认灰图或默认白图强制赋值。组内缺失通道才使用中性值，例如仅有 Opacity 时 Base RGB 使用白色作为合并载体。

### PRJ-LINK-05｜预览会话不修改正式材质

Unity 使用临时 Material 和 `RenderTexture`，快照命中 Renderer 的完整原 `sharedMaterials` 数组，退出预览、窗口关闭、程序集重载、退出编辑器或进入 Play Mode 时恢复。Painter 同样在第一次 Unity 预览覆盖前按 Shader Instance 快照参数，`preview.clear` 时恢复。批量退出必须统一恢复所有绑定，不能只清理当前选中行。

Painter 快照按 Shader Instance ID 隔离。不同 Unity Material 经一键准备后绑定到各自独立实例，所以先同步材质 A、再切换并同步材质 B，不会用 B 的共享参数覆盖 A，也不会丢失 A 的映射；前提是准备与发送前的独立实例校验通过。

默认情况下正式 Material 不调用持久化 `Set*`、不标脏、不保存。两个显式“提升为正式结果”的入口与临时预览分离：

- 开启“保留 Unity → SP 共享参数”后退出：先恢复临时贴图、Atlas、灯光和 SP-only 参数，再把 80 个共享参数作为一次可 Undo 的 Shader Settings 修改重新应用；美术仍需保存 `.spp`。
- 开启“保存 SP → Unity 合并贴图”后退出：只把最后一帧 GPU 合图保存到指定 `Assets` 文件夹；Base/Emission 按 sRGB 导入，Normal/MRA 保持 Linear；不自动赋给正式 Material，也不替代正式 Export Preset。

批量“应用 SP 参数到 Unity”使用一个 Unity Undo 组写入全部 Adapter 白名单参数，但不修改正式贴图引用。所有持久化动作都必须由用户显式触发，不能因关闭实时开关自动提交。

美术简易模式的 `确认参数并结束` 等价于显式提交：先恢复临时贴图、Atlas、灯光、SP-only 状态和 Unity 临时材质，再只重应用共享参数；`放弃修改并恢复` 不保留任何共享参数。关闭窗口只发送 `keepUnityMaterialParameters=false` 的 best-effort 清理并立即恢复 Unity 临时会话，不能替代“确认参数并结束”。

缓存位于 `Library/PainterPreviewCache/UnityToPainter` 和 `PainterToUnity`，不进入版本控制。实时合图通常保留在内存 `RenderTexture`，仅退出保存时执行一次 GPU 回读和 PNG 导入。协议会校验传输文件位于当前工程缓存根目录，不能用来读取任意文件。

## 4. 颜色空间修正

### PRJ-LINK-06｜材质 Color 和贴图使用同一 Linear 计算边界

参数 JSON 的 `colorSpace` 固定为 `linear`。所有 Color RGB 在协议中为 Linear：Unity → Painter 时 Unity sRGB → Linear；Painter → Unity 时 Linear → Unity sRGB；Alpha 原样传递。该规则适用于 `_BaseColor`、`_OtherLightColor`、`_ShadowColor`、`_SpecularColor`、`_EnvColor`、`_IndirectTintColor` 和 `_EmissionColor`。

已解决的典型故障：Painter 中 `#808080` 对应 Linear 约 `0.214`；旧实现把 `0.214` 直接当 Unity sRGB 写入后，Inspector 约显示 `#373737`，导致 SP → Unity 明显变暗。修正后往返应保持 Painter 和 Unity Inspector 约 `#808080`。

贴图方向的规则不同于 Inspector Color：Unity → Painter 对 Base/Emission RGB 手动执行一次 sRGB → Linear；MRA、Normal 和 Alpha 不转换。Painter → Unity 导出 PNG 按 Linear 源数据上传，Hidden GPU Packer 直接写入 Linear `RenderTexture`，不再在 CPU 做 Linear → sRGB 字节转换；GPU Mipmap 在线性域生成。

## 5. 实时增量预览与性能

### PRJ-LINK-07｜只实时同步 SP → Unity，并按 Dirty 组刷新

保留手动 `SP → Unity 贴图+参数预览` 按钮，并提供可选 `SP → Unity 实时预览`。实时默认配置：

```text
分辨率：512
停笔防抖：200 ms
Padding：Diffusion 16 px
```

手动最终检查默认 `Native / Infinite Padding`，与实时性能配置独立。Painter 停笔且 Layer Stack 计算完成后，Python Dirty Companion 记录变化通道；JavaScript 只导出命中的通道，Unity 保留未变化源纹理，并只在 GPU 重建受影响的合图组和 Mipmap。

| Dirty 通道 | 更新组 |
| --- | --- |
| Base Color / Opacity | `_BaseTex` |
| Normal / Height | `_NormalMap` |
| Metallic / Roughness / AO / User1 | `_MRATex` |
| Emissive | `_EmissionTex` |

首次开启、切换 Shader Instance/Texture Set、改变实时分辨率、Python Companion 会话变化、通道增删、未知 Dirty 通道或状态不可读时自动回退全量。批量实时模式按 Texture Set/通道记录 Dirty，再按 `bindingId` 更新正确的临时 Material，不依赖 Painter 当前高亮 Texture Set。手动按钮始终强制全量，作为恢复和最终检查入口。

除绘制通道外，Painter Shader Settings 中的 80 个共享参数按低频轮询检测变化，只更新 Unity 临时材质参数；Painter 计算或贴图导出期间暂停轮询，避免与导出争抢主线程。

GPU 合图只优化 Unity 合并阶段，仍存在 Painter PNG 编码、磁盘读取、Unity PNG 解码/上传。状态栏分别报告 Painter 导出、文件读取、PNG 解码/上传、GPU 合图/Mipmap 提交和 Unity 合计耗时；GPU 项是 CPU 命令提交时间，不代表异步 GPU 完成。`Native/4K` 不用于连续实时绘制。

Unity → Painter 保持手动，避免双向实时同步回环。实时模式也不新增相机、背景、主灯或 Debug 参数同步范围。

## 6. Unity → Painter 灯光预览

### PRJ-LINK-08｜同步 Unity 角色 Shader 的有效主光

命令为 `unity.lighting.apply`，只修改 Painter 当前 Shader Instance 的预览灯光，不修改 Painter 图层，也不反向写 Unity 场景。Unity 窗口显式选择 Directional Light，或使用 `RenderSettings.sun`。

美术简易模式复用同一主灯控件：未手动选择时自动尝试 `RenderSettings.sun`，并在发送前取得当前材质已验证的 binding，把 Texture Set、Shader Instance ID 与 Adapter 一并写入 `target`。Painter 响应必须回传相同 `bindingId` 和 Shader Instance ID；不一致时 Unity 拒绝把操作视为成功。因此灯光只应用到当前材质对应的独立实例，不依赖 Painter 当前下拉选择，也不会影响已经同步的其他材质。专业场景同步模式保持手动选择 Painter 当前兼容实例的旧行为。

有效主光按以下顺序解析：

```text
Directional Light
-> _GlobalCharacterRenderLightDirection / Color / Strength / Toggle / FollowCameraXZ
-> 当前材质 _EffectLightToggle / _EffetDirectionDir / _EffetLightStrength
```

场景灯先合入 `GraphicsSettings.lightsUseColorTemperature` 与 `Light.useColorTemperature`，再转换为 Linear，并乘 `Light.intensity` 得到 HDR RGB。全局角色灯和材质特效灯按各自权重继续混合。最后以最大 RGB 拆分 Painter 颜色和强度，避免 HDR 能量被颜色控件截断。

当前方向约定：

```text
+Z = Azimuth 0°
+X = Azimuth 90°
-Z = Azimuth 180°
-X = Azimuth 270°
+Y = Elevation 90°
-Y = Elevation -90°
```

当前不实现 SP → Unity 灯光同步，不读取 Timeline/Renderer 写入的 `MaterialPropertyBlock`，也不做逐帧灯光跟随。若 Timeline 或其他系统在帧内覆盖当前参数，工具发送的是它能从场景灯、全局 Shader 状态和当前 Material 解析到的值，必须按实际画面再确认。

## 7. 双向相机同步

### PRJ-LINK-10｜只同步相机本体，不做视口或目标画幅对齐

相机使用独立 Python Companion 和 `127.0.0.1:6419`，不触发贴图导出。开启“相机视角实时同步”时，Unity 先发送初始状态，之后任一端移动都会更新另一端。当前同步：位置、朝向、Perspective/Orthographic、垂直 FOV 和正交垂直高度。

相机位置按 Unity/Painter 模型包围盒归一化，位置、Forward 和 Up 使用同一坐标系手性转换。Unity 模型锚点必须是场景实例并使用正数统一缩放；负缩放镜像、非零 Lens Shift、不同 Pose 或不同模型范围会破坏对齐前提。

当前实现明确不读取 Painter 3D View 尺寸、不绘制目标框，也不按 Unity GameView 与 Painter 视口宽高比补偿 FOV。视口比例不同会导致画面边界不同，但不代表相机位置、朝向或 FOV 同步失败。需要严格构图 A/B 时，由使用者人工保持两端宽高比一致，并关闭 Lens Distortion、Panini 等二次投影效果；不得在相机同步功能中重新引入隐式“视口对齐”。

相机开关的生命周期与材质实时预览独立。切换模型、相机、关闭窗口、程序集重载、进入 Play Mode 或关闭 `.spp` 时都应停止旧会话；状态区分别报告 `6418` 材质连接和 `6419` 相机连接，不能只看其中一个端口判断整个工具正常。

## 8. 插件安装与运行时版本

### PRJ-LINK-09｜更新后完整重启 Painter

Unity 安装器同时复制：

- Painter JavaScript/QML 插件 `ProjectACGMaterialBridge`；
- Python Dirty + Camera Companion `projectacg_material_bridge_dirty.py`；
- 当前 Painter Shader。

安装器不搜索 `Painter.exe` 所在程序盘。它以 Windows `MyDocuments` 为 Source of Truth，自动解析当前用户的 `Documents/Adobe/Adobe Substance 3D Painter` 资源根目录；JavaScript/QML 写入 `plugins`，Python Companion 写入 `python/startup`，Shader 写入用户 Shader 目录。更新时删除旧 `python/plugins` 同名 Companion，避免旧脚本遮蔽自动启动版本。随 Unity 工具交付的 `PainterPlugin` 目录保存完整本地插件、Python Companion 和 Painter Shader，外包无需从开发者机器的 Painter 安装目录复制文件。

安装后保存 Painter 工程并完整重启 Painter，再在 Shader Settings 中重新选择 `Chara_Cloth_V2_SP`。已出现过磁盘 `plugin.json` 和 QML 已更新，但 Painter 进程仍加载旧版本，Unity 发送新命令时返回：

```text
不支持的 Preview Link command: unity.lighting.apply
```

该现象的根因不是 Unity 命令名错误，而是 Painter 启动早于插件文件更新时间，旧 QML/JS/Python 命令分发仍在内存中。当前预期握手版本为 Bridge `2.10.1`、Adapter `chara-cloth-v2@2`。排查时分别核对工程源码、随 Unity 工具交付的插件源、用户资源目录、已安装 Shader 和当前运行时握手版本；仅覆盖文件、静态编译或重开窗口不能替代完整进程重启、重开 `.spp` 与重新选择 Shader。

## 9. 当前验证状态

已由源码静态确认：窗口五个一级工作区且默认美术简易模式；状态链为 `Idle / Checking / NeedsSetup / NeedsConfirmation / Ready / Previewing / Committing / Restoring`；请求自动连接；从模型获取去重兼容材质；只做完整同名与唯一 `mat_` ↔ `tex_` 匹配；`painter.targets.ensureBatch` 一次确认后创建/修复独立实例；当前/全部双向请求与简易模式主灯预览都通过 `AttachBindingTarget(...)` 显式路由；按 Shader Instance ID 隔离 Painter 快照；`确认参数并结束` 与 `放弃修改并恢复` 分离；关闭窗口不隐式提交。

同时静态确认：Bridge `2.10.1`、Preview Link v1/6418、独立相机 Link/6419、`chara-cloth-v2@2`、80 项参数白名单、`hello` 工程身份字段、`inventory-fingerprint`、`ArtistMappings.json` 合并保存/过期失效/损坏备份/原子写入、用户资源目录自动解析、`python/startup` Companion、缓存路径校验、双向贴图临时会话、缺失贴图组保持原槽、原生 MRA 与 `channelFormats`、Linear Color 双向转换、Python Dirty 增量组、按 `bindingId` 路由、GPU Packer、三档美术质量预设、有效主光解析、Color Temperature、HDR Color/Strength 拆分和 `unity.lighting.apply` 命令。

历史静态验证包括：`node --check bridge.js`、`plugin.json` 解析、C#/JavaScript Descriptor 数量对齐、GLSL 参数元数据 JSON 和 `git diff --check`。2026-08-08 参数桥接独立 Editor asmdef 定向构建为 `0 Warning / 0 Error`，`openspec validate add-painter-material-parameter-bridge --strict` 通过；工程指纹排序稳定性/变化敏感性与映射 JSON 往返、多材质合并、签名变化失效、损坏备份的开发期临时测试通过。静态和临时测试证据不证明 Odin 实际排版、Painter 当前运行时版本、相机坐标转换或两端视觉结果正确。

已由开发期故障确认：Painter 运行旧插件时会拒绝新增命令或缺少相机能力，完整重启后才会加载新 QML/JS/Python。磁盘文件版本和运行时握手版本必须分别检查。

仍需人工完成：

- 在 Painter 11.0.2 安装并实际加载 Bridge `2.10.1`，执行 Reload、重开 `.spp`、重新选择新版 Shader、默认共用 ID 的实例修复、多材质逐个/全部双向同步、简易模式逐材质主灯隔离、Undo、确认参数和放弃恢复；
- 检查五个一级工作区在最小窗口宽度下不截断、不出现空折叠组，切换后旧字段值和按钮行为保持；
- 执行 Unity/Painter 双向相机移动、透视/正交切换和 FOV/高度往返；确认不同视口比例只改变构图边界，不触发任何隐式 FOV 补偿；
- 使用高光 Ramp、三层 GGX 各向异性和 MatCap 参数/辅助纹理验证 `chara-cloth-v2@2` 的 80 项往返与预览恢复；
- 执行灯光六轴、Color Temperature、全局角色灯和材质特效灯 A/B；
- Base/Normal/MRA/Emission 分别单通道绘制时确认只刷新命中组，并记录 512/1K/2K/Native 的分阶段延迟；
- 以灰阶、纯 RGB 和 HDR Color 验证参数双向往返不偏暗；
- 窗口关闭、断线、脚本重载和进入 Play Mode 后确认 Renderer 材质槽及 Painter Shader 参数全部恢复；
- Timeline 或 Renderer MPB 正在驱动角色灯时，确认未覆盖项被明确识别，而不是误判为灯光已严格同步。

Unity 实际运行画面仍是最终验收标准。Preview Link 只缩短 A/B 路径，不能证明两端 BRDF、后处理、环境卷积或相机已经对齐。
