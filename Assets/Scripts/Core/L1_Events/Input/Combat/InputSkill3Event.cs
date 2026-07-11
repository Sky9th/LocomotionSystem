using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputSkill3Event", fileName = "InputSkill3Event")]
    public sealed class InputSkill3Event : GameEvent<SButtonInputPayload> { }
}
