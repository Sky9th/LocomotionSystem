using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputEquip1Event", fileName = "InputEquip1Event")]
    public sealed class InputEquip1Event : GameEvent<SButtonInputPayload> { }
}
