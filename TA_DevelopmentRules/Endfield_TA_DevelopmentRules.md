# 终末地角色渲染还原与客户端资源解包：TA Development Rules

> 版本：2026-09-28  
> 用途：记录本项目中“客户端资源证据 → RenderDoc/Bundle 分析 → Unity URP 14 还原 → 验证与交付”的可复用方法。

## 1. 文档适用范围

动画解包、ACL 解码、Avatar/TOS 绑定、Generic/Humanoid `.anim` 与 AnimatorController 的完整沉淀已拆分到 [AnimationDevelopment/README_Tech_TAAnimationDevelopmentRules.md](AnimationDevelopment/README_Tech_TAAnimationDevelopmentRules.md)，其中当前终末地路径、工具、产物统计和已知限制见 [Endfield 角色页动画重建 Profile](AnimationDevelopment/Profiles/Endfield/role-page-animation-reconstruction.md)。本文件继续作为角色渲染、RenderDoc、URP、材质和后处理 Profile 的入口，避免把动画导入事实与渲染规则混写。

本规则适用于经授权的本地客户端资产分析、Unity 角色模型与材质效果还原、RenderDoc 帧分析、后处理还原、动画导出和工程化验证。目标是让每个结论都能追溯到原始证据，并让后续工程可以重复执行，而不是依赖一次性的手工调参。

### 1.1 证据等级

- `[EVIDENCE]`：来自文件、RenderDoc 捕获、Unity 序列化数据、编译日志或运行时探针的直接证据。
- `[INFERENCE]`：由多个证据推导出的实现假设，需要在 Unity 或 RenderDoc 中验证。
- `[UNVERIFIED]`：目前缺少证据，不能写成“已还原”。
- `[APPROXIMATION]`：为了在 URP 中复现视觉效果而采用的近似实现，必须记录偏差来源。

所有报告、提交说明和参数表都必须标注等级。证据不足时，优先扩大采样和交叉验证，不通过“看起来像”来补齐结论。

## 2. 总体工作流

```text
输入 ZIP/客户端/RenderDoc
        ↓
隔离解包与完整性校验
        ↓
目录清单、哈希、版本和工具链记录
        ↓
Bundle/资源/脚本/材质/贴图/动画索引
        ↓
RenderDoc 捕获：Pass、资源、常量、纹理、状态
        ↓
按模块重建：模型 → 材质 → 光照 → 后处理 → 动画
        ↓
Unity URP 14 集成与中文参数面板
        ↓
编译检查、运行时日志、截图对比、差异回归
        ↓
证据清单、可复现步骤、限制和交付包
```

每个模块在进入下一个模块前必须达到“有输入、有输出、有验证”的状态。不要在 shader、灯光、后处理和动画同时改变时试图定位差异。

## 3. 解包和证据管理

### 3.1 路径隔离

- 将 ZIP 解压到较短、普通、独立的路径，例如 `D:\EndfieldHandoff`。
- 不要把证据目录放进 `ProjectACG` 或目标 Unity 工程，避免导入器、Library 缓存、自动重命名和 `.meta` 文件污染原始证据。
- 目录名避免空格、括号和过深层级。长路径会影响 PowerShell、Unity AssetDatabase、第三方 Bundle 工具和脚本参数解析。
- 原始包、解压结果、工作副本、导出结果、报告分开保存。

推荐结构：

```text
D:\EndfieldHandoff\
├─ 00_开始这里\                 # 接收说明和第一条提示词
├─ 01_原始ZIP\                  # 不改动
├─ 02_完整解压\                 # 完整性校验对象
├─ 03_证据索引\                 # 哈希、清单、工具版本
├─ 04_工作副本\                 # 允许脚本生成临时文件
└─ 05_导出\                     # FBX、纹理、anim、报告
```

### 3.2 完整性校验

在解压目录执行接收包提供的校验脚本，例如：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File ".\199 完整性校验\VERIFY ALL.PS1"
```

成功条件必须完整记录，不能只看最后一行：

```text
OUTER MANIFEST BAD=0
CHEN VERIFY EXIT=0
PLAN MANIFEST BAD=0
REQUIRED MISSING=0
NESTED ZIP=0
OVERALL VERIFY=PASS
```

注意：用户原始示例中有 `PLAN MANIFEST BAD=B`、`REOUIRED` 等疑似 OCR/转写错误；正式记录以脚本真实输出为准。校验失败时先保存完整日志和失败路径，不要继续把未验证文件当成证据。

### 3.3 哈希和来源记录

对原始 ZIP、RenderDoc `.rdc`、关键 Bundle、导出纹理和生成的 `.anim` 记录 SHA-256、大小、修改时间和来源。证据清单至少包含：

```text
relative_path | sha256 | bytes | source | extraction_tool | status | notes
```

工具输出、错误日志和人工判断分开存放。不要覆盖原始日志；修复脚本时使用带日期的副本。

## 4. 客户端 Bundle 和 Unity 资源分析

### 4.1 索引优先

先建立资源索引，再加载单个资源。必须记录：

- Bundle 名、路径、依赖、CAB 名和 Unity 版本。
- `AnimationClip`、`AnimatorController`、`Avatar`、`Mesh`、`Material`、`Texture2D`、`Shader`、`MonoBehaviour` 的对象 ID。
- 对象之间的引用关系，特别是 Renderer → Material → Texture/Buffer。
- 导出器版本、脚本版本和导出时间。

对于角色 UI 模型，完整解析往往需要同时加载一组依赖 Bundle。已确认的案例中，相关资产目录为 `work\chen-weapon0011-ab-extraction\chen_assets`，需要 40 个相关 Bundle 一起建立引用图；只打开单个 `b8c92fb90410297e1d4e12eb.ab` 会得到不完整结果。

### 4.2 原始资产与工程资产分层

- 原始解包结果保持原格式和原命名。
- Unity 可读副本放在工程外的 staging 目录，完成验证后再复制到 `Assets/EndfieldReconstruction/...`。
- 任何脚本生成的 `.mat`、`.asset`、`.anim` 需要带来源索引，不能用同名文件覆盖原始导出。

### 4.3 Shader 证据提取

从 Bundle/RenderDoc 提取 shader 时，分别保存：

1. shader 二进制或反编译文本；
2. pass、关键字、变体和渲染状态；
3. 常量缓冲布局和字段偏移；
4. 采样纹理的绑定槽、采样器和 UV 变换；
5. 顶点输入语义、TBN、骨骼权重和实例数据；
6. RenderDoc 中该 shader 的实际 draw call 和 pipeline state。

shader 反编译代码只能证明“执行了哪些运算”，不能自动证明字段语义。字段语义需要结合材质序列化数据、贴图通道统计和 RenderDoc 常量值确认。

## 5. RenderDoc `.rdc` 分析规则

### 5.1 捕获文件处理

对 `chenqianyu.rdc` 这类捕获文件先做只读复制和哈希。分析顺序：

1. 选择角色实际可见的 draw call；
2. 记录 Event ID、Pass 名、PS/VS、绑定纹理和 Render Target；
3. 查看 Pipeline State 的 Blend、Depth、Cull、Stencil、Color Mask；
4. 查看 shader 常量缓冲和纹理预览；
5. 导出关键纹理和像素历史；
6. 用同一个 Event ID 与 Unity 截图对照。

### 5.2 判断 shader 是否还原正确

至少逐项比较：

- Base Color、AO、Normal、Roughness、Metallic、Emission 的通道和色彩空间；
- UV 变换、贴图寻址和法线 Y/绿色通道方向；
- 顶点切线和法线是否与模型坐标系一致；
- 阴影接收、阴影投射、透明排序和深度写入；
- 关键字导致的分支，例如 skin、hair、eye、clothPBR；
- 光源方向、颜色、强度、衰减、cookie、阴影 bias；
- 最终颜色进入后处理前的 HDR 范围。

“颜色相近”不等于正确。对同一材质至少截取阴影、中间调、高光三个区域，比较线性空间值和最终显示值。

### 5.3 RenderDoc 证据格式

建议保存：

```text
rdc_analysis/
├─ capture_sha256.txt
├─ event_01234_pipeline.json
├─ event_01234_shader_ps.txt
├─ event_01234_constants.csv
├─ event_01234_textures/
├─ pixel_history_01234.csv
└─ findings.md
```

## 6. 角色材质和 shader 重建

### 6.1 先建立材质模型

将角色按功能拆分：`skin`、`face`、`eye`、`hair`、`clothPBR`、`metal`、`transparent/overlay`。每一类先写输入和输出，再实现计算。

通用 PBR 计算应明确：

```text
N = normalize(normal map transformed by TBN)
V = normalize(cameraPosition - worldPosition)
L = normalize(lightPosition - worldPosition)
H = normalize(V + L)
NoL = saturate(dot(N, L))
NoV = saturate(dot(N, V))
NoH = saturate(dot(N, H))
VoH = saturate(dot(V, H))
```

然后按证据选择 GGX/Disney 或自定义项：`D`、`G`、`F`、diffuse、clear coat、subsurface、rim、anisotropy。任何自定义项必须记录来源 Event、常量或贴图通道。

### 6.2 贴图和色彩空间

- Base Color、Emission 通常按 sRGB 读取；Normal、Mask、Roughness、Metallic、AO 使用线性数据。
- Unity 贴图导入设置必须和 RenderDoc 采样结果一致，包括 sRGB、压缩、Mip、Wrap 和 Filter。
- Roughness 与 Smoothness 常见关系为 `smoothness = 1 - roughness`，但不能默认；从 shader 指令或材质值确认。
- Metallic/Roughness/AO 可能打包在同一张 RGBA 贴图中，必须通过通道统计和常量验证。
- 法线贴图 Y 通道反转会产生“高光方向错”和“光照像贴图错乱”，优先检查这一项。

### 6.3 高光金属和粗糙度不生效的排查

常见原因按优先级：

1. 采样的贴图不是当前 Renderer 使用的贴图；
2. Roughness 被当 Smoothness 使用或反之；
3. Metallic 通道被当作颜色或 Alpha 丢弃；
4. shader 没有启用对应 keyword/variant；
5. 法线空间错误，导致 `NoH` 和高光方向错误；
6. 主光源/反射探针没有有效亮度；
7. 后处理曝光或 Tonemap 把高光压平；
8. MaterialPropertyBlock 在运行时覆盖了材质值。

排查时临时输出单通道 debug pass：Base、Normal、Roughness、Metallic、AO、Specular、PreTonemap，逐项确认数据流。

## 7. URP 14 集成规则

目标工程使用 URP 14.0.12 时，遵守以下 API 生命周期：

### 7.1 ScriptableRendererFeature/Pass

- 不在 `AddRenderPasses` 中读取 `renderer.cameraColorTargetHandle`。
- 在 `ScriptableRenderPass.OnCameraSetup` 或 `Execute` 的允许范围内获取相机目标，并确保目标尚未释放。
- 使用 `RTHandle` 和 `Blitter.BlitCameraTexture` 时，先确保源、目标和 Material 均非空，且目标已通过 `ConfigureTarget`/临时 RT 正确分配。
- `Dispose` 中释放 Material、RTHandle 和临时资源。
- 在 `ConfigureInput`、`renderPassEvent` 和相机类型过滤上明确写出意图。

错误模式：

```text
AddRenderPasses -> renderer.cameraColorTargetHandle
```

这会触发“只能在 ScriptableRenderPass 范围内访问 cameraColorTarget”的异常。正确做法是把 target 延迟到 Pass 生命周期中设置，或让 Feature 只传递配置对象。

### 7.2 Volume API

URP 14 中不要依赖旧管线的 `IPostProcessComponent`。自定义体积组件继承 `VolumeComponent`，用 `BoolParameter/ClampedFloatParameter/ColorParameter` 等字段，按需实现 `IsActive()` 和 `IsTileCompatible()`（若当前 API 要求）。

参数不生效时按顺序检查：

- Volume Profile 是否包含组件且 `active=1`；
- 参数 Override 是否勾选；
- Volume Layer Mask 是否包含 Volume 所在层；
- 相机是否启用了 Post Processing；
- Camera Stack 是否将目标相机纳入；
- Renderer Feature 是否已启用且 renderPassEvent 合理；
- `VolumeManager.instance.stack` 是否读到当前混合值；
- 自定义桥接器是否把值写入实际使用的 URP Volume/Material。

建议增加菜单命令或运行时日志，打印当前混合 Volume 实际参数，而不是只打印 Profile 资产值。

### 7.3 参数桥接

证据参数与运行时参数分两层：

```text
ChenEvidenceVolumeComponent  --(VolumeManager stack)-->  ChenVolumeParameterBridge
                                                       --> Material / URP Volume / RenderFeature
```

桥接器必须处理 Override 状态、Volume 权重、相机层级和默认值。字段映射应有表格，避免“面板看得到但没有写入 shader”。

## 8. Tonemapping、Bloom 和后处理

### 8.1 证据优先

Tonemapping 不能仅通过一张最终截图确定。需要从 RenderDoc 记录：

- Tonemap 前 HDR 颜色；
- Tonemap 后颜色；
- 曝光值、白点、曲线参数；
- LUT 是否参与；
- Bloom threshold、prefilter、downsample、upsample、intensity、scatter；
- 色调映射 pass 的 shader、常量和输入输出格式。

只有最终截图时，结论应标记为 `[APPROXIMATION]`。ACES、Neutral、Filmic 或自定义曲线需要用多组 HDR 输入拟合，不能只用一个中灰点。

### 8.2 黑屏和画面过暗

优先检查：相机颜色目标是否有效、Material 是否为空、RTHandle 是否正确分配、shader 是否编译成当前平台、Render Feature 是否在 Scene/Game 相机上运行、曝光是否被重复应用、颜色空间是否重复转换。

在每个 pass 输出一个临时 debug 颜色，确认黑屏发生在哪个阶段：源 RT、shader 输入、Tonemap 输出或 Blit 回写。

### 8.3 Bloom 验证

Bloom 应拆为：threshold/prefilter → mip chain downsample → upsample/scatter → composite。检查每一步的格式、HDR 范围和 UV 卷积。若只有一张全屏模糊图，通常无法复现真实 Bloom 的高光阈值和边缘扩散。

## 9. 动画解包和 Unity `.anim`

### 9.1 资产定位

先从 `AnimationClip`、`AnimatorController`、`Avatar` 和 UI 预制体的引用关系定位角色页面使用的动画。不要把 UI 视频或表情贴图序列当成骨骼 `.anim`。

已确认的真实 Clip 示例：

```text
A_actor_chen_dialog_state_scratchface_start
A_actor_chen_dialog_state_scratchface_loop
A_actor_chen_dialog_state_scratchface_end
```

这些 Clip 位于 CAB `CAB-4f9126151750655b69df69ae88519443`，对象普通曲线列表为 0，数据在 `m_AclCompressedBuffer`。探针显示采样率 60、绑定数 854、Transform 输出轨道 349、Root 轨道 28、Float 曲线 148、ACL buffer version 10。

### 9.2 ACL 压缩动画

ACL Clip 不能用只支持普通 `transform_frames.json` 的导入器直接还原。正确流程：

1. 解析压缩头、版本、采样率、轨道数和量化范围；
2. 解析 Transform/Root/Float buffer；
3. 恢复每个轨道的绑定哈希或绑定索引；
4. 用 Avatar/骨骼层级建立哈希到路径的映射；
5. 以固定采样率解压到 `localPosition/localRotation/localScale`；
6. 删除误判为常量的轨道前先比较首尾帧误差；
7. 生成 Unity `AnimationClip`，设置 `frameRate`、循环标志和根运动策略；
8. 在目标模型 Avatar 上实测骨骼运动和姿势方向。

### 9.3 Humanoid 兼容性

“存在 Avatar”不等于动画一定能在目标模型播放。验证至少包括：

- Avatar 为 Humanoid 且 Valid；
- 必要的人体骨骼映射完整；
- 动画绑定路径与模型骨骼一致，或已通过 Humanoid retargeting；
- Root/Bip001 坐标系、单位和朝向一致；
- Loop、Root Motion、脚底和手腕没有明显漂移；
- Unity Inspector 中曲线数量和 Clip 长度非零。

## 10. 诊断和自检

### 10.1 编译检查

提交前至少执行：

```text
Assembly-CSharp.csproj       0 error / 0 warning
Assembly-CSharp-Editor.csproj 0 error / 0 warning
```

编辑器日志必须按项目路径和时间过滤。不要把其他工程历史日志中的错误直接归因到当前工程。

### 10.2 运行时检查清单

- Scene 相机正确启用，位置、旋转和 FOV 有记录；
- Renderer、Renderer Feature、Volume Profile 和 Layer Mask 已关联；
- 角色 Renderer 使用预期 Material 和贴图；
- 主光、补光、轮廓光和反射探针存在且参数已写入；
- Tonemap/Bloom/曝光每个 pass 只应用一次；
- 关键 Material 参数调整后画面确实发生变化；
- 截图分辨率、相机、时间、颜色空间相同；
- 运行时无 NullReference、RTHandle 生命周期和 shader 编译错误。

### 10.3 差异定位顺序

```text
模型/姿势/镜头
  → 贴图/UV/法线
  → 光源/阴影/反射
  → 材质 BRDF/高光
  → Tonemap/曝光
  → Bloom/颜色分级/LUT
```

先固定上游输入再调整下游效果。否则一个错误的相机曝光会被误认为是 shader 错误。

## 11. 工具选择

| 任务 | 工具 | 规则 |
|---|---|---|
| 文件校验 | PowerShell、SHA-256 | 保留完整输出和版本 |
| Unity Bundle/对象 | AssetRipper/UnityPy/自写探针 | 先索引后导出，记录对象 ID |
| RenderDoc | RenderDoc UI、Python API | 保存 Event、Pipeline、常量和纹理证据 |
| Shader | DXBC/DXIL/SPIR-V 反编译器、RenderDoc shader debug | 反编译结果必须和 draw call 对齐 |
| 二进制/脚本分析 | Ghidra/IDA/radare2、dnSpy（仅授权样本） | 只分析本地授权文件，保存偏移和版本 |
| Unity 工程 | Unity 2022.3、URP 14.0.12、C# 编译器 | 使用目标版本 API，避免旧后处理接口 |
| 动画 | ACL 解码器、Unity Editor 导入脚本 | 先验证轨道和绑定，再生成 `.anim` |
| 文档 | Markdown、docs-generator | 结论、证据、限制分开写 |

工具安装和版本必须写入 `03_证据索引/toolchain.json`，避免后续同名工具产生不同结果。

## 12. 常见问题、原因和解决方式

| 现象 | 高概率原因 | 解决方式 |
|---|---|---|
| 参数面板变化但画面不变 | 未勾选 Override、Volume 不在 Layer Mask、桥接未执行 | 打印 VolumeStack 和最终 Material 属性 |
| `cameraColorTarget` 生命周期异常 | 在 `AddRenderPasses` 访问 camera target | 延迟到 Pass 的 OnCameraSetup/Execute |
| `BlitCameraTexture` NullReference | Material、源 RT、目标 RTHandle 未初始化 | 在 pass setup 中分配并逐项判空 |
| Tonemap 黑屏 | 重复 Blit、格式/颜色空间错误、shader 编译失败 | 每个阶段输出 debug 颜色并检查 RT 格式 |
| 金属高光很弱 | Metallic/Roughness 通道错、光源/反射为空 | 单通道 debug，确认 BRDF 输入 |
| 身上贴图错乱 | UV、通道打包或材质引用错 | RenderDoc 绑定表对照 Material 引用 |
| 画面俯视角不同 | 相机位置/旋转/FOV 未锁定 | 从截图和捕获记录相机参数，固定序列化值 |
| 动画 Clip 曲线为 0 | ACL 压缩数据未解码 | 解析 `m_AclCompressedBuffer`，不要套普通曲线导入器 |
| Humanoid 不动或扭曲 | Avatar 骨骼映射/绑定路径不匹配 | 验证 Avatar、哈希路径和坐标系 |
| 编译报 `IPostProcessComponent` 缺失 | URP 14 不再提供旧接口 | 改用 `VolumeComponent` 和 URP 14 生命周期 |

## 13. 可复用交付模板

每个模块交付以下文件：

```text
module_name/
├─ README.md                 # 目的、输入、输出、复现步骤
├─ evidence.json             # 文件哈希、Event/Object ID、工具版本
├─ findings.md               # EVIDENCE/INFERENCE/UNVERIFIED
├─ parameters.csv            # 参数名、值、单位、来源、默认值
├─ implementation/           # shader、C#、材质、配置
├─ validation/               # 编译日志、截图、RenderDoc 对照
└─ known_limits.md           # 当前不能证明的内容和下一步
```

参数表字段建议：

```text
name | type | source | value | range | override | runtime_target | verified | notes
```

## 14. 复盘规则

1. 先固定证据版本，再做任何解包或导入。
2. 每次只改变一个模块或一组明确相关参数。
3. 所有视觉结论都要有截图或数值证据。
4. 运行时调参必须能回写到可复现的资产或配置文件。
5. 发生错误时记录最小复现路径、堆栈、版本和修复前后差异。
6. 不把推测写成已完成，不把视频帧当骨骼动画，不把单个 Bundle 当完整依赖图。
7. 生成 `.anim`、材质、shader 或 LUT 后，必须在目标 Unity 版本中重新导入并验证。
8. 对性能配置分别记录 AA、阴影、HDR、Bloom、Tonemap 和分辨率；不要以单一截图推断全部设备档位。
9. 原始证据只读保存，工程改动通过补丁或版本控制提交。
10. 最终文档必须能让另一位开发者在新机器上按步骤复现同一结果。

## 15. 当前案例的已知事实和限制

- 已确认角色对话抓脸的三个真实 `AnimationClip` 及其 ACL 压缩数据；它们是否等同于展示页主循环动作，仍需通过 UI Animator Controller 引用和运行时采样确认。
- 角色动画依赖多个 Bundle，单个 Bundle 导出会缺引用。
- URP 14 的 Render Feature 和 Volume 生命周期是此前黑屏、NullReference 和参数不生效问题的主要排查点。
- Tonemapping 的精确算法必须有 RenderDoc 中的 shader/常量或多组输入输出；只有截图时只能交付近似曲线，并明确标注。
- 角色材质的金属、粗糙度和高光表现必须同时验证贴图通道、BRDF、光源、反射和后处理，不能只改材质颜色。

## 16. 最终验收

验收通过条件：

- 完整性校验 PASS，关键证据有哈希；
- 所有脚本和 shader 在 Unity 2022.3/URP 14 编译无错误；
- 场景运行时无 RenderPass 生命周期异常；
- UI 角色的模型、姿势、相机、FOV、灯光、材质和后处理均有参数来源；
- 动画 Clip 曲线非空，目标 Avatar 实际运动；
- 与 RenderDoc/参考截图的差异有量化或明确的未验证项；
- 交付目录含复现说明、工具版本、日志、截图和已知限制。

