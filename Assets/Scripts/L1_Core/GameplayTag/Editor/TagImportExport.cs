using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// GameplayTag JSON 导入工具。
    /// 从 JSON 文件批量创建 GameplayTagDefinitionSO 资产。
    /// 支持增量导入（已存在的 Tag 跳过）。
    ///
    /// 模组支持：玩家/模组作者编写 JSON → 导入 → Unity 自动生成 .asset 文件。
    /// </summary>
    public static class GameplayTagImporter
    {
        private const string TagsRoot = "Assets/Data/Tags";

        [System.Serializable]
        public class TagEntry
        {
            public string name;
            public string parent;
            public string fullTag;
            public string Directory => string.IsNullOrEmpty(parent) ? "" : parent.Replace('.', '/');
        }

        [System.Serializable]
        public class TagImportFile
        {
            public string version = "1.0";
            public string description;
            public List<TagEntry> tags = new();
        }

        /// <summary>
        /// 从 JSON 文本导入 Tag 资产。
        /// </summary>
        /// <returns>(created, skipped, errors)</returns>
        public static (int created, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, skipped = 0;

            TagImportFile importFile;
            try
            {
                importFile = JsonUtility.FromJson<TagImportFile>(jsonText);
            }
            catch (Exception e)
            {
                errors.Add($"JSON 解析失败: {e.Message}");
                return (0, 0, errors);
            }

            if (importFile?.tags == null || importFile.tags.Count == 0)
            {
                errors.Add("JSON 中没有 tags 数组或数组为空");
                return (0, 0, errors);
            }

            // 第一轮：创建全部资产文件
            var createdMap = new Dictionary<string, string>(); // assetPath → entry.parent (for later)
            var pathToEntry = new Dictionary<string, TagEntry>(); // assetPath → entry

            foreach (var entry in importFile.tags)
            {
                if (string.IsNullOrWhiteSpace(entry.name))
                {
                    errors.Add($"跳过: name 为空");
                    skipped++;
                    continue;
                }

                var dir = TagsRoot;
                if (!string.IsNullOrWhiteSpace(entry.Directory))
                    dir = Path.Combine(TagsRoot, entry.Directory).Replace('\\', '/');

                var assetPath = Path.Combine(dir, $"{entry.name}.asset").Replace('\\', '/');

                var existing = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(assetPath);
                if (existing != null)
                {
                    skipped++;
                    pathToEntry[assetPath] = entry;
                    createdMap[assetPath] = entry.parent;
                    continue;
                }

                var dirPath = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                var tag = ScriptableObject.CreateInstance<GameplayTagDefinitionSO>();
                AssetDatabase.CreateAsset(tag, assetPath);
                tag.name = entry.name; // CreateAsset 后必须显式再设一次 name，否则 leafName 推导失败

                // 强制刷新（OnEnable 不可靠，cachedFullTag 非序列化）
                var rf = typeof(GameplayTagDefinitionSO).GetMethod("AutoDeriveLeafName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                rf?.Invoke(tag, null);
                rf = typeof(GameplayTagDefinitionSO).GetMethod("RefreshCache",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                rf?.Invoke(tag, null);

                created++;

                pathToEntry[assetPath] = entry;
                createdMap[assetPath] = entry.parent;

                Debug.Log($"[TagImporter] Created: {assetPath} → FullTag={tag.FullTag}");
            }

            // 第二轮：按依赖顺序设置 parent（多轮迭代，每轮只设置 parent 已就绪的）
            var refreshMethod = typeof(GameplayTagDefinitionSO).GetMethod("RefreshCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var parentField = typeof(GameplayTagDefinitionSO).GetField("parent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (parentField == null || refreshMethod == null)
            {
                errors.Add("无法通过反射访问 parent 字段或 RefreshCache 方法");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return (created, skipped, errors);
            }

            // 第二-1轮：先把已有资产（FullTag 已正确）注入缓存
            var fullTagCache = new Dictionary<string, string>(); // FullTag → assetPath
            var pending = new List<string>();

            foreach (var assetPath in createdMap.Keys)
            {
                var entry = pathToEntry[assetPath];
                var tag = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(assetPath);
                if (tag == null) continue;

                if (string.IsNullOrWhiteSpace(entry.parent))
                {
                    // 根标签：直接缓存
                    fullTagCache[tag.FullTag] = assetPath;
                    Debug.Log($"[TagImporter] Root cached: {tag.FullTag}");
                }
                else
                {
                    pending.Add(assetPath);
                }
            }

            // 第二-2轮：按 parent 深度排序（计算每个条目的依赖链长度）
            int GetDepth(string assetPath)
            {
                var entry = pathToEntry[assetPath];
                if (string.IsNullOrWhiteSpace(entry.parent)) return 0;
                // 在 createdMap 中查找 parent 条目
                foreach (var kv in createdMap)
                {
                    var e = pathToEntry[kv.Key];
                    var tag = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(kv.Key);
                    if (tag != null && tag.FullTag == entry.parent)
                        return 1 + GetDepth(kv.Key);
                }
                // parent 不在本批次中（已有资产），深度=1
                return 1;
            }

            pending.Sort((a, b) => GetDepth(a).CompareTo(GetDepth(b)));

            // 第二-3轮：多轮迭代直到全部解析或停滞
            int lastPending;
            do
            {
                lastPending = pending.Count;
                var unresolved = new List<string>();

                foreach (var assetPath in pending)
                {
                    var entry = pathToEntry[assetPath];
                    var tag = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(assetPath);
                    if (tag == null) { unresolved.Add(assetPath); continue; }

                    // 在缓存中查找 parent
                    var parentAssetPath = fullTagCache.TryGetValue(entry.parent, out var pap) ? pap : null;
                    GameplayTagDefinitionSO parentSo = null;

                    if (parentAssetPath != null)
                    {
                        parentSo = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(parentAssetPath);
                    }

                    // fallback: 全局搜索（已有资产）
                    if (parentSo == null)
                        parentSo = FindTagByFullTag(entry.parent);

                    if (parentSo == null)
                    {
                        unresolved.Add(assetPath);
                        continue;
                    }

                    parentField.SetValue(tag, parentSo);
                    refreshMethod.Invoke(tag, null);
                    EditorUtility.SetDirty(tag);
                    fullTagCache[tag.FullTag] = assetPath;
                    Debug.Log($"[TagImporter] Linked: {tag.FullTag} ← parent={entry.parent}");
                }

                pending = unresolved;
            }
            while (pending.Count > 0 && pending.Count < lastPending);

            // 剩余的无法解析
            foreach (var assetPath in pending)
            {
                var entry = pathToEntry[assetPath];
                if (!string.IsNullOrWhiteSpace(entry.parent))
                    errors.Add($"找不到 parent '{entry.parent}' (tag={entry.name})");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TagImporter] Done: created={created}, skipped={skipped}, errors={errors.Count}");
            return (created, skipped, errors);
        }

        /// <summary>
        /// 导出所有 GameplayTagDefinitionSO 资产为 JSON 字符串。
        /// </summary>
        public static string ExportToJson()
        {
            var export = new TagImportFile
            {
                version = "1.0",
                description = "Exported from Unity Editor",
                tags = new List<TagEntry>()
            };

            var guids = AssetDatabase.FindAssets("t:GameplayTagDefinitionSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tag = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(path);
                if (tag == null) continue;

                // 不走 cachedFullTag（非序列化，可能因加载顺序错误），直接遍历 parent 链
                var chain = new List<string>();
                var p = tag.Parent;
                while (p != null)
                {
                    chain.Add(p.LeafName);
                    p = p.Parent;
                }
                chain.Reverse();
                chain.Add(tag.LeafName);
                var computedFullTag = string.Join(".", chain);

                export.tags.Add(new TagEntry
                {
                    name = tag.name,
                    parent = tag.Parent != null ? string.Join(".", chain.Take(chain.Count - 1)) : null,
                    fullTag = computedFullTag
                });
            }

            // 按 parent 深度排序（根→叶）
            export.tags.Sort((a, b) =>
            {
                var dA = string.IsNullOrEmpty(a.parent) ? 0 : a.parent.Split('.').Length;
                var dB = string.IsNullOrEmpty(b.parent) ? 0 : b.parent.Split('.').Length;
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
            Debug.Log($"[TagImporter] Exported {jsonPath}");
        }

        /// <summary>
        /// 从 JSON 文件路径导入。
        /// </summary>
        public static (int created, int skipped, List<string> errors) ImportFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                return (0, 0, new List<string> { $"文件不存在: {jsonPath}" });

            var jsonText = File.ReadAllText(jsonPath);
            return ImportFromJson(jsonText);
        }

        /// <summary>
        /// 在已加载的所有 GameplayTagDefinitionSO 中按 FullTag 查找。
        /// </summary>
        private static GameplayTagDefinitionSO FindTagByFullTag(string fullTag)
        {
            var guids = AssetDatabase.FindAssets("t:GameplayTagDefinitionSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tag = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(path);
                if (tag != null && tag.FullTag == fullTag)
                    return tag;
            }
            return null;
        }
    }

    /// <summary>
    /// GameplayTag 导入 Editor 窗口。
    /// 菜单: RedDust > Import GameplayTags from JSON
    /// </summary>
    public class GameplayTagImportWindow : EditorWindow
    {
        private const float Pad = 6f;

        private string _jsonPath = "Assets/Data/Tags/tags_all.json";
        private int _lastCreated, _lastSkipped;
        private List<string> _lastErrors = new();

        [MenuItem("RedDust/Tag Import-Export", priority = 21)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameplayTagImportWindow>("Tag Importer");
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

            EditorGUILayout.LabelField("GameplayTag Importer", EditorStyles.largeLabel);
            var subStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.gray }
            };
            EditorGUILayout.LabelField("L1_Core · GameplayTag · JSON → .asset", subStyle, GUILayout.Width(260));

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
                GameplayTagImporter.TagImportFile preview = null;
                try { preview = JsonUtility.FromJson<GameplayTagImporter.TagImportFile>(File.ReadAllText(_jsonPath)); } catch { }

                if (preview?.tags != null && preview.tags.Count > 0)
                {
                    int newCount = 0, existCount = 0;
                    foreach (var entry in preview.tags)
                    {
                        var d = Path.Combine("Assets/Data/Tags", entry.Directory).Replace('\\', '/');
                        if (File.Exists(Path.Combine(d, $"{entry.name}.asset").Replace('\\', '/'))) existCount++;
                        else newCount++;
                    }
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField($"<b>{preview.tags.Count}</b> 条目 · v{preview.version} · {preview.description ?? "-"}",
                        new GUIStyle(EditorStyles.label) { richText = true });
                    EditorGUILayout.BeginHorizontal();
                    var g = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } };
                    var s = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };
                    EditorGUILayout.LabelField($"新增 {newCount}", g, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"已存在 {existCount}", s);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField("JSON 为空或格式错误", EditorStyles.miniLabel);
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
                (_lastCreated, _lastSkipped, _lastErrors) = GameplayTagImporter.ImportFromFile(_jsonPath);
                AssetDatabase.Refresh();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("导出", GUILayout.Height(32)))
            {
                var outPath = EditorUtility.SaveFilePanel("导出 GameplayTag JSON", "Assets/Data/Tags", "tags_export", "json");
                if (!string.IsNullOrEmpty(outPath))
                {
                    GameplayTagImporter.ExportToFile(outPath);
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
            if (_lastCreated + _lastSkipped == 0 && _lastErrors.Count == 0)
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

            EditorGUILayout.LabelField(
                $"创建 {_lastCreated} · 跳过 {_lastSkipped}" +
                (_lastErrors.Count > 0 ? $" · 错误 {_lastErrors.Count}" : ""),
                _lastErrors.Count > 0 ? errStyle : okStyle);

            if (_lastErrors.Count > 0)
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
            var selected = EditorUtility.OpenFilePanel("选择 Tag 导入 JSON", "Assets/Data/Tags", "json");
            if (string.IsNullOrEmpty(selected)) return;

            var projectPath = Path.GetDirectoryName(Application.dataPath);
            _jsonPath = selected.StartsWith(projectPath!)
                ? selected.Substring(projectPath.Length + 1).Replace('\\', '/')
                : selected;
        }
    }
}
