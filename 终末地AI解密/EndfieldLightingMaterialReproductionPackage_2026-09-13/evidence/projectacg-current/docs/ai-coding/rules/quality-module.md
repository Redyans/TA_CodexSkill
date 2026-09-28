# 性能分级模块开发规范

## 模块概述

QualityModule（性能分级模块）负责管理游戏的画质档位和渲染质量，提供：
- **自动设备检测**：识别CPU/GPU型号、内存、平台等硬件信息
- **4档质量分级**：Low（低配）/ Mid（中配）/ High（高配）/ Ultra（超高配）
- **场景分离配置**：Battle（战斗）和 Lobby（大厅）场景独立管理质量档位
- **运行时动态切换**：支持玩家手动调整质量或根据场景自动切换URP管线

## 核心架构

```
QualityModule/
├── Data/                    # 数据层（设备库、配置值）
├── Device/                  # 设备检测层
├── Settings/                # 质量管理层
├── Runtime/                 # 运行时组件（URP切换、事件）
└── Editor/                  # 编辑器工具
```

### 质量配置的分层真相源

- `Client/ProjectSettings/QualitySettings.asset` 是 Unity Quality Level 的工程级真相源，负责每档的 Shadowmask、实时 Reflection Probe、VSync、LOD、Unity 管线引用等引擎基线；它与 `Client/Assets/AssetRaw/Settings/URP-*.asset`、`QualityAssetRegistry` 管理的场景 URP 资源不是同一层。不要只检查 URP Asset 就断言完整画质配置。
- `Client/Assets/Resources/QualitySettings.asset` 是 Player 运行时的 URP 映射真相源：`QualityAssetRegistry` 通过 `Resources.Load<QualitySettingsAsset>("QualitySettings")` 读取，并按 `ESceneType × EDeviceQuality` 返回同一组 `UniversalRenderPipelineAsset` 实例。它不是 Unity `ProjectSettings/QualitySettings.asset`，也不再通过 YooAsset 运行时加载第二套管线。
- `EDeviceQuality` 的索引是 `Low=0 / Mid=1 / High=2 / Ultra=3`；当前 Unity Quality Level 名称对应 `Low / Middle / High / Ultra`。`QualityPipelineSwitcherWindow` 只有勾选“同时切换 QualityLevel”时才按该索引调用 `QualitySettings.SetQualityLevel(...)`，场景管线切换本身由 `URPQualitySwitcher` 处理。
- 当前 `High` / `Ultra` 的工程基线均使用 Shadowmask（`shadowmaskMode: 0`）、关闭实时 Reflection Probe（`realtimeReflectionProbes: 0`）并关闭 VSync（`vSyncCount: 0`）；进入运行时后 `PerformanceSettingApplier.ApplyFrameRate(...)` 还会再次把 `QualitySettings.vSyncCount` 设为 `0`，再写 `Application.targetFrameRate`。调整烘焙混合光照、反射探针或帧率策略时，应同时核对 `QualitySettings.asset`、运行时覆盖代码和当前场景 URP 资源，避免修改一层却被另一层覆盖。
- 最小验证：静态核对 `QualitySettings.asset`、`QualityPipelineSwitcherWindow.cs`、`PerformanceSettingApplier.cs` 和目标 `URP-*.asset`；随后在 Unity 的 Project Settings / Quality 与 Pipeline Switcher 中分别切换 High、Ultra，并在 Lobby / Battle 场景确认实际管线、混合光照、反射和帧率。纯文本 diff 不能替代该运行态检查。

## 快速使用

### 1. 游戏启动时自动检测

```csharp
// 已在 GameApp.cs 中集成，游戏启动时自动执行
public static void Entrance(object[] objects)
{
    RegisterHotfixModules();
    // QualityModule 会在 OnInit() 时自动检测设备并应用推荐档位
}
```

### 2. 场景切换时应用质量

```csharp
// 进入战斗场景
public void OnEnterBattle()
{
    GameModule.Quality.ApplyForScene(ESceneType.Battle);
}

// 返回大厅
public void OnEnterLobby()
{
    GameModule.Quality.ApplyForScene(ESceneType.Lobby);
}
```

### 3. 玩家手动设置质量

```csharp
// 设置全局质量档位（所有场景生效）
public void OnQualitySettingChanged(int qualityIndex)
{
    var quality = (EDeviceQuality)qualityIndex;
    GameModule.Quality.SetDeviceQuality(quality, saveToPrefs: true);
    
    // 立即应用到当前场景
    ESceneType currentScene = GetCurrentSceneType();
    GameModule.Quality.ApplyForScene(currentScene);
}

// 为特定场景设置不同档位（场景分离）
public void SetBattleQuality(EDeviceQuality quality)
{
    GameModule.Quality.SetSceneQuality(ESceneType.Battle, quality, saveToPrefs: true);
}

public void SetLobbyQuality(EDeviceQuality quality)
{
    GameModule.Quality.SetSceneQuality(ESceneType.Lobby, quality, saveToPrefs: true);
}
```

### 4. 监听质量变更事件

```csharp
using GameLogic;
using TEngine;

public class QualityObserver
{
    public void Initialize()
    {
        // 监听设备质量档位变更
        GameEvent.AddEventListener<EDeviceQuality>(
            QualityEventDefine.OnDeviceQualityChanged, 
            OnDeviceQualityChanged);
        
        // 监听场景质量应用
        GameEvent.AddEventListener<int, int>(
            QualityEventDefine.OnSceneQualityApplied, 
            OnSceneQualityApplied);
    }
    
    private void OnDeviceQualityChanged(EDeviceQuality newQuality)
    {
        Log.Info($"设备质量档位已变更: {newQuality}");
        // 更新UI显示
    }
    
    private void OnSceneQualityApplied(int sceneType, int quality)
    {
        Log.Info($"场景质量已应用: Scene={sceneType}, Quality={quality}");
    }
    
    public void Cleanup()
    {
        GameEvent.RemoveEventListener<EDeviceQuality>(
            QualityEventDefine.OnDeviceQualityChanged, 
            OnDeviceQualityChanged);
        GameEvent.RemoveEventListener<int, int>(
            QualityEventDefine.OnSceneQualityApplied, 
            OnSceneQualityApplied);
    }
}
```

## 编辑器工具使用

### Pipeline Switcher 窗口

**打开方式**: Unity菜单 → `TEngine/Quality/Pipeline Switcher`

**功能**:
1. **快速切换URP管线**：选择场景类型（Battle/Lobby）和质量档位（Low/Mid/High/Ultra）
2. **实时预览**：显示目标管线的关键参数（MSAA、渲染缩放、阴影等）
3. **批量检查**：一键检查所有8组管线配置是否完整
4. **配置保存/恢复**：保存当前配置并支持一键还原
5. **自动记忆**：切换场景类型时自动恢复该场景上次选择的质量档位
6. **持久化**：每次切换质量档位都会自动保存到 EditorPrefs，运行游戏时自动使用
7. **同时切换QualityLevel选项**：勾选后，切换管线时同时修改Unity的QualityLevel；未勾选时，保持原有QualityLevel不变

**EditorPrefs 键**:
- `QualityPipelineSwitcher_BattleQuality` - Battle场景的质量档位
- `QualityPipelineSwitcher_LobbyQuality` - Lobby场景的质量档位
- `QualityModule_EditorQuality` - 当前选择的质量档位（ApplyPipeline时保存）
- `QualityModule_EditorSceneType` - 当前选择的场景类型（ApplyPipeline时保存）

**使用流程**:
1. 打开 Pipeline Switcher 窗口
2. 选择场景类型（Battle/Lobby）- 自动恢复该场景上次选择的质量档位
3. 选择质量档位（Low/Mid/High/Ultra）- 自动保存到 EditorPrefs
4. （可选）勾选"同时切换QualityLevel"选项
5. 点击 `Apply Pipeline` 按钮
6. 配置会自动保存到 EditorPrefs（`QualityModule_EditorQuality` / `EditorSceneType`）
7. 运行游戏时，QualityModule 会自动读取这些配置

**运行时配置加载**:
- 游戏启动时（PlayMode），`QualityModule.OnInit()` 会优先读取 EditorPrefs 中的配置
- 如果有配置，直接使用；如果没有，执行自动检测
- `SceneQualityManager.LoadFromPrefs()` 会从 EditorPrefs 读取场景质量配置
- 如果没有配置，默认使用 `High` 档位

### PlayMode 测试工具

**打开方式**: Unity菜单 → `TEngine/Quality/PlayMode/...`

**功能**:
- `Apply Lobby Low` - 应用大厅低配
- `Apply Lobby Mid` - 应用大厅中配
- `Apply Lobby High` - 应用大厅高配
- `Apply Lobby Ultra` - 应用大厅超高配
- `Apply Battle Low` - 应用战斗低配
- `Apply Battle Mid` - 应用战斗中配
- `Apply Battle High` - 应用战斗高配
- `Apply Battle Ultra` - 应用战斗超高配
- `Auto Detect & Apply` - 自动检测设备并应用推荐档位

**注意**: PlayMode工具仅在运行时（Play Mode）可用，走运行时 QualityModule 逻辑。所有切换操作都会保存到 EditorPrefs。

### 快捷菜单

**打开方式**: Scene视图右键 → `GameObject/Quality/...`

提供快速切换预设（Battle Low/High, Lobby Low/High）。切换操作会保存到 EditorPrefs。

### 运行时质量切换（代码方式）

**使用 RuntimeQualityHelper**:
```csharp
// 快捷切换
RuntimeQualityHelper.BattleMid();
RuntimeQualityHelper.LobbyUltra();

// 自定义切换
RuntimeQualityHelper.SwitchQuality(ESceneType.Battle, EDeviceQuality.High, saveToPrefs: true);

// 自动检测
RuntimeQualityHelper.AutoDetect(saveToPrefs: true);

// 打印状态
RuntimeQualityHelper.PrintCurrentQuality();
```

## URP资源管理

### 资源位置

所有URP管线配置资产位于：
```
Client/Assets/AssetRaw/Settings/URP-*.asset
```

Player 运行时还必须提交：

```
Client/Assets/Resources/QualitySettings.asset
```

该 ScriptableObject 为 Lobby/Battle 的 Low/Mid/High/Ultra 建立序列化引用。修改或新增 URP Asset 时，先在该映射资产中补齐对应行；仅把 `.asset` 放进 `AssetRaw/Settings` 不能让 Player 运行时发现它。

### 命名规则

```
URP-{SceneType}-{QualityName}.asset
```

**示例**:
- `URP-Battle-Low.asset`
- `URP-Lobby-HighFidelity.asset`
- `URP-Battle-Ultra.asset`

**注意**: Lobby的High档使用 `HighFidelity` 命名（保留美术习惯）。

### 资源加载

编辑器与 Player 运行时都通过 `QualityAssetRegistry` 读取 `Assets/Resources/QualitySettings.asset`：

```csharp
if (QualityAssetRegistry.TryGetPipeline(sceneType, quality, out var pipeline))
{
    QualitySettings.renderPipeline = pipeline;
}
```

`URPQualitySwitcher` 不再调用 `GameModule.Resource.LoadAsset` 或按字符串路径查找管线；映射缺失时记录错误并跳过切换。`Assets/AssetRaw/Settings/URP-*.asset` 是被 `QualitySettings.asset` 引用的源资源，不能只修改文件名或依赖 YooAsset 收集规则。

## 配置扩展

### 1. 添加新设备到性能数据库

编辑 `DeviceSpecDatabase.cs`：

```csharp
public static class DeviceSpecDatabase
{
    private static readonly Dictionary<string, DeviceSpec> SpecByKey = new(StringComparer.OrdinalIgnoreCase)
    {
        // 添加新设备
        ["Snapdragon 8 Gen 3"] = new DeviceSpec("Snapdragon 8 Gen 3", 95, 92, "Android"),
        ["Apple A17 Pro"] = new DeviceSpec("Apple A17 Pro", 98, 95, "iOS"),
        // ... 更多设备
    };
}
```

**参数说明**:
- **Name**: 设备名称（用于日志和调试）
- **CpuScore**: CPU性能分数（0-100）
- **GpuScore**: GPU性能分数（0-100）
- **Platform**: 平台标识（Android/iOS/Windows等）

### 2. 配置性能设置值（推荐：使用编辑器）⭐

**推荐方式**: 在编辑器中通过 ScriptableObject 配置

#### 创建配置资产

```
菜单: TEngine > Quality > Create Quality Config Asset
位置: Assets/Resources/QualityConfigAsset.asset
```

**注意**: 配置文件必须放在 `Assets/Resources/` 目录下，运行时才能通过 `Resources.Load()` 加载。

#### 编辑配置

1. 打开配置资产: `TEngine > Quality > Locate Quality Config Asset`
2. 在 Inspector 中编辑（使用Odin表格界面，分6个标签页）
3. 保存配置（Ctrl+S）

**可配置项**（使用Odin Inspector表格编辑）:
- **性能分档阈值**: 定义不同质量档位的性能分数范围和推荐内存
- **渲染设置**（标签页）: MSAA级别、渲染分辨率缩放
- **阴影设置**（标签页）: 阴影分辨率、阴影距离、软阴影
- **光照设置**（标签页）: 附加光源数量、附加光源阴影
- **后处理设置**（标签页）: 泛光、颜色分级、动态模糊、景深、屏幕扭曲
- **角色/动画设置**（标签页）: 蒙皮权重数、动画LOD
- **LOD/贴图设置**（标签页）: LOD偏移、贴图质量
- **帧率设置**（标签页）: 大厅帧率上限、战斗帧率上限

**编辑器功能**:
- **重置为默认值**按钮: 一键恢复所有配置到默认值
- **验证配置**按钮: 检查配置完整性（质量分档数量、设置类型重复等）

**加载机制**:
- **编辑器模式**: 通过 `AssetDatabase.FindAssets()` 查找配置资产
- **运行时模式**: 通过 `Resources.Load<QualityConfigAsset>("QualityConfigAsset")` 加载
- **兜底机制**: 如果未找到配置资产，使用 `DefaultQualityConfig.cs` 中的硬编码默认值

**详细说明**: 查看 [QualityModule README](../../../Client/Assets/GameScripts/HotFix/GameLogic/Module/QualityModule/README.md)

### 3. 代码方式配置（兜底）

如果未创建配置资产，系统会使用 `DefaultQualityConfig.cs` 中的硬编码默认值：

```csharp
// 硬编码配置作为兜底
private static readonly Dictionary<EPerformanceSettingType, int[]> PerformanceValuesHardcoded = new()
{
    // [Low, Mid, High, Ultra]
    { EPerformanceSettingType.MsaaLevel, new[] { 0, 2, 2, 4 } },
    { EPerformanceSettingType.RenderScale, new[] { 75, 85, 90, 100 } },
    // ... 更多设置项
};
```

## 最佳实践

### 1. 场景切换时机

```csharp
// ✅ 推荐：在场景加载完成后应用质量
public class BattleSceneLoader
{
    public async UniTask LoadBattleSceneAsync()
    {
        await GameModule.Scene.LoadSceneAsync("BattleScene");
        
        // 场景加载完成后立即应用质量
        GameModule.Quality.ApplyForScene(ESceneType.Battle);
    }
}
```

### 2. 质量设置UI设计

```csharp
// ✅ 推荐：提供全局档位 + 场景分离选项
public class QualitySettingsUI : UIWindow
{
    private Dropdown _globalQualityDropdown;
    private Toggle _useSeparateSettings;
    private Dropdown _battleQualityDropdown;
    private Dropdown _lobbyQualityDropdown;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        // 初始化显示
        _globalQualityDropdown.value = (int)GameModule.Quality.DeviceQuality;
        _battleQualityDropdown.value = (int)GameModule.Quality.GetSceneQuality(ESceneType.Battle);
        _lobbyQualityDropdown.value = (int)GameModule.Quality.GetSceneQuality(ESceneType.Lobby);
        
        // 绑定事件
        _globalQualityDropdown.onValueChanged.AddListener(OnGlobalQualityChanged);
        _useSeparateSettings.onValueChanged.AddListener(OnSeparateSettingsToggled);
    }
    
    private void OnGlobalQualityChanged(int value)
    {
        var quality = (EDeviceQuality)value;
        GameModule.Quality.SetDeviceQuality(quality, saveToPrefs: true);
        
        // 如果不使用场景分离，清除覆盖
        if (!_useSeparateSettings.isOn)
        {
            GameModule.Quality.ClearSceneQualityOverride(ESceneType.Battle, saveToPrefs: true);
            GameModule.Quality.ClearSceneQualityOverride(ESceneType.Lobby, saveToPrefs: true);
        }
        
        // 立即应用到当前场景
        ApplyToCurrentScene();
    }
}
```

### 3. 性能预警机制

```csharp
// ✅ 推荐：监控帧率并动态降档
public class PerformanceMonitor : MonoBehaviour
{
    private const float LOW_FPS_THRESHOLD = 20f;
    private const int SAMPLE_COUNT = 60;
    
    private float[] _fpsSamples = new float[SAMPLE_COUNT];
    private int _sampleIndex;
    
    private void Update()
    {
        _fpsSamples[_sampleIndex] = 1f / Time.deltaTime;
        _sampleIndex = (_sampleIndex + 1) % SAMPLE_COUNT;
        
        if (_sampleIndex == 0) // 每采样一轮检查一次
        {
            float avgFps = _fpsSamples.Average();
            if (avgFps < LOW_FPS_THRESHOLD)
            {
                SuggestLowerQuality();
            }
        }
    }
    
    private void SuggestLowerQuality()
    {
        var currentQuality = GameModule.Quality.DeviceQuality;
        if (currentQuality == EDeviceQuality.Low)
        {
            return; // 已经是最低档
        }
        
        // 弹窗询问玩家是否降低画质
        ShowQualityDowngradeDialog();
    }
}
```

### 4. 测试自动化

```csharp
#if UNITY_EDITOR
[MenuItem("Tools/Quality/Test All Quality Levels")]
public static void TestAllQualityLevels()
{
    var scenes = new[] { ESceneType.Battle, ESceneType.Lobby };
    var qualities = Enum.GetValues(typeof(EDeviceQuality)).Cast<EDeviceQuality>();
    
    foreach (var scene in scenes)
    {
        foreach (var quality in qualities)
        {
            Debug.Log($"Testing: {scene} - {quality}");
            
            var pipeline = LoadPipelineAsset(scene, quality);
            if (pipeline == null)
            {
                Debug.LogError($"Missing pipeline: {scene}-{quality}");
            }
            else
            {
                Debug.Log($"✓ Pipeline found: {pipeline.name}");
            }
        }
    }
}
#endif
```

## 常见场景

### 场景1: 游戏启动时配置加载 ⭐

**编辑器模式（PlayMode）**:
```csharp
// QualityModule.OnInit() 会自动执行以下逻辑：
public override void OnInit()
{
#if UNITY_EDITOR
    // 1. 优先读取 Pipeline Switcher 保存的 EditorPrefs 配置
    if (TryLoadEditorQualitySettings())
    {
        // 使用编辑器配置
        Log.Info($"QualityModule 使用编辑器设置. DeviceQuality={DeviceQuality}");
        return;
    }
    
    // 2. 如果没有 EditorPrefs 配置，执行自动检测
    Log.Info("QualityModule 未检测到EditorPrefs配置，执行自动检测");
    AutoDetectAndApply(saveToPrefs: true);
    return;
#endif
}
```

**TryLoadEditorQualitySettings() 加载优先级**:
1. **第一优先级**: 读取 `QualityModule_EditorQuality`（ApplyPipeline时保存的当前配置）
   - 同时读取 `QualityModule_EditorSceneType` 确定场景类型
   - 设置全局设备质量和场景覆盖
   - 清除另一场景的覆盖，避免自动检测覆盖
2. **第二优先级**: 如果没有，读取 `QualityPipelineSwitcher_BattleQuality` 和 `QualityPipelineSwitcher_LobbyQuality`（Pipeline Switcher保存的场景配置）
   - 分别设置 Battle 和 Lobby 的场景质量
   - 使用 Battle 的质量作为全局设备质量
3. **第三优先级**: 如果都没有，返回 false，执行自动检测

**打包模式**:
```csharp
#if !UNITY_EDITOR
    // Windows Standalone 使用固定工程高品质基线；移动端按设备评分自动检测。
#if UNITY_STANDALONE_WIN
    ApplyWindowsHighQualityProfile();
#else
    var recommended = AutoDetectAndApply(saveToPrefs: true);
#endif
    return;
#endif
```

Windows Player 的固定基线为 Unity `QualityLevel=High`、Lobby=`HighFidelity`、Battle=`Ultra`；iOS 在自动检测后还会把 Battle 上限限制为 Mid。不要再按“所有 Player 统一 High”理解打包行为。

**SceneQualityManager.LoadFromPrefs() 加载逻辑**:

**编辑器模式**:
1. **第一优先级**: 从 EditorPrefs 读取 Pipeline Switcher 保存的配置
   - Battle: `QualityPipelineSwitcher_BattleQuality`
   - Lobby: `QualityPipelineSwitcher_LobbyQuality`
2. **第二优先级**: 如果没有，从 PlayerPrefs 读取
   - Battle: `Quality_Scene_Battle`
   - Lobby: `Quality_Scene_Lobby`
3. **默认值**: 如果都没有，默认使用 `High` 档位（`EDeviceQuality.High`）

**打包模式（手机）**:
1. **第一优先级**: 从 PlayerPrefs 读取本地设置
   - Battle: `Quality_Scene_Battle`
   - Lobby: `Quality_Scene_Lobby`
2. **默认值**: 如果没有设置，默认使用 `HighFidelity`（即 `EDeviceQuality.High`）

**GetSceneQuality() 默认值**:
- 如果没有场景覆盖值（`_sceneOverride[sceneType] == -1`），默认返回 `EDeviceQuality.High`
- **不再使用 fallbackDeviceQuality 参数**，确保在没有配置的情况下，所有场景都使用 HighFidelity 档位
- 这保证了编辑器模式和打包模式的一致性

**SetSceneQuality() 保存逻辑**:
- **编辑器模式**: 同时保存到 EditorPrefs 和 PlayerPrefs
  - EditorPrefs: `QualityPipelineSwitcher_BattleQuality` / `QualityPipelineSwitcher_LobbyQuality`
  - PlayerPrefs: `Quality_Scene_Battle` / `Quality_Scene_Lobby`
- **打包模式**: 只保存到 PlayerPrefs
  - PlayerPrefs: `Quality_Scene_Battle` / `Quality_Scene_Lobby`

**ClearSceneOverride() 清除逻辑**:
- **编辑器模式**: 同时清除 EditorPrefs 和 PlayerPrefs
- **打包模式**: 只清除 PlayerPrefs

### 场景2: 战斗场景降档优化

```csharp
public class BattleSceneManager
{
    public void OnEnterBattle()
    {
        // 战斗场景自动使用低一档配置（保证流畅度）
        var deviceQuality = GameModule.Quality.DeviceQuality;
        var battleQuality = deviceQuality > EDeviceQuality.Low 
            ? deviceQuality - 1 
            : deviceQuality;
        
        GameModule.Quality.SetSceneQuality(ESceneType.Battle, battleQuality, saveToPrefs: false);
        GameModule.Quality.ApplyForScene(ESceneType.Battle);
    }
}
```

### 场景3: 低端设备检测

```csharp
public class DeviceTierChecker
{
    public bool IsLowEndDevice()
    {
        var quality = GameModule.Quality.DeviceQuality;
        return quality == EDeviceQuality.Low;
    }
    
    public void DisableHeavyEffects()
    {
        if (IsLowEndDevice())
        {
            // 禁用重度特效
            DisablePostProcessing();
            ReduceParticleCount();
            DisableRealtimeReflections();
        }
    }
}
```

## 注意事项

### 1. 线程安全

❌ **错误示例**：在非主线程调用
```csharp
await UniTask.Run(() =>
{
    // ❌ Unity API只能在主线程调用
    GameModule.Quality.ApplyForScene(ESceneType.Battle);
});
```

✅ **正确示例**：切回主线程
```csharp
await UniTask.Run(() =>
{
    // 后台计算...
});

// 切回主线程
await UniTask.SwitchToMainThread();
GameModule.Quality.ApplyForScene(ESceneType.Battle);
```

### 2. 资源打包

`QualitySettings.asset` 的 URP 引用随 Player 一起进入构建，不需要为运行时管线再增加 YooAsset 动态加载规则：

```text
Resources 资产：Assets/Resources/QualitySettings.asset
加载方式：Resources.Load<QualitySettingsAsset>("QualitySettings")
```

`Assets/AssetRaw/Settings/URP-*.asset` 仍是被引用的源资源；若项目另有资源浏览或编辑器预览用途，可按既有收集规则保留，但运行时切换的唯一入口是 `QualityAssetRegistry`。

### 3. 设备数据库更新

设备数据库是硬编码在 `DeviceSpecDatabase.cs` 中的，修改后需要：
1. 重新编译热更新DLL
2. 重新打包资源
3. 更新客户端

建议定期收集线上设备数据并更新数据库。

### 4. URP版本兼容性

`UniversalRenderPipeline.MarkDirty()` API在部分URP版本中不存在，已使用反射兼容：

```csharp
private static void TryMarkDirty()
{
    try
    {
        var method = typeof(UniversalRenderPipeline).GetMethod(
            "MarkDirty",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        method?.Invoke(null, null);
    }
    catch (Exception e)
    {
        Log.Warning($"MarkDirty反射调用失败: {e.Message}");
    }
}
```

无需手动处理，模块内部已兼容。

### 5. 性能设置生效时机

部分设置需要场景重载才能完全生效：
- **立即生效**: MSAA、渲染缩放、帧率限制
- **需要重载**: 贴图质量、阴影距离（部分情况）

建议在切换场景时应用新质量档位，避免中途切换造成卡顿。

### 6. 配置持久化机制 ⭐

#### 编辑器模式（EditorPrefs + PlayerPrefs）

**Pipeline Switcher 自动保存的配置（EditorPrefs）**:
- `QualityPipelineSwitcher_BattleQuality` - Battle场景的质量档位（每次切换Quality时自动保存）
- `QualityPipelineSwitcher_LobbyQuality` - Lobby场景的质量档位（每次切换Quality时自动保存）
- `QualityModule_EditorQuality` - 当前选择的质量档位（ApplyPipeline时保存）
- `QualityModule_EditorSceneType` - 当前选择的场景类型（ApplyPipeline时保存）

**QualityModule.OnInit() 加载优先级**:
1. **第一优先级**: 读取 `QualityModule_EditorQuality`（ApplyPipeline时保存的当前配置）
   - 同时读取 `QualityModule_EditorSceneType` 确定场景类型
   - 设置全局设备质量和场景覆盖
2. **第二优先级**: 如果没有，读取 `QualityPipelineSwitcher_BattleQuality` 和 `QualityPipelineSwitcher_LobbyQuality`
   - 分别设置 Battle 和 Lobby 的场景质量
   - 使用 Battle 的质量作为全局设备质量
3. **第三优先级**: 如果都没有，执行自动检测

**SceneQualityManager.LoadFromPrefs() 加载优先级**:
1. **第一优先级**: 从 EditorPrefs 读取 Pipeline Switcher 保存的配置
   - Battle: `QualityPipelineSwitcher_BattleQuality`
   - Lobby: `QualityPipelineSwitcher_LobbyQuality`
2. **第二优先级**: 如果没有，从 PlayerPrefs 读取
   - Battle: `Quality_Scene_Battle`
   - Lobby: `Quality_Scene_Lobby`
3. **默认值**: 如果都没有，默认使用 `High` 档位（`EDeviceQuality.High`）

**保存时机**:
- **切换Quality时**: 自动保存到 EditorPrefs（`QualityPipelineSwitcher_BattleQuality` / `LobbyQuality`）
- **切换SceneType时**: 自动从 EditorPrefs 读取该场景的质量档位
- **ApplyPipeline时**: 保存到 EditorPrefs（`QualityModule_EditorQuality` / `EditorSceneType`）和 PlayerPrefs

#### 打包模式（PlayerPrefs）

**质量设置使用 PlayerPrefs 保存**:
```
Quality_DeviceLevel        -> 全局设备档位
Quality_Scene_Lobby        -> Lobby场景覆盖档位
Quality_Scene_Battle       -> Battle场景覆盖档位
```

**SceneQualityManager.LoadFromPrefs() 加载逻辑**:
1. **第一优先级**: 从 PlayerPrefs 读取本地设置
   - Battle: `Quality_Scene_Battle`
   - Lobby: `Quality_Scene_Lobby`
2. **默认值**: 如果没有设置，默认使用 `HighFidelity`（即 `EDeviceQuality.High`）

**GetSceneQuality() 默认值**:
- 如果没有场景覆盖值，默认返回 `EDeviceQuality.High`（不再使用 fallbackDeviceQuality）
- 确保在没有配置的情况下，所有场景都使用 HighFidelity 档位

#### 清除配置

**编辑器模式**:
```csharp
// 清除EditorPrefs
UnityEditor.EditorPrefs.DeleteKey("QualityPipelineSwitcher_BattleQuality");
UnityEditor.EditorPrefs.DeleteKey("QualityPipelineSwitcher_LobbyQuality");
UnityEditor.EditorPrefs.DeleteKey("QualityModule_EditorQuality");
UnityEditor.EditorPrefs.DeleteKey("QualityModule_EditorSceneType");

// 清除PlayerPrefs
PlayerPrefs.DeleteKey(QualityDefine.PREF_KEY_DEVICE_QUALITY);
PlayerPrefs.DeleteKey(QualityDefine.PREF_KEY_SCENE_QUALITY_LOBBY);
PlayerPrefs.DeleteKey(QualityDefine.PREF_KEY_SCENE_QUALITY_BATTLE);
PlayerPrefs.Save();
```

**打包模式**:
```csharp
// 清除PlayerPrefs
PlayerPrefs.DeleteKey(QualityDefine.PREF_KEY_DEVICE_QUALITY);
PlayerPrefs.DeleteKey(QualityDefine.PREF_KEY_SCENE_QUALITY_LOBBY);
PlayerPrefs.DeleteKey(QualityDefine.PREF_KEY_SCENE_QUALITY_BATTLE);
PlayerPrefs.Save();
```

#### 配置同步机制

**编辑器模式下的同步**:
- `SetSceneQuality()` 同时保存到 EditorPrefs 和 PlayerPrefs
- `ClearSceneOverride()` 同时清除 EditorPrefs 和 PlayerPrefs
- 确保 EditorPrefs 和 PlayerPrefs 保持一致

**Pipeline Switcher 自动记忆**:
- 切换 SceneType 时，自动从 EditorPrefs 读取该场景上次选择的质量档位
- 如果没有保存的值，默认使用 `High` 档位
- 每次切换 Quality 时，自动保存到 EditorPrefs

## 调试技巧

### 1. 查看当前质量状态

```csharp
[MenuItem("Tools/Quality/Print Current State")]
public static void PrintQualityState()
{
    var quality = GameModule.Quality;
    Debug.Log($"设备档位: {quality.DeviceQuality}");
    Debug.Log($"Lobby档位: {quality.GetSceneQuality(ESceneType.Lobby)}");
    Debug.Log($"Battle档位: {quality.GetSceneQuality(ESceneType.Battle)}");
    
    var pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
    if (pipeline != null)
    {
        Debug.Log($"当前管线: {pipeline.name}");
        Debug.Log($"MSAA: {pipeline.msaaSampleCount}");
        Debug.Log($"渲染缩放: {pipeline.renderScale}");
    }
}
```

### 2. 强制重新检测设备

```csharp
[MenuItem("Tools/Quality/Force Re-Detect Device")]
public static void ForceReDetect()
{
    PlayerPrefs.DeleteKey(QualityDefine.PREF_KEY_DEVICE_QUALITY);
    GameModule.Quality.AutoDetectAndApply(saveToPrefs: true);
}
```

### 3. 模拟低端设备

```csharp
[MenuItem("Tools/Quality/Simulate Low-End Device")]
public static void SimulateLowEndDevice()
{
    GameModule.Quality.SetDeviceQuality(EDeviceQuality.Low, saveToPrefs: false);
    GameModule.Quality.ApplyForScene(ESceneType.Battle);
}
```

## 相关模块

- **QualityAssetRegistry**：从 `Resources/QualitySettings.asset` 解析场景/档位到 URP 资源
- **SceneModule** (`GameModule.Scene`): 场景切换触发质量应用
- **GameEvent**: 质量变更事件通知
- **PlayerPrefs**: 质量设置持久化

## 参考资料

- 模块实现：`Client/Assets/GameScripts/HotFix/GameLogic/Module/QualityModule/`
- URP资源：`Client/Assets/AssetRaw/Settings/`
- 详细文档：`Client/Assets/GameScripts/HotFix/GameLogic/Module/QualityModule/README.md`
