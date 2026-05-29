using System.Collections.Generic;
using RedDust.GameState;
using RedDust.GameScene;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// Boots core subsystems (EventDispatcher, InputManager, CameraService, LocomotionManager) and exposes global access points.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public class GameService : MonoBehaviour
    {
        public static GameService Instance { get; private set; }

        [SerializeField] private GameContext gameContext;
        [SerializeField] private EventDispatcherService eventDispatcher;

        [SerializeField]
        private readonly List<BaseService> registeredServices = new();
        private bool isBootstrapped;
        private bool sessionWasActive;

        private LogChannel Log;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // UI time is independent of gameplay timeScale.
            // All DOTween tweens default to unscaled deltaTime.
            DG.Tweening.DOTween.defaultTimeScaleIndependent = true;

            Log = LogManager.GetChannel(nameof(GameService));
            Log.Info("Bootstrap sequence starting.");

            Bootstrap();
        }

        private void OnDestroy()
        {
            if (eventDispatcher != null)
                eventDispatcher.Unsubscribe<SGameState>(HandleSessionStateChange);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Bootstrap()
        {
            if (isBootstrapped)
            {
                Log.Debug("Bootstrap called but already bootstrapped. Skipping.");
                return;
            }

            gameContext = GetComponentInChildren<GameContext>();
            if (gameContext == null)
            {
                Debug.LogError("GameManager is missing a GameContext reference.", this);
                Log.Error("Missing GameContext reference.");
                return;
            }

            Log.Info("Step 1: Initializing GameContext.");
            gameContext.Initialize();
            registeredServices.Clear();

            Log.Info("Step 2: Discovering and registering services.");

            eventDispatcher = GetComponentInChildren<EventDispatcherService>();
            if (!RegisterService(eventDispatcher, "EventDispatcher"))
            {
                Debug.LogError("GameManager requires a valid EventDispatcher before continuing.", this);
                Log.Error("EventDispatcher registration failed.");
                return;
            }

            registeredServices.Add(eventDispatcher);
            eventDispatcher.Subscribe<SGameState>(HandleSessionStateChange);

            var registered = new List<string>();
            var failed = new List<string>();

            var discoveredServices = GetComponentsInChildren<BaseService>(includeInactive: true);
            foreach (var service in discoveredServices)
            {
                if (service == null || service == eventDispatcher) continue;

                if (RegisterService(service, service.GetType().Name))
                {
                    registeredServices.Add(service);
                    registered.Add(service.GetType().Name);
                }
                else
                {
                    failed.Add(service.GetType().Name);
                }
            }

            Log.Info($"Services registered: [{string.Join(", ", registered)}] ({registered.Count} total)");
            if (failed.Count > 0)
                Log.Warning($"Services failed: [{string.Join(", ", failed)}]");

            Log.Info($"Step 3: Attaching dispatcher, activating subscriptions, notifying {registeredServices.Count} services.");
            AttachDispatcherToServices();
            ActivateServiceSubscriptions();
            InitializeServices();

            Log.Info("Bootstrap completed.");
            isBootstrapped = true;

#if UNITY_EDITOR
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "Core")
                eventDispatcher.Publish(new SLoadSceneRequest(activeScene.name));
#endif
        }

        private bool RegisterService(BaseService service, string label)
        {
            if (service == null)
            {
                Log.Warning($"RegisterService skipped: '{label}' is null.");
                return false;
            }

            service.Register(gameContext);
            return service.IsRegistered;
        }

        private void AttachDispatcherToServices()
        {
            if (eventDispatcher == null || !eventDispatcher.IsRegistered)
            {
                Debug.LogError("Cannot attach dispatcher references before EventDispatcher finishes registering.", this);
                Log.Error("EventDispatcher not ready for AttachDispatcher.");
                return;
            }

            foreach (var service in registeredServices)
            {
                if (service != null)
                    service.AttachDispatcher(eventDispatcher);
            }
        }

        private void ActivateServiceSubscriptions()
        {
            foreach (var service in registeredServices)
            {
                if (service != null)
                    service.ActivateSubscriptions();
            }
        }

        private void InitializeServices()
        {
            foreach (var service in registeredServices)
            {
                if (service != null)
                    service.NotifyInitialized();
            }
        }

        private void HandleSessionStateChange(SGameState state, MetaStruct meta)
        {
            if (state.CurrentState == EGameState.Playing)
            {
                sessionWasActive = true;
                return;
            }

            if (state.CurrentState == EGameState.MainMenu && sessionWasActive)
            {
                sessionWasActive = false;
                TeardownSession();
            }
        }

        private void TeardownSession()
        {
            foreach (var service in registeredServices)
            {
                if (service is IGameplaySessionHandler handler)
                    handler.OnGameplaySessionEnd();
            }

            gameContext.ClearSnapshots();
        }

    }
}
