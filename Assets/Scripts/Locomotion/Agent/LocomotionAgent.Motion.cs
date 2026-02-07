using UnityEngine;

/// <summary>
/// Locomotion simulation, heading, and turn-state helpers extracted from LocomotionAgent.
/// </summary>
public partial class LocomotionAgent : MonoBehaviour
{
    // Locomotion and orientation state
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
    private Vector2 lastMoveInput;

    private void SimulateLocomotion()
    {
        float deltaTime = GameTime.Delta;

        UpdateForwardDirection();
        UpdateFootFrontState();
        UpdateAnchorRotation();
        UpdateTurnState(deltaTime);

        if (LastMoveAction.HasInput)
        {
            lastMoveInput = LastMoveAction.RawInput.normalized;
        }

        CalculateDesiredVelocity(deltaTime);

        Vector2 localVelocity = CalculateLocalVelocity(currentVelocity);

        ELocomotionState state = currentVelocity.sqrMagnitude > Mathf.Epsilon
            ? ELocomotionState.Walk
            : ELocomotionState.Idle;

        lastGroundContact = new SGroundContact(true, transform.position, Vector3.up);

        lookDirection = CalculateClampedLookAngles();

        SPlayerLocomotion snapshot = new SPlayerLocomotion(
            transform.position,
            currentVelocity,
            forwardDirection,
            localVelocity,
            lookDirection,
            state,
            lastGroundContact,
            currentTurnAngle,
            isTurningInPlace,
            isLeftFootOnFront);

        PushSnapshot(snapshot);
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

        Vector3 forward = forwardDirection.sqrMagnitude > Mathf.Epsilon
            ? forwardDirection.normalized
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

    private void UpdateForwardDirection()
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

        forwardDirection = forwardSource.sqrMagnitude > Mathf.Epsilon
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

    private void UpdateTurnState(float deltaTime)
    {
        Vector3 bodyForward = modelRoot != null ? modelRoot.forward : transform.forward;
        bodyForward.y = 0f;
        if (bodyForward.sqrMagnitude <= Mathf.Epsilon)
        {
            bodyForward = forwardDirection;
        }

        Vector3 desiredForward = forwardDirection;
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

        float signedAngle = 0f;
        if (bodyForward.sqrMagnitude > Mathf.Epsilon && desiredForward.sqrMagnitude > Mathf.Epsilon)
        {
            signedAngle = Vector3.SignedAngle(bodyForward.normalized, desiredForward.normalized, Vector3.up);
        }

        if (turnStateCooldown > 0f)
        {
            turnStateCooldown -= deltaTime;
        }

        float absAngle = Mathf.Abs(signedAngle);

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

        currentTurnAngle = signedAngle;
    }
}
