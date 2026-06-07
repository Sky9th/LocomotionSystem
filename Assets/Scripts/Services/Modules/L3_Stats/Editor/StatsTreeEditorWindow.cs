using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RedDust.Stats.Editor
{
    public partial class StatsTreeEditorWindow : EditorWindow
    {
        // -- data --
        private StatsTreeSO tree;
        private List<JsonStatNode> ownNodes = new();       // this tree's raw nodes
        private List<JsonStatNode> workingNodes = new();   // merged display list
        private List<JsonStatNode> parentMerged = new();   // cached: all ancestors merged
        private bool hasChanges;
        private bool needsRebuild;
        private bool hasCycle;
        private Vector2 scroll;
        private int myDepth;
        private readonly Dictionary<string, bool> foldouts = new();
        private readonly List<JsonStatNode> pendingDeletions = new();

        [MenuItem("RedDust/Stats Tree Editor")]
        private static void Open()
            => GetWindow<StatsTreeEditorWindow>("Stats Tree (JSON)");

        private void OnGUI()
        {
            if (needsRebuild)
            {
                RebuildMergedView();
                needsRebuild = false;
            }

            if (hasCycle)
            {
                EditorGUILayout.HelpBox(
                    "Circular inheritance detected! Check InheritsFrom chain.",
                    MessageType.Error);
            }

            const float pad = 6f;

            GUILayout.Space(pad); // top — window edge ↔ first card

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(pad); // left margin

            EditorGUILayout.BeginVertical();
            DrawHeader();

            GUILayout.Space(pad);
            var treeName = tree != null ? $"[{myDepth}] 📁 {tree.name}" : "No Tree Selected";
            GUILayout.Label(treeName, EditorStyles.largeLabel);

            DrawToolbar();
            GUILayout.Space(pad);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawBody();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            GUILayout.Space(pad); // right margin
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(pad); // bottom — last card ↔ window edge

            // process deferred deletions (outside layout)
            if (pendingDeletions.Count > 0)
            {
                foreach (var target in pendingDeletions)
                    DeleteNode(target);
                pendingDeletions.Clear();
            }
        }

        private void DrawHeader()
        {
            const float pad = 6f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(pad); // left padding

            // -- left block: fills remaining width after the 100px save button --
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            var newTree = (StatsTreeSO)EditorGUILayout.ObjectField(
                "Tree", tree, typeof(StatsTreeSO), false,
                GUILayout.ExpandWidth(true));
            if (newTree != tree) { tree = newTree; LoadTree(); }

            GUILayout.Space(2f);

            var prevInherits = tree != null ? tree.InheritsFrom : null;
            var newInherits = (StatsTreeSO)EditorGUILayout.ObjectField(
                "Inherits From", prevInherits, typeof(StatsTreeSO), false,
                GUILayout.ExpandWidth(true));
            if (tree != null && newInherits != prevInherits)
            {
                if (newInherits != null && WouldCreateCycle(tree, newInherits))
                {
                    EditorUtility.DisplayDialog("Circular Inheritance",
                        $"Cannot set '{newInherits.name}' — it would create a circular reference.",
                        "OK");
                }
                else
                {
                    tree.InheritsFrom = newInherits;
                    EditorUtility.SetDirty(tree);
                    BuildParentMerged();
                    RebuildMergedView();
                    hasChanges = true;
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(pad); // margin between left and right

            // -- right block: Save button, fixed 100px, right-aligned naturally --
            var dirty = hasChanges;
            GUI.enabled = dirty;
            GUI.backgroundColor = dirty
                ? new Color(0.4f, 0.8f, 0.4f)
                : Color.white;

            if (GUILayout.Button(dirty ? "💾 Save" : "Saved", GUILayout.Width(100)))
            {
                Save();
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            GUILayout.Space(pad); // right padding
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(pad);
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            const float pad = 6f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(pad); // left padding

            // -- left block --
            if (GUILayout.Button("＋ Folder", GUILayout.Width(100), GUILayout.Height(24)))
            {
                AddRootFolder();
            }

            GUILayout.FlexibleSpace();

            // -- right block --
            if (GUILayout.Button("▼ All", GUILayout.Height(24)))
            {
                ExpandAll();
            }

            if (GUILayout.Button("▲ All", GUILayout.Height(24)))
            {
                CollapseAll();
            }

            GUILayout.Space(pad); // right padding
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(pad);
            EditorGUILayout.EndVertical();
        }

        private void LoadTree()
        {
            foldouts.Clear();
            hasCycle = false;

            if (tree == null) return;

            // deserialize own nodes
            ownNodes.Clear();
            if (!string.IsNullOrEmpty(tree.treeJson))
            {
                var own = JsonUtility.FromJson<TreeDataContainer>(tree.treeJson);
                if (own?.Nodes != null) ownNodes = own.Nodes;
            }

            // resolve DefRef for own nodes
            foreach (var node in ownNodes)
            {
                if (node.Def >= 0 && node.Def < tree.defRefs.Count)
                    node.DefRef = tree.defRefs[node.Def];
            }

            BuildParentMerged();
            RebuildMergedView();
            hasChanges = false;
            Repaint();
        }

        /// <summary>
        /// Build the cached parent-merged list once (all ancestors merged).
        /// Called from LoadTree; after that, only ownNodes change.
        /// </summary>
        private void BuildParentMerged()
        {
            hasCycle = false;
            var inherited = new List<(TreeDataContainer, StatsTreeSO, int)>();
            if (tree != null)
                CollectInheritedNodes(tree.InheritsFrom, inherited);
            myDepth = inherited.Count;

            parentMerged.Clear();
            foreach (var (container, _, depth) in inherited)
                MergeLayer(container.Nodes, parentMerged, depth);

            RefreshPaths(parentMerged);
        }

        /// <summary>
        /// Rebuild workingNodes from cached parentMerged + ownNodes.
        /// Called after every edit to ownNodes.
        /// </summary>
        private void RebuildMergedView()
        {
            workingNodes.Clear();

            // clone parent nodes so edits don't pollute the cache
            foreach (var node in parentMerged)
                workingNodes.Add(CloneNode(node));

            // overlay own nodes
            MergeLayer(ownNodes, workingNodes, myDepth);

            RefreshPaths(workingNodes);
        }

        private static JsonStatNode CloneNode(JsonStatNode src)
        {
            return new JsonStatNode
            {
                Id = src.Id,
                IsEnabled = src.IsEnabled,
                IsFolder = src.IsFolder,
                IsOverride = src.IsOverride,
                ParentId = src.ParentId,
                Def = src.Def,
                OverrideValue = src.OverrideValue,
                DefRef = src.DefRef,
                DuplicateId = src.DuplicateId,
                Depth = src.Depth,
                Path = src.Path,
            };
        }

        /// <summary>
        /// Merge one layer into target by Id. Matching Id → override,
        /// no match → append. Inherited nodes without ParentId inherit
        /// ParentId from the matched target node.
        /// </summary>
        private static void MergeLayer(
            List<JsonStatNode> source,
            List<JsonStatNode> target,
            int depth)
        {
            foreach (var node in source)
            {
                node.Depth = depth;
                var existingIdx = target.FindIndex(n => n.Id == node.Id);
                if (existingIdx >= 0)
                {
                    node.IsOverride = true;
                    if (string.IsNullOrEmpty(node.ParentId))
                        node.ParentId = target[existingIdx].ParentId;
                    target[existingIdx] = node;
                }
                else
                {
                    target.Add(node);
                }
            }
        }

        /// <summary>
        /// Find all direct children of a folder by ParentId.
        /// </summary>
        private static List<JsonStatNode> GetChildren(
            string parentId, List<JsonStatNode> nodes)
        {
            var result = new List<JsonStatNode>();
            foreach (var n in nodes)
            {
                if (n.ParentId == parentId)
                    result.Add(n);
            }
            return result;
        }

        /// <summary>
        /// Rebuild Path for all nodes from roots downward.
        /// Root = ParentId is null or empty.
        /// </summary>
        private static void RefreshPaths(List<JsonStatNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (string.IsNullOrEmpty(n.ParentId))
                    BuildPathRecursive(n, nodes, "");
            }
        }

        private static void BuildPathRecursive(
            JsonStatNode node, List<JsonStatNode> allNodes, string parentPath,
            int depth = 0)
        {
            if (depth > 1000)
                throw new System.StackOverflowException(
                    "Tree path too deep (>1000). Circular ParentId?");

            node.Path = string.IsNullOrEmpty(parentPath)
                ? node.Id
                : $"{parentPath}/{node.Id}";

            if (node.IsFolder)
            {
                foreach (var child in GetChildren(node.Id, allNodes))
                    BuildPathRecursive(child, allNodes, node.Path, depth + 1);
            }
        }

        /// <summary>
        /// Recursively collect inherited nodes top-down (root first).
        /// Depth = result.Count after root recursion = natural depth order.
        /// </summary>
        private static bool WouldCreateCycle(StatsTreeSO node, StatsTreeSO proposedParent)
        {
            var visited = new HashSet<StatsTreeSO>();
            var current = proposedParent;
            while (current != null)
            {
                if (current == node) return true;
                if (!visited.Add(current)) return true;
                current = current.InheritsFrom;
            }
            return false;
        }

        private void CollectInheritedNodes(
            StatsTreeSO current,
            List<(TreeDataContainer container, StatsTreeSO source, int depth)> result,
            HashSet<StatsTreeSO> visited = null)
        {
            if (current == null) return;
            if (visited == null) visited = new HashSet<StatsTreeSO>();
            if (!visited.Add(current))
            {
                hasCycle = true;
                return;
            }

            // recurse to root first
            CollectInheritedNodes(current.InheritsFrom, result, visited);

            // process this level — ancestors already added, so Count = my depth
            if (!string.IsNullOrEmpty(current.treeJson))
            {
                var container = JsonUtility.FromJson<TreeDataContainer>(current.treeJson);
                if (container?.Nodes is { Count: > 0 })
                {
                    var depth = result.Count;
                    foreach (var node in container.Nodes)
                    {
                        if (node.Def >= 0 && node.Def < current.defRefs.Count)
                            node.DefRef = current.defRefs[node.Def];
                    }

                    result.Add((container, current, depth));
                }
            }
        }

        /// <summary>
        /// Returns the set of Ids that appear more than once in workingNodes.
        /// </summary>
        private HashSet<string> GetDuplicateIds()
        {
            var seen = new HashSet<string>();
            var dupes = new HashSet<string>();
            foreach (var node in workingNodes)
            {
                if (!seen.Add(node.Id))
                    dupes.Add(node.Id);
            }
            return dupes;
        }

        private void Save()
        {
            if (tree == null) return;

            // prevent save if any leaf is missing a Def
            var missingDef = ownNodes.FindAll(
                n => !n.IsFolder && n.DefRef == null);
            if (missingDef.Count > 0)
            {
                EditorUtility.DisplayDialog("Missing Def",
                    $"Cannot save. {missingDef.Count} leaf node(s) have no StatDefinition assigned:\n"
                    + string.Join("\n", missingDef.ConvertAll(n => n.Id)),
                    "OK");
                return;
            }

            // sync Def indices for own nodes
            foreach (var node in ownNodes)
            {
                if (node.IsFolder || node.DefRef == null)
                {
                    node.Def = -1;
                    continue;
                }

                var idx = tree.defRefs.IndexOf(node.DefRef);
                if (idx < 0)
                {
                    tree.defRefs.Add(node.DefRef);
                    idx = tree.defRefs.Count - 1;
                }
                node.Def = idx;
            }

            var json = JsonUtility.ToJson(new TreeDataContainer { Nodes = ownNodes }, true);
            tree.treeJson = json;

            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
            hasChanges = false;
            Repaint();
        }

        private void AddRootFolder()
        {
            var id = GenerateUniqueId("NewFolder");
            ownNodes.Add(new JsonStatNode
            {
                Id = id,
                IsEnabled = true,
                IsFolder = true,
                IsOverride = false,
            });
            needsRebuild = true;
            hasChanges = true;
            Repaint();
        }

        private void ExpandAll()
        {
            foreach (var key in new List<string>(foldouts.Keys))
                foldouts[key] = true;
            Repaint();
        }

        private void DeleteNode(JsonStatNode target, int depth = 0)
        {
            if (depth > 1000)
                throw new System.StackOverflowException(
                    "Delete cascade too deep (>1000). Circular ParentId?");

            // cascade-delete children (nodes whose ParentId points to target)
            var children = GetChildren(target.Id, ownNodes);
            foreach (var child in children)
                DeleteNode(child, depth + 1);

            // also cascade from workingNodes (for inherited children)
            children = GetChildren(target.Id, workingNodes);
            foreach (var child in children)
            {
                if (ownNodes.Exists(n => n.Id == child.Id)) continue;
                workingNodes.Remove(child);
                foldouts.Remove(child.Id);
            }

            ownNodes.Remove(target);
            workingNodes.Remove(target);
            foldouts.Remove(target.Id);

            needsRebuild = true;
            hasChanges = true;
            Repaint();
        }

        private void AddChildToFolder(JsonStatNode parent)
        {
            var id = GenerateUniqueId("NewLeaf");
            ownNodes.Add(new JsonStatNode
            {
                Id = id,
                IsEnabled = true,
                IsFolder = false,
                IsOverride = false,
                ParentId = parent.Id,
            });

            needsRebuild = true;
            hasChanges = true;
            Repaint();
        }

        private void CollapseAll()
        {
            foreach (var key in new List<string>(foldouts.Keys))
                foldouts[key] = false;
            Repaint();
        }

        /// <summary>
        /// Find or create a writable copy of a node in ownNodes.
        /// Inherited nodes are cloned as IsOverride=true on first edit.
        /// </summary>
        private JsonStatNode GetOrCreateOwn(JsonStatNode displayNode)
        {
            var own = ownNodes.Find(n => n.Id == displayNode.Id);
            if (own != null) return own;

            // inherited node being edited for the first time: create override
            own = CloneNode(displayNode);
            own.IsOverride = true;
            own.Depth = myDepth;
            ownNodes.Add(own);
            return own;
        }

        private string GenerateUniqueId(string baseId)
        {
            // check against full merged set (inherited + own) to avoid overriding
            if (!workingNodes.Exists(n => n.Id == baseId))
                return baseId;

            var i = 1;
            while (workingNodes.Exists(n => n.Id == $"{baseId}_{i}"))
                i++;
            return $"{baseId}_{i}";
        }

        private void DrawBody()
        {
            const float pad = 6f;
            var dupes = GetDuplicateIds();

            // -- Tree card --
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(pad); // top

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(pad); // left

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            for (var i = 0; i < workingNodes.Count; i++)
            {
                if (!workingNodes[i].IsFolder) continue;
                if (i > 0) GUILayout.Space(2f);
                DrawFolderCard(workingNodes[i]);
            }

            if (workingNodes.Count == 0)
            {
                GUILayout.Space(pad);
                GUILayout.Label("No nodes. Click ＋ Folder to add one.",
                    EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(pad);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(pad); // right
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(pad); // bottom
            EditorGUILayout.EndVertical();

            // -- local helpers --

            void DrawFolderCard(JsonStatNode node)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Space(pad); // top

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(pad); // left

                // -- left: foldout, fixed 35px --
                if (!foldouts.ContainsKey(node.Id))
                    foldouts[node.Id] = true;
                var foldRect = GUILayoutUtility.GetRect(
                    35f, EditorGUIUtility.singleLineHeight);
                foldouts[node.Id] = EditorGUI.Foldout(
                    foldRect, foldouts[node.Id], "", true);
                GUILayout.Space(4f); // textPad

                // -- right block: toolbar + leaf list --
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                // toolbar row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"[{node.Depth}]", EditorStyles.miniLabel,
                    GUILayout.Width(20));
                // folder name — editable only for own nodes
                var isOwn = node.Depth >= myDepth;
                if (isOwn)
                {
                    var newName = EditorGUILayout.TextField(node.Id, GUILayout.Width(110));
                    if (newName != node.Id)
                    {
                        var oldId = node.Id;
                        // update ownNodes entry
                        var own = ownNodes.Find(n => n.Id == oldId);
                        if (own != null) own.Id = newName;
                        // update children's ParentId in ownNodes
                        foreach (var n in ownNodes)
                            if (n.ParentId == oldId) n.ParentId = newName;
                        needsRebuild = true;
                        hasChanges = true;
                    }
                }
                else
                {
                    GUILayout.Label(node.Id, EditorStyles.label,
                        GUILayout.Width(110));
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("＋", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    AddChildToFolder(node);
                }
                GUILayout.Space(2f);
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                GUI.enabled = node.Depth >= myDepth && !node.IsOverride;
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    pendingDeletions.Add(node);
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                // leaf list card (only when expanded and has children)
                var leafChildren = GetChildren(node.Id, workingNodes);
                var expanded = foldouts.TryGetValue(node.Id, out var f) && f;
                if (expanded && leafChildren.Count > 0)
                {
                    GUILayout.Space(2f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(pad); // top

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(pad); // left

                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                    for (var ci = 0; ci < leafChildren.Count; ci++)
                    {
                        if (ci > 0) GUILayout.Space(2f);
                        DrawLeafCard(leafChildren[ci]);
                    }

                    EditorGUILayout.EndVertical();

                    GUILayout.Space(pad); // right
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(pad); // bottom
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();

                // -- local helpers --

                void DrawLeafCard(JsonStatNode leaf)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(pad); // top

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(pad); // left

                    // IsEnabled
                    var rowH = EditorGUIUtility.singleLineHeight;
                    var enabled = EditorGUILayout.Toggle(
                        "", leaf.IsEnabled, GUILayout.Width(14f),
                        GUILayout.Height(rowH));
                    if (enabled != leaf.IsEnabled)
                    {
                        GetOrCreateOwn(leaf).IsEnabled = enabled;
                        needsRebuild = true;
                        hasChanges = true;
                    }
                    GUILayout.Space(pad);

                    // foldout placeholder (for alignment with folder)
                    GUILayout.Space(14f);
                    GUILayout.Space(4f); // textPad

                    // Depth + Id label
                    var isDupe = dupes.Contains(leaf.Id);
                    var isMissingDef = leaf.DefRef == null;
                    GUILayout.Label($"[{leaf.Depth}]", EditorStyles.miniLabel,
                        GUILayout.Width(20), GUILayout.Height(rowH));
                    var defId = !string.IsNullOrEmpty(leaf.DuplicateId)
                        ? leaf.DuplicateId
                        : leaf.DefRef != null ? leaf.DefRef.Id : "—";
                    GUILayout.Label(defId, EditorStyles.label,
                        GUILayout.Width(80), GUILayout.Height(rowH));

                    // Def field — red if duplicate Id, missing Def, or has DuplicateId
                    // Override nodes: Def is read-only (inherited from ancestor)
                    var leafIsOwn = leaf.Depth >= myDepth;
                    GUI.enabled = leafIsOwn && !leaf.IsOverride;
                    if (isDupe || isMissingDef || !string.IsNullOrEmpty(leaf.DuplicateId))
                        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    var newDef = (StatDefinitionSO)EditorGUILayout.ObjectField(
                        leaf.DefRef, typeof(StatDefinitionSO), false,
                        GUILayout.ExpandWidth(true),
                        GUILayout.Height(rowH));
                    GUI.backgroundColor = Color.white;
                    GUI.enabled = true;
                    if (newDef != leaf.DefRef)
                    {
                        var own = GetOrCreateOwn(leaf);
                        own.DefRef = newDef;
                        if (newDef != null)
                        {
                            var idTaken = workingNodes.Exists(
                                n => n.Id == newDef.Id && n != leaf);
                            if (idTaken)
                                own.DuplicateId = newDef.Id; // store intended Id
                            else
                            {
                                own.Id = newDef.Id;
                                leaf.Id = newDef.Id;
                                own.DuplicateId = null;
                            }
                        }
                        else
                        {
                            own.DuplicateId = null;
                        }
                        needsRebuild = true;
                        hasChanges = true;
                    }

                    GUILayout.FlexibleSpace();

                    // Val field
                    var hasOverride = leaf.OverrideValue != float.MinValue;
                    var isOwnOverride = leafIsOwn && hasOverride;
                    var defVal = leaf.DefRef != null ? leaf.DefRef.Default : 0f;
                    var displayVal = hasOverride ? leaf.OverrideValue : defVal;

                    GUILayout.Label("Val",
                        isOwnOverride ? EditorStyles.boldLabel : EditorStyles.miniLabel,
                        GUILayout.Height(rowH));

                    var floatRect = EditorGUILayout.GetControlRect(
                        GUILayout.Width(50), GUILayout.Height(rowH));
                    var numStyle = new GUIStyle(EditorStyles.numberField)
                        { alignment = TextAnchor.MiddleRight };
                    if (isOwnOverride) numStyle.fontStyle = FontStyle.Bold;
                    var newVal = EditorGUI.FloatField(floatRect, displayVal, numStyle);
                    if (Mathf.Abs(newVal - displayVal) > 0.001f)
                    {
                        GetOrCreateOwn(leaf).OverrideValue = newVal;
                        needsRebuild = true;
                        hasChanges = true;
                    }

                    // Clear override — only for own overrides
                    GUI.enabled = isOwnOverride;
                    if (GUILayout.Button("↺", EditorStyles.miniButton,
                        GUILayout.Width(20), GUILayout.Height(rowH)))
                    {
                        var own = ownNodes.Find(n => n.Id == leaf.Id);
                        if (own != null) { ownNodes.Remove(own); needsRebuild = true; }
                        hasChanges = true;
                    }
                    GUI.enabled = true;

                    // Delete — only for non-override own nodes
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    GUI.enabled = leafIsOwn && !leaf.IsOverride;
                    if (GUILayout.Button("✕", EditorStyles.miniButton,
                        GUILayout.Width(22), GUILayout.Height(rowH)))
                    {
                        pendingDeletions.Add(leaf);
                    }
                    GUI.enabled = true;
                    GUI.backgroundColor = Color.white;

                    GUILayout.Space(pad); // right
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(pad); // bottom
                    EditorGUILayout.EndVertical();
                }

                GUILayout.Space(pad); // right
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(pad); // bottom
                EditorGUILayout.EndVertical();
            }
        }
    }
}
