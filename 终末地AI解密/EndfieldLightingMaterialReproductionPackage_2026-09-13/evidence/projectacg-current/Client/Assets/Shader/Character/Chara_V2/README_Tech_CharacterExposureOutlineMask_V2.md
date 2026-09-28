# Character Exposure Outline Mask V2 Tech README

## 1. 技术目的

V2 描边由顶点外扩产生，超出角色本体的像素无法被通用角色 Mask 覆盖。本功能为 V2 Shader 增加专用 `CharacterExposureOutlineMask` Pass，使外扩描边写入现有 Character Exposure Mask。

## 2. 代码入口

- 共享顶点与 Mask Fragment：`Common/CharaOutlinePass_V2.hlsl`
- Mask 提交：URP `Runtime/Passes/PostProcessPass.cs`
- 接入 Shader：`Chara_Cloth_V2.shader`、`Chara_Face_V2.shader`、`Chara_SilkStockings_V2.shader`、`Chara_Hair_V2.shader`、`Chara_Fringe_V2.shader`、`Chara_Skin_V2.shader`、`Monst_V2/Monst_V2.shader`

## 3. 具体实现逻辑

1. 通用角色 Mask 先使用轻量 override material 写入角色本体。
2. URP 再以 `LightMode=CharacterExposureOutlineMask` 提交 V2 描边。
3. 专用 Pass 复用 `OutlinePassVertex`，保持宽度、平滑法线、描边遮罩、FOV/距离修正与正式描边一致。
4. `OutlineExposureMaskPassFragment` 固定向 R 通道写入 1；描边关闭时 discard，避免覆盖已有本体 Mask。
5. UberPost 根据合并后的 Mask 在同一次 Tonemapping/LUT 中选择 Scene 或 Character Exposure。

## 4. Pass 说明

- `LightMode`：`CharacterExposureOutlineMask`
- `Cull Front`：保持反壳描边轮廓。
- `ZTest LEqual`：使用现有场景深度处理遮挡。
- `ZWrite Off`：禁止修改后处理前的场景深度。
- `Blend One Zero`、`ColorMask R`：只向现有角色 Mask 写入二值标记。

## 5. 变体与依赖

- 复用 `_OUTLINE_ON` 与 GPU Instancing 变体。
- 复用各 Shader 原有 bindings，保持 UnityPerMaterial 布局一致。
- 依赖 URP Tonemapping 的 Character Exposure Mask；功能关闭或两路 EV 相同时不会提交该 Pass。

## 6. 性能与边界

- 不新增 RenderTexture、不新增全屏 Pass、不重复 Tonemapping。
- 启用独立 Exposure 且两路 EV 不同时，额外提交一次匹配的 V2 描边几何；Fragment 仅固定写入 R8 Mask。
- 非 V2 描边 Shader 不包含该专用 Pass，仍只按其本体几何参与 Character Exposure Mask。

## 7. 验证方式

1. Scene Exposure 与 Character Exposure 设为不同值，确认本体和描边同步。
2. 关闭 `_OUTLINE_ON` 或全局描边开关，确认专用 Pass 不写入外扩 Mask。
3. 检查角色被场景遮挡时，描边 Mask 不覆盖前景场景。
4. 使用 Frame Debugger 确认 `CharacterExposureOutlineMask` 写入现有 `_CharacterExposureMask`，且没有新增全屏 Tonemapping Pass。
