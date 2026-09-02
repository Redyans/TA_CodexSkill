# Bloom Toon 算法对齐与横纵向 Blur 排查参考

> 类型：`REFERENCE`；适用范围：Unity/URP 全屏 Bloom、Bloom Toon、分离式 Gaussian Blur 和多级 Mip 合成；使用前提：必须以目标项目的 Unity/URP 版本、渲染格式、源码和代表性场景重新验证。本文件不定义某个项目的固定 Pass、路径、参数或视觉目标。

## 1. 适用场景

当需要把一个工程中的 Bloom 效果迁移到另一个工程，或出现“只有上下扩散”“左右扩散不明显”“Frame Debugger 看起来少了一个方向”等问题时，先区分三个目标：

1. **结构相似**：两边都有亮部提取、降采样、Blur 和合成。
2. **算法等价**：同一输入下，采样位置、权重、颜色域、Mip 尺寸和合成顺序一致。
3. **视觉一致**：在同一相机、曝光、颜色空间、质量和参数下，最终图像达到可接受的 A/B 差异。

“都叫 Gaussian Blur”只能证明结构相似，不能证明算法或效果一致。尤其要单独核对 Prefilter、Blur 核、RT/Mip 布局、最终 Uber 合成和参数求值时序。

## 2. 前提与依赖

### 2.1 必须冻结的输入

在修改前记录以下基线：

| 类别 | 需要冻结的事实 |
| --- | --- |
| 颜色 | Color Space、HDR/SDR 格式、Gamma/Linear 转换、RGBM 或其他编码 |
| 输入 | 原图亮部来源、Threshold、Clamp、Prefilter 采样位置和缩放 |
| 分辨率 | 首个 Bloom RT 尺寸、每级 Mip 尺寸、移动端特殊上限、RT FilterMode |
| Blur | 每级核数量、采样偏移、权重、横向/纵向方向参数、边缘 Clamp |
| 合成 | Mip 数量、UV 变换、权重、Tint、Intensity、Dirt 和最终颜色域 |
| 时序 | Pass 顺序、CommandBuffer 录制方式、全局/材质参数设置时机 |
| 运行环境 | Unity/URP 补丁版本、Renderer、相机类型、质量档、目标 API/设备 |

### 2.2 先建立算法矩阵

推荐使用以下矩阵对照源实现和目标实现：

| 阶段 | 源实现 | 目标实现 | 是否同一行为 |
| --- | --- | --- | --- |
| Prefilter | 亮部采样、阈值、Clamp、Scaler |  |  |
| 初始 Blur | 横向核/纵向核 |  |  |
| Downsample | 采样位置和缩放 |  |  |
| Mip0/Mip1/Mip2 Blur | 核和半径 |  |  |
| Mip 存储 | 独立 RT 或 Atlas |  |  |
| Combine | 权重、UV、颜色域 |  |  |
| Uber | 采样、解码、Tint、Intensity |  |  |

每一个“否”都可能改变最终画面；不要用“总体看起来差不多”替代矩阵结论。

## 3. 实现或排查步骤

### 3.1 先确认是不是正确的 Bloom 入口

从 Volume/Renderer 入口一路追到实际 Draw：

```text
Volume 参数
  → IsActive / Algorithm 分支
  → Bloom Setup
  → Prefilter Blit
  → Downsample / Blur Blit
  → Mip Combine
  → UberPost 采样
```

同时确认：

- 使用的是独立 Bloom Shader，还是复用普通 Bloom Shader 的某些 Pass；
- 实际 Material/Shader 是否是预期对象；
- Pass index/Pass Name 是否与 C# 常量一一对应；
- 其他 Bloom 算法是否被无意切换到同一分支；
- `IsActive` 是否依赖 `overrideState`、固定值、动画曲线或运行时开关。

### 3.2 分离式 Blur 必须记录每个方向的 Draw 状态

标准分离式 Blur 是两次全屏 Draw：

```text
source → horizontal temporary RT
temporary RT → vertical destination RT
```

方向应在每次 Draw 被录制时明确绑定：

```csharp
cmd.SetGlobalVector(BlurDirectionId, new Vector4(1f, 0f, 0f, 0f));
Blit(cmd, source, temporary, material, blurPass);

cmd.SetGlobalVector(BlurDirectionId, new Vector4(0f, 1f, 0f, 0f));
Blit(cmd, temporary, destination, material, blurPass);
```

若先用 `Material.SetVector` 设置横向，再设置纵向，随后才执行 CommandBuffer，具体行为取决于渲染 API 和 Blit 封装；常见结果是多个已排队 Draw 观察到最后一次纵向值。需要保证“参数和 Draw 同时进入命令流”，或使用每个 Draw 独立的 MaterialPropertyBlock/Material 实例。

### 3.3 Gaussian 核迁移不要只复制“点数名称”

迁移 Blur 时至少同时复制四项：

1. 采样偏移（包括成对采样和小数偏移）；
2. 每个采样的权重；
3. 采样方向对应的 TexelSize 分量；
4. 边缘 Clamp 和颜色解码/编码方式。

如果源实现使用不同级别的 `6pt / 9pt / 16pt / 20pt` 核，目标实现不能用一个固定 9-tap 核仅改变半径来声称等价。核数量、长尾采样和权重分布会改变光晕半径、中心亮度与边缘衰减。

对于成对采样优化，应保留源算法的实际采样位置，而不是按照函数名重新推导对称位置。某些预计算核使用非整数偏移，是为了利用双线性采样一次读取两个相邻权重；改成整数采样会改变等效核。

### 3.4 Downsample 和 Mip 布局要按“逻辑图像”对齐

源实现可以把多个 Mip 打包到一个垂直 Atlas，目标实现也可以使用多个独立 RT。两者只有在以下条件均满足时才可能视觉等价：

- 每个逻辑 Mip 的宽高一致；
- 每个 Mip 的采样 UV 映射到相同的有效区域；
- Downsample 采样位置和解码方式一致；
- Combine 使用同样的 Mip 权重和滤波方式；
- Atlas 边界没有把相邻区域采样进来。

建议先绘制每个逻辑 Mip 的独立调试图，再验证最终合成；不要直接比较 Atlas 纹理的整张像素图。

### 3.5 用 Frame Debugger 区分 Blur 和 Combine

选中 Draw 时先看其输入纹理和 Shader Pass：

| Draw 类型 | 应该看到的证据 |
| --- | --- |
| Prefilter | 原始 `_BlitTexture`，输出首个 Bloom RT |
| 横向 Blur | `_BlitTexture_TexelSize.x` 生效，方向为 `(1,0)`，输出已出现左右扩散 |
| 纵向 Blur | 方向为 `(0,1)`，在已有结果上增加上下扩散 |
| Downsample | 四点/旋转采样或源实现规定的降采样输入 |
| Combine | 同时绑定多个 Mip；通常不读取 BlurDirection |
| Uber | 绑定最终 Bloom 纹理，并执行 Intensity/Tint/解码 |

如果选中的 Draw 同时绑定多个 Mip，它通常是 Combine，不能用它判断横向 Blur 是否执行。材质面板中残留的方向值也不能代替 Draw 级证据。

### 3.6 通过孤立亮点做方向性验证

真实场景中横向扩散可能被长条形高光、几何轮廓或曝光掩盖。建立最小 Harness：

```text
黑色背景
孤立的高亮小点或小方块
固定曝光、相机和分辨率
关闭其他后处理
```

分别保存：

1. Prefilter 输出；
2. 首次横向输出；
3. 首次纵向输出；
4. 每个逻辑 Mip；
5. 最终 Combine；
6. Uber 后画面。

首个横向输出必须在亮点两侧出现对称能量；若没有，优先查方向参数、TexelSize、输入/输出 RT 和 Pass index。若中间结果正确而最终画面不正确，再查 Combine、Uber、颜色编码和后续后处理。

### 3.7 用数值和图像两套证据收口

静态检查确认：

- Pass index 与 Shader Pass 顺序；
- 采样常量和权重没有截断/错位；
- `Texture/Sampler`、HDR Encode/Decode、颜色空间宏一致；
- RT 尺寸、FilterMode、WrapMode 和临时资源生命周期；
- 每个方向参数在正确 Draw 前设置。

运行时检查确认：

- Frame Debugger/RenderDoc 的 Draw 顺序和输入输出；
- 固定输入下的 A/B 截图和差异图；
- 中心峰值、半高宽、左右/上下能量和总能量；
- 移动端/低配格式、相机分辨率变化和多相机边界；
- 额外 Draw、临时 RT 内存、采样数和 GPU 时间。

## 4. 风险与不适用边界

### 4.1 不能直接下结论的情况

- 只看最终 Combine Draw；
- 只比较 Shader 名称或函数名称；
- 只看到“有横向/纵向两个函数”但没有确认参数进入 Draw；
- 只在一个真实场景中凭肉眼比较；
- 忽略 Color Space、HDR 格式、曝光、Tonemapping 或后续后处理；
- 将动画阈值、运行时亮度、移动端分辨率策略当作静态参数。

### 4.2 常见迁移风险

| 风险 | 表现 | 处理建议 |
| --- | --- | --- |
| 参数状态未录制 | 所有 Blur 看起来只有一个方向 | 使用 CommandBuffer/PropertyBlock 绑定并检查每个 Draw |
| 核被统一替换 | 光晕中心或半径不一致 | 逐级保留源采样偏移和权重 |
| Atlas UV 错位 | Mip 串区、边缘出现条纹 | 先做逻辑 Mip 调试，再验证 UV 变换 |
| Gamma/HDR 不一致 | 亮度、拖尾和边缘衰减偏差 | 对齐 Encode/Decode、RT 格式和颜色域 |
| 阈值时序不一致 | 某时段亮、某时段不亮 | 固定时间或移植同一曲线求值来源 |
| 只改 Shader 不改入口 | 新 Pass 从未执行 | 同步 C# Pass 常量、Material 和资源绑定 |
| 只验证 Editor | 真机出现带宽或格式问题 | 覆盖目标 Renderer、API、设备和质量档 |

### 4.3 性能边界

不同 Gaussian 核可能显著增加采样数。增加 Mip、扩大长尾或保留 Atlas 合成通常会同时影响：

- 全屏纹理采样和带宽；
- 临时 RT 内存和生命周期；
- CommandBuffer Draw 数量；
- 移动端 Tile/Resolve 成本；
- Shader 变体和低配设备编译时间。

先满足效果契约，再在固定场景和目标设备上测量；不要用“采样少所以一定更快”替代 Profiling。

## 5. 验证与回退

### 5.1 最小充分验证矩阵

| 层级 | 通过条件 |
| --- | --- |
| 文本/静态 | UTF-8、函数/变量/Pass 引用完整，采样常量与源实现可逐项对应 |
| C# 编译 | 目标 Runtime/Editor 程序集无新增编译错误 |
| Shader 导入 | Unity 导入没有本次修改引入的 Shader/Include/变体错误 |
| 运行时结构 | Frame Debugger 中 Prefilter、横向、纵向、Mip、Combine 顺序正确 |
| 视觉 A/B | 固定输入、参数、曝光和时间下，方向性、中心峰值、半高宽和差异图达到目标 |
| 平台/性能 | 目标 Renderer、分辨率、API/设备和质量档无不可接受回归 |

### 5.2 回退策略

如果视觉或平台验证失败，优先按职责单元回退：

1. 先回退新 Blur 核，保留已确认的方向录制修复；
2. 再回退 Mip/RT 布局；
3. 最后回退入口或 Uber 合成路径。

每次回退都重新记录 Frame Debugger 和 A/B 结果。不要在无法解释的情况下同时替换 Prefilter、Blur、RT 和 Uber，否则无法定位真正原因。

### 5.3 交付报告必须说明

- 源实现与目标实现的版本、路径和实际入口；
- 已对齐和仍不等价的算法单元；
- 横向/纵向 Draw 的证据；
- C#、Shader、Unity 导入、视觉 A/B、设备和性能验证结果；
- 未覆盖的平台、格式、相机、质量档和剩余风险；
- 明确的回退点和下一步复验方式。
