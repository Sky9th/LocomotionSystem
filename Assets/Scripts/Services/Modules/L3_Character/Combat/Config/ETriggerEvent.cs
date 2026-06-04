namespace RedDust.Character.Combat
{
    /// <summary>
    /// 被动技能触发事件类型。
    /// </summary>
    public enum ETriggerEvent
    {
        /// <summary>击杀敌人时。</summary>
        OnKill = 0,

        /// <summary>命中敌人时。</summary>
        OnHit = 1,

        /// <summary>被击中/受伤时。</summary>
        OnDamaged = 2,

        /// <summary>自身 HP 低于阈值时（持续检查）。</summary>
        OnLowHP = 3,

        /// <summary>成功翻滚闪避后。</summary>
        OnDodge = 4,

        /// <summary>连招达到指定段位时。</summary>
        OnComboStage = 5,

        /// <summary>装备/激活时立即生效。</summary>
        OnEquip = 6,
    }
}
