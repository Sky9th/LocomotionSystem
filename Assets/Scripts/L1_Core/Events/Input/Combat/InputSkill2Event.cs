using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputSkill2Event", fileName = "InputSkill2Event")]
    public sealed class InputSkill2Event : GameEvent<SButtonInputPayload> { }
}
