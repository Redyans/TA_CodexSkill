# RenderDoc v1.47 多 Vulkan 实例捕获案例

> 层级：PROFILE；适用工程：`D:/work2025U3D/共享包体/Var/renderdoc-1.x`；基线：RenderDoc `v1.47`；本文件记录本次源码、构建、交付和验证事实，不得直接提升为跨项目 CORE。

## 1. 项目事实与目标

本次改造的目标是为 Vulkan 多实例诊断增加一个显式开关：当一个程序把渲染和 presentation 分布在多个 Vulkan 实例时，允许一次逻辑捕获联动已注册的 Vulkan capturer，同时保持默认单实例行为。

源码目录为：

```text
D:\work2025U3D\共享包体\Var\renderdoc-1.x
```

该目录来自源码压缩包，当前没有可用的 Git 元数据。因此本案例以源码内容、构建结果、交付文件和命令输出为事实来源，不声称提供相对于上游的完整 Git diff。

## 2. 实现方式

### 2.1 修改文件和数据结构

实现集中在：

- `renderdoc/core/core.cpp`
- `renderdoc/core/core.h`

`RenderDoc` 原有的 `m_DeviceFrameCapturers` 继续作为设备到 `IFrameCapturer` 的注册表；没有新增平行设备表。头文件增加：

```cpp
std::set<IFrameCapturer *> m_ActiveFrameCapturers;
```

该集合表示当前逻辑捕获真正启动的 capturer，与 `m_CapturesActive` 分开：前者用于幂等结束/丢弃，后者用于现有活动捕获计数。

### 2.2 环境变量门控

扩展开关为：

```text
RENDERDOC_VULKAN_CAPTURE_ALL_DEVICES=1
```

`StartFrameCapture`、`EndFrameCapture` 和 `DiscardFrameCapture` 都读取同一开关，并且只有匹配到的主 capturer driver 为 Vulkan 时才扩展到其他 Vulkan capturer。未设置变量时，代码仍只处理原始匹配结果。

当前实现使用进程内静态布尔值缓存环境变量，因此变量应在目标进程第一次进入捕获逻辑前设置；运行中修改环境变量不会可靠地切换模式，切换时应重启目标进程。

### 2.3 开始、结束和丢弃

流程如下：

1. `MatchFrameCapturer(devWnd)` 选择调用方的主 capturer，并加入本次 capturer 列表。
2. 开关打开且 driver 为 Vulkan 时，在 `m_CapturerListLock` 保护下遍历 `m_DeviceFrameCapturers`，追加其他 Vulkan capturer。
3. 对每个 capturer，在锁内插入 `m_ActiveFrameCapturers`；只有插入成功才在锁外调用后端并递增 `m_CapturesActive`。
4. 主 capturer 使用真实 `devWnd`；其他 capturer 传入空 `DeviceOwnedWindow()`，避免 secondary 实例查找主实例的 presentation window。
5. 结束或丢弃时，对每个候选项在锁内先从活动集合移除，再在锁外调用后端；只有成功移除的项才递减 `m_CapturesActive`。
6. 设备或窗口移除时，如果 capturer 已不再被任一注册表引用，则清理活动集合项，避免保留失效对象。当前实现未单独证明“活动捕获中注销”时的后端回收和 `m_CapturesActive` 计数修正，实际使用应保证正常的结束/丢弃先于注销，或补充显式 abort 路径。

此设计保留了普通单实例窗口匹配、active window 和非 Vulkan backend 的原有路径；扩展只影响显式开启的 Vulkan 诊断场景。

## 3. Windows 工具链与构建

本次使用的工具链事实：


| 工具 | 版本/位置 |
| --- | --- |
| Visual Studio Build Tools | 2022 `17.14.37614.0` |
| MSVC | v143 |
| Windows SDK | `10.0.26100.0` |
| CMake | `3.31.6`，`C:\Tools\cmake-3.31.6` |
| Ninja | `1.12.1`，`C:\Tools\ninja` |
| 构建配置 | Windows x64 Release |

Windows 构建以仓库 `renderdoc.sln`/MSBuild 为主。CMake 和 Ninja 已准备好，但不能仅凭它们存在就认定目标 solution、依赖顺序和最终交付可用。

## 4. 构建和打包问题记录

| 现象 | 根因 | 修正或经验 |
| --- | --- | --- |
| solution 构建出现 `MSB4057` | 将 `/t:renderdoc` 当作 solution target | 使用 `/t:Build`，项目目标名必须从 solution/project 文件确认 |
| 单独构建项目找不到 `util/WindowsSDKTarget.props` | 直接项目构建没有 solution 注入的 `SolutionDir` | 优先完整 solution 构建，或显式提供正确的 MSBuild 属性 |
| `renderdoc.dll` 链接缺少 `breakpad_common.lib` | Breakpad 目标未先生成或配置/平台不一致 | 先检查 Breakpad 构建顺序；完整 solution 能提供依赖顺序 |
| `qrenderdoc.exe` 启动缺少 Qt DLL | 初始交付包漏掉 `Qt5Widgets.dll` 等运行时依赖 | 补齐 Qt5 DLL、`qtplugins/platforms/qwindows.dll` 和所需图像插件后再启动验证 |
| `test functional` 未通过 | 交付包不含预期的 `util/test/run_tests.py` | 不把该结果改写成“功能测试通过”；改用当前包内可执行的命令验证并记录缺口 |

## 5. 交付产物

交付目录：

```text
D:\work2025U3D\共享包体\Var\RenderDoc-1.47-MultiVulkan-win64
```

交付压缩包：

```text
D:\work2025U3D\共享包体\Var\RenderDoc-1.47-MultiVulkan-win64-final.zip
```

当前压缩包 SHA256：

```text
2EF1BCE1FC7C5EAEDBE68B298813AF2C3325F68188D90ACBFCF36CBF8E505885
```

交付目录已包含核心可执行文件和 DLL、`renderdoc.json`、Python 模块、Qt DLL、`qtplugins`、许可证和本案例说明。关键文件包括：

- `renderdoc.dll`
- `renderdoccmd.exe`
- `qrenderdoc.exe`
- `renderdocui.exe`
- `renderdocshim64.dll`
- `renderdoc_app.h`
- `Qt5Core.dll`、`Qt5Gui.dll`、`Qt5Widgets.dll`、`Qt5Network.dll`、`Qt5Svg.dll`
- `qtplugins/platforms/qwindows.dll`

## 6. 已完成验证

已执行并成功：

- `renderdoccmd.exe version`，确认版本为 v1.47；
- `renderdoccmd.exe test unit`；
- `renderdoccmd.exe capture --help`；
- 在补齐 Qt 依赖后实际启动 `qrenderdoc.exe`；
- Windows x64 Release 目标和相关驱动/Python 模块构建。

这些结果证明构建产物和基础命令可用，不等价于真实目标程序的多实例捕获或回放验证。

## 7. 未验证项和维护边界

当前仍未完成：

- 未使用真实模拟器、Android 程序或自有多 Vulkan 实例测试程序生成多个 `.rdc`；
- 未验证多个 `.rdc` 的资源完整性、回放顺序和跨实例同步；
- 未实现或验证 `.rdc` 自动合并；
- 未实现 FBX 导出、纹理自动导出、MCP/Python 自动分析等未属于本次目标的功能；
- 未在目标设备上测量多实例模式的捕获时间、文件大小、内存和运行时开销。
- 未验证捕获尚未结束时 Vulkan 设备/窗口注销的并发时序；当前 Profile 依赖正常结束/丢弃先于注销。

维护时先在目标程序上完成“变量关闭基线、变量开启多实例、分别打开和回放所有 `.rdc`”的验证，再考虑扩大默认范围。若验证失败，优先关闭环境变量或替换为原始 RenderDoc 交付包，不要留下半套 DLL、layer manifest 或 Qt plugins。

## 8. 符号改名的结论

本次没有实现以下改名：

```text
renderdoc__replay__marker() -> rendertest__replay__marker()
```

原因是：

1. 改名不改变捕获、序列化、shader 编译或回放的热点路径，不构成性能优化。
2. 若该函数是导出符号或被插件/脚本调用，改名会造成链接失败或运行时找不到入口。
3. 若目的是让检测工具匹配不到 RenderDoc，属于检测/反作弊规避，不在本项目交付范围内。

性能优化应使用 CPU/GPU profiling、捕获前后 A/B 数据、线程/IO/序列化分析和目标程序授权测试，而不是重命名 RenderDoc 标识。
