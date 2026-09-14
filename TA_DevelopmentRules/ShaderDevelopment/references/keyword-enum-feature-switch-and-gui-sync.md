# Unity KeywordEnum 功能开关与材质关键字同步参考

> 类型：REFERENCE；适用范围：用 `[KeywordEnum]` + `shader_feature_*` 把材质上的档位、层数或模式开关做成编译期裁剪（删掉未用分支的采样与 ALU），以及排查「下拉显示与实际渲染不一致」「选了第一档却按第二档渲染」；使用前提：必须以目标工程的 Unity 版本、Shader、存量材质和变体收集方式重新验证。
>
> 关联规则：`CTL-02`、`ORG-02`、`ORG-03`、`VAL-01`、`VAL-03`。变体报告模板见 [shader-variant-report.md](shader-variant-report.md)，变体裁剪与构建安全见 [shader-variant-stripping-and-build-safety.md](shader-variant-stripping-and-build-safety.md)，C# 侧离线编译校验见 [Unity Editor 脚本离线编译校验参考](../../ToolDevelopment/references/unity-editor-script-offline-compile-verification.md)。ProjectACG 实例见 [ProjectACG 顶点色混合层数开关 Profile](../Profiles/ProjectACG/vertex-blend-layer-count-keyword-switch.md)。

## 1. 适用场景

- 材质上一个下拉框要决定「编译几层、启用哪条分支」，未选中的分支采样与 `lerp` 应在编译期消失，而不是用 `0` 权重在运行时遮罩。
- 新增档位下拉后，存量材质（可能完全没有该属性、`m_ValidKeywords` 为空）必须落到与改动前一致的默认档。
- 排查「下拉显示 A、画面却是 B」「改了下拉不生效」「材质上出现 shader 未声明的关键字」。

不适用于：把强度、UV、颜色等连续数值做成关键字；需要运行时全组合的管线状态应使用 `multi_compile` 或全局关键字（见 `CTL-02`）。

## 2. 前提与依赖

| 依赖 | 说明 |
| --- | --- |
| 属性声明 | `[KeywordEnum(A, B, C)] _Feature("Feature", Float) = 0`：Unity 按选项顺序生成 `<属性名大写>_<选项名大写>` 关键字，**选项 0 的关键字就是「默认档」**。 |
| pragma | 关键字必须在**每个会读取该宏的 Pass** 中声明；阶段限定形式（`_fragment` / `_vertex`）必须与其他 Pass 保持一致。 |
| 判定依据 | 材质实际渲染看的是**关键字集合**（`.mat` 的 `m_ValidKeywords` / `m_InvalidKeywords`），不是属性的 `float` 值；两者可以不同步。 |
| 存量材质 | 旧材质可能既没有该属性、也没有任何关键字，必须由 shader 默认值与关键字列表兜底。 |
| ShaderGUI | 属性经 `MaterialEditor.ShaderProperty` 绘制才会走 Unity 的 `KeywordEnumDrawer`，且只在用户操作下拉时同步关键字。 |

## 3. 实现或排查步骤

### 3.1 关键字列表逐项列出枚举状态，默认档必须是第一项

写法（枚举 `N` 项 → 恰好 `N` 个关键字）：

```hlsl
[KeywordEnum(Layer1, Layer2, Layer3, Layer4)] _BlendLayerCount("Blend Layer Count", Float) = 0
...
#pragma shader_feature_local_fragment _BLENDLAYERCOUNT_LAYER1 _BLENDLAYERCOUNT_LAYER2 _BLENDLAYERCOUNT_LAYER3 _BLENDLAYERCOUNT_LAYER4
```

- **多关键字列表里，材质不携带列表内任何关键字时，Unity 用列表第一项渲染。** 这是「`_` 作为首项表示无关键字」用法的来源，也是本类问题最常见的根因：列表写成 `_B _C _D` 而默认档是 `A` 时，所有没写过关键字的材质都会按 `B` 渲染，现象就是「选第 1 档却跑第 2 档」。
- **不要额外加 `_`**：当选项 0 的关键字本身就是「默认/关闭」档时，再加 `_` 只会多编一个内容相同的变体。
- **不要漏掉选项 0 的关键字**：用户选中第 1 档时 Unity 会启用该关键字，未声明会让材质留下未声明关键字（进入 `m_InvalidKeywords`，Inspector 显示异常）。
- 宏分支按「高 → 低」写 `#if / #elif / #else`，并在 `else` 注释里写明它等于哪个档位；关键字互斥时顺序不影响结果，但可读性直接影响后续维护。
- 每个 Pass 都要声明：只在一个 Pass 声明会让其他 Pass（阴影、深度、Meta）各自落到默认分支，导致 SSAO、深度或烘焙与主渲染不一致（见 `ORG-03`）。

### 3.2 变体计数

- 该 Pass 的变体数 = 列表关键字数 = 枚举项数，不存在「枚举项数 + 1」；出现 `+ 1` 通常意味着多写了一个 `_` 或存在默认档之外的兜底档。
- `shader_feature*` 在构建时按材质实际使用剥离，`multi_compile*` 会全部编入；材质可枚举的档位优先 `shader_feature_local`。
- 新增、启用或删除影响变体的 pragma 时按 `VAL-03` 输出报告；静态计数不能替代实际剥离或 Variant Capture 证据。

### 3.3 自定义 ShaderGUI 的关键字同步

`[KeywordEnum]` 的状态存在两处：属性的 `float` 值与材质关键字。以下路径只改前者，会留下「下拉变了、渲染没变」的脏状态：

- Inspector 复制/粘贴属性、预设或工具批量写值、编辑器/运行时脚本 `SetFloat`；
- ShaderGUI 用 `EditorGUILayout.PropertyField` 等绕过 `MaterialPropertyDrawer` 的方式绘制；
- 更换 Shader 或从旧 Shader 迁移材质。

推荐模式（在 `OnGUI` 早期执行一次，幂等）：

1. 从 shader 源码解析出所有 `[KeywordEnum(...)]` 属性名与选项名（形如 `[KeywordEnum(A, B)] _Feature`），得到候选关键字 `<属性名大写>_<选项名大写>`。
2. 用属性当前 `float` 值定位目标档位，只对 **shader 实际声明过的** 关键字启用或禁用；未声明的关键字保持不动，避免往其他 Shader 的材质里写入垃圾关键字。
3. 先比较 `Material.IsKeywordEnabled` 再写，只有真正变化时才写关键字并 `EditorUtility.SetDirty`；`hasMixedValue` 的多材质编辑直接跳过。
4. 静态方法里必须用 `ShaderGUI.FindProperty(name, properties, false)`；调用子类自己的实例 `FindProperty` 重载会直接编译失败（`CS0120`，并让整个 ShaderGUI 不可用）。

该同步只负责把属性值兑现成关键字，不改变关键字语义；默认档仍由 3.1 的首项决定。

### 3.4 排查顺序

1. 看材质：Inspector 的关键字面板、`.mat` 的 `m_ValidKeywords` / `m_InvalidKeywords`，确认材质到底带了哪个关键字（很可能一个都没有）。
2. 看属性：材质里是否存在该属性；不存在时用 shader 默认值（`= 0` → 选项 0）。
3. 看 pragma：列表是否覆盖全部枚举项、首项是否是期望的默认档、是否多余 `_`、所有相关 Pass 是否一致。
4. 看 GUI：属性值变化后关键字是否被同步（3.3）。
5. 看结果：Unity 内重编译并逐档位对比画面；C# 侧改动用离线编译校验作为补充证据（见本文开头的 ToolDevelopment 参考）。

## 4. 风险与不适用边界

| 边界 | 说明 |
| --- | --- |
| 关键字是序列化数据 | 改关键字名、枚举顺序或列表顺序都等于材质数据迁移：必须重新打开材质或由 ShaderGUI 回写，不能只改 shader。 |
| 枚举顺序即语义 | 把默认档从选项 0 挪走会改变所有「无关键字」材质的渲染结果；迁移时应保持选项 0 = 原默认档。 |
| 多 Pass 一致性 | 缺一个 Pass 的 pragma 会造成深度、阴影或烘焙与主渲染分叉，且通常只在特定相机或 SSAO 下可见。 |
| 构建剥离 | `shader_feature` 档位可能被剥离，AssetBundle/热更动态加载路径必须按项目变体收集或 SVC 策略单独确认。 |
| 不属于本参考 | 真需要运行时全组合的状态用 `multi_compile` 或全局关键字；连续数值不要做成关键字。 |

## 5. 验证与回退

| 层级 | 检查 | 通过条件 |
| --- | --- | --- |
| 静态 | 各 Pass 的 pragma 列表一致、逐项对应枚举、首项 = 默认档、无多余 `_`。 | 人工核对通过，关键字命名与材质实际关键字完全一致。 |
| 材质 | 打开材质看关键字面板：同一属性只应存在一个档位关键字，旧材质应在首次打开后被回写。 | 关键字与下拉一致，无 `m_InvalidKeywords` 残留。 |
| 视觉 | 逐档位切换，确认采样分支真的生效（例如 1 层只参与第一层权重）。 | 每档位表现与预期一致，无「选第一档跑第二档」。 |
| 变体 | 构建日志或 Variant Capture 确认实际保留的档位与材质使用一致。 | 只保留被材质使用的档位，动态加载路径不缺档位。 |
| 回退 | 列表首项改回原默认档并重新打开材质；无法回退时保留旧材质备份。 | 无关键字材质与旧版本表现一致。 |
