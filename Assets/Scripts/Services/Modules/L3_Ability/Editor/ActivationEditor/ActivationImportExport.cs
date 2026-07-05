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
        /// <summary>AnimationClip 引用。格式 "{FBX_GUID}|{ClipName}"，导入时按 GUID 加载 FBX 后按 name 匹配子资产。</summary>
        public string animationClip;
        public string animationLayer;
        public float animationSpeed = 1f;
        public bool rootMotion;
        public float windupDuration;
        public float fireWindowDuration;
        public float recoveryDuration;
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

        /// <summary>FBX path → clips 缓存，避免同一 FBX 重复 LoadAllAssetsAtPath。</summary>
        private static readonly Dictionary<string, AnimationClip[]> _fbxClipCache = new();

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
                    animationClip = ClipToJson(a.animationClip),
                    animationLayer = a.animationLayer.ToString(),
                    animationSpeed = a.animationSpeed,
                    rootMotion = a.rootMotion,
                    windupDuration = a.windupDuration,
                    fireWindowDuration = a.fireWindowDuration,
                    recoveryDuration = a.recoveryDuration,
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

        public static (int created, int updated, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, updated = 0, skipped = 0;

            ActivationExportFile file;
            try { file = JsonUtility.FromJson<ActivationExportFile>(jsonText); }
            catch (Exception e) { errors.Add($"Parse failed: {e.Message}"); return (0, 0, 0, errors); }
            if (file?.activations == null || file.activations.Length == 0)
            { errors.Add("Empty or invalid JSON."); return (0, 0, 0, errors); }

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
                    updated++;
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
            return (created, updated, skipped, errors);
        }

        /// <summary>AnimationClip → "{FBX_GUID}|{ClipName}"</summary>
        private static string ClipToJson(AnimationClip clip)
        {
            if (clip == null) return null;
            var path = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(path)) return null;
            var guid = AssetDatabase.AssetPathToGUID(path);
            return $"{guid}|{clip.name}";
        }

        /// <summary>"{FBX_GUID}|{ClipName}" → AnimationClip（GUID 固定 32 位 hex）</summary>
        private static AnimationClip ClipFromJson(string refStr)
        {
            if (string.IsNullOrEmpty(refStr)) return null;
            if (refStr.Length < 34 || refStr[32] != '|')
            {
                Debug.LogWarning($"[ActivationImporter] Invalid animationClip format: '{refStr}' — expected \"{{32-char GUID}}|{{clip name}}\"");
                return null;
            }
            var guid = refStr.Substring(0, 32);
            var clipName = refStr.Substring(33);
            var fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(fbxPath))
            {
                Debug.LogWarning($"[ActivationImporter] animationClip FBX not found: guid={guid}, clip={clipName}");
                return null;
            }
            if (!_fbxClipCache.TryGetValue(fbxPath, out var clips))
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                var clipList = new List<AnimationClip>();
                foreach (var a in allAssets)
                    if (a is AnimationClip ac2) clipList.Add(ac2);
                clips = clipList.ToArray();
                _fbxClipCache[fbxPath] = clips;
            }
            foreach (var ac in clips)
            {
                if (ac.name == clipName)
                    return ac;
            }
            Debug.LogWarning($"[ActivationImporter] animationClip '{clipName}' not found in FBX: {fbxPath}");
            return null;
        }

        private static void ApplyFields(AbilityActivationSO a, ActivationEntry e)
        {
            if (Enum.TryParse<EActivationType>(e.activationType, out var at)) a.activationType = at;
            a.maxChargeTime = e.maxChargeTime;
            a.autoReleaseAtFullCharge = e.autoReleaseAtFullCharge;
            if (!string.IsNullOrEmpty(e.animationClip))
                a.animationClip = ClipFromJson(e.animationClip);
            else
                a.animationClip = null;
            if (Enum.TryParse<EAbilityAnimationLayer>(e.animationLayer, out var al)) a.animationLayer = al;
            a.animationSpeed = e.animationSpeed;
            a.rootMotion = e.rootMotion;
            a.windupDuration = e.windupDuration;
            a.fireWindowDuration = e.fireWindowDuration;
            a.recoveryDuration = e.recoveryDuration;
            a.canCancelWindup = e.canCancelWindup;
            a.canCancelRecovery = e.canCancelRecovery;
        }
    }

    public class ActivationImportWindow : EditorWindow
    {
        private string _filePath = "Assets/Data/Ability/Activations/activations_all.json";
        private string _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Activation Import-Export", priority = 22)]
        public static void Open()
        {
            var window = GetWindow<ActivationImportWindow>("Activation Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Activation Import-Export",
                subtitle: "L3_Ability · JSON ↔ .asset",
                defaultDir: "Assets/Data/Ability/Activations",
                fileExtension: "json",
                defaultFileName: "activations_export",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path =>
                {
                    return ActivationImporter.ImportFromJson(File.ReadAllText(path));
                },
                onExport: path => File.WriteAllText(path, ActivationImporter.ExportToJson())
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            ActivationExportFile preview;
            try { preview = JsonUtility.FromJson<ActivationExportFile>(File.ReadAllText(filePath)); }
            catch { return null; }
            if (preview?.activations == null || preview.activations.Length == 0) return null;

            int total = preview.activations.Length;
            int instant = 0, charged = 0, channel = 0;
            foreach (var a in preview.activations)
            {
                switch (a.activationType) { case "Instant": instant++; break; case "Charged": charged++; break; case "Channel": channel++; break; }
            }

            return $"<b>{total}</b> activations (<color=#66CC66>{instant} Instant</color> · <color=#66B266>{charged} Charged</color> · <color=#4C7EFF>{channel} Channel</color>)\n" +
                   $"v{preview.version} · {preview.description ?? "-"}";
        }
    }
}
#endif
