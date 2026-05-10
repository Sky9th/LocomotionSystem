using System.Collections.Generic;
using UnityEngine;

namespace Game.Stats
{
    [CreateAssetMenu(fileName = "StatsTree", menuName = "Game/Stats/Stats Tree")]
    public class StatsTreeSO : ScriptableObject
    {
        public StatsTreeSO InheritsFrom;
        public StatsNodeSO[] Children;

        public IReadOnlyList<ResolvedStat> Resolve()
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

        private static void MergeNodes(StatsNodeSO[] nodes, List<StatsNodeSO> list)
        {
            foreach (var node in nodes)
            {
                if (node == null) continue;
                var existing = list.FindIndex(n => n.Id == node.Id);
                if (existing >= 0)
                    list[existing] = node;
                else
                    list.Add(node);
                if (node.IsFolder && node.Children != null)
                    MergeNodes(node.Children, list);
            }
        }

        private static IReadOnlyList<ResolvedStat> ExtractLeaves(List<StatsNodeSO> nodes)
        {
            var leaves = new List<ResolvedStat>();
            foreach (var node in nodes)
            {
                if (!node.IsEnabled || node.IsFolder) continue;
                if (node.Def == null) continue;

                var rs = new ResolvedStat { Def = node.Def };
                rs.OverrideDefault = node.OverrideValue;
                rs.EffectiveBehaviors = node.CustomBehaviors is { Length: > 0 }
                    ? node.CustomBehaviors : node.Def.Behaviors;
                leaves.Add(rs);
            }
            return leaves;
        }
    }
}
