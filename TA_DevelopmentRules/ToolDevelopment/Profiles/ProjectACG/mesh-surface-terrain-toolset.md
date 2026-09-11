---
name: projectacg-mesh-surface-terrain-toolset
description: ProjectACG `Assets/Editor/TA_Tools/Scene/` 地表与网格工具集的位置、菜单、数据资产、纯 Editor 边界、顶点色绘制显示/保存方式与旧版组件下线状态。
---

# ProjectACG 网格地形与地表工具集 Profile

## 1. 适用范围与结论

本 Profile 记录 ProjectACG 工作区 `Assets/Editor/TA_Tools/Scene/` 下 5 组地表/网格工具的项目事实。通用规则以 [TA 工具开发 CORE](../../README_Tech_TAToolDevelopmentRules.md) 与 [ProjectACG TA_Tools Profile](README_Tech_ProjectACGTAToolsProfile.md) 为准；跨工程迁移方法见 [Unity Editor 工具集跨工程迁移与旧件下线参考](../../references/unity-editor-tool-cross-project-migration.md)；离线编译校验见 [Unity Editor 脚本离线编译校验参考](../../references/unity-editor-script-offline-compile-verification.md)；笔刷与顶点色 UI 约定见 [Editor 笔刷与顶点色绘制工具 UI 参考](../../references/editor-brush-and-vertex-color-ui-patterns.md)。

直接结论：这 5 组工具当前全部位于 `Assets/Editor/TA_Tools/Scene/` 下，属于 Editor-only 程序集，不进入 Player；地形/网格绘制数据从「对象上的组件」改为 Editor-only 数据资产 `Assets/Tools/MeshSurfaceToolkit/MeshSurfaceDatabase.asset`；使用工具不需要在模型或地形上挂任何脚本。

## 2. 当前版本、入口与代码地图

| 项目 | 当前值 | Source of Truth |
| --- | --- | --- |
| Unity | `2022.3.62f3` | `ProjectSettings/ProjectVersion.txt` |
| 渲染管线 | URP（`TerrainBlendBake` 直接引用 `UnityEngine.Rendering.Universal`） | 工程 `Packages/manifest.json`、工具源码 |
| 工具根目录 | `Assets/Editor/TA_Tools/Scene/` | 当前资产目录 |
| 地形绘制数据 | `Assets/Tools/MeshSurfaceToolkit/MeshSurfaceDatabase.asset` | `MeshSurfaceToolkitPaths.DatabaseAsset` |
| 第三方 UI 依赖 | 地形山石混合烘焙依赖 Odin（`Sirenix`，`OdinEditorWindow`） | `TerrainMeshBlendBakerWindow.cs` |

`MeshSurfaceToolkitPaths` 用「按脚本自身位置反查」解析工具根目录：`AssetDatabase.FindAssets("MeshSurfaceToolkitPaths t:MonoScript")` + `GUIDToAssetPath`，失败时回落到 `FallbackRoot` 常量。因此工具目录可以整体拷到任意位置，只有 `FallbackRoot` 需要随目标工程改写。

## 3. 工具清单

| 工具 | 目录（相对 `Assets/Editor/TA_Tools/Scene/`） | 菜单 | 职责 |
| --- | --- | --- | --- |
| 网格地形绘制工具 | `Terrain/MeshSurfaceToolkit/` | `TA_Tools/Scene/Terrain/网格地形绘制工具` | 在模型/网格上绘制权重图与顶点数据、笔刷库、散布 Prefab。 |
| Terrain Mesh 转换器 | `Terrain/MeshTerrainConverter/` | `TA_Tools/Scene/Terrain/Terrain Mesh转换器` | Unity Terrain 与 Mesh 地形互转，带预览 Shader。 |
| 地形 TextureArray 烘焙器 | `Terrain/TerrainTextureArrayBaker/` | `TA_Tools/Scene/Terrain/地形 TextureArray 烘焙器` | 烘焙地形纹理数组资产。 |
| 导出地形 RGBA 权重图 | `Terrain/TerrainMapExport/` | `TA_Tools/Scene/Terrain/导出地形RGBA权重图` | 导出地形权重图。 |
| 权重图程序化散布工具 | `Terrain/WeightmapProceduralScatter/` | `TA_Tools/Scene/Terrain/权重图程序化散布工具` | 按权重图程序化散布，含预设资产。 |
| MeshDecal 制作 | `MeshDecalTool/` | `TA_Tools/Scene/MeshDecal制作` | Mesh Decal 创建与烘焙，`Runtime/MeshDecal.cs` 为组件。 |
| 反射探针导出工具 | `ReflectionProbePostBake/` | `TA_Tools/Scene/反射探针导出工具` | 反射探针烘焙后导出。 |
| 地形山石混合烘焙 | `TerrainBlendBake/` | `TA_Tools/Scene/地形山石混合烘焙` | 地表颜色/高度烘焙与山石混合，含 3 个 Shader 与 `Usage.txt`。 |
| Foliage Renormalizer | `Foliage Renormalizer/` | `TA_Tools/Scene/Foliage Renormalizer` | 植被法线转移、代理网格生成与 AO 烘焙。 |

工具集**不包含**「迁移旧版地形组件到工具数据」一类的迁移菜单：在源工程与当前工程内搜索 `MenuItem(` 只能得到上表入口。旧组件数据不能通过菜单迁移，只能先保留旧脚本读回或接受数据丢失。

## 4. 纯 Editor 边界与目录语义

整棵 `Assets/Editor/TA_Tools/Scene/` 属于 Editor-only 程序集，因此下列组件当前只存在于编辑器：

- `MeshDecalTool/Runtime/MeshDecal.cs`（`MonoBehaviour`）；
- `Foliage Renormalizer/Scripts/FoliageRenormalizerStorage.cs`（抽象 `MonoBehaviour`）及其派生 `FoliageRenormalizerUtility`。

风险与边界：

- 若 `MeshDecal` 需要在 Player 运行时生效，必须把 Runtime 脚本移出 `Editor` 目录（或迁至独立 Runtime asmdef），否则不进入包体；
- 若 Foliage 组件只在编辑器烘焙流程中使用，当前形态可以接受；
- 「`Editor` 目录下保留 `Runtime/` 子目录」是当前工程既有约定，不是跨项目结论，写入 CORE 前必须重新验证。

## 5. 旧版组件下线记录

现象与根因：目标工程原有 `Assets/Plugins/TA_Tools/Scene/MeshSurfaceToolkit/Runtime/` 的 4 个组件脚本与新的编辑器数据类**同名同命名空间**（`ProjectACG.MeshSurfaceToolkit` 下的 `MeshSurfaceTarget`、`MeshSurfacePaintTarget`、`MeshSurfaceScatterTarget`、`MeshSurfaceTerrainConversionMetadata` 等 8 个类型），同时存在会造成编译警告与序列化歧义。

当前修正：`Assets/Plugins/TA_Tools/Scene/MeshSurfaceToolkit/`（4 个 Runtime 组件 + `Shaders/URP/MeshSurfaceTerrainBlend8.shader`）以及整个 `Assets/Plugins/TA_Tools/` 目录树已移出工程。工程没有版本控制，因此清理前先在工程外保留备份（`%TEMP%/ta_legacy_backup_20260911/`），用移动代替删除。

影响与未验证项：下列资源上的旧组件会显示 `Missing (Mono Script)`，原序列化字段不再生效，需要在 Unity 内手动 `Remove Component`；旧组件里的数据未迁移。

- `Assets/AssetRaw/Scene/Map_ACG_A1C1L1_forest/Map_ACG_A1C1L1_forest.unity`
- `Assets/AssetRaw/Scene/Map_ACG_A1C3L5_MobileBTS/prefab/pre_stateB_A1C3_3D.prefab`
- `Assets/AssetRaw/character/monster/2015002/timeline/tPre_monster_2015002_die.prefab`

## 6. 重复助手去重记录

`Assets/Editor/TA_Tools/Character/VertexColorPainter/MeshSurfaceEditorUtilities.cs` 与迁移进来的实现同名同命名空间，已改名为 `MeshSurfaceEditorUtilities.cs.disabled-old-copy`（`.meta` 一起改名）。回滚方式：把两个文件改回原名。迁移后的 `MeshSurfaceEditorUtilities`（由 `Terrain/MeshSurfaceToolkit/` 提供）已覆盖旧调用方的成员签名，`CharacterVertexColorPainterWindow` 在离线编译下通过。

## 7. 顶点色绘制工具现状

`CharacterVertexColorPainterWindow`（菜单 `TA_Tools/Character/Vertex Color Painter`，目录 `Assets/Editor/TA_Tools/Character/VertexColorPainter/`）当前支持：

- 显示通道 `{"RGB","R","G","B","A"}`，默认 `RGB`：通过预览材质的 `_VertexColorPainterPreviewMode`（0~4）单独查看 R/G/B/A；
- 绘制通道 `{"RGB","R","G","B","A"}`，默认 `A`：RGB 走颜色字段，单通道走 0~1 数值；
- 保存方式 `{"另存为新 Mesh","覆盖源 Mesh"}`：新资产后缀 `_VertexColor`；覆盖以 `CanOverwrite = MeshSurfaceEditorUtilities.IsEditableMeshAsset(mesh, out _)` 为门槛，写入前检查顶点数并 `Undo.RecordObject`；
- 预览材质 `Hidden_TA_Tools_VertexColorPainterPreview.mat` + `CharacterVertexColorPainterPreview.shader` 为专用预览链路，不写正式材质。

笔刷缩略图尺寸问题记录：`Terrain/MeshSurfaceToolkit/Editor/MeshSurfaceToolkitWindow.cs` 曾出现笔刷缩略图「没有占满」，原因是使用了 `ScaleMode.ScaleToFit`；当前用 `BrushPreviewSize = 124f` 固定尺寸配合 `GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true)`。同文件另一处按比例缩放的预览仍保留 `ScaleMode.ScaleToFit`，两处不要统一。

## 8. 体积裁剪记录

`Foliage Renormalizer` 迁移时按其原始目录整体复制，其中工具本体（`Editor/`、`Scripts/`、`Shaders/`）约 0.3 MB，示例与文档占绝大部分。已移出工程并备份：`Demo/`（约 111 MB）、`Generated Meshes/`（约 5.2 MB）、`Offline Guide.pdf`（约 23.2 MB）。保留 `Quick Start Guide - Google Docs.url` 与三个工具子目录。

当前工具目录体积：`Terrain` 约 1.16 MB、`MeshDecalTool` 约 0.09 MB、`ReflectionProbePostBake` 约 0.02 MB、`TerrainBlendBake` 约 0.12 MB、`Foliage Renormalizer` 约 0.34 MB。

## 9. 验证证据与未验证项

已完成：

- 离线编译 31 个脚本：编译器为 Unity 自带 Roslyn `4.3.1`，引用当前工程 `Library/ScriptAssemblies`（排除 `Assembly-CSharp*`）、Sirenix 与 URP 程序集，`exit=0`，仅保留 2 个历史 warning（`FoliageNormalTransfer` 未使用变量、`MeshDecal.m_BakeOutputReference` 未使用字段）；
- 静态检查：菜单路径唯一、迁移类型全名无冲突、旧路径前缀 0 命中、源/目标 `.meta` GUID 交集为空、文件数与字节数对账一致（差异仅来自路径改写）。

未验证（交付时必须说明）：

- Unity 内重导入、窗口打开与代表性绘制流程；
- 顶点色通道显示/另存与覆盖的实际写入结果与 Undo；
- 第 5 节 3 个资源的 `Missing (Mono Script)` 清理；
- `MeshDecal` 与 Foliage 组件是否存在 Player 运行时用途（决定是否需要把 Runtime 脚本移出 `Editor` 目录）。
