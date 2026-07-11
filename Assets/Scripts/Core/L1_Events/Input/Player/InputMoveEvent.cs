using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputMoveEvent", fileName = "InputMoveEvent")]
    public sealed class InputMoveEvent : GameEvent<SVector2InputPayload> { }
}
