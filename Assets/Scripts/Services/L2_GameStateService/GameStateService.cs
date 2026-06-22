using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;
using RedDust.Shared;

namespace RedDust.GameState
{
	/// <summary>
	/// Central authority for high-level game state transitions. Other systems request
	/// state changes through this service so we can broadcast a unified payload and
	/// keep GameContext snapshots in sync.
	/// </summary>
	[DisallowMultipleComponent]
	public class GameStateService : ModuleChildMono
	{
		[Header("State Options")]
		[SerializeField] private EGameState initialState = EGameState.MainMenu;
		[SerializeField] private bool logTransitions;
		private bool hasInitialized;
		[SerializeField] private EGameState currentState;
		[SerializeField] private EGameState previousState;
		[SerializeField] private EscapeInputEventSO escapeEvent;
		private EventDispatcherService _dispatcher; // TODO: 替换为 EventHub — EventDispatcher 即将废弃
		private LogChannel _log;

		public EGameState CurrentState => currentState;
		public EGameState PreviousState => previousState;
		public bool HasInitialized => hasInitialized;

		private void Update()
		{
		}

		public override void OnAssemble()
		{
			_log = LogManager.GetChannel(GetType().Name);

			var state = initialState;
#if UNITY_EDITOR
			if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Core")
				state = EGameState.Playing;
#endif

			previousState = state;
			currentState = state;


			if (logTransitions)
			{
				Debug.Log($"[GameState] Bootstrap at {currentState}.", this);
			}

			GameContext.Instance.RegisterService(this);
		}

		public override void OnWire()
		{
			GameContext.Instance.TryResolveService(out _dispatcher);
			hasInitialized = true;

			if (_dispatcher != null)
			{
				_dispatcher.Subscribe<SGameStateRequest>(HandleStateRequest);
			}

			if (escapeEvent != null) escapeEvent.OnRaised += OnEscape;
		else Debug.LogWarning($"[GameState] escapeEvent not assigned — Esc key will not toggle Pause.", this);

		}

		private void Start()
		{
			ApplyState(currentState, force: true);
		}

		public bool RequestState(EGameState nextState)
		{
			return ApplyState(nextState, force: false);
		}

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
			GameContext.Instance.UpdateSnapshot(snapshot);
			_dispatcher?.Publish(snapshot);

			if (logTransitions)
				Debug.Log($"[GameState] {previousState} -> {currentState}", this);

			return true;
		}

		private void OnDestroy()
		{
		if (escapeEvent != null) escapeEvent.OnRaised -= OnEscape;
			if (_dispatcher != null)
			{
				_dispatcher.Unsubscribe<SGameStateRequest>(HandleStateRequest);
			}
		}

		private void HandleStateRequest(SGameStateRequest evt, MetaStruct _)
		{
			RequestState(evt.TargetState);
		}

		private void OnEscape()
		{
			if (!escapeEvent.IsRequested)
				return;

			switch (currentState)
			{
				case EGameState.Playing:
					RequestState(EGameState.Paused);
					break;
				case EGameState.Paused:
					RequestState(EGameState.Playing);
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
					SetCursorVisibility(true, CursorLockMode.Confined);
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

	}

	public enum EGameState
	{
		Initializing = 0,
		MainMenu = 10,
		Playing = 20,
		Paused = 30
	}
}
