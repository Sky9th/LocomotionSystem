using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂载在角色上的 Locomotion 组件。负责向 LocomotionManager 注册并在后续版本中执行具体运动逻辑。
/// </summary>
[DisallowMultipleComponent]
public partial class LocomotionAgent : MonoBehaviour
{
    [Header("Registration")]
    [SerializeField] private LocomotionManager manager;
    [SerializeField] private bool autoRegister = true;
    [SerializeField] private bool markAsPlayer;
    [SerializeField] private bool subscribePlayerMoveAction = true;
    [SerializeField] private bool subscribePlayerLookAction = true;

    [Header("Rig References")]
    [SerializeField] private Transform followAnchor;
    [SerializeField] private Transform modelRoot;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool autoResolveAnimator = true;

    [Header("Motion Settings")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0f)] private float acceleration = 20f;

    [Header("Head Look Limits")]
    [SerializeField, Range(0f, 90f)] private float maxHeadYawDegrees = 75f;
    [SerializeField, Range(0f, 90f)] private float maxHeadPitchDegrees = 75f;
    [SerializeField, Min(0f)] private float headLookSmoothingSpeed = 540f;

    [Header("Turn Settings")]
    [SerializeField, Range(0f, 180f)] private float turnEnterAngle = 65f;
    [SerializeField, Range(0f, 180f)] private float turnExitAngle = 20f;
    [SerializeField, Min(0f)] private float turnDebounceDuration = 0.25f;
    [SerializeField, Range(0f, 45f)] private float lookStabilityAngle = 2f;
    [SerializeField, Min(0f)] private float lookStabilityDuration = 0.15f;
    [SerializeField, Range(0f, 25f)] private float turnCompletionAngle = 5f;

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
    private bool isTurningInPlace;
    private float currentTurnAngle;
    private float turnStateCooldown;
    private float lastDesiredYaw;
    private float lookStabilityTimer;
    private bool isLeftFootOnFront;

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

        var model = transform.Find("Model");
        if (model != null)
        {
            modelRoot = model;
        }

        var follow = transform.Find("Follow");
        if (follow != null)
        {
            followAnchor = follow;
        }

        if (animator == null && autoResolveAnimator)
        {
            animator = GetComponentInChildren<Animator>();
        }

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

    private void LateUpdate()
    {
        if (modelRoot != null)
        {
            AlignPlayerToModel();
        }
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
        isTurningInPlace = false;
        currentTurnAngle = 0f;
        turnStateCooldown = 0f;
        lastDesiredYaw = 0f;
        lookStabilityTimer = 0f;
        iActionBuffer.Clear();
        UnregisterActionHandlers();
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

    internal void ApplyLookAction(SPlayerLookIAction action)
    {
        if (followAnchor != null)
        {
            Vector3 euler = followAnchor.rotation.eulerAngles;
            euler.z = 0f;
            float pitch = NormalizeAngle180(euler.x);
            pitch = Mathf.Clamp(pitch + action.Delta.y, -maxHeadPitchDegrees, maxHeadPitchDegrees);
            euler.x = pitch;
            euler.y += action.Delta.x;
            followAnchor.rotation = Quaternion.Euler(euler);
        }

        BufferIAction(action);
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

    private void AlignPlayerToModel()
    {
        var worldPos = modelRoot.position;
        transform.position = worldPos;
        modelRoot.localPosition = Vector3.zero;
    }
}
