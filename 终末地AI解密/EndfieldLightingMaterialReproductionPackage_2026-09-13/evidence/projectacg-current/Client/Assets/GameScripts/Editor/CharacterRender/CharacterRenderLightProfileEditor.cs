using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterRenderLightProfile))]
public sealed class CharacterRenderLightProfileEditor : OdinEditor
{
    private static readonly string[] MainLightOverrideModeLabels =
    {
        "跟随场景",
        "跟随相机",
        "自定义光照",
    };
    private static readonly string[] MonsterMainLightOverrideModeLabels =
    {
        "跟随角色",
        "跟随场景",
        "跟随相机",
        "自定义光照",
    };

    private SerializedProperty _mainLightOverrideModeProperty;
    private SerializedProperty _customLightColorProperty;
    private SerializedProperty _customLightStrengthProperty;
    private SerializedProperty _customLightDirectionProperty;
    private SerializedProperty _virtualLightCameraDirectionOffsetEulerProperty;
    private SerializedProperty _enableBackLightProperty;
    private SerializedProperty _virtualLightColorProperty;
    private SerializedProperty _virtualLightIntensityProperty;
    private SerializedProperty _virtualLightOffsetProperty;
    private SerializedProperty _enableCharacterAdditionalLightsProperty;
    private SerializedProperty _enableCharacterAdditionalLightDiffuseProperty;
    private SerializedProperty _enableCharacterAdditionalLightSpecularProperty;
    private SerializedProperty _enablePlanarShadowProperty;
    private SerializedProperty _enablePlanarShadowDirectionFollowCustomLightProperty;
    private SerializedProperty _planarShadowDirectionProperty;
    private SerializedProperty _planarShadowStrengthProperty;
    private SerializedProperty _planarShadowFalloffProperty;
    private SerializedProperty _planarShadowRangeProperty;
    private SerializedProperty _planarShadowPlaneHeightProperty;
    private SerializedProperty _planarShadowGlobalCenterProperty;
    private SerializedProperty _planarShadowColorProperty;
    private SerializedProperty _sceneShadowModeProperty;
    private SerializedProperty _enablePerObjectShadowDirectionFollowMainLightAdjustmentProperty;
    private SerializedProperty _perObjectShadowDirectionProperty;
    private SerializedProperty _selfShadowStrengthProperty;
    private SerializedProperty _environmentShadowStrengthProperty;
    private SerializedProperty _specularShadowStrengthProperty;
    private SerializedProperty _enableOutlineProperty;
    private SerializedProperty _enableRimLightProperty;
    private SerializedProperty _enableRimCustomDirectionProperty;
    private SerializedProperty _rimDirectionSpaceProperty;
    private SerializedProperty _rimDirectionProperty;
    private SerializedProperty _enableRimFakePointMaskProperty;
    private SerializedProperty _rimFakePointMaskPositionProperty;
    private SerializedProperty _rimFakePointMaskRangeProperty;
    private SerializedProperty _rimFakePointMaskPowerProperty;
    private SerializedProperty _enableRimFakeDirectMaskProperty;
    private SerializedProperty _rimFakeDirectMaskDirectionProperty;
    private SerializedProperty _rimFakeDirectMaskPositionProperty;
    private SerializedProperty _rimFakeDirectMaskRangeProperty;
    private SerializedProperty _rimIntensityProperty;
    private SerializedProperty _enableFinalColorGradientProperty;
    private SerializedProperty _finalColorGradientColorProperty;
    private SerializedProperty _finalColorGradientMinYProperty;
    private SerializedProperty _finalColorGradientMaxYProperty;
    private SerializedProperty _enableGlobalIndirectLightProperty;
    private SerializedProperty _overrideSceneAmbientProperty;
    private SerializedProperty _globalIndirectIntensityProperty;
    private SerializedProperty _globalIndirectTintColorProperty;
    private SerializedProperty _monsterMainLightOverrideModeProperty;
    private SerializedProperty _monsterCustomLightColorProperty;
    private SerializedProperty _monsterCustomLightStrengthProperty;
    private SerializedProperty _monsterCustomLightDirectionProperty;
    private SerializedProperty _monsterVirtualLightCameraDirectionOffsetEulerProperty;
    private SerializedProperty _monsterAdditionalLightsEnabledProperty;
    private SerializedProperty _monsterAdditionalLightDiffuseEnabledProperty;
    private SerializedProperty _monsterAdditionalLightSpecularEnabledProperty;
    private SerializedProperty _monsterEnvironmentFollowCharacterProperty;
    private SerializedProperty _monsterEnableGlobalIndirectLightProperty;
    private SerializedProperty _monsterOverrideSceneAmbientProperty;
    private SerializedProperty _monsterGlobalIndirectIntensityProperty;
    private SerializedProperty _monsterGlobalIndirectTintColorProperty;
    private SerializedProperty _monsterOtherFeaturesFollowCharacterProperty;
    private SerializedProperty _monsterEnableOutlineProperty;
    private SerializedProperty _monsterEnableRimLightProperty;
    private SerializedProperty _monsterEnableFinalColorGradientProperty;
    private SerializedProperty _monsterFinalColorGradientColorProperty;
    private SerializedProperty _monsterFinalColorGradientMinYProperty;
    private SerializedProperty _monsterFinalColorGradientMaxYProperty;

    private bool _showDirectLightSection = true;
    private bool _showMainLightOverrideSection = true;
    private bool _showBackLightSection;
    private bool _showPointLightSection;
    private bool _showEnvironmentSection = true;
    private bool _showShadowSection = true;
    private bool _showPlanarShadowSection;
    private bool _showSceneShadowSection;
    private bool _showOtherFeaturesSection = true;
    private bool _showOutlineSection = true;
    // 首次打开 Inspector 时默认展开边缘光，便于发现模拟点灯遮罩参数。
    private bool _showRimLightSection = true;
    private bool _showFinalColorGradientSection;
    private bool _showCharacterCategory = true;
    private bool _showMonsterCategory;
    private bool _showMonsterDirectLightSection = true;
    private bool _showMonsterMainLightSection = true;
    private bool _showMonsterPointLightSection = true;
    private bool _showMonsterEnvironmentSection = true;
    private bool _showMonsterOtherFeaturesSection = true;

    protected override void OnEnable()
    {
        base.OnEnable();
        _mainLightOverrideModeProperty = serializedObject.FindProperty("_mainLightOverrideMode");
        _customLightColorProperty = serializedObject.FindProperty("_customLightColor");
        _customLightStrengthProperty = serializedObject.FindProperty("_customLightStrength");
        _customLightDirectionProperty = serializedObject.FindProperty("_customLightDirection");
        _virtualLightCameraDirectionOffsetEulerProperty = serializedObject.FindProperty("_virtualLightCameraDirectionOffsetEuler");
        _enableBackLightProperty = serializedObject.FindProperty("_enableBackLight");
        _virtualLightColorProperty = serializedObject.FindProperty("_virtualLightColor");
        _virtualLightIntensityProperty = serializedObject.FindProperty("_virtualLightIntensity");
        _virtualLightOffsetProperty = serializedObject.FindProperty("_virtualLightOffset");
        _enableCharacterAdditionalLightsProperty = serializedObject.FindProperty("_enableCharacterAdditionalLights");
        _enableCharacterAdditionalLightDiffuseProperty = serializedObject.FindProperty("_enableCharacterAdditionalLightDiffuse");
        _enableCharacterAdditionalLightSpecularProperty = serializedObject.FindProperty("_enableCharacterAdditionalLightSpecular");
        _enablePlanarShadowProperty = serializedObject.FindProperty("_enablePlanarShadow");
        _enablePlanarShadowDirectionFollowCustomLightProperty = serializedObject.FindProperty("_enablePlanarShadowDirectionFollowCustomLight");
        _planarShadowDirectionProperty = serializedObject.FindProperty("_planarShadowDirection");
        _planarShadowStrengthProperty = serializedObject.FindProperty("_planarShadowStrength");
        _planarShadowFalloffProperty = serializedObject.FindProperty("_planarShadowFalloff");
        _planarShadowRangeProperty = serializedObject.FindProperty("_planarShadowRange");
        _planarShadowPlaneHeightProperty = serializedObject.FindProperty("_planarShadowPlaneHeight");
        _planarShadowGlobalCenterProperty = serializedObject.FindProperty("_planarShadowGlobalCenter");
        _planarShadowColorProperty = serializedObject.FindProperty("_planarShadowColor");
        _sceneShadowModeProperty = serializedObject.FindProperty("_sceneShadowMode");
        _enablePerObjectShadowDirectionFollowMainLightAdjustmentProperty = serializedObject.FindProperty("_enablePerObjectShadowDirectionFollowMainLightAdjustment");
        _perObjectShadowDirectionProperty = serializedObject.FindProperty("_perObjectShadowDirection");
        _selfShadowStrengthProperty = serializedObject.FindProperty("_selfShadowStrength");
        _environmentShadowStrengthProperty = serializedObject.FindProperty("_environmentShadowStrength");
        _specularShadowStrengthProperty = serializedObject.FindProperty("_specularShadowStrength");
        _enableOutlineProperty = serializedObject.FindProperty("_enableOutline");
        _enableRimLightProperty = serializedObject.FindProperty("_enableRimLight");
        _enableRimCustomDirectionProperty = serializedObject.FindProperty("_enableRimCustomDirection");
        _rimDirectionSpaceProperty = serializedObject.FindProperty("_rimDirectionSpace");
        _rimDirectionProperty = serializedObject.FindProperty("_rimDirection");
        _enableRimFakePointMaskProperty = serializedObject.FindProperty("_enableRimFakePointMask");
        _rimFakePointMaskPositionProperty = serializedObject.FindProperty("_rimFakePointMaskPosition");
        _rimFakePointMaskRangeProperty = serializedObject.FindProperty("_rimFakePointMaskRange");
        _rimFakePointMaskPowerProperty = serializedObject.FindProperty("_rimFakePointMaskPower");
        _enableRimFakeDirectMaskProperty = serializedObject.FindProperty("_enableRimFakeDirectMask");
        _rimFakeDirectMaskDirectionProperty = serializedObject.FindProperty("_rimFakeDirectMaskDirection");
        _rimFakeDirectMaskPositionProperty = serializedObject.FindProperty("_rimFakeDirectMaskPosition");
        _rimFakeDirectMaskRangeProperty = serializedObject.FindProperty("_rimFakeDirectMaskRange");
        _rimIntensityProperty = serializedObject.FindProperty("_rimIntensity");
        _enableFinalColorGradientProperty = serializedObject.FindProperty("_enableFinalColorGradient");
        _finalColorGradientColorProperty = serializedObject.FindProperty("_finalColorGradientColor");
        _finalColorGradientMinYProperty = serializedObject.FindProperty("_finalColorGradientMinY");
        _finalColorGradientMaxYProperty = serializedObject.FindProperty("_finalColorGradientMaxY");
        _enableGlobalIndirectLightProperty = serializedObject.FindProperty("_enableGlobalIndirectLight");
        _overrideSceneAmbientProperty = serializedObject.FindProperty("_overrideSceneAmbient");
        _globalIndirectIntensityProperty = serializedObject.FindProperty("_globalIndirectIntensity");
        _globalIndirectTintColorProperty = serializedObject.FindProperty("_globalIndirectTintColor");
        _monsterMainLightOverrideModeProperty = serializedObject.FindProperty("_monsterMainLightOverrideMode");
        _monsterCustomLightColorProperty = serializedObject.FindProperty("_monsterCustomLightColor");
        _monsterCustomLightStrengthProperty = serializedObject.FindProperty("_monsterCustomLightStrength");
        _monsterCustomLightDirectionProperty = serializedObject.FindProperty("_monsterCustomLightDirection");
        _monsterVirtualLightCameraDirectionOffsetEulerProperty = serializedObject.FindProperty("_monsterVirtualLightCameraDirectionOffsetEuler");
        _monsterAdditionalLightsEnabledProperty = serializedObject.FindProperty("_monsterAdditionalLightsEnabled");
        _monsterAdditionalLightDiffuseEnabledProperty = serializedObject.FindProperty("_monsterAdditionalLightDiffuseEnabled");
        _monsterAdditionalLightSpecularEnabledProperty = serializedObject.FindProperty("_monsterAdditionalLightSpecularEnabled");
        _monsterEnvironmentFollowCharacterProperty = serializedObject.FindProperty("_monsterEnvironmentFollowCharacter");
        _monsterEnableGlobalIndirectLightProperty = serializedObject.FindProperty("_monsterEnableGlobalIndirectLight");
        _monsterOverrideSceneAmbientProperty = serializedObject.FindProperty("_monsterOverrideSceneAmbient");
        _monsterGlobalIndirectIntensityProperty = serializedObject.FindProperty("_monsterGlobalIndirectIntensity");
        _monsterGlobalIndirectTintColorProperty = serializedObject.FindProperty("_monsterGlobalIndirectTintColor");
        _monsterOtherFeaturesFollowCharacterProperty = serializedObject.FindProperty("_monsterOtherFeaturesFollowCharacter");
        _monsterEnableOutlineProperty = serializedObject.FindProperty("_monsterEnableOutline");
        _monsterEnableRimLightProperty = serializedObject.FindProperty("_monsterEnableRimLight");
        _monsterEnableFinalColorGradientProperty = serializedObject.FindProperty("_monsterEnableFinalColorGradient");
        _monsterFinalColorGradientColorProperty = serializedObject.FindProperty("_monsterFinalColorGradientColor");
        _monsterFinalColorGradientMinYProperty = serializedObject.FindProperty("_monsterFinalColorGradientMinY");
        _monsterFinalColorGradientMaxYProperty = serializedObject.FindProperty("_monsterFinalColorGradientMaxY");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("角色渲染配置", EditorStyles.boldLabel);
        DrawParameterClipboardActions((CharacterRenderLightProfile)target);

        using (new EditorGUI.IndentLevelScope())
        {
            _showCharacterCategory = DrawCategoryFoldout(_showCharacterCategory, "角色");
            if (_showCharacterCategory)
            {
                DrawDirectLightSection();
                DrawEnvironmentSection();
                DrawShadowSection();
                DrawOtherFeaturesSection();
            }

            _showMonsterCategory = DrawCategoryFoldout(_showMonsterCategory, "怪物");
            if (_showMonsterCategory)
            {
                DrawMonsterDirectLightSection();
                DrawMonsterEnvironmentSection();
                DrawMonsterOtherFeaturesSection();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "修改配置资产会自动同步到引用它的 Character Render Controller。控制器关闭或不存在时，场景阴影、边缘光和最终颜色渐变会回退到材质原有设置。",
            MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawParameterClipboardActions(CharacterRenderLightProfile profile)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledGroupScope(profile == null))
            {
                if (GUILayout.Button("复制配置参数"))
                {
                    GUI.FocusControl(null);
                    CharacterRenderLightProfileClipboard.Copy(profile);
                }
            }

            using (new EditorGUI.DisabledGroupScope(
                profile == null || !CharacterRenderLightProfileClipboard.HasParameters))
            {
                if (GUILayout.Button("粘贴配置参数"))
                {
                    GUI.FocusControl(null);
                    if (CharacterRenderLightProfileClipboard.TryPaste(profile, out string errorMessage))
                    {
                        serializedObject.UpdateIfRequiredOrScript();
                        Repaint();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("粘贴角色渲染配置参数", errorMessage, "确定");
                    }
                }
            }
        }

        EditorGUILayout.Space();
    }

    private void DrawDirectLightSection()
    {
        _showDirectLightSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showDirectLightSection, "直接光设置");
        if (_showDirectLightSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _showMainLightOverrideSection = DrawSubFoldout(_showMainLightOverrideSection, "主光覆盖");
                if (_showMainLightOverrideSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        _mainLightOverrideModeProperty.enumValueIndex = EditorGUILayout.Popup(
                            "模式",
                            _mainLightOverrideModeProperty.enumValueIndex,
                            MainLightOverrideModeLabels);
                        CharacterRenderMainLightOverrideMode mode =
                            (CharacterRenderMainLightOverrideMode)_mainLightOverrideModeProperty.enumValueIndex;
                        if (mode != CharacterRenderMainLightOverrideMode.Off)
                        {
                            EditorGUILayout.PropertyField(_customLightColorProperty, new GUIContent("颜色"));
                            EditorGUILayout.Slider(_customLightStrengthProperty, 0f, 8f, new GUIContent("强度"));
                        }

                        if (mode == CharacterRenderMainLightOverrideMode.FollowCamera)
                        {
                            EditorGUILayout.PropertyField(_virtualLightCameraDirectionOffsetEulerProperty, new GUIContent("额外方向偏移"));
                        }
                        else if (mode == CharacterRenderMainLightOverrideMode.Custom)
                        {
                            EditorGUILayout.PropertyField(_customLightDirectionProperty, new GUIContent("方向"));
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("关闭时跟随场景 Directional Light。", MessageType.None);
                        }
                    }
                }

                EditorGUILayout.Space(2f);
                _showBackLightSection = DrawSubFoldout(_showBackLightSection, "背光补偿");
                if (_showBackLightSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_enableBackLightProperty, new GUIContent("启用"));
                        using (new EditorGUI.DisabledGroupScope(!_enableBackLightProperty.boolValue))
                        {
                            EditorGUILayout.PropertyField(_virtualLightColorProperty, new GUIContent("颜色"));
                            EditorGUILayout.Slider(_virtualLightIntensityProperty, 0f, 2f, new GUIContent("强度"));
                            EditorGUILayout.Slider(_virtualLightOffsetProperty, -1f, 1f, new GUIContent("兰伯特偏移"));
                        }
                    }
                }

                EditorGUILayout.Space(2f);
                _showPointLightSection = DrawSubFoldout(_showPointLightSection, "点光控制");
                if (_showPointLightSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_enableCharacterAdditionalLightsProperty, new GUIContent("启用角色点光"));
                        using (new EditorGUI.DisabledGroupScope(!_enableCharacterAdditionalLightsProperty.boolValue))
                        {
                            EditorGUILayout.PropertyField(
                                _enableCharacterAdditionalLightDiffuseProperty,
                                new GUIContent("启用点光漫反射"));
                            EditorGUILayout.PropertyField(
                                _enableCharacterAdditionalLightSpecularProperty,
                                new GUIContent("启用点光高光"));
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawEnvironmentSection()
    {
        _showEnvironmentSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showEnvironmentSection, "环境光设置");
        if (_showEnvironmentSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_enableGlobalIndirectLightProperty, new GUIContent("启用"));
                using (new EditorGUI.DisabledGroupScope(!_enableGlobalIndirectLightProperty.boolValue))
                {
                    EditorGUILayout.PropertyField(
                        _overrideSceneAmbientProperty,
                        new GUIContent(
                            "覆盖场景环境光",
                            "开启后忽略场景球谐颜色，使用下方“间接漫反射颜色”作为角色环境光颜色。"));
                    EditorGUILayout.Slider(_globalIndirectIntensityProperty, 0f, 8f, new GUIContent("间接漫反射强度"));
                    EditorGUILayout.PropertyField(_globalIndirectTintColorProperty, new GUIContent("间接漫反射颜色"));
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawShadowSection()
    {
        _showShadowSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showShadowSection, "阴影设置");
        if (_showShadowSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _showPlanarShadowSection = DrawSubFoldout(_showPlanarShadowSection, "平面阴影");
                if (_showPlanarShadowSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_enablePlanarShadowProperty, new GUIContent("开启平面阴影"));
                        using (new EditorGUI.DisabledGroupScope(!_enablePlanarShadowProperty.boolValue))
                        {
                            CharacterRenderMainLightOverrideMode mainMode =
                                (CharacterRenderMainLightOverrideMode)_mainLightOverrideModeProperty.enumValueIndex;
                            bool canFollowMainLightOverride = mainMode != CharacterRenderMainLightOverrideMode.Off;
                            if (canFollowMainLightOverride)
                            {
                                EditorGUILayout.PropertyField(
                                    _enablePlanarShadowDirectionFollowCustomLightProperty,
                                    new GUIContent("跟随主光覆盖方向"));
                            }
                            using (new EditorGUI.DisabledGroupScope(
                                canFollowMainLightOverride && _enablePlanarShadowDirectionFollowCustomLightProperty.boolValue))
                            {
                                EditorGUILayout.PropertyField(_planarShadowDirectionProperty, new GUIContent("自定义方向"));
                            }
                            EditorGUILayout.Slider(_planarShadowStrengthProperty, 0f, 4f, new GUIContent("强度"));
                            EditorGUILayout.PropertyField(_planarShadowFalloffProperty, new GUIContent("衰减"));
                            EditorGUILayout.PropertyField(_planarShadowRangeProperty, new GUIContent("范围"));
                            EditorGUILayout.PropertyField(_planarShadowPlaneHeightProperty, new GUIContent("平面高度（世界 Y）"));
                            EditorGUILayout.PropertyField(_planarShadowGlobalCenterProperty, new GUIContent("全局中心（世界坐标）"));
                            EditorGUILayout.PropertyField(_planarShadowColorProperty, new GUIContent("颜色"));
                        }
                    }
                }

                EditorGUILayout.Space(2f);
                _showSceneShadowSection = DrawSubFoldout(_showSceneShadowSection, "场景阴影");
                if (_showSceneShadowSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_sceneShadowModeProperty, new GUIContent("模式"));
                        CharacterRenderSceneShadowMode sceneMode =
                            (CharacterRenderSceneShadowMode)_sceneShadowModeProperty.enumValueIndex;
                        bool usesPos = sceneMode == CharacterRenderSceneShadowMode.POSOnly
                            || sceneMode == CharacterRenderSceneShadowMode.Both;
                        if (usesPos)
                        {
                            CharacterRenderMainLightOverrideMode mainMode =
                                (CharacterRenderMainLightOverrideMode)_mainLightOverrideModeProperty.enumValueIndex;
                            bool canFollowMainLightOverride = mainMode != CharacterRenderMainLightOverrideMode.Off;
                            if (canFollowMainLightOverride)
                            {
                                EditorGUILayout.PropertyField(
                                    _enablePerObjectShadowDirectionFollowMainLightAdjustmentProperty,
                                    new GUIContent("跟随主光覆盖方向"));
                            }
                            using (new EditorGUI.DisabledGroupScope(
                                canFollowMainLightOverride
                                && _enablePerObjectShadowDirectionFollowMainLightAdjustmentProperty.boolValue))
                            {
                                EditorGUILayout.PropertyField(_perObjectShadowDirectionProperty, new GUIContent("POS 自定义方向"));
                            }
                            EditorGUILayout.Slider(
                                _selfShadowStrengthProperty,
                                0f,
                                2f,
                                new GUIContent("Self Shadow 阴影强度"));
                            EditorGUILayout.Slider(
                                _environmentShadowStrengthProperty,
                                0f,
                                2f,
                                new GUIContent("Environment Shadow 阴影强度"));
                            EditorGUILayout.Slider(
                                _specularShadowStrengthProperty,
                                0f,
                                1f,
                                new GUIContent("高光阴影作用强度"));
                        }
                        EditorGUILayout.HelpBox(
                            "0 Off / 1 UnityOnly / 2 POSOnly / 3 Both；选择 POSOnly 或 Both 时会自动启用 POS。",
                            MessageType.None);
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawOtherFeaturesSection()
    {
        _showOtherFeaturesSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showOtherFeaturesSection, "其他特性设置");
        if (_showOtherFeaturesSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _showOutlineSection = DrawSubFoldout(_showOutlineSection, "描边");
                if (_showOutlineSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_enableOutlineProperty, new GUIContent("开启描边"));
                    }
                }

                EditorGUILayout.Space(2f);
                _showRimLightSection = DrawSubFoldout(_showRimLightSection, "边缘光");
                if (_showRimLightSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_enableRimLightProperty, new GUIContent("开启边缘光"));
                        EditorGUILayout.Slider(_rimIntensityProperty, 0f, 8f, new GUIContent("全局强度"));
                        EditorGUILayout.PropertyField(_enableRimCustomDirectionProperty, new GUIContent("自定义方向"));
                        using (new EditorGUI.DisabledGroupScope(!_enableRimCustomDirectionProperty.boolValue))
                        {
                            EditorGUILayout.PropertyField(_rimDirectionSpaceProperty, new GUIContent("方向空间"));
                            EditorGUILayout.PropertyField(_rimDirectionProperty, new GUIContent("方向"));
                        }
                        EditorGUILayout.PropertyField(_enableRimFakePointMaskProperty, new GUIContent("启用模拟点灯遮罩"));
                        using (new EditorGUI.DisabledGroupScope(!_enableRimFakePointMaskProperty.boolValue))
                        {
                            EditorGUILayout.PropertyField(_rimFakePointMaskPositionProperty, new GUIContent("点灯位置偏移"));
                            EditorGUILayout.PropertyField(_rimFakePointMaskRangeProperty, new GUIContent("点灯范围"));
                            EditorGUILayout.Slider(_rimFakePointMaskPowerProperty, 0.25f, 8f, new GUIContent("模拟点灯边缘软硬"));
                        }
                        EditorGUILayout.PropertyField(_enableRimFakeDirectMaskProperty, new GUIContent("启用模拟直接光遮罩"));
                        using (new EditorGUI.DisabledGroupScope(!_enableRimFakeDirectMaskProperty.boolValue))
                        {
                            EditorGUILayout.PropertyField(_rimFakeDirectMaskDirectionProperty, new GUIContent("直接光切面方向"));
                            EditorGUILayout.PropertyField(_rimFakeDirectMaskPositionProperty, new GUIContent("直接光切面位置偏移"));
                            EditorGUILayout.PropertyField(_rimFakeDirectMaskRangeProperty, new GUIContent("直接光范围"));
                        }
                        EditorGUILayout.HelpBox("该开关只门控材质已经编译并启用的 Rim Variant。", MessageType.None);
                    }
                }

                EditorGUILayout.Space(2f);
                _showFinalColorGradientSection = DrawSubFoldout(_showFinalColorGradientSection, "最终颜色渐变");
                if (_showFinalColorGradientSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_enableFinalColorGradientProperty, new GUIContent("开启最终颜色渐变"));
                        using (new EditorGUI.DisabledGroupScope(!_enableFinalColorGradientProperty.boolValue))
                        {
                            EditorGUILayout.PropertyField(_finalColorGradientColorProperty, new GUIContent("渐变颜色"));
                            EditorGUILayout.PropertyField(_finalColorGradientMinYProperty, new GUIContent("渐变最小 Y"));
                            EditorGUILayout.PropertyField(_finalColorGradientMaxYProperty, new GUIContent("渐变最大 Y"));
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private static bool DrawCategoryFoldout(bool expanded, string title)
    {
        EditorGUILayout.Space(8f);
        GUIStyle style = new GUIStyle(EditorStyles.foldoutHeader)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
        };
        expanded = EditorGUILayout.Foldout(expanded, title, true, style);
        EditorGUILayout.Space(2f);
        return expanded;
    }

    private void DrawMonsterDirectLightSection()
    {
        _showMonsterDirectLightSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showMonsterDirectLightSection, "直接光设置");
        if (_showMonsterDirectLightSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _showMonsterMainLightSection = DrawSubFoldout(_showMonsterMainLightSection, "主光覆盖");
                if (_showMonsterMainLightSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        _monsterMainLightOverrideModeProperty.enumValueIndex = EditorGUILayout.Popup(
                            "模式", _monsterMainLightOverrideModeProperty.enumValueIndex, MonsterMainLightOverrideModeLabels);
                        MonsterRenderMainLightOverrideMode mode =
                            (MonsterRenderMainLightOverrideMode)_monsterMainLightOverrideModeProperty.enumValueIndex;
                        if (mode != MonsterRenderMainLightOverrideMode.FollowCharacter
                            && mode != MonsterRenderMainLightOverrideMode.Off)
                        {
                            EditorGUILayout.PropertyField(_monsterCustomLightColorProperty, new GUIContent("颜色"));
                            EditorGUILayout.Slider(_monsterCustomLightStrengthProperty, 0f, 8f, new GUIContent("强度"));
                        }
                        if (mode == MonsterRenderMainLightOverrideMode.FollowCamera)
                        {
                            EditorGUILayout.PropertyField(_monsterVirtualLightCameraDirectionOffsetEulerProperty, new GUIContent("额外方向偏移"));
                        }
                        else if (mode == MonsterRenderMainLightOverrideMode.Custom)
                        {
                            EditorGUILayout.PropertyField(_monsterCustomLightDirectionProperty, new GUIContent("方向"));
                        }
                        else if (mode == MonsterRenderMainLightOverrideMode.FollowCharacter)
                        {
                            EditorGUILayout.HelpBox("主光覆盖的模式、颜色、强度和方向全部跟随角色。", MessageType.None);
                        }
                    }
                }

                EditorGUILayout.Space(2f);
                _showMonsterPointLightSection = DrawSubFoldout(_showMonsterPointLightSection, "点光控制");
                if (_showMonsterPointLightSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_monsterAdditionalLightsEnabledProperty, new GUIContent("启用怪物点光"));
                        using (new EditorGUI.DisabledGroupScope(!_monsterAdditionalLightsEnabledProperty.boolValue))
                        {
                            EditorGUILayout.PropertyField(_monsterAdditionalLightDiffuseEnabledProperty, new GUIContent("启用点光漫反射"));
                            EditorGUILayout.PropertyField(_monsterAdditionalLightSpecularEnabledProperty, new GUIContent("启用点光高光"));
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawMonsterEnvironmentSection()
    {
        _showMonsterEnvironmentSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showMonsterEnvironmentSection, "环境光设置");
        if (_showMonsterEnvironmentSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_monsterEnvironmentFollowCharacterProperty, new GUIContent("跟随角色"));
                if (!_monsterEnvironmentFollowCharacterProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(_monsterEnableGlobalIndirectLightProperty, new GUIContent("启用"));
                    using (new EditorGUI.DisabledGroupScope(!_monsterEnableGlobalIndirectLightProperty.boolValue))
                    {
                        EditorGUILayout.PropertyField(_monsterOverrideSceneAmbientProperty, new GUIContent("覆盖场景环境光"));
                        EditorGUILayout.Slider(_monsterGlobalIndirectIntensityProperty, 0f, 8f, new GUIContent("间接漫反射强度"));
                        EditorGUILayout.PropertyField(_monsterGlobalIndirectTintColorProperty, new GUIContent("间接漫反射颜色"));
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawMonsterOtherFeaturesSection()
    {
        _showMonsterOtherFeaturesSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showMonsterOtherFeaturesSection, "其他特性设置");
        if (_showMonsterOtherFeaturesSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_monsterOtherFeaturesFollowCharacterProperty, new GUIContent("跟随角色"));
                if (!_monsterOtherFeaturesFollowCharacterProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(_monsterEnableOutlineProperty, new GUIContent("开启描边"));
                    EditorGUILayout.PropertyField(_monsterEnableRimLightProperty, new GUIContent("开启边缘光"));
                    EditorGUILayout.PropertyField(_monsterEnableFinalColorGradientProperty, new GUIContent("开启最终颜色渐变"));
                    if (_monsterEnableFinalColorGradientProperty.boolValue)
                    {
                        EditorGUILayout.PropertyField(_monsterFinalColorGradientColorProperty, new GUIContent("渐变颜色"));
                        EditorGUILayout.PropertyField(_monsterFinalColorGradientMinYProperty, new GUIContent("渐变最小 Y"));
                        EditorGUILayout.PropertyField(_monsterFinalColorGradientMaxYProperty, new GUIContent("渐变最大 Y"));
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private static bool DrawSubFoldout(bool value, string label) =>
        EditorGUILayout.Foldout(value, label, true, EditorStyles.foldoutHeader);
}

internal static class CharacterRenderLightProfileClipboard
{
    private const string ClipboardPrefix = "ProjectACG.CharacterRenderLightProfile.Parameters.v1\n";

    public static bool HasParameters
    {
        get
        {
            string clipboardText = EditorGUIUtility.systemCopyBuffer;
            return !string.IsNullOrEmpty(clipboardText)
                && clipboardText.StartsWith(ClipboardPrefix, StringComparison.Ordinal);
        }
    }

    public static void Copy(CharacterRenderLightProfile sourceProfile)
    {
        if (sourceProfile == null)
        {
            return;
        }

        EditorGUIUtility.systemCopyBuffer = ClipboardPrefix + EditorJsonUtility.ToJson(sourceProfile);
    }

    public static bool TryPaste(CharacterRenderLightProfile targetProfile, out string errorMessage)
    {
        if (targetProfile == null)
        {
            errorMessage = "目标配置文件为空。";
            return false;
        }

        string clipboardText = EditorGUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(clipboardText)
            || !clipboardText.StartsWith(ClipboardPrefix, StringComparison.Ordinal))
        {
            errorMessage = "剪贴板中没有有效的角色渲染配置参数。";
            return false;
        }

        string parameterJson = clipboardText.Substring(ClipboardPrefix.Length);
        string originalJson = EditorJsonUtility.ToJson(targetProfile);
        string originalName = targetProfile.name;
        HideFlags originalHideFlags = targetProfile.hideFlags;

        Undo.RecordObject(targetProfile, "粘贴角色渲染配置参数");
        try
        {
            EditorJsonUtility.FromJsonOverwrite(parameterJson, targetProfile);
        }
        catch (Exception exception)
        {
            EditorJsonUtility.FromJsonOverwrite(originalJson, targetProfile);
            targetProfile.name = originalName;
            targetProfile.hideFlags = originalHideFlags;
            errorMessage = $"配置参数格式无效：{exception.Message}";
            return false;
        }

        // 只复制配置字段，目标资产本身的名称和对象标记保持不变。
        targetProfile.name = originalName;
        targetProfile.hideFlags = originalHideFlags;
        EditorUtility.SetDirty(targetProfile);
        targetProfile.NotifyChangedInEditor();
        errorMessage = string.Empty;
        return true;
    }
}
