using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputSkill1Event", fileName = "InputSkill1Event")]
    public sealed class InputSkill1Event : GameEvent<SButtonInputPayload> { }
}
