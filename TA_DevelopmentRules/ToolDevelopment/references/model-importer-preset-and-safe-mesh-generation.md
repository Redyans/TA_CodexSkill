---
name: unity-model-importer-preset-and-safe-mesh-generation
description: Unity Editor 工具在生成外置 Mesh 和 Prefab 时临时应用 ModelImporter 预设、保持旧资源行为并安全恢复的通用实现与排查参考。
---

# Unity ModelImporter 预设与安全 Mesh/Prefab 生成参考

> 类型：REFERENCE；适用范围：Unity Editor 中需要按流程配置覆盖 FBX `ModelImporter` 设置，并据此生成外置 Mesh、Material 或 Prefab 的工具；使用前提：必须按目标 Unity 版本、ModelImporter 公开 API、Prefab 引用方式和实际生成链重新验证。
>
> 关联规则：`TOOL-CMP-01`、`TOOL-ARC-01`、`TOOL-ARC-02`、`TOOL-ARC-03`、`TOOL-OPS-01`、`TOOL-OPS-02`、`TOOL-OPS-03`、`TOOL-VAL-01`、`TOOL-VAL-02`、`TOOL-VAL-03`、`TOOL-EVO-01`。

## 1. 适用场景

这类需求通常同时具备以下特征：

- 角色或模型的 FBX 导入设置需要由流程预设控制，而不是依赖每个 FBX 手工修改。
- 同一套配置要作用于 Mesh 提取、材质提取、Prefab 生成或主模型/LOD 模型。
- 只希望覆盖指定项目；未指定的 `ModelImporter` 设置必须继续使用源 FBX 原值。
- 生成结束后不应把临时设置永久写回源 FBX，也不应让异常留下半套导入结果。
- 两个或多个同源工程需要同步实现，但各自工作区可能存在独立脏改动。

它不是普通 Inspector 字段编辑，而是“配置型批量生成 + 受控导入”的高影响工具。实现前要把输入、导入器、生成输出、恢复和验证边界分开。

## 2. 前提与依赖

### 2.1 先确认资产和 API 边界

1. 源文件必须能通过 `AssetDatabase` 定位，并且其 `AssetImporter` 确认是 `ModelImporter`。
2. 目标 Unity 版本必须公开提供要写入的属性。优先使用 `ModelImporter` 公共 API，不依赖内部序列化键或 Inspector 私有字段。
3. 生成链必须明确 Prefab 最终引用的是 FBX 内部 Mesh 还是外置 Mesh。若设置只在导入期间临时生效，通常必须生成外置 Mesh，才能在恢复 FBX 后保留结果。
4. 主模型、LOD、变体和重入步骤必须明确哪些源文件共享预设，哪些文件需要独立快照。
5. 工具应位于 `Editor` 目录或 Editor-only asmdef；除非有明确 Runtime 消费者，不要把导入器逻辑带入 Player。

### 2.2 先冻结兼容契约

在新增字段前记录现有配置的：

- 序列化字段名和默认值；
- 生成 Mesh/Material/Prefab 的开关与输出路径；
- 已有 Prefab 的刷新/增量策略；
- 源 FBX 是否允许被重导入；
- 旧的一次性开关是否需要迁移；
- 失败、取消和覆盖时的回滚方式。

新增预设默认关闭，不能因为工具升级而改变旧配置的导入结果。

## 3. 实现或排查步骤

### 3.1 配置模型：总开关、独立覆盖和值

推荐为每个可覆盖项目保存一组：

```text
OverrideX = 是否覆盖源 FBX 的 X
X         = 覆盖时写入的值
```

总配置还应有 `Enabled`。实际生效条件应同时满足：

1. 当前步骤确实从模型 FBX 生成；
2. 开启外置 Mesh/Material 生成；
3. `Enabled` 为真；
4. 至少有一个 `OverrideX` 被勾选。

未勾选的项目不能使用预设中的默认值覆盖源 FBX，而应从本次流程最初捕获的原始快照取值。这一点是避免“只想改 Normals 却意外重置 Scale、UV 或 Cameras”的关键。

### 3.2 将 Unity 的复合选项映射为稳定配置

某些 Inspector 选项不是一个 `ModelImporter` 属性。例如 `Optimize Mesh` 通常由两个布尔值组成：`optimizeMeshPolygons` 和 `optimizeMeshVertices`。建议在配置层使用一个清晰的枚举，再在执行层集中映射：

| 配置枚举 | `optimizeMeshPolygons` | `optimizeMeshVertices` |
| --- | ---: | ---: |
| 不优化 | `false` | `false` |
| 全部优化 | `true` | `true` |
| 仅多边形顺序 | `true` | `false` |
| 仅顶点顺序 | `false` | `true` |

不要把这类映射散落在多个 UI 回调或生成分支中；集中函数更容易测试和迁移。

### 3.3 生成前捕获、按步骤应用、完成后恢复

推荐的执行时序如下：

1. 为每个规范化后的 FBX 路径只捕获一次完整原始 `ModelImporter` 快照。
2. 根据当前流程步骤从原始快照重新计算目标值：勾选的项取预设值，未勾选的项取原值。
3. 如果平滑法线、坐标轴重建或其他 Mesh 处理要求顶点读取，临时把 `isReadable` 设为 `true`。
4. 只有确实有差异时才写属性并调用 `SaveAndReimport()`；记录目标路径和改变的项目。
5. 生成外置 Mesh、Material 和 Prefab；主 FBX 与可选 LOD FBX 使用同一预设时分别应用，但共享各自原始快照。
6. 在最外层 `finally` 中遍历所有快照，恢复原值并再次 `SaveAndReimport()`。
7. 恢复某个 FBX 失败时继续尝试其他 FBX，最后汇总异常并让调用方知道流程不完整。

不能在每个步骤结束时只恢复“上一步的值”，因为多步骤构建会把上一步临时值当成下一步的原值。所有步骤都必须回到同一份初始快照。

### 3.4 UI 应显式表达覆盖风险

配置型 Inspector 至少应提供：

- 按 `Scene`、`Meshes`、`Geometry` 分组的 Foldout；
- 每行独立覆盖勾选，未勾选时禁用对应值字段；
- “套用示例”与“清空覆盖”这类批量操作；
- 明确说明只在外置 Mesh 生成时生效；
- 明确说明主模型/LOD 的作用范围、临时重导入和恢复策略；
- 旧字段检测及迁移入口，而不是静默丢弃旧配置。

OnGUI/Inspector 只负责展示和收集输入，不应在绘制阶段执行 `SaveAndReimport()` 或批量写资产。

### 3.5 公开 API 与不稳定字段的取舍

当 Unity Inspector 中出现没有稳定公共属性的字段（例如某些版本的 Legacy 选项）时：

1. 先查目标 Unity 版本的公开 `ModelImporter` API；
2. 没有稳定 API 时不要写内部序列化字段；
3. 将该项明确记录为“不支持/不覆盖”，并说明原因；
4. 若需求必须支持，单独设计版本适配和 Unity 内回归测试，不要混入普通预设逻辑。

### 3.6 日志和异常

每次临时应用或恢复至少记录：工具身份、FBX 路径、改变的设置和阶段。批量结束时汇总成功、失败、跳过和恢复失败。异常必须保留堆栈并能定位到资产；Unity 项目要求使用 `UnityEngine.Debug`（如 `Debug.LogException`），不要引入不属于该工具程序集的运行时日志依赖。

## 4. 风险与不适用边界

- **外置 Mesh 是保存导入结果的前提**：Prefab 若继续引用 FBX 内部 Mesh，恢复源 FBX 后临时法线、切线、顶点焊接或几何设置可能丢失。
- **重导入不是无副作用操作**：`SaveAndReimport()` 可能刷新模型子资产、触发其他导入器或使窗口缓存失效。必须限制路径和次数，不要把局部需求实现成全局 `AssetPostprocessor`。
- **Read/Write 有内存成本**：只为处理阶段临时开启，完成后恢复；不要把它当作默认全工程设置。
- **几何和优化会改变结果**：Weld、Optimize、Index Format、Normals、Tangents 等可能改变顶点数量、顺序、法线或绑定结果；需要用实际 Mesh 检查，而不是只看 Inspector。
- **版本属性会漂移**：不同 Unity 版本的枚举和可写性可能不同；静态编译通过不等于目标版本的导入行为一致。
- **配置只作用于明确的 ModelFbx 步骤**：Prefab-from-step 或仅更新 Prefab 的步骤不能假定会重新生成 Mesh。
- **未做真实 FBX 生成不能宣称端到端安全**：代码、Inspector 和策略测试只能证明结构，不证明生产角色输出、LOD 和恢复一定正确。

## 5. 验证与回退

### 5.1 推荐验证矩阵

| 层级 | 最小检查 | 通过条件 |
| --- | --- | --- |
| 静态 | 公共 API、序列化字段、日志、`TA/MMD` 排除范围 | 无内部字段误用、无不允许的运行时日志依赖 |
| 程序集 | Editor 和 Editor Tests asmdef 编译 | 目标 Unity 版本 `0 warning / 0 error` |
| 策略测试 | 默认关闭、无覆盖、仅一个覆盖、复合 Optimize 映射、来源类型/生成开关 | 未覆盖项保留原值，生效闸门和映射符合预期 |
| Unity UI | 打开配置资产、展开 Foldout、点击示例/清空、保存重开 | UI 字段齐全，默认值不被意外写回 |
| 临时 FBX E2E | 复制非生产 FBX，记录 importer 原值，执行生成，检查外置 Mesh，再比较 importer | 输出符合预设，主模型/LOD 正确，结束后原值完全恢复 |
| 异常/取消 | 在导入、生成和恢复阶段制造失败或取消 | 不留下半成品 Prefab，所有可恢复的 FBX 都回到原值 |
| Player | 检查 asmdef 和 Player 编译 | Editor 工具未进入 Player 程序集或包体 |

### 5.2 回退策略

- 普通用户回退：关闭总开关，或点击“清空覆盖勾选”；旧配置仍按源 FBX 原值生成。
- 旧字段迁移失败：保留隐藏兼容字段，不删除用户数据，要求人工确认后再迁移。
- 真实生成失败：保留原 Prefab 和源 FBX，先检查恢复日志；不要用“再次生成”覆盖正式资产。
- 导入器恢复失败：停止后续自动批处理，保存路径和异常列表，使用版本控制或 FBX 备份恢复；确认所有源文件状态后再重试。
- Unity 版本升级：重新检查公开属性、枚举映射和临时 FBX 矩阵，不以旧版本序列化结果直接视为兼容。

