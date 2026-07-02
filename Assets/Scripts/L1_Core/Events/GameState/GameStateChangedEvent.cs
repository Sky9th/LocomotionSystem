using RedDust.Core;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/GameState/GameStateChanged", fileName = "GameStateChangedEvent")]
    public sealed class GameStateChangedEvent : GameEvent<SGameState> { }
}
