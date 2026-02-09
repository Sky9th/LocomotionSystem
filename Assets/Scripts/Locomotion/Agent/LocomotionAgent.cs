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

    [Header("Config")]
    [SerializeField] private LocomotionConfigProfile config;

    [Header("Debug")]
    [SerializeField] private bool drawDebugVectors;
    [SerializeField, Min(0.1f)] private float debugForwardLength = 2f;

    // Registration & identity state
    private bool isRegistered;

    // Snapshot & input buffering
    private SPlayerLocomotion latestSnapshot = SPlayerLocomotion.Default;
    private readonly Dictionary<System.Type, object> iActionBuffer = new();
    private MoveActionHandler moveActionHandler;
    private LookActionHandler lookActionHandler;

    public Transform Model => modelRoot;
    public bool IsPlayer => markAsPlayer;
    public bool IsRegistered => isRegistered;
    public SPlayerLocomotion Snapshot => latestSnapshot;
    public SPlayerMoveIAction LastMoveAction => TryGetIAction(out SPlayerMoveIAction action) ? action : SPlayerMoveIAction.None;
    public SPlayerLookIAction LastLookAction => TryGetIAction(out SPlayerLookIAction action) ? action : SPlayerLookIAction.None;
    public Vector3 LocomotionHeading => latestSnapshot.LocomotionHeading;
    public Vector2 LookDirection => latestSnapshot.LookDirection;
    public float HeadLookSmoothingSpeed => config.HeadLookSmoothingSpeed;
    public float WalkTurnSpeed => config.WalkTurnSpeed;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{nameof(LocomotionAgent)} on '{name}' requires a {nameof(LocomotionConfigProfile)} assigned.", this);
        }

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

        float deltaTime = GameTime.Delta;
        if (deltaTime <= Mathf.Epsilon)
        {
            return;
        }

        SimulateLocomotion();
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
        locomotionHeading = transform.forward;
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

    private void DrawDebugVectors()
    {
        if (!drawDebugVectors)
        {
            return;
        }

        Debug.DrawRay(transform.position, LocomotionHeading * debugForwardLength, Color.cyan);
        Debug.DrawRay(transform.position, LookDirection * debugForwardLength, Color.yellow);
        Debug.DrawRay(transform.position, latestSnapshot.BodyForward * debugForwardLength, Color.magenta);
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
