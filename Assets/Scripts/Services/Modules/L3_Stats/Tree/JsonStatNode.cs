using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Stats
{
    /// <summary>
    /// Single node in the JSON stat tree. Serialized into StatsTreeData.treeJson.
    /// Id is unique within a tree. Path and Depth are computed at runtime.
    /// </summary>
    [Serializable]
    public class JsonStatNode
    {
        public string Id;
        public bool IsEnabled = true;
        public bool IsFolder;
        public bool IsOverride;             // true = overrides ancestor node at same Path
        public string[] Children;           // child Ids (folders only)

        /// <summary>Index into StatsTreeData.defRefs. -1 = unassigned.</summary>
        public int Def = -1;

        /// <summary>
        /// Override value. <see cref="float.MinValue"/> = use Def.Default.
        /// Sentinel chosen because MinValue is never a real stat value.
        /// </summary>
        public float OverrideValue = float.MinValue;

        // -- runtime only, not serialized --
        [NonSerialized] public string Path;                // "Attributes/Core/Strength"
        [NonSerialized] public StatDefinitionSO DefRef;    // resolved from defRefs[Def]
        [NonSerialized] public int Depth;                   // inheritance depth, Base=0
    }

    /// <summary>
    /// Wrapper for JSON serialization. StatsTreeData.treeJson stores
    /// JsonUtility.ToJson(TreeDataContainer).
    /// </summary>
    [Serializable]
    public class TreeDataContainer
    {
        public List<JsonStatNode> Nodes = new();
    }
}
