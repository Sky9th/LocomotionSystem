using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 命中事件载荷。每次技能激活命中至少一个目标时发布一次。
    /// 通过 GameEvent&lt;SHitEvent&gt; 发布，Audio / VFX / Proficiency 等系统订阅。
    /// </summary>
    public readonly struct SHitEvent
    {
        /// <summary>攻击者 GameObject。</summary>
        public readonly GameObject Attacker;

        /// <summary>所有命中结果。</summary>
        public readonly IReadOnlyList<DamageInfo> Hits;

        /// <summary>触发命中的技能。</summary>
        public readonly SkillDefSO Skill;

        /// <summary>命中点（多目标时取首个）。</summary>
        public readonly Vector3 HitPosition;

        public SHitEvent(GameObject attacker, IReadOnlyList<DamageInfo> hits, SkillDefSO skill, Vector3 hitPosition)
        {
            Attacker = attacker;
            Hits = hits;
            Skill = skill;
            HitPosition = hitPosition;
        }

        public static SHitEvent None => default;
    }
}
