using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// A Timeline clip that drives one or more material properties on renderers below the bound character root.
/// It intentionally has its own source file so Unity can serialize it through a dedicated MonoScript asset.
/// </summary>
[Serializable]
public sealed class CharacterMaterialTimelineClip : PlayableAsset, ITimelineClipAsset
{
    [Tooltip("A Clip can drive multiple renderer material properties. Existing single-property fields below remain as a backwards-compatible fallback.")]
    public List<CharacterMaterialTimelineParameter> parameters = new List<CharacterMaterialTimelineParameter>();

    [Tooltip("Relative path from the Track binding root to the target Renderer. Empty means the binding root itself.")]
    public string rendererPath;
    [Min(0)] public int materialIndex;
    public string propertyName;
    public CharacterMaterialTimelineValueType valueType;
    public float floatValue;
    public Vector4 vectorValue;
    public Color colorValue = Color.white;

    public ClipCaps clipCaps => ClipCaps.Blending;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<CharacterMaterialTimelineBehaviour> playable =
            ScriptPlayable<CharacterMaterialTimelineBehaviour>.Create(graph);
        playable.GetBehaviour().clip = this;
        return playable;
    }
}
