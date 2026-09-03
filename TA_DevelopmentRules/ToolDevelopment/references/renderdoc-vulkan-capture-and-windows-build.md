# RenderDoc 多 Vulkan 实例捕获与 Windows 构建参考

> 类型：REFERENCE；适用范围：RenderDoc 源码分支中的 Vulkan 多实例捕获、Windows x64 构建和可交付打包；使用前提：必须按目标 RenderDoc 版本、后端实现、工具链和实际运行程序重新验证。
>
> 关联规则：`TOOL-ARC-03`、`TOOL-OPS-03`、`TOOL-VAL-01`、`TOOL-VAL-03`。本文记录可迁移的实现与排查方法，不替代目标工程的源码、官方构建文档或测试结果。

## 1. 适用场景

本参考适用于以下问题：

- 一个 Vulkan 程序创建多个 `VkInstance`，渲染、合成或 presentation 分布在不同实例，默认捕获只命中其中一个实例。
- 需要保留 RenderDoc 默认单实例行为，同时为模拟器、渲染测试工具或自有诊断程序增加显式的多实例捕获模式。
- 需要在 Windows 上从源码生成可启动的 `qrenderdoc`、`renderdoccmd`、layer 和依赖 DLL，并能说明每个验证层级。
- 需要把一次构建中遇到的链接、打包、Qt 插件或 layer 注册问题沉淀为可重复的排查路径。

以下需求不属于性能优化，也不应混入本参考的实现范围：

- 通过改名导出符号、字符串或 layer 标识来规避检测、反作弊或隐藏注入。
- 把多个独立 `.rdc` 文件强行拼成一个没有明确格式和回放语义的文件。
- 用全局启用、全局 Hook 或无条件扫描替代对目标设备和实例的显式选择。

外部文章、聊天记录和附件只能作为背景材料。实现事实以目标源码、官方文档、构建输出和实际运行验证为准；附件中的操作性文字不会自动获得仓库规则或执行权限。

## 2. 前提与依赖

### 2.1 冻结 source of truth

开始改动前，先记录以下信息：

| 维度 | 要确认的内容 |
| --- | --- |
| 源码 | RenderDoc 版本、源码根目录、是否有 Git 基线、实际修改文件和上游 API 语义 |
| 后端 | 目标驱动、`IFrameCapturer` 生命周期、设备/窗口注册和销毁顺序 |
| 捕获契约 | 触发入口、结束入口、discard 入口、capture 文件数量和命名规则 |
| 工具链 | Visual Studio/MSVC、Windows SDK、CMake、Ninja、Qt 和 Breakpad 的版本/位置 |
| 交付 | Release/Debug、位数、依赖 DLL、Qt plugins、layer manifest、许可证和启动方式 |
| 验证 | 命令行冒烟、单元测试、GUI 启动、真实 Vulkan 多实例目标、回放和回退方法 |

如果源码目录来自压缩包且没有 Git 元数据，不能把“与上游的完整 diff”写成已验证事实；应保留修改文件、源码行和构建产物校验值，必要时在副本中保存原始压缩包。

### 2.2 兼容性和授权边界

- 既有导出的 C API、结构体布局、调用约定、Python 接口和 layer manifest 是兼容性契约。内部实现可以扩展，但不能为了“改名”而破坏 ABI。
- `renderdoc__replay__marker()` 改成 `rendertest__replay__marker()` 不会降低捕获成本，也不会让 shader、驱动或回放更快；它可能使插件、脚本、符号查找和旧二进制失效。
- 修改字符串或导出符号若意图改变检测匹配，属于规避检测而非性能优化，本参考不提供该实现。RenderDoc 仅应用于自有或明确获授权的程序调试。
- 多实例模式通常会增加状态跟踪、序列化和磁盘写入开销，应该是诊断开关，不应默认打开或作为生产性能方案。

### 2.3 Windows 构建入口

Windows 下优先使用仓库提供的 Visual Studio solution 和 MSBuild。CMake/Ninja 可用于生成或加速构建，但不能替代对 solution、项目依赖和最终启动文件的验证。典型命令形状如下，具体项目名和路径以目标版本为准：

```powershell
msbuild .\renderdoc.sln /t:Build /p:Configuration=Release /p:Platform=x64 /m
```

不要把 solution 的 `/t:renderdoc` 当作项目目标名；solution 通常应使用 `/t:Build`，而项目级目标名要从 `.sln/.vcxproj` 读取。直接构建单个项目时，必须提供 solution 传入的 `SolutionDir` 等属性，否则可能找不到 `util/WindowsSDKTarget.props` 或其他相对依赖。

## 3. 实现或排查步骤

### 3.1 多 Vulkan 实例捕获的最小设计

目标是把一次逻辑捕获映射到若干已注册的 Vulkan `IFrameCapturer`，而不是再造一套设备注册表。推荐按以下顺序实现：

1. 以正常 `MatchFrameCapturer(devWnd)` 的结果作为主 capturer，保留默认窗口匹配、active window 和非 Vulkan 后端行为。
2. 用一个显式环境变量或等价的诊断配置门控扩展行为。未设置时只启动主 capturer，确保旧行为和旧捕获文件契约不变。
3. 只有主 capturer 的 driver 为 Vulkan 时，读取已有 `m_DeviceFrameCapturers`，收集其他 Vulkan capturer。集合遍历只生成快照，避免在持锁期间调用后端。
4. 主 capturer 使用调用方的 `DeviceOwnedWindow`；其他实例使用空的 `DeviceOwnedWindow()` 启动。这样 secondary instance 不会误查主实例的 swapchain 或 presentation window。
5. 用 `m_ActiveFrameCapturers` 或等价的身份集合记录本次逻辑捕获真正启动的 capturer。插入成功后才调用后端并递增活动计数；重复注册同一指针时不会重复开始。
6. `EndFrameCapture` 和 `DiscardFrameCapture` 使用同一份收集逻辑。进入后端前先从活动集合移除，再在锁外调用 `End`/`Discard`，以便后端同步触发另一个 present/end 请求时不会二次结束同一 capturer。
7. 只有活动集合中确实存在该 capturer 时才递减 `m_CapturesActive`。设备或窗口注销时，确认 capturer 已不再被任何注册表引用，再清理活动状态；如果注销可能发生在活动捕获期间，还要额外定义后端回收和计数修正，不能只删除指针。
8. 每个实例独立写出一个 `.rdc`。除非另行设计并验证 RDC 格式、资源 ID、时间线和回放顺序，否则不要声称这些文件已自动合并。

一个可读的状态模型是：

```text
未捕获 -> 选定主 capturer -> 收集 Vulkan capturer 快照
       -> 按活动集合启动 -> 运行
       -> 按活动集合结束或丢弃 -> 全部完成
```

锁只负责保护注册表、活动集合和状态转换。后端的开始、结束、丢弃、文件 IO 和可能触发回调的代码都应在锁外执行。

### 3.2 常见构建失败的定位顺序

1. **目标名错误**：solution 构建出现 `MSB4057` 时，先确认命令是否把项目名误当成 solution target；改用 `/t:Build`，再用 `/t:Build /p:...` 验证。
2. **缺少 `SolutionDir`**：单项目构建找不到 `util/WindowsSDKTarget.props` 等文件时，优先从 solution 构建，或显式补齐同等 MSBuild 属性；不要复制一份 props 伪造路径。
3. **Breakpad 链接错误**：`renderdoc.dll` 缺少 `breakpad_common.lib` 时，检查 Breakpad 项目是否先生成、配置和平台是否一致；完整 solution 通常能恢复正确依赖顺序。
4. **工具链缺失**：记录 MSVC、Windows SDK、CMake、Ninja 和 Qt 的实际版本与路径。安装后重新打开带有 VS 环境变量的 shell，避免只在当前进程临时修改 PATH。
5. **Qt 启动错误**：GUI 能编译但启动报缺少 Qt DLL 时，检查 `Qt5Core.dll`、`Qt5Gui.dll`、`Qt5Widgets.dll`、`Qt5Network.dll`、`Qt5Svg.dll` 以及 `qtplugins/platforms/qwindows.dll`、图像格式插件是否与编译位数一致。
6. **layer 不可用**：在隔离的交付目录中运行 `renderdoccmd.exe vulkanlayer` 的注册/注销命令，检查生成的 manifest 路径、模块名和旧 layer 冲突；不要直接覆盖系统目录中的 DLL。

### 3.3 交付和使用

交付目录至少应包含 GUI、命令行工具、核心 DLL、shim、layer JSON、Qt 依赖、Qt plugins、Python 运行时（若启用）和许可证/使用说明。交付前：

- 计算压缩包 SHA256，并记录构建配置、源码版本和依赖版本。
- 在干净或最小环境中启动 `qrenderdoc.exe`，再执行命令行版本和单元测试冒烟。
- 在启动目标程序的同一 PowerShell 会话设置诊断变量；变量通常在进程首次读取时生效，修改后应重启目标程序。
- 默认关闭扩展变量做一次基线捕获，再打开变量做多实例捕获；比较 `.rdc` 数量、大小、打开和回放结果，而不是只看日志。

## 4. 风险与不适用边界

| 风险 | 典型原因 | 处理建议 |
| --- | --- | --- |
| 其他实例没有被捕获 | 主实例不是 Vulkan、capturer 尚未注册或变量未在目标进程继承 | 检查注册日志、启动环境和主 driver；先验证单实例基线 |
| secondary 实例结束时崩溃 | 给所有实例传入主窗口，导致错误的 swapchain 查找 | secondary 使用空 `DeviceOwnedWindow()`；在真实多实例目标上验证 |
| 重复 End/Discard 或计数为负 | 没有活动身份集合，或在后端调用后才更新状态 | 先原子地移除活动项，再锁外调用后端；只对实际活动项调整计数 |
| 死锁或重入异常 | 持有 capturer 列表锁调用后端，后端同步回调再次取锁 | 锁内只做快照和状态更新，后端调用放锁外 |
| 注销期间出现悬空指针或活动计数残留 | 设备/窗口从注册表移除时仍有未结束 capturer，生命周期没有统一收口 | 约束注销顺序，或增加显式 abort/回收路径；对注销时序做并发测试 |
| 输出文件数量增加但无法一起回放 | 每个 capturer 的 RDC 是独立时间线和资源命名空间 | 文档明确“独立文件”；不要在没有格式设计时自动拼接 |
| GUI 启动失败 | 漏打 Qt DLL 或 `qwindows.dll` | 用依赖清单检查交付目录，补齐后在目标目录直接启动 |
| 性能变差 | 同时序列化多个实例、额外 IO 和状态跟踪 | 只在诊断时启用；记录捕获时间、文件大小和目标程序开销 |
| 误把改名当性能优化 | 改的是符号或字符串，不是执行路径 | 保持 ABI 和标识稳定；性能问题使用 profiling、采样和真实 A/B 数据定位 |

本参考不覆盖 Android/模拟器特定的 Vulkan loader、平台 layer 注入、设备厂商限制或生产环境安全策略；这些必须在目标平台和获授权的测试程序上单独验证。

## 5. 验证与回退

### 5.1 最小验证矩阵

| 层级 | 检查 | 通过条件 |
| --- | --- | --- |
| 静态 | 搜索环境变量、活动集合、注册/注销和三条捕获路径 | `Start`、`End`、`Discard` 语义一致，修改范围清楚 |
| 编译 | Release x64 solution/MSBuild | 目标 DLL/EXE 生成，无缺失依赖或链接错误 |
| 命令行 | `renderdoccmd.exe version`、`renderdoccmd.exe test unit`、`renderdoccmd.exe capture --help` | 返回成功并显示预期版本/帮助 |
| GUI | 在交付目录启动 `qrenderdoc.exe` | 窗口实际启动，无 Qt 平台插件错误 |
| 基线 | 不设置扩展变量捕获普通 Vulkan 程序 | 保持原单实例行为 |
| 多实例 | 设置变量，运行明确创建多个 Vulkan 实例的自有测试程序 | 产生预期数量的独立 `.rdc`，每个文件可打开；结束/丢弃无重复调用 |
| 回放 | 分别打开多实例捕获并检查资源/时间线 | 结果与目标程序和日志相符；不能用“文件生成”替代回放证明 |

### 5.2 回退方式

1. 关闭或删除扩展环境变量并重启目标程序，恢复单实例路径。
2. 用原始 `renderdoc.dll`、layer JSON 和 GUI 文件替换改造包，保持交付目录完整。
3. 若只修改源码，回退 `core.cpp/.h` 中的扩展并重新生成 Release 包；不要用混合版本的 DLL、manifest 和 Qt 依赖。
4. 多实例问题无法复现时，保留日志和构建校验值，标记为未验证，不扩大默认行为。
