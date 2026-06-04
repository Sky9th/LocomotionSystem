using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 单次命中结果。CombatPipeline 产出，AbilityComponent.ApplyDamage() 消费。
    /// 纯数据载体，不包含行为。
    /// </summary>
    public readonly struct SDamageInfo
    {
        /// <summary>被击中的 GameObject。</summary>
        public readonly GameObject Target;

        /// <summary>最终伤害值。</summary>
        public readonly float Amount;

        /// <summary>命中世界坐标。</summary>
        public readonly Vector3 HitPoint;

        /// <summary>从攻击者指向命中点的方向（以攻击者为原点）。</summary>
        public readonly Vector3 HitDirection;

        /// <summary>伤害来源技能。可为 null。</summary>
        public readonly AbilityDefSO SourceSkill;

        public SDamageInfo(GameObject target, float amount, Vector3 hitPoint, Vector3 hitDirection, AbilityDefSO sourceSkill)
        {
            Target = target;
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection.normalized;
            SourceSkill = sourceSkill;
        }

        public static SDamageInfo None => default;
    }
}
