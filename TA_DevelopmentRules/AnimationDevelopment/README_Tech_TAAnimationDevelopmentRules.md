---
name: ta-development-rules-animation
description: TA 通用开发规则中的动画解包、压缩动画解码、Avatar 绑定与 Unity Generic/Humanoid 资产生成模块。
---

# TA 通用开发规则｜动画解包与 Unity 导入模块（CORE + REFERENCE + PROFILE）

## 概览

> 文档模式：`ta-development-rules/animation/v1`
> 语言与编码：中文，UTF-8
> 总入口：[TA 规则总入口](../README_Tech_TADevelopmentRules.md)
> 当前案例 Profile：[Endfield 角色页动画重建](Profiles/Endfield/role-page-animation-reconstruction.md)

本模块适用于经授权的本地客户端动画资产分析、压缩动画解码、骨骼绑定恢复、Unity `AnimationClip`/`AnimatorController` 生成和 Humanoid/Generic 兼容性验证。模块关注“证据可追溯、生成可重复、失败可定位”，不把预渲染视频、UI 动画或截图伪装成骨骼动画。

内容按三层维护：跨项目稳定的职责边界进入 CORE；可迁移的实现技巧、排错步骤和模板进入 REFERENCE；具体游戏、Unity 版本、目录、对象名、工具路径和当前验证结果进入 PROFILE。

## 工作流程

1. **冻结输入**：保存原始 ZIP、Bundle、RenderDoc、Unity 版本和工具版本；原始文件只读。
2. **完整性校验**：执行交付包提供的校验脚本并保存完整输出，不以目录能打开代替校验通过。
3. **建立索引**：先统计 `AnimationClip`、`Animator`、`Avatar`、`Mesh` 和依赖 Bundle，再选择需要导出的 Bundle 集合。
4. **解码证据**：解析普通曲线或压缩 ACL 的头、版本、采样率、轨道、绑定和根运动，输出机器可读 JSON。
5. **恢复绑定**：使用 Avatar TOS、模型层级或绑定哈希建立 Transform path；记录完整、部分和无法解析的轨道。
6. **生成 Unity 资产**：先生成 Generic，再根据 HumanTrait/Animator 绑定生成 Humanoid；创建控制器和证据目录。
7. **校准坐标与采样**：确认单位、坐标轴、根运动、旋转连续性、循环标志和曲线采样策略。
8. **Unity 验证**：在目标 Unity 版本重新导入、编译、加载 Avatar、播放代表性动作，并检查 Console、引用和曲线数量。
9. **交付复盘**：输出统计、失败项、已知限制、复现命令、资产路径、证据哈希和回退方案。

## 通用规则（CORE）

### ANM-CORE-01｜先证明资源类型，再决定导出格式

适用于所有客户端角色动画提取任务。

- **必须（MUST）**：确认对象是骨骼 `AnimationClip`、Animator 状态引用或预渲染资源中的哪一种。
- **应当（SHOULD）**：优先从 `AnimationClip`/`Animator`/`Avatar` 引用图定位，随后再导出数据。
- **禁止（MUST NOT）**：把 `.usm`、Canvas/UI 属性动画、表情贴图序列或截图直接命名为可蒙皮骨骼 `.anim`。
- **验证**：对象存在 Transform/Float/Root 轨道，或能在 Animator/Avatar 关系中证明骨骼消费方。
- **例外与回退**：若只有视频，交付视频证据和“无法恢复骨骼曲线”的结论，不伪造动画。

### ANM-CORE-02｜原始证据、工作副本和 Unity 资产分层

- **必须（MUST）**：原始包、解码输出、临时 staging、工程资产和报告分目录保存。
- **应当（SHOULD）**：导出器先写工作副本，抽样验证通过后再同步到 Unity `Assets`。
- **禁止（MUST NOT）**：直接在原始 Bundle 上覆盖、将整个解包目录塞进工程，或用 Unity `.meta` 污染原始证据。
- **验证**：每个生成资产都能回指 source Bundle、clip 名称、工具版本和证据目录。
- **例外与回退**：临时小样本可在隔离目录直接生成，但仍须保存命令和哈希。

### ANM-CORE-03｜压缩动画必须先解码再生成 Unity 曲线

- **必须（MUST）**：记录压缩格式版本、采样率、Transform/Root/Float buffer、轨道数和绑定哈希。
- **应当（SHOULD）**：使用与目标压缩格式匹配的官方/验证过的解码器，并输出逐帧 JSON 作为中间证据。
- **禁止（MUST NOT）**：仅读取普通曲线列表为空就判定“无动画”，或用错误的普通曲线导入器替代 ACL 解码。
- **验证**：解码输出帧数、轨道数、采样率与 metadata 一致；根运动和旋转连续性通过抽样检查。
- **例外与回退**：无法解码的 clip 保留原始 buffer、错误日志和不可用原因，不生成空壳 `.anim`。

### ANM-CORE-04｜Generic 与 Humanoid 是两条不同的绑定链

- **必须（MUST）**：Generic 使用 Transform path；Humanoid 使用有效 Avatar、Animator bindings 和 HumanTrait muscle 通道。
- **应当（SHOULD）**：同一源 clip 分别生成 Generic/Humanoid，并在目标模型上分别播放验证。
- **禁止（MUST NOT）**：把独立 `.anim` 的 Inspector 当作 FBX Rig 设置面板，或声称 `.anim` 自带完整 Avatar。
- **验证**：Generic 曲线 path 能在目标层级找到；Humanoid Avatar 为 Valid、骨骼映射完整且动作可重定向。
- **例外与回退**：没有对应模型 FBX 时可交付 Humanoid 曲线和 Avatar 证据，但必须标记为 EvidenceOnly。

### ANM-CORE-05｜单位、坐标系和根运动必须显式记录

- **必须（MUST）**：记录源单位、Unity 单位、平移缩放因子、轴向、根节点和 Root Motion 策略。
- **应当（SHOULD）**：转换前后用同一帧的根节点位置、骨盆位置和脚底高度做数值对照。
- **禁止（MUST NOT）**：在不说明上下文的情况下同时应用厘米到米和 PActor 全局缩放，或重复缩放已修复的曲线。
- **验证**：角色身高、骨盆位移、脚底接地和镜头尺度与参考模型一致。
- **例外与回退**：不同 FBX 导入缩放需要不同倍率时，将倍率放入明确命名的变体或导入配置。

### ANM-CORE-06｜批量生成必须幂等、可观察、可回退

- **必须（MUST）**：记录成功、失败、跳过、轨道解析数和输出路径；默认不覆盖已有资产。
- **应当（SHOULD）**：使用 `StartAssetEditing`/`StopAssetEditing` 的 `try/finally`，避免循环内反复 `SaveAssets`/`Refresh`。
- **禁止（MUST NOT）**：静默吞异常、把部分成功报告为全成功，或在没有覆盖开关时删除既有资产。
- **验证**：重复运行结果稳定；失败项可以按 clip 单独重跑；Unity 工程无孤儿引用。
- **例外与回退**：显式 `force/overwrite` 可重建，但必须在日志和文档中标记破坏性范围。

### ANM-CORE-07｜规则结论必须带证据等级和限制

- **必须（MUST）**：区分 `[EVIDENCE]`、`[INFERENCE]`、`[UNVERIFIED]`、`[APPROXIMATION]`。
- **应当（SHOULD）**：形成 Evidence → Finding → Path 链，第三方能从报告回到文件、事件或命令。
- **禁止（MUST NOT）**：把一次视觉相似、单个 Bundle 或未验证 Avatar 写成完整还原。
- **验证**：报告包含输入、工具、输出、验证方法、失败项和回退。
- **例外与回退**：证据不足时保留候选假设，待新 Bundle、RenderDoc 或模型证据补齐后升级。

## 资源加载与规则维护

- 动画解包方法和排错：[动画解包与 Unity 导入参考](references/endfield-animation-extraction-and-unity-import.md)。
- 当前终末地案例：[Endfield 角色页动画重建 Profile](Profiles/Endfield/role-page-animation-reconstruction.md)。
- 资产旁的使用说明仍以工程 `Characters/README.md` 和导入器代码为准；本模块只维护可迁移规则与案例沉淀。
- 若增加新的压缩格式、骨架映射或导出器，先更新 Profile 和证据格式，再更新本模块的 REFERENCE；只有跨项目复现后才能升级 CORE。
