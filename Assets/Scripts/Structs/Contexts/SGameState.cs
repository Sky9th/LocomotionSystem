using System;

/// <summary>
/// Immutable snapshot describing the current and previous game states. Used both
/// as a GameContext snapshot and as the payload when broadcasting state changes.
/// </summary>
[Serializable]
public struct SGameState
{
	public SGameState(EGameState currentState, EGameState previousState)
	{
		CurrentState = currentState;
		PreviousState = previousState;
	}

	public EGameState CurrentState { get; }
	public EGameState PreviousState { get; }
	public bool HasChanged => CurrentState != PreviousState;

	public static SGameState Default => new SGameState(EGameState.Initializing, EGameState.Initializing);
}
