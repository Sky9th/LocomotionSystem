using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/System/InputTimeResumeEvent", fileName = "InputTimeResumeEvent")]
    public sealed class InputTimeResumeEvent : GameEvent<SButtonInputPayload> { }
}
