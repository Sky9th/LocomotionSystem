#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Core;
using RedDust.Properties;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedDust.Items.Editor
{
    // ═══════════════════════════════════════════════════════════════
    // DTO Classes
    // ═══════════════════════════════════════════════════════════════

    [Serializable]
    public class ItemEntry
    {
        public string itemType;       // "Item" | "MeleeWeapon" | "RangedWeapon"
        public string name;           // asset name (not path)
        public string templateName;   // PropertyTreeSO.name — cross-machine portable
        public string overridesJson;  // raw copy of OverridesJson
        public string prefabGuid;     // AssetPathToGUID, null if no prefab
    }

    [Serializable]
    public class ItemExportFile
    {
        public string version = "1.0";
        public string description;
        public ItemEntry[] items;
    }

    // ═══════════════════════════════════════════════════════════════
    // Static Importer
    // ═══════════════════════════════════════════════════════════════

    public static class ItemImporter
    {
        private const string ItemsRoot = "Assets/Data/Items";

        private static readonly Dictionary<string, Type> s_typeMap = new()
        {
            ["Item"] = typeof(ItemDefSO),
            ["MeleeWeapon"] = typeof(MeleeWeaponSO),
            ["RangedWeapon"] = typeof(RangedWeaponSO),
            ["Armor"] = typeof(ArmorSO),
            ["Consumable"] = typeof(ConsumableSO),
            ["Ammo"] = typeof(AmmoSO),
            ["Tool"] = typeof(ToolSO),
            ["Container"] = typeof(ContainerSO),
            ["Material"] = typeof(MaterialSO),
        };

        // ═══════════════════════════════════════════════════════════
        // Export
        // ═══════════════════════════════════════════════════════════

        public static string ExportToJson()
        {
            var export = new ItemExportFile
            {
                version = "1.0",
                description = $"Exported {DateTime.Now:yyyy-MM-dd HH:mm}",
                items = BuildItemEntries()
            };

            return JsonUtility.ToJson(export, true);
        }

        public static void ExportToFile(string jsonPath)
        {
            File.WriteAllText(jsonPath, ExportToJson());
        }

        private static ItemEntry[] BuildItemEntries()
        {
            var entries = new List<ItemEntry>();
            var guids = AssetDatabase.FindAssets("t:ItemDefSO");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemDefSO>(path);
                if (item == null) continue;

                var entry = new ItemEntry
                {
                    itemType = TypeToLabel(item),
                    name = item.name,
                    templateName = item.Template != null ? item.Template.name : null,
                    overridesJson = item.OverridesJson,
                    prefabGuid = item.Prefab != null
                        ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item.Prefab)) : null,
                };

                entries.Add(entry);
            }

            // Sort by (itemType, name) for deterministic output
            entries.Sort((a, b) =>
            {
                int typeCmp = string.CompareOrdinal(a.itemType, b.itemType);
                return typeCmp != 0 ? typeCmp : string.CompareOrdinal(a.name, b.name);
            });

            return entries.ToArray();
        }

        private static string TypeToLabel(PropertyPresetSO item)
        {
            if (item is MeleeWeaponSO) return "MeleeWeapon";
            if (item is RangedWeaponSO) return "RangedWeapon";
            if (item is ArmorSO) return "Armor";
            if (item is ConsumableSO) return "Consumable";
            if (item is AmmoSO) return "Ammo";
            if (item is ToolSO) return "Tool";
            if (item is ContainerSO) return "Container";
            if (item is MaterialSO) return "Material";
            return "Item"; // ItemDefSO or unknown subclass
        }

        // ═══════════════════════════════════════════════════════════
        // Import
        // ═══════════════════════════════════════════════════════════

        public static (int created, int updated, int skipped, List<string> errors)
            ImportFromJson(string jsonText)
        {
            int created = 0, updated = 0, skipped = 0;
            var errors = new List<string>();

            // ── Phase 1: Deserialize ──
            ItemExportFile importFile;
            try
            {
                importFile = JsonUtility.FromJson<ItemExportFile>(jsonText);
            }
            catch (Exception e)
            {
                errors.Add($"JSON parse failed: {e.Message}");
                return (0, 0, 0, errors);
            }

            if (importFile == null)
            {
                errors.Add("JSON deserialized to null.");
                return (0, 0, 0, errors);
            }

            if (importFile.items == null || importFile.items.Length == 0)
            {
                errors.Add("No items in JSON (items array is null or empty).");
                return (0, 0, importFile.items?.Length ?? 0, errors);
            }

            // ── Phase 3 pre-build: Resolve lookups ──
            var templateByName = BuildAssetLookup<PropertyTreeSO>("t:PropertyTreeSO");
            var existingItemByName = BuildExistingItemLookup();

            // Ensure target directory exists
            if (!Directory.Exists(ItemsRoot))
                Directory.CreateDirectory(ItemsRoot);

            // ── Phase 2–4: Validate + Resolve + Create/Update per entry ──
            for (int i = 0; i < importFile.items.Length; i++)
            {
                var entry = importFile.items[i];
                var label = string.IsNullOrEmpty(entry.name) ? $"[{i}]" : entry.name;

                // Validate
                if (string.IsNullOrEmpty(entry.name))
                {
                    errors.Add($"[{i}] Skipping: empty name.");
                    skipped++;
                    continue;
                }

                if (entry.name.Contains('/') || entry.name.Contains('\\'))
                {
                    errors.Add($"[{label}] Name contains path separator — skipping.");
                    skipped++;
                    continue;
                }

                if (!s_typeMap.ContainsKey(entry.itemType ?? ""))
                {
                    errors.Add($"[{label}] Unknown itemType '{entry.itemType}' — skipping.");
                    skipped++;
                    continue;
                }

                var resolvedType = s_typeMap[entry.itemType];

                // Resolve template
                PropertyTreeSO template = null;
                if (!string.IsNullOrEmpty(entry.templateName))
                {
                    if (!templateByName.TryGetValue(entry.templateName, out template))
                        errors.Add($"[{label}] Template '{entry.templateName}' not found — set to null.");
                }

                // Resolve prefab
                GameObject prefab = null;
                if (!string.IsNullOrEmpty(entry.prefabGuid))
                {
                    var prefabPath = AssetDatabase.GUIDToAssetPath(entry.prefabGuid);
                    if (!string.IsNullOrEmpty(prefabPath))
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    else
                        errors.Add($"[{label}] Prefab GUID '{entry.prefabGuid}' not resolvable — set to null.");
                }

                // Create or Update
                var assetPath = $"{ItemsRoot}/{entry.name}.asset";

                if (existingItemByName.TryGetValue(entry.name, out var existing))
                {
                    // ── Update existing ──
                    if (existing.GetType() != resolvedType)
                    {
                        errors.Add($"[{label}] Type mismatch: existing is {existing.GetType().Name}, " +
                                   $"JSON says {entry.itemType} — skipping.");
                        skipped++;
                        continue;
                    }

                    ApplyFields(existing, entry, template, prefab);
                    EditorUtility.SetDirty(existing);
                    updated++;
                }
                else
                {
                    // ── Create new ──
                    var instance = (PropertyPresetSO)ScriptableObject.CreateInstance(resolvedType);
                    instance.name = entry.name;
                    ApplyFields(instance, entry, template, prefab);

                    AssetDatabase.CreateAsset(instance, assetPath);
                    DataLabelTools.EnsureBootLabel(assetPath);
                    created++;
                }
            }

            // ── Phase 5: Persist ──
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return (created, updated, skipped, errors);
        }

        public static (int created, int updated, int skipped, List<string> errors)
            ImportFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                return (0, 0, 0,
                    new List<string> { $"File not found: {jsonPath}" });
            }

            return ImportFromJson(File.ReadAllText(jsonPath));
        }

        // ── Helpers ──

        private static void ApplyFields(PropertyPresetSO target, ItemEntry entry,
            PropertyTreeSO template, GameObject prefab)
        {
            target.Template = template;
            target.OverridesJson = entry.overridesJson;
            target.Prefab = prefab;
        }

        /// <summary>Build a name→asset lookup across the entire project.</summary>
        private static Dictionary<string, T> BuildAssetLookup<T>(string filter) where T : Object
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

        /// <summary>Build a name→ItemDefSO lookup for existing items project-wide.</summary>
        private static Dictionary<string, PropertyPresetSO> BuildExistingItemLookup()
        {
            var dict = new Dictionary<string, PropertyPresetSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:ItemDefSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ItemDefSO>(path);
                if (asset == null) continue;

                if (dict.ContainsKey(asset.name))
                    Debug.LogWarning($"[ItemImporter] Duplicate item name '{asset.name}': " +
                                     $"{AssetDatabase.GetAssetPath(dict[asset.name])} vs {path}. " +
                                     "Using first found.");
                else
                    dict[asset.name] = asset;
            }
            return dict;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // EditorWindow
    // ═══════════════════════════════════════════════════════════════

    public class ItemImportWindow : EditorWindow
    {
        private string _filePath;
        private string _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Item Import-Export", priority = 29)]
        public static void Open()
        {
            var window = GetWindow<ItemImportWindow>("Item Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Item Import-Export",
                subtitle: "L3_Item · JSON ↔ .asset",
                defaultDir: "Assets/Data/Items",
                fileExtension: "json",
                defaultFileName: "items_export",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path => ItemImporter.ImportFromFile(path),
                onExport: path => File.WriteAllText(path, ItemImporter.ExportToJson())
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            ItemExportFile preview;
            try
            {
                var jsonText = File.ReadAllText(filePath);
                preview = JsonUtility.FromJson<ItemExportFile>(jsonText);
            }
            catch { return null; }

            if (preview?.items == null || preview.items.Length == 0) return null;

            int melee = 0, ranged = 0, basic = 0;
            foreach (var entry in preview.items)
            {
                switch (entry.itemType)
                {
                    case "MeleeWeapon": melee++; break;
                    case "RangedWeapon": ranged++; break;
                    default: basic++; break;
                }
            }

            var parts = new List<string>();
            if (melee > 0) parts.Add($"<b>{melee}</b> Melee");
            if (ranged > 0) parts.Add($"<b>{ranged}</b> Ranged");
            if (basic > 0) parts.Add($"<b>{basic}</b> Item");

            return $"<b>{preview.items.Length}</b> items ({string.Join(" / ", parts)})\n" +
                   $"v{preview.version} · {preview.description ?? "-"}";
        }
    }
}
#endif
