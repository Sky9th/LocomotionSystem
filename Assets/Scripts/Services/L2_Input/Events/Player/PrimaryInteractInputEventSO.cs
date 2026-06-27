using RedDust.Core;
using UnityEngine;
namespace RedDust.GameInput {
[CreateAssetMenu(menuName = "RedDust/Events/Input/Player/PrimaryInteract", fileName = "PrimaryInteractEventSO")]
public sealed class PrimaryInteractInputEventSO : GameEvent<SButtonInputPayload> { }
}
