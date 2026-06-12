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
    /// <summary>Ability DTO — 同时支持 Active 和 Passive。</summary>
    [Serializable]
    public class AbilityEntry
    {
        public string abilityType;  // "Active" or "Passive"
        public string name;
        public string internalName;
        public string displayName;
        public string description;
        public string abilityTag;          // FullTag
        public string sharedCooldownTag;   // FullTag
        public float cooldownDuration;
        public string[] targetEffects;     // asset names
        public string[] selfEffects;       // asset names

        // Active only
        public string activation;          // asset name
        public string search;              // asset name
        public bool overrideExclusion;
        public string[] extraExclusionTags; // FullTag[]
        public string noise;               // asset name
        public ComboLinkEntry[] comboLinks;

        // Passive only
        public string trigger;
        public string triggerChannel;      // asset name
        public float triggerValue;
        public string targetRequiredTag;   // FullTag
    }

    [Serializable]
    public class ComboLinkEntry
    {
        public string nextSkill;        // asset name
        public float windowStart;
        public float windowDuration;
        public bool bypassCooldown;
    }

    [Serializable]
    public class AbilityExportFile
    {
        public string version = "1.0";
        public string description;
        public AbilityEntry[] abilities;
    }

    public static class AbilityImporter
    {
        internal const string ActivesDir = "Assets/Data/Ability/Actives";
        internal const string PassivesDir = "Assets/Data/Ability/Passives";

        public static string ExportToJson()
        {
            var entries = new List<AbilityEntry>();

            // Active abilities
            foreach (var guid in AssetDatabase.FindAssets("t:AbilityDefSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<AbilityDefSO>(path);
                if (def == null) continue;
                entries.Add(ExportDef(def));
            }

            // Passive abilities
            foreach (var guid in AssetDatabase.FindAssets("t:PassiveAbilitySO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var p = AssetDatabase.LoadAssetAtPath<PassiveAbilitySO>(path);
                if (p == null) continue;
                entries.Add(ExportPassive(p));
            }

            entries.Sort((a, b) =>
            {
                var tCmp = string.CompareOrdinal(a.abilityType, b.abilityType);
                if (tCmp != 0) return tCmp;
                return string.CompareOrdinal(a.name, b.name);
            });

            return JsonUtility.ToJson(new AbilityExportFile
            {
                version = "1.0",
                description = "Ability definitions",
                abilities = entries.ToArray(),
            }, true);
        }

        private static AbilityEntry ExportDef(AbilityDefSO def)
        {
            var entry = ExportBase(def, "Active");
            entry.activation = def.activation?.name;
            entry.search = def.search?.name;
            entry.overrideExclusion = def.overrideExclusion;
            entry.extraExclusionTags = def.extraExclusionTags?
                .Select(t => t?.FullTag).Where(t => t != null).ToArray();
            entry.noise = def.noise?.name;
            entry.comboLinks = def.comboLinks?.Select(l => new ComboLinkEntry
            {
                nextSkill = l.NextSkill?.name,
                windowStart = l.WindowStart,
                windowDuration = l.WindowDuration,
                bypassCooldown = l.BypassCooldown,
            }).ToArray();
            return entry;
        }

        private static AbilityEntry ExportPassive(PassiveAbilitySO p)
        {
            var entry = ExportBase(p, "Passive");
            entry.trigger = p.trigger.ToString();
            entry.triggerChannel = p.triggerChannel?.name;
            entry.triggerValue = p.triggerValue;
            entry.targetRequiredTag = p.targetRequiredTag?.FullTag;
            return entry;
        }

        private static AbilityEntry ExportBase(AbilitySO a, string type)
        {
            return new AbilityEntry
            {
                abilityType = type,
                name = a.name,
                internalName = a.internalName,
                displayName = a.displayName,
                description = a.description,
                abilityTag = a.abilityTag?.FullTag,
                sharedCooldownTag = a.sharedCooldownTag?.FullTag,
                cooldownDuration = a.cooldownDuration,
                targetEffects = a.targetEffects?.Select(e => e?.name).Where(n => n != null).ToArray(),
                selfEffects = a.selfEffects?.Select(e => e?.name).Where(n => n != null).ToArray(),
            };
        }

        // ═══════════════════════════════════════════════════
        // Import
        // ═══════════════════════════════════════════════════
        public static (int created, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, skipped = 0;

            AbilityExportFile file;
            try { file = JsonUtility.FromJson<AbilityExportFile>(jsonText); }
            catch (Exception e) { errors.Add($"Parse failed: {e.Message}"); return (0, 0, errors); }
            if (file?.abilities == null || file.abilities.Length == 0)
            { errors.Add("Empty or invalid JSON."); return (0, 0, errors); }

            // Build lookups
            var tagByFullTag = BuildTagLookup();
            var activationByName = BuildAssetLookup<AbilityActivationSO>("t:AbilityActivationSO");
            var searchByName = BuildAssetLookup<AbilitySearchSO>("t:AbilitySearchSO");
            var effectByName = BuildAssetLookup<EffectSO>("t:EffectSO");
            var noiseByName = BuildAssetLookup<NoiseEventSO>("t:NoiseEventSO");
            var abilityDefByName = BuildAssetLookup<AbilityDefSO>("t:AbilityDefSO");
            var eventChannelByName = BuildAssetLookup<EventChannelBase>("t:EventChannelBase");

            foreach (var entry in file.abilities)
            {
                if (string.IsNullOrWhiteSpace(entry.name))
                { errors.Add("Skipping entry: empty name"); skipped++; continue; }

                var type = entry.abilityType;
                var isActive = type == "Active";
                var isPassive = type == "Passive";
                if (!isActive && !isPassive)
                { errors.Add($"'{entry.name}': unknown abilityType '{type}'"); skipped++; continue; }

                var dir = isActive ? ActivesDir : PassivesDir;
                var assetPath = Path.Combine(dir, $"{entry.name}.asset").Replace('\\', '/');

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // Check existing
                var existing = AssetDatabase.LoadAssetAtPath<AbilitySO>(assetPath);
                if (existing != null)
                {
                    if ((isActive && existing is not AbilityDefSO) || (isPassive && existing is not PassiveAbilitySO))
                    { errors.Add($"'{entry.name}': type mismatch"); skipped++; continue; }
                    ApplyFields(existing, entry, tagByFullTag, activationByName, searchByName,
                        effectByName, noiseByName, abilityDefByName, eventChannelByName);
                    EditorUtility.SetDirty(existing);
                    skipped++;
                    continue;
                }

                // Create new
                AbilitySO instance = isActive
                    ? ScriptableObject.CreateInstance<AbilityDefSO>()
                    : ScriptableObject.CreateInstance<PassiveAbilitySO>();
                instance.name = entry.name;
                ApplyFields(instance, entry, tagByFullTag, activationByName, searchByName,
                    effectByName, noiseByName, abilityDefByName, eventChannelByName);
                AssetDatabase.CreateAsset(instance, assetPath);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return (created, skipped, errors);
        }

        private static Dictionary<string, GameplayTagDefinitionSO> BuildTagLookup()
        {
            var dict = new Dictionary<string, GameplayTagDefinitionSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:GameplayTagDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var t = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(path);
                if (t != null && !string.IsNullOrEmpty(t.FullTag))
                    dict[t.FullTag] = t;
            }
            return dict;
        }

        private static Dictionary<string, T> BuildAssetLookup<T>(string filter) where T : UnityEngine.Object
        {
            var dict = new Dictionary<string, T>();
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && !dict.ContainsKey(asset.name))
                    dict[asset.name] = asset;
            }
            return dict;
        }

        private static void ApplyFields(AbilitySO a, AbilityEntry entry,
            Dictionary<string, GameplayTagDefinitionSO> tags,
            Dictionary<string, AbilityActivationSO> activations,
            Dictionary<string, AbilitySearchSO> searches,
            Dictionary<string, EffectSO> effects,
            Dictionary<string, NoiseEventSO> noises,
            Dictionary<string, AbilityDefSO> abilityDefs,
            Dictionary<string, EventChannelBase> channels)
        {
            // Base fields
            a.internalName = entry.internalName;
            a.displayName = entry.displayName;
            a.description = entry.description;
            a.cooldownDuration = entry.cooldownDuration;

            if (!string.IsNullOrEmpty(entry.abilityTag) && tags.TryGetValue(entry.abilityTag, out var at))
                a.abilityTag = at;
            if (!string.IsNullOrEmpty(entry.sharedCooldownTag) && tags.TryGetValue(entry.sharedCooldownTag, out var sct))
                a.sharedCooldownTag = sct;

            // Effects
            a.targetEffects = ResolveEffects(entry.targetEffects, effects);
            a.selfEffects = ResolveEffects(entry.selfEffects, effects);

            // Active-specific
            if (a is AbilityDefSO def)
            {
                if (!string.IsNullOrEmpty(entry.activation) && activations.TryGetValue(entry.activation, out var act))
                    def.activation = act;
                if (!string.IsNullOrEmpty(entry.search) && searches.TryGetValue(entry.search, out var s))
                    def.search = s;
                def.overrideExclusion = entry.overrideExclusion;
                def.extraExclusionTags = entry.extraExclusionTags?.Select(t =>
                    tags.TryGetValue(t, out var tag) ? tag : null).Where(t => t != null).ToArray();
                if (def.extraExclusionTags == null || def.extraExclusionTags.Length == 0)
                    def.extraExclusionTags = null;

                if (!string.IsNullOrEmpty(entry.noise) && noises.TryGetValue(entry.noise, out var n))
                    def.noise = n;

                if (entry.comboLinks != null)
                {
                    def.comboLinks = entry.comboLinks.Select(l =>
                    {
                        var link = new SComboLink
                        {
                            WindowStart = l.windowStart,
                            WindowDuration = l.windowDuration,
                            BypassCooldown = l.bypassCooldown,
                        };
                        if (!string.IsNullOrEmpty(l.nextSkill) && abilityDefs.TryGetValue(l.nextSkill, out var ns))
                            link.NextSkill = ns;
                        return link;
                    }).ToArray();
                }
            }

            // Passive-specific
            if (a is PassiveAbilitySO passive)
            {
                if (!string.IsNullOrEmpty(entry.trigger) && Enum.TryParse<ETriggerEvent>(entry.trigger, out var te))
                    passive.trigger = te;
                if (!string.IsNullOrEmpty(entry.triggerChannel) && channels.TryGetValue(entry.triggerChannel, out var ch))
                    passive.triggerChannel = ch;
                passive.triggerValue = entry.triggerValue;
                if (!string.IsNullOrEmpty(entry.targetRequiredTag) && tags.TryGetValue(entry.targetRequiredTag, out var trt))
                    passive.targetRequiredTag = trt;
            }
        }

        private static EffectSO[] ResolveEffects(string[] names, Dictionary<string, EffectSO> effects)
        {
            if (names == null || names.Length == 0) return null;
            var list = new List<EffectSO>();
            foreach (var n in names)
                if (!string.IsNullOrEmpty(n) && effects.TryGetValue(n, out var e))
                    list.Add(e);
            return list.Count > 0 ? list.ToArray() : null;
        }
    }

    /// <summary>Ability Import/Export 窗口。使用共享 EditorImportExport 组件。</summary>
    public class AbilityImportWindow : EditorWindow
    {
        private string _filePath;
        private string _previewText;
        private (int created, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Ability Import-Export", priority = 24)]
        public static void Open()
        {
            var window = GetWindow<AbilityImportWindow>("Ability Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Ability Import-Export",
                subtitle: "L3_Ability · JSON ↔ .asset",
                defaultDir: "Assets/Data/Ability",
                fileExtension: "json",
                defaultFileName: "abilities_export",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path =>
                {
                    var (created, skipped, errors) = AbilityImporter.ImportFromJson(File.ReadAllText(path));
                    return (created, skipped, errors);
                },
                onExport: path => File.WriteAllText(path, AbilityImporter.ExportToJson())
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            AbilityExportFile preview;
            try { preview = JsonUtility.FromJson<AbilityExportFile>(File.ReadAllText(filePath)); }
            catch { return null; }
            if (preview?.abilities == null || preview.abilities.Length == 0) return null;

            int total = preview.abilities.Length;
            int active = 0, passive = 0;
            foreach (var a in preview.abilities)
            {
                switch (a.abilityType) { case "Active": active++; break; case "Passive": passive++; break; }
            }

            return $"<b>{total}</b> abilities (<color=#33AA33>{active} Active</color> · <color=#3388CC>{passive} Passive</color>)\n" +
                   $"v{preview.version} · {preview.description ?? "-"}";
        }
    }
}
#endif
