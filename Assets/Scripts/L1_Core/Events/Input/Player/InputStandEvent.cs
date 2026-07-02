using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputStandEvent", fileName = "InputStandEvent")]
    public sealed class InputStandEvent : GameEvent<SButtonInputPayload> { }
}
