using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputProneEvent", fileName = "InputProneEvent")]
    public sealed class InputProneEvent : GameEvent<SButtonInputPayload> { }
}
