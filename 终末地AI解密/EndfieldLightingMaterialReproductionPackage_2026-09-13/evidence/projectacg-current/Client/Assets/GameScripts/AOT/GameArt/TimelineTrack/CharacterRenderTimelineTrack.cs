using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Per-character Timeline rendering override. Bind the track directly to a
/// character root GameObject; no component is added to the character prefab
/// and no material instance is created.
/// </summary>
[TrackColor(0.30f, 0.72f, 1.0f)]
[TrackClipType(typeof(CharacterRenderTimelineClip))]
[TrackBindingType(typeof(GameObject))]
public sealed class CharacterRenderTimelineTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CharacterRenderTimelineMixer>.Create(graph, inputCount);
    }
}

public sealed class CharacterRenderTimelineBehaviour : PlayableBehaviour
{
    public CharacterRenderTimelineClip clip;
}

public sealed class CharacterRenderTimelineMixer : PlayableBehaviour
{
    private readonly List<RendererBinding> _renderers = new List<RendererBinding>();
    private readonly CharacterRenderTimelineBlendState _blendState = new CharacterRenderTimelineBlendState();
    private GameObject _boundRoot;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        GameObject root = playerData as GameObject;
        if (root == null)
        {
            ClearOverrides();
            return;
        }

        if (_boundRoot != root)
        {
            ClearOverrides();
            CacheRenderers(root);
        }

        _blendState.Reset();
        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f)
            {
                continue;
            }

            Playable input = playable.GetInput(i);
            if (!input.IsValid())
            {
                continue;
            }

            CharacterRenderTimelineBehaviour behaviour =
                ((ScriptPlayable<CharacterRenderTimelineBehaviour>)input).GetBehaviour();
            if (behaviour != null && behaviour.clip != null)
            {
                _blendState.Add(behaviour.clip, weight);
            }
        }

        ApplyOverrides(_blendState);
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        ClearOverrides();
    }

    public override void OnGraphStop(Playable playable)
    {
        ClearOverrides();
    }

    private void CacheRenderers(GameObject root)
    {
        _boundRoot = root;
        _renderers.Clear();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            int materialCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
            if (materialCount > 0)
            {
                _renderers.Add(new RendererBinding(renderer, materialCount));
            }
        }
    }

    private void ApplyOverrides(CharacterRenderTimelineBlendState blendState)
    {
        CharacterRenderTimelineHighQualityShadowOverride.Set(
            this,
            blendState.HighQualityShadowEnableWeight,
            blendState.HighQualityShadowDirectionWeight,
            blendState.HighQualityShadowDirectionMode,
            blendState.HighQualityShadowDirection,
            blendState.HighQualityShadowStrengthWeight,
            blendState.BaseLitPerObjectShadowStrength);

        for (int i = _renderers.Count - 1; i >= 0; i--)
        {
            RendererBinding binding = _renderers[i];
            if (binding.renderer == null)
            {
                _renderers.RemoveAt(i);
                continue;
            }

            for (int materialIndex = 0; materialIndex < binding.materialCount; materialIndex++)
            {
                binding.renderer.GetPropertyBlock(binding.block, materialIndex);
                CharacterRenderTimelineProperties.Apply(binding.block, blendState);
                binding.renderer.SetPropertyBlock(binding.block, materialIndex);
            }
        }
    }

    private void ClearOverrides()
    {
        _blendState.Reset();
        ApplyOverrides(_blendState);
        _boundRoot = null;
        _renderers.Clear();
    }

    private sealed class RendererBinding
    {
        public readonly Renderer renderer;
        public readonly int materialCount;
        public readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

        public RendererBinding(Renderer renderer, int materialCount)
        {
            this.renderer = renderer;
            this.materialCount = materialCount;
        }
    }
}

internal sealed class CharacterRenderTimelineBlendState
{
    private const float Epsilon = 0.0001f;

    private float _mainTotal, _mainBest;
    private Vector3 _mainDirection;
    private Color _mainColor;
    private float _mainStrength;
    private CharacterRenderMainLightOverrideMode _mainMode;
    private bool _backLight;

    private float _virtualTotal, _virtualBest;
    private Vector3 _virtualCameraDirection, _virtualMaskDirection;
    private Color _virtualColor;
    private float _virtualIntensity, _virtualOffset;

    private float _additionalTotal, _additionalBest;
    private bool _additionalEnabled;
    private float _planarTotal, _planarBest;
    private bool _planarEnabled;
    private int _planarDirectionMode;
    private Vector3 _planarDirection, _planarCenter;
    private Color _planarColor;
    private float _planarStrength, _planarFalloff, _planarRange, _planarPlaneHeight;
    private float _outlineTotal, _outlineBest;
    private bool _outlineEnabled;
    private float _rimTotal, _rimBest;
    private bool _rimEnabled;
    private float _gradientTotal, _gradientBest;
    private bool _gradientEnabled;
    private Color _gradientColor;
    private float _gradientMinY, _gradientMaxY;
    private float _sceneShadowTotal, _sceneShadowBest;
    private CharacterRenderSceneShadowMode _sceneShadowMode;
    private float _highQualityShadowDirectionBest;
    private int _highQualityShadowDirectionMode;
    private Vector3 _highQualityShadowDirection;
    private float _highQualityShadowStrengthTotal;
    private float _perObjectShadowStrength, _baseLitPerObjectShadowStrength, _specularShadowStrength;
    private float _indirectTotal, _indirectBest;
    private bool _indirectEnabled, _overrideSceneAmbient;
    private float _indirectIntensity;
    private Color _indirectTint;

    public float MainWeight => Mathf.Clamp01(_mainTotal);
    public Vector3 MainDirection => ResolveDirection(_mainDirection, _mainTotal);
    public Color MainColor => Average(_mainColor, _mainTotal, Color.white);
    public float MainStrength => Average(_mainStrength, _mainTotal, 1f);
    public CharacterRenderMainLightOverrideMode MainMode => _mainMode;
    public bool BackLight => _backLight;
    public float VirtualWeight => Mathf.Clamp01(_virtualTotal);
    public Vector3 VirtualCameraDirection => ResolveDirection(_virtualCameraDirection, _virtualTotal);
    public Vector3 VirtualMaskDirection => ResolveDirection(_virtualMaskDirection, _virtualTotal);
    public Color VirtualColor => Average(_virtualColor, _virtualTotal, Color.white);
    public float VirtualIntensity => Average(_virtualIntensity, _virtualTotal, 0f);
    public float VirtualOffset => Average(_virtualOffset, _virtualTotal, 0f);
    public float AdditionalWeight => Mathf.Clamp01(_additionalTotal);
    public bool AdditionalEnabled => _additionalEnabled;
    public float PlanarWeight => Mathf.Clamp01(_planarTotal);
    public bool PlanarEnabled => _planarEnabled;
    public int PlanarDirectionMode => _planarDirectionMode;
    public Vector3 PlanarDirection => ResolveDirection(_planarDirection, _planarTotal);
    public float PlanarStrength => Average(_planarStrength, _planarTotal, 1f);
    public float PlanarFalloff => Average(_planarFalloff, _planarTotal, 1f);
    public float PlanarRange => Average(_planarRange, _planarTotal, 0f);
    public float PlanarPlaneHeight => Average(_planarPlaneHeight, _planarTotal, 0f);
    public Vector3 PlanarCenter => Average(_planarCenter, _planarTotal, Vector3.zero);
    public Color PlanarColor => Average(_planarColor, _planarTotal, Color.black);
    public float OutlineWeight => Mathf.Clamp01(_outlineTotal);
    public bool OutlineEnabled => _outlineEnabled;
    public float RimWeight => Mathf.Clamp01(_rimTotal);
    public bool RimEnabled => _rimEnabled;
    public float GradientWeight => Mathf.Clamp01(_gradientTotal);
    public bool GradientEnabled => _gradientEnabled;
    public Color GradientColor => Average(_gradientColor, _gradientTotal, Color.white);
    public float GradientMinY => Average(_gradientMinY, _gradientTotal, 0f);
    public float GradientMaxY => Average(_gradientMaxY, _gradientTotal, 1f);
    public float SceneShadowWeight => Mathf.Clamp01(_sceneShadowTotal);
    public CharacterRenderSceneShadowMode SceneShadowMode => _sceneShadowMode;
    public float HighQualityShadowEnableWeight => UsesPerObjectShadow(_sceneShadowMode) ? SceneShadowWeight : 0f;
    public float HighQualityShadowDirectionWeight => Mathf.Clamp01(_highQualityShadowDirectionBest);
    public int HighQualityShadowDirectionMode => _highQualityShadowDirectionMode;
    public Vector3 HighQualityShadowDirection => _highQualityShadowDirection;
    public float HighQualityShadowStrengthWeight => Mathf.Clamp01(_highQualityShadowStrengthTotal);
    public float PerObjectShadowStrength => Average(_perObjectShadowStrength, _highQualityShadowStrengthTotal, 1f);
    public float BaseLitPerObjectShadowStrength => Average(_baseLitPerObjectShadowStrength, _highQualityShadowStrengthTotal, 1f);
    public float SpecularShadowStrength => Average(_specularShadowStrength, _highQualityShadowStrengthTotal, 1f);
    public float IndirectWeight => Mathf.Clamp01(_indirectTotal);
    public bool IndirectEnabled => _indirectEnabled;
    public bool OverrideSceneAmbient => _indirectEnabled && _overrideSceneAmbient;
    public float IndirectIntensity => Average(_indirectIntensity, _indirectTotal, 1f);
    public Color IndirectTint => Average(_indirectTint, _indirectTotal, Color.white);

    public void Reset()
    {
        _mainTotal = _mainBest = _virtualTotal = _virtualBest = _additionalTotal = _additionalBest = 0f;
        _planarTotal = _planarBest = _outlineTotal = _outlineBest = _rimTotal = _rimBest = 0f;
        _gradientTotal = _gradientBest = _sceneShadowTotal = _sceneShadowBest = _highQualityShadowDirectionBest = 0f;
        _highQualityShadowStrengthTotal = _indirectTotal = _indirectBest = 0f;
        _mainDirection = _virtualCameraDirection = _virtualMaskDirection = _planarDirection = _planarCenter = _highQualityShadowDirection = Vector3.zero;
        _mainColor = _virtualColor = _planarColor = _gradientColor = _indirectTint = default;
        _mainStrength = _virtualIntensity = _virtualOffset = _planarStrength = _planarFalloff = _planarRange = _planarPlaneHeight = 0f;
        _gradientMinY = _gradientMaxY = _perObjectShadowStrength = _baseLitPerObjectShadowStrength = _specularShadowStrength = _indirectIntensity = 0f;
        _backLight = _additionalEnabled = _planarEnabled = _outlineEnabled = _rimEnabled = _gradientEnabled = _indirectEnabled = _overrideSceneAmbient = false;
        _mainMode = CharacterRenderMainLightOverrideMode.Off;
        _planarDirectionMode = 0;
        _sceneShadowMode = CharacterRenderSceneShadowMode.UnityOnly;
        _highQualityShadowDirectionMode = 0;
    }

    public void Add(CharacterRenderTimelineClip clip, float weight)
    {
        if (clip.overrideMainLight)
        {
            Vector3 direction = ResolveMainLightDirection(clip);
            _mainTotal += weight; _mainDirection += direction * weight; _mainColor += clip.mainLightColor * weight; _mainStrength += Mathf.Max(0f, clip.mainLightStrength) * weight;
            if (weight > _mainBest) { _mainBest = weight; _mainMode = clip.mainLightMode; _backLight = clip.enableBackLight; }
        }
        if (clip.overrideVirtualLight)
        {
            _virtualTotal += weight; _virtualCameraDirection += clip.virtualLightCameraDirection * weight; _virtualMaskDirection += clip.virtualLightMaskCameraDirection * weight;
            _virtualColor += clip.virtualLightColor * weight; _virtualIntensity += Mathf.Max(0f, clip.virtualLightIntensity) * weight; _virtualOffset += Mathf.Clamp(clip.virtualLightOffset, -1f, 1f) * weight;
        }
        if (clip.overrideAdditionalLights) { _additionalTotal += weight; if (weight > _additionalBest) { _additionalBest = weight; _additionalEnabled = clip.enableAdditionalLights; } }
        if (clip.overridePlanarShadow)
        {
            _planarTotal += weight; _planarDirection += clip.planarShadowDirection * weight; _planarStrength += Mathf.Max(0f, clip.planarShadowStrength) * weight;
            _planarFalloff += Mathf.Max(0f, clip.planarShadowFalloff) * weight; _planarRange += clip.planarShadowRange * weight; _planarPlaneHeight += clip.planarShadowPlaneHeight * weight;
            _planarCenter += clip.planarShadowGlobalCenter * weight; _planarColor += clip.planarShadowColor * weight;
            if (weight > _planarBest) { _planarBest = weight; _planarEnabled = clip.enablePlanarShadow; _planarDirectionMode = Mathf.Clamp(clip.planarShadowDirectionMode, 0, 2); }
        }
        if (clip.overrideOutline) { _outlineTotal += weight; if (weight > _outlineBest) { _outlineBest = weight; _outlineEnabled = clip.enableOutline; } }
        if (clip.overrideRimLight) { _rimTotal += weight; if (weight > _rimBest) { _rimBest = weight; _rimEnabled = clip.enableRimLight; } }
        if (clip.overrideFinalColorGradient)
        {
            _gradientTotal += weight; _gradientColor += clip.finalColorGradientColor * weight; _gradientMinY += clip.finalColorGradientMinY * weight; _gradientMaxY += clip.finalColorGradientMaxY * weight;
            if (weight > _gradientBest) { _gradientBest = weight; _gradientEnabled = clip.enableFinalColorGradient; }
        }
        if (clip.overrideSceneShadowMode) { _sceneShadowTotal += weight; if (weight > _sceneShadowBest) { _sceneShadowBest = weight; _sceneShadowMode = clip.sceneShadowMode; } }
        if (clip.overrideHighQualityShadowDirection && weight > _highQualityShadowDirectionBest)
        {
            _highQualityShadowDirectionBest = weight;
            Vector3 direction = clip.highQualityShadowDirectionFollowMainLightOverride
                && clip.overrideMainLight
                && clip.mainLightMode != CharacterRenderMainLightOverrideMode.Off
                    ? ResolveMainLightDirection(clip)
                    : clip.highQualityShadowDirection;
            _highQualityShadowDirection = direction.sqrMagnitude > Epsilon
                ? direction.normalized
                : Vector3.zero;
            _highQualityShadowDirectionMode = _highQualityShadowDirection == Vector3.zero ? 1 : 3;
        }
        bool clipEnablesPerObjectShadow = clip.overrideSceneShadowMode && UsesPerObjectShadow(clip.sceneShadowMode);
        if (clip.overrideHighQualityShadowStrength || clipEnablesPerObjectShadow)
        {
            _highQualityShadowStrengthTotal += weight;
            _perObjectShadowStrength += Mathf.Clamp01(clip.perObjectShadowStrength) * weight;
            _baseLitPerObjectShadowStrength += Mathf.Clamp01(clip.baseLitPerObjectShadowStrength) * weight;
            _specularShadowStrength += Mathf.Clamp01(clip.specularShadowStrength) * weight;
        }
        if (clip.overrideIndirectLight)
        {
            _indirectTotal += weight; _indirectIntensity += (clip.enableIndirectLight ? Mathf.Max(0f, clip.indirectIntensity) : 1f) * weight;
            _indirectTint += (clip.enableIndirectLight ? clip.indirectTintColor : Color.white) * weight;
            if (weight > _indirectBest) { _indirectBest = weight; _indirectEnabled = clip.enableIndirectLight; _overrideSceneAmbient = clip.overrideSceneAmbient; }
        }
    }

    private static Vector3 ResolveMainLightDirection(CharacterRenderTimelineClip clip)
    {
        return clip.mainLightMode == CharacterRenderMainLightOverrideMode.FollowCamera && Camera.main != null
            ? Camera.main.transform.rotation * Quaternion.Euler(clip.followCameraOffsetEuler) * Vector3.back
            : clip.mainLightDirection;
    }

    private static bool UsesPerObjectShadow(CharacterRenderSceneShadowMode mode)
    {
        return mode == CharacterRenderSceneShadowMode.POSOnly || mode == CharacterRenderSceneShadowMode.Both;
    }

    private static float Average(float value, float total, float fallback) => total > Epsilon ? value / total : fallback;
    private static Vector3 Average(Vector3 value, float total, Vector3 fallback) => total > Epsilon ? value / total : fallback;
    private static Color Average(Color value, float total, Color fallback) => total > Epsilon ? value / total : fallback;
    private static Vector3 ResolveDirection(Vector3 value, float total)
    {
        if (total <= Epsilon || value.sqrMagnitude <= Epsilon) return Vector3.zero;
        return (value / total).normalized;
    }
}

internal static class CharacterRenderTimelineHighQualityShadowOverride
{
    private const float Epsilon = 0.0001f;
    private static readonly int EnableWeightId =
        Shader.PropertyToID("_CharacterRenderTimelineHighQualityShadowEnableWeight");
    private static readonly int WeightId =
        Shader.PropertyToID("_CharacterRenderTimelineHighQualityShadowDirectionWeight");
    private static readonly int ModeId =
        Shader.PropertyToID("_CharacterRenderTimelineHighQualityShadowDirectionMode");
    private static readonly int DirectionId =
        Shader.PropertyToID("_CharacterRenderTimelineHighQualityShadowDirection");
    private static readonly int SharedBaseLitStrengthWeightId =
        Shader.PropertyToID("_CharacterRenderTimelineSharedBaseLitPerObjectShadowStrengthWeight");
    private static readonly int SharedBaseLitStrengthId =
        Shader.PropertyToID("_CharacterRenderTimelineSharedBaseLitPerObjectShadowStrength");
    private static readonly Dictionary<CharacterRenderTimelineMixer, Request> Requests =
        new Dictionary<CharacterRenderTimelineMixer, Request>();
    private static ulong s_NextRegistrationOrder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Requests.Clear();
        s_NextRegistrationOrder = 0;
        Shader.SetGlobalFloat(EnableWeightId, 0f);
        Shader.SetGlobalFloat(WeightId, 0f);
        Shader.SetGlobalFloat(ModeId, 0f);
        Shader.SetGlobalVector(DirectionId, Vector3.zero);
        Shader.SetGlobalFloat(SharedBaseLitStrengthWeightId, 0f);
        Shader.SetGlobalFloat(SharedBaseLitStrengthId, 1f);
    }

    public static void Set(
        CharacterRenderTimelineMixer owner,
        float enableWeight,
        float directionWeight,
        int mode,
        Vector3 direction,
        float strengthWeight,
        float baseLitStrength)
    {
        if (enableWeight <= Epsilon && directionWeight <= Epsilon && strengthWeight <= Epsilon)
        {
            Clear(owner);
            return;
        }

        if (!Requests.TryGetValue(owner, out Request request))
        {
            request.registrationOrder = ++s_NextRegistrationOrder;
        }

        request.enableWeight = Mathf.Clamp01(enableWeight);
        request.directionWeight = Mathf.Clamp01(directionWeight);
        request.mode = mode;
        request.direction = direction;
        request.strengthWeight = Mathf.Clamp01(strengthWeight);
        request.baseLitStrength = Mathf.Max(0f, baseLitStrength);
        Requests[owner] = request;
        ApplyWinner();
    }

    public static void Clear(CharacterRenderTimelineMixer owner)
    {
        if (Requests.Remove(owner))
        {
            ApplyWinner();
        }
    }

    private static void ApplyWinner()
    {
        bool hasWinner = false;
        Request winner = default;
        bool hasStrengthWinner = false;
        Request strengthWinner = default;
        float enableWeight = 0f;
        foreach (Request request in Requests.Values)
        {
            enableWeight = Mathf.Max(enableWeight, request.enableWeight);
            if (request.directionWeight > Epsilon &&
                (!hasWinner ||
                 request.directionWeight > winner.directionWeight + Epsilon ||
                 (Mathf.Abs(request.directionWeight - winner.directionWeight) <= Epsilon &&
                  request.registrationOrder < winner.registrationOrder)))
            {
                hasWinner = true;
                winner = request;
            }

            if (request.strengthWeight > Epsilon &&
                (!hasStrengthWinner ||
                 request.strengthWeight > strengthWinner.strengthWeight + Epsilon ||
                 (Mathf.Abs(request.strengthWeight - strengthWinner.strengthWeight) <= Epsilon &&
                  request.registrationOrder < strengthWinner.registrationOrder)))
            {
                hasStrengthWinner = true;
                strengthWinner = request;
            }
        }

        Shader.SetGlobalFloat(EnableWeightId, enableWeight);
        Shader.SetGlobalFloat(WeightId, hasWinner ? winner.directionWeight : 0f);
        Shader.SetGlobalFloat(ModeId, hasWinner ? winner.mode : 0f);
        Shader.SetGlobalVector(DirectionId, hasWinner ? winner.direction : Vector3.zero);
        Shader.SetGlobalFloat(
            SharedBaseLitStrengthWeightId,
            hasStrengthWinner ? strengthWinner.strengthWeight : 0f);
        Shader.SetGlobalFloat(
            SharedBaseLitStrengthId,
            hasStrengthWinner ? strengthWinner.baseLitStrength : 1f);
    }

    private struct Request
    {
        public float enableWeight;
        public float directionWeight;
        public int mode;
        public Vector3 direction;
        public float strengthWeight;
        public float baseLitStrength;
        public ulong registrationOrder;
    }
}

internal static class CharacterRenderTimelineProperties
{
    private static readonly int MainLightWeight = Shader.PropertyToID("_CharacterRenderTimelineMainLightWeight");
    private static readonly int MainLightDirection = Shader.PropertyToID("_CharacterRenderTimelineMainLightDirection");
    private static readonly int MainLightColor = Shader.PropertyToID("_CharacterRenderTimelineMainLightColor");
    private static readonly int MainLightStrength = Shader.PropertyToID("_CharacterRenderTimelineMainLightStrength");
    private static readonly int MainLightToggle = Shader.PropertyToID("_CharacterRenderTimelineMainLightToggle");
    private static readonly int MainLightFollowCameraXZ = Shader.PropertyToID("_CharacterRenderTimelineMainLightFollowCameraXZ");
    private static readonly int BackLightToggle = Shader.PropertyToID("_CharacterRenderTimelineBackLightToggle");
    private static readonly int VirtualLightWeight = Shader.PropertyToID("_CharacterRenderTimelineVirtualLightWeight");
    private static readonly int VirtualLightCameraDirection = Shader.PropertyToID("_CharacterRenderTimelineVirtualLightCameraDirection");
    private static readonly int VirtualLightMaskCameraDirection = Shader.PropertyToID("_CharacterRenderTimelineVirtualLightMaskCameraDirection");
    private static readonly int VirtualLightColor = Shader.PropertyToID("_CharacterRenderTimelineVirtualLightColor");
    private static readonly int VirtualLightIntensity = Shader.PropertyToID("_CharacterRenderTimelineVirtualLightIntensity");
    private static readonly int VirtualLightOffset = Shader.PropertyToID("_CharacterRenderTimelineVirtualLightOffset");
    private static readonly int AdditionalLightsWeight = Shader.PropertyToID("_CharacterRenderTimelineAdditionalLightsWeight");
    private static readonly int AdditionalLightsEnabled = Shader.PropertyToID("_CharacterRenderTimelineAdditionalLightsEnabled");
    private static readonly int PlanarWeight = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowWeight");
    private static readonly int PlanarToggle = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowToggle");
    private static readonly int PlanarDirectionMode = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowDirectionMode");
    private static readonly int PlanarDirection = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowDirection");
    private static readonly int PlanarStrength = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowStrength");
    private static readonly int PlanarParametersOverride = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowParametersOverride");
    private static readonly int PlanarFalloff = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowFalloff");
    private static readonly int PlanarRange = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowRange");
    private static readonly int PlanarPlaneHeight = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowPlaneHeight");
    private static readonly int PlanarCenter = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowGlobalCenter");
    private static readonly int PlanarColor = Shader.PropertyToID("_CharacterRenderTimelinePlanarShadowColor");
    private static readonly int OutlineWeight = Shader.PropertyToID("_CharacterRenderTimelineOutlineWeight");
    private static readonly int OutlineToggle = Shader.PropertyToID("_CharacterRenderTimelineOutlineToggle");
    private static readonly int SceneShadowWeight = Shader.PropertyToID("_CharacterRenderTimelineSceneShadowWeight");
    private static readonly int SceneShadowOverride = Shader.PropertyToID("_CharacterRenderTimelineSceneShadowModeOverride");
    private static readonly int SceneShadowMode = Shader.PropertyToID("_CharacterRenderTimelineSceneShadowMode");
    private static readonly int HighQualityShadowStrengthWeight = Shader.PropertyToID("_CharacterRenderTimelineHighQualityShadowStrengthWeight");
    private static readonly int PerObjectShadowStrength = Shader.PropertyToID("_CharacterRenderTimelinePerObjectShadowStrength");
    private static readonly int BaseLitPerObjectShadowStrength = Shader.PropertyToID("_CharacterRenderTimelineBaseLitPerObjectShadowStrength");
    private static readonly int SpecularShadowStrength = Shader.PropertyToID("_CharacterRenderTimelineSpecularShadowStrength");
    private static readonly int RimWeight = Shader.PropertyToID("_CharacterRenderTimelineRimLightWeight");
    private static readonly int RimToggle = Shader.PropertyToID("_CharacterRenderTimelineRimLightToggle");
    private static readonly int GradientWeight = Shader.PropertyToID("_CharacterRenderTimelineFinalGradientWeight");
    private static readonly int GradientToggle = Shader.PropertyToID("_CharacterRenderTimelineFinalGradientToggle");
    private static readonly int GradientParametersOverride = Shader.PropertyToID("_CharacterRenderTimelineFinalGradientParametersOverride");
    private static readonly int GradientColor = Shader.PropertyToID("_CharacterRenderTimelineFinalGradientColor");
    private static readonly int GradientMinY = Shader.PropertyToID("_CharacterRenderTimelineFinalGradientMinY");
    private static readonly int GradientMaxY = Shader.PropertyToID("_CharacterRenderTimelineFinalGradientMaxY");
    private static readonly int IndirectWeight = Shader.PropertyToID("_CharacterRenderTimelineIndirectLightWeight");
    private static readonly int OverrideSceneAmbient = Shader.PropertyToID("_CharacterRenderTimelineOverrideSceneAmbient");
    private static readonly int IndirectIntensity = Shader.PropertyToID("_CharacterRenderTimelineIndirectIntensity");
    private static readonly int IndirectTint = Shader.PropertyToID("_CharacterRenderTimelineIndirectTintColor");

    public static void Apply(MaterialPropertyBlock block, CharacterRenderTimelineBlendState state)
    {
        float mainWeight = state.MainWeight;
        block.SetFloat(MainLightWeight, mainWeight);
        if (mainWeight > 0f)
        {
            block.SetVector(MainLightDirection, state.MainDirection);
            block.SetColor(MainLightColor, state.MainColor);
            block.SetFloat(MainLightStrength, state.MainStrength);
            block.SetFloat(MainLightToggle, state.MainMode == CharacterRenderMainLightOverrideMode.Off ? 0f : 1f);
            block.SetFloat(MainLightFollowCameraXZ, state.MainMode == CharacterRenderMainLightOverrideMode.FollowCamera ? 1f : 0f);
            block.SetFloat(BackLightToggle, state.BackLight ? 1f : 0f);
        }

        float virtualWeight = state.VirtualWeight;
        block.SetFloat(VirtualLightWeight, virtualWeight);
        if (virtualWeight > 0f)
        {
            block.SetVector(VirtualLightCameraDirection, state.VirtualCameraDirection);
            block.SetVector(VirtualLightMaskCameraDirection, state.VirtualMaskDirection);
            block.SetColor(VirtualLightColor, state.VirtualColor);
            block.SetFloat(VirtualLightIntensity, state.VirtualIntensity);
            block.SetFloat(VirtualLightOffset, state.VirtualOffset);
        }

        SetBool(block, AdditionalLightsWeight, AdditionalLightsEnabled, state.AdditionalWeight, state.AdditionalEnabled);
        float planarWeight = state.PlanarWeight;
        block.SetFloat(PlanarWeight, planarWeight);
        if (planarWeight > 0f)
        {
            SetBool(block, PlanarWeight, PlanarToggle, planarWeight, state.PlanarEnabled);
            block.SetFloat(PlanarDirectionMode, state.PlanarDirectionMode);
            block.SetVector(PlanarDirection, state.PlanarDirection);
            block.SetFloat(PlanarStrength, state.PlanarStrength);
            block.SetFloat(PlanarParametersOverride, 1f);
            block.SetFloat(PlanarFalloff, state.PlanarFalloff);
            block.SetFloat(PlanarRange, state.PlanarRange);
            block.SetFloat(PlanarPlaneHeight, state.PlanarPlaneHeight);
            block.SetVector(PlanarCenter, state.PlanarCenter);
            block.SetColor(PlanarColor, state.PlanarColor);
        }

        SetBool(block, OutlineWeight, OutlineToggle, state.OutlineWeight, state.OutlineEnabled);
        SetBool(block, RimWeight, RimToggle, state.RimWeight, state.RimEnabled);
        float gradientWeight = state.GradientWeight;
        block.SetFloat(GradientWeight, gradientWeight);
        if (gradientWeight > 0f)
        {
            block.SetFloat(GradientToggle, state.GradientEnabled ? 1f : 0f);
            block.SetFloat(GradientParametersOverride, 1f);
            block.SetColor(GradientColor, state.GradientColor);
            block.SetFloat(GradientMinY, state.GradientMinY);
            block.SetFloat(GradientMaxY, state.GradientMaxY);
        }

        float sceneShadowWeight = state.SceneShadowWeight;
        block.SetFloat(SceneShadowWeight, sceneShadowWeight);
        if (sceneShadowWeight > 0f)
        {
            block.SetFloat(SceneShadowOverride, 1f);
            block.SetFloat(SceneShadowMode, (float)state.SceneShadowMode);
        }

        float highQualityShadowStrengthWeight = state.HighQualityShadowStrengthWeight;
        block.SetFloat(HighQualityShadowStrengthWeight, highQualityShadowStrengthWeight);
        if (highQualityShadowStrengthWeight > 0f)
        {
            block.SetFloat(PerObjectShadowStrength, state.PerObjectShadowStrength);
            block.SetFloat(BaseLitPerObjectShadowStrength, state.BaseLitPerObjectShadowStrength);
            block.SetFloat(SpecularShadowStrength, state.SpecularShadowStrength);
        }

        float indirectWeight = state.IndirectWeight;
        block.SetFloat(IndirectWeight, indirectWeight);
        if (indirectWeight > 0f)
        {
            block.SetFloat(OverrideSceneAmbient, state.OverrideSceneAmbient ? 1f : 0f);
            block.SetFloat(IndirectIntensity, state.IndirectIntensity);
            block.SetColor(IndirectTint, state.IndirectTint);
        }
    }

    private static void SetBool(MaterialPropertyBlock block, int weightId, int valueId, float weight, bool value)
    {
        block.SetFloat(weightId, weight);
        if (weight > 0f)
        {
            block.SetFloat(valueId, value ? 1f : 0f);
        }
    }
}
