# Unity 与 Substance Painter Shader 对齐实战参考

> **类型**：REFERENCE；**适用范围**：Unity 运行时 Shader 与 Adobe Substance 3D Painter 自定义预览 Shader 的材质、光照和显示结果对齐；**使用前提**：必须以目标 Unity Shader、材质、贴图导入设置、Painter 版本和实际显示链重新验证。本文不替代目标 Shader 或工具旁的 Art / Tech README。

## 1. 适用场景

本文用于解决以下问题：

- Unity 与 Painter 参数看似一致，但漫反射、高光、粗糙度或最终颜色仍不一致；
- Painter 中使用独立制作通道，Unity 最终使用 RGBA 打包贴图；
- 同一张 Ramp、HDRI 或 LUT 在两端出现亮度、饱和度、黑点、模糊度差异；
- 需要判断差异来自材质输入、直接光、环境反射还是 Tone Mapping / Color Profile；
- 需要为自定义 Painter Shader 建立可复现的 Debug、编译和视觉验收流程。

停止条件：当同输入 Debug 已确认一致、差异已定位到某一层且该层存在不可等价的平台边界时，应记录边界或局部校准，不再通过无依据地改 Roughness、Mip、曝光或 Base Color 掩盖问题。

## 2. 先拆成五层，不比较“整张最终图”

参数相同只说明 UI 数值相同，不代表执行链相同。对齐前先建立以下分层：

| 层级 | 必须核对的内容 | 典型 Debug |
| --- | --- | --- |
| 材质数据 | Base Color、Normal、Metallic、Roughness/Smoothness、AO、Reflectivity、Opacity、Emission、打包与默认值 | 原始通道、调整后通道 |
| 几何与直接光 | 坐标系、切线基、`N`、`L`、`V`、`H`、光色/强度、阴影、相机偏置、Specular AA | Direct Diffuse、Direct Specular |
| 间接光 | HDRI 内容、旋转、曝光、卷积、Mip 数、Roughness 到 LOD 的响应、IBL BRDF、F0、AO | Indirect Diffuse、Indirect Specular |
| Shader 合成 | 各光照项、Ramp、Rim、Emission、透明度和项目全局参数的顺序 | Final、Final Grayscale |
| 显示与后处理 | 线性/显示编码、Tone Mapping、Color Profile、额外 Color Correction、截图色彩管理 | Base Color Only、灰阶/RGB 色块 |

对齐顺序固定为“材质数据 → 直接光 → 间接光 → Shader 合成 → 显示与后处理”。前一层未通过时，不进入下一层做视觉补偿。

## 3. 建立 Source of Truth 和数据契约

### 3.1 目标 Unity Shader 是唯一事实来源

参考 Painter Shader 只能复用 API、参数声明和输出脚手架，不能继承它的通道含义或 BRDF 假设。移植前至少记录：

- Unity 实际采样了哪张贴图、哪一个通道；
- 通道是 Roughness 还是 Smoothness，是否在 Shader 内反相；
- 贴图缺失、开关关闭和稀疏像素时的默认值；
- 强度、对比度、Power、Clamp、Lerp 的执行顺序；
- 直接高光和 IBL 是否共用同一个 Roughness / F0；
- 哪些项来自 RendererFeature、阴影图、顶点色、全局参数或后处理，Painter 无法提供。

建议先形成一张契约表：

| 语义 | Painter 制作通道 | Unity 最终通道 | 变换 | 颜色空间 |
| --- | --- | --- | --- | --- |
| Base Color | `Base Color` | 目标颜色贴图 RGB | 通常直接输出 | sRGB 颜色 |
| Metallic | `Metallic` | 目标数据通道 | 直接输出 | Linear 数据 |
| Roughness | `Roughness` | 视目标而定 | 目标为 Smoothness 时执行 `1 - Roughness` | Linear 数据 |
| AO | `Ambient Occlusion` | 目标数据通道 | 直接输出 | Linear 数据 |
| Reflectivity | 项目自定义灰度通道 | 目标数据通道 | 直接输出 | Linear 数据 |
| Normal | `Normal` | Normal Map | 按项目法线方向导出 | Normal 数据 |

### 3.2 制作通道与交付打包分离

美术制作阶段优先使用 Painter 原生独立通道。只有最终导出时，才按 Unity Shader 契约打包 RGBA。这样可以：

- 保留 Painter 原生 Generator、Mask、Anchor 和材质工作流；
- 避免美术以 RGBA 颜色通道思维绘制 Roughness、AO 等数据；
- 让 Roughness 到 Smoothness 的反相只存在于一个导出规则中；
- 单独 Debug 每个输入，降低“某个打包通道覆盖原生通道”的风险。

如果自定义 Shader 保留旧打包通道兼容路径，必须明确优先级。旧通道一旦存在就覆盖原生通道时，应在正式制作流程中禁用或移除它，而不是让两套输入同时有效。

### 3.3 Sparse Alpha 不能随意承载数据

Painter 的 `SamplerSparse` 采样 Alpha 可能承担覆盖率或通道有效性语义。不能仅因为 RGBA 还剩 A，就把 Reflectivity 等业务数据放入稀疏通道 Alpha。业务标量应使用独立灰度通道；读取稀疏通道时要明确缺失像素如何回退到默认值。

### 3.4 把输入分成四类再设计同步

同一 Shader 面板中的 Texture/参数不一定拥有相同的制作与传输语义：

| 类型 | 示例 | 处理方式 |
| --- | --- | --- |
| Painter 制作通道 | Base Color、Normal、Metallic、Roughness、AO、Emission、Opacity | 在 Painter 中独立绘制；只在导出或临时预览边界打包 |
| 共享材质参数 | Float、Bool、Enum、Color、Vector | 进入显式 Adapter 白名单；同步类型、单位、范围与必要 Keyword |
| Shader 辅助纹理 | Diffuse/Specular Ramp、Noise、MatCap、预卷积环境 Atlas | 作为预览输入单独传输；不是绘制通道，不从 DCC 反向伪装成绘制结果 |
| 宿主/运行时状态 | 相机、阴影、RendererFeature、Timeline MPB、后处理 | 无可靠映射时冻结或人工输入；参数传输成功不代表该层已对齐 |

分类不清会导致两类典型错误：把辅助纹理塞进 `User` 通道破坏原生制作流程，或在 DCC → Unity 时把缺失的辅助纹理替换成默认图并覆盖正式材质。

## 4. 颜色空间与纹理采样规则

### 4.1 每个颜色变换只执行一次

| 资源 | Unity 常见行为 | Painter 自定义 Shader 对齐方式 |
| --- | --- | --- |
| Base Color / Emission 颜色 | sRGB 纹理采样时硬件解码为 Linear | 确认 Painter API 已返回何种域，禁止重复 Gamma |
| Ramp RGB | 作为颜色纹理时 sRGB → Linear | 若自定义纹理 API 不自动解码，只对 RGB 做精确 sRGB → Linear |
| Ramp Alpha | 控制曲线/权重 | 保持 Linear，不随 RGB 解码 |
| Metallic/Roughness/AO/Mask | Linear 数据 | 不做 sRGB 解码 |
| Identity LUT | Linear 浮点数据 | 非 sRGB、非压缩、无 Mip |
| Painter SDR Color Profile 输出 | Painter 显示域通常期望 sRGB 编码 | 烘焙输出执行一次 Linear → sRGB；下游明确会编码时才关闭 |

“Unity 使用 Linear Color Space”“EXR 是浮点”“Shader 使用 HDR 颜色”都不等于可以省略最终显示编码。判断依据是 LUT 输出接口的契约，而不是工程 Color Space 或文件扩展名。

### 4.2 Ramp 边界必须处理 Wrap 和 Bilinear

Painter 自定义纹理可能使用 Repeat，而 Unity Ramp 通常使用 Clamp。Ramp 首尾色差大时，直接采样 `U=0` 或 `U=1` 会让双线性过滤跨边界混入另一端，表现为针状黑点或亮点。

通用修正是根据 `textureSize()` 把 U 限制到首尾 Texel 中心：

```glsl
float width = max(float(textureSize(rampTex, 0).x), 1.0);
float halfTexel = 0.5 / width;
float safeU = clamp(u, halfTexel, 1.0 - halfTexel);
```

可选 Ramp 还必须有显式启用开关或可靠的“未设置”回退。空白纹理的 Alpha 常为 `1`；若 Alpha 用于明暗重映射，它会把整个模型推到亮面，造成“旋转灯光没有变化”的假象。

## 5. 直接光和直接高光对齐

### 5.1 先冻结坐标与相机

固定以下条件后再比较：

- 同一模型、姿态、相机方位、FOV 和近远裁剪；
- 同一法线格式、Normal Scale、切线空间和绿色通道方向；
- 同一主光方位角、仰角、颜色和线性强度；
- 两端同时关闭 Tone Mapping、Color Profile、额外 Color Correction、阴影和非必要光照项；
- 使用 Direct Diffuse / Direct Specular Debug，不用最终画面判断根因。

### 5.2 “参数相同但高光不同”的检查顺序

依次输出或计算：

1. `N`：原始切线法线、调整后法线、世界法线；
2. `L`：方向定义、符号、方位角零点与仰角；
3. `V`：从表面到相机还是相反方向，是否混入 Camera Forward；
4. `H`：标准 `normalize(L + V)` 还是项目自定义相机偏置公式；
5. `N·H`、`N·V`、`N·L`；
6. F0：非金属基准、Reflectivity、Metallic 与 Base Color 混合；
7. 调整后的 Roughness，而不是只看贴图原值；
8. D/V/F、Clamp、Shadow/AO 门控和最终强度。

Unity 使用非标准相机偏置半角向量时，Painter 必须迁移相同公式才能做严格对比。若 Painter 默认使用标准半角向量，即使所有材质参数一致，高光位置和形状也会不同。

### 5.3 Specular AA 只允许作为诊断旁路

Specular AA 会根据法线变化改变最终 Roughness。排查时可以临时旁路，以确认差异是否来自粗糙度过滤，但不能把“永久关闭 Specular AA”作为跨工具对齐方案。正确流程是：

1. 保留一份原行为基线；
2. 两端比较原始与调整后 Roughness；
3. 临时旁路 Specular AA 做 A/B；
4. 确认根因后恢复 Unity 正式行为，或在 Painter 中实现同等过滤并重新验证。

### 5.4 复杂高光按完整数据流移植

新增 Ramp、各向异性 Lobe 或 MatCap 时，参数名和公式片段相同仍不足以对齐。建议逐项建立以下对照：

```text
纹理颜色域和 Wrap
UV / Tiling / Offset
Tangent / Bitangent / Normal 基底
几何法线、贴图法线和切线空间法线
原始 Eye Vector 与经过项目偏置的 View Vector
Light / Half Vector
共享遮罩及其来源通道
F0 修改发生的位置
每个 Lobe 的 Roughness / Anisotropy / Rotation / Offset
最终合成顺序与 Debug 输出
```

Painter 没有可靠传入 Mesh Tangent/Bitangent 时，可用宿主的 `tangentSpaceToWorldSpace` 分别转换切线空间单位轴 `(1,0,0)`、`(0,1,0)`、`(0,0,1)` 来重建 T/B/N。这样能继承宿主实际的 UV 方向和手性；不要只用 `cross(N,T)` 猜测副切线符号。

普通高光若使用项目相机偏置，不代表所有新增 Lobe 都应复用同一 View。Unity 源码使用原始 Eye Vector 的分支，在 Painter 中也应使用原始 Eye Vector。对齐失败时分别输出 raw V、biased V、H、T、B、N，而不是继续微调 Roughness。

MatCap 的坐标重点在矩阵语义：若 Painter 自动参数提供 View-to-World 法线矩阵，World-to-View 应使用它的转置或逆正交变换；之后再执行 UV Rotation 和 Flip Y。颜色 MatCap 仍按目标 Unity 纹理导入语义执行一次 sRGB → Linear。

### 5.5 共享遮罩、F0 和 Debug 覆盖新增分支

当一个 Alpha/Mask 同时控制 Ramp、各向异性和 MatCap 时，两端应先计算一次共享遮罩，再把同一结果传入所有分支。只对某个 Lobe 应用遮罩会产生“单项 Debug 看似正确，Final 不同”的假象。

Ramp 若在 Unity 中修改 `F0`，影响范围通常同时包含后续 Direct BRDF 和 IBL BRDF。Painter 只在 Direct 结果末端乘 Ramp 虽能匹配部分截图，但不等价于源 Shader。

Debug 应按最终合成职责更新：例如 `Direct Specular` 应包含普通高光、直接光各向异性 Lobe 和 MatCap 的总和；新增功能没有进入相应 Debug 时，Debug 一致不能作为该层已对齐的证据。

## 6. 环境反射与 Roughness / Mip 对齐

### 6.1 Debug 一致只能排除材质 Roughness 输入

如果 Roughness Debug 一致，但 Indirect Specular 不一致，问题位于以下链路之一：

- HDRI 内容或动态范围不同；
- 环境旋转、曝光、坐标系或相机不同；
- Unity Cubemap 与 Painter Environment 的卷积内容不同；
- Mip 数量、Roughness 到 LOD 的映射或采样滤波不同；
- HDR 编码/解码或混合所在的颜色域不同；
- IBL BRDF、F0、多重散射、AO/Shadow 或强度合成不同。

此时不应继续修改材质 Roughness、导出贴图或 Roughness Debug 来“配效果”。

### 6.2 先选择 Painter 原生 Environment 或 Unity 预卷积 Atlas

两条路径解决的目标不同：

| 方案 | 优点 | 边界 | 适用场景 |
| --- | --- | --- | --- |
| Painter 原生 Environment | 接入简单，背景与预览资源由 Painter 管理 | 即使使用同源 HDRI，预卷积 Mip 内容也不保证与 Unity 相同 | 日常制作、只需趋势接近或没有 Unity 导出工具 |
| Unity 预卷积 Atlas | Painter 直接使用 Unity 已卷积的各级 Mip | 需要明确 Atlas 布局、方向、过滤、HDR 解码和元数据契约 | 需要严格对齐环境高光宽度、轮廓和 Roughness 响应 |

Painter 原生路径通常用 `envSampleLOD()` 取样，采样后再执行目标 Unity Shader 的 IBL BRDF、F0、多重散射、AO 和强度逻辑。Unity Atlas 路径应保留原生路径作为 A/B 回退，但不应同时叠加两份环境高光。

### 6.3 把 Atlas 布局当作数据契约

一种可维护的 Unity 预卷积 Atlas 布局是“每个 Mip 一行，每行六个 Face”，面顺序固定为：

```text
+X, -X, +Y, -Y, +Z, -Z
```

每个 Face 可以带 Padding，各 Mip 的 Face Size 通常逐级减半。导出端应伴随 Atlas 记录 `layoutVersion`、`atlasWidth`、`atlasHeight`、`baseFaceSize`、`maxMip`、`padding`、`faceOrder`、`colorEncoding`、Unity 版本和构建平台。采样端必须按这份元数据填写参数，不能从文件名猜测。

这种多行 Mip Atlas 不是交给 Painter Environment 系统自动解析的标准单行 `6 Frames` Cubemap；它应以普通 2D Texture 导入，由自定义 Shader 手动定位 Mip 行、Face 和 Texel。

### 6.4 Roughness 只在环境采样边界转换为 LOD

若目标 Unity Shader 使用 URP 常见的感知粗糙度重映射，Atlas 路径可使用：

```text
perceptualRoughness = saturate(roughness)
perceptualRoughness *= 1.7 - 0.7 * perceptualRoughness
lod = perceptualRoughness * maxMip
```

`maxMip` 是最大 LOD 索引，不是 Mip 总层数。例如 LOD `0～6` 包含 7 层数据。这一转换只属于环境资源采样；不应为了配环境效果而改写材质 Roughness、Direct Specular 输入、Debug 数值或导出贴图。

Painter 原生 Environment 可能有不同的 Mip 数和响应曲线。若实测必须校准，只校准 Environment LOD 输入，并把 HDRI、导入/卷积链、Painter 版本与校准值留在项目 Profile；不把单次测试得到的比例升级为通用常量。

### 6.5 Face 方向、逻辑 UV 和存储 UV 必须分层

导出端的 `FaceUV -> Direction` 必须与采样端的 `Direction -> FaceUV` 互逆。先用 `abs(direction)` 选主轴，再按方向正负选 `+X/-X/+Y/-Y/+Z/-Z`；主 Face 选择和各面 UV 公式中任意一个符号不对，都会表现为环境特征跑到错面或旋转方向错误。

必须区分：

- **逻辑 Cubemap Face UV**：用于方向和面之间的双向换算；
- **Atlas 存储 UV**：用于定位 2D 纹理中的 Texel，可能需要 Face-local V 翻转。

Unity 与 Painter 的纹理 V 原点不同时，只在“逻辑 Face UV ↔ Atlas 存储 UV”边界翻转选中 Face 内的 V。禁止用 `direction.y = -direction.y` 代替，因为它会交换 `+Y/-Y` 的物理含义，典型现象是第三、第四个 Face 上下对调。也不应整张 Atlas 翻转，否则会同时改变 Mip 行顺序。

### 6.6 低分辨率 Mip 必须显式处理跨 Face 过滤

高 Roughness 会访问 `16x16`、`32x32` 等小 Face，半个 Texel 或一次错面采样就会被放大为六面接缝。普通 2D Atlas 的 Bilinear 不知道 Cubemap 邻接关系，不能直接跨 Tile 边界采样。

可用的手动路径是：

1. 由方向求主 Face 和逻辑 Face UV；
2. 转为存储 UV，再转为 `uv * faceSize - 0.5` 的 Texel 坐标；
3. 计算四个 Bilinear Tap；
4. Tap 越界时，将其 Texel 中心恢复为扩展 Face UV，转回方向，再映射到相邻 Face；
5. 通过 `texelFetch` 读取四个 Tap 并手动 Bilinear；
6. 对相邻两层 Mip 重复上述过程，再手动 Trilinear。

如果 Unity 导出端的 Padding 已在面外通过 Cubemap 方向采样烘焙，它包含 Unity 原生 Cubemap 跨面结果。实现时应优先评估是否直接读取这些 Padding Tap，而不是丢弃 Padding 后再做一套邻面重投影。后者仍然是合理的回退，但在三个 Face 相交的角点只能近似 GPU 硬件的 seamless Cubemap filtering。

### 6.7 HDR 必须在滤波前解码

Unity 导出的 Half EXR 可以是 Linear HDR，但 Painter 导入后的实际 GPU 表示仍需要在目标 Painter 版本中确认。如果资源被存为 `RGBA8 UNorm` 或 RGBM，`texelFetch(texture, texel, 0).rgb` 会丢失 Alpha 中的乘数，导致高亮过白、亮度/饱和度异常或 Mip 混合边界错误。

正确顺序是“每个 Tap 先从资源实际编码解码到 Linear HDR → Bilinear → Trilinear → IBL BRDF 合成”。禁止在 RGBM 编码域直接 Bilinear/Trilinear，也不能只对最终混合结果解码。

### 6.8 环境反射的“透视大小”先对齐相机

Cubemap 是方向数据，不包含景物深度；镜面中环境特征的屏幕大小会随逐像素 `viewDir`、投影和球体屏幕占比改变。因此“Painter 里高亮特征更大”不能直接归因为 Atlas UV 错误。

严格 A/B 必须同时对齐投影类型、FOV 数值与轴、Aspect Ratio、相机距离/旋转、模型姿态/缩放和球体屏幕占比。Unity `Camera.fieldOfView` 表示垂直 FOV；Painter 面板中同名数值的轴语义若未经当前版本验证，不应直接视为等价。最稳定的生产方式是从 DCC 向两端导入同一相机和输出比例，再用叠图或网格验证。

### 6.9 标定方法

使用同一灰球/材质球、HDRI、相机、曝光和后处理状态，测试 Roughness：

```text
0 / 0.1 / 0.2 / 0.3 / 0.4 / 0.5 / 0.6 / 0.7 / 0.8 / 0.9 / 1
```

分别保存 Roughness Debug、实际 LOD、Indirect Specular 和最终结果。如果多个连续 Roughness 档完全不变，先查 LOD 是否过早 `floor/round`、Mip 是否缺失/重复或相邻 Mip 是否未 Trilinear；不要先加偏移。校准目标是多个 HDRI 下环境高光的宽度、低频响应和跨面连续性，不是只匹配一张截图。

## 7. Environment 旋转、主光和 Painter 自动参数

Painter 的 `environment_rotation` 通常是 `0～1`，对应 `0～360°`。导入环境库不保证裸变量自动出现在自定义 Shader 作用域；应显式声明自动参数，例如：

```glsl
//: param auto environment_rotation
uniform float environmentRotation;
```

背景旋转与主光方位一比一同步，只能保证数值契约，不能保证 HDRI 内太阳的真实方位与背景角度零点一致。需要物理上跟随 HDRI 主光时，应优先使用宿主提供的主光自动参数，或增加经过标定的太阳方位偏移；不能把 `environment_rotation` 直接等同于任意 HDRI 的太阳方向。

出现“转到某个角度后正面无光”时，先计算正面法线与主光的 `N·L`。灯转到模型背面时画面变暗是方向结果，不是光强被清零。

## 8. Debug 和最小 Harness

### 8.1 Debug 名称、顺序和数值同时对齐

不能只让两端 Debug 名字相似。必须以 Unity 实际 Debugger 的显示顺序表和 Shader 常量为 Source of Truth，同时同步：

- 显示名称；
- UI 顺序；
- 底层数值；
- 输出发生在调整前还是调整后；
- 是否受后处理影响。

Painter 无法提供的 Unity 专有输入，例如顶点色 Smooth Normal、最终高度渐变、实时阴影或屏幕 Mask，不应返回一个看似合理的假结果。使用洋红色等明显错误色，并在文档标注“不支持”。

### 8.2 复杂 Shader 失败时先做最小颜色/光照 Harness

建立 Unity/Painter 成对的最小 Shader，只保留：

- 同一 Base Color 与可选 Normal；
- 手动光方向、光色和强度；
- 同一 Blinn-Phong 或最小 BRDF 公式；
- `Base Color Only`；
- 不使用 Ramp、IBL、阴影、Fog、Specular AA、RendererFeature 和项目全局参数。

使用方法：

1. `Base Color Only` 不一致：先排查贴图 sRGB、颜色参数域和显示/后处理；
2. Base Color 一致、直接漫反射不同：排查 Normal、`L`、坐标和光强；
3. 漫反射一致、高光不同：排查 `V/H`、F0、Roughness 和高光公式；
4. 最小 Harness 一致、复杂 Shader 不一致：按功能逐项恢复 Ramp、IBL、阴影和后处理，找到第一项回归。

自定义 Painter Shader 若输出完整光照结果，应避免再叠加 Painter 原生漫反射/高光。常见做法是把自定义最终结果写入 Emissive 输出，并把宿主的 Albedo、Diffuse Shading 和 Specular Shading 输出清零；具体接口以 Painter 版本验证。

## 9. 常见问题与修正表

| 现象 | 首要根因候选 | 修正与停止条件 |
| --- | --- | --- |
| 灯旋转但漫反射不变 | 空 Ramp Alpha 为 `1`，或旧打包通道覆盖原生输入 | 关闭 Ramp/移除旧通道，Direct Diffuse 恢复响应后再继续 |
| Ramp 产生针状黑点 | Repeat + Bilinear 跨首尾采样 | Clamp 到首尾半 Texel 中心；Alpha 保持 Linear |
| 同一 Ramp 在 Painter 偏淡 | Unity 将 Ramp RGB 作为 sRGB 颜色自动解码，Painter 未解码 | 只对 Painter Ramp RGB 执行一次精确 sRGB → Linear |
| 参数一致但直接高光位置不同 | `H` 公式、相机偏置、`V` 或光方向不同 | 对比 `N/L/V/H`；迁移同一半角向量公式 |
| 普通高光一致，各向异性方向仍不同 | TBN 手性、raw/biased View、方向旋转或法线切线扰动不同 | 输出 T/B/N 与两套 View；按源 Shader 分支逐项对齐 |
| Direct 高光一致，IBL 颜色不同 | Ramp 在 Direct 末端相乘，而源实现先修改共享 F0 | 把 Ramp 移到 F0 阶段并复核后续 IBL BRDF |
| MatCap 上下或旋转相反 | View-to-World/World-to-View 矩阵语义或纹理 V 约定混淆 | 先用纯方向法线图验证矩阵，再单独切换 Rotation/Flip Y |
| Alpha 变化只影响部分高光 | 各分支各自计算或遗漏共享遮罩 | 统一计算共享 Mask，并让 Debug 覆盖所有受控分支 |
| Roughness Debug 变化，环境高光不变或过白 | Environment 预过滤/LOD 链不同 | 保持材质 Roughness，转查 HDRI、Mip、LOD 和 IBL 合成 |
| Unity `0.9～1` 变化小，Painter 仍明显变化 | 两端最高 Mip 内容、数量和 Roughness 响应不同 | 先对比 Unity 预卷积 Atlas；仅在原生 Environment 路径校准 LOD 输入 |
| 连续几档 Roughness 环境高光完全不变 | LOD 过早取整、Mip 内容重复或缺少 Trilinear | 输出实际 LOD；保留小数 LOD 并在两层 Mip 之间混合 |
| Atlas 第三/第四 Face 上下对调 | 用世界方向 Y 翻转补偿纹理 V 原点 | 恢复 `+Y/-Y` 面语义；只翻转选中 Face 的存储 V |
| 高 Roughness 出现六面接缝 | 2D Bilinear 不知 Cubemap 邻接，或跨面 Tap 重投影与 Unity Padding 不同 | 对四 Tap 做跨 Face 处理并手动 Trilinear；对比直接读取预烘焙 Padding |
| Painter 中环境特征看起来更大/更小 | 投影、FOV 轴、Aspect、相机距离或模型屏幕占比不同 | 先用同一导入相机/叠图对齐 `viewDir`，不先改 Atlas UV |
| Half EXR 导入 Painter 后反射过白或偏色 | Painter GPU 资源实际是 RGBM/UNorm，但 Shader 只读 `.rgb` | 确认资源编码；每个 Tap 先解码为 Linear HDR 再过滤 |
| Painter LUT 后饱和度高、暗部更重 | 输出 Linear 值被 Painter 当显示编码使用 | 普通 SDR Color Profile 开启一次 sRGB 输出编码 |
| Painter LUT 高光被截断 | White Point 太低，LUT 前 `HDR / WhitePoint` 被 Clamp | 用 HDR 灰阶选最小覆盖值，两端 White Point 完全一致 |
| `C1503 undefined variable` | 自动参数未在主 Shader 显式声明 | 增加匹配语义的 `param auto` uniform |
| `C1038 redeclaration` | 同一 `shade()` 作用域重复局部变量名 | 搜索整个函数作用域，按职责改成唯一名称 |
| 两端 Direct Debug 一致但最终画面不同 | IBL、合成或后处理仍不同 | 不再改 Direct 参数，继续按层比较 Indirect/Final/Display |

## 10. 最小验证矩阵与回退

### 10.1 静态与编译

- Unity Shader 和 Painter GLSL 分别无新增编译错误；
- Painter `param custom` JSON 可解析，Uniform 无重复，函数作用域无重名；
- 通道、默认值、颜色空间和 Debug 表有书面契约；
- 项目专属校准未写成跨项目默认值。

### 10.2 行为验证

| 阶段 | 输入 | 通过条件 |
| --- | --- | --- |
| 材质 | 黑/白/灰/RGB 色块，四通道阶梯图 | 原始与调整后 Debug 数值一致 |
| 直接光 | 固定相机，方位 `0/90/180/270°` | 漫反射方向和高光位置符合约定 |
| 高级高光 | T/B/N 方向色、raw/biased V、单层 Lobe、三层 Lobe | 坐标基底、每层宽度/偏移/旋转和最终累加均可归因 |
| 共享遮罩 | Alpha `0/0.25/0.5/0.75/1` | Ramp、各向异性和 MatCap 使用同一遮罩响应 |
| MatCap | 纯方向测试图，Rotation `0/90/180/270°`，Flip Y 开/关 | View Space 方向、旋转和上下约定一致 |
| Ramp | 黑白强边界 Ramp，U 接近 `0/1` | 无边界黑点，RGB/Alpha 语义一致 |
| 间接光 | 同源 HDRI，Roughness `0～1` 每 `0.1` 一档 | 实际 LOD 连续，高光宽度可归因，校准不污染 Direct/Debug |
| Atlas 方向 | 六个纯色/带编号 Face，旋转 `0/90/180/270°` | `+X/-X/+Y/-Y/+Z/-Z` 方位正确，Face 内无镜像或上下反转 |
| Atlas 过滤 | 高亮靠近边/角的 HDRI，最高两档 Roughness | 六面边界无突然黑线/色线，相邻 Mip 过渡无跳变 |
| Atlas HDR | 超过 `1.0` 的灰阶与彩色高光 | 采样值与 Unity Linear HDR 对得上，无 RGBM 编码域混合 |
| 显示 | `Base Color Only`、线性灰阶、RGB/HDR 色块 | 无重复 Gamma，White Point 截断边界明确 |
| 最终 | 同模型、相机、HDRI、曝光和后处理 | 剩余差异属于已记录的宿主能力边界 |

### 10.3 回退

任何新校准、颜色转换或采样修正都应能单独关闭。出现回归时按相反顺序回退：先停用 Color Profile，再停用 Environment 校准、Ramp、项目高光偏置和其他合成项，回到最小 Harness；不要通过修改源贴图永久补偿尚未定位的显示差异。

## 11. 与本地预览链路的边界

本文定义 Unity/Painter Shader 之间的材质、光照和显示对齐方法；参数、贴图和灯光如何跨进程临时传输，见 [Unity 与 DCC 本地材质预览链路参考](../../ToolDevelopment/references/local-dcc-preview-link.md)。Preview Link 只能保证已声明的数据按契约传递，不能替代本文对 BRDF、环境卷积、相机、后处理和 Debug 分层的验证。
