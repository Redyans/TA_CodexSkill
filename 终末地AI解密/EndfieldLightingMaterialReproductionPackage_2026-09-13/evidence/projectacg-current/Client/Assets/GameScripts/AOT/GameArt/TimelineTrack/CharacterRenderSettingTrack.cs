using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// A character rendering track with independently blended child layers.
/// Bind only the root track to the character GameObject. Each child layer accepts
/// either material-property clips or render-override clips, but not both.
/// </summary>
[TrackColor(0.20f, 0.78f, 0.66f)]
[TrackClipType(typeof(CharacterMaterialTimelineClip))]
[TrackClipType(typeof(CharacterRenderTimelineClip))]
[TrackBindingType(typeof(GameObject))]
public sealed class CharacterRenderSettingTrack : TrackAsset, ILayerable
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        switch (GetLayerContent())
        {
            case CharacterRenderSettingLayerContent.Material:
                return ScriptPlayable<CharacterMaterialTimelineMixer>.Create(graph, inputCount);
            case CharacterRenderSettingLayerContent.Render:
                return ScriptPlayable<CharacterRenderTimelineMixer>.Create(graph, inputCount);
            case CharacterRenderSettingLayerContent.Mixed:
                return ScriptPlayable<CharacterRenderSettingMixedLayerMixer>.Create(graph, inputCount);
            default:
                return Playable.Create(graph, inputCount);
        }
    }

    Playable ILayerable.CreateLayerMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        // Layer-specific mixers own all value blending. The root only relays the layers.
        return Playable.Create(graph, inputCount);
    }

    public CharacterRenderSettingLayerContent GetLayerContent()
    {
        bool hasMaterial = false;
        bool hasRender = false;

        foreach (TimelineClip clip in GetClips())
        {
            if (clip.asset is CharacterMaterialTimelineClip)
            {
                hasMaterial = true;
            }
            else if (clip.asset is CharacterRenderTimelineClip)
            {
                hasRender = true;
            }
        }

        if (hasMaterial && hasRender)
        {
            return CharacterRenderSettingLayerContent.Mixed;
        }

        if (hasMaterial)
        {
            return CharacterRenderSettingLayerContent.Material;
        }

        return hasRender ? CharacterRenderSettingLayerContent.Render : CharacterRenderSettingLayerContent.Empty;
    }
}

public enum CharacterRenderSettingLayerContent
{
    Empty,
    Material,
    Render,
    Mixed,
}

internal sealed class CharacterRenderSettingMixedLayerMixer : PlayableBehaviour
{
    private bool _reported;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (_reported)
        {
            return;
        }

        _reported = true;
        Debug.LogError(
            "Character RenderSetting Track layer contains both Character Material Timeline Clip and Character Render Timeline Clip. " +
            "Put each clip type on a separate layer so it can use its own mixer.");
    }
}
