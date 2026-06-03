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
    public class StatsTreeData : ScriptableObject
    {
        /// <summary>
        /// Parent tree in the inheritance chain. null = root tree.
        /// </summary>
        public StatsTreeData InheritsFrom;

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
    }
}
