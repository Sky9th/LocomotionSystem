using System;
using System.Collections.Generic;
using System.IO;
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
            /// <summary>文件名（不含扩展名），如 "Tag_Ability"</summary>
            public string name;

            /// <summary>父标签 FullTag，如 "Ability"。null 或空=根标签</summary>
            public string parent;

            /// <summary>可选：子目录路径，相对于 Assets/Data/Tags/。如 "Ability/Melee/Blade"</summary>
            public string directory;
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
                if (!string.IsNullOrWhiteSpace(entry.directory))
                    dir = Path.Combine(TagsRoot, entry.directory).Replace('\\', '/');

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
                created++;

                pathToEntry[assetPath] = entry;
                createdMap[assetPath] = entry.parent;

                Debug.Log($"[TagImporter] Created: {assetPath}");
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

            var remaining = new HashSet<string>(createdMap.Keys);
            var fullTagCache = new Dictionary<string, string>(); // assetPath → FullTag after parent set
            int lastRemaining;
            var maxIterations = 20; // 安全上限

            do
            {
                lastRemaining = remaining.Count;
                var resolvedThisRound = new List<string>();

                foreach (var assetPath in remaining)
                {
                    var entry = pathToEntry[assetPath];
                    var tag = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(assetPath);
                    if (tag == null) continue;

                    // 根标签：无 parent
                    if (string.IsNullOrWhiteSpace(entry.parent))
                    {
                        fullTagCache[assetPath] = tag.FullTag;
                        resolvedThisRound.Add(assetPath);
                        continue;
                    }

                    // 查找 parent SO
                    GameplayTagDefinitionSO parentSo = null;

                    // 先在所有已解析的缓存中通过 FullTag 查找
                    foreach (var kv in fullTagCache)
                    {
                        if (kv.Value == entry.parent)
                        {
                            parentSo = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(kv.Key);
                            break;
                        }
                    }

                    // 如果缓存中没找到，尝试全局搜索（已有资产）
                    if (parentSo == null)
                    {
                        parentSo = FindTagByFullTag(entry.parent);
                    }

                    if (parentSo == null)
                        continue; // parent 还没就绪，等下一轮

                    // 设置 parent 并刷新缓存
                    parentField.SetValue(tag, parentSo);
                    refreshMethod.Invoke(tag, null);
                    EditorUtility.SetDirty(tag);

                    fullTagCache[assetPath] = tag.FullTag;
                    resolvedThisRound.Add(assetPath);

                    Debug.Log($"[TagImporter] Linked: {tag.FullTag} → parent={entry.parent}");
                }

                foreach (var resolved in resolvedThisRound)
                    remaining.Remove(resolved);

                if (remaining.Count == lastRemaining && remaining.Count > 0)
                {
                    // 没有进展但还有剩余 → parent 找不到
                    foreach (var assetPath in remaining)
                    {
                        var entry = pathToEntry[assetPath];
                        if (!string.IsNullOrWhiteSpace(entry.parent))
                            errors.Add($"找不到 parent '{entry.parent}' (tag={entry.name}, path={assetPath})");
                    }
                    break;
                }

                maxIterations--;
            }
            while (remaining.Count > 0 && maxIterations > 0);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TagImporter] Done: created={created}, skipped={skipped}, errors={errors.Count}");
            return (created, skipped, errors);
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
        private string jsonPath = "Assets/Data/Tags/tags_import.json";

        [MenuItem("RedDust/Import GameplayTags from JSON")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameplayTagImportWindow>("Tag Importer");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("GameplayTag JSON 导入", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("JSON 文件路径 (相对于项目根目录)");
            jsonPath = EditorGUILayout.TextField(jsonPath);

            if (GUILayout.Button("选择 JSON 文件", GUILayout.Height(25)))
            {
                var selected = EditorUtility.OpenFilePanel("选择 Tag 导入 JSON", "Assets/Data/Tags", "json");
                if (!string.IsNullOrEmpty(selected))
                {
                    // 转为相对路径
                    var projectPath = Path.GetDirectoryName(Application.dataPath);
                    if (selected.StartsWith(projectPath))
                        jsonPath = selected.Substring(projectPath.Length + 1).Replace('\\', '/');
                    else
                        jsonPath = selected;
                }
            }

            EditorGUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(jsonPath));
            if (GUILayout.Button("导入", GUILayout.Height(35)))
            {
                if (!File.Exists(jsonPath))
                {
                    EditorUtility.DisplayDialog("错误", $"文件不存在:\n{jsonPath}", "确定");
                    return;
                }

                var (created, skipped, errors) = GameplayTagImporter.ImportFromFile(jsonPath);

                var msg = $"创建: {created} 个新 Tag\n跳过: {skipped} 个已存在";
                if (errors.Count > 0)
                    msg += $"\n\n错误 ({errors.Count}):\n" + string.Join("\n", errors);

                EditorUtility.DisplayDialog("导入完成", msg, "确定");
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
