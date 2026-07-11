using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/UI/InputNextEvent", fileName = "InputNextEvent")]
    public sealed class InputNextEvent : GameEvent<SButtonInputPayload> { }
}
