using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Player/InputThirdInteractEvent", fileName = "InputThirdInteractEvent")]
    public sealed class InputThirdInteractEvent : GameEvent<SButtonInputPayload> { }
}
