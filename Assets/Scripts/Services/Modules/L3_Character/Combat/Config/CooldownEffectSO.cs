using RedDust.Core;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 冷却效果定义。门控用——激活前检查是否持有 cooldownTag。
    /// 冷却永远是单数（一个技能一个冷却），与 selfEffects[] 的数组性质不同，独立引用。
    /// 对标 UE GAS：Cooldown GE 的 Duration Policy + GrantedTag。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Combat/Cooldown Effect", fileName = "GE_Cooldown_")]
    public sealed class CooldownEffectSO : ScriptableObject
    {
        [Tooltip("冷却时长（秒）。")]
        public float duration;

        [Tooltip("冷却标签。施加后门控用 HasTagExact 检查。")]
        public GameplayTagDefinitionSO cooldownTag;
    }
}
