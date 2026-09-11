# Unity Editor 脚本离线编译校验参考

> 类型：REFERENCE；适用范围：在没有或不便打开 Unity Editor 时，校验一批 Editor C# 脚本在指定工程内是否可编译；使用前提：必须以目标工程的 Unity 版本、`Library/ScriptAssemblies`、第三方 DLL 与 define 集合重新验证，结论不能替代 Unity 内编译。
>
> 关联规则：`TOOL-VAL-01`、`TOOL-VAL-03`。迁移场景见 [Unity Editor 工具集跨工程迁移与旧件下线参考](unity-editor-tool-cross-project-migration.md)；项目实例见 [ProjectACG 网格地形与地表工具集 Profile](../Profiles/ProjectACG/mesh-surface-terrain-toolset.md)。

## 1. 适用场景

- 迁移、批量改写或合并 Editor 脚本后，先做一次快速编译回归；
- 执行环境无法启动 Unity Editor（例如只读仓库、无人值守机器）；
- 需要把「脚本层面的编译问题」与「Unity 导入/序列化/资产问题」区分开。

不适用于：asmdef 边界与引用方向、Unity 序列化迁移、`.meta`/GUID 一致性、菜单注册、资产行为、Shader 编译和 Player 构建。

## 2. 前提与依赖

| 依赖 | 路径 | 说明 |
| --- | --- | --- |
| Unity 自带 Roslyn | `<UnityEditor>/Data/DotNetSdkRoslyn/csc.dll` | 必须用它；用 `<UnityEditor>/Data/NetCoreRuntime/dotnet.exe` 运行。 |
| 参考程序集（netstandard） | `Data/NetStandard/ref/<ver>/netstandard.dll` | 与 `-nostdlib+` 配套。 |
| 参考程序集（netfx shim） | `Data/NetStandard/compat/<ver>/shims/netfx/*.dll` | 缺失会引入 `CS0012`，见 3.2。 |
| Unity 程序集 | `Data/Managed/UnityEngine.dll`、`UnityEditor.dll`、`Managed/UnityEngine/*.dll` | Editor 脚本的最小 Unity 面。 |
| 工程程序集 | `<Project>/Library/ScriptAssemblies/*.dll` | 排除 `Assembly-CSharp*`，见 3.2。 |
| 第三方 DLL | 工程内插件程序集（例如 Odin/Sirenix） | 与目标工程实际引用一致。 |
| define | 与 Unity 一致 | 至少 `UNITY_EDITOR` 与版本宏，见 3.3。 |

## 3. 实现或排查步骤

### 3.1 用 Unity 自带 Roslyn，不要用 MonoBleedingEdge 的 csc

`MonoBleedingEdge/lib/mono/msbuild/Current/bin/Roslyn/csc.exe` 是 Mono 附带的旧版本（实测 `3.7.0-5.20367.1`），语言与语义版本落后于 Unity 实际使用的编译器（`DotNetSdkRoslyn` 实测 `4.3.1-3.22526.13`）。

典型假阳性：`return a != null ? a : b;`（`RenderTexture`/`Texture2D` 收敛到 `Texture`）会被旧编译器报 `CS0173`，而同一份代码在 Unity 自带编译器下 `exit=0`。

结论：只有当编译器与 Unity 一致时，离线结论才可信。出现「只在离线校验里出现的语法错误」时，先更换编译器再下结论。

### 3.2 引用集合必须成组

- `-nostdlib+` 时必须同时引用 `netstandard.dll` 与 `netfx shim` 组；缺少后者时，以 mscorlib 为目标的第三方程序集（如 `Sirenix.*`）会报 `CS0012`（类型在 `mscorlib` 中定义）。
- 加入工程 `Library/ScriptAssemblies` 时要排除 `Assembly-CSharp*.dll`：否则会与本次参与编译的脚本形成同名类型噪音，并掩盖「该工具是否真的依赖运行时程序集」这一判断。

### 3.3 define 必须显式声明

不声明 `UNITY_EDITOR` 时，被 `#if UNITY_EDITOR` 包裹的类型与枚举不存在，会产生成片假 `CS0246`。建议至少声明：

```text
UNITY_EDITOR;UNITY_<MAJOR>_<MINOR>_OR_NEWER;UNITY_<MAJOR>_<MINOR>;UNITY_<MAJOR>_<MINOR>_<PATCH>;UNITY_STANDALONE;UNITY_STANDALONE_WIN
```

### 3.4 命令组织

引用可能达到数百个，必须写入响应文件再执行，避免命令行长度与转义问题：

```powershell
$unity  = 'C:\Program Files\Unity\Hub\Editor\<版本>\Editor\Data'
$dotnet = Join-Path $unity 'NetCoreRuntime\dotnet.exe'
$csc    = Join-Path $unity 'DotNetSdkRoslyn\csc.dll'

$args = @('-target:library','-nologo','-nostdlib+',"-langversion:$lang", "-define:$define",
          "-out:`"$out.dll`"")
foreach ($r in $refs) { $args += "-r:`"$r`"" }
$args += $sources | ForEach-Object { "`"$_`"" }
$args | Set-Content "<temp>/refs.rsp"
& $dotnet $csc -noconfig "@<temp>/refs.rsp"
```

只编译本次涉及的脚本文件，排除无关模块以减少噪音。历史告警可用 `-nowarn:<code>` 定向抑制，但不要用 `-warnaserror` 或全局屏蔽掩盖新问题。

### 3.5 结果判读

- `exit=0` 且只有已登记的历史 warning → 脚本层面通过；
- 任何 `CSxxxx` 先分类：语法/API/程序集引用属于真问题；只在离线出现的语法错误先怀疑编译器版本与 define；
- 报告必须写明：编译器版本、引用来源、define 集合、参与编译的文件数。

## 4. 风险与不适用边界

| 边界 | 说明 |
| --- | --- |
| asmdef | 不校验程序集边界与引用方向，不能证明 Editor 程序集未引用 Runtime、Runtime 未引用 `UnityEditor`。 |
| 资产身份 | 不校验 `.meta`/GUID、菜单冲突、序列化字段与资产引用。 |
| define | 只能近似 Unity 的 define 组合；Player、平台和包的 define 会随构建配置变化。 |
| 行为 | 不能替代 Unity 重导入、窗口实跑、资产写入与 Shader 编译验证。 |
| 范围 | 子集编译成功不代表整个工程可编译；它只覆盖被编译的文件与所选引用。 |

## 5. 验证与回退

| 层级 | 检查 | 通过条件 |
| --- | --- | --- |
| 编译器一致性 | 记录 `csc` 版本，并与 Unity Console 的编译结论抽样对比一次。 | 结论一致；不一致时以 Unity 为准。 |
| 引用完整性 | 观察是否出现 `CS0012`/`CS0246`。 | 无错误，或剩余错误已定位为真实缺失。 |
| define | 做一次故意去掉 `UNITY_EDITOR` 的对照。 | 对照能复现预期的成片错误，证明当前结论来自正确 define。 |
| 结论冲突 | 与 Unity 内编译结果比对。 | 冲突时以 Unity Editor 结果为准，离线脚本降级为补充证据。 |
| 回退 | 离线产物只写临时目录。 | 工程目录内不残留 `.dll`、`.rsp` 或中间文件。 |
