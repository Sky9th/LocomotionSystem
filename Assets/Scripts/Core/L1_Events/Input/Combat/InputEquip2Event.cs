using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputEquip2Event", fileName = "InputEquip2Event")]
    public sealed class InputEquip2Event : GameEvent<SButtonInputPayload> { }
}
