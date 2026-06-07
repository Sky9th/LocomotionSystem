#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core.Editor
{
    /// <summary>
    /// 标签树数据模型。扫描 AssetDatabase 中所有 GameplayTagDefinitionSO，
    /// 按 parent 引用构建多叉树，提供查找和搜索。
    /// </summary>
    public class TagTreeModel
    {
        public Dictionary<string, GameplayTagDefinitionSO> AllTags = new();
        public Dictionary<string, TagNode> NodeIndex = new();
        public List<TagNode> Roots = new();
        public bool HasCycle { get; private set; }
        public int TotalCount => NodeIndex.Count;

        // ── 扫描构建 ──
        public void Refresh()
        {
            AllTags.Clear();
            NodeIndex.Clear();
            Roots.Clear();
            HasCycle = false;

            var guids = AssetDatabase.FindAssets("t:GameplayTagDefinitionSO");
            var tagList = new List<GameplayTagDefinitionSO>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tag = AssetDatabase.LoadAssetAtPath<GameplayTagDefinitionSO>(path);
                if (tag == null || string.IsNullOrEmpty(tag.FullTag)) continue;
                AllTags[tag.FullTag] = tag;
                tagList.Add(tag);
            }

            // 先建所有节点
            foreach (var tag in tagList)
            {
                var node = new TagNode
                {
                    LeafName = tag.LeafName,
                    FullTag = tag.FullTag,
                    Depth = tag.Depth,
                    Asset = tag
                };
                NodeIndex[tag.FullTag] = node;
            }

            // 连接父子关系
            foreach (var tag in tagList)
            {
                var node = NodeIndex[tag.FullTag];
                if (tag.Parent != null && AllTags.TryGetValue(tag.Parent.FullTag, out var parentTag))
                {
                    var parentNode = NodeIndex[parentTag.FullTag];
                    node.Parent = parentNode;
                    parentNode.Children.Add(node);
                }
            }

            // 收集根 + 循环检测
            var visited = new HashSet<GameplayTagDefinitionSO>();
            foreach (var tag in tagList)
            {
                if (tag.Parent == null)
                {
                    if (!HasCycleInChain(tag, visited))
                        Roots.Add(NodeIndex[tag.FullTag]);
                }
            }

            // 排序
            Roots.Sort((a, b) => string.CompareOrdinal(a.LeafName, b.LeafName));
            SortChildrenRecursive(Roots);
        }

        private bool HasCycleInChain(GameplayTagDefinitionSO start, HashSet<GameplayTagDefinitionSO> visited)
        {
            var current = start;
            var path = new HashSet<GameplayTagDefinitionSO>();
            while (current != null)
            {
                if (!path.Add(current))
                {
                    HasCycle = true;
                    return true;
                }
                current = current.Parent;
            }
            return false;
        }

        private static void SortChildrenRecursive(List<TagNode> nodes)
        {
            foreach (var n in nodes)
            {
                n.Children.Sort((a, b) => string.CompareOrdinal(a.LeafName, b.LeafName));
                SortChildrenRecursive(n.Children);
            }
        }

        // ── 查找 ──
        public TagNode Find(string fullTag)
        {
            NodeIndex.TryGetValue(fullTag ?? "", out var node);
            return node;
        }

        // ── 搜索 ──
        public List<TagNode> Search(string query, string rootFilter = null)
        {
            var results = new List<TagNode>();
            if (string.IsNullOrEmpty(query)) return results;

            var q = query.ToLowerInvariant();

            foreach (var kv in NodeIndex)
            {
                if (!kv.Value.FullTag.ToLowerInvariant().Contains(q)
                    && !kv.Value.LeafName.ToLowerInvariant().Contains(q))
                    continue;

                if (!string.IsNullOrEmpty(rootFilter))
                {
                    var filter = rootFilter.TrimEnd('.');
                    if (!kv.Value.FullTag.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                results.Add(kv.Value);
            }

            results.Sort((a, b) =>
            {
                int Score(TagNode n)
                {
                    var ft = n.FullTag.ToLowerInvariant();
                    var ln = n.LeafName.ToLowerInvariant();
                    if (ft == q) return 0;
                    if (ft.StartsWith(q)) return 1;
                    if (ln.StartsWith(q)) return 2;
                    return 3;
                }
                return Score(a).CompareTo(Score(b));
            });

            return results;
        }

        // ── 获取缺失祖先 ──
        public List<string> GetMissingAncestors(string fullTag)
        {
            var missing = new List<string>();
            var segments = fullTag.Split('.');
            var accumulated = "";
            for (int i = 0; i < segments.Length - 1; i++)
            {
                accumulated = i == 0 ? segments[i] : $"{accumulated}.{segments[i]}";
                if (!NodeIndex.ContainsKey(accumulated))
                    missing.Add(accumulated);
            }
            return missing;
        }

    }
}
#endif
