# Unity 与 DCC 本地材质预览链路参考

> **类型**：REFERENCE；**适用范围**：Unity 与 Substance Painter 等 DCC 之间的本地材质参数、贴图和灯光临时预览；**使用前提**：必须以目标项目的 Shader、贴图通道、DCC API、颜色空间和版本重新验证。本文不定义任何项目的正式资产格式。

## 1. 适用场景

本文用于以下需求：

- 在 Unity 与 DCC 之间双向复制已对齐的材质参数，并在应用前查看差异；
- 把 Unity 已打包贴图发送给 DCC，或把 DCC 独立制作通道临时合并后发送给 Unity；
- 在绘制阶段增量刷新 Unity 预览，降低全量导出带来的延迟和卡顿；
- 把 Unity 当前有效主光临时发送给 DCC，建立更接近运行时的光照 A/B；
- 为非技术美术提供“选择模型 → 一键准备 → 当前/全部同步 → 确认或放弃”的低配置入口，同时保留专业工作区；
- 排查“贴图一致但颜色变暗”“缺图时出现灰色默认图”“插件文件已更新但新命令不生效”等问题。

该链路只服务效果预览和差异定位，不替代 DCC 的分通道制作、正式导出 Preset、Unity 导入设置或运行时材质资产。

## 2. 前提与状态边界

### 2.1 先拆分四个状态域

| 状态域 | 允许同步的内容 | 不应被它修改的内容 |
| --- | --- | --- |
| 材质参数 | 明确白名单内的 Float、Bool、Enum、Color、Vector 和对应 Keyword | 未对齐参数、调试模式、DCC 图层、项目专有运行时状态 |
| 贴图预览 | Unity 已打包贴图或 DCC 临时合并结果 | DCC 正式绘制通道、Unity 正式材质资产和源贴图 |
| 场景/灯光预览 | 主光方向、有效 HDR 光色和强度等少量可表达状态 | Unity 场景灯、Timeline 状态、相机、背景和无法映射的 Renderer 状态 |
| 正式 Authoring | DCC 独立通道、正式导出 Preset、Unity Importer 和材质资源 | 任何仅用于 A/B 的临时缓存或预览材质 |

参数、贴图和灯光不能合成一个不透明的“同步全部”操作。每一类都需要独立命令、独立状态提示和独立恢复路径，否则很难判断差异来自数据传输、Shader 公式还是场景状态。

### 2.2 预览会话必须可恢复

Unity 侧应克隆临时材质并只替换目标 Renderer 的匹配材质槽；Painter 侧应在第一次覆盖自定义 Shader 参数前保存快照。退出预览、窗口关闭、脚本重载、退出编辑器、进入 Play Mode、断线或项目切换时恢复原状态。

恢复操作应当幂等：重复退出不能再次覆盖已经恢复的状态。临时预览不得对正式 Material 调用持久化 `Set*`、标脏或保存；即使 Renderer 的 `sharedMaterials` 临时替换使 Scene 显示 Dirty，也不能擅自清除 Scene Dirty，以免掩盖用户的其他编辑。

### 2.3 持久化是显式提交，不是预览清理的副作用

“退出预览后恢复”和“确认结果后保留”应当是两个不同状态转换。可采用以下顺序：

1. 先恢复参数、辅助纹理、灯光和 Renderer 材质槽的完整快照；
2. 再只把用户明确选择的共享参数作为可 Undo 修改重新应用；
3. 需要保存临时合图时，在退出边界执行一次 GPU 回读/文件导入；
4. 不自动把预览贴图赋给正式 Material，也不自动保存 DCC 工程。

仅关闭“实时刷新”通常只停止后续更新，保留最后一帧临时预览；恢复正式状态应使用独立的“退出预览”。这样可避免一个 Toggle 同时承担暂停、恢复和提交三种互相冲突的语义。

### 2.4 面向美术的流程应显式建模状态

复杂桥接工具不应让使用者通过多个连接按钮、下拉框和内部 JSON 推断当前是否可同步。推荐把简易流程建模为以下可观察状态：

```text
Idle
→ Checking
→ NeedsSetup / NeedsConfirmation
→ Ready
→ Previewing
→ Committing / Restoring
```

UI 只展示当前状态允许的主操作和可行动错误：未连接时由请求自动连接；目标不完整时引导“一键准备/修复”；会修改 DCC 工程结构时集中为一次确认；同步期间禁用冲突操作；结束时必须明确选择 Commit 或 Discard。关闭窗口只能执行非阻塞的 best-effort 恢复，不能被当作隐式提交入口。

日常入口与专业入口应共享同一状态和执行实现，而不是维护两套业务逻辑。简易模式只负责自动发现、收敛配置和隐藏技术参数；专业模式继续提供手动映射、JSON、连接测试和诊断，用于异常处理与高级控制。

## 3. 实现与排查方法

### 3.1 使用本地、版本化、可校验的协议

本地链路建议只监听 `127.0.0.1`，请求至少包含：

```text
schema
version
command
requestId
source
projectPath
payload
target.bindingId
target.textureSet
target.shaderInstanceId
target.adapterId / target.adapterVersion
```

双方应校验协议版本、命令、项目根目录、显式 DCC 目标和响应 `requestId`。当前材质与批量请求都必须携带目标，不能依赖 DCC 面板当前高亮 Texture Set 或下拉框；响应还应回传实际命中的目标供调用端复核。共享文件只允许位于当前项目专用缓存根目录中；读写前使用绝对规范路径，并校验最终路径没有逃逸缓存目录。不要让 DCC 按 Unity 传入的任意路径读写文件。

缓存属于可重建的预览数据，应放在工程临时目录并排除版本控制。缓存分开存放 Unity → DCC、DCC → Unity 和 Unity 内部合图结果，便于清理与定位方向性错误。

### 3.2 参数同步使用显式白名单和类型契约

参数 ID 必须来自两端共同确认的映射表，而不是枚举 Shader 全部参数。每项记录类型、显示名、Unity Keyword 和必要的单位/范围。导入流程先解析并生成差异预览，再由用户决定是否应用；未知参数、类型不符或目标 Shader 不匹配时只报告警告，不静默覆盖。

Bool/Enum 只同步数值还不够。若 Unity Shader 依赖 Keyword，应用参数时必须同步 Keyword 状态；反向导出也要以实际材质值和 Keyword 契约为准。

多 Shader 家族应为每个家族注册独立 Adapter，并把版本与 Profile Schema 分开。JSON 结构不变时 Schema 可保持 v1；参数集合、默认值、Keyword 或贴图语义变化时 Adapter 必须升级。运行时只有 Schema、Adapter ID/Version、Unity Shader 和 DCC Shader 全部匹配才接受同步。

运行时 Bool 不等于 Shader Keyword。映射表应显式记录 Keyword；没有声明 Keyword 的 Bool 只传数值，不能按参数名自动构造一个不存在的宏。

### 3.3 区分“整个贴图组缺失”和“组内通道缺失”

这是预览桥接最容易出错的边界：

- 整个贴图组没有任何来源时，保持目标材质原槽，包括原本为 `None` 的状态；
- 只要组内至少一个通道存在，才生成临时合并贴图；组内缺失通道使用该数据契约的中性值；
- 中性值只用于合图内部，不能被误解为“DCC 有一张默认贴图”；
- Unity → DCC 发送 Unity 最终打包贴图只是预览，DCC 正式制作仍使用 Base Color、Normal、Metallic、Roughness、AO、Emission 等独立通道。

例如仅存在 Opacity 时，Base RGB 可用白色作为合并载体；Base 与 Opacity 都不存在时则不应创建灰色或白色占位图去覆盖原 Base 槽。MRA 的中性值必须由项目通道契约决定，尤其要确认 G 保存的是 Roughness 还是 Smoothness。

还应区分“制作通道”和“Shader 辅助纹理”。Ramp、Noise、MatCap、预卷积 Environment Atlas 等通常是 Unity → DCC 的预览输入，不属于 Base/MRA/Normal/Emission 制作结果。它们应使用独立字段、颜色空间和 Tiling/Offset 契约，不应被 DCC → Unity 导出流程创建、清空或覆盖。

### 3.4 颜色空间在协议边界只转换一次

建议把跨进程参数协议固定为 Linear：

| 数据 | 协议/合图语义 |
| --- | --- |
| Base / Emission RGB | 颜色数据；在进入 Linear Shader 计算前只做一次 sRGB → Linear |
| Normal、Metallic、Roughness/Smoothness、AO、Opacity | Linear 数据，不做 Gamma 转换 |
| Color 参数 RGB | 协议中为 Linear；Unity Inspector 边界负责 sRGB ↔ Linear |
| Color 参数 Alpha | 原样传递 |

Unity `Material.GetColor()` / Inspector 的显示语义和 DCC 面板颜色值不一定处于同一域。若协议声明 Linear，则 Unity → DCC 时在 Unity 边界转成 Linear，DCC → Unity 时在 Unity 边界转回 Inspector/Material 所需的 sRGB 值。典型故障是把 Linear `0.214` 当作 sRGB `0.214` 写进 Unity，最终显示约 `0.04`，画面明显变暗。

DCC 导出的 PNG 扩展名也不能决定采样域。若像素代表 Linear 计算结果，Unity 上传和 GPU 合图必须保持 Linear；Base/Emission 不能在 CPU 和 GPU 各做一次转换。Mipmap 也应在 Linear 数据域生成。

### 3.5 实时刷新以 Dirty 通道映射到合图组

Painter 等 DCC 通常只能在 Layer Stack 计算完成后可靠导出，因此“实时”应定义为停笔后的低延迟增量刷新，而不是逐笔刷采样点或逐帧同步。

推荐流程：

1. Python/宿主事件层记录 Texture Set、通道和修订号；
2. 防抖后读取稳定 Dirty 快照；
3. 将变化通道映射到 Base、Normal、MRA、Emission 等合图组；
4. 只导出命中的通道，保留其他源通道缓存；
5. Unity 只重建命中的 `RenderTexture` 和 Mipmap；
6. 首次同步、Texture Set/Shader Instance/分辨率变化、通道拓扑变化、未知通道或 Dirty 状态不可读时安全回退全量同步。

GPU 合图能减少 CPU 像素循环和部分内存复制，但不能消除 DCC PNG 编码、磁盘传输、Unity 文件读取、PNG 解码和纹理上传。性能状态应分阶段报告耗时；GPU 指标若只统计 CPU 命令提交时间，必须明确它不代表异步 GPU 已执行完成。

连续绘制优先降低实时分辨率、设置合理防抖和有限 Diffusion Padding；最终 A/B 再使用原生分辨率与完整 Padding。双向都自动实时容易形成回环，通常只开放 DCC → Unity 实时，保留 Unity → DCC 手动按钮。

### 3.6 灯光同步必须发送“最终有效输入”

只读取场景 Directional Light 不一定等于角色 Shader 实际使用的灯光。发送前应按运行时顺序解析：场景主光、全局角色覆盖、材质局部覆盖，以及相机跟随等有效方向逻辑。若某状态来自 Timeline、RendererFeature 或 `MaterialPropertyBlock` 且当前工具没有读取能力，应明确标记为未覆盖，不能伪装成已同步。

光色建议先在线性域乘入 Color Temperature 与 Intensity，形成 HDR RGB，再拆为：

```text
strength = max(hdr.r, hdr.g, hdr.b)
color = strength > epsilon ? hdr / strength : 0
```

这样 DCC 的颜色控件保存归一化色相，独立强度保存 HDR 能量。方向协议必须书面约定世界轴、方位角零点/正方向和仰角；发送前后用 `+Z/+X/-Z/-X/+Y/-Y` 六个基准方向验证。

灯光同步默认只做 Unity → DCC 的手动临时预览。反向写场景灯、逐帧跟随或自动覆盖 DCC 环境属于更高风险能力，应在有明确状态所有权和防回环机制后单独设计。

### 3.7 区分插件源码、已安装文件和当前运行时

DCC 插件至少存在三层版本：

1. Unity 工程内的插件源码；
2. 用户插件目录中的已安装副本；
3. 当前 DCC 进程已经加载到内存的 QML/JS/Python 实例。

文件时间或 `plugin.json` 已更新，只能证明前两层。QML、JavaScript、Python、命令服务器和事件订阅通常不会因为覆盖文件而完整热重载；新增命令返回“不支持的 command”时，先核对运行时插件版本并完整重启 DCC，而不是继续修改协议发送端。安装器应复制所有插件层，提示保存工程后完整重启，并提供打开安装目录和当前连接版本信息。

自定义 Shader Resource 还有第四层缓存：DCC 当前工程/Shader Instance 可能继续引用旧 Shader。插件重启后仍缺少新参数或贴图槽时，应重新选择已安装的 Shader，并用握手同时显示插件版本与 Adapter 版本。

### 3.8 多材质同步使用稳定身份，不依赖当前高亮项

多材质绑定至少包含 Unity Material GUID、DCC Texture Set 精确名称、Shader Instance ID 和 Adapter ID/Version。名称只用于生成候选：允许完整同名，以及可证明唯一的受控前缀别名（例如 `mat_` ↔ `tex_`）；不得用包含、编辑距离或排序位置做模糊猜测。执行时应使用稳定 ID 并预检：

- 不允许同一个 Unity Material 或 Texture Set 重复绑定；
- 不允许多个 Texture Set 共用同一个 Shader Instance；
- 不允许未保存资产、重复输出目录或 Adapter 不兼容；
- 当前材质、批量和实时更新都必须携带 `bindingId` 与完整目标，按 Dirty Texture Set 路由到对应临时 Material，不能依赖面板当前高亮项。

如果 DCC 默认让多个 Texture Set 共享同一 Shader Settings 实例，参数和外部预览纹理天然会互相覆盖。工具应先阻断同步，再在一次说明影响的确认后，调用 DCC 能力批量创建或修复独立 Shader Instance；修复后重新拉取 inventory 并全量预检。没有安全修复能力时保持拒绝，不能依赖同步顺序产生“最后一项正确”的偶然结果。

逐个同步不同 Unity Material 时，DCC 侧快照和临时状态也必须按 Shader Instance ID 隔离。准备第二个材质不得删除第一个材质的映射；保存时按 Material GUID 合并更新同一工程记录。这样当前材质切换只改变本次请求目标，不会把前一材质的共享参数替换成最新材质的值。

### 3.9 工程身份缓存只用于恢复候选，每次发送仍需重验证

DCC API 若不能安全提供工程文件路径，可基于排序后的 Texture Set 名称与兼容 Shader Instance inventory 生成不含绝对路径的稳定指纹。缓存键至少包含工程身份、身份类型、inventory 签名和 Unity Material GUID；具体映射还应记录 Texture Set、实例 ID/Label、Shader/URL 与 Adapter 版本。

指纹不是永久可信主键，只用于从本机缓存恢复候选。每次准备或发送前仍需重新验证：

1. 当前工程身份和 inventory 签名仍匹配；
2. Texture Set 存在且没有重复绑定；
3. Shader Instance ID、Label、Shader 和 URL 仍匹配；
4. Adapter ID/Version 兼容；
5. 每个已启用 Texture Set 使用独立 Shader Instance。

任一条件变化时将记录视为过期并回到一键准备，不得静默套用旧 ID。映射应放在可重建、本机私有且不进版本控制的缓存中；JSON 损坏时先改名备份为 `.corrupt-*`，写入使用同目录临时文件加原子替换，避免半写文件破坏全部工程映射。

### 3.10 用质量预设隐藏技术参数，但保留专业控制

简易模式只向美术暴露“流畅 / 标准 / 最终检查”等意图级预设，由预设统一映射实时/手动分辨率、防抖和 Padding。不要让首次使用者先理解 Native、Dirty、防抖毫秒数或 Diffusion Padding 才能开始同步。

预设不能删除底层专业配置：高级工作区仍应显示实际参数并用于诊断；切换工作区不能清空配置、绑定或预览会话。简易与专业入口必须调用同一执行函数，确保自动连接、目标校验、恢复和提交语义一致。

## 4. 常见问题与处理

| 现象 | 常见根因 | 处理 |
| --- | --- | --- |
| DCC → Unity 后整体更暗，贴图本身看似一致 | Color 参数把 Linear 数值当作 Unity sRGB 值写入 | 固定 Linear 协议，在 Unity 导入边界执行 Linear → sRGB，Alpha 不变 |
| Unity → DCC 正常，反向才偏暗 | 两个方向的颜色转换不对称 | 分别用 `0.18/0.5/0.8` 灰阶和纯 RGB 验证双向往返 |
| DCC 没有 Base 却给 Unity 贴了灰图 | 把缺失贴图组错误地替换成默认纹理 | 整组缺失时保持原槽；仅组内缺通道时使用中性值 |
| 只有 Opacity 时 Base 显示灰色 | 合图 RGB 使用了灰色占位 | 只为合图使用白色 RGB；Base/Opacity 都无来源时不覆盖 |
| 只画 Roughness 仍导出全部贴图 | 没有 Dirty 通道到合图组映射，或上下文变化触发全量 | 检查 Dirty Companion、会话 ID、通道拓扑和分辨率配置 |
| GPU 合图后仍有明显延迟 | 时间主要在 DCC 导出、PNG 编码/读取/解码/上传 | 查看分阶段耗时；降低实时分辨率/Padding，保留原生手动检查 |
| 参数 JSON 同步后 Toggle 行为不变 | 数值已写入但 Unity Keyword 未同步 | 在白名单映射中绑定并同步对应 Keyword |
| 新增 Bool 后出现不存在的 Shader Keyword | 按参数名自动推导 Keyword，实际 Shader 使用运行时分支 | 只有映射表显式声明时才切 Keyword；其他 Bool 只同步数值 |
| 退出预览后材质没有恢复 | 首次覆盖前未快照，或异常路径未调用统一退出 | 快照 Renderer 槽和 DCC Shader 参数；所有生命周期汇入幂等恢复 |
| 退出时选择保留参数但辅助贴图也永久留下 | 把预览快照与正式参数提交混成一次覆盖 | 先完整恢复，再只重放白名单参数；辅助纹理和灯光始终恢复 |
| 新增命令仍提示不支持 | DCC 仍运行旧的 QML/JS/Python 实例 | 核对源码/安装/运行时三层版本并完整重启 DCC |
| DCC 默认所有 Texture Set 都显示同一个 ID | 多个 Texture Set 共享同一个 Shader Instance | 阻断同步；一次确认后批量创建/修复独立实例，再刷新 inventory 并重验证 |
| 多材质同步后贴图落到另一个材质 | 依赖当前高亮 Texture Set、模糊名称或共用 Shader Instance | 使用四元绑定、显式 `target` 与 `bindingId`；冲突目标先修复或拒绝 |
| 逐个同步第二个材质后，第一个材质参数也变了 | 快照按当前选中项或共享实例保存，请求没有显式目标 | 快照按 Shader Instance ID 隔离；每次请求携带目标；映射按 Material GUID 合并保存 |
| 重新打开 DCC 工程后错误复用旧映射 | 把缓存 ID 当作永久事实，未检查 inventory 变化 | 工程指纹只恢复候选；发送前重验 Texture Set、实例、Shader/URL 和 Adapter |
| 关闭窗口后意外保留了参数 | 把生命周期清理当作 Commit | 关闭只做 best-effort Discard；只有显式“确认并结束”允许保留共享参数 |
| DCC 中缺少新参数/纹理槽，但插件版本正确 | 当前工程仍持有旧自定义 Shader Resource | 重新选择已安装 Shader，并校验 Adapter 版本 |
| 灯光参数相同但高光方向仍不同 | 只同步场景灯，未解析全局/材质覆盖，或坐标约定不同 | 同步最终有效灯光，并验证六个轴向和 `N/L/V/H` Debug |
| 实时模式频繁卡顿或回环 | 防抖过低、双向自动刷新、每次强制全量 | 只开放一个实时方向；按 Dirty 增量，异常时才全量 |

## 5. 验证与回退

### 5.1 最小验证矩阵

| 层级 | 用例 | 通过条件 |
| --- | --- | --- |
| 协议 | 版本不匹配、未知命令、错误项目路径、缓存越界 | 请求被拒绝且不读写任意路径 |
| 参数 | Float/Bool/Enum/Color/Vector、Keyword、未知参数 | 差异准确；只应用白名单；往返无漂移 |
| Adapter | Schema 相同但 Adapter 版本不同、Shader 名不匹配 | 明确拒绝，不把旧参数集合静默套到新 Shader |
| 美术流程 | 未连接、目标未准备、需要修复、预览中、确认、放弃、直接关窗 | 状态和按钮可行动；只确认一次；关闭不提交；简易/专业模式行为一致 |
| 多材质 | 默认共用实例、重复 Material/Texture Set/Shader Instance、逐个切换同步 | 冲突先修复或拒绝；每个 `bindingId` 只更新自己的目标；前一材质参数不被后一材质覆盖 |
| 映射恢复 | 同工程逐个增加材质、重开工程、inventory/Adapter 变化、缓存损坏 | GUID 记录合并；安全候选可恢复；过期记录失效；损坏缓存有备份且可重建 |
| 颜色 | 灰阶、纯 RGB、HDR Color、Alpha | 两个方向各只转换一次，结果不偏暗/偏亮 |
| 缺失通道 | 整组缺失、单通道存在、组内部分缺失 | 原槽保持或中性值合图符合书面契约 |
| 辅助纹理 | Ramp/Noise/MatCap/Environment 有/无与 ST 变化 | 只影响预览输入；反向导出不创建或覆盖正式辅助纹理 |
| 贴图 | Base、Normal、MRA、Emission 分别单独变化 | 只刷新受影响组，其他纹理和材质状态不变 |
| 实时 | 首次、连续绘制、切 Texture Set、改分辨率、Dirty 不可读 | 正常增量；上下文变化安全全量；断线自动停止 |
| 灯光 | 六个轴向、Color Temperature、HDR 强度、全局/材质覆盖 | DCC 参数与 Unity 最终有效输入一致 |
| 恢复 | 手动退出、窗口关闭、脚本重载、进入 Play、断线 | 两端恢复预览前状态，重复退出无副作用 |
| 显式提交 | 保留参数、保存合图、取消提交 | 只提交选中类别；可 Undo/可定位；不自动赋正式材质 |
| 性能 | 512/1K/2K/Native、不同 Padding | 阶段耗时可解释；实时配置不改变最终手动检查契约 |
| DCC 生命周期 | 覆盖磁盘插件、Reload、完整重启、重开工程、重新选择 Shader | 磁盘版本与握手版本分别可见；真实运行时加载新命令和新资源 |

### 5.2 回退顺序

发生显示差异时先关闭实时刷新，执行一次手动全量同步；再分别退出贴图、灯光和参数预览，回到两端正式材质。颜色问题用纯色/灰阶排除贴图后再查 Color 参数；光照问题先固定相机和主光，再恢复全局/材质覆盖。不要通过修改正式源贴图或材质数值抵消尚未定位的桥接差异。
