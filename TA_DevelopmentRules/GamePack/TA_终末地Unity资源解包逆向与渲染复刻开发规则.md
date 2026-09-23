# 终末地 Unity 资源解包、逆向与渲染复刻开发规则

## 0. 文档定位

本文记录从客户端资源取证、完整性校验、VFS/BLC/CHK 解包、加密 bundle 解密、Unity 资产导出，到 Shader/DXBC 逆向、Unity 2022.3 + URP 14 复刻和 GPU 对比验证的完整工作流。它是可迁移的方法规则，不是某个角色或某个版本的临时结论。

适用范围：

- 终末地客户端中角色展示页、Prefab、模型、贴图、材质、灯光、后处理和 Shader 的授权分析。
- 已有本地证据的继续解包、依赖递归、程序反汇编和效果重建。
- 将结论整理为可复核的工程资产、脚本、日志、截图和报告。

不适用范围：绕过在线服务、修改第三方账号、向外部系统上传密钥或未授权资产。所有操作默认只针对用户明确提供的本地安装目录和离线副本。

## 1. 总原则

1. 先固定证据，再写结论。原始文件、哈希、命令、日志和截图必须可追溯。
2. 先确认资产边界，再修改工程。一个 Prefab 实例、嵌套 Prefab、源 Prefab、Scene 实例不是同一个写入目标。
3. 解密结果必须经过长度、MD5 和 SHA-256 校验，不能以“文件能打开”代替正确性。
4. 逆向结论分为已证实、强推断、待验证三类；待验证数据不能写成确定事实。
5. 一次 A/B 测试只改变一个变量，并锁定相机、分辨率、姿势、灯光、材质、曝光和随机种子。
6. 任何真实密钥只存在于本地受控笔记或环境变量，不进入共享规则文档、截图、Git 或最终报告。
7. GPU 验证必须真的启用图形设备；禁止用 nographics 的结果证明 Shader、Bloom 或 Tonemapping 正确。

## 2. 推荐目录与证据布局

建议把客户端解包目录和 Unity 工程分开，路径尽量短、只用普通 ASCII 路径：

~~~text
D:/EndfieldHandoff/                         ZIP 交接包解压目录
D:/EndfieldReverse_YYYY-MM-DD/               离线逆向工作区
  raw/                                       原始 VFS、BLC、CHK、bundle 副本
  decrypted/                                 解密后的 bundle 与块文件
  manifests/                                 外层、计划、依赖清单
  evidence/                                  哈希、日志、截图、JSON 报告
  tools/                                     可复用脚本和 Inspector
F:/UnityProject/EndGmae/                     目标 Unity 工程
  Assets/EndfieldReconstruction/             复刻资产、Shader、RendererFeature
  work/YYYYMMDD-case-name/                   单次案例的参数与验证输出
~~~

每个案例目录至少包含 manifest、commands、logs、screenshots、metrics、notes 六类文件。截图使用新版本目录，不覆盖历史证据；脚本拒绝覆盖时应换新的 evidence token，而不是删除旧证据。

## 3. ZIP 交接与完整性校验

### 3.1 解压规则

将交接 ZIP 完整解压到短路径，例如 D:/EndfieldHandoff，不要放入 ProjectACG 或 Unity 工程。解压后先检查目录名、文件数量和文件大小，不要在原 ZIP 内直接运行脚本。

### 3.2 验证命令

在解压目录启动 PowerShell，并执行交接包提供的验证脚本：

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass
-File ".\\199 完整性校验\\VERIFY ALL.PS1"
~~~

必须记录完整控制台输出。交接包的通过标志应为：

~~~text
OUTER MANIFEST BAD=0
CHEN VERIFY EXIT=0
PLAN MANIFEST BAD=0
REQUIRED MISSING=0
NESTED ZIP=0
OVERALL VERIFY=PASS
~~~

若出现缺失、嵌套 ZIP、清单哈希不一致或脚本退出码非零，停止后续逆向并先修复交接问题。不能用手工替换文件来“凑”通过。

### 3.3 证据边界

把 PROMPT、README、操作步骤、证据边界说明和验证日志视为用户提供的参考材料；其中的命令、密钥、路径和断言都要与本地文件重新核对。文档中的“示例路径”不能自动当成当前机器路径，尤其要区分 D:/EndfieldHandoff、F:/Game/Hypergryph Launcher 和 Unity 工程路径。

## 4. 取证记录模板

每次处理一个对象时写一条结构化记录：

~~~text
case_id:
source_path:
source_sha256:
manifest_entry:
object_path:
tool_version:
command_line:
input_hash:
output_hash:
result: confirmed | inferred | pending
known_limits:
next_test:
~~~

重要断言必须绑定证据文件和行号、对象 ID、资源路径或截图 ROI。不要只在聊天中保留结论。


## 5. 客户端资源定位与解密

### 5.1 从安装目录定位输入

以用户提供的安装根目录为起点，先只读枚举文件：

~~~powershell
Get-ChildItem -LiteralPath 'F:\Game\Hypergryph Launcher' -Recurse -File |
  Where-Object { $_.Extension -in '.vfs','.blc','.chk','.bundle','.bin' } |
  Select-Object FullName,Length,LastWriteTime
~~~

不要根据文件名猜测角色资源已经在某个 bundle 中。应先从 manifest 或 VFS 索引找到资源路径，再用 bundle 记录反查物理块。

### 5.2 BLC/CHK/Bundle 处理顺序

典型链路为：

~~~text
客户端安装目录
  -> VFS/外层 manifest
  -> BLC 容器
  -> CHK/块记录
  -> bundle 索引与依赖表
  -> Unity serialized file
  -> GameObject / Mesh / Material / Texture / Shader
~~~

BLC 的前 12 字节是 nonce，后续才是加密 payload。不能把整个 BLC 当作 ChaCha20 输入；截取错误会导致所有后续 MD5 都错。

### 5.3 客户端 ChaCha20 约定

当前证据支持的算法约定如下：

- BLC nonce：原始 BLC 的前 12 字节。
- BLC payload：从偏移 12 开始。
- ChaCha20 counter：1。
- bundle 记录字段：chunk、offset、length、use_encrypt、iv_seed。
- 加密 bundle nonce：LE32(3) || LE64(iv_seed)。
- 解密后必须匹配记录中的 content_md5；同时记录 SHA-256。
- 实际密钥从本地受控配置读取，例如环境变量 KEY_HEX_FROM_LOCAL_SECRET，不把密钥写入本文。

伪代码：

~~~python
key = bytes.fromhex(os.environ[KEY_HEX_FROM_LOCAL_SECRET])
nonce = struct.pack(<I, 3) + struct.pack(<Q, iv_seed)
plain = chacha20_xor(key=key, nonce=nonce, counter=1, data=cipher)
assert md5(plain).hexdigest() == content_md5
record_sha256(plain)
~~~

注意 iv_seed 的字节序、counter 起始值和截取长度。任何一个字段错位，都会得到看似随机但长度正确的错误数据。

### 5.4 依赖递归

先解析入口 bundle 的依赖表，再用队列递归抓取依赖。每个节点记录：来源 bundle、目标路径、物理 chunk、解密状态、输出哈希和异常原因。重复依赖使用去重集合，不要反复解密。

推荐输出：dependencies.jsonl、export-summary.json、failed-dependencies.jsonl。已验证的一次案例为 71 个依赖，71 个导出成功，0 个失败；这只是该版本样本，不能硬编码成所有版本的保证。

### 5.5 解密失败排查

1. 检查输入是否为原始副本，确认文件大小和 SHA-256。
2. 检查 BLC 是否跳过 12 字节头部。
3. 检查 ChaCha20 counter 是否为 1。
4. 检查 bundle nonce 是否为小端 3 + iv_seed，不是把 iv_seed 直接截断成 12 字节。
5. 检查记录的 offset/length 是否相对于正确 chunk。
6. 对成功样本重新跑一次，确认结果稳定。
7. 若 MD5 对不上，保留错误输出但标记 pending，禁止继续导入 Unity。

## 6. Unity 资产解析与导出

### 6.1 对象关系

统一按以下关系追踪对象：

~~~text
Manifest path
  -> bundle
    -> serialized object
      -> GameObject / Transform
      -> Mesh / SkinnedMeshRenderer
      -> Material
      -> Texture2D / Cubemap
      -> Shader / SerializedProgram
      -> AnimationClip / Avatar
~~~

Unity AssetStudio 或自定义 Inspector 只负责读取证据，不自动证明运行时行为。要把对象 ID、文件名、引用关系和导出路径写入索引。

### 6.2 Prefab、Transform、相机与灯光

- INIT_POS=(0,300,0) 不是自动的最终角色世界位置。
- lookat_overview 等节点通常是相机目标，不是角色根节点。
- Prefab local TRS 不等于运行时 Scene World TRS；必须递归父节点矩阵并记录最终 world position、rotation、scale。
- m_Enabled=1 只证明组件序列化为启用，不证明所有页面灯光组在同一帧同时生效。
- Emission、Fresnel、Dissolve 等材质项不是自动的真实 Light，不能据此添加点光源。

要复刻角色页面，至少固定：角色 root、骨骼姿势或动画时间、相机 TRS、FOV、裁剪面、灯光的 position/rotation/type/intensity/color/range/spot angle、反射探针和后处理参数。

### 6.3 FBX 与模型导出

FBX 导出成功只说明几何体被写出，不保证以下内容：

- 材质槽顺序和贴图引用正确；
- UV 通道、法线、切线和骨骼权重正确；
- 坐标系、单位、轴向和 bind pose 一致；
- 动画、Avatar、BlendShape 和约束可用。

导出后必须用独立检查脚本验证顶点数、索引数、子网格、材质槽、UV 范围和骨骼矩阵。

### 6.4 贴图导入检查

身上贴图错乱的首要排查项：

- sRGB 与 Linear 导入标志；
- Normal Map importer 类型和压缩格式；
- UV 方向、翻转和第二 UV；
- 通道打包约定（R/G/B/A 各自用途）；
- 材质槽顺序和 renderer submesh 对应；
- mipmap、alpha source、wrap/filter；
- Shader keyword 和纹理采样宏。

颜色贴图通常按 sRGB 采样，法线、遮罩、粗糙度、金属度、深度和 LUT 数据通常按 Linear 采样。不要为了“看起来变亮”而统一勾选 sRGB。

## 7. 工具链规则

推荐组合：

- PowerShell：只读枚举、哈希、批处理和日志归档。
- Python：VFS/BLC/CHK 解析、ChaCha20、依赖递归、JSONL 报告。
- AssetStudio 或自定义 Inspector：Unity serialized object、Prefab、材质、贴图和导出。
- Unity 2022.3.62f3 + URP 14：目标复刻与 GPU 运行验证。
- RenderDoc、PIX 或 Unity Frame Debugger：纹理、RT、pass 顺序和常量观察。
- fxc、DXBC 反汇编工具：保留原始 DXBC、反汇编和寄存器信息。
- Git/LFS 或只读归档：保存原始文件和大体积证据，不覆盖来源。

工具版本、命令行、插件版本必须写入 toolchain.json。发现工具升级后结果变化时，先锁定版本再比较。

## 8. Shader 与 DXBC 逆向

### 8.1 证据链

Shader 还原必须保留四层证据：

1. serialized Shader、Pass、SubShader、RenderQueue、Blend、Cull、ZTest、关键字和变体。
2. SerializedProgram 与平台/关键字/程序索引的映射。
3. 原始 DXBC 二进制及反汇编文本。
4. CPU 或 IL2CPP 上传常量、纹理、关键词的调用证据。

先确认某段程序对应哪个 Pass、哪个平台和哪个变体，再解释指令。不能拿一个相似关键字的变体冒充角色页面实际程序。

### 8.2 指令翻译规则

常用映射：

- dp3 -> dot，保持输入顺序。
- mad -> 乘加，保持原始乘法和加法顺序。
- rcp 后接 max 不能被随意代数重排；这通常是除零保护的一部分。
- mov_sat、mad_sat -> saturate。
- movc -> 条件选择，不要直接当作普通 lerp。
- sample_l_indexable -> 固定 LOD 的纹理采样。
- log、exp 需要确认颜色空间和精度。
- round_ni 不能随意替换成 floor、round 或 trunc。

反汇编转 HLSL 时记录寄存器生命周期、常量缓冲区偏移、采样器绑定和临时变量。对每个猜测写出“原指令 -> HLSL -> 验证方式”。

### 8.3 材质分层

角色通常至少拆成 hair、skin、face、eye、clothPBR 等材质族；不要把所有材质强行塞入一套颜色乘法。每一族记录 BaseColor、Normal、Mask、Roughness、Metallic、AO、Emission、Rim/Fresnel、Subsurface 或眼球专用参数。

材质效果的证据优先级：实际材质属性和 Shader 常量 > 运行时 Frame Debugger/RenderDoc > 贴图命名 > 人工观察。贴图名称只能作为线索。

## 9. Unity URP 复刻实现

### 9.1 RendererFeature 组织

建议将角色效果拆为：

- 角色材质 Shader：处理几何、法线、PBR、皮肤、头发、眼睛和布料。
- Bloom prefilter/blur/upsample pass。
- 内部 HDR LUT 构建 pass。
- Tonemapping/output pass。
- 可选的 Auto Exposure pass，仅在证据闭合后启用。

每个 pass 有明确输入输出 RT、颜色空间、HDR 格式、尺寸、释放时机和执行事件。不要把曝光、Bloom、Tonemapping 全部揉进一个无法调试的大 Shader。

### 9.2 角色姿势与相机

还原角色页面时先关闭 UI，仅验证角色渲染。固定 Scene 中角色 root 的 world TRS、Animator 时间、相机 world TRS、FOV、分辨率、背景颜色和裁剪面；再逐项加入灯光、反射、材质和后处理。

相机目标节点和角色根节点分别记录。若角色在原游戏中由运行时脚本移动，Prefab 初始位置只能作为候选值。

## 10. 客户端后处理复刻

### 10.1 实际处理顺序

已恢复的角色页面路径不是 stock URP ACES，顺序为：

~~~text
Unexposed linear HDR scene
  -> Bloom threshold/prefilter
  -> Bloom multiplied by exposure E
  -> blur and high-quality bicubic upsample
  -> main scene multiplied by E
  -> Add Bloom
  -> internal HDR LogC LUT
  -> ACES_modified
  -> linear-to-sRGB
  -> dither
~~~

如果把 Bloom 放在 Tonemapping 之后，或先做 Tone Mapping 再乘曝光，会产生明显不同的高光、边缘和白布表现。

### 10.2 ACES_modified

客户端核心曲线可写成：

~~~hlsl
float3 denominator = x * (2.936045 * x + 0.887122) + 0.806889;
float3 inverseDenominator = max(rcp(denominator), 1e-4.xxx);
float3 numerator = x * (2.785085 * x + 0.107772);
float3 y = min(numerator * inverseDenominator, 1.0.xxx);

float3 AP1_LUMA = float3(0.272229, 0.674082, 0.053689);
float sourceLuma = dot(x, AP1_LUMA);
float highlightWeight = saturate((sourceLuma - 0.5) * 0.666667);

float mappedLuma = dot(y, AP1_LUMA);
y = mappedLuma.xxx + 0.93 * (y - mappedLuma.xxx);

float3 z;
z.r = dot(float3( 1.705052, -0.621791, -0.083259), y);
z.g = dot(float3(-0.130257,  1.140803, -0.010548), y);
z.b = dot(float3(-0.024003, -0.128969,  1.152972), y);

float maxChannel = max(max(z.r, z.g), max(z.b, 1e-5));
float3 normalized = saturate(z / maxChannel);
return saturate(lerp(z, normalized, highlightWeight));
~~~

不要把它标记为标准 URP ACES。任何改变常量、矩阵顺序、rcp 保护或 highlightWeight 的实现，都必须通过图像差异和中间 RT 对比。

### 10.3 角色页调色证据

角色页已记录的 grading 基线：Saturation=1.08，Shadows RGB=0.89473686，Midtones 约等于 1，Highlights=1；white balance、channel mixer、LGG、split toning、curves、user LUT 为 identity 或 inactive。先使用这些基线，再逐项验证是否有版本差异。


## 11. Bloom、曝光与黑屏排查

### 11.1 Bloom 基线

已恢复的角色页 Bloom 参数：Quality=High，Resolution=Half，Threshold=0.75，Intensity=0.45，Scatter=0.8，Blend=Add，Character bloom control=false。

阈值打包：

~~~text
T = GammaToLinearSpace(0.75) = 0.52252156
K = T * 0.5 + 0.00001
BloomThreshold = float4(T, T-K, 2*K, 0.25/K)
~~~

2560x1440 下原生金字塔尺寸为：960x540、480x270、240x135、120x68、60x34、30x17、15x8、8x4。实现要注意 RT 尺寸取整，不能假定每层都严格除二。

高质量路径包含 5 点独立 threshold、抗火焰亮度权重、9 tap 横向模糊、5 tap 纵向模糊和高质量 bicubic/four-bilinear 上采样。Bloom 在内部 LUT/Tonemapping 之前与主场景相加。

### 11.2 Auto Exposure

当前证据只证明客户端存在 HGAutoExposure，尚未闭合目标帧的 currentExposure、直方图输入域、历史帧和适应速度。生产复刻基线可暂用 sqrt(2)=1.41421356，但把它标为诊断常量；在直方图、pre-exposure 约定、同帧常量和 adaptation 全部对齐前，不要宣称 Auto Exposure 已完全还原。

### 11.3 黑屏问题

一个已定位的黑屏原因是全局 _EndfieldExposureMultiplier = 0。曝光为零会使主场景和 Bloom 全部变黑，即使灯光、材质和模型正确。

Shader 防护：

~~~hlsl
float e = _EndfieldExposureMultiplier;
half exposure = (e > 0.0 && isfinite(e))
    ? (half)max(e, 1e-4)
    : 1.0h;
~~~

同时在 RendererFeature 的 Create、Configure、AddRenderPasses 和编辑器回退路径初始化全局曝光。不要依赖某一帧 pass 偶然写入全局值。


## 12. GPU 验证与 A/B 测试

### 12.1 D3D11 验证命令

~~~powershell
Unity.exe -batchmode -force-d3d11 -quit -projectPath 'F:\UnityProject\EndGmae_CaptureScratch_20260919' -executeMethod ChenRenderingReview.VerifyScratchMigration -logFile 'F:\UnityProject\EndGmae\work\20260919-chen-showcase-reconstruction\evidence\logs\scratch-verify.log'
~~~

不要加 -nographics。命令结束后检查 Unity 日志、截图、Shader 编译错误、设备名称和渲染 API，确认确实走了 D3D11。

### 12.2 A/B 约束

A/B 两侧必须固定：相机 TRS、FOV、分辨率和裁剪面；pose/animation frame；灯光、反射探针和背景；材质、贴图和 shader keywords；exposure、Bloom 迭代、RT 格式；random seed、时间和生成顺序。每次只改一个变量，例如只替换 Tonemapping，或只关闭 Bloom。每个变量保存参数 JSON 和两张截图。

### 12.3 ROI 指标

至少设置 face、eyes、hair、black cloth、teal cloth、white cloth 和 silhouette ROI。记录 linear mean、sRGB Y、EV、Q1/Q3、DeltaE00、MAE 和像素差异比例。整体平均值不能替代局部指标：脸和白布的高光误差可能被背景平均值掩盖。

推荐报告字段：baseline_hash、candidate_hash、camera_hash、exposure、bloom_params、roi_metrics、changed_variable、acceptance_note。

## 13. 常见失败与解决方式

| 现象 | 常见根因 | 处理方式 |
| --- | --- | --- |
| 解密文件能打开但内容错 | nonce/counter/offset 错 | 重新按记录切块，核对 MD5/SHA-256 |
| 角色贴图错乱 | sRGB、UV、通道或材质槽错 | 单独验证纹理导入和 submesh 映射 |
| FBX 有模型但姿势不对 | bind pose、骨骼轴向或动画没导出 | 检查 Avatar、骨骼矩阵和固定帧 |
| 光照不对 | local TRS 当成 world TRS，灯光组误合并 | 递归父矩阵，按页面状态验证灯光 |
| Bloom 看起来过亮/过暗 | 阈值域、曝光顺序或 RT 尺寸错 | 对比 prefilter、每级 pyramid 和合成前 RT |
| tonemapping 偏灰/偏红 | 使用 stock ACES 或矩阵顺序错误 | 使用 ACES_modified 常量并做中间值对比 |
| 画面全黑 | 全局曝光为 0 或 pass 未执行 | 初始化曝光，检查 Feature 生命周期和 Frame Debugger |
| Editor 有图、GPU 无图 | 使用 nographics 或 shader 编译失败 | D3D11 验证，读取 player/editor log |
| 修改保存到错误 Prefab | owner/target 边界不清 | 先解析源资产路径，再执行写入 |
| 截图被覆盖 | 证据目录复用 | 使用新 evidence token，历史目录只读 |


## 14. 可编辑流程图

~~~mermaid
flowchart TD
  A[ZIP 交接包] --> B[完整性校验]
  B -->|PASS| C[只读枚举客户端文件]
  C --> D[VFS / Manifest]
  D --> E[BLC / CHK 定位]
  E --> F[ChaCha20 解密]
  F --> G{MD5 + SHA256}
  G -->|通过| H[依赖递归]
  H --> I[Unity serialized object]
  I --> J[Prefab / Mesh / Texture / Material]
  J --> K[Shader / DXBC / 常量]
  K --> L[URP 角色材质复刻]
  L --> M[Bloom + LUT + ACES_modified]
  M --> N[D3D11 GPU A/B]
  N --> O[ROI 指标与报告]
  G -->|失败| P[保留失败证据并回查 nonce、offset、counter]
  B -->|FAIL| Q[停止并修复交接包]
~~~

流程图只描述工作顺序，不代表每个项目都能无条件自动化。任何节点若证据不足，应回到上一个可验证节点，而不是跳过校验。

## 15. 证据与报告模板

### 15.1 解密记录

~~~json
{
  "input": "raw/chunk_xxx.blc",
  "chunk": "chunk_xxx",
  "offset": 0,
  "length": 0,
  "use_encrypt": true,
  "iv_seed": 0,
  "nonce_rule": "LE32(3)||LE64(iv_seed)",
  "counter": 1,
  "content_md5": "...",
  "output_sha256": "...",
  "status": "confirmed|pending|failed"
}
~~~

### 15.2 渲染对比记录

~~~text
case_id:
reference_image:
candidate_image:
camera_trs_hash:
fov:
resolution:
pose_time:
lights_hash:
material_hash:
exposure:
bloom:
tonemapping:
changed_variable:
roi_metrics:
decision:
~~~

### 15.3 结论分级

- Confirmed：有原始文件、哈希、程序/常量或 GPU 中间结果直接支持。
- Inferred：多个独立证据一致，但缺少运行时闭环；必须写明推断依据。
- Pending：存在未解密依赖、未知变体、未匹配常量或未完成 GPU 验证；不能作为最终实现依据。

## 16. 最终检查清单

- [ ] ZIP 已解压到独立短路径，未混入 Unity 工程。
- [ ] VERIFY ALL.PS1 输出为 OVERALL VERIFY=PASS。
- [ ] 原始 VFS/BLC/CHK 和 manifest 已只读归档并记录 SHA-256。
- [ ] BLC 头部 12 字节、payload、counter=1 和 bundle nonce 规则已核对。
- [ ] 每个解密 bundle 的 content_md5 和 SHA-256 已验证。
- [ ] 依赖递归有成功/失败清单，失败项未被静默跳过。
- [ ] GameObject、Transform、Mesh、Material、Texture、Shader、Animation 引用可追溯。
- [ ] local TRS 与 runtime world TRS 已区分；角色根、相机目标和灯光组未误合并。
- [ ] FBX 的子网格、材质槽、UV、法线、切线、骨骼和姿势已检查。
- [ ] 贴图 sRGB/Linear、Normal Map、通道打包、mipmap 和 keyword 已核对。
- [ ] Shader 变体、Pass、SerializedProgram、DXBC、常量上传证据已归档。
- [ ] 角色材质与 Bloom、LUT、ACES_modified、曝光 pass 分开验证。
- [ ] `_EndfieldExposureMultiplier` 有非零初始化和无效值保护。
- [ ] Auto Exposure 若证据未闭合，仍标为诊断状态。
- [ ] D3D11 GPU 验证未使用 nographics，日志和截图可定位。
- [ ] A/B 每次只改变一个变量，ROI 指标已记录。
- [ ] 最终文档未包含真实密钥、账号、令牌或未授权外传路径。

## 17. 相关文件索引

建议在案例报告中链接以下本地材料：

- `D:/EndfieldReverse_2026-09-18/LOCAL_DECRYPTION_NOTES.md`：本地解密约定和受控密钥位置。
- `D:/EndfieldReverse_2026-09-18/tools/extract_direct_bundle_dependencies.py`：依赖递归导出脚本。
- `F:/UnityProject/EndGmae/Assets/EndfieldReconstruction/Shaders/Runtime/EndfieldLutBuilderHdr.shader`：内部 HDR LUT。
- `F:/UnityProject/EndGmae/Assets/EndfieldReconstruction/Shaders/Runtime/EndfieldUberPost.shader`：后处理主路径。
- `F:/UnityProject/EndGmae/Assets/EndfieldReconstruction/Shaders/Runtime/EndfieldOriginalBloom.shader`：Bloom 实现。
- `F:/UnityProject/EndGmae/Assets/EndfieldReconstruction/Rendering/ChenOriginalPostProcessFeature.cs`：RendererFeature 和全局参数初始化。
- `F:/UnityProject/EndGmae/Packages/com.unity.render-pipelines.universal/Runtime/Passes/PostProcessPass.cs`：URP 对照实现。

本文不替代原始证据。每次换客户端版本、平台、图形 API 或 Shader 变体，都应重新跑完整性、哈希、反汇编和 GPU 验证。
