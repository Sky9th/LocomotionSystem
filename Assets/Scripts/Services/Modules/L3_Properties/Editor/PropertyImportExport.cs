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
        public class PropertyNodeEntry { public string nodeId, parentId, defId; }

        [Serializable]
        public class PropertyTreeEntry { public string treeName, inheritsFrom; public List<PropertyNodeEntry> nodes = new(); }

        [Serializable]
        public class ImportDefs
        {
            public List<FloatPropertyDefSO.JsonData> _float;
            public List<IntPropertyDefSO.JsonData> _int;
            public List<BoolPropertyDefSO.JsonData> _bool;
            public List<StringPropertyDefSO.JsonData> _string;
            public List<RdTagPropertyDefSO.JsonData> _rdTag;
            public List<RdTagListPropertyDefSO.JsonData> _rdTagList;
            public List<AssetRefPropertyDefSO.JsonData> _assetRef;
            public List<AssetRefListPropertyDefSO.JsonData> _assetRefList;
            public List<StructPropertyDefSO.JsonData> _struct;
        }

        [Serializable]
        public class ImportRoot
        {
            public string version;
            public string description;
            public ImportDefs definitions;
            public List<PropertyTreeEntry> trees;
        }

        // ============================================================
        // Import
        // ============================================================

        public static (int created, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, skipped = 0;

            ImportRoot root;
            try { root = JsonUtility.FromJson<ImportRoot>(jsonText); }
            catch (Exception e) { errors.Add($"JSON parse failed: {e.Message}"); return (0, 0, errors); }
            if (root?.definitions == null) { errors.Add("JSON empty or missing definitions"); return (0, 0, errors); }

            var defMap = new Dictionary<string, PropertyDefSO>();
            void AddDef(PropertyDefSO def, PropertyType type)
            {
                if (string.IsNullOrWhiteSpace(def.Id)) { errors.Add("Skip: empty id"); skipped++; return; }
                var dir = $"{DefinitionsRoot}/{type}";
                EnsureDirectory(dir);
                var assetPath = $"{dir}/{SanitizeFileName(def.Id)}.asset";
                if (AssetDatabase.LoadAssetAtPath<PropertyDefSO>(assetPath) != null) { skipped++; defMap[def.Id] = def; return; }
                AssetDatabase.CreateAsset(def, assetPath);
                defMap[def.Id] = def; created++;
            }

            if (root.definitions._float != null) foreach (var d in root.definitions._float) { var def = (FloatPropertyDefSO)PropertyDefSO.Create(PropertyType.Float); def.Id = d.id; def.Description = d.description; def.IsDeprecated = d.isDeprecated; def.Min = d.min; def.Max = d.max; def.DefaultValue = d.defaultValue; AddDef(def, PropertyType.Float); }
            if (root.definitions._int != null) foreach (var d in root.definitions._int) { var def = (IntPropertyDefSO)PropertyDefSO.Create(PropertyType.Int); def.Id = d.id; def.Description = d.description; def.IsDeprecated = d.isDeprecated; def.Min = d.min; def.Max = d.max; def.DefaultValue = d.defaultValue; AddDef(def, PropertyType.Int); }
            if (root.definitions._bool != null) foreach (var d in root.definitions._bool) { var def = (BoolPropertyDefSO)PropertyDefSO.Create(PropertyType.Bool); def.Id = d.id; def.Description = d.description; def.IsDeprecated = d.isDeprecated; def.DefaultValue = d.defaultValue; AddDef(def, PropertyType.Bool); }
            if (root.definitions._string != null) foreach (var d in root.definitions._string) { var def = (StringPropertyDefSO)PropertyDefSO.Create(PropertyType.String); def.Id = d.id; def.Description = d.description; def.IsDeprecated = d.isDeprecated; def.DefaultValue = d.defaultValue; AddDef(def, PropertyType.String); }
            if (root.definitions._rdTag != null) foreach (var d in root.definitions._rdTag) { var def = (RdTagPropertyDefSO)PropertyDefSO.Create(PropertyType.RdTag); def.Id = d.id; def.Description = d.description; def.IsDeprecated = d.isDeprecated; def.DefaultValue = d.defaultValue; AddDef(def, PropertyType.RdTag); }
            if (root.definitions._rdTagList != null) foreach (var d in root.definitions._rdTagList) { var def = (RdTagListPropertyDefSO)PropertyDefSO.Create(PropertyType.RdTagList); def.Id = d.id; def.Description = d.description; def.IsDeprecated = d.isDeprecated; AddDef(def, PropertyType.RdTagList); }
            if (root.definitions._assetRef != null) foreach (var d in root.definitions._assetRef) { var def = (AssetRefPropertyDefSO)PropertyDefSO.Create(PropertyType.AssetRef); def.Id = d.id; def.Description = d.description; def.IsDeprecated = d.isDeprecated; def.DefaultAssetGUID = d.defaultAssetGUID; def.AssetTypeConstraint = d.assetTypeConstraint; AddDef(def, PropertyType.AssetRef); }
            if (root.definitions._assetRefList != null) foreach (var d in root.definitions._assetRefList) { var def = (AssetRefListPropertyDefSO)PropertyDefSO.Create(PropertyType.AssetRefList); def.Id = d.id; def.Description = d.description; def.IsDeprecated = d.isDeprecated; def.AssetTypeConstraint = d.assetTypeConstraint; AddDef(def, PropertyType.AssetRefList); }
            if (root.definitions._struct != null) foreach (var d in root.definitions._struct) { var def = (StructPropertyDefSO)PropertyDefSO.Create(PropertyType.Struct); def.Id = d.id; def.Description = d.description; def.IsDeprecated = d.isDeprecated; def.StructTypeName = d.structTypeName; def.DefaultJson = d.defaultJson ?? "[]"; AddDef(def, PropertyType.Struct); }

            AssetDatabase.SaveAssets();

            // -- Trees --
            var treeMap = new Dictionary<string, PropertyTreeSO>();
            if (root.trees != null)
                foreach (var entry in root.trees)
                {
                    if (string.IsNullOrWhiteSpace(entry.treeName)) { errors.Add("Skip: tree name empty"); skipped++; continue; }
                    var assetPath = $"{TreesRoot}/{SanitizeFileName(entry.treeName)}.asset";
                    if (AssetDatabase.LoadAssetAtPath<PropertyTreeSO>(assetPath) != null) { skipped++; continue; }

                    EnsureDirectory(TreesRoot);
                    var tree = ScriptableObject.CreateInstance<PropertyTreeSO>();
                    tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = entry.nodes?.ConvertAll(n => new PropertyNode { NodeId = n.nodeId, ParentId = n.parentId, DefId = n.defId }) ?? new() }, true);
                    AssetDatabase.CreateAsset(tree, assetPath);
                    treeMap[entry.treeName] = tree;
                    created++;
                }

            AssetDatabase.SaveAssets();

            if (root.trees != null)
                foreach (var entry in root.trees)
                {
                    if (string.IsNullOrEmpty(entry.inheritsFrom) || !treeMap.TryGetValue(entry.treeName, out var tree)) continue;
                    if (!treeMap.TryGetValue(entry.inheritsFrom, out var parent))
                        parent = AssetDatabase.LoadAssetAtPath<PropertyTreeSO>($"{TreesRoot}/{SanitizeFileName(entry.inheritsFrom)}.asset");
                    if (parent == null) { errors.Add($"Parent tree '{entry.inheritsFrom}' not found for '{entry.treeName}'"); continue; }
                    tree.InheritsFrom = parent; EditorUtility.SetDirty(tree);
                }

            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            return (created, skipped, errors);
        }

        // ============================================================
        // Export
        // ============================================================

        public static string ExportToJson()
        {
            var defGuids = AssetDatabase.FindAssets("t:PropertyDefSO", new[] { DefinitionsRoot });
            var root = new ImportRoot { version = "2.0", description = "Exported from Unity Editor", definitions = new ImportDefs() };

            foreach (var guid in defGuids)
            {
                var def = AssetDatabase.LoadAssetAtPath<PropertyDefSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null) continue;
                if (def is FloatPropertyDefSO fd) Add(ref root.definitions._float, new FloatPropertyDefSO.JsonData { id=fd.Id,description=fd.Description,isDeprecated=fd.IsDeprecated,min=fd.Min,max=fd.Max,defaultValue=fd.DefaultValue });
                else if (def is IntPropertyDefSO id) Add(ref root.definitions._int, new IntPropertyDefSO.JsonData { id=id.Id,description=id.Description,isDeprecated=id.IsDeprecated,min=id.Min,max=id.Max,defaultValue=id.DefaultValue });
                else if (def is BoolPropertyDefSO bd) Add(ref root.definitions._bool, new BoolPropertyDefSO.JsonData { id=bd.Id,description=bd.Description,isDeprecated=bd.IsDeprecated,defaultValue=bd.DefaultValue });
                else if (def is StringPropertyDefSO sd) Add(ref root.definitions._string, new StringPropertyDefSO.JsonData { id=sd.Id,description=sd.Description,isDeprecated=sd.IsDeprecated,defaultValue=sd.DefaultValue });
                else if (def is RdTagPropertyDefSO rd) Add(ref root.definitions._rdTag, new RdTagPropertyDefSO.JsonData { id=rd.Id,description=rd.Description,isDeprecated=rd.IsDeprecated,defaultValue=rd.DefaultValue });
                else if (def is RdTagListPropertyDefSO td) Add(ref root.definitions._rdTagList, new RdTagListPropertyDefSO.JsonData { id=td.Id,description=td.Description,isDeprecated=td.IsDeprecated });
                else if (def is AssetRefPropertyDefSO ad) Add(ref root.definitions._assetRef, new AssetRefPropertyDefSO.JsonData { id=ad.Id,description=ad.Description,isDeprecated=ad.IsDeprecated,defaultAssetGUID=ad.DefaultAssetGUID,assetTypeConstraint=ad.AssetTypeConstraint });
                else if (def is AssetRefListPropertyDefSO ald) Add(ref root.definitions._assetRefList, new AssetRefListPropertyDefSO.JsonData { id=ald.Id,description=ald.Description,isDeprecated=ald.IsDeprecated,assetTypeConstraint=ald.AssetTypeConstraint });
                else if (def is StructPropertyDefSO std) Add(ref root.definitions._struct, new StructPropertyDefSO.JsonData { id=std.Id,description=std.Description,isDeprecated=std.IsDeprecated,structTypeName=std.StructTypeName,defaultJson=std.DefaultJson });
            }

            root.trees = BuildTreeEntries();
            return JsonUtility.ToJson(root, true);
        }
        static void Add<T>(ref List<T> list, T item) { (list ??= new()).Add(item); }

        static List<PropertyTreeEntry> BuildTreeEntries()
        {
            var trees = new List<PropertyTreeEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:PropertyTreeSO", new[] { TreesRoot }))
            {
                var tree = AssetDatabase.LoadAssetAtPath<PropertyTreeSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (tree == null) continue;
                var entry = new PropertyTreeEntry { treeName = tree.name, inheritsFrom = tree.InheritsFrom?.name };
                if (!string.IsNullOrEmpty(tree.treeJson))
                {
                    var c = JsonUtility.FromJson<PropertyTreeContainer>(tree.treeJson);
                    if (c?.Nodes != null) entry.nodes = c.Nodes.Select(n => new PropertyNodeEntry { nodeId = n.NodeId, parentId = n.ParentId, defId = n.DefId }).ToList();
                }
                trees.Add(entry);
            }
            return trees;
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
            try
            {
                var root = JsonUtility.FromJson<PropertyImporter.ImportRoot>(File.ReadAllText(filePath));
                if (root?.definitions == null) return null;

                int defCount = 0, existCount = 0;
                void Count<T>(List<T> list, PropertyType type, Func<T, string> getId)
                {
                    if (list == null) return;
                    foreach (var item in list)
                    {
                        var defId = getId(item);
                        if (string.IsNullOrEmpty(defId)) continue;
                        defCount++;
                        if (AssetDatabase.LoadAssetAtPath<PropertyDefSO>($"Assets/Data/Properties/Definitions/{type}/{defId}.asset") != null) existCount++;
                    }
                }
                Count(root.definitions._float,        PropertyType.Float,        d => d.id);
                Count(root.definitions._int,          PropertyType.Int,          d => d.id);
                Count(root.definitions._bool,         PropertyType.Bool,         d => d.id);
                Count(root.definitions._string,       PropertyType.String,       d => d.id);
                Count(root.definitions._rdTag,         PropertyType.RdTag,         d => d.id);
                Count(root.definitions._rdTagList,     PropertyType.RdTagList,     d => d.id);
                Count(root.definitions._assetRef,     PropertyType.AssetRef,     d => d.id);
                Count(root.definitions._assetRefList, PropertyType.AssetRefList, d => d.id);
                Count(root.definitions._struct,       PropertyType.Struct,       d => d.id);

                int treeCount = root.trees?.Count ?? 0;
                return $"<b>{defCount} defs + {treeCount} trees</b>\n" +
                       $"v{root.version ?? "?"} · {root.description ?? "-"}\n" +
                       $"<color=#66CC66>New defs {defCount - existCount}</color>  " +
                       $"<color=#888888>Exist {existCount}</color>";
            }
            catch { return null; }
        }    }
}
