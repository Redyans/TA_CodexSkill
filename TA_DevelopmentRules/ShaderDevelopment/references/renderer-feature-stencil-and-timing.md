# RendererFeature Stencil 与屏幕时序参考

> **用途**：为 `ARC-03`、`ARC-04` 和 `VAL-02` 提供共享 Depth/Stencil、临时对象 ID、屏幕空间资源和历史缓存问题的排查与修复方法。具体 `RenderPassEvent`、附件格式、Pass 名称和缓存实现必须以目标项目源码与 Renderer 资产为准。

## 1. 先建立状态所有权表

涉及 RendererFeature、Custom Pass 或屏幕空间效果时，先逐项记录：

| 项目 | 必须确认的事实 |
| --- | --- |
| 状态载体 | 相机 Color/Depth/Stencil、独立 RT、全局纹理、历史缓存或静态分配器。 |
| 写入协议 | 写入 Pass、`RenderPassEvent`、`Ref`、`Comp`、`Pass`、`ReadMask`、`WriteMask`、ZTest、覆盖范围。 |
| 消费协议 | 消费 Pass、比较规则、是否依赖当前相机/当前帧、无有效数据时的默认值。 |
| 生命周期 | 创建/清理时机、场景切换、相机切换、对象启停、Domain Reload、多相机和 MSAA 边界。 |
| 性能成本 | 全屏 Fill、额外 Draw、临时 RT、带宽、Resolve、缓存更新和目标设备。 |

只看 Shader 的 Stencil 块或只看 RendererFeature Inspector 都不足以归因；必须把 C# Pass 排序、Renderer 资产序列化值、Shader Pass 和实际消费者连成一条链。

## 2. 典型失效模式

### 2.1 临时对象 ID 污染公共 Stencil

典型链路如下：

1. RendererFeature 为对象分配递增 ID。
2. 某个早期 Pass 用 `Comp Always / Pass Replace` 把 ID 写入相机 Stencil。
3. Feature 在自己的体积投影中用该 ID 做筛选，但完成后没有清理。
4. 后续角色、UI、Decal 或其他 Feature 按另一套 Stencil 数值协议比较同一字节。
5. 对象创建足够多、场景切换或 ID 回绕后，问题才稳定出现。

这类问题跨场景保留的通常是静态分配器或缓存状态，不一定是旧场景的 Stencil 像素。必须分别检查“编号从哪里来”和“当前帧是谁把编号重新写进附件”。

### 2.2 调整 Pass 时机掩盖冲突

把生产 Pass 移到消费者之后，可能让 Stencil 冲突暂时消失，但同时改变资源时序：

- Opaque 之前生成：通常可消费当前帧结果，但要求所需 Depth/Stencil 已有效。
- Opaque 之后生成：Opaque 只能使用默认值或历史缓存；若复用上一帧屏幕纹理，运动相机容易出现一帧错位。
- 更晚阶段生成：还要重新评估透明、后处理、相机堆叠和全局纹理覆盖。

因此必须把“静态画面正确”和“运动时同帧对齐”分开验证。时序调整只有在生产者/消费者契约本来就允许历史帧时才是有效方案。

## 3. 修复策略选择

| 策略 | 优点 | 风险/成本 | 适用条件 |
| --- | --- | --- | --- |
| 独立 ID Mask RT | 状态隔离最清晰，不占公共 Stencil 数值空间。 | 新增 RT、带宽、采样和 Shader 改造。 | 多系统共享 Stencil、对象 ID 范围大、长期维护优先。 |
| 私有 Depth-Stencil 附件 | 可继续使用硬件 Stencil Test。 | 全分辨率深度/Stencil 内存，MSAA 成本高；可能需要复制/重建深度。 | 早期路径可用 `ZTest Always`，或能可靠提供私有深度。 |
| 定向 Stencil 清理 | 改动小，不清除其他数值；无需新增全屏 RT。 | 对同一批 Renderer/SubMesh 增加 Stencil-only Draw；必须保证清理几何和 ID 与写入一致。 | 临时 ID 只在一个封闭 Pass 窗口内使用。 |
| 全屏 Stencil-only Clear | GPU 操作直接，通常比重画复杂几何便宜。 | 会清除所有早期 Stencil 所有者；共享 Renderer 中风险大。 | 已完整审计清理点之前不存在需要保留的 Stencil。 |

不得把以下方式单独当成根治：

- 把 ID 限制在某个看似安全的数值区间；未证明活动对象唯一性时会发生碰撞。
- 仅在场景切换或 `OnDisable` 重置分配器；不能解决同场景数量增长和公共附件污染。
- 只移动到 `AfterOpaques`；可能用视觉延迟交换 Stencil 正确。
- 开启名字中带 Eye/Depth/Prepass 的独立选项；必须先证明存在真实生产者、Shader Pass 和消费者。

## 4. 定向清理实现模式

推荐顺序：

```text
写入临时 Stencil ID
  → 完成所有依赖该 ID 的投影/合成
  → 对相同 Renderer 和相同 ID 执行定向清理
  → 后续 Stencil 所有者开始绘制
```

清理 Pass 的核心状态：

```shaderlab
Stencil
{
    Ref [_StencilRef]
    Comp Equal
    Pass Zero
    Fail Keep
    ZFail Keep
}
ZWrite Off
ColorMask 0
```

`ZTest` 和 `Cull` 必须根据写入 Pass 的覆盖规则决定。若写入路径使用 `ZTest Always`，清理通常也需要覆盖所有写入像素；若写入受相机深度限制，必须证明清理仍能覆盖所有已写像素。重画顺序不是安全性的替代品，ID 碰撞必须在分配层单独处理。

## 5. 排查与验证矩阵

### 5.1 静态检查

- 查找所有 `Stencil`、`Ref`、`Comp`、`ReadMask`、`WriteMask`、`Pass Replace/Zero`。
- 查找所有 `RenderPassEvent`、Renderer 资产时机字段、`ConfigureTarget` 和相机 Depth/Stencil 绑定。
- 查找静态 ID/缓存的初始化、递增、回绕、回收、场景/相机清理和 Domain Reload 行为。
- 查找屏幕纹理在 Opaque 前绑定的是本帧结果、默认纹理还是历史缓存。
- 查找 Inspector 开关对应的真实 Shader Tag、生产纹理和消费者；没有闭环的开关不得作为修复建议。

### 5.2 运行时验证

1. 直接进入目标场景，与经过高对象创建量场景后再进入目标场景做 A/B。
2. 快速平移/旋转相机，区分静态正确与历史帧拖后。
3. 覆盖多角色、武器/附件、对象反复启停、ID 超过消费者阈值和 ID 回绕边界。
4. 覆盖环境阴影、自阴影、透明、Outline、PlanarShadow、Eye/Fringe 等相邻消费者。
5. 覆盖实际质量 Renderer、Game/SceneView/额外相机、相机堆叠、MSAA 和目标图形 API。
6. 用 Frame Debugger 或 RenderDoc 确认写入、最后一次消费、清理和下一所有者的实际顺序；必要时在关键 Draw 后检查 Stencil 值。
7. Profiler 对比额外 Draw、顶点量、全屏 Fill、临时 RT 内存和 Resolve 成本。

### 5.3 调试对象 ID

私有字段不应靠猜测判断。优先使用公开只读属性、Debugger Watch、临时开发日志或受控自定义 Inspector 查看“对象名、实例 ID、分配 ID、活动状态、相机和帧号”。日志只用于开发验证，确认后删除或受开关控制，不能把每帧输出留在运行路径。

## 6. 交付记录

此类修复至少记录：

- 现象和可复现路径，包括是否依赖场景顺序或对象数量。
- 共享状态生产者、消费者、冲突数值/位和实际 Pass 顺序。
- 选择当前修复而不选时序后移、ID 限幅、全屏清理或独立 RT 的原因。
- C#/Shader 编译、Frame Debugger、运动镜头、多场景、多角色和性能验证结果。
- 未覆盖的平台、Renderer、相机类型、MSAA、ID 回绕和剩余风险。
