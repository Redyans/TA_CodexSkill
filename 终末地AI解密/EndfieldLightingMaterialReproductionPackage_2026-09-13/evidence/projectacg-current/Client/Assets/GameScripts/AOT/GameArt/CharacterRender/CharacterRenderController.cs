using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum CharacterRenderMainLightAdjustmentMode
{
    Off = 0,
    FollowCamera = 1,
    BackLight = 2,
}

public enum CharacterRenderMainLightOverrideMode
{
    Off = 0,
    FollowCamera = 1,
    Custom = 2,
}

public enum MonsterRenderMainLightOverrideMode
{
    FollowCharacter = 0,
    Off = 1,
    FollowCamera = 2,
    Custom = 3,
}

public enum CharacterRenderSceneShadowMode
{
    Off = 0,
    UnityOnly = 1,
    POSOnly = 2,
    Both = 3,
}

public enum CharacterRenderRimDirectionSpace
{
    View = 0,
    World = 1,
}

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("GameArt/Character Render Controller")]
public sealed class CharacterRenderController : MonoBehaviour
{
    private const float MinDirectionMagnitude = 0.0001f;
    private const float DegreesRange = 360f;
    private const float DegreesStart = -180f;
    private const float MaxPerObjectShadowStrength = 2f;
    /// <summary>标记当前全局 Self/Environment Shadow 强度由 Character Render Control 提供。</summary>
    public const string PerObjectShadowPassStrengthOverrideShaderPropertyName =
        "_GlobalCharacterRenderPerObjectShadowPassStrengthOverride";
    /// <summary>Self Shadow 强度 Shader 全局属性名。</summary>
    public const string SelfShadowStrengthShaderPropertyName = "_GlobalCharacterRenderSelfShadowStrength";
    /// <summary>Environment Shadow 强度 Shader 全局属性名。</summary>
    public const string EnvironmentShadowStrengthShaderPropertyName =
        "_GlobalCharacterRenderEnvironmentShadowStrength";
    private static readonly Color DefaultSceneHandleColor = Color.yellow;
    private static readonly Color DefaultIndirectTintColor = Color.white;
    private static readonly Color DefaultPlanarShadowColor = Color.black;

    private static readonly int GlobalLightDirectionId = Shader.PropertyToID("_GlobalCharacterRenderLightDirection");
    private static readonly int GlobalLightColorId = Shader.PropertyToID("_GlobalCharacterRenderLightColor");
    private static readonly int GlobalLightStrengthId = Shader.PropertyToID("_GlobalCharacterRenderLightStrength");
    private static readonly int GlobalLightToggleId = Shader.PropertyToID("_GlobalCharacterRenderLightToggle");
    private static readonly int GlobalLightFollowCameraXZId =
        Shader.PropertyToID("_GlobalCharacterRenderLightFollowCameraXZ");
    private static readonly int GlobalBackLightToggleId = Shader.PropertyToID("_GlobalCharacterRenderBackLightToggle");
    private static readonly int GlobalVirtualLightCameraDirectionId = Shader.PropertyToID("_GlobalCharacterRenderVirtualLightCameraDirection");
    private static readonly int GlobalVirtualLightMaskCameraDirectionId = Shader.PropertyToID("_GlobalCharacterRenderVirtualLightMaskCameraDirection");
    private static readonly int GlobalVirtualLightColorId = Shader.PropertyToID("_GlobalCharacterRenderVirtualLightColor");
    private static readonly int GlobalVirtualLightIntensityId = Shader.PropertyToID("_GlobalCharacterRenderVirtualLightIntensity");
    private static readonly int GlobalVirtualLightOffsetId = Shader.PropertyToID("_GlobalCharacterRenderVirtualLightOffset");
    private static readonly int GlobalCharacterAdditionalLightsEnabledId = Shader.PropertyToID("_GlobalCharacterAdditionalLightsEnabled");
    private static readonly int GlobalCharacterAdditionalLightsDiffuseEnabledId =
        Shader.PropertyToID("_GlobalCharacterAdditionalLightsDiffuseEnabled");
    private static readonly int GlobalCharacterAdditionalLightsSpecularEnabledId =
        Shader.PropertyToID("_GlobalCharacterAdditionalLightsSpecularEnabled");
    private static readonly int GlobalPlanarShadowToggleId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowToggle");
    private static readonly int GlobalPlanarShadowDirectionModeId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowDirectionMode");
    private static readonly int GlobalPlanarShadowDirectionId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowDirection");
    private static readonly int GlobalPlanarShadowStrengthId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowStrength");
    private static readonly int GlobalPlanarShadowParametersOverrideId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowParametersOverride");
    private static readonly int GlobalPlanarShadowFalloffId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowFalloff");
    private static readonly int GlobalPlanarShadowRangeId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowRange");
    private static readonly int GlobalPlanarShadowPlaneHeightId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowPlaneHeight");
    private static readonly int GlobalPlanarShadowGlobalCenterId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowGlobalCenter");
    private static readonly int GlobalPlanarShadowColorId = Shader.PropertyToID("_GlobalCharacterRenderPlanarShadowColor");
    private static readonly int GlobalOutlineToggleId = Shader.PropertyToID("_GlobalCharacterRenderOutlineToggle");
    private static readonly int GlobalSceneShadowModeOverrideId = Shader.PropertyToID("_GlobalCharacterRenderSceneShadowModeOverride");
    private static readonly int GlobalSceneShadowModeId = Shader.PropertyToID("_GlobalCharacterRenderSceneShadowMode");
    private static readonly int GlobalRimLightToggleId = Shader.PropertyToID("_GlobalCharacterRenderRimLightToggle");
    private static readonly int GlobalRimCustomDirectionEnabledId = Shader.PropertyToID("_GlobalCharacterRenderRimCustomDirectionEnabled");
    private static readonly int GlobalRimDirectionSpaceId = Shader.PropertyToID("_GlobalCharacterRenderRimDirectionSpace");
    private static readonly int GlobalRimDirectionId = Shader.PropertyToID("_GlobalCharacterRenderRimDirection");
    private static readonly int GlobalRimFakePointMaskEnabledId = Shader.PropertyToID("_GlobalCharacterRenderRimFakePointMaskEnabled");
    private static readonly int GlobalRimFakePointMaskPositionId = Shader.PropertyToID("_GlobalCharacterRenderRimFakePointMaskPosition");
    private static readonly int GlobalRimFakePointMaskRangeId = Shader.PropertyToID("_GlobalCharacterRenderRimFakePointMaskRange");
    private static readonly int GlobalRimFakePointMaskPowerId = Shader.PropertyToID("_GlobalCharacterRenderRimFakePointMaskPower");
    private static readonly int GlobalRimFakeDirectMaskEnabledId = Shader.PropertyToID("_GlobalCharacterRenderRimFakeDirectMaskEnabled");
    private static readonly int GlobalRimFakeDirectMaskDirectionId = Shader.PropertyToID("_GlobalCharacterRenderRimFakeDirectMaskDirection");
    private static readonly int GlobalRimFakeDirectMaskPositionId = Shader.PropertyToID("_GlobalCharacterRenderRimFakeDirectMaskPosition");
    private static readonly int GlobalRimFakeDirectMaskRangeId = Shader.PropertyToID("_GlobalCharacterRenderRimFakeDirectMaskRange");
    private static readonly int GlobalRimIntensityId = Shader.PropertyToID("_GlobalCharacterRenderRimIntensity");
    private static readonly int GlobalFinalColorGradientToggleId = Shader.PropertyToID("_GlobalCharacterRenderFinalColorGradientToggle");
    private static readonly int GlobalFinalColorGradientParametersOverrideId = Shader.PropertyToID("_GlobalCharacterRenderFinalColorGradientParametersOverride");
    private static readonly int GlobalFinalColorGradientColorId = Shader.PropertyToID("_GlobalCharacterRenderFinalColorGradientColor");
    private static readonly int GlobalFinalColorGradientMinYId = Shader.PropertyToID("_GlobalCharacterRenderFinalColorGradientMinY");
    private static readonly int GlobalFinalColorGradientMaxYId = Shader.PropertyToID("_GlobalCharacterRenderFinalColorGradientMaxY");
    private static readonly int GlobalHighQualityShadowToggleId = Shader.PropertyToID("_GlobalCharacterRenderHighQualityShadowToggle");
    private static readonly int GlobalPerObjectShadowDirectionModeId = Shader.PropertyToID("_GlobalCharacterRenderPerObjectShadowDirectionMode");
    private static readonly int GlobalPerObjectShadowDirectionId = Shader.PropertyToID("_GlobalCharacterRenderPerObjectShadowDirection");
    private static readonly int GlobalPerObjectShadowPassStrengthOverrideId =
        Shader.PropertyToID(PerObjectShadowPassStrengthOverrideShaderPropertyName);
    private static readonly int GlobalSelfShadowStrengthId = Shader.PropertyToID(SelfShadowStrengthShaderPropertyName);
    private static readonly int GlobalEnvironmentShadowStrengthId =
        Shader.PropertyToID(EnvironmentShadowStrengthShaderPropertyName);
    private static readonly int GlobalSpecularShadowStrengthOverrideId = Shader.PropertyToID("_GlobalCharacterRenderSpecularShadowStrengthOverride");
    private static readonly int GlobalSpecularShadowStrengthId = Shader.PropertyToID("_GlobalCharacterRenderSpecularShadowStrength");
    private static readonly int GlobalIndirectIntensityId = Shader.PropertyToID("_GlobalIndirecIntensity");
    private static readonly int GlobalIndirectTintColorId = Shader.PropertyToID("_GlobalIndirectTintColor");
    private static readonly int GlobalOverrideSceneAmbientId = Shader.PropertyToID("_GlobalCharacterRenderOverrideSceneAmbient");

    private static readonly int GlobalMonsterLightDirectionId = Shader.PropertyToID("_GlobalMonsterRenderLightDirection");
    private static readonly int GlobalMonsterLightColorId = Shader.PropertyToID("_GlobalMonsterRenderLightColor");
    private static readonly int GlobalMonsterLightStrengthId = Shader.PropertyToID("_GlobalMonsterRenderLightStrength");
    private static readonly int GlobalMonsterLightToggleId = Shader.PropertyToID("_GlobalMonsterRenderLightToggle");
    private static readonly int GlobalMonsterLightFollowCameraXZId = Shader.PropertyToID("_GlobalMonsterRenderLightFollowCameraXZ");
    private static readonly int GlobalMonsterAdditionalLightsEnabledId = Shader.PropertyToID("_GlobalMonsterAdditionalLightsEnabled");
    private static readonly int GlobalMonsterAdditionalLightsDiffuseEnabledId = Shader.PropertyToID("_GlobalMonsterAdditionalLightsDiffuseEnabled");
    private static readonly int GlobalMonsterAdditionalLightsSpecularEnabledId = Shader.PropertyToID("_GlobalMonsterAdditionalLightsSpecularEnabled");
    private static readonly int GlobalMonsterOverrideSceneAmbientId = Shader.PropertyToID("_GlobalMonsterRenderOverrideSceneAmbient");
    private static readonly int GlobalMonsterIndirectIntensityId = Shader.PropertyToID("_GlobalMonsterIndirectIntensity");
    private static readonly int GlobalMonsterIndirectTintColorId = Shader.PropertyToID("_GlobalMonsterIndirectTintColor");
    private static readonly int GlobalMonsterOutlineToggleId = Shader.PropertyToID("_GlobalMonsterRenderOutlineToggle");
    private static readonly int GlobalMonsterRimLightToggleId = Shader.PropertyToID("_GlobalMonsterRenderRimLightToggle");
    private static readonly int GlobalMonsterFinalColorGradientToggleId = Shader.PropertyToID("_GlobalMonsterRenderFinalColorGradientToggle");
    private static readonly int GlobalMonsterFinalColorGradientParametersOverrideId = Shader.PropertyToID("_GlobalMonsterRenderFinalColorGradientParametersOverride");
    private static readonly int GlobalMonsterFinalColorGradientColorId = Shader.PropertyToID("_GlobalMonsterRenderFinalColorGradientColor");
    private static readonly int GlobalMonsterFinalColorGradientMinYId = Shader.PropertyToID("_GlobalMonsterRenderFinalColorGradientMinY");
    private static readonly int GlobalMonsterFinalColorGradientMaxYId = Shader.PropertyToID("_GlobalMonsterRenderFinalColorGradientMaxY");

 
    private static readonly int GlobalGunLightDirectionId = Shader.PropertyToID("_GlobalGunRenderLightDirection");
    private static readonly int GlobalGunLightColorId = Shader.PropertyToID("_GlobalGunRenderLightColor");
    private static readonly int GlobalGunLightStrengthId = Shader.PropertyToID("_GlobalGunRenderLightStrength");
    private static readonly int GlobalGunLightToggleId = Shader.PropertyToID("_GlobalGunRenderLightToggle");
    private static readonly int GlobalGunLightFollowCameraXZId = Shader.PropertyToID("_GlobalGunRenderLightFollowCameraXZ");
    private static readonly int GlobalGunAdditionalLightsEnabledId = Shader.PropertyToID("_GlobalGunAdditionalLightsEnabled");
    private static readonly int GlobalGunAdditionalLightsDiffuseEnabledId = Shader.PropertyToID("_GlobalGunAdditionalLightsDiffuseEnabled");
    private static readonly int GlobalGunAdditionalLightsSpecularEnabledId = Shader.PropertyToID("_GlobalGunAdditionalLightsSpecularEnabled");
    private static readonly int GlobalGunOverrideSceneAmbientId = Shader.PropertyToID("_GlobalGunRenderOverrideSceneAmbient");
    private static readonly int GlobalGunIndirectIntensityId = Shader.PropertyToID("_GlobalGunIndirectIntensity");
    private static readonly int GlobalGunIndirectTintColorId = Shader.PropertyToID("_GlobalGunIndirectTintColor");
    private static readonly int GlobalGunOutlineToggleId = Shader.PropertyToID("_GlobalGunRenderOutlineToggle");
    private static readonly int GlobalGunRimLightToggleId = Shader.PropertyToID("_GlobalGunRenderRimLightToggle");
    private static readonly int GlobalGunFinalColorGradientToggleId = Shader.PropertyToID("_GlobalGunRenderFinalColorGradientToggle");
    private static readonly int GlobalGunFinalColorGradientParametersOverrideId = Shader.PropertyToID("_GlobalGunRenderFinalColorGradientParametersOverride");
    private static readonly int GlobalGunFinalColorGradientColorId = Shader.PropertyToID("_GlobalGunRenderFinalColorGradientColor");
    private static readonly int GlobalGunFinalColorGradientMinYId = Shader.PropertyToID("_GlobalGunRenderFinalColorGradientMinY");
    private static readonly int GlobalGunFinalColorGradientMaxYId = Shader.PropertyToID("_GlobalGunRenderFinalColorGradientMaxY");
    [SerializeField] private CharacterRenderLightProfile _profile;
    [Header("角色直接光设置")]
    [SerializeField] private CharacterRenderMainLightOverrideMode _mainLightOverrideMode = CharacterRenderMainLightOverrideMode.Off;
    [SerializeField] private Color _customLightColor = Color.white;
    [SerializeField] [Min(0f)] private float _customLightStrength = 1f;
    [SerializeField] private Vector3 _customLightDirection = Vector3.zero;
    [SerializeField] private Vector3 _virtualLightCameraDirectionOffsetEuler = Vector3.zero;
    [SerializeField] private bool _enableBackLight;
    [SerializeField] private Color _virtualLightColor = Color.white;
    [SerializeField] [Min(0f)] private float _virtualLightIntensity = 0.2f;
    [SerializeField] [Range(-1f, 1f)] private float _virtualLightOffset;
    [SerializeField] private bool _enableCharacterAdditionalLights = true;
    [SerializeField] private bool _enableCharacterAdditionalLightDiffuse = true;
    [SerializeField] private bool _enableCharacterAdditionalLightSpecular = true;
    [SerializeField] private bool _enablePlanarShadow = true;
    [SerializeField] private bool _enablePlanarShadowDirectionFollowCustomLight = false;
    [SerializeField] private Vector3 _planarShadowDirection = Vector3.zero;
    [SerializeField] [Min(0f)] private float _planarShadowStrength = 1f;
    [SerializeField] [Min(0f)] private float _planarShadowFalloff = 1f;
    [SerializeField] private float _planarShadowRange;
    [SerializeField] private float _planarShadowPlaneHeight;
    [SerializeField] private Vector3 _planarShadowGlobalCenter = Vector3.zero;
    [SerializeField] private Color _planarShadowColor = DefaultPlanarShadowColor;
    [SerializeField] private CharacterRenderSceneShadowMode _sceneShadowMode = CharacterRenderSceneShadowMode.UnityOnly;
    [SerializeField] private bool _enableHighQualityShadow = true;
    [SerializeField] private bool _enablePerObjectShadowDirectionFollowCustomLight = false;
    [SerializeField] private bool _enablePerObjectShadowDirectionFollowMainLightAdjustment = false;
    [SerializeField] private Vector3 _perObjectShadowDirection = Vector3.zero;
    [FormerlySerializedAs("_perObjectShadowStrength")]
    [SerializeField] [Range(0f, MaxPerObjectShadowStrength)] private float _selfShadowStrength = 1f;
    [FormerlySerializedAs("_baseLitPerObjectShadowStrength")]
    [SerializeField] [Range(0f, MaxPerObjectShadowStrength)] private float _environmentShadowStrength = 1f;
    [SerializeField] [Range(0f, 1f)] private float _specularShadowStrength = 1f;
    [SerializeField] private bool _enableOutline = true;
    [SerializeField] private bool _enableRimLight = true;
    [SerializeField] private bool _enableRimCustomDirection;
    [SerializeField] private CharacterRenderRimDirectionSpace _rimDirectionSpace = CharacterRenderRimDirectionSpace.World;
    [SerializeField] private Vector3 _rimDirection = Vector3.zero;
    [SerializeField] private bool _enableRimFakePointMask;
    [SerializeField] private Vector3 _rimFakePointMaskPosition = Vector3.zero;
    [SerializeField] [Min(0.01f)] private float _rimFakePointMaskRange = 1f;
    [SerializeField] [Range(0.25f, 8f)] private float _rimFakePointMaskPower = 2f;
    [SerializeField] private bool _enableRimFakeDirectMask;
    [SerializeField] private Vector3 _rimFakeDirectMaskDirection = Vector3.right;
    [SerializeField] private Vector3 _rimFakeDirectMaskPosition = Vector3.zero;
    [SerializeField] [Min(0.01f)] private float _rimFakeDirectMaskRange = 1f;
    [SerializeField] [Min(0f)] private float _rimIntensity = 1f;
    [SerializeField] private bool _enableFinalColorGradient = true;
    [SerializeField] private Color _finalColorGradientColor = Color.white;
    [SerializeField] private float _finalColorGradientMinY;
    [SerializeField] private float _finalColorGradientMaxY = 1f;
    [SerializeField] [HideInInspector] private bool _enableCustomLight;
    [SerializeField] [HideInInspector] private CharacterRenderMainLightAdjustmentMode _mainLightAdjustmentMode = CharacterRenderMainLightAdjustmentMode.Off;
    [SerializeField] [HideInInspector] private bool _directLightHierarchyMigrated = true;
    [SerializeField] [HideInInspector] private int _directLightHierarchyVersion;
    [SerializeField] [HideInInspector] private bool _hierarchyFeatureDefaultsInitialized = true;
    [SerializeField] [HideInInspector] private int _hierarchyFeatureVersion;
    [SerializeField] [HideInInspector] private bool _shadowToggleDefaultsInitialized = true;
    [SerializeField] [HideInInspector] private bool _shadowDirectionDefaultsInitialized = true;
    [SerializeField] [HideInInspector] private bool _outlineToggleDefaultInitialized = true;
    [SerializeField] [HideInInspector] private bool _additionalLightsToggleDefaultInitialized = true;
    [SerializeField] [HideInInspector] private int _additionalLightsLobeToggleVersion;
    [SerializeField] [HideInInspector] private float _lightAnchorOrbit;
    [SerializeField] [HideInInspector] private float _lightAnchorElevation = 90f;

    [Header("角色环境光设置")]
    [SerializeField] private bool _enableGlobalIndirectLight = true;
    [SerializeField] private bool _overrideSceneAmbient;
    [SerializeField] [Min(0f)] private float _globalIndirectIntensity = 1f;
    [SerializeField] private Color _globalIndirectTintColor = DefaultIndirectTintColor;

    [Header("怪物直接光设置")]
    [SerializeField] private MonsterRenderMainLightOverrideMode _monsterMainLightOverrideMode = MonsterRenderMainLightOverrideMode.FollowCharacter;
    [SerializeField] private Color _monsterCustomLightColor = Color.white;
    [SerializeField] [Min(0f)] private float _monsterCustomLightStrength = 1f;
    [SerializeField] private Vector3 _monsterCustomLightDirection = Vector3.zero;
    [SerializeField] private Vector3 _monsterVirtualLightCameraDirectionOffsetEuler = Vector3.zero;
    [SerializeField] private bool _monsterAdditionalLightsEnabled = true;
    [SerializeField] private bool _monsterAdditionalLightDiffuseEnabled = true;
    [SerializeField] private bool _monsterAdditionalLightSpecularEnabled = true;

    [Header("怪物环境光设置")]
    [SerializeField] private bool _monsterEnvironmentFollowCharacter = true;
    [SerializeField] private bool _monsterEnableGlobalIndirectLight = true;
    [SerializeField] private bool _monsterOverrideSceneAmbient;
    [SerializeField] [Min(0f)] private float _monsterGlobalIndirectIntensity = 1f;
    [SerializeField] private Color _monsterGlobalIndirectTintColor = DefaultIndirectTintColor;

    [Header("怪物其他特性设置")]
    [SerializeField] private bool _monsterOtherFeaturesFollowCharacter = true;
    [SerializeField] private bool _monsterEnableOutline = true;
    [SerializeField] private bool _monsterEnableRimLight = true;
    [SerializeField] private bool _monsterEnableFinalColorGradient = true;
    [SerializeField] private Color _monsterFinalColorGradientColor = Color.white;
    [SerializeField] private float _monsterFinalColorGradientMinY;
    [SerializeField] private float _monsterFinalColorGradientMaxY = 1f;

    [Header("Scene 手柄")]
    [SerializeField] [HideInInspector] private bool _showSceneDirection = true;
    [SerializeField] [HideInInspector] private bool _showVirtualLightCameraDirection;
    [SerializeField] [HideInInspector] private bool _showPlanarShadowSceneDirection;
    [SerializeField] [HideInInspector] private bool _showPerObjectShadowSceneDirection;
    [SerializeField] [HideInInspector] private bool _showRimCustomDirection;
    [SerializeField] [HideInInspector] private bool _showRimFakePointMaskPosition;
    [SerializeField] [HideInInspector] private bool _showRimFakeDirectMaskDirection;
    [SerializeField] [HideInInspector] private bool _showRimFakeDirectMaskPosition;

    public CharacterRenderLightProfile Profile
    {
        get => _profile;
        set
        {
            _profile = value;
            if (isActiveAndEnabled)
            {
                ApplyNow();
            }
        }
    }

    public bool HasProfile => _profile != null;

    public CharacterRenderMainLightOverrideMode MainLightOverrideMode
    {
        get
        {
            if (_profile != null)
            {
                return _profile.MainLightOverrideMode;
            }

            EnsureDirectLightHierarchyMigrated();
            return _mainLightOverrideMode;
        }
    }

    // 兼容旧调用；新代码应使用 MainLightOverrideMode。
    public bool EnableCustomLight => MainLightOverrideMode != CharacterRenderMainLightOverrideMode.Off;

    public bool EnableBackLight
    {
        get
        {
            if (_profile != null)
            {
                return _profile.EnableBackLight;
            }

            EnsureDirectLightHierarchyMigrated();
            return _enableBackLight;
        }
    }

    public bool EnableCharacterAdditionalLights
    {
        get
        {
            if (_profile != null)
            {
                return _profile.EnableCharacterAdditionalLights;
            }

            EnsureAdditionalLightsToggleDefaultInitialized();
            return _enableCharacterAdditionalLights;
        }
    }

    public bool EnableCharacterAdditionalLightDiffuse
    {
        get
        {
            if (_profile != null)
            {
                return _profile.EnableCharacterAdditionalLightDiffuse;
            }

            EnsureAdditionalLightsLobeToggleDefaultsInitialized();
            return _enableCharacterAdditionalLightDiffuse;
        }
    }

    public bool EnableCharacterAdditionalLightSpecular
    {
        get
        {
            if (_profile != null)
            {
                return _profile.EnableCharacterAdditionalLightSpecular;
            }

            EnsureAdditionalLightsLobeToggleDefaultsInitialized();
            return _enableCharacterAdditionalLightSpecular;
        }
    }

    public Color CustomLightColor => _profile != null ? _profile.CustomLightColor : _customLightColor;
    public float CustomLightStrength => _profile != null ? _profile.CustomLightStrength : Mathf.Max(0f, _customLightStrength);

    public CharacterRenderMainLightAdjustmentMode MainLightAdjustmentMode =>
        MainLightOverrideMode == CharacterRenderMainLightOverrideMode.FollowCamera
            ? CharacterRenderMainLightAdjustmentMode.FollowCamera
            : CharacterRenderMainLightAdjustmentMode.Off;

    public Vector3 VirtualLightCameraDirectionOffsetEuler =>
        _profile != null ? _profile.VirtualLightCameraDirectionOffsetEuler : _virtualLightCameraDirectionOffsetEuler;

    public Color VirtualLightColor => _profile != null ? _profile.VirtualLightColor : _virtualLightColor;

    public float VirtualLightIntensity =>
        _profile != null ? _profile.VirtualLightIntensity : Mathf.Max(0f, _virtualLightIntensity);

    public float VirtualLightOffset =>
        _profile != null ? _profile.VirtualLightOffset : Mathf.Clamp(_virtualLightOffset, -1f, 1f);

    public bool EnablePlanarShadow
    {
        get
        {
            if (_profile != null)
            {
                return _profile.EnablePlanarShadow;
            }

            EnsureShadowToggleDefaultsInitialized();
            return _enablePlanarShadow;
        }
    }

    public bool EnableHighQualityShadow
    {
        get
        {
            if (_profile != null)
            {
                return _profile.EnableHighQualityShadow;
            }

            EnsureShadowToggleDefaultsInitialized();
            return _enableHighQualityShadow;
        }
    }

    public bool EnablePlanarShadowDirectionFollowMainLightOverride =>
        _profile != null
            ? _profile.EnablePlanarShadowDirectionFollowMainLightOverride
            : _enablePlanarShadowDirectionFollowCustomLight;

    public bool EnablePlanarShadowDirectionFollowCustomLight => EnablePlanarShadowDirectionFollowMainLightOverride;

    public float PlanarShadowStrength =>
        _profile != null
            ? _profile.PlanarShadowStrength
            : Mathf.Max(0f, _planarShadowStrength);

    public float PlanarShadowFalloff =>
        _profile != null
            ? _profile.PlanarShadowFalloff
            : Mathf.Max(0f, _planarShadowFalloff);

    public float PlanarShadowRange => _profile != null ? _profile.PlanarShadowRange : _planarShadowRange;

    public float PlanarShadowPlaneHeight =>
        _profile != null ? _profile.PlanarShadowPlaneHeight : _planarShadowPlaneHeight;

    public Vector3 PlanarShadowGlobalCenter =>
        _profile != null ? _profile.PlanarShadowGlobalCenter : _planarShadowGlobalCenter;

    public Color PlanarShadowColor => _profile != null ? _profile.PlanarShadowColor : _planarShadowColor;

    public CharacterRenderSceneShadowMode SceneShadowMode
    {
        get
        {
            if (_profile != null)
            {
                return _profile.SceneShadowMode;
            }

            EnsureHierarchyFeatureDefaultsInitialized();
            return _sceneShadowMode;
        }
    }

    public bool EnableOutline
    {
        get
        {
            if (_profile != null)
            {
                return _profile.EnableOutline;
            }

            EnsureOutlineToggleDefaultInitialized();
            return _enableOutline;
        }
    }

    public bool EnablePerObjectShadowDirectionFollowMainLightOverride =>
        _profile != null
            ? _profile.EnablePerObjectShadowDirectionFollowMainLightOverride
            : _enablePerObjectShadowDirectionFollowMainLightAdjustment || _enablePerObjectShadowDirectionFollowCustomLight;

    public bool EnablePerObjectShadowDirectionFollowCustomLight => EnablePerObjectShadowDirectionFollowMainLightOverride;
    public bool EnablePerObjectShadowDirectionFollowMainLightAdjustment => EnablePerObjectShadowDirectionFollowMainLightOverride;

    public float SelfShadowStrength =>
        _profile != null
            ? _profile.SelfShadowStrength
            : Mathf.Clamp(_selfShadowStrength, 0f, MaxPerObjectShadowStrength);

    public float EnvironmentShadowStrength =>
        _profile != null
            ? _profile.EnvironmentShadowStrength
            : Mathf.Clamp(_environmentShadowStrength, 0f, MaxPerObjectShadowStrength);

    public float SpecularShadowStrength =>
        _profile != null
            ? _profile.SpecularShadowStrength
            : Mathf.Clamp01(_specularShadowStrength);

    public bool EnableGlobalIndirectLight => _profile != null ? _profile.EnableGlobalIndirectLight : _enableGlobalIndirectLight;

    public bool OverrideSceneAmbient => _profile != null ? _profile.OverrideSceneAmbient : _overrideSceneAmbient;

    public float GlobalIndirectIntensity => _profile != null ? _profile.GlobalIndirectIntensity : Mathf.Max(0f, _globalIndirectIntensity);

    public Color GlobalIndirectTintColor => _profile != null ? _profile.GlobalIndirectTintColor : _globalIndirectTintColor;

    public MonsterRenderMainLightOverrideMode MonsterMainLightOverrideMode =>
        _profile != null ? _profile.MonsterMainLightOverrideMode : _monsterMainLightOverrideMode;
    public Color MonsterCustomLightColor => _profile != null ? _profile.MonsterCustomLightColor : _monsterCustomLightColor;
    public float MonsterCustomLightStrength => _profile != null
        ? _profile.MonsterCustomLightStrength
        : Mathf.Max(0f, _monsterCustomLightStrength);
    public Vector3 MonsterCustomLightDirection => _profile != null
        ? _profile.MonsterCustomLightDirection
        : NormalizeDirectionOrZero(_monsterCustomLightDirection);
    public Vector3 MonsterVirtualLightCameraDirectionOffsetEuler => _profile != null
        ? _profile.MonsterVirtualLightCameraDirectionOffsetEuler
        : _monsterVirtualLightCameraDirectionOffsetEuler;
    public bool MonsterAdditionalLightsEnabled => _profile != null
        ? _profile.MonsterAdditionalLightsEnabled
        : _monsterAdditionalLightsEnabled;
    public bool MonsterAdditionalLightDiffuseEnabled => _profile != null
        ? _profile.MonsterAdditionalLightDiffuseEnabled
        : _monsterAdditionalLightDiffuseEnabled;
    public bool MonsterAdditionalLightSpecularEnabled => _profile != null
        ? _profile.MonsterAdditionalLightSpecularEnabled
        : _monsterAdditionalLightSpecularEnabled;
    public bool MonsterEnvironmentFollowCharacter => _profile != null
        ? _profile.MonsterEnvironmentFollowCharacter
        : _monsterEnvironmentFollowCharacter;
    public bool MonsterEnableGlobalIndirectLight => _profile != null
        ? _profile.MonsterEnableGlobalIndirectLight
        : _monsterEnableGlobalIndirectLight;
    public bool MonsterOverrideSceneAmbient => _profile != null
        ? _profile.MonsterOverrideSceneAmbient
        : _monsterOverrideSceneAmbient;
    public float MonsterGlobalIndirectIntensity => _profile != null
        ? _profile.MonsterGlobalIndirectIntensity
        : Mathf.Max(0f, _monsterGlobalIndirectIntensity);
    public Color MonsterGlobalIndirectTintColor => _profile != null
        ? _profile.MonsterGlobalIndirectTintColor
        : _monsterGlobalIndirectTintColor;
    public bool MonsterOtherFeaturesFollowCharacter => _profile != null
        ? _profile.MonsterOtherFeaturesFollowCharacter
        : _monsterOtherFeaturesFollowCharacter;
    public bool MonsterEnableOutline => _profile != null ? _profile.MonsterEnableOutline : _monsterEnableOutline;
    public bool MonsterEnableRimLight => _profile != null ? _profile.MonsterEnableRimLight : _monsterEnableRimLight;
    public bool MonsterEnableFinalColorGradient => _profile != null
        ? _profile.MonsterEnableFinalColorGradient
        : _monsterEnableFinalColorGradient;
    public Color MonsterFinalColorGradientColor => _profile != null
        ? _profile.MonsterFinalColorGradientColor
        : _monsterFinalColorGradientColor;
    public float MonsterFinalColorGradientMinY => _profile != null
        ? _profile.MonsterFinalColorGradientMinY
        : _monsterFinalColorGradientMinY;
    public float MonsterFinalColorGradientMaxY => _profile != null
        ? _profile.MonsterFinalColorGradientMaxY
        : _monsterFinalColorGradientMaxY;

    public float LightAnchorOrbit => _lightAnchorOrbit;

    public float LightAnchorElevation => _lightAnchorElevation;

    public bool ShowSceneDirection => _showSceneDirection;

    public bool ShowVirtualLightCameraDirection => _showVirtualLightCameraDirection;

    public bool ShowPlanarShadowSceneDirection => _showPlanarShadowSceneDirection;

    public bool ShowPerObjectShadowSceneDirection => _showPerObjectShadowSceneDirection;

    public bool ShowRimCustomDirection => _showRimCustomDirection;

    public bool ShowRimFakePointMaskPosition => _showRimFakePointMaskPosition;

    public bool ShowRimFakeDirectMaskDirection => _showRimFakeDirectMaskDirection;

    public bool ShowRimFakeDirectMaskPosition => _showRimFakeDirectMaskPosition;

    public float SceneHandleLength => 1f;

    public Color SceneHandleColor => DefaultSceneHandleColor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeGlobalShaderDefaults()
    {
        ClearGlobalLight();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void InitializeEditorGlobalShaderDefaults()
    {
        ClearGlobalLight();
    }
#endif

    private void Reset()
    {
        _mainLightOverrideMode = CharacterRenderMainLightOverrideMode.Off;
        _enableCustomLight = false;
        _mainLightAdjustmentMode = CharacterRenderMainLightAdjustmentMode.Off;
        _directLightHierarchyMigrated = true;
        _directLightHierarchyVersion = 1;
        _customLightColor = Color.white;
        _customLightStrength = 1f;
        _customLightDirection = Vector3.zero;
        _enableBackLight = false;
        _virtualLightColor = Color.white;
        _virtualLightIntensity = 0.2f;
        _virtualLightOffset = 0f;
        _enableCharacterAdditionalLights = true;
        _enableCharacterAdditionalLightDiffuse = true;
        _enableCharacterAdditionalLightSpecular = true;
        _enablePlanarShadow = true;
        _enablePlanarShadowDirectionFollowCustomLight = false;
        _planarShadowDirection = Vector3.zero;
        _planarShadowStrength = 1f;
        _planarShadowFalloff = 1f;
        _planarShadowRange = 0f;
        _planarShadowPlaneHeight = 0f;
        _planarShadowGlobalCenter = Vector3.zero;
        _planarShadowColor = DefaultPlanarShadowColor;
        _sceneShadowMode = CharacterRenderSceneShadowMode.UnityOnly;
        _enableOutline = true;
        _enableRimLight = true;
        _enableFinalColorGradient = true;
        _finalColorGradientColor = Color.white;
        _finalColorGradientMinY = 0f;
        _finalColorGradientMaxY = 1f;
        _hierarchyFeatureDefaultsInitialized = true;
        _hierarchyFeatureVersion = 1;
        _enableHighQualityShadow = true;
        _enablePerObjectShadowDirectionFollowCustomLight = false;
        _enablePerObjectShadowDirectionFollowMainLightAdjustment = false;
        _perObjectShadowDirection = Vector3.zero;
        _selfShadowStrength = 1f;
        _environmentShadowStrength = 1f;
        _specularShadowStrength = 1f;
        _shadowToggleDefaultsInitialized = true;
        _shadowDirectionDefaultsInitialized = true;
        _outlineToggleDefaultInitialized = true;
        _additionalLightsToggleDefaultInitialized = true;
        _additionalLightsLobeToggleVersion = 1;
        _enableGlobalIndirectLight = true;
        _overrideSceneAmbient = false;
        _globalIndirectIntensity = 1f;
        _globalIndirectTintColor = DefaultIndirectTintColor;
        _monsterMainLightOverrideMode = MonsterRenderMainLightOverrideMode.FollowCharacter;
        _monsterCustomLightColor = Color.white;
        _monsterCustomLightStrength = 1f;
        _monsterCustomLightDirection = Vector3.zero;
        _monsterVirtualLightCameraDirectionOffsetEuler = Vector3.zero;
        _monsterAdditionalLightsEnabled = true;
        _monsterAdditionalLightDiffuseEnabled = true;
        _monsterAdditionalLightSpecularEnabled = true;
        _monsterEnvironmentFollowCharacter = true;
        _monsterEnableGlobalIndirectLight = true;
        _monsterOverrideSceneAmbient = false;
        _monsterGlobalIndirectIntensity = 1f;
        _monsterGlobalIndirectTintColor = DefaultIndirectTintColor;
        _monsterOtherFeaturesFollowCharacter = true;
        _monsterEnableOutline = true;
        _monsterEnableRimLight = true;
        _monsterEnableFinalColorGradient = true;
        _monsterFinalColorGradientColor = Color.white;
        _monsterFinalColorGradientMinY = 0f;
        _monsterFinalColorGradientMaxY = 1f;
        _virtualLightCameraDirectionOffsetEuler = Vector3.zero;
        SyncAnchorAnglesFromDirection();
        _showSceneDirection = true;
        _showVirtualLightCameraDirection = false;
        _showPlanarShadowSceneDirection = false;
        _showPerObjectShadowSceneDirection = false;
        ApplyNow();
    }

    private void OnEnable()
    {
        EnsureDirectLightHierarchyMigrated();
        EnsureShadowFollowSemantics();
        EnsureHierarchyFeatureDefaultsInitialized();
        EnsureShadowToggleDefaultsInitialized();
        EnsureShadowDirectionDefaultsInitialized();
        EnsureOutlineToggleDefaultInitialized();
        EnsureAdditionalLightsToggleDefaultInitialized();
        EnsureAdditionalLightsLobeToggleDefaultsInitialized();
        SanitizeDirections();
        SyncAnchorAnglesFromDirection();
#if UNITY_EDITOR
        CharacterRenderLightProfile.ProfileChanged -= HandleProfileChanged;
        CharacterRenderLightProfile.ProfileChanged += HandleProfileChanged;
#endif
        RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        ApplyNow();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
#if UNITY_EDITOR
        CharacterRenderLightProfile.ProfileChanged -= HandleProfileChanged;
#endif
        ApplyReplacementOrClear();
    }

    private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!isActiveAndEnabled || camera == null)
        {
            return;
        }

        ApplyNow(camera);
    }

    private void OnValidate()
    {
        EnsureDirectLightHierarchyMigrated();
        EnsureShadowFollowSemantics();
        EnsureHierarchyFeatureDefaultsInitialized();
        EnsureShadowToggleDefaultsInitialized();
        EnsureShadowDirectionDefaultsInitialized();
        EnsureOutlineToggleDefaultInitialized();
        EnsureAdditionalLightsToggleDefaultInitialized();
        EnsureAdditionalLightsLobeToggleDefaultsInitialized();
        _customLightStrength = Mathf.Max(0f, _customLightStrength);
        _planarShadowStrength = Mathf.Max(0f, _planarShadowStrength);
        _planarShadowFalloff = Mathf.Max(0f, _planarShadowFalloff);
        _selfShadowStrength = Mathf.Clamp(_selfShadowStrength, 0f, MaxPerObjectShadowStrength);
        _environmentShadowStrength = Mathf.Clamp(_environmentShadowStrength, 0f, MaxPerObjectShadowStrength);
        _specularShadowStrength = Mathf.Clamp01(_specularShadowStrength);
        _rimFakePointMaskRange = Mathf.Max(0.01f, _rimFakePointMaskRange);
        _rimFakePointMaskPower = Mathf.Clamp(_rimFakePointMaskPower, 0.25f, 8f);
        _rimFakeDirectMaskRange = Mathf.Max(0.01f, _rimFakeDirectMaskRange);
        _rimIntensity = Mathf.Max(0f, _rimIntensity);
        _globalIndirectIntensity = Mathf.Max(0f, _globalIndirectIntensity);
        _monsterCustomLightStrength = Mathf.Max(0f, _monsterCustomLightStrength);
        _monsterGlobalIndirectIntensity = Mathf.Max(0f, _monsterGlobalIndirectIntensity);
        SanitizeDirections();
        NormalizeAnchorAngles();
        SyncAnchorAnglesFromDirection();

        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyNow();
    }

    private void OnDidApplyAnimationProperties()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyNow();
    }

    [ContextMenu("应用角色与怪物渲染设置")]
    public void ApplyNow()
    {
        ApplyNow(Camera.main);
    }

    private void ApplyNow(Camera currentCamera)
    {
        // 控制器同时写入角色与怪物全局参数；怪物跟随项在这里解析为角色当前值。
        CharacterRenderMainLightOverrideMode mainLightOverrideMode = MainLightOverrideMode;
        Vector3 direction = mainLightOverrideMode == CharacterRenderMainLightOverrideMode.FollowCamera
            ? GetVirtualLightCameraDirection(currentCamera)
            : GetWorldDirection();
        Vector3 virtualLightMaskCameraDirection = GetVirtualLightMaskCameraDirection(currentCamera);
        Vector3 planarShadowDirection = GetPlanarShadowDirection();
        Vector3 perObjectShadowDirection = GetHighQualityShadowDirection(currentCamera);
        bool enablePlanarShadow = EnablePlanarShadow;
        bool enableOutline = EnableOutline;
        CharacterRenderSceneShadowMode sceneShadowMode = SceneShadowMode;
        bool sceneShadowUsesPerObjectShadow = sceneShadowMode == CharacterRenderSceneShadowMode.POSOnly
            || sceneShadowMode == CharacterRenderSceneShadowMode.Both;
        bool enableHighQualityShadow = sceneShadowUsesPerObjectShadow;
        Shader.SetGlobalVector(GlobalLightDirectionId, new Vector4(direction.x, direction.y, direction.z, 0f));
        Shader.SetGlobalColor(GlobalLightColorId, CustomLightColor);
        Shader.SetGlobalFloat(GlobalLightStrengthId, CustomLightStrength);
        Shader.SetGlobalFloat(
            GlobalLightToggleId,
            mainLightOverrideMode == CharacterRenderMainLightOverrideMode.Off ? 0f : 1f);
        Shader.SetGlobalFloat(
            GlobalLightFollowCameraXZId,
            mainLightOverrideMode == CharacterRenderMainLightOverrideMode.FollowCamera ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalBackLightToggleId, EnableBackLight ? 1f : 0f);
        Shader.SetGlobalVector(
            GlobalVirtualLightCameraDirectionId,
            new Vector4(
                direction.x,
                direction.y,
                direction.z,
                0f));
        Shader.SetGlobalVector(
            GlobalVirtualLightMaskCameraDirectionId,
            new Vector4(
                virtualLightMaskCameraDirection.x,
                virtualLightMaskCameraDirection.y,
                virtualLightMaskCameraDirection.z,
                0f));
        Shader.SetGlobalColor(GlobalVirtualLightColorId, VirtualLightColor);
        Shader.SetGlobalFloat(GlobalVirtualLightIntensityId, VirtualLightIntensity);
        Shader.SetGlobalFloat(GlobalVirtualLightOffsetId, VirtualLightOffset);
        Shader.SetGlobalFloat(GlobalCharacterAdditionalLightsEnabledId, EnableCharacterAdditionalLights ? 1f : 0f);
        Shader.SetGlobalFloat(
            GlobalCharacterAdditionalLightsDiffuseEnabledId,
            EnableCharacterAdditionalLightDiffuse ? 1f : 0f);
        Shader.SetGlobalFloat(
            GlobalCharacterAdditionalLightsSpecularEnabledId,
            EnableCharacterAdditionalLightSpecular ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalPlanarShadowToggleId, enablePlanarShadow ? 1f : 0f);
        Shader.SetGlobalFloat(
            GlobalPlanarShadowDirectionModeId,
            enablePlanarShadow
                ? (EnablePlanarShadowDirectionFollowMainLightOverride
                    && mainLightOverrideMode != CharacterRenderMainLightOverrideMode.Off
                    ? 2f
                    : (planarShadowDirection.sqrMagnitude >= MinDirectionMagnitude ? 1f : 0f))
                : 0f);
        Shader.SetGlobalVector(
            GlobalPlanarShadowDirectionId,
            new Vector4(planarShadowDirection.x, planarShadowDirection.y, planarShadowDirection.z, 0f));
        Shader.SetGlobalFloat(GlobalPlanarShadowStrengthId, enablePlanarShadow ? PlanarShadowStrength : 0f);
        Shader.SetGlobalFloat(GlobalPlanarShadowParametersOverrideId, 1f);
        Shader.SetGlobalFloat(GlobalPlanarShadowFalloffId, PlanarShadowFalloff);
        Shader.SetGlobalFloat(GlobalPlanarShadowRangeId, PlanarShadowRange);
        Shader.SetGlobalFloat(GlobalPlanarShadowPlaneHeightId, PlanarShadowPlaneHeight);
        Vector3 planarShadowGlobalCenter = PlanarShadowGlobalCenter;
        Shader.SetGlobalVector(
            GlobalPlanarShadowGlobalCenterId,
            new Vector4(planarShadowGlobalCenter.x, planarShadowGlobalCenter.y, planarShadowGlobalCenter.z, 0f));
        Shader.SetGlobalColor(GlobalPlanarShadowColorId, PlanarShadowColor);
        Shader.SetGlobalFloat(GlobalOutlineToggleId, enableOutline ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalSceneShadowModeOverrideId, 1f);
        Shader.SetGlobalFloat(GlobalSceneShadowModeId, (float)sceneShadowMode);
        Shader.SetGlobalFloat(GlobalRimLightToggleId, EnableRimLight ? 1f : 0f);
        Vector3 rimDirection = RimDirection;
        Vector3 rimFakeDirectMaskDirection = RimFakeDirectMaskDirection;
        Vector3 rimFakePointMaskPosition = RimFakePointMaskPosition;
        Vector3 rimFakeDirectMaskPosition = RimFakeDirectMaskPosition;
        Shader.SetGlobalFloat(GlobalRimCustomDirectionEnabledId, EnableRimCustomDirection ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalRimDirectionSpaceId, (float)RimDirectionSpace);
        Shader.SetGlobalVector(GlobalRimDirectionId, new Vector4(rimDirection.x, rimDirection.y, rimDirection.z, 0f));
        Shader.SetGlobalFloat(GlobalRimFakePointMaskEnabledId, EnableRimFakePointMask ? 1f : 0f);
        Shader.SetGlobalVector(GlobalRimFakePointMaskPositionId, new Vector4(rimFakePointMaskPosition.x, rimFakePointMaskPosition.y, rimFakePointMaskPosition.z, 0f));
        Shader.SetGlobalFloat(GlobalRimFakePointMaskRangeId, RimFakePointMaskRange);
        Shader.SetGlobalFloat(GlobalRimFakePointMaskPowerId, RimFakePointMaskPower);
        Shader.SetGlobalFloat(GlobalRimFakeDirectMaskEnabledId, EnableRimFakeDirectMask ? 1f : 0f);
        Shader.SetGlobalVector(GlobalRimFakeDirectMaskDirectionId, new Vector4(rimFakeDirectMaskDirection.x, rimFakeDirectMaskDirection.y, rimFakeDirectMaskDirection.z, 0f));
        Shader.SetGlobalVector(GlobalRimFakeDirectMaskPositionId, new Vector4(rimFakeDirectMaskPosition.x, rimFakeDirectMaskPosition.y, rimFakeDirectMaskPosition.z, 0f));
        Shader.SetGlobalFloat(GlobalRimFakeDirectMaskRangeId, RimFakeDirectMaskRange);
        Shader.SetGlobalFloat(GlobalRimIntensityId, RimIntensity);
        Shader.SetGlobalFloat(GlobalFinalColorGradientToggleId, EnableFinalColorGradient ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalFinalColorGradientParametersOverrideId, 1f);
        Shader.SetGlobalColor(GlobalFinalColorGradientColorId, FinalColorGradientColor);
        Shader.SetGlobalFloat(GlobalFinalColorGradientMinYId, FinalColorGradientMinY);
        Shader.SetGlobalFloat(GlobalFinalColorGradientMaxYId, FinalColorGradientMaxY);
        Shader.SetGlobalFloat(GlobalHighQualityShadowToggleId, enableHighQualityShadow ? 1f : 0f);
        Shader.SetGlobalFloat(
            GlobalPerObjectShadowDirectionModeId,
            enableHighQualityShadow
                ? (perObjectShadowDirection.sqrMagnitude >= MinDirectionMagnitude ? 3f : 1f)
                : 0f);
        Shader.SetGlobalVector(
            GlobalPerObjectShadowDirectionId,
            new Vector4(perObjectShadowDirection.x, perObjectShadowDirection.y, perObjectShadowDirection.z, 0f));
        Shader.SetGlobalFloat(GlobalPerObjectShadowPassStrengthOverrideId, 1f);
        Shader.SetGlobalFloat(GlobalSelfShadowStrengthId, SelfShadowStrength);
        Shader.SetGlobalFloat(GlobalEnvironmentShadowStrengthId, EnvironmentShadowStrength);
        Shader.SetGlobalFloat(GlobalSpecularShadowStrengthOverrideId, 1f);
        Shader.SetGlobalFloat(GlobalSpecularShadowStrengthId, SpecularShadowStrength);
        Shader.SetGlobalFloat(
            GlobalOverrideSceneAmbientId,
            EnableGlobalIndirectLight && OverrideSceneAmbient ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalIndirectIntensityId, EnableGlobalIndirectLight ? GlobalIndirectIntensity : 1f);
        Shader.SetGlobalColor(GlobalIndirectTintColorId, EnableGlobalIndirectLight ? GlobalIndirectTintColor : DefaultIndirectTintColor);

        MonsterRenderMainLightOverrideMode monsterMode = MonsterMainLightOverrideMode;
        bool monsterFollowCharacterLight = monsterMode == MonsterRenderMainLightOverrideMode.FollowCharacter;
        CharacterRenderMainLightOverrideMode resolvedCharacterMode = MainLightOverrideMode;
        bool monsterLightEnabled = monsterFollowCharacterLight
            ? resolvedCharacterMode != CharacterRenderMainLightOverrideMode.Off
            : monsterMode != MonsterRenderMainLightOverrideMode.Off;
        Vector3 monsterDirection = monsterFollowCharacterLight
            ? direction
            : monsterMode == MonsterRenderMainLightOverrideMode.FollowCamera
                ? GetMonsterVirtualLightCameraDirection(currentCamera)
                : MonsterCustomLightDirection;
        Color monsterLightColor = monsterFollowCharacterLight ? CustomLightColor : MonsterCustomLightColor;
        float monsterLightStrength = monsterFollowCharacterLight ? CustomLightStrength : MonsterCustomLightStrength;
        Shader.SetGlobalVector(GlobalMonsterLightDirectionId, new Vector4(monsterDirection.x, monsterDirection.y, monsterDirection.z, 0f));
        Shader.SetGlobalColor(GlobalMonsterLightColorId, monsterLightColor);
        Shader.SetGlobalFloat(GlobalMonsterLightStrengthId, monsterLightStrength);
        Shader.SetGlobalFloat(GlobalMonsterLightToggleId, monsterLightEnabled ? 1f : 0f);
        Shader.SetGlobalFloat(
            GlobalMonsterLightFollowCameraXZId,
            (monsterFollowCharacterLight
                ? resolvedCharacterMode == CharacterRenderMainLightOverrideMode.FollowCamera
                : monsterMode == MonsterRenderMainLightOverrideMode.FollowCamera) ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalMonsterAdditionalLightsEnabledId, MonsterAdditionalLightsEnabled ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalMonsterAdditionalLightsDiffuseEnabledId, MonsterAdditionalLightDiffuseEnabled ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalMonsterAdditionalLightsSpecularEnabledId, MonsterAdditionalLightSpecularEnabled ? 1f : 0f);

        bool monsterEnvironmentFollowCharacter = MonsterEnvironmentFollowCharacter;
        bool monsterIndirectEnabled = monsterEnvironmentFollowCharacter ? EnableGlobalIndirectLight : MonsterEnableGlobalIndirectLight;
        bool monsterOverrideAmbient = monsterEnvironmentFollowCharacter ? OverrideSceneAmbient : MonsterOverrideSceneAmbient;
        float monsterIndirectIntensity = monsterEnvironmentFollowCharacter ? GlobalIndirectIntensity : MonsterGlobalIndirectIntensity;
        Color monsterIndirectTint = monsterEnvironmentFollowCharacter ? GlobalIndirectTintColor : MonsterGlobalIndirectTintColor;
        Shader.SetGlobalFloat(GlobalMonsterOverrideSceneAmbientId, monsterIndirectEnabled && monsterOverrideAmbient ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalMonsterIndirectIntensityId, monsterIndirectEnabled ? monsterIndirectIntensity : 1f);
        Shader.SetGlobalColor(GlobalMonsterIndirectTintColorId, monsterIndirectEnabled ? monsterIndirectTint : DefaultIndirectTintColor);

        bool monsterOtherFollowCharacter = MonsterOtherFeaturesFollowCharacter;
        Shader.SetGlobalFloat(GlobalMonsterOutlineToggleId, (monsterOtherFollowCharacter ? EnableOutline : MonsterEnableOutline) ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalMonsterRimLightToggleId, (monsterOtherFollowCharacter ? EnableRimLight : MonsterEnableRimLight) ? 1f : 0f);
        bool monsterFinalGradientEnabled = monsterOtherFollowCharacter ? EnableFinalColorGradient : MonsterEnableFinalColorGradient;
        Shader.SetGlobalFloat(GlobalMonsterFinalColorGradientToggleId, monsterFinalGradientEnabled ? 1f : 0f);
        Shader.SetGlobalFloat(GlobalMonsterFinalColorGradientParametersOverrideId, 1f);
        Shader.SetGlobalColor(GlobalMonsterFinalColorGradientColorId, monsterOtherFollowCharacter ? FinalColorGradientColor : MonsterFinalColorGradientColor);
        Shader.SetGlobalFloat(GlobalMonsterFinalColorGradientMinYId, monsterOtherFollowCharacter ? FinalColorGradientMinY : MonsterFinalColorGradientMinY);
        Shader.SetGlobalFloat(GlobalMonsterFinalColorGradientMaxYId, monsterOtherFollowCharacter ? FinalColorGradientMaxY : MonsterFinalColorGradientMaxY);
    }

    [ContextMenu("关闭角色自定义灯光")]
    public void DisableNow()
    {
        SetCustomLightEnabled(false);
        ApplyNow();
    }

    public Vector3 GetWorldDirection()
    {
        if (_profile != null)
        {
            return _profile.GetWorldDirection();
        }

        _customLightDirection = NormalizeDirectionOrZero(_customLightDirection);
        return _customLightDirection;
    }

    public Vector3 GetPlanarShadowDirection()
    {
        if (_profile != null)
        {
            return _profile.GetPlanarShadowDirection();
        }

        _planarShadowDirection = NormalizeDirectionOrZero(_planarShadowDirection);
        return _planarShadowDirection;
    }

    public Vector3 GetHighQualityShadowDirection()
    {
        return GetHighQualityShadowDirection(Camera.main);
    }

    private Vector3 GetHighQualityShadowDirection(Camera currentCamera)
    {
        if (EnablePerObjectShadowDirectionFollowMainLightOverride
            && MainLightOverrideMode != CharacterRenderMainLightOverrideMode.Off)
        {
            return GetMainLightAdjustmentShadowDirection(currentCamera);
        }

        if (_profile != null)
        {
            return _profile.GetHighQualityShadowDirection();
        }

        _perObjectShadowDirection = NormalizeDirectionOrZero(_perObjectShadowDirection);
        _monsterCustomLightDirection = NormalizeDirectionOrZero(_monsterCustomLightDirection);
        return _perObjectShadowDirection;
    }

    public void SetWorldDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            if (_profile != null)
            {
                _profile.SetWorldDirection(Vector3.zero);
                ApplyNow();
                return;
            }

            _customLightDirection = Vector3.zero;
            SyncAnchorAnglesFromDirection();
            ApplyNow();
            return;
        }

        if (_profile != null)
        {
            _profile.SetWorldDirection(worldDirection);
            ApplyNow();
            return;
        }

        _customLightDirection = worldDirection.normalized;
        SyncAnchorAnglesFromDirection();
        ApplyNow();
    }

    public void SetPlanarShadowEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetPlanarShadowEnabled(enabled);
            ApplyNow();
            return;
        }

        _enablePlanarShadow = enabled;
        _shadowToggleDefaultsInitialized = true;
        ApplyNow();
    }

    public void SetPlanarShadowDirectionFollowCustomLightEnabled(bool enabled)
    {
        SetPlanarShadowDirectionFollowMainLightOverrideEnabled(enabled);
    }

    public void SetPlanarShadowDirectionFollowMainLightOverrideEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetPlanarShadowDirectionFollowMainLightOverrideEnabled(enabled);
            ApplyNow();
            return;
        }

        _enablePlanarShadowDirectionFollowCustomLight = enabled;
        ApplyNow();
    }

    public void SetPlanarShadowDirection(Vector3 worldDirection)
    {
        if (_profile != null)
        {
            _profile.SetPlanarShadowDirection(worldDirection);
            ApplyNow();
            return;
        }

        _planarShadowDirection = NormalizeDirectionOrZero(worldDirection);
        ApplyNow();
    }

    public void SetPlanarShadowStrength(float strength)
    {
        if (_profile != null)
        {
            _profile.SetPlanarShadowStrength(strength);
            ApplyNow();
            return;
        }

        _planarShadowStrength = Mathf.Max(0f, strength);
        _shadowDirectionDefaultsInitialized = true;
        ApplyNow();
    }

    public bool EnableRimLight
    {
        get
        {
            if (_profile != null)
            {
                return _profile.EnableRimLight;
            }

            EnsureHierarchyFeatureDefaultsInitialized();
            return _enableRimLight;
        }
    }

    public bool EnableRimCustomDirection => _profile != null ? _profile.EnableRimCustomDirection : _enableRimCustomDirection;
    public CharacterRenderRimDirectionSpace RimDirectionSpace => _profile != null ? _profile.RimDirectionSpace : _rimDirectionSpace;
    public Vector3 RimDirection => _profile != null ? _profile.RimDirection : NormalizeDirectionOrZero(_rimDirection);
    public bool EnableRimFakePointMask => _profile != null ? _profile.EnableRimFakePointMask : _enableRimFakePointMask;
    public Vector3 RimFakePointMaskPosition => _profile != null ? _profile.RimFakePointMaskPosition : _rimFakePointMaskPosition;
    public float RimFakePointMaskRange => _profile != null ? _profile.RimFakePointMaskRange : Mathf.Max(0.01f, _rimFakePointMaskRange);
    public float RimFakePointMaskPower => _profile != null ? _profile.RimFakePointMaskPower : Mathf.Clamp(_rimFakePointMaskPower, 0.25f, 8f);
    public bool EnableRimFakeDirectMask => _profile != null ? _profile.EnableRimFakeDirectMask : _enableRimFakeDirectMask;
    public Vector3 RimFakeDirectMaskDirection => _profile != null ? _profile.RimFakeDirectMaskDirection : NormalizeDirectionOrZero(_rimFakeDirectMaskDirection);
    public Vector3 RimFakeDirectMaskPosition => _profile != null ? _profile.RimFakeDirectMaskPosition : _rimFakeDirectMaskPosition;
    public float RimFakeDirectMaskRange => _profile != null ? _profile.RimFakeDirectMaskRange : Mathf.Max(0.01f, _rimFakeDirectMaskRange);
    public float RimIntensity => _profile != null ? _profile.RimIntensity : Mathf.Max(0f, _rimIntensity);

    public bool EnableFinalColorGradient
    {
        get
        {
            if (_profile != null)
            {
                return _profile.EnableFinalColorGradient;
            }

            EnsureHierarchyFeatureDefaultsInitialized();
            return _enableFinalColorGradient;
        }
    }

    public Color FinalColorGradientColor =>
        _profile != null ? _profile.FinalColorGradientColor : _finalColorGradientColor;

    public float FinalColorGradientMinY =>
        _profile != null ? _profile.FinalColorGradientMinY : _finalColorGradientMinY;

    public float FinalColorGradientMaxY =>
        _profile != null ? _profile.FinalColorGradientMaxY : _finalColorGradientMaxY;

    public void SetPlanarShadowFalloff(float falloff)
    {
        if (_profile != null)
        {
            _profile.SetPlanarShadowFalloff(falloff);
        }
        else
        {
            _planarShadowFalloff = Mathf.Max(0f, falloff);
        }

        ApplyNow();
    }

    public void SetPlanarShadowRange(float range)
    {
        if (_profile != null)
        {
            _profile.SetPlanarShadowRange(range);
        }
        else
        {
            _planarShadowRange = range;
        }

        ApplyNow();
    }

    public void SetPlanarShadowPlaneHeight(float planeHeight)
    {
        if (_profile != null)
        {
            _profile.SetPlanarShadowPlaneHeight(planeHeight);
        }
        else
        {
            _planarShadowPlaneHeight = planeHeight;
        }

        ApplyNow();
    }

    public void SetPlanarShadowGlobalCenter(Vector3 globalCenter)
    {
        if (_profile != null)
        {
            _profile.SetPlanarShadowGlobalCenter(globalCenter);
        }
        else
        {
            _planarShadowGlobalCenter = globalCenter;
        }

        ApplyNow();
    }

    public void SetPlanarShadowColor(Color color)
    {
        if (_profile != null)
        {
            _profile.SetPlanarShadowColor(color);
        }
        else
        {
            _planarShadowColor = color;
        }

        ApplyNow();
    }

    public void SetHighQualityShadowEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetHighQualityShadowEnabled(enabled);
            ApplyNow();
            return;
        }

        _enableHighQualityShadow = enabled;
        _shadowToggleDefaultsInitialized = true;
        ApplyNow();
    }

    public void SetSceneShadowMode(CharacterRenderSceneShadowMode mode)
    {
        if (_profile != null)
        {
            _profile.SetSceneShadowMode(mode);
        }
        else
        {
            _sceneShadowMode = mode;
            _hierarchyFeatureDefaultsInitialized = true;
            _hierarchyFeatureVersion = 1;
        }

        ApplyNow();
    }

    public void SetOutlineEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetOutlineEnabled(enabled);
            ApplyNow();
            return;
        }

        _enableOutline = enabled;
        _outlineToggleDefaultInitialized = true;
        ApplyNow();
    }

    public void SetRimLightEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetRimLightEnabled(enabled);
        }
        else
        {
            _enableRimLight = enabled;
            _hierarchyFeatureDefaultsInitialized = true;
            _hierarchyFeatureVersion = 1;
        }

        ApplyNow();
    }

    public void SetRimCustomDirectionEnabled(bool enabled)
    {
        if (_profile != null) _profile.SetRimCustomDirectionEnabled(enabled); else _enableRimCustomDirection = enabled;
        ApplyNow();
    }

    public void SetRimDirectionSpace(CharacterRenderRimDirectionSpace space)
    {
        if (_profile != null) _profile.SetRimDirectionSpace(space); else _rimDirectionSpace = space;
        ApplyNow();
    }

    public void SetRimDirection(Vector3 direction)
    {
        if (_profile != null) _profile.SetRimDirection(direction); else _rimDirection = NormalizeDirectionOrZero(direction);
        ApplyNow();
    }

    public void SetRimFakePointMaskEnabled(bool enabled)
    {
        if (_profile != null) _profile.SetRimFakePointMaskEnabled(enabled); else _enableRimFakePointMask = enabled;
        ApplyNow();
    }

    public void SetRimFakePointMaskPosition(Vector3 position)
    {
        if (_profile != null) _profile.SetRimFakePointMaskPosition(position); else _rimFakePointMaskPosition = position;
        ApplyNow();
    }

    public void SetRimFakePointMaskRange(float value)
    {
        if (_profile != null) _profile.SetRimFakePointMaskRange(value); else _rimFakePointMaskRange = Mathf.Max(0.01f, value);
        ApplyNow();
    }

    public void SetRimFakePointMaskPower(float value)
    {
        if (_profile != null) _profile.SetRimFakePointMaskPower(value); else _rimFakePointMaskPower = Mathf.Clamp(value, 0.25f, 8f);
        ApplyNow();
    }

    public void SetRimFakeDirectMaskEnabled(bool enabled)
    {
        if (_profile != null) _profile.SetRimFakeDirectMaskEnabled(enabled); else _enableRimFakeDirectMask = enabled;
        ApplyNow();
    }

    public void SetRimFakeDirectMaskDirection(Vector3 direction)
    {
        if (_profile != null) _profile.SetRimFakeDirectMaskDirection(direction); else _rimFakeDirectMaskDirection = NormalizeDirectionOrZero(direction);
        ApplyNow();
    }

    public void SetRimFakeDirectMaskPosition(Vector3 position)
    {
        if (_profile != null) _profile.SetRimFakeDirectMaskPosition(position); else _rimFakeDirectMaskPosition = position;
        ApplyNow();
    }

    public void SetRimFakeDirectMaskRange(float value)
    {
        if (_profile != null) _profile.SetRimFakeDirectMaskRange(value); else _rimFakeDirectMaskRange = Mathf.Max(0.01f, value);
        ApplyNow();
    }

    public void SetRimIntensity(float value)
    {
        if (_profile != null) _profile.SetRimIntensity(value); else _rimIntensity = Mathf.Max(0f, value);
        ApplyNow();
    }

    public void SetFinalColorGradientEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetFinalColorGradientEnabled(enabled);
        }
        else
        {
            _enableFinalColorGradient = enabled;
            _hierarchyFeatureDefaultsInitialized = true;
            _hierarchyFeatureVersion = 1;
        }

        ApplyNow();
    }

    public void SetFinalColorGradientColor(Color color)
    {
        if (_profile != null)
        {
            _profile.SetFinalColorGradientColor(color);
        }
        else
        {
            _finalColorGradientColor = color;
        }

        ApplyNow();
    }

    public void SetFinalColorGradientMinY(float value)
    {
        if (_profile != null)
        {
            _profile.SetFinalColorGradientMinY(value);
        }
        else
        {
            _finalColorGradientMinY = value;
        }

        ApplyNow();
    }

    public void SetFinalColorGradientMaxY(float value)
    {
        if (_profile != null)
        {
            _profile.SetFinalColorGradientMaxY(value);
        }
        else
        {
            _finalColorGradientMaxY = value;
        }

        ApplyNow();
    }

    public void SetLightAnchorAngles(float orbit, float elevation)
    {
        orbit = NormalizeAngle(orbit);
        elevation = NormalizeAngle(elevation);
        Vector3 direction = DirectionFromAnchorAngles(orbit, elevation);

        if (_profile != null)
        {
            _profile.SetWorldDirection(direction);
            ApplyNow();
            return;
        }

        _lightAnchorOrbit = orbit;
        _lightAnchorElevation = elevation;
        _customLightDirection = direction;
        SanitizeDirections();
        ApplyNow();
    }

    public void SetCustomLightEnabled(bool enabled)
    {
        SetMainLightOverrideMode(enabled
            ? CharacterRenderMainLightOverrideMode.Custom
            : CharacterRenderMainLightOverrideMode.Off);
    }

    public void SetMainLightOverrideMode(CharacterRenderMainLightOverrideMode mode)
    {
        if (_profile != null)
        {
            _profile.SetMainLightOverrideMode(mode);
            ApplyNow();
            return;
        }

        _mainLightOverrideMode = mode;
        _enableCustomLight = mode != CharacterRenderMainLightOverrideMode.Off;
        _mainLightAdjustmentMode = mode == CharacterRenderMainLightOverrideMode.FollowCamera
            ? CharacterRenderMainLightAdjustmentMode.FollowCamera
            : CharacterRenderMainLightAdjustmentMode.Off;
        _directLightHierarchyMigrated = true;
        _directLightHierarchyVersion = 1;
        ApplyNow();
    }

    public void SetBackLightEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetBackLightEnabled(enabled);
        }
        else
        {
            _enableBackLight = enabled;
            _directLightHierarchyMigrated = true;
            _directLightHierarchyVersion = 1;
        }

        ApplyNow();
    }

    public void SetCharacterAdditionalLightsEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetCharacterAdditionalLightsEnabled(enabled);
            ApplyNow();
            return;
        }

        _enableCharacterAdditionalLights = enabled;
        _additionalLightsToggleDefaultInitialized = true;
        ApplyNow();
    }

    public void SetCharacterAdditionalLightDiffuseEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetCharacterAdditionalLightDiffuseEnabled(enabled);
            ApplyNow();
            return;
        }

        _enableCharacterAdditionalLightDiffuse = enabled;
        _additionalLightsLobeToggleVersion = 1;
        ApplyNow();
    }

    public void SetCharacterAdditionalLightSpecularEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetCharacterAdditionalLightSpecularEnabled(enabled);
            ApplyNow();
            return;
        }

        _enableCharacterAdditionalLightSpecular = enabled;
        _additionalLightsLobeToggleVersion = 1;
        ApplyNow();
    }

    public void SetCustomLightStrength(float strength)
    {
        if (_profile != null)
        {
            _profile.SetCustomLightStrength(strength);
            ApplyNow();
            return;
        }

        _customLightStrength = Mathf.Max(0f, strength);
        ApplyNow();
    }

    public void SetCustomLightColor(Color color)
    {
        if (_profile != null)
        {
            _profile.SetCustomLightColor(color);
            ApplyNow();
            return;
        }

        _customLightColor = color;
        ApplyNow();
    }

    public void SetMainLightAdjustmentMode(CharacterRenderMainLightAdjustmentMode mode)
    {
        SetMainLightOverrideMode(mode == CharacterRenderMainLightAdjustmentMode.FollowCamera
            ? CharacterRenderMainLightOverrideMode.FollowCamera
            : CharacterRenderMainLightOverrideMode.Off);
        if (mode == CharacterRenderMainLightAdjustmentMode.BackLight)
        {
            SetBackLightEnabled(true);
        }
    }

    public void SetVirtualLightCameraDirectionOffsetEuler(Vector3 offsetEuler)
    {
        if (_profile != null)
        {
            _profile.SetVirtualLightCameraDirectionOffsetEuler(offsetEuler);
            ApplyNow();
            return;
        }

        _virtualLightCameraDirectionOffsetEuler = offsetEuler;
        ApplyNow();
    }

    public void SetVirtualLightCameraDirection(Vector3 worldDirection, Camera currentCamera)
    {
        if (currentCamera == null || worldDirection.sqrMagnitude < MinDirectionMagnitude)
        {
            return;
        }

        Vector3 localDirection = Quaternion.Inverse(currentCamera.transform.rotation) * worldDirection.normalized;
        Quaternion localOffset = Quaternion.FromToRotation(Vector3.back, localDirection);
        Vector3 offsetEuler = localOffset.eulerAngles;
        SetVirtualLightCameraDirectionOffsetEuler(new Vector3(
            NormalizeAngle(offsetEuler.x),
            NormalizeAngle(offsetEuler.y),
            NormalizeAngle(offsetEuler.z)));
    }

    public void SetVirtualLightColor(Color color)
    {
        if (_profile != null)
        {
            _profile.SetVirtualLightColor(color);
        }
        else
        {
            _virtualLightColor = color;
        }

        ApplyNow();
    }

    public void SetVirtualLightIntensity(float value)
    {
        if (_profile != null)
        {
            _profile.SetVirtualLightIntensity(value);
        }
        else
        {
            _virtualLightIntensity = Mathf.Max(0f, value);
        }

        ApplyNow();
    }

    public void SetVirtualLightOffset(float value)
    {
        if (_profile != null)
        {
            _profile.SetVirtualLightOffset(value);
        }
        else
        {
            _virtualLightOffset = Mathf.Clamp(value, -1f, 1f);
        }

        ApplyNow();
    }

    public void SetPerObjectShadowDirectionFollowCustomLightEnabled(bool enabled)
    {
        SetPerObjectShadowDirectionFollowMainLightOverrideEnabled(enabled);
    }

    public void SetPerObjectShadowDirectionFollowMainLightOverrideEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetPerObjectShadowDirectionFollowMainLightOverrideEnabled(enabled);
            ApplyNow();
            return;
        }

        _enablePerObjectShadowDirectionFollowMainLightAdjustment = enabled;
        _enablePerObjectShadowDirectionFollowCustomLight = false;
        ApplyNow();
    }

    public void SetPerObjectShadowDirectionFollowMainLightAdjustmentEnabled(bool enabled)
    {
        SetPerObjectShadowDirectionFollowMainLightOverrideEnabled(enabled);
    }

    public void SetHighQualityShadowDirection(Vector3 worldDirection)
    {
        if (_profile != null)
        {
            _profile.SetHighQualityShadowDirection(worldDirection);
            ApplyNow();
            return;
        }

        _perObjectShadowDirection = NormalizeDirectionOrZero(worldDirection);
        ApplyNow();
    }

    public void SetSelfShadowStrength(float strength)
    {
        if (_profile != null)
        {
            _profile.SetSelfShadowStrength(strength);
            ApplyNow();
            return;
        }

        _selfShadowStrength = Mathf.Clamp(strength, 0f, MaxPerObjectShadowStrength);
        ApplyNow();
    }

    public void SetEnvironmentShadowStrength(float strength)
    {
        if (_profile != null)
        {
            _profile.SetEnvironmentShadowStrength(strength);
            ApplyNow();
            return;
        }

        _environmentShadowStrength = Mathf.Clamp(strength, 0f, MaxPerObjectShadowStrength);
        ApplyNow();
    }

    public void SetSpecularShadowStrength(float strength)
    {
        if (_profile != null)
        {
            _profile.SetSpecularShadowStrength(strength);
            ApplyNow();
            return;
        }

        _specularShadowStrength = Mathf.Clamp01(strength);
        ApplyNow();
    }

    public void SetGlobalIndirectIntensity(float intensity)
    {
        if (_profile != null)
        {
            _profile.SetGlobalIndirectIntensity(intensity);
            ApplyNow();
            return;
        }

        _globalIndirectIntensity = Mathf.Max(0f, intensity);
        ApplyNow();
    }

    public void SetGlobalIndirectLightEnabled(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetGlobalIndirectLightEnabled(enabled);
            ApplyNow();
            return;
        }

        _enableGlobalIndirectLight = enabled;
        ApplyNow();
    }

    public void SetOverrideSceneAmbient(bool enabled)
    {
        if (_profile != null)
        {
            _profile.SetOverrideSceneAmbient(enabled);
            ApplyNow();
            return;
        }

        _overrideSceneAmbient = enabled;
        ApplyNow();
    }

    public void SetGlobalIndirectTintColor(Color tintColor)
    {
        if (_profile != null)
        {
            _profile.SetGlobalIndirectTintColor(tintColor);
            ApplyNow();
            return;
        }

        _globalIndirectTintColor = tintColor;
        ApplyNow();
    }

    public void SetMonsterMainLightOverrideMode(MonsterRenderMainLightOverrideMode mode)
    {
        if (_profile != null) _profile.SetMonsterMainLightOverrideMode(mode); else _monsterMainLightOverrideMode = mode;
        ApplyNow();
    }
    public void SetMonsterCustomLightColor(Color color)
    {
        if (_profile != null) _profile.SetMonsterCustomLightColor(color); else _monsterCustomLightColor = color;
        ApplyNow();
    }
    public void SetMonsterCustomLightStrength(float strength)
    {
        if (_profile != null) _profile.SetMonsterCustomLightStrength(strength); else _monsterCustomLightStrength = Mathf.Max(0f, strength);
        ApplyNow();
    }
    public void SetMonsterCustomLightDirection(Vector3 direction)
    {
        if (_profile != null) _profile.SetMonsterCustomLightDirection(direction); else _monsterCustomLightDirection = NormalizeDirectionOrZero(direction);
        ApplyNow();
    }
    public void SetMonsterVirtualLightCameraDirectionOffsetEuler(Vector3 offsetEuler)
    {
        if (_profile != null) _profile.SetMonsterVirtualLightCameraDirectionOffsetEuler(offsetEuler); else _monsterVirtualLightCameraDirectionOffsetEuler = offsetEuler;
        ApplyNow();
    }
    public void SetMonsterAdditionalLightsEnabled(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterAdditionalLightsEnabled(enabled); else _monsterAdditionalLightsEnabled = enabled;
        ApplyNow();
    }
    public void SetMonsterAdditionalLightDiffuseEnabled(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterAdditionalLightDiffuseEnabled(enabled); else _monsterAdditionalLightDiffuseEnabled = enabled;
        ApplyNow();
    }
    public void SetMonsterAdditionalLightSpecularEnabled(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterAdditionalLightSpecularEnabled(enabled); else _monsterAdditionalLightSpecularEnabled = enabled;
        ApplyNow();
    }
    public void SetMonsterEnvironmentFollowCharacter(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterEnvironmentFollowCharacter(enabled); else _monsterEnvironmentFollowCharacter = enabled;
        ApplyNow();
    }
    public void SetMonsterEnableGlobalIndirectLight(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterEnableGlobalIndirectLight(enabled); else _monsterEnableGlobalIndirectLight = enabled;
        ApplyNow();
    }
    public void SetMonsterOverrideSceneAmbient(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterOverrideSceneAmbient(enabled); else _monsterOverrideSceneAmbient = enabled;
        ApplyNow();
    }
    public void SetMonsterGlobalIndirectIntensity(float value)
    {
        if (_profile != null) _profile.SetMonsterGlobalIndirectIntensity(value); else _monsterGlobalIndirectIntensity = Mathf.Max(0f, value);
        ApplyNow();
    }
    public void SetMonsterGlobalIndirectTintColor(Color color)
    {
        if (_profile != null) _profile.SetMonsterGlobalIndirectTintColor(color); else _monsterGlobalIndirectTintColor = color;
        ApplyNow();
    }
    public void SetMonsterOtherFeaturesFollowCharacter(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterOtherFeaturesFollowCharacter(enabled); else _monsterOtherFeaturesFollowCharacter = enabled;
        ApplyNow();
    }
    public void SetMonsterOutlineEnabled(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterOutlineEnabled(enabled); else _monsterEnableOutline = enabled;
        ApplyNow();
    }
    public void SetMonsterRimLightEnabled(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterRimLightEnabled(enabled); else _monsterEnableRimLight = enabled;
        ApplyNow();
    }
    public void SetMonsterFinalColorGradientEnabled(bool enabled)
    {
        if (_profile != null) _profile.SetMonsterFinalColorGradientEnabled(enabled); else _monsterEnableFinalColorGradient = enabled;
        ApplyNow();
    }
    public void SetMonsterFinalColorGradientColor(Color color)
    {
        if (_profile != null) _profile.SetMonsterFinalColorGradientColor(color); else _monsterFinalColorGradientColor = color;
        ApplyNow();
    }
    public void SetMonsterFinalColorGradientMinY(float value)
    {
        if (_profile != null) _profile.SetMonsterFinalColorGradientMinY(value); else _monsterFinalColorGradientMinY = value;
        ApplyNow();
    }
    public void SetMonsterFinalColorGradientMaxY(float value)
    {
        if (_profile != null) _profile.SetMonsterFinalColorGradientMaxY(value); else _monsterFinalColorGradientMaxY = value;
        ApplyNow();
    }

    public void GetLightAnchorAngles(out float orbit, out float elevation)
    {
        GetDirectionAnchorAngles(GetWorldDirection(), out orbit, out elevation);
    }

    public void GetPlanarShadowAnchorAngles(out float orbit, out float elevation)
    {
        GetDirectionAnchorAngles(GetPlanarShadowDirection(), out orbit, out elevation);
    }

    public void SetPlanarShadowAnchorAngles(float orbit, float elevation)
    {
        SetPlanarShadowDirection(DirectionFromAnchorAngles(orbit, elevation));
    }

    public void GetHighQualityShadowAnchorAngles(out float orbit, out float elevation)
    {
        GetDirectionAnchorAngles(GetHighQualityShadowDirection(), out orbit, out elevation);
    }

    public void SetHighQualityShadowAnchorAngles(float orbit, float elevation)
    {
        SetHighQualityShadowDirection(DirectionFromAnchorAngles(orbit, elevation));
    }

    public void CopyResolvedSettingsToProfile(CharacterRenderLightProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        profile.SetMainLightOverrideMode(MainLightOverrideMode);
        profile.SetCustomLightColor(CustomLightColor);
        profile.SetCustomLightStrength(CustomLightStrength);
        profile.SetWorldDirection(GetWorldDirection());
        profile.SetVirtualLightCameraDirectionOffsetEuler(VirtualLightCameraDirectionOffsetEuler);
        profile.SetBackLightEnabled(EnableBackLight);
        profile.SetVirtualLightColor(VirtualLightColor);
        profile.SetVirtualLightIntensity(VirtualLightIntensity);
        profile.SetVirtualLightOffset(VirtualLightOffset);
        profile.SetCharacterAdditionalLightsEnabled(EnableCharacterAdditionalLights);
        profile.SetCharacterAdditionalLightDiffuseEnabled(EnableCharacterAdditionalLightDiffuse);
        profile.SetCharacterAdditionalLightSpecularEnabled(EnableCharacterAdditionalLightSpecular);
        profile.SetPlanarShadowEnabled(EnablePlanarShadow);
        profile.SetPlanarShadowDirectionFollowMainLightOverrideEnabled(EnablePlanarShadowDirectionFollowMainLightOverride);
        profile.SetPlanarShadowDirection(GetPlanarShadowDirection());
        profile.SetPlanarShadowStrength(PlanarShadowStrength);
        profile.SetPlanarShadowFalloff(PlanarShadowFalloff);
        profile.SetPlanarShadowRange(PlanarShadowRange);
        profile.SetPlanarShadowPlaneHeight(PlanarShadowPlaneHeight);
        profile.SetPlanarShadowGlobalCenter(PlanarShadowGlobalCenter);
        profile.SetPlanarShadowColor(PlanarShadowColor);
        profile.SetSceneShadowMode(SceneShadowMode);
        profile.SetOutlineEnabled(EnableOutline);
        profile.SetRimLightEnabled(EnableRimLight);
        profile.SetRimCustomDirectionEnabled(EnableRimCustomDirection);
        profile.SetRimDirectionSpace(RimDirectionSpace);
        profile.SetRimDirection(RimDirection);
        profile.SetRimFakePointMaskEnabled(EnableRimFakePointMask);
        profile.SetRimFakePointMaskPosition(RimFakePointMaskPosition);
        profile.SetRimFakePointMaskRange(RimFakePointMaskRange);
        profile.SetRimFakePointMaskPower(RimFakePointMaskPower);
        profile.SetRimFakeDirectMaskEnabled(EnableRimFakeDirectMask);
        profile.SetRimFakeDirectMaskDirection(RimFakeDirectMaskDirection);
        profile.SetRimFakeDirectMaskPosition(RimFakeDirectMaskPosition);
        profile.SetRimFakeDirectMaskRange(RimFakeDirectMaskRange);
        profile.SetRimIntensity(RimIntensity);
        profile.SetFinalColorGradientEnabled(EnableFinalColorGradient);
        profile.SetFinalColorGradientColor(FinalColorGradientColor);
        profile.SetFinalColorGradientMinY(FinalColorGradientMinY);
        profile.SetFinalColorGradientMaxY(FinalColorGradientMaxY);
        profile.SetHighQualityShadowEnabled(EnableHighQualityShadow);
        profile.SetPerObjectShadowDirectionFollowMainLightOverrideEnabled(EnablePerObjectShadowDirectionFollowMainLightOverride);
        profile.SetHighQualityShadowDirection(GetHighQualityShadowDirection());
        profile.SetSelfShadowStrength(SelfShadowStrength);
        profile.SetEnvironmentShadowStrength(EnvironmentShadowStrength);
        profile.SetSpecularShadowStrength(SpecularShadowStrength);
        profile.SetGlobalIndirectLightEnabled(EnableGlobalIndirectLight);
        profile.SetOverrideSceneAmbient(OverrideSceneAmbient);
        profile.SetGlobalIndirectIntensity(GlobalIndirectIntensity);
        profile.SetGlobalIndirectTintColor(GlobalIndirectTintColor);
        profile.SetMonsterMainLightOverrideMode(MonsterMainLightOverrideMode);
        profile.SetMonsterCustomLightColor(MonsterCustomLightColor);
        profile.SetMonsterCustomLightStrength(MonsterCustomLightStrength);
        profile.SetMonsterCustomLightDirection(MonsterCustomLightDirection);
        profile.SetMonsterVirtualLightCameraDirectionOffsetEuler(MonsterVirtualLightCameraDirectionOffsetEuler);
        profile.SetMonsterAdditionalLightsEnabled(MonsterAdditionalLightsEnabled);
        profile.SetMonsterAdditionalLightDiffuseEnabled(MonsterAdditionalLightDiffuseEnabled);
        profile.SetMonsterAdditionalLightSpecularEnabled(MonsterAdditionalLightSpecularEnabled);
        profile.SetMonsterEnvironmentFollowCharacter(MonsterEnvironmentFollowCharacter);
        profile.SetMonsterEnableGlobalIndirectLight(MonsterEnableGlobalIndirectLight);
        profile.SetMonsterOverrideSceneAmbient(MonsterOverrideSceneAmbient);
        profile.SetMonsterGlobalIndirectIntensity(MonsterGlobalIndirectIntensity);
        profile.SetMonsterGlobalIndirectTintColor(MonsterGlobalIndirectTintColor);
        profile.SetMonsterOtherFeaturesFollowCharacter(MonsterOtherFeaturesFollowCharacter);
        profile.SetMonsterOutlineEnabled(MonsterEnableOutline);
        profile.SetMonsterRimLightEnabled(MonsterEnableRimLight);
        profile.SetMonsterFinalColorGradientEnabled(MonsterEnableFinalColorGradient);
        profile.SetMonsterFinalColorGradientColor(MonsterFinalColorGradientColor);
        profile.SetMonsterFinalColorGradientMinY(MonsterFinalColorGradientMinY);
        profile.SetMonsterFinalColorGradientMaxY(MonsterFinalColorGradientMaxY);
    }

    public void CopyProfileSettingsToLocal(CharacterRenderLightProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        _mainLightOverrideMode = profile.MainLightOverrideMode;
        _enableCustomLight = profile.EnableCustomLight;
        _mainLightAdjustmentMode = profile.MainLightAdjustmentMode;
        _directLightHierarchyMigrated = true;
        _directLightHierarchyVersion = 1;
        _customLightColor = profile.CustomLightColor;
        _customLightStrength = profile.CustomLightStrength;
        _customLightDirection = profile.GetWorldDirection();
        _virtualLightCameraDirectionOffsetEuler = profile.VirtualLightCameraDirectionOffsetEuler;
        _enableBackLight = profile.EnableBackLight;
        _virtualLightColor = profile.VirtualLightColor;
        _virtualLightIntensity = profile.VirtualLightIntensity;
        _virtualLightOffset = profile.VirtualLightOffset;
        _enableCharacterAdditionalLights = profile.EnableCharacterAdditionalLights;
        _enableCharacterAdditionalLightDiffuse = profile.EnableCharacterAdditionalLightDiffuse;
        _enableCharacterAdditionalLightSpecular = profile.EnableCharacterAdditionalLightSpecular;
        _enablePlanarShadow = profile.EnablePlanarShadow;
        _enablePlanarShadowDirectionFollowCustomLight = profile.EnablePlanarShadowDirectionFollowMainLightOverride;
        _planarShadowDirection = profile.GetPlanarShadowDirection();
        _planarShadowStrength = profile.PlanarShadowStrength;
        _planarShadowFalloff = profile.PlanarShadowFalloff;
        _planarShadowRange = profile.PlanarShadowRange;
        _planarShadowPlaneHeight = profile.PlanarShadowPlaneHeight;
        _planarShadowGlobalCenter = profile.PlanarShadowGlobalCenter;
        _planarShadowColor = profile.PlanarShadowColor;
        _sceneShadowMode = profile.SceneShadowMode;
        _enableOutline = profile.EnableOutline;
        _enableRimLight = profile.EnableRimLight;
        _enableRimCustomDirection = profile.EnableRimCustomDirection;
        _rimDirectionSpace = profile.RimDirectionSpace;
        _rimDirection = profile.RimDirection;
        _enableRimFakePointMask = profile.EnableRimFakePointMask;
        _rimFakePointMaskPosition = profile.RimFakePointMaskPosition;
        _rimFakePointMaskRange = profile.RimFakePointMaskRange;
        _rimFakePointMaskPower = profile.RimFakePointMaskPower;
        _enableRimFakeDirectMask = profile.EnableRimFakeDirectMask;
        _rimFakeDirectMaskDirection = profile.RimFakeDirectMaskDirection;
        _rimFakeDirectMaskPosition = profile.RimFakeDirectMaskPosition;
        _rimFakeDirectMaskRange = profile.RimFakeDirectMaskRange;
        _rimIntensity = profile.RimIntensity;
        _enableFinalColorGradient = profile.EnableFinalColorGradient;
        _finalColorGradientColor = profile.FinalColorGradientColor;
        _finalColorGradientMinY = profile.FinalColorGradientMinY;
        _finalColorGradientMaxY = profile.FinalColorGradientMaxY;
        _hierarchyFeatureDefaultsInitialized = true;
        _hierarchyFeatureVersion = 1;
        _enableHighQualityShadow = profile.EnableHighQualityShadow;
        _enablePerObjectShadowDirectionFollowCustomLight = false;
        _enablePerObjectShadowDirectionFollowMainLightAdjustment = profile.EnablePerObjectShadowDirectionFollowMainLightOverride;
        _perObjectShadowDirection = profile.GetHighQualityShadowDirection();
        _selfShadowStrength = profile.SelfShadowStrength;
        _environmentShadowStrength = profile.EnvironmentShadowStrength;
        _specularShadowStrength = profile.SpecularShadowStrength;
        _shadowToggleDefaultsInitialized = true;
        _shadowDirectionDefaultsInitialized = true;
        _outlineToggleDefaultInitialized = true;
        _additionalLightsToggleDefaultInitialized = true;
        _additionalLightsLobeToggleVersion = 1;
        _enableGlobalIndirectLight = profile.EnableGlobalIndirectLight;
        _overrideSceneAmbient = profile.OverrideSceneAmbient;
        _globalIndirectIntensity = profile.GlobalIndirectIntensity;
        _globalIndirectTintColor = profile.GlobalIndirectTintColor;
        _monsterMainLightOverrideMode = profile.MonsterMainLightOverrideMode;
        _monsterCustomLightColor = profile.MonsterCustomLightColor;
        _monsterCustomLightStrength = profile.MonsterCustomLightStrength;
        _monsterCustomLightDirection = profile.MonsterCustomLightDirection;
        _monsterVirtualLightCameraDirectionOffsetEuler = profile.MonsterVirtualLightCameraDirectionOffsetEuler;
        _monsterAdditionalLightsEnabled = profile.MonsterAdditionalLightsEnabled;
        _monsterAdditionalLightDiffuseEnabled = profile.MonsterAdditionalLightDiffuseEnabled;
        _monsterAdditionalLightSpecularEnabled = profile.MonsterAdditionalLightSpecularEnabled;
        _monsterEnvironmentFollowCharacter = profile.MonsterEnvironmentFollowCharacter;
        _monsterEnableGlobalIndirectLight = profile.MonsterEnableGlobalIndirectLight;
        _monsterOverrideSceneAmbient = profile.MonsterOverrideSceneAmbient;
        _monsterGlobalIndirectIntensity = profile.MonsterGlobalIndirectIntensity;
        _monsterGlobalIndirectTintColor = profile.MonsterGlobalIndirectTintColor;
        _monsterOtherFeaturesFollowCharacter = profile.MonsterOtherFeaturesFollowCharacter;
        _monsterEnableOutline = profile.MonsterEnableOutline;
        _monsterEnableRimLight = profile.MonsterEnableRimLight;
        _monsterEnableFinalColorGradient = profile.MonsterEnableFinalColorGradient;
        _monsterFinalColorGradientColor = profile.MonsterFinalColorGradientColor;
        _monsterFinalColorGradientMinY = profile.MonsterFinalColorGradientMinY;
        _monsterFinalColorGradientMaxY = profile.MonsterFinalColorGradientMaxY;
        SyncAnchorAnglesFromDirection();
    }

    private void SanitizeDirections()
    {
        _customLightDirection = NormalizeDirectionOrZero(_customLightDirection);
        _planarShadowDirection = NormalizeDirectionOrZero(_planarShadowDirection);
        _perObjectShadowDirection = NormalizeDirectionOrZero(_perObjectShadowDirection);
        _rimDirection = NormalizeDirectionOrZero(_rimDirection);
        _rimFakeDirectMaskDirection = NormalizeDirectionOrZero(_rimFakeDirectMaskDirection);
    }

    private Vector3 GetMainLightAdjustmentShadowDirection(Camera currentCamera)
    {
        if (MainLightOverrideMode == CharacterRenderMainLightOverrideMode.FollowCamera)
        {
            return GetVirtualLightCameraDirection(currentCamera);
        }

        return GetWorldDirection();
    }

    public Vector3 GetVirtualLightCameraDirection(Camera currentCamera)
    {
        if (currentCamera == null)
        {
            return Vector3.zero;
        }

        Quaternion localOffset = Quaternion.Euler(VirtualLightCameraDirectionOffsetEuler);
        return NormalizeDirectionOrZero(currentCamera.transform.rotation * localOffset * Vector3.back);
    }

    private Vector3 GetMonsterVirtualLightCameraDirection(Camera currentCamera)
    {
        if (currentCamera == null)
        {
            return Vector3.zero;
        }

        Quaternion localOffset = Quaternion.Euler(MonsterVirtualLightCameraDirectionOffsetEuler);
        return NormalizeDirectionOrZero(currentCamera.transform.rotation * localOffset * Vector3.back);
    }

    private static Vector3 GetVirtualLightMaskCameraDirection(Camera currentCamera)
    {
        return currentCamera == null
            ? Vector3.zero
            : NormalizeDirectionOrZero(currentCamera.transform.rotation * Vector3.back);
    }

    private void SyncAnchorAnglesFromDirection()
    {
        Vector3 direction = _customLightDirection.sqrMagnitude < MinDirectionMagnitude
            ? Vector3.zero
            : _customLightDirection.normalized;

        float horizontalMagnitude = new Vector2(direction.x, direction.z).magnitude;
        _lightAnchorOrbit = horizontalMagnitude < MinDirectionMagnitude
            ? 0f
            : NormalizeAngle(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        _lightAnchorElevation = NormalizeAngle(Mathf.Atan2(direction.y, horizontalMagnitude) * Mathf.Rad2Deg);
    }

    private static Vector3 DirectionFromAnchorAngles(float orbit, float elevation)
    {
        float orbitRadians = orbit * Mathf.Deg2Rad;
        float elevationRadians = elevation * Mathf.Deg2Rad;
        float cosElevation = Mathf.Cos(elevationRadians);

        Vector3 direction = new Vector3(
            Mathf.Sin(orbitRadians) * cosElevation,
            Mathf.Sin(elevationRadians),
            Mathf.Cos(orbitRadians) * cosElevation);

        if (direction.sqrMagnitude < MinDirectionMagnitude)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }

    private static void GetDirectionAnchorAngles(Vector3 direction, out float orbit, out float elevation)
    {
        direction = NormalizeDirectionOrZero(direction);
        float horizontalMagnitude = new Vector2(direction.x, direction.z).magnitude;
        orbit = horizontalMagnitude < MinDirectionMagnitude
            ? 0f
            : NormalizeAngle(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        elevation = NormalizeAngle(Mathf.Atan2(direction.y, horizontalMagnitude) * Mathf.Rad2Deg);
    }

    private static Vector3 NormalizeDirectionOrZero(Vector3 direction)
    {
        return direction.sqrMagnitude < MinDirectionMagnitude
            ? Vector3.zero
            : direction.normalized;
    }

    private void NormalizeAnchorAngles()
    {
        _lightAnchorOrbit = NormalizeAngle(_lightAnchorOrbit);
        _lightAnchorElevation = NormalizeAngle(_lightAnchorElevation);
    }

    private static float NormalizeAngle(float angle)
    {
        float offset = angle - DegreesStart;
        return offset - Mathf.Floor(offset / DegreesRange) * DegreesRange + DegreesStart;
    }

    private void ApplyReplacementOrClear()
    {
        CharacterRenderController replacement = FindReplacement(this);
        if (replacement != null)
        {
            replacement.ApplyNow();
            return;
        }

        ClearGlobalLight();
    }

    private static CharacterRenderController FindReplacement(CharacterRenderController current)
    {
        CharacterRenderController[] controllers = FindObjectsOfType<CharacterRenderController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            CharacterRenderController controller = controllers[i];
            if (controller == null || controller == current || !controller.isActiveAndEnabled)
            {
                continue;
            }

            return controller;
        }

        return null;
    }

    private static void ClearGlobalLight()
    {
        Shader.SetGlobalVector(GlobalLightDirectionId, Vector4.zero);
        Shader.SetGlobalColor(GlobalLightColorId, Color.white);
        Shader.SetGlobalFloat(GlobalLightStrengthId, 1f);
        Shader.SetGlobalFloat(GlobalLightToggleId, 0f);
        Shader.SetGlobalFloat(GlobalLightFollowCameraXZId, 0f);
        Shader.SetGlobalFloat(GlobalBackLightToggleId, 0f);
        Shader.SetGlobalVector(GlobalVirtualLightCameraDirectionId, Vector4.zero);
        Shader.SetGlobalVector(GlobalVirtualLightMaskCameraDirectionId, Vector4.zero);
        Shader.SetGlobalColor(GlobalVirtualLightColorId, Color.white);
        Shader.SetGlobalFloat(GlobalVirtualLightIntensityId, 0f);
        Shader.SetGlobalFloat(GlobalVirtualLightOffsetId, 0f);
        Shader.SetGlobalFloat(GlobalCharacterAdditionalLightsEnabledId, 1f);
        Shader.SetGlobalFloat(GlobalCharacterAdditionalLightsDiffuseEnabledId, 1f);
        Shader.SetGlobalFloat(GlobalCharacterAdditionalLightsSpecularEnabledId, 1f);
        Shader.SetGlobalFloat(GlobalPlanarShadowToggleId, 1f);
        Shader.SetGlobalFloat(GlobalPlanarShadowDirectionModeId, 0f);
        Shader.SetGlobalVector(GlobalPlanarShadowDirectionId, Vector4.zero);
        Shader.SetGlobalFloat(GlobalPlanarShadowStrengthId, 1f);
        Shader.SetGlobalFloat(GlobalPlanarShadowParametersOverrideId, 0f);
        Shader.SetGlobalFloat(GlobalPlanarShadowFalloffId, 1f);
        Shader.SetGlobalFloat(GlobalPlanarShadowRangeId, 0f);
        Shader.SetGlobalFloat(GlobalPlanarShadowPlaneHeightId, 0f);
        Shader.SetGlobalVector(GlobalPlanarShadowGlobalCenterId, Vector4.zero);
        Shader.SetGlobalColor(GlobalPlanarShadowColorId, DefaultPlanarShadowColor);
        Shader.SetGlobalFloat(GlobalOutlineToggleId, 1f);
        Shader.SetGlobalFloat(GlobalSceneShadowModeOverrideId, 0f);
        Shader.SetGlobalFloat(GlobalSceneShadowModeId, (float)CharacterRenderSceneShadowMode.UnityOnly);
        Shader.SetGlobalFloat(GlobalRimLightToggleId, 1f);
        Shader.SetGlobalFloat(GlobalRimCustomDirectionEnabledId, 0f);
        Shader.SetGlobalFloat(GlobalRimDirectionSpaceId, (float)CharacterRenderRimDirectionSpace.World);
        Shader.SetGlobalVector(GlobalRimDirectionId, Vector4.zero);
        Shader.SetGlobalFloat(GlobalRimFakePointMaskEnabledId, 0f);
        Shader.SetGlobalVector(GlobalRimFakePointMaskPositionId, Vector4.zero);
        Shader.SetGlobalFloat(GlobalRimFakePointMaskRangeId, 1f);
        Shader.SetGlobalFloat(GlobalRimFakePointMaskPowerId, 2f);
        Shader.SetGlobalFloat(GlobalRimFakeDirectMaskEnabledId, 0f);
        Shader.SetGlobalVector(GlobalRimFakeDirectMaskDirectionId, new Vector4(1f, 0f, 0f, 0f));
        Shader.SetGlobalVector(GlobalRimFakeDirectMaskPositionId, Vector4.zero);
        Shader.SetGlobalFloat(GlobalRimFakeDirectMaskRangeId, 1f);
        Shader.SetGlobalFloat(GlobalRimIntensityId, 1f);
        Shader.SetGlobalFloat(GlobalFinalColorGradientToggleId, 1f);
        Shader.SetGlobalFloat(GlobalFinalColorGradientParametersOverrideId, 0f);
        Shader.SetGlobalColor(GlobalFinalColorGradientColorId, Color.white);
        Shader.SetGlobalFloat(GlobalFinalColorGradientMinYId, 0f);
        Shader.SetGlobalFloat(GlobalFinalColorGradientMaxYId, 1f);
        Shader.SetGlobalFloat(GlobalHighQualityShadowToggleId, 1f);
        Shader.SetGlobalFloat(GlobalPerObjectShadowDirectionModeId, 0f);
        Shader.SetGlobalVector(GlobalPerObjectShadowDirectionId, Vector4.zero);
        Shader.SetGlobalFloat(GlobalPerObjectShadowPassStrengthOverrideId, 0f);
        Shader.SetGlobalFloat(GlobalSelfShadowStrengthId, 1f);
        Shader.SetGlobalFloat(GlobalEnvironmentShadowStrengthId, 1f);
        Shader.SetGlobalFloat(GlobalSpecularShadowStrengthOverrideId, 0f);
        Shader.SetGlobalFloat(GlobalSpecularShadowStrengthId, 1f);
        Shader.SetGlobalFloat(GlobalOverrideSceneAmbientId, 0f);
        Shader.SetGlobalFloat(GlobalIndirectIntensityId, 1f);
        Shader.SetGlobalColor(GlobalIndirectTintColorId, DefaultIndirectTintColor);
        Shader.SetGlobalVector(GlobalMonsterLightDirectionId, Vector4.zero);
        Shader.SetGlobalColor(GlobalMonsterLightColorId, Color.white);
        Shader.SetGlobalFloat(GlobalMonsterLightStrengthId, 1f);
        Shader.SetGlobalFloat(GlobalMonsterLightToggleId, 0f);
        Shader.SetGlobalFloat(GlobalMonsterLightFollowCameraXZId, 0f);
        Shader.SetGlobalFloat(GlobalMonsterAdditionalLightsEnabledId, 1f);
        Shader.SetGlobalFloat(GlobalMonsterAdditionalLightsDiffuseEnabledId, 1f);
        Shader.SetGlobalFloat(GlobalMonsterAdditionalLightsSpecularEnabledId, 1f);
        Shader.SetGlobalFloat(GlobalMonsterOverrideSceneAmbientId, 0f);
        Shader.SetGlobalFloat(GlobalMonsterIndirectIntensityId, 1f);
        Shader.SetGlobalColor(GlobalMonsterIndirectTintColorId, DefaultIndirectTintColor);
        Shader.SetGlobalFloat(GlobalMonsterOutlineToggleId, 1f);
        Shader.SetGlobalFloat(GlobalMonsterRimLightToggleId, 1f);
        Shader.SetGlobalFloat(GlobalMonsterFinalColorGradientToggleId, 1f);
        Shader.SetGlobalFloat(GlobalMonsterFinalColorGradientParametersOverrideId, 0f);
        Shader.SetGlobalColor(GlobalMonsterFinalColorGradientColorId, Color.white);
        Shader.SetGlobalFloat(GlobalMonsterFinalColorGradientMinYId, 0f);
        Shader.SetGlobalFloat(GlobalMonsterFinalColorGradientMaxYId, 1f);
        Shader.SetGlobalVector(GlobalGunLightDirectionId, Vector4.zero);
        Shader.SetGlobalColor(GlobalGunLightColorId, Color.white);
        Shader.SetGlobalFloat(GlobalGunLightStrengthId, 1f);
        Shader.SetGlobalFloat(GlobalGunLightToggleId, 0f);
        Shader.SetGlobalFloat(GlobalGunLightFollowCameraXZId, 0f);
        Shader.SetGlobalFloat(GlobalGunAdditionalLightsEnabledId, 1f);
        Shader.SetGlobalFloat(GlobalGunAdditionalLightsDiffuseEnabledId, 1f);
        Shader.SetGlobalFloat(GlobalGunAdditionalLightsSpecularEnabledId, 1f);
        Shader.SetGlobalFloat(GlobalGunOverrideSceneAmbientId, 0f);
        Shader.SetGlobalFloat(GlobalGunIndirectIntensityId, 1f);
        Shader.SetGlobalColor(GlobalGunIndirectTintColorId, DefaultIndirectTintColor);
        Shader.SetGlobalFloat(GlobalGunOutlineToggleId, 1f);
        Shader.SetGlobalFloat(GlobalGunRimLightToggleId, 1f);
        Shader.SetGlobalFloat(GlobalGunFinalColorGradientToggleId, 1f);
        Shader.SetGlobalFloat(GlobalGunFinalColorGradientParametersOverrideId, 0f);
        Shader.SetGlobalColor(GlobalGunFinalColorGradientColorId, Color.white);
        Shader.SetGlobalFloat(GlobalGunFinalColorGradientMinYId, 0f);
        Shader.SetGlobalFloat(GlobalGunFinalColorGradientMaxYId, 1f);
    }

    private void EnsureDirectLightHierarchyMigrated()
    {
        if (_directLightHierarchyVersion >= 1)
        {
            return;
        }

        if (_mainLightAdjustmentMode == CharacterRenderMainLightAdjustmentMode.FollowCamera)
        {
            _mainLightOverrideMode = CharacterRenderMainLightOverrideMode.FollowCamera;
        }
        else
        {
            _mainLightOverrideMode = _enableCustomLight
                ? CharacterRenderMainLightOverrideMode.Custom
                : CharacterRenderMainLightOverrideMode.Off;
        }

        if (_mainLightAdjustmentMode == CharacterRenderMainLightAdjustmentMode.BackLight)
        {
            _enableBackLight = true;
        }

        _directLightHierarchyMigrated = true;
        _directLightHierarchyVersion = 1;
    }

    private void EnsureHierarchyFeatureDefaultsInitialized()
    {
        if (_hierarchyFeatureVersion >= 1)
        {
            return;
        }

        _sceneShadowMode = CharacterRenderSceneShadowMode.UnityOnly;
        _enableRimLight = true;
        _enableRimCustomDirection = false;
        _rimDirectionSpace = CharacterRenderRimDirectionSpace.World;
        _rimDirection = Vector3.zero;
        _enableRimFakePointMask = false;
        _rimFakePointMaskPosition = Vector3.zero;
        _rimFakePointMaskRange = 1f;
        _rimFakePointMaskPower = 2f;
        _enableRimFakeDirectMask = false;
        _rimFakeDirectMaskDirection = Vector3.right;
        _rimFakeDirectMaskPosition = Vector3.zero;
        _rimFakeDirectMaskRange = 1f;
        _rimIntensity = 1f;
        _enableFinalColorGradient = true;
        _finalColorGradientColor = Color.white;
        _finalColorGradientMinY = 0f;
        _finalColorGradientMaxY = 1f;
        _hierarchyFeatureDefaultsInitialized = true;
        _hierarchyFeatureVersion = 1;
    }

    private void EnsureShadowFollowSemantics()
    {
        if (!_enablePerObjectShadowDirectionFollowCustomLight)
        {
            return;
        }

        _enablePerObjectShadowDirectionFollowMainLightAdjustment = true;
        _enablePerObjectShadowDirectionFollowCustomLight = false;
    }

    private void EnsureShadowToggleDefaultsInitialized()
    {
        if (_shadowToggleDefaultsInitialized)
        {
            return;
        }

        _enablePlanarShadow = true;
        _enableHighQualityShadow = true;
        _shadowToggleDefaultsInitialized = true;
    }

    private void EnsureOutlineToggleDefaultInitialized()
    {
        if (_outlineToggleDefaultInitialized)
        {
            return;
        }

        _enableOutline = true;
        _outlineToggleDefaultInitialized = true;
    }

    private void EnsureShadowDirectionDefaultsInitialized()
    {
        if (_shadowDirectionDefaultsInitialized)
        {
            return;
        }

        _planarShadowDirection = Vector3.zero;
        _planarShadowStrength = 1f;
        _perObjectShadowDirection = Vector3.zero;
        _shadowDirectionDefaultsInitialized = true;
    }

    private void EnsureAdditionalLightsToggleDefaultInitialized()
    {
        if (_additionalLightsToggleDefaultInitialized)
        {
            return;
        }

        _enableCharacterAdditionalLights = true;
        _additionalLightsToggleDefaultInitialized = true;
    }

    private void EnsureAdditionalLightsLobeToggleDefaultsInitialized()
    {
        if (_additionalLightsLobeToggleVersion >= 1)
        {
            return;
        }

        _enableCharacterAdditionalLightDiffuse = true;
        _enableCharacterAdditionalLightSpecular = true;
        _additionalLightsLobeToggleVersion = 1;
    }

#if UNITY_EDITOR
    private void HandleProfileChanged(CharacterRenderLightProfile changedProfile)
    {
        if (changedProfile == null || changedProfile != _profile || !isActiveAndEnabled)
        {
            return;
        }

        ApplyNow();
        SceneView.RepaintAll();
    }
#endif
}
