#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 基于 Unity TreeView 的编辑器树组件。数据驱动，树机制全内置。
    /// </summary>
    public class EditorTreeView : TreeView
    {
        private List<EditorTreeNode> _roots;
        private Action<EditorTreeNode> _onSelect;
        private Action<EditorTreeNode> _onDelete;

        public EditorTreeView() : base(new TreeViewState())
        {
            showBorder = false;
            showAlternatingRowBackgrounds = false;
        }

        public void SetData(List<EditorTreeNode> roots, Action<EditorTreeNode> onSelect = null,
            Action<EditorTreeNode> onDelete = null)
        {
            _roots = roots ?? new List<EditorTreeNode>();
            _onSelect = onSelect;
            _onDelete = onDelete;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1, "root");
            var nextId = 1;

            if (_roots != null && _roots.Count > 0)
            {
                foreach (var node in _roots)
                    root.AddChild(BuildNode(node, ref nextId));
            }

            root.children ??= new List<TreeViewItem>();

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        private TreeViewItem BuildNode(EditorTreeNode node, ref int nextId)
        {
            var id = nextId++;
            var item = new TreeViewItem(id, node.Depth, node.DisplayName);
            if (node.IsFolder)
            {
                foreach (var child in node.Children)
                    item.AddChild(BuildNode(child, ref nextId));
            }
            return item;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            if (_onSelect == null || _roots == null) return;

            foreach (var id in selectedIds)
            {
                var traverseId = 1;
                var node = FindNodeById(_roots, id, ref traverseId);
                if (node != null)
                {
                    _onSelect(node);
                    break;
                }
            }
        }

        protected override void ContextClickedItem(int id)
        {
            if (_onDelete == null || _roots == null) return;

            var traverseId = 1;
            var node = FindNodeById(_roots, id, ref traverseId);
            if (node != null)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Delete"), false, () => _onDelete(node));
                menu.ShowAsContext();
            }
        }

        private static EditorTreeNode FindNodeById(List<EditorTreeNode> nodes, int targetId, ref int currentId)
        {
            foreach (var node in nodes)
            {
                if (currentId == targetId) return node;
                currentId++;
                if (node.IsFolder)
                {
                    var found = FindNodeById(node.Children, targetId, ref currentId);
                    if (found != null) return found;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 编辑器树工具——Demo 数据、排序、计数。
    /// </summary>
    public static class EditorTree
    {
        /// <summary>
        /// 递归排序树节点：文件夹优先，同类型按 DisplayName 字母序。
        /// </summary>
        public static void SortTreeRecursive(List<EditorTreeNode> roots)
        {
            roots.Sort((a, b) =>
            {
                if (a.IsFolder != b.IsFolder) return a.IsFolder ? -1 : 1;
                return string.CompareOrdinal(a.DisplayName, b.DisplayName);
            });
            foreach (var r in roots)
                SortTreeRecursive(r.Children);
        }

        /// <summary>
        /// 递归计算每个节点的 LeafCount（子树中叶子总数）。
        /// </summary>
        public static void ComputeTreeCounts(List<EditorTreeNode> roots)
        {
            foreach (var r in roots)
                r.LeafCount = r.IsFolder ? CountLeaves(r.Children) : 1;
        }

        private static int CountLeaves(List<EditorTreeNode> nodes)
        {
            var count = 0;
            foreach (var n in nodes)
                count += n.IsFolder ? CountLeaves(n.Children) : 1;
            return count;
        }

        #region Demo Data

        /// <summary>
        /// 创建 Demo 树形数据，用于开发期预览。
        /// </summary>
        public static List<EditorTreeNode> CreateDemoData()
        {
            // 叶子
            var meleeAttack = NewLeaf("MeleeAttack", "MeleeAttack");
            var rangedAttack = NewLeaf("RangedAttack", "RangedAttack");
            var shieldBlock = NewLeaf("ShieldBlock", "ShieldBlock");
            var dodgeRoll = NewLeaf("DodgeRoll", "DodgeRoll");
            var passiveAura = NewLeaf("PassiveAura", "PassiveAura");
            var damage = NewLeaf("Damage", "Damage");
            var heal = NewLeaf("Heal", "Heal");
            var speedUp = NewLeaf("SpeedUp", "SpeedUp");
            var shieldBuff = NewLeaf("ShieldBuff", "ShieldBuff");
            var noises = NewLeaf("Noises", "Noises");

            // 文件夹
            var attack = NewFolder("Attack", new[] { meleeAttack, rangedAttack });
            var defense = NewFolder("Defense", new[] { shieldBlock, dodgeRoll });
            var buff = NewFolder("Buff", new[] { speedUp, shieldBuff });

            var abilities = NewFolder("Abilities", new[] { attack, defense, passiveAura });
            var effects = NewFolder("Effects", new[] { damage, heal, buff });

            var roots = new List<EditorTreeNode> { abilities, effects, noises };

            // 统一修正 FullPath / Depth / Parent
            foreach (var root in roots)
                FixNodePaths(root, null);

            ComputeTreeCounts(roots);
            SortTreeRecursive(roots);
            return roots;
        }

        private static EditorTreeNode NewFolder(string name, EditorTreeNode[] children)
        {
            return new EditorTreeNode
            {
                DisplayName = name,
                IsFolder = true,
                Children = new List<EditorTreeNode>(children),
            };
        }

        private static EditorTreeNode NewLeaf(string name, object userData)
        {
            return new EditorTreeNode
            {
                DisplayName = name,
                IsFolder = false,
                UserData = userData,
            };
        }

        private static void FixNodePaths(EditorTreeNode node, EditorTreeNode parent)
        {
            node.Parent = parent;
            node.Depth = parent?.Depth + 1 ?? 0;
            node.FullPath = parent != null ? $"{parent.FullPath}/{node.DisplayName}" : node.DisplayName;
            if (node.IsFolder)
                foreach (var child in node.Children)
                    FixNodePaths(child, node);
        }

        #endregion
    }
}
#endif
