using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Properties.Editor
{
    /// <summary>
    /// PropertyDefSO + PropertyTreeSO JSON 导入/导出。
    /// 模式与 GameplayTagImporter 完全一致。
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
                    assetTypeConstraint = def.AssetTypeConstraint
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
    /// Property 导入 Editor 窗口。
    /// 菜单: RedDust > Property Import-Export
    /// 模式与 GameplayTagImportWindow 一致。
    /// </summary>
    public class PropertyImportWindow : EditorWindow
    {
        private const float Pad = 6f;
        private string _jsonPath = "Assets/Data/Properties/properties_all.json";
        private int _lastCreated, _lastSkipped;
        private List<string> _lastErrors = new();

        [MenuItem("RedDust/Property Import-Export", priority = 42)]
        public static void ShowWindow()
        {
            var window = GetWindow<PropertyImportWindow>("Property Importer");
            window.minSize = new Vector2(480, 280);
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
            DrawImportButton();
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
            EditorGUILayout.LabelField("Property Importer", EditorStyles.largeLabel);
            var subStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, normal = { textColor = Color.gray } };
            EditorGUILayout.LabelField("L3_Properties · JSON → .asset", subStyle, GUILayout.Width(240));
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
            if (GUILayout.Button("…", GUILayout.Width(28), GUILayout.Height(18))) PickFile();
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
                PropertyImporter.PropertyImportFile preview = null;
                try { preview = JsonUtility.FromJson<PropertyImporter.PropertyImportFile>(File.ReadAllText(_jsonPath)); } catch { }

                if (preview != null)
                {
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

                    GUILayout.Space(4);
                    EditorGUILayout.LabelField($"<b>{preview.definitions.Count} defs + {preview.trees.Count} trees</b> · v{preview.version} · {preview.description ?? "-"}",
                        new GUIStyle(EditorStyles.label) { richText = true });
                    EditorGUILayout.BeginHorizontal();
                    var g = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } };
                    var s = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };
                    EditorGUILayout.LabelField($"New defs {newDefs}", g, GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Exist {existDefs}", s, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"New trees {newTrees}", g, GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Exist {existTrees}", s);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField("JSON empty or invalid format", EditorStyles.miniLabel);
                }
            }
            else
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("File not found", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        private void DrawImportButton()
        {
            var hasFile = File.Exists(_jsonPath);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!hasFile);
            if (GUILayout.Button("Import", GUILayout.Height(32)))
            {
                (_lastCreated, _lastSkipped, _lastErrors) = PropertyImporter.ImportFromFile(_jsonPath);
                PropertyDefinitionRegistry.Invalidate();
                AssetDatabase.Refresh();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Export", GUILayout.Height(32)))
            {
                var outPath = EditorUtility.SaveFilePanel("Export Property JSON", "Assets/Data/Properties", "properties_export", "json");
                if (!string.IsNullOrEmpty(outPath))
                {
                    PropertyImporter.ExportToFile(outPath);
                    EditorUtility.DisplayDialog("Export Done", $"Exported to:\n{outPath}", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!hasFile)
            {
                var warnStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.6f, 0.2f) } };
                EditorGUILayout.LabelField("  File not found, select a JSON file", warnStyle);
            }
        }

        private void DrawResult()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_lastCreated + _lastSkipped == 0 && _lastErrors.Count == 0) { EditorGUILayout.EndVertical(); return; }
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            var okStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } };
            var errStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.9f, 0.3f, 0.2f) } };
            EditorGUILayout.LabelField($"Created {_lastCreated} · Skipped {_lastSkipped}" + (_lastErrors.Count > 0 ? $" · Errors {_lastErrors.Count}" : ""),
                _lastErrors.Count > 0 ? errStyle : okStyle);

            if (_lastErrors.Count > 0)
            {
                GUILayout.Space(4);
                foreach (var e in _lastErrors) EditorGUILayout.LabelField($"  ⚠ {e}", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        private void PickFile()
        {
            var selected = EditorUtility.OpenFilePanel("Select Property Import JSON", "Assets/Data/Properties", "json");
            if (string.IsNullOrEmpty(selected)) return;
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            _jsonPath = selected.StartsWith(projectPath!) ? selected[(projectPath.Length + 1)..].Replace('\\', '/') : selected;
        }
    }
}
