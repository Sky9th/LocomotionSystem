using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    /// ⛔ DEPRECATED — 已替换为 InputSkillEvent（单 Action 多 Binding 统一事件）。保留旧 SO 避免引用断裂。</summary>
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputSkill1Event", fileName = "InputSkill1Event")]
    public sealed class InputSkill1Event : GameEvent<SButtonInputPayload> { }
}
