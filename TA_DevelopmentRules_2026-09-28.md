# TA / Codex 开发规则：终末地角色渲染还原、解包、抓帧与动画

更新时间：2026-09-28  
适用范围：本地、已授权的游戏客户端资源分析；Unity 2022.3；URP 14.0.12；角色展示页渲染复现。

## 1. 工作目标与总原则

这套规则用于把客户端证据转换为可复现的 Unity 资产和渲染效果。工作顺序固定为：

1. 保全原始文件，记录来源、时间、路径和 SHA-256。
2. 先验证解包完整性，再分析资源。
3. 用 RenderDoc、对象反射和运行时采样得到可验证参数；截图只用来校准视觉差异。
4. 每次只完成一个模块：资产索引、材质、灯光、后处理、Tonemap、动画。
5. 结论必须带证据路径、对象名、参数或采样结果；无法确认的值写 `unknown` 或“近似重建”。
6. 修改 Unity 后先编译，再检查 Console，再做 Play Mode 或离屏渲染验证。
7. 未解码的压缩数据不能宣称已经还原；近似资产必须在文件名和文档中标明来源。

## 2. 证据等级

| 等级 | 证据 | 可作出的结论 |
|---|---|---|
| A | RenderDoc draw/PSO/Shader/CB/SRV、客户端对象字段、实际运行时采样 | 可直接实现和对比 |
| B | 反射探针、Unity YAML、材质/Volume 序列化字段、完整 Bundle 引用 | 可实现，需运行时验证 |
| C | 截图、命名、相邻版本资源、经验推断 | 只能作为调参起点 |
| D | 没有对象、没有采样、没有路径的猜测 | 不得写成事实 |

优先级：运行时 GPU 参数 > 资源对象参数 > 代码默认值 > 截图视觉猜测。

## 3. 原始文件和目录规则

- 原始 ZIP、RDC、Bundle、AB、CAB 不覆盖、不改名、不写回。
- 分析目录使用短路径，例如 `D:\EndfieldHandoff`，避免路径长度影响 PowerShell、Unity 和第三方工具。
- 每个分析批次建立 `manifest.json` 或 Markdown 清单，记录：绝对路径、文件大小、SHA-256、工具版本、日期、输入输出关系。
- 输出资产放到单独目录，例如：
  - `work\chen-weapon0011-ab-extraction\`
  - `decoded_animation_clips\`
  - `exported_animation_clips\`
  - Unity 工程 `Assets\EndfieldReconstruction\`

## 4. ZIP 完整性门槛

完整解包后运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\EndfieldHandoff\199 完整性校验\VERIFY ALL.PS1"
```

验收必须看到：

```text
OUTER MANIFEST BAD=0
CHEN VERIFY EXIT=0
PLAN MANIFEST BAD=0
REQUIRED MISSING=0
NESTED ZIP=0
OVERALL VERIFY=PASS
```

用户截图或 OCR 可能把 `0` 识别成 `B`，以脚本原始输出为准。任何一项失败都先修复证据链，不进入 Shader 或动画推断。

## 5. Unity Bundle / CAB 资源分析

本次角色证据目录：

```text
F:\Game\Hypergryph Launcher\work\chen-weapon0011-ab-extraction\chen_assets
```

已确认的输入 Bundle 和 CAB：

```text
Data\Bundles\Windows\main\b8c92fb90410297e1d4e12eb.ab
CAB-4f9126151750655b69df69ae88519443
```

关键规则：

- 不能只加载单个 AB 判断引用关系；这批角色资源需要 40 个 Bundle 一起解析。
- 先枚举 `GameObject`、`Transform`、`SkinnedMeshRenderer`、`Mesh`、`Material`、`Texture2D`、`Shader`、`Animator`、`Avatar`、`AnimationClip` 和 `Volume`。
- 对象 ID、文件 ID、CAB 名、Bundle 名和资源路径一起保存，不能只保留显示名称。
- 材质要同时记录 Shader 名、关键字、RenderQueue、Cull/ZWrite/Blend、纹理引用、颜色空间和每个浮点/颜色参数。
- 贴图通道必须记录实际用途：BaseColor、Normal、Mask、Roughness、Metallic、AO、Emission、Matcap、Ramp、LUT 等；不要根据文件名猜通道。

## 6. RenderDoc 截帧分析规则

截帧文件：

```text
C:\Users\admin\Documents\WXWork\1688854862767852\Cache\File\2026-09\chenqianyu.rdc
```

RenderDoc 是角色效果的 GPU 真值。分析顺序：

1. Capture API、设备、分辨率、HDR/色彩空间、MSAA、动态分辨率。
2. Frame graph、RenderPass 顺序、角色开始和结束的 draw call。
3. 每个角色 draw 的 PSO、VS/PS、Shader 反编译、关键字和输入布局。
4. 纹理 SRV/UAV、纹理格式、mip、sRGB 标志、采样器、UV 变换。
5. Constant Buffer / Push Constant 的字段、数值、更新频率和对应材质属性。
6. 深度、阴影、法线、光照、Bloom、LUT、Tonemap、AA 的前后资源。
7. 对照 Unity Frame Debugger 和 RenderDoc，再修改工程。

不要只看最终截图。最终颜色可能已经经过曝光、Bloom、LUT、Tonemap、UI 合成，直接用它反推 BaseColor 会导致材质错误。

## 7. Unity URP 14 RenderFeature 生命周期

URP 14 的硬规则：

- `renderer.cameraColorTargetHandle` 只能在 `SetupRenderPasses` 或 `ScriptableRenderPass` 范围使用。
- `AddRenderPasses` 只做条件判断和 `EnqueuePass`，不要在这里读取 camera color target。
- 在 `SetupRenderPasses` 把 `RTHandle` 传给 Pass；在 `OnCameraSetup` 分配临时 RT；在 `Execute` 做 Blit；在 `Dispose` 释放资源。
- `Blitter.BlitCameraTexture` 的 source、destination、material、shader 都要先检查非空和 `shader.isSupported`。
- 传递源 RT 的 RenderFeature 必须处理 Camera Stack、Preview Camera、SceneView 和相机销毁。
- 自定义 Tonemap 通常使用 `AfterRenderingPostProcessing`；如果在 `BeforeRenderingPostProcessing`，要确认是否会被内置 UberPost 再次处理。

典型错误与原因：

| 报错 | 原因 | 修复 |
|---|---|---|
| `You can only call cameraColorTarget...` | 在 `AddRenderPasses` 访问目标 | 移到 `SetupRenderPasses` |
| `Blitter.BlitCameraTexture` NRE | source、temporary、material 或 shader 为空 | 完整的 `IsUsable` 检查和材质创建 |
| 画面黑色 | RT 生命周期、材质不支持、曝光为零、错误颜色空间或目标未写回 | 按资源、材质、曝光、写回顺序排查 |
| 参数调节不生效 | Volume 未被相机过滤、Override 未勾选、缓存 stack 未刷新、字段未桥接 | 检查相机和 Volume，再看运行时诊断 |

## 8. Volume 和参数桥接

`ChenEvidenceVolumeComponent` 是证据容器；真正被 URP 后处理消费的是原生 `Bloom`、`Vignette`、`DepthOfField`、`ColorAdjustments`、`Tonemapping` 等组件。桥接规则：

1. 场景中的 Volume 必须启用，Profile 必须有对应组件。
2. Volume Layer Mask 必须包含相机所在层。
3. 相机的 `UniversalAdditionalCameraData.renderPostProcessing` 必须开启。
4. 每个需要生效的字段必须勾选 Override，组件本身必须 active。
5. URP 混合完成后，在 Renderer Feature 的 `AddRenderPasses` 阶段读取当前 `VolumeStack`。
6. `Via Scripting` 相机使用自己的 `volumeStack`，不能误用上一台相机的 stack。
7. Inspector 调参后刷新缓存；必要时调用 `VolumeManager.ResetMainStack()` 和相机 `UpdateVolumeStack`。
8. 用诊断菜单打印“当前 Volume 实际参数”，不要只看 asset Inspector。

本次工程关键资产：

```text
F:\UnityProject\EndGmae\Assets\EndfieldReconstruction\Rendering\Generated\ChenOverviewVolumeProfile.asset
F:\UnityProject\EndGmae\Assets\EndfieldReconstruction\Rendering\Generated\ChenUniversalRenderer.asset
F:\UnityProject\EndGmae\Assets\EndfieldReconstruction\Rendering\ChenVolumeParameterBridge.cs
```

已知验收：`ChenEvidenceVolumeComponent active: 1`；Renderer 的 Tonemap Feature 注入点应为 `600`；工程 URP 版本为 `14.0.12`。

## 9. 角色 Shader 还原方法

按材质模块逐个闭环：

- Skin / Body：BaseColor、法线、粗糙度、阴影染色、次表面近似、曝光和高光。
- Face：面部阴影、Ramp、眼睑/眉毛覆盖、脸部 UV 和透明/叠加层。
- Hair：各向异性或方向高光、Alpha Clip、发丝阴影、边缘光和深度排序。
- Eye：虹膜、巩膜、角膜层、折射/高光、透明队列和视线方向。
- Cloth / PBR：金属度、粗糙度、法线、AO、清漆或布料高光、阴影接受。
- Overlay / Shadow：角色专用覆盖层、Matcap、边缘光、轮廓和深度写入。

每个模块要完成：Shader 源码、ShaderGUI 中文参数、材质实例、对应贴图、关键字、RenderQueue、测试球或测试角色、RenderDoc/Unity 对照截图。

高光或粗糙度不明显时依次检查：

1. 贴图是否引用正确，通道是否被当作线性数据读取。
2. 粗糙度是否需要 `1 - value`，Alpha 是否存放 Mask。
3. 法线是否使用 Normal 类型导入，Y 通道方向是否一致。
4. 主光方向、强度、环境反射、曝光是否正确。
5. Shader 是否真的使用了材质属性，Runtime Binder 是否每帧覆盖了 Inspector 值。
6. Tonemap/Bloom 是否把高光压平或误合成。

## 10. 灯光和角色专用参数

角色展示页通常同时存在场景光、角色主光、环境漫反射、阴影染色、相机跟随光和后处理曝光。工程中的 `Runtime` 脚本要明确区分：

- 运行时真正传入 Shader 的参数。
- 只用于证据留档的原始字段。
- 仅作为默认值或回退值的参数。

每个灯光记录：颜色、强度、方向/位置、角度、范围、阴影、是否影响角色、作用于哪个材质模块、是否参与高光、是否受相机跟随。

## 11. Tonemap、Bloom、曝光、LUT 和 AA

### Tonemap

- 原生 URP Tonemapping 和独立 `EndfieldTonemapFeature` 必须互斥。
- 启用自定义路线时将原生 `Tonemapping.mode` 设为 `None`，否则 HDR LUT Builder 可能仍按 ACES 生成 LUT，造成双重映射。
- 自定义 Pass 的注入点、曝光值、LUT、肩部/Toe/线性段参数要在同一帧确认。

### Bloom

- 记录 threshold、intensity、scatter、downscale、maxIterations、highQualityFiltering。
- URP 14 原生 Bloom 为加法合成；客户端证据如果是其他混合模式，只能用自定义合成 Pass 复现。
- Bloom 要在正确的曝光和 Tonemap 阶段比较；直接比较最终截图容易误判强度。

### 自动曝光

- 直方图采样、EV 范围、百分位、中心权重、适应速度和固定曝光回退值分开记录。
- GPU 读回失败时使用有限、非零、可诊断的固定曝光，不能让全局曝光默认为零。
- 记录每帧曝光 multiplier，并确认它在 Bloom 和 Tonemap 前后的位置。

### LUT

- 检查 2D strip / 3D LUT 的尺寸、纹理格式、sRGB、采样坐标和 contribution。
- LUT 只作为证据确认的输入，不把普通颜色贴图误当 LUT。

### 抗锯齿

- 用 RenderDoc 的最终合成、深度、motion vector、resolve 和 shader 关键字确认 MSAA、TAA、FXAA 或 SMAA。
- 分性能档记录独立配置；不能从一个档位推断全部档位。
- 比较 AA 前后的边缘、透明发丝、眼睛高光和运动残影。

## 12. ACL 动画和 Humanoid 导入规则

已确认的三个客户端 `AnimationClip`：

```text
A_actor_chen_dialog_state_scratchface_start
A_actor_chen_dialog_state_scratchface_loop
A_actor_chen_dialog_state_scratchface_end
```

证据位置：

```text
F:\Game\Hypergryph Launcher\work\chen-weapon0011-ab-extraction\clip-reflection-probe.txt
```

反射探针结果：采样率 60；GenericBinding 854；Transform 输出轨道 349；Root 轨道 28；Float 曲线 148；ACL compressed buffer version 10。普通 rotation、position、euler、scale、float 曲线数组为空，动画数据位于 `m_AclCompressedBuffer` 和相关 muscle 数据。

因此：

- 不能把普通曲线 JSON 导入器直接用于这三个 Clip。
- 需要先解 ACL buffer，再按 binding hash/骨骼路径生成 Unity 曲线。
- 导出的 `.anim` 必须验证 clip length、sample rate、binding path、曲线数量、曲线首尾值和实际骨骼运动。
- Humanoid 支持需要把曲线绑定到当前 FBX Avatar 的骨骼路径并在 Animator 中播放实测。
- UI 预渲染 USM 视频不是骨骼动画，不能转换成 Humanoid `.anim`。
- 在 ACL 未解码前，只能称为“压缩动画证据”或“近似重建”，不能称为源模型动画还原。

## 13. 自动化验证门槛

每次代码或资产变更后执行：

1. `Assembly-CSharp.csproj` 和 `Assembly-CSharp-Editor.csproj` 编译。
2. Unity Console 无编译错误、Shader error、RenderTexture 生命周期错误。
3. Scene 相机的 Post Processing、Volume Layer、Renderer Asset 和 Feature 状态打印。
4. 离屏渲染或 Play Mode 截图；记录分辨率、HDR、颜色空间、曝光和相机 FOV。
5. 对角色材质逐项检查贴图、关键字、RenderQueue、光照参数。
6. 对动画检查曲线非空、Avatar、Animator、骨骼运动和循环接缝。
7. 需要与游戏截帧比较时，保持相同分辨率、相机、FOV、色彩空间和曝光。

本次已知编译证据：Unity 工程 `F:\UnityProject\EndGmae` 的 Assembly-CSharp 与 Editor 工程 `dotnet build --no-restore` 均为 0 错误、0 警告；这不能代替 Unity Play Mode 和 GPU 验证。

## 14. 常见问题排查矩阵

| 现象 | 首先检查 | 再检查 |
|---|---|---|
| 画面全黑 | Feature 是否启用、材质/Shader 是否支持、曝光是否为 0 | RTHandle 分配、Blit 写回、颜色空间 |
| Inspector 调参不生效 | Volume active、Profile weight、Layer Mask、Override | stack 刷新、桥接字段、Runtime Binder 覆盖 |
| 角色贴图错乱 | Mesh UV、材质槽、贴图引用、通道 | sRGB/线性、法线导入、贴图数组/Atlas |
| 金属/高光消失 | metallic/roughness 通道、反射探针、主光 | Tonemap、Bloom、曝光、Shader 关键字 |
| 视角俯视或构图不同 | Camera position/rotation/FOV/viewport | parent transform、Canvas/UI 相机、动态相机脚本 |
| Tonemap 发灰或过曝 | 是否双重 Tonemap、曝光倍率 | LUT contribution、HDR、颜色分级顺序 |
| Bloom 过强或不见 | threshold/intensity/scatter、HDR | 注入时机、内置 Bloom 是否重复、分辨率降采样 |
| 动画导入后不动 | 曲线是否为空、路径是否匹配、Animator 是否播放 | Avatar 类型、Root、Humanoid retarget、ACL 解码 |

## 15. 可复制的项目目录模板

```text
Project/
  evidence/
    input-manifest.json
    sha256.txt
    renderdoc/
    bundle-index/
    object-probes/
  tools/
    probes/
    exporters/
    version-lock.md
  reconstruction/
    shaders/
    materials/
    textures/
    lighting/
    postprocess/
    animation/
  validation/
    screenshots/
    renderdoc-comparisons/
    logs/
  docs/
    findings.md
    limitations.md
```

## 16. 最终交付清单

- [ ] 原始 ZIP/RDC/Bundle 未修改，SHA-256 已记录。
- [ ] `VERIFY ALL.PS1` 输出 `OVERALL VERIFY=PASS`。
- [ ] Bundle/CAB/对象 ID/资源引用可追溯。
- [ ] RenderDoc 关键 draw、Shader、纹理、CB、后处理顺序已保存。
- [ ] Shader、材质、Runtime Binder、RenderFeature 和 Volume 参数均有对应资产。
- [ ] URP 14 API 生命周期正确，Camera Target 不在错误回调读取。
- [ ] 自定义 Tonemap 与原生 Tonemap 互斥，曝光和 LUT 顺序明确。
- [ ] 参数调整可通过运行时诊断看到真实 stack 值。
- [ ] 动画来源、ACL 解码状态、Humanoid 验证状态明确标注。
- [ ] 编译、Console、Play Mode、离屏截图和 RenderDoc 对照均完成。
- [ ] 未知项、近似项和未验证项集中列出，不能隐藏在“已还原”描述里。

## 17. 本次任务证据索引

- RenderDoc：`C:\Users\admin\Documents\WXWork\1688854862767852\Cache\File\2026-09\chenqianyu.rdc`
- 客户端资产：`F:\Game\Hypergryph Launcher\work\chen-weapon0011-ab-extraction\chen_assets`
- 动画探针：`F:\Game\Hypergryph Launcher\work\chen-weapon0011-ab-extraction\clip-reflection-probe.txt`
- Unity 工程：`F:\UnityProject\EndGmae`
- Runtime：`Assets\EndfieldReconstruction\Runtime`
- Rendering：`Assets\EndfieldReconstruction\Rendering`
- Shader：`Assets\EndfieldReconstruction\Shaders\Runtime`
- Tonemap：`Assets\EndfieldReconstruction\Rendering\Tonemapping`

这份规则的核心是：先保全和索引证据，再实现单个模块，最后用 GPU 截帧和运行时数据验收。没有路径、对象、参数和验证记录的效果描述，只能作为假设，不能作为最终还原结论。
