# Unity URP Shader 变体白名单与构建期剥离实践

> 类型：REFERENCE；适用范围：Unity URP Shader、ShaderVariantCollection（SVC）和 `IPreprocessShaders` 构建期剥离；使用前必须以目标工程的 Unity/URP 版本、Shader 源码、材质、Renderer 和构建日志重新验证。

## 1. 先记住的结论

Shader 源码中的 `shader_feature` / `multi_compile` 决定“编译器可以生成哪些候选变体”，而不是“最终包体一定包含哪些变体”。最终结果还会受到材质实际状态、Renderer/URP 全局设置、ShaderVariantCollection、平台剥离策略和自定义 `IPreprocessShaders` 的共同影响。

更稳妥的优化顺序是：

1. 先恢复并冻结 Shader 的完整功能声明，确保运行时需要的 Pass、关键字和分支仍然存在。
2. 通过材质、场景、Package 和运行时关键字证据确定允许的组合。
3. 用 SVC 保存“Shader + PassType + 完整关键字集合”的白名单。
4. 在构建期由自定义剥离器只保留白名单命中的变体。
5. 用静态检查、Unity 编译、构建日志和运行时画面/Frame Debugger 分层验证；未做 Unity 或设备验证时，不得把静态结果写成运行时结论。

直接从 Shader 中删除 `#pragma` 是不可逆性更高的方案：它会改变材质可用能力、Inspector/Keyword 契约和未来回滚成本。除非已经完成消费者迁移和平台验证，否则优先保留源码声明、在构建层剥离。

## 2. 变体开关的分类方法

### 2.1 `shader_feature` 与 `multi_compile`

| 声明 | 典型用途 | 变体来源 | 主要风险 |
| --- | --- | --- | --- |
| `shader_feature` | 材质功能开关，例如 Alpha Test、Emission、透明模式 | 通常由材质实际使用状态决定 | 材质或运行时动态开启关键字后，如果没有对应变体会粉色或功能失效 |
| `shader_feature_local` | 只属于当前 Shader/材质的本地关键字 | 材质的 LocalKeyword 状态 | 不能用全局关键字采集结果替代；收集器必须保留本地组合 |
| `multi_compile` | URP/Renderer/平台决定的全局功能，例如主光阴影、光照贴图、附加灯 | Unity/URP 构建矩阵和全局设置 | 组合爆炸；不能只根据单个材质判断全局功能是否需要 |
| `multi_compile_fragment` | 只影响片元阶段的全局分支 | 片元编译组合 | 不能把它误判成顶点开关；仍需按 Pass 检查 |
| `multi_compile_vertex` | 只影响顶点阶段的全局分支 | 顶点编译组合 | 例如 `_CASTING_PUNCTUAL_LIGHT_SHADOW` 只应在 ShadowCaster 顶点路径验证 |
| `multi_compile_instancing` | GPU Instancing 相关宏和实例 ID 传递 | Renderer/材质/平台 | 删除后会破坏实例化能力；没有实例化需求时也应先确认所有材质和 Renderer |

同一行中以空格分隔的选项通常是互斥集合，例如：

```shaderlab
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
```

它不是三个可以同时打开的独立布尔开关，而是“无主光阴影 / 普通主光阴影 / 级联阴影 / 屏幕空间主光阴影”这一组选择。统计变体时不能把它当成 `2^3`。

### 2.2 顶点、片元和 Pass 是三种不同维度

- `shader_feature_local_fragment` 和 `multi_compile_fragment` 表示关键字主要影响片元编译；例如 Alpha、反射探针盒投影和软阴影。
- `multi_compile_vertex` 表示关键字主要影响顶点编译；例如投射点光源阴影时的 `_CASTING_PUNCTUAL_LIGHT_SHADOW`。
- `multi_compile` 没有阶段后缀时，通常可能影响顶点和片元，必须阅读 HLSL 分支和 URP Include 后再分类。
- 一个 Shader 的 Forward、ShadowCaster、DepthOnly、DepthNormals、Meta 等 Pass 各自拥有独立的变体列表。不能只保留 Forward 变体，就认为阴影和深度 Pass 也安全。

变体匹配至少需要三个字段：

```text
Shader.name + PassType + 完整关键字集合
```

仅按关键字名称匹配会把不同 Pass 的组合混在一起；仅按 Shader 名称匹配会误保留或误删除其他 Shader 的变体。

## 3. 推荐的实现架构

### 3.1 Profile 只负责开关和范围

Profile 中建议明确记录：

- 是否启用自定义 Shader 剥离。
- SVC 的工程相对路径（只允许 `Assets/` 或 `Packages/`）。
- SVC 白名单作用于哪些精确的 `Shader.name`。
- 是否启用旧的材质 LocalKeyword 组合剥离。
- 全量剥离 Shader 名单和始终剥离的调试/XR 关键字。

SVC 白名单建议显式指定 Shader 名称。空名称列表可以表示“集合内所有 Shader”，但会扩大影响面，适合经过完整审查的专用集合，不适合共享给多个功能组的通用 SVC。

### 3.2 剥离器的匹配顺序

推荐顺序如下：

1. 空输入直接返回。
2. Profile 配置的全量剥离 Shader 直接清空。
3. Meta、ScenePicking、SceneSelection 等仅 Editor Pass 按策略清理。
4. 对配置了 SVC 的 Shader，先按 `PassType + 完整关键字集合` 判断是否在白名单。
5. 再执行全局强制剥离的 XR/Debug/Editor 关键字。
6. 对未纳入 SVC 管理的 Shader，才执行旧的材质 LocalKeyword 组合和 Assets/Package 额外关键字策略。

这样可以避免“白名单中明确收集的变体又被另一套宽泛规则误删”。但 `AlwaysStripKeywords` 仍然是安全上限，例如项目明确不支持的 XR 或 Debug 变体可以继续覆盖 SVC。

### 3.3 关键字签名必须规范化

Unity 序列化的关键字顺序不应被当成语义。读取 SVC 和 `ShaderCompilerData.shaderKeywordSet` 时都应：

1. 取出所有关键字名称。
2. 删除空名称。
3. 使用稳定序排序。
4. 用同一种分隔符生成签名。

不要只比较字符串原文，否则同一组关键字因顺序不同会被错误剥离。关键字名称本身区分大小写；Inspector 的小写显示不是源码关键字名称，写回 SVC 时必须使用 Unity 实际名称。

### 3.4 PassType 兼容要有边界

不同 Unity/工具版本生成的 SVC 可能把 SRP Pass 序列化成 `PassType.Normal`，而 `ShaderSnippetData` 回调返回 Scriptable Render Pipeline 对应值。可以做“仅当请求 Pass 没有精确记录时，从 Normal 回退匹配”的兼容逻辑，但不能把所有 Pass 无条件视为同一个 Pass。

兼容回退必须记录在文档和日志中；升级 Unity 或更换收集工具后应重新确认 PassType。更安全的长期方案是让收集器与剥离器使用同一 Unity 版本，并在 SVC 中保留真实 PassType。

## 4. SVC 的收集、编辑与维护

### 4.1 自动收集不等于“只收集最终需要的变体”

材质球收集器通常会把材质放入临时场景、使用当前 Renderer/URP 状态渲染，再保存 Unity 捕获到的变体。它会受以下因素影响：

- 材质当前是否打开透明、Alpha Test、Emission 等 LocalKeyword。
- 当前 URP Asset 是否开启软阴影、Additional Lights、Lightmap、Forward+ 等全局功能。
- 临时场景是否触发 ShadowCaster、DepthOnly、DepthNormals、Meta 等 Pass。
- 材质是否来自构建 Package、场景直接引用或 Profile 的 AlwaysInclude 根目录。
- 收集时是否有运行时脚本动态调用 `EnableKeyword`。

因此收集结果需要二次审查。自动收集器负责“获得候选”，不负责替团队决定哪些功能可以删除。

### 4.2 Unity SVC Inspector 的正确用法

Inspector 中常见的三个区域含义：

1. `Pick shader keywords to narrow down variant list`：筛选条件，只缩小列表，不修改资产。
2. `Selected keywords`：当前筛选条件。
3. `Shader variants with these keywords`：符合条件的完整变体候选；每行包含 Pass 和完整关键字集合。
4. `Add selected variants`：把勾选行写入 SVC。

不能因为候选列表中有 11 条就全部添加。每一行都要检查完整关键字集合；一行中只要出现团队决定剔除的关键字，就不能加入白名单。特别要防止把 `_ALPHAMODULATE_ON`、`_ALPHAPREMULTIPLY_ON`、`_ENVIRONMENTREFLECTIONS_OFF`、`_WRITE_RENDERING_LAYERS` 等已列为候选剔除项的变体重新加回去。

### 4.3 手工维护的最低检查

每次保存 SVC 后至少检查：

- Shader GUID 是否仍指向目标 Shader。
- `Shader.name` 是否与 Profile 精确一致。
- 每条变体是否有 `keywords` 和 `passType`。
- 是否存在重复的 `Shader + PassType + keyword signature`。
- 是否误混入禁止关键字、Editor/XR 关键字或不属于该 Pass 的关键字。
- 是否仍有 ShadowCaster、DepthOnly、DepthNormals 等实际消费者需要的 Pass。

SVC 是构建输入资产，不能当作“只在编辑器里看的调试列表”。它的变化应与 Shader、Profile 和验证记录一起评审。

## 5. 变体剥离的决策矩阵

| 能力 | 可以考虑剥离的前提 | 常见误判 |
| --- | --- | --- |
| `_MAIN_LIGHT_SHADOWS_SCREEN` | Renderer 和平台从不使用屏幕空间主光阴影，且没有运行时切换 | 把 Cascade 当作 Screen；二者不是同一分支 |
| `_ADDITIONAL_LIGHTS_VERTEX` | 项目只使用片元附加灯或完全禁用附加灯，并已检查 HLSL 顶点累计逻辑 | 只看 Inspector 的 Additional Lights 数量，不看 Shader 是否读顶点累计结果 |
| `EVALUATE_SH_MIXED` / `EVALUATE_SH_VERTEX` | 已确认 SH 评估模式和低配/高配矩阵，且没有动态切换 | 删除 Vertex 后低配设备环境光或顶点 SH 失效 |
| `_ADDITIONAL_LIGHT_SHADOWS` | 附加灯阴影在所有目标 Renderer/Quality 层级均禁用 | 主光阴影正常不代表附加灯阴影可以删 |
| `_REFLECTION_PROBE_BLENDING` | 反射探针不重叠或项目明确不做混合 | 仍使用 Box Projection 时误删另一项 |
| `_SHADOWS_SOFT_LOW/MEDIUM/HIGH` | Quality/URP Asset 只启用确定的软阴影级别 | 只保留 `_SHADOWS_SOFT`，却漏掉具体质量级别关键字 |
| `_LIGHT_COOKIES` / `_LIGHT_LAYERS` / `_FORWARD_PLUS` | 对应 Renderer Feature、灯光设置和平台矩阵均禁用 | 只修改一个 URP Asset，忘记其他 Quality tier |
| `LIGHTMAP_SHADOW_MIXING` / `SHADOWS_SHADOWMASK` | 项目不使用 Mixed Lighting/Shadowmask，并已检查烘焙场景 | 没有当前场景并不代表资源包未来不会加载烘焙场景 |
| `DYNAMICLIGHTMAP_ON` / `DIRLIGHTMAP_COMBINED` | 项目没有实时/方向性光照贴图 | 仅凭“当前场景没烘焙”删除全局能力 |
| `LOD_FADE_CROSSFADE` | LODGroup 不使用 Cross Fade，且没有运行时切换 | 只检查一个角色或一个场景 |
| `multi_compile_instancing` | 所有材质和 Renderer 都不依赖 GPU Instancing/DOTS/Procedural Instancing | 把“当前没看到实例化”误当成全项目保证 |
| `_SPECULARHIGHLIGHTS_OFF` / `_ENVIRONMENTREFLECTIONS_OFF` | 功能被项目强制固定开启，且 ShaderGUI/脚本不会写入关闭状态 | 只改默认值，不清理已有材质上的历史关键字 |

`LIGHTMAP_SHADOW_MIXING`、`EVALUATE_SH_VERTEX` 等能力如果被项目确认需要，应恢复 Shader 声明并在 SVC 中保留实际需要的组合；“源码存在”不等于“构建一定保留”。

## 6. 常见问题、根因和解决方式

### 6.1 `CS0246: ShaderVariantCollectionAllowList could not be found`

现象通常出现在 Unity 编译器，而普通 `dotnet build` 可能通过。先检查：

- 文件是否存在重复副本。
- `#if/#endif` 是否把类型和字段包在不同条件块。
- 项目是否定义了 `TENGINE_DISABLE_SHADER_STRIPPING`。
- Unity Console 指向的实际文件路径和行号是否是当前工作区。

若代码逻辑正确但 Unity 仍不识别“先引用、后声明”的嵌套类型，可把嵌套类型移动到包含类的开头，或移到同命名空间的独立 `internal sealed class`。移动类型本身不应改变匹配逻辑。修复后先做编辑器程序集静态编译，再让 Unity Refresh/重启重新导入。

### 6.2 SVC 看起来有内容，但构建仍缺变体

优先检查：

1. Profile `Enabled` 和 SVC allow-list 开关是否开启。
2. 路径是否为工程相对路径，资产是否成功导入。
3. Profile Shader 名称是否与 `Shader.name` 完全一致。
4. SVC 的 `passType` 是否与回调的 `snippet.passType` 匹配。
5. 关键字是否存在顺序、大小写或隐藏空格差异。
6. 其他 `AlwaysStripKeywords`、全量剥离名单或平台设置是否仍然覆盖它。
7. 是否有运行时动态关键字没有被收集。

### 6.3 SVC 中出现本来要剔除的关键字

这通常不是剥离器“失效”，而是收集或手工编辑阶段把不应保留的变体写入了 SVC。解决方式是：

- 从受控的材质/Quality/Renderer 状态重新生成。
- 对 SVC 做禁止关键字扫描。
- 删除错误条目后重新导入。
- 把“禁止集合”和“允许集合”写成可执行检查，而不是只靠人工目测。

在清理完成前，不要把“启用 SVC 白名单”当成已经完成优化。

### 6.4 软阴影开关存在但运行时不生效

`_SHADOWS_SOFT`、`_SHADOWS_SOFT_LOW/MEDIUM/HIGH` 只是 Shader 分支。实际效果还依赖：

- 当前 URP Asset/Quality 是否启用软阴影。
- 主光源或附加灯是否投射阴影。
- 阴影贴图、阴影距离和平台 API 是否支持。
- 目标 Pass 的对应变体是否进入构建。
- 自定义剥离器是否误删了运行时需要的组合。

因此必须用 Frame Debugger、构建日志和目标设备画面验证，不能仅凭 Inspector 有关键字就下结论。

## 7. 可执行验证清单

### 静态层

- `rg -n "#pragma|#define|LightMode|CustomEditor" <shader>`：重新扫描所有 Pass 和 pragma。
- `rg -n "#if|#ifdef|#ifndef|KEYWORD" <shader> <hlsl>`：确认每个待剥离关键字确实有分支和消费者。
- 解析 SVC 的 Shader GUID、PassType、关键字签名、重复项和禁止关键字。
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`：确认 Editor C# 至少无编译错误。
- `git diff --check`：确认文档/代码没有空白错误。

### Unity/构建层

- Unity Refresh 后确认 Console 无脚本编译错误。
- 运行一次实际目标构建，检查 `ShaderVariantLogs` 或自定义剥离日志中的 before/after 数量和剥离原因。
- 检查构建产物是否包含目标 Shader、正确 Pass 和预期变体。

### 运行时/设备层

- 覆盖主光阴影、级联阴影、软阴影、Lightmap/Shadowmask、透明/Alpha Test、Emission、反射探针、附加灯等组合。
- 用 Frame Debugger 确认 Forward、ShadowCaster、DepthOnly、DepthNormals 的 Draw 顺序和实际使用的关键词。
- 覆盖 Quality tier、低配/高配设备、场景切换和动态材质关键字。

如果 Unity 因 License、ShaderCompiler 或构建环境阻断，只能报告静态验证结果和未验证风险，不能声称构建期剥离已经被运行时证明。

## 8. 回滚与变更纪律

- 删除源码 pragma 前，先保留完整版本或可回滚提交，并记录受影响材质/Pass/平台。
- SVC 是可审查的白名单资产；修改时只改目标 Shader 条目，不要用全量自动收集覆盖其他 Shader。
- Profile 变化、SVC 变化和 Shader pragma 变化应放在同一变更说明中。
- 发现白名单过窄时，优先补充经过验证的完整组合；发现白名单过宽时，优先删除具体条目并保留原因记录。
- 任何“默认开启、不要变体开关”的需求，都要同时检查源码声明、材质历史关键字和构建白名单，三者只改一处是不完整的。

## 9. 交付时应说明的事实

交付报告至少包含：修改了哪些 Shader/Profile/SVC/剥离器；白名单按什么 Shader/Pass/Keyword 规则匹配；静态构建是否通过；Unity/构建/设备/视觉验证是否完成；哪些能力被明确剥离；哪些仍依赖 Quality、Renderer、运行时动态关键字或未验证平台。不要把理论变体数、SVC 条目数或 `dotnet build` 结果直接等同于最终包体大小和运行时正确性。
