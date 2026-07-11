using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    /// ⛔ DEPRECATED — 已替换为 InputSkillEvent（单 Action 多 Binding 统一事件）。保留旧 SO 避免引用断裂。</summary>
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputSkill2Event", fileName = "InputSkill2Event")]
    public sealed class InputSkill2Event : GameEvent<SButtonInputPayload> { }
}
