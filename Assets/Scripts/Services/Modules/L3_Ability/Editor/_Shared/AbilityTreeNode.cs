#if UNITY_EDITOR
using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// Ability 树节点。文件夹 = Tag category，叶子 = Ability。
    /// 仿 TagNode 设计。
    /// </summary>
    public class AbilityTreeNode
    {
        public string DisplayName;              // tag leafName 或 ability displayName
        public string FullPath;                 // 完整路径，用于 foldout key 和搜索匹配
        public int Depth;                       // 嵌套层级，根=0
        public bool IsFolder;                   // true=Tag 分类, false=Ability 技能
        public GameplayTagDefinitionSO Tag;     // 对应的 Tag（folder 才有）
        public AbilitySO Ability;               // 对应的 Ability（Ability leaf）
        public EffectSO Effect;                 // 对应的 Effect（Effect leaf）
        public AbilitySearchSO Search;          // 对应的 Search（Search leaf）
        public AbilityActivationSO Activation;  // 对应的 Activation（Activation leaf）
        public NoiseEventSO Noise;              // 对应的 Noise（Noise leaf）
        public AbilityTreeNode Parent;
        public List<AbilityTreeNode> Children = new();
        public int AbilityCount;                // 子树中 Ability 总数
        public bool Exists => true;             // 对齐 TagNode.Exists，始终 true
    }
}
#endif
