using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Animates a supported shader property on a renderer below the GameObject bound to this track.
/// Values are written through MaterialPropertyBlock, so neither shared materials nor material instances are modified.
/// </summary>
[TrackColor(0.95f, 0.52f, 0.22f)]
[TrackClipType(typeof(CharacterMaterialTimelineClip))]
[TrackBindingType(typeof(GameObject))]
public sealed class CharacterMaterialTimelineTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CharacterMaterialTimelineMixer>.Create(graph, inputCount);
    }
}

public enum CharacterMaterialTimelineValueType
{
    Float,
    Vector,
    Color,
}

[Serializable]
public sealed class CharacterMaterialTimelineParameter
{
    [Tooltip("Relative path from the Track binding root to the target Renderer. Empty means the binding root itself.")]
    public string rendererPath;
    [Min(0)] public int materialIndex;
    public string propertyName;
    public CharacterMaterialTimelineValueType valueType;
    public float floatValue;
    public Vector4 vectorValue;
    public Color colorValue = Color.white;
}

public sealed class CharacterMaterialTimelineBehaviour : PlayableBehaviour
{
    public CharacterMaterialTimelineClip clip;
}

public sealed class CharacterMaterialTimelineMixer : PlayableBehaviour
{
    private readonly Dictionary<string, Renderer> _renderersByPath = new Dictionary<string, Renderer>();
    private readonly Dictionary<string, BlendState> _blendStates = new Dictionary<string, BlendState>();
    private readonly Dictionary<string, PropertyTarget> _previousTargets = new Dictionary<string, PropertyTarget>();
    private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
    private GameObject _boundRoot;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        GameObject root = playerData as GameObject;
        if (root == null)
        {
            RestorePreviousValues();
            return;
        }

        if (_boundRoot != root)
        {
            RestorePreviousValues();
            CacheRenderers(root);
        }

        _blendStates.Clear();
        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            Playable input = playable.GetInput(i);
            if (!input.IsValid() || input.GetPlayableType() != typeof(CharacterMaterialTimelineBehaviour)) continue;

            CharacterMaterialTimelineBehaviour behaviour =
                ((ScriptPlayable<CharacterMaterialTimelineBehaviour>)input).GetBehaviour();
            if (behaviour != null && behaviour.clip != null)
            {
                AddClip(behaviour.clip, weight);
            }
        }

        ApplyBlendedValues();
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        RestorePreviousValues();
        _renderersByPath.Clear();
        _boundRoot = null;
    }

    private void CacheRenderers(GameObject root)
    {
        _boundRoot = root;
        _renderersByPath.Clear();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            _renderersByPath[GetRelativePath(root.transform, renderer.transform)] = renderer;
        }
    }

    private void AddClip(CharacterMaterialTimelineClip clip, float weight)
    {
        if (clip.parameters != null && clip.parameters.Count > 0)
        {
            for (int i = 0; i < clip.parameters.Count; i++)
            {
                AddParameter(clip.parameters[i], weight);
            }
            return;
        }

        AddParameter(new CharacterMaterialTimelineParameter
        {
            rendererPath = clip.rendererPath,
            materialIndex = clip.materialIndex,
            propertyName = clip.propertyName,
            valueType = clip.valueType,
            floatValue = clip.floatValue,
            vectorValue = clip.vectorValue,
            colorValue = clip.colorValue,
        }, weight);
    }

    private void AddParameter(CharacterMaterialTimelineParameter parameter, float weight)
    {
        if (parameter == null || string.IsNullOrWhiteSpace(parameter.propertyName)) return;
        if (!_renderersByPath.TryGetValue(parameter.rendererPath ?? string.Empty, out Renderer renderer) || renderer == null) return;

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || parameter.materialIndex < 0 || parameter.materialIndex >= materials.Length) return;
        Material material = materials[parameter.materialIndex];
        if (material == null || !material.HasProperty(parameter.propertyName)) return;

        int propertyId = Shader.PropertyToID(parameter.propertyName);
        PropertyTarget target = new PropertyTarget(renderer, parameter.materialIndex, material, propertyId, parameter.valueType);
        string key = target.GetKey();
        if (!_blendStates.TryGetValue(key, out BlendState state))
        {
            state = new BlendState(target);
            _blendStates.Add(key, state);
        }

        state.Add(parameter, weight);
    }

    private void ApplyBlendedValues()
    {
        foreach (KeyValuePair<string, PropertyTarget> previous in _previousTargets)
        {
            if (!_blendStates.ContainsKey(previous.Key))
            {
                ApplyMaterialValue(previous.Value);
            }
        }

        _previousTargets.Clear();
        foreach (KeyValuePair<string, BlendState> pair in _blendStates)
        {
            BlendState state = pair.Value;
            ApplyBlendedValue(state);
            _previousTargets.Add(pair.Key, state.target);
        }
    }

    private void RestorePreviousValues()
    {
        foreach (KeyValuePair<string, PropertyTarget> previous in _previousTargets)
        {
            ApplyMaterialValue(previous.Value);
        }

        _previousTargets.Clear();
        _blendStates.Clear();
    }

    private void ApplyBlendedValue(BlendState state)
    {
        PropertyTarget target = state.target;
        if (!target.IsValid()) return;

        target.renderer.GetPropertyBlock(_propertyBlock, target.materialIndex);
        float weight = Mathf.Clamp01(state.totalWeight);
        switch (target.valueType)
        {
            case CharacterMaterialTimelineValueType.Float:
                _propertyBlock.SetFloat(target.propertyId, Mathf.Lerp(target.material.GetFloat(target.propertyId), state.floatValue / state.totalWeight, weight));
                break;
            case CharacterMaterialTimelineValueType.Vector:
                _propertyBlock.SetVector(target.propertyId, Vector4.Lerp(target.material.GetVector(target.propertyId), state.vectorValue / state.totalWeight, weight));
                break;
            case CharacterMaterialTimelineValueType.Color:
                _propertyBlock.SetColor(target.propertyId, Color.Lerp(target.material.GetColor(target.propertyId), state.colorValue / state.totalWeight, weight));
                break;
        }

        target.renderer.SetPropertyBlock(_propertyBlock, target.materialIndex);
    }

    private void ApplyMaterialValue(PropertyTarget target)
    {
        if (!target.IsValid()) return;
        target.renderer.GetPropertyBlock(_propertyBlock, target.materialIndex);
        switch (target.valueType)
        {
            case CharacterMaterialTimelineValueType.Float: _propertyBlock.SetFloat(target.propertyId, target.material.GetFloat(target.propertyId)); break;
            case CharacterMaterialTimelineValueType.Vector: _propertyBlock.SetVector(target.propertyId, target.material.GetVector(target.propertyId)); break;
            case CharacterMaterialTimelineValueType.Color: _propertyBlock.SetColor(target.propertyId, target.material.GetColor(target.propertyId)); break;
        }
        target.renderer.SetPropertyBlock(_propertyBlock, target.materialIndex);
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (root == target) return string.Empty;
        var names = new Stack<string>();
        for (Transform current = target; current != null && current != root; current = current.parent) names.Push(current.name);
        return string.Join("/", names.ToArray());
    }

    private sealed class BlendState
    {
        public readonly PropertyTarget target;
        public float totalWeight;
        public float floatValue;
        public Vector4 vectorValue;
        public Color colorValue;

        public BlendState(PropertyTarget target) { this.target = target; }
        public void Add(CharacterMaterialTimelineParameter parameter, float weight)
        {
            totalWeight += weight;
            switch (target.valueType)
            {
                case CharacterMaterialTimelineValueType.Float: floatValue += parameter.floatValue * weight; break;
                case CharacterMaterialTimelineValueType.Vector: vectorValue += parameter.vectorValue * weight; break;
                case CharacterMaterialTimelineValueType.Color: colorValue += parameter.colorValue * weight; break;
            }
        }
    }

    private sealed class PropertyTarget
    {
        public readonly Renderer renderer;
        public readonly int materialIndex;
        public readonly Material material;
        public readonly int propertyId;
        public readonly CharacterMaterialTimelineValueType valueType;
        public PropertyTarget(Renderer renderer, int materialIndex, Material material, int propertyId, CharacterMaterialTimelineValueType valueType)
        {
            this.renderer = renderer; this.materialIndex = materialIndex; this.material = material; this.propertyId = propertyId; this.valueType = valueType;
        }
        public bool IsValid() => renderer != null && material != null;
        public string GetKey() => renderer.GetInstanceID() + ":" + materialIndex + ":" + propertyId + ":" + (int)valueType;
    }
}
