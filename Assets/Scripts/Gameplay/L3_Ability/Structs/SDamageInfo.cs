using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 单次命中结果。ExecutionState 产出，Reactor / VFX / Audio / UI 消费。
    /// 纯数据载体，不包含行为。
    ///
    /// Damage[] 按伤害通道分解，每个 DamageEntry 对应一个实体伤害通道（武器/身体），
    /// 经过技能修正后的 outgoing 伤害。
    /// </summary>
    public readonly struct SDamageInfo
    {
        /// <summary>攻击者。</summary>
        public readonly GameObject Caster;

        /// <summary>被击中的目标。</summary>
        public readonly GameObject Target;

        /// <summary>按通道分解的伤害数据。Reactor 侧按 Tag 路由抗性。</summary>
        public readonly DamageEntry[] Damage;

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

        /// <summary>瞬时伤害总和（Duration≤0）。向后兼容大多数消费者。</summary>
        public float TotalAmount
        {
            get
            {
                if (Damage == null) return 0f;
                float sum = 0f;
                foreach (var d in Damage)
                    if (d.IsInstant) sum += d.Amount;
                return sum;
            }
        }

        /// <summary>完整构造。</summary>
        public SDamageInfo(GameObject caster, GameObject target, DamageEntry[] damage,
            Vector3 hitPoint, Vector3 hitDirection,
            AbilitySO sourceAbility = null,
            AbilityInstance sourceInstance = null,
            ImpactEffectSO impactEffect = null)
        {
            Caster = caster;
            Target = target;
            Damage = damage;
            HitPoint = hitPoint;
            HitDirection = hitDirection.normalized;
            SourceAbility = sourceAbility;
            SourceInstance = sourceInstance;
            ImpactEffect = impactEffect;
        }

        public static SDamageInfo None => default;
    }
}
