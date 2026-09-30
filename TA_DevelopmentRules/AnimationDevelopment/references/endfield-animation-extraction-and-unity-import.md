# 动画解包与 Unity 导入参考

> 类型：REFERENCE；适用范围：Unity 客户端角色动画、压缩 ACL、Avatar/TOS 和 Generic/Humanoid 资产生成；使用前提：必须按目标游戏、Unity 版本、骨架和工具链重新验证。

## 1. 适用场景

这份参考用于把客户端里的角色动作恢复成可在 Unity 蒙皮模型上播放的资产。它覆盖：

- ZIP/客户端 Bundle 的隔离、校验和索引；
- Unity `AnimationClip`、`Animator`、`Avatar`、TOS 和绑定哈希的关系恢复；
- ACL 等压缩动画的解码；
- Generic `.anim`、Humanoid `.anim`、AnimatorController 和 Avatar 证据的生成；
- 失败、部分绑定、单位错误、曲线过大和重复导入问题的定位。

## 2. 前提与依赖

### 2.1 必备输入

至少需要以下之一：

1. 角色动画所在 Bundle 和依赖 Bundle；
2. 已导出的 `AnimationClip` metadata 和压缩 buffer；
3. 对应角色模型 FBX 或 Avatar/TOS；
4. 目标 Unity 工程及明确的 Unity/Package 版本。

只有角色页 `.usm` 视频时，只能交付视频或截图证据，不能直接生成骨骼 `.anim`。

### 2.2 工具分工

| 阶段 | 工具类型 | 产物 |
|---|---|---|
| 完整性 | PowerShell 校验脚本、SHA-256 | 校验日志、哈希清单 |
| Bundle 索引 | AnimeStudio/Unity 资产读取器、自写探针 | `assets.jsonl`、对象/依赖清单 |
| Bundle 导出 | manifest 选择器、批量导出脚本 | 动画/模型相关 Bundle 副本 |
| ACL 解码 | 与格式匹配的 native ACL exporter | `transform_frames.json`、`float_frames.json`、`root_motion_frames.json` |
| 绑定恢复 | Avatar TOS、骨架层级和哈希映射脚本 | `binding_map.json`、`transform_track_map.json` |
| Unity 生成 | Editor C#、`AnimationUtility`、`AnimatorController` API | `.anim`、`.controller`、Avatar manifest |
| 验证 | Unity 2022.3、Console、动画预览、运行时探针 | 编译日志、截图、报告 |

工具版本、命令行和输入目录必须写入报告；同名工具不同版本可能改变压缩解码或序列化结果。

## 3. 实现或排查步骤

### 3.1 隔离解包和完整性校验

推荐目录：

```text
D:\EndfieldHandoff\
├─ 01_原始ZIP\
├─ 02_完整解压\
├─ 03_证据索引\
├─ 04_工作副本\
└─ 05_导出\
```

执行交付包中的校验脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File ".\199 完整性校验\VERIFY ALL.PS1"
```

保存完整 stdout/stderr。不要只复制 `OVERALL VERIFY=PASS`；若 manifest 行、required 文件或 nested ZIP 失败，后续结论只能标为未验证。

### 3.2 建立 AnimationClip/Avatar/Bundle 索引

先从 manifest 或对象导出结果筛选：

```text
AnimationClip
AnimatorController
Animator
Avatar
Mesh/SkinnedMeshRenderer
```

对每个 clip 保存：名称、PathID、源 Bundle、依赖 Bundle、采样率、循环标志、普通曲线数量、压缩 buffer 状态和绑定数量。单独打开一个 Bundle 常会缺少 Avatar/TOS 或材质依赖，应按引用图一次加载相关 Bundle 集合。

### 3.3 解码普通曲线和 ACL 压缩曲线

普通曲线能直接通过 Unity/导出器读取时，仍需确认是否存在压缩 buffer。ACL clip 常见特征是普通曲线列表为空，但 `m_AclCompressedBuffer` 有 Transform/Root/Float 数据。

解码步骤：

1. 读取压缩头、版本、采样率、输出轨道数和根轨道索引；
2. 分别写出 Transform、Root Motion、Float buffer 的原始副本和 SHA-256；
3. 使用匹配的 native decoder 输出逐帧 JSON；
4. 检查帧数、时间步长、轨道数、根运动状态和 NaN/Inf；
5. 将解码结果与 clip metadata 交叉核对。

抽样脚本应至少统计“总轨道”和“有变化轨道”。常量轨道可以在 Unity 曲线阶段省略，但不能在解码证据阶段删除。

### 3.4 恢复绑定路径和 Avatar

Transform 绑定通常只保存路径哈希。用对应 Avatar 的 TOS 或模型骨架建立：

```text
PathHash → Transform path → Generic AnimationCurve binding
```

绑定报告至少包含：`PathHash`、`Path`、Position/Rotation/Scale 是否存在、解析状态和对应 Avatar 名称。

常见错误是按角色名字猜 Avatar。应按“动画绑定哈希覆盖率”验证；例如一个角色名可能实际使用 NPC/shared skeleton。覆盖率不足时，继续比对其他 Avatar TOS，而不是生成部分路径后声称完整。

### 3.5 生成 Generic `.anim`

Generic clip 需要为每条有效 Transform path 生成：

```text
Transform.m_LocalPosition.x/y/z
Transform.m_LocalRotation.x/y/z/w
Transform.m_LocalScale.x/y/z（仅非单位缩放时）
```

生成时必须：

- 保持四元数符号连续，避免相邻帧从 `q` 跳到 `-q`；
- 根据源采样率设置 `frameRate`；
- 根据源动作设置 `loopTime`、`stopTime` 和 `wrapMode`；
- 对每个 path 检查首尾/最大误差；
- 对常量位置、旋转、缩放轨道可省略曲线以减小资产体积，但要保留源证据。

### 3.6 生成 Humanoid `.anim`

Humanoid 版本不是把 Generic path 改名，而是根据 Unity Humanoid API 写入：

- Root/Feet/Hands 的 XForm 通道；
- `HumanTrait.MuscleName` 对应的 95 个肌肉通道（具体数量仍以目标 Unity 版本运行时为准）；
- 游戏自定义 vendor 通道保留在 JSON，不强行映射到不存在的 Unity 属性。

`.anim` 文件本身不携带完整 Avatar。正确链路是：

```text
角色 FBX → Model Import Settings → Valid Humanoid Avatar
Animator.avatar → FBX 生成的 Avatar
Animator.runtimeAnimatorController → Humanoid controller
```

### 3.7 单位、PActor 和采样策略

先测量同一帧根节点位移，再决定缩放：

- ACL/游戏骨骼数据若以厘米表达，写入 Unity Generic 曲线通常使用 `0.01` 转为米；
- 某些 PActor FBX 的导入全局缩放可能反向要求 `100` 的专用变体；这不是通用倍率，必须由 FBX Import Settings 和实测姿势决定；
- 已完成单位修复的 clip 不得再次缩放。

大动作可能有数百帧。可以生成最多约 120 个关键帧/曲线的工作资产，同时保留逐帧 JSON；若动作对接触、手指或高速旋转敏感，必须生成逐帧版本并做视觉 A/B。

### 3.8 生成 AnimatorController

每个角色至少生成：

```text
<角色>_Generic.controller
<角色>_Humanoid.controller
```

按动作语义排序并创建状态。`start → loop → end` 可使用 Exit Time；单动作 clip 直接创建一个状态。控制器生成器必须报告 clip 引用 GUID，避免同名资产错绑。

### 3.9 Unity 自检

执行以下检查：

```text
1. Unity 目标版本重新导入全部 .anim/.controller。
2. Console 无脚本编译错误、Missing Script、Missing Motion。
3. Generic 模型层级能找到所有非空 Transform path。
4. Humanoid Avatar 为 Valid，Animator.avatar 非空。
5. Clip length、frameRate、loopTime、曲线数量非零。
6. 播放 start/loop/end，检查根运动、脚底、手腕和四元数翻转。
7. 重复运行生成器，默认不覆盖且统计稳定。
```

## 4. 风险与不适用边界

| 现象 | 根因候选 | 处理 |
|---|---|---|
| 只有 `.usm` | 预渲染视频，不是骨骼 Clip | 交付视频证据，不伪造 `.anim` |
| 普通曲线为 0 | 动画在 ACL buffer | 使用 ACL decoder |
| Transform 路径部分缺失 | Avatar 选错或共享 NPC skeleton | 比较 TOS/hash 覆盖率 |
| 角色整体放大/缩小 | cm/m 或 FBX globalScale 不一致 | 单帧位移对照，幂等缩放 |
| Humanoid 不动 | Avatar 无效、肌肉绑定不匹配或控制器未挂载 | 先验证 Avatar，再看曲线 |
| 动作抖动/突然翻转 | 四元数符号不连续 | 相邻帧点积小于 0 时取反 |
| Unity 导入很慢/内存高 | 每条曲线逐帧序列化、循环内 Refresh | 批量编辑、曲线抽样、最后统一刷新 |
| 重跑后资产翻倍 | 输出路径或命名不幂等 | 固定命名、默认不覆盖、GUID/报告检查 |

## 5. 验证与回退

### 5.1 证据链模板

```text
Evidence: source Bundle/clip metadata/ACL buffer/TOS/Unity log
Finding: tracks and bindings decoded; unit and Avatar mapping selected
Path: evidence folder → importer → .anim/controller → Animator/FBX
```

### 5.2 回退策略

- 保留原始 Bundle 和 raw ACL，不在原地修复；
- 删除或移出生成目录即可回退 Unity 资产；
- 保留 `binding_map.json` 和 `transform_track_map.json`，可在不重复解包的情况下换 Avatar 映射；
- 若单位判断错误，使用专用 rescale 菜单/脚本，并通过阈值判断避免重复缩放；
- 若 Humanoid 验证失败，先交付 Generic 版本和 Humanoid EvidenceOnly manifest，不伪造 Avatar。
