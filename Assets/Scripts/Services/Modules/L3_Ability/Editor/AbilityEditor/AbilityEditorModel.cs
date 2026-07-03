#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using RedDust.Core;
using RedDust.Shared.EditorUI;
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
        public List<ActiveAbilitySO> AllDefs = new();
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
        public List<EditorTreeNode> TreeRoots = new();
        public List<EditorTreeNode> EffectTreeRoots = new();

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

            // 主资产
            var defGuids = AssetDatabase.FindAssets("t:ActiveAbilitySO");
            foreach (var guid in defGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<ActiveAbilitySO>(path);
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
            var nodeIndex = new Dictionary<string, EditorTreeNode>();

            var all = AllAbilities;

            foreach (var ability in all)
            {
                var aTag = ability.abilityTag;
                if (aTag == null)
                {
                    AddToFolder("Uncategorized", 0, ability, null, nodeIndex);
                    continue;
                }

                var tagChain = new List<RdTagDefSO>();
                var t = aTag;
                while (t != null)
                {
                    tagChain.Add(t);
                    t = t.Parent;
                }
                tagChain.Reverse();

                EditorTreeNode parentNode = null;
                var accumulatedPath = "";
                for (int i = 0; i < tagChain.Count; i++)
                {
                    var tag = tagChain[i];
                    accumulatedPath = i == 0 ? tag.LeafName : $"{accumulatedPath}.{tag.LeafName}";

                    if (!nodeIndex.TryGetValue(accumulatedPath, out var folderNode))
                    {
                        folderNode = new EditorTreeNode
                        {
                            DisplayName = tag.LeafName,
                            FullPath = accumulatedPath,
                            Depth = i + 1,
                            IsFolder = true,
                            UserData = tag,
                            Parent = parentNode,
                        };
                        nodeIndex[accumulatedPath] = folderNode;

                        if (parentNode != null)
                            parentNode.Children.Add(folderNode);
                        else
                            TreeRoots.Add(folderNode);
                    }

                    parentNode = folderNode;
                }

                var leafNode = new EditorTreeNode
                {
                    DisplayName = !string.IsNullOrEmpty(ability.displayName)
                        ? ability.displayName : ability.name,
                    FullPath = $"{parentNode.FullPath}/{ability.name}",
                    Depth = parentNode.Depth + 1,
                    IsFolder = false,
                    UserData = ability,
                    Parent = parentNode,
                };
                parentNode.Children.Add(leafNode);
            }

            EditorTree.SortTreeRecursive(TreeRoots);
            EditorTree.ComputeTreeCounts(TreeRoots);
        }

        private void AddToFolder(string folderName, int depth, AbilitySO ability, EditorTreeNode parent,
            Dictionary<string, EditorTreeNode> nodeIndex)
        {
            if (!nodeIndex.TryGetValue(folderName, out var folderNode))
            {
                folderNode = new EditorTreeNode
                {
                    DisplayName = folderName,
                    FullPath = folderName,
                    Depth = depth,
                    IsFolder = true,
                    Parent = parent,
                };
                nodeIndex[folderName] = folderNode;
                TreeRoots.Add(folderNode);
            }

            var leaf = new EditorTreeNode
            {
                DisplayName = !string.IsNullOrEmpty(ability.displayName)
                    ? ability.displayName : ability.name,
                FullPath = $"{folderName}/{ability.name}",
                Depth = depth + 1,
                IsFolder = false,
                UserData = ability,
                Parent = folderNode,
            };
            folderNode.Children.Add(leaf);
        }

        // ── Effect 树构建（用 effectTag 组织，逻辑同 Ability 树）──
        private void BuildEffectTree()
        {
            EffectTreeRoots.Clear();
            var nodeIndex = new Dictionary<string, EditorTreeNode>();

            foreach (var effect in AllEffects)
            {
                var tag = effect.effectTag;
                if (tag == null)
                {
                    AddEffectToFolder("Uncategorized", 0, effect, null, nodeIndex);
                    continue;
                }

                var tagChain = new List<RdTagDefSO>();
                var t = tag;
                while (t != null)
                {
                    tagChain.Add(t);
                    t = t.Parent;
                }
                tagChain.Reverse();

                EditorTreeNode parentNode = null;
                var accumulatedPath = "";
                for (int i = 0; i < tagChain.Count; i++)
                {
                    var ct = tagChain[i];
                    accumulatedPath = i == 0 ? ct.LeafName : $"{accumulatedPath}.{ct.LeafName}";

                    var key = $"eff_{accumulatedPath}";
                    if (!nodeIndex.TryGetValue(key, out var folderNode))
                    {
                        folderNode = new EditorTreeNode
                        {
                            DisplayName = ct.LeafName,
                            FullPath = accumulatedPath,
                            Depth = i + 1,
                            IsFolder = true,
                            UserData = ct,
                            Parent = parentNode,
                        };
                        nodeIndex[key] = folderNode;

                        if (parentNode != null)
                            parentNode.Children.Add(folderNode);
                        else
                            EffectTreeRoots.Add(folderNode);
                    }

                    parentNode = folderNode;
                }

                var leaf = new EditorTreeNode
                {
                    DisplayName = effect.name,
                    FullPath = $"{parentNode.FullPath}/{effect.name}",
                    Depth = parentNode.Depth + 1,
                    IsFolder = false,
                    UserData = effect,
                    Parent = parentNode,
                };
                parentNode.Children.Add(leaf);
            }

            EditorTree.SortTreeRecursive(EffectTreeRoots);
            EditorTree.ComputeTreeCounts(EffectTreeRoots);
        }

        private void AddEffectToFolder(string folderName, int depth, EffectSO effect, EditorTreeNode parent,
            Dictionary<string, EditorTreeNode> nodeIndex)
        {
            var key = $"eff_{folderName}";
            if (!nodeIndex.TryGetValue(key, out var folderNode))
            {
                folderNode = new EditorTreeNode
                {
                    DisplayName = folderName,
                    FullPath = folderName,
                    Depth = depth,
                    IsFolder = true,
                    Parent = parent,
                };
                nodeIndex[key] = folderNode;
                EffectTreeRoots.Add(folderNode);
            }

            var leaf = new EditorTreeNode
            {
                DisplayName = effect.name,
                FullPath = $"{folderName}/{effect.name}",
                Depth = depth + 1,
                IsFolder = false,
                UserData = effect,
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
