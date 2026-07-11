using System;

namespace RedDust.Gameplay.Properties
{
    /// <summary>
    /// Tree node serialized into PropertyTreeSO.treeJson.
    /// NodeId is unique within a merged tree. ParentId references another node's NodeId, "" = root.
    /// DefId references PropertyDefSO.Id. "" = folder node.
    /// </summary>
    [Serializable]
    public class PropertyNode
    {
        /// <summary>Tree-unique id. Leaf node can use a custom name like "Combat_ATK".</summary>
        public string NodeId;

        /// <summary>Parent folder's NodeId. "" or null = root.</summary>
        public string ParentId;

        /// <summary>PropertyDefSO global Id. "" or null = folder node.</summary>
        public string DefId;
    }
}
