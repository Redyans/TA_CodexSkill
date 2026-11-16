<!-- BEGIN automation:ta date:2026-11-12 layer:PROFILE -->
# ProjectACG 工具问题与 FlowMap Painter Profile｜2026-11-12

> 类型：PROFILE；项目：ProjectACG；以下是当日会话报告的项目现状，不是其他项目的默认实现。
> 关联参考：[Editor 资产生成与绘制工具](../../../references/CodexDaily/Development_Reference_2026-11-12.md)；沿用 `TOOL-CMP-01`、`TOOL-OPS-02`、`TOOL-VAL-02`。

## 1. 项目事实与现象

### CharacterPrefabBuilder 与外置 Mesh

- `100301` 的 QHigh FBX 中 Mesh 名改后，生成器已识别新名并创建新 `.mesh`，但当时 Editor 加载的 `Assembly-CSharp-Editor.dll` 早于源代码修改；单独 `dotnet build` 未证明工具已在当前 Editor 更新。
- 增量更新先快照保护旧节点，再删除改名/删减产生的过期 FBX 节点，旧实现因此把预期删除判为数据丢失并中止保存。修正为先登记授权删除的节点及子树，只豁免这些快照项，其他节点、组件与引用仍受保护。
- QHigh Prefab 随后已报告使用新命名、不再含旧节点。旧 `.mesh` 仍由 QLow Prefab、Battle Prefab 和场景引用；会话后续按用户要求将清理改为即使仍有引用也强制删除，并弹窗列出引用资源。受影响资源会变成 Missing，不会自动重绑到新 Mesh。
- 新增/扩展了 Mesh 改名、删除和引用场景回归用例。测试程序集编译与 `git diff --check` 报告通过；Unity EditMode 未运行。变更后的强制删除是否已在实际项目重建中执行，没有后续实证。

### FBX 名称与道具节点

- `Tachi_MG` FBX 内部 Mesh 名包含肉眼不可见的 `U+200B`，且其中一个名称的 `Tachi_MG` 与 `scabbard` 顺序不符合严格规则。普通角色 Mesh 校验路径没有使用已有的剔除辅助函数，故视觉上看似正确仍匹配失败。
- `100301` 悬浮道具 `ModelNodeReferences` 的 `Origin`、`Root` 绑定为空；目标节点已存在于嵌套 `Prop_AttachPoint` Prefab 和道具 Prefab。会话查到生成器的 `PreparePrefab/Populate` 与批量迁移入口可自动添加组件。Git 记录将组件引入归因于提交 `ee8418df`（作者 `frank`，2026-10-16），但无法据此确认具体是单项生成还是批量迁移。
- 会话报告称两个空引用由 `852ef61d`（作者 `bigkor`，2026-11-11）写入。空引用会触发运行时挂载校验；本日只核查，没有在 Unity 修复或复验。

### Timeline 保存工具

- `UltimateTimelineBindingsEditor` 保存快照时遇到绑定表残留的不存在 Track 引用，无法完成保存；至少发现两条失效记录。改为仅清理可确认已失效的绑定，保留有效绑定，并在结果弹窗按 FX 实例报告清理数量。
- Editor 和测试程序集编译通过，报告有 475 条 Editor 警告；Unity 内回归测试要求批处理模式，但工程已有 Unity 实例，未执行。实际重新保存并检查 Prefab 的结果待验证。

### FlowMap Painter

- 工具入口报告为 `TA_Tools → Scene → FlowMap绘制工具`。支持 Mesh 表面流向绘制、固定 UV 方向盖章、Shift 擦除、Undo/Redo、子网格选择、旋转流向手绘、实时动画预览和恢复画布；选择 Mesh 可一键配置材质/贴图并开始绘制。
- 已复用地形工具目录中的 56 张笔刷 PNG，不依赖另一 Editor 程序集的 internal C# 类型；支持圆形、方形、自定义贴图、旋转、反相及灰度/Alpha 采样。目标生产 Shader 为 `Valkyria/Effect/FlowMap`。
- 当前按会话交付描述使用 UV0 且限 0–1；不支持 UDIM/越界 UV，重叠 UV 会共享绘制像素。SkinnedMesh 使用静态姿态快照。画布可选 256/512/1024/2048；每次抬笔同步创建临时恢复 PNG。
- 独立测试曾测得连续绘制更新约 0.5–1 ms；同步读回并压缩恢复 PNG 在 RTX 3060、约 3.2 万三角面样例上约为 512 分辨率 14–23 ms、1024 为 49–85 ms、2048 为 189–307 ms。该测试不含主工程场景渲染，抬笔停顿仍待优化。
- 截止区间末，用户要求读取所选 Mesh 材质、选材质槽及贴图属性后直接载入绘制；当时只有实现计划，代码尚未修改。这里的“贴图通道”究竟指 Shader 贴图属性还是 RGBA 分量，也尚未确认。

## 2. 根因、修正与证据

- **旧 Prefab 节点残留/保护校验误报**：源层级改名/删除与保留快照混用，且 Editor 程序集曾落后于源码。增加显式删除授权后，报告中 QHigh 已切换新名；EditMode 仍未执行。
- **旧 Mesh 文件仍存在**：它仍被其他 Prefab/Scene 引用。用户后来要求无论引用都强制清理；新行为会造成 Missing，不能等同安全迁移。
- **FBX 名称肉眼一致但验证失败**：隐藏零宽字符与命名顺序差异；尚无工具端统一规范化修复的记录。
- **绘制抬笔停顿**：来源会话将其归因于同步 GPU 读回与 PNG 压缩；异步备份被提出为下一步，但当日尚未实现。

## 3. 项目约束与候选

- 处理强制 Mesh 删除前，应对受影响 QLow/Battle/Scene 做显式恢复计划；现有代码不会将其他资产引用自动映射到新 Mesh。
- FlowMap Painter 输出根路径在会话中描述为 `Assets/FlowMaps/<模型名>/`，共享材质不覆盖；需以当前工具源码/README 确认实际资产格式与路径后再扩展。
- 已有材质直接载入功能待实现；开发时分开定义 Renderer 材质槽、Shader 贴图属性和 RGBA 分量选择，避免把三个概念合并。
- 这里的层级保护、工具菜单、目录、性能数字和提交状态均依赖 ProjectACG 当前实现，不升级为 CORE。

## 4. 验证状态与回退

- **已报告**：PrefabBuilder 测试程序集编译、格式检查；QHigh 新节点直接检查；Timeline 保存工具程序集编译；FlowMap Painter 独立编译与 GPU 检查、笔刷操作回归、性能测量。
- **未验证**：PrefabBuilder/Timeline 保存的 Unity EditMode、强制删除的实际资产影响、道具绑定修复、FlowMap Painter 主工程交互、异步备份以及已有材质属性载入。
- **回退**：强制删 Mesh 后应从版本控制恢复被标 Missing 的资源/引用；Prefab 更新失败时保留旧 Prefab 和 Mesh，重新载入新程序集后重建；绘制错误时从临时恢复 PNG 或既有输出贴图恢复。

来源：[01a1cab4-df21-72c3-ba9b-5c0157ab2b5a](../../../../../VA_Worlk_memory/sessions/01a1cab4-df21-72c3-ba9b-5c0157ab2b5a.md)、[01a1ce6c-9a06-75f3-a87c-2e48e35153ea](../../../../../VA_Worlk_memory/sessions/01a1ce6c-9a06-75f3-a87c-2e48e35153ea.md)、[01a1cfd8-0052-70e3-b54e-213cc45ec6fd](../../../../../VA_Worlk_memory/sessions/01a1cfd8-0052-70e3-b54e-213cc45ec6fd.md)、[01a1d02f-f5cf-7c83-8274-b18802855696](../../../../../VA_Worlk_memory/sessions/01a1d02f-f5cf-7c83-8274-b18802855696.md)。
<!-- END automation:ta date:2026-11-12 layer:PROFILE -->
