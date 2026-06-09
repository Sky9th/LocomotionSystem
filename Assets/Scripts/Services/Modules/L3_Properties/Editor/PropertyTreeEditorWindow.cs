using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDust.Properties.Editor
{
    public class PropertyTreeEditorWindow : EditorWindow
    {
        private const float Pad = 6f;
        private const float LeftWidth = 320f;
        private const float RightWidth = 320f;
        private const float DragThreshold = 10f;

        private static readonly Color ColorLocal = new(0.3f, 0.7f, 0.3f);
        private static readonly Color ColorInherit = Color.gray;
        private static readonly Color ColorSave = new(0.4f, 0.8f, 0.4f);
        private static readonly Color ColorDelete = new(0.9f, 0.3f, 0.3f);
        private static readonly Color ColorDrop = new(0.3f, 0.8f, 0.3f, 0.25f);
        private static readonly Color ColorSelected = new(0.3f, 0.5f, 0.8f, 0.3f);

        // -- left --
        private List<PropertyTreeListItem> _leftTreeRoots = new();
        private Dictionary<string, bool> _treeFoldouts = new();
        private string _leftSearch = "";
        private Vector2 _leftScroll;

        // -- center --
        private PropertyTreeSO _tree;
        private List<CenterTreeNode> _centerTreeRoots = new();
        private Dictionary<string, CenterTreeNode> _centerNodeIndex = new();  // NodeId → node
        private Dictionary<string, bool> _centerFoldouts = new();             // NodeId → expanded
        private List<PropertyNode> _ownNodes = new();
        private HashSet<string> _localIds = new();
        private bool _hasChanges;
        private string _highlightedNodeId;
        private string _searchFilter = "";
        private Vector2 _centerScroll;

        /// <summary>
        /// Display tree node for the center panel. Built from merged PropertyNodes
        /// (folders + leaves) across the full inheritance chain.
        /// </summary>
        private class CenterTreeNode
        {
            public string NodeId;
            public string Path;
            public PropertyDefSO Def;        // null = folder
            public bool IsLocal;
            public bool IsFolder => Def == null;
            public List<CenterTreeNode> Children = new();
        }

        // -- right --
        private List<PropertyDefSO> _allDefs = new();
        private Dictionary<PropertyType, List<PropertyDefSO>> _defsByType = new();
        private HashSet<string> _usedDefIds = new();
        private string _rightSearch = "";
        private Vector2 _rightScroll;

        // -- drag --
        private PropertyDefSO _dragDef;
        private Vector2 _dragStart;

        [MenuItem("RedDust/Property Tree Editor", priority = 41)]
        private static void Open() => GetWindow<PropertyTreeEditorWindow>("Property Tree");

        private void OnEnable()
        {
            minSize = new Vector2(900, 500);
            RefreshTreeList();
            RefreshDefPool();
        }

        private void OnDisable()
        {
            if (_hasChanges && _tree != null)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Save changes to '{_tree.name}' before closing?", "Save", "Discard"))
                    Save();
            }
        }

        // ============================================================
        //  Main layout
        // ============================================================
        private void OnGUI()
        {
            // Ctrl+S
            if (_hasChanges && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.S && Event.current.control)
            { Save(); Event.current.Use(); }

            // outer margins
            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            DrawHeader();
            Shared.EditorUI.EditorUIUtility.CardGap(Pad);
            DrawThreeColumns();

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
        }

        // ---- header ----
        private void DrawHeader()
        {
            Shared.EditorUI.EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Property Tree Editor", EditorStyles.largeLabel);

                var sub = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = Color.gray }
                };
                EditorGUILayout.LabelField("L3_Properties · Editor", sub, GUILayout.Width(180));

                GUILayout.FlexibleSpace();

                var oldBg = GUI.backgroundColor;
                if (_hasChanges) GUI.backgroundColor = ColorSave;
                EditorGUI.BeginDisabledGroup(!_hasChanges);
                var label = _hasChanges ? "Save *" : "Save";
                if (GUILayout.Button(label, GUILayout.Height(24), GUILayout.Width(80)))
                    Save();
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = oldBg;

                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawCenterContent()
        {
            if (_tree == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("No tree selected.",
                    new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, normal = { textColor = Color.gray } });
                GUILayout.FlexibleSpace();
                return;
            }

            // Toolbar: search + add folder
            DrawCenterToolbar();
            Shared.EditorUI.EditorUIUtility.CardGap(Pad);

            // Tree scroll
            _centerScroll = EditorGUILayout.BeginScrollView(_centerScroll);

            for (int i = 0; i < _centerTreeRoots.Count; i++)
            {
                var root = _centerTreeRoots[i];
                // Root must be folder — skip leaves at root level
                if (!root.IsFolder) continue;

                if (i > 0) Shared.EditorUI.EditorUIUtility.CardGap(Pad);
                DrawCenterNode(root);
            }

            // "Add Folder" at bottom of tree
            Shared.EditorUI.EditorUIUtility.CardGap(Pad);
            if (GUILayout.Button("+ Add Folder", GUILayout.Height(22)))
                NewFolderDialog.Show(name => AddFolder(name));

            EditorGUILayout.EndScrollView();
        }

        private void DrawCenterToolbar()
        {
            Shared.EditorUI.EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();

                // Search field
                EditorGUILayout.LabelField("Search", EditorStyles.label, GUILayout.Width(45));
                EditorGUI.BeginChangeCheck();
                _searchFilter = EditorGUILayout.TextField(_searchFilter, GUILayout.ExpandWidth(true), GUILayout.Height(22));
                if (EditorGUI.EndChangeCheck()) Repaint();
                if (!string.IsNullOrEmpty(_searchFilter) && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                { _searchFilter = ""; GUI.FocusControl(null); }

                GUILayout.FlexibleSpace();

                // Add folder button
                if (GUILayout.Button("+ Add Folder", GUILayout.Height(22)))
                    NewFolderDialog.Show(name => AddFolder(name));

                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawThreeColumns()
        {
            EditorGUILayout.BeginHorizontal();

            // Left column
            EditorGUILayout.BeginHorizontal(
                GUILayout.Width(LeftWidth), GUILayout.ExpandHeight(true));
            Shared.EditorUI.EditorUIUtility.DrawCard(Pad, () =>
            {
                DrawLeftContent();
            });
            EditorGUILayout.EndHorizontal();

            Shared.EditorUI.EditorUIUtility.CardGap(Pad);

            // Center column
            EditorGUILayout.BeginHorizontal(
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Shared.EditorUI.EditorUIUtility.DrawCard(Pad, () =>
            {
                DrawCenterContent();
            });
            EditorGUILayout.EndHorizontal();

            Shared.EditorUI.EditorUIUtility.CardGap(Pad);

            // Right column
            EditorGUILayout.BeginHorizontal(
                GUILayout.Width(RightWidth), GUILayout.ExpandHeight(true));
            Shared.EditorUI.EditorUIUtility.DrawCard(Pad, () =>
            {
                DrawRightContent();
            });
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        // ============================================================
        //  Left — Tree Browser
        // ============================================================
        private void DrawLeftContent()
        {

            // Toolbar card
            Shared.EditorUI.EditorUIUtility.DrawCard(Pad, () =>
            {
                // Search row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search", EditorStyles.label, GUILayout.Width(45));
                EditorGUI.BeginChangeCheck();
                _leftSearch = EditorGUILayout.TextField(_leftSearch, GUILayout.ExpandWidth(true), GUILayout.Height(22));
                if (EditorGUI.EndChangeCheck()) RefreshTreeList();
                if (!string.IsNullOrEmpty(_leftSearch) && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                { _leftSearch = ""; RefreshTreeList(); GUI.FocusControl(null); }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(Pad);

                // Action buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ New", GUILayout.Height(22)))
                    NewTreeDialog.Show((name, parent) => { CreateTree(name, parent); RefreshTreeList(); });
                if (GUILayout.Button("Refresh", GUILayout.Height(22)))
                {
                    PropertyDefinitionRegistry.Invalidate();
                    RefreshTreeList();
                    RefreshDefPool();
                }
                EditorGUILayout.EndHorizontal();
            });

            Shared.EditorUI.EditorUIUtility.CardGap(Pad);

            // Tree list
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            PropertyTreeListView.DrawTree(
                _leftTreeRoots,
                _treeFoldouts,
                ref _tree,
                _leftSearch,
                onSelect: HandleTreeSelect,
                selectedColor: ColorSelected);

            if (_leftTreeRoots.Count == 0)
            {
                EditorGUILayout.LabelField("No trees found.", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        // ============================================================
        //  Left — Tree Browser Model
        // ============================================================
        private void BuildLeftTree(List<PropertyTreeSO> allTrees)
        {
            _leftTreeRoots.Clear();

            // Create an item for each tree
            var itemByTree = new Dictionary<PropertyTreeSO, PropertyTreeListItem>();
            foreach (var t in allTrees)
            {
                var path = AssetDatabase.GetAssetPath(t);
                var item = new PropertyTreeListItem
                {
                    DisplayName = t.name,
                    FullPath = string.IsNullOrEmpty(path) ? t.name : path,
                    Tree = t,
                };
                itemByTree[t] = item;
            }

            // Build parent-child links
            foreach (var (tree, item) in itemByTree)
            {
                if (tree.InheritsFrom != null
                    && itemByTree.TryGetValue(tree.InheritsFrom, out var parentItem))
                {
                    item.Parent = parentItem;
                    parentItem.Children.Add(item);
                }
                else
                {
                    _leftTreeRoots.Add(item);
                }
            }

            // Compute Depth, LocalNodeCount, InheritsChainLabel for each item
            void Walk(PropertyTreeListItem item, int depth)
            {
                item.Depth = depth;
                item.LocalNodeCount = CountLocalNodes(item.Tree);
                item.InheritsChainLabel = BuildInheritsChainLabel(item.Tree);
                foreach (var child in item.Children)
                    Walk(child, depth + 1);
            }
            foreach (var root in _leftTreeRoots)
                Walk(root, 0);

            // Sort: roots alpha, then children recursively
            _leftTreeRoots.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            void SortChildren(PropertyTreeListItem item)
            {
                item.Children.Sort((a, b) =>
                    string.CompareOrdinal(a.DisplayName, b.DisplayName));
                foreach (var child in item.Children)
                    SortChildren(child);
            }
            foreach (var root in _leftTreeRoots)
                SortChildren(root);
        }

        private static string BuildInheritsChainLabel(PropertyTreeSO tree)
        {
            if (tree.InheritsFrom == null) return "";
            var chain = new List<string>();
            var t = tree.InheritsFrom;
            while (t != null) { chain.Add(t.name); t = t.InheritsFrom; }
            return "<- " + string.Join(" <- ", chain);
        }

        private void HandleTreeSelect(PropertyTreeSO tree)
        {
            if (_hasChanges && _tree != null && _tree != tree)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Save changes to '{_tree.name}' before switching?", "Save", "Discard"))
                    Save();
                else
                    _hasChanges = false;
            }
            SelectTree(tree);
        }

        #region Center tree rendering

        /// <summary>
        /// Draw a folder card. Layout:
        ///   [▶] ┌─ right Vertical ──────────────────────┐
        ///       │ [___Name___]                   [×]    │
        ///       │ ┌ Property Card ──────────────────┐   │
        ///       │ │ Name  Type  LOCAL/inherit  [×]  │   │
        ///       │ └─────────────────────────────────┘   │
        ///       └───────────────────────────────────────┘
        /// </summary>
        private void DrawCenterNode(CenterTreeNode node)
        {
            if (!node.IsFolder)
            {
                DrawLeafCard(node);
                return;
            }

            Shared.EditorUI.EditorUIUtility.DrawCard(Pad, () =>
            {
                const float FoldoutWidth = 14f;
                const float FoldoutGap = 6f;

                EditorGUILayout.BeginHorizontal();

                // --- Foldout (left, fixed 20px) ---
                float rowH = EditorGUIUtility.singleLineHeight;
                bool hasLeaves = node.Children.Count > 0;
                if (!_centerFoldouts.ContainsKey(node.NodeId))
                    _centerFoldouts[node.NodeId] = true;

                EditorGUILayout.BeginHorizontal(GUILayout.Width(FoldoutWidth + FoldoutGap));
                if (hasLeaves)
                {
                    var foldRect = GUILayoutUtility.GetRect(FoldoutWidth, rowH);
                    _centerFoldouts[node.NodeId] = EditorGUI.Foldout(
                        foldRect, _centerFoldouts[node.NodeId], "", true);
                }
                else
                {
                    var dashRect = GUILayoutUtility.GetRect(FoldoutWidth, rowH);
                    GUI.Label(dashRect, "-", new GUIStyle(EditorStyles.label)
                        { alignment = TextAnchor.MiddleCenter });
                }
                GUILayout.Space(FoldoutGap);
                EditorGUILayout.EndHorizontal();

                // --- Right side (Vertical, expand) ---
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                // Header row: TextField (editable name) + delete
                EditorGUILayout.BeginHorizontal();

                // Editable folder name
                GUI.SetNextControlName($"folder_{node.NodeId}");
                var newName = EditorGUILayout.TextField(node.NodeId, GUILayout.Width(160));
                if (newName != node.NodeId && !string.IsNullOrWhiteSpace(newName) && !newName.Contains("/"))
                {
                    // Commit rename on Enter or focus loss
                    var evt = Event.current;
                    if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                    {
                        RenameFolder(node.NodeId, newName);
                        evt.Use();
                    }
                }

                // Delete (only local folders)
                if (node.IsLocal)
                {
                    var oldDel = GUI.backgroundColor;
                    GUI.backgroundColor = ColorDelete;
                    if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        int localCount = CountLocalDescendants(node);
                        if (localCount == 0 || EditorUtility.DisplayDialog("Delete Folder",
                            $"Delete folder '{node.NodeId}' and its {localCount} local properties?\nInherited properties are not affected.",
                            "Delete", "Cancel"))
                        {
                            DeleteFolderByNode(node.NodeId);
                            GUIUtility.ExitGUI();
                        }
                    }
                    GUI.backgroundColor = oldDel;
                }

                EditorGUILayout.EndHorizontal();

                // --- Nested property cards (when expanded) ---
                if (hasLeaves && _centerFoldouts.TryGetValue(node.NodeId, out var expanded) && expanded)
                {
                    Shared.EditorUI.EditorUIUtility.CardGap(Pad);
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        var child = node.Children[i];
                        if (child.IsFolder) continue;
                        if (!string.IsNullOrEmpty(_searchFilter))
                        {
                            var q = _searchFilter.ToLowerInvariant();
                            if (!child.NodeId.ToLowerInvariant().Contains(q)
                                && (child.Def == null || !child.Def.Id.ToLowerInvariant().Contains(q)))
                                continue;
                        }
                        DrawLeafCard(child);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            });
        }

        /// <summary>
        /// Draw a single property leaf as a bordered card row.
        /// Own properties: normal background. Inherited: gray background + gray text.
        /// </summary>
        private void DrawLeafCard(CenterTreeNode node)
        {
            bool isLocal = node.IsLocal;
            var textColor = isLocal ? Color.white : ColorInherit;

            var oldCardBg = GUI.backgroundColor;
            if (!isLocal) GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.6f);

            Shared.EditorUI.EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();

                // --- Name ---
                GUI.color = textColor;
                if (isLocal)
                {
                    GUILayout.Label(node.NodeId, EditorStyles.label, GUILayout.ExpandWidth(true));
                }
                else
                {
                    GUILayout.Label(node.NodeId, EditorStyles.label, GUILayout.ExpandWidth(true));
                }
                GUI.color = Color.white;

                // --- Type ---
                GUI.color = textColor;
                var typeStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleRight
                };
                GUILayout.Label(node.Def != null ? node.Def.Type.ToString() : "-",
                    typeStyle, GUILayout.Width(90));
                GUI.color = Color.white;

                // --- Delete ---
                if (isLocal)
                {
                    var oldDelBg = GUI.backgroundColor;
                    GUI.backgroundColor = ColorDelete;
                    if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                        DeleteLeaf(node.Path);
                    GUI.backgroundColor = oldDelBg;
                }
                else GUILayout.Space(20);

                EditorGUILayout.EndHorizontal();
            });

            GUI.backgroundColor = oldCardBg;
        }

        private void HandleNodeDragDrop(Rect cardRect, CenterTreeNode node)
        {
            var evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (cardRect.Contains(evt.mousePosition))
                {
                    var dragDef = DragAndDrop.objectReferences.Length > 0
                        ? DragAndDrop.objectReferences[0] as PropertyDefSO : null;

                    if (dragDef != null && !_usedDefIds.Contains(dragDef.Id))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        _highlightedNodeId = node.NodeId;
                        if (evt.type == EventType.DragPerform)
                        { DragAndDrop.AcceptDrag(); AddLeafToNode(node.NodeId, dragDef.Id); _highlightedNodeId = null; }
                        Repaint();
                        evt.Use();
                    }
                    else
                    { DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; _highlightedNodeId = null; }
                }
                else if (_highlightedNodeId == node.NodeId) { _highlightedNodeId = null; Repaint(); }
            }
            else if (evt.type == EventType.DragExited) { _highlightedNodeId = null; Repaint(); }
        }

        private int CountLocalDescendants(CenterTreeNode node)
        {
            int count = 0;
            if (!node.IsFolder && node.IsLocal) count++;
            foreach (var c in node.Children) count += CountLocalDescendants(c);
            return count;
        }

        #endregion

        // ============================================================
        //  Right — Property Pool
        // ============================================================
        private void DrawRightContent()
        {
        }

        private static string DefSummary(PropertyDefSO def)
        {
            return def.Type switch
            {
                PropertyType.Float => $"M:{def.Min}-{def.Max} D:{def.DefaultFloat}",
                PropertyType.Int => $"M:{def.MinInt}-{def.MaxInt} D:{def.DefaultInt}",
                PropertyType.Bool => def.DefaultBool ? "true" : "false",
                PropertyType.String => string.IsNullOrEmpty(def.DefaultString) ? "" : $"\"{def.DefaultString}\"",
                PropertyType.AssetRef => string.IsNullOrEmpty(def.AssetTypeConstraint) ? ""
                    : def.AssetTypeConstraint.Split('.').Last(),
                _ => ""
            };
        }

        // ============================================================
        //  Data
        // ============================================================
        private void SelectTree(PropertyTreeSO tree)
        {
            _tree = tree;
            _hasChanges = false;
            _searchFilter = "";
            _highlightedNodeId = null;
            LoadOwnNodes();
            BuildCenterTree();
            RefreshUsedDefs();
        }

        private void LoadOwnNodes()
        {
            _ownNodes.Clear();
            _localIds.Clear();
            if (_tree == null || string.IsNullOrEmpty(_tree.treeJson)) return;
            var c = JsonUtility.FromJson<PropertyTreeContainer>(_tree.treeJson);
            if (c?.Nodes != null) { _ownNodes.AddRange(c.Nodes);
                foreach (var n in _ownNodes) _localIds.Add(n.NodeId); }
        }

        private void BuildCenterTree()
        {
            _centerTreeRoots.Clear();
            _centerNodeIndex.Clear();
            _centerFoldouts.Clear();
            if (_tree == null) return;

            PropertyDefinitionRegistry.Invalidate();
            var allNodes = _tree.ResolveAllNodes();

            // Create CenterTreeNodes for all merged nodes
            foreach (var (nodeId, node) in allNodes)
            {
                var displayNode = new CenterTreeNode
                {
                    NodeId = nodeId,
                    Def = string.IsNullOrEmpty(node.DefId)
                        ? null : PropertyDefinitionRegistry.FindById(node.DefId),
                    IsLocal = _localIds.Contains(nodeId),
                };
                _centerNodeIndex[nodeId] = displayNode;
            }

            // Link children to parents
            foreach (var (nodeId, node) in allNodes)
            {
                if (!_centerNodeIndex.TryGetValue(nodeId, out var displayNode)) continue;

                if (string.IsNullOrEmpty(node.ParentId)
                    || !_centerNodeIndex.TryGetValue(node.ParentId, out var parentNode))
                {
                    _centerTreeRoots.Add(displayNode);
                }
                else
                {
                    parentNode.Children.Add(displayNode);
                }
            }

            // Build paths recursively
            foreach (var root in _centerTreeRoots)
                AssignPath(root, "");

            // Sort: folders first, then alpha
            SortTreeNodes(_centerTreeRoots);

            // Expand all by default
            foreach (var (nodeId, _) in _centerNodeIndex)
                _centerFoldouts[nodeId] = true;
        }

        private static void AssignPath(CenterTreeNode node, string parentPath)
        {
            node.Path = string.IsNullOrEmpty(parentPath)
                ? node.NodeId : $"{parentPath}/{node.NodeId}";
            foreach (var child in node.Children)
                AssignPath(child, node.Path);
        }

        private static void SortTreeNodes(List<CenterTreeNode> nodes)
        {
            nodes.Sort((a, b) =>
            {
                if (a.IsFolder != b.IsFolder) return a.IsFolder ? -1 : 1;
                return string.CompareOrdinal(a.NodeId, b.NodeId);
            });
            foreach (var n in nodes) SortTreeNodes(n.Children);
        }

        private void RefreshUsedDefs()
        {
            _usedDefIds.Clear();
            void Walk(CenterTreeNode node)
            {
                if (node.Def != null) _usedDefIds.Add(node.Def.Id);
                foreach (var child in node.Children) Walk(child);
            }
            foreach (var root in _centerTreeRoots) Walk(root);
        }

        private void RefreshTreeList()
        {
            _treeFoldouts.Clear();

            var filteredTrees = new List<PropertyTreeSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:PropertyTreeSO"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var t = AssetDatabase.LoadAssetAtPath<PropertyTreeSO>(p);
                if (t == null) continue;
                var q = _leftSearch.ToLowerInvariant();
                if (!string.IsNullOrEmpty(q) && !t.name.ToLowerInvariant().Contains(q)) continue;
                filteredTrees.Add(t);
            }
            filteredTrees.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            BuildLeftTree(filteredTrees);
        }

        private void RefreshDefPool()
        {
            _allDefs.Clear(); _defsByType.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:PropertyDefSO"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var d = AssetDatabase.LoadAssetAtPath<PropertyDefSO>(p);
                if (d == null || string.IsNullOrEmpty(d.Id)) continue;
                var q = _rightSearch.ToLowerInvariant();
                if (!string.IsNullOrEmpty(q)
                    && !d.Id.ToLowerInvariant().Contains(q)
                    && !d.Type.ToString().ToLowerInvariant().Contains(q)) continue;
                _allDefs.Add(d);
            }
            _allDefs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            foreach (var d in _allDefs)
            {
                if (!_defsByType.ContainsKey(d.Type)) _defsByType[d.Type] = new List<PropertyDefSO>();
                _defsByType[d.Type].Add(d);
            }
        }

        private static int CountLocalNodes(PropertyTreeSO t)
        {
            if (string.IsNullOrEmpty(t.treeJson)) return 0;
            var c = JsonUtility.FromJson<PropertyTreeContainer>(t.treeJson);
            if (c?.Nodes == null) return 0;
            // Only count leaf nodes (properties with DefId), not folders
            int count = 0;
            foreach (var n in c.Nodes)
                if (!string.IsNullOrEmpty(n.DefId)) count++;
            return count;
        }

        private static string DisplayName(string path)
        { var i = path.LastIndexOf('/'); return i >= 0 ? path[(i + 1)..] : path; }

        // ============================================================
        //  Edit ops
        // ============================================================
        private void AddLeafToNode(string parentNodeId, string nodeId)
        {
            Undo.RecordObject(_tree, "Add Leaf");
            _ownNodes.Add(new PropertyNode { NodeId = nodeId, ParentId = parentNodeId, DefId = "" });
            _hasChanges = true; RefreshAfterEdit();
        }

        private void ReplaceDef(string leafPath, PropertyDefSO def)
        {
            var nodeId = DisplayName(leafPath);
            var node = _ownNodes.FirstOrDefault(n => n.NodeId == nodeId);
            if (node == null) return;
            Undo.RecordObject(_tree, "Replace Def");
            node.DefId = def.Id; _hasChanges = true; RefreshAfterEdit();
        }

        private void RenameLeaf(string leafPath, string newNodeId)
        {
            var oldNodeId = DisplayName(leafPath);
            var node = _ownNodes.FirstOrDefault(n => n.NodeId == oldNodeId);
            if (node == null || string.IsNullOrWhiteSpace(newNodeId) || newNodeId == oldNodeId) return;
            Undo.RecordObject(_tree, "Rename Leaf");
            node.NodeId = newNodeId; _hasChanges = true; RefreshAfterEdit();
        }

        private void RenameFolder(string oldNodeId, string newNodeId)
        {
            if (string.IsNullOrWhiteSpace(newNodeId) || newNodeId == oldNodeId || newNodeId.Contains("/"))
                return;
            var node = _ownNodes.FirstOrDefault(n => n.NodeId == oldNodeId && string.IsNullOrEmpty(n.DefId));
            if (node == null) return;
            Undo.RecordObject(_tree, "Rename Folder");
            // Rename the folder node itself
            node.NodeId = newNodeId;
            // Update all children whose ParentId points to old name
            foreach (var child in _ownNodes)
            {
                if (child.ParentId == oldNodeId)
                    child.ParentId = newNodeId;
            }
            _hasChanges = true; RefreshAfterEdit();
        }

        private void DeleteLeaf(string leafPath)
        {
            var nodeId = DisplayName(leafPath);
            _ownNodes.RemoveAll(n => n.NodeId == nodeId && !string.IsNullOrEmpty(n.DefId));
            _hasChanges = true; RefreshAfterEdit();
        }

        private void DeleteFolderByNode(string nodeId)
        {
            // Remove the folder node
            _ownNodes.RemoveAll(n => n.NodeId == nodeId && string.IsNullOrEmpty(n.DefId));
            // Remove all local descendants
            if (_centerNodeIndex.TryGetValue(nodeId, out var folderNode))
            {
                void CollectLocal(CenterTreeNode node, List<string> ids)
                {
                    if (!node.IsFolder && node.IsLocal) ids.Add(node.NodeId);
                    foreach (var c in node.Children) CollectLocal(c, ids);
                }
                var ids = new List<string>();
                CollectLocal(folderNode, ids);
                foreach (var id in ids)
                    _ownNodes.RemoveAll(n => n.NodeId == id && !string.IsNullOrEmpty(n.DefId));
            }
            _hasChanges = true; RefreshAfterEdit();
        }

        private void AddFolder(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains("/")) return;
            Undo.RecordObject(_tree, "Add Folder");
            _ownNodes.Add(new PropertyNode { NodeId = name, ParentId = "", DefId = "" });
            _hasChanges = true; RefreshAfterEdit();
        }

        private void RefreshAfterEdit()
        {
            PropertyDefinitionRegistry.Invalidate();
            LoadOwnNodes(); BuildCenterTree(); RefreshUsedDefs(); Repaint();
        }

        private void Save()
        {
            if (_tree == null) return;
            Undo.RecordObject(_tree, "Save Property Tree");
            _tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = _ownNodes }, true);
            EditorUtility.SetDirty(_tree); AssetDatabase.SaveAssets();
            _hasChanges = false; Repaint();
        }

        private void CreateTree(string name, PropertyTreeSO parent)
        {
            var dir = "Assets/Data/Properties/Trees";
            if (!AssetDatabase.IsValidFolder(dir))
            { var parts = dir.Split('/'); AssetDatabase.CreateFolder(string.Join("/", parts.Take(parts.Length - 1)), parts.Last()); }
            var tree = CreateInstance<PropertyTreeSO>();
            tree.InheritsFrom = parent;
            tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = new List<PropertyNode>() }, true);
            AssetDatabase.CreateAsset(tree, $"{dir}/{name}.asset");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); SelectTree(tree);
        }

        // ============================================================
        //  Popups
        // ============================================================
        private static class NewTreeDialog
        {
            public static void Show(Action<string, PropertyTreeSO> cb)
            {
                var w = CreateInstance<NewTreePopup>();
                w._cb = cb; w.minSize = new Vector2(300, 100); w.maxSize = new Vector2(400, 140); w.ShowUtility();
            }
            private class NewTreePopup : EditorWindow
            {
                public Action<string, PropertyTreeSO> _cb;
                private string _name = "";
                private PropertyTreeSO _parent;
                private void OnGUI()
                {
                    EditorGUILayout.LabelField("New PropertyTree", EditorStyles.boldLabel);
                    _name = EditorGUILayout.TextField("Name", _name);
                    _parent = (PropertyTreeSO)EditorGUILayout.ObjectField("InheritsFrom", _parent, typeof(PropertyTreeSO), false);
                    EditorGUILayout.BeginHorizontal();
                    GUI.backgroundColor = ColorSave;
                    EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_name));
                    if (GUILayout.Button("Create", GUILayout.Height(24))) { _cb?.Invoke(_name, _parent); Close(); }
                    EditorGUI.EndDisabledGroup();
                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button("Cancel", GUILayout.Height(24))) Close();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private static class NewFolderDialog
        {
            public static void Show(Action<string> cb)
            {
                var w = CreateInstance<NewFolderPopup>();
                w._cb = cb; w.minSize = new Vector2(250, 80); w.maxSize = new Vector2(350, 110); w.ShowUtility();
            }
            private class NewFolderPopup : EditorWindow
            {
                public Action<string> _cb;
                private string _name = "";
                private void OnGUI()
                {
                    EditorGUILayout.LabelField("Add Folder", EditorStyles.boldLabel);
                    _name = EditorGUILayout.TextField("Name", _name);
                    bool valid = !string.IsNullOrWhiteSpace(_name) && !_name.Contains("/");
                    EditorGUILayout.BeginHorizontal();
                    GUI.backgroundColor = ColorSave;
                    EditorGUI.BeginDisabledGroup(!valid);
                    if (GUILayout.Button("Add", GUILayout.Height(24))) { _cb?.Invoke(_name); Close(); }
                    EditorGUI.EndDisabledGroup();
                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button("Cancel", GUILayout.Height(24))) Close();
                    EditorGUILayout.EndHorizontal();
                    if (!string.IsNullOrWhiteSpace(_name) && !valid)
                        EditorGUILayout.HelpBox("Name cannot be empty or contain '/'.", MessageType.Warning);
                }
            }
        }
    }
}
