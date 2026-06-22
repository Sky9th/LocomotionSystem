using RedDust.GameState;
using RedDust.GameScene;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// L1 root. Inherits ModuleHub — child L2 services are auto-discovered in Awake
    /// and follow the unified IModuleChild lifecycle (OnAssemble → OnWire).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public class GameService : ModuleHub
    {
        public static GameService Instance { get; private set; }

        private GameContext _gameContext;
        private EventDispatcherService _dispatcher; // TODO: 替换为 EventHub — EventDispatcher 即将废弃
        private bool _sessionWasActive;
        private LogChannel _log;

        // ── Unity lifecycle ──

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            DG.Tweening.DOTween.defaultTimeScaleIndependent = true;

            _log = LogManager.GetChannel(nameof(GameService));
            _log.Info("Bootstrap sequence starting (Module tree).");

            // Actively instantiate GameContext so it is ready before any child module.
            var go = new GameObject("GameContext");
            go.transform.SetParent(transform);
            _gameContext = go.AddComponent<GameContext>();
            _gameContext.Initialize();
            _log.Info("GameContext instantiated and initialized.");

            // ModuleHub.Awake discovers all ModuleChildMono children, then calls Registry.OnAssembleAll.
            base.Awake();
        }

        protected override void Start()
        {
            // All services self-registered during OnAssemble. Resolve Dispatcher, subscribe.
            if (_gameContext.TryResolveService(out EventDispatcherService dispatcher))
            {
                _dispatcher = dispatcher;
                _dispatcher.Subscribe<SGameState>(HandleSessionStateChange);
                _log.Info("EventDispatcher resolved; SGameState subscription active for Teardown priority.");
            }

            // Wire all child services.
            base.Start();

#if UNITY_EDITOR
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "Core")
                _dispatcher?.Publish(new SLoadSceneRequest(activeScene.name));
#endif
            _log.Info($"Bootstrap complete. {Registry.Count} services assembled and wired.");
        }

        private void OnDestroy()
        {
            if (_dispatcher != null)
                _dispatcher.Unsubscribe<SGameState>(HandleSessionStateChange);

            if (Instance == this)
                Instance = null;
        }

        // ── Session teardown ──

        private void HandleSessionStateChange(SGameState state, MetaStruct meta)
        {
            if (state.CurrentState == EGameState.Playing)
            {
                _sessionWasActive = true;
                return;
            }

            if (state.CurrentState == EGameState.MainMenu && _sessionWasActive)
            {
                _sessionWasActive = false;
                TeardownSession();
            }
        }

        private void TeardownSession()
        {
            var handlers = GetComponentsInChildren<IGameplaySessionHandler>(includeInactive: true);
            foreach (var handler in handlers)
            {
                if (handler != null)
                    handler.OnGameplaySessionEnd();
            }

            _gameContext.ClearSnapshots();
        }
    }
}
