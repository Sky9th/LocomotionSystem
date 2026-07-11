using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/System/InputTimeSlowEvent", fileName = "InputTimeSlowEvent")]
    public sealed class InputTimeSlowEvent : GameEvent<SButtonInputPayload> { }
}
