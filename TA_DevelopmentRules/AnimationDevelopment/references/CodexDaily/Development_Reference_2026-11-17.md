<!-- BEGIN automation:ta date:2026-11-17 layer:REFERENCE -->
# Unity 动画导入压缩与 Editor 自动处理参考｜2026-11-17

> 类型：REFERENCE；适用范围：把 FBX 导入参数、导入后独立 `.anim` 处理和编辑器自动队列拆开设计；使用前提：按目标 Unity 版本、导入事务、动画消费者和回退策略重新验证。
> 关联既有规则：`ANM-CORE-06`、`ANM-CORE-07`；模块入口：[AnimationDevelopment](../../README_Tech_TAAnimationDevelopmentRules.md)。项目事实见 [ProjectACG 动画压缩 Profile](../../Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-17.md)。

## 1. 适用场景

- 需要在 FBX 导入时统一设置原生动画压缩、误差和重新采样。
- 需要在导入完成后处理匹配的 standalone `.anim`，但不应在 `AssetPostprocessor` 中再次修改 FBX Importer。
- Unity Editor 自动队列长时间占用主线程，既要能立即停止后续任务，又要保留手动扫描和候选应用。

## 2. 前提与依赖

- 明确 `OnPreprocessModel`/`OnPreprocessAnimation` 与 `OnPostprocessAllAssets` 的事务边界；FBX Importer 参数应在导入前设置，独立 `.anim` 曲线处理应在导入后进行。
- 区分 Unity 原生 FBX 压缩与 standalone `.anim` 曲线减帧；后者要保留原路径、GUID、帧率、绑定和可恢复备份。
- 自动队列必须有持久开关、暂停状态、备份报告、任务进度和失败恢复；不要让同步重导入成为不可中断的大步骤。

## 3. 实现或排查步骤

1. 在导入前应用 `Animation Compression`、Rotation/Position/Scale Error、`resampleCurves`、循环和重采样配置；把这些字段放入统一的 Model Import Rule Preset 或导入设置服务。
2. 导入完成后再同步已存在的 `.anim`，避免在 `OnPostprocessAllAssets` 中修改 `ModelImporter` 触发重复导入；缺失的 standalone `.anim` 不应被静默创建。
3. 对 `.anim` 曲线先生成候选，再由用户确认应用；应用前备份，保留不支持或不满足误差条件的曲线。
4. 自动处理开关关闭时，停用导入回调和独立动画队列，但保留窗口的手动扫描、压缩和候选应用；已有队列数据按项目策略保留或明确清理。
5. 把耗时的采样、验证、写回和重导入拆成跨帧小步；取消检查只能位于可让出主线程的边界，不能宣称能中断已进入的同步导入调用。
6. 验证配置文件、预设快照、Importer 回读、独立 `.anim` 备份和代表动画播放，分别记录静态编译、Unity 导入和实际效果证据。

## 4. 风险与不适用边界

- 只关闭“自动应用”不会停止候选生成、采样和资源刷新；要停止占用主线程的后续任务必须关闭自动处理入口。
- Unity 界面卡住时，编辑器内按钮不能可靠中断同步任务；强制结束进程可能丢失编辑器改动，应先检查备份和队列报告。
- `AnimationClip` 的 standalone 资产与 FBX 内嵌子资源不是同一种保存合同，不能直接复用同一套 `AssetDatabase.SaveAssetIfDirty` 流程。
- 静态字段检查或 `.csproj` 编译不证明 Unity 实际重新导入、动画播放和包体收益。

## 5. 验证与回退

- 配置修改后检查 JSON/预设快照差异，再在目标 Unity Editor 中重新导入一个代表 FBX，回读 `resampleCurves` 与压缩字段。
- 自动处理要记录队列规模、当前任务、已写回数、备份位置和取消时点；关闭自动处理后重启 Editor，确认内存状态也已重新加载。
- 发现动画变化时，从备份或版本控制恢复原 `.anim`/FBX，再用手动候选流程重跑；不要用“恢复默认设置”代替资源回退。

## 来源与证据边界

- [ProjectACGMain2 动画导入压缩会话](../../../../VA_Worlk_memory/sessions/01a1e781-5896-78e1-855d-90617274c69f.md)
- [ProjectACGMain2 动画自动队列阻塞会话](../../../../VA_Worlk_memory/sessions/01a1e772-1ba0-7302-9e0c-8f66b80dd746.md)

队列规模、配置字段和编译结果均为会话报告；本次日报没有重新运行 Unity 导入或播放。
<!-- END automation:ta date:2026-11-17 layer:REFERENCE -->
