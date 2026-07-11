using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputAttackEvent", fileName = "InputAttackEvent")]
    public sealed class InputAttackEvent : GameEvent<SButtonInputPayload> { }
}
