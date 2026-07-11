#if UNITY_EDITOR
using RedDust.Core.RdTag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Core.Events;
using RedDust.Gameplay.Ability.Editor;
using RedDust.Gameplay.Properties;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Gameplay.Ability
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
        public class BuffAdjunctEntry
        {
            public string propertyId;       // PropertyDefSO.Id
            public float valueAdd;
            public float valueMultiply = 1f;
            public float maxAdd;
            public float maxMultiply = 1f;
        }

        [System.Serializable]
        public class EffectEntry
        {
            public string effectType;  // "Damage" | "DamageMod" | "Impact" | "Execute" | "Cost" | "Buff"
            public string name;        // asset name (without .asset)
            public string description;  // designer-readable text
            public string directory;   // relative to EffectsRoot, e.g. "Damage/Fire"
            public string effectTag;   // FullTag string, nullable
            public float duration;
            public bool stackable;
            public int maxStacks = 1;
            public string[] applicationBlockedTags; // FullTag strings, nullable

            // Damage (DamageEffectSO)
            public float baseValue;

            // Damage Modifier (DamageModifierEffectSO)
            public string targetTag;   // FullTag string
            public float modAdd;
            public float modPercent;
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

            // Buff
            public string[] grantedTags;        // FullTag[]
            public BuffAdjunctEntry[] adjuncts;
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
                        break;
                    case DamageModifierEffectSO m:
                        entry.effectType = "DamageMod";
                        entry.targetTag = m.targetTag?.FullTag;
                        entry.modAdd = m.modAdd;
                        entry.modPercent = m.modPercent;
                        entry.priority = m.priority;
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
                    case BuffEffectSO b:
                        entry.effectType = "Buff";
                        entry.grantedTags = b.grantedTags?.Select(t => t?.FullTag).Where(t => t != null).ToArray();
                        entry.adjuncts = b.adjuncts?.Select(a => new BuffAdjunctEntry
                        {
                            propertyId = a.property?.Id,
                            valueAdd = a.valueAdd,
                            valueMultiply = a.valueMultiply,
                            maxAdd = a.maxAdd,
                            maxMultiply = a.maxMultiply,
                        }).ToArray();
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
        public static (int created, int updated, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, updated = 0, skipped = 0;

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
                return (0, 0, 0, errors);
            }
            if (importFile?.effects == null || importFile.effects.Length == 0)
            {
                errors.Add("JSON is empty or has no effects array.");
                return (0, 0, 0, errors);
            }

            // ── Phase 2: Validate ──
            var valid = new List<EffectEntry>();
            var validTypes = new HashSet<string> { "Damage", "DamageMod", "Impact", "Execute", "Cost", "Buff" };
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
            // Build RdTagDefSO lookup by FullTag
            var tagByFullTag = RdTagLookup.Build();

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
            var resolved = new List<(EffectEntry entry, RdTagDefSO effectTag,
                RdTagDefSO[] blockedTags, PropertyDefSO def)>();
            foreach (var entry in valid)
            {
                // effectTag
                RdTagDefSO resolvedTag = null;
                if (!string.IsNullOrEmpty(entry.effectTag))
                {
                    if (!tagByFullTag.TryGetValue(entry.effectTag, out resolvedTag))
                        errors.Add($"'{entry.name}': effectTag '{entry.effectTag}' not found in project");
                }

                // applicationBlockedTags
                var resolvedBlocked = new List<RdTagDefSO>();
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
                    ApplyFields(existing, entry, effTag, blockedTags, def, tagByFullTag, defById);
                    EditorUtility.SetDirty(existing);
                    updated++;
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
                    "Buff" => ScriptableObject.CreateInstance<BuffEffectSO>(),
                    "DamageMod" => ScriptableObject.CreateInstance<DamageModifierEffectSO>(),
                    _ => null,
                };
                if (instance == null) { errors.Add($"'{entry.name}': unknown type"); skipped++; continue; }

                instance.name = entry.name;
                ApplyFields(instance, entry, effTag, blockedTags, def, tagByFullTag, defById);
                AssetDatabase.CreateAsset(instance, assetPath);
                DataLabelTools.EnsureBootLabel(assetPath);
                created++;
                Debug.Log($"[EffectImporter] Created: {assetPath}");
            }

            // ── Phase 5: Persist ──
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EffectImporter] Done: created={created} updated={updated} skipped={skipped} errors={errors.Count}");
            return (created, updated, skipped, errors);
        }

        public static (int created, int updated, int skipped, List<string> errors) ImportFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                return (0, 0, 0, new List<string> { $"File not found: {jsonPath}" });
            return ImportFromJson(File.ReadAllText(jsonPath));
        }

        // ═══════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════
        private static string EffectTypeString(EffectSO e) => e switch
        {
            DamageEffectSO => "Damage",
            DamageModifierEffectSO => "DamageMod",
            ImpactEffectSO => "Impact",
            ExecuteEffectSO => "Execute",
            CostEffectSO => "Cost",
            BuffEffectSO => "Buff",
            _ => "Unknown",
        };

        private static void ApplyFields(EffectSO instance, EffectEntry entry,
            RdTagDefSO effTag, RdTagDefSO[] blockedTags,
            PropertyDefSO def,
            Dictionary<string, RdTagDefSO> tagByFullTag,
            Dictionary<string, PropertyDefSO> defById)
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
                    break;
                case DamageModifierEffectSO m:
                    tagByFullTag.TryGetValue(entry.targetTag ?? "", out var targetTag);
                    m.targetTag = targetTag;
                    m.modAdd = entry.modAdd;
                    m.modPercent = entry.modPercent;
                    m.priority = entry.priority;
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
                case BuffEffectSO b:
                    b.grantedTags = entry.grantedTags?.Select(t =>
                        tagByFullTag.TryGetValue(t, out var tg) ? tg : null)
                        .Where(t => t != null).ToArray();
                    if (entry.adjuncts != null)
                    {
                        b.adjuncts = entry.adjuncts.Select(a => new SBuffAdjunct
                        {
                            property = !string.IsNullOrEmpty(a.propertyId) && defById.TryGetValue(a.propertyId, out var pd) ? pd : null,
                            valueAdd = a.valueAdd,
                            valueMultiply = a.valueMultiply,
                            maxAdd = a.maxAdd,
                            maxMultiply = a.maxMultiply,
                        }).ToArray();
                    }
                    break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    // Import Window
    // ═══════════════════════════════════════════════════════
    public class EffectImportWindow : EditorWindow
    {
        private string _filePath = "Assets/Data/Ability/Effects/effects_all.json";
        private string _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Effect Import-Export", priority = 24)]
        public static void Open()
        {
            var window = GetWindow<EffectImportWindow>("Effect Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Effect Import-Export",
                subtitle: "L3_Ability · JSON ↔ .asset",
                defaultDir: "Assets/Data/Ability/Effects",
                fileExtension: "json",
                defaultFileName: "effects_export",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path =>
                {
                    return EffectImporter.ImportFromFile(path);
                },
                onExport: path => EffectImporter.ExportToFile(path)
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            EffectImporter.EffectExportFile preview;
            try
            {
                preview = JsonUtility.FromJson<EffectImporter.EffectExportFile>(
                    File.ReadAllText(filePath));
            }
            catch { return null; }
            if (preview?.effects == null || preview.effects.Length == 0) return null;

            int total = preview.effects.Length;
            int dmg = 0, imp = 0, exe = 0, cost = 0, buff = 0;
            int nw = 0, exist = 0;
            foreach (var e in preview.effects)
            {
                switch (e.effectType) { case "Damage": dmg++; break; case "Impact": imp++; break; case "Execute": exe++; break; case "Cost": cost++; break; case "Buff": buff++; break; case "DamageMod": dmg++; break; }
                var dirName = string.IsNullOrWhiteSpace(e.directory) ? "" : e.directory;
                var assetDir = string.IsNullOrEmpty(dirName) ? EffectImporter.EffectsRoot : Path.Combine(EffectImporter.EffectsRoot, dirName).Replace('\\', '/');
                var assetPath = Path.Combine(assetDir, $"{e.name}.asset").Replace('\\', '/');
                if (File.Exists(assetPath)) exist++; else nw++;
            }

            return $"<b>{total}</b> effects (<color=#66CC66>{dmg} Dmg</color> · <color=#66B266>{imp} Imp</color> · <color=#D32222>{exe} Exe</color> · <color=#4C7EFF>{cost} Cost</color> · <color=#8844CC>{buff} Buff</color>)\n" +
                   $"v{preview.version} · {preview.description ?? "-"}\n" +
                   $"<color=#66CC66>New {nw}</color>  Existing {exist}";
        }
    }
}
#endif
