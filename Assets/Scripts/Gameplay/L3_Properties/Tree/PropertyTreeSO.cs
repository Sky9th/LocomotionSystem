using System.Collections.Generic;
using System.Linq;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Properties
{
    /// <summary>
    /// Schema table for an entity type. Defines which properties exist (structure only, no values).
    /// Supports inheritance chain merging — child only adds, never overrides ancestor nodes.
    /// </summary>
    [CreateAssetMenu(fileName = "PropertyTree", menuName = "RedDust/Properties/Property Tree")]
    public class PropertyTreeSO : ScriptableObject
    {
        /// <summary>Parent template. null = root tree.</summary>
        public PropertyTreeSO InheritsFrom;

        /// <summary>JSON serialized PropertyTreeContainer.</summary>
        [TextArea(3, 20)]
        public string treeJson;

        /// <summary>
        /// Resolve the full property structure by walking the inheritance chain,
        /// merging all layers (union by NodeId, ancestor wins),
        /// building paths, and returning a Path → Def mapping.
        /// </summary>
        public Dictionary<string, PropertyDefSO> ResolveStructure()
        {
            var merged = MergeAllNodes(out _);

            // Build child index for O(n) traversal
            var childrenByParent = BuildChildrenIndex(merged);

            // Build paths from roots and collect leaf → Def
            var result = new Dictionary<string, PropertyDefSO>();
            var roots = merged.Values.Where(n => string.IsNullOrEmpty(n.ParentId)).ToList();

            foreach (var root in roots)
                BuildPath(root.NodeId, merged, childrenByParent, "", result);

            // Handle orphans (ParentId doesn't exist in merged)
            var processed = new HashSet<string>();
            foreach (var root in roots) CollectProcessed(root.NodeId, merged, childrenByParent, processed);

            foreach (var (nodeId, node) in merged)
            {
                if (processed.Contains(nodeId)) continue;
                if (string.IsNullOrEmpty(node.DefId)) continue; // orphan folder, skip

                Debug.LogWarning($"[PropertyTree] Orphan node '{nodeId}', ParentId '{node.ParentId}' not found. Treating as root.");
                result[nodeId] = ResolveDef(node.DefId);
            }

            return result;
        }

        /// <summary>
        /// Resolve all nodes (folders + leaves) from the full inheritance chain.
        /// Returns a merged dictionary keyed by NodeId. Used by the editor tree view.
        /// </summary>
        public Dictionary<string, PropertyNode> ResolveAllNodes()
        {
            return MergeAllNodes(out _);
        }

        /// <summary>
        /// Resolve all nodes and return which NodeIds were shadowed by ancestors
        /// (i.e., local nodes that were discarded because an ancestor already has the same NodeId).
        /// The editor uses this to mark conflicted nodes as non-local (inherited).
        /// </summary>
        public Dictionary<string, PropertyNode> ResolveAllNodes(out HashSet<string> ancestorConflicts)
        {
            return MergeAllNodes(out ancestorConflicts);
        }

        /// <summary>
        /// Collect inheritance layers and merge by NodeId (ancestor priority).
        /// Shared by ResolveStructure and ResolveAllNodes to avoid duplication.
        /// </summary>
        private Dictionary<string, PropertyNode> MergeAllNodes(out HashSet<string> ancestorConflicts)
        {
            var layers = new List<(PropertyTreeContainer container, int depth)>();
            CollectInheritedLayers(this, layers, new HashSet<PropertyTreeSO>());

            var merged = new Dictionary<string, PropertyNode>();
            var localNodeIds = new HashSet<string>(); // NodeIds from the local tree only
            ancestorConflicts = new HashSet<string>();

            // Determine local NodeIds (last layer = this tree)
            if (layers.Count > 0)
            {
                var (lastContainer, _) = layers[layers.Count - 1];
                foreach (var n in lastContainer.Nodes)
                    if (!string.IsNullOrEmpty(n.NodeId))
                        localNodeIds.Add(n.NodeId);
            }

            foreach (var (container, depth) in layers)
            {
                foreach (var node in container.Nodes)
                {
                    if (string.IsNullOrEmpty(node.NodeId)) continue;
                    if (merged.ContainsKey(node.NodeId))
                    {
                        // If a local node conflicts with an ancestor, record it
                        if (localNodeIds.Contains(node.NodeId))
                            ancestorConflicts.Add(node.NodeId);
                        // Warning suppressed — BuildCenterTree provides user-friendly diagnostics
                        continue;
                    }
                    merged[node.NodeId] = node;
                }
            }

            return merged;
        }

        /// <summary>
        /// Builds a ParentId → List of child PropertyNodes index for O(n) child lookup.
        /// </summary>
        private static Dictionary<string, List<PropertyNode>> BuildChildrenIndex(
            Dictionary<string, PropertyNode> merged)
        {
            var index = new Dictionary<string, List<PropertyNode>>();
            foreach (var node in merged.Values)
            {
                var key = node.ParentId ?? "";
                if (!index.TryGetValue(key, out var list))
                    index[key] = list = new List<PropertyNode>();
                list.Add(node);
            }
            return index;
        }

        // ---- private helpers ----

        private static void CollectInheritedLayers(
            PropertyTreeSO current,
            List<(PropertyTreeContainer, int)> result,
            HashSet<PropertyTreeSO> visited)
        {
            if (current == null) return;
            if (!visited.Add(current))
            {
                Debug.LogError($"[PropertyTree] Cycle detected in inheritance chain at '{current.name}'");
                return;
            }

            // Recurse to root first
            CollectInheritedLayers(current.InheritsFrom, result, visited);

            if (!string.IsNullOrEmpty(current.treeJson))
            {
                var container = JsonUtility.FromJson<PropertyTreeContainer>(current.treeJson);
                if (container?.Nodes is { Count: > 0 })
                    result.Add((container, result.Count));
            }
        }

        private static void BuildPath(
            string nodeId,
            Dictionary<string, PropertyNode> merged,
            Dictionary<string, List<PropertyNode>> childrenByParent,
            string parentPath,
            Dictionary<string, PropertyDefSO> result)
        {
            if (!merged.TryGetValue(nodeId, out var node)) return;

            var path = string.IsNullOrEmpty(parentPath) ? node.NodeId : $"{parentPath}/{node.NodeId}";

            // Leaf node → add to result
            if (!string.IsNullOrEmpty(node.DefId))
            {
                var def = ResolveDef(node.DefId);
                if (def != null)
                    result[path] = def;
            }

            // Recurse into children — O(1) lookup via index
            if (childrenByParent.TryGetValue(nodeId, out var children))
            {
                foreach (var child in children)
                    BuildPath(child.NodeId, merged, childrenByParent, path, result);
            }
        }

        private static void CollectProcessed(
            string nodeId,
            Dictionary<string, PropertyNode> merged,
            Dictionary<string, List<PropertyNode>> childrenByParent,
            HashSet<string> processed)
        {
            if (!processed.Add(nodeId)) return;
            if (!merged.TryGetValue(nodeId, out var node)) return;

            if (childrenByParent.TryGetValue(nodeId, out var children))
            {
                foreach (var child in children)
                    CollectProcessed(child.NodeId, merged, childrenByParent, processed);
            }
        }

        private static PropertyDefSO ResolveDef(string defId)
        {
            var def = GameService.Instance?.Assets.FindPropertyDef(defId);
            if (def == null)
                Debug.LogWarning($"[PropertyTree] DefId '{defId}' not found in Registry.");
            return def;
        }
    }
}
