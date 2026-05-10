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

            if (tree.InheritsFrom != null)
            {
                EditorGUILayout.LabelField("── Inherited ──", EditorStyles.miniLabel);
                foreach (var node in tree.InheritsFrom.Children ?? new StatsNodeSO[0])
                    DrawNode(node, 0, readOnly: true);
            }

            if (tree.Children is { Length: > 0 })
            {
                if (tree.InheritsFrom != null)
                    EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("── My Overrides ──", EditorStyles.miniLabel);
                foreach (var node in tree.Children)
                    DrawNode(node, 0, readOnly: false);
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

        private void DrawNode(StatsNodeSO node, int depth, bool readOnly, string parentPrefix = "")
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(depth * 16);

            var displayEnabled = readOnly
                ? (pendingEnabled.TryGetValue(node, out var pe) ? pe : node.IsEnabled)
                : node.IsEnabled;
            var toggle = EditorGUILayout.Toggle(displayEnabled, GUILayout.Width(16));
            if (toggle != displayEnabled)
            {
                if (readOnly)
                    pendingEnabled[node] = toggle;
                else
                    node.IsEnabled = toggle;
                hasChanges = true;
            }

            string displayId;
            if (readOnly)
                displayId = node.Id;
            else
            {
                var newId = EditorGUILayout.TextField(node.Id, GUILayout.Width(100));
                if (newId != node.Id)
                {
                    node.Id = newId;
                    node.name = string.IsNullOrEmpty(parentPrefix) ? newId : $"{parentPrefix}_{newId}";
                    EditorUtility.SetDirty(node);
                    hasChanges = true;
                }
                displayId = node.Id;
            }

            if (node.IsFolder)
            {
                var hasChildren = node.Children is { Length: > 0 };
                if (hasChildren)
                {
                    if (!foldouts.ContainsKey(node)) foldouts[node] = true;
                    foldouts[node] = EditorGUILayout.Foldout(foldouts[node], "", true);
                }
                else GUILayout.Space(16);

                EditorGUILayout.LabelField(displayId, GUILayout.Width(100));
                var kidCount = node.Children?.Length ?? 0;
                EditorGUILayout.LabelField($"({kidCount} children)", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Space(16);
                if (readOnly)
                    EditorGUILayout.LabelField(node.Def != null ? node.Def.name : "(null)", GUILayout.Width(180));
                else
                {
                    var newDef = (StatDefSO)EditorGUILayout.ObjectField(node.Def, typeof(StatDefSO), false,
                        GUILayout.Width(180));
                    if (newDef != node.Def) { node.Def = newDef; hasChanges = true; }
                }

                var curVal = pendingOverride.TryGetValue(node, out var po) ? po : node.OverrideValue;
                var newVal = EditorGUILayout.FloatField(curVal, GUILayout.Width(50));
                if (Mathf.Abs(newVal - curVal) > 0.001f)
                {
                    if (readOnly)
                        pendingOverride[node] = newVal;
                    else
                        node.OverrideValue = newVal;
                    hasChanges = true;
                }
            }

            if (node.IsFolder && GUILayout.Button("+", GUILayout.Width(20)))
            {
                var prefix = string.IsNullOrEmpty(parentPrefix) ? node.Id : $"{parentPrefix}_{node.Id}";
                AddChildToNode(node, CreateNode("Child", false, prefix));
            }
            if (!readOnly && GUILayout.Button("x", GUILayout.Width(20)))
                RemoveNode(node);

            EditorGUILayout.EndHorizontal();

            if (node.IsFolder && node.Children != null && foldouts.TryGetValue(node, out var expanded) && expanded)
            {
                var childPrefix = string.IsNullOrEmpty(parentPrefix) ? node.Id : $"{parentPrefix}_{node.Id}";
                foreach (var child in node.Children)
                    DrawNode(child, depth + 1, readOnly, childPrefix);
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
            if (!EditorUtility.DisplayDialog("Remove", $"Remove {target.Id}?", "Yes", "No")) return;
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
