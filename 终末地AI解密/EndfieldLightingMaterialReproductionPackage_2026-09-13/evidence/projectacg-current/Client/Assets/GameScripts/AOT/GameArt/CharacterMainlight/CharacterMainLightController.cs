using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CharacterMainLightController : MonoBehaviour
{
    private enum SceneShadowMode
    {
        UnityOnly = 0,
        Both = 1,
        POSOnly = 2,
        Off = 3,
    }

    private enum TargetMaterialKind
    {
        Unsupported = 0,
        Character = 1,
        Monster = 2,
        Gun = 3,
    }

    private const float MinDirectionMagnitude = 0.0001f;
    private const float DirectionComponentMin = -1f;
    private const float DirectionComponentMax = 1f;
    private const float DirectionYMin = 0.1f;
    private const float DirectionYMax = 1f;
    private const string CustomMainLightKeyword = "_CUSTOMMAINLIGHT_ON";
    private const string CustomEnvironmentLightKeyword = "_CUSTOMENVIRONMENTLIGHT_ON";
    private const string CharacterShaderPrefix = "Valkyria/Chara/Chara_";
    private const string MonsterShaderName = "Valkyria/Chara/Monst_PBR";
    private const string GunShaderName = "Valkyria/Chara/Gun_PBR";
    private const string HairShaderName = "Valkyria/Chara/Chara_Hair";
    private const string FringeShaderName = "Valkyria/Chara/Chara_Fringe";
    private const string CharacterLayerName = "Characters";
    private const string MonsterLayerName = "Monster";
    private const string WeaponLayerName = "Weapon";
    private const string CustomMainLightPerObjectShadowDirectionEnabledName = "_CustomMainLightPerObjectShadowDirectionEnabled";
    private const string CustomMainLightPerObjectShadowDirectionName = "_CustomMainLightPerObjectShadowDirection";
    private const int RuntimeTargetSlotBufferCapacity = 4096;
    private const int RuntimeRendererBufferCapacity = 2048;
    private const int RuntimeBoundsOwnerBufferCapacity = 512;
    private const int MaterialKeywordStateCustomMainLightKnown = 1 << 0;
    private const int MaterialKeywordStateCustomMainLightEnabled = 1 << 1;
    private const int MaterialKeywordStateCustomEnvironmentKnown = 1 << 2;
    private const int MaterialKeywordStateCustomEnvironmentEnabled = 1 << 3;

    private static readonly int GlobalCustomMainLightDirId = Shader.PropertyToID("_GlobalCustomMainLightDir");
    private static readonly int GlobalCustomMainLightColorId = Shader.PropertyToID("_GlobalCustomMainLightColor");
    private static readonly int GlobalCustomMainLightIntensityId = Shader.PropertyToID("_GlobalCustomMainLightIntensity");
    private static readonly int CustomEnvironmentLightId = Shader.PropertyToID("_customEnvironmentLight");
    private static readonly int CustomEnvColorId = Shader.PropertyToID("_CustomEnvColor");
    private static readonly int CustomEnvDiffLerpId = Shader.PropertyToID("_CustomEnvDiffLerp");
    private static readonly int CustomEnvDiffIntensityId = Shader.PropertyToID("_CustomEnvDiffIntensity");
    private static readonly int CustomEnvSpecIntensityId = Shader.PropertyToID("_CustomEnvSpecIntensity");
    private static readonly int IndirSpecIntensityId = Shader.PropertyToID("_IndirSpecIntensity");
    private static readonly int CustomMainLightDirectionId = Shader.PropertyToID("_CustomMainLightDirection");
    private static readonly int AdditionalLightsIntensityId = Shader.PropertyToID("_AdditionalLightsIntensity");
    private static readonly int UsePerObjectShadowId = Shader.PropertyToID("_UsePerObjectShadow");
    private static readonly int PlanarShadowFalloffId = Shader.PropertyToID("_PlanarShadowFalloff");
    private static readonly int PlanarShadowRangeId = Shader.PropertyToID("_PlanarShadowRange");
    private static readonly int PlanarShadowLightDirId = Shader.PropertyToID("_PlanarShadowLightDir");
    private static readonly int PlanarShadowGlobalCenterId = Shader.PropertyToID("_PlanarShadowGlobalCenter");
    private static readonly int PlanarShadowColorId = Shader.PropertyToID("_PlanarShadowColor");
    private static readonly int CustomPerObjectShadowDirectionEnabledId = Shader.PropertyToID(CustomMainLightPerObjectShadowDirectionEnabledName);
    private static readonly int CustomPerObjectShadowDirectionId = Shader.PropertyToID(CustomMainLightPerObjectShadowDirectionName);
    private static readonly ProfilerMarker s_beginCameraRenderingProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.BeginCameraRendering");
    private static readonly ProfilerMarker s_applyNowProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.ApplyNow");
    private static readonly ProfilerMarker s_refreshTargetsProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.RefreshTargets");
    private static readonly ProfilerMarker s_refreshTargetsGetRootObjectsProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.RefreshTargets.GetRootObjects");
    private static readonly ProfilerMarker s_refreshTargetsCollectRootProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.RefreshTargets.CollectRoot");
    private static readonly ProfilerMarker s_refreshTargetsGetRenderersProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.RefreshTargets.GetRenderers");
    private static readonly ProfilerMarker s_refreshTargetsGetSharedMaterialsProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.RefreshTargets.GetSharedMaterials");
    private static readonly ProfilerMarker s_refreshTargetsMaterialFilterProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.RefreshTargets.MaterialFilter");
    private static readonly ProfilerMarker s_refreshTargetsResolveBoundsOwnerProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.RefreshTargets.ResolveBoundsOwner");
    private static readonly ProfilerMarker s_refreshTargetsConfigureMaterialProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.RefreshTargets.ConfigureMaterial");
    private static readonly ProfilerMarker s_refreshTargetsUpdateSlotsProfilerMarker =
        new ProfilerMarker("ProjectACG.Render.CharacterMainLight.RefreshTargets.UpdateSlots");

    private static readonly List<Transform> s_runtimeTargetRoots = new List<Transform>();
    private static readonly List<CharacterMainLightController> s_activeControllers =
        new List<CharacterMainLightController>();
    private static int s_runtimeTargetRootsVersion;
    private static int s_runtimeTargetLayerMask = -1;

    private readonly List<TargetSlot> _targetSlots = new List<TargetSlot>(RuntimeTargetSlotBufferCapacity);
    private readonly List<TargetSlot> _scratchTargetSlots = new List<TargetSlot>(RuntimeTargetSlotBufferCapacity);
    private readonly List<GameObject> _scratchRootObjects = new List<GameObject>(32);
    private readonly List<Renderer> _scratchRenderers = new List<Renderer>(RuntimeRendererBufferCapacity);
    private readonly List<Material> _scratchSharedMaterials = new List<Material>(8);
    private readonly Dictionary<Transform, Bounds> _planarShadowBoundsByOwner = new Dictionary<Transform, Bounds>(RuntimeBoundsOwnerBufferCapacity);
    private readonly Dictionary<Shader, TargetMaterialKind> _targetMaterialKindByShader = new Dictionary<Shader, TargetMaterialKind>(8);
    private readonly Dictionary<Shader, bool> _hairOrFringeShaderByShader = new Dictionary<Shader, bool>(4);
    private readonly Dictionary<Material, int> _materialKeywordStateByMaterial = new Dictionary<Material, int>(128);

    private MaterialPropertyBlock _propertyBlock;
    private bool _targetsRefreshInitialized;
    private int _lastRuntimeTargetRefreshFrame = -1;
    private int _lastRuntimeTargetRefreshVersion = -1;
    private int _targetRendererLayerMask;
    private int _targetSlotsVersion;
    private int _lastAppliedTargetSlotsVersion = -1;
    private bool _hasAppliedRuntimeProperties;
    private bool _hasAppliedCameraState;
    private Camera _lastAppliedSourceCamera;
    private Quaternion _lastAppliedSourceCameraRotation;
    private int _lastAppliedCameraFrame = -1;
    private bool _lastAppliedUseCameraDirection;
    private Vector3 _lastAppliedLightRotationOffsetEuler;
    private Color _lastAppliedMainLightColor;
    private float _lastAppliedMainLightIntensity;
    private Vector3 _lastAppliedMainLightDirection;
    private bool _lastAppliedEnableCustomMainLightOnMaterials;
    private bool _lastAppliedEnableMonsterCustomMainLightOnMaterials;
    private bool _lastAppliedEnableGunCustomMainLightOnMaterials;
    private bool _lastAppliedEnableAdditionalLights;
    private SceneShadowMode _lastAppliedSceneShadowMode;
    private bool _lastAppliedEnableCharacterSceneShadow;
    private bool _lastAppliedEnableHairFringeSelfShadow;
    private bool _lastAppliedEnableMonsterSceneShadow;
    private bool _lastAppliedEnableGunSceneShadow;
    private bool _lastAppliedOverridePerObjectShadowDirection;
    private Vector3 _lastAppliedPerObjectShadowDirection;
    private bool _lastAppliedEnablePlanarShadow;
    private bool _lastAppliedEnableCharacterPlanarShadow;
    private bool _lastAppliedEnableMonsterPlanarShadow;
    private bool _lastAppliedEnableGunPlanarShadow;
    private float _lastAppliedPlanarShadowFalloff;
    private float _lastAppliedPlanarShadowRange;
    private Vector3 _lastAppliedPlanarShadowLightDirection;
    private bool _lastAppliedUsePlanarShadowCameraFacingDirection;
    private float _lastAppliedPlanarShadowPlaneHeight;
    private bool _lastAppliedUsePlanarShadowBoundsCenter;
    private Color _lastAppliedPlanarShadowColor;
    private bool _lastAppliedEnableCustomEnvironmentLightOnMaterials;
    private bool _lastAppliedEnableMonsterCustomEnvironmentLightOnMaterials;
    private bool _lastAppliedEnableGunCustomEnvironmentLightOnMaterials;
    private Color _lastAppliedCustomEnvironmentColor;
    private float _lastAppliedCustomEnvironmentLightBlend;
    private float _lastAppliedIndirectSpecularIntensity;

    [SerializeField] private CharacterMainLightProfile _profile;
    [SerializeField] [HideInInspector] private int _activeConfigurationIndex = -1;
    [SerializeField] [HideInInspector] private string _configurationName = "Hall";

    // Target search behavior is intentionally fixed for the scene-level controller.
    [SerializeField] [HideInInspector] private Transform _searchRoot;
    private Transform _runtimeTargetRoot;
    private bool _hasRuntimeTargetRootOverride;
    [SerializeField] [HideInInspector] private bool _includeInactive;
    [SerializeField] [HideInInspector] private bool _autoRefreshTargets;
    [SerializeField] [HideInInspector] private bool _applyEveryFrame;

    [Header("主光设置")]
    [SerializeField] private bool _useCameraDirection;
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private Vector3 _lightRotationOffsetEuler = Vector3.zero;
    [SerializeField] private Color _mainLightColor = Color.white;
    [SerializeField] [Range(0f, 10f)] private float _mainLightIntensity = 1f;
    [SerializeField] private Vector3 _mainLightDirection = Vector3.up;
    [SerializeField] private bool _enableCustomMainLightOnMaterials = true;
    [SerializeField] private bool _enableMonsterCustomMainLightOnMaterials = true;
    [SerializeField] private bool _enableGunCustomMainLightOnMaterials = true;
    [SerializeField] private bool _enableAdditionalLights = true;

    [Header("阴影设置")]
    [SerializeField] private SceneShadowMode _sceneShadowMode = SceneShadowMode.UnityOnly;
    [SerializeField] private bool _enableCharacterSceneShadow = true;
    [SerializeField] private bool _enableHairFringeSelfShadow;
    [SerializeField] private bool _enableMonsterSceneShadow = true;
    [SerializeField] private bool _enableGunSceneShadow = true;
    [SerializeField] private bool _overridePerObjectShadowDirection;
    [SerializeField] private Vector3 _perObjectShadowDirection = Vector3.up;
    [SerializeField] [HideInInspector] private bool _showPerObjectShadowSceneDirection;
    [SerializeField] [HideInInspector] [Min(0.1f)] private float _perObjectShadowSceneHandleLength = 1.5f;
    [SerializeField] [HideInInspector] private Color _perObjectShadowSceneHandleColor = new Color(0.25f, 0.7f, 1f, 1f);

    [Header("平面阴影设置")]
    [SerializeField] private bool _enablePlanarShadow = true;
    [SerializeField] private bool _enableCharacterPlanarShadow = true;
    [SerializeField] private bool _enableMonsterPlanarShadow = true;
    [SerializeField] private bool _enableGunPlanarShadow = true;
    [SerializeField] [Min(0f)] private float _planarShadowFalloff = 1f;
    [SerializeField] private float _planarShadowRange = 0f;
    [SerializeField] private Vector3 _planarShadowLightDirection = Vector3.up;
    [SerializeField] private bool _usePlanarShadowCameraFacingDirection;
    [SerializeField] private Camera _planarShadowTargetCamera;
    [SerializeField] [HideInInspector] private bool _showPlanarShadowSceneDirection;
    [SerializeField] [HideInInspector] [Min(0.1f)] private float _planarShadowSceneHandleLength = 1.5f;
    [SerializeField] [HideInInspector] private Color _planarShadowSceneHandleColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float _planarShadowPlaneHeight = 0f;
    [SerializeField] private bool _usePlanarShadowBoundsCenter = true;
    [SerializeField] private Color _planarShadowColor = new Color(0f, 0f, 0f, 1f);

    [Header("环境设置")]
    [SerializeField] private bool _enableCustomEnvironmentLightOnMaterials = true;
    [SerializeField] private bool _enableMonsterCustomEnvironmentLightOnMaterials = true;
    [SerializeField] private bool _enableGunCustomEnvironmentLightOnMaterials = true;
    [SerializeField] private Color _customEnvironmentColor = Color.white;
    [SerializeField] [Min(0f)] private float _customEnvironmentLightBlend;
    [SerializeField] [Min(0f)] private float _indirectSpecularIntensity = 1f;
    [SerializeField] private bool _enableCustomEnvironmentCubemap;
    [SerializeField] private Cubemap _customEnvironmentCubemap;
    [SerializeField] [Range(-1f, 1f)] private float _customEnvironmentCubemapRotation;
    [SerializeField] [Min(0f)] private float _customEnvironmentCubemapIntensity;

    [SerializeField] [HideInInspector] private bool _showSceneDirection;
    [SerializeField] [HideInInspector] [Min(0.1f)] private float _sceneHandleLength = 1.5f;
    [SerializeField] [HideInInspector] private Color _sceneHandleColor = new Color(1f, 0.85f, 0.2f, 1f);

    public bool ShowSceneDirection => _showSceneDirection;

    public float SceneHandleLength => Mathf.Max(0.1f, _sceneHandleLength);

    public Color SceneHandleColor => _sceneHandleColor;

    public bool UsesCameraDirection => _useCameraDirection;

    public bool ShowPlanarShadowSceneDirection => _showPlanarShadowSceneDirection;

    public float PlanarShadowSceneHandleLength => Mathf.Max(0.1f, _planarShadowSceneHandleLength);

    public Color PlanarShadowSceneHandleColor => _planarShadowSceneHandleColor;

    public bool UsesPlanarShadowCameraFacingDirection => _usePlanarShadowCameraFacingDirection;

    public bool EnablePlanarShadow => _enablePlanarShadow;

    public bool OverridePerObjectShadowDirection => _overridePerObjectShadowDirection;

    public bool ShowPerObjectShadowSceneDirection => _showPerObjectShadowSceneDirection;

    public float PerObjectShadowSceneHandleLength => Mathf.Max(0.1f, _perObjectShadowSceneHandleLength);

    public Color PerObjectShadowSceneHandleColor => _perObjectShadowSceneHandleColor;

    public CharacterMainLightProfile Profile
    {
        get => _profile;
        set => _profile = value;
    }

    public int ConfigurationCount => _profile != null ? _profile.ConfigurationCount : 0;

    public int ActiveConfigurationIndex => _activeConfigurationIndex;

    public string ConfigurationName
    {
        get => CharacterMainLightProfile.SanitizeConfigurationName(_configurationName);
        set => _configurationName = CharacterMainLightProfile.SanitizeConfigurationName(value);
    }

    public string GetConfigurationName(int index)
    {
        if (_profile == null || !_profile.TryGetConfiguration(index, out CharacterMainLightConfiguration configuration))
        {
            return string.Empty;
        }

        return configuration.DisplayName;
    }

    public static void RegisterTargetRoot(Transform targetRoot)
    {
        if (targetRoot == null)
        {
            return;
        }

        PurgeRuntimeTargetRoots();

        for (int i = s_runtimeTargetRoots.Count - 1; i >= 0; i--)
        {
            Transform registeredRoot = s_runtimeTargetRoots[i];
            if (registeredRoot == targetRoot || IsSameOrChildOf(targetRoot, registeredRoot))
            {
                // Register also acts as a lifecycle notification. A pooled object or a new renderer can
                // appear below an already covered root without changing the root list itself.
                MarkRuntimeTargetsChanged();
                return;
            }

            if (IsSameOrChildOf(registeredRoot, targetRoot))
            {
                s_runtimeTargetRoots.RemoveAt(i);
            }
        }

        s_runtimeTargetRoots.Add(targetRoot);
        MarkRuntimeTargetsChanged();
    }

    public static void UnregisterTargetRoot(Transform targetRoot)
    {
        bool removed = false;
        for (int i = s_runtimeTargetRoots.Count - 1; i >= 0; i--)
        {
            Transform registeredRoot = s_runtimeTargetRoots[i];
            if (registeredRoot == null || (targetRoot != null && registeredRoot == targetRoot))
            {
                s_runtimeTargetRoots.RemoveAt(i);
                removed = true;
            }
        }

        if (removed)
        {
            MarkRuntimeTargetsChanged();
        }
    }

    public static void NotifyTargetsChanged()
    {
        MarkRuntimeTargetsChanged();
    }

    /// <summary>
    /// Restricts this controller instance to one runtime hierarchy instead of the shared global target roots.
    /// Preview light rigs use this to avoid writing to characters owned by other light rigs.
    /// </summary>
    public void SetTargetRoot(Transform targetRoot)
    {
        if (isActiveAndEnabled && _targetsRefreshInitialized)
        {
            // An active prefab is enabled once by the resource loader before its runtime owner can bind
            // a local target. Undo that first scene-wide write before switching the controller to local mode.
            RestoreOwnedPropertyDefaults();
            InvalidateActiveControllersForHandoff();
        }

        _runtimeTargetRoot = targetRoot;
        _hasRuntimeTargetRootOverride = true;
        _targetsRefreshInitialized = false;
        _lastRuntimeTargetRefreshFrame = -1;
        _lastRuntimeTargetRefreshVersion = -1;
        InvalidateAppliedState();

        if (!isActiveAndEnabled)
        {
            return;
        }

        RefreshTargetsInternal();
        ApplyNow();
    }

    private void Reset()
    {
        ApplyHiddenTargetSettingsDefaults();
        RefreshTargets();
        ApplyCurrentDirection();
    }

    private void OnEnable()
    {
        RegisterActiveController(this);
        ApplyHiddenTargetSettingsDefaults();
        RefreshTargets();
        RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
#if UNITY_EDITOR
        CharacterMainLightProfile.ProfileChanged -= HandleProfileChanged;
        CharacterMainLightProfile.ProfileChanged += HandleProfileChanged;
        SceneView.duringSceneGui += HandleDuringSceneGui;
        EditorApplication.hierarchyChanged += HandleEditorHierarchyChanged;
        RequestEditorCameraRefresh();
#endif
        ApplyCurrentDirection();
    }

    private void OnDisable()
    {
        UnregisterActiveController(this);
        ClearPerObjectShadowDirectionGlobal();
        RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
#if UNITY_EDITOR
        CharacterMainLightProfile.ProfileChanged -= HandleProfileChanged;
        SceneView.duringSceneGui -= HandleDuringSceneGui;
        EditorApplication.hierarchyChanged -= HandleEditorHierarchyChanged;
#endif
        RestoreOwnedPropertyDefaults();
        InvalidateAppliedState();
        InvalidateActiveControllersForHandoff();
    }

    private void OnValidate()
    {
        ApplyHiddenTargetSettingsDefaults();
        _mainLightIntensity = Mathf.Max(0f, _mainLightIntensity);
        _customEnvironmentLightBlend = Mathf.Max(0f, _customEnvironmentLightBlend);
        _indirectSpecularIntensity = Mathf.Max(0f, _indirectSpecularIntensity);
        _sceneHandleLength = Mathf.Max(0.1f, _sceneHandleLength);
        _planarShadowFalloff = Mathf.Max(0f, _planarShadowFalloff);
        _planarShadowSceneHandleLength = Mathf.Max(0.1f, _planarShadowSceneHandleLength);
        _perObjectShadowSceneHandleLength = Mathf.Max(0.1f, _perObjectShadowSceneHandleLength);
        _activeConfigurationIndex = Mathf.Clamp(_activeConfigurationIndex, -1, ConfigurationCount - 1);
        _configurationName = ConfigurationName;
        SanitizeDirection();
        SanitizePerObjectShadowDirection();
        SanitizePlanarShadowDirection();

        if (!isActiveAndEnabled)
        {
            return;
        }

        RefreshTargets();
        ApplyCurrentDirection();
#if UNITY_EDITOR
        RequestEditorCameraRefresh();
#endif
    }

    private void OnTransformChildrenChanged()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        RefreshTargets();
        ApplyCurrentDirection();
    }

    private void LateUpdate()
    {
        bool usesCameraDrivenData = _useCameraDirection || (_enablePlanarShadow && _usePlanarShadowCameraFacingDirection);
        bool targetsChanged = !usesCameraDrivenData && Application.isPlaying
            ? RefreshTargetsAtRuntimeIfNeeded()
            : false;

        if (usesCameraDrivenData)
        {
            if (!Application.isPlaying)
            {
                ApplyNow();
            }

            transform.hasChanged = false;
            return;
        }

        if (_applyEveryFrame || targetsChanged || HaveRuntimePropertiesChangedSinceLastApply())
        {
            ApplyNow();
        }
    }

    [ContextMenu("Refresh Character Main Light Targets")]
    public void RefreshTargets()
    {
        RefreshTargetsInternal();
    }

    [ContextMenu("Apply Character Main Light")]
    public void ApplyNow()
    {
        ApplyNow(null);
    }

    public void ApplyNow(Camera currentCamera)
    {
        using var profileScope = s_applyNowProfilerMarker.Auto();

        ApplyPerObjectShadowDirectionGlobal();

        if (!_targetsRefreshInitialized)
        {
            RefreshTargetsInternal();
        }
        else if (Application.isPlaying && !_hasRuntimeTargetRootOverride && _searchRoot == null)
        {
            RefreshTargetsAtRuntimeIfNeeded();
        }

        SnapshotAppliedRuntimeProperties();
        SnapshotAppliedCameraState(currentCamera);
        _lastAppliedTargetSlotsVersion = _targetSlotsVersion;

        if (_targetSlots.Count == 0)
        {
            return;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        Vector3 worldDirection = GetWorldDirection(currentCamera);
        Vector4 directionVector = new Vector4(worldDirection.x, worldDirection.y, worldDirection.z, 0f);
        float sceneShadowMode = (float)_sceneShadowMode;
        Dictionary<Transform, Bounds> planarShadowBoundsByOwner = _usePlanarShadowBoundsCenter
            ? _planarShadowBoundsByOwner
            : null;
        planarShadowBoundsByOwner?.Clear();

        // 通过材质属性块把主光参数写到角色材质槽，避免使用全局 Shader 变量。
        for (int i = 0; i < _targetSlots.Count; i++)
        {
            TargetSlot targetSlot = _targetSlots[i];
            if (!targetSlot.IsValid)
            {
                continue;
            }

            Material targetMaterial = targetSlot.Material;
            TargetMaterialKind targetMaterialKind = GetTargetMaterialKind(targetMaterial);
            if (targetMaterialKind == TargetMaterialKind.Unsupported)
            {
                continue;
            }

            bool useCustomMainLight = ShouldEnableCustomMainLight(targetMaterialKind);
            bool useCustomEnvironmentLight = ShouldEnableCustomEnvironmentLight(targetMaterialKind);
            bool useAdditionalLights = ShouldEnableAdditionalLights(targetMaterialKind);
            bool useSceneShadow = ShouldEnableSceneShadow(targetMaterial, targetMaterialKind);
            bool usePlanarShadow = ShouldEnablePlanarShadow(targetMaterialKind);
            Color customEnvironmentColor = useCustomEnvironmentLight ? _customEnvironmentColor : Color.white;
            float customEnvironmentLightBlend = useCustomEnvironmentLight ? _customEnvironmentLightBlend : 0f;
            float indirectSpecularIntensity = useCustomEnvironmentLight ? _indirectSpecularIntensity : 1f;
            SetCustomMainLightKeywordCached(targetMaterial, useCustomMainLight);
            SetCustomEnvironmentLightKeywordCached(targetMaterial, useCustomEnvironmentLight);

            _propertyBlock.Clear();
            targetSlot.Renderer.GetPropertyBlock(_propertyBlock, targetSlot.MaterialIndex);
            Vector4 targetDirectionVector = targetMaterialKind == TargetMaterialKind.Monster
                ? Vector4.zero
                : directionVector;
            _propertyBlock.SetVector(GlobalCustomMainLightDirId, targetDirectionVector);
            _propertyBlock.SetColor(GlobalCustomMainLightColorId, _mainLightColor);
            _propertyBlock.SetFloat(GlobalCustomMainLightIntensityId, _mainLightIntensity);
            _propertyBlock.SetFloat(AdditionalLightsIntensityId, useAdditionalLights ? 1f : 0f);
            _propertyBlock.SetColor(CustomEnvColorId, customEnvironmentColor);
            _propertyBlock.SetFloat(CustomEnvDiffLerpId, customEnvironmentLightBlend);
            _propertyBlock.SetFloat(CustomEnvDiffIntensityId, customEnvironmentLightBlend);
            _propertyBlock.SetFloat(IndirSpecIntensityId, indirectSpecularIntensity);
            if (targetMaterial.HasProperty(UsePerObjectShadowId))
            {
                float targetSceneShadowMode = useSceneShadow ? sceneShadowMode : (float)SceneShadowMode.Off;
                _propertyBlock.SetFloat(UsePerObjectShadowId, targetSceneShadowMode);
            }
            if (HasPlanarShadowProperties(targetMaterial))
            {
                Vector3 planarShadowGlobalCenter = ResolvePlanarShadowGlobalCenter(targetSlot, planarShadowBoundsByOwner);
                Vector4 planarShadowLightDir = ResolvePlanarShadowLightDir(targetSlot, currentCamera, planarShadowBoundsByOwner);
                Color planarShadowColor = usePlanarShadow
                    ? _planarShadowColor
                    : new Color(_planarShadowColor.r, _planarShadowColor.g, _planarShadowColor.b, 0f);
                _propertyBlock.SetFloat(PlanarShadowFalloffId, _planarShadowFalloff);
                _propertyBlock.SetFloat(PlanarShadowRangeId, _planarShadowRange);
                _propertyBlock.SetVector(PlanarShadowLightDirId, planarShadowLightDir);
                _propertyBlock.SetVector(
                    PlanarShadowGlobalCenterId,
                    new Vector4(planarShadowGlobalCenter.x, planarShadowGlobalCenter.y, planarShadowGlobalCenter.z, 0f));
                _propertyBlock.SetColor(PlanarShadowColorId, planarShadowColor);
            }
            targetSlot.Renderer.SetPropertyBlock(_propertyBlock, targetSlot.MaterialIndex);
        }
    }

    [ContextMenu("Apply Camera Direction")]
    public void ApplyCameraDirection()
    {
        ApplyNow();
    }

    public void SaveCurrentConfiguration(string configurationName)
    {
        if (_profile == null)
        {
            return;
        }

        string sanitizedName = CharacterMainLightProfile.SanitizeConfigurationName(configurationName);
        _activeConfigurationIndex = _profile.SaveConfiguration(CreateConfigurationSnapshot(sanitizedName));
        _configurationName = sanitizedName;
    }

    public void OverwriteConfiguration()
    {
        if (_profile == null)
        {
            return;
        }

        string configurationName = _profile.GetConfigurationName(0);
        if (string.IsNullOrWhiteSpace(configurationName))
        {
            configurationName = ConfigurationName;
        }

        if (_profile.OverwriteConfiguration(0, CreateConfigurationSnapshot(configurationName)))
        {
            _activeConfigurationIndex = 0;
            _configurationName = CharacterMainLightProfile.SanitizeConfigurationName(configurationName);
        }
    }

    public CharacterMainLightConfiguration CreateConfigurationSnapshot(string configurationName)
    {
        string sanitizedName = CharacterMainLightProfile.SanitizeConfigurationName(configurationName);
        return CaptureCurrentConfiguration(sanitizedName);
    }

    public void ApplyConfiguration()
    {
        if (_profile == null || !_profile.TryGetConfiguration(0, out CharacterMainLightConfiguration configuration))
        {
            return;
        }

        ApplyConfiguration(configuration);
        _activeConfigurationIndex = 0;
        _configurationName = configuration.DisplayName;
        ApplyNow();
    }

    public bool TryApplyActiveConfigurationFromProfile()
    {
        if (_profile == null)
        {
            return false;
        }

        if (!_profile.TryGetConfiguration(0, out CharacterMainLightConfiguration configuration))
        {
            return false;
        }

        ApplyConfiguration(configuration);
        _activeConfigurationIndex = 0;
        _configurationName = configuration.DisplayName;
        ApplyNow();
        return true;
    }

    public void DeleteConfiguration()
    {
        if (_profile == null || !_profile.TryGetConfiguration(0, out CharacterMainLightConfiguration configuration))
        {
            return;
        }

        if (!_profile.DeleteConfiguration(0))
        {
            return;
        }

        _configurationName = configuration.DisplayName;
        _activeConfigurationIndex = -1;
    }

    public Vector3 GetWorldDirection()
    {
        return GetWorldDirection(null);
    }

    private Vector3 GetWorldDirection(Camera currentCamera)
    {
        if (!_useCameraDirection || !TryResolveCameraLightDirection(currentCamera, out Vector3 cameraLightDirection))
        {
            return GetManualWorldDirection();
        }

        return cameraLightDirection;
    }

    private Vector3 GetManualWorldDirection()
    {
        SanitizeDirection();

        Vector3 direction = _mainLightDirection.normalized;

        if (direction.sqrMagnitude < MinDirectionMagnitude)
        {
            return Vector3.up;
        }

        return direction.normalized;
    }

    public void SetWorldDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            return;
        }

        Vector3 normalizedDirection = worldDirection.normalized;
        if (_useCameraDirection)
        {
            ApplyNow();
            return;
        }

        _mainLightDirection = normalizedDirection;

        SanitizeDirection();
        ApplyNow();
    }

    public Vector3 GetPlanarShadowWorldDirection()
    {
        return GetPlanarShadowWorldDirection(default, null, null);
    }

    public void SetPlanarShadowWorldDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            return;
        }

        Vector3 normalizedDirection = worldDirection.normalized;
        _planarShadowLightDirection = normalizedDirection;

        SanitizePlanarShadowDirection();
        ApplyNow();
    }

    public Vector3 GetPerObjectShadowWorldDirection()
    {
        SanitizePerObjectShadowDirection();

        Vector3 direction = _perObjectShadowDirection.normalized;

        if (direction.sqrMagnitude < MinDirectionMagnitude)
        {
            return Vector3.up;
        }

        return direction.normalized;
    }

    public void SetPerObjectShadowWorldDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            return;
        }

        Vector3 normalizedDirection = worldDirection.normalized;
        _perObjectShadowDirection = normalizedDirection;

        SanitizePerObjectShadowDirection();
        ApplyNow();
    }

    private Vector4 ResolvePlanarShadowLightDir(
        TargetSlot targetSlot,
        Camera currentCamera,
        Dictionary<Transform, Bounds> boundsByOwner)
    {
        Vector3 direction = GetPlanarShadowWorldDirection(targetSlot, currentCamera, boundsByOwner);
        return new Vector4(direction.x, direction.y, direction.z, _planarShadowPlaneHeight);
    }

    private Vector3 GetPlanarShadowWorldDirection(
        TargetSlot targetSlot,
        Camera currentCamera,
        Dictionary<Transform, Bounds> boundsByOwner)
    {
        if (!_usePlanarShadowCameraFacingDirection
            || !TryResolvePlanarShadowCameraFacingDirection(targetSlot, currentCamera, boundsByOwner, out Vector3 cameraFacingDirection))
        {
            return GetManualPlanarShadowWorldDirection();
        }

        Vector3 offsetDirection = GetPlanarShadowCameraFacingOffsetWorldDirection(cameraFacingDirection, currentCamera);
        Vector3 combinedDirection = cameraFacingDirection + offsetDirection;
        if (combinedDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            return cameraFacingDirection;
        }

        return combinedDirection.normalized;
    }

    private Vector3 GetManualPlanarShadowWorldDirection()
    {
        SanitizePlanarShadowDirection();

        Vector3 direction = _planarShadowLightDirection.normalized;

        if (direction.sqrMagnitude < MinDirectionMagnitude)
        {
            return Vector3.up;
        }

        return direction.normalized;
    }

    private Vector3 GetPlanarShadowCameraFacingOffsetWorldDirection(Vector3 cameraFacingDirection, Camera currentCamera)
    {
        if (_planarShadowLightDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            return Vector3.zero;
        }

        Transform cameraTransform = ResolveCameraTransform(currentCamera, _planarShadowTargetCamera);
        Vector3 referenceUp = cameraTransform != null ? cameraTransform.up : Vector3.up;
        Vector3 right = Vector3.Cross(referenceUp, cameraFacingDirection);
        if (right.sqrMagnitude < MinDirectionMagnitude)
        {
            Vector3 referenceRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            right = Vector3.ProjectOnPlane(referenceRight, cameraFacingDirection);
        }

        if (right.sqrMagnitude < MinDirectionMagnitude)
        {
            right = Vector3.Cross(Vector3.up, cameraFacingDirection);
        }

        if (right.sqrMagnitude < MinDirectionMagnitude)
        {
            right = Vector3.right;
        }

        right.Normalize();
        Vector3 up = Vector3.Cross(cameraFacingDirection, right).normalized;

        return right * _planarShadowLightDirection.x
            + up * _planarShadowLightDirection.y
            + cameraFacingDirection * _planarShadowLightDirection.z;
    }

    private Vector3 ResolvePlanarShadowGlobalCenter(TargetSlot targetSlot, Dictionary<Transform, Bounds> boundsByOwner)
    {
        if (_usePlanarShadowBoundsCenter && TryGetBoundsCenter(targetSlot.BoundsOwner, boundsByOwner, out Vector3 boundsCenter))
        {
            return boundsCenter;
        }

        return Vector3.zero;
    }

    private bool TryResolvePlanarShadowCameraFacingDirection(
        TargetSlot targetSlot,
        Camera currentCamera,
        Dictionary<Transform, Bounds> boundsByOwner,
        out Vector3 direction)
    {
        direction = Vector3.up;

        Transform cameraTransform = ResolveCameraTransform(currentCamera, _planarShadowTargetCamera);
        if (cameraTransform == null)
        {
            return false;
        }

        Vector3 cameraDirectionOrigin = ResolvePlanarShadowGlobalCenter(targetSlot, boundsByOwner);
        Vector3 toCamera = cameraTransform.position - cameraDirectionOrigin;
        if (toCamera.sqrMagnitude < MinDirectionMagnitude)
        {
            return false;
        }

        direction = toCamera.normalized;
        return true;
    }

    private void ApplyCurrentDirection()
    {
        ApplyNow();
    }

    private CharacterMainLightConfiguration CaptureCurrentConfiguration(string configurationName)
    {
        return new CharacterMainLightConfiguration
        {
            Name = CharacterMainLightProfile.SanitizeConfigurationName(configurationName),
            UseCameraDirection = _useCameraDirection,
            LightRotationOffsetEuler = _lightRotationOffsetEuler,
            MainLightColor = _mainLightColor,
            MainLightIntensity = _mainLightIntensity,
            MainLightDirection = _mainLightDirection,
            EnableCustomMainLightOnMaterials = _enableCustomMainLightOnMaterials,
            EnableMonsterCustomMainLightOnMaterials = _enableMonsterCustomMainLightOnMaterials,
            EnableGunCustomMainLightOnMaterials = _enableGunCustomMainLightOnMaterials,
            EnableAdditionalLights = _enableAdditionalLights,
            SceneShadowModeValue = (int)_sceneShadowMode,
            EnableCharacterSceneShadow = _enableCharacterSceneShadow,
            EnableHairFringeSelfShadow = _enableHairFringeSelfShadow,
            EnableMonsterSceneShadow = _enableMonsterSceneShadow,
            EnableGunSceneShadow = _enableGunSceneShadow,
            OverridePerObjectShadowDirection = _overridePerObjectShadowDirection,
            PerObjectShadowDirection = _perObjectShadowDirection,
            EnablePlanarShadow = _enablePlanarShadow,
            EnableCharacterPlanarShadow = _enableCharacterPlanarShadow,
            EnableMonsterPlanarShadow = _enableMonsterPlanarShadow,
            EnableGunPlanarShadow = _enableGunPlanarShadow,
            PlanarShadowFalloff = _planarShadowFalloff,
            PlanarShadowRange = _planarShadowRange,
            PlanarShadowLightDirection = _planarShadowLightDirection,
            UsePlanarShadowCameraFacingDirection = _usePlanarShadowCameraFacingDirection,
            ShowPlanarShadowSceneDirection = _showPlanarShadowSceneDirection,
            PlanarShadowSceneHandleLength = _planarShadowSceneHandleLength,
            PlanarShadowSceneHandleColor = _planarShadowSceneHandleColor,
            PlanarShadowPlaneHeight = _planarShadowPlaneHeight,
            UsePlanarShadowBoundsCenter = _usePlanarShadowBoundsCenter,
            PlanarShadowColor = _planarShadowColor,
            EnableCustomEnvironmentLightOnMaterials = _enableCustomEnvironmentLightOnMaterials,
            EnableMonsterCustomEnvironmentLightOnMaterials = _enableMonsterCustomEnvironmentLightOnMaterials,
            EnableGunCustomEnvironmentLightOnMaterials = _enableGunCustomEnvironmentLightOnMaterials,
            CustomEnvironmentColor = _customEnvironmentColor,
            CustomEnvironmentLightBlend = _customEnvironmentLightBlend,
            IndirectSpecularIntensity = _indirectSpecularIntensity,
            EnableCustomEnvironmentCubemap = _enableCustomEnvironmentCubemap,
            CustomEnvironmentCubemap = _customEnvironmentCubemap,
            CustomEnvironmentCubemapRotation = _customEnvironmentCubemapRotation,
            CustomEnvironmentCubemapIntensity = _customEnvironmentCubemapIntensity,
            ShowSceneDirection = _showSceneDirection,
            SceneHandleLength = _sceneHandleLength,
            SceneHandleColor = _sceneHandleColor,
            ShowPerObjectShadowSceneDirection = _showPerObjectShadowSceneDirection,
            PerObjectShadowSceneHandleLength = _perObjectShadowSceneHandleLength,
            PerObjectShadowSceneHandleColor = _perObjectShadowSceneHandleColor,
        };
    }

    private void ApplyConfiguration(CharacterMainLightConfiguration configuration)
    {
        _useCameraDirection = configuration.UseCameraDirection;
        _lightRotationOffsetEuler = configuration.LightRotationOffsetEuler;
        _mainLightColor = configuration.MainLightColor;
        _mainLightIntensity = configuration.MainLightIntensity;
        _mainLightDirection = configuration.MainLightDirection;
        _enableCustomMainLightOnMaterials = configuration.EnableCustomMainLightOnMaterials;
        _enableMonsterCustomMainLightOnMaterials = configuration.EnableMonsterCustomMainLightOnMaterials;
        _enableGunCustomMainLightOnMaterials = configuration.EnableGunCustomMainLightOnMaterials;
        _enableAdditionalLights = configuration.EnableAdditionalLights;
        _sceneShadowMode = (SceneShadowMode)configuration.SceneShadowModeValue;
        _enableCharacterSceneShadow = configuration.EnableCharacterSceneShadow;
        _enableHairFringeSelfShadow = configuration.EnableHairFringeSelfShadow;
        _enableMonsterSceneShadow = configuration.EnableMonsterSceneShadow;
        _enableGunSceneShadow = configuration.EnableGunSceneShadow;
        _overridePerObjectShadowDirection = configuration.OverridePerObjectShadowDirection;
        _perObjectShadowDirection = configuration.PerObjectShadowDirection;
        _enablePlanarShadow = configuration.EnablePlanarShadow;
        _enableCharacterPlanarShadow = configuration.EnableCharacterPlanarShadow;
        _enableMonsterPlanarShadow = configuration.EnableMonsterPlanarShadow;
        _enableGunPlanarShadow = configuration.EnableGunPlanarShadow;
        _planarShadowFalloff = configuration.PlanarShadowFalloff;
        _planarShadowRange = configuration.PlanarShadowRange;
        _planarShadowLightDirection = configuration.PlanarShadowLightDirection;
        _usePlanarShadowCameraFacingDirection = configuration.UsePlanarShadowCameraFacingDirection;
        _showPlanarShadowSceneDirection = configuration.ShowPlanarShadowSceneDirection;
        _planarShadowSceneHandleLength = configuration.PlanarShadowSceneHandleLength;
        _planarShadowSceneHandleColor = configuration.PlanarShadowSceneHandleColor;
        _planarShadowPlaneHeight = configuration.PlanarShadowPlaneHeight;
        _usePlanarShadowBoundsCenter = configuration.UsePlanarShadowBoundsCenter;
        _planarShadowColor = configuration.PlanarShadowColor;
        _enableCustomEnvironmentLightOnMaterials = configuration.EnableCustomEnvironmentLightOnMaterials;
        _enableMonsterCustomEnvironmentLightOnMaterials = configuration.EnableMonsterCustomEnvironmentLightOnMaterials;
        _enableGunCustomEnvironmentLightOnMaterials = configuration.EnableGunCustomEnvironmentLightOnMaterials;
        _customEnvironmentColor = configuration.CustomEnvironmentColor;
        _customEnvironmentLightBlend = configuration.CustomEnvironmentLightBlend;
        _indirectSpecularIntensity = configuration.IndirectSpecularIntensity;
        _enableCustomEnvironmentCubemap = configuration.EnableCustomEnvironmentCubemap;
        _customEnvironmentCubemap = configuration.CustomEnvironmentCubemap;
        _customEnvironmentCubemapRotation = configuration.CustomEnvironmentCubemapRotation;
        _customEnvironmentCubemapIntensity = configuration.CustomEnvironmentCubemapIntensity;
        _showSceneDirection = configuration.ShowSceneDirection;
        _sceneHandleLength = configuration.SceneHandleLength;
        _sceneHandleColor = configuration.SceneHandleColor;
        _showPerObjectShadowSceneDirection = configuration.ShowPerObjectShadowSceneDirection;
        _perObjectShadowSceneHandleLength = configuration.PerObjectShadowSceneHandleLength;
        _perObjectShadowSceneHandleColor = configuration.PerObjectShadowSceneHandleColor;

        SanitizeDirection();
        SanitizePerObjectShadowDirection();
        SanitizePlanarShadowDirection();
    }

    private void ApplyPerObjectShadowDirectionGlobal()
    {
        Shader.SetGlobalFloat(CustomPerObjectShadowDirectionEnabledId, _overridePerObjectShadowDirection ? 1f : 0f);
        Vector3 perObjectShadowDirection = GetPerObjectShadowWorldDirection();
        Shader.SetGlobalVector(
            CustomPerObjectShadowDirectionId,
            new Vector4(perObjectShadowDirection.x, perObjectShadowDirection.y, perObjectShadowDirection.z, 0f));
    }

    private static void ClearPerObjectShadowDirectionGlobal()
    {
        Shader.SetGlobalFloat(CustomPerObjectShadowDirectionEnabledId, 0f);
    }

    private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!isActiveAndEnabled || (!_useCameraDirection && (!_enablePlanarShadow || !_usePlanarShadowCameraFacingDirection)) || camera == null)
        {
            return;
        }

        using var profileScope = s_beginCameraRenderingProfilerMarker.Auto();

        bool targetsChanged = RefreshTargetsAtRuntimeIfNeeded();
        if (_targetRendererLayerMask == 0 || (camera.cullingMask & _targetRendererLayerMask) == 0)
        {
            return;
        }

        Camera sourceCamera = CharacterMainLightCameraOverride.ResolveSourceCamera(camera);
        ApplyPerObjectShadowDirectionGlobal();
        if (!targetsChanged && !ShouldApplyForCamera(sourceCamera))
        {
            return;
        }

        ApplyNow(sourceCamera);
    }

    private void ApplyHiddenTargetSettingsDefaults()
    {
        // Keep the controller in a predictable scene-wide mode instead of exposing runtime scan knobs in the inspector.
        _searchRoot = null;
        _includeInactive = false;
        _autoRefreshTargets = false;
        _applyEveryFrame = false;
    }

    private bool HaveRuntimePropertiesChangedSinceLastApply()
    {
        if (!_hasAppliedRuntimeProperties)
        {
            return true;
        }

        return _lastAppliedUseCameraDirection != _useCameraDirection
            || _lastAppliedLightRotationOffsetEuler != _lightRotationOffsetEuler
            || _lastAppliedMainLightColor != _mainLightColor
            || !Mathf.Approximately(_lastAppliedMainLightIntensity, _mainLightIntensity)
            || _lastAppliedMainLightDirection != _mainLightDirection
            || _lastAppliedEnableCustomMainLightOnMaterials != _enableCustomMainLightOnMaterials
            || _lastAppliedEnableMonsterCustomMainLightOnMaterials != _enableMonsterCustomMainLightOnMaterials
            || _lastAppliedEnableGunCustomMainLightOnMaterials != _enableGunCustomMainLightOnMaterials
            || _lastAppliedEnableAdditionalLights != _enableAdditionalLights
            || _lastAppliedSceneShadowMode != _sceneShadowMode
            || _lastAppliedEnableCharacterSceneShadow != _enableCharacterSceneShadow
            || _lastAppliedEnableHairFringeSelfShadow != _enableHairFringeSelfShadow
            || _lastAppliedEnableMonsterSceneShadow != _enableMonsterSceneShadow
            || _lastAppliedEnableGunSceneShadow != _enableGunSceneShadow
            || _lastAppliedOverridePerObjectShadowDirection != _overridePerObjectShadowDirection
            || _lastAppliedPerObjectShadowDirection != _perObjectShadowDirection
            || _lastAppliedEnablePlanarShadow != _enablePlanarShadow
            || _lastAppliedEnableCharacterPlanarShadow != _enableCharacterPlanarShadow
            || _lastAppliedEnableMonsterPlanarShadow != _enableMonsterPlanarShadow
            || _lastAppliedEnableGunPlanarShadow != _enableGunPlanarShadow
            || !Mathf.Approximately(_lastAppliedPlanarShadowFalloff, _planarShadowFalloff)
            || !Mathf.Approximately(_lastAppliedPlanarShadowRange, _planarShadowRange)
            || _lastAppliedPlanarShadowLightDirection != _planarShadowLightDirection
            || _lastAppliedUsePlanarShadowCameraFacingDirection != _usePlanarShadowCameraFacingDirection
            || !Mathf.Approximately(_lastAppliedPlanarShadowPlaneHeight, _planarShadowPlaneHeight)
            || _lastAppliedUsePlanarShadowBoundsCenter != _usePlanarShadowBoundsCenter
            || _lastAppliedPlanarShadowColor != _planarShadowColor
            || _lastAppliedEnableCustomEnvironmentLightOnMaterials != _enableCustomEnvironmentLightOnMaterials
            || _lastAppliedEnableMonsterCustomEnvironmentLightOnMaterials != _enableMonsterCustomEnvironmentLightOnMaterials
            || _lastAppliedEnableGunCustomEnvironmentLightOnMaterials != _enableGunCustomEnvironmentLightOnMaterials
            || _lastAppliedCustomEnvironmentColor != _customEnvironmentColor
            || !Mathf.Approximately(_lastAppliedCustomEnvironmentLightBlend, _customEnvironmentLightBlend)
            || !Mathf.Approximately(_lastAppliedIndirectSpecularIntensity, _indirectSpecularIntensity);
    }

    private bool ShouldApplyForCamera(Camera sourceCamera)
    {
        if (!_hasAppliedCameraState ||
            _lastAppliedTargetSlotsVersion != _targetSlotsVersion ||
            HaveRuntimePropertiesChangedSinceLastApply() ||
            !ReferenceEquals(_lastAppliedSourceCamera, sourceCamera))
        {
            return true;
        }

        if (_enablePlanarShadow && (_usePlanarShadowCameraFacingDirection || _usePlanarShadowBoundsCenter))
        {
            // 平面阴影依赖相机位置或目标 Bounds；同一源相机同一帧只需写一次，但运动中仍需逐帧刷新。
            return _lastAppliedCameraFrame != Time.frameCount;
        }

        if (!_useCameraDirection)
        {
            return false;
        }

        Transform sourceTransform = ResolveCameraTransform(sourceCamera);
        return sourceTransform == null || sourceTransform.rotation != _lastAppliedSourceCameraRotation;
    }

    private void SnapshotAppliedCameraState(Camera sourceCamera)
    {
        _hasAppliedCameraState = true;
        _lastAppliedSourceCamera = sourceCamera;
        _lastAppliedCameraFrame = Time.frameCount;
        Transform sourceTransform = ResolveCameraTransform(sourceCamera);
        _lastAppliedSourceCameraRotation = sourceTransform != null
            ? sourceTransform.rotation
            : Quaternion.identity;
    }

    private void SnapshotAppliedRuntimeProperties()
    {
        _hasAppliedRuntimeProperties = true;
        _lastAppliedUseCameraDirection = _useCameraDirection;
        _lastAppliedLightRotationOffsetEuler = _lightRotationOffsetEuler;
        _lastAppliedMainLightColor = _mainLightColor;
        _lastAppliedMainLightIntensity = _mainLightIntensity;
        _lastAppliedMainLightDirection = _mainLightDirection;
        _lastAppliedEnableCustomMainLightOnMaterials = _enableCustomMainLightOnMaterials;
        _lastAppliedEnableMonsterCustomMainLightOnMaterials = _enableMonsterCustomMainLightOnMaterials;
        _lastAppliedEnableGunCustomMainLightOnMaterials = _enableGunCustomMainLightOnMaterials;
        _lastAppliedEnableAdditionalLights = _enableAdditionalLights;
        _lastAppliedSceneShadowMode = _sceneShadowMode;
        _lastAppliedEnableCharacterSceneShadow = _enableCharacterSceneShadow;
        _lastAppliedEnableHairFringeSelfShadow = _enableHairFringeSelfShadow;
        _lastAppliedEnableMonsterSceneShadow = _enableMonsterSceneShadow;
        _lastAppliedEnableGunSceneShadow = _enableGunSceneShadow;
        _lastAppliedOverridePerObjectShadowDirection = _overridePerObjectShadowDirection;
        _lastAppliedPerObjectShadowDirection = _perObjectShadowDirection;
        _lastAppliedEnablePlanarShadow = _enablePlanarShadow;
        _lastAppliedEnableCharacterPlanarShadow = _enableCharacterPlanarShadow;
        _lastAppliedEnableMonsterPlanarShadow = _enableMonsterPlanarShadow;
        _lastAppliedEnableGunPlanarShadow = _enableGunPlanarShadow;
        _lastAppliedPlanarShadowFalloff = _planarShadowFalloff;
        _lastAppliedPlanarShadowRange = _planarShadowRange;
        _lastAppliedPlanarShadowLightDirection = _planarShadowLightDirection;
        _lastAppliedUsePlanarShadowCameraFacingDirection = _usePlanarShadowCameraFacingDirection;
        _lastAppliedPlanarShadowPlaneHeight = _planarShadowPlaneHeight;
        _lastAppliedUsePlanarShadowBoundsCenter = _usePlanarShadowBoundsCenter;
        _lastAppliedPlanarShadowColor = _planarShadowColor;
        _lastAppliedEnableCustomEnvironmentLightOnMaterials = _enableCustomEnvironmentLightOnMaterials;
        _lastAppliedEnableMonsterCustomEnvironmentLightOnMaterials = _enableMonsterCustomEnvironmentLightOnMaterials;
        _lastAppliedEnableGunCustomEnvironmentLightOnMaterials = _enableGunCustomEnvironmentLightOnMaterials;
        _lastAppliedCustomEnvironmentColor = _customEnvironmentColor;
        _lastAppliedCustomEnvironmentLightBlend = _customEnvironmentLightBlend;
        _lastAppliedIndirectSpecularIntensity = _indirectSpecularIntensity;
    }

    private void RestoreOwnedPropertyDefaults()
    {
        if (_targetSlots.Count == 0)
        {
            return;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < _targetSlots.Count; i++)
        {
            TargetSlot targetSlot = _targetSlots[i];
            if (!targetSlot.IsValid || targetSlot.Renderer == null)
            {
                continue;
            }

            Material targetMaterial = targetSlot.Material;
            _propertyBlock.Clear();
            targetSlot.Renderer.GetPropertyBlock(_propertyBlock, targetSlot.MaterialIndex);

            RestoreVectorFromMaterial(targetMaterial, _propertyBlock, GlobalCustomMainLightDirId);
            RestoreColorFromMaterial(targetMaterial, _propertyBlock, GlobalCustomMainLightColorId);
            RestoreFloatFromMaterial(targetMaterial, _propertyBlock, GlobalCustomMainLightIntensityId);
            RestoreFloatFromMaterial(targetMaterial, _propertyBlock, AdditionalLightsIntensityId);
            RestoreColorFromMaterial(targetMaterial, _propertyBlock, CustomEnvColorId);
            RestoreFloatFromMaterial(targetMaterial, _propertyBlock, CustomEnvDiffLerpId);
            RestoreFloatFromMaterial(targetMaterial, _propertyBlock, CustomEnvDiffIntensityId);
            RestoreFloatFromMaterial(targetMaterial, _propertyBlock, IndirSpecIntensityId);
            RestoreFloatFromMaterial(targetMaterial, _propertyBlock, UsePerObjectShadowId);
            RestoreFloatFromMaterial(targetMaterial, _propertyBlock, PlanarShadowFalloffId);
            RestoreFloatFromMaterial(targetMaterial, _propertyBlock, PlanarShadowRangeId);
            RestoreVectorFromMaterial(targetMaterial, _propertyBlock, PlanarShadowLightDirId);
            RestoreVectorFromMaterial(targetMaterial, _propertyBlock, PlanarShadowGlobalCenterId);
            RestoreColorFromMaterial(targetMaterial, _propertyBlock, PlanarShadowColorId);

            // Hair direction, facial parameters and Timeline tracks share the same material slot.
            targetSlot.Renderer.SetPropertyBlock(_propertyBlock, targetSlot.MaterialIndex);
        }
    }

    private static void RestoreFloatFromMaterial(
        Material material,
        MaterialPropertyBlock propertyBlock,
        int propertyId)
    {
        if (material != null && material.HasProperty(propertyId))
        {
            propertyBlock.SetFloat(propertyId, material.GetFloat(propertyId));
        }
    }

    private static void RestoreVectorFromMaterial(
        Material material,
        MaterialPropertyBlock propertyBlock,
        int propertyId)
    {
        if (material != null && material.HasProperty(propertyId))
        {
            propertyBlock.SetVector(propertyId, material.GetVector(propertyId));
        }
    }

    private static void RestoreColorFromMaterial(
        Material material,
        MaterialPropertyBlock propertyBlock,
        int propertyId)
    {
        if (material != null && material.HasProperty(propertyId))
        {
            propertyBlock.SetColor(propertyId, material.GetColor(propertyId));
        }
    }

    private void InvalidateAppliedState()
    {
        _hasAppliedRuntimeProperties = false;
        _hasAppliedCameraState = false;
        _lastAppliedTargetSlotsVersion = -1;
    }

    private static void RegisterActiveController(CharacterMainLightController controller)
    {
        if (controller == null)
        {
            return;
        }

        for (int i = s_activeControllers.Count - 1; i >= 0; i--)
        {
            CharacterMainLightController activeController = s_activeControllers[i];
            if (activeController == null)
            {
                s_activeControllers.RemoveAt(i);
                continue;
            }

            if (activeController == controller)
            {
                return;
            }
        }

        s_activeControllers.Add(controller);
    }

    private static void UnregisterActiveController(CharacterMainLightController controller)
    {
        for (int i = s_activeControllers.Count - 1; i >= 0; i--)
        {
            CharacterMainLightController activeController = s_activeControllers[i];
            if (activeController == null || activeController == controller)
            {
                s_activeControllers.RemoveAt(i);
            }
        }
    }

    private static void InvalidateActiveControllersForHandoff()
    {
        for (int i = s_activeControllers.Count - 1; i >= 0; i--)
        {
            CharacterMainLightController activeController = s_activeControllers[i];
            if (activeController == null)
            {
                s_activeControllers.RemoveAt(i);
                continue;
            }

            activeController.InvalidateAppliedState();
        }
    }

    private bool RefreshTargetsAtRuntimeIfNeeded()
    {
        int runtimeTargetVersion = s_runtimeTargetRootsVersion;
        if (_targetsRefreshInitialized && _lastRuntimeTargetRefreshVersion == runtimeTargetVersion)
        {
            return false;
        }

        if (_lastRuntimeTargetRefreshFrame == Time.frameCount && _lastRuntimeTargetRefreshVersion == runtimeTargetVersion)
        {
            return false;
        }

        return RefreshTargetsInternal();
    }

    private bool RefreshTargetsInternal()
    {
        using var profileScope = s_refreshTargetsProfilerMarker.Auto();

        _materialKeywordStateByMaterial.Clear();
        _scratchTargetSlots.Clear();

        if (_hasRuntimeTargetRootOverride)
        {
            if (_runtimeTargetRoot != null)
            {
                CollectTargetsFromRoot(_runtimeTargetRoot, _scratchTargetSlots, false);
            }

            return CompleteRefreshTargets();
        }

        if (_searchRoot != null)
        {
            CollectTargetsFromRoot(_searchRoot, _scratchTargetSlots, false);
            return CompleteRefreshTargets();
        }

        if (!CollectTargetsFromRuntimeTargetRoots(_scratchTargetSlots) && !Application.isPlaying)
        {
            CollectTargetsFromLoadedScenes(_scratchTargetSlots);
        }

        return CompleteRefreshTargets();
    }

    private bool CompleteRefreshTargets()
    {
        int targetRendererLayerMask = BuildTargetRendererLayerMask(_scratchTargetSlots);
        bool changed;
        using (s_refreshTargetsUpdateSlotsProfilerMarker.Auto())
        {
            changed = UpdateTargetSlotsIfChanged() || targetRendererLayerMask != _targetRendererLayerMask;
        }

        _targetRendererLayerMask = targetRendererLayerMask;
        if (changed)
        {
            _targetSlotsVersion++;
        }

        _lastRuntimeTargetRefreshFrame = Time.frameCount;
        _lastRuntimeTargetRefreshVersion = s_runtimeTargetRootsVersion;
        _targetsRefreshInitialized = true;
        return changed;
    }

    private static int BuildTargetRendererLayerMask(List<TargetSlot> targetSlots)
    {
        int mask = 0;
        for (int i = 0; i < targetSlots.Count; i++)
        {
            Renderer renderer = targetSlots[i].Renderer;
            if (renderer == null)
            {
                continue;
            }

            int layer = renderer.gameObject.layer;
            if (layer >= 0 && layer < 32)
            {
                mask |= 1 << layer;
            }
        }

        return mask;
    }

    private bool CollectTargetsFromRuntimeTargetRoots(List<TargetSlot> targetSlots)
    {
        if (PurgeRuntimeTargetRoots())
        {
            MarkRuntimeTargetsChanged();
        }

        if (s_runtimeTargetRoots.Count == 0)
        {
            return false;
        }

        for (int rootIndex = 0; rootIndex < s_runtimeTargetRoots.Count; rootIndex++)
        {
            Transform targetRoot = s_runtimeTargetRoots[rootIndex];
            if (targetRoot == null || !targetRoot.gameObject.activeInHierarchy)
            {
                continue;
            }

            CollectTargetsFromRoot(targetRoot, targetSlots, false, true);
        }

        return true;
    }

    private void CollectTargetsFromLoadedScenes(List<TargetSlot> targetSlots)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            using (s_refreshTargetsGetRootObjectsProfilerMarker.Auto())
            {
                _scratchRootObjects.Clear();
                scene.GetRootGameObjects(_scratchRootObjects);
            }

            for (int rootIndex = 0; rootIndex < _scratchRootObjects.Count; rootIndex++)
            {
                GameObject rootObject = _scratchRootObjects[rootIndex];
                if (rootObject == null)
                {
                    continue;
                }

                CollectTargetsFromRoot(rootObject.transform, targetSlots, true);
            }
        }
    }

    private bool UpdateTargetSlotsIfChanged()
    {
        if (AreTargetSlotsEqual(_targetSlots, _scratchTargetSlots))
        {
            return false;
        }

        _targetSlots.Clear();
        _targetSlots.AddRange(_scratchTargetSlots);
        return true;
    }

    private void CollectTargetsFromRoot(Transform rootTransform, List<TargetSlot> targetSlots, bool filterByRuntimeTargetLayer)
    {
        CollectTargetsFromRoot(rootTransform, targetSlots, filterByRuntimeTargetLayer, _includeInactive);
    }

    private void CollectTargetsFromRoot(
        Transform rootTransform,
        List<TargetSlot> targetSlots,
        bool filterByRuntimeTargetLayer,
        bool includeInactive)
    {
        if (rootTransform == null)
        {
            return;
        }

        using (s_refreshTargetsCollectRootProfilerMarker.Auto())
        {
            using (s_refreshTargetsGetRenderersProfilerMarker.Auto())
            {
                _scratchRenderers.Clear();
                rootTransform.GetComponentsInChildren(includeInactive, _scratchRenderers);
            }

            for (int rendererIndex = 0; rendererIndex < _scratchRenderers.Count; rendererIndex++)
            {
                Renderer renderer = _scratchRenderers[rendererIndex];
                if (renderer == null || (filterByRuntimeTargetLayer && !IsRuntimeTargetLayer(renderer.gameObject.layer)))
                {
                    continue;
                }

                using (s_refreshTargetsGetSharedMaterialsProfilerMarker.Auto())
                {
                    _scratchSharedMaterials.Clear();
                    renderer.GetSharedMaterials(_scratchSharedMaterials);
                }

                for (int materialIndex = 0; materialIndex < _scratchSharedMaterials.Count; materialIndex++)
                {
                    Material material = _scratchSharedMaterials[materialIndex];
                    TargetMaterialKind targetMaterialKind;
                    using (s_refreshTargetsMaterialFilterProfilerMarker.Auto())
                    {
                        targetMaterialKind = GetTargetMaterialKind(material);
                        if (!IsTargetMaterial(material, targetMaterialKind))
                        {
                            continue;
                        }
                    }

                    Transform boundsOwner;
                    using (s_refreshTargetsResolveBoundsOwnerProfilerMarker.Auto())
                    {
                        boundsOwner = ResolveBoundsOwner(renderer);
                    }

                    using (s_refreshTargetsConfigureMaterialProfilerMarker.Auto())
                    {
                        bool useCustomMainLight = ShouldEnableCustomMainLight(targetMaterialKind);
                        bool useCustomEnvironmentLight = ShouldEnableCustomEnvironmentLight(targetMaterialKind);
                        SetCustomMainLightKeywordCached(material, useCustomMainLight);
                        SetCustomEnvironmentLightKeywordCached(material, useCustomEnvironmentLight);
                        targetSlots.Add(new TargetSlot(renderer, materialIndex, boundsOwner, material));
                    }
                }
            }
        }
    }

    private static bool AreTargetSlotsEqual(List<TargetSlot> left, List<TargetSlot> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (!left[i].Matches(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsRuntimeTargetLayer(int layer)
    {
        int targetLayerMask = GetRuntimeTargetLayerMask();
        return targetLayerMask == 0 || (targetLayerMask & (1 << layer)) != 0;
    }

    private static int GetRuntimeTargetLayerMask()
    {
        if (s_runtimeTargetLayerMask >= 0)
        {
            return s_runtimeTargetLayerMask;
        }

        int mask = 0;
        AddLayerToMask(CharacterLayerName, ref mask);
        AddLayerToMask(MonsterLayerName, ref mask);
        AddLayerToMask(WeaponLayerName, ref mask);
        s_runtimeTargetLayerMask = mask;
        return s_runtimeTargetLayerMask;
    }

    private static void AddLayerToMask(string layerName, ref int mask)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            mask |= 1 << layer;
        }
    }

    private static void MarkRuntimeTargetsChanged()
    {
        s_runtimeTargetRootsVersion++;
    }

    private static bool PurgeRuntimeTargetRoots()
    {
        bool removed = false;
        for (int i = s_runtimeTargetRoots.Count - 1; i >= 0; i--)
        {
            if (s_runtimeTargetRoots[i] == null)
            {
                s_runtimeTargetRoots.RemoveAt(i);
                removed = true;
            }
        }

        return removed;
    }

    private static bool IsSameOrChildOf(Transform child, Transform ancestor)
    {
        if (child == null || ancestor == null)
        {
            return false;
        }

        Transform current = child;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsTargetMaterial(Material material, TargetMaterialKind targetMaterialKind)
    {
        if (material == null || targetMaterialKind == TargetMaterialKind.Unsupported)
        {
            return false;
        }

        bool hasMainLightProperties = material.HasProperty(GlobalCustomMainLightDirId)
            && material.HasProperty(GlobalCustomMainLightColorId)
            && material.HasProperty(GlobalCustomMainLightIntensityId);
        bool hasCustomEnvironmentProperties = material.HasProperty(CustomEnvColorId)
            && (material.HasProperty(CustomEnvDiffLerpId) || material.HasProperty(CustomEnvDiffIntensityId));
        bool hasSceneShadowModeProperty = material.HasProperty(UsePerObjectShadowId);
        bool hasPlanarShadowProperties = HasPlanarShadowProperties(material);

        return hasMainLightProperties || hasCustomEnvironmentProperties || hasSceneShadowModeProperty || hasPlanarShadowProperties;
    }

    private TargetMaterialKind GetTargetMaterialKind(Material material)
    {
        if (material == null)
        {
            return TargetMaterialKind.Unsupported;
        }

        Shader shader = material.shader;
        if (shader == null)
        {
            return TargetMaterialKind.Unsupported;
        }

        if (_targetMaterialKindByShader.TryGetValue(shader, out TargetMaterialKind cachedKind))
        {
            return cachedKind;
        }

        TargetMaterialKind targetMaterialKind = GetTargetMaterialKind(shader.name);
        _targetMaterialKindByShader[shader] = targetMaterialKind;
        return targetMaterialKind;
    }

    private static TargetMaterialKind GetTargetMaterialKind(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
        {
            return TargetMaterialKind.Unsupported;
        }

        if (shaderName.StartsWith(CharacterShaderPrefix, System.StringComparison.Ordinal))
        {
            return TargetMaterialKind.Character;
        }

        if (string.Equals(shaderName, MonsterShaderName, System.StringComparison.Ordinal))
        {
            return TargetMaterialKind.Monster;
        }

        if (string.Equals(shaderName, GunShaderName, System.StringComparison.Ordinal))
        {
            return TargetMaterialKind.Gun;
        }

        return TargetMaterialKind.Unsupported;
    }

    private bool ShouldEnableCustomMainLight(TargetMaterialKind targetMaterialKind)
    {
        switch (targetMaterialKind)
        {
            case TargetMaterialKind.Character:
                return _enableCustomMainLightOnMaterials;
            case TargetMaterialKind.Monster:
                return _enableMonsterCustomMainLightOnMaterials;
            case TargetMaterialKind.Gun:
                return _enableGunCustomMainLightOnMaterials;
            default:
                return false;
        }
    }

    private bool ShouldEnableCustomEnvironmentLight(TargetMaterialKind targetMaterialKind)
    {
        switch (targetMaterialKind)
        {
            case TargetMaterialKind.Character:
                return _enableCustomEnvironmentLightOnMaterials;
            case TargetMaterialKind.Monster:
                return _enableMonsterCustomEnvironmentLightOnMaterials;
            case TargetMaterialKind.Gun:
                return _enableGunCustomEnvironmentLightOnMaterials;
            default:
                return false;
        }
    }

    private bool ShouldEnableAdditionalLights(TargetMaterialKind targetMaterialKind)
    {
        switch (targetMaterialKind)
        {
            case TargetMaterialKind.Character:
            case TargetMaterialKind.Monster:
            case TargetMaterialKind.Gun:
                return _enableAdditionalLights;
            default:
                return false;
        }
    }

    private bool ShouldEnableSceneShadow(Material material, TargetMaterialKind targetMaterialKind)
    {
        switch (targetMaterialKind)
        {
            case TargetMaterialKind.Character:
                return _enableCharacterSceneShadow && (_enableHairFringeSelfShadow || !IsHairOrFringeMaterial(material));
            case TargetMaterialKind.Monster:
                return _enableMonsterSceneShadow;
            case TargetMaterialKind.Gun:
                return _enableGunSceneShadow;
            default:
                return false;
        }
    }

    private bool ShouldEnablePlanarShadow(TargetMaterialKind targetMaterialKind)
    {
        if (!_enablePlanarShadow)
        {
            return false;
        }

        switch (targetMaterialKind)
        {
            case TargetMaterialKind.Character:
                return _enableCharacterPlanarShadow;
            case TargetMaterialKind.Monster:
                return _enableMonsterPlanarShadow;
            case TargetMaterialKind.Gun:
                return _enableGunPlanarShadow;
            default:
                return false;
        }
    }

    private bool IsHairOrFringeMaterial(Material material)
    {
        if (material == null)
        {
            return false;
        }

        Shader shader = material.shader;
        if (shader == null)
        {
            return false;
        }

        if (_hairOrFringeShaderByShader.TryGetValue(shader, out bool cachedIsHairOrFringe))
        {
            return cachedIsHairOrFringe;
        }

        string shaderName = shader.name;
        bool isHairOrFringe = string.Equals(shaderName, HairShaderName, System.StringComparison.Ordinal)
            || string.Equals(shaderName, FringeShaderName, System.StringComparison.Ordinal);
        _hairOrFringeShaderByShader[shader] = isHairOrFringe;
        return isHairOrFringe;
    }

    private static bool HasPlanarShadowProperties(Material material)
    {
        return material != null
            && material.HasProperty(PlanarShadowFalloffId)
            && material.HasProperty(PlanarShadowRangeId)
            && material.HasProperty(PlanarShadowLightDirId)
            && material.HasProperty(PlanarShadowGlobalCenterId)
            && material.HasProperty(PlanarShadowColorId);
    }

    private static void SetCustomMainLightKeyword(Material material, bool enabled)
    {
        if (material == null || !material.HasProperty(CustomMainLightDirectionId))
        {
            return;
        }

        if (enabled)
        {
            material.EnableKeyword(CustomMainLightKeyword);
            return;
        }

        material.DisableKeyword(CustomMainLightKeyword);
    }

    private void SetCustomMainLightKeywordCached(Material material, bool enabled)
    {
        if (material == null || !material.HasProperty(CustomMainLightDirectionId))
        {
            return;
        }

        _materialKeywordStateByMaterial.TryGetValue(material, out int state);
        bool known = (state & MaterialKeywordStateCustomMainLightKnown) != 0;
        bool lastEnabled = (state & MaterialKeywordStateCustomMainLightEnabled) != 0;
        bool materialEnabled = material.IsKeywordEnabled(CustomMainLightKeyword);
        if (known && lastEnabled == enabled && materialEnabled == enabled)
        {
            return;
        }

        if (enabled)
        {
            material.EnableKeyword(CustomMainLightKeyword);
            state |= MaterialKeywordStateCustomMainLightEnabled;
        }
        else
        {
            material.DisableKeyword(CustomMainLightKeyword);
            state &= ~MaterialKeywordStateCustomMainLightEnabled;
        }

        state |= MaterialKeywordStateCustomMainLightKnown;
        _materialKeywordStateByMaterial[material] = state;
    }

    private static void SetCustomEnvironmentLightKeyword(Material material, bool enabled)
    {
        if (material == null || !material.HasProperty(CustomEnvironmentLightId))
        {
            return;
        }

        material.SetFloat(CustomEnvironmentLightId, enabled ? 1f : 0f);
        if (enabled)
        {
            material.EnableKeyword(CustomEnvironmentLightKeyword);
            return;
        }

        material.DisableKeyword(CustomEnvironmentLightKeyword);
    }

    private void SetCustomEnvironmentLightKeywordCached(Material material, bool enabled)
    {
        if (material == null || !material.HasProperty(CustomEnvironmentLightId))
        {
            return;
        }

        _materialKeywordStateByMaterial.TryGetValue(material, out int state);
        bool known = (state & MaterialKeywordStateCustomEnvironmentKnown) != 0;
        bool lastEnabled = (state & MaterialKeywordStateCustomEnvironmentEnabled) != 0;
        bool materialKeywordEnabled = material.IsKeywordEnabled(CustomEnvironmentLightKeyword);
        bool materialPropertyEnabled = material.GetFloat(CustomEnvironmentLightId) > 0.5f;
        if (known
            && lastEnabled == enabled
            && materialKeywordEnabled == enabled
            && materialPropertyEnabled == enabled)
        {
            return;
        }

        material.SetFloat(CustomEnvironmentLightId, enabled ? 1f : 0f);
        if (enabled)
        {
            material.EnableKeyword(CustomEnvironmentLightKeyword);
            state |= MaterialKeywordStateCustomEnvironmentEnabled;
        }
        else
        {
            material.DisableKeyword(CustomEnvironmentLightKeyword);
            state &= ~MaterialKeywordStateCustomEnvironmentEnabled;
        }

        state |= MaterialKeywordStateCustomEnvironmentKnown;
        _materialKeywordStateByMaterial[material] = state;
    }

    private bool TryGetBoundsCenter(Transform boundsOwner, Dictionary<Transform, Bounds> boundsByOwner, out Vector3 center)
    {
        center = Vector3.zero;

        if (boundsOwner == null)
        {
            return false;
        }

        if (boundsByOwner != null && boundsByOwner.TryGetValue(boundsOwner, out Bounds cachedBounds))
        {
            center = cachedBounds.center;
            return true;
        }

        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < _targetSlots.Count; i++)
        {
            TargetSlot targetSlot = _targetSlots[i];
            if (targetSlot.BoundsOwner != boundsOwner)
            {
                continue;
            }

            Renderer renderer = targetSlot.Renderer;
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        if (!hasBounds)
        {
            return false;
        }

        center = bounds.center;
        if (boundsByOwner != null)
        {
            boundsByOwner[boundsOwner] = bounds;
        }

        return true;
    }

    private static Transform ResolveBoundsOwner(Renderer renderer)
    {
        if (renderer == null)
        {
            return null;
        }

        Animator animator = renderer.GetComponentInParent<Animator>();
        if (animator != null)
        {
            return animator.transform;
        }

        return renderer.transform.root;
    }

    private void SanitizeDirection()
    {
        if (_useCameraDirection)
        {
            return;
        }

        if (_mainLightDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            _mainLightDirection = Vector3.up;
        }
    }

    private void SanitizePerObjectShadowDirection()
    {
        if (_perObjectShadowDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            _perObjectShadowDirection = Vector3.up;
        }
    }

    private void SanitizePlanarShadowDirection()
    {
        _planarShadowLightDirection = ClampDirectionComponents(_planarShadowLightDirection);

        if (_usePlanarShadowCameraFacingDirection)
        {
            return;
        }

        if (_planarShadowLightDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            _planarShadowLightDirection = Vector3.up;
        }
    }

    private static Vector3 ClampDirectionComponents(Vector3 direction)
    {
        return new Vector3(
            Mathf.Clamp(direction.x, DirectionComponentMin, DirectionComponentMax),
            Mathf.Clamp(direction.y, DirectionYMin, DirectionYMax),
            Mathf.Clamp(direction.z, DirectionComponentMin, DirectionComponentMax));
    }

    private bool TryResolveCameraLightDirection(Camera currentCamera, out Vector3 lightDirection)
    {
        lightDirection = Vector3.up;

        Transform cameraTransform = ResolveCameraTransform(currentCamera, _targetCamera);
        if (cameraTransform == null)
        {
            return false;
        }

        Quaternion targetRotation = cameraTransform.rotation * Quaternion.Euler(_lightRotationOffsetEuler);
        Vector3 lightForward = targetRotation * Vector3.forward;
        if (lightForward.sqrMagnitude < MinDirectionMagnitude)
        {
            return false;
        }

        // The shader expects a point-to-light direction, so use the opposite of light forward.
        lightDirection = -lightForward.normalized;
        return true;
    }

    private Transform ResolveCameraTransform(Camera currentCamera)
    {
        return ResolveCameraTransform(currentCamera, _targetCamera);
    }

    private Transform ResolveCameraTransform(Camera currentCamera, Camera fallbackCamera)
    {
        if (currentCamera != null)
        {
            return currentCamera.transform;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView sceneView = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
            {
                return sceneView.camera.transform;
            }
        }
#endif

        if (Camera.current != null)
        {
            return Camera.current.transform;
        }

        if (fallbackCamera != null)
        {
            return fallbackCamera.transform;
        }

        if (Application.isPlaying && Camera.main != null)
        {
            return Camera.main.transform;
        }

        return null;
    }

#if UNITY_EDITOR
    private void HandleProfileChanged(CharacterMainLightProfile changedProfile)
    {
        if (changedProfile == null || _profile != changedProfile || _activeConfigurationIndex < 0)
        {
            return;
        }

        if (!TryApplyActiveConfigurationFromProfile())
        {
            return;
        }
        EditorUtility.SetDirty(this);
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private void RequestEditorCameraRefresh()
    {
        if (Application.isPlaying || (!_useCameraDirection && (!_enablePlanarShadow || !_usePlanarShadowCameraFacingDirection)))
        {
            return;
        }

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private void HandleDuringSceneGui(SceneView sceneView)
    {
        if (Application.isPlaying || !isActiveAndEnabled || (!_useCameraDirection && (!_enablePlanarShadow || !_usePlanarShadowCameraFacingDirection)) || sceneView == null || sceneView.camera == null)
        {
            return;
        }

        ApplyNow(sceneView.camera);
    }

    private void HandleEditorHierarchyChanged()
    {
        if (Application.isPlaying || !isActiveAndEnabled)
        {
            return;
        }

        if (RefreshTargetsAtRuntimeIfNeeded())
        {
            ApplyCurrentDirection();
        }

        RequestEditorCameraRefresh();
    }
#endif

    private readonly struct TargetSlot
    {
        public readonly Renderer Renderer;
        public readonly int MaterialIndex;
        public readonly Transform BoundsOwner;
        public readonly Material Material;

        public bool IsValid => Renderer != null && MaterialIndex >= 0;

        public bool Matches(TargetSlot other)
        {
            return Renderer == other.Renderer
                && MaterialIndex == other.MaterialIndex
                && BoundsOwner == other.BoundsOwner
                && Material == other.Material;
        }

        public TargetSlot(Renderer renderer, int materialIndex, Transform boundsOwner, Material material)
        {
            Renderer = renderer;
            MaterialIndex = materialIndex;
            BoundsOwner = boundsOwner;
            Material = material;
        }
    }

}
