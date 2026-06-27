using RedDust.Core;
using UnityEngine;
namespace RedDust.GameInput {
[CreateAssetMenu(menuName = "RedDust/Events/Input/Player/Move", fileName = "MoveEventSO")]
public sealed class MoveInputEventSO : GameEvent<SVector2InputPayload> { }
}
