using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputPrimaryInteractEvent", fileName = "InputPrimaryInteractEvent")]
    public sealed class InputPrimaryInteractEvent : GameEvent<SButtonInputPayload> { }
}
