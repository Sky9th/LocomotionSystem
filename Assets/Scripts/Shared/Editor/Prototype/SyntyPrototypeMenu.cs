#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared
{
    /// Window > Synty Prototype — dockable prefab browser for PolygonPrototype assets
    public static class SyntyPrototypeMenu
{
    private static List<CategoryData> s_cache;

    [MenuItem("RedDust/Synty Prototype Browser")]
    private static void Browse() => SyntyPrototypeBrowser.Open(GetCategories());

    public static List<CategoryData> GetCategories()
    {
        if (s_cache != null) return s_cache;
        s_cache = ScanAllFolders();
        return s_cache;
    }

    public static void InstantiateByPath(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"Failed to load prefab: {path}");
            return;
        }

        var parent = Selection.activeGameObject != null
            ? Selection.activeGameObject.transform
            : null;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, worldPositionStays: false);
        instance.transform.localPosition = Vector3.zero;
        instance.name = Path.GetFileNameWithoutExtension(path);

        Undo.RegisterCreatedObjectUndo(instance, "Create " + instance.name);
        Selection.activeGameObject = instance;
        EditorGUIUtility.PingObject(instance);
    }

    // ---- scanning ----

    private static readonly string BasePath = "Assets/Art/PolygonPrototype/Prefabs";

    private static readonly string[] ScanDirs =
    {
        "Buildings/Simple",
        "Buildings/Polygon",
        "Props",
        "Primitives",
        "Primitives/Polygon",
        "Generic",
        "Vehicle",
    };

    private static readonly HashSet<string> SkipTypes = new()
    {
        "Bat", "BoostPad", "C4", "Knife", "Pistol", "Rifle", "Sword"
    };

    private static List<CategoryData> ScanAllFolders()
    {
        var catMap = new Dictionary<string, CategoryData>();

        foreach (var sub in ScanDirs)
        {
            var dir = Path.Combine(BasePath, sub);
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.prefab"))
            {
                var entry = CreateEntry(file);
                if (entry == null) continue;

                if (!catMap.ContainsKey(entry.category))
                    catMap[entry.category] = new CategoryData { name = entry.category };
                catMap[entry.category].prefabs.Add(entry);
            }
        }

        var order = new[]
        {
            "Walls", "Floors", "Stairs", "Ramps", "Roofs",
            "Railings", "Columns", "Blocks", "Doors & Windows", "Ladders",
            "Primitives", "Props", "Environment", "Vehicles"
        };

        var result = new List<CategoryData>();
        foreach (var cat in order)
        {
            if (catMap.TryGetValue(cat, out var data))
            {
                data.prefabs.Sort((a, b) => a.displayName.CompareTo(b.displayName));
                result.Add(data);
            }
        }

        foreach (var kv in catMap)
            if (!result.Exists(r => r.name == kv.Key))
                result.Add(kv.Value);

        return result;
    }

    private static PrefabEntry CreateEntry(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var category = DetermineCategory(fileName);
        if (string.IsNullOrEmpty(category)) return null;

        return new PrefabEntry
        {
            path = filePath,
            category = category,
            displayName = FormatDisplayName(fileName),
            isPolygon = fileName.EndsWith("P")
        };
    }

    private static string DetermineCategory(string fileName)
    {
        string rest = null;

        if (fileName.StartsWith("SM_Buildings_"))
        {
            rest = fileName.Substring("SM_Buildings_".Length);
            var type = ExtractType(rest);
            if (type.StartsWith("Wall")) return "Walls";
            if (type.StartsWith("Floor")) return "Floors";
            if (type.StartsWith("Stairs")) return "Stairs";
            if (type.StartsWith("Ramp")) return "Ramps";
            if (type.StartsWith("Roof")) return "Roofs";
            if (type.StartsWith("Rail")) return "Railings";
            if (type.StartsWith("Column")) return "Columns";
            if (type.StartsWith("Block")) return "Blocks";
            if (type.StartsWith("Door") || type.StartsWith("Window")) return "Doors & Windows";
            return null;
        }

        if (fileName.StartsWith("SM_Prop_"))
        {
            rest = fileName.Substring("SM_Prop_".Length);
            var type = ExtractType(rest);
            if (SkipTypes.Contains(type)) return null;
            if (type == "Ladder") return "Ladders";
            if (type.StartsWith("Tree") || type == "Bush") return "Environment";
            return "Props";
        }

        if (fileName.StartsWith("SM_Primitive_"))
            return "Primitives";

        if (fileName.StartsWith("SM_Generic_"))
            return "Environment";

        if (fileName.StartsWith("SM_Veh_"))
            return "Vehicles";

        if (fileName.StartsWith("SM_Switch_"))
            return "Props";

        if (fileName.StartsWith("SM_FX_"))
            return null;

        return null;
    }

    private static string ExtractType(string nameWithoutPrefix)
    {
        var parts = nameWithoutPrefix.Split('_');
        var typeParts = new List<string>();
        var dimRegex = new Regex(@"^\d+x\d+$");

        foreach (var part in parts)
        {
            if (dimRegex.IsMatch(part)) break;
            if (Regex.IsMatch(part, @"^\d+$")) break;
            typeParts.Add(part);
        }

        return string.Join("_", typeParts);
    }

    private static string FormatDisplayName(string fileName)
    {
        string[] prefixes =
        {
            "SM_Buildings_", "SM_Prop_", "SM_Primitive_",
            "SM_Generic_", "SM_Veh_", "SM_Switch_", "SM_FX_"
        };

        var rest = fileName;
        foreach (var p in prefixes)
        {
            if (rest.StartsWith(p)) { rest = rest.Substring(p.Length); break; }
        }

        rest = Regex.Replace(rest, @"_\d+P?$", "");
        rest = Regex.Replace(rest, @"^Ramp_(\d+)_", m => $"Ramp {m.Groups[1].Value}° ");

        return rest.Replace("_", " ");
    }

    // ---- data types ----

    public class CategoryData
    {
        public string name;
        public readonly List<PrefabEntry> prefabs = new();
    }

    public class PrefabEntry
    {
        public string path;
        public string category;
        public string displayName;
        public bool isPolygon;
    }
}
}
#endif
