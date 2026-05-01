using System;
using UnityEngine;

[Serializable]
public struct SCharacterMotor
{
    public SCharacterMotor(Vector2 desiredLocalVelocity, Vector2 actualLocalVelocity,
        Vector3 actualPlanarVelocity, float turnAngle)
    {
        DesiredLocalVelocity = desiredLocalVelocity;
        ActualLocalVelocity = actualLocalVelocity;
        ActualPlanarVelocity = actualPlanarVelocity;
        TurnAngle = turnAngle;
    }

    public Vector2 DesiredLocalVelocity { get; }
    public Vector2 ActualLocalVelocity { get; }
    public Vector3 ActualPlanarVelocity { get; }
    public float TurnAngle { get; }

    public static SCharacterMotor Default => new(Vector2.zero, Vector2.zero, Vector3.zero, 0f);
}
