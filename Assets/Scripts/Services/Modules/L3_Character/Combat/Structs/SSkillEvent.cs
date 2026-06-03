namespace RedDust.Character.Combat
{
    /// <summary>
    /// 技能事件载荷。发布给 UI 层（冷却图标、激活反馈、拒绝原因）。
    /// 通过 CombatComponent.OnSkillEvent 或 GameEvent&lt;SSkillEvent&gt; 发布。
    /// </summary>
    public readonly struct SSkillEvent
    {
        /// <summary>事件类型。</summary>
        public readonly ESkillEventType EventType;

        /// <summary>技能槽位索引 (0=Q, 1=E, 2=R, 3=F)。</summary>
        public readonly int SlotIndex;

        /// <summary>拒绝原因。仅 EventType=Rejected 时有值。</summary>
        public readonly string Reason;

        public SSkillEvent(ESkillEventType eventType, int slotIndex, string reason = null)
        {
            EventType = eventType;
            SlotIndex = slotIndex;
            Reason = reason;
        }

        public static SSkillEvent Activated(int slotIndex) => new(ESkillEventType.Activated, slotIndex);
        public static SSkillEvent Completed(int slotIndex) => new(ESkillEventType.Completed, slotIndex);
        public static SSkillEvent Rejected(int slotIndex, string reason) => new(ESkillEventType.Rejected, slotIndex, reason);
    }

}
