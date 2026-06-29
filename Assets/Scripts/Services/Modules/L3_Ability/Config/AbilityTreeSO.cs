using System;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 技能树节点——树内的一个可解锁单元。
    /// 可携带主动技能（ActiveAbilitySO）或被动效果（PassiveAbilitySO），或两者兼有。
    /// prerequisites 为空 = 根节点（初始即可解锁）。
    /// </summary>
    [Serializable]
    public struct SAbilityTreeNode
    {
        /// <summary>节点标识，树内唯一。——"ironBones_1", "elbowStrike"</summary>
        [Tooltip("节点标识，同一棵树内唯一。")]
        public string nodeId;

        /// <summary>主动技能。可选——纯被动节点填 null。</summary>
        [Tooltip("主动技能（Q/E/R/F）。可选。")]
        public ActiveAbilitySO ability;

        /// <summary>被动效果。可选——纯主动节点填 null。</summary>
        [Tooltip("被动效果。可选。")]
        public PassiveAbilitySO passive;

        /// <summary>前置节点 ID。空数组 = 根节点（初始即可解锁）。</summary>
        [Tooltip("前置节点 ID。空 = 根节点。")]
        public string[] prerequisites;
    }

    /// <summary>
    /// 技能/天赋/套路树——纯数据资产。
    ///
    /// 一切皆 AbilityTree：天生技能、天赋筛选、武学套路、丧尸变异——底层同构。
    /// 树内技能通过 SAbilityTreeNode 逐节点解锁，不是一次性全部获得。
    ///
    /// 类别通过 treeTags（rTag）区分，不用 Enum：
    ///   AbilityTree.Innate  — 天生（出生全解锁，不可移除）
    ///   AbilityTree.Talent  — 天赋（创建时选择，逐节点解锁，exclusiveGroup 互斥）
    ///   AbilityTree.Routine — 套路（装备切换，和武器求交，逐节点解锁）
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Ability Tree", fileName = "AbilityTree_")]
    public class AbilityTreeSO : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>树唯一标识。——"ironBones", "bajiQuan"</summary>
        [Tooltip("树唯一标识。")]
        public string treeId;

        /// <summary>显示名称。</summary>
        [Tooltip("显示名称。")]
        public string displayName;

        [TextArea(2, 4)]
        [Tooltip("描述文本。")]
        public string description;

        /// <summary>图标。</summary>
        [Tooltip("图标。")]
        public Sprite icon;

        [Header("Classification")]
        /// <summary>
        /// 类别标签——AbilityTree.Innate / Talent / Routine。
        /// 多选适用：例如一个丧尸 Boss 树可同时有 Innate 和 Boss。
        /// </summary>
        [Tooltip("类别标签。AbilityTree.Innate / Talent / Routine。")]
        public rTagDefSO[] treeTags;

        /// <summary>
        /// 武器兼容标签。树内主动技能只对匹配的武器生效。
        /// 空数组 = 不限武器（纯被动树/徒手树）。
        /// Talent 类别的树通常为空——被动不受武器限制。
        /// </summary>
        [Tooltip("武器兼容标签。空 = 不限武器。")]
        public rTagDefSO[] compatibleWeaponTags;

        [Header("Mutual Exclusion")]
        /// <summary>
        /// 互斥分组。同组只能选一个。
        /// 例如天生体质组：IronBones / AgileGenes / EnduranceBoost → exclusiveGroup = "innate_body"。
        /// 空字符串 = 不参与互斥。
        /// </summary>
        [Tooltip("互斥分组。同组只能选一个。空 = 无互斥。")]
        public string exclusiveGroup;

        [Header("Nodes")]
        /// <summary>技能树的所有节点。根节点 prerequisites 为空。</summary>
        [Tooltip("技能树的所有节点。")]
        public SAbilityTreeNode[] nodes;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // treeId 必填
            if (string.IsNullOrEmpty(treeId))
            {
                Debug.LogWarning($"[AbilityTreeSO] {name}: treeId is empty.", this);
            }

            // 节点 ID 唯一性校验
            if (nodes is not { Length: > 0 }) return;

            // nodeId 非空 + 唯一性
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (string.IsNullOrEmpty(nodes[i].nodeId))
                {
                    Debug.LogWarning($"[AbilityTreeSO] {name}: Nodes[{i}] nodeId is empty.", this);
                    continue;
                }
                if (!seen.Add(nodes[i].nodeId))
                {
                    Debug.LogError($"[AbilityTreeSO] {name}: Duplicate nodeId '{nodes[i].nodeId}' at index {i}.", this);
                }
            }

            // 前置节点有效性校验
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].prerequisites is not { Length: > 0 }) continue;
                foreach (var prereq in nodes[i].prerequisites)
                {
                    if (!seen.Contains(prereq))
                    {
                        Debug.LogWarning($"[AbilityTreeSO] {name}: Node '{nodes[i].nodeId}' references unknown prerequisite '{prereq}'.", this);
                    }
                }
            }

            // 自环检测
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].prerequisites != null && System.Array.IndexOf(nodes[i].prerequisites, nodes[i].nodeId) >= 0)
                {
                    Debug.LogError($"[AbilityTreeSO] {name}: Node '{nodes[i].nodeId}' has itself as prerequisite.", this);
                }
            }

            // 循环依赖检测（仅多节点时检测——单节点自环已被上面捕获）
            if (nodes.Length > 1)
            {
                var cycle = FindCycle(nodes);
                if (cycle != null)
                {
                    Debug.LogError($"[AbilityTreeSO] {name}: Cycle detected in prerequisites: {string.Join(" → ", cycle)}.", this);
                }
            }
        }

        private static string[] FindCycle(SAbilityTreeNode[] nodes)
        {
            var visited = new System.Collections.Generic.HashSet<string>();
            var stack = new System.Collections.Generic.HashSet<string>();
            var path = new System.Collections.Generic.List<string>();

            foreach (var node in nodes)
            {
                if (node.prerequisites is not { Length: > 0 }) continue;
                if (DfsDetectCycle(node.nodeId, nodes, visited, stack, path))
                    return path.ToArray();
            }
            return null;
        }

        private static bool DfsDetectCycle(string nodeId, SAbilityTreeNode[] nodes,
            System.Collections.Generic.HashSet<string> visited,
            System.Collections.Generic.HashSet<string> stack,
            System.Collections.Generic.List<string> path)
        {
            if (stack.Contains(nodeId))
            {
                // 截断路径，只保留从重复节点开始的环
                int cycleStart = path.IndexOf(nodeId);
                if (cycleStart >= 0) path.RemoveRange(0, cycleStart);
                path.Add(nodeId);
                return true;
            }
            if (visited.Contains(nodeId)) return false;

            visited.Add(nodeId);
            stack.Add(nodeId);
            path.Add(nodeId);

            var node = System.Array.Find(nodes, n => n.nodeId == nodeId);
            if (node.prerequisites != null)
            {
                foreach (var prereq in node.prerequisites)
                {
                    if (DfsDetectCycle(prereq, nodes, visited, stack, path))
                        return true;
                }
            }

            stack.Remove(nodeId);
            path.RemoveAt(path.Count - 1);
            return false;
        }
#endif
    }
}
