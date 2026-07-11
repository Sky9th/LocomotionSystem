using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputMoveEvent", fileName = "InputMoveEvent")]
    public sealed class InputMoveEvent : GameEvent<SVector2InputPayload> { }
}
