using UnityEngine;

namespace RedDust.Character.Animation
{
    /// <summary>
    /// ScriptableObject describing locomotion tuning for a specific
    /// posture + gait combination, primarily used to drive turn speeds.
    /// This is v2-only and should not be used by the legacy Locomotion.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AnimationModeConfigSO",
        menuName = "RedDust/Animation/Animation Mode Config")]
    public sealed class AnimationModeConfigSO : ScriptableObject
    {
        [Header("Locomotion Mode Key")]
        [SerializeField] private EPosture posture = EPosture.Standing;
        [SerializeField] private EMovementGait gait = EMovementGait.Walk;

        [Header("Turn Speeds (deg/sec)")]
        [SerializeField, Min(0f)] private float movingTurnSpeed = 720f;

        [Header("Animation Native Speed")]
        [Tooltip("动画在 Speed=1.0 时的实际位移速度 (m/s)，用于计算 buff/减益乘积")]
        [SerializeField, Min(0.01f)] private float animNativeSpeed = 5f;

        [Header("Turn Angles (deg)")]
        [SerializeField, Range(0f, 180f)] private float enterAngle = 90f;
        [SerializeField, Range(0f, 180f)] private float exitAngle = 20f;

        /// <summary>Posture this mode is configured for.</summary>
        public EPosture Posture => posture;

        /// <summary>Gait this mode is configured for.</summary>
        public EMovementGait Gait => gait;

        /// <summary>Animation native speed at Speed=1.0 (m/s).</summary>
        public float AnimNativeSpeed => animNativeSpeed;

        /// <summary>Turn speed when moving (walk/run/etc.).</summary>
        public float MovingTurnSpeed => movingTurnSpeed;

        /// <summary>Angle threshold for entering a dedicated turn animation.</summary>
        public float EnterAngle => enterAngle;

        /// <summary>Angle threshold for exiting a dedicated turn animation.</summary>
        public float ExitAngle => exitAngle;
    }
}
