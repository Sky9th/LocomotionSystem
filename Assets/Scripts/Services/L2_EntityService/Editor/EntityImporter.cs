#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using RedDust.Core.Events;
using RedDust.Gameplay.Properties;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedDust.Services.EntityService.Editor
{
    /// <summary>
    /// Entity Import/Export 共享引擎。
    /// 替代 5 份独立的 XxxImporter 类。差异全部由 EntityImportConfig 参数化。
    /// </summary>
    public static class EntityImporter
    {
        // ═══════════════════════════════════════════════════
        // Export
        // ═══════════════════════════════════════════════════

        public static string ExportToJson(EntityImportConfig config)
        {
            var export = new EntityExportFile
            {
                version = "1.0",
                description = $"Exported {DateTime.Now:yyyy-MM-dd HH:mm}",
                category = config.Category,
                entities = BuildEntries(config)
            };
            return JsonUtility.ToJson(export, true);
        }

        public static void ExportToFile(string jsonPath, EntityImportConfig config)
            => File.WriteAllText(jsonPath, ExportToJson(config));

        private static EntityEntry[] BuildEntries(EntityImportConfig config)
        {
            var entries = new List<EntityEntry>();

            foreach (var guid in AssetDatabase.FindAssets(config.AssetFilter))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<PropertyPresetSO>(path);
                if (preset == null) continue;

                entries.Add(new EntityEntry
                {
                    entityType = TypeToLabel(preset, config),
                    name = preset.name,
                    templateName = preset.Template != null ? preset.Template.name : null,
                    overridesJson = preset.OverridesJson,
                    prefabGuid = preset.Prefab != null
                        ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset.Prefab)) : null,
                });
            }

            // Sort: type label (if multi-type), then name
            entries.Sort((a, b) =>
            {
                int tc = string.CompareOrdinal(a.entityType ?? "", b.entityType ?? "");
                return tc != 0 ? tc : string.CompareOrdinal(a.name, b.name);
            });

            return entries.ToArray();
        }

        /// <summary>反查 Type→Label。单类型返回 null。</summary>
        private static string TypeToLabel(PropertyPresetSO preset, EntityImportConfig config)
        {
            if (config.TypeMap == null) return null;
            var type = preset.GetType();
            foreach (var (label, t) in config.TypeMap)
                if (t == type) return label;
            return null;
        }

        // ═══════════════════════════════════════════════════
        // Import
        // ═══════════════════════════════════════════════════

        public static (int created, int updated, int skipped, List<string> errors)
            ImportFromJson(string jsonText, EntityImportConfig config)
        {
            int created = 0, updated = 0, skipped = 0;
            var errors = new List<string>();

            // Phase 1: Deserialize
            EntityExportFile file;
            try { file = JsonUtility.FromJson<EntityExportFile>(jsonText); }
            catch (Exception e) { errors.Add($"JSON parse failed: {e.Message}"); return (0, 0, 0, errors); }
            if (file?.entities == null || file.entities.Length == 0)
            { errors.Add("No entities in JSON."); return (0, 0, file?.entities?.Length ?? 0, errors); }

            // Phase 2 prep: Build lookups
            var templateByName = BuildAssetLookup<PropertyTreeSO>("t:PropertyTreeSO");
            var existingByName = BuildExistingLookup(config);

            if (!Directory.Exists(config.DataRoot))
                Directory.CreateDirectory(config.DataRoot);

            // Phase 3-4: Validate + Resolve + Create/Update per entry
            foreach (var entry in file.entities)
            {
                var label = string.IsNullOrEmpty(entry.name) ? "?" : entry.name;

                // Validate
                if (string.IsNullOrEmpty(entry.name))
                { errors.Add("Skipping: empty name."); skipped++; continue; }
                if (entry.name.Contains('/') || entry.name.Contains('\\'))
                { errors.Add($"[{label}] Path separator in name — skipping."); skipped++; continue; }

                // Resolve type
                if (!TryResolveType(entry, config, out var resolvedType, out var typeError))
                { errors.Add($"[{label}] {typeError}"); skipped++; continue; }

                // Resolve template + prefab
                templateByName.TryGetValue(entry.templateName ?? "", out var template);
                var prefab = ResolvePrefab(entry.prefabGuid);
                var subDir = GetSubDirectory(entry, config);
                var assetPath = $"{config.DataRoot}{(string.IsNullOrEmpty(subDir) ? "" : $"/{subDir}")}/{entry.name}.asset";
                var assetDir = Path.GetDirectoryName(assetPath);
                if (!Directory.Exists(assetDir))
                    Directory.CreateDirectory(assetDir);

                // Create or Update
                if (existingByName.TryGetValue(entry.name, out var existing))
                {
                    if (existing.GetType() != resolvedType)
                    { errors.Add($"[{label}] Type mismatch: existing is {existing.GetType().Name}."); skipped++; continue; }

                    ApplyFields(existing, entry, template, prefab);
                    EditorUtility.SetDirty(existing);
                    updated++;
                }
                else
                {
                    var instance = (PropertyPresetSO)ScriptableObject.CreateInstance(resolvedType);
                    instance.name = entry.name;
                    ApplyFields(instance, entry, template, prefab);

                    AssetDatabase.CreateAsset(instance, assetPath);
                    DataLabelTools.EnsureBootLabel(assetPath);
                    created++;
                }
            }

            // Phase 5: Persist
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return (created, updated, skipped, errors);
        }

        public static (int created, int updated, int skipped, List<string> errors)
            ImportFromFile(string jsonPath, EntityImportConfig config)
        {
            if (!File.Exists(jsonPath))
                return (0, 0, 0, new List<string> { $"File not found: {jsonPath}" });
            return ImportFromJson(File.ReadAllText(jsonPath), config);
        }

        // ═══════════════════════════════════════════════════
        // Type Resolution
        // ═══════════════════════════════════════════════════

        private static bool TryResolveType(EntityEntry entry, EntityImportConfig config,
            out Type type, out string error)
        {
            if (config.TypeMap != null)
            {
                if (!config.TypeMap.TryGetValue(entry.entityType ?? "", out type))
                {
                    type = null;
                    error = $"Unknown entityType '{entry.entityType}'.";
                    return false;
                }
            }
            else
            {
                type = config.DefaultType;
            }

            error = null;
            return true;
        }

        // ═══════════════════════════════════════════════════
        // Shared Helpers (public — 供其他 Importer 复用)
        // ═══════════════════════════════════════════════════

        internal static void ApplyFields(PropertyPresetSO target, EntityEntry entry,
            PropertyTreeSO template, GameObject prefab)
        {
            target.Template = template;
            target.OverridesJson = entry.overridesJson;
            target.Prefab = prefab;
            SyncContentId(target, entry.overridesJson);
        }

        private static void SyncContentId(PropertyPresetSO target, string overridesJson)
        {
            var container = new ContentIdContainer();
            JsonUtility.FromJsonOverwrite(overridesJson, container);
            foreach (var o in container.Overrides)
            {
                if (o.Path == "Common/Id")
                {
                    target.SetContentId(o.Value);
                    Debug.Log($"[EntityImporter] SyncContentId: '{target.name}' → contentId='{o.Value}'");
                    return;
                }
            }
        }

        [Serializable]
        private class ContentIdEntry { public string Path; public string Value; }

        [Serializable]
        private class ContentIdContainer { public List<ContentIdEntry> Overrides = new(); }

        public static GameObject ResolvePrefab(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            var p = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(p);
        }

        public static Dictionary<string, T> BuildAssetLookup<T>(string filter) where T : Object
        {
            var dict = new Dictionary<string, T>();
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(p);
                if (asset != null && !dict.ContainsKey(asset.name))
                    dict[asset.name] = asset;
            }
            return dict;
        }

        /// <summary>Derive a subdirectory from entityType. Falls back to templateName for single-type categories (e.g. Ammo).</summary>
        private static string GetSubDirectory(EntityEntry entry, EntityImportConfig config)
        {
            // Multi-type: use entityType label (Armor, MeleeWeapon, RangedWeapon, Container, Consumable, etc.)
            if (config.TypeMap != null)
            {
                var et = entry.entityType;
                if (!string.IsNullOrEmpty(et)) return et;
            }

            // Single-type or null entityType: use templateName
            if (!string.IsNullOrEmpty(entry.templateName))
                return entry.templateName;

            return null;
        }

        private static Dictionary<string, PropertyPresetSO> BuildExistingLookup(EntityImportConfig config)
        {
            var dict = new Dictionary<string, PropertyPresetSO>();
            foreach (var guid in AssetDatabase.FindAssets(config.AssetFilter))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<PropertyPresetSO>(p);
                if (asset == null) continue;
                if (!dict.ContainsKey(asset.name))
                    dict[asset.name] = asset;
            }
            return dict;
        }
    }
}
#endif
