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
        private const float Pad = 6f;
        private const float FoldoutWidth = 14f;
        private const float FoldoutGap = 6f;

        public static void DrawTree(
            List<AbilityTreeNode> roots,
            Dictionary<string, bool> foldouts,
            ref AbilitySO selectedAbility,
            string searchFilter,
            AbilityTypeFilter typeFilter,
            Action<ScriptableObject> onLeafSelected = null,
            EffectSO selectedEffect = null,
            AbilitySearchSO selectedSearch = null)
        {
            if (roots == null || roots.Count == 0)
            {
                GUILayout.Space(Pad);
                var grey = new GUIStyle(EditorStyles.label)
                    { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.grey } };
                EditorGUILayout.LabelField("No abilities found.", grey);
                GUILayout.Space(Pad);
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
                if (i > 0) EditorUIUtility.CardGap(Pad);
                DrawNodeCard(visibleRoots[i], foldouts, ref selectedAbility,
                    q, typeFilter, hasSearch, onLeafSelected, selectedEffect, selectedSearch);
            }
        }

        private static void DrawNodeCard(
            AbilityTreeNode node,
            Dictionary<string, bool> foldouts,
            ref AbilitySO selectedAbility,
            string q,
            AbilityTypeFilter typeFilter,
            bool hasSearch,
            Action<ScriptableObject> onLeafSelected = null,
            EffectSO selectedEffect = null,
            AbilitySearchSO selectedSearch = null)
        {
            bool hasChildren = node.IsFolder && node.Children.Count > 0;
            var sel = selectedAbility;
            bool isSelected = !node.IsFolder
                && ((node.Ability != null && sel == node.Ability)
                    || (node.Effect != null && selectedEffect == node.Effect)
                    || (node.Search != null && selectedSearch == node.Search));
            var rowH = EditorGUIUtility.singleLineHeight;

            if (!foldouts.ContainsKey(node.FullPath))
                foldouts[node.FullPath] = false;

            if (hasSearch)
            {
                if (!node.FullPath.ToLowerInvariant().Contains(q)
                    && !HasMatchingDescendant(node, q, typeFilter))
                    return;
            }

            EditorUIUtility.DrawCard(Pad, () =>
            {
                var label = node.IsFolder
                    ? $"{node.DisplayName} ({node.AbilityCount})"
                    : node.DisplayName;
                var isMatch = hasSearch && node.FullPath.ToLowerInvariant().Contains(q);

                // 整行一个 Rect，手动绘三角 + 文字
                var rowRect = GUILayoutUtility.GetRect(
                    GUIContent.none, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(rowH + 2));

                // 点击整行
                if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
                {
                    if (node.IsFolder)
                        foldouts[node.FullPath] = !foldouts[node.FullPath];
                    else
                    {
                        if (onLeafSelected != null)
                            onLeafSelected(node.Ability ?? (ScriptableObject)node.Effect ?? node.Search);
                        else
                            sel = node.Ability;
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
                    var dashStyle = new GUIStyle(EditorStyles.label)
                        { alignment = TextAnchor.MiddleCenter };
                    GUI.Label(iconRect, "-", dashStyle);
                }

                // 文字
                var textRect = new Rect(
                    iconRect.xMax + FoldoutGap, rowRect.y,
                    rowRect.width - FoldoutWidth - FoldoutGap - 2, rowRect.height);
                var textStyle = new GUIStyle(EditorStyles.label)
                    { fontStyle = (isSelected || isMatch) ? FontStyle.Bold : FontStyle.Normal,
                      alignment = TextAnchor.MiddleLeft };
                GUI.Label(textRect, label, textStyle);

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
                        GUILayout.Space(Pad);
                        for (var i = 0; i < visibleChildren.Count; i++)
                        {
                            if (i > 0) EditorUIUtility.CardGap(Pad);
                            DrawNodeCard(visibleChildren[i], foldouts,
                                ref sel, q, typeFilter, hasSearch, onLeafSelected, selectedEffect, selectedSearch);
                        }
                    }
                }
            });

            selectedAbility = sel;
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
            if (filter == AbilityTypeFilter.Active) return node.Ability is AbilityDefSO;
            if (filter == AbilityTypeFilter.Passive) return node.Ability is PassiveAbilitySO;
            return true;
        }
    }
}
#endif
