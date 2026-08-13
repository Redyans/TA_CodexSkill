---
name: projectacg-fbx-source-asset-editor
description: ProjectACG FBX 编辑器当前实现、路径、程序集、验证证据和已知限制。
---

# ProjectACG FBX 源资源编辑器 Profile

> 类型：PROFILE；适用范围：`D:\work2025U3D\Valkyria\ProjectACGMain\ProjectACG\Client` 当前 `FBXEditor`；不可直接迁移到其他项目，迁移前必须重新确认 Unity、Autodesk FBX SDK、菜单、程序集和资产路径。

通用规则见 [工具开发模块](../../README_Tech_TAToolDevelopmentRules.md)，可迁移的实现与排查方法见 [FBX 源数据编辑器开发参考](../../references/fbx-source-asset-editor-development.md)。

## 1. 项目事实与入口

- Unity 基线：`2022.3.62f3`。
- 工具目录：`Assets/Editor/TA_Tools/Common/FBXEditor/Editor/`。
- 工具入口：`TA_Tools/Common/FBX 编辑器`。
- 主程序集：`FBXEditor.Editor`，当前 asmdef 必须设置 `includePlatforms: ["Editor"]`。
- FBX SDK 程序集：`Autodesk.Fbx`；当前项目使用的包版本为 `com.autodesk.fbx@4.2.1`。
- 工具不应进入 Player；`UnityEditor`、`SceneView`、`MenuItem` 和 FBX SDK 写出逻辑只属于 Editor 程序集。

## 2. 当前实现文件分工

- `FBXEditor.cs`：窗口生命周期、顶部资源选择、工具栏、页签和 Editor-only 入口。
- `FBXEditor.Naming.cs`：源 FBX 节点/网格/材质/动画命名和源文件安全写出基础能力。
- `FBXEditor.Axis.cs`：FBX 轴向、单位和节点/网格轴向相关界面与写出入口。
- `FbxAxisSourceProcessor.cs`：FBX SDK Session、导入/导出、节点/几何/动画/姿势基础签名和轴向处理。
- `FBXEditor.SourceData.cs`：源数据页 UI、参数状态、临时文件写出、回读签名验证、覆盖/另存副本和 Unity 重导入。
- `FbxSourceDataProcessor.cs`：单位/比例、Pivot、UV、顶点色、材质槽和骨架层级的 SDK 数据处理。
- `FBXEditor.Editor.asmdef`：Editor-only 平台边界和 `Autodesk.Fbx` 程序集依赖。

## 3. 源数据页功能范围

### 3.1 单位与整体比例

支持毫米、厘米、分米、米、千米、英寸、英尺和码，并提供独立整体倍率。单位转换使用 `FbxSystemUnit.ConvertScene`；整体倍率同步节点、几何、骨骼、动画、蒙皮簇和绑定姿势，最后恢复最终单位设置。

### 3.2 原始 Pivot

支持旋转偏移、旋转枢轴、预旋转、后旋转、缩放偏移和缩放枢轴。默认保持局部/世界变换；目标节点存在变换动画时阻止操作。

### 3.3 UV

支持缩放/偏移、U/V 翻转、重命名、复制、新建空通道、删除和交换。由于当前 C# 绑定不能稳定直接读 UV 元素名称，重命名通过创建等价新 `FbxLayerElementUV` 替换实现。新建通道使用 `eByControlPoint + eDirect`，初始值为 `(0, 0)`，不自动展 UV。

### 3.4 顶点色

支持通道乘数、加值、RGBA 重排、单通道反相、固定颜色填充、新建和删除。新建通道使用控制点映射，并为每个控制点写入初始颜色；写入值限制在 `0~1`。

### 3.5 材质槽

支持槽位上移、下移、替换场景材质、添加场景材质、删除所选未使用槽和删除全部未使用槽。实现会重建节点材质连接，并重映射多边形材质索引；材质判定使用场景材质身份/索引，不只比较名称。

### 3.6 骨架层级

支持移动到其他骨骼下或场景根节点。执行前保存所有节点世界矩阵，执行后求解本地 TRS 并验证默认姿势、蒙皮连接和绑定姿势。目标为自身/子层级或含节点变换动画时阻止执行。

## 4. 当前写出和备份契约

所有源数据操作遵循：

`临时 FBX → Autodesk FBX SDK 回读 → 数据签名比较 → 通过后替换`

覆盖源文件前默认备份 `.fbx` 和 `.meta` 到 `Library/FBXEditorBackups`。另存副本不会替换源文件，也不会把源 `.meta` 当作副本的唯一身份来源。写入成功后使用 `AssetDatabase.ImportAsset(..., ForceUpdate)` 或受控 `Refresh`，再重新读取源数据概览。

签名校验覆盖节点层级、名称和材质连接、单位、Pivot、默认/动画局部与全局矩阵、控制点、法线/切线/副切线、UV、顶点色、材质槽、多边形材质索引、Skin Cluster、BlendShape 和 BindPose/Pose。已确认 FBX 导出器会规范化的完全空数据层和材质索引 DirectArray 数量不作为独立失败条件，但语义数据仍必须比较。

## 5. 当前验证证据

已使用以下 ProjectACG 资源完成 SDK roundtrip：

- 静态模型：`Assets/AssetRaw/Effects/Models/GL/Circle/mesh_fx_circle_003.fbx`
- 蒙皮模型：`Assets/Packages/MagicaCloth2/Example (Can be deleted)/UnityChan/SD_Kohaku_chanz/Utc_sum_humanoid.fbx`
- 动画模型：`Assets/Animations/nv01/nv01_idle.fbx`

已验证范围包括：

- `unit_static`、`unit_animation`、`unit_skinned`
- `pivot`、`pivot_skinned`
- `uv_transform`、`uv_rename`、`uv_duplicate`、`uv_add`、`uv_delete`、`uv_swap`
- `vertex_add`、`vertex_adjust`、`vertex_fill`、`vertex_delete`
- `material`、`material_move`、`material_replace`、`material_remove_selected`、`material_remove_all`
- `skeleton`、`skeleton_animated_block`

Unity 程序集编译已通过；修正 `includePlatforms: ["Editor"]` 后，Unity Tundra 编译日志显示 `FBXEditor.Editor.dll` 成功生成，仅保留原有 `lastScreenCacheFrame` 未使用警告。临时 Probe、编译 targets 和输出目录已清理。

## 6. 已知限制与维护边界

1. 含节点 Transform 动画的 FBX 不允许骨架重设父级；完整支持需要按所有动画栈重写新父空间 TRS 曲线。
2. 含 Transform 动画的目标节点不允许直接修改原始 Pivot；当前实现不静默改写动画基底。
3. `eDirect` 直接材质引用无法通过当前 C# 绑定安全重建时，材质替换会阻止执行。
4. SDK roundtrip 通过不等于所有 DCC 私有扩展都能保留；特殊导出器数据必须增加针对性签名和回读样本。
5. 独立 .NET Probe 大量反复创建/销毁 FBX Manager 曾出现 Native SDK `SEHException`；正式工具保持少量 Session，测试模式必要时拆为独立进程。
6. 不要手工修改 `Library/Bee`、`Library/ScriptAssemblies` 或 Unity 自动生成的 csproj 作为永久修复；这些只用于诊断，源修复应落在 asmdef、源码和项目配置。

## 7. 维护检查清单

- [ ] 修改 `FBXEditor.Editor` 依赖后确认仍为 Editor-only，并执行一次 Player 编译边界检查。
- [ ] 新增 FBX SDK API 前先确认项目实际包版本和 C# 绑定方法签名。
- [ ] 每个源数据操作都有“允许变化/必须不变”的签名契约。
- [ ] 每个覆盖操作都有临时文件、回读校验、备份和失败清理。
- [ ] 对动画、蒙皮、重复材质、空 Layer、MappingMode/ReferenceMode 和只读源文件做边界验证。
- [ ] Unity 重导入后检查动画播放、蒙皮姿势、材质槽、UV 和顶点色，而不是只看 SDK 文件存在。
- [ ] 新限制先记录现象、根因、修正、适用范围、验证证据和回退方式，再决定是否上升为通用规则。
