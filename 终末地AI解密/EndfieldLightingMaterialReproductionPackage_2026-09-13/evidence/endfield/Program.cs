using AS = AnimeStudio;
using System.Reflection;
using System.Text.RegularExpressions;

var envOnly = args.Length > 0 && args[0] == "--env-only";
var files = envOnly ? args.Skip(1).ToArray() : args;
if (files.Length == 0) { Console.Error.WriteLine("usage: EndfieldInspector bundle..."); return 1; }
var mgr = new AS.AssetsManager { Game = AS.GameManager.GetGame(AS.GameType.ArknightsEndfieldCB3), Silent = true, SkipProcess = false };
mgr.LoadFiles(files);
foreach (var sf in mgr.assetsFileList)
{
    var gos = sf.Objects.OfType<AS.GameObject>().ToDictionary(x => x.m_PathID, x => x.Name);
    Console.WriteLine($"BUNDLE {sf.fileName} objects={sf.Objects.Count}");
    for (var externalIndex = 0; externalIndex < sf.m_Externals.Count; externalIndex++)
    {
        var external = sf.m_Externals[externalIndex];
        Console.WriteLine($"EXTERNAL\tFileID={externalIndex + 1}\tFile={external.fileName}\tPath={external.pathName}\tGuid={external.guid}\tType={external.type}");
    }
    foreach (var obj in sf.Objects)
    {
        var type = obj.GetType();
        var name = type.Name;
        // AnimeStudio does not materialize every Unity built-in object as a CLR
        // class (e.g. Light/ReflectionProbe are commonly exposed as
        // AnimeStudio.Object). Use the serialized ClassIDType as the source of
        // truth instead of relying on the CLR type name.
        var nativeLighting = obj.type is AS.ClassIDType.Light
            or AS.ClassIDType.ReflectionProbe
            or AS.ClassIDType.LightProbeGroup
            or AS.ClassIDType.RenderSettings;
        var reflectionTexture = obj.type == AS.ClassIDType.Cubemap;
        if (!nativeLighting && !reflectionTexture && name is not ("Material" or "Shader" or "MonoBehaviour")) continue;
        string dump = "";
        try { dump = obj.Dump() ?? ""; } catch { }
        if (envOnly && !(name == "MonoBehaviour" && dump.Contains("HGLightConfig lightConfig", StringComparison.Ordinal)))
            continue;
        var go = ResolveGameObjectName(obj, dump, gos);
        if (obj is AS.Component comp)
        {
            var f = type.GetField("m_GameObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var p = f?.GetValue(comp);
            var path = p?.GetType().GetField("m_PathID")?.GetValue(p);
            if (path is long id && gos.TryGetValue(id, out var n)) go = n;
        }
        var interesting = string.Join(" | ", dump.Split('\n').Where(x => x.Contains("m_Type") || x.Contains("m_Intensity") || x.Contains("m_Range") || x.Contains("m_SpotAngle") || x.Contains("m_Color") || x.Contains("m_Cookie") || x.Contains("m_Shadow") || x.Contains("m_Baked") || x.Contains("m_Mode") || x.Contains("m_Name") || x.Contains("m_Enabled") || x.Contains("m_UseFog") || x.Contains("m_Ambient") || x.Contains("m_DefaultReflection") || x.Contains("bloom", StringComparison.OrdinalIgnoreCase) || x.Contains("outline", StringComparison.OrdinalIgnoreCase) || x.Contains("rim", StringComparison.OrdinalIgnoreCase) || x.Contains("volume", StringComparison.OrdinalIgnoreCase) || x.Contains("post", StringComparison.OrdinalIgnoreCase)).Take(80));
        if (nativeLighting)
        {
            var typed = TryGetTypedSummary(obj);
            if (!string.IsNullOrEmpty(typed)) interesting = string.IsNullOrEmpty(interesting) ? typed : interesting + " | " + typed;
        }
        Console.WriteLine($"{obj.type}\tCLR={name}\tGO={go}\tID={obj.m_PathID}\t{interesting}");
        if (nativeLighting || reflectionTexture)
            Console.WriteLine($"FULL_NATIVE\tTYPE={obj.type}\tGO={go}\tID={obj.m_PathID}\n{dump}");
        if (name == "MonoBehaviour" &&
            (envOnly ||
             go.Contains("light", StringComparison.OrdinalIgnoreCase) ||
             dump.Contains("m_lightNPR", StringComparison.Ordinal) ||
             dump.Contains("charAutoRimEnable", StringComparison.Ordinal) ||
             dump.Contains("characterBloomControl", StringComparison.Ordinal) ||
             dump.Contains("enableVolumetricFog", StringComparison.Ordinal) ||
             dump.Contains("postExposure", StringComparison.Ordinal)))
        {
            Console.WriteLine($"FULL_MONO\tGO={go}\tID={obj.m_PathID}\n{dump}");
        }
    }
}
return 0;

static string ResolveGameObjectName(AS.Object obj, string dump, IReadOnlyDictionary<long, string> gos)
{
    if (string.IsNullOrEmpty(dump)) return "";
    // Generic built-in objects are represented by AnimeStudio.Object, so they
    // do not expose Component.m_GameObject. Their serialized dump still has
    // the PPtr<GameObject> path id; recover it and map to the GameObject table.
    var m = Regex.Match(dump,
        @"PPtr<[^>]*GameObject>\s+m_GameObject[\s\S]{0,256}?SInt64\s+m_PathID\s*=\s*(-?\d+)",
        RegexOptions.CultureInvariant);
    if (m.Success && long.TryParse(m.Groups[1].Value, out var id) && gos.TryGetValue(id, out var name))
        return name;
    // ToType() reliably exposes the PPtr for built-in objects even when the
    // human-readable Dump() omits the common Light fields.
    try
    {
        var typed = obj.ToType();
        if (typed["m_GameObject"] is System.Collections.Specialized.OrderedDictionary pptr &&
            pptr["m_PathID"] is long path && gos.TryGetValue(path, out var typedName))
            return typedName;
    }
    catch { }
    return "";
}

static string TryGetTypedSummary(AS.Object obj)
{
    try
    {
        var typed = obj.ToType();
        var keys = new[] { "m_Type", "m_Shape", "m_Color", "m_Intensity", "m_SpecularIntensity", "m_Range", "m_SpotAngle", "m_InnerSpotAngle", "m_Shadows", "m_Cookie", "m_RenderMode", "m_BounceIntensity", "m_IsSunSourceLight", "m_ShadowOnly", "m_LightPriority", "m_Enabled" };
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (System.Collections.DictionaryEntry e in typed)
            if (e.Key is string key && keys.Contains(key, StringComparer.Ordinal)) values[key] = e.Value;
        return string.Join(" | ", keys.Where(values.ContainsKey).Select(k => $"{k}={FormatTypedValue(obj, k, values[k])}"));
    }
    catch { return ""; }
}

static string FormatTypedValue(AS.Object obj, string key, object? value)
{
    if (value is null) return "null";
    // `m_Type` is a LightType enum only on native Light objects. ReflectionProbe
    // and other built-ins also expose an integer field with the same name, but
    // its meaning is different (for example ReflectionProbe.Type=0 is not Spot).
    if (key == "m_Type" && obj.type == AS.ClassIDType.Light && value is int lightType)
        return lightType switch
        {
            0 => "Spot(0)",
            1 => "Directional(1)",
            2 => "Point(2)",
            3 => "Area(3)",
            4 => "Rectangle(4)",
            5 => "Disc(5)",
            6 => "Pyramid(6)",
            7 => "Box(7)",
            _ => lightType.ToString()
        };
    if (value is System.Collections.Specialized.OrderedDictionary dict)
    {
        var pairs = new List<string>();
        foreach (System.Collections.DictionaryEntry e in dict)
        {
            if (pairs.Count >= 8) break;
            pairs.Add($"{e.Key}:{e.Value}");
        }
        return "{" + string.Join(",", pairs) + "}";
    }
    return value.ToString() ?? "";
}
