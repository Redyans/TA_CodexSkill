---
name: urp-material-import-and-sh-evaluation
description: Unity/URP 材质默认 Shader、3ds Max Physical Material 导入和 Light Probe 球谐求值模式的实现与排查参考。
---

# URP 材质默认值、3ds Max 导入与 SH 求值参考

> **类型**：`REFERENCE`；适用范围：Unity URP 材质创建、3ds Max FBX 材质导入、Light Probe 球谐（SH）和 Shader 变体治理。
>
> **使用前提**：必须以目标项目的 Unity/URP 版本、嵌入式包源码、材质属性、模型导入器、质量档和实际构建重新验证。本文提供可迁移的方法，不把某个项目的路径、枚举或包补丁当作通用事实。

## 1. 适用场景

以下需求经常同时出现，但职责不同，应先拆开处理：

| 需求 | 正确落点 | 不要混淆为 |
| --- | --- | --- |
| 新建材质默认使用轻量 Shader | Editor 默认 Shader 策略、材质生成器和模板材质 | 删除 URP Lit 源文件 |
| 3ds Max Physical Material 导入后使用 Unlit | `AssetPostprocessor` 的材质映射 | 只修改已有 `.mat` |
| 所有平台使用逐像素 SH | 每个实际运行的 URP Pipeline Asset 的 SH Evaluation Mode | 关闭 Mesh Renderer 的 Light Probes |
| 不使用 Light Probe 的顶点 SH | 选择 `Per Pixel`，让 URP 不启用顶点/混合 SH 关键字 | 关闭所有 Light Probe 光照 |
| 缩减无用 Shader 变体 | `IPreprocessShaders`、Profile、SVC 和构建证据 | 删除 Package Shader 文件 |

## 2. 前提与依赖

开始修改前先确认：

1. 当前 Unity 与 URP 版本，以及 URP 是 Package、嵌入式包还是项目副本。
2. 默认材质的真实创建入口：Unity 内置新建、项目 Editor 工具、导入器、模板 `.mat` 是否各自有一套默认值。
3. 3ds Max 材质描述字段的实际名称、数值语义、纹理方向和颜色空间；不要凭 Autodesk/Unity 旧版本示例猜字段。
4. 运行时实际使用的全部 URP Pipeline Asset。质量档切换器可能覆盖 `GraphicsSettings.defaultRenderPipeline`，只改默认管线不代表所有平台和质量档都生效。
5. Light Probe、Lightmap、Mixed Lighting、Shadowmask 和 Subtractive Lighting 的实际使用者。SH 求值模式与混合光照变体是两个独立维度。
6. 构建收集器、AssetBundle/Addressables/YooAsset BuildMap、ShaderVariantCollection 和运行时动态材质/Keyword 路径。

## 3. 实现或排查步骤

### 3.1 新材质默认 Shader：先覆盖入口，再处理 fallback

URP 的默认 Shader 可能来自多个入口：

```text
Unity Editor 新建材质
    -> 当前 RenderPipelineAsset.defaultShader
    -> 管线/Renderer 的内部默认字段
    -> URP 的 Lit fallback

项目材质工具 / 模板 .mat / 导入器
    -> 各自显式创建或绑定 Shader
```

建议顺序：

1. 让项目材质生成器显式调用 `Shader.Find("Universal Render Pipeline/Unlit")` 或集中策略函数；不要只依赖 Inspector 显示名。
2. 对 Unity 的内置新材质入口，先检查目标版本是否提供可写的 `defaultShader` API。若 API 只读而内部字段稳定可定位，可采用 **Editor-only** 的受控反射策略；失败时只告警，不阻塞编辑器启动。
3. 将策略挂在 `[InitializeOnLoad]`，在当前活动 URP Asset 变化后重新应用；不要把 Editor 代码编入 Player。
4. 模板材质也要同步改 Shader 引用，避免新建模板绕过策略。
5. 不要批量改写已有材质，除非用户明确要求迁移；已有材质是否仍需 Lit 应按材质/场景/构建依赖单独审核。

`Universal Render Pipeline/Lit` 仍可能被 Unity fallback、Package 内置材质、外部插件或旧 `.mat` 引用。默认值改成 Unlit 不等于 Lit 已从工程或 Player 消失。

### 3.2 3ds Max Physical Material → URP Unlit：做数据适配而非简单换名

导入器应把“材质描述字段”映射到目标 Shader 的实际属性：

| 输入数据 | URP Unlit 常见目标 | 处理要点 |
| --- | --- | --- |
| Base Color 标量 | `_BaseColor` | 按目标颜色空间写入；Alpha 语义要单独确认 |
| Base Color 纹理 | `_BaseMap` | 同步纹理、Offset、Scale |
| Opacity/Transparency | `_BaseColor.a` 或目标透明纹理 | 明确输入是“透明度”还是“不透明度” |
| 旧材质 Keyword | 无 | 换 Shader 后清理不属于目标 Shader 的旧 Keyword |
| 透明状态 | `_Surface`、Blend、Queue、RenderType | 通过目标 Shader 的 GUI/初始化函数统一设置 |

3ds Max Physical Material 的 `transparency` 常表示透明程度，而 URP Unlit 的 BaseColor Alpha 表示不透明度，常见适配是：

```text
opacity = 1 - transparency
```

但这不是普适公式：导出器可能已经反相、透明贴图可能代表不同语义，必须用灰阶样本验证。Stock Unlit 通常只有一个 Base Map；当颜色贴图和独立透明贴图都存在时，不能假设它会自动完成两张图的采样、反相和合成。需要精确复现时，应使用专用适配 Shader，而不是悄悄丢掉一张图。

修改导入器后提升导入器版本，让存量模型重新执行材质处理；仅修改代码而不重新导入，旧材质不会自动更新。导入失败时应保留原材质或明确回退，不能写入半完成的 Shader/属性状态。

### 3.3 URP SH Evaluation Mode：Per Pixel 才能排除顶点 SH

在 URP Pipeline Asset 的 Inspector 中，通常位于：

```text
Lighting
    -> Additional Properties / Additional Settings
        -> SH Evaluation Mode
            -> Per Pixel
```

对应的通用语义是：

| 模式 | 含义 | 典型关键字状态 |
| --- | --- | --- |
| `Auto` | Unity 按平台自动选择 | 移动/XR Mobile/Switch 可能选顶点 SH |
| `PerVertex` | 顶点阶段计算 SH | `EVALUATE_SH_VERTEX` |
| `Mixed` | 顶点和像素分担 | `EVALUATE_SH_MIXED` |
| `PerPixel` | 像素阶段计算 SH | 两个求值关键字均不启用 |

要保证“所有平台逐像素”，必须将所有会被加载的 URP Pipeline Asset 都设为 `Per Pixel`，包括运行时质量切换器引用的低/中/高/超高档，而不是只修改 Project Settings 中的默认资产。

`Per Pixel` 不等于“关闭 Light Probe”。它仍然可以读取 Light Probe 的 SH 数据，只是把求值放到片元阶段。若要某个 Renderer 完全不接受 Light Probe，应在该 Renderer 的 `Light Probes` 选项中关闭；这是更强的行为变化，需要单独确认视觉和性能影响。

### 3.4 Mixed Lighting 与 `LIGHTMAP_SHADOW_MIXING` 要单独审计

`LIGHTMAP_SHADOW_MIXING` 属于混合光照/光照贴图阴影路径，不是顶点 SH 关键字。URP Pipeline Asset 的 `Mixed Lighting` 开关通常控制是否生成这类变体。

关闭前应确认工程没有使用：

- Mixed Lights；
- Shadowmask / Distance Shadowmask；
- Subtractive Lighting；
- 依赖烘焙光照阴影混合的场景或平台配置。

仅因为“工程主要使用 Realtime Light Probe”就关闭 Mixed Lighting 是不充分证据。若不能证明没有消费者，保留该维度，并在变体裁剪器中把它当作管线关键字而不是材质局部 Keyword。

### 3.5 与变体剔除的组合关系

SH 求值的 3 种模式与 `LIGHTMAP_SHADOW_MIXING` 开关在理论上形成：

```text
3 种 SH 模式 × 2 种 Mixed Lighting 状态 = 每个 Pass 最多 6 种组合
```

这是单独两个维度的理论上限，不是最终构建总数；还要乘上其他 `multi_compile`/`shader_feature`、Pass、平台、材质状态和 Unity/项目剔除结果。将 `EVALUATE_SH_VERTEX`、`EVALUATE_SH_MIXED` 或 `LIGHTMAP_SHADOW_MIXING` 误当材质局部 Keyword，会让基于材质证据的裁剪误删运行时变体。

如果某 Shader 被 Profile 的“全量剔除”名单命中，所有 Pass 直接归零，保留这三个关键词没有意义；全量剔除和保留指定变体是互斥策略。

### 3.6 推荐排查顺序

```text
Shader.name / 资产路径 / GUID
    -> 静态材质、Prefab、Scene、FBX 嵌入材质
    -> Shader.Find、new Material、EnableKeyword、动态加载
    -> Pipeline/Quality 资产和运行时切换
    -> Collector/BuildMap/Variant Collection
    -> OnProcessShader 的 Pass、关键字和剔除日志
    -> 干净构建 + Player/真机视觉验证
```

遇到粉色材质或变体缺失时，优先暂时移除全量剔除项并清理构建缓存，再以保守策略重建；不要先删除 Package Shader 或继续扩大黑名单。

## 4. 风险与不适用边界

1. Editor 反射策略依赖私有字段名，只能作为受控兼容层；升级 Unity/URP 后必须重新检查字段和默认材质创建链。
2. Unlit 不能自动复现 Physical Material 的金属、粗糙度、法线、AO、发光和透明贴图组合；换 Shader 会改变外观，必须把“减包体”和“保真导入”分开评估。
3. 导入器版本提升只触发重新处理，不保证旧 FBX 被立即重新导入；需要在 Unity 中执行 Reimport 或批量重新导入，并检查生成材质。
4. `Auto` 在移动平台可能选择顶点 SH；只在桌面验证不能证明移动端行为。
5. `Per Pixel` 增加片元阶段计算成本，尤其在高覆盖率、低端移动 GPU 或透明材质上；应以目标设备测量，不以关键字数量推断性能收益。
6. 关闭 Mixed Lighting 或 Light Probe 会改变烘焙、阴影和角色/场景的间接光，不是单纯的变体优化。
7. `Shader.Find` 找不到 Shader、内置材质或运行时动态创建材质，均可能绕过静态材质扫描；静态搜索不能证明运行时安全。

## 5. 验证与回退

### 5.1 最小验证矩阵

| 层级 | 检查 | 通过标准 |
| --- | --- | --- |
| 文本/配置 | Shader 名、GUID、URP Asset 字段、Profile 列表 | 精确匹配，无乱码/尾随空白，所有实际质量档覆盖 |
| Editor 导入 | 新建材质、模板材质、3ds Max FBX 重导入 | 默认 Shader 正确；颜色、贴图、透明、Offset/Scale 可解释 |
| Shader 编译 | 受影响 Shader 的全部实际 Pass | 无新增编译错误；Per Pixel 目标不依赖顶点 SH 变体 |
| 构建 | 清理缓存后重建 Player/AssetBundle | 变体报告、剔除原因和 Shader bundle 与策略一致 |
| 运行时 | 桌面、移动/XR/Switch（按目标）和所有质量档 | 无粉材质；Light Probe、阴影、Mixed Lighting 和透明表现符合预期 |
| 回退 | 删除剔除项/恢复旧 Shader 映射并重建 | 可恢复旧材质与变体，旧缓存不被误当新证据 |

### 5.2 交付报告至少记录

- 实际使用的 URP Pipeline Asset 列表和 `SH Evaluation Mode`；
- 是否关闭 Mixed Lighting，以及 Shadowmask/Subtractive 的证据；
- 导入器版本、字段映射、颜色空间和透明图限制；
- 新材质入口覆盖范围，Editor-only 代码边界和反射失败回退；
- 静态材质引用数、BuildMap/动态路径缺口；
- 理论组合数与 Unity 最终构建变体数的区别；
- 已验证的平台、场景、设备和仍未验证的风险。

