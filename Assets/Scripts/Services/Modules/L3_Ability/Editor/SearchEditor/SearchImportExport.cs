#if UNITY_EDITOR
using System;
using RedDust.Shared.EditorUI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>Search JSON DTO。</summary>
    [Serializable]
    public class SearchEntry
    {
        public string searchType;
        public string name;
        public float range;
        public int maxTargets;
        public string targetFilter;
        public float angle;
        public bool requiresLineOfSight;
    }

    [Serializable]
    public class SearchExportFile
    {
        public string version = "1.0";
        public string description;
        public SearchEntry[] searches;
    }

    public static class SearchImporter
    {
        internal const string Root = "Assets/Data/Ability/Searches";

        public static string ExportToJson()
        {
            var export = new SearchExportFile
            {
                version = "1.0",
                description = "Search definitions",
            };

            var entries = new List<SearchEntry>();
            var guids = AssetDatabase.FindAssets("t:AbilitySearchSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var s = AssetDatabase.LoadAssetAtPath<AbilitySearchSO>(path);
                if (s == null) continue;

                var entry = new SearchEntry
                {
                    name = s.name,
                    searchType = s.searchType.ToString(),
                    range = s.range,
                    maxTargets = s.maxTargets,
                    targetFilter = s.targetFilter.ToString(),
                };

                if (s is ConeSearchSO cone)
                    entry.angle = cone.angle;
                if (s is RaySearchSO ray)
                    entry.requiresLineOfSight = ray.requiresLineOfSight;

                entries.Add(entry);
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            export.searches = entries.ToArray();
            return JsonUtility.ToJson(export, true);
        }

        public static (int created, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, skipped = 0;

            SearchExportFile file;
            try { file = JsonUtility.FromJson<SearchExportFile>(jsonText); }
            catch (Exception e) { errors.Add($"Parse failed: {e.Message}"); return (0, 0, errors); }
            if (file?.searches == null || file.searches.Length == 0)
            { errors.Add("Empty or invalid JSON."); return (0, 0, errors); }

            var validTypes = new HashSet<string> { "Cone", "RayLine", "Circle" };
            foreach (var entry in file.searches)
            {
                if (string.IsNullOrWhiteSpace(entry.name))
                { errors.Add("Skipping entry: empty name"); skipped++; continue; }
                if (!validTypes.Contains(entry.searchType))
                { errors.Add($"'{entry.name}': unknown type '{entry.searchType}'"); skipped++; continue; }

                var dir = Root;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var assetPath = Path.Combine(dir, $"{entry.name}.asset").Replace('\\', '/');
                var existing = AssetDatabase.LoadAssetAtPath<AbilitySearchSO>(assetPath);

                if (existing != null)
                {
                    if (existing.searchType.ToString() != entry.searchType)
                    { errors.Add($"'{entry.name}': type mismatch"); skipped++; continue; }
                    ApplyFields(existing, entry);
                    EditorUtility.SetDirty(existing);
                    skipped++;
                }
                else
                {
                    var instance = CreateInstance(entry);
                    instance.name = entry.name;
                    AssetDatabase.CreateAsset(instance, assetPath);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return (created, skipped, errors);
        }

        private static AbilitySearchSO CreateInstance(SearchEntry e) => e.searchType switch
        {
            "Cone" => ScriptableObject.CreateInstance<ConeSearchSO>(),
            "RayLine" => ScriptableObject.CreateInstance<RaySearchSO>(),
            "Circle" => ScriptableObject.CreateInstance<CircleSearchSO>(),
            _ => null,
        };

        private static void ApplyFields(AbilitySearchSO s, SearchEntry e)
        {
            s.range = e.range;
            s.maxTargets = e.maxTargets;
            if (Enum.TryParse<ETargetFilter>(e.targetFilter, out var tf)) s.targetFilter = tf;
            if (s is ConeSearchSO cone) cone.angle = e.angle;
            if (s is RaySearchSO ray) ray.requiresLineOfSight = e.requiresLineOfSight;
        }
    }

    /// <summary>Search Import/Export 窗口。参考 EffectImportWindow。</summary>
    public class SearchImportWindow : EditorWindow
    {
        private string _filePath;
        private string _preview = "";
        private string _result = "";

        [MenuItem("RedDust/Search Import-Export", priority = 21)]
        public static void Open() => GetWindow<SearchImportWindow>("Search Import-Export");

        private void OnGUI()
        {
            EditorImportExport.Draw(
                "Search Import-Export",
                "Assets/Data/Ability/Searches",
                "json",
                ref _filePath,
                ref _preview,
                ref _result,
                onImport: () => DoImport(),
                onExport: () => DoExport()
            );
        }

        private void DoImport()
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            { _result = "File not found."; return; }

            var (created, skipped, errors) = SearchImporter.ImportFromJson(File.ReadAllText(_filePath));
            _result = $"Created: {created}, Skipped (updated): {skipped}";
            if (errors.Count > 0) _result += "\n" + string.Join("\n", errors);
            _preview = "";
            Debug.Log($"[SearchImport] {_result}");
        }

        private void DoExport()
        {
            var path = EditorUtility.SaveFilePanel("Export Searches", "Assets/Data/Ability/Searches", "searches_export", "json");
            if (string.IsNullOrEmpty(path)) return;
            var json = SearchImporter.ExportToJson();
            File.WriteAllText(path, json);
            _result = $"Exported to {path}";
            Debug.Log($"[SearchImport] {_result}");
        }
    }
}
#endif
