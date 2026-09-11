# ShaderVariantCollection 多来源合并工具参考

> 类型：REFERENCE；适用范围：把多份已保存的 `.shadervariants`（ShaderVariantCollection，下文简称 SVC）按统一去重键合并到指定目标 SVC 的 Unity Editor 工具；使用前提：必须以目标工程的 Unity 版本、Shader 资产、SVC 序列化结构、采集工具和构建消费方重新验证。
>
> 关联规则：`TOOL-CMP-01`、`TOOL-ARC-01`、`TOOL-UI-01`、`TOOL-OPS-01`、`TOOL-OPS-02`、`TOOL-VAL-01`、`TOOL-VAL-02`。
> 关联资料：关键字签名规范化、PassType 兼容边界和 SVC 维护检查见 [Unity URP Shader 变体白名单与构建期剥离实践](../../ShaderDevelopment/references/shader-variant-allowlist-stripping.md) 的 §3.3、§3.4 与 §4；交付检查见 [TA 工具集成检查清单](tool-integration-checklist.md)。

## 1. 适用场景

当变体收集必须按平台、图形 API 或质量档分别执行时，每份采集结果只覆盖当次运行暴露出来的变体。把这些结果汇总给裁剪或构建链路的常见做法有两种：

1. 让裁剪配置文件直接引用多份 SVC；
2. 先把多份 SVC 合并成一份目标 SVC，再让配置文件继续引用单个路径。

选择第 2 种的情况：

- 裁剪侧的配置字段本身是单个资产路径（本机 ProjectACG 的 `ShaderStrippingProfile.ShaderVariantCollectionAllowListPath` 即单路径字符串），为了多端收集去改裁剪器，会同时放大构建期风险与回归面。
- 合并结果需要人工评审、与 Shader/Profile 一起进版本库，或需要脱离裁剪器单独对比。
- 需要按 Shader 或按单条变体做局部挑选，而不是整份集合相加。

反向不适用：

- 合并工具不做裁剪决策，不判断某条变体“该不该留”；它只做集合运算。
- 不生成输入中不存在的变体，也不做变体组合展开。
- 不修改输入资产，不参与运行时代码或 Player 构建链。
- 如果目标只是临时对比两份 SVC 的差异，用只读审计/报告工具更合适，不要先写一份合并结果。

## 2. 前提与依赖

### 2.1 先冻结“合并”的语义

动手前必须先确认三件事，否则 UI 越完整越容易掩盖语义错误：

| 契约维度 | 需要确认的问题 |
| --- | --- |
| 粒度 | 整份集合、指定 Shader 的全部变体，还是指定 Shader 的单条变体？ |
| 模式 | 覆盖目标，还是与目标取去重并集？ |
| 身份 | 什么算“同一条变体”？即去重键由哪些字段组成。 |

粒度与模式是用户可见选择，身份是内部契约。三者都必须写在工具 README 中，并给出执行前的二次确认。

### 2.2 SVC 不是 `ScriptableObject`

`ShaderVariantCollection` 是 Unity 内置资产类型，不是 `ScriptableObject`。由此产生两个必须提前接受的限制：

- 不能用 `ScriptableObject.CreateInstance<ShaderVariantCollection>()` 创建实例；新建目标要走 `AssetDatabase` 的资产创建路径，或先在 Project 中建好资产再引用。
- `Undo.RegisterCompleteObjectUndo` 在部分 Unity 版本上对该类型会抛异常。写入前必须用 `try/catch` 包裹，失败时降级为警告并继续，不能因为无法登记 Undo 就中断合并。

### 2.3 资源来源与目标边界

- 输入与目标都应是工程内已保存的 SVC 资产，通过 `ObjectField`、文件夹选择和拖拽获得，并由 `AssetDatabase.GetAssetPath` 取得路径；不要要求用户输入裸字符串路径。
- 输入没有资产路径时（例如刚在内存里构造、尚未保存的实例）必须显式报错，而不是静默跳过。
- 目标必须校验类型、存在性并落在 `Assets/` 内；目标路径被其它类型资产占用时要在执行前阻断。

## 3. 实现或排查步骤

### 3.1 读取：序列化结构要双路径兼容

用 `SerializedObject` 读取 SVC，逐层兼容，不能只认一条属性路径：

1. Shader 表本身在不同 Unity 版本上暴露为 `m_Shaders.Array` 或 `m_Shaders`，两者都要尝试。
2. 元素索引取 `data[i].first`，取不到时回退到 `first`，得到 Shader 引用。
3. 变体数组取 `second.variants`，逐项读取 `keywords`（字符串）与 `passType`（int）。
4. `keywords` 或 `passType` 属性任一取不到就跳过该条并记录数量，不要用默认值伪造出一条变体。

读取阶段同时统计“原始条数”和“去重后条数”。两者差距过大通常意味着采集端重复写入，或去重键选错了维度。

### 3.2 去重键：引用 Shader 模块的签名规范

去重键固定为：

```text
{Shader 资产路径}|{Shader.name}|{(int)PassType}|{关键字签名}
```

关键字签名必须先把原字符串按空格、制表符、回车、换行拆分，去掉空项，再用稳定序（`StringComparer.Ordinal`）排序并以单个空格拼接。这样同一组关键字只因顺序不同就会被正确判定为同一条。签名规则、大小写语义与 PassType 兼容边界由 Shader 模块定义，本工具只复用，不重新定义，详见 [Unity URP Shader 变体白名单与构建期剥离实践](../../ShaderDevelopment/references/shader-variant-allowlist-stripping.md) 的 §3.3 与 §3.4。

排查时的常见根因：

- 关键字直接按原文比较 → 顺序不同被当成两条，目标文件出现重复变体。
- 用 `Shader.name` 单独做身份 → 同名 Shader 资产（不同目录、不同变体体量）被错误合并。
- 忽略 `PassType` → 同名关键字在不同 Pass 下被压成一条，构建期缺失 Pass。

### 3.3 合并模式与“目标同时作为输入”

- 覆盖模式：选择集直接作为结果，写前清空目标。
- 并集模式：先把目标现有变体读进同一个字典，再用选择集覆盖同键条目，从而同时保留目标原有内容与本次选择。

目标是允许出现在输入列表里的，这在实际使用中很常见（多端依次合并进同一份目标）。实现必须遵守“先读完所有输入，再读或跳过目标，最后统一写回”，否则并集模式会读到被覆盖后的中间状态，导致结果随执行顺序变化。

### 3.4 写回顺序

推荐固定为：

1. 结果按 `ShaderAssetPath → ShaderName → PassType → 关键字签名` 稳定排序；
2. `try/catch` 内登记 Undo，失败只告警；
3. `Clear()`；
4. 逐条 `Add(new ShaderVariantCollection.ShaderVariant(shader, passType, keywords))`；
5. `EditorUtility.SetDirty`；
6. `AssetDatabase.SaveAssets()`；
7. 刷新同名 JSON 清单（可选，见 §3.5）；
8. `AssetDatabase.ImportAsset(path, ForceUpdate)` 与 `AssetDatabase.Refresh(ForceUpdate)`。

稳定排序是必需的，不是格式洁癖：同一组输入重复执行要得到逐字节相同的结果，否则每次合并都会在版本库里产生无意义 diff，也会让“结果是否变化”这一最基本的人工核对失效。

### 3.5 同名 JSON 清单是可选的软依赖

部分工程的打包链路要求 SVC 旁边存在同名 `.json` 清单（例如 YooAsset 的变体集合清单）。这类清单应作为可选依赖处理：

- 通过类型名反射查找清单类型并调用其提取方法，不直接静态引用第三方程序集；
- 类型或方法缺失时静默跳过，让工具仍能只写 SVC；
- SVC 已写入但清单生成失败时只告警，并明确“SVC 已写入”这一事实，避免用户误以为整体失败而重复执行；
- 清单路径由 `Path.ChangeExtension(svcPath, ".json")` 推出，与目标 SVC 同名同级。

### 3.6 大列表变体选择器的 UI 模式

当某个 Shader 的变体达到数百条时，原生 `EditorGUILayout.Popup` 在下拉中会截断文本、无法搜索、难以滚动定位，实际不可用。可迁移的替代结构是“搜索 + 排序 + 可点击列表 + 关键字快捷过滤”：

1. 搜索框：空格分隔多个关键字，全部命中才算匹配（子串、大小写不敏感）；前缀 `-` 表示排除，例如 `_SHADOWS_SOFT -_MAIN_LIGHT_SHADOWS`。
2. 排序下拉：至少提供 `PassType → 关键字`、关键字字典序、关键字数量三种。
3. 列表：自绘行 + 选中/悬停底色，点击整行即选中；行内显示 `PassType` 与关键字摘要，悬停显示完整关键字与数量。
4. 关键字 chip：点一下把该关键字加进搜索条件，再点一下移出；chip 宽度用 `EditorStyles.miniButton.CalcSize()` 累加，超过可用宽度就换行。
5. 选中项详情：显示完整信息，并提供“复制关键字”“用它过滤”等对照操作。

必须遵守的三条约束：

- **选中状态用稳定键保存，不用下标。** 过滤、排序、重新读取输入都会让下标漂移，用下标会在下一次绘制时指向另一条变体。用去重键字符串（例如 `VariantKey`）保存选中项。
- **过滤结果里没有选中项时，保留选中并给出提示，不静默改选。** 静默回落会让用户以为选中项变了；只有确实发生了重新读取输入、原选中键已不存在时才回落到第一条。
- **缓存按 Shader 聚合的结果，不要在 `OnGUI` 里每帧重新排序。** 数百条变体的 LINQ 排序放在绘制路径上会明显拖慢窗口。

自绘行的实现方式：`EditorGUILayout.GetControlRect(false, rowHeight)` 取行矩形，`EditorGUI.DrawRect` 画选中/悬停底色，用 `Event.current.type == EventType.MouseDown && button == 0 && rowRect.Contains(mousePosition)` 判定点击，并用 `EditorGUIUtility.isProSkin` 区分深浅色主题。这条路径同时适用于其它“候选条目多、需要按特征收窄”的选择器。

### 3.7 菜单入口与选择校验

- 主窗口入口与 Project 右键入口分别声明，菜单路径必须在全工程唯一；右键入口建议使用 `MenuItem` 校验函数控制可用性，避免在无 SVC 选择时出现无效菜单项。
- 校验函数中：选中项本身是 SVC 时直接收集；选中项是文件夹时用 `AssetDatabase.FindAssets("t:ShaderVariantCollection", new[] { folder })` 递归收集。
- 窗口内同时支持把 SVC 拖进窗口（`DragAndDrop`），因为“在 Project 里选好再回来点添加”在批量场景下太慢。
- 结果文本要能定位源资产；执行前给出计划（输入数、读取变体数、本次写入数、目标原有数），执行后给出汇总。

## 4. 风险与不适用边界

- **覆盖模式是破坏性操作。** 目标原有内容会被清空，执行前必须二次确认，并在确认文案中给出目标原有的 Shader 数与变体数。
- **合并结果的完整程度完全取决于输入。** 工具不能让变体“更全”；想让三端变体完整，就必须在需要的平台/质量档上各跑过一遍收集。把这一条写进 README，避免把它当成裁剪替代品。
- **不能因为合并方便就绕过变体评审。** 已决定剔除的关键字（Editor/XR 关键字、Profile 中列为剔除项的局部组合）不应通过合并重新进入白名单；合并后仍需按 Shader 模块的最低检查清单复核。
- **`Shader.name` 与资产路径都要参与身份。** 只按名称或只按 GUID 都会在 Shader 改名、移动目录或存在同名 Shader 时产生误判。
- **不承诺 Undo 可用。** 因类型限制，Undo 可能登记失败；回滚路径以版本管理为主，这一点必须在 README 中写明，不能宣称“可撤销”。
- **软依赖缺失不是错误。** 第三方清单类型不存在时工具仍应可用；把这类依赖做成硬引用会让工具只能在特定打包工程中编译。

## 5. 验证与回退

### 5.1 无 Unity 宿主的编译与静态验证

Unity 实例被占用、Licensing 不可用或工程本身存在既有编译错误时，可以先用 Unity 自带 Roslyn 对目标脚本做单文件编译，取得语法、API 与类型引用层面的第一层证据。命令组织、引用集合和 define 声明方式见 [Unity Editor 脚本离线编译校验参考](unity-editor-script-offline-compile-verification.md)，本节只记录本工具实际完成的检查。

本工具本轮完成的是：单文件编译退出码 0、无警告无错误，以及菜单路径在全工程唯一、工具目录无 asmdef 冲突、旧实现类名无残留引用、`UTF-8` 无乱码。

**这一步不能替代 Unity 内的实跑。** `TOOL-VAL-01` 已明确：文本、dotnet 或反射检查只是补充，编译通过不代表菜单已注册、窗口能打开、写入结果正确。交付时必须把“已完成的静态检查”和“仍未完成的 Unity 行为验证”分开写明。

### 5.2 Unity 内的功能与边界验证

至少覆盖：

- 覆盖模式与并集模式各跑一次，核对目标最终的 `shaderCount` / `variantCount`；
- 边界用例：空输入、未指定目标、目标同时作为输入、指定 Shader 在输入中不存在、单条变体写入、选择文件夹递归收集、目标路径被其它类型资产占用；
- 变体选择器：搜索（含 `-` 排除）、排序、行点击、悬停提示、关键字 chip、“用它过滤”，以及搜索结果为空时选中项保持不变；
- 写回后重新打开目标资产与重载域，确认内容、顺序与清单一致；
- 若目标有同名 JSON 清单，确认其计数与实际 SVC 一致。

### 5.3 迁移到其它工程副本时的检查

把已完成的工具搬到同一 Unity 版本的另一份工程副本时，通用流程（迁移单位、GUID/类型/菜单/硬编码路径四项冲突预检、保字节复制、旧件下线顺序）见 [Unity Editor 工具集跨工程迁移与旧件下线参考](unity-editor-tool-cross-project-migration.md)。本工具需要注意的专项：

1. 先确认两侧 Unity 版本一致，再比较 SVC 序列化结构；跨版本必须先重读 `m_Shaders.Array` / `m_Shaders` 的实际形态。
2. `.cs` 与 README 的 `.meta` 可整体拷贝，前提是 GUID 在目标工程唯一。
3. 确认目标工程没有同名类、同菜单路径的既有实现。
4. 确认可选依赖（第三方清单类型、公共函数库）在目标工程存在；缺失时工具应仍能编译并只写 SVC。
5. 逐文件比对哈希，确认迁入的是同一版本，而不是“看起来一样”的旧实现。
6. 在目标工程重新编译，并实际打开菜单确认注册成功。

排查“菜单没出现”时，优先区分三种原因：脚本未编译进 Editor 程序集、菜单路径冲突、目标工程存在阻塞编译的既有错误。直接扫描编译产物（例如 `Library/ScriptAssemblies/Assembly-CSharp-Editor.dll`）中是否包含该菜单字符串，可以把“脚本没进来”和“菜单没注册”区分开。

### 5.4 回退

- 目标 SVC 与同名清单都是工程内普通资产，可直接用版本管理恢复；
- 工具本身不删除文件、不改动输入资产；
- 覆盖模式误操作后，先恢复目标 SVC 与清单，再重新执行合并，不要在文件级手工拼 JSON；
- 迁入其它工程后出现冲突，回退到“删除迁入文件与 `.meta` + 恢复目标工程原状”，不要保留半套实现。
