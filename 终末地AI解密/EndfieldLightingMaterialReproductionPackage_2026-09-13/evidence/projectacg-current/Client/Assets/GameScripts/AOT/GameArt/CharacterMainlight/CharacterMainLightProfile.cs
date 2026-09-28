using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System;
#endif

[CreateAssetMenu(fileName = "CharacterMainLightProfile", menuName = "GameArt/Character Main Light Profile")]
public sealed class CharacterMainLightProfile : ScriptableObject
{
    [SerializeField] private CharacterMainLightConfiguration _configuration;
    [SerializeField] [HideInInspector] [FormerlySerializedAs("_configurations")] private CharacterMainLightConfiguration[] _legacyConfigurations;

#if UNITY_EDITOR
    public static event Action<CharacterMainLightProfile> ProfileChanged;
#endif

    public int ConfigurationCount => _configuration != null ? 1 : 0;

    public int LegacyConfigurationCount => _legacyConfigurations != null ? _legacyConfigurations.Length : 0;

    public bool HasLegacyConfigurations => LegacyConfigurationCount > 0;

    public string GetConfigurationName(int index)
    {
        if (!TryGetConfiguration(index, out CharacterMainLightConfiguration configuration))
        {
            return string.Empty;
        }

        return configuration.DisplayName;
    }

    public bool TryGetConfiguration(int index, out CharacterMainLightConfiguration configuration)
    {
        EnsureConfiguration();
        if (index != 0 || _configuration == null)
        {
            configuration = null;
            return false;
        }

        configuration = _configuration;
        return true;
    }

    public int SaveConfiguration(CharacterMainLightConfiguration configuration)
    {
        if (configuration == null)
        {
            return -1;
        }

        configuration.Name = SanitizeConfigurationName(configuration.Name);
        _configuration = configuration;
        return 0;
    }

    public bool OverwriteConfiguration(int index, CharacterMainLightConfiguration configuration)
    {
        if (index != 0 || configuration == null)
        {
            return false;
        }

        EnsureConfiguration();
        configuration.Name = _configuration != null
            ? _configuration.DisplayName
            : SanitizeConfigurationName(configuration.Name);
        _configuration = configuration;
        return true;
    }

    public bool DeleteConfiguration(int index)
    {
        if (index != 0 || _configuration == null)
        {
            return false;
        }

        _configuration = null;
        return true;
    }

    public int FindConfigurationIndex(string configurationName)
    {
        EnsureConfiguration();
        string sanitizedName = SanitizeConfigurationName(configurationName);
        if (_configuration != null
            && string.Equals(_configuration.DisplayName, sanitizedName, System.StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return -1;
    }

    public bool TryGetLegacyConfiguration(int index, out CharacterMainLightConfiguration configuration)
    {
        if (_legacyConfigurations == null || index < 0 || index >= _legacyConfigurations.Length)
        {
            configuration = null;
            return false;
        }

        configuration = _legacyConfigurations[index];
        return configuration != null;
    }

    public void ClearLegacyConfigurations()
    {
        _legacyConfigurations = null;
    }

    private void EnsureConfiguration()
    {
        if (_configuration != null)
        {
            _configuration.Name = SanitizeConfigurationName(_configuration.Name);
        }
    }

    public static string SanitizeConfigurationName(string configurationName)
    {
        return string.IsNullOrWhiteSpace(configurationName) ? "Hall" : configurationName.Trim();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ProfileChanged?.Invoke(this);
    }
#endif
}

[System.Serializable]
public sealed class CharacterMainLightConfiguration
{
    public string Name = "Hall";
    public bool UseCameraDirection;
    public Vector3 LightRotationOffsetEuler = Vector3.zero;
    public Color MainLightColor = Color.white;
    public float MainLightIntensity = 1f;
    public Vector3 MainLightDirection = Vector3.up;
    public bool EnableCustomMainLightOnMaterials = true;
    public bool EnableMonsterCustomMainLightOnMaterials = true;
    public bool EnableGunCustomMainLightOnMaterials = true;
    public bool EnableAdditionalLights = true;
    public int SceneShadowModeValue;
    public bool EnableCharacterSceneShadow = true;
    public bool EnableHairFringeSelfShadow;
    public bool EnableMonsterSceneShadow = true;
    public bool EnableGunSceneShadow = true;
    public bool OverridePerObjectShadowDirection;
    public Vector3 PerObjectShadowDirection = Vector3.up;
    public bool EnablePlanarShadow = true;
    public bool EnableCharacterPlanarShadow = true;
    public bool EnableMonsterPlanarShadow = true;
    public bool EnableGunPlanarShadow = true;
    public float PlanarShadowFalloff = 1f;
    public float PlanarShadowRange;
    public Vector3 PlanarShadowLightDirection = Vector3.up;
    public bool UsePlanarShadowCameraFacingDirection;
    public bool ShowPlanarShadowSceneDirection;
    public float PlanarShadowSceneHandleLength = 1.5f;
    public Color PlanarShadowSceneHandleColor = new Color(1f, 0.85f, 0.2f, 1f);
    public float PlanarShadowPlaneHeight;
    public bool UsePlanarShadowBoundsCenter = true;
    public Color PlanarShadowColor = new Color(0f, 0f, 0f, 1f);
    public bool EnableCustomEnvironmentLightOnMaterials = true;
    public bool EnableMonsterCustomEnvironmentLightOnMaterials = true;
    public bool EnableGunCustomEnvironmentLightOnMaterials = true;
    public Color CustomEnvironmentColor = Color.white;
    public float CustomEnvironmentLightBlend;
    public float IndirectSpecularIntensity = 1f;
    public bool EnableCustomEnvironmentCubemap;
    public Cubemap CustomEnvironmentCubemap;
    public float CustomEnvironmentCubemapRotation;
    public float CustomEnvironmentCubemapIntensity;
    public bool ShowSceneDirection;
    public float SceneHandleLength = 1.5f;
    public Color SceneHandleColor = new Color(1f, 0.85f, 0.2f, 1f);
    public bool ShowPerObjectShadowSceneDirection;
    public float PerObjectShadowSceneHandleLength = 1.5f;
    public Color PerObjectShadowSceneHandleColor = new Color(0.25f, 0.7f, 1f, 1f);

    public string DisplayName => CharacterMainLightProfile.SanitizeConfigurationName(Name);

    public CharacterMainLightConfiguration Clone()
    {
        return new CharacterMainLightConfiguration
        {
            Name = Name,
            UseCameraDirection = UseCameraDirection,
            LightRotationOffsetEuler = LightRotationOffsetEuler,
            MainLightColor = MainLightColor,
            MainLightIntensity = MainLightIntensity,
            MainLightDirection = MainLightDirection,
            EnableCustomMainLightOnMaterials = EnableCustomMainLightOnMaterials,
            EnableMonsterCustomMainLightOnMaterials = EnableMonsterCustomMainLightOnMaterials,
            EnableGunCustomMainLightOnMaterials = EnableGunCustomMainLightOnMaterials,
            EnableAdditionalLights = EnableAdditionalLights,
            SceneShadowModeValue = SceneShadowModeValue,
            EnableCharacterSceneShadow = EnableCharacterSceneShadow,
            EnableHairFringeSelfShadow = EnableHairFringeSelfShadow,
            EnableMonsterSceneShadow = EnableMonsterSceneShadow,
            EnableGunSceneShadow = EnableGunSceneShadow,
            OverridePerObjectShadowDirection = OverridePerObjectShadowDirection,
            PerObjectShadowDirection = PerObjectShadowDirection,
            EnablePlanarShadow = EnablePlanarShadow,
            EnableCharacterPlanarShadow = EnableCharacterPlanarShadow,
            EnableMonsterPlanarShadow = EnableMonsterPlanarShadow,
            EnableGunPlanarShadow = EnableGunPlanarShadow,
            PlanarShadowFalloff = PlanarShadowFalloff,
            PlanarShadowRange = PlanarShadowRange,
            PlanarShadowLightDirection = PlanarShadowLightDirection,
            UsePlanarShadowCameraFacingDirection = UsePlanarShadowCameraFacingDirection,
            ShowPlanarShadowSceneDirection = ShowPlanarShadowSceneDirection,
            PlanarShadowSceneHandleLength = PlanarShadowSceneHandleLength,
            PlanarShadowSceneHandleColor = PlanarShadowSceneHandleColor,
            PlanarShadowPlaneHeight = PlanarShadowPlaneHeight,
            UsePlanarShadowBoundsCenter = UsePlanarShadowBoundsCenter,
            PlanarShadowColor = PlanarShadowColor,
            EnableCustomEnvironmentLightOnMaterials = EnableCustomEnvironmentLightOnMaterials,
            EnableMonsterCustomEnvironmentLightOnMaterials = EnableMonsterCustomEnvironmentLightOnMaterials,
            EnableGunCustomEnvironmentLightOnMaterials = EnableGunCustomEnvironmentLightOnMaterials,
            CustomEnvironmentColor = CustomEnvironmentColor,
            CustomEnvironmentLightBlend = CustomEnvironmentLightBlend,
            IndirectSpecularIntensity = IndirectSpecularIntensity,
            EnableCustomEnvironmentCubemap = EnableCustomEnvironmentCubemap,
            CustomEnvironmentCubemap = CustomEnvironmentCubemap,
            CustomEnvironmentCubemapRotation = CustomEnvironmentCubemapRotation,
            CustomEnvironmentCubemapIntensity = CustomEnvironmentCubemapIntensity,
            ShowSceneDirection = ShowSceneDirection,
            SceneHandleLength = SceneHandleLength,
            SceneHandleColor = SceneHandleColor,
            ShowPerObjectShadowSceneDirection = ShowPerObjectShadowSceneDirection,
            PerObjectShadowSceneHandleLength = PerObjectShadowSceneHandleLength,
            PerObjectShadowSceneHandleColor = PerObjectShadowSceneHandleColor,
        };
    }
}
