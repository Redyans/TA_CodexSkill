---
name: ta-development-rules-shader
description: TA 通用开发规则中的 Shader 开发模块；覆盖 Unity URP 场景与角色 Shader 的开发、重构、优化和审查，并以可迁移 CORE 与当前工程 PROFILE 分层。
---

# TA 通用开发规则｜Shader 开发模块（CORE + PROFILE）

## 概览

> **文档模式**：ta-development-rules/shader/v1
> **语言与编码**：中文，UTF-8
> **模块入口**：本文件是场景与角色 Shader 规则的唯一规范来源；TA 规则总入口见 [../README_Tech_TADevelopmentRules.md](../README_Tech_TADevelopmentRules.md)，配套报告模板见 [references/shader-variant-report.md](references/shader-variant-report.md)。

本手册将规则分为可迁移的 CORE 和当前工程可替换的 PROFILE。CORE 只定义跨项目稳定的工程边界、分类方法与验证要求；PROFILE 承载 Unity/URP 版本、ShaderGUI、具体 Shader 家族、材质枚举、Pass 矩阵和资产路径。

PBR 不是复杂度等级。实现选择必须同时判断两个维度：

| 维度 | 分类 | 决策作用 |
| --- | --- | --- |
| 光照模型 | Unlit / 标准 URP PBR / Toon 或 Custom Lighting | 决定数据流、URP 库复用和光照职责。 |
| 工程复杂度 | 简单 / 标准 / 复杂 | 决定单文件或 Include、Pass 矩阵、共享模块、RendererFeature 和验证深度。 |

| 组合 | 默认实现模式 | 不适用边界 |
| --- | --- | --- |
| 简单 + Unlit | 单一 `.shader`，一个 Forward Unlit Pass，局部 HLSL 就近实现。 | 需要复用、阴影/深度消费者或屏幕资源时升级分类。 |
| 标准 + URP PBR | 标准 `SurfaceData` / `InputData` 与 URP PBR 路径；默认补齐所需深度/阴影 Pass。 | 不能表达项目材质语义时才进入 Adapter 或复杂分类。 |
| 复杂 + PBR / Toon / Custom Lighting | Shader 专属输入、家族共享 Kernel、专用 Pass、RendererFeature 或屏幕资源分层维护。 | 不得将共享光照或屏幕资源复制进每个 Shader。 |

## 工作流程

1. **加载上下文**：执行 `DOC-03`，读取版本、包锁定、目标与附近实现、材质和消费者。
2. **双轴归类**：执行 `CLS-01`，确定领域（场景/角色）、光照模型和复杂度；不能由代码回答且会改变实现时才最小化追问。
3. **冻结兼容性契约**：修改已有 Shader 时执行 `CMP-01`，先保存视觉、材质、Pass、时序和性能基线。
4. **确定职责与最小范围**：执行 `ARC-*`、`ORG-*`、`CTL-*`、`WRK-*`，只改实现用户效果所需的层、Pass 和资源。
5. **分层验证**：按风险执行 `VAL-*`；新增/重写 Shader 或变体来源变化时必须执行 `VAL-03`。
6. **交付与演进**：按 `ACC-01` 收口；用 `EVO-01` 将已验证的新问题沉淀为候选，而不直接堆叠规则。

## 通用规则（CORE）

### DOC｜文档协议与上下文

#### DOC-01｜层级、术语与稳定 ID

- **必须（MUST）**：未满足不可交付，除非记录批准的例外、影响与回退方式。
- **应当（SHOULD）**：默认行为；不采用时记录替代方案、风险和验证。
- **CORE**：不得包含工程路径、插件、资产名、枚举值或固定 Unity/URP 补丁版本。
- **PROFILE**：可收紧 CORE，不得静默弱化行为保真、序列化、源码边界、编码和验证要求。
- **稳定 ID**：使用 `<模块>-<序号>`，例如 `CTL-02`、`SCN-03`。一条规范性要求只在一个规则中定义，其他位置只引用 ID。

#### DOC-02｜任务阅读索引

| 任务 | 必读模块 | 最小验证 |
| --- | --- | --- |
| 简单非 PBR / Unlit | `CLS-01`、`ORG-01`、`WRK-02`、`VAL-01` | Unity 导入、目标材质绘制、UTF-8 与引用检查。 |
| 标准 PBR | `ARC-01`、`ARC-02`、`CTL-*`、`WRK-*`、`VAL-*` | URP 数据流、Pass、材质与场景回归。 |
| 复杂 PBR / Toon / Custom Lighting | `ARC-*`、`CMP-01`、`ORG-*`、`CTL-*`、对应 `SCN-*` 或 `CHR-*` | 共享消费者、时序、变体、目标设备与 A/B。 |
| RendererFeature / 屏幕资源 | `ARC-03`、`ARC-04`、`WRK-01`、`VAL-02` | 资源生产/消费、共享状态所有权、相机、Frame Debugger。 |
| Keyword / 分支 / ShaderGUI | `CTL-*`、`VAL-03`、对应 Profile | 实际材质状态、构建保留变体、动态加载。 |
| 优化或重构 | `CMP-01`、`WRK-03`、`VAL-*` | 同输入 A/B、接口、性能和时序。 |

#### DOC-03｜实现前上下文加载与澄清闸门

新增、重写、优化或审查 Shader 前，必须读取当前 PROFILE、`ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json`、`Packages/packages-lock.json`。检查目标附近的 `.shader`、`.hlsl`、`.cginc`、`.shadergraph`、RendererFeature/RenderPass、材质及直接消费者；角色任务还必须检查 Prefab、动画和部位 Binding。

必须观察既有属性命名、贴图/Sampler 宏、Pass Tag、Render Queue、Blend、Keyword、Include、`CustomEditor`/Drawer 和缩进风格，并据此确认 Shader 类型、光照/Pass 需求、材质输入、颜色空间、目标平台、性能预算和参数接口。

仅在缺失信息会实质改变实现模式、兼容性、资源接口、目标设备支持或性能预算时向用户追问。已有项目事实能够回答的问题不得重复询问；风险可控但暂时无法确认时，记录假设与验证边界，不把猜测写成事实。

#### DOC-04｜外部规则与模板准入

引用其他项目、生成器、Skill、博客或模板时，必须先将候选要求分为三类：跨项目稳定且可验证的工程规则可进入 CORE；依赖当前版本、ShaderGUI、资产、枚举、渲染器或团队工具的要求只能进入 PROFILE；与当前实现或已确认契约冲突的要求必须拒绝或降级为可选方案，并记录理由。

不得因为外部模板完整就迁入其文件拆分、CustomEditor、包安装、全局宏、独立光照库或默认变体集合。引用外部模板后仍以 `DOC-03`、`CMP-01` 和当前 PROFILE 为最终事实来源。

### CLS｜分类与实现选择

#### CLS-01｜光照模型与复杂度双轴分类

先按光照模型选择职责，再按复杂度选择组织形式：

- **Unlit**：不承担主光、GI、阴影或 PBR BRDF；若只做局部可见效果，保持单文件和单 Forward Pass。
- **标准 URP PBR**：优先复用 URP 的 `SurfaceData`、`InputData`、阴影、GI、附加灯和标准 PBR 调用；不得复制独立主光/BRDF/GI 实现。
- **Toon / Custom Lighting**：只能在明确的共享 Kernel 或最小 Adapter 中替换标准路径无法表达的阶段；部位/材质只提交局部输入和已定义扩展。
- **简单**：一个效果、一个 Shader、无跨 Shader 复用、无专用资源生产者；默认单文件。
- **标准**：常规材质、常规 Pass、无跨家族共享语义；优先复用 URP Include 和已有项目模板。
- **复杂**：多 Pass、共享 Kernel、跨 Shader 复用、屏幕资源、RendererFeature、专用透明/Stencil/深度、运行时材质接口或受控变体；必须建立依赖与验证矩阵。

复杂度由功能边界和消费者决定，不由文件长度、是否使用 PBR 或是否包含一个 Keyword 决定。

### CMP｜行为保真与兼容性契约

#### CMP-01｜优化和重构默认保持行为

已有 Shader 的优化、修改和重构默认属于**行为保真重构**。相同场景、相机、渲染器、质量、灯光、时间点和材质输入下，不得无意改变视觉、属性语义、序列化、Keyword、Pass、Render State、资源绑定、透明/深度关系或时序。

重构前必须冻结：受影响材质、属性/贴图通道、默认值、枚举编码、Keyword、Pass/`LightMode`、Render Queue、Blend、ZWrite、ZTest、Cull、Stencil、RendererFeature、相机、目标平台、Frame Debugger 路径和代表性 A/B 基线。任何有意表现变化必须作为独立 Look Change 需求确认。

### ARC｜渲染职责与源码边界

#### ARC-01｜材质、光照、Pass 与资源的职责分层

- **材质 Surface 层**：采样属性与贴图，完成颜色空间、通道、法线、Alpha Clip 和局部输入解码。
- **Input / Lighting 层**：组装坐标、阴影、GI、附加灯和 Fog 输入；标准 PBR 直接调用 URP，Toon/Custom Lighting 调用共享 Kernel/Adapter。
- **专用 Pass 层**：ShadowCaster、DepthOnly、DepthNormals、Meta、Outline、透明深度等复用同一材质语义，仅保留该 Pass 所需的计算。
- **屏幕资源层**：阴影 Atlas、深度、Stencil 遮罩、全屏纹理和临时 RT 必须由 `ScriptableRendererFeature` / `ScriptableRenderPass` 生产并声明生命周期；材质 Shader 只消费公开接口。

#### ARC-02｜优先复用 URP，禁止无理由独立光照

普通材质 Shader 优先使用标准 URP Include。项目已封装或扩展 URP 时，优先使用已验证的项目 Include、宏、Adapter 和 RendererFeature 写法。标准 URP PBR 能表达的功能不得自行复制 BRDF、主光、附加灯、GI 或阴影逻辑。

只有标准路径无法表达且复用范围明确时，才创建最小 Adapter 或共享 Kernel。不得修改 URP Package；确需管线级行为改变时，必须证明局部 Shader、Adapter 和 RendererFeature 均无法实现，并单独确认升级、合并、回滚与全项目回归成本。

不得为了搭建“通用光照框架”把 URP `Lighting.hlsl`、BRDF 或阴影实现整段内联复制为项目私有库。只有已经存在且被多个 Shader 消费的项目 Adapter/Kernel 才可在明确版本基线下维护；新 Shader 优先直接引用当前 URP 或已有 Adapter。

#### ARC-03｜Pass 与渲染时序是公开接口

`LightMode`、Pass Name、Render Queue、Blend、ZWrite、ZTest、Cull、Stencil、ColorMask、RenderPassEvent、相机生命周期和全局资源名均属于接口。不得因为某 Pass 当前未被看见或被注释，就删除、启用、改名或复用为其他功能；先确认生产者、消费者和实际 Draw 路径。

#### ARC-04｜相机共享状态必须声明所有权与释放边界

相机 Depth、Stencil、全局纹理、历史缓存和临时对象 ID 都是跨 Pass 共享状态，不能当作某个 RendererFeature 的私有草稿区。生产者必须明确附件/纹理、初始值、位或数值范围、`ReadMask` / `WriteMask`、写入者、消费者、有效时段、相机边界和清理/恢复责任；消费者必须说明读取发生在同帧还是历史帧。

临时状态只能在声明的窗口内存活。使用公共 Stencil 保存对象 ID 时，完成最后一次依赖该 ID 的绘制后，必须在下一个 Stencil 所有者执行前定向清理，或改用独立 ID Mask / 私有附件隔离。修改 `RenderPassEvent` 让冲突暂时不可见不等于修复；必须同时评估当前帧正确性、历史缓存、运动镜头错位、透明/深度可用性和额外 Draw/RT 成本。

静态 ID 分配器、对象生命周期和相机附件生命周期必须分开审计。限制 ID 上限、场景切换时重置计数器或在 `OnDisable` 回收编号，只有在并发唯一性、复用时机和全部消费者协议已证明安全时才能采用，不能替代共享状态隔离。修复策略、风险和验证矩阵见 [`references/renderer-feature-stencil-and-timing.md`](references/renderer-feature-stencil-and-timing.md)。

### ORG｜代码组织、注释与材质接口

#### ORG-01｜文件与 Include 边界

- **简单非 PBR Shader**：只生成一个 `<ShaderName>.shader`，所有 HLSL、局部函数和属性声明写在该文件中。
- **功能域 Include**：同一 Shader 的多 Pass 或同一功能族确有稳定复用时，提取专用 `.hlsl`；文件必须有唯一 include guard、清晰输入输出和依赖方向。
- **通用函数库**：仅跨多个独立 Shader 稳定复用、无具体材质语义的纯函数才进入通用库。通用库不得声明特定材质 CBUFFER、属性、Pass 状态、Keyword 或隐式纹理绑定。
- **依赖方向**：`.shader` 可依赖功能域 Include/通用库/URP；功能域 Include 可依赖通用库/URP；通用库不得反向依赖具体 Shader、私有 Binding 或 RendererFeature。

#### ORG-02｜中文技术注释、UTF-8 与 ShaderGUI

项目侧新增或修改的 `.shader`、`.hlsl`、ShaderGUI 和技术文档必须使用 UTF-8。每个 Shader、功能域 Include、通用库和复杂模块要有中文职责说明；自定义入口/跨文件函数在定义处说明输入输出、坐标/颜色空间、资源/Keyword 依赖和重要边界。避免仅复述代码。

属性、Drawer、Foldout、Toggle、Enum、属性名和 Keyword 映射共同构成**材质编辑契约**。CORE 不绑定具体 GUI；具体 `CustomEditor` 和 Drawer 规则必须在 PROFILE 中定义。维护既有文件时保留 BOM 策略，禁止因终端代码页把正常 UTF-8 重新保存为乱码。

#### ORG-03｜资源绑定、插值与跨 Pass 一致性

- 贴图与采样器默认使用当前 URP/项目已采用的 `TEXTURE2D`、`SAMPLER`、`SAMPLE_TEXTURE2D` 等宏；HLSL 实际读取的属性名称与类型必须和 `Properties` 一致。
- 使用 SRP Batcher 兼容写法时，`UnityPerMaterial` 只声明 HLSL 实际读取的每材质数值。仅用于 ShaderLab Render State、Queue、ShaderGUI Feature、Drawer 或 Keyword 控制的属性不应因出现在 `Properties` 中就写入 CBUFFER。
- 新增或条件使用的 `Varyings` 字段，字段声明、顶点赋值、片元读取必须用同一 Keyword/Pass 宏保护；所依赖的类型、宏和 Include 必须在该文件使用前可见，禁止依赖后续 Include 的隐式顺序。
- 顶点位移、顶点动画、裁剪或 Alpha 语义发生变化时，Forward、ShadowCaster、DepthOnly、DepthNormals、Meta 及所有实际消费者必须复用同一几何/裁剪契约。只有没有顶点位移、顶点动画和自定义裁剪需求时，才优先复用 URP/项目通用 Pass Include，避免复制 Vert/Frag。

### CTL｜控制流、Keyword 与变体

#### CTL-01｜运行时控制流选择

不得凭习惯选择 `if / else`、`?:`、`lerp`、`step` 或 `smoothstep`。必须比较条件来源、执行阶段、两侧纹理采样与 ALU、分支发散、屏幕覆盖、导数/抗锯齿、数值边界和目标 GPU：

- 逻辑分支、昂贵路径可完全跳过且同一波前通常一致时，可考虑 `if / else`。
- 两侧都必须计算或只是在标量/向量结果间选择时，`?:` 或 `lerp` 更清晰；不得声称它们会自动减少采样。
- 硬阈值遮罩使用 `step`，需要连续过渡和抗锯齿时使用 `smoothstep` 或正确导数方案；不得以 `step` 替代有意软边。
- `lerp` 不是通用分支替代，两个端点的计算成本、采样和精度必须被计入。

无目标设备测量且不改变功能需求时，保持原实现；不以“无分支”或“Keyword 一定更快”作为优化结论。

#### CTL-02｜Keyword 作用域与变体预算

每个 Keyword 必须声明来源、作用域、阶段、互斥关系、材质/全局控制方、变体增量和剥离路径：

- 材质可收集且可剥离的开关优先 `shader_feature_local`；只影响顶点或片元时评估阶段限定形式。
- 运行时必须支持全组合或由管线控制的状态使用相应 `multi_compile`；全局 Keyword 只用于确实跨 Shader 的管线状态。
- 局部 Keyword 降低全局空间占用，但不自动降低变体数；`multi_compile_local` 仍会生成组合。
- 材质枚举不应自动改为 Keyword。只有编译期裁剪收益明确、变体预算可接受且存量材质/动画/脚本/动态加载已验证时才能迁移。
- 不默认新增 GPU Instancing、DOTS 或每实例属性访问路径；仅当用户明确要求，或目标 Shader 的现有模板/运行时链路已经依赖它们时，才按实际约定接入并计入变体与批处理验证。

### WRK｜最小实现、Pass 与 Shader Model

#### WRK-01｜实现范围和资源选择

以用户要求的效果为中心，只改完成该效果所需的材质、Pass、共享模块或渲染资源。屏幕空间效果优先匹配现有 RendererFeature/Blit Shader 的时序和资源约定；不得为了套模板重构无关管线或把屏幕资源逻辑塞进材质光照。

复杂功能编码前必须记录：领域、双轴分类、影响 Shader/材质/消费者、控制流与变体选择、目标平台、预算、Pass 矩阵、资源生产/消费和验证方式。

#### WRK-02｜默认 Pass 矩阵与 Shader Model

- **普通 Lit/PBR/Custom Lighting 材质**：除非明确 forward-only，默认提供 Forward Lit 及与相同材质输入一致的 `ShadowCaster`、`DepthOnly`、`DepthNormals` Pass；实际 Tag 必须匹配当前 URP/Profile。
- **普通 Unlit Shader**：默认只生成一个 Forward Unlit Pass（通常为 `SRPDefaultUnlit`），不默认生成 `ShadowCaster`、`DepthOnly`、`DepthNormals` 或 Meta。
- **专用领域**：Fullscreen、Decal、Post Effect、Water、Character、Outline、透明预写和 Custom Pass 不得机械套用默认矩阵，必须按实际消费者、深度/Stencil 和时序选择。
- **Shader Model**：所有新生成或重写的普通 Pass 默认声明并使用 `#pragma target 2.0`。既有高 target Pass 不得自动降级；提高 target 必须有已验证的技术/API/Profile 基线理由，并在交付与 `VAL-03` 中说明受影响平台。目标设备支持不明时先澄清。
- **可选能力**：屏幕空间主光阴影、SSAO、Light Cookies、Forward+、Detail、LOD Cross Fade、DOTS、GPU Instancing、Lightmap、附加灯和烘焙接口均按需求、现有模板和 Profile 启用；注释代码不构成支持能力，不得为了“预留”而默认制造变体。
- **粒子特例**：仅当目标确为粒子 Shader 且 Vertex Streams/ShaderGUI 已验证时，才传递粒子顶点色、使用粒子面板或支持相应 BlendMode；普通 Unlit 不默认承担粒子接口。

#### WRK-03｜行为保真重构顺序

先冻结 `CMP-01` 契约，再抽取/替换一个职责单元并用相同输入 A/B；先验证视觉、材质接口、Pass/时序，再确认性能。Pass、target、Keyword、属性名、默认值、枚举、资源名或渲染状态改变属于独立兼容性改动，不得随格式整理混入。

#### WRK-04｜修改既有 Shader 的最小落点

修改前先定位需求属于顶点/Varyings、Surface 输入、InputData、Lighting/Final Blend 或专用 Pass，禁止跨层扩散修改：

- 顶点位移、顶点动画、网格投射或需传给片元的顶点预计算，修改顶点阶段与对应 Varyings，并执行 `ORG-03` 的跨 Pass 一致性检查。
- 基础色、Mask、法线、自发光、透明、Alpha Clip、材质采样和局部混合优先修改 Surface 输入层；不要因为片元表现变化而改动共享 Lighting。
- 视线方向、屏幕坐标、shadowCoord、Fog、baked GI、Lightmap、TBN 或插值输入问题，修改 InputData/输入组装层；不要把输入构造散落回材质采样层。
- 只有需求明确改变 BRDF、直接/间接光、Toon Ramp、SSS、环境反射或最终合成时，才修改 Lighting/Final Blend。优先复用已有单光源或间接光扩展点，保留共享的主光/附加灯遍历、GI、Fog 和最终流程。

#### WRK-05｜标准 URP PBR / SimpleLit 生成检查表

本规则仅适用于未接入项目专用 Toon/Custom Kernel 的标准材质路径，文件拆分仍按 `ORG-01` 决定：

1. **Surface 输入**：按实际需求解码 Base、Normal、Metallic 或 Specular、Smoothness、Occlusion、Emission、Alpha/Alpha Clip；不因模板存在就默认暴露 ClearCoat、Detail、SSS、透明、烘焙或其他材质属性。
2. **Input 输入**：在光照调用前构造世界坐标、法线、视线、阴影坐标、Fog、GI、Lightmap 和 ShadowMask 等所需输入。GI/ShadowMask 属于光照输入，不应散落在材质采样逻辑中；未启用烘焙时不得伪称支持。
3. **光照模型**：PBR 使用当前 URP 版本的标准 PBR 入口，SimpleLit 保持 Specular/SpecGloss 语义，不能套用 Metallic 工作流。只有标准入口不能表达已确认需求时才转入 Adapter 或复杂分类。
4. **辅助 Pass**：ShadowCaster、DepthOnly、DepthNormals、Meta 复用相同的顶点、Alpha 和材质语义；Meta 只在需要 Lightmapper 采集 Albedo/Emission 时提供，不能把注释或未验证的 Meta 当作烘焙支持。
5. **可选能力**：Lightmap、Dynamic Lightmap、ShadowMask、Specular Highlights、Environment Reflection、Forward+、Light Cookies、SSAO、Detail、LOD Cross Fade、DOTS、Instancing 均按需求、Profile 和实际变体预算启用，不默认创建“未来可能使用”的 pragma。

### VAL｜验证与交付

#### VAL-01｜源码、Unity 编译与运行时引用

每次新增、修改或重构 Shader/HLSL/ShaderGUI 后必须完成：

1. 严格 UTF-8 解码，检查 `U+FFFD`、乱码、Include guard、声明/定义/调用、函数参数、Texture/Sampler、CBUFFER、Properties、Keyword、Pass、`LightMode`、`UsePass` 和 ShaderGUI 引用。
2. Unity Editor 导入受影响 Shader，确认没有本次引入的 Include、函数、重定义、语法、资源绑定或变体编译错误；文本检查不能替代编译。
3. 用目标材质与代表性场景实际绘制，确认 Renderer 选择正确 Pass，Keyword/纹理/全局资源生效。涉及 RendererFeature、动画、动态加载或 AssetBundle 时覆盖相机、构建和加载路径。

涉及顶点位置或裁剪变化时，验证所有实际使用的 ShadowCaster、DepthOnly、DepthNormals、Meta、DiffuseOnly、SceneSelection、Picking、MotionVectors 和项目自定义 Pass；不支持或未验证的 Pass 必须在 `VAL-03` 报告中标为相应状态，不能只以 Forward 正确作为结论。

#### VAL-02｜视觉、时序与性能回归

用固定场景/角色、相机、时间点、质量、曝光和输入做 A/B。验证主光、附加灯、阴影、GI、Fog、透明/裁剪、高频法线与高光（含 Specular AA）、深度/Stencil、目标 Pass、资源生产/消费和 Frame Debugger Draw 顺序。

在目标图形 API/设备测量纹理采样、ALU、分支发散、屏幕覆盖、透明 Overdraw、Pass、临时 RT、CPU Draw/Setup 和变体影响。场景任务覆盖相机与 RendererFeature；角色任务还覆盖部位组合、动画帧、表情、眨眼、透明层级和多角色。

#### VAL-03｜Shader 变体、功能支持与低配风险报告

新生成或重写 Shader，或新增、启用、删除会影响变体的 `#define`、`shader_feature`、`multi_compile`、`include_with_pragmas` 时，必须按 [`references/shader-variant-report.md`](references/shader-variant-report.md) 输出报告。

报告逐 Pass 列出来源、阶段、局部/全局作用域、互斥组、参考下限和参考上限。下限按已确认材质 Keyword、固定宏和不可剥离组合保守估算；上限按可见启用 pragma 选项组（含 `include_with_pragmas`）理论展开。**这些数字仅供参考，不等于 Unity 最终编译、剥离或打包数量**；自定义宏管理、ShaderVariantCollection、平台 stripping、材质 Keyword、RendererFeature 和构建设置均会改变结果。

报告还必须标明阴影、DepthOnly、DepthNormals、Meta/烘焙、SSAO、Light Cookies、Forward+、Detail、LOD Cross Fade、DOTS、GPU Instancing、Fog、Additional Lights、透明/BlendMode 的状态：`支持`、`默认未启用`、`默认不支持` 或 `不适用`。默认注释功能不得标为支持。最后列出移动端和低配机器的 Shader Model/API、变体内存/编译、采样/ALU、Overdraw、额外 Pass、临时 RT 和运行时 Keyword 风险，以及实际验证和剩余未验证项。

### ACC｜交付完成标准

#### ACC-01｜交付门槛

仅在以下条件满足后交付：职责和分类明确；未无理由修改 URP Package；材质/序列化/运行时写入兼容；Pass/Render State/资源时序经验证；中文注释与 UTF-8 自检完成；Unity 编译和风险相称的视觉、Frame Debugger、设备、性能和实际变体验证完成；命中 `VAL-03` 时已附报告；PROFILE 专属矩阵已通过。

### EVO｜规则演进与自动更新

#### EVO-01｜证据驱动更新闭环

每次已验证的编译错误、材质/Keyword 失配、视觉回归、Pass/时序问题、平台差异、性能瓶颈或 URP API 差异，都应形成规则候选：记录现象、根因、最终修正、适用范围、兼容性影响、验证证据和回退方式。

候选按范围落位：单一资产写功能说明；同类 Shader 写功能族条目；依赖版本/资产/插件/枚举的写 PROFILE；仅跨项目稳定且证据充分的写 CORE。自动化可收集候选，但不得静默改写已审定规则；规则冲突时修订原条目，不追加同义例外。

## 项目定制规则（PROFILE）

本 PROFILE 仅适用于当前工程。迁移到其他项目时保留 CORE，删除并重建整个 PROFILE；不得带入路径、版本、枚举、Pass 或资产假设。

当前 `ProjectACGMain` 工作区的 Unity 与 Substance Painter Shader 对齐事实已拆分到 [ProjectACG Unity 与 Substance Painter Shader 对齐 Profile](Profiles/ProjectACG/README_Tech_ProjectACGSubstancePainterShaderProfile.md)。其中的 `_MRATex`、Painter 11.0.2、Debug 数值、相机偏置半角向量、Unity 预卷积 Cubemap Atlas 布局与当前 Face/基轴契约只属于该工作区，不并入本文件 CORE。

当前 ProjectACG UI Gamma/sRGB 合成的实际路径、`UICamera`、`URP-UI-Renderer.asset`、Gamma UI Shader、RendererFeature、RT 格式和验证边界见 [ProjectACG Gamma UI / sRGB 合成 Profile](Profiles/ProjectACG/gamma-ui-srgb-composite.md)；跨项目的颜色域、Alpha/Blend、UI 特效和独立 RT 实现经验见 [`references/gamma-ui-color-domain-and-renderer-feature.md`](references/gamma-ui-color-domain-and-renderer-feature.md)。

当前 ProjectACG Skin/Face 肤色 LUT 的 `1024 × 32` 布局、暗部 Albedo、`_ShadowColor`/Ramp 职责、生成器、代表性色块、历史 G 半 texel 偏移和验证边界见 [ProjectACG 肤色颜色 LUT Profile](Profiles/ProjectACG/skin-color-lut.md)；跨项目的条带式 3D LUT 生成、颜色域、采样、插值连续性和圈层 Debug 见 [`references/packed-3d-color-lut-sampling-and-debugging.md`](references/packed-3d-color-lut-sampling-and-debugging.md)。

### PRJ｜当前工程共同基线

#### PRJ-01｜Unity、URP 与源码边界

- 工作区：`D:\work2025U3D\Valkyria\ProjectACG\Client`
- Unity：`2022.3.62f3`；URP：嵌入式 `com.unity.render-pipelines.universal@14.0.12`。
- 开发前以当前嵌入式 URP `14.0.12` 源码和 Unity `2022.3` API 为事实来源；外部示例须先核对 API、宏、签名和 Pass 语义。
- 不修改 URP Package。版本升级或管线改造按 `EVO-01` 建立候选，并完成受影响 Shader、RendererFeature、相机、构建与平台回归。

#### PRJ-02｜统一材质 Inspector

新增或维护的项目 Shader 默认使用：

```shader
CustomEditor "Scarecrow.SimpleShaderGUI"
```

阶段限定 Keyword 不得只依赖 GUI 摘要；必须检查材质实际 Keyword、对应 Pass 编译和构建保留变体。修改 Drawer、Foldout、Toggle、Enum、属性显示名或 Keyword 映射时，同时验证 `.mat`、Prefab、Animation、Timeline、脚本与 `MaterialPropertyBlock` 写入。

当前 `SCN-*` 与 `CHR-*` 目标范围默认使用 `Scarecrow.SimpleShaderGUI`。工程其他目录存在的 LWGUI 或其他历史 Inspector 只在目标 Shader 已绑定且依赖可用时原样维护；不得因外部模板建议而安装/迁移 LWGUI、DDGUI、粒子 ShaderGUI 或替换既有 `CustomEditor`。确需引入新的 Inspector 包或 Editor 代码时，先确认用户需求、包来源、版本、迁移范围和回退方案。

#### PRJ-03｜PerObjectShadowV2 的 Stencil 与屏幕时序契约

`Assets/Shader/RenderFeature/PerObjectShadowV2/` 的屏幕空间 Pass 在 `AfterPrePasses` 路径生成同帧 `_ScreenSpaceShadowMap`；当前实现将晚于 `AfterRenderingPrePasses` 的路径视为可使用相机缓存，Opaque 可能读取上一帧结果。运动镜头要求同帧对齐时，不得把 `AfterOpaques` 当成 Stencil 冲突的最终修复。

`PerObjectShadowProjector` 的 `m_StencilRef` 是 POS 投影阶段的临时对象 ID。分配器从 `2` 递增并跨对象生命周期持续存在，而 Eye/Fringe 使用以 `16` 为阈值的全字节 Stencil 比较；战斗创建大量 Projector 后再进入卧室时，高 ID 会在同一帧干扰角色 Stencil。该问题的根因是 POS 把临时 ID 写进相机公共 Stencil 且生命周期越过了自身最后一个消费者，不是旧场景像素跨场景残留。

当前修复契约是在环境阴影和自阴影体积投影完成后、Eye/Fringe 绘制前，通过 `PerObjectShadowStencilClear` 对同一批 `stencilRenderers` 执行 `Ref = 当前对象 ID`、`Comp Equal`、`Pass Zero` 的定向清理，并保持 `screenSpacePassTiming: 0`。不得用限制 ID 到 `2..15`、仅重置分配器或改到 `AfterOpaques` 代替该生命周期修复；前两者会引入活动对象 ID 冲突，后者会产生历史帧拖后。

`Enable Eye Reveal Depth Prepass` 是独立深度功能，不是 POS Stencil 修复开关。当前工程未发现 `LightMode = EyeRevealDepth` 的 Shader Pass 或 `_CharaEyeRevealDepthTexture` 的 Shader 消费者；不得为解决眼睛/刘海 Stencil 异常而开启。验证必须覆盖“直接进卧室”和“战斗创建角色/武器后进卧室”、快速移动镜头、多角色、自阴影/环境阴影，并在 Frame Debugger 确认 `Stencil 写入 → Volume 投影 → StencilClear → Eye/Fringe` 顺序。

### SCN｜场景 Shader Profile

#### SCN-01｜场景分类与参考范围

| 场景分类 | 当前工程的实现原则 | 必读资产/链路 |
| --- | --- | --- |
| 简单非 PBR | 一个 `.shader` 完成局部 Unlit/Decal/辅助效果；先确认 Queue、Blend 与消费者。 | 目标 Shader、材质、RendererFeature（如有）。 |
| 标准 PBR | 优先 URP 标准 Include、数据流和通用 Pass；不建立独立光照。 | 目标 PBR Shader、URP `Core.hlsl` / `Lighting.hlsl` / `Shadows.hlsl`。 |
| 复杂 PBR / 屏幕资源 | BaseLit、投射、POS、Terrain、Water、复杂透明或 RendererFeature 依赖；维护资源链与时序。 | `Assets/Shader/Scene/New/`、`Assets/Shader/RenderFeature/`、目标材质与相机。 |

场景标准 PBR 的文件拆分以实际职责和现有参考为准：`BattleSceneBaseLit` 使用 Shader、输入和 Pass Include 分层，不强制外部模板的固定三文件命名。只有确有跨 Pass/跨 Shader 复用时才新增功能域 Include；简单 Shader 保持单文件。

#### SCN-02｜BaseLit 与 Per-Object Shadow 合约

`Assets/Shader/Scene/New/PackedMaskPBR/BattleSceneBaseLit.shader` 是场景复杂 PBR 参考。其 `_UsePerObjectShadow` 编码为 `UnityOnly=0`、`Both=1`、`POSOnly=2`、`Off=3`，不得与角色同名枚举混用。

POS 由 `Assets/Shader/RenderFeature/PerObjectShadowV2/` 的 `PerObjectShadowFeature` 生产 `_ScreenSpaceShadowMap`。`Both` 仅将 POS 与 Unity 主方向光阴影相乘，`POSOnly` 不保留 Unity 主光阴影；`_ReceiveShadows` 对 Unity 主光与附加灯阴影的影响及其与 POS 的既有组合顺序必须保持。屏幕空间投射时 POS、Fog 和主光阴影使用投射后位置。

#### SCN-03｜场景 Pass、target 与验证矩阵

BaseLit 当前 Forward、ShadowCaster、DepthOnly、DepthNormals、Meta 均使用 `#pragma target 3.5`，是既有资产的兼容性基线，不得因 `WRK-02` 自动降级。场景目录同时存在 `2.0`、`3.0`、`3.5`、`4.5`，每个既有 Pass 必须按源码和目标平台单独判断。

BaseLit/POS 改动必须覆盖 `_UsePerObjectShadow` 四种模式与 `_ReceiveShadows`，并在 Frame Debugger 验证屏幕资源生成、绑定、投射、Fog、Draw 顺序和多相机行为。高频法线/高光改动还应验证 Specular AA 前后稳定性与高光宽度。

### CHR｜角色 Shader Profile

#### CHR-01｜角色分类与家族边界

| 角色分类 | 当前工程的实现原则 | 必读资产/链路 |
| --- | --- | --- |
| 简单非 PBR | 简单覆盖层或辅助效果可单文件；先确认部位 Queue、Stencil、深度、动画和消费者。 | 目标 Shader、材质、Prefab、动画。 |
| 标准 PBR | 新增纯 PBR 角色材质优先复用 URP 标准 PBR，不复制主光/BRDF/GI。 | 目标 Shader、URP Include、材质和 Pass 消费者。 |
| 复杂 Toon / Hybrid / 多 Pass | Chara_V2 部位输入、Common Kernel、Outline、透明深度、平面阴影、POS 和动画接口集中维护。 | `Assets/Shader/Character/Chara_V2/`、Common、Binding、角色/动画场景。 |

当前 Chara_V2 家族包含 Skin、Face、Hair、Cloth、SilkStockings、Eye、Brow、Fringe、EyeBlend、EyeShadow、HairShadow 与 Monst。各部位属性在私有 Binding 中，跨部位稳定逻辑才进入 `Common/`；不得复制完整部位实现作为新功能起点。

Chara_V2 的现有组织为部位 `.shader`、私有 Binding 与 `Common/` 共享模块，不强制改造成 Surface/LightModel 三文件模板。新增纯 PBR 角色材质可按标准 URP 数据流组织，但一旦接入角色化 Toon/Hybrid、共享光照或专用 Pass，必须遵守 Chara_V2 的 Binding/Common/Pass 边界。

#### CHR-02｜共享 Lighting Kernel 与 Per-Object Shadow

`Common/ToonBRDF_V2.hlsl`、`Chara_AdditionalLights_V2.hlsl`、`Chara_GlobalVirtualLight_V2.hlsl`、`Chara_RimShared_V2.hlsl`、`Chara_FinalColorGradient_V2.hlsl`、`Func_Chara_GlobalEffect_V2.hlsl` 分别承载共享 BRDF、附加灯、主光/虚拟光、Rim、最终颜色和全局效果。新增逻辑必须说明插入在直接光、间接光、Rim、Debug 或最终颜色的哪一阶段，并回归所有消费者。

`Common/Func_Chara_PerObjectShadow_V2.hlsl` 的 `_UsePerObjectShadow` 编码为 `Off=0`、`UnityOnly=1`、`POSOnly=2`、`Both=3`。该编码与 BaseLit 不同；角色 POS、Rim 深度、透明深度和 Fog 必须使用正确当前相机的缩放屏幕坐标。修改共享方向、光色或阴影时，必须做同一角色的多部位组合回归。

#### CHR-03｜角色 Pass 矩阵

| 部位/家族 | 当前关键 Pass | 维护边界 |
| --- | --- | --- |
| Skin / Cloth / Hair / SilkStockings | `UniversalForward`、`Outline`、`CharacterExposureOutlineMask`、`PlanarShadow`、`DepthOnly` | 不因其他部位有 Caster/DepthNormals 就擅自添加。 |
| Face | `TransparentDepthPrepass`、`UniversalForward`、`Outline`、`CharacterExposureOutlineMask`、`PlanarShadow`、`ShadowCaster`、`DepthOnly`、`DepthNormals` | 覆盖 SDF、表情、眉眼遮挡和透明深度。 |
| Monst | `UniversalForward`、`Outline`、`CharacterExposureOutlineMask`、`PlanarShadow`、`ShadowCaster`、`DepthOnly`、`DepthNormals` | Crystal/怪物扩展不破坏共享语义。 |
| Fringe | `Fringe`、`FringeOuter`、`Outline`、`CharacterExposureOutlineMask`、`PlanarShadow`、`DepthOnly` | 不能替换成普通 Forward 而不验证脸部遮挡与排序。 |
| Eye / Brow / 辅助层 | `Eye`、`TransparentDepthPrepass` 或专用叠加路径 | 检查眨眼、表情、透视、近景和实际注入方。 |

注释掉或未被 Renderer 选择的 Pass 不构成支持能力。角色 Forward 正确不代表 Outline、透明深度、Stencil、平面阴影、Caster 或 Depth Pass 正确。

#### CHR-04｜角色 target、材质与运行时接口

Chara_V2 当前存在 `#pragma target 3.0` 和 `3.5`。例如 `Chara_Cloth_V2.shader` 的 `Outline`、`CharacterExposureOutlineMask` 和 `DepthOnly` 当前使用 `3.5`，属于既有角色兼容性基线，不自动降级。

角色材质属性、Drawer、Keyword、动画曲线、Timeline 和 `MaterialPropertyBlock` 都是公开接口。不得为“统一格式”批量改写早期 `shader_feature`、`shader_feature_local` 或 URP `multi_compile`；必须逐 Shader、Pass、材质、动画、Variant Collection、动态加载和实际保留变体迁移。

#### CHR-05｜角色验证矩阵

角色改动必须覆盖目标部位、共享消费者、代表性材质、角色姿态、动画时间点、近远景、运动镜头、透明/Stencil/深度层级、相机、目标设备和实际构建。共享 Kernel、POS 或全局效果改动不得只验证一个部位或 T-Pose 静态画面。

## 资源加载与规则维护

### 按任务加载

- **新增简单非 PBR / Unlit Shader**：读取 `ORG-01`、`WRK-02`、[`references/shader-file-organization.md`](references/shader-file-organization.md) 与目标附近的 Shader。只有目标是粒子时，再读取本节的粒子/ShaderGUI 资源；不要套用 Lit/PBR 多 Pass 或粒子模板。
- **新增或重写标准 URP PBR / SimpleLit**：读取 `ARC-01`、`ARC-02`、`WRK-02`、`WRK-05`、[`references/urp-shader-patterns.md`](references/urp-shader-patterns.md)、[`references/shader-file-organization.md`](references/shader-file-organization.md) 及当前 URP 包源码。按实际需求决定材质输入、烘焙、Meta 与可选功能。
- **Toon / Custom Lighting / 复杂材质**：读取 `WRK-04`、`WRK-05`、对应 `SCN-*` 或 `CHR-*`、目标 Binding/Common/Adapter 与所有消费者。需要扩展共享光照时先确认已有入口，不生成新的独立 Lighting 框架。
- **屏幕资源、RendererFeature、透明/深度/阴影链路**：读取 `ARC-03`、`ARC-04`、[`references/renderer-feature-stencil-and-timing.md`](references/renderer-feature-stencil-and-timing.md)、[`references/project-integration-checklist.md`](references/project-integration-checklist.md)、相关 `ScriptableRendererFeature` / `ScriptableRenderPass` 与目标相机；使用 Frame Debugger 确认生产、绑定、消费与清理时序。
- **UI Gamma/sRGB 颜色域、独立 UI RT 或 UI Composite**：读取 [`references/gamma-ui-color-domain-and-renderer-feature.md`](references/gamma-ui-color-domain-and-renderer-feature.md)，再读取目标工程的 UI Shader、Canvas、相机、RendererFeature、RT 格式和特效消费者；不要仅靠 Shader 内 `GammaToLinear/LinearToGamma` 推断 Blend 颜色域。
- **2D 条带式 3D 颜色 LUT、调色与切片圈层排查**：读取 [`references/packed-3d-color-lut-sampling-and-debugging.md`](references/packed-3d-color-lut-sampling-and-debugging.md)，冻结色立方体轴顺序、输入/存储/采样颜色域、半 texel、Importer 和最终合成职责；先用 Identity 分层输出，不用 Shadow Tint 或曝光掩盖坐标问题。
- **材质 Inspector、Drawer、Keyword 与动画接口**：读取 `PRJ-02`、`Assets/Plugins/CustomShaderGUI/README_Tech_ShaderGUIFeatureShowcase.md`、`ShaderGUIFeatureShowcase.shader`、`Editor/SimpleShaderGUI.cs` 和对应 `PropertyDraw/*.cs`。默认以 `Scarecrow.SimpleShaderGUI` 的真实 Drawer、隐藏状态属性与 Keyword 行为为准，不引用外部 LWGUI/DDGUI 语法。
- **生成/重写 Shader 或调整变体来源**：读取 `CTL-02`、`VAL-03` 与 [`references/shader-variant-report.md`](references/shader-variant-report.md)，并在交付中输出变体参考、功能支持状态、低配风险和实际构建验证边界。
- **Unity 与 Substance Painter 自定义预览 Shader 对齐**：先读 [`references/unity-substance-painter-parity.md`](references/unity-substance-painter-parity.md)，按材质数据、直接光、间接光、合成和显示域分层；当前 `ProjectACGMain` 任务再读 [`Profiles/ProjectACG/README_Tech_ProjectACGSubstancePainterShaderProfile.md`](Profiles/ProjectACG/README_Tech_ProjectACGSubstancePainterShaderProfile.md)，不得把项目 MRA、Debug 数值或 Environment 校准写成通用默认值。
- **ProjectACG Gamma UI 实现或维护**：读取 [`Profiles/ProjectACG/gamma-ui-srgb-composite.md`](Profiles/ProjectACG/gamma-ui-srgb-composite.md) 与通用 Gamma UI 参考；以当前 `UICamera`、`URP-UI-Renderer.asset`、`GammaUIDefault` 和 `GammaUICompositeFeature` 的真实序列化配置为准。
- **ProjectACG Skin/Face 肤色 LUT 实现或维护**：读取 [`Profiles/ProjectACG/skin-color-lut.md`](Profiles/ProjectACG/skin-color-lut.md) 与通用 3D LUT 参考；以当前 Skin/Face Shader、`SkinColorLutGenerator`、功能旁 README 和最终 TextureImporter 回读为准，历史半 texel 修正必须同步覆盖脸身和存量 LUT A/B。
- **加密代理 Shader 明文还原与独立化**：先读 [`references/encrypted-proxy-shader-restoration.md`](references/encrypted-proxy-shader-restoration.md)，确认授权、代理/容器/工具映射、可信反序列化、Property/Keyword/Pass 保真、材质回退和 Unity 编译/视觉验证边界；批量提取与批量材质迁移必须分阶段。
- **新增文件、材质配置、RendererFeature 或集成风险审查**：读取 [`references/project-integration-checklist.md`](references/project-integration-checklist.md)，核对 `.mat`、Prefab、Scene、Animation、Timeline、脚本写入、AssetBundle、相机和 Renderer 配置。

资源加载应遵循最小充分原则：只读取与任务分类和风险相关的资料；若 Profile、目标资产或当前嵌入式 URP 与参考文档冲突，以实际项目事实为准并记录差异。

| 任务 | 读取资源 | 目的 |
| --- | --- | --- |
| 任意 Shader 改动 | `DOC-03`、`PRJ-01`、版本文件、包文件、目标与附近 Shader/Include/材质 | 确认 API、接口、既有风格与编译入口。 |
| 场景复杂 PBR / POS | `SCN-02`、`SCN-03`、BaseLit、PerObjectShadowV2、目标相机 | 保持枚举、阴影合并和时序。 |
| 角色复杂 Toon / 多 Pass | `CHR-02` 至 `CHR-05`、Binding、Common、Prefab、动画 | 保持 Kernel、部位层级、动画和 Pass 契约。 |
| 2D 条带式 3D LUT / 肤色 LUT | [`references/packed-3d-color-lut-sampling-and-debugging.md`](references/packed-3d-color-lut-sampling-and-debugging.md)、目标 Shader/生成器/Importer、当前项目 Profile | 冻结颜色域和布局，验证插值连续性，区分 LUT、阴影色和 Ramp。 |
| 分支 / Keyword / GUI | `CTL-*`、`PRJ-02`、材质、脚本、动画、构建证据 | 评估变体、序列化和运行时写入。 |
| 生成/重写 Shader 或变体变化 | `WRK-02`、`VAL-03`、[`references/shader-variant-report.md`](references/shader-variant-report.md) | 输出变体参考、功能支持、低配风险与实际验证边界。 |
| Unity / Painter Shader 对齐 | [`references/unity-substance-painter-parity.md`](references/unity-substance-painter-parity.md)、目标 Unity/Painter Shader、材质、贴图导入、当前项目 Profile | 分层验证通道、Direct、Environment 和显示域，隔离项目校准。 |

规则新增、修改或废弃时使用 `EVO-01` 记录候选来源、根因、范围、验证与回退。已存在规则能表达的内容必须更新原条目；不得重新拆分为场景/角色重复 CORE，也不得把项目事实写入 CORE。
