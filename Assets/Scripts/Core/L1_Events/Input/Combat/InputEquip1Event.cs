using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputEquip1Event", fileName = "InputEquip1Event")]
    public sealed class InputEquip1Event : GameEvent<SButtonInputPayload> { }
}
