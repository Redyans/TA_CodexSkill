<!-- BEGIN automation:ta date:2026-11-12 layer:PROFILE -->
# ProjectACG Timeline 变更 Profile｜2026-11-12

> 类型：PROFILE；项目：ProjectACG；记录当日角色 Timeline Track 与大招预览/运行问题。
> 关联参考：[Timeline 材质替换与动态绑定](../../../references/CodexDaily/Development_Reference_2026-11-12.md)；沿用 `TML-DOC-01`、`TML-ARC-03`、`TML-LIF-02`、`TML-VAL-01`。

## 1. 项目事实与现象

### Character Material Swap Track

- 原有角色材质轨控制 Shader 参数，不能替换整个材质；新增 `Character Material Swap Track + Clip`，复用 `ICharacterTimelineBindingTrack` / `TimelineBindingResolver` 角色绑定契约。
- Clip 配置一个替换材质组后，可添加多个 Mesh 及材质槽；同一 Mesh 多槽合并一行显示并支持选择多个槽。Clip 结束、停止或退出预览时恢复原材质，不修改材质资产。
- 同一帧按 Renderer 聚合：路径/槽数读取复用，变化后才写回；直接替换 shared material 引用，不调用 `.material` 创建实例。
- 普通显式/自动绑定和动态重绑的隔离测试曾报告 12/12 通过。但用户实际大招播放时没有生效；根因定位为主 Timeline 被 `SceneTimelineAnimationTransition` 包装，FX 子 Timeline 的自动查找只识别原生 `TimelineAsset`，因此看不到外层已绑定角色。
- 后续修改为包装资产暴露原始 Timeline，并更新角色发现、嵌套 Director 查找及动态 Bind/Unbind。`GameLogic.csproj` 编译与 `git diff --check` 报告通过；包装场景对应 EditMode 测试及真实演出播放未复验。
- 绑定模式建议使用“自动获取角色”，并移除 Track 上残留的预览角色手动绑定；Mesh 路径和槽位必须匹配运行时模型。若其他代码写入被多 Mesh 共用的材质属性，可能影响全部 Renderer。
- 没有 Profiler 基准；会话报告的逐帧缓存和批量写回是实现检查，不是实测耗时结论。

### 大招武器可见性与角色初始化

- `UltimateTimelineBindings.HideCasterWeapon` 一度暴露 Inspector，默认 `true`，并能控制大招演出中隐藏战斗武器；用户随后要求关闭且不显示，最终改为 `HideCasterWeapon => false`，旧 Prefab 序列化开启值也不再生效。
- 最终修改报告为编译通过但实际播放未测。提交功能时除运行时代码和 Inspector 外，还需包含同步更新的 `UltimateTimelinePreviewAndWeaponTests.cs`，旧测试曾反射访问已移除字段；会话给出的完整提交范围为 4 个文件。
- `100901` 大招 Prefab 的 `cPre_hero_100901_battle_77` 报初始化异常：`lowMeshRoot` 为空，`highMeshRoot` 有值，`MeshAll` 下只有高模。该运行时模型要求低模/高模不同且为 `MeshAll` 直接子节点。异常在 `PlayerWeaponAimRigController.Awake()`，早于读取隐藏武器设置；本日只定位，没有修 Prefab。

## 2. 根因、修正与证据

- **材质轨在实际演出不生效**：测试覆盖了直接 Timeline 绑定，但没有覆盖包装后的 Transition Asset 与 FX 子 Director 查找路径。增加包装资产解包和嵌套搜索后代码通过编译；最终播放仍待证。
- **武器开关与旧资产状态不一致**：运行时旧方案读取 Prefab 序列化字段；最终用户需求改成固定关闭，使用恒定返回值使旧字段值无效，并删除 Inspector 控件。
- **大招初始化失败**：缺少低模根引用和层级结构不符合角色运行时契约；该错误独立于隐藏武器控制。

## 3. 项目约束与候选

- 材质组可以表达“一材质对多个 Mesh/槽”，但材质属性仍共享；若需要各 Renderer 独立参数，应另行设计局部参数写入，不把 shared material 替换误认为独立实例。
- 自动绑定、手动绑定和包装 Timeline 的优先级必须跟项目真实动态绑定入口一致；静态测试通过不能替代演出链路验证。
- `HideCasterWeapon` 当前会话最终状态是硬编码关闭；若以后需要重新开放，应作为独立运行时接口与 Prefab 迁移变更处理。
- 这里的 Track、字段、Prefabs 和角色层级约定均为 ProjectACG Profile 内容，不升级为 CORE。

## 4. 验证状态与回退

- **已报告**：Track 普通绑定隔离测试 12/12；包装绑定修复的运行时代码编译与格式检查；武器开关修改的编译检查。
- **未验证**：包装 Timeline 的 Unity EditMode、真实大招材质替换、目标设备性能、武器隐藏最终表现、`100901` Prefab 结构修复。
- **回退**：Track 不生效时先移除手动绑定并核对动态 Binding/嵌套 Director；必要时禁用 Clip 并恢复保存的材质数组。角色初始化失败时修复低/高模引用或使用不含战斗控制组件的预览角色；这与材质 Track 无关。

来源：[01a1cf40-ee85-79b3-af3a-15bf06a6a14d](../../../../../VA_Worlk_memory/sessions/01a1cf40-ee85-79b3-af3a-15bf06a6a14d.md)、[01a1cec6-dec3-7260-9d98-b069c5285e29](../../../../../VA_Worlk_memory/sessions/01a1cec6-dec3-7260-9d98-b069c5285e29.md)。
<!-- END automation:ta date:2026-11-12 layer:PROFILE -->
