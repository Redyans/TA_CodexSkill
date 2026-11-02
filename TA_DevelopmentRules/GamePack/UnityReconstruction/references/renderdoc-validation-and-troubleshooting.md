# RenderDoc 复刻验证与故障排查

## 适用场景

资源已经导入或抓帧数据已导出，但 Unity 画面、动画、Shader 或本地管线与目标不一致时使用。先执行模块的 [UCR-VAL-01、UCR-VAL-02](../README_Tech_UnityCaptureReconstructionRules.md)；下面是可迁移的定位方法，具体数值以当前项目 Profile 为准。

## 前提与依赖

至少有原始输入清单、源事件的 RT/资源格式与内容、目标场景、版本/管线配置、可保存的 Unity GPU 回读、原参考图的明确颜色转换。逐字节结论需要同一格式/尺寸/输入；无法满足时报告数值误差与限制，不混用两种口径。

## 实现或排查步骤

### 用逐层比较缩小问题

| 层 | 核对内容 | 通过的证据 | 失败后先查 |
| --- | --- | --- | --- |
| 输入 | 源路径、SHA-256、版本、RDC event | 输入 manifest 与原文件哈希 | 错抓帧、截图非同帧、资源被覆盖 |
| 资产 | Mesh/Avatar、贴图 mip/face、动画 binding | Unity 回读字节、路径/曲线计数、骨骼采样 | PNG 重压缩、丢运行期通道/曲线 |
| Shader/Pass | 变体、Render State、采样器和常量 | Unity 编译、同输入 GPU 回归 | DXBC 反编译错误、精度/采样差异 |
| 时序 | 阴影、AO、透明、反射、TAA、Bloom | 每阶段 RT 回读与 event 对应 | 资源在不同 event 被重写、Pass 次序错误 |
| 场景 | 实时阴影/反射、人物/环境 HDR | 固定相机和姿态的区域指标 | 矩阵、深度、历史缓存、几何差异 |
| 显示 | 最终图与播放 | sRGB/linear 明确转换、整图/分区误差、PlayMode | flipY、曝光/LUT 重复、异步任务未完成 |

参考图自比较应为零。屏幕图比较时保持原分辨率，明确是否 `flipY`、IEC linear-to-sRGB、alpha premultiply、MSAA resolve、crop 或重采样；HDR 比较直接在相同线性域进行。可靠不透明掩码应由原几何深度/三角形覆盖生成，并单列透明与 alpha-test 边缘；不得通过图像配准、颜色拟合或背景替换降低误差而不声明。

### 常见故障与根因

| 现象 | 排查方法与修复 | 验证/回退 |
| --- | --- | --- |
| `.anim` 有文件但只剩 1 秒、零曲线 | 检查源 clip 是否只有 runtime StreamedClip；解析原系数、Avatar/TOS 并显式写编辑器曲线。 | 比对原时长、binding/key、真实骨骼采样；无映射 hash 保留原数据。 |
| 动作显示几乎不动 | 先比较源曲线的标量范围与实际骨骼位移，确认是否原始近静止片段。 | 不用“视觉不明显”直接判坏；检查源字节与 Unity 持久化曲线。 |
| 原 mesh 某些效果缺失 | 将 RenderDoc 顶点输入/输出与 AssetBundle Mesh 通道逐点比较，定位运行期新增 UV/顶点流。 | 只补已取证通道，重验 submesh 和描边/粒子 draw。 |
| 导出的顶点色含 NaN 或极大浮点数 | 读取 `GetVertexInputs()` 的 `format`、`vertexBuffer`、`byteStride` 和偏移；零步长顶点流代表每顶点复用相同字节。按实际 UNorm/UInt/Float 类型解码，不能仅根据组件字节宽度选择 float 解包。 | 保留原四字节样本与通道布局，重建后核对顶点色、UV 和特效 draw；旧误读 JSON 只作故障证据。 |
| PNG 看起来对、材质仍有偏差 | 核查 BC 块、mip、cube face、sRGB view、sampler 和 AG 法线解码；对原 GPU raw 做 native 字节比较。 | 保留 PNG 预览，最终材质切回校验通过的 native texture。 |
| Shader 反编译后编译失败或输出异常 | 对照 DXBC 原指令的 swizzle、组件数、整数重解释、saturate 与运算顺序，局部修正生成器。 | 保留原 ASM 和修改 diff；用真实输入做 GPU 回归。 |
| 相机画面整体错位 | 查 VS 实际读取的 VP 槽；同一 cb 中可能有反射相机的旧 oblique 矩阵。 | 导出 clip-space 投影点，对原 draw 逐顶点比较。 |
| 阴影只剩极暗窄范围 | 检查 RTHandle 实际是否按 shadow map 分配、比较采样器是否启用、深度方向及 compare func。 | 分别回读 shadowmap、比较结果和 screen-shadow；修复后释放旧 RT。 |
| 反射和主场景局部差异大 | 查反射专用 View/斜截投影、反向剔除、独立 shadowmap 与原反射纹理 sampler；资源 ID 可能在帧中复用/重写。 | 单独导出反射 RT 对照，不以主相机矩阵或旧资源快照替代。 |
| AO 边缘/水面差异 | 核对 depth 生成者、AO 所在时序、atlas 全层、上采样尺寸取整、水面是否写 depth。 | 独立验证每阶段，再跑实时场景；同输入一致不代表实时深度一致。 |
| TAA 拖影/抖动错误 | 核对当前/历史 HDR、motion、深度、投影 jitter 与非 jitter 矩阵；每相机 history 独立，不能同时读写同一 RT。 | 先用原输入验证 TAA，再检查连续帧 motion/history。 |
| Bloom/调色过亮或黑位错 | 查 LogC/LUT 输入域、曝光纹理、Bloom pyramid 和最终 sRGB 转换；避免重复 URP tonemapper。 | 每层回读，分别比较 HDR、Uber 后线性图和显示图。 |
| URP 类型 CS0246 / 包不加载 | 检查 `Packages` 入口与 UPM 实际 source；项目 `Assets` 内包不能直接作为本项目的 `file:` 包。 | 用目标 Editor 日志、lock 和脚本加载报告验证；按备份恢复包/manifest。 |
| 后台命令成功但报告未更新 | PlayMode/导出可能异步；核对最终报告时间、退出码和控制队列状态。 | 等待任务完成，不重用未完成请求或旧报告。 |
| 抓帧 draw 的普通 VS/PS 适配后少一整块布料或身体 | 查输入拓扑和 Hull/Domain 是否实际绑定，读取 `GetPostVSData(..., GSOut)` 的顶点数量与布局；不能把 PatchList 当普通三角形列表。 | 保存 HS/DS 原字节、常量与后细分样本；先核对细分因子和拓扑，再单独验证 Domain 位移分支和实时动画。 |
| 批量场景改相机后全黑 | 检查导出 JSON 的向量形状；Unity `JsonUtility` 的 `Vector3` 字段需要 `{x,y,z}`，数组会读为默认零值。 | 写入前校验位置范围与 FOV；重开场景并重渲染全批量，不能只检查脚本返回成功。 |
| 连续扫描动作相位时分数改善，独立复拍却变差 | 在同一时间点重复渲染、倒序扫描，再从重新打开的场景单独采样；检查骨骼未绑定通道、次级运动与 TAA/反射历史是否继承上一步。 | 只用独立复拍确认相位；连续扫描结果作为诊断，失败时恢复原场景值并保存新报告。 |
| RDC 容器头可解析但回放读取越界 | 对照正常文件核查 section 偏移、长度与文件末尾；先复制原件再试验修补，分别记录 `OpenFile` 与 `OpenCapture` 的状态。 | 容器头修补不能证明压缩帧完整；回放失败时保留原 SHA-256，要求新抓帧，不由截图推造 GPU 常量。 |

### 记录 Evidence → Finding → Path

每个主要结论用以下最小记录；大型原始文件只保存哈希与相对工程路径，不复制到规则库。`Evidence` 是观察，`Finding` 是解释，`Path` 表示“客户端/抓帧 → 中间数据 → Unity 实现 → 验证”的调用/数据路径。

```text
E-01 source_ref=<项目内相对路径> event=<实际事件> sha256=<原始文件哈希>
     repro=<脚本或 Unity 方法> observed=<原始输出、尺寸、格式>
F-01 evidence=[E-01] status=validated|candidate confidence=high|medium|low
     conclusion=<只覆盖已观察范围的结论> location=<实现或资产>
P-01 source -> extraction -> import/render -> GPU/PlayMode result
     each step: evidence=<E-ID> finding=<F-ID or none>
```

复制到其他角色时新建角色清单和报告，不复用旧角色事件号、哈希、相位或 shader 常量。先用同一共用 Shader/RendererFeature 的代表性输入验证，再记录当前角色需要的差异层。

批量角色镜头要先由角色主视图 draw 事件确定，再从该事件的实际 VS 常量读相机与投影。捕获中的首个 draw 可能属于阴影图、反射或深度预通道，其矩阵不能直接写入展示场景。细分 draw 的 `VS` 也可能只透传控制点，镜头常量实际在 `DS`；选择读数事件时先核查 Shader 阶段。

## 风险与不适用边界

单帧中的反射、粒子、DynamicBone 和 TAA history 只是当时状态；捕获前运动过程无法从当前输出唯一反推。独立模块的逐 bit 校验只能证明该模块在给定输入下的实现，不能替代实时场景的多帧验收。旧阶段报告带时间和条件，后续代码变动后必须重测。

## 验证与回退

故障修复后先重跑最窄的原输入测试，再重跑受影响的场景分区、播放和目标工程健康检查。每次保存新前缀与日志；修复无效时恢复修改前 Package/Renderer/材质配置，保留失败报告便于比对。项目实例与已验证指标见 [BookOfYog Profile](../Profiles/BookOfYog/linglong-showcase-reconstruction.md)。
