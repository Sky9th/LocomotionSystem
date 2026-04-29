using System;
using UnityEngine;

[Serializable]
public readonly struct SLocomotionTraversal
{
    public SLocomotionTraversal(ELocomotionTraversalType type, ELocomotionTraversalStage stage,
        float obstacleHeight, Vector3 obstaclePoint, Vector3 targetPoint, Vector3 facingDirection)
    {
        Type = type;
        Stage = stage;
        ObstacleHeight = obstacleHeight;
        ObstaclePoint = obstaclePoint;
        TargetPoint = targetPoint;
        FacingDirection = facingDirection;
    }

    public ELocomotionTraversalType Type { get; }
    public ELocomotionTraversalStage Stage { get; }
    public float ObstacleHeight { get; }
    public Vector3 ObstaclePoint { get; }
    public Vector3 TargetPoint { get; }
    public Vector3 FacingDirection { get; }

    public bool HasRequest => Type != ELocomotionTraversalType.None;
    public bool IsActive => Stage == ELocomotionTraversalStage.Requested
                         || Stage == ELocomotionTraversalStage.Committed;

    public static SLocomotionTraversal None => new(
        ELocomotionTraversalType.None, ELocomotionTraversalStage.Idle,
        0f, Vector3.zero, Vector3.zero, Vector3.forward);
}
