using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputPrimaryInteractEvent", fileName = "InputPrimaryInteractEvent")]
    public sealed class InputPrimaryInteractEvent : GameEvent<SButtonInputPayload> { }
}
