using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 游戏效果抽象基类。所有效果——伤害、硬直、消耗、噪音、冷却、Buff——统一管道。
    /// ActiveAbilitySO.targetEffects[] / selfEffects[] 持有子类实例。
    ///
    /// 对标 UE GAS UGameplayEffect。
    /// </summary>
    public abstract class EffectSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("效果本身的身份标签。DamageEffect→Damage.Elemental.Fire, Impact→Impact.Launch。防御/AI/VFX 用此路由。")]
        public RdTagDefSO effectTag;

        [Tooltip("效果描述。策划可读的说明文本。")]
        public string description;

        [Header("Duration")]
        [Tooltip("持续时间（秒）。≤0 为瞬时效果，>0 持续 tick。")]
        public float duration;

        [Header("Stacking")]
        [Tooltip("是否可叠加。false 则重复施加时刷新持续时间。")]
        public bool stackable;

        [Tooltip("最大叠加层数。仅 stackable=true 时生效。")]
        public int maxStacks = 1;

        [Header("Gating (Phase 5+)")]
        [Tooltip("施加条件。任意一个匹配则拒绝施加。")]
        public RdTagDefSO[] applicationBlockedTags;
    }
}
