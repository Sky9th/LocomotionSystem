using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputStandEvent", fileName = "InputStandEvent")]
    public sealed class InputStandEvent : GameEvent<SButtonInputPayload> { }
}
