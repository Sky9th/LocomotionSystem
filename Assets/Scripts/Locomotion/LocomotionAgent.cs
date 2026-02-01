using System.Collections.Generic;
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
    [SerializeField] private bool subscribePlayerMoveAction = true;
    [SerializeField] private bool subscribePlayerLookAction = true;
    [Header("Rig References")]
    [SerializeField] private Transform followTarget;

    [Header("Motion Settings")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0f)] private float acceleration = 20f;

    [Header("Head Look Limits")]
    [SerializeField, Range(0f, 90f)] private float maxHeadYawDegrees = 75f;
    [SerializeField, Range(0f, 90f)] private float maxHeadPitchDegrees = 75f;
    [SerializeField, Min(0f)] private float headLookSmoothingSpeed = 540f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugVectors;
    [SerializeField, Min(0.1f)] private float debugForwardLength = 2f;

    private bool isRegistered;
    private SPlayerLocomotion latestSnapshot = SPlayerLocomotion.Default;
    private readonly Dictionary<System.Type, object> iActionBuffer = new();
    private MoveActionHandler moveActionHandler;
    private LookActionHandler lookActionHandler;
    private Vector3 currentVelocity;
    private SGroundContact lastGroundContact = SGroundContact.None;
    private Vector3 forwardDirection = Vector3.forward;
    private Vector2 lookDirection = Vector2.zero;

    public bool IsPlayer => markAsPlayer;
    public bool IsRegistered => isRegistered;
    public SPlayerLocomotion Snapshot => latestSnapshot;
    public SPlayerMoveIAction LastMoveAction => TryGetIAction(out SPlayerMoveIAction action) ? action : SPlayerMoveIAction.None;
    public Vector3 ForwardDirection => forwardDirection;
    public Vector2 LookDirection => lookDirection;
    public float HeadLookSmoothingSpeed => headLookSmoothingSpeed;

    private void Awake()
    {
        if (manager == null)
        {
            manager = FindManagerInScene();
        }
        ResolveFollowTarget();
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

        UpdateForwardDirection();
        SimulateLocomotion(deltaTime);
        DrawDebugVectors();
    }

    private void OnDisable()
    {
        if (manager != null && isRegistered)
        {
            manager.UnregisterComponent(this);
            isRegistered = false;
        }

        currentVelocity = Vector3.zero;
        lastGroundContact = SGroundContact.None;
        forwardDirection = transform.forward;
        iActionBuffer.Clear();
        UnregisterActionHandlers();
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
            RegisterActionHandlers();
            return true;
        }

        return false;
    }

    public void PushSnapshot(SPlayerLocomotion snapshot)
    {
        latestSnapshot = snapshot;
        manager?.PublishSnapshot(this, snapshot);
    }

    internal void BufferIAction<TAction>(TAction action) where TAction : struct
    {
        iActionBuffer[typeof(TAction)] = action;
    }

    public bool TryGetIAction<TAction>(out TAction action) where TAction : struct
    {
        if (iActionBuffer.TryGetValue(typeof(TAction), out object boxed) && boxed is TAction typed)
        {
            action = typed;
            return true;
        }

        action = default;
        return false;
    }

    private void RegisterActionHandlers()
    {
        if (subscribePlayerMoveAction)
        {
            moveActionHandler ??= new MoveActionHandler(this);
            moveActionHandler.Subscribe();
        }

        if (subscribePlayerLookAction)
        {
            lookActionHandler ??= new LookActionHandler(this);
            lookActionHandler.Subscribe();
        }
    }

    private void UnregisterActionHandlers()
    {
        moveActionHandler?.Unsubscribe();
        lookActionHandler?.Unsubscribe();
    }

    private void SimulateLocomotion(float deltaTime)
    {
        Vector3 desiredVelocity = CalculateDesiredVelocity();
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * deltaTime);

        ELocomotionState state = currentVelocity.sqrMagnitude > Mathf.Epsilon
            ? ELocomotionState.Walk
            : ELocomotionState.Idle;

        lastGroundContact = new SGroundContact(true, transform.position, Vector3.up);

        Vector2 lookDirection = CalculateClampedLookAngles();

        SPlayerLocomotion snapshot = new SPlayerLocomotion(
            transform.position,
            currentVelocity,
            forwardDirection,
            lookDirection,
            state,
            lastGroundContact);

        PushSnapshot(snapshot);
    }

    private Vector3 CalculateDesiredVelocity()
    {
        SPlayerMoveIAction moveAction = LastMoveAction;
        if (moveAction.HasInput)
        {
            return moveAction.WorldDirection * moveSpeed;
        }

        return Vector3.zero;
    }

    internal void ApplyLookAction(SPlayerLookIAction action)
    {
        if (followTarget != null)
        {
            Vector3 euler = followTarget.rotation.eulerAngles;
            euler.z = 0f;
            float pitch = NormalizeAngle180(euler.x);
            pitch = Mathf.Clamp(pitch + action.Delta.y, -maxHeadPitchDegrees, maxHeadPitchDegrees);
            euler.x = pitch;
            euler.y += action.Delta.x;
            followTarget.rotation = Quaternion.Euler(euler);
        }

        BufferIAction(action);
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

    private void UpdateForwardDirection()
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

        forwardDirection = forwardSource.sqrMagnitude > Mathf.Epsilon
            ? forwardSource.normalized
            : Vector3.forward;
    }

    private Vector2 CalculateClampedLookAngles()
    {
        if (followTarget == null)
        {
            return Vector2.zero;
        }

        Vector3 rawForward = followTarget.forward;
        if (rawForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        Quaternion targetRotation = Quaternion.LookRotation(rawForward, Vector3.up);
        Quaternion localDelta = Quaternion.Inverse(transform.rotation) * targetRotation;
        Vector3 euler = localDelta.eulerAngles;

        float yaw = Mathf.Clamp(NormalizeAngle180(euler.y), -maxHeadYawDegrees, maxHeadYawDegrees);
        float pitch = Mathf.Clamp(NormalizeAngle180(euler.x), -maxHeadPitchDegrees, maxHeadPitchDegrees);

        return new Vector2(yaw, pitch);
    }

    private static float NormalizeAngle180(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }
        else if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    private void DrawDebugVectors()
    {
        if (!drawDebugVectors)
        {
            return;
        }

        Debug.DrawRay(transform.position, forwardDirection * debugForwardLength, Color.cyan);
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
