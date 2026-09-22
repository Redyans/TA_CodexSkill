# ProjectACG 角色死亡溶解 Profile

> 类型：PROFILE；适用工程：ProjectACG；通用实现与排查方法见 [溶解采样空间、阈值与材质生命周期参考](../../references/dissolve-space-threshold-and-material-lifecycle.md)。

本文记录 ProjectACG 当前 `CharacterDeath + Chara_V2` 的具体实现、接口、材质预览故障根因、修正方式和验证边界。路径、字段和提交哈希仅用于当前工程，不应上升为跨项目规则。

## 1. 项目事实与 Source of Truth

### 1.1 当前源码

| 职责 | 路径 |
| --- | --- |
| 死亡效果运行时控制 | `Client/Assets/GameScripts/HotFix/GameLogic/Battle/CharacterMonsterEffect/CharacterDeath.cs` |
| 自定义 Inspector 与编辑器预览调度 | `Client/Assets/GameScripts/Editor/CharacterDeathEditor.cs` |
| 角色 V2 溶解共享函数 | `Client/Assets/Shader/Character/Common/Func_Chara_Dissolve_V2.hlsl` |
| 角色 V2 部位 Shader | `Client/Assets/Shader/Character/Chara_V2/**` |
| 每部位材质参数声明 | `Client/Assets/Shader/Character/Chara_V2/**/*Bindings.hlsl` |

角色死亡与武器溶解刻意分离：角色只依赖 `_DEATH_ON` 和 `_DissolveThreshold`；Shader 名含 `Gun` 的材质由 `WeaponDissolveController` 独立驱动，`CharacterDeath` 不覆盖其阈值。

### 1.2 当前公开参数

主要 Shader 参数：

- `_DissolveNoiseMap` / `_DissolveNoiseMap_ST`
- `_DissolveThreshold`
- `_DissolveLineWidth` / `_DissolveNoiseStrength`
- `_DissolveEdgeDissipationStrength`
- `_DissolveEdgeDissipationSpeed`
- `_DissolveEdgeDissipationDirection`
- `_DissolveColor` / `_DissolveDarkColor`
- `_DissolveAxisOS` / `_DissolveAxisMin` / `_DissolveAxisMax`
- `_DissolveAxisSpace`
- `_DissolveMode` / `_DissolveCenter01`
- `_DISSOLVE_OFFSET` / `_DissolveOffsetDirection` / `_DissolveOffsetThreshold` / `_DissolveOffsetScale`

当前 `CharacterDissolveMode` 只暴露：

- `Directional = 0`，Inspector 显示“定向”；
- `NonDirectional = 4`，Inspector 显示“非定向”。

数值 `4` 保留旧 `NoAxis` 资源的序列化兼容。旧反向、两边向内和两边向外枚举不再暴露，`OnValidate` 将非 4 的旧值折叠为当前定向模式。

## 2. 当前空间实现

### 2.1 `_DissolveAxisSpace`

| 值 | Inspector | 轴向坐标 | 噪声采样 |
| --- | --- | --- | --- |
| `0` | 本地空间 | `dot(positionOS, axisOS)` | `positionOS` 投影到垂直于轴向的平面。 |
| `1` | 世界空间 | `dot(positionWS, sharedWorldAxis)` | `positionWS` 投影到垂直于共享世界轴的平面。 |
| `2` | 屏幕空间 | normalized screen UV 与屏幕轴点积 | normalized screen UV。 |

`CharaDissolve_GetNoiseUV(positionOS, positionWS, positionCS)` 是当前统一入口。定向和非定向都使用它；非定向只跳过轴向推进，不再固定用模型 UV。

Inspector 中“轴向空间/噪声采样空间”位于“噪声偏移”下方。定向模式显示轴向来源、手动轴向和范围；非定向模式只保留噪声采样空间选择。

### 2.2 手动屏幕轴

Screen 模式下，`CharacterDeath.ResolveDissolveAxisWS` 将手动 `(x,y,z)` 按当前相机 `right/up/forward` 解释。TransformRight/Up/Forward 也直接映射相机平面方向，避免使用世界 X/Y 后，朝向相机的轴在 Shader 中退化为 Y fallback。

Shader 的 `CharaDissolve_GetScreenAxisDir` 将世界轴变到 View Space，并按 `_ScaledScreenParams` 宽高比修正 X。屏幕手动 X/Y 是否生效必须在目标 Game View 宽高比和实际渲染相机中复验。

### 2.3 边缘消散方向

`_DissolveEdgeDissipationDirection` 是 `Vector2`，表示当前噪声投影平面内的流动方向。`CharaDissolve_GetEdgeDissipationUV` 先调用统一噪声 UV，再叠加：

```hlsl
flowDirection * (_Time.y * _DissolveEdgeDissipationSpeed)
```

因此 Local/World 改变投影平面，Screen 使用相机屏幕平面；消散方向会随 `_DissolveAxisSpace` 改变。方向字段不升级为 `Vector3`，以保持资源序列化兼容。

边缘侵蚀只作用于 `_DissolveLineWidth` 附近：强度、边缘宽度或阈值为 0 时旁路。当前复用 `_DissolveNoiseMap`，没有新增第二张噪声纹理。

## 3. 当前阈值语义

### 3.1 脚本端

`ResolveDissolveThreshold`：

- `dissolveEnabled == false`：固定返回 `0`。
- 静态参数模式：直接使用 `dissolveThreshold`。
- 死亡曲线模式：对隐藏的 `dissolveProgressStart/end` 执行 `InverseLerp + Clamp01`；`end` 至少大于 `start` 一个 `DissolveAxisRangeEpsilon`。

进度起点/终点已从参数面板隐藏，但字段仍保留旧资源序列化和曲线逻辑。`dissolveEnabled` 是全局开关，关闭后本地、世界、屏幕、定向和非定向都不得裁剪。

### 3.2 Shader 端

`CharaDissolve_Value` 的定向模式先计算 `axis01`，再使用：

```hlsl
float centeredNoise = (noise - 0.5) * 2.0;
float edgeFade = saturate(min(_DissolveThreshold, 1.0 - _DissolveThreshold) * 2.0);
float noisyThreshold = _DissolveThreshold
    + centeredNoise * _DissolveNoiseStrength * edgeFade;
float dissolveValue = dissolveDir - noisyThreshold + _DissolveThreshold;
```

`edgeFade` 在阈值 0 和 1 时归零，确保噪声只打散中间边缘。`CharaDissolve_Clip` 在阈值接近 0 时直接返回；其余使用：

```hlsl
clip(dissolveValue - _DissolveThreshold - 1e-4);
```

该 epsilon 确保阈值 1 时完全裁剪。

### 3.3 定向上下界

当前定向上下界为 `_DissolveAxisMin/_DissolveAxisMax`。`ResolveDissolveAxisRange`：

- 非定向固定调试范围为 `0..1`，不计算轴向 bounds。
- 手动范围直接使用 Inspector 值。
- 自动范围遍历支持溶解材质的 Renderer 包围盒角点并投影。
- Local 使用 `SkinnedMeshRenderer.localBounds` 或 mesh bounds，并可按脏标记缓存。
- World/Screen 使用当前 `Renderer.bounds`；死亡动作、模型形变、世界位置和相机会改变投影，因此播放期间重算。
- 范围小于 epsilon 时，以中心扩为长度 1，避免除零和整块突变。

调试字段 `resolvedDissolveAxisOS/min/max` 用于核对 CPU 计算是否与 Shader 输入一致。

## 4. 骨骼、Renderer 与材质槽传值

`CharacterDeath` 挂在角色表现根，不向每根骨骼写阈值。当前流程：

1. `targetRenderers` 为空时，`CacheRenderersIfNeeded` 收集子层级 MeshRenderer/SkinnedMeshRenderer。
2. 自动采集模式在 `Play`、启用和运行期间调用 `RefreshTargetRenderersForRuntime`，补齐换装、LOD 和对象池新增 Renderer。
3. Shader Fragment 使用蒙皮/顶点偏移后的 `i.positionWS`，并按需重建 `positionOS`，溶解边界跟随当前动画姿态。
4. `ApplyDeathProgress` 遍历每个 Renderer 的每个材质槽，写统一阈值、噪声 ST、轴向、范围、颜色和偏移参数。
5. `SupportsCharacterDissolve` 要求材质存在 `_DissolveThreshold`，并排除 Shader 名含 `Gun` 的武器材质。

HairShadow 使用独立 `forceRenderingOff` 状态缓存；死亡结束、禁用和重置时恢复原值，不能通过删除材质解决头发阴影层残留。

## 5. 当前运行时材质所有权

### 5.1 实例材质

`GetRuntimeMaterials` 使用 `renderer.materials`，与武器溶解控制器保持同一运行时语义。缓存按 Renderer 保存，但每次比较：

- 材质槽数量；
- 每个槽位的实例引用。

即使槽位数量不变，只要换装系统替换了引用，也会刷新缓存并标记材质状态脏。

`_DEATH_ON`、`_RIM_ON`、Blend、ZWrite 和 RenderQueue 等状态写入实例材质。Rim 原始 Keyword 状态单独缓存，退出死亡时恢复，而不是固定关闭。

### 5.2 MaterialPropertyBlock

每个 Renderer/材质槽先调用 `BattleRuntimeUpdateUtility.SynchronizeBattleShaderTimeMaterialBinding`，再读取现有 MPB、写死亡参数并提交。该顺序用于避免时间绑定逻辑清空 `_DeathMaskUVOffset`、`_DeathProgress` 等字段。

关键数值同时写实例材质和 MPB：实例材质提供稳定基线，MPB 提供每 Renderer/槽覆盖。不能只写 shared material，也不能用 `SetPropertyBlock(null)` 清空其他系统数据。

V2 角色通过 Dither/溶解裁剪消失并保持 Opaque，不进入旧透明 Phase。旧版不支持 Dither 的材质才沿用透明混合路径。

## 6. 编辑器预览与撤回丢材质根因

### 6.1 当前预览租约

进入预览时，每个 Renderer：

- clone 原始 `sharedMaterials` 数组容器；
- 保存每材质槽原始 MPB；
- 为非空材质创建 `HideFlags.HideAndDontSave` 的临时副本；
- 记录为 `EditorPreviewRendererLease`。

静态字典 `Renderer -> lease` 允许同一个 Renderer 被多个 `CharacterDeath` owner 共享。只有最后一个 owner 退出时，才先恢复原始材质和 MPB，再 `DestroyImmediate` 临时材质。

### 6.2 已确认根因

撤回后 cloth/hair 材质槽变成 `None` 的根因是 Unity 编辑器序列化重绑与临时材质销毁发生竞态，不是 Shader `clip`：

1. 预览把 Renderer 指向临时材质数组。
2. Undo、Animation Mode 或 Prefab 重建触发 `OnDisable`，Unity 随后还会恢复序列化材质数组。
3. 旧实现立即销毁临时材质，或另一个预览会话把临时数组保存成“原材质”。
4. Unity 后续重绑把已销毁引用写回 Renderer，Inspector 显示 `None`。

### 6.3 当前修正

- 非运行编辑器态的 `OnDisable` 不立即释放预览。
- `CharacterDeathEditorPreview` 监听 Undo、Animation Mode 退出和 inactive preview，并使用 `EditorApplication.delayCall` 延迟一个 editor tick。
- 活动 lease 仍有效但 Renderer 暂时不再引用预览数组时，`EnsureEditorPreviewMaterials` 重新挂回同一数组。
- 保存原材质时 clone 数组，避免数组容器被 Unity 后续重绑污染。
- `ApplyDeathProgress` 在编辑器没有活动预览时直接返回，禁止写真实材质资产。
- `OnDestroy` 作为场景关闭、脚本重载或 Prefab 重建未经过正常退出时的最终恢复点。

## 7. 试错过程与经验

### 7.1 从“运行不生效”到材质实例一致性

Timeline/静态参数模式不一定调用 `Play`，早期仅依赖播放状态导致参数改变但材质链路没有激活。当前使用 `StaticDeathPresentationState` 比较表现参数，检测静态变化后进入同一写入路径；状态结构新增字段时必须同步 `CreateStaticPresentationState` 和 `Matches`。

运行期随后发现 `sharedMaterials` 与实际渲染实例不一致，改为 `renderer.materials` 并比较槽位引用。经验是先确认“写到哪个材质对象”，再排查 Shader 属性。

### 7.2 从单会话恢复到 Renderer 级 lease

单个组件保存/恢复材质在简单场景有效，但嵌套 Prefab 或多个 `CharacterDeath` 同时预览时，后进入会话会把前一个临时材质当成原材质。Renderer 级共享 lease 和 owner 计数解决了对象所有权问题。

经验是：临时资源的 owner 边界必须按实际共享对象定义，不能只按 Inspector/组件会话定义。

### 7.3 从立即释放到延迟退出

即使 lease 正确，Undo/Animation Mode 的回调时序仍可能让 Unity 在释放后再次写回临时引用。修复不在于“多写几次 sharedMaterials”，而是等待编辑器完成序列化重绑，再统一恢复一次。

经验是：生命周期竞态问题要控制恢复时序和唯一释放点；重复调用 `sharedMaterials`/`SetSharedMaterials` 可能扩大竞态。

### 7.4 从固定 UV 到统一空间采样

早期非定向模式使用模型 UV，轴向空间只影响定向推进，造成面板切换空间但非定向噪声不变。当前定向/非定向共用 `CharaDissolve_GetNoiseUV`，边缘消散也共用该投影。

经验是：先区分轴向推进与噪声采样，再决定是否共享同一空间参数；面板文案必须对应真实 Shader 数据流。

### 7.5 从噪声叠加到端点稳定

简单将 noise 加到 `axis01` 会破坏 0/1 端点。当前让噪声强度在阈值两端淡出，并在 clip 前对 0 阈值旁路。

经验是：先冻结“0 完整、1 清空”的数学契约，再增加任何艺术扰动。

## 8. 调试顺序

1. 检查 `dissolveEnabled` 和最终 `_DissolveThreshold`；关闭时必须是 0。
2. 检查 `_DEATH_ON` 是否在实际运行时材质实例上启用。
3. 检查目标 Renderer/材质槽是否支持 `_DissolveThreshold`，武器是否被正确排除。
4. 检查 `resolvedDissolveAxisOS/min/max` 与当前空间、相机和 bounds。
5. 临时输出 noise、axis01、dissolveValue、edgeBand，确认问题所在层。
6. 检查 Bindings 与 Shader Properties 是否包含相同字段。
7. 检查 MPB 是否在时间绑定或其他表现控制器之后被覆盖。
8. 材质槽 `None` 时优先查 preview lease、Undo/Animation Mode 和 `DestroyImmediate` 时序。
9. Forward 正常后继续验证 Outline、Depth、Shadow 和透明深度 Pass；一个 Pass 正常不代表整个角色家族完成。

## 9. 当前验证证据

### 9.1 已完成的静态检查

- `Func_Chara_Dissolve_V2.hlsl` 的 Local/World/Screen 采样入口与边缘消散 UV 已核对。
- `_DissolveThreshold=0` 旁路、阈值 1 的 epsilon 裁剪、噪声端点淡出已核对。
- 角色 V2 Shader 属性和五组主要 Bindings 中的边缘消散字段一致性已检查。
- `CharacterDeath` 材质实例、MPB、静态状态缓存、预览 lease 和 Undo 延迟退出路径已静态核对。
- 相关文件 `git diff --check` 已通过。

### 9.2 未完成验证

- 当前工作环境没有可用 Unity 命令行和可靠 C# `dotnet` 编译入口，本轮未完成 Shader 编译和 Game View 画面验证。
- Screen 模式需要在实际相机、宽高比、动态分辨率、透视/正交和镜头缩放下复验。
- 骨骼动画、换装、LOD、对象池、多材质槽和武器独立控制需要 Unity Play Mode 复验。
- Undo/Redo、Animation Mode、Prefab Stage、组件禁用、场景关闭和域重载需要人工编辑器生命周期回归。
- 项目 harness 的 `.agents/skills` 路径、Git LFS 临时目录权限、既有 UI `RemoveAllListeners` 和本地化生成失败与本功能无直接关系，不能作为该溶解链路失败证据。

## 10. 历史证据索引

以下提交用于追溯原因，不代表恢复旧实现：

| 提交 | 结论 |
| --- | --- |
| `244f56327d4cbdc138f981e5b38faca382acae65` | Undo/Animation Mode 延迟退出预览，避免销毁临时材质后被 Unity 写回。 |
| `e9edcb6d06` / `5969d4611d` | 引入 Renderer 级共享预览 lease，解决多个预览会话互相保存临时材质。 |
| `1c87c8db58` / `dd5930a57a` | 增加静态表现状态变化检测，使 Timeline/静态参数进入运行时写入路径。 |
| `243610ad1b` | 从武器溶解函数拆出 `Func_Chara_Dissolve_V2.hlsl`，隔离角色 `_DEATH_ON` 与武器开关。 |
| `ad677e97f1` | 保留旧数值 4 为非定向，并收敛旧溶解模式枚举。 |

## 11. Unity 回归清单

- 开关关闭、阈值 0、0.5、1；0 完整可见、1 完全裁剪。
- 定向/非定向分别切换 Local/World/Screen。
- Screen 手动 X/Y 方向、画幅比、相机旋转、FOV、正交尺寸。
- 边缘消散方向 X/Y、正/负速度、强度 0、边缘宽度 0。
- 骨骼动作、多个 SkinnedMeshRenderer、多个材质槽、换装、LOD、对象池复用。
- 角色本体与武器阈值互不覆盖。
- 进入/退出预览、Undo/Redo、Animation Mode、Prefab Stage、组件禁用、场景关闭、域重载。
- 同一 Renderer 多个预览 owner；先退出任意 owner 均不丢材质。
- Forward、Outline、Depth、Shadow、透明深度及实际渲染消费者。

## 12. 维护边界

- 修改共享函数前执行 Shader 模块 `CMP-01`、`CHR-01`、`CHR-04`、`CHR-05`，回归全部 Chara_V2 消费者。
- 新增溶解字段时同步所有 Shader Properties、Bindings、`StaticDeathPresentationState`、实例材质写入和 MPB 写入。
- 旧序列化枚举值、材质属性名和 Keyword 属于兼容接口，不为面板简化直接删除。
- 编辑器预览修复不得污染运行时材质路径；运行时优化也不得删除编辑器的原始材质/MPB 恢复能力。
- 没有 Unity 画面和生命周期复验时，只能声明静态检查通过。
