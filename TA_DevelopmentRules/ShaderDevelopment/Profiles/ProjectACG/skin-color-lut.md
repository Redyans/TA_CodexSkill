# ProjectACG 肤色颜色 LUT Profile

> **Profile ID**：`projectacg-skin-color-lut-v1`
>
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACGMain\ProjectACG\Client`
>
> **事实快照**：`2026-08-18`
>
> **通用参考**：[2D 条带式 3D 颜色 LUT 的生成、采样与调试参考](../../references/packed-3d-color-lut-sampling-and-debugging.md)

## 1. 目的、边界与 Source of Truth

本文记录 ProjectACG `Chara_Skin_V2` / `Chara_Face_V2` 的肤色 LUT 真实路径、暗部合成职责、`1024 × 32` 采样布局、生成器接口、已确认的历史半 texel 偏移、调试现象和验证方案。

本文不是 LUT 工具的美术操作手册。菜单、参数、预设、覆盖导出和 Importer 说明继续以功能旁 README 为准；源码、材质或导入结果与本文不一致时，以当前实际实现为准并修订本文。

| 职责 | 当前 Source of Truth |
| --- | --- |
| Skin Shader | `Assets/Shader/Character/Chara_V2/Chara_Skin_V2.shader` |
| Face Shader | `Assets/Shader/Character/Chara_V2/Chara_Face_V2.shader` |
| LUT 生成器 | `Assets/Editor/TA_Tools/Character/SkinColorLutGenerator/` |
| 美术说明 | `Assets/Editor/TA_Tools/Character/SkinColorLutGenerator/README_Art_SkinColorLutGenerator.md` |
| 技术说明 | `Assets/Editor/TA_Tools/Character/SkinColorLutGenerator/README_Tech_SkinColorLutGenerator.md` |
| 生成核心 | `Assets/Editor/TA_Tools/Character/SkinColorLutGenerator/Editor/SkinColorLutBakeUtility.cs` |
| 导入/回读 | `Assets/Editor/TA_Tools/Character/SkinColorLutGenerator/Editor/SkinColorLutAssetWriter.cs` |
| 预设 | `Assets/Editor/TA_Tools/Character/SkinColorLutGenerator/Editor/SkinColorLutPresetLibrary.cs` |
| Editor 测试 | `Assets/Editor/TA_Tools/Character/SkinColorLutGenerator/Tests/Editor/SkinColorLutGeneratorTests.cs` |

不得把本 Profile 的属性名、路径、Face/Skin 合成顺序或历史偏移迁移到其他项目。

## 2. 当前 LUT 资源与颜色域契约

### PACG-SKINLUT-01｜固定使用标准 `32³` 水平 B-slice 布局

当前生成器输出：

```text
色立方体：32 × 32 × 32
PNG：1024 × 32
排列：32 个 B slice 横排
slice 内：R 横向，G 纵向
输入/输出：sRGB 表达
```

像素索引：

```text
x = blueIndex * 32 + redIndex
y = greenIndex
RGB = (redIndex, greenIndex, blueIndex) / 31
```

Shader 接收 Linear `baseColor`，通过项目内分段函数转成 sRGB 查找坐标；LUT PNG 按 sRGB 颜色纹理导入，GPU 采样后返回 Linear RGB，继续进入 Linear 光照合成。这一闭环不同于“Linear 数据 LUT + sRGB Off”，维护时不能只因资源名含 LUT 就关闭 sRGB。

当前 Importer 契约：

```text
sRGB (Color Texture) = On
Filter Mode          = Bilinear
Wrap Mode            = Clamp
Generate Mip Maps    = Off
Compression          = None
Max Size             = 1024
Read/Write           = Off
常见平台 Override    = Off
资源标签              = Whitelist
```

`Whitelist` 用于避免项目全局贴图导入规则重新套用 ASTC。最终判断以 `SkinColorLutAssetWriter` 导入后的回读校验为准，不以写入前设置或 Inspector 缩略图为准。

### PACG-SKINLUT-02｜当前通道重排对应 B-slice、R-in-slice、G-vertical

Skin 与 Face 当前都先执行：

```hlsl
float3 albedoSrgb = LinearToSrgb(albedoLinear.brg);
```

因此函数局部通道为：

```text
albedoSrgb.x = 原始 B，用于 slice 选择和 sliceLerp
albedoSrgb.y = 原始 R，用于 slice 内横向 U
albedoSrgb.z = 原始 G，用于纵向 V
```

调试时不能把 `albedoSrgb.x` 误写成“原始 R”。若将 `.brg` 改回 `.rgb`，必须同时重写所有坐标轴映射并用 Identity 验证，不能只改 swizzle。

## 3. LUT、阴影色和 Ramp 的当前合成职责

### PACG-SKINLUT-03｜LUT 只替换暗部 Albedo 来源

Skin 正式设计：

```hlsl
float3 albedoLight = albedoSssRefine * 0.96;
float3 albedoLutRefine = albedoSssRefine;
if (_UseSkinLut > 0.5)
{
    albedoLutRefine = ToonSkinSampleSkinLutColor(baseColor);
}
float3 albedoDark = albedoLutRefine * 0.96 * _AlbedoDarkStrength;
```

Face 正式设计：

```hlsl
float3 albedoLight = faceAlbedo * 0.96;
float3 lutAlbedo = (_UseSkinLut > 0.5)
    ? FaceSampleSkinLutColor(faceAlbedo)
    : faceAlbedo;
float3 albedoDark = lutAlbedo * 0.96 * _AlbedoDarkStrength;
```

所以 `_LutColorTex` 的输出是暗部 Albedo 的原始映射，不是最终屏幕暗部；后续还会经过 `_AlbedoDarkStrength`、`_AlbedoDarkSaturation`、AO/Scene Shadow/NoL、Ramp、`_ShadowColor`、灯光、间接光和后处理。

### PACG-SKINLUT-04｜“AO阴影颜色”实际是组合暗区主光色

Skin 当前暗区权重：

```hlsl
float aoForLighting = lerp(1.0, ao, chinDark);
float totalShadowArea = min(min(aoForLighting, sceneShadow), remapNoL);
float3 mainLightTint = lerp(_ShadowColor.rgb, mainLightColor, totalShadowArea)
    * mainLightIntensity;
```

`_ShadowColor` 的 Inspector 显示名是“AO阴影颜色”，但实际同时受 AO、Scene/PerObject Shadow 和 Ramp/NoL 控制，不是只染 AO。

Face 当前设置：

```hlsl
float aoForLighting = 1.0;
```

因此 Face 的 `_ShadowColor` 主要由 SDF/Lambert 明暗、Scene Shadow 和 Ramp 驱动，尤其不能解释成 Face 贴图 AO 色。

最终暗部可近似理解为：

```text
finalDark ≈ LUT(baseColor)
          × _AlbedoDarkStrength
          × _ShadowColor
          × Ramp
          × 其他光照强度
```

这解释了 LUT 与 `_ShadowColor` 的视觉重叠，也解释了两者和有色 Ramp 同时强调时为什么容易双重偏色。

### PACG-SKINLUT-05｜默认按需求决定是否启用 LUT

当前建议职责：

| 场景 | 推荐控制 |
| --- | --- |
| 单一角色、基础肤色范围窄、只需统一冷暖 | 优先 `_ShadowColor + _AlbedoDarkStrength + Ramp`，验证 LUT 是否可关闭 |
| 不同基础肤色需要分别映射 | 启用 LUT，先把 `_ShadowColor` 和 Ramp 恢复中性做 A/B |
| 深肤需要保层次、浅肤需要偏粉/偏暖 | 使用非线性 LUT，不用一组固定乘色强行覆盖 |
| LUT 只做统一乘色或压暗 | 评估是否值得增加采样和资源维护 |

启用 LUT 后，不建议同时使用强烈偏红 LUT、强烈偏红 `_ShadowColor` 和强烈偏红 Ramp。先确定主要调色层，再逐层恢复项目正式参数。

## 4. 生成器预览和预设的正确语义

### PACG-SKINLUT-06｜代表性色块是固定输入样本，不是材质实时取色

窗口当前内置六个 sRGB 输入：

```text
浅肤 = (0.90, 0.75, 0.65)
暖肤 = (0.80, 0.55, 0.45)
中肤 = (0.65, 0.40, 0.30)
深肤 = (0.38, 0.20, 0.14)
中灰 = (0.50, 0.50, 0.50)
白点 = (1.00, 1.00, 1.00)
```

每一行执行：

```csharp
output = SkinColorLutBakeUtility.Evaluate(inputSrgb, adjustments);
```

左侧表示“如果基础色输入接近该固定 RGB”，右侧表示“当前 LUT 参数将其映射成什么 RGB”。右侧数值还没有乘 Shader 暗部强度、Shadow Color、Ramp 或灯光。

预设会先恢复 Identity，再写 Exposure、Contrast、Saturation、Hue 和 RGB Lift/Gamma/Gain；它作用于整个 `RGB → RGB` 函数，不是只修改与预设名称相同的某一行。即使 LUT 只给浅肤角色使用，也要观察深色输入，因为同一基础贴图仍可能包含嘴角、鼻孔、耳朵、脖子和线条等深色像素。

发现深色输入某个通道被 Clamp 到 `0` 时，应先判断是否为有意风格，再检查与 `_ShadowColor`、Ramp 相乘后的最终层次；不能只以浅肤第一行好看作为通过条件。

### PACG-SKINLUT-07｜Identity 是实现基线，不是一个肤色风格

Identity 状态应满足：

```text
代表性色块左侧 ≈ 右侧
目标材质 LUT Off ≈ Identity LUT On
```

允许 PNG 8-bit 量化和采样产生很小误差，不允许出现明显偏色、周期性圈层或亮度跳变。Identity 未通过前，不调整肤色预设、Shadow Color 或曝光补偿。

## 5. 已确认的问题与解决方向

### PACG-SKINLUT-08｜`lutLerp.rrr` 的黑白圈是预期调试图

当前插值权重等价于：

```hlsl
float lutLerp = frac(originalBlueSrgb * 31.0);
```

临时返回：

```hlsl
return lutLerp.rrr;
```

会把每个 B slice 区间内的 `0 → 1` 锯齿波显示成 31 组重复黑白条带。它只证明权重周期，不代表最终 LUT 颜色，也不能据此判断 `lerp` 会“一圈 lut0、一圈 lut1”。

正式边界连续性依赖：

```text
边界前 lerp(sliceN,   sliceN+1, 1) = sliceN+1
边界后 lerp(sliceN+1, sliceN+2, 0) = sliceN+1
```

验收应恢复：

```hlsl
return lerp(lut0, lut1, lutLerp);
```

Fragment 中若使用 `return float4(albedoLutRefine, 1)` 查看 LUT 输出，后面的 `_ShadowColor`、AO、Ramp、Scene Shadow、Specular 和 Final 合成全部不可达。此类短路只用于单层诊断，提交或视觉验收前必须移除。

### PACG-SKINLUT-09｜当前第二个 B slice 额外偏移半个 G texel

Skin 与 Face 当前正式采样都包含：

```hlsl
lutUVFinal + float2(0.03125, 0.015625)
```

其中：

```text
0.03125  = 1 / 32，移动到下一个水平 B slice
0.015625 = 0.5 / 32，同时向 G 方向移动半个 texel
```

当前生成器始终输出标准布局，不会把额外半个 G texel 反向烘焙进 PNG。对于标准水平 B-slice，候选修正是：

```hlsl
lutUVFinal + float2(0.03125, 0.0)
```

该修正会让边界两侧对公共 slice 使用相同 G 坐标，消除由坐标不一致带来的周期性细跳变风险。但它属于现有角色视觉兼容性改动，不能只改 Skin 或只看一张截图后直接收口。

实施时同步修改 Skin/Face，并固定条件比较：

- Identity LUT 的输入/输出和切片边界；
- 浅肤、中肤、深肤、中灰、白点；
- 脸颊、耳朵、下颌、脖子和脸身接缝；
- LUT Off、历史偏移、修正偏移三组；
- 不同相机距离、光照方向和目标平台导入结果。

若存量 LUT 或角色已经围绕历史偏移做过人工补偿，应记录受影响材质和回退方式；不能把“标准数学更正确”当成存量视觉自动兼容的证明。

## 6. 当前实现和维护方案

### PACG-SKINLUT-10｜生成器只负责颜色映射和受控导入

`SkinColorLutGenerator` 当前职责：

- 从 Identity `32³` 色立方体生成 `1024 × 32` PNG；
- 应用 Exposure、Lift/Gamma/Gain、Contrast、Saturation、Hue 和曲线；
- 提供 Identity 与肤色起点预设；
- 显示 LUT 纵向放大图和代表性色块数值；
- 显式选择项目文件夹与文件名；
- 覆盖前确认，失败时尝试恢复原 PNG 与 `.meta`；
- 配置并回读 Importer，添加 `Whitelist` 防止全局 Hook 重新压缩。

它不负责：

- 从角色截图反推 LUT；
- 把 AO、Ramp、灯光、阴影或后处理烘进 LUT；
- 修改 Shader、材质、Prefab 或场景；
- 证明预设在所有角色、灯光和设备上已经验收。

工具实现、菜单和参数变化应先更新功能旁 README；本 Profile 只维护会影响 Shader/资源集成和跨任务诊断的项目事实。

### PACG-SKINLUT-11｜性能和开关按当前实现测量

`_UseSkinLut > 0.5` 的启用路径会采样 `lut0`、`lut1` 两次并执行手动 B 插值。关闭路径不应为了“预览一致”仍强制采样 LUT。是否把动态 Float 开关迁移为 Keyword，必须结合角色屏幕覆盖、目标移动 GPU、材质数量、分支一致性、变体预算和构建剥离实测；当前不能仅凭两次采样认定必须增加 Keyword。

## 7. ProjectACG 验证流程

### 7.1 分层采样验证

按顺序短路输出：

```text
原始 baseColor
→ sRGB 后原始 B 通道
→ floor(B × 31) / 31
→ frac(B × 31)
→ lut0
→ lut1
→ lerp(lut0, lut1, lutLerp)
```

预期：

- 原始 B 连续；
- slice index 是 32 级单调灰阶；
- `lutLerp` 是重复黑白锯齿波；
- `lut0/lut1` 是相邻颜色端点；
- 最终 `lerp` 在 slice 边界连续。

### 7.2 材质职责 A/B

```text
A. LUT Off；使用正式 _ShadowColor
B. Identity LUT On；_ShadowColor = 白色，Ramp 中性
C. 正式 LUT On；_ShadowColor = 白色，Ramp 中性
D. 正式 LUT On；恢复正式 _ShadowColor 和 Ramp
```

如果 A 能覆盖所有代表肤色和深色细节，可以不启用 LUT；如果不同基础色需要不同响应，才由 C 承担颜色分级。D 用于最终项目风格，不用于证明 LUT 单层正确。

### 7.3 资产与平台验证

导出后至少回读：

```text
尺寸 1024 × 32
sRGB On
Bilinear
Clamp
Mip Off
Compression None
平台 Override Off
Whitelist 已存在
```

再覆盖 Skin、Face、脸身接缝、多肤色和目标 Android 格式。Inspector 预览不能代替原始 PNG 像素、Frame Debugger/RenderDoc 或真机画面。

## 8. 当前验证边界与回退

本次规则沉淀基于当前 Shader、生成器和功能 README 的静态代码核对，以及 `lutLerp`/暗部合成的数学分析；没有在本文写入过程中修改 Shader、材质或 LUT 资产，也没有重新执行 Unity Test Runner、角色场景视觉 A/B 或目标移动设备性能测试。

若实施半 texel 修正，安全回退是同时恢复 Skin/Face 的第二 slice UV 偏移，并保留 LUT PNG、`.meta` 和材质引用不变。不得用删除或重生成全部 LUT 作为第一回退手段。
