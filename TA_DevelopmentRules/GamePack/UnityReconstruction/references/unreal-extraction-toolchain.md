# UE 资源解包与 Unity 转换工具链参考

> 类型：REFERENCE；适用范围：已授权的本地 UE 客户端资源、原生参数与 GPU 抓帧分析；使用前提：按目标引擎、游戏分支、容器和目标 Unity 版本重新验证。

## 1. 适用场景

本页用于把 UE 容器中的模型、贴图、骨架、动作、材质参数和运行时依赖转换成可追溯的 Unity 资产。它解决工具选择、原始数据保存、映射完整性和转换排错，不代替展示玩法或完整渲染器实现。

规范来源为 [客户端到 Unity 复刻模块](../README_Tech_UnityCaptureReconstructionRules.md)：输入身份用 `UCR-EVD-01`，实际渲染契约用 `UCR-EVD-02`，依赖/资产持久化及实际求值来源用 `UCR-AST-01/02`，并行输出所有权用 `UCR-ISO-01`，分层结果、目标宿主与正式实时输入验收用 `UCR-VAL-01/02/03`。本页只提供操作方法，不另立一套强制规则。

具体工具版本、补丁、命令与验证数字见 [DragonSwordAwakening Profile](../Profiles/DragonSwordAwakening/character-rendering-reconstruction.md)。更深入的曲线/坐标问题见 [UE 动画与次级运动](unreal-animation-and-secondary-motion.md)，Shader 与后处理见 [UE 渲染链移植](unreal-render-pipeline-porting.md)，实际导入与播放见 [隔离 Unity Worker 与交付验证](isolated-unity-worker-and-delivery-validation.md)。

## 2. 前提与依赖

### 2.1 先冻结输入与输出边界

建立游戏根、抓帧根、Unity 工程根和独立复刻根别名，保存原始容器、Shipping、截图、RDC、目标 Unity/包锁定的身份。大型文件可保存只读原件与哈希清单；每份派生数据记录源对象/包、脚本版本和参数。需要本地秘密输入的解包器由本地文件读取，秘密不进入命令行回显、日志、报告或共享清单。

分别记录“要还原的角色/展示状态”和“支持它的依赖闭包”。展示动作可能引用另一目录中的武器、共用 Montage、面部曲线或 Notify；UI 资源可以作为查找展示逻辑的证据，而不导入目标 UI。

### 2.2 按缺口选择工具

| 缺口/问题 | 优先能力 | 结果检查与边界 |
| --- | --- | --- |
| 容器、资产名与目录概览 | FModel 等资产浏览器或兼容版本的包解析库 | 记录能否挂载、未加载容器、文件总量；浏览成功不等于正式导出成功 |
| 批量可复现导出、属性/骨架原始数据 | CUE4Parse 等可修改解析库与项目 CLI | 保存本地补丁和产物清单；上游版本号不能表示本地改动 |
| 旧引擎或特殊模型导出兼容性 | UModel 等兼容性探针 | 先用小样本核验骨架/权重/动作；文件存在不能成为生产来源证明 |
| 压缩动画解码 | 与目标格式匹配的 ACL/native decoder | 核验 managed/native ABI、依赖 revision、末帧与默认轨道；managed build 成功不足以证明 native decoder 可用 |
| 骨架、morph、动画中间格式转换 | Blender 等 DCC 的后台脚本 | 记录导入/导出选项，检查索引、bind、颜色空间和单位；预览相似不足以验收 |
| 缺失原生字段/默认值 | 反射表、可用引擎源码、受控反汇编 | 区分序列化值、继承默认值、构造值和运行期赋值 |
| 当前帧 Shader、光照与后处理 | 可实际回放目标 RDC 的 RenderDoc | 用实际 draw/dispatch、资源版本与 CB；资产浏览器无法替代这一层 |
| DXIL/DXBC 分析与目标编译 | 对应 DXC/反汇编器、目标平台编译器 | 编译成功只证明可编译，仍需目标 GPU 同输入比较 |
| 最终 Unity 引用、渲染和播放 | 匹配版本的真实 Unity 宿主/隔离 Worker | 核对实际包来源、GUID、帧输出与完成报告；无 GPU 模式不证明画面 |

这张表按能力选工具，不要求每项都安装。先尝试现有匹配工具；只有明确缺口才增加依赖。记录“实际用于生产”“仅浏览/交叉核验”“已安装未验证”，避免以安装清单冒充导出链。

常用官方入口如下，用于查找源码、发布版和帮助；本表不表示某个最新版本已经通过目标游戏验证，也不补证历史下载来源。选择版本后按下一节保存身份。

| 工具 | 官方入口 | 取用时关注 |
| --- | --- | --- |
| CUE4Parse / FModel | [CUE4Parse](https://github.com/FabianFG/CUE4Parse)、[FModel](https://github.com/4sval/FModel) | 游戏分支、mappings、native decoder、现有补丁 |
| UModel | [UE Viewer](https://www.gildor.org/en/projects/umodel) | 支持游戏/引擎列表及实际样本兼容性 |
| RenderDoc | [官网](https://renderdoc.org/)、[源码](https://github.com/baldurk/renderdoc) | 抓帧 API、Replay 兼容性、Python 模块与运行时匹配 |
| Blender | [下载](https://www.blender.org/download/) | 固定版本、后台模式、GLB/FBX 导入导出选项 |
| ACL / RTM | [ACL](https://github.com/nfrechette/acl)、[RTM](https://github.com/nfrechette/rtm) | revision 配套、ABI、压缩版本与默认轨道 |
| DXC / FidelityFX | [DXC](https://github.com/microsoft/DirectXShaderCompiler)、[FidelityFX SDK](https://github.com/GPUOpen-LibrariesAndSDKs/FidelityFX-SDK) | 目标 Shader Model、实际指令/CB 布局、头文件依赖和许可证 |
| .NET / CMake | [.NET](https://dotnet.microsoft.com/download)、[CMake](https://cmake.org/download/) | 目标 framework、native 架构及生成器 |
| Capstone | [项目](https://www.capstone-engine.org/) | 指令集、位宽与反汇编版本 |

### 2.3 保存可重建的工具身份

工具来源优先官方仓库、发布页或供应商安装包。下载记录包含来源 URL、release/tag/commit、许可证、下载件 SHA-256、执行文件 SHA-256 和取用日期；自编译工具还记录本地补丁、构建命令、编译器、运行时、native 子模块 revision 与生成文件哈希。

旧文件只剩本地字节时可补算 SHA，并明确“下载来源未追溯”；哈希不能逆向证明下载来源。不要为了补清单扫描无关用户目录，也不要将密钥候选文件包含进递归文本输出。

建议的来源清单字段：

```json
{
  "role": "production-exporter",
  "sourceUrl": "<official repository or release>",
  "sourceRevision": "<tag or commit>",
  "localPatchIdentity": "<patch SHA-256 or clean-tree revision>",
  "archiveSha256": "<sha256, or null with reason>",
  "binarySha256": "<sha256>",
  "runtime": "<actual runtime version>",
  "nativeDependencies": [],
  "licenseRef": "<saved license path>",
  "validatedCapability": "<tested sample and report path>"
}
```

## 3. 实现或排查步骤

### 3.1 挂载、索引并冻结导出任务

1. 识别引擎版本、容器形式与游戏专用分支。用小样本确认解析版本，不仅依赖目录名或工具默认 UE 版本。
2. 挂载并保存容器状态、未加载项、文件索引和错误。全挂载成功是后续分析前提，不能自动证明所有对象格式都支持。
3. 用索引匹配已知角色对象，再用 AssetRegistry、hard/soft references 和实际展示配置递归扩展依赖。
4. 把生产任务写成 manifest：源包/对象、角色归属、依赖、输出路径、转换入口、预期资产种类。对缺失项保留明确状态。
5. 冻结该轮输入和输出前缀；共享索引/汇总由单进程维护，并发任务读取冻结结果或写独立目录。

依赖清单可分别统计源包、源对象、解码片段、Unity clip、FBX 和展示动作。它们不是同一个数量：一个 Montage 可引用多条 sequence，一个序列也可能被多个菜单动作复用。

### 3.2 验证 unversioned/native mappings

借用 usmap 能帮助启动解析，但它只是候选 schema。遇到属性截断、字段语义异常或同一配置只导出前半段时，按以下顺序排查：

| 观察 | 下一步证据 | 可接受的结论范围 |
| --- | --- | --- |
| 对象读取报错或越界 | 原始 export 边界、属性数、继承链、目标分支结构 | 先确认布局错误，避免把后续读出的数字当成真实参数 |
| 无报错但字段明显少 | 原生反射表、class/super、字段索引/类型/嵌套结构 | 只有结束位置和字段结构吻合后才提升完整性结论 |
| 字段不存在 | CDO/构造默认值、蓝图默认值、实例覆盖和运行期写入 | 缺省可能来自继承或构造，不能直接置零 |
| 静态属性与画面不同 | 实际 RDC CB、脚本/PropertyAccess、动态输入 | 静态配置说明候选行为，抓帧说明当前行为 |
| 只导出常规属性仍不足 | bytecode、sparse data、图节点和表达式绑定 | 导出脚本数据并保存引用；不能用自设常量替换遗漏表达式 |

定制 mappings 同时保存字段名称、类型、索引、super、源地址/源码依据和导出结束验证。成功样本至少覆盖目标角色真正用到的 class，而不只是任意能解开的资源。

### 3.3 保存原始通道，再转换目标格式

原始资源与可导入资产分开保存：原容器/原始 export、结构化 JSON/TRS/曲线、转换中间件、Unity 最终资产各有清单。带 Unity 忽略标记的资料目录适合存工具和大证据；具体目录名由项目 Profile 定义。

| 资源 | 转换前保留 | 转换后抽查 |
| --- | --- | --- |
| Skeletal Mesh | 顶点/索引、子网格、材质槽、bounds、bone map、bind、全部 influence、原颜色与法线/切线编码 | 顶点重排映射、重复 influence 合并、bind/scale/mirror、所有通道、实际蒙皮 |
| Skeleton | 直接读取的 local translation/quaternion/scale、父索引、原骨名 | 禁用有损的 matrix→TRS 往返作为唯一真值；分别检查纯 q 与矩阵/位置 |
| 纹理 | 原格式、所有 mip/face、sRGB、view、swizzle、sampler、原字节 | PNG 仅预览；目标解压/上传字节、颜色域与采样语义另验 |
| 动画 | 原轨道、默认骨姿态、帧数/时长/终点、插值、RichCurve 值和切线 | 时间不坍缩，未动画骨沿用 ref，面部保留负值/超范围，末帧可达 |
| Montage/Notify | section/next section、blend、slot、notify、socket、武器/身体 skeleton | 展示动作来源可追溯，切换与挂接行为符合源时间线 |
| 物理/特效 | 图节点、表达式输入、坐标域、单位、solver 参数、VM/生命周期 | 单独验证输入求值、坐标换算和算法；随机初态另行声明 |

DCC 中间步骤容易引入“看起来正确”的数据变化：颜色线性/sRGB 转换、负 scale 处理、只保留最大四权重、骨骼 edit 表达限制、法线归一化和索引重排。每一步用小样本比较原始通道，并把必要修正留在可追踪转换脚本中。

已正式安装并建立 GUID/绑定/索引清单的模型，后续修正可以采用派生运行时 Mesh 或重新生成完整安装计划；选择取决于项目契约。不要混用新导出模型与旧的 primitive/顶点对应表。

### 3.4 将原生默认值与运行时行为接上

静态资产能回答“存了什么”，原生构造和引擎逻辑能回答“缺省时是什么”，运行时图与抓帧能回答“此时实际用了什么”。把这三层分别写入清单，尤其关注 Blend、Alpha、枚举 selector、重力、挂点、Physics/Animation 节点和后处理覆盖标志。

对可隔离数学过程建立原生 fixture：提供输入、源实现/反汇编依据、单位/坐标域、目标输出和误差。先检查默认输入与边界输入，再检查动画驱动输入。此阶段的通过不代替长期历史轨迹验证，详见 [动画与次级运动参考](unreal-animation-and-secondary-motion.md)。

### 3.5 把资源与实际 GPU 使用绑定

对目标 draw/dispatch 保存 Shader 字节、资源/视图/sampler、CB、输入布局、网格/子网格、RT 格式与深度/混合状态。把解析器导出的纹理/模型与实际 GPU 输入对应起来；相同名字、相同缩略图或相同 Resource ID 不足以保证同一事件版本。

单独记录 Shader 语言转换与管线行为：同一像素代码如果输入 mip、颜色域、深度或 Pass 顺序变化，结果仍可能不同。目标编译器和 GPU API 也可能改变半精度行为。用原输入对每个模块定量验证，再进入实时场景比较；详细流程见 [UE 渲染链移植](unreal-render-pipeline-porting.md)。

### 3.6 命令与输出示例的标注方法

工具参数不是通用协议。文档先声明使用哪一个 CLI、项目根与输出根，再给出由该工具源码或帮助确认的参数。以下只是定制导出器的流程示意，不是可执行 CLI；实际参数解析与入口以项目 Profile 引用的工具源码为准：

```text
mount/index → asset-registry query → references closure
→ raw/properties/native mappings verification
→ mesh/texture/reference-skeleton/animation-track export
→ isolated DCC conversion → staging channel audits
→ identity-preserving installation → Unity validation
```

下面是只读核验模板，两个尖括号值由目标项目已存在的目录和报告替换后使用；它不是已执行的验证记录，也不触发导出：

```powershell
$reconstructionRoot = '<absolute project reconstruction root>'
$reportRelativePath = '<existing relative manifest path>'
$reportPath = Join-Path $reconstructionRoot $reportRelativePath
Get-FileHash -Algorithm SHA256 -LiteralPath $reportPath
Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
```

若一个脚本只读资产却重写审计 JSON，将其描述为“只读资产、写报告”；不要简称“完全只读”。导出、构建、安装和启动 Unity 是独立阶段，示例注明其写入位置和需要保留的旧报告，避免读者把审计命令当成无副作用查看器。

## 4. 风险与不适用边界

- 容器全挂载、解析零错误、导出文件非空分别证明不同阶段；它们都不自动证明依赖完整、动作正确或画面一致。
- 某游戏的 mappings、帧率修正、坐标基、native 默认值和 DCC 补丁只能作为其他项目的调查线索。原版本改变后重新验证。
- 存在工具可执行文件只能证明可用候选；正式来源以导出日志、脚本、输入身份与输出 manifest 为证。
- JPG 与 RDC 可能不是同一时刻，RDC 也通常不含捕获前完整物理/随机/temporal history。单帧无法补出完整动态历史。
- 原 mip、面部曲线或 native 代码缺失时，可以记录替代实现和误差，但不把替代结果写成源数据精确恢复。
- 不同 Shader Model、图形 API 和半精度执行可能产生有界数值差；离线编译通过不承担精度验收。
- 本页不覆盖所有 UE 游戏的加密、容器扩展、服务器逻辑或完整玩法复刻；超出已知格式时回到对应项目证据，避免盲目枚举无关资料。

## 5. 验证与回退

以“源身份 → 依赖闭包 → 原始数据 → 转换 → 目标宿主 → 图像/动态”形成报告链，每层保留成功、失败和未验证状态。

| 层 | 结果记录 | 失败时回到 |
| --- | --- | --- |
| 输入/工具 | 原始文件 SHA、工具来源与补丁、native 依赖、命令 | 版本/分支/下载来源；不先调画面参数 |
| 解析 | 挂载状态、字段结构/结束位置、依赖缺口 | mappings、继承、原生默认与脚本输入 |
| 转换 | 原始通道差异、索引对应、bind、曲线/mip | 发生首次差异的 DCC/导出步骤 |
| 安装 | 文件身份、`.meta`/GUID、角色归属与场景引用 | 安装计划和保存的正式资产 |
| 宿主 | 实际 Unity/包 source/API、编译、缺失引用、播放结果 | 隔离 Worker 配置与项目入口 |
| 效果 | 同输入模块、实时缓冲、最终图、动态抽样的独立结论 | 对应层数据；不能用更低层通过掩盖更高层差异 |

回退保存文件与 `.meta`，按同一安装清单成组恢复模型、绑定/索引计划和材质引用；全局包/管线变化使用事前保存的配置。新证据写新前缀或先归档旧报告，报告哈希和完成时点随之更新。

在工具链升级前，用现有小样本做同输入回归，再决定是否批量重建。对稳定生产资产，工具更新本身不是重导理由；有明确缺陷或新依赖时才扩展验证范围。项目最终交付的未验证项和历史警告继续留在 Profile，不因后续一次通过而抹除。
