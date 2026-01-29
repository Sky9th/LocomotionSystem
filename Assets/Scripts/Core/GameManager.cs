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
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private LocomotionManager locomotionManager;

    public GameContext Context => gameContext;
    public EventDispatcher Dispatcher => eventDispatcher;
    public InputManager Input => inputManager;
    public CameraManager Camera => cameraManager;
    public LocomotionManager Locomotion => locomotionManager;

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

        RegisterService(eventDispatcher, nameof(eventDispatcher));
        RegisterService(inputManager, nameof(inputManager));
        RegisterService(cameraManager, nameof(cameraManager));
        RegisterService(locomotionManager, nameof(locomotionManager));

        isBootstrapped = true;
    }

    private void RegisterService(RuntimeServiceBase service, string label)
    {
        if (service == null)
        {
            Debug.LogWarning($"GameManager could not register service '{label}' because the reference is missing.", this);
            return;
        }

        service.Register(gameContext);
    }
}
