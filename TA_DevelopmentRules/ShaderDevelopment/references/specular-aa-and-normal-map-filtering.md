# Specular AA 与法线贴图方差过滤参考

> 类型：REFERENCE；适用范围：Unity URP 的 Lit/PBR、Toon/Hybrid 或自定义光照中由几何法线、法线贴图高频细节引起的镜面高光闪烁；使用前提：必须以目标项目的 Shader 数据流、URP 版本、贴图导入和目标 GPU 重新验证。

本文总结高光抗锯齿（Specular Anti-Aliasing，简称 Specular AA）的原理、接入位置、法线贴图覆盖方式、强制开启策略和验证方法。它是实现与排查参考，不替代目标 Shader 的材质、Pass、RendererFeature、变体和设备契约。

## 1. 适用场景

- 低模曲面、硬边、远距离模型或运动镜头下，镜面高光出现像素级闪烁、断裂或跳动。
- 法线贴图包含比当前屏幕像素更高频的细节，导致高光宽度随相机移动不稳定。
- 需要在 Direct Specular 与 Indirect Specular/IBL 共用同一粗糙度输入的 PBR 路径中稳定高光。
- 需要把高光过滤作为材质光照的一部分，而不是依赖后处理 TAA、FXAA 或 Bloom 掩盖问题。

以下情况不应直接套用本文：

- 纯漫反射或不使用 BRDF 粗糙度的 Unlit 效果；
- 高光来自独立的 MatCap、各向异性 Lobe、卡通 Ramp 或屏幕纹理，且这些分支没有共享 PBR 粗糙度；
- 目标平台、Shader Model 或图形 API 尚未确认支持片元导数；
- 需求实际是几何边缘抗锯齿、透明排序或阴影锯齿，而不是镜面 NDF 的高频闪烁。

## 2. 前提与依赖

### 2.1 先冻结输入契约

在修改前记录以下事实：

| 项目 | 要确认的内容 |
| --- | --- |
| 粗糙度语义 | 输入是 `roughness` 还是感知 `smoothness`；是否在 Shader 内反相或做非线性调整。 |
| 法线来源 | 顶点/几何法线、插值法线、法线贴图、Detail/叠加法线、投影重建法线。 |
| 光照消费者 | Direct Specular、Indirect Specular/IBL、附加灯、各向异性或自定义 Lobe 是否共用该输入。 |
| 执行阶段 | `ddx/ddy` 只能在 Fragment 阶段估算屏幕方差；不能把导数结果在 Vertex 阶段预计算。 |
| Pass 范围 | Forward 之外是否有透明深度、Outline、ShadowCaster、DepthNormals 或其他实际使用高光的 Pass。 |
| 平台约束 | 目标 Shader Model、移动 GPU、分辨率、MSAA、相机 FOV/距离和性能预算。 |

### 2.2 优先复用 URP 的过滤函数

目标 URP 版本若提供 `CommonMaterial.hlsl` 的以下函数，应优先复用并核对签名，而不是复制一套 BRDF：

- `GeometricNormalVariance`：由 `ddx/ddy` 计算屏幕空间几何法线方差；
- `ProjectedSpaceGeometricNormalFiltering`：把几何法线方差并入感知光滑度；
- `TextureNormalVariance` / `TextureNormalFiltering`：根据法线贴图覆盖区域的平均法线长度估算纹理方差；
- `ProjectedSpaceNormalFiltering` / `NormalFiltering`：对合并后的方差执行粗糙度过滤。

函数所在路径、参数顺序和版本实现必须以当前工程嵌入式 URP 源码为准；不能只根据其他 URP 版本或博客中的签名调用。

## 3. 实现或排查步骤

### 3.1 区分几何法线方差和法线贴图方差

Specular AA 不是“把法线变平”，而是估算一个像素覆盖区域内的法线分布，并增加 BRDF 的有效粗糙度。两类方差来源要分开记录：

**几何法线方差**（屏幕导数）：

```hlsl
varianceGeometry = screenSpaceVariance *
    (dot(ddx(geometricNormalWS), ddx(geometricNormalWS)) +
     dot(ddy(geometricNormalWS), ddy(geometricNormalWS)));
```

它适合处理曲面、硬边和低模几何的欠采样。`screenSpaceVariance` 是像素重建核的权重，具体上限和默认值以目标 URP 实现为准；URP 参考实现建议不超过 `0.25`。

**法线贴图方差**（纹理覆盖区域）：

`TextureNormalVariance` 接收覆盖区域内的平均法线长度 `avgNormalLength`。平均长度越小，说明像素内法线方向分散越大，应增加的粗糙度越多。这个长度不能从普通法线纹理的单次 RGB 采样可靠推断，通常需要：

1. 离线预烘的平均法线向量/长度通道；
2. 项目约定的法线贴图 Alpha 或其他辅助纹理通道；
3. 运行时用额外采样和导数估算（精度、平台和成本需要单独评估）。

没有这类数据时，只能准确宣称“几何法线 Specular AA”，不能宣称已经过滤法线贴图高频方差。

### 3.2 在共享 BRDF 输入前过滤粗糙度

推荐数据流如下：

```text
采样材质贴图
  → 得到最终法线贴图输入与原始 roughness/smoothness
  → 在 Fragment 计算几何方差（可选地叠加纹理方差）
  → 过滤后的粗糙度/光滑度写回共享 BRDF 输入
  → Direct Specular + Indirect Specular/IBL + 其他共用该 BRDF 的分支
```

对于感知 `smoothness`，可直接使用 URP 的 `ProjectedSpaceGeometricNormalFiltering`；对于 `roughness`，应先转成感知光滑度，过滤后再转回：

```hlsl
float perceptualSmoothness = saturate(1.0 - roughness);
perceptualSmoothness = ProjectedSpaceNormalFiltering(
    perceptualSmoothness,
    varianceGeometry + varianceTexture * textureVarianceWeight,
    threshold);
roughness = 1.0 - perceptualSmoothness;
```

不要把 `smoothness` 当成 `roughness` 直接相加。URP 的过滤函数内部会在感知光滑度、粗糙度平方和 NDF 参数之间转换；应保持该转换的完整性，并在最终写回时 `saturate`。

过滤结果必须在 `InitializeBRDFData` 或等价 BRDF 构造之前产生，并且所有使用该 BRDF 的直接/间接高光都读取同一个结果。只过滤主光而遗漏 IBL，会出现“直射高光稳定、环境高光仍闪烁”的假修复。

### 3.3 几何法线的选择

- 普通模型：保存未叠加法线贴图的几何/插值法线，或按目标 URP 约定使用其几何法线输入；不要误把最终切线空间法线当作几何方差。
- 屏幕空间投影/Decal：若光照位置由屏幕重建，使用重建位置的场景法线，并处理朝向翻转和退化三角形；投影坐标的法线不能继续沿用原网格法线。
- 使用 `SafeNormalize` 或等价保护，避免退化法线导致 NaN 扩散。

这里的“几何法线”是方差估计的来源，不代表要用它替换 BRDF 的着色法线。最终 `normalWS` 仍可包含法线贴图，以保持漫反射、阴影和既有材质表现。

### 3.4 保持导数计算的片元一致性

`ddx/ddy` 依赖 Fragment 四边形的相邻像素。应让方差计算处于稳定的片元控制流中：

- 不要把导数调用放进只由材质/像素结果决定的高度发散分支；
- 如果 Alpha Clip、discard 或早退会影响导数邻居，按目标平台验证导数有效窗口，必要时在裁剪前计算；
- 不要在 Vertex 阶段计算并插值一个“屏幕方差”，那会失去像素级覆盖信息；
- 透明、双面和投影 Pass 要分别确认法线朝向、裁剪和导数行为。

### 3.5 强制开启还是材质开关

两种策略的取舍：

| 策略 | 适用条件 | 成本与风险 |
| --- | --- | --- |
| 强制开启 | 所有材质都要求稳定高光，且已接受每像素导数/粗糙度过滤成本。 | 无材质状态分裂和变体膨胀；低端设备和低覆盖材质也承担固定成本。 |
| 材质/质量开关 | 只有部分材质或质量档位需要，且已有可靠的运行时控制、变体收集和设备测量。 | 增加序列化接口、分支/变体和测试矩阵；开关失效或漏收集会造成行为不一致。 |

若需求是“默认开启、不要开关”，实现应直接把过滤写入光照数据流：不新增 Shader 属性、`shader_feature`、`multi_compile`、全局 Keyword 或 CBUFFER 字段，也不要保留一个实际不生效的伪开关。未来若性能测量证明需要质量档位，应另立兼容性变更，不能偷偷恢复材质开关。

### 3.6 常见问题与修正方向

| 现象/错误做法 | 根因 | 修正 |
| --- | --- | --- |
| 有法线贴图的模型仍会闪烁，却宣称“已支持法线贴图 AA” | 只计算了几何法线方差，没有纹理平均法线长度/方差数据。 | 明确降级为几何法线 AA，或补齐预烘/辅助通道并重新验证。 |
| 高光变宽后漫反射、阴影也变了 | 直接修改或平滑了 `normalWS`，而不是只增加 BRDF 粗糙度。 | 保留原着色法线，只过滤共享 Specular 的 roughness/smoothness。 |
| 只有主光高光稳定，IBL 仍跳动 | Direct 和 Indirect 使用了不同的粗糙度变量或过滤位置太晚。 | 在共同 BRDF 输入前过滤，并确认 `GlobalIllumination` 也读取该 BRDF。 |
| 过滤效果随模型距离、FOV、分辨率变化很大 | 屏幕导数本来就依赖投影和像素覆盖；参数未经目标画面校准。 | 固定相机/分辨率做 A/B，再覆盖近远景和运动镜头；不要把某个截图参数当全局真值。 |
| 通过后处理 TAA/FXAA 后闪烁减弱 | 后处理只是时空/边缘重建，未修复 NDF 欠采样；可能留下拖影或峰值能量问题。 | 先在材质光照层做 Specular AA，再把后处理作为独立抗锯齿验证。 |
| `roughness += variance` 后高光异常或反转 | 粗糙度、感知光滑度和 NDF 方差的语义混用。 | 使用目标 URP 的过滤函数，明确 `roughness ↔ perceptualSmoothness` 转换。 |
| 为了调试直接 `return` 高光或粗糙度 | 后续 AO、阴影、Fog、间接光和最终合成不可达，短路画面被误当成正式结果。 | 使用受控 Debug 输出；验收前恢复完整路径并搜索临时 `return`。 |

## 4. 风险与不适用边界

- Specular AA 是能量/宽度近似，不保证恢复真实多重采样结果；尖锐峰值通常会被压低或展宽。
- `threshold` 过大可能使所有材质显得过于粗糙；过小则无法抑制闪烁。参数应按代表性材质、屏幕覆盖和目标设备校准。
- 法线贴图过滤依赖导出与导入契约。只把 Alpha 当作方差通道而不确认现有素材含义，可能破坏透明、遮罩或压缩数据。
- 各向异性、MatCap、卡通高光和自定义查找纹理若不读取同一粗糙度，不能仅凭 PBR Specular AA 结论覆盖它们。
- ShadowCaster、DepthOnly、DepthNormals 等不使用高光的 Pass 通常不需要过滤，但仍要检查是否共享会受影响的输入或宏；不要为了“全 Pass 一致”机械复制计算。
- 强制开启会增加所有采用该 Shader 的覆盖像素的 ALU 和导数成本。移动端必须使用实际 GPU、分辨率和材质覆盖率测量，而不是仅看静态编译通过。

## 5. 验证与回退

### 5.1 静态验证

1. 确认过滤函数在目标 URP 源码中存在，参数顺序和输入语义正确。
2. 搜索 Shader 属性、Keyword、CBUFFER 和脚本写入，确认“无开关”要求没有残留伪接口。
3. 从法线贴图采样到最终 BRDF 的数据流逐层核对，确认几何法线、着色法线和纹理方差没有混用。
4. 检查 Direct/Indirect/附加灯/自定义 Lobe 是否共用过滤后的输入。
5. 对所有受影响文件执行 UTF-8、尾随空白、Include 路径和 Shader 语法检查。

### 5.2 Unity/运行时验证

- 用同一材质、灯光、曝光、相机和分辨率做 Specular AA Off/On A/B；On 只应改变高光宽度/稳定性，不应改变漫反射、阴影和材质贴图颜色。
- 覆盖无高光、低光滑度、高光滑度、金属/非金属、低模硬边、曲面和高频法线贴图模型。
- 固定相机后逐帧截图，再快速平移/旋转镜头；覆盖近景、远景、不同 FOV 和不同输出分辨率。
- 单独输出原始 smoothness/roughness、几何方差、纹理方差、过滤后值、Direct Specular、Indirect Specular；不要只看最终颜色。
- 用 Frame Debugger 或 RenderDoc 确认目标 Pass、BRDF 输入和资源绑定；用 Profiler/目标设备测量导数、ALU、带宽和帧时长。
- 若项目有法线贴图预烘通道，回读导入格式、压缩、Mip、Filter/Wrap 和 Alpha 语义，并验证无方差数据的旧材质回退。

### 5.3 回退

最小回退是移除或旁路粗糙度过滤调用，保留原有法线、材质属性、贴图和 Pass 接口不变。若问题只来自参数，应先回退 `screenSpaceVariance`/`threshold` 或暂时使用几何法线版本，不要删除法线贴图或重烘全部资产。任何恢复开关、增加方差通道或修改贴图导入的方案都属于新的兼容性与资产迁移变更，应单独记录和验证。
