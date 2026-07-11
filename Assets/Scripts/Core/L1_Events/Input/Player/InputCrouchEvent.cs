using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputCrouchEvent", fileName = "InputCrouchEvent")]
    public sealed class InputCrouchEvent : GameEvent<SButtonInputPayload> { }
}
