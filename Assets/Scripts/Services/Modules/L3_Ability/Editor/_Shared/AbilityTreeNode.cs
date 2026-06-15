#if UNITY_EDITOR
using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// Ability 树节点。文件夹 = Tag category，叶子 = 技能/效果/搜索/激活/噪音资产。
    /// 5 个类型便利属性最终落到 <see cref="Asset"/>。
    /// </summary>
    public class AbilityTreeNode
    {
        public string DisplayName;
        public string FullPath;
        public int Depth;
        public bool IsFolder;
        public GameplayTagDefinitionSO Tag;
        public AbilityTreeNode Parent;
        public List<AbilityTreeNode> Children = new();
        public int LeafCount;

        /// <summary>叶子：持有的资产（5 种类型便利属性均映射到此）。文件夹：null。</summary>
        public ScriptableObject Asset;

        public AbilitySO Ability
        {
            get => Asset as AbilitySO;
            set => Asset = value;
        }
        public EffectSO Effect
        {
            get => Asset as EffectSO;
            set => Asset = value;
        }
        public AbilitySearchSO Search
        {
            get => Asset as AbilitySearchSO;
            set => Asset = value;
        }
        public AbilityActivationSO Activation
        {
            get => Asset as AbilityActivationSO;
            set => Asset = value;
        }
        public NoiseEventSO Noise
        {
            get => Asset as NoiseEventSO;
            set => Asset = value;
        }
    }
}
#endif
