using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/UI/InputEscapeEvent", fileName = "InputEscapeEvent")]
    public sealed class InputEscapeEvent : GameEvent<SButtonInputPayload> { }
}
