using System;
using UnityEngine;

[Serializable]
public struct SLocomotionMotor
{
    public SLocomotionMotor(Vector2 desiredLocalVelocity,
        Vector2 actualLocalVelocity, Vector3 actualPlanarVelocity,
        Vector3 locomotionHeading, float turnAngle)
    {
        DesiredLocalVelocity = desiredLocalVelocity;
        ActualLocalVelocity = actualLocalVelocity;
        ActualPlanarVelocity = actualPlanarVelocity;
        LocomotionHeading = locomotionHeading.sqrMagnitude > Mathf.Epsilon ? locomotionHeading.normalized : Vector3.forward;
        TurnAngle = turnAngle;
    }

    public Vector2 DesiredLocalVelocity { get; }
    public Vector2 ActualLocalVelocity { get; }
    public Vector3 ActualPlanarVelocity { get; }
    public Vector3 LocomotionHeading { get; }
    public float TurnAngle { get; }

    public static SLocomotionMotor Default => new(
        Vector2.zero, Vector2.zero, Vector3.zero, Vector3.forward, 0f);
}
