using UnityEngine;

/// <summary>
/// Bridges locomotion snapshots/intent data to presentation layers (Animancer, VFX, UI).
/// Computes smoothed speed/acceleration and derived planar velocity, keeping LocomotionAgent slim.
/// </summary>
[DisallowMultipleComponent]
public class LocomotionAdapter : MonoBehaviour
{

    [Header("Dependencies")]
    [SerializeField] private LocomotionAgent agent;

    [Header("Debug")]
    [SerializeField] private bool logSnapshot;

    [Header("Runtime Snapshot (ReadOnly)")]
    [SerializeField, Tooltip("Speed sampled from LocomotionAgent snapshot.")]
    private float runtimeSpeed;
    [SerializeField, Tooltip("Acceleration derived from speed delta.")]
    private float runtimeAcceleration;
    [SerializeField, Tooltip("Move intent projected to X/Y.")]
    private Vector2 runtimeIntent;
    [SerializeField, Tooltip("Head yaw/pitch in degrees sampled from LocomotionAgent (X = yaw, Y = pitch).")]
    private Vector2 runtimeHeadLook;
    [SerializeField, Tooltip("Planar velocity projected onto right (X) / forward (Y) axes.")]
    private Vector2 runtimePlanarSpeed;

    public float Speed => runtimeSpeed;
    public float Acceleration => runtimeAcceleration;
    public Vector2 Intent => runtimeIntent;
    public Vector2 HeadLook => runtimeHeadLook;
    public Vector2 PlanarSpeed => runtimePlanarSpeed;

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<LocomotionAgent>();
        }
    }

    private void Update()
    {
        if (agent == null)
        {
            return;
        }

        SPlayerLocomotion snapshot = agent.Snapshot;

        float deltaTime = Time.deltaTime;

        runtimeAcceleration = deltaTime > Mathf.Epsilon ? (snapshot.Speed - runtimeSpeed) / Mathf.Max(deltaTime, Mathf.Epsilon) : 0f;
        runtimeSpeed = snapshot.Speed;
        runtimeIntent = agent.LastMoveAction.RawInput;
        runtimeHeadLook = snapshot.LookDirection;
        runtimePlanarSpeed = CalculatePlanarSpeed(snapshot);

        if (logSnapshot)
        {
            Logger.Log(snapshot);
        }
    }

    private Vector2 CalculatePlanarSpeed(SPlayerLocomotion snapshot)
    {
        Vector3 planarVelocity = Vector3.ProjectOnPlane(snapshot.Velocity, Vector3.up);
        Vector3 forward = snapshot.Forward.sqrMagnitude > Mathf.Epsilon ? snapshot.Forward : transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude <= Mathf.Epsilon)
        {
            right = transform.right;
        }

        forward.Normalize();
        right.Normalize();

        float rightSpeed = Vector3.Dot(planarVelocity, right);
        float forwardSpeed = Vector3.Dot(planarVelocity, forward);
        return new Vector2(rightSpeed, forwardSpeed);
    }

}
