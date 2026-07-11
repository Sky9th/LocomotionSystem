#if UNITY_EDITOR
using System.Collections.Generic;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 编辑器树节点——文件夹或叶子。
    /// 纯数据契约：消费者按约定构造节点数据，Tree 组件负责渲染。
    /// 不持有特定资产类型，通过 <see cref="UserData"/> 携带自定义载荷。
    /// </summary>
    public class EditorTreeNode
    {
        public string DisplayName;
        public string FullPath;
        public int Depth;
        public bool IsFolder;

        /// <summary>消费者自定义数据载荷（如 ScriptableObject 引用等）。</summary>
        public object UserData;

        public EditorTreeNode Parent;
        public List<EditorTreeNode> Children = new();

        /// <summary>子树中叶子节点总数。</summary>
        public int LeafCount;
    }
}
#endif
