using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputSecondaryInteractEvent", fileName = "InputSecondaryInteractEvent")]
    public sealed class InputSecondaryInteractEvent : GameEvent<SButtonInputPayload> { }
}
