using UnityEngine;

/// <summary>
/// Locomotion simulation, heading, and turn-state helpers extracted from LocomotionAgent.
/// </summary>
public partial class LocomotionAgent : MonoBehaviour
{
    // Locomotion and orientation state
    private Vector3 currentVelocity;
    private SGroundContact lastGroundContact = SGroundContact.None;
    private Vector3 locomotionHeading = Vector3.forward;
    private Vector2 lookDirection = Vector2.zero;
    private bool isTurningInPlace;
    private float currentTurnAngle;
    private float turnStateCooldown;
    private float lastDesiredYaw;
    private float lookStabilityTimer;
    private bool isLeftFootOnFront;
    private Vector2 lastMoveInput;

    private void SimulateLocomotion()
    {
        float deltaTime = TimeConstants.Delta;

        UpdateLocomotionHeading();
        UpdateFootFrontState();
        UpdateAnchorRotation();
        CalculateTurnAngle();
        UpdateTurnState(deltaTime);

        if (LastMoveAction.HasInput)
        {
            lastMoveInput = LastMoveAction.RawInput.normalized;
        }

        CalculateDesiredVelocity(deltaTime);

        Vector2 localVelocity = CalculateLocalVelocity(currentVelocity);

        // Split into orthogonal layers: high-level state + gait + posture + condition.
        EMovementGait gait = ResolveMovementGait(currentVelocity);
        EPostureState posture = ResolvePostureState();
        ELocomotionCondition condition = ResolveLocomotionCondition();
        ELocomotionState state = ResolveHighLevelState(gait, lastGroundContact);

        lastGroundContact = new SGroundContact(true, transform.position, Vector3.up);

        lookDirection = CalculateClampedLookAngles();

        Vector3 bodyForward = modelRoot != null ? modelRoot.forward : transform.forward;
        bodyForward.y = 0f;

        SPlayerLocomotion snapshot = new SPlayerLocomotion(
            transform.position,
            currentVelocity,
            locomotionHeading,
            bodyForward,
            localVelocity,
            lookDirection,
            state,
            lastGroundContact,
            currentTurnAngle,
            isTurningInPlace,
            isLeftFootOnFront,
            posture,
            gait,
            condition);

        PushSnapshot(snapshot);
    }

    /// <summary>
    /// Derive movement gait (Idle/Walk/Run/Sprint/Crawl) from current speed only.
    /// </summary>
    private EMovementGait ResolveMovementGait(Vector3 velocity)
    {
        float speed = velocity.magnitude;
        if (speed <= Mathf.Epsilon)
        {
            return EMovementGait.Idle;
        }

        // Use MoveSpeed from config as a reference to split
        // the range [0, MoveSpeed] into three rough bands: Walk / Run / Sprint.
        float maxSpeed = Mathf.Max(config.MoveSpeed, 0.01f);
        float walkThreshold = maxSpeed * 0.4f;
        float runThreshold = maxSpeed * 0.8f;

        if (speed < walkThreshold)
        {
            return EMovementGait.Walk;
        }

        if (speed < runThreshold)
        {
            return EMovementGait.Run;
        }

        return EMovementGait.Sprint;
    }

    /// <summary>
    /// Currently treats all posture as Standing; will be refined when
    /// crouch/prone inputs are integrated.
    /// </summary>
    private EPostureState ResolvePostureState()
    {
        return EPostureState.Standing;
    }

    /// <summary>
    /// Currently always returns Normal; future implementations can map health or debuffs
    /// into different locomotion conditions (injured, heavy load, etc.).
    /// </summary>
    private ELocomotionCondition ResolveLocomotionCondition()
    {
        return ELocomotionCondition.Normal;
    }

    /// <summary>
    /// Derive a coarse high-level locomotion state from gait and ground contact.
    /// This is intended for broad Animator/FSM branching, while the detailed
    /// behaviour is driven by posture, gait, and condition.
    /// </summary>
    private static ELocomotionState ResolveHighLevelState(EMovementGait gait, SGroundContact contact)
    {
        if (!contact.IsGrounded)
        {
            return ELocomotionState.Airborne;
        }

        if (gait == EMovementGait.Idle)
        {
            return ELocomotionState.GroundedIdle;
        }

        return ELocomotionState.GroundedMoving;
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

    private Vector2 CalculateLocalVelocity(Vector3 worldVelocity)
    {
        if (LastMoveAction.HasInput)
        {
            return LastMoveAction.RawInput.normalized * worldVelocity.magnitude;
        }

        if (lastMoveInput.sqrMagnitude > Mathf.Epsilon && worldVelocity.sqrMagnitude > Mathf.Epsilon)
        {
            return lastMoveInput.normalized * worldVelocity.magnitude;
        }

        Vector3 planarVelocity = new Vector3(worldVelocity.x, 0f, worldVelocity.z);
        if (planarVelocity.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        Vector3 forward = locomotionHeading.sqrMagnitude > Mathf.Epsilon
            ? locomotionHeading.normalized
            : Vector3.forward;

        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float localY = Vector3.Dot(planarVelocity, forward);
        float localX = Vector3.Dot(planarVelocity, right);

        return new Vector2(localX, localY);
    }

    private void CalculateDesiredVelocity(float deltaTime)
    {
        Vector3 desiredVelocity;
        if (LastMoveAction.HasInput)
        {
            desiredVelocity = LastMoveAction.WorldDirection * config.MoveSpeed;
        }
        else
        {
            desiredVelocity = Vector3.zero;
        }
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, config.Acceleration * deltaTime);
    }

    private void UpdateLocomotionHeading()
    {
        Vector3 forwardSource;
        if (followAnchor != null)
        {
            forwardSource = followAnchor.forward;
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

        locomotionHeading = forwardSource.sqrMagnitude > Mathf.Epsilon
            ? forwardSource.normalized
            : Vector3.forward;
    }

    private Vector2 CalculateClampedLookAngles()
    {
        if (followAnchor == null)
        {
            return Vector2.zero;
        }

        Vector3 rawForward = followAnchor.forward;
        if (rawForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        Quaternion targetRotation = Quaternion.LookRotation(rawForward, Vector3.up);

        Quaternion bodyRotation = transform.rotation;
        if (modelRoot != null)
        {
            Vector3 bodyForward = modelRoot.forward;
            bodyForward.y = 0f;
            if (bodyForward.sqrMagnitude > Mathf.Epsilon)
            {
                bodyRotation = Quaternion.LookRotation(bodyForward.normalized, Vector3.up);
            }
        }

        Quaternion localDelta = Quaternion.Inverse(bodyRotation) * targetRotation;
        Vector3 euler = localDelta.eulerAngles;

        float yaw = Mathf.Clamp(NormalizeAngle180(euler.y), -config.MaxHeadYawDegrees, config.MaxHeadYawDegrees);
        float pitch = -Mathf.Clamp(NormalizeAngle180(euler.x), -config.MaxHeadPitchDegrees, config.MaxHeadPitchDegrees);

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

    /// <summary>
    /// Calculates the signed planar turn angle (in degrees) between the character body forward
    /// and the locomotion heading. Returns 0 if either vector has no magnitude.
    /// </summary>
    private void CalculateTurnAngle()
    {
        Vector3 bodyForward = modelRoot.forward;
        bodyForward.y = 0f;

        Vector3 desiredForward = locomotionHeading;
        desiredForward.y = 0f;
        
        currentTurnAngle = Vector3.SignedAngle(bodyForward.normalized, desiredForward.normalized, Vector3.up);
    }

    private void UpdateTurnState(float deltaTime)
    {
        Vector3 desiredForward = locomotionHeading;
        desiredForward.y = 0f;
        if (desiredForward.sqrMagnitude <= Mathf.Epsilon)
        {
            desiredForward = Vector3.forward;
        }

        float desiredYaw = Mathf.Atan2(desiredForward.x, desiredForward.z) * Mathf.Rad2Deg;
        float yawDelta = Mathf.Abs(Mathf.DeltaAngle(desiredYaw, lastDesiredYaw));
        if (yawDelta <= config.LookStabilityAngle)
        {
            lookStabilityTimer += deltaTime;
        }
        else
        {
            lookStabilityTimer = 0f;
        }
        lastDesiredYaw = desiredYaw;

        if (turnStateCooldown > 0f)
        {
            turnStateCooldown -= deltaTime;
        }

        float absAngle = Mathf.Abs(currentTurnAngle);

        bool wantsTurn = absAngle >= config.TurnEnterAngle;
        bool lookIsStable = lookStabilityTimer >= config.LookStabilityDuration;
        bool shouldCompleteTurn = absAngle <= config.TurnCompletionAngle;

        if (!isTurningInPlace && wantsTurn && turnStateCooldown <= 0f && lookIsStable)
        {
            isTurningInPlace = true;
            turnStateCooldown = config.TurnDebounceDuration;
        }
        else if (isTurningInPlace && shouldCompleteTurn)
        {
            isTurningInPlace = false;
            turnStateCooldown = config.TurnDebounceDuration;
        }
    }
}
