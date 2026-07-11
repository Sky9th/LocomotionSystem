namespace RedDust.Ability
{
    /// <summary>
    /// 受击反应等级。ImpactEffectSO 资产配置，CharacterCombat 据此选择 LocomotionAnimationSetSO 对应动画。
    /// </summary>
    public enum EHitReactionLevel
    {
        /// <summary>轻受击 → LocomotionAnimationSetSO.hitReactionFlinch</summary>
        Flinch,

        /// <summary>重受击 → hitReactionStagger</summary>
        Stagger,

        /// <summary>击倒 → hitReactionKnockdown</summary>
        Knockdown,
    }
}
