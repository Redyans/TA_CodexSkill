<!-- BEGIN automation:ta date:2026-11-17 layer:PROFILE -->
# ProjectACG Shader 变体裁剪与 Android 菲涅尔 Profile｜2026-11-17

> 类型：PROFILE；项目：ProjectACG；记录指定会话中的 BaseLit/NBShader 统计、裁剪器改动和 Android 资源证据。本次日报没有读取项目源码、SVC 或执行 Unity/Android 构建。
> 关联参考：[Shader 变体白名单裁剪与移动端参数失效排查](../../../references/CodexDaily/Development_Reference_2026-11-17.md)；模块入口：[ShaderDevelopment](../../../README_Tech_URPShaderDevelopmentRules.md)。

## 1. 项目事实与现象

- `Valkyria/Scene/BaseLit` 的静态材质扫描报告涉及 431 个 `.mat`。SVC `ShaderVariantsCollect.shadervariants` 的一次读取包含 **334** 条 BaseLit 组合：PassType 8 为 5、PassType 11 为 1、PassType 13 为 328；其中 **276** 条含指定实时阴影关键字，理论剩余 58 条。`_MAIN_LIGHT_SHADOWS_SCREEN` 与 `_SHADOWS_SOFT_LOW/MEDIUM/HIGH` 在该次 BaseLit SVC 中未出现。
- `Effects/NBShader` 的一次读取包含 **369** 条：PassType 8 为 39、13 为 328、14 为 2。主要功能轴包括 `_FX_LIGHT_MODE_UNLIT`、`_MASKMAP_ON`、`_DISSOLVE`、`_NOISEMAP`、`_SOFTPARTICLES_ON`、`_FRESNEL`、`_VERTEX_OFFSET` 和透明混合；该次集合未发现 BaseLit 那组主光/附加光/软阴影关键字。
- 会话实现了按 Shader 精确匹配的“强制剔除关键字”配置，配置写入 `ShaderStrippingProfile.asset`，并在 `ShaderStrippingProfile.cs`、Inspector、`TEngineShaderVariantStripper` 和缓存指纹中接入。BaseLit 配置了 8 个实时阴影关键字；裁剪顺序为候选 → 强制剔除 → SVC 白名单 → 回退。
- Android 菲涅尔问题涉及 `CharacterDissolveEffect.cs`。会话报告确认脚本每帧把 `fresnelIntensity` 写入 `_CharaEffectFresnelParams.z`，但 Android 资源清单为 `2026-07-22`，Windows 清单为 `2026-11-17`，且 `GameLogic.dll.bytes` 为 `2026-05-16`；Android 清单缺少 `CharacterDynamicCameraDissolveResources` 和 `an_vfx_Trans_Dissove`。

## 2. 根因、修正与取舍

- BaseLit 不能把实时阴影关键字直接从源码删掉来实现“编辑器有、手机无”；当前裁剪器的白名单提前返回也使普通 `AssetsShaderExtraStripKeywordNames` 对 BaseLit 不生效。会话将规则放到白名单前，并约束回退不重新放回禁用组合。
- BaseLit 的 276/334 是 SVC 静态筛选，不是最终 Android 包体计数；URP 仍可能生成阴影贴图，ShadowCaster、其他 Shader 和自定义阴影仍需独立处理。
- 菲涅尔当前更直接的可疑根因是 Android 使用了旧资源清单/DLL，导致控制器找不到溶解资源并提前退出，而不是脚本字段本身没有写入。另有材质缺少 `_CharaEffectDissolveEnabled`、旧 Shader/材质和 GLES/Vulkan 变体兼容等待排查项。

## 3. 会话报告的验证证据

| 检查 | 会话报告 | 边界 |
| --- | --- | --- |
| SVC 统计 | BaseLit 334/276/58；NBShader 369，按 PassType 和关键字频次完成分析。 | 静态集合不等于目标平台最终编译。 |
| 裁剪器 | `git diff --check`、Profile YAML、关键字配置、Unity 资源/GUID 和禁止 `dynamic` 检查通过。 | 未完成 Android/iOS 打包。 |
| C# 构建 | `dotnet build` 被缺失 .NET Framework 4.7.1 reference assemblies/Unity 生成依赖阻断。 | 不能据此判定 Unity 编译失败或成功。 |
| Android 诊断 | 脚本属性链路和资源清单/DLL 时间差已报告。 | 未在 Android 真机验证静态强度、动画采样或 GLES/Vulkan。 |

## 4. 未验证项与回退

- BaseLit 需要重新收集/构建目标质量档，确认无阴影组合可被 URP 全局关键字正确匹配，并检查实际包体 Shader 变体和手机画面。
- NBShader 的低频关键字（如调试 Fresnel、距离淡出、色差、扭曲折射、Alpha Modulate）只能列为候选，不能按频次直接删除。
- Android 首先用 `FORCE_REGENERATE=true`、`SKIP_BUILD_ASSETS=false`，必要时 `FORCE_REBUILD_ASSETS=true`、`FORCE_REBUILD_SHADERS=true` 重新生成并检查清单/DLL，再用真机验证 `0/0.5/1` 强度和 GLES3/Vulkan。
- 若裁剪造成材质异常，关闭 BaseLit 强制规则并恢复此前构建缓存/SVC；若 Android 仍无菲涅尔，继续检查材质属性和平台 Shader 日志。

## 来源会话

- [01a1e8fa-53c6-7570-8796-c6ec61c5b961](../../../../../VA_Worlk_memory/sessions/01a1e8fa-53c6-7570-8796-c6ec61c5b961.md)
- [01a1ea3a-4be0-74a3-a981-0f1fe3627403](../../../../../VA_Worlk_memory/sessions/01a1ea3a-4be0-74a3-a981-0f1fe3627403.md)

<!-- END automation:ta date:2026-11-17 layer:PROFILE -->
