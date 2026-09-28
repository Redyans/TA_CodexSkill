using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterMainLightController))]
public sealed class CharacterMainLightControllerEditor : OdinEditor
{
    private const string DefaultProfileName = "CharacterMainLightProfile";
    private const string ProfileAssetDirectory = "Assets/GameScripts/AOT/GameArt/CharacterMainlight";

    private SerializedProperty _profileProperty;
    private SerializedProperty _activeConfigurationIndexProperty;
    private SerializedProperty _configurationNameProperty;
    private SerializedProperty _useCameraDirectionProperty;
    private SerializedProperty _targetCameraProperty;
    private SerializedProperty _lightRotationOffsetEulerProperty;
    private SerializedProperty _mainLightColorProperty;
    private SerializedProperty _mainLightIntensityProperty;
    private SerializedProperty _mainLightDirectionProperty;
    private SerializedProperty _enableCustomMainLightOnMaterialsProperty;
    private SerializedProperty _enableMonsterCustomMainLightOnMaterialsProperty;
    private SerializedProperty _enableGunCustomMainLightOnMaterialsProperty;
    private SerializedProperty _enableAdditionalLightsProperty;
    private SerializedProperty _sceneShadowModeProperty;
    private SerializedProperty _enableCharacterSceneShadowProperty;
    private SerializedProperty _enableHairFringeSelfShadowProperty;
    private SerializedProperty _enableMonsterSceneShadowProperty;
    private SerializedProperty _enableGunSceneShadowProperty;
    private SerializedProperty _overridePerObjectShadowDirectionProperty;
    private SerializedProperty _perObjectShadowDirectionProperty;
    private SerializedProperty _showPerObjectShadowSceneDirectionProperty;
    private SerializedProperty _enablePlanarShadowProperty;
    private SerializedProperty _enableCharacterPlanarShadowProperty;
    private SerializedProperty _enableMonsterPlanarShadowProperty;
    private SerializedProperty _enableGunPlanarShadowProperty;
    private SerializedProperty _planarShadowFalloffProperty;
    private SerializedProperty _planarShadowRangeProperty;
    private SerializedProperty _planarShadowLightDirectionProperty;
    private SerializedProperty _usePlanarShadowCameraFacingDirectionProperty;
    private SerializedProperty _planarShadowTargetCameraProperty;
    private SerializedProperty _showPlanarShadowSceneDirectionProperty;
    private SerializedProperty _planarShadowPlaneHeightProperty;
    private SerializedProperty _usePlanarShadowBoundsCenterProperty;
    private SerializedProperty _planarShadowColorProperty;
    private SerializedProperty _enableCustomEnvironmentLightOnMaterialsProperty;
    private SerializedProperty _enableMonsterCustomEnvironmentLightOnMaterialsProperty;
    private SerializedProperty _enableGunCustomEnvironmentLightOnMaterialsProperty;
    private SerializedProperty _customEnvironmentColorProperty;
    private SerializedProperty _customEnvironmentLightBlendProperty;
    private SerializedProperty _indirectSpecularIntensityProperty;
    private SerializedProperty _showSceneDirectionProperty;

    private Quaternion _directionRotation = Quaternion.identity;
    private Quaternion _perObjectShadowDirectionRotation = Quaternion.identity;
    private Quaternion _planarShadowDirectionRotation = Quaternion.identity;
    private bool _suppressAutoOverwriteOnce;
    private string _configurationNameInput = string.Empty;

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheProperties();
        AssignDefaultProfileIfMissing();
        SyncConfigurationNameInputFromProperty();
    }

    public override void OnInspectorGUI()
    {
        CharacterMainLightController controller = (CharacterMainLightController)target;
        serializedObject.Update();
        if (_configurationNameProperty != null
            && string.IsNullOrEmpty(_configurationNameInput))
        {
            SyncConfigurationNameInputFromProperty();
        }

        Object previousProfile = _profileProperty != null ? _profileProperty.objectReferenceValue : null;
        EditorGUI.BeginChangeCheck();

        DrawConfigurationSettings();
        DrawCustomDirectLightSettings();
        DrawCustomEnvironmentLightSettings();
        DrawCustomShadowSettings();

        serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck())
        {
            bool profileChanged = _profileProperty != null && !ReferenceEquals(previousProfile, _profileProperty.objectReferenceValue);
            if (profileChanged && controller.Profile != null)
            {
                _suppressAutoOverwriteOnce = true;
                controller.ApplyConfiguration();
                EditorUtility.SetDirty(controller);
                RepaintAfterConfigurationChange();
                return;
            }

            if (_suppressAutoOverwriteOnce)
            {
                _suppressAutoOverwriteOnce = false;
            }
            else
            {
                TryAutoOverwriteConfiguration(controller);
            }
        }
    }

    private void OnSceneGUI()
    {
        CharacterMainLightController controller = (CharacterMainLightController)target;
        if (controller == null)
        {
            return;
        }

        if (controller.EnablePlanarShadow && controller.ShowPlanarShadowSceneDirection && !controller.UsesPlanarShadowCameraFacingDirection)
        {
            Transform planarTargetTransform = controller.transform;
            Vector3 planarOrigin = planarTargetTransform.position;
            Vector3 planarWorldDirection = controller.GetPlanarShadowWorldDirection();
            float planarHandleLength = controller.PlanarShadowSceneHandleLength;
            Vector3 planarTip = planarOrigin + planarWorldDirection * planarHandleLength;

            if (_planarShadowDirectionRotation == Quaternion.identity)
            {
                _planarShadowDirectionRotation = Quaternion.FromToRotation(Vector3.forward, planarWorldDirection);
            }

            using (new Handles.DrawingScope(controller.PlanarShadowSceneHandleColor))
            {
                Handles.DrawAAPolyLine(3f, planarOrigin, planarTip);
                Handles.ConeHandleCap(
                    0,
                    planarTip,
                    Quaternion.LookRotation(planarWorldDirection),
                    HandleUtility.GetHandleSize(planarTip) * 0.18f,
                    EventType.Repaint);

                EditorGUI.BeginChangeCheck();
                Quaternion newPlanarRotation = Handles.RotationHandle(_planarShadowDirectionRotation, planarOrigin);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "调整平面阴影方向");
                    _planarShadowDirectionRotation = newPlanarRotation;
                    controller.SetPlanarShadowWorldDirection(_planarShadowDirectionRotation * Vector3.forward);
                    EditorUtility.SetDirty(controller);
                    serializedObject.UpdateIfRequiredOrScript();
                }
            }
        }

        if (controller.OverridePerObjectShadowDirection && controller.ShowPerObjectShadowSceneDirection)
        {
            Transform shadowTargetTransform = controller.transform;
            Vector3 shadowOrigin = shadowTargetTransform.position;
            Vector3 shadowWorldDirection = controller.GetPerObjectShadowWorldDirection();
            float shadowHandleLength = controller.PerObjectShadowSceneHandleLength;
            Vector3 shadowTip = shadowOrigin + shadowWorldDirection * shadowHandleLength;

            if (_perObjectShadowDirectionRotation == Quaternion.identity)
            {
                _perObjectShadowDirectionRotation = Quaternion.FromToRotation(Vector3.forward, shadowWorldDirection);
            }

            using (new Handles.DrawingScope(controller.PerObjectShadowSceneHandleColor))
            {
                Handles.DrawAAPolyLine(3f, shadowOrigin, shadowTip);
                Handles.ConeHandleCap(
                    0,
                    shadowTip,
                    Quaternion.LookRotation(shadowWorldDirection),
                    HandleUtility.GetHandleSize(shadowTip) * 0.18f,
                    EventType.Repaint);

                EditorGUI.BeginChangeCheck();
                Quaternion newShadowRotation = Handles.RotationHandle(_perObjectShadowDirectionRotation, shadowOrigin);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "调整 Per Object Shadow 方向");
                    _perObjectShadowDirectionRotation = newShadowRotation;
                    controller.SetPerObjectShadowWorldDirection(_perObjectShadowDirectionRotation * Vector3.forward);
                    EditorUtility.SetDirty(controller);
                    serializedObject.UpdateIfRequiredOrScript();
                }
            }
        }

        if (!controller.ShowSceneDirection || controller.UsesCameraDirection)
        {
            return;
        }

        Transform targetTransform = controller.transform;
        Vector3 origin = targetTransform.position;
        Vector3 worldDirection = controller.GetWorldDirection();
        float handleLength = controller.SceneHandleLength;
        Vector3 tip = origin + worldDirection * handleLength;

        if (_directionRotation == Quaternion.identity)
        {
            _directionRotation = Quaternion.FromToRotation(Vector3.forward, worldDirection);
        }

        using (new Handles.DrawingScope(controller.SceneHandleColor))
        {
            Handles.DrawAAPolyLine(3f, origin, tip);
            Handles.ConeHandleCap(
                0,
                tip,
                Quaternion.LookRotation(worldDirection),
                HandleUtility.GetHandleSize(tip) * 0.18f,
                EventType.Repaint);

            EditorGUI.BeginChangeCheck();
            Quaternion newRotation = Handles.RotationHandle(_directionRotation, origin);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(controller, "调整角色主光方向");
                _directionRotation = newRotation;
                controller.SetWorldDirection(_directionRotation * Vector3.forward);
                EditorUtility.SetDirty(controller);
                serializedObject.UpdateIfRequiredOrScript();
            }
        }
    }

    private void CacheProperties()
    {
        _profileProperty = serializedObject.FindProperty("_profile");
        _activeConfigurationIndexProperty = serializedObject.FindProperty("_activeConfigurationIndex");
        _configurationNameProperty = serializedObject.FindProperty("_configurationName");
        _useCameraDirectionProperty = serializedObject.FindProperty("_useCameraDirection");
        _targetCameraProperty = serializedObject.FindProperty("_targetCamera");
        _lightRotationOffsetEulerProperty = serializedObject.FindProperty("_lightRotationOffsetEuler");
        _mainLightColorProperty = serializedObject.FindProperty("_mainLightColor");
        _mainLightIntensityProperty = serializedObject.FindProperty("_mainLightIntensity");
        _mainLightDirectionProperty = serializedObject.FindProperty("_mainLightDirection");
        _enableCustomMainLightOnMaterialsProperty = serializedObject.FindProperty("_enableCustomMainLightOnMaterials");
        _enableMonsterCustomMainLightOnMaterialsProperty = serializedObject.FindProperty("_enableMonsterCustomMainLightOnMaterials");
        _enableGunCustomMainLightOnMaterialsProperty = serializedObject.FindProperty("_enableGunCustomMainLightOnMaterials");
        _enableAdditionalLightsProperty = serializedObject.FindProperty("_enableAdditionalLights");
        _sceneShadowModeProperty = serializedObject.FindProperty("_sceneShadowMode");
        _enableCharacterSceneShadowProperty = serializedObject.FindProperty("_enableCharacterSceneShadow");
        _enableHairFringeSelfShadowProperty = serializedObject.FindProperty("_enableHairFringeSelfShadow");
        _enableMonsterSceneShadowProperty = serializedObject.FindProperty("_enableMonsterSceneShadow");
        _enableGunSceneShadowProperty = serializedObject.FindProperty("_enableGunSceneShadow");
        _overridePerObjectShadowDirectionProperty = serializedObject.FindProperty("_overridePerObjectShadowDirection");
        _perObjectShadowDirectionProperty = serializedObject.FindProperty("_perObjectShadowDirection");
        _showPerObjectShadowSceneDirectionProperty = serializedObject.FindProperty("_showPerObjectShadowSceneDirection");
        _enablePlanarShadowProperty = serializedObject.FindProperty("_enablePlanarShadow");
        _enableCharacterPlanarShadowProperty = serializedObject.FindProperty("_enableCharacterPlanarShadow");
        _enableMonsterPlanarShadowProperty = serializedObject.FindProperty("_enableMonsterPlanarShadow");
        _enableGunPlanarShadowProperty = serializedObject.FindProperty("_enableGunPlanarShadow");
        _planarShadowFalloffProperty = serializedObject.FindProperty("_planarShadowFalloff");
        _planarShadowRangeProperty = serializedObject.FindProperty("_planarShadowRange");
        _planarShadowLightDirectionProperty = serializedObject.FindProperty("_planarShadowLightDirection");
        _usePlanarShadowCameraFacingDirectionProperty = serializedObject.FindProperty("_usePlanarShadowCameraFacingDirection");
        _planarShadowTargetCameraProperty = serializedObject.FindProperty("_planarShadowTargetCamera");
        _showPlanarShadowSceneDirectionProperty = serializedObject.FindProperty("_showPlanarShadowSceneDirection");
        _planarShadowPlaneHeightProperty = serializedObject.FindProperty("_planarShadowPlaneHeight");
        _usePlanarShadowBoundsCenterProperty = serializedObject.FindProperty("_usePlanarShadowBoundsCenter");
        _planarShadowColorProperty = serializedObject.FindProperty("_planarShadowColor");
        _enableCustomEnvironmentLightOnMaterialsProperty = serializedObject.FindProperty("_enableCustomEnvironmentLightOnMaterials");
        _enableMonsterCustomEnvironmentLightOnMaterialsProperty = serializedObject.FindProperty("_enableMonsterCustomEnvironmentLightOnMaterials");
        _enableGunCustomEnvironmentLightOnMaterialsProperty = serializedObject.FindProperty("_enableGunCustomEnvironmentLightOnMaterials");
        _customEnvironmentColorProperty = serializedObject.FindProperty("_customEnvironmentColor");
        _customEnvironmentLightBlendProperty = serializedObject.FindProperty("_customEnvironmentLightBlend");
        _indirectSpecularIntensityProperty = serializedObject.FindProperty("_indirectSpecularIntensity");
        _showSceneDirectionProperty = serializedObject.FindProperty("_showSceneDirection");
    }

    private void SyncConfigurationNameInputFromProperty()
    {
        if (_configurationNameProperty == null)
        {
            _configurationNameInput = string.Empty;
            return;
        }

        _configurationNameInput = CharacterMainLightProfile.SanitizeConfigurationName(_configurationNameProperty.stringValue);
    }

    private void AssignDefaultProfileIfMissing()
    {
        CharacterMainLightController controller = target as CharacterMainLightController;
        if (controller == null || controller.Profile != null)
        {
            return;
        }

        CharacterMainLightProfile defaultProfile = FindDefaultProfile();
        if (defaultProfile == null)
        {
            return;
        }

        serializedObject.Update();
        _profileProperty.objectReferenceValue = defaultProfile;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static CharacterMainLightProfile FindDefaultProfile()
    {
        string[] guids = AssetDatabase.FindAssets($"{DefaultProfileName} t:{nameof(CharacterMainLightProfile)}");
        CharacterMainLightProfile fallbackProfile = null;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            CharacterMainLightProfile profile = AssetDatabase.LoadAssetAtPath<CharacterMainLightProfile>(path);
            if (profile == null)
            {
                continue;
            }

            if (profile.name == DefaultProfileName)
            {
                return profile;
            }

            fallbackProfile ??= profile;
        }

        return fallbackProfile;
    }

    private void DrawConfigurationSettings()
    {
        CharacterMainLightController controller = (CharacterMainLightController)target;
        if (controller == null)
        {
            return;
        }

        EditorGUILayout.LabelField("配置", EditorStyles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                string nextConfigurationNameInput = EditorGUILayout.TextField("配置名", _configurationNameInput);
                if (EditorGUI.EndChangeCheck())
                {
                    _configurationNameInput = nextConfigurationNameInput;
                    if (_configurationNameProperty != null)
                    {
                        _configurationNameProperty.stringValue = CharacterMainLightProfile.SanitizeConfigurationName(_configurationNameInput);
                    }
                }

                if (GUILayout.Button("创建新配置", GUILayout.Width(120f)))
                {
                    GUI.FocusControl(null);
                    EditorGUIUtility.editingTextField = false;
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.UpdateIfRequiredOrScript();
                    CreateProfileAsset(controller);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (_profileProperty != null)
                {
                    EditorGUILayout.PropertyField(_profileProperty, new GUIContent("共享配置文件"));
                }

                EditorGUI.BeginDisabledGroup(controller.Profile == null);
                if (GUILayout.Button("定位配置文件", GUILayout.Width(100f)))
                {
                    Selection.activeObject = controller.Profile;
                    EditorGUIUtility.PingObject(controller.Profile);
                }
                EditorGUI.EndDisabledGroup();
            }

            DrawConfigurationList(controller);
        }

        EditorGUILayout.Space();
    }

    private void DrawConfigurationList(CharacterMainLightController controller)
    {
        CharacterMainLightProfile profile = controller.Profile;
        if (profile == null)
        {
            EditorGUILayout.HelpBox("请先指定或创建一个共享配置文件。建议 hall、battle、detail、bedroom 各自使用独立的 Profile 资产，避免多人修改同一文件产生冲突。", MessageType.Info);
            return;
        }

        if (profile.HasLegacyConfigurations)
        {
            EditorGUILayout.HelpBox("当前绑定的 Profile 仍包含旧的多配置数据。建议先拆分成 hall / battle / detail / bedroom 等独立资产，再分别绑定到对应场景。", MessageType.Warning);
            if (GUILayout.Button("拆分旧版多配置 Profile"))
            {
                Selection.activeObject = profile;
                EditorApplication.ExecuteMenuItem("Tools/GameArt/Character Main Light/Split Legacy Profiles");
            }

            EditorGUILayout.Space();
        }

        if (profile.ConfigurationCount == 0)
        {
            EditorGUILayout.HelpBox("当前 Profile 还没有配置数据。创建新配置后，后续在场景里修改参数会自动覆盖回当前 Profile。", MessageType.Info);
        }
    }

    private void TryAutoOverwriteConfiguration(CharacterMainLightController controller)
    {
        if (controller == null || controller.Profile == null || controller.ConfigurationCount == 0)
        {
            return;
        }

        Undo.RecordObject(controller.Profile, "自动覆盖 MainLight 配置");
        controller.OverwriteConfiguration();
        EditorUtility.SetDirty(controller.Profile);
        EditorUtility.SetDirty(controller);
        serializedObject.UpdateIfRequiredOrScript();
    }

    private void CreateProfileAsset(CharacterMainLightController controller)
    {
        string configurationName = CharacterMainLightProfile.SanitizeConfigurationName(_configurationNameInput);
        if (string.IsNullOrWhiteSpace(configurationName))
        {
            EditorUtility.DisplayDialog("创建新配置", "请先输入配置名。", "确定");
            return;
        }

        if (!AssetDatabase.IsValidFolder(ProfileAssetDirectory))
        {
            EditorUtility.DisplayDialog("创建新配置", $"目标目录不存在：{ProfileAssetDirectory}", "确定");
            return;
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{ProfileAssetDirectory}/{configurationName}.asset");
        CharacterMainLightProfile profile = CreateInstance<CharacterMainLightProfile>();
        profile.SaveConfiguration(controller.CreateConfigurationSnapshot(configurationName));

        AssetDatabase.CreateAsset(profile, assetPath);
        AssetDatabase.SaveAssets();
        serializedObject.ApplyModifiedProperties();
        Undo.RecordObject(controller, "指定 MainLight 共享配置文件");
        _suppressAutoOverwriteOnce = true;
        controller.ConfigurationName = configurationName;
        controller.Profile = profile;
        controller.ApplyConfiguration();
        EditorUtility.SetDirty(controller);
        serializedObject.UpdateIfRequiredOrScript();
        SyncConfigurationNameInputFromProperty();
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);
    }

    private void RepaintAfterConfigurationChange()
    {
        serializedObject.UpdateIfRequiredOrScript();
        _directionRotation = Quaternion.identity;
        _perObjectShadowDirectionRotation = Quaternion.identity;
        _planarShadowDirectionRotation = Quaternion.identity;
        SceneView.RepaintAll();
        Repaint();
    }

    private void DrawCustomDirectLightSettings()
    {
        EditorGUILayout.LabelField("自定义直射光", EditorStyles.boldLabel);
        if (_enableMonsterCustomMainLightOnMaterialsProperty == null || _enableGunCustomMainLightOnMaterialsProperty == null)
        {
            EditorGUILayout.HelpBox("未找到怪物/枪械直射光开关字段，请等待 Unity 完成脚本编译后重新选择该组件。", MessageType.Warning);
            return;
        }

        EditorGUILayout.PropertyField(_enableCustomMainLightOnMaterialsProperty, new GUIContent("开启角色自定义直射光"));
        EditorGUILayout.PropertyField(_enableMonsterCustomMainLightOnMaterialsProperty, new GUIContent("开启怪物自定义直射光"));
        EditorGUILayout.PropertyField(_enableGunCustomMainLightOnMaterialsProperty, new GUIContent("开启枪械自定义直射光"));
        if (_enableAdditionalLightsProperty != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("附加光源设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableAdditionalLightsProperty, new GUIContent("开启 Additional Light"));
        }

        if (!_enableCustomMainLightOnMaterialsProperty.boolValue
            && !_enableMonsterCustomMainLightOnMaterialsProperty.boolValue
            && !_enableGunCustomMainLightOnMaterialsProperty.boolValue)
        {
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(_useCameraDirectionProperty, new GUIContent("使用相机方向"));
            if (_useCameraDirectionProperty.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_targetCameraProperty, new GUIContent("目标相机"));
                    EditorGUILayout.PropertyField(_lightRotationOffsetEulerProperty, new GUIContent("相机方向偏移"));
                }
            }

            EditorGUILayout.PropertyField(_mainLightColorProperty, new GUIContent("直射光颜色"));
            EditorGUILayout.PropertyField(_mainLightIntensityProperty, new GUIContent("直射光强度"));
            if (!_useCameraDirectionProperty.boolValue)
            {
                DrawVector3WithSetButton(_mainLightDirectionProperty, _showSceneDirectionProperty, "直射光方向", "设置");
            }
        }
    }

    private void DrawCustomEnvironmentLightSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("自定义环境光", EditorStyles.boldLabel);
        if (_enableMonsterCustomEnvironmentLightOnMaterialsProperty == null || _enableGunCustomEnvironmentLightOnMaterialsProperty == null)
        {
            EditorGUILayout.HelpBox("未找到怪物/枪械环境光开关字段，请等待 Unity 完成脚本编译后重新选择该组件。", MessageType.Warning);
            return;
        }

        EditorGUILayout.PropertyField(_enableCustomEnvironmentLightOnMaterialsProperty, new GUIContent("开启角色自定义环境光"));
        EditorGUILayout.PropertyField(_enableMonsterCustomEnvironmentLightOnMaterialsProperty, new GUIContent("开启怪物自定义环境光"));
        EditorGUILayout.PropertyField(_enableGunCustomEnvironmentLightOnMaterialsProperty, new GUIContent("开启枪械自定义环境光"));

        if (!_enableCustomEnvironmentLightOnMaterialsProperty.boolValue
            && !_enableMonsterCustomEnvironmentLightOnMaterialsProperty.boolValue
            && !_enableGunCustomEnvironmentLightOnMaterialsProperty.boolValue)
        {
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.LabelField("环境光设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_customEnvironmentColorProperty, new GUIContent("环境光颜色"));
            EditorGUILayout.PropertyField(_customEnvironmentLightBlendProperty, new GUIContent("环境光混合"));
            EditorGUILayout.PropertyField(_indirectSpecularIntensityProperty, new GUIContent("间接高光强度"));
        }
    }

    private void DrawCustomShadowSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("自定义阴影", EditorStyles.boldLabel);

        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.LabelField("场景阴影", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableCharacterSceneShadowProperty, new GUIContent("开启角色场景阴影"));
            EditorGUILayout.PropertyField(_enableHairFringeSelfShadowProperty, new GUIContent("开启头发刘海自投影"));
            EditorGUILayout.PropertyField(_enableMonsterSceneShadowProperty, new GUIContent("开启怪物场景阴影"));
            EditorGUILayout.PropertyField(_enableGunSceneShadowProperty, new GUIContent("开启枪械场景阴影"));
            EditorGUILayout.PropertyField(_sceneShadowModeProperty, new GUIContent("场景阴影模式"));
            EditorGUILayout.PropertyField(_overridePerObjectShadowDirectionProperty, new GUIContent("覆盖 Per Object Shadow 方向"));
            if (_overridePerObjectShadowDirectionProperty.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawVector3WithSetButton(_perObjectShadowDirectionProperty, _showPerObjectShadowSceneDirectionProperty, "Per Object Shadow 方向", "设置");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("平面阴影", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enablePlanarShadowProperty, new GUIContent("开启平面阴影"));
            if (_enablePlanarShadowProperty.boolValue)
            {
                EditorGUILayout.PropertyField(_enableCharacterPlanarShadowProperty, new GUIContent("开启角色平面阴影"));
                EditorGUILayout.PropertyField(_enableMonsterPlanarShadowProperty, new GUIContent("开启怪物平面阴影"));
                EditorGUILayout.PropertyField(_enableGunPlanarShadowProperty, new GUIContent("开启枪械平面阴影"));
                DrawPlanarShadowSettingsBody();
            }
        }
    }

    private void DrawLightingSettings()
    {
        EditorGUILayout.LabelField("主光设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_enableCustomMainLightOnMaterialsProperty, new GUIContent("自动开启材质自定义光源"));

        if (_enableCustomMainLightOnMaterialsProperty.boolValue)
        {
            EditorGUILayout.PropertyField(_useCameraDirectionProperty, new GUIContent("使用相机方向"));
            if (_useCameraDirectionProperty.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_targetCameraProperty, new GUIContent("目标相机"));
                    EditorGUILayout.PropertyField(_lightRotationOffsetEulerProperty, new GUIContent("相机方向偏移"));
                }
            }

            EditorGUILayout.PropertyField(_mainLightColorProperty, new GUIContent("主光颜色"));
            EditorGUILayout.PropertyField(_mainLightIntensityProperty, new GUIContent("主光强度"));
            if (!_useCameraDirectionProperty.boolValue)
            {
                DrawVector3WithSetButton(_mainLightDirectionProperty, _showSceneDirectionProperty, "主光方向", "设置");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("场景阴影设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_sceneShadowModeProperty, new GUIContent("场景阴影模式"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("自定义环境光", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_enableCustomEnvironmentLightOnMaterialsProperty, new GUIContent("自动开启材质自定义环境光"));
        if (_enableCustomEnvironmentLightOnMaterialsProperty.boolValue)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_customEnvironmentColorProperty, new GUIContent("自定义环境颜色"));
                EditorGUILayout.PropertyField(_customEnvironmentLightBlendProperty, new GUIContent("自定义环境光混合"));
                EditorGUILayout.PropertyField(_indirectSpecularIntensityProperty, new GUIContent("间接高光强度"));
            }
        }
    }

    private void DrawPlanarShadowSettingsBody()
    {
        EditorGUILayout.PropertyField(_planarShadowFalloffProperty, new GUIContent("平面阴影衰减"));
        EditorGUILayout.PropertyField(_planarShadowRangeProperty, new GUIContent("平面阴影范围"));
        DrawPlanarShadowVector3WithSetButton(_planarShadowLightDirectionProperty, _showPlanarShadowSceneDirectionProperty, "平面阴影方向", "设置");
        EditorGUILayout.HelpBox("x 控制左右，y 控制上下，z 控制前后。", MessageType.None);
        EditorGUILayout.PropertyField(_usePlanarShadowCameraFacingDirectionProperty, new GUIContent("使用相机方向"));
        if (_usePlanarShadowCameraFacingDirectionProperty.boolValue)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_planarShadowTargetCameraProperty, new GUIContent("目标相机"));
            }
        }

        EditorGUILayout.PropertyField(_planarShadowPlaneHeightProperty, new GUIContent("阴影平面高度"));
        EditorGUILayout.PropertyField(_usePlanarShadowBoundsCenterProperty, new GUIContent("使用包围盒中心"));
        EditorGUILayout.PropertyField(_planarShadowColorProperty, new GUIContent("平面阴影颜色"));
    }

    private void DrawPlanarShadowSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("平面阴影设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_planarShadowFalloffProperty, new GUIContent("平面阴影衰减"));
        EditorGUILayout.PropertyField(_planarShadowRangeProperty, new GUIContent("平面阴影范围"));
        DrawPlanarShadowVector3WithSetButton(_planarShadowLightDirectionProperty, _showPlanarShadowSceneDirectionProperty, "平面阴影方向", "设置");
        EditorGUILayout.HelpBox("x 控制左右，y 控制上下，z 控制前后。", MessageType.None);
        EditorGUILayout.PropertyField(_usePlanarShadowCameraFacingDirectionProperty, new GUIContent("使用相机方向"));
        if (_usePlanarShadowCameraFacingDirectionProperty.boolValue)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_planarShadowTargetCameraProperty, new GUIContent("目标相机"));
            }
        }

        EditorGUILayout.PropertyField(_planarShadowPlaneHeightProperty, new GUIContent("阴影平面高度"));
        EditorGUILayout.PropertyField(_usePlanarShadowBoundsCenterProperty, new GUIContent("使用包围盒中心"));
        EditorGUILayout.PropertyField(_planarShadowColorProperty, new GUIContent("平面阴影颜色"));
    }

    private void DrawVector3WithSetButton(SerializedProperty vectorProperty, SerializedProperty setStateProperty, string fieldLabel, string buttonLabel)
    {
        Rect position = EditorGUILayout.GetControlRect();
        float buttonWidth = 68f;
        Rect contentRect = new Rect(position)
        {
            width = position.width - buttonWidth,
        };
        Rect buttonRect = new Rect(position)
        {
            x = position.xMax - 63f,
            width = 60f,
        };

        EditorGUI.BeginProperty(position, GUIContent.none, vectorProperty);
        EditorGUI.BeginChangeCheck();
        Vector3 newValue = EditorGUI.Vector3Field(contentRect, fieldLabel, vectorProperty.vector3Value);
        if (EditorGUI.EndChangeCheck())
        {
            vectorProperty.vector3Value = newValue;
            serializedObject.ApplyModifiedProperties();
            CharacterMainLightController controller = (CharacterMainLightController)target;
            _directionRotation = Quaternion.FromToRotation(Vector3.forward, controller.GetWorldDirection());
            serializedObject.UpdateIfRequiredOrScript();
        }

        bool previousState = setStateProperty.boolValue;
        Color oldColor = GUI.color;
        if (previousState)
        {
            GUI.color = Color.green;
        }

        bool nextState = GUI.Toggle(buttonRect, previousState, buttonLabel, "Button");
        GUI.color = oldColor;

        if (nextState != previousState)
        {
            setStateProperty.boolValue = nextState;
            if (nextState)
            {
                CharacterMainLightController controller = (CharacterMainLightController)target;
                _directionRotation = Quaternion.FromToRotation(Vector3.forward, controller.GetWorldDirection());
            }

            SceneView.RepaintAll();
        }

        EditorGUI.EndProperty();
    }

    private void DrawPlanarShadowVector3WithSetButton(
        SerializedProperty vectorProperty,
        SerializedProperty setStateProperty,
        string fieldLabel,
        string buttonLabel)
    {
        Rect position = EditorGUILayout.GetControlRect();
        float buttonWidth = 68f;
        Rect contentRect = new Rect(position)
        {
            width = position.width - buttonWidth,
        };
        Rect buttonRect = new Rect(position)
        {
            x = position.xMax - 63f,
            width = 60f,
        };

        EditorGUI.BeginProperty(position, GUIContent.none, vectorProperty);
        EditorGUI.BeginChangeCheck();
        Vector3 newValue = EditorGUI.Vector3Field(contentRect, fieldLabel, vectorProperty.vector3Value);
        if (EditorGUI.EndChangeCheck())
        {
            vectorProperty.vector3Value = ClampPlanarShadowDirectionComponents(newValue);
            serializedObject.ApplyModifiedProperties();
            CharacterMainLightController controller = (CharacterMainLightController)target;
            _planarShadowDirectionRotation = Quaternion.FromToRotation(Vector3.forward, controller.GetPlanarShadowWorldDirection());
            serializedObject.UpdateIfRequiredOrScript();
        }

        bool previousState = setStateProperty.boolValue;
        Color oldColor = GUI.color;
        if (previousState)
        {
            GUI.color = Color.green;
        }

        bool nextState = GUI.Toggle(buttonRect, previousState, buttonLabel, "Button");
        GUI.color = oldColor;

        if (nextState != previousState)
        {
            setStateProperty.boolValue = nextState;
            if (nextState)
            {
                CharacterMainLightController controller = (CharacterMainLightController)target;
                _planarShadowDirectionRotation = Quaternion.FromToRotation(Vector3.forward, controller.GetPlanarShadowWorldDirection());
            }

            SceneView.RepaintAll();
        }

        EditorGUI.EndProperty();
    }

    private static Vector3 ClampPlanarShadowDirectionComponents(Vector3 direction)
    {
        return new Vector3(
            Mathf.Clamp(direction.x, -1f, 1f),
            Mathf.Clamp(direction.y, 0.1f, 1f),
            Mathf.Clamp(direction.z, -1f, 1f));
    }
}

