using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Locomotion component attached to a character. Responsible for registering with
/// the LocomotionManager and driving character movement logic.
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
    public SMoveIAction LastMoveAction => TryGetIAction(out SMoveIAction action) ? action : SMoveIAction.None;
    public SLookIAction LastLookAction => TryGetIAction(out SLookIAction action) ? action : SLookIAction.None;
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

        var model = transform.Find(CommonConstants.ModelChildName);
        if (model != null)
        {
            modelRoot = model;
        }

        var follow = transform.Find(CommonConstants.FollowAnchorChildName);
        if (follow != null)
        {
            followAnchor = follow;
        }

        if (animator == null && autoResolveAnimator)
        {
            animator = GetComponentInChildren<Animator>();
        }
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

        float deltaTime = TimeConstants.Delta;
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
}
