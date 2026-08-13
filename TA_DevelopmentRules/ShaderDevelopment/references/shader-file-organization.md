# Shader 文件组织与职责参考

> **用途**：为 `ORG-01`、`ORG-03`、`WRK-04` 提供生成和重构时的最小文件边界检查。本文不规定固定三文件模板；实际项目 Profile 和既有 Shader 的职责分层优先。

## 1. 文件边界选择

| 情况 | 推荐组织 | 禁止事项 |
| --- | --- | --- |
| 简单、非 PBR、无复用的 Shader | 单一 `.shader`，局部 HLSL 就近维护。 | 为少量代码创建空 `.hlsl` 或伪通用目录。 |
| 同一 Shader 的多 Pass 或同一功能族复用 | 提取功能域 Include，例如输入、Pass 或数学函数。 | Include 反向依赖具体 Shader 或形成循环引用。 |
| 多个独立 Shader 稳定复用的无状态逻辑 | 提取通用函数库，API 显式传入资源和数据。 | 在通用库中隐式绑定材质属性、Keyword、CBUFFER 或 Render State。 |
| 既有项目已有 Binding/Common/Adapter | 保持当前职责和依赖方向，按最小落点维护。 | 为统一命名而强制迁移为外部模板的文件结构。 |

## 2. 代码职责定位

| 改动类型 | 首选落点 | 连带检查 |
| --- | --- | --- |
| Base、Mask、Normal、Emission、Alpha、局部混合 | Surface/材质输入层 | Alpha Clip 与全部消费者 Pass。 |
| 坐标、视线、TBN、shadowCoord、Fog、GI、Lightmap | InputData/插值输入层 | Varyings 声明、顶点赋值、片元读取。 |
| BRDF、直接/间接光、Toon Ramp、SSS、最终合成 | Lighting/Final Blend 或既有 Adapter | 共享 Kernel 的全部消费者与目标设备。 |
| 顶点位移、顶点动画、网格投射 | 顶点阶段与 Varyings | Forward、ShadowCaster、DepthOnly、DepthNormals、Meta 和专用 Pass。 |

## 3. 绑定与跨 Pass 自检

1. `Varyings` 的条件字段在声明、顶点赋值、片元读取三处使用相同宏保护；依赖 Include 必须在使用前出现。
2. HLSL 读取的 `Properties`、CBUFFER、Texture/Sampler 名称和类型一致；仅供 ShaderLab/GUI/Render State 的属性不写入 `UnityPerMaterial`。
3. 贴图优先使用当前项目采用的 URP 宏；不得混入另一渲染管线的采样约定。
4. 顶点、Alpha Clip、透明和裁剪语义在所有实际消费者 Pass 保持一致。没有自定义几何或裁剪需求时，优先复用 URP/项目通用 Pass Include。
5. 新 Include 使用唯一 guard，声明、定义和调用完整；不得让函数依赖未记录的 Include 顺序、同名全局或隐式 Keyword。
