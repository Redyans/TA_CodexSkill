using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System;
#endif

[CreateAssetMenu(fileName = "charaRenderSetting", menuName = "GameArt/角色 Render Setting 配置")]
public sealed class CharacterRenderLightProfile : ScriptableObject
{
    private const float MinDirectionMagnitude = 0.0001f;
    private const float MaxPerObjectShadowStrength = 2f;
    private static readonly Color DefaultIndirectTintColor = Color.white;

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
    [SerializeField] private bool _enablePlanarShadowDirectionFollowCustomLight;
    [SerializeField] private Vector3 _planarShadowDirection = Vector3.zero;
    [SerializeField] [Min(0f)] private float _planarShadowStrength = 1f;
    [SerializeField] [Min(0f)] private float _planarShadowFalloff = 1f;
    [SerializeField] private float _planarShadowRange;
    [SerializeField] private float _planarShadowPlaneHeight;
    [SerializeField] private Vector3 _planarShadowGlobalCenter = Vector3.zero;
    [SerializeField] private Color _planarShadowColor = Color.black;
    [SerializeField] private CharacterRenderSceneShadowMode _sceneShadowMode = CharacterRenderSceneShadowMode.UnityOnly;
    [SerializeField] private bool _enableHighQualityShadow = true;
    [SerializeField] private bool _enablePerObjectShadowDirectionFollowCustomLight;
    [SerializeField] private bool _enablePerObjectShadowDirectionFollowMainLightAdjustment;
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

    // 旧版数据保留用于一次性迁移：BackLight 曾经是主光调整枚举的一部分。
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

#if UNITY_EDITOR
    public static event Action<CharacterRenderLightProfile> ProfileChanged;

    public void NotifyChangedInEditor() => ProfileChanged?.Invoke(this);
#endif

    public CharacterRenderMainLightOverrideMode MainLightOverrideMode
    {
        get
        {
            EnsureDirectLightHierarchyMigrated();
            return _mainLightOverrideMode;
        }
    }

    // 兼容旧调用；新代码应使用 MainLightOverrideMode。
    public bool EnableCustomLight => MainLightOverrideMode != CharacterRenderMainLightOverrideMode.Off;

    public CharacterRenderMainLightAdjustmentMode MainLightAdjustmentMode =>
        MainLightOverrideMode == CharacterRenderMainLightOverrideMode.FollowCamera
            ? CharacterRenderMainLightAdjustmentMode.FollowCamera
            : CharacterRenderMainLightAdjustmentMode.Off;

    public bool EnableBackLight
    {
        get
        {
            EnsureDirectLightHierarchyMigrated();
            return _enableBackLight;
        }
    }

    public bool EnableCharacterAdditionalLights
    {
        get
        {
            EnsureAdditionalLightsToggleDefaultInitialized();
            return _enableCharacterAdditionalLights;
        }
    }

    public bool EnableCharacterAdditionalLightDiffuse
    {
        get
        {
            EnsureAdditionalLightsLobeToggleDefaultsInitialized();
            return _enableCharacterAdditionalLightDiffuse;
        }
    }

    public bool EnableCharacterAdditionalLightSpecular
    {
        get
        {
            EnsureAdditionalLightsLobeToggleDefaultsInitialized();
            return _enableCharacterAdditionalLightSpecular;
        }
    }

    public Color CustomLightColor => _customLightColor;
    public float CustomLightStrength => Mathf.Max(0f, _customLightStrength);
    public Vector3 VirtualLightCameraDirectionOffsetEuler => _virtualLightCameraDirectionOffsetEuler;
    public Color VirtualLightColor => _virtualLightColor;
    public float VirtualLightIntensity => Mathf.Max(0f, _virtualLightIntensity);
    public float VirtualLightOffset => Mathf.Clamp(_virtualLightOffset, -1f, 1f);
    public Vector3 CustomLightDirection => GetWorldDirection();

    public bool EnablePlanarShadow
    {
        get
        {
            EnsureShadowToggleDefaultsInitialized();
            return _enablePlanarShadow;
        }
    }

    public CharacterRenderSceneShadowMode SceneShadowMode
    {
        get
        {
            EnsureHierarchyFeatureDefaultsInitialized();
            return _sceneShadowMode;
        }
    }

    public bool EnableHighQualityShadow
    {
        get
        {
            EnsureShadowToggleDefaultsInitialized();
            return _enableHighQualityShadow;
        }
    }

    public bool EnablePlanarShadowDirectionFollowMainLightOverride => _enablePlanarShadowDirectionFollowCustomLight;
    public bool EnablePlanarShadowDirectionFollowCustomLight => EnablePlanarShadowDirectionFollowMainLightOverride;
    public float PlanarShadowStrength
    {
        get
        {
            EnsureShadowDirectionDefaultsInitialized();
            return Mathf.Max(0f, _planarShadowStrength);
        }
    }

    public float PlanarShadowFalloff => Mathf.Max(0f, _planarShadowFalloff);
    public float PlanarShadowRange => _planarShadowRange;
    public float PlanarShadowPlaneHeight => _planarShadowPlaneHeight;
    public Vector3 PlanarShadowGlobalCenter => _planarShadowGlobalCenter;
    public Color PlanarShadowColor => _planarShadowColor;

    public bool EnableOutline
    {
        get
        {
            EnsureOutlineToggleDefaultInitialized();
            return _enableOutline;
        }
    }

    public bool EnableRimLight
    {
        get
        {
            EnsureHierarchyFeatureDefaultsInitialized();
            return _enableRimLight;
        }
    }

    public bool EnableRimCustomDirection => _enableRimCustomDirection;
    public CharacterRenderRimDirectionSpace RimDirectionSpace => _rimDirectionSpace;
    public Vector3 RimDirection => NormalizeDirectionOrZero(_rimDirection);
    public bool EnableRimFakePointMask => _enableRimFakePointMask;
    public Vector3 RimFakePointMaskPosition => _rimFakePointMaskPosition;
    public float RimFakePointMaskRange => Mathf.Max(0.01f, _rimFakePointMaskRange);
    public float RimFakePointMaskPower => Mathf.Clamp(_rimFakePointMaskPower, 0.25f, 8f);
    public bool EnableRimFakeDirectMask => _enableRimFakeDirectMask;
    public Vector3 RimFakeDirectMaskDirection => NormalizeDirectionOrZero(_rimFakeDirectMaskDirection);
    public Vector3 RimFakeDirectMaskPosition => _rimFakeDirectMaskPosition;
    public float RimFakeDirectMaskRange => Mathf.Max(0.01f, _rimFakeDirectMaskRange);
    public float RimIntensity => Mathf.Max(0f, _rimIntensity);

    public bool EnableFinalColorGradient
    {
        get
        {
            EnsureHierarchyFeatureDefaultsInitialized();
            return _enableFinalColorGradient;
        }
    }

    public Color FinalColorGradientColor => _finalColorGradientColor;
    public float FinalColorGradientMinY => _finalColorGradientMinY;
    public float FinalColorGradientMaxY => _finalColorGradientMaxY;

    public bool EnablePerObjectShadowDirectionFollowMainLightOverride =>
        _enablePerObjectShadowDirectionFollowMainLightAdjustment || _enablePerObjectShadowDirectionFollowCustomLight;
    public bool EnablePerObjectShadowDirectionFollowCustomLight => EnablePerObjectShadowDirectionFollowMainLightOverride;
    public bool EnablePerObjectShadowDirectionFollowMainLightAdjustment => EnablePerObjectShadowDirectionFollowMainLightOverride;
    public float SelfShadowStrength => Mathf.Clamp(_selfShadowStrength, 0f, MaxPerObjectShadowStrength);
    public float EnvironmentShadowStrength => Mathf.Clamp(_environmentShadowStrength, 0f, MaxPerObjectShadowStrength);
    public float SpecularShadowStrength => Mathf.Clamp01(_specularShadowStrength);
    public bool EnableGlobalIndirectLight => _enableGlobalIndirectLight;
    public bool OverrideSceneAmbient => _overrideSceneAmbient;
    public float GlobalIndirectIntensity => Mathf.Max(0f, _globalIndirectIntensity);
    public Color GlobalIndirectTintColor => _globalIndirectTintColor;

    public MonsterRenderMainLightOverrideMode MonsterMainLightOverrideMode => _monsterMainLightOverrideMode;
    public Color MonsterCustomLightColor => _monsterCustomLightColor;
    public float MonsterCustomLightStrength => Mathf.Max(0f, _monsterCustomLightStrength);
    public Vector3 MonsterCustomLightDirection => NormalizeDirectionOrZero(_monsterCustomLightDirection);
    public Vector3 MonsterVirtualLightCameraDirectionOffsetEuler => _monsterVirtualLightCameraDirectionOffsetEuler;
    public bool MonsterAdditionalLightsEnabled => _monsterAdditionalLightsEnabled;
    public bool MonsterAdditionalLightDiffuseEnabled => _monsterAdditionalLightDiffuseEnabled;
    public bool MonsterAdditionalLightSpecularEnabled => _monsterAdditionalLightSpecularEnabled;
    public bool MonsterEnvironmentFollowCharacter => _monsterEnvironmentFollowCharacter;
    public bool MonsterEnableGlobalIndirectLight => _monsterEnableGlobalIndirectLight;
    public bool MonsterOverrideSceneAmbient => _monsterOverrideSceneAmbient;
    public float MonsterGlobalIndirectIntensity => Mathf.Max(0f, _monsterGlobalIndirectIntensity);
    public Color MonsterGlobalIndirectTintColor => _monsterGlobalIndirectTintColor;
    public bool MonsterOtherFeaturesFollowCharacter => _monsterOtherFeaturesFollowCharacter;
    public bool MonsterEnableOutline => _monsterEnableOutline;
    public bool MonsterEnableRimLight => _monsterEnableRimLight;
    public bool MonsterEnableFinalColorGradient => _monsterEnableFinalColorGradient;
    public Color MonsterFinalColorGradientColor => _monsterFinalColorGradientColor;
    public float MonsterFinalColorGradientMinY => _monsterFinalColorGradientMinY;
    public float MonsterFinalColorGradientMaxY => _monsterFinalColorGradientMaxY;

    public Vector3 GetWorldDirection() => NormalizeDirectionOrZero(_customLightDirection);
    public Vector3 GetPlanarShadowDirection() => NormalizeDirectionOrZero(_planarShadowDirection);
    public Vector3 GetHighQualityShadowDirection() => NormalizeDirectionOrZero(_perObjectShadowDirection);

    public void SetMainLightOverrideMode(CharacterRenderMainLightOverrideMode mode)
    {
        _mainLightOverrideMode = mode;
        _enableCustomLight = mode != CharacterRenderMainLightOverrideMode.Off;
        _mainLightAdjustmentMode = mode == CharacterRenderMainLightOverrideMode.FollowCamera
            ? CharacterRenderMainLightAdjustmentMode.FollowCamera
            : CharacterRenderMainLightAdjustmentMode.Off;
        _directLightHierarchyMigrated = true;
        _directLightHierarchyVersion = 1;
    }

    public void SetCustomLightEnabled(bool enabled)
    {
        SetMainLightOverrideMode(enabled
            ? CharacterRenderMainLightOverrideMode.Custom
            : CharacterRenderMainLightOverrideMode.Off);
    }

    public void SetMainLightAdjustmentMode(CharacterRenderMainLightAdjustmentMode mode)
    {
        SetMainLightOverrideMode(mode == CharacterRenderMainLightAdjustmentMode.FollowCamera
            ? CharacterRenderMainLightOverrideMode.FollowCamera
            : CharacterRenderMainLightOverrideMode.Off);
        if (mode == CharacterRenderMainLightAdjustmentMode.BackLight)
        {
            _enableBackLight = true;
        }
    }

    public void SetBackLightEnabled(bool enabled)
    {
        _enableBackLight = enabled;
        _directLightHierarchyMigrated = true;
        _directLightHierarchyVersion = 1;
    }

    public void SetCharacterAdditionalLightsEnabled(bool enabled)
    {
        _enableCharacterAdditionalLights = enabled;
        _additionalLightsToggleDefaultInitialized = true;
    }

    public void SetCharacterAdditionalLightDiffuseEnabled(bool enabled)
    {
        _enableCharacterAdditionalLightDiffuse = enabled;
        _additionalLightsLobeToggleVersion = 1;
    }

    public void SetCharacterAdditionalLightSpecularEnabled(bool enabled)
    {
        _enableCharacterAdditionalLightSpecular = enabled;
        _additionalLightsLobeToggleVersion = 1;
    }

    public void SetCustomLightColor(Color color) => _customLightColor = color;
    public void SetCustomLightStrength(float strength) => _customLightStrength = Mathf.Max(0f, strength);
    public void SetVirtualLightCameraDirectionOffsetEuler(Vector3 offsetEuler) => _virtualLightCameraDirectionOffsetEuler = offsetEuler;
    public void SetVirtualLightColor(Color color) => _virtualLightColor = color;
    public void SetVirtualLightIntensity(float value) => _virtualLightIntensity = Mathf.Max(0f, value);
    public void SetVirtualLightOffset(float value) => _virtualLightOffset = Mathf.Clamp(value, -1f, 1f);

    public void SetWorldDirection(Vector3 worldDirection) => _customLightDirection = NormalizeDirectionOrZero(worldDirection);

    public void SetPlanarShadowEnabled(bool enabled)
    {
        _enablePlanarShadow = enabled;
        _shadowToggleDefaultsInitialized = true;
    }

    public void SetPlanarShadowDirectionFollowMainLightOverrideEnabled(bool enabled) =>
        _enablePlanarShadowDirectionFollowCustomLight = enabled;

    public void SetPlanarShadowDirectionFollowCustomLightEnabled(bool enabled) =>
        SetPlanarShadowDirectionFollowMainLightOverrideEnabled(enabled);


    public void SetPlanarShadowDirection(Vector3 worldDirection) =>
        _planarShadowDirection = NormalizeDirectionOrZero(worldDirection);

    public void SetPlanarShadowStrength(float strength)
    {
        _planarShadowStrength = Mathf.Max(0f, strength);
        _shadowDirectionDefaultsInitialized = true;
    }

    public void SetPlanarShadowFalloff(float falloff) => _planarShadowFalloff = Mathf.Max(0f, falloff);
    public void SetPlanarShadowRange(float range) => _planarShadowRange = range;
    public void SetPlanarShadowPlaneHeight(float planeHeight) => _planarShadowPlaneHeight = planeHeight;
    public void SetPlanarShadowGlobalCenter(Vector3 globalCenter) => _planarShadowGlobalCenter = globalCenter;
    public void SetPlanarShadowColor(Color color) => _planarShadowColor = color;

    public void SetSceneShadowMode(CharacterRenderSceneShadowMode mode)
    {
        _sceneShadowMode = mode;
        _hierarchyFeatureDefaultsInitialized = true;
        _hierarchyFeatureVersion = 1;
    }

    public void SetHighQualityShadowEnabled(bool enabled)
    {
        _enableHighQualityShadow = enabled;
        _shadowToggleDefaultsInitialized = true;
    }

    public void SetOutlineEnabled(bool enabled)
    {
        _enableOutline = enabled;
        _outlineToggleDefaultInitialized = true;
    }

    public void SetRimLightEnabled(bool enabled)
    {
        _enableRimLight = enabled;
        _hierarchyFeatureDefaultsInitialized = true;
        _hierarchyFeatureVersion = 1;
    }

    public void SetRimCustomDirectionEnabled(bool enabled) => _enableRimCustomDirection = enabled;
    public void SetRimDirectionSpace(CharacterRenderRimDirectionSpace space) => _rimDirectionSpace = space;
    public void SetRimDirection(Vector3 direction) => _rimDirection = NormalizeDirectionOrZero(direction);
    public void SetRimFakePointMaskEnabled(bool enabled) => _enableRimFakePointMask = enabled;
    public void SetRimFakePointMaskPosition(Vector3 position) => _rimFakePointMaskPosition = position;
    public void SetRimFakePointMaskRange(float value) => _rimFakePointMaskRange = Mathf.Max(0.01f, value);
    public void SetRimFakePointMaskPower(float value) => _rimFakePointMaskPower = Mathf.Clamp(value, 0.25f, 8f);
    public void SetRimFakeDirectMaskEnabled(bool enabled) => _enableRimFakeDirectMask = enabled;
    public void SetRimFakeDirectMaskDirection(Vector3 direction) => _rimFakeDirectMaskDirection = NormalizeDirectionOrZero(direction);
    public void SetRimFakeDirectMaskPosition(Vector3 position) => _rimFakeDirectMaskPosition = position;
    public void SetRimFakeDirectMaskRange(float value) => _rimFakeDirectMaskRange = Mathf.Max(0.01f, value);
    public void SetRimIntensity(float value) => _rimIntensity = Mathf.Max(0f, value);

    public void SetFinalColorGradientEnabled(bool enabled)
    {
        _enableFinalColorGradient = enabled;
        _hierarchyFeatureDefaultsInitialized = true;
        _hierarchyFeatureVersion = 1;
    }

    public void SetFinalColorGradientColor(Color color) => _finalColorGradientColor = color;
    public void SetFinalColorGradientMinY(float value) => _finalColorGradientMinY = value;
    public void SetFinalColorGradientMaxY(float value) => _finalColorGradientMaxY = value;

    public void SetPerObjectShadowDirectionFollowMainLightOverrideEnabled(bool enabled)
    {
        _enablePerObjectShadowDirectionFollowMainLightAdjustment = enabled;
        _enablePerObjectShadowDirectionFollowCustomLight = false;
    }

    public void SetPerObjectShadowDirectionFollowCustomLightEnabled(bool enabled) =>
        SetPerObjectShadowDirectionFollowMainLightOverrideEnabled(enabled);

    public void SetPerObjectShadowDirectionFollowMainLightAdjustmentEnabled(bool enabled) =>
        SetPerObjectShadowDirectionFollowMainLightOverrideEnabled(enabled);

    public void SetHighQualityShadowDirection(Vector3 worldDirection) =>
        _perObjectShadowDirection = NormalizeDirectionOrZero(worldDirection);

    public void SetSelfShadowStrength(float strength) =>
        _selfShadowStrength = Mathf.Clamp(strength, 0f, MaxPerObjectShadowStrength);
    public void SetEnvironmentShadowStrength(float strength) =>
        _environmentShadowStrength = Mathf.Clamp(strength, 0f, MaxPerObjectShadowStrength);
    public void SetSpecularShadowStrength(float strength) => _specularShadowStrength = Mathf.Clamp01(strength);
    public void SetGlobalIndirectIntensity(float intensity) => _globalIndirectIntensity = Mathf.Max(0f, intensity);
    public void SetGlobalIndirectLightEnabled(bool enabled) => _enableGlobalIndirectLight = enabled;
    public void SetOverrideSceneAmbient(bool enabled) => _overrideSceneAmbient = enabled;
    public void SetGlobalIndirectTintColor(Color tintColor) => _globalIndirectTintColor = tintColor;

    public void SetMonsterMainLightOverrideMode(MonsterRenderMainLightOverrideMode mode) => _monsterMainLightOverrideMode = mode;
    public void SetMonsterCustomLightColor(Color color) => _monsterCustomLightColor = color;
    public void SetMonsterCustomLightStrength(float strength) => _monsterCustomLightStrength = Mathf.Max(0f, strength);
    public void SetMonsterCustomLightDirection(Vector3 direction) => _monsterCustomLightDirection = NormalizeDirectionOrZero(direction);
    public void SetMonsterVirtualLightCameraDirectionOffsetEuler(Vector3 offsetEuler) => _monsterVirtualLightCameraDirectionOffsetEuler = offsetEuler;
    public void SetMonsterAdditionalLightsEnabled(bool enabled) => _monsterAdditionalLightsEnabled = enabled;
    public void SetMonsterAdditionalLightDiffuseEnabled(bool enabled) => _monsterAdditionalLightDiffuseEnabled = enabled;
    public void SetMonsterAdditionalLightSpecularEnabled(bool enabled) => _monsterAdditionalLightSpecularEnabled = enabled;
    public void SetMonsterEnvironmentFollowCharacter(bool enabled) => _monsterEnvironmentFollowCharacter = enabled;
    public void SetMonsterEnableGlobalIndirectLight(bool enabled) => _monsterEnableGlobalIndirectLight = enabled;
    public void SetMonsterOverrideSceneAmbient(bool enabled) => _monsterOverrideSceneAmbient = enabled;
    public void SetMonsterGlobalIndirectIntensity(float value) => _monsterGlobalIndirectIntensity = Mathf.Max(0f, value);
    public void SetMonsterGlobalIndirectTintColor(Color color) => _monsterGlobalIndirectTintColor = color;
    public void SetMonsterOtherFeaturesFollowCharacter(bool enabled) => _monsterOtherFeaturesFollowCharacter = enabled;
    public void SetMonsterOutlineEnabled(bool enabled) => _monsterEnableOutline = enabled;
    public void SetMonsterRimLightEnabled(bool enabled) => _monsterEnableRimLight = enabled;
    public void SetMonsterFinalColorGradientEnabled(bool enabled) => _monsterEnableFinalColorGradient = enabled;
    public void SetMonsterFinalColorGradientColor(Color color) => _monsterFinalColorGradientColor = color;
    public void SetMonsterFinalColorGradientMinY(float value) => _monsterFinalColorGradientMinY = value;
    public void SetMonsterFinalColorGradientMaxY(float value) => _monsterFinalColorGradientMaxY = value;

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

#if UNITY_EDITOR
        ProfileChanged?.Invoke(this);
#endif
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

    private void SanitizeDirections()
    {
        _customLightDirection = NormalizeDirectionOrZero(_customLightDirection);
        _virtualLightIntensity = Mathf.Max(0f, _virtualLightIntensity);
        _virtualLightOffset = Mathf.Clamp(_virtualLightOffset, -1f, 1f);
        _planarShadowDirection = NormalizeDirectionOrZero(_planarShadowDirection);
        _perObjectShadowDirection = NormalizeDirectionOrZero(_perObjectShadowDirection);
        _monsterCustomLightDirection = NormalizeDirectionOrZero(_monsterCustomLightDirection);
        _rimDirection = NormalizeDirectionOrZero(_rimDirection);
        _rimFakeDirectMaskDirection = NormalizeDirectionOrZero(_rimFakeDirectMaskDirection);
    }

    private static Vector3 NormalizeDirectionOrZero(Vector3 direction) =>
        direction.sqrMagnitude < MinDirectionMagnitude ? Vector3.zero : direction.normalized;

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
        _planarShadowFalloff = 1f;
        _planarShadowRange = 0f;
        _planarShadowPlaneHeight = 0f;
        _planarShadowGlobalCenter = Vector3.zero;
        _planarShadowColor = Color.black;
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
}
