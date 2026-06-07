using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Stats.Editor
{
    /// <summary>
    /// Stat JSON 导入/导出工具。
    /// 支持 StatDefinitionSO（单个定义）和 StatsTreeSO（树结构）两种资产。
    /// </summary>
    public static class StatImporter
    {
        internal const string StatsRoot = "Assets/Data/Stats";
        internal const string DefinitionsRoot = "Assets/Data/Stats/Definitions";

        [System.Serializable]
        public class StatDefEntry
        {
            public string id;
            public string category;
            public float min;
            public float max = 100f;
            public float defaultValue = 100f;
            public bool isConsumable;
            public float consumeRate;
            public float consumeInterval;
            public bool isRestorable;
            public float restoreRate;
            public float restoreInterval;
            public bool isCumulative;
        }

        [System.Serializable]
        public class StatsTreeEntry
        {
            public string name;
            public string directory;
            public string inheritsFrom;
            public List<string> defRefs = new();
            public List<JsonStatNode> nodes = new();
        }

        [System.Serializable]
        public class StatExportFile
        {
            public string version = "1.0";
            public string description;
            public List<StatDefEntry> definitions = new();
            public List<StatsTreeEntry> trees = new();
        }

        /// <summary>
        /// 导出所有 StatDefinitionSO 和 StatsTreeSO 资产为 JSON 字符串。
        /// </summary>
        public static string ExportToJson()
        {
            var export = new StatExportFile
            {
                version = "1.0",
                description = "Exported from Unity Editor"
            };

            // ---- definitions ----
            var defGuids = AssetDatabase.FindAssets("t:StatDefinitionSO");
            foreach (var guid in defGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<StatDefinitionSO>(path);
                if (def == null) continue;

                // category = subdirectory under DefinitionsRoot
                var category = "";
                var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir) && dir.StartsWith(DefinitionsRoot + "/"))
                {
                    var rel = dir.Substring(DefinitionsRoot.Length + 1);
                    var slashIdx = rel.IndexOf('/');
                    category = slashIdx >= 0 ? rel.Substring(0, slashIdx) : rel;
                }

                export.definitions.Add(new StatDefEntry
                {
                    id = def.Id,
                    category = category,
                    min = def.Min,
                    max = def.Max,
                    defaultValue = def.Default,
                    isConsumable = def.isConsumable,
                    consumeRate = def.consumeRate,
                    consumeInterval = def.consumeInterval,
                    isRestorable = def.isRestorable,
                    restoreRate = def.restoreRate,
                    restoreInterval = def.restoreInterval,
                    isCumulative = def.isCumulative,
                });
            }

            // sort definitions: category then id
            export.definitions.Sort((a, b) =>
            {
                var catCmp = string.CompareOrdinal(a.category, b.category);
                if (catCmp != 0) return catCmp;
                return string.CompareOrdinal(a.id, b.id);
            });

            // ---- trees ----
            var treeGuids = AssetDatabase.FindAssets("t:StatsTreeSO");
            // name → tree SO (for depth calculation)
            var allTreesByName = new Dictionary<string, StatsTreeSO>();
            var treeInfos = new List<(string path, StatsTreeSO tree)>();

            foreach (var guid in treeGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tree = AssetDatabase.LoadAssetAtPath<StatsTreeSO>(path);
                if (tree == null) continue;

                allTreesByName[tree.name] = tree;
                treeInfos.Add((path, tree));
            }

            // helper: compute inheritance depth (recursive, cycle-safe)
            int GetDepth(StatsTreeSO t, HashSet<StatsTreeSO> visited = null)
            {
                if (t == null) return 0;
                visited ??= new HashSet<StatsTreeSO>();
                if (!visited.Add(t)) return 0; // cycle
                if (t.InheritsFrom == null) return 0;
                return 1 + GetDepth(t.InheritsFrom, visited);
            }

            foreach (var (path, tree) in treeInfos)
            {
                // directory: relative to Assets/Data/Stats/
                var relDir = "";
                var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir) && dir.StartsWith(StatsRoot + "/"))
                    relDir = dir.Substring(StatsRoot.Length + 1);
                else if (!string.IsNullOrEmpty(dir) && dir != StatsRoot)
                    relDir = dir;

                // defRefs → Id strings
                var idRefs = new List<string>();
                foreach (var def in tree.defRefs)
                {
                    idRefs.Add(def != null ? def.Id : "MISSING");
                }

                // deserialize nodes from treeJson
                var nodes = new List<JsonStatNode>();
                if (!string.IsNullOrEmpty(tree.treeJson))
                {
                    try
                    {
                        var container = JsonUtility.FromJson<TreeDataContainer>(tree.treeJson);
                        if (container?.Nodes != null)
                            nodes = container.Nodes;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[StatImporter] Failed to parse treeJson for '{tree.name}': {e.Message}");
                    }
                }

                export.trees.Add(new StatsTreeEntry
                {
                    name = tree.name,
                    directory = relDir,
                    inheritsFrom = tree.InheritsFrom != null ? tree.InheritsFrom.name : null,
                    defRefs = idRefs,
                    nodes = nodes,
                });
            }

            // sort trees: inheritance depth first (root → leaf), then name
            export.trees.Sort((a, b) =>
            {
                allTreesByName.TryGetValue(a.name, out var ta);
                allTreesByName.TryGetValue(b.name, out var tb);
                var dA = GetDepth(ta);
                var dB = GetDepth(tb);
                var dCmp = dA.CompareTo(dB);
                if (dCmp != 0) return dCmp;
                return string.CompareOrdinal(a.name, b.name);
            });

            return JsonUtility.ToJson(export, true);
        }

        /// <summary>
        /// 导出到 JSON 文件。
        /// </summary>
        public static void ExportToFile(string jsonPath)
        {
            var json = ExportToJson();
            var dir = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(jsonPath, json);
            Debug.Log($"[StatImporter] Exported {jsonPath}");
        }

        /// <summary>
        /// 从 JSON 文本导入资产（五阶段）。
        /// Phase 1: 导入 StatDefinitionSO .asset
        /// Phase 2: 解析 Tree defRefs（Id 字符串 → SO 引用）
        /// Phase 3: 导入 StatsTreeSO .asset（不链 InheritsFrom）
        /// Phase 4: 链接 InheritsFrom
        /// Phase 5: 持久化
        /// </summary>
        /// <returns>(defsCreated, defsSkipped, treesCreated, treesSkipped, errors)</returns>
        public static (int defsCreated, int defsSkipped, int treesCreated, int treesSkipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int defsCreated = 0, defsSkipped = 0;
            int treesCreated = 0, treesSkipped = 0;

            StatExportFile importFile;
            try
            {
                importFile = JsonUtility.FromJson<StatExportFile>(jsonText);
            }
            catch (Exception e)
            {
                errors.Add($"JSON 解析失败: {e.Message}");
                return (0, 0, 0, 0, errors);
            }

            if (importFile == null)
            {
                errors.Add("JSON 反序列化返回 null");
                return (0, 0, 0, 0, errors);
            }

            importFile.definitions ??= new List<StatDefEntry>();
            importFile.trees ??= new List<StatsTreeEntry>();

            if (importFile.definitions.Count == 0 && importFile.trees.Count == 0)
            {
                errors.Add("JSON 中没有 definitions 和 trees（或均为空）");
                return (0, 0, 0, 0, errors);
            }

            // ---- Phase 1: Import StatDefinitionSO assets ----
            var idToAssetPath = new Dictionary<string, string>(); // Stat Id → asset path
            var batchCreatedIds = new HashSet<string>(); // intra-batch duplicate detection

            foreach (var entry in importFile.definitions)
            {
                if (string.IsNullOrWhiteSpace(entry.id))
                {
                    errors.Add("跳过: definition id 为空");
                    defsSkipped++;
                    continue;
                }

                var cat = string.IsNullOrWhiteSpace(entry.category) ? "Attribute" : entry.category;
                var assetDir = Path.Combine(DefinitionsRoot, cat).Replace('\\', '/');
                var assetPath = Path.Combine(assetDir, $"{entry.id}.asset").Replace('\\', '/');

                // check path conflict
                var existing = AssetDatabase.LoadAssetAtPath<StatDefinitionSO>(assetPath);
                if (existing != null)
                {
                    if (existing.Id == entry.id)
                    {
                        defsSkipped++;
                        idToAssetPath[entry.id] = assetPath;
                        continue;
                    }
                    else
                    {
                        errors.Add($"路径冲突: '{assetPath}' 已存在，但其 Id='{existing.Id}'（预期 '{entry.id}'）");
                        defsSkipped++;
                        continue;
                    }
                }

                // global duplicate check: same Id at a different path
                var allDefGuids = AssetDatabase.FindAssets("t:StatDefinitionSO");
                bool duplicateId = false;
                foreach (var defGuid in allDefGuids)
                {
                    var defPath = AssetDatabase.GUIDToAssetPath(defGuid);
                    if (defPath == assetPath) continue;
                    var def = AssetDatabase.LoadAssetAtPath<StatDefinitionSO>(defPath);
                    if (def != null && def.Id == entry.id)
                    {
                        errors.Add($"跳过 '{entry.id}': 同 Id 的资产已存在于 '{defPath}'");
                        duplicateId = true;
                        break;
                    }
                }
                if (duplicateId) { defsSkipped++; continue; }

                // intra-batch duplicate: same Id already created in this import
                if (!batchCreatedIds.Add(entry.id))
                {
                    errors.Add($"跳过 '{entry.id}': 同批次内 Id 重复");
                    defsSkipped++;
                    continue;
                }

                // create directory
                if (!Directory.Exists(assetDir))
                    Directory.CreateDirectory(assetDir);

                // create asset
                var so = ScriptableObject.CreateInstance<StatDefinitionSO>();
                so.Id = entry.id;
                so.Min = entry.min;
                so.Max = entry.max;
                so.Default = entry.defaultValue;
                so.isConsumable = entry.isConsumable;
                so.consumeRate = entry.consumeRate;
                so.consumeInterval = entry.consumeInterval;
                so.isRestorable = entry.isRestorable;
                so.restoreRate = entry.restoreRate;
                so.restoreInterval = entry.restoreInterval;
                so.isCumulative = entry.isCumulative;

                AssetDatabase.CreateAsset(so, assetPath);
                so.name = entry.id; // set .name after CreateAsset
                defsCreated++;
                idToAssetPath[entry.id] = assetPath;

                Debug.Log($"[StatImporter] Created definition: {assetPath}");
            }

            // ---- Phase 2: Resolve defRefs (Id string → StatDefinitionSO reference) ----
            // Build complete lookup: all StatDefinitionSO in project
            var allDefsById = new Dictionary<string, StatDefinitionSO>();
            var allGuids = AssetDatabase.FindAssets("t:StatDefinitionSO");
            foreach (var guid in allGuids)
            {
                var defPath = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<StatDefinitionSO>(defPath);
                if (def != null && !string.IsNullOrEmpty(def.Id))
                    allDefsById[def.Id] = def;
            }

            // Also load Phase 1 creations (FindAssets may not see them before SaveAssets)
            foreach (var kv in idToAssetPath)
            {
                if (!allDefsById.ContainsKey(kv.Key))
                {
                    var def = AssetDatabase.LoadAssetAtPath<StatDefinitionSO>(kv.Value);
                    if (def != null && !string.IsNullOrEmpty(def.Id))
                        allDefsById[def.Id] = def;
                }
            }

            // For each tree, resolve its defRefs
            var resolvedDefRefs = new Dictionary<string, List<StatDefinitionSO>>(); // tree name → resolved list

            foreach (var treeEntry in importFile.trees)
            {
                var resolved = new List<StatDefinitionSO>();
                var missingIds = new List<string>();

                if (treeEntry.defRefs != null)
                {
                    foreach (var idStr in treeEntry.defRefs)
                    {
                        if (allDefsById.TryGetValue(idStr, out var def))
                            resolved.Add(def);
                        else
                        {
                            resolved.Add(null); // placeholder to preserve index
                            missingIds.Add(idStr);
                        }
                    }
                }

                if (missingIds.Count > 0)
                    errors.Add($"Tree '{treeEntry.name}': defRefs 引用了项目中不存在的 Stat Id: {string.Join(", ", missingIds)}");

                resolvedDefRefs[treeEntry.name] = resolved;
            }

            // ---- Phase 3: Create StatsTreeSO assets (no InheritsFrom linking yet) ----
            var treeNameToAssetPath = new Dictionary<string, string>();

            // pre-validate: reject duplicate tree names in the batch
            var treeNamesInBatch = new HashSet<string>();
            foreach (var treeEntry in importFile.trees)
            {
                if (string.IsNullOrWhiteSpace(treeEntry.name)) continue;
                if (!treeNamesInBatch.Add(treeEntry.name))
                {
                    errors.Add($"Tree name '{treeEntry.name}' 在 JSON 中重复（同名 Tree 不支持）");
                }
            }

            foreach (var treeEntry in importFile.trees)
            {
                if (string.IsNullOrWhiteSpace(treeEntry.name))
                {
                    errors.Add("跳过: tree name 为空");
                    treesSkipped++;
                    continue;
                }

                // skip duplicate names (already reported in pre-validation)
                if (treeNameToAssetPath.ContainsKey(treeEntry.name))
                {
                    treesSkipped++;
                    continue;
                }

                var treeDirName = string.IsNullOrWhiteSpace(treeEntry.directory) ? "" : treeEntry.directory;
                var treeDir = string.IsNullOrEmpty(treeDirName)
                    ? StatsRoot
                    : Path.Combine(StatsRoot, treeDirName).Replace('\\', '/');
                var assetPath = Path.Combine(treeDir, $"{treeEntry.name}.asset").Replace('\\', '/');

                // check existing
                var existing = AssetDatabase.LoadAssetAtPath<StatsTreeSO>(assetPath);
                if (existing != null)
                {
                    treesSkipped++;
                    treeNameToAssetPath[treeEntry.name] = assetPath;
                    continue;
                }

                // create directory
                if (!Directory.Exists(treeDir))
                    Directory.CreateDirectory(treeDir);

                // create tree asset
                var tree = ScriptableObject.CreateInstance<StatsTreeSO>();
                AssetDatabase.CreateAsset(tree, assetPath);
                tree.name = treeEntry.name;

                // set defRefs
                tree.defRefs = resolvedDefRefs.TryGetValue(treeEntry.name, out var refs)
                    ? refs
                    : new List<StatDefinitionSO>();

                // serialize nodes as treeJson
                var nodes = treeEntry.nodes ?? new List<JsonStatNode>();
                var container = new TreeDataContainer { Nodes = nodes };
                tree.treeJson = JsonUtility.ToJson(container, true);

                // InheritsFrom set in Phase 4
                tree.InheritsFrom = null;

                EditorUtility.SetDirty(tree);
                treesCreated++;
                treeNameToAssetPath[treeEntry.name] = assetPath;

                Debug.Log($"[StatImporter] Created tree: {assetPath}");
            }

            // ---- Phase 4: Link InheritsFrom ----
            foreach (var treeEntry in importFile.trees)
            {
                if (string.IsNullOrWhiteSpace(treeEntry.inheritsFrom)) continue;

                // find child tree
                if (!treeNameToAssetPath.TryGetValue(treeEntry.name, out var childPath))
                    continue; // not in this batch (duplicate name or not in JSON)
                var childTree = AssetDatabase.LoadAssetAtPath<StatsTreeSO>(childPath);
                if (childTree == null) { errors.Add($"无法加载 '{treeEntry.name}'"); continue; }

                // find parent tree
                StatsTreeSO parentTree = null;

                // first check this batch
                if (treeNameToAssetPath.TryGetValue(treeEntry.inheritsFrom, out var parentPath))
                {
                    parentTree = AssetDatabase.LoadAssetAtPath<StatsTreeSO>(parentPath);
                }

                // fallback: global search by name
                if (parentTree == null)
                {
                    var allTreeGuids = AssetDatabase.FindAssets("t:StatsTreeSO");
                    foreach (var tGuid in allTreeGuids)
                    {
                        var tPath = AssetDatabase.GUIDToAssetPath(tGuid);
                        var t = AssetDatabase.LoadAssetAtPath<StatsTreeSO>(tPath);
                        if (t != null && t.name == treeEntry.inheritsFrom)
                        {
                            parentTree = t;
                            break;
                        }
                    }
                }

                if (parentTree == null)
                {
                    errors.Add($"Tree '{treeEntry.name}': inheritsFrom='{treeEntry.inheritsFrom}' 但未找到父树");
                    continue;
                }

                // cycle detection
                if (WouldCreateCycle(childTree, parentTree))
                {
                    errors.Add($"Tree '{treeEntry.name}': inheritsFrom='{treeEntry.inheritsFrom}' 会导致循环继承");
                    continue;
                }

                childTree.InheritsFrom = parentTree;
                EditorUtility.SetDirty(childTree);
                Debug.Log($"[StatImporter] Linked: {treeEntry.name} inheritsFrom {treeEntry.inheritsFrom}");
            }

            // ---- Phase 5: Persist ----
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[StatImporter] Done: defsCreated={defsCreated}, defsSkipped={defsSkipped}, treesCreated={treesCreated}, treesSkipped={treesSkipped}, errors={errors.Count}");
            return (defsCreated, defsSkipped, treesCreated, treesSkipped, errors);
        }

        /// <summary>
        /// 检查设置 parentTree 为 childTree 的 InheritsFrom 是否会产生循环。
        /// 复用 StatsTreeEditorWindow 的检测逻辑。
        /// </summary>
        private static bool WouldCreateCycle(StatsTreeSO node, StatsTreeSO proposedParent)
        {
            var visited = new HashSet<StatsTreeSO>();
            var current = proposedParent;
            while (current != null)
            {
                if (current == node) return true;
                if (!visited.Add(current)) return true; // already a cycle
                current = current.InheritsFrom;
            }
            return false;
        }

        /// <summary>
        /// 从 JSON 文件路径导入。
        /// </summary>
        public static (int defsCreated, int defsSkipped, int treesCreated, int treesSkipped, List<string> errors) ImportFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                return (0, 0, 0, 0, new List<string> { $"文件不存在: {jsonPath}" });

            var jsonText = File.ReadAllText(jsonPath);
            return ImportFromJson(jsonText);
        }
    }

    /// <summary>
    /// Stat 导入 Editor 窗口。
    /// 菜单: RedDust > Stat Import-Export
    /// </summary>
    public class StatImportWindow : EditorWindow
    {
        private const float Pad = 6f;

        private string _jsonPath = "Assets/Data/Stats/stats_all.json";
        private int _lastDefsCreated, _lastDefsSkipped;
        private int _lastTreesCreated, _lastTreesSkipped;
        private List<string> _lastErrors = new();

        [MenuItem("RedDust/Stat Import-Export", priority = 41)]
        public static void ShowWindow()
        {
            var window = GetWindow<StatImportWindow>("Stat Importer");
            window.minSize = new Vector2(520, 380);
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

            EditorGUILayout.LabelField("Stat Importer", EditorStyles.largeLabel);
            var subStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.gray }
            };
            EditorGUILayout.LabelField("L3_Stats · StatImportExport · JSON ↔ .asset", subStyle, GUILayout.Width(290));

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

            EditorGUILayout.LabelField("JSON 文件", EditorStyles.boldLabel);
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

            EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);

            if (File.Exists(_jsonPath))
            {
                StatImporter.StatExportFile preview = null;
                try
                {
                    preview = JsonUtility.FromJson<StatImporter.StatExportFile>(File.ReadAllText(_jsonPath));
                }
                catch { }

                if (preview != null)
                {
                    int defCount = preview.definitions?.Count ?? 0;
                    int treeCount = preview.trees?.Count ?? 0;

                    if (defCount + treeCount > 0)
                    {
                        // count new vs existing definitions
                        int newDefs = 0, existDefs = 0;
                        if (preview.definitions != null)
                        {
                            foreach (var entry in preview.definitions)
                            {
                                if (string.IsNullOrWhiteSpace(entry.id)) continue;
                                var cat = string.IsNullOrWhiteSpace(entry.category) ? "Attribute" : entry.category;
                                var assetPath = Path.Combine(StatImporter.DefinitionsRoot, cat, $"{entry.id}.asset").Replace('\\', '/');
                                if (File.Exists(assetPath)) existDefs++;
                                else newDefs++;
                            }
                        }

                        // count new vs existing trees
                        int newTrees = 0, existTrees = 0;
                        if (preview.trees != null)
                        {
                            foreach (var entry in preview.trees)
                            {
                                if (string.IsNullOrWhiteSpace(entry.name)) continue;
                                var dir = string.IsNullOrWhiteSpace(entry.directory) ? "" : entry.directory;
                                var treeDir = Path.Combine(StatImporter.StatsRoot, dir).Replace('\\', '/');
                                var assetPath = Path.Combine(treeDir, $"{entry.name}.asset").Replace('\\', '/');
                                if (File.Exists(assetPath)) existTrees++;
                                else newTrees++;
                            }
                        }

                        GUILayout.Space(4);
                        EditorGUILayout.LabelField(
                            $"<b>{defCount}</b> 定义 · <b>{treeCount}</b> 树 · v{preview.version} · {preview.description ?? "-"}",
                            new GUIStyle(EditorStyles.label) { richText = true });

                        EditorGUILayout.BeginHorizontal();
                        var green = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } };
                        var gray = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };

                        EditorGUILayout.LabelField($"新增定义 {newDefs}", green, GUILayout.Width(100));
                        EditorGUILayout.LabelField($"已存在 {existDefs}", gray, GUILayout.Width(80));
                        GUILayout.Space(12);
                        EditorGUILayout.LabelField($"新增树 {newTrees}", green, GUILayout.Width(100));
                        EditorGUILayout.LabelField($"已存在 {existTrees}", gray);
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.Space(4);
                        EditorGUILayout.LabelField("JSON 为空（无 definitions 也无 trees）", EditorStyles.miniLabel);
                    }
                }
                else
                {
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField("JSON 解析失败或格式错误", EditorStyles.miniLabel);
                }
            }
            else
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("文件不存在", EditorStyles.miniLabel);
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
            if (GUILayout.Button("导入", GUILayout.Height(32)))
            {
                (_lastDefsCreated, _lastDefsSkipped, _lastTreesCreated, _lastTreesSkipped, _lastErrors) =
                    StatImporter.ImportFromFile(_jsonPath);
                AssetDatabase.Refresh();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("导出", GUILayout.Height(32)))
            {
                var outPath = EditorUtility.SaveFilePanel("导出 Stat JSON", "Assets/Data/Stats", "stats_export", "json");
                if (!string.IsNullOrEmpty(outPath))
                {
                    StatImporter.ExportToFile(outPath);
                    EditorUtility.DisplayDialog("导出完成", $"已导出到:\n{outPath}", "确定");
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!hasFile)
            {
                var warnStyle = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(1f, 0.6f, 0.2f) } };
                EditorGUILayout.LabelField("  文件不存在，请选择 JSON 文件", warnStyle);
            }
        }

        private void DrawResult()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_lastDefsCreated + _lastDefsSkipped + _lastTreesCreated + _lastTreesSkipped == 0
                && _lastErrors.Count == 0)
            {
                EditorGUILayout.EndVertical();
                return;
            }
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            var okStyle = new GUIStyle(EditorStyles.label)
            { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } };
            var errStyle = new GUIStyle(EditorStyles.label)
            { normal = { textColor = new Color(0.9f, 0.3f, 0.2f) } };

            var hasErrors = _lastErrors.Count > 0;
            EditorGUILayout.LabelField(
                $"定义: 创建 {_lastDefsCreated} · 跳过 {_lastDefsSkipped}  |  " +
                $"树: 创建 {_lastTreesCreated} · 跳过 {_lastTreesSkipped}" +
                (hasErrors ? $"  |  错误 {_lastErrors.Count}" : ""),
                hasErrors ? errStyle : okStyle);

            if (hasErrors)
            {
                GUILayout.Space(4);
                foreach (var e in _lastErrors)
                    EditorGUILayout.LabelField($"  ⚠ {e}", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        private void PickFile()
        {
            var selected = EditorUtility.OpenFilePanel("选择 Stat 导入 JSON", "Assets/Data/Stats", "json");
            if (string.IsNullOrEmpty(selected)) return;

            var projectPath = Path.GetDirectoryName(Application.dataPath);
            _jsonPath = selected.StartsWith(projectPath!)
                ? selected.Substring(projectPath.Length + 1).Replace('\\', '/')
                : selected;
        }
    }
}
