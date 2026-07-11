namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 被动技能触发事件类型。
    /// </summary>
    public enum ETriggerEvent
    {
        /// <summary>未设置。默认值，防止 OnKill 被误触发。</summary>
        None = 0,

        /// <summary>击杀敌人时。</summary>
        OnKill = 1,

        /// <summary>命中敌人时。</summary>
        OnHit = 2,

        /// <summary>被击中/受伤时。</summary>
        OnDamaged = 3,

        /// <summary>自身 HP 低于阈值时（持续检查）。</summary>
        OnLowHP = 4,

        /// <summary>成功翻滚闪避后。</summary>
        OnDodge = 5,

        /// <summary>连招达到指定段位时。</summary>
        OnComboStage = 6,

        /// <summary>装备/激活时立即生效。</summary>
        OnEquip = 7,

        /// <summary>实体进入触发器区域时（陷阱/环境）。</summary>
        OnEnterArea = 8,

        /// <summary>实体离开触发器区域时。</summary>
        OnExitArea = 9,
    }
}
