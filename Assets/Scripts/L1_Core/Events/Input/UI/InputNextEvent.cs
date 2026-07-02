using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/UI/InputNextEvent", fileName = "InputNextEvent")]
    public sealed class InputNextEvent : GameEvent<SButtonInputPayload> { }
}
