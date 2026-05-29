using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Stats
{
    [CreateAssetMenu(fileName = "StatsTree", menuName = "RedDust/Stats/Stats Tree")]
    public class StatsTreeSO : ScriptableObject
    {
        public StatsTreeSO InheritsFrom;
        public StatsNodeSO[] Children;

        public IReadOnlyList<StatInstance> Resolve()
        {
            var nodes = CollectNodes();
            return ExtractLeaves(nodes);
        }

        private List<StatsNodeSO> CollectNodes()
        {
            var list = new List<StatsNodeSO>();
            if (InheritsFrom != null)
                CollectFrom(InheritsFrom, list);
            if (Children != null)
                MergeNodes(Children, list);
            return list;
        }

        private static void CollectFrom(StatsTreeSO tree, List<StatsNodeSO> list)
        {
            if (tree == null) return;
            if (tree.InheritsFrom != null)
                CollectFrom(tree.InheritsFrom, list);
            if (tree.Children != null)
                MergeNodes(tree.Children, list);
        }

        private static void MergeNodes(StatsNodeSO[] nodes, List<StatsNodeSO> list, string parentPath = "")
        {
            foreach (var node in nodes)
            {
                if (node == null) continue;
                node.Path = string.IsNullOrEmpty(parentPath) ? node.Id : $"{parentPath}/{node.Id}";
                var existing = list.FindIndex(n => n.Path == node.Path);
                if (existing >= 0)
                    list[existing] = node;
                else
                    list.Add(node);
                if (node.IsFolder && node.Children != null)
                    MergeNodes(node.Children, list, node.Path);
            }
        }

        private static IReadOnlyList<StatInstance> ExtractLeaves(List<StatsNodeSO> nodes)
        {
            var instances = new List<StatInstance>();
            foreach (var node in nodes)
            {
                if (!node.IsEnabled || node.IsFolder) continue;
                if (node.Def == null) continue;

                var instance = new StatInstance(node.Def, node.OverrideValue)
                {
                    Path = node.Path
                };
                instances.Add(instance);
            }
            return instances;
        }
    }
}
