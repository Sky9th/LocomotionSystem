#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core.Editor
{
    public class TagNode
    {
        public string LeafName;
        public string FullTag;
        public int Depth;
        public bool Exists;
        public TagNode Parent;
        public List<TagNode> Children = new();
    }

    /// <summary>
    /// 标签树共享渲染器。严格参照 StatsTreeEditorWindow 的嵌套布局。
    /// </summary>
    public static class TagTreeView
    {
        private const float Pad = 6f;
        private const float FoldoutWidth = 14f;
        private const float FoldoutGap = 6f;
        private const float DepthWidth = 24f;

        public static void DrawTree(
            List<TagNode> roots,
            Dictionary<string, bool> foldouts,
            ref string selectedFullTag,
            string searchFilter = null,
            string rootFilter = null)
        {
            if (roots == null || roots.Count == 0)
            {
                GUILayout.Space(Pad);
                var greyLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                greyLabel.normal.textColor = Color.grey;
                EditorGUILayout.LabelField("No tags loaded.", greyLabel);
                GUILayout.Space(Pad);
                return;
            }

            foreach (var root in roots)
            {
                if (!string.IsNullOrEmpty(rootFilter))
                {
                    if (root.LeafName != rootFilter.TrimEnd('.'))
                        continue;
                }
                DrawNodeCard(root, foldouts, ref selectedFullTag, searchFilter);
            }
        }

        private static void DrawNodeCard(
            TagNode node,
            Dictionary<string, bool> foldouts,
            ref string selectedFullTag,
            string searchFilter)
        {
            bool hasChildren = node.Children.Count > 0;
            bool isSelected = selectedFullTag == node.FullTag;
            var rowH = EditorGUIUtility.singleLineHeight;

            if (!foldouts.ContainsKey(node.FullTag))
                foldouts[node.FullTag] = false;

            // ── 行：foldout + 右块（参照 StatsTree DrawFolderCard）──
            EditorGUILayout.BeginHorizontal();

            // 左：仅折叠箭头 — 固定 18px
            EditorGUILayout.BeginHorizontal(GUILayout.Width(FoldoutWidth + FoldoutGap));
            if (hasChildren)
            {
                var foldRect = GUILayoutUtility.GetRect(FoldoutWidth, rowH);
                foldouts[node.FullTag] = EditorGUI.Foldout(foldRect, foldouts[node.FullTag], "", true);
            }
            else
            {
                GUILayout.Space(FoldoutWidth);
            }
            GUILayout.Space(FoldoutGap);
            EditorGUILayout.EndHorizontal();

            // 右块：深度 + 名称 + 子节点
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // ── 名称行（深度在右块内，与 StatsTree 一致）──
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"[{node.Depth}]", EditorStyles.label, GUILayout.Width(DepthWidth));

            var label = node.LeafName;

            var style = node.Exists
                ? new GUIStyle(EditorStyles.label) { fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal }
                : new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.grey } };

            if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true)))
                selectedFullTag = node.FullTag;

            EditorGUILayout.EndHorizontal();

            // ── 子节点 ──
            bool expanded = foldouts.TryGetValue(node.FullTag, out var exp) && exp;
            if (hasChildren && expanded)
            {
                GUILayout.Space(Pad);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Space(Pad);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(Pad);
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (i > 0) GUILayout.Space(Pad);
                    DrawNodeCard(node.Children[i], foldouts, ref selectedFullTag, searchFilter);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(Pad);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(Pad);
                EditorGUILayout.EndVertical();

                GUILayout.Space(Pad);
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
