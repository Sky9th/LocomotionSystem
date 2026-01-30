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
    [Header("Rig References")]
    [SerializeField] private Transform followTarget;

    [Header("Motion Settings")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0f)] private float acceleration = 20f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugVectors;
    [SerializeField, Min(0.1f)] private float debugForwardLength = 2f;

    private bool isRegistered;
    private PlayerLocomotionStruct latestSnapshot = PlayerLocomotionStruct.Default;
    private PlayerMoveIntentStruct lastMoveIntent = PlayerMoveIntentStruct.None;
    private MoveIntentHandler moveIntentHandler;
    private Vector3 currentVelocity;
    private GroundContactStruct lastGroundContact = GroundContactStruct.None;
    private Vector3 followForward = Vector3.forward;

    public bool IsPlayer => markAsPlayer;
    public bool IsRegistered => isRegistered;
    public PlayerLocomotionStruct Snapshot => latestSnapshot;
    public PlayerMoveIntentStruct LastMoveIntent => lastMoveIntent;
    public Vector3 FollowForward => followForward;

    private void Awake()
    {
        if (manager == null)
        {
            manager = FindManagerInScene();
        }
        ResolveFollowTarget();
        UpdateFollowForward();
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

    private void Update()
    {
        if (!isRegistered)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            return;
        }

        UpdateFollowForward();
        DrawDebugVectors();
        SimulateLocomotion(deltaTime);
    }

    private void OnDisable()
    {
        if (manager != null && isRegistered)
        {
            manager.UnregisterComponent(this);
            isRegistered = false;
        }

        lastMoveIntent = PlayerMoveIntentStruct.None;
        currentVelocity = Vector3.zero;
        lastGroundContact = GroundContactStruct.None;
        followForward = transform.forward;
        UnregisterIntentHandlers();
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
            RegisterIntentHandlers();
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


    private void RegisterIntentHandlers()
    {
        if (subscribePlayerMoveIntent)
        {
            moveIntentHandler ??= new MoveIntentHandler(this);
            moveIntentHandler.Subscribe();
        }
    }

    private void UnregisterIntentHandlers()
    {
        moveIntentHandler?.Unsubscribe();
    }

    private void SimulateLocomotion(float deltaTime)
    {
        Vector3 desiredVelocity = CalculateDesiredVelocity();
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * deltaTime);

        PlayerLocomotionState state = currentVelocity.sqrMagnitude > Mathf.Epsilon
            ? PlayerLocomotionState.Walk
            : PlayerLocomotionState.Idle;
        Vector3 forward = followForward;

        lastGroundContact = new GroundContactStruct(true, transform.position, Vector3.up);

        PlayerLocomotionStruct snapshot = new PlayerLocomotionStruct(
            transform.position,
            currentVelocity,
            forward,
            state,
            lastGroundContact);

        PushSnapshot(snapshot);
    }

    private Vector3 CalculateDesiredVelocity()
    {
        if (lastMoveIntent.HasInput)
        {
            return lastMoveIntent.WorldDirection * moveSpeed;
        }

        return Vector3.zero;
    }

    private void ResolveFollowTarget()
    {
        if (followTarget != null)
        {
            return;
        }

        var follow = transform.Find("Follow");
        if (follow != null)
        {
            followTarget = follow;
        }
    }

    private void UpdateFollowForward()
    {
        Vector3 forwardSource;
        if (followTarget != null)
        {
            forwardSource = followTarget.forward;
        }
        else
        {
            forwardSource = transform.forward;
        }

        forwardSource.y = 0f;
        if (forwardSource.sqrMagnitude <= Mathf.Epsilon)
        {
            forwardSource = transform.forward;
            forwardSource.y = 0f;
        }

        followForward = forwardSource.sqrMagnitude > Mathf.Epsilon
            ? forwardSource.normalized
            : Vector3.forward;
    }

    private void DrawDebugVectors()
    {
        if (!drawDebugVectors)
        {
            return;
        }

        Debug.DrawRay(transform.position, followForward * debugForwardLength, Color.cyan);
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
