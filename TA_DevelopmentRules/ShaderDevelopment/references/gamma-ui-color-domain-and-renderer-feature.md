# UI Gamma/sRGB 数值域与 RendererFeature 实现参考

> **类型**：`REFERENCE`
>
> **适用范围**：Linear Color Space 的 Unity UI、URP RendererFeature、独立 UI RenderTexture、透明混合、相机堆叠与 SceneView/GameView 排查。
>
> **使用前提**：必须用目标工程的 Unity/URP 版本、Canvas 模式、Renderer 资产、Shader、纹理导入和 Frame Debugger 重新验证。本文不替代项目 `PROFILE`。涉及 Pass、临时 RT、相机共享状态和 Stencil 时，同时遵循 `ARC-03`、`ARC-04`、`VAL-02`。

## 1. 颜色域与透明混合

### GUI-01｜把 UI 美术数值域和 3D 光照域分开定义

3D 场景继续使用 Linear 颜色域，保证光照、能量叠加、PBR、阴影与后处理的计算语义。普通 SDR 屏幕 UI 若要求“面板填 50% 灰看起来接近 50% 灰，白图 50% Alpha 压黑底得到约 `128/255`”，需要的是 sRGB/Gamma 编码数值域中的 UI 混合窗口，不是把整个工程改成 Gamma。

这里的“符合美术直觉”是指符合 Photoshop/传统 UI 合成的编码数值插值习惯，不代表 sRGB 数值在感知亮度上严格均匀，也不代表物理正确。

### GUI-02｜50% 是否显示为 50% 由 RGB Blend 所在域决定

设白色前景覆盖黑色背景，Alpha 为 `0.5`：

```text
Linear Blend:
mixedLinear = 1.0 * 0.5 + 0.0 * 0.5 = 0.5
displaySRGB = LinearToSRGB(0.5) ≈ 0.735 ≈ 188/255

Gamma/sRGB 数值域 Blend:
mixedGamma = 1.0 * 0.5 + 0.0 * 0.5 = 0.5
displaySRGB ≈ 0.5 ≈ 128/255
```

一般形式为：

```text
sceneGamma  = LinearToSRGB(sceneLinear)
mixedGamma  = uiGamma * alpha + sceneGamma * (1 - alpha)
outputLinear = SRGBToLinear(mixedGamma)
```

Alpha 是覆盖率/权重，不做 Gamma 编码。只转换 RGB；对 `pow(half4, ...)` 连 Alpha 一起处理会直接破坏覆盖率。

### GUI-03｜Shader 内补 `GammaToLinear/LinearToGamma` 不能抵消已经发生的 Blend

Fragment Shader 只能决定进入 Blend 单元的源值，不能改变固定功能 Blend 对源、目标执行运算时使用的 Render Target 数值域。若先在线性目标上完成了透明混合，再对结果做一次 `LinearToSRGB`，无法恢复本应在 Gamma 数值域逐层完成的中间结果。

正式实现使用 Unity/URP 提供的标准分段 sRGB 函数。`pow(x, 2.2)` / `pow(x, 1/2.2)` 只能作为近似说明，不应替代标准 sRGB 分段函数；尤其不能作用于 Alpha、负 HDR 值或未定义输入域的数据。

## 2. 两类可落地架构

### GUI-04｜先选择“增量专用 Shader”还是“完整 UI Gamma 窗口”

两类架构都可以保持 3D 和后处理在线性域，区别在迁移边界：

| 架构 | 绘制内容 | 普通 `UI/Default` | 优点 | 主要代价 |
| --- | --- | --- | --- | --- |
| A. 透明 Gamma UI RT + 预乘合成 | 只绘制显式 Gamma UI Shader | 不能直接接入透明 RT 主路径 | 对旧 UI 侵入小，适合 A/B 和渐进迁移 | 新旧路径不能跨组精确排序；每个功能族要有 Gamma Pass |
| B. 场景预填充 Gamma RT + 完整 UI 绘制 + Resolve | 将目标 UI 整组绘制到已含场景 Gamma 值的 RT | 输入契约满足时可以继续使用 | 同一 UI 绘制组内排序、Stencil 和默认材质更自然 | 需要接管整层 UI、调整 Renderer Layer Mask/Pass，并迁移纹理与顶点色契约 |
| C. 修改 URP 源码拆分透明物体与 UI | 在 URP 内增加 UI 前后插入点 | 可以 | SceneView/单 Renderer 时序可定制 | 长期维护 URP 分叉，升级和多平台回归成本最高 |

最小修复不是固定选 A 或 B，而是选择能闭合当前项目颜色、排序、Stencil、纹理和回退边界的最小方案。已经存在大量自定义 UI 特效且要求全局层级一致时，A 的渐进迁移成本可能高于 B；只需验证少量普通 Image 时，A 通常更安全。

### GUI-05｜架构 A 使用透明 RT 时必须输出正确的预乘颜色和覆盖率

透明 Gamma UI RT 的普通 Alpha Shader 推荐输出：

```hlsl
half3 gammaRgb = ...;       // 已是 sRGB/Gamma 编码数值
half alpha = saturate(...); // 线性覆盖率
return half4(gammaRgb * alpha, alpha);
```

对应：

```shaderlab
Blend One OneMinusSrcAlpha
```

这样多层标准 Source-Over UI 会在 RT 中形成正确的预乘 RGB 与整体覆盖率，随后可以：

```hlsl
outputGamma = uiGammaPremultiplied + sceneGamma * (1 - uiAlpha);
```

不能把未修改的直通 Alpha `UI/Default` 直接绘制到透明 RT 后，再把 RT Alpha 当作整体覆盖率。`Blend SrcAlpha OneMinusSrcAlpha` 同时作用于 Alpha 通道时，首层 Alpha 会写成 `a * a`；RGB/Alpha 不再满足后续预乘合成契约。除非 Shader 使用独立 Alpha Blend 因子并完整验证，否则应使用专用预乘输出。

### GUI-06｜架构 B 先把场景写成 Gamma 数值，再在同一目标绘制完整 UI

完整 UI Gamma 窗口的流程为：

```text
Camera Color（Linear）
    ↓ LinearToSRGB
非 sRGB Gamma UI RT，先保存场景 Gamma 数值
    ↓
目标 UI 以原生 Blend/Stencil/ColorMask/Sorting 绘制
    ↓ SRGBToLinear
Camera Color（Linear）
```

因为目标 RT 已含场景颜色，不需要从透明 RT Alpha 重建整体覆盖率。该架构可保留材质自己的 Blend、Stencil、ColorMask、ZTest/ZWrite 与 Cull，但要求：

1. 默认 Renderer 不再重复绘制同一 UI Layer，或有等价的可靠拦截；
2. Gamma Draw 支持实际 ShaderTag，例如 `SRPDefaultUnlit`、`UniversalForward`、`UniversalForwardOnly` 和项目专用 Tag；
3. 透明对象使用 `SortingCriteria.CommonTransparent`；
4. Mask 与子节点共享同一 Gamma Color RT 和 Depth/Stencil；
5. 颜色纹理、顶点色、材质颜色和自定义 Shader 输出都已经满足 Gamma 数值域契约。

不要用 `RenderStateBlock` 全局覆盖 Blend/Stencil/ColorMask 来“兼容所有 UI Shader”，这会破坏 Mask、Alpha Clip、Additive/Multiply、SoftMask 和自定义效果。

## 3. 输入、Shader 与资源契约

### GUI-07｜两种架构的纹理 sRGB 规则相反，禁止混用

| 输入 | 架构 A：专用 Gamma Shader | 架构 B：原生 `UI/Default` 直通 Gamma RT |
| --- | --- | --- |
| 普通颜色 `Texture2D` | 通常保持 `sRGB (Color Texture) = On`，采样为 Linear 后由 Shader 恢复为 sRGB 数值 | 若 Shader 不做恢复转换，则需要 `sRGB = Off`，让文件中的编码数值直通 |
| SpriteAtlas | 与源 Sprite 和最终 Atlas 的采样契约一致 | 最终 Atlas 也必须满足直通 Gamma 数值契约 |
| Alpha | 不做颜色转换 | 不做颜色转换 |
| Mask/Noise/SDF/Distortion/Lookup | 按数据纹理处理，通常 `sRGB = Off` | 同左 |
| RenderTexture/Video | 根据 `GraphicsFormat`、`RenderTextureReadWrite`、`RenderTexture.sRGB` 和生产者判断 | 同左，不能照搬 TextureImporter 规则 |

同一张颜色贴图若同时被 3D Linear 材质和 UI Gamma 直通路径使用，不能简单全局关闭 sRGB；应拆资源、拆采样路径或使用显式转换 Shader。

### GUI-08｜GammaUIDefault 对齐的是 UI 接口，不是逐行复制颜色路径

普通 UGUI Default 兼容版至少保留：

- `_MainTex`、`_Color`、`_TextureSampleAdd`；
- Stencil 比较、引用、操作、读写掩码；
- `_ColorMask`、`UNITY_UI_ALPHACLIP`；
- SpriteAtlas、RectMask2D、`Cull Off`、`ZWrite Off`、`ZTest [unity_GUIZTestMode]`；
- 与现有 Mask/RectMask2D 相同的 UV、顶点色和 Alpha 语义。

架构 A 的兼容 Shader 必须增加专用 `LightMode`、输入域转换和预乘输出，因此不可能与内置 `UI/Default` 内部逐行一致。ShaderGUI 只影响 Inspector、属性写入和 Keyword，不能改变 Blend 已经发生的域。

架构 B 可以继续使用内置 `UI/Default`，但“材质不变”不等于“资源契约不变”：颜色纹理直通、Canvas 顶点色和材质颜色仍需验证。

### GUI-09｜UI 特效、TMP 和粒子按功能族建立颜色与 Blend 契约

普通 Alpha UI 的结论不能直接套到：

```text
UI/Blur
UI/Distortion
UI/Particle
UI/Additive
UI/Multiply
UI/TMP
UI/Spine
UI/Video
```

这些功能可能依赖 Vertex Streams、Flipbook、SDF、屏幕颜色、Grab/Blit、HDR 强度、Bloom、独立 Alpha Blend 或多 Pass。需要逐族回答：

1. 输入 RGB 是 Linear 还是 sRGB 编码数值；
2. 数据纹理是否被误做颜色转换；
3. Blend 在哪一个 RT/数值域发生；
4. 输出是否预乘；
5. 是否需要 HDR，目标 RT 是否会截断；
6. ShaderTag、RenderQueue、Stencil、ColorMask 和 SoftMask 是否仍被保留。

TMP SDF Atlas 是数据纹理。字体颜色、Outline、Underlay、Glow、Gradient、Emoji 和多材质 SubMesh 要分别验证；若 Shader 有 `FORCE_LINEAR` 一类 Keyword，必须确认它不会在 Gamma RT 中再次把顶点色转成 Linear。

### GUI-10｜排序和 Stencil 只在同一实际绘制路径中可靠

架构 A 会把 Gamma UI 整组在旧 UI 之后合成，因此：

- Gamma UI 组内可按 `CommonTransparent` 排序；
- 旧 Linear UI 与 Gamma UI 之间不能依靠同一 Canvas 的 Sibling 顺序精确交错；
- Mask Graphic 和被遮罩子树应统一走同一个 Gamma Draw；
- 旧路径写入的 Stencil 不能假设在独立 Gamma Depth/Stencil 中存在。

架构 B 若把目标 UI 完整交给同一 Draw/DepthStencil，可以保留组内原生排序和 Mask；但 Opaque 与 Transparent 两个 Queue 仍不会仅凭 Sibling 顺序跨队列交错。需要精确交错时，统一 RenderQueue/绘制组或拆 Canvas，不要只挪 `RenderPassEvent`。

## 4. Canvas、默认材质与 RendererFeature

### GUI-11｜新建 Image/RawImage 不会因新增 Shader 自动改变默认材质

UGUI 新建 `Image`/`RawImage` 的默认路径仍来自 `Graphic.defaultGraphicMaterial` 和项目 Prefab/Editor 工作流。只新增 Shader、改 ShaderGUI 或创建材质都不会全局替换 `Material=None`。

自动赋材质若确有必要，默认只处理新建且 `material == null` 的对象，并限制目标 Canvas、Layer 和 Camera；不能覆盖已有显式材质、Prefab 或第三方组件。运行时动态创建对象需要独立入口。对于 RenderTexture、视频和 HDR RawImage，不能与普通颜色 Texture2D 共用无条件默认规则。

架构 B 若已经完整接管 UI Layer，普通 `Image`/颜色 Texture2D `RawImage` 可以继续 `Material=None`；但自定义特效 Shader 仍需满足 Gamma RT 输入输出契约。

### GUI-12｜Canvas 模式、相机过滤和 Layer Mask 是功能条件，不是面板细节

`Screen Space - Overlay` 的 UGUI 通常由 `Canvas.RenderOverlays` 在相机 Renderer 流程之外提交，RendererFeature 的 `DrawRenderers` 无法接管。要进入相机 Feature，使用：

```text
Render Mode   = Screen Space - Camera
Render Camera = 目标 UI Camera
Layer         = Feature/Renderer 约定的 UI Layer
```

相机、Renderer Index、Camera Stack、`CameraType`、Base/Overlay 类型、目标相机名称和 Feature Active 状态都必须与 `ShouldRender` 条件一致。

架构 B 若通过 Renderer Layer Mask 排除默认 UI，三处配置要形成单一集合关系：

```text
Camera Culling Mask              = UI
Renderer Opaque/Transparent Mask = 不包含 UI
Feature Gamma UI Layer Mask      = UI
```

否则可能默认绘制一次、Feature 再绘制一次。两个 `50%` Alpha 连续绘制的有效覆盖率为：

```text
1 - (1 - 0.5)^2 = 0.75
```

架构 A 需要默认 Renderer 保留旧 UI Layer，并用 ShaderTag/Guard 避免专用 Gamma Shader fallback 重复绘制；不能机械套用架构 B 的 Layer Mask 配置。

Canvas 的 `vertexColorAlwaysGammaSpace` 只定义顶点色输入，不会改变 Blend 域。根 Canvas 设置也不自动覆盖所有独立嵌套 Canvas；运行时创建的 Canvas 必须在创建点显式满足契约。

## 5. 排查、验证与方案取舍

### GUI-13｜用 Frame Debugger 故障签名先定位路径，再分析颜色数学

| Frame Debugger/现象 | 高概率根因 | 首要检查 |
| --- | --- | --- |
| Feature Pass 存在，但中间没有 UI Draw，之后出现 `UGUI.Rendering.RenderOverlays` / `Canvas.RenderOverlays` | Canvas 是 `Screen Space - Overlay` | 改为 Camera Canvas 并绑定目标 UI Camera |
| 默认 `Draw Transparents` 和 Gamma Draw 都出现同一 UI | Layer/Shader fallback 未排除，发生重复绘制 | Renderer Layer Mask、ShaderTag、Guard 与 fallback |
| GameView 显示，SceneView 不显示 | SceneView 不运行目标 Feature，Shader 只有自定义 LightMode | 增加受控 fallback 或为 SceneView 配置等价路径 |
| 组内排序正确，新旧 UI 交错错误 | 分属旧 Linear 与 Gamma Composite 两条路径 | 统一迁移同一 Canvas/子树或拆 Canvas |
| 白图 Alpha 正确，彩色 Sprite 偏暗/偏亮 | 纹理或 Atlas sRGB 契约与架构不一致 | 同时检查源纹理、Atlas 和 Shader 采样转换 |
| 普通 Image 正常，视频/Camera RT 偏暗 | RenderTexture 输入域未适配 | `RenderTexture.sRGB`、GraphicsFormat、生产者与专用 Shader |
| Mask 失效 | Mask 与子节点不共享 Draw/DepthStencil | Stencil 属性、ColorMask、Queue、RT |
| 自定义 Shader 消失 | LightMode/ShaderTag 不在 Feature 支持列表 | 实际 Pass Tag 与 Draw Settings |

遇到“50% 不对”时先确认 UI 实际进入哪个 Pass，再确认目标 RT 的 GraphicsFormat/sRGB 标志，最后才调整转换函数。不要从 Inspector 勾选项直接推断 GPU Blend 域。

### GUI-14｜用像素测试和多层用例建立最小验收矩阵

| 场景 | Gamma 数值域预期 | 用途 |
| --- | --- | --- |
| 黑底白图，Alpha `0.3/0.5/0.8` | SDR sRGB 约 `77/128/204` | 验证单层 Alpha Blend |
| 黑底两层 50% 白图 | 约 `191/255` | 验证多层覆盖率 `0.75` |
| 不透明灰 `128/255` | 约 `128/255` | 防止重复编码/解码 |
| 彩色 Sprite + Image Tint | 与美术参考一致 | 验证纹理、顶点色和材质色的乘法域 |
| SpriteAtlas/RectMask2D | 采样和裁剪不变 | 验证 Default 接口 |
| Mask 子树/SoftMask | 模板与软遮罩正确 | 验证共享 DepthStencil 与 Shader 接口 |
| TMP 彩色字/Outline/Underlay/Glow | 与目标参考一致 | 验证 SDF 功能族 |
| Additive/Multiply/粒子 | 单独参考图与 HDR/Bloom 契约 | 不能用普通 Alpha 结论代替 |
| RawImage Texture2D/Video/Camera RT | 分输入域一致 | 验证动态纹理 |
| SceneView/GameView | 明确是一致、近似或不保证 | 防止把 fallback 当最终输出 |
| 目标移动端 GPU | 带宽、RT 内存和 Tile Load/Store 可接受 | 静态编译不能替代 |

像素读取应来自无额外缩放、无未知后处理的 SDR sRGB 输出或原始抓帧。Inspector 预览和肉眼截图不够证明 RT 数值正确。

### GUI-15｜RenderTexture 没有 TextureImporter 的 sRGB 勾选项

RenderTexture 的颜色契约由以下共同决定：

```text
RenderTextureReadWrite
GraphicsFormat
RenderTexture.sRGB
创建时的 Active Color Space
生产者写入域
采样 Shader 的解码行为
```

因此“所有 UI 图片关闭/开启 sRGB”不能推广到 RenderTexture。若 RawImage 采样得到 Linear RGB、目标是 Gamma UI RT，专用 Shader 可只对 RGB 执行 `LinearToSRGB(sampleLinear.rgb)`，Alpha 保持不变，同时保留 UI Stencil、RectMask2D、Blend 和 ColorMask。

### GUI-16｜四相机 `OnRenderImage` 和 URP 源码分叉只作为有边界的备选

四相机流程“场景 → LinearToGamma → UI → GammaToLinear”在 Built-in/NGUI 中可以成立，但不适合作为 URP 默认实现：

- `OnRenderImage` 不是 URP 的正式 RendererFeature 路径；
- 多相机会增加 CPU Culling/Camera Setup、移动端 Tile Load/Store 和调试复杂度；
- `pow(half4, ...)` 会误处理 Alpha；
- `fixed`、默认 LDR RT 和粗略 `pow(2.2)` 会截断 HDR 或引入误差；
- 后处理和 Camera Stack 的前后顺序必须重新证明。

修改 URP 源码，把透明物体与 UI 拆成两个 `DrawObjectsPass`，再在 UI 前后插入转换，数学上可行，也更容易让 SceneView 单 Renderer 走同一时序；但会形成 embedded URP 分叉。升级补丁版本、Renderer 变体、2D Renderer、XR、PostProcess、FinalBlit 和 Camera Stack 都要回归。只有项目明确接受长期维护成本时才选择。

### GUI-17｜性能预算包含 RT 常驻、全屏带宽和额外提交

以 `1920×1080` 为例，不含对齐和平台压缩：

```text
R8G8B8A8 Color RT       ≈ 7.9 MiB
R16G16B16A16_SFloat RT  ≈ 15.8 MiB
32-bit DepthStencil     ≈ 7.9 MiB
64-bit DepthStencil     ≈ 15.8 MiB
```

架构 A 通常需要 Gamma Color、独立 DepthStencil、Scene Color Copy、至少两次全屏 Blit，以及 Gamma UI Draw；若为 SceneView fallback 在默认透明 Pass 提交后用 `clip` 屏蔽，还会增加一次被丢弃的几何/像素提交。

架构 B 通常需要 Gamma Color、DepthStencil、场景初始化 Blit、Resolve Blit 和完整 UI Draw。若改成“转换到临时 RT，再 Copy 回 Camera”两阶段 Pass，可能达到四次全屏 Blit。

RTHandle 复用可以避免每帧 C# 分配，但不消除显存常驻、Clear、带宽和 Tile Store/Load。性能结论必须以目标分辨率、Render Scale、HDR 格式、MSAA 和真机 GPU Profiler 为准。

### GUI-18｜实施和回退按架构成组执行

实施前冻结：

1. 目标是增量 A/B 还是整层 UI 接管；
2. 纹理、Atlas、顶点色、材质色、RT/视频的输入域；
3. Canvas 模式、UICamera、Renderer Index、Layer Mask 与 ShaderTag；
4. 普通 Alpha、Mask、TMP、粒子、Additive/Multiply 的功能范围；
5. SceneView 是像素一致、线性近似还是不保证；
6. 目标平台的 RT 格式和性能预算。

架构 A 的安全回退顺序：

```text
恢复受影响 Image/子树的旧材质
→ 禁用 Gamma Feature
→ 保留 GUID/Renderer 引用直到资源引用确认清理
```

架构 B 的安全回退顺序：

```text
先恢复 Renderer 默认 UI Layer Mask
→ 再禁用 Gamma Feature
→ 再恢复 Canvas 顶点色和纹理/Atlas sRGB 导入契约
```

不能先禁用 Feature 再保留“默认 Renderer 排除 UI”的配置，否则整层 UI 会消失。任何批量纹理重导入都应先生成清单和差异，避免把共享给 3D 的颜色贴图或数据纹理错误迁移。
