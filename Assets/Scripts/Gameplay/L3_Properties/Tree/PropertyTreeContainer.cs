using System;
using System.Collections.Generic;

namespace RedDust.Gameplay.Properties
{
    /// <summary>
    /// JSON serialization wrapper for a list of PropertyNode.
    /// PropertyTreeSO.treeJson = JsonUtility.ToJson(container).
    /// </summary>
    [Serializable]
    public class PropertyTreeContainer
    {
        public List<PropertyNode> Nodes = new();
    }
}
