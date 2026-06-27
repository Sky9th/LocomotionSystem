using RedDust.Core;
using UnityEngine;
namespace RedDust.GameInput {
[CreateAssetMenu(menuName = "RedDust/Events/Input/Player/Look", fileName = "LookEventSO")]
public sealed class LookInputEventSO : GameEvent<SVector2InputPayload> { }
}
