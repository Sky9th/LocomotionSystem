using System.Collections.Generic;
using System.Linq;
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
            // 1. Collect layers from root to leaf
            var layers = new List<(PropertyTreeContainer container, int depth)>();
            CollectInheritedLayers(this, layers, new HashSet<PropertyTreeSO>());

            // 2. Merge: union by NodeId, ancestor priority
            var merged = new Dictionary<string, PropertyNode>();
            foreach (var (container, depth) in layers)
            {
                foreach (var node in container.Nodes)
                {
                    if (string.IsNullOrEmpty(node.NodeId)) continue;

                    if (merged.ContainsKey(node.NodeId))
                    {
                        Debug.LogWarning($"[PropertyTree] NodeId conflict: '{node.NodeId}' at depth {depth}, keeping ancestor.");
                        continue;
                    }
                    merged[node.NodeId] = node;
                }
            }

            // 3. Build paths from roots and collect leaf → Def
            var result = new Dictionary<string, PropertyDefSO>();
            var roots = merged.Values.Where(n => string.IsNullOrEmpty(n.ParentId)).ToList();

            foreach (var root in roots)
                BuildPath(root.NodeId, merged, "", result);

            // 4. Handle orphans (ParentId doesn't exist in merged)
            var processed = new HashSet<string>();
            foreach (var root in roots) CollectProcessed(root.NodeId, merged, processed);

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
            var layers = new List<(PropertyTreeContainer container, int depth)>();
            CollectInheritedLayers(this, layers, new HashSet<PropertyTreeSO>());

            var merged = new Dictionary<string, PropertyNode>();
            foreach (var (container, depth) in layers)
            {
                foreach (var node in container.Nodes)
                {
                    if (string.IsNullOrEmpty(node.NodeId)) continue;
                    if (merged.ContainsKey(node.NodeId))
                    {
                        Debug.LogWarning($"[PropertyTree] NodeId conflict: '{node.NodeId}' at depth {depth}, keeping ancestor.");
                        continue;
                    }
                    merged[node.NodeId] = node;
                }
            }

            return merged;
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

            // Recurse into children
            var children = merged.Values.Where(n => n.ParentId == nodeId).ToList();
            foreach (var child in children)
                BuildPath(child.NodeId, merged, path, result);
        }

        private static void CollectProcessed(
            string nodeId,
            Dictionary<string, PropertyNode> merged,
            HashSet<string> processed)
        {
            if (!processed.Add(nodeId)) return;
            if (!merged.TryGetValue(nodeId, out var node)) return;

            var children = merged.Values.Where(n => n.ParentId == nodeId);
            foreach (var child in children)
                CollectProcessed(child.NodeId, merged, processed);
        }

        private static PropertyDefSO ResolveDef(string defId)
        {
            var def = PropertyDefinitionRegistry.FindById(defId);
            if (def == null)
                Debug.LogWarning($"[PropertyTree] DefId '{defId}' not found in Registry.");
            return def;
        }
    }
}
