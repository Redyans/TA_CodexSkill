---
name: projectacg-3ds-max-spring-animation-baker-profile
description: ProjectACG 3ds Max Spring Animation Baker 的路径、版本、胸部 Rig、四态编辑、直接 PRS 可逆 Bake、诊断证据和当前验证边界。
---

# ProjectACG 3ds Max Spring Animation Baker Profile

> 类型：PROFILE；适用范围：当前 ProjectACG 的 3ds Max Spring 胸部次级运动工具；通用规则见 [DCC 开发模块](../../README_Tech_DCCDevelopmentRules.md)，实现与排查方法见 [3ds Max Spring Controller 动画编辑与可逆 Bake 参考](../../references/3ds-max-spring-controller-edit-and-reversible-bake.md)。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| 当前工具版本 | `2.1.0`；已完成静态修改，尚未获得目标 Max 的本版 Bake 结果。 |
| 主脚本 | `D:\work2025U3D\Tool\3dsMax\SpringAnimationBaker\SpringAnimationBaker.ms`。 |
| 功能 README | `D:\work2025U3D\Tool\3dsMax\SpringAnimationBaker\README.md`。 |
| 独立诊断脚本 | `D:\work2025U3D\Tool\3dsMax\SpringAnimationBaker\SpringRigDiagnostic.ms`，版本标记 `2.1-time-safe`。 |
| 运行方式 | 不安装；拖入单个 `.ms`，或使用 `Scripting > Run Script`。 |
| 目标 DCC | 3ds Max 2024.2；报告的 `MaxVersion` 为 `#(26000, 64, 0, 26, 2, 0, 22013, 2024, ".2 Update")`。 |
| 当前测试场景 | `G:\Connect\ConnectDocs\Doc\WorkDoc\Repor\Work_word\Valkyria\周内容\2026年8月21日\mesh_100601_QHigh胸加弹簧骨骼.max`。 |
| 最新报告 | 同目录的 `SpringAnimationBaker_Diagnostic_Report.txt`。 |
| 最终消费者 | ProjectACG 角色胸部蒙皮骨；最终仍需 FBX → Unity 验证。 |

不要修改用户保留的备份目录：

```text
D:\work2025U3D\Tool\3dsMax\SpringAnimationBaker - 副本
```

用户明确要求不启动 3ds Max 代跑；代码代理只修改脚本和做静态检查，真实 Max 测试由用户执行。

## 2. 当前 Rig 契约

### 2.1 Spring Helper

当前报告确认 4 个 Position 上直接 Spring：

```text
Point003
Point014
Point015
Point007
```

它们分别挂在胸部骨骼/Point 层级中。该列表来自当前场景报告，不得硬编码成跨 Rig 事实；每次 Bake 前仍要自动扫描并在 UI 核对。

### 2.2 最终 Bake 骨骼

当前项目适配器匹配：

```maxscript
matchPattern node.name pattern:"Breast_*_bone_*" ignoreCase:true
```

当前报告确认左右各 6 根，共 12 根：

```text
Breast_R_bone_000 ~ Breast_R_bone_005
Breast_L_bone_000 ~ Breast_L_bone_005
```

Spring Point 与 Bake 骨骼必须分开配置；正式 Bake 最终蒙皮骨，而不是只 Bake Helper。

### 2.3 变换结构

当前关键风险节点：

- `Breast_R_bone_003`、`Breast_L_bone_003` 使用 `Scale_Expression`；
- `bone_003`/`bone_004` 当前世界矩阵存在非均匀缩放；
- `bone_004` 的父级也存在非均匀缩放；
- Rig 包含镜像/负轴语义，`rotationpart` 不是唯一稳定表示；
- 部分骨骼使用 Position/Rotation List，`bone_005` 的 Parent 使用 Link Constraint。

旧报告已经证明 `Scale_Expression` 不是固定 `1.04`。错误时间轴下曾观测到右侧 `1.34075`、左侧 `1.41156`；这些具体动态数值受旧时间 Bug 影响，不能继续作为正确帧值引用，但足以证明 Scale 输出会随求值变化，不能保留静态原 Scale 代替 Bake。

## 3. 当前状态与实现

### 3.1 四态工作流

| UI 状态 | 当前实现 |
| --- | --- |
| 快速编辑 | 恢复实时 Spring 和 Original PRS，开启 `SpringQuickEditMode`，只回退指定帧。 |
| 无模拟编辑 | 用静态 Position Bypass 替换直接 Spring，编辑原动画，不显示 Baked 次级运动。 |
| 精确预览 | 恢复 Spring，关闭 Quick Edit，从起始帧用显式 `Time` 逐帧求值到当前帧。 |
| Baked | 使用直接 Baked Position/Rotation/Scale，复用 Bake 时验证过的静态 Spring Bypass。 |

UI 切换按钮使用结果导向文案，并提供橙色“正在…”与蓝色“✓ 当前”反馈。

### 3.2 持久状态

`ACGSB_SceneData` 保存节点集合、范围、Step 和当前模式；`ACGSB_NodeData version:2` 保存：

```text
Original Spring Position / Bypass Position / Is Bypassed
Original Position / Rotation / Scale
Baked Position / Rotation / Scale
Legacy Position/Rotation/Scale List references
Has Bake / start / end / step
```

2.x 新 Bake 使用直接 PRS Controller 引用切换；旧 1.x List 只保留识别和安全 Unbake。检测到旧 List 时，执行新 Bake 前要求先 Unbake。

### 3.3 两种 Bake 模式

- **兼容 Bake（推荐）**：直接写 Position/Rotation/Scale；World Position 和完整 Basis 参与阻断；镜像矩阵 Rotation 只作参考。
- **严格 Bake**：相同写入，再让 Rotation 与 Parent Local Scale 参与阻断。

两种模式都执行：自动预检 → 顺序采样 → 健康检查 → Spring Bypass → 直接 PRS 写 Key → World 验证 → Baked/Live/Baked 往返 → 成功提交或失败回滚。

### 3.4 当前写入语义

实时 World/Parent World 采样后，通过隐藏普通 PRS Point 求解目标 Local PRS：

- Position：Solver Position；
- Rotation：`solver.rotation.controller.value`；
- Scale：`solver.scale.controller.value`，同时保存 Local `scalepart` 供验证。

每个采样时间显式 `addNewKey`，再在对应 `at time` 中写入直接 `Linear_Position`、`Linear_Rotation`、`Bezier_Scale` 的 Controller `.value`。

## 4. 已确认问题、证据和解决方式

### 4.1 UI、加载和扫描

- 删除安装依赖，保持单 `.ms` 拖入运行；空安装脚本不作为入口。
- 修正顶层 `local`、catch 重抛、Struct 成员声明顺序和 `.NET` 单参数事件。
- 窗口当前为 `620 × 920`；Spinner 使用独立 Label，关键按钮使用较大中文字体。
- Spring/Bake 自动扫描分开，Bake 名称扫描排除 Spring Point。

### 4.2 Live/Baked 往返

早期 Bake Key 已存在，但 Live → Baked 后视觉结果丢失。根因是切回 Baked 时重新采样并覆盖静态 Spring Bypass 基准。当前方案保存并复用 Bake 成功时同一套 Bypass，并在声明成功前执行 Baked → Live → Baked 往返验证。

### 4.3 List 与动态 Scale

旧 List Bake 在镜像骨骼和 `Scale_Expression` 下改变复合变换语义，而且只保留原 Scale 会丢失动态结果。2.x 改为直接 Original/Baked PRS 引用切换，并逐帧写 Position/Rotation/Scale。

### 4.4 Rotation 属性、Controller 与 Key 的语义差异

`2.0.0` 报告中 Expected Local TM 与 Actual Local TM 基本互为转置。报告还显示当时“采样 Rotation = 写入 Controller Rotation”，但 World Basis 错误，证明隐藏 Solver 的 `node.rotation` 不能直接写入 `Linear_Rotation.value`。

`2.0.1` 改为采样 `solver.rotation.controller.value`，第 0 帧大范围转置错误消失。

`2.0.2` 尝试直接写 `RotationKey.value` 后，第 0 帧再次出现约 `164°` 和 Basis `1.979` 的错误；报告显示 Key 写入值与 Controller 读回值在四元数向量部分取反。当前已撤销 Key 直写，恢复 Controller `.value` 语义。

### 4.5 MAXScript Time/ticks 共同根因

旧代码使用：

```maxscript
local evaluationTime = evaluationFrame * ticksPerFrame
sliderTime = evaluationTime
```

`evaluationTime` 是 Integer。MAXScript 在时间上下文中把普通数字解释为帧，因此第 1 帧产生的 `160` 被当成第 160 帧，而不是 160 ticks。该错误同时影响：

- Spring Bypass 的 Rest 时间；
- 精确预览；
- 顺序采样；
- `addNewKey`；
- Baked 验证；
- 独立诊断脚本。

证据是 `2.0.1` 报告的第 1、2、3 帧复杂矩阵完全相同，错误位置统一打印为 `160`。`2.1.0` 统一改为：

```maxscript
local evaluationTime = 1f * evaluationFrame
```

并在验证报告增加：

```text
采样时间范围：0f -> 50f | Time
```

该根因由 Autodesk 2024 MAXScript Time Values 官方文档确认；`2.1.0` 尚待用户在真实 Max 复测。

### 4.6 预检、验证与回滚

自动预检分通过、警告、阻断。镜像/非均匀 Scale 为警告，最终由 World Basis 判断；无效数值、零轴、不可逆矩阵、引用损坏和外部部分替换直接阻断。

失败后：

- 恢复实时 Spring；
- 回滚本次无效直接 PRS；
- 原 Controller 不清除；
- `HasBake=false`；
- 自动写入详细 TXT。

## 5. 版本诊断结论

| 版本 | 报告结论 | 当前处理 |
| --- | --- | --- |
| `1.2.4/1.2.5` | 用户确认曾成功生成 Bake；后续主要问题是 Live/Baked 往返 | 保留其“世界采样、静态 Bypass 复用、往返验证”原则 |
| `1.2.8~1.9.0` | 增加预检、Scale、详细报告；仍受镜像/动态 Scale 表示影响 | 诊断和回滚继续保留 |
| `2.0.0` | 直接 PRS；Rotation Local TM 转置，第 0 帧大误差 | Rotation 改采 Controller 值 |
| `2.0.1` | 第 0 帧转置修复；第 1~3 帧相同，错误打印 `160` | 定位为时间类型错误 |
| `2.0.2` | Key 直写使 Rotation 第 0 帧回退 | 撤销 Key 直写 |
| `2.1.0` | 显式 `Time`，恢复 Controller 写入，诊断增加时间类型 | 静态检查通过；真实 Max 待验 |

## 6. 当前验证状态

### 6.1 已完成

- `2.0.0~2.0.2` 用户侧真实 Max 报告已用于定位矩阵、Controller 和时间问题。
- 最新失败均正确回滚，报告显示 12 根骨骼 `HasBake=false`、`Storage=None`。
- `2.1.0` 主脚本完成括号/字符串、版本、显式 Time、三类 Key 写入和旧 ticks 时间用法静态检查。
- `SpringRigDiagnostic.ms` 同步为显式 Time。
- Autodesk 2024 官方文档确认普通数字按帧解释、Time 转 Integer 才返回 ticks。
- 用户备份目录未修改，Codex 未启动 3ds Max。

### 6.2 尚未完成

- `2.1.0` 尚未在目标场景完成兼容 Bake。
- 尚未确认新报告显示 `采样时间范围：0f -> 50f | Time`。
- 尚未确认 51 个 Position/Rotation/Scale Key 落在 `0f..50f`。
- 尚未获得兼容 Bake 的 Position/Basis 通过结果。
- 严格 Bake、重复 Bake、Live/Baked 往返、保存重开、Unbake 尚待本版验证。
- 尚未量化 Baked/Bypass 性能，也未完成 FBX → Unity 验收。

## 7. 下一轮验收顺序

1. 另存场景备份，确认动画范围 `0..50`、Step `1`。
2. 拖入主脚本，确认标题 `2.1.0`。
3. 自动扫描并核对 4 个 Spring、12 根 Bake 骨骼。
4. 先执行兼容 Bake。
5. 若成功，检查每条直接 PRS 为 51 Key，并执行 Live/Baked 多次往返。
6. 若失败，先看报告的 `采样时间范围`；必须是 `0f -> 50f | Time`。
7. 再按首个超限父节点分析 Rotation/Scale，不从子级放大误差倒推。
8. 兼容 Bake 通过后再测严格 Bake。
9. 保存重开，验证状态和 Unbake。
10. 导出 FBX，在 Unity 核对胸部蒙皮骨动画、轴向、单位和帧范围。

## 8. 维护检查单

- [ ] 保持单 `.ms` 拖入运行，不默认引入安装器。
- [ ] 不修改 `SpringAnimationBaker - 副本`。
- [ ] Spring Helper 与最终 Bake 骨骼分开配置。
- [ ] Quick Edit 只作近似；精确预览/Bake 强制关闭。
- [ ] 所有时间 API 输入必须是 `Time`，禁止把 ticks Integer 直接传入。
- [ ] 诊断必须打印采样时间首尾和类型。
- [ ] 采样阶段不写 Key；健康检查通过后才写。
- [ ] Position/Rotation/Scale 使用各自验证过的 Controller 语义。
- [ ] 动态 Scale 必须逐帧 Bake；Shear 由 Basis 验证决定是否支持。
- [ ] 兼容与严格 Bake 都执行 World A/B、往返和失败回滚。
- [ ] Live/Baked 只切输出；Unbake 才清理 Bake。
- [ ] 外部替换 Controller 时阻断，不强制覆盖。
- [ ] UI 必须有进行中、成功和失败反馈。
- [ ] 静态检查不能替代真实 Max、真实 Rig 和 Unity 验证。
