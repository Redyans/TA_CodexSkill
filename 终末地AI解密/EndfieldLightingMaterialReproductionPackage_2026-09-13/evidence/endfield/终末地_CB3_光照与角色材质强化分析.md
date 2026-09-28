# 《明日方舟：终末地》CB3 光照与角色材质强化分析

日期：2026-09-13

## 结论

终末地并不是“一个太阳平行光 + PBR 材质”的照明方案。当前资源和运行时代码证明它至少叠加了五层：

1. 自定义环境主直射光、Sky、Fog、Exposure、Shadow；
2. 场景 Point/Spot Light、Irradiance Volume、Reflection Probe/Cubemap；
3. 角色页按镜头和页签切换的 Point/Spot 灯组；
4. 大招 Timeline 动态控制的角色专用灯、角色 Volume 与 Bloom；
5. Additive/Fresnel/Dissolve 发光材质形成的“假光”。

强化角色材质感的核心不是单纯把主光加亮，而是：**角色专用灯隔离背景、金属专用镜面分支、粗糙度筛选、NPR Ramp/SDF 分界、可调 Rim 宽度、Cubemap/IV 间接光、环境阴影倍率，以及逐镜头动画化灯光**。

## 1. 普通场景

### map01/lv006 环境 Profile

9 个 `lv006` Profile 已全部定向反序列化。它们是自定义环境 MonoBehaviour，内嵌 `HGLightConfig`、`HGSkyConfig`、Fog、Exposure、Shadow 等配置；不是标准 Unity `VolumeProfile`。

- 基础环境 `art_01`：主直射光启用，`40000 lux / 4800K`，Direct Specular=1，间接漫反射/镜面=1/1；Sky 启用，Baked Indirect=2，并绑定 `T_reflectionprobe_envdefault2`；普通雾、高度雾、体积雾、Light Shaft、Color Grading、Exposure、Shadow 均启用。体积雾距离 90m，直接光散射=1，天空散射=0；Contrast=+10，Contact Shadow=0.5。
- 矿区/洞穴：本 Profile 的 Light/Sky 为 inactive，只覆盖 Fog/Height Fog/Volumetric Fog。不能把 inactive 字段中的 25000 lux 当成实际覆盖值。
- Boss 区：主直射光 `60000 lux / 5500K`，但 Direct Specular=0；Sky Direct=10、Baked Indirect=2，体积雾 200m，曝光直方图范围 -3~-2。
- Boss 隧道：自己的 Light/Sky/普通 Fog inactive，只覆盖 Height/Volumetric Fog 和 Exposure；体积雾带明显自发光色。
- 侵蚀区与废墟分别提高体积雾直接/天空散射，用空气透视和轮廓分离强化画面层次。

完整参数见 `lv006-envparse-table.tsv` 和 `lv006-envparse-summary-all.json`。

### 非平行光与间接光

- `lv006_spotlight_postmodel.prefab` 名称虽含 spotlight，实际组件是 Point Light：强度 77.9、Range 10、3000K、Bounce 2；不是 Sun Source，且带 Metal-only Spec、Rim 与 Volumetric Scattering 参数。
- 对 `map01/lv001` 的两个 lighting-deco marker 定向核验到 6 个原生附加光：4 Point + 2 Spot，强度 4–15，Range 5–43；均为非烘焙、非 character-only，并带 Reflection-Probe light mesh 与体积散射配置。
- 已抽到普通场景烘焙探针 `ReflectionProbe-1443706896`：128×128、6 faces、8 mip。
- `map01` 资源池另有 Irradiance Volume 数据和大量 Reflection Probe EXR；当前尚未把这些探针逐个映射到 `lv006` 坐标。
- 关卡数据还包含 `Env_EnergyPoint_Dark.asset` 的局部环境球：Scale 80×80×80，进入/离开按 6s/2s 淡变。这说明环境光本身也会按区域动态切换。

## 2. 角色资料页与抽卡

此前的 bundle 聚合灯数不能视为同时启用数。按控制器 `m_LightList` 精确解析 23 个标准角色灯光 prefab 后：

| 页面灯组 | 原生灯总数 | 每角色 | 类型 |
|---|---:|---:|---|
| overview | 206 | 7–13 | 132 Point + 74 Spot |
| skill | 197 | 6–12 | 149 Point + 48 Spot |
| equip | 151 | 4–9 | 110 Point + 41 Spot |

三组共 554 个原生 Light，全部 PPtr 一一解析成功，无 Directional/Area，`m_SpecularIntensity=1`。Lua 运行时会关闭其他角色灯组，再按页面把旧/新灯组做 1 秒交叉淡变；稳态并不是 554 盏同时启用。灯组通过 `InitLightFollower` 跟随角色模型。

554/554 个灯都唯一关联一个 `HGAdditionalLightData`：

- `m_lightNPRSpecMetalOnly=1`：554/554；
- `m_lightNPRSpecMaxRoughness=0.6`：545/554；
- `m_lightNPRSpecRoughnessBias=0.8/0.95`：549/554；
- `m_lightNPRRimWidth` 主峰为 1.0（235）、0.4（180）、0.3（59）；
- `m_LightCharacterOnly=1`：531/554；
- 23 个非 character-only 项正好是每个角色 skill 组的一盏 `FloorLight`，强度 1.4、Range 7.4；
- `m_volumetricScatteringIntensity=1` 与 `enableLightMeshForReflectionProbe=1`：均为 554/554。

这套组织说明：短 Range 的 Spec/Fill 灯负责在金属附件、衣料和脸部制造精确高光；Spot Rim 负责轮廓；唯一会照到场景的 FloorLight 负责脚底和地面托光。最后一句属于命名和参数组合的设计意图推断，尚未用运行时抓帧验证。

抽卡代码只启用 `light_overview`，同时应用 `volume_overview`，并创建独立的 `Data/IrradianceVolume/<platform>/gacha/character`。资料页还按页签用 `GetMainLightBiasTween` 修改角色 Volume 的主光偏置，并对角色列表相机启用 DOF。

`light_chr_0024_qianneng` 是“潜能场景”特例，不是标准角色三页灯组：其中有 5 个烘焙 Reflection Probe。不能把这 5 个探针泛化为每个角色页面的固定配置。

## 3. 大招/技能演出

24 个大招 AnimationClip 均为 60fps，时长 1.3–2.833s；共 459 个绑定，其中：

- `GameObject.m_IsActive`：211；
- `Light.m_Intensity`：65；
- `Light.m_Color` 通道：30；
- 其他 Light 属性：6。

代表样本：

- **Laevat，2.017s**：2 个 Point Fill 动画强度，4 个 Spot Rim 动画启停；Rim 初始强度最高 100。六灯均 character-only、metal-only、粗糙度上限 0.6/偏置 0.8，体积散射=1。
- **Lifeng，1.933s**：Face、VFX、口部 Point/Spot 都有实际绑定；部分灯静态强度为 0，但 Clip 动画化其 Intensity。`Point Light_vfx` 还同时动画 Transform、Active 与 `m_lightNPRRampSDFBias`。这些灯不使用自身的体积散射。
- **Perlica，1.65s**：6 盏原生灯均有 Active 曲线，并动态控制强度、颜色和 NPR SDF；背光 Spot 初始强度 206.19，三盏背光/Rim 的 Rim Width=1。

三个样本的已解析灯均 character-only、metal-only；但它们的 `enableLightMeshForReflectionProbe=0`，不能把角色资料页的 Reflection-Probe light mesh 结论泛化到大招。

18 个大招 `HGCharacterVolume` 样本中：18/18 覆盖非空 `charMaxCubemap`，并统一覆盖 Ambient Base=1、Directional Ambient=0.6、Directional Param=0.15；14/18 开启主光控制并覆盖环境阴影倍率。眼睛基础光/高光/散射、Auto Rim、Face Rim、Outline 均没有 override，说明这些样本主要通过灯、Cubemap、环境阴影和曝光塑形，而不是简单打开眼睛/描边开关。

18 个 Bloom 中，13 个 override `characterBloomControl`：9 个为 1，4 个为 0。因此“部分大招使用独立角色 Bloom”成立，“所有大招都启用角色 Bloom”不成立。

## 4. 材质假光

Fire、Lightning、Natural 三套 Rimlight，以及 Laevat 剑/衣物光和 Reaper 手光，均存在无原生 Light 的发光材质方案。三套通用 Rimlight 材质都配置：

- `_UseLighting=0`；
- `_UseForceDynamicLighting=0`；
- `_EnvLightStrength=0`；
- Transparent + Fresnel，并通过曝光强度、HDR 颜色、Opacity/Dissolve 曲线形成亮边。

因此屏幕上“发亮”不一定会照亮场景。终末地会把真实 Point/Spot 与材质假光分开使用：需要角色和环境产生受光响应时用真实灯，只需要能量轮廓和高亮图形时用 Emissive/Fresnel/Dissolve。

## 5. 证据边界

- `m_active=0` 只证明该 Profile 的这一段配置不生效；“继承主环境”符合分层结构，但尚未恢复运行时混合器代码。
- `m_lightNPRType`、`autoExposureMode`、`skyMaterialType` 等数值枚举名尚未恢复。
- Prefab 的 `m_Enabled=1` 不等于证明运行时某一帧激活；角色页有 Lua 切换链、大招有 Clip 绑定链，所以这两部分证据更强。
- ACL 压缩关键帧值尚未解码，当前能确认绑定属性和总时长，不能给出逐帧强度/颜色数值。
- 反射 EXR 的地图级库存不等于同屏同时使用；也尚未完成全部空间坐标映射。
- 以上结论是资源、组件和运行时绑定分析，不是最终帧像素复刻。最终效果还受 shader 实现、质量档、曝光和运行时缩放影响。

## 6. 验证与证据

- `dotnet build EndfieldInspector.csproj -c Release --no-restore`：0 error；仅有无法访问 NuGet 漏洞索引的 `NU1900` 警告。
- 所有本轮 Python 工具执行 `py_compile`：通过。
- 角色页 23×3 分组全部解析成功，554 个 Light 无缺失、无歧义、无跨组重复。
- 未修改 `F:\` 游戏目录；所有新增解析物均保存在当前工作区。

关键文件：

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
