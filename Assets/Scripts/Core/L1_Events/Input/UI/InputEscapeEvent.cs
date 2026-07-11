using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/UI/InputEscapeEvent", fileName = "InputEscapeEvent")]
    public sealed class InputEscapeEvent : GameEvent<SButtonInputPayload> { }
}
