#if UNITY_EDITOR
using RedDust.Core.RdTag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Core.Events;
using RedDust.Gameplay.Ability.Editor;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>AbilityTree 节点 DTO——对应 SAbilityTreeNode。</summary>
    [Serializable]
    public class TreeNodeEntry
    {
        public string nodeId;
        public string ability;      // ActiveAbilitySO asset name, nullable
        public string passive;      // PassiveAbilitySO asset name, nullable
        public string[] prerequisites;  // nodeId[], empty = root
    }

    /// <summary>AbilityTree 导出 DTO。</summary>
    [Serializable]
    public class AbilityTreeEntry
    {
        public string treeId;
        public string displayName;
        public string description;
        public string icon;                // asset path, nullable
        public string[] treeTags;          // FullTag[]
        public string[] compatibleWeaponTags; // FullTag[]
        public string[] compatibleGripTags;   // FullTag[]
        public string exclusiveGroup;      // "" = no exclusion
        public TreeNodeEntry[] nodes;
    }

    [Serializable]
    public class AbilityTreeExportFile
    {
        public string version = "1.0";
        public string description;
        public AbilityTreeEntry[] trees;
    }

    /// <summary>
    /// AbilityTreeSO JSON 导入/导出工具。
    /// 仿 AbilityImporter 模式——ExportToJson / ImportFromJson。
    /// 资产目录: Assets/Data/Ability/AbilityTrees/
    /// </summary>
    public static class AbilityTreeImporter
    {
        internal const string TreesRoot = "Assets/Data/Ability/AbilityTrees";

        // ═══════════════════════════════════════════════════
        // Export
        // ═══════════════════════════════════════════════════

        public static string ExportToJson()
        {
            var entries = new List<AbilityTreeEntry>();

            foreach (var guid in AssetDatabase.FindAssets("t:AbilityTreeSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tree = AssetDatabase.LoadAssetAtPath<AbilityTreeSO>(path);
                if (tree == null) continue;
                entries.Add(ExportTree(tree));
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.treeId, b.treeId));

            return JsonUtility.ToJson(new AbilityTreeExportFile
            {
                version = "1.0",
                description = "AbilityTree definitions — Innate / Talent / Routine",
                trees = entries.ToArray(),
            }, true);
        }

        private static AbilityTreeEntry ExportTree(AbilityTreeSO tree)
        {
            return new AbilityTreeEntry
            {
                treeId = tree.treeId,
                displayName = tree.displayName,
                description = tree.description,
                icon = tree.icon != null ? AssetDatabase.GetAssetPath(tree.icon) : null,
                treeTags = tree.treeTags?.Select(t => t?.FullTag).Where(t => t != null).ToArray(),
                compatibleWeaponTags = tree.compatibleWeaponTags?
                    .Select(t => t?.FullTag).Where(t => t != null).ToArray(),
                compatibleGripTags = tree.compatibleGripTags?
                    .Select(t => t?.FullTag).Where(t => t != null).ToArray(),
                exclusiveGroup = tree.exclusiveGroup ?? "",
                nodes = tree.nodes?.Select(ExportNode).ToArray(),
            };
        }

        private static TreeNodeEntry ExportNode(SAbilityTreeNode node)
        {
            return new TreeNodeEntry
            {
                nodeId = node.nodeId,
                ability = node.ability?.name,
                passive = node.passive?.name,
                prerequisites = node.prerequisites?.Length > 0 ? node.prerequisites : null,
            };
        }

        // ═══════════════════════════════════════════════════
        // Import
        // ═══════════════════════════════════════════════════

        public static (int created, int updated, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, updated = 0, skipped = 0;

            AbilityTreeExportFile file;
            try { file = JsonUtility.FromJson<AbilityTreeExportFile>(jsonText); }
            catch (Exception e) { errors.Add($"Parse failed: {e.Message}"); return (0, 0, 0, errors); }
            if (file?.trees == null || file.trees.Length == 0)
            { errors.Add("Empty or invalid JSON."); return (0, 0, 0, errors); }

            // Build lookups
            var tagByFullTag = BuildTagLookup();
            var abilityByName = BuildAssetLookup<ActiveAbilitySO>("t:ActiveAbilitySO");
            var passiveByName = BuildAssetLookup<PassiveAbilitySO>("t:PassiveAbilitySO");

            foreach (var entry in file.trees)
            {
                if (string.IsNullOrWhiteSpace(entry.treeId))
                { errors.Add("Skipping entry: empty treeId"); skipped++; continue; }

                // Determine sub-directory from treeTags
                var categoryDir = DetermineCategoryDir(entry.treeTags);
                var dir = $"{TreesRoot}/{categoryDir}";

                // Remove entries the importer doesn't use
                EnsureDirectoryExists(dir);

                var assetPath = $"{dir}/{entry.treeId}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<AbilityTreeSO>(assetPath);

                if (existing != null)
                {
                    ApplyTreeFields(existing, entry, tagByFullTag, abilityByName, passiveByName, errors);
                    EditorUtility.SetDirty(existing);
                    updated++;
                    continue;
                }

                var tree = ScriptableObject.CreateInstance<AbilityTreeSO>();
                tree.treeId = entry.treeId;
                ApplyTreeFields(tree, entry, tagByFullTag, abilityByName, passiveByName, errors);

                AssetDatabase.CreateAsset(tree, assetPath);
                DataLabelTools.EnsureBootLabel(assetPath);
                EditorUtility.SetDirty(tree);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return (created, updated, skipped, errors);
        }

        private static SAbilityTreeNode ImportNode(TreeNodeEntry entry,
            Dictionary<string, ActiveAbilitySO> abilityByName,
            Dictionary<string, PassiveAbilitySO> passiveByName,
            List<string> errors, string treeId)
        {
            var node = new SAbilityTreeNode { nodeId = entry.nodeId ?? "" };

            if (!string.IsNullOrEmpty(entry.ability))
            {
                if (abilityByName.TryGetValue(entry.ability, out var abilityDef))
                    node.ability = abilityDef;
                else
                    errors.Add($"'{treeId}/{entry.nodeId}': ability '{entry.ability}' not found");
            }

            if (!string.IsNullOrEmpty(entry.passive))
            {
                if (passiveByName.TryGetValue(entry.passive, out var passiveDef))
                    node.passive = passiveDef;
                else
                    errors.Add($"'{treeId}/{entry.nodeId}': passive '{entry.passive}' not found");
            }

            node.prerequisites = entry.prerequisites ?? Array.Empty<string>();
            return node;
        }

        private static void ApplyTreeFields(AbilityTreeSO tree, AbilityTreeEntry entry,
            Dictionary<string, RdTagDefSO> tagByFullTag,
            Dictionary<string, ActiveAbilitySO> abilityByName,
            Dictionary<string, PassiveAbilitySO> passiveByName,
            List<string> errors)
        {
            tree.displayName = entry.displayName ?? entry.treeId;
            tree.description = entry.description ?? "";
            tree.exclusiveGroup = entry.exclusiveGroup ?? "";

            // Resolve tags
            tree.treeTags = ResolveTags(entry.treeTags, tagByFullTag, errors, entry.treeId);
            tree.compatibleWeaponTags = ResolveTags(entry.compatibleWeaponTags, tagByFullTag, errors, entry.treeId);
            tree.compatibleGripTags = ResolveTags(entry.compatibleGripTags, tagByFullTag, errors, entry.treeId);

            // Resolve icon
            if (!string.IsNullOrEmpty(entry.icon))
            {
                tree.icon = AssetDatabase.LoadAssetAtPath<Sprite>(entry.icon);
                if (tree.icon == null)
                    errors.Add($"'{entry.treeId}': icon not found at '{entry.icon}'");
            }

            // Resolve nodes
            tree.nodes = entry.nodes?.Select(n => ImportNode(n, abilityByName, passiveByName, errors, entry.treeId)).ToArray()
                         ?? Array.Empty<SAbilityTreeNode>();
        }

        // ═══════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════

        private static string DetermineCategoryDir(string[] treeTags)
        {
            if (treeTags == null || treeTags.Length == 0) return "Innate";

            foreach (var tag in treeTags)
            {
                if (tag != null)
                {
                    if (tag.EndsWith(".Talent")) return "Talent";
                    if (tag.EndsWith(".Routine")) return "Routine";
                }
            }
            return "Innate";
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static Dictionary<string, RdTagDefSO> BuildTagLookup() => RdTagLookup.Build();

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

        private static RdTagDefSO[] ResolveTags(string[] fullTags,
            Dictionary<string, RdTagDefSO> lookup, List<string> errors, string treeId)
        {
            if (fullTags == null || fullTags.Length == 0) return Array.Empty<RdTagDefSO>();

            var result = new List<RdTagDefSO>();
            foreach (var ft in fullTags)
            {
                if (string.IsNullOrEmpty(ft)) continue;
                if (lookup.TryGetValue(ft, out var tag))
                    result.Add(tag);
                else
                    errors.Add($"'{treeId}': tag '{ft}' not found");
            }
            return result.ToArray();
        }
    }
}
#endif
