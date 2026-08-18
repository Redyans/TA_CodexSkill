# 3ds Max Spring Controller 动画编辑与可逆 Bake 参考

> 类型：REFERENCE；适用范围：3ds Max 中由 Spring/Jiggle 等有历史依赖的 Controller 驱动 Helper 或骨骼、动画师需要流畅 K 帧并在完成后得到可导出逐帧动画的工具；使用前提：必须以目标 3ds Max 版本、Spring 插件、Rig 控制器结构、动画范围和导出消费者重新验证。本文实现 `DCC-STA-01`、`DCC-EVL-01`、`DCC-BKE-*`、`DCC-REV-*`，不替代 [DCC CORE](../README_Tech_DCCDevelopmentRules.md)。

## 1. 适用场景

Spring Controller 会在时间或上游骨骼变化时重新计算次级运动。动画师拖动主骨、调整关键帧或跳转时间轴时，实时求值会和交互操作争用主线程，表现为移动骨骼、Scrub 和 K 帧卡顿。

这类需求不宜只做一个“开/关 Spring”按钮。完整工作流通常需要四种结果：

| 状态 | 用途 | Spring 求值 | 结果精度 | 是否保留 Bake |
| --- | --- | --- | --- | --- |
| 快速编辑 | 日常 K 帧，保留近似次级运动反馈 | 仍求值，但只回退有限帧 | 近似 | 保留 |
| 无模拟编辑 | 最流畅的骨骼编辑 | 从直接 Position 链断开 | 不显示实时次级运动 | 保留 |
| 精确预览 | 审核当前帧的真实模拟 | 从起始帧顺序完整求值 | 精确 | 保留 |
| Baked | 播放、交付和导出 | 停止参与最终输出 | 逐帧采样结果 | 使用 |

Live/Baked 切换与 Unbake 含义不同：Live/Baked 只切换当前输出，仍保存 Bake 数据；Unbake 才移除工具创建的外层 Controller List 并恢复原始 Position/Rotation Controller。

以下情况需要先扩展方案，不能直接套用本文：

- Spring 嵌套在 Position List、Constraint、Biped/CAT 锁定轨道或第三方复合控制器内部；
- 需要 Bake Scale、非均匀缩放、Shear、负缩放或动态父级；
- 模拟依赖碰撞、脚本回调、缓存文件或不能仅靠时间推进触发的外部状态；
- 最终消费者需要世界空间、Root Motion、不同轴向/单位或专用导出骨架。

## 2. 前提与依赖

### 2.1 先确认控制图，不要只看“弹簧”参数

可靠的静态/运行时检查至少包括：

1. Spring 是否直接挂在节点 `position.controller`；
2. Spring Helper 和最终蒙皮/导出骨骼是否是不同节点；
3. 最终骨骼是否同时有 Position、Rotation、Constraint、父级或 Scale 动画；
4. 动画范围、起始帧和必要 Warm-up 是否已设置；
5. Spring 插件和 Controller 类是否在目标 3ds Max 中可实例化和恢复；
6. 最终 FBX/引擎消费哪些节点和变换通道。

类名可以用 `classOf controller as string` 和 `getPropNames controller` 诊断。`PositionSpring`、`Point3Spring` 或显示名包含 Spring/Jiggle 只能作为候选；真正接管前仍要检查它位于预期轨道，并对未知/嵌套结构阻断。

### 2.2 Quick Edit 是交互优化，不是关闭模拟

3ds Max `Autodesk.Max.IInterface8` 提供：

- `SpringQuickEditMode`
- `SpringRollingStart`

官方 SDK 说明 Quick Edit 在 Spring 失效时只回退一定帧数重新计算，而不是从起点完整重算，因此可显著提高交互性。它仍会执行 Spring 求值，且结果是编辑近似，不适合作为最终精确 Bake。

MAXScript 可通过 `Autodesk.Max.dll` 访问：

```maxscript
dotNet.loadAssembly ((getDir #maxRoot) + "\\Autodesk.Max.dll")
local gi = (dotNetClass "Autodesk.Max.GlobalInterface").Instance
local core8 = gi.COREInterface8

local oldQuick = core8.SpringQuickEditMode
local oldRolling = core8.SpringRollingStart
core8.SpringRollingStart = 10
core8.SpringQuickEditMode = true
```

修改前保存旧值，窗口关闭、失败和 Bake 完成后按状态恢复。若 `COREInterface8` 或属性不可用，应停止依赖 Quick Edit 的操作并报告版本边界。

### 2.3 直接运行脚本与安装包是两种交付模式

单个团队工具、尚在快速迭代且用户希望“拖入即用”时，单一 `.ms` 通常更可靠：

- 顶层定义全局版本、Custom Attribute、核心 `struct`、Rollout 和 `createDialog`；
- 再次拖入时先 `destroyDialog` 旧 Rollout，避免同时存在两个窗口；
- 场景持久状态写入 Custom Attribute，不依赖安装目录；
- 不创建 `.mcr`、启动脚本或用户目录副本。

只有需要菜单注册、统一部署、自动更新或跨团队版本管理时，才增加安装层；安装层不能成为核心求值和恢复逻辑的前置依赖。

## 3. 实现或排查步骤

### 3.1 先写状态表，再写按钮

推荐把每个状态的副作用冻结为一张表：

| 状态 | Spring Controller | 原始骨骼轨道 | Baked 轨道 | Quick Edit | 用户动作 |
| --- | --- | --- | --- | --- | --- |
| 快速编辑 | 已恢复 | 100% | 0% | 开 | 继续 K 帧 |
| 无模拟编辑 | 替换为静态 Bypass | 100% | 0% | 可保持开，但无直接 Spring 可算 | 最流畅编辑 |
| 精确预览 | 已恢复 | 100% | 0% | 关 | 从起始帧算到当前帧 |
| Baked | 替换为静态 Bypass | 0% | 100% | 恢复用户原偏好 | 播放/导出 |

每次转换先验证所有 Spring 节点和 Bake 节点，再统一修改。不要在某个节点失败后继续切换剩余节点。

### 3.2 Quick Edit：保留反馈的首选编辑模式

进入快速编辑的顺序：

1. 验证目标集合；
2. 若当前为 Bypass，恢复原 Spring Controller；
3. 将最终骨骼切到 Original 轨道；
4. 设置 `SpringRollingStart`；
5. 开启 `SpringQuickEditMode`；
6. 刷新当前时间和视口。

回退帧数越小越流畅，但与完整结果偏差可能越大。它应作为美术可调的性能/反馈折中值，而不是精确度保证。

### 3.3 无模拟编辑：真正从 Position 求值链断开 Spring

只把 Mass、Tension、Effect、Iterations 等参数设为 0，或把下游权重设为 0，都不一定阻止依赖图访问 Spring。稳定的“无模拟”方案是临时替换直接 Position Controller：

1. 在 Rest/起始帧读取 Helper 的 Parent Local Position；
2. 为节点创建静态 `Position_XYZ`；
3. 使用 Custom Attribute 的 `#maxObject` 字段保存原 Spring Controller 和 Bypass Controller 引用；
4. 先写入引用，再把 `node.position.controller` 替换成 Bypass；
5. 标记 `isSpringBypassed`。

场景级状态建议拆为：

```text
Root scene data
  Spring node table
  Bake node table
  Frame range / sample step
  Current mode

Per-node data
  Original Spring Position
  Bypass Position
  Is bypassed
  Original Position / Rotation
  Baked Position / Rotation Lists
  Baked child controllers
  Has bake / range / step
```

恢复前检查：

- 原 Spring 引用仍存在；
- 当前 Position Controller 仍等于工具保存的 Bypass；
- 当前节点没有被删除或替换；
- 整个目标集合都可恢复。

任一节点被外部修改时停止恢复，避免把动画师的新控制器覆盖成旧引用。

### 3.4 精确预览：从起始帧按时间顺序强制求值

有历史依赖的 Spring 不能只把 `sliderTime` 直接跳到目标帧。精确预览流程：

1. 保存当前时间；
2. 恢复全部 Spring；
3. 将最终骨骼切回 Original；
4. 关闭 Quick Edit；
5. 从起始帧逐帧推进到当前帧；
6. 每帧读取最终骨骼或 Spring 节点的 `transform`，强制依赖图求值；
7. 在取消、异常和完成路径关闭进度 UI。

伪代码：

```maxscript
for frame = startFrame to targetFrame do
(
    sliderTime = frame * ticksPerFrame
    for node in evaluationNodes do local evaluatedTM = node.transform
)
```

若 Rig 需要起始帧前的稳定时间，应把 Warm-up 明确加入范围，而不是假设第 0 帧已处于平衡状态。

### 3.5 两阶段 Bake：采样与写键完全分离

#### 阶段 A：顺序求解并缓存

1. 恢复 Spring 和 Original 轨道；
2. 关闭 Quick Edit；
3. 从起始帧按 `sampleStep` 顺序推进，确保结束帧总被采样；
4. 对每根最终骨骼计算 Parent Local TM：

```maxscript
local localTM = if node.parent != undefined then \
    node.transform * (inverse node.parent.transform) \
else \
    node.transform
```

5. 只把 `translationpart`、`rotationpart` 和采样时间存入内存；
6. 取消时直接丢弃缓存，此时场景控制器尚未被修改。

#### 阶段 B：临时写键并提交

1. 为每根骨骼创建临时 `Linear_Position` 和 `TCB_Rotation`；
2. 在 `with animate on` 下写入全部缓存键；
3. 所有节点写键成功后，再把临时 Controller 替换进正式 Baked Slot；
4. 若最终替换中途失败，恢复此前旧 Baked Controller；
5. 只有本次新创建的 Controller List 才在失败时移除。

这种顺序避免两个常见问题：

- 边采样边写键会改变后续 Spring 输入，导致同一次 Bake 自我污染；
- 重新 Bake 直接清空旧轨道，会让一次中途失败破坏上次可用结果。

### 3.6 可逆 Controller List 结构

3ds Max 自带 MassFX Bake 脚本也使用 Position/Rotation List、Boolean Float 权重和活动轨道切换。可复用的结构是：

```text
Position List
 ├─ Linear Position（Baked）
 └─ Original Position

Rotation List
 ├─ TCB Rotation（Baked）
 └─ Original Rotation
```

每个 List 的两个权重都使用 `Boolean_Float`：

```text
Baked：Baked 100 / Original 0 / active 1
Live： Baked   0 / Original 100 / active 2
```

设置权重前检查当前节点 Controller 仍等于工具保存的 List。切到 Baked 后还应 Bypass Spring Helper；仅把 Original 权重设为 0 不能证明上游 Spring 不再被其他依赖访问。

Unbake 的语义是：

1. 恢复 Spring；
2. 全量预检所有 Bake List；
3. 把节点 Position/Rotation Controller 恢复为保存的 Original 引用；
4. 清除工具保存的 Bake 引用和状态；
5. 不修改 Scale Controller。

### 3.7 扫描 Spring Helper 与 Bake 骨骼必须分开

自动扫描建议分成两套规则：

- Spring Helper：运行时检查 `position.controller` 是否是直接 Spring/Jiggle 候选，或是否处于工具保存的 Bypass 状态；
- Bake 骨骼：使用项目 Profile 的明确命名/集合/层级规则，结果仍允许人工覆盖；
- 交集：Spring Helper 和 Bake 目标不能是同一节点；
- 空结果：说明匹配规则，并提供手动选择入口；
- 结果：列表显示节点名，诊断显示 handle、当前/原控制器类型和 Bake 状态。

自动命名匹配只能是项目适配器。通用实现不应硬编码某个角色的 `Breast_*`、`Point*` 或其他节点名。

### 3.8 失败恢复和全局状态

长操作需要集中保存并恢复：

- `sliderTime`；
- `SpringQuickEditMode` 和 `SpringRollingStart`；
- `maxOps.autoKeyDefaultKeyOn`；
- 当前 Live/Baked 权重和活动轨道；
- Spring Bypass 状态；
- 进度条；
- 本次新创建或已提交的 Controller 数量。

操作应使用 `busy` 防止重复点击。错误弹窗加入操作上下文，例如“自动扫描胸部 Bake 骨骼失败”，不要只显示 `getCurrentException()` 的泛化类型错误。

### 3.9 MAXScript UI 的中文与 DPI 处理

原生 Rollout Spinner 的标题可能从输入框位置向左布局，长中文/中英混排会越过 GroupBox。可使用：

- Spinner 标题置空；
- 独立 `.NET Label` 放置中文标题；
- 适当扩大窗口、列宽和按钮高度；
- 需要调字号的按钮改为 `System.Windows.Forms.Button`；
- 使用 `System.Drawing.Font` 设置团队可用中文字体；
- 用结果导向的两段式文字代替 `Quick Edit`、`Detach Spring`、`Full Solve` 等内部术语。

MAXScript 的 `dotNetControl` 事件处理器使用一个事件参数，例如：

```maxscript
on btnBake Click eventArgs do
(
    -- execute bake
)
```

应从目标 3ds Max 随附脚本中核对事件签名，不能直接照搬普通 C# 的 `(sender, eventArgs)`。

## 4. 风险与不适用边界

### 4.1 已遇到的错误与解决方式

| 现象/错误 | 根因 | 解决方式 | 预防检查 |
| --- | --- | --- | --- |
| `Compile error: no local declarations at top level` | 安装脚本在 MAXScript 顶层声明 `local` | 去掉不必要安装器，改为单 `.ms`；或把局部变量放入函数/结构作用域 | 搜索顶层 `local`，在目标 Max 实际加载 |
| `only throws without arguments are permitted in catch expressions` | 在 `catch` 中使用 `throw (getCurrentException())` | `catch` 内重抛只用 `throw()`；自定义 `throw "..."` 放在非 catch 路径 | 搜索所有 `catch` 与 `throw` |
| `Call needs function or class, got: undefined` | `struct` 成员调用的定义顺序导致被调用符号解析为 `undefined`，或函数/类在当前版本未加载 | 把被调用成员放到调用方之前；逐步日志定位；加载/检查宿主程序集与类 | 按声明顺序审计内部调用，错误弹窗带操作上下文 |
| 自动扫描报错但弹窗没有步骤 | 最外层只显示原始异常 | 为每个按钮包装具体操作名，并把详细诊断写入 Listener | 每个公共入口模拟失败一次 |
| `dotNetControl` 按钮事件编译失败 | 使用普通 C# 两参数事件签名 | 使用 Max Rollout 支持的单事件参数处理器 | 对照目标版本自带脚本示例 |
| Spinner 标题向左越界 | 原生 Spinner 的长标题布局空间不足 | Spinner 使用空标题，旁边放独立 Label；扩大窗口/列宽 | 在中文界面和实际 DPI 截图检查 |
| 字体太小且原生按钮无法调字号 | Rollout 原生控件不提供所需字体属性 | 只把需要强调的 Label/Button 换成 `.NET` 控件并设置字体 | 检查主题、点击、禁用态和高 DPI |
| Quick Edit 仍有卡顿 | Quick Edit 只是缩短回算窗口，仍执行 Spring | 增加真正的 Controller Bypass 无模拟模式 | 比较普通、Quick、Bypass 拖骨延迟 |
| Bake 后仍在后台算 Spring | 只切换 Baked 权重，没有断开上游求值链 | 切到 Baked 后 Bypass Spring Helper | 诊断当前 Position Controller，播放时测性能 |
| Bake 结果与 Live 漂移 | 随机跳帧、Quick Edit 未关闭、边采样边写键或起始状态不同 | 从同一起始帧顺序求值；采样/写键分离；正式 Bake 强制关闭 Quick Edit | 重复 Bake、逐帧 A/B |
| Unbake 覆盖了用户后续修改 | 恢复时只相信旧缓存，没有检查当前控制器 | 保存工具临时对象引用；恢复前检查当前引用仍匹配 | 外部替换一个控制器后测试阻断 |
| Bake 到 Helper 后引擎无动画 | Helper 不是最终蒙皮/导出消费者 | 分开配置 Spring Helper 与最终 Bake 骨骼 | 导出后在最终引擎检查蒙皮 |

### 4.2 仍需按 Rig 验证的技术边界

- `Position_XYZ` Bypass 只覆盖直接 Position Spring，不能透明处理嵌套 List、Constraint 或锁定控制器。
- `Linear_Position + TCB_Rotation` 是一种可用轨道组合，不保证适合所有旋转插值、Euler Filter 或游戏导出设置。
- Parent Local Position/Rotation 不包含 Scale/Shear 的完整矩阵信息。
- 逐帧 Step `1` 最稳定但会增加键数；Step 大于 `1` 需要验证插值不会丢失高频次级运动。
- 读取 `node.transform` 能在当前实现中触发求值，但第三方模拟可能要求专用 Reset、Cache 或 Update API。
- Spring 起始帧、场景动画范围和 Warm-up 必须由 Rig/动作事实决定，不能长期依赖默认 `0-100`。
- Baked 状态的流畅不等于导出正确；FBX 轴向、单位、骨骼过滤和 Unity Importer 仍是独立验证层。

## 5. 验证与回退

### 5.1 最小验证矩阵

| 层级 | 用例 | 通过条件 |
| --- | --- | --- |
| 脚本加载 | 首次拖入、重复拖入、关闭窗口后再拖入 | 无编译错误；旧窗口被替换；版本正确 |
| API | Quick Edit 开/关、不同初始 Rolling Start | 属性可读写；退出恢复原值 |
| 扫描 | 直接 Spring、非 Spring、已 Bypass、特殊命名骨骼 | 结果准确；空结果可行动；Helper/Bake 无交集 |
| 快速编辑 | 主骨频繁移动、修改历史关键帧、时间轴 Scrub | 延迟低于完整 Spring；明确为近似结果 |
| 无模拟 | 进入、保存重开、退出、外部替换 Controller | Spring 不再直接求值；可恢复；外部修改被阻断 |
| 精确预览 | 起始帧到当前帧、当前帧早于起始帧、取消 | 顺序求值；非法范围阻断；进度正确清理 |
| Bake | Step 1、结束帧非整除、取消采样、取消写键 | 结束帧有键；采样取消不改控制器；写键失败可回滚 |
| 重复 Bake | 同一输入连续两次 Bake | 逐帧结果一致；旧结果仅在新结果完整后替换 |
| Live/Baked | 往返切换多次 | 权重、Active Slot、Spring Bypass 和画面一致 |
| Unbake | 正常、缺失引用、外部修改 | 正常恢复原轨道；损坏状态阻断且不覆盖 |
| 持久化 | Bypass/Baked 后保存、关闭、重开 | 状态与引用仍可识别并恢复 |
| 变换 | 父级运动、旋转、高频振动、非均匀缩放候选 | Position/Rotation A/B；不支持项明确阻断或记录风险 |
| 性能 | 普通 Spring、Quick Edit、Bypass、Baked | 记录相同动作和帧范围下的交互/播放耗时 |
| 导出 | FBX 导出并进入最终引擎 | 最终蒙皮骨动画、轴向、单位和帧范围一致 |

### 5.2 分层验证顺序

1. 做 UTF-8、括号/字符串、函数声明顺序、事件签名和关键 API Token 检查。
2. 在目标 3ds Max 版本拖入脚本，确认 Rollout、字体、DPI、按钮和 Listener。
3. 用最小测试场景验证单个直接 Spring 的 Quick/Bypass/Restore。
4. 在真实 Rig 核对 Spring Helper、最终 Bake 骨骼、动画范围和父级结构。
5. 运行精确预览和完整 Bake，执行逐帧 Live/Baked A/B 与重复 Bake。
6. 保存重开，验证 Bypass/Baked/Unbake。
7. 导出 FBX，在最终引擎验证蒙皮、变换和帧范围。

### 5.3 回退策略

- 操作前另存场景副本；工具恢复能力不能代替源文件备份和版本控制。
- Quick Edit 或 UI 改造失败时，可回退到原 Spring，不应影响已保存的场景结构。
- Bypass 失败时只恢复本次已替换节点，并恢复时间滑块。
- Bake 采样失败时不创建新轨道；写键或提交失败时保留旧 Bake/Original。
- 检测到外部控制器修改时停止自动恢复，由 TA 在备份场景中人工比对，不强制覆盖。
- 未完成真实 Max、真实场景或最终引擎验证时，交付只能标记对应层级为待验。
