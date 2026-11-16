<!-- BEGIN automation:ta date:2026-11-12 layer:REFERENCE -->
# Timeline 材质替换与动态绑定参考｜2026-11-12

> 类型：REFERENCE；适用范围：按运行时角色/Renderer/材质槽替换材质引用的 Timeline Track；使用前提：重新确认目标 Timeline 资产类型、绑定策略、材质共享范围和 Clip 生命周期。
> 关联既有规则：`TML-DOC-01`、`TML-ARC-03`、`TML-LIF-02`、`TML-VAL-01`；ProjectACG 实现见 [Timeline 变更 Profile](../../Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-12.md)。

## 1. 适用场景

- 演出期间需要整材质替换，而既有 Track 只控制材质 Shader 参数。
- 一个目标材质要应用到多个 Mesh 或同一 Mesh 的多个材质槽。
- 运行时角色动态实例化/重绑，Track 需跟随角色变化，结束时恢复原材质。
- 需要在预览与运行时替换材质，同时避免创建材质实例或修改共享材质资产。

## 2. 前提与依赖

- 明确目标是替换“材质引用”还是写 Shader 属性；二者使用不同写入边界。整材质替换不应借助共享材质参数或 `MaterialPropertyBlock` 冒充资源替换。
- 冻结绑定方式：显式 Track Binding、Timeline 绑定解析器或已有角色动画 Track 的 AutoResolve。动态实例必须能从实际主/子 Timeline 结构找到当前角色。
- 建立 Renderer、Mesh 相对路径、材质槽集合和 Clip 时间范围的稳定合同；不同 LOD/皮肤若层级或槽位不同需单独配置。
- 使用 shared-material 引用时必须保存每个目标的原始数组，并定义并行 Clip 对同一 Renderer/槽的仲裁和恢复行为。

## 3. 实现或排查步骤

1. 新增明确职责的 Track、Clip 与 Mixer；Inspector 允许一个材质组指向多 Mesh，每个 Mesh 选择多个槽，减少一个槽一组造成的重复 UI。
2. 绑定优先于自动搜索。对包装的主 Timeline、嵌套 Director 和 FX 子 Timeline 递归解析实际角色；仅通过 `TrackAsset` 名称查找外层绑定可能漏掉包装资产。
3. 同一帧按 Renderer 聚合目标槽：每个 Mesh 路径只解析一次、每个 Renderer 只读取一次材质数组；合并写入后只在槽位确实变化时写回一次。
4. 用 `GetSharedMaterials/SetSharedMaterials` 或目标版本等价接口替换引用，避免读写 `.material` 触发实例化；保存原引用并在 Clip 退出、Stop、解绑和预览退出时恢复。
5. 明确重叠 Clip 的胜出方式，并保证失效 Mesh 路径、越界材质槽和丢失角色不会静默写错对象。动态重绑后旧角色恢复、新角色重新解析。
6. 隔离测试显式绑定、AutoResolve、包装 Timeline、嵌套 Director、动态重绑、重叠槽、材质数组写回和每种退出路径；最后在真实演出中验证。

## 4. 风险与不适用边界

- Track 通过测试并不证明生产动态绑定入口会绑定它；外层包装资产或子 Director 可能使解析器看不到角色。
- 共享材质引用不会创建新实例，但多个 Renderer 会共用同一资源；其他代码对材质属性的写入可能影响全部使用者。
- Mesh 路径与材质槽配置可能只适用于某个 LOD/皮肤；运行时找不到目标时必须给出诊断，不可假定全角色结构相同。
- 多轨同时写同一槽时，若未定义仲裁，结果可能取决于求值次序。
- 缓存 Renderer 和数组可减少查询/分配，但没有目标场景 Profiler 数据时不能声称具体耗时或性能提升。

## 5. 验证与回退

在同一 Director 上覆盖手动/自动绑定、动态加载、主 Timeline 包装、FX 子 Director、多个 Mesh/槽、多个重叠 Clip、Stop、Pause、解绑和重新播放。检查材质数组逐槽结果、材质实例数量及 Clip 结束后的原引用恢复。异常时禁用新 Track 或回退原材质数组快照；Timeline 编译通过但实际播放未验时应明确保留待测状态。

来源：[01a1cf40-ee85-79b3-af3a-15bf06a6a14d](../../../../VA_Worlk_memory/sessions/01a1cf40-ee85-79b3-af3a-15bf06a6a14d.md)。
<!-- END automation:ta date:2026-11-12 layer:REFERENCE -->
