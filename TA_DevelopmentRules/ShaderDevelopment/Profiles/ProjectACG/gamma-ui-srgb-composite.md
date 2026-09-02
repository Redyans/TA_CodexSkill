# ProjectACG Gamma UI / sRGB 合成 Profile

> **Profile ID**：`projectacg-gamma-ui-srgb-composite-v2`
>
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACG\Client`
>
> **事实快照**：`2026-08-18`
>
> **用途**：记录 ProjectACG 当前已经合入工程的 Gamma UI 增量方案、实际源码和 Renderer 配置、成功使用方式、已解决问题、性能边界和未闭环项。本文是项目 `PROFILE`，不得上升为跨项目 CORE。通用原理与其他架构见 [`references/gamma-ui-color-domain-and-renderer-feature.md`](../../references/gamma-ui-color-domain-and-renderer-feature.md)。

## 1. 当前工程基线

| 项目 | 当前事实 |
| --- | --- |
| Unity | `2022.3.62f3` |
| Color Space | `Linear`，`ProjectSettings/ProjectSettings.asset` 为 `m_ActiveColorSpace: 1` |
| URP Core | embedded `14.0.12` |
| URP Universal | embedded `14.0.12` |
| URP Universal Config | embedded `14.0.10` |
| UI 根 Prefab | `Assets/TEngine/Settings/Prefab/UIRoot.prefab` |
| UI Camera | `UICamera`，Overlay Camera，Layer/Culling Mask `UI/5`，Renderer Index `2` |
| UI Camera 后处理 | `m_RenderPostProcessing: 0` |
| UI Renderer | `Assets/AssetRaw/Settings/URP-UI-Renderer.asset` |
| Canvas | `Screen Space - Camera`，绑定 `UICamera` |
| Canvas 顶点色 | `m_VertexColorAlwaysGammaSpace: 0` |
| Gamma UI Shader | `Assets/Shader/UI/GammaUI/GammaUIDefault.shader` / `Valkyria/UI/GammaDefault` |
| Gamma Composite Shader | `Assets/Shader/RenderFeature/UICorrectGamma/GammaUIComposite.shader` |
| Gamma Feature | `Assets/Shader/RenderFeature/UICorrectGamma/GammaUICompositeFeature.cs` |

当前合入的是通用参考中的**架构 A：透明 Gamma UI RT + 专用 Gamma Shader + Composite**。它是增量迁移方案，不是“整个 UI Layer 使用内置 `UI/Default` 绘制到预填充 Gamma RT”的架构 B。

### UI Shader 定位索引

| 归类 | 位置/Shader 名称 | 当前用途 |
| --- | --- | --- |
| 工程旧 UI | `Assets/Shader/UI/Shader_UI.shader` / `Valkyria/UI/Default` | 工程自定义旧 Linear UI |
| Unity 内置 UI | Package/内置 `UI/Default` | `Material=None` 的普通旧 UI；不会进入当前 Gamma Draw |
| Gamma 普通 UI | `Assets/Shader/UI/GammaUI/GammaUIDefault.shader` / `Valkyria/UI/GammaDefault` | 当前普通 Image/颜色 Texture2D RawImage 的 Gamma A/B 材质 |
| Gamma Composite | `Assets/Shader/RenderFeature/UICorrectGamma/GammaUIComposite.shader` | 场景 Linear 与透明 Gamma UI RT 的全屏合成 |
| UI 资源/特效 | `Assets/AssetRaw/Shaders/S_UI_Trail.shader`、`S_UI_Error.shader`、`S_UI_CharacterPresentationComposite.shader`、`S_UIPopupBackgroundBlur.shader`、`S_AlphaUI_s.shader`、`S_TextOutline_s.shader` | 尚未统一进入 Gamma 路径 |
| UI 粒子候选 | `Assets/AssetRaw/Shaders/ES_BaseParticle.shader`、`ES_AdvancedParticle.shader`、`Dissolve AlphaBlend NoEdge Particle.shader` | 保留特效 Shader，不能替换成普通 Gamma Default |
| TMP/视频 | `Assets/TextMesh Pro/Shaders/`、`Assets/Plugins/AVProVideo/Runtime/Shaders/Resources/` | 独立颜色契约，当前未迁移 |

定位实际消费者时，从 `Image`/`RawImage`/TMP/粒子 Renderer 的材质与 Shader GUID 反查 Prefab/Scene/运行时加载链；不能只搜索文件名中的 `UI`。

## 2. 资源和序列化配置

### PACG-UI-01｜RendererFeature 引用链

`URP-UI-Renderer.asset` 当前引用：

| 对象 | 当前值 |
| --- | --- |
| Renderer GUID | `4691ddbe44b8abb408aa7146f956373e` |
| Feature fileID | `7342165098732146501` |
| Feature Script GUID | `1caa06ebad3f4a1a8352647c2d38c767` |
| Composite Shader GUID | `c03ede2b01f0439d9433b22f5310fccb` |
| GammaUIDefault GUID | `6e724c86902e4c32aa8e346cb6b4f7f5` |
| Renderer Feature Map | `454f33c0419ce465` |
| `featureEnabled` | `1` |
| `targetCameraName` | `UICamera` |
| `gammaUiLayerMask` | `32`，Layer 5 |
| `m_IntermediateTextureMode` | `1` |

`URP-Battle-Low/Mid/High/Ultra` 和 `URP-Lobby-Low/Mid/HighFidelity/Ultra` 的 Renderer Data List 第 3 项都引用该 UI Renderer，对应 `UICamera.m_RendererIndex = 2`。

移动 Shader、Feature 或 Renderer 资产时要同步 `.meta` 和序列化 GUID，不得只按 Shader 名称判断是否接入。

### PACG-UI-02｜当前 Renderer 仍保留 UI Layer

`URP-UI-Renderer.asset` 当前为：

```yaml
m_OpaqueLayerMask.m_Bits: 32
m_TransparentLayerMask.m_Bits: 32
gammaUiLayerMask.m_Bits: 32
```

这是架构 A 的配置：默认 Renderer 继续绘制 `UI/Default`、TMP 和旧特效；Gamma Feature 额外只抓取 `LightMode = GammaUI`。

不要把架构 B 的“Renderer Opaque/Transparent Mask 排除 UI”配置直接套到当前实现。当前 Feature 没有接管 `UI/Default` 的完整 ShaderTag 集合，先把默认 Mask 改为 `0` 会让旧 UI/TMP/特效消失。

### PACG-UI-03｜Feature 生效条件

`GammaUICompositeFeature.ShouldRender` 同时要求：

- `featureEnabled = true`；
- `QualitySettings.activeColorSpace == ColorSpace.Linear`；
- `CameraType.Game`；
- Camera Render Type 为 `Overlay`；
- `targetCameraName` 为空或等于相机名，当前为 `UICamera`；
- Composite Shader、相机 Color Target 和临时 RT 格式均可用。

因此 Feature 不会自动作用于 SceneView、Preview Camera、Base Camera 或其他名字的 Overlay Camera。

## 3. 当前执行时序和 RT

### PACG-UI-04｜四个 Pass 的真实职责

当前 Pass 顺序为：

```text
Gamma UI Forward Guard
    BeforeRenderingTransparents - 1
    全局 _GammaUIFeatureActive = 1

URP 默认 Draw Transparents
    旧 UI/TMP/特效正常绘制
    GammaUIDefault 的 UniversalForward fallback 被 clip 丢弃

Gamma UI Draw
    AfterRenderingTransparents
    只匹配 ShaderTag "GammaUI"
    RenderQueueRange.transparent
    SortingCriteria.CommonTransparent

Gamma UI Composite
    AfterRenderingTransparents + 1
    Camera Color → Scene Color Copy
    Scene Linear + Gamma RT → Camera Color Linear

Gamma UI Forward Guard Cleanup
    AfterRenderingTransparents + 2
    全局 _GammaUIFeatureActive = 0
```

Guard 解决的是 GameView 重复绘制：`GammaUIDefault` 为 SceneView 保留了 `UniversalForward` fallback，默认透明 Pass 会提交该 Pass；Guard 让其 Fragment 全部 `clip`，随后专用 Gamma Draw 再真正绘制。

这不是零成本排除。Gamma 材质仍可能在默认透明 Pass 产生一次被丢弃的 Draw/顶点/光栅提交，随后在 Gamma Draw 再提交一次。性能分析不能只数最终可见 Draw。

### PACG-UI-05｜临时 RT 契约

| RT | 当前配置 | 用途 |
| --- | --- | --- |
| `_GammaUIColor` | `R8G8B8A8_UNorm`、非 sRGB 数值存储、1x MSAA | 保存预乘的 Gamma/sRGB 编码 UI RGB 与正确覆盖率 |
| `_GammaUIDepth` | `DefaultFormat.DepthStencil`、独立 Depth/Stencil、1x MSAA | 让 Gamma Mask 与子节点在同一 Stencil 中完成 |
| `_GammaUISceneColorCopy` | 继承 Camera Color GraphicsFormat、无 Depth、1x MSAA | Composite 前保存场景 Linear 颜色 |

Gamma Draw 对 Color/Depth 执行透明清除。Composite 数学为：

```hlsl
sceneGamma = LinearToSRGB(max(sceneLinear.rgb, 0));
outputGamma = uiGammaPremultiplied.rgb
            + sceneGamma * (1 - uiAlpha);
outputLinear = SRGBToLinear(max(outputGamma, 0));
```

输出 Alpha 为：

```hlsl
uiAlpha + sceneLinear.a * (1 - uiAlpha)
```

最终写回 Linear Camera Color，后续 URP 输出链继续按原工程完成最终显示编码。

`R8G8B8A8_UNorm` 会把 Gamma UI 颜色限制在 SDR `0..1`，当前方案不承诺 Gamma UI HDR/超白发光。

## 4. Shader、纹理和 Canvas 契约

### PACG-UI-06｜`GammaUIDefault` 对齐内置 Default 的基础接口

当前 Shader 保留：

- `_MainTex`、`_Color`、`_TextureSampleAdd`；
- Stencil Comp/Ref/Op/ReadMask/WriteMask；
- `_ColorMask`、`UNITY_UI_ALPHACLIP`；
- SpriteAtlas、RectMask2D Softness；
- `Cull Off`、`ZWrite Off`、`ZTest [unity_GUIZTestMode]`。

Gamma Pass 使用：

```shaderlab
Tags { "LightMode" = "GammaUI" }
Blend One OneMinusSrcAlpha
```

Fragment 分别把贴图 RGB、Canvas 顶点色 RGB、材质色 RGB 从 Linear 恢复为 sRGB 编码数值，在 Gamma 域相乘，Alpha 保持覆盖率，并输出预乘颜色。

第二个 `UniversalForward` Pass 是 SceneView/未接入 Feature 相机的 Linear fallback。它只提供可见性近似，不保证与 GameView 的 Gamma 像素一致。

`GammaUIDefault` 不包含工程 `Valkyria/UI/Default` 的 `_MainColorIntensity`、`_MainOpaIntensity`、任意 Blend、Additive、Cull/ZWrite 扩展；Inspector 基础属性对齐不代表自定义功能完全对齐。

### PACG-UI-07｜当前颜色纹理必须保持 sRGB 开启

当前架构 A 的 Shader 明确假设普通颜色 Texture2D：

```text
sRGB (Color Texture) = On
采样得到 Linear RGB
GammaUIDefault 再 LinearToSRGB
写入非 sRGB Gamma RT
```

因此当前工程**不能把所有 UI 颜色图片统一关闭 sRGB**。`2026-08-18` 静态扫描结果：

```text
Assets/AssetRaw/UIRaw:
TextureImporter meta sRGB On  = 2897
TextureImporter meta sRGB Off = 2
其他 meta/非纹理 meta         = 89

Assets/AssetArt/Atlas:
SpriteAtlas sRGB On  = 50
SpriteAtlas sRGB Off = 0
```

这与当前 Gamma Shader 契约一致。两个 sRGB Off 资源需要按其实际数据用途判断，不能反向推导成全目录规则。Mask、Noise、SDF、Distortion 等数据纹理仍按数据处理。

如果未来切换到架构 B，用内置 `UI/Default` 直通 Gamma RT，才需要重新设计并批量迁移颜色纹理/Atlas 的 sRGB 契约；不能把两个架构的导入规则混用。

### PACG-UI-08｜当前 Canvas 必须进入 UICamera Renderer

`UIRoot.prefab` 当前根 Canvas：

```text
Render Mode   = Screen Space - Camera
Render Camera = UICamera
Layer         = UI/5
vertexColorAlwaysGammaSpace = false
```

测试 Canvas 若改成 `Screen Space - Overlay`，Frame Debugger 会看到 Feature 的 Guard/Draw/Composite，但实际 UI 随后出现在：

```text
UGUI.Rendering.RenderOverlays
Canvas.RenderOverlays
```

此时 UI 绕过 Gamma Draw，颜色修正不会生效。修复是恢复 `Screen Space - Camera` 并绑定 `UICamera`，不是继续调整 Shader 的 `pow`。

`vertexColorAlwaysGammaSpace = false` 是当前 Shader 的输入契约；不要只勾选该开关。若未来改为 `true`，必须同步删除/分支处理 Shader 对顶点色的再次 `LinearToSRGB`，并覆盖所有嵌套与运行时新建 Canvas。

### PACG-UI-17｜RectMask2D 真机失效案例：动态参数与变体剥离

#### 项目事实与现象

在 `GammaUIDefault` / `GammaUIDefaultSoftMaskable` 上，编辑器中 `RectMask2D` 可裁剪，打包真机曾出现 Image 完全不受矩形裁剪的现象。UGUI 本身的实现链路已在当前工程的 `com.unity.ugui@1.0.0` 源码中确认：`RectMask2D.PerformClipping` 计算合成矩形后调用 `MaskableGraphic.SetClipRect`，再由 `CanvasRenderer.EnableRectClipping` 写入当前 Renderer 的状态；`MaskableGraphic.Cull` 另行处理 `canvasRenderer.cull`。因此没有证据表明需要把 `RectMask2D` 换成 `Mask`。

目标 Shader、材质和剥离配置的事实来源：

| 项目 | 当前证据 |
| --- | --- |
| Image Shader | `Assets/Shader/UI/GammaUI/GammaUIDefault.shader` |
| SoftMaskable Shader | `Assets/Shader/UI/GammaUI/GammaUIDefaultSoftMaskable.shader` |
| 对照实现 | `Assets/Shader/UI/GammaUI/GammaUIAdditive.cginc` 使用普通 `multi_compile` 和 `UnityGet2DClipping` |
| 典型材质 | `Assets/AssetRaw/Materials/GammaUIDefault.mat`，静态材质 Keyword 为空 |
| 自定义剥离器 | `Assets/TEngine/Editor/ShaderStripping/TEngineShaderVariantStripper.cs` |
| 剥离 Profile | `Assets/TEngine/Editor/ShaderStripping/ShaderStrippingProfile.asset`：`Enabled = 1`、`EnableAssetsLocalComboStrip = 1`、`FallbackPolicy = 0 (KeepAll)` |
| SoftMask 注册 | `Assets/ProjectSettings/UISoftMaskProjectSettings.asset` 显式登记 `Valkyria/UI/GammaDefault` → `Hidden/Valkyria/UI/GammaDefault (SoftMaskable)`；当前静态变体条目主要看到 `SOFTMASKABLE` 与 `SOFTMASKABLE UNITY_UI_ALPHACLIP`，未看到 `UNITY_UI_CLIP_RECT` 组合，但这不等于最终包一定缺失 |

#### 根因判断与兼容性风险

最强根因候选是 `UNITY_UI_CLIP_RECT` 的 Keyword 作用域与本项目本地组合剥离策略不兼容：

1. 历史版本在两个 Shader 的 `GammaUI` 与 `UniversalForward` Pass 中使用 `#pragma multi_compile_local _ UNITY_UI_CLIP_RECT`。
2. `ShaderVariantUsageCollector` 以 `Material.enabledKeywords` 建立材质组合；没有启用 Keyword 的材质会记录为空字符串/空本地签名。
3. `TEngineShaderVariantStripper.GetLocalKeywordNameSet` 只把不可被全局覆盖的本地 Keyword 纳入签名；`multi_compile_local` 的 `UNITY_UI_CLIP_RECT` 会进入该集合。
4. 对 `Assets/` Shader 开启本地组合剥离后，材质只提供空组合时，裁剪变体可能被判断为未使用而从最终包剥除。运行时 `CanvasRenderer` 仍会设置裁剪矩形，但 Shader 没有 `UNITY_UI_CLIP_RECT` 分支可执行，表现为“整张 Image 可见”。

这是**构建链静态推导的高概率解释**，尚未用出问题的最终 Android 包的 Variant 日志或设备 Frame Debugger 证明。`FallbackPolicy = KeepAll` 只控制“完全没有采集到材质证据”的 Shader；材质已被收集且空组合形成一条记录时，仍可能进入本地组合剥离。

另一个真实但次优先的风险是参数布局：`_TextureSampleAdd`、`_ClipRect`、`_UIMaskSoftnessX`、`_UIMaskSoftnessY` 属于 CanvasRenderer 动态输入。若放在 `UnityPerMaterial`，在 SRP Batcher 与自定义 `GammaUICompositeFeature` 的 `context.DrawRenderers` 路径中可能读取默认值、前一个 Renderer 的值或过期矩形。它更能解释“矩形错误/串值/Softness 失效”，不能单独证明“完全不裁剪”。

#### 已落地修正与边界

当前工作区分支 `feature/TA_modelImportSetting` 的 HEAD `7f65cf994b` 位于相关修正之后，已包含以下 Shader 提交：

- `576efcc32e`：将 `_TextureSampleAdd`、`_ClipRect`、`_UIMaskSoftnessX/Y` 移出 `UnityPerMaterial`，两个 Shader 的两个 Pass 同步调整；
- `00fe97e973`：将 `UNITY_UI_CLIP_RECT` 与 `UNITY_UI_ALPHACLIP` 从 `multi_compile_local` 改为普通 `multi_compile`，两个 Shader 的两个 Pass 同步调整。

当前源码仍保留原有的 `UNITY_UI_CLIP_RECT` 计算和 SoftMask 分支；修复目标是保证 UGUI 动态参数和运行时 Keyword 能到达同一套裁剪逻辑，而不是改业务脚本或改变 Mask 类型。由于 Gamma UI 使用 `GammaUICompositeFeature` 的 `ShaderTagId("GammaUI")` / `context.DrawRenderers`，必须把 Shader 修复、RendererFeature 路径和变体构建作为一条链复验。

#### 验证证据、未验证项与回退

落地后按以下顺序验证：

1. 在 `RectMask2D` 下让 Image 明显越出边界；同一 Image 临时改用 `UI/Default`，确认 `validRect`、`canvasRenderer.cull` 和 RectMask2D 行为正常。
2. 换回 `GammaUIDefault`，在 Frame Debugger/RenderDoc 确认实际 `Used Shader/Pass = GammaUI`、当前 `_ClipRect` 为有限且正确的矩形，并确认 `UNITY_UI_CLIP_RECT` 变体存在。
3. 分别验证 `GammaUIDefault` 与 `GammaUIDefaultSoftMaskable`；覆盖子节点移动/缩放、嵌套 RectMask2D、非零 Softness、Alpha Clip 和 SceneView fallback。
4. 重新导入 Shader/Material，并重新收集、构建和替换 YooAsset/AssetBundle；不能用旧 Bundle 证明新 Shader 已进入包。保存 Shader stripping 日志或 Variant Capture，记录两个 Shader、两个 Pass 的实际保留组合。

当前仅有源码、提交历史和配置的静态证据；没有完成重新构建 Android 包、Unity Frame Debugger/RenderDoc 抓帧或真机 A/B。因此不能把 `00fe97e973` 已存在等同于“用户手上的问题包已修复”。若仍失败，先按“无 Draw（CPU Cull/Canvas/Layer）→ 有 Draw 但无像素（Pass/变体/参数）”分类，不要直接改 Prefab。

安全回退仍遵循 [Gamma UI 通用排查与回退参考](../../references/gamma-ui-color-domain-and-renderer-feature.md)：先恢复受影响 Image/子树的旧材质，再禁用 Gamma Feature，保留 Shader/Feature GUID 和 Bundle 引用，待资源与包体确认后再清理。不要把 RectMask2D 改成 Mask、用脚本 Alpha 伪造裁剪或删除 Gamma 材质来掩盖变体问题。

## 5. 正确使用和迁移边界

### PACG-UI-09｜普通 Image 的当前使用方法

1. 保持工程 Color Space 为 `Linear`。
2. 确认目标 Canvas 为 `Screen Space - Camera`，绑定 `UICamera`，对象 Layer 为 `UI`。
3. 保持颜色 Texture2D 和 SpriteAtlas 的 sRGB 开启。
4. 新建材质，Shader 选择 `Valkyria/UI/GammaDefault`。
5. 把该材质显式赋给需要验证或迁移的 `Image`。
6. 黑底白图测试 Alpha `0.3 / 0.5 / 0.8`，目标 SDR sRGB 像素约 `77 / 128 / 204`。
7. 再测不透明 `128/255` 灰色，以及两层 50% 白图约 `191/255`。
8. 用 Frame Debugger 确认对象出现在 `Gamma UI Draw`，并且 fallback Draw 被 Guard 丢弃。

`Material=None` 仍使用 Unity 内置 `UI/Default`，走旧 Linear UI。当前工程没有“新建 Image/RawImage 自动使用 Gamma Shader”的 Editor Hook，也没有全局默认材质替换。

普通颜色 Texture2D 的 `RawImage` 可以显式使用 Gamma 材质，但必须先确认其纹理输入域。RenderTexture、视频、Camera RT 和 HDR RT 不属于无条件兼容范围。

### PACG-UI-10｜旧 UI 和 Gamma UI 混用时的真实影响

- 旧 `UI/Default`、工程旧 UI Shader、TMP 和旧特效仍在线性相机目标中先绘制。
- Gamma UI 整组在其后 Composite；同一 Canvas 内新旧两条路径不能按 Sibling 精确交错。
- Feature 即使本帧没有可见 Gamma UI，也仍可能保留 RT 并执行 Scene Copy 和 Composite；旧 UI 理论颜色只经历一次标准 sRGB 往返，但仍有半精度误差、带宽和全屏成本。
- Mask Graphic 与被遮罩 Gamma 子树必须统一使用 Gamma 路径，否则独立 `_GammaUIDepth` 中没有旧路径写入的 Stencil。
- 同一 Canvas/子树需要严格层级时，整组迁移到 Gamma Shader；无法迁移 TMP/粒子/特效时，拆 Canvas 并显式设置 Canvas Sorting，不能依赖单个对象 Sibling 修复跨路径排序。

后续“全部 UI 替换为新 Shader”只有在普通 Image 范围内成立。TMP、Spine、Blur、Distortion、Additive/Multiply 和粒子必须保留自己的功能 Shader，并增加等价 Gamma Pass/颜色契约，不能替换成 `GammaUIDefault`。

### PACG-UI-11｜SceneView 是 Linear fallback，不是最终 Gamma 预览

当前 Feature 只运行 `CameraType.Game + Overlay + UICamera`，SceneView 不进入 Gamma Composite。`GammaUIDefault` 的 `UniversalForward` fallback 解决了“SceneView 完全不显示”，但 SceneView 仍是 Linear Blend 近似。

因此当前保证是：

```text
GameView/UICamera：目标 Gamma 合成
SceneView：对象可见、便于编辑，但不保证像素一致
```

若项目必须让 SceneView 与 GameView 像素一致，需要为 SceneView 建立等价 Gamma 路径，或接受 URP Renderer/源码级更大改造；不能把现有 fallback 结果当成已解决的一致性证明。

## 6. TMP、粒子、视频与 RenderTexture

### PACG-UI-12｜TMP 和 UI 特效当前仍是未迁移功能族

项目 `Assets/TextMesh Pro/Shaders/TMPro_Mobile.cginc` 存在：

```hlsl
#if (FORCE_LINEAR && !UNITY_COLORSPACE_GAMMA)
    color = SRGBToLinear(input.color);
#endif
```

当前 `.mat` 静态扫描未找到 `FORCE_LINEAR` 文本，但这不证明运行时材质、Keyword 变体或第三方 TMP Shader 均未开启。普通白字可能可用，彩色字、FaceColor、Outline、Underlay、Glow、Gradient、Emoji、SoftMask 和多材质 SubMesh 尚未作为 Gamma 路径验收。

UI 粒子继续使用粒子/特效 Shader。要进入当前架构 A，至少需要专用 `LightMode = GammaUI`、正确输入域、对应 Blend、Vertex Streams、Flipbook、Stencil/SoftMask 和 HDR/Bloom 证明。

### PACG-UI-13｜`LoginPersistentMediaRT` 没有 TextureImporter sRGB 开关

`Assets/GameScripts/AOT/Common/LoginSharedVideoSurface.cs` 当前创建：

```csharp
new RenderTexture(width, height, 0, RenderTextureFormat.Default)
```

名称为 `LoginPersistentMediaRT`，生产者是：

```text
VideoPlayer
VideoRenderMode.RenderTexture
```

RenderTexture 的颜色域由实际 GraphicsFormat、`RenderTexture.sRGB`、Active Color Space、VideoPlayer 写入行为和采样 Shader 共同决定，没有 TextureImporter 的 `sRGB (Color Texture)` 面板项。

当前尚未为该视频 RT 提供专用 Gamma RawImage Shader，也未验证直接套 `GammaUIDefault` 的结果。完成前不得把普通 Texture2D 的导入规则套到视频 RT。

## 7. 性能、验证状态和回退

### PACG-UI-14｜当前性能成本

以 `1920×1080` 为例：

```text
_GammaUIColor R8G8B8A8      ≈ 7.9 MiB
_GammaUIDepth                ≈ 7.9～15.8 MiB
_GammaUISceneColorCopy       ≈ 7.9～15.8 MiB（取决于 Camera Color 格式）
临时 RT 合计                ≈ 23.7～39.6 MiB
```

每帧主要新增：

- 1 次 Gamma UI RT Color/Depth Clear；
- 1 次 Camera Color → Scene Copy 全屏 Blit；
- 1 次 Gamma Composite 全屏 Blit；
- Gamma UI 专用 Draw；
- Gamma 材质 fallback 在默认透明 Pass 的被丢弃提交；
- 2 次场景 RGB 的标准 Linear/sRGB 转换。

RTHandle 会复用资源，不等于没有显存/带宽成本。移动端真机应记录 Render Scale、HDR、MSAA、Tile Load/Store、GPU Frame Time 和内存峰值；当前没有真机性能结论。

### PACG-UI-15｜本轮已确认和未确认

已确认：

- 当前 Feature、Shader、Renderer 与 UIRoot 等目标实现已被 Git 跟踪；本次规则更新只做静态核对，没有修改 Unity 工程文件。
- Unity/URP 版本、Feature 条件、Pass 时序、ShaderTag、RT 格式、Renderer Layer Mask、Canvas 模式和纹理/Atlas sRGB 状态已从当前磁盘文件静态核对。
- 用户在本轮交互中确认：测试 Canvas 进入 UICamera 路径后，50% Gamma UI 视觉测试结果正确。
- 当前实现已经增加 SceneView Linear fallback，并用 Forward Guard 避免 GameView 可见的 fallback 重复叠加。

未确认：

- 本次规则更新没有重新启动 Unity 做 Shader 导入、Console、Frame Debugger 或原始像素抓取；用户确认属于交互验收，不等价于自动化像素证据。
- 彩色 Image Tint、两层透明、完整 SpriteAtlas、Mask/RectMask2D/SoftMask 尚未形成系统截图矩阵。
- TMP 全功能、Spine、UI 粒子、Additive/Multiply、Blur/Distortion、Video/Camera RT、HDR/Bloom、SceneView 像素一致和移动端性能尚未闭环。

### PACG-UI-16｜当前架构的安全回退

1. 先把已迁移 `Image`/子树的材质恢复为原 `UI/Default` 或工程旧 UI 材质。
2. 确认场景、Prefab 和运行时不再引用 `Valkyria/UI/GammaDefault`。
3. 再禁用 `GammaUICompositeFeature`。
4. 保留 Shader/Feature `.meta` 与 Renderer 引用，直到资源引用和回归确认完成；不要先删 GUID。
5. 不修改当前 UI 颜色纹理/Atlas 的 sRGB 导入设置，因为当前架构没有要求批量关闭。

若未来改成架构 B，回退顺序必须另行更新为“先恢复 Renderer UI Layer Mask，再禁用 Feature，再恢复纹理/Canvas 输入契约”，不能沿用当前顺序。
