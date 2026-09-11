# NBShader 2D UI GammaUI 接入与排查参考

> **类型**：`REFERENCE`
>
> **适用范围**：Unity/URP 中将复杂粒子/特效 Shader 接入独立 `GammaUI` UI RenderTexture，并解决 UI 特效排序、重复绘制、颜色域和 SceneView/GameView 差异。
>
> **使用前提**：本文记录可迁移的实现方法与本次 ProjectACG 验证到的边界。使用前必须重新核对目标工程的 Shader、RendererFeature、Canvas、相机、纹理导入、URP 版本和变体剥离配置；项目事实以对应 `PROFILE` 为准。本文不承诺仅凭静态编译或 Inspector 预览得到最终视觉结论。

## 1. 适用场景与目标

NBShader 这类复杂特效通常同时服务于场景粒子和 UI 粒子/RawImage/Sprite。若 UI 需要与 `GammaUIDefault` 处在同一透明 UI 排序组，不能只给 Shader 添加一个 `LightMode` 字符串；必须同时闭合：

```text
材质模式 → Pass Intent/变体 → Shader Pass Tag
→ RendererFeature DrawRenderers → 普通 Forward fallback Guard
→ 独立 RT 的颜色/Alpha 合成 → Feature 关闭时回退
```

目标应先写清楚：

1. 是只修 UI 排序，还是同时修颜色观感；
2. 是只支持某个 UI 功能族，还是支持全部 2D UI 模式；
3. Multiply、屏幕扭曲、HDR/Bloom、SoftMask 是否在范围内；
4. SceneView 是“可见即可”“颜色接近”还是“与 GameView 像素一致”。

## 2. NBShader 的四种 2D UI 模式契约

当前 NBShader 材质协议中，以下枚举值表示 UI 来源：

| UI 模式 | `_MeshSourceMode` | GammaUI 适配条件 |
| --- | ---: | --- |
| 2D RawImage | `2` | `_TransparentMode == Transparent` |
| 2D Sprite | `3` | `_TransparentMode == Transparent` |
| 2D Material Texture | `4` | `_TransparentMode == Transparent` |
| 2D UIParticle | `5` | `_TransparentMode == Transparent` |

接入判断必须集中在一个可复用谓词中，不要只判断 `5`：

```hlsl
bool NBIsGammaUIEffectMaterial()
{
    return _MeshSourceMode > 1.5 && _MeshSourceMode < 5.5 &&
           _TransparentMode > 0.5 && _TransparentMode < 1.5;
}
```

C# 材质意图解析也必须使用同一范围：

```csharp
bool gammaUiPassEnabled =
    isNBShaderMaterial &&
    IsUIEffectMeshSource(meshMode) &&
    transparentMode == TransparentTransparent;
```

这样做的原因：

- 只判断 UIParticle 会导致 RawImage/Sprite/Material Texture 仍留在普通 Forward，排序继续断开；
- 不判断透明模式会把 Opaque/Cutoff 错误送入独立 UI RT；
- Shader 与 C# 意图不一致时，可能出现材质有 Pass、但变体收集或运行时判定不一致。

`_TransparentMode` 必须在 HLSL 输入中显式声明，并与 Shader 属性名保持一致；不能假设未声明的材质属性会自动可用。

## 3. Pass、排序与重复绘制

### 3.1 必须保留专用 Pass 和普通回退 Pass

NBShader 应保留：

```shaderlab
Pass
{
    Name "GammaUI"
    Tags { "LightMode" = "GammaUI" }
    Blend One OneMinusSrcAlpha
    ...
}

Pass
{
    Name "UniversalForward"
    Tags { "LightMode" = "UniversalForward" }
    ...
}
```

`GammaUI` Pass 只负责独立 Gamma UI RT；`UniversalForward` 负责 SceneView、Feature 关闭和未接入 Feature 的相机。不要删除普通 Pass 来“强制排序”，否则编辑器预览和 Feature 关闭时会直接消失。

### 3.2 Feature 开启时必须抑制普通 Forward

如果同一材质同时被默认透明 Pass 和 `GammaUI` Draw 绘制，会出现：

- 亮度加深/颜色变脏；
- Alpha 覆盖率重复；
- UI 看起来排序错乱；
- Frame Debugger 中同一对象出现两次 Draw。

推荐在共享片元入口加 Guard：

```hlsl
#if defined(NB_GAMMA_UI_PASS)
    if (!NBIsGammaUIEffectMaterial() || !NBSupportsGammaUIBlend())
        clip(-1.0h);
#else
    if (_GammaUIFeatureActive > 0.5 &&
        NBIsGammaUIEffectMaterial() &&
        NBSupportsGammaUIBlend())
        clip(-1.0h);
#endif
```

Guard 是运行时 Feature 状态的防重绘机制，不是排序机制本身；它必须与 Feature 的设置/清理生命周期成对存在。

### 3.3 Feature DrawSettings 必须可证明地匹配

`GammaUICompositeFeature` 至少要核对：

- `ShaderTagId("GammaUI")`；
- `RenderQueueRange.transparent`；
- `SortingCriteria.CommonTransparent`；
- UI Layer Mask；
- Canvas 为 `Screen Space - Camera`，Render Camera 为目标 UI Camera；
- UI Camera、Renderer Index、Camera Stack、Feature Active 条件一致。

`Screen Space - Overlay` 会绕过相机 RendererFeature 的 `DrawRenderers`；Frame Debugger 里看到 `Canvas.RenderOverlays` 不代表 Gamma Pass 失效，而是 Canvas 没有进入该 Feature 的绘制路径。

### 3.4 LightMode 不是排序开关

`LightMode = "GammaUI"` 只是把 Pass 暴露给对应 `ShaderTagId`。最终排序由：

```text
RenderQueue + SortingCriteria + Canvas/Renderer 提交顺序 + Camera/Layer
```

共同决定。只改 Tag、不改普通 Forward Guard、Layer、Canvas 和材质意图，不能解决重复绘制或跨路径排序。

## 4. 颜色域契约：计算保持 Linear，最终输出只转换一次

### 4.1 推荐的 NBShader 颜色管线

对于 Game 的 GammaUI Pass，推荐：

```text
贴图/顶点色/材质色采样与特效中间量
    ↓
Linear 域计算：Dissolve、Emission、Ramp、Fog、ColorAdjustment、光照/扭曲
    ↓
最终 RGB 一次 Linear → sRGB
    ↓
按 GammaUI RT 契约预乘 Alpha
    ↓
Blend One OneMinusSrcAlpha 写入非 sRGB UNorm RT
```

不要在每个特效步骤中重复 Gamma：

```text
采样后 Gamma → ColorAdjustment → 再 Gamma
```

也不要把 `_LinearToGamma` 旧标记直接当成 GammaUI 最终输出开关，否则可能造成二次编码。GammaUI Pass 应由自己的最终输出分支统一负责一次转换。

### 4.2 与 GammaUIDefault 的“一致”要分层

“和 UI 一致”有两种不同含义：

1. **输出契约一致**：两者都向非 sRGB Gamma RT 写入预乘 RGB/线性 Alpha，并使用 `Blend One OneMinusSrcAlpha`。这是排序/合成所需的最低一致性。
2. **基础颜色数学完全一致**：`GammaUIDefault` 可能将贴图 RGB、顶点 RGB、材质 RGB 分别恢复到 sRGB 数值域后再相乘；NBShader 为保留特效语义，通常先在线性域完成复杂效果，再在末端编码。两者不一定逐像素相同。

因此验证报告必须明确写“最终输出契约一致”还是“基础颜色计算一致”，不能只写“颜色已统一”。

若要严格复刻 `GammaUIDefault`，必须只对 NBShader 的 2D UI 基础颜色分支做 A/B：

```text
颜色贴图 RGB、顶点色 RGB、材质色 RGB
→ 分别 LinearToSRGB
→ Gamma 数值域相乘
→ Alpha 保持线性
→ 预乘并写入 Gamma RT
```

该改动会改变特效颜色行为，必须独立批准和验收；不要把它作为 LightMode/排序修复的隐含副作用。

### 4.3 RT 与输入纹理不可混用规则

- 普通颜色 Texture2D/SpriteAtlas 的 sRGB 导入状态必须与采样函数契约一致；
- Alpha、Mask、Noise、SDF、Distortion、Lookup 等数据通道不得套用颜色转换；
- RenderTexture、Video、Camera RT 没有 TextureImporter 的统一 sRGB 勾选语义，必须核对 `GraphicsFormat`、`RenderTexture.sRGB`、生产者和采样 Shader；
- Gamma UI 使用 `R8G8B8A8_UNorm` 时，HDR/超白 RGB 会被截断，不得宣称支持 HDR/Bloom。

## 5. Blend 边界与不支持项

### 5.1 Alpha/Premultiply/Additive

独立 Gamma RT 的复合通常要求：

```shaderlab
Blend One OneMinusSrcAlpha
```

片元输出应满足：

```hlsl
rgb = gammaRgb * alpha;
a = alpha;
```

NBShader 的 Alpha/Premultiply/Additive 需要逐一确认最终是否满足该预乘契约。Additive 若复用固定 `One OneMinusSrcAlpha` 合成，必须验证 alpha 不会错误遮暗场景。

### 5.2 Multiply 不应静默伪装成 GammaUI

Multiply 依赖场景目标颜色；独立透明 Gamma RT 没有实时场景 destination，不能仅通过普通预乘输出等价表达。因此推荐：

- Multiply 留在 `UniversalForward`；
- `NBSupportsGammaUIBlend()` 明确返回不支持；
- 需要 Gamma 排序时，另设计带场景输入的合成路径，而不是把 Multiply 硬塞进透明 RT。

同理，屏幕颜色扭曲、Grab/Camera Opaque、Blur、Video、HDR Bloom 不能因“进入 UI”自动视为已兼容。

## 6. SceneView/GameView 差异与可维护方案

### 6.1 当前常见差异根因

GameView 可能执行：

```text
Gamma UI Draw → Gamma UI RT → Gamma Composite → Camera Color
```

SceneView 常常执行：

```text
UniversalForward fallback → 编辑器显示链
```

因此颜色深浅不一致可能来自：

- 不同 Pass；
- 不同 Blend 域；
- 是否执行最终 Linear→sRGB；
- 是否预乘 Alpha；
- 不同 Camera/Renderer/后处理/HDR/分辨率；
- SceneView 的 Gizmos、Scene Lighting 和编辑器显示链。

不能把 SceneView 与 GameView 的肉眼差异直接归因于材质颜色。

### 6.2 方案取舍

| 方案 | 结果 | 难度 | 维护度 | 推荐 |
| --- | --- | ---: | ---: | --- |
| 共享颜色函数/输入契约，SceneView 保持 fallback | 颜色接近；不保证像素一致 | 中 | 好 | 首选 |
| SceneView 也运行完整 GammaUI Draw/Composite | UI 路径更接近 Game | 中高 | 中 | 有明确需求再做 |
| 修改/分叉 URP 以统一 SceneView 时序 | 控制力最高 | 高 | 差 | 不推荐 |

完整 SceneView Gamma 路径不能只删掉 `cameraType == Game`：

- SceneView 是 Base Camera，不是当前 UICamera Overlay；
- SceneView 使用的默认 Renderer 可能没有 Gamma Feature；
- 需要处理多窗口 SceneView、编辑器相机、Gizmos、Scene Lighting 和全局 Shader 状态；
- 必须重新验证 UI Layer、Canvas、Depth/Stencil、RT 生命周期和 Feature 关闭回退。

因此交付前先声明一致性目标：

```text
GameView 最终正确
SceneView 可见
或
SceneView/GameView 颜色近似
或
SceneView/GameView 像素一致
```

## 7. 变体、意图解析与回退

### 7.1 C# 与 Shader 必须使用同一模式协议

当 Shader 增加新专用 Pass 时，检查：

```text
Shader 属性枚举
→ MaterialIntentProtocol 常量
→ MaterialIntentResolver
→ PassFeatureCatalog
→ Variant/Stripper/Collector
→ 实际材质和运行时动态材质
```

GammaUI Pass 作为核心 Pass 时，不能只依赖质量档 Feature；否则运行时 Feature 已开启但变体可能被剥除。

### 7.2 Feature 关闭必须可用

Feature 关闭时：

- 不设置 `_GammaUIFeatureActive` Guard；
- 不执行 GammaUI Draw/Composite；
- 材质仍可通过 `UniversalForward` 显示；
- 不应要求用户手动切换 Shader Pass 或重新制作材质。

Feature 开启时：

- GammaUI 材质进入专用 Draw；
- 普通 Forward 不重复绘制；
- 不支持的 Blend 模式继续走普通路径；
- Frame Debugger 可证明实际 Used Shader/Pass。

## 8. 最小排查与验证矩阵

### 8.1 排查顺序

```text
1. Color Space 是否 Linear
2. Feature 是否 Active、ShouldRender 是否满足
3. Canvas 是否 Screen Space - Camera
4. Camera/Renderer/Layer Mask 是否匹配
5. Frame Debugger 是否出现 Gamma UI Draw
6. 是否同时出现 UniversalForward 重复 Draw
7. 实际 Used Shader/Pass 和变体是否正确
8. RT GraphicsFormat/sRGB/MSAA/DepthStencil 是否符合契约
9. 颜色贴图、Atlas、顶点色和材质色输入域
10. 最后才调颜色公式
```

### 8.2 最小矩阵

| 用例 | 必查结论 |
| --- | --- |
| 四种模式 `2/3/4/5` | 都能进入 GammaUI Draw |
| Feature 开/关 | 开启不重复；关闭仍走 UniversalForward |
| Opaque/Cutoff | 不进入 GammaUI |
| Alpha/Premultiply/Additive | 逐项确认预乘与覆盖率 |
| Multiply/屏幕扭曲 | 明确不支持或使用独立场景输入方案 |
| RectMask2D/Mask/SoftMask | Draw、Stencil、动态裁剪参数和变体正确 |
| SceneView/GameView | 明确是可见、近似还是像素一致 |
| 彩色 Sprite/Atlas | 颜色输入域和基础颜色计算符合预期 |
| RawImage Texture2D/RenderTexture/Video | 分别验证输入域，不套统一 sRGB 结论 |
| 目标设备/构建包 | 变体、Bundle、Frame Debugger/RenderDoc、GPU 成本闭环 |

静态 `dotnet build` 和 `git diff --check` 只能证明源码/文本基本完整，不能证明 Unity Shader 导入、Frame Debugger、真实排序、像素颜色或移动端性能。

## 9. 安全回退和维护清单

发生异常时按功能族回退，不要先删除 Pass 或资源：

```text
确认实际 Draw/Pass
→ 对受影响材质恢复 UniversalForward 或旧材质
→ 禁用 GammaUI Feature
→ 保留 Shader/Feature/Renderer GUID 和引用
→ 重新导入并重建变体/AssetBundle
→ 用 GameView/Frame Debugger 做 A/B
```

维护时每次改动至少记录：

- 支持的 `_MeshSourceMode` 和 `_TransparentMode`；
- 支持/不支持的 Blend 和屏幕输入；
- GammaUI 与 UniversalForward 的 Guard 条件；
- ShaderTag、RenderQueue、SortingCriteria、Layer Mask；
- RT 格式、sRGB/MSAA/DepthStencil；
- SceneView/GameView 一致性目标；
- 静态检查、Unity 导入、Frame Debugger、设备和包体验证状态。

不推荐：

- 只加 `LightMode` 不改重复绘制 Guard；
- 只改 Shader 不改 C# Pass Intent/变体；
- 把 Opaque/Cutoff/Multiply 强行送进透明 Gamma RT；
- 用 `_LinearToGamma` 或 `pow(2.2)` 作为临时颜色补偿；
- 用 SceneView 颜色代替 GameView 最终验收；
- 把“Shader 编译通过”写成“排序/颜色/真机已验证”。
