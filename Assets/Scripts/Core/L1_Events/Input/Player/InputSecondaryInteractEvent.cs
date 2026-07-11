using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputSecondaryInteractEvent", fileName = "InputSecondaryInteractEvent")]
    public sealed class InputSecondaryInteractEvent : GameEvent<SButtonInputPayload> { }
}
