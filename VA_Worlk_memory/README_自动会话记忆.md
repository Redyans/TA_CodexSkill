# VA Codex 会话记忆

记忆根目录：`G:\Connect\ConnectDocs\Doc\Resume\作品\工程\Codex\规则\TA_DevelopmentRules\TA_CodexSkill\VA_Worlk_memory`

每个本地 Codex 主会话生成一个 `sessions/<会话ID>.md`，后续问答更新同一个文件，恢复会话也继续更新。每轮回答后由 Stop Hook 保存，中断和正常结束时补存。首次安装后需要重开会话并审核新 Hook。

保存用户问题、Codex 可见的进度文字和回答。相同问题在不同轮次仍保留；不导出系统提示词、开发者指令、工具执行数据和模型内部推理。图片保留占位说明。文件是问答记录，不是模型自动生成的结论摘要，也不会自动加载进新会话。

## 文件位置

- 全局配置：`C:\Users\admin\.codex\hooks.json`（注意不在 hooks 子目录内）。
- 记录程序：`C:\Users\admin\.codex\hooks\va_work_memory.py`。
- 保存路径设置：`C:\Users\admin\.codex\hooks\va_work_memory.json` 的 `output_root`。
- 记录目录：本目录下 `sessions`。

## 启用与验证

在桌面应用的 Settings > Hooks / 设置 > Hooks 审核并信任 VA 会话记忆 Hook；CLI 可使用 `/hooks`。重开一个本地 Codex 会话，完成一次问答，再检查 `sessions` 文件修改时间。安装测试验证了脚本；只有实际完成一轮新会话后才能确认自动触发。

改变 Hook 或脚本后，Codex 可能要求重新审核。无需放宽沙箱或绕过 Hook 信任。云端会话不一定支持本机命令 Hook。

## 停用与维护

在 Hooks 设置中禁用这三个 Hook 即可停用；已经生成的文档保留。以后若合并其他 Hook，停用时只移除调用 `va_work_memory.py` 的条目。

脚本只使用 Python 标准库，无需 API Key、额外费用或外部网络请求。当前使用 Codex 自带 Python 的绝对路径；如果未来该运行时移动，需要更新全局 hooks.json。

G 盘必须可访问且可写。写入失败会显示“VA 会话记忆保存失败”，不会假装保存成功。输出是普通本地文件；该目录原有的同步、备份设置照常适用。

转录 JSONL 格式不是官方稳定接口。脚本适配了本机 0.154.0 格式；若未来格式变化，可能需要更新解析器。

官方资料：https://learn.chatgpt.com/docs/hooks
