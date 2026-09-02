---
name: urp-shader-resource-ownership-and-bundle-dedup
description: Unity/URP 中 Shader 重复现象的分类、资源所有权、AssetBundle 收集边界、运行时加载与验证参考。
---

# URP Shader 资源所有权与 AssetBundle 去重参考

> **类型**：`REFERENCE`
>
> **适用范围**：Unity/URP、AssetBundle/Addressables/YooAsset、运行时质量切换、后处理和 `Hidden/*` Shader 的重复实例排查。
>
> **使用前提**：必须以目标工程的 Unity/URP 版本、资源收集器、Player 设置、运行时加载器和实际构建重新验证。本文不把某个项目的路径、版本或资产名当成通用事实。

## 1. 先区分三种“重复”

看到很多同名 Shader，不要直接删除文件。先判断重复发生在哪一层：

| 类型 | 现象 | 是否通常是问题 | 首要证据 |
| --- | --- | --- | --- |
| 源文件重复 | 工程中存在多个同名/近似名 `.shader` | 可能是问题，也可能是不同 Shader 名 | ShaderLab `Shader "..."`、GUID、材质引用 |
| 运行时实例重复 | Player 和 AssetBundle 各加载一份同一逻辑 Shader | 通常是资源所有权问题，会增加内存和加载复杂度 | 资源加载链、Bundle manifest、Memory Profiler |
| 变体数量多 | 一个 Shader 下面有很多 Keyword/Pass 组合 | 不等于 Shader 对象重复；URP/后处理常见 | `#pragma`、Variant Collection、构建报告 |

`Hidden/Universal Render Pipeline/UberPost` 属于 URP 后处理链使用的隐藏 Shader。它有多个 Pass 和 Keyword 变体是正常的；真正需要修的是“同一依赖被 Player 与运行时 Bundle 各持有一份”，而不是把变体数量误当成源文件重复。

## 2. 画出真实资源链，而不是只看 Shader 名

典型链路如下：

```text
GraphicsSettings / QualitySettings
        └─ URP Pipeline Asset
             └─ RendererData
                  └─ PostProcessData / RendererFeature / Blit 资源
                       └─ UberPost、Bloom、FinalPost 等 Shader

AssetBundle collector + AutoCollectShaders
        └─ 收集被内容资源依赖的 Shader
             └─ unityshaders.bundle

运行时 URP 加载器
        └─ 从 Bundle 再加载一套 Pipeline/Renderer
             └─ 同一逻辑 Shader 形成第二条持有链
```

多个 URP Pipeline 资产引用同一个 `PostProcessData` 或同一个 Shader GUID，本身不会自动产生多份 Shader 资产；问题通常来自**两个加载所有者**或**旧 Bundle 与新 Player/Bundle 同时驻留**。

## 3. 资源所有权与实现选择

### 3.1 默认主推：Player 统一持有 URP

适用于 Pipeline/Renderer 属于固定客户端版本、需要参与 Graphics/Quality 设置、不会作为独立内容热更的工程。

1. Player/Resources 或等价的静态序列化入口持有所有运行时 URP Pipeline 引用。
2. 运行时质量切换只返回这些已持有的对象，不再通过 YooAsset/AssetBundle 加载第二套 Pipeline。
3. AssetBundle collector 保留普通运行时设置，但排除 Pipeline、RendererData 及其作为主资源的配置文件。
4. `AutoCollectShaders` 可以继续用于真正的内容 Shader；不要为了修 URP 所有权而全局关闭。

### 3.2 只有确需热更时才让 Bundle 持有 URP

如果 Pipeline/Renderer 真的需要按版本热更，必须反向执行：

- Player 不再序列化同一套 URP 引用；
- Graphics/Quality 设置在切换前由唯一的 Bundle 实例接管；
- 旧 Pipeline、Renderer、Shader 和 Bundle 句柄在切换后可确认释放；
- 构建、manifest、缓存失效和 Memory Profiler 均有证据。

不能让 Player 和 Bundle 同时作为“备用方案”。

## 4. 排查与修复工作流

### 4.1 资产与 GUID 盘点

先列出所有 Pipeline、RendererData、PostProcessData 和 `Hidden/*` Shader 的路径、GUID、引用者。至少检查：

- `ProjectSettings/GraphicsSettings.asset`；
- `ProjectSettings/QualitySettings.asset`；
- 每个 URP Pipeline 的 RendererData 列表；
- RendererFeature、Volume/PostProcess 资源；
- 材质、Prefab、Scene、脚本和配置生成物。

不要只用文件名判断是否重复，也不要在没有材质/场景/Renderer 消费者证据时删除 Shader。

### 4.2 运行时加载盘点

搜索所有 `LoadAsset<UniversalRenderPipelineAsset>`、`LoadAsset<ScriptableRendererData>`、`Resources.Load`、`Addressables.LoadAsset` 和自定义反射加载入口，建立：

```text
调用者 → 资源系统 → 资产地址/GUID → 生命周期/释放 → 当前管线设置
```

如果同一质量切换既读取 `QualitySettings`/`GraphicsSettings`，又从 YooAsset 加载 Pipeline，配置过滤本身不够，必须改运行时入口。

### 4.3 收集器边界

优先保留原目录收集范围，再用过滤规则排除确定的 Pipeline/Renderer 类型或命名集合。不要为了排除 URP 而把整个 Settings 目录缩成一个子目录，否则可能误丢普通运行时设置。

过滤逻辑至少要覆盖：

- Pipeline 资产；
- `ScriptableRendererData` / Renderer 资产；
- 工程中用于 URP 时序或多相机的同目录管线配置；
- 它们作为依赖被间接重新纳入时的 Bundle 行为。

### 4.4 构建和运行时证据

静态搜索只能证明“代码和配置意图”。要证明已去重，还必须：

1. 变更后全量重建 AssetBundle/Addressables；
2. 检查新 manifest 和 shader bundle，不使用旧 Simulate/缓存作为证据；
3. 在 Development Player 中切换所有代表性质量和场景；
4. 用 Memory Profiler/对象搜索确认同一逻辑 Shader 的实例数量和来源；
5. 用 Frame Debugger/画面回归确认后处理、Bloom、UI 和材质没有失效。

## 5. 常见错误修法

| 错误修法 | 为什么不可靠 | 正确替代 |
| --- | --- | --- |
| 直接删除 `UberPost` 或 URP Shader 文件 | 破坏 URP 包或 Renderer 依赖，可能粉材质/后处理失效 | 修资源所有权和加载链 |
| 全局关闭 `AutoCollectShaders` | 会让真正的内容 Shader 缺失；不能解决 Player/Bundle 双持有 | 只排除 URP Pipeline/Renderer，保留内容 Shader 收集 |
| 只把 collector 改到某个子目录 | 可能漏掉普通设置，产生隐性功能回归 | 原目录 + 精确过滤规则 |
| 只改资源配置、不改运行时代码 | 代码仍会尝试从 Bundle 加载，切换可能失败或走备用链 | 配置和运行时入口同时改 |
| 只看 Shader 名称计数 | 混淆变体、源文件和运行时对象 | 结合 GUID、manifest、加载调用和 Profiler |

## 6. 验证矩阵与回退

| 层级 | 必须回答的问题 | 通过标准 |
| --- | --- | --- |
| 源码/配置 | 是否仍有第二个 URP 加载入口？收集器是否误伤普通设置？ | 代码搜索和配置检查符合唯一所有者设计 |
| Editor 编译 | 自定义过滤器、注册表和运行时代码是否可编译？ | Unity Editor 编译通过；`dotnet build` 仅作补充 |
| Bundle 构建 | 新 manifest 是否不再把 URP Pipeline/Renderer 作为 Bundle 主资源？ | 全量构建后检查 manifest/shader bundle |
| Player 运行时 | 所有场景/质量切换是否正常？ | 无粉材质、后处理正常、旧 Bundle 可释放 |
| 内存/视觉 | `UberPost` 是否只有预期实例？ | Memory Profiler + Frame Debugger/截图证据 |

回退时必须成组恢复“收集器配置 + 运行时加载入口 + Player 映射”，并清理/失效对应新旧 Bundle 缓存。不要只回退其中一层。

