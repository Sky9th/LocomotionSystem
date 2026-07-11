using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputJumpEvent", fileName = "InputJumpEvent")]
    public sealed class InputJumpEvent : GameEvent<SButtonInputPayload> { }
}
