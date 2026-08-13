---
name: encrypted-shader-recovery-workflow
description: 在已授权且项目自带解密能力的前提下，识别 Unity 代理 Shader、定位加密源码容器、内存解密，并以行为保真方式重建独立明文 Shader 的流程与验证规则。
---

# 加密 Shader 读取与独立明文还原流程

> **用途**：处理 Unity 工程中只保留 Properties 和占位 Pass、真实实现存放在加密容器中的代理 Shader；在不修改生成物、不批量替换现有材质的前提下，恢复可审计源码并重建独立 Shader。
>
> **适用边界**：仅用于有权访问的本地工程、工程自带的加密数据和工程自带的解密程序集。不得把本文当作绕过第三方授权、破解外部商业 Shader 或传播受限源码的依据。
>
> **关联规则**：实施前遵守 Shader CORE 的 `DOC-03`、`CMP-01`、`ORG-*`、`CTL-*`、`VAL-*`；新增 Unity 资产时执行 [`project-integration-checklist.md`](project-integration-checklist.md)。

## 1. 目标、成功标准与停止条件

### 1.1 目标

完整流程不是“把某段 HLSL 抄出来”，而是交付一个满足以下条件的独立明文 Shader：

1. 找到代理 Shader 对应的真实 Shader 名称与加密源码容器。
2. 使用项目自身的合法解密入口在内存中恢复源码，并核对容器名称、长度和完整性。
3. 保存原始源码证据，不直接修改代理目录或加密容器。
4. 解密还原资产统一放入项目自有 `Assets` 下的独立目录，不与原 Shader、临时文件或报告混放。
5. 文件名和 ShaderLab 内部名称默认在原名称末尾追加统一后缀 `_Readable`；项目需要其他后缀时由 Profile 统一覆盖。
6. 根据当前工程实际 Unity、URP 和目标图形 API 核对 Include、宏和函数签名，禁止跨版本猜用 API。
7. 保持材质属性 ID、类型、默认值、Keyword、Pass、Render State 和 ShaderGUI/Drawer 契约。
8. 对“容器解密结果 → 独立明文源码”执行可审计差异检查；有加密 API 时还应完成不落盘的加密/解密回环验证。
9. 用复制的代表性材质验证切换到明文 Shader 后属性、贴图、Keyword 和材质级状态无丢失。
10. Unity 成功导入且 ShaderUtil/编译日志无本次新增错误；完成材质 A/B 后才宣称行为一致。
11. 原代理、原材质和构建链默认保持不变，回退只需停止使用新增 Shader。

### 1.2 必须停止并报告的情况

出现以下任一情况时停止，不得猜测或伪造“等价实现”：

- 工程没有提供可确认来源的解密程序集或合法入口。
- 无法证明目标 `.eng`、二进制资源或 AssetBundle 与目标 Shader 的映射关系。
- 容器元数据中的 Shader 名称、原始长度或校验信息与请求不一致。
- 解密依赖的原生库缺失、版本不匹配，或调用结果为空、乱码、截断。
- 解密源码依赖当前工程不存在的 Include、RendererFeature、全局纹理或脚本协议。
- 代理 Shader 与实际运行画面不一致，但材质替换/运行时加载链尚未查清。
- 用户没有授权输出、保存或传播恢复后的源码。

停止时应交付已确认的映射、缺失依赖、失败命令和下一步验证入口，而不是根据 Properties 猜写效果。

## 2. 先识别：它是不是代理 Shader

不要因为文件扩展名是 `.shader` 就默认它是 Source of Truth。按以下证据判断。

### 2.1 常见代理特征

- 目录名包含 `GenProxyShader`、`GenPoxyEngine`、`ProxyShader`、`PoxyShader` 等生成物语义。
- Shader 名包含 `Proxy` / `Poxy` 前缀，而材质或业务使用的原始名称不含该前缀。
- `Properties` 很完整，但 HLSL 没有声明、采样或使用这些属性。
- 贴图、Mask、Dissolve、Polar、Panner 等属性存在，片元函数却只返回固定色或通用 Lambert/Specular。
- 文件没有实际需要的 UV、顶点色、深度或屏幕坐标输入。
- 没有能够解释行为的 `#include`、`UsePass`、`Fallback` 或 `CustomEditor`。
- 工程中存在加密配置、代理生成目录、加密代码目录或材质替换程序集。

### 2.2 最小检查命令

```powershell
Get-Content -LiteralPath '<ProxyShader.shader>' -Raw

rg -n 'GenProxy|GenPoxy|ProxyShader|PoxyShader|EncryptShader|ShaderEncrypt' `
  '<PackageOrProjectRoot>'

rg -n '_MainTex|_Mask|_Polar|_Panner|#include|UsePass|CustomEditor' `
  '<ProxyShader.shader>'
```

结论必须写清楚：

- 当前文件自己实现了什么。
- 哪些 Properties 在当前文件中完全未使用。
- 实现是否可能位于 Include、UsePass、ShaderGUI、运行时替换或加密容器。
- 继续搜索的停止条件是什么。

## 3. 解析 Unity 逻辑包路径

Unity Project 窗口显示的：

```text
Packages/com.company.package/...
```

不保证物理文件位于：

```text
<ProjectRoot>/Packages/com.company.package/...
```

先读取：

```text
Packages/manifest.json
Packages/packages-lock.json
```

例如：

```json
"com.company.package": "file:local/com.company.package@1.0.0"
```

真实路径通常是：

```text
<ProjectRoot>/Packages/local/com.company.package@1.0.0/...
```

### 3.1 规则

- 必须先解析 manifest，再读取或写入文件。
- 不得因为逻辑路径不存在就断言资源缺失。
- `Packages/local`、`Packages/Local` 的大小写和实际目录以磁盘为准。
- 代理包默认视为生成/供应链资产；即使物理可写，也不得直接修改。
- 新明文 Shader 默认落在项目自有 `Assets` 目录，不写回包缓存和代理目录。

## 4. 定位加密配置、代码容器和解密组件

优先搜索以下字段和类型名：

```text
ShaderEncryptCodeOutPutPath
GetShaderEncryptCodePath
DebugShaderSourceCodePath
GenProxyShaderPath
GenPoxyShader
GenEncryptCode
ShaderCodeEntityClass
GetDecrypt
ReplacePoxyShaderMgr
MaterialShaderReplace
```

可能的证据包括：

- `ShaderConfig.asset`：记录加密代码、调试源码和代理输出目录。
- `*.eng`：按 Shader 名称哈希生成的加密容器。
- 编辑器程序集：负责生成代理、读取加密代码、恢复 Shader、替换材质。
- 原生插件：提供实际加解密函数。
- Shader Variant Collection：证明代理 GUID 被材质和构建变体引用。
- `.mat`：证明现有材质序列化到代理 Shader GUID。

### 4.1 不要只凭文件名假设映射

推荐顺序：

1. 从代理 Shader 名和材质上下文推断原始 Shader 名候选。
2. 对容器执行只读文本/二进制字符串搜索，查找容器内的明文元数据。
3. 读取容器实体的 `ShaderName` 等字段进行确认。
4. 最后才验证“Shader 名 UTF-8 MD5 = 容器文件名”的规则。
5. 映射确认后记录：代理路径、代理 GUID、真实 Shader 名、容器路径、原始长度、加密长度。

UTF-8 MD5 示例：

```powershell
$shaderName = 'Engine/Effects/Example'
$md5 = [Security.Cryptography.MD5]::Create()
try
{
    $hash = [BitConverter]::ToString(
        $md5.ComputeHash([Text.Encoding]::UTF8.GetBytes($shaderName))
    ).Replace('-', '').ToLowerInvariant()
}
finally
{
    $md5.Dispose()
}

"$shaderName -> $hash.eng"
```

MD5 在这里只承担确定性文件名映射，不承担安全校验。不同项目可能使用 GUID、SHA、索引表或 AssetBundle，不得泛化。

## 5. 安全检查程序集与容器

### 5.1 先做低风险元数据检查

在反序列化或调用原生插件前，先检查：

- DLL 名称、文件大小、时间戳和 `.meta` GUID。
- 程序集引用和可加载类型。
- 容器头、可读字符串、实体类型名、ShaderName。
- 原生插件架构是否匹配当前进程。
- 工程 Unity 版本和该工具生成数据时的版本。

程序集缺少 Unity 依赖时，`Assembly.GetTypes()` 可能抛出 `ReflectionTypeLoadException`。此时：

- 不要为了列出全部类型就安装或替换系统组件。
- 捕获 `LoaderExceptions`。
- 已知完整类型名时优先使用 `Assembly.GetType('<FullName>', false)`。
- 只反射完成当前映射与解密所需的方法，不扩大到整个工具链。

### 5.2 BinaryFormatter 边界

旧工具可能使用 `BinaryFormatter` 保存 `ShaderCodeEntityClass`。

- `BinaryFormatter` 对不可信输入不安全，可能在反序列化期间实例化任意类型。
- 只能反序列化同一受信工程、版本控制内、来源明确的容器。
- 不处理下载文件、聊天附件或来源不明的 `.eng`。
- 能使用项目提供的高层 `GetCode` / `GetDecrypt` API 时，优先于自行解析二进制格式。
- 新建批处理工具时不得继续采用 `BinaryFormatter` 作为新格式。

## 6. 使用项目自带入口在内存中解密

### 6.1 原则

- 默认只在内存中恢复。
- 首次输出写到 `Temp` 或项目外审计目录，扩展名优先 `.txt`，避免 Unity 立即导入。
- 核对 Shader 名、源码长度、开头 `Shader "..."`、Properties、Pass 和结尾闭合后，才能进入还原阶段。
- 日志只记录映射、长度、Hash 和错误，不输出完整专有源码。
- 不调用“批量替换代理 Shader”“删除原始 Shader”“刷新全部材质”等有广泛副作用的方法。

### 6.2 已验证项目适配示例

以下示例适用于已经确认具备这些类型的工程：

```text
Encrypt.ShaderCodeEntityClass
Encrypt.CSharp2CplusTools.GetDecrypt(byte[], int)
ABEncrypt.dll
EncrypModule.dll
```

使用前必须替换路径并确认类型签名，不得原样套到其他工程。

```powershell
param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectRoot,

    [Parameter(Mandatory = $true)]
    [string] $ShaderName
)

$packageRoot = Join-Path $ProjectRoot 'Packages/local/com.q1.core@1.0.0'
$managedDll  = Join-Path $packageRoot 'Editor/Script/ABEncrypt.dll'
$pluginDir   = Join-Path $packageRoot 'Editor/Plugins'
$codesDir    = Join-Path $packageRoot 'Editor/GRes/EncryptRes/Codes'

$md5 = [Security.Cryptography.MD5]::Create()
try
{
    $hash = [BitConverter]::ToString(
        $md5.ComputeHash([Text.Encoding]::UTF8.GetBytes($ShaderName))
    ).Replace('-', '').ToLowerInvariant()
}
finally
{
    $md5.Dispose()
}

$engPath = Join-Path $codesDir ($hash + '.eng')

foreach ($path in @($managedDll, $pluginDir, $engPath))
{
    if (-not (Test-Path -LiteralPath $path))
    {
        throw "缺少解密依赖：$path"
    }
}

# 让 P/Invoke 能定位项目自带的原生插件。
$env:PATH = $pluginDir + ';' + $env:PATH

$assembly = [Reflection.Assembly]::LoadFrom($managedDll)

$stream = [IO.File]::OpenRead($engPath)
try
{
    # 仅允许对受信工程内的已确认容器使用。
    $formatter = New-Object Runtime.Serialization.Formatters.Binary.BinaryFormatter
    $entity = $formatter.Deserialize($stream)
}
finally
{
    $stream.Dispose()
}

if ($entity.ShaderName -ne $ShaderName)
{
    throw "容器 ShaderName 不匹配：期望 $ShaderName，实际 $($entity.ShaderName)"
}

$decryptType = $assembly.GetType(
    'Encrypt.CSharp2CplusTools',
    $true
)

$getDecrypt = $decryptType.GetMethod(
    'GetDecrypt',
    [Reflection.BindingFlags] 'Public,Static'
)

if ($null -eq $getDecrypt)
{
    throw '未找到公开静态方法 GetDecrypt'
}

$source = $getDecrypt.Invoke(
    $null,
    @($entity.ShaderCodStr, $entity.Length)
)

if ([string]::IsNullOrWhiteSpace($source))
{
    throw '解密结果为空'
}

if ($source.Length -gt $entity.Length -or
    -not $source.TrimStart().StartsWith('Shader "'))
{
    throw '解密结果的长度或 Shader 头不符合预期'
}

# 首次只写入 Temp，不让 Unity 自动导入。
$safeName = ($ShaderName -replace '[\/:*?"<>|]', '_')
$outputDir = Join-Path $ProjectRoot 'Temp/DecryptedShaders'
$outputPath = Join-Path $outputDir ($safeName + '.shader.txt')

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
[IO.File]::WriteAllText(
    $outputPath,
    $source,
    (New-Object Text.UTF8Encoding($false))
)

Write-Output "Recovered: $ShaderName"
Write-Output "Container: $engPath"
Write-Output "SourceLength: $($source.Length)"
Write-Output "Output: $outputPath"
```

### 6.3 常见失败

| 现象 | 根因 | 处理 |
| --- | --- | --- |
| `Unable to load DLL 'EncrypModule'` | 原生插件目录不在 DLL 搜索路径。 | 在加载/调用程序集前，把项目插件目录加入当前进程 `PATH`；确认 x64/x86。 |
| `ReflectionTypeLoadException` | 缺少 UnityEditor/UnityEngine 模块，枚举全部类型失败。 | 直接按完整类型名 `GetType`；只加载所需类型。 |
| `.eng` 能找到但 ShaderName 不一致 | Hash 输入、大小写、前缀或命名规则错误。 | 读取容器元数据，不要继续解密或写文件。 |
| 源码为空或长度异常 | 插件版本、密钥、容器版本不匹配。 | 停止；比对 DLL、原生插件和容器版本。 |
| 中文乱码 | Shell 默认编码或错误的字符串编码。 | 文档和输出统一 UTF-8；读取旧文件时显式指定编码。 |
| 修改代理文件后效果没变化 | 运行时/编辑器使用解密后的真实 Shader 或替换材质。 | 查材质替换链；不要把代理当 Source of Truth。 |
| 代理改动被覆盖 | 代理目录由生成器重建。 | 新实现写入 `Assets`，不改 `GenProxy/GenPoxy`。 |

## 7. 从恢复源码重建独立明文 Shader

### 7.1 首轮只做行为保真重建

第一版不得顺手优化、改名或修正可疑逻辑。冻结以下契约：

| 契约 | 必须记录 |
| --- | --- |
| Shader 名 | 原名、代理名、新独立名；新名必须唯一。 |
| Properties | 属性 ID、类型、默认值、Drawer、Enum 编码、贴图默认值。 |
| Keyword | 名称、local/global、控制属性、分支阶段、默认状态。 |
| Pass | Name、LightMode、数量、顺序、Fallback。 |
| Render State | Queue、RenderType、Blend、Cull、ZWrite、ZTest、Stencil、ColorMask。 |
| HLSL 输入 | Attributes、Varyings、Instancing、Stereo、屏幕坐标。 |
| 资源绑定 | CBUFFER、Texture/Sampler、全局纹理、脚本设置值。 |
| 数学语义 | UV、时间、颜色空间、Mask 通道、Alpha、精度和裁剪。 |
| Inspector | `CustomEditor`、ShaderGUI 类型、PropertyDrawer、分组、显示/隐藏条件和 Keyword 同步。 |
| 外部依赖 | URP Include、项目 Include、RendererFeature、脚本消费者。 |

允许的首轮可读性改动：

- 整理缩进和空行。
- 增加解释真实公式的技术注释。
- 把重复表达式命名为局部变量。
- 在数值等价已确认时使用 `dot` 等等价表达。
- 在不改变属性 ID、Drawer、ShaderGUI 和显示逻辑的前提下整理 Inspector 文本格式。

禁止的首轮改动：

- 修改属性 ID、类型、默认值或 Enum 数字。
- 把 `half` 全部升级为 `float`，或反向降精度。
- 删除“看起来没用”的 Keyword、Pass、字段或 Render State。
- 改 Blend、ZTest、Queue、Cull、时间相位、Polar 范围或通道权重。
- 删除或替换原 `CustomEditor`，或在未验证 Keyword/Render Queue 同步前退回默认 Inspector。
- 自动修改已有材质、Prefab、Scene、SVC 或构建设置。
- 把工程私有依赖复制成所谓“通用库”。

### 7.2 输出位置与 GUID

解密还原的 Shader **必须**进入独立目录。默认目录和命名规则：

```text
Assets/<ProjectOwnedShaderRoot>/DecryptedShaders/<OriginalRelativeDirectory>/<OriginalFileName>_Readable.shader

Shader "<OriginalShaderName>_Readable"
```

要求：

- 不写入 `Library`、`PackageCache`、第三方 Package、代理目录或加密目录。
- `DecryptedShaders` 为默认独立目录名；项目 Profile 可以统一替换，但不得让每个 Shader 自选目录。
- 文件名默认在原文件名末尾追加 `_Readable`，例如 `Screen_Tx.shader` → `Screen_Tx_Readable.shader`。
- ShaderLab 内部名称默认在原 Shader 名末尾追加 `_Readable`，例如 `Engine/Effects/Screen_Tx_Readable`。
- 若必须增加项目命名空间避免全局重名，可以增加统一前缀，但末级名称仍保留“原名称 + 后缀”。
- 同一批次只能采用一个后缀；不得混用 `_Readable`、`_Decrypt`、`_New` 等临时命名。
- 独立目录内只保存正式还原资产及其 `.meta`；中间明文、反射输出和报告继续放在 `Temp` 或项目外审计目录。
- 新 `.shader` 必须使用新 GUID，禁止复用代理 GUID。
- 新目录和文档在 Unity 资产树内时补齐 `.meta`。
- GUID 必须在 `Assets` 和 `Packages` 范围内检查唯一性。
- 已有材质不自动切换；测试时复制代表性材质再切换。

### 7.3 加密、解密与还原必须做三层一致性检查

不能只以“成功得到一段 Shader 文本”作为解密正确证据。至少检查三层：

1. **容器一致性**：容器中的 `ShaderName`、声明长度、代理映射、文件名 Hash 与目标一致；不一致立即停止。
2. **加密/解密回环**：若项目公开 `GetEncrypt` 和 `GetDecrypt`，仅在内存中验证 `Decrypt(Encrypt(source)) == source`。加密算法使用随机 IV/盐时密文字节可能不同，不能用“新旧密文完全相等”作为唯一标准，也不得覆盖原 `.eng`。
3. **独立还原一致性**：保存解密原文的 SHA-256；将独立 Shader 与解密原文逐项比较 Properties、Keyword、Pass、Render State、CBUFFER、采样、数学、精度、ShaderGUI 和依赖。所有源码差异必须归类为命名/目录、格式/注释、或有版本证据的 API 适配。

只允许规范化 BOM 和换行后计算解密原文 Hash；不得通过删除注释、空白、预处理行或重排宏来制造“相同”，因为换行和预处理结构可能具有语义。独立 Shader 经过可读性整理后 Hash 不同是正常的，但差异清单必须为空或全部落入批准的允许项。

### 7.4 ShaderGUI 和 Inspector 契约尽量保持一致

- 解密源码含 `CustomEditor` 时，先定位对应 ShaderGUI C# 类型、程序集、版本和依赖，不得静默删除。
- 原 ShaderGUI 可访问且兼容新 Shader 属性 ID 时，优先继续复用同一类型。
- ShaderGUI 内若按 Shader 名、路径、Pass 或隐藏属性做硬编码判断，必须先适配新后缀名称并验证旧 Shader 不受影响。
- 原 ShaderGUI 不可复用时，在项目自有 Editor 目录实现最小兼容版本；分组、属性顺序、显示/隐藏、Toggle/Enum、Keyword、Render Queue、Blend/ZWrite 等联动尽量与原行为一致。
- PropertyDrawer、`[Toggle]`、`[Enum]`、`[KeywordEnum]`、`[HideInInspector]` 等也是 Inspector 契约，不能只复制显示属性名。
- 只能使用默认 Inspector 时，必须列出缺失行为和材质风险，不能标记为“ShaderGUI 已保持一致”。
- ShaderGUI 验证需要实际打开代表性材质，操作开关并确认序列化值、Keyword 和 Render State 同步；C# 编译成功不等于行为一致。

### 7.5 材质切换必须证明无数据丢失

属性 ID 相同只能保证序列化值有机会继续被读取，不能直接承诺材质无损。测试时先复制代表性材质，并在切换 Shader 前后生成快照。

切换前必须保存：

- 全部 Shader Properties 的名称、类型和值。
- Texture、Texture Scale、Texture Offset。
- Color、Vector、Float/Range、Int（当前 Unity 版本支持时）。
- Keyword 集合及对应控制属性。
- `renderQueue`、`enableInstancing`、`doubleSidedGI`、`globalIlluminationFlags`。
- 项目依赖的 Override Tag、Pass Enable、Blend/ZWrite/Cull 等隐藏状态。

切换流程：

1. 在复制材质上把 `material.shader` 指向 `<OriginalShaderName>_Readable`。
2. 按相同属性 ID 和兼容类型恢复快照；贴图必须同时恢复 Scale/Offset。
3. 执行原 ShaderGUI/项目已有的 `MaterialChanged`、Validate 或 Keyword 同步入口；没有公开入口时通过实际 Inspector 操作验证。
4. 再次生成快照，并逐项比较。除 Shader 引用、明确批准的后缀名称和新 GUID 外，不得出现未解释差异。
5. 任一属性、贴图、Keyword 或材质级状态丢失时，判定替换失败，禁止进入批量替换。

还必须检查：

- Toggle/Enum Drawer 是否重新同步 Keyword。
- 贴图 Scale/Offset 是使用 `_ST` 还是自定义 Vector。
- CBUFFER 类型和 SRP Batcher 布局。
- 默认值为零时，新建材质是否完全透明。
- 脚本、动画、Timeline 和 `MaterialPropertyBlock` 是否按属性名写值。
- Shader Variant Collection 是否仍只引用代理 GUID。
- 构建剥离是否保留新 Shader 的实际 Keyword 组合。

正式修改材质资产时必须使用 Undo、记录路径并限制到明确清单；材质被多个 Prefab/Renderer 共享时先报告共享资产影响范围。默认不自动修改原材质。

### 7.6 Unity、URP 与 API 版本必须以当前工程为准

还原前必须读取并记录：

```text
ProjectSettings/ProjectVersion.txt
Packages/manifest.json
Packages/packages-lock.json
目标 URP/Core/ShaderGraph package.json
目标平台 Graphics API
```

规则：

- Include 路径、宏、结构体、函数签名和返回类型以当前工程锁定的本地 URP 源码为准，不凭记忆套用其他版本示例。
- 解密源码来自不同 Unity/URP 版本时，先保存未经适配的原文，再单独记录兼容修改；版本适配不得混入“行为保真还原”且不留差异说明。
- 优先使用当前版本公开 API 和项目既有 Adapter；不得为了让旧源码编译就修改 URP Package。
- 检查 `TransformObjectToHClip`、`ComputeScreenPos`、光照/阴影入口、纹理采样宏、CBUFFER、Instancing、Stereo、Fog 和 RendererFeature 接口的当前签名。
- ShaderGUI 还要核对当前 UnityEditor 的 `ShaderUtil`、LocalKeyword、MaterialProperty 和序列化 API。
- 至少在项目实际目标 API 上产生编译记录；只在编辑器默认 DX11 或只做文本检查不足以证明 Android GLES3/Vulkan 可用。

## 8. 验证矩阵

### 8.1 L0：静态合同检查

- ShaderLab/HLSL 花括号和 `HLSLPROGRAM/ENDHLSL` 配对。
- Include 在当前锁定 URP 版本存在。
- 已记录 Unity、URP、Core/ShaderGraph 和目标图形 API 版本，使用的 API 能在对应本地包源码中定位。
- Properties、CBUFFER、Texture/Sampler 和 HLSL 使用一致。
- 解密原文 Hash 已记录，加密/解密回环（可用时）和还原差异清单已完成。
- `CustomEditor`、ShaderGUI 和 PropertyDrawer 已保留或明确记录兼容替代。
- 输出位于统一独立目录，文件名和 Shader 名符合“原名称 + `_Readable`”规则。
- 新 Shader 名和 GUID 唯一。
- 原代理、`.eng`、程序集和已有材质没有被修改。
- 解密源码、明文重建版与差异说明可审计。

### 8.2 L1：Unity 导入与编译

至少确认：

- `AssetImportWorker` 记录目标路径、GUID 和成功 artifact。
- `UnityShaderCompiler` 预处理 `ok=1`。
- 使用当前版本可用的 `ShaderUtil.ShaderHasError` / `ShaderUtil.GetShaderMessages`（或等价项目工具）检查新增 Shader；Error 数量必须为 0。
- Editor、AssetImportWorker 和 UnityShaderCompiler 日志中没有与新增 Shader 关联的 `Shader error`、`failed`。
- 代表性 API/平台产生实际编译记录；只看到静态文本检查不算 Unity 编译。
- 当前工程同时存在的旧错误必须与本次新增 Shader 分开归因。

### 8.3 L2：代表性材质 A/B

复制原材质，保持相同：

- 贴图、参数、Keyword。
- 切换前后材质快照；除 Shader 引用外，兼容属性和材质级状态必须一致。
- 相机、Renderer、灯光、时间点。
- 粒子顶点色、网格、Sorting、Render Queue。
- 质量等级、色彩空间和目标 API。

对比：

- 固定时间截图。
- 动画完整周期。
- Polar 开/关、闪烁开/关、Mask 通道组合。
- Blend 组合、深度遮挡、正反面、透明排序。
- Frame Debugger 的 Pass、Render State、绑定纹理和 Keyword。
- ShaderGUI 的分组、显隐、Toggle/Enum、Keyword、Render Queue 和隐藏 Render State 联动。

未完成 A/B 时只能说“源码与合同已还原、Unity 已导入”，不能说“画面完全一致”。

### 8.4 L3：运行时与构建

批量迁移或正式替换前还要验证：

- Android GLES3/Vulkan、Windows DX11 等目标 API。
- Shader Variant Collection、构建剥离和 AssetBundle。
- 动态加载、Addressables/资源系统、材质替换脚本。
- 多相机、Camera Stack、动态分辨率、XR/Stereo。
- 性能：变体、指令、纹理采样、透明 Overdraw、SRP Batcher。
- 真机时间动画、颜色空间和半精度表现。

## 9. 批量处理策略

后续需要处理大量 Shader 时，先把单个流程稳定，再自动化。批量工具必须采用“扫描 → 映射 → 预检 → 单体恢复 → 报告 → 人工确认 → 可选落盘”，不得一键替换全工程。

### 9.1 批处理清单

每个 Shader 记录：

```text
ProxyPath
ProxyGuid
ProxyShaderName
OriginalShaderName
ContainerPath
ContainerHash
SourceLength
SourceSHA256
DecryptStatus
EncryptDecryptRoundTripStatus
DependencyStatus
UnityVersion
URPVersion
TargetGraphicsAPI
OutputPath
OutputFolderRule
NamingSuffix
NewShaderName
NewGuid
ShaderGUIStatus
MaterialSnapshotStatus
UnityImportStatus
ShaderErrorStatus
MaterialABStatus
BuildStatus
FailureReason
```

### 9.2 批处理规则

- 默认 Dry Run，只生成映射报告。
- 所有正式输出进入统一 `DecryptedShaders`（或 Profile 指定）独立目录，并使用同一 `_Readable`（或 Profile 指定）后缀。
- 输出路径确定性生成，并检测同名、大小写和非法字符冲突。
- 每个 Shader 单独捕获失败；一个失败不得中断或污染其他结果。
- 首版单线程调用原生解密插件，除非已证明线程安全。
- 不在日志打印完整源码和密钥。
- 不自动切换材质、不自动修改 SVC、不自动删除代理/容器。
- 输出前检查目标路径，已存在文件默认拒绝覆盖。
- 对每个输出记录原容器 Hash、源码 Hash 和工具版本，便于追溯。
- 按 Unity/URP/目标 API 版本分组处理；版本不一致的 Shader 不得共用未验证的 API 适配结果。
- ShaderGUI、材质快照、ShaderUtil Error 和代表性 A/B 任一失败时，不得把该 Shader 标为可替换。
- 新源码进入版本控制前完成授权确认和代表性 A/B。
- 解密只解决“可读性”，不自动解决版权、维护权和构建链归属。

## 10. 已验证案例：com.q1.core 的 Screen_Tx

> 本节是项目适配案例，不属于跨项目固定事实。版本、路径、类型名和容器格式变化后必须重新确认。

已确认环境：

```text
Unity: 2022.3.62f3
URP: 12.1.6
Package: com.q1.core@1.0.0
Proxy: GenPoxyEngine/Engine/Effects/Screen_Tx/Screen_Tx.shader
OriginalShaderName: Engine/Effects/Screen_Tx
Container: 9968dc85896128959c18c7bd6186df06.eng
ManagedDecryptor: Editor/Script/ABEncrypt.dll
NativePlugin: Editor/Plugins/EncrypModule.dll
Entity: Encrypt.ShaderCodeEntityClass
API: Encrypt.CSharp2CplusTools.GetDecrypt(byte[], int)
```

确认结果：

- `Engine/Effects/Screen_Tx` 的 UTF-8 MD5 是 `9968dc85896128959c18c7bd6186df06`。
- 容器内 `ShaderName` 与目标一致。
- 记录的原始源码长度为 `4041`，加密字节长度为 `4048`。
- 代理 Shader 只保留 Properties 和通用光照占位 Pass。
- 解密源码实际包含屏幕 UV、Polar、Panner、Glitter、RGBA Mask、屏幕 Mask 和透明 Render State。
- 独立明文 Shader 使用新名称和新 GUID，保留原属性 ID。
- Unity AssetImportWorker 成功导入；UnityShaderCompiler GLES3 预处理/变体记录为 `ok=1`。
- 尚未完成具体材质的 Game View A/B，因此不能把导入成功等同于最终视觉一致。

## 11. 交付模板

```markdown
## 结果

- 代理 Shader：
- 真实 Shader 名：
- 加密容器：
- 独立明文 Shader：
- 新 Shader 名 / GUID：

## 保持的兼容契约

- Properties：
- Keyword：
- Pass / Render State：
- 外部依赖：

## 验证

- 映射与容器元数据：
- 解密完整性：
- Unity 导入/编译：
- 代表性材质 A/B：
- 目标平台/构建：

## 未验证与风险

- 未验证项：
- 构建链影响：
- 材质/SVC/AssetBundle 风险：
- 回退方式：
```

## 12. 最终检查清单

- [ ] 已确认用户对目标源码和解密工具具有访问权限。
- [ ] 已解析 Unity 逻辑包路径到真实物理路径。
- [ ] 已证明当前文件是代理，而不是遗漏 Include/UsePass。
- [ ] 已确认真实 Shader 名与容器一一对应。
- [ ] 只反序列化可信的工程内数据。
- [ ] 使用项目自带解密入口，未猜测密钥或格式。
- [ ] 首次解密结果只写入 Temp/项目外审计路径。
- [ ] 正式还原 Shader 位于统一独立目录，没有与原 Shader、临时文件或报告混放。
- [ ] 文件名与 ShaderLab 名称采用“原名称 + `_Readable`”（或 Profile 统一后缀），名称和 GUID 唯一。
- [ ] 已记录 Unity、URP、相关包和目标 Graphics API，并按当前本地包源码核对 API。
- [ ] 加密/解密回环（可用时）、解密原文 Hash 和还原差异清单已完成。
- [ ] 属性、Keyword、Pass、Render State、ShaderGUI/Drawer 和依赖合同已冻结。
- [ ] 没有修改代理、容器、现有材质或构建配置。
- [ ] 代表性复制材质切换前后快照一致，没有属性、贴图、Keyword 或材质级状态丢失。
- [ ] ShaderGUI 已复用或完成最小兼容实现，并验证 Inspector 联动。
- [ ] Unity 已导入，ShaderUtil 和编译日志无本次新增 Shader Error。
- [ ] 代表性材质完成固定条件 A/B，或明确标记未验证且未宣称行为一致。
- [ ] 批量处理默认 Dry Run、逐项报告、拒绝覆盖。
- [ ] 最终记录修改范围、验证证据、剩余风险和回退方式。
