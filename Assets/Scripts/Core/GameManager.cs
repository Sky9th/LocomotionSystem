using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boots core subsystems (EventDispatcher, InputManager, CameraManager, LocomotionManager) and exposes global access points.
/// </summary>
[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private GameContext gameContext;
    [SerializeField] private EventDispatcher eventDispatcher;
    [SerializeField] private GameState gameState;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private LocomotionManager locomotionManager;
    [SerializeField] private TimeScaleManager timeScaleService;
    [SerializeField] private UIManager uiService;
    [Header("Cursor Options")]
    [SerializeField] private bool lockCursorWhenPlaying = true;
    
    public GameContext Context => gameContext;
    public EventDispatcher Dispatcher => eventDispatcher;
    public GameState GameState => gameState;
    public InputManager Input => inputManager;
    public CameraManager Camera => cameraManager;
    public LocomotionManager Locomotion => locomotionManager;
    public TimeScaleManager TimeScale => timeScaleService;
    public UIManager UI => uiService;

    private readonly List<BaseService> registeredServices = new();
    private bool isBootstrapped;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Logger.Log("GameManager Awake: starting bootstrap sequence.", nameof(GameManager), this);

        WireDependencies();
        Bootstrap();

        // Not common service functionality below, but GameManager needs to respond to game state changes to manage cursor state, so we subscribe to the event here.
        SubscribeDispatcherEvents();
        ApplyCursorMode(gameState != null ? gameState.CurrentState : EGameState.Initializing);
        CreatePlayer();
    }

    private void CreatePlayer()
    {
        if (PlayerPrefab == null)
        {
            Debug.LogError("PlayerPrefab reference is missing in GameManager.", this);
            return;
        }

        GameObject playerInstance = Instantiate(PlayerPrefab);
        playerInstance.name = PlayerPrefab.name;
    }

    private void OnDestroy()
    {
        UnsubscribeDispatcherEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void WireDependencies()
    {
        if (eventDispatcher == null)
        {
            eventDispatcher = GetComponentInChildren<EventDispatcher>();
        }

        if (inputManager == null)
        {
            inputManager = GetComponentInChildren<InputManager>();
        }

        if (gameState == null)
        {
            gameState = GetComponentInChildren<GameState>();
        }

        if (gameContext == null)
        {
            gameContext = GetComponentInChildren<GameContext>();
        }

        if (cameraManager == null)
        {
            cameraManager = GetComponentInChildren<CameraManager>();
        }

        if (locomotionManager == null)
        {
            locomotionManager = GetComponentInChildren<LocomotionManager>();
        }

        if (timeScaleService == null)
        {
            timeScaleService = GetComponentInChildren<TimeScaleManager>();
        }

        if (uiService == null)
        {
            uiService = GetComponentInChildren<UIManager>();
        }
    }

    private void Bootstrap()
    {
        if (isBootstrapped)
        {
            Logger.Log("GameManager.Bootstrap called but already bootstrapped. Skipping.", nameof(GameManager), this);
            return;
        }

        if (gameContext == null)
        {
            Debug.LogError("GameManager is missing a GameContext reference.", this);
            Logger.LogError("GameManager is missing a GameContext reference.", nameof(GameManager), this);
            return;
        }

        Logger.Log("Bootstrap Step 1: Initializing GameContext.", nameof(GameManager), this);
        gameContext.Initialize();
        Logger.Log($"GameContext after Initialize. IsInitialized={gameContext.IsInitialized}, RegisteredServiceCount={gameContext.RegisteredServiceCount}", nameof(GameManager), this);
        registeredServices.Clear();

        Logger.Log("Bootstrap Step 2: Registering core services.", nameof(GameManager), this);
        RegisterService(eventDispatcher, nameof(eventDispatcher));

        if (eventDispatcher == null || !eventDispatcher.IsRegistered)
        {
            Debug.LogError("GameManager requires a valid EventDispatcher before continuing.", this);
            Logger.LogError("GameManager requires a valid EventDispatcher before continuing.", nameof(GameManager), this);
            return;
        }

        RegisterService(gameState, nameof(gameState));
        RegisterService(inputManager, nameof(inputManager));
        RegisterService(cameraManager, nameof(cameraManager));
        RegisterService(locomotionManager, nameof(locomotionManager));
        RegisterService(timeScaleService, nameof(timeScaleService));
        RegisterService(uiService, nameof(uiService));

        Logger.Log($"Bootstrap Step 3: Attaching dispatcher and activating {registeredServices.Count} registered services.", nameof(GameManager), this);
        AttachDispatcherToServices();
        ActivateServiceSubscriptions();
        InitializeServices();

        Logger.Log($"GameManager bootstrap completed. RegisteredServices={registeredServices.Count}", nameof(GameManager), this);
        isBootstrapped = true;
    }

    private void RegisterService(BaseService service, string label)
    {
        if (service == null)
        {
            Debug.LogWarning($"GameManager could not register service '{label}' because the reference is missing.", this);
            Logger.LogWarning($"RegisterService skipped: '{label}' is null.", nameof(GameManager), this);
            return;
        }

        Logger.Log($"RegisterService starting for '{label}' ({service.GetType().Name}).", nameof(GameManager), service);
        service.Register(gameContext);

        if (service.IsRegistered && !registeredServices.Contains(service))
        {
            registeredServices.Add(service);
            Logger.Log($"RegisterService succeeded for '{label}' ({service.GetType().Name}). IsRegistered={service.IsRegistered}", nameof(GameManager), service);
        }
        else
        {
            Logger.LogWarning($"RegisterService did not complete for '{label}' ({service.GetType().Name}). IsRegistered={service.IsRegistered}", nameof(GameManager), service);
        }
    }

    private void AttachDispatcherToServices()
    {
        if (eventDispatcher == null || !eventDispatcher.IsRegistered)
        {
            Debug.LogError("Cannot attach dispatcher references before EventDispatcher finishes registering.", this);
            Logger.LogError("Cannot attach dispatcher references before EventDispatcher finishes registering.", nameof(GameManager), this);
            return;
        }

        Logger.Log($"Attaching EventDispatcher to {registeredServices.Count} services.", nameof(GameManager), this);
        foreach (var service in registeredServices)
        {
            if (service == null)
            {
                continue;
            }

            Logger.Log($"AttachDispatcher -> {service.GetType().Name}", nameof(GameManager), service);
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

            Logger.Log($"ActivateSubscriptions -> {service.GetType().Name}", nameof(GameManager), service);
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

            Logger.Log($"NotifyInitialized -> {service.GetType().Name}", nameof(GameManager), service);
            service.NotifyInitialized();
        }
    }

    private void SubscribeDispatcherEvents()
    {
        if (eventDispatcher == null)
        {
            Debug.LogWarning("Cannot subscribe to dispatcher events without a valid EventDispatcher reference.", this);
            return;
        }

        eventDispatcher.Subscribe<SUIEscapeIAction>(HandleEscapeIntent);
    }

    private void UnsubscribeDispatcherEvents()
    {
        if (eventDispatcher == null)
        {
            return;
        }

        eventDispatcher.Unsubscribe<SUIEscapeIAction>(HandleEscapeIntent);
    }

    private void HandleEscapeIntent(SUIEscapeIAction payload, MetaStruct meta)
    {
        if (!payload.IsPressed || gameState == null)
        {
            return;
        }

        switch (gameState.CurrentState)
        {
            case EGameState.MainMenu:
                gameState.RequestState(EGameState.Playing);
                break;
            case EGameState.Playing:
                gameState.RequestState(EGameState.MainMenu);
                break;
        }
        ApplyCursorMode(gameState != null ? gameState.CurrentState : EGameState.Initializing);
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
}
