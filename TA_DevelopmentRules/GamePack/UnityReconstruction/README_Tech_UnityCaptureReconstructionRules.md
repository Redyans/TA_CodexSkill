---
name: ta-development-rules-unity-capture-reconstruction
description: 标准 UnityFS 客户端与 RenderDoc 抓帧的资源、动画和渲染效果复刻规则；区分通用规则、操作参考和项目证据。
---

# TA 通用开发规则｜Unity 抓帧复刻模块（CORE + PROFILE）

## 概览

> **文档模式**：ta-development-rules/unity-capture-reconstruction/v1
> **语言与编码**：中文，UTF-8
> **总入口**：[TA 通用开发规则](../../README_Tech_TADevelopmentRules.md)

本模块适用于已获授权的本地 Unity 客户端资源与 GPU 抓帧分析，以及在目标 Unity 工程中恢复可编辑的模型、动画、材质、光照、背景和后处理。它不替代具体资产旁的功能 README，也不把单帧结果扩展成所有镜头和动画时刻的结论。

按任务读取 [资源与渲染复刻工作流](references/unity-asset-and-render-reconstruction-workflow.md)、[验证与故障排查](references/renderdoc-validation-and-troubleshooting.md)，再读取 [依盖之书 Profile](Profiles/BookOfYog/linglong-showcase-reconstruction.md)。加密容器的专项流程见既有[终末地规则](../TA_终末地Unity资源解包逆向与渲染复刻开发规则.md)；标准 UnityFS 不默认套用其 VFS/BLC/CHK 解密步骤。Shader、RendererFeature 的一般职责仍以 [Shader 开发模块](../../ShaderDevelopment/README_Tech_URPShaderDevelopmentRules.md) 为准。

## 工作流程

1. **加载上下文**：确认授权输入、游戏/目标 Unity 版本、渲染管线、图形 API、参考图和抓帧是否同一时刻。
2. **分类并冻结目标**：界定角色、环境、UI 和动态历史的范围；为原始文件、目标画面和工程状态建立哈希与版本清单。
3. **建立依赖闭包**：索引 bundle 和对象引用，恢复模型、骨架、贴图、动作及材质映射，记录不能解析的依赖。
4. **恢复 GPU 数据流**：按实际 draw/dispatch、资源版本、常量缓冲和 Pass 顺序重建 Shader、光照、阴影、反射、AO 与后处理。
5. **最小集成**：把资源、证据、脚本和可修改管线集中在工程内；只修改有证据要求的管线入口并保存回退配置。
6. **分层验证与交付**：先核对字节、曲线和几何，再做同输入 GPU 对比、实时场景区域对比及动态播放，分别声明结论范围。

## 通用规则（CORE）

### EVD｜输入与证据

#### UCR-EVD-01｜固定原始输入与结论来源

对客户端、截图、抓帧和目标工程建立独立来源记录，所有推断都能回到原文件、实际 GPU 事件或可复现的宿主验证。

- **必须（MUST）**：保存源路径、大小、SHA-256、抓帧事件/资源版本、工具版本和生成命令；区分原始证据与处理后的预览图。
- **应当（SHOULD）**：以只读副本分析大型客户端资源，用清单连接源包、依赖、导出物和 Unity 资产。
- **禁止（MUST NOT）**：仅凭截图推定未执行的 Shader 变体、隐藏几何、动画状态或此前帧的模拟历史。
- **验证**：抽查源文件哈希，按清单复现至少一条“源对象 → GPU 事件 → 工程资产 → 比较报告”的证据链。
- **例外与回退**：无法获得某项原始输入时，记录缺口并将对应结论降为候选；不补造来源。

#### UCR-EVD-02｜以实际执行路径确定渲染契约

复刻的单位是特定帧的实际 draw、dispatch 和资源读写链，而不是材质名或静态 Prefab 标志。

- **必须（MUST）**：记录目标 draw 的网格/子网格、VS/PS、状态、纹理视图与 sampler、常量缓冲、RT 格式、Pass 顺序和资源在该事件的内容。
- **应当（SHOULD）**：把不透明、透明、阴影、反射、AO、TAA、Bloom、调色和 UI 分开定位，再确认跨 Pass 的输入输出。
- **禁止（MUST NOT）**：把同一 GPU Resource ID 在另一时刻的内容、未使用的矩阵槽或静态序列化开关当作当前 draw 的事实。
- **验证**：在抓帧中回溯每个关键输出的最后写入者及直接读取者，与 Unity 实际 Frame Debugger/回读顺序核对。
- **例外与回退**：抓帧缺少历史帧时，只验证当前可见资源和同输入算法，不宣称恢复完整时间行为。

### AST｜资源与动作

#### UCR-AST-01｜验证依赖闭包与持久化资产

模型、骨架、动画和贴图必须在目标 Unity 中成为可引用、可播放、可追溯的资产。

- **必须（MUST）**：按对象引用和 bundle 依赖提取；保存原骨名、绑定路径、顶点通道、纹理格式/mip/face、sRGB 与 sampler 语义；导入后检查序列化、引用和实际播放。
- **应当（SHOULD）**：将角色专属动作放在该角色动画目录，身体与武器等独立骨架分开标注；无法映射的曲线保留原 hash、key 与切线供后续研究。
- **禁止（MUST NOT）**：以“`.anim` 已生成”或“PNG 看起来相似”代替曲线、原生纹理和渲染输入的验证。
- **验证**：统计 clip 数、曲线/key 数、缺失路径、实际骨骼位移；检查纹理全部 mip/face 字节或记录有损转换边界。
- **例外与回退**：原引擎/平台格式目标不支持时，保留原字节和转换日志，用量化误差说明替代资产。

### VAL｜复刻质量与交付

#### UCR-VAL-01｜分离模块一致性和整场景一致性

相同原始输入下的单 Pass 校验、实时重建输出和最终显示图分别是不同的验证命题。

- **必须（MUST）**：每份指标标注输入来源、时间点、分辨率、颜色域、GPU API、像素范围、掩码及是否配准/拟合；保留 MAE、RMSE、最大误差或逐字节结果。
- **应当（SHOULD）**：按原生资源、几何/动作、独立 GPU Pass、实时缓冲、HDR、最终图和动态播放逐层定位差异。
- **禁止（MUST NOT）**：把 TAA/AO 等同输入逐 bit 一致写成整场景逐 bit 一致；把不同时间点 JPG 与 RDC 直接做像素验收。
- **验证**：至少一次原参考图自比较为零；新报告用新的前缀保存，避免覆盖旧阶段证据。
- **例外与回退**：无法锁定动态历史时，报告当前可复现的固定姿态/当前输入结果和未验证范围。

#### UCR-VAL-02｜在实际目标宿主验证管线和资源入口

离线编译和脚本解析不能证明 Unity 目标工程成功加载、渲染并播放。

- **必须（MUST）**：检查目标工程实际 Unity/URP 包解析、脚本编译、场景缺失引用、目标图形 API 的 GPU 输出和 PlayMode；修改 Package/Graphics/Quality 前保留可恢复配置。
- **应当（SHOULD）**：使用独立后台 Worker 或批处理验证，减少对正在使用的编辑器和鼠标的干扰。
- **禁止（MUST NOT）**：用 `-nographics` 的结果证明 Shader 或后处理正确；把后台任务“已启动”当成异步验证“已完成”。
- **验证**：保存编辑器日志与最终 JSON/图像报告；核对 `.meta`/GUID、包 source 与场景依赖。
- **例外与回退**：目标 GPU 暂不可用时只报告静态/离线检查，待真实宿主验证后再提升结论等级。

## 项目定制规则（PROFILE）

《依盖之书》玲珑展示场景的路径、版本、资源集合、算法数值、已解决故障及当前验证状态全部在 [BookOfYog Profile](Profiles/BookOfYog/linglong-showcase-reconstruction.md)。不得把该 Profile 的事件号、矩阵、包安装方式或指标直接用于其他角色或游戏。

## 资源加载与规则维护

| 任务 | 最小加载资料 |
| --- | --- |
| 新角色资源解包 | `UCR-EVD-01`、`UCR-AST-01`、工作流的索引/动画段、目标项目 Profile、当前角色依赖清单 |
| Shader/环境/后处理复刻 | `UCR-EVD-02`、`UCR-VAL-01`、工作流的 GPU 数据流段、对应 Shader 模块规则、实际 RDC |
| URP 管线改动 | `UCR-VAL-02`、目标 `ProjectVersion.txt`、manifest/lock、当前 Renderer 与回退配置 |
| 排错与复测 | [验证与故障排查](references/renderdoc-validation-and-troubleshooting.md)、最新带时点的报告与编辑器日志 |

新问题先写入项目 Profile 的事实与证据；复现、根因和跨项目适用范围确认后，再提炼至 reference 或 CORE。项目源码和资产的单功能说明继续放在其附近。
