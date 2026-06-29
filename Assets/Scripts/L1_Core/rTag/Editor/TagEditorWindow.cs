#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core.Editor
{
    public class TagEditorWindow : EditorWindow
    {
        private const float InspectorWidth = 300f;

        // -- 数据 --
        private TagTreeModel _model;
        private bool _needsRefresh = true;

        // -- 视图 --
        private string _searchText = "";
        private string _selectedFullTag;
        private EditorTreeView _treeView;
        private Vector2 _inspectorScroll;

        // -- 创建表单 --
        private string _createLeafName = "";

        [MenuItem("RedDust/Tag Editor", priority = 5)]
        private static void Open()
            => GetWindow<TagEditorWindow>("Tag Editor");

        private void OnEnable()
        {
            _model = new TagTreeModel();
            _treeView = new EditorTreeView();
            _needsRefresh = true;
        }

        private void OnGUI()
        {
            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginVertical();

            DrawHeader();
            GUILayout.Space(EditorTokens.Pad);
            DrawToolbar();
            GUILayout.Space(EditorTokens.Pad);
            DrawSearchBar();
            GUILayout.Space(EditorTokens.Pad);
            DrawMainContent();
            GUILayout.Space(EditorTokens.Pad);
            DrawStatusBar();

            EditorGUILayout.EndVertical();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(EditorTokens.Pad);
        }

        // ── Header ──
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.LabelField("Tag Editor", EditorStyles.largeLabel, GUILayout.ExpandWidth(true));
            var rightStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            EditorGUILayout.LabelField("L1_Core · rTag", rightStyle, GUILayout.Width(180));

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndVertical();
        }

        // ── Toolbar ──
        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);

            if (EditorButton.Draw("＋ Create Tag", size: EditorButtonSize.Small))
                StartCreateRoot();

            if (EditorButton.Draw("🔄 Refresh", size: EditorButtonSize.Small))
            {
                _needsRefresh = true;
                _selectedFullTag = null;
            }

            GUILayout.FlexibleSpace();

            if (_treeView != null)
            {
                if (EditorButton.Draw("▼ All", size: EditorButtonSize.Small))
                    _treeView.ExpandAll();

                if (EditorButton.Draw("▲ All", size: EditorButtonSize.Small))
                    _treeView.CollapseAll();
            }

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndVertical();
        }

        // ── Search ──
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.LabelField("Search", EditorStyles.label, GUILayout.Width(45));
            _searchText = EditorGUILayout.TextField(_searchText, GUILayout.ExpandWidth(true));

            if (!string.IsNullOrEmpty(_searchText) && EditorButton.Draw("✕", size: EditorButtonSize.Small, width: 20f))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndVertical();
        }

        // ── 主区域 ──
        private void DrawMainContent()
        {
            EditorGUILayout.BeginHorizontal();
            DrawTreePanel();
            GUILayout.Space(EditorTokens.Pad);
            DrawInspectorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTreePanel()
        {
            if (_needsRefresh)
            {
                _model.Refresh();
                _treeView.SetData(_model.Roots, onSelect: node =>
                {
                    _selectedFullTag = node?.FullPath;
                    _isCreating = false;
                    Repaint();
                });
                _needsRefresh = false;
            }
            _treeView.searchString = _searchText;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            var rect = EditorGUILayout.GetControlRect(
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _treeView.OnGUI(rect);
            EditorGUILayout.EndVertical();
        }

        private void DrawInspectorPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(InspectorWidth));
            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            var panelTitle = _isCreating ? "Create Tag"
                : (string.IsNullOrEmpty(_selectedFullTag) ? "Properties" : "Tag Details");
            EditorGUILayout.LabelField(panelTitle, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(EditorTokens.Pad);

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            if (_isCreating)
            {
                DrawCreateForm(_creatingUnderFullTag);
            }
            else if (string.IsNullOrEmpty(_selectedFullTag))
            {
                DrawEmptyInspector();
            }
            else
            {
                var node = _model.Find(_selectedFullTag);
                if (node != null)
                {
                    DrawTagDetails(node);
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndVertical();
        }

        // ── 空白 Inspector ──
        private void DrawEmptyInspector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.LabelField("Select a tag or click ＋ to create", EditorStyles.label);
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ── 已有标签详情 ──
        private void DrawTagDetails(EditorTreeNode node)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Tag Details", EditorStyles.boldLabel);
            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Leaf", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.DisplayName, EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("FullTag", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.FullPath, EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Depth", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.Depth.ToString(), EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Parent", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.Parent?.FullPath ?? "(root)", EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Children", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.Children.Count.ToString(), EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            var path = AssetDatabase.GetAssetPath(node.UserData as rTagDefSO);
            if (!string.IsNullOrEmpty(path))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Asset", EditorStyles.label, GUILayout.Width(60));
                EditorGUILayout.LabelField(path, EditorStyles.label);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.BeginHorizontal();
            if (EditorButton.Draw("Ping Asset", size: EditorButtonSize.Small))
                EditorGUIUtility.PingObject(node.UserData as rTagDefSO);

            if (EditorButton.Draw("Delete", EditorButtonType.Danger, width: 80f))
                DeleteTag(node);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ── 创建表单（parentFullTag=null → 根标签；非null → 子标签）──
        private void DrawCreateForm(string parentFullTag)
        {
            var isRoot = string.IsNullOrEmpty(parentFullTag);
            var fullTag = isRoot ? _createLeafName : $"{parentFullTag}.{_createLeafName}";
            if (!string.IsNullOrEmpty(_createLeafName))
                fullTag = isRoot ? _createLeafName : $"{parentFullTag}.{_createLeafName}";

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginVertical();

            var title = isRoot ? "Create Root Tag" : $"Create Child of '{parentFullTag}'";
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Leaf Name", GUILayout.Width(70));
            _createLeafName = EditorGUILayout.TextField(_createLeafName);
            EditorGUILayout.EndHorizontal();

            if (!isRoot && !string.IsNullOrEmpty(_createLeafName))
            {
                var projected = $"{parentFullTag}.{_createLeafName}";
                var missing = _model.GetMissingAncestors(projected);
                if (missing.Count > 0)
                {
                    GUILayout.Space(4f);
                    EditorGUILayout.HelpBox(
                        $"Will also create:\n{string.Join("\n", missing)}",
                        MessageType.Info);
                }
                EditorGUILayout.LabelField($"FullTag: {projected}", EditorStyles.label);
            }

            GUILayout.Space(EditorTokens.Pad);

            var hasName = !string.IsNullOrEmpty(_createLeafName);
            if (EditorButton.Draw("Create Tag", EditorButtonType.Success, EditorButtonSize.Small, enabled: hasName))
            {
                var target = isRoot ? _createLeafName : $"{parentFullTag}.{_createLeafName}";
                try
                {
                    var created = TagCreator.CreateTagChain(target);
                    _needsRefresh = true;
                    _selectedFullTag = created.FullTag;
                    _isCreating = false;
                    _creatingUnderFullTag = null;
                    _createLeafName = "";
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TagEditor] Failed to create tag '{target}': {ex.Message}");
                }
            }

            GUILayout.Space(EditorTokens.Pad);

            if (EditorButton.Draw("Cancel", size: EditorButtonSize.Small))
            {
                _isCreating = false;
                _creatingUnderFullTag = null;
                _createLeafName = "";
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ── 创建入口 ──
        private bool _isCreating;
        private string _creatingUnderFullTag;

        private void StartCreateRoot()
        {
            _isCreating = true;
            _creatingUnderFullTag = null;
            _selectedFullTag = null;
            _createLeafName = "";
        }

        private void StartCreateChild(EditorTreeNode parent)
        {
            _isCreating = true;
            _creatingUnderFullTag = parent.FullPath;
            _selectedFullTag = null;
            _createLeafName = "";
        }

        // ── 删除 ──
        private void DeleteTag(EditorTreeNode node)
        {
            if (node == null || node.UserData as rTagDefSO == null) return;

            var tagPath = AssetDatabase.GetAssetPath(node.UserData as rTagDefSO);

            // 1. 检查外部引用
            var referencers = FindReferencers(tagPath);
            if (referencers.Count > 0)
            {
                EditorUtility.DisplayDialog("Cannot Delete",
                    $"'{node.FullPath}' is referenced by {referencers.Count} other asset(s):\n\n{string.Join("\n", referencers)}\n\nRemove those references first.",
                    "OK");
                return;
            }

            // 2. 收集子孙
            var descendants = new List<rTagDefSO>();
            CollectDescendants(node, descendants);

            // 3. 确认
            var msg = descendants.Count > 0
                ? $"Delete '{node.FullPath}'?\n\nThis tag has {descendants.Count} child tag(s):\n{string.Join("\n", descendants.ConvertAll(t => $"  - {t.FullTag}"))}\n\nThese will also be deleted."
                : $"Delete '{node.FullPath}'?\n\nNo child tags. No external references.";

            if (!EditorUtility.DisplayDialog("Delete Tag", msg, "Delete", "Cancel"))
                return;

            // 4. 执行
            foreach (var child in descendants)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(child));
            AssetDatabase.DeleteAsset(tagPath);
            AssetDatabase.SaveAssets();

            _selectedFullTag = null;
            _needsRefresh = true;
        }

        /// <summary>查找引用了指定路径资产的所有资产</summary>
        private static List<string> FindReferencers(string assetPath)
        {
            var refs = new List<string>();
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return refs;

            var allGuids = AssetDatabase.FindAssets("", new[] { "Assets/Data", "Assets/Scripts" });
            foreach (var g in allGuids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p == assetPath) continue;
                if (p.EndsWith(".cs")) continue;

                var deps = AssetDatabase.GetDependencies(p, false);
                if (Array.IndexOf(deps, assetPath) >= 0)
                    refs.Add(p);
            }
            return refs;
        }

        private void CollectDescendants(EditorTreeNode node, List<rTagDefSO> result)
        {
            foreach (var child in node.Children)
            {
                if (child.UserData as rTagDefSO != null) result.Add(child.UserData as rTagDefSO);
                CollectDescendants(child, result);
            }
        }

        // ── 状态栏 ──
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.LabelField($"{_model.TotalCount} tags", EditorStyles.label);

            if (!string.IsNullOrEmpty(_selectedFullTag))
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(_selectedFullTag, EditorStyles.label);
            }

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
