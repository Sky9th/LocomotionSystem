using UnityEngine;

/// <summary>
/// 挂载在角色上的 Locomotion 组件。负责向 LocomotionManager 注册并在后续版本中执行具体运动逻辑。
/// </summary>
[DisallowMultipleComponent]
public class LocomotionAgent : MonoBehaviour
{
    [Header("Registration")]
    [SerializeField] private LocomotionManager manager;
    [SerializeField] private bool autoRegister = true;
    [SerializeField] private bool markAsPlayer;
    [SerializeField] private bool subscribePlayerMoveIntent = true;

    private bool isRegistered;
    private PlayerLocomotionStruct latestSnapshot = PlayerLocomotionStruct.Default;
    private PlayerMoveIntentStruct lastMoveIntent = PlayerMoveIntentStruct.None;
    private EventDispatcher eventDispatcher;
    private bool isSubscribedToMoveIntent;

    public bool IsPlayer => markAsPlayer;
    public bool IsRegistered => isRegistered;
    public PlayerLocomotionStruct Snapshot => latestSnapshot;

    private void Awake()
    {
        if (manager == null)
        {
            manager = FindManagerInScene();
        }
    }

    private void OnEnable()
    {
        // FixedUpdate handles retrying registration each frame.
    }

    private void FixedUpdate()
    {
        if (!autoRegister || isRegistered)
        {
            return;
        }

        TryRegisterWithManager();
    }

    private void OnDisable()
    {
        if (manager != null && isRegistered)
        {
            manager.UnregisterComponent(this);
            isRegistered = false;
        }

        lastMoveIntent = PlayerMoveIntentStruct.None;
        UnsubscribePlayerMoveIntent();
    }

    public bool TryRegisterWithManager()
    {
        if (isRegistered)
        {
            return true;
        }

        if (manager == null)
        {
            manager = FindManagerInScene();
        }

        if (manager == null || !manager.IsRegistered)
        {
            return false;
        }

        if (manager.RegisterComponent(this))
        {
            isRegistered = true;
            SubscribePlayerMoveIntent();
            return true;
        }

        return false;
    }

    public void PushSnapshot(PlayerLocomotionStruct snapshot)
    {
        latestSnapshot = snapshot;
        manager?.PublishSnapshot(this, snapshot);
    }

    internal void BufferPlayerMoveIntent(PlayerMoveIntentStruct intent)
    {
        lastMoveIntent = intent;
    }


    private void SubscribePlayerMoveIntent()
    {
        if (!subscribePlayerMoveIntent || isSubscribedToMoveIntent)
        {
            return;
        }

        if (GameContext.Instance != null && GameContext.Instance.TryResolveService(out EventDispatcher dispatcher))
        {
            eventDispatcher = dispatcher;
            eventDispatcher.Subscribe<PlayerMoveIntentStruct>(OnPlayerMoveIntent);
            isSubscribedToMoveIntent = true;
        }
    }

    private void OnPlayerMoveIntent(PlayerMoveIntentStruct intent, MetaStruct meta)
    {
        if (!isRegistered || !subscribePlayerMoveIntent)
        {
            return;
        }

        BufferPlayerMoveIntent(intent);
    }

    private void UnsubscribePlayerMoveIntent()
    {
        if (!isSubscribedToMoveIntent || eventDispatcher == null)
        {
            return;
        }

        eventDispatcher.Unsubscribe<PlayerMoveIntentStruct>(OnPlayerMoveIntent);
        isSubscribedToMoveIntent = false;
    }


    private LocomotionManager FindManagerInScene()
    {
        if (GameContext.Instance != null && GameContext.Instance.TryResolveService(out LocomotionManager resolved))
        {
            return resolved;
        }

        return FindObjectOfType<LocomotionManager>();
    }
}
