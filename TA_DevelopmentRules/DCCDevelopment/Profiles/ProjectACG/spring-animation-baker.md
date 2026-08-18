---
name: projectacg-3ds-max-spring-animation-baker-profile
description: ProjectACG 3ds Max Spring Animation Baker 的路径、版本、Rig 扫描、四态求值、可逆 Bake、已解决问题和当前验证边界。
---

# ProjectACG 3ds Max Spring Animation Baker Profile

> 类型：PROFILE；适用范围：当前 ProjectACG 的 3ds Max Spring 胸部次级运动工具；通用规则见 [DCC 开发模块](../../README_Tech_DCCDevelopmentRules.md)，实现与排查方法见 [3ds Max Spring Controller 动画编辑与可逆 Bake 参考](../../references/3ds-max-spring-controller-edit-and-reversible-bake.md)。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| 工具版本 | `1.2.2`。 |
| 主脚本 | `D:\work2025U3D\Valkyria\ProjectACG\Client\Tools\3dsMax\SpringAnimationBaker\SpringAnimationBaker.ms`。 |
| 功能 README | `D:\work2025U3D\Valkyria\ProjectACG\Client\Tools\3dsMax\SpringAnimationBaker\README.md`。 |
| 运行方式 | 不安装；将单个 `.ms` 拖入 3ds Max 视口，或使用 `Scripting > Run Script`。 |
| 目标 DCC | 本机 3ds Max 2024.2；通过 `Autodesk.Max.IInterface8` 访问 Spring Quick Edit。 |
| 目标场景 | `G:\mesh_100601_QHigh胸加弹簧骨骼.max`，当前文件存在，大小约 13.18 MiB。 |
| 参考资料 | 工程根目录的 `骨骼绑定.docx`、`视频.mp4`，与动画文档共享目录中的同名文件一致。 |
| Spring 插件证据 | 场景/插件静态信息出现 `spring.dlc`；本机插件二进制字符串出现 `PositionSpring`、`Point3Spring`。运行时仍以节点实际 Controller 为准。 |
| 最终消费者 | ProjectACG 角色胸部蒙皮骨骼；正式验收还需要 FBX → Unity 动画结果。 |

工具目录只保留 `SpringAnimationBaker.ms` 和紧邻 `README.md`。用户已明确不需要安装器或 `.mcr`，后续维护不得默认重新引入安装流程。

## 2. 当前 Rig 与扫描契约

### 2.1 Spring Helper

自动扫描遍历场景节点，只接受 `position.controller` 上的直接 Spring/Jiggle 候选；节点处于本工具保存的 Bypass 状态时也可被识别。类名匹配和 Mass/Drag/Tension 等属性只用于兼容性候选，设置目标时还会再次验证直接 Controller 结构。

一次已观察的工具扫描结果包含：

```text
Point003
Point007
Point014
Point015
```

该列表是当前场景的一次运行记录，不是应硬编码的固定事实。目标 `.max` 的静态字符串还包含其他 `Point*` 节点，因此每次正式 Bake 前仍需在 UI 列表核对实际 Helper。

### 2.2 最终 Bake 骨骼

当前自动规则为：

```maxscript
matchPattern node.name pattern:"Breast_*_bone_*" ignoreCase:true
```

并排除已配置 Spring Helper。目标 `.max` 的静态内容已确认存在 12 个名称：

```text
Breast_L_bone_000 ~ Breast_L_bone_005
Breast_R_bone_000 ~ Breast_R_bone_005
```

特殊命名 Rig 使用“选中骨骼设为目标”，但 Spring Helper 与 Bake 骨骼不能重合。正式导出 Bake 最终蒙皮骨，而不是只 Bake `Point*` Helper。

### 2.3 动画范围

场景静态元数据曾显示动画范围可能为 `0-0`。工具会从当前 `animationRange` 初始化起止帧，但美术在执行精确预览或 Bake 前必须核对：

- 动作真实起始帧；
- 动作真实结束帧；
- Spring 是否需要起始帧前 Warm-up；
- `sampleStep`，正式输出默认使用 `1`。

不能依赖工具默认 `0-100` 代替当前动作范围。

## 3. 当前实现结构

### 3.1 场景级和节点级持久状态

脚本使用两个 Custom Attribute 定义，`version:1` 且 `attribID` 固定：

```text
ACGSB_SceneData
  toolVersion
  springNodes / bakeNodes
  rollingFrames
  bakeStartFrame / bakeEndFrame / sampleStep
  currentMode

ACGSB_NodeData
  originalSpringPosition / bypassPosition / isSpringBypassed
  originalPosition / originalRotation
  bakedPositionList / bakedRotationList
  bakedPosition / bakedRotation / hasBake
  bakedStartFrame / bakedEndFrame / bakedSampleStep
```

Scene Data 挂在 `rootNode`，Node Data 挂在受管理节点。`#maxObject` 保存真实 Controller 引用，因此 Bypass/Bake 状态可随 `.max` 保存并在重开后恢复。

维护时区分工具显示版本和 CA Schema 版本：仅改 UI/逻辑可更新 `ACGSB_VERSION`；新增/改变持久字段或迁移语义时需要单独设计 Custom Attribute 兼容与迁移，不能只改字符串版本。

### 3.2 四态工作流

| UI 状态 | 当前实现 |
| --- | --- |
| 快速编辑 / 只重算最近帧 | 恢复 Spring；Original 权重 100%；通过 `SpringQuickEditMode=true` 和 `SpringRollingStart` 做近似回算。 |
| 无模拟编辑 / 完全停止弹簧运算 | 在起始帧读取 Helper 局部位置，用静态 `Position_XYZ` 替换直接 Spring；原 Controller 持久化保存。 |
| 精确预览 / 从起始帧完整求解 | 恢复 Spring、Original 轨道，关闭 Quick Edit，从起始帧逐帧推进到当前帧并读取 Transform。 |
| Baked | 顺序采样最终骨骼，写入可逆 List Controller，切 Baked 权重，再 Bypass Spring。 |

Bake 后两个结果切换按钮使用结果导向文案：

- `切回实时弹簧（继续调整）`
- `切回烘焙动画（播放/导出）`

“切回实时弹簧”不会删除 Bake 数据；“移除烘焙 / 恢复原控制器”才执行 Unbake。

### 3.3 Bypass 与外部修改保护

工具只处理节点 Position 上直接挂载的 Spring：

1. 在 Rest/起始帧缓存 Parent Local Position；
2. 创建 `Position_XYZ`，`animate off` 写静态值；
3. 保存原 Spring 和 Bypass 引用；
4. 替换 Position Controller；
5. 失败时只恢复本次已经替换的节点。

恢复前会检查当前 `node.position.controller == data.bypassPosition`。若动画师或其他工具在无模拟状态下替换过 Controller，自动恢复会停止并提示，不覆盖外部修改。

### 3.4 精确求值和两阶段 Bake

精确预览与 Bake 都会关闭 Quick Edit，并从配置起始帧按时间顺序推进。每帧读取最终骨骼 Transform 触发求值。

Bake 分两阶段：

1. **采样**：缓存每根最终骨骼的 Parent Local `translationpart`、`rotationpart` 和时间；不修改目标 Controller。
2. **写入**：为所有节点创建临时 `Linear_Position` / `TCB_Rotation`，写完全部 Key 后才提交到正式 Baked Slot。

重新 Bake 时保留旧 Baked Controller，只有新控制器全部写完才替换；提交中途失败会按已提交数量恢复旧结果。本次新建的 List 才会在失败时移除。

### 3.5 可逆 Controller List

最终骨骼结构：

```text
Position List
 ├─ ACG Spring Baked Position：Linear Position
 └─ Original Position

Rotation List
 ├─ ACG Spring Baked Rotation：TCB Rotation
 └─ Original Rotation
```

两个 Slot 权重控制器使用 `Boolean_Float`：

```text
Baked：Slot 1 = 100，Slot 2 = 0，Active = 1
Live： Slot 1 = 0，Slot 2 = 100，Active = 2
```

该用法已对照本机 3ds Max 2024.2 自带 MassFX `px_bake.ms`。当前工具只 Bake Position 和 Rotation，Scale 保持原控制器。

### 3.6 全局偏好与异常恢复

脚本捕获并恢复：

- `SpringQuickEditMode`；
- `SpringRollingStart`；
- Bake 写键期间的 `maxOps.autoKeyDefaultKeyOn`；
- `sliderTime`；
- Progress UI；
- Live/Baked 权重和 Spring Bypass 状态。

`busy` 阻止重复操作。Bake 失败时优先恢复 Live Spring、Original 权重、原 Quick Edit 偏好和时间滑块，再显示错误。

## 4. 已确认问题、根因和解决方式

### 4.1 安装脚本顶层 `local` 导致编译失败

**现象**：

```text
Compile error: no local declarations at top level: sourceDir
```

**根因**：旧安装脚本在 MAXScript 顶层使用 `local`，且安装流程不符合用户“拖 `.ms` 直接运行”的交付要求。

**修正**：删除 `Install_SpringAnimationBaker.ms` 和 `.mcr`，核心工具保持单文件直接运行。

### 4.2 `catch` 内带参数重抛导致编译失败

**现象**：

```text
only throws without arguments are permitted in catch expressions
```

**根因**：使用 `throw (getCurrentException())` 重新抛出。

**修正**：所有 `catch` 内重抛统一为 `throw()`；业务校验仍可在非 catch 路径使用 `throw "错误文本"`。

### 4.3 自动扫描 Bake 骨骼调用 `undefined`

**现象**：

```text
Type error: Call needs function or class, got: undefined
```

**根因**：`scanBreastBakeNodes` 在 `struct` 中调用了声明在它之后的 `configuredSpringNodes`，当前 MAXScript 解析路径将未解析成员当作 `undefined`。

**修正**：把配置访问器移动到扫描函数之前；扫描按钮错误信息增加“自动扫描胸部 Bake 骨骼失败”上下文。

**验证边界**：声明顺序静态检查已通过；仍需在目标场景再次确认自动扫描实际返回 12 根骨骼。

### 4.4 UI 标题越界和字体过小

**现象**：原生 Spinner 的 `Quick Edit 回退帧` 标题向左越过 GroupBox；模式和 Bake 按钮字体偏小，英文内部术语对美术不直观。

**修正**：

- 窗口扩大到 `620 × 870`；
- Spinner 使用空标题，独立 `.NET Label` 显示中文；
- 模式按钮使用 `System.Windows.Forms.Button` 和微软雅黑；
- 参数标签 `10pt`，模式按钮 `10.5pt`，Bake 操作按钮 `11.5pt`；
- `.NET` Click 事件使用 MAXScript 的单参数签名；
- 按钮改为“只重算最近帧”“完全停止弹簧运算”“从起始帧完整求解”“继续调整”“播放/导出”等结果导向文本。

用户截图已确认 `.NET` Bake 按钮能显示和点击区域正常；最终 `1.2.2` 只在 `1.2.1` 基础上更新两个切换按钮文案。

## 5. 当前验证状态

### 5.1 已完成

- 本机 `Autodesk.Max.xml` 已确认 `IInterface8.SpringQuickEditMode` 与 `SpringRollingStart` 的语义。
- 3ds Max 自带 MassFX `px_bake.ms` 已确认 `Position_List`、`Rotation_List`、`Boolean_Float`、权重和 Active Slot 用法。
- Spring 插件静态信息已确认 `PositionSpring`、`Point3Spring` 类名候选。
- 目标 `.max` 静态字符串已确认 12 根 `Breast_*_bone_*` 名称和多个 `Point*` 节点。
- 用户已在 3ds Max 中成功打开工具窗口；早期 Spring 自动扫描和无模拟状态有 UI 运行记录。
- `1.2.2` 源码已完成括号/字符串、关键 Token、成员声明顺序、`.NET` 单参数事件签名和 UI 文案静态检查。
- 功能 README 已记录运行方式、四态工作流、安全边界、诊断和验收建议。

### 5.2 尚未完成

- 尚未获得修正成员声明顺序后“自动扫描胸部 Bake 骨骼 = 12 根”的最终截图/Listener 记录。
- 尚未在目标场景完成“精确预览到当前帧”的逐帧结果验收。
- 尚未完成首次完整 Bake、连续两次 Bake 一致性、Live/Baked 逐帧 A/B。
- 尚未验证 Baked 状态确实消除 Spring 播放/编辑卡顿的量化数据。
- 尚未验证保存重开后 Bypass/Baked 状态和 Unbake 恢复。
- 尚未验证非均匀缩放、Shear、约束或特殊父级对 Parent Local Position/Rotation 的影响。
- 尚未导出 FBX 并在 Unity 中确认最终蒙皮骨骼动画、轴向、单位和帧范围。
- 自动运行 `3dsmaxbatch.exe` 加载当前脚本曾被代理执行环境拒绝，因此不能用该结果替代用户侧真实 Max 测试。

## 6. 推荐验收顺序

1. 备份目标 `.max`，把动画范围设置为真实动作范围。
2. 拖入 `SpringAnimationBaker.ms`，确认标题版本 `1.2.2` 和 UI 无越界。
3. 自动扫描直接 Spring，核对 Helper 列表和当前 Controller 类型。
4. 自动扫描胸部 Bake 骨骼，确认左右各 6 根、共 12 根，且没有 `Point*`。
5. 分别在普通 Spring、快速编辑、无模拟编辑下拖动相同主骨，记录延迟。
6. 从起始帧执行精确预览到代表帧，检查 Spring 运动连续。
7. 以 `sampleStep=1` 完整 Bake；逐帧切换 Live/Baked 做 A/B。
8. 在同一输入上再 Bake 一次，确认结果一致。
9. 切 Baked 后 Scrub/K 帧，确认 Spring Helper 不再参与实时求值。
10. 保存、关闭、重开，验证 Live/Baked 和 Unbake。
11. 导出 FBX，在 Unity 中核对胸部蒙皮骨骼动画、帧范围、轴向和单位。

## 7. 维护检查单

- [ ] 保持单 `.ms` 拖入运行；除非用户明确改变部署方式，不新增安装器或 `.mcr`。
- [ ] 只接管直接 Position Spring；未知/嵌套 Controller 阻断并报告。
- [ ] Spring Helper 与最终 Bake 骨骼分开配置，禁止交集。
- [ ] Quick Edit 只用于近似编辑；精确预览和 Bake 强制关闭。
- [ ] 有历史依赖的 Spring 从动作起始/Warm-up 按时间顺序求值。
- [ ] 采样阶段不写关键帧；全部临时轨道成功后才提交。
- [ ] Live/Baked 切换保留 Bake；Unbake 才恢复原控制器并清理包装。
- [ ] 恢复前检查当前 Controller 仍是工具创建对象，外部修改时停止。
- [ ] 所有失败路径恢复时间、Quick Edit、Auto Key、进度和已修改节点。
- [ ] UI 改动在真实 3ds Max 中文界面和团队 DPI 下检查，`.NET` 事件使用单参数签名。
- [ ] 修改 Custom Attribute 字段时单独设计 Schema 迁移，不只更新工具版本字符串。
- [ ] 静态检查后仍需完成真实 Max、真实场景和 Unity 消费者验证。
