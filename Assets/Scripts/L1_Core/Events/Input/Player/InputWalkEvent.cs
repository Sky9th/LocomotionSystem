using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputWalkEvent", fileName = "InputWalkEvent")]
    public sealed class InputWalkEvent : GameEvent<SButtonInputPayload> { }
}
