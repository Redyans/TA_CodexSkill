<!-- BEGIN automation:ta date:2026-11-17 layer:REFERENCE -->
# Shader 变体白名单裁剪与移动端参数失效排查参考｜2026-11-17

> 类型：REFERENCE；适用范围：分析 SVC/Shader 变体、按 Shader 强制剔除构建候选，以及排查 Android 上运行时材质参数不生效；使用前提：重新核对目标 Unity/URP、Shader 源码、资源包、平台编译日志和实际设备。
> 关联既有规则：`VAL-03`、`CTL-02`、`SHD-VAL-01`；模块入口：[ShaderDevelopment](../../README_Tech_URPShaderDevelopmentRules.md)。项目事实见 [ProjectACG Shader Profile](../../Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-17.md)。

## 1. 适用场景

- 需要从 `ShaderVariantCollection` 统计关键字组合，并决定哪些功能只能在 Player/AssetBundle 构建阶段裁剪。
- SVC 白名单已启用，但还需要按 Shader 名称强制排除某些关键字组合。
- 编辑器烘焙需要保留实时阴影变体，而移动端包体希望不包含 BaseLit 的实时阴影接收组合。
- Inspector/脚本参数在编辑器有效，Android 包中不生效，需要区分脚本写入、材质属性、Shader 变体、资源清单和平台编译问题。

## 2. 前提与依赖

- 先区分源码中的 `shader_feature`/`multi_compile`、SVC 条目、构建后实际变体和运行时全局关键字；一个数字不能代表全部层级。
- 记录 Shader 精确名、PassType、关键字完整组合、质量档/Renderer、平台和 SVC 采集来源。`PassType 13` 可能合并多个源码 Pass，不能仅凭枚举值命名。
- 强制剔除规则必须有精确 Shader 匹配、默认空配置、缓存指纹参与和“全部候选被剔除”的回退策略；白名单回退不能重新放回禁用组合。

## 3. 实现或排查步骤

1. 从 SVC 按 Shader GUID/名称提取条目，统计总数、PassType、关键字频次、目标关键字交集和无目标关键字组合；多个关键字出现在同一条时不能把频次相加。
2. 将强制剔除放在 SVC 精确匹配之前：候选包含指定关键字则删除整条组合；未包含的候选再进入白名单匹配。不要从组合 A 中“删掉关键字后自动变成组合 B”。
3. 保留必要的无关键字状态 `_`，并确认 ShadowCaster、Meta、Lightmap、Shadowmask 等烘焙/投射路径是否仍有独立需求。若只想移动端无实时接收阴影，优先保留源码声明、只在目标构建剔除。
4. 对低频关键字先检查材质序列化、运行时动态赋值、外部资源包和不同质量档；低频不等于可直接删除。删除源码 Pass 或关键字前，分别验证编辑器、烘焙、Player 和设备。
5. Android 参数失效时沿链路检查：脚本字段是否被曲线采样、材质是否有属性、关键字/分支是否在平台变体中、资源包/清单/DLL 是否为当前版本、GLES/Vulkan 编译日志是否有 fatal error。对照静态强度 `0/0.5/1` 与目标图形 API。
6. 对移动资源产物记录时间和包含项。若 Android 清单或 DLL 明显早于当前 Windows 产物，先修复构建输入/强制重建，再讨论 Shader 数值或裁剪。

## 4. 风险与不适用边界

- 直接从 Shader 源码删除实时阴影变体会同时影响编辑器实时预览和依赖该变体的工具；这不等于只影响手机包。
- 裁剪 BaseLit 的接收阴影变体不会自动停止 ShadowCaster、其他 Shader 的阴影或项目自定义阴影，也不一定减少阴影贴图生成 GPU 开销。
- SVC 静态条目数量不是最终 Player 编译数量；平台、质量档、Graphics API 和回退逻辑都可能改变结果。
- “脚本每帧写入参数”和“设备画面变化”之间还隔着资源包版本、材质属性存在性、平台变体和动画采样；未做 Android 构建/真机验证时不能定根因。

## 5. 验证与回退

- 构建前输出候选总数、强制剔除数、剩余无关键字组合和回退警告；构建后读取实际平台变体报告和 Shader 编译日志。
- 设备上分别验证 Lightmap、Shadowmask、实时主光/附加光阴影、ShadowCaster 投射、自定义阴影和目标材质参数；编辑器烘焙与 Player 画面分开验收。
- 若材质变粉、参数失效或回退命中禁用组合，关闭规则/恢复 SVC 和 Shader 源码，重新构建干净包；不要只改运行时全局关键字掩盖缺失变体。

## 来源与证据边界

- [BaseLit/NBShader 变体与裁剪会话](../../../../VA_Worlk_memory/sessions/01a1e8fa-53c6-7570-8796-c6ec61c5b961.md)
- [CharacterDissolveEffect Android 菲涅尔会话](../../../../VA_Worlk_memory/sessions/01a1ea3a-4be0-74a3-a981-0f1fe3627403.md)

本参考中的数量、构建顺序和资源时间差均来自会话报告；本次日报没有重新读取 SVC、运行 Unity 构建或连接 Android 设备。
<!-- END automation:ta date:2026-11-17 layer:REFERENCE -->
