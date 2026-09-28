# Character Exposure Outline Mask V2 Art README

## 1. Shader 信息

- 适用范围：`Chara_Cloth_V2`、`Chara_Face_V2`、`Chara_SilkStockings_V2`、`Chara_Hair_V2`、`Chara_Fringe_V2`、`Chara_Skin_V2`、`Monst_V2`。
- 功能：当 URP Tonemapping 开启角色/场景独立 Exposure 时，让 V2 描边像素跟随 Character Exposure。

## 2. 参数功能说明

- 本功能不新增材质参数。
- 描边宽度、平滑法线、描边遮罩和全局描边开关继续使用原有参数。
- Tonemapping 中的 `Scene Exposure (EV)`、`Character Exposure (EV)` 和 `Character Layer Mask` 控制曝光分离。

## 3. 调试流程

1. 在 Volume 的 Tonemapping 中开启 `Separate Character Exposure`。
2. 确认角色所在层包含在 `Character Layer Mask` 中。
3. 将 Scene Exposure 与 Character Exposure 设置为明显不同的值。
4. 检查角色本体和外扩描边是否同步响应 Character Exposure。

## 4. 常见问题

- 描边仍跟随 Scene Exposure：确认材质使用的是上述 V2 Shader，并确认角色层已包含在 Character Layer Mask 中。
- 描边 Mask 不出现：确认材质已启用 `_OUTLINE_ON`，且全局角色描边开关处于开启状态。

## 5. 注意事项

- 该功能只影响 Exposure 分类，不改变描边颜色、宽度、雾效或正式描边提交顺序。
- Scene Exposure 与 Character Exposure 相同时不会生成角色 Mask，也不会执行额外描边 Mask Pass。
