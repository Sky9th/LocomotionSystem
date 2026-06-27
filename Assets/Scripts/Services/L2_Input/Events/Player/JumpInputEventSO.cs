using RedDust.Core;
using UnityEngine;
namespace RedDust.GameInput {
[CreateAssetMenu(menuName = "RedDust/Events/Input/Player/Jump", fileName = "JumpEventSO")]
public sealed class JumpInputEventSO : GameEvent<SButtonInputPayload> { }
}
