using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private GameStateService gameState;

    [SerializeField]
    private readonly List<BaseService> registeredServices = new();
    private bool isBootstrapped;
    private bool sessionWasActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Logger.Log("GameManager Awake: starting bootstrap sequence.", nameof(GameService), this);

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
            Logger.Log("GameManager.Bootstrap called but already bootstrapped. Skipping.", nameof(GameService), this);
            return;
        }

        gameContext = GetComponentInChildren<GameContext>();
        if (gameContext == null)
        {
            Debug.LogError("GameManager is missing a GameContext reference.", this);
            Logger.LogError("GameManager is missing a GameContext reference.", nameof(GameService), this);
            return;
        }

        Logger.Log("Bootstrap Step 1: Initializing GameContext.", nameof(GameService), this);
        gameContext.Initialize();
        Logger.Log($"GameContext after Initialize. IsInitialized={gameContext.IsInitialized}, RegisteredServiceCount={gameContext.RegisteredServiceCount}", nameof(GameService), this);
        registeredServices.Clear();

        Logger.Log("Bootstrap Step 2: Discovering and registering services.", nameof(GameService), this);

        eventDispatcher = GetComponentInChildren<EventDispatcherService>();
        // Ensure the EventDispatcher is registered first since other services depend on it.
        if (!RegisterService(eventDispatcher, nameof(eventDispatcher)))
        {
            Debug.LogError("GameManager requires a valid EventDispatcher before continuing.", this);
            Logger.LogError("GameManager requires a valid EventDispatcher before continuing.", nameof(GameService), this);
            return;
        }

        registeredServices.Add(eventDispatcher);

        // Subscribe before other services so TeardownSession runs first
        // when SGameState is published synchronously.
        eventDispatcher.Subscribe<SGameState>(HandleSessionStateChange);

        // Automatically discover and register all BaseService instances under this GameManager,
        // so new services can be added without updating this bootstrap code.
        var discoveredServices = GetComponentsInChildren<BaseService>(includeInactive: true);
        foreach (var service in discoveredServices)
        {
            if (service == null || service == eventDispatcher)
            {
                continue;
            }

            // Use the component name as the label to keep logs readable.
            if (RegisterService(service, service.name))
            {
                registeredServices.Add(service);
            }
        }

        Logger.Log($"Bootstrap Step 3: Attaching dispatcher and activating {registeredServices.Count} registered services.", nameof(GameService), this);
        AttachDispatcherToServices();
        ActivateServiceSubscriptions();
        InitializeServices();

        Logger.Log($"GameManager bootstrap completed. RegisteredServices={registeredServices.Count}", nameof(GameService), this);
        isBootstrapped = true;

#if UNITY_EDITOR
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.name != "Core")
        {
            var sceneService = GetComponentInChildren<SceneService>();
            if (sceneService != null)
                sceneService.SetCurrentContentScene(activeScene.name);

            var coreScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Core");
            if (coreScene.isLoaded)
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(coreScene);

            eventDispatcher.Publish(new SSceneLoadComplete(activeScene.name, null));
        }
#endif
    }

    private bool RegisterService(BaseService service, string label)
    {
        if (service == null)
        {
            Debug.LogWarning($"GameManager could not register service '{label}' because the reference is missing.", this);
            Logger.LogWarning($"RegisterService skipped: '{label}' is null.", nameof(GameService), this);
            return false;
        }

        Logger.Log($"RegisterService starting for '{label}' ({service.GetType().Name}).", nameof(GameService), service);
        service.Register(gameContext);

        if (service.IsRegistered)
        {
            Logger.Log($"RegisterService succeeded for '{label}' ({service.GetType().Name}). IsRegistered={service.IsRegistered}", nameof(GameService), service);
            return true;
        }
        else
        {
            Logger.LogWarning($"RegisterService did not complete for '{label}' ({service.GetType().Name}). IsRegistered={service.IsRegistered}", nameof(GameService), service);
            return false;
        }
    }

    private void AttachDispatcherToServices()
    {
        if (eventDispatcher == null || !eventDispatcher.IsRegistered)
        {
            Debug.LogError("Cannot attach dispatcher references before EventDispatcher finishes registering.", this);
            Logger.LogError("Cannot attach dispatcher references before EventDispatcher finishes registering.", nameof(GameService), this);
            return;
        }

        Logger.Log($"Attaching EventDispatcher to {registeredServices.Count} services.", nameof(GameService), this);
        foreach (var service in registeredServices)
        {
            if (service == null)
            {
                continue;
            }

            Logger.Log($"AttachDispatcher -> {service.GetType().Name}", nameof(GameService), service);
            service.AttachDispatcher(eventDispatcher);
        }
    }

    private void ActivateServiceSubscriptions()
    {
        foreach (var service in registeredServices)
        {
            if (service == null)
            {
                continue;
            }

            Logger.Log($"ActivateSubscriptions -> {service.GetType().Name}", nameof(GameService), service);
            service.ActivateSubscriptions();
        }
    }

    private void InitializeServices()
    {
        foreach (var service in registeredServices)
        {
            if (service == null)
            {
                continue;
            }

            Logger.Log($"NotifyInitialized -> {service.GetType().Name}", nameof(GameService), service);
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
