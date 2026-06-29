using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 活跃树——AbilityTreeSO + 运行时解锁状态 + 来源标识。
    /// 纯数据容器，由 AbilityForest 管理。
    /// </summary>
    internal class ActiveTree
    {
        /// <summary>技能树静态数据。</summary>
        public AbilityTreeSO Tree;

        /// <summary>这棵树内已解锁的 nodeId 集合。</summary>
        public HashSet<string> UnlockedNodeIds;

        /// <summary>
        /// 来源标识——用于 RemoveBySource 精确移除。
        /// 角色天生树 = "innate"，天赋树 = "talent"，武器树 = itemInstance，习得树 = "learned"。
        /// </summary>
        public object Source;
    }

    /// <summary>
    /// 技能森林——角色持有的所有活跃 AbilityTree 运行时集合。
    ///
    /// 树组成森林：AbilityTreeSO 是一棵静态树，AbilityForest 是角色运行时持有的树集合。
    /// 管理多来源活跃树（天生/天赋/武器/习得），追踪节点解锁状态，按武器兼容解析可用技能。
    ///
    /// 纯 C# 类，无 MonoBehaviour。由 CharacterActor 在 Awake 创建并持有，
    /// 解析结果通过 BuildContext 传递给 PlayerDirector 和 AbilityExecutor。
    /// </summary>
    internal class AbilityForest
    {
        private readonly List<ActiveTree> _activeTrees = new();
        private rTagContainer _weaponTags;

        /// <summary>创建技能森林，注入天生树（全解锁，source="innate"）。</summary>
        public AbilityForest(AbilityTreeSO[] innateTrees)
        {
            if (innateTrees == null || innateTrees.Length == 0)
            {
                Debug.Log("[AbilityForest] Initialized with 0 innate trees.");
                return;
            }

            AddTrees(innateTrees, source: "innate");
            Debug.Log($"[AbilityForest] Init — {ResolveSummary()}");
        }

        // 远期 — 角色创建系统 + SCharacterBuild 接入后扩展构造函数：
        // public AbilityForest(AbilityTreeSO[] innateTrees, TreeSelection[] talents) : this(innateTrees)
        // {
        //     if (talents != null)
        //         foreach (var t in talents)
        //             AddTree(ResolveTree(t.treeId), t.nodeIds, source: "talent");
        // }

        /// <summary>最近一次解析产出的主动技能列表（Q/E/R/F）。</summary>
        public ActiveAbilitySO[] ResolvedActives { get; private set; } = System.Array.Empty<ActiveAbilitySO>();

        /// <summary>最近一次解析产出的被动技能列表。</summary>
        public PassiveAbilitySO[] ResolvedPassives { get; private set; } = System.Array.Empty<PassiveAbilitySO>();

        // ── 武器 ──────────────────────────────────────────

        /// <summary>
        /// 更新当前武器标签并触发技能重解析。
        /// 装备切换时由 CharacterActor.SwitchWeapon 调用。
        /// </summary>
        public void SetWeaponTags(rTagContainer weaponTags)
        {
            _weaponTags = weaponTags;
            Resolve();
            Debug.Log($"[AbilityForest] WeaponTags updated — {ResolveSummary()}");
        }

        // ── 树管理 ──────────────────────────────────────────

        /// <summary>批量添加树（全解锁），自动 Resolve。null/空数组安全。</summary>
        public void AddTrees(AbilityTreeSO[] trees, object source)
        {
            if (trees == null) return;
            foreach (var tree in trees)
                AddTreeInternal(tree, source);
            Resolve();
            Debug.Log($"[AbilityForest] +{trees.Length} tree(s) from '{source}' — {ResolveSummary()}");
        }

        /// <summary>添加一棵树并解锁全部节点，自动 Resolve。</summary>
        public void AddTree(AbilityTreeSO tree, object source)
        {
            if (tree == null) return;
            AddTreeInternal(tree, source);
            Resolve();
            Debug.Log($"[AbilityForest] +tree '{tree.treeId}' from '{source}' — {ResolveSummary()}");
        }

        /// <summary>部分解锁添加，自动 Resolve。</summary>
        public void AddTree(AbilityTreeSO tree, HashSet<string> initialUnlocks, object source)
        {
            if (tree == null) return;

            var valid = new HashSet<string>();
            if (tree.nodes != null && initialUnlocks != null)
            {
                foreach (var node in tree.nodes)
                {
                    if (node.nodeId != null && initialUnlocks.Contains(node.nodeId))
                        valid.Add(node.nodeId);
                }
            }

            _activeTrees.Add(new ActiveTree { Tree = tree, UnlockedNodeIds = valid, Source = source });
            Resolve();
        }

        /// <summary>全解锁添加（内部，不触发 Resolve——由上层聚合后统一调用）。</summary>
        private void AddTreeInternal(AbilityTreeSO tree, object source)
        {
            if (tree == null) return;

            var allNodeIds = new HashSet<string>();
            if (tree.nodes != null)
            {
                foreach (var node in tree.nodes)
                    if (!string.IsNullOrEmpty(node.nodeId))
                        allNodeIds.Add(node.nodeId);
            }

            _activeTrees.Add(new ActiveTree { Tree = tree, UnlockedNodeIds = allNodeIds, Source = source });
        }

        /// <summary>按来源移除，自动 Resolve。</summary>
        public void RemoveBySource(object source)
        {
            _activeTrees.RemoveAll(t => t.Source == source);
            Resolve();
            Debug.Log($"[AbilityForest] Removed source '{source}' — {ResolveSummary()}");
        }

        // ── 节点管理 ──────────────────────────────────────────

        /// <summary>解锁单个节点，自动 Resolve。</summary>
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

        /// <summary>查询指定节点是否已解锁。</summary>
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

        // ── 技能解析（内部）──────────────────────────────────

        /// <summary>使用当前存储的 weaponTags 重解析。状态变化时自动调用。</summary>
        private void Resolve()
        {
            var actives = new List<ActiveAbilitySO>();
            var passives = new List<PassiveAbilitySO>();

            foreach (var at in _activeTrees)
            {
                var tree = at.Tree;
                if (tree == null || tree.nodes == null) continue;

                if (!IsWeaponCompatible(tree.compatibleWeaponTags, _weaponTags))
                    continue;

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

        private static bool IsWeaponCompatible(
            rTagDefSO[] compatibleTags,
            rTagContainer weaponTags)
        {
            if (compatibleTags == null || compatibleTags.Length == 0)
                return true;

            if (weaponTags == null)
                return false;

            foreach (var tag in compatibleTags)
            {
                if (tag != null && weaponTags.HasTag(tag.FullTag))
                    return true;
            }
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

            return $"Actives({ResolvedActives.Length}): [{string.Join(", ", activeNames)}] | " +
                   $"Passives({ResolvedPassives.Length}): [{string.Join(", ", passiveNames)}]";
        }
    }
}
