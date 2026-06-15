#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// Ability 树渲染器。每个节点一张卡片，间隙 Pad。
    /// </summary>
    public static class AbilityTreeView
    {
        private const float FoldoutWidth = 14f;
        private const float FoldoutGap = 6f;

        public static void DrawTree(
            List<AbilityTreeNode> roots,
            Dictionary<string, bool> foldouts,
            ScriptableObject selectedAsset,
            string searchFilter,
            AbilityTypeFilter typeFilter,
            Action<ScriptableObject> onLeafSelected = null,
            Action<ScriptableObject> onDeleteLeaf = null)
        {
            if (roots == null || roots.Count == 0)
            {
                GUILayout.Space(EditorTokens.Pad);
                EditorGUILayout.LabelField("No abilities found.", EditorTokens.EmptyStateStyle);
                GUILayout.Space(EditorTokens.Pad);
                return;
            }

            bool hasSearch = !string.IsNullOrEmpty(searchFilter);
            var q = hasSearch ? searchFilter.ToLowerInvariant() : null;

            var visibleRoots = new List<AbilityTreeNode>();
            foreach (var root in roots)
            {
                if (hasSearch && !NodeOrDescendantsMatch(root, q, typeFilter))
                    continue;
                visibleRoots.Add(root);
            }

            if (hasSearch)
                foreach (var root in visibleRoots)
                    AutoExpandMatching(root, q, typeFilter, foldouts);

            for (var i = 0; i < visibleRoots.Count; i++)
            {
                if (i > 0) EditorCard.Gap(EditorTokens.Pad);
                DrawNodeCard(visibleRoots[i], foldouts, selectedAsset,
                    q, typeFilter, hasSearch, onLeafSelected, onDeleteLeaf);
            }
        }

        private static void DrawNodeCard(
            AbilityTreeNode node,
            Dictionary<string, bool> foldouts,
            ScriptableObject selectedAsset,
            string q,
            AbilityTypeFilter typeFilter,
            bool hasSearch,
            Action<ScriptableObject> onLeafSelected,
            Action<ScriptableObject> onDeleteLeaf)
        {
            bool hasChildren = node.IsFolder && node.Children.Count > 0;
            var rowH = EditorGUIUtility.singleLineHeight;

            if (!foldouts.ContainsKey(node.FullPath))
                foldouts[node.FullPath] = false;

            if (hasSearch)
            {
                if (!node.FullPath.ToLowerInvariant().Contains(q)
                    && !HasMatchingDescendant(node, q, typeFilter))
                    return;
            }

            var isSelected = !node.IsFolder && node.Asset != null && selectedAsset == node.Asset;

            EditorCard.Draw(() =>
            {
                var label = node.IsFolder
                    ? $"{node.DisplayName} ({node.LeafCount})"
                    : node.DisplayName;
                var isMatch = hasSearch && node.FullPath.ToLowerInvariant().Contains(q);

                var btnW = (!node.IsFolder && onDeleteLeaf != null) ? 20f : 0f;

                var rowRect = GUILayoutUtility.GetRect(
                    GUIContent.none, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(rowH + 2));

                var clickRect = rowRect;
                if (btnW > 0f) clickRect.width -= btnW + 2;
                if (GUI.Button(clickRect, GUIContent.none, GUIStyle.none))
                {
                    if (Event.current.button == 1 && !node.IsFolder && onDeleteLeaf != null)
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Delete"), false, () => onDeleteLeaf(node.Asset));
                        menu.ShowAsContext();
                    }
                    else if (node.IsFolder)
                        foldouts[node.FullPath] = !foldouts[node.FullPath];
                    else
                    {
                        onLeafSelected?.Invoke(node.Asset);
                        GUI.FocusControl(null);
                    }
                }

                // 三角/横线
                var iconRect = new Rect(rowRect.x + 2, rowRect.y, FoldoutWidth, rowRect.height);
                if (hasChildren)
                {
                    foldouts[node.FullPath] = EditorGUI.Foldout(
                        iconRect, foldouts[node.FullPath], "", true);
                }
                else
                {
                    var dashStyle = new GUIStyle()
                        { alignment = TextAnchor.MiddleCenter, fontSize = EditorTokens.FontBase };
                    GUI.Label(iconRect, "-", dashStyle);
                }

                // 文字
                var textRect = new Rect(
                    iconRect.xMax + FoldoutGap, rowRect.y,
                    rowRect.width - FoldoutWidth - FoldoutGap - 2 - btnW - 2, rowRect.height);
                var textStyle = new GUIStyle()
                    { fontStyle = (isSelected || isMatch) ? FontStyle.Bold : FontStyle.Normal,
                      alignment = TextAnchor.MiddleLeft, fontSize = EditorTokens.FontBase };
                GUI.Label(textRect, label, textStyle);

                if (btnW > 0f)
                {
                    var delRect = new Rect(textRect.xMax + 2, rowRect.y, btnW, rowRect.height);
                    if (EditorButton.Draw(delRect, "✕", EditorButtonType.Danger))
                        onDeleteLeaf(node.Asset);
                }

                // 子节点
                if (hasChildren && foldouts[node.FullPath])
                {
                    var visibleChildren = new List<AbilityTreeNode>();
                    foreach (var child in node.Children)
                    {
                        if (hasSearch && !NodeOrDescendantsMatch(child, q, typeFilter))
                            continue;
                        visibleChildren.Add(child);
                    }

                    if (visibleChildren.Count > 0)
                    {
                        GUILayout.Space(EditorTokens.Pad);
                        for (var i = 0; i < visibleChildren.Count; i++)
                        {
                            if (i > 0) EditorCard.Gap(EditorTokens.Pad);
                            DrawNodeCard(visibleChildren[i], foldouts, selectedAsset,
                                q, typeFilter, hasSearch, onLeafSelected, onDeleteLeaf);
                        }
                    }
                }
            });
        }

        private static bool NodeOrDescendantsMatch(AbilityTreeNode node, string q,
            AbilityTypeFilter filter)
        {
            if (!FilterAllows(node, filter)) return false;
            if (node.DisplayName.ToLowerInvariant().Contains(q)
                || node.FullPath.ToLowerInvariant().Contains(q)) return true;
            if (node.IsFolder)
                foreach (var c in node.Children)
                    if (NodeOrDescendantsMatch(c, q, filter)) return true;
            return false;
        }

        private static bool HasMatchingDescendant(AbilityTreeNode node, string q,
            AbilityTypeFilter filter)
        {
            foreach (var c in node.Children)
            {
                if (!FilterAllows(c, filter)) continue;
                if (c.FullPath.ToLowerInvariant().Contains(q)) return true;
                if (c.IsFolder && HasMatchingDescendant(c, q, filter)) return true;
            }
            return false;
        }

        private static void AutoExpandMatching(AbilityTreeNode node, string q,
            AbilityTypeFilter filter, Dictionary<string, bool> foldouts)
        {
            if (node.IsFolder && HasMatchingDescendant(node, q, filter))
                foldouts[node.FullPath] = true;
            foreach (var c in node.Children)
                AutoExpandMatching(c, q, filter, foldouts);
        }

        private static bool FilterAllows(AbilityTreeNode node, AbilityTypeFilter filter)
        {
            if (filter == AbilityTypeFilter.All) return true;
            if (node.IsFolder)
            {
                foreach (var c in node.Children)
                    if (FilterAllows(c, filter)) return true;
                return false;
            }
            if (filter == AbilityTypeFilter.Active) return node.Asset is AbilityDefSO;
            if (filter == AbilityTypeFilter.Passive) return node.Asset is PassiveAbilitySO;
            return true;
        }
    }
}
#endif
