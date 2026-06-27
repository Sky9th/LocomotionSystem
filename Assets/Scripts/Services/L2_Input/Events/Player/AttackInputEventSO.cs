using RedDust.Core;
using UnityEngine;
namespace RedDust.GameInput {
[CreateAssetMenu(menuName = "RedDust/Events/Input/Player/Attack", fileName = "AttackEventSO")]
public sealed class AttackInputEventSO : GameEvent<SButtonInputPayload> { }
}
