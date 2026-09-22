# Unity Timeline 嵌套 Prefab 保存与生成器绑定参考

> 类型：`REFERENCE`；适用范围：用 Unity Editor 工具保存主 Timeline Prefab 内的嵌套 Prefab，或生成、修复 Timeline Prefab 上的绑定组件与序列化引用；使用前提：必须以目标项目的 Unity 版本、Prefab 层级、Timeline 资产、绑定组件与运行时消费者重新验证。
>
> 关联规则：`TML-ARC-01`、`TML-ARC-02`、`TML-CMP-01`、`TML-EDT-01`、`TML-VAL-01`；模块入口见 [Timeline 开发规则](../README_Tech_TimelineDevelopmentRules.md)；资产生成与持久绑定见 [Timeline 资产生成、Prefab 绑定与增量组装参考](timeline-asset-generation-and-prefab-binding.md)；镜头模式与运行时边界见 [Timeline 镜头制作、Editor 预览与运行时边界参考](timeline-camera-authoring-editor-preview-and-runtime-boundary.md)。

本文沉淀一个真实问题的处理方式：主 Timeline Prefab 内嵌套了特效、镜头或其它 Prefab 后，制作人员希望把层级中的人工修改只保存回某个嵌套源 Prefab，不保存、不覆盖、也不删除外层 Timeline Prefab。相同问题还会出现在“生成工具给主 Prefab 添加运行时绑定组件”这类自动写入流程中。两类任务共享同一个根因：编辑器工具必须先把“层级实例、源 Prefab、外层 Prefab、运行时消费者”四个所有权边界分开，再决定写入目标。

## 1. 适用场景

- 主 Timeline Prefab 的 `fx_group`、`camAni_group` 或其它容器下放有嵌套 Prefab 实例，需要把调整保存回该实例对应的源 Prefab。
- 保存嵌套 Prefab 时，外层 Timeline Prefab 仍处于打开或脏状态，但本次操作不得连带保存外层。
- 保存完成后，层级中不要又出现一份与源 Prefab 内容重复的实例覆盖。
- 同一个层级容器中存在多个实例，需要批量保存，但发现多个实例指向同一源 Prefab 时要阻止覆盖。
- 需要给主 Timeline Prefab 上的 `UltimateTimelineBindings` 之类运行时组件补字段或引用，并验证生成资产仍能被运行时正确消费。
- 排查以下故障：`ArgumentNullException: Value cannot be null. Parameter name: obj`、保存嵌套 Prefab 后外层 Timeline Prefab 被覆盖、主 Prefab 丢失、保存成功但层级出现两份相同对象、生成器写入字段后运行无效。

本参考不定义具体项目的固定目录、预制体名称、组件名、轨道名或相机模式。目标是项目特有时，只作为案例，不转化为跨项目规则。

## 2. 前提与依赖

### 2.1 先冻结四层所有权

| 层 | 例子 | 保存动作的边界 |
| --- | --- | --- |
| 层级中的 Prefab 实例 | `fx_group/tPre_fx_...` | 这是编辑现场，不是写入目标。 |
| 嵌套源 Prefab | `tPre_fx_...prefab` | “保存到源 Prefab”只写这里。 |
| 外层主 Timeline Prefab | `tPre_hero_...prefab` | 未被明确要求时不得 `ApplyPrefabInstance`、`SavePrefabAsset` 或连带 `SaveAssets`。 |
| 运行时组件与消费者 | 主 Prefab 上的绑定组件、Timeline Track、Runtime Context | 编辑器写入字段后仍要经过实际消费者的读取合同验证。 |

把四层拆开之后，工具 UI 或生成入口需要先回答：本次按钮或本次生成到底允许修改哪一层。  
如果一个入口同时可能修改多层，应拆成独立选项，例如“保存当前 Prefab”“保存嵌套 Prefab”“直接驱动模式的同步轨道”。不要让一个按钮隐式扩大写入范围。

### 2.2 先区分三种 Prefab 保存语义

| 语义 | 适用对象 | 典型 API | 关键风险 |
| --- | --- | --- | --- |
| 保存 Prefab Mode 中的当前资产 | 正打开在 Prefab Mode 的 Prefab | `PrefabUtility.SavePrefabAsset` | 不要误把场景实例当 Prefab Mode 内容处理。 |
| 把场景实例修改应用到外层源 Prefab | 场景中的 Prefab 实例 | `PrefabUtility.ApplyPrefabInstance` | 会保存整个实例根；嵌套 Prefab 位于其中时不能代替“只保存嵌套”。 |
| 写回一个指定的嵌套源 Prefab | 层级中的嵌套实例 | 克隆实例、解包克隆、`PrefabUtility.SaveAsPrefabAsset`、`RevertPrefabInstance` | 最容易出现源解析错误、外层误覆盖和实例覆盖残留。 |

三种语义不能混用一个 API 分支。原问题就来自把“嵌套实例”当成“普通实例”，并把不安全的源对象解析结果直接传给 Prefab API；当对象为空或解析链错误时，抛出了 `GetCorrespondingObjectFromSource<T>` 的 `ArgumentNullException`。

### 2.3 一次性收集写前快照

保存多个嵌套 Prefab 时，导入其中一个子 Prefab 可能使其它活动层级对象重新导入或失效。稳妥做法是先遍历活动层级并创建所有待写克隆与定位信息，再进入写入阶段；写入阶段不要重新扫描已经可能失效的层级。

```csharp
sealed class PrefabSaveSnapshot
{
    internal readonly GameObject GameObject;   // 已解包的克隆，只用于写入。
    internal readonly GameObject InstanceRoot; // 原活动实例，用于保存后定位或回退。
    internal readonly string AssetPath;        // 解析出的嵌套源 Prefab。
    internal readonly string InstanceName;
    internal readonly Transform InstanceParent;
}
```

快照对象要在 `finally` 中统一销毁，避免工具运行后在场景或 Prefab Mode 中留下临时对象。

## 3. 实现或排查步骤

### 3.1 源 Prefab 解析必须使用最近实例根，并对外层路径做拒绝

不要直接把 `PrefabUtility.GetCorrespondingObjectFromSource<T>` 的返回值传给后续 API，除非先判空。嵌套层级、错误 Pack Mode 或中间资产导入状态可能让该调用得到空对象，从而在泛型 API 内部抛出 `ArgumentNullException`。

推荐顺序如下：

1. 校验目标是非持久化、有效场景中的 Prefab 实例根。
2. 用 `PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot` 解析最近的嵌套源路径。
3. 最近根路径不可用时，才回退到 `GetCorrespondingObjectFromOriginalSource`，再通过 `AssetDatabase.GetAssetPath` 取得路径。
4. 只接受非空、以 `.prefab` 结尾、且不等于外层 Timeline Prefab 的路径。
5. 对解析出的资产再次 `LoadAssetAtPath<GameObject>` 校验，并用项目已有的所有权标识排除误指向外层主 Prefab 的情况。

```csharp
private static string ResolveNestedPrefabAssetPath(
    string nearestInstanceRootPath,
    string originalSourceAssetPath,
    string ownerAssetPath)
{
    if (IsUsableNestedPrefabAssetPath(nearestInstanceRootPath, ownerAssetPath))
        return nearestInstanceRootPath;

    return IsUsableNestedPrefabAssetPath(originalSourceAssetPath, ownerAssetPath)
        ? originalSourceAssetPath
        : string.Empty;
}

private static bool IsUsableNestedPrefabAssetPath(string assetPath, string ownerAssetPath)
{
    return !string.IsNullOrEmpty(assetPath) &&
           assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
           (string.IsNullOrEmpty(ownerAssetPath) ||
            !string.Equals(assetPath, ownerAssetPath, StringComparison.OrdinalIgnoreCase));
}
```

如果外层 Prefab 上挂有唯一的拥有者标识组件，可在加载源资产后再次判断该组件是否存在；存在则说明解析结果很可能是外层 Prefab，应直接拒绝保存。这个检查不能替代路径检查，但能覆盖预制体关系异常的情况。

### 3.2 克隆、解包、写回、重导、还原，顺序不能交换

嵌套源 Prefab 的写回推荐固定为下列序列：

1. 克隆活动嵌套实例。
2. 将克隆从原层级剥离；名称对齐源资产，避免 `SaveAsPrefabAsset` 产生额外命名噪声。
3. 只对克隆解包，解决“克隆根仍是即将被覆盖 Prefab 的实例”造成的自引用问题。
4. 用 `PrefabUtility.SaveAsPrefabAsset(clone, assetPath, out bool success)` 写回解析出的源路径。
5. 用 `ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate` 只导入该源 Prefab。
6. 重新定位原活动实例，并调用 `PrefabUtility.RevertPrefabInstance(liveInstance, InteractionMode.AutomatedAction)` 清除已经写入源资产的实例覆盖。
7. 在 `finally` 中销毁克隆，并让外层 Prefab 保持原状。

```csharp
GameObject clonedObject = UnityEngine.Object.Instantiate(instanceRoot);
clonedObject.name = sourceAsset.name;
clonedObject.transform.SetParent(null, false);
UnpackClonedPrefabRoot(clonedObject);

PrefabUtility.SaveAsPrefabAsset(clonedObject, assetPath, out bool success);
if (!success)
{
    throw new InvalidOperationException("Unity did not save the nested source prefab.");
}

AssetDatabase.ImportAsset(
    assetPath,
    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

PrefabUtility.RevertPrefabInstance(liveInstance, InteractionMode.AutomatedAction);
```

这里的“解包”只作用于克隆，不要对活动实例解包。活动实例需要保留 Prefab 关系，保存后才能通过 `RevertPrefabInstance` 清理覆盖。

### 3.3 不要用 `AssetDatabase.SaveAssets()` 保存嵌套 Prefab

当主 Timeline 实例或外层 Prefab 仍在打开状态时，`AssetDatabase.SaveAssets()` 可能把其它已脏资产一并落盘；这会违背“只保存嵌套源 Prefab”的用户意图。工具应只对目标路径调用 `AssetDatabase.ImportAsset`，并在结果文案中明确“外层 Timeline Prefab 未保存”。

如果需要让 Timeline 子资产先落盘，应单独识别 Timeline 子资产并显式保存其目标对象；不要把 `SaveAssets` 当作通用的“确认写入”步骤。

### 3.4 写入后必须清理活动实例覆盖

只写源 Prefab 而不清理活动实例覆盖，会出现两种常见结果：

- 层级中仍保留新增子对象或属性覆盖，视觉上像源 Prefab 内容出现两份。
- 白模、特效或子节点重复，关闭再打开 Prefab 才恢复正常。

正确理解是：写回源 Prefab 后，活动实例已经不需要继续保留这些覆盖。保存流程应在同步导入源 Prefab 之后，对活动实例调用 `RevertPrefabInstance`。整个回滚按“单次嵌套实例”为单位执行，不要对外层 Timeline 实例整体 `Apply` 或 `Revert`。

### 3.5 递归收集时排除外层根，并对重复源路径提前报错

批量保存嵌套 Prefab 时建议支持以下作用域：

| 选项 | 含义 | 实现要点 |
| --- | --- | --- |
| 保存当前 Prefab | 将承载组件所在 Prefab 保存回自身源资产 | Prefab Mode 使用 `SavePrefabAsset`；活动实例使用 `ApplyPrefabInstance`。 |
| 保存嵌套 Prefab | 只保存容器下的嵌套实例 | 必须排除外层 Prefab 根，不把主 Prefab 自己加入待写列表。 |
| 递归 | 连同更深层嵌套实例一起收集 | 按层级深度从深到浅排序，先写子级再写父级。 |
| 包含未激活 | 是否扫描未激活对象 | 作为明确的用户选项，不要隐式跳过或隐式包含。 |

多个活动实例可能指向同一个源 Prefab。如果工具逐个写回，后者会覆盖前者，造成“最后点击顺序决定内容”的不确定结果。收集到快照后应使用不区分大小写的路径集合提前检测重复路径；命中时报告所有冲突实例并跳过该源，不能静默覆盖。

```csharp
HashSet<string> assetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
if (!assetPaths.Add(snapshot.AssetPath))
{
    errors.Add(nestedRoot.name + ": multiple instances resolve to " + snapshot.AssetPath);
    UnityEngine.Object.DestroyImmediate(snapshot.GameObject);
    continue;
}
```

### 3.6 自身保存与嵌套保存要分开实现

“保存当前 Prefab”和“保存层级内嵌套 Prefab”是两个语义分支：

- Prefab Mode 中保存当前资产：`PrefabStageUtility.GetPrefabStage` 后调用 `PrefabUtility.SavePrefabAsset`。
- 活动场景实例中保存自身：解析最近实例根与路径，调用 `PrefabUtility.ApplyPrefabInstance`，再同步导入该路径。
- 保存嵌套 Prefab：按第 3.1 至 3.4 节的克隆写回流程处理。

不要把三者合并成一个“找最近根后统一 Apply”的分支。外层 Timeline Prefab 保存范围过大时，很可能因此覆盖或删除用户没有请求保存的内容。

### 3.7 将独立保存能力做成无运行时行为的通用组件

同一套逻辑可以抽成可手动挂到目标 Prefab 上的编辑器入口：

- 运行时组件只保存选项，不执行保存，也不持有场景对象或 AssetDatabase 引用。
- 自定义 Inspector 绘制范围选项与保存按钮；保存逻辑留在 Editor 程序集或在 `#if UNITY_EDITOR` 内实现。
- 选项至少覆盖 `saveSelf`、`saveNestedPrefabs`、`recursive`、`includeInactive`，默认值应让“只保存嵌套”为显式选择，而不是意外连带保存自身。
- 绑定组件挂在指定 Prefab 根或容器上时，容器本身就是嵌套保存范围；这样同一工具既能保存当前 Prefab，也能保存层级内的嵌套 Prefab。
- 无运行时逻辑的组件仍要遵守运行时程序集边界：Prefab 上存在组件，不代表运行时需要调用它；编辑器代码不得污染 HotFix 运行时程序集。

按钮文案必须与作用范围一致。只保存嵌套 Prefab 时，结果提示应明确“外层 Timeline Prefab 未保存”；不要使用“全部保存”之类可能让制作人员误判覆盖范围的文案。

### 3.8 生成工具写组件时通过 `SerializedObject` 写私有字段

生成器给主 Prefab 添加运行时组件时，不要假设组件公开了编辑器需要写入的私有序列化字段，也不要用反射绕过序列化。推荐：

1. `AddComponent<T>()` 或复用现有组件。
2. `new SerializedObject(component)`。
3. `FindProperty("<serializedFieldName>")`，命中不到时显式失败。
4. 写 `objectReferenceValue`、`enumValueIndex` 等值。
5. `ApplyModifiedPropertiesWithoutUndo()`。
6. `EditorUtility.SetDirty(component)`。
7. 如果使用 `LoadPrefabContents` / `SaveAsPrefabAsset` 修改 Prefab，完成后强制重载并验证磁盘资产。

字段不存在时应作为生成失败上报，而不是静默跳过。否则生成器会“成功”产出一个缺少关键引用的 Prefab。

### 3.9 自动写入内容必须按模式隔离

生成器写入组件、轨道或引用时，先按制作类型或镜头模式确定应存在的内容。一个模式新增的同步组件或轨道，不应在其它模式也留下。推荐的验证顺序是：

1. 确认当前模式需要的组件存在。
2. 确认组件字段值等于目标对象，而不是仅“非空”。
3. 确认目标对象属于保存后的 Prefab，而不是生成过程中的临时 `PrefabContents` 对象。
4. 确认非当前模式不应存在的组件、轨道或引用已经移除。
5. 确认运行时消费者能读取该字段，并做出与生成器声明一致的行为。

## 4. 常见问题与根因

| 现象 | 根因 | 解决方向 |
| --- | --- | --- |
| `ArgumentNullException: Parameter name: obj` | 把空对象或错误解析链传给 `GetCorrespondingObjectFromSource<T>` 等 Prefab API。 | 先校验实例根，再组合最近实例根路径与原始源回退；所有 API 调用前判空。 |
| 点“保存嵌套 Prefab”后外层 Timeline Prefab 被覆盖或主 Prefab 丢失 | 保存范围扩大到了外层实例根；使用了 `ApplyPrefabInstance` 或 `SaveAssets`。 | 克隆、解包克隆、只写嵌套源路径、只同步导入该资产，并在文案中明确外层未保存。 |
| 源 Prefab 写成功，但层级的嵌套实例没有更新 | 只写了磁盘资产，没有同步导入，或活动实例没有重新绑定。 | 强制同步导入源 Prefab，再重新定位活动实例并按其生命周期处理。 |
| 保存后层级出现两份相同对象 | 源 Prefab 已包含修改，但活动实例覆盖仍然存在。 | 写入并导入后调用 `RevertPrefabInstance`，清除已经落到源资产上的覆盖。 |
| 当前层级看起来重复，不确定能否忽略 | 重复来自尚未清理的实例覆盖；若覆盖进入了 Prefab 或场景资产，构建会读取落盘后的重复结构。 | 不能只凭“当前编辑器显示”判断对构建无影响；先清理覆盖并验证磁盘 Prefab/场景，再以构建产物验证。 |
| 多个子实例保存结果互相覆盖 | 多个活动实例解析到同一源 Prefab。 | 收集阶段用路径集合拒绝重复，列出全部冲突实例。 |
| 递归保存顺序不稳定 | 父子嵌套同时写入，父级先被覆盖。 | 按层级深度从深到浅排序，子级先写。 |
| 生成器写入字段成功，运行时仍拿不到引用 | 写错字段、写错对象、写了 `PrefabContents` 临时对象，或运行时模式不读取该字段。 | 用 `SerializedObject` 精确写字段，保存后从磁盘回读，并用真实运行时消费者验证。 |
| 非目标模式多出同步组件或轨道 | 生成逻辑没有按模式隔离，或历史结构没有清理。 | 每个模式声明独占结构，生成作用域内清理旧结构，并验证其它模式不残留。 |
| 生成预览看似正常，打包或 Player 无效 | 只验证了 Editor 预览路径，没有验证运行时消费者与构建产物。 | 把编辑器预览和运行时资产分别验证；运行时只保留运行时依赖。 |

## 5. 风险与不适用边界

| 边界 | 说明 |
| --- | --- |
| Prefab 关系异常 | 当嵌套关系、Packing Mode、导入状态或索引发生变化时，最近实例根路径可能不可用；必须保留原路径回退和拒绝策略，不能猜测目标。 |
| 同名源资产 | 只能通过完整资产路径区分。禁止只按文件名匹配源 Prefab。 |
| 人工覆盖与源资产的关系 | 本参考只定义“把当前实例内容写到指定源资产，并清理这批覆盖”的语义；更复杂的合并策略、字段白名单和冲突 UI 需要单独设计。 |
| 外层 Prefab 脏状态 | 工具不保存外层，不代表外层一定没有其它脏改；交付说明应明确本次工具未验证外层其它修改。 |
| 撤销 | Prefab 资产写入、同步导入和 `RevertPrefabInstance` 通常不具备完整 Undo。回退依赖版本管理或明确的备份。 |
| 递归保存 | 父级和子级都参与保存时，内容可能互相影响；没有明确的父子所有权时不应默认开启递归。 |
| Prefab Mode | 工具应明确支持“活动实例保存”还是“Prefab Mode 保存”。两者不能混用同一目标解析逻辑。 |
| 必须进入 Prefab Mode | 只保存嵌套源 Prefab 不需要打开该 Prefab；强制进入会扩大切换、导入和覆盖风险。工具应优先在活动层级中完成快照与写回。 |
| 生成器绑定 | 作为运行时入口的组件和字段名属于项目 Profile；生成器自动添加组件并不等于运行时链路已经完成，仍要由真实消费者验证。 |
| 取消或回退过的方案 | 不要因为一次试验曾把生成器改成某模式专用结构，就把它写成通用规则；文档要注明方案是否保留，避免后续实现误用。 |

## 6. 验证与回退

### 6.1 最小验证矩阵

| 层级 | 操作 | 通过条件 |
| --- | --- | --- |
| 源解析 | 选择普通嵌套实例、错误层级对象、Project 资产。 | 普通实例解析出嵌套源 Prefab；其它情况明确拒绝且不写资产。 |
| 只保存嵌套 | 在外层 Timeline Prefab 有未保存修改时点击保存嵌套 Prefab。 | 嵌套源 Prefab 更新；外层 Prefab 没有被保存、覆盖或删除。 |
| 多实例 | 容器内放多个嵌套实例。 | 每个源 Prefab 按预期更新，日志列出目标路径。 |
| 重复源 | 两个实例指向同一个源 Prefab。 | 写入前报冲突，不出现后者覆盖前者。 |
| 覆盖清理 | 在活动实例中新增子对象或改属性后保存，再关闭重开 Prefab。 | 层级只剩源 Prefab 的一份内容，关闭重开后结果一致。 |
| Prefab Mode | 在 Prefab Mode 中保存当前 Prefab。 | 走 `SavePrefabAsset` 分支，不误用场景实例分支。 |
| 嵌套深度 | 父子均参与递归保存。 | 更深层先写，最终层级和源资产一致。 |
| 生成组件 | 生成目标模式的主 Prefab。 | 目标组件存在，序列化字段指向正确 Prefab 内对象，其它模式无残留。 |
| 运行时消费 | 进入运行时或 Play 模式，实际消费生成字段。 | Track、Binding、Runtime Context 或相机输出按声明行为工作，而不是只看到字段非空。 |
| 构建边界 | 运行静态检查和对应运行时验证。 | Editor-only 代码不进入运行时；运行时程序集不引用 `UnityEditor`。 |

### 6.2 调试顺序

1. 先确认活动实例是否真的属于 Prefab，是否在有效场景中。
2. 打印最近实例根路径、原始源路径、外层 Prefab 路径，确认三者关系。
3. 确认写入路径是嵌套源 Prefab，而不是外层 Prefab。
4. 确认克隆已经解包；未解包时检查是否出现自引用。
5. 确认同步导入完成，再重新定位活动实例。
6. 确认 `RevertPrefabInstance` 调用的对象仍是待处理实例，而不是重新导入后失效的旧引用。
7. 对生成组件，用 `SerializedProperty` 回读字段值，再从磁盘 Prefab 重新加载并检查一次。
8. 最后验证运行时消费者；不要停在“Inspector 字段看起来正确”。

### 6.3 回退策略

- 源路径解析失败时，停止本次保存，保留活动实例与外层 Prefab。
- 批量保存中出现单个源冲突时，跳过该源，继续处理其它独立源，并明确列出失败项。
- 写入完成后如果定位不到原活动实例，报告“源 Prefab 已保存但实例覆盖未清理”，让用户通过重开 Prefab 或版本回退处理。
- 生成器修改不满足运行时合同时应回退生成入口或模式分支，而不是把错误字段留在资产中。
- 需要回滚已写 Prefab 时，优先使用版本管理恢复源资产；工具本身不应隐式重置用户未提交的其它改动。

### 6.4 交付检查清单

- [ ] 已区分活动实例、嵌套源 Prefab、外层 Prefab 和运行时消费者。
- [ ] 源路径解析使用最近实例根优先、原始源回退，并拒绝外层 Prefab。
- [ ] 保存嵌套 Prefab 使用克隆、解包克隆、`SaveAsPrefabAsset`、同步导入、`RevertPrefabInstance`。
- [ ] 未使用 `AssetDatabase.SaveAssets()` 连带保存外层 Prefab。
- [ ] 递归收集按深度排序，重复源路径提前阻止。
- [ ] 自身保存与嵌套保存走独立分支。
- [ ] 通用组件不包含运行时保存行为，编辑器入口和选项在 Inspector 中可见，文案与实际写入范围一致。
- [ ] 生成器通过 `SerializedObject` / `SerializedProperty` 写字段，保存后回读。
- [ ] 模式专属组件、轨道和引用有清理与非目标模式反向验证。
- [ ] 已确认层级中的重复对象没有以实例覆盖形式留在 Prefab 或场景资产中，并完成必要的构建侧验证。
- [ ] 已记录报告过的日期、对应提交、删除与保留的方案。
- [ ] 已明确区分本参考的项目案例与可迁移方法。
