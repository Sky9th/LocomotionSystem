using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputSprintEvent", fileName = "InputSprintEvent")]
    public sealed class InputSprintEvent : GameEvent<SButtonInputPayload> { }
}
