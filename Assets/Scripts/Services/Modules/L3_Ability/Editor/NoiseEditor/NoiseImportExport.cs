#if UNITY_EDITOR
using System;
using RedDust.Shared.EditorUI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Core;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    [Serializable]
    public class NoiseEntry
    {
        public string name;
        public string noiseType;  // FullTag string
        public float level;
        public float decayRadius;
    }

    [Serializable]
    public class NoiseExportFile
    {
        public string version = "1.0";
        public string description;
        public NoiseEntry[] noises;
    }

    public static class NoiseImporter
    {
        internal const string Root = "Assets/Data/Ability/Noises";

        public static string ExportToJson()
        {
            var entries = new List<NoiseEntry>();
            var guids = AssetDatabase.FindAssets("t:NoiseEventSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var n = AssetDatabase.LoadAssetAtPath<NoiseEventSO>(path);
                if (n == null) continue;

                entries.Add(new NoiseEntry
                {
                    name = n.name,
                    noiseType = n.noiseType?.FullTag,
                    level = n.level,
                    decayRadius = n.decayRadius,
                });
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var export = new NoiseExportFile
            {
                version = "1.0",
                description = "Noise definitions",
                noises = entries.ToArray(),
            };
            return JsonUtility.ToJson(export, true);
        }

        public static (int created, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, skipped = 0;

            NoiseExportFile file;
            try { file = JsonUtility.FromJson<NoiseExportFile>(jsonText); }
            catch (Exception e) { errors.Add($"Parse failed: {e.Message}"); return (0, 0, errors); }
            if (file?.noises == null || file.noises.Length == 0)
            { errors.Add("Empty or invalid JSON."); return (0, 0, errors); }

            // Build tag lookup by FullTag
            var tagByFullTag = new Dictionary<string, GameplayTagDefinitionSO>();
            var tagGuids = AssetDatabase.FindAssets("t:GameplayTagDefinitionSO");
            foreach (var tg in tagGuids)
            {
                var tp = AssetDatabase.GUIDToAssetPath(tg);
                var t = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(tp);
                if (t != null && !string.IsNullOrEmpty(t.FullTag))
                    tagByFullTag[t.FullTag] = t;
            }

            foreach (var entry in file.noises)
            {
                if (string.IsNullOrWhiteSpace(entry.name))
                { errors.Add("Skipping entry: empty name"); skipped++; continue; }

                var dir = Root;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var assetPath = Path.Combine(dir, $"{entry.name}.asset").Replace('\\', '/');
                var existing = AssetDatabase.LoadAssetAtPath<NoiseEventSO>(assetPath);

                if (existing != null)
                {
                    ApplyFields(existing, entry, tagByFullTag);
                    EditorUtility.SetDirty(existing);
                    skipped++;
                }
                else
                {
                    var instance = ScriptableObject.CreateInstance<NoiseEventSO>();
                    instance.name = entry.name;
                    ApplyFields(instance, entry, tagByFullTag);
                    AssetDatabase.CreateAsset(instance, assetPath);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return (created, skipped, errors);
        }

        private static void ApplyFields(NoiseEventSO n, NoiseEntry e,
            Dictionary<string, GameplayTagDefinitionSO> tagByFullTag)
        {
            if (!string.IsNullOrEmpty(e.noiseType))
            {
                if (tagByFullTag.TryGetValue(e.noiseType, out var tag))
                    n.noiseType = tag;
            }
            n.level = e.level;
            n.decayRadius = e.decayRadius;
        }
    }

    /// <summary>Noise Import/Export 窗口。使用共享 EditorImportExport 组件。</summary>
    public class NoiseImportWindow : EditorWindow
    {
        private string _filePath;
        private string _previewText;
        private (int created, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Noise Import-Export", priority = 23)]
        public static void Open()
        {
            var window = GetWindow<NoiseImportWindow>("Noise Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Noise Import-Export",
                subtitle: "L3_Ability · JSON ↔ .asset",
                defaultDir: "Assets/Data/Ability/Noises",
                fileExtension: "json",
                defaultFileName: "noises_export",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path =>
                {
                    var (created, skipped, errors) = NoiseImporter.ImportFromJson(File.ReadAllText(path));
                    return (created, skipped, errors);
                },
                onExport: path => File.WriteAllText(path, NoiseImporter.ExportToJson())
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            NoiseExportFile preview;
            try { preview = JsonUtility.FromJson<NoiseExportFile>(File.ReadAllText(filePath)); }
            catch { return null; }
            if (preview?.noises == null || preview.noises.Length == 0) return null;

            return $"<b>{preview.noises.Length}</b> noises\n" +
                   $"v{preview.version} · {preview.description ?? "-"}";
        }
    }
}
#endif
