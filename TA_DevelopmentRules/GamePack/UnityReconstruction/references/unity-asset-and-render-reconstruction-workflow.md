# Unity 资源与抓帧渲染复刻工作流

## 适用场景

已获授权的本地 Unity 客户端可以读取资源包，且有目标效果截图、RenderDoc 抓帧或二者之一；目标是在另一 Unity 版本/管线中形成可编辑、可播放的场景。规范边界见 [UCR-EVD-01、UCR-EVD-02、UCR-AST-01](../README_Tech_UnityCaptureReconstructionRules.md)。本页是操作参考；每项参数仍须由当前项目证据确定。

首个角色贯通后，按[批量角色制作与光照验收参考](batch-character-reconstruction-playbook.md) 复用公共实现并逐角色处理差异；该页集中说明目录边界、实际材质槽、采样/细分插值、独立复拍和近似方案。实际案例及后续状态见[依盖之书批量 Profile](../Profiles/BookOfYog/batch-reconstruction-lessons-and-status.md)。

## 前提与依赖

| 工具/入口 | 用途 | 选择和核对点 |
| --- | --- | --- |
| RenderDoc Python Replay API | 枚举 draw/dispatch、导出 shader、常量、资源和像素原始字节 | 记录捕获版本、事件号、资源在事件时的内容与视图格式；自动化可离屏运行。 |
| UnityPy | UnityFS、对象索引、依赖、原序列化字段 | 对压缩/运行期对象不要假定一次通用导出能保真。 |
| Unity 原生 AssetBundle/Animation API | Mesh、Avatar、Prefab、AnimationClip 的 Unity 语义导入 | 仍要检查持久化后的资产与骨架绑定。 |
| 3Dmigoto cmd_Decompiler、DXBC 反汇编/编译器 | 将实际 VS/PS 指令映射到 HLSL/URP | 自动反编译只是草稿；以指令、寄存器和 GPU 输出校验。 |
| Il2CppDumper、定向反汇编/仿真 | 运行时网格变更、动态骨骼或相机脚本的必要分支 | 只分析当前效果实际调用的代码，标记机器码、stub 和 Unity 行为边界。 |
| 目标 Unity Editor + D3D11 GPU 回读 | 资产持久化、Shader 编译、Pass 输出与播放 | 离线 fxc/Roslyn 结果只是前置检查。 |

先记录游戏和目标 Unity 版本、URP/PPv2 版本、图形 API、色彩空间、分辨率、capture event、源图与 RDC 是否同一动作时刻。工具版本写入项目 Profile，不写入通用规则。

## 实现或排查步骤

### 1. 建立输入清单和角色依赖闭包

1. 只读枚举客户端的 bundle 清单、container、PathID、对象类型和 bundle 依赖。先找展示页 Prefab/Animator，再沿 Mesh、Avatar、Material、Texture、AnimationClip、Controller 与 override 引用递归追踪。
2. 给源 bundle、RDC、截图和抽取副本保存长度、SHA-256、对象路径与 source ID。记录重复对象和版本差异，不能按文件名相似度直接选角色。
3. 确认实际界面状态引用的 clip 与“同一角色所有可替换动作”的集合。多个 UI 状态可指向同一个 override clip；两者分别列清单。
4. 将提取脚本、源副本、依赖索引与导入 manifest 放在单一工程根下，避免每个角色重复复制共用渲染管线和后处理。

建议的资产边界（目录名可按项目调整）：

```text
Assets/<Reconstruction>/
  Characters/<Character>/Animations/  # 角色专属、可替换 clip
  Characters/<Character>/Models/      # 模型/骨架/预制体
  Environments/<Scene>/               # 背景、地面、特效与场景配置
  Shaders/ Runtime/ Settings/         # 多角色共享实现
  Evidence/ Validation/               # 来源清单与分层报告
  Tools~/                             # 导出脚本、工作副本、临时运行环境
```

### 2. 恢复模型、顶点通道和贴图

1. 用原生 AssetBundle 加载器读取 Mesh、SkinnedMeshRenderer、bindpose、bone 名/顺序、子网格、UV0..n、顶点色和 bounds。对照 RenderDoc 实际 draw 的 IA 输入和顶点输出，检查运行时新增或改写的通道。
2. 对原纹理同时保留“源资产预览”和“GPU 原格式数据”。PNG 适合查看，不代表 BC 压缩块、mip、cube face、sRGB view、采样器和法线通道编码仍相同。
3. 目标 GPU 格式可用时，从原始 raw/块数据构建 native texture；逐 mip、face、slice 回读并按 SHA-256 校验。显式记录 BC1/BC3/BC6H、linear/sRGB、wrap、filter、LOD 与 swizzle。
4. 材质资产与原 GPU draw 建立一对一 binding 清单。对于未命名常量，把 float 位模式和对应指令使用位置保留；不要凭数值范围猜它是颜色、曝光或矩阵。

### 3. 恢复动画和运行时姿态

1. 先查看 clip 的 editor curves 与 runtime stream 是否为空；再验证 `Instantiate` + 保存后的 `.anim` 时长、曲线、key 和实际采样位移。空 editor curves 不等于源动作没有内容。
2. 对 StreamedClip/压缩曲线，按原系数和时间生成 Hermite key：保留原 value、时间和入/出切线，不强制按 30/60 fps 重采样。Avatar/TOS 的 binding hash 解析为完整 Transform 路径，未映射 hash 单独保存。
3. 分开构建身体和武器 Animator/Controller，记录各自动作相位与循环标志。固定抓帧姿态用于隔离渲染误差；原 clip 播放用于证明场景能动画更新。
4. 对 DynamicBone、布料或 CameraDriven 行为，先找源组件参数及实际执行分支，再用机器码/运行时证据还原。单帧抓帧不提供捕获前的速度、上帧矩阵和相机路径。

### 4. 用 RenderDoc 还原实际渲染链

1. 先导出事件顺序表：阴影 caster、深度预通道、不透明、描边、AO、反射、透明/粒子、TAA、Bloom、调色、UI。记录每个 Pass 的输入/输出 Resource ID、当时的内容哈希、RT 格式/尺寸和 Blend/Depth/Cull/Stencil 状态。
2. 对目标 draw 保存 VS/PS DXBC、反汇编、输入布局、常量缓冲、SRV、sampler 与实际输出。HLSL 从运算和数据流恢复；自动反编译的向量分量、类型、饱和、浮点顺序逐项核对。先编译，再用同输入 GPU 结果确认。
3. 相机位置、VP、主光、点光、SH、反射 probe 均从被实际指令读取的槽定位。同一常量缓冲可能同时保留主相机和反射相机矩阵，静态字段也可能与本帧实际 draw 不一致。
4. 阴影要分离 caster 几何/深度偏移、shadowmap、比较采样与屏幕 resolve。反射要分离镜像 View、斜截投影、反向剔除、反射专用阴影与 RT sRGB 写入。AO 要核对 depth pyramid、atlas 层、上采样与合成时序。
5. 后处理按前后缓冲追踪曝光、TAA history/motion/depth、Bloom 每层、LUT 的输入域和输出域、暗角/dither、最终颜色转换。已有 LUT 承担 tone mapping 时，不能再叠一层 URP 默认 tonemapper。
6. 粒子抓帧网格和 Shader 时间动画只能说明当前帧外观及已恢复的局部变化；完整 emitter 生命周期需另取原配置及跨帧证据。

### 5. 适配目标 Unity/URP

1. 保存 `ProjectVersion.txt`、`Packages/manifest.json`/lock、Graphics/Quality、RendererData 与场景引用的修改前副本。
2. 先用项目 Shader、RendererFeature 或 CameraData 接口实现有证据要求的行为；若必须改 URP 内部投影/运动向量，锁定版本、集中一份源码、记录最小 patch 和重建入口。
3. 使用 Unity 认可的包入口，并检查实际解析来源、版本和程序集引用。修改渲染管线资产时，确保 Graphics 与实际质量档均指向本地 Renderer/Pipeline，避免“场景看似正确但运行时读另一份配置”。
4. 每个相机单独管理 history 和临时 RT；明确生产/消费时序与释放。对深度比较纹理检查 RT 实际 `shadowSamplingMode`，不能只看 descriptor 值。

### 6. 分层验收

先做资源字节/曲线/路径/几何验证，再做原输入的独立 Pass GPU 回归，然后做实时阴影、反射、AO、HDR、最终图和 PlayMode。比较图必须固定相机、姿态、内部/输出分辨率、色彩传递函数、后处理开关及同一 frame event。对人物可靠不透明内区、透明边缘和背景分开统计；旧报告保留，新测量带新前缀。

## 风险与不适用边界

- 只有 JPG 时可分析风格、构图和可见效果，无法恢复不可见纹理、Shader 分支或动态历史。
- 只有单帧 RDC 时可精确定位当帧实际路径，但不能证明其他姿态、相机和粒子生命周期。
- DXBC 转 HLSL 的表达式等价不自动保证 GPU bit 相同；采样、half/float、导数、插值和运算顺序都可能改变结果。
- 原 Unity 版本与目标 URP 不同，序列化、贴图格式、动画运行时数据、投影/深度方向和后处理输入域需分别确认。
- 加密专用容器需读对应专项规则；本页的标准 UnityFS 路径不包含解密步骤。

## 验证与回退

用 [验证与故障排查](renderdoc-validation-and-troubleshooting.md) 的矩阵记录每层输出。回退时恢复包/管线配置备份和原 GUID，保留证据报告及失败日志；不要以删除历史报告或重写源输入来消除差异。实际项目的入口、命令和当前指标见 [BookOfYog Profile](../Profiles/BookOfYog/linglong-showcase-reconstruction.md)。
