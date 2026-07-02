using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/UI/InputPreviousEvent", fileName = "InputPreviousEvent")]
    public sealed class InputPreviousEvent : GameEvent<SButtonInputPayload> { }
}
