using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterRenderController))]
public sealed class CharacterRenderControllerEditor : OdinEditor
{
    private const string DefaultProfileName = "charaRenderSetting";
    private const string ProfileAssetDirectory = "Assets/AssetRaw/Settings/CharaRenderSettings";
    private const float MinDirectionMagnitude = 0.0001f;
    private static readonly Vector3 ScenePreviewFallbackDirection = Vector3.forward;
    private static readonly Color PlanarShadowHandleColor = new Color(0.1f, 0.85f, 1f, 1f);
    private static readonly Color HighQualityShadowHandleColor = new Color(1f, 0.35f, 0.85f, 1f);
    private static readonly Color RimFakePointMaskHandleColor = new Color(1f, 0.65f, 0.15f, 1f);
    private static readonly Color RimFakeDirectMaskHandleColor = new Color(0.35f, 0.9f, 0.45f, 1f);
    private static readonly Color RimCustomDirectionHandleColor = new Color(0.75f, 0.5f, 1f, 1f);
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

    private delegate void GetAnchorAnglesDelegate(out float orbit, out float elevation);

    private sealed class AngleFieldState
    {
        public Vector2 Position;
        public float Radius;
    }

    private static readonly Vector2[] LightRayOffsets =
    {
        Vector2.zero,
        new Vector2(0.85f, 0f),
        new Vector2(-0.85f, 0f),
        new Vector2(0f, 0.85f),
        new Vector2(0f, -0.85f),
        new Vector2(0.6f, 0.6f),
        new Vector2(-0.6f, -0.6f),
    };

    private static readonly Color VirtualLightCameraDirectionHandleColor = new Color(0.3f, 0.85f, 1f, 1f);

    private SerializedProperty _profileProperty;
    private SerializedProperty _showSceneDirectionProperty;
    private SerializedProperty _showVirtualLightCameraDirectionProperty;
    private SerializedProperty _showPlanarShadowSceneDirectionProperty;
    private SerializedProperty _showPerObjectShadowSceneDirectionProperty;
    private SerializedProperty _showRimCustomDirectionProperty;
    private SerializedProperty _showRimFakePointMaskPositionProperty;
    private SerializedProperty _showRimFakeDirectMaskDirectionProperty;
    private SerializedProperty _showRimFakeDirectMaskPositionProperty;
    private SerializedProperty _enableGlobalIndirectLightProperty;
    private SerializedProperty _globalIndirectIntensityProperty;
    private SerializedProperty _globalIndirectTintColorProperty;

    private Quaternion _directionRotation = Quaternion.identity;
    private Quaternion _planarShadowDirectionRotation = Quaternion.identity;
    private Quaternion _highQualityShadowDirectionRotation = Quaternion.identity;
    private bool _showProfileSection = true;
    private bool _showLightSection = true;
    private bool _showMainLightAdjustmentSection = true;
    private bool _showBackLightSection;
    private bool _showAdditionalLightsSection = true;
    private bool _showShadowSection = true;
    private bool _showPlanarShadowSection;
    private bool _showHighQualityShadowSection;
    private bool _showOutlineSection = true;
    private bool _showEnvironmentSection = true;
    private bool _showOtherFeaturesSection = true;
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
    private bool _overridesTransformToolVisibility;
    private bool _transformToolsWereHidden;

    private static Vector2 s_CurrentMousePosition;
    private static Vector2 s_DragStartScreenPosition;
    private static Vector2 s_DragScreenOffset;

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheProperties();
    }

    protected override void OnDisable()
    {
        RestoreTransformToolVisibility();
        base.OnDisable();
    }

    public override void OnInspectorGUI()
    {
        CharacterRenderController controller = (CharacterRenderController)target;
        serializedObject.Update();
        UnityEngine.Object previousProfile = _profileProperty != null ? _profileProperty.objectReferenceValue : null;

        DrawProfileSettings(controller);
        _showCharacterCategory = DrawCategoryFoldout(_showCharacterCategory, "角色");
        if (_showCharacterCategory)
        {
            DrawDirectLightBody(controller);
            DrawEnvironmentBody(controller);
            DrawShadowBody(controller);
            DrawOtherFeaturesBody(controller);
        }

        _showMonsterCategory = DrawCategoryFoldout(_showMonsterCategory, "怪物");
        if (_showMonsterCategory)
        {
            DrawMonsterDirectLightBody(controller);
            DrawMonsterEnvironmentBody(controller);
            DrawMonsterOtherFeaturesBody(controller);
        }

        EnforceExclusiveSceneDirectionHandles();
        UpdateTransformToolVisibility(controller);
        serializedObject.ApplyModifiedProperties();

        if (_profileProperty != null && !ReferenceEquals(previousProfile, _profileProperty.objectReferenceValue))
        {
            HandleProfileReferenceChanged(controller, previousProfile as CharacterRenderLightProfile);
        }
    }

    private void OnSceneGUI()
    {
        CharacterRenderController controller = (CharacterRenderController)target;
        if (controller == null)
        {
            return;
        }

        if (controller.ShowSceneDirection
            && controller.MainLightOverrideMode == CharacterRenderMainLightOverrideMode.Custom)
        {
            _directionRotation = DrawSceneDirectionHandle(
                controller,
                "角色自定义灯光",
                "调整角色自定义灯光方向",
                controller.SceneHandleColor,
                controller.GetWorldDirection,
                controller.SetWorldDirection,
                _directionRotation);
        }

        if (controller.ShowVirtualLightCameraDirection
            && controller.MainLightOverrideMode == CharacterRenderMainLightOverrideMode.FollowCamera)
        {
            DrawVirtualLightCameraDirectionHandle(controller);
        }

        if (controller.ShowPlanarShadowSceneDirection
            && controller.EnablePlanarShadow
            && !(controller.MainLightOverrideMode != CharacterRenderMainLightOverrideMode.Off
                && controller.EnablePlanarShadowDirectionFollowMainLightOverride))
        {
            _planarShadowDirectionRotation = DrawSceneDirectionHandle(
                controller,
                "平面阴影自定义方向",
                "调整平面阴影自定义方向",
                PlanarShadowHandleColor,
                controller.GetPlanarShadowDirection,
                controller.SetPlanarShadowDirection,
                _planarShadowDirectionRotation);
        }

        if (controller.ShowPerObjectShadowSceneDirection
            && (controller.SceneShadowMode == CharacterRenderSceneShadowMode.POSOnly
                || controller.SceneShadowMode == CharacterRenderSceneShadowMode.Both)
            && !(controller.MainLightOverrideMode != CharacterRenderMainLightOverrideMode.Off
                && controller.EnablePerObjectShadowDirectionFollowMainLightOverride))
        {
            _highQualityShadowDirectionRotation = DrawSceneDirectionHandle(
                controller,
                "高清阴影自定义方向",
                "调整高清阴影自定义方向",
                HighQualityShadowHandleColor,
                controller.GetHighQualityShadowDirection,
                controller.SetHighQualityShadowDirection,
                _highQualityShadowDirectionRotation);
        }

        if (controller.ShowRimCustomDirection
            && controller.EnableRimCustomDirection)
        {
            DrawRimCustomDirectionHandle(controller);
        }

        if (controller.ShowRimFakePointMaskPosition
            && controller.EnableRimFakePointMask)
        {
            DrawRimFakePointMaskPositionHandle(controller);
        }

        if (controller.ShowRimFakeDirectMaskDirection
            && controller.EnableRimFakeDirectMask)
        {
            DrawRimFakeDirectMaskDirectionHandle(controller);
        }

        if (controller.ShowRimFakeDirectMaskPosition
            && controller.EnableRimFakeDirectMask)
        {
            DrawRimFakeDirectMaskPositionHandle(controller);
        }
    }

    private void DrawRimFakePointMaskPositionHandle(CharacterRenderController controller)
    {
        SceneView sceneView = SceneView.currentDrawingSceneView;
        Camera sceneCamera = sceneView != null ? sceneView.camera : null;
        if (sceneCamera == null)
        {
            return;
        }

        Vector3 rootPosition = controller.transform.position;
        Vector3 offset = controller.RimFakePointMaskPosition;
        Vector3 rootViewPosition = sceneCamera.transform.InverseTransformPoint(rootPosition);
        bool isViewSpace = controller.EnableRimCustomDirection
            && controller.RimDirectionSpace == CharacterRenderRimDirectionSpace.View;
        Vector3 maskPosition = isViewSpace
            ? sceneCamera.transform.TransformPoint(rootViewPosition + offset)
            : rootPosition + offset;
        float range = Mathf.Max(0.01f, controller.RimFakePointMaskRange);

        using (new Handles.DrawingScope(RimFakePointMaskHandleColor))
        {
            Handles.DrawWireDisc(maskPosition, Vector3.up, range);
            Handles.DrawWireDisc(maskPosition, Vector3.right, range);
            Handles.DrawWireDisc(maskPosition, Vector3.forward, range);
            Handles.SphereHandleCap(
                0,
                maskPosition,
                Quaternion.identity,
                HandleUtility.GetHandleSize(maskPosition) * 0.08f,
                EventType.Repaint);
            Handles.DrawDottedLine(rootPosition, maskPosition, 4f);
            Handles.Label(
                maskPosition + Vector3.up * HandleUtility.GetHandleSize(maskPosition) * 0.16f,
                $"模拟点灯位置偏移（{(isViewSpace ? "View" : "World")}）",
                EditorStyles.boldLabel);

            Quaternion handleRotation = isViewSpace
                ? sceneCamera.transform.rotation
                : Quaternion.identity;
            EditorGUI.BeginChangeCheck();
            Vector3 nextMaskPosition = Handles.PositionHandle(maskPosition, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 nextOffset = GetRimMaskOffset(
                    rootPosition,
                    nextMaskPosition,
                    sceneCamera,
                    isViewSpace);
                ApplySettingsChange(
                    controller,
                    "调整模拟点灯位置偏移",
                    () => controller.SetRimFakePointMaskPosition(nextOffset));
            }
        }
    }

    private void DrawRimCustomDirectionHandle(CharacterRenderController controller)
    {
        SceneView sceneView = SceneView.currentDrawingSceneView;
        Camera sceneCamera = sceneView != null ? sceneView.camera : null;
        if (sceneCamera == null)
        {
            return;
        }

        bool isViewSpace = controller.RimDirectionSpace == CharacterRenderRimDirectionSpace.View;
        Vector3 direction = controller.RimDirection;
        Vector3 worldDirection = isViewSpace
            ? sceneCamera.transform.TransformDirection(direction)
            : direction;
        if (worldDirection.sqrMagnitude < MinDirectionMagnitude * MinDirectionMagnitude)
        {
            worldDirection = Vector3.forward;
        }

        worldDirection.Normalize();
        Vector3 origin = controller.transform.position;
        float handleSize = HandleUtility.GetHandleSize(origin);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, worldDirection);

        using (new Handles.DrawingScope(RimCustomDirectionHandleColor))
        {
            Handles.DrawLine(origin, origin + worldDirection * handleSize * 0.9f);
            Handles.ArrowHandleCap(
                0,
                origin,
                Quaternion.LookRotation(worldDirection),
                handleSize * 0.9f,
                EventType.Repaint);
            Handles.Label(
                origin + Vector3.up * handleSize * 0.24f,
                $"边缘光自定义方向（{(isViewSpace ? "View" : "World")}）",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Quaternion nextRotation = Handles.RotationHandle(rotation, origin);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 nextWorldDirection = nextRotation * Vector3.forward;
                Vector3 nextDirection = isViewSpace
                    ? sceneCamera.transform.InverseTransformDirection(nextWorldDirection)
                    : nextWorldDirection;
                ApplySettingsChange(
                    controller,
                    "调整边缘光自定义方向",
                    () => controller.SetRimDirection(nextDirection));
            }
        }
    }

    private void DrawRimFakeDirectMaskDirectionHandle(CharacterRenderController controller)
    {
        SceneView sceneView = SceneView.currentDrawingSceneView;
        Camera sceneCamera = sceneView != null ? sceneView.camera : null;
        if (sceneCamera == null)
        {
            return;
        }

        bool isViewSpace = IsRimMaskViewSpace(controller);
        Vector3 maskPosition = GetRimMaskWorldPosition(
            controller,
            controller.RimFakeDirectMaskPosition,
            sceneCamera,
            isViewSpace);
        Vector3 direction = controller.RimFakeDirectMaskDirection;
        Vector3 worldDirection = isViewSpace
            ? sceneCamera.transform.TransformDirection(direction)
            : direction;
        if (worldDirection.sqrMagnitude < MinDirectionMagnitude * MinDirectionMagnitude)
        {
            worldDirection = Vector3.forward;
        }

        worldDirection.Normalize();
        float handleSize = HandleUtility.GetHandleSize(maskPosition);
        float previewRadius = Mathf.Max(handleSize * 0.3f, controller.RimFakeDirectMaskRange * 0.25f);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, worldDirection);

        using (new Handles.DrawingScope(RimFakeDirectMaskHandleColor))
        {
            Handles.DrawWireDisc(maskPosition, worldDirection, previewRadius);
            Handles.ArrowHandleCap(
                0,
                maskPosition,
                Quaternion.LookRotation(worldDirection),
                handleSize * 0.7f,
                EventType.Repaint);
            Handles.Label(
                maskPosition + Vector3.up * handleSize * 0.2f,
                $"模拟直接光切面方向（{(isViewSpace ? "View" : "World")}）",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Quaternion nextRotation = Handles.RotationHandle(rotation, maskPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 nextWorldDirection = nextRotation * Vector3.forward;
                Vector3 nextDirection = isViewSpace
                    ? sceneCamera.transform.InverseTransformDirection(nextWorldDirection)
                    : nextWorldDirection;
                ApplySettingsChange(
                    controller,
                    "调整模拟直接光切面方向",
                    () => controller.SetRimFakeDirectMaskDirection(nextDirection));
            }
        }
    }

    private void DrawRimFakeDirectMaskPositionHandle(CharacterRenderController controller)
    {
        SceneView sceneView = SceneView.currentDrawingSceneView;
        Camera sceneCamera = sceneView != null ? sceneView.camera : null;
        if (sceneCamera == null)
        {
            return;
        }

        Vector3 rootPosition = controller.transform.position;
        Vector3 offset = controller.RimFakeDirectMaskPosition;
        bool isViewSpace = IsRimMaskViewSpace(controller);
        Vector3 maskPosition = GetRimMaskWorldPosition(controller, offset, sceneCamera, isViewSpace);
        Vector3 direction = controller.RimFakeDirectMaskDirection;
        Vector3 worldDirection = isViewSpace
            ? sceneCamera.transform.TransformDirection(direction)
            : direction;
        if (worldDirection.sqrMagnitude < MinDirectionMagnitude * MinDirectionMagnitude)
        {
            worldDirection = Vector3.forward;
        }

        worldDirection.Normalize();
        float handleSize = HandleUtility.GetHandleSize(maskPosition);
        float previewRadius = Mathf.Max(handleSize * 0.3f, controller.RimFakeDirectMaskRange * 0.25f);

        using (new Handles.DrawingScope(RimFakeDirectMaskHandleColor))
        {
            Handles.DrawWireDisc(maskPosition, worldDirection, previewRadius);
            Handles.DrawDottedLine(rootPosition, maskPosition, 4f);
            Handles.SphereHandleCap(
                0,
                maskPosition,
                Quaternion.identity,
                handleSize * 0.08f,
                EventType.Repaint);
            Handles.Label(
                maskPosition + Vector3.up * handleSize * 0.16f,
                $"模拟直接光切面位置偏移（{(isViewSpace ? "View" : "World")}）",
                EditorStyles.boldLabel);

            Quaternion handleRotation = isViewSpace
                ? sceneCamera.transform.rotation
                : Quaternion.identity;
            EditorGUI.BeginChangeCheck();
            Vector3 nextMaskPosition = Handles.PositionHandle(maskPosition, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 nextOffset = GetRimMaskOffset(
                    rootPosition,
                    nextMaskPosition,
                    sceneCamera,
                    isViewSpace);
                ApplySettingsChange(
                    controller,
                    "调整模拟直接光切面位置偏移",
                    () => controller.SetRimFakeDirectMaskPosition(nextOffset));
            }
        }
    }

    private static bool IsRimMaskViewSpace(CharacterRenderController controller)
    {
        return controller.EnableRimCustomDirection
            && controller.RimDirectionSpace == CharacterRenderRimDirectionSpace.View;
    }

    private static Vector3 GetRimMaskWorldPosition(
        CharacterRenderController controller,
        Vector3 offset,
        Camera sceneCamera,
        bool isViewSpace)
    {
        Vector3 rootPosition = controller.transform.position;
        if (!isViewSpace)
        {
            return rootPosition + offset;
        }

        Vector3 rootViewPosition = sceneCamera.transform.InverseTransformPoint(rootPosition);
        return sceneCamera.transform.TransformPoint(rootViewPosition + offset);
    }

    private static Vector3 GetRimMaskOffset(
        Vector3 rootPosition,
        Vector3 maskPosition,
        Camera sceneCamera,
        bool isViewSpace)
    {
        Vector3 worldOffset = maskPosition - rootPosition;
        return isViewSpace
            ? sceneCamera.transform.InverseTransformDirection(worldOffset)
            : worldOffset;
    }

    private Quaternion DrawSceneDirectionHandle(
        CharacterRenderController controller,
        string label,
        string undoName,
        Color handleColor,
        Func<Vector3> getDirection,
        Action<Vector3> setDirection,
        Quaternion directionRotation)
    {
        Vector3 origin = controller.transform.position;
        Vector3 lightTravelDirection = ResolveScenePreviewDirection(getDirection());
        if (directionRotation == Quaternion.identity)
        {
            directionRotation = Quaternion.FromToRotation(Vector3.forward, lightTravelDirection);
        }

        using (new Handles.DrawingScope(handleColor))
        {
            DrawDirectionalLightPreview(origin, lightTravelDirection, controller.SceneHandleLength);
            Handles.Label(
                origin + Vector3.up * HandleUtility.GetHandleSize(origin) * 0.25f,
                label,
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Quaternion newRotation = Handles.RotationHandle(directionRotation, origin);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySettingsChange(controller, undoName, () =>
                {
                    setDirection(-(newRotation * Vector3.forward));
                });
                directionRotation = newRotation;
            }
        }

        return directionRotation;
    }

    private void DrawVirtualLightCameraDirectionHandle(CharacterRenderController controller)
    {
        Camera sceneCamera = GetDirectionPreviewCamera();
        Vector3 direction = controller.GetVirtualLightCameraDirection(sceneCamera);
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 origin = controller.transform.position;
        Vector3 lightTravelDirection = ResolveScenePreviewDirection(direction);
        using (new Handles.DrawingScope(VirtualLightCameraDirectionHandleColor))
        {
            DrawDirectionalLightPreview(origin, lightTravelDirection, controller.SceneHandleLength);
            Handles.Label(
                origin + Vector3.up * HandleUtility.GetHandleSize(origin) * 0.5f,
                "偏移后相机方向",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Quaternion newRotation = Handles.RotationHandle(
                Quaternion.FromToRotation(Vector3.forward, lightTravelDirection),
                origin);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySettingsChange(
                    controller,
                    "调整偏移后相机方向",
                    () => controller.SetVirtualLightCameraDirection(-(newRotation * Vector3.forward), sceneCamera));
            }
        }
    }

    private static Camera GetDirectionPreviewCamera()
    {
        if (Camera.current != null)
        {
            return Camera.current;
        }

        return SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.camera
            : Camera.main;
    }

    private static void DrawDirectionalLightPreview(Vector3 origin, Vector3 worldDirection, float handleLength)
    {
        float handleSize = HandleUtility.GetHandleSize(origin);
        float sourceRadius = Mathf.Max(handleSize * 0.18f, handleLength * 0.12f);
        float sourceOffset = sourceRadius * 1.35f;
        float rayLength = Mathf.Max(handleLength, handleSize * 0.9f);
        Vector3 sourceCenter = origin - worldDirection * sourceOffset;

        GetDirectionBasis(worldDirection, out Vector3 right, out Vector3 up);

        Handles.DrawWireDisc(sourceCenter, worldDirection, sourceRadius);
        Handles.DrawWireDisc(sourceCenter, worldDirection, sourceRadius * 0.6f);
        Handles.DrawSolidDisc(sourceCenter, worldDirection, sourceRadius * 0.08f);

        for (int i = 0; i < LightRayOffsets.Length; i++)
        {
            Vector2 offset2D = LightRayOffsets[i];
            Vector3 offset = (right * offset2D.x + up * offset2D.y) * sourceRadius;
            Vector3 rayStart = sourceCenter + offset;
            Vector3 rayEnd = rayStart + worldDirection * rayLength;
            Handles.DrawAAPolyLine(3f, rayStart, rayEnd);
        }

        Vector3 conePosition = sourceCenter + worldDirection * rayLength;
        Handles.ConeHandleCap(
            0,
            conePosition,
            Quaternion.LookRotation(worldDirection),
            HandleUtility.GetHandleSize(conePosition) * 0.14f,
            EventType.Repaint);
    }

    private static void GetDirectionBasis(Vector3 direction, out Vector3 right, out Vector3 up)
    {
        Vector3 referenceUp = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.98f
            ? Vector3.right
            : Vector3.up;

        right = Vector3.Cross(referenceUp, direction).normalized;
        up = Vector3.Cross(direction, right).normalized;
    }

    private void CacheProperties()
    {
        _profileProperty = serializedObject.FindProperty("_profile");
        _showSceneDirectionProperty = serializedObject.FindProperty("_showSceneDirection");
        _showVirtualLightCameraDirectionProperty =
            serializedObject.FindProperty("_showVirtualLightCameraDirection");
        _showPlanarShadowSceneDirectionProperty =
            serializedObject.FindProperty("_showPlanarShadowSceneDirection");
        _showPerObjectShadowSceneDirectionProperty =
            serializedObject.FindProperty("_showPerObjectShadowSceneDirection");
        _showRimCustomDirectionProperty =
            serializedObject.FindProperty("_showRimCustomDirection");
        _showRimFakePointMaskPositionProperty =
            serializedObject.FindProperty("_showRimFakePointMaskPosition");
        _showRimFakeDirectMaskDirectionProperty =
            serializedObject.FindProperty("_showRimFakeDirectMaskDirection");
        _showRimFakeDirectMaskPositionProperty =
            serializedObject.FindProperty("_showRimFakeDirectMaskPosition");
        _enableGlobalIndirectLightProperty = serializedObject.FindProperty("_enableGlobalIndirectLight");
        _globalIndirectIntensityProperty = serializedObject.FindProperty("_globalIndirectIntensity");
        _globalIndirectTintColorProperty = serializedObject.FindProperty("_globalIndirectTintColor");
    }

    private void DrawProfileSettings(CharacterRenderController controller)
    {
        _showProfileSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showProfileSection, "配置文件");
        if (_showProfileSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_profileProperty, new GUIContent("角色 Render Setting 配置"));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("新建配置"))
                    {
                        GUI.FocusControl(null);
                        CreateProfileAsset(controller);
                    }

                    EditorGUI.BeginDisabledGroup(controller == null || controller.Profile == null);
                    if (GUILayout.Button("定位配置"))
                    {
                        Selection.activeObject = controller.Profile;
                        EditorGUIUtility.PingObject(controller.Profile);
                    }
                    EditorGUI.EndDisabledGroup();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(controller == null || controller.Profile == null);
                    if (GUILayout.Button("复制配置参数"))
                    {
                        GUI.FocusControl(null);
                        CharacterRenderLightProfileClipboard.Copy(controller.Profile);
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(
                        controller == null
                        || controller.Profile == null
                        || !CharacterRenderLightProfileClipboard.HasParameters);
                    if (GUILayout.Button("粘贴配置参数"))
                    {
                        GUI.FocusControl(null);
                        if (CharacterRenderLightProfileClipboard.TryPaste(controller.Profile, out string errorMessage))
                        {
                            serializedObject.UpdateIfRequiredOrScript();
                            RepaintAfterSettingsChange();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("粘贴角色渲染配置参数", errorMessage, "确定");
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }

                if (controller != null && controller.Profile != null)
                {
                    EditorGUILayout.HelpBox(
                        "当前灯光和阴影参数写入共享配置资产，也可以直接在 Project 面板中选中这个配置文件进行调节。",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "当前使用场景本地参数。若希望与场景解耦，请创建或指定一个配置资产。",
                        MessageType.None);
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space();
    }

    private void DrawDirectLightBody(CharacterRenderController controller)
    {
        if (controller == null)
        {
            return;
        }

        _showLightSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showLightSection, "直接光设置");
        if (_showLightSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _showMainLightAdjustmentSection = EditorGUILayout.Foldout(
                    _showMainLightAdjustmentSection,
                    "主光覆盖",
                    true,
                    EditorStyles.foldoutHeader);
                if (_showMainLightAdjustmentSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        CharacterRenderMainLightOverrideMode mode = (CharacterRenderMainLightOverrideMode)EditorGUILayout.Popup(
                            "模式",
                            (int)controller.MainLightOverrideMode,
                            MainLightOverrideModeLabels);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplySettingsChange(controller, "调整主光覆盖模式", () => controller.SetMainLightOverrideMode(mode));
                        }

                        if (mode != CharacterRenderMainLightOverrideMode.Off)
                        {
                            EditorGUI.BeginChangeCheck();
                            Color color = EditorGUILayout.ColorField("颜色", controller.CustomLightColor);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(controller, "调整主光覆盖颜色", () => controller.SetCustomLightColor(color));
                            }

                            EditorGUI.BeginChangeCheck();
                            float strength = EditorGUILayout.Slider("强度", controller.CustomLightStrength, 0f, 8f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(controller, "调整主光覆盖强度", () => controller.SetCustomLightStrength(strength));
                            }
                        }

                        if (mode == CharacterRenderMainLightOverrideMode.FollowCamera)
                        {
                            DrawVirtualLightCameraDirectionOffsetFieldWithEditButton(controller);
                        }
                        else if (mode == CharacterRenderMainLightOverrideMode.Custom)
                        {
                            DrawLightAnchorInspector(controller);
                            DrawDirectionFieldWithEditButton(controller, "方向", "编辑");
                            EditorGUILayout.PropertyField(_showSceneDirectionProperty, new GUIContent("显示方向手柄"));
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("关闭时跟随场景 Directional Light。", MessageType.None);
                        }
                    }
                }

                EditorGUILayout.Space(2f);
                _showBackLightSection = EditorGUILayout.Foldout(
                    _showBackLightSection,
                    "背光补偿",
                    true,
                    EditorStyles.foldoutHeader);
                if (_showBackLightSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        bool enableBackLight = EditorGUILayout.Toggle("启用", controller.EnableBackLight);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplySettingsChange(controller, "调整背光开关", () => controller.SetBackLightEnabled(enableBackLight));
                        }

                        using (new EditorGUI.DisabledGroupScope(!controller.EnableBackLight))
                        {
                            EditorGUI.BeginChangeCheck();
                            Color color = EditorGUILayout.ColorField("颜色", controller.VirtualLightColor);
                            float intensity = EditorGUILayout.Slider("强度", controller.VirtualLightIntensity, 0f, 2f);
                            float offset = EditorGUILayout.Slider("兰伯特偏移", controller.VirtualLightOffset, -1f, 1f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(controller, "调整背光参数", () =>
                                {
                                    controller.SetVirtualLightColor(color);
                                    controller.SetVirtualLightIntensity(intensity);
                                    controller.SetVirtualLightOffset(offset);
                                });
                            }
                        }
                    }
                }

                EditorGUILayout.Space(2f);
                _showAdditionalLightsSection = EditorGUILayout.Foldout(
                    _showAdditionalLightsSection,
                    "点光控制",
                    true,
                    EditorStyles.foldoutHeader);
                if (_showAdditionalLightsSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        bool enabled = EditorGUILayout.Toggle("启用角色点光", controller.EnableCharacterAdditionalLights);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplySettingsChange(controller, "调整角色点光开关", () => controller.SetCharacterAdditionalLightsEnabled(enabled));
                        }

                        using (new EditorGUI.DisabledGroupScope(!controller.EnableCharacterAdditionalLights))
                        {
                            EditorGUI.BeginChangeCheck();
                            bool enableDiffuse = EditorGUILayout.Toggle(
                                "启用点光漫反射",
                                controller.EnableCharacterAdditionalLightDiffuse);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(
                                    controller,
                                    "调整点光漫反射开关",
                                    () => controller.SetCharacterAdditionalLightDiffuseEnabled(enableDiffuse));
                            }

                            EditorGUI.BeginChangeCheck();
                            bool enableSpecular = EditorGUILayout.Toggle(
                                "启用点光高光",
                                controller.EnableCharacterAdditionalLightSpecular);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(
                                    controller,
                                    "调整点光高光开关",
                                    () => controller.SetCharacterAdditionalLightSpecularEnabled(enableSpecular));
                            }
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
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

    private void DrawMonsterDirectLightBody(CharacterRenderController controller)
    {
        _showMonsterDirectLightSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showMonsterDirectLightSection, "直接光设置");
        if (_showMonsterDirectLightSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _showMonsterMainLightSection = EditorGUILayout.Foldout(_showMonsterMainLightSection, "主光覆盖", true, EditorStyles.foldoutHeader);
                if (_showMonsterMainLightSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        MonsterRenderMainLightOverrideMode mode = (MonsterRenderMainLightOverrideMode)EditorGUILayout.Popup(
                            "模式", (int)controller.MonsterMainLightOverrideMode, MonsterMainLightOverrideModeLabels);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplySettingsChange(controller, "调整怪物主光覆盖模式", () => controller.SetMonsterMainLightOverrideMode(mode));
                        }

                        if (mode != MonsterRenderMainLightOverrideMode.FollowCharacter
                            && mode != MonsterRenderMainLightOverrideMode.Off)
                        {
                            EditorGUI.BeginChangeCheck();
                            Color color = EditorGUILayout.ColorField("颜色", controller.MonsterCustomLightColor);
                            float strength = EditorGUILayout.Slider("强度", controller.MonsterCustomLightStrength, 0f, 8f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(controller, "调整怪物主光参数", () =>
                                {
                                    controller.SetMonsterCustomLightColor(color);
                                    controller.SetMonsterCustomLightStrength(strength);
                                });
                            }
                        }

                        if (mode == MonsterRenderMainLightOverrideMode.FollowCamera)
                        {
                            EditorGUI.BeginChangeCheck();
                            Vector3 offset = EditorGUILayout.Vector3Field("额外方向偏移", controller.MonsterVirtualLightCameraDirectionOffsetEuler);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(controller, "调整怪物相机光方向偏移", () => controller.SetMonsterVirtualLightCameraDirectionOffsetEuler(offset));
                            }
                        }
                        else if (mode == MonsterRenderMainLightOverrideMode.Custom)
                        {
                            EditorGUI.BeginChangeCheck();
                            Vector3 direction = EditorGUILayout.Vector3Field("方向", controller.MonsterCustomLightDirection);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(controller, "调整怪物主光方向", () => controller.SetMonsterCustomLightDirection(direction));
                            }
                        }
                        else if (mode == MonsterRenderMainLightOverrideMode.FollowCharacter)
                        {
                            EditorGUILayout.HelpBox("主光覆盖的模式、颜色、强度和方向全部跟随角色。", MessageType.None);
                        }
                    }
                }

                EditorGUILayout.Space(2f);
                _showMonsterPointLightSection = EditorGUILayout.Foldout(_showMonsterPointLightSection, "点光控制", true, EditorStyles.foldoutHeader);
                if (_showMonsterPointLightSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        bool enabled = EditorGUILayout.Toggle("启用怪物点光", controller.MonsterAdditionalLightsEnabled);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplySettingsChange(controller, "调整怪物点光开关", () => controller.SetMonsterAdditionalLightsEnabled(enabled));
                        }
                        using (new EditorGUI.DisabledGroupScope(!controller.MonsterAdditionalLightsEnabled))
                        {
                            EditorGUI.BeginChangeCheck();
                            bool diffuse = EditorGUILayout.Toggle("启用点光漫反射", controller.MonsterAdditionalLightDiffuseEnabled);
                            bool specular = EditorGUILayout.Toggle("启用点光高光", controller.MonsterAdditionalLightSpecularEnabled);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(controller, "调整怪物点光分量", () =>
                                {
                                    controller.SetMonsterAdditionalLightDiffuseEnabled(diffuse);
                                    controller.SetMonsterAdditionalLightSpecularEnabled(specular);
                                });
                            }
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawMonsterEnvironmentBody(CharacterRenderController controller)
    {
        _showMonsterEnvironmentSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showMonsterEnvironmentSection, "环境光设置");
        if (_showMonsterEnvironmentSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();
                bool follow = EditorGUILayout.Toggle("跟随角色", controller.MonsterEnvironmentFollowCharacter);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(controller, "调整怪物环境光跟随", () => controller.SetMonsterEnvironmentFollowCharacter(follow));
                }
                if (!controller.MonsterEnvironmentFollowCharacter)
                {
                    EditorGUI.BeginChangeCheck();
                    bool enabled = EditorGUILayout.Toggle("启用", controller.MonsterEnableGlobalIndirectLight);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplySettingsChange(controller, "调整怪物环境光开关", () => controller.SetMonsterEnableGlobalIndirectLight(enabled));
                    }
                    using (new EditorGUI.DisabledGroupScope(!controller.MonsterEnableGlobalIndirectLight))
                    {
                        EditorGUI.BeginChangeCheck();
                        bool overrideAmbient = EditorGUILayout.Toggle("覆盖场景环境光", controller.MonsterOverrideSceneAmbient);
                        float intensity = EditorGUILayout.Slider("间接漫反射强度", controller.MonsterGlobalIndirectIntensity, 0f, 8f);
                        Color tint = EditorGUILayout.ColorField("间接漫反射颜色", controller.MonsterGlobalIndirectTintColor);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplySettingsChange(controller, "调整怪物环境光参数", () =>
                            {
                                controller.SetMonsterOverrideSceneAmbient(overrideAmbient);
                                controller.SetMonsterGlobalIndirectIntensity(intensity);
                                controller.SetMonsterGlobalIndirectTintColor(tint);
                            });
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawMonsterOtherFeaturesBody(CharacterRenderController controller)
    {
        _showMonsterOtherFeaturesSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showMonsterOtherFeaturesSection, "其他特性设置");
        if (_showMonsterOtherFeaturesSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();
                bool follow = EditorGUILayout.Toggle("跟随角色", controller.MonsterOtherFeaturesFollowCharacter);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(controller, "调整怪物其他特性跟随", () => controller.SetMonsterOtherFeaturesFollowCharacter(follow));
                }
                if (!controller.MonsterOtherFeaturesFollowCharacter)
                {
                    EditorGUI.BeginChangeCheck();
                    bool outline = EditorGUILayout.Toggle("开启描边", controller.MonsterEnableOutline);
                    bool rim = EditorGUILayout.Toggle("开启边缘光", controller.MonsterEnableRimLight);
                    bool gradient = EditorGUILayout.Toggle("开启最终颜色渐变", controller.MonsterEnableFinalColorGradient);
                    Color gradientColor = controller.MonsterFinalColorGradientColor;
                    float minY = controller.MonsterFinalColorGradientMinY;
                    float maxY = controller.MonsterFinalColorGradientMaxY;
                    if (gradient)
                    {
                        gradientColor = EditorGUILayout.ColorField("渐变颜色", gradientColor);
                        minY = EditorGUILayout.FloatField("渐变最小 Y", minY);
                        maxY = EditorGUILayout.FloatField("渐变最大 Y", maxY);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplySettingsChange(controller, "调整怪物其他特性", () =>
                        {
                            controller.SetMonsterOutlineEnabled(outline);
                            controller.SetMonsterRimLightEnabled(rim);
                            controller.SetMonsterFinalColorGradientEnabled(gradient);
                            controller.SetMonsterFinalColorGradientColor(gradientColor);
                            controller.SetMonsterFinalColorGradientMinY(minY);
                            controller.SetMonsterFinalColorGradientMaxY(maxY);
                        });
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawOtherFeaturesBody(CharacterRenderController controller)
    {
        if (controller == null)
        {
            return;
        }

        _showOtherFeaturesSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showOtherFeaturesSection, "其他特性设置");
        if (_showOtherFeaturesSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _showOutlineSection = EditorGUILayout.Foldout(_showOutlineSection, "描边", true, EditorStyles.foldoutHeader);
                if (_showOutlineSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        bool enabled = EditorGUILayout.Toggle("开启描边", controller.EnableOutline);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplySettingsChange(controller, "调整描边开关", () => controller.SetOutlineEnabled(enabled));
                        }
                    }
                }

                EditorGUILayout.Space(2f);
                _showRimLightSection = EditorGUILayout.Foldout(_showRimLightSection, "边缘光", true, EditorStyles.foldoutHeader);
                if (_showRimLightSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        bool enabled = EditorGUILayout.Toggle("开启边缘光", controller.EnableRimLight);
                        float intensity = EditorGUILayout.Slider("全局强度", controller.RimIntensity, 0f, 8f);
                        bool customDirectionEnabled = EditorGUILayout.Toggle("自定义方向", controller.EnableRimCustomDirection);
                        CharacterRenderRimDirectionSpace directionSpace = controller.RimDirectionSpace;
                        Vector3 direction = controller.RimDirection;
                        using (new EditorGUI.DisabledGroupScope(!customDirectionEnabled))
                        {
                            directionSpace = (CharacterRenderRimDirectionSpace)EditorGUILayout.EnumPopup("方向空间", directionSpace);
                            direction = DrawRimCustomDirectionFieldWithEditButton(controller, direction);
                        }
                        bool fakePointMaskEnabled = EditorGUILayout.Toggle("启用模拟点灯遮罩", controller.EnableRimFakePointMask);
                        float fakePointMaskRange = controller.RimFakePointMaskRange;
                        float fakePointMaskPower = controller.RimFakePointMaskPower;
                        using (new EditorGUI.DisabledGroupScope(!fakePointMaskEnabled))
                        {
                            DrawRimFakePointMaskPositionFieldWithEditButton(controller);
                            fakePointMaskRange = EditorGUILayout.FloatField("点灯范围", fakePointMaskRange);
                            fakePointMaskPower = EditorGUILayout.Slider("模拟点灯边缘软硬", fakePointMaskPower, 0.25f, 8f);
                        }
                        bool fakeDirectMaskEnabled = EditorGUILayout.Toggle("启用模拟直接光遮罩", controller.EnableRimFakeDirectMask);
                        Vector3 fakeDirectMaskDirection = controller.RimFakeDirectMaskDirection;
                        Vector3 fakeDirectMaskPosition = controller.RimFakeDirectMaskPosition;
                        float fakeDirectMaskRange = controller.RimFakeDirectMaskRange;
                        using (new EditorGUI.DisabledGroupScope(!fakeDirectMaskEnabled))
                        {
                            fakeDirectMaskDirection = DrawRimFakeDirectMaskDirectionFieldWithEditButton(
                                controller,
                                fakeDirectMaskDirection);
                            fakeDirectMaskPosition = DrawRimFakeDirectMaskPositionFieldWithEditButton(
                                controller,
                                fakeDirectMaskPosition);
                            fakeDirectMaskRange = EditorGUILayout.FloatField("直接光范围", fakeDirectMaskRange);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplySettingsChange(controller, "调整边缘光全局控制", () =>
                            {
                                controller.SetRimLightEnabled(enabled);
                                controller.SetRimIntensity(intensity);
                                controller.SetRimCustomDirectionEnabled(customDirectionEnabled);
                                controller.SetRimDirectionSpace(directionSpace);
                                controller.SetRimDirection(direction);
                                controller.SetRimFakePointMaskEnabled(fakePointMaskEnabled);
                                controller.SetRimFakePointMaskRange(fakePointMaskRange);
                                controller.SetRimFakePointMaskPower(fakePointMaskPower);
                                controller.SetRimFakeDirectMaskEnabled(fakeDirectMaskEnabled);
                                controller.SetRimFakeDirectMaskDirection(fakeDirectMaskDirection);
                                controller.SetRimFakeDirectMaskPosition(fakeDirectMaskPosition);
                                controller.SetRimFakeDirectMaskRange(fakeDirectMaskRange);
                            });
                        }
                        EditorGUILayout.HelpBox("该开关只门控材质已经编译并启用的 Rim Variant。", MessageType.None);
                    }
                }

                EditorGUILayout.Space(2f);
                _showFinalColorGradientSection = EditorGUILayout.Foldout(
                    _showFinalColorGradientSection,
                    "最终颜色渐变",
                    true,
                    EditorStyles.foldoutHeader);
                if (_showFinalColorGradientSection)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        bool enabled = EditorGUILayout.Toggle("开启最终颜色渐变", controller.EnableFinalColorGradient);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplySettingsChange(controller, "调整最终颜色渐变开关", () => controller.SetFinalColorGradientEnabled(enabled));
                        }

                        using (new EditorGUI.DisabledGroupScope(!controller.EnableFinalColorGradient))
                        {
                            EditorGUI.BeginChangeCheck();
                            Color color = EditorGUILayout.ColorField("渐变颜色", controller.FinalColorGradientColor);
                            float minY = EditorGUILayout.FloatField("渐变最小 Y", controller.FinalColorGradientMinY);
                            float maxY = EditorGUILayout.FloatField("渐变最大 Y", controller.FinalColorGradientMaxY);
                            if (EditorGUI.EndChangeCheck())
                            {
                                ApplySettingsChange(controller, "调整最终颜色渐变参数", () =>
                                {
                                    controller.SetFinalColorGradientColor(color);
                                    controller.SetFinalColorGradientMinY(minY);
                                    controller.SetFinalColorGradientMaxY(maxY);
                                });
                            }
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawShadowBody(CharacterRenderController controller)
    {
        if (controller == null)
        {
            return;
        }

        _showShadowSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showShadowSection, "阴影设置");
        if (_showShadowSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _showPlanarShadowSection = EditorGUILayout.Foldout(
                    _showPlanarShadowSection,
                    "平面阴影",
                    true,
                    EditorStyles.foldoutHeader);
                if (_showPlanarShadowSection)
                {
                    DrawPlanarShadowModule(controller);
                }

                EditorGUILayout.Space(2f);
                _showHighQualityShadowSection = EditorGUILayout.Foldout(
                    _showHighQualityShadowSection,
                    "场景阴影",
                    true,
                    EditorStyles.foldoutHeader);
                if (_showHighQualityShadowSection)
                {
                    DrawSceneShadowModule(controller);
                }

               
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space();
    }

    private void DrawSceneShadowModule(CharacterRenderController controller)
    {
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUI.BeginChangeCheck();
            CharacterRenderSceneShadowMode mode =
                (CharacterRenderSceneShadowMode)EditorGUILayout.EnumPopup("模式", controller.SceneShadowMode);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySettingsChange(controller, "调整场景阴影模式", () => controller.SetSceneShadowMode(mode));
            }

            bool usesPerObjectShadow = mode == CharacterRenderSceneShadowMode.POSOnly
                || mode == CharacterRenderSceneShadowMode.Both;
            if (usesPerObjectShadow)
            {
                DrawHighQualityShadowModule(controller);
            }

            EditorGUILayout.HelpBox(
                "0 Off / 1 UnityOnly / 2 POSOnly / 3 Both；选择 POSOnly 或 Both 时会自动启用 POS。",
                MessageType.None);
        }
    }

    private void DrawPlanarShadowModule(CharacterRenderController controller)
    {
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUI.BeginChangeCheck();
            bool nextEnabled = EditorGUILayout.Toggle("开启平面阴影", controller.EnablePlanarShadow);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySettingsChange(
                    controller,
                    "调整平面阴影开关",
                    () => controller.SetPlanarShadowEnabled(nextEnabled));
            }

            using (new EditorGUI.DisabledGroupScope(!controller.EnablePlanarShadow))
            {
                bool canFollowMainLightOverride =
                    controller.MainLightOverrideMode != CharacterRenderMainLightOverrideMode.Off;
                if (canFollowMainLightOverride)
                {
                    EditorGUI.BeginChangeCheck();
                    bool followMainLightOverride = EditorGUILayout.Toggle(
                        "跟随主光覆盖方向",
                        controller.EnablePlanarShadowDirectionFollowMainLightOverride);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplySettingsChange(
                            controller,
                            "调整平面阴影跟随主光覆盖方向",
                            () => controller.SetPlanarShadowDirectionFollowMainLightOverrideEnabled(followMainLightOverride));
                    }

                }

                bool followsMainLightOverride = canFollowMainLightOverride
                    && controller.EnablePlanarShadowDirectionFollowMainLightOverride;
                if (!followsMainLightOverride)
                {
                    _planarShadowDirectionRotation = DrawShadowDirectionAnchorInspector(
                        controller,
                        "Planar Shadow Light Anchor",
                        "调整平面阴影 Light Anchor",
                        controller.GetPlanarShadowAnchorAngles,
                        controller.SetPlanarShadowAnchorAngles,
                        controller.GetPlanarShadowDirection,
                        _planarShadowDirectionRotation);
                    _planarShadowDirectionRotation = DrawShadowDirectionFieldWithEditButton(
                        controller,
                        "平面阴影自定义方向",
                        "调整平面阴影自定义方向",
                        controller.GetPlanarShadowDirection,
                        controller.SetPlanarShadowDirection,
                        _showPlanarShadowSceneDirectionProperty,
                        _planarShadowDirectionRotation);
                }

                float strength = controller.PlanarShadowStrength;
                EditorGUI.BeginChangeCheck();
                float nextStrength = EditorGUILayout.Slider("平面阴影强度", strength, 0f, 4f);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(
                        controller,
                        "调整平面阴影强度",
                        () => controller.SetPlanarShadowStrength(nextStrength));
                }

                EditorGUI.BeginChangeCheck();
                float nextFalloff = EditorGUILayout.FloatField("平面阴影衰减", controller.PlanarShadowFalloff);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(
                        controller,
                        "调整平面阴影衰减",
                        () => controller.SetPlanarShadowFalloff(nextFalloff));
                }

                EditorGUI.BeginChangeCheck();
                float nextRange = EditorGUILayout.FloatField("平面阴影范围", controller.PlanarShadowRange);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(
                        controller,
                        "调整平面阴影范围",
                        () => controller.SetPlanarShadowRange(nextRange));
                }

                EditorGUI.BeginChangeCheck();
                float nextPlaneHeight = EditorGUILayout.FloatField("平面高度（世界 Y）", controller.PlanarShadowPlaneHeight);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(
                        controller,
                        "调整平面阴影平面高度",
                        () => controller.SetPlanarShadowPlaneHeight(nextPlaneHeight));
                }

                EditorGUI.BeginChangeCheck();
                Vector3 nextGlobalCenter = EditorGUILayout.Vector3Field("全局中心（世界坐标）", controller.PlanarShadowGlobalCenter);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(
                        controller,
                        "调整平面阴影全局中心",
                        () => controller.SetPlanarShadowGlobalCenter(nextGlobalCenter));
                }

                EditorGUI.BeginChangeCheck();
                Color nextColor = EditorGUILayout.ColorField("平面阴影颜色", controller.PlanarShadowColor);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(
                        controller,
                        "调整平面阴影颜色",
                        () => controller.SetPlanarShadowColor(nextColor));
                }
            }

            EditorGUILayout.HelpBox(
                controller.MainLightOverrideMode != CharacterRenderMainLightOverrideMode.Off
                    && controller.EnablePlanarShadowDirectionFollowMainLightOverride
                    ? "当前平面阴影方向的 XYZ 完整跟随主光覆盖方向。"
                    : "自定义方向为 (0, 0, 0) 时沿用材质的 _PlanarShadowLightDir.xyz；平面高度、衰减、范围、全局中心和颜色由 Character Render Control 统一覆盖。",
                MessageType.None);
            EditorGUILayout.HelpBox(
                "平面高度和全局中心使用世界坐标，并由当前有效的 Character Render Control 对场景内 Chara V2/V3 统一生效。",
                MessageType.Info);
        }
    }

    private void DrawHighQualityShadowModule(CharacterRenderController controller)
    {
        using (new EditorGUI.IndentLevelScope())
        {
            bool canFollowMainLightOverride =
                controller.MainLightOverrideMode != CharacterRenderMainLightOverrideMode.Off;
            if (canFollowMainLightOverride)
            {
                EditorGUI.BeginChangeCheck();
                bool followMainLightOverride = EditorGUILayout.Toggle(
                    "跟随主光覆盖方向",
                    controller.EnablePerObjectShadowDirectionFollowMainLightOverride);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(
                        controller,
                        "调整场景阴影跟随主光覆盖方向",
                        () => controller.SetPerObjectShadowDirectionFollowMainLightOverrideEnabled(followMainLightOverride));
                }
            }

            bool followsMainLightOverride = canFollowMainLightOverride
                && controller.EnablePerObjectShadowDirectionFollowMainLightOverride;
            if (!followsMainLightOverride)
            {
                _highQualityShadowDirectionRotation = DrawShadowDirectionAnchorInspector(
                    controller,
                    "High Quality Shadow Light Anchor",
                    "调整高清阴影 Light Anchor",
                    controller.GetHighQualityShadowAnchorAngles,
                    controller.SetHighQualityShadowAnchorAngles,
                    controller.GetHighQualityShadowDirection,
                    _highQualityShadowDirectionRotation);
                _highQualityShadowDirectionRotation = DrawShadowDirectionFieldWithEditButton(
                    controller,
                    "高清阴影自定义方向",
                    "调整高清阴影自定义方向",
                    controller.GetHighQualityShadowDirection,
                    controller.SetHighQualityShadowDirection,
                    _showPerObjectShadowSceneDirectionProperty,
                    _highQualityShadowDirectionRotation);
            }

            float strength = controller.SelfShadowStrength;
            EditorGUI.BeginChangeCheck();
            float nextStrength = EditorGUILayout.Slider("Self Shadow 阴影强度", strength, 0f, 2f);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySettingsChange(
                    controller,
                    "调整 Self Shadow 阴影强度",
                    () => controller.SetSelfShadowStrength(nextStrength));
            }

            float environmentStrength = controller.EnvironmentShadowStrength;
            EditorGUI.BeginChangeCheck();
            float nextEnvironmentStrength = EditorGUILayout.Slider(
                "Environment Shadow 阴影强度",
                environmentStrength,
                0f,
                2f);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySettingsChange(
                    controller,
                    "调整 Environment Shadow 阴影强度",
                    () => controller.SetEnvironmentShadowStrength(nextEnvironmentStrength));
            }

            float specularShadowStrength = controller.SpecularShadowStrength;
            EditorGUI.BeginChangeCheck();
            float nextSpecularShadowStrength = EditorGUILayout.Slider(
                "高光阴影作用强度",
                specularShadowStrength,
                0f,
                1f);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySettingsChange(
                    controller,
                    "调整高光阴影作用强度",
                    () => controller.SetSpecularShadowStrength(nextSpecularShadowStrength));
            }

            EditorGUILayout.HelpBox(
                followsMainLightOverride
                    ? "当前复用主光覆盖方向；FollowCamera 使用相机方向，Custom 使用角色自定义灯光方向。"
                    : "自定义方向为 (0, 0, 0) 时跟随场景主平行光；点击“编辑”可在 Scene 视图调整方向。Volume 显式方向 Override 仍具有更高优先级。",
                MessageType.None);
        }
    }

    private void DrawEnvironmentBody(CharacterRenderController controller)
    {
        if (controller == null)
        {
            return;
        }

        _showEnvironmentSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showEnvironmentSection, "环境光设置");
        if (_showEnvironmentSection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                bool enabled = controller.EnableGlobalIndirectLight;
                EditorGUI.BeginChangeCheck();
                bool nextEnabled = EditorGUILayout.Toggle("启用", enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplySettingsChange(controller, "调整角色环境光开关", () => controller.SetGlobalIndirectLightEnabled(nextEnabled));
                }

                using (new EditorGUI.DisabledGroupScope(!controller.EnableGlobalIndirectLight))
                {
                    bool overrideSceneAmbient = controller.OverrideSceneAmbient;
                    EditorGUI.BeginChangeCheck();
                    bool nextOverrideSceneAmbient = EditorGUILayout.Toggle(
                        new GUIContent(
                            "覆盖场景环境光",
                            "开启后忽略场景球谐颜色，使用下方“间接漫反射颜色”作为角色环境光颜色。"),
                        overrideSceneAmbient);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplySettingsChange(
                            controller,
                            "调整场景环境光覆盖开关",
                            () => controller.SetOverrideSceneAmbient(nextOverrideSceneAmbient));
                    }

                    float indirectIntensity = controller.GlobalIndirectIntensity;
                    EditorGUI.BeginChangeCheck();
                    float nextIndirectIntensity = EditorGUILayout.Slider(new GUIContent("间接漫反射强度"), indirectIntensity, 0f, 8f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplySettingsChange(
                            controller,
                            "调整角色环境光强度",
                            () => controller.SetGlobalIndirectIntensity(nextIndirectIntensity));
                    }

                    Color indirectTint = controller.GlobalIndirectTintColor;
                    EditorGUI.BeginChangeCheck();
                    Color nextIndirectTint = EditorGUILayout.ColorField(new GUIContent("间接漫反射颜色"), indirectTint);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplySettingsChange(
                            controller,
                            "调整角色环境光颜色",
                            () => controller.SetGlobalIndirectTintColor(nextIndirectTint));
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private static Vector3 ResolveScenePreviewDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            return ScenePreviewFallbackDirection;
        }

        return -worldDirection.normalized;
    }

    private void DrawLightAnchorInspector(CharacterRenderController controller)
    {
        if (controller == null)
        {
            return;
        }

        float widgetHeight = EditorGUIUtility.singleLineHeight * 5f;
        controller.GetLightAnchorAngles(out float orbit, out float elevation);

        bool orbitChanged;
        bool elevationChanged;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            orbit = AngleField(
                EditorGUILayout.GetControlRect(false, widgetHeight),
                "Orbit",
                orbit,
                90f,
                new Color(0f, 1f, 0f, 0.2f),
                true);
            orbitChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            elevation = AngleField(
                EditorGUILayout.GetControlRect(false, widgetHeight),
                "Elevation",
                elevation,
                180f,
                new Color(0f, 0f, 1f, 0.2f),
                true);
            elevationChanged = EditorGUI.EndChangeCheck();
        }

        Rect angleRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
        float[] angles = { orbit, elevation };
        EditorGUI.BeginChangeCheck();
        EditorGUI.MultiFloatField(
            angleRect,
            new GUIContent("Light Anchor"),
            new[]
            {
                new GUIContent("Orbit"),
                new GUIContent("Elevation"),
            },
            angles);

        if (EditorGUI.EndChangeCheck())
        {
            orbit = angles[0];
            elevation = angles[1];
            orbitChanged = true;
            elevationChanged = true;
        }

        if (!orbitChanged && !elevationChanged)
        {
            return;
        }

        ApplySettingsChange(controller, "调整角色自定义灯光 Light Anchor", () =>
        {
            controller.SetLightAnchorAngles(orbit, elevation);
            _directionRotation = Quaternion.FromToRotation(
                Vector3.forward,
                ResolveScenePreviewDirection(controller.GetWorldDirection()));
        });
    }

    private void DrawDirectionFieldWithEditButton(CharacterRenderController controller, string fieldLabel, string buttonLabel)
    {
        Rect position = EditorGUILayout.GetControlRect();
        Rect contentRect = new Rect(position)
        {
            width = position.width - 68f,
        };
        Rect buttonRect = new Rect(position)
        {
            x = position.xMax - 63f,
            width = 60f,
        };

        EditorGUI.BeginChangeCheck();
        Vector3 newValue = EditorGUI.Vector3Field(contentRect, fieldLabel, controller.GetWorldDirection());
        if (EditorGUI.EndChangeCheck())
        {
            ApplySettingsChange(controller, "调整角色自定义灯光方向", () =>
            {
                controller.SetWorldDirection(newValue);
                _directionRotation = Quaternion.FromToRotation(
                    Vector3.forward,
                    ResolveScenePreviewDirection(controller.GetWorldDirection()));
            });
        }

        bool previousState = _showSceneDirectionProperty.boolValue;
        Color oldColor = GUI.color;
        if (previousState)
        {
            GUI.color = Color.green;
        }

        bool nextState = GUI.Toggle(buttonRect, previousState, buttonLabel, "Button");
        GUI.color = oldColor;

        if (nextState != previousState)
        {
            SetExclusiveSceneDirectionHandle(_showSceneDirectionProperty, nextState);
            if (nextState)
            {
                _directionRotation = Quaternion.FromToRotation(
                    Vector3.forward,
                    ResolveScenePreviewDirection(controller.GetWorldDirection()));
            }

            SceneView.RepaintAll();
        }
    }

    private void DrawRimFakePointMaskPositionFieldWithEditButton(CharacterRenderController controller)
    {
        Rect position = EditorGUILayout.GetControlRect();
        Rect contentRect = new Rect(position)
        {
            width = position.width - 68f,
        };
        Rect buttonRect = new Rect(position)
        {
            x = position.xMax - 63f,
            width = 60f,
        };

        EditorGUI.BeginChangeCheck();
        Vector3 nextOffset = EditorGUI.Vector3Field(
            contentRect,
            "点灯位置偏移",
            controller.RimFakePointMaskPosition);
        if (EditorGUI.EndChangeCheck())
        {
            ApplySettingsChange(
                controller,
                "调整模拟点灯位置偏移",
                () => controller.SetRimFakePointMaskPosition(nextOffset));
        }

        bool previousState = _showRimFakePointMaskPositionProperty.boolValue;
        Color oldColor = GUI.color;
        if (previousState)
        {
            GUI.color = Color.green;
        }

        bool nextState = GUI.Toggle(buttonRect, previousState, "位置", "Button");
        GUI.color = oldColor;
        if (nextState != previousState)
        {
            SetExclusiveSceneDirectionHandle(_showRimFakePointMaskPositionProperty, nextState);
            // 此开关属于 Controller 的 Scene 可视化状态，而不是 Profile 参数。
            // 立即落盘，避免外层 Rim 参数批量提交刷新 SerializedObject 后把它恢复为旧值。
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            SceneView.RepaintAll();
        }
    }

    private Vector3 DrawRimFakeDirectMaskDirectionFieldWithEditButton(
        CharacterRenderController controller,
        Vector3 direction)
    {
        return DrawRimMaskVectorFieldWithEditButton(
            controller,
            "直接光切面方向",
            "旋转",
            direction,
            _showRimFakeDirectMaskDirectionProperty);
    }

    private Vector3 DrawRimCustomDirectionFieldWithEditButton(
        CharacterRenderController controller,
        Vector3 direction)
    {
        return DrawRimMaskVectorFieldWithEditButton(
            controller,
            "方向",
            "旋转",
            direction,
            _showRimCustomDirectionProperty);
    }

    private Vector3 DrawRimFakeDirectMaskPositionFieldWithEditButton(
        CharacterRenderController controller,
        Vector3 positionOffset)
    {
        return DrawRimMaskVectorFieldWithEditButton(
            controller,
            "直接光切面位置偏移",
            "位置",
            positionOffset,
            _showRimFakeDirectMaskPositionProperty);
    }

    private Vector3 DrawRimMaskVectorFieldWithEditButton(
        CharacterRenderController controller,
        string fieldLabel,
        string buttonLabel,
        Vector3 value,
        SerializedProperty showHandleProperty)
    {
        Rect position = EditorGUILayout.GetControlRect();
        Rect contentRect = new Rect(position)
        {
            width = position.width - 68f,
        };
        Rect buttonRect = new Rect(position)
        {
            x = position.xMax - 63f,
            width = 60f,
        };

        Vector3 nextValue = EditorGUI.Vector3Field(contentRect, fieldLabel, value);
        if (showHandleProperty == null)
        {
            return nextValue;
        }

        bool previousState = showHandleProperty.boolValue;
        Color oldColor = GUI.color;
        if (previousState)
        {
            GUI.color = Color.green;
        }

        bool nextState = GUI.Toggle(buttonRect, previousState, buttonLabel, "Button");
        GUI.color = oldColor;
        if (nextState != previousState)
        {
            SetExclusiveSceneDirectionHandle(showHandleProperty, nextState);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            SceneView.RepaintAll();
        }

        return nextValue;
    }

    private void DrawVirtualLightCameraDirectionOffsetFieldWithEditButton(CharacterRenderController controller)
    {
        Rect position = EditorGUILayout.GetControlRect();
        Rect contentRect = new Rect(position)
        {
            width = position.width - 68f,
        };
        Rect buttonRect = new Rect(position)
        {
            x = position.xMax - 63f,
            width = 60f,
        };

        EditorGUI.BeginChangeCheck();
        Vector3 offsetEuler = EditorGUI.Vector3Field(
            contentRect,
            "额外方向偏移",
            controller.VirtualLightCameraDirectionOffsetEuler);
        if (EditorGUI.EndChangeCheck())
        {
            ApplySettingsChange(
                controller,
                "调整主光额外方向偏移",
                () => controller.SetVirtualLightCameraDirectionOffsetEuler(offsetEuler));
        }

        bool previousState = _showVirtualLightCameraDirectionProperty.boolValue;
        Color oldColor = GUI.color;
        if (previousState)
        {
            GUI.color = Color.green;
        }

        bool nextState = GUI.Toggle(buttonRect, previousState, "编辑", "Button");
        GUI.color = oldColor;
        if (nextState != previousState)
        {
            SetExclusiveSceneDirectionHandle(_showVirtualLightCameraDirectionProperty, nextState);
            SceneView.RepaintAll();
        }
    }

    private Quaternion DrawShadowDirectionAnchorInspector(
        CharacterRenderController controller,
        string anchorLabel,
        string undoName,
        GetAnchorAnglesDelegate getAnchorAngles,
        Action<float, float> setAnchorAngles,
        Func<Vector3> getDirection,
        Quaternion directionRotation)
    {
        float widgetHeight = EditorGUIUtility.singleLineHeight * 5f;
        getAnchorAngles(out float orbit, out float elevation);

        bool orbitChanged;
        bool elevationChanged;
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            orbit = AngleField(
                EditorGUILayout.GetControlRect(false, widgetHeight),
                "Orbit",
                orbit,
                90f,
                new Color(0f, 1f, 0f, 0.2f),
                true);
            orbitChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            elevation = AngleField(
                EditorGUILayout.GetControlRect(false, widgetHeight),
                "Elevation",
                elevation,
                180f,
                new Color(0f, 0f, 1f, 0.2f),
                true);
            elevationChanged = EditorGUI.EndChangeCheck();
        }

        Rect angleRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
        float[] angles = { orbit, elevation };
        EditorGUI.BeginChangeCheck();
        EditorGUI.MultiFloatField(
            angleRect,
            new GUIContent(anchorLabel),
            new[]
            {
                new GUIContent("Orbit"),
                new GUIContent("Elevation"),
            },
            angles);
        if (EditorGUI.EndChangeCheck())
        {
            orbit = angles[0];
            elevation = angles[1];
            orbitChanged = true;
            elevationChanged = true;
        }

        if (!orbitChanged && !elevationChanged)
        {
            return directionRotation;
        }

        ApplySettingsChange(controller, undoName, () => setAnchorAngles(orbit, elevation));
        return Quaternion.FromToRotation(
            Vector3.forward,
            ResolveScenePreviewDirection(getDirection()));
    }

    private Quaternion DrawShadowDirectionFieldWithEditButton(
        CharacterRenderController controller,
        string fieldLabel,
        string undoName,
        Func<Vector3> getDirection,
        Action<Vector3> setDirection,
        SerializedProperty showSceneDirectionProperty,
        Quaternion directionRotation)
    {
        Rect position = EditorGUILayout.GetControlRect();
        Rect contentRect = new Rect(position)
        {
            width = position.width - 68f,
        };
        Rect buttonRect = new Rect(position)
        {
            x = position.xMax - 63f,
            width = 60f,
        };

        EditorGUI.BeginChangeCheck();
        Vector3 newValue = EditorGUI.Vector3Field(contentRect, fieldLabel, getDirection());
        if (EditorGUI.EndChangeCheck())
        {
            ApplySettingsChange(controller, undoName, () => setDirection(newValue));
            directionRotation = Quaternion.FromToRotation(
                Vector3.forward,
                ResolveScenePreviewDirection(getDirection()));
        }

        if (showSceneDirectionProperty == null)
        {
            return directionRotation;
        }

        bool previousState = showSceneDirectionProperty.boolValue;
        Color oldColor = GUI.color;
        if (previousState)
        {
            GUI.color = Color.green;
        }

        bool nextState = GUI.Toggle(buttonRect, previousState, "编辑", "Button");
        GUI.color = oldColor;
        if (nextState == previousState)
        {
            return directionRotation;
        }

        SetExclusiveSceneDirectionHandle(showSceneDirectionProperty, nextState);
        if (nextState)
        {
            directionRotation = Quaternion.FromToRotation(
                Vector3.forward,
                ResolveScenePreviewDirection(getDirection()));
        }

        SceneView.RepaintAll();
        return directionRotation;
    }

    private void SetExclusiveSceneDirectionHandle(SerializedProperty activeProperty, bool enabled)
    {
        if (enabled)
        {
            if (_showSceneDirectionProperty != null)
            {
                _showSceneDirectionProperty.boolValue = false;
            }

            if (_showPlanarShadowSceneDirectionProperty != null)
            {
                _showPlanarShadowSceneDirectionProperty.boolValue = false;
            }

            if (_showPerObjectShadowSceneDirectionProperty != null)
            {
                _showPerObjectShadowSceneDirectionProperty.boolValue = false;
            }

            if (_showRimCustomDirectionProperty != null)
            {
                _showRimCustomDirectionProperty.boolValue = false;
            }

            if (_showVirtualLightCameraDirectionProperty != null)
            {
                _showVirtualLightCameraDirectionProperty.boolValue = false;
            }

            if (_showRimFakePointMaskPositionProperty != null)
            {
                _showRimFakePointMaskPositionProperty.boolValue = false;
            }

            if (_showRimFakeDirectMaskDirectionProperty != null)
            {
                _showRimFakeDirectMaskDirectionProperty.boolValue = false;
            }

            if (_showRimFakeDirectMaskPositionProperty != null)
            {
                _showRimFakeDirectMaskPositionProperty.boolValue = false;
            }
        }

        activeProperty.boolValue = enabled;
    }

    private void EnforceExclusiveSceneDirectionHandles()
    {
        if (_showSceneDirectionProperty == null
            || _showPlanarShadowSceneDirectionProperty == null
            || _showPerObjectShadowSceneDirectionProperty == null
            || _showVirtualLightCameraDirectionProperty == null
            || _showRimCustomDirectionProperty == null
            || _showRimFakePointMaskPositionProperty == null
            || _showRimFakeDirectMaskDirectionProperty == null
            || _showRimFakeDirectMaskPositionProperty == null)
        {
            return;
        }

        int activeCount = 0;
        if (_showSceneDirectionProperty.boolValue)
        {
            activeCount++;
        }

        if (_showPlanarShadowSceneDirectionProperty.boolValue)
        {
            activeCount++;
        }

        if (_showPerObjectShadowSceneDirectionProperty.boolValue)
        {
            activeCount++;
        }

        if (_showVirtualLightCameraDirectionProperty.boolValue)
        {
            activeCount++;
        }

        if (_showRimCustomDirectionProperty.boolValue)
        {
            activeCount++;
        }

        if (_showRimFakePointMaskPositionProperty.boolValue)
        {
            activeCount++;
        }

        if (_showRimFakeDirectMaskDirectionProperty.boolValue)
        {
            activeCount++;
        }

        if (_showRimFakeDirectMaskPositionProperty.boolValue)
        {
            activeCount++;
        }

        if (activeCount <= 1)
        {
            return;
        }

        if (_showSceneDirectionProperty.boolValue)
        {
            SetExclusiveSceneDirectionHandle(_showSceneDirectionProperty, true);
        }
        else if (_showPlanarShadowSceneDirectionProperty.boolValue)
        {
            SetExclusiveSceneDirectionHandle(_showPlanarShadowSceneDirectionProperty, true);
        }
        else if (_showPerObjectShadowSceneDirectionProperty.boolValue)
        {
            SetExclusiveSceneDirectionHandle(_showPerObjectShadowSceneDirectionProperty, true);
        }
        else if (_showRimCustomDirectionProperty.boolValue)
        {
            SetExclusiveSceneDirectionHandle(_showRimCustomDirectionProperty, true);
        }
        else if (_showRimFakePointMaskPositionProperty.boolValue)
        {
            SetExclusiveSceneDirectionHandle(_showRimFakePointMaskPositionProperty, true);
        }
        else if (_showRimFakeDirectMaskDirectionProperty.boolValue)
        {
            SetExclusiveSceneDirectionHandle(_showRimFakeDirectMaskDirectionProperty, true);
        }
        else if (_showRimFakeDirectMaskPositionProperty.boolValue)
        {
            SetExclusiveSceneDirectionHandle(_showRimFakeDirectMaskPositionProperty, true);
        }
        else
        {
            SetExclusiveSceneDirectionHandle(_showVirtualLightCameraDirectionProperty, true);
        }
    }

    private void UpdateTransformToolVisibility(CharacterRenderController controller)
    {
        bool shouldHideTransformTools = controller != null
            && ((controller.EnableRimFakePointMask
                    && IsEnabled(_showRimFakePointMaskPositionProperty))
                || (controller.EnableRimFakeDirectMask
                    && IsEnabled(_showRimFakeDirectMaskPositionProperty)));
        if (shouldHideTransformTools)
        {
            if (_overridesTransformToolVisibility)
            {
                return;
            }

            _transformToolsWereHidden = Tools.hidden;
            Tools.hidden = true;
            _overridesTransformToolVisibility = true;
            return;
        }

        RestoreTransformToolVisibility();
    }

    private static bool IsEnabled(SerializedProperty property)
    {
        return property != null && property.boolValue;
    }

    private void RestoreTransformToolVisibility()
    {
        if (!_overridesTransformToolVisibility)
        {
            return;
        }

        Tools.hidden = _transformToolsWereHidden;
        _overridesTransformToolVisibility = false;
    }

    private static UnityEngine.Object GetSettingsTarget(CharacterRenderController controller)
    {
        if (controller == null)
        {
            return null;
        }

        return controller.Profile != null ? controller.Profile : controller;
    }

    private void ApplySettingsChange(CharacterRenderController controller, string undoName, Action applyAction)
    {
        if (controller == null || applyAction == null)
        {
            return;
        }

        UnityEngine.Object settingsTarget = GetSettingsTarget(controller);
        if (settingsTarget == null)
        {
            return;
        }

        Undo.RecordObject(settingsTarget, undoName);
        applyAction();
        EditorUtility.SetDirty(settingsTarget);
        serializedObject.UpdateIfRequiredOrScript();
        RepaintAfterSettingsChange();
    }

    private void HandleProfileReferenceChanged(CharacterRenderController controller, CharacterRenderLightProfile previousProfile)
    {
        if (controller == null)
        {
            return;
        }

        if (controller.Profile == null && previousProfile != null)
        {
            Undo.RecordObject(controller, "移除角色自定义灯光配置");
            controller.CopyProfileSettingsToLocal(previousProfile);
            EditorUtility.SetDirty(controller);
        }

        controller.ApplyNow();
        _directionRotation = Quaternion.FromToRotation(
            Vector3.forward,
            ResolveScenePreviewDirection(controller.GetWorldDirection()));
        _planarShadowDirectionRotation = Quaternion.FromToRotation(
            Vector3.forward,
            ResolveScenePreviewDirection(controller.GetPlanarShadowDirection()));
        _highQualityShadowDirectionRotation = Quaternion.FromToRotation(
            Vector3.forward,
            ResolveScenePreviewDirection(controller.GetHighQualityShadowDirection()));
        RepaintAfterSettingsChange();
    }

    private void RepaintAfterSettingsChange()
    {
        SceneView.RepaintAll();
        Repaint();
    }

    private void CreateProfileAsset(CharacterRenderController controller)
    {
        if (controller == null)
        {
            return;
        }

        EnsureFolder(ProfileAssetDirectory);
        if (!AssetDatabase.IsValidFolder(ProfileAssetDirectory))
        {
            EditorUtility.DisplayDialog(
                "创建角色自定义灯光配置",
                $"目标目录不存在：{ProfileAssetDirectory}",
                "确定");
            return;
        }

        string sceneName = controller.gameObject.scene.IsValid() && !string.IsNullOrWhiteSpace(controller.gameObject.scene.name)
            ? controller.gameObject.scene.name
            : string.Empty;
        string assetFileName = string.IsNullOrWhiteSpace(sceneName)
            ? DefaultProfileName
            : $"{sceneName}_{DefaultProfileName}";
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{ProfileAssetDirectory}/{assetFileName}.asset");

        CharacterRenderLightProfile profile = CreateInstance<CharacterRenderLightProfile>();
        controller.CopyResolvedSettingsToProfile(profile);

        AssetDatabase.CreateAsset(profile, assetPath);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(controller, "创建角色自定义灯光配置");
        controller.Profile = profile;
        EditorUtility.SetDirty(controller);
        serializedObject.UpdateIfRequiredOrScript();

        _directionRotation = Quaternion.FromToRotation(
            Vector3.forward,
            ResolveScenePreviewDirection(controller.GetWorldDirection()));
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);
        RepaintAfterSettingsChange();
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        if (parts.Length == 0)
        {
            return;
        }

        string currentPath = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private static AngleFieldState GetAngleFieldState(int id)
    {
        return (AngleFieldState)GUIUtility.GetStateObject(typeof(AngleFieldState), id);
    }

    private static float AngleField(Rect knobRect, string label, float angle, float offset, Color sectionColor, bool enabled)
    {
        int id = GUIUtility.GetControlID("CharacterRenderAngleSlider".GetHashCode(), FocusType.Passive);
        AngleFieldState state = GetAngleFieldState(id);

        using (new GUI.GroupScope(knobRect))
        {
            Rect localRect = new Rect(0f, 0f, knobRect.width, knobRect.height);
            GUI.Box(localRect, GUIContent.none, EditorStyles.helpBox);

            Rect labelRect = new Rect(0f, 2f, localRect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label, EditorStyles.centeredGreyMiniLabel);
        }

        Rect diskRect = new Rect(
            knobRect.x + 8f,
            knobRect.y + EditorGUIUtility.singleLineHeight + 4f,
            knobRect.width - 16f,
            knobRect.height - EditorGUIUtility.singleLineHeight - 12f);

        if (Event.current.type == EventType.Repaint)
        {
            state.Radius = Mathf.Min(diskRect.width, diskRect.height) * 0.5f;
            state.Position = diskRect.center;
        }

        if (Math.Abs(state.Radius) < Mathf.Epsilon)
        {
            return angle;
        }

        float newAngle;
        bool didReset = GUIUtility.hotControl == 0
            && Event.current.type == EventType.MouseDown
            && Event.current.button == 1
            && diskRect.Contains(Event.current.mousePosition);

        if (didReset)
        {
            newAngle = 0f;
            Event.current.Use();
            GUI.changed = true;
        }
        else if (enabled)
        {
            Vector2 srcPos = new Vector2(
                Mathf.Cos((angle + offset) * Mathf.Deg2Rad),
                Mathf.Sin((angle + offset) * Mathf.Deg2Rad)) * state.Radius + state.Position;

            Vector2 dstPos = Slider2DCircular(id, srcPos, 5f, Handles.CircleHandleCap);
            dstPos -= state.Position;
            dstPos.Normalize();

            newAngle = NormalizeAngle(Mathf.Atan2(dstPos.y, dstPos.x) * Mathf.Rad2Deg - offset);
            newAngle = Mathf.Round(newAngle * 100.0f) / 100.0f;
        }
        else
        {
            newAngle = 0f;
        }

        if (Event.current.type == EventType.Repaint)
        {
            DrawAngleWidget(state.Position, state.Radius, newAngle, offset, sectionColor, enabled);
        }

        return newAngle;
    }

    private static void DrawAngleWidget(Vector2 center, float radius, float angleDegrees, float offset, Color sectionColor, bool enabled)
    {
        Vector2 originPosition = center + new Vector2(
            Mathf.Cos(offset * Mathf.Deg2Rad),
            Mathf.Sin(offset * Mathf.Deg2Rad)) * radius;

        Vector2 toOrigin = originPosition - center;

        Vector2 handlePosition = center + new Vector2(
            Mathf.Cos((angleDegrees + offset) * Mathf.Deg2Rad),
            Mathf.Sin((angleDegrees + offset) * Mathf.Deg2Rad)) * radius;

        Color backupColor = Handles.color;
        Handles.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        Handles.DrawSolidDisc(center, Vector3.forward, radius);
        Handles.color = new Color(0f, 0f, 0f, 0.65f);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
        Handles.color = enabled ? sectionColor : new Color(sectionColor.r, sectionColor.g, sectionColor.b, 0.08f);
        Handles.DrawSolidArc(
            center,
            Vector3.forward,
            Quaternion.AngleAxis(offset, Vector3.forward) * Vector3.right,
            angleDegrees,
            radius);
        Handles.color = new Color(1f, 1f, 1f, 0.85f);
        Handles.DrawLine(center + toOrigin * 0.75f, center + toOrigin * 0.9f);
        Handles.DrawLine(center, handlePosition);
        Handles.DrawSolidDisc(handlePosition, Vector3.forward, 5f);
        Handles.color = backupColor;
    }

    private static Vector2 Slider2DCircular(int id, Vector2 position, float size, Handles.CapFunction drawCapFunction)
    {
        EventType type = Event.current.GetTypeForControl(id);

        switch (type)
        {
            case EventType.MouseDown:
                if (Event.current.button == 0 && HandleUtility.nearestControl == id && !Event.current.alt)
                {
                    GUIUtility.keyboardControl = id;
                    GUIUtility.hotControl = id;
                    s_CurrentMousePosition = Event.current.mousePosition;
                    s_DragStartScreenPosition = Event.current.mousePosition;
                    Vector2 guiPoint = HandleUtility.WorldToGUIPoint(position);
                    s_DragScreenOffset = s_CurrentMousePosition - guiPoint;
                    EditorGUIUtility.SetWantsMouseJumping(1);
                    Event.current.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id && (Event.current.button == 0 || Event.current.button == 2))
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(0);
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    s_CurrentMousePosition = Event.current.mousePosition;
                    Vector2 center = position;
                    position = Handles.inverseMatrix.MultiplyPoint(s_CurrentMousePosition - s_DragScreenOffset);
                    if (!Mathf.Approximately((center - position).magnitude, 0f))
                    {
                        GUI.changed = true;
                    }
                    Event.current.Use();
                }
                break;

            case EventType.KeyDown:
                if (GUIUtility.hotControl == id && Event.current.keyCode == KeyCode.Escape)
                {
                    GUIUtility.hotControl = 0;
                    position = Handles.inverseMatrix.MultiplyPoint(s_DragStartScreenPosition - s_DragScreenOffset);
                    GUI.changed = true;
                    Event.current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(0);
                }
                break;

            case EventType.Layout:
                HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, size));
                break;

            case EventType.Repaint:
                Handles.color = GUIUtility.hotControl == id ? Handles.selectedColor : Handles.color;
                drawCapFunction(id, position, Quaternion.identity, size, EventType.Repaint);
                break;
        }

        return position;
    }

    private static float NormalizeAngle(float angle)
    {
        const float range = 360f;
        const float startValue = -180f;
        float offset = angle - startValue;
        return offset - Mathf.Floor(offset / range) * range + startValue;
    }
}
