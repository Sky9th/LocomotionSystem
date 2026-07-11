using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 冲击效果。硬直 + 击退。防御侧拿 staggerValue 跟自身霸体阈值比较，自行决定反应。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Effect/Impact", fileName = "ImpactEffect_")]
    public sealed class ImpactEffectSO : EffectSO
    {
        [Header("Reaction")]
        [Tooltip("受击反应等级。决定播放 Flinch / Stagger / Knockdown 中的哪个动画。")]
        public EHitReactionLevel reactionLevel = EHitReactionLevel.Flinch;

        [Header("Impact")]
        [Tooltip("冲击值。0=无硬直, 越大越难防。防御侧比较自身霸体阈值。")]
        public float staggerValue;

        [Tooltip("击退力度。0=纯硬直无位移。")]
        public float knockbackForce;

        [Tooltip("击退方向。")]
        public EKnockbackDirection knockbackDir;
    }
}
