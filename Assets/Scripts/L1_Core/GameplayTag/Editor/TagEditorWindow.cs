#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core.Editor
{
    public class TagEditorWindow : EditorWindow
    {
        private const float Pad = 6f;
        private const float InspectorWidth = 300f;

        // -- 数据 --
        private TagTreeModel _model;
        private bool _needsRefresh = true;

        // -- 视图 --
        private string _searchText = "";
        private string _selectedFullTag;
        private readonly Dictionary<string, bool> _foldouts = new();
        private Vector2 _treeScroll;
        private Vector2 _inspectorScroll;

        // -- 创建表单 --
        private string _createLeafName = "";

        [MenuItem("RedDust/Tag Editor", priority = 20)]
        private static void Open()
            => GetWindow<TagEditorWindow>("Tag Editor");

        private void OnEnable()
        {
            _model = new TagTreeModel();
            _needsRefresh = true;
        }

        private void OnGUI()
        {
            if (_needsRefresh)
            {
                _model.Refresh();
                _needsRefresh = false;
            }

            GUILayout.Space(Pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            DrawHeader();
            GUILayout.Space(Pad);
            DrawToolbar();
            GUILayout.Space(Pad);
            DrawSearchBar();
            GUILayout.Space(Pad);
            DrawMainContent();
            GUILayout.Space(Pad);
            DrawStatusBar();

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(Pad);
        }

        // ── Header ──
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);

            EditorGUILayout.LabelField("Tag Editor", EditorStyles.largeLabel, GUILayout.ExpandWidth(true));
            var rightStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            EditorGUILayout.LabelField("L1_Core · GameplayTag", rightStyle, GUILayout.Width(180));

            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        // ── Toolbar ──
        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);

            if (GUILayout.Button("＋ Create Tag", GUILayout.Height(24)))
            {
                StartCreateRoot();
            }

            if (GUILayout.Button("🔄 Refresh", GUILayout.Height(24)))
            {
                _needsRefresh = true;
                _foldouts.Clear();
                _selectedFullTag = null;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("▼ All", GUILayout.Height(24)))
                SetAllFoldouts(true);

            if (GUILayout.Button("▲ All", GUILayout.Height(24)))
                SetAllFoldouts(false);

            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        private void SetAllFoldouts(bool expanded)
        {
            void Walk(List<TagNode> nodes)
            {
                foreach (var n in nodes)
                {
                    if (n.Children.Count > 0)
                        _foldouts[n.FullTag] = expanded;
                    Walk(n.Children);
                }
            }
            Walk(_model.Roots);
        }

        // ── Search ──
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);

            EditorGUILayout.LabelField("Search", EditorStyles.label, GUILayout.Width(45));
            _searchText = EditorGUILayout.TextField(_searchText, GUILayout.ExpandWidth(true));

            if (!string.IsNullOrEmpty(_searchText) && GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }

            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        // ── 主区域 ──
        private void DrawMainContent()
        {
            EditorGUILayout.BeginHorizontal();
            DrawTreePanel();
            GUILayout.Space(Pad);
            DrawInspectorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTreePanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            GUILayout.Space(Pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.LabelField("Tag Tree", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2f);

            _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();
            TagTreeView.DrawTree(_model.Roots, _foldouts, ref _selectedFullTag, _searchText, onCreateChild: StartCreateChild);
            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        private void DrawInspectorPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(InspectorWidth));
            GUILayout.Space(Pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            var panelTitle = _isCreating ? "Create Tag"
                : (string.IsNullOrEmpty(_selectedFullTag) ? "Properties" : "Tag Details");
            EditorGUILayout.LabelField(panelTitle, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(Pad);

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

            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();
        }

        // ── 空白 Inspector ──
        private void DrawEmptyInspector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.LabelField("Select a tag or click ＋ to create", EditorStyles.label);
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ── 已有标签详情 ──
        private void DrawTagDetails(TagNode node)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Tag Details", EditorStyles.boldLabel);
            GUILayout.Space(Pad);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Leaf", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.LeafName, EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("FullTag", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.FullTag, EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Depth", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.Depth.ToString(), EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Parent", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.Parent?.FullTag ?? "(root)", EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Children", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.Children.Count.ToString(), EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            var path = AssetDatabase.GetAssetPath(node.Asset);
            if (!string.IsNullOrEmpty(path))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Asset", EditorStyles.label, GUILayout.Width(60));
                EditorGUILayout.LabelField(path, EditorStyles.label);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(Pad);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Asset", GUILayout.Height(24)))
            {
                EditorGUIUtility.PingObject(node.Asset);
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("Delete", GUILayout.Height(24), GUILayout.Width(80)))
            {
                DeleteTag(node);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
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
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            var title = isRoot ? "Create Root Tag" : $"Create Child of '{parentFullTag}'";
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.Space(Pad);

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

            GUILayout.Space(Pad);

            var hasName = !string.IsNullOrEmpty(_createLeafName);
            GUI.enabled = hasName;
            GUI.backgroundColor = hasName ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
            if (GUILayout.Button("Create Tag", GUILayout.Height(24)))
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
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            GUILayout.Space(Pad);

            if (GUILayout.Button("Cancel", GUILayout.Height(24)))
            {
                _isCreating = false;
                _creatingUnderFullTag = null;
                _createLeafName = "";
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
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

        private void StartCreateChild(TagNode parent)
        {
            _isCreating = true;
            _creatingUnderFullTag = parent.FullTag;
            _selectedFullTag = null;
            _createLeafName = "";
        }

        // ── 删除 ──
        private void DeleteTag(TagNode node)
        {
            if (node == null || node.Asset == null) return;

            var tagPath = AssetDatabase.GetAssetPath(node.Asset);

            // 1. 检查外部引用
            var referencers = FindReferencers(tagPath);
            if (referencers.Count > 0)
            {
                EditorUtility.DisplayDialog("Cannot Delete",
                    $"'{node.FullTag}' is referenced by {referencers.Count} other asset(s):\n\n{string.Join("\n", referencers)}\n\nRemove those references first.",
                    "OK");
                return;
            }

            // 2. 收集子孙
            var descendants = new List<GameplayTagDefinitionSO>();
            CollectDescendants(node, descendants);

            // 3. 确认
            var msg = descendants.Count > 0
                ? $"Delete '{node.FullTag}'?\n\nThis tag has {descendants.Count} child tag(s):\n{string.Join("\n", descendants.ConvertAll(t => $"  - {t.FullTag}"))}\n\nThese will also be deleted."
                : $"Delete '{node.FullTag}'?\n\nNo child tags. No external references.";

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

        private void CollectDescendants(TagNode node, List<GameplayTagDefinitionSO> result)
        {
            foreach (var child in node.Children)
            {
                if (child.Asset != null) result.Add(child.Asset);
                CollectDescendants(child, result);
            }
        }

        // ── 状态栏 ──
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);

            EditorGUILayout.LabelField($"{_model.TotalCount} tags", EditorStyles.label);

            if (!string.IsNullOrEmpty(_selectedFullTag))
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(_selectedFullTag, EditorStyles.label);
            }

            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
