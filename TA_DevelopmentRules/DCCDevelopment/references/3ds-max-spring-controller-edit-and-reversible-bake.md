# 3ds Max Spring Controller 动画编辑与可逆 Bake 参考

> 类型：REFERENCE；适用范围：3ds Max 中由 Spring/Jiggle 等历史相关 Controller 驱动 Helper 或骨骼，动画师需要流畅 K 帧并在完成后得到可播放、可回退、可导出的逐帧动画；使用前提：必须以目标 3ds Max 版本、插件、Rig、动画范围和最终消费者重新验证。本文落实 `DCC-STA-01`、`DCC-EVL-01`、`DCC-BKE-*`、`DCC-REV-*`，不替代 [DCC CORE](../README_Tech_DCCDevelopmentRules.md)。

## 1. 适用场景

Spring Controller 会随时间和上游骨骼变化重新计算。动画师移动骨骼、修改历史关键帧或 Scrub 时，模拟求值会与交互争用主线程。完整工具不应只有“开/关 Spring”，而应明确以下状态：

| 状态 | 用途 | Spring 求值 | 结果精度 | Bake 数据 |
| --- | --- | --- | --- | --- |
| 快速编辑 | 日常 K 帧，保留近似反馈 | 仍求值，只回退有限帧 | 近似 | 保留 |
| 无模拟编辑 | 最流畅地编辑原动画 | 从直接 Position 链断开 | 不显示实时次级运动 | 保留 |
| 精确预览 | 审核真实 Spring 结果 | 从起始/Warm-up 帧顺序求值 | 精确 | 保留 |
| Baked | 播放、交付和导出 | 最终输出不再依赖 Spring | 采样结果 | 使用 |

Live/Baked 切换只改变当前输出，不删除 Bake；Unbake 才移除工具生成的数据并恢复原 Controller。以下情况不能直接套用单一实现：

- Spring 嵌套在 List、Constraint、Biped/CAT 锁定轨道或第三方复合 Controller 内；
- 存在动态 Scale、非均匀/负缩放、Shear、镜像骨轴或动态父级；
- 模拟依赖碰撞、回调、缓存文件或专用 Reset/Update API；
- 最终消费者要求世界空间、Root Motion、特殊轴向/单位或独立导出骨架。

## 2. 前提与依赖

### 2.1 先确认控制图和最终消费者

至少检查：

1. Spring 是否直接挂在 `node.position.controller`；
2. Spring Helper 与最终蒙皮/导出骨是否为不同节点；
3. 最终骨骼的 Position、Rotation、Scale、Constraint、Parent 和 List 结构；
4. 动画起止帧、采样步长和必要 Warm-up；
5. 插件/Controller 是否能在目标 Max 中恢复；
6. FBX/引擎实际消费哪些节点和通道。

`classOf`、`getPropNames`、显示名或 Mass/Drag/Tension 等属性只能形成候选。接管前仍要验证 Controller 位于预期轨道；未知结构应阻断并报告。

### 2.2 Quick Edit 是近似优化，不是关闭模拟

`Autodesk.Max.IInterface8` 提供 `SpringQuickEditMode` 与 `SpringRollingStart`。Quick Edit 只缩短失效后的回算窗口，仍会执行 Spring，不能作为最终 Bake 精度来源。修改前应保存旧值，退出、取消、失败和 Bake 完成后按状态恢复。

```maxscript
dotNet.loadAssembly ((getDir #maxRoot) + "\\Autodesk.Max.dll")
local gi = (dotNetClass "Autodesk.Max.GlobalInterface").Instance
local core8 = gi.COREInterface8
local oldQuick = core8.SpringQuickEditMode
local oldRolling = core8.SpringRollingStart

core8.SpringRollingStart = 10
core8.SpringQuickEditMode = true
```

若 API 不可用，工具应禁用依赖它的操作，而不是静默退化成未知结果。

### 2.3 MAXScript 时间必须使用 `Time`，不能把 ticks Integer 再传回时间 API

这是动画脚本中容易被日志掩盖的高风险问题。Autodesk 的 [Time Values](https://help.autodesk.com/cloudhelp/2024/ENU/MAXScript-Help/files/MAXScript-Language-Reference/Values/Time-Data-Values/GUID-51429B01-2FC6-4746-9E88-5EB5D93056CC.html) 明确规定：

- 普通 Integer/Float 在时间上下文中始终解释为“帧数”；
- `timeValue as integer` 返回 ticks；
- `ticksPerFrame` 是 ticks 与帧之间的换算量，不是构造帧时间的必要乘数。

错误写法：

```maxscript
local evaluationTime = frame * ticksPerFrame -- Integer，例如第 1 帧得到 160
sliderTime = evaluationTime                 -- 又被解释为第 160 帧
addNewKey controller evaluationTime         -- Key 也落到错误时间
```

正确写法：

```maxscript
local evaluationTime = 1f * frame -- 结果是 Time，例如 1f
sliderTime = evaluationTime
addNewKey controller evaluationTime
```

如果输入本来就是 ticks，必须显式构造 ticks 时间值或转换为帧后再生成 `Time`；不要依赖隐式转换。诊断报告应打印：

```text
采样时间范围：0f -> 50f | Time
```

同时检查连续帧是否异常地完全相同。若第 1、2、3 帧的复杂动态矩阵完全一致，应优先排查时间类型、范围外钳制和 Key 时间，而不是先归因于 Spring 或插值。

官方参考：

- [MAXScript Time Values](https://help.autodesk.com/cloudhelp/2024/ENU/MAXScript-Help/files/MAXScript-Language-Reference/Values/Time-Data-Values/GUID-51429B01-2FC6-4746-9E88-5EB5D93056CC.html)
- [Controller Key Functions](https://help.autodesk.com/cloudhelp/2024/ENU/MAXScript-Help/files/3ds-Max-Objects-and-Interfaces/Animation-Controllers/Controller-Common-Properties/GUID-B1700B1D-B1EA-4A6C-B4A3-A29DB26C8C02.html)

### 2.4 单 `.ms` 与安装包是两种交付模式

单团队工具、快速迭代且用户要求拖入即用时，优先单一 `.ms`：重复拖入先关闭旧 Rollout；持久状态写入场景 Custom Attribute；不创建 `.mcr`、启动脚本或用户目录副本。只有菜单注册、统一部署、自动更新或长期服务确有价值时才增加安装层。

## 3. 实现方式

### 3.1 先写 State/Transition，再写按钮

| 状态 | Spring | 原 Controller | Baked Controller | Quick Edit | 用户动作 |
| --- | --- | --- | --- | --- | --- |
| 快速编辑 | 恢复 | 使用 | 保留但不输出 | 开 | 继续 K 帧 |
| 无模拟编辑 | 静态 Bypass | 使用 | 保留但不输出 | 无直接 Spring 可算 | 编辑原动画 |
| 精确预览 | 恢复 | 使用 | 保留但不输出 | 关 | 从起点完整求解 |
| Baked | 静态 Bypass | 保存引用 | 使用 | 恢复用户偏好 | 播放/导出 |

每次转换先对完整目标集合预检，再统一修改。按钮应显示“正在处理”和最终当前状态，不能只靠底部状态栏让用户猜测是否点击成功。

### 3.2 无模拟编辑必须真正断开求值链

将 Mass、Tension、Effect 或 Iterations 设为 0，或把下游权重设为 0，都不能证明 Spring 不再被依赖图访问。直接 Position Spring 的稳定 Bypass 流程：

1. 在明确的 Rest/起始帧读取 Helper 的 Parent Local Position；
2. 创建静态 `Position_XYZ`；
3. 用节点 Custom Attribute 的 `#maxObject` 保存原 Spring 与 Bypass 引用；
4. 全量预检通过后替换 `node.position.controller`；
5. 恢复前确认当前 Controller 仍等于工具保存的 Bypass。

若其他工具或动画师已替换当前 Controller，停止自动恢复，不能覆盖外部修改。

### 3.3 精确预览从同一起始状态逐帧求值

```maxscript
for frame = startFrame to targetFrame do
(
    sliderTime = 1f * frame
    for node in evaluationNodes do local evaluatedTM = node.transform
)
```

正式预览/Bake 必须关闭 Quick Edit，并从起始帧或 Warm-up 帧按顺序推进。即使输出 `sampleStep > 1`，历史相关模拟也应逐帧求值，只减少缓存/写 Key 的频率。

### 3.4 Bake 分成采样、健康检查、写入和验证

#### 阶段 A：实时顺序采样

1. 恢复 Spring 和原 Controller；
2. 关闭 Quick Edit；
3. 从起始帧逐帧求值；
4. 缓存最终骨骼 World TM、实时 Parent World TM 和显式 `Time`；
5. 计算目标 Parent Local TM；
6. 缓存可写入目标 Controller 的 Position/Rotation/Scale 语义值；
7. 此阶段不修改任何 Bake Controller。

优先缓存 World TM，因为它是用户真正看到的最终结果；Parent Local 数据用于写入，但最终正确性仍由 World A/B 判断。

#### 阶段 B：采样健康检查

检查每个节点/帧：

- 采样数量是否完整；
- Position/Rotation/Scale 是否有限值；
- 矩阵是否零轴、不可逆或包含 NaN/无穷大；
- 是否镜像、非均匀缩放或含 Shear；
- Scale 是否随动画变化；
- 时间数组是否为 `Time`，范围与帧数是否正确。

阻断项不得进入写轨道阶段。风险项可以警告，但必须依靠最终逐帧验证确认。

#### 阶段 C：事务写入

1. 保存原 Position/Rotation/Scale Controller 引用；
2. 创建新的直接 Bake Controller 或项目明确采用的 List 层；
3. 为所有采样时间显式 `addNewKey`；
4. 在对应 `at time` 上写 Controller 语义值；
5. 所有节点写完后才提交状态；
6. 失败时恢复原引用或上一次有效 Bake。

`addNewKey` 创建的新 Key 初值是该时刻的插值值。不能只假设第一次 `.value` 赋值一定创建了起始 Key。

#### 阶段 D：结果验证与往返验证

切到 Baked 后逐帧比较缓存 Live 与实际 Baked：

- 世界位置误差；
- 世界矩阵三条 Basis 行误差；
- 旋转参考误差；
- Parent Local Scale 误差；
- Baked → Live → Baked 往返后的同一结果。

任何阻断误差都应回滚本次无效轨道。失败报告至少包含节点、帧、Parent、Expected/Actual World TM、Local TM、Controller 类型、最大误差及报告路径。

### 3.5 Position、Rotation、Scale 不能共用一个“通用 value 写法”

3ds Max 中以下三层语义不应混为一谈：

1. `node.rotation` / `node.scale`；
2. `node.rotation.controller.value` / `node.scale.controller.value`；
3. `MAXKey.value`。

在镜像骨骼和 PRS Controller 上，它们可能对应不同的矩阵方向、四元数逆、ScaleValue 轴向或有效值换算。已观察到的典型信号包括：

- Expected Local TM 与 Actual Local TM 基本互为转置；
- 写入的 Rotation 与 Controller 读取值看似相同，但节点 World TM 不一致；
- Rotation Key `.value` 写入后 Controller 读回得到逆四元数；
- Scale 期望值与读回值不同，但位置仍正确。

更稳妥的实现是给 Position、Rotation、Scale 分别建立适配器：

- 用一个未挂父级的隐藏普通 PRS Point 分解目标 Local TM；
- Position 读取能与目标 Position Controller 对齐的值；
- Rotation 优先读取 Solver 的 `rotation.controller.value`，并用目标 `Linear_Rotation.value` 写入/读回验证；
- Scale 同时保留 `scalepart` 与 `scale.controller.value`，用目标 `Bezier_Scale.value` 写入/读回验证；
- 只有 A/B 证明 Key 语义一致时才直接写 `MAXKey.value`。

不要仅靠“数值看起来接近”接受结果，必须比较最终 World Basis。

### 3.6 动态 Scale、镜像和 Shear 决定 Bake 边界

非均匀 Scale 本身不是一律禁止，但必须区分：

- 静态非均匀 Scale；
- 随 Spring/父级 K 动画变化的 Scale；
- 旋转与非均匀 Scale 组合形成的轴向耦合/Shear；
- 负缩放/镜像导致的 handedness 变化。

如果原 `Scale_Expression` 的实时输出会变化，保留原 Scale Controller 会丢失 Live 结果；必须逐帧 Bake Scale。标准 PRS 无法表达动态 Shear 时，应由最终 Basis 验证阻断，而不是放宽容差掩盖。

`matrix3.rotationpart` 在镜像/非均匀 Scale 下不一定能唯一恢复期望骨轴；隐藏 PRS Solver 和最终 World 验证比直接 `rotationpart/scalepart` 更可靠。

### 3.7 直接 Controller 切换与 List Controller 的选择

两种存储方式都必须可逆：

| 方式 | 优点 | 主要风险 | 适用条件 |
| --- | --- | --- | --- |
| Position/Rotation/Scale List | 在 Track View 中直观看到 Original/Baked Slot | List 权重、活动层和复合变换可能改变镜像/Scale 组合语义 | 简单 PR、已通过真实 Rig A/B |
| 直接 Original/Baked Controller 引用切换 | 避开 List 混合；三通道语义清晰 | 必须持久化六个引用并严查部分外部替换 | 镜像、动态 Scale 或 List 已产生偏差的 Rig |

直接切换结构：

```text
Per-node persistent data
  Original Position / Rotation / Scale
  Baked Position / Rotation / Scale
  Has Bake / range / step
```

切到 Live 恢复 Original 三条引用；切到 Baked 使用 Baked 三条引用并复用已经验证过的静态 Spring Bypass。Unbake 才清除 Bake 引用。旧 List 数据可保留只读识别与安全 Unbake，但升级到直接模式前应先 Unbake，避免混合两套存储语义。

### 3.8 兼容 Bake 与严格 Bake

可提供两种验证策略，但不能把“兼容”理解为跳过关键安全检查：

- **兼容 Bake**：写完整 Position/Rotation/Scale，世界 Position 与完整 Basis 参与阻断；镜像矩阵的 `rotationpart` 可仅作参考。
- **严格 Bake**：在兼容验证基础上，Rotation 与 Parent Local Scale 也参与阻断。

两种模式都应执行预检、逐帧写入、世界 A/B、失败回滚和 Live/Baked 往返。严格模式失败不代表资源必然不可用，但必须由报告判断是表示差异、Rig 风险还是实际形变差异。

### 3.9 自动预检与错误分级

点击 Bake 后先运行预检：

- **通过**：配置、范围、API、Controller 引用满足前提；
- **警告**：非均匀缩放、镜像、Shear 候选、层级关系或 Warm-up 风险，可继续但必须最终验证；
- **阻断**：目标为空、范围非法、引用损坏、外部部分替换、矩阵无效、零轴或不可逆。

错误信息应说明“失败阶段 + 节点/帧 + 原因 + 回滚结果”，不能只显示 Runtime Error。诊断报告不能被后续一次普通预检覆盖；应保留最近一次详细验证。

### 3.10 MAXScript 与 UI 实现经验

- 顶层不能声明不允许的 `local`；单脚本尽量把局部变量放入函数/Struct/Rollout。
- `catch` 内重抛使用 `throw()`；业务错误在非 catch 路径抛出。
- Struct 成员调用可能受声明顺序影响；被调用成员放在调用方之前。
- Spinner 长标题用独立 Label；需要字号的按钮局部使用 WinForms。
- `dotNetControl` Rollout 事件按目标 Max 示例使用单事件参数。
- 长操作用 `busy`、Progress、取消和 `try/catch` 集中恢复。
- 点击后按钮先显示橙色“正在…”，成功状态保持蓝色“✓ 当前”。

## 4. 常见问题、根因与解决方式

| 现象/错误 | 根因 | 解决方式 | 预防检查 |
| --- | --- | --- | --- |
| `no local declarations at top level` | 顶层使用非法 `local` | 改为单 `.ms` 或移入函数/Struct | 静态搜索顶层声明，真实 Max 加载 |
| `only throws without arguments...` | `catch` 内带参数 `throw` | 重抛统一 `throw()` | 审计全部 catch |
| `Call needs function or class, got: undefined` | Struct 成员顺序或宿主类未加载 | 调整声明顺序并增加入口上下文 | 按成员依赖顺序审计 |
| Quick Edit 仍卡 | 它仍执行 Spring | 增加真正 Controller Bypass | 普通/Quick/Bypass 同动作计时 |
| Baked 后仍算 Spring | 只切权重，未断上游链 | Baked 状态复用静态 Bypass | 检查 Helper 当前 Controller |
| Bake 到 Helper 后引擎没动画 | Helper 不是最终消费者 | Bake 最终蒙皮/导出骨 | FBX → 引擎验证 |
| Live/Baked 往返后画面丢失 | 重新采样覆盖静态 Bypass 基准 | 复用 Bake 成功时同一套 Bypass 引用 | 自动 Baked → Live → Baked 验证 |
| 无 K 动画正常，K 后失败 | 父级动画暴露矩阵、Scale、时间或求值顺序问题 | 逐帧报告 Parent/Local/World，检查时间类型和动态 Scale | 必测父级 K 动画用例 |
| 第 1、2、3 帧结果完全相同 | ticks Integer 被时间 API 再当帧解释，或范围外钳制 | 全部时间使用 `1f * frame` | 报告 Time 类型与首尾范围 |
| Expected/Actual Local TM 互为转置 | 节点 Rotation 与 Controller Rotation 语义混用 | 采样/写入同一 Controller 语义 | 报告 Written/Actual Controller Rotation |
| Rotation Key 写入后变成逆 | `MAXKey.value` 与 Controller 有效值语义不同 | 为 Rotation 使用验证过的 Controller 写入适配器 | 写后立即读回并做 World A/B |
| 第 0 帧被后一帧值覆盖 | 空 Controller 未显式建立起始 Key | 每个采样时间先 `addNewKey` | 检查 Key 数与 Key 时间 |
| Scale Expression 断链后回到静态值 | 实时 Scale 会随 Spring/父级动画变化 | 采样并逐帧 Bake Scale | 报告首帧与最大 Scale 变化 |
| `bone_004` 位置误差放大 | 父 `bone_003` Basis/Scale 有小误差 | 先修父节点表示，再看子级 | 按父到子定位首个超限节点 |
| 非均匀 Scale 警告很多 | 风险提示与实际失败混在一起 | 警告可继续，但由 Basis/Scale 验证决定 | 不因警告直接判死，也不忽略验证 |
| Bake 失败后仍能点 Baked | UI 未区分失败状态或残留引用 | 回滚新轨道并将 `HasBake=false` | 诊断 Storage/Key 数/HasBake |

## 5. 风险与不适用边界

- 直接 `Position_XYZ` Bypass 不能透明处理嵌套 List、Constraint 或锁定 Controller。
- 读取 `node.transform` 通常会触发依赖求值，但第三方模拟可能要求专用 API。
- Step `1` 最可靠但 Key 多；Step 大于 `1` 必须验证高频运动插值。
- Spring 起点和 Warm-up 必须由动作事实决定，不能长期依赖默认范围。
- 标准 PRS 无法精确表达所有动态 Shear；验证失败时不要盲目增大容差。
- DCC 内 Baked 流畅不等于 FBX/Unity 正确；轴向、单位、骨骼过滤和 Importer 是独立层。
- 未在目标 Max、真实 Rig 和最终引擎验证前，只能声明静态检查通过。

## 6. 验证与回退

### 6.1 最小验证矩阵

| 层级 | 用例 | 通过条件 |
| --- | --- | --- |
| 脚本加载 | 首次/重复拖入、关闭后重开 | 无编译错误，版本正确，旧窗口被替换 |
| 时间 | `0..N`、非零起点、Step 1/2、结束帧非整除 | 报告 `Time`；Key 落在真实帧；结束帧存在 |
| 扫描 | 直接 Spring、非 Spring、Bypass、特殊命名 | Helper/Bake 无交集，空结果可行动 |
| 快速/无模拟 | 拖骨、改历史 Key、Scrub | Quick 更快；Bypass 不再直接求值 Spring |
| 精确预览 | 起点到当前帧、Warm-up、取消 | 顺序求值，状态恢复完整 |
| Bake | Position/Rotation/Scale、父级 K 动画 | Key 数/时间正确，World Position/Basis 通过 |
| 镜像/Scale | 负轴、非均匀、动态 Scale、Shear 候选 | 可表达项通过；不可表达项明确阻断 |
| 往返 | Baked → Live → Baked、多次切换 | Bypass、Controller 引用和画面一致 |
| 重复 Bake | 同输入连续两次 | 逐帧一致；失败不破坏旧结果 |
| Unbake | 正常、引用缺失、外部替换 | 正常恢复；损坏状态阻断不覆盖 |
| 持久化 | 保存、关闭、重开 | 状态和引用仍可识别与恢复 |
| 导出 | FBX → 最终引擎 | 最终蒙皮骨、帧范围、轴向和单位一致 |

### 6.2 诊断报告最小字段

- 工具/场景/Max 版本；
- 当前模式、Bake 模式、Quick Edit API；
- Spring/Bake 节点 handle、Parent、Controller、Storage、Key 数；
- 预检通过/警告/阻断；
- 采样时间首尾、类型、帧数；
- 最大 Position/Rotation/Basis/Local Scale 误差；
- 首批超限节点的 Expected/Actual World/Local TM；
- Written/Actual Controller PRS；
- 失败阶段、回滚结果和报告路径。

### 6.3 回退策略

- 操作前另存场景副本；工具可逆性不能替代备份。
- 采样失败不创建轨道；写入/验证失败恢复 Live 和原 Controller。
- 重复 Bake 只在新结果完整通过后替换旧结果。
- 检测到外部 Controller 修改时停止自动恢复。
- 关闭窗口、取消和异常都恢复时间、选择、Quick Edit、Auto Key、Progress 和临时节点。
- 无真实 Max/真实 Rig/引擎验证时，交付明确标记待验层级。
