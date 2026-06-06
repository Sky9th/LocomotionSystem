using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Stats
{
    /// <summary>
    /// ScriptableObject that holds a stat tree definition as JSON.
    /// Replaces the old StatsTreeSO + StatsNodeSO approach.
    ///
    /// Each tree has its own JSON (treeJson) and a flat Def lookup (defRefs).
    /// Inheritance is resolved at load time by walking InheritsFrom upward.
    /// </summary>
    [CreateAssetMenu(fileName = "StatsTreeData", menuName = "RedDust/Stats/Stats Tree Data")]
    public class StatsTreeSO : ScriptableObject
    {
        /// <summary>
        /// Parent tree in the inheritance chain. null = root tree.
        /// </summary>
        public StatsTreeSO InheritsFrom;

        /// <summary>
        /// JSON serialized TreeDataContainer. Use DeserializeNodes() to read.
        /// </summary>
        [TextArea(3, 20)]
        public string treeJson;

        /// <summary>
        /// Def lookup table. Indices are stable (only append, never remove).
        /// JsonStatNode.Def references an index into this list.
        /// </summary>
        public List<StatDefinitionSO> defRefs = new();

        /// <summary>
        /// Resolve the full stat tree into StatInstance leaves.
        /// Walks inheritance chain (base first), merges layers by Id,
        /// builds paths from ParentId, skips folders.
        /// </summary>
        public IReadOnlyList<StatInstance> Resolve()
        {
            // 1. Collect inherited layers (base → child)
            var layers = new List<(TreeDataContainer container, StatsTreeSO source, int depth)>();
            CollectInheritedLayers(this, layers, new HashSet<StatsTreeSO>());

            // 2. Merge layers by Id (later layer overrides earlier)
            var merged = new List<JsonStatNode>();
            foreach (var (container, source, depth) in layers)
            {
                foreach (var node in container.Nodes)
                {
                    // Resolve DefRef from source's defRefs
                    if (node.Def >= 0 && node.Def < source.defRefs.Count)
                        node.DefRef = source.defRefs[node.Def];
                }
                MergeLayer(container.Nodes, merged, depth);
            }

            // 3. Build paths from roots
            RefreshPaths(merged);

            // 4. Extract leaves → StatInstance
            var instances = new List<StatInstance>();
            foreach (var node in merged)
            {
                if (!node.IsEnabled || node.IsFolder) continue;
                if (node.DefRef == null) continue;

                float value = node.OverrideValue != float.MinValue ? node.OverrideValue : node.DefRef.Default;
                instances.Add(new StatInstance(node.DefRef, value) { Path = node.Path });
            }

            return instances;
        }

        private static void CollectInheritedLayers(
            StatsTreeSO current,
            List<(TreeDataContainer, StatsTreeSO, int)> result,
            HashSet<StatsTreeSO> visited)
        {
            if (current == null) return;
            if (!visited.Add(current)) return; // cycle detected, skip

            // Recurse to root first
            CollectInheritedLayers(current.InheritsFrom, result, visited);

            // Process this level
            if (!string.IsNullOrEmpty(current.treeJson))
            {
                var container = JsonUtility.FromJson<TreeDataContainer>(current.treeJson);
                if (container?.Nodes is { Count: > 0 })
                    result.Add((container, current, result.Count));
            }
        }

        /// <summary>Merge by Id. Same Id → override, no match → append.</summary>
        private static void MergeLayer(List<JsonStatNode> source, List<JsonStatNode> target, int depth)
        {
            foreach (var node in source)
            {
                if (string.IsNullOrEmpty(node.Id)) continue;

                node.Depth = depth;
                var existingIdx = target.FindIndex(n => n.Id == node.Id);
                if (existingIdx >= 0)
                {
                    node.IsOverride = true;
                    // Inherit ParentId if empty
                    if (string.IsNullOrEmpty(node.ParentId))
                        node.ParentId = target[existingIdx].ParentId;
                    target[existingIdx] = node;
                }
                else
                {
                    target.Add(node);
                }
            }
        }

        /// <summary>Rebuild Path from roots (ParentId empty/null) downward.</summary>
        private static void RefreshPaths(List<JsonStatNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (string.IsNullOrEmpty(n.ParentId))
                    BuildPath(n, nodes, "");
            }
        }

        private static void BuildPath(JsonStatNode node, List<JsonStatNode> allNodes, string parentPath)
        {
            node.Path = string.IsNullOrEmpty(parentPath) ? node.Id : $"{parentPath}/{node.Id}";

            // Recurse into children
            foreach (var child in allNodes)
            {
                if (child.ParentId == node.Id)
                    BuildPath(child, allNodes, node.Path);
            }
        }
    }
}
