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
    [Serializable]
    public class ActivationEntry
    {
        public string name;
        public string activationType;
        public float maxChargeTime;
        public bool autoReleaseAtFullCharge;
        public string animationLayer;
        public float animationSpeed = 1f;
        public bool rootMotion;
        public float windupDuration;
        public float fireWindowDuration;
        public bool canCancelWindup;
        public bool canCancelRecovery;
    }

    [Serializable]
    public class ActivationExportFile
    {
        public string version = "1.0";
        public string description;
        public ActivationEntry[] activations;
    }

    public static class ActivationImporter
    {
        internal const string Root = "Assets/Data/Ability/Activations";

        public static string ExportToJson()
        {
            var entries = new List<ActivationEntry>();
            var guids = AssetDatabase.FindAssets("t:AbilityActivationSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<AbilityActivationSO>(path);
                if (a == null) continue;

                entries.Add(new ActivationEntry
                {
                    name = a.name,
                    activationType = a.activationType.ToString(),
                    maxChargeTime = a.maxChargeTime,
                    autoReleaseAtFullCharge = a.autoReleaseAtFullCharge,
                    animationLayer = a.animationLayer.ToString(),
                    animationSpeed = a.animationSpeed,
                    rootMotion = a.rootMotion,
                    windupDuration = a.windupDuration,
                    fireWindowDuration = a.fireWindowDuration,
                    canCancelWindup = a.canCancelWindup,
                    canCancelRecovery = a.canCancelRecovery,
                });
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var export = new ActivationExportFile
            {
                version = "1.0",
                description = "Activation definitions",
                activations = entries.ToArray(),
            };
            return JsonUtility.ToJson(export, true);
        }

        public static (int created, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, skipped = 0;

            ActivationExportFile file;
            try { file = JsonUtility.FromJson<ActivationExportFile>(jsonText); }
            catch (Exception e) { errors.Add($"Parse failed: {e.Message}"); return (0, 0, errors); }
            if (file?.activations == null || file.activations.Length == 0)
            { errors.Add("Empty or invalid JSON."); return (0, 0, errors); }

            foreach (var entry in file.activations)
            {
                if (string.IsNullOrWhiteSpace(entry.name))
                { errors.Add("Skipping entry: empty name"); skipped++; continue; }

                var dir = Root;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var assetPath = Path.Combine(dir, $"{entry.name}.asset").Replace('\\', '/');
                var existing = AssetDatabase.LoadAssetAtPath<AbilityActivationSO>(assetPath);

                if (existing != null)
                {
                    ApplyFields(existing, entry);
                    EditorUtility.SetDirty(existing);
                    skipped++;
                }
                else
                {
                    var instance = ScriptableObject.CreateInstance<AbilityActivationSO>();
                    instance.name = entry.name;
                    ApplyFields(instance, entry);
                    AssetDatabase.CreateAsset(instance, assetPath);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return (created, skipped, errors);
        }

        private static void ApplyFields(AbilityActivationSO a, ActivationEntry e)
        {
            if (Enum.TryParse<EActivationType>(e.activationType, out var at)) a.activationType = at;
            a.maxChargeTime = e.maxChargeTime;
            a.autoReleaseAtFullCharge = e.autoReleaseAtFullCharge;
            if (Enum.TryParse<EAbilityAnimationLayer>(e.animationLayer, out var al)) a.animationLayer = al;
            a.animationSpeed = e.animationSpeed;
            a.rootMotion = e.rootMotion;
            a.windupDuration = e.windupDuration;
            a.fireWindowDuration = e.fireWindowDuration;
            a.canCancelWindup = e.canCancelWindup;
            a.canCancelRecovery = e.canCancelRecovery;
        }
    }

    public class ActivationImportWindow : EditorWindow
    {
        private string _filePath;
        private string _preview = "";
        private string _result = "";

        [MenuItem("RedDust/Activation Import-Export", priority = 22)]
        public static void Open() => GetWindow<ActivationImportWindow>("Activation Import-Export");

        private void OnGUI()
        {
            EditorImportExport.Draw(
                "Activation Import-Export",
                "Assets/Data/Ability/Activations",
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

            var (created, skipped, errors) = ActivationImporter.ImportFromJson(File.ReadAllText(_filePath));
            _result = $"Created: {created}, Skipped (updated): {skipped}";
            if (errors.Count > 0) _result += "\n" + string.Join("\n", errors);
            _preview = "";
            Debug.Log($"[ActivationImport] {_result}");
        }

        private void DoExport()
        {
            var path = EditorUtility.SaveFilePanel("Export Activations", "Assets/Data/Ability/Activations", "activations_export", "json");
            if (string.IsNullOrEmpty(path)) return;
            var json = ActivationImporter.ExportToJson();
            File.WriteAllText(path, json);
            _result = $"Exported to {path}";
            Debug.Log($"[ActivationImport] {_result}");
        }
    }
}
#endif
