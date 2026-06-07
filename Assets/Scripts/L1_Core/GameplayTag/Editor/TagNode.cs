#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Core.Editor
{
    /// <summary>
    /// 标签树节点。TagTreeModel 构建，TagTreeView 渲染。
    /// </summary>
    public class TagNode
    {
        public string LeafName;
        public string FullTag;
        public int Depth;
        public GameplayTagDefinitionSO Asset;
        public TagNode Parent;
        public List<TagNode> Children = new();
        public bool Exists => Asset != null;
    }
}
#endif
