<!-- BEGIN automation:ta date:2026-11-16 layer:REFERENCE -->
# Shader 坐标空间、内部径向效果与 UI 模糊链参考｜2026-11-16

> 类型：REFERENCE；适用范围：Shader 光照方向、表面上的内部球形效果和 UI 图标模糊；使用前提：以目标管线、相机、材质、Renderer、图集和设备重新验证。
> 关联既有规则：`DOC-03`、`CMP-01`、`ARC-03`、`VAL-03`；GammaUI Pass 与颜色域见 [NBShader GammaUI 接入参考](../nbshader-2d-ui-gammaui-integration.md)；ProjectACG 当日实现见 [Shader Profile](../../Profiles/ProjectACG/CodexDaily/Development_Profile_2026-11-16.md)。

## 1. 适用场景

- 旋转模型时，需要判断光照方向应固定在世界中，还是随模型一起转动。
- 在表面上模拟一个模型内部的球形亮斑，尤其是球体、圆柱等表面到中心距离变化很小的形状。
- UI 图标需要低分辨率 Dual Kawase 模糊、图集边界隔离、按图标开关和材质级强度控制。

## 2. 前提与依赖

- 先声明每个向量、中心和距离使用的坐标空间。光照方向、法线、球心和观察射线必须转换到同一空间后再参与点积或距离计算。
- 内部发光若只在表面片元计算，应明确目标是表面投影近似还是体积透射；二者的物理含义和验证方法不同。
- UI 模糊要核对 GammaUI/普通 UI Pass、预乘 Alpha、Stencil、RectMask2D、AlphaClip、SpriteAtlas UV、UI Camera 与 RendererFeature 顺序。
- 将“模糊半径”“单图标模糊混合量”和“最终合成强度”分成不同参数；确认每个参数由材质、组件还是共享 Feature 拥有。

## 3. 实现或排查步骤

### 3.1 让光照坐标语义一致

1. 若方向固定在世界中，将法线变换到 World Space 后与世界方向计算 Lambert；模型旋转时受光面随之改变。
2. 若方向要固定在模型上，则方向与法线都使用 Object Space；模型旋转不应改变模型内部的明暗分布。
3. 改坐标空间时同步改材质字段名和序列化值，避免 Inspector 继续显示旧语义；再验证旋转模型前后的明暗变化。

### 3.2 将球形内部发光投影到表面

1. 先区分表面点到球心的距离与视线到内部球心的最近距离。直接使用 `distance(positionOS, center)` 时，球体表面的距离近似常量，不能形成中心亮、边缘暗的圆形核心。
2. 若目标是内部球体的可见截面，可沿观察方向进入模型，计算该射线离 Object Space 球心的最近距离，再按半径和平滑径向曲线衰减。
3. 单独控制球心、半径、HDR 颜色与遮罩；先关闭噪声等附加项确认几何衰减，再逐个加回。
4. 说明该方法是沿视线的表面投影近似，不是真实体积透射或散射；观察方向、模型局部坐标和球体与表面的相对位置都会影响结果。

### 3.3 控制 UI 图标的 Dual Kawase 模糊

1. 分清图标 Source Draw、Downsample/Upsample Filter 与最终 Composite。不同阶段的 Render Pass 不是重复绘制同一图标。
2. 低分辨率链负责模糊采样；全屏合成若需要从相机颜色读并写回同一目标，通常仍需独立的相机分辨率副本以避免读写冲突。用 Frame Debugger 确认真实 RT 尺寸和 Blit 数量。
3. 用按图标启用状态控制是否进入 Blur 链。`BlurStrength = 0` 应回到清晰图标并退出模糊注册，不能把图标隐藏；强度表示清晰/模糊贡献，不等于半径。
4. 图标级颜色、强度和图集矩形可放到图标组件/材质；共享 `spread`、`iterations` 和 `downsample` 留在 RendererFeature。需要逐图标动画的属性必须由 AnimationClip 能驱动的组件或材质实例提供，并刷新 UI 材质。
5. 图集采样限制在该 Sprite 的 UV 矩形内；需要不同矩形或属性时使用独立材质实例，避免串采相邻图标。

## 4. 风险与不适用边界

- 世界空间与物体空间提供不同的旋转语义；只变换法线或只变换方向会造成不一致结果。
- 射线最近距离法呈现的是球形光源投影近似，不能证明存在正确的体积吸收、折射或透射。
- 下采样可降低滤波像素数量，但全分辨率合成和相机副本仍有带宽/显存成本；一个低分辨率 Blur RT 不代表整条链都低分辨率。
- `BlurStrength` 为零时跳过模糊链可节省采样，但 RenderFeature 仍可能被 URP 每帧调用并做轻量条件判断。
- 隔离 Shader 编译、harness 或独立像素测试不能证明目标 GameView、Mask/Canvas 组合、移动端 GPU 时间和发热达标。

## 5. 验证与回退

- 坐标空间：分别旋转物体并移动世界灯向量，检查预期的受光面是否变化；比较中心、半径边缘和球面/圆柱面。
- Shader 功能：测默认关闭、遮罩边界、HDR、透明度、噪声关闭和不同模型；为关键像素断言保存本轮输入与输出。
- UI：检查强度 0/中间值/最大值、Atlas UV、Stencil、RectMask2D、AlphaClip、Canvas 排序、RendererFeature 开关和 AnimationClip 播放；Frame Debugger 区分 RT 尺寸、Draw、Blit 与 Copy。
- 性能：在目标分辨率与设备上测 GPU 时间、临时 RT 格式/尺寸和带宽；比较不同 `downsample`、`iterations` 与无启用图标的基线。
- 回退时关闭独立 Feature/材质路径，保留原 GammaUI 材质和旧 Shader；视觉或性能不达标时先减少模糊层级或回到已验证的旧实现。

## 来源与证据边界

- [BaseLitGalaxy 内部发光会话](../../../../VA_Worlk_memory/sessions/01a1d320-8c76-7d73-b99f-fd89b5b32b66.md)
- [世界空间 Lambert 与外描边 Shader 会话](../../../../VA_Worlk_memory/sessions/01a1e293-676a-7c00-8a16-868eb54acef8.md)
- [FlowMap 晶体内发光会话](../../../../VA_Worlk_memory/sessions/01a1d02b-41cb-7cd1-a5e1-b9ccad6f60fe.md)
- [ProjectACG UI Dual Kawase Blur 会话](../../../../VA_Worlk_memory/sessions/01a1e275-b375-7483-8842-f85daa399b89.md)
- [独立 UI Shader 需求调整会话](../../../../VA_Worlk_memory/sessions/01a1c5c9-7942-7061-9a27-699e9e25fa9e.md)

射线投影和坐标空间数字来自会话报告；Dual Kawase 的 GameView 与移动设备性能未验。独立 Shader 需求会话只记录了从组件改为 Shader-only 及“实现这个魔术”的方向调整，没有留下该要求的实现或验证结果；其范围候选见 ProjectACG Profile。
<!-- END automation:ta date:2026-11-16 layer:REFERENCE -->
