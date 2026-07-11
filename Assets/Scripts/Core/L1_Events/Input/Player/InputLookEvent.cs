using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputLookEvent", fileName = "InputLookEvent")]
    public sealed class InputLookEvent : GameEvent<SVector2InputPayload> { }
}
