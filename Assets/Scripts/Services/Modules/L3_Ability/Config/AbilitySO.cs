using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 技能定义抽象基类。AbilityDefSO（主动）和 PassiveAbilitySO（被动）的公共根。
    /// 提取共享的 Identity、Effects、Cooldown，SDamageInfo / HitReactionComponent 等
    /// 消费侧使用此类型，不区分主动被动。
    /// </summary>
    public abstract class AbilitySO : ScriptableObject
    {
        [Header("Identity")]
        public string internalName;
        public string displayName;
        public Sprite icon;
        [TextArea(2, 4)]
        public string description;

        [Tooltip("技能分类标签。主动/被动都用此字段组织目录结构。与 activeTag（激活期间持有）不同。")]
        public GameplayTagDefinitionSO categoryTag;

        [Header("Effects")]
        [Tooltip("施加给目标的效果。")]
        public EffectSO[] targetEffects;

        [Tooltip("激活时对持有者自己的效果。")]
        public EffectSO[] selfEffects;

        [Header("Cooldown")]
        [Tooltip("冷却时长（秒）。0=无冷却。")]
        public float cooldownDuration;

        [Tooltip("联动冷却标签。非 null=与其他技能共享冷却。")]
        public GameplayTagDefinitionSO sharedCooldownTag;
    }
}
