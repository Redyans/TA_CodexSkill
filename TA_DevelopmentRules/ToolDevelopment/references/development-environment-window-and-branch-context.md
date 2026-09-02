# Windows 多工程开发环境的窗口、工程路径与 Git 分支识别参考

> 类型：REFERENCE；适用范围：Windows 上同时维护多个独立 Unity 工程、需要区分 VS Code 窗口和 Git 分支的开发环境；使用前提：先按目标机器实际安装路径、VS Code 版本和 Git 仓库结构重新验证。本文不改变 Unity 运行时、Editor 工具或 Player 构建行为。

## 1. 适用场景

当一台 Windows 机器同时打开多个 Unity 工程时，仅依靠 VS Code 图标或编辑器标签很容易把工程、分支和文件改错。推荐把问题拆成三个显示层：

| 显示层 | 负责内容 | 典型实现 | 不能替代的内容 |
| --- | --- | --- | --- |
| Windows 外部窗口标签 | 把多个独立 VS Code 窗口合并为一组并快速切换 | TidyTabs、Groupy 等窗口级工具 | Git 分支真相、Unity 工作区边界 |
| VS Code 窗口标题 | 显示当前工程名或路径 | `window.title`、`${rootName}`、`${folderPath}` | 动态分支，除非另有已验证扩展 |
| VS Code/GitLens 视图 | 显示和切换当前 Git 分支 | VS Code 源代码管理、GitLens | Windows 外部标签页 |

直接结论：多个独立 Unity 工程应保持独立 VS Code 窗口；窗口级标签只负责聚合，工程路径和 Git 分支分别由标题与 Git 视图提供。不要把多个工程简单加入同一个多根工作区来模拟窗口标签。

## 2. 前提与依赖

### 2.1 独立工程窗口

- 每个 Unity 工程单独启动一个 VS Code 窗口，并在该窗口中打开实际工程目录。
- 如果 Git 仓库根目录在工程目录的上级，VS Code 仍可能从上级仓库读取分支；必须用 `git rev-parse --show-toplevel` 确认，而不是只看当前打开的子目录名。
- Unity/C# 扩展、解决方案、调试配置和生成目录应保持在各自窗口上下文，避免跨工程混用。

### 2.2 本机案例（仅作可复用排查证据，不是通用默认值）

| 项目 | 已观察值 | 证据/边界 |
| --- | --- | --- |
| TidyTabs 安装目录 | `D:\RJ\TidyTabs` | 通过目录枚举确认 |
| TidyTabs GUI | `D:\RJ\TidyTabs\TidyTabs.Gui.exe` | 应启动 GUI；`TidyTabs.Daemon.exe` 是后台组件 |
| GitLens 扩展 | `eamodio.gitlens-19.0.1` | 读取本机扩展 `package.json` |
| VS Code 打开目录 | `D:\work2025U3D\Valkyria\ProjectACGMain3\ProjectACG\Client` | 当前窗口路径 |
| Git 仓库根目录 | `D:/work2025U3D/Valkyria/ProjectACGMain3/ProjectACG` | `git rev-parse --show-toplevel` |
| 当前分支 | `feature/TA_Variant-` | `git branch --show-current` |

上述路径、版本和分支随机器或工作区变化，使用前必须重新读取；不得把它们写成跨项目 CORE 规则。

## 3. 实现与使用步骤

### 3.1 用 TidyTabs 聚合多个 VS Code 窗口

1. 启动 TidyTabs GUI：

   ```powershell
   Start-Process 'D:\RJ\TidyTabs\TidyTabs.Gui.exe'
   ```

2. 在任务栏通知区域确认 TidyTabs 托盘图标已经运行。
3. 用 `文件 → 新建窗口` 或分别从工程目录启动多个 VS Code 窗口；不要先把它们合并为一个多根工作区。
4. 将 VS Code 窗口暂时恢复为非最大化，便于拖动。
5. 将鼠标移到窗口顶部/左上角，确认出现的是 **TidyTabs 生成的外部窗口标签**，不是 VS Code 编辑器标签。
6. 拖动该外部标签到另一个 VS Code 窗口顶部，等待高亮目标区域后松开；成功后多个窗口会显示在同一组标签中。
7. 无标签或拖动无高亮时，右键托盘图标检查应用排除/允许列表，确认 `Code.exe` 未被排除；修改后重启 TidyTabs 或 VS Code 再试。
8. 需要拆分时，将外部标签拖出标签组，或使用 TidyTabs 菜单中的分离/移到新窗口操作；具体菜单名以本机版本为准。

关键边界：TidyTabs 是 Windows 窗口级工具，不是 VS Code 扩展；拖动对象、合并对象和拆分对象都属于外部窗口层。不能把 VS Code 编辑器内的文件标签拖动行为当作 TidyTabs 合并成功。

### 3.2 用 VS Code 原生标题显示工程路径

在工程级 `.vscode/settings.json` 中设置标题，避免不同窗口只显示相同的 `Visual Studio Code`：

```json
{
    "window.titleBarStyle": "custom",
    "window.title": "${rootName} · ${folderPath} · ${appName}"
}
```

如果标题过长，可只保留：

```json
{
    "window.titleBarStyle": "custom",
    "window.title": "${rootName} · ${appName}"
}
```

`${rootName}`、`${folderPath}` 和 `${appName}` 是 VS Code 标题变量；它们能标识工程，不代表 Git 分支。工程级设置只影响当前窗口/工作区，不应为了显示路径去覆盖用户全局设置。

### 3.3 用 VS Code 与 GitLens 显示当前分支

- VS Code 左下角状态栏的 Git 分支是最可靠的当前分支入口，点击即可切换。
- GitLens 的 `gitlens.views.showCurrentBranchOnTop` 只控制 GitLens 视图顶部是否显示当前分支：

  ```json
  {
      "gitlens.views.showCurrentBranchOnTop": true
  }
  ```

- 该设置不会改变 Windows 标题栏，也不会自动改变 TidyTabs 外部标签文本。
- GitLens `19.0.1` 的本机 `package.json` 未发现已验证的“把当前分支动态写入 VS Code Windows 标题”的配置；不要仅凭设置名或网络文章断言它支持该功能。

### 3.4 需要标题中动态分支时的验证顺序

如果确实要求 TidyTabs 标签直接显示分支，应先确认专用扩展的真实身份，再安装和验证：

1. 在 VS Code 扩展页记录扩展发布者、名称、版本、配置键和权限范围。
2. 阅读扩展 `package.json` 或官方文档，确认它确实修改 `window.title`，并确认分支变量来自当前仓库而不是固定文本。
3. 在一个无关测试工程中验证切换分支、重载窗口、无 Git 仓库、Detached HEAD 和多仓库窗口。
4. 确认 TidyTabs 读取到的是扩展更新后的 Windows 标题，而不是缓存的旧标签。
5. 只有全部通过后，才将扩展加入团队推荐或工作区配置；否则继续使用“标题显示路径 + 状态栏显示分支”的低风险组合。

## 4. 常见问题与处理

| 现象 | 可能根因 | 处理方式 |
| --- | --- | --- |
| VS Code 窗口没有 TidyTabs 小标签 | TidyTabs GUI 未运行、`Code.exe` 被排除、窗口工具未捕获 | 先查托盘图标，再查允许/排除列表，最后重启 GUI |
| 拖动后没有合并 | 拖动的是 VS Code 编辑器标签或普通标题栏；窗口处于最大化 | 恢复非最大化，确认 TidyTabs 外部标签出现，再拖到顶部高亮区 |
| 标签只显示 `Visual Studio Code` | 未配置标题或标题变量不适用当前窗口类型 | 在工程级设置加入 `${rootName}`/`${folderPath}`，重载窗口 |
| GitLens 视图有分支，标题没有 | GitLens 视图设置与 Windows 标题是不同显示层 | 保留状态栏/视图分支；不要把 `showCurrentBranchOnTop` 当标题功能 |
| VS Code 显示的仓库和 Unity 工程不一致 | 打开的是子目录，Git 根在上级；或窗口包含多个仓库 | 执行 `git rev-parse --show-toplevel`，检查多根工作区和仓库边界 |
| 分支名称在窗口间看起来相同 | 标题只显示工程名，或 TidyTabs 缓存旧标题 | 增加路径、设置不同工作区颜色，并重载/重启后重新确认 |
| 网上推荐的标题扩展无法安装或找不到配置 | Marketplace/网络/TLS 问题，或扩展已改名/下架 | 记录扩展 ID 和版本；无法本地或官方验证时不要写成确定结论 |

## 5. 风险与不适用边界

- TidyTabs、Groupy 等窗口聚合工具不理解 Unity 的项目、程序集、Git 仓库或分支语义；它们只能改变窗口组织方式。
- 多根工作区不等同于多个独立 Unity 工程。C# 解决方案、Unity 扩展上下文、调试配置和资产路径可能相互干扰；除非明确验证，否则不要用它替代独立窗口。
- 窗口标题可以被远程窗口、空工作区、临时编辑器、扩展或 VS Code 版本改变；标题只能作为辅助识别，不能替代 Git 命令和版本控制操作前确认。
- 分支名可能包含很长的路径、特殊字符或 Detached HEAD 状态；标题扩展若未处理这些情况，可能截断、缓存或显示误导文本。
- 任何自动切换分支、批量打开工程、自动执行脚本或全局 Hook 都会扩大误操作范围；默认保持手动、显式和可回退。

## 6. 验证与回退

### 6.1 最小验证矩阵

- [ ] 每个 VS Code 窗口的工程路径与 Unity 工程目录一致。
- [ ] `git rev-parse --show-toplevel` 指向预期仓库；`git branch --show-current` 与状态栏一致。
- [ ] 切换到另一个分支后，状态栏和 GitLens 视图同步更新。
- [ ] TidyTabs 能合并两个独立 VS Code 窗口，并能分离回独立窗口。
- [ ] 关闭一个标签不会关闭或修改其他工程窗口。
- [ ] 重启 TidyTabs、重载 VS Code 后，标题路径和分支状态仍可重新确认。
- [ ] 无 Git 仓库、Detached HEAD、多根工作区至少各验证一次；失败时不启用动态标题扩展。

### 6.2 回退策略

1. TidyTabs 行为异常：先分离窗口，再退出 TidyTabs；VS Code 工程本身不受 Git 或 Unity 资源影响。
2. 标题配置异常：删除工程级 `window.title`，恢复 VS Code 默认标题；不要修改 Git 数据。
3. 标题扩展异常：禁用/卸载扩展，保留 GitLens 和 VS Code 原生分支显示。
4. 误把多个工程放入多根工作区：关闭该工作区，分别用 `文件 → 打开文件夹` 重新打开；不要执行跨仓库批量分支操作。

## 7. 可沉淀规则

- 窗口聚合、工程路径、Git 分支必须按显示层分责；一个工具的视图结果不能冒充另一层的真实状态。
- 任何“标题动态显示分支”的方案都必须以本机扩展元数据、分支切换和窗口重载验证为前提；未验证时使用路径标题加状态栏分支。
- 多个独立 Unity 工程默认使用独立 VS Code 窗口；多根工作区只在明确需要、依赖隔离已验证时使用。
- 记录工具安装路径、版本、仓库根目录和当前分支时，标记为机器/项目事实，不升级为跨项目 CORE；迁移时重新检查。
- 外部工具网络请求、Marketplace 查询或 TLS 失败时，只报告失败证据和未验证边界，不把推测的扩展名称、配置键或版本限制写成规则。
