using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RedDust.Stats.Editor
{
    public partial class StatsTreeEditorWindow : EditorWindow
    {
        // -- data --
        private StatsTreeData tree;
        private List<JsonStatNode> ownNodes = new();       // this tree's raw nodes
        private List<JsonStatNode> workingNodes = new();   // merged display list
        private bool hasChanges;
        private Vector2 scroll;
        private int myDepth;
        private readonly Dictionary<string, bool> foldouts = new();
        private readonly List<JsonStatNode> pendingDeletions = new();

        [MenuItem("Window/Stats Tree Editor (JSON)")]
        private static void Open()
            => GetWindow<StatsTreeEditorWindow>("Stats Tree (JSON)");

        private void OnGUI()
        {
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

            var newTree = (StatsTreeData)EditorGUILayout.ObjectField(
                "Tree", tree, typeof(StatsTreeData), false,
                GUILayout.ExpandWidth(true));
            if (newTree != tree) { tree = newTree; LoadTree(); }

            GUILayout.Space(2f);

            var prevInherits = tree != null ? tree.InheritsFrom : null;
            var newInherits = (StatsTreeData)EditorGUILayout.ObjectField(
                "Inherits From", prevInherits, typeof(StatsTreeData), false,
                GUILayout.ExpandWidth(true));
            if (tree != null && newInherits != prevInherits)
            {
                tree.InheritsFrom = newInherits;
                EditorUtility.SetDirty(tree);
                RebuildWorkingNodes();
                hasChanges = true; // InheritsFrom change
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

            if (tree == null) return;

            // collect inherited nodes (top-down: root ancestor first)
            var inherited = new List<(TreeDataContainer, StatsTreeData, int)>();
            CollectInheritedNodes(tree.InheritsFrom, inherited);

            myDepth = inherited.Count; // how many ancestors above me

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

            RebuildWorkingNodes();
            hasChanges = false;
            Repaint();
        }

        private void RebuildWorkingNodes()
        {
            var inherited = new List<(TreeDataContainer, StatsTreeData, int)>();
            if (tree != null)
                CollectInheritedNodes(tree.InheritsFrom, inherited);
            myDepth = inherited.Count;

            // simple append for display — TODO: proper merge
            workingNodes.Clear();
            foreach (var (container, _, depth) in inherited)
            {
                foreach (var node in container.Nodes)
                    node.Depth = depth;
                workingNodes.AddRange(container.Nodes);
            }
            foreach (var node in ownNodes)
            {
                node.Depth = myDepth;
                workingNodes.Add(node);
            }
        }

        /// <summary>
        /// Recursively collect inherited nodes top-down (root first).
        /// Depth = result.Count after root recursion = natural depth order.
        /// </summary>
        private void CollectInheritedNodes(
            StatsTreeData current,
            List<(TreeDataContainer container, StatsTreeData source, int depth)> result)
        {
            if (current == null) return;

            // recurse to root first
            CollectInheritedNodes(current.InheritsFrom, result);

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

        private void Save()
        {
            if (tree == null) return;

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
            RebuildWorkingNodes();
            hasChanges = true;
            Repaint();
        }

        private void ExpandAll()
        {
            foreach (var key in new List<string>(foldouts.Keys))
                foldouts[key] = true;
            Repaint();
        }

        private void DeleteNode(JsonStatNode target)
        {
            // recursively delete children first (from ownNodes)
            if (target.Children != null)
            {
                foreach (var childId in target.Children)
                {
                    var child = ownNodes.Find(n => n.Id == childId)
                               ?? workingNodes.Find(n => n.Id == childId);
                    if (child != null) DeleteNode(child);
                }
            }

            // remove from any other node's Children array (in ownNodes)
            foreach (var node in ownNodes)
            {
                if (node.Children == null) continue;
                var list = new List<string>(node.Children);
                list.Remove(target.Id);
                node.Children = list.ToArray();
            }

            ownNodes.Remove(target);
            workingNodes.Remove(target);
            foldouts.Remove(target.Id);

            RebuildWorkingNodes();
            hasChanges = true;
            Repaint();
        }

        private void AddChildToFolder(JsonStatNode parent)
        {
            var id = GenerateUniqueId("NewLeaf");
            var child = new JsonStatNode
            {
                Id = id,
                IsEnabled = true,
                IsFolder = false,
                IsOverride = false,
            };
            ownNodes.Add(child);

            var list = parent.Children != null
                ? new List<string>(parent.Children)
                : new List<string>();
            list.Add(id);
            parent.Children = list.ToArray();

            RebuildWorkingNodes();
            hasChanges = true;
            Repaint();
        }

        private void CollapseAll()
        {
            foreach (var key in new List<string>(foldouts.Keys))
                foldouts[key] = false;
            Repaint();
        }

        private string GenerateUniqueId(string baseId)
        {
            if (!ownNodes.Exists(n => n.Id == baseId))
                return baseId;

            var i = 1;
            while (ownNodes.Exists(n => n.Id == $"{baseId}_{i}"))
                i++;
            return $"{baseId}_{i}";
        }

        private void DrawBody()
        {
            const float pad = 6f;

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
                var newName = EditorGUILayout.TextField(node.Id, GUILayout.Width(110));
                if (newName != node.Id) { node.Id = newName; hasChanges = true; }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("＋", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    AddChildToFolder(node);
                }
                GUILayout.Space(2f);
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    pendingDeletions.Add(node);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                // leaf list card (only when expanded and has children)
                var hasChildren = node.Children is { Length: > 0 };
                var expanded = foldouts.TryGetValue(node.Id, out var f) && f;
                if (expanded && hasChildren)
                {
                    GUILayout.Space(2f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(pad); // top

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(pad); // left

                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                    if (node.Children != null)
                    {
                        for (var ci = 0; ci < node.Children.Length; ci++)
                        {
                            var child = workingNodes.Find(
                                n => n.Id == node.Children[ci]);
                            if (child == null) continue;
                            if (ci > 0) GUILayout.Space(2f);
                            DrawLeafCard(child);
                        }
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
                        leaf.IsEnabled = enabled;
                        hasChanges = true;
                    }
                    GUILayout.Space(pad);

                    // foldout placeholder (for alignment with folder)
                    GUILayout.Space(14f);
                    GUILayout.Space(4f); // textPad

                    // Depth + Id label
                    GUILayout.Label($"[{leaf.Depth}]", EditorStyles.miniLabel,
                        GUILayout.Width(20), GUILayout.Height(rowH));
                    var defId = leaf.DefRef != null ? leaf.DefRef.Id : "—";
                    GUILayout.Label(defId, EditorStyles.label,
                        GUILayout.Width(80), GUILayout.Height(rowH));

                    // Def field
                    var newDef = (StatDefinitionSO)EditorGUILayout.ObjectField(
                        leaf.DefRef, typeof(StatDefinitionSO), false,
                        GUILayout.ExpandWidth(true),
                        GUILayout.Height(rowH));
                    if (newDef != leaf.DefRef)
                    {
                        leaf.DefRef = newDef;
                        hasChanges = true;
                    }

                    GUILayout.FlexibleSpace();

                    // Val field
                    var hasOverride = leaf.OverrideValue != float.MinValue;
                    // TODO: default should come from merged inheritance chain,
                    // not just leaf.DefRef.Default — ancestor overrides affect it.
                    var defVal = leaf.DefRef != null ? leaf.DefRef.Default : 0f;
                    var displayVal = hasOverride ? leaf.OverrideValue : defVal;

                    GUILayout.Label("Val",
                        hasOverride ? EditorStyles.boldLabel : EditorStyles.miniLabel,
                        GUILayout.Height(rowH));

                    var floatRect = EditorGUILayout.GetControlRect(
                        GUILayout.Width(50), GUILayout.Height(rowH));
                    var numStyle = new GUIStyle(EditorStyles.numberField)
                        { alignment = TextAnchor.MiddleRight };
                    if (hasOverride) numStyle.fontStyle = FontStyle.Bold;
                    var newVal = EditorGUI.FloatField(floatRect, displayVal, numStyle);
                    if (Mathf.Abs(newVal - displayVal) > 0.001f)
                    {
                        leaf.OverrideValue = newVal;
                        hasChanges = true;
                    }

                    // Clear override
                    GUI.enabled = hasOverride;
                    if (GUILayout.Button("↺", EditorStyles.miniButton,
                        GUILayout.Width(20), GUILayout.Height(rowH)))
                    {
                        leaf.OverrideValue = float.MinValue;
                        hasChanges = true;
                        Repaint();
                    }
                    GUI.enabled = true;

                    // Delete
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("✕", EditorStyles.miniButton,
                        GUILayout.Width(22), GUILayout.Height(rowH)))
                    {
                        pendingDeletions.Add(leaf);
                    }
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
