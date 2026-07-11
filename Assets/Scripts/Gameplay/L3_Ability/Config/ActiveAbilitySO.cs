using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 单个技能的完整数据定义。纯配置，无运行时状态。
    /// 身份 + 激活方式 + 搜索形状 + 效果数组。
    /// 被 AbilityExecutor / AbilityPipeline / AbilityDriver 消费。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Active Ability", fileName = "Ability_")]
    public sealed class ActiveAbilitySO : AbilitySO
    {
        [Header("Activation")]
        [Tooltip("技能「怎么放」的完整定义。动画、阶段时机、激活方式。")]
        public AbilityActivationSO activation;


        [Header("Search")]
        [Tooltip("技能「往哪打」的完整定义。搜索形状、范围、目标筛选。")]
        public AbilitySearchSO search;


        [Header("Gating")]
        [Tooltip("无视互斥门控。true=此技能可在互斥标签存在时激活（如翻滚、急救）。")]
        public bool overrideExclusion;

        [Tooltip("额外互斥标签。除了默认的 abilityTag.Parent 前缀匹配外，持有这些标签也会阻止激活。用于跨分类互斥。")]
        public RdTagDefSO[] extraExclusionTags;

        [Header("Noise")]
        [Tooltip("噪音事件。激活时广播，AI 听觉系统消费。不在 effects 数组里。")]
        public NoiseEventSO noise;

        [Header("Combo (Phase 4.1b)")]
        [Tooltip("连招衔接列表。当前技能在窗口内可衔接的下一个技能。")]
        public SComboLink[] comboLinks;


    }
}
