using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RedDust.Properties
{
    /// <summary>
    /// Resolved final property values for an entity instance.
    /// Pure data — no runtime Tick, Modifier, or events.
    /// Internal storage is per-type native buckets. No runtime string→float parsing.
    /// </summary>
    public class ResolvedPropertyBag
    {
        private readonly Dictionary<string, float> _floats = new();
        private readonly Dictionary<string, int> _ints = new();
        private readonly Dictionary<string, bool> _bools = new();
        private readonly Dictionary<string, string> _strings = new();
        private readonly Dictionary<string, string> _tags = new();
        private readonly Dictionary<string, string[]> _tagLists = new();
        private readonly Dictionary<string, UnityEngine.Object> _assetRefs = new();
        private readonly Dictionary<string, UnityEngine.Object[]> _assetRefLists = new();
        private readonly HashSet<string> _keys = new(); // O(1) single lookup for TryGet

        // ---- type-safe getters ----

        public float GetFloat(string path) => _floats.TryGetValue(path, out var v) ? v : 0f;
        public int GetInt(string path) => _ints.TryGetValue(path, out var v) ? v : 0;
        public bool GetBool(string path) => _bools.TryGetValue(path, out var v) && v;
        public string GetString(string path) => _strings.TryGetValue(path, out var v) ? v : null;
        public string GetTag(string path) => _tags.TryGetValue(path, out var v) ? v : null;
        public string[] GetTagList(string path) => _tagLists.TryGetValue(path, out var v) ? v : null;
        public T GetAsset<T>(string path) where T : UnityEngine.Object => _assetRefs.TryGetValue(path, out var v) ? v as T : null;
        public T[] GetAssetList<T>(string path) where T : UnityEngine.Object => _assetRefLists.TryGetValue(path, out var v) ? v as T[] : null;

        public bool TryGet(string path) => _keys.Contains(path);

        // ---- build from structure + overrides ----

        /// <summary>
        /// Build a ResolvedPropertyBag from a resolved structure (Path → Def) and overrides JSON.
        /// Validates types and ranges. Overrides win; missing overrides fall back to Def.Default.
        /// </summary>
        public static ResolvedPropertyBag Build(
            Dictionary<string, PropertyDefSO> structure,
            string overridesJson)
        {
            var bag = new ResolvedPropertyBag();
            var overrides = ParseOverrides(overridesJson);

            foreach (var (path, def) in structure)
            {
                if (def == null) continue;
                bag._keys.Add(path);

                if (overrides.TryGetValue(path, out var rawValue))
                {
                    bag.SetFromRaw(path, def, rawValue);
                }
                else
                {
                    bag.SetDefault(path, def);
                }
            }

            // Warn about overrides that don't match any property in the structure
            foreach (var (path, _) in overrides)
            {
                if (!structure.ContainsKey(path))
                    Debug.LogWarning($"[PropertyBag] Override key '{path}' not found in template structure. Skipped.");
            }

            return bag;
        }

        // ---- internal helpers ----

        [Serializable]
        private class OverrideEntry { public string Path; public string Value; }

        [Serializable]
        private class OverrideContainer { public List<OverrideEntry> Overrides = new(); }

        private static Dictionary<string, string> ParseOverrides(string overridesJson)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(overridesJson)) return result;

            try
            {
                var container = JsonUtility.FromJson<OverrideContainer>(overridesJson);
                if (container?.Overrides != null)
                {
                    foreach (var entry in container.Overrides)
                    {
                        if (!string.IsNullOrEmpty(entry.Path))
                            result[entry.Path] = entry.Value;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PropertyBag] Failed to parse overridesJson: {e.Message}");
            }

            return result;
        }

        private void SetFromRaw(string path, PropertyDefSO def, string rawValue)
        {
            switch (def.Type)
            {
                case PropertyType.Float:
                    if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    {
                        f = Mathf.Clamp(f, def.Min, def.Max);
                        _floats[path] = f;
                    }
                    else
                        Debug.LogWarning($"[PropertyBag] Cannot parse '{rawValue}' as Float for '{path}'");
                    break;

                case PropertyType.Int:
                    if (int.TryParse(rawValue, out var i))
                    {
                        i = Mathf.Clamp(i, def.MinInt, def.MaxInt);
                        _ints[path] = i;
                    }
                    else
                        Debug.LogWarning($"[PropertyBag] Cannot parse '{rawValue}' as Int for '{path}'");
                    break;

                case PropertyType.Bool:
                    if (bool.TryParse(rawValue, out var b))
                        _bools[path] = b;
                    else
                        Debug.LogWarning($"[PropertyBag] Cannot parse '{rawValue}' as Bool for '{path}'");
                    break;

                case PropertyType.String:
                    _strings[path] = rawValue;
                    break;

                case PropertyType.GameplayTag:
                    _tags[path] = rawValue;
                    break;

                case PropertyType.GameplayTagList:
                    _tagLists[path] = ParseTagArray(rawValue);
                    break;

                case PropertyType.AssetRef:
                    _assetRefs[path] = ResolveAsset(rawValue, def.AssetTypeConstraint, path);
                    break;

                case PropertyType.AssetRefList:
                    _assetRefLists[path] = ResolveAssetList(rawValue, def.AssetTypeConstraint, path);
                    break;
            }
        }

        private void SetDefault(string path, PropertyDefSO def)
        {
            switch (def.Type)
            {
                case PropertyType.Float:
                    _floats[path] = def.DefaultFloat;
                    break;
                case PropertyType.Int:
                    _ints[path] = def.DefaultInt;
                    break;
                case PropertyType.Bool:
                    _bools[path] = def.DefaultBool;
                    break;
                case PropertyType.String:
                    _strings[path] = def.DefaultString;
                    break;
                case PropertyType.GameplayTag:
                case PropertyType.GameplayTagList:
                    // No default — empty
                    break;
                case PropertyType.AssetRef:
                    _assetRefs[path] = ResolveAsset(def.DefaultAssetGUID, def.AssetTypeConstraint, path);
                    break;
                case PropertyType.AssetRefList:
                    // No default — empty array
                    break;
            }
        }

        private static string[] ParseTagArray(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return Array.Empty<string>();

            // Expects JSON array format: ["Tag.A","Tag.B"]
            try
            {
                var wrapper = JsonUtility.FromJson<TagListWrapper>($"{{\"Items\":{rawValue}}}");
                return wrapper?.Items ?? Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        [Serializable]
        private class TagListWrapper { public string[] Items; }

        private static UnityEngine.Object ResolveAsset(string guid, string typeConstraint, string path)
        {
            if (string.IsNullOrEmpty(guid)) return null;

#if UNITY_EDITOR
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return null;

            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj != null && !string.IsNullOrEmpty(typeConstraint))
            {
                var expectedType = System.Type.GetType(typeConstraint);
                if (expectedType != null && !expectedType.IsInstanceOfType(obj))
                {
                    Debug.LogWarning($"[PropertyBag] Asset type mismatch for '{path}': expected {typeConstraint}, got {obj.GetType()}");
                    return null;
                }
            }
            return obj;
#else
            return null; // Runtime: serialized in build asset
#endif
        }

        private static UnityEngine.Object[] ResolveAssetList(string rawValue, string typeConstraint, string path)
        {
            if (string.IsNullOrEmpty(rawValue)) return Array.Empty<UnityEngine.Object>();

            // Expects JSON array format: ["guid://...","guid://..."]
            try
            {
                var wrapper = JsonUtility.FromJson<GuidListWrapper>($"{{\"Items\":{rawValue}}}");
                if (wrapper?.Items == null) return Array.Empty<UnityEngine.Object>();

                var result = new List<UnityEngine.Object>();
                foreach (var guid in wrapper.Items)
                {
                    var obj = ResolveAsset(guid, typeConstraint, path);
                    if (obj != null) result.Add(obj);
                }
                return result.ToArray();
            }
            catch
            {
                return Array.Empty<UnityEngine.Object>();
            }
        }

        [Serializable]
        private class GuidListWrapper { public string[] Items; }
    }
}
