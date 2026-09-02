# ProjectACG BattleSceneBaseLit 变体剥离 Profile

> 类型：PROFILE；适用工程：ProjectACG 当前工作区；本文记录本次 `BattleSceneBaseLit` / SVC 白名单改造的项目事实、决策和验证边界。迁移到其他工程时只保留通用 REFERENCE，路径、版本、Shader 名称、PassType 和关键字矩阵必须重新建立。

## 1. 项目事实与源文件

| 项目 | 当前事实 |
| --- | --- |
| Unity | `2022.3.62f3` |
| URP | 嵌入式 `com.unity.render-pipelines.universal@14.0.12` |
| 目标 Shader | `Valkyria/Scene/BaseLit` |
| Shader 源文件 | `Client/Assets/Shader/Scene/New/PackedMaskPBR/BattleSceneBaseLit.shader` |
| HLSL 文件 | `PackedMaskPBRForwardPass.hlsl`、`PackedMaskPBRLighting.hlsl`、`PackedMaskPBRShadowCasterPass.hlsl`、`PackedMaskPBRDepthOnlyPass.hlsl`、`PackedMaskPBRDepthNormalsPass.hlsl` |
| 剥离器 | `Client/Assets/TEngine/Editor/ShaderStripping/TEngineShaderVariantStripper.cs` |
| Profile | `Client/Assets/TEngine/Editor/ShaderStripping/ShaderStrippingProfile.asset` |
| SVC | `Client/Assets/TEngine/Editor/ShaderStripping/NewShaderVariants.shadervariants` |

当前 Shader 包含 `UniversalForward`、`ShadowCaster`、`DepthOnly`、`DepthNormals` 和 `Meta` Pass。Forward 及深度/阴影 Pass 使用 `#pragma target 3.5`；这是现有兼容基线，不能仅因变体优化自动降级。

## 2. 本次保留的源码能力

本次策略是“源码恢复完整能力，构建层用 SVC 剥离”，因此 Forward 中恢复/保留了下列变体来源：

```text
_SURFACE_TYPE_TRANSPARENT
_ALPHATEST_ON
_ALPHAPREMULTIPLY_ON
_ALPHAMODULATE_ON
_EMISSION
_USE_BASE_HSV
_SPECULARHIGHLIGHTS_OFF
_ENVIRONMENTREFLECTIONS_OFF

_MAIN_LIGHT_SHADOWS
_MAIN_LIGHT_SHADOWS_CASCADE
_MAIN_LIGHT_SHADOWS_SCREEN
_ADDITIONAL_LIGHTS_VERTEX
_ADDITIONAL_LIGHTS
EVALUATE_SH_MIXED
EVALUATE_SH_VERTEX
_ADDITIONAL_LIGHT_SHADOWS
_REFLECTION_PROBE_BLENDING
_REFLECTION_PROBE_BOX_PROJECTION
_SHADOWS_SOFT
_SHADOWS_SOFT_LOW
_SHADOWS_SOFT_MEDIUM
_SHADOWS_SOFT_HIGH
_SCREEN_SPACE_OCCLUSION
_LIGHT_COOKIES
_LIGHT_LAYERS
_FORWARD_PLUS
LIGHTMAP_SHADOW_MIXING
SHADOWS_SHADOWMASK
DIRLIGHTMAP_COMBINED
LIGHTMAP_ON
DYNAMICLIGHTMAP_ON
LOD_FADE_CROSSFADE
multi_compile_instancing
```

雾没有恢复 `multi_compile_fog`，而是在 Shader 中固定：

```shaderlab
#define FOG_LINEAR 1
```

这意味着雾的模式不是构建变体选择；但实际雾是否可见仍取决于 URP/场景的雾输入和运行时环境。

ShadowCaster、DepthOnly、DepthNormals 各自仍保留 Alpha Test、LOD Cross Fade 和必要的实例化/投射分支。不要只根据 Forward 白名单删除这些 Pass 的变体。

## 3. 当前构建策略

Profile 当前配置意图为：

```yaml
Enabled: 1
EnableShaderVariantCollectionAllowList: 1
ShaderVariantCollectionAllowListPath: Assets/TEngine/Editor/ShaderStripping/NewShaderVariants.shadervariants
ShaderVariantCollectionAllowListShaderNames:
- Valkyria/Scene/BaseLit
```

`TEngineShaderVariantStripper` 的白名单匹配规则是：

1. 从 SVC 读取 Shader 引用、`passType`、`keywords`。
2. 对关键字排序并生成规范化签名。
3. 回调中按 `Shader.name + snippet.passType + 完整关键字签名` 匹配。
4. 不匹配的条目执行 `data.RemoveAt(i)`，记录原因 `ShaderVariantCollectionAllowListMismatch`。
5. XR/Debug/Editor 的 `AlwaysStripKeywords` 仍然可以覆盖 SVC。
6. 对显式受 SVC 管理的 BaseLit，不再叠加旧的材质 LocalKeyword 组合剥离和 Assets 额外关键字剥离，避免白名单中已审查的组合被第二套策略误删。

当集合只有 `PassType.Normal` 而回调没有精确 Pass 记录时，代码提供从 Normal 到实际 SRP Pass 的兼容回退；如果集合已有真实 PassType，则仍按真实 Pass 严格匹配。升级 Unity 或重建 SVC 后必须重新检查这个兼容假设。

## 4. 关键字保留/剥离决策

### 4.1 已确认需要进入 BaseLit 白名单的能力

以下组合来自当前场景、材质或目标质量矩阵的保守保留范围，最终仍要以目标构建捕获和运行时验证为准：

```text
_SURFACE_TYPE_TRANSPARENT
_ALPHATEST_ON
_EMISSION
_USE_BASE_HSV
_MAIN_LIGHT_SHADOWS
_MAIN_LIGHT_SHADOWS_CASCADE
_ADDITIONAL_LIGHTS
EVALUATE_SH_VERTEX
_REFLECTION_PROBE_BOX_PROJECTION
_SHADOWS_SOFT
_SHADOWS_SOFT_HIGH
LIGHTMAP_SHADOW_MIXING
SHADOWS_SHADOWMASK
LIGHTMAP_ON
```

这些不是 14 条独立变体，而是多组互斥选项与功能组合的笛卡尔积。Forward、DepthOnly、DepthNormals 和 ShadowCaster 要分别记录。

### 4.2 当前决策中计划剥离的候选

下列关键字是本次优化目标，但只有在 SVC 清理完成且运行时矩阵确认后才可以视为“构建不会保留”：

```text
_ALPHAMODULATE_ON
_ALPHAPREMULTIPLY_ON
_ADDITIONAL_LIGHTS_VERTEX
EVALUATE_SH_MIXED
_ADDITIONAL_LIGHT_SHADOWS
_REFLECTION_PROBE_BLENDING
_SHADOWS_SOFT_LOW
_SHADOWS_SOFT_MEDIUM
_LIGHT_COOKIES
_WRITE_RENDERING_LAYERS
_LIGHT_LAYERS
_FORWARD_PLUS
DIRLIGHTMAP_COMBINED
DYNAMICLIGHTMAP_ON
LOD_FADE_CROSSFADE
INSTANCING_ON / multi_compile_instancing 产生的实例化变体
_CASTING_PUNCTUAL_LIGHT_SHADOW
_SPECULARHIGHLIGHTS_OFF
_MAIN_LIGHT_SHADOWS_SCREEN
_ENVIRONMENTREFLECTIONS_OFF
_SCREEN_SPACE_OCCLUSION
```

`_SPECULARHIGHLIGHTS_OFF` 和 `_ENVIRONMENTREFLECTIONS_OFF` 的“默认开启”只表示项目不提供 Inspector 切换或不允许关闭；如果历史材质仍序列化了这些 LocalKeyword，必须清理材质状态或让 SVC 明确不收录它们，不能只修改默认值。

软阴影也不是只保留一个 `_SHADOWS_SOFT` 就完成：URP 质量级别可能额外产生 `_SHADOWS_SOFT_LOW/MEDIUM/HIGH`，必须根据实际 URP Asset/Quality tier 和目标平台决定。

## 5. 当前 SVC 的静态风险快照

截至 2026-09-02 的工作区静态读取，`NewShaderVariants.shadervariants` 文件约 7 KB，当前可见条目为 20 条，全部标记为 `passType: 13`。其中仍能扫描到：

```text
_ALPHAMODULATE_ON
_ALPHAPREMULTIPLY_ON
_ENVIRONMENTREFLECTIONS_OFF
_SPECULARHIGHLIGHTS_OFF
_WRITE_RENDERING_LAYERS
```

这与“上述候选已从白名单剔除”的目标不一致，说明该 SVC 可能是从 Inspector 手工添加的候选集合，或处于重新收集后的中间状态。当前不能把它当作干净的最终白名单，也不能据此断言这些变体已经不会进入构建。

在打包前应完成以下动作：

1. 备份当前 SVC。
2. 从受控的材质、Quality 和 Renderer 状态重新收集 BaseLit。
3. 对完整关键字集合执行禁止关键字扫描。
4. 删除不应保留的条目，确认 PassType 和 Shader GUID。
5. 重新导入后再运行实际构建和剥离日志检查。

## 6. SVC Inspector 操作约定

- 顶部 `Pick shader keywords to narrow down variant list` 只是筛选器，不是剥离配置。
- `Selected keywords` 是当前筛选条件。
- 下方每一行是一个完整的 Pass/Keyword 变体；必须展开检查整行，不要只看前半段。
- `Add selected variants` 会直接修改 SVC。截图里显示的“Add 11 selected variants”不能默认全部点击。
- 只要候选行出现本节 4.2 的禁止关键字，就不要加入。
- 通过 SVC 维护白名单时，必须保留无关键字基础组合、透明/Alpha Test、Emission、主光阴影、软阴影、Lightmap/Shadowmask 等经过验证的实际组合。

## 7. 本次遇到的问题与解决方式

### 7.1 Unity 报 `ShaderVariantCollectionAllowList` 找不到

代码逻辑和 `dotnet build Assembly-CSharp-Editor.csproj -nologo` 均可通过，但 Unity 曾在字段声明处报 `CS0246`。排查重点是条件编译宏、重复文件和 Unity 实际加载路径。兼容修复把嵌套类型移动到 `TEngineShaderVariantStripper` 类体开头，使类型在字段声明前可见，避免 Unity 编译器/增量导入对嵌套类型前向引用的差异。

### 7.2 理论上只保留 SVC，实际上仍被其他规则剥离

根因是旧的 LocalKeyword 组合规则、Assets 额外关键字规则和 SVC 同时生效。解决方式是对“显式受 SVC 管理的 Shader”关闭这两套宽泛规则，只保留安全的全局 AlwaysStrip 覆盖；未纳入 SVC 的其他 Shader 继续沿用旧策略。

### 7.3 关键字顺序不同导致白名单误判

根因是 SVC 的 `keywords` 字符串和 Unity 回调的 `ShaderKeywordSet` 顺序不稳定。解决方式是两侧都按关键字名称排序后比较签名，不能直接比较原始字符串。

### 7.4 PassType 不一致

收集工具可能写入 `Normal (0)`，而 Shader 回调返回 SRP Pass 值。当前实现保留了有边界的 Normal 回退；长期应使用同版本 Unity/工具重新生成 SVC，并在日志中输出每种 PassType 的数量，避免无条件放宽匹配。

## 8. 验证状态与边界

已完成的静态验证：

- `dotnet build Assembly-CSharp-Editor.csproj -nologo`：0 个错误，存在项目既有警告。
- `git diff --check`：通过。
- 已核对 BaseLit pragma、HLSL 分支、Profile 字段和 SVC Shader GUID。

尚未完成或不能由静态检查替代的验证：

- Unity 实际导入、Shader 编译和完整 AssetBundle/Player 构建。
- Frame Debugger 中各 Pass 的真实关键词和 Draw 顺序。
- 软阴影、Quality tier、低配设备和目标图形 API 的画面验证。
- 包体中最终变体数量、运行时 Shader 加载和粉色材质回归。

此前尝试使用 Unity BatchMode 时遇到 `Licensing::IpcConnector / LicenseClient-admin refused`，因此只能把上述项目状态标记为静态已验证、Unity/设备未验证。后续打包测试必须以构建日志和实际 Player 结果为准。

## 9. ProjectACG 交付前检查表

- [ ] Profile 的 SVC 开关、路径和 Shader.name 精确匹配。
- [ ] SVC 只包含审查过的 Shader、PassType 和完整关键字签名。
- [ ] SVC 中无 `_ALPHAMODULATE_ON`、`_ALPHAPREMULTIPLY_ON` 等计划剥离关键字。
- [ ] BaseLit 的 Forward、ShadowCaster、DepthOnly、DepthNormals 实际需要的组合均有记录。
- [ ] Quality/URP Asset 的软阴影、Lightmap、Additional Lights 和反射探针设置与 SVC 来源一致。
- [ ] 没有运行时脚本动态开启未收集的关键字；如有，已纳入专门白名单。
- [ ] Unity Refresh/编译无错误，构建日志能看到 SVC mismatch 的剥离原因。
- [ ] 至少完成一轮目标场景、Quality tier、Frame Debugger 和 Player 画面验证。
