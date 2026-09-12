# 溶解采样空间、阈值与材质生命周期参考

> 类型：REFERENCE；适用范围：Unity 中由 Shader、运行时脚本和编辑器预览共同驱动的角色、怪物或道具溶解效果；使用前提：必须以目标项目的渲染管线、Shader 属性、Renderer 结构、动画和材质所有权重新验证。

本文总结溶解效果的空间选择、阈值边界、骨骼蒙皮传值、材质实例化与编辑器预览排错方法。它是可迁移的实现与诊断参考，不替代目标工程的 Shader、Keyword、Pass、材质资产、动画和对象池契约。

## 1. 适用场景

- 需要在模型 UV、本地、世界或屏幕空间之间选择噪声采样方式。
- 需要保证统一进度 `0` 完全可见、`1` 完全溶解，而噪声只扰动中间边缘。
- 需要实现从上到下、从左到右或任意轴向推进的定向溶解。
- SkinnedMeshRenderer 绑定骨骼后，需要让多个 Renderer 和材质槽共享同一阈值。
- 运行时换装、LOD、对象池或材质重绑定导致参数写入不生效。
- 编辑器预览、Animation Mode、Undo/Redo 或 Prefab 重建后出现材质槽 `None`、临时材质泄漏或材质资产被污染。

不应直接套用本文的情况：

- 目标只是一次性全屏后处理，不涉及 Renderer 材质和骨骼蒙皮；
- Shader Graph 或第三方框架已经定义了不可替换的阈值、空间和预览生命周期；
- 需求实际是透明排序、深度、Stencil、阴影 Pass 或 RenderQueue 问题，而不是溶解值计算。

## 2. 前提与依赖

### 2.1 冻结公开接口

修改前记录：

| 类别 | 需要确认的内容 |
| --- | --- |
| Shader 接口 | 阈值、噪声贴图、Tiling/Offset、轴向、上下界、模式、边缘宽度/颜色、Keyword。 |
| 空间输入 | `positionOS`、`positionWS`、`positionCS`、模型 UV 是否都能从 Vertex 传到 Fragment。 |
| Renderer | MeshRenderer/SkinnedMeshRenderer 数量、材质槽、嵌套局部坐标、换装和 LOD 行为。 |
| 材质所有权 | shared asset、运行时实例、MaterialPropertyBlock、编辑器临时副本分别由谁创建和恢复。 |
| 动画入口 | Animator、Timeline、代码曲线、静态 Inspector 参数或对象池重置。 |
| Pass 范围 | Forward、Outline、Depth、Shadow、透明深度等哪些 Pass 必须同步裁剪。 |

先确认这些契约，再决定采样空间和脚本结构。只看最终颜色无法判断问题来自坐标、阈值、材质引用还是渲染 Pass。

### 2.2 明确四类空间

以下概念可以共享配置，但不能混为一谈：

1. 轴向推进空间：决定“从哪里向哪里溶解”。
2. 噪声采样空间：决定噪声图案附着在哪里。
3. 边缘消散流动空间：决定噪声沿哪个二维平面流动。
4. 顶点偏移空间：决定被溶解部位向本地、世界方向或法线方向移动。

出现“X 轴不生效”“屏幕空间只有 Y 变化”时，应逐项检查，而不是只改一个方向向量。

## 3. 采样空间选择

### 3.1 决策表

| 采样方式 | 常见计算 | 优点 | 风险与缺点 | 适用场景 |
| --- | --- | --- | --- | --- |
| 模型 UV | `uv * tiling + offset` | 成本低、贴图可控、艺术资源容易预览。 | UV 接缝、拉伸和密度不一致；不同部件的展开难以连续。 | 明确要求沿纹理图案溶解、兼容旧材质。 |
| 本地空间 | 将 `positionOS` 投影到稳定二维平面。 | 跟随模型移动和旋转，不受相机缩放；不依赖 UV 质量。 | 多 Renderer 的局部坐标、原点和缩放可能不同；需要统一轴向和范围策略。 | 角色自身的空间溶解、希望图案附着于模型。 |
| 世界空间 | 将 `positionWS` 投影到世界二维平面。 | 多部件可共享世界图案；角色旋转不改变世界方向。 | 角色移动会穿过噪声场；世界坐标过大时需关注精度；范围随姿态变化。 | 世界固定扫描、魔法场、多个对象共享图案。 |
| 屏幕空间 | `GetNormalizedScreenSpaceUV(positionCS)`。 | 方向直接对应镜头画面；不依赖模型 UV；适合演出控制。 | 相机移动、FOV 和缩放会让图案相对模型滑动；宽高比和动态分辨率必须处理。 | 镜头特写、画面方向明确的死亡消散。 |

### 3.2 本地/世界投影平面

定向溶解通常已有一根归一化轴 `axis`。噪声可以投影到垂直于该轴的平面：

```hlsl
float3 fallbackUp = abs(axis.y) < 0.999
    ? float3(0.0, 1.0, 0.0)
    : float3(1.0, 0.0, 0.0);
float3 basisU = normalize(cross(fallbackUp, axis));
float3 basisV = cross(axis, basisU);
float2 projectedUV = float2(dot(position, basisU), dot(position, basisV));
```

`fallbackUp` 避免轴向接近世界 Y 时叉积退化。Local 使用 `positionOS + local axis`；World 使用 `positionWS + shared world axis`。如果每个子 Renderer 都把世界轴再次按自身矩阵转换，会导致不同部件方向不一致。

### 3.3 屏幕空间方向

屏幕坐标以宽高归一化后，视觉长度仍受画幅比影响。将三维方向变到 View Space 后，可按 aspect 修正 X：

```hlsl
float aspect = screenWidth / max(screenHeight, 1.0);
float2 dir = normalize(float2(axisVS.x / aspect, axisVS.y));
```

手动屏幕方向应按相机 `right/up/forward` 解释，而不是直接把世界 X/Y 当成屏幕 X/Y。透视/正交、动态分辨率、竖屏/横屏都需要实测。

### 3.4 非定向模式

“非定向”只表示不使用轴向进度，不等于必须回到模型 UV。若产品希望空间设置同时改变噪声采样，非定向模式应继续走同一个 Local/World/Screen UV helper，只跳过 `axis01` 计算。

如果旧资源依赖模型 UV，应显式保留兼容模式或迁移规则，不能静默改变所有存量材质。

## 4. 0 到 1 的阈值语义

### 4.1 先定义值域

设 `dissolveValue` 越大越可见，统一裁剪表达式为：

```hlsl
clip(dissolveValue - threshold - epsilon);
```

- `threshold = 0`：应完全可见。
- `threshold = 1`：应完全裁剪。
- `epsilon` 用于确保等于上边界的样本也被裁剪。

如果 `threshold = 0` 还承担“全局关闭”，应在 `clip` 前显式旁路：

```hlsl
if (threshold <= epsilon)
{
    return;
}
```

这比仅依赖噪声贴图恰好大于 0 更可靠，也避免关闭开关后仍出现零星裁剪。

### 4.2 噪声不能破坏端点

直接使用 `axis01 + centeredNoise * strength` 会让 `threshold=0` 或 `1` 仍有残留。更稳定的做法是让噪声在两端淡出：

```hlsl
float centeredNoise = (noise - 0.5) * 2.0;
float edgeFade = saturate(min(threshold, 1.0 - threshold) * 2.0);
float noisyThreshold = threshold + centeredNoise * noiseStrength * edgeFade;
float dissolveValue = axis01 - noisyThreshold + threshold;
```

先验证无噪声的严格边界，再增加噪声、边缘色、边缘消散和顶点偏移。艺术扰动不得改变全局进度语义。

### 4.3 进度映射

脚本把外部进度映射到阈值时应处理上下界退化：

```csharp
float end = Mathf.Max(progressEnd, progressStart + epsilon);
float threshold = Mathf.Clamp01(Mathf.InverseLerp(progressStart, end, progress));
```

全局开关关闭时直接输出 `0`，不要只关闭 Inspector 显示或 Keyword 而保留旧阈值。

## 5. 定向溶解的上下界

定向溶解通常先把轴向坐标归一化：

```hlsl
float axis01 = saturate(
    (axisCoord - axisMin) / max(axisMax - axisMin, epsilon));
```

`axisMin/axisMax` 是空间范围，不是第二套播放进度：

- 自动范围：从目标 Renderer 包围盒角点投影得到，适合不同身高、动作和多部件角色。
- 手动范围：适合演出严格对齐、跨角色固定标尺或自动 bounds 不可靠的对象。
- Local bounds 可按脏标记缓存；World/Screen 范围依赖当前姿态、位置和相机，通常需要在播放期间重算。

范围过小会提前整块消失；范围过大会让进度集中在少数帧。排查“棋盘格/噪声只有几帧变化”时，除了算法阈值离散度，也应检查 `axisMin/axisMax`、播放曲线区间和最终 dither/透明阶段是否叠加。

## 6. 骨骼蒙皮后的阈值传递

SkinnedMeshRenderer 不需要给每根骨骼单独写阈值。阈值是 Renderer/材质级状态，骨骼只负责生成变形后的顶点位置：

1. 控制器挂在角色根或稳定表现根。
2. 收集其下所有目标 MeshRenderer/SkinnedMeshRenderer；显式排除武器、特效或不支持的 Shader。
3. Vertex 阶段先完成需要的顶点偏移，再得到 `positionWS/positionCS`。
4. Fragment 用变形后的位置计算 Local/World/Screen 坐标。
5. 对每个 Renderer 的每个材质槽写相同的阈值、轴向、范围和噪声参数。

换装、LOD 和对象池可能在初始化后新增 Renderer 或替换材质。运行时应重新扫描目标，并比较每个材质槽的实例引用，而不是只比较数组长度。

## 7. 材质实例化与参数写入

### 7.1 三类对象的边界

| 对象 | 适合用途 | 主要风险 |
| --- | --- | --- |
| `sharedMaterials` | 读取原始资产引用、编辑器预览保存/恢复。 | 直接修改会污染所有使用同一材质的对象和资产。 |
| `materials` | 运行时每个 Renderer 的独立材质状态、Keyword、RenderQueue。 | 会实例化材质；需要缓存、复用和生命周期管理。 |
| `MaterialPropertyBlock` | 每 Renderer/材质槽的高频数值、纹理覆盖。 | 不控制所有 Keyword/RenderState；可能被其他系统清空或覆盖。 |

推荐运行时同时处理：

- Keyword、Blend、ZWrite、RenderQueue 等材质状态写实例材质。
- 阈值、颜色、方向等参数写每槽 MPB；若其他系统会重建 MPB，也同步写实例材质作为基线。
- 写入前读取现有 MPB，再只修改当前 owner 的字段，避免清空其他表现系统的数据。

### 7.2 缓存失效

材质缓存必须在以下情况失效：

- Renderer 新增/销毁；
- 材质槽数量变化；
- 槽位数量不变但引用被换装系统替换；
- 对象池复用；
- Shader 或 Keyword 所有者改变。

只缓存 `sharedMaterials` 或只在 `Awake` 扫描一次，容易产生“脚本写了但画面不变”的假象。

## 8. 编辑器预览与撤回丢材质

### 8.1 安全预览模型

进入预览时：

1. clone `Renderer.sharedMaterials` 数组容器。
2. 保存每个材质槽原始 MPB。
3. 为非空材质创建 `HideAndDontSave` 临时副本。
4. 将 Renderer 指向预览材质数组。
5. 后续预览参数只写临时材质和预览 MPB。

退出预览时：

1. 先恢复原始材质数组。
2. 恢复每槽原始 MPB。
3. 再销毁临时材质。
4. 清理缓存和 owner 记录。

顺序不能反过来。先销毁临时材质、后恢复 Renderer，Unity 可能在序列化重绑过程中保存已销毁引用。

### 8.2 多预览 owner

同一个 Renderer 可能同时被场景组件、嵌套 Prefab 或多个 Inspector 预览。如果每个会话独立保存“当前材质”，后进入者可能把前一个会话的临时材质当成原材质。

可按 Renderer 建立共享 lease：保存唯一的原始数组、预览数组、原始 MPB 和 owner 集合。只有最后一个 owner 退出时才恢复并销毁。

### 8.3 Undo/Animation Mode 时序

Undo、Animation Mode 退出、Prefab 重建或组件禁用可能先触发 `OnDisable`，随后才完成 Renderer 的序列化材质数组重绑。在 `OnDisable` 中立即 `DestroyImmediate` 会导致 Unity 随后写回已销毁引用，材质槽显示 `None`。

更安全的方式是：

- Undo/Animation Mode 回调只安排退出；
- 使用一个 editor tick 的延迟，等 Unity 状态稳定后恢复并释放；
- 活动 lease 期间若 Unity 暂时恢复原数组，重新挂回同一份预览数组；
- 没有活动预览时，编辑器参数写入函数直接返回，禁止写入真实材质资产。

## 9. 调试技巧

### 9.1 分层输出

按顺序临时输出：

1. 原始 noise。
2. `axisCoord` / `axis01`。
3. `dissolveValue`。
4. `dissolveValue - threshold`。
5. edge band / edge noise。
6. 最终 clip 前后覆盖率。

不要同时调颜色、噪声、范围和透明度。每次只验证一层，并在完成后移除临时 `return` 和 Debug 分支。

### 9.2 CPU 与 Shader 对照

- Inspector 显示解析后的轴向和 min/max。
- Frame Debugger/RenderDoc 检查目标 Pass、Keyword、材质实例和 CBUFFER/MPB 值。
- 检查每个 Renderer/材质槽，不要只看第一个 body 材质。
- 检查 Outline、Depth、Shadow 或透明深度 Pass 是否也执行同样裁剪。
- 检查相机宽高比、动态分辨率、Game View/Scene View 和多相机差异。

### 9.3 材质丢失排查顺序

1. 区分材质槽 `None`、已销毁临时对象和 Shader 裁剪导致不可见。
2. 查当前是否处于预览、Undo、Animation Mode、Prefab Stage 或脚本域重载。
3. 查原始/预览数组长度、每槽引用、owner 数和恢复顺序。
4. 查 `OnDisable`/`OnDestroy` 是否重复释放。
5. 运行时再查 `materials` 缓存、换装重绑定和 MPB 覆盖。
6. 最后检查 Keyword、RenderQueue、Blend、ZWrite 和 clip。

## 10. 风险与不适用边界

- 屏幕空间图案天然随相机变化；不能同时要求它严格锁屏又完全附着模型。
- 世界空间图案天然随对象位移产生穿越；这是空间选择结果，不是 UV 漂移 Bug。
- 非均匀模型缩放和非均匀 noise tiling 会改变视觉方向和速度，需要按最终资产验证。
- 顶点偏移若在所有顶点统一执行，会造成整体漂移；只移动溶解部位时需要顶点阶段可计算或近似同一阈值遮罩。
- MPB 不替代材质 Keyword 和 RenderState，材质实例也不替代每 Renderer 的独立参数。
- 编辑器静态检查不能证明 Undo/Prefab/Animation Mode 生命周期正确，必须实际操作复验。

## 11. 验证与回退

### 11.1 最小验证矩阵

| 维度 | 条件 |
| --- | --- |
| 阈值 | 开关关闭、0、接近 0、0.5、接近 1、1。 |
| 空间 | UV、Local、World、Screen；旋转模型、移动对象、移动/缩放相机。 |
| Renderer | Mesh、SkinnedMesh、多部件、多材质槽、换装、LOD、对象池。 |
| 模式 | 定向、非定向、边缘消散开/关、顶点偏移开/关。 |
| 编辑器 | 进入/退出预览、Undo/Redo、Animation Mode、Prefab Stage、禁用组件、场景关闭、域重载。 |
| Pass | Forward、Outline、Depth、Shadow、透明深度和实际消费者。 |

### 11.2 回退

回退时按最小影响顺序：

1. 关闭边缘消散和顶点偏移，保留基础阈值。
2. 关闭噪声强度，验证纯轴向 `axis01`。
3. 回退到已知采样空间，不改变材质属性名和序列化值。
4. 编辑器预览问题只回退预览生命周期，不删除运行时溶解接口。
5. 不通过删除材质资产、清空 Renderer 材质槽或回滚整个工作区处理局部故障。
