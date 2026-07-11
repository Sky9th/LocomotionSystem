using RedDust.Core;
using RedDust.GameState;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/GameState/GameStateChangeRequest", fileName = "GameStateChangeRequestEvent")]
    public sealed class GameStateChangeRequestEvent : GameEvent<SGameStateRequest> { }
}
