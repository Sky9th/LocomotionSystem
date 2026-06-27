using RedDust.Core;
using UnityEngine;
namespace RedDust.GameInput {
[CreateAssetMenu(menuName = "RedDust/Events/Input/Player/Walk", fileName = "WalkEventSO")]
public sealed class WalkInputEventSO : GameEvent<SButtonInputPayload> { }
}
