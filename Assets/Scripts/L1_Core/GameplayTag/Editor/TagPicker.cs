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
    /// </summary>
    public class TagPicker : PopupWindowContent
    {
        private const float Pad = 6f;

        // -- 参数 --
        private readonly string _rootFilter;
        private readonly bool _allowCreate;
        private readonly string _currentFullTag;
        private readonly Action<GameplayTagDefinitionSO> _onSelected;

        // -- 数据 --
        private TagTreeModel _model;

        // -- 状态 --
        private string _searchText = "";
        private string _selectedFullTag;
        private readonly Dictionary<string, bool> _foldouts = new();
        private Vector2 _scroll;

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
            _model = new TagTreeModel();
            _model.Refresh();

            if (!string.IsNullOrEmpty(_currentFullTag))
                ExpandAncestors(_currentFullTag);
        }

        public override Vector2 GetWindowSize()
            => new(340, 420);

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(Pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);

            EditorGUILayout.BeginVertical();

            // ── 搜索框 ──
            DrawSearchField();
            GUILayout.Space(Pad);

            // ── 搜索结果 ──
            if (!string.IsNullOrEmpty(_searchText))
            {
                var matches = _model.Search(_searchText, _rootFilter);
                if (matches.Count > 0)
                {
                    EditorGUILayout.LabelField($"Matches: {matches.Count}", EditorStyles.label);
                }
                else if (_allowCreate)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(Pad);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(Pad);
                    EditorGUILayout.LabelField($"Create new tag: {_searchText}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                    if (GUILayout.Button("Create", GUILayout.Width(60), GUILayout.Height(22)))
                    {
                        try
                        {
                            var newTag = TagCreator.CreateTagChain(_searchText);
                            _onSelected?.Invoke(newTag);
                            editorWindow.Close();
                            return;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[TagPicker] Failed: {ex.Message}");
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.Space(Pad);
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(Pad);
                    EditorGUILayout.EndVertical();
                }
                GUILayout.Space(Pad);
            }

            // ── 树（搜索时自动过滤 + 展开匹配路径）──
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();
            TagTreeView.DrawTree(_model.Roots, _foldouts, ref _selectedFullTag,
                searchFilter: _searchText,
                rootFilter: _rootFilter);
            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            GUILayout.Space(Pad);

            // ── 底部按钮 ──
            DrawFooter();

            EditorGUILayout.EndVertical();

            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(Pad);
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

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                editorWindow.Close();
            }

            GUI.enabled = !string.IsNullOrEmpty(_selectedFullTag);
            if (GUILayout.Button("Select", GUILayout.Width(80)))
            {
                var node = _model.Find(_selectedFullTag);
                if (node != null)
                    _onSelected?.Invoke(node.Asset);
                editorWindow.Close();
            }
            GUI.enabled = true;

            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
        }

        private void SelectTag(TagNode node)
        {
            if (node == null) return;
            _onSelected?.Invoke(node.Asset);
            editorWindow.Close();
        }

        // ── 辅助 ──
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
    }
}
#endif
