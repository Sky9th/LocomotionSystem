using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputJumpEvent", fileName = "InputJumpEvent")]
    public sealed class InputJumpEvent : GameEvent<SButtonInputPayload> { }
}
