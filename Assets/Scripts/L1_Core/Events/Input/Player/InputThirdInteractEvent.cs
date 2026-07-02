using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputThirdInteractEvent", fileName = "InputThirdInteractEvent")]
    public sealed class InputThirdInteractEvent : GameEvent<SButtonInputPayload> { }
}
