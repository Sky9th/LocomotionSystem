using RedDust.Core.Events;
using RedDust.Services.GameState;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/GameState/GameStateChangeRequest", fileName = "GameStateChangeRequestEvent")]
    public sealed class GameStateChangeRequestEvent : GameEvent<SGameStateRequest> { }
}
