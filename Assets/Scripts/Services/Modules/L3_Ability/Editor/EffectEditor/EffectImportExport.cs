#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Core;
using RedDust.Properties;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// Effect JSON 导入/导出工具。
    /// 支持所有 EffectSO 子类：Damage, Impact, Execute, Cost。
    /// 仿 StatImporter 五阶段模式。
    /// </summary>
    public static class EffectImporter
    {
        internal const string EffectsRoot = "Assets/Data/Ability/Effects";

        // ═══════════════════════════════════════════════════
        // DTO
        // ═══════════════════════════════════════════════════
        [System.Serializable]
        public class EffectEntry
        {
            public string effectType;  // "Damage" | "Impact" | "Execute" | "Cost"
            public string name;        // asset name (without .asset)
            public string description;  // designer-readable text
            public string directory;   // relative to EffectsRoot, e.g. "Damage/Fire"
            public string effectTag;   // FullTag string, nullable
            public float duration;
            public bool stackable;
            public int maxStacks = 1;
            public string[] applicationBlockedTags; // FullTag strings, nullable

            // Damage
            public float baseValue;
            public float modAdd;
            public float modMult = 1f;
            public int priority;

            // Impact
            public float staggerValue;
            public float knockbackForce;
            public string knockbackDir;

            // Execute
            public float hpThreshold;

            // Cost
            public string defId;
            public float amount;
        }

        [System.Serializable]
        public class EffectExportFile
        {
            public string version = "1.0";
            public string description;
            public EffectEntry[] effects;
        }

        // ═══════════════════════════════════════════════════
        // Export
        // ═══════════════════════════════════════════════════
        public static string ExportToJson()
        {
            var export = new EffectExportFile
            {
                version = "1.0",
                description = "Exported from Unity Editor",
            };

            var entries = new List<EffectEntry>();
            var guids = AssetDatabase.FindAssets("t:EffectSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var effect = AssetDatabase.LoadAssetAtPath<EffectSO>(path);
                if (effect == null) continue;

                var entry = new EffectEntry
                {
                    name = effect.name,
                    description = effect.description,
                    effectTag = effect.effectTag?.FullTag,
                    duration = effect.duration,
                    stackable = effect.stackable,
                    maxStacks = effect.maxStacks,
                    applicationBlockedTags = effect.applicationBlockedTags?
                        .Select(t => t?.FullTag).Where(t => t != null).ToArray(),
                };

                // directory relative to EffectsRoot
                var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir) && dir.StartsWith(EffectsRoot + "/"))
                    entry.directory = dir.Substring(EffectsRoot.Length + 1);
                else if (!string.IsNullOrEmpty(dir) && dir != EffectsRoot)
                    entry.directory = dir;

                switch (effect)
                {
                    case DamageEffectSO d:
                        entry.effectType = "Damage";
                        entry.baseValue = d.baseValue;
                        entry.modAdd = d.modAdd;
                        entry.modMult = d.modMult;
                        entry.priority = d.priority;
                        break;
                    case ImpactEffectSO i:
                        entry.effectType = "Impact";
                        entry.staggerValue = i.staggerValue;
                        entry.knockbackForce = i.knockbackForce;
                        entry.knockbackDir = i.knockbackDir.ToString();
                        break;
                    case ExecuteEffectSO x:
                        entry.effectType = "Execute";
                        entry.hpThreshold = x.hpThreshold;
                        break;
                    case CostEffectSO c:
                        entry.effectType = "Cost";
                        entry.defId = c.def?.Id;
                        entry.amount = c.amount;
                        break;
                    default:
                        continue; // unknown type, skip
                }

                entries.Add(entry);
            }

            // sort: effectType then name
            entries.Sort((a, b) =>
            {
                var tCmp = string.CompareOrdinal(a.effectType, b.effectType);
                if (tCmp != 0) return tCmp;
                return string.CompareOrdinal(a.name, b.name);
            });

            export.effects = entries.ToArray();
            return JsonUtility.ToJson(export, true);
        }

        public static void ExportToFile(string jsonPath)
        {
            var json = ExportToJson();
            var dir = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(jsonPath, json);
            Debug.Log($"[EffectImporter] Exported {jsonPath}");
        }

        // ═══════════════════════════════════════════════════
        // Import
        // ═══════════════════════════════════════════════════
        public static (int created, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, skipped = 0;

            AssetDatabase.Refresh();

            // ── Phase 1: Deserialize ──
            EffectExportFile importFile;
            try
            {
                importFile = JsonUtility.FromJson<EffectExportFile>(jsonText);
            }
            catch (Exception e)
            {
                errors.Add($"JSON parse failed: {e.Message}");
                return (0, 0, errors);
            }
            if (importFile?.effects == null || importFile.effects.Length == 0)
            {
                errors.Add("JSON is empty or has no effects array.");
                return (0, 0, errors);
            }

            // ── Phase 2: Validate ──
            var valid = new List<EffectEntry>();
            var validTypes = new HashSet<string> { "Damage", "Impact", "Execute", "Cost" };
            foreach (var entry in importFile.effects)
            {
                if (string.IsNullOrWhiteSpace(entry.name))
                { errors.Add("Skipping entry: name is empty"); skipped++; continue; }
                if (string.IsNullOrWhiteSpace(entry.effectType)
                    || !validTypes.Contains(entry.effectType))
                { errors.Add($"Skipping '{entry.name}': unknown effectType '{entry.effectType}'"); skipped++; continue; }
                valid.Add(entry);
            }

            // ── Phase 3: Resolve references ──
            // Build GameplayTagDefinitionSO lookup by FullTag
            var tagByFullTag = new Dictionary<string, GameplayTagDefinitionSO>();
            var tagGuids = AssetDatabase.FindAssets("t:GameplayTagDefinitionSO");
            foreach (var tg in tagGuids)
            {
                var tp = AssetDatabase.GUIDToAssetPath(tg);
                var t = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(tp);
                if (t != null && !string.IsNullOrEmpty(t.FullTag))
                    tagByFullTag[t.FullTag] = t;
            }

            // Build PropertyDefSO lookup by Id
            var defById = new Dictionary<string, PropertyDefSO>();
            var defGuids = AssetDatabase.FindAssets("t:PropertyDefSO");
            foreach (var dg in defGuids)
            {
                var dp = AssetDatabase.GUIDToAssetPath(dg);
                var d = AssetDatabase.LoadAssetAtPath<PropertyDefSO>(dp);
                if (d != null && !string.IsNullOrEmpty(d.Id))
                    defById[d.Id] = d;
            }

            // Resolve per entry
            var resolved = new List<(EffectEntry entry, GameplayTagDefinitionSO effectTag,
                GameplayTagDefinitionSO[] blockedTags, PropertyDefSO def)>();
            foreach (var entry in valid)
            {
                // effectTag
                GameplayTagDefinitionSO resolvedTag = null;
                if (!string.IsNullOrEmpty(entry.effectTag))
                {
                    if (!tagByFullTag.TryGetValue(entry.effectTag, out resolvedTag))
                        errors.Add($"'{entry.name}': effectTag '{entry.effectTag}' not found in project");
                }

                // applicationBlockedTags
                var resolvedBlocked = new List<GameplayTagDefinitionSO>();
                if (entry.applicationBlockedTags != null)
                {
                    foreach (var bt in entry.applicationBlockedTags)
                    {
                        if (string.IsNullOrEmpty(bt)) continue;
                        if (tagByFullTag.TryGetValue(bt, out var btso))
                            resolvedBlocked.Add(btso);
                        else
                            errors.Add($"'{entry.name}': blockedTag '{bt}' not found in project");
                    }
                }

                // def (Cost only)
                PropertyDefSO resolvedStat = null;
                if (entry.effectType == "Cost" && !string.IsNullOrEmpty(entry.defId))
                {
                    if (!defById.TryGetValue(entry.defId, out resolvedStat))
                        errors.Add($"'{entry.name}': defId '{entry.defId}' not found in project");
                }

                resolved.Add((entry, resolvedTag, resolvedBlocked.ToArray(), resolvedStat));
            }

            // ── Phase 4: Create/Update assets ──
            foreach (var (entry, effTag, blockedTags, def) in resolved)
            {
                var dirName = string.IsNullOrWhiteSpace(entry.directory) ? "" : entry.directory;
                var assetDir = string.IsNullOrEmpty(dirName)
                    ? EffectsRoot
                    : Path.Combine(EffectsRoot, dirName).Replace('\\', '/');
                var assetPath = Path.Combine(assetDir, $"{entry.name}.asset").Replace('\\', '/');

                // check existing
                var existing = AssetDatabase.LoadAssetAtPath<EffectSO>(assetPath);
                if (existing != null)
                {
                    // update in place (only if type matches)
                    if (EffectTypeString(existing) != entry.effectType)
                    {
                        errors.Add($"'{entry.name}': exists as {EffectTypeString(existing)}, cannot overwrite with {entry.effectType}");
                        skipped++;
                        continue;
                    }
                    ApplyFields(existing, entry, effTag, blockedTags, def);
                    EditorUtility.SetDirty(existing);
                    skipped++;
                    continue;
                }

                // check duplicate name at different path
                var allEffectGuids = AssetDatabase.FindAssets("t:EffectSO");
                bool dup = false;
                foreach (var eg in allEffectGuids)
                {
                    var ep = AssetDatabase.GUIDToAssetPath(eg);
                    if (ep == assetPath) continue;
                    var ee = AssetDatabase.LoadAssetAtPath<EffectSO>(ep);
                    if (ee != null && ee.name == entry.name)
                    {
                        errors.Add($"'{entry.name}': duplicate name — asset already exists at '{ep}'");
                        dup = true;
                        break;
                    }
                }
                if (dup) { skipped++; continue; }

                // create new
                if (!Directory.Exists(assetDir))
                    Directory.CreateDirectory(assetDir);

                EffectSO instance = entry.effectType switch
                {
                    "Damage" => ScriptableObject.CreateInstance<DamageEffectSO>(),
                    "Impact" => ScriptableObject.CreateInstance<ImpactEffectSO>(),
                    "Execute" => ScriptableObject.CreateInstance<ExecuteEffectSO>(),
                    "Cost" => ScriptableObject.CreateInstance<CostEffectSO>(),
                    _ => null,
                };
                if (instance == null) { errors.Add($"'{entry.name}': unknown type"); skipped++; continue; }

                instance.name = entry.name;
                ApplyFields(instance, entry, effTag, blockedTags, def);
                AssetDatabase.CreateAsset(instance, assetPath);
                created++;
                Debug.Log($"[EffectImporter] Created: {assetPath}");
            }

            // ── Phase 5: Persist ──
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EffectImporter] Done: created={created} skipped={skipped} errors={errors.Count}");
            return (created, skipped, errors);
        }

        public static (int created, int skipped, List<string> errors) ImportFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                return (0, 0, new List<string> { $"File not found: {jsonPath}" });
            return ImportFromJson(File.ReadAllText(jsonPath));
        }

        // ═══════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════
        private static string EffectTypeString(EffectSO e) => e switch
        {
            DamageEffectSO => "Damage",
            ImpactEffectSO => "Impact",
            ExecuteEffectSO => "Execute",
            CostEffectSO => "Cost",
            _ => "Unknown",
        };

        private static void ApplyFields(EffectSO instance, EffectEntry entry,
            GameplayTagDefinitionSO effTag, GameplayTagDefinitionSO[] blockedTags,
            PropertyDefSO def)
        {
            instance.effectTag = effTag;
            instance.description = entry.description;
            instance.duration = entry.duration;
            instance.stackable = entry.stackable;
            instance.maxStacks = entry.maxStacks;
            instance.applicationBlockedTags = blockedTags.Length > 0 ? blockedTags : null;

            switch (instance)
            {
                case DamageEffectSO d:
                    d.baseValue = entry.baseValue;
                    d.modAdd = entry.modAdd;
                    d.modMult = entry.modMult;
                    d.priority = entry.priority;
                    break;
                case ImpactEffectSO i:
                    i.staggerValue = entry.staggerValue;
                    i.knockbackForce = entry.knockbackForce;
                    if (!string.IsNullOrEmpty(entry.knockbackDir)
                        && Enum.TryParse<EKnockbackDirection>(entry.knockbackDir, out var kd))
                        i.knockbackDir = kd;
                    break;
                case ExecuteEffectSO x:
                    x.hpThreshold = Mathf.Clamp01(entry.hpThreshold);
                    break;
                case CostEffectSO c:
                    c.def = def;
                    c.amount = entry.amount;
                    break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    // Import Window
    // ═══════════════════════════════════════════════════════
    public class EffectImportWindow : EditorWindow
    {
        private const float Pad = 6f;

        private string _jsonPath = "Assets/Data/Ability/Effects/effects_all.json";
        private int _lastCreated, _lastSkipped;
        private List<string> _lastErrors = new();

        [MenuItem("RedDust/Effect Import-Export", priority = 2)]
        public static void ShowWindow()
        {
            var window = GetWindow<EffectImportWindow>("Effect Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            DrawHeader();
            GUILayout.Space(Pad);
            DrawFilePicker();
            GUILayout.Space(Pad);
            DrawPreview();
            GUILayout.Space(Pad);
            DrawButtons();
            GUILayout.Space(Pad);
            DrawResult();

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.LabelField("Effect Import-Export", EditorStyles.largeLabel);
            var sub = new GUIStyle(EditorStyles.label)
                { alignment = TextAnchor.MiddleRight, normal = { textColor = Color.gray } };
            EditorGUILayout.LabelField("L3_Ability · JSON ↔ .asset", sub, GUILayout.Width(230));
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        private void DrawFilePicker()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("JSON File", EditorStyles.boldLabel);
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            _jsonPath = EditorGUILayout.TextField(_jsonPath);
            if (GUILayout.Button("…", GUILayout.Width(28), GUILayout.Height(18)))
                PickFile();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        private void DrawPreview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (File.Exists(_jsonPath))
            {
                EffectImporter.EffectExportFile preview = null;
                try
                {
                    preview = JsonUtility.FromJson<EffectImporter.EffectExportFile>(
                        File.ReadAllText(_jsonPath));
                }
                catch { }

                if (preview?.effects != null && preview.effects.Length > 0)
                {
                    int total = preview.effects.Length;
                    int dmg = 0, imp = 0, exe = 0, cost = 0;
                    int nw = 0, ex = 0;
                    foreach (var e in preview.effects)
                    {
                        switch (e.effectType)
                        {
                            case "Damage": dmg++; break;
                            case "Impact": imp++; break;
                            case "Execute": exe++; break;
                            case "Cost": cost++; break;
                        }
                        var dirName = string.IsNullOrWhiteSpace(e.directory) ? "" : e.directory;
                        var assetDir = string.IsNullOrEmpty(dirName)
                            ? EffectImporter.EffectsRoot
                            : Path.Combine(EffectImporter.EffectsRoot, dirName).Replace('\\', '/');
                        var assetPath = Path.Combine(assetDir, $"{e.name}.asset").Replace('\\', '/');
                        if (File.Exists(assetPath)) ex++; else nw++;
                    }

                    GUILayout.Space(4);
                    EditorGUILayout.LabelField(
                        $"<b>{total}</b> effects ({dmg} Dmg · {imp} Imp · {exe} Exe · {cost} Cost) · v{preview.version} · {preview.description ?? "-"}",
                        new GUIStyle(EditorStyles.label) { richText = true });

                    EditorGUILayout.BeginHorizontal();
                    var green = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } };
                    var gray = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };
                    EditorGUILayout.LabelField($"New {nw}", green, GUILayout.Width(60));
                    EditorGUILayout.LabelField($"Existing {ex}", gray);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField("JSON is empty or parse failed.", EditorStyles.miniLabel);
                }
            }
            else
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("File not found.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        private void DrawButtons()
        {
            var hasFile = File.Exists(_jsonPath);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!hasFile);
            if (GUILayout.Button("Import", GUILayout.Height(32)))
            {
                (_lastCreated, _lastSkipped, _lastErrors) =
                    EffectImporter.ImportFromFile(_jsonPath);
                AssetDatabase.Refresh();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Export", GUILayout.Height(32)))
            {
                var outPath = EditorUtility.SaveFilePanel(
                    "Export Effects JSON", "Assets/Data/Ability/Effects", "effects_export", "json");
                if (!string.IsNullOrEmpty(outPath))
                {
                    EffectImporter.ExportToFile(outPath);
                    EditorUtility.DisplayDialog("Export Done", $"Exported to:\n{outPath}", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!hasFile)
            {
                var warn = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(1f, 0.6f, 0.2f) } };
                EditorGUILayout.LabelField("  File not found. Select a JSON file.", warn);
            }
        }

        private void DrawResult()
        {
            if (_lastCreated + _lastSkipped == 0 && _lastErrors.Count == 0) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            var hasErrors = _lastErrors.Count > 0;
            var okStyle = new GUIStyle(EditorStyles.label)
                { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } };
            var errStyle = new GUIStyle(EditorStyles.label)
                { normal = { textColor = new Color(0.9f, 0.3f, 0.2f) } };

            EditorGUILayout.LabelField(
                $"Created: {_lastCreated}  ·  Skipped: {_lastSkipped}" +
                (hasErrors ? $"  |  Errors: {_lastErrors.Count}" : ""),
                hasErrors ? errStyle : okStyle);

            if (hasErrors)
            {
                GUILayout.Space(4);
                EditorGUILayout.TextArea(string.Join("\n", _lastErrors), EditorStyles.miniLabel, GUILayout.MinHeight(40));
            }
            else
            {
                GUILayout.Space(4);
                EditorGUILayout.TextArea("No errors.", EditorStyles.miniLabel, GUILayout.MinHeight(20));
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        private void PickFile()
        {
            var selected = EditorUtility.OpenFilePanel(
                "Select Effect JSON", "Assets/Data/Ability/Effects", "json");
            if (string.IsNullOrEmpty(selected)) return;

            var projectPath = Path.GetDirectoryName(Application.dataPath);
            _jsonPath = selected.StartsWith(projectPath!)
                ? selected.Substring(projectPath.Length + 1).Replace('\\', '/')
                : selected;
        }
    }
}
#endif
