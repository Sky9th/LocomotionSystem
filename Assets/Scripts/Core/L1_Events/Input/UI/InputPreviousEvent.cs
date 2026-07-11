using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/UI/InputPreviousEvent", fileName = "InputPreviousEvent")]
    public sealed class InputPreviousEvent : GameEvent<SButtonInputPayload> { }
}
