using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Game.Stats.Editor
{
    public class StatsTreeWindow : EditorWindow
    {
        private StatsTreeSO tree;
        private Vector2 scroll;
        private bool showResolved;
        private bool hasChanges;
        private readonly Dictionary<StatsNodeSO, bool> pendingEnabled = new();
        private readonly Dictionary<StatsNodeSO, float> pendingOverride = new();

        [MenuItem("Window/Stats Tree Editor")]
        private static void Open() => GetWindow<StatsTreeWindow>("Stats Tree");

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            tree = (StatsTreeSO)EditorGUILayout.ObjectField("Tree", tree, typeof(StatsTreeSO), false);
            if (tree != null)
            {
                GUI.enabled = hasChanges;
                if (GUILayout.Button(hasChanges ? "💾 Save" : "✅ Saved", GUILayout.Width(70)))
                {
                    ApplyPendingOverrides();
                    AssetDatabase.SaveAssets();
                    hasChanges = false;
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            if (tree == null) return;

            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"📁 {tree.name}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var inherit = (StatsTreeSO)EditorGUILayout.ObjectField("Inherits From",
                tree.InheritsFrom, typeof(StatsTreeSO), false);
            if (EditorGUI.EndChangeCheck()) { tree.InheritsFrom = inherit; hasChanges = true; }

            EditorGUILayout.Space(5);

            var inherited = tree.InheritsFrom;
            var merged = MergeTrees(inherited, tree);
            if (merged.Count > 0)
            {
                EditorGUILayout.LabelField("📁 Tree", EditorStyles.boldLabel);
                foreach (var entry in merged)
                    DrawMergedEntry(entry, 0);
            }

            if (GUILayout.Button("+ Add Folder"))
                AddChildToTree(CreateNode("NewFolder", true, ""));
            if (GUILayout.Button("+ Add Leaf"))
                AddChildToTree(CreateNode("NewLeaf", false, ""));

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            showResolved = EditorGUILayout.Foldout(showResolved, "Resolved Flat List", true);
            if (showResolved)
                EditorGUILayout.HelpBox(
                    $"Total: {tree.Resolve().Count} active leaves.\n" +
                    "Use the Debug Log in CharacterActor.Awake for full tree print.",
                    MessageType.Info);
        }

        private Dictionary<StatsNodeSO, bool> foldouts = new();

        private class MergedEntry
        {
            public StatsNodeSO node;
            public bool isOverride;
            public bool isLocalOnly;
            public List<MergedEntry> children = new();
            public string path;
        }

        private List<MergedEntry> MergeTrees(StatsTreeSO inheritedTree, StatsTreeSO localTree)
        {
            var localChildren = new List<StatsNodeSO>(localTree?.Children ?? new StatsNodeSO[0]);
            var inheritedChildren = inheritedTree?.Children ?? new StatsNodeSO[0];
            var result = new List<MergedEntry>();

            foreach (var inh in inheritedChildren)
            {
                if (inh == null) continue;
                var local = FindAndRemove(localChildren, inh.Id);
                var entry = new MergedEntry
                {
                    node = local ?? inh,
                    isOverride = local != null,
                    isLocalOnly = false,
                    path = inh.Id,
                };
                if (inh.IsFolder)
                    entry.children = MergeFolderChildren(inh, local, localChildren);
                result.Add(entry);
            }

            foreach (var local in localChildren)
            {
                if (local == null) continue;
                var entry = new MergedEntry
                {
                    node = local,
                    isOverride = false,
                    isLocalOnly = true,
                    path = local.Id,
                };
                if (local.IsFolder)
                    entry.children = MergeFolderChildren(null, local, localChildren);
                result.Add(entry);
            }

            return result;
        }

        private List<MergedEntry> MergeFolderChildren(StatsNodeSO inheritedFolder, StatsNodeSO localFolder,
            List<StatsNodeSO> rootLocalChildren)
        {
            var localKids = new List<StatsNodeSO>(localFolder?.Children ?? new StatsNodeSO[0]);
            var inhKids = inheritedFolder?.Children ?? new StatsNodeSO[0];
            var folderId = (inheritedFolder ?? localFolder).Id;
            var result = new List<MergedEntry>();

            foreach (var inh in inhKids)
            {
                if (inh == null) continue;
                var local = FindAndRemove(localKids, inh.Id) ?? FindAndRemove(rootLocalChildren, inh.Id);
                var entry = new MergedEntry
                {
                    node = local ?? inh,
                    isOverride = local != null,
                    isLocalOnly = false,
                    path = $"{folderId}/{inh.Id}",
                };
                if (inh.IsFolder)
                    entry.children = MergeFolderChildren(inh, local, rootLocalChildren);
                result.Add(entry);
            }

            foreach (var local in localKids)
            {
                if (local == null) continue;
                result.Add(new MergedEntry
                {
                    node = local,
                    isOverride = false,
                    isLocalOnly = true,
                    path = $"{folderId}/{local.Id}",
                });
            }

            return result;
        }

        private static StatsNodeSO FindAndRemove(List<StatsNodeSO> nodes, string id)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].Id == id)
                {
                    var n = nodes[i];
                    nodes.RemoveAt(i);
                    return n;
                }
            }
            return null;
        }

        private void DrawMergedEntry(MergedEntry entry, int depth)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 16);

            // Toggle
            var node = entry.node;
            var enabled = node.IsEnabled;
            var toggle = EditorGUILayout.Toggle(enabled, GUILayout.Width(16));
            if (toggle != enabled)
            {
                node.IsEnabled = toggle;
                EditorUtility.SetDirty(entry.isOverride || entry.isLocalOnly ? node : tree);
                hasChanges = true;
            }

            // Id
            if (entry.isLocalOnly && !entry.isOverride)
            {
                var newId = EditorGUILayout.TextField(node.Id, GUILayout.Width(100));
                if (newId != node.Id)
                {
                    node.Id = newId;
                    node.name = string.IsNullOrEmpty(entry.path) ? newId : entry.path;
                    EditorUtility.SetDirty(node);
                    hasChanges = true;
                }
            }
            else if (entry.isOverride)
            {
                EditorGUILayout.LabelField(node.Id, EditorStyles.boldLabel, GUILayout.Width(100));
            }
            else
            {
                EditorGUILayout.LabelField(node.Id, GUILayout.Width(100));
            }

            // Folder or Leaf
            if (node.IsFolder)
            {
                DrawFolderToggle(node);
                AddFolderButtons(entry, depth);
            }
            else
            {
                DrawLeafContent(node, entry);
                if ((entry.isOverride || entry.isLocalOnly) && GUILayout.Button("x", GUILayout.Width(20)))
                {
                    if (CanDeleteNode(node))
                        RemoveNode(node);
                }
            }

            EditorGUILayout.EndHorizontal();

            // Expanded children
            if (node.IsFolder && foldouts.TryGetValue(node, out var exp) && exp)
            {
                foreach (var child in entry.children)
                    DrawMergedEntry(child, depth + 1);
            }
        }

        private void DrawFolderToggle(StatsNodeSO node)
        {
            var hasChildren = node.Children is { Length: > 0 };
            if (hasChildren)
            {
                if (!foldouts.ContainsKey(node)) foldouts[node] = true;
                foldouts[node] = EditorGUILayout.Foldout(foldouts[node], "", true);
            }
            else GUILayout.Space(16);

            EditorGUILayout.LabelField(node.Id, GUILayout.Width(100));
            var kidCount = node.Children?.Length ?? 0;
            EditorGUILayout.LabelField($"({kidCount} children)", EditorStyles.miniLabel);
        }

        private void AddFolderButtons(MergedEntry entry, int depth)
        {
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                var target = (entry.isOverride || entry.isLocalOnly)
                    ? entry.node
                    : FindOrCreateLocalFolder(entry.node.Id);
                AddChildToNode(target, CreateNode("Child", false, entry.path));
            }
            if ((entry.isOverride || entry.isLocalOnly) && GUILayout.Button("x", GUILayout.Width(20)))
            {
                if (CanDeleteNode(entry.node))
                    RemoveNode(entry.node);
            }
        }

        private void DrawLeafContent(StatsNodeSO node, MergedEntry entry)
        {
            GUILayout.Space(16);

            if (entry.isOverride || entry.isLocalOnly)
            {
                var newDef = (StatDefSO)EditorGUILayout.ObjectField(node.Def, typeof(StatDefSO), false, GUILayout.Width(180));
                if (newDef != node.Def) { node.Def = newDef; hasChanges = true; }
            }
            else
            {
                EditorGUILayout.LabelField(node.Def != null ? node.Def.name : "(null)", GUILayout.Width(180));
            }

            var curVal = pendingOverride.TryGetValue(node, out var po) ? po : node.OverrideValue;
            var newVal = EditorGUILayout.FloatField(curVal, GUILayout.Width(50));
            if (Mathf.Abs(newVal - curVal) > 0.001f)
            {
                if (entry.isOverride || entry.isLocalOnly)
                {
                    node.OverrideValue = newVal;
                    EditorUtility.SetDirty(node);
                }
                else
                    pendingOverride[node] = newVal;
                hasChanges = true;
            }
        }

        private void DrawFolderChildren(StatsNodeSO displayNode, StatsNodeSO originalNode, int depth,
            bool isLocal, StatsTreeSO inheritedTree, string parentPath)
        {
            var hasChildren = (displayNode.Children is { Length: > 0 }) ||
                (originalNode != displayNode && originalNode.Children is { Length: > 0 });
            if (hasChildren)
            {
                if (!foldouts.ContainsKey(displayNode)) foldouts[displayNode] = true;
                foldouts[displayNode] = EditorGUILayout.Foldout(foldouts[displayNode], "", true);
            }
            else GUILayout.Space(16);

            EditorGUILayout.LabelField(displayNode.Id, GUILayout.Width(100));
            var kidCount = (displayNode.Children?.Length ?? 0) +
                (originalNode != displayNode ? (originalNode.Children?.Length ?? 0) : 0);
            EditorGUILayout.LabelField($"({kidCount} children)", EditorStyles.miniLabel);
        }

        private void DrawLeafContent(StatsNodeSO displayNode, StatsNodeSO originalNode,
            bool isEditable, bool isInherited)
        {
            GUILayout.Space(16);

            if (isEditable)
            {
                var newDef = (StatDefSO)EditorGUILayout.ObjectField(displayNode.Def, typeof(StatDefSO), false, GUILayout.Width(180));
                if (newDef != displayNode.Def) { displayNode.Def = newDef; hasChanges = true; }
            }
            else
            {
                EditorGUILayout.LabelField(displayNode.Def != null ? displayNode.Def.name : "(null)", GUILayout.Width(180));
            }

            var curVal = pendingOverride.TryGetValue(originalNode, out var po) ? po : displayNode.OverrideValue;
            var newVal = EditorGUILayout.FloatField(curVal, GUILayout.Width(50));
            if (Mathf.Abs(newVal - curVal) > 0.001f)
            {
                if (isEditable)
                {
                    displayNode.OverrideValue = newVal;
                    EditorUtility.SetDirty(displayNode);
                }
                else
                    pendingOverride[originalNode] = newVal;
                hasChanges = true;
            }
        }

        private void AddButtonsToMergedNode(StatsNodeSO folderNode, string parentPath, bool isPureLocal, bool isLocalOverride)
        {
            if (!folderNode.IsFolder) return;
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                var prefix = string.IsNullOrEmpty(parentPath) ? folderNode.Id : $"{parentPath}_{folderNode.Id}";
                var target = isPureLocal || isLocalOverride ? folderNode : FindOrCreateLocalFolder(folderNode.Id);
                AddChildToNode(target, CreateNode("Child", false, prefix));
            }
            if ((isPureLocal || isLocalOverride) && GUILayout.Button("x", GUILayout.Width(20)))
            {
                if (CanDeleteNode(folderNode))
                    RemoveNode(folderNode);
            }
        }

        private static StatsNodeSO CreateNode(string id, bool isFolder, string parentPrefix)
        {
            var node = CreateInstance<StatsNodeSO>();
            node.Id = id;
            node.IsFolder = isFolder;
            node.name = string.IsNullOrEmpty(parentPrefix) ? id : $"{parentPrefix}_{id}";
            return node;
        }

        private void AddChildToTree(StatsNodeSO child)
        {
            AssetDatabase.AddObjectToAsset(child, tree);
            var list = new List<StatsNodeSO>(tree.Children ?? new StatsNodeSO[0]) { child };
            tree.Children = list.ToArray();
            EditorUtility.SetDirty(tree);
            hasChanges = true;
        }

        private void AddChildToInheritedFolder(StatsNodeSO inheritedFolder, StatsNodeSO child)
        {
            var local = FindOrCreateLocalFolder(inheritedFolder.Id);
            AddChildToNode(local, child);
        }

        private StatsNodeSO FindOrCreateLocalFolder(string folderId)
        {
            foreach (var node in tree.Children ?? new StatsNodeSO[0])
            {
                if (node != null && node.Id == folderId && node.IsFolder)
                    return node;
            }

            var folder = CreateNode(folderId, true, "");
            AddChildToTree(folder);
            return folder;
        }

        private bool CanDeleteNode(StatsNodeSO target)
        {
            var inheritors = FindInheritingTrees();
            if (inheritors.Count == 0) return true;

            var overriddenBy = new List<string>();
            foreach (var childTree in inheritors)
            {
                if (TreeOverridesNode(childTree, target))
                    overriddenBy.Add(childTree.name);
            }

            if (overriddenBy.Count > 0)
            {
                EditorUtility.DisplayDialog("Cannot Delete",
                    $"'{target.Id}' is overridden by:\n{string.Join("\n", overriddenBy)}\n\nRemove their overrides first.",
                    "OK");
                return false;
            }

            return true;
        }

        private List<StatsTreeSO> FindInheritingTrees()
        {
            var result = new List<StatsTreeSO>();
            var guids = AssetDatabase.FindAssets("t:StatsTreeSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var other = AssetDatabase.LoadAssetAtPath<StatsTreeSO>(path);
                if (other == null || other == tree) continue;
                if (TreeInheritsFrom(other, tree))
                    result.Add(other);
            }
            return result;
        }

        private static bool TreeInheritsFrom(StatsTreeSO candidate, StatsTreeSO ancestor)
        {
            var current = candidate.InheritsFrom;
            while (current != null)
            {
                if (current == ancestor) return true;
                current = current.InheritsFrom;
            }
            return false;
        }

        private static bool TreeOverridesNode(StatsTreeSO childTree, StatsNodeSO target)
        {
            foreach (var node in childTree.Children ?? new StatsNodeSO[0])
            {
                if (node != null && node.Id == target.Id)
                    return true;
            }
            return false;
        }

        private void AddChildToNode(StatsNodeSO parent, StatsNodeSO child)
        {
            AssetDatabase.AddObjectToAsset(child, tree);
            var list = new List<StatsNodeSO>(parent.Children ?? new StatsNodeSO[0]) { child };
            parent.Children = list.ToArray();
            EditorUtility.SetDirty(parent);
            hasChanges = true;
        }

        private void RemoveNode(StatsNodeSO target)
        {
            var hasKids = target.IsFolder && target.Children is { Length: > 0 };
            var msg = hasKids
                ? $"Delete '{target.Id}' and ALL its children? This cannot be undone."
                : $"Remove '{target.Id}'?";
            if (!EditorUtility.DisplayDialog("Remove", msg, "Yes", "No")) return;

            if (target.Children != null)
            {
                for (var i = target.Children.Length - 1; i >= 0; i--)
                    RemoveNode(target.Children[i]);
            }
            RemoveFromParent(target);
            DestroyImmediate(target, true);
            EditorUtility.SetDirty(tree);
            hasChanges = true;
        }

        private void RemoveFromParent(StatsNodeSO target)
        {
            var list = new List<StatsNodeSO>(tree.Children ?? new StatsNodeSO[0]);
            if (list.Remove(target)) { tree.Children = list.ToArray(); return; }
            foreach (var node in tree.Children ?? new StatsNodeSO[0])
                RemoveFromNode(node, target);
        }

        private void ApplyPendingOverrides()
        {
            foreach (var kv in pendingEnabled)
            {
                var clone = Instantiate(kv.Key);
                clone.IsEnabled = kv.Value;
                clone.Children = kv.Key.Children;
                clone.name = kv.Key.name;
                AddChildToTreeInternal(clone);
            }
            foreach (var kv in pendingOverride)
            {
                if (pendingEnabled.ContainsKey(kv.Key)) continue;
                var clone = Instantiate(kv.Key);
                clone.OverrideValue = kv.Value;
                clone.Children = kv.Key.Children;
                clone.name = kv.Key.name;
                AddChildToTreeInternal(clone);
            }
            pendingEnabled.Clear();
            pendingOverride.Clear();
        }

        private void AddChildToTreeInternal(StatsNodeSO child)
        {
            AssetDatabase.AddObjectToAsset(child, tree);
            var list = new List<StatsNodeSO>(tree.Children ?? new StatsNodeSO[0]) { child };
            tree.Children = list.ToArray();
            EditorUtility.SetDirty(tree);
        }

        private void RemoveFromNode(StatsNodeSO parent, StatsNodeSO target)
        {
            if (parent.Children == null) return;
            var list = new List<StatsNodeSO>(parent.Children);
            if (list.Remove(target)) { parent.Children = list.ToArray(); return; }
            foreach (var child in parent.Children)
                RemoveFromNode(child, target);
        }
    }
}
