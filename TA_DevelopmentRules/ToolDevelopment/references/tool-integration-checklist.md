# TA 工具集成检查清单

本清单服务于新增文件、改 Editor 工具、资产写入、菜单、程序集、导入/构建 Hook 或 Shader/材质接口时的最小充分检查。按任务命中项执行，不要求无关工具全部命中。

## 1. 实现前

- [ ] 已读取工具 CORE、当前项目 Profile、目标工具 README 和相邻同类实现。
- [ ] 已确认工具类型、目标用户、输入、输出、读写范围、覆盖策略、Undo、取消边界和回滚方式。
- [ ] 已确认是否仅 Editor；若涉及 Runtime、导入、构建、SceneView 或全局事件，已找到真实消费者和关闭/回滚入口。
- [ ] 已检查现有菜单、窗口、ContextMenu、设置面板和 asmdef，避免重复入口或错误依赖方向。
- [ ] 已确认用户可见文案的本地化机制与 Profile 术语；资源路径可由对象引用或 AssetDatabase 获得。

## 2. 代码与资产边界

- [ ] Editor 代码在 Editor 目录或 Editor-only asmdef 中；不修改 Library、Temp、Logs 或自动生成 csproj。
- [ ] 无运行时需求时，未新增 Runtime 依赖、场景对象、构建配置、Addressables/YooAsset 或 URP 构建逻辑。
- [ ] 新增配置/资源文件有正确 .meta；移动 Unity 资产时资产与 .meta 同步移动，GUID 可追溯。
- [ ] 资源批量操作的扫描、预览、确认、执行、汇总分层；OnGUI / Repaint 不执行资产写入。
- [ ] 需要 AssetDatabase.StartAssetEditing 的流程用 try/finally 成对保护，循环内无频繁 SaveAssets / Refresh。

## 3. 交互与执行

- [ ] 输入校验能指出缺失、类型错误、路径错误、冲突、重名、覆盖和不可处理项。
- [ ] 写入、覆盖、替换、删除、迁移或不可安全中断操作已有显式确认；能 Undo 的对象编辑已接入 Undo。
- [ ] 批量/长耗时操作显示进度、取消条件、耗时与总数/成功/失败/跳过汇总。
- [ ] 日志能定位到工具、对象/路径和失败原因；结果可复制或定位到资产。
- [ ] 临时预览、事件订阅、缓存、RenderTexture 和临时材质都有释放/恢复策略。

## 4. 导入、构建与运行时高风险项

- [ ] 未将局部工具需求错误实现为 AssetPostprocessor、构建回调、静态初始化或全局更新事件。
- [ ] 若确需 Hook，已定义路径/类型过滤、幂等性、递归保护、禁用开关、性能预算、异常策略和回滚。
- [ ] 已验证无关资产、增量导入、BatchMode/CI、Domain Reload、Player 编译和禁用状态。
- [ ] Editor 与 Runtime 有清晰 asmdef 边界；Player 不引用 UnityEditor。

## 5. Shader / 材质专项

- [ ] 已同时读取 Shader 开发模块、当前 Shader Profile 和目标 Shader/Include/材质/脚本消费者。
- [ ] 写入的属性、Texture、Keyword、RenderQueue、CustomEditor/Drawer、动画或 Timeline 接口均真实存在且语义兼容。
- [ ] 已区分局部材质、MaterialPropertyBlock 和全局 Shader 状态，避免影响无关角色/场景。
- [ ] 已在 Unity 内重新编译 Shader，并用目标材质/场景验证结果；变体变更已输出 Shader 变体报告。

## 6. 交付前

- [ ] 已完成 UTF-8、乱码、命名空间、API、asmdef、菜单冲突、链接和 diff 空白检查。
- [ ] 已在 Unity 中打开实际入口并验证代表性路径和至少一个边界用例。
- [ ] 已新增或更新紧邻工具的 Art README / Tech README；小型修复已同步受影响条目。
- [ ] 最终说明修改文件、菜单/窗口/资源入口、验证证据、未验证项和剩余风险。
