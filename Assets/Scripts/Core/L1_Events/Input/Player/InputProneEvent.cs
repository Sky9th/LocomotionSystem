using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputProneEvent", fileName = "InputProneEvent")]
    public sealed class InputProneEvent : GameEvent<SButtonInputPayload> { }
}
