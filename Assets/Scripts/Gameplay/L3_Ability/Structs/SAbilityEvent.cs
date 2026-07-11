namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 技能事件载荷。发布给 UI 层（冷却图标、激活反馈、拒绝原因）。
    /// 通过 AbilityExecutor.OnSkillEvent 或 GameEvent&lt;SAbilityEvent&gt; 发布。
    /// </summary>
    public readonly struct SAbilityEvent
    {
        /// <summary>事件类型。</summary>
        public readonly EAbilityEventType EventType;

        /// <summary>技能槽位索引 (0=Q, 1=E, 2=R, 3=F)。</summary>
        public readonly int SlotIndex;

        /// <summary>拒绝原因。仅 EventType=Rejected 时有值。</summary>
        public readonly string Reason;

        public SAbilityEvent(EAbilityEventType eventType, int slotIndex, string reason = null)
        {
            EventType = eventType;
            SlotIndex = slotIndex;
            Reason = reason;
        }

        public static SAbilityEvent Activated(int slotIndex) => new(EAbilityEventType.Activated, slotIndex);
        public static SAbilityEvent Completed(int slotIndex) => new(EAbilityEventType.Completed, slotIndex);
        public static SAbilityEvent Rejected(int slotIndex, string reason) => new(EAbilityEventType.Rejected, slotIndex, reason);
    }

}
