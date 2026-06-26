using System;
using System.Collections.Generic;
using System.Linq;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Properties.Editor
{
    public class PropertyTreeEditorWindow : EditorWindow
    {
        private const float LeftWidth = 320f;
        private const float RightWidth = 320f;
        private const float DragThreshold = 10f;

        private static readonly Color ColorInherit = Color.gray;
        private static readonly Color ColorDelete = new(0.9f, 0.3f, 0.3f);
        private static readonly Color ColorSelected = new(0.3f, 0.5f, 0.8f, 0.3f);

        // Cached GUIStyles — lazy-init to avoid NRE during static ctor (EditorStyles not ready yet)
        private static GUIStyle _emptyCenterStyle;
        private static GUIStyle EmptyCenterStyle => _emptyCenterStyle ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleCenter, fontSize = 13, normal = { textColor = Color.gray } };
        private static GUIStyle _searchToolbarLabel;
        private static GUIStyle SearchToolbarLabel => _searchToolbarLabel ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleLeft };
        private static GUIStyle _dashLabel;
        private static GUIStyle DashLabel => _dashLabel ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleCenter };
        private static GUIStyle _anchorIcon;
        private static GUIStyle AnchorIcon => _anchorIcon ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
        private static GUIStyle _floatingNameStyle;
        private static GUIStyle FloatingNameStyle => _floatingNameStyle ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleLeft, fontSize = EditorStyles.label.fontSize };
        private static GUIStyle _floatingTypeStyle;
        private static GUIStyle FloatingTypeStyle => _floatingTypeStyle ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleRight, fontSize = EditorStyles.label.fontSize };

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
        private HashSet<string> _inheritedNodeIds = new(); // cached, updated in BuildCenterTree
        private HashSet<string> _warnedConflicts = new(); // suppress duplicate warnings per session
        private bool _hasChanges;
        private string _searchFilter = "";

        // Drag & drop reorder
        private string _dragNodeId = null;
        private string _dragParentId = null;
        private int _dropIndex = -1;
        private string _dropParentId;

        // Track inline rename editing state per folder (nodeId → edited name).
        // We cache the edited text here so we can feed it back as the TextField's
        // value parameter — this prevents IMGUI from resetting the text on focus loss.
        private Dictionary<string, string> _folderEdits = new();
        private float _bestDropDistance;
        private float _defDropTargetY;
        private string _defDropTargetNodeId;
        private int _defDropIndex;
        private int _folderDropIndex = -1;
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

        [MenuItem("RedDust/Property Tree Editor", priority = 5)]
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
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginVertical();

            DrawHeader();
            Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);
            DrawThreeColumns();

            EditorGUILayout.EndVertical();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(EditorTokens.Pad);
        }

        // ---- header ----
        private void DrawHeader()
        {
            Shared.EditorUI.EditorCard.Draw(() =>
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorLabel.Draw("Property Tree Editor", style: EditorTokens.HeaderTitleStyle);
                    var subWidth = EditorTokens.BreadcrumbStyle.CalcSize(new GUIContent("L3_Properties · Editor")).x;
                    EditorLabel.Draw("L3_Properties · Editor", subWidth, style: EditorTokens.BreadcrumbStyle);
                    GUILayout.FlexibleSpace();
                    if (EditorButton.Primary(_hasChanges ? "Save *" : "Save",
                            EditorButtonSize.Medium, enabled: _hasChanges))
                        Save();
                    EditorGUILayout.EndHorizontal();
                });
        }

        private void DrawCenterContent()
        {
            if (_tree == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("No tree selected.", EmptyCenterStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            // Toolbar: search + add folder
            DrawCenterToolbar();
            Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);

            // Tree scroll
            _centerScroll = EditorGUILayout.BeginScrollView(_centerScroll);

            // Reset per-frame drag state
            _dropParentId = null;
            _bestDropDistance = float.MaxValue;
            _defDropTargetY = float.MinValue;
            _defDropTargetNodeId = null;
            _folderDropIndex = -1;

            // Collect visible roots
            var visibleRoots = new List<CenterTreeNode>();
            foreach (var root in _centerTreeRoots)
                if (root.IsFolder) visibleRoots.Add(root);

            bool draggingFolder = IsDraggingFolder();

            // Draw and capture rects
            var folderRects = new List<Rect>();
            for (int i = 0; i < visibleRoots.Count; i++)
            {
                var root = visibleRoots[i];

                if (i > 0) Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);

                if (root.NodeId == _dragNodeId && draggingFolder)
                {
                    folderRects.Add(GUILayoutUtility.GetRect(0, 0));
                    continue;
                }

                DrawCenterNode(root);
                folderRects.Add(GUILayoutUtility.GetLastRect());
            }

            // Draw root-level leaf nodes (e.g., Entity base properties — DisplayName, Icon...)
            foreach (var root in _centerTreeRoots)
            {
                if (root.IsFolder) continue;
                DrawLeafCard(root);
            }

            // Overlay drop handling (no layout space, same pattern as property reorder)
            if (draggingFolder)
            {
                HandleFolderReorder(visibleRoots, folderRects);
                DrawFloatingCard();
            }

            // Handle drag end at top level
            if (!string.IsNullOrEmpty(_dragNodeId) && Event.current.type == EventType.DragExited)
            {
                CleanupDrag();
                Repaint();
            }

            // Finalize property reorder
            if (!string.IsNullOrEmpty(_dragNodeId) && Event.current.type == EventType.DragPerform && !string.IsNullOrEmpty(_dropParentId))
            {
                DragAndDrop.AcceptDrag();
                ReorderLeaf(_dragNodeId, _dropParentId, _dropIndex);
                CleanupDrag();
                Repaint();
                Event.current.Use();
            }

            // Finalize DefSO drop from right panel: closest folder below mouse wins
            if (string.IsNullOrEmpty(_dragNodeId) && Event.current.type == EventType.DragPerform && !string.IsNullOrEmpty(_defDropTargetNodeId))
            {
                var dragDef = DragAndDrop.objectReferences.Length > 0
                    ? DragAndDrop.objectReferences[0] as PropertyDefSO : null;
                if (dragDef != null && !_usedDefIds.Contains(dragDef.Id))
                {
                    DragAndDrop.AcceptDrag();
                    AddDefToFolder(_defDropTargetNodeId, dragDef, _defDropIndex);
                }
                _defDropTargetNodeId = null;
                _defDropTargetY = float.MinValue;
                _defDropIndex = 0;
                Repaint();
                Event.current.Use();
            }

            // "Add Folder" at bottom of tree
            Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);
            if (EditorButton.Draw("+ Add Folder", size: EditorButtonSize.Small))
                AddFolder("New Folder");

            EditorGUILayout.EndScrollView();
        }

        private void DrawCenterToolbar()
        {
            Shared.EditorUI.EditorCard.Draw(() =>
            {
                EditorGUILayout.BeginHorizontal();

                // Search field — use shared search row
                _searchFilter = EditorSearchBar.Draw(_searchFilter, 45f);

                GUILayout.FlexibleSpace();

                if (EditorButton.Draw("+ Add Folder", size: EditorButtonSize.Small))
                    AddFolder("New Folder");

                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawThreeColumns()
        {
            EditorGUILayout.BeginHorizontal();

            // Left column
            EditorGUILayout.BeginHorizontal(
                GUILayout.Width(LeftWidth), GUILayout.ExpandHeight(true));
            Shared.EditorUI.EditorCard.Draw(() =>
            {
                DrawLeftContent();
            });
            EditorGUILayout.EndHorizontal();

            Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);

            // Center column
            EditorGUILayout.BeginHorizontal(
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Shared.EditorUI.EditorCard.Draw(() =>
            {
                DrawCenterContent();
            });
            EditorGUILayout.EndHorizontal();

            Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);

            // Right column
            EditorGUILayout.BeginHorizontal(
                GUILayout.Width(RightWidth), GUILayout.ExpandHeight(true));
            Shared.EditorUI.EditorCard.Draw(() =>
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
            Shared.EditorUI.EditorCard.Draw(() =>
            {
                // Search row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search", SearchToolbarLabel, GUILayout.Width(45), GUILayout.Height(22));
                EditorGUI.BeginChangeCheck();
                _leftSearch = EditorGUILayout.TextField(_leftSearch, GUILayout.ExpandWidth(true), GUILayout.Height(22));
                if (EditorGUI.EndChangeCheck()) RefreshTreeList();
                if (!string.IsNullOrEmpty(_leftSearch) && EditorButton.Draw("x", size: EditorButtonSize.Small, width: 20f))
                { _leftSearch = ""; RefreshTreeList(); GUI.FocusControl(null); }
                EditorGUILayout.EndHorizontal();

                EditorCard.Gap(EditorTokens.Pad);

                // Action buttons
                EditorGUILayout.BeginHorizontal();
                if (EditorButton.Draw("+ New", size: EditorButtonSize.Small))
                    PropertyTreeEditorPopups.NewTreeDialog.Show((name, parent) => { CreateTree(name, parent); RefreshTreeList(); });
                if (EditorButton.Draw("Refresh", size: EditorButtonSize.Small))
                {
                    PropertyDefinitionRegistry.Invalidate();
                    RefreshTreeList();
                    RefreshDefPool();
                }
                EditorGUILayout.EndHorizontal();
            });

            Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);

            // Tree list
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            PropertyTreeListView.DrawTree(
                _leftTreeRoots,
                _treeFoldouts,
                ref _tree,
                _leftSearch,
                onSelect: HandleTreeSelect,
                selectedColor: ColorSelected,
                onDelete: HandleTreeDelete,
                onCreateChild: HandleCreateChild);

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
                // Entity 是隐式基树，不显示在左侧面板
                if (t.name == "Entity") continue;

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

        private void HandleTreeDelete(PropertyTreeSO tree)
        {
            if (tree == null) return;
            var path = AssetDatabase.GetAssetPath(tree);
            if (string.IsNullOrEmpty(path)) return;

            // Verify no child trees inherit from this one
            foreach (var guid in AssetDatabase.FindAssets("t:PropertyTreeSO"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p == path) continue;
                var t = AssetDatabase.LoadAssetAtPath<PropertyTreeSO>(p);
                if (t != null && t.InheritsFrom == tree)
                {
                    EditorUtility.DisplayDialog("Cannot Delete",
                        $"'{tree.name}' has child trees that inherit from it.\nDelete '{t.name}' first.", "OK");
                    return;
                }
            }

            if (!EditorUtility.DisplayDialog("Delete Tree",
                $"Delete '{tree.name}'?\nThis will delete the asset file permanently.", "Delete", "Cancel"))
                return;

            if (_tree == tree) { _tree = null; _centerTreeRoots.Clear(); }
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            RefreshTreeList();
        }

        private void HandleCreateChild(PropertyTreeSO parent)
        {
            if (parent == null) return;
            PropertyTreeEditorPopups.NewTreeDialog.Show((name, p) =>
            {
                CreateTree(name, p);
                RefreshTreeList();
            }, parent);
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

            Shared.EditorUI.EditorCard.Draw(() =>
            {
                const float AnchorWidth = 10f;
                const float FoldoutWidth = 14f;
                const float FoldoutGap = 6f;

                EditorGUILayout.BeginHorizontal();

                // --- Anchor + Foldout (left, fixed 30px) ---
                float rowH = EditorGUIUtility.singleLineHeight;
                bool hasLeaves = node.Children.Count > 0;
                if (!_centerFoldouts.ContainsKey(node.NodeId))
                    _centerFoldouts[node.NodeId] = true;

                EditorGUILayout.BeginHorizontal(GUILayout.Width(AnchorWidth + FoldoutWidth + FoldoutGap));

                // Drag anchor "≡" — hover highlight
                var anchorRect = GUILayoutUtility.GetRect(AnchorWidth, rowH);
                bool anchorHover = anchorRect.Contains(Event.current.mousePosition);
                var oldAnchorColor = GUI.color;
                GUI.color = anchorHover ? new Color(0.7f, 0.7f, 0.7f) : Color.gray;
                GUI.Label(anchorRect, "≡", AnchorIcon);
                GUI.color = oldAnchorColor;

                if (Event.current.type == EventType.MouseDrag
                    && anchorRect.Contains(Event.current.mousePosition)
                    && string.IsNullOrEmpty(_dragNodeId) && node.IsLocal)
                {
                    _dragNodeId = node.NodeId;
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new UnityEngine.Object[] { _tree };
                    DragAndDrop.StartDrag("FolderDrag");
                    Event.current.Use();
                }

                // Foldout
                if (hasLeaves)
                {
                    var foldRect = GUILayoutUtility.GetRect(FoldoutWidth, rowH);
                    _centerFoldouts[node.NodeId] = EditorGUI.Foldout(
                        foldRect, _centerFoldouts[node.NodeId], "", true);
                }
                else
                {
                    var dashRect = GUILayoutUtility.GetRect(FoldoutWidth, rowH);
                    GUI.Label(dashRect, "-", DashLabel);
                }
                GUILayout.Space(FoldoutGap);
                EditorGUILayout.EndHorizontal();

                // --- Right side (Vertical, expand) ---
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                // Header row: TextField (editable name) + delete
                EditorGUILayout.BeginHorizontal();

                // Editable folder name — uses cached edit buffer so IMGUI
                // doesn't reset the value when the TextField loses focus.
                string ctrlName = $"folder_{node.NodeId}";
                bool hasFocus = GUI.GetNameOfFocusedControl() == ctrlName;
                bool hasCachedEdit = _folderEdits.TryGetValue(node.NodeId, out var cachedName);

                // Just lost focus? Commit the rename, then fall through to normal display.
                if (!hasFocus && hasCachedEdit)
                {
                    if (cachedName != node.NodeId)
                        TryRenameFolder(node.NodeId, cachedName);
                    _folderEdits.Remove(node.NodeId);
                    hasCachedEdit = false;
                }

                string displayName = hasCachedEdit ? cachedName : node.NodeId;
                GUI.SetNextControlName(ctrlName);
                GUI.enabled = string.IsNullOrEmpty(_dragNodeId) && node.IsLocal;
                var newName = EditorGUILayout.TextField(displayName, GUILayout.Width(160));
                GUI.enabled = true;

                // Cache changes while the TextField has focus.
                if (hasFocus && newName != displayName)
                    _folderEdits[node.NodeId] = newName;

                // Commit on Enter.
                if (hasFocus)
                {
                    var evt = Event.current;
                    if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                    {
                        if (_folderEdits.TryGetValue(node.NodeId, out var enterName) && enterName != node.NodeId)
                            TryRenameFolder(node.NodeId, enterName);
                        _folderEdits.Remove(node.NodeId);
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
                var headerEndY = GUILayoutUtility.GetLastRect().yMax;

                var visibleChildren = new List<CenterTreeNode>();
                for (int i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    if (child.IsFolder) continue;
                    visibleChildren.Add(child);
                }

                // --- Nested property cards (when expanded) ---
                var cardRects = new List<Rect>();
                bool expanded = _centerFoldouts.TryGetValue(node.NodeId, out var exp) && exp;
                if (hasLeaves && expanded)
                {
                    Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);

                    for (int i = 0; i < visibleChildren.Count; i++)
                    {
                        if (visibleChildren[i].NodeId == _dragNodeId)
                        {
                            cardRects.Add(GUILayoutUtility.GetRect(0, 0));
                            continue;
                        }
                        bool match = !string.IsNullOrEmpty(_searchFilter)
                            && (visibleChildren[i].NodeId.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant())
                                || (visibleChildren[i].Def != null && visibleChildren[i].Def.Id.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant())));
                        DrawLeafCard(visibleChildren[i], match);
                        cardRects.Add(GUILayoutUtility.GetLastRect());
                    }

                    // Draw floating card at mouse position during drag
                    if (!string.IsNullOrEmpty(_dragNodeId))
                        DrawFloatingCard();
                }
                else if (!string.IsNullOrEmpty(_dragNodeId))
                {
                    // Collapsed or empty: use header area as drop zone
                    cardRects.Add(GUILayoutUtility.GetLastRect());
                }

                // Handle property reorder drop — exclude dragged node from sibling list
                if (!string.IsNullOrEmpty(_dragNodeId))
                {
                    var siblings = new List<CenterTreeNode>();
                    foreach (var c in visibleChildren)
                        if (c.NodeId != _dragNodeId)
                            siblings.Add(c);
                    HandlePropertyDrop(node.NodeId, siblings, cardRects);
                }

                HandleDefDrop(node, headerEndY, visibleChildren, cardRects);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            });
        }

        /// <summary>
        /// Accept PropertyDefSO drops from the right panel into this folder.
        /// (Separate from property reorder to avoid conflict with _dragNodeId system.)
        /// </summary>
        private void HandleDefDrop(CenterTreeNode node, float headerEndY, List<CenterTreeNode> children, List<Rect> cardRects)
        {
            var evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
            if (!string.IsNullOrEmpty(_dragNodeId)) return;

            var dragDef = DragAndDrop.objectReferences.Length > 0
                ? DragAndDrop.objectReferences[0] as PropertyDefSO : null;
            if (dragDef == null) return;

            // Mouse must be below this folder's header
            if (evt.mousePosition.y < headerEndY) return;

            // Track closest folder below mouse (highest headerEndY that's still ≤ mouse)
            if (headerEndY > _defDropTargetY)
            {
                _defDropTargetY = headerEndY;
                _defDropTargetNodeId = node.NodeId;

                // Calculate insert index from card rects
                _defDropIndex = children.Count;
                var mouseY = evt.mousePosition.y;
                for (int i = 0; i < cardRects.Count; i++)
                {
                    if (cardRects[i].height <= 0) continue;
                    if (mouseY < cardRects[i].center.y) { _defDropIndex = i; break; }
                }

                DragAndDrop.visualMode = _usedDefIds.Contains(dragDef.Id)
                    ? DragAndDropVisualMode.Rejected
                    : DragAndDropVisualMode.Copy;
                Repaint();
            }
        }

        /// <summary>
        /// Draw a single property leaf as a bordered card row.
        /// Own properties: normal background. Inherited: gray background + gray text.
        /// </summary>
        private void DrawLeafCard(CenterTreeNode node, bool searchMatch = false)
        {
            bool isLocal = node.IsLocal;
            var textColor = isLocal ? Color.white : ColorInherit;

            var oldCardBg = GUI.backgroundColor;
            if (!isLocal) GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.6f);

            Shared.EditorUI.EditorCard.Draw(() =>
            {
                var rowH = EditorGUIUtility.singleLineHeight;
                EditorGUILayout.BeginHorizontal(GUILayout.Height(rowH));

                // --- Name (native tooltip = Def.Description, with Type fallback) ---
                GUI.color = textColor;
                var tooltip = node.Def != null
                    ? (string.IsNullOrEmpty(node.Def.Description)
                        ? node.Def.Type.ToString()
                        : $"{node.Def.Description}\n\nType: {node.Def.Type}")
                    : "-";
                var nameContent = new GUIContent(node.NodeId, tooltip);
                var nameStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = searchMatch ? FontStyle.Bold : FontStyle.Normal
                };
                GUILayout.Label(nameContent, nameStyle, GUILayout.ExpandWidth(true), GUILayout.Height(rowH));
                GUI.color = Color.white;

                // --- Type ---
                GUI.color = textColor;
                var typeStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleRight
                };
                GUILayout.Label(node.Def != null ? node.Def.Type.ToString() : "-",
                    typeStyle, GUILayout.Width(60), GUILayout.Height(rowH));
                GUI.color = Color.white;

                // --- Detail button "?" ---
                if (node.Def != null)
                {
                    if (EditorButton.Draw("?", EditorButtonType.Primary, EditorButtonSize.Small, 20f))
                        PropertyTreeEditorPopups.DefDetailPopup.Show(node.Def);
                }
                else GUILayout.Space(20);

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

            // --- Drag initiation ---
            var cardRect = GUILayoutUtility.GetLastRect();
            if (isLocal && Event.current.type == EventType.MouseDrag && cardRect.Contains(Event.current.mousePosition))
            {
                _dragNodeId = node.NodeId;
                _dragParentId = FindParentId(node.NodeId);
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new UnityEngine.Object[] { _tree };
                DragAndDrop.StartDrag("PropertyDrag");
                Event.current.Use();
            }
        }

        /// <summary>
        /// Detect drop position from card rects, handle DragUpdated/DragPerform,
        /// and draw an overlay indicator line. Does NOT consume layout space.
        /// </summary>
        private void HandlePropertyDrop(string parentId, List<CenterTreeNode> children, List<Rect> cardRects)
        {
            if (string.IsNullOrEmpty(_dragNodeId)) return;
            if (IsDraggingFolder()) return; // Folder drag uses separate system
            if (cardRects.Count == 0) return;

            var evt = Event.current;
            var mouseY = evt.mousePosition.y;

            // Empty/collapsed folder: check if mouse is over the header area
            if (children.Count == 0)
            {
                if (!cardRects[0].Contains(evt.mousePosition)) return;
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    _dropIndex = 0;
                    _dropParentId = parentId;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        ReorderLeaf(_dragNodeId, parentId, 0);
                        CleanupDrag();
                    }
                    Repaint();
                    evt.Use();
                }
                if (_dropIndex == 0 && _dropParentId == parentId && Event.current.type == EventType.Repaint)
                {
                    var r = cardRects[0];
                    EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 4), new Color(0.3f, 0.7f, 1f, 0.8f));
                }
                return;
            }

            // Filter placeholder rects (from skipped dragged card)
            var validRects = new List<Rect>();
            for (int i = 0; i < cardRects.Count; i++)
                if (cardRects[i].height > 0)
                    validRects.Add(cardRects[i]);

            if (validRects.Count == 0) return;

            // Calculate distance from mouse to this folder's cards area
            float areaTop = validRects[0].y;
            float areaBottom = validRects[validRects.Count - 1].yMax;
            float distToArea = mouseY < areaTop ? areaTop - mouseY
                             : mouseY > areaBottom ? mouseY - areaBottom
                             : 0;

            // Determine insert index from card rect midpoints
            int insertIndex = children.Count;
            for (int i = 0; i < cardRects.Count; i++)
            {
                if (cardRects[i].height <= 0) continue;
                float midY = cardRects[i].center.y;
                if (mouseY < midY) { insertIndex = i; break; }
            }

            // Track best match: the folder closest to the mouse wins
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (string.IsNullOrEmpty(_dropParentId) || distToArea < _bestDropDistance)
                {
                    _dropIndex = insertIndex;
                    _dropParentId = parentId;
                    _bestDropDistance = distToArea;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                }
                Repaint();
            }

            // Draw overlay indicator at winning folder
            if (_dropIndex >= 0 && _dropParentId == parentId && Event.current.type == EventType.Repaint)
            {
                Rect indicatorRect;
                if (_dropIndex < cardRects.Count)
                {
                    var r = cardRects[_dropIndex];
                    indicatorRect = new Rect(r.x, r.y - 2, r.width, 4);
                }
                else
                {
                    var r = cardRects[cardRects.Count - 1];
                    indicatorRect = new Rect(r.x, r.yMax, r.width, 4);
                }
                EditorGUI.DrawRect(indicatorRect, new Color(0.3f, 0.7f, 1f, 0.8f));
            }
        }

        private void DrawFloatingCard()
        {
            if (string.IsNullOrEmpty(_dragNodeId)) return;

            // Find the dragged node
            CenterTreeNode dragNode = null;
            foreach (var (_, node) in _centerNodeIndex)
                if (node.NodeId == _dragNodeId) { dragNode = node; break; }
            if (dragNode == null) return;

            var mousePos = Event.current.mousePosition;
            float cardWidth = 280f;
            float cardHeight = EditorGUIUtility.singleLineHeight + EditorTokens.Pad * 2;

            var floatingRect = new Rect(mousePos.x + 10, mousePos.y - cardHeight / 2, cardWidth, cardHeight);

            // Semi-transparent background
            var oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.85f);
            GUI.Box(floatingRect, "", EditorStyles.helpBox);
            GUI.color = oldColor;

            // Content
            var contentRect = new Rect(floatingRect.x + EditorTokens.Pad, floatingRect.y + EditorTokens.Pad,
                cardWidth - EditorTokens.Pad * 2, cardHeight - EditorTokens.Pad * 2);

            GUI.Label(new Rect(contentRect.x, contentRect.y, cardWidth * 0.7f, contentRect.height),
                dragNode.NodeId, FloatingNameStyle);

            GUI.Label(new Rect(contentRect.x + cardWidth * 0.7f, contentRect.y, cardWidth * 0.3f - EditorTokens.Pad * 2, contentRect.height),
                dragNode.Def != null ? dragNode.Def.Type.ToString() : "-", FloatingTypeStyle);
        }

        private void CleanupDrag()
        {
            _dragNodeId = null;
            _dragParentId = null;
            _dropIndex = -1;
            _dropParentId = null;
            _bestDropDistance = float.MaxValue;
            _folderDropIndex = -1;
        }

        private bool IsDraggingFolder()
        {
            if (string.IsNullOrEmpty(_dragNodeId)) return false;
            foreach (var n in _ownNodes)
                if (n.NodeId == _dragNodeId && string.IsNullOrEmpty(n.DefId)) return true;
            return false;
        }

        private void HandleFolderReorder(List<CenterTreeNode> roots, List<Rect> folderRects)
        {
            if (!IsDraggingFolder()) return;
            if (folderRects.Count == 0) return;

            var evt = Event.current;
            var mouseY = evt.mousePosition.y;

            // Filter valid rects
            var validRects = new List<Rect>();
            for (int i = 0; i < folderRects.Count; i++)
                if (folderRects[i].height > 0)
                    validRects.Add(folderRects[i]);
            if (validRects.Count == 0) return;

            float areaTop = validRects[0].y - EditorTokens.Pad;
            if (mouseY < areaTop) return;

            // Calculate insert index from rect midpoints
            int insertIndex = roots.Count;
            for (int i = 0; i < folderRects.Count; i++)
            {
                if (folderRects[i].height <= 0) continue;
                if (mouseY < folderRects[i].center.y) { insertIndex = i; break; }
            }

            if (evt.type == EventType.DragUpdated)
            {
                _folderDropIndex = insertIndex;
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                Repaint();
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                ReorderFolder(_dragNodeId, insertIndex);
                CleanupDrag();
                Repaint();
                evt.Use();
            }

            // Draw overlay indicator (only use valid rects)
            if (_folderDropIndex >= 0 && Event.current.type == EventType.Repaint && validRects.Count > 0)
            {
                Rect indicatorRect;
                int validIdx = 0;
                for (int i = 0; i < _folderDropIndex && i < folderRects.Count; i++)
                    if (folderRects[i].height > 0) validIdx++;
                if (validIdx < validRects.Count)
                {
                    var r = validRects[validIdx];
                    indicatorRect = new Rect(r.x, r.y - 2, r.width, 4);
                }
                else
                {
                    var r = validRects[validRects.Count - 1];
                    indicatorRect = new Rect(r.x, r.yMax, r.width, 4);
                }
                EditorGUI.DrawRect(indicatorRect, new Color(0.3f, 0.7f, 1f, 0.8f));
            }
        }

        private void ReorderFolder(string nodeId, int targetIndex)
        {
            var node = _ownNodes.FirstOrDefault(n => n.NodeId == nodeId && string.IsNullOrEmpty(n.DefId));
            if (node == null) return;

            Undo.RecordObject(_tree, "Reorder Folder");
            _ownNodes.Remove(node);

            // Map targetIndex (all root folders) to local-only position (inherited first)
            int inheritedBefore = 0;
            foreach (var root in _centerTreeRoots)
                if (root.IsFolder && !root.IsLocal) inheritedBefore++;
            int localTarget = targetIndex - inheritedBefore;
            if (localTarget < 0) localTarget = 0;

            // Find target position among LOCAL root folders
            int insertAt = _ownNodes.Count;
            int rootCount = 0;
            for (int i = 0; i < _ownNodes.Count; i++)
            {
                if (string.IsNullOrEmpty(_ownNodes[i].ParentId) && string.IsNullOrEmpty(_ownNodes[i].DefId))
                {
                    if (rootCount == localTarget) { insertAt = i; break; }
                    rootCount++;
                }
            }

            _ownNodes.Insert(insertAt, node);
            _tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = _ownNodes }, true);
            EditorUtility.SetDirty(_tree);
            _hasChanges = true; RefreshAfterEdit();
        }

        private string FindParentId(string nodeId)
        {
            foreach (var n in _ownNodes)
                if (n.NodeId == nodeId && !string.IsNullOrEmpty(n.DefId))
                    return n.ParentId;
            return "";
        }

        private void ReorderLeaf(string nodeId, string targetParentId, int insertIndex)
        {
            // Find the node
            var node = _ownNodes.FirstOrDefault(n => n.NodeId == nodeId && !string.IsNullOrEmpty(n.DefId));
            if (node == null) return;

            Undo.RecordObject(_tree, "Reorder Property");

            // Remove from current position
            _ownNodes.Remove(node);

            // Find all siblings in target parent and their positions
            var siblings = new List<int>(); // indices in _ownNodes
            for (int i = 0; i < _ownNodes.Count; i++)
            {
                var n = _ownNodes[i];
                if (n.ParentId == targetParentId && !string.IsNullOrEmpty(n.DefId))
                    siblings.Add(i);
            }

            // Determine insert position in _ownNodes
            int targetListIndex;
            // Map insertIndex (all children) to local-only position (inherited come first)
            int inheritedBefore = 0;
            if (_centerNodeIndex.TryGetValue(targetParentId, out var parentNode))
                foreach (var child in parentNode.Children)
                    if (!child.IsLocal) inheritedBefore++;
            int localInsert = insertIndex - inheritedBefore;
            if (localInsert < 0) localInsert = 0;
            if (localInsert > siblings.Count) localInsert = siblings.Count;

            if (localInsert >= siblings.Count)
            {
                // Insert after last sibling
                targetListIndex = siblings.Count > 0 ? siblings[siblings.Count - 1] + 1 : _ownNodes.Count;
            }
            else
            {
                targetListIndex = siblings[localInsert];
            }

            // Update parent if changed
            node.ParentId = targetParentId;

            // Insert at target position
            _ownNodes.Insert(targetListIndex, node);

            // Save to treeJson so RefreshAfterEdit doesn't overwrite the reorder
            _tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = _ownNodes }, true);
            EditorUtility.SetDirty(_tree);

            _hasChanges = true;
            RefreshAfterEdit();
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
            // Search
            DrawRightSearch();
            Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);

            // Add button
            Shared.EditorUI.EditorCard.Draw(() =>
            {
                if (GUILayout.Button("+ Add", GUILayout.Height(22)))
                {
                    PropertyTreeEditorPopups.CreateDefDialog.Show(def =>
                    {
                        RefreshDefPool();
                        RefreshUsedDefs();
                    });
                }
            });
            Shared.EditorUI.EditorCard.Gap(EditorTokens.Pad);

            // Def pool card
            Shared.EditorUI.EditorCard.Draw(() =>
            {
                EditorGUILayout.LabelField("Property Pool", EditorStyles.boldLabel);
                GUILayout.Space(EditorTokens.Pad);

                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

                // Filter and sort
                var filtered = new List<PropertyDefSO>();
                foreach (var d in _allDefs)
                {
                    if (string.IsNullOrEmpty(_rightSearch)
                        || d.Id.ToLowerInvariant().Contains(_rightSearch.ToLowerInvariant())
                        || d.Type.ToString().ToLowerInvariant().Contains(_rightSearch.ToLowerInvariant()))
                        filtered.Add(d);
                }
                filtered.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

                foreach (var def in filtered)
                {
                    bool used = _usedDefIds.Contains(def.Id);
                    DrawRightDefRow(def, used);
                }

                if (filtered.Count == 0)
                    EditorGUILayout.LabelField("No definitions found.", EditorStyles.centeredGreyMiniLabel);

                EditorGUILayout.EndScrollView();

                // Drop zone for removing properties from tree
                HandleRightPanelDrop();
            });
        }

        private void DrawRightSearch()
        {
            Shared.EditorUI.EditorCard.Draw(() =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search", SearchToolbarLabel, GUILayout.Width(45), GUILayout.Height(22));
                EditorGUI.BeginChangeCheck();
                _rightSearch = EditorGUILayout.TextField(_rightSearch, GUILayout.ExpandWidth(true), GUILayout.Height(22));
                if (EditorGUI.EndChangeCheck()) RefreshDefPool();
                if (!string.IsNullOrEmpty(_rightSearch) && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                { _rightSearch = ""; RefreshDefPool(); GUI.FocusControl(null); }
                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawRightDefRow(PropertyDefSO def, bool isUsed)
        {
            var rowH = EditorGUIUtility.singleLineHeight;

            var oldBg = GUI.backgroundColor;
            if (isUsed) GUI.backgroundColor = new Color(0.2f, 0.3f, 0.2f, 0.5f);

            Shared.EditorUI.EditorCard.Draw(() =>
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(rowH));

                // Name with type in tooltip: "(Float) description"
                var tooltipRight = string.IsNullOrEmpty(def.Description)
                    ? $"({def.Type})"
                    : $"({def.Type}) {def.Description}";
                var nameContent = new GUIContent(def.Id, tooltipRight);
                var nameStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = isUsed ? Color.gray : Color.white }
                };
                GUILayout.Label(nameContent, nameStyle, GUILayout.ExpandWidth(true), GUILayout.Height(rowH));

                // Detail button "?"
                var oldInfoBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
                if (GUILayout.Button("?", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(rowH)))
                    PropertyTreeEditorPopups.DefDetailPopup.Show(def);
                GUI.backgroundColor = oldInfoBg;

                // Delete
                var oldDelBg = GUI.backgroundColor;
                GUI.backgroundColor = ColorDelete;
                if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(rowH)))
                {
                    if (isUsed)
                    {
                        EditorUtility.DisplayDialog("Cannot Delete",
                            $"'{def.Id}' is used in the current tree. Remove it from the tree first.", "OK");
                    }
                    else if (EditorUtility.DisplayDialog("Delete Definition",
                        $"Delete '{def.Id}'?\nThis will delete the asset file permanently.", "Delete", "Cancel"))
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(def));
                        AssetDatabase.SaveAssets();
                        RefreshDefPool();
                        RefreshUsedDefs();
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = oldDelBg;

                EditorGUILayout.EndHorizontal();
            });

            GUI.backgroundColor = oldBg;

            // Drag initiation — start drag with this PropertyDefSO
            var rowRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDrag && rowRect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new UnityEngine.Object[] { def };
                DragAndDrop.StartDrag("DefDrag");
                Event.current.Use();
            }
        }

        private void HandleRightPanelDrop()
        {
            var evt = Event.current;
            if (string.IsNullOrEmpty(_dragNodeId)) return;

            // Use a generous drop zone covering the right panel area
            var dropRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight * 3, GUILayout.ExpandWidth(true));

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                // Draw drop indicator
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(dropRect, new Color(0.9f, 0.3f, 0.3f, 0.4f));

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    DeleteLeaf(_dragNodeId);
                    CleanupDrag();
                }
                Repaint();
                evt.Use();
            }
        }

        // ============================================================
        //  Data
        // ============================================================
        private void SelectTree(PropertyTreeSO tree)
        {
            _tree = tree;
            _hasChanges = false;
            _searchFilter = "";
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
            var allNodes = _tree.ResolveAllNodes(out var ancestorConflicts);

            // Cache inherited NodeIds for name-conflict checks (AddFolder, TryRenameFolder, etc.)
            _inheritedNodeIds.Clear();
            foreach (var (nodeId, _) in allNodes)
                if (!_localIds.Contains(nodeId) || ancestorConflicts.Contains(nodeId))
                    _inheritedNodeIds.Add(nodeId);

            // Create CenterTreeNodes for all merged nodes.
            // IsLocal = true ONLY when the node genuinely originates from this tree,
            // i.e. it is NOT shadowed by an inherited ancestor with the same NodeId.
            foreach (var (nodeId, node) in allNodes)
            {
                bool isLocal = _localIds.Contains(nodeId) && !ancestorConflicts.Contains(nodeId);

                if (ancestorConflicts.Contains(nodeId) && _warnedConflicts.Add(nodeId))
                {
                    Debug.LogWarning($"[PropertyTreeEditor] NodeId conflict: '{nodeId}' was shadowed by an inherited ancestor. " +
                        "The ancestor's version is shown (non-editable). To override it, rename the local node to a unique NodeId.");
                }

                var displayNode = new CenterTreeNode
                {
                    NodeId = nodeId,
                    Def = string.IsNullOrEmpty(node.DefId)
                        ? null : PropertyDefinitionRegistry.FindById(node.DefId),
                    IsLocal = isLocal,
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

        private void SortTreeNodes(List<CenterTreeNode> nodes)
        {
            nodes.Sort((a, b) =>
            {
                // Folders always first
                if (a.IsFolder != b.IsFolder) return a.IsFolder ? -1 : 1;

                // Inherited always before local (uses IsLocal, not _ownNodes.FindIndex,
                // because FindIndex can hit a conflicting local node with the same NodeId)
                if (a.IsLocal != b.IsLocal) return a.IsLocal ? 1 : -1;

                // Both inherited → alpha
                if (!a.IsLocal) return string.CompareOrdinal(a.NodeId, b.NodeId);

                // Both local → follow _ownNodes order
                int ia = _ownNodes.FindIndex(n => n.NodeId == a.NodeId);
                int ib = _ownNodes.FindIndex(n => n.NodeId == b.NodeId);
                if (ia < 0 && ib < 0) return string.CompareOrdinal(a.NodeId, b.NodeId);
                if (ia >= 0 && ib >= 0) return ia.CompareTo(ib);
                return ia < 0 ? -1 : 1;
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

        private void AddDefToFolder(string parentNodeId, PropertyDefSO def, int insertIndex = -1)
        {
            Undo.RecordObject(_tree, "Add Property");

            // Auto-suffix if NodeId conflicts with inherited or existing local nodes
            var inheritedIds = GetInheritedNodeIds();
            string nodeId = def.Id;
            int suffix = 1;
            while (inheritedIds.Contains(nodeId) || _ownNodes.Any(n => n.NodeId == nodeId))
                nodeId = $"{def.Id} {++suffix}";

            var node = new PropertyNode { NodeId = nodeId, ParentId = parentNodeId, DefId = def.Id };

            if (insertIndex < 0)
            {
                _ownNodes.Add(node);
            }
            else
            {
                // Count local siblings first
                int siblingCount = 0;
                for (int i = 0; i < _ownNodes.Count; i++)
                    if (_ownNodes[i].ParentId == parentNodeId && !string.IsNullOrEmpty(_ownNodes[i].DefId))
                        siblingCount++;

                // Clamp to local-only range (inherited children come after locals)
                int localInsert = insertIndex;
                if (localInsert > siblingCount) localInsert = siblingCount;

                // Find insert position
                int targetIndex = _ownNodes.Count;
                int count = 0;
                for (int i = 0; i < _ownNodes.Count; i++)
                {
                    if (_ownNodes[i].ParentId == parentNodeId && !string.IsNullOrEmpty(_ownNodes[i].DefId))
                    {
                        if (count == localInsert) { targetIndex = i; break; }
                        count++;
                    }
                }
                _ownNodes.Insert(targetIndex, node);
            }

            _tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = _ownNodes }, true);
            EditorUtility.SetDirty(_tree);
            _hasChanges = true; RefreshAfterEdit();
        }

        private void TryRenameFolder(string oldNodeId, string newNodeId)
        {
            if (string.IsNullOrWhiteSpace(newNodeId))
            {
                EditorUtility.DisplayDialog("Invalid Name", "Folder name cannot be empty.", "OK");
                return;
            }
            if (newNodeId == oldNodeId) return;
            if (newNodeId.Contains("/"))
            {
                EditorUtility.DisplayDialog("Invalid Name", "Folder name cannot contain '/'.", "OK");
                return;
            }
            // Check duplicate — own nodes AND inherited
            var inheritedIds = GetInheritedNodeIds();
            if (inheritedIds.Contains(newNodeId))
            {
                EditorUtility.DisplayDialog("Duplicate Name",
                    $"'{newNodeId}' already exists in an inherited Tree. Choose a different name.", "OK");
                return;
            }
            foreach (var n in _ownNodes)
                if (n.NodeId == newNodeId && string.IsNullOrEmpty(n.DefId))
                {
                    EditorUtility.DisplayDialog("Duplicate Name", $"A folder named '{newNodeId}' already exists.", "OK");
                    return;
                }
            RenameFolder(oldNodeId, newNodeId);
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
            _tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = _ownNodes }, true);
            EditorUtility.SetDirty(_tree);
            _hasChanges = true; RefreshAfterEdit();
        }

        private void DeleteLeaf(string leafPath)
        {
            var nodeId = DisplayName(leafPath);
            _ownNodes.RemoveAll(n => n.NodeId == nodeId && !string.IsNullOrEmpty(n.DefId));
            _tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = _ownNodes }, true);
            EditorUtility.SetDirty(_tree);
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
            _tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = _ownNodes }, true);
            EditorUtility.SetDirty(_tree);
            _hasChanges = true; RefreshAfterEdit();
        }

        /// <summary>Get all NodeIds from the inherited (non-local) part of the merged tree.
        /// Uses the cache populated by BuildCenterTree to avoid redundant merge.</summary>
        private HashSet<string> GetInheritedNodeIds()
        {
            return _inheritedNodeIds;
        }

        private void AddFolder(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains("/")) return;

            // Auto-rename if duplicate — check both own AND inherited nodes
            var usedNames = GetInheritedNodeIds();
            foreach (var n in _ownNodes) usedNames.Add(n.NodeId);
            string finalName = name;
            int suffix = 1;
            while (usedNames.Contains(finalName))
                finalName = $"{name} {++suffix}";

            Undo.RecordObject(_tree, "Add Folder");
            // Insert after last root-level node
            int insertAt = _ownNodes.Count;
            for (int i = _ownNodes.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(_ownNodes[i].ParentId))
                { insertAt = i + 1; break; }
            }
            _ownNodes.Insert(insertAt, new PropertyNode { NodeId = finalName, ParentId = "", DefId = "" });
            _tree.treeJson = JsonUtility.ToJson(new PropertyTreeContainer { Nodes = _ownNodes }, true);
            EditorUtility.SetDirty(_tree);
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
        //  Popups — now in PropertyTreeEditorPopups.cs
        // ============================================================
    }
}
