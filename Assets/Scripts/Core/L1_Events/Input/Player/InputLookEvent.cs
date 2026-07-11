using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputLookEvent", fileName = "InputLookEvent")]
    public sealed class InputLookEvent : GameEvent<SVector2InputPayload> { }
}
