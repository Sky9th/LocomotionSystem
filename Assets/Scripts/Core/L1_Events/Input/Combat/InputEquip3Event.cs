using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputEquip3Event", fileName = "InputEquip3Event")]
    public sealed class InputEquip3Event : GameEvent<SButtonInputPayload> { }
}
