# UI Gamma/sRGB 数值域与 RendererFeature 实现参考

> **用途**：记录 UI 颜色空间、透明混合、独立 UI RT 和 RendererFeature 的通用判断与实现经验。本文是 `references/` 技术参考，不替代目标工程的 Shader、Renderer 资产、版本和平台 Profile。涉及 Pass、临时 RT、相机堆叠和 Stencil 时，应同时遵循 `ARC-03`、`ARC-04`、`VAL-02`，并以实际工程源码为最终事实源。

## 1. 结论先行

### GUI-01｜UI 的美术数值域和 3D 的光照域是不同目标

- 3D 场景使用 Linear 颜色域是正确的：光照、能量叠加、PBR 和阴影计算需要线性数值。
- 普通屏幕 UI 的主要诉求通常是“面板填 50% 灰，看起来就是 50% 灰；Alpha 填 50%，黑底白图得到约 128/255”。这属于 sRGB/Gamma 编码数值域的调试直觉，不等同于物理光照计算。
- 不能因为 UI 希望 Gamma 直觉，就把工程或 3D 相机改成 Gamma。正确边界是保留 3D Linear，在 UI 自己的绘制和合成窗口内使用 Gamma 数值域。

### GUI-02｜50% Alpha 是否得到 50% 显示值取决于 Blend 发生的域

设前景为白、背景为黑、Alpha 为 `0.5`：

```text
Linear 混合：0.5 Linear -> 编码到 sRGB 后约 0.735（约 188/255）
Gamma 混合：0.5 sRGB 数值 -> 显示约 0.5（约 128/255）
```

因此“Alpha 输入 50% 得到 50% 视觉效果”不是 Alpha 本身的 Gamma 问题，而是 RGB 的混合域问题。Alpha 是覆盖率/权重，始终按线性标量使用，不做 Gamma 编码。

### GUI-03｜只在 Shader 中调用 Gamma/Linear 转换不能改变固定功能 Blend 的颜色域

`LinearToSRGB` 或 `SRGBToLinear` 只能改变 Shader 输出或采样值。真正的颜色混合由 GPU Blend 在当前 Render Target 的数值域中执行：

```text
Cdst = Csrc * SrcFactor + Cdst * DstFactor
```

如果 Render Target 是 sRGB 格式，硬件还可能在写入/读取时自动编码或解码。把 `gammalinear` 反复包在 Shader 里，不能“抵消”已经发生的 Linear Blend；常见后果是双重编码、双重解码、半透明边缘发灰或不透明灰色失真。

## 2. 推荐架构：独立 Gamma UI RT + 最终合成

### GUI-04｜方案 3 是 Linear 3D 工程中的最小安全边界

推荐的职责分层如下：

```text
3D/旧 UI（Linear 路径）
        ↓
Gamma UI Shader 绘制到非 sRGB UI Color RT
        ↓  在 sRGB 数值域转换场景并合成
合成结果转回 Linear
        ↓
URP 后续输出链路只做一次最终显示编码
```

关键点：

1. 场景和旧 UI 保持现有 Linear 路径，避免全局行为变化。
2. Gamma UI RT 使用非 sRGB 的 `UNorm` 颜色格式，避免写入时被硬件再次编码。
3. UI Shader 输出已经是 Gamma/sRGB 编码数值；在 RT 内使用预乘 Alpha 混合。
4. Composite Shader 读取场景 Linear，临时转换为 sRGB 数值，与 UI Gamma 颜色合成，再转回 Linear 写回相机颜色目标。
5. 最终相机输出仍由工程原有 Linear/sRGB 输出链路负责，不能在多个层级重复编码。

### GUI-05｜普通 Alpha UI 的推荐输出契约

普通 UI Shader 在 Gamma UI RT 中应使用：

```hlsl
half3 gammaRgb = ...;       // 已是 sRGB/Gamma 数值
half alpha = saturate(...); // 线性覆盖率
return half4(gammaRgb * alpha, alpha);
```

对应 Render State：

```shaderlab
Blend One OneMinusSrcAlpha
```

这是预乘 Alpha。它允许多层 UI 在同一 Gamma 数值域累积，并避免把 Alpha 乘两次。若沿用直通 Alpha 输出，必须重新证明边缘、纹理预乘约定、Mask 和所有混合模式的一致性，不应只替换一个 Blend 因子。

### GUI-06｜输入颜色与数据纹理必须分开处理

在 Linear 工程中，当前 UI Shader 的输入契约需要明确写出来：

| 输入 | 普通颜色 UI 的处理 | 原因 |
| --- | --- | --- |
| 颜色纹理 RGB | 使用 `sRGB (Color Texture)` 导入，采样得到 Linear 后转为 sRGB 数值 | 保持贴图内容与美术颜色选择一致 |
| 材质颜色、顶点色 RGB | 按实际 Canvas/材质约定处理；若 Shader 假定输入 Linear，则转为 sRGB 数值 | 避免 Inspector 数值与 Blend 域不一致 |
| Alpha | 不做 Gamma 转换 | Alpha 是覆盖率，不是显示颜色 |
| Mask、Noise、SDF、Distortion、Lookup、UV 数据 | 默认不做 Gamma 转换 | 这些是数据，不是颜色；转换会改变阈值和形状 |
| HDR、发光强度 | 单独定义范围和合成规则 | 不能套用普通 SDR Alpha UI |

`Texture2D` 和 `RenderTexture` 不能按名字推断输入域。`RawImage` 使用普通颜色 `Texture2D` 时可复用普通 Gamma UI 契约；Camera RenderTexture、视频、HDR RT、动态生成的非颜色数据必须先确认其 sRGB 标志和生产者。

### GUI-07｜Vertex Color Always In Gamma Space 只改变输入，不改变 Blend

Canvas 的 `vertexColorAlwaysGammaSpace` 或同类开关只决定顶点色传入 Shader 的数值域，不会把 GPU Blend 变成 Gamma Blend。

- Shader 假定顶点色输入为 Linear 时，保持该开关关闭，并在 Shader 中统一转为 Gamma 数值。
- 开启后，必须使用 `UNITY_UI_VERTEX_COLOR_ALWAYS_GAMMA_SPACE` 等实际宏分支，避免对已经是 Gamma 的顶点色再次 `LinearToSRGB`。
- 不能把“勾选开关”当作独立 Gamma UI 方案；RT 格式、Pass 选择和最终合成仍需完整闭环。

## 3. Shader 兼容与功能边界

### GUI-08｜GammaUIDefault 应与普通 UI Default 对齐接口，不复制错误的颜色路径

普通 UI 兼容版至少应保留：

- `_MainTex`、`_Color`、`_TextureSampleAdd`、RectMask2D 相关输入；
- Stencil 的比较、引用、读写掩码和操作属性；
- `_ColorMask`、`UNITY_UI_ALPHACLIP`；
- Sprite Atlas、`Cull Off`、`ZWrite Off`、`ZTest [unity_GUIZTestMode]`；
- 与现有 Mask/RectMask2D 消费者相同的 UV、顶点色和 Alpha 语义。

兼容版内部必须有自己的颜色域实现：使用专用 `LightMode = GammaUI`，写预乘 Gamma 颜色到 Gamma UI RT。不能仅把 Shader 名称改为 `UI/Default`，也不能把工程自定义 UI Shader 的 `_MainColorIntensity`、`_MainOpaIntensity`、Additive、任意 Blend、Cull/ZWrite 扩展强行混进内置接口。

ShaderGUI 只能影响 Inspector 展示、属性写入和 Keyword，不能改变 Blend 已经发生的颜色域。GammaUIDefault 不绑定项目自定义 ShaderGUI 时，保留内置 Default 的属性名即可得到一致的基础 Inspector；若后续绑定 ShaderGUI，必须保持序列化属性和 Keyword 兼容。

### GUI-09｜UI 特效和粒子不能直接套用普通 GammaUIDefault

UI 粒子仍应使用粒子/特效 Shader，因为它们依赖 Vertex Streams、粒子颜色、Flipbook、软粒子、噪声、扭曲和不同 BlendMode。正确做法是为每个功能族建立 Gamma 版本或共享 Gamma UI Include：

```text
GammaUI/Default   普通 UGUI Image/RawImage
GammaUI/Blur      屏幕采样与模糊，明确采样域
GammaUI/Particle  粒子顶点流、粒子纹理与 Alpha/Blend 契约
GammaUI/Additive  加法与 HDR 强度契约
GammaUI/TMP       SDF、字体颜色与裁剪契约
```

粒子特效显示在 UI 层并不自动正确。只要它仍走旧 Linear 特效路径，就会和 Gamma UI 发生颜色域差异；只要它进入 Gamma UI RT，就必须实现 `GammaUI` Pass、输入域转换和对应的 Blend 证明。Additive、Multiply、Bloom、HDR 和发光强度不能直接使用 `Blend One OneMinusSrcAlpha` 的普通 Alpha 结论。

### GUI-10｜普通 UI 的 Gamma 合成不能跨路径假设排序和 Stencil

Gamma UI Draw Pass 通常在普通透明 UI 之后统一绘制，再一次性合成。因此：

- 旧 Linear UI 与 Gamma UI 之间的 Canvas 排序不会自动保持；需要同一颜色路径，或明确接受整组 UI 的层级边界。
- 普通 Mask 写入的 Stencil 不能假设 Gamma UI Draw Pass 可读；Mask Graphic 和被遮罩的 Gamma UI 子树应统一走 GammaUI Pass。
- RectMask2D 依赖 Shader 自己实现裁剪，普通和 Gamma Shader 需要分别保留该逻辑。
- 需要跨路径精确排序时，应先拆 Canvas/Renderer 或统一迁移颜色路径，不要用改变 Pass 时间的方式掩盖问题。

## 4. 默认材质与迁移策略

### GUI-11｜Unity 新建 Image 不会因为 Shader 名称自动换材质

UGUI `Image` 的新建默认材质来自 `Graphic.defaultGraphicMaterial` 和编辑器/Prefab 工作流。仅创建 `Valkyria/UI/GammaDefault`、修改 ShaderGUI 或修改 Shader 名称，都不会全局替换 Unity 的默认材质。

安全的自动赋材质钩子应满足：

1. 只处理新建对象且 `material == null` 的 `Image`；
2. 限定目标 Canvas、UI Layer、UICamera/Screen Space Camera 等工程边界；
3. 不覆盖已有显式材质、Prefab 约定或第三方组件材质；
4. `RawImage` 仅在输入是普通颜色 `Texture2D` 时默认接管，RenderTexture/视频/HDR 输入走显式配置；
5. 运行时动态创建的 Image 另行处理，编辑器导入钩子不能替代运行时赋值；
6. 提供关闭开关、日志和回退材质，避免导入/重载时批量改写资产。

在迁移期，先用显式材质做 A/B，确认颜色、Mask、特效和排序，再通过 Prefab 或编辑器钩子接管“新建默认”。不要为了自动替换而修改 Unity Package 或全局 Graphic 默认值。

### GUI-12｜RendererFeature 开关不是“是否勾选就修好颜色”的单一选项

启用 Feature 的含义应由真实 Pass、目标相机、LayerMask、RT 和消费者共同决定。禁用时，GammaUI 材质没有对应 Draw Pass，通常不会显示；启用时，旧 UI 不会被 Gamma Pass 重绘，但会增加 RT、Draw、Blit 和合成成本。

如果 Feature 每帧都创建/清除 Gamma RT 并执行场景往返，即使当前没有 Gamma UI，也可能产生近似 Linear→sRGB→Linear 的量化、精度和性能开销。若要消除空跑成本，应增加“本帧是否有 GammaUI Renderer”的可靠判断，而不是靠名称或材质猜测。

## 5. 方案选择与不推荐做法

| 方案 | 结论 | 主要问题 |
| --- | --- | --- |
| 全工程切 Gamma | 不推荐 | 3D 光照、PBR、后处理、外部纹理和平台输出全部改变，影响面远超 UI |
| 保持 Linear Blend，只在 UI Shader 内 `LinearToSRGB`/`SRGBToLinear` | 不可作为根治 | 转换发生在 Blend 前后，无法改变固定功能 Blend 的域，容易双重编码 |
| 关闭 sRGB 或把相机全部当 Gamma | 不推荐 | 破坏场景与纹理导入契约，不能只修 UI |
| 独立非 sRGB UI RT + Gamma Draw + Composite | 推荐 | 改动集中在 UI Renderer/Feature/Shader，需承担额外 RT、Blit、排序和功能迁移成本 |

## 6. 排查顺序

### GUI-13｜先查资源链，再查数学

遇到“50% 不像 50%”“透明边缘发灰”“开关勾了无效”时，按以下顺序建立证据：

1. 列出实际使用的 Shader 和材质消费者；区分 Unity 内置 `UI/Default`、工程自定义 UI Shader、粒子/特效 Shader 和 ShaderGraph。
2. 检查工程 `Color Space`、目标 Camera 类型、Canvas 渲染模式、Renderer Index、LayerMask 和 RendererFeature 序列化配置。
3. 检查 Shader 的 `LightMode`、Queue、Blend、ColorMask、Stencil、RectMask2D 和 Alpha Clip；不要只看 Shader 名称。
4. 检查颜色纹理的 sRGB 导入标志，确认 Alpha/Mask/SDF/Noise 没有被错误当作颜色转换。
5. 在 Frame Debugger/RenderDoc 中确认 Draw 是否进入预期 Pass、Gamma RT 是否为非 sRGB、Composite 是否读取正确的场景颜色和 UI 纹理。
6. 用黑底白图做 `0.3 / 0.5 / 0.8` Alpha 测试，再用不透明 `128/255` 灰色测试，区分“Alpha 数学错误”和“颜色域错误”。
7. 分别验证旧 UI、Gamma UI、Mask、RectMask2D、粒子/TMP、相机堆叠、MSAA、低配图形 API 和 HDR 输出。

### GUI-14｜最小验收矩阵

| 场景 | 预期 | 说明 |
| --- | --- | --- |
| 黑底白图，Alpha `0.3/0.5/0.8` | SDR sRGB 约 `77/128/204` | 验证 Gamma 数值域 Alpha 混合 |
| 不透明灰 `128/255` | 仍约 `128/255` | 防止“跳过编码”造成灰色失真 |
| 两层半透明白 | 与同域预乘公式一致 | 验证多层 Blend，不只验证单层 |
| SpriteAtlas/RectMask2D | 裁剪和采样不变 | 验证 Default 兼容接口 |
| Mask 子树 | Stencil 正确 | Mask Graphic 与内容必须同路径 |
| 无 Gamma 材质 | 旧 UI 视觉基本不变 | 另行记录额外 RT/Blit 成本 |
| Gamma 与旧 UI 交错排序 | 明确不保证跨路径顺序 | 需要产品决定迁移或拆 Canvas |

## 7. 验证与回退

新增 Shader/Include/RendererFeature 后，至少完成严格 UTF-8、属性/Pass/Tag/Include 静态检查和 Unity 导入编译；屏幕资源改动还要完成 Frame Debugger 时序检查、代表性材质截图和性能对比。文本编译成功不能替代运行时验证，Unity 未导入或目标平台未覆盖时必须在交付中明确标注。

回退顺序应为：关闭 Gamma UI 材质使用 → 关闭 UI Renderer Feature → 恢复原有 Linear UI 材质。不要直接删除 RT、GUID 或 Renderer 序列化引用；资产迁移时同步 `.meta` 并检查引用链。
