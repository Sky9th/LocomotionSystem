#if UNITY_EDITOR
using System.Collections.Generic;

namespace RedDust.Gameplay.Properties.Editor
{
    /// <summary>
    /// Tree item node for the PropertyTree list. Represents one PropertyTreeSO
    /// within the inheritance hierarchy. Mirrors AbilityTreeNode / TagNode.
    /// </summary>
    public class PropertyTreeListItem
    {
        /// <summary>Asset name (e.g., "Pistol"). Same as Tree.name.</summary>
        public string DisplayName;

        /// <summary>
        /// Unique key for foldout state and search matching.
        /// Uses the asset path to guarantee uniqueness.
        /// </summary>
        public string FullPath;

        /// <summary>Depth in the inheritance chain. Root (no InheritsFrom) = 0.</summary>
        public int Depth;

        /// <summary>The ScriptableObject asset this node represents.</summary>
        public PropertyTreeSO Tree;

        /// <summary>Parent node in the inheritance tree. null for roots.</summary>
        public PropertyTreeListItem Parent;

        /// <summary>Child trees that inherit from this one.</summary>
        public List<PropertyTreeListItem> Children = new();

        /// <summary>
        /// Number of PropertyNodes defined in THIS tree's own treeJson
        /// (not counting inherited ancestors).
        /// </summary>
        public int LocalNodeCount;

        /// <summary>
        /// Pre-computed inheritance chain label for display.
        /// E.g., "Pistol" → "← Firearm ← RangedWeapon ← WeaponBase".
        /// Empty string for root trees.
        /// </summary>
        public string InheritsChainLabel;

        /// <summary>Convenience: true when this tree has child inheritors.</summary>
        public bool HasChildren => Children.Count > 0;
    }
}
#endif
