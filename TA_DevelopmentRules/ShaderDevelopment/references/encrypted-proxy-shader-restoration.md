---
name: encrypted-proxy-shader-restoration
description: 面向 Unity 项目的加密代理 Shader 识别、可信解密、行为保真还原、独立明文资产生成、材质迁移和验证流程。
---

# 加密代理 Shader 明文还原与独立化流程

## 1. 目的与适用边界

本文用于处理以下类型的 Unity Shader 资产：

- Project 窗口中存在 `.shader`，但文件只保留 `Properties` 和占位 Pass，实际效果代码不可见。
- Shader 名、目录或工具配置包含 `Proxy` / `Poxy` / `GenProxyShaderPath` 等代理生成特征。
- 真实源码由项目自有工具加密到 `.eng` 等容器，并由 Editor DLL、原生插件或构建流程恢复。
- 需要在获得授权的前提下，把单个或一批 Shader 还原为可阅读、可维护、无需解密依赖的独立明文资产。

本流程不是通用破解方案。只有同时满足以下条件时才执行解密：

1. 用户或项目负责人明确授权处理目标 Shader。
2. 解密程序集、原生插件、容器和密钥均来自当前项目自身工具链。
3. 目标是项目维护、迁移、调试或行为保真重建，不绕过第三方授权、DRM、许可证或远端访问控制。
4. 全程不覆盖原始容器、代理资产、已有明文源文件或用户未确认的材质引用。

缺少授权、工具来源不可信、版本/架构无法确认、解密结果结构异常或目标文件已存在时，立即停止，不猜测密钥、不替换文件、不批量推进。

## 2. 心智模型

必须区分四层资产，不能把代理文件当成真实实现：

```text
材质序列化引用
    ↓
代理 Shader（保留 GUID、Properties、占位 Pass）
    ↓ Editor/构建工具链
加密代码容器（例如 Codes/<hash>.eng）
    ↓ 项目自带托管 DLL + 原生解密插件
真实 Shader 源码 / Unity 内存中的 Shader 资产
```

代理 Shader 的主要职责通常是：

- 保留材质属性 ID、类型、默认值和 Inspector 入口。
- 保留稳定 GUID，避免材质丢失 Shader 引用。
- 在真实代码不可用时提供可导入的占位实现。
- 配合 Editor 工具或构建流程把材质临时替换到真实 Shader。

因此，代理文件能导入、能显示属性、甚至能渲染一个简单颜色，不代表其中包含真实效果。

## 3. 快速判定清单

命中以下多项时，优先按代理/加密链路调查：

- Shader 名为 `PoxyEngineShader/...`、`Proxy/...` 或明显不是材质原始名称。
- 路径位于 `GenPoxyEngine`、`GenProxyShader`、`PoxyShaders` 等生成目录。
- `Properties` 很多，但 `vert` / `frag` 完全不读取这些属性。
- 代理 Pass 只输出通用 Lambert、固定颜色或错误占位效果。
- 文件没有可疑 `#include`、`UsePass` 或 `CustomEditor`，仍看不到效果逻辑。
- 项目中存在 `ShaderEncryptCodeOutPutPath`、`GetShaderEncryptCodePath`、`DebugShaderSourceCodePath`、`GenProxyShaderPath`。
- 工具目录存在 `.eng`、`ABEncrypt.dll`、`EncrypModule.dll` 或类似文件。
- 材质和 ShaderVariantCollection 引用代理 Shader GUID，但运行画面明显不是代理 Pass 的结果。

停止搜索条件：已经确认代理文件、加密容器、解密工具和真实 Shader 名四者的对应关系；继续扫全仓库不会改变当前决策时，转入单文件提取和验证，不做无边界搜索。

## 4. 标准工作流

### EPSR-01｜先解析 Unity 逻辑包路径

Unity 显示的：

```text
Packages/com.vendor.core/...
```

不一定是物理目录。先读取：

```text
Packages/manifest.json
Packages/packages-lock.json
```

若依赖为：

```json
"com.vendor.core": "file:local/com.vendor.core@1.0.0"
```

真实文件通常位于：

```text
<ProjectRoot>/Packages/local/com.vendor.core@1.0.0/...
```

不得因为逻辑路径在文件系统中不存在，就判断源码或包缺失。后续搜索、读取 DLL、定位 `.eng` 和生成路径都使用解析后的物理包根目录。

### EPSR-02｜读取配置，确认 source of truth

优先搜索：

```text
GenProxyShaderPath
ShaderEncryptCodeOutPutPath
GetShaderEncryptCodePath
DebugShaderSourceCodePath
DebugShader
AutoRefresh
```

记录以下事实：

| 项目 | 必须确认的内容 |
| --- | --- |
| 代理目录 | 哪些 `.shader` 是生成物，是否会被刷新覆盖。 |
| 容器目录 | `.eng` 或其他加密文件的实际位置。 |
| 调试源码目录 | 工具能否按配置输出临时明文。 |
| 自动刷新 | 打开 Unity、脚本重载或构建时是否自动替换材质。 |
| 排除项 | 哪些 Shader 不经过加密流程。 |
| 工具版本 | Editor DLL、原生插件、Unity/URP 版本是否匹配。 |

代理目录一律视为生成物：只能读取和比对，不直接把它改成最终明文实现。

### EPSR-03｜确定真实 Shader 名与容器映射

不要只通过删除 `PoxyEngineShader/` 前缀猜真实名称。优先级如下：

1. 解密容器内序列化的 `ShaderName`。
2. 项目工具公开的 `GetShaderName` / `GetCode` / `GetPoxyShader` 等 API。
3. 配置、材质迁移表或构建日志。
4. 只有前三者缺失时，才把命名规则作为待验证假设。

当前已验证工具链采用：

```text
容器文件名 = UTF-8(真实 Shader 名) 的 MD5 小写十六进制 + ".eng"
```

例如：

```text
Engine/Effects/Example
    ↓ UTF-8 MD5
<32位小写哈希>.eng
```

MD5 这里只是文件定位键，不是安全校验。找到文件后仍必须反序列化并检查容器中的 `ShaderName` 与目标完全一致，防止映射错误、旧版本残留或极低概率哈希冲突。

### EPSR-04｜只用可信项目容器做反序列化

当前 `.eng` 容器是 .NET `BinaryFormatter` 序列化的 `Encrypt.ShaderCodeEntityClass`，主要字段为：

```text
ShaderName   : string
ShaderCodStr : byte[]
Length       : int
```

安全边界：

- `BinaryFormatter` 反序列化不安全，只能读取当前项目工具生成且来源可信的本地文件。
- 禁止反序列化下载文件、聊天附件、未知仓库产物或用户无法确认来源的 `.eng`。
- 必须先加载与容器版本匹配的项目程序集，否则类型无法解析。
- 不需要为了取得数据类而强行加载所有 UnityEditor 类型；程序集 `GetTypes()` 因 Unity 依赖失败时，可直接用 `Assembly.GetType("Encrypt.ShaderCodeEntityClass")` 获取目标类型。

反序列化后先输出元数据，不立即写入 `Assets`：

```text
容器路径
ShaderName
声明长度
加密字节长度
工具程序集版本
原生插件路径和架构
```

### EPSR-05｜使用项目自带公开入口解密

当前工具链的已验证入口为：

```text
托管程序集：Editor/Script/ABEncrypt.dll
原生插件：  Editor/Plugins/EncrypModule.dll
类型：      Encrypt.CSharp2CplusTools
方法：      public static string GetDecrypt(byte[] encryptedBytes, int encryptedLengthOutput)
```

若报错：

```text
Unable to load DLL 'EncrypModule'
```

先检查：

1. 原生 DLL 是否存在。
2. 当前 PowerShell/Unity 进程位数是否与 DLL 一致。
3. 调用解密方法前，插件目录是否已加入当前进程 `PATH`。
4. `.meta` 中的 Editor/平台启用范围是否符合当前环境。
5. 托管 DLL 与原生 DLL 是否来自同一工具版本。

不得从网络下载同名 DLL，也不得用其他版本的插件“试到能跑为止”。

### EPSR-06｜单 Shader 只读提取脚本模板

下面的 PowerShell 模板只适用于当前已确认的 `ABEncrypt + EncrypModule + ShaderCodeEntityClass` 工具链。先把输出写到 `Assets` 之外审查；默认拒绝覆盖目标文件。

```powershell
param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectRoot,

    [Parameter(Mandatory = $true)]
    [string] $CorePackageRoot,

    [Parameter(Mandatory = $true)]
    [string] $ShaderName,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$codesDir = Join-Path $CorePackageRoot 'Editor\GRes\EncryptRes\Codes'
$managedDll = Join-Path $CorePackageRoot 'Editor\Script\ABEncrypt.dll'
$nativePluginDir = Join-Path $CorePackageRoot 'Editor\Plugins'
$nativeDll = Join-Path $nativePluginDir 'EncrypModule.dll'

foreach ($required in @($ProjectRoot, $CorePackageRoot, $codesDir, $managedDll, $nativeDll)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Required path not found: $required"
    }
}

$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$assetsFullPath = [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot 'Assets')).TrimEnd('\', '/')
$isInsideAssets =
    $outputFullPath.Equals($assetsFullPath, [System.StringComparison]::OrdinalIgnoreCase) -or
    $outputFullPath.StartsWith($assetsFullPath + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)

if ($isInsideAssets) {
    throw "Audit output must stay outside Assets: $outputFullPath"
}

if (Test-Path -LiteralPath $outputFullPath) {
    throw "Refusing to overwrite existing output: $outputFullPath"
}

$md5 = [System.Security.Cryptography.MD5]::Create()
try {
    $nameBytes = [System.Text.Encoding]::UTF8.GetBytes($ShaderName)
    $hash = ([System.BitConverter]::ToString($md5.ComputeHash($nameBytes))).Replace('-', '').ToLowerInvariant()
}
finally {
    $md5.Dispose()
}

$containerPath = Join-Path $codesDir ($hash + '.eng')
if (-not (Test-Path -LiteralPath $containerPath)) {
    throw "Encrypted Shader container not found: $containerPath"
}

# 必须在反序列化前加载定义 ShaderCodeEntityClass 的可信项目程序集。
$assembly = [System.Reflection.Assembly]::LoadFrom($managedDll)

$stream = [System.IO.File]::OpenRead($containerPath)
try {
    # 只允许读取当前项目自己生成、来源可信的 .eng。
    $formatter = New-Object System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
    $entity = $formatter.Deserialize($stream)
}
finally {
    $stream.Dispose()
}

if ($entity.ShaderName -cne $ShaderName) {
    throw "Shader name mismatch. Requested='$ShaderName', Container='$($entity.ShaderName)'"
}

# 让当前进程能找到项目配套的原生解密插件。
$previousPath = $env:PATH
$env:PATH = $nativePluginDir + ';' + $env:PATH
try {
    $decryptType = $assembly.GetType('Encrypt.CSharp2CplusTools', $true)
    $method = $decryptType.GetMethod(
        'GetDecrypt',
        [System.Reflection.BindingFlags]'Public,Static'
    )

    if ($null -eq $method) {
        throw 'Public GetDecrypt method was not found.'
    }

    $source = $method.Invoke($null, @($entity.ShaderCodStr, $entity.Length))
}
finally {
    $env:PATH = $previousPath
}

if ([string]::IsNullOrWhiteSpace($source) -or -not $source.TrimStart().StartsWith('Shader "')) {
    throw 'Decrypted output is empty or is not a ShaderLab source.'
}

$outputDirectory = Split-Path -Parent $outputFullPath
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputFullPath, $source, $utf8NoBom)

[PSCustomObject]@{
    ShaderName = $entity.ShaderName
    Container = $containerPath
    DeclaredLength = $entity.Length
    EncryptedBytes = $entity.ShaderCodStr.Length
    Output = $outputFullPath
}
```

注意：解密结果字符数可能因 CRLF/LF、末尾换行或编码转换与容器的 `Length` 不完全一致。不能仅凭字符串长度判失败；必须同时验证 Shader 名、ShaderLab 结构、Properties、Pass 和 Unity 导入结果。

### EPSR-07｜先审计源码，再生成独立 Shader

解密后按下列顺序建立行为清单：

1. Shader 原始名称、Fallback、CustomEditor。
2. 所有 Property ID、类型、默认值、Drawer、Enum 数值。
3. CBUFFER 字段、Texture/Sampler、脚本或 MaterialPropertyBlock 写入。
4. Pass 名、`LightMode`、Queue、RenderType。
5. Blend、ZWrite、ZTest、Cull、ColorMask、Stencil、Offset。
6. `shader_feature` / `shader_feature_local` / `multi_compile`、宏分支和互斥关系。
7. Vertex/Fragment 输入输出、屏幕坐标、深度、雾、阴影和目标平台 pragma。
8. Include 的真实版本和当前项目 URP 兼容性。
9. 材质、Prefab、Scene、Animation、Timeline、脚本、Variant Collection 和 AssetBundle 消费者。

独立明文 Shader 必须：

- 放在授权的 `Assets/...` 源码目录，而不是代理生成目录。
- 使用新的、唯一的 Shader 名，避免与加密真实 Shader 或代理 Shader 冲突。
- 保留所有序列化 Property ID，包括历史拼写错误；显示名可以改善，ID 不得顺手更名。
- 保留 Enum 数值、默认值、Keyword 名、Local/Global 作用域和 Render State。
- 保留原有材质行为；改变默认值、Blend、精度、Pass 或分支属于独立 Look Change，不得混入还原任务。
- 新增 `.meta`，GUID 必须全项目唯一；新建文件夹处于 Unity 资产树时同步提供文件夹 `.meta`。
- 不把原始代理 GUID 复制给新 Shader。
- 不因代码可读性重构而删除看似“无用”的 Keyword、Property 或 Pass；先查消费者和构建保留链。

允许的可读性整理仅限：缩进、注释、局部变量名、等价表达式和 Inspector 显示名。任何数学精度、求值顺序、颜色空间、UV、分支、默认值或渲染状态变化都必须单独记录和验证。

### EPSR-08｜材质迁移必须保持可回退

单材质验证时：

1. 复制代表性材质，不直接改原材质。
2. 把副本切换到新的明文 Shader。
3. 检查纹理、Color/Vector/Float、Render Queue 和 Keyword 是否保留。
4. 对照 `.mat` YAML 和 Inspector，确认历史属性没有因为新 Shader 暂未声明而丢失。
5. 检查 Toggle 是否同时写 Float 与 Keyword；不要假设 `_FEATURE`、`_FEATURE_ON` 或 Local Keyword 会自动同步。
6. 检查 Animation、Timeline、脚本、`MaterialPropertyBlock` 使用的是 Property ID 还是 Shader 名。
7. 视觉确认后，才决定是否迁移其他材质。

直接写 `sharedMaterials`、修改共享材质资产或批量切换 Shader 会影响所有消费者。必须先输出影响清单、使用 Undo/备份策略，并由用户确认迁移范围。

## 5. 验证矩阵

### 5.1 静态验证

- UTF-8 严格解码，无 `U+FFFD` 和乱码。
- ShaderLab/HLSL 花括号、`HLSLPROGRAM/ENDHLSL` 成对。
- Include 在当前物理包路径存在。
- Properties 与 CBUFFER/采样代码逐项对应。
- Keyword 声明、Toggle、宏分支与真实材质 Keyword 对应。
- Pass、LightMode、Render State 与解密源一致。
- 新 Shader 名和 `.meta` GUID 唯一。
- 没有覆盖原代理、容器或既有明文源文件。

### 5.2 Unity 导入与编译

当目标项目已打开时，优先读取当前项目日志而不是再启动同一工程：

```text
<ProjectRoot>/Logs/AssetImportWorker*.log
<ProjectRoot>/Logs/shadercompiler-UnityShaderCompiler*.log
```

最小证据：

- AssetImportWorker 记录目标路径、GUID 和 artifact id。
- Shader 预处理记录 `ok=1`。
- 至少目标 API/平台和实际 Keyword 组合有 `compileSnippet ... ok=1`。
- 日志中不存在与目标 Shader 路径关联的 `Shader error`、`failed` 或 include 错误。

仅看到 `type=Vertex ... ok=1` 不代表 Fragment、所有 Keyword、所有 Pass 和所有平台都已验证。必须用代表性材质触发实际 Fragment/变体编译；涉及构建剥离时还要检查 Player 构建和 ShaderVariantCollection。

### 5.3 视觉 A/B

使用同一份：

- Mesh/Particle/UI 几何体
- 材质参数和贴图
- 相机、Renderer、质量等级和图形 API
- 时间点、动画状态和顶点色
- 灯光、深度、Opaque Texture、Camera Texture 等输入

对比加密真实 Shader 与明文 Shader：

- RGB、Alpha、Blend 和排序
- 屏幕 UV、极坐标、流动、闪烁和 Mask
- ZTest/ZWrite、遮挡、Cull 和 Stencil
- Game View、Scene View、目标设备
- Frame Debugger 中 Pass、Draw 顺序、Keyword 和纹理绑定

通过导入和编译不等于视觉一致；没有 A/B 时必须明确写“源码/编译已验证，视觉未验证”。

### 5.4 构建与加载

若原链路涉及 AssetBundle、Addressables、热更或运行时 Shader 替换，还需确认：

- 新 Shader 是否进入正确 Bundle/Addressables Group。
- 构建剥离是否保留实际 Keyword 变体。
- 运行时是否仍按旧 Shader 名查找。
- 原替换工具是否会把新材质重新指回代理 Shader。
- 独立明文 Shader 是否违反发布包的源码/IP 管理规则。

## 6. 批量处理规则

### EPSR-09｜先建清单，单个试点后批量

批量前建立表格：

| 字段 | 内容 |
| --- | --- |
| Real Shader Name | 容器内确认的真实 Shader 名。 |
| Proxy Path / GUID | 代理资产及稳定引用。 |
| Container Path | `.eng` 或其他容器。 |
| Output Path / New Name | 新明文资产位置和唯一 Shader 名。 |
| Material Count | 直接材质消费者数量。 |
| Property/Keyword/Pass 摘要 | 序列化和变体边界。 |
| Decrypt Status | 未处理/成功/异常。 |
| Unity Compile Status | API、Pass、Keyword 证据。 |
| Visual Status | 未验证/部分/通过。 |
| Migration Status | 未迁移/试点/已确认批量。 |

先选一个结构简单、材质明确的 Shader 走完整闭环。只有它通过“容器映射 → 解密 → 独立化 → Unity 编译 → 材质 A/B → 回退”后，才复用脚本批量提取。

批量提取和批量迁移必须分开：

1. 第一阶段只生成项目外的明文审计副本和清单。
2. 第二阶段生成 `Assets` 下独立 Shader 与 `.meta`。
3. 第三阶段只迁移用户确认的材质集合。
4. 每阶段都能按清单删除新增资产或恢复材质引用。

不得一次性解密全部 `.eng` 并直接写入 `Assets`，也不得自动扫描后批量替换所有共享材质。

### EPSR-10｜自动化输出必须可审计

自动化脚本至少记录：

```text
时间
工具版本
真实 Shader 名
容器路径与哈希
输出路径与 GUID
是否覆盖（必须为 false）
解密结果校验
Property/Keyword/Pass 摘要
Unity artifact/编译证据
错误与跳过原因
```

脚本默认行为：

- `WhatIf` / Dry Run 优先。
- 输入列表显式，不以全仓库扫描结果直接执行。
- 输出已存在时失败，不覆盖。
- 单文件失败不继续迁移其材质。
- 不删除代理、容器或原始源码。
- 不提交、不推送、不改构建配置。

## 7. 常见失败与根因

| 现象 | 根因 | 处理 |
| --- | --- | --- |
| `Packages/com.vendor...` 物理路径不存在 | Unity 逻辑包映射到 `file:local/...` | 读取 manifest/lock，使用真实包根。 |
| 代理属性很多但效果代码不存在 | 当前文件就是生成代理 | 查 `GenProxyShaderPath` 和 `.eng`，不要继续找不存在的 include。 |
| `.eng` 能看到 Shader 名，其余是乱码 | 容器元数据明文、代码字节已加密 | 用匹配版本的项目 API 解密，不做字符串拼接。 |
| `GetTypes()` 报 UnityEditor 依赖缺失 | 在普通 PowerShell 中反射 Unity Editor 程序集 | 直接 `Assembly.GetType()` 获取数据类/解密类，避免遍历全部类型。 |
| `Unable to load DLL 'EncrypModule'` | 原生插件未进入 DLL 搜索路径、架构或版本不匹配 | 调用前设置当前进程 PATH，核对位数和配套版本。 |
| 解密字符串长度与 `Length` 不完全相同 | 换行、编码、终止符或工具长度语义不同 | 验证 ShaderLab 结构和 Unity 导入，不只比长度。 |
| 新材质完全透明 | 忠实保留的原默认 Color/Alpha/Mask/Tiling 为 0 | 用已有材质副本迁移；新材质显式配置非零参数，不擅改还原默认值。 |
| Unity 导入成功但画面不一致 | Keyword、Render State、默认值、顶点色、时间或屏幕输入未对齐 | 用同输入 A/B 和 Frame Debugger 定位。 |
| 编译日志只有 Vertex `ok=1` | Unity 尚未触发 Fragment/其他变体 | 绑定代表性材质并触发目标 Pass/API，再查日志。 |
| 修改代理后很快被还原 | 代理目录由工具重新生成 | 把明文 Shader 放入独立授权目录，禁止改生成物。 |
| 材质切换后参数丢失 | Property ID、类型、Enum 数值或 Keyword 改名 | 还原历史 ID；显示名与内部 ID 分离。 |

## 8. 完成交付检查表

- [ ] 已确认授权和工具来源。
- [ ] 已解析逻辑包到真实物理路径。
- [ ] 已确认代理、容器、真实 Shader 名和工具版本映射。
- [ ] 只反序列化可信的项目本地容器。
- [ ] 解密输出先写入 `Assets` 外，且没有覆盖文件。
- [ ] 已审计 Properties、Keywords、Pass、Render State、Includes 和消费者。
- [ ] 新 Shader 名、路径和 GUID 唯一。
- [ ] Property ID、类型、默认值、Enum、Keyword 和 Render State 保真。
- [ ] 未修改代理目录、容器和原始材质。
- [ ] Unity AssetImportWorker 已成功导入。
- [ ] 目标 Fragment、Pass、Keyword 和图形 API 已触发编译。
- [ ] 代表性材质完成视觉 A/B；若未完成已明确标注。
- [ ] AssetBundle/Addressables/构建剥离链路按需验证。
- [ ] 批量处理有显式清单、试点、日志和回退边界。

## 9. 不推荐方案

- 直接把代理 Shader 的占位 `frag` 当真实实现继续修改。
- 只根据 Properties 猜测效果并重写，却不读取加密源。
- 把解密源码覆盖回 `GenPoxyEngine` 等生成目录。
- 给新 Shader 复制代理 GUID，制造 GUID 冲突。
- 为了“更合理”顺手修改历史默认值、拼写错误 ID、Blend 或 Keyword。
- 仅凭静态阅读、括号检查或一个 Vertex `ok=1` 宣称完成。
- 在未确认共享材质影响时批量切换 Shader。
- 对未知 `.eng` 使用 `BinaryFormatter`，或加载来源不明的托管/原生 DLL。
- 将明文源码纳入发布、版本库或外发包，却未确认项目 IP 与保密策略。