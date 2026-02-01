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

    [SerializeField] private GameContext gameContext;
    [SerializeField] private EventDispatcher eventDispatcher;
    [SerializeField] private GameState gameState;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private LocomotionManager locomotionManager;
    [Header("Cursor Options")]
    [SerializeField] private bool lockCursorWhenPlaying = true;
    [SerializeField] private bool hideCursorWhenPlaying = true;
    [SerializeField] private bool showCursorInMenus = true;
    
    public GameContext Context => gameContext;
    public EventDispatcher Dispatcher => eventDispatcher;
    public GameState GameState => gameState;
    public InputManager Input => inputManager;
    public CameraManager Camera => cameraManager;
    public LocomotionManager Locomotion => locomotionManager;

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

        WireDependencies();
        Bootstrap();
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
    }

    private void Bootstrap()
    {
        if (isBootstrapped)
        {
            return;
        }

        if (gameContext == null)
        {
            Debug.LogError("GameManager is missing a GameContext reference.", this);
            return;
        }

        gameContext.Initialize();
        registeredServices.Clear();

        RegisterService(eventDispatcher, nameof(eventDispatcher));

        if (eventDispatcher == null || !eventDispatcher.IsRegistered)
        {
            Debug.LogError("GameManager requires a valid EventDispatcher before continuing.", this);
            return;
        }

        RegisterService(gameState, nameof(gameState));
        RegisterService(inputManager, nameof(inputManager));
        RegisterService(cameraManager, nameof(cameraManager));
        RegisterService(locomotionManager, nameof(locomotionManager));

        AttachDispatcherToServices();
        ActivateServiceSubscriptions();

        SubscribeDispatcherEvents();
        ApplyCursorMode(gameState != null ? gameState.CurrentState : EGameState.Initializing);
        
        isBootstrapped = true;
    }

    private void RegisterService(BaseService service, string label)
    {
        if (service == null)
        {
            Debug.LogWarning($"GameManager could not register service '{label}' because the reference is missing.", this);
            return;
        }

        service.Register(gameContext);

        if (service.IsRegistered && !registeredServices.Contains(service))
        {
            registeredServices.Add(service);
        }
    }

    private void AttachDispatcherToServices()
    {
        if (eventDispatcher == null || !eventDispatcher.IsRegistered)
        {
            Debug.LogError("Cannot attach dispatcher references before EventDispatcher finishes registering.", this);
            return;
        }

        foreach (var service in registeredServices)
        {
            service?.AttachDispatcher(eventDispatcher);
        }
    }

    private void ActivateServiceSubscriptions()
    {
        foreach (var service in registeredServices)
        {
            service?.ActivateSubscriptions();
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
        Logger.Log($"GameManager received SUIEscapeIAction: IsPressed={payload.IsPressed}");
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
