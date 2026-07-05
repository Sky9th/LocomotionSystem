using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 单次命中结果。AbilityExecutor 产出，防御公式 / VFX / Audio 消费。
    /// 纯数据载体，不包含行为。
    /// </summary>
    public readonly struct SDamageInfo
    {
        /// <summary>攻击者。</summary>
        public readonly GameObject Caster;

        /// <summary>被击中的目标。</summary>
        public readonly GameObject Target;

        /// <summary>最终伤害值。</summary>
        public readonly float Amount;

        /// <summary>伤害类型标签。武器多伤害类型时并存，防御公式逐项路由抗性。</summary>
        public readonly RdTag[] EffectTags;

        /// <summary>命中世界坐标。</summary>
        public readonly Vector3 HitPoint;

        /// <summary>从攻击者指向命中点的方向。</summary>
        public readonly Vector3 HitDirection;

        /// <summary>伤害来源技能。ActiveAbilitySO 或 PassiveAbilitySO。</summary>
        public readonly AbilitySO SourceAbility;

        /// <summary>施法者的技能实例。Reactor 侧施加 Buff/Tag 时用作 Owner。</summary>
        public readonly AbilityInstance SourceInstance;

        /// <summary>冲击效果（硬直+击退）。null 表示纯伤害无冲击。</summary>
        public readonly ImpactEffectSO ImpactEffect;

        /// <summary>完整构造。</summary>
        public SDamageInfo(GameObject caster, GameObject target, float amount, RdTag[] effectTags,
            Vector3 hitPoint, Vector3 hitDirection,
            AbilitySO sourceAbility = null,
            AbilityInstance sourceInstance = null,
            ImpactEffectSO impactEffect = null)
        {
            Caster = caster;
            Target = target;
            Amount = amount;
            EffectTags = effectTags;
            HitPoint = hitPoint;
            HitDirection = hitDirection.normalized;
            SourceAbility = sourceAbility;
            SourceInstance = sourceInstance;
            ImpactEffect = impactEffect;
        }

        public static SDamageInfo None => default;
    }
}
