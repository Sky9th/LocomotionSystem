using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/System/InputTimeResumeEvent", fileName = "InputTimeResumeEvent")]
    public sealed class InputTimeResumeEvent : GameEvent<SButtonInputPayload> { }
}
