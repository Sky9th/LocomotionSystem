using System;
using UnityEngine;

[Serializable]
public struct SCharacterKinematic
{
    public SCharacterKinematic(
        Vector3 position,
        Vector3 bodyForward,
        Vector2 lookDirection,
        SGroundContact groundContact,
        SForwardObstacleDetection forwardObstacleDetection)
    {
        Position = position;
        BodyForward = bodyForward.sqrMagnitude > Mathf.Epsilon ? bodyForward.normalized : Vector3.forward;
        LookDirection = lookDirection;
        GroundContact = groundContact;
        ForwardObstacleDetection = forwardObstacleDetection;
    }

    public Vector3 Position { get; }
    public Vector3 BodyForward { get; }
    public Vector2 LookDirection { get; }
    public SGroundContact GroundContact { get; }
    public SForwardObstacleDetection ForwardObstacleDetection { get; }

    public static SCharacterKinematic Default => new SCharacterKinematic(
        Vector3.zero,
        Vector3.forward,
        Vector2.zero,
        SGroundContact.None,
        SForwardObstacleDetection.None);
}
