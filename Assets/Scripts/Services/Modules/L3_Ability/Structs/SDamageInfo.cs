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

        /// <summary>伤害类型标签。防御公式用此路由抗性。</summary>
        public readonly GameplayTag EffectTag;

        /// <summary>命中世界坐标。</summary>
        public readonly Vector3 HitPoint;

        /// <summary>从攻击者指向命中点的方向。</summary>
        public readonly Vector3 HitDirection;

        /// <summary>伤害来源技能。AbilityDefSO 或 PassiveAbilitySO。</summary>
        public readonly AbilitySO SourceAbility;

        public SDamageInfo(GameObject caster, GameObject target, float amount, GameplayTag effectTag,
            Vector3 hitPoint, Vector3 hitDirection,
            AbilitySO sourceAbility = null)
        {
            Caster = caster;
            Target = target;
            Amount = amount;
            EffectTag = effectTag;
            HitPoint = hitPoint;
            HitDirection = hitDirection.normalized;
            SourceAbility = sourceAbility;
        }

        public static SDamageInfo None => default;
    }
}
