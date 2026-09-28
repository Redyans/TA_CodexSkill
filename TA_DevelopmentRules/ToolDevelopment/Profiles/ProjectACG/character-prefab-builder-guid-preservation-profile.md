---
name: projectacg-character-prefab-builder-guid-preservation-profile
description: ProjectACG CharacterPrefabBuilder 的 Prefab GUID 保持、删除后重建、登记资产、实现路径、静态验证和 Unity 验证边界 Profile。
---

# ProjectACG CharacterPrefabBuilder GUID 保持 Profile

> 类型：PROFILE；适用范围：ProjectACG `Client` 工作区的 `CharacterPrefabBuilder`；通用实现模式见 [Unity 角色 Prefab 生成器开发经验参考](../../references/character-prefab-builder-development-patterns.md)。本 Profile 记录当前项目的具体路径、实现和验证状态，不应直接当作其他项目的通用规则。

## 1. 项目事实与入口

| 项目项 | 当前事实 |
| --- | --- |
| Unity 基线 | `2022.3.62f3`。 |
| 工具目录 | `Assets/Editor/TA_Tools/Character/CharacterPrefabBuilder/`。 |
| 生成器入口 | `CharacterFbxPrefabBuilderWindow.cs`。 |
| 生成器菜单 | `TA_Tools/Character/角色FBX生成Prefab`。 |
| 重新生成菜单 | `Assets/Prefab生成/重新生成Prefab`。 |
| 更新菜单 | `Assets/Prefab生成/更新已有Prefab`。 |
| Timeline 生成 | `Assets/Prefab生成/生成大招Timeline专用Prefab`、`Assets/Prefab生成/生成剧情Timeline专用Prefab`。 |
| Editor 程序集 | `TA_Tools.CharacterPrefabBuilder.Editor`。 |
| 当前工作区 | `D:\work2025U3D\Valkyria\ProjectACG\Client`。 |
| 角色资源根 | `Assets/AssetRaw/character/`、`Assets/AssetArt/character/`。 |

完整的生成、材质槽、LOD、ModelImporter 和模块快照约束仍分别以以下 Profile 为准：

- [CharacterPrefabBuilder 生成与材质槽同步](character-prefab-builder-generation-and-material-sync.md)
- [CharacterPrefabBuilder ModelImporter 预设](character-prefab-model-importer-preset.md)
- [Character Prefab 模块快照与恢复](character-prefab-module-snapshot-and-restore.md)

## 2. 项目问题与根因

### 2.1 现象

工具原来在生成分支中直接调用 `PrefabUtility.SaveAsPrefabAsset`。当目标 Prefab 仍存在时，同路径保存通常会沿用原 `.meta`，因此 GUID 不变；但用户先在 Unity Project 窗口删除 Prefab，再重新生成时，Unity 也会删除 `.meta`，新文件会获得新 GUID。

这会影响：

- 场景、Timeline、配置资产和脚本中的 Prefab 引用；
- 以 GUID 作为稳定身份的模块快照、层级对象和外部资源引用；
- 跨分支合并、资源包收集和增量构建的资产身份判断；
- 需要把“删除后再生成”当作内容重建而不是新资产创建的美术工作流。

### 2.2 根因

文件名、Prefab 名称和输出路径不能恢复旧 GUID。旧 GUID 唯一存放在原 `.meta` 或其他可靠登记中；一旦 `.meta` 和登记信息同时丢失，Unity 没有可推导的旧身份。

因此问题不能通过“再次使用同名文件”解决，必须在工具层保存路径到 GUID 的关系，并在写入前恢复 `.meta`。

## 3. 当前实现

### 3.1 新增资产身份登记

当前新增：

- `CharacterPrefabGuidPreservation.cs`：统一 Prefab 保存和 GUID 检查。
- `CharacterPrefabGuidRegistryAsset.cs`：定义可序列化的登记资产类型。
- `PrefabBuildGuidRegistry.asset`：登记角色 Prefab 路径到 GUID 的映射。
- 对应 `.meta` 文件：必须和脚本、登记资产一起进入版本控制。

登记资产当前路径：

```text
Assets/Editor/TA_Tools/Character/CharacterPrefabBuilder/PrefabBuildGuidRegistry.asset
```

登记项结构：

```text
PrefabPath: Assets/AssetRaw/character/.../some.prefab
Guid:       32 位十六进制 GUID
```

初始登记覆盖当前 `Assets/AssetRaw/character/` 与 `Assets/AssetArt/character/` 下的 234 个 Prefab；静态检查确认 234 个路径、234 个 GUID、无重复 GUID，且登记值与当前 `.meta` 全部一致。

### 3.2 统一保存入口

`CharacterFbxPrefabBuilderWindow.cs` 的以下保存分支已经改为统一调用 `CharacterPrefabGuidPreservation.SaveAsPrefabAsset`：

1. Model FBX 全量刷新；
2. Model FBX 保留已有并补充；
3. Prefab From Step 全量刷新；
4. Prefab From Step 保留已有并补充；
5. Timeline 专用 Prefab 创建或更新。

构建逻辑负责组装内存中的 Prefab，GUID 适配层负责资产保存、`.meta` 准备、导入和最终验证，避免某个分支绕过保护逻辑。

### 3.3 保存流程

当前保存适配层按以下顺序处理：

1. 规范化目标路径，要求位于 `Assets/` 下。
2. 读取当前路径的 GUID；若存在，写入登记表。
3. 若当前资产不存在但 `.meta` 仍存在，读取 `.meta` 中的 GUID 并登记。
4. 若 `.meta` 也不存在，查询 `PrefabBuildGuidRegistry.asset`。
5. 找到登记 GUID 时，先用 `AssetDatabase.GUIDToAssetPath` 检查 GUID 是否被其他资源占用。
6. GUID 未被占用时，在目标 Prefab 保存前恢复目标 `.meta` 的 GUID。
7. 调用 `PrefabUtility.SaveAsPrefabAsset`。
8. 使用 `AssetDatabase.ImportAsset` 强制导入并回读 `AssetDatabase.AssetPathToGUID`。
9. 期望 GUID 与实际 GUID 不一致时停止并报错；当前代码保留一次删除目标后重试的保护分支，但不接受静默换 GUID。
10. 保存成功后更新登记表。

### 3.4 新资产与旧资产行为

| 情况 | 当前行为 |
| --- | --- |
| 目标 Prefab 存在且 `.meta` 正常 | 沿用当前 GUID，并刷新登记表。 |
| 目标文件不存在但 `.meta` 仍在 | 读取 `.meta` GUID，沿用并登记。 |
| 目标文件和 `.meta` 都不存在，但登记表有记录 | 恢复 `.meta` GUID 后生成，并回读确认。 |
| 目标文件和 `.meta` 都不存在，登记表无记录 | 让 Unity 生成新 GUID，成功后登记。 |
| 登记 GUID 已被其他资源占用 | 阻断保存，不覆盖其他资源的 `.meta`。 |
| 保存后实际 GUID 与期望值不一致 | 报错，不把结果标记为成功。 |

## 4. 开发制作经验

### 4.1 保存逻辑必须集中

Prefab 生成工具很容易在 Model、LOD、Timeline、Prefab From Step 等分支中出现多个保存调用。只在主分支加 GUID 保护是不够的；任何遗漏的保存旁路都会让行为不一致。

维护时先搜索全部 `SaveAsPrefabAsset` 调用，再把每一个路径分支归类。新分支必须复用保存适配器，不能直接调用 Unity API。

### 4.2 GUID 是身份，不是内容

GUID 保持只能说明资产身份延续，不代表生成内容、组件、层级、材质、外部引用或第三方缓存自动正确。重新生成仍需执行：

- 内容刷新或增量策略；
- 手工模块快照与恢复；
- 外部资源 GUID/local fileID 检查；
- Prefab 重开与重新导入；
- Timeline、场景、运行时消费者验证。

不要把“GUID 一样”当作“Prefab 完整兼容”。

### 4.3 登记资产本身也是生产资产

登记表不是临时缓存。它必须：

- 放在稳定的工程路径；
- 与 `.meta` 一起进入版本控制；
- 使用明确的可序列化类型；
- 检查重复路径和重复 GUID；
- 在分支合并后重新校验；
- 在资产迁移或路径变更时同步更新。

如果登记表放在 `Library`、临时目录或被忽略的生成目录，删除后重建保护无法在其他机器或分支上复现。

### 4.4 `.meta` 修改必须保持最小范围

只恢复目标资产所需的 GUID 和合法导入器头，不要从另一个 Prefab 复制完整 `.meta`。不同资产的 importer 设置、外部对象映射和用户数据可能不同，复制整份 `.meta` 会产生隐蔽的导入问题。

当前实现采用最小 `PrefabImporter` 结构作为恢复内容；在目标 Unity 编辑器内仍需验证不同 PrefabImporter 选项是否需要额外保留。若发现目标 `.meta` 有必须保留的字段，应改为“读取旧 `.meta`、只替换 GUID、保留其他字段”的策略。

### 4.5 不要用删除重试掩盖身份错误

保存后的 GUID 不一致时，工具可以在确认目标只属于当前构建、且登记 GUID 未被占用的情况下重试。但删除重试必须：

- 只作用于本次生成目标；
- 记录删除前后的路径和 GUID；
- 失败时抛出明确错误；
- 不删除源 FBX、源 Prefab、登记资产或其他资源；
- 在真实 Unity 测试中确认不会丢失目标 PrefabImporter 配置。

如果不能证明这些条件，应该直接阻断，而不是自动删除并继续。

## 5. 问题、解决方式与边界

| 问题 | 根因 | 当前解决方式 | 仍需注意 |
| --- | --- | --- | --- |
| 同路径覆盖 GUID 不变但删除后变化 | 删除了 `.meta` | 登记表保存路径到 GUID，重建前恢复 `.meta`。 | 登记表和 `.meta` 必须提交。 |
| 新增脚本无法读取登记资产 | 私有嵌套 `ScriptableObject` 类型不适合作为长期 YAML 资产契约 | 抽出公开 `CharacterPrefabGuidRegistryAsset` 与 `CharacterPrefabGuidRegistryEntry`。 | Unity 导入后需检查脚本 GUID 和 YAML 反序列化。 |
| 登记 GUID 与其他资源冲突 | 资产移动、分支合并或手工复制 `.meta` | 恢复前 `GUIDToAssetPath` 冲突检查，冲突即阻断。 | 不允许覆盖冲突资源。 |
| 只检查内存 Prefab 导致假成功 | 保存/导入后身份可能变化 | 保存后强制导入，再读取实际 GUID。 | 仍需在 Unity 编辑器中做真实回归。 |
| 其他生成分支绕过保护 | 多个直接 `SaveAsPrefabAsset` 入口 | 统一替换 5 个已知保存入口。 | 后续新增入口必须再次全量搜索。 |
| 手工内容仍然丢失 | GUID 只保护资产身份，不迁移内容 | 使用刷新/增量策略和模块快照恢复。 | 需按组件和层级映射验证。 |
| 登记表记录过时 | 资产路径移动或手工替换 | 生成后更新登记表，静态扫描路径与 `.meta`。 | 路径迁移需要专门工具或人工同步。 |
| Unity 尚未真实导入验证 | 当前环境无可用 Unity Editor | 已完成文本、路径、GUID 唯一性和登记一致性检查。 | 需在目标 Unity 中执行编译和删除重建回归。 |

## 6. 验证证据与未验证项

### 6.1 当前已完成

- `PrefabBuildGuidRegistry.asset` 登记 234 个角色 Prefab。
- 登记路径数与 GUID 数均为 234。
- 登记 GUID 无重复。
- 登记 GUID 与当前 234 个 `.meta` 值全部一致。
- 所有已知 Prefab 保存入口已替换为统一适配器。
- 相关文件通过 `git diff --check` 静态检查。
- 文档按 `REFERENCE` 与 `PROFILE` 分层，未把项目路径、版本和当前资产数量写入通用参考文档。

### 6.2 当前未完成

当前会话没有可用的 Unity Editor 可执行文件，因此以下内容尚未在宿主内闭环：

- `TA_Tools.CharacterPrefabBuilder.Editor` 真实 Domain Reload 和编译；
- 登记资产的 Unity YAML 反序列化；
- 删除 Prefab 和 `.meta` 后重新生成并回读 GUID；
- GUID 冲突时的阻断行为；
- 保存重试分支对 PrefabImporter 字段和引用的影响；
- Timeline、场景和资源包消费者对恢复后 GUID 的实际读取。

在上述验证完成前，该功能应视为“静态实现完成、宿主行为待验”，不能把文档中的流程当作已通过生产回归。

## 7. 维护清单

- [ ] 新增 Prefab 保存分支前搜索全部 `SaveAsPrefabAsset` 调用。
- [ ] 所有保存分支统一调用 GUID 保存适配器。
- [ ] 登记表路径、`.meta` 和脚本 GUID 均可解析。
- [ ] 登记表没有重复路径或重复 GUID。
- [ ] 登记表与当前目标 `.meta` 一致，路径变更已同步处理。
- [ ] 删除后重建测试覆盖文件保留、`.meta` 删除、登记表恢复、新资产和 GUID 冲突。
- [ ] 保存后重新导入并读取真实 GUID，不只检查内存对象。
- [ ] GUID 保持测试与内容迁移、材质槽、LOD、模块快照测试分开记录。
- [ ] 失败时不覆盖其他资源、不删除源资产、不静默生成新 GUID。
- [ ] 生产交付时提交登记资产及其 `.meta`，并记录 Unity 实测结果。
