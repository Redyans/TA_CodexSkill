# ProjectACG 严格复刻《明日方舟：终末地》CB3 光照与角色材质效果总方案

> 文档日期：2026-09-13  
> 状态：设计与证据汇总；尚未进入实现  
> 目标工程：ProjectACG，Unity 2022.3.62f3，URP 14.0.12  
> 证据来源：本包 `evidence/endfield/` 目录  
> ProjectACG 现状快照：本包 `evidence/projectacg-current/` 目录

## 0. 文档目的

本文档给出在 ProjectACG 中严格还原《明日方舟：终末地》CB3 光照和角色材质强化体系的完整落地方案，包括：

1. 终末地现有证据及其可信边界；
2. 已确认的环境、角色页、抽卡、大招和假光材质参数；
3. ProjectACG 当前渲染能力与差距；
4. 全部候选实现路线、主推路线及不选原因；
5. 目标架构、资源结构、运行时状态和生命周期；
6. 分阶段实施、验证、回滚和交付标准；
7. 当前不能凭空补齐的参数与正式实现前的证据门。

本文档不把推断写成事实。参数按以下等级管理：

| 等级 | 定义 | 使用规则 |
|---|---|---|
| A | 资源、组件、运行时代码或绑定直接证明 | 可进入导入清单，不允许人工改写 |
| B | 结构或字段已经证明，但最终 Shader 数学、枚举或运行时语义尚未恢复 | 先恢复原实现，再决定 ProjectACG 执行后端 |
| C | 当前没有足够证据 | 禁止给默认值，禁止凭经验调参 |

---

## 1. 总结论

终末地不是“一个 Directional Light + PBR 材质”的方案，而是至少五层叠加：

1. 环境主直射光、Sky、Fog、Exposure、Shadow；
2. 场景 Point/Spot、Irradiance Volume、Reflection Probe/Cubemap；
3. 每角色、每页签独立的 Point/Spot 灯组；
4. 大招 Timeline 动态灯、角色 Volume、Cubemap 和独立角色 Bloom；
5. Emissive/Fresnel/Dissolve 假光材质。

严格复刻不能只改角色 Shader，也不能只增加几盏灯。ProjectACG 必须同时完成环境、角色灯组、角色 Volume、间接光、Timeline、Bloom、假光材质和质量档治理。

当前可以确认的核心机制是：

- 真实 Point/Spot Light 与假光材质分开使用；
- 角色灯使用 character-only 隔离背景；
- 每灯拥有 Metal-only、Roughness、NPR Ramp/SDF、Rim 和体积散射参数；
- 资料页按角色和页签切灯组，旧/新灯组交叉淡变 1 秒；
- 抽卡强制使用 Overview 灯组，并创建角色专用 Irradiance Volume；
- 大招逐镜头动画化灯的 Active、Intensity、Color、Transform 和 NPR 参数；
- Character Volume 负责 Cubemap、环境阴影、主光偏置和角色 Bloom。

### 1.1 严格复刻必须先补齐的证据

以下三项是正式实现前的硬门槛：

1. 恢复 `HGAdditionalLightData` 对应的 Shader 分支、枚举和强度/衰减传递函数；
2. 解码 24 个大招 ACL 压缩曲线的逐帧关键帧；
3. 恢复环境 Profile 的运行时优先级、继承、混合和区域切换规则。

此外还需要补齐：

- 554 盏角色页灯的完整 Transform、Spot/Inner Spot Angle 和 Shadow 数据导出；
- `m_lightNPRType` 数值到语义的映射；
- Reflection-Probe light mesh 的实际渲染语义；
- 角色 Bloom 的隔离与合成顺序；
- lv006 全量 Irradiance Volume / Reflection Probe 空间坐标；
- 终末地角色灯组到 ProjectACG 角色的显式映射；
- ProjectACG `Advance` 页签的灯组来源。

在这些项目未通过前，只能称为“架构和数据准备”，不能称为像素级严格复刻。

---

## 2. 终末地已确认参数

## 2.1 map01/lv006 环境 Profile

以下只把 `m_active=1` 的配置当作当前 Profile 的有效覆盖。`m_active=0` 的存储值不得当成运行时生效参数。

| Profile | 主光 | Direct Spec | 间接 D/S | Sky | 体积雾：距离 / Direct / Sky | 自动曝光 |
|---|---:|---:|---:|---|---|---|
| `art_01` | `40000 lux` | `1` | `1 / 1` | 开，Baked `2`，Direct `1` | `90 / 1 / 0` | `-6.5 ~ 4` |
| `entry_overview_01` | `20000` | `1` | `1 / 1` | 不覆盖 | `100 / 1 / 0` | 不覆盖 |
| `mining_area_01` | 不覆盖 | — | — | 不覆盖 | `150 / 0 / 1` | 不覆盖 |
| `mining_area_cave_01` | 不覆盖 | — | — | 不覆盖 | `80 / 0 / 0` | 不覆盖 |
| `boss_area_01` | `60000 lux` | `0` | `1 / 1` | 开，Baked `2`，Direct `10` | `200 / 1 / 0` | `-3 ~ -2` |
| `boss_tunnel` | 不覆盖 | — | — | 不覆盖 | `200 / 1 / 1` | `-2.5 ~ -2` |
| `erosion_area_01` | `20000` | `1` | `1 / 1` | 不覆盖 | `90 / 5 / 2` | 不覆盖 |
| `ruins_01` | `40000` | `1` | `1.5 / 1` | 不覆盖 | `200 / 1 / 2` | 不覆盖 |
| `tunnel_01` | `40000` | `1` | `1.5 / 1` | 不覆盖 | `157 / 1 / 0` | `-6.5 ~ 4` |

其他 A 级参数：

- `art_01`：`4800K`；
- `art_01`：Contrast `+10`；
- `art_01`：Contact Shadow `0.5`；
- `art_01`：默认反射 `T_reflectionprobe_envdefault2`；
- `boss_area_01`：`5500K`；
- 能源点局部环境球：Scale `80×80×80`；
- 能源点进入/离开淡变：`6s / 2s`。

注意：`40000/60000 lux` 是终末地 `HGLightConfig.directLux` 原始值。在终末地光照传递函数恢复前，不得直接等价为 ProjectACG URP Directional 的最终屏幕亮度。

## 2.2 普通场景局部灯与间接光

`lv006_spotlight_postmodel.prefab` 名称包含 spotlight，但实际为 Point Light：

- Intensity `77.9`；
- Range `10`；
- Color Temperature `3000K`；
- Bounce `2`；
- Metal-only Spec；
- NPR Rim Width `0.4`；
- Volumetric Scattering `1`。

`map01/lv001` 两个 lighting-deco marker：

- 4 Point + 2 Spot；
- Intensity `4–15`；
- Range `5–43`；
- 非烘焙；
- 非 character-only；
- 带 Reflection-Probe light mesh；
- 部分 Volumetric Scattering 为 `4 / 20`。

Reflection Probe 样本：

- Resolution `128×128`；
- 6 faces；
- 8 mip。

该 Probe 只是普通场景样本，不能泛化为角色资料页固定分辨率。

## 2.3 角色资料页灯组

| 页面 | 总灯数 | 每角色 | 类型 | 原始强度范围 | Range 范围 |
|---|---:|---:|---|---:|---:|
| Overview | 206 | 7–13 | 132 Point + 74 Spot | `0.07–50` | `0.03845215–4.56` |
| Skill | 197 | 6–12 | 149 Point + 48 Spot | `0.1–57.1` | `0.026775643–7.4` |
| Equip | 151 | 4–9 | 110 Point + 41 Spot | `0.1–30` | `0.08274999–8.85` |

23 个角色源 Rig 的精确灯数，顺序为 `Overview / Skill / Equip`：

```text
light_chr_0002_endminm   12 / 10 / 7
light_chr_0004_pelica    10 /  7 / 5
light_chr_0005_chen       8 /  9 / 8
light_chr_0006_wolfgd     9 /  9 / 9
light_chr_0007_ikut       9 / 10 / 8
light_chr_0009_azrila    10 /  9 / 5
light_chr_0011_seraph     7 /  7 / 6
light_chr_0012_avywen     9 /  9 / 9
light_chr_0013_aglina    13 / 11 / 8
light_chr_0014_aurora     8 /  7 / 5
light_chr_0015_lifeng     9 /  8 / 7
light_chr_0016_laevat    10 /  8 / 8
light_chr_0017_yvonne    11 /  8 / 7
light_chr_0018_dapan     10 / 10 / 8
light_chr_0019_karin      7 /  8 / 6
light_chr_0020_meurs      8 /  6 / 7
light_chr_0021_whiten     7 /  9 / 6
light_chr_0022_bounda     7 /  7 / 4
light_chr_0023_antal      9 /  7 / 6
light_chr_0024_deepfin    9 / 10 / 5
light_chr_0025_ardelia    8 /  8 / 5
light_chr_0026_lastrite   7 /  8 / 4
light_chr_0029_pograni    9 / 12 / 8
```

554 盏灯公共约束：

- Native Unity Light：`554/554`；
- Specular Intensity=`1`：`554/554`；
- `m_lightNPRSpecMetalOnly=1`：`554/554`；
- Roughness Max：
  - `0.6`：545；
  - `0.566`：5；
  - `0`：4；
- Roughness Bias：
  - `0.8`：460；
  - `0.95`：89；
  - `0`：5；
- Character-only：531；
- Volumetric Scattering=`1`：554；
- Reflection-Probe light mesh=`1`：554；
- Rim Width 主峰：
  - `1.0`：235；
  - `0.4`：180；
  - `0.3`：59。

每个 Skill 页唯一非 character-only 的 `FloorLight`：

- 共 23 盏；
- Intensity=`1.4`；
- Range=`7.4`；
- `m_LightCharacterOnly=0`。

运行时：

- 同一时刻只启用当前角色的当前页面灯组；
- 旧灯组和新灯组 `1s` 交叉淡变；
- 灯组通过 LightFollower 跟随角色模型；
- 抽卡只启用 `light_overview`；
- 抽卡应用 `volume_overview`；
- 抽卡创建独立 `Data/IrradianceVolume/<platform>/gacha/character`；
- 页签切换还会修改 Character Volume 的主光 Bias。

## 2.4 大招/技能演出

- AnimationClip：24 个；
- 全部 `60fps`；
- 时长 `1.3–2.833s`；
- 总绑定 459；
- `GameObject.m_IsActive`：211；
- `Light.m_Intensity`：65；
- `Light.m_Color` 通道：30；
- 其他 Light 属性：6。

代表样本：

- Laevat：6 灯；Rim 初始最高强度 `100`；
- Perlica：背光 Spot 初始强度 `206.19`；
- Lifeng：动态绑定 Active、Intensity、Transform 和 `m_lightNPRRampSDFBias`；
- 已解析样本灯均 character-only、metal-only；
- 大招样本 `enableLightMeshForReflectionProbe=0`，不能沿用角色资料页的 `1`。

18 个大招 Character Volume：

- `18/18` 覆盖非空 `charMaxCubemap`；
- Ambient Base=`1`；
- Directional Ambient=`0.6`；
- Directional Param=`0.15`；
- `14/18` 开启角色主光控制并覆盖环境阴影倍率；
- `13/18` 覆盖角色 Bloom，其中 9 开、4 关。

当前 ACL 关键帧尚未解码。只能建立属性通道，不能手写逐帧曲线。

## 2.5 通用假光材质

Fire、Lightning、Natural 三套 Rimlight 公共值：

- `_UseLighting=0`；
- `_UseForceDynamicLighting=0`；
- `_EnvLightStrength=0`；
- `_UseFresnel=1`；
- `_UseDissolve=0`；
- `_FresnelBias=0.455`；
- `_FresnelPower=1`；
- `_SrcBlend=1`；
- `_DstBlend=10`；
- `_AlphaSrcBlend=1`；
- `_AlphaDstBlend=10`；
- `_ZWrite=0`；
- Transparent。

| 字段 | Fire / `1248` | Lightning / `1249` | Natural / `1247` |
|---|---:|---:|---:|
| `_ExpIntensity` | `10.9` | `20` | `6.2` |
| `_ExpThreshold` | `0.352` | `0.5` | `0` |
| `_TintColorIntensity` | `3.8` | `35` | `3.8` |
| `_FresnelAffectOpacity` | `0.72` | `0` | `0.6` |
| `_FresnelColor` | `(1.1487,0.6216,0,1)` | `(1,0.8831,0.4104,0.6980)` | `(0.3349,1,0.6377,0.1804)` |
| `_BlendTint` | `(0,56.2196,383.4981,1)` | `(14.9285,13.9364,9.8866,1)` | `(2,1.8362,1.3396,1)` |

这些是 Linear/HDR 原始序列化值，不得先压成普通 0–1 色值。

---

## 3. ProjectACG 当前现状

## 3.1 工程与 URP

- Unity `2022.3.62f3`；
- URP `14.0.12`；
- Linear Color Space；
- Linear Light Intensity、Color Temperature 已开启；
- Lobby/Battle Mid/High/Ultra 使用 Per Pixel Additional Lights；
- Additional Lights Per Object Limit=`4`；
- Light Layers 关闭；
- Light Cookies 关闭；
- Low 关闭 HDR 和 Additional Lights。

以上配置无法原生承载终末地角色页最多 13 盏灯和 character-only 分层。

## 3.2 角色渲染双轨

当前存在两套相邻系统：

1. `CharacterRenderLightProfile` / `CharacterRenderController`
   - 主光覆盖；
   - 虚拟补光；
   - Additional Lights Diffuse/Specular；
   - Self/Environment/Specular Shadow；
   - Rim 与假光遮罩；
   - 间接光。
2. `CharacterMainLightController`
   - 通过 MaterialPropertyBlock 写入角色材质槽；
   - 自定义主光和环境光；
   - Cubemap；
   - Additional Lights；
   - PerObjectShadow、PlanarShadow。

正式落地必须统一收口到 `CharacterRender*`。旧 `CharacterMainLightController` 是迁移来源，不继续作为并行权威控制器。

## 3.3 V2 Shader 缺口

当前 `Chara_AdditionalLights_V2.hlsl`：

- Lambert Diffuse；
- 简化 D×V Specular；
- 只读取颜色、方向、距离和 Spot 衰减；
- 不采样额外灯阴影；
- 不支持 Metal-only、Roughness Max/Bias、NPR Ramp/SDF、每灯 Rim、character-only 和体积散射。

## 3.4 角色材质覆盖

`Client/Assets/AssetRaw/character/hero` 静态库存：

- `.mat` 101；
- V2 Shader 51；
- 已知旧版/非 V2 45；
- Shader GUID 未解析 5。

这是目录库存，不是 Prefab 实际引用覆盖率。实现前还要生成 `Prefab -> Renderer -> Material -> Shader` 真正使用链。

## 3.5 当前角色详情场景

`Map_ACG_Gacha_Detail.unity` 当前包含：

- Realtime Reflection Probe，Resolution `512`；
- Basic / Skill / Advance / Equip 等相机；
- 页面相机 FOV=`18°`；
- Directional Intensity=`0.5`；
- Point Intensity=`1`，Range=`1.5`；
- CharacterMainLightController Main Intensity=`1.2`。

终末地 Overview 相机约为 Unity 纵向 `20°`，但仍需在相同宽高比和相机口径下做最终确认。当前不能把“约 20°”写成所有页面精确值。

---

## 4. 目标架构

## 4.1 Environment Lighting Stack

建议建立明确的环境光照域：

```text
EnvironmentLightingProfile
├── DirectLightConfig
├── SkyConfig
├── FogConfig
├── HeightFogConfig
├── VolumetricFogConfig
├── ExposureConfig
├── ColorGradingConfig
├── LightShaftConfig
├── ShadowConfig
├── IrradianceVolumeReference
└── ReflectionProbeSet
```

运行时状态：

```text
State:
  BaseProfile
  ActiveRegionProfiles[]
  BlendWeights[]
  CurrentExposure
  CurrentIrradianceVolume
  CurrentReflectionSet

Input:
  SceneEntered
  RegionEntered
  RegionExited
  QualityChanged

Transition:
  BaseProfile + Region Overrides
  inactive 字段不覆盖
  EnergyPoint Enter 6s / Exit 2s

Effect:
  Direct Light / Sky / Fog / IV / Probe / Exposure / Shadow 同步更新
```

标准 URP Volume 可以作为部分字段执行后端，但只有在插值、曝光和合成顺序与终末地等价时才允许使用。不等价字段由自定义 `VolumeComponent` / `ScriptableRendererFeature` 实现。

## 4.2 Character Lighting Rig

角色灯光资源结构：

```text
CharacterLightingRigPrefab
├── light_overview
├── light_skill
├── light_equip
├── volume_overview
├── volume_skill
├── volume_equip
└── LightFollower
```

每盏灯保持终末地的 authoring 结构：

```text
UnityEngine.Light
└── CharacterAdditionalLightData
    ├── characterOnly
    ├── nprType
    ├── rampBias
    ├── rampShadowDimmer
    ├── rampSdfBias
    ├── rampSdfDramatic
    ├── specMetalOnly
    ├── specMaxRoughness
    ├── specRoughnessBias
    ├── rimWidth
    ├── rimAlbedoAlpha
    ├── fogAlpha
    ├── fogFalloffFactor
    ├── fogRampBias
    ├── fogDirectionalFalloff
    ├── volumetricScatteringIntensity
    ├── falloffExponent
    └── reflectionProbeLightMesh
```

原生 `Light` 保存 Type、Transform、Color、Intensity、Range、Spot Angle、Shadow、Color Temperature 和 Bounce。Additional Data 不能和 Light 参数重复维护。

## 4.3 Character Volume

统一扩展现有 `CharacterRender*`：

- `charMaxCubemap`；
- Ambient Base；
- Ambient Direction/Intensity/Param；
- Main Light Control；
- Main Light Multiplier；
- Environment Shadow Multiplier；
- Main Light Range Bias；
- Camera Follow Main Light Bias；
- Character Bloom override；
- Ignore Scene Additional Lights / Scene Environment。

`CharacterMainLightController` 的 Cubemap、环境光、目标 Renderer 和 MPB 写入能力迁入统一控制器；迁移完成后旧组件进入兼容/弃用状态。

## 4.4 角色页状态转换

```text
State:
  CharacterId
  PresentationMode: Detail / Gacha / Ultimate
  CurrentTab: Overview / Skill / Equip
  PreviousRig + CurrentRig
  CrossFadeWeight
  CharacterVolume
  PresentationVersion

Input:
  CharacterChanged
  TabChanged
  GachaEntered / GachaExited
  UltimateStarted / UltimateStopped
  QualityChanged

Transition:
  Detail: PreviousRig -> CurrentRig, 1s crossfade
  Gacha: force Overview + overview volume + gacha IV
  Ultimate: push scoped override; stop/cancel restores previous state

Effect:
  Activate rig
  Bind LightFollower
  Upload per-light data
  Apply cubemap/IV/bloom/main-light bias
  Switch action and camera under the same version/cancellation boundary
```

页签灯组切换接入 `OutGameCharacterSystem.OnCharacterDetailMainTabChanged` / `PlayDetailTabTransitionAsync`，不能从 UI 层直接切灯。

## 4.5 大招 Timeline

```text
AnimationTrack
  - Light Active
  - Intensity
  - Color
  - Transform

CharacterAdditionalLightTrack
  - NPR Type
  - Ramp Bias / Shadow Dimmer
  - SDF Bias / Dramatic
  - Roughness / Rim 等逐灯字段

CharacterRenderTrack
  - charMaxCubemap
  - Ambient 1 / 0.6 / 0.15
  - Main Light Control
  - Environment Shadow Multiplier

CharacterBloomTrack
  - characterBloomControl override

CharacterMaterialTrack
  - Emissive
  - Fresnel
  - Opacity
  - Dissolve
```

大招开始时 push Lighting Scope；正常结束、跳过、打断、死亡和场景卸载均必须恢复。

## 4.6 真灯和假光分离

- 需要角色或环境产生真实受光：Point/Spot + Additional Data；
- 只需要能量轮廓、剑光、手光、溶解边：Transparent Emissive/Fresnel/Dissolve；
- 假光不写入 IV、Reflection Probe 或场景直接光；
- 动态参数通过 MPB/Timeline 驱动，不实例化共享材质。

---

## 5. GPU 实现路线

当前证据确认的是终末地的组件和运行时语义，尚未确认 GPU 数据传输方式。最终路线必须由 Phase 0 证据选择。

## 5.1 方案 A：自定义角色灯光 Buffer

流程：

1. Native Light 用于编辑和 Timeline；
2. 运行时收集当前角色灯组与 Additional Data；
3. 按目标 Renderer/Rig 建立 light offset/count；
4. 通过 MPB 给 Renderer 写入 rig index 或 buffer range；
5. V2 Shader 单 Pass 遍历当前角色灯；
6. character-only 灯不进入普通场景 URP Light 列表。

优点：

- 不受 URP 每对象 4 灯限制；
- 一次材质 Pass 可处理完整灯组；
- 每灯 NPR 参数可完整上传；
- 容易按角色隔离。

风险：

- 必须恢复终末地数据布局和 Shader 数学；
- GLES/移动端 buffer 和分支成本需验证；
- FloorLight 与场景材质的接收路径需要单独设计。

这是 ProjectACG 当前最合适的等价候选，但不能在原始 GPU 路径恢复前声称与终末地实现相同。

## 5.2 方案 B：URP Forward+ + Light Layers

优点：

- 原生支持更多可见灯；
- 原生 Light authoring 成本低；
- Light Layers 可辅助隔离。

不足：

- `HGAdditionalLightData` 没有 URP 原生通道；
- Metal-only、Roughness Bias、NPR SDF、Rim Width 仍需自定义；
- 无证据证明终末地使用 Forward+；
- 单纯开启 Forward+ 不构成严格复刻。

仅适合快速验证灯位或在原始实现被证明等价时使用。

## 5.3 方案 C：逐灯 Additive Character Pass

优点：

- 每盏灯的数学、遮罩、Blend 和阴影完全可控；
- 适合作为逐灯 Shader Oracle 和差异诊断；
- 不受 URP per-object light limit。

不足：

- 7–13 灯显著增加角色 Renderer Draw；
- 移动端成本高；
- 尚无证据证明终末地采用此方案。

适合作为验证后端；只有恢复证据证明原方案为逐灯累加时才作为终局。

## 5.4 方案 D：仅使用现有 CharacterRender 虚拟光/Rim

该方案只能模拟画面方向，不满足严格复刻：

- 无法复刻每角色 4–13 盏局部灯；
- 无法复刻每灯 Metal-only/Roughness/NPR/Rim；
- 无法复刻真实 FloorLight、IV 和体积散射；
- 无法复刻大招逐灯曲线。

因此明确不采用。

## 5.5 路线选择规则

- 若终末地恢复结果为 packed light buffer：采用 A；
- 若恢复结果为 additive passes：采用 C；
- 只有证据证明 Forward+ 语义等价时才采用 B；
- D 永远不作为严格复刻终局。

---

## 6. ProjectACG 建议落点

| 目标 | 现有落点 | 计划 |
|---|---|---|
| 角色统一 Profile | `CharacterRenderLightProfile.cs` | 增加 Character Volume、Cubemap、环境阴影和 Rig 引用 |
| 统一运行时 Owner | `CharacterRenderController.cs` | 吸收旧 MPB/Cubemap 能力，管理 Rig 与 scoped override |
| 旧控制器 | `CharacterMainLightController.cs` | 作为迁移来源，最终兼容/弃用 |
| Additional Light Shader | `Chara_AdditionalLights_V2.hlsl` | 按恢复数学补齐每灯 NPR 分支 |
| 页签事务 | `OutGameCharacterSystem.cs` | 灯组、动作、相机、Volume 共用 version/cancellation |
| 角色详情场景 | `Map_ACG_Gacha_Detail.unity` | 由统一 Rig 取代固定统一两灯 |
| 大招 | `CharacterRenderTimeline*` / `CharacterMaterialTimeline*` | 增加 Light Rig、Additional Data、Volume、Bloom 通道 |
| 质量档 | `QualityModule` + 8 套 URP Asset | 建立明确 Full Fidelity 路线，低档不冒充严格复刻 |
| Editor 工具 | `Client/Assets/GameScripts/Editor/CharacterRender/` | 证据导入、Prefab 生成、覆盖扫描和自动校验 |

建议的新资源位置需在 OpenSpec 中最终确认，例如：

```text
Client/Assets/AssetRaw/CharacterLighting/
├── Rigs/<CharacterId>/
├── Volumes/<CharacterId>/
├── Cubemaps/
├── IrradianceVolumes/
└── Validation/
```

这是 ProjectACG 的建议资源组织，不是终末地原始路径证据。

---

## 7. 分阶段实施

## Phase 0：证据补全与参考基线

1. 导出 554 盏灯的完整 Light 与 Transform 数据；
2. 恢复 `HGAdditionalLightData` Shader 分支和枚举；
3. 恢复强度、衰减、Metal-only、Roughness、Rim、Ramp/SDF 数学；
4. 解码 24 个大招 ACL 曲线；
5. 恢复环境 Profile 混合器；
6. 映射 lv006 IV/Probe；
7. 确认角色 Bloom 隔离和合成顺序；
8. 固定终末地参考帧与 HDR 中间缓冲；
9. 为全部证据记录 SHA-256。

退出条件：Shader、曲线、环境混合三项全部恢复，并能用独立小样解释参考帧。

## Phase 1：OpenSpec 与渲染基建

- 创建跨 Shader、AOT、HotFix、场景、Timeline、质量档的 OpenSpec change；
- 明确 source of truth、生成链、迁移和回滚；
- 统一 `CharacterRender*`；
- 建立 Native Light + Additional Data authoring；
- 建立环境 Profile 和区域混合器；
- 建立逐层 Debug View。

## Phase 2：终末地源金丝雀

在隔离 LookDev 场景先复刻终末地源 Rig，不直接猜 ProjectACG 角色映射。

建议覆盖三类样本：

- Perlica：完整稳态灯组；
- Laevat：高强度 Rim；
- Lifeng：动态 SDF 和 Transform。

退出条件：逐灯开关、HDR 中间缓冲、最终合成和参考帧均可解释。

## Phase 3：角色资料页

- 导入 23 套终末地源 Rig 模板；
- ProjectACG 角色显式绑定来源 Rig；
- 实现 Overview/Skill/Equip；
- 实现 1 秒交叉淡变；
- LightFollower 跟随；
- 动作、相机、灯、Volume 共用 presentation version；
- 验证连续切页、换角色、关闭窗口和卸载场景。

`Advance` 页签没有终末地对应证据，必须单独确认，不能擅自 alias。

## Phase 4：抽卡

- 强制 Overview；
- 应用 `volume_overview`；
- 接入 gacha/character 独立 IV；
- 固定抽卡相机和 DOF；
- 验证结束后的灯、Volume、IV 回收。

## Phase 5：大招

- 导入 24 个 60fps Clip；
- 绑定 Active/Intensity/Color/Transform；
- 导入 Additional Data 动画；
- 导入 18 个 Cubemap/Volume；
- 导入 13 个 Bloom override；
- 验证结束、跳过、打断、死亡、重播和场景卸载。

## Phase 6：普通场景

- 先复刻 `lv006 art_01`；
- 再实现 mining、cave、boss、erosion、ruins、tunnel；
- 接入 Point/Spot、IV、Probe、Fog、Light Shaft、Exposure；
- 最后实现能源球 `80×80×80`、`6s/2s`。

## Phase 7：质量档与性能

- 建立 Full Fidelity 验收档；
- Full Fidelity 不删灯、不替换数学；
- Low/Mid/High/Ultra 的降级需来自终末地平台证据或明确标记为 ProjectACG 自有策略；
- Low 当前无 HDR，不能冒充完整效果；
- PC、Android、iOS 分别测量。

---

## 8. 验证与验收

## 8.1 自动数据门禁

- 23×3 灯组全部存在；
- 灯数严格为 `206 / 197 / 151`；
- 554 个 Light 与 Additional Data 一一对应；
- 无丢失、歧义和跨组重复；
- 531 character-only；
- 23 个 Skill FloorLight；
- 全量 Metal-only、Roughness、Rim、Volumetric、reflection-light-mesh 与证据一致；
- 页签 Crossfade=`1s`；
- Gacha 只激活 Overview；
- Ultimate Clip=`60fps`；
- Character Volume 公共值=`1 / 0.6 / 0.15`。

## 8.2 固定图像条件

每个用例必须固定：

- 模型与材质版本；
- Pose 和动画帧；
- 相机 Transform、FOV、宽高比；
- RenderTexture 尺寸；
- Exposure/Tonemapping；
- Cubemap、IV 和 Reflection Probe；
- 质量档和平台。

逐层输出：

1. Albedo/Normal/Roughness/Metallic；
2. Main Diffuse/Specular；
3. Additional Diffuse；
4. Metal-only Specular；
5. NPR Ramp/SDF；
6. Rim；
7. IV/SH；
8. Cubemap；
9. Character Bloom；
10. Fog/Light Shaft；
11. 最终合成。

工具：

- RenderDoc；
- Unity Frame Debugger；
- HDR 中间缓冲读取；
- Overlay/Difference；
- SSIM、Delta E、亮度直方图和轮廓差异。

不能先凭经验写死 SSIM/Delta E 门槛。门槛应由相同参考场景的重复抓帧噪声生成。

## 8.3 性能验收

- CPU 灯组切换；
- GPU Additional Light；
- Volumetric Fog；
- Character Bloom；
- Shader variant；
- Draw Call；
- 显存；
- PC/Android/iOS GPU 时间。

静态检查、C# 编译和 Shader 编译均不能替代 Unity 内视觉验收。

---

## 9. 不采用的方案

- 不直接把 `40000 lux`、`77.9`、`57.1`、`206.19` 填入 URP 再凭肉眼调曝光；
- 不用一盏 Directional 模拟全部角色灯；
- 不用全局 Bloom/Exposure 代替角色 Bloom；
- 不用 Fresnel 假光代替需要产生真实受光的 Point/Spot；
- 不在每个角色材质中重复手填逐灯参数；
- 不同时启用 554 盏灯；
- 不把角色页 `reflectionProbeLightMesh=1` 泛化到大招；
- 不把 inactive Profile 的储存值当作有效覆盖；
- 不在 Shader 数学、ACL 曲线和环境混合器恢复前宣称严格复刻完成；
- 不继续扩大 `CharacterRenderController` / `CharacterMainLightController` 双轨竞争。

---

## 10. 外部决策与剩余风险

1. 验收目标平台和分辨率必须确定；PC、Android、iOS 不能共用一个性能结论。
2. 终末地 Rig 到 ProjectACG 角色的映射必须显式指定；自动按名称、颜色、体型匹配都属于猜测。
3. ProjectACG `Advance` 页签没有终末地灯组来源，必须单独决定。
4. ProjectACG 模型、法线、材质贴图、Ramp/SDF 与终末地不同，结构和参数一致不等于像素自然一致。
5. ProjectACG 当前 URP Asset、`OutGameCharacterSystem.cs`、Battle Timeline 等路径存在用户本地改动；正式实施时必须 path-specific 集成，不能覆盖现有工作。
6. 当前尚未运行 Unity、RenderDoc 或真机验证；本包是静态证据和实施设计，不是效果验收结果。

---

## 11. 包内文件索引

### 11.1 终末地证据

目标目录：`evidence/endfield/`

至少包含：

- `终末地_CB3_光照与角色材质强化分析.md`
- `lv006-envparse-table.tsv`
- `lv006-envparse-summary-all.json`
- `lv006-envparse-dump-all.txt`
- `evidence/charinfo/charinfo-native-groups.csv`
- `evidence/charinfo/charinfo-native-lights.csv`
- `evidence/charinfo/charinfo-hgadditional-summary.txt`
- `evidence/ultimate/ult-animation-bindings.txt`
- `evidence/ultimate/ult-binding-paths.txt`
- `evidence/ultimate/ult-light-npr.txt`
- `evidence/ultimate/ult-hgcharacter-volumes.txt`
- `evidence/ultimate/rimlight-materials.txt`
- `evidence/scene/map01-lv001-lightingdeco.txt`
- `evidence/scene/map01-lv001-reflectionprobe.txt`

以 `MANIFEST.sha256` 为实际收录清单和校验依据。

### 11.2 ProjectACG 现状快照

目标目录：`evidence/projectacg-current/`

建议收录：

- `Client/Packages/manifest.json`
- `Client/ProjectSettings/GraphicsSettings.asset`
- `Client/ProjectSettings/QualitySettings.asset`
- `Client/Assets/Resources/QualitySettings.asset`
- `Client/Assets/AssetRaw/Settings/URP-*.asset`
- `Client/Assets/AssetRaw/Settings/URP-*-Renderer.asset`
- `Client/Assets/AssetRaw/Scene/map_Character_Chouka/Map_ACG_Gacha_Detail.unity`
- `Client/Assets/GameScripts/AOT/GameArt/CharacterRender/CharacterRenderLightProfile.cs`
- `Client/Assets/GameScripts/AOT/GameArt/CharacterRender/CharacterRenderController.cs`
- `Client/Assets/GameScripts/AOT/GameArt/CharacterMainlight/CharacterMainLightProfile.cs`
- `Client/Assets/GameScripts/AOT/GameArt/CharacterMainlight/CharacterMainLightController.cs`
- `Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderTimelineClip.cs`
- `Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterRenderTimelineTrack.cs`
- `Client/Assets/GameScripts/AOT/GameArt/TimelineTrack/CharacterMaterialTimelineClip.cs`
- `Client/Assets/GameScripts/HotFix/GameLogic/Module/OutGameModule/OutGameCharacterSystem.cs`
- `Client/Assets/Shader/Character/Chara_V2/Common/Chara_AdditionalLights_V2.hlsl`
- `docs/ai-coding/rules/quality-module.md`
- `docs/ai-coding/verification-matrix.md`

- `Client/Assets/GameScripts/AOT/GameArt/CharacterMainlight/*.asset` 与 `CharacterMainLightCameraOverride.cs`
- `Client/Assets/GameScripts/Editor/CharacterRender/*.cs`、`Editor/CharacterMainlightEditer/CharacterMainLightControllerEditor.cs`
- `Client/Assets/Shader/Character/Chara_V2/**/*.hlsl`、`*.shader`、`*.md`
- `Client/Assets/TA_Test/EndField/Settings/URP-*.asset`
- `Client/Assets/TA_Test/map_Character_Display/Settings/URP-*.asset`
- `Client/Assets/TA_Test/map_Character_Display/LightTimeline.playable`
- `evidence/projectacg-current/character-hero-material-inventory.tsv`（101 个 hero 材质的路径与 Shader 行快照）

所有文件只作为生成本方案时的只读快照。实现时仍以 ProjectACG 工作区当前文件为准。

---

## 12. 交付状态

- 终末地证据：已完成资源、组件和运行时绑定级分析；
- ProjectACG 映射：已完成静态架构与主要差距确认；
- 运行时代码/Shader/场景修改：未开始；
- Unity 编译与运行：未执行；
- 真机与视觉验收：未执行；
- 严格复刻状态：等待 Phase 0 三项强制证据补全。
