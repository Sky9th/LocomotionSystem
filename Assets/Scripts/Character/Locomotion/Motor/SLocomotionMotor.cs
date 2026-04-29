using System;
using UnityEngine;

[Serializable]
public struct SLocomotionMotor
{
    public SLocomotionMotor(Vector2 desiredLocalVelocity, Vector3 desiredPlanarVelocity,
        Vector2 actualLocalVelocity, Vector3 actualPlanarVelocity, float actualSpeed,
        Vector3 locomotionHeading, float turnAngle, bool isLeftFootOnFront)
    {
        DesiredLocalVelocity = desiredLocalVelocity;
        DesiredPlanarVelocity = desiredPlanarVelocity;
        ActualLocalVelocity = actualLocalVelocity;
        ActualPlanarVelocity = actualPlanarVelocity;
        ActualSpeed = actualSpeed;
        LocomotionHeading = locomotionHeading.sqrMagnitude > Mathf.Epsilon ? locomotionHeading.normalized : Vector3.forward;
        TurnAngle = turnAngle;
        IsLeftFootOnFront = isLeftFootOnFront;
    }

    public Vector2 DesiredLocalVelocity { get; }
    public Vector3 DesiredPlanarVelocity { get; }
    public Vector2 ActualLocalVelocity { get; }
    public Vector3 ActualPlanarVelocity { get; }
    public float ActualSpeed { get; }
    public Vector3 LocomotionHeading { get; }
    public float TurnAngle { get; }
    public bool IsLeftFootOnFront { get; }

    public static SLocomotionMotor Default => new(
        Vector2.zero, Vector3.zero, Vector2.zero, Vector3.zero,
        0f, Vector3.forward, 0f, true);
}
