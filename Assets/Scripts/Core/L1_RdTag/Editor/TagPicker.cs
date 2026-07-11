#if UNITY_EDITOR
using RedDust.Core.RdTag;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RedDust.Shared.EditorUI;
namespace RedDust.Core.RdTag.Editor
{
    /// <summary>
    /// 可嵌入的标签选择器 Popup。
    /// 任何 Editor 都可调用 TagPicker.Show() 弹出此窗口选标签。
    /// </summary>
    public class TagPicker : PopupWindowContent
    {

        // -- 参数 --
        private readonly string _rootFilter;
        private readonly bool _allowCreate;
        private readonly string _currentFullTag;
        private readonly Action<RdTagDefSO> _onSelected;

        // -- 数据 --
        private TagTreeModel _model;

        // -- 状态 --
        private string _searchText = "";
        private string _selectedFullTag;
        private EditorTreeView _treeView;

        // ── 静态入口 ──
        public static void Show(
            Rect activatorRect,
            string rootFilter = null,
            bool allowCreate = true,
            string currentFullTag = null,
            Action<RdTagDefSO> onSelected = null)
        {
            var popup = new TagPicker(rootFilter, allowCreate, currentFullTag, onSelected);
            PopupWindow.Show(activatorRect, popup);
        }

        private TagPicker(string rootFilter, bool allowCreate, string currentFullTag, Action<RdTagDefSO> onSelected)
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
            _model = TagTreeModel.GetCached();

            var roots = _model.Roots;
            if (!string.IsNullOrEmpty(_rootFilter))
            {
                var filter = _rootFilter.TrimEnd('.');
                // 优先精确匹配 FullPath（支持多段路径如 "Ability.Effect"），
                // 回退到根节点 DisplayName 匹配（单段如 "Ability"）
                var targetNode = _model.Find(filter);
                if (targetNode != null)
                    roots = new System.Collections.Generic.List<EditorTreeNode> { targetNode };
                else
                    roots = roots.Where(r => r.DisplayName == filter).ToList();
            }

            _treeView = new EditorTreeView();
            _treeView.SetData(roots, onSelect: node =>
            {
                var tag = node?.UserData as RdTagDefSO;
                if (tag != null)
                {
                    _onSelected?.Invoke(tag);
                    editorWindow.Close();
                }
            });

            if (!string.IsNullOrEmpty(_currentFullTag))
                _treeView.ExpandAll();
        }

        public override Vector2 GetWindowSize()
            => new(340, 420);

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginVertical();

            // ── 搜索框 ──
            DrawSearchField();
            EditorCard.Gap(EditorTokens.Pad);

            // ── 搜索结果 ──
            if (!string.IsNullOrEmpty(_searchText))
            {
                var matches = _model.Search(_searchText, _rootFilter);
                if (matches.Count > 0)
                {
                    EditorCard.Draw(() =>
                    {
                        EditorGUILayout.LabelField($"Matches: {matches.Count}", EditorStyles.miniBoldLabel);
                    });
                    EditorCard.Gap(EditorTokens.Pad);
                }
                else if (_allowCreate)
                {
                    EditorCard.Draw(() =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Create new tag: {_searchText}", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (EditorButton.Draw("Create", EditorButtonType.Success, EditorButtonSize.Small, width: 60))
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
                        EditorGUILayout.EndHorizontal();
                    });
                    EditorCard.Gap(EditorTokens.Pad);
                }
            }

            // ── 树 ──
            if (_treeView != null)
            {
                _treeView.searchString = _searchText;
                EditorCard.Draw(() =>
                {
                    var rect = EditorGUILayout.GetControlRect(
                        GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true),
                        GUILayout.MinHeight(200));
                    _treeView.OnGUI(rect);
                });
            }

            EditorCard.Gap(EditorTokens.Pad);

            // ── 底部按钮 ──
            DrawFooter();

            EditorGUILayout.EndVertical();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(EditorTokens.Pad);
        }

        private void DrawSearchField()
        {
            _searchText = EditorSearchBar.Draw(_searchText);
        }

        private void DrawFooter()
        {
            EditorCard.Draw(() =>
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (EditorButton.Draw("Cancel", EditorButtonType.Default, EditorButtonSize.Medium, width: 80))
                    editorWindow.Close();

                if (EditorButton.Draw("Select", EditorButtonType.Primary, EditorButtonSize.Medium,
                        width: 80, enabled: !string.IsNullOrEmpty(_selectedFullTag)))
                {
                    var node = _model.Find(_selectedFullTag);
                    if (node != null)
                        _onSelected?.Invoke(node.UserData as RdTagDefSO);
                    editorWindow.Close();
                }

                EditorGUILayout.EndHorizontal();
            });
        }

    }
}
#endif
