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

    /// <summary>Search Import/Export 窗口。使用共享 EditorImportExport 组件。</summary>
    public class SearchImportWindow : EditorWindow
    {
        private string _filePath;
        private string _previewText;
        private (int created, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Search Import-Export", priority = 23)]
        public static void Open()
        {
            var window = GetWindow<SearchImportWindow>("Search Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Search Import-Export",
                subtitle: "L3_Ability · JSON ↔ .asset",
                defaultDir: "Assets/Data/Ability/Searches",
                fileExtension: "json",
                defaultFileName: "searches_export",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path =>
                {
                    var (created, skipped, errors) = SearchImporter.ImportFromJson(File.ReadAllText(path));
                    return (created, skipped, errors);
                },
                onExport: path => File.WriteAllText(path, SearchImporter.ExportToJson())
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            SearchExportFile preview;
            try { preview = JsonUtility.FromJson<SearchExportFile>(File.ReadAllText(filePath)); }
            catch { return null; }
            if (preview?.searches == null || preview.searches.Length == 0) return null;

            int total = preview.searches.Length;
            int cone = 0, ray = 0, circle = 0;
            foreach (var s in preview.searches)
            {
                switch (s.searchType) { case "Cone": cone++; break; case "RayLine": ray++; break; case "Circle": circle++; break; }
            }

            return $"<b>{total}</b> searches (<color=#66CC66>{cone} Cone</color> · <color=#66B266>{ray} Ray</color> · <color=#4C7EFF>{circle} Circle</color>)\n" +
                   $"v{preview.version} · {preview.description ?? "-"}";
        }
    }
}
#endif
