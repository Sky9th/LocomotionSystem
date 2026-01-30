using UnityEngine;

/// <summary>
/// Bridges locomotion snapshots/intent data to presentation layers (Animator, VFX, UI).
/// Keeps LocomotionAgent slim by exposing smoothed speed/acceleration and feeding animator parameters.
/// </summary>
[DisallowMultipleComponent]
public class LocomotionAdapter : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private LocomotionAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private bool autoResolveAnimator = true;

    [Header("Debug")]
    [SerializeField] private bool logSnapshot;

    [Header("Runtime Snapshot (ReadOnly)")]
    [SerializeField, Tooltip("Speed sampled from LocomotionAgent snapshot.")]
    private float runtimeSpeed;
    [SerializeField, Tooltip("Acceleration derived from speed delta.")]
    private float runtimeAcceleration;
    [SerializeField, Tooltip("Move intent projected to X/Y.")]
    private Vector2 runtimeIntent;

    public float Speed => runtimeSpeed;
    public float Acceleration => runtimeAcceleration;
    public Vector2 Intent => runtimeIntent;
    public bool HasValidAnimatorParameters { get; private set; }

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<LocomotionAgent>();
        }

        if (autoResolveAnimator && animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (agent == null)
        {
            return;
        }

        PlayerLocomotionStruct snapshot = agent.Snapshot;

        float deltaTime = Time.deltaTime;

        runtimeAcceleration = deltaTime > Mathf.Epsilon ? (snapshot.Speed - runtimeSpeed) / Mathf.Max(deltaTime, Mathf.Epsilon) : 0f;
        runtimeSpeed = snapshot.Speed;
        runtimeIntent = agent.LastMoveIntent.RawInput;
        if (animator != null)
        {
            PushToAnimator(snapshot);
        }

        if (logSnapshot)
        {
            Logger.Log(snapshot);
        }
    }

    private void PushToAnimator(PlayerLocomotionStruct snapshot)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(LocomotionAnimatorParameters.SpeedHash, runtimeSpeed);
        animator.SetFloat(LocomotionAnimatorParameters.AccelerationHash, runtimeAcceleration);
        animator.SetInteger(LocomotionAnimatorParameters.StateHash, (int)snapshot.State);
        animator.SetFloat(LocomotionAnimatorParameters.MoveXHash, runtimeIntent.x);
        animator.SetFloat(LocomotionAnimatorParameters.MoveYHash, runtimeIntent.y);
        animator.SetBool(LocomotionAnimatorParameters.GroundedHash, snapshot.IsGrounded);
    }

    public void SetAnimator(Animator targetAnimator)
    {
        animator = targetAnimator;
    }

}
