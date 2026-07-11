using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputAttackEvent", fileName = "InputAttackEvent")]
    public sealed class InputAttackEvent : GameEvent<SButtonInputPayload> { }
}
