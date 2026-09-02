# ProjectACG Bloom Toon URP14 移植 Profile

> **Profile ID**：`projectacg-bloom-toon-urp14-port-v1`
>
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACGMain2\ProjectACG\Client`
>
> **事实快照**：`2026-09-02`
>
> **用途**：记录 ProjectACG 当前将参考工程 Bloom Toon 算法合并到 URP14 `Bloom` 入口的实现方式、已解决的横纵向 Blur 问题、参数与资源边界、验证证据和未闭环项。本文是项目 `PROFILE`，不得上升为跨项目 CORE。通用迁移与排查方法见 [`../../references/bloom-toon-parity-and-debugging.md`](../../references/bloom-toon-parity-and-debugging.md)。

## 1. 项目事实与参考基线

### 1.1 当前工程

| 项目 | 当前事实 |
| --- | --- |
| Unity | `2022.3.62f3` |
| Color Space | `Linear`，`ProjectSettings/ProjectSettings.asset` 的 `m_ActiveColorSpace: 1` |
| URP | embedded `14.0.12` |
| Bloom 入口 | `Bloom.algorithm` 的 `BloomAlgorithm.BloomToon`，当前序列化值为 `4` |
| C# 实现 | `Packages/com.unity.render-pipelines.universal@14.0.12/Runtime/Passes/PostProcessPass.cs` 的 `SetupBloomToon` / `SetupStandaloneBloomPyramid` |
| Shader 实现 | `Packages/com.unity.render-pipelines.universal@14.0.12/Shaders/PostProcessing/Bloom.shader` |
| Volume 参数 | `bloomToonThreshold`、`bloomToonScaler`、`bloomToonIntensity` |
| 当前默认值 | Threshold `0.7`、Scaler `2`、Intensity `0.75`；多数现有 Profile 中这些参数的 `m_OverrideState` 为 `0` |
| 参考图使用值 | 对比 Profile 中常见 Intensity 为 `0.21`，Threshold 约 `0.6956` 或 `0.85`，Scaler 为 `2` |

当前 `Bloom.IsActive()` 对 `BloomAlgorithm.BloomToon` 只判断 `bloomToonIntensity.value > 0`，不要求该参数的 `overrideState`；参考工程的独立 `BloomToon.IsActive()` 同时要求 `intensity.overrideState`。因此在当前工程中，不能仅凭 Inspector 的 Override 勾选状态判断 Toon 是否执行，必须结合 Volume Stack 的最终值、算法枚举和实际 Frame Debugger Draw 判断。

### 1.2 参考工程

本次对齐依据：

```text
F:\UnityProject\Game（志文版）\Game\Packages\local\com.unity.render-pipelines.universal@12.1.6
```

主要事实来源：

- `Runtime/Overrides/BloomToon.cs`：独立 `BloomToon` Volume，Threshold 为 `AnimationCurveParameter`；
- `Runtime/Passes/PostProcessPass.cs` 的 `SetupBloomToon`：四分之一分辨率首层、移动端高度上限、横向/纵向 Blur、垂直 Atlas 和合成；
- `Shaders/PostProcessing/BloomToon.shader`：旋转四点 Prefilter/Down、`GaussBlur6pt`、`GaussBlur9pt`、`GaussBlur16pt`、`GaussBlur20pt` 和 Combine；
- 对比 Profile `F:\UnityProject\Game（志文版）\Game\Assets\G_Artist\Terrain\PostProcessing\URP_Post Profile.asset`：Bloom Toon Threshold 曲线、Scaler `2`、Intensity `0.21`。

## 2. 当前合并后的执行链

当前没有新增独立 `BloomToon` Volume 或独立 Material，而是保留 URP14 的 Bloom 入口：

```text
BloomAlgorithm.BloomToon
  → SetupBloomToon
  → SetupStandaloneBloomPyramid(..., useBloomToonReferencePipeline: true)
  → Bloom Toon Prefilter
  → 初始 6pt Gaussian：横向 → 纵向
  → 三次旋转四点 Downsample
  → Mip0 9pt：横向 → 纵向
  → Mip1 16pt：横向 → 纵向
  → Mip2 20pt：横向 → 纵向
  → 独立 Mip Combine
  → URP14 普通 Bloom Uber 合成
```

### 2.1 C# Pass 映射

URP14 原有 Pass `0–18` 保持不动，新增映射如下：

| C# 常量 | Shader Pass | 参考职责 |
| --- | --- | --- |
| `k_BloomToonPrefilterPass = 18` | `Bloom Toon Prefilter` | 四点旋转亮部提取 |
| `k_BloomToonDownsamplePass = 19` | `Bloom Toon Downsample` | 四点旋转降采样 |
| `k_BloomToonBlur6Pass = 20` | `Bloom Toon Gauss Blur 6pt` | 首层 Blur |
| `k_BloomToonBlur9Pass = 21` | `Bloom Toon Gauss Blur 9pt` | Mip0 Blur |
| `k_BloomToonBlur16Pass = 22` | `Bloom Toon Gauss Blur 16pt` | Mip1 Blur |
| `k_BloomToonBlur20Pass = 23` | `Bloom Toon Gauss Blur 20pt` | Mip2 Blur |

维护 Pass 时，必须同步 C# 常量、Shader Pass 顺序和 Frame Debugger 的实际 Pass Name。不要在其他算法前面插入 Pass 而不重新核对全部 index。

### 2.2 参考采样核的移植方式

`Bloom.shader` 新增了四个 Toon 专用 Blur 函数：

```text
BloomToonGaussBlur6
BloomToonGaussBlur9
BloomToonGaussBlur16
BloomToonGaussBlur20
```

每个函数保留参考工程的采样偏移和权重，并通过：

```hlsl
_BlitTexture_TexelSize.xy * _StandaloneBloomBlurDirection.xy
```

将同一核分别应用于横向和纵向。`SampleBloomToon` 对采样坐标执行边缘 Clamp，避免长尾采样越界。

当前工程处于 Linear 色彩空间。非 RGBM 路径下，当前 `EncodeHDR`/`DecodeHDR` 对 Bloom 颜色是恒等行为；若平台触发 URP 的 RGBM fallback，不得直接声称与参考工程逐像素等价，必须重新做设备验证。

### 2.3 分辨率和 RT 组织

当前 Bloom Toon 首层按参考策略从相机尺寸先取半分辨率，再右移一次得到四分之一分辨率；移动端在相机高度不小于 `810` 时沿用参考的 `748 / 2` 高度上限和等比宽度策略。

当前使用 URP14 的独立 RT：

| 逻辑层 | 当前 RT | 典型 `1920×1080` 尺寸 |
| --- | --- | --- |
| 首层 Prefilter/Blur | `m_BloomMipDown[0]` | `480×270` |
| Mip0 | `m_BloomMipDown[1]` | `240×135` |
| Mip1 | `m_BloomMipDown[2]` | `120×67` 或按描述符取整 |
| Mip2 | `m_BloomMipDown[3]` | `60×33` 或按描述符取整 |

参考工程把三个逻辑 Mip 打包到垂直 Atlas；当前独立 RT 只要尺寸、采样区域、Blur 和权重一致，逻辑效果可以接近，但纹理布局本身不是同一实现。若要求严格像素级复刻，需进一步移植 Atlas UV 组织并重新验证边界。

## 3. 已解决的问题与根因

### PACG-BLOOM-01｜CommandBuffer 中横向方向被纵向值覆盖

**现象**：画面看起来主要是上下扩散，误以为当前 Bloom 没有左右 Blur；Frame Debugger 选中的 Draw 同时绑定多个 Mip。

**根因**：旧 `ExecuteStandaloneSeparableBlur` 先调用 `Material.SetVector((1,0))` 录制横向 Blit，再调用 `Material.SetVector((0,1))` 录制纵向 Blit。由于参数设置不属于 CommandBuffer 的 Draw 状态，执行时可能让两个排队 Draw 都读取最后一次纵向值。

**修正**：改用命令流参数：

```csharp
cmd.SetGlobalVector(ShaderConstants._StandaloneBloomBlurDirection, new Vector4(1f, 0f, 0f, 0f));
Blitter.BlitCameraTexture(...);
cmd.SetGlobalVector(ShaderConstants._StandaloneBloomBlurDirection, new Vector4(0f, 1f, 0f, 0f));
Blitter.BlitCameraTexture(...);
```

该修正同时作用于首层和三个 Mip 的横/纵向 Blur。维护时不得改回只依赖 `Material.SetVector` 的写法。

### PACG-BLOOM-02｜只替换 Prefilter 不能达到参考效果

**现象**：当前工程虽然已有旋转四点 Toon Prefilter，但光晕形状、半径和亮度分布仍与参考工程不同。

**根因**：当前原实现复用了 Standalone Bloom 的固定 9-tap Gaussian、独立 Mip 流程和普通 Bloom 合成；参考实现使用不同级别的 `6pt / 9pt / 16pt / 20pt` 核、旋转四点 Downsample 和 Atlas 合成。

**修正**：新增 Toon 专用 Downsample 和四级 Gaussian Pass，只在 `useBloomToonReferencePipeline: true` 时使用；Standalone Danbaidong Bloom 继续使用原有 Pass，避免无关算法回归。

### PACG-BLOOM-03｜Combine Draw 不能证明 Blur 方向

**现象**：选中最终 Draw 后看到 `_BlitTexture` 和多个 `_StandaloneBloomMip*`，但看不到预期的横向效果。

**根因**：最终 `FragStandaloneCombine` 只按权重采样并合成 Mip，不读取 `_StandaloneBloomBlurDirection`；材质面板显示的是上一个 Blur 设置留下的值。

**修正**：方向性验证必须选中实际横向/纵向 Blur Draw，并查看中间 RT；推荐使用黑底孤立高亮小点做最小 Harness。

### PACG-BLOOM-04｜`overrideState` 语义与参考入口不同

**现象**：某些当前工程 Volume 资产将 `algorithm` Override 设为 `4`，但 `bloomToonIntensity`、Threshold 和 Scaler 的 `m_OverrideState` 保持 `0`；Bloom Toon 仍可能使用组件/堆栈中的非零默认值执行。参考工程则要求独立 `BloomToon.intensity.overrideState` 为 `true` 才激活。

**根因**：URP14 合并入口复用了 `Bloom` 组件，`Bloom.IsActive()` 的 Toon 分支只检查最终 `value`，没有复刻参考独立组件的 Override 门槛。这是参数激活契约差异，不是 Blur 核问题。

**当前决策**：本次只移植 Bloom 算法，不擅自改变已有 Volume 激活语义，以免影响现有场景；制作对比图或验收时，显式 Override `bloomToonThreshold`、`bloomToonScaler` 和 `bloomToonIntensity`，并记录 Volume Stack 最终值。如果项目要求“未勾选 Intensity Override 就绝不执行”，应单独把 `IsActive()` 改为 `bloomToonIntensity.overrideState && bloomToonIntensity.value > 0f`，并回归全部 Bloom 算法和现有 Profile。

**验证边界**：当前未对全部 Volume、相机堆叠和运行时 Volume 混合做激活矩阵验证；不能把 `m_OverrideState: 0` 直接解释为“Bloom Toon 一定关闭”。

## 4. 仍然不完全等价的边界

当前移植的目标是“在 URP14 Bloom 入口中复刻参考算法和效果”，不是宣称两个版本的所有实现细节逐字相同：

1. **Volume 参数不同**：参考使用带 TOD 求值的 `AnimationCurveParameter`；当前使用固定 `MinFloatParameter bloomToonThreshold`。当前工程没有确认等价的 TOD 曲线来源，因此保留固定参数。
2. **Mip 存储不同**：参考为垂直 Atlas，当前为独立 `RTHandle`。逻辑 Mip 和合成权重已对齐，纹理布局不同。
3. **最终 Uber 路径不同**：参考使用独立 `_BLOOM_TOON` 分支；当前通过 URP14 普通 Bloom Uber 路径叠加最终 Bloom。当前 Bloom Toon 使用白色 Tint 和关闭 Dirt，视觉目标接近，但不是同一 Shader 分支。
4. **颜色编码边界不同**：参考 Toon Shader 未启用 RGBM 变体；当前复用 URP14 `EncodeHDR`/`DecodeHDR`，在 RGBM fallback 平台需单独验证。
5. **采样实现组织不同**：当前使用统一 `SampleBloomToon` 和独立 Fullscreen Pass，参考使用带 UV Clamp/Atlas 变换的预计算顶点结构；采样常量和逻辑方向已对齐，但指令级实现不相同。

这些差异必须在视觉 A/B 报告中明确，不能只写“算法一致”。

## 5. 验证证据与未闭环项

### 5.1 已完成

- `Unity.RenderPipelines.Universal.Runtime.csproj` 编译通过：`0 Warning / 0 Error`。
- `Bloom.shader` Pass 索引静态核对为 `0–23`，C# 常量与 Shader Pass 顺序一致。
- 当前 Unity Editor 已重新导入 `Bloom.shader`；Editor 日志未发现本次新增 Bloom Pass 的 Shader 编译错误。
- 两个修改文件已通过 `git diff --check`。
- 静态代码确认每个 Blur 阶段都录制 `(1,0)` 横向 Draw 和 `(0,1)` 纵向 Draw。

### 5.2 未完成

- 尚未在 Unity Frame Debugger 中逐个保存横向/纵向中间 RT 并与参考工程截图做同输入 A/B。
- 尚未在实际移动设备或 RGBM fallback 格式验证采样、带宽和视觉差异。
- 尚未确认 ProjectACG 是否需要把参考的 TOD Threshold 曲线接入当前 Bloom 参数。
- 尚未证明独立 RT 与参考 Atlas 在所有相机宽高、边缘 Clamp 和动态分辨率下达到像素级一致。

### 5.3 推荐复验顺序

1. 在一个 Bloom Toon Volume 中显式 Override `algorithm = 4`、Threshold、Scaler 和 Intensity，避免当前参数 `m_OverrideState = 0` 造成误判。
2. 使用黑底孤立亮点，抓取 Prefilter、首层横向、首层纵向、三个 Mip 的横/纵向和 Combine。
3. 检查横向 Draw 的 `_StandaloneBloomBlurDirection` 为 `(1,0,0,0)`，并确认输出左右两侧已有能量。
4. 使用参考工程相同时段的 Threshold 和相同 Intensity，做最终 Uber 后 A/B 截图和差异图。
5. 在目标 Renderer、分辨率、质量档和移动端格式上测量 Draw、临时 RT、采样和 GPU 时间。

## 6. 维护和回退

### 6.1 维护规则

- 修改 `Bloom.shader` Pass 顺序时，必须同步 `PostProcessPass.cs` 常量和 Frame Debugger 映射。
- 修改 Gaussian 偏移/权重时，必须同时更新参考对照表，并重新做孤立亮点方向性验证。
- 不要把参考工程的 `BloomToon` 独立 Volume 类型、TOD 依赖或 Atlas 资源假设写入 URP14 CORE；这些只属于本 Profile 或后续明确的功能需求。
- 不要为修 Bloom Toon 顺手改变 Standalone Danbaidong、Beautify 或 Unity 默认 Bloom 的路径；当前 C# 通过 `useBloomToonReferencePipeline` 保持其原实现。
- 不要删除 URP14 的普通 Bloom Pass；其他算法仍使用原有 Pass index。

### 6.2 回退点

按影响最小原则回退：

1. 保留 `cmd.SetGlobalVector` 的方向修正，仅回退 Toon 专用核到原 Standalone 9-tap；
2. 再回退 Toon 专用 Downsample/分辨率策略；
3. 最后才回退 `SetupStandaloneBloomPyramid` 的 Toon 分支。

回退后必须重新检查 Bloom Toon 的横向/纵向 Draw，不能以 C# 编译通过代替视觉验证。
