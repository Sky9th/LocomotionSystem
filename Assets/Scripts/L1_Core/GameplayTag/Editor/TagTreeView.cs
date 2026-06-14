#if UNITY_EDITOR
using System.Collections.Generic;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core.Editor
{
    /// <summary>
    /// 标签树共享渲染器。严格参照 StatsTreeEditorWindow 的嵌套布局。
    /// </summary>
    public static class TagTreeView
    {
        private const float FoldoutWidth = 14f;
        private const float FoldoutGap = 6f;
        private const float DepthWidth = 18f;

        public static void DrawTree(
            List<TagNode> roots,
            Dictionary<string, bool> foldouts,
            ref string selectedFullTag,
            string searchFilter = null,
            string rootFilter = null,
            System.Action<TagNode> onCreateChild = null)
        {
            if (roots == null || roots.Count == 0)
            {
                GUILayout.Space(EditorTokens.Pad);
                var greyLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                greyLabel.normal.textColor = Color.grey;
                EditorGUILayout.LabelField("No tags loaded.", greyLabel);
                GUILayout.Space(EditorTokens.Pad);
                return;
            }

            bool hasSearch = !string.IsNullOrEmpty(searchFilter);
            var q = hasSearch ? searchFilter.ToLowerInvariant() : null;

            // 先筛选可见的根节点
            var visibleRoots = new List<TagNode>();
            foreach (var root in roots)
            {
                if (!string.IsNullOrEmpty(rootFilter) && root.LeafName != rootFilter.TrimEnd('.'))
                    continue;
                if (hasSearch && !NodeOrDescendantsMatch(root, q))
                    continue;
                visibleRoots.Add(root);
            }

            // 搜索时自动展开匹配路径
            if (hasSearch)
                foreach (var root in visibleRoots)
                    AutoExpandMatching(root, q, foldouts);

            // 渲染，间距只在可见节点间
            for (var i = 0; i < visibleRoots.Count; i++)
            {
                if (i > 0) GUILayout.Space(EditorTokens.Pad);
                DrawNodeCard(visibleRoots[i], foldouts, ref selectedFullTag, searchFilter, q, onCreateChild);
            }
        }

        private static bool NodeOrDescendantsMatch(TagNode node, string q)
        {
            if (node.FullTag.ToLowerInvariant().Contains(q))
                return true;
            foreach (var child in node.Children)
                if (NodeOrDescendantsMatch(child, q))
                    return true;
            return false;
        }

        private static void AutoExpandMatching(TagNode node, string q, Dictionary<string, bool> foldouts)
        {
            if (HasMatchingDescendant(node, q))
                foldouts[node.FullTag] = true;
            foreach (var child in node.Children)
                AutoExpandMatching(child, q, foldouts);
        }

        private static bool HasMatchingDescendant(TagNode node, string q)
        {
            foreach (var child in node.Children)
            {
                if (child.FullTag.ToLowerInvariant().Contains(q))
                    return true;
                if (HasMatchingDescendant(child, q))
                    return true;
            }
            return false;
        }

        private static void DrawNodeCard(
            TagNode node,
            Dictionary<string, bool> foldouts,
            ref string selectedFullTag,
            string searchFilter,
            string searchQuery = null,
            System.Action<TagNode> onCreateChild = null)
        {
            bool hasChildren = node.Children.Count > 0;
            bool isSelected = selectedFullTag == node.FullTag;
            bool hasSearch = !string.IsNullOrEmpty(searchQuery);

            if (!foldouts.ContainsKey(node.FullTag))
                foldouts[node.FullTag] = false;

            // 搜索时跳过不匹配且无匹配子孙的节点
            if (hasSearch && !node.FullTag.ToLowerInvariant().Contains(searchQuery)
                && !HasMatchingDescendant(node, searchQuery))
                return;

            var rowH = EditorGUIUtility.singleLineHeight;

            // ── 行：折叠区始终占位（叶子用短横线 -）──
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.Width(FoldoutWidth + FoldoutGap));
            if (hasChildren)
            {
                var foldRect = GUILayoutUtility.GetRect(FoldoutWidth, rowH);
                foldouts[node.FullTag] = EditorGUI.Foldout(foldRect, foldouts[node.FullTag], "", true);
            }
            else
            {
                var dashRect = GUILayoutUtility.GetRect(FoldoutWidth, rowH);
                var dashStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(dashRect, "–", dashStyle);
            }
            GUILayout.Space(FoldoutGap);
            EditorGUILayout.EndHorizontal();

            // 右块：深度 + 名称 + 子节点
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // ── 名称行（深度在右块内）──
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"[{node.Depth}]", EditorStyles.label, GUILayout.Width(DepthWidth));

            var label = node.LeafName;
            var isMatch = hasSearch && node.FullTag.ToLowerInvariant().Contains(searchQuery);

            var style = new GUIStyle(EditorStyles.label)
            {
                fontStyle = (isSelected || isMatch) ? FontStyle.Bold : FontStyle.Normal,
            };

            if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true)))
                selectedFullTag = node.FullTag;

            if (node.Exists && onCreateChild != null && !hasSearch)
            {
                if (GUILayout.Button("＋", EditorStyles.miniButton, GUILayout.Width(20)))
                    onCreateChild(node);
            }

            EditorGUILayout.EndHorizontal();

            // ── 子节点 ──
            bool expanded = foldouts.TryGetValue(node.FullTag, out var exp) && exp;
            if (hasChildren && expanded)
            {
                // 搜索时检查是否有匹配的子节点，全部不匹配则隐藏卡片避免空白
                bool anyVisible = !hasSearch;
                if (hasSearch)
                {
                    foreach (var child in node.Children)
                        if (NodeOrDescendantsMatch(child, searchQuery)) { anyVisible = true; break; }
                }

                if (anyVisible)
                {
                    // 先筛选出需渲染的子节点
                    var visibleChildren = new List<TagNode>();
                    foreach (var child in node.Children)
                        if (!hasSearch || NodeOrDescendantsMatch(child, searchQuery))
                            visibleChildren.Add(child);

                    if (visibleChildren.Count > 0)
                    {
                        GUILayout.Space(EditorTokens.Pad);
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Space(EditorTokens.Pad);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(EditorTokens.Pad);
                        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                        for (var i = 0; i < visibleChildren.Count; i++)
                        {
                            if (i > 0) GUILayout.Space(EditorTokens.Pad);
                            DrawNodeCard(visibleChildren[i], foldouts, ref selectedFullTag, searchFilter, searchQuery, onCreateChild);
                        }

                        EditorGUILayout.EndVertical();
                        GUILayout.Space(EditorTokens.Pad);
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(EditorTokens.Pad);
                        EditorGUILayout.EndVertical();

                        GUILayout.Space(EditorTokens.Pad);
                    }
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // ── 右键菜单 ──
            if (Event.current.type == EventType.ContextClick)
            {
                var rowRect = GUILayoutUtility.GetLastRect();
                if (rowRect.Contains(Event.current.mousePosition))
                {
                    selectedFullTag = node.FullTag;
                    ShowContextMenu(node);
                    Event.current.Use();
                }
            }
        }

        private static void ShowContextMenu(TagNode node)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Create Child Tag..."), false, () =>
                Debug.Log($"[TagEditor] Create child under: {node.FullTag} (TODO Phase 2)"));

            menu.AddSeparator("");

            if (node.Exists)
            {
                menu.AddItem(new GUIContent("Select Asset"), false, () => { });
                menu.AddItem(new GUIContent("Ping Asset"), false, () => { });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Select Asset"));
                menu.AddDisabledItem(new GUIContent("Ping Asset"));
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Delete Tag"), false, () =>
                Debug.Log($"[TagEditor] Delete: {node.FullTag} (TODO Phase 2)"));

            menu.ShowAsContext();
        }
    }
}
#endif
