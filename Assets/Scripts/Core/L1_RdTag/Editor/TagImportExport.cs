using RedDust.Core.RdTag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core.RdTag.Editor
{
    /// <summary>
    /// RdTag JSON 导入工具。
    /// 从 JSON 文件批量创建 RdTagDefSO 资产。
    /// 支持增量导入（已存在的 Tag 跳过）。
    ///
    /// 模组支持：玩家/模组作者编写 JSON → 导入 → Unity 自动生成 .asset 文件。
    /// </summary>
    public static class RdTagImporter
    {
        private const string TagsRoot = "Assets/Data/Tags";

        [System.Serializable]
        public class TagEntry
        {
            public string name;
            public string parent;
            public string fullTag;
            public string description;
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
        public static (int created, int updated, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, updated = 0, skipped = 0;

            TagImportFile importFile;
            try
            {
                importFile = JsonUtility.FromJson<TagImportFile>(jsonText);
            }
            catch (Exception e)
            {
                errors.Add($"JSON 解析失败: {e.Message}");
                return (0, 0, 0, errors);
            }

            if (importFile?.tags == null || importFile.tags.Count == 0)
            {
                errors.Add("JSON 中没有 tags 数组或数组为空");
                return (0, 0, 0, errors);
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

                var existing = AssetDatabase.LoadAssetAtPath<RdTagDefSO>(assetPath);
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

                var tag = ScriptableObject.CreateInstance<RdTagDefSO>();
                AssetDatabase.CreateAsset(tag, assetPath);
                DataLabelTools.EnsureBootLabel(assetPath);
                tag.name = entry.name; // CreateAsset 后必须显式再设一次 name，否则 leafName 推导失败

                // 写入 description
                var descField = typeof(RdTagDefSO).GetField("description",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (descField != null && !string.IsNullOrEmpty(entry.description))
                    descField.SetValue(tag, entry.description);

                // 强制刷新（OnEnable 不可靠，cachedFullTag 非序列化）
                var rf = typeof(RdTagDefSO).GetMethod("AutoDeriveLeafName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                rf?.Invoke(tag, null);
                rf = typeof(RdTagDefSO).GetMethod("RefreshCache",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                rf?.Invoke(tag, null);

                created++;

                pathToEntry[assetPath] = entry;
                createdMap[assetPath] = entry.parent;

                Debug.Log($"[TagImporter] Created: {assetPath} → FullTag={tag.FullTag}");
            }

            // 第二轮：按依赖顺序设置 parent（多轮迭代，每轮只设置 parent 已就绪的）
            var refreshMethod = typeof(RdTagDefSO).GetMethod("RefreshCache",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var parentField = typeof(RdTagDefSO).GetField("parent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (parentField == null || refreshMethod == null)
            {
                errors.Add("无法通过反射访问 parent 字段或 RefreshCache 方法");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return (created, updated, skipped, errors);
            }

            // 第二-1轮：先把已有资产（FullTag 已正确）注入缓存
            var fullTagCache = new Dictionary<string, string>(); // FullTag → assetPath
            var pending = new List<string>();

            foreach (var assetPath in createdMap.Keys)
            {
                var entry = pathToEntry[assetPath];
                var tag = AssetDatabase.LoadAssetAtPath<RdTagDefSO>(assetPath);
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

            // 第二-2轮：按 parent 深度排序（按 parent 字符串中 dot 数量计算深度）
            int GetDepth(string assetPath)
            {
                var entry = pathToEntry[assetPath];
                if (string.IsNullOrWhiteSpace(entry.parent)) return 0;
                // parent="Damage.Physical" → 1 dot → depth=2
                return entry.parent.Count(c => c == '.') + 1;
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
                    var tag = AssetDatabase.LoadAssetAtPath<RdTagDefSO>(assetPath);
                    if (tag == null) { unresolved.Add(assetPath); continue; }

                    // 在缓存中查找 parent
                    var parentAssetPath = fullTagCache.TryGetValue(entry.parent, out var pap) ? pap : null;
                    RdTagDefSO parentSo = null;

                    if (parentAssetPath != null)
                    {
                        parentSo = AssetDatabase.LoadAssetAtPath<RdTagDefSO>(parentAssetPath);
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
            return (created, updated, skipped, errors);
        }

        /// <summary>
        /// 导出所有 RdTagDefSO 资产为 JSON 字符串。
        /// </summary>
        public static string ExportToJson()
        {
            var export = new TagImportFile
            {
                version = "1.0",
                description = "Exported from Unity Editor",
                tags = new List<TagEntry>()
            };

            var guids = AssetDatabase.FindAssets("t:RdTagDefSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tag = AssetDatabase.LoadAssetAtPath<RdTagDefSO>(path);
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
        public static (int created, int updated, int skipped, List<string> errors) ImportFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                return (0, 0, 0, new List<string> { $"文件不存在: {jsonPath}" });

            var jsonText = File.ReadAllText(jsonPath);
            return ImportFromJson(jsonText);
        }

        /// <summary>
        /// 在已加载的所有 RdTagDefSO 中按 FullTag 查找。
        /// </summary>
        private static RdTagDefSO FindTagByFullTag(string fullTag)
        {
            var guids = AssetDatabase.FindAssets("t:RdTagDefSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tag = AssetDatabase.LoadAssetAtPath<RdTagDefSO>(path);
                if (tag != null && tag.FullTag == fullTag)
                    return tag;
            }
            return null;
        }
    }

    /// <summary>
    /// RdTag Import-Export 窗口。使用共享 EditorImportExport 组件。
    /// </summary>
    public class RdTagImportWindow : EditorWindow
    {
        private string _filePath = "Assets/Data/Tags/tags_all.json";
        private string _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Tag Import-Export", priority = 26)]
        public static void Open()
        {
            var window = GetWindow<RdTagImportWindow>("Tag Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Tag Import-Export",
                subtitle: "L1_Core · RdTag · JSON ↔ .asset",
                defaultDir: "Assets/Data/Tags",
                fileExtension: "json",
                defaultFileName: "tags_export",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path =>
                {
                    return RdTagImporter.ImportFromFile(path);
                },
                onExport: path => File.WriteAllText(path, RdTagImporter.ExportToJson())
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            RdTagImporter.TagImportFile preview;
            try { preview = JsonUtility.FromJson<RdTagImporter.TagImportFile>(File.ReadAllText(filePath)); }
            catch { return null; }
            if (preview?.tags == null || preview.tags.Count == 0) return null;

            int newCount = 0, existCount = 0;
            foreach (var entry in preview.tags)
            {
                var d = Path.Combine("Assets/Data/Tags", entry.Directory).Replace('\\', '/');
                if (File.Exists(Path.Combine(d, $"{entry.name}.asset").Replace('\\', '/'))) existCount++;
                else newCount++;
            }

            return $"<b>{preview.tags.Count}</b> entries\n" +
                   $"v{preview.version} · {preview.description ?? "-"}\n" +
                   $"<color=#66CC66>New {newCount}</color>  <color=#888888>Exist {existCount}</color>";
        }
    }
}
