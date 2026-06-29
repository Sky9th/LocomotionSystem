using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Properties.Editor
{
    /// <summary>
    /// PropertyDefSO + PropertyTreeSO JSON 导入/导出。
    /// 模式与 rTagImporter 完全一致。
    /// </summary>
    public static class PropertyImporter
    {
        private const string DefinitionsRoot = "Assets/Data/Properties/Definitions";
        private const string TreesRoot = "Assets/Data/Properties/Trees";

        [Serializable]
        public class PropertyDefEntry
        {
            public string id;
            public string type;
            public bool isDeprecated;
            public string description;

            public float min;
            public float max = 100f;
            public float defaultFloat = 100f;

            public int minInt;
            public int maxInt = 100;
            public int defaultInt;

            public string defaultString;
            public string defaultAssetGUID;
            public string assetTypeConstraint;
            public string structTypeName;
            public string defaultStructJson;
        }

        [Serializable]
        public class PropertyNodeEntry
        {
            public string nodeId;
            public string parentId;
            public string defId;
        }

        [Serializable]
        public class PropertyTreeEntry
        {
            public string treeName;
            public string inheritsFrom;
            public List<PropertyNodeEntry> nodes = new();
        }

        [Serializable]
        public class PropertyImportFile
        {
            public string version = "1.0";
            public string description;
            public List<PropertyDefEntry> definitions = new();
            public List<PropertyTreeEntry> trees = new();
        }

        public static (int created, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, skipped = 0;

            PropertyImportFile importFile;
            try { importFile = JsonUtility.FromJson<PropertyImportFile>(jsonText); }
            catch (Exception e) { errors.Add($"JSON parse failed: {e.Message}"); return (0, 0, errors); }

            if (importFile == null) { errors.Add("JSON is empty"); return (0, 0, errors); }

            // -- Phase 1: create definitions --
            var defMap = new Dictionary<string, PropertyDefSO>();
            foreach (var entry in importFile.definitions)
            {
                if (string.IsNullOrWhiteSpace(entry.id)) { errors.Add("Skip: def id empty"); skipped++; continue; }
                if (!Enum.TryParse<PropertyType>(entry.type, out var propType)) { errors.Add($"Unknown type '{entry.type}' for '{entry.id}'"); skipped++; continue; }

                var assetPath = $"{DefinitionsRoot}/{SanitizeFileName(entry.id)}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<PropertyDefSO>(assetPath);
                if (existing != null) { skipped++; defMap[entry.id] = existing; continue; }

                EnsureDirectory(DefinitionsRoot);
                var def = ScriptableObject.CreateInstance<PropertyDefSO>();
                def.Id = entry.id;
                def.Description = entry.description ?? string.Empty;
                def.Type = propType;
                def.IsDeprecated = entry.isDeprecated;
                def.Min = entry.min;
                def.Max = entry.max;
                def.DefaultFloat = entry.defaultFloat;
                def.MinInt = entry.minInt;
                def.MaxInt = entry.maxInt;
                def.DefaultInt = entry.defaultInt;
                def.DefaultString = entry.defaultString;
                def.DefaultAssetGUID = entry.defaultAssetGUID;
                def.AssetTypeConstraint = entry.assetTypeConstraint;
                def.StructTypeName = entry.structTypeName;
                def.DefaultStructJson = entry.defaultStructJson;

                AssetDatabase.CreateAsset(def, assetPath);
                defMap[entry.id] = def;
                created++;
            }

            AssetDatabase.SaveAssets();

            // -- Phase 2: create trees --
            var treeMap = new Dictionary<string, PropertyTreeSO>();
            foreach (var entry in importFile.trees)
            {
                if (string.IsNullOrWhiteSpace(entry.treeName)) { errors.Add("Skip: tree name empty"); skipped++; continue; }

                var assetPath = $"{TreesRoot}/{SanitizeFileName(entry.treeName)}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<PropertyTreeSO>(assetPath);
                if (existing != null) { skipped++; treeMap[entry.treeName] = existing; continue; }

                EnsureDirectory(TreesRoot);
                var container = new PropertyTreeContainer
                {
                    Nodes = entry.nodes.Select(n => new PropertyNode
                    {
                        NodeId = n.nodeId,
                        ParentId = n.parentId,
                        DefId = n.defId
                    }).ToList()
                };

                var tree = ScriptableObject.CreateInstance<PropertyTreeSO>();
                tree.treeJson = JsonUtility.ToJson(container, true);
                AssetDatabase.CreateAsset(tree, assetPath);
                treeMap[entry.treeName] = tree;
                created++;
            }

            AssetDatabase.SaveAssets();

            // -- Phase 3: link InheritsFrom --
            foreach (var entry in importFile.trees)
            {
                if (string.IsNullOrEmpty(entry.inheritsFrom)) continue;
                if (!treeMap.TryGetValue(entry.treeName, out var tree)) continue;
                if (!treeMap.TryGetValue(entry.inheritsFrom, out var parent))
                {
                    // fallback: search on disk
                    var parentPath = $"{TreesRoot}/{SanitizeFileName(entry.inheritsFrom)}.asset";
                    parent = AssetDatabase.LoadAssetAtPath<PropertyTreeSO>(parentPath);
                }
                if (parent == null) { errors.Add($"Parent tree '{entry.inheritsFrom}' not found for '{entry.treeName}'"); continue; }

                tree.InheritsFrom = parent;
                EditorUtility.SetDirty(tree);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return (created, skipped, errors);
        }

        public static string ExportToJson()
        {
            var export = new PropertyImportFile
            {
                version = "1.0",
                description = "Exported from Unity Editor"
            };

            var defGuids = AssetDatabase.FindAssets("t:PropertyDefSO", new[] { DefinitionsRoot });
            foreach (var guid in defGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<PropertyDefSO>(path);
                if (def == null) continue;
                export.definitions.Add(new PropertyDefEntry
                {
                    id = def.Id, type = def.Type.ToString(), isDeprecated = def.IsDeprecated,
                    description = def.Description,
                    min = def.Min, max = def.Max, defaultFloat = def.DefaultFloat,
                    minInt = def.MinInt, maxInt = def.MaxInt, defaultInt = def.DefaultInt,
                    defaultString = def.DefaultString, defaultAssetGUID = def.DefaultAssetGUID,
                    assetTypeConstraint = def.AssetTypeConstraint,
                    structTypeName = def.StructTypeName,
                    defaultStructJson = def.DefaultStructJson
                });
            }

            var treeGuids = AssetDatabase.FindAssets("t:PropertyTreeSO", new[] { TreesRoot });
            foreach (var guid in treeGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tree = AssetDatabase.LoadAssetAtPath<PropertyTreeSO>(path);
                if (tree == null) continue;

                var entry = new PropertyTreeEntry
                {
                    treeName = tree.name,
                    inheritsFrom = tree.InheritsFrom?.name
                };
                if (!string.IsNullOrEmpty(tree.treeJson))
                {
                    var container = JsonUtility.FromJson<PropertyTreeContainer>(tree.treeJson);
                    if (container?.Nodes != null)
                        entry.nodes = container.Nodes.Select(n => new PropertyNodeEntry
                        { nodeId = n.NodeId, parentId = n.ParentId, defId = n.DefId }).ToList();
                }
                export.trees.Add(entry);
            }

            return JsonUtility.ToJson(export, true);
        }

        public static void ExportToFile(string jsonPath)
        {
            var json = ExportToJson();
            var dir = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(jsonPath, json);
            Debug.Log($"[PropertyImporter] Exported to {jsonPath}");
        }

        public static (int created, int skipped, List<string> errors) ImportFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath)) return (0, 0, new List<string> { $"File not found: {jsonPath}" });
            return ImportFromJson(File.ReadAllText(jsonPath));
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c.ToString(), "_");
            return name;
        }
    }

    /// <summary>
    /// Property Import-Export 窗口。使用共享 EditorImportExport 组件。
    /// </summary>
    public class PropertyImportWindow : EditorWindow
    {
        private string _filePath = "Assets/Data/Properties/properties_all.json";
        private string _previewText;
        private (int created, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Property Import-Export", priority = 27)]
        public static void Open()
        {
            var window = GetWindow<PropertyImportWindow>("Property Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Property Import-Export",
                subtitle: "L3_Properties · JSON ↔ .asset",
                defaultDir: "Assets/Data/Properties",
                fileExtension: "json",
                defaultFileName: "properties_export",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path =>
                {
                    var (created, skipped, errors) = PropertyImporter.ImportFromFile(path);
                    PropertyDefinitionRegistry.Invalidate();
                    return (created, skipped, errors);
                },
                onExport: path => File.WriteAllText(path, PropertyImporter.ExportToJson())
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            PropertyImporter.PropertyImportFile preview;
            try { preview = JsonUtility.FromJson<PropertyImporter.PropertyImportFile>(File.ReadAllText(filePath)); }
            catch { return null; }
            if (preview == null) return null;

            int newDefs = 0, existDefs = 0, newTrees = 0, existTrees = 0;
            foreach (var d in preview.definitions)
            {
                var p = $"Assets/Data/Properties/Definitions/{d.id}.asset";
                if (AssetDatabase.LoadAssetAtPath<PropertyDefSO>(p) != null) existDefs++; else newDefs++;
            }
            foreach (var t in preview.trees)
            {
                var p = $"Assets/Data/Properties/Trees/{t.treeName}.asset";
                if (AssetDatabase.LoadAssetAtPath<PropertyTreeSO>(p) != null) existTrees++; else newTrees++;
            }

            return $"<b>{preview.definitions.Count} defs + {preview.trees.Count} trees</b>\n" +
                   $"v{preview.version} · {preview.description ?? "-"}\n" +
                   $"<color=#66CC66>New defs {newDefs}</color>  <color=#888888>Exist {existDefs}</color>  " +
                   $"<color=#66CC66>New trees {newTrees}</color>  <color=#888888>Exist {existTrees}</color>";
        }
    }
}
