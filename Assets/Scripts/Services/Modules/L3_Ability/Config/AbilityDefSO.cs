using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 单个技能的完整数据定义。纯配置，无运行时状态。
    /// 身份 + 激活方式 + 搜索形状 + 效果数组。
    /// 被 AbilityComponent / AbilityPipeline / AbilityDriver 消费。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Ability Definition", fileName = "Ability_")]
    public sealed class AbilityDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string internalName;
        public string displayName;
        public Sprite icon;
        [TextArea(2, 4)]
        public string description;
        [Tooltip("技能分类标签。用于 TagMutualExclusionSO 互斥匹配，与 activeTag（激活期间持有）不同。")]
        public GameplayTagDefinitionSO categoryTag;


        [Header("Activation")]
        [Tooltip("技能「怎么放」的完整定义。动画、阶段时机、激活方式。")]
        public AbilityActivationSO activation;


        [Header("Search")]
        [Tooltip("技能「往哪打」的完整定义。搜索形状、范围、目标筛选。")]
        public AbilitySearchSO search;


        [Header("Effects")]
        [Tooltip("命中目标后施加的效果。DamageEffectSO, ImpactEffectSO, ExecuteEffectSO 等。")]
        public EffectSO[] targetEffects;

        [Tooltip("激活时对自己施加的效果。CostEffectSO, EffectSO 等。")]
        public EffectSO[] selfEffects;

        [Header("Cooldown")]
        [Tooltip("冷却效果。门控用，激活前检查是否持有 cooldownTag。")]
        public CooldownRuleSO cooldownEffect;

        [Header("Gating")]
        [Tooltip("激活期间持有的标签。结束时移除。与 TagMutualExclusionSO 配合做互斥门控。")]
        public GameplayTagDefinitionSO activeTag;

        [Tooltip("无视全局互斥。true=此技能可在互斥标签存在时激活（如翻滚、急救）。")]
        public bool overrideExclusion;

        [Header("Noise")]
        [Tooltip("噪音事件。激活时广播，AI 听觉系统消费。不在 effects 数组里。")]
        public NoiseEventSO noise;

        [Header("Combo (Phase 4.1b)")]
        [Tooltip("连招衔接列表。当前技能在窗口内可衔接的下一个技能。")]
        public SComboLink[] comboLinks;


    }
}
