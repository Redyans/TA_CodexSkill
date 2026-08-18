# 2D 条带式 3D 颜色 LUT 的生成、采样与调试参考

> **类型**：`REFERENCE`
>
> **适用范围**：Unity/URP 材质中的 `RGB → RGB` 颜色映射、`N³` 色立方体打包为 `N² × N` 2D 纹理、手动第三轴插值、LUT 生成工具、肤色/暗部颜色分级和切片条带排查。
>
> **使用前提**：必须以目标 Shader 的输入颜色域、通道顺序、LUT 排列、TextureImporter、最终合成顺序和目标设备重新验证。本文不替代具体功能旁的 Art / Tech README，也不规定所有 LUT 都必须使用同一种 sRGB/Linear 契约。

## 1. 适用场景与停止条件

本文用于解决以下问题：

- 需要从 Identity 色立方体生成可调色的 2D LUT；
- 需要理解 `lut0`、`lut1`、`sliceIndex`、`lutLerp` 的职责；
- `return lutLerp.xxx` 后模型出现一圈圈黑白条带；
- 最终 `lerp(lut0, lut1, lutLerp)` 仍出现切片边界、色阶或接缝；
- LUT、阴影色、Ramp 都能给暗部染色，需要划清职责；
- 工具预览中的“输入色 → LUT 输出色”被误解成最终屏幕颜色；
- Identity LUT 改变了颜色，或平台导入后与 Editor 预览不一致。

停止条件：当 Identity 在目标 Shader 中通过、最终输出连续、输入/存储/采样颜色域已闭合、Importer 回读满足契约，并且 LUT 与阴影/Ramp 的职责已经由代表性材质 A/B 证明时，应停止继续用视觉参数补偿实现问题。若差异已定位到项目专用合成、历史坐标或平台压缩，应转入对应项目 `PROFILE`，不要把校准值写成通用默认。

## 2. 先划分颜色映射与光照染色

### 2.1 LUT 改写颜色，阴影色改写光照

典型暗部合成可抽象为：

```text
darkAlbedo = LUT(baseColor) * darkStrength
shadowLight = lerp(shadowTint, mainLightColor, lightArea)
finalDark ≈ darkAlbedo * shadowLight * rampColor
```

三类控制虽然都能改变暗部观感，但输入和职责不同：

| 控制 | 主要输入 | 能力 | 不适合承担 |
| --- | --- | --- | --- |
| 颜色 LUT | 基础颜色 RGB | 非线性、分颜色区域的 `RGB → RGB` 映射 | 决定阴影几何、场景遮挡或 Ramp 分界 |
| 阴影色 / Shadow Tint | AO、Scene Shadow、`N·L` 等得到的暗区权重 | 用固定颜色统一染暗区光照 | 针对不同基础肤色分别映射 |
| Ramp | 明暗位置、风格化分界 | 控制过渡形状并可附带风格色 | 替代完整颜色分级或输入色适配 |

单个固定基础色下，阴影色可以近似 LUT。设 LUT 映射为 `F(C)`，只要目标颜色附近满足：

```text
F(C) ≈ C ⊙ K
```

固定乘色 `K` 就能近似替代。若不同输入色需要不同的色相、饱和度、Gamma、对比度或分通道响应，则不存在一组固定 `K` 同时覆盖全部颜色，LUT 才有明确价值。

### 2.2 避免三套强染色相乘

LUT、阴影色和有色 Ramp 同时强烈偏暖或偏红时，最终乘法容易造成：

- 深色区域某个通道提前裁剪到 `0`；
- 暗部发脏、过红或饱和度失控；
- 浅色肤色看似正确，深色肤色失去层次；
- 美术通过反向调整灯色掩盖材质映射问题。

建议先指定唯一的主要调色层。LUT 承担输入色适配时，先把阴影色和 Ramp 恢复中性；只需统一冷暖时，优先验证固定阴影色是否已经足够。

## 3. `N³` 色立方体的 2D 条带布局

一种常见布局是把 B 作为切片轴，按水平方向排列：

```text
N      = 每轴采样数
Width  = N * N
Height = N

x = blueIndex * N + redIndex
y = greenIndex
```

当 `N = 32`：

```text
色立方体 = 32 × 32 × 32
2D 纹理  = 1024 × 32
横向      = 32 个 B slice；每个 slice 宽 32 texel
slice 内  = R 横向、G 纵向
```

Identity LUT 的离散点为：

```text
RGB = (redIndex, greenIndex, blueIndex) / (N - 1)
```

生成器和 Shader 必须共同冻结以下契约，不能只约定分辨率：

| 项目 | 需要明确的内容 |
| --- | --- |
| 切片轴 | R、G、B 中哪一轴选择 slice |
| slice 排列 | 水平还是垂直，正序还是反序 |
| slice 内坐标 | 哪个通道对应 U/V，图像原点如何处理 |
| 输入坐标域 | LUT 查找前使用 Linear 还是 sRGB 数值 |
| 存储输出域 | PNG/EXR 中保存 Linear 还是 sRGB 编码值 |
| 采样输出域 | TextureImporter 和 GPU 解码后 Shader 得到什么域 |
| 边界 | 半 texel、Clamp、最后一个 slice、平台缩放与压缩 |

## 4. 正确的切片采样与插值

以下示例表达标准水平 B-slice 布局。函数名和颜色转换按项目替换：

```hlsl
float3 SamplePackedColorLut(float3 inputLinear)
{
    const float N = 32.0;
    const float InvN = 1.0 / N;
    const float Width = N * N;

    // 仅当 LUT 坐标契约是 sRGB 时执行；否则直接使用目标域。
    float3 inputSrgb = LinearToSRGB(saturate(inputLinear));

    float redCoord = inputSrgb.r * (N - 1.0);
    float greenCoord = inputSrgb.g * (N - 1.0);
    float blueCoord = inputSrgb.b * (N - 1.0);

    float slice0 = floor(blueCoord);
    float slice1 = min(slice0 + 1.0, N - 1.0);
    float sliceLerp = blueCoord - slice0;

    float2 uv0 = float2(
        (slice0 * N + redCoord + 0.5) / Width,
        (greenCoord + 0.5) / N);

    // 只切换 B slice；R/G 坐标必须保持一致。
    float2 uv1 = uv0 + float2((slice1 - slice0) * InvN, 0.0);

    float3 lut0 = SAMPLE_TEXTURE2D(_ColorLut, sampler_ColorLut, uv0).rgb;
    float3 lut1 = SAMPLE_TEXTURE2D(_ColorLut, sampler_ColorLut, uv1).rgb;
    return lerp(lut0, lut1, sliceLerp);
}
```

2D Bilinear 负责 slice 内 R/G 两轴插值；手动 `lerp` 负责 B 轴插值，三者共同形成近似三线性采样。

### 4.1 `lutLerp` 是锯齿波，但最终结果应连续

切片局部插值权重为：

```text
lutLerp = frac(B * (N - 1))
```

它会在每个切片区间内从 `0` 增长到接近 `1`，跨过边界后重新回到 `0`。直接返回：

```hlsl
return lutLerp.xxx;
```

必然显示重复的黑白条带或等高线。这是正确的权重可视化，不是最终 LUT 色阶。

最终插值在边界处应连续。边界前：

```text
lerp(sliceN, sliceN+1, 1) = sliceN+1
```

边界后端点组合切换为：

```text
lerp(sliceN+1, sliceN+2, 0) = sliceN+1
```

虽然权重从 `1` 跳回 `0`，公共端点也同步从 `lut1` 变成新的 `lut0`，两侧仍是同一个采样值。只有公共端点被不同 UV、颜色域或过滤方式采样时，最终输出才会保留切片接缝。

### 4.2 相邻 slice 只允许改变 slice 轴

水平 B-slice 布局中，相邻 slice 偏移通常是：

```hlsl
float2(1.0 / N, 0.0)
```

如果第二次采样同时改变 V，例如：

```hlsl
uv1 = uv0 + float2(1.0 / N, 0.5 / N);
```

那么边界前会收敛到“下一 slice、G 加半 texel”，边界后却从“同一 slice、原 G”开始。两侧公共端点不再相同，最终可能出现周期性细圈、颜色跳变或 G 方向偏色。

## 5. 颜色域与 TextureImporter 契约

颜色 LUT 至少存在两种有效设计，不能把某一种导入设置机械推广到所有项目：

### 5.1 sRGB 编码 LUT

```text
生成器输入/输出：sRGB 编码数值
PNG：保存 sRGB RGB
TextureImporter：sRGB On
Shader 坐标：按约定把 Linear 基础色转为 sRGB 后查找
GPU 采样结果：自动解码为 Linear，进入后续 Linear 光照
```

### 5.2 Linear 数据 LUT

```text
生成器输入/输出：Linear 数值
纹理：保存 Linear 数据或浮点数据
TextureImporter：sRGB Off
Shader 坐标和输出：保持 Linear
```

关键不是“LUT 一律开或关 sRGB”，而是生产者、文件、Importer、采样器和消费者对同一数值域达成闭环。Identity 是最小验收基线：在目标域中输入与输出应一致，不允许通过曝光、Shadow Tint 或后处理反向抵消颜色域错误。

### 5.3 2D 条带 LUT 的基础导入检查

一般需要检查：

- 分辨率严格等于生成契约，平台 Max Size 不得缩小；
- `Filter Mode = Bilinear`；
- `Wrap Mode = Clamp`；
- Mipmap 关闭，避免不同 mip 跨 slice 混合；
- 压缩关闭，尤其避免块压缩污染相邻 slice；
- 平台 Override 不得重新缩放或压缩；
- sRGB 按颜色域契约设置，不按文件扩展名猜测；
- 覆盖已有资产时保留 GUID，并在导入后回读最终 Importer，而不是只相信写入前设置。

## 6. 分层 Debug 顺序

不要直接从最终角色截图判断 LUT 坐标。建议逐层短路输出，并在完成后恢复正式返回：

```hlsl
// 1. 输入通道：应连续，不应出现周期性重置
return inputSrgb.bbb;

// 2. 离散 slice：应出现 N 级单调灰阶
float slice01 = slice0 / (N - 1.0);
return slice01.xxx;

// 3. slice 内权重：预期出现重复黑白锯齿波
return sliceLerp.xxx;

// 4. 两个端点
return lut0;
return lut1;

// 5. 最终 LUT 输出：正常情况下连续
return lerp(lut0, lut1, sliceLerp);
```

若函数内已经恢复正式返回，但 Fragment 仍存在更早的 `return`，后续 AO、Shadow、Ramp、Specular 和 Final Color 都不可达。调试时应记录短路位置，验收前搜索所有临时 `return`、常量色和 Debug Keyword，不能把短路画面当成正式结果。

## 7. 常见问题、根因与解决方向

| 现象 | 高概率根因 | 解决方向 |
| --- | --- | --- |
| `lutLerp.xxx` 一圈圈黑白 | 正在显示 `frac(sliceCoord)` | 属于预期；恢复最终 `lerp` 判断连续性 |
| 最终 LUT 仍一圈圈 | `lut0/lut1` 公共端点 UV 不一致、slice 轴错误、压缩/Mip/缩放 | 先用 Identity，检查第二个 UV 是否只移动 slice 轴，再查 Importer |
| Identity LUT 改变颜色 | sRGB/Linear 重复转换或漏转换、RGB 轴顺序不一致 | 同时输出输入色、LUT 输出和 raw sample，核对生产/消费域 |
| slice 边缘串色 | Repeat、UV 未对齐 texel 中心、平台块压缩 | Clamp、半 texel 中心、无压缩、无 Mip |
| B 接近 `1` 时异常 | 最后 slice 未 Clamp，仍访问不存在的下一 slice | `slice1 = min(slice0 + 1, N - 1)`，偏移按两者差值计算 |
| 深色肤色变纯红/纯黑 | 预设过强、通道被 Clamp 到 `0`、又叠加阴影色和 Ramp | 查看代表性色块数值；先中性化其他染色层，降低曲线/通道调整 |
| Editor 正常、Android 异常 | 平台 Override、ASTC/ETC、Max Size 或全局导入 Hook 改写 | 回读目标平台 Importer，检查最终格式和尺寸；必要时使用受控白名单 |
| Inspector 预览黑但导出可能正确 | 预览显示域、Alpha、拉伸或平台预览误导 | 检查原始 PNG 像素和通道，区分写入日志、导入设置与最终显示 |

## 8. LUT 生成工具的实现建议

### 8.1 从 Identity 出发

生成器应先构造完整 Identity 色立方体，再按固定顺序应用调整。推荐将以下内容显式化：

- 输入/输出颜色域；
- Exposure、Lift/Gamma/Gain、Contrast、Saturation、Hue、分通道曲线的执行顺序；
- 每一步的 Clamp 位置；
- Identity 快速路径，避免中性配置产生无意义浮点漂移；
- 输出尺寸、轴排列和像素原点；
- 预设只是参数起点，不是最终角色标准。

### 8.2 代表性色块是函数预览，不是材质取色器

界面中的：

```text
代表性输入色 → 当前 LUT 输出色
```

只是在若干固定 RGB 输入上执行同一个 `Evaluate(input)`。左侧不是当前材质实时采样，也不是“只修改这一类肤色”的局部开关；右侧也不是最终屏幕暗部，而是后续还会进入暗部强度、AO、Ramp、Shadow Tint、灯光和后处理的 LUT 输出。

预览应同时显示数值，帮助发现：

- 白点是否漂移；
- 中灰是否被意外染色；
- 深色输入是否有通道裁剪到 `0`；
- 某个“浅肤预设”是否破坏了同一贴图中的深色细节。

### 8.3 写入后以回读为准

可靠的输出流程应按“临时生成 → 编码 → 原子替换/覆盖确认 → AssetDatabase 导入 → TextureImporter 配置 → 再导入 → 回读校验”执行。覆盖失败时恢复 PNG 与 `.meta`，不要留下内容与 GUID/Importer 不一致的半成品。

## 9. A/B、性能与验收矩阵

### 9.1 功能 A/B

固定角色、贴图、灯光、相机、曝光、Tone Mapping、Ramp、AO 和 Scene Shadow，至少比较：

```text
A. LUT Off；调 Shadow Tint 达到目标暗部
B. Identity LUT On；Shadow Tint 中性
C. 正式 LUT On；Shadow Tint 中性
D. 正式 LUT On；恢复项目正式 Shadow Tint/Ramp
```

覆盖浅肤、中肤、深肤、中灰、白点、脸颊/耳朵偏红区、下颌/脖子、脸身接缝和不同光照方向。若一组固定 Shadow Tint 能覆盖全部代表色，LUT 的维护和采样成本可能没有必要；若不同输入色需要不同映射，才保留 LUT。

### 9.2 采样与性能

2D 条带 3D LUT 的第三轴插值通常增加两次纹理采样。运行时开关只有在目标 GPU、分支一致性、Shader 变体和覆盖率被测量后才能判断是动态分支还是 Keyword 更合适；不要仅凭“分支”或“LUT 很小”宣称无成本。角色大面积覆盖和移动端应检查采样、带宽、变体及平台纹理格式。

### 9.3 最小验收

| 层级 | 通过条件 |
| --- | --- |
| 生成器 | Identity 像素、轴排列、输出尺寸、代表性色块和覆盖失败恢复正确 |
| Importer | 尺寸、Bilinear、Clamp、Mip、压缩、sRGB、平台 Override 回读符合契约 |
| Shader | 输入通道、slice、权重、端点、最终 `lerp` 逐层正确；无临时短路 |
| 视觉 | 固定条件下无 slice 圈层；肤色、深色细节、脸身接缝和多肤色可接受 |
| 集成 | LUT、阴影色、Ramp 职责清楚；材质开关、资源引用、构建和加载路径有效 |
| 性能 | 目标 API/设备上的采样、内存、格式和变体风险已记录 |

无法完成 Unity 画面、构建或真机验证时，应明确记录未验证项，不能用静态公式证明最终视觉和性能已经通过。
