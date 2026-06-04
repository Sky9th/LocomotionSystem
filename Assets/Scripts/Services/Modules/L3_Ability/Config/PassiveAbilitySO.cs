using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 被动技能定义。事件触发 → 条件检查 → 施加效果。
    /// 与主动技能共享 EffectSO 体系，但不走 activation/search/noise/combo 管道。
    ///
    /// 两种触发路径：
    /// - 常用: trigger 枚举，AbilityComponent 内部流程节点直接匹配（OnHit/OnKill/OnDamaged/OnDodge/OnComboStage/OnEquip）
    /// - 罕用: triggerChannel 引用外部 EventChannel 资产（时间/天气/Boss阶段），非 null 时覆盖枚举匹配
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Passive Ability", fileName = "Passive_")]
    public sealed class PassiveAbilitySO : ScriptableObject
    {
        [Header("Identity")]
        public string internalName;
        public string displayName;
        public Sprite icon;
        [TextArea(2, 4)]
        public string description;

        [Header("Trigger")]
        [Tooltip("触发事件类型。常用事件走枚举。")]
        public ETriggerEvent trigger;

        [Tooltip("外部事件通道。非 null 时覆盖 trigger 枚举，用于罕见事件（时间/天气/Boss阶段）。")]
        public EventChannelBase triggerChannel;

        [Tooltip("触发参数。OnLowHP=HP阈值(0~1), OnComboStage=连招段号。")]
        public float triggerValue;

        [Header("Condition")]
        [Tooltip("事件主体需持有此标签才触发。null=无条件。")]
        public GameplayTagDefinitionSO conditionTag;

        [Header("Effects")]
        [Tooltip("施加给事件主体（被击中的敌人/攻击者/Boss）。")]
        public EffectSO[] targetEffects;

        [Tooltip("施加给技能持有者自己。")]
        public EffectSO[] selfEffects;

        [Header("Cooldown")]
        [Tooltip("触发冷却。null=每次事件都触发。")]
        public CooldownRuleSO cooldownEffect;
    }
}
