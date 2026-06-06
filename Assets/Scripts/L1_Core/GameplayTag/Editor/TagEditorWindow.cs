#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Core.Editor
{
    /// <summary>
    /// 标签编辑器 — 独立管理窗口。
    /// 查看/搜索/创建/删除 GameplayTag 资产。
    /// Phase 1: 硬编码假数据，验证 UI 布局和交互流程。
    /// </summary>
    public class TagEditorWindow : EditorWindow
    {
        private const float Pad = 6f;
        private const float InspectorWidth = 300f;

        // -- 数据 (Phase 1: 假数据; Phase 2: TagTreeModel) --
        private List<TagNode> _roots = new();

        // -- 视图状态 --
        private string _searchText = "";
        private string _selectedFullTag;
        private readonly Dictionary<string, bool> _foldouts = new();
        private Vector2 _treeScroll;
        private Vector2 _inspectorScroll;

        // -- 创建表单 --
        private string _createLeafName = "";

        [MenuItem("Tools/RedDust/Tag Editor")]
        private static void Open()
            => GetWindow<TagEditorWindow>("Tag Editor");

        private void OnEnable()
        {
            BuildFakeTree();
        }

        private void OnGUI()
        {
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

        // ── Phase 1 假数据 ──
        private void BuildFakeTree()
        {
            /*
             * 模拟已存在的标签树:
             * Damage (exists)
             *   Physical (exists)
             *     Slash (exists), Pierce (exists), Blunt (missing), Bite (missing)
             *   Elemental (missing)
             *     Fire (missing), Cold (missing)
             *   Biological (exists)
             *     Bleed (exists), Disease (missing)
             */
            var damage = MkRoot("Damage", true);
            var physical = MkChild("Physical", "Damage", true, damage);
            MkChild("Slash", "Damage.Physical", true, physical);
            MkChild("Pierce", "Damage.Physical.Pierce", true, physical);
            MkChild("Blunt", "Damage.Physical.Blunt", false, physical);
            MkChild("Bite", "Damage.Physical.Bite", false, physical);

            var elemental = MkChild("Elemental", "Damage.Elemental", false, damage);
            MkChild("Fire", "Damage.Elemental.Fire", false, elemental);
            MkChild("Cold", "Damage.Elemental.Cold", false, elemental);

            var biological = MkChild("Biological", "Damage.Biological", true, damage);
            MkChild("Bleed", "Damage.Biological.Bleed", true, biological);
            MkChild("Disease", "Damage.Biological.Disease", false, biological);

            _roots.Add(damage);
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

        private int CountNodes(List<TagNode> nodes)
        {
            int count = 0;
            foreach (var n in nodes) { count += 1 + CountNodes(n.Children); }
            return count;
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
                // TODO Phase 2
            }

            if (GUILayout.Button("🔄 Refresh", GUILayout.Height(24)))
            {
                BuildFakeTree();
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
            Walk(_roots);
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

            if (!string.IsNullOrEmpty(_searchText) && GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20))) // ✕
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

            // -- 左: Tag Tree --
            DrawTreePanel();

            GUILayout.Space(Pad);

            // -- 右: Inspector --
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
            TagTreeView.DrawTree(_roots, _foldouts, ref _selectedFullTag, _searchText);
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
            EditorGUILayout.LabelField("Create Tag", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(Pad);

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            if (string.IsNullOrEmpty(_selectedFullTag))
            {
                DrawEmptyInspector();
            }
            else
            {
                var node = FindNode(_selectedFullTag);
                if (node == null)
                {
                    DrawEmptyInspector();
                }
                else if (node.Exists)
                {
                    DrawTagDetails(node);
                }
                else
                {
                    DrawCreateForm(node);
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
            var greyLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            greyLabel.normal.textColor = Color.grey;
            EditorGUILayout.LabelField("Select a tag to inspect", greyLabel);
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ── 已有标签 → 详情 ──
        private void DrawTagDetails(TagNode node)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Tag Details", EditorStyles.boldLabel);
            GUILayout.Space(Pad);

            // LeafName
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Leaf", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.LeafName, EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            // FullTag
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("FullTag", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.FullTag, EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            // Depth
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Depth", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.Depth.ToString(), EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            // Parent
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Parent", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.Parent?.FullTag ?? "(root)", EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            // Children count
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Children", EditorStyles.label, GUILayout.Width(60));
            EditorGUILayout.LabelField(node.Children.Count.ToString(), EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(Pad);

            // Actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Asset", GUILayout.Height(24)))
            {
                // TODO Phase 2: EditorGUIUtility.PingObject(node.Asset)
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("Delete", GUILayout.Height(24), GUILayout.Width(80)))
            {
                // TODO Phase 2
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ── 未创建标签 → 创建表单 ──
        private void DrawCreateForm(TagNode node)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField($"Will create: {node.FullTag}", EditorStyles.label);

            // 祖先检查
            var missingAncestors = GetMissingAncestors(node);
            if (missingAncestors.Count > 0)
            {
                GUILayout.Space(4f);
                EditorGUILayout.HelpBox(
                    $"Missing ancestors will also be created:\n{string.Join("\n", missingAncestors)}",
                    MessageType.Info);
            }

            GUILayout.Space(Pad);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Leaf Name", GUILayout.Width(70));
            _createLeafName = EditorGUILayout.TextField(_createLeafName);
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(_createLeafName))
            {
                _createLeafName = node.LeafName;
            }

            GUILayout.Space(Pad);

            var hasName = !string.IsNullOrEmpty(_createLeafName);
            GUI.enabled = hasName;
            GUI.backgroundColor = hasName ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
            if (GUILayout.Button("Create Tag", GUILayout.Height(24)))
            {
                // TODO Phase 2: TagCreator.CreateTagChain(node.FullTag)
                Debug.Log($"[TagEditor] Create: {node.FullTag} (TODO Phase 2)");
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
        }

        private List<string> GetMissingAncestors(TagNode node)
        {
            var missing = new List<string>();
            var current = node.Parent;
            while (current != null)
            {
                if (!current.Exists)
                    missing.Insert(0, current.FullTag);
                current = current.Parent;
            }
            return missing;
        }

        // ── 状态栏 ──
        private void DrawStatusBar()
        {
            var total = CountNodes(_roots);
            var existing = CountExisting(_roots);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);

            EditorGUILayout.LabelField($"{existing} existing · {total - existing} needed", EditorStyles.label);

            if (!string.IsNullOrEmpty(_selectedFullTag))
            {
                GUILayout.FlexibleSpace();
                var sel = FindNode(_selectedFullTag);
                EditorGUILayout.LabelField(_selectedFullTag, EditorStyles.label);
            }

            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
        }

        private int CountExisting(List<TagNode> nodes)
        {
            int count = 0;
            foreach (var n in nodes)
            {
                if (n.Exists) count++;
                count += CountExisting(n.Children);
            }
            return count;
        }
    }
}
#endif
