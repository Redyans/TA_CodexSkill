# ProjectACG Gamma UI / sRGB 合成 Profile

> **Profile ID**：`projectacg-gamma-ui-srgb-composite-v1`
>
> **适用工程**：`D:\work2025U3D\Valkyria\ProjectACG\Client`
>
> **用途**：记录当前 ProjectACG UI Gamma 方案的实际源码、Renderer 配置、颜色契约、使用方式、兼容边界和验证状态。本文依赖工程路径、Unity/URP 版本和序列化 GUID，不得上升为跨项目 CORE。通用原理见 [`references/gamma-ui-color-domain-and-renderer-feature.md`](../../references/gamma-ui-color-domain-and-renderer-feature.md)。

## 1. 当前工程基线

| 项目 | 当前事实 |
| --- | --- |
| Unity | `2022.3.62f3` |
| URP | `14.0.12` |
| 工程 Color Space | `Linear` |
| UI 根 Prefab | `Assets/TEngine/Settings/Prefab/UIRoot.prefab` |
| UI Camera | `UICamera`，Overlay Camera，Layer `UI/5`，Renderer Index `2` |
| UI Renderer | `Assets/AssetRaw/Settings/URP-UI-Renderer.asset` |
| Canvas 顶点色开关 | `m_VertexColorAlwaysGammaSpace: 0` |
| Gamma UI Shader | `Assets/Shader/UI/GammaUI/GammaUIDefault.shader` |
| Gamma UI Shader 名称 | `Valkyria/UI/GammaDefault` |
| Gamma Composite Shader | `Assets/Shader/RenderFeature/UICorrectGamma/GammaUIComposite.shader` |
| Gamma Feature | `Assets/Shader/RenderFeature/UICorrectGamma/GammaUICompositeFeature.cs` |

当前工程仍保留旧 Linear UI 路径；Gamma UI 是按材质和专用 `LightMode` 选择的增量路径，不是把整个 URP 或所有 UI 默认材质切换到 Gamma。

### UI Shader 定位索引

以下是当前仓库扫描到的 UI/特效相关源码位置。它们表示源码归属，不等价于“所有资源当前都被场景使用”；实际使用关系仍需从材质的 `m_Shader` GUID、Prefab/Scene 组件和运行时加载链确认。

| 归类 | 位置/Shader 名称 | 当前判断 |
| --- | --- | --- |
| 工程普通 UI | `Assets/Shader/UI/Shader_UI.shader` / `Valkyria/UI/Default` | 工程自定义旧 UI Default，不能与 Unity 内置 `UI/Default` 混为一谈 |
| Gamma UI | `Assets/Shader/UI/GammaUI/GammaUIDefault.shader` / `Valkyria/UI/GammaDefault` | 本 Profile 的 Gamma UI 普通 Alpha 路径 |
| Gamma Composite | `Assets/Shader/RenderFeature/UICorrectGamma/GammaUIComposite.shader` | `GammaUICompositeFeature` 的隐藏全屏合成 Shader |
| UI 资源/特效 | `Assets/AssetRaw/Shaders/S_UI_Trail.shader`、`S_UI_Error.shader`、`S_UI_CharacterPresentationComposite.shader`、`S_UIPopupBackgroundBlur.shader`、`S_AlphaUI_s.shader`、`S_TextOutline_s.shader` | UI 专用或 UI 相关 Shader，需逐材质确认颜色域和 Blend |
| UI 粒子/特效候选 | `Assets/AssetRaw/Shaders/ES_BaseParticle.shader`、`ES_AdvancedParticle.shader`、`Dissolve AlphaBlend NoEdge Particle.shader` | 粒子/特效路径，不应直接替换为 `GammaUIDefault` |
| 工程特效候选 | `Assets/Shader/Effect/Effect_Distortion.shader`、`Effect_Fresnel.shader`、`Assets/Shader/CustomEffect/Addtive.shader`、`Scroll2TexBend.shader` | 可被 UI 或场景复用，必须按实际消费者确认输入域 |
| 文本/视频插件 | `Assets/TextMesh Pro/Shaders/`、`Assets/Plugins/AVProVideo/Runtime/Shaders/Resources/` | 独立 TMP/视频颜色契约，当前未迁移到 Gamma UI |
| Unity 内置 UI | Unity/URP 内置 `UI/Default` | 不一定在 `Assets` 中；不能通过 `rg Assets` 找到源码后就假设不存在 |

定位 UI 实际用到的 Shader 时，先从目标 `Image`/`RawImage`/TMP/粒子 Renderer 的材质拿 Shader GUID，再回到上述源码；不要只搜索 `UI` 字符串，也不要把插件模板、编辑器预览 Shader 当成运行时 UI 结论。

## 2. 资源和序列化配置

### PACG-UI-01｜RendererFeature 引用链

`URP-UI-Renderer.asset` 当前包含一个 `GammaUICompositeFeature`：

| 字段 | 当前值 |
| --- | --- |
| Feature fileID | `7342165098732146501` |
| Feature Script GUID | `1caa06ebad3f4a1a8352647c2d38c767` |
| Composite Shader GUID | `c03ede2b01f0439d9433b22f5310fccb` |
| Renderer Feature Map | `454f33c0419ce465` |
| targetCameraName | `UICamera` |
| gammaUiLayerMask | `32`（Layer 5） |
| featureEnabled | `1` |

修改文件位置或移动目录时，必须同步 `.meta` 和 GUID 引用；不要只依赖 Shader 名称或文件名判断 Feature 是否已接入。

### PACG-UI-02｜Feature 的生效条件

`GammaUICompositeFeature.ShouldRender` 只有在以下条件同时满足时才运行：

- Feature 开关和 `featureEnabled` 都开启；
- `QualitySettings.activeColorSpace == ColorSpace.Linear`；
- 相机存在、类型为 `Game`；
- 相机为 `Overlay`；
- `targetCameraName` 为空或等于当前相机名（当前为 `UICamera`）。

因此它不是全局管线模式开关，也不会影响所有相机；但只要配置在 `URP-UI-Renderer` 并对 `UICamera` 生效，就会在该相机每帧增加 Gamma UI Draw/Composite 的资源和执行成本。

## 3. 当前执行时序和资源

### PACG-UI-03｜两个 Pass 的时序

`Create` 中的 Pass 时序为：

```text
Gamma UI Draw       AfterRenderingTransparents
Gamma UI Composite  AfterRenderingTransparents + 1
```

Draw Pass 使用 `ShaderTagId("GammaUI")` 和 `RenderQueueRange.transparent`，只绘制目标 Layer 的 GammaUI Pass。旧 `UI/Default` 或旧特效 Shader 不会因为该 Pass 被重新绘制。

Composite Pass 紧随 Draw Pass：先复制当前相机颜色到 `_GammaUISceneColorCopy`，再读取 Gamma UI RT 与场景副本进行全屏合成，写回相机颜色目标。

### PACG-UI-04｜临时 RT 契约

| RT | 当前配置 | 用途 |
| --- | --- | --- |
| `_GammaUIColor` | `R8G8B8A8_UNorm`、非 sRGB 数值存储、1x MSAA | 保存已经编码为 sRGB/Gamma 数值的预乘 UI 颜色 |
| `_GammaUIDepth` | `DefaultFormat.DepthStencil`、独立 Depth/Stencil、1x MSAA | 隔离 Gamma UI 的 Mask/Stencil 和深度状态 |
| `_GammaUISceneColorCopy` | 相机颜色目标描述符的非深度副本 | 保存 Composite 前的场景 Linear 颜色 |

Feature 会检查颜色和 Depth/Stencil 格式支持；不支持时打印一次警告并跳过本帧。`SetupRenderPasses` 在不同相机复用同一 Feature 时会先 `Reset` Pass 引用，避免把旧相机 RTHandle 带入当前帧。

## 4. Shader 颜色和材质契约

### PACG-UI-05｜`GammaUIDefault` 的兼容范围

`Assets/Shader/UI/GammaUI/GammaUIDefault.shader` 的目标是普通 UGUI Default 的兼容版，保留：

- `_MainTex`、`_Color`、`_TextureSampleAdd`；
- Stencil 比较/引用/操作/读写掩码；
- `_ColorMask`、`UNITY_UI_ALPHACLIP`；
- Sprite Atlas、RectMask2D、Cull/ZWrite/ZTest 的 Default 约定；
- 不绑定项目自定义 ShaderGUI，因此 Inspector 属性与内置 Default 兼容。

它没有把工程自定义 `Valkyria/UI/Default` 的 `_MainColorIntensity`、`_MainOpaIntensity`、任意 Blend、Additive 或 Cull/ZWrite 扩展混入兼容版。ShaderGUI 一致不代表颜色路径一致；颜色路径由 Pass、RT 和 Composite 决定。

### PACG-UI-06｜Gamma Shader 的核心差异

Shader 使用：

```shaderlab
Name "GammaUI"
Tags { "LightMode" = "GammaUI" }
Blend One OneMinusSrcAlpha
```

在 `vertexColorAlwaysGammaSpace = false` 的当前 Canvas 约定下，贴图 RGB、顶点色 RGB 和材质色 RGB 先按输入是 Linear 处理，再分别 `LinearToSRGB`，在 Gamma 数值域相乘。Alpha 只使用原始覆盖率，不转换；最后输出：

```hlsl
return half4(gammaRgb * alpha, alpha);
```

颜色贴图保持 `sRGB (Color Texture)` 开启；Alpha、Mask、SDF、Noise 和其他数据纹理不能按颜色贴图处理。

### PACG-UI-07｜Composite 的数学

`GammaUIComposite.shader` 假设：

- `_BlitTexture` 是场景 Linear 颜色；
- `_GammaUITexture` 是非 sRGB RT 中的预乘 Gamma UI 颜色。

核心流程为：

```hlsl
sceneGamma = LinearToSRGB(sceneLinear.rgb);
outputGamma = uiGammaPremultiplied.rgb + sceneGamma * (1 - uiAlpha);
outputLinear = SRGBToLinear(outputGamma);
```

最终写回 Linear 相机目标，由工程原有输出链路完成一次最终 sRGB 显示编码。不要在 Gamma RT 上启用 sRGB 自动写入，也不要再给 Composite 结果额外执行一次显示编码。

## 5. 使用、迁移与兼容边界

### PACG-UI-08｜当前使用方法

1. 保持工程 Color Space 为 Linear。
2. 确认 `URP-UI-Renderer.asset` 的 Feature 已启用，目标相机为 `UICamera`。
3. 新建材质，Shader 选择 `Valkyria/UI/GammaDefault`。
4. 把材质显式赋给测试 `Image`，不要先依赖自动默认材质替换。
5. 用黑底白 Sprite 测试 Alpha `0.3 / 0.5 / 0.8`，再测试不透明 `128/255` 灰色。
6. 通过 SDR sRGB 截图读取中心像素，目标约为 `77 / 128 / 204` 和 `128/255`。

### PACG-UI-09｜旧 UI 与新 Gamma UI 混用

- 旧 UI 不会被 GammaUI Draw Pass 重绘，因此已有 `UI/Default` 材质不会自动改变为 Gamma 视觉。
- Feature 即使没有 Gamma 材质，也可能分配/清除 RT、复制场景并执行 Linear→sRGB→Linear 往返，存在额外 GPU、带宽和量化成本。
- Gamma UI 在旧透明 UI 之后统一合成，跨两条路径的 Canvas 排序不保证。需要精确交错层级时，应统一迁移颜色路径或拆分 Canvas。
- Mask Graphic 和被遮罩的 Gamma UI 子树应统一走 GammaUI；不能依赖旧 Linear Pass 预先写入的 Stencil。
- 开关关闭后 GammaUI 材质没有匹配的 Draw Pass，通常不会显示；回退应恢复旧材质或关闭该材质使用，而不是删除 GUID/RT 配置。

### PACG-UI-10｜RawImage、粒子和后续 Shader

- 普通 `RawImage + Texture2D` 可以使用 Gamma UI Default，但必须确认纹理是颜色纹理。
- Camera RenderTexture、视频、HDR RT 和动态数据纹理不能直接套用，需要明确输入域和输出范围。
- UI 粒子继续使用粒子/特效 Shader，不应改成普通 `GammaUIDefault`。当前工程尚未提供 Gamma Particle Pass；后续应建立 `GammaUI/Particle` 或共享 Include，并单独验证 Vertex Streams、Flipbook、粒子 Alpha、Additive/Multiply、Bloom 和 HDR。
- TMP、Spine、Blur、Distortion、Additive 和 Multiply 当前均不属于已验证范围。它们应分别建立颜色域和 Blend 契约，不能只复制普通 UI 的 `Blend One OneMinusSrcAlpha`。

## 6. 验证状态

### 已有证据

- `0.3 / 0.5 / 0.8` 的 Gamma 数值目标分别为约 `77 / 128 / 204`；不透明 `128/255` 灰色应保持约 `128/255`。
- 旧版 GammaUIDefault 曾在 Unity ShaderCompiler 日志中成功编译 Vertex/Fragment D3D11。
- `GammaUICompositeFeature.cs` 曾通过独立 C# Harness 编译，记录为 `0 warning / 0 error`。
- 当前源码已做 UTF-8、括号、属性契约、Pass/LightMode 和 `git diff --check` 静态检查。

### 当前未闭环

- 当前最后一版 Gamma Shader/Composite/Feature 的 Unity 重新导入与完整 Console 日志尚未在本 Profile 更新时重新执行。
- 尚未完成 Frame Debugger/RenderDoc 的实际 Draw 顺序、RT sRGB 标志和 Stencil 读写确认。
- 新建 Image/RawImage 自动默认 Gamma 材质的 Editor Hook 尚未实现，也没有运行时动态创建覆盖。
- UI 粒子、TMP、Spine、Additive/Multiply、HDR/Bloom、RenderTexture/视频、相机堆叠、MSAA 和真机平台尚未验收。

交付或继续扩展前，必须把上述未验证项按风险补入项目验证矩阵；不能把静态检查或单张截图当作完整管线证明。

## 7. 维护规则

1. 移动 Shader、Feature、Composite 或材质目录时，同步 `.meta`、GUID、Renderer 资产和文档链接。
2. 修改 `LightMode`、RenderPassEvent、RT 格式、Blend、Stencil 或目标相机时，必须重新做时序和兼容回归。
3. 新增 Gamma UI Shader 时先确认它属于普通 Alpha、粒子、TMP、模糊、Additive、Multiply 还是 HDR 功能族，再决定是否复用 Include；不要把普通 Default 当通用特效基类。
4. 只在新建且没有显式材质的 Image/RawImage 上做自动默认赋材质，并限定 UICamera/UI Layer；已有资源不批量覆盖。
5. 每次交付记录实际 Unity 导入、截图像素、Frame Debugger、性能和未验证平台；项目事实只能留在本 Profile，不能复制进 CORE。
