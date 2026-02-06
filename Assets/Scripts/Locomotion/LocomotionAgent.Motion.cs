using UnityEngine;

/// <summary>
/// Locomotion simulation, heading, and turn-state helpers extracted from LocomotionAgent.
/// </summary>
public partial class LocomotionAgent : MonoBehaviour
{
    private void SimulateLocomotion(float deltaTime)
    {
        Vector3 desiredVelocity = CalculateDesiredVelocity();
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * deltaTime);

        UpdateFootFrontState();

        ELocomotionState state = currentVelocity.sqrMagnitude > Mathf.Epsilon
            ? ELocomotionState.Walk
            : ELocomotionState.Idle;

        lastGroundContact = new SGroundContact(true, transform.position, Vector3.up);

        lookDirection = CalculateClampedLookAngles();

        SPlayerLocomotion snapshot = new SPlayerLocomotion(
            transform.position,
            currentVelocity,
            forwardDirection,
            lookDirection,
            state,
            lastGroundContact,
            currentTurnAngle,
            isTurningInPlace,
            isLeftFootOnFront);

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

        float yaw = Mathf.Clamp(NormalizeAngle180(euler.y), -maxHeadYawDegrees, maxHeadYawDegrees);
        float pitch = -Mathf.Clamp(NormalizeAngle180(euler.x), -maxHeadPitchDegrees, maxHeadPitchDegrees);

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
}
