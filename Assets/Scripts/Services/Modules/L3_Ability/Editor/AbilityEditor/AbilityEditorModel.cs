#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using RedDust.Core;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// Ability Editor 数据模型。扫描 AssetDatabase，缓存所有 Ability 及子资产。
    /// 仿 TagTreeModel 设计——纯数据层，不涉 UI。
    /// </summary>
    public class AbilityEditorModel
    {
        // ── 主资产 ──
        public List<AbilityDefSO> AllDefs = new();
        public List<PassiveAbilitySO> AllPassives = new();
        public List<AbilitySO> AllAbilities => AllDefs.Cast<AbilitySO>().Concat(AllPassives).ToList();

        // ── 子资产缓存（按具体类型，供右栏 Picker 使用）──
        public List<AbilityActivationSO> AllActivations = new();
        public List<AbilitySearchSO> AllSearches = new();
        public List<EffectSO> AllEffects = new();
        public List<NoiseEventSO> AllNoises = new();

        // ── 索引 ──
        public Dictionary<string, AbilitySO> AbilityIndex = new();

        // ── 树 ──
        public List<AbilityTreeNode> TreeRoots = new();
        public List<AbilityTreeNode> EffectTreeRoots = new();
        public Dictionary<string, AbilityTreeNode> TreeNodeIndex = new();

        // ── 统计 ──
        public int TotalCount => AbilityIndex.Count;

        /// <summary>
        /// 全量扫描 AssetDatabase，重建所有列表、索引和树。
        /// </summary>
        public void Refresh()
        {
            AllDefs.Clear();
            AllPassives.Clear();
            AllActivations.Clear();
            AllSearches.Clear();
            AllEffects.Clear();
            AllNoises.Clear();
            AbilityIndex.Clear();
            TreeRoots.Clear();
            TreeNodeIndex.Clear();

            // 主资产
            var defGuids = AssetDatabase.FindAssets("t:AbilityDefSO");
            foreach (var guid in defGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<AbilityDefSO>(path);
                if (def != null)
                {
                    AllDefs.Add(def);
                    AbilityIndex[def.name] = def;
                }
            }

            var passiveGuids = AssetDatabase.FindAssets("t:PassiveAbilitySO");
            foreach (var guid in passiveGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var passive = AssetDatabase.LoadAssetAtPath<PassiveAbilitySO>(path);
                if (passive != null)
                {
                    AllPassives.Add(passive);
                    AbilityIndex[passive.name] = passive;
                }
            }

            // 子资产
            var actGuids = AssetDatabase.FindAssets("t:AbilityActivationSO");
            foreach (var guid in actGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<AbilityActivationSO>(path);
                if (a != null) AllActivations.Add(a);
            }

            var searchGuids = AssetDatabase.FindAssets("t:AbilitySearchSO");
            foreach (var guid in searchGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var s = AssetDatabase.LoadAssetAtPath<AbilitySearchSO>(path);
                if (s != null) AllSearches.Add(s);
            }

            var effectGuids = AssetDatabase.FindAssets("t:EffectSO");
            foreach (var guid in effectGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var e = AssetDatabase.LoadAssetAtPath<EffectSO>(path);
                if (e != null) AllEffects.Add(e);
            }

            var noiseGuids = AssetDatabase.FindAssets("t:NoiseEventSO");
            foreach (var guid in noiseGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var n = AssetDatabase.LoadAssetAtPath<NoiseEventSO>(path);
                if (n != null) AllNoises.Add(n);
            }

            // 排序
            AllDefs.Sort((a, b) => string.CompareOrdinal(a.displayName, b.displayName));
            AllPassives.Sort((a, b) => string.CompareOrdinal(a.displayName, b.displayName));
            AllActivations.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            AllSearches.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            AllEffects.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            AllNoises.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            BuildTree();
            BuildEffectTree();
        }

        // ── Ability 树构建 ──
        private void BuildTree()
        {
            TreeRoots.Clear();
            TreeNodeIndex.Clear();

            // 1. 收集所有技能，按 filter 分类
            var all = AllAbilities;

            // 2. 为每个有 abilityTag 的技能创建路径
            foreach (var ability in all)
            {
                var aTag = ability.abilityTag;
                if (aTag == null)
                {
                    // 无 abilityTag → 放 Uncategorized
                    AddToFolder("Uncategorized", 0, ability, null);
                    continue;
                }

                // 按 tag parent 链构建文件夹路径
                var tagChain = new List<GameplayTagDefinitionSO>();
                var t = aTag;
                while (t != null)
                {
                    tagChain.Add(t);
                    t = t.Parent;
                }
                tagChain.Reverse(); // 从根到叶

                // 确保每层文件夹存在
                AbilityTreeNode parentNode = null;
                var accumulatedPath = "";
                for (int i = 0; i < tagChain.Count; i++)
                {
                    var tag = tagChain[i];
                    accumulatedPath = i == 0 ? tag.LeafName : $"{accumulatedPath}.{tag.LeafName}";

                    if (!TreeNodeIndex.TryGetValue(accumulatedPath, out var folderNode))
                    {
                        folderNode = new AbilityTreeNode
                        {
                            DisplayName = tag.LeafName,
                            FullPath = accumulatedPath,
                            Depth = i + 1,
                            IsFolder = true,
                            Tag = tag,
                            Parent = parentNode,
                        };
                        TreeNodeIndex[accumulatedPath] = folderNode;

                        if (parentNode != null)
                            parentNode.Children.Add(folderNode);
                        else
                            TreeRoots.Add(folderNode);
                    }

                    parentNode = folderNode;
                }

                // 将技能作为叶子添加到最深文件夹
                var leafNode = new AbilityTreeNode
                {
                    DisplayName = !string.IsNullOrEmpty(ability.displayName)
                        ? ability.displayName : ability.name,
                    FullPath = $"{parentNode.FullPath}/{ability.name}",
                    Depth = parentNode.Depth + 1,
                    IsFolder = false,
                    Ability = ability,
                    Parent = parentNode,
                };
                parentNode.Children.Add(leafNode);
            }

            // 3. 排序：文件夹先，字母序；叶子在后，字母序
            void SortRecursive(List<AbilityTreeNode> nodes)
            {
                nodes.Sort((a, b) =>
                {
                    if (a.IsFolder != b.IsFolder)
                        return a.IsFolder ? -1 : 1;
                    return string.CompareOrdinal(a.DisplayName, b.DisplayName);
                });
                foreach (var n in nodes)
                    SortRecursive(n.Children);
            }
            SortRecursive(TreeRoots);

            // 4. 计算每棵子树的 Ability count
            ComputeAbilityCount();
        }

        private void AddToFolder(string folderName, int depth, AbilitySO ability, AbilityTreeNode parent)
        {
            if (!TreeNodeIndex.TryGetValue(folderName, out var folderNode))
            {
                folderNode = new AbilityTreeNode
                {
                    DisplayName = folderName,
                    FullPath = folderName,
                    Depth = depth,
                    IsFolder = true,
                    Parent = parent,
                };
                TreeNodeIndex[folderName] = folderNode;
                TreeRoots.Add(folderNode);
            }

            var leaf = new AbilityTreeNode
            {
                DisplayName = !string.IsNullOrEmpty(ability.displayName)
                    ? ability.displayName : ability.name,
                FullPath = $"{folderName}/{ability.name}",
                Depth = depth + 1,
                IsFolder = false,
                Ability = ability,
                Parent = folderNode,
            };
            folderNode.Children.Add(leaf);
        }

        private void ComputeAbilityCount()
        {
            int CountRecursive(AbilityTreeNode node)
            {
                if (!node.IsFolder) return 1;
                var total = 0;
                foreach (var c in node.Children) total += CountRecursive(c);
                node.AbilityCount = total;
                return total;
            }
            foreach (var root in TreeRoots) CountRecursive(root);
        }

        // ── Effect 树构建（用 effectTag 组织，逻辑同 Ability 树）──
        private void BuildEffectTree()
        {
            EffectTreeRoots.Clear();

            foreach (var effect in AllEffects)
            {
                var tag = effect.effectTag;
                if (tag == null)
                {
                    AddEffectToFolder("Uncategorized", 0, effect, null);
                    continue;
                }

                var tagChain = new List<GameplayTagDefinitionSO>();
                var t = tag;
                while (t != null)
                {
                    tagChain.Add(t);
                    t = t.Parent;
                }
                tagChain.Reverse();

                AbilityTreeNode parentNode = null;
                var accumulatedPath = "";
                for (int i = 0; i < tagChain.Count; i++)
                {
                    var ct = tagChain[i];
                    accumulatedPath = i == 0 ? ct.LeafName : $"{accumulatedPath}.{ct.LeafName}";

                    var key = $"eff_{accumulatedPath}";
                    if (!TreeNodeIndex.TryGetValue(key, out var folderNode))
                    {
                        folderNode = new AbilityTreeNode
                        {
                            DisplayName = ct.LeafName,
                            FullPath = accumulatedPath,
                            Depth = i + 1,
                            IsFolder = true,
                            Tag = ct,
                            Parent = parentNode,
                        };
                        TreeNodeIndex[key] = folderNode;

                        if (parentNode != null)
                            parentNode.Children.Add(folderNode);
                        else
                            EffectTreeRoots.Add(folderNode);
                    }

                    parentNode = folderNode;
                }

                var leaf = new AbilityTreeNode
                {
                    DisplayName = effect.name,
                    FullPath = $"{parentNode.FullPath}/{effect.name}",
                    Depth = parentNode.Depth + 1,
                    IsFolder = false,
                    Effect = effect,
                    Parent = parentNode,
                };
                parentNode.Children.Add(leaf);
            }

            void SortRecursive(List<AbilityTreeNode> nodes)
            {
                nodes.Sort((a, b) =>
                {
                    if (a.IsFolder != b.IsFolder) return a.IsFolder ? -1 : 1;
                    return string.CompareOrdinal(a.DisplayName, b.DisplayName);
                });
                foreach (var n in nodes) SortRecursive(n.Children);
            }
            SortRecursive(EffectTreeRoots);

            int CountRecursive(AbilityTreeNode node)
            {
                if (!node.IsFolder) return 1;
                var total = 0;
                foreach (var c in node.Children) total += CountRecursive(c);
                node.AbilityCount = total;
                return total;
            }
            foreach (var root in EffectTreeRoots) CountRecursive(root);
        }

        private void AddEffectToFolder(string folderName, int depth, EffectSO effect, AbilityTreeNode parent)
        {
            var key = $"eff_{folderName}";
            if (!TreeNodeIndex.TryGetValue(key, out var folderNode))
            {
                folderNode = new AbilityTreeNode
                {
                    DisplayName = folderName,
                    FullPath = folderName,
                    Depth = depth,
                    IsFolder = true,
                    Parent = parent,
                };
                TreeNodeIndex[key] = folderNode;
                EffectTreeRoots.Add(folderNode);
            }

            var leaf = new AbilityTreeNode
            {
                DisplayName = effect.name,
                FullPath = $"{folderName}/{effect.name}",
                Depth = depth + 1,
                IsFolder = false,
                Effect = effect,
                Parent = folderNode,
            };
            folderNode.Children.Add(leaf);
        }
    }

    public enum AbilityTypeFilter
    {
        All,
        Active,
        Passive,
    }

    /// <summary>
    /// 右栏子资产槽位标识。中间栏点击哪个槽位，右栏就显示对应类型的 Picker。
    /// </summary>
    public enum SubAssetSlot
    {
        None,
        Activation,        // AbilityActivationSO
        Search,            // AbilitySearchSO (abstract)
        TargetEffects,     // EffectSO[] — 添加操作
        SelfEffects,       // EffectSO[] — 添加操作
        Noise,             // NoiseEventSO
    }
}
#endif
