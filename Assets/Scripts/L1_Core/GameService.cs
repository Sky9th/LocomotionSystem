using RedDust.GameState;
using RedDust.GameScene;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// L1 root. Inherits ModuleBehaviour — child L2 services are auto-discovered and
    /// follow the unified IInitializable lifecycle (OnAssemble → OnWire).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public class GameService : ModuleBehaviour
    {
        public static GameService Instance { get; private set; }

        private GameContext _gameContext;
        private EventDispatcherService _dispatcher; // TODO: 替换为 EventHub — EventDispatcher 即将废弃
        private bool _sessionWasActive;
        private LogChannel _log;
        private int _wiredCount;

        /// <summary>Called by child services at the end of their OnWire.</summary>
        public void NotifyServiceWired()
        {
            _wiredCount++;
        }

        // ── Unity lifecycle ──

        private new void Awake()
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

            // ModuleBehaviour.Awake discovers all IInitializable children,
            // then calls OnAssemble → Registry.OnAssembleAll.
            base.Awake();
        }

        private void OnDestroy()
        {
            if (_dispatcher != null)
                _dispatcher.Unsubscribe<SGameState>(HandleSessionStateChange);

            if (Instance == this)
                Instance = null;
        }

        // ── IInitializable (Module tree) ──

        public override void OnAssemble()
        {
            // Actively instantiate GameContext so it is ready before any child module.
            var go = new GameObject("GameContext");
            go.transform.SetParent(transform);
            _gameContext = go.AddComponent<GameContext>();
            _gameContext.Initialize();

            _log.Info("GameContext instantiated and initialized.");
        }

        public override void OnWire()
        {
            // Register EventDispatcher first so it can be resolved by child services.
            var ed = GetComponentInChildren<EventDispatcherService>();
            if (ed != null)
            {
                _gameContext.RegisterService(ed);
                _dispatcher = ed;
                _dispatcher.Subscribe<SGameState>(HandleSessionStateChange);
                _log.Info("EventDispatcher registered; SGameState subscription active for Teardown priority.");
            }

            // Wire all child services first so their subscriptions are active.
            _wiredCount = 0;
            base.OnWire();

            // Now that all services are wired, safe to publish scene load.
#if UNITY_EDITOR
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "Core")
                _dispatcher?.Publish(new SLoadSceneRequest(activeScene.name));
#endif

            int expected = Registry.Count;
            if (_wiredCount == expected)
                _log.Info($"All {_wiredCount} services wired successfully.");
            else
                _log.Error($"Service wiring mismatch: {_wiredCount}/{expected} reported.");
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
