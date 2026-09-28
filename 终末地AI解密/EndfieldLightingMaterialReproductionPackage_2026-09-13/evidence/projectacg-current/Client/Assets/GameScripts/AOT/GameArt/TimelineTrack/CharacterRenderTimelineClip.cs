using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Serialized Timeline data for character render overrides.
/// This type intentionally has its own source file so Unity can resolve the
/// Clip's MonoScript and display it as a normal Timeline playable asset.
/// </summary>
[Serializable]
public sealed class CharacterRenderTimelineClip : PlayableAsset, ITimelineClipAsset
{
    [Header("主光")]
    public bool overrideMainLight = true;
    public CharacterRenderMainLightOverrideMode mainLightMode = CharacterRenderMainLightOverrideMode.Custom;
    public Vector3 mainLightDirection = new Vector3(0f, 1f, 0f);
    public Vector3 followCameraOffsetEuler;
    public Color mainLightColor = Color.white;
    [Min(0f)] public float mainLightStrength = 1f;
    public bool enableBackLight;

    [Header("虚拟补光")]
    public bool overrideVirtualLight;
    public Vector3 virtualLightCameraDirection = new Vector3(0f, 0f, 1f);
    public Vector3 virtualLightMaskCameraDirection = new Vector3(0f, 0f, 1f);
    public Color virtualLightColor = Color.white;
    [Min(0f)] public float virtualLightIntensity = 0.2f;
    [Range(-1f, 1f)] public float virtualLightOffset;
    public bool overrideAdditionalLights;
    public bool enableAdditionalLights = true;

    [Header("平面阴影")]
    public bool overridePlanarShadow;
    public bool enablePlanarShadow = true;
    [Tooltip("0=材质方向，1=固定方向，2=跟随本 Clip 主光")]
    [Range(0, 2)] public int planarShadowDirectionMode;
    public Vector3 planarShadowDirection = new Vector3(0f, 1f, 0f);
    [Min(0f)] public float planarShadowStrength = 1f;
    [Min(0f)] public float planarShadowFalloff = 1f;
    public float planarShadowRange;
    public float planarShadowPlaneHeight;
    public Vector3 planarShadowGlobalCenter;
    public Color planarShadowColor = Color.black;

    [Header("描边、Rim 与高度渐变")]
    public bool overrideOutline;
    public bool enableOutline = true;
    public bool overrideRimLight;
    public bool enableRimLight = true;
    public bool overrideFinalColorGradient;
    public bool enableFinalColorGradient = true;
    public Color finalColorGradientColor = Color.white;
    public float finalColorGradientMinY;
    public float finalColorGradientMaxY = 1f;

    [Header("场景阴影与环境光")]
    [Tooltip("只覆盖该角色最终采样的阴影合成模式；不改变 PerObjectShadow RenderFeature 的共享投影方向。")]
    public bool overrideSceneShadowMode;
    public CharacterRenderSceneShadowMode sceneShadowMode = CharacterRenderSceneShadowMode.UnityOnly;
    [Tooltip("覆盖当前相机下所有 PerObjectShadow 的投影方向。方向为零时跟随场景主平行光；PerObjectShadowVolume 的显式方向优先。")]
    public bool overrideHighQualityShadowDirection;
    [Tooltip("开启后复用本 Clip 的主光覆盖方向；仅在本 Clip 已覆盖主光且主光模式不是 Off 时生效。")]
    public bool highQualityShadowDirectionFollowMainLightOverride;
    public Vector3 highQualityShadowDirection = Vector3.zero;
    [Tooltip("覆盖 POS 阴影强度。Chara V2 与高光参数只作用于绑定角色；BaseLit 是场景接收材质，因此同一相机内共享。选择 POSOnly/Both 时会自动使用这些非零默认值。")]
    public bool overrideHighQualityShadowStrength;
    [Range(0f, 1f)] public float perObjectShadowStrength = 1f;
    [Range(0f, 1f)] public float baseLitPerObjectShadowStrength = 1f;
    [Range(0f, 1f)] public float specularShadowStrength = 1f;
    public bool overrideIndirectLight;
    public bool enableIndirectLight = true;
    [Tooltip("启用角色环境光时，忽略场景球谐颜色，改用间接漫反射颜色统一着色。")]
    public bool overrideSceneAmbient;
    [Min(0f)] public float indirectIntensity = 1f;
    public Color indirectTintColor = Color.white;

    public ClipCaps clipCaps => ClipCaps.Blending;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<CharacterRenderTimelineBehaviour> playable =
            ScriptPlayable<CharacterRenderTimelineBehaviour>.Create(graph);
        playable.GetBehaviour().clip = this;
        return playable;
    }
}
