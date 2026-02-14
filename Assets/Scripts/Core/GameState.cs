using UnityEngine;

/// <summary>
/// Central authority for high-level game state transitions. Other systems request
/// state changes through this service so we can broadcast a unified payload and
/// keep GameContext snapshots in sync.
/// </summary>
[DisallowMultipleComponent]
public class GameState : BaseService
{
	[Header("State Options")]
	[SerializeField] private EGameState initialState = EGameState.Initializing;
	[SerializeField] private bool logTransitions;
	[Header("Cursor Options")]
	[SerializeField] private bool lockCursorWhenPlaying = true;

	private bool hasInitialized;
	[SerializeField] private EGameState currentState;
	[SerializeField] private EGameState previousState;

	public EGameState CurrentState => currentState;
	public EGameState PreviousState => previousState;
	public bool HasInitialized => hasInitialized;

	protected override bool OnRegister(GameContext context)
	{
		context.RegisterService(this);

		previousState = initialState;
		currentState = initialState;

		var snapshot = new SGameState(currentState, previousState);
		PushSnapshot(snapshot);

		if (logTransitions)
		{
			Debug.Log($"[GameState] Bootstrap at {currentState}.", this);
		}

		return true;
	}

	protected override void OnServicesReady()
	{
	}

	protected override void OnDispatcherAttached()
	{
		hasInitialized = true;
		ApplyState(currentState, force: true);
	}

	/// <summary>
	/// Attempts to switch into the requested state. No-op if already in that state.
	/// </summary>
	public bool RequestState(EGameState nextState)
	{
		return ApplyState(nextState, force: false);
	}

	/// <summary>
	/// Forces a state change even if the new state matches the current one.
	/// </summary>
	public void ForceState(EGameState nextState)
	{
		ApplyState(nextState, force: true);
	}

	private bool ApplyState(EGameState nextState, bool force)
	{
		if (!hasInitialized)
		{
			Debug.LogWarning("GameState has not finished registering; ignoring transition request.", this);
			return false;
		}

		if (!force && nextState == currentState)
		{
			return false;
		}

		previousState = currentState;
		currentState = nextState;

		ApplyCursorMode(currentState);

		var snapshot = new SGameState(currentState, previousState);
		Logger.Log($"GameState transitioning: {previousState} -> {currentState}");
		PushSnapshot(snapshot);
		PublishStateChange(snapshot);

		if (logTransitions)
		{
			Debug.Log($"[GameState] {previousState} -> {currentState}", this);
		}

		return true;
	}

	protected override void OnSubscriptionsActivated()
	{
		base.OnSubscriptionsActivated();
		if (Dispatcher != null)
		{
			Dispatcher.Subscribe<SUIEscapeIAction>(HandleEscapeIntent);
		}
	}

	private void OnDestroy()
	{
		if (Dispatcher != null)
		{
			Dispatcher.Unsubscribe<SUIEscapeIAction>(HandleEscapeIntent);
		}
	}

	private void HandleEscapeIntent(SUIEscapeIAction payload, MetaStruct meta)
	{
		if (!payload.IsPressed)
		{
			return;
		}

		switch (currentState)
		{
			case EGameState.MainMenu:
				RequestState(EGameState.Playing);
				break;
			case EGameState.Playing:
				RequestState(EGameState.MainMenu);
				break;
		}
	}

	private void ApplyCursorMode(EGameState state)
	{
		switch (state)
		{
			case EGameState.MainMenu:
			case EGameState.Paused:
				SetCursorVisibility(true, CursorLockMode.None);
				break;
			case EGameState.Playing:
				var targetLock = lockCursorWhenPlaying ? CursorLockMode.Locked : CursorLockMode.Confined;
				SetCursorVisibility(false, targetLock);
				break;
			default:
				SetCursorVisibility(true, CursorLockMode.None);
				break;
		}
	}

	private void SetCursorVisibility(bool isVisible, CursorLockMode lockMode)
	{
		Cursor.visible = isVisible;
		Cursor.lockState = lockMode;
	}

	private void PushSnapshot(SGameState snapshot)
	{
		GameContext?.UpdateSnapshot(snapshot);
	}

	private void PublishStateChange(SGameState snapshot)
	{
		Dispatcher.Publish(snapshot);
	}
}

public enum EGameState
{
	Initializing = 0,
	MainMenu = 10,
	Playing = 20,
	Paused = 30
}
