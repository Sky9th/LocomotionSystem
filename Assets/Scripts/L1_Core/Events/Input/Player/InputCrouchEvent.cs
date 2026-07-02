using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputCrouchEvent", fileName = "InputCrouchEvent")]
    public sealed class InputCrouchEvent : GameEvent<SButtonInputPayload> { }
}
