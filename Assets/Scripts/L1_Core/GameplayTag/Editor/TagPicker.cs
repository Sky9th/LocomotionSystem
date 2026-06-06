#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core.Editor
{
    /// <summary>
    /// 可嵌入的标签选择器 Popup。
    /// 任何 Editor 都可调用 TagPicker.Show() 弹出此窗口选标签。
    /// Phase 1: 壳子 — 搜索框 + 假数据树。
    /// Phase 2: 接入真实 TagTreeModel + 创建回调。
    /// </summary>
    public class TagPicker : PopupWindowContent
    {
        private const float Pad = 6f;

        // -- 参数 --
        private readonly string _rootFilter;
        private readonly bool _allowCreate;
        private readonly string _currentFullTag;
        private readonly Action<GameplayTagDefinitionSO> _onSelected;

        // -- 状态 --
        private string _searchText = "";
        private string _selectedFullTag;
        private readonly Dictionary<string, bool> _foldouts = new();
        private List<TagNode> _roots;
        private Vector2 _scroll;

        [MenuItem("Tools/RedDust/Tag Picker (Test)")]
        private static void TestOpen()
        {
            var rect = new Rect(600, 300, 0, 0);
            Show(rect, rootFilter: null, allowCreate: true, onSelected: tag =>
            {
                if (tag != null)
                    Debug.Log($"[TagPicker] Selected: {tag.FullTag}");
                else
                    Debug.Log("[TagPicker] Cancelled / new tag requested");
            });
        }

        // ── 静态入口 ──
        public static void Show(
            Rect activatorRect,
            string rootFilter = null,
            bool allowCreate = true,
            string currentFullTag = null,
            Action<GameplayTagDefinitionSO> onSelected = null)
        {
            var popup = new TagPicker(rootFilter, allowCreate, currentFullTag, onSelected);
            PopupWindow.Show(activatorRect, popup);
        }

        private TagPicker(string rootFilter, bool allowCreate, string currentFullTag, Action<GameplayTagDefinitionSO> onSelected)
        {
            _rootFilter = rootFilter;
            _allowCreate = allowCreate;
            _currentFullTag = currentFullTag;
            _onSelected = onSelected;
            _selectedFullTag = currentFullTag;
        }

        public override void OnOpen()
        {
            base.OnOpen();
            BuildFakeTree();

            // 展开当前已选标签的祖先
            if (!string.IsNullOrEmpty(_currentFullTag))
            {
                ExpandAncestors(_currentFullTag);
            }
        }

        public override Vector2 GetWindowSize()
            => new(320, 400);

        public override void OnGUI(Rect rect)
        {
            // ── 搜索框 ──
            DrawSearchField();
            GUILayout.Space(Pad);

            // ── 搜索匹配结果 ──
            if (!string.IsNullOrEmpty(_searchText))
            {
                DrawSearchResults();
                GUILayout.Space(Pad);
            }

            // ── 浏览树 ──
            if (string.IsNullOrEmpty(_searchText))
            {
                EditorGUILayout.LabelField("Or browse tree:", EditorStyles.label);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            TagTreeView.DrawTree(_roots, _foldouts, ref _selectedFullTag, rootFilter: _rootFilter);
            EditorGUILayout.EndScrollView();

            GUILayout.Space(Pad);

            // ── 底部按钮 ──
            DrawFooter();
        }

        private void DrawSearchField()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            if (!string.IsNullOrEmpty(_searchText) && GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchResults()
        {
            // Phase 1: 简单子串匹配
            var matches = new List<TagNode>();
            SearchNodes(_roots, _searchText.ToLowerInvariant(), matches);

            if (matches.Count > 0)
            {
                EditorGUILayout.LabelField($"Matches: {matches.Count}", EditorStyles.label);

                foreach (var m in matches.Take(10))
                {
                    EditorGUILayout.BeginHorizontal();

                    var style = m.Exists
                        ? EditorStyles.label
                        : new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.grey } };

                    if (GUILayout.Button(m.FullTag, style))
                    {
                        SelectTag(m);
                        return;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (matches.Count > 10)
                {
                    EditorGUILayout.LabelField($"... and {matches.Count - 10} more", EditorStyles.label);
                }
            }
            else if (_allowCreate)
            {
                // 无匹配 → 建议新建（加粗灰显）
                var createStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.grey } };
                EditorGUILayout.LabelField($"Create: {_searchText}", createStyle);
            }
            else
            {
                var greyLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                greyLabel.normal.textColor = Color.grey;
                EditorGUILayout.LabelField("No matches found.", greyLabel);
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                editorWindow.Close();
            }

            GUI.enabled = !string.IsNullOrEmpty(_selectedFullTag);
            if (GUILayout.Button("Select", GUILayout.Width(80)))
            {
                SelectTag(FindNode(_selectedFullTag));
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void SelectTag(TagNode node)
        {
            if (node == null) return;
            // Phase 1: callback with null (no real SO yet)
            _onSelected?.Invoke(null);
            editorWindow.Close();
        }

        // ── Phase 1 假数据 ──
        private void BuildFakeTree()
        {
            _roots = new List<TagNode>();

            var damage = MkRoot("Damage", true);
            var physical = MkChild("Physical", "Damage.Physical", true, damage);
            MkChild("Slash", "Damage.Physical.Slash", true, physical);
            MkChild("Pierce", "Damage.Physical.Pierce", true, physical);
            MkChild("Blunt", "Damage.Physical.Blunt", false, physical);
            MkChild("Bite", "Damage.Physical.Bite", false, physical);

            var elemental = MkChild("Elemental", "Damage.Elemental", false, damage);
            MkChild("Fire", "Damage.Elemental.Fire", false, elemental);
            MkChild("Cold", "Damage.Elemental.Cold", false, elemental);

            var biological = MkChild("Biological", "Damage.Biological", true, damage);
            MkChild("Bleed", "Damage.Biological.Bleed", true, biological);
            MkChild("Disease", "Damage.Biological.Disease", false, biological);
        }

        private TagNode MkRoot(string leafName, bool exists)
        {
            var node = new TagNode { LeafName = leafName, FullTag = leafName, Depth = 1, Exists = exists };
            _roots.Add(node);
            return node;
        }

        private TagNode MkChild(string leafName, string fullTag, bool exists, TagNode parent)
        {
            var node = new TagNode
            {
                LeafName = leafName,
                FullTag = fullTag,
                Depth = parent.Depth + 1,
                Exists = exists,
                Parent = parent
            };
            parent.Children.Add(node);
            return node;
        }

        private void ExpandAncestors(string fullTag)
        {
            var parts = fullTag.Split('.');
            var accumulated = "";
            for (int i = 0; i < parts.Length; i++)
            {
                accumulated = i == 0 ? parts[i] : $"{accumulated}.{parts[i]}";
                _foldouts[accumulated] = true;
            }
        }

        private TagNode FindNode(string fullTag)
        {
            TagNode Search(List<TagNode> nodes)
            {
                foreach (var n in nodes)
                {
                    if (n.FullTag == fullTag) return n;
                    var found = Search(n.Children);
                    if (found != null) return found;
                }
                return null;
            }
            return Search(_roots);
        }

        private void SearchNodes(List<TagNode> nodes, string query, List<TagNode> results)
        {
            foreach (var n in nodes)
            {
                if (n.FullTag.ToLowerInvariant().Contains(query) || n.LeafName.ToLowerInvariant().Contains(query))
                    results.Add(n);
                SearchNodes(n.Children, query, results);
            }
        }
    }
}
#endif
