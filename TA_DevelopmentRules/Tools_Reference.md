# TA Codex 工具参考

## 文件与完整性

- PowerShell：路径、大小、时间、`Get-FileHash -Algorithm SHA256`、清单比对。
- `VERIFY ALL.PS1`：解包后的总完整性门槛；以 `OVERALL VERIFY=PASS` 为准。
- 7-Zip：查看 ZIP/MSI/CAB，不覆盖原始文件。
- `rg` / `rg --files`：快速检索资源名、Shader 名、参数名和日志。

## Unity 资源

- Unity 2022.3 + URP 14.0.12：目标运行时和 API 基线。
- AssetRipper / AssetStudio：Bundle、CAB、Mesh、Material、Texture、Animator、Avatar、AnimationClip 初步索引。
- UnityPy 或自写 C# Probe：批量读取对象引用、序列化字段和文件 ID。
- Unity Editor Frame Debugger：验证 RenderFeature 顺序、材质、关键字和后处理。
- `dotnet build --no-restore`：先做 C# 编译门禁；不能替代 Unity 编译和 GPU 验证。

## GPU 与效果

- RenderDoc：读取 `.rdc` 的 Event、Pipeline State、Shader、SRV/UAV、Constant Buffer、RenderPass、纹理格式和后处理链。
- Unity RenderDoc 集成：同一分辨率、HDR、色彩空间、MSAA、相机和曝光下对照。
- HLSL/ShaderLab：按 skin、face、hair、eye、clothPBR、overlay 分模块实现。
- Compute Shader：直方图自动曝光、LUT 构建或其他屏幕空间计算；验证 buffer 尺寸、线程组和读回生命周期。

## 动画

- C# 反射探针：读取 `AnimationClip`、GenericBinding、`m_AclCompressedBuffer`、`m_MuscleClip`、采样率和轨道数量。
- ACL 解码器：必须能把 compressed transform tracks 解成时间采样，再映射到 Unity 骨骼路径。
- Unity AnimationClip API：生成曲线、设置 sample rate、length、loop 和 root motion。
- Humanoid Avatar / Animator：验证骨骼路径和实际运动；仅有 Avatar 资产不等于动画兼容。

## 验证工具链

1. 文件哈希和 `VERIFY ALL.PS1`。
2. Bundle/CAB 对象索引和 JSON 证据。
3. C# Probe 输出对象字段与路径。
4. `dotnet build --no-restore`。
5. Unity Console、Frame Debugger、Play Mode。
6. RenderDoc 与 Unity 帧逐项对照。
7. 结果、未知项和限制写入 Markdown；任何推断都标注证据等级。
