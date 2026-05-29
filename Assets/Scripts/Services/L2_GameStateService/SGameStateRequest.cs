namespace RedDust.GameState
{
    /// <summary>
    /// Event payload requesting a game state transition.
    /// GameStateService subscribes and applies the transition through its normal state machine.
    /// </summary>
    public readonly struct SGameStateRequest
    {
        public readonly EGameState TargetState;

        public SGameStateRequest(EGameState targetState)
        {
            TargetState = targetState;
        }
    }
}
