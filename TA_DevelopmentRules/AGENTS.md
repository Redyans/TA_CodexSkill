# TA_DevelopmentRules 写作约束

本文件适用于当前目录及所有子目录。

## 修改前

- 必须先读取 [README_Tech_TADevelopmentRules.md](README_Tech_TADevelopmentRules.md) 的“写作前置规则”，再读取目标模块入口。
- 新建模块或调整文档结构时，必须参考 [ShaderDevelopment/README_Tech_URPShaderDevelopmentRules.md](ShaderDevelopment/README_Tech_URPShaderDevelopmentRules.md) 的分层、稳定 ID、章节顺序和工程规范语气；只参考结构，不复制 Shader 领域内容。
- 先确认目标内容属于 `CORE`、`REFERENCE`、`PROFILE` 还是功能 README；未完成分类不得开始写入。

## 写入闸门

- 跨项目稳定且证据充分的规范才进入 `CORE`。
- 依赖项目名、路径、版本、类型、字段、枚举、菜单、资产、GUID、Pass、Keyword、Renderer 配置、团队约定或当前验证状态的内容必须进入对应项目 `PROFILE`。
- 可迁移的技巧、经验、实现模式、排查步骤、决策表和检查清单进入 `references/`，不得与 CORE 并行定义强制规则。
- 单一功能的使用说明和维护记录留在其源码或资产旁，不得为了集中管理复制到本目录。
- 不确定时默认保留为项目 Profile 候选；没有复现、根因和验证证据的个案不得升级为 CORE。

## 统一格式

- 新模块使用“概览 → 工作流程 → 通用规则（CORE）→ 项目定制规则（PROFILE）→ 资源加载与规则维护”的主结构；`references/` 和 `Profiles/<Project>/` 按内容性质拆分。
- 规范性规则使用稳定 ID 和动作性标题，正文按“结论/适用范围 → MUST/SHOULD/MUST NOT → 验证 → 例外与回退”组织。
- 技巧、经验和技术参考按“适用场景 → 前提与依赖 → 实现或排查步骤 → 风险与不适用边界 → 验证与回退”组织。
- 使用中文、UTF-8、英文文件名和相对链接；代码标识符、路径、菜单、Pass、Keyword 与命令使用反引号。
- 已有规则能表达时更新原条目，其他位置只引用稳定 ID；禁止追加同义规则、聊天式流水账或未经验证的绝对结论。
- 新增或移动 Unity 文档资产时同步维护 `.meta`，保持 GUID 唯一。

## 完成前

至少检查分层、章节结构、稳定 ID、相对链接、UTF-8/乱码、尾随空白、`.meta`，并明确记录未验证项。除非用户明确要求，不因统一格式而批量改写无关既有文档。
