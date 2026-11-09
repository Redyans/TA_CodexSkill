# AlphaAce 运行换枪：非可读网格回归记录

> 类型：PROFILE；日期：2026-11-07；对应用户运行时切换 Famas、闪光弹和多款 Famas 皮肤的报错。
> 工程与证据别名见 [完整案例](project-and-weapon-showcase-recovery.md)。本页单独记录新修复，避免历史三样本报告被误用作全目录验证。

## 1. 事实与可复现问题

用户提供的 Console 显示：

```text
Not allowed to access vertices on mesh 'Famas_1P_Replaced' (isReadable is false)
Not allowed to access triangles/indices on mesh 'Grenade_Flash_1P_Replaced'
InvalidDataException: Cannot calculate skinned bounds
AlphaAceShowcaseWeaponUtility.PrepareWeapon
```

触发入口是 `WeaponShowcase.unity` 的 PlayMode 目录切换。旧目录检查只是确认配置行和 Prefab 存在；旧截图只覆盖 AK47、M4RAS 及一项程序皮肤。该验证没有覆盖运行时 CPU 网格读取权限。

## 2. 根因与修正

| 根因/后续发现 | 实际修正 | 保留边界 |
| --- | --- | --- |
| 不可读源 Mesh 直接访问 vertices/triangles | 可读性分支；不可读蒙皮由引擎 BakeMesh 到临时可读网格 | 不批量改原资产 Read/Write |
| Unity 2019 BakeMesh 已带世界缩放 | 先补偿 lossyScale，再映射世界空间 | 合成测试覆盖单位及两种非单位均匀缩放 |
| 包围盒仅支持 SkinnedMeshRenderer | 纳入静态 MeshRenderer；不可读静态 mesh 用八角点 | 保守估计不等于精确三角形边界 |
| 按 item 派生 ID 找不到配置 | 先按原逻辑回退配置表 WeaponType | 不能仅凭 ID 前缀推所有条目的检视类型 |
| 配置中确实缺少原型姿态 | 通用角度/单位缩放/自动构图，界面提示 | 是离线预览近似，不是补造原配置 |
| 拳头等 Prefab 没有独立网格 | 界面显示没有独立展示模型 | 没有生成假模型，也不代表原游戏拳头系统已恢复 |
| 停用的粒子组材质为空 | 只检查启用且子层级有效的 Renderer | 实际启用的 Renderer 缺材质仍明确失败 |
| 粒子 sharedMaterials 还包含未启用的拖尾空槽 | 分别检查主材质与 trails.enabled 时的 trailMaterial | 不替换烟雾 Shader，也不为了通过检查停用效果 |
| AUG_Goth 程序皮肤原贴图缺失 | 生成前按原 PaintKeyValues/Body/Scope 路径检查；返回 Unavailable 并保留 Prefab 原材质 | 不伪造贴图、不把降级状态记为皮肤成功 |
| 加载中途异常后未记录到当前对象 | 失败路径释放皮肤、停用并销毁临时实例 | 不关闭整个异常报告系统 |

Steampunk 中 `group` 对象的组件是 `ParticleSystemRenderer`，原 Prefab 的 `m_Enabled: 0`、材质 fileID 为 0。它是合法的停用辅助组；不能因为该槽为空就判定主体武器 Shader 丢失。

随后 `A01_smoke_001` 的错误也不是 Shader 源文件缺失：`FX_GL_LitSmoke_11025.mat` 与 `VFXUber.shader` 都可加载，重导入前后保持有效。诊断显示同一粒子 Renderer 的主材质有效、第二槽为 null；原 ParticleSystem 的 TrailModule.enabled=0。实际根因是把可选拖尾空槽当成必须绑定的材质。证据见 `MESH_FIX/effect-reference-reimport.txt`，保留了 GUID/fileID、重导入前后以及 Prefab 各材质槽。

## 3. 实现位置与所有权

修改文件均相对 `PROJECT`：

- `Assets/Scripts/Recovery/AlphaAceShowcaseWeaponUtility.cs`：可读/不可读蒙皮分支、静态网格、显示几何判断、通用预览。
- `Assets/Scripts/Recovery/AlphaAceWeaponShowcase.cs`：原配置回退、提示、有效 Renderer 检查、加载失败清理。
- `Assets/Editor/AlphaAceShowcaseBuilder.cs`：复用公共顶点计算入口。
- `Assets/Editor/AlphaAceShowcaseMeshValidation.cs`：新增显式验证入口，记录几何覆盖、相机帧、程序皮肤和切换状态。
- `Assets/Scripts/Recovery/AlphaAceOfflineSkin.cs`：在提交异步任务前检查本地材质、Shader、Body/Scope 纹理；缺失则释放本次状态并提示。

源 Mesh、Prefab、原始检视配置未批量改写。临时网格有 finally 清理；独立 Worker 与真实项目日志写 `MESH_FIX`，不覆盖旧展示截图。

## 4. 验证设计

### 4.1 合成用例

构造一个可见三角形和远处退化三角形，使用真实骨骼、bindpose、旋转和位移；记录可读源的世界顶点，再 `UploadMeshData(true)`，验证不可读路径。

测试矩阵是 scale `1 / 0.6 / 2` × 对象激活/非激活。修正前 scale 0.6 最大误差约 0.289；修正后六组最大误差均小于 `1e-6`，退化远点不参与边界。证据为 `mesh-worker/result-before-scale-fix.txt`、`mesh-worker/result.txt` 与对应 Unity 日志。

这能验证此版本的缩放与非激活行为，不覆盖所有非均匀缩放、镜像、父层剪切或特殊 GPU 变形。

### 4.2 真实目录几何

在 PlayMode 读取恢复场景的 catalog，以 `prefabPath + weaponType` 分组，对每个组合实际实例化、采样姿态并计算有限且非零的边界。记录不可读 skinned renderer 数、原配置回退、通用姿态、没有独立模型及失败数。

这比只检查资源路径更接近真实问题，但仍不等于每个条目的 Shader、皮肤、特效和最终图均已验收。

### 4.3 实际换枪与相机输出

使用场景控制器的真实 `SelectWeapon`，每项等待至少 12 个目标相机帧；有程序皮肤时等待 Succeed，失败/超时则结束。保存 1280×720 输出和可见网格 viewport 范围。

最终选择序列覆盖：

| item | 用途 |
| --- | --- |
| 111010001 | Famas 基础款、最后返回重复 |
| 111010002 | 原配置指向闪光弹，验证用户截图中的不可读资源 |
| 111010003 | Famas Snake 程序皮肤 |
| 111010006 | Famas Steampunk，停用粒子组空材质分支 |
| 111010008 | Famas Blending 程序皮肤 |
| 111050001 | 原 AK47 基础款回归 |
| 111030005 | 原 M4RAS 程序皮肤回归 |
| 111100001 | M4L21 缺专用配置的通用预览 |
| 111130001 | 配置表 WeaponType 回退到 11106；AUG_Goth 缺纹理时明确显示 Unavailable 的降级分支 |
| 132010001 | 无独立网格条目的非异常提示分支 |

最后一次返回 Famas，使测试包含 11 次选择、最多 10 张模型输出。验证期间临时隐藏控制 UI、绑定输出 RT；结束恢复原选择、交互角度、相机 target/aspect 和控制 UI，后台宿主自行退出。

## 5. 阶段证据与结论边界

最终运行是 `MESH_FIX/run-20261107-205922`，完整日志 `unity-mesh-regression-v9.log`，实际子进程退出码 0。结果如下：

| 项目 | 最终结果 |
| --- | --- |
| 目录几何 | 219 个唯一 Prefab/pose 组合通过，计入 221 个不可读 SkinnedMeshRenderer；geometry errors 0 |
| 姿态覆盖 | 28 个组合使用明确标注的通用预览；原配置 WeaponType 回退另有实际记录 |
| 空模型 | 3 个唯一组合没有独立显示网格；拳头条目实际切换提示通过 |
| 实际选择 | 11 次选择，10 张 PNG，每次采集至少 12 个目标相机帧 |
| 目标皮肤 | Snake、Steampunk、Blending、M4RAS_SeCai 均为 Succeed |
| 已知缺资源 | AUG_Goth 为预期 Unavailable，保留原 Prefab 材质，未恢复该皮肤 |
| 错误与退出 | 专项 runtime errors 0；Unity ExitCode 0；没有遗留验证 request 文件 |
| 重复选择 | 返回 Famas 后继续正常显示；两张 PNG 文件哈希不同，不声明逐像素一致 |

已查看 Steampunk 与 Blending 的最终输出，均显示完整枪体和对应皮肤。报告的 AK47 顶点 viewport 下界存在负值，因此本轮不将“无异常”扩展为所有顶点均在画面内；历史样本姿态、辅助网格和构图精确性仍需另验。

| 证据 | 结果与意义 |
| --- | --- |
| `mesh-worker/result-before-scale-fix.txt` | 复现双重缩放，不隐藏失败记录 |
| `mesh-worker/result.txt` | 六组合成用例成功 |
| `run-20261107-201514/result.txt` | 真实非可读网格已通过，随后发现缺少检视配置 |
| `run-20261107-202045/result.txt` | 缺配置处理后继续推进，发现无独立网格条目 |
| `run-20261107-202510/result.txt` | 219 个几何组合成功；实际 Famas/闪光弹/Snake 成功，Steampunk 被停用粒子组空材质误拦截 |
| `run-20261107-203045/result.txt` | 几何通过；发现验证器在延迟换枪前误给下一项截图编号，本轮不用于完整换枪验收 |
| `unity-mesh-regression-v5.log` 及其 run 目录 | 增加选择中状态、实际 item 核对和皮肤输入断言，继续暴露真实材质槽问题 |
| `run-20261107-203524/result.txt`、`run-20261107-204404/result.txt` | 定位到启用粒子的空槽，单独重导入 Shader/材质未消除误判 |
| `run-20261107-204907/result.txt` | 增加实际槽位诊断，确认 ParticleSystemRenderer 的可选拖尾空槽；该轮仍失败 |
| `run-20261107-205341/result.txt` | 截图中的 5 项及 AK47/M4RAS 回归通过；扩大到 AUG_Goth 时发现原纹理缺失，整轮仍失败 |
| `unity-mesh-regression-v9.log` 及对应 run 目录 | 增加缺资源预检后的最终回归；AUG_Goth 只验降级提示，不验皮肤还原 |

当前文件快照和最新结果目录由 [证据清单](evidence-snapshot.json) 关联。报告中的 `Runtime errors` 只统计专项窗口内 Error/Exception/Assert；全工程既有 Wwise、菜单等 warning 不因此消失。

验证器本身也需要审查。实际相机回调可能在同一个 Editor 更新间隔触发多次；只递增测试索引、随后 delayCall 换枪，会把旧对象的输出标成新条目。现在等待选择完成后再累计帧，并确认实际 item、皮肤组件 ID 和 paintKit。早期错标截图不作为对应条目通过的证据。

AUG_Goth 缺失路径是 `Assets/Bundle/Materials/_hd/Weapons/1P/MainWeapon/AUG_Goth/AUG_Goth_C.tga`；工程和 art_assetBackup 的路径检查未找到对应文件，Scope_C 并非枪身颜色图的等价替代。最终测试只对该已知缺口接受明确 Unavailable，报告输出 `procedural skin NOT restored`；其他预期可用皮肤仍要求 Succeed。

## 6. 复现与回退

菜单入口为 `Tools/AlphaAce/Weapon Showcase/Validate runtime mesh switching`，在展示场景 PlayMode 显式执行。后台使用 `-executeMethod AlphaAceShowcaseMeshValidation.BatchValidate`，不加 `-quit` 或 `-nographics`；具体进程等待和日志语义见 [工具参考](../../references/unity-recovery-tooling-and-evidence.md)。

`MESH_FIX` 保存四份原源文件 `.cs.before` 以及本次改动清单和补丁。回退仅撤销已确认的本次代码差异，并保留后续用户改动。验证结束检查 `Library/AlphaAceShowcaseMeshValidation.request` 已消费，不遗留将来启动的自动执行请求。

## 7. 可迁移的经验

- EditMode、PlayMode 和 Player 的可访问资源能力分别验证。
- “读取失败 → 空边界 → 无对象”是一条因果链，优先修第一个错误。
- 用原始可读数据对照不可读分支；捕捉“能显示但大小错”的静默错误。
- 检查当前会参与绘制的 Renderer；停用的辅助组与主体缺材质分开处理。
- Renderer 类型决定材质槽语义；粒子拖尾未启用时的空槽不等于资源缺失。
- 主键与原配置回退优先于通用姿态，近似结果在界面和文档显式标注。
- 全目录几何覆盖、代表皮肤实际渲染、完整原游戏复刻分别给结论。

