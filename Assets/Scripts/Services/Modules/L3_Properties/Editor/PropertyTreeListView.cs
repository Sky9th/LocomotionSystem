#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Properties.Editor
{
    /// <summary>
    /// PropertyTree list renderer. Each node is a DrawCard, children nested
    /// inside the parent card. Mirrors AbilityTreeView.
    /// </summary>
    public static class PropertyTreeListView
    {
        private const float FoldoutWidth = 14f;
        private const float FoldoutGap = 6f;

        private static GUIStyle _dashLabel;
        private static GUIStyle DashLabel => _dashLabel ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleCenter };

        /// <summary>
        /// Draw the tree list.
        /// </summary>
        public static void DrawTree(
            List<PropertyTreeListItem> roots,
            Dictionary<string, bool> foldouts,
            ref PropertyTreeSO selectedTree,
            string searchFilter = null,
            Action<PropertyTreeSO> onSelect = null,
            Color selectedColor = default,
            Action<PropertyTreeSO> onDelete = null,
            Action<PropertyTreeSO> onCreateChild = null)
        {
            if (roots == null || roots.Count == 0)
                return;

            if (selectedColor == default)
                selectedColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);

            bool hasSearch = !string.IsNullOrEmpty(searchFilter);
            var q = hasSearch ? searchFilter.ToLowerInvariant() : null;

            // Filter visible roots by search
            var visibleRoots = new List<PropertyTreeListItem>();
            foreach (var root in roots)
            {
                if (hasSearch && !NodeOrDescendantsMatch(root, q))
                    continue;
                visibleRoots.Add(root);
            }

            // Auto-expand ancestors of matching nodes during search
            if (hasSearch)
                foreach (var root in visibleRoots)
                    AutoExpandMatching(root, q, foldouts);

            // Render roots as sibling cards
            for (var i = 0; i < visibleRoots.Count; i++)
            {
                if (i > 0) EditorCard.Gap(EditorTokens.Pad);
                DrawNodeCard(visibleRoots[i], foldouts, ref selectedTree,
                    q, hasSearch, onSelect, selectedColor, onDelete, onCreateChild);
            }
        }

        // ── per-node rendering ──

        private static void DrawNodeCard(
            PropertyTreeListItem node,
            Dictionary<string, bool> foldouts,
            ref PropertyTreeSO selectedTree,
            string q,
            bool hasSearch,
            Action<PropertyTreeSO> onSelect,
            Color selectedColor,
            Action<PropertyTreeSO> onDelete = null,
            Action<PropertyTreeSO> onCreateChild = null)
        {
            // Capture ref to local (avoids lambda limitation)
            var sel = selectedTree;
            bool isSelected = sel != null && sel == node.Tree;
            var rowH = EditorGUIUtility.singleLineHeight;

            // Ensure foldout state exists
            if (!foldouts.ContainsKey(node.FullPath))
                foldouts[node.FullPath] = false;

            // Selected highlight
            var oldBg = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = selectedColor;

            EditorCard.Draw(() =>
            {
                EditorGUILayout.BeginHorizontal();

                // ── Foldout area: fixed 20px (14 + 6) ──
                EditorGUILayout.BeginHorizontal(
                    GUILayout.Width(FoldoutWidth + FoldoutGap));

                // Reset bg so foldout renders normally regardless of selection
                var foldBg = GUI.backgroundColor;
                GUI.backgroundColor = Color.white;

                if (node.HasChildren)
                {
                    var foldRect = GUILayoutUtility.GetRect(FoldoutWidth, rowH);
                    foldouts[node.FullPath] = EditorGUI.Foldout(
                        foldRect, foldouts[node.FullPath], "", true);
                }
                else
                {
                    var dashRect = GUILayoutUtility.GetRect(FoldoutWidth, rowH);
                    GUI.Label(dashRect, "-", DashLabel);
                }

                GUI.backgroundColor = foldBg;
                GUILayout.Space(FoldoutGap);
                EditorGUILayout.EndHorizontal();

                // ── Name area ──
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginHorizontal();

                var label = node.LocalNodeCount > 0
                    ? $"{node.DisplayName}  +{node.LocalNodeCount}"
                    : node.DisplayName;

                var isMatch = hasSearch
                    && node.FullPath.ToLowerInvariant().Contains(q);
                var nameStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = (isSelected || isMatch)
                        ? FontStyle.Bold : FontStyle.Normal
                };

                if (GUILayout.Button(label, nameStyle, GUILayout.ExpandWidth(true)))
                {
                    sel = node.Tree;
                    onSelect?.Invoke(node.Tree);
                    GUI.FocusControl(null);
                }

                // Inheritance chain label (gray, right side)
                if (!string.IsNullOrEmpty(node.InheritsChainLabel))
                {
                    var oldColor = GUI.color;
                    GUI.color = Color.gray;
                    GUILayout.Label(node.InheritsChainLabel, EditorStyles.miniLabel);
                    GUI.color = oldColor;
                }

                // Create child button — quickly create a tree inheriting from this one
                if (onCreateChild != null)
                {
                    var oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                    if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                        onCreateChild(node.Tree);
                    GUI.backgroundColor = oldBg;
                }

                // Delete button (only for leaf trees with no inheritors)
                if (!node.HasChildren && onDelete != null)
                {
                    var oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                        onDelete(node.Tree);
                    GUI.backgroundColor = oldBg;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

                // ── Children: nested INSIDE the parent card ──
                if (node.HasChildren && foldouts[node.FullPath])
                {
                    var visibleChildren = new List<PropertyTreeListItem>();
                    foreach (var child in node.Children)
                    {
                        if (hasSearch && !NodeOrDescendantsMatch(child, q))
                            continue;
                        visibleChildren.Add(child);
                    }

                    if (visibleChildren.Count > 0)
                    {
                        GUILayout.Space(EditorTokens.Pad);
                        for (var i = 0; i < visibleChildren.Count; i++)
                        {
                            if (i > 0) EditorCard.Gap(EditorTokens.Pad);
                            DrawNodeCard(visibleChildren[i], foldouts, ref sel,
                                q, hasSearch, onSelect, selectedColor, onDelete, onCreateChild);
                        }
                    }
                }
            });

            GUI.backgroundColor = oldBg;

            // Write back captured ref
            selectedTree = sel;
        }

        // ── search helpers ──

        private static bool NodeOrDescendantsMatch(PropertyTreeListItem node, string q)
        {
            if (string.IsNullOrEmpty(q)) return true;

            if (node.DisplayName.ToLowerInvariant().Contains(q)
                || node.FullPath.ToLowerInvariant().Contains(q)
                || (node.InheritsChainLabel != null
                    && node.InheritsChainLabel.ToLowerInvariant().Contains(q)))
                return true;

            if (node.HasChildren)
                foreach (var child in node.Children)
                    if (NodeOrDescendantsMatch(child, q))
                        return true;

            return false;
        }

        private static bool HasMatchingDescendant(PropertyTreeListItem node, string q)
        {
            foreach (var child in node.Children)
            {
                if (child.DisplayName.ToLowerInvariant().Contains(q)
                    || child.FullPath.ToLowerInvariant().Contains(q)
                    || (child.InheritsChainLabel != null
                        && child.InheritsChainLabel.ToLowerInvariant().Contains(q)))
                    return true;
                if (child.HasChildren && HasMatchingDescendant(child, q))
                    return true;
            }
            return false;
        }

        private static void AutoExpandMatching(
            PropertyTreeListItem node, string q,
            Dictionary<string, bool> foldouts)
        {
            if (node.HasChildren && HasMatchingDescendant(node, q))
                foldouts[node.FullPath] = true;
            foreach (var child in node.Children)
                AutoExpandMatching(child, q, foldouts);
        }
    }
}
#endif
