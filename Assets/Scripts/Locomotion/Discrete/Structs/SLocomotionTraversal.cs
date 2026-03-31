using System;
using UnityEngine;

namespace Game.Locomotion.Discrete.Structs
{
    /// <summary>
    /// Snapshot describing the current traversal request or execution state.
    /// Traversal stays separate from regular locomotion state because it may
    /// temporarily override normal movement and animation flow.
    /// </summary>
    [Serializable]
    public readonly struct SLocomotionTraversal
    {
        public SLocomotionTraversal(
            ELocomotionTraversalType type,
            ELocomotionTraversalStage stage,
            float obstacleHeight,
            Vector3 obstaclePoint,
            Vector3 targetPoint,
            Vector3 facingDirection)
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
        public bool IsActive => Stage == ELocomotionTraversalStage.Requested || Stage == ELocomotionTraversalStage.Committed;

        public static SLocomotionTraversal Default => new SLocomotionTraversal(
            ELocomotionTraversalType.None,
            ELocomotionTraversalStage.Idle,
            0f,
            Vector3.zero,
            Vector3.zero,
            Vector3.forward);

        public static SLocomotionTraversal None => Default;
    }
}