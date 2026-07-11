using RedDust.Core.GameService;
using RedDust.Core.RdTag;
using System.Collections.Generic;
using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    internal class ActiveTree
    {
        public AbilityTreeSO Tree;
        public HashSet<string> UnlockedNodeIds;
        public object Source;
    }

    /// <summary>
    /// 技能森林——角色持有的所有活跃 AbilityTree 运行时集合。
    /// 纯 C# 类，无 MonoBehaviour。
    /// </summary>
    internal class AbilityForest
    {
        private readonly List<ActiveTree> _activeTrees = new();
        private RdTagContainer _weaponTags;

        /// <summary>最近一次解析产出的主动技能列表。</summary>
        public ActiveAbilitySO[] ResolvedActives { get; private set; } = System.Array.Empty<ActiveAbilitySO>();

        /// <summary>最近一次解析产出的被动技能列表。</summary>
        public PassiveAbilitySO[] ResolvedPassives { get; private set; } = System.Array.Empty<PassiveAbilitySO>();

        /// <summary>
        /// 创建技能森林并注入天生技能树。ids 为 treeId 字符串数组，经 Assets 解析。
        /// null / 空数组 = 无天生树（后续通过 SetInnateTrees 添加）。
        /// </summary>
        public AbilityForest(string[] innateTreeIds)
        {
            AddInnateTrees(innateTreeIds);
        }

        /// <summary>设置天生技能树 ID。用于 Awake 时 Entity 尚未绑定的延迟注入。</summary>
        public void SetInnateTrees(string[] treeIds)
        {
            RemoveBySource("innate");
            AddInnateTrees(treeIds);
        }

        private void AddInnateTrees(string[] treeIds)
        {
            if (treeIds == null || treeIds.Length == 0) return;
            var trees = GameService.Instance.Assets.ResolveAbilityTrees(treeIds);
            if (trees.Length == 0) return;
            foreach (var t in trees)
                AddTreeInternal(t, "innate");
            Resolve();
        }

        // ── 武器 ──────────────────────────────────────────

        public void SetWeaponTags(RdTagContainer weaponTags)
        {
            _weaponTags = weaponTags;
            Resolve();
        }

        // ── 树管理 ──────────────────────────────────────────

        public void AddTrees(AbilityTreeSO[] trees, object source)
        {
            if (trees == null) return;
            foreach (var tree in trees)
                AddTreeInternal(tree, source);
            Resolve();
        }

        public void AddTree(AbilityTreeSO tree, object source)
        {
            if (tree == null) return;
            AddTreeInternal(tree, source);
            Resolve();
        }

        public void AddTree(AbilityTreeSO tree, HashSet<string> initialUnlocks, object source)
        {
            if (tree == null) return;

            var valid = new HashSet<string>();
            if (tree.nodes != null && initialUnlocks != null)
            {
                foreach (var node in tree.nodes)
                    if (node.nodeId != null && initialUnlocks.Contains(node.nodeId))
                        valid.Add(node.nodeId);
            }

            _activeTrees.Add(new ActiveTree { Tree = tree, UnlockedNodeIds = valid, Source = source });
            Resolve();
        }

        public void RemoveBySource(object source)
        {
            _activeTrees.RemoveAll(t => t.Source == source);
            Resolve();
            Debug.Log($"[AbilityForest] Removed source '{source}' — {ResolveSummary()}");
        }

        // ── 节点管理 ──────────────────────────────────────────

        public void UnlockNode(string treeId, string nodeId)
        {
            if (string.IsNullOrEmpty(treeId) || string.IsNullOrEmpty(nodeId)) return;

            foreach (var at in _activeTrees)
            {
                if (at.Tree.treeId != treeId) continue;
                if (at.Tree.nodes == null) return;
                foreach (var node in at.Tree.nodes)
                {
                    if (node.nodeId == nodeId)
                    {
                        at.UnlockedNodeIds.Add(nodeId);
                        Resolve();
                        Debug.Log($"[AbilityForest] Unlocked node '{nodeId}' in '{treeId}' — {ResolveSummary()}");
                        return;
                    }
                }
                return;
            }
        }

        public bool IsNodeUnlocked(string treeId, string nodeId)
        {
            if (string.IsNullOrEmpty(treeId) || string.IsNullOrEmpty(nodeId)) return false;
            foreach (var at in _activeTrees)
            {
                if (at.Tree.treeId == treeId)
                    return at.UnlockedNodeIds.Contains(nodeId);
            }
            return false;
        }

        // ── 技能解析 ──────────────────────────────────────────

        private void Resolve()
        {
            var actives = new List<ActiveAbilitySO>();
            var passives = new List<PassiveAbilitySO>();

            foreach (var at in _activeTrees)
            {
                var tree = at.Tree;
                if (tree == null || tree.nodes == null) continue;

                if (!IsWeaponCompatible(tree.compatibleWeaponTags, _weaponTags)) continue;
                if (!IsGripCompatible(tree.compatibleGripTags, _weaponTags)) continue;

                foreach (var node in tree.nodes)
                {
                    if (string.IsNullOrEmpty(node.nodeId)) continue;
                    if (!at.UnlockedNodeIds.Contains(node.nodeId)) continue;
                    if (node.ability != null) actives.Add(node.ability);
                    if (node.passive != null) passives.Add(node.passive);
                }
            }

            ResolvedActives = actives.ToArray();
            ResolvedPassives = passives.ToArray();
        }

        // ── 内部 ──────────────────────────────────────────

        private void AddTreeInternal(AbilityTreeSO tree, object source)
        {
            if (tree == null) return;
            var allNodeIds = new HashSet<string>();
            if (tree.nodes != null)
                foreach (var node in tree.nodes)
                    if (!string.IsNullOrEmpty(node.nodeId))
                        allNodeIds.Add(node.nodeId);
            _activeTrees.Add(new ActiveTree { Tree = tree, UnlockedNodeIds = allNodeIds, Source = source });
        }

        private static bool IsWeaponCompatible(RdTagDefSO[] compatibleTags, RdTagContainer weaponTags)
        {
            if (compatibleTags == null || compatibleTags.Length == 0) return true;
            if (weaponTags == null) return false;
            foreach (var tag in compatibleTags)
                if (tag != null && weaponTags.HasTag(tag.FullTag))
                    return true;
            return false;
        }

        private static bool IsGripCompatible(RdTagDefSO[] compatibleGripTags, RdTagContainer equipmentTags)
        {
            if (compatibleGripTags == null || compatibleGripTags.Length == 0) return true;
            if (equipmentTags == null) return false;
            foreach (var tag in compatibleGripTags)
                if (tag != null && equipmentTags.HasTag(tag.FullTag))
                    return true;
            return false;
        }

        // ── 日志 ──────────────────────────────────────────

        private string ResolveSummary()
        {
            var activeNames = new List<string>();
            foreach (var a in ResolvedActives)
                if (a != null) activeNames.Add(a.internalName);
            var passiveNames = new List<string>();
            foreach (var p in ResolvedPassives)
                if (p != null) passiveNames.Add(p.internalName);
            return $"Actives({ResolvedActives.Length}): [{string.Join(", ", activeNames)}] | Passives({ResolvedPassives.Length}): [{string.Join(", ", passiveNames)}]";
        }
    }
}
