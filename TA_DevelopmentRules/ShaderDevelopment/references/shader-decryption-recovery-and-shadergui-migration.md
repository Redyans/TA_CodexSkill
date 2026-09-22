---
name: shader-decryption-recovery-and-shadergui-migration
description: Unity 加密代理 Shader 解密还原后，继续反编译原 ShaderGUI、恢复 Inspector 与 Keyword 联动，并整理跨工程迁移文件闭包的方法、陷阱和验证清单。
---

# Shader 解密恢复、ShaderGUI 还原与跨工程迁移参考

> 类型：REFERENCE；适用范围：已有工程自带解密能力的 Unity Shader 解密还原、原 `CustomEditor` 行为恢复、材质 Inspector 保真和单 Shader 跨工程迁移；使用前提：必须以目标工程的 Unity/URP 版本、Shader 源码、Editor 程序集、材质状态和实际图形 API 重新验证。
>
> 关联规则：`DOC-03`、`CMP-01`、`ORG-02`、`ORG-03`、`CTL-02`、`VAL-01`、`VAL-03`。解密容器与独立明文 Shader 的完整流程见 [encrypted-shader-recovery-workflow.md](encrypted-shader-recovery-workflow.md) 和 [encrypted-proxy-shader-restoration.md](encrypted-proxy-shader-restoration.md)，C# 离线编译见 [Unity Editor 脚本离线编译校验参考](../../ToolDevelopment/references/unity-editor-script-offline-compile-verification.md)，工具跨工程迁移见 [Unity 工具集跨工程迁移与旧件下线](../../ToolDevelopment/references/unity-editor-tool-cross-project-migration.md)。

## 1. 适用场景

- 加密 Shader 已经能够解密或提取出明文源码，但仍看不到原来完整的材质 Inspector。
- 明文 Shader 的 `CustomEditor` 指向工程私有 Editor 程序集，目标工程没有该程序集或类型。
- 迁移后出现“某个参数没有还原”“原面板有开关、新面板没有”“修改开关但渲染不变”等现象。
- 需要判断缺失的是 Shader 属性、HLSL 输入、Keyword、ShaderGUI 状态同步，还是材质序列化数据。
- 需要把一个独立 Shader 和它的 ShaderGUI 复制到另一个 Unity 工程，并明确最小文件闭包、GUID 处理和验证边界。

不适用于：没有授权或工具来源不可信的加密数据；仅凭 Properties 猜测并重写商业 Shader；需要修改 URP Package 或复刻完整编辑器框架的任务。此类任务应停在上游授权、依赖或架构评审。

## 2. 前提与依赖

| 依赖 | 必须确认的内容 |
| --- | --- |
| 授权 | 用户或项目负责人允许读取、解密、保存和迁移目标源码与编辑器代码。 |
| 工具链 | 加密容器格式、项目自带解密入口、托管 DLL、原生插件和版本匹配关系已经确认。 |
| Unity/URP | 目标工程的 Unity、URP、Core 包版本，以及目标图形 API；不能按记忆套用其他版本 API。 |
| Shader 源码 | 解密原文、明文重建版、`Properties`、Pass、Keyword、Render State、HLSL 输入和 `CustomEditor` 均可审计。 |
| Editor 程序集 | 能找到定义原 ShaderGUI 的程序集；反编译工具、UnityEditor 引用和程序集依赖可解析。 |
| 材质样本 | 至少一份使用原 Shader 的代表性 `.mat`，包含非默认参数、贴图和 Keyword。 |
| 迁移边界 | 是否只迁移 Shader 与 GUI，是否包含示例材质、目录 `.meta`、运行时脚本或 Editor 依赖。 |
| 目标项目 | 已有 URP/Core 依赖；目标 Editor 程序集、程序集定义、GUID 和同名类不存在冲突。 |

原 ShaderGUI 只在 Unity Editor 中生效。独立兼容 GUI 必须放在目标工程的 `Editor` 目录，或放在仅由 Editor 程序集引用的目录；不得把它放进运行时程序集并期待 Player 构建通过。

## 3. 实现或排查步骤

### 3.1 先建立四层证据

把“能解密”与“能还原”分开。至少要建立以下四层证据：

```text
加密容器/程序集
    → 解密后的 ShaderLab/HLSL 原文
    → 编译后的 Shader 契约（Properties、Keyword、Pass、Render State、Varying）
    → ShaderGUI 与材质序列化状态
```

| 层 | 主要问题 | 不能只看的现象 |
| --- | --- | --- |
| 加密原文 | 真实 Shader 名、完整结构、原始 `CustomEditor` | 文件能打开、开头像 Shader |
| Shader 契约 | 属性、Keyword、Pass、输入通道、材质状态 | Unity 导入成功 |
| ShaderGUI | 分组、显隐、开关、模式、Blend、Queue、Keyword 同步 | C# 编译成功、面板能打开 |
| 材质状态 | 序列化值、贴图、`m_ValidKeywords`、材质覆盖状态 | Inspector 显示正常、单帧能渲染 |

任何一层缺失，都不能把任务标记为“功能已完整迁移”。

### 3.2 解密和解包先保存原文

解密流程按已有参考执行，重点是：

1. 先确认代理 Shader、真实 Shader 名、加密容器和工具版本的映射。
2. 解密结果先写到 `Temp` 或项目外审计目录，不先写入 `Assets`。
3. 核对 `Shader "..."`、`Properties`、`SubShader`、Pass、`CustomEditor` 和结尾闭合。
4. 保存原文 SHA-256；不要用“字符串长度一致”代替结构校验。
5. 正式还原资产使用新的 GUID；不要覆盖代理、容器或原始材质。

如果还原版为了可读性追加 `_Readable`，要把“源码改名”和“GUI 身份比较”分开审计，见 3.5。

### 3.3 对比时要检查功能，不只检查属性名称

把代理/旧工程版本和解密原文按同一层级比较：

| 检查项 | 目的 |
| --- | --- |
| `Shader "..."` 名称 | 确认身份比较、`Shader.Find` 和材质引用是否仍能命中。 |
| `Properties` | 检查属性 ID、类型、默认值、Drawer、Enum 数值和隐藏属性。 |
| `#pragma shader_feature*` / `multi_compile*` | 检查功能档位、默认状态和 Pass 一致性。 |
| CBUFFER 与 HLSL 声明 | 确认 GUI 显示的值是否真的参与渲染。 |
| Vertex/Fragment 输入 | 检查 `COLOR`、`TEXCOORD0`、`TEXCOORD1`、`TEXCOORD2` 等顶点流是否仍在。 |
| Pass/Render State | 检查 Queue、Blend、ZWrite、ZTest、Cull、Stencil、ColorMask。 |
| `CustomEditor` | 确认 Inspector 实现是否被遗漏、替换或降级成默认面板。 |
| Include 与脚本消费者 | 检查目标工程依赖、运行时写入和 RendererFeature 接口。 |

“代理 Shader 有属性但没有实现”与“明文 Shader 有属性但 GUI 没显示”是两个不同问题。前者是 Shader 功能缺失，后者通常是 ShaderGUI 或状态同步缺失。

### 3.4 恢复原始 ShaderGUI 的入口

从明文源码末尾定位 `CustomEditor`：

```hlsl
CustomEditor "ScreenSpaceDecalGUI"
```

再按以下顺序处理：

1. 搜索类名、`ShaderGUI` 基类和程序集来源。
2. 优先使用项目自己的源码；只有源码不可用时才反编译 Editor 程序集。
3. 反编译时只提取目标类型和必要依赖，不遍历或导出整个工程程序集。
4. 记录原类名、命名空间、基类、字段、`OnGUI` 顺序、`FindProperty` 调用、状态更新函数和外部类型依赖。
5. 判断原 GUI 能否在目标工程复用；不能复用时实现最小兼容 GUI，而不是用默认 Inspector 静默替代。

反编译 Editor 程序集时需要 UnityEditor/UnityEngine 引用。若普通反射因 Unity 依赖失败，优先按完整类型名反编译单类型，不要为了列类型而安装、替换或加载无关程序集。

### 3.5 精确 Shader 名或路径比较是隐蔽兼容点

原 ShaderGUI 常见以下写法：

```csharp
if (material.shader.name == "Engine/Effects/ParticleToScreenSpaceDecal" ||
    material.shader.name == "Engine/Effects/ParticleToModelScreenSpaceDecal")
{
    // 只在这里显示数据溶解、特殊模式或隐藏参数
}
```

这类判断不是普通 UI 细节，而是功能门槛。把 Shader 名改成 `..._Readable` 后，分支可能整体不执行，结果表现为“参数没有还原”：

- Inspector 不显示原来的数据溶解开关。
- `_Dissolve` 没有被 GUI 写入正确模式。
- 数据溶解 Keyword 没有打开。
- `CustomData` 输入仍在 Shader 中，但用户无法从面板启用对应模式。

处理原则：

- 先枚举所有 Shader 名、路径、GUID、Pass 名、版本号或类型名比较。
- 迁移时同步修改精确名称；或者实现独立兼容 GUI，按其加载的新 Shader 名称判断。
- 修改原 GUI 时必须回归旧 Shader，不能为支持 `_Readable` 而让原分支失效。
- 不允许把精确判断静默改成宽泛的 `Contains` 匹配；宽泛匹配会误命中其他 Shader，属于新的兼容风险。

### 3.6 区分 Property、Mode、Keyword 和顶点流

四个概念经常被混在一起：

| 概念 | 例子 | 说明 |
| --- | --- | --- |
| Inspector 标签 | `Dissolve From CustomData(1.w)` | 只说明用户如何理解参数，不要求存在同名 Shader Property。 |
| Shader Property | `_Dissolve("Dissolve Mode", Float)` | 参与材质序列化和 GUI 状态计算，不一定直接显示。 |
| Keyword | `_DISSOLVE_SIMPLE_DATA` | 控制 HLSL 编译分支；必须和实际材质状态同步。 |
| 顶点流 | `half customData : TEXCOORD2` | 由网格、粒子或渲染器提供，不是普通材质 Float 属性。 |

因此出现“Inspector 有开关，但 Shader 没有同名属性”并不矛盾。GUI 可以用一个开关组合 `_Dissolve` 数值和多个 Keyword；Shader 再从顶点流读取实际溶解值。

一个可复用的排查方法是先画出映射：

```text
用户操作
    → GUI 读取/写入的 float 或 Vector
    → GUI 打开/关闭的 Keyword
    → HLSL 分支
    → 顶点流/贴图/时间输入
    → 最终颜色、Alpha 或 Render State
```

任何箭头没有证据，都可能产生“面板可以操作，但渲染不响应”的假象。

### 3.7 兼容 ShaderGUI 的最小实现模式

兼容 GUI 应以原行为为契约，而不是重新设计面板。实现顺序建议如下：

1. 用 `ShaderGUI.FindProperty(name, properties, false)` 查找可选属性，允许目标 Shader 缺少非核心字段。
2. 保持原来的分组顺序和可见条件，尤其是同一个属性在不同模式下隐藏的行为。
3. 每个 Toggle、Enum 或模式变化都在一个幂等的更新函数中同时同步 Float、Keyword、材质状态和必要贴图。
4. 每个模式更新函数都先处理关闭状态，确保关闭后不会残留旧 Keyword 或旧贴图。
5. `OnGUI` 只负责读取当前材质状态、绘制控件和派发更新，不在每帧无条件重写材质。
6. `MaterialChanged` 等首次进入函数可以保留为空或只做最小初始化；没有原源码依据时，不要借机加入新的材质默认值。
7. 对多材质编辑、丢失属性、空贴图和 Undo 行为保留原 GUI 的处理方式。

一个常见的幂等同步骨架如下：

```csharp
private static void UpdateMode(
    Material material,
    bool enabled,
    bool useCustomData,
    bool useEdge)
{
    SetKeyword(material, "_DISSOLVE_SIMPLE", false);
    SetKeyword(material, "_DISSOLVE_EDGE", false);
    SetKeyword(material, "_DISSOLVE_SIMPLE_DATA", false);
    SetKeyword(material, "_DISSOLVE_EDGE_DATA", false);

    if (!enabled)
    {
        material.SetFloat("_Dissolve", 0f);
        return;
    }

    if (useCustomData && useEdge)
    {
        SetKeyword(material, "_DISSOLVE_EDGE_DATA", true);
        material.SetFloat("_Dissolve", 4f);
        return;
    }

    if (useCustomData)
    {
        SetKeyword(material, "_DISSOLVE_SIMPLE_DATA", true);
        material.SetFloat("_Dissolve", 3f);
        return;
    }

    if (useEdge)
    {
        SetKeyword(material, "_DISSOLVE_EDGE", true);
        material.SetFloat("_Dissolve", 2f);
        return;
    }

    SetKeyword(material, "_DISSOLVE_SIMPLE", true);
    material.SetFloat("_Dissolve", 1f);
}

private static void SetKeyword(Material material, string keyword, bool enabled)
{
    if (enabled)
    {
        material.EnableKeyword(keyword);
    }
    else
    {
        material.DisableKeyword(keyword);
    }
}
```

材质 Keyword 的开关应使用 `Material.EnableKeyword` / `Material.DisableKeyword`。不要用 `Shader.EnableKeyword` 之类的全局 API 代替材质状态，除非目标 Shader 明确使用全局 Keyword。

### 3.8 隐藏属性、材质状态和 Inspector 不能只看显示项

以下内容经常被“看起来没用”误导而漏迁移：

| 类型 | 例子 | 风险 |
| --- | --- | --- |
| 隐藏模式值 | `_Dissolve` | GUI 根据它推导模式，丢值后只能落到默认分支。 |
| 混合状态 | `_SrcBlend`、`_DstBlend`、`_ZWrite`、`_BlendOp` | 面板正常但透明、遮挡和排序错误。 |
| 队列与标签 | `RenderType`、`renderQueue` | Inspector 显示值正确，Frame Debugger 中顺序错误。 |
| 隐藏缩放参数 | `_DistortionStrengthScaled` | 位移强度与预期不一致。 |
| 顶点流 | `customData`、`texCoord1` | 面板有开关，但数据溶解、遮罩或形变无输入。 |
| 质量/裁剪开关 | `#pragma shader_feature_local` | 材质 Keyword 缺失或构建被剥离。 |

迁移后要同时检查 Shader 的 `Properties`、实际 HLSL 读取、ShaderGUI 写入和材质序列化值。只保留属性名而丢掉 GUI 逻辑，仍然是不完整迁移。

### 3.9 Editor 脚本离线编译校验

ShaderGUI 是 Editor C#，在 Unity 尚未重新导入时可先用 Unity 自带 Roslyn 做离线静态校验。至少检查：

- 目标 `.cs` 是否能在当前 Unity/C# 语言版本下编译。
- 是否正确引用 `UnityEditor`、`UnityEngine` 和项目 Editor 程序集。
- 是否绕开了不需要的 `Assembly-CSharp*` 或 Player-only 程序集。
- 是否真的不依赖原工程的 `Q1.Core.Editor.dll`、加密 DLL 或运行时私有类型。
- `UNITY_EDITOR` 条件和 `#if` 分支是否覆盖目标编辑器版本。

Roslyn 或离线编译成功只证明语法、引用和类型可见性，不证明 Inspector 行为正确。它应作为 Unity 打开材质和操作开关之前的快速失败检查。

### 3.10 跨工程迁移的最小文件闭包

迁移单位不是单个 `.shader`，而是“可加载 Shader + 可操作材质 Inspector + 必要的样本验证资产”。

| 优先级 | 文件 | 作用 |
| --- | --- | --- |
| 必需 | `ParticleToModelScreenSpaceDecal_Readable.shader` | 独立明文 Shader。 |
| 必需 | `ParticleToModelScreenSpaceDecal_Readable.shader.meta` | 保持资产 GUID 与导入类型；导入前检查冲突。 |
| 必需 | `ParticleToModelScreenSpaceDecalReadableGUI.cs` | 替代原 Editor DLL 中的 ShaderGUI。 |
| 必需 | `ParticleToModelScreenSpaceDecalReadableGUI.cs.meta` | 保持 Editor 脚本资产 GUID；导入前检查冲突。 |
| 建议 | 示例 `.mat` 与其 `.meta` | 验证属性、贴图、Keyword 和 Render State 是否完整。 |
| 可选 | Shader/GUI 所在目录的 `.meta` | 仅当目标工程需要保持目录 GUID 或已有引用时迁移。 |

通常不需要迁移：

- URP/Core 的 `Core.hlsl`、`Common.hlsl`、`Lighting.hlsl`。
- 原工程的 `Q1.Core.Editor.dll`、`ABEncrypt.dll` 或原生解密插件。
- 旧代理 Shader、旧 `.eng` 容器和未使用的材质副本。

前提是目标 Shader 只依赖公开 URP/Core 包，且兼容 GUI 不引用原 Editor DLL。若还保留旧运行时替换链，应把该链单独作为依赖评审，不能把 DLL 随手复制到目标工程。

### 3.11 已复盘案例：ParticleToModelScreenSpaceDecal

> 以下内容是本次工程适配证据，不是跨项目固定事实。路径、类名、GUID、模式和关键字必须以目标工程为准。

本次问题链路是：

```text
代理 Shader
    → 解密得到 Engine/Effects/ParticleToModelScreenSpaceDecal
    → 源码中的 CustomEditor "ScreenSpaceDecalGUI"
    → 原 GUI 在 com.q1.core 的 Editor 程序集中
    → 明文 Shader 改为 Engine/Effects/ParticleToModelScreenSpaceDecal_Readable
    → 原 GUI 的精确名称分支不再命中
    → Dissolve From CustomData(1.w) 等模式没有在迁移后显示
```

已确认的关键事实：

| 项目 | 事实 |
| --- | --- |
| 原 Shader | `Engine/Effects/ParticleToModelScreenSpaceDecal` |
| 原 GUI 类型 | `ScreenSpaceDecalGUI` |
| 明文 Shader | `Engine/Effects/ParticleToModelScreenSpaceDecal_Readable` |
| 兼容 GUI | `ParticleToModelScreenSpaceDecalReadableGUI` |
| 数据溶解标签 | `Dissolve From CustomData(1.w)` |
| Shader 顶点流 | `half customData : TEXCOORD2` |
| 数据溶解读取 | `half dissolveData = i.customData` |
| 模式映射 | `1` 简单、`2` 边缘、`3` 简单 + CustomData、`4` 边缘 + CustomData |

原 GUI 只在精确匹配 `ParticleToScreenSpaceDecal` 或 `ParticleToModelScreenSpaceDecal` 时进入数据溶解分支。改名后，这个分支被跳过，所以问题不是 Shader 缺少 `customData` 参数，而是 GUI 的身份判断和新 Shader 名不一致。

兼容 GUI 保留的关键行为包括：

- `Cull Mode`、Blending Mode、Albedo、Polar UV、UV Anim、Mask、Dissolve、Distortion 和 Advanced Options 的绘制顺序。
- Alpha/Additive 对 `_SrcBlend`、`_DstBlend`、`_ZWrite`、`_ALPHATEST_ON`、RenderType 和 `renderQueue` 的联动。
- `_Polar` 与 `_POLAR_ON`、`_PolarUV` 与 `_POLAR_UV` 的同步。
- `_UVAnim` 与 `_UVANIM_DEFAULT`、`_Mask` 与 `_MASK_ON`、`_Distortion` 与 `_DISTORTION_UV` 的同步。
- 四种 Dissolve Keyword 的互斥切换，以及数据模式下对 `_DissolveLevel` 的处理。

本次验证边界：

- 兼容 GUI 已通过离线 Roslyn 编译。
- Shader 已导入，四条溶解 Keyword 路径已检查。
- 这不等于所有材质都完成最终视觉 A/B，也不等于目标图形 API、AssetBundle 和变体剥离全部通过。

### 3.12 迁移时推荐的执行顺序

1. 在源工程完成解密、原文审计和明文 Shader 重建。
2. 在源工程定位 `CustomEditor`，反编译或复用原 ShaderGUI。
3. 用代表性材质逐个测试原 GUI 的模式、Keyword、贴图和 Render State。
4. 判断目标工程是否需要独立兼容 GUI；若需要，复制最小依赖并移除旧程序集引用。
5. 在目标工程建立测试目录，先导入 Shader、GUI、示例材质和 `.meta`。
6. 先检查 GUID 冲突、程序集作用域和 `CustomEditor` 类型解析，再打开材质。
7. 用同一组材质参数完成 Inspector、Keyword、Frame Debugger 和固定条件视觉对比。
8. 只有单 Shader 闭环通过后，才考虑迁移其他 Shader、批量材质或运行时替换链。

## 4. 风险与不适用边界

| 风险 | 典型现象 | 正确处理 |
| --- | --- | --- |
| 精确 Shader 名/路径比较 | 改名后某个分支消失，参数没有还原 | 适配新名称或实现兼容 GUI；回归旧名称。 |
| 默认 Inspector 替代 | 面板能打开，但丢失分组、模式、Keyword 和 Blend 逻辑 | 明确列缺失项，不能称为 GUI 已迁移。 |
| Float 与 Keyword 不同步 | 下拉变了，画面不响应；或旧 Keyword 残留 | 在一个幂等更新函数中同步 Float、Keyword 和状态。 |
| 隐藏属性漏迁移 | 属性在 Shader 中存在，但 GUI 无法写入或清除 | 保留隐藏属性、默认值和初始化行为。 |
| 顶点流误解为材质属性 | GUI 有开关，Shader 却拿不到数据 | 检查网格/粒子 Vertex Streams 和语义绑定。 |
| 只验证 C# 编译 | 编译通过，但 Inspector 打开即重置材质 | 进行实际材质操作和序列化快照验证。 |
| Meta/GUID 冲突 | Unity 将脚本或 Shader 识别为其他资产 | 导入前扫描 GUID，冲突时明确保留、替换或重新生成。 |
| Editor 依赖进入 Player | 目标工程构建时找不到 UnityEditor 或旧 DLL | 兼容 GUI 只放 Editor 目录，移除运行时程序集引用。 |
| 反编译误差 | GUI 逻辑看起来正确，但与真实行为不同 | 以原类型、原材质行为和 Unity API 回读为准，不盲信反编译文本。 |
| 只做静态还原 | Unity 导入成功但视觉不一致 | 用代表性材质、目标 API 和 Frame Debugger 做 A/B。 |

本文不把任何一次项目的模式编号、类名、路径或 GUID 当成通用 ABI。迁移到其他 Shader 时，必须先重新建立属性、模式、Keyword、顶点流和 GUI 状态的映射表。

## 5. 验证与回退

### 5.1 静态验证

- 解密原文、明文 Shader 和兼容 GUI 都已经保存审计证据。
- `Shader "..."`、`CustomEditor`、类名、命名空间和文件名一致。
- `Properties`、CBUFFER、HLSL 声明、Pass、Keyword 和 Render State 已逐项对照。
- 精确名称/路径分支、`Shader.Find`、运行时加载和材质替换脚本已检查。
- GUI 不依赖原工程的 Editor DLL、运行时私有类型或未迁移的加密组件。
- 所有 `.meta` GUID 在目标工程范围内唯一。
- 文档、源码和脚本使用 UTF-8；代码标识符和路径使用反引号。

### 5.2 Unity/Editor 验证

- C# 离线编译通过，且没有依赖 `Assembly-CSharp*` 或 Player-only 程序集。
- Unity AssetImportWorker 成功导入 Shader 和 GUI，没有 `Shader error`、类型缺失或程序集错误。
- 代表性材质能打开，Inspector 的分组、显隐、Toggle、Enum 和 Advanced Options 与原 GUI 一致。
- 每个模式都能验证 Float、Keyword、贴图、Render Queue、Blend 和 ZWrite 的联动。
- 输入模式关闭后，旧 Keyword、贴图和隐藏值被清理或恢复为原行为。

### 5.3 材质与渲染 A/B

使用同一份材质副本、Mesh/粒子、相机、灯光、质量档、时间点和图形 API，对比：

- Albedo、Alpha、颜色空间和透明度。
- Polar UV、UV Anim、Mask 和 Distortion。
- 普通溶解、边缘溶解、CustomData 溶解和边缘 + CustomData 溶解。
- Queue、Blend、ZWrite、ZTest、Cull、Stencil 和 Frame Debugger 顺序。
- 原 GUI 与兼容 GUI 操作后，材质序列化值和 `m_ValidKeywords` 是否一致。

只完成源码恢复或 Unity 导入时，结论应写成“源码与导入已验证，视觉未验证”，不能写成“功能完全一致”。

### 5.4 迁移验证

在一个干净的目标工程副本中验证：

1. 只复制必需文件和 `.meta`，不复制原 DLL 和代理依赖。
2. 打开示例材质，确认 `CustomEditor` 被解析为兼容 GUI。
3. 逐项操作所有模式，确认材质状态和实际渲染同步。
4. 运行目标图形 API、构建、变体收集或 AssetBundle 路径（如项目使用）。
5. 核对迁移后的目录结构、程序集引用和 GUID，没有覆盖目标工程已有资产。

### 5.5 回退

- 删除新增的明文 Shader、GUI、示例材质和对应 `.meta`，恢复原代理 Shader 与材质引用。
- 恢复原 `CustomEditor` 或原 Editor 程序集；不要修改原代理目录来“回退”。
- 若兼容 GUI 出现问题，先停用新材质引用和 GUI，不删除原加密容器、原 Editor 程序集或用户材质。
- 批量迁移前保留目录清单、材质清单和 GUID 清单，确保每个新增资产都能单独撤回。

## 6. 可复用检查清单

- [ ] 已确认授权、工具来源和目标源码范围。
- [ ] 已区分加密原文、Shader 契约、ShaderGUI 和材质状态四层证据。
- [ ] 已定位并分析原 `CustomEditor` 类型。
- [ ] 已检查所有精确 Shader 名、路径、Pass、GUID 或版本比较。
- [ ] 已建立 Property → Mode → Keyword → HLSL → 顶点流的映射。
- [ ] 已恢复隐藏属性、模式值、Blend、Queue 和 Keyword 同步。
- [ ] 兼容 GUI 已移除原 Editor DLL 依赖并放入 `Editor` 目录。
- [ ] `.meta` GUID 已扫描且无冲突。
- [ ] 已完成离线 C# 编译和 Unity 导入验证。
- [ ] 已完成代表性材质的 Inspector、Keyword、Frame Debugger 和视觉 A/B。
- [ ] 已记录目标图形 API、构建剥离和未验证边界。
- [ ] 已确认仅迁移 Shader、GUI、示例材质和必要 `.meta`，没有把无关 DLL/容器带入目标工程。
