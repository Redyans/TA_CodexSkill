# ProjectACG 人物 Mask 与扰动描边开发 Profile

> 类型：PROFILE；Profile ID：`projectacg-custom-post-processing-outline-v1`。
>
> 事实快照：2026-10-20。实际核对工程：`D:\work2025U3D\Valkyria\ProjectACGMain2\ProjectACG\Client`。本轮文档会话所在工作区是 Main3，但本文没有把 Main3 当成效果代码真相源。
>
> 用途：沉淀本轮人物火焰/灵体效果的集成事实、问题根因、实现选择与验证边界，供后续开发参考；不是单个材质的操作手册。通用方法见 [Mask、扰动与软边参考](../../references/screen-space-mask-distorted-outline.md) 和 [性能、制作与验证参考](../../references/screen-space-effect-production-and-validation.md)。

## 1. 项目事实与目标边界

### 1.1 需求落点

开发需求从现有自定义后处理人物描边出发，增加人物外缘火焰般流动扭曲、可调外圈颜色、实心 Mask 输入、相机距离补偿和可柔化边缘。后续又涉及低端 Android 30 FPS、降分辨率、可选多级 Kawase、多个预设、粉蓝效果专用化，以及 Shader 正式目录迁移。

用户明确将模糊总开关放在 RendererFeature，将分辨率、模糊参数等镜头相关设置放在 Clip；材质承担艺术外观。用户当时使用的 Timeline 片段没有保存，要求改好代码后在自己的当前场景复验。因此，本记录不把未保存片段描述为已交付 `.playable`，也不据此生成或恢复场景资产。

`video(13).mp4`、Unity URP 能量扰动文章及外部 `Irelans_DistortedOutline-main` 工程用于视觉/算法参考。视频和文章不构成精确像素对齐或真机性能证据；外部 Shader 的框架、路径和材质数据需要适配当前工程协议。

### 1.2 版本及目录

| 项目 | 2026-10-20 核对结果 |
| --- | --- |
| Unity | `2022.3.62f3`，来源 `ProjectSettings/ProjectVersion.txt` |
| URP / Core | embedded `14.0.12`，来源 `Packages/packages-lock.json`；实际编译仍以当前包源码为准 |
| 正式渲染目录 | `Assets/Shader/RenderFeature/CustomPostProcessing/` |
| 当前专用 Shader | `CustomPostProcessingEffectOutline.shader` |
| 当前 Shader 菜单名 | `Valkyria/Custom Post Processing/Effect Character Outline` |
| 当前粉蓝材质 | 同目录 `Feature_OutlineSS_08_PinkBlueSpirit.mat` |
| 通用效果 Shader | 同目录 `CustomPostProcessingDistortedOutline.shader` 与 `.hlsl` |
| RendererFeature | 同目录 `CustomPostProcessingRendererFeature.cs` |
| 人物 Mask Shader | 同目录 `CustomPostProcessingMask.shader` |
| Clip / Track / Runtime | `Assets/GameScripts/AOT/GameArt/TimelineTrack/CustomPostProcessingClip.cs`、`CustomPostProcessingTrack.cs`、`CustomPostProcessingRuntime.cs` |
| Timeline Editor | `Assets/GameScripts/Editor/TimelineTrack/CustomPostProcessingTimelineEditor.cs` |

Shader GUID：`67ccc583a0fa4fd19b5eb653bb83cb98`。材质 GUID：`695e9037c3254ab0b8b9c638116d6d9b`。当前材质的 `m_Shader` 指向该 Shader GUID，`m_ValidKeywords`、`m_InvalidKeywords` 均为空。

历史上先有 `Assets/Resources/Shader/SS_Outline_Distort.shader` 的适配与预设，再为粉蓝效果制作 `SS_Outline_PinkBlueSpirit.shader`；2026-10-16 正式迁移交付名为 `CustomPostProcessingSpiritOutline.shader`，菜单名为 `Valkyria/Custom Post Processing/Spirit Character Outline`。截至本次快照，实际文件已经是 `CustomPostProcessingEffectOutline.shader`，材质也已移动。当前路径与历史交付不同，但 Shader GUID 仍相同；仅凭旧路径消失不能认定资产被回退。

本次只记录当前事实，没有恢复旧命名、替换贴图或修改用户当前材质。

## 2. 资源与控制链的当前实现

### 2.1 Mask 从哪里来

`RenderObjectMask` 遍历片段解析出的 `MeshRenderer` / `SkinnedMeshRenderer`，使用 `_exclusionMaskMaterial` 作为 override material 重画各 SubMesh。它没有调用角色自身的外扩描边 Pass。

人物 Mask 使用单通道 `R8_UNorm`、单采样、双线性过滤；独立 Mask 不绑定相机 MSAA 深度附件。`CustomPostProcessingMask.shader` 的 Pass 为 `Mask`，状态为 `ZTest Always`、`ZWrite Off`、`Cull Off`、`ColorMask R`，通过采样 `_CameraDepthTexture` 手工判定可见性。文件内的 `ZTest Always` 因此不代表无视场景遮挡。

当前 override Shader 没有原角色材质的 Alpha Clip 纹理处理；透明镂空和专门顶点变形不能自动视为一致。透明遮挡物是否有效还取决于其是否进入当前相机深度。

未绑定目标时，C# 绑定黑纹理并将人物 Mask texel size 置零；主效果检测无效尺寸后返回原场景。相机屏幕 texel size 与实际 Mask RT texel size 分开传递，不能混用。

### 2.2 形状选择及模糊顺序

当前顺序为：

```text
RenderObjectMask
→ RenderOutlineMask，返回当前效果形状
→ RenderBlurredMask，可选
→ effect.Material 的 Pass 0 合成
→ 可选排除/替换合成，或回拷到源颜色
```

`RenderOutlineMask` 支持空心轮廓与实心人物。实心像素外扩为零时直接返回人物 Mask；实心 UV 缩放也可以重用人物 Mask，由后续采样执行逆向 UV 缩放。需要形态学外扩时才进入 `GenerateOutlineMask`。

当前生成核为三圈、每圈八个方向，最多读取二十四个邻居加中心；有零宽度及中心已覆盖等提前返回。空心输入执行减去人物覆盖的计算，实心保留内部。这个生成 Pass 的开销单独计入，不与最终 Shader 的四次形状采样混淆。

`outlineResolutionScale` 与 `objectMaskResolutionScale` 都相对完整相机描述符计算；它们不是串联相乘的两次降采样。

### 2.3 控制归属与当前字段

| 所有者 | 字段/参数 | 当前语义 |
| --- | --- | --- |
| RendererFeature | `enableMaskKawaseBlur` | 全局允许模糊；源码初始值为 false，具体 Renderer 资产可能覆盖 |
| Clip | `objectMaskResolutionScale` | 默认 `0.5`，运行范围 `0.25..1` |
| Clip | `outlineResolutionScale` | 默认 `0.5`，运行范围 `0.25..1` |
| Clip | `maskBlurLevels` | 默认 `2`，范围 `0..4`，零层跳过 |
| Clip | `maskBlurOffset` | 默认 `1`，范围 `0.5..2` |
| Clip / 运行时 | 目标绑定、实心/空心、外扩模式/数值/中心 | 决定形状来源和外扩 |
| 材质 | `_OutlineScaleWithCamera`、`_OutlineReferenceDistance` | 决定宽度及扰动是否使用投影补偿 |
| 材质 | `_EffectVisibility` | 当前源码新增的整体显现，默认 `1` |
| Timeline | `_CustomPostProcessingWeight` | 运行时片段权重 |

默认值不是已保存 Clip 的最终值。本次没有审计当前用户场景的 Renderer 序列化开关、未保存 Clip 或运行时质量系统自动分档，不声称低端/高端设备已自动切换配置。

`CustomPostProcessingRuntimeEffect` 对非法分辨率/偏移提供回退并限制范围；其构造参数默认值与新建 Clip 的字段初值不同，后续加字段时要同时核对这两个入口。

### 2.4 Kawase 的具体 Pass 与资源

| 环节 | 当前实现 |
| --- | --- |
| Pass 发现 | `FindPass("MaskKawaseDownsample")` 与 `FindPass("MaskKawaseUpsample")`；缺任一个则旁路 |
| 下采样 | 每层宽高减半；核为中心权重 4 加四个对角各 1，归一化除 8，共五次读取 |
| 上采样 | 四个轴向各 1 加四个对角各 2，归一化除 12，共八次读取 |
| 偏移 | `sourceTexelSize × 0.5 × maskBlurOffset`；上采样轴向再乘 2 |
| 层数 | 最多四层；尺寸到 `1×1` 后停止 |
| 最终结果 | 回到输入形状尺寸，使用 Blur 池独立 RT，保留原始人物/轮廓 RT |
| 绑定 | `_CustomPostProcessingBlurredMask` 与 `_CustomPostProcessingUseBlurredMask` |
| 测量标记 | `Mask Dual Kawase Blur` |

实际生成 `L` 层时为 `L` 次下采样加 `L` 次上采样，即 `2L` Draw。常见二层是四次 Blur Draw，不含人物重画、形状生成、主效果和颜色回拷。理想尺寸下的处理面积模型见通用参考；当前两种核采样数不同，需要分别计费。

每个效果开始先清空 Blur 标志和纹理，Pass 结束再次复位；中间父层仅在不再需要旧内容时复用上采样，最终结果不覆写原始人物 Mask。闲置句柄会释放，Feature 关闭时处理相关资源。后续修改资源池时仍需审计命令记录期间的句柄生命周期，不能只看 C# 引用是否为空。

### 2.5 相机距离补偿

`CustomPostProcessingOutlineProjection.GetPixelSizeUV` 当前计算为：

```csharp
float clipW = projection.m32 * -viewDepth + projection.m33;
float unit = referenceDistance / (1080f * 1.7320508f * clipW);
Vector2 pixelSize = new Vector2(
    Mathf.Abs(projection.m00) * unit,
    Mathf.Abs(projection.m11) * unit);
```

这是现有参数的标定：1080p、垂直 FOV 60°，`1.7320508` 为 `cot(30°)`。使用正的视空间深度与实际投影矩阵，非法值走 fallback；取合并 Renderer Bounds 中心作为目标深度近似。

`_CustomPostProcessingOutlinePixelSize.xy` 用于形状/外缘偏移；扰动打开补偿时使用它的 Y 分量乘 `1080`。横向扰动额外按屏幕 texel 比例修正。噪声和细节仍按屏幕 UV 采样，所以“屏幕 UV”与“远近厚度补偿”可以同时成立。

这不是逐片元世界深度外扩，也不改变 Bloom 半径。正交/特殊投影的矩阵处理与 Mask 深度转换是两件事，不能因宽度公式包含 `clipW` 就声称整条链路已通过正交、XR 或反向 Z 的全部场景测试。

### 2.6 Timeline 材质实例与状态

Track 通过 `GetRuntimeMaterial` 缓存按效果创建的材质实例，标记 `HideAndDontSave`。预览中每帧从源材质 `CopyPropertiesFromMaterial`，再覆盖曲线控制的属性，使美术修改可见，并将 Timeline 写入留在临时实例。

`ProcessFrame` 以 owner 更新当前帧效果；没有活动片段时可提交空帧。源码注释明确 `OnGraphStop` 也会在暂停/编辑器求值时出现，因此保留图仍存在时的最后求值状态；`OnPlayableDestroy` 移除 owner，销毁实例并清理缓存。运行中用 `Destroy`，编辑模式用 `DestroyImmediate`。

以上是当前代码事实，本轮没有重新执行 Unity 预览退出、Domain Reload、双 Director 或重叠片段回归。

## 3. 专用效果的当前计算与材质基线

### 3.1 现有算法边界

当前专用 Shader 的 Pass 顺序：`SpiritCharacterOutline`、`GenerateOutlineMask`、`MaskKawaseDownsample`、`MaskKawaseUpsample`。文件与菜单已改名，但主 Pass 名仍保留 `SpiritCharacterOutline`；实际主绘制使用 Pass 索引 0，辅助 Pass 使用名称查找。后续统一名称前先搜索所有调用者。

主计算为：屏幕噪声产生单向位移 → 四方向读取形状 → 最大值为 outer、最小值为 core、差为 rim → 原始位置人物保护 → 细节发光 → 主体与外缘分色合成。它没有只采一次形状；“实心输入”不等于“一次纹理读取”。

`SampleEffectMask` 在 Blur 标志有效时读模糊图，否则根据输入选择原始人物或生成轮廓图。`SampleShape` 执行 UV 缩放等形状选择，再使用：

```hlsl
smoothstep(0.001h, max(0.0011h, _EdgeSoftness), mask)
```

`_EdgeSoftness=1` 保留较宽连续过渡，接近零时趋于硬阈值。该参数是覆盖率映射，不是屏幕像素单位的模糊半径。

人物保护始终读取未扰动、未做 Kawase 的原始人物 Mask。低分辨率时，以四个周围样本的最小值支持小孔修复，再与中心取最大值；过渡使用屏幕与 Mask texel 比例限制宽度。直接使用周围样本最大值曾导致保护区外扩，吃掉贴身火焰；当前逻辑的意图是减少该误伤，仍可能填掉真实小孔。

细节纹理经过阈值与柔和度后只增加 RGB 发光，主体与外缘都使用它。它不控制形状 Alpha，因此当前专用 Shader **没有独立噪声溶解功能**。`_EffectVisibility` 与 Timeline 权重相乘可以整体淡出，但不等于图案溶解。

通用 `CustomPostProcessingDistortedOutline.hlsl` 则有独立溶解路径：`_DissolveSoftness` 最小值保护、`edgeValue / softness` 的连续覆盖、强度 0/1 端点处理，并乘入 `coverage`。它采用线性过渡，不应误写为该段使用 `smoothstep`。专用版本若未来要软溶解，需要明确增加覆盖率运算并重新验证，不可仅凭有软 Mask 就声称已支持。

### 3.2 当前粉蓝材质的复现快照

该表用于识别版本和解释结果，不替代资产旁参数说明；数值不是通用推荐值。

| 项目 | 当前保存值 |
| --- | --- |
| 扰动纹理 | `Assets/AssetRaw/Effects/Textures/noise/Eff_Noise_46.png`，GUID `1ee0f54139ca3e64f985138d84aae77f` |
| 扰动平铺 | `(6, 2)`，偏移 `(0, 0)` |
| 扰动 | 强度 `-2.36`，速度 `0`，方向 `(0, 0.8)` |
| 细节纹理 | `Assets/AssetRaw/Effects/Textures/noise/Eff_Noise_39.tga`，GUID `4a3bda6726a30a845a3da1210f4fb958` |
| 细节平铺 / 滚动 | `(2, 1.2)` / `(0.02, -0.065)` |
| 细节阈值 / 过渡 / 强度 | `0.12` / `0.55` / `0.9` |
| 细节 HDR 色 | `(0.45, 0.9, 1.6, 1)` |
| 主体 HDR 色 | `(1.15, 2.5, 4.8, 1)` |
| 外缘 HDR 色 | `(4.2, 0.28, 1.55, 1)`；外缘可调色，不固定为黑 |
| 外缘宽度 / 柔和程度 | `5` / `1` |
| 距离补偿 / 参考距离 | `1` / `1` |
| 材质内保存的形状默认 | 实心、像素外扩模式、外扩 `0`；执行时 Clip 可覆盖 |

当前材质尚未显式保存 `_EffectVisibility` 条目，Shader 默认是 `1`。2026-10-16 专用化验证基线使用另一张扰动贴图 `ef_common_noisewave_003.png`，扰动速度为 `-0.13`；本次发现现值已不同，不能把历史图像零误差结果套到当前材质。当前速度零意味着扰动图不由该速度项滚动，细节纹理仍有自己的时间滚动。

### 3.3 参数裁剪与正式迁移经验

历史专用化删除了九个当前粉蓝目标不需要的属性：

```text
_DistortCentered
_DetailEnabled
_DetailBaseOpacity
_DetailRimOnly
_TwinkleSpeed
_RainbowEnabled
_RainbowFrequency
_RainbowSpeed
_RainbowSaturation
```

同时裁掉 `_SS_DETAIL`、`_SS_RAINBOW` 模式与相关表达式，而不是仅隐藏面板。保留细节贴图、流速、阈值、柔和度、强度和颜色，因为它们仍参与粉蓝外观；隐藏的 `_OutlineWidthPixels` 保留为空心模式协议。专用化范围当时只针对目标粉蓝材质，没有要求把所有预设都改成同一能力集合。

性能收益主要是减少模式、变体及维护复杂度。噪声和细节仍各有采样，四方向形状读取也仍在，不能据此宣称主 Pass 大幅提速。

2026-10-16 迁移保存原 Shader `.meta` GUID；记录显示材质及其 `.meta` 字节未改，旧 Shader 路径移除，修改集中在正式路径、Shader 菜单名与主 Pass 名。后续现有文件又有命名及整体显现改动，历史迁移证据仅覆盖当时那次变更。

## 4. 问题、根因、解决路径与证据

| 项目事实/现象 | 根因或当前解释 | 修正/约束 | 证据与未验证边界 |
| --- | --- | --- | --- |
| 搜不到材质、怀疑回退 | Main2/Main3 及历史资源路径混用；后续还有重命名/移动 | 以实际工程绝对路径、GUID 和当前文件定位 | 当前 Shader/材质及 GUID 已读回；不能推断用户编辑器缓存状态 |
| 相机远处感觉轮廓过粗 | 固定像素宽度相对小人物变粗；UV 外扩、低分辨率和 Bloom 也会贡献 | 引入参考距离投影标定，分别调宽度与扰动 | 当前矩阵公式已核对；最终镜头观感及平台需场景复验 |
| 问 Mask 是否来自角色描边 Pass | “描边”名称混淆了几何描边与后处理 Mask | 独立 override material 重画原始几何 | `RenderObjectMask`、`DrawRenderer` 与 Mask Shader |
| 降分辨率后贴身填充退缩 | 消费端保护膨胀会吃掉效果；生产端深度取样错位也能打洞 | 分别检查原始 RT 与保护量，避免用一种补偿掩盖两种原因 | 当前两处实现均有代码；没有把一次视觉症状全归到单一根因 |
| 边缘已软但溶解仍硬 | Mask 轮廓与溶解孔洞是不同覆盖率来源 | 每个新生成的边界各自做连续覆盖 | 专用版本只做细节发光；通用版本另有软溶解路径 |
| 要高配模糊、低配关闭 | 质量策略与镜头艺术参数职责不同 | Feature 总控＋Clip 层数/偏移；关闭跳过 Blur，恢复直接形状输入 | 当前 C# 与辅助 Pass；未完成设备质量系统的自动映射审计 |
| 黑色外圈不想保留 | 两色分解的默认外缘色为黑，不是必然的黑边伪影 | 保留独立 `_RimColor` 与 Alpha | 当前粉蓝材质外缘已是粉色 |
| 想删不属于粉蓝的功能 | 通用预设能力超出当前材质需求 | 单独专用化，保留活跃依赖与 Clip 协议 | 历史静态检查与 17 组离线 A/B；现版本新增内容未复跑 |

### 4.1 当前 Mask 深度修正的具体实现

低分辨率 Mask 的栅格中心与全分辨率相机深度纹素中心不同。斜面上直接比较两个位置的深度，会让角色表面误判为被自己遮挡。

当前 `CustomPostProcessingMask.shader`：

1. 通过相机深度尺寸求实际纹素中心 `sampleUV = (depthPixel + 0.5) / depthSize`。
2. 使用 `ddx/ddy` 与屏幕 UV Jacobian 重建当前三角形原始深度梯度，将物体深度投影到该 `sampleUV`。
3. 物体和场景深度均转为 eye depth 后比较，容差为 `max(0.002, sceneEyeDepth * 0.001)`。
4. 首个样本被遮挡且低分辨率足迹大于 `1.01` 时，额外检查三个邻近深度纹素中心，取最大可见性。
5. `clip(visibility)`，有效区域写入红通道 1。

这比无条件扩大人物保护区更接近生产端原因，但邻近样本接受策略可能放松细薄外部遮挡边界。本次只核实代码和数学意图；薄墙、交叠网格、特殊投影、移动 GPU 导数及 MSAA 场景仍需实际验证，不提升为无条件通用规则。

## 5. 性能结论与质量建议

目标设备是低端 Android、30 FPS，总帧预算约 33.3 ms；该数字是整帧预算，不是本效果的预算。本轮没有真机 GPU 计时，不能给出本效果固定毫秒值。

| 档位方向 | 建议起点 | 上线前需确认 |
| --- | --- | --- |
| 低端 | 关闭附加 Kawase；先比较半分辨率与全分辨率人物 Mask；能直采实心时跳过形状生成 | 细结构、贴身填充、真实遮挡和同屏总成本 |
| 中高端 | 从一至二层 Kawase 开始按视觉收益调整 | 模糊层数的收益、Draw/RT 成本及主体保护 |
| 大软边需求 | 在近似同宽度下比较 Dual Kawase 与优化两遍 Gaussian | 实际设备、输出尺寸、帧频和热稳定性 |

当前二层 Blur 是四次 Draw；主合成仍处理相机画面。降低 Mask 分辨率不会让整条后处理自动按面积同比降耗。代码还在无排除/替换合成需求时避开原始颜色副本，但仍有主效果结果回拷；不能宣传为完全没有全屏拷贝。

对角色数、重叠 Clip 数、骨骼 SubMesh 数、Mask 生成、形状生成、Blur、主合成及回拷分别计数后再设预算。对 `Mask Dual Kawase Blur` 的测量只代表 Blur 子阶段。

## 6. 验证证据、工具与局限

### 6.1 已有证据的适用范围

| 证据 | 已知结果 | 可以证明 | 不能证明 |
| --- | --- | --- | --- |
| 2026-10-16 专用化静态断言 | 九个属性删除；保留值及相关辅助函数/Pass 对照通过 | 当时裁剪范围与保真意图 | 当前所有源文件仍与当时相同 |
| 实际 Main2 URP 头文件＋D3DCompile | 五个入口通过 | 当时 HLSL 在该离线编译配置有效 | Unity 导入、Shader target 3.0 全部后端、Android 编译 |
| D3D11 WARP 离线图像 | 十七组前后对照，最大绝对差均为零 | 当时固定输入下目标效果保真 | Unity 当前场景、当前材质、生产 Mask、GPU 性能 |
| 2026-10-16 迁移清单 | 原 GUID 保持、材质/其 meta 字节不变、旧 Shader 移除 | 当时资源迁移结果 | 后续再改名或改参数后的视觉保真 |
| 历史资源检查 | `check-unity-assets.ps1 -Scope Changed` 与差异检查通过记录 | 当时资产/meta 引用检查 | 真机视觉及完整构建 |
| 2026-10-20 文档核对 | 读取当前代码、材质、meta、版本及历史 JSON；保存关键文件哈希 | 本 Profile 的当前路径和实现事实 | 本轮没有重跑 Unity、离线渲染或设备测量 |

历史资源检查曾伴随无关 `tl_fx_100101_bigskill01.playable` 的 `m_Script` GUID 解析警告；Assets 范围扫描无法解析包内脚本时应分别核对来源，不能归咎于新 Shader，也不能把整个工程表述为零警告。

### 6.2 历史离线验证的可复核摘要

工具目录：`C:\Users\admin\AppData\Local\Temp\ss-pink-specialized-20261016\`。

关键文件为 `prepare.py`、`verify.py`、`manifest.json`、`baseline.json`、`verification.json`、`original.shader`、`original.mat`、`delivery/` 和 `backup/`。编译辅助为 `C:\Users\admin\AppData\Local\Temp\verify_outline_feather.py`，离线渲染器为 `C:\Users\admin\AppData\Local\Temp\projectacg-outline-presets\render.exe`。

历史编译入口：`Vert`、`Frag`、`FragGenerateOutline`、`FragMaskKawaseDownsample`、`FragMaskKawaseUpsample`；离线 profile 使用 `vs_5_0` / `ps_5_0`。当前源码的 `#pragma target 3.0` 与离线编译 profile 不是同一验证，不能混写为“移动端编译通过”。

以下为已读取 `verification.json` 的结果压缩记录：

```json
{
  "date": "2026-10-16",
  "status": "pass",
  "entry_points": 5,
  "comparison_pairs": 17,
  "render_count": 34,
  "maximum_absolute_difference_across_cases": 0.0,
  "backend": "D3D11 WARP",
  "limitation": "No Unity scene, Android compiler or GPU timing"
}
```

十七个样本为：`time0`、`time06`、`time12`、`zeroWeight`、`halfWeight`、`noBinding`、`empty`、`full`、`halfScale`、`cameraOff`、`blurred`、`halfResolution`、`uvScale`、`expandedInput`、`hollowInput`、`transparentColors`、`detailZero`。

输入为固定场景/人物测试图、960×540 输出与实际纹理像素，按导入标志处理 sRGB。测试涵盖有限值、旁路、Alpha、权重、保护与时间变化。其中 `blurred` 用 `scipy.ndimage.gaussian_filter(mask, 2)` 构造软输入，验证消费端可处理软图；它不是实际 Kawase 调度测试。形状模式也是受控输入测试，不能替代真实人物几何生产链。

迁移目录：`C:\Users\admin\AppData\Local\Temp\ss-spirit-migration-93a7c5328afc4ddabc22df956980d5dd\`；`installed.json` 中历史 Shader 哈希为 `E473640D58A534C7DD8AD55D1750CFC1495BADE7FD8E17B39741C83D18E6F6AE`。这些 Temp 路径仅用于追溯当前机器，不作为长期工具安装契约；若需要长期自动回归，应将最小 Harness 独立纳入维护范围。

### 6.3 当前关键文件指纹

以下 SHA-256 对应本次读取时的 Main2 文件，不代表远端提交版本。目录均为 `Assets/Shader/RenderFeature/CustomPostProcessing/`。

| 文件 | SHA-256 |
| --- | --- |
| `CustomPostProcessingEffectOutline.shader` | `9F3215021E0B593629FB43775A68D0E75F74AF19217D56ADA795FD389184929E` |
| `Feature_OutlineSS_08_PinkBlueSpirit.mat` | `7D50712E5C20988D89F6BEE33DBF453964F23A65AC2DCF65D60D366E8DC9F155` |
| `CustomPostProcessingRendererFeature.cs` | `4414BAB5D94954CBFFF568C845B1086EEFC9D5324C455CD8751E728E670CFC73` |
| `CustomPostProcessingMask.shader` | `93E5DAD055A56087FFA230EC1A10F885912CE0C3AEC8F983D97557EA87473F8C` |

### 6.4 后续复验与回退边界

后续代码变更可在 Main2 仓库使用 `Tools/Codex/check-unity-assets.ps1 -Scope Changed` 检查资产引用；新增未跟踪文件同时单独核对。当前效果正式上线前仍需：

- Unity 导入与 Console，实际 Clip/Renderer 配置，以及无绑定、暂停、退出预览、重叠 Clip 与多相机状态。
- Frame Debugger 查看人物 Mask、生成形状、各级 Blur、原始保护和最终合成，确认关闭模糊后确实跳过相关 Draw。
- 全/半/四分之一尺寸、近中远、FOV、屏幕裁切、斜面自遮挡、薄墙、真实镂空、武器和头发细节。
- 目标 Android 图形 API 的构建/变体保留、低端 30 FPS 场景下的 GPU 耗时、同屏数压力及持续运行。
- 当前改名、`_EffectVisibility`、扰动贴图与速度变化后的 A/B；不能引用旧版本零差异替代。

关闭 Kawase、提高 Mask 分辨率、恢复原材质引用或恢复同 GUID 的迁移前文件分别对应不同回退层。不要用历史 Temp 备份直接覆盖当前用户调好的材质，也不要自动恢复未保存的 Timeline。先冻结当前版本，再按本次实际修改范围回退。

本次只写规则库文档，没有改 Unity 工程文件、启动 Unity 或保存用户场景。稳定原则沿用 Shader 模块 `CMP-01`、`ARC-03/04`、`ORG-01/02/03`、`CTL-01/02`、`WRK-01/02/03`、`EVO-01`；本 Profile 中的具体路径、常数和验证状态不提升为 CORE。
