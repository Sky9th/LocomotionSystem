using RedDust.Core.Events;
using RedDust.Assets;
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

        /// <summary>Centralized asset catalog. Populated by AssetService during boot, queried by all services.</summary>
        public AssetCatalog Assets { get; private set; }

        private GameContext _gameContext;
        private EventHub _eventHub;
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
            Assets = new AssetCatalog();

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
            // 服务级 EventHub 显式注册，角色级不碰 GameContext
            _eventHub = GetComponentInChildren<EventHub>();
            if (_eventHub != null)
            {
                _gameContext.RegisterService(_eventHub);
                _eventHub.Get<GameStateChangedEvent>().Register(HandleSessionStateChange);
            }

            // Wire all child services.
            base.Start();

            // All services wired — trigger first-scene load.
            if (_gameContext.TryResolveService(out SceneService sceneService))
                sceneService.Load();

            _log.Info($"Bootstrap complete. {Registry.Count} services assembled and wired.");
        }

        private void OnDestroy()
        {
            if (_eventHub != null)
                _eventHub.Get<GameStateChangedEvent>().Unregister(HandleSessionStateChange);

            if (Instance == this)
                Instance = null;
        }

        // ── Session teardown ──

        private void HandleSessionStateChange(SGameState state)
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
