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

    [SerializeField]
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

        Bootstrap();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Bootstrap()
    {
        if (isBootstrapped)
        {
            Logger.Log("GameManager.Bootstrap called but already bootstrapped. Skipping.", nameof(GameManager), this);
            return;
        }

        gameContext = GetComponentInChildren<GameContext>();
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

        Logger.Log("Bootstrap Step 2: Discovering and registering services.", nameof(GameManager), this);

        eventDispatcher = GetComponentInChildren<EventDispatcher>();
        // Ensure the EventDispatcher is registered first since other services depend on it.
        if (!RegisterService(eventDispatcher, nameof(eventDispatcher)))
        {
            Debug.LogError("GameManager requires a valid EventDispatcher before continuing.", this);
            Logger.LogError("GameManager requires a valid EventDispatcher before continuing.", nameof(GameManager), this);
            return;
        }

        registeredServices.Add(eventDispatcher);

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

        Logger.Log($"Bootstrap Step 3: Attaching dispatcher and activating {registeredServices.Count} registered services.", nameof(GameManager), this);
        AttachDispatcherToServices();
        ActivateServiceSubscriptions();
        InitializeServices();

        Logger.Log($"GameManager bootstrap completed. RegisteredServices={registeredServices.Count}", nameof(GameManager), this);
        isBootstrapped = true;
    }

    private bool RegisterService(BaseService service, string label)
    {
        if (service == null)
        {
            Debug.LogWarning($"GameManager could not register service '{label}' because the reference is missing.", this);
            Logger.LogWarning($"RegisterService skipped: '{label}' is null.", nameof(GameManager), this);
            return false;
        }

        Logger.Log($"RegisterService starting for '{label}' ({service.GetType().Name}).", nameof(GameManager), service);
        service.Register(gameContext);

        if (service.IsRegistered)
        {
            Logger.Log($"RegisterService succeeded for '{label}' ({service.GetType().Name}). IsRegistered={service.IsRegistered}", nameof(GameManager), service);
            return true;
        }
        else
        {
            Logger.LogWarning($"RegisterService did not complete for '{label}' ({service.GetType().Name}). IsRegistered={service.IsRegistered}", nameof(GameManager), service);
            return false;
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

}
